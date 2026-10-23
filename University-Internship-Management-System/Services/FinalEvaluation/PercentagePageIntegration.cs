using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Net.Http;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class PercentagePageContext
    {
        public PercentageEvaluationWorkflow Workflow { get; set; }
        public CompletionPlacement[] Placements { get; set; }
        public string UserId { get; set; }
        public bool IsDevelopment { get; set; }
        public string DatasetPath { get; set; }
        public string WriteBlocker { get; set; }
        // Only trusted server adapters may provide report/detail text. No HTML is accepted.
        public Func<string, Task<string>> ReadReportsAsync { get; set; }
        public Func<string, Task<string>> ReadDetailsAsync { get; set; }
    }
    public static class PercentagePageIntegration
    {
        public static Func<HttpContext, bool, Task<PercentagePageContext>> Factory { get; set; }
        public static Task<PercentagePageContext> Resolve(HttpContext context, bool academic)
        {
            // Application_Start registers the verified Firebase composition. Tests may inject a trusted factory.
            return Factory == null ? Task.FromResult<PercentagePageContext>(null) : Factory(context, academic);
        }
    }
    // One session-owned fixture shared by all four module pages. Never a production fallback.
    internal sealed class PercentageDevelopmentHub
    {
        internal readonly IFinalEvaluationRepository Repository;
        internal readonly Sources Source = new Sources();
        internal readonly ICompletionDecisionGuard Guard;
        internal readonly InMemoryPrivateCompletionReportStorage Storage = new InMemoryPrivateCompletionReportStorage();
        internal readonly Dictionary<string, string> Documents = new Dictionary<string, string>();
        internal bool IsPersistent { get { return Repository is FirebaseDevelopmentDemoRepository; } }
        internal string DatasetPath { get { return (Repository as FirebaseDevelopmentDemoRepository)?.DatasetPath; } }
        internal int SelectedStudent = 0;
        internal async Task<CompletionDocument> GenerateReportAsync(string id)
        {
            if (!await Source.CanGenerateAsync("local-coordinator", id)) throw new UnauthorizedAccessException();
            var review = await Repository.GetCompletionReviewAsync(id);
            if (review == null || review.Status != CompletionStatus.Approved) throw new InvalidOperationException("Coordinator Approved completion is required.");
            var existing = await Source.GetIssuedDocumentIdAsync(id, review.StudentId);
            if (existing != null)
            {
                var metadata = await Repository.GetCompletionDocumentAsync(id, existing);
                if (metadata != null && metadata.StudentId == review.StudentId && metadata.PolicyId == review.PolicyId && metadata.PolicyVersion == review.PolicyVersion && metadata.CompletionApprovedAtUtc == review.ReviewedAtUtc)
                    try { CompletionReportValidation.Metadata(metadata); var bytes = await Storage.ReadAsync(metadata.StorageObjectPath); if (CompletionReportValidation.Hash(bytes) == metadata.Sha256 && bytes.LongLength == metadata.ByteLength) return metadata; } catch (System.IO.IOException) { } catch (RepositoryException) { /* Session-private PDF bytes may expire; explicitly regenerate. */ }
            }
            var service = new CompletionReportService(Repository, new User("local-coordinator"), Source, Source, new CompletionReportPdfRenderer(), Storage);
            var document = await service.GenerateAsync(id); Documents[id] = document.DocumentId; return document;
        }
        internal static PercentageDevelopmentHub Resolve(HttpContext context)
        {
            bool persistent = Environment.GetEnvironmentVariable("UIMS_FINAL_EVALUATION_FIREBASE_DEMO") == "1";
            if (System.Configuration.ConfigurationManager.AppSettings["FinalEvaluationLocalDevelopmentEnabled"] != "true" ||
                !context.IsDebuggingEnabled || !context.Request.IsLocal) return null;
            string key = "FinalEvaluation.Percentage.v2.LocalHub." + persistent;
            var result = context.Session[key] as PercentageDevelopmentHub;
            if (result == null)
            {
                if (persistent)
                {
                    FirebaseDevelopmentDemoRepository.VerifyExternalDevelopmentCredentials();
                    string id = null; var cookie = context.Request.Cookies["Uims.FinalEvaluation.DemoDataset"];
                    try { if (cookie != null) id = System.Text.Encoding.UTF8.GetString(MachineKey.Unprotect(HttpServerUtility.UrlTokenDecode(cookie.Value), "FinalEvaluation.FirebaseDemo.v1", context.Session.SessionID)); } catch { }
                    Guid parsed; if (!Guid.TryParseExact(id, "N", out parsed) || parsed == Guid.Empty) id = Guid.NewGuid().ToString("N");
                    context.Items["FinalEvaluation.DemoDatasetPath"] = FirebaseDevelopmentDemoRepository.Namespace + "/sessions/" + id;
                    var repository = new FirebaseDevelopmentDemoRepository(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) }, new GoogleAdcAccessTokenProvider(), new FirebaseFinalEvaluationConfiguration(), id);
                    repository.SeedAsync().GetAwaiter().GetResult();
                    result = new PercentageDevelopmentHub(repository);
                    context.Response.Cookies.Add(new HttpCookie("Uims.FinalEvaluation.DemoDataset", HttpServerUtility.UrlTokenEncode(MachineKey.Protect(System.Text.Encoding.UTF8.GetBytes(id), "FinalEvaluation.FirebaseDemo.v1", context.Session.SessionID))) { HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Strict, Path = "/" });
                }
                else result = new PercentageDevelopmentHub();
                context.Session[key] = result;
            }
            return result;
        }
        internal PercentageDevelopmentHub(IFinalEvaluationRepository persistentRepository = null)
        {
            // Explicit fictional testing policy, never a representation of university rules.
            Repository = persistentRepository ?? new InMemoryFinalEvaluationRepository(new[] { new EvaluationPolicy { PolicyId = "local-fictional-policy", Version = "test-v2", IsApproved = true,
                PassingThreshold = 70, RequiredHours = 10, StudentFinalOutcomeDisclosureAllowed = true,
                CompletionDecisions = new CompletionDecisionPolicy { Approve = new CompletionDecisionRule { Enabled = true }, RequestChanges = new CompletionDecisionRule { Enabled = true }, Reject = new CompletionDecisionRule { Enabled = false } },
                CompletionReport = new CompletionReportPolicy { Enabled = true } } });
            Guard = persistentRepository as ICompletionDecisionGuard ?? new LocalGuard();
            var company = new PercentageEvaluationWorkflow(Repository, new User("local-company"), Source, Source, Guard);
            if (persistentRepository == null) company.SaveDraftAsync("dev-company-internship", "null_etag", null, "").GetAwaiter().GetResult();
            Source.Hub = this;
        }
        internal sealed class User : IAuthenticatedUser { public string UserId { get; private set; } internal User(string id) { UserId = id; } }
        internal sealed class LocalGuard : ICompletionDecisionGuard
        {
            private readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
            public async Task<ICompletionDecisionLease> AcquireAsync(string id) { await mutex.WaitAsync(); return new Lease(mutex); }
            private sealed class Lease : ICompletionDecisionLease
            {
                private readonly SemaphoreSlim mutex; internal Lease(SemaphoreSlim mutex) { this.mutex = mutex; }
                public Task<bool> IsCurrentAsync(CompletionReadiness value) { return Task.FromResult(true); }
                public void Dispose() { mutex.Release(); }
            }
        }
        internal sealed class Sources : IPercentageEvaluationAccess, ICompletionPlacementReader, ICompletionEligibilityReader,
            IApprovedTrainingHoursReader, IRequiredTrainingHoursReader, IRequiredTasksReader, IRequiredReportsReader,
            ICoordinatorEvaluationAccess, ICompletionReportAccess, ICompletionReportDataReader, IStudentCompletionPlacementReader,
            IStudentVisibleTaskReader, IStudentOverallProgressReader, IStudentCompletionReportLocator
        {
            internal PercentageDevelopmentHub Hub;
            internal readonly CompletionPlacement[] Placements = DevelopmentDemoSeed.Placements();
            public Task<CompletionPlacement> GetAsync(string id) { return Task.FromResult(Placements.SingleOrDefault(p => p.InternshipId == id)); }
            public Task<bool> CanCompanyAsync(string uid, CompletionPlacement p) { return Task.FromResult(uid == "local-company" && Placements.Any(x => x.InternshipId == p.InternshipId && x.StudentId == p.StudentId)); }
            public Task<bool> CanAcademicAsync(string uid, CompletionPlacement p) { return Task.FromResult(uid == "local-academic" && Placements.Any(x => x.InternshipId == p.InternshipId && x.StudentId == p.StudentId)); }
            public Task<bool> IsEligibleAsync(CompletionPlacement p) { return Task.FromResult(Placements.Any(x => x.InternshipId == p.InternshipId)); }
            public Task<bool> IsCoordinatorAsync(string uid) { return Task.FromResult(uid == "local-coordinator"); }
            public Task<bool> CanReviewInternshipAsync(string uid, string id) { return Task.FromResult(uid == "local-coordinator" && Placements.Any(p => p.InternshipId == id)); }
            private TrustedCompletionData<T> Data<T>(string id, T value) { var p = Placements.Single(x => x.InternshipId == id); return new TrustedCompletionData<T> { IsTrusted = true, InternshipId = id, StudentId = p.StudentId, PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion, Value = value }; }
            public Task<TrustedCompletionData<bool?>> GetEligibilityAsync(string id) { return Task.FromResult(Data<bool?>(id, true)); }
            Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id) { return Task.FromResult(Data(id, DevelopmentDemoSeed.Hours(id))); }
            Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { return Task.FromResult(Data<decimal?>(id, 10)); }
            Task<TrustedCompletionData<CompletionChecklist>> IRequiredTasksReader.GetAsync(string id) { return Task.FromResult(Data(id, DevelopmentDemoSeed.TaskRequirements(id))); }
            Task<TrustedCompletionData<CompletionChecklist>> IRequiredReportsReader.GetAsync(string id) { return Task.FromResult(Data(id, DevelopmentDemoSeed.ReportRequirements(id))); }
            public Task<bool> CanGenerateAsync(string uid, string id) { return CanReviewInternshipAsync(uid, id); }
            public Task<bool> IsStudentOwnerAsync(string uid, string id, string student)
            { int index = uid == "local-student" ? 0 : Array.IndexOf(new[] { "local-student-A", "local-student-B", "local-student-C" }, uid); return Task.FromResult(index >= 0 && id == Placements[index].InternshipId && (student == null || student == Placements[index].StudentId)); }
            public async Task<VerifiedCompletionReportData> GetAsync(string id, string student, string policy, string version)
            {
                var review = await Hub.Repository.GetCompletionReviewAsync(id);
                return new VerifiedCompletionReportData { IsTrusted = true, InternshipId = id, StudentId = student, PolicyId = policy, PolicyVersion = version,
                    ApprovedAtUtc = review == null ? null : review.ReviewedAtUtc, StudentName = Placements.Single(p => p.InternshipId == id && p.StudentId == student).DisplayName, StudentNumber = "DEMO-" + student, OrganizationName = "DEMO Northstar Training",
                    PeriodStart = new DateTime(2026, 1, 1), PeriodEnd = new DateTime(2026, 2, 1), ApprovedHours = 10 };
            }
            Task<TrustedCompletionData<StudentTaskRecord[]>> IStudentVisibleTaskReader.GetAsync(string id) { return Task.FromResult(Data(id, DevelopmentDemoSeed.Tasks(id))); }
            Task<TrustedCompletionData<decimal?>> IStudentOverallProgressReader.GetAsync(string id) { return Task.FromResult(Data<decimal?>(id, null)); }
            public Task<string> GetIssuedDocumentIdAsync(string id, string student) { var persistent = Hub.Repository as IDevelopmentDemoDocuments; if (persistent != null) return persistent.LatestDocumentAsync(id); string doc; Hub.Documents.TryGetValue(id, out doc); return Task.FromResult(doc); }
        }
    }
}
