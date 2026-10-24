using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Implementations must hold authoritative identity/scope, placement, policy, evaluation,
    // hours and requirement revisions stable through the final repository CAS. A mere timer
    // or process-local lock over remote sources is NOT sufficient. No production guard exists.
    public interface ICompletionDecisionGuard
    { Task<ICompletionDecisionLease> AcquireAsync(string internshipId); }
    public interface ICompletionDecisionLease : IDisposable
    { Task<bool> IsCurrentAsync(CompletionReadiness evidence); }
    public interface ICompletionDecisionRepository
    {
        Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string internshipId);
        Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite decision, string expectedVersion);
    }
    // Constructed only by the authorized workflow, never deserialized from browser data.
    public sealed class CompletionDecisionWrite
    {
        internal CompletionDecisionWrite() { }
        public string InternshipId { get; internal set; }
        public string StudentId { get; internal set; }
        public string CoordinatorId { get; internal set; }
        public string PolicyId { get; internal set; }
        public string PolicyVersion { get; internal set; }
        public string DecisionId { get; internal set; }
        public CompletionDecisionKind Kind { get; internal set; }
        public string Reason { get; internal set; }
        public string SupportingNotes { get; internal set; }
        public string[] ReadinessBlockers { get; internal set; }
        public decimal? ApprovedHours { get; internal set; }
        public decimal? RequiredHours { get; internal set; }
        public string EvidenceDigest { get; internal set; }
    }
    public sealed class CompletionDecisionRequest
    {
        public string InternshipId { get; set; }
        public string ExpectedVersion { get; set; }
        public string ExpectedStudentId { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
        public string DecisionId { get; set; }
        public CompletionDecisionKind Kind { get; set; }
        public string Reason { get; set; }
        public string SupportingNotes { get; set; }
        public bool Confirmed { get; set; }
    }
    public sealed class CompletionDecisionView
    {
        public CompletionReadiness Readiness { get; internal set; }
        public RecordSnapshot<CompletionReview> Record { get; internal set; }
    }
    public sealed class CompletionDecisionResult
    {
        public CompletionReview Review { get; internal set; }
        public bool AlreadyRecorded { get; internal set; }
    }
    public sealed class CompletionDecisionWorkflow
    {
        private readonly IAuthenticatedUser user;
        private readonly ICoordinatorEvaluationAccess access;
        private readonly CoordinatorReadinessWorkflow readiness;
        private readonly ICompletionDecisionRepository decisions;
        private readonly ICompletionDecisionGuard guard;
        public bool IsConfigured { get { return guard != null && decisions != null; } }
        public CompletionDecisionWorkflow(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            ICoordinatorEvaluationAccess access, CoordinatorReadinessWorkflow readiness, ICompletionDecisionGuard guard,
            ICompletionDecisionRepository decisionRepository = null)
        { this.user = user; this.access = access; this.readiness = readiness ?? throw new ArgumentNullException("readiness"); this.guard = guard; decisions = decisionRepository ?? repository as ICompletionDecisionRepository; }
        private async Task Authorize(string id)
        {
            FirebaseKey.Require(id);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId) || access == null ||
                !await access.IsCoordinatorAsync(user.UserId).ConfigureAwait(false) || !await access.CanReviewInternshipAsync(user.UserId, id).ConfigureAwait(false)) throw new UnauthorizedAccessException();
        }
        public async Task<CompletionDecisionView> LoadAsync(string internshipId)
        {
            await Authorize(internshipId).ConfigureAwait(false);
            if (decisions == null) throw new InvalidOperationException("Conditional completion repository is unavailable.");
            var evidence = await readiness.ReviewAsync(internshipId).ConfigureAwait(false);
            var record = await decisions.GetCompletionReviewSnapshotAsync(internshipId).ConfigureAwait(false);
            if (evidence.Placement == null) throw new InvalidOperationException("Trusted placement is required for completion decisions.");
            if (record.Value != null && (record.Value.InternshipId != internshipId || record.Value.StudentId != evidence.Placement.StudentId))
                throw new RepositoryException("Completion review identity does not match trusted placement.");
            if (record.Value != null) FinalEvaluationValidation.Completion(record.Value);
            return new CompletionDecisionView { Readiness = evidence, Record = record };
        }
        public async Task<CompletionDecisionResult> DecideAsync(CompletionDecisionRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            // Snapshot caller input before any awaits; no browser identity or timestamp is accepted.
            var input = JsonConvert.DeserializeObject<CompletionDecisionRequest>(JsonConvert.SerializeObject(request));
            await Authorize(input.InternshipId).ConfigureAwait(false);
            if (!IsConfigured) throw new InvalidOperationException("Trusted completion decision integrations are not connected.");
            if (!input.Confirmed) throw new ArgumentException("Explicit decision confirmation is required.");
            FirebaseKey.Require(input.DecisionId);
            if (string.IsNullOrWhiteSpace(input.ExpectedVersion)) throw new ConflictException();
            using (var lease = await guard.AcquireAsync(input.InternshipId).ConfigureAwait(false))
            {
                if (lease == null) throw new InvalidOperationException("Authoritative data could not be protected for this decision.");
                var current = await LoadAsync(input.InternshipId).ConfigureAwait(false);
                var evidence = current.Readiness;
                CompletionDecisionValidation.Rule(evidence.ApplicablePolicy, evidence.Placement.PolicyId, evidence.Placement.PolicyVersion, input.Kind, input.Reason, input.SupportingNotes);
                if (input.ExpectedStudentId != evidence.Placement.StudentId || input.PolicyId != evidence.Placement.PolicyId || input.PolicyVersion != evidence.Placement.PolicyVersion) throw new ConflictException();
                var recorded = FindRetry(current.Record.Value, input);
                if (recorded != null) return new CompletionDecisionResult { Review = current.Record.Value, AlreadyRecorded = true };
                if (current.Record.Version != input.ExpectedVersion) throw new ConflictException();
                CompletionDecisionValidation.Mutable(current.Record.Value);
                if (input.Kind == CompletionDecisionKind.Approve && !evidence.IsReady)
                    throw new ArgumentException("Completion is not ready: " + string.Join(" ", evidence.Blockers));
                if (!await lease.IsCurrentAsync(CopyEvidence(evidence)).ConfigureAwait(false)) throw new ConflictException();
                await Authorize(input.InternshipId).ConfigureAwait(false);
                var write = new CompletionDecisionWrite {
                    InternshipId = input.InternshipId, StudentId = evidence.Placement.StudentId, CoordinatorId = user.UserId,
                    PolicyId = evidence.Placement.PolicyId, PolicyVersion = evidence.Placement.PolicyVersion, DecisionId = input.DecisionId, Kind = input.Kind,
                    Reason = input.Reason ?? "", SupportingNotes = input.SupportingNotes ?? "", ReadinessBlockers = evidence.Blockers.ToArray(),
                    ApprovedHours = evidence.ApprovedHours, RequiredHours = evidence.RequiredHours,
                    EvidenceDigest = Digest(evidence) };
                try
                {
                    var saved = await decisions.ApplyCompletionDecisionAsync(write, input.ExpectedVersion).ConfigureAwait(false);
                    return new CompletionDecisionResult { Review = saved };
                }
                catch (ConflictException)
                {
                    // Another identical in-flight request may have won. Only an exact persisted
                    // audit event can acknowledge that retry; other concurrent decisions conflict.
                    var latest = await decisions.GetCompletionReviewSnapshotAsync(input.InternshipId).ConfigureAwait(false);
                    if (latest.Value != null && latest.Value.InternshipId == input.InternshipId && latest.Value.StudentId == evidence.Placement.StudentId)
                    {
                        FinalEvaluationValidation.Completion(latest.Value);
                        if (FindRetry(latest.Value, input) != null)
                            return new CompletionDecisionResult { Review = latest.Value, AlreadyRecorded = true };
                    }
                    throw;
                }
            }
        }
        private CompletionDecisionAudit FindRetry(CompletionReview review, CompletionDecisionRequest request)
        {
            var found = review == null || review.DecisionHistory == null ? null : review.DecisionHistory.SingleOrDefault(x => x.DecisionId == request.DecisionId);
            if (found != null && (found.CoordinatorId != user.UserId || found.Kind != request.Kind || found.Reason != (request.Reason ?? "") ||
                found.SupportingNotes != (request.SupportingNotes ?? "") || found.PolicyId != request.PolicyId || found.PolicyVersion != request.PolicyVersion)) throw new ConflictException();
            return found;
        }
        private static string Digest(CompletionReadiness evidence)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evidence)))).Replace("-", "").ToLowerInvariant(); }
        private static T Copy<T>(T value) { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value)); }
        private static CompletionReadiness CopyEvidence(CompletionReadiness value)
        {
            return new CompletionReadiness { Placement = Copy(value.Placement), ApplicablePolicy = Copy(value.ApplicablePolicy),
                ApprovedHours = value.ApprovedHours, RequiredHours = value.RequiredHours, Blockers = value.Blockers.ToArray(),
                Evaluations = value.Evaluations == null ? null : new CoordinatorSubmittedEvaluations { PercentageEvaluation = Copy(value.Evaluations.PercentageEvaluation), LegacyRecordsDetected = value.Evaluations.LegacyRecordsDetected } };
        }
    }
    internal static class CompletionDecisionValidation
    {
        internal static void Mutable(CompletionReview current)
        { if (current != null && (current.Status == CompletionStatus.Approved || current.Status == CompletionStatus.Rejected)) throw new ConflictException(); }
        internal static void Rule(EvaluationPolicy policy, string policyId, string version, CompletionDecisionKind kind, string reason, string notes)
        {
            if (policy == null || !policy.IsProductionApproved || policy.PolicyId != policyId || policy.Version != version || policy.CompletionDecisions == null)
                throw new ArgumentException("An applicable approved completion decision policy is required.");
            if (!Enum.IsDefined(typeof(CompletionDecisionKind), kind)) throw new ArgumentException("Unsupported completion decision.");
            var rule = kind == CompletionDecisionKind.Approve ? policy.CompletionDecisions.Approve : kind == CompletionDecisionKind.RequestChanges ? policy.CompletionDecisions.RequestChanges : policy.CompletionDecisions.Reject;
            if (rule == null || !rule.Enabled) throw new ArgumentException("This decision is not explicitly supported by the approved policy.");
            if ((kind != CompletionDecisionKind.Approve || rule.RequireReason) && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A clear decision reason is required.");
            if (rule.RequireSupportingNotes && string.IsNullOrWhiteSpace(notes)) throw new ArgumentException("Supporting notes are required by the approved policy.");
            if ((reason ?? "").Length > 1000 || (notes ?? "").Length > 1000) throw new ArgumentException("Decision reason and notes must each be at most 1000 characters.");
        }
        internal static CompletionReview Apply(CompletionReview current, CompletionDecisionWrite write, EvaluationPolicy policy)
        {
            FirebaseKey.Require(write.InternshipId); FirebaseKey.Require(write.StudentId); FirebaseKey.Require(write.CoordinatorId); FirebaseKey.Require(write.DecisionId);
            Rule(policy, write.PolicyId, write.PolicyVersion, write.Kind, write.Reason, write.SupportingNotes); Mutable(current);
            if (current != null) FinalEvaluationValidation.Completion(current);
            if (current != null && (current.InternshipId != write.InternshipId || current.StudentId != write.StudentId || current.PolicyId != write.PolicyId || current.PolicyVersion != write.PolicyVersion)) throw new ConflictException();
            if (write.ReadinessBlockers == null || string.IsNullOrWhiteSpace(write.EvidenceDigest)) throw new ArgumentException("Verified readiness evidence is required.");
            if (write.Kind == CompletionDecisionKind.Approve && (write.ReadinessBlockers.Length != 0 || !write.ApprovedHours.HasValue || !write.RequiredHours.HasValue || write.RequiredHours < 0 || write.ApprovedHours < write.RequiredHours)) throw new ConflictException();
            var history = current == null || current.DecisionHistory == null ? new List<CompletionDecisionAudit>() : current.DecisionHistory.ToList();
            if (history.Any(x => x == null || string.IsNullOrWhiteSpace(x.DecisionId)) || history.Select(x => x.DecisionId).Distinct(StringComparer.Ordinal).Count() != history.Count || history.Any(x => x.DecisionId == write.DecisionId)) throw new ConflictException();
            var now = DateTimeOffset.UtcNow;
            var nextStatus = write.Kind == CompletionDecisionKind.Approve ? CompletionStatus.Approved : write.Kind == CompletionDecisionKind.Reject ? CompletionStatus.Rejected : CompletionStatus.ChangesRequested;
            if (current != null) FinalEvaluationValidation.CompletionTransition(current.Status, nextStatus);
            history.Add(new CompletionDecisionAudit { DecisionId = write.DecisionId, CoordinatorId = write.CoordinatorId, Kind = write.Kind,
                PreviousStatus = current == null ? (CompletionStatus?)null : current.Status,
                PolicyId = write.PolicyId, PolicyVersion = write.PolicyVersion, Reason = write.Reason, SupportingNotes = write.SupportingNotes,
                DecidedAtUtc = now, ReadinessBlockers = write.ReadinessBlockers.ToArray(), ApprovedHours = write.ApprovedHours, RequiredHours = write.RequiredHours, EvidenceDigest = write.EvidenceDigest });
            return new CompletionReview { InternshipId = write.InternshipId, StudentId = write.StudentId, PolicyId = write.PolicyId, PolicyVersion = write.PolicyVersion,
                ReviewerId = write.CoordinatorId, Status = nextStatus,
                Feedback = write.Reason, CreatedAtUtc = current == null ? now : current.CreatedAtUtc, UpdatedAtUtc = now, ReviewedAtUtc = now, DecisionHistory = history };
        }
    }
}
