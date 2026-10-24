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
<link href="../Assets/shared/workspace-identity.css?v=20261024-polish" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-polish" defer></script>
</head>
<body class="dashboard-direct app-page assign-supervisors-page shared-workspace-shell">

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
                            <p>Organize students into groups and link each group to an academic supervisor from the beginning or modify assignments when needed.</p>
                        </div>
                        <div class="hero-action">
                            <asp:Button ID="btnSaveAll" runat="server" CssClass="btn-primary-hero" Text="Save All Changes" OnClick="btnSaveAll_Click" />
                        </div>
                    </div>

                    <asp:Label ID="lblMessage" runat="server" CssClass="page-message"></asp:Label>

                    <!-- Assignments Panel -->
                    <section class="monitor-panel">
                        <div class="monitor-panel-heading" style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px;">
                            <div>
                                <h2>Student Groups &amp; Supervisors</h2>
                                <p>Manage group allocations and link academic supervisors.</p>
                            </div>

                            <!-- فلتر التخصصات أو الأقسام -->
                            <div class="panel-tools">
                                <label class="select-wrap">
                                    <svg class="ic" style="width: 16px; height: 16px;"><use href="#i-filter"/></svg>
                                    <select id="departmentFilter" aria-label="Filter by department" onchange="filterGroupsTable()">
                                        <option value="">All Departments</option>
                                        <option value="Software Engineering">Software Engineering</option>
                                        <option value="Computer Science">Computer Science</option>
                                        <option value="Artificial Intelligence">Artificial Intelligence</option>
                                    </select>
                                </label>
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
                                <tbody>
                                    <asp:Repeater ID="rptGroups" runat="server" OnItemCommand="rptGroups_ItemCommand">
                                        <ItemTemplate>
                                            <tr>
                                                <td>
                                                    <span class="group-name"><%# Eval("GroupName") %></span>
                                                    <span class="group-id">ID: <%# Eval("GroupId") %></span>
                                                    <asp:HiddenField ID="hfGroupId" runat="server" Value='<%# Eval("GroupId") %>' />
                                                </td>
                                                <td class="dept-cell"><%# Eval("Major") %></td>
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
        //@ts-nocheck
        function filterGroupsTable() {
            var selectElem = document.getElementById("departmentFilter");
            var table = document.getElementById("groupsTable");
            if (!selectElem || !table) return;

            var filterVal = selectElem.value.toLowerCase().trim();
            var trs = table.getElementsByTagName("tr");

            for (var i = 1; i < trs.length; i++) {
                var tr = trs[i];
                var deptTd = tr.querySelector(".dept-cell");
                
                if (deptTd) {
                    var deptText = deptTd.textContent.trim().toLowerCase();
                    
                    if (filterVal === "" || deptText.indexOf(filterVal) > -1) {
                        tr.style.display = "";
                    } else {
                        tr.style.display = "none";
                    }
                }
            }
        }

        document.addEventListener("DOMContentLoaded", function () {
            const msg = document.getElementById('<%= lblMessage.ClientID %>');
            if (msg && msg.innerText.trim() !== "") {
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
    <script src="<%= ResolveUrl("~/Js_project/Public/workspace-navigation.js?v=20261024") %>" defer="defer"></script>
</body>
</html>