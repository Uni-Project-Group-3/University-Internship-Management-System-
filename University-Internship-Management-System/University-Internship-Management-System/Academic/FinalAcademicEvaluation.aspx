<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FinalAcademicEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Academic.FinalAcademicEvaluation" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Final Academic Evaluation | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />

    <link runat="server" href="~/Assets/academic/final-academic-evaluation.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/academic.css" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page evaluation-page">
    <form id="form1" runat="server">
        <div class="app-layout">
                        <aside class="dash-sidebar" id="dashboardSidebar" aria-label="Academic navigation">
                <a href="<%= ResolveUrl("~/Academic/test.aspx") %>" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text">
                        <strong>JUST</strong>
                        <span>Internships</span>
                    </span>
                </a>
                <button type="button" class="dash-sidebar-close" data-sidebar-close aria-label="Close navigation">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="m6 6 12 12M6 18 18 6"/>
                    </svg>
                </button>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav">
                        <li>
                            <a href="<%= ResolveUrl("~/Academic/test.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <path d="M3 10 12 3l9 7v13H3V10Z"/>
                                    <path d="M9 20v-7h6v7"/>
                                </svg>
                                <span>Dashboard</span>
                            </a>
                        </li>
                        <li>
                            <a href="<%= ResolveUrl("~/Academic/StudentProgressMonitor.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <path d="M2 13h5l3-9 4 16 3-9h5"/>
                                </svg>
                                <span>Student Progress Monitor</span>
                            </a>
                        </li>
                        <li>
                            <a href="<%= ResolveUrl("~/Academic/FinalAcademicEvaluation.aspx") %>" class="active" aria-current="page">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <rect x="5" y="3" width="14" height="18" rx="2"/>
                                    <path d="m8 12 3 3 5-6"/>
                                </svg>
                                <span>Final Academic Evaluation</span>
                            </a>
                        </li>
                    </ul>
                    <div class="dash-side-divider">
                    </div>
                    <ul class="dash-side-nav">
                        <li>
                            <span class="dash-nav-disabled" aria-disabled="true" title="Account page is not available yet">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="4"/>
                                    <path d="M12 2v3m0 14v3M2 12h3m14 0h3M5 5l2 2m10 10 2 2M5 19l2-2M17 7l2-2"/>
                                </svg>
                                <span>My Account</span>
                            </span>
                        </li>
                        <li>
                            <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="9"/>
                                    <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                </svg>
                                <span>Help</span>
                            </a>
                        </li>
                    </ul>
                </nav>
                <div class="dash-side-promo">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <rect x="5" y="3" width="14" height="18" rx="2"/>
                        <path d="m8 12 3 3 5-6"/>
                    </svg>
                    <p>Support your students.<br />Review their training journey.</p>
                    <a href="<%= ResolveUrl("~/Academic/FinalAcademicEvaluation.aspx") %>">Start evaluating &rarr;</a>
                </div>
            </aside>
            <button type="button" class="dash-sidebar-backdrop" data-sidebar-close aria-label="Close navigation" tabindex="-1" hidden>
            </button>
            <div class="dash-main">
                <header class="dash-topbar">
                    <button type="button" class="dash-menu-btn" id="dashboardMenuToggle" aria-controls="dashboardSidebar" aria-expanded="false" aria-label="Open navigation">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <path d="M4 6h16M4 12h16M4 18h16"/>
                        </svg>
                    </button>
                    <label class="dash-search">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <circle cx="10" cy="10" r="6"/>
                            <path d="m15 15 6 6"/>
                        </svg>
                        <span class="dash-sr-only">Search this page</span>
                        <input type="search" id="dashboardSearch" data-search-target="evaluationSearch" placeholder="Search for students or companies..." />
                    </label>
                    <div class="dash-topbar-actions">
                        <button type="button" class="dash-icon-btn" disabled aria-label="Notifications are not connected" title="Notifications are not connected">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                <path d="M5 17h14l-2-3V9a5 5 0 0 0-10 0v5l-2 3Zm5 3h4"/>
                            </svg>
                        </button>
                        <details class="dash-profile">
                            <summary class="dash-user-chip">
                                <span class="dash-avatar">AS</span>
                                <span class="dash-user-name">Academic Supervisor<span class="dash-role">Academic workspace</span>
                            </span>
                            <span aria-hidden="true">&#8964;</span>
                        </summary>
                        <div class="dash-profile-menu">
                            <a href="<%= ResolveUrl("~/Contact.aspx") %>">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                    <circle cx="12" cy="12" r="9"/>
                                    <path d="M9 8a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3v1"/>
                                </svg>Help &amp; contact</a>
                                <a href="<%= ResolveUrl("~/Auth/Login.aspx") %>">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                        <circle cx="12" cy="8" r="3"/>
                                        <path d="M5 21v-3a7 7 0 0 1 14 0v3"/>
                                    </svg>Sign in</a>
                                </div>
                            </details>
                        </div>
                    </header>
            <main class="app-content evaluation-main">

                <section class="evaluation-heading"><div><p class="evaluation-eyebrow">ACADEMIC ASSESSMENT</p><h1>Final Academic Evaluation</h1><p>Evaluate your assigned students and send their final academic evaluations to the University Internship Coordinator.</p></div><span class="evaluation-department"><span aria-hidden="true">&#9678;</span> Department: not connected</span></section>
                <div class="evaluation-notice"><strong>Interface preview</strong><span>Assigned students, department, and grading criteria will come from the authorized account after Firebase integration.</span></div>
                <section class="evaluation-stats" aria-label="Evaluation summary">
                    <article><div><span>My Students</span><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="8" r="3"/><path d="M3 21v-4a6 6 0 0 1 12 0v4m1-16a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5"/></svg></div><strong>&mdash;</strong><small>Officially assigned to you</small></article>
                    <article><div><span>Pending Evaluations</span><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg></div><strong>&mdash;</strong><small>Not started and draft evaluations</small></article>
                    <article><div><span>Submitted Evaluations</span><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg></div><strong>&mdash;</strong><small>Sent for Coordinator review</small></article>
                </section>
                <section class="evaluation-panel" aria-labelledby="studentsTitle">
                    <div class="evaluation-panel-heading"><div><h2 id="studentsTitle">Student evaluations</h2><p>Your academic assessment, ready for the next step.</p></div><details class="evaluation-preview-menu"><summary>Preview popups <span aria-hidden="true">&#8964;</span></summary><div><button type="button" data-preview="details">Student details</button><button type="button" data-preview="evaluate">Evaluate student</button><button type="button" data-preview="draft">Edit draft</button><button type="button" data-preview="submitted">View submitted evaluation</button></div></details></div>
                    <div class="evaluation-toolbar"><label class="evaluation-search"><span>Search students</span><input type="search" id="evaluationSearch" placeholder="Student name or university ID..." /></label><label><span>Evaluation status</span><select id="evaluationStatus"><option value="">All statuses</option><option>Not Started</option><option>Draft</option><option>Submitted</option></select></label><button type="button" class="evaluation-text-button" id="clearEvaluationFilters">Clear filters</button></div>
                    <div class="evaluation-table-wrap" role="region" aria-label="Student evaluations table" tabindex="0"><table><thead><tr><th scope="col">Student name</th><th scope="col">University ID</th><th scope="col">Training company</th><th scope="col">Evaluation status</th><th scope="col">Actions</th></tr></thead><tbody></tbody></table></div>
                    <div class="evaluation-empty"><svg viewBox="0 0 48 48" aria-hidden="true"><rect x="10" y="7" width="28" height="35" rx="4"/><path d="M18 7V4h12v8H18V7m-1 14h14m-14 7h9m-9 7h6"/></svg><h3 id="evaluationEmptyTitle">Ready for your academic review</h3><p id="evaluationEmptyText">Your assigned students will appear here once the account and data connection are configured.</p><span>No student records or evaluations are loaded.</span></div>
                    <div class="evaluation-panel-footer"><span>Student data is not connected</span><span class="evaluation-chip">Read-only preview environment</span></div>
                </section>
                <p class="evaluation-guidance"><strong>Assess. Submit. Review.</strong> Academic evaluation submission sends your assessment to the Coordinator. Final internship completion is a separate review.</p>
                <footer class="evaluation-footer"><span>&copy; 2026 Jordan University of Science and Technology</span><span>University Internship Management System</span></footer>
            </main></div>
        </div>
        <dialog id="evaluationDialog" class="evaluation-dialog" aria-labelledby="evaluationDialogTitle" aria-describedby="evaluationPreviewNote">
            <header class="evaluation-dialog-header"><div><p class="evaluation-eyebrow" id="evaluationDialogEyebrow">WORKFLOW PREVIEW</p><h2 id="evaluationDialogTitle">Student details</h2></div><button type="button" class="evaluation-close" id="closeEvaluationDialog" aria-label="Close evaluation popup">&times;</button></header>
            <div class="evaluation-dialog-body">
                <p class="evaluation-inline-note" id="evaluationPreviewNote">Layout preview only. No student selected. Nothing is sent to Firebase or the Coordinator.</p>
                <div data-view="details" hidden>
                    <div class="evaluation-student-header"><span class="evaluation-avatar" aria-hidden="true">&#9675;</span><div><strong>No student selected</strong><small>Assigned student information will appear here.</small></div></div>
                    <dl class="evaluation-info"><div><dt>Student name</dt><dd>&mdash;</dd></div><div><dt>University ID</dt><dd>&mdash;</dd></div><div><dt>Department</dt><dd>&mdash;</dd></div><div><dt>Training company</dt><dd>&mdash;</dd></div><div><dt>Company supervisor</dt><dd>&mdash;</dd></div><div><dt>Training status</dt><dd>&mdash;</dd></div></dl>
                    <section class="evaluation-detail-section"><h3>Training &amp; progress</h3><div class="evaluation-hours"><div><span>Required hours</span><strong>&mdash;</strong></div><div><span>Approved hours</span><strong>&mdash;</strong></div><div><span>Task completion</span><strong>&mdash;</strong></div></div><p class="evaluation-help">Training hours and task progress remain separate measures.</p></section>
                    <section class="evaluation-detail-section"><h3>Company feedback</h3><p class="evaluation-help">No company feedback loaded.</p></section>
                </div>
                <div data-view="editor" hidden>
                    <div class="evaluation-student-header"><span class="evaluation-avatar" aria-hidden="true">&#9675;</span><div><strong>No student selected</strong><small>University ID &mdash; &nbsp; &middot; &nbsp; Department &mdash;</small></div><span class="evaluation-chip" id="previewDraftStatus">Not Started</span></div>
                    <section class="evaluation-detail-section"><h3>Evaluation criteria</h3><div class="evaluation-config-note"><strong>Approved grading configuration required</strong><p>Criteria, rating scales, and weights have not been provided. No scoring system is assumed in this preview.</p></div><label class="evaluation-field">University evaluation criteria<input type="text" value="Awaiting approved configuration" disabled /></label></section>
                    <section class="evaluation-detail-section"><h3>Academic feedback</h3><label class="evaluation-field" for="academicComments">Comments <span class="evaluation-optional">Preview field</span><textarea id="academicComments" rows="4" maxlength="2000" placeholder="Write academic feedback and observations..." aria-describedby="academicCommentsError commentsCounter"></textarea></label><div class="evaluation-field-meta"><span id="academicCommentsError" class="evaluation-field-error" aria-live="polite"></span><span id="commentsCounter">0 / 2000</span></div><p class="evaluation-help">For this preview, enter at least 10 characters to review the confirmation layout. This is not an official grading requirement.</p></section>
                    <p id="draftPreviewMessage" class="evaluation-success-message" role="status"></p>
                </div>
                <div data-view="confirmation" hidden>
                    <div class="evaluation-confirm-icon" aria-hidden="true">&#8599;</div><h3>Review before sending</h3><p class="evaluation-help">The final assessment will be sent to the University Internship Coordinator. Submitted evaluations are read only by default.</p><dl class="evaluation-info"><div><dt>Student</dt><dd>No student selected</dd></div><div><dt>Evaluation criteria</dt><dd>Configuration required</dd></div></dl><section class="evaluation-detail-section"><h3>Academic comments</h3><p id="confirmationComments" class="evaluation-comment-copy"></p></section><p class="evaluation-help">Sending is disabled until the student, approved criteria, authorization, and Firebase connection are available. This does not approve internship completion.</p>
                </div>
                <div data-view="submitted" hidden>
                    <div class="evaluation-student-header"><span class="evaluation-avatar" aria-hidden="true">&#10003;</span><div><strong>Submitted evaluation layout</strong><small>No submitted record loaded</small></div><span class="evaluation-chip">Read only</span></div><dl class="evaluation-info"><div><dt>Student name / ID</dt><dd>&mdash;</dd></div><div><dt>Department / Company</dt><dd>&mdash;</dd></div><div><dt>Submission date</dt><dd>&mdash;</dd></div><div><dt>Evaluation status</dt><dd>No record loaded</dd></div></dl><section class="evaluation-detail-section"><h3>Evaluation criteria</h3><p class="evaluation-help">Submitted criteria and their original rating scales will appear here.</p></section><section class="evaluation-detail-section"><h3>Academic comments</h3><p class="evaluation-help">No submitted comments loaded.</p></section>
                </div>
                <div data-view="discard" hidden><h3>Discard unsaved preview changes?</h3><p class="evaluation-help">Your latest edits have not been kept in the preview draft. Leave this popup to discard those changes, or return to continue editing.</p></div>
            </div>
            <footer class="evaluation-dialog-footer">
                <div data-footer="details" hidden><button type="button" class="evaluation-button evaluation-button--secondary" data-close>Close</button></div>
                <div data-footer="editor" hidden><button type="button" class="evaluation-button evaluation-button--secondary" data-close>Cancel</button><button type="button" class="evaluation-button evaluation-button--secondary" id="savePreviewDraft">Keep preview draft</button><button type="button" class="evaluation-button" id="reviewEvaluation">Review submission</button></div>
                <div data-footer="confirmation" hidden><button type="button" class="evaluation-button evaluation-button--secondary" id="backToEvaluation">Back to evaluation</button><button type="button" class="evaluation-button" disabled>Send to Coordinator</button></div>
                <div data-footer="submitted" hidden><button type="button" class="evaluation-button evaluation-button--secondary" data-close>Close</button></div>
                <div data-footer="discard" hidden><button type="button" class="evaluation-button evaluation-button--secondary" id="continueEvaluation">Keep editing</button><button type="button" class="evaluation-button evaluation-button--danger" id="discardEvaluation">Discard changes</button></div>
            </footer>
        </dialog>
    </form>
    <script src="<%= ResolveUrl("~/Js_project/Academic/final-academic-evaluation.js") %>"></script>
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js") %>"></script>
</body>
</html>
