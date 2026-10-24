using System;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;

namespace UIMS.Attendance
{
    public class AttendanceFirebase
    {
        // رابط واحد فقط يتغير عند دمج مشروع الفريق.
        public static string DefaultDatabaseUrl { get { return new AttendanceConfiguration().DatabaseUrl + "/"; } }
        private static readonly HttpClient SharedClient = new HttpClient(new AttendanceHttpHandler()) { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly Lazy<Task<GoogleCredential>> ServerCredential = new Lazy<Task<GoogleCredential>>(LoadServerCredential);
        private static async Task<GoogleCredential> LoadServerCredential()
        {
            // نعيد استخدام الاعتماد المحدد بالصلاحيات لكي يستفيد من تجديد التوكن المدمج.
            return (await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false)).CreateScoped(
                "https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
        }
        private readonly HttpClient client;
        private readonly AttendanceConfiguration configuration;
        public AttendanceFirebase() : this(SharedClient, new AttendanceConfiguration(), () => AttendanceConfiguration.IsLocalDevelopment(System.Web.HttpContext.Current)) { }
        public AttendanceFirebase(HttpClient httpClient, AttendanceConfiguration config, Func<bool> isLocalDevelopment)
        {
            client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            configuration = config ?? throw new ArgumentNullException(nameof(config));
            if (isLocalDevelopment == null) throw new ArgumentNullException(nameof(isLocalDevelopment));
        }

        public static string Setting(string name)
        {
            return Environment.GetEnvironmentVariable(name) ??
                Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        }

        public static void CheckKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 180)
                throw new InvalidOperationException("Invalid record ID.");
            foreach (char c in key)
                if (".$#[]/\\".IndexOf(c) >= 0 || char.IsControl(c))
                    throw new InvalidOperationException("Invalid record ID.");
        }

        private async Task<HttpResponseMessage> Send(string method, string path, JObject data, string etag)
        {
            configuration.EnsureEnabled();
            if (method != "GET")
            {
                configuration.EnsureWritesAllowed();
                configuration.EnsureWriteScope(path, data);
                if (method != "PUT" || string.IsNullOrWhiteSpace(etag) || etag == "*")
                    throw new UIMS.Services.FirebaseException(403, "Attendance writes require an exact record ETag.");
            }
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("An attendance database path is required.");
            foreach (string key in path.Split('/')) CheckKey(key);
            string database = configuration.DatabaseUrl;
            using (var request = new HttpRequestMessage(new HttpMethod(method), database.TrimEnd('/') + "/uimsAttendance/" + path + ".json"))
            {
                var metrics = AttendanceTiming.Current.Value;
                var timing = metrics == null ? null : metrics.Begin(method, path);
                if (timing != null) request.Properties[AttendanceHttpHandler.TimingKey] = timing;
                string accessToken;
                try
                {
                    var credential = await ServerCredential.Value.ConfigureAwait(false);
                    accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception error) when (error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException ||
                    error is Newtonsoft.Json.JsonException ||
                    error is Google.Apis.Auth.OAuth2.Responses.TokenResponseException)
                {
                    // لا نعرض محتوى الاعتماد أو رد خدمة التوكن للمستخدم.
                    throw new UIMS.Services.FirebaseException(503, "Attendance server credentials are unavailable. Ask the administrator to verify Application Default Credentials.");
                }
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (method == "GET") request.Headers.Add("X-Firebase-ETag", "true");
                if (etag != null) request.Headers.TryAddWithoutValidation("if-match", etag);
                if (data != null) request.Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response;
                double started = metrics == null ? 0 : metrics.Elapsed;
                try { response = await client.SendAsync(request).ConfigureAwait(false); }
                finally
                {
                    if (metrics != null) metrics.Record(started, metrics.Elapsed, path.Split('/')[0]);
                    if (timing != null)
                    {
                        timing.BufferMs = Math.Max(0, metrics.Elapsed - started - timing.HeadersMs);
                        timing.EndMs = metrics.Elapsed;
                    }
                }
                if (response.RequestMessage == null) response.RequestMessage = request;
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    response.Dispose();
                    throw new UIMS.Services.FirebaseException(503, "Firebase denied the attendance server request. Ask the administrator to verify the server credential and database permissions.");
                }
                return response;
            }
        }

        public virtual async Task<AttendanceSnapshot> Read(string path)
        {
            using (var response = await Send("GET", path, null, null).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var processing = Stopwatch.StartNew();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject data;
                // نحافظ على نص التاريخ كما خزنه Firebase دون تحويله تلقائيًا إلى Date.
                using (var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(json)))
                {
                    reader.DateParseHandling = Newtonsoft.Json.DateParseHandling.None;
                    JToken value = JToken.ReadFrom(reader);
                    if (value.Type != JTokenType.Null && !(value is JObject))
                        throw new InvalidOperationException("Firebase returned an incomplete attendance record. An object was expected.");
                    data = value.Type == JTokenType.Null ? new JObject() : (JObject)value;
                }
                var timing = AttendanceHttpHandler.Timing(response.RequestMessage);
                if (timing != null) timing.ParseMs = processing.Elapsed.TotalMilliseconds;
                // قد يعيد Firebase قيمة ETag بدون علامات اقتباس؛ نقرأ الهيدر الخام كما هو.
                IEnumerable<string> etags;
                string etag = response.Headers.TryGetValues("ETag", out etags) ? string.Join("", etags) : null;
                return new AttendanceSnapshot
                {
                    Data = data,
                    Etag = etag
                };
            }
        }

        public virtual async Task<bool> SaveIfUnchanged(string path, JObject data, string etag)
        {
            if (data == null) throw new InvalidOperationException("An attendance record is required. No data was written.");
            if (string.IsNullOrEmpty(etag)) throw new InvalidOperationException("Firebase did not provide an ETag. No data was written.");
            using (var response = await Send("PUT", path, data, etag).ConfigureAwait(false))
            {
                if ((int)response.StatusCode == 412) return false;
                response.EnsureSuccessStatusCode();
                return true;
            }
        }
    }

    public sealed class AttendanceConfiguration
    {
        internal sealed class VerifiedWriteRequest
        {
            internal string Uid, Role, Id, Action;
        }
        internal static readonly AsyncLocal<VerifiedWriteRequest> WriteRequest = new AsyncLocal<VerifiedWriteRequest>();
        private readonly Func<string, string> readSetting;
        public AttendanceConfiguration() : this(key => System.Configuration.ConfigurationManager.AppSettings[key]) { }
        public AttendanceConfiguration(Func<string, string> settings) { readSetting = settings ?? throw new ArgumentNullException(nameof(settings)); }
        private string Setting(string key) { return readSetting(key); }
        public string DatabaseUrl { get { return (Setting("FirebaseDatabaseUrl") ?? "").TrimEnd('/'); } }
        public bool Enabled { get { return Setting("AttendanceEnabled") == "true"; } }
        public bool LiveWritesEnabled { get { return Setting("AttendanceLiveWritesEnabled") == "true"; } }
        public static bool IsLocalDevelopment(System.Web.HttpContext context)
        {
            // Do not create a localhost-only identity or business-data repository.
            // Attendance must use the verified Firebase UID and the shared database.
            return false;
        }
        public void EnsureEnabled()
        {
            if (!Enabled || !string.Equals(Setting("FirebaseEnabled"), "true", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured. Attendance integration is disabled.");
            if (!string.Equals(Setting("FirebaseAuthenticationEnabled"), "true", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(503, "Firebase Authentication is disabled for attendance. No request was sent.");
            if (Setting("AttendanceSchemaValidated") != "true") throw new UIMS.Services.FirebaseException(503, "The configured attendance schema has not been validated.");
            Uri url;
            if (!Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out url) || url.Scheme != "https" || !url.IsDefaultPort || url.AbsolutePath != "/" || url.UserInfo != "" || url.Query != "" || url.Fragment != "" ||
                !(url.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) || url.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(503, "Configure the shared HTTPS FirebaseDatabaseUrl in Web.config.");
        }
        public void EnsureWritesAllowed()
        {
            EnsureEnabled();
            if (!LiveWritesEnabled) throw new UIMS.Services.FirebaseException(503, "Attendance live writes are disabled pending explicit development-test approval. No record was changed.");
        }

        private JObject IntegrationManifest()
        {
            string path = Setting("AttendanceIntegrationManifestFile");
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                var context = System.Web.HttpContext.Current;
                if (context != null && !context.Request.IsLocal)
                    throw new InvalidOperationException();
                string root = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath ?? AppDomain.CurrentDomain.BaseDirectory;
                if (!Path.IsPathRooted(path) || Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException();
                var manifest = JObject.Parse(File.ReadAllText(path));
                if ((string)manifest["projectId"] != Setting("FirebaseProjectId") ||
                    ((string)manifest["databaseUrl"] ?? "").TrimEnd('/') != DatabaseUrl)
                    throw new InvalidOperationException();
                return manifest;
            }
            catch (Exception error) when (error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException ||
                error is ArgumentException || error is Newtonsoft.Json.JsonException)
            {
                throw new UIMS.Services.FirebaseException(503, "The local attendance integration manifest is unavailable or invalid.");
            }
        }

        public bool IsAuthorizedIntegrationActor(string role, string id, string uid)
        {
            var manifest = IntegrationManifest();
            var actors = manifest?[role == "student" ? "students" : "supervisors"] as JObject;
            return actors != null && !string.IsNullOrWhiteSpace(uid) && (string)actors[id] == uid;
        }

        public void EnsureWriteScope(string path, JObject data)
        {
            if (Setting("AttendanceProductionWritesEnabled") == "true")
            {
                CheckProductionWrite(path, data);
                return;
            }
            // التشغيل الحالي يسمح بكتابة تجربة معزولة محددة فقط، ولا يفتح الكتابة لطلاب الفريق.
            var manifest = IntegrationManifest();
            var allowed = manifest?["allowedWrites"] as JArray;
            var segments = (path ?? "").Split('/');
            if (manifest == null || allowed == null || !allowed.Any(x => x.Type == JTokenType.String && (string)x == path) ||
                segments.Length != 3 || (segments[0] != "attendance" && segments[0] != "earlyCheckoutRequests") || data == null ||
                data["isTestData"] == null || data["isTestData"].Type != JTokenType.Boolean || !(bool)data["isTestData"] ||
                (string)data["assignmentId"] != segments[1] || (string)data["attendanceDate"] != segments[2])
                throw new UIMS.Services.FirebaseException(403, "Attendance writes are restricted to the approved isolated integration paths.");
            var students = manifest["students"] as JObject;
            var supervisors = manifest["supervisors"] as JObject;
            var assignments = manifest["assignments"] as JArray;
            var companies = manifest["companies"] as JArray;
            if (students == null || supervisors == null || assignments == null || companies == null ||
                students[(string)data["studentId"] ?? ""] == null || supervisors[(string)data["companySupervisorId"] ?? ""] == null ||
                !assignments.Any(x => (string)x == (string)data["assignmentId"]) || !companies.Any(x => (string)x == (string)data["companyId"]))
                throw new UIMS.Services.FirebaseException(403, "The attendance write does not match the approved test identities.");
            try
            {
                string backupPath = (string)manifest["backupFile"];
                if (string.IsNullOrWhiteSpace(backupPath) || !Path.IsPathRooted(backupPath)) throw new InvalidOperationException();
                string root = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath ?? AppDomain.CurrentDomain.BaseDirectory;
                if (Path.GetFullPath(backupPath).StartsWith(Path.GetFullPath(root).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException();
                var backup = JObject.Parse(File.ReadAllText(backupPath));
                var snapshot = (backup["snapshots"] as JObject)?[path] as JObject;
                if ((string)backup["projectId"] != Setting("FirebaseProjectId") || snapshot == null ||
                    snapshot["existed"] == null || snapshot["existed"].Type != JTokenType.Boolean || (bool)snapshot["existed"])
                    throw new InvalidOperationException();
            }
            catch (Exception error) when (error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException ||
                error is ArgumentException || error is Newtonsoft.Json.JsonException)
            {
                throw new UIMS.Services.FirebaseException(503, "An external backup confirming a new isolated test path is required before writing.");
            }
        }

        private static void CheckProductionWrite(string path, JObject data)
        {
            var context = System.Web.HttpContext.Current;
            var actor = WriteRequest.Value;
            var parts = (path ?? "").Split('/');
            DateTime date;
            if (context == null || !context.Request.IsLocal || context.Request.HttpMethod != "POST" ||
                actor == null || string.IsNullOrWhiteSpace(actor.Uid) || string.IsNullOrWhiteSpace(actor.Id) ||
                parts.Length != 3 || (parts[0] != "attendance" && parts[0] != "earlyCheckoutRequests") ||
                !DateTime.TryParseExact(parts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
                string.CompareOrdinal(parts[2], AttendanceService.Today()) > 0 || data == null ||
                (string)data["assignmentId"] != parts[1] || (string)data["attendanceDate"] != parts[2] ||
                data["isTestData"] == null || data["isTestData"].Type != JTokenType.Boolean || (bool)data["isTestData"])
                throw new UIMS.Services.FirebaseException(403, "Production attendance write is outside the authenticated local request scope.");
            foreach (string field in new[] { "studentId", "companyId", "companySupervisorId", "assignmentId" })
                AttendanceFirebase.CheckKey(AttendanceService.Text(data, field));
            bool student = actor.Role == "student" && actor.Action == "requestEarly" &&
                parts[0] == "earlyCheckoutRequests" && (string)data["studentId"] == actor.Id;
            bool supervisor = actor.Role == "company" && (string)data["companySupervisorId"] == actor.Id &&
                ((parts[0] == "attendance" && (actor.Action == "issueQr" || actor.Action == "scan" || actor.Action == "review")) ||
                 (parts[0] == "earlyCheckoutRequests" && actor.Action == "review"));
            if ((!student && !supervisor) || (actor.Action != "review" && parts[2] != AttendanceService.Today()))
                throw new UIMS.Services.FirebaseException(403, "This authenticated role cannot perform the requested attendance write.");
        }
    }

    // نسجل أزمنة فقط؛ لا نسجل هيدر Authorization أو محتوى الرد أو معرّفات الأشخاص.
    internal sealed class AttendanceRequestTiming
    {
        public int Id;
        public string Branch, Method;
        public double StartMs, EndMs, HeadersMs, BufferMs, ParseMs;
        public double TokenLockMs = 0, TokenFileMs = 0;
        public double TokenMs = 0; // End-user ID token is already verified by the route.
        public bool TokenRefreshed = false;
        public int ConnectionsBefore, ConnectionsAfter;
    }
    internal sealed class AttendanceHttpHandler : DelegatingHandler
    {
        internal const string TimingKey = "Uims.Attendance.Timing";
        internal AttendanceHttpHandler() : base(new HttpClientHandler()) { }
        internal static AttendanceRequestTiming Timing(HttpRequestMessage request)
        {
            object value;
            return request != null && request.Properties.TryGetValue(TimingKey, out value) ? value as AttendanceRequestTiming : null;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            var timing = Timing(request);
            if (timing == null) return await base.SendAsync(request, cancellation).ConfigureAwait(false);
            var endpoint = ServicePointManager.FindServicePoint(request.RequestUri);
            timing.ConnectionsBefore = endpoint.CurrentConnections;
            var watch = Stopwatch.StartNew();
            try { return await base.SendAsync(request, cancellation).ConfigureAwait(false); }
            finally
            {
                timing.HeadersMs = watch.Elapsed.TotalMilliseconds;
                timing.ConnectionsAfter = endpoint.CurrentConnections;
            }
        }
    }

    // قياس محلي فقط؛ لا نسجل روابط السجلات أو التوكن أو محتوى البيانات.
    internal sealed class AttendanceTiming
    {
        internal static readonly AsyncLocal<AttendanceTiming> Current = new AsyncLocal<AttendanceTiming>();
        private readonly List<AttendanceRequestTiming> requests = new List<AttendanceRequestTiming>();
        internal AttendanceRequestTiming Begin(string method, string path)
        {
            lock (intervals)
            {
                var result = new AttendanceRequestTiming { Id = requests.Count + 1, Method = method, Branch = path.Split('/')[0], StartMs = Elapsed };
                requests.Add(result);
                return result;
            }
        }
        internal string Details()
        {
            lock (intervals) return Newtonsoft.Json.JsonConvert.SerializeObject(new { requests = requests.Take(16), total = requests.Count });
        }
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Dictionary<string, double> branches = new Dictionary<string, double>();
        private readonly List<Tuple<double, double>> intervals = new List<Tuple<double, double>>();
        private readonly List<Tuple<double, double>> authIntervals = new List<Tuple<double, double>>();
        internal void RecordAuth(double start, double end)
        {
            lock (intervals) authIntervals.Add(Tuple.Create(start, end));
        }
        private static double Busy(IEnumerable<Tuple<double, double>> spans)
        {
            double busy = 0, end = 0;
            foreach (var interval in spans.OrderBy(x => x.Item1))
            {
                busy += Math.Max(0, interval.Item2 - Math.Max(end, interval.Item1));
                end = Math.Max(end, interval.Item2);
            }
            return busy;
        }
        internal double Elapsed { get { return clock.Elapsed.TotalMilliseconds; } }
        internal void Record(double start, double end, string branch)
        {
            lock (intervals)
            {
                intervals.Add(Tuple.Create(start, end));
                if (!branches.ContainsKey(branch)) branches[branch] = 0;
                branches[branch] += end - start;
            }
        }
        internal string Header()
        {
            lock (intervals)
            {
                double elapsed = Elapsed;
                return string.Format(CultureInfo.InvariantCulture,
                    "attendance;dur={0:F2}, firebase;dur={1:F2}, processing;dur={2:F2}, credential;dur={3:F2}, firebaseRequests;desc=\"{4}\"",
                    elapsed, Busy(intervals), Math.Max(0, elapsed - Busy(intervals.Concat(authIntervals))),
                    Busy(authIntervals), intervals.Count) + string.Concat(branches.Select(x =>
                        string.Format(CultureInfo.InvariantCulture, ", fb_{0};dur={1:F2}", x.Key, x.Value)));
            }
        }
    }

    public class AttendanceSnapshot
    {
        public JObject Data { get; set; }
        public string Etag { get; set; }
    }
}
