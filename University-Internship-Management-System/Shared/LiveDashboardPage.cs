using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Services.FinalEvaluation;

namespace University_Internship_Management_System.Shared
{
    public abstract class LiveDashboardPage : Page
    {
        protected abstract string DashboardRole { get; }
        public string DashboardContent { get; private set; } = "";
        protected override void OnLoad(EventArgs e) { base.OnLoad(e); Response.Cache.SetNoStore(); RegisterAsyncTask(new PageAsyncTask(LoadAsync)); }
        private Control Find(Control parent, string id) { if (parent.ID == id) return parent; foreach (Control child in parent.Controls) { var match = Find(child, id); if (match != null) return match; } return null; }
        private void Set(string id, object value) { var label = Find(this, id) as Label; if (label != null) label.Text = HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture)); }
        private static string T(JToken t, string field) { return LiveFirebaseData.Text(t, field); }
        private static bool Done(JToken t) { return new[] { "completed", "evaluated" }.Contains(T(t, "status").ToLowerInvariant()); }
        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var data = await LiveFirebaseData.OpenAsync(Context);
                if (data.Role != DashboardRole) throw new UnauthorizedAccessException();
                string name = T(data.Profile, "fullName"); var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Set("lblUserName", name); Set("lblInitials", string.Concat(parts.Take(2).Select(p => p[0])));
                var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)); Set("lblDate", now.ToString("ddd, MMM dd, hh:mm tt"));
                Set("lblGreeting", (now.Hour < 12 ? "Good morning" : now.Hour < 18 ? "Good afternoon" : "Good evening") + ", " + parts.FirstOrDefault());
                var placements = data.Placements(); var ids = placements.Select(p => p.InternshipId).ToArray(); var studentIds = placements.Select(p => p.StudentId).ToArray();
                var tasks = data.Records("tasks").Where(t => ids.Contains(T(t, "placementId"))).ToArray();
                var applications = data.Records("applications").Where(a => DashboardRole == "coordinator" || DashboardRole == "student" && (T(a, "studentUid") == data.UserId || T(a, "studentId") == T(data.Profile, "studentId")) || DashboardRole == "companySupervisor" && T(a, "companyId") == T(data.Profile, "companyId")).ToArray();
                var opportunities = data.Records("opportunities").Where(o => DashboardRole == "coordinator" || T(o, "companyId") == T(data.Profile, "companyId")).ToArray();
                decimal hours = 0;
                foreach (string id in ids) { var ledger = await ((IApprovedTrainingHoursReader)data).GetAsync(id); hours += ledger.Value.Where(h => h.IsApproved).Sum(h => h.Hours); }
                if (DashboardRole == "student") { Set("lblApplied", applications.Length); Set("lblInReview", applications.Count(a => new[] { "pending", "submitted", "inreview", "underreview" }.Contains(T(a, "status").ToLowerInvariant()))); Set("lblTasks", tasks.Count(t => !Done(t))); Set("lblHours", hours.ToString("0.#")); }
                if (DashboardRole == "companySupervisor") { Set("lblOpportunities", opportunities.Length); Set("lblApplications", applications.Length); Set("lblTrainees", placements.Length); Set("lblToReview", tasks.Count(t => T(t, "status").Equals("submitted", StringComparison.OrdinalIgnoreCase))); }
                int due = 0;
                foreach (string id in ids) { var eval = (await data.GetPercentageAsync(id)).Value; if (eval?.Current?.SubmittedAtUtc != null && PercentageEvaluationWorkflow.CurrentReview(eval) == null) due++; }
                if (DashboardRole == "academicSupervisor") { Set("lblStudents", studentIds.Distinct().Count()); Set("lblInTraining", placements.Count(p => T(data.Records("placements").FirstOrDefault(x => T(x, "id") == p.InternshipId), "status") == "active")); Set("lblDelayed", placements.Count(p => { DateTime end; var raw = data.Records("placements").FirstOrDefault(x => T(x, "id") == p.InternshipId); return DateTime.TryParse(T(raw, "endDate"), out end) && end.Date < now.Date && T(raw, "completionStatus") != "Completed"; })); Set("lblEvalDue", due); }
                if (DashboardRole == "coordinator") { Set("lblStudents", data.Records("users").Count(u => T(u, "role") == "student")); Set("lblCompanies", data.Records("companies").Count()); Set("lblOpportunities", opportunities.Length); Set("lblAwaiting", data.Records("completionReviews").Count(r => studentIds.Contains(T(r, "studentId")) && T(r, "status") == "Pending")); }
                var html = new StringBuilder("<section class='panel'><div class='panel-head'><h3>Live internship records</h3></div>");
                foreach (var p in placements.Take(8)) html.Append("<div class='list-row'><div class='row-body'><div class='row-title'>").Append(HttpUtility.HtmlEncode(p.DisplayName ?? p.StudentId)).Append("</div><div class='row-sub'>").Append(HttpUtility.HtmlEncode(p.InternshipId)).Append("</div></div><span class='status info'>").Append(HttpUtility.HtmlEncode((await data.GetCompletionReviewAsync(p.InternshipId))?.Status.ToString() ?? "In training")).Append("</span></div>");
                if (placements.Length == 0) html.Append("<p>No internship placements are currently assigned.</p>");
                if (DashboardRole == "student" || DashboardRole == "companySupervisor") { html.Append("<div class='panel-head'><h3>Recent applications</h3></div>"); foreach (var a in applications.OrderByDescending(a => T(a, "createdAt")).Take(5)) html.Append("<div class='list-row'><div class='row-body'>").Append(HttpUtility.HtmlEncode(T(a, "studentName") + " · " + T(a, "companyName"))).Append("</div><span class='status info'>").Append(HttpUtility.HtmlEncode(T(a, "status"))).Append("</span></div>"); if (applications.Length == 0) html.Append("<p>No applications yet.</p>"); }
                DashboardContent = html.Append("</section>").ToString();
            }
            catch (UnauthorizedAccessException) { Response.Redirect(ResolveUrl("~/Auth/Login.aspx"), false); Context.ApplicationInstance.CompleteRequest(); }
            catch (Exception error) { Trace.Warn("Dashboard", error.GetType().Name); foreach (string id in new[] { "lblApplied", "lblInReview", "lblTasks", "lblHours", "lblOpportunities", "lblApplications", "lblTrainees", "lblToReview", "lblStudents", "lblInTraining", "lblDelayed", "lblEvalDue", "lblCompanies", "lblAwaiting" }) Set(id, "—"); DashboardContent = "<div role='alert' class='alert alert-danger'>Firebase dashboard data could not be loaded. Refresh or sign in again.</div>"; }
        }
        protected void btnLogout_Click(object sender, EventArgs e) { System.Web.Security.FormsAuthentication.SignOut(); Session.Clear(); Session.Abandon(); Response.Redirect("~/Auth/Login.aspx", false); Context.ApplicationInstance.CompleteRequest(); }
    }
}
