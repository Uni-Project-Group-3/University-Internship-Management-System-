<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CoordinatorDashboard.aspx.cs" Inherits="UIMS.Coordinator.CoordinatorDashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Coordinator Dashboard | JUST Internships</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link href="~/Assets/coordinator/coordinator-dashboard.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Coordinator) ============ -->
            <aside class="sidebar" id="sidebar">
                <a href="CoordinatorDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav">
                    <li><a href="CoordinatorDashboard.aspx" class="active"><i class="bi bi-house-door"></i>Dashboard</a></li>
                    <li><a href="ManageStudents.aspx"><i class="bi bi-mortarboard"></i>Manage Students</a></li>
                    <li><a href="ManageCompanies.aspx"><i class="bi bi-building"></i>Manage Companies</a></li>
                    <li><a href="ManageSupervisors.aspx"><i class="bi bi-person-badge"></i>Academic Supervisors</a></li>
                    <li><a href="AssignSupervisors.aspx"><i class="bi bi-diagram-3"></i>Assign Supervisors</a></li>
                    <li><a href="MonitorPlacements.aspx"><i class="bi bi-activity"></i>Placements &amp; Progress</a></li>
                    <li><a href="CompletionReview.aspx"><i class="bi bi-patch-check"></i>Final Completion Review<span class="count">4</span></a></li>
                    <li><a href="ContactMessages.aspx"><i class="bi bi-envelope"></i>Contact Messages</a></li>
                </ul>

                <div class="side-divider"></div>

                <ul class="side-nav">
                    <li><a href="Account.aspx"><i class="bi bi-gear"></i>Coordinator Account</a></li>
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
                    <button type="button" class="menu-btn" onclick="toggleSidebar()"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input type="text" placeholder="Search for students, companies, or supervisors..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" /><span class="role">Internship Coordinator</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Account.aspx"><i class="bi bi-person me-2"></i>Account</a></li>
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
                    <p class="subtitle">An overview of internships across the faculty.</p>

                    <!-- Stats -->
                    <div class="row g-3">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-mortarboard"></i></div>
                                <div class="stat-label">Students</div>
                                <div class="stat-value"><asp:Label ID="lblStudents" runat="server" /></div>
                                <div class="stat-note">96 currently placed</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon"><i class="bi bi-building"></i></div>
                                <div class="stat-label">Companies</div>
                                <div class="stat-value"><asp:Label ID="lblCompanies" runat="server" /></div>
                                <div class="stat-note">3 inactive</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">Opportunities</div>
                                <div class="stat-value"><asp:Label ID="lblOpportunities" runat="server" /></div>
                                <div class="stat-note">Open this semester</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon success"><i class="bi bi-patch-check"></i></div>
                                <div class="stat-label">Awaiting Approval</div>
                                <div class="stat-value"><asp:Label ID="lblAwaiting" runat="server" /></div>
                                <div class="stat-note">Both evaluations in</div>
                            </div>
                        </div>
                    </div>

                    <!-- Hero -->
                    <section class="hero" style="--hero-image: url('../Images/campus.jpg')">
                        <div class="hero-tag">Final completion review <i class="bi bi-arrow-up-right"></i></div>
                        <h2>Close the loop with confidence.</h2>
                        <p>Review company and academic evaluations, verify requirements, and issue completion reports.</p>
                        <a href="CompletionReview.aspx" class="btn-hero">Review completions <i class="bi bi-arrow-right"></i></a>
                    </section>

                    <div class="row g-3">
                        <!-- Pending completion -->
                        <div class="col-lg-7">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Pending Completion Approval</h3>
                                    <a href="CompletionReview.aspx">View all</a>
                                </div>
                                <div class="list-row">
                                    <div class="avatar">RS</div>
                                    <div class="row-body">
                                        <div class="row-title">Rana Saleh</div>
                                        <div class="row-sub">Orange Jordan · Dr. Khaled Omari</div>
                                    </div>
                                    <div class="row-meta"><span class="status success">Ready</span><div class="when">Final: 88.5</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="avatar">SO</div>
                                    <div class="row-body">
                                        <div class="row-title">Sami Odeh</div>
                                        <div class="row-sub">Zain Jordan · Dr. Lina Haddad</div>
                                    </div>
                                    <div class="row-meta"><span class="status success">Ready</span><div class="when">Final: 91.0</div></div>
                                </div>
                                <div class="list-row">
                                    <div class="avatar">HM</div>
                                    <div class="row-body">
                                        <div class="row-title">Hala Mahmoud</div>
                                        <div class="row-sub">Aramex · Dr. Khaled Omari</div>
                                    </div>
                                    <div class="row-meta"><span class="status warning">Academic eval pending</span><div class="when">Company: received</div></div>
                                </div>
                            </div>
                        </div>

                        <!-- Right column -->
                        <div class="col-lg-5 d-flex flex-column gap-3">
                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Contact Messages</h3>
                                    <a href="ContactMessages.aspx">View all</a>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-envelope"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Partnership request</div>
                                        <div class="row-sub">hr@techjo.com</div>
                                    </div>
                                    <span class="status info">New</span>
                                </div>
                                <div class="list-row">
                                    <div class="row-icon soft"><i class="bi bi-envelope-open"></i></div>
                                    <div class="row-body">
                                        <div class="row-title">Login issue</div>
                                        <div class="row-sub">student@just.edu.jo</div>
                                    </div>
                                    <span class="status neutral">Read</span>
                                </div>
                            </div>

                            <div class="panel">
                                <div class="panel-head">
                                    <h3>Placement Overview</h3>
                                    <a href="MonitorPlacements.aspx">Details</a>
                                </div>
                                <div class="d-flex justify-content-between small mt-2">
                                    <span class="fw-semibold">Students placed</span><span class="text-secondary">96 / 128</span>
                                </div>
                                <div class="progress-slim"><span style="width: 75%"></span></div>
                                <div class="d-flex justify-content-between small mt-3">
                                    <span class="fw-semibold">Completed internships</span><span class="text-secondary">21 / 96</span>
                                </div>
                                <div class="progress-slim"><span class="success" style="width: 22%"></span></div>
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
