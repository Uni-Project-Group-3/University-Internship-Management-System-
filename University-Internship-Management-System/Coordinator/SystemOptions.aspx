<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SystemOptions.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.SystemOptions" %>

<!DOCTYPE html>
<html lang="en">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>System Options | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/coordinator/Manage_Academic_Supervisor.css?v=20261024-link-cleanup" rel="stylesheet" />
        <link href="../Assets/coordinator/system-options.css?v=20261024" rel="stylesheet" />
        <link href="../Assets/shared/workspace-identity.css?v=20261024-coordinator-banner-2" rel="stylesheet" />
        <script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
    </head>
    <body class="coordinator-banner management-page" data-page="system-options">
        <form id="form1" runat="server" novalidate="novalidate">
            <div class="app">
                <aside class="sidebar workspace-canonical-sidebar" id="sidebar" aria-label="Coordinator navigation"><a class="brand workspace-nav-brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Internship Coordinator</p><nav class="workspace-nav-primary" aria-label="Internship Coordinator pages"><a class="workspace-nav-link" href="CoordinatorDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="../Coordinator/UniversityRecords.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 4h16v16H4V4Zm4 0v16M4 9h16M4 14h16"/></svg><span>University Records</span></a><a class="workspace-nav-link" href="ManageStudents.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Students</span></a><a class="workspace-nav-link" href="ManageCompanies.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Companies</span></a><a class="workspace-nav-link" href="Manage_Academic_Supervisor.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Manage Academic Supervisors</span></a><a class="workspace-nav-link" href="Assign_Supervisors.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>Assign Academic Supervisors</span></a><a class="workspace-nav-link" href="MonitorPlacementsProgress.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>Placements &amp; Progress</span></a><a class="workspace-nav-link" href="ContactMessages.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Contact Messages</span></a><a class="workspace-nav-link active" href="SystemOptions.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M20 18h0"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/></svg><span>System Options</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CoordinatorAccountProfile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CssClass="text-danger">
                                <i class="bi bi-box-arrow-right" aria-hidden="true">
                                </i>Logout</asp:LinkButton></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
                <div class="sidebar-backdrop" aria-hidden="true"></div>
                <div class="main">
                    <div class="content">
                        <div class="page-header management-hero">
                            <div>
                                <p class="management-eyebrow">SYSTEM SETUP</p><h1 class="greeting">System Options</h1>
                                <p class="subtitle mb-0">Manage the dropdown lists used across the system. Every change is saved to Firebase immediately.</p>
                            </div>
                            <button type="button" class="btn custom-btn-primary" id="newListButton">+ New list</button>
                        </div>
                        <p id="optionsNotice" class="options-notice" role="status" aria-live="polite">Loading options from Firebase...</p>
                        <div class="options-layout">
                            <section class="panel options-lists" aria-labelledby="listsTitle">
                                <h2 id="listsTitle" class="panel-title">Lists</h2>
                                <div id="optionLists" class="options-list-buttons"></div>
                            </section>
                            <section class="panel options-items" aria-labelledby="itemsTitle">
                                <div class="options-items-heading">
                                    <div><h2 id="itemsTitle" class="panel-title">Options</h2><p id="listKeyHint" class="options-hint"></p></div>
                                    <div class="options-list-actions"><button type="button" id="renameListButton" class="options-secondary" disabled="disabled">Rename list</button><button type="button" id="deleteListButton" class="options-danger" disabled="disabled">Delete list</button></div>
                                </div>
                                <div class="options-add">
                                    <label for="newOptionLabel" class="visually-hidden">New option label</label>
                                    <input id="newOptionLabel" type="text" maxlength="120" placeholder="New option label, e.g. Amman" disabled="disabled" />
                                    <button type="button" id="addOptionButton" class="btn custom-btn-primary" disabled="disabled">Add option</button>
                                </div>
                                <div class="table-responsive">
                                    <table class="table custom-table align-middle">
                                        <thead><tr><th>Label</th><th>Key (automatic)</th><th>Order</th><th>Active</th><th class="text-end">Actions</th></tr></thead>
                                        <tbody id="optionRows"><tr><td colspan="5">Choose a list.</td></tr></tbody>
                                    </table>
                                </div>
                                <p class="options-hint">Use a list on any page with <code>&lt;select data-system-options="KEY"&gt;</code> and the <code>system-options.js</code> script. Open pages pick up changes within 30 seconds.</p>
                            </section>
                        </div>
                    </div>
                </div>
            </div>
        </form>
        <script src="../Js_project/system-options.js?v=20261024" defer></script>
        <script src="../Js_project/Coordinator/system-options-manager.js?v=20261024" defer></script>
    </body>
</html>
