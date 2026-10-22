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

namespace University_Internship_Management_System.Coordinator
{
    // Coordinator: read the Contact Us messages, mark them as read and reply (Firebase: /messages)
    public partial class ContactMessages : Page
    {
        private const string DatabaseUrl = "https://just-41ad1-default-rtdb.europe-west1.firebasedatabase.app";

        HttpClient client = new HttpClient();
        JObject db;       // the whole database
        JObject user;     // the signed-in coordinator
        string userId;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();

            db = Get("");
            userId = Session["UserId"] as string ?? "coord-1";   // sample account until Login sets Session["UserId"]
            user = db["users"]?[userId] as JObject;
            if (user == null || (string)user["role"] != "admin") user = null;

            // An action sent by the page JavaScript
            string action = Request.Headers["X-Action"];
            if (action == null) return;
            if (user == null) Reply(new { error = "Please sign in again." });

            JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
            if (action == "readMessage") MarkAsRead((string)data["id"]);
            if (action == "replyMessage") SendReply((string)data["id"], (string)data["reply"]);

            db = Get("");          // read again so the page gets the new data
            Reply(LoadData());
        }

        // ================= Data for the page =================
        JObject LoadData()
        {
            if (user == null) return null;

            user["mustChangePassword"] = false;
            return new JObject
            {
                ["user"] = user,
                ["messages"] = new JArray(List("messages").OrderByDescending(m => (string)m["createdAt"])),
                ["users"] = new JArray(),
                ["companies"] = new JArray(),
                ["opportunities"] = new JArray(),
                ["applications"] = new JArray(),
                ["placements"] = new JArray(),
                ["tasks"] = new JArray(),
                ["submissions"] = new JArray(),
                ["progress"] = new JArray(),
                ["students"] = new JArray(),
                ["groups"] = new JArray(),
                ["evaluations"] = new JArray()
            };
        }

        // ================= Mark as read =================
        void MarkAsRead(string id)
        {
            JObject message = db["messages"]?[id] as JObject;
            if (message == null)
            {
                Reply(new { error = "Message not found." });
            }

            // A replied message keeps its "replied" status
            if ((string)message["status"] == "unread")
            {
                Put("messages/" + id + "/status", "read");
            }
        }

        // ================= Reply =================
        void SendReply(string id, string reply)
        {
            JObject message = db["messages"]?[id] as JObject;
            if (message == null)
            {
                Reply(new { error = "Message not found." });
            }

            reply = (reply ?? "").Trim();
            if (reply == "")
            {
                Reply(new { error = "Write a reply first." });
            }

            if (reply.Length > 2000)
            {
                Reply(new { error = "The reply must be 2,000 characters or fewer." });
            }

            Put("messages/" + id + "/reply", reply);
            Put("messages/" + id + "/status", "replied");
            Put("messages/" + id + "/repliedAt", DateTime.UtcNow.ToString("o"));
            Put("messages/" + id + "/repliedBy", userId);
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
        if (reply && reply.error) throw new Error(reply.error);
        data = reply;
    }
};";
    }
}