using System;

namespace University_Internship_Management_System.Shared
{
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
