<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="ManageCompanies.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.ManageCompanies" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Manage Companies | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />

    <link runat="server" href="~/Assets/coordinator/manage-companies.css?v=20261024-actions" rel="stylesheet" />

<link href="../Assets/shared/workspace-identity.css?v=20261024-coordinator-banner-2" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner dashboard-direct app-page companies-page management-page" data-page="manage-companies">
    <form id="form1" runat="server" novalidate="novalidate"><asp:HiddenField ID="companyCsrf" runat="server" ClientIDMode="Static" />
        <div class="app-layout">
                        <aside class="dash-sidebar workspace-canonical-sidebar" id="dashboardSidebar" aria-label="Coordinator navigation"><a class="brand workspace-nav-brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Internship Coordinator</p><nav class="workspace-nav-primary" aria-label="Internship Coordinator pages"><a class="workspace-nav-link" href="CoordinatorDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="../Coordinator/UniversityRecords.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4h16v16H4V4Zm4 0v16M4 9h16M4 14h16"/></svg><span>University Records</span></a><a class="workspace-nav-link" href="ManageStudents.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Students</span></a><a class="workspace-nav-link active" href="ManageCompanies.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Companies</span></a><a class="workspace-nav-link" href="Manage_Academic_Supervisor.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Academic Supervisors</span></a><a class="workspace-nav-link" href="Assign_Supervisors.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Assign Academic Supervisors</span></a><a class="workspace-nav-link" href="MonitorPlacementsProgress.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>Placements &amp; Progress</span></a><a class="workspace-nav-link" href="ContactMessages.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Contact Messages</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CoordinatorAccountProfile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <button type="button" class="dash-sidebar-backdrop" data-sidebar-close="" aria-label="Close navigation" tabindex="-1" hidden>
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
                                <span class="dash-avatar">CO</span>
                                <span class="dash-user-name">Coordinator<span class="dash-role">Internship Coordinator</span>
                            </span>
                            <span aria-hidden="true">&#8964;</span>
                        </summary>
                        <div class="dash-profile-menu">
                            <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="9"/>
                                    <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                </svg>Help &amp; contact</a>
                                <a data-uims-logout="true" href="<%= ResolveUrl("~/Auth/Login.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="8" r="3"/>
                                        <path d="M5 21v-3a7 7 0 0 1 14 0v3"/>
                                    </svg>Log out</a>
                                </div>
                            </details>
                        </div>
                    </header>
            <main class="app-content companies-main">

                <section class="companies-heading management-hero" aria-labelledby="pageTitle"><div><p class="companies-eyebrow">PARTNERS &amp; PEOPLE</p><h1 id="pageTitle">Manage Companies</h1><p>Manage partner companies and supervisor accounts.</p></div><button type="button" class="companies-button" data-open="addCompany"><span aria-hidden="true">+</span> Add Company</button></section>
                <div id="companyPageNotice" class="management-feedback" role="status" aria-live="polite" hidden></div>
                <section class="companies-stats" aria-label="Company summary">
                    <article><div class="companies-stat-top"><span>Total Companies</span><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3"/></svg></div><strong>&mdash;</strong><small>All university partners</small></article>
                    <article><div class="companies-stat-top"><span>Active Companies</span><svg class="companies-icon-success" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></svg></div><strong>&mdash;</strong><small>Open for collaboration</small></article>
                    <article><div class="companies-stat-top"><span>Inactive Companies</span><svg class="companies-icon-warning" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M9 8v8m6-8v8"/></svg></div><strong>&mdash;</strong><small>Company status only</small></article>
                    <article><div class="companies-stat-top"><span>Company Supervisors</span><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="8" r="3"/><path d="M3 21v-4a6 6 0 0 1 12 0v4m1-16a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5"/></svg></div><strong>&mdash;</strong><small>Linked supervisor accounts</small></article>
                </section>
                <section class="companies-panel" aria-labelledby="directoryTitle">
                    <div class="companies-panel-heading"><div><h2 id="directoryTitle">Company directory</h2><p>Your partner network, organized in one place.</p></div></div>
                    <div class="companies-toolbar"><label class="companies-search"><span>Search directory</span><input type="search" id="companySearch" placeholder="Search companies, supervisors, or email..." /></label><label><span>Company status</span><select id="companyStatus"><option value="">All companies</option><option value="active">Active companies</option><option value="inactive">Deactivated companies</option></select></label><label><span>Supervisor account</span><select id="accountStatus"><option value="">All accounts</option><option value="active">Active accounts</option><option value="disabled">Deactivated accounts</option><option value="password">Password change required</option></select></label><label><span>Sort by</span><select id="companySort"><option>Name: A to Z</option><option>Name: Z to A</option><option>Industry / Sector</option></select></label><button type="button" id="resetCompanyFilters" class="companies-text-button">Reset</button></div>
                    <div class="companies-table-wrap" role="region" aria-label="Company directory table" tabindex="0"><table><thead><tr><th scope="col">Company / Industry</th><th scope="col">Company email</th><th scope="col">Company status</th><th scope="col">Supervisor</th><th scope="col">Supervisor email</th><th scope="col">Account status</th><th scope="col">Actions</th></tr></thead><tbody id="companyRows"></tbody></table></div><div class="companies-empty"><div class="companies-empty-icon"><svg viewBox="0 0 48 48" aria-hidden="true"><path d="M12 41V9h21v32M33 20h7v21M6 41h38M19 16h7m-7 7h7m-7 7h7m-6 11v-5h6v5"/></svg></div><h3 id="companyEmptyTitle">Your next partnership starts here</h3><p id="companyEmptyMessage">Company profiles and their linked supervisors will appear here once the database is connected.</p><button type="button" class="companies-button companies-button--secondary" data-open="addCompany">Explore the company form <span aria-hidden="true">&#8594;</span></button></div>
                    <div class="companies-pagination"><span aria-live="polite" id="companyPageSummary">Loading records...</span><div><label for="companyPageSize">Rows per page</label><select id="companyPageSize"><option selected>15</option><option>25</option><option>50</option></select><button type="button" aria-label="Previous page" id="companyPrevious" disabled>&larr;</button><nav id="companyPageNumbers" aria-label="Company directory pages"></nav><span id="companyPageNumber" class="management-sr-only">1 / 1</span><button type="button" aria-label="Next page" id="companyNext" disabled>&rarr;</button></div></div>
                </section>

                <footer class="companies-footer"><span>&copy; 2026 Jordan University of Science and Technology</span><span>University Internship Management System</span></footer>
            </main></div>
        </div>
        <dialog id="companyEditor" class="companies-modal companies-modal--editor" aria-labelledby="editorTitle" aria-describedby="editorNotice">
            <header class="companies-modal-header"><div><p class="companies-eyebrow">PARTNER MANAGEMENT</p><h2 id="editorTitle">Add company &amp; supervisor</h2><p>Build a new university partnership.</p></div><button type="button" class="companies-close" data-close="" aria-label="Close company form">&times;</button></header>
            <div class="companies-modal-body"><p class="companies-inline-note" id="editorNotice">Confirm &amp; Send creates the supervisor account and emails a password setup link to the supervisor email entered below.</p>
                <section class="companies-form-section"><h3><span>01</span> Company information</h3><div class="companies-form-grid">
                    <label class="companies-wide">Company name <span class="companies-required">*</span><input id="companyName" type="text" maxlength="160" autocomplete="organization" required data-company-field="" /></label>
                    <label>Industry / Sector<input id="industry" type="text" maxlength="100" data-company-field="" /></label><label>Official email<input id="officialEmail" type="email" maxlength="254" autocomplete="email" data-company-field="" /></label>
                    <label>Phone<input id="companyPhone" type="tel" inputmode="numeric" pattern="[0-9]*" title="Use digits only (0-9)" maxlength="40" autocomplete="tel" data-company-field="" /></label><label>Website <span class="companies-optional">Optional</span><input id="companyWebsite" type="url" placeholder="https://" maxlength="300" data-company-field="" /></label>
                    <label class="companies-wide">Address<input id="companyAddress" type="text" maxlength="300" autocomplete="street-address" data-company-field="" /></label><label class="companies-wide">Description <span class="companies-optional">Optional</span><textarea id="companyDescription" rows="2" maxlength="1000" data-company-field=""></textarea></label>
                </div></section>
                <section class="companies-form-section"><h3><span>02</span> Company supervisor</h3><div class="companies-form-grid">
                    <label class="companies-wide">Full name <span class="companies-required">*</span><input id="supervisorName" type="text" maxlength="160" autocomplete="name" required data-company-field="" /></label><label>Email address <span class="companies-required">*</span><input id="supervisorEmail" type="email" maxlength="254" autocomplete="email" required data-company-field="" /></label><label>Phone number<input id="supervisorPhone" type="tel" inputmode="numeric" pattern="[0-9]*" title="Use digits only (0-9)" maxlength="40" autocomplete="tel" data-company-field="" /></label>
                </div><div class="companies-provisioning"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 5v2"/></svg><div><strong>Secure account provisioning</strong><p>The supervisor chooses a password through the emailed Firebase link, then signs in directly to their workspace.</p></div></div></section>
                <p class="companies-validation" id="companyValidation" aria-live="polite"></p>
            </div>
            <footer class="companies-modal-footer"><span>Password setup links are sent to the supervisor’s own email.</span><div><button type="button" class="companies-button companies-button--secondary" data-close="">Cancel</button><button type="button" class="companies-button" id="saveCompany">Confirm &amp; Send</button></div></footer>
        </dialog>
        <dialog id="accountPreview" class="companies-modal companies-modal--account" aria-labelledby="accountTitle">
            <header class="companies-modal-header"><div><p class="companies-eyebrow">SUPERVISOR ACCOUNT</p><h2 id="accountTitle">Account actions</h2><p>Manage the selected Firebase supervisor account.</p></div><button type="button" class="companies-close" data-close="" aria-label="Close account actions">&times;</button></header>
            <div class="companies-modal-body"><div class="companies-account-target"><span aria-hidden="true">&#9675;</span><div><strong>No supervisor selected</strong><p>A verified supervisor name and email will appear here.</p></div><span class="companies-badge">Preview</span></div>
                <div class="companies-action-list" role="group" aria-label="Supervisor actions"><button type="button" data-account-action="sendSetup"><span><strong>Send password setup link</strong><small>Email this supervisor a link to choose a password</small></span></button>
                    <button type="button" data-account-action="details"><span><strong>View Details</strong><small>Company link, contact information, and account status</small></span><span aria-hidden="true">&#8599;</span></button>
                    <button type="button" data-account-action="edit"><span><strong>Edit</strong><small>Update company and supervisor information</small></span><span aria-hidden="true">&#8599;</span></button>

                </div>
            </div><footer class="companies-modal-footer"><span>Changes affect only the selected supervisor.</span><button type="button" class="companies-button companies-button--secondary" data-close="">Close</button></footer>
        </dialog>
        <dialog id="accountConfirmation" class="companies-modal companies-modal--confirmation" aria-labelledby="confirmationTitle" aria-describedby="confirmationDescription">
            <header class="companies-modal-header"><div><p class="companies-eyebrow">CONFIRM ACTION</p><h2 id="confirmationTitle">Account action</h2></div><button type="button" class="companies-close" data-close="" aria-label="Close confirmation">&times;</button></header>
            <div class="companies-modal-body"><div class="companies-account-target"><div><strong>No supervisor selected</strong><p>Name and email will be verified before this action.</p></div></div><p id="confirmationDescription" class="companies-confirmation-copy"></p></div>
            <footer class="companies-modal-footer"><button type="button" class="companies-button companies-button--secondary" id="backToActions">Close</button><button type="button" class="companies-button" id="confirmAccountAction" disabled>Confirm</button></footer>
        </dialog>
    </form>
<script src="<%= ResolveUrl("~/Js_project/Coordinator/manage-companies.js?v=20261024-company-email") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js?v=20261024-link-cleanup") %>"></script>
</body>
</html>
