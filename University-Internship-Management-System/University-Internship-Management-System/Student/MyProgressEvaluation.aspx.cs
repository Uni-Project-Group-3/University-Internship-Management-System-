using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Services;

namespace University_Internship_Management_System.Student
{
    public partial class MyProgressEvaluation : Page
    {
        protected string PlacementStatus { get; private set; } = "Placement status unavailable";
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            var account = Context.Items["Uims.Account"] as UimsAccount;
            if (account == null || account.Role != "student" || account.MustChangePassword) throw new UnauthorizedAccessException();
            NavName.Text = account.DisplayName ?? account.Email;
            ToggleExamDemo.Visible = ExamDemoNotice.Visible = ExamList.Visible = ExamDetails.Visible = false;
            NoExams.Visible = true;
            RegisterAsyncTask(new PageAsyncTask(() => LoadProgressAsync(account)));
        }
        protected void ToggleExamDemo_Click(object sender, EventArgs e) { NoExams.Visible = true; }
        protected void Exams_ItemCommand(object sender, RepeaterCommandEventArgs e) { ExamDetails.Visible = false; }
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            SessionService.Logout(SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
        private async Task LoadProgressAsync(UimsAccount account)
        {
            var database = Context.Items["Uims.Database"] as FirebaseRealtimeDatabaseRepository;
            if (database == null) throw new UnauthorizedAccessException();
            var source = await new StudentPortalService(database).LoadAsync(account, "progress");
            var progress = new StudentDashboardData { ApprovedHours = (decimal?)source["placement"]?["approvedHours"], RequiredHours = (decimal?)source["placement"]?["requiredHours"] };
            PlacementStatus = "Placement: " + ((string)source["placement"]?["status"] ?? "not recorded");
            ApprovedHours.Text = HoursApprovalStatus.Text = progress.ApprovedHours?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";
            RequiredHours.Text = progress.RequiredHours?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";
            RemainingHours.Text = progress.RequiredHours.HasValue && progress.ApprovedHours.HasValue ? Math.Max(0m, progress.RequiredHours.Value - progress.ApprovedHours.Value).ToString("0.##", CultureInfo.InvariantCulture) : "—";
            PendingHours.Text = EvaluatedExams.Text = "—";
            decimal percent = progress.RequiredHours > 0 && progress.ApprovedHours.HasValue ? Math.Min(100m, progress.ApprovedHours.Value * 100m / progress.RequiredHours.Value) : 0;
            HoursPercent.Text = progress.RequiredHours.HasValue && progress.ApprovedHours.HasValue ? percent.ToString("0.##", CultureInfo.InvariantCulture) : "—";
            HoursProgress.Attributes["value"] = percent.ToString("0.##", CultureInfo.InvariantCulture);
            var tasks = ((JArray)source["tasks"]).OfType<JObject>().ToArray();
            bool ready = (string)source["states"]?["tasks"] == "ready" || (string)source["states"]?["tasks"] == "not_placed";
            AssignedTasks.Text = ready ? tasks.Length.ToString(CultureInfo.InvariantCulture) : "—";
            SubmittedTasks.Text = ready ? tasks.Count(row => (string)row["status"] == "submitted").ToString(CultureInfo.InvariantCulture) : "—";
            EvaluatedTasks.Text = ready ? tasks.Count(row => (string)row["status"] == "reviewed").ToString(CultureInfo.InvariantCulture) : "—";
            RevisionTasks.Text = "—";
            Tasks.DataSource = tasks.Select(row => new { Title = (string)row["title"], Reference = (string)row["id"], StatusKey = (string)row["status"], Status = ((string)row["status"] ?? "Unknown").Replace("_", " "), Badge = (string)row["status"] == "reviewed" ? "success" : "warning", Feedback = "Private final evaluations and unpublished grades are not exposed.", NoteClass = "", NextStep = "Hours are approved separately.", History = "Due: " + (string)row["dueDate"] });
            Tasks.DataBind();
        }
    }
}
