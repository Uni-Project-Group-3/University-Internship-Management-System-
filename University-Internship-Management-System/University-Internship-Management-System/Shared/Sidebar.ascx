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
