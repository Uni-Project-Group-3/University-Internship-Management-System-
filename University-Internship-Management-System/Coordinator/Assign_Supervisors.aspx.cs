using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using University_Internship_Management_System.Shared;
namespace University_Internship_Management_System.Coordinator {
public partial class Assign_Supervisors : Page {
protected void Page_Load(object sender, EventArgs e) { Response.Cache.SetNoStore(); var identity=Context.User?.Identity as FormsIdentity; if(identity==null||!identity.IsAuthenticated||identity.Ticket.Expired||FirebaseSignInService.CanonicalRole(identity.Ticket.UserData)!="coordinator") throw new HttpException(403,"Sign in with your coordinator account."); }
protected void btnLogout_Click(object sender,EventArgs e){FormsAuthentication.SignOut();Session.Clear();Response.Redirect("~/Auth/Login.aspx",false);}
} }
