<%@ Page Async="true" Language="C#" AutoEventWireup="true" CodeBehind="MonitorPlacementsProgress.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.MonitorPlacementsProgress" %>
<%@ Register Src="~/Shared/CoordinatorSidebar.ascx" TagPrefix="shared" TagName="RoleSidebar" %>
<%@ Register Src="~/Shared/WorkspaceNavbar.ascx" TagPrefix="shared" TagName="WorkspaceNavbar" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Monitor Placements &amp; Progress | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/monitor-placements-progress.css" rel="stylesheet" />
    <!-- DataTables CSS -->
<link rel="stylesheet" href="https://cdn.datatables.net/1.13.6/css/jquery.dataTables.min.css" />
<!-- jQuery (مطلوب لعمل DataTables) -->
<script src="https://code.jquery.com/jquery-3.7.0.min.js"></script>
<!-- DataTables JS -->
<script src="https://cdn.datatables.net/1.13.6/js/jquery.dataTables.min.js"></script>
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261024-workspace" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/workspace-navigation.css?v=20261024-hide-search" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page placements-page shared-workspace-shell">

    <!-- Icon sprite -->
    <svg width="0" height="0" style="position:absolute" aria-hidden="true">
        <defs>
            <symbol id="i-home" viewBox="0 0 24 24"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></symbol>
            <symbol id="i-chart" viewBox="0 0 24 24"><path d="M4 20V5m0 15h16M8 16v-5m5 5V7m5 9v-8"/></symbol>
            <symbol id="i-report" viewBox="0 0 24 24"><path d="M7 3h8l4 4v14H7zM15 3v5h4M10 13h6m-6 4h6"/></symbol>
            <symbol id="i-evaluate" viewBox="0 0 24 24"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></symbol>
            <symbol id="i-user" viewBox="0 0 24 24"><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></symbol>
            <symbol id="i-logout" viewBox="0 0 24 24"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></symbol>
            <symbol id="i-search" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></symbol>
            <symbol id="i-bell" viewBox="0 0 24 24"><path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10 21a2 2 0 0 0 4 0"/></symbol>
            <symbol id="i-users" viewBox="0 0 24 24"><circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0M16 4.6a3.5 3.5 0 0 1 0 6.8M18 14.2a6.5 6.5 0 0 1 3.5 5.8"/></symbol>
            <symbol id="i-briefcase" viewBox="0 0 24 24"><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 13h18"/></symbol>
            <symbol id="i-check" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></symbol>
            <symbol id="i-alert" viewBox="0 0 24 24"><path d="M12 3 2 20h20L12 3ZM12 10v4m0 3h.01"/></symbol>
            <symbol id="i-building" viewBox="0 0 24 24"><path d="M3 21h18M5 21V8l7-4 7 4v13M9 12h2m2 0h2M9 16h2m2 0h2"/></symbol>
            <symbol id="i-cap" viewBox="0 0 24 24"><path d="m2 9 10-5 10 5-10 5L2 9Z"/><path d="M6 11.5V16c0 1.5 2.7 3 6 3s6-1.5 6-3v-4.5M22 9v6"/></symbol>
            <symbol id="i-filter" viewBox="0 0 24 24"><path d="M3 5h18l-7 8v6l-4-2v-4L3 5Z"/></symbol>
            <symbol id="i-inbox" viewBox="0 0 24 24"><path d="M3 13h5l1.5 3h5L16 13h5M5 5h14l2 8v6H3v-6l2-8Z"/></symbol>
        </defs>
    </svg>

    <form id="form1" runat="server">
        <div class="app-layout">
            
            <!-- Sidebar -->
            <shared:RoleSidebar ID="RoleNavigation" runat="server" SidebarId="workspaceSidebar" />

            <!-- Main Content -->
            <div class="dash-main">
                <shared:WorkspaceNavbar ID="WorkspaceNavigation" runat="server" SearchInputId="dashboardSearch" SearchInputType="search" SearchPlaceholder="Search for students, companies, or supervisors..." AccountLabel="Coordinator" AccountRole="Internship Coordinator" AccountInitials="CO" AccountUrl="~/Coordinator/CoordinatorAccountProfile.aspx" SidebarId="workspaceSidebar" />

                <main class="app-content placements-main">
                    <!-- Hero Banner -->
                    <div class="hero-banner">
                        <div class="hero-waves" aria-hidden="true"><i></i><i></i><i></i><i></i></div>
                        <div class="hero-text">
                            <span class="hero-badge"><svg class="ic"><use href="#i-chart"/></svg>UNIVERSITY-WIDE OVERSIGHT</span>
                            <h1>Monitor Placements &amp; Progress</h1>
                            <p>Track university-wide internships, training hours, tasks, evaluations, and student progress.</p>
                            <span class="hero-info"><svg class="ic"><use href="#i-building"/></svg>Jordan University of Science and Technology &nbsp;·&nbsp; University-wide Management</span>
                        </div>

                        <section class="hero-stats">
                            <article class="monitor-stat">
                                <span class="stat-icon s-blue"><svg class="ic"><use href="#i-users"/></svg></span>
                                <div><strong><asp:Literal ID="lblTotalStudents" runat="server" Text="0" /></strong><p>Total Students</p></div>
                            </article>
                            <article class="monitor-stat">
                                <span class="stat-icon s-amber"><svg class="ic"><use href="#i-briefcase"/></svg></span>
                                <div><strong><asp:Literal ID="lblActiveInternships" runat="server" Text="0" /></strong><p>Active Internships</p></div>
                            </article>
                            <article class="monitor-stat">
                                <span class="stat-icon s-green"><svg class="ic"><use href="#i-check"/></svg></span>
                                <div><strong><asp:Literal ID="lblCompletedTasks" runat="server" Text="0" /></strong><p>Completed Tasks</p></div>
                            </article>
                        </section>
                    </div>

                    <asp:Label ID="lblMessage" runat="server" CssClass="page-message"></asp:Label>

                    <!-- Table Panel -->
                    <section class="monitor-panel">
                        <div class="monitor-panel-heading">
                            <div>
                                <h2 id="directoryTitle">Placement overview</h2>
                                <p style="color: var(--text-secondary); font-size: 0.9rem; margin: 4px 0 0;">One clear view of every student's training journey.</p>
                            </div>
                        </div>

                        <div class="monitor-table-wrap">
                            <table class="monitor-table" id="placementsTable">
                                <thead>
                                    <tr>
                                        <th>Student ID</th>
                                        <th>Department</th>
                                        <th>Company</th>
                                        <th>Company supervisor</th>
                                        <th>Academic supervisor</th>
                                        <th>Training status</th>
                                        <th>Progress Hours</th>
                                        <th>Task completion</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptPlacements" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td>
                                                    <div class="student-cell">
                                                        <div>
                                                            <strong style="color: var(--dark); font-weight: 700;"><%# Eval("StudentName") %></strong><br />
                                                            <small style="color: var(--muted);"><%# Eval("StudentId") %></small>
                                                        </div>
                                                    </div>
                                                </td>
                                                <td><%# Eval("Major") %></td>
                                                <td><%# Eval("Company") %></td>
                                                <td><%# Eval("CompanySupervisor") %></td>
                                                <td><%# Eval("AcademicSupervisor") %></td>
                                                <td>
                                                    <span class="status-track">
                                                        <%# Eval("TrainingStatus") %>
                                                    </span>
                                                </td>
                                                <td><strong><%# Eval("ApprovedHours") %></strong> / <%# Eval("RequiredHours") %></td>
                                                <td><strong><%# Eval("TaskPercentage") %>%</strong></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </section>
                </main>
            </div>
        </div>
    </form>
    
    <script src="<%= ResolveUrl("~/Js_project/Public/workspace-navigation.js?v=20261024") %>" defer="defer"></script>
</body>
</html>