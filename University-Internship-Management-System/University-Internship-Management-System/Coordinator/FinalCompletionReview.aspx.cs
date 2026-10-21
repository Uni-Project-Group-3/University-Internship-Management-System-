using System;
using System.Web;
using System.Web.UI;
using University_Internship_Management_System.Services;
namespace University_Internship_Management_System.Coordinator
{
    public partial class FinalCompletionReview : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            var account = Context.Items["Uims.Account"] as UimsAccount ?? throw new UnauthorizedAccessException();
            lblUserName.Text = HttpUtility.HtmlEncode(account.DisplayName);
            lblInitials.Text = HttpUtility.HtmlEncode(account.DisplayName.Substring(0,1));
        }
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            SessionService.Logout(SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
