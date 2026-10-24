using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Google.Apis.Auth.OAuth2;

namespace UIMS.Tasks
{
    public sealed class TaskActor
    {
        public string Uid { get; private set; }
        public string Role { get; private set; }
        public string StudentId { get; private set; }
        public string CompanySupervisorId { get; private set; }
        public string CompanyId { get; private set; }
        public string AcademicSupervisorId { get; private set; }

        private TaskActor(string uid, string role, string studentId, string supervisorId, string companyId)
        { Uid = uid; Role = role; StudentId = studentId; CompanySupervisorId = supervisorId; CompanyId = companyId; AcademicSupervisorId = role == "academicSupervisor" ? uid : null; }

        // Values must come from the verified server ticket, current /users/{uid} profile and
        // (for company users without an explicit profile mapping) /companySupervisors/{id}.firebaseUid.
        public static TaskActor FromVerifiedProfile(string uid, string ticketRole, JObject profile, string mappedSupervisorId = null, bool allowAcademicSupervisor = false)
        {
            if (string.IsNullOrWhiteSpace(uid) || profile == null || profile.Value<bool?>("isActive") != true ||
                (University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "accountSetupStatus").Length > 0 && !string.Equals(University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "accountSetupStatus"), "complete", StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "deletionStatus"), "pending", StringComparison.OrdinalIgnoreCase))
                throw new UIMS.Services.FirebaseException(403, "The verified Firebase account has no active, completed UIMS profile.");

            string profileRole = University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "role"));
            string role = University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(ticketRole);
            if (role == null || role != profileRole) throw new UIMS.Services.FirebaseException(403, "The signed-in role no longer matches the current Firebase profile.");

            if (role == "student")
            {
                string studentId = University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "studentId");
                if (studentId.Length == 0) studentId = University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "id");
                if (studentId.Length == 0) throw new UIMS.Services.FirebaseException(403, "The student profile has no existing studentId or id mapping.");
                return new TaskActor(uid, role, studentId, null, null);
            }

            if (role == "companySupervisor")
            {
                string supervisorId = University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "companySupervisorId");
                if (supervisorId.Length == 0) supervisorId = mappedSupervisorId ?? "";
                if (supervisorId.Length == 0) throw new UIMS.Services.FirebaseException(403, "No company supervisor profile mapping for this verified Firebase UID was found.");
                return new TaskActor(uid, role, null, supervisorId, University_Internship_Management_System.Shared.StudentManagementService.Text(profile, "companyId"));
            }

            // Existing academic supervisor profiles are keyed by Firebase Auth UID.
            // Their UID is also the supervisor ID used by student assignments.
            if (role == "academicSupervisor" && allowAcademicSupervisor) return new TaskActor(uid, role, null, null, null);

            throw new UIMS.Services.FirebaseException(403, "This account role is not enabled for the requested operation.");
        }

        internal bool IsStudent(string id) { return Role == "student" && (id == StudentId || id == Uid); }
    }

    // Uses the established top-level /tasks records and server-resolved actors.
    public sealed partial class TaskService
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 12 }) { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly Lazy<Task<GoogleCredential>> ServerCredential = new Lazy<Task<GoogleCredential>>(() => GoogleCredential.GetApplicationDefaultAsync());
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> AssignmentLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly HttpClient client;
        private readonly UIMS.Services.FirebaseConfiguration firebase;
        private readonly string databaseUrl;
        private readonly string root;
        private readonly TaskActor actor;
        private readonly Func<CancellationToken, Task<string>> accessToken;
        private readonly UIMS.Services.FirebaseFileStorageService fileStorage;
        private readonly bool enabled;
        private readonly bool writesEnabled;
        private readonly bool schemaValidated;
        internal string ActorRole { get { return actor == null ? null : actor.Role; } }
        internal Task<JObject> ChangesAsync(long previous, CancellationToken cancellation) { Ensure(false); return TaskChangeFeed.WaitAsync(previous, cancellation); }

        public TaskService() : this(k => WebConfigurationManager.AppSettings[k], Client, null, GetServerAccessTokenAsync) { }
        public TaskService(TaskActor verifiedActor) : this(k => WebConfigurationManager.AppSettings[k], Client, verifiedActor, GetServerAccessTokenAsync) { }
        public TaskService(Func<string, string> setting, HttpClient httpClient, TaskActor verifiedActor,
            Func<CancellationToken, Task<string>> tokenProvider = null, UIMS.Services.FirebaseFileStorageService storage = null)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            actor = verifiedActor;
            accessToken = tokenProvider ?? GetServerAccessTokenAsync;
            firebase = new UIMS.Services.FirebaseConfiguration(setting("FirebaseEnabled"), setting("FirebaseProjectId"), setting("FirebaseDatabaseUrl"), setting("FirebaseApiKey"), setting("FirebaseLiveWritesEnabled"), setting("FirebaseTaskSchemaValidated"), setting("FirebaseSchemaValidated"), setting("FirebaseAuthenticationEnabled"));
            enabled = firebase.Enabled && string.Equals(setting("TasksEnabled"), "true", StringComparison.OrdinalIgnoreCase);
            databaseUrl = firebase.DatabaseUrl.TrimEnd('/');
            root = string.IsNullOrWhiteSpace(setting("FirebaseTaskRootPath")) ? "tasks" : setting("FirebaseTaskRootPath").Trim('/');
            schemaValidated = string.Equals(setting("FirebaseTaskSchemaValidated"), "true", StringComparison.OrdinalIgnoreCase);
            writesEnabled = string.Equals(setting("TasksLiveWritesEnabled"), "true", StringComparison.OrdinalIgnoreCase);
            fileStorage = storage ?? new UIMS.Services.FirebaseFileStorageService(setting, client);
        }

        private static async Task<string> GetServerAccessTokenAsync(CancellationToken cancellation)
        {
            return await University_Internship_Management_System.Shared.FirebaseServerCredential.AccessTokenAsync(cancellation).ConfigureAwait(false);
        }

        private void Ensure(bool write, string requiredRole = null)
        {
            if (!enabled) throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured for tasks.");
            firebase.EnsureAuthenticationEnabled();
            if (!schemaValidated || root != "tasks") throw new UIMS.Services.FirebaseException(503, "The existing /tasks schema is not validated.");
            Uri url;
            if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out url) || url.Scheme != "https" || !url.IsDefaultPort || url.AbsolutePath != "/")
                throw new UIMS.Services.FirebaseException(503, "Configure the HTTPS Firebase database URL in Web.config.");
            if (actor == null || string.IsNullOrWhiteSpace(actor.Uid)) throw new UIMS.Services.FirebaseException(401, "Sign in with a verified Firebase account before using tasks.");
            if (requiredRole != null && actor.Role != requiredRole) throw new UIMS.Services.FirebaseException(403, "This task operation is not allowed for the signed-in role.");
            if (write && (!writesEnabled || !firebase.LiveWritesEnabled || !firebase.SchemaValidated)) throw new UIMS.Services.FirebaseException(503, "Firebase task writes are disabled until the task schema, authorization and development configuration are validated.");
        }

        private async Task<HttpRequestMessage> AuthorizedRequestAsync(HttpMethod method, string url, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var request = new HttpRequestMessage(method, url);
            try
            {
                string token = await accessToken(cancellation).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(token)) throw new UIMS.Services.FirebaseException(503, "Trusted server Firebase credentials did not provide an access token.");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return request;
            }
            catch (UIMS.Services.FirebaseException) { request.Dispose(); throw; }
            catch (Exception error) when (error is InvalidOperationException || error is System.IO.IOException || error is System.Security.SecurityException)
            {
                request.Dispose();
                throw new UIMS.Services.FirebaseException(503, "Trusted server Firebase credentials are unavailable. Configure application default credentials on the server.");
            }
        }

        private static void CheckKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 180 || key.Any(c => ".$#[]/\\".IndexOf(c) >= 0 || char.IsControl(c)))
                throw new UIMS.Services.FirebaseException(400, "Invalid Firebase record ID.");
        }

        private string Url(string path)
        {
            return UrlAt(root, path);
        }

        private string UrlAt(string collection, string path)
        {
            CheckKey(collection);
            if (!string.IsNullOrWhiteSpace(path))
                foreach (var part in path.Split('/')) CheckKey(part);
            return databaseUrl + "/" + collection + (string.IsNullOrWhiteSpace(path) ? "" : "/" + path) + ".json";
        }

        private async Task<TaskRecord> Read(string path, CancellationToken cancellation)
        {
            return await ReadAt(root, path, cancellation).ConfigureAwait(false);
        }

        private async Task<TaskRecord> ReadAt(string collection, string path, CancellationToken cancellation)
        {
            using (var request = await AuthorizedRequestAsync(HttpMethod.Get, UrlAt(collection, path), cancellation).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    await CheckResponse(response, "/" + collection + (string.IsNullOrWhiteSpace(path) ? "" : "/" + path)).ConfigureAwait(false);
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var etag = response.Headers.Contains("ETag") ? string.Join("", response.Headers.GetValues("ETag")) : null;
                    return new TaskRecord { Data = string.IsNullOrWhiteSpace(json) || json == "null" ? new JObject() : JObject.Parse(json), ETag = etag };
                }
            }
        }

        private async Task Write(string method, string path, JToken data, string etag, CancellationToken cancellation)
        {
            await WriteAt(root, method, path, data, etag, cancellation).ConfigureAwait(false);
        }

        private async Task WriteAt(string collection, string method, string path, JToken data, string etag, CancellationToken cancellation)
        {
            using (var request = await AuthorizedRequestAsync(new HttpMethod(method), UrlAt(collection, path), cancellation).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(etag)) request.Headers.TryAddWithoutValidation("if-match", etag);
                request.Content = new StringContent(data == null ? "null" : data.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) throw new UIMS.Services.FirebaseException(409, "This Firebase record changed. Refresh the page and try again.");
                    await CheckResponse(response, "/" + collection + "/" + path).ConfigureAwait(false);
                }
            }
        }

        private async Task PatchTaskPaths(JObject updates, CancellationToken cancellation)
        {
            foreach (var property in updates.Properties())
                foreach (var segment in property.Name.Split('/')) CheckKey(segment);
            // Firebase multi-location PATCH is atomic and preserves all unrelated Cycle 3 records.
            using (var request = await AuthorizedRequestAsync(new HttpMethod("PATCH"), databaseUrl + "/" + root + ".json", cancellation).ConfigureAwait(false))
            {
                request.Content = new StringContent(updates.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                    await CheckResponse(response, "taskGroups and taskAssignments").ConfigureAwait(false);
            }
        }

        private static async Task CheckResponse(HttpResponseMessage response, string path)
        {
            if (response.IsSuccessStatusCode) return;
            var code = (int)response.StatusCode;
            if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UIMS.Services.FirebaseException(403, "Firebase Rules denied access to " + (path.StartsWith("/", StringComparison.Ordinal) ? path : "/tasks/" + path) + ". No authentication or Rules bypass was attempted.");
            throw new UIMS.Services.FirebaseException(code >= 500 ? 502 : code, "Firebase task request failed (HTTP " + code.ToString(CultureInfo.InvariantCulture) + ").");
        }

        private async Task<JObject> ReadData(string path, CancellationToken cancellation) { return (await Read(path, cancellation).ConfigureAwait(false)).Data; }
        private static string Text(JToken row, string field)
        {
            var token = row is JObject ? row[field] : null;
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.Date)
            {
                var date = token.Value<DateTime>();
                var utc = date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();
                return utc.ToString("o", CultureInfo.InvariantCulture);
            }
            return token.ToString();
        }
        private static string FirstText(JToken row, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = Text(row, field);
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return "";
        }
        private static int Number(JToken row, string field, int fallback = 0) { int value; return int.TryParse(Text(row, field), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback; }
        private static IEnumerable<JProperty> Rows(JObject value) { return value == null ? Enumerable.Empty<JProperty>() : value.Properties(); }
        private static bool GroupHasStudent(JObject group, string id)
        {
            var members = group["studentIds"] as JArray;
            if (members != null && members.Any(x => x.Type == JTokenType.String && string.Equals(x.ToString(), id, StringComparison.Ordinal))) return true;
            var assignees = group["assignees"] as JObject;
            return assignees != null && assignees[id] != null && assignees[id].Type == JTokenType.Boolean && (bool)assignees[id];
        }
        private static string Status(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "new": case "assigned": return "assigned";
                case "in progress": case "in_progress": case "inprogress": return "inprogress";
                case "inreview": case "submitted": return "submitted";
                case "revisionrequested": case "revision needed": case "revision": return "revision";
                case "completed": case "evaluated": case "reviewed": return "completed";
                default: return value;
            }
        }

        private async Task<string> AssignedStudentIdAsync(JObject task, CancellationToken cancellation)
        {
            var assignedStudent = Text(task, "studentId");
            if (assignedStudent.Length > 0) return assignedStudent;
            var placementId = Text(task, "placementId");
            if (placementId.Length == 0) return "";
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            return Text(placement, "studentId");
        }

        private async Task EnsureStudentOwnsTaskAsync(JObject task, CancellationToken cancellation)
        {
            string assignedStudent = await AssignedStudentIdAsync(task, cancellation).ConfigureAwait(false);
            if (!actor.IsStudent(assignedStudent)) throw new UIMS.Services.FirebaseException(403, "This task is not assigned to the verified student.");
            string placementId = Text(task, "placementId");
            if (placementId.Length == 0) return; // Preserve legacy direct student assignments.
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            string linkedStudent = Text(placement, "studentId");
            string placementStatus = Text(placement, "status");
            if (placement.Count == 0 || !actor.IsStudent(linkedStudent) ||
                (!placementStatus.Equals("active", StringComparison.OrdinalIgnoreCase) && !placementStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(403, "The task placement is not active for the verified student.");
        }

        private async Task EnsureCompanyOwnsTaskAsync(JObject task, CancellationToken cancellation)
        {
            if (Text(task, "assignedBy") != actor.CompanySupervisorId) throw new UIMS.Services.FirebaseException(403, "This task does not belong to the verified company supervisor.");
            string placementId = Text(task, "placementId");
            if (placementId.Length == 0) return; // Preserve legacy records without a placement link.
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            string placementStatus = Text(placement, "status");
            string taskStudentId = Text(task, "studentId");
            if (placement.Count == 0 || Text(placement, "companySupervisorId") != actor.CompanySupervisorId ||
                (!placementStatus.Equals("active", StringComparison.OrdinalIgnoreCase) && !placementStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase)) ||
                (actor.CompanyId.Length > 0 && Text(placement, "companyId").Length > 0 && Text(placement, "companyId") != actor.CompanyId) ||
                (taskStudentId.Length > 0 && taskStudentId != Text(placement, "studentId")))
                throw new UIMS.Services.FirebaseException(403, "The task placement is not authorized for this company supervisor.");
        }

        private static JObject FindStudentProfile(JObject users, string businessId, string uid)
        {
            if (users == null) return null;
            if (!string.IsNullOrWhiteSpace(uid) && users[uid] is JObject byUid &&
                University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(byUid, "role")) == "student") return byUid;
            foreach (var entry in Rows(users))
            {
                var profile = entry.Value as JObject;
                if (profile == null || University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(profile, "role")) != "student") continue;
                if (Text(profile, "studentId") == businessId || Text(profile, "id") == businessId || entry.Name == businessId) return profile;
            }
            return null;
        }

        public Task<JObject> CheckStorageAvailabilityAsync(CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            cancellation.ThrowIfCancellationRequested();
            return fileStorage.CheckAvailabilityAsync(cancellation);
        }

        public async Task<JObject> LoadAsync(string role, CancellationToken cancellation)
        {
            Ensure(false);
            if (role != actor.Role) throw new UIMS.Services.FirebaseException(403, "The requested task view does not match the verified account role.");
            // Imported tasks live at /tasks/{id}; legacy tasks link to placements by placementId.
            // Resolve those existing relations so old and new task records appear for the right roles.
            var taskRead = ReadData("", cancellation); var placementRead = ReadAt("placements", "", cancellation); var userRead = ReadAt("users", "", cancellation);
            await Task.WhenAll(taskRead, placementRead, userRead).ConfigureAwait(false);
            var records = await taskRead.ConfigureAwait(false); var placementRecords = await placementRead.ConfigureAwait(false); var userRecords = await userRead.ConfigureAwait(false);
            var placements = Rows(placementRecords.Data).Where(x => x.Value is JObject)
                .ToDictionary(x => x.Name, x => (JObject)x.Value, StringComparer.Ordinal);
            var outputTasks = new JArray(); var outputSubs = new JArray(); var students = new JArray();
            var currentStudentProfile = FindStudentProfile(userRecords.Data, actor.StudentId, actor.Uid);
            var configuredStudentName = FirstText(currentStudentProfile, "displayName", "fullName", "name");
            if (configuredStudentName.Length == 0) configuredStudentName = actor.StudentId ?? actor.Uid;
            if (role == "companySupervisor")
            {
                foreach (var placement in placements)
                {
                    var row = placement.Value;
                    var placementStatus = Text(row, "status");
                    var assignedStudent = Text(row, "studentId");
                    if (Text(row, "companySupervisorId") != actor.CompanySupervisorId ||
                        (actor.CompanyId.Length > 0 && Text(row, "companyId") != actor.CompanyId) ||
                        (!placementStatus.Equals("active", StringComparison.OrdinalIgnoreCase) && !placementStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase)) || assignedStudent.Length == 0) continue;
                    var profile = FindStudentProfile(userRecords.Data, assignedStudent, assignedStudent);
                    if (profile == null || (Text(profile, "role").Length > 0 && University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(profile, "role")) != "student")) continue;
                    var displayName = FirstText(profile, "displayName", "fullName", "name");
                    students.Add(new JObject { ["id"] = assignedStudent, ["name"] = displayName.Length > 0 ? displayName : assignedStudent, ["placementId"] = placement.Key });
                }
            }
            foreach (var entry in Rows(records))
            {
                var task = entry.Value as JObject; if (task == null) continue;
                var assignedStudent = Text(task, "studentId");
                var placementId = Text(task, "placementId");
                JObject linkedPlacement = null;
                bool hasPlacement = placementId.Length > 0 && placements.TryGetValue(placementId, out linkedPlacement);
                if (assignedStudent.Length == 0 && hasPlacement) assignedStudent = Text(linkedPlacement, "studentId");
                var assignedSupervisor = Text(task, "assignedBy");
                if (role == "student" && !actor.IsStudent(assignedStudent)) continue;
                if (role == "companySupervisor" && assignedSupervisor != actor.CompanySupervisorId) continue;
                if (hasPlacement)
                {
                    string placementStatus = Text(linkedPlacement, "status");
                    bool activePlacement = placementStatus.Equals("active", StringComparison.OrdinalIgnoreCase) || placementStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase);
                    if (!activePlacement || (Text(task, "studentId").Length > 0 && Text(task, "studentId") != Text(linkedPlacement, "studentId"))) continue;
                    if (role == "student" && !actor.IsStudent(Text(linkedPlacement, "studentId"))) continue;
                    if (role == "companySupervisor" && (Text(linkedPlacement, "companySupervisorId") != actor.CompanySupervisorId ||
                        (actor.CompanyId.Length > 0 && Text(linkedPlacement, "companyId").Length > 0 && Text(linkedPlacement, "companyId") != actor.CompanyId))) continue;
                }
                else if (placementId.Length > 0) continue;
                var status = Text(task, "status");
                var submissions = TaskSubmissionHistory.Read(task["submissions"]);
                foreach (var submission in submissions.Records)
                    outputSubs.Add(new JObject { ["id"] = entry.Name + "-" + Number(submission, "revision", 1).ToString(CultureInfo.InvariantCulture),
                        ["taskId"] = entry.Name, ["assignmentId"] = entry.Name,
                        ["description"] = Text(submission, "description"), ["link"] = Text(submission, "link"),
                        ["fileName"] = Text(submission, "fileName"), ["revision"] = Number(submission, "revision", 1),
                        ["createdAt"] = Text(submission, "submittedAt"), ["attachmentIds"] = submission["attachmentIds"] == null ? new JArray() : submission["attachmentIds"].DeepClone() });
                outputTasks.Add(new JObject {
                    ["id"] = entry.Name, ["studentId"] = assignedStudent,
                    ["placementId"] = placementId, ["studentName"] = role == "student" && actor.IsStudent(assignedStudent) ? configuredStudentName : FirstText(FindStudentProfile(userRecords.Data, assignedStudent, assignedStudent), "displayName", "fullName", "name", "studentId"), ["title"] = Text(task, "title"),
                    ["description"] = Text(task, "description"),
                    ["deadline"] = Text(task, "dueDate").Length > 0 ? Text(task, "dueDate") : Text(task, "deadline"),
                    ["criteria"] = Text(task, "evaluationCriteria"), ["feedback"] = actor.Role == "student" && IsExamTask(task) ? "" : Text(task, "feedback"),
                    ["status"] = Text(task, "archivedAt").Length > 0 ? "archived" : Status(status), ["rawStatus"] = status,
                    ["score"] = actor.Role == "student" && IsExamTask(task) || task["score"] == null ? JValue.CreateNull() : task["score"].DeepClone(),
                    ["maximum"] = Number(task, "maximumScore", 100)
                });
                ((JObject)outputTasks.Last)["batchId"] = task["batchId"];
                ((JObject)outputTasks.Last)["startsAt"] = task["startsAt"] ?? task["assignedAt"];
                ((JObject)outputTasks.Last)["taskType"] = task["taskType"] ?? new JValue("standard");
                ((JObject)outputTasks.Last)["extension"] = task["extension"]?.DeepClone();
                ((JObject)outputTasks.Last)["instructionVersion"] = (int?)task["instructionVersion"] ?? 1;
                ((JObject)outputTasks.Last)["instructionVersions"] = task["instructionVersions"]?.DeepClone();
                ((JObject)outputTasks.Last)["archived"] = Text(task, "archivedAt").Length > 0;
                ((JObject)outputTasks.Last)["notifications"] = new JArray((task["notifications"] as JObject ?? new JObject()).Properties().Where(n => Text(n.Value, "recipientRole") == role).Select(n => n.Value.DeepClone()));
                ((JObject)outputTasks.Last)["attachmentIds"] = task["attachmentIds"] == null ? new JArray() : task["attachmentIds"].DeepClone();
            }
            return new JObject { ["role"] = role, ["identity"] = role == "student" ? actor.StudentId : actor.CompanySupervisorId,
                ["tasks"] = outputTasks, ["submissions"] = outputSubs, ["students"] = students, ["serverNow"] = DateTimeOffset.UtcNow.ToString("o") };
        }

        public async Task<JObject> LoadBoardAsync(string placementId, CancellationToken cancellation)
        {
            Ensure(false);
            CheckKey(placementId);
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            EnsureBoardPlacementAccess(placement, placementId);

            var taskRecords = (await ReadAt("tasks", "", cancellation).ConfigureAwait(false)).Data;
            var boardRecord = await ReadAt("taskBoards", placementId, cancellation).ConfigureAwait(false);
            var board = NormalizeBoard(boardRecord.Data, placementId);
            string defaultColumn = board["columns"].Children<JProperty>()
                .OrderBy(x => (int)x.Value["order"]).ThenBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => x.Name).First();
            if (board["columns"]["todo"] != null) defaultColumn = "todo";
            var visibleTasks = Rows(taskRecords).Where(x => x.Value is JObject && Text((JObject)x.Value, "placementId") == placementId)
                .Where(x => Text(x.Value, "archivedAt").Length == 0)
                .Where(x => TaskBelongsToPlacement((JObject)x.Value, placement)).OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
            var positions = (JObject)board["positions"];
            var cards = new JArray();
            int defaultOrder = 0;
            foreach (var entry in visibleTasks)
            {
                JObject position = positions[entry.Name] as JObject;
                string columnId = WorkflowColumn((JObject)entry.Value);
                if (board["columns"][columnId] == null) throw new UIMS.Services.FirebaseException(409, "The task board contains a position for an unknown column.");
                int order = position == null ? defaultOrder++ * 1024 : ReadBoardOrder(position);
                var task = (JObject)entry.Value;
                cards.Add(new JObject {
                    ["id"] = entry.Name, ["studentId"] = Text(task, "studentId").Length > 0 ? Text(task, "studentId") : Text(placement, "studentId"),
                    ["title"] = Text(task, "title"), ["description"] = Text(task, "description"),
                    ["status"] = Status(Text(task, "status")), ["rawStatus"] = Text(task, "status"),
                    ["columnId"] = columnId, ["order"] = order, ["dueDate"] = FirstText(task, "dueDate", "deadline"),
                    ["score"] = actor.Role == "student" && IsExamTask(task) || task["score"] == null ? JValue.CreateNull() : task["score"].DeepClone(),
                    ["maximumScore"] = Number(task, "maximumScore", 100)
                });
            }
            var columns = new JArray(DefaultBoardColumns().Children<JProperty>()
                .OrderBy(x => (int)x.Value["order"]).ThenBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => new JObject { ["id"] = x.Name, ["title"] = Text((JObject)x.Value, "title"), ["order"] = (int)x.Value["order"] }));
            return new JObject { ["placementId"] = placementId, ["canMove"] = actor.Role == "companySupervisor",
                ["columns"] = columns, ["tasks"] = cards, ["updatedAt"] = Text(board, "updatedAt") };
        }

        public async Task<JObject> MoveBoardTaskAsync(string placementId, string taskId, string columnId, string beforeTaskId, string operationId, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor");
            CheckKey(placementId); CheckKey(taskId); CheckKey(columnId); CheckKey(operationId);
            if (!string.IsNullOrWhiteSpace(beforeTaskId)) CheckKey(beforeTaskId);
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            EnsureBoardPlacementAccess(placement, placementId);

            var task = (await ReadAt("tasks", taskId, cancellation).ConfigureAwait(false)).Data;
            if (task.Count == 0) throw new UIMS.Services.FirebaseException(404, "Task not found.");
            RequireTaskOpen(task);
            if (columnId != WorkflowColumn(task))
                throw new UIMS.Services.FirebaseException(409, "Use task acceptance, submission or supervisor approval to change workflow stage. Dragging only reorders tasks within their current stage.");
            if (Text(task, "placementId") != placementId || !TaskBelongsToPlacement(task, placement))
                throw new UIMS.Services.FirebaseException(403, "This task does not belong to the selected placement.");

            var current = await ReadAt("taskBoards", placementId, cancellation).ConfigureAwait(false);
            var board = NormalizeBoard(current.Data, placementId);
            if (board["columns"][columnId] == null) throw new UIMS.Services.FirebaseException(400, "Choose a valid task board column.");
            string fingerprint = BoardMoveFingerprint(taskId, columnId, beforeTaskId);
            var operations = (JObject)board["operations"];
            var previous = operations[operationId] as JObject;
            if (previous != null)
            {
                if (Text(previous, "fingerprint") != fingerprint) throw new UIMS.Services.FirebaseException(409, "This board operation ID was already used for a different move.");
                return new JObject { ["success"] = true, ["placementId"] = placementId, ["taskId"] = taskId, ["duplicate"] = true };
            }

            var taskRecords = (await ReadAt("tasks", "", cancellation).ConfigureAwait(false)).Data;
            var related = Rows(taskRecords).Where(x => x.Value is JObject && Text((JObject)x.Value, "placementId") == placementId && TaskBelongsToPlacement((JObject)x.Value, placement))
                .OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
            if (!related.Any(x => x.Name == taskId)) throw new UIMS.Services.FirebaseException(409, "The task changed while loading the board. Refresh and retry.");

            var positions = (JObject)board["positions"];
            string defaultColumn = board["columns"].Children<JProperty>().OrderBy(x => (int)x.Value["order"])
                .ThenBy(x => x.Name, StringComparer.Ordinal).Select(x => x.Name).First();
            if (board["columns"]["todo"] != null) defaultColumn = "todo";
            var ordering = related.Select(x => new BoardItem { Id = x.Name,
                ColumnId = WorkflowColumn((JObject)x.Value),
                Order = positions[x.Name] == null ? 0 : ReadBoardOrder((JObject)positions[x.Name]) }).ToList();
            if (ordering.Any(x => board["columns"][x.ColumnId] == null)) throw new UIMS.Services.FirebaseException(409, "The board contains a task position for an unknown column.");
            var moving = ordering.First(x => x.Id == taskId);
            string sourceColumn = moving.ColumnId;
            var sourceItems = ordering.Where(x => x.ColumnId == sourceColumn && x.Id != taskId).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
            var targetItems = sourceColumn == columnId ? sourceItems : ordering.Where(x => x.ColumnId == columnId && x.Id != taskId).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
            int insertAt = targetItems.Count;
            if (!string.IsNullOrWhiteSpace(beforeTaskId))
            {
                if (beforeTaskId == taskId) throw new UIMS.Services.FirebaseException(400, "A task cannot be ordered before itself.");
                insertAt = targetItems.FindIndex(x => x.Id == beforeTaskId);
                if (insertAt < 0) throw new UIMS.Services.FirebaseException(400, "The target task is not in the selected column.");
            }
            targetItems.Insert(insertAt, moving);
            moving.ColumnId = columnId;
            var positionsToSave = new HashSet<string>(targetItems.Select(x => x.Id), StringComparer.Ordinal);
            if (sourceColumn != columnId) foreach (var item in sourceItems) positionsToSave.Add(item.Id);
            foreach (var item in targetItems) item.Order = (targetItems.IndexOf(item) + 1) * 1024;
            if (sourceColumn != columnId)
                for (int i = 0; i < sourceItems.Count; i++) sourceItems[i].Order = (i + 1) * 1024;
            foreach (var item in ordering.Where(x => positionsToSave.Contains(x.Id)))
                positions[item.Id] = new JObject { ["columnId"] = item.ColumnId, ["order"] = item.Order };

            board["placementId"] = placementId;
            board["updatedAt"] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            board["updatedByUid"] = actor.Uid;
            operations[operationId] = new JObject { ["fingerprint"] = fingerprint, ["taskId"] = taskId,
                ["columnId"] = columnId, ["beforeTaskId"] = string.IsNullOrWhiteSpace(beforeTaskId) ? JValue.CreateNull() : new JValue(beforeTaskId),
                ["actorUid"] = actor.Uid, ["completedAt"] = board["updatedAt"].DeepClone() };
            await WriteAt("taskBoards", "PUT", placementId, board, current.ETag, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true, ["placementId"] = placementId, ["taskId"] = taskId, ["duplicate"] = false };
        }

        public async Task<JObject> ReorderColumnsAsync(string placementId, JArray columnIds, string operationId, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); CheckKey(placementId); CheckKey(operationId);
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            EnsureBoardPlacementAccess(placement, placementId);
            var current = await ReadAt("taskBoards", placementId, cancellation).ConfigureAwait(false); var board = NormalizeBoard(current.Data, placementId);
            var columns = (JObject)board["columns"];
            if (columnIds == null || columnIds.Count != columns.Count || columnIds.Any(x => x.Type != JTokenType.String) || columnIds.Values<string>().Distinct().Count() != columns.Count || columnIds.Values<string>().Any(id => columns[id] == null))
                throw new UIMS.Services.FirebaseException(400, "Include every existing column exactly once.");
            string fingerprint = "columns:" + string.Join(",", columnIds.Values<string>());
            var operations = (JObject)board["operations"]; var previous = operations[operationId] as JObject;
            if (previous != null) { if (Text(previous, "fingerprint") != fingerprint) throw new UIMS.Services.FirebaseException(409, "Operation ID already belongs to a different board change."); return new JObject { ["success"] = true, ["duplicate"] = true }; }
            string oldDefault = columns.Properties().OrderBy(c => (int)c.Value["order"]).First().Name;
            if (columns["todo"] != null) oldDefault = "todo";
            var positions = (JObject)board["positions"];
            var tasks = await ReadData("", cancellation).ConfigureAwait(false); int legacyOrder = 0;
            foreach (var task in tasks.Properties().Where(t => t.Value is JObject && Text(t.Value, "placementId") == placementId && TaskBelongsToPlacement((JObject)t.Value, placement)).OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                if (positions[task.Name] == null) positions[task.Name] = new JObject { ["columnId"] = oldDefault, ["order"] = legacyOrder * 1024 };
                legacyOrder++;
            }
            for (int i = 0; i < columnIds.Count; i++) columns[(string)columnIds[i]]["order"] = i;
            board["updatedAt"] = DateTimeOffset.UtcNow.ToString("o"); board["updatedByUid"] = actor.Uid;
            operations[operationId] = new JObject { ["fingerprint"] = fingerprint, ["actorUid"] = actor.Uid, ["completedAt"] = board["updatedAt"] };
            await WriteAt("taskBoards", "PUT", placementId, board, current.ETag, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true };
        }

        private void EnsureBoardPlacementAccess(JObject placement, string placementId)
        {
            string status = Text(placement, "status");
            if (placement == null || placement.Count == 0 ||
                (!status.Equals("active", StringComparison.OrdinalIgnoreCase) && !status.Equals("confirmed", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(404, "An active or confirmed placement was not found.");
            if (actor.Role == "student" && !actor.IsStudent(Text(placement, "studentId")))
                throw new UIMS.Services.FirebaseException(403, "Students can view only their own placement board.");
            if (actor.Role == "companySupervisor" && (Text(placement, "companySupervisorId") != actor.CompanySupervisorId ||
                (actor.CompanyId.Length > 0 && Text(placement, "companyId").Length > 0 && Text(placement, "companyId") != actor.CompanyId)))
                throw new UIMS.Services.FirebaseException(403, "Company supervisors can access boards only for their own placements.");
            if (actor.Role != "student" && actor.Role != "companySupervisor")
                throw new UIMS.Services.FirebaseException(403, "This role cannot access task boards.");
        }

        private static bool TaskBelongsToPlacement(JObject task, JObject placement)
        {
            string taskStudent = Text(task, "studentId");
            string placementStudent = Text(placement, "studentId");
            return taskStudent.Length == 0 || taskStudent == placementStudent;
        }

        private static JObject NormalizeBoard(JObject existing, string placementId)
        {
            var board = existing == null || !existing.HasValues ? new JObject() : (JObject)existing.DeepClone();
            if (board["placementId"] != null && Text(board, "placementId") != placementId)
                throw new UIMS.Services.FirebaseException(409, "The board placement ID does not match its database path.");
            if (board["columns"] == null || board["columns"].Type == JTokenType.Null) board["columns"] = DefaultBoardColumns();
            if (!(board["columns"] is JObject) || !((JObject)board["columns"]).HasValues)
                throw new UIMS.Services.FirebaseException(409, "The task board column definitions are malformed.");
            var columnIds = new HashSet<string>(StringComparer.Ordinal); var columnOrders = new HashSet<int>();
            foreach (var column in ((JObject)board["columns"]).Properties())
            {
                CheckKey(column.Name);
                if (!(column.Value is JObject) || string.IsNullOrWhiteSpace(Text((JObject)column.Value, "title")) || Text((JObject)column.Value, "title").Length > 80)
                    throw new UIMS.Services.FirebaseException(409, "A task board column has an invalid title.");
                int order = Number((JObject)column.Value, "order", -1);
                if (order < 0 || !columnOrders.Add(order) || !columnIds.Add(column.Name))
                    throw new UIMS.Services.FirebaseException(409, "Task board column ordering is invalid.");
            }
            if (board["positions"] == null || board["positions"].Type == JTokenType.Null) board["positions"] = new JObject();
            if (!(board["positions"] is JObject)) throw new UIMS.Services.FirebaseException(409, "Task board positions are malformed.");
            foreach (var position in ((JObject)board["positions"]).Properties())
            {
                CheckKey(position.Name);
                var value = position.Value as JObject;
                if (value == null || string.IsNullOrWhiteSpace(Text(value, "columnId")) || ReadBoardOrder(value) < 0)
                    throw new UIMS.Services.FirebaseException(409, "A task board position is malformed.");
            }
            if (board["operations"] == null || board["operations"].Type == JTokenType.Null) board["operations"] = new JObject();
            if (!(board["operations"] is JObject)) throw new UIMS.Services.FirebaseException(409, "Task board operation receipts are malformed.");
            foreach (var operation in ((JObject)board["operations"]).Properties())
            {
                CheckKey(operation.Name);
                if (!(operation.Value is JObject)) throw new UIMS.Services.FirebaseException(409, "A task board operation receipt is malformed.");
            }
            if (board["placementId"] == null) board["placementId"] = placementId;
            return board;
        }

        private static JObject DefaultBoardColumns()
        {
            return new JObject {
                ["todo"] = new JObject { ["title"] = "New / Pending", ["order"] = 0 },
                ["inProgress"] = new JObject { ["title"] = "In Progress", ["order"] = 1 },
                ["review"] = new JObject { ["title"] = "Review", ["order"] = 2 },
                ["done"] = new JObject { ["title"] = "Complete", ["order"] = 3 }
            };
        }

        private static string WorkflowColumn(JObject task)
        {
            switch (Status(Text(task, "status")))
            {
                case "completed": return "done";
                case "submitted": return "review";
                case "inprogress": case "revision": return "inProgress";
                default: return "todo";
            }
        }

        private static int ReadBoardOrder(JObject position)
        {
            int order = Number(position, "order", -1);
            if (order < 0) throw new UIMS.Services.FirebaseException(409, "A task board position has an invalid order value.");
            return order;
        }

        private static string BoardMoveFingerprint(string taskId, string columnId, string beforeTaskId)
        {
            string raw = taskId + "\n" + columnId + "\n" + (beforeTaskId ?? "");
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        private sealed class BoardItem
        {
            public string Id { get; set; }
            public string ColumnId { get; set; }
            public int Order { get; set; }
        }

        public async Task<JObject> StartAsync(string assignmentId, CancellationToken cancellation)
        {
            Ensure(true, "student"); CheckKey(assignmentId);
            var current = await Read(assignmentId, cancellation).ConfigureAwait(false); var row = current.Data;
            await EnsureStudentOwnsTaskAsync(row, cancellation).ConfigureAwait(false);
            RequireTaskOpen(row);
            DateTimeOffset opening;
            if (DateTimeOffset.TryParse(Text(row, "startsAt"), CultureInfo.InvariantCulture, DateTimeStyles.None, out opening) && DateTimeOffset.UtcNow < opening)
                throw new UIMS.Services.FirebaseException(409, "This task has not reached its scheduled start time.");
            var currentStatus = Status(Text(row, "status"));
            if (currentStatus == "inprogress") return new JObject { ["success"] = true };
            if (currentStatus != "assigned") throw new UIMS.Services.FirebaseException(409, "This task cannot be started in its current status.");
            row["status"] = "In Progress";
            await Write("PUT", assignmentId, row, current.ETag, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true };
        }

        public async Task<JObject> SubmitAsync(string assignmentId, string description, string link, string fileName, CancellationToken cancellation)
        {
            return await SubmitAsync(assignmentId, description, link, fileName, null, cancellation).ConfigureAwait(false);
        }

        private async Task<JObject> SubmitAsync(string assignmentId, string description, string link, string fileName, IList<string> attachmentIds, CancellationToken cancellation)
        {
            Ensure(true, "student"); CheckKey(assignmentId);
            bool hasAttachment = attachmentIds != null && attachmentIds.Count > 0;
            description = description ?? ""; link = link ?? "";
            if ((!hasAttachment && string.IsNullOrWhiteSpace(description)) || description.Trim().Length > 3000)
                throw new UIMS.Services.FirebaseException(400, "Enter a work description up to 3000 characters, or attach your work.");
            Uri parsed;
            if ((!hasAttachment && string.IsNullOrWhiteSpace(link)) ||
                (!string.IsNullOrWhiteSpace(link) && (!Uri.TryCreate(link, UriKind.Absolute, out parsed) || (parsed.Scheme != "https" && parsed.Scheme != "http"))))
                throw new UIMS.Services.FirebaseException(400, "Attach your work or enter a valid HTTP or HTTPS work link.");
            var gate = AssignmentLocks.GetOrAdd(assignmentId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var current = await Read(assignmentId, cancellation).ConfigureAwait(false); var task = current.Data;
                await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
                RequireStandardTask(task);
                RequireSubmissionDeadline(task);
                var status = Status(Text(task, "status"));
                if (status != "inprogress" && status != "revision") throw new UIMS.Services.FirebaseException(409, "Start the task first, or wait until the supervisor requests a revision.");
                var history = TaskSubmissionHistory.Read(task["submissions"]);
                int revision = history.NextRevision;
                var submission = new JObject { ["description"] = description.Trim(), ["link"] = link.Trim(),
                    ["fileName"] = (fileName ?? "").Trim(), ["submittedAt"] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
                submission["instructionVersion"] = (int?)task["instructionVersion"] ?? 1;
                submission["deadlineAtSubmission"] = task["dueDate"]?.DeepClone();
                if (attachmentIds != null && attachmentIds.Count > 0) submission["attachmentIds"] = new JArray(attachmentIds);
                history.Append(submission);
                task["submissions"] = history.ToJson(); task["status"] = "Submitted";
                AddTaskActivity(task, "submitted", "Student submission " + revision.ToString(CultureInfo.InvariantCulture) + " is ready for review.");
                await Write("PUT", assignmentId, task, current.ETag, cancellation).ConfigureAwait(false);
                return new JObject { ["success"] = true, ["revision"] = revision };
            }
            finally { gate.Release(); }
        }

        public Task<JObject> CreateAsync(string title, string description, string deadline, string placementId, string criteria, int maximum, CancellationToken cancellation)
        { return CreateInstanceAsync(title, description, deadline, placementId, criteria, maximum, null, cancellation); }

        private async Task<JObject> CreateInstanceAsync(string title, string description, string deadline, string placementId, string criteria, int maximum, string batchId, CancellationToken cancellation, string startsAt = null, string taskType = "standard")
        {
            Ensure(true, "companySupervisor");
            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 150 || string.IsNullOrWhiteSpace(description) || description.Trim().Length > 3000 || string.IsNullOrWhiteSpace(criteria) || criteria.Trim().Length > 500 || maximum < 1 || maximum > 1000)
                throw new UIMS.Services.FirebaseException(400, "Complete the task title, requirements, criteria and a score from 1 to 1000.");
            CheckKey(placementId);
            DateTimeOffset parsed; if (!DateTimeOffset.TryParse(deadline, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed)) throw new UIMS.Services.FirebaseException(400, "Choose a valid task deadline.");
            DateTimeOffset starts;
            if (startsAt == null) starts = DateTimeOffset.UtcNow;
            else if (!DateTimeOffset.TryParse(startsAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out starts) || starts >= parsed)
                throw new UIMS.Services.FirebaseException(400, "The task start must be a valid date before its deadline.");
            if (taskType != "standard" && taskType != "exam") throw new UIMS.Services.FirebaseException(400, "Choose a valid task type.");
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            var student = Text(placement, "studentId");
            var placementStatus = Text(placement, "status");
            if (placement.Count == 0 || Text(placement, "companySupervisorId") != actor.CompanySupervisorId ||
                (!placementStatus.Equals("active", StringComparison.OrdinalIgnoreCase) && !placementStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase)) || student.Length == 0)
                throw new UIMS.Services.FirebaseException(403, "Choose an active placement assigned to this company supervisor.");
            var profiles = (await ReadAt("users", "", cancellation).ConfigureAwait(false)).Data;
            var profile = FindStudentProfile(profiles, student, student);
            if (profile == null) throw new UIMS.Services.FirebaseException(404, "The placement does not resolve to a student profile in /users.");
            if (actor.CompanyId.Length > 0 && Text(placement, "companyId").Length > 0 && Text(placement, "companyId") != actor.CompanyId)
                throw new UIMS.Services.FirebaseException(403, "The placement belongs to a different company.");
            string taskId = "UIMS-TASK-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            if (batchId != null) taskId = "BULK-" + batchId + "-" + placementId;
            var task = new JObject { ["id"] = taskId, ["title"] = title.Trim(), ["description"] = description.Trim(),
                ["studentId"] = student, ["placementId"] = placementId, ["assignedBy"] = actor.CompanySupervisorId, ["assignedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["dueDate"] = parsed.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), ["status"] = "Assigned", ["isDemo"] = false,
                ["evaluationCriteria"] = criteria.Trim(), ["maximumScore"] = maximum, ["submissions"] = new JArray() };
            if (batchId != null) task["batchId"] = batchId;
            task["startsAt"] = starts.ToUniversalTime().ToString("o"); task["taskType"] = taskType;
            string creationSignature;
            using (var hash = System.Security.Cryptography.SHA256.Create())
                creationSignature = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(new JArray(title.Trim(), description.Trim(), parsed.ToUniversalTime().ToString("o"), criteria.Trim(), maximum, placementId).ToString(Formatting.None))));
            if (startsAt != null || taskType != "standard") creationSignature += "|" + starts.ToUniversalTime().ToString("o") + "|" + taskType;
            task["creationSignature"] = creationSignature;
            AddTaskActivity(task, "assigned", "A new task was assigned to you.");
            var current = await Read(taskId, cancellation).ConfigureAwait(false);
            if (batchId != null && current.Data.Count > 0)
            {
                await EnsureCompanyOwnsTaskAsync(current.Data, cancellation).ConfigureAwait(false);
                if (Text(current.Data, "batchId") != batchId || Text(current.Data, "placementId") != placementId)
                    throw new UIMS.Services.FirebaseException(409, "Batch ID conflicts with an existing task.");
                if (Text(current.Data, "creationSignature") != creationSignature)
                    throw new UIMS.Services.FirebaseException(409, "This batch was already used with different task content. Finish the original operation before creating a new assignment.");
                return new JObject { ["success"] = true, ["id"] = taskId, ["reused"] = true };
            }
            if (current.Data.Count > 0) throw new UIMS.Services.FirebaseException(409, "A task with this generated ID already exists. Try again.");
            await Write("PUT", taskId, task, "null_etag", cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true, ["id"] = taskId };
        }

        private static DateTimeOffset TaskDeadline(JObject task)
        {
            string raw = FirstText(task, "dueDate", "deadline"); DateTimeOffset date;
            if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date)) return DateTimeOffset.MaxValue;
            return raw.Length == 10 ? date.AddDays(1) : date;
        }
        // Exam results are exposed exclusively by the release-gated exam receipt.
        private static bool IsExamTask(JObject task)
        { return Text(task, "taskType") == "exam" || Text(task, "examId").Length > 0 || Text(task, "examWorkflowStatus").Length > 0; }

        private static void RequireStandardTask(JObject task)
        {
            if (Text(task, "taskType") == "exam" || Text(task, "examWorkflowStatus").Length > 0)
                throw new UIMS.Services.FirebaseException(409, "Use the examination submission and result approval workflow for this task.");
        }

        private static void RequireSubmissionDeadline(JObject task)
        { RequireTaskOpen(task); if (DateTimeOffset.UtcNow >= TaskDeadline(task)) throw new UIMS.Services.FirebaseException(409, "The submission deadline has passed. Request an extension."); }
        private static void RequireTaskOpen(JObject task)
        { if (Text(task, "archivedAt").Length > 0) throw new UIMS.Services.FirebaseException(409, "This task is archived and is available as read-only history."); }
        private static bool HasTaskGrade(JObject task)
        { return task["score"] != null && task["score"].Type != JTokenType.Null || Status(Text(task, "status")) == "completed"; }
        private static string FutureDeadline(string value)
        {
            DateTimeOffset date;
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date) || date <= DateTimeOffset.UtcNow)
                throw new UIMS.Services.FirebaseException(400, "Choose a future deadline including date, time and timezone.");
            return date.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }
        private void AddTaskActivity(JObject task, string kind, string detail)
        {
            var history = task["activity"] as JObject ?? new JObject();
            history[Guid.NewGuid().ToString("N")] = new JObject { ["kind"] = kind, ["detail"] = detail, ["actorUid"] = actor.Uid, ["at"] = DateTimeOffset.UtcNow.ToString("o"), ["studentId"] = task["studentId"] };
            task["activity"] = history;
            task["updatedAt"] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var notifications = task["notifications"] as JObject ?? new JObject();
            notifications[Guid.NewGuid().ToString("N")] = new JObject { ["kind"] = kind, ["message"] = detail, ["createdAt"] = DateTimeOffset.UtcNow.ToString("o"), ["recipientRole"] = actor.Role == "student" ? "companySupervisor" : "student", ["recipientId"] = actor.Role == "student" ? task["assignedBy"] : task["studentId"] };
            task["notifications"] = notifications;
        }
        // Creation plans contain answer keys and stay in the EXISTING private collection.
        // Progress is reconciled from business records, never accepted from the browser.
        private string CreationPath(string batchId = null)
        { CheckKey(actor.CompanySupervisorId); if (batchId != null) CheckKey(batchId); return "creationOperations/" + actor.CompanySupervisorId + (batchId == null ? "" : "/" + batchId); }
        private static JToken CanonicalCreation(JToken value)
        {
            if (value is JObject) return new JObject(((JObject)value).Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, CanonicalCreation(p.Value))));
            if (value is JArray) return new JArray(((JArray)value).Select(CanonicalCreation));
            return value.DeepClone();
        }
        private static JArray CreationFiles(JObject plan, string field) { return plan[field] as JArray ?? new JArray(); }
        private static void ValidateCreationFiles(JArray files, bool questions)
        {
            if (files.Count > 100) throw new UIMS.Services.FirebaseException(400, "Too many creation files.");
            foreach (var file in files.OfType<JObject>())
            {
                string hash = Text(file, "sha256");
                if (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c)) || Text(file, "name").Length == 0 || Text(file, "name").Length > 255 ||
                    ((long?)file["size"] ?? 0) <= 0 || ((long?)file["size"] ?? 0) > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes)
                    throw new UIMS.Services.FirebaseException(400, "Invalid creation file fingerprint.");
                UIMS.Services.FirebaseFileStorageService.SanitizeFileName(Text(file,"name"));
                CheckKey(Text(file, questions ? "questionId" : "id"));
            }
            if (files.Any(file => !(file is JObject))) throw new UIMS.Services.FirebaseException(400, "Invalid creation files.");
        }
        private async Task ValidateCreationOwner(JObject operation, CancellationToken cancellation)
        {
            if (Text(operation, "ownerUid") != actor.Uid || Text(operation, "companyId") != (actor.CompanyId ?? "")) throw new UIMS.Services.FirebaseException(403, "This creation operation belongs to another supervisor.");
            var placements = operation["plan"]?["payload"]?["placementIds"] as JArray;
            if (placements == null || !placements.HasValues) throw new UIMS.Services.FirebaseException(409, "The creation plan is incomplete.");
            foreach (string id in placements.Values<string>()) EnsureBoardPlacementAccess((await ReadAt("placements", id, cancellation).ConfigureAwait(false)).Data, id);
        }
        public async Task<JObject> BeginCreationAsync(JObject plan, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor");
            if (plan == null || Encoding.UTF8.GetByteCount(plan.ToString(Formatting.None)) > 512 * 1024) throw new UIMS.Services.FirebaseException(400, "Invalid creation plan.");
            var payload = plan["payload"] as JObject; string batchId = Text(payload, "batchId"); CheckKey(batchId);
            var placements = payload?["placementIds"] as JArray;
            if (batchId.Length > 80 || placements == null || placements.Count < 1 || placements.Count > 200 || placements.Any(p => p.Type != JTokenType.String) || placements.Values<string>().Distinct().Count() != placements.Count)
                throw new UIMS.Services.FirebaseException(400, "Select valid independent placements.");
            foreach (string id in placements.Values<string>()) CheckKey(id);
            ValidateCreationFiles(CreationFiles(plan, "resources"), false); ValidateCreationFiles(CreationFiles(plan, "questionImages"), true);
            if (string.IsNullOrWhiteSpace(Text(payload,"title")) || Text(payload,"title").Length > 150 || string.IsNullOrWhiteSpace(Text(payload,"description")) || Text(payload,"description").Length > 3000 || Text(payload,"criteria").Length > 500 || Number(payload,"maximum",0) < 1 || Number(payload,"maximum",0) > 1000) throw new UIMS.Services.FirebaseException(400, "Invalid task creation fields.");
            if (CreationFiles(plan,"resources").OfType<JObject>().Select(file => Text(file,"id")).Distinct().Count() != CreationFiles(plan,"resources").Count) throw new UIMS.Services.FirebaseException(400,"Duplicate resource identifiers.");
            if (Text(payload, "taskType") == "exam" && !(plan["exam"] is JObject)) throw new UIMS.Services.FirebaseException(400, "The exam definition is required for recovery.");
            if (Text(payload,"taskType") == "exam") TaskExamService.ValidateCreationDefinition((JObject)plan["exam"]);
            string signature;
            using (var hash = System.Security.Cryptography.SHA256.Create()) signature = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(CanonicalCreation(plan).ToString(Formatting.None))));
            var operation = new JObject { ["batchId"] = batchId, ["ownerUid"] = actor.Uid, ["companyId"] = actor.CompanyId ?? "", ["signature"] = signature,
                ["plan"] = plan.DeepClone(), ["status"] = "incomplete", ["createdAt"] = DateTimeOffset.UtcNow.ToString("o") };
            await ValidateCreationOwner(operation, cancellation).ConfigureAwait(false);
            var path = CreationPath(batchId); var existing = await ReadAt("taskExamAnswerKeys", path, cancellation).ConfigureAwait(false);
            if (existing.Data.Count == 0)
            {
                try { await WriteAt("taskExamAnswerKeys", "PUT", path, operation, existing.ETag ?? "null_etag", cancellation).ConfigureAwait(false); }
                catch (UIMS.Services.FirebaseException error) when (error.StatusCode == 409) { existing = await ReadAt("taskExamAnswerKeys", path, cancellation).ConfigureAwait(false); if (Text(existing.Data, "signature") != signature) throw; }
            }
            else if (Text(existing.Data, "signature") != signature) throw new UIMS.Services.FirebaseException(409, "This creation ID already has a different immutable plan.");
            return await CreationOperationAsync(batchId, false, cancellation).ConfigureAwait(false);
        }
        public async Task<JObject> ListCreationOperationsAsync(CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            var records = (await ReadAt("taskExamAnswerKeys", CreationPath(), cancellation).ConfigureAwait(false)).Data; var items = new JArray();
            foreach (var entry in records.Properties().Where(p => p.Value is JObject && Text(p.Value, "status") != "complete"))
            {
                var record = (JObject)entry.Value;
                if (Text(record, "ownerUid") != actor.Uid || Text(record, "companyId") != (actor.CompanyId ?? "")) continue;
                items.Add(new JObject { ["batchId"] = entry.Name, ["title"] = record["plan"]?["payload"]?["title"], ["createdAt"] = record["createdAt"] });
            }
            return new JObject { ["operations"] = items };
        }
        public async Task<JObject> CreationOperationAsync(string batchId, bool checkpoint, CancellationToken cancellation)
        {
            Ensure(checkpoint, "companySupervisor"); var path = CreationPath(batchId);
            var current = await ReadAt("taskExamAnswerKeys", path, cancellation).ConfigureAwait(false); var operation = current.Data;
            if (!operation.HasValues) throw new UIMS.Services.FirebaseException(404, "Creation operation not found.");
            await ValidateCreationOwner(operation, cancellation).ConfigureAwait(false);
            var plan = (JObject)operation["plan"]; var payload = (JObject)plan["payload"];
            var tasks = (await ReadAt("tasks", "", cancellation).ConfigureAwait(false)).Data;
            bool examRequired = Text(payload, "taskType") == "exam";
            var exams = examRequired ? (await ReadAt("taskExams", "", cancellation).ConfigureAwait(false)).Data : new JObject();
            var files = (CreationFiles(plan, "resources").HasValues || CreationFiles(plan, "questionImages").HasValues) ? (await ReadAt("fileAttachments", "", cancellation).ConfigureAwait(false)).Data : new JObject();
            var progress = new JArray(); bool complete = true;
            foreach (string placementId in ((JArray)payload["placementIds"]).Values<string>())
            {
                string taskId = "BULK-" + batchId + "-" + placementId; var task = tasks[taskId] as JObject; bool saved = task != null;
                if (saved) { await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false); if (Text(task, "batchId") != batchId || Text(task, "placementId") != placementId) throw new UIMS.Services.FirebaseException(409, "A task ID belongs to a different creation plan."); }
                var resources = new JObject(); var images = new JObject(); string examId = ""; JObject exam = null;
                if (examRequired)
                {
                    using (var hash = System.Security.Cryptography.SHA256.Create()) examId = "EX-" + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(actor.CompanySupervisorId + "|" + placementId + "|" + taskId + "|" + batchId))).Replace("-", "").ToLowerInvariant();
                    exam = exams[examId] as JObject;
                    if (exam != null && (Text(exam, "taskId") != taskId || Text(exam, "placementId") != placementId || Text(exam, "createdBy") != actor.CompanySupervisorId)) throw new UIMS.Services.FirebaseException(403, "The examination belongs to another assignment.");
                }
                foreach (var entry in files.Properties().Where(p => p.Value is JObject))
                {
                    var file = (JObject)entry.Value;
                    if (Text(file, "status") != "ready" || Text(file, "taskId") != taskId) continue;
                    foreach (var expected in CreationFiles(plan, "resources").OfType<JObject>())
                        if (Text(file, "purpose") == "taskAttachment" && Text(file, "sha256") == Text(expected, "sha256") && Text(file, "fileName") == UIMS.Services.FirebaseFileStorageService.SanitizeFileName(Text(expected, "name")) && (long?)file["sizeBytes"] == (long?)expected["size"] && (task?["attachmentIds"] as JArray ?? new JArray()).Values<string>().Contains(entry.Name)) resources[Text(expected, "id")] = entry.Name;
                    foreach (var expected in CreationFiles(plan, "questionImages").OfType<JObject>())
                        if (Text(file, "purpose") == "examQuestionImage" && Text(file, "examId") == examId && Text(file, "questionId") == Text(expected, "questionId") && Text(file, "sha256") == Text(expected, "sha256") && (long?)file["sizeBytes"] == (long?)expected["size"])
                        {
                            string questionId = Text(expected,"questionId");
                            string linked = Text(exam?["draft"]?["questions"]?[questionId],"imageFileId");
                            if (images[questionId] == null || linked == entry.Name) images[questionId] = entry.Name;
                        }
                }
                bool resourcesReady = CreationFiles(plan, "resources").OfType<JObject>().All(f => resources[Text(f, "id")] != null);
                bool published = exam != null && Text(exam, "status") == "published";
                if (published) published = CreationFiles(plan,"questionImages").OfType<JObject>().All(file => images[Text(file,"questionId")] != null && Text(exam["draft"]?["questions"]?[Text(file,"questionId")],"imageFileId") == (string)images[Text(file,"questionId")]);
                complete &= saved && resourcesReady && (!examRequired || published);
                progress.Add(new JObject { ["placementId"] = placementId, ["taskId"] = taskId, ["taskSaved"] = saved, ["resourceFiles"] = resources, ["resourcesReady"] = resourcesReady, ["examId"] = examId, ["examPublished"] = published, ["images"] = images });
            }
            string status = complete ? "complete" : "incomplete";
            if (checkpoint)
            {
                operation["status"] = status; operation["progress"] = progress.DeepClone(); operation["updatedAt"] = DateTimeOffset.UtcNow.ToString("o");
                await WriteAt("taskExamAnswerKeys", "PUT", path, operation, current.ETag, cancellation).ConfigureAwait(false);
            }
            return new JObject { ["batchId"] = batchId, ["plan"] = plan.DeepClone(), ["progress"] = progress, ["status"] = status };
        }

        public async Task<JObject> BulkCreateAsync(JObject input, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); string batch = (string)input["batchId"]; CheckKey(batch);
            if (string.IsNullOrWhiteSpace(batch) || batch.Length > 80) throw new UIMS.Services.FirebaseException(400, "A stable batch identifier is required.");
            var ids = input["placementIds"] as JArray;
            if (ids == null || ids.Count == 0 || ids.Count > 200) throw new UIMS.Services.FirebaseException(400, "Select between 1 and 200 placements.");
            if (ids.Any(id => id.Type != JTokenType.String) || input["maximum"]?.Type != JTokenType.Integer) throw new UIMS.Services.FirebaseException(400, "Placements must be identifiers and maximum score must be a whole number.");
            var results = new JArray();
            foreach (string placement in ids.Values<string>().Distinct())
            {
                try { var result = await CreateInstanceAsync((string)input["title"], (string)input["description"], (string)input["deadline"], placement, (string)input["criteria"], (int?)input["maximum"] ?? 0, batch, cancellation, (string)input["startsAt"], (string)input["taskType"] ?? "standard").ConfigureAwait(false); result["placementId"] = placement; results.Add(result); }
                catch (UIMS.Services.FirebaseException ex) { results.Add(new JObject { ["placementId"] = placement, ["success"] = false, ["message"] = ex.Message }); }
            }
            return new JObject { ["batchId"] = batch, ["results"] = results, ["success"] = results.All(r => (bool?)r["success"] == true) };
        }
        public async Task<JObject> EditAsync(JObject input, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); string id = (string)input["taskId"]; CheckKey(id);
            var origin = await Read(id, cancellation).ConfigureAwait(false); await EnsureCompanyOwnsTaskAsync(origin.Data, cancellation).ConfigureAwait(false);
            string title = (string)input["title"], description = (string)input["description"], criteria = (string)input["criteria"];
            if (string.IsNullOrWhiteSpace(title) || title.Length > 150 || string.IsNullOrWhiteSpace(description) || description.Length > 3000 || string.IsNullOrWhiteSpace(criteria) || criteria.Length > 500)
                throw new UIMS.Services.FirebaseException(400, "Title, description and instructions are required within their field limits.");
            string due = FutureDeadline((string)input["deadline"]);
            bool resubmit = (bool?)input["resubmit"] == true;
            var targets = new List<string> { id };
            if ((bool?)input["all"] == true)
            {
                string batch = Text(origin.Data, "batchId"); if (batch.Length == 0) throw new UIMS.Services.FirebaseException(400, "This task has no bulk assignment group.");
                var records = await ReadData("", cancellation).ConfigureAwait(false);
                targets = records.Properties().Where(r => r.Value is JObject && Text(r.Value, "batchId") == batch && Text(r.Value, "assignedBy") == actor.CompanySupervisorId).Select(r => r.Name).ToList();
            }
            var affected = new JArray(); var excluded = new JArray(); var failed = new JArray();
            foreach (string target in targets)
            {
                try
                {
                var current = await Read(target, cancellation).ConfigureAwait(false); var row = current.Data;
                await EnsureCompanyOwnsTaskAsync(row, cancellation).ConfigureAwait(false);
                RequireTaskOpen(row);
                if (HasTaskGrade(row)) { excluded.Add(target); continue; }
                string individualDeadline = input["deadlines"] is JObject ? (string)input["deadlines"][target] : null;
                string effectiveDue = individualDeadline == null ? due : FutureDeadline(individualDeadline);
                if (resubmit && TaskSubmissionHistory.Read(row["submissions"]).HasSubmissions && DateTimeOffset.Parse(effectiveDue, CultureInfo.InvariantCulture) == TaskDeadline(row))
                    throw new UIMS.Services.FirebaseException(400, "Choose a different future deadline for the student's new submission.");
                var versions = InstructionHistory(row["instructionVersions"]);
                int version = (int?)row["instructionVersion"] ?? 1;
                versions[version.ToString(CultureInfo.InvariantCulture)] = new JObject { ["title"] = row["title"], ["description"] = row["description"], ["evaluationCriteria"] = row["evaluationCriteria"], ["dueDate"] = row["dueDate"], ["attachmentIds"] = row["attachmentIds"]?.DeepClone(), ["savedAt"] = DateTimeOffset.UtcNow.ToString("o") };
                row["instructionVersions"] = versions; row["instructionVersion"] = version + 1;
                if (row["originalDeadline"] == null) row["originalDeadline"] = (row["dueDate"] ?? row["deadline"])?.DeepClone();
                row["title"] = title.Trim(); row["description"] = description.Trim(); row["evaluationCriteria"] = criteria.Trim(); row["dueDate"] = effectiveDue;
                if (resubmit && TaskSubmissionHistory.Read(row["submissions"]).HasSubmissions) row["status"] = "Revision Needed";
                AddTaskActivity(row, resubmit ? "resubmissionRequested" : "instructionsUpdated", "Supervisor updated task instructions and deadline.");
                await Write("PUT", target, row, current.ETag, cancellation).ConfigureAwait(false); affected.Add(target);
                }
                catch (UIMS.Services.FirebaseException error) { failed.Add(new JObject { ["taskId"] = target, ["message"] = error.Message }); }
            }
            return new JObject { ["success"] = failed.Count == 0, ["affected"] = affected, ["excluded"] = excluded, ["failed"] = failed };
        }
        public async Task<JObject> ExtensionAsync(JObject input, CancellationToken cancellation)
        {
            Ensure(true); string id = (string)input["taskId"]; CheckKey(id);
            var current = await Read(id, cancellation).ConfigureAwait(false); var row = current.Data;
            var extension = row["extension"] as JObject;
            if (actor.Role == "student")
            {
                await EnsureStudentOwnsTaskAsync(row, cancellation).ConfigureAwait(false);
                RequireTaskOpen(row);
                if (DateTimeOffset.UtcNow < TaskDeadline(row) || HasTaskGrade(row) || Status(Text(row, "status")) == "submitted") throw new UIMS.Services.FirebaseException(409, "Extensions are available for overdue work awaiting submission.");
                if (extension != null && (Text(extension, "status") == "pending" || Text(extension, "status") == "rejected")) throw new UIMS.Services.FirebaseException(409, "A request is already pending or was finally rejected.");
                string reason = (string)input["reason"];
                if (string.IsNullOrWhiteSpace(reason) || reason.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Enter an extension reason up to 3000 characters.");
                extension = new JObject { ["status"] = "pending", ["reason"] = reason.Trim(), ["proposedDeadline"] = FutureDeadline((string)input["deadline"]), ["oldDeadline"] = (row["dueDate"] ?? row["deadline"])?.DeepClone(), ["requestedAt"] = DateTimeOffset.UtcNow.ToString("o") };
            }
            else if (actor.Role == "companySupervisor")
            {
                await EnsureCompanyOwnsTaskAsync(row, cancellation).ConfigureAwait(false);
                RequireTaskOpen(row);
                if (extension == null || Text(extension, "status") != "pending") throw new UIMS.Services.FirebaseException(409, "No pending extension request exists.");
                bool approve = (bool?)input["approve"] == true; string reason = (string)input["reason"] ?? "";
                if ((!approve && string.IsNullOrWhiteSpace(reason)) || reason.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Provide a rejection reason up to 3000 characters.");
                extension["status"] = approve ? "approved" : "rejected"; extension["decisionReason"] = reason.Trim(); extension["decidedAt"] = DateTimeOffset.UtcNow.ToString("o"); extension["decidedBy"] = actor.Uid;
                if (approve) { string due = FutureDeadline((string)input["deadline"]); extension["approvedDeadline"] = due; row["dueDate"] = due; }
            }
            else throw new UIMS.Services.FirebaseException(403, "This role cannot decide task extensions.");
            row["extension"] = extension;
            var history = row["extensionHistory"] as JObject ?? new JObject(); history[Guid.NewGuid().ToString("N")] = extension.DeepClone(); row["extensionHistory"] = history;
            AddTaskActivity(row, "extension" + Text(extension, "status"), Text(extension, "reason"));
            await Write("PUT", id, row, current.ETag, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true, ["extension"] = extension };
        }

        public async Task<JObject> ArchiveAsync(string taskId, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); CheckKey(taskId);
            var current = await Read(taskId, cancellation).ConfigureAwait(false); var task = current.Data;
            await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            if (Text(task, "archivedAt").Length > 0) return new JObject { ["success"] = true, ["duplicate"] = true };
            task["archivedAt"] = DateTimeOffset.UtcNow.ToString("o"); task["archivedBy"] = actor.Uid;
            AddTaskActivity(task, "archived", "The supervisor archived this task. Your saved work and grades remain available.");
            await Write("PUT", taskId, task, current.ETag, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true };
        }

        private static JObject InstructionHistory(JToken value)
        {
            if (value is JObject) return (JObject)value.DeepClone();
            var result = new JObject(); var array = value as JArray;
            if (array != null) for (int index = 0; index < array.Count; index++)
                if (array[index] != null && array[index].Type != JTokenType.Null)
                    result[index.ToString(CultureInfo.InvariantCulture)] = array[index].DeepClone();
            return result;
        }

        public async Task<JObject> ReviewAsync(string assignmentId, string action, int score, string feedback, CancellationToken cancellation, string revisionDeadline = null)
        {
            Ensure(true, "companySupervisor"); CheckKey(assignmentId);
            if (action != "revision" && action != "grade") throw new UIMS.Services.FirebaseException(400, "Invalid task review action.");
            if (feedback == null || feedback.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Feedback must be 3000 characters or fewer.");
            var gate = AssignmentLocks.GetOrAdd(assignmentId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var current = await Read(assignmentId, cancellation).ConfigureAwait(false); var task = current.Data;
                await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
                RequireStandardTask(task);
                RequireTaskOpen(task);
                var submissions = TaskSubmissionHistory.Read(task["submissions"]);
                if (Status(Text(task, "status")) != "submitted" || !submissions.HasSubmissions)
                    throw new UIMS.Services.FirebaseException(409, "Only a task with a saved student submission can be reviewed.");
                var maximum = Number(task, "maximumScore", 100);
                if (action == "grade" && (score < 0 || score > maximum))
                    throw new UIMS.Services.FirebaseException(400, "The score must be between 0 and " + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                var previousFeedback = task["feedback"]?.DeepClone();
                task["feedback"] = feedback.Trim();
                if (action == "revision")
                {
                    string due = FutureDeadline(revisionDeadline);
                    if (task["originalDeadline"] == null) task["originalDeadline"] = task["dueDate"]?.DeepClone();
                    AddTaskActivity(task, "resubmissionRequested", "New submission requested. Previous deadline: " + Text(task, "dueDate") + "; new deadline: " + due);
                    task["dueDate"] = due; task["status"] = "Revision Needed";
                }
                else
                {
                    var grades = task["gradeHistory"] as JObject ?? new JObject();
                    if (HasTaskGrade(task) && !grades.HasValues) grades["legacy"] = new JObject { ["score"] = task["score"]?.DeepClone(), ["maximumScore"] = task["maximumScore"]?.DeepClone(), ["feedback"] = previousFeedback };
                    grades[Guid.NewGuid().ToString("N")] = new JObject { ["score"] = score, ["maximumScore"] = maximum, ["feedback"] = feedback.Trim(), ["submissionRevision"] = submissions.Records.Last()["revision"], ["gradedAt"] = DateTimeOffset.UtcNow.ToString("o"), ["gradedBy"] = actor.Uid };
                    task["gradeHistory"] = grades; task["score"] = score; task["status"] = "Completed";
                    AddTaskActivity(task, "graded", "Your task was graded by the company supervisor.");
                }
                await Write("PUT", assignmentId, task, current.ETag, cancellation).ConfigureAwait(false);
                return new JObject { ["success"] = true };
            }
            finally { gate.Release(); }
        }

        public async Task<JObject> SubmitWithAttachmentAsync(string taskId, string description, string link, string fileName, string contentType, byte[] bytes, CancellationToken cancellation)
        {
            Ensure(true, "student"); CheckKey(taskId);
            var task = (await Read(taskId, cancellation).ConfigureAwait(false)).Data;
            await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            RequireStandardTask(task);
            RequireSubmissionDeadline(task);
            var status = Status(Text(task, "status"));
            if (status != "inprogress" && status != "revision") throw new UIMS.Services.FirebaseException(409, "Start the task first, or wait until the supervisor requests a revision.");
            string id = UIMS.Services.FirebaseFileStorageService.CreateFileId();
            var stored = await UploadAndRegisterAsync(taskId, task, id, "taskSubmission", fileName, contentType, bytes, cancellation).ConfigureAwait(false);
            try
            {
                var result = await SubmitAsync(taskId, description, link, fileName, new[] { id }, cancellation).ConfigureAwait(false);
                result["attachment"] = PublicFileMetadata((JObject)(await ReadAt("fileAttachments", id, cancellation).ConfigureAwait(false)).Data);
                return result;
            }
            catch
            {
                try { if (await IsTaskLinkedToFileAsync(taskId, id, "taskSubmission").ConfigureAwait(false))
                    return new JObject { ["success"] = true, ["revision"] = LatestFileRevision((await Read(taskId, CancellationToken.None).ConfigureAwait(false)).Data, id) }; }
                catch { }
                await CleanupUnlinkedFileAsync(id, stored.ObjectPath).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<JObject> UploadTaskAttachmentAsync(string taskId, string fileName, string contentType, byte[] bytes, CancellationToken cancellation)
        {
            Ensure(true, "companySupervisor"); CheckKey(taskId);
            var current = await Read(taskId, cancellation).ConfigureAwait(false); var task = current.Data;
            await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            RequireTaskOpen(task);
            if (HasTaskGrade(task)) throw new UIMS.Services.FirebaseException(409, "Resources are locked after this task is graded.");
            UIMS.Services.FirebaseFileStorageService.ValidateFile(fileName, contentType, bytes);
            string fingerprint;
            using (var hash = System.Security.Cryptography.SHA256.Create()) fingerprint = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            foreach (var existingId in (task["attachmentIds"] as JArray ?? new JArray()).Values<string>())
            {
                CheckKey(existingId);
                var metadata = (await ReadAt("fileAttachments", existingId, cancellation).ConfigureAwait(false)).Data;
                if (Text(metadata, "status") == "ready" && Text(metadata, "purpose") == "taskAttachment" && Text(metadata, "taskId") == taskId && Text(metadata, "sha256") == fingerprint && Text(metadata, "fileName") == fileName)
                    return new JObject { ["success"] = true, ["attachment"] = PublicFileMetadata(metadata), ["reused"] = true };
            }
            string id = UIMS.Services.FirebaseFileStorageService.CreateFileId();
            var stored = await UploadAndRegisterAsync(taskId, task, id, "taskAttachment", fileName, contentType, bytes, cancellation).ConfigureAwait(false);
            try
            {
                if (task["attachmentIds"] != null && task["attachmentIds"].Type != JTokenType.Array)
                    throw new UIMS.Services.FirebaseException(409, "The task attachment reference format is unsupported; no existing task data was changed.");
                var ids = task["attachmentIds"] as JArray ?? new JArray();
                if (ids.Values<string>().Contains(id, StringComparer.Ordinal)) throw new UIMS.Services.FirebaseException(409, "This file is already attached to the task.");
                ids.Add(id); task["attachmentIds"] = ids;
                await Write("PUT", taskId, task, current.ETag, cancellation).ConfigureAwait(false);
                var result = new JObject { ["success"] = true, ["attachment"] = PublicFileMetadata((JObject)(await ReadAt("fileAttachments", id, cancellation).ConfigureAwait(false)).Data) };
                return result;
            }
            catch
            {
                try { if (await IsTaskLinkedToFileAsync(taskId, id, "taskAttachment").ConfigureAwait(false))
                    return new JObject { ["success"] = true, ["attachment"] = PublicFileMetadata((JObject)(await ReadAt("fileAttachments", id, CancellationToken.None).ConfigureAwait(false)).Data) }; }
                catch { }
                await CleanupUnlinkedFileAsync(id, stored.ObjectPath).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<JObject> ListTaskFilesAsync(string taskId, CancellationToken cancellation)
        {
            Ensure(false); CheckKey(taskId);
            var task = (await Read(taskId, cancellation).ConfigureAwait(false)).Data;
            if (actor.Role == "student") await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            else if (actor.Role == "companySupervisor") await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            else throw new UIMS.Services.FirebaseException(403, "Task files are not available to this role.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var taskIds = task["attachmentIds"] as JArray;
            if (taskIds != null) foreach (var item in taskIds.Values<string>()) if (!string.IsNullOrWhiteSpace(item)) ids.Add(item);
            var history = TaskSubmissionHistory.Read(task["submissions"]);
            foreach (var submission in history.Records)
            {
                var attached = submission["attachmentIds"] as JArray;
                if (attached != null) foreach (var item in attached.Values<string>()) if (!string.IsNullOrWhiteSpace(item)) ids.Add(item);
            }
            var files = new JArray();
            foreach (string id in ids)
            {
                CheckKey(id);
                var metadata = (await ReadAt("fileAttachments", id, cancellation).ConfigureAwait(false)).Data as JObject;
                if (metadata != null && (string)metadata["taskId"] == taskId && (string)metadata["status"] == "ready") files.Add(PublicFileMetadata(metadata));
            }
            return new JObject { ["success"] = true, ["files"] = files };
        }

        public async Task<TaskFileDownload> DownloadTaskFileAsync(string fileId, CancellationToken cancellation)
        {
            Ensure(false); CheckKey(fileId);
            var metadata = (await ReadAt("fileAttachments", fileId, cancellation).ConfigureAwait(false)).Data as JObject;
            if (metadata == null || !metadata.HasValues || (string)metadata["status"] != "ready") throw new UIMS.Services.FirebaseException(404, "The requested task file is unavailable.");
            string taskId = Text(metadata, "taskId"); CheckKey(taskId);
            var task = (await Read(taskId, cancellation).ConfigureAwait(false)).Data;
            bool linked = false;
            if (Text(metadata, "purpose") == "taskAttachment")
            {
                var ids = task["attachmentIds"] as JArray;
                linked = ids != null && ids.Values<string>().Contains(fileId, StringComparer.Ordinal);
            }
            else if (Text(metadata, "purpose") == "taskSubmission")
            {
                foreach (var submission in TaskSubmissionHistory.Read(task["submissions"]).Records)
                {
                    var ids = submission["attachmentIds"] as JArray;
                    if (ids != null && ids.Values<string>().Contains(fileId, StringComparer.Ordinal)) { linked = true; break; }
                }
            }
            if (!linked) throw new UIMS.Services.FirebaseException(403, "This file is not linked to the requested task history.");
            if (actor.Role == "student") await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            else if (actor.Role == "companySupervisor") await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);
            else throw new UIMS.Services.FirebaseException(403, "Task files are not available to this role.");
            if (Text(metadata, "studentId") != Text(task, "studentId") || Text(metadata, "placementId") != Text(task, "placementId"))
                throw new UIMS.Services.FirebaseException(403, "The file metadata does not match the task relationship.");
            string objectPath = Text(metadata, "storagePath");
            byte[] content = await fileStorage.DownloadAsync(objectPath, Text(metadata, "storageProvider"), cancellation).ConfigureAwait(false);
            if (content.LongLength != Number(metadata, "sizeBytes", -1)) throw new UIMS.Services.FirebaseException(502, "The stored file size does not match its metadata.");
            return new TaskFileDownload { FileName = Text(metadata, "fileName"), ContentType = Text(metadata, "contentType"), Content = content };
        }

        private async Task<UIMS.Services.StoredFile> UploadAndRegisterAsync(string taskId, JObject task, string id, string purpose, string fileName, string contentType, byte[] bytes, CancellationToken cancellation)
        {
            string safeName = UIMS.Services.FirebaseFileStorageService.SanitizeFileName(fileName);
            string verifiedType = UIMS.Services.FirebaseFileStorageService.ValidateFile(safeName, contentType, bytes);
            string placementId = Text(task, "placementId"); CheckKey(placementId);
            CheckKey(taskId);
            string objectPath = "uims/task-attachments/" + placementId + "/" + taskId + "/" + id;
            var stored = await fileStorage.UploadAsync(objectPath, safeName, verifiedType, bytes, cancellation).ConfigureAwait(false);
            try
            {
                var metadata = new JObject { ["id"] = id, ["purpose"] = purpose, ["status"] = "ready", ["taskId"] = taskId,
                    ["placementId"] = placementId, ["studentId"] = Text(task, "studentId"), ["ownerUid"] = actor.Uid,
                    ["fileName"] = stored.FileName, ["contentType"] = stored.ContentType, ["sizeBytes"] = stored.SizeBytes,
                    ["sha256"] = stored.Sha256, ["storageProvider"] = stored.Provider, ["storagePath"] = stored.ObjectPath, ["storageGeneration"] = stored.Generation ?? "",
                    ["createdAt"] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
                await WriteAt("fileAttachments", "PUT", id, metadata, "null_etag", cancellation).ConfigureAwait(false);
                return stored;
            }
            catch
            {
                bool absent = false;
                try {
                    var confirmed = (await ReadAt("fileAttachments", id, CancellationToken.None).ConfigureAwait(false)).Data;
                    absent = !confirmed.HasValues;
                    if (Text(confirmed,"status")=="ready" && Text(confirmed,"storagePath")==stored.ObjectPath && Text(confirmed,"sha256")==stored.Sha256) return stored;
                } catch { /* Preserve the private object if the outcome is still unknown. */ }
                if (absent) await CleanupUnlinkedFileAsync(id, stored.ObjectPath).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<bool> IsTaskLinkedToFileAsync(string taskId, string fileId, string purpose)
        {
            var task = (await Read(taskId, CancellationToken.None).ConfigureAwait(false)).Data;
            if (purpose == "taskAttachment")
            {
                var ids = task["attachmentIds"] as JArray;
                return ids != null && ids.Values<string>().Contains(fileId, StringComparer.Ordinal);
            }
            foreach (var submission in TaskSubmissionHistory.Read(task["submissions"]).Records)
            {
                var ids = submission["attachmentIds"] as JArray;
                if (ids != null && ids.Values<string>().Contains(fileId, StringComparer.Ordinal)) return true;
            }
            return false;
        }

        private static int LatestFileRevision(JObject task, string fileId)
        {
            int revision = 0;
            foreach (var submission in TaskSubmissionHistory.Read(task["submissions"]).Records)
            {
                var ids = submission["attachmentIds"] as JArray;
                if (ids != null && ids.Values<string>().Contains(fileId, StringComparer.Ordinal)) revision = Number(submission, "revision", revision);
            }
            return revision;
        }

        private async Task CleanupUnlinkedFileAsync(string id, string objectPath)
        {
            try
            {
                var metadata = await ReadAt("fileAttachments", id, CancellationToken.None).ConfigureAwait(false);
                if (metadata.Data != null && metadata.Data.HasValues) await WriteAt("fileAttachments", "DELETE", id, null, metadata.ETag, CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* A failed cleanup leaves an unlinked private object; it is never downloadable through this API. */ }
            try { await fileStorage.DeleteObjectAsync(objectPath, CancellationToken.None).ConfigureAwait(false); } catch { }
        }

        private static JObject PublicFileMetadata(JObject metadata)
        {
            return new JObject { ["id"] = Text(metadata, "id"), ["purpose"] = Text(metadata, "purpose"),
                ["fileName"] = Text(metadata, "fileName"), ["contentType"] = Text(metadata, "contentType"),
                ["sizeBytes"] = Number(metadata, "sizeBytes", 0), ["createdAt"] = Text(metadata, "createdAt") };
        }
    }

    public sealed class TaskFileDownload
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }

    internal sealed class TaskRecord { public JObject Data { get; set; } public string ETag { get; set; } }

    // Existing installations may contain either the legacy revision array or a revision-keyed
    // object. Keep the wire shape for each record; new records use the established array shape.
    internal sealed class TaskSubmissionHistory
    {
        private readonly bool objectShape;
        private readonly List<JObject> records;
        private readonly List<int> revisions;

        private TaskSubmissionHistory(bool objectShape, List<JObject> records, List<int> revisions)
        { this.objectShape = objectShape; this.records = records; this.revisions = revisions; }

        internal bool HasSubmissions { get { return records.Count > 0; } }
        internal int NextRevision
        {
            get
            {
                int maximum = revisions.Count == 0 ? 0 : revisions.Max();
                if (maximum == int.MaxValue) throw InvalidHistory("The submission revision limit has been reached.");
                return maximum + 1;
            }
        }
        internal IEnumerable<JObject> Records
        {
            get
            {
                for (int i = 0; i < records.Count; i++)
                {
                    var copy = (JObject)records[i].DeepClone();
                    if (copy["revision"] == null && revisions[i] > 0) copy["revision"] = revisions[i];
                    yield return copy;
                }
            }
        }

        internal static TaskSubmissionHistory Read(JToken value)
        {
            bool hadValue = value != null && value.Type != JTokenType.Null;
            var rows = new List<JObject>(); var revisions = new List<int>(); var used = new HashSet<int>();
            if (!hadValue) return new TaskSubmissionHistory(false, rows, revisions);

            var array = value as JArray;
            if (array != null)
            {
                foreach (var token in array)
                {
                    var row = token as JObject;
                    if (row == null) throw InvalidHistory();
                    int revision;
                    if (TryRevision(row["revision"], out revision))
                    {
                        if (!used.Add(revision)) throw InvalidHistory("Duplicate submission revision.");
                        revisions.Add(revision);
                    }
                    else if (row["revision"] == null || row["revision"].Type == JTokenType.Null)
                    {
                        rows.Add((JObject)row.DeepClone()); revisions.Add(0);
                        continue;
                    }
                    else throw InvalidHistory();
                    rows.Add((JObject)row.DeepClone());
                }

                // Old array entries without a revision remain byte-for-byte equivalent as JSON.
                // Give them deterministic in-memory revision numbers for ordering and next-number
                // calculation, without changing those historical objects when we serialize.
                int synthetic = 1;
                for (int i = 0; i < rows.Count; i++)
                {
                    int actual;
                    if (TryRevision(rows[i]["revision"], out actual)) continue;
                    while (used.Contains(synthetic)) synthetic++;
                    used.Add(synthetic); revisions[i] = synthetic++;
                }
                return new TaskSubmissionHistory(false, rows, revisions);
            }

            var keyed = value as JObject;
            if (keyed == null) throw InvalidHistory();
            foreach (var property in keyed.Properties())
            {
                int keyRevision;
                if (!int.TryParse(property.Name, NumberStyles.None, CultureInfo.InvariantCulture, out keyRevision) || keyRevision < 1 ||
                    property.Name != keyRevision.ToString(CultureInfo.InvariantCulture) || !used.Add(keyRevision))
                    throw InvalidHistory("Submission object keys must be unique positive revision numbers.");
                var row = property.Value as JObject;
                if (row == null) throw InvalidHistory();
                int embeddedRevision;
                if (row["revision"] != null && row["revision"].Type != JTokenType.Null &&
                    (!TryRevision(row["revision"], out embeddedRevision) || embeddedRevision != keyRevision))
                    throw InvalidHistory("A keyed submission revision does not match its object key.");
                rows.Add((JObject)row.DeepClone()); revisions.Add(keyRevision);
            }
            return new TaskSubmissionHistory(true, rows, revisions);
        }

        internal void Append(JObject submission)
        {
            if (submission == null) throw new ArgumentNullException(nameof(submission));
            int revision = NextRevision;
            var copy = (JObject)submission.DeepClone(); copy["revision"] = revision;
            records.Add(copy); revisions.Add(revision);
        }

        internal JToken ToJson()
        {
            if (objectShape)
            {
                var result = new JObject();
                for (int i = 0; i < records.Count; i++)
                {
                    int revision;
                    if (!TryRevision(records[i]["revision"], out revision))
                        revision = revisions[i];
                    var copy = (JObject)records[i].DeepClone();
                    result[revision.ToString(CultureInfo.InvariantCulture)] = copy;
                }
                return result;
            }

            var array = new JArray();
            foreach (var record in records)
            {
                var copy = (JObject)record.DeepClone();
                array.Add(copy);
            }
            // Missing history and explicit null are normalized to the established array for a
            // new submission, while existing array records stay arrays.
            return array;
        }

        private static bool TryRevision(JToken token, out int revision)
        {
            revision = 0;
            if (token == null || token.Type != JTokenType.Integer) return false;
            long value;
            if (!long.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 1 || value > int.MaxValue) return false;
            revision = (int)value;
            return true;
        }
        private static UIMS.Services.FirebaseException InvalidHistory(string message = null)
        { return new UIMS.Services.FirebaseException(409, message ?? "Task submission history has an unsupported or malformed format. No data was changed."); }
    }

    public static class TaskActorResolver
    {
        public static async Task<TaskActor> ResolveAsync(HttpContext context, CancellationToken cancellation, bool allowAcademicSupervisor = false)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var configuration = new UIMS.Services.FirebaseConfiguration();
            configuration.EnsureAuthenticationEnabled();
            var identity = context.User == null ? null : context.User.Identity as System.Web.Security.FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired || string.IsNullOrWhiteSpace(identity.Name))
                throw new UIMS.Services.FirebaseException(401, "Sign in through Firebase Authentication before using task operations.");

            string role = University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
            if (role == null || !context.User.IsInRole(role)) throw new UIMS.Services.FirebaseException(403, "The signed-in account does not have a task role.");
            var gateway = new University_Internship_Management_System.Shared.StudentFirebaseGateway(configuration);
            context.Items["TaskApiStage"] = "verify-authentication-uid";
            if (!await gateway.IsActiveAuthenticationUidAsync(identity.Name).ConfigureAwait(false))
                throw new UIMS.Services.FirebaseException(401, "The Firebase Authentication UID no longer exists or is disabled.");
            cancellation.ThrowIfCancellationRequested();

            string key = University_Internship_Management_System.Shared.StudentManagementService.Key(identity.Name);
            context.Items["TaskApiStage"] = "read-verified-profile";
            var profile = await gateway.ReadAsync("users/" + key).ConfigureAwait(false) as JObject;
            if (profile == null || !profile.HasValues)
                throw new UIMS.Services.FirebaseException(403, "The authenticated account has no matching /users/{uid} profile.");

            string mappedSupervisorId = null;
            if (role == "companySupervisor" && string.IsNullOrWhiteSpace((string)profile["companySupervisorId"]))
            {
                context.Items["TaskApiStage"] = "resolve-company-supervisor";
                // Existing attendance records define this UID mapping; require exactly one active match.
                var supervisors = await gateway.ReadAsync("companySupervisors").ConfigureAwait(false) as JObject;
                var matches = supervisors == null ? new List<JProperty>() : supervisors.Properties().Where(item =>
                    item.Value is JObject && string.Equals((string)item.Value["firebaseUid"], identity.Name, StringComparison.Ordinal) &&
                    string.Equals((string)item.Value["status"], "active", StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count != 1) throw new UIMS.Services.FirebaseException(403, "The verified UID must map to exactly one active /companySupervisors record.");
                mappedSupervisorId = matches[0].Name;
            }
            cancellation.ThrowIfCancellationRequested();
            context.Items["TaskApiStage"] = "build-verified-task-actor";
            return TaskActor.FromVerifiedProfile(identity.Name, role, profile, mappedSupervisorId, allowAcademicSupervisor);
        }
    }

    public sealed class TasksApiHandler : System.Web.HttpTaskAsyncHandler
    {
        public static async Task<JObject> DispatchAsync(TaskService service, string method, string action, JObject data, CancellationToken cancellation)
        {
            if (method == "GET" && action == "creationOperations") return await service.ListCreationOperationsAsync(cancellation).ConfigureAwait(false);
            if (method == "GET" && action == "creationOperation") return await service.CreationOperationAsync((string)data["batchId"], false, cancellation).ConfigureAwait(false);
            if (method == "POST" && action == "beginCreation") return await service.BeginCreationAsync(data, cancellation).ConfigureAwait(false);
            if (method == "POST" && action == "checkpointCreation") return await service.CreationOperationAsync((string)data["batchId"], true, cancellation).ConfigureAwait(false);
            if (method == "GET" && action == "trainees") return await service.LoadTraineesAsync(cancellation).ConfigureAwait(false);
            if (method == "GET" && action == "traineeProgress") return await service.LoadTraineeProgressAsync((string)data["placementId"], cancellation).ConfigureAwait(false);
            if (service == null) throw new ArgumentNullException(nameof(service));
            data = data ?? new JObject();
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "board")
                return await service.LoadBoardAsync((string)data["placementId"], cancellation).ConfigureAwait(false);
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "attachments")
                return await service.ListTaskFilesAsync((string)data["taskId"], cancellation).ConfigureAwait(false);
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "storageAvailability")
                return await service.CheckStorageAvailabilityAsync(cancellation).ConfigureAwait(false);
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(action))
                return await service.LoadAsync(service.ActorRole, cancellation).ConfigureAwait(false);
            if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(405, "Unsupported task request.");
            if (action == "bulkCreate") return await service.BulkCreateAsync(data, cancellation).ConfigureAwait(false);
            if (action == "edit") return await service.EditAsync(data, cancellation).ConfigureAwait(false);
            if (action == "extension") return await service.ExtensionAsync(data, cancellation).ConfigureAwait(false);
            if (action == "archive") return await service.ArchiveAsync((string)data["taskId"], cancellation).ConfigureAwait(false);
            if (action == "columns") return await service.ReorderColumnsAsync((string)data["placementId"], data["columnIds"] as JArray, (string)data["operationId"], cancellation).ConfigureAwait(false);
            if (action == "move" && service.ActorRole == "companySupervisor") return await service.MoveBoardTaskAsync((string)data["placementId"], (string)data["taskId"], (string)data["columnId"], (string)data["beforeTaskId"], (string)data["operationId"], cancellation).ConfigureAwait(false);
            if (action == "start" && service.ActorRole == "student") return await service.StartAsync((string)data["assignmentId"], cancellation).ConfigureAwait(false);
            if (action == "submit" && service.ActorRole == "student") return await service.SubmitAsync((string)data["assignmentId"], (string)data["description"], (string)data["link"], (string)data["fileName"], cancellation).ConfigureAwait(false);
            if (action == "create" && service.ActorRole == "companySupervisor") return await service.CreateAsync((string)data["title"], (string)data["description"], (string)data["deadline"], (string)data["placementId"], (string)data["criteria"], (int?)data["maximum"] ?? 0, cancellation).ConfigureAwait(false);
            if (action == "review" && service.ActorRole == "companySupervisor") return await service.ReviewAsync((string)data["assignmentId"], (string)data["reviewAction"], (int?)data["score"] ?? 0, (string)data["feedback"] ?? "", cancellation, (string)data["deadline"]).ConfigureAwait(false);
            throw new UIMS.Services.FirebaseException(403, "This task operation is not permitted for the verified account role.");
        }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.SuppressFormsAuthenticationRedirect = true;
            context.Response.Cache.SetNoStore(); context.Response.ContentType = "application/json";
            try
            {
                if (context.User?.Identity?.IsAuthenticated != true)
                    throw new UIMS.Services.FirebaseException(401, "Your sign-in expired. Sign in again to load your tasks.");
                var config = new UIMS.Services.FirebaseConfiguration();
                context.Items["TaskApiStage"] = "validate-firebase-authentication";
                config.EnsureAuthenticationEnabled();
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.Response.ClientDisconnectedToken))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(45));
                    var cancellation = timeout.Token;
                    string method = context.Request.HttpMethod;
                    string action = string.IsNullOrEmpty(context.Request.QueryString["action"]) ? null : context.Request.QueryString["action"];
                    JObject data = new JObject();
                    if (method == "GET" && action == "creationOperation") data["batchId"] = context.Request.QueryString["batchId"];
                    if (method == "GET" && action == "traineeProgress") data["placementId"] = context.Request.QueryString["placementId"];
                    if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "board")
                        data["placementId"] = context.Request.QueryString["placementId"];
                    if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "attachments")
                        data["taskId"] = context.Request.QueryString["taskId"];
                    if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "download")
                        data["fileId"] = context.Request.QueryString["fileId"];
                    if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateSameOrigin(context);
                        string requestType = context.Request.ContentType?.Split(';')[0].Trim();
                        if (string.Equals(requestType, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
                        {
                            if (context.Request.ContentLength < 0 || context.Request.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes + 128 * 1024)
                                throw new UIMS.Services.FirebaseException(413, "The upload request exceeds the permitted file size.");
                            var uploadActor = await TaskActorResolver.ResolveAsync(context, cancellation).ConfigureAwait(false);
                            var uploadService = new TaskService(uploadActor);
                            var file = context.Request.Files["file"];
                            if (file == null || file.ContentLength <= 0) throw new UIMS.Services.FirebaseException(400, "Choose a file to upload.");
                            if (file.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes) throw new UIMS.Services.FirebaseException(413, "Files are limited to 10 MiB.");
                            byte[] content = await ReadUploadAsync(file, cancellation).ConfigureAwait(false);
                            JObject uploadResult;
                            if (action == "attach" && uploadActor.Role == "companySupervisor")
                                uploadResult = await uploadService.UploadTaskAttachmentAsync(context.Request.Form["taskId"], file.FileName, file.ContentType, content, cancellation).ConfigureAwait(false);
                            else if (action == "submit" && uploadActor.Role == "student")
                                uploadResult = await uploadService.SubmitWithAttachmentAsync(context.Request.Form["assignmentId"], context.Request.Form["description"], context.Request.Form["link"], file.FileName, file.ContentType, content, cancellation).ConfigureAwait(false);
                            else throw new UIMS.Services.FirebaseException(403, "This upload action is not allowed for the verified account role.");
                            context.Response.StatusCode = 200;
                            await context.Response.Output.WriteAsync(uploadResult.ToString(Formatting.None)).ConfigureAwait(false);
                            return;
                        }
                        if (!string.Equals(requestType, "application/json", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(415, "Task updates must use JSON or the supported multipart upload form.");
                        if (context.Request.ContentLength < 0 || context.Request.ContentLength > (action == "beginCreation" ? 512 * 1024 : 32768)) throw new UIMS.Services.FirebaseException(413, "Task request is too large.");
                        string body;
                        using (var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) body = await reader.ReadToEndAsync().ConfigureAwait(false);
                        data = JObject.Parse(body);
                    }
                    else if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(405, "Unsupported task request.");

                    var actor = await TaskActorResolver.ResolveAsync(context, cancellation).ConfigureAwait(false);
                    context.Items["TaskApiStage"] = "execute-authorized-task-operation";
                    var service = new TaskService(actor);
                    if (method == "GET" && action == "changes")
                    {
                        long previous; if (!long.TryParse(context.Request.QueryString["version"], out previous)) previous = -1;
                        var changes = await service.ChangesAsync(previous, cancellation).ConfigureAwait(false);
                        await context.Response.Output.WriteAsync(changes.ToString(Formatting.None)).ConfigureAwait(false); return;
                    }
                    if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && action == "download")
                    {
                        var download = await service.DownloadTaskFileAsync((string)data["fileId"], cancellation).ConfigureAwait(false);
                        context.Response.Clear(); context.Response.ContentType = download.ContentType;
                        context.Response.AddHeader("X-Content-Type-Options", "nosniff");
                        context.Response.AddHeader("Content-Disposition", "attachment; filename*=UTF-8''" + Uri.EscapeDataString(download.FileName));
                        context.Response.AddHeader("Content-Length", download.Content.Length.ToString(CultureInfo.InvariantCulture));
                        context.Response.StatusCode = 200;
                        await context.Response.OutputStream.WriteAsync(download.Content, 0, download.Content.Length, cancellation).ConfigureAwait(false);
                        return;
                    }
                    JObject result = await DispatchAsync(service, method, action, data, cancellation).ConfigureAwait(false);
                    context.Response.StatusCode = 200;
                    await context.Response.Output.WriteAsync(result.ToString(Formatting.None)).ConfigureAwait(false);
                }
            }
            catch (UIMS.Services.FirebaseException ex) { WriteError(context, ex.StatusCode, ex.Message); }
            catch (OperationCanceledException) { WriteError(context, 504, "Firebase task request timed out."); }
            catch (JsonException) { WriteError(context, 400, "Invalid task request JSON."); }
            catch (Exception error)
            {
                string reference = RecordFailure(context, error);
                WriteError(context, 503, "Task service is unavailable. Reference: " + reference + ".");
            }
        }

        private static string RecordFailure(HttpContext context, Exception error)
        {
            string reference = Guid.NewGuid().ToString("N");
            var details = new StringBuilder(DateTimeOffset.UtcNow.ToString("o") + " reference=" + reference + " stage=" + Convert.ToString(context.Items["TaskApiStage"]) + Environment.NewLine);
            for (var current = error; current != null; current = current.InnerException)
            {
                details.AppendLine("type=" + current.GetType().FullName + " hresult=" + current.HResult.ToString(CultureInfo.InvariantCulture));
                // Do not log exception messages, HTTP bodies, profiles, credentials or tokens.
                details.AppendLine(current.StackTrace);
            }
            System.Diagnostics.Trace.TraceWarning("Tasks API failed; reference={0}; type={1}", reference, error.GetType().Name);
            try { System.IO.File.AppendAllText(context.Server.MapPath("~/App_Data/task-diagnostics.log"), details.ToString()); }
            catch (Exception loggingError) { System.Diagnostics.Trace.TraceWarning("Task diagnostics unavailable: {0}", loggingError.GetType().Name); }
            return reference;
        }

        private static void ValidateSameOrigin(HttpContext context)
        {
            Uri origin;
            string value = context.Request.Headers["Origin"];
            if (!Uri.TryCreate(value, UriKind.Absolute, out origin) || origin.Scheme != context.Request.Url.Scheme ||
                !string.Equals(origin.Authority, context.Request.Url.Authority, StringComparison.OrdinalIgnoreCase))
                throw new UIMS.Services.FirebaseException(403, "Task updates must come from this UIMS site.");
        }

        private static async Task<byte[]> ReadUploadAsync(HttpPostedFile file, CancellationToken cancellation)
        {
            using (var output = new System.IO.MemoryStream())
            {
                var buffer = new byte[81920]; int read;
                while ((read = await file.InputStream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                {
                    if (output.Length + read > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes)
                        throw new UIMS.Services.FirebaseException(413, "Files are limited to 10 MiB.");
                    await output.WriteAsync(buffer, 0, read, cancellation).ConfigureAwait(false);
                }
                return output.ToArray();
            }
        }

        private static void WriteError(HttpContext context, int status, string message)
        {
            context.Response.SuppressFormsAuthenticationRedirect = true;
            context.Response.StatusCode = status; context.Response.TrySkipIisCustomErrors = true;
            context.Response.Write(new JObject { ["success"] = false, ["message"] = message }.ToString(Formatting.None));
        }
    }
}
