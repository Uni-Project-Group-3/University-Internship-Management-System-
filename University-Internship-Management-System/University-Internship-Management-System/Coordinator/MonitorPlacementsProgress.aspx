<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MonitorPlacementsProgress.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.MonitorPlacementsProgress" %>
<%@ Register Src="~/Shared/Sidebar.ascx" TagPrefix="shared" TagName="Sidebar" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Monitor Placements &amp; Progress | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/sidebar.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/monitor-placements-progress.css" rel="stylesheet" />
</head>
<body class="app-page placements-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <shared:Sidebar runat="server" HomeUrl="~/Coordinator/test.aspx" UserRole="Coordinator" />
            <main class="app-content placements-main" id="placementsApp" data-source="<%= ResolveUrl("~/Coordinator/MonitorPlacementsProgress.aspx?data=1") %>">
                <header class="placements-topbar"><span>Faculty of Information Technology <span aria-hidden="true">/</span> Coordinator workspace</span><img src="<%= ResolveUrl("~/Assets/shared/images/just-logo.png") %>" alt="Jordan University of Science and Technology" /></header>
                <section class="placements-heading"><div><p class="placements-eyebrow">UNIVERSITY-WIDE OVERSIGHT</p><h1>Monitor Placements &amp; Progress</h1><p>Track university-wide internships, training hours, tasks, evaluations, and student progress.</p></div><span class="placements-chip">JSON preview</span></section>
                <div class="placements-notice"><strong>Demonstration data</strong><span id="snapshotLabel">Loading a fixed training snapshot...</span><span>No Firebase connection or data writes.</span></div>
                <section class="placements-stats" aria-label="Placement summary">
                    <article><span>Total Students</span><strong id="totalStudents">&mdash;</strong><small>Across all departments</small></article>
                    <article><span>Active Internships</span><strong id="activeInternships">&mdash;</strong><small>Currently in training</small></article>
                    <article><span>Completed Internships</span><strong id="completedInternships">&mdash;</strong><small>Recorded internship status</small></article>
                    <article class="placements-stat-risk"><span>At-Risk Internships</span><strong id="riskInternships">&mdash;</strong><small>Delays or pending follow-up</small></article>
                </section>
                <section class="placements-panel" aria-labelledby="directoryTitle">
                    <div class="placements-panel-heading"><div><h2 id="directoryTitle">Placement overview</h2><p>One clear view of every student's training journey.</p></div><span id="recordCount" class="placements-chip">Loading</span></div>
                    <div class="placements-filters">
                        <label class="placements-search"><span>Search students or supervisors</span><input type="search" id="placementSearch" placeholder="Name, student ID, company, or supervisor..." /></label>
                        <label><span>Department</span><select id="departmentFilter"><option value="">All departments</option></select></label>
                        <label><span>Company</span><select id="placementCompanyFilter"><option value="">All companies</option></select></label>
                        <label><span>Training status</span><select id="placementStatusFilter"><option value="">All statuses</option></select></label>
                        <label><span>Sort by</span><select id="placementSort"><option value="name">Name: A to Z</option><option value="name-desc">Name: Z to A</option><option value="hours">Approved hours: high to low</option><option value="tasks">Tasks: high to low</option><option value="risk">At risk first</option></select></label>
                        <button type="button" id="clearPlacements" class="placements-link-button">Clear filters</button>
                    </div>
                    <div id="placementMessage" class="placements-message" role="status" aria-live="polite">Loading placements...</div>
                    <button type="button" id="retryPlacements" class="placements-button" hidden>Retry loading</button>
                    <div class="placements-table-wrap" tabindex="0" role="region" aria-label="Internship placements table"><table class="placements-table"><thead><tr><th scope="col">Student / ID</th><th scope="col">Department</th><th scope="col">Company</th><th scope="col">Company supervisor</th><th scope="col">Academic supervisor</th><th scope="col">Training status</th><th scope="col">Approved / Required</th><th scope="col">Task completion</th><th scope="col">Progress indicators</th><th scope="col">Actions</th></tr></thead><tbody id="placementRows"></tbody></table></div>
                    <div class="placements-pagination"><span id="placementResults" aria-live="polite"></span><div><label for="placementPageSize">Rows</label><select id="placementPageSize"><option>5</option><option>10</option><option>25</option></select><button type="button" id="previousPlacements" aria-label="Previous page" disabled>&larr;</button><span id="placementPageLabel">&mdash;</span><button type="button" id="nextPlacements" aria-label="Next page" disabled>&rarr;</button></div></div>
                </section>
                <p class="placements-guidance"><strong>Monitoring, with context.</strong> Task completion and approved hours are separate measures. Final internship approval belongs to the Final Completion Review workflow.</p>
                <footer class="placements-footer"><span>&copy; 2026 Jordan University of Science and Technology</span><span>University Internship Management System</span></footer>
                <noscript><p>Enable JavaScript to load the JSON dataset and use monitoring controls.</p></noscript>
            </main>
        </div>
        <dialog id="placementDetails" class="placements-drawer" aria-labelledby="placementDetailsTitle">
            <header class="placements-drawer-header"><div><p class="placements-eyebrow">PLACEMENT DETAILS</p><h2 id="placementDetailsTitle">Student details</h2><p id="detailStudentId"></p></div><button type="button" id="closePlacementDetails" class="placements-close" aria-label="Close student details">&times;</button></header>
            <div id="placementDetailsBody"></div>
        </dialog>
    </form>
    <script src="<%= ResolveUrl("~/Js_project/Coordinator/monitor-placements-progress.js") %>"></script>
</body>
</html>
