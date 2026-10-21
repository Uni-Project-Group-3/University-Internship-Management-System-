using System;
using System.Web.Security;
using System.Web.UI;

namespace University_Internship_Management_System.Coordinator
{
    public partial class Assign_Supervisors : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            University_Internship_Management_System.Services.SessionService.Logout(University_Internship_Management_System.Services.SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
