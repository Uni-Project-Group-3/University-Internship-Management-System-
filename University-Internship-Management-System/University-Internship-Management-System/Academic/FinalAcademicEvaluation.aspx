<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="FinalAcademicEvaluation.aspx.cs" Inherits="University_Internship_Management_System.Academic.FinalAcademicEvaluation" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Final Academic Evaluation | JUST UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/academic/final-academic-evaluation.css" rel="stylesheet" />
</head>
<body class="dashboard-direct app-page evaluation-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <!-- القائمة الجانبية -->
            <aside class="dash-sidebar" id="dashboardSidebar">
                <a href="AcademicDashboard.aspx" class="dash-brand">
                    <span class="dash-brand-logo">J</span>
                    <span class="dash-brand-text"><strong>JUST</strong><span>Internships</span></span>
                </a>
                <nav aria-label="Workspace navigation">
                    <ul class="dash-side-nav">
                        <li><a href="AcademicDashboard.aspx"><span>Dashboard</span></a></li>
                        <li><a href="StudentProgressMonitor.aspx"><span>Student Progress Monitor</span></a></li>
                        <li><a href="FinalAcademicEvaluation.aspx" class="active"><span>Final Academic Evaluation</span></a></li>
                    </ul>
                </nav>
            </aside>

            <div class="dash-main">
                <main class="app-content evaluation-main">
                    <section class="evaluation-heading">
                        <div>
                            <p class="evaluation-eyebrow">ACADEMIC ASSESSMENT</p>
                            <h1>Final Academic Evaluation</h1>
                        </div>
                    </section>

                    <!-- البطاقات الإحصائية -->
                    <section class="evaluation-stats">
                        <article>
                            <div><span>My Students</span></div>
                            <strong><asp:Literal ID="lblTotalStudents" runat="server" Text="0" /></strong>
                        </article>
                        <article>
                            <div><span>Pending Evaluations</span></div>
                            <strong><asp:Literal ID="lblPending" runat="server" Text="0" /></strong>
                        </article>
                        <article>
                            <div><span>Submitted Evaluations</span></div>
                            <strong><asp:Literal ID="lblSubmitted" runat="server" Text="0" /></strong>
                        </article>
                    </section>

                    <asp:Label ID="lblMessage" runat="server" CssClass="d-block mb-3 text-danger fw-bold"></asp:Label>

                    <!-- جدول الطلاب والتقييمات -->
                    <section class="evaluation-panel">
                        <div class="evaluation-panel-heading">
                            <div><h2>Student evaluations</h2></div>
                        </div>
                        
                        <div class="evaluation-table-wrap">
                            <table>
                                <thead>
                                    <tr>
                                        <th>Student Name</th>
                                        <th>University ID</th>
                                        <th>Major</th>
                                        <th>Evaluation Status</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptEvaluations" runat="server" OnItemCommand="rptEvaluations_ItemCommand">
                                        <ItemTemplate>
                                            <tr>
                                                <td><strong><%# Eval("StudentName") %></strong></td>
                                                <td><%# Eval("StudentId") %></td>
                                                <td><%# Eval("Major") %></td>
                                                <td>
                                                    <span class="evaluation-chip" style='<%# Eval("StatusStyle") %>'>
                                                        <%# Eval("Status") %>
                                                    </span>
                                                </td>
                                                <td>
                                                    <asp:LinkButton runat="server" CommandName="Evaluate" CommandArgument='<%# Eval("StudentId") %>' Text="Evaluate Student" CssClass="evaluation-text-button" />
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

        <!-- نافذة التقييم المنبثقة (Modal) -->
        <dialog id="evaluationDialog" class="evaluation-dialog" runat="server" clientidmode="Static">
            <header class="evaluation-dialog-header">
                <div>
                    <p class="evaluation-eyebrow">FINAL ASSESSMENT</p>
                    <h2>Evaluate Student: <asp:Literal ID="lblEvalName" runat="server" /></h2>
                    <asp:HiddenField ID="hfEvalStudentId" runat="server" />
                </div>
                <button type="button" class="evaluation-close" onclick="document.getElementById('evaluationDialog').close();">&times;</button>
            </header>
            
            <div class="evaluation-dialog-body" style="padding: 20px;">
                <section class="evaluation-detail-section mb-4">
                    <h3>Final Decision</h3>
                    <asp:DropDownList ID="ddlFinalDecision" runat="server" CssClass="evaluation-field" style="width:100%; padding:8px;">
                        <asp:ListItem Text="-- Select Decision --" Value="" />
                        <asp:ListItem Text="Pass (تم الاجتياز)" Value="Pass" />
                        <asp:ListItem Text="Fail (لم يجتز)" Value="Fail" />
                        <asp:ListItem Text="Incomplete (غير مكتمل)" Value="Incomplete" />
                    </asp:DropDownList>
                </section>

                <section class="evaluation-detail-section">
                    <h3>Academic Comments</h3>
                    <asp:TextBox ID="txtEvalComments" runat="server" TextMode="MultiLine" Rows="4" CssClass="evaluation-field" style="width:100%;" placeholder="Write final academic remarks for the coordinator..."></asp:TextBox>
                </section>
                
                <asp:Label ID="lblModalMsg" runat="server" ForeColor="Green" CssClass="d-block mt-2"></asp:Label>
            </div>

            <footer class="evaluation-dialog-footer" style="display:flex; justify-content:flex-end; padding:20px; gap:10px;">
                <button type="button" class="evaluation-button evaluation-button--secondary" onclick="document.getElementById('evaluationDialog').close();">Cancel</button>
                <asp:Button ID="btnSubmitEval" runat="server" Text="Submit to Coordinator" CssClass="evaluation-button" OnClick="btnSubmitEval_Click" />
            </footer>
        </dialog>
    </form>
    
    <!-- سكريبت التنقل الأساسي فقط (بدون أصفار الـ Demo) -->
    <script src="<%= ResolveUrl("~/Js_project/Public/dashboard-navigation.js") %>"></script>
</body>
</html>