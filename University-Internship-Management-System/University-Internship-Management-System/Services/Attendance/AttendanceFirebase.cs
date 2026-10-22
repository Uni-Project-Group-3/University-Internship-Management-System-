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
        public const string DefaultDatabaseUrl = "https://uniproject-96de4-default-rtdb.firebaseio.com/";
        private static readonly HttpClient Client = new HttpClient(new AttendanceHttpHandler()) { Timeout = TimeSpan.FromSeconds(20) };

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

        private static readonly SemaphoreSlim TokenLock = new SemaphoreSlim(1, 1);
        private static string cachedToken, cachedFile;
        private static DateTime cachedModified, tokenExpires;
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static async Task<string> AccessToken(AttendanceRequestTiming timing)
        {
            string file = Setting("UIMS_ATTENDANCE_SERVICE_ACCOUNT_FILE");
            if (string.IsNullOrWhiteSpace(file)) return FileAccessToken();
            var step = Stopwatch.StartNew();
            await TokenLock.WaitAsync().ConfigureAwait(false);
            if (timing != null) timing.TokenLockMs = step.Elapsed.TotalMilliseconds;
            try
            {
                step.Restart();
                DateTime modified = File.GetLastWriteTimeUtc(file);
                if (timing != null) timing.TokenFileMs = step.Elapsed.TotalMilliseconds;
                if (cachedFile == file && cachedModified == modified && DateTime.UtcNow < tokenExpires)
                    return cachedToken;
                if (timing != null) timing.TokenRefreshed = true;
                JObject account = JObject.Parse(File.ReadAllText(file));
                string email = (string)account["client_email"], pem = (string)account["private_key"];
                if ((string)account["type"] != "service_account" || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pem))
                    throw new InvalidOperationException("Invalid service account configuration.");

                // نوقع طلب اعتماد مؤقت محليًا؛ المفتاح الخاص لا يرسل إلى Google أو المتصفح.
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var claims = new JObject
                {
                    ["iss"] = email, ["aud"] = TokenEndpoint,
                    ["scope"] = "https://www.googleapis.com/auth/firebase.database https://www.googleapis.com/auth/userinfo.email",
                    ["iat"] = now, ["exp"] = now + 3600
                };
                string unsigned = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}")) + "." +
                    Base64Url(Encoding.UTF8.GetBytes(claims.ToString(Newtonsoft.Json.Formatting.None)));
                byte[] keyBytes = Convert.FromBase64String(pem.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", ""));
                string assertion;
                try
                {
                    using (var key = CngKey.Import(keyBytes, CngKeyBlobFormat.Pkcs8PrivateBlob))
                    using (var rsa = new RSACng(key))
                        assertion = unsigned + "." + Base64Url(rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                }
                finally { Array.Clear(keyBytes, 0, keyBytes.Length); }

                using (var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" }, { "assertion", assertion }
                }))
                using (var response = await Client.PostAsync(TokenEndpoint, content).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException("Google OAuth rejected server credentials (HTTP " + (int)response.StatusCode + ").");
                    JObject result = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    string token = (string)result["access_token"];
                    int lifetime = (int?)result["expires_in"] ?? 0;
                    if (string.IsNullOrWhiteSpace(token) || lifetime <= 60)
                        throw new InvalidOperationException("Google OAuth returned no usable access token.");
                    // يبقى التوكن في ذاكرة السيرفر فقط ويجدد قبل انتهاء صلاحيته بدقيقة.
                    cachedToken = token; cachedFile = file; cachedModified = modified;
                    tokenExpires = DateTime.UtcNow.AddSeconds(lifetime - 60);
                    return token;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is Newtonsoft.Json.JsonException || ex is CryptographicException || ex is FormatException)
            {
                throw new InvalidOperationException("Cannot load or sign with the server service account. Check file access and key format.");
            }
            finally { TokenLock.Release(); }
        }

        private static string FileAccessToken()
        {
            // الملف السري خارج المشروع، ولا يرسل محتواه للمتصفح.
            string file = Setting("UIMS_ATTENDANCE_ACCESS_TOKEN_FILE");
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                throw new InvalidOperationException("Configure UIMS_ATTENDANCE_SERVICE_ACCOUNT_FILE with the server-only service account JSON path.");
            string token = File.ReadAllText(file).Trim();
            if (token.Length < 20 || token.StartsWith("{") || token.Contains("\n") || token.Contains("\r"))
                throw new InvalidOperationException("The server Firebase access token file is invalid.");
            return token;
        }

        private static async Task<HttpResponseMessage> Send(string method, string path, JObject data, string etag)
        {
            foreach (string key in path.Split('/')) CheckKey(key);
            string database = Setting("UIMS_ATTENDANCE_DATABASE_URL") ?? DefaultDatabaseUrl;
            Uri address;
            if (!Uri.TryCreate(database, UriKind.Absolute, out address) || address.Scheme != "https" ||
                !(address.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) ||
                  address.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)) ||
                address.AbsolutePath != "/" || address.Query != "" || address.UserInfo != "")
                throw new InvalidOperationException("Configure a valid HTTPS Firebase Realtime Database root URL.");
            using (var request = new HttpRequestMessage(new HttpMethod(method), database.TrimEnd('/') + "/uimsAttendance/" + path + ".json"))
            {
                var metrics = AttendanceTiming.Current.Value;
                var timing = metrics == null ? null : metrics.Begin(method, path);
                if (timing != null) request.Properties[AttendanceHttpHandler.TimingKey] = timing;
                double authStarted = metrics == null ? 0 : metrics.Elapsed;
                string token;
                try { token = await AccessToken(timing).ConfigureAwait(false); }
                finally
                {
                    if (metrics != null) metrics.RecordAuth(authStarted, metrics.Elapsed);
                    if (timing != null) timing.TokenMs = metrics.Elapsed - authStarted;
                }
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (method == "GET") request.Headers.Add("X-Firebase-ETag", "true");
                if (etag != null) request.Headers.TryAddWithoutValidation("if-match", etag);
                if (data != null) request.Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response;
                double started = metrics == null ? 0 : metrics.Elapsed;
                try { response = await Client.SendAsync(request).ConfigureAwait(false); }
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
                    throw new InvalidOperationException("Firebase refused access. Renew the server OAuth access token and check its permissions; do not open public database rules.");
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

    // نسجل أزمنة فقط؛ لا نسجل هيدر Authorization أو محتوى الرد أو معرّفات الأشخاص.
    internal sealed class AttendanceRequestTiming
    {
        public int Id;
        public string Branch, Method;
        public double StartMs, EndMs, TokenMs, TokenLockMs, TokenFileMs, HeadersMs, BufferMs, ParseMs;
        public bool TokenRefreshed;
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
