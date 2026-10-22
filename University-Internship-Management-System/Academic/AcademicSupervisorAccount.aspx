<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AcademicSupervisorAccount.aspx.cs" Inherits="University_Internship_Management_System.Academic.AcademicSupervisorAccount" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Academic Supervisor Account | JUST Internships</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    
    <link runat="server" href="~/Assets/academic/supervisor-account.css?v=20261021-original-ui" rel="stylesheet" />
</head>
<body class="app-page supervisor-account-page role-shell" data-demo-role="academic">
    <form id="form1" runat="server">
        <div class="app">
            <aside class="sidebar" id="sidebar" aria-label="Academic Supervisor navigation" data-role-sidebar="true"><button class="sidebar-close" type="button" aria-label="Close navigation menu"><svg class="bi bi-x nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 6 12 12M6 18 18 6"/></svg></button>
                <a href="AcademicDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Academic pages">
<li><a href="AcademicDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="StudentProgressMonitor.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg><span>Student Progress Monitor</span></a></li>
<li><a href="FinalAcademicEvaluation.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Academic Evaluation</span></a></li>
<li><a href="AcademicSupervisorAccount.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Account</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav" role="list"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li></ul>

                <div class="side-promo">
                    <svg class="bi bi-journal-check nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><rect x="4" y="2" width="16" height="20" rx="2"/><path d="M8 2v20M2 6h4m-4 6h4m-4 6h4m5-7 2 2 4-5"/></svg>
                    <p>Review company evaluations<br />and support student progress.</p>
                    <a href="FinalAcademicEvaluation.aspx">Start evaluating &rarr;</a>
                </div>
            </aside>
            <button type="button" class="sidebar-backdrop" aria-label="Close navigation menu" tabindex="-1"></button>
            <div class="main">
                <header class="topbar">
                    <button type="button" class="menu-btn" aria-label="Open navigation menu" aria-expanded="false" aria-controls="sidebar"><svg class="bi bi-list nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M3 6h18M3 12h18M3 18h18"/></svg></button>
                    <div class="search">
                        <svg class="bi bi-search nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/></svg>
                        <label class="nav-sr-only" for="navigationSearch">Search workspace</label><input id="navigationSearch" type="search" placeholder="Search for students or companies..." disabled="disabled" aria-describedby="navigationAvailability" />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" disabled="disabled" aria-label="Notifications unavailable" title="Notifications are not connected yet"><svg class="bi bi-bell nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M4 17h16l-2-4V9a6 6 0 0 0-12 0v4l-2 4Zm6 4h4M12 1v2"/></svg></button>
                        <details class="dropdown" id="accountMenu">
                            <summary class="user-chip" aria-label="Academic Supervisor account menu">
                                <span class="avatar"><span id="lblInitials" aria-hidden="true"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg></span></span>
                                <span class="name"><span id="lblUserName">Academic account</span><span class="role">Academic Supervisor</span></span>
                                <svg class="bi bi-chevron-down small nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 9 6 6 6-6"/></svg>
                            </summary>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="AcademicSupervisorAccount.aspx" aria-current="page"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button id="btnLogout" type="button" class="dropdown-item" disabled="disabled" title="Sign-in is not connected yet"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                <span id="navigationAvailability" class="nav-sr-only">Workspace search and notifications require a connected account.</span></header>
                <main class="app-content">
                <div class="account-shell">
                    <nav class="account-breadcrumb" aria-label="Breadcrumb"><a href="AcademicDashboard.aspx">Academic workspace</a><span aria-hidden="true">/</span><span aria-current="page">My Account</span></nav>
                    <header class="account-heading"><div><p class="account-eyebrow">YOUR SUPERVISION JOURNEY</p><h1>Academic Supervisor Account</h1><p>View your academic details and manage your contact information.</p></div><span class="account-role">Academic Supervisor</span></header>
                    <div id="accountAvailability" class="account-notice" role="note"><span aria-hidden="true">i</span><p><strong>Account connection pending.</strong> No supervisor data is loaded. You can try the contact fields, but changes cannot be saved yet.</p></div>
                    <div class="account-layout">
                        <aside class="account-summary" aria-labelledby="supervisorSummaryTitle"><div class="account-avatar" aria-hidden="true"><svg viewBox="0 0 48 48"><circle cx="24" cy="17" r="7"/><path d="M10 39v-3c0-8 6-12 14-12s14 4 14 12v3"/></svg></div><span class="account-small-label">ACADEMIC SUPERVISOR</span><h2 id="supervisorSummaryTitle">Your account</h2><p>Your name and academic details will appear once your account is connected.</p><div class="account-summary-status"><span aria-hidden="true"></span>Not connected</div><div class="account-summary-note">Help students connect their academic learning with practical experience.</div><a class="account-monitor-link" href="StudentProgressMonitor.aspx">Student Progress Monitor <span aria-hidden="true">&#8599;</span></a></aside>
                        <div class="account-sections">
                            <section class="account-card" aria-labelledby="academicAccountTitle"><div class="account-section-heading"><div><h2 id="academicAccountTitle">Academic Information</h2><p>Official account details are managed by the university.</p></div><span class="account-view-only">View only</span></div><dl class="account-info"><div><dt>Full Name</dt><dd>Not connected</dd></div><div><dt>Staff ID</dt><dd>Not connected</dd></div><div><dt>Department</dt><dd>Not connected</dd></div><div><dt>Account Role</dt><dd>Academic Supervisor <span class="account-inline-note">Page role</span></dd></div><div class="account-field-wide"><dt>University Email</dt><dd>Not connected</dd></div></dl></section>
                            <section class="account-card" aria-labelledby="contactInformationTitle"><div class="account-section-heading"><div><h2 id="contactInformationTitle">Contact Information</h2><p>Help students and coordinators know how to reach you.</p></div></div><div class="account-form-grid"><div class="account-field"><label for="supervisorPhone">Phone Number</label><input id="supervisorPhone" name="phone" type="tel" maxlength="25" autocomplete="tel" title="Enter 7 to 25 characters using numbers, spaces, +, parentheses, dots or hyphens." pattern="[+0-9\(\) .\-]{7,25}" placeholder="Enter your phone number" aria-describedby="supervisorPhoneHelp" /><span id="supervisorPhoneHelp" class="account-help">Include your country code when needed.</span></div><div class="account-field"><label for="supervisorOffice">Office Location</label><input id="supervisorOffice" name="officeLocation" type="text" maxlength="120" placeholder="Building and room number" /></div><div class="account-field account-field-wide"><label for="supervisorOfficeHours">Office Hours</label><input id="supervisorOfficeHours" name="officeHours" type="text" maxlength="200" placeholder="Add your availability for student meetings" aria-describedby="officeHoursHelp" /><span id="officeHoursHelp" class="account-help">Up to 200 characters. This is an optional contact detail.</span></div></div></section>
                            <section class="account-card account-access" aria-labelledby="accountAccessTitle"><span class="account-access-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="m12 3 8 4v5c0 5-8 9-8 9s-8-4-8-9V7l8-4Z"/><path d="m8 12 3 3 5-6"/></svg></span><div><h2 id="accountAccessTitle">Account Access</h2><p>Use the sign-in page to access the existing password recovery form.</p><a href="../Auth/Login.aspx">Go to sign in <span aria-hidden="true">&#8594;</span></a></div></section>
                            <footer class="account-actions"><p>Saving requires a verified supervisor account and a connection to the university records.</p><button id="saveAcademicProfile" type="button" aria-describedby="accountAvailability">Save Changes</button></footer>
                        </div>
                    </div>
                    <footer class="account-footer"><span>UIMS &middot; JUST Internships</span><span>Academic workspace</span></footer>
                </div>
            </main>
            </div>
        </div>
    </form>
    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
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
<script src="../Js_project/Academic/academic-supervisor-account.js?v=20261021-original-ui"></script>
</body>
</html>
