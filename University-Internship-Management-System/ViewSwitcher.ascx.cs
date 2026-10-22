using System;
using System.Web;
using System.Web.Routing;
using System.Web.UI;
using Microsoft.AspNet.FriendlyUrls.Resolvers;

namespace University_Internship_Management_System
{
    public partial class ViewSwitcher : UserControl
    {
        protected string CurrentView { get; private set; }
        protected string AlternateView { get; private set; }
        protected string SwitchUrl { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            const string switchRoute = "AspNet.FriendlyUrls.SwitchView";
            if (RouteTable.Routes[switchRoute] == null)
            {
                Visible = false;
                return;
            }

            bool isMobile = WebFormsFriendlyUrlResolver.IsMobileView(new HttpContextWrapper(Context));
            CurrentView = isMobile ? "Mobile" : "Desktop";
            AlternateView = isMobile ? "Desktop" : "Mobile";
            SwitchUrl = GetRouteUrl(switchRoute, new
            {
                view = AlternateView,
                __FriendlyUrls_SwitchViews = true
            }) + "?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl);
        }
    }
}
