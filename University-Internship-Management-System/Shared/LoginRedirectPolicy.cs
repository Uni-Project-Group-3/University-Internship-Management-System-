using System;

namespace University_Internship_Management_System.Shared
{
    public static class LoginSession
    {
        public static void End(System.Web.HttpContext context)
        {
            context.Items["Uims.SignedOut"] = true;
            var identity = context.User?.Identity as System.Web.Security.FormsIdentity;
            string role = identity == null ? null : FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
            foreach (string name in CookieNames(role))
                context.Response.Cookies.Add(new System.Web.HttpCookie(name, "")
                {
                    Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true,
                    Secure = context.Request.IsSecureConnection, SameSite = System.Web.SameSiteMode.Lax,
                    Path = System.Web.Security.FormsAuthentication.FormsCookiePath
                });
            context.Session?.Remove("Uims.FirebaseIdToken");
            context.Response.Cache.SetNoStore();
            context.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(""), new string[0]);
            System.Threading.Thread.CurrentPrincipal = context.User;
        }

        public static string[] CookieNames(string role)
        {
            string canonical = FirebaseSignInService.CanonicalRole(role);
            var names = new System.Collections.Generic.List<string> { System.Web.Security.FormsAuthentication.FormsCookieName, ".UIMS.FirstLogin" };
            if (canonical != null) names.Add(".UIMS.Role." + canonical);
            return names.ToArray();
        }
    }

    public static class LoginRedirectPolicy
    {
        // Return only fixed routes after the account's real role has been verified.
        // Never return the supplied URL itself or allow another role into coordinator pages.
        public static string CoordinatorPage(string role, string returnUrl)
        {
            if (role != "coordinator" || string.IsNullOrWhiteSpace(returnUrl)) return null;
            string path = returnUrl;
            if (path.StartsWith("~/", StringComparison.Ordinal)) path = path.Substring(2);
            else if (path.StartsWith("/", StringComparison.Ordinal)) path = path.Substring(1);
            else return null;
            foreach (string page in new[] { "ManageStudents", "ManageCompanies", "Manage_Academic_Supervisor" })
                if (string.Equals(path, "Coordinator/" + page, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(path, "Coordinator/" + page + ".aspx", StringComparison.OrdinalIgnoreCase))
                    return "~/Coordinator/" + page + ".aspx";
            return null;
        }
    }
}
