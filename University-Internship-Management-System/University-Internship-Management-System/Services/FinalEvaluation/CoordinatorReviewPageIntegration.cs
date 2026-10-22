using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class CoordinatorReviewPageContext
    {
        private readonly IAuthenticatedUser user;
        private readonly ICoordinatorEvaluationAccess access;
        private readonly CompletionPlacement[] placements;
        public CoordinatorReadinessWorkflow Workflow { get; private set; }
        public CompletionDecisionWorkflow Decisions { get; private set; }
        public string UserId { get { return user == null ? null : user.UserId; } }
        public bool IsDevelopment { get; internal set; }
        public string DevelopmentDatasetPath { get; internal set; }
        public Func<string, Task<CompletionDocument>> GenerateReportAsync { get; set; }
        public CoordinatorReviewPageContext(CoordinatorReadinessWorkflow workflow, IAuthenticatedUser user,
            ICoordinatorEvaluationAccess access, IEnumerable<CompletionPlacement> placements, CompletionDecisionWorkflow decisions = null)
        { Workflow = workflow ?? throw new ArgumentNullException("workflow"); this.user = user; this.access = access; this.placements = placements.ToArray(); Decisions = decisions; }
        public async Task<CompletionPlacement[]> GetAuthorizedPlacementsAsync()
        {
            if (user == null || string.IsNullOrWhiteSpace(user.UserId) || access == null || !await access.IsCoordinatorAsync(user.UserId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            var allowed = new List<CompletionPlacement>();
            foreach (var placement in placements)
                if (await access.CanReviewInternshipAsync(user.UserId, placement.InternshipId).ConfigureAwait(false)) allowed.Add(placement);
            return allowed.ToArray();
        }
    }
    public static class CoordinatorReviewPageIntegration
    {
        // Only trusted server composition may register this. No production adapter is registered.
        public static Func<HttpContext, CoordinatorReviewPageContext> Factory { get; set; }
        public static CoordinatorReviewPageContext Resolve(HttpContext context)
        {
            if (Factory != null) return Factory(context);
            var hub = PercentageDevelopmentHub.Resolve(context);
            if (hub != null)
            {
                var user = new PercentageDevelopmentHub.User("local-coordinator");
                var readiness = new CoordinatorReadinessWorkflow(hub.Repository, user, hub.Source, hub.Source, hub.Source, hub.Source, hub.Source, hub.Source);
                return new CoordinatorReviewPageContext(readiness, user, hub.Source, hub.Source.Placements,
                    new CompletionDecisionWorkflow(hub.Repository, user, hub.Source, readiness, hub.Guard)) { IsDevelopment = true, DevelopmentDatasetPath = hub.DatasetPath, GenerateReportAsync = hub.GenerateReportAsync };
            }
            if (!context.IsDebuggingEnabled || !context.Request.IsLocal || Environment.GetEnvironmentVariable("UIMS_COORDINATOR_REVIEW_IN_MEMORY") != "1") return null;
            bool enableDecisions = Environment.GetEnvironmentVariable("UIMS_COORDINATOR_DECISIONS_IN_MEMORY") == "1";
            const string sessionKey = "FinalEvaluation.Coordinator.DecisionDevelopmentContext";
            if (enableDecisions && context.Session[sessionKey] is CoordinatorReviewPageContext) return (CoordinatorReviewPageContext)context.Session[sessionKey];
            // Independent fictional fixtures. Never constructs Firebase or loads credentials.
            var source = new DevelopmentSources();
            var policy = new EvaluationPolicy { PolicyId = "dev-readiness-policy", Version = "test-v1", IsApproved = true, RequiredHours = 10 };
            var notesPolicy = new EvaluationPolicy { PolicyId = "dev-notes-policy", Version = "test-v1", IsApproved = true, RequiredHours = 10 };
            if (enableDecisions)
            {
                policy.CompletionDecisions = new CompletionDecisionPolicy { Approve = new CompletionDecisionRule { Enabled = true }, RequestChanges = new CompletionDecisionRule { Enabled = true }, Reject = new CompletionDecisionRule { Enabled = false } };
                notesPolicy.CompletionDecisions = new CompletionDecisionPolicy { Approve = new CompletionDecisionRule { Enabled = true, RequireReason = true, RequireSupportingNotes = true } };
            }
            var companies = source.Placements.Where(p => p.InternshipId != "dev-review-missing").Select(p => new CompanyFinalEvaluation {
                InternshipId = p.InternshipId, StudentId = p.StudentId, EvaluatorId = "fictional-company", PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion,
                Status = EvaluationStatus.Submitted, SubmittedAtUtc = DateTimeOffset.Parse("2026-10-20T10:00:00Z"), Feedback = "Fictional submitted company feedback <test>",
                Scores = new Dictionary<string, decimal?> { { "testCriterion", 4 } }, CriterionFeedback = new Dictionary<string, string> { { "testCriterion", "Fictional company criterion feedback" } } }).ToArray();
            var academics = source.Placements.Select(p => new AcademicFinalEvaluation {
                InternshipId = p.InternshipId, StudentId = p.StudentId, EvaluatorId = "fictional-academic", PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion,
                Status = p.InternshipId == "dev-review-missing" ? EvaluationStatus.Draft : EvaluationStatus.Submitted,
                SubmittedAtUtc = p.InternshipId == "dev-review-missing" ? null : (DateTimeOffset?)DateTimeOffset.Parse("2026-10-21T11:00:00Z"),
                Feedback = p.InternshipId == "dev-review-missing" ? "CONFIDENTIAL_DRAFT_MUST_NOT_APPEAR" : "Fictional submitted academic feedback",
                Scores = new Dictionary<string, decimal?> { { "analysis", 5 } } }).ToArray();
            var repository = new InMemoryFinalEvaluationRepository(new[] { policy, notesPolicy }, companies, academics);
            var workflow = new CoordinatorReadinessWorkflow(repository, source, source, source, source, source, source, source);
            var decisions = enableDecisions ? new CompletionDecisionWorkflow(repository, source, source, workflow, source, new DevelopmentDecisionStore(repository)) : null;
            var result = new CoordinatorReviewPageContext(workflow, source, source, source.Placements, decisions) { IsDevelopment = true };
            if (enableDecisions) context.Session[sessionKey] = result;
            return result;
        }
        private sealed class DevelopmentDecisionStore : ICompletionDecisionRepository
        {
            private readonly InMemoryFinalEvaluationRepository repository;
            internal DevelopmentDecisionStore(InMemoryFinalEvaluationRepository repository) { this.repository = repository; }
            public Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string id) { return repository.GetCompletionReviewSnapshotAsync(id); }
            public Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite decision, string version)
            {
                if (decision.InternshipId == "dev-review-write-failure") throw new RepositoryException("Simulated isolated decision persistence failure.");
                return repository.ApplyCompletionDecisionAsync(decision, version);
            }
        }
        private sealed class DevelopmentSources : IAuthenticatedUser, ICoordinatorEvaluationAccess, ICompletionPlacementReader,
            IApprovedTrainingHoursReader, IRequiredTrainingHoursReader, IRequiredTasksReader, IRequiredReportsReader, ICompletionDecisionGuard
        {
            internal readonly CompletionPlacement[] Placements = new[] { "dev-review-ready", "dev-review-blocked", "dev-review-missing", "dev-review-stale", "dev-review-notes", "dev-review-write-failure" }
                .Select(id => new CompletionPlacement { InternshipId = id, StudentId = id + "-student", DisplayName = "Fictional " + id, PolicyId = id == "dev-review-notes" ? "dev-notes-policy" : "dev-readiness-policy", PolicyVersion = "test-v1" }).ToArray();
            private readonly SemaphoreSlim decisionGate = new SemaphoreSlim(1, 1);
            // Only immutable local fixtures are protected by this process-local gate. It is NOT a real integration guard.
            public async Task<ICompletionDecisionLease> AcquireAsync(string id)
            { await decisionGate.WaitAsync().ConfigureAwait(false); return new DevelopmentLease(decisionGate, id); }
            private sealed class DevelopmentLease : ICompletionDecisionLease
            {
                private SemaphoreSlim gate; private readonly string id;
                internal DevelopmentLease(SemaphoreSlim gate, string id) { this.gate = gate; this.id = id; }
                public Task<bool> IsCurrentAsync(CompletionReadiness evidence) { return Task.FromResult(gate != null && evidence.Placement != null && evidence.Placement.InternshipId == id); }
                public void Dispose() { var held = Interlocked.Exchange(ref gate, null); if (held != null) held.Release(); }
            }
            public string UserId { get { return "dev-readiness-coordinator"; } }
            public Task<bool> IsCoordinatorAsync(string uid) { return Task.FromResult(uid == UserId); }
            public Task<bool> CanReviewInternshipAsync(string uid, string id) { return Task.FromResult(uid == UserId && Placements.Any(p => p.InternshipId == id)); }
            public Task<CompletionPlacement> GetAsync(string id) { return Task.FromResult(Placements.SingleOrDefault(p => p.InternshipId == id)); }
            private TrustedCompletionData<T> Data<T>(string id, T value)
            { var p = Placements.Single(x => x.InternshipId == id); return new TrustedCompletionData<T> { IsTrusted = true, InternshipId = id, StudentId = p.StudentId, PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion, Value = value }; }
            Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id)
            { return Task.FromResult(Data(id, new[] { new TrainingHourEntry { EntryId = "approved", Hours = id == "dev-review-blocked" ? 3 : 10, IsApproved = true }, new TrainingHourEntry { EntryId = "pending", Hours = 100, IsApproved = false } })); }
            Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { return Task.FromResult(Data<decimal?>(id, 10)); }
            Task<TrustedCompletionData<CompletionChecklist>> IRequiredTasksReader.GetAsync(string id)
            { return Task.FromResult(Data(id, new CompletionChecklist { RequiredIds = new[] { "fictional-task" }, CompletedIds = id == "dev-review-blocked" ? new string[0] : new[] { "fictional-task" } })); }
            Task<TrustedCompletionData<CompletionChecklist>> IRequiredReportsReader.GetAsync(string id)
            { return Task.FromResult(Data(id, new CompletionChecklist { RequiredIds = new[] { "fictional-report" }, CompletedIds = new[] { "fictional-report" } })); }
        }
    }
}
