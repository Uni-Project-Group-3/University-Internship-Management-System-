<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" CodeBehind="FinalCompletionReview.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.FinalCompletionReview" %>


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
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Final Completion Review | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/coordinator/final-completion-review.css?v=20261021-original-ui" rel="stylesheet" />
</head>
<body class="standalone-shell">
    <form id="reviewForm" runat="server">
        <div class="app">
            <!-- سايد بار المنسق داخل الصفحة مباشرة حسب التنظيم المتفق عليه -->
            <aside class="sidebar" id="coordinatorSidebar" aria-label="Coordinator navigation">
                <button type="button" id="closeSidebar" class="sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="CoordinatorDashboard.aspx" class="brand">
                    <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="Jordan University of Science and Technology logo" width="52" height="52" />
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Coordinator pages">
<li><a href="CoordinatorDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ManageStudents.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Students</span></a></li>
<li><a href="ManageCompanies.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg><span>Manage Companies</span></a></li>
<li><a href="MonitorPlacementsProgress.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Placements &amp; Progress</span></a></li>
<li><a href="FinalCompletionReview.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Completion Review</span></a></li>
<li><a href="ContactMessages.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Contact Messages</span></a></li>

<li><a href='<%= ResolveUrl("~/Coordinator/Manage_Academic_Supervisor.aspx") %>'><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage Academic Supervisors</span></a></li>

<li><a href="Assign_Supervisors.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Assign Academic Supervisors</span></a></li>

<li><a href="CoordinatorAccountProfile.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Coordinator Account</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li>
</ul>

                <div class="side-promo">
                    <i class="bi bi-person-plus"></i>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a href="ManageStudents.aspx">Add students &rarr;</a>
                </div>
            </aside>
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
            <header class="page-heading">
                <p class="eyebrow">JUST / COORDINATOR WORKSPACE</p>
                <h1>Final Completion Review</h1>
                <p>Review submitted evaluations and completion readiness. Decisions require verified policy and trusted integrations.</p>
            </header>

            <p class="preview-note"><asp:Literal ID="integrationNotice" runat="server" Mode="Encode" /></p>

            <section class="selection-panel" aria-label="Select student">
                <div><label for="studentSelect" class="form-label">Student to review</label><asp:DropDownList ID="studentSelect" runat="server" ClientIDMode="Static" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="StudentSelect_Changed" /><asp:Button ID="refreshReview" runat="server" Text="Refresh readiness" CssClass="secondary-button" OnClick="RefreshReview_Click" CausesValidation="false" /></div>
                <span id="reviewStatus" runat="server" ClientIDMode="Static" class="status-badge status-pending" role="status">Awaiting review</span>
            </section>

            <div class="row g-4 align-items-start">
                <div class="col-lg-8">
                    <section class="review-card" aria-labelledby="studentHeading">
                        <div class="section-heading"><span class="section-number" aria-hidden="true">01</span><h2 id="studentHeading">Student &amp; placement</h2></div>
                        <dl class="information-grid">
                            <div><dt>Student</dt><dd id="studentName" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Student ID</dt><dd id="studentNumber" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>University / College</dt><dd>Not connected</dd></div>
                            <div><dt>Specialization</dt><dd id="studentMajor" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Company</dt><dd id="companyName" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Internship</dt><dd id="internshipTitle" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Company supervisor</dt><dd id="companySupervisor" runat="server" ClientIDMode="Static"></dd></div>
                            <div><dt>Academic supervisor</dt><dd id="academicSupervisor" runat="server" ClientIDMode="Static"></dd></div>
                        </dl>
                    </section>

                    <!-- التقييمان مخصصان للمنسق، ولا يعرضان للطالب -->
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

                <aside class="col-lg-4" aria-labelledby="decisionHeading">
                    <section class="review-card decision-panel">
                        <p class="eyebrow">FINAL STEP</p>
                        <h2 id="decisionHeading">Completion readiness</h2>
                        <p id="eligibilityMessage" runat="server" ClientIDMode="Static" class="section-description" aria-live="polite"></p>
                        <label for="reviewNotes" class="form-label">Coordinator notes <span>(see applicable policy)</span></label>
                        <asp:TextBox ID="reviewNotes" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="4" MaxLength="1000" CssClass="form-control" ValidateRequestMode="Disabled" Enabled="false" />
                        <label for="decisionReason" class="form-label">Decision reason (required for Request Changes / Reject)</label><asp:TextBox ID="decisionReason" runat="server" ClientIDMode="Static" TextMode="MultiLine" Rows="3" MaxLength="1000" CssClass="form-control" ValidateRequestMode="Disabled" Enabled="false" /><div class="form-check review-check"><asp:CheckBox ID="reviewAcknowledgment" runat="server" ClientIDMode="Static" Enabled="false" /><label for="reviewAcknowledgment" class="form-check-label">I reviewed the current Company evaluation, Academic Review, and completion requirements.</label></div>
                        <button runat="server" ClientIDMode="Static" type="button" id="approveButton" class="primary-button" data-bs-toggle="modal" data-bs-target="#approvalModal" disabled>Approve completion <span aria-hidden="true">&rarr;</span></button>
                        <div class="report-section">
                            <h3>Official completion report</h3>
                            <p class="section-description">Generate a private Completion Report after saved Coordinator approval. Firebase DEMO persists metadata only; PDF bytes remain in private server memory.</p>
                            <asp:Button ID="generateCompletionReport" runat="server" Text="Generate / Verify Completion Report PDF" CssClass="secondary-button" Enabled="false" OnClick="GenerateCompletionReport_Click" /><p role="status"><asp:Literal ID="reportGenerationMessage" runat="server" Mode="Encode" /></p>
                            <p class="preview-note">Printing/certificates are not enabled. The authorized Student can download a generated PDF.</p>
                        </div>
                    </section>
                </aside>
            </div>

            <div class="review-actions"><button ID="openCompletionChanges" runat="server" ClientIDMode="Static" type="button" class="secondary-button" data-bs-toggle="modal" data-bs-target="#changesModal" Visible="false">Request Changes</button><button ID="openCompletionReject" runat="server" ClientIDMode="Static" type="button" class="secondary-button" data-bs-toggle="modal" data-bs-target="#rejectModal" Visible="false">Reject Completion</button></div><div id="approvalNotice" class="approval-notice" role="status" tabindex="-1" hidden></div><p role="status" aria-live="polite"><asp:Literal ID="decisionMessage" runat="server" Mode="Encode" /></p><p id="savedDecision" runat="server" ClientIDMode="Static" class="section-description"></p><ul id="decisionHistory" runat="server" ClientIDMode="Static" class="requirements-list"></ul><asp:HiddenField ID="decisionTicket" runat="server" ClientIDMode="Static" />

            <!-- هذا القسم وحده يظهر عند الطباعة، دون التقييمات أو ملاحظات المنسق -->
            

            <div class="modal fade" id="changesModal" tabindex="-1" aria-labelledby="changesModalTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="changesModalTitle">Confirm Request Changes</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cancel decision"></button></div><div class="modal-body"><p>Confirm the reason and acknowledgment entered in the decision panel. The server rechecks policy, scope and concurrency. Company evaluations are not unlocked by a Coordinator decision.</p></div><div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Cancel</button><asp:Button ID="requestCompletionChanges" runat="server" Text="Request Changes" CssClass="secondary-button" Visible="false" OnClick="RequestCompletionChanges_Click" /></div></div></div></div><div class="modal fade" id="rejectModal" tabindex="-1" aria-labelledby="rejectModalTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="rejectModalTitle">Confirm Completion Rejection</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cancel decision"></button></div><div class="modal-body"><p>Confirm the reason and acknowledgment entered in the decision panel. The server rechecks policy, scope and concurrency. Company evaluations are not unlocked by a Coordinator decision.</p></div><div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Cancel</button><asp:Button ID="rejectCompletion" runat="server" Text="Reject Completion" CssClass="secondary-button" Visible="false" OnClick="RejectCompletion_Click" /></div></div></div></div><div class="modal fade" id="approvalModal" tabindex="-1" aria-labelledby="approvalTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
                    <div class="modal-header"><h2 id="approvalTitle" class="modal-title fs-5">Confirm completion approval</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                    <div class="modal-body"><p id="approvalSummary" runat="server" ClientIDMode="Static"></p><label for="decisionReason" class="form-label">Decision reason (when required by policy)</label><span>Use the reason entered in the completion decision panel.</span><p>Confirm only after reviewing the latest requirements. The server rechecks authorization, policy and readiness before saving.</p><p class="preview-note">Approval is saved only after confirmed persistence. Generate the private report separately after approval.</p></div>
                    <div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Keep reviewing</button><asp:Button ID="confirmApproval" runat="server" ClientIDMode="Static" Enabled="false" CssClass="primary-button" Text="Confirm approval" OnClick="ConfirmApproval_Click" CausesValidation="false" /></div>
                </div></div>
            </div>
            <noscript><p>Readiness is rendered by the server. Use Refresh readiness after selecting a placement when JavaScript is disabled.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Coordinator/final-completion-review.js?v=20261022-decisions"></script>
</body>
</html>
