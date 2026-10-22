using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class AcademicRequirements : IAcademicSubmissionRequirements
    {
        public bool Eligible = true, Satisfied = true;
        public Func<AcademicFinalEvaluation, EvaluationPolicy, bool> Rule;
        public Task<bool> IsEligibleAsync(string id) { return Task.FromResult(Eligible); }
        public Task<bool> ArePolicyRequirementsSatisfiedAsync(AcademicFinalEvaluation candidate, EvaluationPolicy policy)
        { return Task.FromResult(Satisfied && (Rule == null || Rule(candidate, policy))); }
    }
    private sealed class CoordinatorAccess : ICoordinatorEvaluationAccess
    {
        public bool Role = true, Scope = true;
        public Task<bool> IsCoordinatorAsync(string uid) { return Task.FromResult(Role); }
        public Task<bool> CanReviewInternshipAsync(string uid, string id) { return Task.FromResult(Scope); }
    }
    private static EvaluationPolicy AcademicSubmissionPolicy()
    { var p = AcademicPolicy(); p.AcademicCommentsRequired = true; p.PassingThreshold = 70; return p; }
    private static AcademicFinalEvaluation CompleteAcademicDraft()
    {
        return new AcademicFinalEvaluation { InternshipId = "fictional-internship", StudentId = "fictional-student", EvaluatorId = "fictional-academic",
            PolicyId = "fictional-policy", PolicyVersion = "test-v1", Feedback = "fictional academic final comments",
            Scores = new Dictionary<string, decimal?> { { "analysis", 4 }, { "summary", 1 } } };
    }
    private static InMemoryFinalEvaluationRepository AcademicSubmissionRepo(EvaluationPolicy policy = null)
    { return new InMemoryFinalEvaluationRepository(new[] { policy ?? AcademicSubmissionPolicy() }, new[] { SubmittedCompanyFixture() }, new[] { CompleteAcademicDraft() }); }
    private static AcademicSubmissionService AcademicSubmitService(IFinalEvaluationRepository repo, SubmissionAccess access = null, AcademicRequirements requirements = null)
    { return new AcademicSubmissionService(repo, new User { UserId = "fictional-academic" }, access ?? new SubmissionAccess { AcademicRole = true, AcademicAssigned = true }, requirements ?? new AcademicRequirements()); }
    private static AcademicDraftWorkflow AcademicSubmitFlow(IFinalEvaluationRepository repo)
    { return new AcademicDraftWorkflow(repo, new User { UserId = "fictional-academic" }, new Assignment { Allowed = true }, new SubmissionAccess { AcademicRole = true, AcademicAssigned = true }, new[] { AcademicPlacement() }, new AcademicRequirements()); }
    private static CoordinatorEvaluationReader CoordinatorReader(IFinalEvaluationRepository repo, CoordinatorAccess access = null)
    { return new CoordinatorEvaluationReader(repo, new User { UserId = "fictional-coordinator" }, access ?? new CoordinatorAccess()); }
    private sealed class AcademicSubmitFailureRepository : IFinalEvaluationRepository, IAcademicSubmissionRepository
    {
        private readonly IFinalEvaluationRepository inner;
        public AcademicSubmitFailureRepository(IFinalEvaluationRepository inner) { this.inner = inner; }
        public Task SubmitAcademicAsync(AcademicFinalEvaluation value, string version) { throw new RepositoryException("Simulated submission write failure."); }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { return inner.GetCompanyAsync(id); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation value) { return inner.CreateCompanyDraftAsync(value); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation value, string version) { return inner.UpdateCompanyDraftAsync(value, version); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { return inner.GetAcademicAsync(id); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation value) { return inner.CreateAcademicDraftAsync(value); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation value, string version) { return inner.UpdateAcademicDraftAsync(value, version); }
        public Task<CompletionReview> GetCompletionReviewAsync(string id) { return inner.GetCompletionReviewAsync(id); }
        public Task<CompletionDocument> GetCompletionDocumentAsync(string id, string doc) { return inner.GetCompletionDocumentAsync(id, doc); }
        public Task<EvaluationPolicy> GetPolicyAsync(string id) { return inner.GetPolicyAsync(id); }
    }
    private static async Task RunAcademicSubmissionTests()
    {
        await Test("academic submission transitions a saved Draft with backend timestamp and preserved creation time", async () =>
        {
            var repo = AcademicSubmissionRepo(); var before = await repo.GetAcademicAsync("fictional-internship"); var start = DateTimeOffset.UtcNow;
            before.Value.CreatedAtUtc = DateTimeOffset.UtcNow.AddYears(-1);
            await AcademicSubmitService(repo).SubmitAsync(before.Value, before.Version);
            var after = await repo.GetAcademicAsync("fictional-internship");
            Assert(after.Value.Status == EvaluationStatus.Submitted && after.Value.SubmittedAtUtc >= start && after.Value.SubmittedAtUtc <= DateTimeOffset.UtcNow);
            Assert(after.Value.CreatedAtUtc == default(DateTimeOffset) && after.Version != before.Version && before.Value.Status == EvaluationStatus.Draft);
        });
        await Test("academic submission requires configured scores comments and criterion feedback", async () =>
        {
            var policy = AcademicSubmissionPolicy(); policy.AcademicCriteria["analysis"].RequireFeedback = true;
            var repo = AcademicSubmissionRepo(policy); var snap = await repo.GetAcademicAsync("fictional-internship");
            snap.Value.Scores.Remove("summary"); await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Scores["summary"] = 1; snap.Value.Feedback = " "; await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Feedback = "comments"; await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.CriterionFeedback["analysis"] = "policy-required feedback";
            await AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version);
        });
        await Test("academic submission respects explicitly optional scores and comments", async () =>
        {
            var policy = AcademicSubmissionPolicy(); policy.AcademicCommentsRequired = false;
            foreach (var criterion in policy.AcademicCriteria.Values) criterion.Required = false;
            var repo = AcademicSubmissionRepo(policy); var snap = await repo.GetAcademicAsync("fictional-internship"); snap.Value.Scores.Clear(); snap.Value.Feedback = "";
            await AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version);
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.Status == EvaluationStatus.Submitted);
        });
        await Test("academic final scores and feedback reject unknown criteria and invalid ranges", async () =>
        {
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            snap.Value.Scores["analysis"] = 99; await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Scores["analysis"] = 4; snap.Value.Scores["companyWork"] = 4; await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Scores.Remove("companyWork"); snap.Value.CriterionFeedback["unknown"] = "feedback";
            await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.Status == EvaluationStatus.Draft);
        });
        await Test("academic submission fails for missing unapproved and unspecified final policies", async () =>
        {
            var unapproved = AcademicSubmissionPolicy(); unapproved.IsApproved = false;
            var unspecified = AcademicSubmissionPolicy(); unspecified.AcademicCommentsRequired = null;
            foreach (var repo in new[] { AcademicSubmissionRepo(unapproved), AcademicSubmissionRepo(unspecified), new InMemoryFinalEvaluationRepository(academics: new[] { CompleteAcademicDraft() }) })
            {
                var snap = await repo.GetAcademicAsync("fictional-internship");
                await Reject<ArgumentException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            }
        });
        await Test("academic submission denies wrong role unassigned supervisor spoofed evaluator and anonymous user", async () =>
        {
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            await Reject<UnauthorizedAccessException>(() => AcademicSubmitService(repo, new SubmissionAccess { AcademicRole = false, AcademicAssigned = true }).SubmitAsync(snap.Value, snap.Version));
            await Reject<UnauthorizedAccessException>(() => AcademicSubmitService(repo, new SubmissionAccess { AcademicRole = true, AcademicAssigned = false }).SubmitAsync(snap.Value, snap.Version));
            await Reject<UnauthorizedAccessException>(() => new AcademicSubmissionService(repo, new User { UserId = null }, new SubmissionAccess(), new AcademicRequirements()).SubmitAsync(snap.Value, snap.Version));
            snap.Value.EvaluatorId = "browser-spoof"; await Reject<UnauthorizedAccessException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
        });
        await Test("academic submission fails closed without eligibility and policy requirement confirmation", async () =>
        {
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            await Reject<ArgumentException>(() => AcademicSubmitService(repo, requirements: new AcademicRequirements { Eligible = false }).SubmitAsync(snap.Value, snap.Version));
            await Reject<ArgumentException>(() => AcademicSubmitService(repo, requirements: new AcademicRequirements { Satisfied = false }).SubmitAsync(snap.Value, snap.Version));
            await Reject<UnauthorizedAccessException>(() => new AcademicSubmissionService(repo, new User { UserId = "fictional-academic" }, new SubmissionAccess { AcademicRole = true, AcademicAssigned = true }, null).SubmitAsync(snap.Value, snap.Version));
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.Status == EvaluationStatus.Draft);
        });
        await Test("academic submission requires an existing Draft and blocks duplicates and later edits", async () =>
        {
            var empty = new InMemoryFinalEvaluationRepository(new[] { AcademicSubmissionPolicy() });
            await Reject<ConflictException>(() => AcademicSubmitService(empty).SubmitAsync(CompleteAcademicDraft(), "missing-version"));
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            await Reject<ConflictException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, null));
            await AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version);
            var locked = await repo.GetAcademicAsync("fictional-internship");
            await Reject<ConflictException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
            await Reject<ConflictException>(() => repo.UpdateAcademicDraftAsync(snap.Value, locked.Version));
            await Reject<ConflictException>(() => repo.CreateAcademicDraftAsync(snap.Value));
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.SubmittedAtUtc == locked.Value.SubmittedAtUtc);
        });
        await Test("policy-specific academic checks receive current final values without mutation leaks", async () =>
        {
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            snap.Value.Scores["analysis"] = 5;
            var requirements = new AcademicRequirements { Rule = (candidate, policy) => {
                Assert(candidate.Scores["analysis"] == 5 && policy.IsApproved && policy.AcademicCommentsRequired == true);
                candidate.Scores["analysis"] = 999; policy.IsApproved = false; return true;
            } };
            await AcademicSubmitService(repo, requirements: requirements).SubmitAsync(snap.Value, snap.Version);
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.Scores["analysis"] == 5);
            Assert((await repo.GetPolicyAsync("fictional-policy")).IsApproved);
        });
        await Test("stale academic submission preserves a newer draft", async () =>
        {
            var repo = AcademicSubmissionRepo(); var stale = await repo.GetAcademicAsync("fictional-internship");
            var newer = CompleteAcademicDraft(); newer.Feedback = "newer draft"; await repo.UpdateAcademicDraftAsync(newer, stale.Version);
            await Reject<ConflictException>(() => AcademicSubmitService(repo).SubmitAsync(stale.Value, stale.Version));
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.Feedback == "newer draft");
        });
        await Test("simultaneous academic submissions have exactly one winner", async () =>
        {
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            var attempts = Enumerable.Range(0, 2).Select(i => Task.Run(async () => {
                try { await AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version); return true; }
                catch (ConflictException) { return false; }
            })).ToArray();
            Assert((await Task.WhenAll(attempts)).Count(x => x) == 1);
        });
        await Test("academic submission failures do not acknowledge success or mutate input or draft", async () =>
        {
            var repo = AcademicSubmissionRepo(); var snap = await repo.GetAcademicAsync("fictional-internship");
            await Reject<RepositoryException>(() => AcademicSubmitService(new AcademicSubmitFailureRepository(repo)).SubmitAsync(snap.Value, snap.Version));
            Assert(snap.Value.Status == EvaluationStatus.Draft && (await repo.GetAcademicAsync("fictional-internship")).Version == snap.Version);
            repo.SimulateFailure = true; await Reject<RepositoryException>(() => AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version));
        });
        await Test("academic form workflow submits current values and criterion feedback atomically", async () =>
        {
            var repo = AcademicSubmissionRepo(); var flow = AcademicSubmitFlow(repo); var snap = await flow.LoadAsync("fictional-internship");
            await flow.SubmitAsync("fictional-internship", snap.Record.Version, "fictional-policy", "test-v1",
                new Dictionary<string, string> { { "analysis", "5" }, { "summary", "2" } }, "current academic form", new Dictionary<string, string> { { "analysis", "academic feedback" } });
            var loaded = await flow.LoadAsync("fictional-internship");
            Assert(loaded.IsSubmitted && loaded.Record.Value.Scores["analysis"] == 5 && loaded.Record.Value.CriterionFeedback["analysis"] == "academic feedback");
            await Reject<UnauthorizedAccessException>(() => AcademicFlow(repo).SubmitAsync("fictional-internship", loaded.Record.Version, "fictional-policy", "test-v1", new Dictionary<string, string>(), ""));
        });
        await Test("authorized Coordinator retrieves both actual submitted evaluations without database mutation", async () =>
        {
            var repo = AcademicSubmissionRepo(); var beforeCompany = await repo.GetCompanyAsync("fictional-internship"); var snap = await repo.GetAcademicAsync("fictional-internship");
            await AcademicSubmitService(repo).SubmitAsync(snap.Value, snap.Version); var submitted = await repo.GetAcademicAsync("fictional-internship");
            var result = await CoordinatorReader(repo).GetSubmittedAsync("fictional-internship");
            Assert(result.Company.Status == EvaluationStatus.Submitted && result.Academic.Status == EvaluationStatus.Submitted);
            result.Academic.Feedback = "local mutation";
            Assert((await CoordinatorReader(repo).GetSubmittedAsync("fictional-internship")).Academic.Feedback != "local mutation");
            Assert((await repo.GetCompanyAsync("fictional-internship")).Version == beforeCompany.Version && (await repo.GetAcademicAsync("fictional-internship")).Version == submitted.Version);
            Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null);
        });
        await Test("Coordinator handoff never exposes Draft or missing evaluation details", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicSubmissionPolicy() }, new[] { CompleteDraft() }, new[] { CompleteAcademicDraft() });
            var result = await CoordinatorReader(repo).GetSubmittedAsync("fictional-internship"); Assert(result.Company == null && result.Academic == null);
            result = await CoordinatorReader(new InMemoryFinalEvaluationRepository()).GetSubmittedAsync("fictional-internship"); Assert(result.Company == null && result.Academic == null);
            var submitted = CompleteAcademicDraft(); submitted.Status = EvaluationStatus.Submitted;
            result = await CoordinatorReader(new InMemoryFinalEvaluationRepository(academics: new[] { submitted })).GetSubmittedAsync("fictional-internship");
            Assert(result.Company == null && result.Academic.Status == EvaluationStatus.Submitted);
        });
        await Test("Coordinator handoff denies students out-of-scope users anonymous users and missing adapters before reads", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository { SimulateFailure = true };
            await Reject<UnauthorizedAccessException>(() => CoordinatorReader(repo, new CoordinatorAccess { Role = false, Scope = true }).GetSubmittedAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => CoordinatorReader(repo, new CoordinatorAccess { Role = true, Scope = false }).GetSubmittedAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => new CoordinatorEvaluationReader(repo, new User { UserId = "fictional-student" }, new CoordinatorAccess { Role = false, Scope = true }).GetSubmittedAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => new CoordinatorEvaluationReader(repo, new User { UserId = null }, new CoordinatorAccess()).GetSubmittedAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => new CoordinatorEvaluationReader(repo, new User { UserId = "fictional-student" }, null).GetSubmittedAsync("fictional-internship"));
        });
        await Test("Coordinator handoff rejects inconsistent student identities and propagates retrieval errors", async () =>
        {
            var company = SubmittedCompanyFixture(); company.StudentId = "different-student";
            var academic = CompleteAcademicDraft(); academic.Status = EvaluationStatus.Submitted;
            var repo = new InMemoryFinalEvaluationRepository(companies: new[] { company }, academics: new[] { academic });
            await Reject<RepositoryException>(() => CoordinatorReader(repo).GetSubmittedAsync("fictional-internship"));
            repo.SimulateFailure = true; await Reject<RepositoryException>(() => CoordinatorReader(repo).GetSubmittedAsync("fictional-internship"));
        });
        await Test("Firebase academic submission uses exact ETag server timestamps and explicit HTTP failures", async () =>
        {
            int puts = 0; HttpStatusCode status = HttpStatusCode.OK;
            var handler = new Handler { Respond = request => {
                if (request.Method == HttpMethod.Get) return Response(JsonConvert.SerializeObject(request.RequestUri.AbsolutePath.StartsWith("/evaluationPolicies/") ? (object)AcademicSubmissionPolicy() : CompleteAcademicDraft()));
                Assert(request.Method == HttpMethod.Put && request.RequestUri.AbsolutePath == "/academicFinalEvaluations/fictional-internship.json" && string.Join("", request.Headers.GetValues("if-match")) == "\"test-version\"");
                var body = Newtonsoft.Json.Linq.JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert((string)body["status"] == "Submitted" && DateTimeOffset.Parse((string)body["submittedAtUtc"]) > DateTimeOffset.UtcNow.AddMinutes(-1));
                puts++; return status == HttpStatusCode.OK ? Response("null") : new HttpResponseMessage(status);
            } };
            await AcademicSubmitService(Rest(handler)).SubmitAsync(CompleteAcademicDraft(), "\"test-version\""); Assert(puts == 1);
            status = HttpStatusCode.PreconditionFailed; await Reject<ConflictException>(() => AcademicSubmitService(Rest(handler)).SubmitAsync(CompleteAcademicDraft(), "\"test-version\""));
            status = HttpStatusCode.ServiceUnavailable; await Reject<RepositoryException>(() => AcademicSubmitService(Rest(handler)).SubmitAsync(CompleteAcademicDraft(), "\"test-version\""));
        });
    }
}
