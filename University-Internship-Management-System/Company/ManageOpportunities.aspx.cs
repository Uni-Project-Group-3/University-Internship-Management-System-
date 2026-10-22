using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly University_Internship_Management_System.Shared.StudentFirebaseGateway firebase = new University_Internship_Management_System.Shared.StudentFirebaseGateway();
        JObject db;       // the whole database
        JObject user;     // the signed-in user
        string userId;

        protected void Page_Load(object sender, EventArgs e)
        {
            userId = Context.User?.Identity?.IsAuthenticated == true ? Context.User.Identity.Name : null;
            db = LoadCompanySnapshot(userId);
            user = db["users"]?[userId] as JObject;
            if (user == null) return;

            // A save sent by the page JavaScript
            string action = Request.Headers["X-Action"];
            if (action == null) return;
            if (user == null) Reply(new { error = "Please sign in again." });

            JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
            if (action == "opportunity") SaveOpportunity(data);
            if (action == "closeOpportunity") CloseOpportunity((string)data["id"]);

            db = LoadCompanySnapshot(userId); // read current Firebase records after the confirmed write
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

        JObject LoadCompanySnapshot(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid) || !firebase.IsActiveAuthenticationUidAsync(uid).GetAwaiter().GetResult())
                return new JObject();
            string key = University_Internship_Management_System.Shared.StudentManagementService.Key(uid);
            var profile = firebase.ReadAsync("users/" + key).GetAwaiter().GetResult() as JObject;
            var identity = University_Internship_Management_System.Shared.FirebaseSignInService.ProfileResult(uid, profile);
            if (identity.Role != "companySupervisor") return new JObject();
            string companyId = (string)profile["companyId"];
            if (string.IsNullOrWhiteSpace(companyId)) return new JObject();

            var snapshot = new JObject
            {
                ["users"] = new JObject { [uid] = profile },
                ["companies"] = new JObject(),
                ["applications"] = firebase.ReadEqualAsync("applications", "companyId", companyId).GetAwaiter().GetResult() as JObject ?? new JObject(),
                ["opportunities"] = firebase.ReadEqualAsync("opportunities", "companyId", companyId).GetAwaiter().GetResult() as JObject ?? new JObject(),
                ["placements"] = firebase.ReadEqualAsync("placements", "companyId", companyId).GetAwaiter().GetResult() as JObject ?? new JObject(),
                ["tasks"] = new JObject()
            };
            var company = firebase.ReadAsync("companies/" + University_Internship_Management_System.Shared.StudentManagementService.Key(companyId)).GetAwaiter().GetResult();
            if (company != null && company.Type == JTokenType.Object) snapshot["companies"][companyId] = company.DeepClone();
            foreach (var app in (snapshot["applications"] as JObject ?? new JObject()).Properties())
            {
                string studentId = (string)app.Value["studentId"];
                if (string.IsNullOrWhiteSpace(studentId)) continue;
                var student = firebase.ReadEqualAsync("users", "studentId", studentId).GetAwaiter().GetResult() as JObject;
                if (student != null) foreach (var item in student.Properties()) snapshot["users"][item.Name] = item.Value.DeepClone();
            }
            foreach (var placement in (snapshot["placements"] as JObject ?? new JObject()).Properties())
            {
                var tasks = firebase.ReadEqualAsync("tasks", "placementId", placement.Name).GetAwaiter().GetResult() as JObject;
                if (tasks != null) foreach (var item in tasks.Properties()) snapshot["tasks"][item.Name] = item.Value.DeepClone();
            }
            return snapshot;
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
            int seats = 0, duration = 0, hours = 0;
            DateTime start = DateTime.MinValue, deadline = DateTime.MinValue;
            if (!int.TryParse((string)data["seats"], out seats) ||
                !int.TryParse((string)data["duration"], out duration) ||
                !int.TryParse((string)data["requiredHours"], out hours) ||
                !DateTime.TryParseExact((string)data["start"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start) ||
                !DateTime.TryParseExact((string)data["deadline"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out deadline))
                Reply(new { error = "Enter valid dates, seats, duration and required training hours." });

            string major = ((string)data["department"] ?? "").Trim();
            var opportunity = new UIMS.Models.Opportunity
            {
                Id = id,
                Title = (string)data["title"],
                Description = (string)data["description"],
                Requirements = (string)data["requirements"],
                Majors = string.IsNullOrWhiteSpace(major) ? new List<string>() : new List<string> { major },
                Location = (string)data["location"],
                WorkMode = (string)data["workMode"],
                Seats = seats,
                DurationWeeks = duration,
                RequiredHours = hours,
                StartDate = start,
                Deadline = deadline
            };
            var service = new UIMS.Services.OpportunityService();
            if (string.IsNullOrWhiteSpace(id)) service.CreateAsync(userId, opportunity).GetAwaiter().GetResult();
            else service.UpdateAsync(userId, opportunity).GetAwaiter().GetResult();
        }

        // ================= Close =================
        void CloseOpportunity(string id)
        {
            new UIMS.Services.OpportunityService().CloseAsync(userId, id).GetAwaiter().GetResult();
        }

        // ================= Firebase =================
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
