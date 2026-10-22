<%@ Page Language="C#" AutoEventWireup="true" async="true" CodeBehind="ManageStudents.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.ManageStudents" %>
<!DOCTYPE html>
<html lang="en"><head runat="server"><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Manage Students | UIMS</title><link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />    <link runat="server" href="~/Assets/coordinator/manage-students.css?v=20261022-complete" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page completion-page" data-page="manage-students"><form id="form1" runat="server"><div class="app-layout">            <aside class="dash-sidebar" id="dashboardSidebar" aria-label="Coordinator navigation">
                <a href="<%= ResolveUrl("~/Coordinator/CoordinatorDashboard.aspx") %>" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text">
                        <strong>JUST</strong>
                        <span>Internships</span>
                    </span>
                </a>
                <button type="button" class="dash-sidebar-close" data-sidebar-close="" aria-label="Close navigation">
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

<li><a href='<%= ResolveUrl("~/Coordinator/Manage_Academic_Supervisor.aspx") %>'><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage Academic Supervisors</span></a></li>

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
            <button type="button" class="dash-sidebar-backdrop" data-sidebar-close="" aria-label="Close navigation" tabindex="-1" hidden>
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
                    </header><main class="app-content" id="manageStudentsPage">
    <p class="completion-eyebrow">STUDENT RECORDS</p>
    <h1>Manage Students</h1>
    <p class="completion-subtitle">Create student accounts and follow academic information, supervision and completion.</p>
    <asp:HiddenField ID="studentCsrf" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="studentPageState" runat="server" ClientIDMode="Static" />
    <asp:Label ID="studentListMessage" runat="server" ClientIDMode="Static" role="status" CssClass="student-page-message" />
    <asp:Panel ID="emailDeliveryNotice" runat="server" Visible="false" CssClass="completion-notice" role="status">
        Email delivery is unavailable. Students can still be saved; no welcome email will be sent.
    </asp:Panel>
    <section class="completion-panel" aria-labelledby="studentListTitle">
        <header><div><h2 id="studentListTitle">Student accounts</h2><p>Account actions preserve university records and academic history.</p></div>
            <div class="student-list-actions">
                <asp:Button ID="refreshStudentsButton" runat="server" Text="Refresh" CausesValidation="false"
                    CssClass="completion-button completion-button--secondary" OnClick="RefreshStudents_Click" />
                <button type="button" class="completion-button" id="addStudentButton">Add student</button>
            </div>
        </header>
        <div class="completion-filters">
            <label for="studentDepartmentFilter">Department<select id="studentDepartmentFilter" runat="server" ClientIDMode="Static"><option value="">All departments</option></select></label>
            <label for="studentSupervisorFilter">Academic supervisor<select id="studentSupervisorFilter" runat="server" ClientIDMode="Static"><option value="">All supervisors</option></select></label>
            <label for="completionSearch">Search students<input type="search" id="completionSearch" runat="server" ClientIDMode="Static" placeholder="Name, student ID or email" maxlength="150" /></label>
            <label for="completionStatusFilter">Completion status<select id="completionStatusFilter" runat="server" ClientIDMode="Static"><option value="">All completion statuses</option></select></label>
            <button type="button" class="completion-button completion-button--secondary" id="completionClearFilters">Clear filters</button>
        </div>
        <p id="studentFilterSummary" class="student-filter-summary" role="status" aria-live="polite"></p>
        <div class="completion-table-wrap" role="region" aria-label="Student accounts" tabindex="0">
            <asp:GridView ID="studentsGrid" runat="server" ClientIDMode="Static" AutoGenerateColumns="false"
                DataKeyNames="Uid" GridLines="None" UseAccessibleHeader="true" ShowHeaderWhenEmpty="true"
                EmptyDataText="No student accounts found. Add a student or refresh the list."
                OnSelectedIndexChanged="ViewDetails_Click"
                OnRowDataBound="StudentGrid_RowDataBound">
                <Columns>
                    <asp:BoundField DataField="FullName" HeaderText="Student" HtmlEncode="true" />
                    <asp:BoundField DataField="StudentId" HeaderText="Student ID" HtmlEncode="true" />
                    <asp:BoundField DataField="Email" HeaderText="Email" HtmlEncode="true" />
                    <asp:BoundField DataField="Department" HeaderText="Department" HtmlEncode="true" />
                    <asp:BoundField DataField="Supervisor" HeaderText="Academic supervisor" HtmlEncode="true" />
                    <asp:BoundField DataField="CompletionStatus" HeaderText="Completion" HtmlEncode="true" />
                    <asp:BoundField DataField="AccountStatus" HeaderText="Account" HtmlEncode="true" />
                    <asp:CommandField ShowSelectButton="true" SelectText="View details" CausesValidation="false" />
                    <asp:TemplateField HeaderText="Actions"><ItemTemplate>
                        <button type="button" class="student-delete-action" data-delete-student='<%#: Eval("Uid") %>' data-student-name='<%#: Eval("FullName") %>'>Delete</button>
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <p id="studentNoMatches" class="student-filter-summary" hidden>No students match these filters.</p>
    </section>
</main></div></div>

<dialog id="studentDeleteDialog" class="completion-dialog" aria-labelledby="studentDeleteTitle">
    <header class="completion-dialog-header"><h2 id="studentDeleteTitle">Delete student?</h2><button type="button" class="completion-close" data-close="" aria-label="Cancel deletion">&times;</button></header>
    <div class="completion-dialog-body">
        <p>Delete the login and account profile for <strong id="studentDeleteName"></strong>?</p>
        <p>This permanently removes their Firebase login and users profile. University records and academic history will be retained.</p>
        <asp:HiddenField ID="studentDeleteUid" runat="server" ClientIDMode="Static" />
        <div class="student-form-actions">
            <button type="button" class="completion-button completion-button--secondary" data-close="">Cancel</button>
            <asp:Button ID="confirmDeleteStudentButton" runat="server" ClientIDMode="Static" Text="Delete student" CssClass="completion-button" UseSubmitBehavior="true" CausesValidation="false" OnClick="DeleteStudent_Click" />
        </div>
    </div>
</dialog>
<dialog id="studentDetailsDialog" class="completion-dialog" aria-labelledby="studentDetailsTitle">
    <header class="completion-dialog-header"><h2 id="studentDetailsTitle">Student details</h2><button type="button" class="completion-close" data-close="" aria-label="Close student details">&times;</button></header>
    <div class="completion-dialog-body">
        <asp:Panel ID="studentDetailsPanel" runat="server" ClientIDMode="Static" Visible="false">
            <asp:Literal ID="studentDetailsContent" runat="server" />
        </asp:Panel>
        <button type="button" class="completion-button completion-button--secondary" data-close="">Close</button>
    </div>
</dialog>

<dialog id="studentEditor" class="completion-dialog" aria-labelledby="studentEditorTitle">
    <header class="completion-dialog-header"><h2 id="studentEditorTitle">Add student</h2><button type="button" class="completion-close" data-close="" aria-label="Close">&times;</button></header>
    <div class="completion-dialog-body"><div id="studentFields">
        <div class="student-lookup">
            <label for="studentUniversityId" class="student-label">Student ID</label>
            <div class="student-input-action">
                <input id="studentUniversityId" runat="server" ClientIDMode="Static" maxlength="30" inputmode="numeric" placeholder="Enter student ID" />
                <asp:Button ID="findStudentButton" runat="server" ClientIDMode="Static" Text="Find student" CssClass="completion-button student-find" CausesValidation="false" OnClick="findStudentButton_Click" />
            </div>
            <asp:Label ID="studentLookupMessage" runat="server" ClientIDMode="Static" CssClass="student-lookup-status" role="status" />
        </div>
        <div class="student-record"><div class="student-detail-grid">
            <label class="student-label">Full name<input id="studentName" runat="server" ClientIDMode="Static" disabled="disabled" /></label>
            <label class="student-label">Department<input id="studentDepartment" runat="server" ClientIDMode="Static" disabled="disabled" /></label>
            <label class="student-label student-email">University email<input id="studentEmail" runat="server" ClientIDMode="Static" type="email" disabled="disabled" /></label>
        </div></div>
        <div class="student-password-section">
            <label for="studentTemporaryPassword" class="student-label">Temporary password</label>
            <div class="student-input-action">
                <input id="studentTemporaryPassword" runat="server" ClientIDMode="Static" EnableViewState="false" type="text" readonly="readonly" autocomplete="off" spellcheck="false" />
                <asp:Button ID="generateStudentPassword" runat="server" ClientIDMode="Static" Text="Generate password" CssClass="completion-button completion-button--secondary" CausesValidation="false" OnClick="generateStudentPassword_Click" />
                <button type="button" id="copyStudentPassword" class="completion-button completion-button--secondary" aria-label="Copy temporary password">Copy</button>
            </div>
            <span id="studentPasswordStatus" class="student-field-help" role="status"></span>
        </div>
    </div>
    <div class="student-form-actions"><button type="button" class="completion-button completion-button--secondary" data-close="">Cancel</button>
        <asp:Button ID="saveStudentButton" runat="server" ClientIDMode="Static" Text="Confirm" CssClass="completion-button" CausesValidation="false" OnClick="saveStudentButton_Click" />
    </div></div>
</dialog>
</form>
<script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
<script src="../Js_project/Coordinator/manage-students.js?v=20261022-delete-submit"></script>
</body></html>
