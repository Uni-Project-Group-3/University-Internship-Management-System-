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
        JObject user;     // the signed-in user
        JObject pageData;
        string userId;

        protected void Page_Load(object sender, EventArgs e)
        {
            userId = Context.User?.Identity?.IsAuthenticated == true ? Context.User.Identity.Name : null;
            if (!string.IsNullOrWhiteSpace(userId)) { try { pageData = new UIMS.Services.ApplicationWorkflowService().LoadStudentAsync(userId).GetAwaiter().GetResult(); } catch (Exception error) { pageData = Unavailable(error.Message); } user = pageData["user"] as JObject; }

            // A save sent by the page JavaScript
            string action = Request.Headers["X-Action"];
            if (action == null) return;
            if (user == null) Reply(new { error = "Please sign in again." });

            try
            {
                JObject data = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
                if (action == "confirmOffer") new UIMS.Services.ApplicationWorkflowService().ConfirmOfferAsync(userId, (string)data["id"]).GetAwaiter().GetResult();
                else Reply(new { error = "This application action is not supported." });
                pageData = new UIMS.Services.ApplicationWorkflowService().LoadStudentAsync(userId).GetAwaiter().GetResult();
                Reply(LoadData());
            }
            catch (UIMS.Services.FirebaseException error) { Response.StatusCode = error.StatusCode; Reply(new { error = error.Message }); }
            catch (University_Internship_Management_System.Shared.FirebaseLoginException error) { Response.StatusCode = 403; Reply(new { error = error.Message }); }
            catch (Exception error) { Trace.Warn("MyApplications", error.GetType().Name); Response.StatusCode = 503; Reply(new { error = "The application request could not be completed. Check Firebase availability and retry." }); }
        }

        // ================= Data for the page =================
        // The student's applications + open opportunities for the student's specialization
        JObject LoadData()
        {
            return pageData;
        }

        private JObject Unavailable(string message)
        {
            return new JObject { ["user"] = new JObject { ["role"] = "student", ["fullName"] = Context.User.Identity.Name, ["mustChangePassword"] = false },
                ["users"] = new JArray(), ["companies"] = new JArray(), ["opportunities"] = new JArray(), ["applications"] = new JArray(),
                ["placements"] = new JArray(), ["tasks"] = new JArray(), ["submissions"] = new JArray(), ["progress"] = new JArray(), ["messages"] = new JArray(),
                ["connectionError"] = string.IsNullOrWhiteSpace(message) ? "Firebase data could not be loaded." : message };
        }

        // ================= Confirm offer =================
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
