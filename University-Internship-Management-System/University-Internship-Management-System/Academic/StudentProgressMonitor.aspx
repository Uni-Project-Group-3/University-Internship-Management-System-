<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StudentProgressMonitor.aspx.cs" Inherits="University_Internship_Management_System.Academic.StudentProgressMonitor" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <title>Student Progress Monitor | JUST UIMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/progress-monitor.css" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page monitor-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <!-- القائمة الجانبية كما هي في تصميمك -->
            <aside class="dash-sidebar" id="dashboardSidebar">
                <a href="AcademicDashboard.aspx" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text"><strong>JUST</strong><span>Internships</span></span>
                </a>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav">
                        <li><a href="AcademicDashboard.aspx"><span>Dashboard</span></a></li>
                        <li><a href="StudentProgressMonitor.aspx" class="active"><span>Student Progress Monitor</span></a></li>
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

                    <!-- البطاقات العلوية للإحصائيات -->
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

                    <!-- جدول الطلاب -->
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

        <!-- النافذة المنبثقة (Modal) لعرض تفاصيل الطالب والملاحظات -->
        <dialog id="studentDetails" class="monitor-drawer" runat="server" clientidmode="Static">
            <div class="monitor-drawer-heading">
                <div>
                    <p class="monitor-eyebrow">STUDENT OVERVIEW</p>
                    <h2><asp:Literal ID="lblDetName" runat="server" Text="Select a student" /></h2>
                    <asp:HiddenField ID="hfCurrentStudentId" runat="server" />
                </div>
                <!-- زر إغلاق النافذة باستخدام الجافاسكريبت المباشر -->
                <button type="button" class="monitor-close" onclick="document.getElementById('studentDetails').close();">&#215;</button>
            </div>

            <section class="monitor-detail-section">
                <h3>Training hours</h3>
                <p>Approved: <strong><asp:Literal ID="lblDetAppHours" runat="server" Text="0" /></strong> / <asp:Literal ID="lblDetReqHours" runat="server" Text="300" /></p>
            </section>

            <section class="monitor-detail-section">
                <h3>Academic feedback</h3>
                <label class="monitor-feedback-label">Your comments</label>
                <asp:TextBox ID="txtFeedback" runat="server" TextMode="MultiLine" Rows="4" CssClass="w-100" placeholder="Write academic feedback here..."></asp:TextBox>
                <br /><br />
                <asp:Button ID="btnSaveFeedback" runat="server" Text="Save feedback" CssClass="monitor-button" OnClick="btnSaveFeedback_Click" />
                <br />
                <asp:Label ID="lblFeedbackMsg" runat="server" ForeColor="Green"></asp:Label>
            </section>
        </dialog>
    </form>
    
    <!-- حذفنا سكريبتات الـ Demo الوهمية واحتفظنا فقط بالتنقل -->
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js") %>"></script>
</body>
</html>