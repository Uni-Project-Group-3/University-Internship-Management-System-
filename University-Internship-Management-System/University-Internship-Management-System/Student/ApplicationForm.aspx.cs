using System;
using System.Web;
using System.Web.UI;
using University_Internship_Management_System.Services;

namespace University_Internship_Management_System.Student
{
    public partial class ApplicationForm : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            var account = Context.Items["Uims.Account"] as UimsAccount;
            if (account == null || account.Role != "student" || account.MustChangePassword) throw new UnauthorizedAccessException();
            lblUserName.Text = HttpUtility.HtmlEncode(account.DisplayName ?? account.Email);
            lblInitials.Text = "ST";
        }
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            SessionService.Logout(SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
