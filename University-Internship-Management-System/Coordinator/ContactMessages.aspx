<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ContactMessages.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.ContactMessages" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Contact Messages | JUST Internships</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    
    <link runat="server" href="~/Assets/coordinator/contact-messages.css?v=3" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner app-page contact-messages-page role-shell" data-demo-role="admin">
    <form id="form1" runat="server">
        <div class="app">
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar" aria-label="Internship Coordinator navigation" data-role-sidebar="true"><a class="brand workspace-nav-brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Internship Coordinator</p><nav class="workspace-nav-primary" aria-label="Internship Coordinator pages"><a class="workspace-nav-link" href="CoordinatorDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="../Coordinator/UniversityRecords.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4h16v16H4V4Zm4 0v16M4 9h16M4 14h16"/></svg><span>University Records</span></a><a class="workspace-nav-link" href="ManageStudents.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Students</span></a><a class="workspace-nav-link" href="ManageCompanies.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Companies</span></a><a class="workspace-nav-link" href="Manage_Academic_Supervisor.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Academic Supervisors</span></a><a class="workspace-nav-link" href="Assign_Supervisors.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Assign Academic Supervisors</span></a><a class="workspace-nav-link" href="MonitorPlacementsProgress.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>Placements &amp; Progress</span></a><a class="workspace-nav-link active" href="ContactMessages.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Contact Messages</span></a><a class="workspace-nav-link" href="SystemOptions.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M20 18h0"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/></svg><span>System Options</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CoordinatorAccountProfile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <button type="button" class="sidebar-backdrop" aria-label="Close navigation menu" tabindex="-1"></button>
            <div class="main">
                <header class="topbar">
                    <button type="button" class="menu-btn" aria-label="Open navigation menu" aria-expanded="false" aria-controls="sidebar"><svg class="bi bi-list nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M3 6h18M3 12h18M3 18h18"/></svg></button>
                    <div class="search">
                        <svg class="bi bi-search nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/></svg>
                        <label class="nav-sr-only" for="navigationSearch">Search workspace</label><input id="navigationSearch" type="search" placeholder="Search for students, companies, or supervisors..." disabled="disabled" aria-describedby="navigationAvailability" />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" disabled="disabled" aria-label="Notifications unavailable" title="Notifications are not connected yet"><svg class="bi bi-bell nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M4 17h16l-2-4V9a6 6 0 0 0-12 0v4l-2 4Zm6 4h4M12 1v2"/></svg></button>
                        <details class="dropdown" id="accountMenu">
                            <summary class="user-chip" aria-label="Internship Coordinator account menu">
                                <span class="avatar"><span id="lblInitials" aria-hidden="true"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg></span></span>
                                <span class="name"><span id="lblUserName">Coordinator account</span><span class="role">Internship Coordinator</span></span>
                                <svg class="bi bi-chevron-down small nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 9 6 6 6-6"/></svg>
                            </summary>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CoordinatorAccountProfile.aspx" class="dropdown-item" role="link"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button id="btnLogout" type="button" class="dropdown-item"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                <span id="navigationAvailability" class="nav-sr-only">Workspace search and notifications require a connected account.</span></header>

                <main class="app-content" id="mainContent">
                <div class="messages-shell">
                    <nav class="messages-breadcrumb" aria-label="Breadcrumb">
                        <a href="CoordinatorDashboard.aspx">Coordinator workspace</a>
                        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 6 6 6-6 6"/></svg>
                        <span aria-current="page">Contact Messages</span>
                    </nav>

                    <header class="messages-hero">
                        <div class="messages-hero-text">
                            <p class="messages-eyebrow"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 6 9 7 9-7"/></svg>Stay connected</p>
                            <h1>Contact Messages</h1>
                            <p>Review, track and follow up on every inquiry sent through Contact Us &mdash; all in one organized inbox.</p>
                        </div>
                        <div class="messages-hero-actions">
                            <span class="messages-live"><span aria-hidden="true"></span>Inbox synced</span>
                        </div>
                    </header>

                    <section class="messages-stats" aria-label="Contact message summary">
                        <article>
                            <div><span>Total messages</span><span class="messages-stat-icon"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 6 9 7 9-7"/></svg></span></div>
                            <strong>&#8212;</strong>
                            <p>All received inquiries</p>
                        </article>
                        <article>
                            <div><span>Unread</span><span class="messages-stat-icon"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 12v7H4V7h11M4 8l8 6 5-4"/><circle cx="19" cy="5" r="3"/></svg></span></div>
                            <strong>&#8212;</strong>
                            <p>Waiting to be reviewed</p>
                        </article>
                        <article>
                            <div><span>Replied</span><span class="messages-stat-icon"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 5-6 6 6 6M3 11h11a7 7 0 0 1 7 7v2"/></svg></span></div>
                            <strong>&#8212;</strong>
                            <p>Inquiries with a sent response</p>
                        </article>
                    </section>

                    <section class="messages-card" aria-labelledby="inboxTitle">
                        <header class="messages-card-heading">
                            <div>
                                <h2 id="inboxTitle">Inbox</h2>
                                <p>Sender details, subjects and conversation status in one place.</p>
                            </div>
                        </header>

                        <fieldset class="messages-filters" disabled="disabled">
                            <legend class="messages-sr-only">Search and filter messages</legend>
                            <div class="msg-field messages-search">
                                <label for="messageSearch">Search messages</label>
                                <div class="msg-control">
                                    <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="10" cy="10" r="6"/><path d="m15 15 6 6"/></svg>
                                    <input id="messageSearch" type="search" placeholder="Search name, email, or subject" autocomplete="off" />
                                </div>
                            </div>
                            <div class="msg-field">
                                <label for="messageStatus">Message status</label>
                                <div class="msg-control msg-select"><select id="messageStatus"><option value="">All messages</option></select></div>
                            </div>
                            <div class="msg-field">
                                <label for="messageSort">Sort by</label>
                                <div class="msg-control msg-select"><select id="messageSort"><option value="">Newest first</option></select></div>
                            </div>
                        </fieldset>

                        <div class="messages-table-wrap">
                            <table class="messages-table">
                                <caption class="messages-sr-only">Contact messages</caption>
                                <thead><tr><th scope="col">Sender</th><th scope="col">Subject</th><th scope="col">Received</th><th scope="col">Status</th><th scope="col"><span class="messages-sr-only">Actions</span></th></tr></thead>
                                <tbody>
                                    <tr class="msg-skeleton" aria-hidden="true"><td colspan="5"><span></span><span></span></td></tr>
                                    <tr class="msg-skeleton" aria-hidden="true"><td colspan="5"><span></span><span></span></td></tr>
                                    <tr class="msg-skeleton" aria-hidden="true"><td colspan="5"><span></span><span></span></td></tr>
                                </tbody>
                            </table>
                        </div>

                        <div class="messages-empty" role="status">
                            <span class="messages-empty-icon" aria-hidden="true"><svg viewBox="0 0 48 48"><rect x="7" y="13" width="34" height="25" rx="5"/><path d="m8 16 16 12 16-12M17 8h14M21 3h6"/></svg></span>
                            <h3>No messages found</h3>
                            <p>Try a different search or change the status filter.</p>
                        </div>

                        <footer class="messages-list-footer">
                            <span>Loading messages&hellip;</span>
                            <div aria-label="Message pagination">
                                <button type="button" disabled="disabled" aria-label="Previous page"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m15 6-6 6 6 6"/></svg></button>
                                <button type="button" disabled="disabled" aria-label="Next page"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 6 6 6-6 6"/></svg></button>
                            </div>
                        </footer>
                    </section>

                    <dialog id="messageDialog" aria-labelledby="messageDialogTitle">
                        <header class="messages-dialog-head">
                            <div>
                                <p class="messages-eyebrow">Inquiry</p>
                                <h2 id="messageDialogTitle">Contact message</h2>
                            </div>
                            <button type="button" class="messages-dialog-close" data-close aria-label="Close message"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m6 6 12 12M6 18 18 6"/></svg></button>
                        </header>
                        <div class="messages-preview-body">
                            <div class="messages-detail-layout">
                                <section class="messages-original" aria-labelledby="originalMessageTitle">
                                    <h3 id="originalMessageTitle">Message details</h3>
                                    <dl class="messages-metadata">
                                        <div><dt>Sender name</dt><dd>&#8212;</dd></div>
                                        <div><dt>Email address</dt><dd>&#8212;</dd></div>
                                        <div><dt>Subject</dt><dd>&#8212;</dd></div>
                                        <div><dt>Received</dt><dd>&#8212;</dd></div>
                                        <div><dt>Status</dt><dd>&#8212;</dd></div>
                                    </dl>
                                    <div class="messages-body-placeholder">
                                        <h3>Message</h3>
                                        <p>&#8212;</p>
                                    </div>
                                </section>
                            </div>
                            <div class="messages-reply-actions">
                                <button type="button" id="markMessageRead"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m5 12 5 5 9-10"/></svg><span>Mark as read</span></button>
                            </div>
                        </div>
                    </dialog>

                    <div class="messages-guidance">
                        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 3 8 4v5c0 5-8 9-8 9s-8-4-8-9V7l8-4Z"/><path d="m8 12 3 3 5-6"/></svg>
                        <p><strong>Keep conversations in context.</strong> Only authorized coordinators can view messages, mark them as read, or reply.</p>
                    </div>
                    <footer class="messages-footer"><span>UIMS &middot; JUST Internships</span><span>Coordinator workspace</span></footer>
                </div>
                <p id="uimsPageNotice" class="messages-toast" role="status" hidden="hidden"></p>
            </main>
            </div>
        </div>
    </form>
    <script>
        // @ts-nocheck
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
<script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script><script src="../Js_project/Coordinator/contact-messages.js?v=20261021-modern"></script>
    <script>
        // @ts-nocheck
        // Dialog state: "Mark as read" is active only while the open message is unread
        (() => {
            const dialog = document.getElementById("messageDialog");
            const markRead = document.getElementById("markMessageRead");
            if (!dialog || !markRead) return;
            const label = markRead.querySelector("span");

            function updateButton() {
                const statusTitle = [...dialog.querySelectorAll(".messages-metadata dt")].find(dt => dt.textContent.trim() === "Status");
                const statusValue = statusTitle && statusTitle.nextElementSibling;
                const status = statusValue ? statusValue.textContent.trim().toLowerCase() : "";
                if (statusValue && statusValue.dataset.status !== status) statusValue.dataset.status = status;

                markRead.disabled = status !== "unread";
                const text = markRead.disabled ? "Already read" : "Mark as read";
                if (label && label.textContent !== text) label.textContent = text;
            }

            new MutationObserver(updateButton).observe(dialog, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ["open"] });
            markRead.addEventListener("click", () => setTimeout(updateButton, 0));
            updateButton();
        })();

        // Toast: page notices appear briefly, then fade out
        (() => {
            const toast = document.getElementById("uimsPageNotice");
            if (!toast) return;
            let timer;
            new MutationObserver(() => {
                if (toast.hidden) return;
                clearTimeout(timer);
                toast.classList.remove("is-hiding");
                void toast.offsetWidth;
                toast.classList.add("is-showing");
                timer = setTimeout(() => {
                    toast.classList.add("is-hiding");
                    timer = setTimeout(() => { toast.hidden = true; toast.classList.remove("is-hiding", "is-showing"); }, 320);
                }, 3800);
            }).observe(toast, { childList: true, characterData: true, subtree: true, attributes: true, attributeFilter: ["hidden"] });
        })();

        // Page transition: the main content fades out before moving to another page
        (() => {
            const content = document.getElementById("mainContent");
            if (!content) return;
            const reduce = window.matchMedia("(prefers-reduced-motion: reduce)");
            document.addEventListener("click", event => {
                if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
                const link = event.target instanceof Element ? event.target.closest("a[href]") : null;
                if (!link || link.hasAttribute("download") || (link.target && link.target !== "_self")) return;
                const url = new URL(link.href, location.href);
                if (url.origin !== location.origin || (url.pathname === location.pathname && url.search === location.search)) return;
                if (reduce.matches) return;
                event.preventDefault();
                content.classList.add("is-leaving");
                setTimeout(() => { location.href = url.href; }, 220);
            });
            window.addEventListener("pageshow", () => content.classList.remove("is-leaving"));
        })();
    </script>
</body>
</html>