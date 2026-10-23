<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Web.Security" %>
<%@ Import Namespace="University_Internship_Management_System.Shared" %>
<script runat="server">
    protected void Page_Load(object sender, System.EventArgs e)
    {
        var identity = Context.User == null ? null : Context.User.Identity as FormsIdentity;
        string role = identity == null || !identity.IsAuthenticated ? null : FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
        if (role != "academicSupervisor") { Response.StatusCode = identity == null || !identity.IsAuthenticated ? 401 : 403; Response.TrySkipIisCustomErrors = true; Response.End(); }
    }
</script>
<!DOCTYPE html>
<html lang="en"><head runat="server"><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Weekly Reports | JUST Internships</title><link href="../Content/bootstrap.min.css" rel="stylesheet" /><link href="../Assets/shared/colors.css" rel="stylesheet" /><link href="../Assets/company/WeeklyReports.css" rel="stylesheet" /><link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" /></head>
<body><form id="form1" runat="server"><div class="wr-shell"><aside class="wr-sidebar"><a class="wr-brand" href="AcademicDashboard.aspx"><img src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="44" height="44" /><span><strong>JUST</strong><small>Internships</small></span></a><nav aria-label="Academic navigation"><a href="AcademicDashboard.aspx">Dashboard</a><a href="StudentProgressMonitor.aspx">Student Progress</a><a class="active" href="WeeklyReports.aspx" aria-current="page">Weekly Reports</a><a href="FinalAcademicEvaluation.aspx">Final Academic Evaluation</a><a href="AcademicSupervisorAccount.aspx">My Account</a></nav></aside>
<main class="wr-main"><header class="wr-top"><a href="AcademicDashboard.aspx">University Internship Management System</a><span>Academic Supervisor</span></header><div class="wr-content"><div id="wrNotice" class="wr-notice" role="status" aria-live="polite">Loading assigned reports…</div><div class="wr-heading"><div><span class="wr-kicker">ACADEMIC WORKSPACE</span><h1>Assigned students and weekly reports</h1><p>Review company reports for currently assigned students and monitor their training progress.</p></div><button id="refreshAcademic" class="wr-secondary" type="button">Refresh</button></div>
<div class="wr-academic-layout"><section class="wr-card"><h2>Assigned students</h2><div id="academicStudents" class="wr-report-list"><p class="wr-empty">Loading current assignments…</p></div><h2>Weekly reports</h2><div id="academicReports" class="wr-report-list"><p class="wr-empty">Loading assigned reports…</p></div></section><section class="wr-card" id="reportDetail"><h2>Report review</h2><p class="wr-muted">Select a report to inspect its immutable revisions, review history, task progress, exam scores, and approved attendance hours.</p><div id="academicDetail"></div></section></div>
</div></main></div></form><script src="../Js_project/workflow-dialogs.js?v=20261023"></script><script src="../Js_project/Academic/WeeklyReports.js"></script></body></html>
