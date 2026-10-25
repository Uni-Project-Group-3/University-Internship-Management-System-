<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Profile.aspx.cs" Inherits="University_Internship_Management_System.Student.Profile" %>
<%@ Register Src="~/Shared/StudentSidebar.ascx" TagPrefix="shared" TagName="StudentSidebar" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <title>My Profile | JUST Internship</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />

    <link runat="server"
        href="~/Assets/shared/colors.css" rel="stylesheet" />

    

    <link runat="server"
        href="~/Assets/student/profile.css?v=20261024-student-hero" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/account-form-design.css?v=20261024-hide-firebase-badge" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261023" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>

<body class="coordinator-banner app-page role-shell student-profile-page" data-demo-role="student">
    <form id="form1" runat="server" aria-describedby="profileAvailability" enctype="multipart/form-data">
        <div class="app">
            <shared:StudentSidebar ID="RoleNavigation" runat="server" SidebarId="StudentSidebar" />
            <div class="main">
                <header class="topbar">
                    <button type="button" class="menu-btn" aria-label="Open navigation menu" aria-expanded="false" aria-controls="StudentSidebar"><svg class="bi bi-list nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M3 6h18M3 12h18M3 18h18"/></svg></button>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" disabled="disabled" aria-label="Notifications unavailable" title="Notifications are not connected yet"><svg class="bi bi-bell nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M4 17h16l-2-4V9a6 6 0 0 0-12 0v4l-2 4Zm6 4h4M12 1v2"/></svg></button>
                        <details class="dropdown" id="accountMenu">
                            <summary class="user-chip" aria-label="Student account menu">
                                <span class="avatar"><span id="lblInitials" runat="server" aria-hidden="true">ST</span></span>
                                <span class="name"><span id="lblUserName" runat="server">Student account</span><span class="role">Student</span></span>
                                <svg class="bi bi-chevron-down small nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 9 6 6 6-6"/></svg>
                            </summary>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Profile.aspx" aria-current="page"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg>My Profile</a></li>
                                <li><a class="dropdown-item" role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-key nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="7" cy="8" r="4"/><path d="m10 11 10 10m-4-4 3-3m-6 0 3-3"/></svg>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button id="btnLogout" type="button" class="dropdown-item"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                </header>
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
                                <asp:Literal ID="litSummaryInitials" runat="server" Mode="Encode" />
                            </div>

                            <h2><asp:Literal ID="litSummaryName" runat="server" Mode="Encode" /></h2>

                            <p class="profile-summary-text">
                                <asp:Literal ID="litSummaryDetails" runat="server" Mode="Encode" />
                            </p>

                            <div class="profile-summary-divider"></div>

                            <span class="profile-summary-label">
                                Internship Management System
                            </span>

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
                                            <asp:Literal ID="litFullName" runat="server" Mode="Encode" />
                                        </dd>
                                    </div>

                                    <div class="profile-detail">
                                        <dt>Student ID</dt>
                                        <dd>
                                            <asp:Literal ID="litStudentId" runat="server" Mode="Encode" />
                                        </dd>
                                    </div>

                                    <div class="profile-detail">
                                        <dt>Major</dt>
                                        <dd>
                                            <asp:Literal ID="litMajor" runat="server" Mode="Encode" />
                                        </dd>
                                    </div>

                                    <div class="profile-detail">
                                        <dt>Required Training Hours</dt>
                                        <dd>
                                            <asp:Literal ID="litRequiredHours" runat="server" Mode="Encode" />
                                        </dd>
                                    </div>

                                    <div class="profile-detail profile-detail-wide">
                                        <dt>University Email</dt>
                                        <dd>
                                            <asp:Literal ID="litUniversityEmail" runat="server" Mode="Encode" />
                                        </dd>
                                    </div>
                                </dl>
                            <div class="profile-form-grid profile-contact-fields">

                                    <div class="profile-field">
                                        <label for="txtPhone">Phone Number</label>

                                        <asp:TextBox ID="txtPhone" runat="server" ClientIDMode="Static" TextMode="Phone" MaxLength="25" autocomplete="tel" placeholder="Enter your phone number" aria-describedby="phoneHelp" title="Enter 7 to 25 characters using numbers, spaces, +, parentheses, dots or hyphens." pattern="[+0-9() .\-]{7,25}" />

                                        <span id="phoneHelp" class="profile-help">
                                            Include your country code
                                            when needed.
                                        </span>
                                    </div>

                                    <div class="profile-field">
                                        <label for="txtCity">City</label>

                                        <asp:TextBox ID="txtCity" runat="server" ClientIDMode="Static" MaxLength="80" autocomplete="address-level2" placeholder="Enter your city" />
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
                                        <label for="cvUpload">CV File</label>
                                        <div class="profile-file-picker-row">
                                            <asp:FileUpload ID="cvUpload" runat="server" ClientIDMode="Static" CssClass="profile-file-input" aria-describedby="cvHelp" />
                                            <label class="profile-save profile-file-picker" for="cvUpload">Choose File</label>
                                            <span id="cvSelectedName" class="profile-file-name" aria-live="polite"><asp:Literal ID="litCvFileName" runat="server" Mode="Encode" /></span>
                                        </div>
                                        <a class="profile-cv-download" href="Profile.aspx?download=cv"><svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M12 3v12m-5-5 5 5 5-5M4 16v5h16v-5"/></svg>Download saved CV</a>
                                        <span id="cvHelp" class="profile-help">PDF, DOC, or DOCX, up to 10 MiB. Files are stored privately and downloaded through your authenticated account.</span>
                                    </div>

                                    <div class="profile-field profile-field-wide">
                                        <label for="txtPortfolioUrl">Portfolio Link</label>

                                        <asp:TextBox ID="txtPortfolioUrl" runat="server" ClientIDMode="Static" TextMode="Url" MaxLength="2000" title="Enter a complete link starting with https://" pattern="https://.+" aria-describedby="portfolioHelp" placeholder="https://example.com/my-portfolio" /><span id="portfolioHelp" class="profile-help">Use a complete HTTPS link.</span>
                                    </div>

                                </div>
                            </section>

                            <div id="pnlMessage" runat="server" class="profile-message" role="status" visible="false"><p id="profileAvailability"><asp:Literal ID="litProfileMessage" runat="server" Mode="Encode" /></p></div>

                            <footer class="profile-actions">
                                <p>
                                    Academic details can only be
                                    updated by your coordinator.
                                </p>

                                <asp:Button ID="btnSave" runat="server" ClientIDMode="Static" CssClass="profile-save" Text="Save Changes" OnClick="btnSave_Click" aria-describedby="profileAvailability" />
                            </footer>

                        </div>
                    </div>
                </div>
            </main>
            </div>
        </div>
    </form>
    <script src="../Js_project/Demo/demo-store.js?v=20261023-hide-unavailable-notice"></script>
<script>
        (() => {
            const cvInput = document.getElementById("cvUpload");
            const selectedName = document.getElementById("cvSelectedName");
            if (!cvInput || !selectedName) return;
            cvInput.addEventListener("change", () => {
                selectedName.textContent = cvInput.files && cvInput.files.length
                    ? cvInput.files[0].name
                    : "No file chosen";
            });
        })();

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
<script src="../Js_project/Student/profile.js?v=20261021-original-ui"></script>
</body>
</html>
