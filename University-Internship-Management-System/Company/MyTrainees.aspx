<%@ Page Language="C#" AutoEventWireup="true" Async="true" CodeBehind="MyTrainees.aspx.cs" Inherits="University_Internship_Management_System.Company.MyTrainees" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Trainees | JUST Internships</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />

    <link runat="server" href="~/Assets/company/my-trainees.css?v=20261023-trainees-hero" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261024-polish" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-polish" defer></script>
</head>
<body class="app-page trainees-page role-shell" data-demo-role="company">
    <form id="form1" runat="server">
        <div class="app">
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar" aria-label="Company Supervisor navigation" data-role-sidebar="true"><a class="brand workspace-nav-brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Company Supervisor</p><nav class="workspace-nav-primary" aria-label="Company Supervisor pages"><a class="workspace-nav-link" href="CompanyDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ManageOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Opportunities</span></a><a class="workspace-nav-link" href="ApplicationsReview.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Applications Review</span></a><a class="workspace-nav-link active" href="MyTrainees.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Trainees</span></a><a class="workspace-nav-link" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Manage Tasks</span></a><a class="workspace-nav-link" href="ManageExams.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Task Exams</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Reports</span></a><a class="workspace-nav-link" href="ReportsFinalEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Final Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CompanySupervisorAccount.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <button type="button" class="sidebar-backdrop" aria-label="Close navigation menu" tabindex="-1"></button>
            <div class="main">
                <header class="topbar">
                    <button type="button" class="menu-btn" aria-label="Open navigation menu" aria-expanded="false" aria-controls="sidebar"><svg class="bi bi-list nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M3 6h18M3 12h18M3 18h18"/></svg></button>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" disabled="disabled" aria-label="Notifications unavailable" title="Notifications are not connected yet"><svg class="bi bi-bell nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M4 17h16l-2-4V9a6 6 0 0 0-12 0v4l-2 4Zm6 4h4M12 1v2"/></svg></button>
                        <details class="dropdown" id="accountMenu">
                            <summary class="user-chip" aria-label="Company Supervisor account menu">
                                <span class="avatar"><span id="lblInitials" aria-hidden="true"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg></span></span>
                                <span class="name"><span id="lblUserName">Company account</span><span class="role">Company Supervisor</span></span>
                                <svg class="bi bi-chevron-down small nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 9 6 6 6-6"/></svg>
                            </summary>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CompanySupervisorAccount.aspx" class="dropdown-item" role="link"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button id="btnLogout" type="button" class="dropdown-item"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                <span id="navigationAvailability" class="nav-sr-only">Workspace search and notifications require a connected account.</span></header>
                <main class="app-content">
                <div class="trainees-content">
                    <nav class="trainees-breadcrumb" aria-label="Breadcrumb"><a href="CompanyDashboard.aspx">Company workspace</a><span aria-hidden="true">/</span><span aria-current="page">My Trainees</span></nav>
                    <header class="trainees-heading">
                        <div><p class="trainees-eyebrow"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 7h1m4 0h1m-6 4h1m4 0h1M10 21v-6h4v6"/></svg>COMPANY SUPERVISOR · TRAINING TEAM</p><h1>My Trainees</h1><p>Follow your trainees, track their progress, and support them through every stage of their internship.</p></div>
                        <a class="trainees-hero-action" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V2h6v2m-6 6 2 2 4-4m-6 8h6"/></svg>Manage tasks</a>
                    </header>
                    <div class="trainees-notice" id="traineesAvailability" role="note"><span class="trainees-notice-icon" aria-hidden="true">i</span><div><strong>Loading your company trainees…</strong><p>Loading confirmed placements from Firebase.</p></div><span class="trainees-preview-tag">Firebase</span></div>
                    <section class="trainees-stats" aria-label="Trainee summary">
                        <article class="trainees-stat"><div><span>Total trainees</span><span class="trainees-stat-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><circle cx="9" cy="8" r="3"/><path d="M3 20v-3a6 6 0 0 1 12 0v3m1-15a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5v1"/></svg></span></div><strong aria-label="Unavailable">&#8212;</strong><p>Across your company&#8217;s internships</p></article>
                        <article class="trainees-stat"><div><span>Currently training</span><span class="trainees-stat-icon trainees-stat-icon--active" aria-hidden="true"><svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="8"/><path d="m10 8 6 4-6 4V8Z"/></svg></span></div><strong aria-label="Unavailable">&#8212;</strong><p>Active training placements</p></article>
                        <article class="trainees-stat"><div><span>Completed training</span><span class="trainees-stat-icon trainees-stat-icon--complete" aria-hidden="true"><svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="8"/><path d="m8 12 3 3 5-6"/></svg></span></div><strong aria-label="Unavailable">&#8212;</strong><p>Finished training placements</p></article>
                        <article class="trainees-stat"><div><span>Assigned tasks</span><span class="trainees-stat-icon trainees-stat-icon--pending" aria-hidden="true"><svg viewBox="0 0 24 24"><rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V2h6v2M9 10h6m-6 4h4m-4 4h3"/></svg></span></div><strong aria-label="Unavailable">&#8212;</strong><p>Tasks saved for your trainees</p></article>
                    </section>
                    <section class="trainees-card" aria-labelledby="traineesListTitle">
                        <div class="trainees-card-heading"><div><h2 id="traineesListTitle">Your trainees</h2><p>Students accepted into internships published by your company.</p></div><span class="trainees-data-badge"><span aria-hidden="true"></span>Loading records</span></div>
                        <fieldset class="trainees-filters" disabled="disabled" aria-describedby="traineesAvailability"><legend class="trainees-sr-only">Search and filter trainees, available after data connection</legend>
                            <div class="trainees-search"><label for="traineeSearch">Search trainees</label><div><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="10" cy="10" r="6"/><path d="m15 15 5 5"/></svg><input id="traineeSearch" type="search" placeholder="Search by name or student ID" /></div></div>
                            <div class="trainees-filter"><label for="internshipFilter">Internship</label><select id="internshipFilter"><option>All internships</option></select></div>
                            <div class="trainees-filter"><label for="statusFilter">Training status</label><select id="statusFilter"><option>All statuses</option><option>Currently training</option><option>Completed training</option></select></div>
                        </fieldset>
                        <div class="trainees-table-wrap"><table class="trainees-table"><caption class="trainees-sr-only">Company trainees. No records loaded; company data is not connected.</caption><thead><tr><th scope="col">Trainee</th><th scope="col">Internship</th><th scope="col">Training period</th><th scope="col">Progress</th><th scope="col">Status</th><th scope="col">Actions</th></tr></thead><tbody><tr><td colspan="6"><div class="trainees-empty"><div class="trainees-empty-icon" aria-hidden="true"><svg viewBox="0 0 48 48"><rect x="8" y="10" width="32" height="30" rx="5"/><path d="M17 10V7h14v3M16 31c0-4 3-6 8-6s8 2 8 6"/><circle cx="24" cy="19" r="4"/></svg></div><h3>Your trainee list will appear here</h3><p>Once company records are connected, you can view accepted students, follow their training progress, and access their evaluations.</p><span class="trainees-empty-note">No trainee records have been loaded.</span></div></td></tr></tbody></table></div>
                        <footer class="trainees-list-footer"><span>Records unavailable</span><div class="trainees-pagination" aria-label="Trainee list pagination"><button type="button" disabled="disabled" aria-label="Previous page">&#8592;</button><span aria-label="Page unavailable">&#8212;</span><button type="button" disabled="disabled" aria-label="Next page">&#8594;</button></div></footer>
                    </section>
                    <dialog id="companyTraineeDialog" class="trainees-details" aria-label="Trainee details"><header><h2>Trainee details</h2><button type="button" data-close aria-label="Close trainee details">&times;</button></header>
                        <div class="trainees-details-body"><p class="trainees-details-note">Select a trainee to view authorized Firebase records.</p><dl class="trainees-info"><div><dt>Full name</dt><dd>Not connected</dd></div><div><dt>Student ID</dt><dd>Not connected</dd></div><div><dt>University / Major</dt><dd>Not connected</dd></div><div><dt>Internship</dt><dd>Not connected</dd></div><div><dt>Company supervisor</dt><dd>Not connected</dd></div><div><dt>Training period</dt><dd>Not connected</dd></div></dl><div class="trainees-details-actions"><div><h3>Training &amp; evaluation</h3><p id="traineeProgressSummary"></p><label>Training date<input id="approvedHoursDate" type="date" required /></label><label>Explicitly approved hours<input id="approvedHoursValue" type="number" min="0.25" max="24" step="0.25" required /></label><p>Hours are approved separately from task completion. </p><button type="button" id="saveApprovedHours">Record approved hours</button></div><div><button type="button" id="viewTraineeProgress">View training progress</button><button type="button" id="openTraineeEvaluation">Evaluate trainee</button></div></div></div>
                    </dialog>
                    <footer class="trainees-footer"><span>UIMS &middot; JUST Internships</span><span>Company workspace</span></footer>
                </div>
            </main>
            </div>
        </div>
    </form>

<script>
        (() => {
            "use strict";
            const sidebar = document.querySelector(".sidebar");
            const menuButton = document.querySelector(".menu-btn");
            const backdrop = document.querySelector(".sidebar-backdrop");
            const main = document.querySelector(".main");
            const userMenu = document.getElementById("accountMenu");
            const closeButton = sidebar && sidebar.querySelector(".sidebar-close");
            const accountToggle = userMenu && userMenu.querySelector("summary");
            if (!(sidebar instanceof HTMLElement) || !(menuButton instanceof HTMLButtonElement) ||
                !(backdrop instanceof HTMLButtonElement) || !(main instanceof HTMLElement) ||
                !(userMenu instanceof HTMLElement) || !(closeButton instanceof HTMLButtonElement) ||
                !(accountToggle instanceof HTMLElement)) return;
            const ui = { sidebar, menuButton, backdrop, main, userMenu, closeButton, accountToggle };
            const mobile = window.matchMedia("(max-width: 991.98px)");
            /** @param {boolean} open */
            function setSidebar(open) {
                open = Boolean(open && mobile.matches);
                document.body.classList.toggle("sidebar-open", open);
                if (open) ui.main.setAttribute("inert", "");
                else ui.main.removeAttribute("inert");
                ui.menuButton.setAttribute("aria-expanded", String(open));
                ui.menuButton.setAttribute("aria-label", open ? "Close navigation menu" : "Open navigation menu");
                if (open) {
                    ui.userMenu.removeAttribute("open");
                    ui.closeButton.focus();
                }
            }
            function closeSidebar() { setSidebar(false); ui.menuButton.focus(); }
            ui.menuButton.addEventListener("click", () => setSidebar(!document.body.classList.contains("sidebar-open")));
            ui.closeButton.addEventListener("click", closeSidebar);
            ui.backdrop.addEventListener("click", closeSidebar);
            document.addEventListener("keydown", event => {
                if (event.key === "Escape") {
                    if (document.body.classList.contains("sidebar-open")) closeSidebar();
                    if (ui.userMenu.hasAttribute("open")) {
                        ui.userMenu.removeAttribute("open");
                        ui.accountToggle.focus();
                    }
                }
                if (event.key === "Tab" && document.body.classList.contains("sidebar-open")) {
                    const items = ui.sidebar.querySelectorAll('a[href], button:not([disabled])');
                    const first = items.item(0);
                    const last = items.item(items.length - 1);
                    if (event.shiftKey && document.activeElement === first && last instanceof HTMLElement) {
                        event.preventDefault(); last.focus();
                    }
                    if (!event.shiftKey && document.activeElement === last && first instanceof HTMLElement) {
                        event.preventDefault(); first.focus();
                    }
                }
            });
            document.addEventListener("click", event => {
                if (event.target instanceof Node && !ui.userMenu.contains(event.target)) ui.userMenu.removeAttribute("open");
            });
            ui.userMenu.addEventListener("focusout", event => {
                if (!(event.relatedTarget instanceof Node) || !ui.userMenu.contains(event.relatedTarget)) ui.userMenu.removeAttribute("open");
            });
            mobile.addEventListener("change", () => setSidebar(false));
        })();
    </script>
<script src="../Js_project/Company/my-trainees.js?v=20261023-hide-connected-notice"></script>
</body>
</html>
