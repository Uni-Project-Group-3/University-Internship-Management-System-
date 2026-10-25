<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="Manage_Academic_Supervisor.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.Manage_Academic_Supervisor" %>

<!DOCTYPE html>
<html lang="en">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Manage Academic Supervisors | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />

        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/coordinator/Manage_Academic_Supervisor.css?v=20261024-link-cleanup" rel="stylesheet" />
    <link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
    <body class="coordinator-banner management-page" data-page="manage-academic-supervisors">
        <form id="form1" runat="server" novalidate="novalidate"><asp:HiddenField ID="academicCsrf" runat="server" ClientIDMode="Static" />
            <div class="app">

                <!-- ============ SIDEBAR (Coordinator) ============ -->
                <aside class="sidebar workspace-canonical-sidebar" id="sidebar" aria-label="Coordinator navigation"><a class="brand workspace-nav-brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Internship Coordinator</p><nav class="workspace-nav-primary" aria-label="Internship Coordinator pages"><a class="workspace-nav-link" href="CoordinatorDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="../Coordinator/UniversityRecords.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4h16v16H4V4Zm4 0v16M4 9h16M4 14h16"/></svg><span>University Records</span></a><a class="workspace-nav-link" href="ManageStudents.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Students</span></a><a class="workspace-nav-link" href="ManageCompanies.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Companies</span></a><a class="workspace-nav-link active" href="Manage_Academic_Supervisor.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Academic Supervisors</span></a><a class="workspace-nav-link" href="Assign_Supervisors.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Assign Academic Supervisors</span></a><a class="workspace-nav-link" href="MonitorPlacementsProgress.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>Placements &amp; Progress</span></a><a class="workspace-nav-link" href="ContactMessages.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Contact Messages</span></a><a class="workspace-nav-link" href="SystemOptions.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M20 18h0"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/></svg><span>System Options</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CoordinatorAccountProfile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CssClass="text-danger">
                                <i class="bi bi-box-arrow-right" aria-hidden="true">
                                </i>Logout</asp:LinkButton></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
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
                                <span class="name">Coordinator Portal<span class="role">Internship Coordinator</span>
                                </span>
                            </div>
                        </div>
                    </header>

                    <!-- ============ CONTENT ============ -->
                    <div class="content">
                        <div class="page-header management-hero d-flex justify-content-between align-items-center animate-fade-up template-style-1">
                            <div>
                                <p class="management-eyebrow">ACADEMIC TEAM</p><h1 class="greeting">Manage Academic Supervisors</h1>
                                <p class="subtitle mb-0">Create and manage academic supervisor accounts and departments.</p>
                            </div>
                            <button type="button" class="btn custom-btn-primary" id="addAcademicButton">
                                <i class="bi bi-plus-lg me-2" aria-hidden="true">
                                </i>Add New Supervisor
                            </button>
                        </div>


                        <p class="preview-note">Look up an official staff ID before creating an account. No email is sent.</p>
                        <div id="pageNotice" class="page-notice" role="status" aria-live="polite" hidden>
                        </div>
                        <!-- STATS GRID -->
                        <div class="stats-grid">
                            <div class="stat-card animate-fade-up template-style-2">
                                <div class="stat-icon template-style-3">
                                    <i class="bi bi-people" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>0</h3>
                                    <span>Total Supervisors</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-4">
                                <div class="stat-icon template-style-5">
                                    <i class="bi bi-person-check" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>0</h3>
                                    <span>Active Accounts</span>
                                </div>
                            </div>
                            <div class="stat-card animate-fade-up template-style-6">
                                <div class="stat-icon template-style-7">
                                    <i class="bi bi-diagram-2" aria-hidden="true">
                                    </i>
                                </div>
                                <div class="stat-details">
                                    <h3>0</h3>
                                    <span>Departments</span>
                                </div>
                            </div>
                        </div>

                        <!-- SUPERVISORS TABLE -->
                        <div class="panel animate-fade-up template-style-8">
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <h3 class="panel-title m-0">Supervisors Directory</h3>
                                <select id="deptFilter" aria-label="Filter by department" class="form-select w-auto custom-input py-1 px-3"><option value="all">All Departments</option></select>
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
                                    <tbody id="academicRows"></tbody>
                                </table>
                                <!-- Empty Search Message -->
                                <div id="noResultsMsg" class="text-center py-4">No supervisors match your search.</div>
                            </div>
                            <div class="management-pagination"><span id="academicPageSummary" role="status" aria-live="polite"></span><div><label for="academicPageSize">Rows per page</label><select id="academicPageSize"><option selected>15</option><option>25</option><option>50</option></select><button type="button" id="academicPrevious" aria-label="Previous page">&#8592;</button><nav id="academicPageNumbers" aria-label="Directory pages"></nav><button type="button" id="academicNext" aria-label="Next page">&#8594;</button></div></div>
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
                            <p class="text-muted">Find an official university staff record. Name, email and department cannot be entered manually.</p>
<div class="mb-3"><label for="academicStaffId" class="form-label custom-label">Staff ID</label><div class="d-flex gap-2"><input id="academicStaffId" type="text" class="form-control custom-input" maxlength="40" placeholder="For example A1001" /><button type="button" id="findAcademic" class="btn custom-btn-primary">Find supervisor</button></div><p id="academicLookupMessage" role="status" class="mt-2"></p></div>
                            <div id="modalError" class="alert alert-danger" role="alert" hidden>
                            </div>
                            <div class="mb-3">
                                <label for="supName" class="form-label custom-label">Full name</label>
                                <input id="supName" type="text" class="form-control custom-input" readonly />
                            </div>
                            <div class="mb-3">
                                <label for="supEmail" class="form-label custom-label">Email address</label>
                                <input id="supEmail" type="email" class="form-control custom-input" readonly />
                            </div>
                            <div class="mb-3">
                                <label for="supDept" class="form-label custom-label">Department</label>
                                <input id="supDept" type="text" class="form-control custom-input" readonly />
                            </div>
<div class="mb-3"><label for="supUniversity" class="form-label custom-label">University</label><input id="supUniversity" class="form-control custom-input" readonly /></div>
<div class="mb-3"><label for="supCollege" class="form-label custom-label">College</label><input id="supCollege" class="form-control custom-input" readonly /></div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <p>Confirm &amp; Send emails a password setup link to the university email shown above. Use that same email to sign in after setting your password.</p>
                            <button type="button" id="saveSupBtn" class="btn custom-btn-primary" disabled>Confirm &amp; Send</button>
                        </div>
                    </div>
                </div>
            </div>
            <div class="modal" id="deleteModal" tabindex="-1" aria-hidden="true" aria-labelledby="deleteModalTitle">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content custom-modal">
                        <div class="modal-header">
                            <h2 class="modal-title fs-5" id="deleteModalTitle">Academic supervisor account</h2>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close">
                            </button>
                        </div>
                        <div class="modal-body">
                            <p id="statusTarget">
                            </p>
                            <p id="academicActionDescription" class="text-muted"></p><p id="academicActionError" role="alert" class="text-danger"></p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" id="confirmStatusBtn" class="btn custom-btn-primary">Confirm</button>
                        </div>
                    </div>
                </div>
            </div>
<div class="modal" id="academicDetailsModal" tabindex="-1" aria-labelledby="academicDetailsTitle" aria-hidden="true"><div class="modal-dialog modal-dialog-centered modal-dialog-scrollable"><div class="modal-content custom-modal"><div class="modal-header"><h2 id="academicDetailsTitle" class="modal-title fs-5">Supervisor details</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close details"></button></div><div class="modal-body"><p id="academicDetailsText" style="white-space:pre-line"></p></div><div class="modal-footer"><button type="button" class="btn custom-btn-light" data-bs-dismiss="modal">Close</button></div></div></div></div>
        </form>
<script src="../Scripts/bootstrap.bundle.min.js">
        </script>
        <script src="../Js_project/Coordinator/Manage_Academic_Supervisor.js?v=20261024-directory-load">
        </script>
    </body>
</html>
