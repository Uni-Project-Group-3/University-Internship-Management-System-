using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class StudentSources : IStudentCompletionPlacementReader, IApprovedTrainingHoursReader, IRequiredTrainingHoursReader,
        IStudentVisibleTaskReader, IStudentOverallProgressReader, IStudentCompletionReportLocator, IStudentPendingTrainingHoursReader
    {
        internal int Reads, LocatorReads;
        internal bool Trusted = true, MissingPlacement, FailTasks, FailPending;
        internal string DataStudent = "fictional-student", DocumentId;
        internal decimal Approved = 10;
        internal decimal? Required = 20, Overall = 35, Pending;
        internal StudentTaskRecord[] Tasks = new[] {
            new StudentTaskRecord { Id = "visible-task", Title = "Student visible task", IsVisibleToStudent = true, Status = StudentTaskStatus.Evaluated, IsResultPublished = true, Grade = 4, MaximumGrade = 7, StudentVisibleFeedback = "Published task feedback" },
            new StudentTaskRecord { Id = "unpublished-task", Title = "Unpublished result", IsVisibleToStudent = true, Status = StudentTaskStatus.Submitted, Grade = 999, StudentVisibleFeedback = "PRIVATE_UNPUBLISHED_FEEDBACK" },
            new StudentTaskRecord { Id = "hidden-task", Title = "PRIVATE_HIDDEN_TASK", Status = StudentTaskStatus.Assigned, StudentVisibleFeedback = "PRIVATE_HIDDEN_FEEDBACK" } };
        private TrustedCompletionData<T> Data<T>(T value) { Reads++; return new TrustedCompletionData<T> { IsTrusted = Trusted, InternshipId = "fictional-internship", StudentId = DataStudent, PolicyId = "fictional-policy", PolicyVersion = "test-v1", Value = value }; }
        public Task<CompletionPlacement> GetAsync(string id) { Reads++; return Task.FromResult(MissingPlacement ? null : new CompletionPlacement { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1" }); }
        Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id) { return Task.FromResult(Data(new[] { new TrainingHourEntry { EntryId = "approved", Hours = Approved, IsApproved = true }, new TrainingHourEntry { EntryId = "pending", Hours = 100, IsApproved = false } })); }
        Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { return Task.FromResult(Data(Required)); }
        Task<TrustedCompletionData<StudentTaskRecord[]>> IStudentVisibleTaskReader.GetAsync(string id) { if (FailTasks) throw new InvalidOperationException("PRIVATE_SOURCE_ERROR"); return Task.FromResult(Data(Tasks)); }
        Task<TrustedCompletionData<decimal?>> IStudentOverallProgressReader.GetAsync(string id) { return Task.FromResult(Data(Overall)); }
        public Task<TrustedCompletionData<decimal?>> GetPendingHoursAsync(string id) { if (FailPending) throw new InvalidOperationException("PRIVATE_PENDING_ERROR"); return Task.FromResult(Data(Pending)); }
        public Task<string> GetIssuedDocumentIdAsync(string id, string student) { LocatorReads++; return Task.FromResult(DocumentId); }
    }
    private static StudentCompletionWorkflow StudentFlow(ReportFixture f, StudentSources source, string uid = "fictional-student")
    { return new StudentCompletionWorkflow(f.Repo, new User { UserId = uid }, f.Access, source, source, source, source, source, source, f.Service(uid)); }
    private static async Task RunStudentCompletionTests()
    {
        await Test("Student grade card distinguishes unavailable source from a verified empty source", async () => {
            var f = await ReportSetup();
            var empty = await StudentFlow(f, new StudentSources { Tasks = new StudentTaskRecord[0] }).LoadAsync("fictional-internship");
            var unavailable = await StudentFlow(f, new StudentSources { FailTasks = true }).LoadAsync("fictional-internship");
            Assert(empty.PublishedTaskGradesText == "No published task grades available");
            Assert(unavailable.PublishedTaskGradesText == "Published task grades unavailable");
            Assert(unavailable.Status == CompletionStatus.Approved);
        });
        await Test("Student published grade count remains separate from unpublished results", async () => {
            var f = await ReportSetup(); var source = new StudentSources();
            var view = await StudentFlow(f, source).LoadAsync("fictional-internship");
            Assert(view.PublishedTaskGradesText == "1" && !JsonConvert.SerializeObject(view.Tasks).Contains("999"));
        });
        await Test("Student pending hours require explicit trusted pending data rather than unapproved ledger entries", async () => {
            var f = await ReportSetup();
            var view = await StudentFlow(f, new StudentSources { Pending = 3 }).LoadAsync("fictional-internship");
            Assert(view.PendingHours == 3 && view.ApprovedHours == 10 && view.RemainingHours == 10 && view.HoursPercent == 50);
        });
        await Test("Student unknown pending hours are not replaced with the unapproved total", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources()).LoadAsync("fictional-internship");
            Assert(view.PendingHours == null && view.ApprovedHours == 10 && view.Notices.Any(n => n.Contains("pending-status")));
        });
        await Test("Student negative wrong-owner and failed pending sources remain unavailable", async () => {
            var f = await ReportSetup();
            foreach (var source in new[] { new StudentSources { Pending = -1 }, new StudentSources { Pending = 3, DataStudent = "another-student" }, new StudentSources { Pending = 3, FailPending = true } }) {
                var view = await StudentFlow(f, source).LoadAsync("fictional-internship");
                Assert(view.PendingHours == null && !JsonConvert.SerializeObject(view).Contains("PRIVATE_PENDING_ERROR"));
            }
        });
        await Test("Student remaining hours clamp to zero and unknown inputs stay unknown", async () => {
            var f = await ReportSetup();
            var exceeded = await StudentFlow(f, new StudentSources { Approved = 30, Pending = 0 }).LoadAsync("fictional-internship");
            var unknown = await StudentFlow(f, new StudentSources { Trusted = false }).LoadAsync("fictional-internship");
            Assert(exceeded.RemainingHours == 0 && exceeded.HoursPercent == 100 && exceeded.PendingHours == 0);
            Assert(unknown.RemainingHours == null && unknown.PendingHours == null);
        });

        await Test("task grade count excludes unpublished hidden and feedback-only records", async () => {
            var f = await ReportSetup(); var source = new StudentSources();
            source.Tasks = source.Tasks.Concat(new[] {
                new StudentTaskRecord { Id = "hidden-published", Status = StudentTaskStatus.Evaluated, IsResultPublished = true, Grade = 90 },
                new StudentTaskRecord { Id = "feedback-only", Status = StudentTaskStatus.Evaluated, IsVisibleToStudent = true, IsResultPublished = true, StudentVisibleFeedback = "Authorized feedback only" }
            }).ToArray();
            var view = await StudentFlow(f, source).LoadAsync("fictional-internship"); Assert(view.TasksVerified && view.PublishedTaskGradeCount == 1);
        });
        await Test("empty trusted tasks have no published task grades", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { Tasks = new StudentTaskRecord[0] }).LoadAsync("fictional-internship");
            Assert(view.TasksVerified && view.PublishedTaskGradeCount == 0 && view.Tasks.Length == 0);
        });
        await Test("untrusted task data never supplies a published grade count", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { Trusted = false }).LoadAsync("fictional-internship");
            Assert(!view.TasksVerified && view.PublishedTaskGradeCount == 0 && view.Tasks.Length == 0);
        });
        await Test("student reads own approved status without requiring an existing PDF", async () => {
            var f = await ReportSetup(); var source = new StudentSources(); var view = await StudentFlow(f, source).LoadAsync("fictional-internship");
            Assert(view.Status == CompletionStatus.Approved && view.ApprovedAtUtc.HasValue && !view.ReportAvailable && view.ReportMessage.Contains("not yet"));
        });
        await Test("student own approved report availability uses existing private PDF integrity validation", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); var source = new StudentSources { DocumentId = doc.DocumentId };
            var view = await StudentFlow(f, source).LoadAsync("fictional-internship"); Assert(view.ReportAvailable && view.Status == CompletionStatus.Approved);
        });
        await Test("student other owner anonymous and non-student denied before privileged reads", async () => {
            var f = await ReportSetup(); f.Repo.SimulateFailure = true; var source = new StudentSources();
            foreach (string uid in new[] { "unrelated-student", "fictional-coordinator", null }) await Reject<UnauthorizedAccessException>(() => StudentFlow(f, source, uid).LoadAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => StudentFlow(f, source).LoadAsync("another-internship")); Assert(source.Reads == 0 && source.LocatorReads == 0);
        });
        await Test("student missing completion record remains unknown rather than invented Pending", async () => {
            var f = await ReportSetup(); f.Repo = new InMemoryFinalEvaluationRepository(); var view = await StudentFlow(f, new StudentSources()).LoadAsync("fictional-internship");
            Assert(view.Status == null && !view.ReportAvailable);
        });
        await Test("student Pending and Changes Requested do not expose a download", async () => {
            foreach (var status in new[] { CompletionStatus.Pending, CompletionStatus.ChangesRequested, CompletionStatus.Rejected }) {
                var f = await ReportSetup(status); var source = new StudentSources { DocumentId = Guid.NewGuid().ToString("N") }; var view = await StudentFlow(f, source).LoadAsync("fictional-internship");
                Assert(view.Status == status && !view.ReportAvailable && view.ApprovedAtUtc == null && source.LocatorReads == 0);
            }
        });
        await Test("student hours exclude unapproved entries while overall progress is separately trusted", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources()).LoadAsync("fictional-internship");
            Assert(view.ApprovedHours == 10 && view.RequiredHours == 20 && view.HoursPercent == 50 && view.OverallPercent == 35);
        });
        await Test("student missing hours or progress never fabricate zeros or completion percentages", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { Trusted = false }).LoadAsync("fictional-internship");
            Assert(view.Status == CompletionStatus.Approved && view.ApprovedHours == null && view.RequiredHours == null && view.OverallPercent == null && view.Tasks.Length == 0);
        });
        await Test("student mismatched requirement bindings cannot expose another student data", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { DataStudent = "another-student" }).LoadAsync("fictional-internship");
            Assert(view.ApprovedHours == null && view.Tasks.Length == 0 && view.Status == CompletionStatus.Approved);
        });
        await Test("student zero required hours and missing overall do not invent a percentage", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { Required = 0, Overall = null }).LoadAsync("fictional-internship");
            Assert(view.HoursPercent == null && view.OverallPercent == null && view.RequiredHours == 0);
        });
        await Test("student large approved hours safely cap hours progress without altering completion", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { Approved = decimal.MaxValue, Required = 0.01m }).LoadAsync("fictional-internship"); Assert(view.HoursPercent == 100 && view.Status == CompletionStatus.Approved);
        });
        await Test("student task projection contains only visible published grades and feedback", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources()).LoadAsync("fictional-internship"); var json = JsonConvert.SerializeObject(view);
            Assert(view.Tasks.Length == 2 && json.Contains("4 / 7") && json.Contains("Published task feedback") && !json.Contains("PRIVATE_") && !json.Contains("999"));
        });
        await Test("student task grades honor supplied values without fixed university score limits", async () => {
            var f = await ReportSetup(); var source = new StudentSources(); source.Tasks[0].Grade = -2.5m; source.Tasks[0].MaximumGrade = 70.5m;
            Assert((await StudentFlow(f, source).LoadAsync("fictional-internship")).Tasks[0].NextStep.Contains("-2.5 / 70.5"));
        });
        await Test("student source failures hide sensitive errors while preserving authoritative Approved status", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources { FailTasks = true }).LoadAsync("fictional-internship");
            Assert(view.Status == CompletionStatus.Approved && view.Tasks.Length == 0 && !JsonConvert.SerializeObject(view).Contains("PRIVATE_SOURCE_ERROR"));
        });
        await Test("student projection has no raw reviews private audit history IDs or storage references", async () => {
            var f = await ReportSetup(); var view = await StudentFlow(f, new StudentSources()).LoadAsync("fictional-internship"); var json = JsonConvert.SerializeObject(view);
            Assert(!json.Contains("PRIVATE_COORDINATOR_REASON_MUST_NOT_APPEAR") && !json.Contains("DecisionHistory") && !json.Contains("StorageObjectPath") && !json.Contains("DocumentId") && !json.Contains("StudentId"));
        });
        await Test("student owner download returns valid PDF with matching private metadata", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); var source = new StudentSources { DocumentId = doc.DocumentId };
            var download = await StudentFlow(f, source).DownloadAsync("fictional-internship"); Assert(download.ContentType == "application/pdf" && CompletionReportValidation.Hash(download.Content) == doc.Sha256);
        });
        await Test("student unauthorized download denied even with a known valid document ID", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); var source = new StudentSources { DocumentId = doc.DocumentId }; int reads = f.Storage.Reads;
            await Reject<UnauthorizedAccessException>(() => StudentFlow(f, source, "unrelated-student").DownloadAsync("fictional-internship")); Assert(source.LocatorReads == 0 && f.Storage.Reads == reads);
        });
        await Test("student missing PDF gives not found while preserving Approved status", async () => {
            var f = await ReportSetup(); var flow = StudentFlow(f, new StudentSources());
            try { await flow.DownloadAsync("fictional-internship"); Assert(false); } catch (StudentReportUnavailableException error) { Assert(error.HttpStatus == 404); }
            Assert((await flow.LoadAsync("fictional-internship")).Status == CompletionStatus.Approved);
        });
        await Test("student corrupt PDF disables availability without reverting Approved", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); var source = new StudentSources { DocumentId = doc.DocumentId }; f.Storage.CorruptRead = true;
            var flow = StudentFlow(f, source); var view = await flow.LoadAsync("fictional-internship"); Assert(!view.ReportAvailable && view.Status == CompletionStatus.Approved);
            try { await flow.DownloadAsync("fictional-internship"); Assert(false); } catch (StudentReportUnavailableException error) { Assert(error.HttpStatus == 503); }
        });
        await Test("student missing private object returns safe unavailable response", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); f.Storage.FailRead = true;
            try { await StudentFlow(f, new StudentSources { DocumentId = doc.DocumentId }).DownloadAsync("fictional-internship"); Assert(false); } catch (StudentReportUnavailableException error) { Assert(error.HttpStatus == 503 && !error.Message.Contains("completion-reports/")); }
        });
        await Test("student unapproved download returns conflict before report lookup", async () => {
            var f = await ReportSetup(CompletionStatus.Pending); var source = new StudentSources();
            try { await StudentFlow(f, source).DownloadAsync("fictional-internship"); Assert(false); } catch (StudentReportUnavailableException error) { Assert(error.HttpStatus == 409 && source.LocatorReads == 0); }
        });
        await Test("student missing trusted placement and authorization adapters fail closed", async () => {
            var f = await ReportSetup(); await Reject<InvalidOperationException>(() => StudentFlow(f, new StudentSources { MissingPlacement = true }).LoadAsync("fictional-internship"));
            var flow = new StudentCompletionWorkflow(f.Repo, new User(), null, null, null, null, null, null, null, null); await Reject<UnauthorizedAccessException>(() => flow.LoadAsync("fictional-internship"));
        });
        await Test("student report reads and downloads never mutate approval or document metadata", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); var before = JsonConvert.SerializeObject(await f.Repo.GetCompletionReviewSnapshotAsync("fictional-internship"));
            var flow = StudentFlow(f, new StudentSources { DocumentId = doc.DocumentId }); await flow.LoadAsync("fictional-internship"); await flow.DownloadAsync("fictional-internship");
            Assert(before == JsonConvert.SerializeObject(await f.Repo.GetCompletionReviewSnapshotAsync("fictional-internship")) && JsonConvert.SerializeObject(doc) == JsonConvert.SerializeObject(await f.Repo.GetCompletionDocumentAsync("fictional-internship", doc.DocumentId)));
        });
    }
}
