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
        public static Task<StudentCompletionPageContext> ResolveAsync(HttpContext context)
        {
            // Application_Start normally registers the Firebase composition; recover if this
            // request arrives before the app-start hook in a recycled or test-hosted app-domain.
            if (Factory == null) FinalEvaluationFirebaseIntegration.Register();
            // No runtime demo users or in-memory business records.
            return Factory == null ? Task.FromResult<StudentCompletionPageContext>(null) : Factory(context);
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
