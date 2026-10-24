using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // These are server integration contracts, never browser attestations. Adapters must verify
    // provenance and current applicability. Null or IsTrusted=false means unknown, not complete.
    public sealed class CompletionPlacement
    {
        public string InternshipId { get; set; }
        public string StudentId { get; set; }
        public string DisplayName { get; set; }
        public string CompanyName { get; set; }
        public string PlacementId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
    }
    public sealed class TrustedCompletionData<T>
    {
        public bool IsTrusted { get; set; }
        public string InternshipId { get; set; }
        public string StudentId { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
        public T Value { get; set; }
    }
    public sealed class TrainingHourEntry
    {
        public string EntryId { get; set; }
        public decimal Hours { get; set; }
        public bool IsApproved { get; set; }
    }
    public sealed class CompletionChecklist
    {
        // Explicit empty RequiredIds is valid only when the trusted source defines no requirements.
        public string[] RequiredIds { get; set; }
        public string[] CompletedIds { get; set; }
    }
    public interface ICompletionPlacementReader { Task<CompletionPlacement> GetAsync(string internshipId); }
    public interface IApprovedTrainingHoursReader { Task<TrustedCompletionData<TrainingHourEntry[]>> GetAsync(string internshipId); }
    public interface IRequiredTrainingHoursReader { Task<TrustedCompletionData<decimal?>> GetAsync(string internshipId); }
    public interface IRequiredTasksReader { Task<TrustedCompletionData<CompletionChecklist>> GetAsync(string internshipId); }
    public interface IRequiredReportsReader { Task<TrustedCompletionData<CompletionChecklist>> GetAsync(string internshipId); }
    public interface ICompletionEligibilityReader { Task<TrustedCompletionData<bool?>> GetEligibilityAsync(string internshipId); }
    public sealed class CompletionReadiness
    {
        public CompletionPlacement Placement { get; internal set; }
        public CoordinatorSubmittedEvaluations Evaluations { get; internal set; }
        public EvaluationPolicy ApplicablePolicy { get; internal set; }
        public decimal? ApprovedHours { get; internal set; }
        public decimal? RequiredHours { get; internal set; }
        public string[] Blockers { get; internal set; }
        public bool IsReady { get { return Blockers != null && Blockers.Length == 0; } }
    }
    // Read-only snapshot for human review. Not an atomic approval or permission to mutate completion.
    public sealed class CoordinatorReadinessWorkflow
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICoordinatorEvaluationAccess access;
        private readonly ICompletionPlacementReader placements;
        private readonly IApprovedTrainingHoursReader hours;
        private readonly IRequiredTrainingHoursReader requiredHours;
        private readonly IRequiredTasksReader tasks;
        private readonly IRequiredReportsReader reports;
        public CoordinatorReadinessWorkflow(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            ICoordinatorEvaluationAccess access, ICompletionPlacementReader placements,
            IApprovedTrainingHoursReader hours, IRequiredTrainingHoursReader requiredHours,
            IRequiredTasksReader tasks, IRequiredReportsReader reports)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository");
            this.user = user; this.access = access; this.placements = placements;
            this.hours = hours; this.requiredHours = requiredHours; this.tasks = tasks; this.reports = reports;
        }
        public async Task<CompletionReadiness> ReviewAsync(string internshipId)
        {
            FirebaseKey.Require(internshipId);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId) || access == null ||
                !await access.IsCoordinatorAsync(user.UserId).ConfigureAwait(false) ||
                !await access.CanReviewInternshipAsync(user.UserId, internshipId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            var reasons = new List<string>();
            var result = new CompletionReadiness();
            var placement = await Read(() => placements == null ? Task.FromResult<CompletionPlacement>(null) : placements.GetAsync(internshipId), reasons, "Trusted placement data is unavailable.");
            if (placement == null || placement.InternshipId != internshipId || string.IsNullOrWhiteSpace(placement.StudentId) ||
                string.IsNullOrWhiteSpace(placement.PolicyId) || string.IsNullOrWhiteSpace(placement.PolicyVersion))
            { reasons.Add("Trusted placement and applicable policy reference are required."); result.Blockers = reasons.ToArray(); return result; }
            result.Placement = placement;
            result.Evaluations = await Read(() => new CoordinatorEvaluationReader(repository, user, access).GetSubmittedAsync(internshipId), reasons, "Submitted evaluations could not be retrieved.");
            var evaluations = result.Evaluations;
            var percentage = evaluations == null ? null : evaluations.PercentageEvaluation;
            if (evaluations != null && evaluations.LegacyRecordsDetected) reasons.Add("Legacy evaluation records require an explicit audited migration; old academic scores are not review approval.");
            if (percentage == null || !percentage.Current.SubmittedAtUtc.HasValue) reasons.Add("Company final evaluation is missing or not Submitted.");
            var academicReview = PercentageEvaluationWorkflow.CurrentReview(percentage);
            if (academicReview == null) reasons.Add("Academic Review pending for the current Company evaluation version.");
            else if (academicReview.Kind == AcademicReviewKind.RequestRevision) reasons.Add("Academic Request Revision blocks completion until a revised submission is approved.");
            if (percentage != null && (percentage.StudentId != placement.StudentId || percentage.PolicyId != placement.PolicyId || percentage.PolicyVersion != placement.PolicyVersion))
            { reasons.Add("Current evaluation does not match trusted placement and policy."); result.Evaluations = null; }
            if (evaluations != null)
            {
                foreach (var evaluation in new EvaluationRecord[] { evaluations.Company, evaluations.Academic }.Where(e => e != null))
                    if (evaluation.StudentId != placement.StudentId || evaluation.PolicyId != placement.PolicyId || evaluation.PolicyVersion != placement.PolicyVersion || !evaluation.SubmittedAtUtc.HasValue)
                    { reasons.Add("Submitted evaluation identity, policy version or submission timestamp does not match trusted placement requirements."); result.Evaluations = null; break; }
            }
            var policy = await Read(() => repository.GetPolicyAsync(FirebaseKey.Require(placement.PolicyId)), reasons, "Applicable evaluation policy could not be retrieved.");
            result.ApplicablePolicy = policy;
            if (policy == null || policy.PolicyId != placement.PolicyId || policy.Version != placement.PolicyVersion)
                reasons.Add("Applicable university policy is missing or its version does not match.");
            else if (!policy.IsProductionApproved) reasons.Add(policy.IsTestPolicy ? "Applicable policy is marked for development/testing and cannot authorize official completion." : "Applicable university policy is not approved.");
            if (policy != null && policy.IsProductionApproved)
            {
                if (!policy.PassingThreshold.HasValue) reasons.Add("Approved passing threshold is unavailable; final result cannot be determined.");
                else if (policy.PassingThreshold < 0 || policy.PassingThreshold > 100) reasons.Add("Approved passing threshold is invalid.");
                else if (percentage != null && percentage.Current.Percentage < policy.PassingThreshold) reasons.Add("Company final percentage is below the approved passing threshold.");
            }
            var eligibilitySource = placements as ICompletionEligibilityReader;
            var eligible = await Read(() => eligibilitySource == null ? Task.FromResult<TrustedCompletionData<bool?>>(null) : eligibilitySource.GetEligibilityAsync(internshipId), reasons, "Trusted internship eligibility is unavailable.");
            if (!Matches(eligible, placement) || eligible.Value != true) reasons.Add("Internship eligibility is missing, untrusted or not satisfied.");
            var approved = await Read(() => hours == null ? Task.FromResult<TrustedCompletionData<TrainingHourEntry[]>>(null) : hours.GetAsync(internshipId), reasons, "Approved-hours source is unavailable.");
            if (!Matches(approved, placement) || approved.Value == null || approved.Value.Any(e => e == null || string.IsNullOrWhiteSpace(e.EntryId) || e.Hours < 0) ||
                approved.Value.Select(e => e.EntryId).Distinct(StringComparer.Ordinal).Count() != approved.Value.Length)
                reasons.Add("Trusted approved-hours ledger is missing or invalid.");
            else
            {
                try { result.ApprovedHours = approved.Value.Where(e => e.IsApproved).Sum(e => e.Hours); }
                catch (OverflowException) { reasons.Add("Approved-hours ledger total is invalid."); }
            }
            var required = await Read(() => requiredHours == null ? Task.FromResult<TrustedCompletionData<decimal?>>(null) : requiredHours.GetAsync(internshipId), reasons, "Required-hours source is unavailable.");
            if (!Matches(required, placement) || !required.Value.HasValue || required.Value < 0)
                reasons.Add("Trusted required training hours are missing or invalid.");
            else
            {
                result.RequiredHours = required.Value;
                if (policy != null && policy.RequiredHours.HasValue && policy.RequiredHours != required.Value) reasons.Add("Required hours conflict with the applicable policy.");
            }
            if (result.ApprovedHours.HasValue && result.RequiredHours.HasValue && result.ApprovedHours < result.RequiredHours)
                reasons.Add("Approved training hours are insufficient.");
            Check(await Read(() => tasks == null ? Task.FromResult<TrustedCompletionData<CompletionChecklist>>(null) : tasks.GetAsync(internshipId), reasons, "Required-task source is unavailable."), placement, "tasks", reasons);
            Check(await Read(() => reports == null ? Task.FromResult<TrustedCompletionData<CompletionChecklist>>(null) : reports.GetAsync(internshipId), reasons, "Required-report source is unavailable."), placement, "reports", reasons);
            result.Blockers = reasons.ToArray(); return result;
        }
        private static bool Matches<T>(TrustedCompletionData<T> data, CompletionPlacement placement)
        { return data != null && data.IsTrusted && data.InternshipId == placement.InternshipId && data.StudentId == placement.StudentId && data.PolicyId == placement.PolicyId && data.PolicyVersion == placement.PolicyVersion; }
        private static void Check(TrustedCompletionData<CompletionChecklist> data, CompletionPlacement placement, string name, List<string> reasons)
        {
            if (!Matches(data, placement) || data.Value == null || data.Value.RequiredIds == null || data.Value.CompletedIds == null ||
                data.Value.RequiredIds.Concat(data.Value.CompletedIds).Any(string.IsNullOrWhiteSpace) ||
                data.Value.RequiredIds.Distinct(StringComparer.Ordinal).Count() != data.Value.RequiredIds.Length)
            { reasons.Add("Trusted required " + name + " data is missing or invalid."); return; }
            var missing = data.Value.RequiredIds.Except(data.Value.CompletedIds, StringComparer.Ordinal).ToArray();
            if (missing.Length > 0) reasons.Add("Required " + name + " are incomplete: " + string.Join(", ", missing) + ".");
        }
        private static async Task<T> Read<T>(Func<Task<T>> read, List<string> reasons, string failure) where T : class
        {
            try { return await read().ConfigureAwait(false); }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { reasons.Add(failure); return null; }
        }
    }
}
