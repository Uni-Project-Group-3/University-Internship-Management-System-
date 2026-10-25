using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace University_Internship_Management_System
{
    // Contact Us (public page): anyone can send a message to the internship coordinator.
    // Every message is saved in Firebase under /messages/{id} with status "unread".
    public partial class Contact : Page
    {
        static readonly HttpClient client = new HttpClient();

        // Read straight from Web.config, so the global FirebaseEnabled /
        // FirebaseLiveWritesEnabled switches (used by other features) do not block Contact Us
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
            // contact-demo.js sends a POST with the header X-Action: contact
            if (Request.HttpMethod != "POST" || Request.Headers["X-Action"] != "contact") return;

            object result;
            try
            {
                JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
                string error = SaveMessage(data);
                result = error == null ? (object)new { sent = true } : new { error };
            }
            catch (Exception ex)
            {
                // TEMP: show the real reason while testing (remove before going live)
                result = new { error = "The message could not be sent: " + ex.GetBaseException().Message };
            }

            Reply(result);
        }

        // ================= Save the message =================
        // Returns an error text, or null when the message was saved
        string SaveMessage(JObject data)
        {
            string name = Text(data["name"]);
            string email = Text(data["email"]);
            string subject = Text(data["subject"]);
            string message = Text(data["message"]);

            if (name == "" || email == "" || subject == "" || message == "")
                return "Please fill in all fields.";

            if (email.Length > 254 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Enter a valid email address.";

            if (name.Length > 100 || subject.Length > 150 || message.Length > 2000)
                return "The message is too long.";

            string id = "msg-" + Guid.NewGuid().ToString("N").Substring(0, 12);

            Put("messages/" + id, new JObject
            {
                ["id"] = id,
                ["name"] = name,
                ["email"] = email,
                ["subject"] = subject,
                ["message"] = message,
                ["status"] = "unread",
                ["reply"] = "",
                ["createdAt"] = DateTime.UtcNow.ToString("o")
            });
            UIMS.Services.Notify.Send(UIMS.Services.Notify.WithRole("coordinator"), "New contact message", name + ": " + subject, "Coordinator/ContactMessages.aspx", "message");

            return null;
        }

        static string Text(JToken value)
        {
            return ((string)value ?? "").Trim();
        }

        // ================= Firebase =================
        void Put(string path, object value)
        {
            var body = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            client.PutAsync(Url(path), body).Result.EnsureSuccessStatusCode();
        }

        // The visitor is not signed in, so the SERVER signs the request with the database secret
        // (Web.config: <add key="FirebaseSecret" value="..." />). Firebase rules do not block it.
        string Url(string path)
        {
            string url = DatabaseUrl + "/" + path + ".json";
            string secret = System.Configuration.ConfigurationManager.AppSettings["FirebaseSecret"];
            return string.IsNullOrEmpty(secret) ? url : url + "?auth=" + Uri.EscapeDataString(secret);
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
        // Replaces demo-store.js with a small window.UimsDemo that posts back to this page.
        // The page is public, so there is no data to load (null).
        protected override void Render(HtmlTextWriter writer)
        {
            StringWriter html = new StringWriter();
            base.Render(new HtmlTextWriter(html));

            string script = "<script>" + PageScript.Replace("DATA", "null") + "</script>";
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