<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" ViewStateEncryptionMode="Always" CodeBehind="ReportsFinalEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Company.ReportsFinalEvaluation" %>
<%@ Register Src="~/Shared/CompanySidebar.ascx" TagPrefix="uims" TagName="CompanySidebar" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" media="print" data-evaluation-external="true" />
    <noscript><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" /></noscript>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Reports &amp; Final Evaluation | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/evaluation-colors.css?v=20261024-fast-paint" rel="stylesheet" />
    <link runat="server" href="~/Assets/company/reports-final-evaluation.css?v=20261024-remove-system-status" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/evaluation-workspace.css?v=20261023" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" media="print" data-evaluation-external="true" />
    <link href="../Assets/shared/workflow-ui.css?v=20261025-final-cards" rel="stylesheet" />
    <script>
        document.querySelectorAll('link[data-evaluation-external]').forEach(function (link) {
            function activate() { link.media = 'all'; }
            link.addEventListener('load', activate);
            if (link.sheet) activate();
        });
    </script>
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>

</head>
<body class="coordinator-banner app-page uims-workspace company-evaluation" data-demo-role="company">
    <form id="form1" runat="server" novalidate="novalidate">
        <asp:ScriptManager ID="PageScripts" runat="server" EnablePartialRendering="true" />
        <div class="app-layout">
            <uims:CompanySidebar ID="companySidebar" runat="server" SidebarId="sidebar" /><div class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main workspace-region"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">Company Supervisor &middot; Firebase read-only</span></span></span></div></header>
                
<main class="app-content workspace-content evaluation-main" id="companyWorkspace">
<section class="uims-page-hero" aria-labelledby="companyEvaluationTitle">
<div class="uims-hero-copy"><p class="uims-hero-eyebrow">JUST / COMPANY SUPERVISOR WORKSPACE</p><h1 id="companyEvaluationTitle">Reports &amp; Final Evaluation</h1><p>Manage trainee reports, submit final evaluations, and track academic review status.</p></div>
<section class="company-review-stats uims-hero-stats" aria-label="Authorized evaluation statistics"><article class="uims-hero-stat"><h2>Assigned Trainees</h2><strong data-company-stat="assigned"><%= AssignedTraineesCount %></strong></article><article class="uims-hero-stat"><h2>Company Drafts</h2><strong data-company-stat="drafts"><%= CompanyDraftCount %></strong></article><article class="uims-hero-stat"><h2>Awaiting Academic Review</h2><strong data-company-stat="awaiting"><%= AwaitingReviewCount %></strong></article><article class="uims-hero-stat"><h2>Academic Approvals</h2><strong data-company-stat="approved"><%= AcademicApprovalCount %></strong></article></section></section>
<asp:Literal ID="DraftAvailability" runat="server" Mode="Encode" Visible="false" />
<asp:HiddenField ID="selectedInternship" runat="server" /><asp:HiddenField ID="evaluationTicket" runat="server" EnableViewState="false" /><asp:HiddenField ID="activePanel" runat="server" ClientIDMode="Static" />

<section class="workspace-card evaluation-panel"><div class="workspace-heading evaluation-panel-heading"><h2>Assigned trainees</h2></div><asp:Literal ID="OverviewCounts" runat="server" Visible="false" Mode="Encode" /><div class="workspace-filter-grid evaluation-toolbar"><label class="workspace-field">Search trainees<input type="search" id="traineeSearch" class="workspace-input" /></label><label class="workspace-field">Evaluation status<select id="evaluationFilter" class="workspace-input"><option value="">All statuses</option><option>Not Started</option><option>Draft</option><option>Submitted</option><option>Revision Requested</option><option>Approved</option></select></label><button type="button" class="workspace-button secondary" id="clearEvaluationFilters">Clear filters</button></div><div class="workspace-table-scroll evaluation-table-wrap"><table class="table workspace-table trainee-table"><thead><tr><th>Student / placement</th><th>Evaluation from Firebase</th><th>Actions</th></tr></thead><tbody><asp:Literal ID="traineeRows" runat="server" /></tbody></table></div></section>
<div class="modal fade company-info-modal" id="informationDialog" tabindex="-1" aria-labelledby="informationTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered modal-dialog-scrollable"><div class="modal-content"><div class="modal-header company-modal-header"><div><p class="company-modal-eyebrow">JUST / COMPANY SUPERVISOR</p><h2 class="modal-title fs-5" id="informationTitle"><asp:Literal ID="InformationTitle" runat="server" Mode="Encode" /></h2></div><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close information"></button></div><div class="modal-body company-details-body" id="informationContent"><asp:Literal ID="InformationText" runat="server" Mode="Encode" /></div></div></div></div>
<div class="modal fade company-evaluation-modal" id="evaluationDialog" tabindex="-1" aria-labelledby="evaluationTitle" aria-hidden="true"><div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content">
<div class="modal-header company-modal-header"><div><p class="company-modal-eyebrow">JUST / COMPANY SUPERVISOR WORKSPACE</p><h2 class="modal-title fs-5" id="evaluationTitle">Company Final Evaluation</h2><p class="company-modal-student"><asp:Literal ID="SelectedName" runat="server" Mode="Encode" /></p></div><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close evaluation"></button></div>
<div class="modal-body company-evaluation-body">
<section class="company-modal-section company-evaluation-summary" aria-labelledby="companyEvaluationDetailsHeading"><div class="company-section-heading"><div><p class="company-modal-eyebrow">RECORD</p><h3 id="companyEvaluationDetailsHeading">Evaluation Details</h3></div><span class="company-status-badge" id="companyStatusBadge"><asp:Literal ID="FinalStatus" runat="server" Mode="Encode" /></span></div><dl class="company-summary-grid"><div><dt>Submitted</dt><dd><asp:Literal ID="SubmissionTime" runat="server" Mode="Encode" /></dd></div><div><dt>Academic review</dt><dd><asp:Literal ID="ReviewStatus" runat="server" Mode="Encode" /></dd></div></dl><p class="workspace-hint company-outcome"><asp:Literal ID="FinalOutcome" runat="server" Mode="Encode" /></p></section>
<section class="company-modal-section" aria-labelledby="companyPercentageHeading"><div class="company-section-heading"><div><p class="company-modal-eyebrow">FINAL SCORE</p><h3 id="companyPercentageHeading">Final Evaluation Percentage</h3></div></div><div class="workspace-field evaluation-field"><label for="FinalPercentage" class="form-label">Percentage</label><div class="company-percentage-control"><asp:TextBox ID="FinalPercentage" runat="server" ClientIDMode="Static" EnableViewState="false" CssClass="form-control company-modal-input" TextMode="Number" min="0" max="100" step="any" inputmode="decimal" ValidateRequestMode="Disabled" /><span aria-hidden="true">%</span></div><small class="company-field-help">Enter the final company score in the range 0–100. Partial drafts may be saved.</small></div></section>
<section class="company-modal-section" aria-labelledby="companyFeedbackHeading"><div class="company-section-heading"><div><p class="company-modal-eyebrow">SUPERVISOR FEEDBACK</p><h3 id="companyFeedbackHeading">Comments</h3></div></div><div class="workspace-field evaluation-field"><label for="FinalComments" class="form-label">Company Supervisor comments</label><asp:TextBox ID="FinalComments" runat="server" ClientIDMode="Static" EnableViewState="false" CssClass="form-control company-modal-input company-comments" TextMode="MultiLine" Rows="4" ValidateRequestMode="Disabled" /></div></section>
<p class="workspace-hint company-separate-grades">Task grades are separate from the Company Final Evaluation.</p>
<asp:Panel ID="AcademicReviewPanel" runat="server" CssClass="company-modal-section company-revision-panel"><div class="company-section-heading"><div><p class="company-modal-eyebrow">ACADEMIC REVIEW</p><h3>Request Revision</h3></div></div><label for="RevisionReason" class="form-label">Reason for Request Revision (required)</label><asp:TextBox ID="RevisionReason" runat="server" ClientIDMode="Static" CssClass="form-control company-modal-input company-comments" TextMode="MultiLine" Rows="3" ValidateRequestMode="Disabled" /></asp:Panel>
<div class="company-confirmation"><div class="form-check"><asp:CheckBox ID="ConfirmAction" runat="server" Text="I confirm the final submission or academic review decision." /></div></div>
<p class="workspace-error company-form-message" role="alert"><asp:Literal ID="EvaluationError" runat="server" Mode="Encode" /></p><p class="workspace-feedback company-form-message" role="status"><asp:Literal ID="OperationMessage" runat="server" Mode="Encode" /></p>
</div><div class="modal-footer company-modal-footer"><button type="button" class="btn company-modal-button company-close-button" data-bs-dismiss="modal">Close</button><asp:Button ID="SaveDraft" runat="server" Text="Save Draft" CssClass="btn company-modal-button company-save-button" OnClick="SaveDraft_Click" /><asp:Button ID="ConfirmSubmission" runat="server" Text="Submit Evaluation" CssClass="btn company-modal-button company-submit-button" OnClick="ConfirmSubmission_Click" /><asp:Button ID="ApproveReview" runat="server" Text="Approve Evaluation" CssClass="btn company-modal-button company-submit-button" OnClick="ApproveReview_Click" /><asp:Button ID="RequestRevision" runat="server" Text="Request Revision" CssClass="btn company-modal-button company-revision-button" OnClick="RequestRevision_Click" /></div>
</div></div></div></main>

            </div>
        </div>
    </form>

<script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/final-evaluation-v2.js?v=20261023-local-fixes" defer="defer"></script>
<script src="../Js_project/final-report-summary.js?v=20261023"></script>
</body>
</html>
