using System;
using System.Web.UI;

namespace University_Internship_Management_System.Shared
{
    public partial class HomeNavbar : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            foreach (var link in new[] { HomeLink, AboutLink, ContactLink, LoginLink })
            {
                bool isActive = string.Equals(Page.AppRelativeVirtualPath,
                    link.HRef, StringComparison.OrdinalIgnoreCase);
                link.Attributes["class"] = "home-navbar__link" +
                    (link == LoginLink ? " home-navbar__sign-in" : "") +
                    (isActive ? " is-active" : "");

                if (isActive)
                {
                    link.Attributes["aria-current"] = "page";
                }
                else
                {
                    link.Attributes.Remove("aria-current");
                }
            }
        }
    }
}
