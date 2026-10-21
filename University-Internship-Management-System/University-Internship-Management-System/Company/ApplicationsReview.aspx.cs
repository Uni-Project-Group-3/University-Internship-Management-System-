using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace University_Internship_Management_System.Company
{
    // Company: review applications and accept or reject them (Firebase: /applications)
    public partial class ApplicationsReview : System.Web.UI.Page
    {
        private const string DatabaseUrl = "https://just-41ad1-default-rtdb.europe-west1.firebasedatabase.app";

        HttpClient client = new HttpClient();
        JObject db;       // the whole database
        JObject user;     // the signed-in user
        string userId;

        protected void Page_Load(object sender, EventArgs e)
        {
            db = Get("");
            userId = Session["UserId"] as string ?? "sup-orange";   // sample account until Login sets Session["UserId"]
            user = db["users"]?[userId] as JObject;
            if (user == null || (string)user["role"] != "company") user = null;

            // A save sent by the page JavaScript
            string action = Request.Headers["X-Action"];
            if (action == null) return;
            if (user == null) Reply(new { error = "Please sign in again." });

            JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
            if (action == "decision") Decide(data);

            db = Get("");          // read again so the page gets the new data
            Reply(LoadData());
        }

        // ================= Data for the page =================
        // This company's opportunities and the applications sent to them
        JObject LoadData()
        {
            if (user == null) return null;

            string companyId = (string)user["companyId"];
            var applications = List("applications").Where(a => (string)a["companyId"] == companyId).ToList();
            var students = List("users").Where(u => applications.Any(a => (string)a["studentId"] == (string)u["id"]));

            user["mustChangePassword"] = false;
            return new JObject
            {
                ["user"] = user,
                ["users"] = new JArray(students),
                ["companies"] = new JArray(List("companies")),
                ["opportunities"] = new JArray(List("opportunities").Where(o => (string)o["companyId"] == companyId)),
                ["applications"] = new JArray(applications),
                ["placements"] = new JArray(),
                ["tasks"] = new JArray(),
                ["submissions"] = new JArray(),
                ["progress"] = new JArray(),
                ["messages"] = new JArray()
            };
        }

        // Accepted + confirmed students on an opportunity
        int AcceptedCount(string opportunityId)
        {
            return List("applications").Count(a => (string)a["opportunityId"] == opportunityId &&
                ((string)a["status"] == "accepted" || (string)a["status"] == "confirmed"));
        }

        // ================= Accept / Reject =================
        void Decide(JObject data)
        {
            string id = (string)data["id"];
            string status = (string)data["status"];          // "accepted" or "rejected"
            JObject application = db["applications"]?[id] as JObject;

            if (application == null || (string)application["companyId"] != (string)user["companyId"])
            {
                Reply(new { error = "Application not found." });
            }

            if ((string)application["status"] != "pending")
            {
                Reply(new { error = "This application was already reviewed." });
            }

            if (status != "accepted" && status != "rejected")
            {
                Reply(new { error = "Choose accept or reject." });
            }

            if (status == "accepted")
            {
                string opportunityId = (string)application["opportunityId"];
                JObject opportunity = db["opportunities"]?[opportunityId] as JObject;
                if (opportunity == null || AcceptedCount(opportunityId) >= (int)opportunity["seats"])
                {
                    Reply(new { error = "All seats are filled for this opportunity." });
                }
            }

            Put("applications/" + id + "/status", status);
            Put("applications/" + id + "/reason", (string)data["reason"]);
            Put("applications/" + id + "/decidedAt", DateTime.UtcNow.ToString("o"));
        }

        // ================= Firebase =================
        JObject Get(string path)
        {
            string json = client.GetStringAsync(DatabaseUrl + "/" + path + ".json").Result;
            return json == "null" ? new JObject() : JObject.Parse(json);
        }

        void Put(string path, object data)
        {
            string body = JsonConvert.SerializeObject(data);
            client.PutAsync(DatabaseUrl + "/" + path + ".json", new StringContent(body, Encoding.UTF8, "application/json")).Wait();
        }

        // All records under a node, for example List("opportunities")
        IEnumerable<JObject> List(string node)
        {
            JObject records = db[node] as JObject;
            if (records == null) return new List<JObject>();
            return records.Properties().Select(p => (JObject)p.Value);
        }

        // Sends JSON back to the page JavaScript and stops the page here
        void Reply(object data)
        {
            Response.Clear();
            Response.ContentType = "application/json";
            Response.Write(JsonConvert.SerializeObject(data));
            Response.End();
        }

        // ================= Connect the page JavaScript =================
        // The page JavaScript reads its data from window.UimsDemo (it was demo-store.js).
        // Here we put a small script in its place that gives the data from Firebase
        // and sends every save back to this page.
        protected override void Render(HtmlTextWriter writer)
        {
            StringWriter html = new StringWriter();
            base.Render(new HtmlTextWriter(html));

            string data = JsonConvert.SerializeObject(LoadData()).Replace("</", "<\\/");
            string script = "<script>" + PageScript.Replace("DATA", data) + "</script>";

            writer.Write(Regex.Replace(html.ToString(), @"<script[^>]*demo-store\.js[^>]*></script>", m => script));
        }

        const string PageScript = @"
var data = DATA;
window.UimsDemo = {
    departments: ['Computer Science', 'Computer Engineering', 'Software Engineering', 'Computer Information Systems', 'Cybersecurity', 'Data Science & AI', 'Network Engineering & Security'],
    enabled: function () { return true; },
    view: function () { return data; },
    current: function () { return data && data.user; },
    logout: function () { data = null; },
    transaction: function (action, payload) {
        var request = new XMLHttpRequest();
        request.open('POST', location.href, false);
        request.setRequestHeader('X-Action', action);
        request.send(JSON.stringify(payload));
        var reply = JSON.parse(request.responseText);
        if (reply.error) throw new Error(reply.error);
        data = reply;
    }
};";
    }
}