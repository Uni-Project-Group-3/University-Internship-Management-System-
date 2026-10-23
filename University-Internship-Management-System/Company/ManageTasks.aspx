<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ManageTasks.aspx.cs" Inherits="University_Internship_Management_System.Company.ManageTasks" %>

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
        <link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
</head>
    <body data-demo-role="company">
        <form id="form1" runat="server">
            <div class="app">

                <!-- ============ SIDEBAR (Company Supervisor) ============ -->
                <aside class="sidebar" id="sidebar" aria-label="Company navigation">
                    <button type="button" class="sidebar-close" aria-label="Close navigation">
                        <i class="bi bi-x-lg" aria-hidden="true">
                        </i>
                    </button>
                    <a href="CompanyDashboard.aspx" class="brand">
                        <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48" />
                        <div class="brand-text">
                            <strong>JUST</strong>
                            <span>Internships</span>
                        </div>
                    </a>

                    <ul class="side-nav">
                        <li>
                            <a href="CompanyDashboard.aspx">
                                <i class="bi bi-house-door" aria-hidden="true">
                                </i>
                                <span>Dashboard</span>
                            </a>
                        </li>
                        <li>
                            <a href="ManageOpportunities.aspx">
                                <i class="bi bi-briefcase" aria-hidden="true">
                                </i>
                                <span>Manage Opportunities</span>
                            </a>
                        </li>
                        <li>
                            <a href="ApplicationsReview.aspx">
                                <i class="bi bi-inbox" aria-hidden="true">
                                </i>
                                <span>Applications Review</span>
                            </a>
                        </li>
                        <li>
                            <a href="MyTrainees.aspx">
                                <i class="bi bi-people" aria-hidden="true">
                                </i>
                                <span>My Trainees</span>
                            </a>
                        </li>
                        <li>
                            <a href="ManageTasks.aspx" class="active" aria-current="page">
                                <i class="bi bi-list-check" aria-hidden="true">
                                </i>
                                <span>Manage &amp; Review Tasks</span>
                            </a>
                        </li>
                        <li><a href="ManageExams.aspx"><i class="bi bi-patch-question" aria-hidden="true"></i><span>Task Exams</span></a></li>
                        <li><a href="WeeklyReports.aspx"><i class="bi bi-file-earmark-word" aria-hidden="true"></i><span>Weekly Reports</span></a></li>
                        <li>
                            <a href="ReportsFinalEvaluation.aspx">
                                <i class="bi bi-clipboard-check" aria-hidden="true">
                                </i>
                                <span>Reports &amp; Final Evaluation</span>
                            </a>
                        </li>
                    </ul>

                    <div class="side-divider">
                    </div>

                    <ul class="side-nav">
                        <li>
                            <a href="CompanySupervisorAccount.aspx">
                                <i class="bi bi-gear" aria-hidden="true">
                                </i>Profile</a>
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
                        <i class="bi bi-building-add" aria-hidden="true">
                        </i>
                        <p>Have a new role?<br />Reach JUST students today.</p>
                        <a href="ManageOpportunities.aspx">Post opportunity &rarr;</a>
                    </div>
                </aside>
                <div class="sidebar-backdrop" aria-hidden="true">
                </div>

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
                        <div class="page-header d-flex justify-content-between align-items-center animate-fade-up template-style-1">
                            <div>
                                <h1 class="greeting">Manage & Review Tasks</h1>
                                <p class="subtitle mb-0">Create tasks, specify deadlines, review student submissions, and provide grades.</p>
                            </div>
                            <button type="button" class="btn custom-btn-primary" data-bs-toggle="modal" data-bs-target="#createTaskModal">
                                <i class="bi bi-plus-lg me-2" aria-hidden="true">
                                </i>Create New Task
                            </button>
                        </div>

                        <!-- STATS GRID -->
                        <div class="stats-grid">
                            <div class="stat-card animate-fade-up template-style-2">
                                <div class="stat-icon primary">
                                    <i class="bi bi-list-task" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>8</h3>
                                    <span>Total Assigned</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-3">
                                <div class="stat-icon primary-soft">
                                    <i class="bi bi-eye" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>3</h3>
                                    <span>Awaiting Review</span>
                                </div>
                            </div>
                            <div class="stat-card border-danger-left animate-fade-up template-style-4">
                                <div class="stat-icon danger">
                                    <i class="bi bi-exclamation-circle" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3 class="text-danger">2</h3>
                                    <span class="text-danger fw-semibold">Revision Requested</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-5">
                                <div class="stat-icon success">
                                    <i class="bi bi-check-circle" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>3</h3>
                                    <span>Completed</span>
                                </div>
                            </div>
                        </div>

                        <!-- TOOLBAR (Search & Filter) -->
                        <div class="toolbar animate-fade-up template-style-6">
                            <div class="search-box">
                                <i class="bi bi-search" aria-hidden="true">
                                </i>
                                <input type="text" id="taskSearch" aria-label="Search tasks" placeholder="Search tasks by title..." />
                            </div>
                            <div class="filter-box">
                                <select id="statusFilter" aria-label="Filter by task status" class="form-select">
                                    <option value="all">All Statuses</option>
                                    <option value="submitted">Awaiting Review</option>
                                    <option value="revision">Not complete</option>
                                    <option value="completed">Completed</option>
                                    <option value="assigned">New</option>
                                    <option value="inprogress">In Progress</option>
                                </select>
                            </div>
                        </div>

                        <!-- TASKS LIST -->
                        <div class="d-flex justify-content-between align-items-center mb-3"><h4 class="m-0 fw-bold fs-5">Tasks</h4><span id="taskCount" class="badge badge-primary-soft fs-6">Loading…</span></div>
                        <div class="task-board-toolbar" id="taskBoardToolbar">
                            <label class="board-filter-field" for="deadlineFilter">Deadline<select id="deadlineFilter" class="form-select"><option value="all">Any deadline</option><option value="overdue">Overdue</option><option value="week">Due in 7 days</option></select></label>
                            <label class="board-filter-field" for="batchFilter">Assignment batch<select id="batchFilter" class="form-select"><option value="all">All assignment batches</option></select></label>
                            <label class="board-filter-field" for="boardPlacementSelect">Trainee placement<select id="boardPlacementSelect" class="form-select" aria-label="Select trainee task board"></select></label>
                            <div class="btn-group" role="group" aria-label="Task view">
                                <button type="button" class="btn btn-primary" id="boardModeButton" aria-pressed="true">Board</button>
                                <button type="button" class="btn btn-outline-primary" id="listModeButton" aria-pressed="false">List</button>
                            </div>
                            <span class="task-board-help">Drag cards to organize work. Workflow status changes only through task actions.</span>
                        </div>
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
                            <div class="mb-3"><label for="newTaskCriteria" class="form-label custom-label">Instructions &amp; assessment criteria <span aria-hidden="true">*</span></label><textarea class="form-control custom-input" id="newTaskCriteria" rows="4" required maxlength="500" placeholder="Explain the steps, deliverables and how the work will be assessed."></textarea><small>Clear instructions for the student · up to 500 characters.</small></div>
                            <div class="row mb-3">
                                <div class="col-md-6">
                                    <label for="newTaskDeadline" class="form-label custom-label">Deadline date &amp; time <span aria-hidden="true">*</span></label>
                                    <input type="datetime-local" id="newTaskDeadline" required class="form-control custom-input"><small class="task-timezone"></small>
                                </div>
                                <div class="col-md-6"><label class="form-label custom-label" for="newTaskMaximum">Maximum grade <span aria-hidden="true">*</span></label><input type="number" id="newTaskMaximum" class="form-control custom-input" min="1" max="1000" step="1" value="10" required /><small>Whole number from 1 to 1000.</small></div>
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
                            <section class="task-compose-section"><h6>03 <span>Resources</span> <small>Optional</small></h6><label for="createTaskResources" class="form-label custom-label">Supporting files</label><div class="task-compose-upload"><i class="bi bi-cloud-arrow-up" aria-hidden="true"></i><input id="createTaskResources" type="file" multiple class="form-control" accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg" /><small>PDF, Office documents, text or images · up to 10 MiB each.</small></div></section>
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
                        <nav class="company-task-tabs" role="tablist" aria-label="Review task sections">
                            <button type="button" role="tab" data-company-tab="details" aria-selected="true">Details</button>
                            <button type="button" role="tab" data-company-tab="submission" aria-selected="false">Submission</button>
                            <button type="button" role="tab" data-company-tab="exam" aria-selected="false">Exam results</button>
                            <button type="button" role="tab" data-company-tab="files" aria-selected="false">Files</button>
                            <button type="button" role="tab" data-company-tab="grading" aria-selected="false">Task grading</button>
                            <button type="button" role="tab" data-company-tab="history" aria-selected="false">History</button>
                        </nav>
                        <div class="modal-body p-4">
                            <div id="modalErrorContainer" class="alert alert-danger custom-alert template-style-11">
                            </div>
                            <section data-company-panel="details"><div id="taskFullDetails"></div></section>
                            <section data-company-panel="exam" hidden><h6>Examination review</h6><div id="companyTaskExams" aria-live="polite"></div></section>
                            <section data-company-panel="history" hidden><h6>Submission and instruction history</h6><div id="companyTaskHistory"></div></section>
                            <section data-company-panel="submission" hidden>
                            <div class="submission-details-box mb-4">
                                <strong class="d-block mb-1 text-dark">Student Description:</strong>
                                <p class="mb-3 text-muted">Submission details load from Firebase when a task is opened.</p>
                                <strong class="d-block mb-1 text-dark">Submitted Link:</strong>
                                <span class="text-muted">Submission link and attachment details load from Firebase.</span>
                            </div>
                            </section>
                            <section data-company-panel="files" hidden>
                            <div class="mb-4">
                                <strong class="d-block mb-2 text-dark">Task files and submission attachments</strong>
                                <div id="companyTaskAttachments" class="small mb-3" aria-live="polite">Loading authorized files…</div>
                                <label for="companyTaskFile" class="form-label custom-label">Attach a task resource</label>
                                <input type="file" id="companyTaskFile" class="form-control custom-input" accept=".pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg" />
                                <small class="text-muted mt-2 d-block">PDF, DOCX, XLSX, PPTX, TXT, PNG or JPEG · maximum 10 MiB.</small>
                                <button type="button" class="btn btn-light mt-2" id="uploadTaskFileBtn">Upload to Firebase Storage</button>
                            </div>
                            </section>
                            <section data-company-panel="grading" hidden>
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
        <script src="../Js_project/firebase-client.js?v=20261023-task-connection"></script>
        <script src="../Js_project/task-dialogs.js?v=20261023-task-connection"></script>
        <script src="../Js_project/Company/TaskExams.js?v=20261023-task-connection"></script>
        <script src="../Js_project/Company/ManageTasks.js?v=20261023-task-connection">
        </script>
        <script src="../Js_project/Company/TaskComposer.js?v=20261023-publish-fix"></script>
    </body>
</html>
