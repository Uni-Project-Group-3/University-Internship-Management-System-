<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExploreOpportunities.aspx.cs" Inherits="University_Internship_Management_System.Student.ExploreOpportunities" %>


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
    <title>Explore Opportunities | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/explore-opportunities.css?v=20261024-view" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
<body class="coordinator-banner standalone-shell" data-demo-role="student">
    <form id="exploreForm" runat="server">
        <div class="app">
            <!-- نفس السايد بار المرسل، مكرر داخل الصفحة مع ضبط الروابط المتوفرة -->
            <aside class="sidebar workspace-canonical-sidebar" id="studentSidebar" aria-label="Student navigation"><a class="brand workspace-nav-brand" href="StudentDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Student workspace</p><nav class="workspace-nav-primary" aria-label="Student workspace pages"><a class="workspace-nav-link" href="StudentDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link active" href="ExploreOpportunities.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Explore Opportunities</span></a><a class="workspace-nav-link" href="MyApplications.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>My Applications</span></a><a class="workspace-nav-link" href="MyTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>My Tasks</span></a><a class="workspace-nav-link" href="MyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>My Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Report Status</span></a><a class="workspace-nav-link" href="MyProgressEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>My Progress &amp; Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="Profile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Profile</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <div id="sidebarBackdrop" class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main" id="studentMain">
                <header class="topbar">
                    <button type="button" class="menu-btn" id="openSidebar" aria-label="Open navigation" aria-controls="studentSidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
     
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
        <main class="opportunities-page">
            <header class="page-heading">
                <div class="heading-text">
                    <p class="eyebrow"><i class="bi bi-compass"></i> JUST / STUDENT WORKSPACE</p>
                    <h1>Explore Opportunities</h1>
                    <p class="page-description">Find a place to learn, contribute, and take your next step.</p>
                    <div class="college-label"><i class="bi bi-bank"></i> Jordan University of Science and Technology <span aria-hidden="true">&middot;</span> Information Technology</div>
                </div>
                <div class="heading-stats" aria-label="Opportunity summary">
                    <div class="heading-stat">
                        <span class="heading-stat-icon"><i class="bi bi-briefcase"></i></span>
                        <span><strong id="heroOpen">3</strong><small>Open now</small></span>
                    </div>
                    <div class="heading-stat">
                        <span class="heading-stat-icon warning"><i class="bi bi-alarm"></i></span>
                        <span><strong id="heroSoon">—</strong><small>Closing this week</small></span>
                    </div>
                    <div class="heading-stat">
                        <span class="heading-stat-icon success"><i class="bi bi-send-check"></i></span>
                        <span><strong id="heroApplied">—</strong><small>Your applications</small></span>
                    </div>
                </div>
            </header>

            <section class="search-panel" aria-label="Filter opportunities">
                <div class="filter-grid">
                    <div class="filter-field filter-search">
                        <label for="searchInput" class="form-label">Search opportunities</label>
                        <div class="input-icon">
                            <i class="bi bi-search"></i>
                            <input type="search" id="searchInput" class="form-control" placeholder="Opportunity title or company name" />
                        </div>
                    </div>


                    <div class="filter-field">
                        <label for="sortBy" class="form-label">Sort by</label>
                        <div class="input-icon">
                            <i class="bi bi-sort-down"></i>
                            <select id="sortBy" class="form-select">
                                <option value="deadline">Deadline soonest</option>
                                <option value="newest">Newest first</option>
                                <option value="title">Title A–Z</option>
                            </select>
                        </div>
                    </div>
                </div>
            </section>

            <div class="results-heading">
                <h2>Available opportunities <span id="resultCount" aria-live="polite">3</span></h2>
                <button type="button" id="resetFilters" class="reset-button" hidden><i class="bi bi-arrow-counterclockwise"></i> Reset filters</button>
            </div>
            <p class="sample-note" id="sampleNote"><i class="bi bi-info-circle"></i> Sample opportunities for preview. Availability will be provided by the system.</p>

            <!-- بيانات تجريبية للواجهة، وتستبدل لاحقاً بفرص مناسبة لتخصص الطالب من قاعدة البيانات -->
            <section id="opportunitiesList" class="row g-4" aria-label="Available internship opportunities">
                <div class="col-md-6 col-xxl-4 opportunity-item" data-major="Software Engineering" data-location="Amman" data-id="sample-web">
                    <article class="opportunity-card">
                        <div class="card-top">
                            <div class="company-heading"><span class="company-mark tone-0" aria-hidden="true">NT</span><div class="min-w-0"><p class="company-name">North Tech</p><p class="company-location"><i class="bi bi-geo-alt"></i> Amman &middot; On-site</p></div></div>
                            <span class="deadline-badge" data-deadline="2026-11-01"><i class="bi bi-calendar-event"></i> Apply by 01 Nov</span>
                        </div>
                        <h3>Web Development Intern</h3>
                        <div class="card-tags"><span class="specialization"><i class="bi bi-mortarboard"></i> Software Engineering</span><span class="mode-tag"><i class="bi bi-building"></i> On-site</span></div>
                        <p class="opportunity-description">Build practical web development skills while working with a team on useful digital products.</p>
                        <dl class="opportunity-facts">
                            <div><dt><i class="bi bi-hourglass-split"></i> Duration</dt><dd>8 weeks</dd></div>
                            <div><dt><i class="bi bi-people"></i> Available seats</dt><dd>3 students</dd></div>
                            <div><dt><i class="bi bi-calendar-event"></i> Deadline</dt><dd><time datetime="2026-11-01">01 Nov 2026</time></dd></div>
                            <div><dt><i class="bi bi-clock-history"></i> Training hours</dt><dd>240 hours</dd></div>
                        </dl>
                        <div class="opportunity-details" hidden><p>Assist with responsive interfaces, test web pages, and collaborate on small development tasks under a company supervisor.</p><h4>Preferred skills</h4><p>These skills are preferred, not mandatory. Students who are still developing them are welcome to apply.</p><ul><li>Basic HTML, CSS, and JavaScript.</li><li>Familiarity with C# and Git.</li><li>Interest in teamwork and learning.</li></ul></div>
                        <button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#opportunityModal">View details <i class="bi bi-arrow-right"></i></button>
                    </article>
                </div>
                <div class="col-md-6 col-xxl-4 opportunity-item" data-major="Computer Science" data-location="Irbid" data-id="sample-data">
                    <article class="opportunity-card">
                        <div class="card-top">
                            <div class="company-heading"><span class="company-mark tone-2" aria-hidden="true">DL</span><div class="min-w-0"><p class="company-name">Data Lab</p><p class="company-location"><i class="bi bi-geo-alt"></i> Irbid &middot; On-site</p></div></div>
                            <span class="deadline-badge" data-deadline="2026-11-05"><i class="bi bi-calendar-event"></i> Apply by 05 Nov</span>
                        </div>
                        <h3>Data Analysis Intern</h3>
                        <div class="card-tags"><span class="specialization"><i class="bi bi-mortarboard"></i> Computer Science</span><span class="mode-tag"><i class="bi bi-building"></i> On-site</span></div>
                        <p class="opportunity-description">Explore real-world datasets and learn how to turn information into clear, useful insights.</p>
                        <dl class="opportunity-facts">
                            <div><dt><i class="bi bi-hourglass-split"></i> Duration</dt><dd>10 weeks</dd></div>
                            <div><dt><i class="bi bi-people"></i> Available seats</dt><dd>2 students</dd></div>
                            <div><dt><i class="bi bi-calendar-event"></i> Deadline</dt><dd><time datetime="2026-11-05">05 Nov 2026</time></dd></div>
                            <div><dt><i class="bi bi-clock-history"></i> Training hours</dt><dd>300 hours</dd></div>
                        </dl>
                        <div class="opportunity-details" hidden><p>Help prepare datasets, create basic reports, and explain findings with guidance from a data analysis team.</p><h4>Preferred skills</h4><p>These skills are preferred, not mandatory. Students who are still developing them are welcome to apply.</p><ul><li>Basic Python and SQL.</li><li>Understanding of data structures.</li><li>Attention to detail.</li></ul></div>
                        <button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#opportunityModal">View details <i class="bi bi-arrow-right"></i></button>
                    </article>
                </div>
                <div class="col-md-6 col-xxl-4 opportunity-item" data-major="Cybersecurity" data-location="Remote" data-id="sample-security">
                    <article class="opportunity-card">
                        <div class="card-top">
                            <div class="company-heading"><span class="company-mark tone-3" aria-hidden="true">SP</span><div class="min-w-0"><p class="company-name">Secure Path</p><p class="company-location"><i class="bi bi-geo-alt"></i> Remote</p></div></div>
                            <span class="deadline-badge" data-deadline="2026-11-08"><i class="bi bi-calendar-event"></i> Apply by 08 Nov</span>
                        </div>
                        <h3>Security Operations Intern</h3>
                        <div class="card-tags"><span class="specialization"><i class="bi bi-mortarboard"></i> Cybersecurity</span><span class="mode-tag"><i class="bi bi-house"></i> Remote</span></div>
                        <p class="opportunity-description">Develop security fundamentals through guided activities, monitoring, and practical team exercises.</p>
                        <dl class="opportunity-facts">
                            <div><dt><i class="bi bi-hourglass-split"></i> Duration</dt><dd>8 weeks</dd></div>
                            <div><dt><i class="bi bi-people"></i> Available seats</dt><dd>2 students</dd></div>
                            <div><dt><i class="bi bi-calendar-event"></i> Deadline</dt><dd><time datetime="2026-11-08">08 Nov 2026</time></dd></div>
                            <div><dt><i class="bi bi-clock-history"></i> Training hours</dt><dd>240 hours</dd></div>
                        </dl>
                        <div class="opportunity-details" hidden><p>Practice reviewing sample security alerts, documenting observations, and applying basic security procedures in a supervised environment.</p><h4>Preferred skills</h4><p>These skills are preferred, not mandatory. Students who are still developing them are welcome to apply.</p><ul><li>Basic networking knowledge.</li><li>Familiarity with operating systems.</li><li>Interest in information security.</li></ul></div>
                        <button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#opportunityModal">View details <i class="bi bi-arrow-right"></i></button>
                    </article>
                </div>
            </section>

            <div id="emptyState" class="empty-state" hidden>
                <div class="empty-art"><i class="bi bi-search"></i></div>
                <h3>No matching opportunities</h3>
                <p>Try a different search or reset your filters.</p>
                <button type="button" class="empty-reset" id="emptyReset"><i class="bi bi-arrow-counterclockwise"></i> Reset filters</button>
            </div>
            <noscript><p class="sample-note">Enable JavaScript to filter opportunities and view their full details.</p></noscript>

            <div class="modal fade explore-modal" id="opportunityModal" tabindex="-1" aria-labelledby="modalTitle" aria-hidden="true">
                <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content">
                        <div class="modal-header">
                            <span class="company-mark lg tone-0" id="modalMark" aria-hidden="true">—</span>
                            <div class="modal-heading">
                                <p id="modalCompany" class="modal-company"></p>
                                <h2 class="modal-title" id="modalTitle">Opportunity details</h2>
                                <div class="modal-chips">
                                    <span id="modalMajor" class="specialization"></span>
                                    <span id="modalLocation" class="chip"></span>
                                </div>
                            </div>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <div id="modalDeadline" class="deadline-banner" hidden></div>
                            <div id="modalFacts" class="modal-facts"></div>
                            <div id="modalDetails" class="modal-details"></div>
                            <p class="sample-note modal-note" id="modalNote"><i class="bi bi-info-circle"></i> Sample opportunity. You can preview the application form without submitting a real application.</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="secondary-button" data-bs-dismiss="modal">Close</button>
                            <a id="applyLink" class="apply-button" href="ApplicationForm.aspx">Apply now <i class="bi bi-arrow-right"></i></a>
                        </div>
                    </div>
                </div>
            </div>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/explore-opportunities.js?v=20261024-view"></script>
</body>
</html>
