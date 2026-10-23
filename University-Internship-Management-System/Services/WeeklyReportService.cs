using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UIMS.Tasks
{
    /// <summary>Company-authored weekly reports over the existing Firebase RTDB root and private Storage.</summary>
    public sealed class WeeklyReportService
    {
        private static readonly HttpClient SharedClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 12 }) { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly Lazy<Task<GoogleCredential>> Credential = new Lazy<Task<GoogleCredential>>(() => GoogleCredential.GetApplicationDefaultAsync());
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private readonly Func<string, string> setting;
        private readonly HttpClient client;
        private readonly TaskActor actor;
        private readonly Func<CancellationToken, Task<string>> tokenProvider;
        private readonly UIMS.Services.FirebaseFileStorageService storage;
        private readonly Func<string, string, string, byte[], CancellationToken, Task<UIMS.Services.StoredFile>> upload;
        private readonly Func<string, CancellationToken, Task<byte[]>> download;
        private readonly Func<string, CancellationToken, Task> delete;
        private readonly string root;
        private readonly bool enabled;
        private readonly bool writesEnabled;
        private readonly bool schemaValidated;

        public WeeklyReportService(TaskActor verifiedActor) : this(k => WebConfigurationManager.AppSettings[k], SharedClient, verifiedActor, GetAccessTokenAsync, null) { }

        public WeeklyReportService(Func<string, string> settings, HttpClient httpClient, TaskActor verifiedActor,
            Func<CancellationToken, Task<string>> accessTokenProvider = null, UIMS.Services.FirebaseFileStorageService fileStorage = null,
            Func<string, string, string, byte[], CancellationToken, Task<UIMS.Services.StoredFile>> uploadOverride = null,
            Func<string, CancellationToken, Task<byte[]>> downloadOverride = null,
            Func<string, CancellationToken, Task> deleteOverride = null)
        {
            setting = settings ?? throw new ArgumentNullException(nameof(settings));
            client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            actor = verifiedActor;
            tokenProvider = accessTokenProvider ?? GetAccessTokenAsync;
            storage = fileStorage ?? new UIMS.Services.FirebaseFileStorageService(setting, httpClient);
            upload = uploadOverride ?? storage.UploadAsync;
            download = downloadOverride ?? storage.DownloadAsync;
            delete = deleteOverride ?? storage.DeleteObjectAsync;
            root = (setting("FirebaseDatabaseUrl") ?? "").TrimEnd('/');
            enabled = IsTrue(setting("FirebaseEnabled")) && IsTrue(setting("WeeklyReportsEnabled"));
            writesEnabled = IsTrue(setting("FirebaseLiveWritesEnabled")) && IsTrue(setting("WeeklyReportsLiveWritesEnabled"));
            schemaValidated = IsTrue(setting("FirebaseSchemaValidated")) && IsTrue(setting("WeeklyReportsSchemaValidated"));
        }

        private static bool IsTrue(string value) { return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); }
        private static async Task<string> GetAccessTokenAsync(CancellationToken cancellation)
        {
            return await University_Internship_Management_System.Shared.FirebaseServerCredential.AccessTokenAsync(cancellation).ConfigureAwait(false);
        }
        private void Ensure(bool write, string requiredRole = null)
        {
            // All settings and role checks happen before creating a token or making a network request.
            if (!enabled) throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured for weekly reports. No Firebase or Storage request was sent.");
            if (!IsTrue(setting("FirebaseAuthenticationEnabled"))) throw new UIMS.Services.FirebaseException(503, "Firebase Authentication is not configured for weekly reports.");
            if (!schemaValidated) throw new UIMS.Services.FirebaseException(503, "The weekly report schema is not validated for this environment.");
            Uri uri;
            if (!Uri.TryCreate(root, UriKind.Absolute, out uri) || uri.Scheme != "https" || !uri.IsDefaultPort || uri.AbsolutePath != "/" ||
                !(uri.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(503, "Configure the central HTTPS FirebaseDatabaseUrl in Web.config.");
            if (actor == null || string.IsNullOrWhiteSpace(actor.Uid)) throw new UIMS.Services.FirebaseException(401, "A verified Firebase UID is required.");
            if (requiredRole != null && !string.Equals(actor.Role, requiredRole, StringComparison.Ordinal)) throw new UIMS.Services.FirebaseException(403, "This weekly report operation is not allowed for the signed-in role.");
            bool reviewOnly = write && requiredRole == "academicSupervisor" && actor.Role == "academicSupervisor";
            bool reviewAllowed = reviewOnly && IsTrue(setting("FirebaseLiveWritesEnabled")) && IsTrue(setting("WeeklyReportReviewsEnabled"));
            if (write && !writesEnabled && !reviewAllowed) throw new UIMS.Services.FirebaseException(503, "Weekly report uploads are deferred until Storage is available. No data was changed.");
            if (write && !reviewOnly && (!IsTrue(setting("FirebaseStorageEnabled")) || !IsTrue(setting("FirebaseStorageLiveWritesEnabled")))) throw new UIMS.Services.FirebaseException(503, "Private Firebase Storage writes are disabled. No file was uploaded.");
        }

        private string Url(string path)
        {
            string[] segments = (path ?? "").Split('/').Where(p => p.Length > 0).ToArray();
            if (segments.Length == 0 || segments.Any(p => p == "." || p == ".." || p.Any(c => c == '.' || c == '#' || c == '$' || c == '[' || c == ']' || c == '/' || c == '\\' || char.IsControl(c))))
                throw new UIMS.Services.FirebaseException(400, "Invalid Firebase record path.");
            return root + "/" + string.Join("/", segments.Select(Uri.EscapeDataString)) + ".json";
        }
        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, JToken body, string etag, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            string token;
            try { token = await tokenProvider(cancellation).ConfigureAwait(false); }
            catch (Exception error) when (!(error is OperationCanceledException) && !(error is UIMS.Services.FirebaseException))
            { throw new UIMS.Services.FirebaseException(503, "Trusted server credentials for Firebase Realtime Database are unavailable."); }
            if (string.IsNullOrWhiteSpace(token)) throw new UIMS.Services.FirebaseException(503, "Trusted server credentials did not provide a Firebase access token.");
            var request = new HttpRequestMessage(method, Url(path));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (method == HttpMethod.Get) request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
            if (!string.IsNullOrWhiteSpace(etag)) request.Headers.TryAddWithoutValidation("if-match", etag);
            if (body != null) request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            try { using (request) return await client.SendAsync(request, cancellation).ConfigureAwait(false); }
            catch (HttpRequestException) { request.Dispose(); throw new UIMS.Services.FirebaseException(502, "Firebase Realtime Database could not be reached."); }
        }
        private static async Task EnsureResponseAsync(HttpResponseMessage response, CancellationToken cancellation)
        {
            if (response.IsSuccessStatusCode) return;
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new UIMS.Services.FirebaseException(403, "Firebase denied the weekly report request. Verify trusted credentials and least-privilege database/Storage Rules.");
            if ((int)response.StatusCode == 412) throw new UIMS.Services.FirebaseException(409, "This weekly report changed concurrently. Refresh and retry.");
            if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500) throw new UIMS.Services.FirebaseException(503, "Firebase is temporarily unavailable for weekly reports.");
            throw new UIMS.Services.FirebaseException((int)response.StatusCode, "Firebase rejected the weekly report request.");
        }
        private async Task<Node> ReadAsync(string path, CancellationToken cancellation)
        {
            using (var response = await SendAsync(HttpMethod.Get, path, null, null, cancellation).ConfigureAwait(false))
            {
                await EnsureResponseAsync(response, cancellation).ConfigureAwait(false);
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string etag = response.Headers.ETag == null ? null : response.Headers.ETag.Tag;
                if (etag == null) { IEnumerable<string> values; if (response.Headers.TryGetValues("ETag", out values)) etag = values.FirstOrDefault(); }
                return new Node { Value = string.IsNullOrWhiteSpace(json) || json == "null" ? null : JToken.Parse(json), ETag = etag };
            }
        }
        private async Task<JToken> ReadEqualAsync(string path, string field, string value, CancellationToken cancellation)
        {
            string query = "?orderBy=" + Uri.EscapeDataString(JsonConvert.SerializeObject(field)) + "&equalTo=" + Uri.EscapeDataString(JsonConvert.SerializeObject(value));
            string token = await tokenProvider(cancellation).ConfigureAwait(false);
            using (var request = new HttpRequestMessage(HttpMethod.Get, Url(path) + query))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    // Older configured Rules omit some equality indexes. Keep Rules private:
                    // filter the same collection on the trusted server, never in the browser.
                    if ((int)response.StatusCode == 400 && json.IndexOf("Index not defined", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var source = (await ReadAsync(path, cancellation).ConfigureAwait(false)).Value as JObject ?? new JObject();
                        return new JObject(source.Properties().Where(p => p.Value is JObject && Text(p.Value, field) == value).Select(p => new JProperty(p.Name, p.Value.DeepClone())));
                    }
                    await EnsureResponseAsync(response, cancellation).ConfigureAwait(false);
                    return string.IsNullOrWhiteSpace(json) || json == "null" ? null : JToken.Parse(json);
                }
            }
        }
        private async Task<JObject> WriteAsync(string path, JObject value, string etag, HttpMethod method, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(etag)) throw new UIMS.Services.FirebaseException(503, "A Firebase ETag is required for every weekly report write.");
            using (var response = await SendAsync(method, path, value, etag, cancellation).ConfigureAwait(false))
            {
                await EnsureResponseAsync(response, cancellation).ConfigureAwait(false);
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(json) || json == "null" ? (JObject)value.DeepClone() : JObject.Parse(json);
            }
        }
        private async Task DeleteAsync(string path, string etag, CancellationToken cancellation)
        {
            using (var response = await SendAsync(HttpMethod.Delete, path, null, etag, cancellation).ConfigureAwait(false)) await EnsureResponseAsync(response, cancellation).ConfigureAwait(false);
        }
        private static string Text(JToken value, params string[] fields)
        { foreach (string field in fields) { JToken item = value is JObject ? ((JObject)value)[field] : null; if (item != null && item.Type != JTokenType.Null && item.ToString().Trim().Length > 0) return item.ToString().Trim(); } return ""; }
        private static bool ActivePlacement(JToken placement)
        {
            string status = Text(placement, "status", "placementStatus");
            return status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) || status.Equals("active", StringComparison.OrdinalIgnoreCase) || status.Equals("inProgress", StringComparison.OrdinalIgnoreCase) || status.Equals("ongoing", StringComparison.OrdinalIgnoreCase);
        }
        private static void AddRecords(IDictionary<string, JObject> target, JToken value)
        {
            var obj = value as JObject; if (obj == null) return;
            foreach (var pair in obj.Properties()) if (pair.Value is JObject) target[pair.Name] = (JObject)pair.Value;
        }
        private static string StableReportId(string placementId, int weekNumber)
        {
            using (var sha = SHA256.Create())
            { byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(placementId + "\n" + weekNumber.ToString(CultureInfo.InvariantCulture))); return "WR-" + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(); }
        }
        private static string UtcNow() { return DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture); }
        private static string DateOnly(string value, string field)
        {
            DateTime date;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) throw new UIMS.Services.FirebaseException(400, field + " must use yyyy-MM-dd.");
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        private static int WeekNumber(JToken value)
        {
            int week = (int?)value ?? 0; if (week < 1 || week > 60) throw new UIMS.Services.FirebaseException(400, "Choose a valid training week number (1–60)."); return week;
        }
        private async Task<JObject> RequirePlacementAsync(string placementId, CancellationToken cancellation, bool requireActive = true)
        {
            if (string.IsNullOrWhiteSpace(placementId) || placementId.Length > 160) throw new UIMS.Services.FirebaseException(400, "Choose an active placement.");
            var node = await ReadAsync("placements/" + placementId, cancellation).ConfigureAwait(false); var placement = node.Value as JObject;
            if (placement == null) throw new UIMS.Services.FirebaseException(404, "The placement was not found.");
            if (requireActive && !ActivePlacement(placement)) throw new UIMS.Services.FirebaseException(409, "Weekly report submissions require an active or confirmed placement.");
            if (actor.Role == "companySupervisor")
            {
                string placementCompany = Text(placement, "companyId");
                bool companyMatch = actor.CompanyId.Length > 0 && placementCompany == actor.CompanyId;
                string placementSupervisor = Text(placement, "companySupervisorId", "supervisorId", "assignedSupervisorId");
                bool supervisorMatch = placementSupervisor == actor.CompanySupervisorId;
                if (placementSupervisor.Length > 0 ? !supervisorMatch : !companyMatch) throw new UIMS.Services.FirebaseException(403, "This placement is outside the verified company supervisor's authority.");
                if (placementCompany.Length > 0 && actor.CompanyId.Length > 0 && placementCompany != actor.CompanyId) throw new UIMS.Services.FirebaseException(403, "The placement belongs to another company.");
            }
            return placement;
        }
        private async Task<string> ResolveAcademicSupervisorAsync(string studentId, CancellationToken cancellation)
        {
            var records = new Dictionary<string, JObject>(StringComparer.Ordinal);
            var current = await ReadAsync("supervisorAssignments", cancellation).ConfigureAwait(false);
            if (current.Value is JObject currentRows)
                foreach (var row in currentRows.Properties())
                    if (row.Value is JObject assignment && Text(assignment, "studentId") == studentId && CurrentAcademicAssignment(assignment)) records["current-" + row.Name] = assignment;
            AddRecords(records, await ReadEqualAsync("assignments", "studentId", studentId, cancellation).ConfigureAwait(false));
            var active = records.Values.Where(a => CurrentAcademicAssignment(a) && Text(a, "studentId") == studentId)
                .Select(a => Text(a, "academicSupervisorId")).Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            if (active.Count > 1) throw new UIMS.Services.FirebaseException(409, "The student has conflicting active academic supervisor assignments; coordinator review is required.");
            if (active.Count == 1) return active[0];

            // Existing student profiles also carry academicSupervisorId. Use a scoped equality query, never a root dump.
            var profiles = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(profiles, await ReadEqualAsync("users", "studentId", studentId, cancellation).ConfigureAwait(false));
            var profileIds = profiles.Values.Where(p => Text(p, "role").Equals("student", StringComparison.OrdinalIgnoreCase))
                .Select(p => Text(p, "academicSupervisorId")).Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            if (profileIds.Count > 1) throw new UIMS.Services.FirebaseException(409, "The student profile has conflicting academic supervisor assignments.");
            if (profileIds.Count == 1) return profileIds[0];
            throw new UIMS.Services.FirebaseException(409, "The student has no current academic supervisor assignment.");
        }
        private static bool CurrentAcademicAssignment(JObject assignment)
        {
            return assignment.Value<bool?>("active") != false && assignment.Value<bool?>("isActive") != false &&
                !new[] { "inactive", "ended", "reassigned", "cancelled" }.Contains(Text(assignment, "status").ToLowerInvariant());
        }
        private async Task EnsureCurrentAssignmentAsync(string studentId, string expectedSupervisorId, CancellationToken cancellation)
        {
            string assigned = await ResolveAcademicSupervisorAsync(studentId, cancellation).ConfigureAwait(false);
            if (assigned != expectedSupervisorId) throw new UIMS.Services.FirebaseException(403, "The report is not assigned to the verified academic supervisor under the current coordinator assignment.");
        }
        private async Task EnsureCompanyStudentPlacementAsync(string placementId, string studentId, CancellationToken cancellation, bool requireActive = false)
        {
            var placement = await RequirePlacementAsync(placementId, cancellation, requireActive).ConfigureAwait(false);
            string placedStudent = Text(placement, "studentId", "studentProfileId", "traineeId");
            if (placedStudent.Length == 0 || placedStudent != studentId) throw new UIMS.Services.FirebaseException(403, "The selected student is not the student on this placement.");
        }

        public async Task<JObject> ListCompanyPlacementsAsync(CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            var placements = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (actor.CompanyId.Length > 0) AddRecords(placements, await ReadEqualAsync("placements", "companyId", actor.CompanyId, cancellation).ConfigureAwait(false));
            AddRecords(placements, await ReadEqualAsync("placements", "companySupervisorId", actor.CompanySupervisorId, cancellation).ConfigureAwait(false));
            AddRecords(placements, await ReadEqualAsync("placements", "supervisorId", actor.CompanySupervisorId, cancellation).ConfigureAwait(false));
            var result = new JArray();
            foreach (var pair in placements)
            {
                var placement = pair.Value; string studentId = Text(placement, "studentId", "studentProfileId", "traineeId");
                if (studentId.Length == 0 || !ActivePlacement(placement)) continue;
                try { await EnsureCompanyStudentPlacementAsync(pair.Key, studentId, cancellation).ConfigureAwait(false); }
                catch (UIMS.Services.FirebaseException ex) when (ex.StatusCode == 403 || ex.StatusCode == 404) { continue; }
                var profiles = new Dictionary<string, JObject>(StringComparer.Ordinal);
                AddRecords(profiles, await ReadEqualAsync("users", "studentId", studentId, cancellation).ConfigureAwait(false));
                var profile = profiles.Values.FirstOrDefault(p => Text(p, "role").Equals("student", StringComparison.OrdinalIgnoreCase));
                result.Add(new JObject { ["placementId"] = pair.Key, ["studentId"] = studentId,
                    ["studentName"] = profile == null ? studentId : Text(profile, "fullName", "displayName", "name"),
                    ["companyId"] = Text(placement, "companyId"), ["status"] = Text(placement, "status", "placementStatus"),
                    ["academicSupervisorId"] = await ResolveAcademicSupervisorAsync(studentId, cancellation).ConfigureAwait(false) });
            }
            return new JObject { ["placements"] = result };
        }

        public async Task<JObject> ListCompanyReportsAsync(CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            var records = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (actor.CompanyId.Length > 0) AddRecords(records, await ReadEqualAsync("weeklyReports", "companyId", actor.CompanyId, cancellation).ConfigureAwait(false));
            AddRecords(records, await ReadEqualAsync("weeklyReports", "companySupervisorId", actor.CompanySupervisorId, cancellation).ConfigureAwait(false));
            var result = new JObject();
            foreach (var pair in records)
            {
                string reportCompany = Text(pair.Value, "companyId");
                if (Text(pair.Value, "companySupervisorId") != actor.CompanySupervisorId && (actor.CompanyId.Length == 0 || reportCompany != actor.CompanyId)) continue;
                try { await EnsureCompanyStudentPlacementAsync(Text(pair.Value, "placementId"), Text(pair.Value, "studentId"), cancellation).ConfigureAwait(false); }
                catch (UIMS.Services.FirebaseException) { continue; }
                result[pair.Key] = PublicReport(pair.Value);
            }
            return new JObject { ["reports"] = result };
        }

        public async Task<JObject> ListAcademicReportsAsync(CancellationToken cancellation)
        {
            Ensure(false, "academicSupervisor");
            var records = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(records, await ReadEqualAsync("weeklyReports", "academicSupervisorId", actor.AcademicSupervisorId, cancellation).ConfigureAwait(false));
            // Include historical reports where the student is now assigned to this supervisor, even if the original routing snapshot differs.
            var currentAssignments = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(currentAssignments, await ReadEqualAsync("assignments", "academicSupervisorId", actor.AcademicSupervisorId, cancellation).ConfigureAwait(false));
            var currentRoster = await ReadAsync("supervisorAssignments", cancellation).ConfigureAwait(false);
            if (currentRoster.Value is JObject rosterRows)
                foreach (var row in rosterRows.Properties())
                    if (row.Value is JObject assignment && Text(assignment, "academicSupervisorId") == actor.AcademicSupervisorId && CurrentAcademicAssignment(assignment)) currentAssignments["current-" + row.Name] = assignment;
            var studentIds = currentAssignments.Values.Where(CurrentAcademicAssignment).Select(a => Text(a, "studentId")).Where(s => s.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            // Existing profiles are also an established assignment source; every candidate
            // is rechecked below so reassignment never grants historical access.
            var assignedProfiles = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(assignedProfiles, await ReadEqualAsync("users", "academicSupervisorId", actor.AcademicSupervisorId, cancellation).ConfigureAwait(false));
            studentIds.AddRange(assignedProfiles.Values.Where(p => Text(p, "role") == "student").Select(p => Text(p, "studentId")).Where(id => id.Length > 0 && !studentIds.Contains(id)).Distinct());
            foreach (string studentId in studentIds) AddRecords(records, await ReadEqualAsync("weeklyReports", "studentId", studentId, cancellation).ConfigureAwait(false));
            var assignedStudents = new JArray();
            foreach (string studentId in studentIds)
            {
                try { await EnsureCurrentAssignmentAsync(studentId, actor.AcademicSupervisorId, cancellation).ConfigureAwait(false); }
                catch (UIMS.Services.FirebaseException e) when (e.StatusCode == 403 || e.StatusCode == 409) { continue; }
                var profiles = new Dictionary<string, JObject>(StringComparer.Ordinal);
                AddRecords(profiles, await ReadEqualAsync("users", "studentId", studentId, cancellation).ConfigureAwait(false));
                var placements = new Dictionary<string, JObject>(StringComparer.Ordinal);
                AddRecords(placements, await ReadEqualAsync("placements", "studentId", studentId, cancellation).ConfigureAwait(false));
                AddRecords(placements, await ReadEqualAsync("placements", "studentProfileId", studentId, cancellation).ConfigureAwait(false));
                var studentPlacements = new JArray();
                foreach (var placementPair in placements)
                {
                    var placement = placementPair.Value;
                    if (Text(placement, "studentId", "studentProfileId", "traineeId") != studentId) continue;
                    studentPlacements.Add(new JObject { ["placementId"] = placementPair.Key, ["companyId"] = placement["companyId"],
                        ["companyName"] = placement["companyName"], ["status"] = placement["status"], ["startDate"] = placement["startDate"], ["endDate"] = placement["endDate"] });
                }
                var profile = profiles.Values.FirstOrDefault();
                assignedStudents.Add(new JObject { ["studentId"] = studentId, ["name"] = Text(profile, "fullName", "name", "studentName"),
                    ["email"] = Text(profile, "email", "universityEmail"), ["department"] = Text(profile, "department", "major"),
                    ["placements"] = studentPlacements });
            }
            var result = new JObject();
            foreach (var pair in records)
            {
                try { await EnsureCurrentAssignmentAsync(Text(pair.Value, "studentId"), actor.AcademicSupervisorId, cancellation).ConfigureAwait(false); }
                catch (UIMS.Services.FirebaseException) { continue; }
                result[pair.Key] = PublicReport(pair.Value);
            }
            return new JObject { ["assignedStudents"] = assignedStudents, ["reports"] = result };
        }

        public async Task<JObject> ListStudentReportsAsync(CancellationToken cancellation)
        {
            Ensure(false, "student");
            var records = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(records, await ReadEqualAsync("weeklyReports", "studentId", actor.StudentId, cancellation).ConfigureAwait(false));
            var result = new JObject();
            foreach (var pair in records)
            {
                if (Text(pair.Value, "studentId") != actor.StudentId) continue;
                var placement = await ReadAsync("placements/" + Text(pair.Value, "placementId"), cancellation).ConfigureAwait(false);
                if (!(placement.Value is JObject) || Text(placement.Value, "studentId", "studentProfileId", "traineeId") != actor.StudentId) continue;
                var visible = new JObject { ["reportId"] = pair.Key, ["placementId"] = pair.Value["placementId"], ["weekNumber"] = pair.Value["weekNumber"], ["weekStart"] = pair.Value["weekStart"], ["weekEnd"] = pair.Value["weekEnd"], ["status"] = pair.Value["status"], ["submittedAt"] = pair.Value["submittedAt"], ["currentRevisionId"] = pair.Value["currentRevisionId"] };
                if (Text(pair.Value, "status") == "approved") visible["companyComments"] = pair.Value["companyComments"];
                if (pair.Value["latestApprovedEvaluation"] is JObject) visible["academicEvaluation"] = pair.Value["latestApprovedEvaluation"].DeepClone();
                result[pair.Key] = visible;
            }
            return new JObject { ["reports"] = result };
        }

        public async Task<JObject> GetAcademicReportAsync(string reportId, CancellationToken cancellation)
        {
            Ensure(false, "academicSupervisor"); var node = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); var report = node.Value as JObject;
            if (report == null) throw new UIMS.Services.FirebaseException(404, "Weekly report was not found.");
            await EnsureCurrentAssignmentAsync(Text(report, "studentId"), actor.AcademicSupervisorId, cancellation).ConfigureAwait(false);
            var placement = await ReadAsync("placements/" + Text(report, "placementId"), cancellation).ConfigureAwait(false);
            if (!(placement.Value is JObject) || Text(placement.Value, "studentId", "studentProfileId", "traineeId") != Text(report, "studentId")) throw new UIMS.Services.FirebaseException(403, "The report's student and placement relationship is no longer valid.");
            return new JObject { ["report"] = PublicReport(report), ["reviews"] = await ReadReviewsAsync(reportId, cancellation).ConfigureAwait(false), ["progress"] = await GetAcademicProgressAsync(report, cancellation).ConfigureAwait(false) };
        }

        public async Task<JObject> GetCompanyReportAsync(string reportId, CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor"); var node = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); var report = node.Value as JObject;
            if (report == null) throw new UIMS.Services.FirebaseException(404, "Weekly report was not found.");
            await EnsureCompanyStudentPlacementAsync(Text(report, "placementId"), Text(report, "studentId"), cancellation).ConfigureAwait(false);
            return new JObject { ["report"] = PublicReport(report), ["reviews"] = await ReadReviewsAsync(reportId, cancellation).ConfigureAwait(false) };
        }

        public async Task<JObject> TaskSuggestionsAsync(string placementId, string weekStart, string weekEnd, CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            var placement = await RequirePlacementAsync(placementId, cancellation).ConfigureAwait(false);
            string student = Text(placement, "studentId", "studentProfileId", "traineeId");
            return new JObject { ["tasks"] = await BuildTaskSnapshotAsync(placementId, student, DateOnly(weekStart, "weekStart"), DateOnly(weekEnd, "weekEnd"), null, cancellation).ConfigureAwait(false) };
        }
        private async Task<JArray> BuildTaskSnapshotAsync(string placementId, string studentId, string weekStart, string weekEnd, JArray selection, CancellationToken cancellation)
        {
            var records = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(records, await ReadEqualAsync("tasks", "placementId", placementId, cancellation).ConfigureAwait(false));
            if (selection != null && (selection.Count > 200 || selection.Any(t => t.Type != JTokenType.String))) throw new UIMS.Services.FirebaseException(400, "Select valid task identifiers.");
            var chosen = selection == null ? null : new HashSet<string>(selection.Values<string>(), StringComparer.Ordinal);
            var snapshot = new JArray();
            DateTimeOffset start = DateTimeOffset.Parse(weekStart, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), end = DateTimeOffset.Parse(weekEnd, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).AddDays(1);
            foreach (var pair in records)
            {
                var task = pair.Value;
                if (Text(task, "placementId") != placementId || Text(task, "studentId", "assignedTo", "traineeId") != studentId || Text(task, "assignedBy", "companySupervisorId") != actor.CompanySupervisorId) continue;
                bool suggested = new[] { "assignedAt", "dueDate", "updatedAt" }.Any(field => { DateTimeOffset date; return DateTimeOffset.TryParse(Text(task, field), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date) && date >= start && date < end; });
                if (!suggested) suggested = TaskSubmissionHistory.Read(task["submissions"]).Records.Any(sub => { DateTimeOffset date; return DateTimeOffset.TryParse(Text(sub, "submittedAt"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date) && date >= start && date < end; });
                if (!suggested) suggested = (task["activity"] as JObject ?? new JObject()).Properties().Any(activity => { DateTimeOffset date; return DateTimeOffset.TryParse(Text(activity.Value, "at"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date) && date >= start && date < end; });
                if (chosen != null && !chosen.Remove(pair.Key)) continue;
                var row = new JObject { ["taskId"] = pair.Key, ["studentId"] = studentId, ["placementId"] = placementId, ["suggested"] = suggested, ["capturedAt"] = UtcNow() };
                foreach (var field in new[] { "title", "description", "evaluationCriteria", "instructionVersion", "dueDate", "status", "score", "maximumScore", "feedback", "batchId" }) if (task[field] != null) row[field] = task[field].DeepClone();
                snapshot.Add(row);
            }
            if (chosen != null && chosen.Count > 0) throw new UIMS.Services.FirebaseException(403, "One or more selected tasks are outside this student's authorized placement.");
            return selection == null ? new JArray(snapshot.Where(t => (bool?)t["suggested"] == true)) : snapshot;
        }

        public async Task<JObject> SubmitAsync(string placementId, int weekValue, string weekStartValue, string weekEndValue, string fileName, string contentType, byte[] bytes, string companyComments, CancellationToken cancellation, JArray selectedTaskIds = null)
        {
            Ensure(true, "companySupervisor");
            int week = WeekNumber(new JValue(weekValue)); string weekStart = DateOnly(weekStartValue, "weekStart"), weekEnd = DateOnly(weekEndValue, "weekEnd");
            DateTime start = DateTime.ParseExact(weekStart, "yyyy-MM-dd", CultureInfo.InvariantCulture), end = DateTime.ParseExact(weekEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (end < start || (end - start).TotalDays > 6) throw new UIMS.Services.FirebaseException(400, "The report week must end on or after its start and span no more than seven calendar days.");
            var placement = await RequirePlacementAsync(placementId, cancellation).ConfigureAwait(false);
            string studentId = Text(placement, "studentId", "studentProfileId", "traineeId");
            if (studentId.Length == 0) throw new UIMS.Services.FirebaseException(409, "The active placement has no student relationship.");
            string academicId = await ResolveAcademicSupervisorAsync(studentId, cancellation).ConfigureAwait(false);
            string reportId = StableReportId(placementId, week);
            var existing = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false);
            if (existing.Value != null) throw new UIMS.Services.FirebaseException(409, "A weekly report already exists for this placement and training week. Use the resubmit action only after revision was requested.");
            return await StoreNewOrRevisionAsync(reportId, placementId, null, placement, studentId, academicId, week, weekStart, weekEnd, fileName, contentType, bytes, companyComments, cancellation, selectedTaskIds).ConfigureAwait(false);
        }

        public async Task<JObject> ResubmitAsync(string reportId, string fileName, string contentType, byte[] bytes, string companyComments, CancellationToken cancellation, JArray selectedTaskIds = null)
        {
            Ensure(true, "companySupervisor");
            var reportNode = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); var report = reportNode.Value as JObject;
            if (report == null) throw new UIMS.Services.FirebaseException(404, "Weekly report was not found.");
            await EnsureCompanyStudentPlacementAsync(Text(report, "placementId"), Text(report, "studentId"), cancellation, requireActive: true).ConfigureAwait(false);
            if (Text(report, "status") != "revisionRequested") throw new UIMS.Services.FirebaseException(409, "A new report revision is allowed only after the academic supervisor requests changes.");
            string currentAcademic = await ResolveAcademicSupervisorAsync(Text(report, "studentId"), cancellation).ConfigureAwait(false);
            var placementNode = await ReadAsync("placements/" + Text(report, "placementId"), cancellation).ConfigureAwait(false);
            return await StoreNewOrRevisionAsync(reportId, Text(report, "placementId"), reportNode, placementNode.Value as JObject,
                Text(report, "studentId"), currentAcademic, (int?)report["weekNumber"] ?? 0, Text(report, "weekStart"), Text(report, "weekEnd"), fileName, contentType, bytes, companyComments, cancellation, selectedTaskIds).ConfigureAwait(false);
        }

        private async Task<JObject> StoreNewOrRevisionAsync(string reportId, string placementId, Node existing, JObject placement, string studentId, string academicId,
            int week, string weekStart, string weekEnd, string fileName, string contentType, byte[] bytes, string companyComments, CancellationToken cancellation, JArray selectedTaskIds)
        {
            if (placement == null) throw new UIMS.Services.FirebaseException(404, "The report placement was not found.");
            string normalized = UIMS.Services.FirebaseFileStorageService.ValidateFile(fileName, contentType, bytes);
            if (!normalized.Equals(DocxMime, StringComparison.Ordinal)) throw new UIMS.Services.FirebaseException(415, "Weekly reports must be valid Microsoft Word .docx documents.");
            if (companyComments != null && companyComments.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Company comments are limited to 3,000 characters.");
            var taskSnapshot = await BuildTaskSnapshotAsync(placementId, studentId, weekStart, weekEnd, selectedTaskIds, cancellation).ConfigureAwait(false);
            string revisionId = "REV-" + Guid.NewGuid().ToString("N"); string fileId = UIMS.Services.FirebaseFileStorageService.CreateFileId();
            string objectPath = "uims/weekly-reports/" + reportId + "/" + revisionId + "/" + fileId + ".docx";
            UIMS.Services.StoredFile stored = await upload(objectPath, fileName, normalized, bytes, cancellation).ConfigureAwait(false);
            if (stored == null || stored.ObjectPath != objectPath || stored.ContentType != DocxMime || stored.SizeBytes != bytes.LongLength)
            {
                try { await delete(objectPath, CancellationToken.None).ConfigureAwait(false); } catch { }
                throw new UIMS.Services.FirebaseException(502, "Firebase Storage did not confirm the exact weekly report upload.");
            }
            string now = UtcNow();
            var metadata = new JObject { ["id"] = fileId, ["purpose"] = "weeklyReport", ["status"] = "ready", ["reportId"] = reportId, ["revisionId"] = revisionId,
                ["placementId"] = placementId, ["studentId"] = studentId, ["ownerUid"] = actor.Uid, ["ownerProfileId"] = actor.CompanySupervisorId,
                ["fileName"] = stored.FileName, ["contentType"] = stored.ContentType, ["sizeBytes"] = stored.SizeBytes, ["sha256"] = stored.Sha256,
                ["storagePath"] = stored.ObjectPath, ["storageGeneration"] = stored.Generation, ["createdAt"] = now };

            bool reportWriteAttempted = false;
            try
            {
                await WriteAsync("fileAttachments/" + fileId, metadata, "null_etag", HttpMethod.Put, cancellation).ConfigureAwait(false);
                JObject updated;
                if (existing == null)
                {
                    var revision = Revision(revisionId, 1, stored, fileId, companyComments, now);
                    revision["taskSnapshot"] = taskSnapshot;
                    updated = new JObject { ["reportId"] = reportId, ["placementId"] = placementId, ["studentId"] = studentId,
                        ["companyId"] = Text(placement, "companyId"), ["companySupervisorId"] = actor.CompanySupervisorId, ["academicSupervisorId"] = academicId,
                        ["weekNumber"] = week, ["weekStart"] = weekStart, ["weekEnd"] = weekEnd, ["status"] = "submitted",
                        ["uploadedByUid"] = actor.Uid, ["currentRevisionId"] = revisionId, ["revisionNumber"] = 1, ["createdAt"] = now,
                        ["updatedAt"] = now, ["submittedAt"] = now, ["companyComments"] = string.IsNullOrWhiteSpace(companyComments) ? null : companyComments.Trim(),
                        ["revisions"] = new JObject { [revisionId] = revision } };
                    reportWriteAttempted = true;
                    updated = await WriteAsync("weeklyReports/" + reportId, updated, "null_etag", HttpMethod.Put, cancellation).ConfigureAwait(false);
                }
                else
                {
                    updated = (JObject)existing.Value.DeepClone();
                    if (updated["revisions"] != null && !(updated["revisions"] is JObject))
                        throw new UIMS.Services.FirebaseException(409, "Stored weekly report revision history is malformed; no history was changed.");
                    var revisions = updated["revisions"] as JObject ?? new JObject();
                    if (revisions[revisionId] != null) throw new UIMS.Services.FirebaseException(409, "The revision ID already exists.");
                    int next = ((int?)updated["revisionNumber"] ?? revisions.Properties().Count()) + 1;
                    revisions[revisionId] = Revision(revisionId, next, stored, fileId, companyComments, now);
                    revisions[revisionId]["taskSnapshot"] = taskSnapshot;
                    updated["revisions"] = revisions; updated["currentRevisionId"] = revisionId; updated["revisionNumber"] = next;
                    updated["academicSupervisorId"] = academicId; updated["uploadedByUid"] = actor.Uid; updated["companyComments"] = string.IsNullOrWhiteSpace(companyComments) ? null : companyComments.Trim();
                    updated["status"] = "resubmitted"; updated["submittedAt"] = now; updated["updatedAt"] = now;
                    reportWriteAttempted = true;
                    updated = await WriteAsync("weeklyReports/" + reportId, updated, existing.ETag, HttpMethod.Put, cancellation).ConfigureAwait(false);
                }
                return new JObject { ["report"] = PublicReport(updated), ["message"] = "Weekly report revision submitted to the assigned academic supervisor." };
            }
            catch
            {
                // A timeout can happen after RTDB commits. Confirm the immutable revision before any compensation.
                bool reconciliationSucceeded = false;
                if (reportWriteAttempted)
                {
                    try
                    {
                        var persisted = await ReadAsync("weeklyReports/" + reportId, CancellationToken.None).ConfigureAwait(false);
                        var persistedReport = persisted.Value as JObject;
                        if (persistedReport != null && Text(persistedReport, "currentRevisionId") == revisionId && persistedReport["revisions"]?[revisionId] != null)
                            return new JObject { ["report"] = PublicReport(persistedReport), ["message"] = "Weekly report revision is saved in Firebase." };
                        reconciliationSucceeded = true;
                    }
                    catch { /* The write outcome is unknown: leave the private object intact rather than break a possible committed record. */ }
                }
                if (reportWriteAttempted && !reconciliationSucceeded) throw;
                // Storage object names are unique; cleanup is limited to this just-created object and metadata.
                try
                {
                    var meta = await ReadAsync("fileAttachments/" + fileId, CancellationToken.None).ConfigureAwait(false);
                    if (meta.Value != null && !string.IsNullOrWhiteSpace(meta.ETag)) await DeleteAsync("fileAttachments/" + fileId, meta.ETag, CancellationToken.None).ConfigureAwait(false);
                }
                catch { /* An unlinked object remains private; never delete historical or uncertain metadata. */ }
                try { await delete(objectPath, CancellationToken.None).ConfigureAwait(false); } catch { }
                throw;
            }
        }
        private JObject Revision(string revisionId, int number, UIMS.Services.StoredFile file, string fileId, string comments, string submittedAt)
        {
            return new JObject { ["revisionId"] = revisionId, ["revisionNumber"] = number, ["fileId"] = fileId, ["fileName"] = file.FileName,
                ["contentType"] = file.ContentType, ["sizeBytes"] = file.SizeBytes, ["sha256"] = file.Sha256, ["storagePath"] = file.ObjectPath,
                ["storageGeneration"] = file.Generation, ["companyComments"] = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim(),
                ["submittedAt"] = submittedAt, ["uploadedByUid"] = actor.Uid };
        }
        private static JObject PublicReport(JObject report)
        {
            var safe = (JObject)report.DeepClone();
            var revisions = safe["revisions"] as JObject;
            if (revisions != null) foreach (var revision in revisions.Properties())
            { var value = revision.Value as JObject; if (value != null) value.Remove("storagePath"); }
            return safe;
        }
        private async Task<JObject> ReadReviewsAsync(string reportId, CancellationToken cancellation)
        { var node = await ReadAsync("weeklyReportReviews/" + reportId, cancellation).ConfigureAwait(false); return node.Value as JObject ?? new JObject(); }

        public async Task<JObject> ReviewAsync(string reportId, string revisionId, string decision, string feedback, string requestId, CancellationToken cancellation, decimal? academicScore = null, string descriptiveRating = null)
        {
            Ensure(true, "academicSupervisor");
            if (decision != "approved" && decision != "revisionRequested") throw new UIMS.Services.FirebaseException(400, "Choose approve or request revision.");
            if (string.IsNullOrWhiteSpace(feedback) || feedback.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Academic feedback is required and limited to 3,000 characters.");
            Guid parsed; if (!Guid.TryParse(requestId, out parsed)) throw new UIMS.Services.FirebaseException(400, "A valid idempotency request ID is required."); requestId = parsed.ToString("N");
            var node = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); var report = node.Value as JObject;
            if (report == null) throw new UIMS.Services.FirebaseException(404, "Weekly report was not found.");
            await EnsureCurrentAssignmentAsync(Text(report, "studentId"), actor.AcademicSupervisorId, cancellation).ConfigureAwait(false);
            if (decision == "approved" && (!academicScore.HasValue || academicScore < 0 || academicScore > 100 || string.IsNullOrWhiteSpace(descriptiveRating) || descriptiveRating.Length > 80))
                throw new UIMS.Services.FirebaseException(400, "Approval requires an independent academic score from 0 to 100 and a separately selected descriptive rating.");
            bool awaitingReview = (Text(report, "status") == "submitted" || Text(report, "status") == "resubmitted") && Text(report, "currentRevisionId") == revisionId;
            var revision = report["revisions"]?[revisionId] as JObject;
            if (revision == null) throw new UIMS.Services.FirebaseException(404, "The submitted revision was not found.");
            string reviewPath = "weeklyReportReviews/" + reportId + "/" + requestId;
            var existingReview = await ReadAsync(reviewPath, cancellation).ConfigureAwait(false);
            if (existingReview.Value is JObject)
            {
                var prior = (JObject)existingReview.Value;
                if (Text(prior, "revisionId") != revisionId || Text(prior, "reviewerUid") != actor.Uid || Text(prior, "decision") != decision)
                    throw new UIMS.Services.FirebaseException(409, "The idempotency key has already been used for a different review.");
                if (Text(prior, "feedback") != feedback.Trim() || decision == "approved" && ((decimal?)prior["academicScore"] != academicScore || Text(prior, "descriptiveRating") != descriptiveRating.Trim()))
                    throw new UIMS.Services.FirebaseException(409, "This review ID already has a different evaluation or feedback.");
                var latest = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false);
                if (Text(latest.Value, "lastReviewId") != requestId) throw new UIMS.Services.FirebaseException(409, "This review request is still pending reconciliation; refresh before retrying.");
                prior = await FinalizeReviewRecordAsync(reviewPath, prior, cancellation).ConfigureAwait(false);
                return new JObject { ["review"] = prior, ["report"] = PublicReport((JObject)latest.Value) };
            }
            if (!awaitingReview) throw new UIMS.Services.FirebaseException(409, "This report revision is stale or is not awaiting review.");
            string reviewedAt = UtcNow();
            var review = new JObject { ["reviewId"] = requestId, ["reportId"] = reportId, ["revisionId"] = revisionId,
                ["academicSupervisorId"] = actor.AcademicSupervisorId, ["reviewerUid"] = actor.Uid, ["decision"] = decision,
                ["feedback"] = feedback.Trim(), ["reviewedAt"] = reviewedAt, ["status"] = "pendingCommit" };
            if (decision == "approved") { review["academicScore"] = academicScore.Value; review["descriptiveRating"] = descriptiveRating.Trim(); }
            await WriteAsync(reviewPath, review, "null_etag", HttpMethod.Put, cancellation).ConfigureAwait(false);
            var updated = (JObject)report.DeepClone(); updated["status"] = decision; updated["lastReviewId"] = requestId; updated["lastReviewedAt"] = reviewedAt; updated["updatedAt"] = reviewedAt;
            if (decision == "approved")
            {
                var evaluation = new JObject { ["revisionId"] = revisionId, ["score"] = academicScore.Value, ["maximumScore"] = 100, ["rating"] = descriptiveRating.Trim(), ["feedback"] = feedback.Trim(), ["approvedAt"] = reviewedAt, ["academicSupervisorId"] = actor.AcademicSupervisorId };
                updated["revisions"][revisionId]["academicEvaluation"] = evaluation;
                updated["latestApprovedEvaluation"] = evaluation.DeepClone();
            }
            try
            {
                await EnsureCurrentAssignmentAsync(Text(report, "studentId"), actor.AcademicSupervisorId, cancellation).ConfigureAwait(false);
                updated = await WriteAsync("weeklyReports/" + reportId, updated, node.ETag, HttpMethod.Put, cancellation).ConfigureAwait(false);
                review = await FinalizeReviewRecordAsync(reviewPath, review, cancellation).ConfigureAwait(false);
                return new JObject { ["review"] = review, ["report"] = PublicReport(updated) };
            }
            catch (UIMS.Services.FirebaseException)
            {
                Node latest = null;
                try { latest = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); } catch { }
                if (latest != null && Text(latest.Value, "lastReviewId") == requestId)
                {
                    review = await FinalizeReviewRecordAsync(reviewPath, review, cancellation).ConfigureAwait(false);
                    return new JObject { ["review"] = review, ["report"] = PublicReport((JObject)latest.Value) };
                }
                Node pending = null;
                try { pending = await ReadAsync(reviewPath, cancellation).ConfigureAwait(false); } catch { }
                if (pending != null && pending.Value is JObject && pending.ETag != null)
                {
                    var stale = (JObject)pending.Value; stale["status"] = "stale";
                    try { await WriteAsync(reviewPath, stale, pending.ETag, HttpMethod.Put, cancellation).ConfigureAwait(false); } catch { }
                }
                throw;
            }
        }

        public async Task<JObject> AddDiscussionAsync(JObject input, CancellationToken cancellation)
        {
            Ensure(true);
            string reportId = Text(input, "reportId"), revisionId = Text(input, "revisionId"), taskId = Text(input, "taskId"), message = Text(input, "message").Trim(), requestId = Text(input, "requestId");
            Guid operation; if (!Guid.TryParse(requestId, out operation) || message.Length == 0 || message.Length > 3000) throw new UIMS.Services.FirebaseException(400, "A request ID and comment up to 3000 characters are required.");
            var node = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); var report = node.Value as JObject;
            if (report == null) throw new UIMS.Services.FirebaseException(404, "Report not found.");
            if (actor.Role == "academicSupervisor") await EnsureCurrentAssignmentAsync(Text(report, "studentId"), actor.AcademicSupervisorId, cancellation).ConfigureAwait(false);
            else if (actor.Role == "companySupervisor") await EnsureCompanyStudentPlacementAsync(Text(report, "placementId"), Text(report, "studentId"), cancellation).ConfigureAwait(false);
            else throw new UIMS.Services.FirebaseException(403, "Report discussions are private to the authorized supervisors.");
            var revision = report["revisions"]?[revisionId] as JObject;
            if (revision == null) throw new UIMS.Services.FirebaseException(404, "Report revision not found.");
            if (taskId.Length > 0 && !(revision["taskSnapshot"] as JArray ?? new JArray()).Any(t => Text(t, "taskId") == taskId)) throw new UIMS.Services.FirebaseException(403, "This task is outside the immutable report snapshot.");
            var discussions = report["discussions"] as JObject ?? new JObject(); string key = operation.ToString("N");
            if (discussions[key] != null) { if (Text(discussions[key], "authorUid") != actor.Uid || Text(discussions[key], "message") != message || Text(discussions[key], "taskId") != taskId || Text(discussions[key], "revisionId") != revisionId) throw new UIMS.Services.FirebaseException(409, "Discussion request ID was already used."); return new JObject { ["success"] = true }; }
            discussions[key] = new JObject { ["revisionId"] = revisionId, ["taskId"] = taskId, ["message"] = message, ["authorUid"] = actor.Uid, ["authorRole"] = actor.Role, ["createdAt"] = UtcNow() }; report["discussions"] = discussions;
            await WriteAsync("weeklyReports/" + reportId, report, node.ETag, HttpMethod.Put, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true };
        }

        private async Task<JObject> FinalizeReviewRecordAsync(string path, JObject review, CancellationToken cancellation)
        {
            var node = await ReadAsync(path, cancellation).ConfigureAwait(false); var current = node.Value as JObject;
            if (current == null) throw new UIMS.Services.FirebaseException(503, "The saved academic review record could not be confirmed.");
            if (Text(current, "status") == "committed") return current;
            current["status"] = "committed";
            return await WriteAsync(path, current, node.ETag, HttpMethod.Put, cancellation).ConfigureAwait(false);
        }

        public async Task<UIMS.Tasks.TaskFileDownload> DownloadAsync(string reportId, string revisionId, CancellationToken cancellation)
        {
            Ensure(false); var node = await ReadAsync("weeklyReports/" + reportId, cancellation).ConfigureAwait(false); var report = node.Value as JObject;
            if (report == null) throw new UIMS.Services.FirebaseException(404, "Weekly report was not found.");
            if (actor.Role == "companySupervisor") await EnsureCompanyStudentPlacementAsync(Text(report, "placementId"), Text(report, "studentId"), cancellation).ConfigureAwait(false);
            else if (actor.Role == "academicSupervisor") await EnsureCurrentAssignmentAsync(Text(report, "studentId"), actor.AcademicSupervisorId, cancellation).ConfigureAwait(false);
            else throw new UIMS.Services.FirebaseException(403, "Students cannot download or edit company weekly report files.");
            if (string.IsNullOrWhiteSpace(revisionId)) revisionId = Text(report, "currentRevisionId");
            var revision = report["revisions"]?[revisionId] as JObject; if (revision == null) throw new UIMS.Services.FirebaseException(404, "The report revision was not found.");
            string path = Text(revision, "storagePath"); if (path.Length == 0 || Text(revision, "contentType") != DocxMime) throw new UIMS.Services.FirebaseException(404, "The private Word report is unavailable.");
            byte[] contents = await download(path, cancellation).ConfigureAwait(false);
            UIMS.Services.FirebaseFileStorageService.ValidateFile(Text(revision, "fileName"), DocxMime, contents);
            string hash; using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(contents)).Replace("-", "").ToLowerInvariant();
            if (contents.LongLength != (long?)revision["sizeBytes"] || !string.Equals(hash, Text(revision, "sha256"), StringComparison.OrdinalIgnoreCase))
                throw new UIMS.Services.FirebaseException(502, "The private report file does not match its immutable Firebase metadata.");
            return new UIMS.Tasks.TaskFileDownload { FileName = Text(revision, "fileName"), ContentType = DocxMime, Content = contents };
        }

        private async Task<JObject> GetAcademicProgressAsync(JObject report, CancellationToken cancellation)
        {
            string placementId = Text(report, "placementId"), studentId = Text(report, "studentId");
            var tasks = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(tasks, await ReadEqualAsync("tasks", "placementId", placementId, cancellation).ConfigureAwait(false));
            var visibleTasks = new JArray();
            foreach (var pair in tasks)
            {
                if (Text(pair.Value, "studentId", "assignedTo", "traineeId") != studentId) continue;
                visibleTasks.Add(new JObject { ["taskId"] = pair.Key, ["title"] = Text(pair.Value, "title", "name"), ["status"] = pair.Value["status"], ["score"] = pair.Value["grade"] ?? pair.Value["score"], ["maximumScore"] = pair.Value["maximumScore"], ["dueDate"] = pair.Value["dueDate"] });
            }
            var exams = new Dictionary<string, JObject>(StringComparer.Ordinal);
            AddRecords(exams, await ReadEqualAsync("taskExams", "placementId", placementId, cancellation).ConfigureAwait(false));
            var results = new JArray();
            foreach (var exam in exams)
            {
                var attempts = await ReadAsync("taskExamAttempts/" + exam.Key + "/" + studentId, cancellation).ConfigureAwait(false);
                var attemptObject = attempts.Value as JObject; if (attemptObject == null) continue;
                foreach (var attempt in attemptObject.Properties())
                {
                    var item = attempt.Value as JObject; if (item == null || Text(item, "resultStatus") != "released" || !(Text(item, "status") == "submitted" || Text(item, "status") == "expired")) continue;
                    results.Add(new JObject { ["examId"] = exam.Key, ["attemptId"] = attempt.Name, ["status"] = item["status"], ["score"] = item["score"], ["maximumScore"] = item["maximumScore"], ["submittedAt"] = item["submittedAt"], ["gradedAt"] = item["gradedAt"] });
                }
            }
            long approvedMinutes = 0;
            var attendanceStudent = await ReadAsync("uimsAttendance/students/" + studentId, cancellation).ConfigureAwait(false);
            string attendanceAssignmentId = Text(attendanceStudent.Value, "activeAssignmentId");
            if (attendanceAssignmentId.Length > 0)
            {
                var attendanceAssignment = await ReadAsync("uimsAttendance/trainingAssignments/" + attendanceAssignmentId, cancellation).ConfigureAwait(false);
                if (Text(attendanceAssignment.Value, "studentId") != studentId || Text(attendanceAssignment.Value, "status") != "active")
                    throw new UIMS.Services.FirebaseException(409, "The existing attendance assignment is not active for this student.");
                var attendance = await ReadAsync("uimsAttendance/attendance/" + attendanceAssignmentId, cancellation).ConfigureAwait(false);
                var days = attendance.Value as JObject;
                if (days != null) foreach (var day in days.Properties())
                {
                    var row = day.Value as JObject;
                    if (row == null || Text(row, "assignmentId") != attendanceAssignmentId || Text(row, "studentId") != studentId ||
                        Text(row, "hoursApprovalStatus") != "approved" || !(Text(row, "status") == "completed" || Text(row, "status") == "earlyDeparture")) continue;
                    long minutes = (long?)row["workedMinutes"] ?? 0L;
                    if (minutes <= 0)
                    {
                        long startAt = (long?)row["checkInAt"] ?? 0L, endAt = (long?)row["checkOutAt"] ?? 0L;
                        if (startAt > 0 && endAt > startAt) minutes = (endAt - startAt) / 60000;
                    }
                    if (minutes > 0)
                    {
                        if (approvedMinutes > long.MaxValue - minutes) throw new UIMS.Services.FirebaseException(409, "Approved attendance minutes exceed the supported range.");
                        approvedMinutes += minutes;
                    }
                }
            }
            return new JObject { ["tasks"] = visibleTasks, ["examResults"] = results, ["approvedTrainingMinutes"] = approvedMinutes,
                ["approvedTrainingHours"] = Math.Round(approvedMinutes / 60.0, 2), ["attendanceSource"] = "uimsAttendance/attendance" };
        }

        private sealed class Node { public JToken Value; public string ETag; }
    }
}
