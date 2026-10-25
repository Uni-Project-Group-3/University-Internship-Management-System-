<%@ Page Language="C#" Async="true" AutoEventWireup="true" ViewStateEncryptionMode="Always" CodeBehind="CoordinatorAccountProfile.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.CoordinatorAccountProfile" %>
<%@ Register Src="~/Shared/CoordinatorSidebar.ascx" TagPrefix="shared" TagName="CoordinatorSidebar" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Coordinator Account Profile | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/coordinator-account-profile.css?v=20261024-account-hero" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/account-form-design.css?v=20261024-hide-firebase-badge" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/role-sidebars.css?v=20261023" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261025-notifications" defer></script>
</head>
<body class="coordinator-banner app-page uims-workspace coordinator-profile" data-demo-role="admin">
    <form id="form1" runat="server" novalidate="novalidate">
        <div class="app-layout">
            <shared:CoordinatorSidebar ID="RoleNavigation" runat="server" SidebarId="sidebar" />
            <div class="main"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">University Internship Coordinator</span></span></span></div></header>
            <main class="app-content">
                <header class="workspace-header">
                    <span class="workspace-eyebrow">University internship coordinator / Account</span>
                    <h1>Coordinator Account Profile</h1>
                    <p class="workspace-muted">View and manage your account information.</p>
                </header>
                <p class="profile-demo-note" style="display:none"><strong>Live profile.</strong> <asp:Literal ID="AccountConnection" runat="server" Mode="Encode" /></p>
                <div class="account-profile-layout">
                <section class="workspace-card profile-overview" aria-labelledby="profileOverviewTitle">
                    <div class="profile-heading">
                        <span class="profile-avatar" aria-hidden="true"><span data-uims-profile="initials"><asp:Literal ID="ProfileInitials" runat="server" Mode="Encode" EnableViewState="false" /></span></span>
                        <div class="profile-identity">
                            <h2 id="profileOverviewTitle"><span data-uims-profile="name"><asp:Literal ID="ProfileName" runat="server" Mode="Encode" EnableViewState="false" /></span></h2>
                            <p>University Internship Coordinator</p>
                            <span class="profile-email"><span data-uims-profile="email"><asp:Literal ID="OverviewEmail" runat="server" Mode="Encode" EnableViewState="false" /></span></span>
                            <span class="profile-university"><asp:Literal ID="UniversitySummary" runat="server" Mode="Encode" /></span>
                        </div>
                    </div>
                    <div class="profile-overview-actions">
                        <span class="workspace-badge">Firebase account</span>
                        <asp:Button ID="EditProfile" runat="server" ClientIDMode="Static" Text="Edit Profile" CssClass="workspace-button" OnClick="EditProfile_Click" CausesValidation="false" Visible="false" />
                    </div>
                </section>
                <div class="account-profile-details">
                <div id="profileMessage" class="workspace-feedback profile-message" role="status" aria-live="polite"><asp:Literal ID="ProfileMessage" runat="server" Mode="Encode" /></div>
                <div class="profile-columns">
                    <section class="workspace-card" aria-labelledby="profileTitle">
                        <div class="workspace-heading"><div><h2 id="profileTitle">Personal information</h2><p><asp:Literal ID="EditingHint" runat="server" Mode="Encode" EnableViewState="false" /></p></div><asp:Label ID="EditingStatus" runat="server" CssClass="workspace-badge" EnableViewState="false" /></div>
                        <asp:Panel ID="ReadProfile" runat="server">
                            <dl class="profile-values">
                                <div><dt>Full name</dt><dd><span data-uims-profile="name"><asp:Literal ID="DisplayName" runat="server" Mode="Encode" EnableViewState="false" /></span></dd></div>
                                <div><dt>Phone number</dt><dd><span data-uims-profile="phone"><asp:Literal ID="DisplayPhone" runat="server" Mode="Encode" EnableViewState="false" /></span></dd></div>
                                <div><dt>Account email <span class="profile-protected-label">Protected</span></dt><dd><span data-uims-profile="email"><asp:Literal ID="DisplayEmail" runat="server" Mode="Encode" EnableViewState="false" /></span></dd></div>
                                <div><dt>Office location</dt><dd><span data-uims-profile="office"><asp:Literal ID="DisplayOffice" runat="server" Mode="Encode" EnableViewState="false" /></span></dd></div>
                            </dl>
                        </asp:Panel>
                        <div id="profileEditDialog" class="profile-edit-form" aria-labelledby="profileTitle"><asp:Panel ID="EditProfileFields" runat="server" Visible="false">
                            <div id="profileValidation" class="workspace-error profile-validation" role="alert"><asp:Literal ID="ValidationError" runat="server" Mode="Encode" /></div>
                            <div class="profile-fields">
                                <div class="workspace-field">
                                    <asp:Label runat="server" AssociatedControlID="FullName" CssClass="workspace-label" Text="Full name (required)" />
                                    <asp:TextBox ID="FullName" runat="server" ClientIDMode="Static" CssClass="workspace-input" MaxLength="80" ReadOnly="true" autocomplete="name" aria-describedby="profileValidation" />
                                </div>
                                <div class="workspace-field">
                                    <asp:Label runat="server" AssociatedControlID="Phone" CssClass="workspace-label" Text="Phone number (optional)" />
                                    <asp:TextBox ID="Phone" runat="server" ClientIDMode="Static" CssClass="workspace-input" TextMode="Phone" MaxLength="25" ReadOnly="true" autocomplete="tel" aria-describedby="profileValidation" />
                                </div>
                            </div>
                            <div class="workspace-field">
                                <asp:Label runat="server" AssociatedControlID="Office" CssClass="workspace-label" Text="Office location (optional)" />
                                <asp:TextBox ID="Office" runat="server" ClientIDMode="Static" CssClass="workspace-input" MaxLength="100" ReadOnly="true" aria-describedby="officeHint profileValidation" />
                                <span id="officeHint" class="workspace-hint">Up to 100 characters.</span>
                            </div>
                            <div class="profile-protected-email"><span class="workspace-label">Account email &middot; read-only</span><span><span data-uims-profile="email"><asp:Literal ID="EditEmail" runat="server" Mode="Encode" EnableViewState="false" /></span></span></div>
                            <div class="workspace-actions profile-edit-actions">
                                <asp:Button ID="SaveProfile" runat="server" ClientIDMode="Static" Text="Save Changes" CssClass="workspace-button" OnClick="SaveProfile_Click" Visible="false" />
                                <asp:Button ID="CancelEdit" runat="server" ClientIDMode="Static" Text="Cancel" CssClass="workspace-button secondary" OnClick="CancelEdit_Click" CausesValidation="false" Visible="false" formnovalidate="formnovalidate" />
                            </div>
                        </asp:Panel></div>
                    </section>
                    <aside class="workspace-card profile-account-card">
                        <section aria-labelledby="accountTitle">
                            <h2 id="accountTitle">University &amp; role</h2>
                            <p class="workspace-hint">Protected account information</p>
                            <dl class="account-details">
                                <div><dt>University</dt><dd><asp:Literal ID="UniversityName" runat="server" Mode="Encode" EnableViewState="false" /></dd></div>
                                <div><dt>College / department</dt><dd><asp:Literal ID="CollegeDepartment" runat="server" Mode="Encode" EnableViewState="false" /></dd></div>
                                <div><dt>Role</dt><dd>University Internship Coordinator</dd></div>
                                <div><dt>Coordinator ID</dt><dd><asp:Literal ID="CoordinatorId" runat="server" Mode="Encode" EnableViewState="false" /></dd></div>
                            </dl>
                            <p class="workspace-hint profile-reference">University and coordinator details come from your account record.</p>
                        </section>
                    </aside>
                </div>
                </div>
                </div>
            </main>
            </div>
        </div>
    </form>
    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="<%= ResolveUrl("~/Js_project/Coordinator/coordinator-account-profile.js?v=20261023-live-university") %>" defer="defer"></script>
</body>
</html>
