<%@ Control Language="C#" Inherits="University_Internship_Management_System.Shared.RoleSidebarBase" %>
<!-- Role-specific static navigation: edit this file to change this role's menu. -->
<aside class="sidebar dash-sidebar wr-sidebar uims-fixed-sidebar" id="<%= SidebarId %>" data-uims-sidebar-role="academic" aria-label="Academic Supervisor navigation">
    <a class="uims-side-brand" href="<%= ResolveUrl("~/Academic/AcademicDashboard.aspx") %>">
        <img class="uims-side-logo" src="<%= ResolveUrl("~/Assets/shared/images/JUST-Logo-01.png") %>" width="43" height="43" alt="JUST logo" />
        <span class="uims-side-brandtext"><strong>JUST</strong><small>Internships</small></span>
    </a>
    <button type="button" class="sidebar-close legacy-sidebar-close dash-sidebar-close uims-sidebar-close" data-sidebar-close aria-label="Close navigation">&times;</button>
    <div class="uims-side-role">Academic Supervisor</div>
    <nav class="uims-side-navigation" aria-label="Academic Supervisor pages">
        <ul class="uims-side-links">
            <li><a class="uims-side-link<%= ActiveClass("~/Academic/AcademicDashboard.aspx") %>" href="<%= ResolveUrl("~/Academic/AcademicDashboard.aspx") %>"<%= ActiveAria("~/Academic/AcademicDashboard.aspx") %>><svg class="uims-side-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
            <li><a class="uims-side-link<%= ActiveClass("~/Academic/StudentProgressMonitor.aspx") %>" href="<%= ResolveUrl("~/Academic/StudentProgressMonitor.aspx") %>"<%= ActiveAria("~/Academic/StudentProgressMonitor.aspx") %>><svg class="uims-side-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 20V5m0 15h16M8 16v-5m5 5V7m5 9v-8"/></svg><span>Student Progress Monitor</span></a></li>
            <li><a class="uims-side-link<%= ActiveClass("~/Academic/WeeklyReports.aspx") %>" href="<%= ResolveUrl("~/Academic/WeeklyReports.aspx") %>"<%= ActiveAria("~/Academic/WeeklyReports.aspx") %>><svg class="uims-side-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M7 3h8l4 4v14H7zM15 3v5h4M10 13h6m-6 4h6"/></svg><span>Weekly Reports</span></a></li>
            <li><a class="uims-side-link<%= ActiveClass("~/Academic/FinalAcademicEvaluation.aspx") %>" href="<%= ResolveUrl("~/Academic/FinalAcademicEvaluation.aspx") %>"<%= ActiveAria("~/Academic/FinalAcademicEvaluation.aspx") %>><svg class="uims-side-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Final Academic Evaluation</span></a></li>
        </ul>
    </nav>
    <div class="uims-side-footer">
        <a class="uims-side-link<%= ActiveClass("~/Academic/AcademicSupervisorAccount.aspx") %>" href="<%= ResolveUrl("~/Academic/AcademicSupervisorAccount.aspx") %>"<%= ActiveAria("~/Academic/AcademicSupervisorAccount.aspx") %>><svg class="uims-side-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></svg><span>My Account</span></a>
        <a class="uims-side-link" href="<%= ResolveUrl("~/Contact.aspx") %>"><svg class="uims-side-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3m0 3v.2"/></svg><span>Help &amp; Contact</span></a>
    </div>
</aside>
