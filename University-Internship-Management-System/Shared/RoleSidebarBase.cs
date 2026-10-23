using System;
using System.Web.UI;

namespace University_Internship_Management_System.Shared
{
    // Navigation only. User role and access are enforced by the existing page/API guards,
    // NOT by displaying or hiding menu links. No database or Firebase calls here.
    public class RoleSidebarBase : UserControl
    {
        public string SidebarId { get; set; } = "sidebar";

        protected string ActiveClass(string target)
        {
            return IsCurrent(target) ? " is-active active" : string.Empty;
        }

        protected string ActiveAria(string target)
        {
            return IsCurrent(target) ? " aria-current=\"page\"" : string.Empty;
        }

        private bool IsCurrent(string target)
        {
            return Page != null && string.Equals(Page.AppRelativeVirtualPath, target,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
