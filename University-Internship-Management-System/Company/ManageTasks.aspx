<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ManageTasks.aspx.cs" Inherits="University_Internship_Management_System.Company.ManageTasks" %>
<%@ Register Src="~/Shared/CompanySidebar.ascx" TagPrefix="just" TagName="RoleSidebar" %>


<!DOCTYPE html>
<html lang="en">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Manage Tasks | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />

        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/company/ManageTasks.css?v=20261022-task-board" rel="stylesheet" />
        <link href="../Assets/shared/workflow-ui.css?v=20261025-final-cards" rel="stylesheet" />
<script src="../Js_project/shared-role-navigation.js" defer></script>
<link rel="stylesheet" href="../Assets/shared/role-sidebar.css" />
<link rel="stylesheet" href="../Assets/shared/just-theme.css" />

<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
<link href="../Assets/company/company-hero.css?v=20261024-blue" rel="stylesheet" />
</head>
    <body class="coordinator-banner uims-tasks-theme" data-demo-role="company">
        <form id="form1" runat="server">
<just:RoleSidebar ID="SharedRoleNavigation" runat="server" SidebarId="sidebar" />
            <div class="app">

                <!-- ============ SIDEBAR (Company Supervisor) ============ -->



                <div class="main">
                    <!-- ============ TOPBAR ============ -->
                    <header class="topbar">
                        <button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false">
                            <i class="bi bi-list" aria-hidden="true">
                            </i>
                        </button>
                        <div class="topbar-actions ms-auto">
                            <div class="user-chip">
                                <span class="avatar">C</span>
                                <span class="name">Company Portal<span class="role">Company workspace</span>
                                </span>
                            </div>
                        </div>
                    </header>

                    <!-- ============ CONTENT ============ -->
                    <div class="content">
                        <p class="preview-note">Loading live task records from Firebase…</p>
                        <div id="pageNotice" class="page-notice" role="status" aria-live="polite" hidden>
                        </div>
                        <section class="tasks-premium-hero company-reference-hero"><div><span class="tasks-role-badge">JUST / COMPANY SUPERVISOR WORKSPACE</span><h1>Manage Tasks & Exams</h1><p>Create tasks, assign students, monitor progress and release results.</p></div><div class="tasks-hero-metrics"><span><strong id="heroTaskCount">—</strong> active tasks</span><span><strong id="heroExamCount">—</strong> exam tasks</span></div></section>
                            <button type="button" class="btn custom-btn-primary" data-bs-toggle="modal" data-bs-target="#createTaskModal">
                                <i class="bi bi-plus-lg me-2" aria-hidden="true">
                                </i>Create New Task
                            </button>

                        <!-- STATS GRID -->
                        <div class="stats-grid tasks-premium-stats"><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 6h16M4 12h16M4 18h16"/></svg></span><div class="stat-details"><h3>—</h3><span>Total Tasks</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M12 3v9l6 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg></span><div class="stat-details"><h3>—</h3><span>New / Pending</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 12h14m-5-5 5 5-5 5"/></svg></span><div class="stat-details"><h3>—</h3><span>In Progress</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M5 3h14v18H5V3Zm4 5h6m-6 5h6"/></svg></span><div class="stat-details"><h3>—</h3><span>Awaiting Review</span></div></article><article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="m5 12 4 4L19 6"/></svg></span><div class="stat-details"><h3>—</h3><span>Complete</span></div></article></div>

                        <div class="toolbar task-filter-toolbar" id="taskBoardToolbar" aria-label="Task filters and view">
                            <label class="task-filter-field" for="taskSearch">Task Name<select id="taskSearch" aria-label="Task Name"><option value="">All Tasks</option></select></label>
                            <label class="task-filter-field" for="statusFilter">Task Status<select id="statusFilter" aria-label="Task Status">
                                <option value="all">All Statuses</option><option value="assigned">New / Pending</option><option value="inprogress">In Progress</option><option value="submitted">Review</option><option value="completed">Complete</option><option value="revision">Revision Requested</option>
                            </select></label>
                            <div class="btn-group task-view-toggle" role="group" aria-label="Task view"><button type="button" class="btn btn-primary" id="boardModeButton" aria-pressed="true">Board</button><button type="button" class="btn btn-outline-primary" id="listModeButton" aria-pressed="false">List</button></div>
                        </div>
                        <span id="taskCount" class="visually-hidden" aria-live="polite">Loading tasks…</span>
                        <section id="taskBoard" class="task-board" aria-label="Trainee task board" aria-live="polite"></section>
                        <div class="task-list" id="taskList">

<!-- Empty State -->
                            <div id="noTasksMsg" class="no-tasks animate-fade-up template-style-9">
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

            <!-- ============ CREATE TASK MODAL ============ -->
            <div class="modal fade" id="createTaskModal" tabindex="-1" aria-labelledby="createTaskHeading" aria-hidden="true" data-bs-backdrop="static">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
                    <div class="modal-content custom-modal">
                        <div class="modal-header pb-0 border-0 pt-4 px-4">
                            <div><span class="task-compose-eyebrow">COMPANY WORKSPACE / NEW ASSIGNMENT</span><h5 id="createTaskHeading" class="modal-title fw-bold template-style-10">Create a task</h5><p class="task-compose-intro">Set clear expectations. Each selected student receives an independent task.</p></div>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body p-4">
                            <div id="createTaskError" class="alert alert-danger" role="alert" hidden>
                            </div>
                            <div class="task-compose-grid"><section class="task-compose-section"><h6>01 <span>Task details</span></h6>
                            <div class="mb-3">
                                <label for="newTaskTitle" class="form-label custom-label">Task title <span aria-hidden="true">*</span></label>
                                <input type="text" class="form-control custom-input" id="newTaskTitle" required maxlength="150" placeholder="e.g. RESTful API Integration">
                            </div>
                            <div class="mb-4">
                                <label for="newTaskDescription" class="form-label custom-label">Description <span aria-hidden="true">*</span></label>
                                <textarea class="form-control custom-input" rows="3" id="newTaskDescription" required maxlength="3000" placeholder="Describe requirements clearly...">
                                </textarea>
                            </div>
                            <div class="mb-3"><label for="newTaskCriteria" class="form-label custom-label">Instructions &amp; assessment criteria <span aria-hidden="true">*</span></label><textarea class="form-control custom-input" id="newTaskCriteria" rows="4" required maxlength="500" placeholder="Explain the steps, deliverables and how the work will be assessed."></textarea><small>Clear instructions for the student &middot; up to 500 characters.</small></div>
                            <div class="row mb-3">
                                <div class="col-md-4">
                                    <label for="newTaskStart">Start date &amp; time *</label><input id="newTaskStart" type="datetime-local" required class="form-control" /></div><div class="col-md-4"><label for="newTaskDeadline" class="form-label custom-label">Deadline date &amp; time <span aria-hidden="true">*</span></label>
                                    <input type="datetime-local" id="newTaskDeadline" required class="form-control custom-input"><small class="task-timezone"></small>
                                </div>
                                <div class="col-md-4"><label class="form-label custom-label" for="newTaskMaximum">Maximum grade <span aria-hidden="true">*</span></label><input type="number" id="newTaskMaximum" class="form-control custom-input" min="1" max="1000" step="1" value="10" required /><small>Whole number from 1 to 1000.</small></div>
                            </div></section>
                            <section class="task-compose-section task-compose-students"><h6>02 <span>Assign students</span></h6>
                                    <label for="studentSearch" class="form-label custom-label">Eligible students <span aria-hidden="true">*</span></label>
                                    <select id="newTaskStudent" class="form-control custom-input" hidden>
                                        <option value="">Loading active trainee placements…</option>
                                    </select>
                                    <div id="studentPicker" class="student-picker">
                                        <input id="studentSearch" type="search" class="form-control" placeholder="Search students by name" aria-label="Search eligible students" />
                                        <div class="d-flex gap-2 mt-2"><button type="button" id="selectAllStudents" class="btn btn-light btn-sm">Select all</button><button type="button" id="clearStudents" class="btn btn-light btn-sm">Clear</button><span id="selectedStudentCount" role="status">0 selected</span></div>
                                        <div id="studentChips" class="student-chips mt-2"></div>
                                        <div id="studentOptions" class="student-picker-list"></div>
                                        <span id="studentSelectionError" class="field-error" role="alert"></span>
                                    </div>
                                    <p class="task-compose-help">Select one, several or all students. Submissions, grades and board positions stay separate.</p>
                            </section>
                            <section class="task-compose-section"><h6>03 <span>Resources</span> <small>Optional</small></h6><label for="createTaskResources" class="form-label custom-label">Supporting files</label><div class="task-compose-upload"><i class="bi bi-cloud-arrow-up" aria-hidden="true"></i><input id="createTaskResources" type="file" multiple class="form-control" accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg" /><small>PDF, Office documents, text or images &middot; up to 10 MiB each.</small></div></section>
                            <section class="task-compose-section"><h6>04 <span>Examination</span> <small>Optional</small></h6><label class="task-compose-exam"><input type="checkbox" id="configureTaskExam" /><span><strong>Add an examination</strong><small>Build MCQ, True / False and image questions inside this popup after creating the assignments.</small></span></label></section></div>
                            <div id="taskExamBuilder" class="task-builder" hidden></div>
                        </div>
                        <div class="modal-footer border-0 pb-4 px-4">
                            <button type="button" class="btn btn-light custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn custom-btn-primary" id="publishTaskBtn">Create task</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ REVIEW MODAL ============ -->
            <div class="modal" id="reviewModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable company-task-dialog">
                    <div class="modal-content custom-modal">
                        <div class="modal-header pb-0 border-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold template-style-10">Review & Grade Submission</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>

                        <div class="modal-body p-4">
                            <div id="modalErrorContainer" class="alert alert-danger custom-alert template-style-11">
                            </div>
                            <section data-company-panel="details"><div id="taskFullDetails"></div></section>
                            <section data-company-panel="exam"><h6>Examination review</h6><div id="companyTaskExams" aria-live="polite"></div></section>
                            <section data-company-panel="history"><h6>Submission and instruction history</h6><div id="companyTaskHistory"></div></section>
                            <section data-company-panel="submission">
                            <div class="submission-details-box mb-4">
                                <strong class="d-block mb-1 text-dark">Student Description:</strong>
                                <p class="mb-3 text-muted">Submission details load from Firebase when a task is opened.</p>
                                <strong class="d-block mb-1 text-dark">Submitted Link:</strong>
                                <span class="text-muted">Submission link and attachment details load from Firebase.</span>
                            </div>
                            </section>
                            <section data-company-panel="files">
                            <div class="mb-4">
                                <strong class="d-block mb-2 text-dark">Task files and submission attachments</strong>
                                <div id="companyTaskAttachments" class="small mb-3" aria-live="polite">Loading authorized files…</div>
                                <label for="companyTaskFile" class="form-label custom-label">Attach a task resource</label>
                                <input type="file" id="companyTaskFile" class="form-control custom-input" accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg" />
                                <small class="text-muted mt-2 d-block">PDF, DOCX, XLSX, PPTX, TXT, PNG or JPEG &middot; maximum 10 MiB.</small>
                                <button type="button" class="btn btn-light mt-2" id="uploadTaskFileBtn">Upload to Firebase Storage</button>
                            </div>
                            </section>
                            <section data-company-panel="grading">
                            <div class="mb-4">
                                <label for="gradeInput" class="form-label custom-label">Task score</label>
                                <p class="text-muted small">The score range follows the assigned task.</p>
                                <input type="number" id="gradeInput" class="form-control custom-input" min="0" max="100" step="1" required placeholder="e.g. 95">
                            </div>
                            <div class="mb-2">
                                <label for="feedbackInput" class="form-label custom-label">Supervisor Feedback / Notes (optional)</label>
                                <textarea id="feedbackInput" class="form-control custom-input" rows="3" placeholder="Write feedback or request modifications...">
                                </textarea>
                            </div>
                            </section>
                        </div>
                        <div class="modal-footer border-0 pb-4 px-4 d-flex justify-content-between">
                            <button type="button" class="btn custom-btn-danger" id="requestRevisionBtn">Request Revision</button>
                            <button type="button" class="btn custom-btn-success" id="approveTaskBtn">Approve & Grade</button>
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
        <script src="../Js_project/Company/TaskExams.js?v=20261025-flow"></script>
        <script src="../Js_project/Company/ManageTasks.js?v=20261025-task-details">
        </script>
        <script src="../Js_project/Company/TaskComposer.js?v=20261025-flow"></script>
    <link href="../Assets/task-flow.css?v=20261025-trello-view" rel="stylesheet" />
</body>
</html>
