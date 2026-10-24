<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Import Namespace="System.Web.Security" %>
<%@ Import Namespace="University_Internship_Management_System.Shared" %>
<script runat="server">
    protected void Page_Load(object sender, System.EventArgs e)
    {
        var identity = Context.User == null ? null : Context.User.Identity as FormsIdentity;
        string role = identity == null || !identity.IsAuthenticated ? null : FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
        if (role != "student") { Response.StatusCode = identity == null || !identity.IsAuthenticated ? 401 : 403; Response.TrySkipIisCustomErrors = true; Response.End(); }
    }
</script>
<!DOCTYPE html>
<html lang="en"><head runat="server"><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Weekly Report Status | JUST Internships</title><link href="../Content/bootstrap.min.css" rel="stylesheet" /><link href="../Assets/shared/colors.css" rel="stylesheet" /><link href="../Assets/company/WeeklyReports.css?v=20261024-student-hide-notice" rel="stylesheet" /><link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" /></head>
<body class="student-weekly-page"><form id="form1" runat="server"><div class="wr-shell"><aside class="wr-sidebar"><a class="wr-brand" href="StudentDashboard.aspx"><img src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="44" height="44" /><span><strong>JUST</strong><small>Internships</small></span></a><nav aria-label="Student navigation"><a href="StudentDashboard.aspx">Dashboard</a><a href="ExploreOpportunities.aspx">Explore Opportunities</a><a href="MyApplications.aspx">My Applications</a><a href="MyTasks.aspx">My Tasks</a><a href="MyExams.aspx">My Exams</a><a href="MyProgressEvaluation.aspx">Progress &amp; Evaluation</a><a class="active" href="WeeklyReports.aspx" aria-current="page">Weekly Report Status</a><a href="Profile.aspx">My Profile</a></nav></aside>
<main class="wr-main"><header class="wr-top"><a href="StudentDashboard.aspx">University Internship Management System</a><span>Student Portal</span></header><div class="wr-content"><div id="wrNotice" class="wr-notice" role="status" aria-live="polite">Loading report status…</div><div class="wr-heading"><div><span class="wr-kicker">STUDENT WORKSPACE</span><h1>Weekly report status</h1><p>View reports submitted by your company supervisor. Reports and files are managed by the company and academic supervisor.</p></div><button id="refreshStudent" class="wr-secondary wr-hero-action" type="button">Refresh</button></div><section class="wr-card"><div id="studentReports" class="wr-report-list"><p class="wr-empty">Loading your report statuses…</p></div></section></div></main></div></form><script src="../Js_project/workflow-dialogs.js?v=20261023"></script><script src="../Js_project/Student/WeeklyReports.js"></script></body></html>
