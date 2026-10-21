<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentDashboard.aspx.cs" Inherits="UIMS.Student.StudentDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Student Dashboard | JUST Internships</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link href="~/Assets/Student/student-dashboard.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Student) ============ -->
            <aside class="sidebar" id="sidebar">
                <a href="StudentDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav">
                    <li><a href="StudentDashboard.aspx" class="active"><i class="bi bi-house-door"></i>Dashboard</a></li>
                    <li><a href="Opportunities.aspx"><i class="bi bi-compass"></i>Explore Opportunities</a></li>
                    <li><a href="MyApplications.aspx"><i class="bi bi-file-earmark-text"></i>My Applications</a></li>
                    <li><a href="MyTasks.aspx"><i class="bi bi-list-check"></i>My Tasks<span class="count">3</span></a></li>
                    <li><a href="MyProgress.aspx"><i class="bi bi-graph-up-arrow"></i>Progress &amp; Evaluation</a></li>
                    <li><a href="MyProfile.aspx"><i class="bi bi-person"></i>My Profile</a></li>
                </ul>

                <div class="side-divider"></div>

                <ul class="side-nav">
                    <li><a href="ChangePassword.aspx"><i class="bi bi-gear"></i>Settings</a></li>
                    <li><a href="../Contact.aspx"><i class="bi bi-question-circle"></i>Help</a></li>
                </ul>

                <div class="side-promo">
                    <i class="bi bi-mortarboard-fill"></i>
                    <p>Build your future<br />with real experience.</p>
                    <a href="Opportunities.aspx">Explore opportunities &rarr;</a>
                </div>
            </aside>
            <div class="sidebar-backdrop" onclick="toggleSidebar()"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input type="text" placeholder="Search for internships, companies, or skills..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" /><span class="role">Student</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="MyProfile.aspx"><i class="bi bi-person me-2"></i>My Profile</a></li>
                                <li><a class="dropdown-item" href="ChangePassword.aspx"><i class="bi bi-key me-2"></i>Change Password</a></li>
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
                    <p class="subtitle">Your internship journey, all in one place.</p>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-file-earmark-text"></i></div>
                                <div class="stat-label">Applied</div>
                                <div class="stat-value"><asp:Label ID="lblApplied" runat="server" /></div>
                                <div class="stat-note">2 this month</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-clock"></i></div>
                                <div class="stat-label">In Review</div>
                                <div class="stat-value"><asp:Label ID="lblInReview" runat="server" /></div>
                                <div class="stat-note">1 new update</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-check-lg"></i></div>
                                <div class="stat-label">Pending Tasks</div>
                                <div class="stat-value"><asp:Label ID="lblTasks" runat="server" /></div>
                                <div class="stat-note">Next due Oct 23</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon"><i class="bi bi-hourglass-split"></i></div>
                                <div class="stat-label">Approved Hours</div>
                                <div class="stat-value"><asp:Label ID="lblHours" runat="server" /></div>
                                <div class="stat-note">of 280 required</div>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-tag">Featured opportunity <i class="bi bi-arrow-up-right"></i></div>
                        <h2>Find your place in tech.</h2>
                        <p>Discover internships that match your college and major.</p>
                        <a href="Opportunities.aspx" class="btn-hero">Explore opportunities <i class="bi bi-arrow-right"></i></a>
                    </section>

                    <div class="row g-3">
                        <!-- Recent applications -->
                        <div class="col-lg-7">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Recent Applications</h3>
                                    <a href="MyApplications.aspx">View all</a>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon">O</div>
                                    <div class="row-body">
                                        <div class="row-title">Frontend Developer Intern</div>
                                        <div class="row-sub">Orange Jordan</div>
                                    </div>
                                    <div class="row-meta"><span class="status info">In Review</span><div class="when">Oct 12, 2026</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon">Z</div>
                                    <div class="row-body">
                                        <div class="row-title">Software Engineering Intern</div>
                                        <div class="row-sub">Zain Jordan</div>
                                    </div>
                                    <div class="row-meta"><span class="status success">Accepted</span><div class="when">Oct 8, 2026</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon">A</div>
                                    <div class="row-body">
                                        <div class="row-title">IT Support Intern</div>
                                        <div class="row-sub">Aramex</div>
                                    </div>
                                    <div class="row-meta"><span class="status danger">Rejected</span><div class="when">Oct 5, 2026</div></div>
                                </div>
                            </div>
                        </div>

                        <!-- Right column -->
                        <div class="col-lg-5 d-flex flex-column gap-3">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Upcoming Deadlines</h3>
                                    <a href="MyTasks.aspx">View all</a>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-calendar-event"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Weekly Report #3</div>
                                        <div class="row-sub">Zain Jordan</div>
                                    </div>
                                    <div class="row-meta"><div class="when-strong">Thu, Oct 23</div><div class="when"><i class="bi bi-clock"></i> 11:59 PM</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-arrow-repeat"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Login Page UI</div>
                                        <div class="row-sub"><span class="status warning">Revision requested</span></div>
                                    </div>
                                    <div class="row-meta"><div class="when-strong">Mon, Oct 27</div><div class="when"><i class="bi bi-clock"></i> 02:30 PM</div></div>
                                </div>
                            </div>

                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Internship Progress</h3>
                                    <a href="MyProgress.aspx">Details</a>
                                </div>
                                <div class="d-flex justify-content-between small mt-2">
                                    <span class="fw-semibold">Training hours</span><span class="text-secondary">96 / 280 h</span>
                                </div>
                                <div class="progress-slim"><span style="width: 34%"></span></div>
                                <div class="d-flex justify-content-between small mt-3">
                                    <span class="fw-semibold">Tasks completed</span><span class="text-secondary">5 / 8</span>
                                </div>
                                <div class="progress-slim"><span class="success" style="width: 62%"></span></div>
                            </div>
                        </div>
                    </div>
                </main>
            </div>
        </div>
    </form>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
