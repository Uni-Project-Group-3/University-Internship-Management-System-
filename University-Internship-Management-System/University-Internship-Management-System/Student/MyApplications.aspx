<%@ Page Language="C#" %>

<script runat="server">
    protected void btnLogout_Click(object sender, System.EventArgs e)
    {
        Session.Clear();
        Session.Abandon();
        Response.Redirect("~/Auth/Login.aspx");
    }
</script>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Applications | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/my-applications.css?v=20261021-original-shell" rel="stylesheet" />
</head>
<body>
    <form id="applicationsForm" runat="server">
        <div class="app">
            <!-- نفس السايد بار المرسل، مكرر داخل الصفحة مع ضبط الروابط المتوفرة -->
            <aside class="sidebar" id="studentSidebar" aria-label="Student navigation">
                <button type="button" id="closeSidebar" class="sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="StudentDashboard.aspx" class="brand">
                    <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="Jordan University of Science and Technology logo" width="52" height="52" />
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav">
                    <li><a href="StudentDashboard.aspx"><i class="bi bi-house-door"></i>Dashboard</a></li>
                    <li><a href="ExploreOpportunities.aspx"><i class="bi bi-compass"></i>Explore Opportunities</a></li>
                    <li><a href="MyApplications.aspx" class="active" aria-current="page"><i class="bi bi-file-earmark-text"></i>My Applications</a></li>
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
                    <a href="ExploreOpportunities.aspx">Explore opportunities &rarr;</a>
                </div>
            </aside>
            <div id="sidebarBackdrop" class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main" id="studentMain">
                <header class="topbar">
                    <button type="button" class="menu-btn" id="openSidebar" aria-label="Open navigation" aria-controls="studentSidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="navbarSearch" type="search" aria-label="Workspace search" placeholder="Search for internships, companies, or skills..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" id="navbarNotifications" aria-label="Notifications preview"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown" aria-expanded="false" aria-label="Account menu">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" Text="ST" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" Text="Student" /><span class="role">Student</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="MyProfile.aspx"><i class="bi bi-person me-2"></i>My Profile</a></li>
                                <li><a class="dropdown-item" href="ChangePassword.aspx"><i class="bi bi-key me-2"></i>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>
                <p id="navbarNotice" class="navbar-notice" role="status" hidden></p>
        <main class="applications-page">
            <header class="page-heading">
                <div>
                    <p class="eyebrow">JUST / STUDENT WORKSPACE</p>
                    <h1>My Applications</h1>
                    <p>Follow your applications and find your next step.</p>
                </div>
                <a href="ExploreOpportunities.aspx" class="primary-link">Explore opportunities <span aria-hidden="true">&rarr;</span></a>
            </header>

            <p class="preview-note">Sample applications for preview. These are separate from the application form. Confirmation resets when you refresh.</p>

            <section class="row g-3 summary-row" aria-label="Application summary">
                <div class="col-6 col-lg-3"><div class="summary-card"><span>Total applications</span><strong>4</strong></div></div>
                <div class="col-6 col-lg-3"><div class="summary-card"><span>Pending review</span><strong class="pending-text">1</strong></div></div>
                <div class="col-6 col-lg-3"><div class="summary-card"><span>Accepted offers</span><strong class="accepted-text">2</strong></div></div>
                <div class="col-6 col-lg-3"><div class="summary-card"><span>Rejected</span><strong class="rejected-text">1</strong></div></div>
            </section>

            <div class="guidance"><strong>Your next step</strong><p>An accepted offer needs your confirmation. You can confirm only one internship offer.</p></div>
            <div id="confirmationNotice" class="confirmation-notice" role="status" tabindex="-1" hidden></div>

            <section aria-labelledby="applicationsHeading">
                <div class="list-heading">
                    <h2 id="applicationsHeading">Your applications <span id="resultCount" aria-live="polite">(4)</span></h2>
                    <div class="status-filter">
                        <label for="statusFilter">Status</label>
                        <select id="statusFilter" class="form-select">
                            <option value="">All statuses</option>
                            <option value="Pending">Pending</option>
                            <option value="Accepted">Accepted</option>
                            <option value="Rejected">Rejected</option>
                        </select>
                    </div>
                </div>

                <!-- طلبات تجريبية، وتستبدل لاحقاً بطلبات الطالب المسجل فقط -->
                <div class="application-list">
                    <article class="application-card" data-id="sample-web" data-status="Accepted">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark" aria-hidden="true">NT</span><div><h3>Web Development Intern</h3><p class="company-name">North Tech</p></div></div>
                            <span class="status-badge status-accepted">Accepted</span>
                        </div>
                        <div class="application-meta"><span>Amman · On-site</span><span>Software Engineering</span><span>8 weeks</span><span>Applied <time datetime="2026-10-15">15 Oct 2026</time></span></div>
                        <p class="decision-note">Your application has been accepted. Review the offer before confirming.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal">View details</button><button type="button" class="confirm-button" data-bs-toggle="modal" data-bs-target="#confirmModal">Confirm offer <span aria-hidden="true">&rarr;</span></button></div>
                    </article>

                    <article class="application-card" data-id="sample-security" data-status="Accepted">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark" aria-hidden="true">SP</span><div><h3>Security Operations Intern</h3><p class="company-name">Secure Path</p></div></div>
                            <span class="status-badge status-accepted">Accepted</span>
                        </div>
                        <div class="application-meta"><span>Remote</span><span>Cybersecurity</span><span>8 weeks</span><span>Applied <time datetime="2026-10-14">14 Oct 2026</time></span></div>
                        <p class="decision-note">Your application has been accepted. Review the offer before confirming.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal">View details</button><button type="button" class="confirm-button" data-bs-toggle="modal" data-bs-target="#confirmModal">Confirm offer <span aria-hidden="true">&rarr;</span></button></div>
                    </article>

                    <article class="application-card" data-id="sample-data" data-status="Pending">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark" aria-hidden="true">DL</span><div><h3>Data Analysis Intern</h3><p class="company-name">Data Lab</p></div></div>
                            <span class="status-badge status-pending">Pending</span>
                        </div>
                        <div class="application-meta"><span>Irbid · On-site</span><span>Computer Science</span><span>10 weeks</span><span>Applied <time datetime="2026-10-18">18 Oct 2026</time></span></div>
                        <p class="decision-note">Your application is waiting for the company supervisor's review.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal">View details</button></div>
                    </article>

                    <article class="application-card" data-id="sample-qa" data-status="Rejected">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark" aria-hidden="true">CT</span><div><h3>Software Testing Intern</h3><p class="company-name">Clear Tech</p></div></div>
                            <span class="status-badge status-rejected">Rejected</span>
                        </div>
                        <div class="application-meta"><span>Amman · On-site</span><span>Software Engineering</span><span>8 weeks</span><span>Applied <time datetime="2026-10-12">12 Oct 2026</time></span></div>
                        <p class="decision-note">Reason: All available seats have been filled.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal">View details</button></div>
                    </article>
                </div>
                <div id="emptyState" class="empty-state" hidden><h3>No applications with this status</h3><p>Choose another status to see your applications.</p></div>
            </section>

            <div class="modal fade" id="applicationModal" tabindex="-1" aria-labelledby="detailsTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
                    <div class="modal-header"><h2 id="detailsTitle" class="modal-title fs-5">Application details</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                    <div class="modal-body"><p id="detailsCompany" class="modal-company"></p><div id="detailsMeta"></div><p id="detailsStatus"></p><p id="detailsDecision"></p><p class="preview-note">Sample record only. Submitted documents will appear here after database integration.</p></div>
                    <div class="modal-footer"><button type="button" class="details-button" data-bs-dismiss="modal">Close</button></div>
                </div></div>
            </div>

            <div class="modal fade" id="confirmModal" tabindex="-1" aria-labelledby="confirmTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
                    <div class="modal-header"><h2 id="confirmTitle" class="modal-title fs-5">Confirm this offer?</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                    <div class="modal-body"><p id="selectedOffer"></p><p>You can confirm only one internship offer. Other applications keep their current status.</p><p class="preview-note">Preview only — your choice will reset when you refresh this page.</p></div>
                    <div class="modal-footer"><button type="button" class="details-button" data-bs-dismiss="modal">Keep reviewing</button><button type="button" id="confirmOffer" class="confirm-button">Confirm offer</button></div>
                </div></div>
            </div>
            <noscript><p>Enable JavaScript to filter applications, view details, and preview offer confirmation.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/my-applications.js?v=20261021-original-shell"></script>
</body>
</html>
