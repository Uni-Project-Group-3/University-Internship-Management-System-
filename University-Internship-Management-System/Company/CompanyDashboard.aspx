<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CompanyDashboard.aspx.cs" Inherits="UIMS.Company.CompanyDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Company Dashboard | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/Company/company-dashboard.css?v=20261021-original-ui" rel="stylesheet" />
</head>
<body data-demo-role="company">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar" id="sidebar">
<button type="button" class="legacy-sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="CompanyDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Company pages">
<li><a href="CompanyDashboard.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ManageOpportunities.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Opportunities</span></a></li>
<li><a href="ApplicationsReview.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Applications Review</span></a></li>
<li><a href="MyTrainees.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M5 21v-3a7 7 0 0 1 14 0v3"/></svg><span>My Trainees</span></a></li>

<li><a href="ManageTasks.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage &amp; Review Tasks</span></a></li>

<li><a href="ReportsFinalEvaluation.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Reports &amp; Final Evaluation</span></a></li>

<li><a href="CompanySupervisorAccount.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Supervisor Account</span></a></li>
<li><a href="VerifyAttendance.aspx"><i class="bi bi-qr-code-scan" aria-hidden="true"></i><span>Verify Attendance</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li>
</ul>

                <div class="side-promo">
                    <i class="bi bi-building-add"></i>
                    <p>Have a new role?<br />Reach JUST students today.</p>
                    <a href="ManageOpportunities.aspx">Post opportunity &rarr;</a>
                </div>
            </aside>
            <div class="sidebar-backdrop" onclick="toggleSidebar()"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input type="text" placeholder="Search for trainees, tasks, or applications..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" /><span class="role">Company Supervisor</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CompanySupervisorAccount.aspx" class="dropdown-item"><i class="bi bi-person me-2"></i>Account</a></li>
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
                    <p class="subtitle">Manage your opportunities, trainees, and evaluations.</p>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">Open Opportunities</div>
                                <div class="stat-value"><asp:Label ID="lblOpportunities" runat="server" /></div>
                                <div class="stat-note">12 seats available</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-inbox"></i></div>
                                <div class="stat-label">New Applications</div>
                                <div class="stat-value"><asp:Label ID="lblApplications" runat="server" /></div>
                                <div class="stat-note">Awaiting your decision</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-people"></i></div>
                                <div class="stat-label">Active Trainees</div>
                                <div class="stat-value"><asp:Label ID="lblTrainees" runat="server" /></div>
                                <div class="stat-note">Across 3 opportunities</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon warning"><i class="bi bi-hourglass-split"></i></div>
                                <div class="stat-label">To Review</div>
                                <div class="stat-value"><asp:Label ID="lblToReview" runat="server" /></div>
                                <div class="stat-note">Submissions pending</div>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-tag">Quick action <i class="bi bi-arrow-up-right"></i></div>
                        <h2>Shape the next generation.</h2>
                        <p>Create a task for your trainees with clear deadlines and evaluation criteria.</p>
                        <a href="ManageTasks.aspx" class="btn-hero">Create a task <i class="bi bi-arrow-right"></i></a>
                    </section>

                    <div class="row g-3">
                        <!-- Latest applications -->
                        <div class="col-lg-7">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Latest Applications</h3>
                                    <a href="ApplicationsReview.aspx">View all</a>
                                </div>
                                <div class="list-row">
                                    <div class="avatar">LH</div>
                                    <div class="row-body">
                                        <div class="row-title">Layla Haddad</div>
                                        <div class="row-sub">Frontend Developer Intern · Computer Science</div>
                                    </div>
                                    <div class="row-meta"><span class="status warning">Pending</span><div class="when">Oct 19, 2026</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="avatar">OK</div>
                                    <div class="row-body">
                                        <div class="row-title">Omar Khalil</div>
                                        <div class="row-sub">Data Analyst Intern · Software Engineering</div>
                                    </div>
                                    <div class="row-meta"><span class="status warning">Pending</span><div class="when">Oct 18, 2026</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="avatar">RS</div>
                                    <div class="row-body">
                                        <div class="row-title">Rana Saleh</div>
                                        <div class="row-sub">Frontend Developer Intern · Computer Engineering</div>
                                    </div>
                                    <div class="row-meta"><span class="status success">Accepted</span><div class="when">Oct 15, 2026</div></div>
                                </div>
                            </div>
                        </div>

                        <!-- Submissions to review -->
                        <div class="col-lg-5">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Submissions to Review</h3>
                                    <a href="ManageTasks.aspx">View all</a>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-file-earmark-code"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Login Page UI</div>
                                        <div class="row-sub">Ahmad Saleem · resubmitted</div>
                                    </div>
                                    <a href="ManageTasks.aspx" class="btn btn-soft">Review</a>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-journal-text"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Weekly Report #2</div>
                                        <div class="row-sub">Rana Saleh · Oct 18</div>
                                    </div>
                                    <a href="ManageTasks.aspx" class="btn btn-soft">Review</a>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-clock-history"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Hours log · 16 h</div>
                                        <div class="row-sub">Ahmad Saleem · week 3</div>
                                    </div>
                                    <a href="MyTrainees.aspx" class="btn btn-soft">Approve</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </main>
            </div>
        </div>
    </form>

    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
<script src="../Js_project/Company/company-dashboard.js?v=20261021-original-ui"></script>
</body>
</html>
