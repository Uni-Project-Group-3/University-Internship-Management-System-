<%@ Page Language="C#" AutoEventWireup="true" CodePage="65001" %>
<%@ Register Src="~/Shared/CompanySidebar.ascx" TagPrefix="uims" TagName="CompanySidebar" %>
<!DOCTYPE html>
<html lang="en"><head runat="server"><meta charset="utf-8"/><base href="<%= ResolveUrl("~/") %>" /><meta name="viewport" content="width=device-width, initial-scale=1"/><title>Final training reports | JUST Internships</title><link href="Assets/shared/colors.css" rel="stylesheet"/><link href="Assets/final-reports.css?v=20261024-one-page" rel="stylesheet"/>    <link href="Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
<asp:PlaceHolder ID="CompanyWorkspaceStyles" runat="server" Visible="false"><link href="Assets/shared/workspace-identity.css?v=20261024-company-shell" rel="stylesheet"/><link href="Assets/company/final-reports-shell.css?v=20261024-hero-layout" rel="stylesheet"/></asp:PlaceHolder>
</head>
<body id="pageBody" runat="server"><form runat="server"><div class="app-layout final-reports-layout">
<asp:PlaceHolder ID="CompanySidebarSlot" runat="server" Visible="false"><uims:CompanySidebar ID="companySidebar" runat="server" SidebarId="sidebar"/></asp:PlaceHolder>
<div class="main final-reports-main"><asp:PlaceHolder ID="CompanyTopbarSlot" runat="server" Visible="false"><header class="topbar company-report-topbar"><div class="topbar-actions ms-auto"><span class="company-report-avatar" aria-hidden="true">CS</span><span class="company-report-user"><strong>Company Supervisor</strong><small>Internship workspace</small></span><a class="company-report-account" href="Company/CompanySupervisorAccount.aspx" aria-label="Company Supervisor account"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg></a></div></header></asp:PlaceHolder>
<main class="final-workspace"><header class="final-report-hero"><div class="final-report-hero-copy"><span class="final-report-hero-kicker">JUST / COMPANY SUPERVISOR WORKSPACE</span><h1>Final reports &amp; approvals</h1><p>Review authorized trainee reports and track final approval progress.</p></div></header><p id="finalNotice" role="status">Loading authorized records...</p><section id="finalSummary" aria-label="Training completion summary"></section><div class="report-tools"><label>Search student name or ID<input id="finalSearch" type="search"/></label><label>Status<select id="finalFilter"><option value="">All statuses</option><option value="draft">Draft</option><option value="pendingAcademicReview">Academic review</option><option value="companyRevisionRequested">Company revision requested</option><option value="pendingAdminReview">Admin review</option><option value="academicRevisionRequested">Academic corrections</option><option value="completed">Completed</option></select></label><button type="button" id="finalReload">Refresh records</button></div><section id="finalRecords" aria-live="polite"></section></main></div></div></form><script src="Js_project/firebase-client.js?v=20261024-final-tab-session"></script><script src="Js_project/final-reports.js?v=20261024-hide-success-notice"></script><script src="Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script><script runat="server">
protected void Page_Init(object sender, EventArgs e)
{
    var isCompanySupervisor = Context.User != null && Context.User.Identity != null && Context.User.Identity.IsAuthenticated && Context.User.IsInRole("companySupervisor");
    CompanyWorkspaceStyles.Visible = isCompanySupervisor;
    CompanySidebarSlot.Visible = isCompanySupervisor;
    CompanyTopbarSlot.Visible = isCompanySupervisor;
    if (isCompanySupervisor) pageBody.Attributes["class"] = "company-report-shell";
}
</script></body></html>
