<%@ Page Async="true" Language="C#" AutoEventWireup="true" CodeBehind="Assign_Supervisors.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.Assign_Supervisors" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Assign Academic Supervisors | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
   <link rel="stylesheet" type="text/css" href="../Assets/coordinator/Assign_Supervisors.css?v=2.0" />
</head>
<body class="dashboard-direct app-page assign-supervisors-page">

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
        </defs>
    </svg>

    <form id="form1" runat="server">
        <div class="app-layout">
            
            <!-- Sidebar -->
            <aside class="uims-fixed-sidebar" aria-label="Coordinator navigation">
                <a href="CoordinatorDashboard.aspx" class="dash-brand">
                    <span class="dash-brand-logo">JU</span>
                    <span class="dash-brand-text"><strong>JUST</strong><span>Internships</span></span>
                </a>
                <div class="uims-side-role">Coordinator</div>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav">
                        <li><a class="uims-side-link" href="CoordinatorDashboard.aspx"><svg class="ic"><use href="#i-home"/></svg><span>Dashboard</span></a></li>
                        <li><a class="uims-side-link" href="ManageStudents.aspx"><svg class="ic"><use href="#i-evaluate"/></svg><span>Manage Students</span></a></li>
                        <li><a class="uims-side-link" href="ManageCompanies.aspx"><svg class="ic"><use href="#i-building"/></svg><span>Manage Companies</span></a></li>
                        <li><a class="uims-side-link" href="MonitorPlacementsProgress.aspx"><svg class="ic"><use href="#i-chart"/></svg><span>Placements &amp; Progress</span></a></li>
                        <li><a class="uims-side-link" href="FinalCompletionReview.aspx"><svg class="ic"><use href="#i-report"/></svg><span>Final Completion Review</span></a></li>
                        <li><a class="uims-side-link" href="ContactMessages.aspx"><svg class="ic"><use href="#i-inbox"/></svg><span>Contact Messages</span></a></li>
                        <li><a class="uims-side-link" href="Manage_Academic_Supervisor.aspx"><svg class="ic"><use href="#i-user"/></svg><span>Academic Supervisors</span></a></li>
                        <li><a class="uims-side-link active" href="Assign_Supervisors.aspx"><svg class="ic"><use href="#i-users"/></svg><span>Assign Supervisors</span></a></li>
                    </ul>
                </nav>
            </aside>

            <!-- Main Content -->
            <div class="dash-main">
                <header class="topbar">
                    <label class="topbar-search">
                        <svg class="ic"><use href="#i-search"/></svg>
                        <input type="search" id="searchGroups" placeholder="Search student groups or supervisors..." />
                    </label>
                    <div class="topbar-right">
                        <span class="icon-btn" title="Notifications"><svg class="ic"><use href="#i-bell"/></svg></span>
                        <div class="user-chip">
                            <span class="av">CO</span>
                            <div><b>Coordinator</b><small>Internship Coordinator</small></div>
                        </div>
                    </div>
                </header>

                <main class="app-content">
                    <!-- Hero Banner -->
                    <div class="hero-banner">
                        <div class="hero-text">
                            <span class="hero-badge"><svg class="ic"><use href="#i-users"/></svg>GROUP ASSIGNMENTS</span>
                            <h1>Assign Academic Supervisors</h1>
                            <p>Organize students into groups and link each group to an academic supervisor from the beginning or modify assignments when needed.</p>
                        </div>
                        <div class="hero-action">
                            <asp:Button ID="btnSaveAll" runat="server" CssClass="btn-primary-hero" Text="Save All Changes" OnClick="btnSaveAll_Click" />
                        </div>
                    </div>

                    <asp:Label ID="lblMessage" runat="server" CssClass="page-message"></asp:Label>

                    <!-- Assignments Panel -->
                    <section class="monitor-panel">
                        <div class="monitor-panel-heading">
                            <div>
                                <h2>Student Groups &amp; Supervisors</h2>
                                <p>Manage group allocations and link academic supervisors.</p>
                            </div>
                        </div>

                        <div class="monitor-table-scroll">
                            <table class="monitor-table">
                                <thead>
                                    <tr>
                                        <th>Group ID / Name</th>
                                        <th>Department / Major</th>
                                        <th>Students Count</th>
                                        <th>Assigned Academic Supervisor</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptGroups" runat="server" OnItemCommand="rptGroups_ItemCommand">
                                        <ItemTemplate>
                                            <tr>
                                                <td>
                                                    <span class="group-name"><%# Eval("GroupName") %></span>
                                                    <span class="group-id">ID: <%# Eval("GroupId") %></span>
                                                    <asp:HiddenField ID="hfGroupId" runat="server" Value='<%# Eval("GroupId") %>' />
                                                </td>
                                                <td><%# Eval("Major") %></td>
                                                <td><strong><%# Eval("StudentCount") %></strong> Students</td>
                                                <td>
                                                    <asp:DropDownList ID="ddlSupervisors" runat="server" CssClass="form-control-sm" DataSource='<%# GetSupervisorsList() %>' DataTextField="SupervisorName" DataValueField="SupervisorId" SelectedValue='<%# Eval("AssignedSupervisorId") %>'>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:Button ID="btnUpdateSingle" runat="server" CssClass="btn-action-sm" Text="Update" CommandName="UpdateGroup" CommandArgument='<%# Eval("GroupId") %>' />
                                                </td>
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
  <script>
    document.addEventListener("DOMContentLoaded", function () {
        const msg = document.getElementById('<%= lblMessage.ClientID %>');
        if (msg && msg.innerText.trim() !== "") {
            // إظهار الرسالة وتفعيل اختفائها التلقائي بعد 4 ثوانٍ
            setTimeout(function () {
                msg.style.transition = "all 0.5s ease";
                msg.style.opacity = "0";
                msg.style.transform = "translateY(-15px)";
                setTimeout(function () {
                    msg.style.display = "none";
                }, 500);
            }, 4000);
        }
    });
  </script>
</body>
</html>