<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyProgressEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Student.MyProgressEvaluation" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Progress &amp; Evaluation | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/student/my-progress-evaluation.css?v=20261021-original-ui" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
</head>
<body class="app-page uims-workspace student-progress" data-demo-role="student">
    <form id="form1" runat="server">
        <div class="app-layout">
            <aside class="sidebar" id="sidebar" aria-label="Student navigation">
    <button type="button" class="sidebar-close" aria-label="Close navigation"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
    <a class="brand" href="StudentDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48"/><span class="brand-text"><strong>JUST</strong><span>Internships</span></span></a>
    <ul class="side-nav"><li><a href="StudentDashboard.aspx"><i class="bi bi-house-door" aria-hidden="true"></i><span>Dashboard</span></a></li>
<li><a href="ExploreOpportunities.aspx"><i class="bi bi-compass" aria-hidden="true"></i><span>Explore Opportunities</span></a></li>
<li><a href="MyApplications.aspx"><i class="bi bi-file-earmark-text" aria-hidden="true"></i><span>My Applications</span></a></li>
<li><a href="ApplicationForm.aspx"><i class="bi bi-file-earmark-plus" aria-hidden="true"></i><span>Application Form</span></a></li>
<li><a href="MyTasks.aspx"><i class="bi bi-list-check" aria-hidden="true"></i><span>My Tasks</span></a></li>
<li><a href="MyProgressEvaluation.aspx" class="active" aria-current="page"><i class="bi bi-graph-up-arrow" aria-hidden="true"></i><span>My Progress &amp; Evaluation</span></a></li>
<li><a href="Profile.aspx"><i class="bi bi-person" aria-hidden="true"></i><span>My Profile</span></a></li></ul>
    <div class="side-divider"></div><ul class="side-nav"><li><a href="../Contact.aspx"><i class="bi bi-question-circle" aria-hidden="true"></i><span>Help &amp; contact</span></a></li><li><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right" aria-hidden="true"></i><span>Logout</span></asp:LinkButton></li></ul>
</aside><div class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">UIMS workspace · frontend preview</span></span></span></div></header>
            <main class="app-content" id="mainContent">
                <header class="workspace-header">
                    <span class="workspace-eyebrow">Student workspace / Your internship</span>
                    <h1>My Progress &amp; Evaluation</h1>
                    <p class="workspace-muted">Review published exam results, supervisor feedback, and approved training progress.</p>
                </header>
                <div class="workspace-note"><strong>Read-only frontend demonstration.</strong> Training hours and tasks are sample data, not your account. Firebase and authentication are not connected. Exam layout examples are separate from real published results.</div>
                <dl class="workspace-stats">
                    <div class="workspace-stat"><dt>Approved training hours</dt><dd><span data-uims-progress="ApprovedHours"><asp:Literal ID="ApprovedHours" runat="server" /></span></dd><small>Explicitly approved sample time entries</small></div>
                    <div class="workspace-stat"><dt>Required hours · demo</dt><dd><span data-uims-progress="RequiredHours"><asp:Literal ID="RequiredHours" runat="server" /></span></dd><small>Sample internship requirement</small></div>
                    <div class="workspace-stat"><dt>Hours remaining</dt><dd><span data-uims-progress="RemainingHours"><asp:Literal ID="RemainingHours" runat="server" /></span></dd><small>Required minus approved hours</small></div>
                    <div class="workspace-stat"><dt>Published exam results</dt><dd><span data-uims-progress="EvaluatedExams"><asp:Literal ID="EvaluatedExams" runat="server" /></span></dd><small>Evaluated assessments released to the student</small></div>
                </dl>
                <div class="workspace-columns">
                    <section class="workspace-card" aria-labelledby="trainingTitle">
                        <h2 id="trainingTitle">Training progress</h2>
                        <p class="workspace-muted">Sample placement · Northstar Software · Software Engineering</p>
                        <div class="progress-summary"><span>Approved hours toward requirement</span><strong><span data-uims-progress="HoursPercent"><asp:Literal ID="HoursPercent" runat="server" /></span>%</strong></div>
                        <progress id="HoursProgress" runat="server" max="100" value="0" aria-label="Approved training hours progress"></progress>
                        <p class="workspace-hint">Task completion does not approve training hours. Only approved time entries count.</p>
                        <span class="workspace-badge success"><span data-uims-progress="HoursApprovalStatus"><asp:Literal ID="HoursApprovalStatus" runat="server" Mode="Encode" /></span> hours approved · demo</span>
                        <span class="workspace-badge warning"><span data-uims-progress="PendingHours"><asp:Literal ID="PendingHours" runat="server" Mode="Encode" /></span> hours awaiting approval · demo</span>
                    </section>
                    <section class="workspace-card" aria-labelledby="completionTitle">
                        <div class="workspace-heading"><h2 id="completionTitle">Completion status</h2><span class="workspace-badge warning">In progress · demo</span></div>
                        <p>Coordinator approval has not been recorded.</p><ol class="workspace-hint"><li>Coordinator approves completion.</li><li>The student confirms completion in the Student Dashboard.</li><li>The certificate is issued and the one-hour grace period begins.</li><li>A trusted scheduled worker disables account access after the deadline.</li></ol><a class="workspace-button secondary" href="StudentDashboard.aspx">Open Student Dashboard</a>
                        <p class="workspace-hint">Reaching the hour requirement alone does not approve internship completion.</p>
                    </section>
                </div>
                <section class="workspace-card" aria-labelledby="examsTitle">
                    <div class="workspace-heading">
                        <div><h2 id="examsTitle">Exam results</h2><p>Company Supervisors assign and publish grades. Academic Supervisors may add feedback; they cannot replace the company grade.</p></div>
                        <asp:Button ID="ToggleExamDemo" runat="server" Text="Preview illustrative exam results" CssClass="workspace-button secondary" OnClick="ToggleExamDemo_Click" CausesValidation="false" />
                    </div>
                    <asp:Panel ID="ExamDemoNotice" runat="server" CssClass="workspace-note" Visible="false"><strong>Illustrative exam layout only.</strong> These fictional examples are not student records, official criteria, or a university grading scale. The example maximum belongs to each example assessment.</asp:Panel>
                    <asp:Panel ID="NoExams" runat="server" CssClass="workspace-empty" role="status">No published exam results are available. Unpublished grades are not displayed. Use the separate illustrative preview to try exam selection.</asp:Panel>
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
                                            <td><span class="workspace-badge success">Published · illustrative</span></td>
                                        </tr>
                                    </ItemTemplate></asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                        <asp:Panel ID="SelectExamNotice" runat="server" CssClass="workspace-empty">Choose View details beside an exam to read its published result and both supervisors' permitted feedback.</asp:Panel>
                    </asp:Panel>
                </section>
                <dialog id="examDialog" class="workspace-dialog" aria-labelledby="examDetailTitle"><button type="button" class="popup-close" data-close-exam aria-label="Close exam details">×</button><asp:Panel ID="ExamDetails" runat="server" ClientIDMode="Static" Visible="false" CssClass="workspace-card exam-details" role="region" tabindex="-1" aria-labelledby="examDetailTitle">
                    <div class="workspace-heading"><div><span class="workspace-eyebrow">Published result · illustrative preview</span><h2 id="examDetailTitle"><asp:Literal ID="ExamTitle" runat="server" Mode="Encode" /></h2></div><button type="button" class="workspace-button secondary" data-close-exam>Back to exam results</button></div>
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
                    <div class="workspace-heading">
                        <div><h2 id="tasksTitle">Tasks &amp; supervisor feedback</h2><p>Task-level feedback only. Private final supervisor evaluations are not shown.</p></div>
                        <div>
                            <label class="workspace-label" for="taskFilter">Show tasks</label>
                            <select id="taskFilter" class="workspace-input task-filter">
                                <option value="all">All tasks</option><option value="evaluated">Evaluated</option><option value="submitted">Submitted</option><option value="revision">Revisions needed</option><option value="assigned">Assigned</option>
                            </select>
                        </div>
                    </div>
                    <dl class="workspace-stats task-stats">
                        <div class="workspace-stat"><dt>Assigned tasks · total</dt><dd><span data-uims-progress="AssignedTasks"><asp:Literal ID="AssignedTasks" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Submitted · pending review</dt><dd><span data-uims-progress="SubmittedTasks"><asp:Literal ID="SubmittedTasks" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Evaluated tasks</dt><dd><span data-uims-progress="EvaluatedTasks"><asp:Literal ID="EvaluatedTasks" runat="server" /></span></dd></div>
                        <div class="workspace-stat"><dt>Revision required</dt><dd><span data-uims-progress="RevisionTasks"><asp:Literal ID="RevisionTasks" runat="server" /></span></dd></div>
                    </dl>
                    <div class="workspace-actions task-submission-access"><a href="MyTasks.aspx" class="workspace-button secondary">My Tasks · submissions</a></div>
                    <p id="myTasksNote" class="workspace-hint">Open My Tasks to review the separate task-submission interface. Training-hour approval remains independent.</p>
                    <div class="workspace-table-scroll" tabindex="0" role="region" aria-label="Task evaluations table">
                        <table class="workspace-table">
                            <caption>Demonstration task records. Exam results above are separate from task progress.</caption>
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
                    <p id="taskFilterStatus" class="workspace-hint" role="status" aria-live="polite"></p>
                </section>
                <section class="workspace-card" aria-labelledby="reportTitle">
                    <div class="workspace-heading"><div><h2 id="reportTitle">Official Internship Completion Report</h2><p>Your approved completion document, when issued by the university.</p></div><span class="workspace-badge warning">Awaiting Coordinator approval</span></div>
                    <div class="report-status"><strong>No official PDF is available.</strong><p class="workspace-muted">Download and print will become available only after Coordinator approval and an authorized report is supplied.</p></div>
                    <div class="workspace-actions"><button type="button" class="workspace-button" disabled="disabled" aria-describedby="reportAccessNote">Download PDF</button><button type="button" class="workspace-button secondary" disabled="disabled" aria-describedby="reportAccessNote">Print report</button></div>
                    <p id="reportAccessNote" class="workspace-hint">Report access is unavailable in this frontend prototype. No official report is generated.</p>
                </section>
            </main>
            </div>
        </div>
    </form>
    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="<%= ResolveUrl("~/Js_project/Student/my-progress-evaluation.js?v=20261021-original-ui") %>" defer="defer"></script>
</body>
</html>
