using System;
using System.Web;
using System.Web.Security;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    public partial class Assign_Supervisors : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var identity = Context.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired)
            {
                Response.Redirect("~/Auth/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != "coordinator")
                throw new HttpException(403, "Coordinator access is required.");
            // Groups and selections are loaded and validated by AcademicAssignmentsApi.
        }
    }
}
