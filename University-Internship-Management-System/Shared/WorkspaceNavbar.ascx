<%@ Control Language="C#" %>
<script runat="server">
    // Presentation settings only. Authentication and data access remain owned by each page.
    public string SearchInputId { get; set; } = "workspaceSearch";
    public string SearchInputType { get; set; } = "search";
    public string SearchPlaceholder { get; set; } = "Search workspace...";
    public string AccountLabel { get; set; } = "Account";
    public string AccountRole { get; set; } = "";
    public string AccountInitials { get; set; } = "U";
    public string AccountUrl { get; set; } = "~/Default.aspx";
    public string SidebarId { get; set; } = "workspaceSidebar";
</script>
<header class="topbar workspace-navbar" aria-label="Workspace toolbar">
    <button type="button" class="workspace-menu-toggle" aria-label="Open navigation menu" aria-expanded="false" aria-controls="<%= Server.HtmlEncode(SidebarId) %>">
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M3 12h18M3 18h18" /></svg>
    </button>
    <label class="topbar-search workspace-search">
        <span class="workspace-sr-only"><%= Server.HtmlEncode(SearchPlaceholder) %></span>
        <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></svg>
        <input type="<%= Server.HtmlEncode(SearchInputType) %>" id="<%= Server.HtmlEncode(SearchInputId) %>" placeholder="<%= Server.HtmlEncode(SearchPlaceholder) %>" autocomplete="off" />
    </label>
    <div class="topbar-right workspace-navbar-actions">
        <span class="workspace-notifications" role="img" aria-label="Notifications"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M10 21h4"/></svg></span>
        <a class="user-chip workspace-user-chip" href="<%= ResolveUrl(AccountUrl) %>">
            <span class="av workspace-avatar" aria-hidden="true"><%= Server.HtmlEncode(AccountInitials) %></span>
            <span class="workspace-account-text"><strong><%= Server.HtmlEncode(AccountLabel) %></strong><small><%= Server.HtmlEncode(AccountRole) %></small></span>
            <svg class="workspace-account-arrow" viewBox="0 0 24 24" aria-hidden="true"><path d="m9 5 7 7-7 7"/></svg>
        </a>
    </div>
</header>
