using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    // Coordinator: manage the Firebase dropdown lists. All reads and writes go through SystemOptionsApi.ashx.
    public partial class SystemOptions : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore(); Response.Cache.SetCacheability(HttpCacheability.NoCache);
            var identity = Context.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != "coordinator")
            {
                Response.Redirect(ResolveUrl("~/Auth/Login.aspx") + "?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e) { LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }
    }
}
