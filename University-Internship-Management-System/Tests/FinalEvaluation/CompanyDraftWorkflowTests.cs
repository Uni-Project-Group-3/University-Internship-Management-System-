using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private static CompanyDraftPlacement Placement()
    {
        return new CompanyDraftPlacement { InternshipId = "fictional-internship", StudentId = "fictional-student",
            PolicyId = "fictional-policy", PolicyVersion = "test-v1", StudentName = "Fictional test student" };
    }
    private static CompanyDraftWorkflow Workflow(IFinalEvaluationRepository repo, bool authorized = true)
    {
        return new CompanyDraftWorkflow(repo, new User(), new Assignment { Allowed = authorized }, new[] { Placement() });
    }
    private static Task Save(CompanyDraftWorkflow workflow, string version, string score, string comments = "fictional draft")
    {
        return workflow.SaveAsync("fictional-internship", version, "fictional-policy", "test-v1",
            new Dictionary<string, string> { { "testCriterion", score } },
            new Dictionary<string, string> { { "testCriterion", "criterion feedback" } }, comments);
    }
    private sealed class WriteFailureRepository : IFinalEvaluationRepository
    {
        private readonly IFinalEvaluationRepository inner;
        public WriteFailureRepository(IFinalEvaluationRepository inner) { this.inner = inner; }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { return inner.GetCompanyAsync(id); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation value) { throw new RepositoryException("Simulated write failure."); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation value, string version) { throw new RepositoryException("Simulated write failure."); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { return inner.GetAcademicAsync(id); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation value) { return inner.CreateAcademicDraftAsync(value); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation value, string version) { return inner.UpdateAcademicDraftAsync(value, version); }
        public Task<CompletionReview> GetCompletionReviewAsync(string id) { return inner.GetCompletionReviewAsync(id); }
        public Task<CompletionDocument> GetCompletionDocumentAsync(string id, string documentId) { return inner.GetCompletionDocumentAsync(id, documentId); }
        public Task<EvaluationPolicy> GetPolicyAsync(string id) { return inner.GetPolicyAsync(id); }
    }
    private static async Task RunCompanyDraftTests()
    {
        await Test("company page workflow loads policy for an unanswered draft", async () =>
        {
            var state = await Workflow(new InMemoryFinalEvaluationRepository(new[] { Policy() })).LoadAsync("fictional-internship");
            Assert(state.Record.Value == null && state.Policy.CompanyCriteria["testCriterion"].Maximum == 7 && !state.IsSubmitted);
        });
        await Test("company page workflow creates partial draft with criterion feedback", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() });
            await Save(Workflow(repo), null, "", "");
            var read = await Workflow(repo).LoadAsync("fictional-internship");
            Assert(read.Record.Value.Scores["testCriterion"] == null && read.Record.Value.Feedback == "" && read.Record.Value.CriterionFeedback["testCriterion"] == "criterion feedback");
        });
        await Test("company page workflow updates and reloads across workflow instances", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() });
            await Save(Workflow(repo), null, "2.75"); var first = await Workflow(repo).LoadAsync("fictional-internship");
            await Save(Workflow(repo), first.Record.Version, "6.25", "updated");
            var fresh = await Workflow(repo).LoadAsync("fictional-internship");
            Assert(fresh.Record.Value.Scores["testCriterion"] == 6.25m && fresh.Record.Value.Feedback == "updated" && fresh.Record.Version != first.Record.Version);
        });
        await Test("company page workflow rejects policy violations without replacing draft", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() }); var flow = Workflow(repo);
            await Save(flow, null, "3"); var read = await flow.LoadAsync("fictional-internship");
            await Reject<ArgumentException>(() => Save(flow, read.Record.Version, "8"));
            await Reject<ArgumentException>(() => Save(flow, read.Record.Version, "not a number"));
            Assert((await flow.LoadAsync("fictional-internship")).Record.Value.Scores["testCriterion"] == 3);
        });
        await Test("company page workflow refuses stale updates and duplicate create", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() }); var flow = Workflow(repo);
            await Save(flow, null, "3"); var stale = await flow.LoadAsync("fictional-internship");
            await Save(flow, stale.Record.Version, "4");
            await Reject<ConflictException>(() => Save(flow, stale.Record.Version, "5"));
            await Reject<ConflictException>(() => Save(flow, null, "5"));
            Assert((await flow.LoadAsync("fictional-internship")).Record.Value.Scores["testCriterion"] == 4);
        });
        await Test("company page workflow reads Submitted as locked and rejects save", async () =>
        {
            var submitted = Draft(); submitted.Status = EvaluationStatus.Submitted;
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() }, new[] { submitted }); var flow = Workflow(repo);
            var loaded = await flow.LoadAsync("fictional-internship"); Assert(loaded.IsSubmitted);
            await Reject<ConflictException>(() => Save(flow, loaded.Record.Version, "4"));
            Assert((await repo.GetCompanyAsync(submitted.InternshipId)).Value.Status == EvaluationStatus.Submitted);
        });
        await Test("company page workflow requires trusted placement and assigned evaluator", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() });
            await Reject<UnauthorizedAccessException>(() => Workflow(repo).LoadAsync("browser-forged-internship"));
            await Reject<UnauthorizedAccessException>(() => Workflow(repo, false).LoadAsync("fictional-internship"));
            var other = Draft(); other.EvaluatorId = "browser-forged-evaluator";
            var seeded = new InMemoryFinalEvaluationRepository(new[] { Policy() }, new[] { other });
            await Reject<UnauthorizedAccessException>(() => Workflow(seeded).LoadAsync("fictional-internship"));
        });
        await Test("company page workflow reports read and write failures without mutating input", async () =>
        {
            var repo = new InMemoryFinalEvaluationRepository(new[] { Policy() });
            var scores = new Dictionary<string, string> { { "testCriterion", "4" } };
            var flow = Workflow(new WriteFailureRepository(repo));
            await Reject<RepositoryException>(() => flow.SaveAsync("fictional-internship", null, "fictional-policy", "test-v1", scores, new Dictionary<string, string>(), "preserve me"));
            Assert(scores["testCriterion"] == "4" && (await repo.GetCompanyAsync("fictional-internship")).Value == null);
            await Save(Workflow(repo), null, "3"); var state = await Workflow(repo).LoadAsync("fictional-internship");
            await Reject<RepositoryException>(() => Save(flow, state.Record.Version, "4"));
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value.Scores["testCriterion"] == 3);
            repo.SimulateFailure = true;
            await Reject<RepositoryException>(() => Workflow(repo).LoadAsync("fictional-internship"));
        });
    }
}
