using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using University_Internship_Management_System.Services.FinalEvaluation;

namespace University_Internship_Management_System.Company
{
    public partial class MyTrainees : Page
    {
        public string TraineeContent { get; private set; } = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            RegisterAsyncTask(new PageAsyncTask(LoadAsync));
        }
        private static string H(string text) { return HttpUtility.HtmlEncode(text ?? ""); }
        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var data = await LiveFirebaseData.OpenAsync(Context);
                if (data.Role != "companySupervisor") throw new UnauthorizedAccessException();
                var placements = data.Placements();
                var html = new StringBuilder("<p role='status'>Connected to Firebase. " + placements.Length + " assigned trainees.</p><label for='liveTraineeSearch'>Search trainees</label><input class='form-control mb-3' id='liveTraineeSearch' type='search' placeholder='Student name or ID'><div class='trainees-table-wrap'><table class='trainees-table'><thead><tr><th>Student</th><th>Training period</th><th>Approved hours</th><th>Completion</th><th>Actions</th></tr></thead><tbody>");
                foreach (var p in placements)
                {
                    var raw = data.Records("placements").FirstOrDefault(r => LiveFirebaseData.Text(r, "id") == p.InternshipId);
                    var ledger = await ((IApprovedTrainingHoursReader)data).GetAsync(p.InternshipId);
                    var required = await ((IRequiredTrainingHoursReader)data).GetAsync(p.InternshipId);
                    var review = await data.GetCompletionReviewAsync(p.InternshipId);
                    html.Append("<tr data-live-trainee><td>").Append(H(p.DisplayName)).Append("<br>").Append(H(p.StudentId)).Append("</td><td>").Append(H(LiveFirebaseData.Text(raw, "startDate"))).Append(" – ").Append(H(LiveFirebaseData.Text(raw, "endDate"))).Append("</td><td>").Append(H(ledger.Value.Where(h => h.IsApproved).Sum(h => h.Hours).ToString("0.#"))).Append(" / ").Append(H(required.Value?.ToString("0.#") ?? "Unknown")).Append("</td><td>").Append(H(review?.Status.ToString() ?? "In training")).Append("</td><td><a href='ReportsFinalEvaluation.aspx?internshipId=").Append(HttpUtility.UrlEncode(p.InternshipId)).Append("'>Final evaluation</a> · <a href='ManageTasks.aspx'>Tasks</a> · <a href='WeeklyReports.aspx'>Reports</a></td></tr>");
                }
                if (placements.Length == 0) html.Append("<tr><td colspan='5'>No internship placements are assigned to this supervisor.</td></tr>");
                TraineeContent = html.Append("</tbody></table></div><script>document.getElementById('liveTraineeSearch').addEventListener('input',function(){const q=this.value.toLowerCase();document.querySelectorAll('[data-live-trainee]').forEach(row=>row.hidden=!row.textContent.toLowerCase().includes(q));});</script>").ToString();
            }
            catch (UnauthorizedAccessException) { Response.Redirect("~/Auth/Login.aspx", false); Context.ApplicationInstance.CompleteRequest(); }
            catch (Exception error) { Trace.Warn("MyTrainees", error.GetType().Name); TraineeContent = "<div role='alert' class='alert alert-danger'>Firebase trainees could not be loaded. Refresh or sign in again.</div>"; }
        }
    }
}
