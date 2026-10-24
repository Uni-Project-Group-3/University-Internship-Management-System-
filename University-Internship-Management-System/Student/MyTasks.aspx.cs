using System;
using System.Web.Security;
using System.Web.UI;

namespace University_Internship_Management_System.Student
{
    public partial class MyTasks : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
        }

        protected void btnLogout_Click(object sender, EventArgs e) { University_Internship_Management_System.Shared.LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }
    }
}
