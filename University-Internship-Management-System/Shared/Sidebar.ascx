<%@ Control Language="C#" CodeBehind="Sidebar.ascx.cs" Inherits="University_Internship_Management_System.Shared.Sidebar" %>
<aside class="app-sidebar" aria-label="User sidebar">
    <a class="app-sidebar__brand" runat="server" href="~/Default.aspx" aria-label="JUST Internships home">
        <span class="app-sidebar__mark" aria-hidden="true">J</span>
        <span><strong>JUST</strong><span class="app-sidebar__brand-subtitle">Internships</span></span>
    </a>
    <p class="app-sidebar__section"><asp:Literal ID="RoleLabel" runat="server" Mode="Encode" /> workspace</p>
    <nav class="app-sidebar__nav" aria-label="Workspace navigation">
        <asp:HyperLink ID="HomeLink" runat="server" CssClass="app-sidebar__link">
            <svg class="app-sidebar__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M3 10 12 3l9 7v10a1 1 0 0 1-1 1h-5v-8H9v8H4a1 1 0 0 1-1-1Z" /></svg>
            <span>Dashboard</span>
        </asp:HyperLink>
        <asp:HyperLink ID="ProgressMonitorLink" runat="server" NavigateUrl="~/Academic/StudentProgressMonitor.aspx" CssClass="app-sidebar__link" Visible="false">
            <svg class="app-sidebar__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M8 16v-4m4 4V8m4 8v-6" /></svg>
            <span>Student Progress</span>
        </asp:HyperLink>
        <asp:HyperLink ID="ManageCompaniesLink" runat="server" NavigateUrl="~/Coordinator/ManageCompanies.aspx" CssClass="app-sidebar__link" Visible="false">
            <svg class="app-sidebar__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M4 21V4h11v17M15 9h5v12M1 21h22M8 8h3m-3 4h3m-3 4h3" /></svg>
            <span>Manage Companies</span>
        </asp:HyperLink>
        <asp:HyperLink ID="PlacementsLink" runat="server" NavigateUrl="~/Coordinator/MonitorPlacementsProgress.aspx" CssClass="app-sidebar__link" Visible="false">
            <svg class="app-sidebar__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><rect x="3" y="4" width="18" height="16" rx="2" /><path d="M7 15v-3m5 3V8m5 7v-5" /></svg>
            <span>Monitor Placements &amp; Progress</span>
        </asp:HyperLink>
        <asp:HyperLink ID="FinalEvaluationLink" runat="server" NavigateUrl="~/Academic/FinalAcademicEvaluation.aspx" CssClass="app-sidebar__link" Visible="false">
            <svg class="app-sidebar__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2" /><path d="m8 12 3 3 5-6" /></svg>
            <span>Final Academic Evaluation</span>
        </asp:HyperLink>
        <asp:HyperLink ID="ManageStudentsLink" runat="server" NavigateUrl="~/Coordinator/ManageStudents.aspx" CssClass="app-sidebar__link" Visible="false"><svg class="app-sidebar__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><circle cx="9" cy="8" r="3"/><path d="M3 21v-4a6 6 0 0 1 12 0v4m1-16a3 3 0 0 1 0 6m2 3a5 5 0 0 1 3 5"/></svg><span>Manage Students</span></asp:HyperLink>
    </nav>
    <div class="app-sidebar__footer">
        <div class="app-sidebar__note">
            <span class="app-sidebar__note-label">YOUR NEXT CHAPTER</span>
            <p>Build your future<br />with real experience.</p>
        </div>
        <a class="app-sidebar__link" runat="server" href="~/Default.aspx">
            <span aria-hidden="true">&#8592;</span><span>Back to home</span>
        </a>
    </div>
</aside>
