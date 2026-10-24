<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="StudentDashboard.aspx.cs" Inherits="UIMS.Student.StudentDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Dashboard | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/Student/student-dashboard.css?v=20261024-modern-ui" rel="stylesheet" />
</head>
<body data-demo-role="student">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Student) ============ -->
            <aside class="sidebar" id="sidebar">
<button type="button" class="legacy-sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="StudentDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Student pages">
<li><a href="StudentDashboard.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ExploreOpportunities.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Explore Opportunities</span></a></li>
<li><a href="MyApplications.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Applications</span></a></li>
<li><a href="Profile.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M5 21v-3a7 7 0 0 1 14 0v3"/></svg><span>My Profile</span></a></li>

<li><a href="MyTasks.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Tasks</span></a></li>

<li><a href="MyProgressEvaluation.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Progress &amp; Evaluation</span></a></li>
<li><a href="MyAttendance.aspx"><i class="bi bi-qr-code-scan" aria-hidden="true"></i><span>My Attendance</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li>
</ul>

                <div class="side-promo">
                    <i class="bi bi-mortarboard-fill"></i>
                    <p>Build your future<br />with real experience.</p>
                    <a href="ExploreOpportunities.aspx">Explore opportunities &rarr;</a>
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
                                <span class="name"><asp:Label ID="lblUserName" runat="server" /><span class="role">Student</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Profile.aspx"><i class="bi bi-person me-2"></i>My Profile</a></li>
                                <li><a class="dropdown-item" aria-disabled="true" title="This page is not available yet"><i class="bi bi-key me-2"></i>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>

                <!-- ============ CONTENT (redesigned) ============ -->
                <main class="content"><p class="uims-feedback" id="studentCompletionNotice" role="status" hidden></p><button type="button" class="btn-hero completion-btn" id="reviewStudentCompletion" hidden><i class="bi bi-patch-check"></i>Review completion &amp; certificate</button>

                    <!-- Page header -->
                    <section class="page-head">
                        <div class="page-head-text">
                            <h1 class="greeting"><asp:Label ID="lblGreeting" runat="server" /></h1>
                            <p class="subtitle">Your internship journey, all in one place.</p>
                        </div>
                        <div class="page-head-actions">
                            <a href="MyApplications.aspx" class="btn-pill"><i class="bi bi-file-earmark-text"></i>My applications</a>
                            <a href="ExploreOpportunities.aspx" class="btn-pill primary"><i class="bi bi-search"></i>Find internships</a>
                        </div>
                    </section>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-file-earmark-text"></i></div>
                                <div class="stat-label">Applied</div>
                                <div class="stat-value"><asp:Label ID="lblApplied" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="MyApplications.aspx" class="stat-link"><span>View applications</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-clock"></i></div>
                                <div class="stat-label">In Review</div>
                                <div class="stat-value"><asp:Label ID="lblInReview" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="MyApplications.aspx" class="stat-link"><span>Track status</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-check-lg"></i></div>
                                <div class="stat-label">Pending Tasks</div>
                                <div class="stat-value"><asp:Label ID="lblTasks" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="MyTasks.aspx" class="stat-link"><span>Open tasks</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon"><i class="bi bi-hourglass-split"></i></div>
                                <div class="stat-label">Approved Hours</div>
                                <div class="stat-value"><asp:Label ID="lblHours" runat="server" /></div>
                                <div class="stat-note">Live Firebase records</div>
                                <a href="MyAttendance.aspx" class="stat-link"><span>Attendance</span><i class="bi bi-arrow-right"></i></a>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-main">
                            <div class="hero-tag">Featured opportunity <i class="bi bi-arrow-up-right"></i></div>
                            <h2>Find your place in tech.</h2>
                            <p>Discover internships that match your college and major.</p>
                            <div class="hero-actions">
                                <a href="ExploreOpportunities.aspx" class="btn-hero">Explore opportunities <i class="bi bi-arrow-right"></i></a>
                                <a href="ApplicationForm.aspx" class="btn-hero-ghost"><i class="bi bi-pencil-square"></i>Apply now</a>
                            </div>
                        </div>

                        <div class="hero-aside" aria-label="Getting ready checklist">
                            <div class="hero-aside-title"><span>Get ready to apply</span><i class="bi bi-list-check"></i></div>
                            <a href="Profile.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-person-badge"></i></span>
                                <span class="hero-check-text"><strong>Complete your profile</strong><small>Phone, portfolio and skills</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="ApplicationForm.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-file-earmark-person"></i></span>
                                <span class="hero-check-text"><strong>Prepare your CV link</strong><small>Add it to your application form</small></span>
                                <i class="bi bi-chevron-right"></i>
                            </a>
                            <a href="MyTasks.aspx" class="hero-check">
                                <span class="hero-check-icon"><i class="bi bi-kanban"></i></span>
                                <span class="hero-check-text"><strong>Stay on top of tasks</strong><small>Submit work before deadlines</small></span>
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
                            <p class="section-desc">Everything you need during your internship, one click away.</p>
                        </div>

                        <div class="quick-grid">
                            <a href="ExploreOpportunities.aspx" class="quick-tile" style="--i:1">
                                <span class="quick-icon"><i class="bi bi-compass"></i></span>
                                <span class="quick-body"><strong>Explore opportunities</strong><span>Browse roles that match your major.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="ApplicationForm.aspx" class="quick-tile tone-info" style="--i:2">
                                <span class="quick-icon"><i class="bi bi-pencil-square"></i></span>
                                <span class="quick-body"><strong>Application form</strong><span>Apply with your CV and motivation.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="MyApplications.aspx" class="quick-tile tone-warning" style="--i:3">
                                <span class="quick-icon"><i class="bi bi-hourglass-split"></i></span>
                                <span class="quick-body"><strong>My applications</strong><span>See which applications are under review.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="MyTasks.aspx" class="quick-tile tone-success" style="--i:4">
                                <span class="quick-icon"><i class="bi bi-check2-square"></i></span>
                                <span class="quick-body"><strong>My tasks</strong><span>Work on and submit assigned tasks.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="MyAttendance.aspx" class="quick-tile tone-info" style="--i:5">
                                <span class="quick-icon"><i class="bi bi-qr-code-scan"></i></span>
                                <span class="quick-body"><strong>My attendance</strong><span>Show your QR code and check your hours.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                            <a href="MyProgressEvaluation.aspx" class="quick-tile tone-success" style="--i:6">
                                <span class="quick-icon"><i class="bi bi-graph-up-arrow"></i></span>
                                <span class="quick-body"><strong>Progress &amp; evaluation</strong><span>Follow your evaluations and feedback.</span></span>
                                <i class="bi bi-arrow-up-right quick-arrow"></i>
                            </a>
                        </div>
                    </section>

                    <!-- NEW: Internship journey -->
                    <section class="section-block" aria-labelledby="workflowTitle">
                        <div class="section-head">
                            <div>
                                <span class="section-eyebrow">How it works</span>
                                <h2 class="section-title" id="workflowTitle">Your internship journey</h2>
                            </div>
                            <p class="section-desc">Four steps from setting up your profile to receiving your completion certificate.</p>
                        </div>

                        <ol class="workflow">
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">1</span><span class="step-line"></span></div>
                                <h3>Prepare</h3>
                                <p>Complete your profile so companies can learn about your skills.</p>
                                <a href="Profile.aspx" class="step-link">My profile <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">2</span><span class="step-line"></span></div>
                                <h3>Apply</h3>
                                <p>Find a suitable opportunity and send your application.</p>
                                <a href="ExploreOpportunities.aspx" class="step-link">Opportunities <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">3</span><span class="step-line"></span></div>
                                <h3>Train</h3>
                                <p>Attend daily, complete your tasks and collect approved hours.</p>
                                <a href="MyTasks.aspx" class="step-link">My tasks <i class="bi bi-arrow-right"></i></a>
                            </li>
                            <li class="workflow-step">
                                <div class="step-marker"><span class="step-num">4</span><span class="step-line"></span></div>
                                <h3>Complete</h3>
                                <p>Receive your evaluations and confirm your internship completion.</p>
                                <a href="MyProgressEvaluation.aspx" class="step-link">My progress <i class="bi bi-arrow-right"></i></a>
                            </li>
                        </ol>
                    </section>



                </main>
            </div>
        </div>
    <dialog id="studentCompletionDialog" class="uims-completion-dialog" aria-labelledby="studentCompletionTitle"><header><h2 id="studentCompletionTitle">Your internship completion</h2><button type="button" data-close aria-label="Close completion notice">&times;</button></header><div><p id="studentCompletionInfo"></p><p id="studentCompletionWarning">Confirming makes a demonstration certificate available and starts a persisted 60-minute grace period. This simulation checks expiry on access; it is not a real background scheduler.</p><button type="button" id="confirmStudentCompletion">Confirm Completion</button><button type="button" id="reviewCompletionLater" data-close>Review Later</button><button type="button" id="downloadStudentCertificate">Download demo certificate</button><button type="button" id="printStudentCertificate">Print demo certificate</button></div></dialog></form>


<script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-original-ui") %>"></script>
</body>
</html>
