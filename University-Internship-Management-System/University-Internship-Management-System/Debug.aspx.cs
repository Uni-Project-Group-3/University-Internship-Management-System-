using System;
using System.Web.UI;

namespace University_Internship_Management_System
{
    public partial class DebugPages : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            if (!Context.IsDebuggingEnabled || !Request.IsLocal)
            {
                Response.StatusCode = 404;
                Response.TrySkipIisCustomErrors = true;
            }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (Context.IsDebuggingEnabled && Request.IsLocal)
            {
                base.Render(writer);
            }
        }
    }
}
