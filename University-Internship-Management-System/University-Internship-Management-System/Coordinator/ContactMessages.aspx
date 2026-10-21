<%@ Page Title="Contact Messages" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Contact Messages | JUST Internships</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/coordinator.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/contact-messages.css" rel="stylesheet" />
</head>
<body class="app-page contact-messages-page role-shell">
    <form id="form1" runat="server">
        <div class="app">
            <aside class="sidebar" id="sidebar" aria-label="Internship Coordinator navigation" data-role-sidebar="true"><button class="sidebar-close" type="button" aria-label="Close navigation menu"><svg class="bi bi-x nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m6 6 12 12M6 18 18 6"/></svg></button>
                <a href="test.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" role="list">
                    <li><a href="test.aspx" ><svg class="bi bi-house-door nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg>Dashboard</a></li>
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-mortarboard nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m2 8 10-5 10 5-10 5L2 8Zm4 3v6c3 3 9 3 12 0v-6m4-3v9"/></svg>Manage Students</a></li>
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-building nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M4 21V3h13v18m0-10h4v10M8 7h1m3 0h1m-5 4h1m3 0h1m-5 4h1m3 0h1M9 21v-3h4v3"/></svg>Manage Companies</a></li>
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-person-badge nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="10" cy="9" r="3"/><path d="M5 18c0-5 10-5 10 0m1-11h2m-2 4h2"/></svg>Academic Supervisors</a></li>
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-diagram-3 nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><rect x="9" y="2" width="6" height="6" rx="1"/><rect x="2" y="16" width="6" height="6" rx="1"/><rect x="16" y="16" width="6" height="6" rx="1"/><path d="M12 8v4H5v4m7-4h7v4"/></svg>Assign Supervisors</a></li>
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-activity nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M2 12h5l3-9 4 18 3-9h5"/></svg>Placements &amp; Progress</a></li>
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-patch-check nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m12 2 3 3h4v4l3 3-3 3v4h-4l-3 3-3-3H5v-4l-3-3 3-3V5h4l3-3Z"/><path d="m8 12 3 3 5-6"/></svg>Final Completion Review</a></li>
                    <li><a href="ContactMessages.aspx" class="active" aria-current="page"><svg class="bi bi-envelope nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><rect x="2" y="4" width="20" height="16" rx="2"/><path d="m2 6 10 7L22 6"/></svg>Contact Messages</a></li>
                </ul>

                <div class="side-divider"></div>

                <ul class="side-nav" role="list">
                    <li><a role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-gear nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="m9 3-1 3-3 1-2 4 2 2v4l4 2 3-1 3 1 4-2v-4l2-2-2-4-3-1-1-3H9Z"/><circle cx="12" cy="11" r="3"/></svg>Coordinator Account</a></li>
                </ul>

                <div class="side-promo">
                    <svg class="bi bi-person-plus nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="9" cy="7" r="4"/><path d="M2 21v-2a7 7 0 0 1 14 0v2m4-12v8m-4-4h8"/></svg>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a role="link" aria-disabled="true" title="This page is not available in the project yet">Add students &rarr;</a>
                </div>
            </aside>
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
                                <li><a class="dropdown-item" role="link" aria-disabled="true" title="This page is not available in the project yet"><svg class="bi bi-person nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="12" cy="7" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/></svg>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button id="btnLogout" type="button" class="dropdown-item" disabled="disabled" title="Sign-in is not connected yet"><svg class="bi bi-box-arrow-right nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M10 3H3v18h7m4-15 6 6-6 6M8 12h12"/></svg>Log out</button></li>
                            </ul></details>
                    </div>
                <span id="navigationAvailability" class="nav-sr-only">Workspace search and notifications require a connected account.</span></header>
                <main class="app-content">
                <div class="messages-shell">
                    <nav class="messages-breadcrumb" aria-label="Breadcrumb"><a href="test.aspx">Coordinator workspace</a><span aria-hidden="true">/</span><span aria-current="page">Contact Messages</span></nav>
                    <header class="messages-heading"><div><p class="messages-eyebrow">STAY CONNECTED</p><h1>Contact Messages</h1><p>A central place to review inquiries sent through Contact Us.</p></div><a class="messages-public-link" href="../Contact.aspx">View Contact Us <span aria-hidden="true">&#8599;</span></a></header>
                    <div class="messages-notice" id="messagesAvailability" role="note"><span class="messages-info-icon" aria-hidden="true">i</span><div><strong>Inbox connection pending.</strong><p>Contact Us does not send or save messages yet. No messages are loaded, and inbox actions are unavailable.</p></div><span class="messages-preview-badge">Design preview</span></div>
                    <section class="messages-stats" aria-label="Contact message summary"><article><div><span>Total messages</span><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 6 9 7 9-7"/></svg></div><strong aria-label="Unavailable">&#8212;</strong><p>All received inquiries</p></article><article><div><span>Unread</span><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 12v7H4V7h11M4 8l8 6 5-4"/><circle cx="19" cy="5" r="3"/></svg></div><strong aria-label="Unavailable">&#8212;</strong><p>Waiting to be reviewed</p></article><article><div><span>Replied</span><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 5-6 6 6 6M3 11h11a7 7 0 0 1 7 7v2"/></svg></div><strong aria-label="Unavailable">&#8212;</strong><p>Inquiries with a sent response</p></article></section>
                    <section class="messages-card" aria-labelledby="inboxTitle"><header class="messages-card-heading"><div><h2 id="inboxTitle">Inbox</h2><p>Sender details, subjects, and conversation status in one place.</p></div><span class="messages-disconnected"><span aria-hidden="true"></span>Data not connected</span></header>
                        <fieldset class="messages-filters" disabled="disabled" aria-describedby="messagesAvailability"><legend class="messages-sr-only">Search and filter messages after integration</legend><div class="messages-search"><label for="messageSearch">Search messages</label><input id="messageSearch" type="search" placeholder="Search name, email, or subject" /></div><div><label for="messageStatus">Message status</label><select id="messageStatus"><option>All messages</option><option>Unread</option><option>Read</option><option>Replied</option></select></div><div><label for="messageSort">Sort by</label><select id="messageSort"><option>Newest first</option><option>Oldest first</option></select></div></fieldset>
                        <div class="messages-table-wrap"><table class="messages-table"><caption class="messages-sr-only">Contact messages. No records are loaded because the inbox is not connected.</caption><thead><tr><th scope="col">Sender</th><th scope="col">Subject</th><th scope="col">Received</th><th scope="col">Status</th><th scope="col">Actions</th></tr></thead><tbody><tr><td colspan="5"><div class="messages-empty"><span class="messages-empty-icon" aria-hidden="true"><svg viewBox="0 0 48 48"><rect x="7" y="13" width="34" height="25" rx="5"/><path d="m8 16 16 12 16-12M17 8h14M21 3h6"/></svg></span><h3>Your messages will appear here</h3><p>Once message delivery is connected, this inbox will show inquiries submitted through the Contact Us form.</p><span>No contact messages have been loaded.</span></div></td></tr></tbody></table></div>
                        <footer class="messages-list-footer"><span>Records unavailable</span><div aria-label="Message pagination"><button type="button" disabled="disabled" aria-label="Previous page">&#8592;</button><span aria-label="Page unavailable">&#8212;</span><button type="button" disabled="disabled" aria-label="Next page">&#8594;</button></div></footer>
                    </section>
                    <details class="messages-preview"><summary><span><span class="messages-eyebrow">LAYOUT PREVIEW</span><strong>Message details &amp; reply</strong></span><span class="messages-expand-label">Preview layout <span aria-hidden="true">+</span></span></summary><div class="messages-preview-body"><p class="messages-detail-notice">Layout preview only. Select a real inquiry after integration to view its contents or reply.</p><div class="messages-detail-layout"><section class="messages-original" aria-labelledby="originalMessageTitle"><h2 id="originalMessageTitle">Message details</h2><dl class="messages-metadata"><div><dt>Sender name</dt><dd>Not connected</dd></div><div><dt>Email address</dt><dd>Not connected</dd></div><div><dt>Subject</dt><dd>Not connected</dd></div><div><dt>Received</dt><dd>Not connected</dd></div><div><dt>Status</dt><dd>Not connected</dd></div></dl><div class="messages-body-placeholder"><h3>Message</h3><p>The selected inquiry will appear here after the inbox is connected.</p></div></section><section class="messages-reply" aria-labelledby="replyTitle"><h2 id="replyTitle">Your reply</h2><p>Replies will be addressed to the sender of the selected message.</p><label for="messageReply">Reply message</label><textarea id="messageReply" name="replyDraft" rows="7" maxlength="2000" placeholder="Preview your response here..." aria-describedby="replyHelp"></textarea><span id="replyHelp" class="messages-help">Up to 2,000 characters. This preview is not saved or sent.</span><div class="messages-reply-actions"><button type="button" disabled="disabled" aria-describedby="messagesAvailability">Mark as read</button><button type="button" disabled="disabled" aria-describedby="replyHelp">Send Reply</button></div></section></div></div></details>
                    <div class="messages-guidance"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 3 8 4v5c0 5-8 9-8 9s-8-4-8-9V7l8-4Z"/><path d="m8 12 3 3 5-6"/></svg><p><strong>Keep conversations in context.</strong> The inbox must be connected to authorized coordinator access before any messages can be viewed, marked as read, or replied to.</p></div>
                    <footer class="messages-footer"><span>UIMS &middot; JUST Internships</span><span>Coordinator workspace</span></footer>
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
</body>
</html>
