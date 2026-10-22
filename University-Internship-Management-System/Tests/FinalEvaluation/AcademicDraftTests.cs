using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private static EvaluationPolicy AcademicPolicy()
    {
        var policy = ApprovedPolicy();
        policy.AcademicCriteria = new Dictionary<string, CriterionPolicy> {
            { "analysis", new CriterionPolicy { Label = "Academic test analysis", Minimum = -3, Maximum = 12, Required = true } },
            { "summary", new CriterionPolicy { Label = "Academic test summary", Minimum = 0, Maximum = 2, Required = true } } };
        return policy;
    }
    private static AcademicDraftPlacement AcademicPlacement()
    { return new AcademicDraftPlacement { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1" }; }
    private static AcademicDraftWorkflow AcademicFlow(IFinalEvaluationRepository repo, SubmissionAccess access = null)
    {
        return new AcademicDraftWorkflow(repo, new User { UserId = "fictional-academic" }, new Assignment { Allowed = true },
            access ?? new SubmissionAccess { AcademicRole = true, AcademicAssigned = true }, new[] { AcademicPlacement() });
    }
    private static Task AcademicSave(AcademicDraftWorkflow flow, string version, string score = "2.75", string comments = "fictional academic feedback")
    { return flow.SaveAsync("fictional-internship", version, "fictional-policy", "test-v1", new Dictionary<string, string> { { "analysis", score } }, comments); }
    private static CompanyFinalEvaluation SubmittedCompanyFixture()
    { var company = CompleteDraft(); company.Status = EvaluationStatus.Submitted; company.SubmittedAtUtc = DateTimeOffset.UtcNow; return company; }
    private sealed class AcademicWriteFailureRepository : IFinalEvaluationRepository
    {
        private readonly IFinalEvaluationRepository inner;
        public AcademicWriteFailureRepository(IFinalEvaluationRepository inner) { this.inner = inner; }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { return inner.GetCompanyAsync(id); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation v) { return inner.CreateCompanyDraftAsync(v); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation v, string version) { return inner.UpdateCompanyDraftAsync(v, version); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { return inner.GetAcademicAsync(id); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation v) { throw new RepositoryException("Simulated academic write failure."); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation v, string version) { throw new RepositoryException("Simulated academic write failure."); }
        public Task<CompletionReview> GetCompletionReviewAsync(string id) { return inner.GetCompletionReviewAsync(id); }
        public Task<CompletionDocument> GetCompletionDocumentAsync(string id, string documentId) { return inner.GetCompletionDocumentAsync(id, documentId); }
        public Task<EvaluationPolicy> GetPolicyAsync(string id) { return inner.GetPolicyAsync(id); }
    }
    private static async Task RunAcademicDraftTests()
    {
        await Test("assigned academic reviews submitted company evaluation with original criteria", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }, new[] { SubmittedCompanyFixture() });
            var review = await AcademicFlow(repo).ReviewCompanyAsync("fictional-internship");
            Assert(review.Evaluation.Status == EvaluationStatus.Submitted && review.OriginalPolicy.CompanyCriteria.ContainsKey("testCriterion"));
            Assert((await AcademicFlow(repo).LoadAsync("fictional-internship")).Record.Value == null);
        });
        await Test("unassigned academic is denied before privileged company or academic reads", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository { SimulateFailure = true };
            var flow = AcademicFlow(repo, new SubmissionAccess { AcademicRole = true, AcademicAssigned = false });
            await Reject<UnauthorizedAccessException>(() => flow.ReviewCompanyAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => flow.LoadAsync("fictional-internship"));
        });
        await Test("company Draft and missing company records are not exposed as submitted", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }, new[] { CompleteDraft() });
            Assert((await AcademicFlow(repo).ReviewCompanyAsync("fictional-internship")).Evaluation == null);
            Assert((await AcademicFlow(new InMemoryFinalEvaluationRepository()).ReviewCompanyAsync("fictional-internship")).Evaluation == null);
        });
        await Test("students cannot review confidential company records or save academic drafts", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository { SimulateFailure = true };
            var flow = AcademicFlow(repo, new SubmissionAccess { AcademicRole = false, AcademicAssigned = true });
            await Reject<UnauthorizedAccessException>(() => flow.ReviewCompanyAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => AcademicSave(flow, null));
        });
        await Test("academic draft create accepts incomplete scores and blank comments", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() });
            await AcademicSave(AcademicFlow(repo), null, "", "");
            var loaded = await AcademicFlow(repo).LoadAsync("fictional-internship");
            Assert(loaded.Record.Value.Status == EvaluationStatus.Draft && loaded.Record.Value.Scores["analysis"] == null && loaded.Record.Value.Feedback == "");
        });
        await Test("academic draft update and reload are independent from company scores", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }, new[] { SubmittedCompanyFixture() });
            await AcademicSave(AcademicFlow(repo), null); var original = await AcademicFlow(repo).LoadAsync("fictional-internship");
            await AcademicSave(AcademicFlow(repo), original.Record.Version, "11.5", "updated academic feedback");
            var fresh = await AcademicFlow(repo).LoadAsync("fictional-internship");
            Assert(fresh.Record.Value.Scores["analysis"] == 11.5m && !fresh.Record.Value.Scores.ContainsKey("testCriterion"));
            Assert(fresh.Record.Value.Feedback == "updated academic feedback" && fresh.Record.Version != original.Record.Version);
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value.Scores["testCriterion"] == 3);
        });
        await Test("academic draft rejects invalid scores and company criterion IDs", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }); var flow = AcademicFlow(repo);
            await Reject<ArgumentException>(() => AcademicSave(flow, null, "99"));
            await Reject<ArgumentException>(() => AcademicSave(flow, null, "not numeric"));
            await Reject<ArgumentException>(() => flow.SaveAsync("fictional-internship", null, "fictional-policy", "test-v1", new Dictionary<string, string> { { "testCriterion", "3" } }, ""));
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value == null);
        });
        await Test("academic draft denies missing unapproved and empty academic policies", async () =>
        {
            var unapproved = AcademicPolicy(); unapproved.IsApproved = false;
            var empty = AcademicPolicy(); empty.AcademicCriteria.Clear();
            foreach (var repo in new[] { new InMemoryFinalEvaluationRepository(), new InMemoryFinalEvaluationRepository(new[] { unapproved }), new InMemoryFinalEvaluationRepository(new[] { empty }) })
                await Reject<ArgumentException>(() => AcademicSave(AcademicFlow(repo), null));
        });
        await Test("stale academic saves and duplicate creates do not overwrite newer drafts", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }); var flow = AcademicFlow(repo);
            await AcademicSave(flow, null); var stale = await flow.LoadAsync("fictional-internship");
            await AcademicSave(flow, stale.Record.Version, "4");
            await Reject<ConflictException>(() => AcademicSave(flow, stale.Record.Version, "5"));
            await Reject<ConflictException>(() => AcademicSave(flow, null, "5"));
            Assert((await flow.LoadAsync("fictional-internship")).Record.Value.Scores["analysis"] == 4);
        });
        await Test("concurrent academic draft updates have exactly one winner", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }); var flow = AcademicFlow(repo);
            await AcademicSave(flow, null); var snap = await flow.LoadAsync("fictional-internship");
            var attempts = Enumerable.Range(0, 2).Select(i => Task.Run(async () => {
                try { await AcademicSave(AcademicFlow(repo), snap.Record.Version, (i + 4).ToString()); return true; }
                catch (ConflictException) { return false; }
            })).ToArray();
            Assert((await Task.WhenAll(attempts)).Count(x => x) == 1);
        });
        await Test("Submitted academic records stay read only and cannot be recreated", async () =>
        {
            var value = new AcademicFinalEvaluation { InternshipId = "fictional-internship", StudentId = "fictional-student", EvaluatorId = "fictional-academic",
                PolicyId = "fictional-policy", PolicyVersion = "test-v1", Status = EvaluationStatus.Submitted, SubmittedAtUtc = DateTimeOffset.UtcNow };
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }, academics: new[] { value });
            var loaded = await AcademicFlow(repo).LoadAsync(value.InternshipId); Assert(loaded.IsSubmitted);
            await Reject<ConflictException>(() => AcademicSave(AcademicFlow(repo), loaded.Record.Version));
            await Reject<ConflictException>(() => AcademicSave(AcademicFlow(repo), null));
            Assert((await repo.GetAcademicAsync(value.InternshipId)).Value.Status == EvaluationStatus.Submitted);
        });
        await Test("academic write failures preserve input and stored values without false success", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }); var flow = AcademicFlow(new AcademicWriteFailureRepository(repo));
            var input = new Dictionary<string, string> { { "analysis", "4" } };
            await Reject<RepositoryException>(() => flow.SaveAsync("fictional-internship", null, "fictional-policy", "test-v1", input, "preserve me"));
            Assert(input["analysis"] == "4" && (await repo.GetAcademicAsync("fictional-internship")).Value == null);
            await AcademicSave(AcademicFlow(repo), null); var loaded = await AcademicFlow(repo).LoadAsync("fictional-internship");
            await Reject<RepositoryException>(() => AcademicSave(flow, loaded.Record.Version, "5"));
            Assert((await repo.GetAcademicAsync("fictional-internship")).Value.Scores["analysis"] == 2.75m);
            repo.SimulateFailure = true;
            await Reject<RepositoryException>(() => AcademicFlow(repo).LoadAsync("fictional-internship"));
            Assert(input["analysis"] == "4");
        });
        await Test("academic access fails closed without user assignment or role adapters", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository { SimulateFailure = true };
            var user = new User { UserId = null };
            var flow = new AcademicDraftWorkflow(repo, user, new Assignment { Allowed = true }, new SubmissionAccess { AcademicRole = true, AcademicAssigned = true }, new[] { AcademicPlacement() });
            await Reject<UnauthorizedAccessException>(() => flow.LoadAsync("fictional-internship"));
            flow = new AcademicDraftWorkflow(repo, new User(), new Assignment { Allowed = true }, null, new[] { AcademicPlacement() });
            await Reject<UnauthorizedAccessException>(() => flow.LoadAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => AcademicFlow(repo).LoadAsync("browser-forged-internship"));
        });
        await Test("academic drafts reject evaluator mismatch and policy revision mismatch", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }); var flow = AcademicFlow(repo);
            await Reject<ConflictException>(() => flow.SaveAsync("fictional-internship", null, "fictional-policy", "wrong-version", new Dictionary<string, string>(), ""));
            var other = new AcademicFinalEvaluation { InternshipId = "fictional-internship", StudentId = "fictional-student", EvaluatorId = "another-academic", PolicyId = "fictional-policy", PolicyVersion = "test-v1" };
            var seeded = new InMemoryFinalEvaluationRepository(new[] { AcademicPolicy() }, academics: new[] { other });
            await Reject<UnauthorizedAccessException>(() => AcademicFlow(seeded).LoadAsync("fictional-internship"));
        });
        await Test("company review retains recorded values if original criterion policy is unavailable", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(companies: new[] { SubmittedCompanyFixture() });
            var review = await AcademicFlow(repo).ReviewCompanyAsync("fictional-internship");
            Assert(review.Evaluation.Scores["testCriterion"] == 3 && review.OriginalPolicy == null);
        });
    }
}
