using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Request-owned snapshot. Mutations use the database-root ETag, protecting *all* evidence
    // through the conditional commit, including writes by other pages/processes. An unrelated
    // concurrent write causes a conflict; no remote record is silently overwritten.
    public sealed class LiveFirebaseData : IFinalEvaluationRepository, IPercentageEvaluationRepository,
        ICompletionDecisionRepository, ICompletionDecisionGuard, IAuthenticatedUser,
        ICoordinatorEvaluationAccess, IPercentageEvaluationAccess, ICompletionPlacementReader,
        IStudentCompletionPlacementReader, ICompletionEligibilityReader, IApprovedTrainingHoursReader,
        IRequiredTrainingHoursReader, IRequiredTasksReader, IRequiredReportsReader,
        IStudentVisibleTaskReader, IStudentOverallProgressReader, IStudentPendingTrainingHoursReader,
        IStudentCompletionReportLocator, ICompletionReportAccess, ICompletionReportDataReader,
        ICompletionDocumentMetadataRepository
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        private static readonly GoogleAdcAccessTokenProvider Tokens = new GoogleAdcAccessTokenProvider();
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };
        private readonly UIMS.Services.FirebaseConfiguration configuration = new UIMS.Services.FirebaseConfiguration();
        private JObject root;
        private string etag;
        private bool leased;
        public string UserId { get; private set; }
        public JObject Profile { get { return Node("users/" + FirebaseKey.Require(UserId)) as JObject; } }
        public string Role { get { return FirebaseSignInService.CanonicalRole(Text(Profile, "role")); } }
        public static string Text(JToken token, string field) { return token == null ? "" : (string)token[field] ?? ""; }
        public IEnumerable<JObject> Records(string path) { return (Node(path) as JObject ?? new JObject()).Properties().Select(p => p.Value as JObject).Where(p => p != null); }
        private IEnumerable<JProperty> Properties(string path) { return (Node(path) as JObject ?? new JObject()).Properties(); }
        private JToken Node(string path) { JToken value = root; foreach (var key in path.Split('/')) value = value?[key]; return value; }
        private T Read<T>(string path) where T : class { return Node(path)?.ToObject<T>(JsonSerializer.Create(Json)); }
        private string Version(string path)
        {
            var value = Node(path); if (value == null) return "null_etag";
            using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToString(Formatting.None))));
        }
        public static async Task<LiveFirebaseData> OpenAsync(HttpContext context)
        {
            if (context?.User?.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException();
            var gateway = new StudentFirebaseGateway();
            string identity = context.User.Identity.Name;
            var account = identity.Contains("@") ? await gateway.FindByEmailAsync(identity).ConfigureAwait(false) : await gateway.FindAsync(identity).ConfigureAwait(false);
            if (account == null || account.Disabled) throw new UnauthorizedAccessException();
            var data = new LiveFirebaseData { UserId = account.Uid };
            await data.RefreshAsync().ConfigureAwait(false);
            data.RequireProfile(); return data;
        }
        private void RequireProfile()
        {
            if (Profile == null || Profile.Value<bool?>("isActive") != true || Role == null || Text(Profile, "deletionStatus") == "pending" ||
                (Text(Profile, "accountSetupStatus") != "" && Text(Profile, "accountSetupStatus") != "complete")) throw new UnauthorizedAccessException();
        }
        private async Task<HttpRequestMessage> RequestAsync(HttpMethod method)
        {
            configuration.EnsureEnabled();
            var request = new HttpRequestMessage(method, configuration.DatabaseUrl.TrimEnd('/') + "/.json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Tokens.GetAccessTokenAsync().ConfigureAwait(false)); return request;
        }
        private async Task RefreshAsync()
        {
            using (var request = await RequestAsync(HttpMethod.Get).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
                using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode(); etag = FirebaseDatabaseVersion.ReadETag(response);
                    root = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerSettings { DateParseHandling = DateParseHandling.None }) ?? new JObject();
                }
            }
        }
        private async Task CommitAsync(string path, object value, string expectedVersion)
        {
            if (!leased || Version(path) != expectedVersion) throw new ConflictException();
            new FirebaseFinalEvaluationConfiguration().EnsureWritesAllowed();
            var next = (JObject)root.DeepClone(); JObject parent = next; var keys = path.Split('/');
            foreach (var key in keys.Take(keys.Length - 1)) { if (!(parent[key] is JObject)) parent[key] = new JObject(); parent = (JObject)parent[key]; }
            parent[keys.Last()] = JToken.FromObject(value, JsonSerializer.Create(Json));
            using (var request = await RequestAsync(HttpMethod.Put).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("if-match", etag);
                request.Content = new StringContent(next.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) { await RefreshAsync().ConfigureAwait(false); throw new ConflictException(); }
                    response.EnsureSuccessStatusCode();
                }
            }
            await RefreshAsync().ConfigureAwait(false);
        }
        public async Task<ICompletionDecisionLease> AcquireAsync(string id)
        {
            FirebaseKey.Require(id); if (leased) throw new ConflictException();
            await RefreshAsync().ConfigureAwait(false); RequireProfile();
            if (!await new StudentFirebaseGateway().IsActiveAuthenticationUidAsync(UserId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            leased = true; return new Lease(this, id);
        }
        private sealed class Lease : ICompletionDecisionLease
        {
            private LiveFirebaseData owner; private readonly string id;
            public Lease(LiveFirebaseData owner, string id) { this.owner = owner; this.id = id; }
            public Task<bool> IsCurrentAsync(CompletionReadiness evidence) { return Task.FromResult(owner != null && owner.leased && evidence?.Placement?.InternshipId == id); }
            public void Dispose() { if (owner != null) owner.leased = false; owner = null; }
        }
        private JObject Placement(string id) { return Node("placements/" + FirebaseKey.Require(id)) as JObject; }
        private bool Owns(JObject p) { return p != null && (Text(p, "studentUid") == UserId || (Text(Profile, "studentId") != "" && Text(p, "studentId") == Text(Profile, "studentId"))); }
        private bool Academic(JObject p)
        {
            string student = Text(p, "studentId");
            var assignments = Records("assignments").Where(a => Text(a, "studentId") == student && a.Value<bool?>("active") != false).ToArray();
            return assignments.Length > 0 ? assignments.Any(a => Text(a, "academicSupervisorId") == UserId) : Text(p, "academicSupervisorId") == UserId;
        }
        private bool Company(JObject p)
        {
            string supervisor = Text(Profile, "companySupervisorId"); if (supervisor == "") supervisor = UserId;
            return p != null && Text(p, "companySupervisorId") == supervisor && Text(p, "companyId") == Text(Profile, "companyId");
        }
        private bool Coordinator(JObject p)
        {
            // Coordinator account management is university-wide. Apply university/college scope
            // whenever those IDs are present on both the profile and authoritative student.
            if (p == null || Role != "coordinator") return false;
            var student = Properties("users").Select(x => x.Value).FirstOrDefault(u => Text(u, "studentId") == Text(p, "studentId"));
            return new[] { "universityId", "collegeId" }.All(key => Text(Profile, key) == "" || Text(Profile, key) == Text(student, key));
        }
        public CompletionPlacement[] Placements()
        {
            return Properties("placements").Where(x => x.Value is JObject).Where(x => Role == "coordinator" ? Coordinator((JObject)x.Value) : Role == "student" ? Owns((JObject)x.Value) : Role == "academicSupervisor" ? Academic((JObject)x.Value) : Role == "companySupervisor" && Company((JObject)x.Value))
                .Select(x => MapPlacement(x.Name, (JObject)x.Value)).ToArray();
        }
        private CompletionPlacement MapPlacement(string id, JObject p) { return new CompletionPlacement { InternshipId = id, StudentId = Text(p, "studentId"), DisplayName = Text(p, "studentName"), PolicyId = Text(p, "policyId"), PolicyVersion = Text(p, "policyVersion") }; }
        public Task<CompletionPlacement> GetAsync(string id) { var p = Placement(id); return Task.FromResult(p == null ? null : MapPlacement(id, p)); }
        public Task<bool> IsCoordinatorAsync(string uid) { return Task.FromResult(uid == UserId && Role == "coordinator"); }
        public Task<bool> CanReviewInternshipAsync(string uid, string id) { return Task.FromResult(uid == UserId && Coordinator(Placement(id))); }
        public Task<bool> CanCompanyAsync(string uid, CompletionPlacement p) { return Task.FromResult(uid == UserId && Role == "companySupervisor" && Company(Placement(p.InternshipId))); }
        public Task<bool> CanAcademicAsync(string uid, CompletionPlacement p) { return Task.FromResult(uid == UserId && Role == "academicSupervisor" && Academic(Placement(p.InternshipId))); }
        public Task<bool> IsEligibleAsync(CompletionPlacement p) { return Task.FromResult(new[] { "active", "completed" }.Contains(Text(Placement(p.InternshipId), "status").ToLowerInvariant())); }
        public Task<bool> CanGenerateAsync(string uid, string id) { return CanReviewInternshipAsync(uid, id); }
        public Task<bool> IsStudentOwnerAsync(string uid, string id, string student) { var p = Placement(id); return Task.FromResult(uid == UserId && Role == "student" && Owns(p) && (student == null || student == Text(p, "studentId"))); }
        private TrustedCompletionData<T> Data<T>(string id, T value, bool trusted = true) { var p = Placement(id); return new TrustedCompletionData<T> { IsTrusted = trusted && p != null, InternshipId = id, StudentId = Text(p, "studentId"), PolicyId = Text(p, "policyId"), PolicyVersion = Text(p, "policyVersion"), Value = value }; }
        public Task<TrustedCompletionData<bool?>> GetEligibilityAsync(string id) { return Task.FromResult(Data<bool?>(id, new[] { "active", "completed" }.Contains(Text(Placement(id), "status").ToLowerInvariant()))); }
        private IEnumerable<JObject> Attendance(string id)
        {
            var p = Placement(id);
            var assignments = Properties("uimsAttendance/trainingAssignments").Where(a => Text(a.Value, "placementId") == id && Text(a.Value, "studentId") == Text(p, "studentId"));
            return assignments.SelectMany(a => Records("uimsAttendance/attendance/" + a.Name)).Where(a => Text(a, "studentId") == Text(p, "studentId"));
        }
        Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id)
        {
            return Task.FromResult(Data(id, Attendance(id).Select(a => new TrainingHourEntry { EntryId = Text(a, "assignmentId") + ":" + Text(a, "attendanceDate"), Hours = (a.Value<decimal?>("workedMinutes") ?? 0) / 60m, IsApproved = Text(a, "hoursApprovalStatus") == "approved" }).ToArray()));
        }
        public Task<TrustedCompletionData<decimal?>> GetPendingHoursAsync(string id) { return Task.FromResult(Data<decimal?>(id, Attendance(id).Where(a => Text(a, "hoursApprovalStatus") == "pending").Sum(a => (a.Value<decimal?>("workedMinutes") ?? 0) / 60m))); }
        Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { var p = Placement(id); return Task.FromResult(Data<decimal?>(id, p?.Value<decimal?>("requiredTrainingHours") ?? p?.Value<decimal?>("requiredHours"))); }
        private IEnumerable<JObject> Tasks(string id) { return Records("tasks").Where(t => Text(t, "placementId") == id && Text(t, "studentId") == Text(Placement(id), "studentId")); }
        Task<TrustedCompletionData<CompletionChecklist>> IRequiredTasksReader.GetAsync(string id)
        {
            var tasks = Tasks(id).ToArray(); return Task.FromResult(Data(id, new CompletionChecklist { RequiredIds = tasks.Select(t => Text(t, "id")).ToArray(), CompletedIds = tasks.Where(t => new[] { "completed", "evaluated" }.Contains(Text(t, "status").ToLowerInvariant())).Select(t => Text(t, "id")).ToArray() }));
        }
        Task<TrustedCompletionData<CompletionChecklist>> IRequiredReportsReader.GetAsync(string id)
        {
            // Missing requirements are unknown, never an invented empty checklist.
            var p = Placement(id); var required = p?["requiredReportIds"] as JArray ?? Node("evaluationPolicies/" + Text(p, "policyId"))?["requiredReportIds"] as JArray;
            var completed = Properties("weeklyReports").Where(r => Text(r.Value, "placementId") == id && Text(r.Value, "status").Equals("approved", StringComparison.OrdinalIgnoreCase)).Select(r => r.Name).ToArray();
            return Task.FromResult(Data(id, new CompletionChecklist { RequiredIds = required?.Values<string>().ToArray(), CompletedIds = completed }, required != null));
        }
        Task<TrustedCompletionData<StudentTaskRecord[]>> IStudentVisibleTaskReader.GetAsync(string id)
        {
            return Task.FromResult(Data(id, Tasks(id).Select(t => new StudentTaskRecord { Id = Text(t, "id"), Title = Text(t, "title"), IsVisibleToStudent = t.Value<bool?>("isVisibleToStudent") != false,
                Status = Text(t, "status").Equals("completed", StringComparison.OrdinalIgnoreCase) || Text(t, "status").Equals("evaluated", StringComparison.OrdinalIgnoreCase) ? StudentTaskStatus.Evaluated : Text(t, "status").Equals("submitted", StringComparison.OrdinalIgnoreCase) ? StudentTaskStatus.Submitted : Text(t, "status").Equals("revision", StringComparison.OrdinalIgnoreCase) ? StudentTaskStatus.Revision : StudentTaskStatus.Assigned,
                IsResultPublished = t.Value<bool?>("isResultPublished") == true, Grade = t.Value<decimal?>("score"), MaximumGrade = t.Value<decimal?>("maximumScore"), StudentVisibleFeedback = Text(t, "feedback") }).ToArray()));
        }
        Task<TrustedCompletionData<decimal?>> IStudentOverallProgressReader.GetAsync(string id) { return Task.FromResult(Data<decimal?>(id, null, false)); }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { return Task.FromResult(new RecordSnapshot<CompanyFinalEvaluation>(Read<CompanyFinalEvaluation>("companyFinalEvaluations/" + FirebaseKey.Require(id)), Version("companyFinalEvaluations/" + id))); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { return Task.FromResult(new RecordSnapshot<AcademicFinalEvaluation>(Read<AcademicFinalEvaluation>("academicFinalEvaluations/" + FirebaseKey.Require(id)), Version("academicFinalEvaluations/" + id))); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation v) { throw new NotSupportedException("Use percentage evaluations."); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation v, string version) { throw new NotSupportedException(); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation v) { throw new NotSupportedException("Use academic review."); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation v, string version) { throw new NotSupportedException(); }
        public Task<CompletionReview> GetCompletionReviewAsync(string id) { return Task.FromResult(Read<CompletionReview>("completionReviews/" + FirebaseKey.Require(id))); }
        public Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string id) { return Task.FromResult(new RecordSnapshot<CompletionReview>(Read<CompletionReview>("completionReviews/" + FirebaseKey.Require(id)), Version("completionReviews/" + id))); }
        public Task<CompletionDocument> GetCompletionDocumentAsync(string id, string doc) { return Task.FromResult(Read<CompletionDocument>("completionDocuments/" + FirebaseKey.Require(id) + "/" + FirebaseKey.Require(doc))); }
        public Task<EvaluationPolicy> GetPolicyAsync(string id) { return Task.FromResult(Read<EvaluationPolicy>("evaluationPolicies/" + FirebaseKey.Require(id))); }
        public Task<RecordSnapshot<PercentageEvaluation>> GetPercentageAsync(string id)
        {
            var path = "finalEvaluationReviewsV2/" + FirebaseKey.Require(id); var value = Read<PercentageEvaluation>(path);
            if (value != null) PercentageEvaluationWorkflow.Validate(value); return Task.FromResult(new RecordSnapshot<PercentageEvaluation>(value, Version(path)));
        }
        public async Task SavePercentageAsync(PercentageEvaluation value, string version)
        {
            var path = "finalEvaluationReviewsV2/" + FirebaseKey.Require(value.InternshipId);
            PercentageHistory.Check(Read<PercentageEvaluation>(path), value); await CommitAsync(path, value, version).ConfigureAwait(false);
        }
        public async Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite write, string version)
        {
            var path = "completionReviews/" + FirebaseKey.Require(write.InternshipId);
            var next = CompletionDecisionValidation.Apply(Read<CompletionReview>(path), write, await GetPolicyAsync(write.PolicyId).ConfigureAwait(false));
            await CommitAsync(path, next, version).ConfigureAwait(false); return next;
        }
        public Task<string> GetIssuedDocumentIdAsync(string id, string student) { return Task.FromResult(Records("completionDocuments/" + FirebaseKey.Require(id)).Where(d => Text(d, "studentId") == student && Text(d, "status") == "Issued").OrderByDescending(d => Text(d, "issuedAtUtc")).Select(d => Text(d, "documentId")).FirstOrDefault()); }
        public async Task CreateIssuedCompletionReportAsync(CompletionDocument document)
        {
            CompletionReportValidation.Metadata(document);
            using (await AcquireAsync(document.InternshipId).ConfigureAwait(false))
            {
                if (!await CanGenerateAsync(UserId, document.InternshipId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
                var review = await GetCompletionReviewAsync(document.InternshipId).ConfigureAwait(false);
                if (review?.Status != CompletionStatus.Approved || review.StudentId != document.StudentId || review.ReviewedAtUtc != document.CompletionApprovedAtUtc) throw new ConflictException();
                await CommitAsync("completionDocuments/" + FirebaseKey.Require(document.InternshipId) + "/" + FirebaseKey.Require(document.DocumentId), document, "null_etag").ConfigureAwait(false);
            }
        }
        public Task<VerifiedCompletionReportData> GetAsync(string id, string student, string policy, string version)
        {
            var p = Placement(id); var review = Read<CompletionReview>("completionReviews/" + id); DateTime start, end;
            return Task.FromResult(new VerifiedCompletionReportData { IsTrusted = p != null && Text(p, "studentId") == student && Text(p, "policyId") == policy && Text(p, "policyVersion") == version,
                InternshipId = id, StudentId = student, PolicyId = policy, PolicyVersion = version, ApprovedAtUtc = review?.ReviewedAtUtc,
                StudentName = Text(p, "studentName"), StudentNumber = student, OrganizationName = Text(p, "companyName"), UniversityName = Text(p, "universityName"), CollegeName = Text(p, "collegeName"),
                PeriodStart = DateTime.TryParse(Text(p, "startDate"), out start) ? start : (DateTime?)null, PeriodEnd = DateTime.TryParse(Text(p, "endDate"), out end) ? end : (DateTime?)null,
                ApprovedHours = Attendance(id).Where(a => Text(a, "hoursApprovalStatus") == "approved").Sum(a => (a.Value<decimal?>("workedMinutes") ?? 0) / 60m) });
        }
    }
    public static class LiveFirebaseIntegration
    {
        public static void Register()
        {
            PercentagePageIntegration.Factory = PercentageAsync;
            CoordinatorReviewPageIntegration.Factory = context => Task.Run(() => CoordinatorAsync(context)).GetAwaiter().GetResult();
            StudentCompletionPageIntegration.Factory = StudentAsync;
        }
        private static CompletionReportService Reports(LiveFirebaseData data) { return new CompletionReportService(data, data, data, data, new CompletionReportPdfRenderer(), new PrivateStorage()); }
        private static async Task<PercentagePageContext> PercentageAsync(HttpContext context, bool academic)
        {
            var data = await LiveFirebaseData.OpenAsync(context).ConfigureAwait(false);
            if (data.Role != (academic ? "academicSupervisor" : "companySupervisor")) throw new UnauthorizedAccessException();
            return new PercentagePageContext { UserId = data.UserId, Placements = data.Placements(), Workflow = new PercentageEvaluationWorkflow(data, data, data, data, data),
                ReadDetailsAsync = id => Task.FromResult("Firebase placement: " + id), ReadReportsAsync = id => Task.FromResult("Review weekly reports on the Weekly Reports page.") };
        }
        private static async Task<CoordinatorReviewPageContext> CoordinatorAsync(HttpContext context)
        {
            var data = await LiveFirebaseData.OpenAsync(context).ConfigureAwait(false); if (data.Role != "coordinator") throw new UnauthorizedAccessException();
            var readiness = new CoordinatorReadinessWorkflow(data, data, data, data, data, data, data, data);
            var reports = Reports(data);
            return new CoordinatorReviewPageContext(readiness, data, data, data.Placements(), new CompletionDecisionWorkflow(data, data, data, readiness, data)) { GenerateReportAsync = reports.GenerateAsync };
        }
        private static async Task<StudentCompletionPageContext> StudentAsync(HttpContext context)
        {
            var data = await LiveFirebaseData.OpenAsync(context).ConfigureAwait(false); if (data.Role != "student") throw new UnauthorizedAccessException();
            var p = data.Placements().FirstOrDefault();
            return new StudentCompletionPageContext(new StudentCompletionWorkflow(data, data, data, data, data, data, data, data, data, Reports(data)), p?.InternshipId, LiveFirebaseData.Text(data.Profile, "fullName"));
        }
        private sealed class PrivateStorage : IPrivateCompletionReportStorage
        {
            // Completion-report records predate provider metadata; preserve their Firebase transport.
            private readonly UIMS.Services.FirebaseFileStorageService storage = new UIMS.Services.FirebaseFileStorageService(k => k == "FileStorageProvider" ? "firebase" : ConfigurationManager.AppSettings[k]);
            public async Task PutNewAsync(string key, byte[] bytes) { await storage.UploadAsync(key, "completion-report.pdf", "application/pdf", bytes, CancellationToken.None).ConfigureAwait(false); }
            public Task<byte[]> ReadAsync(string key) { return storage.DownloadAsync(key, CancellationToken.None); }
            public async Task DeleteIfHashMatchesAsync(string key, string hash) { var bytes = await ReadAsync(key).ConfigureAwait(false); if (CompletionReportValidation.Hash(bytes) == hash) await storage.DeleteObjectAsync(key, CancellationToken.None).ConfigureAwait(false); }
        }
    }
}
