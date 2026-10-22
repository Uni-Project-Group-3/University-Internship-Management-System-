<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" ViewStateEncryptionMode="Always" CodeBehind="FinalAcademicEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Academic.FinalAcademicEvaluation" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Company Final Evaluation Review | JUST UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" /><link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />

    <link runat="server" href="~/Assets/academic/final-academic-evaluation.css?v=20261022-review-ui" rel="stylesheet" />
    
</head>
<body class="dashboard-direct app-page evaluation-page" data-demo-role="academic">
    <form id="form1" runat="server" novalidate="novalidate">
                                <div class="app-layout">
                        <aside class="dash-sidebar" id="dashboardSidebar" aria-label="Academic navigation">
                <a href="<%= ResolveUrl("~/Academic/AcademicDashboard.aspx") %>" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text">
                        <strong>JUST</strong>
                        <span>Internships</span>
                    </span>
                </a>
                <button type="button" class="dash-sidebar-close" data-sidebar-close aria-label="Close navigation">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="m6 6 12 12M6 18 18 6"/>
                    </svg>
                </button>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav" aria-label="Academic pages">
<li><a href="AcademicDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="StudentProgressMonitor.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Student Progress Monitor</span></a></li>
<li><a href="FinalAcademicEvaluation.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Academic Evaluation</span></a></li>
<li><a href="AcademicSupervisorAccount.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Account</span></a></li>
</ul>
                    <div class="dash-side-divider">
                    </div>
                    <ul class="dash-side-nav">
                        
                        <li>
                            <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="9"/>
                                    <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                </svg>
                                <span>Help</span>
                            </a>
                        </li>
                    </ul>
                </nav>
                <div class="dash-side-promo">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <rect x="5" y="3" width="14" height="18" rx="2"/>
                        <path d="m8 12 3 3 5-6"/>
                    </svg>
                    <p>Support your students.<br />Review their training journey.</p>
                    <a href="FinalAcademicEvaluation.aspx">Review evaluations &rarr;</a>
                </div>
            </aside>
            <button type="button" class="dash-sidebar-backdrop" data-sidebar-close aria-label="Close navigation" tabindex="-1" hidden>
            </button>
            <div class="dash-main">
                <header class="dash-topbar">
                    <button type="button" class="dash-menu-btn" id="dashboardMenuToggle" aria-controls="dashboardSidebar" aria-expanded="false" aria-label="Open navigation">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <path d="M4 6h16M4 12h16M4 18h16"/>
                        </svg>
                    </button>
                    <label class="dash-search">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <circle cx="10" cy="10" r="6"/>
                            <path d="m15 15 6 6"/>
                        </svg>
                        <span class="dash-sr-only">Search this page</span>
                        <input type="search" id="dashboardSearch" data-search-target="evaluationSearch" placeholder="Search for students or companies..." />
                    </label>
                    <div class="dash-topbar-actions">
                        <button type="button" class="dash-icon-btn" disabled aria-label="Notifications are not connected" title="Notifications are not connected">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                <path d="M5 17h14l-2-3V9a5 5 0 0 0-10 0v5l-2 3Zm5 3h4"/>
                            </svg>
                        </button>
                        <details class="dash-profile">
                            <summary class="dash-user-chip">
                                <span class="dash-avatar">AS</span>
                                <span class="dash-user-name">Academic Supervisor<span class="dash-role">Academic workspace</span>
                            </span>
                            <span aria-hidden="true">&#8964;</span>
                        </summary>
                        <div class="dash-profile-menu">
                            <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="9"/>
                                    <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                </svg>Help &amp; contact</a>
                                <a href="<%= ResolveUrl("~/Auth/Login.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="8" r="3"/>
                                        <path d="M5 21v-3a7 7 0 0 1 14 0v3"/>
                                    </svg>Sign in</a>
                                </div>
                            </details>
                        </div>
                    </header>
            
<main class="app-content workspace-content evaluation-main" id="companyWorkspace">
<header class="workspace-header evaluation-page-header"><h1>Company Final Evaluation Review</h1><p>Company percentage → Academic review → Coordinator completion.</p></header>
<p class="workspace-note evaluation-inline-note" role="status"><asp:Literal ID="DraftAvailability" runat="server" Mode="Encode" /></p>
<asp:HiddenField ID="selectedInternship" runat="server" /><asp:HiddenField ID="evaluationTicket" runat="server" EnableViewState="false" /><asp:HiddenField ID="activePanel" runat="server" ClientIDMode="Static" />
<section class="evaluation-stats academic-review-stats" aria-label="Evaluation statistics"><article><div>Assigned Students</div><strong data-stat="AssignedStudentCount"><asp:Literal ID="AssignedStudentCount" runat="server" Mode="Encode" Text="—" /></strong></article><article><div>Company Drafts</div><strong data-stat="CompanyDraftCount"><asp:Literal ID="CompanyDraftCount" runat="server" Mode="Encode" Text="—" /></strong></article><article><div>Awaiting Academic Review</div><strong data-stat="AwaitingReviewCount"><asp:Literal ID="AwaitingReviewCount" runat="server" Mode="Encode" Text="—" /></strong></article><article><div>Academic Approvals</div><strong data-stat="AcademicApprovalCount"><asp:Literal ID="AcademicApprovalCount" runat="server" Mode="Encode" Text="—" /></strong></article></section>
<section class="workspace-card evaluation-panel"><div class="workspace-heading evaluation-panel-heading"><h2>Assigned trainees</h2></div><div class="workspace-filter-grid evaluation-toolbar"><label class="workspace-field">Search trainees<input type="search" id="traineeSearch" class="workspace-input" /></label><label class="workspace-field">Evaluation status<select id="evaluationFilter" class="workspace-input"><option value="">All statuses</option><option>Not Started</option><option>Draft</option><option>Submitted</option><option>Revision Requested</option><option>Approved</option></select></label><button type="button" class="workspace-button secondary" id="clearEvaluationFilters">Clear filters</button></div><div class="workspace-table-scroll evaluation-table-wrap"><table class="table workspace-table"><thead><tr><th>Student / internship</th><th>Evaluation status</th><th>Actions</th></tr></thead><tbody><asp:Literal ID="traineeRows" runat="server" /></tbody></table></div></section>
<div class="modal fade" id="informationDialog" tabindex="-1" aria-labelledby="informationTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered modal-dialog-scrollable"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="informationTitle"><asp:Literal ID="InformationTitle" runat="server" Mode="Encode" /></h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close information"></button></div><div class="modal-body"><asp:Literal ID="InformationText" runat="server" Mode="Encode" /></div></div></div></div>
<div class="modal fade" id="evaluationDialog" tabindex="-1" aria-labelledby="evaluationTitle" aria-hidden="true"><div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content">
<div class="modal-header"><h2 class="modal-title fs-5" id="evaluationTitle">Company Final Evaluation — <asp:Literal ID="SelectedName" runat="server" Mode="Encode" /></h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close evaluation"></button></div>
<div class="modal-body"><p>Status: <strong><asp:Literal ID="FinalStatus" runat="server" Mode="Encode" /></strong></p><p>Submitted UTC: <asp:Literal ID="SubmissionTime" runat="server" Mode="Encode" /></p>
<div class="academic-percentage-summary"><span>Company Final Percentage</span><strong id="FinalPercentageDisplay" dir="ltr" lang="en"><asp:Literal ID="CompanyPercentageDisplay" runat="server" Mode="Encode" /></strong></div><asp:TextBox ID="FinalPercentage" runat="server" Visible="false" />
<div class="workspace-field evaluation-field"><label for="FinalComments" class="form-label">Company Supervisor comments (read-only)</label><asp:TextBox ID="FinalComments" runat="server" ClientIDMode="Static" EnableViewState="false" CssClass="form-control" TextMode="MultiLine" Rows="4" ReadOnly="true" ValidateRequestMode="Disabled" /></div>
<p class="workspace-hint"><asp:Literal ID="FinalOutcome" runat="server" Mode="Encode" /></p><p class="workspace-hint">Company task grades are separate from this final evaluation. Academic review does not change the Company percentage.</p>
<p>Academic review: <strong><asp:Literal ID="ReviewStatus" runat="server" Mode="Encode" /></strong></p><p><asp:Literal ID="ReviewTimestamp" runat="server" Mode="Encode" /></p><p><asp:Literal ID="RecordedReviewReason" runat="server" Mode="Encode" /></p>
<asp:Panel ID="AcademicReviewPanel" runat="server"><asp:HiddenField ID="reviewChoice" runat="server" ClientIDMode="Static" Value="approve" /><fieldset class="mb-3"><legend class="fs-6">Choose review action</legend><label class="me-3"><input type="radio" name="academicReviewChoice" value="approve" checked="checked" /> Approve Evaluation</label><label><input type="radio" name="academicReviewChoice" value="revision" /> Request Revision</label></fieldset><div id="revisionReasonSection" hidden="hidden"><label for="RevisionReason" class="form-label">Reason for Request Revision (required)</label><asp:TextBox ID="RevisionReason" runat="server" ClientIDMode="Static" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="3000" ValidateRequestMode="Disabled" /></div></asp:Panel>
<div class="form-check mt-3"><asp:CheckBox ID="ConfirmAction" runat="server" ClientIDMode="Static" Text="I confirm Academic Approval of this submitted Company evaluation." /></div>
<p class="workspace-error text-danger" role="alert"><asp:Literal ID="EvaluationError" runat="server" Mode="Encode" /></p><p class="workspace-feedback text-success" role="status"><asp:Literal ID="OperationMessage" runat="server" Mode="Encode" /></p>
</div><div class="modal-footer"><asp:Button ID="SaveDraft" runat="server" Text="Save Draft" CssClass="btn btn-secondary" OnClick="SaveDraft_Click" /><asp:Button ID="ConfirmSubmission" runat="server" Text="Submit Evaluation" CssClass="btn btn-primary" OnClick="ConfirmSubmission_Click" /><asp:Button ID="ApproveReview" runat="server" Text="Confirm Academic Approval" CssClass="btn btn-primary" OnClick="ApproveReview_Click" /><asp:Button ID="RequestRevision" runat="server" Text="Confirm Request Revision" CssClass="btn btn-outline-secondary" OnClick="RequestRevision_Click" /><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Close</button></div>
</div></div></div></main>
</div>
        </div>
        
    </form>

<script src="../Js_project/final-evaluation-v2.js?v=4" defer="defer"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
<script src="../Scripts/bootstrap.bundle.min.js"></script></body>
</html>
