<%@ Page Async="true" Language="C#" AutoEventWireup="true" CodeBehind="AcademicDashboard.aspx.cs" Inherits="UIMS.Academic.AcademicDashboard" %>

<!DOCTYPE html>
<html lang="en" dir="ltr">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Academic Dashboard | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/academic-dashboard.css?v=20261021-original-ui" rel="stylesheet" />
</head>
<body data-portal="academic">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Academic Supervisor) ============ -->
            <aside class="sidebar" id="sidebar">
<button type="button" class="legacy-sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="AcademicDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Academic pages">
<li><a href="AcademicDashboard.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="StudentProgressMonitor.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Student Progress Monitor</span></a></li>
<li><a href="FinalAcademicEvaluation.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Academic Evaluation</span></a></li>
<li><a href="AcademicSupervisorAccount.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Account</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li></ul>

                <div class="side-promo">
                    <i class="bi bi-journal-check"></i>
                    <p>Review assigned students<br />and their training records.</p>
                    <a href="FinalAcademicEvaluation.aspx">Start evaluating &rarr;</a>
                </div>
            </aside>
            <div class="sidebar-backdrop" onclick="toggleSidebar()"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input type="text" placeholder="Search for students or companies..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" /><span class="role">Academic Supervisor</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="AcademicSupervisorAccount.aspx"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>

                <!-- ============ CONTENT ============ -->
                <main class="content">
                    <div class="date-line"><asp:Label ID="lblDate" runat="server" /></div>
                    <h1 class="greeting"><asp:Label ID="lblGreeting" runat="server" /></h1>
                    <p class="subtitle">Follow your students' training progress and evaluations.</p>
                    <p class="account-status" role="status"><%= System.Web.HttpUtility.HtmlEncode(AccountSummary) %></p>
                    <p role="status"><%= System.Web.HttpUtility.HtmlEncode(DashboardStatus) %></p>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-people"></i></div>
                                <div class="stat-label">My Students</div>
                                <div class="stat-value"><asp:Label ID="lblStudents" runat="server" /></div>
                                <div class="stat-note">Verified Firebase records only</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">In Training</div>
                                <div class="stat-value"><asp:Label ID="lblInTraining" runat="server" /></div>
                                <div class="stat-note">Verified Firebase records only</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon danger"><i class="bi bi-exclamation-triangle"></i></div>
                                <div class="stat-label">Delayed</div>
                                <div class="stat-value"><asp:Label ID="lblDelayed" runat="server" /></div>
                                <div class="stat-note">Verified Firebase records only</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-clipboard-check"></i></div>
                                <div class="stat-label">Evaluations Due</div>
                                <div class="stat-value"><asp:Label ID="lblEvalDue" runat="server" /></div>
                                <div class="stat-note">Evaluation readiness is not configured</div>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-tag">Action needed <i class="bi bi-arrow-up-right"></i></div>
                        <h2>Guide every step of the journey.</h2>
                        <p>Review company evaluations and send your final academic evaluation to the coordinator.</p>
                        <a href="FinalAcademicEvaluation.aspx" class="btn-hero">Open evaluations <i class="bi bi-arrow-right"></i></a>
                    </section>

                    <div class="row g-3">
                        <!-- Students overview -->
                        <div class="col-lg-7">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>My Students</h3>
                                    <a href="StudentProgressMonitor.aspx">View all</a>
                                </div>
                                <%= FirstRecordsHtml %>
                            </div>
                        </div>

                        <!-- Evaluations ready -->
                        <div class="col-lg-5">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Task Follow-up</h3>
                                    <a href="FinalAcademicEvaluation.aspx">View all</a>
                                </div>
                                <%= SecondRecordsHtml %>
                            </div>
                        </div>
                    </div>
                </main>
            </div>
        </div>
    </form>

    <script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
<script src="../Js_project/Academic/academic-dashboard.js?v=20261021-original-ui"></script>
<script src="../Js_project/Public/tab-session.js" defer="defer"></script>
</body>
</html>
