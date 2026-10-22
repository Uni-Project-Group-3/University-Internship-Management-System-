<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MonitorPlacementsProgress.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.MonitorPlacementsProgress" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Monitor Placements &amp; Progress | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/monitor-placements-progress.css" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page placements-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <aside class="dash-sidebar" id="dashboardSidebar" aria-label="Coordinator navigation">
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
                        <li><a href="ManageStudents.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Students</span></a></li>
                        <li><a href="ManageCompanies.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg><span>Manage Companies</span></a></li>
                        <li><a href="MonitorPlacementsProgress.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Placements &amp; Progress</span></a></li>
                        <li><a href="FinalCompletionReview.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Completion Review</span></a></li>
                        <li><a href="ContactMessages.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Contact Messages</span></a></li>
                        <li><a href="Manage_Academic_Supervisor.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage Academic Supervisors</span></a></li>
                        <li><a href="Assign_Supervisors.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Assign Academic Supervisors</span></a></li>
                        <li><a href="CoordinatorAccountProfile.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Coordinator Account</span></a></li>
                    </ul>
                    <div class="dash-side-divider"></div>
                    <ul class="dash-side-nav"></ul>
                </nav>
                <div class="dash-side-promo">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="m2 9 10-5 10 5-10 5-10-5Zm4 3v6c4 3 8 3 12 0v-6m4-3v8"/>
                    </svg>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a href="<%= ResolveUrl("~/Coordinator/ManageStudents.aspx") %>">Add students &rarr;</a>
                </div>
            </aside>
            <button type="button" class="dash-sidebar-backdrop" data-sidebar-close aria-label="Close navigation" tabindex="-1" hidden></button>
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
                        <input type="search" id="dashboardSearch" data-search-target="placementSearch" placeholder="Search for students, companies, or supervisors..." />
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
                                <span class="dash-user-name">Coordinator<span class="dash-role">Internship Coordinator</span></span>
                                <span aria-hidden="true">&#8964;</span>
                            </summary>
                            <div class="dash-profile-menu">
                                <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="12" r="9"/>
                                        <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                    </svg>Help &amp; contact
                                </a>
                                <a href="<%= ResolveUrl("~/Auth/Login.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="8" r="3"/>
                                        <path d="M5 21v-3a7 7 0 0 1 14 0v3"/>
                                    </svg>Sign in
                                </a>
                            </div>
                        </details>
                    </div>
                </header>
                <main class="app-content placements-main" id="placementsApp">
                    <section class="placements-heading">
                        <div>
                            <p class="placements-eyebrow">UNIVERSITY-WIDE OVERSIGHT</p>
                            <h1>Monitor Placements &amp; Progress</h1>
                            <p>Track university-wide internships, training hours, tasks, evaluations, and student progress.</p>
                        </div>
                    </section>
                    
                    <asp:Label ID="lblMessage" runat="server" CssClass="d-block mb-3 text-danger fw-bold"></asp:Label>

                    <section class="placements-stats" aria-label="Placement summary">
                        <article>
                            <span>Total Students</span>
                            <strong><asp:Literal ID="lblTotalStudents" runat="server" Text="0" /></strong>
                            <small>Across all departments</small>
                        </article>
                        <article>
                            <span>Active Internships</span>
                            <strong><asp:Literal ID="lblActiveInternships" runat="server" Text="0" /></strong>
                            <small>Currently in training</small>
                        </article>
                        <article>
                            <span>Completed Tasks</span>
                            <strong><asp:Literal ID="lblCompletedTasks" runat="server" Text="0" /></strong>
                            <small>University wide</small>
                        </article>
                        <article class="placements-stat-risk">
                            <span>At-Risk Internships</span>
                            <strong><asp:Literal ID="lblRiskInternships" runat="server" Text="0" /></strong>
                            <small>Delays or pending follow-up</small>
                        </article>
                    </section>

                    <section class="placements-panel" aria-labelledby="directoryTitle">
                        <div class="placements-panel-heading">
                            <div>
                                <h2 id="directoryTitle">Placement overview</h2>
                                <p>One clear view of every student's training journey.</p>
                            </div>
                        </div>

                        <div class="placements-table-wrap" tabindex="0" role="region" aria-label="Internship placements table">
                            <table class="placements-table">
                                <thead>
                                    <tr>
                                        <th scope="col">Student / ID</th>
                                        <th scope="col">Department</th>
                                        <th scope="col">Company</th>
                                        <th scope="col">Company supervisor</th>
                                        <th scope="col">Academic supervisor</th>
                                        <th scope="col">Training status</th>
                                        <th scope="col">Approved / Required</th>
                                        <th scope="col">Task completion</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptPlacements" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td>
                                                    <strong><%# Eval("StudentName") %></strong><br />
                                                    <small style="color: gray;"><%# Eval("StudentId") %></small>
                                                </td>
                                                <td><%# Eval("Major") %></td>
                                                <td><%# Eval("Company") %></td>
                                                <td><%# Eval("CompanySupervisor") %></td>
                                                <td><%# Eval("AcademicSupervisor") %></td>
                                                <td><span class="placements-chip" style="background:#e9ecef; color:#333;"><%# Eval("TrainingStatus") %></span></td>
                                                <td><strong><%# Eval("ApprovedHours") %></strong> / <%# Eval("RequiredHours") %></td>
                                                <td><strong><%# Eval("TaskPercentage") %>%</strong></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </section>
                    
                    <p class="placements-guidance"><strong>Monitoring, with context.</strong> Task completion and approved hours are separate measures. Final internship approval belongs to the Final Completion Review workflow.</p>
                    <footer class="placements-footer">
                        <span>&copy; 2026 Jordan University of Science and Technology</span>
                        <span>University Internship Management System</span>
                    </footer>
                </main>
            </div>
        </div>
    </form>
    
    <!-- تم حذف سكريبتات Demo الوهمية والإبقاء على سكريبت التنقل -->
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js") %>"></script>
</body>
</html>