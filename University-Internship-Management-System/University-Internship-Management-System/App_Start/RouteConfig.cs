using System.Web.Routing;
using Microsoft.AspNet.FriendlyUrls;

namespace University_Internship_Management_System
{
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            var settings = new FriendlyUrlSettings
            {
                // Keep both ASPX and friendly routes, but never redirect away a POST carrying the tab ticket.
                AutoRedirectMode = RedirectMode.Off
            };
            routes.EnableFriendlyUrls(settings);
        }
    }
}
