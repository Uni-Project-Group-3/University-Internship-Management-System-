<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentProgressMonitor.aspx.cs" Inherits="University_Internship_Management_System.Academic.StudentProgressMonitor" %>
<%@ Register Src="~/Shared/AcademicSidebar.ascx" TagPrefix="shared" TagName="RoleSidebar" %>
<%@ Register Src="~/Shared/WorkspaceNavbar.ascx" TagPrefix="shared" TagName="WorkspaceNavbar" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <title>Student Progress Monitor | JUST UIMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="stylesheet" type="text/css" href="/Assets/shared/colors.css" />
    <link rel="stylesheet" type="text/css" href="/Assets/academic/progress-monitor.css?v=20261024-details" />
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261024-workspace" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/workspace-navigation.css?v=20261024-hide-search" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-system-banner" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner dashboard-direct app-page monitor-page shared-workspace-shell">

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
            <symbol id="i-clock" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></symbol>
            <symbol id="i-tasks" viewBox="0 0 24 24"><path d="M9 6h11M9 12h11M9 18h11M3.5 6l1.5 1.5L7 5M3.5 12l1.5 1.5L7 11M3.5 18l1.5 1.5L7 17"/></symbol>
            <symbol id="i-star" viewBox="0 0 24 24"><path d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9L12 3Z"/></symbol>
            <symbol id="i-pen" viewBox="0 0 24 24"><path d="M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16v4ZM13.5 6.5l4 4"/></symbol>
            <symbol id="i-filter" viewBox="0 0 24 24"><path d="M3 5h18l-7 8v6l-4-2v-4L3 5Z"/></symbol>
            <symbol id="i-link" viewBox="0 0 24 24"><path d="M14 4h6v6m0-6-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5"/></symbol>
            <symbol id="i-inbox" viewBox="0 0 24 24"><path d="M3 13h5l1.5 3h5L16 13h5M5 5h14l2 8v6H3v-6l2-8Z"/></symbol>
        </defs>
    </svg>

    <form id="form1" runat="server">
        <div class="app-layout">

            <!-- Sidebar -->
            <shared:RoleSidebar ID="RoleNavigation" runat="server" SidebarId="workspaceSidebar" />

            <!-- Main Content -->
            <div class="dash-main">

                <!-- Top bar -->
                <shared:WorkspaceNavbar ID="WorkspaceNavigation" runat="server" SearchInputId="searchInput" SearchInputType="text" SearchPlaceholder="Search students or majors..." AccountLabel="Supervisor" AccountRole="Academic" AccountInitials="AS" AccountUrl="~/Academic/AcademicSupervisorAccount.aspx" SidebarId="workspaceSidebar" />

                <!-- Hero -->
                <div class="hero-banner">
                    <div class="hero-waves" aria-hidden="true"><i></i><i></i><i></i><i></i></div>

                    <div class="hero-text">
                        <span class="hero-badge"><svg class="ic"><use href="#i-chart"/></svg>JUST / ACADEMIC WORKSPACE</span>
                        <h1>Student Progress Monitor</h1>
                        <p>Track your assigned students, monitor tasks, and review training hours securely.</p>
                        <span class="hero-info"><svg class="ic"><use href="#i-building"/></svg>Jordan University of Science and Technology &nbsp;&middot;&nbsp; Academic Supervision</span>
                    </div>

                </div>

                <section class="hero-stats">
                    <article class="monitor-stat">
                        <span class="stat-icon s-blue"><svg class="ic"><use href="#i-users"/></svg></span>
                        <div><strong><asp:Literal ID="lblAssignedStudents" runat="server" Text="0" /></strong><p>Assigned Students</p></div>
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

                <!-- Students table -->
                <section class="monitor-panel">
                    <div class="monitor-panel-heading">
                        <h2>Your assigned students</h2>
                        <span class="count-pill" id="countPill">0</span>
                        <div class="panel-tools">
                            <label class="select-wrap">
                                <svg class="ic"><use href="#i-filter"/></svg>
                                <select id="statusFilter">
                                    <option value="">All statuses</option>
                                    <option value="track">On Track</option>
                                    <option value="behind">Behind Schedule</option>
                                </select>
                            </label>
                        </div>
                    </div>

                    <div class="monitor-table-scroll">
                        <table class="monitor-table" id="studentsTable">
                            <thead>
                                <tr><th>Student</th><th>Major</th><th>Approved Hours</th><th>Task Progress</th><th>Status</th><th>Details</th></tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptStudents" runat="server" OnItemCommand="rptStudents_ItemCommand">
                                    <ItemTemplate>
                                        <tr data-status='<%# Convert.ToBoolean(Eval("HasDelays")) ? "behind" : "track" %>'>
                                            <td>
                                                <div class="student-cell">
                                                    <span class="avatar"><%# string.IsNullOrEmpty((string)Eval("StudentName")) ? "?" : Eval("StudentName").ToString().Trim().Substring(0, 1).ToUpper() %></span>
                                                    <strong><%# Eval("StudentName") %></strong>
                                                </div>
                                            </td>
                                            <td><span class="major-tag"><svg class="ic"><use href="#i-cap"/></svg><%# Eval("Major") %></span></td>
                                            <td>
                                                <span class="monitor-chip"><%# Eval("ApprovedHours") %> / <%# Eval("RequiredHours") %> h</span>
                                               <div class="bar"><span style='<%# "width:" + (Convert.ToInt32(Eval("RequiredHours")) == 0 ? "0" : Math.Min(100, Convert.ToInt32(Eval("ApprovedHours")) * 100 / Convert.ToInt32(Eval("RequiredHours"))).ToString()) + "%" %>'></span></div>
                                            </td>
                                            <td>
                                                <span class="pct"><%# Eval("TaskPercentage") %>%</span>
                                                <div class="bar green"><span style='<%# "width:" + Eval("TaskPercentage") + "%" %>'></span></div>
                                            </td>
                                            <td><%# Convert.ToBoolean(Eval("HasDelays")) ? "<span class='status-behind'><svg class='ic'><use href='#i-alert'/></svg>Behind Schedule</span>" : "<span class='status-track'><svg class='ic'><use href='#i-check'/></svg>On Track</span>" %></td>
                                            <td><asp:LinkButton runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("StudentId") %>' Text="Details" CssClass="monitor-details-button" OnClientClick="if(showStudentDetailsLoading(this)===false)return false;" /></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>

                        <div class="empty-state" id="emptyState">
                            <svg class="ic"><use href="#i-inbox"/></svg>
                            <b>No students found</b>
                            Try a different name or clear the filter.
                        </div>
                    </div>

                    <div class="pager">
                        <button type="button" id="btnPrev" class="monitor-button" onclick="changePage(-1)">Previous</button>
                        <span id="pageInfo">Page 1</span>
                        <button type="button" id="btnNext" class="monitor-button" onclick="changePage(1)">Next</button>
                    </div>
                </section>
            </div>
        </div>

        <!-- Modal -->
        <asp:ScriptManager ID="MonitorScriptManager" runat="server" />
        <asp:UpdatePanel ID="StudentDetailsPanel" runat="server" UpdateMode="Conditional">
        <Triggers><asp:AsyncPostBackTrigger ControlID="rptStudents" EventName="ItemCommand" /></Triggers>
        <ContentTemplate>
        <dialog id="studentDetails" class="modern-modal" runat="server" clientidmode="Static" aria-label="Student progress details">
            <div class="modal-header">
                <div class="modal-id">
                    <span class="modal-avatar"><svg class="ic"><use href="#i-user"/></svg></span>
                    <div>
                        <h2><asp:Literal ID="lblDetName" runat="server" Text="Select a student" /></h2>
                        <span class="subtitle">Student Overview &amp; Progress</span>
                        <asp:HiddenField ID="hfCurrentStudentId" runat="server" />
                    </div>
                </div>
                <button type="button" class="close-btn" onclick="document.getElementById('studentDetails').close();" aria-label="Close student details" title="Close">&#215;</button>
            </div>

            <p class="monitor-details-loading" role="status">Loading current student records…</p>
            <div class="modal-body">
                <div class="info-grid">
                    <div class="info-card">
                        <h3><span class="tile"><svg class="ic"><use href="#i-clock"/></svg></span>Training Hours Progress</h3>
                        <p class="highlight-text">Approved: <span class="highlight-number"><asp:Literal ID="lblDetAppHours" runat="server" Text="0" /></span> / <asp:Literal ID="lblDetReqHours" runat="server" Text="300" /> hours</p>
                    </div>
                    <div class="info-card">
                        <h3><span class="tile"><svg class="ic"><use href="#i-building"/></svg></span>Placement Details</h3>
                        <p class="highlight-text">Company: <strong><asp:Literal ID="lblDetCompany" runat="server" Text="Not Assigned" /></strong></p>
                        <p class="highlight-text">Supervisor: <strong><asp:Literal ID="lblDetCompSup" runat="server" Text="Not Assigned" /></strong></p>
                    </div>
                </div>

                <div class="info-card">
                    <h3><span class="tile"><svg class="ic"><use href="#i-tasks"/></svg></span>Tasks &amp; Submissions</h3>
                    <asp:Repeater ID="rptTasks" runat="server">
                        <HeaderTemplate><table class="task-table"><tr><th>Task Title</th><th>Status</th><th>Score</th><th>Submission</th></tr></HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td class="task-title"><%# Eval("Title") %></td><td><%# Eval("Status") %></td><td><%# Eval("Score") %></td>
                                <td><%# string.IsNullOrEmpty(Eval("SubmissionLink")?.ToString()) ? "<span style='color:#94A3B8; font-size:0.85em;'>No file</span>" : "<a href='" + Eval("SubmissionLink") + "' target='_blank' class='task-link'><svg class='ic' style='width:15px;height:15px'><use href='#i-link'/></svg>View File</a>" %></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate></table></FooterTemplate>
                    </asp:Repeater>
                    <asp:Label ID="lblNoTasks" runat="server" Text="No tasks assigned yet." Visible="false" style="color:#94A3B8; display:block; padding:10px 0;"></asp:Label>
                </div>

                <div class="info-card">
                    <h3><span class="tile"><svg class="ic"><use href="#i-star"/></svg></span>Company Evaluation</h3>
                    <p style="margin:0; color:#64748B;"><asp:Literal ID="lblCompanyEval" runat="server" Text="No evaluations submitted yet." /></p>
                </div>

                <div class="info-card">
                    <h3><span class="tile"><svg class="ic"><use href="#i-pen"/></svg></span>Academic Feedback</h3>
                    <label class="feedback-label">Send your comments and guidance:</label>
                    <asp:TextBox ID="txtFeedback" runat="server" TextMode="MultiLine" Rows="3" CssClass="feedback-box"></asp:TextBox>
                    <div class="action-row">
                        <asp:Button ID="btnSaveFeedback" runat="server" Text="Save Feedback" CssClass="btn-primary" OnClick="btnSaveFeedback_Click" />
                        <asp:Label ID="lblFeedbackMsg" runat="server" Font-Bold="true"></asp:Label>
                    </div>
                </div>
            </div>
        </dialog>
        </ContentTemplate>
        </asp:UpdatePanel>
    </form>

    <!-- Search + filter + pagination script -->
    <script type="text/javascript">
 //@ts-nocheck
        var currentPage = 1;
        var rowsPerPage = 5;
        function showStudentDetailsLoading(button) {
            if (window.monitorDetailsPending) return false;
            window.monitorDetailsPending = true;
            var dialog = document.getElementById('studentDetails');
            dialog.classList.add('is-loading'); dialog.setAttribute('aria-busy','true');
            if (!dialog.open) dialog.showModal();
        }
        if (window.Sys && Sys.WebForms) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function(sender, args) {
            window.monitorDetailsPending = false;
            var dialog = document.getElementById('studentDetails');
            dialog.classList.remove('is-loading'); dialog.removeAttribute('aria-busy');
            if (args.get_error()) { args.set_errorHandled(true); dialog.querySelector('.monitor-details-loading').textContent = 'Details could not be loaded. Close and retry.'; dialog.classList.add('is-loading'); }
        });

        function getRows() {
            var table = document.getElementById("studentsTable");
            var tbody = table ? table.getElementsByTagName("tbody")[0] : null;
            return tbody ? Array.prototype.slice.call(tbody.getElementsByTagName("tr")) : [];
        }

        function getMatches() {
            var searchInput = document.getElementById("searchInput");
            var statusFilter = document.getElementById("statusFilter");
            var q = searchInput ? searchInput.value.trim().toLowerCase() : "";
            var st = statusFilter ? statusFilter.value : "";

            return getRows().filter(function (r) {
                var okText = !q || r.innerText.toLowerCase().indexOf(q) > -1;
                var okStatus = !st || r.getAttribute("data-status") === st;
                return okText && okStatus;
            });
        }

        function setupPagination() {
            var all = getRows();
            var matches = getMatches();
            var maxPage = Math.ceil(matches.length / rowsPerPage) || 1;
            if (currentPage > maxPage) currentPage = maxPage;
            if (currentPage < 1) currentPage = 1;

            all.forEach(function (r) { r.style.display = "none"; });
            matches.forEach(function (r, i) {
                if (i >= (currentPage - 1) * rowsPerPage && i < currentPage * rowsPerPage) r.style.display = "";
            });

            var countPill = document.getElementById("countPill");
            if (countPill) countPill.innerText = matches.length;

            var emptyState = document.getElementById("emptyState");
            if (emptyState) emptyState.style.display = matches.length === 0 ? "block" : "none";

            var pageInfo = document.getElementById("pageInfo");
            if (pageInfo) pageInfo.innerText = "Page " + currentPage + " of " + maxPage;

            var btnPrev = document.getElementById("btnPrev");
            if (btnPrev) btnPrev.disabled = (currentPage === 1);

            var btnNext = document.getElementById("btnNext");
            if (btnNext) btnNext.disabled = (currentPage === maxPage);
        }

        function changePage(step) {
            currentPage += step;
            setupPagination();
        }

        function resetAndRender() { 
            currentPage = 1; 
            setupPagination(); 
        }

        window.onload = function () {
            var s = document.getElementById("searchInput");
            if (s) {
                s.addEventListener("input", resetAndRender);
                s.addEventListener("keydown", function (e) { if (e.key === "Enter") e.preventDefault(); });
            }

            var f = document.getElementById("statusFilter");
            if (f) {
                f.addEventListener("change", resetAndRender);
            }

            setupPagination();
        };
    </script>
    <script src="<%= ResolveUrl("~/Js_project/Public/workspace-navigation.js?v=20261024") %>" defer="defer"></script>
</body>
</html>
