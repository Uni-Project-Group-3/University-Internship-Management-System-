<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentProgressMonitor.aspx.cs" Inherits="University_Internship_Management_System.Academic.StudentProgressMonitor" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <title>Student Progress Monitor | JUST UIMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/progress-monitor.css" rel="stylesheet" />
    <style>
        /* تحسينات التصميم للجدول والنافذة المنبثقة */
        .status-behind { background-color: #fde8e8; color: #c81e1e; padding: 4px 10px; border-radius: 6px; font-size: 0.85em; font-weight: 600; display: inline-block; }
        .status-track { background-color: #def7ec; color: #03543f; padding: 4px 10px; border-radius: 6px; font-size: 0.85em; font-weight: 600; display: inline-block; }
        
        .modern-modal { width: 700px; max-width: 95vw; padding: 0; border: none; border-radius: 12px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
        .modal-header { background: #f8f9fa; padding: 20px 24px; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center; border-radius: 12px 12px 0 0; }
        .modal-body { padding: 24px; max-height: 65vh; overflow-y: auto; background: #fafafa; }
        
        .info-card { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; margin-bottom: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
        .info-card h3 { margin-top: 0; font-size: 1.1em; color: #374151; border-bottom: 2px solid #f3f4f6; padding-bottom: 8px; margin-bottom: 12px; }
        
        .task-table { width: 100%; border-collapse: collapse; font-size: 0.9em; }
        .task-table th, .task-table td { border-bottom: 1px solid #e5e7eb; padding: 12px 8px; text-align: left; }
        .task-table th { color: #6b7280; font-weight: 600; text-transform: uppercase; font-size: 0.8em; }
        .task-table tr:last-child td { border-bottom: none; }
        .task-link { color: #2563eb; text-decoration: none; font-weight: 500; display: inline-flex; align-items: center; gap: 4px; }
        .task-link:hover { text-decoration: underline; color: #1d4ed8; }
        
        .feedback-box { width: 100%; border: 1px solid #d1d5db; border-radius: 6px; padding: 12px; font-family: inherit; resize: vertical; margin-bottom: 12px; }
        .feedback-box:focus { outline: none; border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,0.1); }
    </style>
</head>
<body class="dashboard-direct app-page monitor-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <aside class="dash-sidebar" id="dashboardSidebar">
                <a href="AcademicDashboard.aspx" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text"><strong>JUST</strong><span>Internships</span></span>
                </a>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav">
                        <li><a href="AcademicDashboard.aspx"><span>Dashboard</span></a></li>
                        <li><a href="StudentProgressMonitor.aspx" class="active"><span>Student Progress Monitor</span></a></li>
                        <li><a href="WeeklyReports.aspx"><span>Weekly Reports</span></a></li>
                    </ul>
                </nav>
            </aside>

            <div class="dash-main">
                <main class="app-content monitor-main">
                    <section class="monitor-heading">
                        <div>
                            <p class="monitor-eyebrow">INTERNSHIP OVERSIGHT</p>
                            <h1>Student Progress Monitor</h1>
                        </div>
                    </section>

                    <section class="monitor-stats">
                        <article class="monitor-stat">
                            <p>Assigned Students</p>
                            <strong><asp:Literal ID="lblAssignedStudents" runat="server" Text="0" /></strong>
                        </article>
                        <article class="monitor-stat">
                            <p>Active Internships</p>
                            <strong><asp:Literal ID="lblActiveInternships" runat="server" Text="0" /></strong>
                        </article>
                        <article class="monitor-stat">
                            <p>Completed Tasks</p>
                            <strong><asp:Literal ID="lblCompletedTasks" runat="server" Text="0" /></strong>
                        </article>
                    </section>

                    <asp:Label ID="lblMessage" runat="server" CssClass="d-block mb-3 text-danger fw-bold"></asp:Label>

                    <section class="monitor-panel">
                        <div class="monitor-panel-heading">
                            <div><h2>Your assigned students</h2></div>
                        </div>
                        <div class="monitor-table-scroll">
                            <table class="monitor-table">
                                <thead>
                                    <tr>
                                        <th>Student Name</th>
                                        <th>Major</th>
                                        <th>Approved Hours</th>
                                        <th>Task Progress</th>
                                        <th>Status</th>
                                        <th>Details</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptStudents" runat="server" OnItemCommand="rptStudents_ItemCommand">
                                        <ItemTemplate>
                                            <tr>
                                                <td><strong><%# Eval("StudentName") %></strong></td>
                                                <td><%# Eval("Major") %></td>
                                                <td><span class="monitor-chip"><%# Eval("ApprovedHours") %> / <%# Eval("RequiredHours") %></span></td>
                                                <td><%# Eval("TaskPercentage") %>%</td>
                                                <td>
                                                    <%# Convert.ToBoolean(Eval("HasDelays")) ? "<span class='status-behind'>⚠️ Behind Schedule</span>" : "<span class='status-track'>✓ On Track</span>" %>
                                                </td>
                                                <td>
                                                    <asp:LinkButton runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("StudentId") %>' Text="View Details" CssClass="monitor-button monitor-button--secondary" />
                                                </td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </section>
                </main>
            </div>
        </div>

        <!-- النافذة المنبثقة (Modal) بالتصميم الجديد -->
        <dialog id="studentDetails" class="modern-modal" runat="server" clientidmode="Static">
            <div class="modal-header">
                <div>
                    <h2 style="margin:0; font-size:1.4em; color:#111827;"><asp:Literal ID="lblDetName" runat="server" Text="Select a student" /></h2>
                    <span style="font-size:0.85em; color:#6b7280;">Student Overview & Progress</span>
                    <asp:HiddenField ID="hfCurrentStudentId" runat="server" />
                </div>
                <button type="button" class="monitor-close" onclick="document.getElementById('studentDetails').close();" style="background:none; border:none; font-size:1.5em; cursor:pointer; color:#6b7280;">&#215;</button>
            </div>

            <div class="modal-body">
                
                <div class="info-card">
                    <h3>Training Hours Progress</h3>
                    <p style="margin:0; font-size:1.1em;">
                        Approved: <strong style="color:#2563eb;"><asp:Literal ID="lblDetAppHours" runat="server" Text="0" /></strong> / <asp:Literal ID="lblDetReqHours" runat="server" Text="300" /> hours
                    </p>
                </div>

                <div class="info-card">
                    <h3>Tasks & Submissions</h3>
                    <asp:Repeater ID="rptTasks" runat="server">
                        <HeaderTemplate>
                            <table class="task-table">
                                <tr>
                                    <th>Task Title</th>
                                    <th>Status</th>
                                    <th>Score</th>
                                    <th>Submission</th>
                                </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                                <tr>
                                    <td style="font-weight:500; color:#374151;"><%# Eval("Title") %></td>
                                    <td><%# Eval("Status") %></td>
                                    <td><%# Eval("Score") %></td>
                                    <td>
                                        <%# string.IsNullOrEmpty(Eval("SubmissionLink").ToString()) ? "<span style='color:#9ca3af; font-size:0.9em;'>No file attached</span>" : $"<a href='{Eval("SubmissionLink")}' target='_blank' class='task-link'>View File ↗</a>" %>
                                    </td>
                                </tr>
                        </ItemTemplate>
                        <FooterTemplate>
                            </table>
                        </FooterTemplate>
                    </asp:Repeater>
                    <asp:Label ID="lblNoTasks" runat="server" Text="No tasks assigned to this student yet." Visible="false" ForeColor="#6b7280" style="display:block; padding:10px 0;"></asp:Label>
                </div>

                <div class="info-card">
                    <h3>Company Evaluation</h3>
                    <p style="margin:0; color:#4b5563;"><asp:Literal ID="lblCompanyEval" runat="server" Text="No evaluations submitted by the company supervisor yet." /></p>
                </div>

                <div class="info-card" style="margin-bottom: 0;">
                    <h3>Academic Feedback</h3>
                    <label style="display:block; margin-bottom:8px; font-size:0.9em; color:#4b5563;">Send your comments and guidance to the student:</label>
                    <asp:TextBox ID="txtFeedback" runat="server" TextMode="MultiLine" Rows="3" CssClass="feedback-box" placeholder="Write your feedback here..."></asp:TextBox>
                    
                    <div style="display:flex; align-items:center; gap:15px;">
                        <asp:Button ID="btnSaveFeedback" runat="server" Text="Save Feedback" CssClass="monitor-button" OnClick="btnSaveFeedback_Click" style="background:#2563eb; color:white; border:none; padding:8px 16px; border-radius:6px; cursor:pointer;" />
                        <asp:Label ID="lblFeedbackMsg" runat="server" Font-Bold="true"></asp:Label>
                    </div>
                </div>

            </div>
        </dialog>
    </form>
    
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js") %>"></script>
</body>
</html>