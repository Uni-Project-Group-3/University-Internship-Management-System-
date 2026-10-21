using System;
using System.Web.UI;

namespace University_Internship_Management_System.Shared
{
    public partial class Sidebar : UserControl
    {
        public string HomeUrl { get; set; } = "~/Default.aspx";
        public string UserRole { get; set; } = "User";

        protected override void OnPreRender(EventArgs e)
        {
            HomeLink.NavigateUrl = HomeUrl;
            RoleLabel.Text = UserRole;
            PlacementsLink.Visible = string.Equals(UserRole, "Coordinator", StringComparison.OrdinalIgnoreCase)
                || string.Equals(UserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(Page.AppRelativeVirtualPath, "~/Coordinator/MonitorPlacementsProgress.aspx", StringComparison.OrdinalIgnoreCase))
            {
                PlacementsLink.CssClass = "app-sidebar__link is-active";
                PlacementsLink.Attributes["aria-current"] = "page";
            }
            ManageCompaniesLink.Visible = string.Equals(UserRole, "Coordinator", StringComparison.OrdinalIgnoreCase)
                || string.Equals(UserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(Page.AppRelativeVirtualPath, "~/Coordinator/ManageCompanies.aspx", StringComparison.OrdinalIgnoreCase))
            {
                ManageCompaniesLink.CssClass = "app-sidebar__link is-active";
                ManageCompaniesLink.Attributes["aria-current"] = "page";
            }
            ProgressMonitorLink.Visible = string.Equals(UserRole, "Academic", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(Page.AppRelativeVirtualPath, "~/Academic/StudentProgressMonitor.aspx", StringComparison.OrdinalIgnoreCase))
            {
                ProgressMonitorLink.CssClass += " is-active";
                ProgressMonitorLink.Attributes["aria-current"] = "page";
            }

            bool isHome = string.Equals(Page.AppRelativeVirtualPath,
                HomeUrl, StringComparison.OrdinalIgnoreCase);
            HomeLink.CssClass = isHome ? "app-sidebar__link is-active" : "app-sidebar__link";

            if (isHome)
            {
                HomeLink.Attributes["aria-current"] = "page";
            }
            else
            {
                HomeLink.Attributes.Remove("aria-current");
            }

            base.OnPreRender(e);
        }
    }
}
