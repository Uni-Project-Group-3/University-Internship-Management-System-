<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Manage_Academic_Supervisor.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.Manage_Academic_Supervisor" %>

<!DOCTYPE html>
<html lang="en">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Manage Academic Supervisors | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />

        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/coordinator/Manage_Academic_Supervisor.css?v=20261021-original-ui" rel="stylesheet" />
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
                            <a href="Manage_Academic_Supervisor.aspx" class="active" aria-current="page">
                                <i class="bi bi-person-badge" aria-hidden="true">
                                </i>
                                <span>Manage Academic Supervisors</span>
                            </a>
                        </li>
                        <li>
                            <a href="Assign_Supervisors.aspx">
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
                            <input type="text" id="searchInput" aria-label="Search academic supervisors" placeholder="Search by name, ID, or department..." />
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
                                <h1 class="greeting">Academic Supervisors</h1>
                                <p class="subtitle mb-0">Create and manage academic supervisor accounts and departments.</p>
                            </div>
                            <button type="button" class="btn custom-btn-primary" data-bs-toggle="modal" data-bs-target="#addSupervisorModal">
                                <i class="bi bi-plus-lg me-2" aria-hidden="true">
                                </i>Add New Supervisor
                            </button>
                        </div>

                        <!-- STATS GRID -->
                        <div class="stats-grid">
                            <div class="stat-card animate-fade-up template-style-2">
                                <div class="stat-icon template-style-3">
                                    <i class="bi bi-people" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>24</h3>
                                    <span>Total Supervisors</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-4">
                                <div class="stat-icon template-style-5">
                                    <i class="bi bi-person-check" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>22</h3>
                                    <span>Active Accounts</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-6">
                                <div class="stat-icon template-style-7">
                                    <i class="bi bi-diagram-2" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>5</h3>
                                    <span>Departments</span>
                                </div>
                            </div>
                        </div>

                        <!-- SUPERVISORS TABLE -->
                        <div class="panel animate-fade-up template-style-8">
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <h3 class="panel-title m-0">Supervisors Directory</h3>
                                <select id="deptFilter" aria-label="Filter by department" class="form-select w-auto custom-input py-1 px-3">
                                    <option value="all">All Departments</option>
                                    <option value="Computer Science">Computer Science</option>
                                    <option value="Software Engineering">Software Engineering</option>
                                    <option value="Network Engineering">Network Engineering</option>
                                    <option>Artificial Intelligence</option>
                                    <option>Cybersecurity</option>
                                    <option>Computer Information Systems</option>
                                </select>
                            </div>

                            <div class="table-responsive">
                                <table class="table custom-table align-middle" id="supervisorsTable">
                                    <thead>
                                        <tr>
                                            <th>Supervisor Info</th>
                                            <th>Department</th>
                                            <th>Assigned Groups</th>
                                            <th>Status</th>
                                            <th class="text-end">Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <!-- Row 1 -->
                                        <tr data-dept="Computer Science">
                                            <td>
                                                <div class="d-flex align-items-center gap-3">
                                                    <div class="table-avatar">RK</div>
                                                    <div>
                                                        <div class="fw-bold text-dark supervisor-name">Dr. Rami Al-Khatib</div>
                                                        <div class="text-muted small">ID: SUP-9012 • rami.k@just.edu.jo</div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>
                                                <span class="dept-badge">Computer Science</span>
                                            </td>
                                            <td>
                                                <span class="badge bg-light text-dark border">3 Groups</span>
                                            </td>
                                            <td>
                                                <span class="status-badge success">Active</span>
                                            </td>
                                            <td class="text-end">
                                                <div class="action-buttons">
                                                    <button type="button" class="action-btn edit-btn" title="Edit" data-bs-toggle="modal" data-bs-target="#addSupervisorModal">
                                                        <i class="bi bi-pencil-square" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                    <button type="button" class="action-btn delete-btn" title="Deactivate" data-bs-toggle="modal" data-bs-target="#deleteModal">
                                                        <i class="bi bi-person-x" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                        <!-- Row 2 -->
                                        <tr data-dept="Software Engineering">
                                            <td>
                                                <div class="d-flex align-items-center gap-3">
                                                    <div class="table-avatar alt-1">SO</div>
                                                    <div>
                                                        <div class="fw-bold text-dark supervisor-name">Dr. Samah Obeid</div>
                                                        <div class="text-muted small">ID: SUP-8834 • samah.o@just.edu.jo</div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>
                                                <span class="dept-badge">Software Engineering</span>
                                            </td>
                                            <td>
                                                <span class="badge bg-light text-dark border">2 Groups</span>
                                            </td>
                                            <td>
                                                <span class="status-badge success">Active</span>
                                            </td>
                                            <td class="text-end">
                                                <div class="action-buttons">
                                                    <button type="button" class="action-btn edit-btn" title="Edit" data-bs-toggle="modal" data-bs-target="#addSupervisorModal">
                                                        <i class="bi bi-pencil-square" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                    <button type="button" class="action-btn delete-btn" title="Deactivate" data-bs-toggle="modal" data-bs-target="#deleteModal">
                                                        <i class="bi bi-person-x" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                        <!-- Row 3 -->
                                        <tr data-dept="Network Engineering">
                                            <td>
                                                <div class="d-flex align-items-center gap-3">
                                                    <div class="table-avatar alt-2">MA</div>
                                                    <div>
                                                        <div class="fw-bold text-dark supervisor-name">Eng. Mahmoud Ali</div>
                                                        <div class="text-muted small">ID: SUP-7710 • m.ali@just.edu.jo</div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>
                                                <span class="dept-badge">Network Engineering</span>
                                            </td>
                                            <td>
                                                <span class="text-muted small">No Groups Yet</span>
                                            </td>
                                            <td>
                                                <span class="status-badge danger">Inactive</span>
                                            </td>
                                            <td class="text-end">
                                                <div class="action-buttons">
                                                    <button type="button" class="action-btn edit-btn" title="Edit" data-bs-toggle="modal" data-bs-target="#addSupervisorModal">
                                                        <i class="bi bi-pencil-square" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                    <button type="button" class="action-btn text-success" title="Activate">
                                                        <i class="bi bi-person-check" aria-hidden="true">
                                                        </i>
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                                <!-- Empty Search Message -->
                                <div id="noResultsMsg" class="text-center py-4 template-style-9">No supervisors match your search.</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="modal" id="addSupervisorModal" tabindex="-1" aria-hidden="true" aria-labelledby="addSupervisorModalTitle">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header">
                            <h2 class="modal-title fs-5" id="addSupervisorModalTitle">Add Academic Supervisor</h2>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body">
                            <p class="text-muted">Preview profile only. No authentication account will be created.</p>
                            <div id="modalError" class="alert alert-danger" role="alert" hidden>
                            </div>
                            <div class="mb-3">
                                <label for="supName" class="form-label custom-label">Full name</label>
                                <input id="supName" type="text" class="form-control custom-input" required maxlength="100" />
                            </div>
                            <div class="mb-3">
                                <label for="supEmail" class="form-label custom-label">Email address</label>
                                <input id="supEmail" type="email" class="form-control custom-input" required maxlength="254" />
                            </div>
                            <div class="mb-3">
                                <label for="supDept" class="form-label custom-label">Department</label>
                                <select id="supDept" class="form-select custom-input" required>
                                    <option value="">Select department</option>
                                    <option>Computer Science</option>
                                    <option>Software Engineering</option>
                                    <option>Network Engineering</option>
                                    <option>Artificial Intelligence</option>
                                    <option>Cybersecurity</option>
                                    <option>Computer Information Systems</option>
                                </select>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="saveSupBtn" class="btn custom-btn-primary">Save Preview Profile</button>
                        </div>
                    </div>
                </div>
            </div>
            <div class="modal" id="deleteModal" tabindex="-1" aria-hidden="true" aria-labelledby="deleteModalTitle">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header">
                            <h2 class="modal-title fs-5" id="deleteModalTitle">Change Preview Account Status</h2>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body">
                            <p id="statusTarget">
                            </p>
                            <p class="text-muted">This changes the preview only. Firebase Authentication is unaffected.</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="confirmStatusBtn" class="btn custom-btn-primary">Confirm</button>
                        </div>
                    </div>
                </div>
            </div>
        </form>
        <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="../Scripts/bootstrap.bundle.min.js">
        </script>
        <script src="../Js_project/Coordinator/Manage_Academic_Supervisor.js?v=20261021-original-ui">
        </script>
    </body>
</html>
