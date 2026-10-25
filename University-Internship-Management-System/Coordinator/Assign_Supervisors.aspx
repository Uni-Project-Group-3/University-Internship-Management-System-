<%@ Page Async="true" Language="C#" AutoEventWireup="true" CodeBehind="Assign_Supervisors.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.Assign_Supervisors" %>
<%@ Register Src="~/Shared/CoordinatorSidebar.ascx" TagPrefix="shared" TagName="RoleSidebar" %>
<%@ Register Src="~/Shared/WorkspaceNavbar.ascx" TagPrefix="shared" TagName="WorkspaceNavbar" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Assign Academic Supervisors | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link rel="stylesheet" type="text/css" href="../Assets/coordinator/Assign_Supervisors.css?v=2.0" />
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261024-workspace" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/workspace-navigation.css?v=20261024-hide-search" rel="stylesheet" />
    <link rel="stylesheet" href="../Assets/coordinator/academic-assignment.css?v=20261024-group-remove" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
<body class="coordinator-banner dashboard-direct app-page assign-supervisors-page shared-workspace-shell">

    <!-- Icon sprite -->
    <svg width="0" height="0" style="position:absolute" aria-hidden="true">
        <defs>
            <symbol id="i-home" viewBox="0 0 24 24"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></symbol>
            <symbol id="i-chart" viewBox="0 0 24 24"><path d="M4 20V5m0 15h16M8 16v-5m5 5V7m5 9v-8"/></symbol>
            <symbol id="i-report" viewBox="0 0 24 24"><path d="M7 3h8l4 4v14H7zM15 3v5h4M10 13h6m-6 4h6"/></symbol>
            <symbol id="i-evaluate" viewBox="0 0 24 24"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></symbol>
            <symbol id="i-user" viewBox="0 0 24 24"><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></symbol>
            <symbol id="i-search" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></symbol>
            <symbol id="i-bell" viewBox="0 0 24 24"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10 21a2 2 0 0 0 4 0"/></symbol>
            <symbol id="i-users" viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 4.6a3.5 3.5 0 0 1 0 6.8M18 14.2a6.5 6.5 0 0 1 3.5 5.8"/></symbol>
            <symbol id="i-building" viewBox="0 0 24 24"><path d="M3 21h18M5 21V8l7-4 7 4v13M9 12h2m2 0h2M9 16h2m2 0h2"/></symbol>
            <symbol id="i-inbox" viewBox="0 0 24 24"><path d="M3 13h5l1.5 3h5L16 13h5M5 5h14l2 8v6H3v-6l2-8Z"/></symbol>
            <symbol id="i-check" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></symbol>
            <symbol id="i-filter" viewBox="0 0 24 24"><path d="M3 5h18l-7 8v6l-4-2v-4L3 5Z"/></symbol>
        </defs>
    </svg>

    <form id="form1" runat="server">
        <div class="app-layout">
            
            <!-- Sidebar -->
            <shared:RoleSidebar ID="RoleNavigation" runat="server" SidebarId="workspaceSidebar" />

            <!-- Main Content -->
            <div class="dash-main">
                <shared:WorkspaceNavbar ID="WorkspaceNavigation" runat="server" SearchInputId="searchGroups" SearchInputType="search" SearchPlaceholder="Search student groups or supervisors..." AccountLabel="Coordinator" AccountRole="Internship Coordinator" AccountInitials="CO" AccountUrl="~/Coordinator/CoordinatorAccountProfile.aspx" SidebarId="workspaceSidebar" />

                <main class="app-content">
                    <!-- Hero Banner -->
                    <div class="hero-banner">
                        <div class="hero-text">
                            <span class="hero-badge"><svg class="ic"><use href="#i-users"/></svg>GROUP ASSIGNMENTS</span>
                            <h1>Assign Academic Supervisors</h1>
                            <p>Choose an academic supervisor, assign one university major, and build a group of students to follow their training.</p>
                        </div>
                    </div>

                    <p id="assignmentNotice" class="assignment-notice" role="status" aria-live="polite">Loading groups...</p>

                    <!-- Assignments Panel -->
                    <section class="monitor-panel">
                        <div class="monitor-panel-heading" style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px;">
                            <div>
                                <h2>Student Groups &amp; Supervisors</h2>
                                <p>Manage group allocations and link academic supervisors.</p>
                            </div>

                            <div class="panel-tools">
                                <label class="select-wrap">Major
                                    <select id="departmentFilter" aria-label="Filter groups by major"><option value="">All majors</option></select>
                                </label>
                                <button type="button" class="assignment-secondary" id="refreshAssignments">Refresh</button>
                                <button type="button" class="assignment-primary" id="newAssignment" disabled="disabled">+ Add group</button>
                            </div>
                        </div>

                        <div class="monitor-table-scroll">
                            <table class="monitor-table" id="groupsTable">
                                <thead>
                                    <tr>
                                        <th>Group ID / Name</th>
                                        <th>Department / Major</th>
                                        <th>Students Count</th>
                                        <th>Assigned Academic Supervisor</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody id="assignmentRows"><tr><td colspan="5">Loading groups...</td></tr></tbody>
                            </table>
                        </div>
                    </section>
                </main>
            </div>
        </div>
    </form>

    <dialog id="assignmentDialog" aria-labelledby="assignmentTitle">
        <div class="assignment-dialog-heading">
            <div><span class="assignment-eyebrow">ACADEMIC SUPERVISION</span><h2 id="assignmentTitle">Add student group</h2></div>
            <button type="button" id="assignmentClose" class="assignment-close" aria-label="Close group dialog">&times;</button>
        </div>
        <p id="assignmentIntro" class="assignment-intro">Each supervisor is responsible for one major. Select students from that major to join the group.</p>
        <div id="assignmentFields">
            <dl id="assignmentSummary" class="assignment-summary" hidden="hidden">
                <div><dt>Academic supervisor</dt><dd id="summaryProfessor"></dd></div>
                <div><dt>University major</dt><dd id="summaryMajor"></dd></div>
                <div><dt>Students in group</dt><dd id="summaryCount"></dd></div>
            </dl>
            <section id="assignmentCurrent" class="assignment-current" aria-labelledby="assignmentCurrentTitle" hidden="hidden">
                <h3 id="assignmentCurrentTitle">Current students</h3>
                <div id="assignmentCurrentList" class="assignment-students"></div>
            </section>
            <div id="assignmentSetup">
                <label class="assignment-field" for="assignmentProfessor"><span>01 &nbsp; Academic supervisor <span aria-hidden="true">*</span></span>
                <select id="assignmentProfessor" required="required"></select>
                </label>
                <label class="assignment-field" for="assignmentDepartment"><span>02 &nbsp; University major <span aria-hidden="true">*</span></span>
                <select id="assignmentDepartment" required="required" disabled="disabled"></select>
                </label>
                <p id="assignmentMajorHint" class="assignment-hint"></p>
                <label class="assignment-field" for="assignmentName"><span>Group name (optional)</span><input id="assignmentName" maxlength="120" placeholder="For example: Computer Science - Group A" /></label>
            </div>
            <section aria-labelledby="assignmentStudentsTitle">
                <h3 id="assignmentStudentsTitle">03 &nbsp; Students</h3>
                <p id="assignmentStudentsHint" class="assignment-hint">Students already in another group or assigned to another supervisor cannot be selected.</p>
                <div class="assignment-student-tools">
                    <input id="studentSearch" type="search" placeholder="Search by name or student ID" aria-label="Search eligible students" disabled="disabled" />
                    <button type="button" class="assignment-secondary" id="studentSelectAll" disabled="disabled">Select all</button>
                    <button type="button" class="assignment-secondary" id="studentClear">Clear</button>
                </div>
                <div id="assignmentStudents" class="assignment-students"></div>
                <p id="studentCount" class="assignment-count" aria-live="polite">0 selected</p>
            </section>
        </div>
        <p id="assignmentError" role="alert" class="assignment-error"></p>
        <div class="assignment-dialog-footer"><button type="button" id="assignmentCancel" class="assignment-secondary">Cancel</button><button type="button" id="assignmentSave" class="assignment-primary">Create group</button></div>
    </dialog>
    <script src="../Js_project/Coordinator/Assign_Supervisors.js?v=20261024-group-remove" defer="defer"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/workspace-navigation.js?v=20261024") %>" defer="defer"></script>
</body>
</html>