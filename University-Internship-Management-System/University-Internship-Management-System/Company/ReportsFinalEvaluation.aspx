<%@ Page Language="C#" AutoEventWireup="true" ViewStateEncryptionMode="Always" CodeBehind="ReportsFinalEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Company.ReportsFinalEvaluation" %>
<!DOCTYPE html>
<html lang="en" dir="ltr">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Reports &amp; Final Evaluation | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/company/reports-final-evaluation.css?v=20261021-role-pages-final" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
</head>
<body class="app-page uims-workspace company-evaluation" data-portal="company">
    <form id="form1" runat="server" novalidate="novalidate">
        <asp:ScriptManager ID="PageScripts" runat="server" EnablePartialRendering="true" />
        <div class="app-layout">
            <aside class="sidebar" id="sidebar" aria-label="Company navigation">
    <button type="button" class="sidebar-close" aria-label="Close navigation"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
    <a class="brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48"/><span class="brand-text"><strong>JUST</strong><span>Internships</span></span></a>
    <ul class="side-nav"><li><a href="CompanyDashboard.aspx"><i class="bi bi-house-door" aria-hidden="true"></i><span>Dashboard</span></a></li>
<li><a href="ManageOpportunities.aspx"><i class="bi bi-briefcase" aria-hidden="true"></i><span>Manage Opportunities</span></a></li>
<li><a href="ApplicationsReview.aspx"><i class="bi bi-inbox" aria-hidden="true"></i><span>Applications Review</span></a></li>
<li><a href="MyTrainees.aspx"><i class="bi bi-people" aria-hidden="true"></i><span>My Trainees</span></a></li>
<li><a href="ManageTasks.aspx"><i class="bi bi-list-check" aria-hidden="true"></i><span>Manage &amp; Review Tasks</span></a></li>
<li><a href="ReportsFinalEvaluation.aspx" class="active" aria-current="page"><i class="bi bi-clipboard-check" aria-hidden="true"></i><span>Reports &amp; Final Evaluation</span></a></li>
<li><a href="CompanySupervisorAccount.aspx"><i class="bi bi-person-circle" aria-hidden="true"></i><span>Supervisor Account</span></a></li></ul>
    <div class="side-divider"></div><ul class="side-nav"><li><a href="../Contact.aspx"><i class="bi bi-question-circle" aria-hidden="true"></i><span>Help &amp; contact</span></a></li><li><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right" aria-hidden="true"></i><span>Logout</span></asp:LinkButton></li></ul>
</aside><div class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main workspace-region"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">Verified company account</span></span></span></div></header>
                <asp:UpdatePanel ID="WorkspacePanel" runat="server" UpdateMode="Conditional"><ContentTemplate>
                <main class="app-content workspace-content" id="companyWorkspace">
                    <asp:HiddenField ID="ActiveView" runat="server" ClientIDMode="Static" Value="trainees" />
                    <asp:HiddenField ID="ReportMode" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="SearchValue" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="StatusValue" runat="server" ClientIDMode="Static" Value="all" />
                    <header class="workspace-header"><span class="workspace-eyebrow">Company supervisor workspace</span><h1>Reports &amp; Final Evaluation</h1><p class="workspace-muted">Manage trainee reports and complete final evaluations.</p></header>
                <asp:Panel ID="OverviewView" runat="server" ClientIDMode="Static" data-workspace-view="trainees">
                    <p class="demo-note">Live company-owned placements and progress reports. Evaluation submission is not yet enabled.</p>
                    <dl class="workspace-stats compact-summary">
                        <div class="workspace-stat"><dt>Total Trainees</dt><dd><span data-uims-report="TraineeTotal"><asp:Literal ID="TraineeTotal" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Pending Reports</dt><dd><span data-uims-report="PendingReports"><asp:Literal ID="PendingReports" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Pending Evaluations</dt><dd><span data-uims-report="DraftTotal"><asp:Literal ID="DraftTotal" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Submitted Evaluations</dt><dd><span data-uims-report="SubmittedTotal"><asp:Literal ID="SubmittedTotal" runat="server" /></span></dd></div>
                    </dl>
                    <section class="workspace-card" aria-labelledby="internTitle">
                        <div class="workspace-heading"><div><h2 id="internTitle" tabindex="-1">Trainees</h2><p>Your company’s verified placements</p></div></div>
                        <div class="trainee-filters">
                            <div><label class="workspace-label" for="traineeSearch">Search trainees</label><input id="traineeSearch" type="search" class="workspace-input" placeholder="Name, ID, university or major" /></div>
                            <div><label class="workspace-label" for="evaluationFilter">Evaluation status</label><select id="evaluationFilter" class="workspace-input"><option value="all">All statuses</option><option value="not-started">Not started</option><option value="draft">Draft</option><option value="ready">Ready for submission</option><option value="submitted">Submitted</option></select></div>
                            <button type="button" class="workspace-button secondary" data-clear-filters="true">Clear filters</button>
                        </div>
                        <div class="workspace-table-scroll" tabindex="0" role="region" aria-label="Assigned trainees table">
                            <table class="workspace-table trainee-table">
                                <thead><tr><th scope="col">Student</th><th scope="col">Report status</th><th scope="col">Evaluation status</th><th scope="col">Actions</th></tr></thead>
                                <tbody><asp:Repeater ID="Trainees" runat="server" EnableViewState="false" OnItemCommand="Trainees_ItemCommand"><ItemTemplate>
                                    <tr data-trainee-row="true" data-trainee-id='<%#: Eval("Id") %>' data-status='<%#: Eval("StatusKey") %>'>
                                        <td class="trainee-identity"><strong><%#: Eval("Name") %></strong><small><%#: Eval("StudentNumber") %> · <%#: Eval("University") %></small><small><%#: Eval("Major") %></small></td>
                                        <td data-label="Reports"><span class="workspace-badge"><%#: Eval("Reports") %></span></td>
                                        <td data-label="Evaluation"><span class="workspace-badge" data-evaluation-status="true"><%#: Eval("Status") %></span></td>
                                        <td class="trainee-action-cell"><div class="trainee-actions">
                                            <asp:LinkButton runat="server" Text="View Reports" CssClass="row-action" CommandName="Reports" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" data-student-id='<%# Eval("Id") %>' data-target-view="reports" aria-label='<%# "View Reports: " + Eval("Name") %>' />
                                            <asp:LinkButton runat="server" Text='<%# Eval("Action") %>' CssClass="row-action" CommandName="Evaluation" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" data-student-id='<%# Eval("Id") %>' data-target-view="evaluation" aria-label='<%# Eval("Action") + ": " + Eval("Name") %>' />
                                            <asp:LinkButton runat="server" Text="View Details" CssClass="row-action" CommandName="Details" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" data-student-id='<%# Eval("Id") %>' data-target-view="details" aria-label='<%# "View Details: " + Eval("Name") %>' />
                                        </div></td>
                                    </tr>
                                </ItemTemplate></asp:Repeater></tbody>
                            </table>
                        </div>
                        <p id="traineeSearchStatus" class="workspace-hint" role="status" aria-live="polite"></p>
                        <p id="noTraineeMatches" class="workspace-empty" hidden="hidden">No trainees match your search and status filter.</p>
                    </section>
                </asp:Panel>
                <asp:Panel ID="SelectInternMessage" runat="server" CssClass="workspace-empty" Visible="false">Choose an action beside a trainee to begin.</asp:Panel>
                <dialog id="traineeDialog" class="workspace-dialog" aria-label="Trainee workspace"><button type="button" class="popup-close" data-open-view="trainees" aria-label="Close trainee details">×</button><asp:Panel ID="InternDetails" runat="server" ClientIDMode="Static" Visible="false">
                    <div class="student-context">
                        <button type="button" class="workspace-button secondary" data-open-view="trainees">← Back to Trainees</button>
                        <div><span class="workspace-eyebrow">Selected trainee · demo</span><h2><span data-uims-report="SelectedName"><asp:Literal ID="SelectedName" runat="server" Mode="Encode" /></span></h2></div>
                        <div class="intern-selector"><asp:Label runat="server" AssociatedControlID="Intern" CssClass="workspace-label" Text="Change trainee" /><asp:DropDownList ID="Intern" runat="server" CssClass="workspace-input" AutoPostBack="true" OnSelectedIndexChanged="Intern_SelectedIndexChanged" /></div>
                    </div>
                    <nav class="section-links" aria-label="Trainee sections"><button type="button" data-open-view="reports">Reports</button><button type="button" data-open-view="evaluation">Evaluation</button><button type="button" data-open-view="details">Details &amp; progress</button></nav>
                    <asp:Panel ID="ReportsView" runat="server" ClientIDMode="Static" CssClass="workspace-card" data-workspace-view="reports" aria-labelledby="reportsTitle">
                        <div class="workspace-heading"><div><h2 id="reportsTitle" tabindex="-1">Internship reports</h2><p>Select a report to view its contents or add review feedback.</p></div></div>
                        <p class="workspace-hint">Live placement reports. University grading and evaluation submission are not configured.</p>
                        <asp:Panel ID="NoReports" runat="server" CssClass="workspace-empty">No submitted demonstration reports are available for this trainee.</asp:Panel>
                        <asp:Panel ID="ReportSection" runat="server">
                            <div class="workspace-table-scroll" tabindex="0" role="region" aria-label="Internship reports table">
                                <table class="workspace-table reports-table"><thead><tr><th scope="col">Report / actions</th><th scope="col">Submitted</th><th scope="col">Status</th><th scope="col">Attachment</th></tr></thead><tbody>
                                    <asp:Repeater ID="Reports" runat="server" EnableViewState="false" OnItemCommand="Reports_ItemCommand"><ItemTemplate>
                                        <tr><td><strong><%#: Eval("Title") %></strong><div class="trainee-actions"><asp:LinkButton runat="server" Text="View Report" CssClass="row-action" CommandName="Read" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" aria-label='<%# "View Report: " + Eval("Title") %>' /><asp:LinkButton runat="server" Text="Review / Add Feedback" CssClass="row-action" CommandName="Review" CommandArgument='<%# Eval("Id") %>' CausesValidation="false" aria-label='<%# "Review: " + Eval("Title") %>' /></div></td><td><%#: Eval("Submitted") %></td><td><span class="workspace-badge"><%#: Eval("ReviewStatus") %></span></td><td>No file attached</td></tr>
                                    </ItemTemplate></asp:Repeater>
                                </tbody></table>
                            </div>
                            <asp:Panel ID="ReportDetailsPanel" runat="server" ClientIDMode="Static" CssClass="report-detail-panel">
                                <div class="workspace-heading"><h3 tabindex="-1" id="reportDetailTitle">Report details</h3><button type="button" class="row-action" data-close-report="true">Close report</button></div>
<div class="review-grid">
                                <div class="workspace-field"><asp:Label runat="server" AssociatedControlID="Report" CssClass="workspace-label" Text="View submitted report" /><asp:DropDownList ID="Report" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Report_SelectedIndexChanged" CssClass="workspace-input" /></div>
                                <div class="workspace-field"><label for="reportScore" class="workspace-label">Report grade</label><input id="reportScore" class="workspace-input" value="University grading rules required" disabled="disabled" /></div>
                            </div>
                            <div class="report-excerpt"><h3>Report excerpt · demonstration</h3><p><span data-uims-report="ReportExcerpt"><asp:Literal ID="ReportExcerpt" runat="server" Mode="Encode" /></span></p><p><strong>Supervisor feedback:</strong> <span data-uims-report="ReportExistingFeedback"><asp:Literal ID="ReportExistingFeedback" runat="server" Mode="Encode" /></span></p><div class="workspace-actions"><button type="button" id="openReportLink" class="workspace-button secondary" disabled="disabled">Open submitted link</button><button type="button" id="downloadReportSummary" class="workspace-button secondary" disabled="disabled">Download submission summary (TXT)</button></div><span class="workspace-hint">No file or link was provided for this sample report.</span></div>
                            <asp:Panel ID="ReportFeedbackPanel" runat="server" ClientIDMode="Static">
<div class="workspace-field"><asp:Label runat="server" AssociatedControlID="ReportFeedback" CssClass="workspace-label" Text="Report feedback" /><asp:TextBox ID="ReportFeedback" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" CssClass="workspace-input" data-maxlength="2000" aria-describedby="reportFeedbackHint" /><span id="reportFeedbackHint" class="workspace-hint">Required for a review preview; maximum 2,000 characters. The actual report is unchanged.</span></div>
                            <div class="review-grid">
                                <div class="workspace-field"><asp:Label runat="server" AssociatedControlID="ReportOutcome" CssClass="workspace-label" Text="Proposed review outcome · demo only" /><asp:DropDownList ID="ReportOutcome" runat="server" CssClass="workspace-input"><asp:ListItem Value="reviewed">Mark reviewed</asp:ListItem><asp:ListItem Value="corrections">Request corrections</asp:ListItem></asp:DropDownList></div>
                                <div class="workspace-actions"><asp:Button ID="PreviewReport" runat="server" Text="Preview report review" CssClass="workspace-button secondary" OnClick="PreviewReport_Click" data-review-action="report" /></div>
                            </div>
                            <asp:Button ID="SaveReportReview" runat="server" Text="Save preview review" CssClass="workspace-button" OnClick="PreviewReport_Click" data-review-action="report" /><div class="workspace-feedback" role="status" aria-live="polite"><span data-uims-report="ReportPreview"><asp:Literal ID="ReportPreview" runat="server" Mode="Encode" /></span></div>
</asp:Panel>
                                <button type="button" class="workspace-button secondary" data-review-report="true">Review / Add Feedback</button>
                            </asp:Panel>
                        </asp:Panel>
                    </asp:Panel>
<asp:Panel ID="DetailsView" runat="server" ClientIDMode="Static" CssClass="workspace-card" data-workspace-view="details" aria-labelledby="performanceTitle">
                        <h2 id="performanceTitle" tabindex="-1">Internship details &amp; progress</h2><p class="workspace-hint">Internship status: In progress · demo. Completion requires Coordinator approval.</p>
                        <p class="workspace-muted">Tasks and assessments are separate from the confidential final evaluation below.</p>
                        <dl class="workspace-stats performance-stats">
                            <div class="workspace-stat"><dt>Assigned / completed / pending tasks</dt><dd class="compact-stat"><span data-uims-report="TaskCount"><asp:Literal ID="TaskCount" runat="server" /></span></dd></div>
                            <div class="workspace-stat"><dt>Evaluated assessments</dt><dd><span data-uims-report="AssessmentCount"><asp:Literal ID="AssessmentCount" runat="server" /></span></dd></div>
                            <div class="workspace-stat"><dt>Approved / required training hours</dt><dd class="compact-stat"><span data-uims-report="Hours"><asp:Literal ID="Hours" runat="server" /></span></dd></div>
                            <div class="workspace-stat"><dt>Approved-hour progress</dt><dd><span data-uims-report="ProgressPercent"><asp:Literal ID="ProgressPercent" runat="server" /></span>%</dd></div>
                        </dl>
                        <progress ID="TrainingProgress" runat="server" max="100" value="0" aria-label="Approved training hours progress"></progress>
                        <p class="workspace-hint">Only explicitly approved sample time entries count. Task completion does not approve hours; the Coordinator has not approved completion.</p>
                        <div class="workspace-empty"><span data-uims-report="TaskSummary"><asp:Literal ID="TaskSummary" runat="server" Mode="Encode" /></span></div>
                        <h3 class="mt-4">Assessment summary</h3>
                        <p class="workspace-hint">Assessment creation is not implemented in this checkout. These illustrative results do not define an official scale. Academic feedback cannot replace the company grade.</p>
                        <asp:Panel ID="NoAssessments" runat="server" CssClass="workspace-empty">No demonstration assessments are available for this trainee.</asp:Panel>
                        <div class="workspace-table-scroll" tabindex="0" role="region" aria-label="Assessment summary table">
                            <table class="workspace-table"><thead><tr><th scope="col">Assessment / date</th><th scope="col">Grade / maximum</th><th scope="col">Status</th><th scope="col">Company feedback</th><th scope="col">Academic feedback</th></tr></thead><tbody><asp:Repeater ID="Assessments" runat="server" EnableViewState="false"><ItemTemplate><tr><td><strong><%#: Eval("Title") %></strong><small><%#: Eval("Date") %></small></td><td><%#: Eval("Grade") %></td><td>Published · demo</td><td><%#: Eval("CompanyFeedback") %></td><td><%#: Eval("AcademicFeedback") %></td></tr></ItemTemplate></asp:Repeater></tbody></table>
                        </div>
                    </asp:Panel>
<asp:Panel ID="EvaluationView" runat="server" ClientIDMode="Static" CssClass="workspace-card" data-workspace-view="evaluation" aria-labelledby="finalTitle">
                        <div class="workspace-heading"><div><h2 id="finalTitle" tabindex="-1">Final company evaluation</h2><p>For the assigned Academic Supervisor; not part of the student's published exam results.</p></div><asp:Label ID="FinalStatus" runat="server" ClientIDMode="Static" CssClass="workspace-badge" /></div>
                        <div class="workspace-note"><strong>Illustrative criteria only.</strong> No university-approved evaluation method is configured. Scores and calculated results below are demonstrations, never an official final result.</div>
                        <div class="workspace-field"><asp:Label runat="server" AssociatedControlID="CriteriaConfig" CssClass="workspace-label" Text="Demonstration scoring configuration" /><asp:DropDownList ID="CriteriaConfig" runat="server" AutoPostBack="true" OnSelectedIndexChanged="CriteriaConfig_SelectedIndexChanged" CssClass="workspace-input"><asp:ListItem Value="points">Example A · sum of criterion points</asp:ListItem><asp:ListItem Value="weighted">Example B · normalized weighted index</asp:ListItem></asp:DropDownList><span class="workspace-hint">Changing configuration clears unsaved scores. Neither example is an approved university policy.</span></div>
                        <asp:Repeater ID="Criteria" runat="server" EnableViewState="false"><ItemTemplate>
                            <div class="criterion-row" data-criterion="true" data-max='<%#: Eval("Maximum", "{0:0.##}") %>' data-min='<%#: Eval("Minimum", "{0:0.##}") %>' data-weight='<%#: Eval("Weight", "{0:0.##}") %>'>
                                <div><h3><%#: Eval("Name") %></h3><p class="workspace-hint"><%#: Eval("Description") %></p><span class="workspace-hint">Range: <%#: Eval("Minimum", "{0:0.##}") %>–<%#: Eval("Maximum", "{0:0.##}") %> · Weight: <%#: Eval("WeightLabel") %></span></div>
                                <div><asp:Label runat="server" AssociatedControlID="Score" CssClass="workspace-label" Text="Earned score" /><asp:TextBox ID="Score" runat="server" TextMode="Number" aria-label='<%# "Score: " + Eval("Name") %>' CssClass="workspace-input criterion-score" min='<%# Eval("Minimum", "{0:0.##}") %>' max='<%# Eval("Maximum", "{0:0.##}") %>' step="0.01" /></div>
                                <details class="criterion-feedback"><summary>Criterion feedback (optional)</summary><asp:Label runat="server" AssociatedControlID="CriterionFeedback" CssClass="workspace-label" Text='<%# "Feedback: " + Eval("Name") %>' /><asp:TextBox ID="CriterionFeedback" runat="server" TextMode="MultiLine" Rows="2" CssClass="workspace-input" data-maxlength="1000" /></details>
                            </div>
                        </ItemTemplate></asp:Repeater>
                        <p class="workspace-hint">Criterion feedback is optional (up to 1,000 characters each). All scores and general comments are required for a complete preview. Incomplete drafts are allowed.</p>
                        <div class="workspace-field"><asp:Label runat="server" AssociatedControlID="FinalComments" CssClass="workspace-label" Text="General evaluation comments" /><asp:TextBox ID="FinalComments" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="4" CssClass="workspace-input" data-maxlength="3000" aria-describedby="finalCommentHint" /><span id="finalCommentHint" class="workspace-hint">Maximum 3,000 characters. Saved demo drafts survive selection changes in this page only; a new visit restores the original sample data.</span></div>
                        <div id="demoCalculation" class="demo-calculation" role="status" aria-live="polite">Enter scores to calculate an illustrative result.</div>
                        <div class="workspace-error" role="alert"><asp:Literal ID="EvaluationError" runat="server" Mode="Encode" /></div>
                        <div class="workspace-actions evaluation-actions">
                            <asp:Button ID="CancelEvaluation" runat="server" Text="Cancel" CssClass="workspace-button secondary" OnClick="CancelEvaluation_Click" CausesValidation="false" />
                            <asp:Button ID="SaveDraft" runat="server" Text="Save Draft" CssClass="workspace-button secondary" OnClick="SaveDraft_Click" data-review-action="draft" />
                            <asp:Button ID="PreviewDraft" runat="server" Text="Preview" CssClass="workspace-button" OnClick="PreviewDraft_Click" data-review-action="evaluation" />
                            <button ID="ReviewSubmission" runat="server" ClientIDMode="Static" type="button" class="workspace-button secondary" >Review preview submission</button>
                        </div>
                        <div id="finalPreviewMessage" class="workspace-feedback" role="status" aria-live="polite"><asp:Literal ID="FinalPreview" runat="server" Mode="Encode" /></div>
                        <asp:Panel ID="EvaluationPreview" runat="server" ClientIDMode="Static" Visible="false" CssClass="evaluation-preview" role="region" aria-label="Evaluation preview">
                            <h3>Evaluation review · demonstration</h3><p><asp:Literal ID="PreviewResult" runat="server" Mode="Encode" /></p>
                            <dl class="preview-details"><asp:Repeater ID="PreviewCriteria" runat="server" EnableViewState="false"><ItemTemplate><div><dt><%#: Eval("Name") %> · <%#: Eval("Score") %></dt><dd><%#: Eval("Feedback") %></dd></div></ItemTemplate></asp:Repeater></dl>
                            <p class="preview-comments"><span data-uims-report="PreviewComments"><asp:Literal ID="PreviewComments" runat="server" Mode="Encode" /></span></p>
                        </asp:Panel>
                        <div class="submission-route">Assigned Academic Supervisor: <span data-uims-report="AcademicRecipient"><asp:Literal ID="AcademicRecipient" runat="server" Mode="Encode" /></span><br />Submission date: <span data-uims-report="SubmissionDate"><asp:Literal ID="SubmissionDate" runat="server" Mode="Encode" /></span></div>
                        <details class="workflow-help"><summary>Submission and privacy requirements</summary><p>Real submission requires approved criteria, verified assignments and persistent duplicate protection. Confidential forms require server authorization and Firebase rules. Submitted evaluations remain read-only.</p></details>
                    </asp:Panel>
                </asp:Panel></dialog>
                <dialog id="submissionDialog" class="workspace-dialog profile-dialog submission-dialog" aria-labelledby="submissionTitle" aria-describedby="submissionDescription">
                    <h2 id="submissionTitle">Review final submission</h2>
                    <p id="submissionDescription">A real final submission sends the confidential evaluation to the assigned Academic Supervisor and locks further editing. Review all scores and comments first.</p>
                    <div class="workspace-note"><strong>Preview submission only.</strong> This locks the sample evaluation in this page visit only. Nothing is sent to Firebase or an Academic Supervisor. University-approved criteria and verified permissions are not connected.</div>
                    <div class="workspace-actions"><asp:Button ID="ConfirmSubmission" runat="server" Text="Confirm preview submission" CssClass="workspace-button" OnClick="ConfirmSubmission_Click" data-review-action="evaluation" /><button type="button" class="workspace-button secondary" id="closeSubmissionDialog">Back to evaluation</button></div>
                </dialog>                </main>
                </ContentTemplate></asp:UpdatePanel>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="<%= ResolveUrl("~/Js_project/Company/reports-final-evaluation.js?v=20261021-role-pages-final") %>" defer="defer"></script>
<script src="../Js_project/Public/tab-session.js" defer="defer"></script>
</body>
</html>
