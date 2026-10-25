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
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261024-workspace" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/workspace-navigation.css?v=20261024-hide-search" rel="stylesheet" />
    <style>
        .panel-tools { display: flex; align-items: center; gap: 12px; margin-left: auto; }
        .select-wrap {
            display: inline-flex; align-items: center; gap: 8px;
            background: #FFF; border: 1px solid var(--border, #DFE3EB);
            border-radius: 10px; padding: 6px 12px; font-size: 0.88rem; color: var(--text, #242844);
        }
        .select-wrap select {
            border: none; outline: none; background: transparent; font-family: inherit; font-size: inherit; color: inherit; cursor: pointer;
        }
    </style>
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-translate" defer></script>
</head>
<body class="coordinator-banner dashboard-direct app-page placements-page shared-workspace-shell">

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
                            <span class="hero-info"><svg class="ic"><use href="#i-building"/></svg>Jordan University of Science and Technology &nbsp;&middot;&nbsp; University-wide Management</span>
                        </div>

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

                    <asp:Label ID="lblMessage" runat="server" CssClass="page-message"></asp:Label>

                    <!-- Table Panel -->
                    <section class="monitor-panel">
                        <div class="monitor-panel-heading" style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px;">
                            <div>
                                <h2 id="directoryTitle">Placement overview</h2>
                                <p style="color: var(--text-secondary); font-size: 0.9rem; margin: 4px 0 0;">One clear view of every student's training journey.</p>
                            </div>

                            <!-- عنصر الفلتر الجديد -->
                            <div class="panel-tools">
                                <label class="select-wrap">
                                    <svg class="ic" style="width: 16px; height: 16px;"><use href="#i-filter"/></svg>
                                    <select id="statusFilter" aria-label="Filter by training status" onchange="filterTable()">
                                        <option value="">All Statuses</option>
                                        <option value="active">Active</option>
                                        <option value="pending">Pending</option>
                                        <option value="completed">Completed</option>
                                    </select>
                                </label>
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
    
    <!-- كود الفلترة المباشر والآمن -->
    <script>
//@ts-nocheck
        function filterTable() {
            var filterVal = document.getElementById("statusFilter").value.toLowerCase();
            var table = document.getElementById("placementsTable");
            var trs = table.getElementsByTagName("tr");

            for (var i = 1; i < trs.length; i++) {
                var tr = trs[i];
                var statusSpan = tr.querySelector(".status-track");
                
                if (statusSpan) {
                    var statusText = statusSpan.textContent.trim().toLowerCase();
                    
                    if (filterVal === "" || statusText.includes(filterVal)) {
                        tr.style.display = "";
                    } else {
                        tr.style.display = "none";
                    }
                }
            }
        }
    </script>
    <script src="<%= ResolveUrl("~/Js_project/Public/workspace-navigation.js?v=20261024") %>" defer="defer"></script>
</body>
</html>