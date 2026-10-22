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
    // Contact Us: anyone can send a message to the internship coordinator (Firebase: /messages)
    public partial class Contact : Page
    {
        private const string DatabaseUrl = "https://just-41ad1-default-rtdb.europe-west1.firebasedatabase.app";

        HttpClient client = new HttpClient();

        protected void Page_Load(object sender, EventArgs e)
        {
            // A message sent by the page JavaScript
            string action = Request.Headers["X-Action"];
            if (action != "contact") return;

            JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
            SendMessage(data);
            Reply(new { sent = true });
        }

        // The page is public, so there is no signed-in user and no data to load
        JObject LoadData()
        {
            return null;
        }

        // ================= Send message =================
        void SendMessage(JObject data)
        {
            string name = ((string)data["name"] ?? "").Trim();
            string email = ((string)data["email"] ?? "").Trim();
            string subject = ((string)data["subject"] ?? "").Trim();
            string message = ((string)data["message"] ?? "").Trim();

            if (name == "" || email == "" || subject == "" || message == "")
            {
                Reply(new { error = "Please fill in all fields." });
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                Reply(new { error = "Enter a valid email address." });
            }

            if (name.Length > 100 || subject.Length > 150 || message.Length > 2000)
            {
                Reply(new { error = "The message is too long." });
            }

            string id = "msg-" + Guid.NewGuid().ToString("N").Substring(0, 10);

            var newMessage = new
            {
                id = id,
                name = name,
                email = email,
                subject = subject,
                message = message,
                status = "unread",
                reply = "",
                createdAt = DateTime.UtcNow.ToString("o")
            };

            Put("messages/" + id, newMessage);
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