<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyTasks.aspx.cs" Inherits="University_Internship_Management_System.Student.MyTasks" %>

<!DOCTYPE html>
<html lang="en">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>My Tasks | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />

        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/student/MyTasks.css?v=20261022-task-board" rel="stylesheet" />
        <link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
</head>
    <body data-demo-role="student">
        <form id="form1" runat="server">
            <div class="app">

                <!-- ============ SIDEBAR (Student) ============ -->
                <aside class="sidebar" id="sidebar" aria-label="Student navigation">
                    <button type="button" class="sidebar-close" aria-label="Close navigation">
                        <i class="bi bi-x-lg" aria-hidden="true">
                        </i>
                    </button>
                    <a href="StudentDashboard.aspx" class="brand">
                        <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48" />
                        <div class="brand-text">
                            <strong>JUST</strong>
                            <span>Internships</span>
                        </div>
                    </a>

                    <ul class="side-nav">
                        <li>
                            <a href="StudentDashboard.aspx">
                                <i class="bi bi-house-door" aria-hidden="true">
                                </i>
                                <span>Dashboard</span>
                            </a>
                        </li>
                        <li>
                            <a href="ExploreOpportunities.aspx">
                                <i class="bi bi-compass" aria-hidden="true">
                                </i>
                                <span>Explore Opportunities</span>
                            </a>
                        </li>
                        <li>
                            <a href="MyApplications.aspx">
                                <i class="bi bi-file-earmark-text" aria-hidden="true">
                                </i>
                                <span>My Applications</span>
                            </a>
                        </li>
                        <li>
                            <a href="ApplicationForm.aspx">
                                <i class="bi bi-file-earmark-plus" aria-hidden="true">
                                </i>
                                <span>Application Form</span>
                            </a>
                        </li>
                        <li>
                            <a href="MyTasks.aspx" class="active" aria-current="page">
                                <i class="bi bi-list-check" aria-hidden="true">
                                </i>
                                <span>My Tasks</span>
                            </a>
                        </li>
                        <li><a href="MyExams.aspx"><i class="bi bi-patch-question" aria-hidden="true"></i><span>My Exams</span></a></li>
                        <li><a href="WeeklyReports.aspx"><i class="bi bi-file-earmark-text" aria-hidden="true"></i><span>Weekly Report Status</span></a></li>
                        <li>
                            <a href="MyProgressEvaluation.aspx">
                                <i class="bi bi-graph-up-arrow" aria-hidden="true">
                                </i>
                                <span>Progress &amp; Evaluation</span>
                            </a>
                        </li>
                        <li>
                            <a href="Profile.aspx">
                                <i class="bi bi-person" aria-hidden="true">
                                </i>
                                <span>My Profile</span>
                            </a>
                        </li>
                    </ul>

                    <div class="side-divider">
                    </div>

                    <ul class="side-nav">
                        <li>
                            <a aria-disabled="true" title="Page not available yet">
                                <i class="bi bi-gear" aria-hidden="true">
                                </i>Settings</a>
                        </li>
                        <li>
                            <a href="../Contact.aspx">
                                <i class="bi bi-question-circle" aria-hidden="true">
                                </i>Help</a>
                        </li>
                        <li>
                            <asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CssClass="text-danger">
                                <i class="bi bi-box-arrow-right" aria-hidden="true">
                                </i>Logout</asp:LinkButton>
                        </li>
                    </ul>

                    <div class="side-promo">
                        <i class="bi bi-mortarboard-fill" aria-hidden="true">
                        </i>
                        <p>Build your future<br />with real experience.</p>
                        <a href="ExploreOpportunities.aspx">Explore opportunities &rarr;</a>
                    </div>
                </aside>
                <div class="sidebar-backdrop" aria-hidden="true">
                </div>

                <div class="main">
                    <!-- TOPBAR -->
                    <header class="topbar">
                        <button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false">
                            <i class="bi bi-list" aria-hidden="true">
                            </i>
                        </button>
                        <div class="topbar-actions ms-auto">
                            <div class="user-chip">
                                <span class="avatar">ST</span>
                                <span class="name">Student Portal<span class="role">Student workspace</span>
                                </span>
                            </div>
                        </div>
                    </header>

                    <!-- CONTENT -->
                    <div class="content">
                        <p class="preview-note">Loading live task records from Firebase…</p>
                        <div id="pageNotice" class="page-notice" role="status" aria-live="polite" hidden>
                        </div>
                        <div class="page-header animate-fade-up template-style-1">
                            <h1 class="greeting">My Tasks</h1>
                            <p class="subtitle">View assigned tasks, track deadlines, and submit your work.</p>
                        </div>

                        <!-- STATS CARDS -->
                        <div class="stats-grid">
                            <div class="stat-card animate-fade-up template-style-2">
                                <div class="stat-icon primary">
                                    <i class="bi bi-list-task" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>4</h3>
                                    <span>Total Tasks</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-3">
                                <div class="stat-icon warning">
                                    <i class="bi bi-hourglass-split" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>1</h3>
                                    <span>In Progress</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-4">
                                <div class="stat-icon primary-soft">
                                    <i class="bi bi-eye" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>1</h3>
                                    <span>Awaiting Review</span>
                                </div>
                            </div>
                            <div class="stat-card border-danger-left animate-fade-up template-style-5">
                                <div class="stat-icon danger">
                                    <i class="bi bi-exclamation-circle" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3 class="text-danger">1</h3>
                                    <span class="text-danger fw-semibold">Revision Requested</span>
                                </div>
                            </div>
                        </div>

                        <!-- SEARCH & FILTER TOOLBAR -->
                        <div class="toolbar animate-fade-up template-style-6">
                            <div class="search-box">
                                <i class="bi bi-search" aria-hidden="true">
                                </i>
                                <input type="text" id="taskSearch" aria-label="Search tasks" placeholder="Search tasks by name..." />
                            </div>
                            <div class="filter-box">
                                <select id="taskFilter" aria-label="Filter by task status" class="form-select">
                                    <option value="all">All Statuses</option>
                                    <option value="assigned">In Progress</option>
                                    <option value="submitted">Awaiting Review</option>
                                    <option value="revision">Revision Requested</option>
                                    <option value="completed">Completed</option>
                                </select>
                            </div>
                        </div>

                        <!-- TASK LIST -->
                        <div class="task-board-toolbar" id="taskBoardToolbar">
                            <label for="boardPlacementSelect">Internship placement</label>
                            <select id="boardPlacementSelect" class="form-select" aria-label="Select internship task board"></select>
                            <div class="btn-group" role="group" aria-label="Task view">
                                <button type="button" class="btn btn-primary" id="boardModeButton" aria-pressed="true">Board</button>
                                <button type="button" class="btn btn-outline-primary" id="listModeButton" aria-pressed="false">List</button>
                            </div>
                            <span class="task-board-help">Board positions are shared with your company supervisor. Task actions determine workflow status.</span>
                        </div>
                        <section id="taskBoard" class="task-board" aria-label="My task board" aria-live="polite"></section>
                        <div class="task-list" id="taskList">

<!-- Empty State -->
                            <div id="noTasksMsg" class="no-tasks animate-fade-up template-style-11">
                                <div class="empty-icon">
                                    <i class="bi bi-inbox" aria-hidden="true">
                                    </i>
                                </div>
                                <h4 class="fw-bold mt-3 mb-1">Loading tasks</h4>
                                <p class="text-muted m-0">Loading live task records from Firebase.</p>
                            </div>

                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ SUBMIT MODAL ============ -->
            <div class="modal" id="submitModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header pb-0 border-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold template-style-12">Submit Work</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body p-4">
                            <p class="text-muted mb-4 template-style-13">Provide your work details, repository links, or attach files for supervisor review.</p>

                            <div id="modalError" class="alert alert-danger custom-alert template-style-14">
                            </div>

                            <div id="studentTaskEvaluation" class="alert alert-success" role="status" hidden>
                                <strong>Institution evaluation</strong>
                                <div class="mt-2">Score: <span id="studentTaskScore"></span> / <span id="studentTaskMaximum"></span></div>
                                <p id="studentTaskFeedback" class="mb-0 mt-2"></p>
                            </div>

                            <div class="mb-4">
                                <label class="form-label custom-label">Work Description <span class="text-danger">*</span>
                                </label>
                                <div id="studentSubmissionHistory" class="text-muted small"></div><div id="studentTaskAttachments" class="small mb-3" aria-live="polite"></div><textarea id="workDesc" class="form-control custom-input" rows="4" placeholder="Describe what you completed...">
                                </textarea>
                            </div>

                            <div class="mb-4">
                                <label class="form-label custom-label">Link (GitHub / Drive) <span class="text-danger">*</span>
                                </label>
                                <input type="url" id="workLink" class="form-control custom-input" placeholder="https://github.com/..." />
                            </div>

                            <div class="mb-2">
                                <label class="form-label custom-label">Attach File (Optional)</label>
                                <input type="file" id="workFile" class="form-control custom-input" accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg" />
                                <small class="text-muted mt-2 d-block template-style-15">Optional: PDF, DOCX, XLSX, PPTX, TXT, PNG or JPEG · maximum 10 MiB. Files are stored privately in Firebase Storage.</small>
                            </div>
                        </div>
                        <div class="modal-footer border-0 pb-4 px-4">
                            <button type="button" class="btn btn-light custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="confirmSubmitBtn" class="btn btn-primary custom-btn-primary">Submit for Review</button>
                        </div>
                    </div>
                </div>
            </div>

        </form>
        <script src="../Scripts/bootstrap.bundle.min.js">
        </script>
        <script src="../Js_project/workflow-dialogs.js?v=20261023"></script>
        <script src="../Js_project/Student/MyTasks.js?v=20261023-workflow">
        </script>
    </body>
</html>
