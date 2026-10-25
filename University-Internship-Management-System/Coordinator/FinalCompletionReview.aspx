<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" CodeBehind="FinalCompletionReview.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.FinalCompletionReview" %>
<%@ Register Src="~/Shared/CoordinatorSidebar.ascx" TagPrefix="uims" TagName="CoordinatorSidebar" %>


<script runat="server">
    protected void btnLogout_Click(object sender, System.EventArgs e)
    {
        Session.Clear();
        Session.Abandon();
        Response.Redirect("~/Auth/Login.aspx");
    }
</script>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" media="print" data-evaluation-external="true" />
    <noscript><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" /></noscript>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Final Completion Review | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" media="print" data-evaluation-external="true" />
    <link href="../Assets/shared/evaluation-colors.css?v=20261024-fast-paint" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/final-completion-review.css?v=20261024-coordinator-company-shell-v5" rel="stylesheet" />
    <link href="../Assets/shared/evaluation-workspace.css?v=20261023" rel="stylesheet" />
    <script>
        document.querySelectorAll('link[data-evaluation-external]').forEach(function (link) {
            function activate() { link.media = 'all'; }
            link.addEventListener('load', activate);
            if (link.sheet) activate();
        });
    </script>
<link href="../Assets/shared/workspace-identity.css?v=20261025-account-button" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>

</head>
<body class="coordinator-banner standalone-shell">
    <form id="reviewForm" runat="server">
        <div class="app">
            <!-- سايد بار المنسق داخل الصفحة مباشرة حسب التنظيم المتفق عليه -->
            <uims:CoordinatorSidebar ID="coordinatorSidebarControl" runat="server" SidebarId="coordinatorSidebar" />
            <div id="sidebarBackdrop" class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main" id="coordinatorMain">
                <header class="topbar">
                    <button type="button" class="menu-btn" id="openSidebar" aria-label="Open navigation" aria-controls="coordinatorSidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="navbarSearch" type="search" aria-label="Search authorized students" placeholder="Search assigned students..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" id="navbarNotifications" disabled="disabled" aria-label="Notifications preview"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown" aria-expanded="false" aria-label="Account menu">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" Text="CO" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" Text="Coordinator" /><span class="role">Internship Coordinator</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CoordinatorAccountProfile.aspx" class="dropdown-item"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>
                <p id="navbarNotice" class="navbar-notice" role="status" hidden></p>
        <main class="review-page">
            <asp:HiddenField ID="detailsVisible" runat="server" ClientIDMode="Static" />
            <section class="uims-page-hero" aria-labelledby="completionReviewTitle"><div class="uims-hero-copy"><p class="uims-hero-eyebrow">JUST / COORDINATOR WORKSPACE</p><h1 id="completionReviewTitle"><i class="bi bi-clipboard-check" aria-hidden="true"></i> Final Completion Review</h1><p>Review approved evaluations and verify internship requirements before confirming final completion.</p></div></section>
            <section class="completion-statistics coordinator-summary" aria-label="Authorized completion statistics">
                <article class="completion-stat stat-ready"><i class="bi bi-people" aria-hidden="true"></i><div><strong><asp:Literal ID="readyCount" runat="server" Mode="Encode" Text="—" /></strong><h2>Ready for Final Review</h2><p>All readiness checks satisfied</p></div></article>
                <article class="completion-stat stat-review"><i class="bi bi-clock" aria-hidden="true"></i><div><strong><asp:Literal ID="inReviewCount" runat="server" Mode="Encode" Text="—" /></strong><h2>In Review</h2><p>Saved UnderReview status</p></div></article>
                <article class="completion-stat stat-complete"><i class="bi bi-check-circle" aria-hidden="true"></i><div><strong><asp:Literal ID="completedCount" runat="server" Mode="Encode" Text="—" /></strong><h2>Completed Internships</h2><p>Saved Approved decisions</p></div></article>
                <article class="completion-stat stat-returned"><i class="bi bi-file-earmark-text" aria-hidden="true"></i><div><strong><asp:Literal ID="returnedCount" runat="server" Mode="Encode" Text="—" /></strong><h2>Returned for Revision</h2><p>Saved ChangesRequested decisions</p></div></article>
            </section>

            <section class="completion-filters" aria-label="Filter authorized students">
                <label>Search students<input type="search" id="completionSearch" placeholder="Student name or ID" class="form-control" /></label>
                <label>Academic approval<select id="academicFilter" class="form-select"><option value="">All statuses</option><option>Approved</option><option>Pending Review</option><option>Revision Requested</option><option>Unavailable</option></select></label>
                <label>Final completion<select id="completionFilter" class="form-select"><option value="">All statuses</option><option>Ready for Final Review</option><option>Pending</option><option>In Review</option><option>Completed</option><option>Returned for Revision</option><option>Rejected</option><option>Unavailable</option></select></label>
                <label>Placement period<asp:DropDownList ID="periodFilter" runat="server" ClientIDMode="Static" CssClass="form-select" Enabled="false" aria-describedby="periodExplanation" /></label>
                <button type="button" id="clearCompletionFilters" class="primary-button">Clear filters</button>
                <p id="periodExplanation" class="filter-note">Placement period options appear only when trusted start and end dates are available.</p>
            </section>
            <p id="completionLoading" role="status" hidden><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Loading authorized review…</p>
            <div class="completion-dashboard">
            <section class="review-card completion-list" aria-labelledby="completionListTitle">
                <div class="completion-list-heading"><h2 id="completionListTitle">Students for Final Completion Review</h2><span id="completionResultCount" role="status"></span><button type="button" id="showStudentDetails" class="secondary-button">Show details</button></div>
                <asp:Panel ID="reviewErrorPanel" runat="server" Visible="false" CssClass="completion-error" role="alert"><asp:Literal ID="reviewErrorMessage" runat="server" Mode="Encode" /></asp:Panel>
                <div class="completion-table-scroll"><table class="table completion-table"><thead><tr><th scope="col">Student</th><th scope="col">Company</th><th scope="col">Academic Approval</th><th scope="col">Final Completion</th><th scope="col">Placement Period</th><th scope="col">Actions</th></tr></thead><tbody><asp:Literal ID="completionRows" runat="server" /></tbody></table></div>
                <p id="noCompletionMatches" role="status" hidden>No authorized students match your filters.</p>
                <details class="completion-selector"><summary>Select a student or refresh readiness</summary><label for="studentSelect" class="form-label">Student to review</label><asp:DropDownList ID="studentSelect" runat="server" ClientIDMode="Static" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="StudentSelect_Changed" /><asp:Button ID="refreshReview" runat="server" Text="Refresh readiness" CssClass="secondary-button" OnClick="RefreshReview_Click" CausesValidation="false" /></details>
            </section>
            <section id="studentDetailsPanel" class="completion-details" aria-labelledby="studentDetailsTitle" tabindex="-1" hidden>
                <div class="details-heading"><h2 id="studentDetailsTitle">Student Details</h2><button type="button" id="closeStudentDetails" class="btn-close" aria-label="Close student details"></button></div>
                <span id="reviewStatus" runat="server" ClientIDMode="Static" class="status-badge status-pending" role="status">Unavailable</span>
            <div class="completion-detail-content">
                <div class="completion-evaluation-content">
                    <section class="review-card" aria-labelledby="studentHeading">
                        <div class="section-heading"><span class="section-number" aria-hidden="true">01</span><h2 id="studentHeading">Student &amp; placement</h2></div>
                        <dl class="information-grid">
                            <div><dt>Student</dt><dd id="studentName" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Student ID</dt><dd id="studentNumber" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>University / College</dt><dd id="studentUniversityCollege" runat="server" ClientIDMode="Static">Not available</dd></div>
                            <div><dt>Specialization</dt><dd id="studentMajor" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Placement ID</dt><dd id="placementNumber" runat="server" ClientIDMode="Static">Not available</dd></div>
                            <div><dt>Placement period</dt><dd id="placementPeriod" runat="server" ClientIDMode="Static">Unavailable</dd></div>
                            <div><dt>Company</dt><dd id="companyName" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Internship</dt><dd id="internshipTitle" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Company supervisor</dt><dd id="companySupervisor" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Academic supervisor</dt><dd id="academicSupervisor" runat="server" ClientIDMode="Static"></dd></div>
                        </dl>
                    </section>

                    <div class="completion-tabs" role="tablist" aria-label="Student review sections">
                        <button type="button" role="tab" id="evaluationTab" aria-controls="evaluationTabPanel" aria-selected="true" tabindex="0" data-review-tab="evaluationTabPanel">Evaluation Details</button>
                        <button type="button" role="tab" id="documentsTab" aria-controls="documentsTabPanel" aria-selected="false" tabindex="-1" data-review-tab="documentsTabPanel">Documents</button>
                        <button type="button" role="tab" id="historyTab" aria-controls="historyTabPanel" aria-selected="false" tabindex="-1" data-review-tab="historyTabPanel">History</button>
                    </div>
                    <div role="tabpanel" id="documentsTabPanel" aria-labelledby="documentsTab" hidden>                        <div class="report-section">
                            <h3>Official completion report</h3>
                            <p class="section-description">Generate a private Completion Report after saved Coordinator approval. Availability depends on approved policy, trusted profile data and private storage.</p>
                            <asp:Button ID="generateCompletionReport" runat="server" Text="Generate / Verify Completion Report PDF" CssClass="secondary-button" Enabled="false" OnClick="GenerateCompletionReport_Click" /><p role="status"><asp:Literal ID="reportGenerationMessage" runat="server" Mode="Encode" /></p>
                            <p class="preview-note">Student delivery requires the existing authorized private report integration.</p>
                        </div><p class="preview-note">Coordinator PDF viewing and downloading are unavailable: no authorized Coordinator download adapter is configured.</p><button type="button" class="secondary-button" disabled aria-describedby="pdfUnavailable">View / Download PDF</button><p id="pdfUnavailable" class="preview-note">A verified private document locator and Coordinator download authorization are required.</p></div>
                    <div role="tabpanel" id="historyTabPanel" aria-labelledby="historyTab" hidden><h3>Decision history</h3><p id="savedDecision" runat="server" ClientIDMode="Static" class="section-description"></p><ul id="decisionHistory" runat="server" ClientIDMode="Static" class="requirements-list"></ul><p id="historyEmpty" class="preview-note">No authorized decision history is available.</p></div>
                    <div role="tabpanel" id="evaluationTabPanel" aria-labelledby="evaluationTab">
                    <section class="review-card" aria-labelledby="companyHeading">
                        <div class="evaluation-heading"><h2 id="companyHeading">Company final evaluation</h2><span id="companyStatus" runat="server" ClientIDMode="Static" class="status-badge"></span></div>
                        <p class="section-description">Submitted information is read-only and visible only to authorized Coordinators.</p>
                        <dl class="evaluation-facts"><div><dt>Submitted on</dt><dd id="companyDate" runat="server" ClientIDMode="Static"></dd></div><div><dt>Submitted record</dt><dd id="companyRecommendation" runat="server" ClientIDMode="Static"></dd></div></dl>
                        <p id="companyScores" runat="server" ClientIDMode="Static" class="feedback-text"></p><h3>Supervisor feedback</h3><p id="companyFeedback" runat="server" ClientIDMode="Static" class="feedback-text"></p>
                    </section>

                    <section class="review-card" aria-labelledby="academicHeading">
                        <div class="evaluation-heading"><h2 id="academicHeading">Academic Review</h2><span id="academicStatus" runat="server" ClientIDMode="Static" class="status-badge"></span></div>
                        <p class="section-description">Sent to the coordinator by the assigned academic supervisor.</p>
                        <dl class="evaluation-facts"><div><dt>Submitted on</dt><dd id="academicDate" runat="server" ClientIDMode="Static"></dd></div><div><dt>Submitted record</dt><dd id="academicRecommendation" runat="server" ClientIDMode="Static"></dd></div></dl>
                        <p id="academicScores" runat="server" ClientIDMode="Static" class="feedback-text"></p><h3>Academic feedback</h3><p id="academicFeedback" runat="server" ClientIDMode="Static" class="feedback-text"></p>
                        <p class="preview-note">The Company percentage and exact-version Academic review are shown read-only. This readiness review does not calculate grades or invent grading rules.</p>
                    </section>

                    <section class="review-card" aria-labelledby="requirementsHeading">
                        <div class="section-heading"><span class="section-number" aria-hidden="true">02</span><h2 id="requirementsHeading">Completion requirements</h2></div>
                        <div class="hours-heading"><h3>Approved training hours</h3><span id="hoursText" runat="server" ClientIDMode="Static"></span></div>
                        <progress id="hoursProgress" runat="server" ClientIDMode="Static" aria-label="Approved training hours"></progress>
                        <p class="section-description">Only approved hours count. Completed tasks do not automatically approve hours.</p>
                        <ul id="requirementsList" runat="server" ClientIDMode="Static" class="requirements-list"></ul>
                        <p class="preview-note">Requirements come from trusted sources for the applicable policy. Missing data blocks readiness.</p>
                    </section>
                    </div>
                </div>

                <aside class="completion-decision" aria-labelledby="decisionHeading">
                    <section class="review-card decision-panel">
                        <p class="eyebrow">FINAL STEP</p>
                        <h2 id="decisionHeading">Completion readiness</h2>
                        <p id="eligibilityMessage" runat="server" ClientIDMode="Static" class="section-description" aria-live="polite"></p>
                        <label for="reviewNotes" class="form-label">Coordinator notes <span>(see applicable policy)</span></label>
                        <asp:TextBox ID="reviewNotes" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="4" MaxLength="1000" CssClass="form-control" ValidateRequestMode="Disabled" Enabled="false" />
                        <label for="decisionReason" class="form-label">Decision reason (required for Request Changes / Reject)</label><asp:TextBox ID="decisionReason" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" MaxLength="1000" CssClass="form-control" ValidateRequestMode="Disabled" Enabled="false" /><div class="form-check review-check"><asp:CheckBox ID="reviewAcknowledgment" runat="server" ClientIDMode="Static" Enabled="false" /><label for="reviewAcknowledgment" class="form-check-label">I reviewed the current Company evaluation, Academic Review, and completion requirements.</label></div>
                        <button runat="server" ClientIDMode="Static" type="button" id="approveButton" class="primary-button" data-bs-toggle="modal" data-bs-target="#approvalModal" disabled>Complete Final Review <span aria-hidden="true">&rarr;</span></button>

                    </section>
                </aside>
            </div>

            <div class="review-actions"><button ID="openCompletionChanges" runat="server" ClientIDMode="Static" type="button" class="secondary-button" data-bs-toggle="modal" data-bs-target="#changesModal" Visible="false">Request Changes</button><button ID="openCompletionReject" runat="server" ClientIDMode="Static" type="button" class="secondary-button" data-bs-toggle="modal" data-bs-target="#rejectModal" Visible="false">Reject Completion</button></div><div id="approvalNotice" class="approval-notice" role="status" tabindex="-1" hidden></div><p role="status" aria-live="polite"><asp:Literal ID="decisionMessage" runat="server" Mode="Encode" /></p><asp:HiddenField ID="decisionTicket" runat="server" ClientIDMode="Static" />

            <!-- هذا القسم وحده يظهر عند الطباعة، دون التقييمات أو ملاحظات المنسق -->


            <div class="modal fade" id="changesModal" tabindex="-1" aria-labelledby="changesModalTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="changesModalTitle">Confirm Request Changes</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cancel decision"></button></div><div class="modal-body"><p>Confirm the reason and acknowledgment entered in the decision panel. The server rechecks policy, scope and concurrency. Company evaluations are not unlocked by a Coordinator decision.</p></div><div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Cancel</button><asp:Button ID="requestCompletionChanges" runat="server" Text="Request Changes" CssClass="secondary-button" Visible="false" OnClick="RequestCompletionChanges_Click" /></div></div></div></div><div class="modal fade" id="rejectModal" tabindex="-1" aria-labelledby="rejectModalTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="rejectModalTitle">Confirm Completion Rejection</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cancel decision"></button></div><div class="modal-body"><p>Confirm the reason and acknowledgment entered in the decision panel. The server rechecks policy, scope and concurrency. Company evaluations are not unlocked by a Coordinator decision.</p></div><div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Cancel</button><asp:Button ID="rejectCompletion" runat="server" Text="Reject Completion" CssClass="secondary-button" Visible="false" OnClick="RejectCompletion_Click" /></div></div></div></div><div class="modal fade" id="approvalModal" tabindex="-1" aria-labelledby="approvalTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
                    <div class="modal-header"><h2 id="approvalTitle" class="modal-title fs-5">Confirm completion approval</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                    <div class="modal-body"><p id="approvalSummary" runat="server" ClientIDMode="Static"></p><label for="decisionReason" class="form-label">Decision reason (when required by policy)</label><span>Use the reason entered in the completion decision panel.</span><p>Confirm only after reviewing the latest requirements. The server rechecks authorization, policy and readiness before saving.</p><p class="preview-note">Approval is saved only after confirmed persistence. Generate the private report separately after approval.</p></div>
                    <div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Keep reviewing</button><asp:Button ID="confirmApproval" runat="server" ClientIDMode="Static" Enabled="false" CssClass="primary-button" Text="Confirm approval" OnClick="ConfirmApproval_Click" CausesValidation="false" /></div>
                </div></div>
            </div>
            </section></div>
            <noscript><p>Readiness is rendered by the server. Use Refresh readiness after selecting a placement when JavaScript is disabled.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Coordinator/final-completion-review.js?v=20261024-coordinator-review-v2"></script>
</body>
</html>
