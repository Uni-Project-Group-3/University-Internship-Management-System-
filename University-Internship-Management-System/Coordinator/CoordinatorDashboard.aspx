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
</head>
<body data-demo-role="admin">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Coordinator) ============ -->
            <aside class="sidebar" id="sidebar">
<button type="button" class="legacy-sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="CoordinatorDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Coordinator pages">
<li><a href="CoordinatorDashboard.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ManageStudents.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Students</span></a></li>
<li><a href="ManageCompanies.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg><span>Manage Companies</span></a></li>
<li><a href="MonitorPlacementsProgress.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Placements &amp; Progress</span></a></li>
<li><a href="FinalCompletionReview.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Completion Review</span></a></li>
<li><a href="ContactMessages.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Contact Messages</span></a></li>

<li><a href='<%= ResolveUrl("~/Coordinator/Manage_Academic_Supervisor.aspx") %>'><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage Academic Supervisors</span></a></li>

<li><a href="Assign_Supervisors.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Assign Academic Supervisors</span></a></li>

<li><a href="CoordinatorAccountProfile.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Coordinator Account</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li>
</ul>

                <div class="side-promo">
                    <i class="bi bi-person-plus"></i>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a href="ManageStudents.aspx">Add students &rarr;</a>
                </div>
            </aside>
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
