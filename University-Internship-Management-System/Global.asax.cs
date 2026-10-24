using System;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using System.Threading;
using System.Web.Optimization;
using System.Web.Routing;

namespace University_Internship_Management_System
{
    public class Global : HttpApplication
    {
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            // A tab may carry its own signed Forms Authentication ticket. Never trust a UID/role supplied by the browser.
            string tabTicket = Request.Headers["X-UIMS-Session"];
            if (string.IsNullOrWhiteSpace(tabTicket) && Request.HttpMethod == "POST") tabTicket = Request.Form["__UimsSession"];
            string folder = Request.AppRelativeCurrentExecutionFilePath.Split('/').Length > 1 ? Request.AppRelativeCurrentExecutionFilePath.Split('/')[1] : "";
            string routeRole = folder.Equals("Student", StringComparison.OrdinalIgnoreCase) ? "student" : folder.Equals("Company", StringComparison.OrdinalIgnoreCase) ? "companySupervisor" : folder.Equals("Academic", StringComparison.OrdinalIgnoreCase) ? "academicSupervisor" : folder.Equals("Coordinator", StringComparison.OrdinalIgnoreCase) ? "coordinator" : null;
            if (routeRole == null && (Request.AppRelativeCurrentExecutionFilePath.EndsWith("TasksApi.ashx", StringComparison.OrdinalIgnoreCase) || Request.AppRelativeCurrentExecutionFilePath.EndsWith("TaskExamsApi.ashx", StringComparison.OrdinalIgnoreCase)))
                routeRole = Shared.FirebaseSignInService.CanonicalRole(Request.Headers["X-UIMS-Role"]);
            bool explicitTicket = !string.IsNullOrWhiteSpace(tabTicket);
            if (!explicitTicket && routeRole != null) tabTicket = Request.Cookies[".UIMS.Role." + routeRole]?.Value;
            if (!explicitTicket && routeRole != null && string.IsNullOrWhiteSpace(tabTicket))
            {
                var currentIdentity = Context.User?.Identity as FormsIdentity;
                if (currentIdentity == null || Shared.FirebaseSignInService.CanonicalRole(currentIdentity.Ticket.UserData) != routeRole)
                { Context.User = new GenericPrincipal(new GenericIdentity(""), new string[0]); Thread.CurrentPrincipal = Context.User; return; }
            }
            if (!string.IsNullOrWhiteSpace(tabTicket))
            {
                FormsAuthenticationTicket ticket = null;
                try { ticket = FormsAuthentication.Decrypt(tabTicket); } catch { }
                string ticketRole = ticket == null ? null : Shared.FirebaseSignInService.CanonicalRole(ticket.UserData);
                if (ticket == null || ticket.Expired || ticketRole == null || (routeRole != null && routeRole != ticketRole))
                {
                    Context.User = new GenericPrincipal(new GenericIdentity(""), new string[0]);
                    Thread.CurrentPrincipal = Context.User;
                    return;
                }
                Context.User = new GenericPrincipal(new FormsIdentity(ticket), new[] { ticketRole });
            }
            var identity = Context.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired) return;
            string role = University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
            if (role == null) return;
            var principal = new GenericPrincipal(identity, new[] { role });
            Context.User = principal;
            Thread.CurrentPrincipal = principal;
        }
        protected void Application_Start(object sender, EventArgs e)
        {
            University_Internship_Management_System.Services.FinalEvaluation.LiveFirebaseIntegration.Register();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Services.FinalEvaluation.FinalEvaluationFirebaseIntegration.Register();

        }
    }
}
