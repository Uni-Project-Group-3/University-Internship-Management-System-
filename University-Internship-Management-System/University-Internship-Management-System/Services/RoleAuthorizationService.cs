using System;
using System.Collections.Generic;

namespace University_Internship_Management_System.Services
{
    public static class RoleAuthorizationService
    {
        private static readonly Dictionary<string, string> Dashboards = new Dictionary<string, string>
        {
            { "student", "Student/StudentDashboard.aspx" }, { "company", "Company/CompanyDashboard.aspx" },
            { "academic", "Academic/AcademicDashboard.aspx" }, { "admin", "Coordinator/CoordinatorDashboard.aspx" }
        };
        public static string NormalizeRole(string role)
        {
            switch (role) { case "academicSupervisor": return "academic"; case "companySupervisor": return "company"; case "coordinator": return "admin"; default: return role; }
        }
        public static bool IsSupported(string role) => role != null && Dashboards.ContainsKey(role);
        public static string Dashboard(UimsAccount account) => Dashboards[account.Role];
        public static bool IsIdentityDashboard(UimsAccount account, string path)
        {
            if (account == null || account.MustChangePassword || !IsSupported(account.Role)) return false;
            string route = (path ?? "").TrimStart('~', '/');
            if (!route.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase)) route += ".aspx";
            return string.Equals(route, Dashboard(account), StringComparison.OrdinalIgnoreCase);
        }
        public static bool AllowsPath(UimsAccount account, string path)
        {
            string folder = path.TrimStart('~', '/').Split('/')[0];
            return string.Equals(folder, account.Role == "admin" ? "Coordinator" : account.Role, StringComparison.OrdinalIgnoreCase);
        }
        public static void RequireCompany(UimsAccount account, string companyId)
        {
            if (account.Role != "company" || string.IsNullOrEmpty(companyId) || account.CompanyId != companyId)
                throw new UnauthorizedAccessException();
        }
        public static void RequireStudent(UimsAccount account, string studentUid)
        {
            if (account.Role != "student" || account.Uid != studentUid) throw new UnauthorizedAccessException();
        }
        public static void RequireAssignedAcademic(UimsAccount account, string supervisorUid, string department)
        {
            if (account.Role != "academic" || account.Uid != supervisorUid || account.Department != department)
                throw new UnauthorizedAccessException();
        }
    }
}
