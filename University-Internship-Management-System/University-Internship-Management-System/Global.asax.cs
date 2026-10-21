using System;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Threading.Tasks;
using University_Internship_Management_System.Services;

namespace University_Internship_Management_System
{
    public class Global : HttpApplication
    {
        public override void Init()
        {
            base.Init();
            var helper = new EventHandlerTaskAsyncHelper(AuthorizePortalAsync);
            AddOnPostAcquireRequestStateAsync(helper.BeginEventHandler, helper.EndEventHandler);
        }

        private async Task AuthorizePortalAsync(object sender, EventArgs args)
        {
            var context = ((HttpApplication)sender).Context;
            string route = context.Request.AppRelativeCurrentExecutionFilePath.TrimStart('~', '/');
            string folder = route.Split('/')[0].ToLowerInvariant();
            bool portal = folder == "student" || folder == "company" || folder == "academic" || folder == "coordinator";
            if (route.Equals("Debug.aspx", StringComparison.OrdinalIgnoreCase) || route.Equals("Debug", StringComparison.OrdinalIgnoreCase) || route.StartsWith("Public/test", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 404;
                context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (!portal) return;
            context.Response.Cache.SetNoStore();
            try
            {
                if (!context.Request.IsSecureConnection) throw new UnauthorizedAccessException();
                if (context.Request.HttpMethod != "POST" || !Auth.Gateway.IsSameOrigin(context.Request)) throw new UnauthorizedAccessException();
                var session = SessionService.Get(SessionService.Handle(context.Request));
                var identity = await SessionService.VerifyAsync(SessionService.Handle(context.Request));
                var account = await new UserAccountService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).GetActiveAsync(identity);
                SessionService.RequireCurrent(session, account);
                if (account.MustChangePassword) LoginDiagnostics.Write("FirstLoginRestriction");
                if (session.Uid != identity.Uid || account.MustChangePassword || !RoleAuthorizationService.AllowsPath(account, route))
                    throw new UnauthorizedAccessException();
                // Student routes have explicit live adapters; unknown/other-role pages stay gated.
                string studentPage = route.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ? route.Substring(0, route.Length - 5) : route;
                if (account.Role == "student" && Array.Exists(new[] { "Student/StudentDashboard", "Student/ExploreOpportunities", "Student/ApplicationForm", "Student/MyApplications", "Student/Profile", "Student/MyTasks", "Student/MyProgressEvaluation" }, page => page.Equals(studentPage, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Items["Uims.Account"] = account;
                    context.Items["Uims.Database"] = new FirebaseRealtimeDatabaseRepository(session.IdToken);
                    return;
                }
                // Account-only dashboards render the verified RTDB profile, not local fixtures.
                // Business sections stay unavailable until their own scoped live adapters exist.
                if (RoleAuthorizationService.IsIdentityDashboard(account, route) || RolePortalService.Supports(account, route))
                {
                    context.Items["Uims.Account"] = account;
                    context.Items["Uims.Database"] = new FirebaseRealtimeDatabaseRepository(session.IdToken);
                    LoginDiagnostics.Write("RoleDashboardIdentityAuthorized");
                    return;
                }
                // Do not render legacy local fixtures as if they were live Firebase records.
                LoginDiagnostics.Write("DashboardReadiness503", 503);
                context.Response.StatusCode = 503;
                context.Response.Write("This page is not ready for live data yet. Authentication and role checks passed; no demonstration records are being displayed.");
            }
            catch (UnauthorizedAccessException)
            {
                LoginDiagnostics.Write("TabTicketAuthorizationFailure", 401);
                context.Response.StatusCode = 401;
                context.Response.RedirectLocation = VirtualPathUtility.ToAbsolute("~/Auth/Login.aspx");
                context.Response.Write("Please sign in to access this portal.");
            }
            catch (Exception)
            {
                context.Response.StatusCode = 503;
                context.Response.Write("Identity and profile verification are temporarily unavailable. No local fallback is enabled.");
            }
            context.Response.TrySkipIisCustomErrors = true;
            context.ApplicationInstance.CompleteRequest();
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
