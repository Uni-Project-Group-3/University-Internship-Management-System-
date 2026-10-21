using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using University_Internship_Management_System.Services;

namespace UIMS.Student
{
    public partial class StudentDashboard : Page
    {
        protected string RecentApplicationsHtml { get; private set; }
        protected string DeadlinesHtml { get; private set; }
        protected string HoursText { get; private set; } = "Approved-hours data unavailable";
        protected string TasksText { get; private set; } = "Task data unavailable";
        protected string TasksPercent { get; private set; } = "0";
        protected string HoursPercent { get; private set; } = "0";
        protected bool HasHoursProgress { get; private set; }
        protected string DataStatus { get; private set; } = "Loading verified student data.";

        protected void Page_Load(object sender, EventArgs e)
        {
            var account = Context.Items["Uims.Account"] as UimsAccount;
            if (account == null || account.Role != "student" || account.MustChangePassword) throw new UnauthorizedAccessException();
            string name = account.DisplayName ?? account.Email;
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            lblUserName.Text = HttpUtility.HtmlEncode(name);
            lblInitials.Text = HttpUtility.HtmlEncode(string.Concat(parts.Take(2).Select(part => char.ToUpper(part[0]))));
            lblGreeting.Text = HttpUtility.HtmlEncode((JordanTime.LocalNow.Hour < 12 ? "Good morning" : JordanTime.LocalNow.Hour < 18 ? "Good afternoon" : "Good evening") + ", " + parts.FirstOrDefault() + ".");
            lblDate.Text = JordanTime.Format(DateTime.UtcNow) + " Jordan Time (UTC+3)";
            lblApplied.Text = lblInReview.Text = lblTasks.Text = lblHours.Text = "—";
            RegisterAsyncTask(new PageAsyncTask(() => LoadDataAsync(account)));
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            SessionService.Logout(SessionService.Handle(Request));
            Response.Cache.SetNoStore();
            Response.Redirect("~/Auth/Login.aspx");
        }

        private async Task LoadDataAsync(UimsAccount account)
        {
            var database = Context.Items["Uims.Database"] as FirebaseRealtimeDatabaseRepository;
            if (database == null) throw new UnauthorizedAccessException();
            var data = await new StudentDashboardService(database).LoadAsync(account);
            lblApplied.Text = Count(data.ApplicationCount);
            lblInReview.Text = Count(data.ReviewCount);
            lblTasks.Text = Count(data.PendingTasks);
            lblHours.Text = data.ApprovedHours?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";
            RecentApplicationsHtml = data.ApplicationsState == DashboardReadState.Ready
                ? (data.Applications.Count == 0 ? Empty("No applications yet.") : string.Concat(data.Applications.Select(Row)))
                : Empty(StateMessage(data.ApplicationsState, "Applications"));
            DeadlinesHtml = data.TasksState == DashboardReadState.Ready
                ? (data.Deadlines.Count == 0 ? Empty("No pending task deadlines.") : string.Concat(data.Deadlines.Select(Row)))
                : Empty(StateMessage(data.TasksState, "Tasks"));
            if (data.PlacementState == DashboardReadState.Ready)
            {
                HoursText = data.ApprovedHours.Value.ToString("0.##", CultureInfo.InvariantCulture) + " / " + data.RequiredHours.Value.ToString("0.##", CultureInfo.InvariantCulture) + " h";
                HasHoursProgress = data.RequiredHours > 0;
                if (HasHoursProgress) HoursPercent = Math.Min(100m, data.ApprovedHours.Value * 100m / data.RequiredHours.Value).ToString("0.##", CultureInfo.InvariantCulture);
            }
            else HoursText = StateMessage(data.PlacementState, "Hours");
            if (data.TasksState == DashboardReadState.Ready || data.TasksState == DashboardReadState.NotPlaced)
            {
                TasksText = data.ReviewedTasks + " / " + data.TotalTasks;
                TasksPercent = (data.TotalTasks > 0 ? data.ReviewedTasks.Value * 100m / data.TotalTasks.Value : 0m).ToString("0.##", CultureInfo.InvariantCulture);
            }
            else TasksText = StateMessage(data.TasksState, "Tasks");
            DataStatus = new[] { data.ApplicationsState, data.PlacementState, data.TasksState }.All(state => state == DashboardReadState.Ready || state == DashboardReadState.NotPlaced)
                ? "Live Firebase data verified for your profile. Hours and task review do not imply final internship approval."
                : "Your account is verified. Some data sections need permissions or record verification; no demonstration values are displayed.";
        }
        private static string Count(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "—";
        private static string StateMessage(DashboardReadState state, string section)
        {
            switch (state)
            {
                case DashboardReadState.PermissionDenied: return section + ": student-only read permission is required.";
                case DashboardReadState.SchemaMismatch: return section + ": record relationships or fields need verification.";
                case DashboardReadState.NotPlaced: return "No current placement is recorded.";
                default: return section + ": Firebase could not be reached. Refresh to retry.";
            }
        }
        private static string Empty(string text) => "<div class='list-row'><p>" + HttpUtility.HtmlEncode(text) + "</p></div>";
        private static string Row(DashboardItem item)
        {
            string style = item.Status == "accepted" || item.Status == "reviewed" ? "success" : item.Status == "rejected" ? "danger" : "info";
            string status = (item.Status ?? "").Replace("_", " ");
            return "<div class='list-row'><div class='row-body'><div class='row-title'>" + HttpUtility.HtmlEncode(item.Title) +
                "</div><div class='row-sub'>" + HttpUtility.HtmlEncode(item.Subtitle) + "</div></div><div class='row-meta'><span class='status " + style + "'>" +
                HttpUtility.HtmlEncode(status) + "</span><div class='when'>" + HttpUtility.HtmlEncode(item.DateLabel) + "</div></div></div>";
        }
    }
}
