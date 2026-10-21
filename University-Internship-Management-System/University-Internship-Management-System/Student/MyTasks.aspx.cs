using System;
using University_Internship_Management_System.Services;
using System.Web.UI;

namespace University_Internship_Management_System.Student
{
    public partial class MyTasks : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            SessionService.Logout(SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
