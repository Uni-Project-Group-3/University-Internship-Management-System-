using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using University_Internship_Management_System.Shared;
namespace University_Internship_Management_System.Coordinator {
public partial class Assign_Supervisors : Page {
protected void Page_Load(object sender, EventArgs e)
{
    Response.Cache.SetNoStore();
    var identity = Context.User?.Identity as FormsIdentity;
    bool authenticated = identity != null && identity.IsAuthenticated && !identity.Ticket.Expired;
    string role = authenticated ? FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) : null;
    if (role == "coordinator") return;
    string destination = role == "student" ? "~/Student/StudentDashboard.aspx" :
        role == "companySupervisor" ? "~/Company/CompanyDashboard.aspx" :
        role == "academicSupervisor" ? "~/Academic/AcademicDashboard.aspx" : "~/Auth/Login.aspx?account=new";
    Response.Redirect(destination, false);
    Response.SuppressContent = true;
    Context.ApplicationInstance.CompleteRequest();
}
protected void btnLogout_Click(object sender,EventArgs e) { University_Internship_Management_System.Shared.LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }
} }
