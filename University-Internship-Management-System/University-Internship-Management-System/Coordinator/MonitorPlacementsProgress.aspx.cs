using System;
using System.IO;
using System.Web;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Coordinator
{
    public partial class MonitorPlacementsProgress : Page
    {
        private bool IsDataRequest => Request.QueryString["data"] == "1";

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (IsDataRequest)
            {
                LoadMockData();
            }
        }

        private void LoadMockData()
        {
            Response.ContentType = "application/json";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            try
            {
                string json = File.ReadAllText(Server.MapPath("~/App_Data/placements-progress.json"));
                Response.Write(JObject.Parse(json).ToString());
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is Newtonsoft.Json.JsonException)
            {
                Response.StatusCode = 503;
                Response.TrySkipIisCustomErrors = true;
                Response.Write("{\"error\":\"The demonstration dataset could not be loaded.\"}");
            }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (!IsDataRequest)
            {
                base.Render(writer);
            }
        }
    }
}
