using System;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using System.Threading;
using System.Web.Optimization;
using System.Web.Routing;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace University_Internship_Management_System
{
    public class Global : HttpApplication
    {
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
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
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.GetApplicationDefault()
                });
            }
        }
    }
}
