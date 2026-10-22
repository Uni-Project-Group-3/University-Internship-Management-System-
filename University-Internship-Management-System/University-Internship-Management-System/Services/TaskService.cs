using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UIMS.Tasks
{
    // Uses the existing top-level /tasks records shown in the imported UIMS database.
    // This local no-sign-in mode is suitable only where Firebase Rules permit the request.
    public sealed class TaskService
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> AssignmentLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly HttpClient client;
        private readonly Func<bool> noAuthenticationEnabled;
        private readonly UIMS.Services.FirebaseConfiguration firebase;
        private readonly string databaseUrl;
        private readonly string root;
        private readonly string studentId;
        private readonly string companyId;
        private readonly bool enabled;
        private readonly bool writesEnabled;
        private readonly bool schemaValidated;

        public TaskService() : this(k => WebConfigurationManager.AppSettings[k], Client) { }
        public TaskService(Func<string, string> setting) : this(setting, Client) { }
        public TaskService(Func<string, string> setting, HttpClient httpClient) : this(setting, httpClient, () => IsNoAuthenticationConfigured(setting)) { }
        public TaskService(Func<string, string> setting, HttpClient httpClient, Func<bool> isNoAuthenticationEnabled)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            noAuthenticationEnabled = isNoAuthenticationEnabled ?? throw new ArgumentNullException(nameof(isNoAuthenticationEnabled));
            firebase = new UIMS.Services.FirebaseConfiguration(setting("FirebaseEnabled"), setting("FirebaseProjectId"), setting("FirebaseDatabaseUrl"), setting("FirebaseApiKey"), setting("FirebaseLiveWritesEnabled"), setting("FirebaseTaskSchemaValidated"));
            enabled = firebase.Enabled && string.Equals(setting("TasksEnabled"), "true", StringComparison.OrdinalIgnoreCase);
            databaseUrl = firebase.DatabaseUrl.TrimEnd('/');
            root = string.IsNullOrWhiteSpace(setting("FirebaseTaskRootPath")) ? "tasks" : setting("FirebaseTaskRootPath").Trim('/');
            studentId = string.IsNullOrWhiteSpace(setting("TasksLocalStudentId")) ? "DEMO-ST-001" : setting("TasksLocalStudentId");
            companyId = string.IsNullOrWhiteSpace(setting("TasksLocalCompanyId")) ? "DEMO-CS-003" : setting("TasksLocalCompanyId");
            schemaValidated = string.Equals(setting("FirebaseTaskSchemaValidated"), "true", StringComparison.OrdinalIgnoreCase);
            writesEnabled = string.Equals(setting("TasksLiveWritesEnabled"), "true", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNoAuthenticationEnabled(HttpContext context)
        {
            // Explicit user-selected no-login task mode. Access is controlled by Firebase Rules.
            return context != null && IsNoAuthenticationConfigured(k => WebConfigurationManager.AppSettings[k]);
        }

        public static bool IsNoAuthenticationConfigured(Func<string, string> setting)
        {
            return setting != null && string.Equals(setting("TasksNoAuthenticationEnabled"), "true", StringComparison.OrdinalIgnoreCase);
        }

        private void Ensure(bool write)
        {
            if (!enabled) throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured for tasks.");
            firebase.EnsureEnabled();
            if (!schemaValidated || root != "tasks") throw new UIMS.Services.FirebaseException(503, "The existing /tasks schema is not validated.");
            Uri url;
            if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out url) || url.Scheme != "https" || !url.IsDefaultPort || url.AbsolutePath != "/")
                throw new UIMS.Services.FirebaseException(503, "Configure the HTTPS Firebase database URL in Web.config.");
            if (!noAuthenticationEnabled()) throw new UIMS.Services.FirebaseException(403, "Task access without sign-in is disabled in Web.config.");
            if (write && !writesEnabled) throw new UIMS.Services.FirebaseException(503, "Firebase task writes are disabled in Web.config.");
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
            using (var request = new HttpRequestMessage(HttpMethod.Get, UrlAt(collection, path)))
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
            using (var request = new HttpRequestMessage(new HttpMethod(method), Url(path)))
            {
                if (!string.IsNullOrWhiteSpace(etag)) request.Headers.TryAddWithoutValidation("if-match", etag);
                request.Content = new StringContent(data == null ? "null" : data.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) throw new UIMS.Services.FirebaseException(409, "This task changed in Firebase. Refresh the page and try again.");
                    await CheckResponse(response, path).ConfigureAwait(false);
                }
            }
        }

        private async Task PatchTaskPaths(JObject updates, CancellationToken cancellation)
        {
            foreach (var property in updates.Properties())
                foreach (var segment in property.Name.Split('/')) CheckKey(segment);
            // Firebase multi-location PATCH is atomic and preserves all unrelated Cycle 3 records.
            using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), databaseUrl + "/" + root + ".json"))
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
                case "in progress": case "inprogress": return "inprogress";
                case "inreview": case "submitted": return "submitted";
                case "revisionrequested": case "revision needed": case "revision": return "revision";
                case "completed": case "evaluated": return "completed";
                default: return value;
            }
        }

        public async Task<JObject> LoadAsync(string role, CancellationToken cancellation)
        {
            Ensure(false);
            if (role != "student" && role != "company") throw new UIMS.Services.FirebaseException(400, "Unsupported task page.");
            // The imported schema stores each task as one record under /tasks/{id}.
            // Use the existing studentId and assignedBy links, and map dueDate to the UI deadline.
            var records = await ReadData("", cancellation).ConfigureAwait(false);
            var outputTasks = new JArray(); var outputSubs = new JArray(); var students = new JArray();
            var configuredStudentName = studentId;
            if (role == "company")
            {
                var studentProfile = (await ReadAt("users", studentId, cancellation).ConfigureAwait(false)).Data;
                var name = Text(studentProfile, "fullName"); if (name.Length == 0) name = Text(studentProfile, "name");
                if (studentProfile.Count > 0)
                {
                    configuredStudentName = name.Length > 0 ? name : studentId;
                    students.Add(new JObject { ["id"] = studentId, ["name"] = configuredStudentName });
                }
            }
            foreach (var entry in Rows(records))
            {
                var task = entry.Value as JObject; if (task == null) continue;
                var assignedStudent = Text(task, "studentId");
                var assignedSupervisor = Text(task, "assignedBy");
                if (role == "student" && assignedStudent != studentId) continue;
                if (role == "company" && assignedSupervisor != companyId) continue;
                var status = Text(task, "status");
                var submissions = task["submissions"] as JArray ?? new JArray();
                foreach (var submission in submissions.OfType<JObject>())
                    outputSubs.Add(new JObject { ["id"] = entry.Name + "-" + Number(submission, "revision", 1).ToString(CultureInfo.InvariantCulture),
                        ["taskId"] = entry.Name, ["assignmentId"] = entry.Name,
                        ["description"] = Text(submission, "description"), ["link"] = Text(submission, "link"),
                        ["fileName"] = Text(submission, "fileName"), ["revision"] = Number(submission, "revision", 1),
                        ["createdAt"] = Text(submission, "submittedAt") });
                outputTasks.Add(new JObject {
                    ["id"] = entry.Name, ["studentId"] = assignedStudent,
                    ["studentName"] = assignedStudent == studentId ? configuredStudentName : assignedStudent, ["title"] = Text(task, "title"),
                    ["description"] = Text(task, "description"),
                    ["deadline"] = Text(task, "dueDate").Length > 0 ? Text(task, "dueDate") : Text(task, "deadline"),
                    ["criteria"] = Text(task, "evaluationCriteria"), ["feedback"] = Text(task, "feedback"),
                    ["status"] = Status(status), ["rawStatus"] = status,
                    ["score"] = task["score"] == null ? JValue.CreateNull() : task["score"].DeepClone(),
                    ["maximum"] = Number(task, "maximumScore", 100)
                });
            }
            return new JObject { ["role"] = role, ["identity"] = role == "student" ? studentId : companyId,
                ["tasks"] = outputTasks, ["submissions"] = outputSubs, ["students"] = students };
        }

        public async Task<JObject> StartAsync(string assignmentId, CancellationToken cancellation)
        {
            Ensure(true); CheckKey(assignmentId);
            var current = await Read(assignmentId, cancellation).ConfigureAwait(false); var row = current.Data;
            if (Text(row, "studentId") != studentId) throw new UIMS.Services.FirebaseException(403, "This task is not assigned to the configured local student.");
            var currentStatus = Status(Text(row, "status"));
            if (currentStatus == "inprogress") return new JObject { ["success"] = true };
            if (currentStatus != "assigned") throw new UIMS.Services.FirebaseException(409, "This task cannot be started in its current status.");
            row["status"] = "In Progress";
            await Write("PUT", assignmentId, row, current.ETag, cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true };
        }

        public async Task<JObject> SubmitAsync(string assignmentId, string description, string link, string fileName, CancellationToken cancellation)
        {
            Ensure(true); CheckKey(assignmentId);
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 3000)
                throw new UIMS.Services.FirebaseException(400, "Enter a work description up to 3000 characters.");
            Uri parsed;
            if (string.IsNullOrWhiteSpace(link) || !Uri.TryCreate(link, UriKind.Absolute, out parsed) || (parsed.Scheme != "https" && parsed.Scheme != "http"))
                throw new UIMS.Services.FirebaseException(400, "Enter a valid HTTP or HTTPS work link.");
            var gate = AssignmentLocks.GetOrAdd(assignmentId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var current = await Read(assignmentId, cancellation).ConfigureAwait(false); var task = current.Data;
                if (Text(task, "studentId") != studentId) throw new UIMS.Services.FirebaseException(403, "This task is not assigned to the configured local student.");
                var status = Status(Text(task, "status"));
                if (status != "inprogress" && status != "revision") throw new UIMS.Services.FirebaseException(409, "Start the task first, or wait until the supervisor requests a revision.");
                var history = task["submissions"] as JArray ?? new JArray();
                var revision = history.OfType<JObject>().Select(x => Number(x, "revision")).DefaultIfEmpty(0).Max() + 1;
                history.Add(new JObject { ["revision"] = revision, ["description"] = description.Trim(), ["link"] = link.Trim(),
                    ["fileName"] = (fileName ?? "").Trim(), ["submittedAt"] = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) });
                task["submissions"] = history; task["status"] = "Submitted";
                await Write("PUT", assignmentId, task, current.ETag, cancellation).ConfigureAwait(false);
                return new JObject { ["success"] = true, ["revision"] = revision };
            }
            finally { gate.Release(); }
        }

        public async Task<JObject> CreateAsync(string title, string description, string deadline, string student, string criteria, int maximum, CancellationToken cancellation)
        {
            Ensure(true);
            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 150 || string.IsNullOrWhiteSpace(description) || description.Trim().Length > 3000 || string.IsNullOrWhiteSpace(criteria) || criteria.Trim().Length > 500 || maximum < 1 || maximum > 1000)
                throw new UIMS.Services.FirebaseException(400, "Complete the task title, requirements, criteria and a score from 1 to 1000.");
            CheckKey(student);
            DateTime parsed; if (!DateTime.TryParseExact(deadline, new[] { "yyyy-MM-dd", "yyyy-MM-ddTHH:mm", "o" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed)) throw new UIMS.Services.FirebaseException(400, "Choose a valid task deadline.");
            var profile = (await ReadAt("users", student, cancellation).ConfigureAwait(false)).Data;
            if (profile.Count == 0) throw new UIMS.Services.FirebaseException(404, "The selected student profile does not exist in /users.");
            string taskId = "UIMS-TASK-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            var task = new JObject { ["id"] = taskId, ["title"] = title.Trim(), ["description"] = description.Trim(),
                ["studentId"] = student, ["assignedBy"] = companyId, ["assignedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["dueDate"] = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ["status"] = "Assigned", ["isDemo"] = false,
                ["evaluationCriteria"] = criteria.Trim(), ["maximumScore"] = maximum };
            var current = await Read(taskId, cancellation).ConfigureAwait(false);
            if (current.Data.Count > 0) throw new UIMS.Services.FirebaseException(409, "A task with this generated ID already exists. Try again.");
            await Write("PUT", taskId, task, "null_etag", cancellation).ConfigureAwait(false);
            return new JObject { ["success"] = true, ["id"] = taskId };
        }

        public async Task<JObject> ReviewAsync(string assignmentId, string action, int score, string feedback, CancellationToken cancellation)
        {
            Ensure(true); CheckKey(assignmentId);
            if (action != "revision" && action != "grade") throw new UIMS.Services.FirebaseException(400, "Invalid task review action.");
            if (feedback == null || feedback.Length > 3000) throw new UIMS.Services.FirebaseException(400, "Feedback must be 3000 characters or fewer.");
            var gate = AssignmentLocks.GetOrAdd(assignmentId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                var current = await Read(assignmentId, cancellation).ConfigureAwait(false); var task = current.Data;
                if (Text(task, "assignedBy") != companyId) throw new UIMS.Services.FirebaseException(403, "This task does not belong to the configured company supervisor.");
                if (Status(Text(task, "status")) != "submitted" || !(task["submissions"] is JArray) || !((JArray)task["submissions"]).HasValues)
                    throw new UIMS.Services.FirebaseException(409, "Only a task with a saved student submission can be reviewed.");
                var maximum = Number(task, "maximumScore", 100);
                if (action == "grade" && (score < 0 || score > maximum))
                    throw new UIMS.Services.FirebaseException(400, "The score must be between 0 and " + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                task["feedback"] = feedback.Trim();
                if (action == "revision") task["status"] = "Revision Needed";
                else { task["score"] = score; task["status"] = "Completed"; }
                await Write("PUT", assignmentId, task, current.ETag, cancellation).ConfigureAwait(false);
                return new JObject { ["success"] = true };
            }
            finally { gate.Release(); }
        }
    }

    internal sealed class TaskRecord { public JObject Data { get; set; } public string ETag { get; set; } }

    public sealed class TasksApiHandler : System.Web.HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.Cache.SetNoStore(); context.Response.ContentType = "application/json";
            try
            {
                if (!TaskService.IsNoAuthenticationEnabled(context)) throw new UIMS.Services.FirebaseException(403, "Task access without sign-in is disabled in Web.config.");
                var service = new TaskService();
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
                {
                    var cancellation = timeout.Token; var action = context.Request.HttpMethod == "GET" ? "load" : context.Request.QueryString["action"];
                    JObject result;
                    if (context.Request.HttpMethod == "GET" && action == "load") result = await service.LoadAsync(context.Request.QueryString["role"], cancellation).ConfigureAwait(false);
                    else if (context.Request.HttpMethod == "POST")
                    {
                        string body;
                        using (var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) body = await reader.ReadToEndAsync().ConfigureAwait(false);
                        var data = JObject.Parse(body);
                        string role = context.Request.QueryString["role"]; action = context.Request.QueryString["action"];
                        if (role == "student" && action == "start") result = await service.StartAsync((string)data["assignmentId"], cancellation).ConfigureAwait(false);
                        else if (role == "student" && action == "submit") result = await service.SubmitAsync((string)data["assignmentId"], (string)data["description"], (string)data["link"], (string)data["fileName"], cancellation).ConfigureAwait(false);
                        else if (role == "company" && action == "create") result = await service.CreateAsync((string)data["title"], (string)data["description"], (string)data["deadline"], (string)data["studentId"], (string)data["criteria"], (int?)data["maximum"] ?? 0, cancellation).ConfigureAwait(false);
                        else if (role == "company" && action == "review") result = await service.ReviewAsync((string)data["assignmentId"], (string)data["reviewAction"], (int?)data["score"] ?? 0, (string)data["feedback"] ?? "", cancellation).ConfigureAwait(false);
                        else throw new UIMS.Services.FirebaseException(400, "Unsupported task operation.");
                    }
                    else throw new UIMS.Services.FirebaseException(405, "Unsupported task request.");
                    context.Response.StatusCode = 200; await context.Response.Output.WriteAsync(result.ToString(Formatting.None)).ConfigureAwait(false);
                }
            }
            catch (UIMS.Services.FirebaseException ex) { context.Response.StatusCode = ex.StatusCode; context.Response.TrySkipIisCustomErrors = true; await context.Response.Output.WriteAsync(new JObject { ["success"] = false, ["message"] = ex.Message }.ToString(Formatting.None)).ConfigureAwait(false); }
            catch (OperationCanceledException) { context.Response.StatusCode = 504; context.Response.TrySkipIisCustomErrors = true; await context.Response.Output.WriteAsync("{\"success\":false,\"message\":\"Firebase task request timed out.\"}").ConfigureAwait(false); }
            catch (JsonException) { context.Response.StatusCode = 400; context.Response.TrySkipIisCustomErrors = true; await context.Response.Output.WriteAsync("{\"success\":false,\"message\":\"Invalid task request JSON.\"}").ConfigureAwait(false); }
            catch (Exception) { context.Response.StatusCode = 502; context.Response.TrySkipIisCustomErrors = true; await context.Response.Output.WriteAsync("{\"success\":false,\"message\":\"Task operation failed. Check Firebase connectivity and Rules.\"}").ConfigureAwait(false); }
        }
    }
}
