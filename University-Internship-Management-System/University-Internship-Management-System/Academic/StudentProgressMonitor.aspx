<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentProgressMonitor.aspx.cs" Inherits="University_Internship_Management_System.Academic.StudentProgressMonitor" %>
<%@ Register Src="~/Shared/Sidebar.ascx" TagPrefix="shared" TagName="Sidebar" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <title>Student Progress Monitor | JUST UIMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/sidebar.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/progress-monitor.css" rel="stylesheet" />
</head>
<body class="app-page monitor-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <shared:Sidebar runat="server" HomeUrl="~/Academic/test.aspx" UserRole="Academic" />
            <main class="app-content monitor-main">
                <header class="monitor-topbar">
                    <span>Faculty of Information Technology <span class="monitor-divider">/</span> Academic workspace</span>
                    <span class="monitor-identity"><img src="<%= ResolveUrl("~/Assets/shared/images/just-logo.png") %>" alt="JUST" /> Academic Supervisor</span>
                </header>
                <section class="monitor-heading" aria-labelledby="pageTitle">
                    <div><p class="monitor-eyebrow">INTERNSHIP OVERSIGHT</p><h1 id="pageTitle">Student Progress Monitor</h1><p>Track student internships, training hours, tasks, and evaluations.</p></div>
                    <button type="button" class="monitor-button monitor-button--secondary" id="previewDetails">Preview details layout <span aria-hidden="true">&#8599;</span></button>
                </section>
                <div class="monitor-notice" role="status"><span class="monitor-dot" aria-hidden="true"></span><strong>Design preview</strong><span>Firebase integration is scheduled for later. No student records are loaded or saved.</span></div>
                <section class="monitor-stats" aria-label="Student progress summary">
                    <article class="monitor-stat"><span class="monitor-stat-icon" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><circle cx="9" cy="8" r="3"/><path d="M3 20v-3a6 6 0 0 1 12 0v3m1-15a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5"/></svg></span><p>Assigned Students</p><strong>&#8212;</strong><small>Official supervisor assignments</small></article>
                    <article class="monitor-stat"><span class="monitor-stat-icon" aria-hidden="true">&#9635;</span><p>Active Internships</p><strong>&#8212;</strong><small>Currently in training</small></article>
                    <article class="monitor-stat"><span class="monitor-stat-icon monitor-stat-icon--warning" aria-hidden="true">&#9719;</span><p>Pending Reviews / Revisions</p><strong>&#8212;</strong><small>Submissions needing attention</small></article>
                    <article class="monitor-stat"><span class="monitor-stat-icon monitor-stat-icon--danger" aria-hidden="true">!</span><p>At-Risk Students</p><strong>&#8212;</strong><small>Delays and outstanding approvals</small></article>
                </section>
                <section class="monitor-panel" aria-labelledby="studentsTitle">
                    <div class="monitor-panel-heading"><div><h2 id="studentsTitle">Your assigned students</h2><p>A clear view of every student's training journey.</p></div><span class="monitor-chip">Data not connected</span></div>
                    <div class="monitor-filters">
                        <label class="monitor-search"><span>Search students</span><input id="studentSearch" type="search" placeholder="Search name or university ID&#8230;" /></label>
                        <label><span>Major</span><select id="majorFilter"><option value="">All majors</option></select></label>
                        <label><span>Company</span><select id="companyFilter"><option value="">All companies</option></select></label>
                        <label><span>Internship status</span><select id="statusFilter"><option value="">All statuses</option></select></label>
                        <label><span>Risk status</span><select id="riskFilter"><option value="">All risk levels</option></select></label>
                        <button type="button" class="monitor-reset" id="resetFilters">Reset</button>
                    </div>
                    <div class="monitor-table-scroll" role="region" aria-label="Assigned students table" tabindex="0">
                        <table class="monitor-table"><thead><tr>
                            <th scope="col">Student / University ID</th><th scope="col">Major</th><th scope="col">Training company / Supervisor</th><th scope="col">Internship status</th><th scope="col">Task progress</th><th scope="col">Approved hours</th><th scope="col">Risk / Delay</th><th scope="col">Details</th>
                        </tr></thead><tbody><tr><td colspan="8"><div class="monitor-empty"><span class="monitor-empty-icon" aria-hidden="true"><svg viewBox="0 0 48 48" fill="none" stroke="currentColor" stroke-width="1.6"><rect x="9" y="8" width="30" height="34" rx="5"/><path d="M17 8V5h14v7H17V8m0 14h14m-14 7h9m-9 7h6"/></svg></span><h3 id="emptyTitle">Ready for your students</h3><p id="emptyMessage">Assigned students and their verified progress will appear here once Firebase is connected.</p><span class="monitor-empty-note">No sample records. No estimated progress.</span></div></td></tr></tbody></table>
                    </div>
                    <div class="monitor-pagination"><span id="resultsStatus" aria-live="polite">Student data is not connected</span><div><label for="pageSize">Rows per page</label><select id="pageSize"><option>10</option><option>25</option><option>50</option></select><button type="button" disabled aria-label="Previous page">&#8592;</button><span>&#8212;</span><button type="button" disabled aria-label="Next page">&#8594;</button></div></div>
                </section>
                <div class="monitor-guidance"><span aria-hidden="true">&#9432;</span><p><strong>Progress with context.</strong> Task completion and approved training hours are tracked separately. Risk indicators highlight attention needed and do not change official internship status.</p></div>
                <footer class="monitor-footer"><span>&#169; 2026 Jordan University of Science and Technology</span><span>University Internship Management System</span></footer>
            </main>
        </div>
        <dialog id="studentDetails" class="monitor-drawer" aria-labelledby="detailsTitle" aria-describedby="detailsPreviewNote">
            <div class="monitor-drawer-heading"><div><p class="monitor-eyebrow">STUDENT OVERVIEW</p><h2 id="detailsTitle">Student details</h2></div><button type="button" class="monitor-close" id="closeDetails" aria-label="Close student details">&#215;</button></div>
            <p class="monitor-drawer-notice" id="detailsPreviewNote">Layout preview only. Select an assigned student after integration to view their records.</p>
            <nav class="monitor-tabs" aria-label="Student detail sections"><a href="#studentInfo">Overview</a><a href="#trainingHours">Training hours</a><a href="#studentTasks">Tasks</a><a href="#academicFeedback">Feedback</a></nav>
            <section class="monitor-detail-section" id="studentInfo"><h3>Student information</h3><dl class="monitor-info"><div><dt>Student name</dt><dd>&#8212;</dd></div><div><dt>University ID</dt><dd>&#8212;</dd></div><div><dt>Major / Department</dt><dd>&#8212;</dd></div><div><dt>Academic supervisor</dt><dd>&#8212;</dd></div><div><dt>Training company</dt><dd>&#8212;</dd></div><div><dt>Company supervisor</dt><dd>&#8212;</dd></div><div><dt>Internship status</dt><dd>&#8212;</dd></div></dl></section>
            <section class="monitor-detail-section" id="trainingHours"><h3>Training hours</h3><div class="monitor-hour-grid"><div><span>Required</span><strong>&#8212;</strong></div><div><span>Submitted</span><strong>&#8212;</strong></div><div><span>Approved</span><strong>&#8212;</strong></div><div><span>Remaining</span><strong>&#8212;</strong></div></div><div class="monitor-track" aria-hidden="true"></div><p class="monitor-detail-note">Hours progress is unavailable until approved records are connected. Completing tasks does not automatically approve hours.</p><h4>Task completion</h4><div class="monitor-track" aria-hidden="true"></div><p class="monitor-detail-note">Tracked separately from approved training hours.</p></section>
            <section class="monitor-detail-section" id="studentTasks"><h3>Tasks &amp; submissions</h3><div class="monitor-detail-empty">Task descriptions, deadlines, submissions, revision requests, and resubmission history will appear here.</div></section>
            <section class="monitor-detail-section"><h3>Company evaluations <span class="monitor-chip">Read only</span></h3><p class="monitor-detail-note">No evaluations loaded. Scores will retain the university's configured grading scale.</p></section>
            <section class="monitor-detail-section" id="academicFeedback"><h3>Academic feedback</h3><label class="monitor-feedback-label" for="feedbackText">Your comments</label><textarea id="feedbackText" rows="4" maxlength="2000" placeholder="Feedback will be available for your assigned students after integration." disabled></textarea><p class="monitor-detail-note">Saving will require verified supervisor access and a server timestamp.</p><button type="button" class="monitor-button" disabled>Save feedback</button></section>
        </dialog>
    </form>
    <script src="<%= ResolveUrl("~/Js_project/Academic/progress-monitor.js") %>"></script>
</body>
</html>
