<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ManageStudents.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.ManageStudents" %>
<!DOCTYPE html><html xmlns="http://www.w3.org/1999/xhtml" lang="en"><head runat="server"><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Manage Students | UIMS</title><link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />    <link runat="server" href="~/Assets/coordinator/manage-students.css?v=20261021-original-ui" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page completion-page" data-demo-role="admin"><form id="form1" runat="server"><div class="app-layout">            <aside class="dash-sidebar" id="dashboardSidebar" aria-label="Coordinator navigation">
                <a href="<%= ResolveUrl("~/Coordinator/CoordinatorDashboard.aspx") %>" class="dash-brand">
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
                    <ul class="dash-side-nav" aria-label="Coordinator pages">
<li><a href="CoordinatorDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ManageStudents.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Students</span></a></li>
<li><a href="ManageCompanies.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg><span>Manage Companies</span></a></li>
<li><a href="MonitorPlacementsProgress.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Placements &amp; Progress</span></a></li>
<li><a href="FinalCompletionReview.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Completion Review</span></a></li>
<li><a href="ContactMessages.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Contact Messages</span></a></li>

<li><a href="Manage_Academic_Supervisor.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage Academic Supervisors</span></a></li>

<li><a href="Assign_Supervisors.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Assign Academic Supervisors</span></a></li>

<li><a href="CoordinatorAccountProfile.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Coordinator Account</span></a></li>
</ul>
                    <div class="dash-side-divider">
                    </div>
                    <ul class="dash-side-nav">
                        

</ul>
                </nav>
                <div class="dash-side-promo">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="m2 9 10-5 10 5-10 5-10-5Zm4 3v6c4 3 8 3 12 0v-6m4-3v8"/>
                    </svg>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a href="<%= ResolveUrl("~/Coordinator/ManageStudents.aspx") %>">Add students &rarr;</a>
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
                        <input type="search" id="dashboardSearch" data-search-target="completionSearch" placeholder="Search for students, companies, or supervisors..." />
                    </label>
                    <div class="dash-topbar-actions">
                        <button type="button" class="dash-icon-btn" disabled aria-label="Notifications are not connected" title="Notifications are not connected">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                <path d="M5 17h14l-2-3V9a5 5 0 0 0-10 0v5l-2 3Zm5 3h4"/>
                            </svg>
                        </button>
                        <details class="dash-profile">
                            <summary class="dash-user-chip">
                                <span class="dash-avatar">CO</span>
                                <span class="dash-user-name">Coordinator<span class="dash-role">Internship Coordinator</span>
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
                    </header><main class="app-content"><p class="completion-eyebrow">STUDENT RECORDS</p><h1>Manage Students</h1><p class="completion-subtitle">Monitor completion acknowledgments, certificate delivery, and account status.</p><p class="completion-notice">Interface preview only. Student records and completion operations are not connected.</p><section class="completion-panel"><header><div><h2>Student completion records</h2><p>Academic history remains available after account deactivation.</p></div><button type="button" class="completion-button" id="addStudentButton">Add student</button></header><div class="completion-filters"><label>Department<select id="studentDepartmentFilter"></select></label><label>Academic supervisor<select id="studentSupervisorFilter"></select></label><label>Search students<input type="search" id="completionSearch" placeholder="Student name or university ID..." /></label><label>Completion status<select id="completionStatusFilter"><option value="">All statuses</option><option>In Training</option><option>Pending Student Confirmation</option><option>Completed — Grace Period</option><option>Completed — Disabled</option></select></label><button type="button" class="completion-button completion-button--secondary" id="completionClearFilters">Clear filters</button></div><div class="completion-table-wrap" role="region" aria-label="Student completion records" tabindex="0"><table><thead><tr><th scope="col">Student / ID</th><th scope="col">Completion status</th><th scope="col">Confirmed at (UTC)</th><th scope="col">Disable deadline (UTC)</th><th scope="col">Certificate</th><th scope="col">Actions</th></tr></thead><tbody></tbody></table></div><div class="completion-empty"><h3 id="completionEmptyTitle">Student records are not connected</h3><p>No student records are loaded. Search results will be available after integration.</p></div></section><p class="completion-help">Completed accounts use automatic deactivation. Manual Disable/Enable actions are not included in this completion workflow.</p></main></div></div>
<dialog id="completionDialog" class="completion-dialog" aria-labelledby="completionDialogTitle" aria-describedby="completionDialogNote"><header class="completion-dialog-header"><div><p class="completion-eyebrow">STUDENT COMPLETION</p><h2 id="completionDialogTitle">Student details</h2></div><button type="button" class="completion-close" data-completion-close aria-label="Close student details">&times;</button></header><div class="completion-dialog-body"><p id="completionDialogNote" class="completion-notice">Layout preview only. No student record is selected or changed.</p><div data-completion-view="record"><label class="completion-preview-label">Preview completion state<select id="completionRecordState"><option value="training">In Training</option><option value="pending">Pending Student Confirmation</option><option value="grace">Completed — Grace Period</option><option value="disabled">Completed — Disabled</option></select></label><article id="completionRecordPreview" class="completion-record"><span id="completionRecordBadge" class="completion-badge">In Training</span><h3>No student selected</h3><p id="completionRecordDescription" class="completion-subtitle">Training and academic evaluations are in progress.</p><dl class="completion-info"><div><dt>Company / Internship</dt><dd>&mdash;</dd></div><div><dt>Completion approved at (UTC)</dt><dd>&mdash;</dd></div><div><dt>Student confirmed at (UTC)</dt><dd>&mdash;</dd></div><div><dt>Account disable deadline (UTC)</dt><dd>&mdash;</dd></div><div><dt>Account disabled at (UTC)</dt><dd>&mdash;</dd></div><div><dt>Certificate ID / Status</dt><dd>No document loaded</dd></div></dl></article><p class="completion-help">Dates and certificate metadata will come from trusted server records. Full academic history is retained; company, evaluations, and approved hours are not deleted.</p><button type="button" class="completion-button completion-button--secondary" data-completion-close>Close</button></div></div></dialog><dialog id="studentEditor" class="completion-dialog" aria-labelledby="studentEditorTitle"><header class="completion-dialog-header"><h2 id="studentEditorTitle">Add student</h2><button type="button" class="completion-close" data-close aria-label="Close">&times;</button></header><div class="completion-dialog-body"><p id="studentActionDescription"></p><div id="studentFields"><label>Full name<input id="studentName" required maxlength="100" /></label><label>Email<input id="studentEmail" type="email" required maxlength="254" /></label><label>University ID<input id="studentUniversityId" required maxlength="30" /></label><label>Department<select id="studentDepartment" required></select></label></div><button type="button" class="completion-button completion-button--secondary" data-close>Cancel</button><button type="button" id="saveStudentButton" class="completion-button">Confirm</button></div></dialog></form><script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="<%= ResolveUrl("~/Js_project/Public/completion.js?v=20261021-original-ui") %>"></script>    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
<script src="../Js_project/Coordinator/manage-students.js?v=20261021-original-ui"></script>
</body></html>