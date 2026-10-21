using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace University_Internship_Management_System.Services
{
    public sealed class VerifiedIdentity
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }

    public interface IFirebaseIdentityVerifier
    {
        Task<VerifiedIdentity> VerifyAsync(string idToken);
    }

    public interface IFirebaseAccountAdmin
    {
        Task CreateAsync(string uid, string email, string name, string password);
        Task SetPasswordAsync(string uid, string password);
        Task SetDisabledAsync(string uid, bool disabled);
        Task DeleteAsync(string uid);
        Task RevokeAsync(string uid);
    }

    public sealed class FirebaseAuthenticationService : IFirebaseIdentityVerifier, IFirebaseAccountAdmin
    {
        internal static readonly Lazy<FirebaseApp> App = new Lazy<FirebaseApp>(() =>
        {
            FirebaseConfiguration.ValidateCredentials();
            return FirebaseApp.Create(new AppOptions
            {
                ProjectId = FirebaseConfiguration.ProjectId,
                Credential = GoogleCredential.GetApplicationDefault()
            }, "uims-server");
        });
        public static void EnsureConfigured()
        {
            FirebaseConfiguration.ValidateCredentials();
            var app = App.Value;
        }

        public async Task<VerifiedIdentity> VerifyAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 16000)
                throw new UnauthorizedAccessException();
            try
            {
                var auth = FirebaseAuth.GetAuth(App.Value);
                var token = await auth.VerifyIdTokenAsync(idToken, true).ConfigureAwait(false);
                var user = await auth.GetUserAsync(token.Uid).ConfigureAwait(false);
                if (user.Disabled) throw new UnauthorizedAccessException();
                return new VerifiedIdentity { Uid = token.Uid, Email = user.Email,
                    ExpiresUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddSeconds(Convert.ToDouble(token.Claims["exp"])) };
            }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.InvalidIdToken ||
                error.AuthErrorCode == AuthErrorCode.ExpiredIdToken || error.AuthErrorCode == AuthErrorCode.RevokedIdToken ||
                error.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                throw new UnauthorizedAccessException("Sign-in is no longer valid.");
            }
        }

        public async Task CreateAsync(string uid, string email, string name, string password)
        {
            await FirebaseAuth.GetAuth(App.Value).CreateUserAsync(new UserRecordArgs
            { Uid = uid, Email = email, DisplayName = name, Password = password, Disabled = true }).ConfigureAwait(false);
        }
        public async Task SetPasswordAsync(string uid, string password)
        {
            await FirebaseAuth.GetAuth(App.Value).UpdateUserAsync(new UserRecordArgs { Uid = uid, Password = password }).ConfigureAwait(false);
        }
        public async Task SetDisabledAsync(string uid, bool disabled)
        {
            await FirebaseAuth.GetAuth(App.Value).UpdateUserAsync(new UserRecordArgs { Uid = uid, Disabled = disabled }).ConfigureAwait(false);
        }
        public async Task DeleteAsync(string uid)
        {
            try { await FirebaseAuth.GetAuth(App.Value).DeleteUserAsync(uid).ConfigureAwait(false); }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { }
        }
        public Task RevokeAsync(string uid) => FirebaseAuth.GetAuth(App.Value).RevokeRefreshTokensAsync(uid);

    }

    internal static class FirebaseHttp
    {
        internal static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        internal static async Task<JObject> PostIdentityAsync(string operation, object payload)
        {
            string url = "https://identitytoolkit.googleapis.com/v1/accounts:" + operation +
                "?key=" + Uri.EscapeDataString(FirebaseConfiguration.WebApiKey);
            using (var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"))
            using (var response = await Client.PostAsync(url, content).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) { await LoginDiagnostics.HttpFailureAsync(response, "FirebaseUidVerificationFailure").ConfigureAwait(false); throw new UnauthorizedAccessException("Authentication request was not accepted."); }
                return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }
    }
}

namespace University_Internship_Management_System.Services
{
    internal static class LoginDiagnostics
    {
        private static readonly object Gate = new object();
        internal static void Write(string category, int status = 0, string code = "NONE")
        {
            // Callers supply fixed categories and allowlisted codes, never exception text or request data.
            var context = System.Web.HttpContext.Current;
            string requestId = context?.Items["Uims.DiagnosticId"] as string;
            if (requestId == null && context != null) context.Items["Uims.DiagnosticId"] = requestId = Guid.NewGuid().ToString("N");
            string line = DateTime.UtcNow.ToString("O") + " request=" + (requestId ?? "background") + " category=" + category + " http=" + status + " code=" + code;
            System.Diagnostics.Trace.WriteLine("UIMS_AUTH " + line);
            try
            {
                string file = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/login-diagnostics.log");
                if (file == null) return;
                lock (Gate)
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
                    if (System.IO.File.Exists(file) && new System.IO.FileInfo(file).Length > 1048576)
                        {
                        string previous = file + ".previous";
                        if (System.IO.File.Exists(previous)) System.IO.File.Delete(previous);
                        System.IO.File.Move(file, previous);
                    }
                    System.IO.File.AppendAllText(file, line + Environment.NewLine);
                }
            }
            catch { System.Diagnostics.Trace.WriteLine("UIMS_AUTH diagnostic file unavailable"); }
        }
        internal static UnauthorizedAccessException Reject(string category)
        {
            Write(category);
            return new UnauthorizedAccessException();
        }
        internal static async Task HttpFailureAsync(HttpResponseMessage response, string category)
        {
            string code = "UNRECOGNIZED";
            try
            {
                var body = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                string candidate = (string)(body["error"] as JObject)?["message"];
                var allowed = new[] { "INVALID_LOGIN_CREDENTIALS", "EMAIL_NOT_FOUND", "INVALID_PASSWORD", "USER_DISABLED", "TOO_MANY_ATTEMPTS_TRY_LATER", "API_KEY_INVALID", "INVALID_API_KEY", "PROJECT_NOT_FOUND", "CONFIGURATION_NOT_FOUND", "OPERATION_NOT_ALLOWED", "INVALID_ID_TOKEN", "TOKEN_EXPIRED", "USER_NOT_FOUND", "INVALID_REFRESH_TOKEN" };
                if (Array.IndexOf(allowed, candidate) >= 0) code = candidate;
                if (candidate == "API_KEY_INVALID" || candidate == "INVALID_API_KEY" || candidate == "PROJECT_NOT_FOUND" || candidate == "CONFIGURATION_NOT_FOUND" || candidate == "OPERATION_NOT_ALLOWED")
                    category = "FirebaseConfigurationFailure";
            }
            catch (JsonException) { }
            Write(category, (int)response.StatusCode, code);
        }
    }
}
