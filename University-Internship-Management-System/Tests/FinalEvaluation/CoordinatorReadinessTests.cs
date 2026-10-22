using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class ReadinessSources : ICompletionPlacementReader, IApprovedTrainingHoursReader,
        IRequiredTrainingHoursReader, IRequiredTasksReader, IRequiredReportsReader, ICompletionEligibilityReader
    {
        internal CompletionPlacement Placement = new CompletionPlacement { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1" };
        internal bool Trusted = true, Fail, MissingPlacement, MissingHours, MissingRequired, MissingTasks, MissingReports;
        internal int Reads;
        internal string DataStudent = "fictional-student", DataVersion = "test-v1";
        internal decimal? Required = 10;
        internal TrainingHourEntry[] Entries = new[] { new TrainingHourEntry { EntryId = "approved", Hours = 10, IsApproved = true }, new TrainingHourEntry { EntryId = "pending", Hours = 100, IsApproved = false } };
        internal CompletionChecklist Tasks = new CompletionChecklist { RequiredIds = new[] { "task-a" }, CompletedIds = new[] { "task-a" } };
        internal CompletionChecklist Reports = new CompletionChecklist { RequiredIds = new[] { "report-a" }, CompletedIds = new[] { "report-a" } };
        private void Check() { Reads++; if (Fail) throw new InvalidOperationException("PRIVATE_SOURCE_ERROR"); }
        private TrustedCompletionData<T> Data<T>(T value) { Check(); return new TrustedCompletionData<T> { IsTrusted = Trusted, InternshipId = Placement.InternshipId, StudentId = DataStudent, PolicyId = Placement.PolicyId, PolicyVersion = DataVersion, Value = value }; }
        public Task<TrustedCompletionData<bool?>> GetEligibilityAsync(string id) { return Task.FromResult(Data<bool?>(true)); }
        public Task<CompletionPlacement> GetAsync(string id) { Check(); return Task.FromResult(MissingPlacement ? null : Placement); }
        Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id) { return Task.FromResult(MissingHours ? null : Data(Entries)); }
        Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { return Task.FromResult(MissingRequired ? null : Data(Required)); }
        Task<TrustedCompletionData<CompletionChecklist>> IRequiredTasksReader.GetAsync(string id) { return Task.FromResult(MissingTasks ? null : Data(Tasks)); }
        Task<TrustedCompletionData<CompletionChecklist>> IRequiredReportsReader.GetAsync(string id) { return Task.FromResult(MissingReports ? null : Data(Reports)); }
    }
    private static InMemoryFinalEvaluationRepository ReadinessRepo(EvaluationPolicy policy = null, bool missingPolicy = false, bool missingCompany = false, bool missingAcademic = false, bool draft = false)
    {
        return ModernSeed(policy ?? AcademicSubmissionPolicy(), missingPolicy, missingCompany, missingAcademic, draft);
    }

    private static CoordinatorReadinessWorkflow ReadinessFlow(IFinalEvaluationRepository repo, ReadinessSources source = null, CoordinatorAccess access = null, string uid = "fictional-coordinator")
    { source = source ?? new ReadinessSources(); return new CoordinatorReadinessWorkflow(repo, new User { UserId = uid }, access ?? new CoordinatorAccess(), source, source, source, source, source); }
    private static bool Blocked(CompletionReadiness value, string text) { return !value.IsReady && value.Blockers.Any(x => x.Contains(text)); }
    private static async Task RunReadinessTests()
    {
        await Test("readiness authorized Coordinator sees actual submitted records and verified requirements", async () => {
            var result = await ReadinessFlow(ReadinessRepo()).ReviewAsync("fictional-internship");
            Assert(result.IsReady && result.Evaluations.PercentageEvaluation.Current.Percentage == 80 && PercentageEvaluationWorkflow.CurrentReview(result.Evaluations.PercentageEvaluation).Kind == AcademicReviewKind.Approve && result.ApprovedHours == 10 && result.RequiredHours == 10);
        });
        await Test("readiness rejects students non-Coordinators anonymous and out-of-scope callers before any reads", async () => {
            var repo = ReadinessRepo(); repo.SimulateFailure = true; var source = new ReadinessSources();
            await Reject<UnauthorizedAccessException>(() => ReadinessFlow(repo, source, new CoordinatorAccess { Role = false }, "fictional-student").ReviewAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => ReadinessFlow(repo, source, new CoordinatorAccess { Scope = false }).ReviewAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => ReadinessFlow(repo, source, uid: null).ReviewAsync("fictional-internship"));
            Assert(source.Reads == 0);
        });
        await Test("readiness missing authorization adapters deny access", async () => {
            await Reject<UnauthorizedAccessException>(() => new CoordinatorReadinessWorkflow(ReadinessRepo(), new User(), null, null, null, null, null, null).ReviewAsync("fictional-internship"));
        });
        await Test("readiness missing Company evaluation blocks", async () => { Assert(Blocked(await ReadinessFlow(ReadinessRepo(missingCompany: true)).ReviewAsync("fictional-internship"), "Company final evaluation")); });
        await Test("readiness missing Academic evaluation blocks", async () => { Assert(Blocked(await ReadinessFlow(ReadinessRepo(missingAcademic: true)).ReviewAsync("fictional-internship"), "Academic Review")); });
        await Test("readiness excludes Draft details", async () => { var result = await ReadinessFlow(ReadinessRepo(draft: true)).ReviewAsync("fictional-internship"); Assert(Blocked(result, "not Submitted") && result.Evaluations.Academic == null); });
        await Test("readiness sufficient approved hours may exceed the configured requirement", async () => { var s = new ReadinessSources(); s.Entries[0].Hours = 12.5m; Assert((await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship")).IsReady); });
        await Test("readiness excludes unapproved hours and reports insufficient approved hours", async () => { var s = new ReadinessSources(); s.Entries[0].Hours = 9; var r = await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship"); Assert(Blocked(r, "insufficient") && r.ApprovedHours == 9); });
        await Test("readiness missing approved-hours source blocks", async () => { Assert(Blocked(await ReadinessFlow(ReadinessRepo(), new ReadinessSources { MissingHours = true }).ReviewAsync("fictional-internship"), "approved-hours")); });
        await Test("readiness missing required-hours source never defaults to zero", async () => { foreach (var s in new[] { new ReadinessSources { MissingRequired = true }, new ReadinessSources { Required = null } }) Assert(Blocked(await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship"), "required training hours")); });
        await Test("readiness incomplete tasks give specific missing identifiers", async () => { var s = new ReadinessSources(); s.Tasks.CompletedIds = new string[0]; Assert(Blocked(await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship"), "tasks are incomplete: task-a")); });
        await Test("readiness incomplete reports give specific missing identifiers", async () => { var s = new ReadinessSources(); s.Reports.CompletedIds = new string[0]; Assert(Blocked(await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship"), "reports are incomplete: report-a")); });
        await Test("readiness missing task and report requirements block", async () => { var r = await ReadinessFlow(ReadinessRepo(), new ReadinessSources { MissingTasks = true, MissingReports = true }).ReviewAsync("fictional-internship"); Assert(Blocked(r, "tasks data is missing") && Blocked(r, "reports data is missing")); });
        await Test("readiness explicit trusted empty task and report requirements are allowed", async () => { var s = new ReadinessSources { Tasks = new CompletionChecklist { RequiredIds = new string[0], CompletedIds = new string[0] }, Reports = new CompletionChecklist { RequiredIds = new string[0], CompletedIds = new string[0] } }; Assert((await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship")).IsReady); });
        await Test("readiness missing policy blocks", async () => { Assert(Blocked(await ReadinessFlow(ReadinessRepo(missingPolicy: true)).ReviewAsync("fictional-internship"), "policy is missing")); });
        await Test("readiness unapproved policy blocks", async () => { var p = AcademicSubmissionPolicy(); p.IsApproved = false; Assert(Blocked(await ReadinessFlow(ReadinessRepo(p)).ReviewAsync("fictional-internship"), "not approved")); });
        await Test("readiness policy version and hours conflicts block", async () => { var p = AcademicSubmissionPolicy(); p.Version = "other"; Assert(Blocked(await ReadinessFlow(ReadinessRepo(p)).ReviewAsync("fictional-internship"), "version does not match")); p.Version = "test-v1"; p.RequiredHours = 20; Assert(Blocked(await ReadinessFlow(ReadinessRepo(p)).ReviewAsync("fictional-internship"), "conflict")); });
        await Test("readiness untrusted or wrong-student or wrong-policy requirement data block", async () => { foreach (var s in new[] { new ReadinessSources { Trusted = false }, new ReadinessSources { DataStudent = "another-student" }, new ReadinessSources { DataVersion = "stale" } }) { var r = await ReadinessFlow(ReadinessRepo(), s).ReviewAsync("fictional-internship"); Assert(!r.IsReady && r.ApprovedHours == null && r.RequiredHours == null && Blocked(r, "tasks data")); } });
        await Test("readiness missing placement and missing integration adapters block", async () => {
            Assert(Blocked(await ReadinessFlow(ReadinessRepo(), new ReadinessSources { MissingPlacement = true }).ReviewAsync("fictional-internship"), "Trusted placement"));
            var source = new ReadinessSources(); var flow = new CoordinatorReadinessWorkflow(ReadinessRepo(), new User(), new CoordinatorAccess(), source, null, null, null, null);
            var r = await flow.ReviewAsync("fictional-internship"); Assert(!r.IsReady && r.Blockers.Length == 4);
        });
        await Test("readiness source and repository errors are safe blockers", async () => { var repo = ReadinessRepo(); repo.SimulateFailure = true; Assert(! (await ReadinessFlow(repo).ReviewAsync("fictional-internship")).IsReady); var r = await ReadinessFlow(ReadinessRepo(), new ReadinessSources { Fail = true }).ReviewAsync("fictional-internship"); Assert(!r.IsReady && !string.Join("", r.Blockers).Contains("PRIVATE_SOURCE_ERROR")); });
        await Test("readiness invalid negative duplicate and overflow hour ledgers block", async () => {
            foreach (var entries in new[] { new[] { new TrainingHourEntry { EntryId = "a", Hours = -1 } }, new[] { new TrainingHourEntry { EntryId = "a" }, new TrainingHourEntry { EntryId = "a" } }, new[] { new TrainingHourEntry { EntryId = "a", Hours = decimal.MaxValue, IsApproved = true }, new TrainingHourEntry { EntryId = "b", Hours = decimal.MaxValue, IsApproved = true } } }) Assert(! (await ReadinessFlow(ReadinessRepo(), new ReadinessSources { Entries = entries }).ReviewAsync("fictional-internship")).IsReady);
        });
        await Test("readiness never creates or changes completion evaluations or documents", async () => {
            var repo = ReadinessRepo(); var beforeC = await repo.GetCompanyAsync("fictional-internship"); var beforeA = await repo.GetAcademicAsync("fictional-internship");
            for (int i = 0; i < 2; i++) { var r = await ReadinessFlow(repo).ReviewAsync("fictional-internship"); r.Evaluations.PercentageEvaluation.Current.Comments = "local change"; }
            Assert((await repo.GetCompanyAsync("fictional-internship")).Version == beforeC.Version && (await repo.GetAcademicAsync("fictional-internship")).Version == beforeA.Version);
            Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null && await repo.GetCompletionDocumentAsync("fictional-internship", "fictional-doc") == null);
            var review = new CompletionReview { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1", Status = CompletionStatus.UnderReview, Feedback = "Existing review is unchanged" };
            var document = new CompletionDocument { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1", DocumentId = "fictional-doc", Status = DocumentStatus.Pending };
            var seeded = new InMemoryFinalEvaluationRepository(new[] { AcademicSubmissionPolicy() }, new[] { SubmittedCompanyFixture() }, new[] { CompleteAcademicDraft() }, new[] { review }, new[] { document });
            await ReadinessFlow(seeded).ReviewAsync("fictional-internship");
            Assert(JsonConvert.SerializeObject(await seeded.GetCompletionReviewAsync("fictional-internship")) == JsonConvert.SerializeObject(review));
            Assert(JsonConvert.SerializeObject(await seeded.GetCompletionDocumentAsync("fictional-internship", "fictional-doc")) == JsonConvert.SerializeObject(document));
        });
        await Test("readiness Firebase repository uses GET only through mocked HTTP with no network", async () => {
            int reads = 0; var academic = CompleteAcademicDraft(); academic.Status = EvaluationStatus.Submitted; academic.SubmittedAtUtc = DateTimeOffset.UtcNow;
            var handler = new Handler { Respond = request => { Assert(request.Method == HttpMethod.Get); reads++; object value = request.RequestUri.AbsolutePath.StartsWith("/evaluationPolicies/") ? (object)AcademicSubmissionPolicy() : request.RequestUri.AbsolutePath.StartsWith("/companyFinalEvaluations/") ? SubmittedCompanyFixture() : (object)academic; return Response(JsonConvert.SerializeObject(value)); } };
            Assert(!(await ReadinessFlow(Rest(handler)).ReviewAsync("fictional-internship")).IsReady && reads >= 3);
        });
        await Test("readiness rejects evaluation placement identity and missing submission timestamp", async () => {
            var source = new ReadinessSources(); source.Placement.StudentId = "wrong-student";
            var result = await ReadinessFlow(ReadinessRepo(), source).ReviewAsync("fictional-internship"); Assert(Blocked(result, "placement"));
            var company = SubmittedCompanyFixture(); company.SubmittedAtUtc = null;
            var repo = new InMemoryFinalEvaluationRepository(new[] { AcademicSubmissionPolicy() }, new[] { company });
            Assert(Blocked(await ReadinessFlow(repo).ReviewAsync("fictional-internship"), "Legacy"));
        });
        await Test("readiness page placement enumeration denies students and filters unauthorized scope", async () => {
            var source = new ReadinessSources(); var repo = ReadinessRepo();
            var ctx = new CoordinatorReviewPageContext(ReadinessFlow(repo), new User { UserId = "fictional-student" }, new CoordinatorAccess { Role = false }, new[] { source.Placement });
            await Reject<UnauthorizedAccessException>(() => ctx.GetAuthorizedPlacementsAsync());
            ctx = new CoordinatorReviewPageContext(ReadinessFlow(repo), new User(), new CoordinatorAccess { Scope = false }, new[] { source.Placement });
            Assert((await ctx.GetAuthorizedPlacementsAsync()).Length == 0);
        });
    }
}
