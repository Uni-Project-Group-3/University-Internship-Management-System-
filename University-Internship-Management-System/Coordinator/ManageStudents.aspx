<%@ Page Language="C#" AutoEventWireup="true" async="true" CodeBehind="ManageStudents.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.ManageStudents" %>
<!DOCTYPE html><html xmlns="http://www.w3.org/1999/xhtml" lang="en"><head runat="server"><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Manage Students | UIMS</title><link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />    <link runat="server" href="~/Assets/coordinator/manage-students.css?v=20261024-link-cleanup" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261024-coordinator-banner" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner dashboard-direct app-page completion-page management-page" data-page="manage-students"><form id="form1" runat="server"><div class="app-layout">            <aside class="dash-sidebar workspace-canonical-sidebar" id="dashboardSidebar" aria-label="Coordinator navigation"><a class="brand workspace-nav-brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Internship Coordinator</p><nav class="workspace-nav-primary" aria-label="Internship Coordinator pages"><a class="workspace-nav-link" href="CoordinatorDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="../Coordinator/UniversityRecords.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4h16v16H4V4Zm4 0v16M4 9h16M4 14h16"/></svg><span>University Records</span></a><a class="workspace-nav-link active" href="ManageStudents.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Students</span></a><a class="workspace-nav-link" href="ManageCompanies.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Companies</span></a><a class="workspace-nav-link" href="Manage_Academic_Supervisor.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Academic Supervisors</span></a><a class="workspace-nav-link" href="Assign_Supervisors.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Assign Academic Supervisors</span></a><a class="workspace-nav-link" href="MonitorPlacementsProgress.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>Placements &amp; Progress</span></a><a class="workspace-nav-link" href="ContactMessages.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Contact Messages</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CoordinatorAccountProfile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
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
                                <a data-uims-logout="true" href="<%= ResolveUrl("~/Auth/Login.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="8" r="3"/>
                                        <path d="M5 21v-3a7 7 0 0 1 14 0v3"/>
                                    </svg>Log out</a>
                                </div>
                            </details>
                        </div>
                    </header><main class="app-content" id="manageStudentsPage">
    <header class="management-hero"><div><p class="completion-eyebrow">STUDENT RECORDS</p><h1>Manage Students</h1><p class="completion-subtitle">Create student accounts and follow academic information, supervision and completion.</p></div></header>
    <asp:HiddenField ID="studentCsrf" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="studentPageState" runat="server" ClientIDMode="Static" />
    <asp:Label ID="studentListMessage" runat="server" ClientIDMode="Static" role="status" CssClass="student-page-message" />
    <asp:Panel ID="emailDeliveryNotice" runat="server" Visible="false" CssClass="completion-notice" role="status">
        Email delivery is unavailable. Students can still be saved; no welcome email will be sent.
    </asp:Panel>
    <section class="completion-panel" aria-labelledby="studentListTitle">
        <header><div><h2 id="studentListTitle">Student accounts</h2><p>Manage student accounts and academic information.</p></div>
            <div class="student-list-actions">
                <asp:Button ID="refreshStudentsButton" runat="server" Visible="false" Text="Refresh" OnClick="RefreshStudents_Click" />
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
                EmptyDataText="No student accounts found. Add a student to get started."
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
                    <asp:TemplateField HeaderText="Details"><ItemTemplate><asp:LinkButton ID="viewStudent" runat="server" CommandName="Select" CausesValidation="false" CssClass="management-icon-button" ToolTip="View student details" aria-label="View student details"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></svg></asp:LinkButton></ItemTemplate></asp:TemplateField>
                    <asp:TemplateField HeaderText="Actions"><ItemTemplate>
                        <button type="button" class="student-delete-action" data-delete-student='<%#: Eval("Uid") %>' data-student-name='<%#: Eval("FullName") %>' title="Delete student" aria-label="Delete student"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7m4-7v7"/></svg></button>
                    </ItemTemplate></asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
        <div class="management-pagination"><span id="studentPageSummary" role="status" aria-live="polite"></span><div><label for="studentPageSize">Rows per page</label><select id="studentPageSize"><option selected>15</option><option>25</option><option>50</option></select><button type="button" id="studentPrevious" aria-label="Previous page">&#8592;</button><nav id="studentPageNumbers" aria-label="Directory pages"></nav><button type="button" id="studentNext" aria-label="Next page">&#8594;</button></div></div>
        <p id="studentNoMatches" class="student-filter-summary" hidden>No students match these filters.</p>
    </section>
</main></div></div>

<dialog id="studentDeleteDialog" class="completion-dialog" aria-labelledby="studentDeleteTitle">
    <header class="completion-dialog-header"><h2 id="studentDeleteTitle">Delete student?</h2><button type="button" class="completion-close" data-close="" aria-label="Cancel deletion">&times;</button></header>
    <div class="completion-dialog-body">
        <p>Permanently delete the account for <strong id="studentDeleteName"></strong>?</p>
        <p>This permanently deletes the login, profile and linked internship history, including attendance and approved hours. Official university records remain. This cannot be undone.</p>
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
    </div>
    <div class="student-form-actions"><button type="button" class="completion-button completion-button--secondary" data-close="">Cancel</button>
        <p class="student-field-help">Confirm &amp; Send emails a password setup link to the university email shown above. Use that same email to sign in after setting your password.</p>
        <asp:Button ID="saveStudentButton" runat="server" ClientIDMode="Static" Text="Confirm &amp; Send" CssClass="completion-button" CausesValidation="false" OnClick="saveStudentButton_Click" />
    </div></div>
</dialog>
</form>
<script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261024-link-cleanup") %>"></script>
<script src="../Js_project/Coordinator/manage-students.js?v=20261024-link-cleanup"></script>
</body></html>
