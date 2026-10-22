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
using Newtonsoft.Json.Linq;

namespace UIMS.Attendance
{
    public class AttendanceFirebase
    {
        // رابط واحد فقط يتغير عند دمج مشروع الفريق.
        public static string DefaultDatabaseUrl { get { return new AttendanceConfiguration().DatabaseUrl + "/"; } }
        private static readonly HttpClient SharedClient = new HttpClient(new AttendanceHttpHandler()) { Timeout = TimeSpan.FromSeconds(20) };
        private readonly HttpClient client;
        private readonly AttendanceConfiguration configuration;
        private readonly Func<bool> localDevelopment;
        public AttendanceFirebase() : this(SharedClient, new AttendanceConfiguration(), () => AttendanceConfiguration.IsLocalDevelopment(System.Web.HttpContext.Current)) { }
        public AttendanceFirebase(HttpClient httpClient, AttendanceConfiguration config, Func<bool> isLocalDevelopment)
        {
            client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            configuration = config ?? throw new ArgumentNullException(nameof(config));
            localDevelopment = isLocalDevelopment ?? throw new ArgumentNullException(nameof(isLocalDevelopment));
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
            if (!localDevelopment())
                throw new UIMS.Services.FirebaseException(403, "Attendance without sign-in is restricted to explicitly enabled localhost development.");
            if (method != "GET")
            {
                configuration.EnsureWritesAllowed();
                var parts = path.Split('/');
                bool attendanceWrite = parts.Length == 3 && parts[1].StartsWith("att_demo_", StringComparison.Ordinal) &&
                    (parts[0] == "attendance" || parts[0] == "earlyCheckoutRequests");
                // إنشاء فقط بمعرّفات تجربة عشوائية؛ null_etag يمنع استبدال أي بيانات سابقة.
                bool newTestData = method == "PUT" && etag == "null_etag" && parts.Length >= 2 &&
                    parts[1].StartsWith("att_demo_session_", StringComparison.Ordinal) && data != null &&
                    AttendanceService.Flag(data, "isTestData") &&
                    ((parts.Length == 2 && new[] { "students", "companies", "companySupervisors", "academicSupervisors", "trainingAssignments" }.Contains(parts[0])) ||
                     (parts.Length == 3 && parts[0] == "schedules"));
                if (!attendanceWrite && !newTestData)
                    throw new UIMS.Services.FirebaseException(403, "Development writes are limited to isolated test records.");
            }
            foreach (string key in path.Split('/')) CheckKey(key);
            string database = configuration.DatabaseUrl;
            using (var request = new HttpRequestMessage(new HttpMethod(method), database.TrimEnd('/') + "/uimsAttendance/" + path + ".json"))
            {
                var metrics = AttendanceTiming.Current.Value;
                var timing = metrics == null ? null : metrics.Begin(method, path);
                if (timing != null) request.Properties[AttendanceHttpHandler.TimingKey] = timing;
                // Explicit local development mode: no Firebase Authentication or server OAuth credential.
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
                    throw new InvalidOperationException("Firebase denied access at /uimsAttendance/" + path + ". Current Rules do not permit this request. No Authentication or server-credential fallback was attempted.");
                }
                return response;
            }
        }

        public async Task<AttendanceSnapshot> Read(string path)
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

        public async Task<bool> SaveIfUnchanged(string path, JObject data, string etag)
        {
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
        private readonly Func<string, string> readSetting;
        public AttendanceConfiguration() : this(key => System.Configuration.ConfigurationManager.AppSettings[key]) { }
        public AttendanceConfiguration(Func<string, string> settings) { readSetting = settings ?? throw new ArgumentNullException(nameof(settings)); }
        private string Setting(string key) { return readSetting(key); }
        public string DatabaseUrl { get { return (Setting("AttendanceDatabaseUrl") ?? "").TrimEnd('/'); } }
        public bool Enabled { get { return Setting("AttendanceEnabled") == "true"; } }
        public bool LiveWritesEnabled { get { return Setting("AttendanceLiveWritesEnabled") == "true"; } }
        public static bool IsLocalDevelopment(System.Web.HttpContext context)
        {
            return context != null && (System.Configuration.ConfigurationManager.AppSettings["AttendanceLocalDevelopmentEnabled"] == "true" ||
                AttendanceFirebase.Setting("UIMS_ATTENDANCE_LOCAL_TEST") == "true") && context.Request.IsLocal && context.Request.Url.IsLoopback &&
                string.IsNullOrEmpty(context.Request.Headers["X-Forwarded-For"]) && string.IsNullOrEmpty(context.Request.Headers["Forwarded"]);
        }
        public void EnsureEnabled()
        {
            if (!Enabled) throw new UIMS.Services.FirebaseException(503, "Attendance Firebase connection is disabled.");
            if (Setting("AttendanceSchemaValidated") != "true") throw new UIMS.Services.FirebaseException(503, "The configured attendance schema has not been validated.");
            Uri url;
            if (!Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out url) || url.Scheme != "https" || !url.IsDefaultPort || url.AbsolutePath != "/" || url.UserInfo != "" || url.Query != "" || url.Fragment != "" ||
                !(url.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) || url.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)))
                throw new UIMS.Services.FirebaseException(503, "Configure a valid HTTPS AttendanceDatabaseUrl in Web.config.");
        }
        public void EnsureWritesAllowed()
        {
            EnsureEnabled();
            if (!LiveWritesEnabled) throw new UIMS.Services.FirebaseException(503, "Attendance live writes are disabled pending explicit development-test approval. No record was changed.");
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
