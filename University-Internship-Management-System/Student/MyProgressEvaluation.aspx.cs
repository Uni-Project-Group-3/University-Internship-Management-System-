using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

namespace University_Internship_Management_System.Student
{
    public partial class MyProgressEvaluation : Page
    {
        private bool downloadRequested, fileResponse;
        private bool demoSelectionChanged;
        protected void Page_Init(object sender, EventArgs e)
        {
            Session["FinalEvaluation.StudentCompletion.PostbackBinding"] = true;
            ViewStateUserKey = Session.SessionID;
            Response.Cache.SetNoStore(); Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            var sidebar = FindControlRecursive(this, "studentSidebar") as System.Web.UI.UserControl;
            var slot = sidebar == null ? null : FindControlRecursive(sidebar, "LogoutSlot") as System.Web.UI.WebControls.PlaceHolder;
            if (slot != null && slot.Controls.Count == 0)
            {
                var logout = new LinkButton { ID = "btnLogout", Text = "Logout", CausesValidation = false, CssClass = "uims-side-link uims-side-logout" };
                logout.Click += btnLogout_Click;
                slot.Controls.Add(logout);
            }
        }
        private static Control FindControlRecursive(Control parent, string id)
        {
            if (parent == null) return null;
            if (string.Equals(parent.ID, id, StringComparison.Ordinal)) return parent;
            foreach (Control child in parent.Controls)
            {
                var match = FindControlRecursive(child, id);
                if (match != null) return match;
            }
            return null;
        }
        protected void Page_Load(object sender, EventArgs e) { RegisterAsyncTask(new PageAsyncTask(LoadCompletionAsync)); }
        protected void RefreshProgress_Click(object sender, EventArgs e) { } // Page_Load re-reads the authorized workflow on every request.
        protected void DownloadCompletionReport_Click(object sender, EventArgs e) { downloadRequested = true; }
        protected void DemoStudentChoice_Changed(object sender, EventArgs e) { demoSelectionChanged = true; }
        protected void ToggleExamDemo_Click(object sender, EventArgs e) { }
        protected void Exams_ItemCommand(object source, RepeaterCommandEventArgs e) { }
        protected void btnLogout_Click(object sender, EventArgs e) { University_Internship_Management_System.Shared.LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }
        protected override void Render(HtmlTextWriter writer)
        {
            if (fileResponse) return; // CompleteRequest alone does not stop the Web Forms renderer.
            ClientScript.RegisterForEventValidation(DownloadCompletionReport.UniqueID, "");
            base.Render(writer);
        }
        private void Empty()
        {
            NavName.Text = "Student Portal";
            StudentAccessErrorPanel.Visible = false;
            foreach (var literal in new[] { ApprovedHours, RequiredHours, RequiredHoursInline, RemainingHours, HoursPercent, HoursApprovalStatus, PendingHours, ApprovedHoursNotice, RequiredHoursNotice, OverallProgressNotice, AssignedTasks, SubmittedTasks, EvaluatedTasks, RevisionTasks, EvaluatedExams }) literal.Text = "Unknown";
            ApprovedHoursNotice.Text = RequiredHoursNotice.Text = OverallProgressNotice.Text = "";
            CompletionStatusText.Text = "Not available"; CompletionStatusMessage.Text = "Trusted student completion integration is unavailable.";
            CompletionStatusBadge.Attributes["class"] = "workspace-badge warning student-completion-badge";
            CompletionApprovalDate.Text = "Not available"; OverallCompletionProgress.Text = "Not yet available";
            ReportStatusText.Text = "Unavailable"; ReportAvailabilityText.Text = "Report access is not connected.";
            DownloadCompletionReport.Visible = false; HoursProgress.Attributes.Remove("value");
            HoursProgress.Visible = false;
            PendingHoursNotice.Text = "Pending hours require a trusted attendance source; rejected entries are not counted as pending.";
            Tasks.DataSource = new StudentVisibleTask[0]; Tasks.DataBind();
            TaskEmptyState.Visible = true; TasksTablePanel.Visible = false;
            TaskEmptyState.Controls.Clear();
            TaskEmptyState.Controls.Add(new LiteralControl("<i class=\"bi bi-inbox\" aria-hidden=\"true\"></i><strong>Task data unavailable</strong><span>Published task results will appear here when the trusted Tasks integration is connected.</span>"));
            taskFilterStatus.InnerText = "Trusted task information is unavailable.";
            EvaluatedExams.Text = "Published task grades unavailable";
            ToggleExamDemo.Enabled = false; ToggleExamDemo.Text = "Exam integration unavailable";
            ExamDemoNotice.Visible = false; ExamList.Visible = false; ExamDetails.Visible = false; NoExams.Visible = true;
            Exams.DataSource = new object[0]; Exams.DataBind();
        }
        private async Task LoadCompletionAsync()
        {
            Empty();
            try
            {
                var context = await StudentCompletionPageIntegration.ResolveAsync(Context);
                if (context == null)
                {
                    StudentAccessMessage.Text = "Official Firebase student access is disabled until trusted account and internship integrations are connected. Progress records have not been loaded; this does not mean your internship has no records.";
                    StudentAccessErrorPanel.Visible = true;
                    if (downloadRequested) SendError(503, "Trusted student report access is not connected.");
                    return;
                }
                DemoStudentPanel.Visible = context.IsDevelopment && context.DevelopmentDatasetPath != null;
                if (DemoStudentPanel.Visible) DemoStudentChoice.SelectedValue = context.DemoStudentIndex.ToString(CultureInfo.InvariantCulture);
                string id = (demoSelectionChanged ? null : Request.QueryString["internshipId"]) ?? context.DefaultInternshipId;
                if (string.IsNullOrWhiteSpace(id)) { StudentAccessMessage.Text = "No authorized internship is assigned to this student."; StudentAccessErrorPanel.Visible = true; if (downloadRequested) SendError(404, "No authorized internship is available."); return; }
                if (downloadRequested)
                {
                    var report = await context.Workflow.DownloadAsync(id);
                    fileResponse = true; Response.Clear(); Response.BufferOutput = true;
                    Response.ContentType = report.ContentType;
                    Response.Charset = "";
                    Response.AddHeader("Content-Disposition", "attachment; filename=\"" + report.FileName + "\"");
                    Response.AddHeader("Content-Length", report.Content.LongLength.ToString(CultureInfo.InvariantCulture));
                    Response.Headers["X-Content-Type-Options"] = "nosniff";
            var sidebar = FindControlRecursive(this, "studentSidebar") as System.Web.UI.UserControl;
            var slot = sidebar == null ? null : FindControlRecursive(sidebar, "LogoutSlot") as System.Web.UI.WebControls.PlaceHolder;
            if (slot != null && slot.Controls.Count == 0)
            {
                var logout = new LinkButton { ID = "btnLogout", Text = "Logout", CausesValidation = false, CssClass = "uims-side-link uims-side-logout" };
                logout.Click += btnLogout_Click;
                slot.Controls.Add(logout);
            }
                    Response.Cache.SetNoStore(); Response.Cache.SetCacheability(HttpCacheability.Private);
                    Response.BinaryWrite(report.Content); Context.ApplicationInstance.CompleteRequest(); return;
                }
                var view = await context.Workflow.LoadAsync(id);
                NavName.Text = context.DisplayName ?? "Student Portal";
                StudentAccessMessage.Text = "";
                StudentAccessErrorPanel.Visible = false;
                ApprovedHours.Text = HoursApprovalStatus.Text = Number(view.ApprovedHours); RequiredHours.Text = RequiredHoursInline.Text = Number(view.RequiredHours);
                RemainingHours.Text = Number(view.RemainingHours);
                ApprovedHoursNotice.Text = view.ApprovedHours.HasValue ? "" : "Approved attendance data is not yet available from a verified source.";
                RequiredHoursNotice.Text = view.RequiredHours.HasValue ? "" : "Required hours are not yet available from a verified active placement.";
                PendingHours.Text = Number(view.PendingHours);
                PendingHoursNotice.Text = view.PendingHours.HasValue ? "Only attendance explicitly awaiting approval is included. Rejected records do not count." : "Pending hours are unavailable because the trusted pending-status source is not connected or could not be verified.";
                HoursPercent.Text = view.HoursPercent.HasValue ? Number(view.HoursPercent) + "%" : "Not supplied";
                HoursProgress.Visible = view.HoursPercent.HasValue;
                if (view.HoursPercent.HasValue) HoursProgress.Attributes["value"] = view.HoursPercent.Value.ToString(CultureInfo.InvariantCulture);
                OverallCompletionProgress.Text = view.OverallPercent.HasValue ? Number(view.OverallPercent) + "%" : "Not yet available";
                OverallProgressNotice.Text = view.OverallPercent.HasValue ? "" : "A trusted overall-progress value has not been supplied.";
                CompletionStatusText.Text = view.Status.HasValue ? (view.Status == CompletionStatus.ChangesRequested ? "Changes requested" : view.Status.Value.ToString()) : "Not recorded";
                CompletionStatusBadge.Attributes["class"] = "workspace-badge " + (view.Status == CompletionStatus.Approved ? "success" : view.Status == CompletionStatus.ChangesRequested || view.Status == CompletionStatus.Rejected ? "danger" : "warning") + " student-completion-badge";
                CompletionStatusMessage.Text = view.Status == CompletionStatus.Approved ? "Your internship completion is Approved. PDF availability is shown separately below." :
                    view.Status == CompletionStatus.UnderReview ? "Your completion request is under Coordinator review." :
                    view.Status == CompletionStatus.Pending ? "Completion is pending Coordinator review." :
                    view.Status == CompletionStatus.ChangesRequested ? "The Coordinator requested changes. Review the completion requirements with your supervisors." :
                    view.Status == CompletionStatus.Rejected ? "The Coordinator recorded a rejected completion decision." : "No Coordinator completion decision is recorded yet.";
                CompletionApprovalDate.Text = view.ApprovedAtUtc.HasValue ? view.ApprovedAtUtc.Value.ToString("u") : "Not recorded";
                Tasks.DataSource = view.Tasks; Tasks.DataBind();
                TasksTablePanel.Visible = view.TasksVerified && view.Tasks.Length > 0;
                TaskEmptyState.Visible = !TasksTablePanel.Visible;
                if (view.TasksVerified && view.Tasks.Length == 0)
                {
                    TaskEmptyState.Controls.Clear();
                    TaskEmptyState.Controls.Add(new LiteralControl("<i class=\"bi bi-inbox\" aria-hidden=\"true\"></i><strong>No published task grades available</strong><span>No student-visible task records were found.</span>"));
                }
                else if (!view.TasksVerified)
                {
                    TaskEmptyState.Controls.Clear();
                    TaskEmptyState.Controls.Add(new LiteralControl("<i class=\"bi bi-cloud-slash\" aria-hidden=\"true\"></i><strong>Task data unavailable</strong><span>A trusted published-task data source is not connected or could not be verified.</span>"));
                }
                EvaluatedExams.Text = view.PublishedTaskGradesText;
                if (view.TasksVerified) {
                    AssignedTasks.Text = view.Tasks.Length.ToString(CultureInfo.InvariantCulture);
                    SubmittedTasks.Text = view.Tasks.Count(t => t.StatusKey == "submitted").ToString(CultureInfo.InvariantCulture);
                    EvaluatedTasks.Text = view.Tasks.Count(t => t.StatusKey == "evaluated").ToString(CultureInfo.InvariantCulture);
                    RevisionTasks.Text = view.Tasks.Count(t => t.StatusKey == "revision").ToString(CultureInfo.InvariantCulture);
                }
                taskFilterStatus.InnerText = view.TasksVerified ? (view.Tasks.Length == 0 ? "No student-visible tasks are available." : view.Tasks.Length + " student-visible tasks.") : "Trusted task information is unavailable.";
                ReportStatusText.Text = view.Status == CompletionStatus.Approved ? "Completion Approved" : "Not approved";
                ReportAvailabilityText.Text = view.ReportMessage + " Final evaluation outcome: " + view.FinalOutcome + "."; DownloadCompletionReport.Visible = view.ReportAvailable;
            }
            catch (UnauthorizedAccessException) { Empty(); StudentAccessMessage.Text = "Access denied. A verified student and ownership of this internship are required."; StudentAccessErrorPanel.Visible = true; if (downloadRequested) SendError(403, "Access denied."); else { Response.StatusCode = 403; Response.TrySkipIisCustomErrors = true; } }
            catch (EvaluationIntegrationUnavailableException error) { Empty(); StudentAccessMessage.Text = error.Message; StudentAccessErrorPanel.Visible = true; if (downloadRequested) SendError(503, error.Message); }
            catch (StudentReportUnavailableException error) { SendError(error.HttpStatus, error.Message); }
            catch (ArgumentException) { if (downloadRequested) SendError(400, "Invalid report request."); else { Response.StatusCode = 400; Response.TrySkipIisCustomErrors = true; StudentAccessMessage.Text = "Invalid completion request."; StudentAccessErrorPanel.Visible = true; } }
            catch (Exception) { if (downloadRequested) SendError(503, "Report access is temporarily unavailable."); else { Response.StatusCode = 503; Response.TrySkipIisCustomErrors = true; StudentAccessMessage.Text = "Completion information could not be verified. Please try again later."; StudentAccessErrorPanel.Visible = true; } }
        }
        private static string Number(decimal? value) { return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : "Unknown"; }
        private void SendError(int status, string message)
        {
            fileResponse = true; Response.Clear(); Response.StatusCode = status; Response.TrySkipIisCustomErrors = true; Response.SuppressFormsAuthenticationRedirect = true;
            Response.ContentType = "text/plain; charset=utf-8"; Response.Cache.SetNoStore(); Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Write(message); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
