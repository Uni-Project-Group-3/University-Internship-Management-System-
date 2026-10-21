<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Assign_Supervisors.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.Assign_Supervisors" %>

<!DOCTYPE html>
<html lang="en">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Assign Academic Supervisors | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />

        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/coordinator/Assign_Supervisors.css?v=20261021-original-ui" rel="stylesheet" />
    </head>
    <body data-demo-role="admin">
        <form id="form1" runat="server">
            <div class="app">

                <!-- ============ SIDEBAR (Coordinator) ============ -->
                <aside class="sidebar" id="sidebar" aria-label="Coordinator navigation">
                    <button type="button" class="sidebar-close" aria-label="Close navigation">
                        <i class="bi bi-x-lg" aria-hidden="true">
                        </i>
                    </button>
                    <a href="CoordinatorDashboard.aspx" class="brand">
                        <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48" />
                        <div class="brand-text">
                            <strong>JUST</strong>
                            <span>Internships</span>
                        </div>
                    </a>

                    <ul class="side-nav">
                        <li>
                            <a href="CoordinatorDashboard.aspx">
                                <i class="bi bi-house-door" aria-hidden="true">
                                </i>
                                <span>Dashboard</span>
                            </a>
                        </li>
                        <li>
                            <a href="ManageStudents.aspx">
                                <i class="bi bi-mortarboard" aria-hidden="true">
                                </i>
                                <span>Manage Students</span>
                            </a>
                        </li>
                        <li>
                            <a href="ManageCompanies.aspx">
                                <i class="bi bi-building" aria-hidden="true">
                                </i>
                                <span>Manage Companies</span>
                            </a>
                        </li>
                        <li>
                            <a href="Manage_Academic_Supervisor.aspx">
                                <i class="bi bi-person-badge" aria-hidden="true">
                                </i>
                                <span>Manage Academic Supervisors</span>
                            </a>
                        </li>
                        <li>
                            <a href="Assign_Supervisors.aspx" class="active" aria-current="page">
                                <i class="bi bi-diagram-3" aria-hidden="true">
                                </i>
                                <span>Assign Academic Supervisors</span>
                            </a>
                        </li>
                        <li>
                            <a href="MonitorPlacementsProgress.aspx">
                                <i class="bi bi-activity" aria-hidden="true">
                                </i>
                                <span>Monitor Placements &amp; Progress</span>
                            </a>
                        </li>
                        <li>
                            <a href="FinalCompletionReview.aspx">
                                <i class="bi bi-patch-check" aria-hidden="true">
                                </i>
                                <span>Final Completion Review</span>
                            </a>
                        </li>
                        <li>
                            <a href="ContactMessages.aspx">
                                <i class="bi bi-envelope" aria-hidden="true">
                                </i>
                                <span>Contact Messages</span>
                            </a>
                        </li>
                    </ul>

                    <div class="side-divider">
                    </div>

                    <ul class="side-nav">
                        <li>
                            <a href="CoordinatorAccountProfile.aspx">
                                <i class="bi bi-gear" aria-hidden="true">
                                </i>Coordinator Account</a>
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
                        <i class="bi bi-person-plus" aria-hidden="true">
                        </i>
                        <p>New semester?<br />Add students and assign groups.</p>
                        <a href="ManageStudents.aspx">Add students &rarr;</a>
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
                        <div class="search">
                            <i class="bi bi-search" aria-hidden="true">
                            </i>
                            <input type="text" id="searchInput" aria-label="Search groups or supervisors" placeholder="Search groups or supervisors..." />
                        </div>
                        <div class="topbar-actions ms-auto">

                            <div class="user-chip">
                                <span class="avatar" aria-hidden="true">CO</span>
                                <span class="name">Coordinator Portal<span class="role">Interface preview</span>
                                </span>
                            </div>
                        </div>
                    </header>

                    <!-- ============ CONTENT ============ -->
                    <div class="content">
                        <p class="preview-note">Interface preview · Sample data. Changes last until this page is reloaded; no Firebase updates.</p>
                        <div id="pageNotice" class="page-notice" role="status" aria-live="polite" hidden>
                        </div>
                        <div class="page-header d-flex justify-content-between align-items-center animate-fade-up template-style-1">
                            <div>
                                <h1 class="greeting">Assign Supervisors</h1>
                                <p class="subtitle mb-0">Manage student groups and link them to their academic supervisors.</p>
                            </div>
                            <button type="button" class="btn custom-btn-primary" data-bs-toggle="modal" data-bs-target="#addGroupModal">
                                <i class="bi bi-plus-lg me-2" aria-hidden="true">
                                </i> Create New Group
                            </button>
                        </div>

                        <!-- STATS CARDS -->
                        <div class="stats-grid">
                            <div class="stat-card animate-fade-up template-style-2">
                                <div class="stat-icon template-style-3">
                                    <i class="bi bi-collection" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>12</h3>
                                    <span>Total Groups</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-4">
                                <div class="stat-icon template-style-5">
                                    <i class="bi bi-check-circle" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>10</h3>
                                    <span>Assigned Groups</span>
                                </div>
                            </div>
                            <div class="stat-card border-danger-left animate-fade-up template-style-6">
                                <div class="stat-icon template-style-7">
                                    <i class="bi bi-exclamation-circle" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3 class="text-danger">2</h3>
                                    <span class="text-danger fw-semibold">Action Required (Unassigned)</span>
                                </div>
                            </div>
                        </div>

                        <!-- MAIN TABLE PANEL -->
                        <div class="panel animate-fade-up template-style-8">
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <h3 class="panel-title m-0">Student Groups</h3>
                            </div>
                            <div class="table-responsive">
                                <table class="table custom-table align-middle mb-0" id="groupsTable">
                                    <thead>
                                        <tr>
                                            <th>Group Details</th>
                                            <th>Department</th>
                                            <th>Students</th>
                                            <th>Assigned Supervisor</th>
                                            <th class="text-end">Manage</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <!-- Row 1: Assigned Group -->
                                        <tr class="group-row">
                                            <td>
                                                <div class="fw-bold text-dark group-name">Group A - Fall 2026</div>
                                                <div class="text-muted small">Code: GRP-101</div>
                                            </td>
                                            <td>
                                                <span class="dept-badge">Computer Science</span>
                                            </td>
                                            <td>
                                                <button type="button" class="btn-link-custom" data-bs-toggle="modal" data-bs-target="#viewStudentsModal">
                                                    <i class="bi bi-people" aria-hidden="true">
                                                    </i> 15 Students
                                                </button>
                                            </td>
                                            <td>
                                                <div class="supervisor-chip">
                                                    <div class="sup-avatar">RK</div>
                                                    <div class="sup-info">
                                                        <span class="sup-name fw-bold">Dr. Rami Al-Khatib</span>
                                                        <span class="sup-id text-muted small">ID: SUP-9012</span>
                                                    </div>
                                                </div>
                                            </td>
                                            <td class="text-end">
                                                <div class="action-buttons">
                                                    <button type="button" class="action-btn edit-btn" title="Modify Assignment" data-bs-toggle="modal" data-bs-target="#assignModal">
                                                        <i class="bi bi-pencil-square" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                    <button type="button" class="action-btn delete-btn" title="Delete Group" data-bs-toggle="modal" data-bs-target="#deleteGroupModal">
                                                        <i class="bi bi-trash" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>

                                        <!-- Row 2: Unassigned Group -->
                                        <tr class="group-row highlight-row template-style-9">
                                            <td>
                                                <div class="fw-bold text-dark group-name">Group B - Fall 2026</div>
                                                <div class="text-muted small">Code: GRP-102</div>
                                            </td>
                                            <td>
                                                <span class="dept-badge">Software Engineering</span>
                                            </td>
                                            <td>
                                                <button type="button" class="btn-link-custom" data-bs-toggle="modal" data-bs-target="#viewStudentsModal">
                                                    <i class="bi bi-people" aria-hidden="true">
                                                    </i> 12 Students
                                                </button>
                                            </td>
                                            <td>
                                                <span class="unassigned-badge">
                                                    <i class="bi bi-exclamation-triangle-fill me-1" aria-hidden="true">
                                                    </i> Pending Assignment</span>
                                            </td>
                                            <td class="text-end">
                                                <div class="action-buttons">
                                                    <button type="button" class="btn custom-btn-primary btn-sm px-3 assign-btn text-white border-0" data-bs-toggle="modal" data-bs-target="#assignModal">
                                                        <i class="bi bi-link-45deg me-1" aria-hidden="true">
                                                        </i> Assign</button>
                                                    <button type="button" class="action-btn delete-btn" title="Delete Group" data-bs-toggle="modal" data-bs-target="#deleteGroupModal">
                                                        <i class="bi bi-trash" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                                <div id="noResultsMsg" class="text-center py-5 template-style-10">
                                    <i class="bi bi-search text-muted fs-1 d-block mb-3" aria-hidden="true">
                                    </i>
                                    <h5 class="fw-bold">No groups found</h5>
                                    <p class="text-muted">Try adjusting your search query.</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 1. ADD NEW GROUP MODAL ============ -->
            <div class="modal" id="addGroupModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header border-0 pb-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold fs-5">Create New Student Group</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body p-4">
                            <div id="addGroupError" class="alert alert-danger custom-alert template-style-11">
                            </div>
                            <div class="mb-4">
                                <label class="form-label custom-label">Group Name <span class="text-danger">*</span>
                                </label>
                                <input type="text" id="newGroupName" class="form-control custom-input" placeholder="e.g. Group C - Fall 2026" />
                            </div>
                            <div class="mb-2">
                                <label class="form-label custom-label">Department <span class="text-danger">*</span>
                                </label>
                                <select id="newGroupDept" class="form-select custom-input">
                                    <option value="" disabled selected>Select Department...</option>
                                    <option>Computer Science</option>
                                    <option>Software Engineering</option>
                                    <option>Network Engineering</option>
                                </select>
                            </div>
                        </div>
                        <div class="modal-footer border-0 pb-4 px-4">
                            <button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="saveGroupBtn" class="btn custom-btn-primary">Create Group</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 2. ASSIGN SUPERVISOR MODAL ============ -->
            <div class="modal" id="assignModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header border-0 pb-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold fs-5">Assign Academic Supervisor</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body p-4">
                            <div id="assignError" class="alert alert-danger custom-alert template-style-11">
                            </div>
                            <div class="mb-4">
                                <label class="form-label custom-label text-muted">Selected Group</label>
                                <input type="text" id="selectedGroupName" class="form-control custom-input bg-light" readonly />
                            </div>
                            <div class="mb-2">
                                <label class="form-label custom-label">Select Supervisor <span class="text-danger">*</span>
                                </label>
                                <select id="supervisorSelect" class="form-select custom-input">
                                    <option value="" disabled selected>Choose from available staff...</option>
                                    <option>Dr. Samah Obeid (Software Engineering)</option>
                                    <option>Dr. Rami Al-Khatib (Computer Science)</option>
                                </select>
                            </div>
                        </div>
                        <div class="modal-footer border-0 pb-4 px-4">
                            <button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="confirmAssignBtn" class="btn custom-btn-primary">Save Assignment</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 3. VIEW STUDENTS MODAL ============ -->
            <div class="modal" id="viewStudentsModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header border-0 pb-3 pt-4 px-4 border-bottom">
                            <h5 class="modal-title fw-bold fs-5">Students in Group</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body p-0">
                            <ul id="groupStudents" class="list-group list-group-flush">
                            </ul>
                            <div class="p-4">
                                <label for="groupStudentSelect" class="form-label">Available sample students</label>
                                <select id="groupStudentSelect" class="form-select">
                                    <option value="">Select student</option>
                                    <option value="146003">Omar Hasan · 146003</option>
                                    <option value="146004">Lina Khaled · 146004</option>
                                </select>
                                <p id="studentGroupError" class="text-danger" role="alert">
                                </p>
                            </div>
                        </div>
                        <div class="modal-footer bg-light border-0 p-3">
                            <button type="button" class="btn btn-outline-primary rounded-pill w-100 fw-bold" id="addGroupStudentBtn">
                                <i class="bi bi-person-plus me-2" aria-hidden="true">
                                </i> Add Student to Group</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 4. DELETE GROUP MODAL ============ -->
            <div class="modal" id="deleteGroupModal" tabindex="-1" aria-hidden="true" aria-labelledby="deleteGroupTitle">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-body text-center p-4">
                            <div class="mb-3 text-danger template-style-12">
                                <i class="bi bi-exclamation-circle" aria-hidden="true">
                                </i>
                            </div>
                            <h4 class="fw-bold mb-2" id="deleteGroupTitle">Delete this Group?</h4>
                            <p class="text-muted mb-4">Are you sure you want to delete this group? All assigned students will become unassigned.</p>
                            <div class="d-flex justify-content-center gap-3">
                                <button type="button" class="btn custom-btn-light px-4" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn custom-btn-primary px-4 border-0 template-style-13" id="deleteGroupBtn">Yes, Delete</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

        </form>

        <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="../Scripts/bootstrap.bundle.min.js">
        </script>
        <script src="../Js_project/Coordinator/Assign_Supervisors.js?v=20261021-original-ui">
        </script>
    </body>
</html>
