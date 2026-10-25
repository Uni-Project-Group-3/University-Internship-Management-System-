<%@ Page Language="C#" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Task Exams | JUST Internships</title>
    <link href="../Assets/company/TaskExams.css" rel="stylesheet" />
    <link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261024-refine" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-task-flow" defer></script>
</head>
<body>
    <aside class="sidebar workspace-static-sidebar workspace-canonical-sidebar" aria-label="Company navigation"><a class="brand workspace-nav-brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Company Supervisor</p><nav class="workspace-nav-primary" aria-label="Company Supervisor pages"><a class="workspace-nav-link" href="CompanyDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ManageOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Opportunities</span></a><a class="workspace-nav-link" href="ApplicationsReview.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Applications Review</span></a><a class="workspace-nav-link" href="MyTrainees.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Trainees</span></a><a class="workspace-nav-link" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Manage Tasks</span></a><a class="workspace-nav-link active" href="ManageExams.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Task Exams</span></a><a class="workspace-nav-link" href="VerifyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3h6v6H3V3Zm12 0h6v6h-6V3ZM3 15h6v6H3v-6Zm12 0h2v2h-2v-2Zm4 0h2v6h-6v-2h4v-4Z"/></svg><span>Verify Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Reports</span></a><a class="workspace-nav-link" href="ReportsFinalEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Final Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CompanySupervisorAccount.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside><main class="exam-shell">
        <header class="exam-header"><div><a href="ManageTasks.aspx">&larr; Manage Tasks</a><h1>Task Exams</h1><p>Create and publish versioned exams linked to existing placement tasks.</p></div><button id="newExam" class="primary" type="button">New exam</button></header>
        <div id="notice" class="notice" role="status">Loading Firebase exam records&hellip;</div>
        <div class="exam-layout">
            <section class="panel exam-list"><h2>Your exams</h2><div id="examList" class="stack"><p class="muted">Loading&hellip;</p></div></section>
            <section class="panel editor" aria-labelledby="editorTitle">
                <h2 id="editorTitle">Create exam draft</h2>
                <form id="examForm">
                    <input id="examId" type="hidden" /><input id="draftRevision" type="hidden" value="0" />
                    <div class="grid two"><label>Placement ID<input id="placementId" required maxlength="160" autocomplete="off" /></label><label>Task ID<input id="taskId" required maxlength="160" autocomplete="off" /></label></div>
                    <label>Exam title<input id="title" required maxlength="180" /></label>
                    <label>Instructions<textarea id="instructions" rows="2" maxlength="4000"></textarea></label>
                    <div class="grid two"><label>Opens at<input id="opensAt" type="datetime-local" required /></label><label>Closes at<input id="closesAt" type="datetime-local" required /></label></div>
                    <div class="grid two"><label>Duration (minutes)<input id="duration" type="number" min="1" max="600" value="30" required /></label><label>Attempt limit<input id="attemptLimit" type="number" min="1" max="10" value="1" required /></label></div>
                    <div class="question-head"><h3>Questions</h3><button type="button" id="addQuestion" class="secondary">Add question</button></div>
                    <div id="questions" class="stack"></div>
                    <div class="actions"><button id="saveDraft" type="button" class="secondary">Save draft</button><button id="publishExam" type="button" class="primary">Publish exam</button></div>
                </form>
            </section>
        </div>
    </main>
    <link href="../Assets/task-workspace.css" rel="stylesheet" />
    <script src="../Js_project/firebase-client.js?v=20261023-live-review"></script>
    <script src="../Js_project/task-dialogs.js?v=20261025-confirm-popup"></script>
    <script src="../Js_project/Company/TaskExams.js?v=20261025-flow"></script>
<link href="../Assets/task-flow.css?v=20261025-task-details" rel="stylesheet" />
</body>
</html>
