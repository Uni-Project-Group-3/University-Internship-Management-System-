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

namespace University_Internship_Management_System.Student
{
    // Student: follow applications and confirm one accepted offer
    public partial class MyApplications : System.Web.UI.Page
    {
        private const string DatabaseUrl = "https://just-41ad1-default-rtdb.europe-west1.firebasedatabase.app";

        HttpClient client = new HttpClient();
        JObject db;       // the whole database
        JObject user;     // the signed-in user
        string userId;

        protected void Page_Load(object sender, EventArgs e)
        {
            db = Get("");
            userId = Session["UserId"] as string ?? "stu-sara";   // sample account until Login sets Session["UserId"]
            user = db["users"]?[userId] as JObject;
            if (user == null || (string)user["role"] != "student") user = null;

            // A save sent by the page JavaScript
            string action = Request.Headers["X-Action"];
            if (action == null) return;
            if (user == null) Reply(new { error = "Please sign in again." });

            JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
            if (action == "confirmOffer") ConfirmOffer((string)data["id"]);

            db = Get("");          // read again so the page gets the new data
            Reply(LoadData());
        }

        // ================= Data for the page =================
        // The student's applications + open opportunities for the student's specialization
        JObject LoadData()
        {
            if (user == null) return null;

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            var myApplications = List("applications").Where(a => (string)a["studentId"] == userId).ToList();
            var opportunities = List("opportunities").Where(o =>
                ((string)o["status"] == "open" && (string)o["department"] == (string)user["department"] && string.Compare((string)o["deadline"], today) >= 0)
                || myApplications.Any(a => (string)a["opportunityId"] == (string)o["id"]));

            user["mustChangePassword"] = false;
            return new JObject
            {
                ["user"] = user,
                ["users"] = new JArray(user),
                ["companies"] = new JArray(List("companies")),
                ["opportunities"] = new JArray(opportunities),
                ["applications"] = new JArray(myApplications),
                ["placements"] = new JArray(myApplications.Where(a => (string)a["status"] == "confirmed")),
                ["tasks"] = new JArray(),
                ["submissions"] = new JArray(),
                ["progress"] = new JArray(),
                ["messages"] = new JArray()
            };
        }

        // ================= Confirm offer =================
        void ConfirmOffer(string id)
        {
            JObject application = db["applications"]?[id] as JObject;

            if (application == null || (string)application["studentId"] != userId || (string)application["status"] != "accepted")
            {
                Reply(new { error = "Only an accepted offer can be confirmed." });
            }

            if (List("applications").Any(a => (string)a["studentId"] == userId && (string)a["status"] == "confirmed"))
            {
                Reply(new { error = "You already confirmed an internship." });
            }

            Put("applications/" + id + "/status", "confirmed");
            Put("applications/" + id + "/confirmedAt", DateTime.UtcNow.ToString("o"));

            // The other accepted offers give their seat back to the company
            foreach (JObject other in List("applications").Where(a => (string)a["studentId"] == userId &&
                                                                 (string)a["status"] == "accepted" && (string)a["id"] != id))
            {
                Put("applications/" + (string)other["id"] + "/status", "withdrawn");
            }
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