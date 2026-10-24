using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    public sealed class FirebaseLoginResult
    {
        public string Uid { get; set; }
        public string Role { get; set; }
        public bool MustChangePassword { get; set; }
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public JObject Profile { get; set; }
    }

    public sealed class FirebaseLoginException : Exception
    {
        public int StatusCode { get; private set; }
        public FirebaseLoginException(string message) : this(message, 401) { }
        public FirebaseLoginException(string message, int statusCode) : base(message) { StatusCode = statusCode; }
    }

    public sealed class FirebaseSignInService
    {
        private static readonly HttpClient SharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private readonly HttpClient client;

        private readonly string apiKey;
        private readonly Func<string, Task<string>> verify;
        private readonly Func<string, string, Task<JObject>> profile;
        private readonly UIMS.Services.FirebaseConfiguration firebaseConfiguration;

        public FirebaseSignInService() : this(SharedClient, ApiKey(), VerifyAsync,
            (uid, idToken) => ReadProfileAsync(uid, idToken), new UIMS.Services.FirebaseConfiguration()) { }

        public FirebaseSignInService(HttpClient client, string apiKey,
            Func<string, Task<string>> verify, Func<string, Task<JObject>> profile)
            : this(client, apiKey, verify, (uid, idToken) => profile(uid), new UIMS.Services.FirebaseConfiguration()) { }

        public FirebaseSignInService(HttpClient client, string apiKey,
            Func<string, Task<string>> verify, Func<string, Task<JObject>> profile,
            UIMS.Services.FirebaseConfiguration configuration)
            : this(client, apiKey, verify, (uid, idToken) => profile(uid), configuration) { }

        internal FirebaseSignInService(HttpClient client, string apiKey,
            Func<string, Task<string>> verify, Func<string, string, Task<JObject>> profile,
            UIMS.Services.FirebaseConfiguration configuration)
        {
            this.client = client; this.apiKey = apiKey; this.verify = verify; this.profile = profile;
            firebaseConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public static string ApiKey()
        {
            return Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ??
                ConfigurationManager.AppSettings["FirebaseApiKey"];
        }

        public async Task<FirebaseLoginResult> SignInAsync(string email, string password)
        {
            try { firebaseConfiguration.EnsureAuthenticationEnabled(); }
            catch (UIMS.Services.FirebaseException error) { throw new FirebaseLoginException(error.Message, 503); }
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new FirebaseLoginException("Login needs the Firebase Web API key configured on the server.");
            using (var content = new StringContent(JsonConvert.SerializeObject(new
            {
                email = email.Trim(), password, returnSecureToken = true
            }), Encoding.UTF8, "application/json"))
            using (var response = await client.PostAsync(
                "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + Uri.EscapeDataString(apiKey), content))
            {
                var body = JObject.Parse(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    string code = (string)body["error"]?["message"] ?? "";
                    if (code.StartsWith("API_KEY_INVALID") || code.StartsWith("CONFIGURATION_NOT_FOUND") || code.StartsWith("OPERATION_NOT_ALLOWED"))
                        throw new FirebaseLoginException("Check the Firebase Web API key and enable Email/Password sign-in for this project.");
                    if (code.StartsWith("TOO_MANY_ATTEMPTS") || (int)response.StatusCode == 429)
                        throw new FirebaseLoginException("Too many sign-in attempts. Wait a minute and try again.");
                    throw new FirebaseLoginException("Sign-in failed. Check the email and password, or contact your coordinator if the account is disabled.");
                }
                string idToken = (string)body["idToken"];
                string localId = (string)body["localId"];
                if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(localId))
                    throw new FirebaseLoginException("Firebase returned an incomplete sign-in response.");
                // The HTTPS Identity Toolkit sign-in response is the trusted source of this UID.
                // Avoid a redundant accounts:lookup round trip during login; VerifySessionAsync
                // still verifies bearer tokens before protected API operations.
                string uid = localId;
                var result = ProfileResult(uid, await profile(uid, idToken).ConfigureAwait(false));
                result.IdToken = idToken;
                result.RefreshToken = (string)body["refreshToken"];
                int expires;
                if (!int.TryParse((string)body["expiresIn"], out expires) || expires < 1 || string.IsNullOrWhiteSpace(result.RefreshToken))
                    throw new FirebaseLoginException("Firebase returned an incomplete authentication session.");
                result.ExpiresIn = expires;
                return result;
            }
        }

        public async Task<FirebaseLoginResult> RefreshAsync(string refreshToken)
        {
            try { firebaseConfiguration.EnsureAuthenticationEnabled(); }
            catch (UIMS.Services.FirebaseException error) { throw new FirebaseLoginException(error.Message, 503); }
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 8192)
                throw new FirebaseLoginException("The authentication session is incomplete. Sign in again.");
            var form = new System.Collections.Generic.Dictionary<string, string>
            {
                { "grant_type", "refresh_token" }, { "refresh_token", refreshToken }
            };
            using (var content = new FormUrlEncodedContent(form))
            using (var response = await client.PostAsync("https://securetoken.googleapis.com/v1/token?key=" + Uri.EscapeDataString(apiKey), content).ConfigureAwait(false))
            {
                var body = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (!response.IsSuccessStatusCode)
                    throw new FirebaseLoginException("The authentication session expired or was revoked. Sign in again.");
                string idToken = (string)body["id_token"];
                string uid = (string)body["user_id"];
                string nextRefresh = (string)body["refresh_token"];
                int expires;
                if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(nextRefresh) ||
                    !int.TryParse((string)body["expires_in"], out expires) || expires < 1)
                    throw new FirebaseLoginException("Firebase returned an incomplete refreshed session.");
                if (!string.Equals(uid, await verify(idToken).ConfigureAwait(false), StringComparison.Ordinal))
                    throw new FirebaseLoginException("The refreshed Firebase identity could not be verified.");
                var result = ProfileResult(uid, await profile(uid, idToken).ConfigureAwait(false));
                result.IdToken = idToken;
                result.RefreshToken = nextRefresh;
                result.ExpiresIn = expires;
                return result;
            }
        }

        public async Task<FirebaseLoginResult> VerifySessionAsync(string idToken)
        {
            try { firebaseConfiguration.EnsureAuthenticationEnabled(); }
            catch (UIMS.Services.FirebaseException error) { throw new FirebaseLoginException(error.Message, 503); }
            if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 8192)
                throw new FirebaseLoginException("A valid Firebase sign-in is required.");
            string uid = await verify(idToken).ConfigureAwait(false);
            var result = ProfileResult(uid, await profile(uid, idToken).ConfigureAwait(false));
            result.IdToken = idToken;
            return result;
        }

        public static FirebaseLoginResult ProfileResult(string uid, JObject value)
        {
            if (value == null)
                throw new FirebaseLoginException("Your email and password were accepted, but your database profile was not found. The profile must be at users/" + uid + " using the exact Authentication UID.", 403);
            if (value.Value<bool?>("isActive") != true)
                throw new FirebaseLoginException("Your profile exists, but isActive is not the boolean true. Profile: users/" + uid, 403);
            string setupStatus = StudentManagementService.Text(value, "accountSetupStatus");
            if (setupStatus != "" && setupStatus != "complete")
                throw new FirebaseLoginException("Your account setup is unfinished. Profile: users/" + uid + ". Setup status: " + setupStatus, 403);
            if (StudentManagementService.Text(value, "deletionStatus") == "pending")
                throw new FirebaseLoginException("Your account is pending deletion. Contact your coordinator.", 403);
            string role = CanonicalRole(StudentManagementService.Text(value, "role"));
            if (role == null) throw new FirebaseLoginException("Your account has no supported role. Contact your coordinator.", 403);
            // Password setup and recovery use the Firebase email link. Legacy profile flags
            // must not reopen the old first-login dialog after a successful sign-in.
            return new FirebaseLoginResult { Uid = uid, Role = role, MustChangePassword = false, Profile = LinkOnlyProfile(value) };
        }

        private static JObject LinkOnlyProfile(JObject value)
        {
            var profile = (JObject)value.DeepClone();
            profile["mustChangePassword"] = false;
            return profile;
        }

        public static string CanonicalRole(string role)
        {
            switch ((role ?? "").Trim().ToLowerInvariant())
            {
                case "coordinator": case "admin": return "coordinator";
                case "student": return "student";
                case "academicsupervisor": case "academic_supervisor": case "academic": return "academicSupervisor";
                case "companysupervisor": case "company_supervisor": case "company": return "companySupervisor";
                default: return null;
            }
        }

        public static string Destination(string role)
        {
            switch (role)
            {
                case "coordinator": return "~/Coordinator/ManageStudents.aspx";
                case "student": return "~/Student/StudentDashboard.aspx";
                case "academicSupervisor": return "~/Academic/AcademicDashboard.aspx";
                case "companySupervisor": return "~/Company/CompanyDashboard.aspx";
                default: throw new FirebaseLoginException("Account role is unavailable.");
            }
        }

        private static async Task<string> VerifyAsync(string idToken)
        {
            string key = ApiKey();
            if (string.IsNullOrWhiteSpace(key)) throw new FirebaseLoginException("Firebase Authentication is missing its Web API key.", 503);
            using (var content = new StringContent(JsonConvert.SerializeObject(new { idToken }), Encoding.UTF8, "application/json"))
            using (var response = await SharedClient.PostAsync("https://identitytoolkit.googleapis.com/v1/accounts:lookup?key=" + Uri.EscapeDataString(key), content).ConfigureAwait(false))
            {
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject body;
                try { body = JObject.Parse(json); }
                catch (JsonException) { throw new FirebaseLoginException("Firebase Authentication returned an invalid verification response.", 503); }
                if (!response.IsSuccessStatusCode)
                    throw new FirebaseLoginException("The Firebase sign-in token is expired, invalid, or revoked. Sign in again.", 401);
                var users = body["users"] as JArray;
                var user = users != null && users.Count > 0 ? users[0] as JObject : null;
                string uid = (string)user?["localId"];
                if (string.IsNullOrWhiteSpace(uid) || user.Value<bool?>("disabled") == true)
                    throw new FirebaseLoginException("The Firebase Authentication account is disabled or unavailable.", 403);
                return uid;
            }
        }

        public static async Task<JObject> ReadProfileAsync(string uid)
        {
            var context = HttpContext.Current;
            string idToken = context?.Session?["Uims.FirebaseIdToken"] as string;
            return await ReadProfileAsync(uid, idToken).ConfigureAwait(false);
        }

        public static async Task<JToken> ReadDatabasePathAsync(string collection, string idToken)
        {
            var config = new UIMS.Services.FirebaseConfiguration();
            config.EnsureAuthenticationEnabled();
            if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 8192)
                throw new FirebaseLoginException("A valid Firebase sign-in is required to read database data.", 401);
            string path = "";
            if (!string.IsNullOrWhiteSpace(collection))
            {
                string[] segments = collection.Trim().Split('/');
                if (Array.Exists(segments, segment => string.IsNullOrWhiteSpace(segment) || segment == "." || segment == ".."))
                    throw new ArgumentException("A valid Firebase database path is required.", nameof(collection));
                path = "/" + string.Join("/", Array.ConvertAll(segments, Uri.EscapeDataString));
            }
            string query = string.IsNullOrEmpty(path) ? "?shallow=true&auth=" : "?auth=";
            string url = config.DatabaseUrl.TrimEnd('/') + path + ".json" + query + Uri.EscapeDataString(idToken);
            using (var response = await SharedClient.GetAsync(url).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new FirebaseLoginException("Firebase Rules denied access to this database path.", 403);
                if (!response.IsSuccessStatusCode)
                    throw new FirebaseLoginException("The Firebase database could not be reached.", 503);
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try { return string.IsNullOrWhiteSpace(json) ? JValue.CreateNull() : JToken.Parse(json); }
                catch (JsonException) { throw new FirebaseLoginException("Firebase returned invalid JSON for this database path.", 503); }
            }
        }

        public static async Task PatchDatabasePathAsync(string path, object value, string idToken)
        {
            var config = new UIMS.Services.FirebaseConfiguration();
            config.EnsureWritesAllowed();
            if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 8192)
                throw new FirebaseLoginException("A valid Firebase sign-in is required to save database data.", 401);
            string[] segments = (path ?? "").Split('/');
            if (string.IsNullOrWhiteSpace(path) ||
                Array.Exists(segments, segment => string.IsNullOrWhiteSpace(segment) || segment == "." || segment == ".."))
                throw new ArgumentException("A valid Firebase database path is required.", nameof(path));
            string escapedPath = string.Join("/", Array.ConvertAll(segments, Uri.EscapeDataString));
            string url = config.DatabaseUrl.TrimEnd('/') + "/" + escapedPath + ".json?auth=" + Uri.EscapeDataString(idToken);
            using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
                using (var response = await SharedClient.SendAsync(request).ConfigureAwait(false))
                {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new FirebaseLoginException("Firebase Rules denied the database update. Check the authenticated write rules.", 403);
                if (!response.IsSuccessStatusCode)
                    throw new FirebaseLoginException("The Firebase database could not save the profile update.", 503);
                }
            }
        }

        private static async Task<JObject> ReadProfileAsync(string uid, string idToken)
        {
            var config = new UIMS.Services.FirebaseConfiguration();
            config.EnsureAuthenticationEnabled();
            if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 8192)
                throw new FirebaseLoginException("A valid Firebase sign-in is required to read the matching user profile.", 401);
            string key = StudentManagementService.Key(uid);
            string url = config.DatabaseUrl.TrimEnd('/') + "/users/" + Uri.EscapeDataString(key) + ".json?auth=" + Uri.EscapeDataString(idToken);
            using (var response = await SharedClient.GetAsync(url).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new FirebaseLoginException("Firebase denied access to users/" + key + ". Check the authenticated database Rules and UID-to-profile mapping.", 403);
                if (!response.IsSuccessStatusCode)
                    throw new FirebaseLoginException("The Firebase user profile could not be read. Check database connectivity.", 503);
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json) || string.Equals(json.Trim(), "null", StringComparison.OrdinalIgnoreCase)) return null;
                try
                {
                    var result = JObject.Parse(json);
                    if (CanonicalRole(StudentManagementService.Text(result, "role")) == "companySupervisor")
                    {
                        string companyId = StudentManagementService.Text(result, "companyId");
                        if (string.IsNullOrWhiteSpace(companyId)) throw new FirebaseLoginException("Your company account is not linked to a company.", 403);
                        StudentManagementService.Key(companyId);
                        // Use the existing server gateway; no additional client database Rules are needed.
                        var company = await new StudentFirebaseGateway().ReadAsync("companies/" + companyId).ConfigureAwait(false) as JObject;
                        if (!CompanyManagementService.AllowsSignIn(company)) throw new FirebaseLoginException("Your company is inactive or unavailable. Contact your coordinator.", 403);
                    }
                    return result;
                }
                catch (JsonException) { throw new FirebaseLoginException("The profile at users/" + key + " is not a valid object.", 403); }
            }
        }
    }
}
