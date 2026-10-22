<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Profile.aspx.cs" Inherits="University_Internship_Management_System.Student.Profile" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <title>My Profile | JUST Internship</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />

    <link runat="server"
        href="~/Assets/shared/colors.css" rel="stylesheet" />

    

    <link runat="server"
        href="~/Assets/student/profile.css?v=20261021-original-ui" rel="stylesheet" />
</head>

<body class="app-page role-shell" data-demo-role="student">
    <form id="form1" runat="server" aria-describedby="profileAvailability" enctype="multipart/form-data">
        <div class="app">
            <aside class="sidebar" id="StudentSidebar" aria-label="Student navigation" data-role-sidebar="true"><button class="sidebar-close" type="button" aria-label="Close navigation menu"><svg class="bi bi-x nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 6 12 12M6 18 18 6"/></svg></button>
                <a href="StudentDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Student pages">
<li><a href="StudentDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ExploreOpportunities.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Explore Opportunities</span></a></li>
<li><a href="MyApplications.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Applications</span></a></li>
<li><a href="ApplicationForm.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Application Form</span></a></li>
<li><a href="Profile.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M5 21v-3a7 7 0 0 1 14 0v3"/></svg><span>My Profile</span></a></li>

<li><a href="MyTasks.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Tasks</span></a></li>

<li><a href="MyProgressEvaluation.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Progress &amp; Evaluation</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav" role="list"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li></ul>

                <div class="side-promo">
                    <svg class="bi bi-mortarboard-fill nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m2 8 10-5 10 5-10 5L2 8Zm4 3v6c3 3 9 3 12 0v-6m4-3v9"/></svg>
                    <p>Build your future<br />with real experience.</p>
                    <a href="ExploreOpportunities.aspx">Explore opportunities &rarr;</a>
                </div>
            </aside>
            <button type="button" class="sidebar-backdrop" aria-label="Close navigation menu" tabindex="-1"></button>
            <div class="main">
                <header class="topbar">
                    <button type="button" class="menu-btn" aria-label="Open navigation menu" aria-expanded="false" aria-controls="StudentSidebar"><svg class="bi bi-list nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M3 6h18M3 12h18M3 18h18"/></svg></button>
                    <div class="search">
                        <svg class="bi bi-search nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/></svg>
                        <label class="nav-sr-only" for="navigationSearch">Search workspace</label><input id="navigationSearch" type="search" placeholder="Search for internships, companies, or skills..." disabled="disabled" aria-describedby="navigationAvailability" />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" disabled="disabled" aria-label="Notifications unavailable" title="Notifications are not connected yet"><svg class="bi bi-bell nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M4 17h16l-2-4V9a6 6 0 0 0-12 0v4l-2 4Zm6 4h4M12 1v2"/></svg></button>
                        <details class="dropdown" id="accountMenu">
                            <summary class="user-chip" aria-label="Student account menu">
                                <span class="avatar"><span id="lblInitials" aria-hidden="true"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg></span></span>
                                <span class="name"><span id="lblUserName">Student account</span><span class="role">Student</span></span>
                                <svg class="bi bi-chevron-down small nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 9 6 6 6-6"/></svg>
                            </summary>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Profile.aspx" aria-current="page"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg>My Profile</a></li>
                                <li><a class="dropdown-item" role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-key nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="7" cy="8" r="4"/><path d="m10 11 10 10m-4-4 3-3m-6 0 3-3"/></svg>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button id="btnLogout" type="button" class="dropdown-item" disabled="disabled" title="Sign-in is not connected yet"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                <span id="navigationAvailability" class="nav-sr-only">Workspace search and notifications require a connected account.</span></header>
                <main class="app-content">
                <div class="profile-page">

                    <header class="profile-heading">
                        <div>
                            <p class="profile-eyebrow">STUDENT ACCOUNT</p>
                            <h1>My Profile</h1>
                            <p class="profile-description">
                                View your academic information and manage
                                your personal details.
                            </p>
                        </div>

                        <span class="profile-role">Student</span>
                    </header>

                    <div class="profile-layout">

                        <aside class="profile-summary"
                            aria-label="Profile summary">

                            <div class="profile-avatar" aria-hidden="true">
                                ST
                            </div>

                            <h2>Student Profile</h2>

                            <p class="profile-summary-text">
                                Your academic details will appear here
                                once your account is connected.
                            </p>

                            <div class="profile-summary-divider"></div>

                            <span class="profile-summary-label">
                                Internship Management System
                            </span>

                            <div class="profile-note">
                                Academic information is managed by
                                the university internship coordinator.
                            </div>
                        </aside>

                        <div class="profile-sections">

                            <section class="profile-card"
                                aria-labelledby="academicTitle">

                                <div class="profile-section-heading">
                                    <div>
                                        <h2 id="academicTitle">
                                            Academic Information
                                        </h2>
                                        <p>Academic records are managed by your coordinator. You can edit your phone number and city below.</p>
                                    </div>
                                </div>

                                <dl class="profile-academic-grid">
                                    <div class="profile-detail">
                                        <dt>Full Name</dt>
                                        <dd>
                                            <span id="litFullName">Not connected</span>
                                        </dd>
                                    </div>

                                    <div class="profile-detail">
                                        <dt>Student ID</dt>
                                        <dd>
                                            <span id="litStudentId">Not connected</span>
                                        </dd>
                                    </div>

                                    <div class="profile-detail">
                                        <dt>Major</dt>
                                        <dd>
                                            <span id="litMajor">Not connected</span>
                                        </dd>
                                    </div>

                                    <div class="profile-detail">
                                        <dt>Required Training Hours</dt>
                                        <dd>
                                            <span id="litRequiredHours">Not connected</span>
                                        </dd>
                                    </div>

                                    <div class="profile-detail profile-detail-wide">
                                        <dt>University Email</dt>
                                        <dd>
                                            <span id="litUniversityEmail">Not connected</span>
                                        </dd>
                                    </div>
                                </dl>
                            <div class="profile-form-grid profile-contact-fields">

                                    <div class="profile-field">
                                        <label for="txtPhone">Phone Number</label>

                                        <input id="txtPhone" name="phone" type="tel" maxlength="25" autocomplete="tel" placeholder="Enter your phone number" aria-describedby="phoneHelp" title="Enter 7 to 25 characters using numbers, spaces, +, parentheses, dots or hyphens." pattern="[+0-9() .\-]{7,25}" />

                                        <span id="phoneHelp" class="profile-help">
                                            Include your country code
                                            when needed.
                                        </span>
                                    </div>

                                    <div class="profile-field">
                                        <label for="txtCity">City</label>

                                        <input id="txtCity" name="city" type="text" maxlength="80" autocomplete="address-level2" placeholder="Enter your city" />
                                    </div>

                                </div>
                            
                            </section>

                            

                            <section class="profile-card"
                                aria-labelledby="linksTitle">

                                <div class="profile-section-heading">
                                    <div>
                                        <h2 id="linksTitle">
                                            CV &amp; Portfolio
                                        </h2>
                                        <p>
                                            Add your CV file and a link to your portfolio.
                                        </p>
                                    </div>
                                </div>

                                <div class="profile-form-grid">

                                    <div class="profile-field profile-field-wide">
                                        <label for="cvFile">CV File</label>

                                        <input id="cvFile" name="cvFile" type="file" accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document" aria-describedby="cvHelp" />

                                        <span id="cvHelp" class="profile-help">Choose a PDF or Word document. File selection is available, but uploading and saving are not connected yet.</span>
                                    </div>

                                    <div class="profile-field profile-field-wide">
                                        <label for="txtPortfolioUrl">Portfolio Link</label>

                                        <input id="txtPortfolioUrl" name="portfolioUrl" type="url" maxlength="2000" title="Enter a complete link starting with https://" pattern="https://.+" aria-describedby="portfolioHelp" placeholder="https://example.com/my-portfolio" /><span id="portfolioHelp" class="profile-help">Use a complete HTTPS link.</span>
                                    </div>

                                </div>
                            </section>

                            <div id="pnlMessage" class="profile-message" role="note"><p id="profileAvailability">Profile data is not connected yet. You can try the fields and select a CV file, but changes and files cannot be saved or uploaded yet. Your academic details will appear once your account is connected.</p></div>

                            <footer class="profile-actions">
                                <p>
                                    Academic details can only be
                                    updated by your coordinator.
                                </p>

                                <button id="btnSave" type="button" class="profile-save" disabled aria-describedby="profileAvailability" enctype="multipart/form-data">Save Changes</button>
                            </footer>

                        </div>
                    </div>
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
<script src="../Js_project/Student/profile.js?v=20261021-original-ui"></script>
</body>
</html>