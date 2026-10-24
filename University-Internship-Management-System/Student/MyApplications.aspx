<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyApplications.aspx.cs" Inherits="University_Internship_Management_System.Student.MyApplications" %>

<script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
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
    <link href="../Assets/student/my-applications.css?v=20261023-modern" rel="stylesheet" />
</head>
<body class="standalone-shell" data-demo-role="student">
    <form id="applicationsForm" runat="server">
        <div class="app">
            <!-- نفس السايد بار المرسل، مكرر داخل الصفحة مع ضبط الروابط المتوفرة -->
            <aside class="sidebar" id="studentSidebar" aria-label="Student navigation">
                <button type="button" id="closeSidebar" class="sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="StudentDashboard.aspx" class="brand">
                    <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="Jordan University of Science and Technology logo" width="52" height="52" />
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Student pages">
<li><a href="StudentDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ExploreOpportunities.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Explore Opportunities</span></a></li>
<li><a href="MyApplications.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Applications</span></a></li>
<li><a href="ApplicationForm.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Application Form</span></a></li>
<li><a href="Profile.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M5 21v-3a7 7 0 0 1 14 0v3"/></svg><span>My Profile</span></a></li>

<li><a href="MyTasks.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Tasks</span></a></li>

<li><a href="MyProgressEvaluation.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Progress &amp; Evaluation</span></a></li>
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
                                <li><a class="dropdown-item" href="Profile.aspx"><i class="bi bi-person me-2"></i>My Profile</a></li>
                                <li><a class="dropdown-item" aria-disabled="true" title="This page is not available yet"><i class="bi bi-key me-2"></i>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>
                <p id="navbarNotice" class="navbar-notice" role="status" hidden></p>
        <main class="applications-page">
            <p class="preview-note" id="previewNote">Sample applications for preview. These are separate from the application form. Confirmation resets when you refresh.</p>

            <header class="page-heading">
                <div class="heading-text">
                    <p class="eyebrow"><i class="bi bi-folder2-open"></i> JUST / STUDENT WORKSPACE</p>
                    <h1>My Applications</h1>
                    <p>Follow your applications and find your next step.</p>
                </div>
                <a href="ExploreOpportunities.aspx" class="primary-link"><i class="bi bi-compass"></i> Explore opportunities <i class="bi bi-arrow-right"></i></a>
            </header>

            <section class="row g-3 summary-row" aria-label="Application summary">
                <div class="col-6 col-xl-3"><div class="summary-card"><span class="summary-icon primary"><i class="bi bi-folder2-open"></i></span><div><span>Total applications</span><strong>4</strong></div></div></div>
                <div class="col-6 col-xl-3"><div class="summary-card"><span class="summary-icon warning"><i class="bi bi-hourglass-split"></i></span><div><span>Pending review</span><strong class="pending-text">1</strong></div></div></div>
                <div class="col-6 col-xl-3"><div class="summary-card"><span class="summary-icon success"><i class="bi bi-patch-check"></i></span><div><span>Accepted offers</span><strong class="accepted-text">2</strong></div></div></div>
                <div class="col-6 col-xl-3"><div class="summary-card"><span class="summary-icon danger"><i class="bi bi-x-circle"></i></span><div><span>Rejected</span><strong class="rejected-text">1</strong></div></div></div>
            </section>

            <div class="guidance" id="guidance">
                <span class="guidance-icon"><i class="bi bi-signpost-2" id="guidanceIcon"></i></span>
                <div>
                    <strong id="guidanceTitle">Your next step</strong>
                    <p id="guidanceText">An accepted offer needs your confirmation. You can confirm only one internship offer.</p>
                </div>
            </div>
            <div id="confirmationNotice" class="confirmation-notice" role="status" tabindex="-1" hidden></div>

            <section aria-labelledby="applicationsHeading">
                <div class="list-heading">
                    <h2 id="applicationsHeading">Your applications <span id="resultCount" aria-live="polite">4</span></h2>
                    <div class="status-filter">
                        <label for="statusFilter"><i class="bi bi-funnel"></i> Status</label>
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
                    <article class="application-card status-accepted-card" data-id="sample-web" data-status="Accepted">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark tone-0" aria-hidden="true">NT</span><div class="min-w-0"><h3>Web Development Intern</h3><p class="company-name">North Tech</p></div></div>
                            <span class="status-badge status-accepted"><i class="bi bi-patch-check"></i> Accepted</span>
                        </div>
                        <div class="application-meta"><span><i class="bi bi-geo-alt"></i> Amman · On-site</span><span><i class="bi bi-mortarboard"></i> Software Engineering</span><span><i class="bi bi-hourglass-split"></i> 8 weeks</span><span><i class="bi bi-calendar3"></i> Applied <time datetime="2026-10-15">15 Oct 2026</time></span></div>
                        <ol class="tracker" aria-label="Application progress"><li class="done"><span class="dot"></span><span class="label">Applied</span></li><li class="done"><span class="dot"></span><span class="label">Reviewed</span></li><li class="done"><span class="dot"></span><span class="label">Accepted</span></li><li class="current"><span class="dot"></span><span class="label">Confirm</span></li></ol>
                        <p class="decision-note note-success"><i class="bi bi-info-circle"></i> Your application has been accepted. Review the offer before confirming.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal"><i class="bi bi-eye"></i> View details</button><button type="button" class="confirm-button" data-bs-toggle="modal" data-bs-target="#confirmModal"><i class="bi bi-check2-circle"></i> Confirm offer</button></div>
                    </article>

                    <article class="application-card status-accepted-card" data-id="sample-security" data-status="Accepted">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark tone-3" aria-hidden="true">SP</span><div class="min-w-0"><h3>Security Operations Intern</h3><p class="company-name">Secure Path</p></div></div>
                            <span class="status-badge status-accepted"><i class="bi bi-patch-check"></i> Accepted</span>
                        </div>
                        <div class="application-meta"><span><i class="bi bi-geo-alt"></i> Remote</span><span><i class="bi bi-mortarboard"></i> Cybersecurity</span><span><i class="bi bi-hourglass-split"></i> 8 weeks</span><span><i class="bi bi-calendar3"></i> Applied <time datetime="2026-10-14">14 Oct 2026</time></span></div>
                        <ol class="tracker" aria-label="Application progress"><li class="done"><span class="dot"></span><span class="label">Applied</span></li><li class="done"><span class="dot"></span><span class="label">Reviewed</span></li><li class="done"><span class="dot"></span><span class="label">Accepted</span></li><li class="current"><span class="dot"></span><span class="label">Confirm</span></li></ol>
                        <p class="decision-note note-success"><i class="bi bi-info-circle"></i> Your application has been accepted. Review the offer before confirming.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal"><i class="bi bi-eye"></i> View details</button><button type="button" class="confirm-button" data-bs-toggle="modal" data-bs-target="#confirmModal"><i class="bi bi-check2-circle"></i> Confirm offer</button></div>
                    </article>

                    <article class="application-card status-pending-card" data-id="sample-data" data-status="Pending">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark tone-2" aria-hidden="true">DL</span><div class="min-w-0"><h3>Data Analysis Intern</h3><p class="company-name">Data Lab</p></div></div>
                            <span class="status-badge status-pending"><i class="bi bi-hourglass-split"></i> Pending</span>
                        </div>
                        <div class="application-meta"><span><i class="bi bi-geo-alt"></i> Irbid · On-site</span><span><i class="bi bi-mortarboard"></i> Computer Science</span><span><i class="bi bi-hourglass-split"></i> 10 weeks</span><span><i class="bi bi-calendar3"></i> Applied <time datetime="2026-10-18">18 Oct 2026</time></span></div>
                        <ol class="tracker" aria-label="Application progress"><li class="done"><span class="dot"></span><span class="label">Applied</span></li><li class="current"><span class="dot"></span><span class="label">Under review</span></li><li><span class="dot"></span><span class="label">Decision</span></li><li><span class="dot"></span><span class="label">Confirm</span></li></ol>
                        <p class="decision-note note-warning"><i class="bi bi-clock-history"></i> Your application is waiting for the company supervisor's review.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal"><i class="bi bi-eye"></i> View details</button></div>
                    </article>

                    <article class="application-card status-rejected-card" data-id="sample-qa" data-status="Rejected">
                        <div class="application-top">
                            <div class="company-heading"><span class="company-mark tone-1" aria-hidden="true">CT</span><div class="min-w-0"><h3>Software Testing Intern</h3><p class="company-name">Clear Tech</p></div></div>
                            <span class="status-badge status-rejected"><i class="bi bi-x-circle"></i> Rejected</span>
                        </div>
                        <div class="application-meta"><span><i class="bi bi-geo-alt"></i> Amman · On-site</span><span><i class="bi bi-mortarboard"></i> Software Engineering</span><span><i class="bi bi-hourglass-split"></i> 8 weeks</span><span><i class="bi bi-calendar3"></i> Applied <time datetime="2026-10-12">12 Oct 2026</time></span></div>
                        <ol class="tracker" aria-label="Application progress"><li class="done"><span class="dot"></span><span class="label">Applied</span></li><li class="done"><span class="dot"></span><span class="label">Reviewed</span></li><li class="danger"><span class="dot"></span><span class="label">Rejected</span></li></ol>
                        <p class="decision-note note-danger"><i class="bi bi-chat-left-text"></i> Reason: All available seats have been filled.</p>
                        <div class="application-actions"><button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#applicationModal"><i class="bi bi-eye"></i> View details</button></div>
                    </article>
                </div>
                <div id="emptyState" class="empty-state" hidden><div class="empty-art"><i class="bi bi-inbox"></i></div><h3 id="emptyTitle">No applications with this status</h3><p id="emptyText">Choose another status to see your applications.</p></div>
            </section>

            <div class="modal fade app-modal" id="applicationModal" tabindex="-1" aria-labelledby="detailsTitle" aria-hidden="true">
                <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content">
                    <div class="modal-header">
                        <span class="company-mark lg tone-0" id="detailsMark" aria-hidden="true"><i class="bi bi-briefcase"></i></span>
                        <div class="modal-heading">
                            <p id="detailsCompany" class="modal-company"></p>
                            <h2 id="detailsTitle" class="modal-title">Application details</h2>
                            <p id="detailsStatus" class="details-status"></p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div id="detailsTrack"></div>
                        <div id="detailsMeta"></div>
                        <div id="detailsDecision" class="details-decision"></div>
                        <div id="detailsExtra"></div>
                        <p class="preview-note" id="detailsNote">Sample record only. Submitted documents will appear here after database integration.</p>
                    </div>
                    <div class="modal-footer"><button type="button" class="details-button" data-bs-dismiss="modal">Close</button></div>
                </div></div>
            </div>

            <div class="modal fade app-modal confirm-modal" id="confirmModal" tabindex="-1" aria-labelledby="confirmTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
                    <div class="modal-header">
                        <span class="modal-icon"><i class="bi bi-check2-circle"></i></span>
                        <div class="modal-heading">
                            <h2 id="confirmTitle" class="modal-title">Confirm this offer?</h2>
                            <p class="modal-sub">This becomes your internship placement.</p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p id="selectedOffer" class="selected-offer"></p>
                        <ul class="consequence-list">
                            <li><span class="cl-icon success"><i class="bi bi-briefcase"></i></span> Your internship placement starts with this company.</li>
                            <li><span class="cl-icon warning"><i class="bi bi-arrow-repeat"></i></span> You can confirm only one internship offer. Your other accepted offers are withdrawn so their seats go to other students.</li>
                            <li><span class="cl-icon neutral"><i class="bi bi-hourglass-split"></i></span> Pending and rejected applications keep their current status.</li>
                        </ul>
                        <p class="preview-note" id="confirmNote">Preview only — your choice will reset when you refresh this page.</p>
                    </div>
                    <div class="modal-footer"><button type="button" class="details-button" data-bs-dismiss="modal">Keep reviewing</button><button type="button" id="confirmOffer" class="confirm-button"><i class="bi bi-check-lg"></i> Confirm offer</button></div>
                </div></div>
            </div>
            <noscript><p>Enable JavaScript to filter applications, view details, and preview offer confirmation.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/my-applications.js?v=20261023-modern"></script>
</body>
</html>
