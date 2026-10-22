using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.Security;
using System.Text;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

namespace University_Internship_Management_System.Coordinator
{
    public partial class FinalCompletionReview : Page
    {
        private CoordinatorReviewPageContext reviewContext;
        private string initializationError;
        private bool decisionPostback;
        private bool reportPostback;
        private CompletionDecisionKind decisionKind = CompletionDecisionKind.Approve;
        protected void Page_Init(object sender, EventArgs e)
        {
            // Persist the session cookie for user-bound postback MACs, not evaluation data.
            Session["FinalEvaluation.CoordinatorReview.PostbackBinding"] = true;
            ViewStateUserKey = Session.SessionID;
            reviewAcknowledgment.InputAttributes["class"] = "form-check-input";
            Response.Cache.SetNoStore(); Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            try
            {
                reviewContext = CoordinatorReviewPageIntegration.Resolve(Context);
                if (reviewContext == null) return;
            }
            catch (UnauthorizedAccessException) { Deny(); }
            catch (Exception) { initializationError = "Trusted review integrations are unavailable. No completion decision was made."; reviewContext = null; studentSelect.Items.Clear(); }
        }
        protected void Page_Load(object sender, EventArgs e)
        { RegisterAsyncTask(new PageAsyncTask(LoadReviewAsync)); }
        protected void StudentSelect_Changed(object sender, EventArgs e) { }
        protected void RefreshReview_Click(object sender, EventArgs e) { }
        protected void ConfirmApproval_Click(object sender, EventArgs e) { decisionPostback = true; }
        protected void RequestCompletionChanges_Click(object sender, EventArgs e) { decisionPostback = true; decisionKind = CompletionDecisionKind.RequestChanges; }
        protected void RejectCompletion_Click(object sender, EventArgs e) { decisionPostback = true; decisionKind = CompletionDecisionKind.Reject; }
        protected void GenerateCompletionReport_Click(object sender, EventArgs e) { reportPostback = true; }
        protected override void Render(HtmlTextWriter writer)
        {
            // Recognize these fixed form-control shapes even when disabled, so forged posts
            // receive the same fail-closed service/UI response instead of a framework error.
            // This does not authorize a decision: the protected ticket and all domain checks remain required.
            ClientScript.RegisterForEventValidation(reviewAcknowledgment.UniqueID);
            ClientScript.RegisterForEventValidation(confirmApproval.UniqueID, "");
            ClientScript.RegisterForEventValidation(approveButton.UniqueID, "");
            ClientScript.RegisterForEventValidation(requestCompletionChanges.UniqueID, "");
            ClientScript.RegisterForEventValidation(rejectCompletion.UniqueID, "");
            ClientScript.RegisterForEventValidation(generateCompletionReport.UniqueID, "");
            base.Render(writer);
        }
        private void Deny()
        {
            initializationError = "Access denied. An authorized Coordinator and internship scope are required.";
            Response.StatusCode = 403; Response.TrySkipIisCustomErrors = true;
            reviewContext = null; studentSelect.Items.Clear();
        }
        private async Task LoadReviewAsync()
        {
            integrationNotice.Text = reviewContext == null
                ? "Official Firebase review is disabled until trusted authentication, scope, placement, policy, hours, tasks and reports are connected."
                : reviewContext.DevelopmentDatasetPath != null ? "LOCAL-ONLY Firebase DEMO: " + reviewContext.DevelopmentDatasetPath + ". Fictional data only; official paths are inaccessible."
                : reviewContext.IsDevelopment ? "Isolated in-memory development review. Fictional data and requirements only. No Firebase operations. " + (reviewContext.Decisions == null ? "Completion decisions are disabled." : "Isolated decisions enabled; generated PDFs remain private test objects.")
                : "Authorized review. Decisions require configured trusted integrations; document actions remain unavailable.";
            approveButton.Disabled = true; confirmApproval.Enabled = false; reviewNotes.Enabled = false; decisionReason.Enabled = false; reviewAcknowledgment.Enabled = false;
            if (reviewContext == null || reviewContext.Decisions == null)
            { reviewNotes.Text = ""; decisionReason.Text = ""; reviewAcknowledgment.Checked = false; decisionTicket.Value = ""; }
            reviewStatus.InnerText = "Blocked";
            generateCompletionReport.Enabled = false;
            eligibilityMessage.InnerText = initializationError ?? "Trusted review integrations are not connected.";
            studentSelect.Enabled = reviewContext != null; refreshReview.Enabled = reviewContext != null;
            foreach (var control in new[] { studentName, studentNumber, studentMajor, companyName, internshipTitle, companySupervisor, academicSupervisor,
                companyStatus, companyDate, companyRecommendation, companyFeedback, companyScores, academicStatus, academicDate, academicRecommendation, academicFeedback, academicScores, hoursText }) control.InnerText = "Not available";
            if (reviewContext == null) return;
            try
            {
                // Async services run without blocking the ASP.NET synchronization context.
                // Posted selection is validated explicitly below after rebuilding authorized options.
                var placements = await reviewContext.GetAuthorizedPlacementsAsync();
                foreach (var placement in placements) studentSelect.Items.Add(new ListItem(placement.DisplayName ?? placement.InternshipId, placement.InternshipId));
                string id = Request.Form[studentSelect.UniqueID] ?? Request.QueryString["internshipId"] ?? studentSelect.SelectedValue;
                if (string.IsNullOrWhiteSpace(id) || studentSelect.Items.FindByValue(id) == null) throw new UnauthorizedAccessException();
                studentSelect.SelectedValue = id;
                if (!decisionPostback && Request.Form["__EVENTTARGET"] == studentSelect.UniqueID)
                { reviewNotes.Text = ""; decisionReason.Text = ""; reviewAcknowledgment.Checked = false; }
                var decisionView = reviewContext.Decisions == null ? null : await reviewContext.Decisions.LoadAsync(id);
                var result = decisionView == null ? await reviewContext.Workflow.ReviewAsync(id) : decisionView.Readiness;
                if (reportPostback)
                {
                    if (reviewContext.GenerateReportAsync == null) reportGenerationMessage.Text = "Private report generation integration is unavailable. No PDF was created.";
                    else try { await reviewContext.GenerateReportAsync(id); reportGenerationMessage.Text = "Completion Report generated/verified through the private service. It is available to the authorized Student. No certificate or public URL was created."; }
                    catch (Exception) { reportGenerationMessage.Text = "The report could not be authorized or confirmed. Completion approval is unchanged; private storage may be unavailable."; }
                    decisionView = reviewContext.Decisions == null ? null : await reviewContext.Decisions.LoadAsync(id);
                }
                CompletionReview acknowledged = null;
                bool saveConfirmed = false;
                if (decisionPostback)
                {
                    try
                    {
                        if (reviewContext.Decisions == null) throw new InvalidOperationException("Trusted completion decision integrations are not connected.");
                        var ticket = ReadTicket(id);
                        var saved = await reviewContext.Decisions.DecideAsync(new CompletionDecisionRequest {
                            InternshipId = id, ExpectedStudentId = ticket.StudentId, ExpectedVersion = ticket.Version, PolicyId = ticket.PolicyId, PolicyVersion = ticket.PolicyVersion,
                            DecisionId = ticket.DecisionId, Kind = decisionKind, Reason = decisionReason.Text,
                            SupportingNotes = reviewNotes.Text, Confirmed = reviewAcknowledgment.Checked });
                        acknowledged = saved.Review; saveConfirmed = true;
                        decisionMessage.Text = saved.AlreadyRecorded ? "Decision already persisted; no duplicate decision was created." : "Completion decision saved and confirmed: " + saved.Review.Status + ". No document was generated or delivered.";
                        try { decisionView = await reviewContext.Decisions.LoadAsync(id); result = decisionView.Readiness; }
                        catch (Exception) { decisionView = null; decisionMessage.Text += " Reload could not be confirmed; refresh to retrieve current state."; }
                    }
                    catch (UnauthorizedAccessException) { throw; }
                    catch (ConflictException) { decisionMessage.Text = "Decision was not saved: the review or authoritative data changed, or a terminal decision already exists. Refresh before retrying."; }
                    catch (ArgumentException error) { decisionMessage.Text = "Decision was not saved: " + error.Message; }
                    catch (Exception) { decisionMessage.Text = "Approval persistence was not confirmed. Your reason and notes are preserved. Refresh or retry the same request to verify its outcome."; }
                }
                if (result.Placement != null)
                {
                    studentName.InnerText = result.Placement.DisplayName ?? result.Placement.StudentId;
                    studentNumber.InnerText = result.Placement.StudentId; internshipTitle.InnerText = result.Placement.InternshipId;
                }
                var evaluation = result.Evaluations?.PercentageEvaluation;
                var current = evaluation?.Current; var academicReview = PercentageEvaluationWorkflow.CurrentReview(evaluation);
                companyStatus.InnerText = PercentageEvaluationWorkflow.Status(evaluation);
                companyDate.InnerText = current?.SubmittedAtUtc?.ToString("u") ?? "Not submitted";
                companyRecommendation.InnerText = "Company version: " + (current?.VersionId ?? "Unavailable");
                companyFeedback.InnerText = current?.Comments ?? "Unavailable";
                companyScores.InnerText = "Final Evaluation Percentage: " + (current?.Percentage?.ToString(CultureInfo.InvariantCulture) ?? "Unavailable") + "; policy passing threshold: " + (result.ApplicablePolicy?.PassingThreshold?.ToString(CultureInfo.InvariantCulture) ?? "Unavailable; result cannot yet be determined");
                companySupervisor.InnerText = current?.CompanySupervisorId ?? "Unavailable";
                academicStatus.InnerText = academicReview == null ? "Academic Review pending" : academicReview.Kind.ToString();
                academicDate.InnerText = academicReview?.DecidedAtUtc.ToString("u") ?? "Not reviewed";
                academicRecommendation.InnerText = "Review of exact company version: " + (academicReview?.CompanyVersionId ?? "Unavailable");
                academicFeedback.InnerText = academicReview?.Reason ?? "No review decision recorded";
                academicScores.InnerText = "Academic review only; no independent academic percentage.";
                academicSupervisor.InnerText = academicReview?.AcademicSupervisorId ?? "Unavailable";
                hoursText.InnerText = (result.ApprovedHours.HasValue ? result.ApprovedHours.Value.ToString(CultureInfo.InvariantCulture) : "Unknown") + " approved / " +
                    (result.RequiredHours.HasValue ? result.RequiredHours.Value.ToString(CultureInfo.InvariantCulture) : "Unknown") + " required";
                if (result.ApprovedHours.HasValue && result.RequiredHours > 0)
                {
                    hoursProgress.Attributes["max"] = result.RequiredHours.Value.ToString(CultureInfo.InvariantCulture);
                    hoursProgress.Attributes["value"] = Math.Min(result.ApprovedHours.Value, result.RequiredHours.Value).ToString(CultureInfo.InvariantCulture);
                }
                reviewStatus.InnerText = result.IsReady ? "Ready for completion review" : "Blocked";
                reviewStatus.Attributes["class"] = "status-badge " + (result.IsReady ? "status-success" : "status-pending");
                eligibilityMessage.InnerText = result.IsReady ? "All checked readiness requirements are satisfied. Completion has not been approved." : "Completion is not ready. Resolve the blockers below and refresh.";
                foreach (string reason in result.IsReady ? new[] { "Current Company submission academically approved; passing policy, eligibility, approved hours, tasks and reports verified. Academic approval alone does not approve completion." } : result.Blockers)
                { var item = new HtmlGenericControl("li"); item.InnerText = reason; requirementsList.Controls.Add(item); }
                RenderDecision(decisionView, acknowledged, saveConfirmed, id);
            }
            catch (UnauthorizedAccessException) { Deny(); eligibilityMessage.InnerText = initializationError; }
            catch (Exception) { eligibilityMessage.InnerText = "Readiness could not be verified. Refresh when trusted services are available. No completion decision was made."; }
        }
        private sealed class DecisionTicket
        {
            public string UserId { get; set; }
            public string InternshipId { get; set; }
            public string StudentId { get; set; }
            public string PolicyId { get; set; }
            public string PolicyVersion { get; set; }
            public string Version { get; set; }
            public string DecisionId { get; set; }
        }
        private string[] TicketPurposes { get { return new[] { "FinalEvaluation.CompletionApproval.v1", Session.SessionID, reviewContext.UserId }; } }
        private DecisionTicket ReadTicket(string id)
        {
            try
            {
                var bytes = MachineKey.Unprotect(Convert.FromBase64String(decisionTicket.Value), TicketPurposes);
                var ticket = JsonConvert.DeserializeObject<DecisionTicket>(Encoding.UTF8.GetString(bytes));
                if (ticket == null || ticket.UserId != reviewContext.UserId || ticket.InternshipId != id) throw new ArgumentException();
                return ticket;
            }
            catch (Exception) { throw new ArgumentException("The protected decision reference is invalid. Refresh before retrying."); }
        }
        private void RenderDecision(CompletionDecisionView view, CompletionReview acknowledged, bool confirmed, string id)
        {
            var record = view == null ? acknowledged : view.Record.Value;
            if (record != null)
            {
                savedDecision.InnerText = "Saved decision: " + record.Status + "; Coordinator: " + record.ReviewerId + "; policy: " + record.PolicyId + "/" + record.PolicyVersion + "; UTC: " + (record.ReviewedAtUtc.HasValue ? record.ReviewedAtUtc.Value.ToString("u") : "Not recorded") + "; reason: " + record.Feedback;
                foreach (var audit in record.DecisionHistory ?? new System.Collections.Generic.List<CompletionDecisionAudit>())
                { var item = new HtmlGenericControl("li"); item.InnerText = audit.Kind + " · " + audit.CoordinatorId + " · " + audit.DecidedAtUtc.ToString("u") + " · " + audit.Reason + " · Notes: " + audit.SupportingNotes + " · Decision ID: " + audit.DecisionId + " · Requirements: " + string.Join(" ", audit.ReadinessBlockers ?? new string[0]); decisionHistory.Controls.Add(item); }
                if (record.Status == CompletionStatus.Approved || record.Status == CompletionStatus.Rejected)
                { reviewStatus.InnerText = record.Status.ToString(); eligibilityMessage.InnerText = "The saved completion decision is " + record.Status + ". Further decisions are locked. No document generation or delivery is implied."; }
            }
            var policy = view == null ? null : view.Readiness.ApplicablePolicy;
            var rule = policy == null || policy.CompletionDecisions == null ? null : policy.CompletionDecisions.Approve;
            bool canApprove = reviewContext.Decisions != null && reviewContext.Decisions.IsConfigured && view != null && view.Readiness.IsReady && policy.IsApproved && rule != null && rule.Enabled &&
                (record == null || (record.Status != CompletionStatus.Approved && record.Status != CompletionStatus.Rejected));
            approveButton.Disabled = !canApprove; confirmApproval.Enabled = canApprove; reviewAcknowledgment.Enabled = canApprove; reviewNotes.Enabled = canApprove; decisionReason.Enabled = canApprove;
            bool nonterminal = record == null || (record.Status != CompletionStatus.Approved && record.Status != CompletionStatus.Rejected);
            generateCompletionReport.Enabled = record != null && record.Status == CompletionStatus.Approved && reviewContext.GenerateReportAsync != null && policy?.CompletionReport?.Enabled == true;
            if (!generateCompletionReport.Enabled) reportGenerationMessage.Text = "Report generation requires saved Coordinator approval, approved report policy and an authorized private storage service. Print/certificates are not enabled.";
            bool decisionsAvailable = reviewContext.Decisions != null && reviewContext.Decisions.IsConfigured && policy != null && policy.IsApproved && nonterminal;
            requestCompletionChanges.Visible = requestCompletionChanges.Enabled = decisionsAvailable && policy.CompletionDecisions?.RequestChanges?.Enabled == true;
            rejectCompletion.Visible = rejectCompletion.Enabled = decisionsAvailable && policy.CompletionDecisions?.Reject?.Enabled == true;
            openCompletionChanges.Visible = requestCompletionChanges.Visible; openCompletionChanges.Disabled = !requestCompletionChanges.Enabled;
            openCompletionReject.Visible = rejectCompletion.Visible; openCompletionReject.Disabled = !rejectCompletion.Enabled;
            reviewAcknowledgment.Enabled = reviewNotes.Enabled = decisionReason.Enabled = canApprove || requestCompletionChanges.Enabled || rejectCompletion.Enabled;
            approvalSummary.InnerText = canApprove ? "Readiness checks currently pass. The server will recheck current authoritative data at confirmation. " + (rule.RequireReason ? "A reason is required. " : "") + (rule.RequireSupportingNotes ? "Supporting notes are required." : "") : "Approval is disabled because readiness, policy, integrations or current decision state do not permit it.";
            if (view != null && (!decisionPostback || confirmed))
            {
                var ticket = new DecisionTicket { UserId = reviewContext.UserId, InternshipId = id, StudentId = view.Readiness.Placement.StudentId, PolicyId = view.Readiness.Placement.PolicyId,
                    PolicyVersion = view.Readiness.Placement.PolicyVersion, Version = view.Record.Version, DecisionId = Guid.NewGuid().ToString("N") };
                decisionTicket.Value = Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ticket)), TicketPurposes));
            }
        }
        private static void RenderEvaluation(EvaluationRecord record, HtmlGenericControl status, HtmlGenericControl date, HtmlGenericControl details,
            HtmlGenericControl feedback, HtmlGenericControl scores, HtmlGenericControl supervisor)
        {
            status.InnerText = record == null ? "Missing or not Submitted" : "Submitted";
            status.Attributes["class"] = "status-badge " + (record == null ? "status-pending" : "status-success");
            if (record == null) return;
            date.InnerText = record.SubmittedAtUtc.HasValue ? record.SubmittedAtUtc.Value.ToString("u") : "Submission timestamp unavailable";
            details.InnerText = "Policy " + record.PolicyId + " / " + record.PolicyVersion + "; evaluator " + record.EvaluatorId;
            supervisor.InnerText = record.EvaluatorId; feedback.InnerText = record.Feedback ?? "No general feedback supplied";
            var criterionFeedback = record is CompanyFinalEvaluation ? ((CompanyFinalEvaluation)record).CriterionFeedback : ((AcademicFinalEvaluation)record).CriterionFeedback;
            scores.InnerText = "Scores: " + (record.Scores == null ? "Unavailable" : string.Join("; ", record.Scores.Select(pair => pair.Key + ": " + (pair.Value.HasValue ? pair.Value.Value.ToString(CultureInfo.InvariantCulture) : "Not supplied")))) +
                "; Criterion feedback: " + (criterionFeedback == null || criterionFeedback.Count == 0 ? "None supplied" : string.Join("; ", criterionFeedback.Select(pair => pair.Key + ": " + pair.Value)));
        }
    }
}
