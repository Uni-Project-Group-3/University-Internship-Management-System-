using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace University_Internship_Management_System.Shared
{
    // A small wrapper over the Firebase Realtime Database REST API
    // Web.config: FirebaseUrl and FirebaseSecret (the secret is optional if your rules allow access)
    public class FirebaseDb
    {
        static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        static readonly JsonSerializerSettings settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };

        static FirebaseDb()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        readonly UIMS.Services.FirebaseConfiguration configuration = new UIMS.Services.FirebaseConfiguration();

        string Url(string path)
        {
            configuration.EnsureEnabled();
            return configuration.DatabaseUrl.TrimEnd('/') + "/" + (path ?? "").Trim('/') + ".json";
        }

        string Send(HttpMethod method, string path, object body)
        {
            if (method != HttpMethod.Get) configuration.EnsureWritesAllowed();
            return Task.Run(async () =>
            {
                using (var request = new HttpRequestMessage(method, Url(path)))
                {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                    await FirebaseServerCredential.AccessTokenAsync(System.Threading.CancellationToken.None).ConfigureAwait(false));
                if (body != null)
                    request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                using (var response = await http.SendAsync(request))
                {
                    string text = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        throw new AppError("Firebase records are unavailable (HTTP " + (int)response.StatusCode + ").");
                    return text;
                }
                }
            }).GetAwaiter().GetResult();
        }

        // Read a node (null if it does not exist)
        public JToken Get(string path)
        {
            string text = Send(HttpMethod.Get, path, null);
            return string.IsNullOrWhiteSpace(text) ? null : Parse(text);
        }

        public JObject GetObject(string path)
        {
            return Get(path) as JObject;
        }

        // All records under a node, e.g. List("opportunities")
        public List<JObject> List(string path)
        {
            var node = Get(path) as JObject;
            var result = new List<JObject>();
            if (node == null) return result;
            foreach (var p in node.Properties())
            {
                var record = p.Value as JObject;
                if (record == null) continue;
                if (record["id"] == null) record["id"] = p.Name;
                result.Add(record);
            }
            return result;
        }

        // Replace a node in full
        public void Set(string path, object value)
        {
            Send(HttpMethod.Put, path, value);
        }

        // Update fields only. Update("", {...}) writes several paths at once (atomically)
        public void Update(string path, object value)
        {
            Send(new HttpMethod("PATCH"), path, value);
        }

        public static JToken Parse(string json)
        {
            return JsonConvert.DeserializeObject<JToken>(json, settings);
        }

        public static string NewId(string prefix)
        {
            return prefix + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        // Prevent any ID from the browser from being used to reach another path in Firebase
        public static string Key(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, "^[A-Za-z0-9_-]{1,128}$"))
                throw new AppError("Invalid record id.");
            return value;
        }

        public static string Now
        {
            get { return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture); }
        }
    }

    // An error whose message we show to the user as-is
    public class AppError : Exception
    {
        public AppError(string message) : base(message) { }
    }
}
