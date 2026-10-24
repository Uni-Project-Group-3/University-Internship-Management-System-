using System;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS
{
    /// <summary>
    /// Narrow authentication bridge used by firebase-client.js. It deliberately
    /// exposes no generic database path, REST proxy, or browser-selected identity.
    /// </summary>
    public sealed class FirebaseApiHandler : HttpTaskAsyncHandler, IRequiresSessionState
    {
        private static readonly string[] InspectableRoots = {
            "users", "students", "companies", "academicSupervisors", "opportunities",
            "applications", "placements", "tasks", "taskBoards", "attendance",
            "weeklyReports", "weeklyReportReviews", "taskExams", "taskExamAttempts",
            "finalEvaluations", "completionReviews", "notifications", "contactMessages"
        };

        public override bool IsReusable { get { return false; } }

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.SuppressFormsAuthenticationRedirect = true;
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
            context.Response.ContentType = "application/json; charset=utf-8";
            // FormsAuthentication otherwise converts the JSON 401 returned for a rejected
            // Firebase sign-in into a 302 login-page redirect, which hides the real API error.
            context.Response.SuppressFormsAuthenticationRedirect = true;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "same-origin";
            try
            {
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                    throw new ApiFailure(405, "Use POST for this endpoint.");
                RequireSameOrigin(context.Request);
                if (context.Request.ContentLength > 16384)
                    throw new ApiFailure(413, "The request is too large.");
                string raw;
                using (var reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8)) raw = await reader.ReadToEndAsync();
                if (raw.Length > 16384) throw new ApiFailure(413, "The request is too large.");
                JObject request = JObject.Parse(raw);
                object result = await DispatchAsync(context, request);
                Write(context, 200, new { success = true, data = result });
            }
            catch (ApiFailure error) { Write(context, error.Status, new { success = false, message = error.Message }); }
            catch (FirebaseLoginException error)
            {
                if (error.RetryAfterSeconds > 0) context.Response.Headers["Retry-After"] = error.RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Write(context, error.StatusCode, new { success = false, message = error.Message, retryAfterSeconds = error.RetryAfterSeconds });
            }
            catch (UIMS.Services.FirebaseException error) { Write(context, error.StatusCode, new { success = false, message = error.Message }); }
            catch (UnauthorizedAccessException) { Write(context, 403, new { success = false, message = "This account is not authorized for that operation." }); }
            catch (Exception error)
            {
                context.Trace.Warn("FirebaseApi", error.GetType().Name);
                Write(context, 503, new { success = false, message = "Firebase authentication or the authorized profile service is unavailable. No database change was confirmed." });
            }
        }

        private static async Task<object> DispatchAsync(HttpContext context, JObject request)
        {
            string action = (string)request["action"];
            switch (action)
            {
                case "configuration": return Configuration();
                case "signIn": return await SignInAsync(context, request);
                case "refresh": return await RefreshAsync(context, request);
                case "identity": return await IdentityAsync(context);
                case "logout": return Logout(context);
                case "inspectSchema": return await InspectSchemaAsync(context, request);
                default: throw new ApiFailure(400, "This Firebase API action is not supported.");
            }
        }

        private static object Configuration()
        {
            var config = new UIMS.Services.FirebaseConfiguration();
            if (!config.AuthenticationEnabled)
                return new { enabled = false, message = "Firebase Authentication is disabled in this environment. No external request was made." };
            config.EnsureAuthenticationEnabled();
            return new { enabled = true, projectId = config.ProjectId, message = "Firebase Authentication configuration is available." };
        }

        private static async Task<object> SignInAsync(HttpContext context, JObject request)
        {
            string email = ((string)request["email"] ?? "").Trim();
            string password = (string)request["password"];
            if (email.Length == 0 || email.Length > 254 || password == null || password.Length == 0 || password.Length > 4096)
                throw new ApiFailure(400, "Enter a valid email address and password.");
            var blockedUntil = context.Session["Uims.LoginBlockedUntil"] as DateTime?;
            if (blockedUntil.HasValue && blockedUntil.Value > DateTime.UtcNow)
                throw new FirebaseLoginException("Please wait before trying to sign in again.", 429, Math.Max(1, (int)Math.Ceiling((blockedUntil.Value - DateTime.UtcNow).TotalSeconds)));
            try
            {
                var result = await new FirebaseSignInService().SignInAsync(email, password);
                context.Session["Uims.LoginAttempts"] = 0;
                context.Session["Uims.FirebaseIdToken"] = result.IdToken;
                if (result.MustChangePassword) IssueFirstLoginCookie(context, result.Uid);
                else IssueFormsCookie(context, result);
                return SessionJson(result);
            }
            catch (FirebaseLoginException error)
            {
                if (error.StatusCode == 429)
                    context.Session["Uims.LoginBlockedUntil"] = DateTime.UtcNow.AddSeconds(Math.Max(1, error.RetryAfterSeconds));
                else if (error.StatusCode == 401)
                {
                    int attempts = (context.Session["Uims.LoginAttempts"] as int? ?? 0) + 1;
                    context.Session["Uims.LoginAttempts"] = attempts >= 5 ? 0 : attempts;
                    if (attempts >= 5) context.Session["Uims.LoginBlockedUntil"] = DateTime.UtcNow.AddMinutes(1);
                }
                throw;
            }
        }

        private static async Task<object> RefreshAsync(HttpContext context, JObject request)
        {
            var result = await new FirebaseSignInService().RefreshAsync((string)request["refreshToken"]);
            context.Session["Uims.FirebaseIdToken"] = result.IdToken;
            IssueFormsCookie(context, result);
            return SessionJson(result);
        }

        private static async Task<object> IdentityAsync(HttpContext context)
        {
            var identity = RequireBearer(context.Request);
            var result = await new FirebaseSignInService().VerifySessionAsync(identity);
            context.Session["Uims.FirebaseIdToken"] = identity;
            return new { uid = result.Uid, profileId = result.Uid, role = result.Role };
        }

        private static object Logout(HttpContext context)
        {
            LoginSession.End(context);
            return new { signedOut = true };
        }

        private static async Task<object> InspectSchemaAsync(HttpContext context, JObject request)
        {
            var token = RequireBearer(context.Request);
            var result = await new FirebaseSignInService().VerifySessionAsync(token);
            if (result.Role != "coordinator") throw new UnauthorizedAccessException();
            string collection = ((string)request["collection"] ?? "").Trim();
            if (collection.Length != 0 && !InspectableRoots.Contains(collection, StringComparer.Ordinal))
                throw new ApiFailure(400, "Choose one of the supported Firebase collections.");
            JToken value = await FirebaseSignInService.ReadDatabasePathAsync(collection, token);
            var records = value as JObject;
            string[] keys = records == null ? new string[0] : records.Properties().Select(p => p.Name).Take(500).ToArray();
            return new { collection = collection.Length == 0 ? "/" : "/" + collection, recordKeys = keys, truncated = keys.Length == 500 };
        }

        private static string RequireBearer(HttpRequest request)
        {
            string authorization = request.Headers["Authorization"] ?? "";
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || authorization.Length <= 7)
                throw new ApiFailure(401, "A verified Firebase sign-in is required.");
            return authorization.Substring(7).Trim();
        }

        private static object SessionJson(FirebaseLoginResult result)
        {
            return new { idToken = result.IdToken, refreshToken = result.RefreshToken, expiresIn = result.ExpiresIn,
                profileId = result.Uid, role = result.Role, mustChangePassword = result.MustChangePassword,
                tabTicket = result.MustChangePassword ? null : FormsAuthentication.Encrypt(new FormsAuthenticationTicket(2, result.Uid, DateTime.Now, DateTime.Now.AddMinutes(30), false, result.Role, FormsAuthentication.FormsCookiePath)) };
        }

        private static void IssueFormsCookie(HttpContext context, FirebaseLoginResult result)
        {
            var previous = context.User?.Identity as FormsIdentity;
            string previousRole = previous == null || previous.Ticket.Expired ? null : FirebaseSignInService.CanonicalRole(previous.Ticket.UserData);
            if (previousRole != null && previousRole != result.Role)
                context.Response.Cookies.Add(new HttpCookie(".UIMS.Role." + previousRole, FormsAuthentication.Encrypt(previous.Ticket))
                { HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax, Path = FormsAuthentication.FormsCookiePath });
            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(2, result.Uid, now, now.AddMinutes(30), false,
                result.Role, FormsAuthentication.FormsCookiePath);
            context.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax,
                Path = FormsAuthentication.FormsCookiePath
            });
            context.Response.Cookies.Add(new HttpCookie(".UIMS.Role." + result.Role, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax,
                Path = FormsAuthentication.FormsCookiePath
            });
        }

        private static void IssueFirstLoginCookie(HttpContext context, string uid)
        {
            var now = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(2, uid, now, now.AddMinutes(10), false,
                "password-change", FormsAuthentication.FormsCookiePath);
            context.Response.Cookies.Add(new HttpCookie(".UIMS.FirstLogin", FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax,
                Path = FormsAuthentication.FormsCookiePath
            });
        }

        private static void RequireSameOrigin(HttpRequest request)
        {
            string origin = request.Headers["Origin"];
            Uri source = null;
            if (!string.IsNullOrWhiteSpace(origin)) Uri.TryCreate(origin, UriKind.Absolute, out source);
            else if (request.UrlReferrer != null) source = request.UrlReferrer;
            if (source == null || !string.Equals(source.Scheme, request.Url.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(source.Authority, request.Url.Authority, StringComparison.OrdinalIgnoreCase))
                throw new ApiFailure(403, "The request origin could not be verified. Reload the page and try again.");
        }

        private static void Write(HttpContext context, int status, object body)
        {
            context.Response.SuppressFormsAuthenticationRedirect = true;
            context.Response.StatusCode = status;
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.Write(JsonConvert.SerializeObject(body));
        }

        private sealed class ApiFailure : Exception
        {
            public int Status { get; private set; }
            public ApiFailure(int status, string message) : base(message) { Status = status; }
        }
    }
}
