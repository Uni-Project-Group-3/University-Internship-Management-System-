using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class StudentCompletionPageContext
    {
        public StudentCompletionWorkflow Workflow { get; private set; }
        public string DefaultInternshipId { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsDevelopment { get; internal set; }
        public string DevelopmentDatasetPath { get; internal set; }
        public int DemoStudentIndex { get; internal set; }
        public StudentCompletionPageContext(StudentCompletionWorkflow workflow, string defaultInternshipId, string displayName)
        { Workflow = workflow ?? throw new ArgumentNullException("workflow"); DefaultInternshipId = defaultInternshipId; DisplayName = displayName; }
    }
    public static class StudentCompletionPageIntegration
    {
        // Trusted request-level server composition. Never registered from request/browser fields.
        public static Func<HttpContext, Task<StudentCompletionPageContext>> Factory { get; set; }
        public static async Task<StudentCompletionPageContext> ResolveAsync(HttpContext context)
        {
            if (Factory != null) return await Factory(context).ConfigureAwait(false);
            var hub = PercentageDevelopmentHub.Resolve(context);
            if (hub != null)
            {
                int choice;
                // Explicit demo-session capability: choose one of this owned dataset's three
                // fictional Students, never a browser user ID/role or a production identity.
                if (int.TryParse(context.Request.Form["DemoStudentChoice"], out choice) && choice >= 0 && choice < 3) hub.SelectedStudent = choice;
                string id = hub.Source.Placements[hub.SelectedStudent].InternshipId;
                var review = await hub.Repository.GetCompletionReviewAsync(id);
                if (!hub.IsPersistent && review != null && review.Status == CompletionStatus.Approved && !hub.Documents.ContainsKey(id))
                {
                    // LOCAL fixture publication only, through the existing authorized private service.
                    await hub.GenerateReportAsync(id);
                }
                var user = new PercentageDevelopmentHub.User("local-student-" + (char)('A' + hub.SelectedStudent));
                var localReports = new CompletionReportService(hub.Repository, user, hub.Source, hub.Source, new CompletionReportPdfRenderer(), hub.Storage);
                var localWorkflow = new StudentCompletionWorkflow(hub.Repository, user, hub.Source, hub.Source, hub.Source, hub.Source, hub.Source, hub.Source, hub.Source, localReports);
                return new StudentCompletionPageContext(localWorkflow, id, "Fictional local student") { IsDevelopment = true, DevelopmentDatasetPath = hub.DatasetPath, DemoStudentIndex = hub.SelectedStudent };
            }
            if (!context.IsDebuggingEnabled || !context.Request.IsLocal || Environment.GetEnvironmentVariable("UIMS_STUDENT_COMPLETION_IN_MEMORY") != "1") return null;
            const string key = "FinalEvaluation.StudentCompletion.DevelopmentContext";
            var existing = context.Session[key] as StudentCompletionPageContext;
            if (existing != null) return existing;
            var source = new DevelopmentSources();
            var policy = new EvaluationPolicy { PolicyId = "dev-student-policy", Version = "test-v1", IsApproved = true, CompletionReport = new CompletionReportPolicy { Enabled = true } };
            var reviews = source.Ids.Select(id => new CompletionReview { InternshipId = id, StudentId = "dev-student-record", PolicyId = policy.PolicyId, PolicyVersion = policy.Version,
                Status = id == "dev-student-pending" ? CompletionStatus.Pending : CompletionStatus.Approved, ReviewerId = "fictional-coordinator", CreatedAtUtc = source.ApprovedAt, UpdatedAtUtc = source.ApprovedAt,
                ReviewedAtUtc = id == "dev-student-pending" ? null : (DateTimeOffset?)source.ApprovedAt, Feedback = "CONFIDENTIAL_COORDINATOR_NOTE_MUST_NOT_APPEAR" }).ToArray();
            var repository = new InMemoryFinalEvaluationRepository(new[] { policy }, reviews: reviews);
            var storage = new InMemoryPrivateCompletionReportStorage();
            var generator = new CompletionReportService(repository, new DevelopmentUser("dev-report-generator"), source, source, new CompletionReportPdfRenderer(), storage);
            foreach (string id in new[] { "dev-student-ready", "dev-student-corrupt", "dev-student-missing-object" })
            { var doc = await generator.GenerateAsync(id).ConfigureAwait(false); source.Documents[id] = doc.DocumentId; }
            var student = new DevelopmentUser("dev-student-owner");
            var reports = new CompletionReportService(repository, student, source, source, new CompletionReportPdfRenderer(), new DevelopmentStorage(storage));
            var workflow = new StudentCompletionWorkflow(repository, student, source, source, source, source, source, source, source, reports);
            var result = new StudentCompletionPageContext(workflow, "dev-student-ready", "Fictional student") { IsDevelopment = true };
            context.Session[key] = result; return result;
        }
        private sealed class DevelopmentUser : IAuthenticatedUser
        { public string UserId { get; private set; } internal DevelopmentUser(string uid) { UserId = uid; } }
        private sealed class DevelopmentStorage : IPrivateCompletionReportStorage
        {
            private readonly IPrivateCompletionReportStorage inner;
            internal DevelopmentStorage(IPrivateCompletionReportStorage inner) { this.inner = inner; }
            public Task PutNewAsync(string key, byte[] bytes) { return inner.PutNewAsync(key, bytes); }
            public Task DeleteIfHashMatchesAsync(string key, string hash) { return inner.DeleteIfHashMatchesAsync(key, hash); }
            public async Task<byte[]> ReadAsync(string key)
            {
                if (key.Contains("dev-student-missing-object/")) throw new IOException("Simulated private storage outage.");
                var bytes = await inner.ReadAsync(key).ConfigureAwait(false);
                if (key.Contains("dev-student-corrupt/")) bytes[bytes.Length - 8] ^= 1;
                return bytes;
            }
        }
        private sealed class DevelopmentSources : ICompletionReportAccess, ICompletionReportDataReader, IStudentCompletionPlacementReader,
            IApprovedTrainingHoursReader, IRequiredTrainingHoursReader, IStudentVisibleTaskReader, IStudentOverallProgressReader, IStudentCompletionReportLocator
        {
            internal readonly string[] Ids = { "dev-student-ready", "dev-student-no-report", "dev-student-pending", "dev-student-corrupt", "dev-student-missing-object", "dev-student-untrusted" };
            internal readonly DateTimeOffset ApprovedAt = DateTimeOffset.Parse("2026-10-21T10:00:00Z");
            internal readonly Dictionary<string, string> Documents = new Dictionary<string, string>();
            public Task<bool> CanGenerateAsync(string uid, string id) { return Task.FromResult(uid == "dev-report-generator" && Ids.Contains(id)); }
            public Task<bool> IsStudentOwnerAsync(string uid, string id, string student) { return Task.FromResult(uid == "dev-student-owner" && Ids.Contains(id) && (student == null || student == "dev-student-record")); }
            public Task<CompletionPlacement> GetAsync(string id) { return Task.FromResult(Ids.Contains(id) ? new CompletionPlacement { InternshipId = id, StudentId = "dev-student-record", PolicyId = "dev-student-policy", PolicyVersion = "test-v1" } : null); }
            public Task<VerifiedCompletionReportData> GetAsync(string id, string student, string policy, string version)
            { return Task.FromResult(new VerifiedCompletionReportData { IsTrusted = true, InternshipId = id, StudentId = student, PolicyId = policy, PolicyVersion = version, ApprovedAtUtc = ApprovedAt,
                StudentName = "Fictional student", StudentNumber = "TEST-ONLY", OrganizationName = "Fictional training organization", PeriodStart = new DateTime(2026, 1, 1), PeriodEnd = new DateTime(2026, 2, 1), ApprovedHours = 10 }); }
            private TrustedCompletionData<T> Data<T>(string id, T value) { return new TrustedCompletionData<T> { IsTrusted = id != "dev-student-untrusted", InternshipId = id, StudentId = "dev-student-record", PolicyId = "dev-student-policy", PolicyVersion = "test-v1", Value = value }; }
            Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id)
            { return Task.FromResult(Data(id, new[] { new TrainingHourEntry { EntryId = "approved", Hours = id == "dev-student-pending" ? 6 : 10, IsApproved = true }, new TrainingHourEntry { EntryId = "not-approved", Hours = 50, IsApproved = false } })); }
            Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { return Task.FromResult(Data<decimal?>(id, id == "dev-student-pending" ? 12 : 10)); }
            Task<TrustedCompletionData<decimal?>> IStudentOverallProgressReader.GetAsync(string id) { return Task.FromResult(Data<decimal?>(id, id == "dev-student-pending" ? 30 : 100)); }
            Task<TrustedCompletionData<StudentTaskRecord[]>> IStudentVisibleTaskReader.GetAsync(string id)
            { return Task.FromResult(Data(id, new[] {
                new StudentTaskRecord { Id = "TEST-TASK-1", Title = "Fictional published task", Status = StudentTaskStatus.Evaluated, IsVisibleToStudent = true, IsResultPublished = true, Grade = 4, MaximumGrade = 7, StudentVisibleFeedback = "Published task feedback <plain text>" },
                new StudentTaskRecord { Id = "TEST-TASK-2", Title = "Fictional unpublished task", Status = StudentTaskStatus.Submitted, IsVisibleToStudent = true, Grade = 999, StudentVisibleFeedback = "PRIVATE_UNPUBLISHED_TASK_FEEDBACK" },
                new StudentTaskRecord { Id = "PRIVATE-TASK", Title = "PRIVATE_HIDDEN_TASK", Status = StudentTaskStatus.Assigned, StudentVisibleFeedback = "PRIVATE_HIDDEN_TASK_FEEDBACK" } })); }
            public Task<string> GetIssuedDocumentIdAsync(string id, string student) { string value; return Task.FromResult(Documents.TryGetValue(id, out value) ? value : null); }
        }
    }
}
