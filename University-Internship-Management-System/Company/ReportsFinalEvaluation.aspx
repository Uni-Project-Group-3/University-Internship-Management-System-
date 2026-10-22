<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" ViewStateEncryptionMode="Always" CodeBehind="ReportsFinalEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Company.ReportsFinalEvaluation" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Reports &amp; Final Evaluation | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/company/reports-final-evaluation.css?v=20261022-statistics" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
</head>
<body class="app-page uims-workspace company-evaluation" data-demo-role="company">
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
<li><a href="WeeklyReports.aspx"><i class="bi bi-file-earmark-word" aria-hidden="true"></i><span>Weekly Reports</span></a></li>
<li><a href="ReportsFinalEvaluation.aspx" class="active" aria-current="page"><i class="bi bi-clipboard-check" aria-hidden="true"></i><span>Reports &amp; Final Evaluation</span></a></li>
<li><a href="CompanySupervisorAccount.aspx"><i class="bi bi-person-circle" aria-hidden="true"></i><span>Supervisor Account</span></a></li></ul>
    <div class="side-divider"></div><ul class="side-nav"><li><a href="../Contact.aspx"><i class="bi bi-question-circle" aria-hidden="true"></i><span>Help &amp; contact</span></a></li><li><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right" aria-hidden="true"></i><span>Logout</span></asp:LinkButton></li></ul>
</aside><div class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main workspace-region"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">Company Supervisor · Firebase read-only</span></span></span></div></header>
                
<main class="app-content workspace-content evaluation-main" id="companyWorkspace">
<header class="workspace-header evaluation-page-header"><h1>Reports &amp; Final Evaluation</h1><p>View assigned trainees and their company evaluation records from Firebase.</p></header>
<p class="workspace-note evaluation-inline-note" role="status"><asp:Literal ID="DraftAvailability" runat="server" Mode="Encode" /></p>
<asp:HiddenField ID="selectedInternship" runat="server" /><asp:HiddenField ID="evaluationTicket" runat="server" EnableViewState="false" /><asp:HiddenField ID="activePanel" runat="server" ClientIDMode="Static" />
<section class="row g-2 mb-3 company-review-stats" aria-label="Evaluation statistics"><div class="col-12 col-sm-6 col-xl-3"><article class="company-review-stat h-100"><h2>Assigned Placements</h2><strong data-company-stat="assigned"><%= AssignedTraineesCount %></strong></article></div><div class="col-12 col-sm-6 col-xl-3"><article class="company-review-stat h-100"><h2>Final Evaluations</h2><strong data-company-stat="drafts"><%= CompanyDraftCount %></strong></article></div><div class="col-12 col-sm-6 col-xl-3"><article class="company-review-stat h-100"><h2>Midterm Evaluations</h2><strong data-company-stat="awaiting"><%= AwaitingReviewCount %></strong></article></div><div class="col-12 col-sm-6 col-xl-3"><article class="company-review-stat h-100"><h2>Without Evaluation</h2><strong data-company-stat="approved"><%= AcademicApprovalCount %></strong></article></div></section>
<section class="workspace-card evaluation-panel"><div class="workspace-heading evaluation-panel-heading"><h2>Assigned trainees</h2></div><asp:Literal ID="OverviewCounts" runat="server" Visible="false" Mode="Encode" /><div class="workspace-filter-grid evaluation-toolbar"><label class="workspace-field">Search trainees<input type="search" id="traineeSearch" class="workspace-input" /></label><label class="workspace-field">Firebase evaluation<select id="evaluationFilter" class="workspace-input"><option value="">All evaluations</option><option>Final evaluation</option><option>Midterm evaluation</option><option>No evaluation</option></select></label><button type="button" class="workspace-button secondary" id="clearEvaluationFilters">Clear filters</button></div><div class="workspace-table-scroll evaluation-table-wrap"><table class="table workspace-table"><thead><tr><th>Student / placement</th><th>Evaluation from Firebase</th><th>Source</th></tr></thead><tbody><asp:Literal ID="traineeRows" runat="server" /></tbody></table></div></section>
<div class="modal fade" id="informationDialog" tabindex="-1" aria-labelledby="informationTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered modal-dialog-scrollable"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="informationTitle"><asp:Literal ID="InformationTitle" runat="server" Mode="Encode" /></h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close information"></button></div><div class="modal-body"><asp:Literal ID="InformationText" runat="server" Mode="Encode" /></div></div></div></div>
<div class="modal fade" id="evaluationDialog" tabindex="-1" aria-labelledby="evaluationTitle" aria-hidden="true"><div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content">
<div class="modal-header"><h2 class="modal-title fs-5" id="evaluationTitle">Company Final Evaluation — <asp:Literal ID="SelectedName" runat="server" Mode="Encode" /></h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close evaluation"></button></div>
<div class="modal-body"><p>Status: <strong><asp:Literal ID="FinalStatus" runat="server" Mode="Encode" /></strong></p><p>Submitted UTC: <asp:Literal ID="SubmissionTime" runat="server" Mode="Encode" /></p>
<div class="workspace-field evaluation-field"><label for="FinalPercentage" class="form-label">Final Evaluation Percentage (0–100)</label><asp:TextBox ID="FinalPercentage" runat="server" ClientIDMode="Static" EnableViewState="false" CssClass="form-control" TextMode="Number" min="0" max="100" step="any" ValidateRequestMode="Disabled" /></div>
<div class="workspace-field evaluation-field"><label for="FinalComments" class="form-label">Company Supervisor comments</label><asp:TextBox ID="FinalComments" runat="server" ClientIDMode="Static" EnableViewState="false" CssClass="form-control" TextMode="MultiLine" Rows="4" ValidateRequestMode="Disabled" /></div>
<p class="workspace-hint"><asp:Literal ID="FinalOutcome" runat="server" Mode="Encode" /></p><p class="workspace-hint">Task grades are separate. Partial Drafts are allowed. Save changes before confirming submission.</p>
<p>Academic review: <asp:Literal ID="ReviewStatus" runat="server" Mode="Encode" /></p>
<asp:Panel ID="AcademicReviewPanel" runat="server"><label for="RevisionReason" class="form-label">Reason for Request Revision (required)</label><asp:TextBox ID="RevisionReason" runat="server" ClientIDMode="Static" CssClass="form-control" TextMode="MultiLine" Rows="3" ValidateRequestMode="Disabled" /></asp:Panel>
<div class="form-check mt-3"><asp:CheckBox ID="ConfirmAction" runat="server" Text="I confirm the final submission or academic review decision." /></div>
<p class="workspace-error text-danger" role="alert"><asp:Literal ID="EvaluationError" runat="server" Mode="Encode" /></p><p class="workspace-feedback text-success" role="status"><asp:Literal ID="OperationMessage" runat="server" Mode="Encode" /></p>
</div><div class="modal-footer"><asp:Button ID="SaveDraft" runat="server" Text="Save Draft" CssClass="btn btn-secondary" OnClick="SaveDraft_Click" /><asp:Button ID="ConfirmSubmission" runat="server" Text="Submit Evaluation" CssClass="btn btn-primary" OnClick="ConfirmSubmission_Click" /><asp:Button ID="ApproveReview" runat="server" Text="Approve Evaluation" CssClass="btn btn-primary" OnClick="ApproveReview_Click" /><asp:Button ID="RequestRevision" runat="server" Text="Request Revision" CssClass="btn btn-outline-secondary" OnClick="RequestRevision_Click" /><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Close</button></div>
</div></div></div></main>

            </div>
        </div>
    </form>

<script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/final-evaluation-v2.js?v=3" defer="defer"></script>
</body>
</html>
