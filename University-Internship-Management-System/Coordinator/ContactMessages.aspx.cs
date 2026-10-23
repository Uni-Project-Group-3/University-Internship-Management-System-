using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    // Coordinator: reads the Contact Us messages from Firebase (/messages) and marks them as read.
    public partial class ContactMessages : Page
    {
        static readonly HttpClient client = new HttpClient();

        string userId;   // Firebase key of the signed-in coordinator
        JObject user;    // the coordinator record from /users (null = not allowed)

        // Read straight from Web.config, so the global FirebaseEnabled /
        // FirebaseLiveWritesEnabled switches (used by other features) do not block this page
        static string DatabaseUrl
        {
            get
            {
                string url = System.Configuration.ConfigurationManager.AppSettings["FirebaseDatabaseUrl"]
                          ?? System.Configuration.ConfigurationManager.AppSettings["FirebaseUrl"];
                if (string.IsNullOrEmpty(url)) throw new InvalidOperationException("FirebaseDatabaseUrl is missing in Web.config.");
                return url.TrimEnd('/');
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            FindCoordinator();

            // contact-messages.js sends a POST with the header X-Action
            string action = Request.Headers["X-Action"];
            if (Request.HttpMethod != "POST" || action == null) return;

            object result;
            try
            {
                result = RunAction(action);
            }
            catch (Exception)
            {
                result = new { error = "Could not update the message. Please try again." };
            }

            Reply(result);
        }

        // ================= Signed-in coordinator =================
        void FindCoordinator()
        {
            JObject users = Get("users");
            userId = Session["UserId"] as string;

            // Until Login sets Session["UserId"], use the first coordinator account (demo only)
            if (string.IsNullOrEmpty(userId))
            {
                userId = users.Properties()
                    .Where(p => IsCoordinator(p.Value as JObject))
                    .Select(p => p.Name)
                    .FirstOrDefault();
            }

            JObject found = userId != null ? users[userId] as JObject : null;
            user = IsCoordinator(found) ? found : null;
        }

        static bool IsCoordinator(JObject account)
        {
            return account != null
                && FirebaseSignInService.CanonicalRole((string)account["role"]) == "coordinator"
                && (bool?)account["isActive"] != false;
        }

        // ================= Actions from the page =================
        object RunAction(string action)
        {
            if (user == null) return new { error = "Please sign in again." };

            JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
            string id = (string)data["id"];

            string error;
            if (action == "readMessage") error = MarkAsRead(id);
            else if (action == "replyMessage") error = SendReply(id, (string)data["reply"]);
            else error = "Unknown action.";

            // On success the page gets the fresh data and redraws the table
            return error != null ? (object)new { error } : LoadData();
        }

        string MarkAsRead(string id)
        {
            JObject message = FindMessage(id);
            if (message == null) return "Message not found.";

            // A replied message keeps its "replied" status
            if ((string)message["status"] == "unread")
                Write("PUT", "messages/" + id + "/status", "read");

            return null;
        }

        // Ready on the server; the current page has no reply box yet
        string SendReply(string id, string reply)
        {
            JObject message = FindMessage(id);
            if (message == null) return "Message not found.";

            reply = (reply ?? "").Trim();
            if (reply == "") return "Write a reply first.";
            if (reply.Length > 2000) return "The reply must be 2,000 characters or fewer.";

            Write("PATCH", "messages/" + id, new JObject
            {
                ["reply"] = reply,
                ["status"] = "replied",
                ["repliedAt"] = DateTime.UtcNow.ToString("o"),
                ["repliedBy"] = userId
            });
            return null;
        }

        JObject FindMessage(string id)
        {
            // Only accept ids made by Contact.aspx (stops paths like "../users")
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^msg-[A-Za-z0-9]+$")) return null;

            JObject message = Get("messages/" + id);
            return message.HasValues ? message : null;
        }

        // ================= Data for the page =================
        JObject LoadData()
        {
            if (user == null) return null;

            var messages = Get("messages").Properties()
                .Where(p => p.Value is JObject)
                .Select(p => Normalize(p.Name, (JObject)p.Value))
                .OrderByDescending(m => (string)m["createdAt"]);

            return new JObject
            {
                ["user"] = new JObject
                {
                    ["id"] = userId,
                    ["name"] = (string)user["fullName"] ?? "Coordinator",
                    ["email"] = (string)user["email"] ?? "",
                    ["role"] = "admin",              // the page expects data-demo-role="admin"
                    ["mustChangePassword"] = false
                },
                ["messages"] = new JArray(messages),
                // Empty lists the shared page script reads
                ["progress"] = new JArray(),
                ["applications"] = new JArray(),
                ["placements"] = new JArray(),
                ["tasks"] = new JArray(),
                ["submissions"] = new JArray()
            };
        }

        // Makes sure every message has the fields the page uses
        static JObject Normalize(string key, JObject message)
        {
            message["id"] = key;
            if (message["status"] == null) message["status"] = "unread";
            if (message["createdAt"] == null) message["createdAt"] = "";
            return message;
        }

        // ================= Firebase =================
        JObject Get(string path)
        {
            string json = client.GetStringAsync(Url(path)).Result;
            return json == "null" ? new JObject() : JObject.Parse(json);
        }

        // The server signs every request with the database secret (Web.config: FirebaseSecret)
        string Url(string path)
        {
            string url = DatabaseUrl + "/" + path + ".json";
            string secret = System.Configuration.ConfigurationManager.AppSettings["FirebaseSecret"];
            return string.IsNullOrEmpty(secret) ? url : url + "?auth=" + Uri.EscapeDataString(secret);
        }

        // PUT replaces a value, PATCH updates only the given fields
        void Write(string method, string path, object value)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), Url(path))
            {
                Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json")
            };
            client.SendAsync(request).Result.EnsureSuccessStatusCode();
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
        // Replaces demo-store.js with a small window.UimsDemo that holds the Firebase data
        // and sends every action back to this page.
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