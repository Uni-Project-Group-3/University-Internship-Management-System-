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

namespace UIMS.Company
{
    // Company: add, edit and close internship opportunities (Firebase: /opportunities)
    public partial class ManageOpportunities : System.Web.UI.Page
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
            if (action == "opportunity") SaveOpportunity(data);
            if (action == "closeOpportunity") CloseOpportunity((string)data["id"]);

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

        // ================= Add / Edit =================
        void SaveOpportunity(JObject data)
        {
            string id = (string)data["id"];
            string companyId = (string)user["companyId"];
            string title = (string)data["title"];
            string description = (string)data["description"];
            string location = (string)data["location"];
            string deadline = (string)data["deadline"];
            string start = (string)data["start"];
            int seats, duration, hours;
            int.TryParse((string)data["seats"], out seats);
            int.TryParse((string)data["duration"], out duration);
            int.TryParse((string)data["requiredHours"], out hours);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(deadline) ||
                string.IsNullOrWhiteSpace(start) || seats < 1 || duration < 1 || hours < 1)
            {
                Reply(new { error = "Please fill in all fields." });
            }

            if (string.Compare(deadline, start) > 0)
            {
                Reply(new { error = "The application deadline must be on or before the start date." });
            }

            JObject opportunity;
            if (string.IsNullOrEmpty(id))
            {
                // New opportunity
                if (string.Compare(deadline, DateTime.Today.ToString("yyyy-MM-dd")) < 0)
                {
                    Reply(new { error = "The application deadline can't be in the past." });
                }

                id = "opp-" + Guid.NewGuid().ToString("N").Substring(0, 10);
                opportunity = new JObject();
                opportunity["id"] = id;
                opportunity["companyId"] = companyId;
                opportunity["status"] = "open";
                opportunity["createdAt"] = DateTime.UtcNow.ToString("o");
            }
            else
            {
                // Edit: only this company's own opportunity
                opportunity = db["opportunities"]?[id] as JObject;
                if (opportunity == null || (string)opportunity["companyId"] != companyId)
                {
                    Reply(new { error = "Opportunity not found." });
                }

                int accepted = AcceptedCount(id);
                if (seats < accepted)
                {
                    Reply(new { error = accepted + " students are already accepted — seats can't be lower than that." });
                }
            }

            opportunity["title"] = title;
            opportunity["description"] = description;
            opportunity["requirements"] = (string)data["requirements"];
            opportunity["department"] = (string)data["department"];
            opportunity["location"] = location;
            opportunity["workMode"] = (string)data["workMode"];
            opportunity["seats"] = seats;
            opportunity["duration"] = duration;
            opportunity["requiredHours"] = hours;
            opportunity["deadline"] = deadline;
            opportunity["start"] = start;

            Put("opportunities/" + id, opportunity);
        }

        // ================= Close =================
        void CloseOpportunity(string id)
        {
            JObject opportunity = db["opportunities"]?[id] as JObject;
            if (opportunity == null || (string)opportunity["companyId"] != (string)user["companyId"])
            {
                Reply(new { error = "Opportunity not found." });
            }

            Put("opportunities/" + id + "/status", "closed");
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