<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ManageCompanies.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.ManageCompanies" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Manage Companies | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />

    <link runat="server" href="~/Assets/coordinator/manage-companies.css?v=20261021-role-pages-final" rel="stylesheet" />
    
</head>
<body class="dashboard-direct app-page companies-page" data-portal="admin">
    <form id="form1" runat="server">
        <div class="app-layout">
                        <aside class="dash-sidebar" id="dashboardSidebar" aria-label="Coordinator navigation">
                <a href="<%= ResolveUrl("~/Coordinator/CoordinatorDashboard.aspx") %>" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text">
                        <strong>JUST</strong>
                        <span>Internships</span>
                    </span>
                </a>
                <button type="button" class="dash-sidebar-close" data-sidebar-close aria-label="Close navigation">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="m6 6 12 12M6 18 18 6"/>
                    </svg>
                </button>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav" aria-label="Coordinator pages">
<li><a href="CoordinatorDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ManageStudents.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Students</span></a></li>
<li><a href="ManageCompanies.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg><span>Manage Companies</span></a></li>
<li><a href="MonitorPlacementsProgress.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Placements &amp; Progress</span></a></li>
<li><a href="FinalCompletionReview.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Completion Review</span></a></li>
<li><a href="ContactMessages.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Contact Messages</span></a></li>

<li><a href="Manage_Academic_Supervisor.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage Academic Supervisors</span></a></li>

<li><a href="Assign_Supervisors.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Assign Academic Supervisors</span></a></li>

<li><a href="CoordinatorAccountProfile.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Coordinator Account</span></a></li>
</ul>
                    <div class="dash-side-divider">
                    </div>
                    <ul class="dash-side-nav">
                        

</ul>
                </nav>
                <div class="dash-side-promo">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="m2 9 10-5 10 5-10 5-10-5Zm4 3v6c4 3 8 3 12 0v-6m4-3v8"/>
                    </svg>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a href="<%= ResolveUrl("~/Coordinator/ManageStudents.aspx") %>">Add students &rarr;</a>
                </div>
            </aside>
            <button type="button" class="dash-sidebar-backdrop" data-sidebar-close aria-label="Close navigation" tabindex="-1" hidden>
            </button>
            <div class="dash-main">
                <header class="dash-topbar">
                    <button type="button" class="dash-menu-btn" id="dashboardMenuToggle" aria-controls="dashboardSidebar" aria-expanded="false" aria-label="Open navigation">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <path d="M4 6h16M4 12h16M4 18h16"/>
                        </svg>
                    </button>
                    <label class="dash-search">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <circle cx="10" cy="10" r="6"/>
                            <path d="m15 15 6 6"/>
                        </svg>
                        <span class="dash-sr-only">Search this page</span>
                        <input type="search" id="dashboardSearch" data-search-target="companySearch" placeholder="Search for students, companies, or supervisors..." />
                    </label>
                    <div class="dash-topbar-actions">
                        <button type="button" class="dash-icon-btn" disabled aria-label="Notifications are not connected" title="Notifications are not connected">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                <path d="M5 17h14l-2-3V9a5 5 0 0 0-10 0v5l-2 3Zm5 3h4"/>
                            </svg>
                        </button>
                        <details class="dash-profile">
                            <summary class="dash-user-chip">
                                <span class="dash-avatar" id="lblInitials">—</span>
                                <span class="dash-user-name"><span id="lblUserName">Verified account</span><span class="dash-role">Internship Coordinator</span>
                            </span>
                            <span aria-hidden="true">&#8964;</span>
                        </summary>
                        <div class="dash-profile-menu">
                            <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="9"/>
                                    <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                </svg>Help &amp; contact</a>
                                <a href="<%= ResolveUrl("~/Auth/Login.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="8" r="3"/>
                                        <path d="M5 21v-3a7 7 0 0 1 14 0v3"/>
                                    </svg>Sign in</a>
                                </div>
                            </details>
                        </div>
                    </header>
            <main class="app-content companies-main">

                <section class="companies-heading" aria-labelledby="pageTitle"><div><p class="companies-eyebrow">PARTNERS &amp; PEOPLE</p><h1 id="pageTitle">Manage Companies</h1><p>Manage partner companies and supervisor accounts.</p></div><button type="button" class="companies-button" data-open="addCompany"><span aria-hidden="true">+</span> Add Company</button></section>
                <div class="companies-notice" role="status"><span aria-hidden="true">&#9432;</span><div><strong>Design preview</strong><span>Firebase integration comes later. No accounts or company records can be created, updated, or removed here yet.</span></div></div>
                <section class="companies-stats" aria-label="Company summary">
                    <article><div class="companies-stat-top"><span>Total Companies</span><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg></div><strong>&mdash;</strong><small>All university partners</small></article>
                    <article><div class="companies-stat-top"><span>Active Companies</span><svg class="companies-icon-success" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></svg></div><strong>&mdash;</strong><small>Open for collaboration</small></article>
                    <article><div class="companies-stat-top"><span>Inactive Companies</span><svg class="companies-icon-warning" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M9 8v8m6-8v8"/></svg></div><strong>&mdash;</strong><small>Company status only</small></article>
                    <article><div class="companies-stat-top"><span>Company Supervisors</span><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="8" r="3"/><path d="M3 21v-4a6 6 0 0 1 12 0v4m1-16a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5"/></svg></div><strong>&mdash;</strong><small>Linked supervisor accounts</small></article>
                </section>
                <section class="companies-panel" aria-labelledby="directoryTitle">
                    <div class="companies-panel-heading"><div><h2 id="directoryTitle">Company directory</h2><p>Your partner network, organized in one place.</p></div><button type="button" class="companies-text-button" data-open="accountPreview">Preview account actions <span aria-hidden="true">&#8599;</span></button></div>
                    <div class="companies-toolbar"><label class="companies-search"><span>Search directory</span><input type="search" id="companySearch" placeholder="Search companies, supervisors, or email..." /></label><label><span>Company status</span><select id="companyStatus"><option value="">All companies</option><option value="active">Active companies</option><option value="inactive">Inactive companies</option></select></label><label><span>Supervisor account</span><select id="accountStatus"><option value="">All accounts</option><option value="active">Active accounts</option><option value="disabled">Disabled accounts</option><option value="password">Password change required</option></select></label><label><span>Sort by</span><select id="companySort"><option>Name: A to Z</option><option>Name: Z to A</option><option>Industry / Sector</option></select></label><button type="button" id="resetCompanyFilters" class="companies-text-button">Reset</button></div>
                    <div class="companies-table-wrap" role="region" aria-label="Company directory table" tabindex="0"><table><thead><tr><th scope="col">Company / Industry</th><th scope="col">Company email</th><th scope="col">Company status</th><th scope="col">Supervisor</th><th scope="col">Supervisor email</th><th scope="col">Account status</th><th scope="col">Actions</th></tr></thead><tbody></tbody></table></div><div class="companies-empty"><div class="companies-empty-icon"><svg viewBox="0 0 48 48" aria-hidden="true"><path d="M12 41V9h21v32M33 20h7v21M6 41h38M19 16h7m-7 7h7m-7 7h7m-6 11v-5h6v5"/></svg></div><h3 id="companyEmptyTitle">Your next partnership starts here</h3><p id="companyEmptyMessage">Company profiles and their linked supervisors will appear here once the database is connected.</p><button type="button" class="companies-button companies-button--secondary" data-open="addCompany">Explore the company form <span aria-hidden="true">&#8594;</span></button></div>
                    <div class="companies-pagination"><span aria-live="polite">No connected records</span><div><label for="companyPageSize">Rows per page</label><select id="companyPageSize"><option>10</option><option>25</option><option>50</option></select><button type="button" aria-label="Previous page" disabled>&larr;</button><span>&mdash;</span><button type="button" aria-label="Next page" disabled>&rarr;</button></div></div>
                </section>
                <section class="companies-context" aria-label="Account management guidance"><div><span aria-hidden="true">&#9678;</span><p><strong>Company and account status are separate.</strong> Managing one supervisor will not change other supervisors or the company.</p></div><div><span aria-hidden="true">&#9633;</span><p><strong>Academic history stays intact.</strong> Account removal and company removal are separate operations.</p></div></section>
                <footer class="companies-footer"><span>&copy; 2026 Jordan University of Science and Technology</span><span>University Internship Management System</span></footer>
            </main></div>
        </div>
        <dialog id="companyEditor" class="companies-modal companies-modal--editor" aria-labelledby="editorTitle" aria-describedby="editorNotice">
            <header class="companies-modal-header"><div><p class="companies-eyebrow">PARTNER MANAGEMENT</p><h2 id="editorTitle">Add company &amp; supervisor</h2><p>Build a new university partnership.</p></div><button type="button" class="companies-close" data-close aria-label="Close company form">&times;</button></header>
            <div class="companies-modal-body"><p class="companies-inline-note" id="editorNotice">Design preview only. This form does not create or update records.</p>
                <section class="companies-form-section"><h3><span>01</span> Company information</h3><div class="companies-form-grid">
                    <label class="companies-wide">Company name <span class="companies-required">*</span><input id="companyName" type="text" maxlength="160" autocomplete="organization" required data-company-field /></label>
                    <label>Industry / Sector<input id="industry" type="text" maxlength="100" data-company-field /></label><label>Official email<input id="officialEmail" type="email" maxlength="254" autocomplete="email" data-company-field /></label>
                    <label>Phone<input id="companyPhone" type="tel" maxlength="40" autocomplete="tel" data-company-field /></label><label>Website <span class="companies-optional">Optional</span><input id="companyWebsite" type="url" placeholder="https://" maxlength="300" data-company-field /></label>
                    <label class="companies-wide">Address<input id="companyAddress" type="text" maxlength="300" autocomplete="street-address" data-company-field /></label><label class="companies-wide">Description <span class="companies-optional">Optional</span><textarea id="companyDescription" rows="2" maxlength="1000" data-company-field></textarea></label>
                </div></section>
                <section class="companies-form-section"><h3><span>02</span> Company supervisor</h3><div class="companies-form-grid">
                    <label class="companies-wide">Full name <span class="companies-required">*</span><input id="supervisorName" type="text" maxlength="160" autocomplete="name" required data-company-field /></label><label>Email address <span class="companies-required">*</span><input id="supervisorEmail" type="email" maxlength="254" autocomplete="email" required data-company-field /></label><label>Phone number<input id="supervisorPhone" type="tel" maxlength="40" autocomplete="tel" data-company-field /></label>
                </div><div class="companies-provisioning"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 5v2"/></svg><div><strong>Secure account provisioning</strong><p>Temporary credentials will be issued by the server after integration. New supervisors will be active and required to change their password on first login.</p></div></div></section>
                <p class="companies-validation" id="companyValidation" aria-live="polite"></p>
            </div>
            <footer class="companies-modal-footer"><span>Saving is unavailable until Firebase is connected.</span><div><button type="button" class="companies-button companies-button--secondary" data-close>Cancel</button><button type="button" class="companies-button" id="validateCompany">Check form</button><button type="button" class="companies-button" disabled id="saveCompany">Create company</button></div></footer>
        </dialog>
        <dialog id="accountPreview" class="companies-modal companies-modal--account" aria-labelledby="accountTitle">
            <header class="companies-modal-header"><div><p class="companies-eyebrow">SUPERVISOR ACCOUNT</p><h2 id="accountTitle">Account actions</h2><p>Preview the account management workflow.</p></div><button type="button" class="companies-close" data-close aria-label="Close account actions">&times;</button></header>
            <div class="companies-modal-body"><div class="companies-account-target"><span aria-hidden="true">&#9675;</span><div><strong>No supervisor selected</strong><p>A verified supervisor name and email will appear here.</p></div><span class="companies-badge">Preview</span></div>
                <div class="companies-action-list" role="group" aria-label="Preview supervisor actions">
                    <button type="button" data-account-action="details"><span><strong>View Details</strong><small>Company link, contact information, and account status</small></span><span aria-hidden="true">&#8599;</span></button>
                    <button type="button" data-account-action="edit"><span><strong>Edit</strong><small>Update company and supervisor information</small></span><span aria-hidden="true">&#8599;</span></button>
                    <button type="button" data-account-action="reset"><span><strong>Reset Password</strong><small>Issue temporary credentials and require a password change</small></span><span aria-hidden="true">&#8594;</span></button>
                    <button type="button" data-account-action="disable"><span><strong>Disable Account</strong><small>Block access for this supervisor only</small></span><span aria-hidden="true">&#8594;</span></button>
                    <button type="button" data-account-action="enable"><span><strong>Enable Account</strong><small>Restore access to the same account</small></span><span aria-hidden="true">&#8594;</span></button>
                    <button type="button" class="companies-danger-action" data-account-action="delete"><span><strong>Delete Account</strong><small>Remove sign-in access while preserving academic history</small></span><span aria-hidden="true">&#8594;</span></button>
                </div>
            </div><footer class="companies-modal-footer"><span>Preview only. Account operations are not connected.</span><button type="button" class="companies-button companies-button--secondary" data-close>Close</button></footer>
        </dialog>
        <dialog id="accountConfirmation" class="companies-modal companies-modal--confirmation" aria-labelledby="confirmationTitle" aria-describedby="confirmationDescription">
            <header class="companies-modal-header"><div><p class="companies-eyebrow">ACTION PREVIEW</p><h2 id="confirmationTitle">Account action</h2></div><button type="button" class="companies-close" data-close aria-label="Close confirmation">&times;</button></header>
            <div class="companies-modal-body"><div class="companies-account-target"><div><strong>No supervisor selected</strong><p>Name and email will be verified before this action.</p></div></div><p id="confirmationDescription" class="companies-confirmation-copy"></p><p class="companies-inline-note">This is a layout preview. No Firebase operation will run.</p></div>
            <footer class="companies-modal-footer"><button type="button" class="companies-button companies-button--secondary" id="backToActions">Back to actions</button><button type="button" class="companies-button" id="confirmAccountAction" disabled>Confirm</button></footer>
        </dialog>
    </form>
    <script src="<%= ResolveUrl("~/Js_project/Coordinator/manage-companies.js?v=20261021-role-pages-final") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261021-role-pages-final") %>"></script>
<script src="../Js_project/Public/tab-session.js" defer="defer"></script>
</body>
</html>
