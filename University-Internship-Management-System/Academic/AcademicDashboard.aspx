<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="AcademicDashboard.aspx.cs" Inherits="UIMS.Academic.AcademicDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Academic Dashboard | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/academic-dashboard.css?v=20261024-modern-ui" rel="stylesheet" />
</head>
<body data-demo-role="academic">
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
<li><a href="WeeklyReports.aspx"><span>Weekly Reports</span></a></li>
<li><a href="FinalAcademicEvaluation.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Academic Evaluation</span></a></li>
<li><a href="AcademicSupervisorAccount.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Account</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li></ul>

                <div class="side-promo">
                    <i class="bi bi-journal-check"></i>
                    <p>2 company evaluations<br />are ready for your review.</p>
                    <a href="FinalAcademicEvaluation.aspx">Start evaluating &rarr;</a>
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

                <!-- ============ CONTENT (redesigned) ============ -->
                <main class="content">

                    <!-- Page header -->
                    <section class="page-head">
                        <div class="page-head-text">
                            <h1 class="greeting"><asp:Label ID="lblGreeting" runat="server" /></h1>
                            <p class="subtitle">Follow your students' training progress and evaluations.</p>
                        </div>
                        <div class="page-head-actions">
                            <a href="WeeklyReports.aspx" class="btn-pill"><i class="bi bi-journal-text"></i>Weekly reports</a>
                            <a href="StudentProgressMonitor.aspx" class="btn-pill primary"><i class="bi bi-activity"></i>Monitor students</a>
                        </div>
                    </section>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-people"></i></div>
                                <div class="stat-label">My Students</div>
                                <div class="stat-value"><asp:Label ID="lblStudents" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="StudentProgressMonitor.aspx" class="stat-link"><span>View students</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">In Training</div>
                                <div class="stat-value"><asp:Label ID="lblInTraining" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="StudentProgressMonitor.aspx" class="stat-link"><span>Track progress</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon danger"><i class="bi bi-exclamation-triangle"></i></div>
                                <div class="stat-label">Delayed</div>
                                <div class="stat-value"><asp:Label ID="lblDelayed" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="StudentProgressMonitor.aspx" class="stat-link"><span>Follow up</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-clipboard-check"></i></div>
                                <div class="stat-label">Evaluations Due</div>
                                <div class="stat-value"><asp:Label ID="lblEvalDue" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="FinalAcademicEvaluation.aspx" class="stat-link"><span>Evaluate now</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-main">
                            <div class="hero-tag">Action needed <i class="bi bi-arrow-up-right"></i></div>
                            <h2>Guide every step of the journey.</h2>
                            <p>Review company evaluations and send your final academic evaluation to the coordinator.</p>
                            <div class="hero-actions">
                                <a href="FinalAcademicEvaluation.aspx" class="btn-hero">Open evaluations <i class="bi bi-arrow-right"></i></a>
                                <a href="WeeklyReports.aspx" class="btn-hero-ghost"><i class="bi bi-journal-text"></i>Weekly reports</a>
                            </div>
                        </div>

                        <div class="hero-aside" aria-label="Daily checklist">
                            <div class="hero-aside-title"><span>Today's checklist</span><i class="bi bi-list-check"></i></div>
                            <a href="StudentProgressMonitor.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-exclamation-diamond"></i></span>
                                <span class="hero-check-text"><strong>Check delayed students</strong><small>Hours, tasks and risk indicators</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="WeeklyReports.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-journal-text"></i></span>
                                <span class="hero-check-text"><strong>Read weekly reports</strong><small>Leave feedback for your students</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="FinalAcademicEvaluation.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-clipboard-check"></i></span>
                                <span class="hero-check-text"><strong>Finish evaluations</strong><small>Review company assessments</small></span>
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
                            <p class="section-desc">Jump straight into the work you do most as an academic supervisor.</p>
                        </div>

                        <div class="quick-grid">
                            <a href="StudentProgressMonitor.aspx" class="quick-tile" style="--i:1">
                                <span class="quick-icon"><i class="bi bi-activity"></i></span>
                                <span class="quick-body"><strong>Progress monitor</strong><span>See hours, tasks and status for each student.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="WeeklyReports.aspx" class="quick-tile tone-info" style="--i:2">
                                <span class="quick-icon"><i class="bi bi-journal-text"></i></span>
                                <span class="quick-body"><strong>Weekly reports</strong><span>Read student reports and add feedback.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="FinalAcademicEvaluation.aspx" class="quick-tile tone-success" style="--i:3">
                                <span class="quick-icon"><i class="bi bi-award"></i></span>
                                <span class="quick-body"><strong>Final evaluation</strong><span>Review company assessments and submit yours.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="StudentProgressMonitor.aspx" class="quick-tile tone-warning" style="--i:4">
                                <span class="quick-icon"><i class="bi bi-exclamation-triangle"></i></span>
                                <span class="quick-body"><strong>Delayed students</strong><span>Follow up with students who are falling behind.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="AcademicSupervisorAccount.aspx" class="quick-tile tone-info" style="--i:5">
                                <span class="quick-icon"><i class="bi bi-person-gear"></i></span>
                                <span class="quick-body"><strong>My account</strong><span>Update your office hours and contact details.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="../Contact.aspx" class="quick-tile tone-success" style="--i:6">
                                <span class="quick-icon"><i class="bi bi-life-preserver"></i></span>
                                <span class="quick-body"><strong>Help &amp; contact</strong><span>Reach the internship coordinator.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                        </div>
                    </section>

                    <!-- NEW: Supervision journey -->
                    <section class="section-block" aria-labelledby="workflowTitle">
                        <div class="section-head">
                            <div>
                                <span class="section-eyebrow">How it works</span>
                                <h2 class="section-title" id="workflowTitle">Academic supervision journey</h2>
                            </div>
                            <p class="section-desc">Four stages from following your students to sending the final evaluation to the coordinator.</p>
                        </div>

                        <ol class="workflow">
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">1</span><span class="step-line"></span></div>
                                <h3>Monitor</h3>
                                <p>Follow each student's hours, tasks and training status.</p>
                                <a href="StudentProgressMonitor.aspx" class="step-link">Progress <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">2</span><span class="step-line"></span></div>
                                <h3>Guide</h3>
                                <p>Read weekly reports and give feedback that keeps students on track.</p>
                                <a href="WeeklyReports.aspx" class="step-link">Reports <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">3</span><span class="step-line"></span></div>
                                <h3>Review</h3>
                                <p>Check the company supervisor's final assessment for each student.</p>
                                <a href="FinalAcademicEvaluation.aspx" class="step-link">Assessments <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">4</span><span class="step-line"></span></div>
                                <h3>Evaluate</h3>
                                <p>Submit your academic evaluation so the coordinator can approve completion.</p>
                                <a href="FinalAcademicEvaluation.aspx" class="step-link">Evaluate <i class="bi bi-arrow-right"></i></a>
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
