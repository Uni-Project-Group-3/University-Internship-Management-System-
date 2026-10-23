using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public interface IPercentageEvaluationRepository
    {
        Task<RecordSnapshot<PercentageEvaluation>> GetPercentageAsync(string internshipId);
        Task SavePercentageAsync(PercentageEvaluation value, string expectedVersion);
    }
    public interface IPercentageEvaluationAccess
    {
        Task<bool> CanCompanyAsync(string userId, CompletionPlacement placement);
        Task<bool> CanAcademicAsync(string userId, CompletionPlacement placement);
        Task<bool> IsEligibleAsync(CompletionPlacement placement);
    }
    public sealed class PercentageEvaluationWorkflow
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IPercentageEvaluationRepository percentages;
        private readonly IAuthenticatedUser user;
        private readonly IPercentageEvaluationAccess access;
        private readonly ICompletionPlacementReader placements;
        private readonly ICompletionDecisionGuard guard;
        public PercentageEvaluationWorkflow(IFinalEvaluationRepository repository, IAuthenticatedUser user, IPercentageEvaluationAccess access, ICompletionPlacementReader placements, ICompletionDecisionGuard guard = null)
        { this.repository = repository; percentages = repository as IPercentageEvaluationRepository; this.user = user; this.access = access; this.placements = placements; this.guard = guard; }
        private async Task<ICompletionDecisionLease> Mutation(string id)
        {
            if (guard == null) throw new UnauthorizedAccessException("Trusted evaluation/completion concurrency guard is unavailable.");
            var lease = await guard.AcquireAsync(id);
            if (lease == null) throw new ConflictException();
            try { var completion = await repository.GetCompletionReviewAsync(id); if (completion != null && (completion.Status == CompletionStatus.Approved || completion.Status == CompletionStatus.Rejected)) throw new ConflictException(); return lease; }
            catch { lease.Dispose(); throw; }
        }
        private async Task<CompletionPlacement> Authorize(string id, bool academic)
        {
            FirebaseKey.Require(id);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId) || access == null || placements == null || percentages == null) throw new UnauthorizedAccessException("Trusted identity, assignment and review adapters are required.");
            var p = await placements.GetAsync(id);
            if (p == null || p.InternshipId != id || string.IsNullOrWhiteSpace(p.StudentId) ||
                !(academic ? await access.CanAcademicAsync(user.UserId, p) : await access.CanCompanyAsync(user.UserId, p))) throw new UnauthorizedAccessException();
            return p;
        }
        public async Task<RecordSnapshot<PercentageEvaluation>> LoadAsync(string id, bool academic = false)
        {
            var p = await Authorize(id, academic);
            var snapshot = await percentages.GetPercentageAsync(id);
            if (snapshot.Value != null) Validate(snapshot.Value, p);
            // A legacy record must be migrated explicitly, not silently replaced with a blank v2.
            if (snapshot.Value == null && ((await repository.GetCompanyAsync(id)).Value != null || (await repository.GetAcademicAsync(id)).Value != null))
                throw new ArgumentException("Legacy evaluation detected. An authorized, audited migration is required; old scores are not academic approval.");
            return snapshot;
        }
        public async Task SaveDraftAsync(string id, string version, decimal? percentage, string comments, bool openRevision = false)
        {
            var p = await Authorize(id, false); var old = await LoadAsync(id); CheckVersion(old, version);
            using (var lease = await Mutation(id)) {
            old = await LoadAsync(id); CheckVersion(old, version);
            var policy = await Policy(p); ValidatePercentage(percentage); Comments(comments);
            var value = old.Value ?? new PercentageEvaluation { InternshipId = id, StudentId = p.StudentId, PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion };
            if (value.Current != null && value.Current.CompanySupervisorId != user.UserId) throw new UnauthorizedAccessException();
            if (value.Current != null && value.Current.SubmittedAtUtc.HasValue)
            {
                var review = CurrentReview(value);
                if (!openRevision || review == null || review.Kind != AcademicReviewKind.RequestRevision) throw new ConflictException();
                value.Current = NewVersion(value.Current.Number + 1);
            }
            else if (openRevision) throw new ConflictException();
            if (value.Current == null) value.Current = NewVersion(1);
            value.Current.Percentage = percentage; value.Current.Comments = comments ?? ""; value.Current.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await percentages.SavePercentageAsync(value, old.Version);
            }
        }
        private CompanyPercentageVersion NewVersion(int number) { return new CompanyPercentageVersion { VersionId = Guid.NewGuid().ToString("N"), Number = number, CompanySupervisorId = user.UserId }; }
        public async Task SubmitAsync(string id, string version, bool confirmed)
        {
            var p = await Authorize(id, false); if (!confirmed) throw new ArgumentException("Confirm final submission.");
            using (var lease = await Mutation(id)) {
            var old = await LoadAsync(id); CheckVersion(old, version); var value = old.Value;
            if (value == null || value.Current == null || value.Current.SubmittedAtUtc.HasValue) throw new ConflictException();
            if (value.Current.CompanySupervisorId != user.UserId) throw new UnauthorizedAccessException();
            await Policy(p); if (!await access.IsEligibleAsync(p)) throw new UnauthorizedAccessException("Internship eligibility is not verified.");
            ValidatePercentage(value.Current.Percentage); if (!value.Current.Percentage.HasValue || string.IsNullOrWhiteSpace(value.Current.Comments)) throw new ArgumentException("A final percentage and comments are required.");
            value.Current.UpdatedAtUtc = DateTimeOffset.UtcNow; value.Current.SubmittedAtUtc = value.Current.UpdatedAtUtc;
            value.Submissions.Add(Clone(value.Current)); await percentages.SavePercentageAsync(value, old.Version);
            }
        }
        public async Task ReviewAsync(string id, string version, string companyVersion, AcademicReviewKind kind, string reason, string decisionId, bool confirmed)
        {
            var p = await Authorize(id, true); if (!confirmed || !Enum.IsDefined(typeof(AcademicReviewKind), kind)) throw new ArgumentException("Confirm a valid review decision.");
            FirebaseKey.Require(decisionId); Comments(reason);
            if (kind == AcademicReviewKind.RequestRevision && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A clear revision reason is required.");
            using (var lease = await Mutation(id)) {
            var old = await LoadAsync(id, true); CheckVersion(old, version); var value = old.Value;
            if (value == null || value.Current == null || !value.Current.SubmittedAtUtc.HasValue || value.Current.VersionId != companyVersion || CurrentReview(value) != null || value.Reviews.Any(r => r.DecisionId == decisionId)) throw new ConflictException();
            await Policy(p); if (!await access.IsEligibleAsync(p)) throw new UnauthorizedAccessException("Internship eligibility is not verified.");
            value.Reviews.Add(new AcademicReviewDecision { DecisionId = decisionId, CompanyVersionId = companyVersion, Kind = kind, Reason = reason ?? "", AcademicSupervisorId = user.UserId, DecidedAtUtc = DateTimeOffset.UtcNow });
            await percentages.SavePercentageAsync(value, old.Version);
            }
        }
        private async Task<EvaluationPolicy> Policy(CompletionPlacement p)
        {
            if (string.IsNullOrWhiteSpace(p.PolicyId) || string.IsNullOrWhiteSpace(p.PolicyVersion)) throw new ArgumentException("An explicit placement/evaluation policy reference is required. No default university policy is selected.");
            var policy = await repository.GetPolicyAsync(p.PolicyId);
            if (policy == null || !policy.IsApproved || policy.PolicyId != p.PolicyId || policy.Version != p.PolicyVersion) throw new ArgumentException("An applicable approved university policy is required.");
            if (policy.PassingThreshold.HasValue && (policy.PassingThreshold < 0 || policy.PassingThreshold > 100)) throw new ArgumentException("Policy passing percentage is invalid.");
            return policy;
        }
        public async Task<string> OutcomeAsync(string id, bool academic = false)
        {
            var p = await Authorize(id, academic); var snapshot = await LoadAsync(id, academic);
            if (string.IsNullOrWhiteSpace(p.PolicyId) || string.IsNullOrWhiteSpace(p.PolicyVersion)) return "Cannot yet be determined: an explicit applicable policy reference is required.";
            var policy = await repository.GetPolicyAsync(p.PolicyId);
            if (policy == null || !policy.IsApproved || policy.PolicyId != p.PolicyId || policy.Version != p.PolicyVersion || !policy.PassingThreshold.HasValue || policy.PassingThreshold < 0 || policy.PassingThreshold > 100 || snapshot.Value?.Current.SubmittedAtUtc == null)
                return "Cannot yet be determined: submitted result and approved passing threshold are required.";
            return (snapshot.Value.Current.Percentage >= policy.PassingThreshold ? "Pass" : "Fail") + " against the applicable approved passing threshold (" + policy.PassingThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%). Academic review and Coordinator completion remain separate decisions.";
        }
        public static AcademicReviewDecision CurrentReview(PercentageEvaluation value) { return value == null || value.Current == null ? null : value.Reviews.SingleOrDefault(r => r.CompanyVersionId == value.Current.VersionId); }
        public static string Status(PercentageEvaluation value)
        {
            if (value == null || value.Current == null) return "Not Started";
            if (!value.Current.SubmittedAtUtc.HasValue) return "Draft";
            var review = CurrentReview(value); return review == null ? "Submitted" : review.Kind == AcademicReviewKind.Approve ? "Approved" : "Revision Requested";
        }
        private static void CheckVersion(RecordSnapshot<PercentageEvaluation> old, string version) { if (string.IsNullOrWhiteSpace(version) || old.Version != version) throw new ConflictException(); }
        public static T Clone<T>(T value) { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value)); }
        public static void ValidatePercentage(decimal? value) { if (value.HasValue && (value < 0 || value > 100)) throw new ArgumentException("Final Evaluation Percentage must be between 0 and 100."); }
        private static void Comments(string value) { if (value != null && value.Length > 3000) throw new ArgumentException("Comments and reasons cannot exceed 3,000 characters."); }
        public static void Validate(PercentageEvaluation value, CompletionPlacement p = null)
        {
            if (value.SchemaVersion != 2 || value.Current == null || value.Reviews == null || value.Submissions == null) throw new ArgumentException("Invalid evaluation version contract.");
            if (value.Submissions.Any(v => v == null) || value.Reviews.Any(r => r == null)) throw new ArgumentException("Invalid history entry.");
            FirebaseKey.Require(value.InternshipId); FirebaseKey.Require(value.StudentId); FirebaseKey.Require(value.PolicyId); FirebaseKey.Require(value.PolicyVersion);
            if (p != null && (p.InternshipId != value.InternshipId || p.StudentId != value.StudentId || p.PolicyId != value.PolicyId || p.PolicyVersion != value.PolicyVersion)) throw new UnauthorizedAccessException();
            foreach (var v in value.Submissions.Concat(new[] { value.Current })) { FirebaseKey.Require(v.VersionId); FirebaseKey.Require(v.CompanySupervisorId); ValidatePercentage(v.Percentage); Comments(v.Comments); if (v.Number < 1 || (v.SubmittedAtUtc.HasValue && (!v.Percentage.HasValue || string.IsNullOrWhiteSpace(v.Comments)))) throw new ArgumentException("Invalid submitted version."); }
            if (value.Submissions.Select(v => v.VersionId).Distinct().Count() != value.Submissions.Count || value.Submissions.Select(v => v.Number).Distinct().Count() != value.Submissions.Count || value.Reviews.Select(r => r.CompanyVersionId).Distinct().Count() != value.Reviews.Count || value.Reviews.Select(r => r.DecisionId).Distinct().Count() != value.Reviews.Count) throw new ArgumentException("Duplicate history.");
            foreach (var r in value.Reviews) { FirebaseKey.Require(r.DecisionId); FirebaseKey.Require(r.AcademicSupervisorId); if (!Enum.IsDefined(typeof(AcademicReviewKind), r.Kind) || !value.Submissions.Any(v => v.VersionId == r.CompanyVersionId) || (r.Kind == AcademicReviewKind.RequestRevision && string.IsNullOrWhiteSpace(r.Reason))) throw new ArgumentException("Invalid review history."); }
            if (value.Current.SubmittedAtUtc.HasValue && !value.Submissions.Any(v => JsonConvert.SerializeObject(v) == JsonConvert.SerializeObject(value.Current))) throw new ArgumentException("Current submission differs from immutable history.");
            if (value.Current.Number != value.Submissions.Count + (value.Current.SubmittedAtUtc.HasValue ? 0 : 1) || !value.Submissions.Select(v => v.Number).SequenceEqual(Enumerable.Range(1, value.Submissions.Count))) throw new ArgumentException("Non-sequential version history.");
            foreach (var v in value.Submissions) if (!v.SubmittedAtUtc.HasValue || v.SubmittedAtUtc.Value == default(DateTimeOffset) || v.UpdatedAtUtc != v.SubmittedAtUtc.Value) throw new ArgumentException("Invalid server submission timestamp.");
            foreach (var r in value.Reviews) if (r.DecidedAtUtc == default(DateTimeOffset) || r.DecidedAtUtc < value.Submissions.Single(v => v.VersionId == r.CompanyVersionId).SubmittedAtUtc) throw new ArgumentException("Invalid review timestamp.");
        }
    }
}
