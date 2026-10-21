using System;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.UI;
using University_Internship_Management_System.Services;

namespace UIMS.Company
{
    public partial class CompanyDashboard : System.Web.UI.Page
    {
        protected string FirstRecordsHtml { get; private set; } = "";
        protected string SecondRecordsHtml { get; private set; } = "";
        protected string DashboardStatus { get; private set; } = "Loading live Firebase records.";
        protected string AccountSummary { get; private set; }
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            var account = Context.Items["Uims.Account"] as UimsAccount;
            if (account == null || account.Role != "company" || account.MustChangePassword) throw new UnauthorizedAccessException();
            string name = account.DisplayName ?? account.Email;
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            lblUserName.Text = HttpUtility.HtmlEncode(name);
            lblInitials.Text = HttpUtility.HtmlEncode(string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0]))));
            lblDate.Text = JordanTime.Format(DateTime.UtcNow) + " " + JordanTime.Label;
            string greeting = JordanTime.LocalNow.Hour < 12 ? "Good morning" : JordanTime.LocalNow.Hour < 18 ? "Good afternoon" : "Good evening";
            lblGreeting.Text = HttpUtility.HtmlEncode(greeting + ", " + (parts.FirstOrDefault() ?? "User") + ".");
            AccountSummary = "Signed in as " + account.Email + " · " + "Company: " + account.CompanyId;
            lblOpportunities.Text = lblApplications.Text = lblTrainees.Text = lblToReview.Text = "—";
            RegisterAsyncTask(new PageAsyncTask(() => LoadDataAsync(account)));
        }
        private async Task LoadDataAsync(UimsAccount account)
        {
            var database = Context.Items["Uims.Database"] as FirebaseRealtimeDatabaseRepository;
            if (database == null) throw new UnauthorizedAccessException();
            var data = await new RoleDashboardService(database).LoadAsync(account);
            lblOpportunities.Text = data.Counts[0]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            lblApplications.Text = data.Counts[1]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            lblTrainees.Text = data.Counts[2]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
            lblToReview.Text = data.Counts[3]?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—";
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
