using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace University_Internship_Management_System.Shared
{
    // Shared base for the five pages:
    // 1) Reads the signed-in user from Firebase
    // 2) Injects the data into the page in place of demo-store.js
    // 3) Receives page saves (X-Action header) and returns the updated data
    public abstract class InternshipPage : Page
    {
        protected readonly FirebaseDb Db = new FirebaseDb();
        protected string Uid;          // Firebase uid of the signed-in user
        protected JObject Profile;     // users/{uid}
        string loadError;
        bool replied;

        protected abstract string Role { get; }              // "companySupervisor" or "student"
        protected abstract JObject BuildView();               // the page data
        protected virtual void Handle(string action, JObject data)
        {
            throw new AppError("This action is not supported.");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Uid = Context.User?.Identity?.IsAuthenticated == true ? Context.User.Identity.Name : null;
            try
            {
                if (!string.IsNullOrEmpty(Uid))
                {
                    var profile = Db.GetObject("users/" + FirebaseDb.Key(Uid));
                    if (profile != null && S(profile["role"]) == Role && (bool?)profile["isActive"] != false)
                        Profile = profile;
                }
            }
            catch (Exception error) { loadError = error.Message; }

            string action = Request.Headers["X-Action"];
            if (action == null) return;

            object result;
            try
            {
                if (Profile == null) throw new AppError(loadError ?? "Please sign in again.");
                string body = new StreamReader(Request.InputStream).ReadToEnd();
                Handle(action, string.IsNullOrWhiteSpace(body) ? new JObject() : (FirebaseDb.Parse(body) as JObject ?? new JObject()));
                result = BuildView();
            }
            catch (AppError error) { result = new { error = error.Message }; }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine("[" + action + "] " + error); // shows up in the Visual Studio Output window
                result = new { error = "Could not save to Firebase: " + error.Message };
            }

            Response.Clear();
            Response.ContentType = "application/json";
            Response.Write(JsonConvert.SerializeObject(result));
            replied = true;
            Context.ApplicationInstance.CompleteRequest();
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (replied) return; // the reply was JSON

            var html = new StringWriter();
            base.Render(new HtmlTextWriter(html));

            string data = JsonConvert.SerializeObject(PageView()).Replace("</", "<\\/");
            string script = "<script>" + PageScript.Replace("DATA", data) + "</script>";
            writer.Write(Regex.Replace(html.ToString(), @"<script[^>]*demo-store\.js[^>]*></script>", m => script));
        }

        JObject PageView()
        {
            if (Profile == null && loadError == null) return null; // not signed in → the page sends the user to Login
            try { if (Profile != null) return BuildView(); }
            catch (Exception error) { loadError = error.Message; }
            var user = new JObject { ["role"] = UimsData.JsRole(Role), ["name"] = Uid, ["mustChangePassword"] = false };
            var view = UimsData.View(user, new JObject[0], new JObject[0], new JObject[0], new JObject[0], new JObject[0]);
            view["connectionError"] = "Firebase data could not be loaded: " + loadError;
            return view;
        }

        protected static string S(JToken t) { return UimsData.S(t); }
        protected static int Int(JToken t) { return UimsData.Int(t); }

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
        var reply;
        try { reply = JSON.parse(request.responseText); }
        catch (e) { reply = { error: 'Server error (' + request.status + '). Check the Visual Studio Output window.' }; }
        if (reply.error) { alert(reply.error); throw new Error(reply.error); }
        data = reply;
    }
};";
    }

    // Converts Firebase records into the shape the page JavaScript expects
    public static class UimsData
    {
        public static string S(JToken t)
        {
            return t == null || t.Type == JTokenType.Null ? "" : t.ToString();
        }

        public static int Int(JToken t)
        {
            int n;
            return t != null && int.TryParse(t.ToString(), out n) ? n : 0;
        }

        static string Or(string a, string b) { return string.IsNullOrWhiteSpace(a) ? b : a; }

        // Jordan time (UTC+3) as yyyy-MM-dd
        public static string Today
        {
            get { return DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        }

        // The pages use data-demo-role="company" / "student"
        public static string JsRole(string role) { return role == "companySupervisor" ? "company" : role; }

        public static JObject User(string uid, JObject u)
        {
            string studentId = S(u["studentId"]);
            return new JObject
            {
                ["id"] = Or(studentId, uid),
                ["uid"] = uid,
                ["role"] = JsRole(S(u["role"])),
                ["name"] = S(u["fullName"]),
                ["email"] = S(u["email"]),
                ["phone"] = S(u["phone"]),
                ["department"] = Or(S(u["department"]), S(u["majorId"])),
                ["universityId"] = Or(studentId, S(u["universityId"])),
                ["companyId"] = S(u["companyId"]),
                ["mustChangePassword"] = false
            };
        }

        public static JObject Company(JObject c)
        {
            return new JObject { ["id"] = S(c["id"]), ["name"] = S(c["name"]) };
        }

        public static JObject Opportunity(JObject o)
        {
            var majors = o["majors"] as JArray ?? new JArray();
            return new JObject
            {
                ["id"] = S(o["id"]),
                ["companyId"] = S(o["companyId"]),
                ["companyName"] = S(o["companyName"]),
                ["title"] = S(o["title"]),
                ["description"] = S(o["description"]),
                ["requirements"] = S(o["requirements"]),
                ["department"] = S(majors.FirstOrDefault()),
                ["majors"] = majors,
                ["location"] = S(o["location"]),
                ["workMode"] = Or(S(o["workMode"]), "On-site"),
                ["seats"] = Int(o["seats"]),
                ["duration"] = Int(o["durationWeeks"]),
                ["requiredHours"] = Int(o["requiredHours"]),
                ["start"] = S(o["startDate"]),
                ["deadline"] = S(o["deadline"]),
                ["status"] = S(o["status"]) == "closed" ? "closed" : "open",
                ["acceptedCount"] = Int(o["acceptedCount"]),
                ["applicantsCount"] = Int(o["applicantsCount"]),
                ["createdAt"] = S(o["createdAt"])
            };
        }

        public static JObject Application(JObject a)
        {
            var copy = (JObject)a.DeepClone();
            foreach (var field in new[] { "status", "createdAt", "motivation", "phone", "cvLink", "portfolioLink", "reason" })
                if (copy[field] == null || copy[field].Type == JTokenType.Null) copy[field] = "";
            return copy;
        }

        // Is the opportunity open to a student of this major? (published + deadline not passed + major matches)
        public static bool IsOpenFor(JObject o, string major)
        {
            if (S(o["status"]) == "closed") return false;
            if (string.CompareOrdinal(S(o["deadline"]), Today) < 0) return false;
            var majors = o["majors"] as JArray;
            return majors == null || majors.Count == 0 || majors.Any(m => S(m) == major);
        }

        public static JObject View(JObject user, IEnumerable<JObject> users, IEnumerable<JObject> companies,
            IEnumerable<JObject> opportunities, IEnumerable<JObject> applications, IEnumerable<JObject> placements)
        {
            return new JObject
            {
                ["user"] = user,
                ["users"] = new JArray(users),
                ["companies"] = new JArray(companies),
                ["opportunities"] = new JArray(opportunities),
                ["applications"] = new JArray(applications),
                ["placements"] = new JArray(placements),
                ["tasks"] = new JArray(),
                ["submissions"] = new JArray(),
                ["progress"] = new JArray(),
                ["messages"] = new JArray()
            };
        }

        // ===== Company data: its opportunities + applications to it + the applicant students =====
        public static JObject CompanyView(FirebaseDb db, string uid, JObject profile)
        {
            string companyId = S(profile["companyId"]);
            if (companyId == "") throw new AppError("This account is not linked to a company.");

            var opportunities = db.List("opportunities").Where(o => S(o["companyId"]) == companyId).ToList();
            var applications = db.List("applications").Where(a => S(a["companyId"]) == companyId).ToList();
            var company = db.GetObject("companies/" + FirebaseDb.Key(companyId))
                          ?? new JObject { ["name"] = S(profile["companyName"]) };
            company["id"] = companyId;

            var allUsers = db.GetObject("users") ?? new JObject();
            var students = new List<JObject>();
            foreach (var a in applications)
            {
                string studentId = S(a["studentId"]);
                if (students.Any(s => S(s["id"]) == studentId)) continue;

                string key = S(a["studentUid"]);
                var u = key == "" ? null : allUsers[key] as JObject;
                if (u == null)
                {
                    var match = allUsers.Properties().FirstOrDefault(p => p.Value is JObject
                        && S(p.Value["studentId"]) == studentId && S(p.Value["role"]) == "student");
                    if (match != null) { key = match.Name; u = (JObject)match.Value; }
                }
                students.Add(u != null ? User(key, u) : new JObject
                {
                    ["id"] = studentId,
                    ["name"] = Or(S(a["studentName"]), studentId),
                    ["universityId"] = studentId,
                    ["department"] = "",
                    ["email"] = ""
                });
            }

            return View(User(uid, profile), students, new[] { Company(company) },
                opportunities.Select(Opportunity), applications.Select(Application), new JObject[0]);
        }

        // ===== Student data: open opportunities for their major + their applications + their placement =====
        public static JObject StudentView(FirebaseDb db, string uid, JObject profile)
        {
            string studentId = S(profile["studentId"]);
            string major = Or(S(profile["department"]), S(profile["majorId"]));

            var applications = db.List("applications").Where(a => S(a["studentId"]) == studentId).ToList();
            var applied = new HashSet<string>(applications.Select(a => S(a["opportunityId"])));
            var allOpportunities = db.List("opportunities");

            var opportunities = new List<JObject>();
            foreach (var o in allOpportunities)
            {
                bool open = IsOpenFor(o, major);
                if (!open && !applied.Contains(S(o["id"]))) continue;
                var item = Opportunity(o);
                if (!open) item["status"] = "closed"; // kept only to show the title in My Applications
                if (((JArray)item["majors"]).Any(m => S(m) == major)) item["department"] = major;
                opportunities.Add(item);
            }

            var companies = new Dictionary<string, JObject>();
            foreach (var c in db.List("companies")) companies[S(c["id"])] = Company(c);
            foreach (var o in allOpportunities)
            {
                string cid = S(o["companyId"]);
                if (cid != "" && !companies.ContainsKey(cid))
                    companies[cid] = new JObject { ["id"] = cid, ["name"] = S(o["companyName"]) };
            }

            var placements = db.List("placements").Where(p => S(p["studentId"]) == studentId);

            return View(User(uid, profile), new JObject[0], companies.Values, opportunities,
                applications.Select(Application), placements);
        }
    }
}