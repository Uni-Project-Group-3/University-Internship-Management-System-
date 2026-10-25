<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" ViewStateEncryptionMode="Always" CodeBehind="FinalAcademicEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Academic.FinalAcademicEvaluation" %>
<%@ Register Src="~/Shared/AcademicSidebar.ascx" TagPrefix="uims" TagName="AcademicSidebar" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" media="print" data-evaluation-external="true" />
    <noscript><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" /></noscript>
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Final Academic Evaluation | JUST UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" /><link runat="server" href="~/Assets/shared/evaluation-colors.css?v=20261024-fast-paint" rel="stylesheet" />

    <link runat="server" href="~/Assets/shared/evaluation-workspace.css?v=20261023" rel="stylesheet" />
    
    <link href="../Assets/shared/workflow-ui.css?v=20261025-send-certificate" rel="stylesheet" />
    <script>
        document.querySelectorAll('link[data-evaluation-external]').forEach(function (link) {
            function activate() { link.media = 'all'; }
            link.addEventListener('load', activate);
            if (link.sheet) activate();
        });
    </script>
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<link runat="server" href="~/Assets/academic/final-academic-evaluation.css?v=20261025-academic-modals-1" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-translate" defer></script>

</head>
<body class="coordinator-banner dashboard-direct app-page evaluation-page academic-final-evaluation" data-demo-role="academic">
    <form id="form1" runat="server" novalidate="novalidate">
                                <div class="app-layout">
                        <uims:AcademicSidebar ID="academicSidebar" runat="server" SidebarId="dashboardSidebar" />
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
<section class="uims-page-hero" aria-labelledby="academicEvaluationTitle">
<div class="uims-hero-copy"><p class="uims-hero-eyebrow">JUST / ACADEMIC SUPERVISOR WORKSPACE</p><h1 id="academicEvaluationTitle">Final Academic Evaluation</h1><p>Review submitted Company Final Evaluations and record the academic decision for assigned students.</p></div>
</section>
<section class="academic-review-stats academic-review-stat-cards" aria-label="Authorized evaluation statistics"><article class="uims-hero-stat"><h2>Assigned Students</h2><strong data-stat="AssignedStudentCount"><asp:Literal ID="AssignedStudentCount" runat="server" Mode="Encode" Text="—" /></strong></article><article class="uims-hero-stat"><h2>Company Drafts</h2><strong data-stat="CompanyDraftCount"><asp:Literal ID="CompanyDraftCount" runat="server" Mode="Encode" Text="—" /></strong></article><article class="uims-hero-stat"><h2>Awaiting Academic Review</h2><strong data-stat="AwaitingReviewCount"><asp:Literal ID="AwaitingReviewCount" runat="server" Mode="Encode" Text="—" /></strong></article><article class="uims-hero-stat"><h2>Academic Approvals</h2><strong data-stat="AcademicApprovalCount"><asp:Literal ID="AcademicApprovalCount" runat="server" Mode="Encode" Text="—" /></strong></article></section>
<asp:Literal ID="DraftAvailability" runat="server" Mode="Encode" Visible="false" />
<asp:HiddenField ID="selectedInternship" runat="server" /><asp:HiddenField ID="evaluationTicket" runat="server" EnableViewState="false" /><asp:HiddenField ID="activePanel" runat="server" ClientIDMode="Static" />

<section class="workspace-card evaluation-panel academic-trainees-card">
    <div class="workspace-heading evaluation-panel-heading"><div><p class="academic-card-eyebrow">SUPERVISOR WORKSPACE</p><h2>Assigned Trainees</h2><p>Search authorized students and review their company evaluation status.</p></div></div>
    <div class="workspace-filter-grid evaluation-toolbar academic-evaluation-toolbar">
        <label class="workspace-field academic-search-field"><span>Search trainees</span><span class="academic-search-control"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="10.8" cy="10.8" r="6.8" /><path d="m16 16 5 5" /></svg><input type="search" id="traineeSearch" class="workspace-input" autocomplete="off" /></span></label>
        <label class="workspace-field academic-status-field"><span>Evaluation status</span><select id="evaluationFilter" class="workspace-input"><option value="">All statuses</option><option>Not Started</option><option>Draft</option><option>Submitted</option><option>Revision Requested</option><option>Approved</option></select></label>
        <button type="button" class="workspace-button secondary academic-clear-filters" id="clearEvaluationFilters"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 5h16l-6.3 7.2v5.1l-3.4 1.8v-6.9L4 5Z" /><path d="m17 17 4 4m0-4-4 4" /></svg><span>Clear Filters</span></button>
    </div>
    <div class="workspace-table-scroll evaluation-table-wrap academic-trainees-table-wrap"><table class="table workspace-table academic-trainees-table"><thead><tr><th>Student</th><th>Company</th><th>Company Evaluation</th><th>Academic Review</th><th>Actions</th></tr></thead><tbody><asp:Literal ID="traineeRows" runat="server" /></tbody></table></div>
    <footer class="academic-trainee-footer" id="academicTraineeFooter" aria-live="polite" aria-atomic="true">
        <div class="academic-trainee-count"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg><span id="academicTraineeSummary">Showing — of — trainees</span></div>
        <span class="academic-authorized-badge" id="academicAuthorizedCount">— authorized</span>
    </footer>
</section>
<div class="modal fade academic-details-modal" id="informationDialog" tabindex="-1" aria-labelledby="informationTitle" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable academic-details-dialog">
        <div class="modal-content academic-details-content">
            <div class="modal-header academic-details-header">
                <div class="academic-details-heading-copy">
                    <span class="academic-details-eyebrow">JUST / ACADEMIC SUPERVISOR WORKSPACE</span>
                    <h2 class="modal-title" id="informationTitle"><asp:Literal ID="InformationTitle" runat="server" Mode="Encode" /></h2>
                    <p class="academic-details-subtitle">Complete information about the student's internship placement.</p>
                </div>
                <button type="button" class="academic-details-icon-close" data-bs-dismiss="modal" aria-label="Close Student and Internship Details">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><path d="m6 6 12 12M18 6 6 18" /></svg>
                </button>
            </div>
            <div class="modal-body academic-details-body">
                <div id="informationContent" class="academic-details-content-grid"><asp:Literal ID="InformationText" runat="server" Mode="Encode" /></div>
            </div>
            <div class="modal-footer academic-details-footer">
                <button type="button" class="academic-details-close-button" data-bs-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>
<div class="modal fade academic-evaluation-modal" id="evaluationDialog" tabindex="-1" aria-labelledby="evaluationTitle" aria-hidden="true"><div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable academic-evaluation-dialog"><div class="modal-content academic-evaluation-content">
<div class="modal-header academic-evaluation-header"><div class="academic-evaluation-heading-copy"><span class="academic-evaluation-eyebrow">ACADEMIC SUPERVISOR WORKSPACE</span><h2 class="modal-title" id="evaluationTitle">Company Final Evaluation — <asp:Literal ID="SelectedName" runat="server" Mode="Encode" /></h2><p>Review the submitted company evaluation and record the academic decision.</p></div><button type="button" class="academic-evaluation-icon-close" data-bs-dismiss="modal" aria-label="Close evaluation"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><path d="m6 6 12 12M18 6 6 18" /></svg></button></div>
<div class="modal-body academic-evaluation-body">
<section class="academic-evaluation-section" aria-labelledby="companyEvaluationSectionTitle"><h3 id="companyEvaluationSectionTitle"><span class="academic-evaluation-section-icon" aria-hidden="true">01</span><span>Company Evaluation</span><span class="academic-evaluation-status"><asp:Literal ID="FinalStatus" runat="server" Mode="Encode" /></span></h3><div class="academic-evaluation-section-body"><div class="academic-evaluation-meta"><div><span>Final score</span><strong id="FinalPercentageDisplay" dir="ltr" lang="en"><asp:Literal ID="CompanyPercentageDisplay" runat="server" Mode="Encode" /></strong></div><div><span>Submitted</span><strong><asp:Literal ID="SubmissionTime" runat="server" Mode="Encode" /></strong></div></div><asp:TextBox ID="FinalPercentage" runat="server" Visible="false" /><div class="workspace-field evaluation-field academic-company-feedback"><label for="FinalComments" class="form-label">Company Supervisor feedback</label><asp:TextBox ID="FinalComments" runat="server" ClientIDMode="Static" EnableViewState="false" CssClass="form-control" TextMode="MultiLine" Rows="4" ReadOnly="true" ValidateRequestMode="Disabled" /></div><p class="workspace-hint academic-evaluation-note"><asp:Literal ID="FinalOutcome" runat="server" Mode="Encode" /></p><p class="workspace-hint academic-evaluation-note">Company task grades are separate from this final evaluation. Academic review does not change the Company percentage.</p></div></section>
<section class="academic-evaluation-section academic-review-section" aria-labelledby="academicReviewSectionTitle"><h3 id="academicReviewSectionTitle"><span class="academic-evaluation-section-icon" aria-hidden="true">02</span><span>Academic Review</span><span class="academic-evaluation-status academic-review-status"><asp:Literal ID="ReviewStatus" runat="server" Mode="Encode" /></span></h3><div class="academic-evaluation-section-body"><p class="academic-review-recorded"><asp:Literal ID="ReviewTimestamp" runat="server" Mode="Encode" /></p><p class="academic-review-recorded"><asp:Literal ID="RecordedReviewReason" runat="server" Mode="Encode" /></p>
<asp:Panel ID="AcademicReviewPanel" runat="server" CssClass="academic-review-controls"><asp:HiddenField ID="reviewChoice" runat="server" ClientIDMode="Static" Value="approve" /><fieldset class="academic-review-choice"><legend>Choose review action</legend><label><input type="radio" name="academicReviewChoice" value="approve" checked="checked" /> Approve Evaluation</label><label><input type="radio" name="academicReviewChoice" value="revision" /> Request Revision</label></fieldset><div id="revisionReasonSection" class="academic-revision-reason" hidden="hidden"><label for="RevisionReason" class="form-label">Reason for Request Revision (required)</label><asp:TextBox ID="RevisionReason" runat="server" ClientIDMode="Static" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="3000" ValidateRequestMode="Disabled" /></div></asp:Panel>
<div class="form-check academic-review-confirm"><asp:CheckBox ID="ConfirmAction" runat="server" ClientIDMode="Static" Text="I confirm Academic Approval of this submitted Company evaluation." /></div></div></section>
<div class="academic-evaluation-feedback" aria-live="polite"><p class="workspace-error text-danger" role="alert"><asp:Literal ID="EvaluationError" runat="server" Mode="Encode" /></p><p class="workspace-feedback text-success" role="status"><asp:Literal ID="OperationMessage" runat="server" Mode="Encode" /></p></div>
</div><div class="modal-footer academic-evaluation-footer"><asp:Button ID="SaveDraft" runat="server" Text="Save Draft" CssClass="btn btn-secondary academic-action-button academic-action-secondary" OnClick="SaveDraft_Click" /><asp:Button ID="ConfirmSubmission" runat="server" Text="Submit Evaluation" CssClass="btn btn-primary academic-action-button" OnClick="ConfirmSubmission_Click" /><asp:Button ID="ApproveReview" runat="server" Text="Confirm Academic Approval" CssClass="btn btn-primary academic-action-button" OnClick="ApproveReview_Click" /><asp:Button ID="RequestRevision" runat="server" Text="Confirm Request Revision" CssClass="btn btn-outline-secondary academic-action-button academic-action-secondary" OnClick="RequestRevision_Click" /><button type="button" class="btn academic-action-button academic-action-close" data-bs-dismiss="modal">Close</button></div>
</div></div></div></main>
</div>
        </div>
        
    </form>

<script src="../Js_project/final-evaluation-v2.js?v=20261023-local-fixes" defer="defer"></script>
<script>
    document.addEventListener('DOMContentLoaded', function () {
        var modal = document.getElementById('informationDialog');
        var title = document.getElementById('informationTitle');
        var subtitle = modal && modal.querySelector('.academic-details-subtitle');
        if (!modal || !title || title.textContent.trim().toLowerCase() !== 'student and internship details') {
            if (subtitle) subtitle.hidden = true;
            return;
        }

        title.textContent = 'Student & Internship Details';
        var sections = modal.querySelectorAll('.company-details-section');
        var sectionNames = ['student', 'placement', 'supervisors', 'timeline'];
        var sectionIcons = [
            '<circle cx="9" cy="8" r="3.5"/><path d="M3 20v-1.5A4.5 4.5 0 0 1 7.5 14h3A4.5 4.5 0 0 1 15 18.5V20"/><path d="M16 5.5a3.5 3.5 0 0 1 0 6.8M18 14.5a4.5 4.5 0 0 1 3 4.2V20"/>',
            '<path d="M3 21h18M5 21V7l7-4 7 4v14M9 9h.01M15 9h.01M9 13h.01M15 13h.01M10 21v-4h4v4"/>',
            '<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20v-1.5A4.5 4.5 0 0 1 7 14h4a4.5 4.5 0 0 1 4.5 4.5V20M16 5a3.5 3.5 0 0 1 0 6.8M18 14.5a4.5 4.5 0 0 1 3.5 4.4V20"/>',
            '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M16 3v4M8 3v4M3 10h18m-12 4h6m-6 3h4"/>'
        ];
        sections.forEach(function (section, index) {
            section.classList.add('academic-detail-section', 'academic-detail-section--' + (sectionNames[index] || 'other'));
            var heading = section.querySelector('h3');
            if (heading && !heading.querySelector('.academic-detail-section-mark')) {
                var mark = document.createElement('span');
                mark.className = 'academic-detail-section-mark';
                mark.setAttribute('aria-hidden', 'true');
                var icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                icon.setAttribute('viewBox', '0 0 24 24');
                icon.setAttribute('fill', 'none');
                icon.setAttribute('stroke', 'currentColor');
                icon.setAttribute('stroke-width', '1.65');
                icon.setAttribute('stroke-linecap', 'round');
                icon.setAttribute('stroke-linejoin', 'round');
                icon.innerHTML = sectionIcons[index] || sectionIcons[0];
                mark.appendChild(icon);
                heading.prepend(mark);
            }
        });

        modal.querySelectorAll('.company-details-grid > div').forEach(function (row) {
            var label = row.querySelector('dt');
            var value = row.querySelector('dd');
            if (!label || !value || !/^(company|academic) supervisor$/i.test(label.textContent.trim())) return;
            var identifier = value.textContent.trim();
            value.textContent = 'Name unavailable';
            if (!identifier || /^(unavailable|not available|name unavailable)$/i.test(identifier)) return;

            var disclosure = document.createElement('details');
            disclosure.className = 'academic-id-disclosure';
            var summary = document.createElement('summary');
            summary.textContent = 'Authorized troubleshooting identifier';
            var code = document.createElement('code');
            code.textContent = identifier;
            disclosure.append(summary, code);
            value.appendChild(disclosure);
        });
    });
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.academic-trainees-table .workspace-badge').forEach(function (badge) {
            badge.dataset.academicStatus = badge.textContent.trim().toLowerCase().replace(/\s+/g, '-');
        });
    });
</script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
<script src="../Scripts/bootstrap.bundle.min.js"></script><script src="../Js_project/final-report-summary.js?v=20261024-academic-no-summary-card-2" data-display-card="false"></script>
</body>
</html>
