<%@ Page Language="C#" AutoEventWireup="true" ViewStateEncryptionMode="Always" CodeBehind="CoordinatorAccountProfile.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.CoordinatorAccountProfile" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Coordinator Account Profile | UIMS</title>
    <link runat="server" href="~/Content/bootstrap.min.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/coordinator-account-profile.css?v=20261021-original-ui" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
</head>
<body class="app-page uims-workspace coordinator-profile" data-demo-role="admin">
    <form id="form1" runat="server" novalidate="novalidate">
        <div class="app-layout">
            <aside class="sidebar" id="sidebar" aria-label="Coordinator navigation">
    <button type="button" class="sidebar-close" aria-label="Close navigation"><i class="bi bi-x-lg" aria-hidden="true"></i></button>
    <a class="brand" href="CoordinatorDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48"/><span class="brand-text"><strong>JUST</strong><span>Internships</span></span></a>
    <ul class="side-nav"><li><a href="CoordinatorDashboard.aspx"><i class="bi bi-house-door" aria-hidden="true"></i><span>Dashboard</span></a></li>
<li><a href="ManageStudents.aspx"><i class="bi bi-mortarboard" aria-hidden="true"></i><span>Manage Students</span></a></li>
<li><a href="ManageCompanies.aspx"><i class="bi bi-building" aria-hidden="true"></i><span>Manage Companies</span></a></li>
<li><a href='<%= ResolveUrl("~/Coordinator/Manage_Academic_Supervisor.aspx") %>'><i class="bi bi-person-badge" aria-hidden="true"></i><span>Manage Academic Supervisors</span></a></li>
<li><a href="Assign_Supervisors.aspx"><i class="bi bi-diagram-3" aria-hidden="true"></i><span>Assign Academic Supervisors</span></a></li>
<li><a href="MonitorPlacementsProgress.aspx"><i class="bi bi-activity" aria-hidden="true"></i><span>Monitor Placements &amp; Progress</span></a></li>
<li><a href="FinalCompletionReview.aspx"><i class="bi bi-patch-check" aria-hidden="true"></i><span>Final Completion Review</span></a></li>
<li><a href="ContactMessages.aspx"><i class="bi bi-envelope" aria-hidden="true"></i><span>Contact Messages</span></a></li>
<li><a href="CoordinatorAccountProfile.aspx" class="active" aria-current="page"><i class="bi bi-person-circle" aria-hidden="true"></i><span>Coordinator Account</span></a></li></ul>
    <div class="side-divider"></div><ul class="side-nav"><li><a href="../Contact.aspx"><i class="bi bi-question-circle" aria-hidden="true"></i><span>Help &amp; contact</span></a></li><li><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right" aria-hidden="true"></i><span>Logout</span></asp:LinkButton></li></ul>
</aside><div class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main"><header class="topbar"><button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list" aria-hidden="true"></i></button><span class="topbar-label">University Internship Management System</span><div class="topbar-actions ms-auto"><span class="avatar" aria-hidden="true">U</span><span class="user-chip"><span class="name"><asp:Literal ID="NavName" runat="server" Mode="Encode"/><span class="role">UIMS workspace · frontend preview</span></span></span></div></header>
            <main class="app-content">
                <header class="workspace-header">
                    <span class="workspace-eyebrow">University internship coordinator / Account</span>
                    <h1>Coordinator Account Profile</h1>
                    <p class="workspace-muted">View and manage your account information.</p>
                </header>
                <p class="profile-demo-note"><strong>Demo profile.</strong> This is fictional account information. Changes update this page's preview only; nothing is saved to Firebase.</p>
                <section class="workspace-card profile-overview" aria-labelledby="profileOverviewTitle">
                    <div class="profile-heading">
                        <span class="profile-avatar" aria-hidden="true"><span data-uims-profile="initials"><asp:Literal ID="ProfileInitials" runat="server" Mode="Encode" EnableViewState="false" /></span></span>
                        <div class="profile-identity">
                            <h2 id="profileOverviewTitle"><span data-uims-profile="name"><asp:Literal ID="ProfileName" runat="server" Mode="Encode" EnableViewState="false" /></span></h2>
                            <p>University Internship Coordinator</p>
                            <span class="profile-email"><span data-uims-profile="email"><asp:Literal ID="OverviewEmail" runat="server" Mode="Encode" EnableViewState="false" /></span></span>
                            <span class="profile-university">University not linked · demonstration account</span>
                        </div>
                    </div>
                    <div class="profile-overview-actions">
                        <span class="workspace-badge">Demo account</span>
                        <asp:Button ID="EditProfile" runat="server" ClientIDMode="Static" Text="Edit Profile" CssClass="workspace-button" OnClick="EditProfile_Click" CausesValidation="false" />
                    </div>
                </section>
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
                        <dialog id="profileEditDialog" class="workspace-dialog profile-dialog" aria-labelledby="editDialogTitle"><div class="workspace-heading"><h2 id="editDialogTitle">Edit personal information</h2><button type="button" class="popup-close" data-profile-cancel aria-label="Close edit profile">×</button></div><asp:Panel ID="EditProfileFields" runat="server" Visible="false">
                            <div id="profileValidation" class="workspace-error profile-validation" role="alert"><asp:Literal ID="ValidationError" runat="server" Mode="Encode" /></div>
                            <div class="profile-fields">
                                <div class="workspace-field">
                                    <asp:Label runat="server" AssociatedControlID="FullName" CssClass="workspace-label" Text="Full name (required)" />
                                    <asp:TextBox ID="FullName" runat="server" ClientIDMode="Static" CssClass="workspace-input" MaxLength="80" ReadOnly="true" autocomplete="name" aria-describedby="nameHint profileValidation" />
                                    <span id="nameHint" class="workspace-hint">2–80 characters. Your account identity stays the same.</span>
                                </div>
                                <div class="workspace-field">
                                    <asp:Label runat="server" AssociatedControlID="Phone" CssClass="workspace-label" Text="Phone number (optional)" />
                                    <asp:TextBox ID="Phone" runat="server" ClientIDMode="Static" CssClass="workspace-input" TextMode="Phone" MaxLength="25" ReadOnly="true" autocomplete="tel" aria-describedby="phoneHint profileValidation" />
                                    <span id="phoneHint" class="workspace-hint">At least 7 digits; up to 25 characters including +, spaces, parentheses and hyphens.</span>
                                </div>
                            </div>
                            <div class="workspace-field">
                                <asp:Label runat="server" AssociatedControlID="Office" CssClass="workspace-label" Text="Office location (optional)" />
                                <asp:TextBox ID="Office" runat="server" ClientIDMode="Static" CssClass="workspace-input" MaxLength="100" ReadOnly="true" aria-describedby="officeHint profileValidation" />
                                <span id="officeHint" class="workspace-hint">Up to 100 characters.</span>
                            </div>
                            <div class="profile-protected-email"><span class="workspace-label">Account email · read-only</span><span><span data-uims-profile="email"><asp:Literal ID="EditEmail" runat="server" Mode="Encode" EnableViewState="false" /></span></span></div>
                            <p class="workspace-hint">Save Changes updates this demonstration preview only. Your role, email, university and identity cannot be edited here.</p>
                            <div class="workspace-actions profile-edit-actions">
                                <asp:Button ID="SaveProfile" runat="server" ClientIDMode="Static" Text="Save Changes" CssClass="workspace-button" OnClick="SaveProfile_Click" Visible="false" />
                                <asp:Button ID="CancelEdit" runat="server" ClientIDMode="Static" Text="Cancel" CssClass="workspace-button secondary" OnClick="CancelEdit_Click" CausesValidation="false" Visible="false" formnovalidate="formnovalidate" />
                            </div>
                        </asp:Panel></dialog>
                    </section>
                    <aside class="workspace-card profile-account-card">
                        <section aria-labelledby="accountTitle">
                            <h2 id="accountTitle">University &amp; role</h2>
                            <p class="workspace-hint">Protected account information</p>
                            <dl class="account-details">
                                <div><dt>University</dt><dd>Not linked</dd></div>
                                <div><dt>College / department</dt><dd>Not provided</dd></div>
                                <div><dt>Role</dt><dd>University Internship Coordinator</dd></div>
                                <div><dt>Coordinator ID</dt><dd>Not connected</dd></div>
                            </dl>
                            <p class="workspace-hint profile-reference">Demo profile reference: DEMO-COORDINATOR-01</p>
                        </section>
                        <section class="profile-security" aria-labelledby="securityTitle">
                            <h2 id="securityTitle">Account security</h2>
                            <span class="workspace-badge warning">Authentication not connected</span>
                            <p class="workspace-hint">Email and password changes require the verified account security workflow. Password management is unavailable in this prototype.</p>
                        </section>
                    </aside>
                </div>
            </main>
            </div>
        </div>
    </form>
    <script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
<script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="<%= ResolveUrl("~/Js_project/Coordinator/coordinator-account-profile.js?v=20261021-original-ui") %>" defer="defer"></script>
</body>
</html>
