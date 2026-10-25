<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyTasks.aspx.cs" Inherits="University_Internship_Management_System.Student.MyTasks" %>
<%@ Register Src="~/Shared/StudentSidebar.ascx" TagPrefix="just" TagName="RoleSidebar" %>


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
        <link href="../Assets/shared/workflow-ui.css?v=20261025-send-certificate" rel="stylesheet" />
<script src="../Js_project/shared-role-navigation.js" defer></script>
<link rel="stylesheet" href="../Assets/shared/role-sidebar.css" />
<link rel="stylesheet" href="../Assets/shared/just-theme.css" />

<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
    <body class="coordinator-banner uims-tasks-theme" data-demo-role="student">
        <form id="form1" runat="server">
<just:RoleSidebar ID="SharedRoleNavigation" runat="server" SidebarId="sidebar" />
            <div class="app">

                <!-- ============ SIDEBAR (Student) ============ -->



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
                        <section class="tasks-premium-hero"><div><span class="tasks-role-badge">JUST / STUDENT WORKSPACE</span><h1>My Tasks & Exams</h1><p>Track your assignments, examinations and progress in one place.</p></div><div class="tasks-hero-metrics"><span><strong id="heroTaskCount">—</strong> active tasks</span><span><strong id="heroExamCount">—</strong> exam tasks</span></div></section>

                        <!-- STATS CARDS -->
                        <div class="stats-grid tasks-premium-stats"><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 6h16M4 12h16M4 18h16"/></svg></span><div class="stat-details"><h3>—</h3><span>Total Tasks</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M12 3v9l6 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg></span><div class="stat-details"><h3>—</h3><span>New / Pending</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></span><div class="stat-details"><h3>—</h3><span>In Progress</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 3h14v18H5V3Zm4 5h6m-6 5h6"/></svg></span><div class="stat-details"><h3>—</h3><span>Awaiting Review</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="m5 12 4 4L19 6"/></svg></span><div class="stat-details"><h3>—</h3><span>Complete</span></div></article></div>


                        <!-- SEARCH & FILTER TOOLBAR -->
                        <div class="toolbar task-filter-toolbar" id="taskBoardToolbar" aria-label="Task filters and view">
                            <label class="task-filter-field" for="taskSearch">Task Name<select id="taskSearch" aria-label="Task Name"><option value="">All Tasks</option></select></label>
                            <label class="task-filter-field" for="taskFilter">Task Status<select id="taskFilter" aria-label="Task Status">
                                <option value="all">All Statuses</option><option value="assigned">New / Pending</option><option value="inprogress">In Progress</option><option value="submitted">Review</option><option value="completed">Complete</option><option value="revision">Revision Requested</option>
                            </select></label>
                            <div class="btn-group task-view-toggle" role="group" aria-label="Task view"><button type="button" class="btn btn-primary" id="boardModeButton" aria-pressed="true">Board</button><button type="button" class="btn btn-outline-primary" id="listModeButton" aria-pressed="false">List</button></div>
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
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable student-task-dialog">
                    <div class="modal-content custom-modal">
                        <div class="modal-header pb-0 border-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold template-style-12">Submit Work</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>

                        <div class="modal-body p-4">
                            <div id="modalError" role="alert" class="alert alert-danger custom-alert template-style-14">
                            </div>
                            <section id="studentPanelDetails" data-student-panel="details">
                            <div id="studentTaskInstructions" class="student-task-summary"></div>
                            <div id="studentTaskEvaluation" class="alert alert-success" role="status" hidden>
                                <strong>Company task grade</strong>
                                <div class="mt-2">Score: <span id="studentTaskScore"></span> / <span id="studentTaskMaximum"></span></div>
                                <p id="studentTaskFeedback" class="mb-0 mt-2"></p>
                            </div>
                            </section>
                            <section id="studentPanelSubmission" data-student-panel="submission">
                            <h6 class="fw-bold">Submit your work</h6>
                            <p class="text-muted">Upload your completed work or provide a work link for your company supervisor. Notes are optional with an attachment.</p>
                            <div class="mb-4">
                                <label for="workDesc" class="form-label custom-label">Work Description (required without an attachment)
                                </label>
                                <textarea id="workDesc" class="form-control custom-input" rows="6" placeholder="Describe what you completed...">
                                </textarea>
                            </div>

                            <div class="mb-4">
                                <label for="workLink" class="form-label custom-label">Work link (required without an attachment)
                                </label>
                                <input type="url" id="workLink" class="form-control custom-input" placeholder="https://github.com/..." />
                            </div>

                            <div class="mb-2">
                                <label for="workFile" class="form-label custom-label">Attach file (optional)</label>
                                <input type="file" id="workFile" class="form-control custom-input" accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg" />
                                <small class="text-muted mt-2 d-block template-style-15">Optional: PDF, DOCX, XLSX, PPTX, TXT, PNG or JPEG &middot; maximum 10 MiB. Files are stored privately in Firebase Storage.</small>
                            </div>
                            </section>
                            <section id="studentPanelExam" data-student-panel="exam">
                                <div id="taskExamWorkspace"><p id="taskExamNotice" class="notice" role="status">Open this tab to load the exam linked to this task.</p><div id="taskExamList" class="exam-list"></div></div>
                            </section>
                            <section id="studentPanelFiles" data-student-panel="files"><h6 class="fw-bold">Resources and submitted files</h6><div id="studentTaskAttachments" aria-live="polite"></div></section>
                            <section id="studentPanelHistory" data-student-panel="history"><h6 class="fw-bold">Submissions and instruction history</h6><div id="studentSubmissionHistory"></div></section>
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
        <link href="../Assets/task-workspace.css?v=20261023-task-connection" rel="stylesheet" />
<link href="../Assets/tasks-premium.css?v=20261024-buttons" rel="stylesheet" />
        <script src="../Js_project/firebase-client.js?v=20261023-task-connection"></script>
        <script src="../Js_project/task-dialogs.js?v=20261025-confirm-popup"></script>
        <script src="../Js_project/Student/TaskExams.js?v=20261025-exam-cards"></script>
        <script src="../Js_project/workflow-dialogs.js?v=20261023"></script>
        <script src="../Js_project/Student/MyTasks.js?v=20261025-flow">
        </script>
    <link href="../Assets/task-flow.css?v=20261025-trello-view" rel="stylesheet" />
</body>
</html>
