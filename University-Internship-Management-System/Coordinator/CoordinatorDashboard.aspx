<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="CoordinatorDashboard.aspx.cs" Inherits="UIMS.Coordinator.CoordinatorDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Coordinator Dashboard | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/coordinator-dashboard.css?v=20261024-modern-ui" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261024-coordinator-banner-2" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner" data-demo-role="admin">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Coordinator) ============ -->
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar"><a class="brand workspace-nav-brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Internship Coordinator</p><nav class="workspace-nav-primary" aria-label="Internship Coordinator pages"><a class="workspace-nav-link active" href="CoordinatorDashboard.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="../Coordinator/UniversityRecords.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4h16v16H4V4Zm4 0v16M4 9h16M4 14h16"/></svg><span>University Records</span></a><a class="workspace-nav-link" href="ManageStudents.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Students</span></a><a class="workspace-nav-link" href="ManageCompanies.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Companies</span></a><a class="workspace-nav-link" href="Manage_Academic_Supervisor.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Academic Supervisors</span></a><a class="workspace-nav-link" href="Assign_Supervisors.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Assign Academic Supervisors</span></a><a class="workspace-nav-link" href="MonitorPlacementsProgress.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>Placements &amp; Progress</span></a><a class="workspace-nav-link" href="ContactMessages.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Contact Messages</span></a><a class="workspace-nav-link" href="SystemOptions.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M20 18h0"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/></svg><span>System Options</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CoordinatorAccountProfile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
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
                                <span class="name"><asp:Label ID="lblUserName" runat="server" /><span class="role">Internship Coordinator</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CoordinatorAccountProfile.aspx" class="dropdown-item"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>

                <!-- ============ CONTENT ============ -->
                <main class="content">

                    <!-- Page header -->
                    <section class="page-head">
                        <div class="page-head-text">
                            <h1 class="greeting"><asp:Label ID="lblGreeting" runat="server" /></h1>
                            <p class="subtitle">An overview of internships across the faculty.</p>
                        </div>
                        <div class="page-head-actions">
                            <a href="ManageStudents.aspx" class="btn-pill"><i class="bi bi-person-plus"></i>Add students</a>
                            <a href="FinalCompletionReview.aspx" class="btn-pill primary"><i class="bi bi-patch-check"></i>Review completions</a>
                        </div>
                    </section>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-mortarboard"></i></div>
                                <div class="stat-label">Students</div>
                                <div class="stat-value"><asp:Label ID="lblStudents" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="ManageStudents.aspx" class="stat-link"><span>Manage students</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-building"></i></div>
                                <div class="stat-label">Companies</div>
                                <div class="stat-value"><asp:Label ID="lblCompanies" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="ManageCompanies.aspx" class="stat-link"><span>Manage companies</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">Opportunities</div>
                                <div class="stat-value"><asp:Label ID="lblOpportunities" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="MonitorPlacementsProgress.aspx" class="stat-link"><span>View placements</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-patch-check"></i></div>
                                <div class="stat-label">Awaiting Approval</div>
                                <div class="stat-value"><asp:Label ID="lblAwaiting" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="FinalCompletionReview.aspx" class="stat-link"><span>Review now</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-main">
                            <div class="hero-tag">Final completion review <i class="bi bi-arrow-up-right"></i></div>
                            <h2>Close the loop with confidence.</h2>
                            <p>Review company and academic evaluations, verify requirements, and issue completion reports.</p>
                            <div class="hero-actions">
                                <a href="FinalCompletionReview.aspx" class="btn-hero">Review completions <i class="bi bi-arrow-right"></i></a>
                                <a href="MonitorPlacementsProgress.aspx" class="btn-hero-ghost"><i class="bi bi-activity"></i>Placements</a>
                            </div>
                        </div>

                        <div class="hero-aside" aria-label="Daily checklist">
                            <div class="hero-aside-title"><span>Today's checklist</span><i class="bi bi-list-check"></i></div>
                            <a href="FinalCompletionReview.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-patch-check"></i></span>
                                <span class="hero-check-text"><strong>Approve completions</strong><small>Both evaluations submitted</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="Assign_Supervisors.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-person-check"></i></span>
                                <span class="hero-check-text"><strong>Assign supervisors</strong><small>Link students to academic supervisors</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="ContactMessages.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-envelope"></i></span>
                                <span class="hero-check-text"><strong>Answer messages</strong><small>Unread contact requests</small></span>
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
                            <p class="section-desc">Jump straight into the work you do most as internship coordinator.</p>
                        </div>

                        <div class="quick-grid">
                            <a href="ManageStudents.aspx" class="quick-tile" style="--i:1">
                                <span class="quick-icon"><i class="bi bi-mortarboard"></i></span>
                                <span class="quick-body"><strong>Manage students</strong><span>Add students and keep their records up to date.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="ManageCompanies.aspx" class="quick-tile tone-info" style="--i:2">
                                <span class="quick-icon"><i class="bi bi-building"></i></span>
                                <span class="quick-body"><strong>Manage companies</strong><span>Approve and maintain partner companies.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="MonitorPlacementsProgress.aspx" class="quick-tile tone-success" style="--i:3">
                                <span class="quick-icon"><i class="bi bi-activity"></i></span>
                                <span class="quick-body"><strong>Placements &amp; progress</strong><span>Track hours, tasks and training status.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="Assign_Supervisors.aspx" class="quick-tile tone-warning" style="--i:4">
                                <span class="quick-icon"><i class="bi bi-person-check"></i></span>
                                <span class="quick-body"><strong>Assign supervisors</strong><span>Link each student to an academic supervisor.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href='<%= ResolveUrl("~/Coordinator/Manage_Academic_Supervisor.aspx") %>' class="quick-tile tone-info" style="--i:5">
                                <span class="quick-icon"><i class="bi bi-person-badge"></i></span>
                                <span class="quick-body"><strong>Academic supervisors</strong><span>Add and manage faculty supervisors.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="ContactMessages.aspx" class="quick-tile tone-success" style="--i:6">
                                <span class="quick-icon"><i class="bi bi-envelope"></i></span>
                                <span class="quick-body"><strong>Contact messages</strong><span>Read and reply to incoming requests.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                        </div>
                    </section>

                    <!-- NEW: Coordination journey -->
                    <section class="section-block" aria-labelledby="workflowTitle">
                        <div class="section-head">
                            <div>
                                <span class="section-eyebrow">How it works</span>
                                <h2 class="section-title" id="workflowTitle">Internship coordination journey</h2>
                            </div>
                            <p class="section-desc">Four stages from setting up the semester to approving final completion.</p>
                        </div>

                        <ol class="workflow">
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">1</span><span class="step-line"></span></div>
                                <h3>Set up</h3>
                                <p>Add students and partner companies for the new semester.</p>
                                <a href="ManageStudents.aspx" class="step-link">Students <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">2</span><span class="step-line"></span></div>
                                <h3>Assign</h3>
                                <p>Give every student an academic supervisor to follow their training.</p>
                                <a href="Assign_Supervisors.aspx" class="step-link">Supervisors <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">3</span><span class="step-line"></span></div>
                                <h3>Monitor</h3>
                                <p>Watch placements, hours and risks across the whole faculty.</p>
                                <a href="MonitorPlacementsProgress.aspx" class="step-link">Progress <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">4</span><span class="step-line"></span></div>
                                <h3>Approve</h3>
                                <p>Verify both evaluations and issue the completion report.</p>
                                <a href="FinalCompletionReview.aspx" class="step-link">Completions <i class="bi bi-arrow-right"></i></a>
                            </li>
                        </ol>
                    </section>



                </main>
            </div>
        </div>
    <dialog id="demoCycleDialog" class="uims-notifications" aria-labelledby="demoCycleTitle"><h2 id="demoCycleTitle">Local demo cycle controls</h2><p>Advance the persisted test clock to check the 60-minute grace period. This does not schedule a real background worker.</p><label>Minutes to advance<input id="demoClockMinutes" type="number" value="61" min="1" max="525600" required /></label><button type="button" id="advanceDemoClock">Advance test clock</button><button type="button" id="exportDemoState">Export local demo records</button><button type="button" data-close>Close</button></dialog></form>


<script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
</body>
</html>
