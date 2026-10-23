using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Configuration;

namespace UIMS.Tasks
{
    /// <summary>Exam workflows over the existing Firebase RTDB root. Answer keys are returned only to authorized company review APIs.</summary>
    public sealed class TaskExamService
    {
        private static readonly HttpClient SharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        private static readonly Lazy<Task<GoogleCredential>> Credential = new Lazy<Task<GoogleCredential>>(() => GoogleCredential.GetApplicationDefaultAsync());
        private readonly Func<string, string> setting;
        private readonly HttpClient client;
        private readonly TaskActor actor;
        private readonly Func<CancellationToken, Task<string>> tokenProvider;
        private readonly string root;
        private readonly bool enabled;
        private readonly bool writesEnabled;
        private readonly bool schemaValidated;

        public TaskExamService(TaskActor verifiedActor) : this(k => WebConfigurationManager.AppSettings[k], SharedClient, verifiedActor, GetAccessTokenAsync, null) { }
        public TaskExamService(Func<string, string> settings, HttpClient httpClient, TaskActor verifiedActor,
            Func<CancellationToken, Task<string>> accessTokenProvider = null, UIMS.Services.FirebaseFileStorageService storage = null)
        {
            setting = settings ?? throw new ArgumentNullException(nameof(settings));
            client = httpClient ?? throw new ArgumentNullException(nameof(httpClient)); actor = verifiedActor;
            tokenProvider = accessTokenProvider ?? GetAccessTokenAsync;
            root = (setting("FirebaseDatabaseUrl") ?? "").TrimEnd('/');
            enabled = IsTrue(setting("FirebaseEnabled")) && IsTrue(setting("TaskExamsEnabled"));
            writesEnabled = IsTrue(setting("FirebaseLiveWritesEnabled")) && IsTrue(setting("TaskExamsLiveWritesEnabled"));
            schemaValidated = IsTrue(setting("FirebaseSchemaValidated")) && IsTrue(setting("TaskExamsSchemaValidated"));
            Storage = storage ?? new UIMS.Services.FirebaseFileStorageService(setting, httpClient);
        }
        internal UIMS.Services.FirebaseFileStorageService Storage { get; private set; }

        private static bool IsTrue(string value) { return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); }
        private static async Task<string> GetAccessTokenAsync(CancellationToken cancellation)
        {
            var scoped = (await Credential.Value.ConfigureAwait(false)).CreateScoped("https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
            cancellation.ThrowIfCancellationRequested();
            return await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync().ConfigureAwait(false);
        }
        private void Ensure(bool write, string role = null)
        {
            if (!enabled) throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured for exams.");
            if (!IsTrue(setting("FirebaseAuthenticationEnabled"))) throw new UIMS.Services.FirebaseException(503, "Firebase Authentication is not configured for exams.");
            Uri uri;
            if (!Uri.TryCreate(root, UriKind.Absolute, out uri) || uri.Scheme != "https" || !uri.IsDefaultPort ||
                uri.AbsolutePath != "/" || !(uri.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(503, "Configure the HTTPS Firebase database root in Web.config.");
            if (!schemaValidated) throw new UIMS.Services.FirebaseException(503, "The exam schema is not validated for this environment.");
            if (actor == null || string.IsNullOrWhiteSpace(actor.Uid)) throw new UIMS.Services.FirebaseException(401, "A verified Firebase account is required.");
            if (role != null && actor.Role != role) throw new UIMS.Services.FirebaseException(403, "This exam operation is not allowed for the signed-in role.");
            if (write && (!writesEnabled || !IsTrue(setting("FirebaseTaskSchemaValidated")))) throw new UIMS.Services.FirebaseException(503, "Exam writes are disabled until schema and authorization are validated.");
        }
        private string Url(string path) { return root + "/" + string.Join("/", path.Split('/').Where(p => p.Length != 0).Select(Uri.EscapeDataString)) + ".json"; }
        private async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, string json, string etag, CancellationToken cancellation)
        {
            if (!string.IsNullOrWhiteSpace(etag) && method != HttpMethod.Put)
                throw new InvalidOperationException("Conditional Firebase writes require PUT.");
            string token = await tokenProvider(cancellation).ConfigureAwait(false);
            var request = new HttpRequestMessage(method, Url(path));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (method == HttpMethod.Get) request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
            if (!string.IsNullOrWhiteSpace(etag)) request.Headers.TryAddWithoutValidation("if-match", etag);
            if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return await client.SendAsync(request, cancellation).ConfigureAwait(false);
        }
        private async Task<ExamNode> ReadAsync(string path, CancellationToken cancellation)
        {
            using (var response = await RequestAsync(HttpMethod.Get, path, null, null, cancellation).ConfigureAwait(false))
            {
                await EnsureResponse(response, cancellation).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                IEnumerable<string> etags; string etag = response.Headers.ETag == null ? (response.Headers.TryGetValues("ETag", out etags) ? etags.FirstOrDefault() : null) : response.Headers.ETag.Tag;
                return new ExamNode { Data = string.IsNullOrWhiteSpace(body) || body == "null" ? null : JObject.Parse(body), ETag = etag };
            }
        }
        private async Task<JToken> ReadTokenAsync(string path, CancellationToken cancellation)
        {
            using (var response = await RequestAsync(HttpMethod.Get, path, null, null, cancellation).ConfigureAwait(false))
            { await EnsureResponse(response, cancellation).ConfigureAwait(false); var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false); return string.IsNullOrWhiteSpace(body) || body == "null" ? null : JToken.Parse(body); }
        }
        private async Task<JToken> ReadEqualAsync(string path, string field, string value, CancellationToken cancellation)
        {
            string token = await tokenProvider(cancellation).ConfigureAwait(false);
            string query = "?orderBy=" + Uri.EscapeDataString(JsonConvert.SerializeObject(field)) + "&equalTo=" + Uri.EscapeDataString(JsonConvert.SerializeObject(value));
            using (var request = new HttpRequestMessage(HttpMethod.Get, Url(path) + query))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                { await EnsureResponse(response, cancellation).ConfigureAwait(false); var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false); return string.IsNullOrWhiteSpace(body) || body == "null" ? null : JToken.Parse(body); }
            }
        }
        private static async Task EnsureResponse(HttpResponseMessage response, CancellationToken cancellation)
        {
            if (response.IsSuccessStatusCode) return;
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new UIMS.Services.FirebaseException(403, "Firebase denied the exam request. Verify trusted credentials and least-privilege Rules.");
            if ((int)response.StatusCode == 412) throw new UIMS.Services.FirebaseException(409, "This exam changed concurrently. Refresh and retry.");
            if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500) throw new UIMS.Services.FirebaseException(503, "Firebase exam service is temporarily unavailable.");
            throw new UIMS.Services.FirebaseException((int)response.StatusCode, "Firebase rejected the exam operation." + (string.IsNullOrWhiteSpace(body) ? "" : " " + body));
        }
        private async Task<JObject> WriteAsync(string path, JObject value, string etag, bool patch, CancellationToken cancellation)
        {
            using (var response = await RequestAsync(patch ? new HttpMethod("PATCH") : HttpMethod.Put, path, value.ToString(Formatting.None), etag, cancellation).ConfigureAwait(false))
            { await EnsureResponse(response, cancellation).ConfigureAwait(false); var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false); return string.IsNullOrWhiteSpace(text) || text == "null" ? value : JObject.Parse(text); }
        }
        private async Task<JObject> WriteImmutableAsync(string path, JObject value, CancellationToken cancellation)
        {
            try { return await WriteAsync(path, value, "null_etag", false, cancellation).ConfigureAwait(false); }
            catch (UIMS.Services.FirebaseException ex)
            {
                if (ex.StatusCode != 409) throw;
                var stored = (await ReadAsync(path, cancellation).ConfigureAwait(false)).Data;
                if (stored == null) throw;
                var expected = (JObject)value.DeepClone(); var actual = (JObject)stored.DeepClone();
                expected.Remove("publishedAt"); actual.Remove("publishedAt");
                if (!JToken.DeepEquals(expected, actual)) throw new UIMS.Services.FirebaseException(409, "An immutable exam record already exists with different content.");
                return stored;
            }
        }
        private static string Text(JToken obj, params string[] names)
        { foreach (var n in names) { var value = obj is JObject ? ((JObject)obj)[n] : null; if (value != null && value.Type != JTokenType.Null && value.ToString().Trim().Length != 0) return value.ToString(); } return ""; }
        private static bool Matches(JToken record, string expected, params string[] fields)
        { return fields.Any(f => string.Equals(Text(record, f), expected, StringComparison.OrdinalIgnoreCase)); }
        private async Task<JObject> RequirePlacementAsync(string placementId, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(placementId)) throw new UIMS.Services.FirebaseException(400, "A placement is required.");
            var placement = await ReadTokenAsync("placements/" + placementId, cancellation).ConfigureAwait(false) as JObject;
            if (placement == null) throw new UIMS.Services.FirebaseException(404, "Placement was not found.");
            if (actor.Role == "companySupervisor")
            {
                string placementCompany = Text(placement, "companyId");
                bool sameCompany = actor.CompanyId.Length > 0 && placementCompany.Length > 0 && actor.CompanyId == placementCompany;
                bool assignedSupervisor = Matches(placement, actor.CompanySupervisorId, "companySupervisorId", "supervisorId", "assignedSupervisorId");
                if ((placementCompany.Length > 0 && actor.CompanyId.Length > 0 && placementCompany != actor.CompanyId) || (!sameCompany && !assignedSupervisor))
                    throw new UIMS.Services.FirebaseException(403, "This placement is not assigned to the verified company supervisor or company.");
            }
            if (actor.Role == "student" && !StudentMatches(placement, "studentId", "studentProfileId"))
                throw new UIMS.Services.FirebaseException(403, "This placement is not assigned to the verified student.");
            var status = Text(placement, "status", "placementStatus");
            if (status.Length > 0 && !(status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) || status.Equals("active", StringComparison.OrdinalIgnoreCase) || status.Equals("inProgress", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(409, "The internship placement is not active for exam use.");
            return placement;
        }
        private async Task<JObject> RequireTaskAsync(string taskId, string placementId, CancellationToken cancellation)
        {
            var task = await ReadTokenAsync("tasks/" + taskId, cancellation).ConfigureAwait(false) as JObject;
            if (task == null) throw new UIMS.Services.FirebaseException(404, "The linked task was not found.");
            if (!string.Equals(Text(task, "placementId"), placementId, StringComparison.Ordinal)) throw new UIMS.Services.FirebaseException(403, "The task does not belong to this placement.");
            if (actor.Role == "companySupervisor")
            {
                string taskCompany = Text(task, "companyId"); bool sameCompany = actor.CompanyId.Length > 0 && taskCompany.Length > 0 && actor.CompanyId == taskCompany;
                bool assignedSupervisor = Matches(task, actor.CompanySupervisorId, "assignedBy", "companySupervisorId", "supervisorId");
                if ((taskCompany.Length > 0 && actor.CompanyId.Length > 0 && taskCompany != actor.CompanyId) || (!sameCompany && !assignedSupervisor))
                    throw new UIMS.Services.FirebaseException(403, "The linked task is outside this company supervisor's authority.");
            }
            if (actor.Role == "student" && !StudentMatches(task, "studentId", "assignedTo", "traineeId")) throw new UIMS.Services.FirebaseException(403, "The linked task is not assigned to this student.");
            return task;
        }
        private string ExamPath(string id) { return "taskExams/" + id; }
        private bool StudentMatches(JToken record, params string[] fields)
        { return fields.Any(field => actor.IsStudent(Text(record, field))); }
        private async Task<JObject> ReadExamAsync(string id, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new UIMS.Services.FirebaseException(400, "An exam ID is required.");
            ValidateKey(id);
            var node = await ReadAsync(ExamPath(id), cancellation).ConfigureAwait(false);
            if (node.Data == null) throw new UIMS.Services.FirebaseException(404, "Exam was not found.");
            return node.Data;
        }
        private async Task EnsureExamCompanyAsync(JObject exam, CancellationToken cancellation)
        {
            var placement = await RequirePlacementAsync(Text(exam, "placementId"), cancellation).ConfigureAwait(false);
            var linkedTask = await RequireTaskAsync(Text(exam, "taskId"), Text(exam, "placementId"), cancellation).ConfigureAwait(false);
            if (!ResponsibleExamCreator(actor.CompanySupervisorId, placement, linkedTask)) throw new UIMS.Services.FirebaseException(403, "This examination is not assigned to the verified supervisor.");
            string examCompany = Text(exam, "companyId"); string creator = Text(exam, "createdBy");
            if (examCompany.Length > 0 ? actor.CompanyId.Length == 0 || !string.Equals(examCompany, actor.CompanyId, StringComparison.Ordinal) : !string.Equals(creator, actor.CompanySupervisorId, StringComparison.Ordinal))
                throw new UIMS.Services.FirebaseException(403, "This exam belongs to another company.");
        }

        public async Task<JObject> ListAsync(CancellationToken cancellation)
        {
            Ensure(false);
            var result = new JObject();
            var candidates = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (actor.Role == "companySupervisor")
            {
                string companyId = actor.CompanyId;
                if (companyId.Length > 0)
                {
                    AddRecords(candidates, await ReadEqualAsync("taskExams", "companyId", companyId, cancellation).ConfigureAwait(false));
                }
                AddRecords(candidates, await ReadEqualAsync("taskExams", "createdBy", actor.CompanySupervisorId, cancellation).ConfigureAwait(false));
            }
            else if (actor.Role == "student")
            {
                var placements = new Dictionary<string, JObject>(StringComparer.Ordinal);
                foreach (var identity in new[] { actor.StudentId, actor.Uid }.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
                {
                    AddRecords(placements, await ReadEqualAsync("placements", "studentId", identity, cancellation).ConfigureAwait(false));
                    AddRecords(placements, await ReadEqualAsync("placements", "studentProfileId", identity, cancellation).ConfigureAwait(false));
                }
                foreach (var placement in placements.Keys) AddRecords(candidates, await ReadEqualAsync("taskExams", "placementId", placement, cancellation).ConfigureAwait(false));
            }
            foreach (var pair in candidates)
            {
                var exam = pair.Value;
                if (actor.Role == "companySupervisor")
                {
                    try { await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false); result[pair.Key] = await CompanyExamSummaryAsync(pair.Key, exam, cancellation).ConfigureAwait(false); }
                    catch (UIMS.Services.FirebaseException ex) { if (ex.StatusCode != 403 && ex.StatusCode != 404) throw; }
                }
                else if (actor.Role == "student" && string.Equals(Text(exam, "status"), "published", StringComparison.OrdinalIgnoreCase) &&
                    await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false)) result[pair.Key] = await StudentExamSummaryAsync(pair.Key, exam, cancellation).ConfigureAwait(false);
            }
            return result;
        }
        private static void AddRecords(Dictionary<string, JObject> target, JToken records)
        {
            var map = records as JObject; if (map == null) return;
            foreach (var pair in map.Properties()) if (pair.Value is JObject) target[pair.Name] = (JObject)pair.Value;
        }
        private async Task<JObject> CompanyExamSummaryAsync(string examId, JObject source, CancellationToken cancellation)
        {
            var visible = PublicExam(source, true);
            var students = await ReadTokenAsync("taskExamAttempts/" + examId, cancellation).ConfigureAwait(false) as JObject ?? new JObject();
            var latest = students.Properties().Select(student => (student.Value as JObject ?? new JObject()).Properties()
                .Where(p => p.Value is JObject).OrderByDescending(p => ParseDate(p.Value["startedAt"])).Select(p => (JObject)p.Value).FirstOrDefault())
                .Where(item => item != null).ToList();
            int pending = latest.Count(item => (Text(item, "status") == "submitted" || Text(item, "status") == "expired") &&
                Text(item, "resultStatus") != "released" && Text(item, "resultStatus") != "approvedHidden" && Text(item, "resultStatus") != "revisionRequested");
            visible["pendingReviewCount"] = pending;
            visible["reviewState"] = pending > 0 ? "Awaiting supervisor review" : latest.Any(item => Text(item, "status") == "inProgress") ? "In progress" :
                latest.Any(item => Text(item, "resultStatus") == "revisionRequested") ? "Not complete" : latest.Any(item => Text(item, "resultStatus") == "approvedHidden") ? "Completed · Result hidden" :
                latest.Any(item => Text(item, "resultStatus") == "released") ? "Completed · Result released" : "No submissions yet";
            return visible;
        }
        private async Task<bool> IsStudentExamAuthorizedAsync(JObject exam, CancellationToken cancellation)
        {
            try
            {
                var placement = await RequirePlacementAsync(Text(exam, "placementId"), cancellation).ConfigureAwait(false);
                var task = await RequireTaskAsync(Text(exam, "taskId"), Text(exam, "placementId"), cancellation).ConfigureAwait(false);
                string creator = Text(exam, "createdBy"), company = Text(exam, "companyId");
                bool responsible = ResponsibleExamCreator(creator, placement, task);
                if (!responsible || (company.Length > 0 && Text(placement, "companyId").Length > 0 && company != Text(placement, "companyId"))) return false;
                return true;
            }
            catch (UIMS.Services.FirebaseException ex) { if (ex.StatusCode == 403 || ex.StatusCode == 404) return false; throw; }
        }
        private static bool ResponsibleExamCreator(string creator, JObject placement, JObject task)
        { return creator.Length > 0 && (Matches(placement, creator, "companySupervisorId", "supervisorId", "assignedSupervisorId") || Matches(task, creator, "assignedBy", "companySupervisorId", "supervisorId")); }
        private static JObject PublicExam(JObject source, bool company)
        {
            var visible = (JObject)source.DeepClone();
            if (!company) visible.Remove("draft"); visible.Remove("answerKey"); visible.Remove("grading");
            if (company) return visible;
            // Public projections contain only the published immutable version and safe availability settings.
            var version = Text(source, "publishedVersion");
            visible.Remove("versions");
            visible["version"] = version;
            return visible;
        }
        private async Task<JObject> StudentExamSummaryAsync(string examId, JObject source, CancellationToken cancellation)
        {
            int version = (int?)source["publishedVersion"] ?? 0;
            var published = await ReadTokenAsync(ExamPath(examId) + "/versions/" + version, cancellation).ConfigureAwait(false) as JObject;
            if (published == null) return null;
            published = StudentAvailability(source, published, actor.StudentId);
            var summary = new JObject { ["title"] = published["title"], ["opensAt"] = published["opensAt"], ["closesAt"] = published["closesAt"], ["durationMinutes"] = published["durationMinutes"], ["attemptLimit"] = published["attemptLimit"], ["totalPoints"] = published["totalPoints"], ["publishedAt"] = published["publishedAt"], ["version"] = published["version"] };
            var visible = new JObject { ["id"] = examId, ["taskId"] = source["taskId"], ["placementId"] = source["placementId"], ["status"] = "published", ["publishedVersion"] = version, ["versionData"] = summary };
            summary["instructions"] = published["instructions"];
            var task = await ReadTokenAsync("tasks/" + Text(source, "taskId"), cancellation).ConfigureAwait(false);
            visible["taskTitle"] = Text(task, "title");
            summary["questionCount"] = (published["questions"] as JObject)?.Count ?? 0;
            visible["serverNow"] = UtcNow();
            return PublicExam(visible, false);
        }
        public async Task<JObject> EligibilityAsync(string examId, CancellationToken cancellation)
        {
            Ensure(false, "student"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            if (Text(exam, "status") != "published" || !await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false))
                throw new UIMS.Services.FirebaseException(404, "Exam is unavailable.");
            var version = await ReadTokenAsync(ExamPath(examId) + "/versions/" + Text(exam, "publishedVersion"), cancellation).ConfigureAwait(false) as JObject;
            if (version != null) version = StudentAvailability(exam, version, actor.StudentId);
            var attempts = await ReadTokenAsync("taskExamAttempts/" + examId + "/" + actor.StudentId, cancellation).ConfigureAwait(false) as JObject ?? new JObject();
            var now = DateTimeOffset.UtcNow;
            bool active = attempts.Properties().Any(p => Text(p.Value, "status") == "inProgress" && now < ParseDate(p.Value["deadlineAt"]));
            return new JObject { ["serverNow"] = UtcNow(), ["eligible"] = version != null && now >= ParseDate(version["opensAt"]) && now < ParseDate(version["closesAt"]) && (active || attempts.Count < ((int?)version["attemptLimit"] ?? 1)) };
        }
        public async Task<JObject> CreateDraftAsync(JObject input, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor");
            string placementId = Text(input, "placementId"), taskId = Text(input, "taskId");
            var placement = await RequirePlacementAsync(placementId, cancellation).ConfigureAwait(false); var linkedTask = await RequireTaskAsync(taskId, placementId, cancellation).ConfigureAwait(false);
            if (!ResponsibleExamCreator(actor.CompanySupervisorId, placement, linkedTask)) throw new UIMS.Services.FirebaseException(403, "Only a supervisor responsible for this task or placement can create its examination.");
            var examId = "EX-" + Guid.NewGuid().ToString("N");
            string creationKey = Text(input, "creationKey");
            if (creationKey.Length > 0)
            {
                ValidateKey(creationKey);
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    examId = "EX-" + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(actor.CompanySupervisorId + "|" + placementId + "|" + taskId + "|" + creationKey))).Replace("-", "").ToLowerInvariant();
                var prior = (await ReadAsync(ExamPath(examId), cancellation).ConfigureAwait(false)).Data;
                if (prior != null)
                {
                    await EnsureExamCompanyAsync(prior, cancellation).ConfigureAwait(false);
                    if (Text(prior, "taskId") != taskId || Text(prior, "placementId") != placementId)
                        throw new UIMS.Services.FirebaseException(409, "The examination creation key belongs to another assignment.");
                    return new JObject { ["id"] = examId, ["exam"] = PublicExam(prior, true) };
                }
            }
            var draft = ValidateDraft(input, null); var draftKeys = (JObject)draft["answerKeys"].DeepClone(); draft.Remove("answerKeys");
            var record = new JObject { ["id"] = examId, ["taskId"] = taskId, ["placementId"] = placementId, ["companyId"] = actor.CompanyId,
                ["createdBy"] = actor.CompanySupervisorId, ["status"] = "draft", ["publishedVersion"] = 0, ["draftRevision"] = 1, ["draft"] = draft, ["createdAt"] = UtcNow() };
            await WriteImmutableAsync("taskExamAnswerKeys/" + examId + "/draftVersions/1", draftKeys, cancellation).ConfigureAwait(false);
            try { await WriteAsync(ExamPath(examId), record, "null_etag", false, cancellation).ConfigureAwait(false); }
            catch (UIMS.Services.FirebaseException error) when (creationKey.Length > 0 && error.StatusCode == 409)
            {
                var prior = (await ReadAsync(ExamPath(examId), cancellation).ConfigureAwait(false)).Data;
                if (prior == null) throw;
                await EnsureExamCompanyAsync(prior, cancellation).ConfigureAwait(false);
                return new JObject { ["id"] = examId, ["exam"] = PublicExam(prior, true) };
            }
            return new JObject { ["id"] = examId, ["exam"] = PublicExam(record, true) };
        }
        public async Task<JObject> SaveDraftAsync(string examId, JObject input, string etag, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); var node = await ReadAsync(ExamPath(examId), cancellation).ConfigureAwait(false); var exam = node.Data;
            if (exam == null) throw new UIMS.Services.FirebaseException(404, "Exam was not found."); await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            if (Text(exam, "status") != "draft") throw new UIMS.Services.FirebaseException(409, "Published exams are immutable. Create a new draft exam to edit.");
            int expectedRevision, currentRevision = (int?)exam["draftRevision"] ?? 1;
            if (!int.TryParse(etag, NumberStyles.Integer, CultureInfo.InvariantCulture, out expectedRevision) || expectedRevision != currentRevision) throw new UIMS.Services.FirebaseException(409, "This draft changed in another session. Reload it before saving.");
            var draft = ValidateDraft(input, exam); var draftKeys = (JObject)draft["answerKeys"].DeepClone(); draft.Remove("answerKeys");
            int nextRevision = currentRevision + 1;
            await WriteImmutableAsync("taskExamAnswerKeys/" + examId + "/draftVersions/" + nextRevision, draftKeys, cancellation).ConfigureAwait(false);
            exam["draft"] = draft; exam["draftRevision"] = nextRevision; exam["updatedAt"] = UtcNow();
            await WriteAsync(ExamPath(examId), exam, node.ETag, false, cancellation).ConfigureAwait(false);
            return new JObject { ["id"] = examId, ["draft"] = draft, ["draftRevision"] = nextRevision };
        }
        private static JObject ValidateDraft(JObject input, JObject existing)
        {
            if (input == null) throw new UIMS.Services.FirebaseException(400, "Exam content is required.");
            string title = Text(input, "title").Trim(); if (title.Length < 3 || title.Length > 180) throw new UIMS.Services.FirebaseException(400, "Exam title must contain 3 to 180 characters.");
            int duration, attemptLimit;
            if (!int.TryParse(Text(input, "durationMinutes"), out duration) || duration < 1 || duration > 600) throw new UIMS.Services.FirebaseException(400, "Exam duration must be between 1 and 600 minutes.");
            if (!int.TryParse(Text(input, "attemptLimit"), out attemptLimit) || attemptLimit < 1 || attemptLimit > 10) throw new UIMS.Services.FirebaseException(400, "Attempt limit must be between 1 and 10.");
            DateTimeOffset opens, closes;
            string opensText = Text(input, "opensAt"), closesText = Text(input, "closesAt");
            if (!DateTimeOffset.TryParse(opensText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out opens) || !DateTimeOffset.TryParse(closesText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out closes) || closes <= opens)
                throw new UIMS.Services.FirebaseException(400, "Valid opening and closing times are required.");
            var questions = input["questions"] as JArray;
            if (questions == null || questions.Count == 0 || questions.Count > 100) throw new UIMS.Services.FirebaseException(400, "Add between 1 and 100 questions before saving.");
            var ids = new HashSet<string>(StringComparer.Ordinal); var output = new JObject(); var keys = new JObject(); var order = new JArray(); decimal total = 0;
            foreach (var token in questions)
            {
                var q = token as JObject; if (q == null) throw new UIMS.Services.FirebaseException(400, "Invalid question format.");
                string id = Text(q, "id"); if (id.Length == 0) id = "Q-" + Guid.NewGuid().ToString("N");
                ValidateKey(id);
                if (!ids.Add(id)) throw new UIMS.Services.FirebaseException(400, "Question IDs must be unique.");
                string type = Text(q, "type"), prompt = Text(q, "prompt").Trim(); decimal points;
                if (prompt.Length == 0 || prompt.Length > 4000 || !decimal.TryParse(Text(q, "points"), NumberStyles.Number, CultureInfo.InvariantCulture, out points) || points <= 0 || points > 1000)
                    throw new UIMS.Services.FirebaseException(400, "Each question requires a prompt and positive points.");
                JObject question = new JObject { ["id"] = id, ["type"] = type, ["prompt"] = prompt, ["points"] = points, ["order"] = order.Count };
                var imageId = Text(q, "imageFileId"); if (imageId.Length > 0) question["imageFileId"] = imageId;
                if (type == "multipleChoice")
                {
                    var options = q["options"] as JArray; var answer = Text(q, "correctOptionId");
                    if (options == null || options.Count < 2 || options.Count > 8 || !options.OfType<JObject>().Any(o => Text(o, "id") == answer)) throw new UIMS.Services.FirebaseException(400, "Multiple-choice questions need 2 to 8 options and a valid correct option.");
                    var safeOptions = new JArray(); var safeIds = new HashSet<string>(StringComparer.Ordinal); var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var optionToken in options)
                    { var option = optionToken as JObject; string oid = option == null ? "" : Text(option, "id"); string label = option == null ? "" : Text(option, "text").Trim(); if (oid.Length == 0 || label.Length == 0 || !safeIds.Add(oid)) throw new UIMS.Services.FirebaseException(400, "Answer options must have unique IDs and non-empty text."); safeOptions.Add(new JObject { ["id"] = oid, ["text"] = label }); }
                    question["options"] = safeOptions; keys[id] = new JObject { ["correctOptionId"] = answer, ["points"] = points };
                    foreach (var option in safeOptions) if (!labels.Add(Text(option, "text"))) throw new UIMS.Services.FirebaseException(400, "Answer option text must be unique.");
                }
                else if (type == "trueFalse")
                { bool answer; if (!bool.TryParse(Text(q, "correctAnswer"), out answer)) throw new UIMS.Services.FirebaseException(400, "True/false questions require a boolean answer."); keys[id] = new JObject { ["correctAnswer"] = answer, ["points"] = points }; }
                else throw new UIMS.Services.FirebaseException(400, "Question type must be multipleChoice or trueFalse.");
                total += points; order.Add(id); output[id] = question;
            }
            return new JObject { ["title"] = title, ["instructions"] = Text(input, "instructions"), ["opensAt"] = opens.ToUniversalTime().ToString("o"), ["closesAt"] = closes.ToUniversalTime().ToString("o"), ["durationMinutes"] = duration, ["attemptLimit"] = attemptLimit, ["totalPoints"] = total, ["questionOrder"] = order, ["questions"] = output, ["answerKeys"] = keys };
        }
        public async Task<JObject> PublishAsync(string examId, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); var node = await ReadAsync(ExamPath(examId), cancellation).ConfigureAwait(false); if (node.Data == null) throw new UIMS.Services.FirebaseException(404, "Exam was not found.");
            await EnsureExamCompanyAsync(node.Data, cancellation).ConfigureAwait(false);
            if (Text(node.Data, "status") == "published") return new JObject { ["id"] = examId, ["status"] = "published", ["version"] = node.Data["publishedVersion"], ["duplicate"] = true };
            var draft = node.Data["draft"] as JObject; if (draft == null) throw new UIMS.Services.FirebaseException(409, "Save a valid draft before publishing.");
            foreach (var question in ((JObject)draft["questions"]).Properties())
            {
                string imageId = Text(question.Value, "imageFileId"); if (imageId.Length == 0) continue;
                ValidateKey(imageId);
                var metadata = await ReadTokenAsync("fileAttachments/" + imageId, cancellation).ConfigureAwait(false) as JObject;
                if (metadata == null || Text(metadata, "status") != "ready" || Text(metadata, "purpose") != "examQuestionImage" || Text(metadata, "examId") != examId || Text(metadata, "questionId") != question.Name)
                    throw new UIMS.Services.FirebaseException(409, "A question image is incomplete or belongs to another exam/question.");
            }
            int version = (int?)(node.Data["publishedVersion"]) ?? 0; version++;
            var versionRecord = (JObject)draft.DeepClone(); versionRecord["version"] = version; versionRecord["publishedAt"] = UtcNow();
            var keys = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/draftVersions/" + (int?)node.Data["draftRevision"], cancellation).ConfigureAwait(false) as JObject;
            if (keys == null || keys.Properties().Count() != ((JObject)draft["questions"]).Properties().Count()) throw new UIMS.Services.FirebaseException(409, "Draft answer keys are incomplete. Save the draft again before publishing.");
            // Write immutable keys and version first. The published pointer is the commit point; an interrupted publish leaves unreachable data only.
            await WriteImmutableAsync("taskExamAnswerKeys/" + examId + "/" + version, keys, cancellation).ConfigureAwait(false);
            await WriteImmutableAsync(ExamPath(examId) + "/versions/" + version, versionRecord, cancellation).ConfigureAwait(false);
            // The child version write changes the parent's ETag. Re-read before the conditional PUT,
            // preserve every existing field/version and refuse to publish a concurrently edited draft.
            var commit = await ReadAsync(ExamPath(examId), cancellation).ConfigureAwait(false);
            if (commit.Data == null) throw new UIMS.Services.FirebaseException(409, "The exam changed during publication. Retry after reloading.");
            if (Text(commit.Data, "status") == "published" && (int?)commit.Data["publishedVersion"] == version)
                return new JObject { ["id"] = examId, ["status"] = "published", ["version"] = version, ["duplicate"] = true };
            if (Text(commit.Data, "status") != "draft" || !JToken.DeepEquals(commit.Data["draftRevision"], node.Data["draftRevision"]) || !JToken.DeepEquals(commit.Data["draft"], draft))
                throw new UIMS.Services.FirebaseException(409, "The draft changed during publication. Reload the examination before publishing.");
            commit.Data["status"] = "published"; commit.Data["publishedVersion"] = version; commit.Data["publishedAt"] = versionRecord["publishedAt"];
            await WriteAsync(ExamPath(examId), commit.Data, commit.ETag, false, cancellation).ConfigureAwait(false);
            return new JObject { ["id"] = examId, ["status"] = "published", ["version"] = version };
        }
        public async Task<JObject> GetStudentExamAsync(string examId, CancellationToken cancellation)
        {
            Ensure(false, "student"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            if (Text(exam, "status") != "published" || !await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false)) throw new UIMS.Services.FirebaseException(404, "Published exam was not found for this placement.");
            int version = (int?)exam["publishedVersion"] ?? 0; if (version < 1) throw new UIMS.Services.FirebaseException(404, "Published exam version was not found.");
            var publicVersion = await ReadTokenAsync(ExamPath(examId) + "/versions/" + version, cancellation).ConfigureAwait(false) as JObject;
            if (publicVersion == null) throw new UIMS.Services.FirebaseException(404, "Published exam version was not found.");
            await RequireActiveAttemptAsync(examId, version, cancellation).ConfigureAwait(false);
            return PublicExam(new JObject { ["id"] = examId, ["taskId"] = exam["taskId"], ["placementId"] = exam["placementId"], ["status"] = "published", ["publishedVersion"] = version, ["versionData"] = publicVersion }, false);
        }
        public async Task<JObject> GetCompanyExamAsync(string examId, CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false); await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            var visible = PublicExam(exam, true);
            if (Text(exam, "status") == "draft")
            {
                int revision = (int?)exam["draftRevision"] ?? 1;
                var keys = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/draftVersions/" + revision, cancellation).ConfigureAwait(false) as JObject;
                var questions = visible["draft"]?["questions"] as JObject;
                if (keys != null && questions != null) foreach (var key in keys.Properties()) if (questions[key.Name] is JObject) foreach (var field in ((JObject)key.Value).Properties()) ((JObject)questions[key.Name])[field.Name] = field.Value.DeepClone();
            }
            else if (Text(exam, "status") == "published")
            {
                string version = Text(exam, "publishedVersion");
                var keys = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/" + version, cancellation).ConfigureAwait(false) as JObject;
                var questions = visible["draft"]?["questions"] as JObject;
                if (keys != null && questions != null) foreach (var key in keys.Properties()) if (questions[key.Name] is JObject) foreach (var field in ((JObject)key.Value).Properties()) ((JObject)questions[key.Name])[field.Name] = field.Value.DeepClone();
            }
            return visible;
        }
        public async Task<JObject> StartAttemptAsync(string examId, CancellationToken cancellation)
        {
            Ensure(true, "student"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            if (Text(exam, "status") != "published" || !await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false)) throw new UIMS.Services.FirebaseException(404, "Published exam was not found for this placement.");
            int version = (int?)exam["publishedVersion"] ?? 0; var published = await ReadTokenAsync(ExamPath(examId) + "/versions/" + version, cancellation).ConfigureAwait(false) as JObject;
            if (published == null) throw new UIMS.Services.FirebaseException(404, "Published exam version was not found.");
            published = StudentAvailability(exam, published, actor.StudentId);
            var now = DateTimeOffset.UtcNow; var opensAt = ParseDate(published["opensAt"]); var closesAt = ParseDate(published["closesAt"]);
            if (now < opensAt || now >= closesAt) throw new UIMS.Services.FirebaseException(409, "This exam is outside its availability window (server time " + now.ToString("o") + "; opens " + opensAt.ToString("o") + "; closes " + closesAt.ToString("o") + ").");
            string branch = "taskExamAttempts/" + examId + "/" + actor.StudentId;
            for (int retry = 0; retry < 3; retry++)
            {
                var attempts = await ReadAsync(branch, cancellation).ConfigureAwait(false); var items = attempts.Data ?? new JObject();
                now = DateTimeOffset.UtcNow;
                if (now >= closesAt) throw new UIMS.Services.FirebaseException(409, "This exam has closed.");
                bool expired = false;
                foreach (var existing in items.Properties())
                    if (existing.Value is JObject && Text(existing.Value, "status") == "inProgress" && Text(existing.Value, "version") == version.ToString(CultureInfo.InvariantCulture))
                    {
                        if (DateTimeOffset.UtcNow < ParseDate(existing.Value["deadlineAt"])) { await UpdateLinkedTaskAsync(exam, "inprogress", existing.Name, cancellation).ConfigureAwait(false); return StudentAttemptReceipt((JObject)existing.Value, true); }
                        await SaveAnswersAsync(examId, existing.Name, null, true, cancellation).ConfigureAwait(false); expired = true;
                    }
                if (expired) continue;
                int count = items.Properties().Count(p => p.Value is JObject);
                int limit = (int?)published["attemptLimit"] ?? 1; if (count >= limit) throw new UIMS.Services.FirebaseException(409, "The attempt limit has been reached.");
                string attemptId = "AT-" + Guid.NewGuid().ToString("N");
                var deadline = now.AddMinutes((int?)published["durationMinutes"] ?? 1);
                var closeAt = ParseDate(published["closesAt"]); if (closeAt < deadline) deadline = closeAt;
                var attempt = new JObject { ["id"] = attemptId, ["examId"] = examId, ["taskId"] = exam["taskId"], ["placementId"] = exam["placementId"], ["studentId"] = actor.StudentId,
                    ["version"] = version, ["status"] = "inProgress", ["startedAt"] = now.ToString("o"), ["deadlineAt"] = deadline.ToString("o"), ["answers"] = new JObject() };
                items[attemptId] = attempt;
                try { await WriteAsync(branch, items, attempts.ETag ?? "null_etag", false, cancellation).ConfigureAwait(false); await UpdateLinkedTaskAsync(exam, "inprogress", attemptId, cancellation).ConfigureAwait(false); return StudentAttemptReceipt(attempt, true); }
                catch (UIMS.Services.FirebaseException ex) { if (ex.StatusCode != 409 || retry == 2) throw; }
            }
            throw new UIMS.Services.FirebaseException(409, "Unable to reserve an exam attempt. Refresh and retry.");
        }
        public async Task<JObject> SaveAnswersAsync(string examId, string attemptId, JObject answers, bool submit, CancellationToken cancellation)
        {
            ValidateKey(examId); ValidateKey(attemptId);
            Ensure(true, "student"); string path = "taskExamAttempts/" + examId + "/" + actor.StudentId + "/" + attemptId;
            var node = await ReadAsync(path, cancellation).ConfigureAwait(false); if (node.Data == null) throw new UIMS.Services.FirebaseException(404, "Exam attempt was not found.");
            if (Text(node.Data, "studentId") != actor.StudentId || Text(node.Data, "examId") != examId) throw new UIMS.Services.FirebaseException(403, "This attempt does not belong to the verified student.");
            var authorizedExam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            if (!await IsStudentExamAuthorizedAsync(authorizedExam, cancellation).ConfigureAwait(false)) throw new UIMS.Services.FirebaseException(403, "This exam is outside your current placement.");
            if (Text(node.Data, "status") == "submitted" || Text(node.Data, "status") == "expired") { string resultState = Text(node.Data, "resultStatus"); await UpdateLinkedTaskAsync(authorizedExam, resultState == "released" || resultState == "approvedHidden" ? "completed" : resultState == "revisionRequested" ? "revision" : "submitted", attemptId, cancellation).ConfigureAwait(false); return StudentAttemptReceipt(node.Data); }
            DateTimeOffset deadline = ParseDate(node.Data["deadlineAt"]);
            if (DateTimeOffset.UtcNow >= deadline) { submit = true; answers = null; }
            int version = (int?)node.Data["version"] ?? 0;
            var published = await ReadTokenAsync(ExamPath(examId) + "/versions/" + version, cancellation).ConfigureAwait(false) as JObject;
            if (published != null) published = StudentAvailability(authorizedExam, published, actor.StudentId);
            var closing = ParseDate(published?["closesAt"]); if (closing < deadline) deadline = closing;
            if (DateTimeOffset.UtcNow >= deadline) { submit = true; answers = null; }
            var storedAnswers = submit ? ValidateAnswers(published, answers ?? node.Data["answers"] as JObject) : ValidateAnswers(published, answers);
            if (!submit)
            {
                node.Data["answers"] = storedAnswers;
                return StudentAttemptReceipt(await WriteAsync(path, node.Data, node.ETag, false, cancellation).ConfigureAwait(false));
            }
            var keys = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/" + version, cancellation).ConfigureAwait(false) as JObject;
            if (keys == null) throw new UIMS.Services.FirebaseException(503, "Exam answer key is unavailable to the grading service.");
            decimal score = Grade(storedAnswers, keys), maximum = (decimal?)published["totalPoints"] ?? 0m;
            node.Data["answers"] = storedAnswers; // Calculated grades live beside private answer keys, never on student-readable attempts.
            string resultId = Guid.NewGuid().ToString("N");
            await WriteAsync("taskExamAnswerKeys/" + examId + "/results/" + actor.StudentId + "/" + attemptId + "/" + resultId,
                new JObject { ["score"] = score, ["maximumScore"] = maximum, ["calculatedAt"] = UtcNow() }, "null_etag", false, cancellation).ConfigureAwait(false);
            node.Data["calculatedResultId"] = resultId;
            node.Data["resultStatus"] = "pendingApproval"; node.Data["gradedAt"] = UtcNow(); node.Data["submittedAt"] = UtcNow(); node.Data["status"] = DateTimeOffset.UtcNow >= deadline ? "expired" : "submitted";
            try { var savedAttempt = await WriteAsync(path, node.Data, node.ETag, false, cancellation).ConfigureAwait(false); await UpdateLinkedTaskAsync(authorizedExam, "submitted", attemptId, cancellation).ConfigureAwait(false); return StudentAttemptReceipt(savedAttempt); }
            catch (UIMS.Services.FirebaseException ex) { if (ex.StatusCode == 409) { var latest = await ReadAsync(path, cancellation).ConfigureAwait(false); if (latest.Data != null && Text(latest.Data, "status") != "inProgress") return StudentAttemptReceipt(latest.Data); } throw; }
        }
        private static JObject ValidateAnswers(JObject exam, JObject answers)
        {
            if (exam == null) throw new UIMS.Services.FirebaseException(503, "Published exam version is unavailable.");
            var clean = new JObject(); if (answers == null) return clean;
            var questions = exam["questions"] as JObject;
            foreach (var pair in answers.Properties())
            {
                var question = questions == null ? null : questions[pair.Name] as JObject; if (question == null) throw new UIMS.Services.FirebaseException(400, "An answer references a question outside this exam version.");
                string type = Text(question, "type");
                if (type == "multipleChoice") { string option = pair.Value.Type == JTokenType.String ? pair.Value.ToString() : Text(pair.Value, "optionId"); var options = question["options"] as JArray; if (options == null || !options.OfType<JObject>().Any(o => Text(o, "id") == option)) throw new UIMS.Services.FirebaseException(400, "An answer references an invalid option."); clean[pair.Name] = option; }
                else if (type == "trueFalse") { bool value; var raw = pair.Value.Type == JTokenType.Boolean ? pair.Value.ToString() : Text(pair.Value, "value"); if (!bool.TryParse(raw, out value)) throw new UIMS.Services.FirebaseException(400, "True/false answers must be boolean."); clean[pair.Name] = value; }
                else throw new UIMS.Services.FirebaseException(400, "Unsupported question type in published exam.");
            }
            return clean;
        }
        private static decimal Grade(JObject answers, JObject keys)
        {
            decimal score = 0; foreach (var pair in keys.Properties())
            {
                JToken answer; if (!answers.TryGetValue(pair.Name, out answer)) continue;
                string option = Text(pair.Value, "correctOptionId"); bool? correctBoolean = pair.Value["correctAnswer"] == null ? (bool?)null : (bool?)pair.Value["correctAnswer"];
                bool correct = option.Length > 0 ? answer.Type == JTokenType.String && answer.ToString() == option : correctBoolean.HasValue && answer.Type == JTokenType.Boolean && (bool)answer == correctBoolean.Value;
                if (correct) score += (decimal?)pair.Value["points"] ?? 0m;
            }
            return score;
        }
        private static JObject StudentAttemptReceipt(JObject attempt, bool includeAnswers = false)
        {
            var result = new JObject();
            foreach (var field in new[] { "id", "examId", "version", "status", "startedAt", "deadlineAt", "submittedAt", "resultStatus", "revisionReason", "revisionDeadline" })
                if (attempt[field] != null) result[field] = attempt[field].DeepClone();
            result["serverNow"] = UtcNow();
            if (Text(attempt, "resultStatus") == "released")
                foreach (var field in new[] { "score", "maximumScore", "releasedAt", "feedback" }) if (attempt[field] != null) result[field] = attempt[field].DeepClone();
            if (includeAnswers) result["answers"] = attempt["answers"] == null ? new JObject() : attempt["answers"].DeepClone();
            return result;
        }
        private async Task RequireActiveAttemptAsync(string examId, int version, CancellationToken cancellation)
        {
            var published = await ReadTokenAsync(ExamPath(examId) + "/versions/" + version, cancellation).ConfigureAwait(false) as JObject;
            if (published != null) published = StudentAvailability(await ReadExamAsync(examId, cancellation).ConfigureAwait(false), published, actor.StudentId);
            var now = DateTimeOffset.UtcNow;
            if (published == null || now < ParseDate(published["opensAt"]) || now >= ParseDate(published["closesAt"]))
                throw new UIMS.Services.FirebaseException(403, "Exam questions are unavailable outside the examination window.");
            var attempts = await ReadTokenAsync("taskExamAttempts/" + examId + "/" + actor.StudentId, cancellation).ConfigureAwait(false) as JObject;
            if (attempts == null || !attempts.Properties().Any(p => p.Value is JObject && Text(p.Value, "status") == "inProgress" &&
                (int?)p.Value["version"] == version && DateTimeOffset.UtcNow < ParseDate(p.Value["deadlineAt"])))
                throw new UIMS.Services.FirebaseException(403, "Start an eligible attempt before viewing exam questions.");
        }
        public async Task<JObject> ReleaseResultAsync(string examId, string studentId, string attemptId, string feedback, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor");
            if (feedback == null || feedback.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Feedback must be 3000 characters or fewer.");
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(attemptId) || studentId.IndexOfAny("/.#$[]".ToCharArray()) >= 0 || attemptId.IndexOfAny("/.#$[]".ToCharArray()) >= 0)
                throw new UIMS.Services.FirebaseException(400, "A valid student and attempt are required.");
            var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            var placement = await RequirePlacementAsync(Text(exam, "placementId"), cancellation).ConfigureAwait(false);
            if (!Matches(placement, studentId, "studentId", "studentProfileId")) throw new UIMS.Services.FirebaseException(403, "Student is outside this exam placement.");
            string path = "taskExamAttempts/" + examId + "/" + studentId + "/" + attemptId;
            var node = await ReadAsync(path, cancellation).ConfigureAwait(false);
            if (node.Data == null || Text(node.Data, "studentId") != studentId || Text(node.Data, "examId") != examId ||
                !(Text(node.Data, "status") == "submitted" || Text(node.Data, "status") == "expired"))
                throw new UIMS.Services.FirebaseException(409, "Only a completed authorized attempt can be released.");
            if (Text(node.Data, "resultStatus") == "released") { await UpdateLinkedTaskAsync(exam, "completed", attemptId, cancellation).ConfigureAwait(false); return StudentAttemptReceipt(node.Data); }
            if (Text(node.Data, "resultStatus") == "revisionRequested") throw new UIMS.Services.FirebaseException(409, "This attempt was returned for revision. Review the new submission instead.");
            var calculated = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/results/" + studentId + "/" + attemptId + "/" + Text(node.Data, "calculatedResultId"), cancellation).ConfigureAwait(false) as JObject;
            // Legacy completed attempts retain their original calculated grade.
            if (calculated == null && node.Data["score"] != null) calculated = new JObject { ["score"] = node.Data["score"], ["maximumScore"] = node.Data["maximumScore"] };
            if (calculated == null) throw new UIMS.Services.FirebaseException(409, "Calculated result is unavailable.");
            node.Data["score"] = calculated["score"]; node.Data["maximumScore"] = calculated["maximumScore"];
            node.Data["resultStatus"] = "released"; node.Data["releasedAt"] = UtcNow(); node.Data["releasedBy"] = actor.Uid; node.Data["feedback"] = feedback.Trim();
            RecordResultVisibility(node.Data, "released");
            await WriteAsync(path, node.Data, node.ETag, false, cancellation).ConfigureAwait(false);
            await UpdateLinkedTaskAsync(exam, "completed", attemptId, cancellation).ConfigureAwait(false);
            return StudentAttemptReceipt(node.Data);
        }
        private static JObject StudentAvailability(JObject exam, JObject published, string studentId)
        {
            var copy = (JObject)published.DeepClone();
            var grants = exam["retakeAuthorizations"]?[studentId] as JObject;
            if (grants == null || grants.Count == 0) return copy;
            var latest = grants.Properties().Where(p => p.Value is JObject).OrderByDescending(p => ParseDate(p.Value["requestedAt"])).FirstOrDefault();
            if (latest == null) return copy;
            copy["opensAt"] = latest.Value["requestedAt"]?.DeepClone(); copy["closesAt"] = latest.Value["deadline"]?.DeepClone();
            copy["attemptLimit"] = ((int?)published["attemptLimit"] ?? 1) + grants.Count;
            return copy;
        }
        public async Task<JObject> RequestExamRevisionAsync(string examId, string studentId, string attemptId, string reason, string deadline, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); ValidateKey(studentId); ValidateKey(attemptId);
            if (string.IsNullOrWhiteSpace(reason) || reason.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Enter a revision reason up to 3000 characters.");
            DateTimeOffset due;
            if (!DateTimeOffset.TryParse(deadline, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out due) || due <= DateTimeOffset.UtcNow) throw new UIMS.Services.FirebaseException(400, "Set a new future examination deadline.");
            var node = await ReadAsync(ExamPath(examId), cancellation).ConfigureAwait(false); var exam = node.Data;
            if (exam == null) throw new UIMS.Services.FirebaseException(404, "Exam not found.");
            await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            var placement = await RequirePlacementAsync(Text(exam, "placementId"), cancellation).ConfigureAwait(false);
            if (!Matches(placement, studentId, "studentId", "studentProfileId")) throw new UIMS.Services.FirebaseException(403, "Student is outside this exam placement.");
            var attemptNode = await ReadAsync("taskExamAttempts/" + examId + "/" + studentId + "/" + attemptId, cancellation).ConfigureAwait(false);
            if (attemptNode.Data == null || !(Text(attemptNode.Data, "status") == "submitted" || Text(attemptNode.Data, "status") == "expired")) throw new UIMS.Services.FirebaseException(409, "Only a submitted attempt can be returned for revision.");
            var all = exam["retakeAuthorizations"] as JObject ?? new JObject(); var grants = all[studentId] as JObject ?? new JObject();
            if (grants[attemptId] != null && Text(attemptNode.Data, "reviewStatus") == "revisionRequested") return new JObject { ["success"] = true, ["revision"] = grants[attemptId].DeepClone(), ["duplicate"] = true };
            var attempts = await ReadTokenAsync("taskExamAttempts/" + examId + "/" + studentId, cancellation).ConfigureAwait(false) as JObject;
            if (attempts != null && attempts.Properties().Any(p => p.Name != attemptId && p.Value is JObject && ParseDate(p.Value["startedAt"]) > ParseDate(attemptNode.Data["startedAt"])))
                throw new UIMS.Services.FirebaseException(409, "Review the student's latest attempt before requesting another revision.");
            if (grants[attemptId] == null)
            {
                grants[attemptId] = new JObject { ["originalAttemptId"] = attemptId, ["reason"] = reason.Trim(), ["deadline"] = due.ToUniversalTime().ToString("o"), ["requestedAt"] = UtcNow(), ["requestedBy"] = actor.Uid };
                all[studentId] = grants; exam["retakeAuthorizations"] = all;
                await WriteAsync(ExamPath(examId), exam, node.ETag, false, cancellation).ConfigureAwait(false);
            }
            attemptNode.Data["reviewStatus"] = "revisionRequested"; attemptNode.Data["resultStatus"] = "revisionRequested"; RecordResultVisibility(attemptNode.Data, "revisionRequested");
            attemptNode.Data["revisionReason"] = grants[attemptId]["reason"]?.DeepClone(); attemptNode.Data["revisionDeadline"] = grants[attemptId]["deadline"]?.DeepClone();
            await WriteAsync("taskExamAttempts/" + examId + "/" + studentId + "/" + attemptId, attemptNode.Data, attemptNode.ETag, false, cancellation).ConfigureAwait(false);
            await UpdateLinkedTaskAsync(exam, "revision", attemptId, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true, ["revision"] = grants[attemptId].DeepClone() };
        }
        private async Task UpdateLinkedTaskAsync(JObject exam, string state, string attemptId, CancellationToken cancellation)
        {
            string path = "tasks/" + Text(exam, "taskId");
            for (int retry = 0; retry < 3; retry++)
            {
                var node = await ReadAsync(path, cancellation).ConfigureAwait(false); if (node.Data == null) return;
                var task = node.Data;
                if (Text(task, "placementId") != Text(exam, "placementId")) throw new UIMS.Services.FirebaseException(403, "The examination task placement changed.");
                var history = task["examWorkflowHistory"] as JObject ?? new JObject(); string eventId = attemptId + "-" + state;
                if (history[eventId] != null) return;
                history[eventId] = new JObject { ["state"] = state, ["at"] = UtcNow(), ["attemptId"] = attemptId, ["examId"] = exam["id"], ["by"] = actor.Uid };
                task["examWorkflowHistory"] = history; task["examWorkflowStatus"] = state;
                if (task["score"] == null && Text(task, "archivedAt").Length == 0) task["status"] = state == "completed" ? "Completed" : state == "revision" ? "Revision Needed" : state == "submitted" ? "Submitted" : "In Progress";
                var notifications = task["notifications"] as JObject ?? new JObject();
                notifications[eventId] = new JObject { ["message"] = "Examination status: " + state, ["recipientRole"] = actor.Role == "student" ? "companySupervisor" : "student", ["createdAt"] = UtcNow() }; task["notifications"] = notifications;
                try { await WriteAsync(path, task, node.ETag, false, cancellation).ConfigureAwait(false); return; }
                catch (UIMS.Services.FirebaseException error) when (error.StatusCode == 409 && retry < 2) { }
            }
        }
        private void RecordResultVisibility(JObject attempt, string action)
        {
            var history = attempt["reviewHistory"] as JObject ?? new JObject();
            history[Guid.NewGuid().ToString("N")] = new JObject { ["action"] = action, ["at"] = UtcNow(), ["by"] = actor.Uid, ["score"] = attempt["score"]?.DeepClone(), ["maximumScore"] = attempt["maximumScore"]?.DeepClone() };
            attempt["reviewHistory"] = history;
        }
        public async Task<JObject> HideResultAsync(string examId, string studentId, string attemptId, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); ValidateKey(studentId); ValidateKey(attemptId);
            var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false); await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            var placement = await RequirePlacementAsync(Text(exam, "placementId"), cancellation).ConfigureAwait(false);
            if (!Matches(placement, studentId, "studentId", "studentProfileId")) throw new UIMS.Services.FirebaseException(403, "Student is outside this exam placement.");
            string path = "taskExamAttempts/" + examId + "/" + studentId + "/" + attemptId;
            var node = await ReadAsync(path, cancellation).ConfigureAwait(false);
            if (node.Data == null || Text(node.Data, "studentId") != studentId || Text(node.Data, "examId") != examId || !(Text(node.Data, "status") == "submitted" || Text(node.Data, "status") == "expired")) throw new UIMS.Services.FirebaseException(409, "Only a completed attempt result can be hidden.");
            if (Text(node.Data, "resultStatus") == "approvedHidden") return StudentAttemptReceipt(node.Data);
            if (Text(node.Data, "resultStatus") != "released") throw new UIMS.Services.FirebaseException(409, "This result is already hidden pending review.");
            node.Data["resultStatus"] = "approvedHidden"; node.Data["hiddenAt"] = UtcNow(); node.Data["hiddenBy"] = actor.Uid; RecordResultVisibility(node.Data, "hidden");
            await WriteAsync(path, node.Data, node.ETag, false, cancellation).ConfigureAwait(false);
            return StudentAttemptReceipt(node.Data);
        }
        public async Task<JObject> GetMyAttemptAsync(string examId, string attemptId, CancellationToken cancellation)
        {
            Ensure(false, "student"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            if (!await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false)) throw new UIMS.Services.FirebaseException(404, "Exam attempt was not found.");
            var attempt = await ReadTokenAsync("taskExamAttempts/" + examId + "/" + actor.StudentId + "/" + attemptId, cancellation).ConfigureAwait(false) as JObject;
            if (attempt == null || Text(attempt, "studentId") != actor.StudentId) throw new UIMS.Services.FirebaseException(404, "Exam attempt was not found.");
            if (Text(attempt, "status") == "inProgress" && DateTimeOffset.UtcNow >= ParseDate(attempt["deadlineAt"]))
            {
                await SaveAnswersAsync(examId, attemptId, null, true, cancellation).ConfigureAwait(false);
                attempt = await ReadTokenAsync("taskExamAttempts/" + examId + "/" + actor.StudentId + "/" + attemptId, cancellation).ConfigureAwait(false) as JObject;
            }
            return StudentAttemptReceipt(attempt, true);
        }
        public async Task<JObject> ListMyAttemptsAsync(string examId, CancellationToken cancellation)
        {
            Ensure(false, "student"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false);
            if (!await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false)) throw new UIMS.Services.FirebaseException(404, "Exam was not found for this placement.");
            var items = await ReadTokenAsync("taskExamAttempts/" + examId + "/" + actor.StudentId, cancellation).ConfigureAwait(false) as JObject;
            var output = new JObject(); if (items == null) return output;
            foreach (var pair in items.Properties()) if (pair.Value is JObject) output[pair.Name] = await GetMyAttemptAsync(examId, pair.Name, cancellation).ConfigureAwait(false);
            return output;
        }
        public async Task<JObject> ListAttemptsAsync(string examId, CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false); await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            var attempts = await ReadTokenAsync("taskExamAttempts/" + examId, cancellation).ConfigureAwait(false) as JObject ?? new JObject();
            foreach (var student in attempts.Properties()) foreach (var item in (student.Value as JObject ?? new JObject()).Properties())
            {
                var attempt = item.Value as JObject; if (attempt == null || Text(attempt, "status") == "inProgress") continue;
                var grade = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/results/" + student.Name + "/" + item.Name + "/" + Text(attempt, "calculatedResultId"), cancellation).ConfigureAwait(false) as JObject;
                if (grade != null) { attempt["score"] = grade["score"]; attempt["maximumScore"] = grade["maximumScore"]; }
                attempt["answerKey"] = await ReadTokenAsync("taskExamAnswerKeys/" + examId + "/" + Text(attempt, "version"), cancellation).ConfigureAwait(false);
            }
            return attempts;
        }
        public async Task<JObject> UploadQuestionImageAsync(string examId, string questionId, string fileName, string contentType, byte[] bytes, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); var exam = await ReadExamAsync(examId, cancellation).ConfigureAwait(false); await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            if (Text(exam, "status") != "draft") throw new UIMS.Services.FirebaseException(409, "Question images can only be added to a draft exam.");
            var questions = exam["draft"]?["questions"] as JObject;
            if (string.IsNullOrWhiteSpace(questionId) || questions == null || questions[questionId] == null) throw new UIMS.Services.FirebaseException(404, "The image must be linked to an existing draft question.");
            string signature = UIMS.Services.FirebaseFileStorageService.ValidateFile(fileName, contentType, bytes);
            if (!(signature == "image/jpeg" || signature == "image/png")) throw new UIMS.Services.FirebaseException(415, "Exam question images must be PNG or JPEG files.");
            string fileId = UIMS.Services.FirebaseFileStorageService.CreateFileId(); string objectPath = "uims/exam-question-images/" + Text(exam, "placementId") + "/" + examId + "/" + fileId;
            var stored = await Storage.UploadAsync(objectPath, fileName, signature, bytes, cancellation).ConfigureAwait(false);
            var metadata = new JObject { ["id"] = fileId, ["purpose"] = "examQuestionImage", ["status"] = "ready", ["examId"] = examId, ["questionId"] = questionId,
                ["taskId"] = exam["taskId"], ["placementId"] = exam["placementId"], ["ownerUid"] = actor.Uid, ["ownerProfileId"] = actor.CompanySupervisorId,
                ["fileName"] = stored.FileName, ["contentType"] = stored.ContentType, ["sizeBytes"] = stored.SizeBytes, ["sha256"] = stored.Sha256,
                ["storagePath"] = stored.ObjectPath, ["storageGeneration"] = stored.Generation, ["createdAt"] = UtcNow() };
            try { await WriteAsync("fileAttachments/" + fileId, metadata, "null_etag", false, cancellation).ConfigureAwait(false); }
            catch { try { await Storage.DeleteObjectAsync(objectPath, cancellation).ConfigureAwait(false); } catch { } throw; }
            return metadata;
        }
        public async Task<UIMS.Tasks.TaskFileDownload> DownloadQuestionImageAsync(string fileId, CancellationToken cancellation)
        {
            Ensure(false); var metadata = await ReadTokenAsync("fileAttachments/" + fileId, cancellation).ConfigureAwait(false) as JObject;
            if (metadata == null || Text(metadata, "purpose") != "examQuestionImage" || Text(metadata, "status") != "ready") throw new UIMS.Services.FirebaseException(404, "Exam image was not found.");
            var exam = await ReadExamAsync(Text(metadata, "examId"), cancellation).ConfigureAwait(false);
            if (actor.Role == "companySupervisor") await EnsureExamCompanyAsync(exam, cancellation).ConfigureAwait(false);
            else if (actor.Role == "student")
            {
                if (Text(exam, "status") != "published" || !await IsStudentExamAuthorizedAsync(exam, cancellation).ConfigureAwait(false)) throw new UIMS.Services.FirebaseException(403, "This exam image is not available to the verified student.");
                int version = (int?)exam["publishedVersion"] ?? 0; await RequireActiveAttemptAsync(Text(metadata, "examId"), version, cancellation).ConfigureAwait(false); var published = await ReadTokenAsync(ExamPath(Text(metadata, "examId")) + "/versions/" + version, cancellation).ConfigureAwait(false) as JObject;
                if (published == null || !published.SelectTokens("questions.*.imageFileId").Any(x => x.ToString() == fileId)) throw new UIMS.Services.FirebaseException(403, "This image is not published for the student's exam.");
            }
            else throw new UIMS.Services.FirebaseException(403, "This image is not available for the verified role.");
            return new UIMS.Tasks.TaskFileDownload { FileName = Text(metadata, "fileName"), ContentType = Text(metadata, "contentType"), Content = await Storage.DownloadAsync(Text(metadata, "storagePath"), cancellation).ConfigureAwait(false) };
        }
        private static string UtcNow() { return DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static void ValidateKey(string value)
        { if (string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.IndexOfAny("/.#$[]\\".ToCharArray()) >= 0 || value.Any(char.IsControl)) throw new UIMS.Services.FirebaseException(400, "Invalid exam record identifier."); }
        private static DateTimeOffset ParseDate(JToken value)
        {
            if (value == null) return DateTimeOffset.MinValue;
            if (value.Type == JTokenType.Date)
            {
                object raw = ((JValue)value).Value;
                if (raw is DateTimeOffset) return ((DateTimeOffset)raw).ToUniversalTime();
                if (raw is DateTime)
                {
                    var date = (DateTime)raw;
                    return date.Kind == DateTimeKind.Local ? new DateTimeOffset(date).ToUniversalTime() : new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc));
                }
            }
            DateTimeOffset result; if (!DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result)) return DateTimeOffset.MinValue; return result.ToUniversalTime();
        }
        private sealed class ExamNode { public JObject Data; public string ETag; }
    }
}
