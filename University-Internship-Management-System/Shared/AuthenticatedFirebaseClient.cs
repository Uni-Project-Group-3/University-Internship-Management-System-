using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using University_Internship_Management_System.Services.FinalEvaluation;

namespace University_Internship_Management_System.Shared
{
    // Compatibility transport for the existing WebClient-based monitoring pages.
    // Credentials are attached only to the configured HTTPS database, never another host.
    public sealed class AuthenticatedFirebaseClient : WebClient
    {
        private static readonly GoogleAdcAccessTokenProvider Tokens = new GoogleAdcAccessTokenProvider();
        public static string DatabaseUrl { get { var c = new UIMS.Services.FirebaseConfiguration(); c.EnsureEnabled(); return c.DatabaseUrl.TrimEnd('/') + "/"; } }
        protected override WebRequest GetWebRequest(Uri address)
        {
            if (!address.AbsoluteUri.StartsWith(DatabaseUrl, StringComparison.Ordinal) || !string.IsNullOrEmpty(address.Query)) throw new InvalidOperationException("The request must use the configured Firebase database.");
            var request = (HttpWebRequest)base.GetWebRequest(address);
            if (request.Method != "GET") new UIMS.Services.FirebaseConfiguration().EnsureWritesAllowed();
            request.AllowAutoRedirect = false;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + Task.Run(() => Tokens.GetAccessTokenAsync()).GetAwaiter().GetResult();
            return request;
        }
        public static string RequireRole(HttpContext context, string role)
        {
            if (context?.User?.Identity?.IsAuthenticated != true) throw new HttpException(403, "Sign in with an active " + role + " account.");
            var gateway = new StudentFirebaseGateway(); string uid = context.User.Identity.Name;
            var account = Task.Run(() => gateway.FindAsync(uid)).GetAwaiter().GetResult();
            var profile = Task.Run(() => gateway.ReadAsync("users/" + StudentManagementService.Key(uid))).GetAwaiter().GetResult() as Newtonsoft.Json.Linq.JObject;
            if (account == null || account.Disabled || profile == null || profile.Value<bool?>("isActive") != true || FirebaseSignInService.CanonicalRole(StudentManagementService.Text(profile, "role")) != role)
                throw new HttpException(403, "An active " + role + " account is required.");
            return uid;
        }
    }
}
