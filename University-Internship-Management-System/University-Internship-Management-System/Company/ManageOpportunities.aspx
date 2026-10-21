<%@ Page Language="C#" AutoEventWireup="true" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Manage Opportunities | JUST Internships</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link href="~/Assets/company/company-dashboard.css" rel="stylesheet" />
    <link href="~/Assets/company/manage-opportunities.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar" id="sidebar">
                <a href="CompanyDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav">
                    <li><a href="CompanyDashboard.aspx"><i class="bi bi-house-door"></i>Dashboard</a></li>
                    <li><a href="ManageOpportunities.aspx" class="active"><i class="bi bi-briefcase"></i>Manage Opportunities</a></li>
                    <li><a href="ApplicationsReview.aspx"><i class="bi bi-inbox"></i>Applications Review<span class="count">6</span></a></li>
                    <li><a href="MyTrainees.aspx"><i class="bi bi-people"></i>My Trainees</a></li>
                    <li><a href="ManageTasks.aspx"><i class="bi bi-list-check"></i>Manage &amp; Review Tasks</a></li>
                    <li><a href="FinalEvaluation.aspx"><i class="bi bi-clipboard-check"></i>Reports &amp; Final Evaluation</a></li>
                </ul>

                <div class="side-divider"></div>

                <ul class="side-nav">
                    <li><a href="Account.aspx"><i class="bi bi-gear"></i>Supervisor Account</a></li>
                    <li><a href="../Contact.aspx"><i class="bi bi-question-circle"></i>Help</a></li>
                </ul>
            </aside>
            <div class="sidebar-backdrop" data-action="toggle-sidebar"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" data-action="toggle-sidebar" aria-label="Open menu"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input type="search" id="searchBox" placeholder="Search your opportunities..." autocomplete="off" />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" aria-label="Notifications"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown">
                                <span class="avatar">MH</span>
                                <span class="name">Mohammad H.<span class="role">Company Supervisor</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Account.aspx"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><a class="dropdown-item text-danger" href="../Login.aspx"><i class="bi bi-box-arrow-right me-2"></i>Log out</a></li>
                            </ul>
                        </div>
                    </div>
                </header>

                <!-- ============ CONTENT ============ -->
                <main class="content">

                    <!-- Page header -->
                    <div class="page-head">
                        <div>
                            <div class="date-line">Company Supervisor · Orange Jordan</div>
                            <h1 class="page-title">Manage Opportunities</h1>
                            <p class="subtitle mb-0">Post internships for JUST students, choose who can apply, and close them when seats are filled.</p>
                        </div>
                        <button type="button" class="btn-primary-pill" data-action="new">
                            <i class="bi bi-plus-lg"></i> New opportunity
                        </button>
                    </div>

                    <!-- Stats -->
                    <div class="row g-3 mb-4">
                        <div class="col-6 col-xl-3">
                            <div class="stat-card blue">
                                <div class="stat-icon"><i class="bi bi-briefcase"></i></div>
                                <div class="stat-label">Total</div>
                                <div class="stat-value" id="statTotal">00</div>
                                <div class="stat-note">Opportunities posted</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card ice">
                                <div class="stat-icon success"><i class="bi bi-unlock"></i></div>
                                <div class="stat-label">Open</div>
                                <div class="stat-value" id="statOpen">00</div>
                                <div class="stat-note">Accepting applications</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon"><i class="bi bi-person-check"></i></div>
                                <div class="stat-label">Seats left</div>
                                <div class="stat-value" id="statSeats">00</div>
                                <div class="stat-note">Across open opportunities</div>
                            </div>
                        </div>
                        <div class="col-6 col-xl-3">
                            <div class="stat-card">
                                <div class="stat-icon warning"><i class="bi bi-inbox"></i></div>
                                <div class="stat-label">Applicants</div>
                                <div class="stat-value" id="statApplicants">00</div>
                                <div class="stat-note">On open opportunities</div>
                            </div>
                        </div>
                    </div>

                    <!-- Toolbar -->
                    <div class="filter-bar">
                        <div class="filter-tabs" id="statusTabs" role="tablist">
                            <button type="button" class="active" data-status="all">All <span data-count="all">0</span></button>
                            <button type="button" data-status="Open">Open <span data-count="Open">0</span></button>
                            <button type="button" data-status="Closed">Closed <span data-count="Closed">0</span></button>
                        </div>
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

                    <!-- Opportunity cards (filled by manage-opportunities.js) -->
                    <div class="row g-3" id="oppGrid"></div>

                    <!-- Empty state -->
                    <div class="empty-state" id="emptyState" hidden>
                        <i class="bi bi-briefcase"></i>
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
                            <p class="modal-sub" id="oppModalSub">Fill in the details below. Students in the selected majors will see it right away.</p>
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
                                            <label for="oppRequirements">Requirements</label>
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
                                            <p>Only students in the selected majors will see this opportunity.</p>
                                        </div>
                                    </div>

                                    <div class="field">
                                        <label>Specializations</label>
                                        <ul class="major-picker" id="majorPicker"></ul>
                                        <div class="field-error" data-error-for="majorPicker" hidden></div>
                                    </div>

                                    <div class="row g-3">
                                        <div class="col-sm-5">
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
                                        <div class="col-sm-7">
                                            <div class="field mb-0">
                                                <label>Work mode</label>
                                                <ul class="segmented" id="workMode">
                                                    <li><input type="radio" name="workMode" id="wmOnsite" value="On-site" checked="checked" /><label for="wmOnsite">On-site</label></li>
                                                    <li><input type="radio" name="workMode" id="wmHybrid" value="Hybrid" /><label for="wmHybrid">Hybrid</label></li>
                                                    <li><input type="radio" name="workMode" id="wmRemote" value="Remote" /><label for="wmRemote">Remote</label></li>
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
                                                <label for="oppStart">Start date</label>
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
                                        <span id="dateHintText">Pick a start date and duration to see when the internship ends.</span>
                                    </div>
                                </section>
                            </div>

                            <!-- ======== Live preview ======== -->
                            <aside class="opp-preview">
                                <div class="preview-sticky">
                                    <div class="preview-label"><i class="bi bi-eye"></i> Live preview</div>
                                    <div class="opp-card preview-card">
                                        <div class="opp-top">
                                            <span class="status success" id="pvStatus">Open</span>
                                            <span class="opp-deadline"><i class="bi bi-calendar-event"></i> Apply by <span id="pvDeadline">—</span></span>
                                        </div>
                                        <h3 class="opp-title" id="pvTitle">Opportunity title</h3>
                                        <div class="opp-meta">
                                            <span><i class="bi bi-geo-alt"></i><span id="pvLocation">Location</span></span>
                                            <span><i class="bi bi-laptop"></i><span id="pvMode">On-site</span></span>
                                            <span><i class="bi bi-hourglass-split"></i><span id="pvDuration">8</span> weeks</span>
                                        </div>
                                        <p class="opp-desc" id="pvDesc">A short description of the internship appears here.</p>
                                        <div class="opp-majors" id="pvMajors"><span class="tag tag-empty">No majors selected</span></div>
                                        <div class="opp-seats">
                                            <div class="d-flex justify-content-between">
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
                        <span class="footer-hint"><i class="bi bi-shield-check"></i> All fields are required</span>
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
                            <li><i class="bi bi-x-circle"></i> Students can no longer see it or apply.</li>
                            <li><i class="bi bi-people"></i> Accepted students keep their seats.</li>
                            <li id="closePendingLine"><i class="bi bi-hourglass-split"></i> <span></span></li>
                            <li><i class="bi bi-arrow-counterclockwise"></i> You can reopen it anytime.</li>
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

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Company/manage-opportunities.js"></script>
</body>
</html>
