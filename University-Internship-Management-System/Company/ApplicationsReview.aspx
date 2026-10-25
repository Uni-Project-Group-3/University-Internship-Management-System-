<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ApplicationsReview.aspx.cs" Inherits="University_Internship_Management_System.Company.ApplicationsReview" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Applications Review | JUST Internships</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    
    <link runat="server" href="~/Assets/company/applications-review.css?v=20261023-modern" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
<body class="coordinator-banner" data-demo-role="company">
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar"><a class="brand workspace-nav-brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Company Supervisor</p><nav class="workspace-nav-primary" aria-label="Company Supervisor pages"><a class="workspace-nav-link" href="CompanyDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ManageOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Opportunities</span></a><a class="workspace-nav-link active" href="ApplicationsReview.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Applications Review</span></a><a class="workspace-nav-link" href="MyTrainees.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Trainees</span></a><a class="workspace-nav-link" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Manage Tasks</span></a><a class="workspace-nav-link" href="ManageExams.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Task Exams</span></a><a class="workspace-nav-link" href="VerifyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3h6v6H3V3Zm12 0h6v6h-6V3ZM3 15h6v6H3v-6Zm12 0h2v2h-2v-2Zm4 0h2v6h-6v-2h4v-4Z"/></svg><span>Verify Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Reports</span></a><a class="workspace-nav-link" href="ReportsFinalEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Final Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CompanySupervisorAccount.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
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
                            <h1 class="page-title">Applications Review</h1>
                            <p class="page-subtitle">Review JUST students who applied to your opportunities. Accept them, or reject with a reason they can read.</p>
                        </div>
                        <div class="hero-pending" aria-live="polite">
                            <span class="hero-pending-value" id="heroPending">0</span>
                            <span class="hero-pending-label">waiting for<br />your decision</span>
                        </div>
                    </section>

                    <!-- Stats -->
                    <div class="row g-3 mb-4">
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon warning"><i class="bi bi-hourglass-split"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Pending</div>
                                    <div class="kpi-value" id="statPending">0</div>
                                    <div class="kpi-note">Waiting for your decision</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon success"><i class="bi bi-check2-circle"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Accepted</div>
                                    <div class="kpi-value" id="statAccepted">0</div>
                                    <div class="kpi-note" id="statAcceptedNote">—</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon danger"><i class="bi bi-x-circle"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Rejected</div>
                                    <div class="kpi-value" id="statRejected">0</div>
                                    <div class="kpi-note">With a reason sent</div>
                                </div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="kpi">
                                <div class="kpi-icon primary"><i class="bi bi-person-check"></i></div>
                                <div class="kpi-body">
                                    <div class="kpi-label">Seats left</div>
                                    <div class="kpi-value" id="statSeats">0</div>
                                    <div class="kpi-note">Across open opportunities</div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Toolbar -->
                    <div class="review-toolbar">
                        <div class="filter-tabs" id="statusTabs" role="tablist" aria-label="Filter by status">
                            <button type="button" class="active" data-status="all">All <span data-count="all">0</span></button>
                            <button type="button" data-status="Pending">Pending <span data-count="Pending">0</span></button>
                            <button type="button" data-status="Accepted">Accepted <span data-count="Accepted">0</span></button>
                            <button type="button" data-status="Rejected">Rejected <span data-count="Rejected">0</span></button>
                        </div>

                        <div class="toolbar-end">
                            <div class="toolbar-select">
                                <i class="bi bi-briefcase"></i>
                                <select id="oppFilter" aria-label="Filter by opportunity">
                                    <option value="all">All opportunities</option>
                                </select>
                            </div>
                            <div class="toolbar-select sort">
                                <i class="bi bi-sort-down"></i>
                                <select id="sortBy" aria-label="Sort applications">
                                    <option value="newest">Newest first</option>
                                    <option value="oldest">Oldest first</option>
                                    <option value="name">Name A–Z</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <!-- Selected opportunity summary (shown when one opportunity is chosen) -->
                    <div class="opp-summary" id="oppSummary" hidden></div>

                    <!-- Applications list -->
                    <div class="app-panel">
                        <div class="panel-bar">
                            <h2 class="panel-bar-title"><i class="bi bi-people"></i> Applicants</h2>
                            <span class="result-count" id="resultCount" aria-live="polite"></span>
                        </div>
                        <table class="app-table">
                            <thead>
                                <tr>
                                    <th>Student</th>
                                    <th>Opportunity</th>
                                    <th>Applied</th>
                                    <th>Status</th>
                                    <th class="text-end">Actions</th>
                                </tr>
                            </thead>
                            <tbody id="appRows"></tbody>
                        </table>

                        <div class="empty-state" id="emptyState" hidden>
                            <div class="empty-art"><i class="bi bi-inbox"></i></div>
                            <h3 id="emptyTitle">No applications match</h3>
                            <p id="emptyText">Try another opportunity, status, or search word.</p>
                        </div>
                    </div>
                </main>
            </div>
        </div>

        <!-- ============ DETAILS DRAWER ============ -->
        <div class="offcanvas offcanvas-end app-drawer" tabindex="-1" id="appDrawer" aria-labelledby="drawerName">
            <div class="offcanvas-header">
                <div class="drawer-person">
                    <span class="avatar lg" id="drawerAvatar">--</span>
                    <div class="min-w-0">
                        <h2 class="offcanvas-title" id="drawerName">Student</h2>
                        <div class="drawer-sub" id="drawerSub"></div>
                        <div class="drawer-status" id="drawerStatus"></div>
                    </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Close"></button>
            </div>
            <div class="offcanvas-body" id="drawerBody"></div>
            <div class="drawer-footer" id="drawerFooter"></div>
        </div>

        <!-- ============ ACCEPT MODAL ============ -->
        <div class="modal fade review-modal" id="acceptModal" tabindex="-1" aria-labelledby="acceptTitle" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <div class="modal-icon success"><i class="bi bi-check2-circle"></i></div>
                        <div class="modal-heading">
                            <h2 class="modal-title" id="acceptTitle">Accept application</h2>
                            <p class="modal-sub" id="acceptSub"></p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="seat-preview" id="acceptSeats"></div>
                        <div class="info-note">
                            <i class="bi bi-info-circle"></i>
                            <span>The student must confirm your offer. They can confirm only one offer — if they pick another company, the seat is released.</span>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn-ghost lg" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn-primary-pill success" id="confirmAccept"><i class="bi bi-check-lg"></i> Accept student</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- ============ REJECT MODAL ============ -->
        <div class="modal fade review-modal" id="rejectModal" tabindex="-1" aria-labelledby="rejectTitle" aria-hidden="true">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <div class="modal-icon danger"><i class="bi bi-x-circle"></i></div>
                        <div class="modal-heading">
                            <h2 class="modal-title" id="rejectTitle">Reject application</h2>
                            <p class="modal-sub" id="rejectSub"></p>
                        </div>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <label class="field-label">Reason</label>
                        <div class="reason-chips" id="reasonChips">
                            <label><input type="radio" name="rejectReason" value="All seats are filled" /><span>All seats are filled</span></label>
                            <label><input type="radio" name="rejectReason" value="Skills don't match the requirements" /><span>Skills don't match the requirements</span></label>
                            <label><input type="radio" name="rejectReason" value="Major doesn't fit the role" /><span>Major doesn't fit the role</span></label>
                            <label><input type="radio" name="rejectReason" value="Incomplete application" /><span>Incomplete application</span></label>
                            <label><input type="radio" name="rejectReason" value="Other" /><span>Other</span></label>
                        </div>
                        <div class="field-error" id="reasonError" hidden>Choose a reason.</div>

                        <div class="field-top mt-3">
                            <label class="field-label" for="rejectMessage">Message to the student</label>
                            <span class="char-count" id="rejectCount">0 / 400</span>
                        </div>
                        <textarea id="rejectMessage" class="form-control" rows="4" maxlength="400" placeholder="Explain the decision kindly — the student will see this message."></textarea>
                        <div class="field-error" id="messageError" hidden>Write a message for the student.</div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn-ghost lg" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn-primary-pill danger" id="confirmReject"><i class="bi bi-x-lg"></i> Reject application</button>
                    </div>
                </div>
            </div>
        </div>

        <!-- ============ TOAST ============ -->
        <div class="toast-zone">
            <div class="toast review-toast" id="reviewToast" role="status" aria-live="polite" aria-atomic="true">
                <i class="bi bi-check-circle-fill" id="toastIcon"></i>
                <span id="toastText"></span>
            </div>
        </div>
    </form>

    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="<%= ResolveUrl("~/Scripts/bootstrap.bundle.min.js") %>"></script>
    <script src="../Js_project/Company/applications-review.js?v=20261023-modern"></script>
</body>
</html>
