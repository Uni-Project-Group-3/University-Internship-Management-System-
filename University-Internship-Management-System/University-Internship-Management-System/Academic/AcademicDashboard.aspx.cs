using System;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.UI;
using University_Internship_Management_System.Services;

namespace UIMS.Academic
{
    public partial class AcademicDashboard : System.Web.UI.Page
    {
        protected string FirstRecordsHtml { get; private set; } = "";
        protected string SecondRecordsHtml { get; private set; } = "";
        protected string DashboardStatus { get; private set; } = "Loading live Firebase records.";
        protected string AccountSummary { get; private set; }
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            var account = Context.Items["Uims.Account"] as UimsAccount;
            if (account == null || account.Role != "academic" || account.MustChangePassword) throw new UnauthorizedAccessException();
            string name = account.DisplayName ?? account.Email;
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            lblUserName.Text = HttpUtility.HtmlEncode(name);
            lblInitials.Text = HttpUtility.HtmlEncode(string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0]))));
            lblDate.Text = JordanTime.Format(DateTime.UtcNow) + " " + JordanTime.Label;
            string greeting = JordanTime.LocalNow.Hour < 12 ? "Good morning" : JordanTime.LocalNow.Hour < 18 ? "Good afternoon" : "Good evening";
            lblGreeting.Text = HttpUtility.HtmlEncode(greeting + ", " + (parts.FirstOrDefault() ?? "User") + ".");
            AccountSummary = "Signed in as " + account.Email + " · " + "Academic program: " + (account.ProgramId ?? account.Department);
            lblStudents.Text = lblInTraining.Text = lblDelayed.Text = lblEvalDue.Text = "—";
            RegisterAsyncTask(new PageAsyncTask(() => LoadDataAsync(account)));
        }
        private async Task LoadDataAsync(UimsAccount account)
        {
            var database = Context.Items["Uims.Database"] as FirebaseRealtimeDatabaseRepository;
            if (database == null) throw new UnauthorizedAccessException();
            var data = await new RoleDashboardService(database).LoadAsync(account);
            lblStudents.Text = data.Counts[0]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            lblInTraining.Text = data.Counts[1]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            lblDelayed.Text = data.Counts[2]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            lblEvalDue.Text = data.Counts[3]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            FirstRecordsHtml = RoleDashboardService.Render(data.First, "No verified records to display, or read permission is unavailable.");
            SecondRecordsHtml = RoleDashboardService.Render(data.Second, "No verified records to display, or read permission is unavailable.");
            DashboardStatus = data.Status;
        }
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            SessionService.Logout(SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
