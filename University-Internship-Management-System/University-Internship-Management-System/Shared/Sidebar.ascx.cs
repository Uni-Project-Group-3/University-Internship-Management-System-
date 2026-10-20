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

            bool isHome = string.Equals(Request.AppRelativeCurrentExecutionFilePath,
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
