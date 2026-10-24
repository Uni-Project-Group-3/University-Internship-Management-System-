using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Configuration;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Shared Web Forms orchestration only; authorization and persistence stay in C# workflows.
    public abstract class PercentageEvaluationPage : Page
    {
        protected override void OnPreInit(EventArgs e)
        {
            UIMS.Services.FinalReportPageRoute.TransferIfEnabled(this, Academic ? "academicSupervisor" : "companySupervisor");
            base.OnPreInit(e);
        }
        private static readonly HttpClient FirebaseReadClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        protected abstract bool Academic { get; }
        private string operation;
        private PercentagePageContext context;
        private T Field<T>(string id) where T : Control { return (T)Find(this, id); }
        private static Control Find(Control parent, string id) { if (parent.ID == id) return parent; foreach (Control child in parent.Controls) { var match = Find(child, id); if (match != null) return match; } return null; }
        protected override void OnInit(EventArgs e)
        {
            Session["FinalEvaluation.Percentage.Postback"] = true; ViewStateUserKey = Session.SessionID; base.OnInit(e);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e); Response.Cache.SetNoStore(); Response.Headers["X-Content-Type-Options"] = "nosniff";
            RegisterAsyncTask(new PageAsyncTask(LoadWorkflow));
        }
        protected override void Render(HtmlTextWriter writer)
        {
            // Recognize a legacy forged percentage post only to ignore it in the Academic
            // workflow. It is never read as an Academic score or used to update Company data.
            if (Academic) ClientScript.RegisterForEventValidation(Field<TextBox>("FinalPercentage").UniqueID, "");
            base.Render(writer);
        }
        protected void SaveDraft_Click(object sender, EventArgs e) { operation = "save"; }
        protected void ConfirmSubmission_Click(object sender, EventArgs e) { operation = "submit"; }
        protected void ApproveReview_Click(object sender, EventArgs e) { operation = "approve"; }
        protected void RequestRevision_Click(object sender, EventArgs e) { operation = "revision"; }
        protected void btnLogout_Click(object sender, EventArgs e) { University_Internship_Management_System.Shared.LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }
        private static string H(string value) { return HttpUtility.HtmlEncode(value ?? ""); }
        protected virtual void BindStatistics(int? assigned, int? drafts, int? awaiting, int? approved)
        {
            if (!Academic) return;
            foreach (var item in new[] {
                new { Id = "AssignedStudentCount", Value = assigned }, new { Id = "CompanyDraftCount", Value = drafts },
                new { Id = "AwaitingReviewCount", Value = awaiting }, new { Id = "AcademicApprovalCount", Value = approved } })
                Field<Literal>(item.Id).Text = item.Value.HasValue ? item.Value.Value.ToString(CultureInfo.InvariantCulture) : "—";
        }
        private void DisableActions()
        {
            foreach (var id in new[] { "SaveDraft", "ConfirmSubmission", "ApproveReview", "RequestRevision" }) Field<Button>(id).Enabled = false;
            Field<CheckBox>("ConfirmAction").Enabled = false;
        }
        private async Task LoadWorkflow()
        {
            var rows = Field<Literal>("traineeRows"); var availability = Field<Literal>("DraftAvailability");
            Field<HiddenField>("activePanel").Value = "";
            BindStatistics(null, null, null, null);
            Field<Button>("ApproveReview").Visible = Field<Button>("RequestRevision").Visible = Academic;
            Field<Button>("SaveDraft").Visible = Field<Button>("ConfirmSubmission").Visible = !Academic;
            Field<Panel>("AcademicReviewPanel").Visible = false;
            foreach (var id in new[] { "SaveDraft", "ConfirmSubmission", "ApproveReview", "RequestRevision" }) Field<Button>(id).Enabled = false;
            try
            {
                context = await PercentagePageIntegration.Resolve(Context, Academic);
                if (context == null || context.Workflow == null || string.IsNullOrWhiteSpace(context.UserId))
                {
                    ClearEditor();
                    availability.Text = "Supervisor access integration is unavailable. Verified supervisor identity and assignment sources are not connected; Firebase records have not been loaded.";
                    rows.Text = "<tr><td colspan=3>No authorized students are connected.</td></tr>"; return;
                }
                availability.Text = context.DatasetPath != null ? "LOCAL-ONLY Firebase DEMO. Fictional data persists only at " + context.DatasetPath + ". No official Firebase paths. Test policy values are not university rules."
                    : context.IsDevelopment ? "LOCAL-ONLY isolated fictional test mode. Shared session memory; no Firebase writes. Fixture passing threshold is not a university rule." : "Authorized company percentage and academic review workflow.";
                if (context.Placements == null)
                {
                    ClearEditor(); availability.Text = "Authorized placement information is unavailable. No empty-data conclusion can be made.";
                    rows.Text = "<tr><td colspan=3>Placement records could not be verified.</td></tr>"; return;
                }
                if (!string.IsNullOrWhiteSpace(context.WriteBlocker)) availability.Text += " " + context.WriteBlocker;
                var html = new StringBuilder();
                bool incomplete = false;
                var states = new Dictionary<string, RecordSnapshot<PercentageEvaluation>>();
                int columnCount = Academic ? 5 : 3;
                foreach (var p in context.Placements ?? new CompletionPlacement[0])
                {
                    try
                    {
                        var s = await context.Workflow.LoadAsync(p.InternshipId, Academic); states[p.InternshipId] = s;
                        string status = PercentageEvaluationWorkflow.Status(s.Value);
                        var menu = new StringBuilder();
                        Action<string, string> item = (name, label) =>
                        {
                            string icon = name == "openReports" ? "bi-file-earmark-text" : name == "openDetails" ? "bi-person-vcard" :
                                name == "openRevision" ? "bi-arrow-repeat" : label.StartsWith("View", StringComparison.Ordinal) ? "bi-eye" : "bi-pencil-square";
                            menu.Append("<li><button class=\"dropdown-item company-action-item\" type=\"submit\" name=\"").Append(name)
                                .Append("\" value=\"").Append(H(p.InternshipId)).Append("\"><i class=\"bi ").Append(icon)
                                .Append("\" aria-hidden=\"true\"></i><span>").Append(H(label)).Append("</span></button></li>");
                        };
                        if (Academic)
                        {
                            if (s.Value?.Current.SubmittedAtUtc != null) { item("openEvaluation", "View Company Evaluation"); item("openEvaluation", PercentageEvaluationWorkflow.CurrentReview(s.Value) == null ? "Review Evaluation" : "View Review Decision"); }
                            if (context.ReadDetailsAsync != null) item("openDetails", "View Student Details");
                        }
                        else
                        {
                            if (context.ReadReportsAsync != null) item("openReports", "View Reports");
                            if (context.ReadDetailsAsync != null) item("openDetails", "View Details");
                            item(status == "Revision Requested" ? "openRevision" : "openEvaluation", status == "Not Started" ? "Start Evaluation" : status == "Draft" ? "Continue Draft" : status == "Revision Requested" ? "Revise Evaluation" : "View Evaluation");
                        }
                        if (Academic)
                        {
                            string companyStatus = s.Value?.Current == null ? "Not Started" : s.Value.Current.SubmittedAtUtc.HasValue ? "Submitted" : "Draft";
                            var academicReview = PercentageEvaluationWorkflow.CurrentReview(s.Value);
                            string reviewStatus = academicReview == null ? (companyStatus == "Submitted" ? "Pending Review" : "Unavailable") :
                                academicReview.Kind == AcademicReviewKind.Approve ? "Approved" : "Revision Requested";
                            html.Append("<tr data-evaluation-row=\"true\" data-status=\"").Append(H(status)).Append("\"><td class=\"trainee-identity\" data-label=\"Student\"><strong>").Append(H(p.DisplayName)).Append("</strong><span class=\"workspace-hint d-block\">Student ID: ").Append(H(p.StudentId)).Append("</span></td><td data-label=\"Company\">").Append(H(p.CompanyName ?? "Unavailable")).Append("</td><td data-label=\"Company evaluation\"><span class=\"workspace-badge evaluation-chip\">").Append(H(companyStatus)).Append("</span></td><td data-label=\"Academic review\"><span class=\"workspace-badge evaluation-chip\">").Append(H(reviewStatus)).Append("</span></td><td class=\"trainee-action-cell\" data-label=\"Actions\"><div class=\"dropdown company-action-dropdown\"><button type=\"button\" class=\"btn btn-outline-secondary dropdown-toggle company-action-toggle\" data-bs-toggle=\"dropdown\" data-bs-boundary=\"viewport\" aria-expanded=\"false\" aria-label=\"Actions: ").Append(H(p.DisplayName)).Append("\">Actions</button><ul class=\"dropdown-menu company-action-menu\">").Append(menu).Append("</ul></div></td></tr>");
                        }
                        else html.Append("<tr data-evaluation-row=\"true\" data-status=\"").Append(H(status)).Append("\"><td class=\"trainee-identity\"><strong>").Append(H(p.DisplayName)).Append("</strong></td><td data-label=\"Evaluation status\"><span class=\"workspace-badge evaluation-chip\">").Append(H(status)).Append("</span></td><td class=\"trainee-action-cell\" data-label=\"Actions\"><div class=\"dropdown company-action-dropdown\"><button type=\"button\" class=\"btn btn-outline-secondary dropdown-toggle company-action-toggle\" data-bs-toggle=\"dropdown\" data-bs-boundary=\"viewport\" aria-expanded=\"false\" aria-label=\"Actions: ").Append(H(p.DisplayName)).Append("\">Actions</button><ul class=\"dropdown-menu company-action-menu\">").Append(menu).Append("</ul></div></td></tr>");
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (ArgumentException) { incomplete = true; html.Append("<tr><td>").Append(H(p.DisplayName)).Append("</td><td colspan=2>Legacy or invalid evaluation: migration required; actions disabled.</td></tr>"); }
                }
                rows.Text = html.Length == 0 ? "<tr><td colspan=" + columnCount.ToString(CultureInfo.InvariantCulture) + ">No authorized students available.</td></tr>" : html.ToString();
                if (!incomplete)
                {
                    var statistics = EvaluationPageStatistics.From(states.Values.Select(snapshot => snapshot.Value));
                    BindStatistics(statistics.Assigned, statistics.Drafts, statistics.AwaitingReview, statistics.Approved);
                }
                else availability.Text += " Some evaluation records are invalid or require migration. Statistics are unavailable until all authorized records can be verified.";
                string selection = null, panel = null;
                foreach (var key in new[] { "openReports", "openDetails", "openEvaluation", "openRevision" })
                    if (!string.IsNullOrEmpty(Request.Unvalidated.Form[key])) { selection = Request.Unvalidated.Form[key]; panel = key; break; }
                if (selection == null) selection = IsPostBack ? Field<HiddenField>("selectedInternship").Value : Request.QueryString["internshipId"];
                if (string.IsNullOrEmpty(selection)) return;
                if (!states.ContainsKey(selection))
                {
                    ClearEditor();
                    Field<Literal>("EvaluationError").Text = "The requested internship is not in your verified Academic Supervisor assignment scope.";
                    availability.Text = "Access to the requested internship was denied. Your authorized student list remains available.";
                    Response.StatusCode = 403; Response.TrySkipIisCustomErrors = true;
                    return;
                }
                var placement = context.Placements.Single(p => p.InternshipId == selection);
                var state = await context.Workflow.LoadAsync(selection, Academic);
                Field<HiddenField>("selectedInternship").Value = selection;
                Field<Literal>("SelectedName").Text = placement.DisplayName;
                Field<HiddenField>("activePanel").Value = panel == "openReports" || panel == "openDetails" ? "informationDialog" : "evaluationDialog";
                if (panel == "openReports" || panel == "openDetails")
                {
                    var adapter = panel == "openReports" ? context.ReadReportsAsync : context.ReadDetailsAsync;
                    Field<Literal>("InformationText").Text = adapter == null ? "Trusted data source is unavailable." : await adapter(selection);
                    Field<Literal>("InformationTitle").Text = panel == "openReports" ? "Authorized reports" : "Student and internship details";
                    return;
                }
                bool failed = false;
                if (Academic && operation != null) Field<HiddenField>("reviewChoice").Value = operation == "revision" ? "revision" : "approve";
                if (operation != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(context.WriteBlocker)) throw new ArgumentException(context.WriteBlocker);
                        var ticket = ReadTicket(selection);
                        if (operation == "save")
                        {
                            if (Academic) throw new UnauthorizedAccessException();
                            var text = Field<TextBox>("FinalPercentage").Text.Trim(); decimal parsed;
                            if (text.Length > 0 && !decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out parsed)) throw new ArgumentException("Enter a percentage between 0 and 100.");
                            decimal? percentage = text.Length == 0 ? (decimal?)null : decimal.Parse(text, CultureInfo.InvariantCulture);
                            await context.Workflow.SaveDraftAsync(selection, ticket.Version, percentage, Field<TextBox>("FinalComments").Text, ticket.Revision);
                        }
                        else if (operation == "submit")
                        {
                            if (Academic) throw new UnauthorizedAccessException();
                            await context.Workflow.SubmitAsync(selection, ticket.Version, Field<CheckBox>("ConfirmAction").Checked);
                        }
                        else
                        {
                            if (!Academic) throw new UnauthorizedAccessException();
                            await context.Workflow.ReviewAsync(selection, ticket.Version, ticket.CompanyVersion, operation == "approve" ? AcademicReviewKind.Approve : AcademicReviewKind.RequestRevision,
                                operation == "revision" ? Field<TextBox>("RevisionReason").Text : "", ticket.DecisionId, Field<CheckBox>("ConfirmAction").Checked);
                        }
                        Field<Literal>("OperationMessage").Text = "Saved and confirmed by the repository. " + (context.DatasetPath != null ? "Firebase development dataset only: " + context.DatasetPath : context.IsDevelopment ? "Isolated memory only; no Firebase writes." : "");
                        // Reload before refreshing displayed states. Never optimistic browser status changes.
                        operation = null;
                        await LoadWorkflow(); return;
                    }
                    catch (Exception error) when (error is ArgumentException || error is ConflictException || error is UnauthorizedAccessException || error is RepositoryException)
                    { failed = true; Field<Literal>("EvaluationError").Text = error is ArgumentException ? error.Message : error is ConflictException ? "The evaluation changed. Your entries are preserved; reopen before retrying." : "Operation could not be authorized or confirmed. Your entries are preserved."; }
                    state = await context.Workflow.LoadAsync(selection, Academic);
                }
                var current = state.Value?.Current; var review = PercentageEvaluationWorkflow.CurrentReview(state.Value);
                if (Academic && current?.SubmittedAtUtc == null)
                {
                    Field<TextBox>("FinalPercentage").Text = Field<TextBox>("FinalComments").Text = "";
                    Field<TextBox>("FinalPercentage").ReadOnly = Field<TextBox>("FinalComments").ReadOnly = true;
                    Field<HiddenField>("activePanel").Value = "informationDialog";
                    Field<Literal>("InformationTitle").Text = "Company Evaluation unavailable";
                    Field<Literal>("InformationText").Text = "The current Company version is not Submitted. Draft content cannot be reviewed or approved.";
                    return;
                }
                Field<Literal>("FinalStatus").Text = PercentageEvaluationWorkflow.Status(state.Value);
                Field<Literal>("SubmissionTime").Text = current?.SubmittedAtUtc?.ToString("u") ?? "Not submitted";
                Field<Literal>("ReviewStatus").Text = review == null ? "Academic Review pending" : review.Kind + "; " + review.DecidedAtUtc.ToString("u") + (Academic || review.Kind == AcademicReviewKind.RequestRevision ? "; " + review.Reason : "");
                if (Academic)
                {
                    Field<Literal>("CompanyPercentageDisplay").Text = current?.Percentage?.ToString("0.##", CultureInfo.InvariantCulture) + "%";
                    Field<Literal>("ReviewStatus").Text = review == null ? "Pending Review" : review.Kind == AcademicReviewKind.Approve ? "Approved" : "Revision Requested";
                    Field<Literal>("ReviewTimestamp").Text = review == null ? "" : "Reviewed UTC: " + review.DecidedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
                    Field<Literal>("RecordedReviewReason").Text = review?.Kind == AcademicReviewKind.RequestRevision ? "Revision reason: " + review.Reason : "";
                    var choice = Field<HiddenField>("reviewChoice"); choice.Value = choice.Value == "revision" ? "revision" : "approve";
                    Field<CheckBox>("ConfirmAction").Text = choice.Value == "revision" ? "I confirm Request Revision of this Company evaluation for the reason provided." : "I confirm Academic Approval of this submitted Company evaluation.";
                    Field<CheckBox>("ConfirmAction").Enabled = review == null;
                    Field<TextBox>("RevisionReason").ReadOnly = review != null;
                }
                Field<Literal>("FinalOutcome").Text = await context.Workflow.OutcomeAsync(selection, Academic);
                bool revision = panel == "openRevision" || (IsPostBack && !string.IsNullOrWhiteSpace(Field<HiddenField>("evaluationTicket").Value) && SafeRevision(selection));
                revision = revision && current?.SubmittedAtUtc != null;
                // Keep record status permissions distinct from the deployment-wide Firebase write gate.
                // A Draft/revision may be editable by status while still read-only in production until
                // the trusted guard, approved policy and schema/write configuration are ready.
                bool writesAvailable = string.IsNullOrWhiteSpace(context.WriteBlocker);
                if (!context.IsDevelopment)
                {
                    try { new FirebaseFinalEvaluationConfiguration().EnsureWritesAllowed(); }
                    catch (UIMS.Services.FirebaseException)
                    {
                        writesAvailable = false;
                        availability.Text += " Firebase records are connected. Evaluation writes are disabled in configuration.";
                    }
                }
                var companyWriteState = Academic ? null : await context.Workflow.GetCompanyWriteStateAsync(selection, revision);
                bool recordAllowsEdit = companyWriteState != null && companyWriteState.CanEditDraft;
                bool recordAllowsSubmit = companyWriteState != null && companyWriteState.CanSubmit;
                bool editable = writesAvailable && !Academic && recordAllowsEdit;
                Field<TextBox>("FinalPercentage").ReadOnly = Field<TextBox>("FinalComments").ReadOnly = !editable;
                if (!Academic) Field<CheckBox>("ConfirmAction").Enabled = writesAvailable && recordAllowsSubmit;
                Field<Button>("SaveDraft").Enabled = editable;
                Field<Button>("ConfirmSubmission").Enabled = writesAvailable && !Academic && recordAllowsSubmit;
                Field<Panel>("AcademicReviewPanel").Visible = writesAvailable && Academic && review == null;
                Field<Button>("ApproveReview").Enabled = Field<Button>("RequestRevision").Enabled = writesAvailable && Academic && current?.SubmittedAtUtc != null && review == null;
                if (Academic && !writesAvailable) Field<CheckBox>("ConfirmAction").Enabled = false;
                if (companyWriteState != null && companyWriteState.CompletionFinalized)
                    Field<Literal>("EvaluationError").Text = "Completion has been finalized; evaluation changes are locked.";
                if (!failed) { Field<TextBox>("FinalPercentage").Text = current?.Percentage?.ToString(CultureInfo.InvariantCulture) ?? ""; Field<TextBox>("FinalComments").Text = current?.Comments ?? ""; Field<HiddenField>("evaluationTicket").Value = Protect(new Ticket { InternshipId = selection, Version = state.Version, CompanyVersion = current?.VersionId, Revision = revision, DecisionId = Guid.NewGuid().ToString("N") }); }
            }
            catch (EvaluationAuthorizationFailureException error)
            {
                ClearEditor(); BindStatistics(null, null, null, null);
                rows.Text = "<tr><td colspan=" + (Academic ? "5" : "3") + ">Access denied. No confidential records are displayed.</td></tr>";
                Response.StatusCode = 403; Response.TrySkipIisCustomErrors = true;
                availability.Text = AuthorizationMessage(error.ReasonCode);
            }
            catch (UnauthorizedAccessException)
            {
                ClearEditor(); BindStatistics(null, null, null, null);
                rows.Text = "<tr><td colspan=" + (Academic ? "5" : "3") + ">Access denied. No confidential records are displayed.</td></tr>";
                Response.StatusCode = 403; Response.TrySkipIisCustomErrors = true;
                availability.Text = Academic
                    ? "Access denied. The requested evaluation is outside your verified assignment scope, or its identity mapping no longer matches. Sign in again or contact the Coordinator to verify the assignment."
                    : "Access denied. A verified assigned supervisor is required.";
            }
            catch (EvaluationIntegrationUnavailableException error)
            {
                ClearEditor(); BindStatistics(null, null, null, null); availability.Text = error.Message;
                rows.Text = "<tr><td colspan=" + (Academic ? "5" : "3") + ">Authorized Firebase access is blocked. See the integration message above.</td></tr>";
            }
            catch (Exception)
            {
                // Keep entered form values on read failures; disable every mutation and hide stale rows.
                DisableActions(); BindStatistics(null, null, null, null);
                Field<Literal>("OperationMessage").Text = "";
                rows.Text = "<tr><td colspan=" + (Academic ? "5" : "3") + ">Evaluation records could not be loaded. Refresh to retry.</td></tr>";
                availability.Text = "Evaluation data could not be verified. No success is reported. This is a read or integration failure, not a confirmed empty student list.";
            }
        }
        private void ClearEditor()
        {
            foreach (var id in new[] { "FinalPercentage", "FinalComments", "RevisionReason" }) { var field = Find(this, id) as TextBox; if (field != null) { field.Text = ""; field.ReadOnly = true; } }
            foreach (var id in new[] { "SelectedName", "FinalStatus", "SubmissionTime", "ReviewStatus", "FinalOutcome", "OperationMessage", "InformationText", "InformationTitle", "CompanyPercentageDisplay", "ReviewTimestamp", "RecordedReviewReason", "EvaluationError" })
            { var field = Find(this, id) as Literal; if (field != null) field.Text = ""; }
            foreach (var id in new[] { "selectedInternship", "evaluationTicket", "activePanel", "reviewChoice" })
            { var field = Find(this, id) as HiddenField; if (field != null) field.Value = ""; }
            Field<CheckBox>("ConfirmAction").Checked = false;
            Field<Panel>("AcademicReviewPanel").Visible = false;
            DisableActions();
        }
        private string AuthorizationMessage(string reason)
        {
            switch (reason)
            {
                case "signed_identity_or_role": return "Access denied. Sign in using an active Academic Supervisor account; this session is missing, expired, or has a different role.";
                case "firebase_auth_uid": return "Access denied. Firebase Authentication could not verify an active account for this session.";
                case "profile_missing": return "Access denied. The authenticated account has no matching users/{uid} profile.";
                case "profile_inactive": return "Access denied. The Firebase user profile is not active.";
                case "profile_setup_incomplete": return "Access denied. The Firebase account setup is not complete.";
                case "profile_deletion_pending": return "Access denied. This account is pending deletion.";
                case "profile_role_mismatch": return "Access denied. The Firebase profile role does not match Academic Supervisor.";
                case "password_change_required": return "Access denied. Complete the required password change, then sign in again.";
                default: return "Access denied. The Academic Supervisor identity could not be verified.";
            }
        }
        private sealed class Ticket { public string InternshipId, Version, CompanyVersion, DecisionId; public bool Revision; }
        private string Protect(Ticket ticket) { return Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ticket)), "PercentageEvaluationV2", Session.SessionID, context.UserId, Academic.ToString())); }
        private Ticket ReadTicket(string id)
        {
            try { var value = JsonConvert.DeserializeObject<Ticket>(Encoding.UTF8.GetString(MachineKey.Unprotect(Convert.FromBase64String(Field<HiddenField>("evaluationTicket").Value), "PercentageEvaluationV2", Session.SessionID, context.UserId, Academic.ToString()))); if (value.InternshipId != id) throw new ConflictException(); return value; }
            catch { throw new ConflictException(); }
        }
        private bool SafeRevision(string id) { try { return ReadTicket(id).Revision; } catch { return false; } }
    }
    public sealed class EvaluationPageStatistics
    {
        public int Assigned { get; private set; }
        public int Drafts { get; private set; }
        public int AwaitingReview { get; private set; }
        public int Approved { get; private set; }
        public static EvaluationPageStatistics From(IEnumerable<PercentageEvaluation> authorizedEvaluations)
        {
            if (authorizedEvaluations == null) throw new ArgumentNullException("authorizedEvaluations");
            var statuses = authorizedEvaluations.Select(PercentageEvaluationWorkflow.Status).ToArray();
            return new EvaluationPageStatistics { Assigned = statuses.Length, Drafts = statuses.Count(value => value == "Draft"),
                AwaitingReview = statuses.Count(value => value == "Submitted"), Approved = statuses.Count(value => value == "Approved") };
        }
    }

}
