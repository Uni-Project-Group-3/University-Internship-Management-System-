<%@ Page Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" CodeBehind="MyProgressEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Student.MyProgressEvaluation" %>
<%@ Register Src="~/Shared/StudentSidebar.ascx" TagPrefix="uims" TagName="StudentSidebar" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous" />
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" media="print" data-evaluation-external="true" />
    <noscript><link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&amp;family=Inter:wght@400;500;600;700&amp;display=swap" /></noscript>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Progress &amp; Evaluation | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/evaluation-colors.css?v=20261024-fast-paint" rel="stylesheet" />
    <link runat="server" href="~/Assets/student/my-progress-evaluation.css?v=20261024-student-task-data-v2" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/evaluation-workspace.css?v=20261023" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" media="print" data-evaluation-external="true" />
    <link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
    <script>
        document.querySelectorAll('link[data-evaluation-external]').forEach(function (link) {
            function activate() { link.media = 'all'; }
            link.addEventListener('load', activate);
            if (link.sheet) activate();
        });
    </script>
</head>
<body class="app-page uims-workspace student-progress">
    <form id="form1" runat="server">
        <div class="app-layout">
            <uims:StudentSidebar ID="studentSidebar" runat="server" SidebarId="sidebar" /><div class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">Student workspace</span></span></span></div></header>
            <main class="app-content" id="mainContent">
                <section class="uims-page-hero" aria-labelledby="studentProgressTitle">
<div class="uims-hero-copy"><p class="uims-hero-eyebrow">JUST / STUDENT WORKSPACE</p><h1 id="studentProgressTitle">My Progress &amp; Evaluation</h1><p>Review published task grades, supervisor feedback, and approved training progress.</p></div>
<dl class="workspace-stats uims-hero-stats" aria-label="Authorized internship progress">
<div class="workspace-stat uims-hero-stat"><dt>Approved training hours</dt><dd><span data-uims-progress="ApprovedHours"><asp:Literal ID="ApprovedHours" runat="server" /></span></dd></div>
<div class="workspace-stat uims-hero-stat"><dt>Required training hours</dt><dd><span data-uims-progress="RequiredHours"><asp:Literal ID="RequiredHours" runat="server" /></span></dd></div>
<div class="workspace-stat uims-hero-stat"><dt>Hours remaining</dt><dd><span data-uims-progress="RemainingHours"><asp:Literal ID="RemainingHours" runat="server" /></span></dd></div>
<div class="workspace-stat uims-hero-stat"><dt>Published Task Grades</dt><dd><span data-uims-progress="EvaluatedExams"><asp:Literal ID="EvaluatedExams" runat="server" /></span></dd></div>
</dl></section>
                <asp:Panel ID="StudentAccessErrorPanel" runat="server" Visible="false" CssClass="student-access-error" role="alert"><asp:Literal ID="StudentAccessMessage" runat="server" Mode="Encode" /></asp:Panel>
                <asp:Panel ID="DemoStudentPanel" runat="server" Visible="false" CssClass="workspace-note"><asp:Label runat="server" AssociatedControlID="DemoStudentChoice" Text="LOCAL DEMO Student (this dataset only)" /><asp:DropDownList ID="DemoStudentChoice" runat="server" AutoPostBack="true" OnSelectedIndexChanged="DemoStudentChoice_Changed" CssClass="workspace-input"><asp:ListItem Value="0">Student A — Lina (fictional)</asp:ListItem><asp:ListItem Value="1">Student B — Omar (fictional)</asp:ListItem><asp:ListItem Value="2">Student C — Noor (fictional)</asp:ListItem></asp:DropDownList></asp:Panel>

                <div class="workspace-columns">
                    <section class="workspace-card student-progress-card" aria-labelledby="trainingTitle">
                        <div class="workspace-heading student-section-heading"><div class="student-section-title"><span class="student-section-icon" aria-hidden="true"><i class="bi bi-clock-history"></i></span><div><h2 id="trainingTitle">Training progress</h2><p>Only approved attendance contributes to your required training hours.</p></div></div><div class="student-refresh-actions"><asp:Button ID="RefreshProgress" runat="server" ClientIDMode="Static" Text="Refresh progress" CssClass="workspace-button secondary student-refresh-button" OnClick="RefreshProgress_Click" CausesValidation="false" /><span id="progressLoading" role="status" hidden><i class="bi bi-arrow-repeat" aria-hidden="true"></i> Refreshing authorized progress...</span></div></div>
                        <div class="student-progress-summary"><span>Approved hours toward requirement</span><strong><span data-uims-progress="HoursPercent"><asp:Literal ID="HoursPercent" runat="server" /></span></strong></div>
                        <progress id="HoursProgress" runat="server" max="100" value="0" aria-label="Approved training hours progress"></progress>
                        <div class="student-hour-cards">
                            <div class="student-hour-card approved"><span><i class="bi bi-check-circle" aria-hidden="true"></i> Approved hours</span><strong><span data-uims-progress="HoursApprovalStatus"><asp:Literal ID="HoursApprovalStatus" runat="server" Mode="Encode" /></span></strong><small><asp:Literal ID="ApprovedHoursNotice" runat="server" Mode="Encode" /></small></div>
                            <div class="student-hour-card pending"><span><i class="bi bi-hourglass-split" aria-hidden="true"></i> Pending hours</span><strong><span data-uims-progress="PendingHours"><asp:Literal ID="PendingHours" runat="server" Mode="Encode" /></span></strong></div>
                            <div class="student-hour-card required"><span><i class="bi bi-flag" aria-hidden="true"></i> Required hours</span><strong><span data-uims-progress="RequiredHoursInline"><asp:Literal ID="RequiredHoursInline" runat="server" /></span></strong><small><asp:Literal ID="RequiredHoursNotice" runat="server" Mode="Encode" /></small></div>
                        </div>
                        <p class="workspace-hint student-hours-note"><i class="bi bi-info-circle" aria-hidden="true"></i> <asp:Literal ID="PendingHoursNotice" runat="server" Mode="Encode" /></p>
                    </section>
                    <section class="workspace-card student-progress-card" aria-labelledby="completionTitle">
                        <div class="workspace-heading student-section-heading"><div class="student-section-title"><span class="student-section-icon completion" aria-hidden="true"><i class="bi bi-patch-check"></i></span><div><h2 id="completionTitle">Completion status</h2><p>Based on the Coordinator's recorded decision.</p></div></div><span id="CompletionStatusBadge" runat="server" class="workspace-badge warning student-completion-badge"><asp:Literal ID="CompletionStatusText" runat="server" Mode="Encode" /></span></div>
                        <p class="student-completion-message"><asp:Literal ID="CompletionStatusMessage" runat="server" Mode="Encode" /></p>
                        <div class="student-completion-facts"><div><span>Approval date (UTC)</span><strong><asp:Literal ID="CompletionApprovalDate" runat="server" Mode="Encode" /></strong></div><div><span>Overall completion progress</span><strong><span data-uims-progress="OverallProgress"><asp:Literal ID="OverallCompletionProgress" runat="server" Mode="Encode" /></span></strong><small><asp:Literal ID="OverallProgressNotice" runat="server" Mode="Encode" /></small></div></div>
                        <p class="workspace-hint">Meeting the hour requirement alone does not approve internship completion.</p><a class="workspace-button secondary student-dashboard-link" href="StudentDashboard.aspx"><i class="bi bi-grid-1x2" aria-hidden="true"></i> Open Student Dashboard</a>
                    </section>
                </div>
                <section class="workspace-card" aria-labelledby="examsTitle" runat="server" visible="false">
                    <div class="workspace-heading">
                        <div><h2 id="examsTitle">Exam results</h2><p>Company Supervisors assign and publish grades. Academic Supervisors may add feedback; they cannot replace the company grade.</p></div>
                        <asp:Button ID="ToggleExamDemo" runat="server" Text="Preview illustrative exam results" CssClass="workspace-button secondary" OnClick="ToggleExamDemo_Click" CausesValidation="false" />
                    </div>
                    <asp:Panel ID="ExamDemoNotice" runat="server" CssClass="workspace-note" Visible="false"><strong>Illustrative exam layout only.</strong> These fictional examples are not student records, official criteria, or a university grading scale. The example maximum belongs to each example assessment.</asp:Panel>
                    <asp:Panel ID="NoExams" runat="server" CssClass="workspace-empty" role="status">Trusted published exam integration is not connected. No exam records or private final evaluations are displayed.</asp:Panel>
                    <asp:Panel ID="ExamList" runat="server" Visible="false">
                        <div class="workspace-table-scroll" tabindex="0" role="region" aria-label="Published exam results table">
                            <table class="workspace-table exam-table">
                                <caption>Read-only results. Each assessment retains its own maximum grade; no combined grade or weighting is calculated.</caption>
                                <thead><tr><th scope="col">Exam / details</th><th scope="col">Exam date</th><th scope="col">Company Supervisor</th><th scope="col">Grade earned</th><th scope="col">Maximum grade</th><th scope="col">Status</th></tr></thead>
                                <tbody>
                                    <asp:Repeater ID="Exams" runat="server" EnableViewState="false" OnItemCommand="Exams_ItemCommand"><ItemTemplate>
                                        <tr>
                                            <td><strong><%#: Eval("Title") %></strong><br /><asp:LinkButton runat="server" CommandName="ViewExam" CommandArgument='<%# Eval("Id") %>' CssClass="exam-details-link" CausesValidation="false" Text="View details" aria-label='<%# "View details: " + Eval("Title") %>' /></td>
                                            <td><%#: Eval("ExamDate", "{0:dd MMM yyyy}") %></td>
                                            <td><%#: Eval("CompanySupervisor") %></td>
                                            <td><%#: Eval("Grade", "{0:0.##}") %></td>
                                            <td><%#: Eval("MaximumGrade", "{0:0.##}") %></td>
                                            <td><span class="workspace-badge success">Published &middot; illustrative</span></td>
                                        </tr>
                                    </ItemTemplate></asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                        <asp:Panel ID="SelectExamNotice" runat="server" CssClass="workspace-empty">Choose View details beside an exam to read its published result and both supervisors' permitted feedback.</asp:Panel>
                    </asp:Panel>
                </section>
                <dialog id="examDialog" runat="server" visible="false" class="workspace-dialog" aria-labelledby="examDetailTitle"><button type="button" class="popup-close" data-close-exam aria-label="Close exam details">×</button><asp:Panel ID="ExamDetails" runat="server" ClientIDMode="Static" Visible="false" CssClass="workspace-card exam-details" role="region" tabindex="-1" aria-labelledby="examDetailTitle">
                    <div class="workspace-heading"><div><span class="workspace-eyebrow">Published result &middot; illustrative preview</span><h2 id="examDetailTitle"><asp:Literal ID="ExamTitle" runat="server" Mode="Encode" /></h2></div><button type="button" class="workspace-button secondary" data-close-exam>Back to exam results</button></div>
                    <p><asp:Literal ID="ExamDescription" runat="server" Mode="Encode" /></p>
                    <dl class="exam-facts">
                        <div><dt>Exam date</dt><dd><asp:Literal ID="ExamDate" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Company Supervisor</dt><dd><asp:Literal ID="ExamSupervisor" runat="server" Mode="Encode" /></dd></div>
                        <div><dt>Published grade / maximum</dt><dd><asp:Literal ID="ExamGrade" runat="server" Mode="Encode" /></dd></div>
                    </dl>
                    <div class="exam-feedback-grid">
                        <section class="exam-feedback" aria-labelledby="companyFeedbackTitle"><h3 id="companyFeedbackTitle">Company Supervisor feedback</h3><p><span data-uims-progress="CompanyFeedback"><asp:Literal ID="CompanyFeedback" runat="server" Mode="Encode" /></span></p><span class="workspace-hint">The Company Supervisor owns and publishes this exam grade.</span></section>
                        <section class="exam-feedback" aria-labelledby="academicFeedbackTitle"><div class="workspace-heading"><h3 id="academicFeedbackTitle">Academic Supervisor feedback</h3><asp:Label ID="AcademicFeedbackStatus" runat="server" CssClass="workspace-badge" /></div><p><span data-uims-progress="AcademicFeedback"><asp:Literal ID="AcademicFeedback" runat="server" Mode="Encode" /></span></p><span class="workspace-hint">Academic feedback does not modify the published company grade.</span></section>
                    </div>
                </asp:Panel></dialog>
                <section class="workspace-card" aria-labelledby="tasksTitle">
                    <div class="workspace-heading student-section-heading">
                        <div class="student-section-title"><span class="student-section-icon tasks" aria-hidden="true"><i class="bi bi-list-check"></i></span><div><h2 id="tasksTitle">Tasks &amp; supervisor feedback</h2><p>Task-level feedback only. Private final supervisor evaluations are not shown.</p></div></div>
                        <div>
                            <label class="workspace-label" for="taskFilter">Show tasks</label>
                            <select id="taskFilter" class="workspace-input task-filter" aria-describedby="taskFilterStatus">
                                <option value="all">All tasks</option><option value="submitted">Submitted</option><option value="evaluated">Evaluated</option><option value="revision">Revision required</option><option value="assigned">Assigned</option><option value="inprogress">In progress</option>
                            </select>
                        </div>
                    </div>
                    <dl class="workspace-stats task-stats">
                        <div class="workspace-stat"><dt>Assigned tasks (total)</dt><dd><span data-uims-progress="AssignedTasks"><asp:Literal ID="AssignedTasks" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Submitted / pending review</dt><dd><span data-uims-progress="SubmittedTasks"><asp:Literal ID="SubmittedTasks" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Evaluated tasks</dt><dd><span data-uims-progress="EvaluatedTasks"><asp:Literal ID="EvaluatedTasks" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Revision required</dt><dd><span data-uims-progress="RevisionTasks"><asp:Literal ID="RevisionTasks" runat="server" /></span></dd></div>
                    </dl>
                    <div class="workspace-actions task-submission-access"><a href="MyTasks.aspx" class="workspace-button secondary">My Tasks and submissions</a></div>
                    <p id="myTasksNote" class="workspace-hint">Open My Tasks to review the separate task-submission interface. Training-hour approval remains independent.</p>
                    <asp:Panel ID="TaskEmptyState" runat="server" CssClass="student-task-empty" Visible="true" role="status"><i class="bi bi-inbox" aria-hidden="true"></i><strong>Task data unavailable</strong><span>Published task results will appear here when the trusted Tasks integration is connected.</span></asp:Panel>
                    <asp:Panel ID="TasksTablePanel" runat="server" Visible="false">
                    <div class="workspace-table-scroll" tabindex="0" role="region" aria-label="Task evaluations table">
                        <table class="workspace-table">
                            <caption>Authorized published task grades and feedback. Final evaluation results remain separate.</caption>
                            <thead><tr><th scope="col">Task / history</th><th scope="col">Status</th><th scope="col">Company supervisor feedback</th></tr></thead>
                            <tbody>
                                <asp:Repeater ID="Tasks" runat="server" EnableViewState="false">
                                    <ItemTemplate>
                                        <tr data-task-status="<%#: Eval("StatusKey") %>">
                                            <td><strong><%#: Eval("Title") %></strong><small><%#: Eval("Reference") %></small>
                                                <details><summary>Submission &amp; evaluation history</summary><ul class="task-history"><li><%#: Eval("History") %></li></ul></details>
                                            </td>
                                            <td><span class="workspace-badge <%#: Eval("Badge") %>"><%#: Eval("Status") %></span></td>
                                            <td><%#: Eval("Feedback") %><small class="<%#: Eval("NoteClass") %>"><%#: Eval("NextStep") %></small></td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                    </asp:Panel>
                    <p id="taskFilterStatus" runat="server" ClientIDMode="Static" class="workspace-hint" role="status" aria-live="polite"></p>
                </section>
                <section class="workspace-card" aria-labelledby="reportTitle">
                    <div class="workspace-heading student-section-heading"><div class="student-section-title"><span class="student-section-icon report" aria-hidden="true"><i class="bi bi-file-earmark-pdf"></i></span><div><h2 id="reportTitle">Official Internship Completion Report</h2><p>Your approved completion document, when issued by the university.</p></div></div><span class="workspace-badge warning"><asp:Literal ID="ReportStatusText" runat="server" Mode="Encode" /></span></div>
                    <div class="report-status"><strong><asp:Literal ID="ReportAvailabilityText" runat="server" Mode="Encode" /></strong><p class="workspace-muted">Download requires Coordinator approval and a verified private report. To print, download the PDF and use your PDF reader.</p></div>
                    <div class="workspace-actions"><asp:Button ID="DownloadCompletionReport" runat="server" ClientIDMode="Static" Text="Download Completion Report" CssClass="workspace-button student-download-button" OnClick="DownloadCompletionReport_Click" CausesValidation="false" Visible="false" aria-describedby="reportAccessNote" /><button type="button" class="workspace-button secondary" disabled="disabled" aria-describedby="reportAccessNote" title="Download the verified PDF first, then print it from your PDF reader."><i class="bi bi-printer" aria-hidden="true"></i> Print report</button></div>
                    <p id="reportAccessNote" class="workspace-hint">Downloads are verified on the server for the authenticated internship owner. Availability is separate from completion approval.</p>
                </section>
            </main>
            </div>
        </div>
    </form>

<script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="<%= ResolveUrl("~/Js_project/Student/my-progress-evaluation.js?v=20261024-student-progress-ui") %>" defer="defer"></script>
</body>
</html>
