<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ManageOpportunities.aspx.cs" Inherits="UIMS.Company.ManageOpportunities" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Manage Opportunities | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    
    <link runat="server" href="~/Assets/company/manage-opportunities.css?v=20261024-modal-spacing" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
<body data-demo-role="company" class="coordinator-banner opportunities-workspace">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar"><a class="brand workspace-nav-brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Company Supervisor</p><nav class="workspace-nav-primary" aria-label="Company Supervisor pages"><a class="workspace-nav-link" href="CompanyDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link active" href="ManageOpportunities.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Opportunities</span></a><a class="workspace-nav-link" href="ApplicationsReview.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Applications Review</span></a><a class="workspace-nav-link" href="MyTrainees.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Trainees</span></a><a class="workspace-nav-link" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Manage Tasks</span></a><a class="workspace-nav-link" href="ManageExams.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Task Exams</span></a><a class="workspace-nav-link" href="VerifyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3h6v6H3V3Zm12 0h6v6h-6V3ZM3 15h6v6H3v-6Zm12 0h2v2h-2v-2Zm4 0h2v6h-6v-2h4v-4Z"/></svg><span>Verify Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Reports</span></a><a class="workspace-nav-link" href="ReportsFinalEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Final Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CompanySupervisorAccount.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <div class="sidebar-backdrop" data-action="toggle-sidebar"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" data-action="toggle-sidebar" aria-label="Open menu"><i class="bi bi-list"></i></button>
   
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" aria-label="Notifications"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown">
                                <span class="avatar">MH</span>
                                <span class="name">Mohammad H.<span class="role">Company Supervisor</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CompanySupervisorAccount.aspx" class="dropdown-item"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><a class="dropdown-item text-danger" aria-disabled="true" title="This page is not available yet"><i class="bi bi-box-arrow-right me-2"></i>Log out</a></li>
                            </ul>
                        </div>
                    </div>
                </header>

                <!-- ============ CONTENT ============ -->
                <main class="content">

                    <!-- Page header -->
                    <section class="page-hero">
                        <div class="page-hero-text">
                            <div class="hero-eyebrow"><i class="bi bi-building"></i> Company Supervisor &middot; Orange Jordan</div>
                            <h1 class="page-title">Manage Opportunities</h1>
                            <p class="page-subtitle">Post internships for JUST students, choose who can apply, and close them when seats are filled.</p>
                        </div>
                        <div class="page-hero-actions">
                            <button type="button" class="btn-hero-pill" data-action="new">
                                <i class="bi bi-plus-lg"></i> New opportunity
                            </button>
                        </div>
                    </section>

                    <!-- Stats -->
                    <div class="row g-3 mb-4">
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon primary"><i class="bi bi-briefcase"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Total</div>
                                    <div class="kpi-value" id="statTotal">0</div>
                                    <div class="kpi-note">Opportunities posted</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon success"><i class="bi bi-unlock"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Open</div>
                                    <div class="kpi-value" id="statOpen">0</div>
                                    <div class="kpi-note">Accepting applications</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon info"><i class="bi bi-person-check"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Seats left</div>
                                    <div class="kpi-value" id="statSeats">0</div>
                                    <div class="kpi-note">Across open opportunities</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon warning"><i class="bi bi-inbox"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Applicants</div>
                                    <div class="kpi-value" id="statApplicants">0</div>
                                    <div class="kpi-note">On open opportunities</div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Toolbar -->
                    <div class="toolbar">
                        <div class="filter-tabs" id="statusTabs" role="tablist" aria-label="Filter by status">
                            <button type="button" class="active" data-status="all">All <span data-count="all">0</span></button>
                            <button type="button" data-status="Open">Open <span data-count="Open">0</span></button>
                            <button type="button" data-status="Closed">Closed <span data-count="Closed">0</span></button>
                        </div>
                        <div class="toolbar-end">
                            <span class="result-count" id="resultCount" aria-live="polite"></span>
                            <div class="toolbar-select">
                                <i class="bi bi-sort-down"></i>
                                <select id="sortBy" aria-label="Sort opportunities">
                                    <option value="newest">Newest first</option>
                                    <option value="deadline">Deadline soonest</option>
                                    <option value="applicants">Most applicants</option>
                                    <option value="title">Title A–Z</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <!-- Opportunity cards (filled by manage-opportunities.js) -->
                    <div class="row g-3 opp-grid" id="oppGrid"></div>

                    <!-- Empty state -->
                    <div class="empty-state" id="emptyState" hidden>
                        <div class="empty-art"><i class="bi bi-briefcase"></i></div>
                        <h3 id="emptyTitle">No opportunities here yet</h3>
                        <p id="emptyText">Post your first internship so JUST students can start applying.</p>
                        <button type="button" class="btn-primary-pill mt-3" data-action="new"><i class="bi bi-plus-lg"></i> New opportunity</button>
                    </div>
                </main>
            </div>
        </div>

        <!-- ============ ADD / EDIT MODAL ============ -->
        <div class="modal fade" id="oppModal" tabindex="-1" aria-labelledby="oppModalTitle" aria-hidden="true">
            <div class="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable">
                <div class="modal-content opp-modal">

                    <!-- Header -->
                    <div class="modal-header">
                        <div class="modal-icon"><i class="bi bi-briefcase" id="oppModalIcon"></i></div>
                        <div class="modal-heading">
                            <h2 class="modal-title" id="oppModalTitle">New opportunity</h2>
                            <p class="modal-sub" id="oppModalSub">Fill in the details below. Students in the selected specialization will see it right away.</p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>

                    <div class="modal-body">
                        <div class="opp-form-grid">
                            <!-- ======== Form ======== -->
                            <div class="opp-form-main" id="oppForm">

                                <!-- 1. Basic information -->
                                <section class="form-block">
                                    <div class="block-head">
                                        <span class="block-num">1</span>
                                        <div>
                                            <h3>Basic information</h3>
                                            <p>What the internship is and what the trainee will do.</p>
                                        </div>
                                    </div>

                                    <div class="field">
                                        <div class="field-top">
                                            <label for="oppTitle">Opportunity title</label>
                                            <span class="char-count" data-for="oppTitle"></span>
                                        </div>
                                        <input type="text" id="oppTitle" class="form-control" maxlength="100" placeholder="e.g. Frontend Developer Intern" />
                                        <div class="field-error" data-error-for="oppTitle" hidden></div>
                                    </div>

                                    <div class="field">
                                        <div class="field-top">
                                            <label for="oppDescription">Description</label>
                                            <span class="char-count" data-for="oppDescription"></span>
                                        </div>
                                        <textarea id="oppDescription" class="form-control" rows="3" maxlength="1000" placeholder="What will the trainee work on? Which team will they join?"></textarea>
                                        <div class="field-error" data-error-for="oppDescription" hidden></div>
                                    </div>

                                    <div class="field mb-0">
                                        <div class="field-top">
                                            <label for="oppRequirements">Requirements <span class="optional">Optional</span></label>
                                            <span class="char-count" data-for="oppRequirements"></span>
                                        </div>
                                        <textarea id="oppRequirements" class="form-control" rows="2" maxlength="600" placeholder="Skills, tools, or courses the student should have"></textarea>
                                        <div class="field-error" data-error-for="oppRequirements" hidden></div>
                                    </div>
                                </section>

                                <!-- 2. Who can apply -->
                                <section class="form-block">
                                    <div class="block-head">
                                        <span class="block-num">2</span>
                                        <div>
                                            <h3>Who can apply</h3>
                                            <p>Only students in the selected specialization will see this opportunity.</p>
                                        </div>
                                    </div>

                                    <div class="field">
                                        <label>Specialization</label>
                                        <ul class="major-picker" id="majorPicker"></ul>
                                        <div class="field-error" data-error-for="majorPicker" hidden></div>
                                    </div>

                                    <div class="row g-3">
                                        <div class="col-sm-6">
                                            <div class="field mb-0">
                                                <label for="oppSeats">Available seats</label>
                                                <div class="stepper">
                                                    <button type="button" class="step-btn" data-step="-1" data-target="oppSeats" aria-label="Fewer seats"><i class="bi bi-dash-lg"></i></button>
                                                    <input type="number" id="oppSeats" class="form-control" min="1" max="50" />
                                                    <button type="button" class="step-btn" data-step="1" data-target="oppSeats" aria-label="More seats"><i class="bi bi-plus-lg"></i></button>
                                                </div>
                                                <div class="field-error" data-error-for="oppSeats" hidden></div>
                                            </div>
                                        </div>
                                        <div class="col-sm-6">
                                            <div class="field mb-0">
                                                <label for="oppRequiredHours">Required training hours</label>
                                                <div class="stepper">
                                                    <button type="button" class="step-btn" data-step="-10" data-target="oppRequiredHours" aria-label="Fewer hours"><i class="bi bi-dash-lg"></i></button>
                                                    <input type="number" id="oppRequiredHours" class="form-control" min="1" max="2000" value="160" required />
                                                    <button type="button" class="step-btn" data-step="10" data-target="oppRequiredHours" aria-label="More hours"><i class="bi bi-plus-lg"></i></button>
                                                </div>
                                                <div class="field-error" data-error-for="oppRequiredHours" hidden></div>
                                            </div>
                                        </div>
                                        <div class="col-12">
                                            <div class="field mb-0">
                                                <label>Work mode</label>
                                                <ul class="segmented" id="workMode">
                                                    <li><input type="radio" name="workMode" id="wmOnsite" value="On-site" checked="checked" /><label for="wmOnsite"><i class="bi bi-building"></i> On-site</label></li>
                                                    <li><input type="radio" name="workMode" id="wmHybrid" value="Hybrid" /><label for="wmHybrid"><i class="bi bi-arrow-left-right"></i> Hybrid</label></li>
                                                    <li><input type="radio" name="workMode" id="wmRemote" value="Remote" /><label for="wmRemote"><i class="bi bi-house"></i> Remote</label></li>
                                                </ul>
                                            </div>
                                        </div>
                                        <div class="col-12">
                                            <div class="field mb-0">
                                                <label for="oppLocation">Location</label>
                                                <div class="input-icon">
                                                    <i class="bi bi-geo-alt"></i>
                                                    <input type="text" id="oppLocation" class="form-control" maxlength="60" placeholder="e.g. Amman, Shmeisani" />
                                                </div>
                                                <div class="field-error" data-error-for="oppLocation" hidden></div>
                                            </div>
                                        </div>
                                    </div>
                                </section>

                                <!-- 3. Dates -->
                                <section class="form-block">
                                    <div class="block-head">
                                        <span class="block-num">3</span>
                                        <div>
                                            <h3>Dates</h3>
                                            <p>When applications close and when training starts.</p>
                                        </div>
                                    </div>

                                    <div class="row g-3">
                                        <div class="col-sm-4">
                                            <div class="field mb-0">
                                                <label for="oppDeadline">Application deadline</label>
                                                <input type="date" id="oppDeadline" class="form-control" />
                                                <div class="field-error" data-error-for="oppDeadline" hidden></div>
                                            </div>
                                        </div>
                                        <div class="col-sm-4">
                                            <div class="field mb-0">
                                                <label for="oppStart">Expected start date</label>
                                                <input type="date" id="oppStart" class="form-control" />
                                                <div class="field-error" data-error-for="oppStart" hidden></div>
                                            </div>
                                        </div>
                                        <div class="col-sm-4">
                                            <div class="field mb-0">
                                                <label for="oppDuration">Duration (weeks)</label>
                                                <div class="stepper">
                                                    <button type="button" class="step-btn" data-step="-1" data-target="oppDuration" aria-label="Fewer weeks"><i class="bi bi-dash-lg"></i></button>
                                                    <input type="number" id="oppDuration" class="form-control" min="1" max="52" />
                                                    <button type="button" class="step-btn" data-step="1" data-target="oppDuration" aria-label="More weeks"><i class="bi bi-plus-lg"></i></button>
                                                </div>
                                                <div class="field-error" data-error-for="oppDuration" hidden></div>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="date-hint" id="dateHint">
                                        <i class="bi bi-calendar-check"></i>
                                        <span id="dateHintText">Training weeks begin on company acceptance. Attendance is available at any hour during that period.</span>
                                    </div>
                                </section>
                            </div>

                            <!-- ======== Live preview ======== -->
                            <aside class="opp-preview">
                                <div class="preview-sticky">
                                    <div class="preview-label"><span class="live-dot"></span> Live preview</div>
                                    <div class="opp-card preview-card">
                                        <div class="opp-top">
                                            <span class="status success" id="pvStatus">Open</span>
                                            <span class="opp-deadline"><i class="bi bi-calendar-event"></i> Apply by <span id="pvDeadline">—</span></span>
                                        </div>
                                        <div class="opp-head">
                                            <div class="opp-logo" id="pvLogo">OJ</div>
                                            <div class="opp-head-text">
                                                <h3 class="opp-title is-placeholder" id="pvTitle">Opportunity title</h3>
                                                <div class="opp-company" id="pvCompany">Orange Jordan</div>
                                            </div>
                                        </div>
                                        <div class="opp-meta">
                                            <span><i class="bi bi-geo-alt"></i><span id="pvLocation">Location</span></span>
                                            <span><i class="bi bi-laptop"></i><span id="pvMode">On-site</span></span>
                                            <span><i class="bi bi-hourglass-split"></i><span id="pvDuration">8</span>wks</span>
                                            <span><i class="bi bi-clock-history"></i><span id="pvHours">160</span>h</span>
                                        </div>
                                        <p class="opp-desc is-placeholder" id="pvDesc">A short description of the internship appears here.</p>
                                        <div class="opp-majors" id="pvMajors"><span class="tag tag-empty">No specialization selected</span></div>
                                        <div class="opp-seats">
                                            <div class="seats-row">
                                                <span><strong id="pvAccepted">0</strong> of <span id="pvSeats">1</span> seats filled</span>
                                                <span class="text-secondary"><span id="pvApplicants">0</span> applicants</span>
                                            </div>
                                            <div class="progress-slim"><span id="pvBar" style="width: 0%"></span></div>
                                        </div>
                                    </div>
                                    <p class="preview-note"><i class="bi bi-info-circle"></i> This is how the card looks on your list and to students.</p>
                                </div>
                            </aside>
                        </div>
                    </div>

                    <!-- Footer -->
                    <div class="modal-footer">
                        <span class="footer-hint"><i class="bi bi-shield-check"></i> All fields except requirements are required</span>
                        <button type="button" class="btn-ghost lg" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn-primary-pill" id="saveOpp"><i class="bi bi-check-lg"></i> <span id="saveOppText">Publish opportunity</span></button>
                    </div>
                </div>
            </div>
        </div>

        <!-- ============ CLOSE CONFIRM MODAL ============ -->
        <div class="modal fade confirm-modal" id="closeModal" tabindex="-1" aria-labelledby="closeTitle" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <div class="modal-icon danger"><i class="bi bi-lock"></i></div>
                        <div class="modal-heading">
                            <h2 class="modal-title" id="closeTitle">Close this opportunity?</h2>
                            <p class="modal-sub" id="closeSub"></p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <ul class="consequence-list">
                            <li><span class="cl-icon danger"><i class="bi bi-x-circle"></i></span> Students can no longer see it or apply.</li>
                            <li><span class="cl-icon success"><i class="bi bi-people"></i></span> Accepted students keep their seats.</li>
                            <li id="closePendingLine" hidden><span class="cl-icon warning"><i class="bi bi-hourglass-split"></i></span> <span></span></li>
                        </ul>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn-ghost lg" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn-primary-pill danger" id="confirmClose"><i class="bi bi-lock"></i> Close opportunity</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- ============ TOAST ============ -->
        <div class="toast-zone">
            <div class="toast page-toast" id="pageToast" role="status" aria-live="polite" aria-atomic="true">
                <i class="bi bi-check-circle-fill" id="toastIcon"></i>
                <span id="toastText"></span>
            </div>
        </div>
    </form>

    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="../Js_project/Company/manage-opportunities.js?v=20261023-modern"></script>
</body>
</html>
