<%@ Page Language="C#" AutoEventWireup="true" CodePage="65001" %>
<%@ Import Namespace="System.Web.Security" %>
<%@ Import Namespace="University_Internship_Management_System.Shared" %>
<script runat="server">
    protected void Page_Load(object sender, System.EventArgs e)
    {
        var identity = Context.User == null ? null : Context.User.Identity as FormsIdentity;
        string role = identity == null || !identity.IsAuthenticated ? null : FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
        if (role != "companySupervisor") { Response.StatusCode = identity == null || !identity.IsAuthenticated ? 401 : 403; Response.TrySkipIisCustomErrors = true; Response.End(); }
    }
</script>
<!DOCTYPE html>
<html lang="en"><head runat="server"><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Weekly Reports | JUST Internships</title><link href="../Content/bootstrap.min.css" rel="stylesheet" /><link href="../Assets/shared/colors.css" rel="stylesheet" /><link href="../Assets/company/WeeklyReports.css?v=20261024-hide-error-banner" rel="stylesheet" /></head>
<body class="company-weekly-page"><form id="form1" runat="server"><div class="wr-shell">
<aside class="wr-sidebar"><a class="wr-brand" href="CompanyDashboard.aspx"><img src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="44" height="44" /><span><strong>JUST</strong><small>Internships</small></span></a>
<nav aria-label="Company navigation"><a href="CompanyDashboard.aspx">Dashboard</a><a href="ManageOpportunities.aspx">Manage Opportunities</a><a href="ApplicationsReview.aspx">Applications Review</a><a href="MyTrainees.aspx">My Trainees</a><a href="ManageTasks.aspx">Manage Tasks</a><a href="ManageExams.aspx">Task Exams</a><a class="active" href="WeeklyReports.aspx" aria-current="page">Weekly Reports</a><a href="ReportsFinalEvaluation.aspx">Final Evaluation</a><a href="CompanySupervisorAccount.aspx">Profile</a></nav></aside>
<main class="wr-main"><header class="wr-top"><a href="CompanyDashboard.aspx">University Internship Management System</a><span>Company Supervisor</span></header><div class="wr-content">
<nav class="wr-breadcrumb" aria-label="Breadcrumb"><a href="CompanyDashboard.aspx">Company workspace</a><span aria-hidden="true">/</span><span aria-current="page">Weekly Reports</span></nav>
<header class="wr-heading"><div><span class="wr-kicker"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 7h1m4 0h1m-6 4h1m4 0h1M10 21v-6h4v6"/></svg>COMPANY SUPERVISOR · WEEKLY REPORTS</span><h1>Weekly Reports</h1><p>Share your trainees’ weekly progress, follow academic reviews, and respond to feedback.</p></div><a class="wr-hero-action" href="#weeklySubmit"><span aria-hidden="true">+</span>Submit a report</a></header>
<div id="wrNotice" class="wr-notice" role="status" aria-live="polite">Loading weekly report service…</div>
<section id="weeklySubmit" class="wr-card"><h2>Submit a weekly report</h2><p class="wr-muted">Reports are routed to the student’s currently assigned academic supervisor. Each training week can have one report; requested changes create a new preserved revision.</p>
<div class="wr-grid"><label>Active trainee placement<select id="placementSelect" required><option value="">Loading assigned students...</option></select></label><label>Training week<input id="weekNumber" type="number" min="1" max="60" required /></label><label>Week starts<input id="weekStart" type="date" required /></label><label>Week ends<input id="weekEnd" type="date" required /></label></div>
<label>Company performance comments <span class="wr-muted">(optional)</span><textarea id="companyComments" rows="3" maxlength="3000" placeholder="Describe the student's work and progress this week..."></textarea></label>
<label>Word report (.docx)<input id="reportFile" type="file" accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document" required /><small>Microsoft Word DOCX only · maximum 10 MiB · stored privately in Firebase Storage.</small></label>
<button id="submitReport" class="wr-primary" type="button">Submit weekly report</button></section>
<section class="wr-card"><div class="wr-card-heading"><div><h2>Report history</h2><p class="wr-muted">View revisions, academic decisions, and download previously submitted files.</p></div><button id="refreshCompany" class="wr-secondary" type="button">Refresh</button></div><div id="companyReports" class="wr-report-list"><p class="wr-empty">Loading reports…</p></div></section>
</div></main></div></form><link href="../Assets/task-workspace.css" rel="stylesheet" /><script src="../Js_project/task-dialogs.js"></script><script src="../Js_project/weekly-task-context.js"></script><script src="../Js_project/Company/WeeklyReports.js?v=20261024-pre-design-logic"></script></body></html>
