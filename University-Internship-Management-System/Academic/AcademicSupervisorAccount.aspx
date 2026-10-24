<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="AcademicSupervisorAccount.aspx.cs" Inherits="University_Internship_Management_System.Academic.AcademicSupervisorAccount" %>
<%@ Register Src="~/Shared/AcademicSidebar.ascx" TagPrefix="shared" TagName="AcademicSidebar" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Academic Supervisor Account | JUST Internships</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    
    <link runat="server" href="~/Assets/academic/supervisor-account.css?v=20261024-academic-hero" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/account-form-design.css?v=20261024-hide-firebase-badge" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261023" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261024-refine" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="app-page supervisor-account-page role-shell" data-demo-role="academic">
    <form id="form1" runat="server">
        <div class="app">
            <shared:AcademicSidebar ID="RoleNavigation" runat="server" SidebarId="sidebar" />
            <div class="main">
                <header class="topbar">
                    <button type="button" class="menu-btn" aria-label="Open navigation menu" aria-expanded="false" aria-controls="sidebar"><svg class="bi bi-list nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M3 6h18M3 12h18M3 18h18"/></svg></button>
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
                                <li><button id="btnLogout" type="button" class="dropdown-item"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                <span id="navigationAvailability" class="nav-sr-only">Notifications require a connected account.</span></header>
                <main class="app-content">
                <div class="account-shell">
                    <nav class="account-breadcrumb" aria-label="Breadcrumb"><a href="AcademicDashboard.aspx">Academic workspace</a><span aria-hidden="true">/</span><span aria-current="page">My Account</span></nav>
                    <header class="account-heading"><div><p class="account-eyebrow">YOUR SUPERVISION JOURNEY</p><h1>Academic Supervisor Account</h1><p>View your academic details and manage your contact information.</p></div><span class="account-role">Academic Supervisor</span></header>
                    <asp:Literal ID="litStatus" runat="server" Visible="false" />
                    <div class="account-layout">
                        <aside class="account-summary" aria-labelledby="supervisorSummaryTitle"><div class="account-avatar" aria-hidden="true"><svg viewBox="0 0 48 48"><circle cx="24" cy="17" r="7"/><path d="M10 39v-3c0-8 6-12 14-12s14 4 14 12v3"/></svg></div><span class="account-small-label">ACADEMIC SUPERVISOR</span><h2 id="supervisorSummaryTitle"><asp:Literal ID="litSummaryName" runat="server" /></h2><p><asp:Literal ID="litSummaryDetails" runat="server" /></p></aside>
                        <div class="account-sections">
                            <section class="account-card" aria-labelledby="academicAccountTitle"><div class="account-section-heading"><div><h2 id="academicAccountTitle">Academic Information</h2><p>Official account details are managed by the university.</p></div><span class="account-view-only">View only</span></div><dl class="account-info"><div><dt>Full Name</dt><dd><asp:Literal ID="litFullName" runat="server" /></dd></div><div><dt>College ID</dt><dd><asp:Literal ID="litCollegeId" runat="server" /></dd></div><div><dt>Department</dt><dd><asp:Literal ID="litDepartment" runat="server" /></dd></div><div><dt>Account Role</dt><dd>Academic Supervisor <span class="account-inline-note">Firebase profile</span></dd></div><div class="account-field-wide"><dt>University Email</dt><dd><asp:Literal ID="litEmail" runat="server" /></dd></div></dl></section>
                            <section class="account-card" aria-labelledby="contactInformationTitle"><div class="account-section-heading"><div><h2 id="contactInformationTitle">Contact Information</h2><p>Help students and coordinators know how to reach you.</p></div></div><div class="account-form-grid"><div class="account-field"><label for="supervisorPhone">Phone Number</label><input id="supervisorPhone" runat="server" ClientIDMode="Static" name="phone" type="text" inputmode="tel" maxlength="25" autocomplete="tel" title="Enter 7 to 25 characters using numbers, spaces, +, parentheses, dots or hyphens." pattern="[+0-9\(\) .\-]{7,25}" placeholder="Enter your phone number" aria-describedby="supervisorPhoneHelp" /><span id="supervisorPhoneHelp" class="account-help">Include your country code when needed.</span></div><div class="account-field"><label for="supervisorOffice">Office Location</label><input id="supervisorOffice" runat="server" ClientIDMode="Static" name="officeLocation" type="text" maxlength="120" placeholder="Building and room number" /></div><div class="account-field account-field-wide"><label for="supervisorOfficeHours">Office Hours</label><input id="supervisorOfficeHours" runat="server" ClientIDMode="Static" name="officeHours" type="text" maxlength="200" placeholder="Add your availability for student meetings" aria-describedby="officeHoursHelp" /><span id="officeHoursHelp" class="account-help">Up to 200 characters. This is an optional contact detail.</span></div></div></section>
                            <footer class="account-actions"><asp:Button ID="saveAcademicProfile" runat="server" ClientIDMode="Static" Text="Save Changes" OnClick="SaveAcademicProfile_Click" CausesValidation="true" /></footer>
                        </div>
                    </div>
                    <footer class="account-footer"><span>UIMS &middot; JUST Internships</span><span>Academic workspace</span></footer>
                </div>
            </main>
            </div>
        </div>
    </form>
    <script src="../Js_project/Demo/demo-store.js?v=20261023-hide-unavailable-notice"></script>
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
                !(backdrop instanceof HTMLElement) || !(main instanceof HTMLElement) ||
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
