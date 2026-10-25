<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="CompanyDashboard.aspx.cs" Inherits="UIMS.Company.CompanyDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Company Dashboard | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/Company/company-dashboard.css?v=20261024-modern-ui" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
<body class="coordinator-banner" data-demo-role="company">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar"><a class="brand workspace-nav-brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Company Supervisor</p><nav class="workspace-nav-primary" aria-label="Company Supervisor pages"><a class="workspace-nav-link active" href="CompanyDashboard.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ManageOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Opportunities</span></a><a class="workspace-nav-link" href="ApplicationsReview.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Applications Review</span></a><a class="workspace-nav-link" href="MyTrainees.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Trainees</span></a><a class="workspace-nav-link" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Manage Tasks</span></a><a class="workspace-nav-link" href="ManageExams.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Task Exams</span></a><a class="workspace-nav-link" href="VerifyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3h6v6H3V3Zm12 0h6v6h-6V3ZM3 15h6v6H3v-6Zm12 0h2v2h-2v-2Zm4 0h2v6h-6v-2h4v-4Z"/></svg><span>Verify Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Reports</span></a><a class="workspace-nav-link" href="ReportsFinalEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Final Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CompanySupervisorAccount.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <div class="sidebar-backdrop" onclick="toggleSidebar()"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>

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

                <!-- ============ CONTENT (redesigned) ============ -->
                <main class="content">

                    <!-- Page header -->
                    <section class="page-head">
                        <div class="page-head-text">
                            <h1 class="greeting"><asp:Label ID="lblGreeting" runat="server" /></h1>
                            <p class="subtitle">Manage your opportunities, trainees, and evaluations.</p>
                        </div>
                        <div class="page-head-actions">
                            <a href="ApplicationsReview.aspx" class="btn-pill"><i class="bi bi-inbox"></i>Review applications</a>
                            <a href="ManageOpportunities.aspx" class="btn-pill primary"><i class="bi bi-plus-lg"></i>Post opportunity</a>
                        </div>
                    </section>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">Open Opportunities</div>
                                <div class="stat-value"><asp:Label ID="lblOpportunities" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="ManageOpportunities.aspx" class="stat-link"><span>Manage</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-inbox"></i></div>
                                <div class="stat-label">New Applications</div>
                                <div class="stat-value"><asp:Label ID="lblApplications" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="ApplicationsReview.aspx" class="stat-link"><span>Review</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-people"></i></div>
                                <div class="stat-label">Active Trainees</div>
                                <div class="stat-value"><asp:Label ID="lblTrainees" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="MyTrainees.aspx" class="stat-link"><span>View trainees</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon warning"><i class="bi bi-hourglass-split"></i></div>
                                <div class="stat-label">To Review</div>
                                <div class="stat-value"><asp:Label ID="lblToReview" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="ManageTasks.aspx" class="stat-link"><span>Open tasks</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-main">
                            <div class="hero-tag">Quick action <i class="bi bi-arrow-up-right"></i></div>
                            <h2>Shape the next generation.</h2>
                            <p>Create a task for your trainees with clear deadlines and evaluation criteria.</p>
                            <div class="hero-actions">
                                <a href="ManageTasks.aspx" class="btn-hero">Create a task <i class="bi bi-arrow-right"></i></a>
                                <a href="ReportsFinalEvaluation.aspx" class="btn-hero-ghost"><i class="bi bi-clipboard-check"></i>Evaluations</a>
                            </div>
                        </div>

                        <div class="hero-aside" aria-label="Daily checklist">
                            <div class="hero-aside-title"><span>Today's checklist</span><i class="bi bi-list-check"></i></div>
                            <a href="ApplicationsReview.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-person-check"></i></span>
                                <span class="hero-check-text"><strong>Screen new applicants</strong><small>Shortlist or respond to candidates</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="ManageTasks.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-chat-square-text"></i></span>
                                <span class="hero-check-text"><strong>Give task feedback</strong><small>Review pending submissions</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="VerifyAttendance.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-qr-code-scan"></i></span>
                                <span class="hero-check-text"><strong>Verify attendance</strong><small>Scan today's trainee check-ins</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                        </div>
                    </section>

                    <!-- NEW: Quick actions -->
                    <section class="section-block" aria-labelledby="quickActionsTitle">
                        <div class="section-head">
                            <div>
                                <span class="section-eyebrow">Shortcuts</span>
                                <h2 class="section-title" id="quickActionsTitle">Quick actions</h2>
                            </div>
                            <p class="section-desc">Jump straight into the work you do most as a company supervisor.</p>
                        </div>

                        <div class="quick-grid">
                            <a href="ManageOpportunities.aspx" class="quick-tile" style="--i:1">
                                <span class="quick-icon"><i class="bi bi-megaphone"></i></span>
                                <span class="quick-body"><strong>Post an opportunity</strong><span>Publish a new internship role for JUST students.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="ApplicationsReview.aspx" class="quick-tile tone-info" style="--i:2">
                                <span class="quick-icon"><i class="bi bi-file-earmark-person"></i></span>
                                <span class="quick-body"><strong>Review applications</strong><span>Accept, reject, or shortlist applicants.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="MyTrainees.aspx" class="quick-tile tone-success" style="--i:3">
                                <span class="quick-icon"><i class="bi bi-people"></i></span>
                                <span class="quick-body"><strong>My trainees</strong><span>Track hours, progress, and placement status.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="ManageTasks.aspx" class="quick-tile tone-warning" style="--i:4">
                                <span class="quick-icon"><i class="bi bi-kanban"></i></span>
                                <span class="quick-body"><strong>Manage tasks</strong><span>Assign work and review trainee submissions.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="VerifyAttendance.aspx" class="quick-tile tone-info" style="--i:5">
                                <span class="quick-icon"><i class="bi bi-qr-code-scan"></i></span>
                                <span class="quick-body"><strong>Verify attendance</strong><span>Confirm trainee presence with a QR scan.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="ReportsFinalEvaluation.aspx" class="quick-tile tone-success" style="--i:6">
                                <span class="quick-icon"><i class="bi bi-award"></i></span>
                                <span class="quick-body"><strong>Final evaluation</strong><span>Submit reports and close out internships.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                        </div>
                    </section>

                    <!-- NEW: Supervision workflow -->
                    <section class="section-block" aria-labelledby="workflowTitle">
                        <div class="section-head">
                            <div>
                                <span class="section-eyebrow">How it works</span>
                                <h2 class="section-title" id="workflowTitle">Supervision journey</h2>
                            </div>
                            <p class="section-desc">Four stages from posting a role to signing off a trainee's final evaluation.</p>
                        </div>

                        <ol class="workflow">
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">1</span><span class="step-line"></span></div>
                                <h3>Publish</h3>
                                <p>Describe the role, required skills, and duration of the internship.</p>
                                <a href="ManageOpportunities.aspx" class="step-link">Opportunities <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">2</span><span class="step-line"></span></div>
                                <h3>Select</h3>
                                <p>Review applicants' profiles and accept the best-fit students.</p>
                                <a href="ApplicationsReview.aspx" class="step-link">Applications <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">3</span><span class="step-line"></span></div>
                                <h3>Mentor</h3>
                                <p>Assign tasks, verify attendance, and give timely feedback.</p>
                                <a href="ManageTasks.aspx" class="step-link">Tasks <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">4</span><span class="step-line"></span></div>
                                <h3>Evaluate</h3>
                                <p>Complete the final report so the university can approve completion.</p>
                                <a href="ReportsFinalEvaluation.aspx" class="step-link">Evaluation <i class="bi bi-arrow-right"></i></a>
                            </li>
                        </ol>
                    </section>



                </main>
            </div>
        </div>
    </form>


<script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
</body>
</html>
