using System;
using System.Web.UI;

namespace University_Internship_Management_System
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            HomeNavigation.Visible = Page is _Default;
        }
    }
}
