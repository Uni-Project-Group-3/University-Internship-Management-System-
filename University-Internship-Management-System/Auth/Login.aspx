<%@ Page Title="Sign In | UIMS" Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" CodeBehind="Login.aspx.cs" Inherits="University_Internship_Management_System.Auth.Login" %>
<!DOCTYPE html>
<html lang="en">
<head id="Head1" runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>Sign In | UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/auth/auth.css" rel="stylesheet" />
</head>
<body class="auth-page">
    <svg class="auth-sprite" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <symbol id="icon-mail" viewBox="0 0 24 24"><rect x="3" y="5" width="18" height="14" rx="3" /><path d="m4 7 8 6 8-6" /></symbol>
        <symbol id="icon-lock" viewBox="0 0 24 24"><rect x="5" y="10" width="14" height="11" rx="3" /><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 5v2" /></symbol>
        <symbol id="icon-eye" viewBox="0 0 24 24"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" /><circle cx="12" cy="12" r="3" /></symbol>
        <symbol id="icon-arrow" viewBox="0 0 24 24"><path d="M4 12h15m-6-6 6 6-6 6" /></symbol>
        <symbol id="icon-shield" viewBox="0 0 24 24"><path d="m12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6l8-3Z" /><path d="m8 12 3 3 5-6" /></symbol>
    </svg>
    <header class="home-header">
    <nav class="home-navbar" aria-label="Public navigation">
        <a class="home-navbar__brand" href="~/Default.aspx" runat="server">
            <img class="home-navbar__logo" src="~/Assets/shared/images/just-logo.png" runat="server" width="40" height="40" alt="Official JUST emblem" />
            <span><strong>UIMS</strong><span class="home-navbar__subtitle">JUST Internships</span></span>
        </a>
        <button class="home-navbar__toggle" type="button" aria-label="Open navigation menu" aria-expanded="false" aria-controls="publicNavigationLinks" hidden="hidden">
            <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
        </button>
        <div id="publicNavigationLinks" class="home-navbar__links">
            <a id="HomeLink" ClientIDMode="Static" runat="server" class="home-navbar__link" href="~/Default.aspx">Home</a>
            <a id="AboutLink" ClientIDMode="Static" runat="server" class="home-navbar__link" href="~/About.aspx">About Us</a>
            <a id="ContactLink" ClientIDMode="Static" runat="server" class="home-navbar__link" href="~/Contact.aspx">Contact Us</a>
            <a id="LoginLink" ClientIDMode="Static" runat="server" aria-current="page" class="home-navbar__link home-navbar__sign-in is-active" href="~/Auth/Login.aspx">Sign In</a>
        </div>
    </nav>
</header>
    <form id="form1" runat="server" class="auth-layout" novalidate="novalidate">
        <asp:HiddenField ID="CsrfToken" runat="server" ClientIDMode="Static" />
        <div class="auth-shell">
        <section class="auth-campus" aria-labelledby="campusTitle">
            <img class="auth-campus__image" src="../Assets/auth/just-campus.jpg" alt="The library courtyard at Jordan University of Science and Technology" fetchpriority="high" />
            <div class="auth-campus__overlay"></div>
            <a class="auth-campus__wordmark" href="../Default.aspx" aria-label="UIMS home">UIMS<span>JUST INTERNSHIPS</span></a>





        </section>
        <main class="auth-panel">
            <div class="auth-panel__inner" id="loginContent">
                <a class="auth-brand" href="https://www.just.edu.jo/" target="_blank" rel="noopener noreferrer">
                    <img src="../Assets/shared/images/just-logo.png" width="72" height="72" alt="Official JUST emblem" />
                    <span><strong>Jordan University of<br />Science and Technology</strong><span lang="ar" dir="rtl">&#1580;&#1575;&#1605;&#1593;&#1577; &#1575;&#1604;&#1593;&#1604;&#1608;&#1605; &#1608;&#1575;&#1604;&#1578;&#1603;&#1606;&#1608;&#1604;&#1608;&#1580;&#1610;&#1575; &#1575;&#1604;&#1571;&#1585;&#1583;&#1606;&#1610;&#1577;</span></span>
                </a>
                <div class="auth-welcome">
                    <h2>Welcome Back</h2>
                    <p>Sign in to your internship workspace.</p>
                </div>
                <div class="auth-fields">
                    <div class="auth-field">
                        <label for="Email">Email address</label>
                        <div class="auth-input-wrap">
                            <svg class="auth-icon" aria-hidden="true"><use href="#icon-mail" /></svg>
                            <asp:TextBox ID="Email" runat="server" ClientIDMode="Static" TextMode="Email" CssClass="auth-input" MaxLength="254" autocomplete="username" placeholder="Enter your email address" aria-describedby="emailError" />
                        </div>
                        <span id="emailError" class="auth-field-error" aria-live="polite"><asp:Literal ID="EmailError" runat="server" Mode="Encode" /></span>
                    </div>
                    <div class="auth-field">
                        <label for="Password">Password</label>
                        <div class="auth-input-wrap">
                            <svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg>
                            <asp:TextBox ID="Password" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="auth-input" autocomplete="current-password" placeholder="Enter your password" aria-describedby="passwordError" />
                            <button class="auth-eye" type="button" data-toggle="Password" aria-controls="Password" aria-label="Show password" aria-pressed="false"><svg class="auth-icon" aria-hidden="true"><use href="#icon-eye" /></svg></button>
                        </div>
                        <span id="passwordError" class="auth-field-error" aria-live="polite"><asp:Literal ID="PasswordError" runat="server" Mode="Encode" /></span>
                    </div>
                    <div class="auth-options"><button id="ForgotPassword" type="button" class="auth-link" aria-haspopup="dialog" aria-controls="forgotPasswordDialog">Forgot Password?</button></div>
                    <div id="authMessage" class="auth-message" role="status" aria-live="polite"><asp:Literal ID="AuthMessage" runat="server" Mode="Encode" /></div>
                    <asp:Button ID="SignIn" runat="server" ClientIDMode="Static" Text="Sign In" CssClass="auth-button" OnClick="SignIn_Click" CausesValidation="false" data-action="login" data-loading="Signing in..." />
                    <p class="auth-account-note">Use the account provided by your administrator.</p>
                </div>
                <div class="auth-footer"><svg class="auth-icon" aria-hidden="true"><use href="#icon-shield" /></svg><span>Authorized university users only</span></div>
            </div>
        </main>
        </div>
        <dialog id="forgotPasswordDialog" class="auth-dialog auth-reset-dialog" aria-labelledby="resetTitle" aria-describedby="resetDescription">
            <button type="button" class="auth-dialog__close" data-close-reset="true" aria-label="Close password reset">
                <svg class="auth-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="m6 6 12 12M6 18 18 6" /></svg>
            </button>
            <div class="auth-dialog__icon"><svg class="auth-icon" aria-hidden="true"><use href="#icon-mail" /></svg></div>
            <span class="auth-eyebrow">LET'S GET YOU BACK IN</span>
            <h2 id="resetTitle">Reset Your Password</h2>
            <p id="resetDescription">Enter your account email address and we'll send you instructions to reset your password.</p>
            <div id="resetFields">
                <div class="auth-field">
                    <label for="ResetEmail">Email address</label>
                    <div class="auth-input-wrap">
                        <svg class="auth-icon" aria-hidden="true"><use href="#icon-mail" /></svg>
                        <input id="ResetEmail" type="email" class="auth-input" maxlength="254" autocomplete="email" inputmode="email" placeholder="Enter your email address" aria-describedby="resetEmailError" />
                    </div>
                    <span id="resetEmailError" class="auth-field-error" aria-live="polite"></span>
                </div>
                <p id="resetError" class="auth-message" role="alert" hidden="hidden"></p>
                <button id="SendResetLink" class="auth-button" type="button">Send Reset Link</button>
            </div>
            <div id="resetConfirmation" class="auth-reset-confirmation" role="status" tabindex="-1" hidden="hidden">
                <svg class="auth-icon" aria-hidden="true"><use href="#icon-shield" /></svg>
                <p>If an account exists for this email, password reset instructions will be sent.</p>
            </div>
            <button type="button" class="auth-link auth-reset-back" data-close-reset="true"><svg class="auth-icon" aria-hidden="true"><use href="#icon-arrow" /></svg>Back to Login</button>
        </dialog>
        <dialog id="firstLoginDialog" class="auth-dialog auth-first-dialog" aria-labelledby="changeTitle" aria-describedby="changeDescription">
            <div class="auth-dialog__icon"><svg class="auth-icon" aria-hidden="true"><use href="#icon-shield" /></svg></div>
            <span class="auth-eyebrow">SECURE YOUR ACCOUNT</span>
            <h2 id="changeTitle">Change Your Password</h2>
            <p id="changeDescription">Choose a strong password to secure your account before continuing to your workspace.</p>
            <div class="auth-field">
                <label for="NewPassword">New password</label>
                <div class="auth-input-wrap">
                    <svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg>
                    <asp:TextBox ID="NewPassword" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="auth-input" autocomplete="new-password" placeholder="Create a strong password" aria-describedby="passwordRules strengthText" />
                    <button class="auth-eye" type="button" data-toggle="NewPassword" aria-controls="NewPassword" aria-label="Show new password" aria-pressed="false"><svg class="auth-icon" aria-hidden="true"><use href="#icon-eye" /></svg></button>
                </div>
                <div class="auth-strength" id="strengthMeter" role="meter" aria-label="Password strength" aria-valuemin="0" aria-valuemax="4" aria-valuenow="0"><span></span><span></span><span></span><span></span></div>
                <span id="strengthText" class="auth-hint" aria-live="polite">Password strength</span>
                <ul id="passwordRules" class="auth-password-rules"><li data-rule="length">At least 6 characters. Firebase applies the configured password policy.</li></ul>
            </div>
            <div class="auth-field">
                <label for="ConfirmPassword">Confirm new password</label>
                <div class="auth-input-wrap">
                    <svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg>
                    <asp:TextBox ID="ConfirmPassword" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="auth-input" autocomplete="new-password" placeholder="Re-enter your new password" aria-describedby="matchText" />
                    <button class="auth-eye" type="button" data-toggle="ConfirmPassword" aria-controls="ConfirmPassword" aria-label="Show confirmation password" aria-pressed="false"><svg class="auth-icon" aria-hidden="true"><use href="#icon-eye" /></svg></button>
                </div>
                <span id="matchText" class="auth-hint" aria-live="polite"></span>
            </div>
            <div class="auth-message" role="alert"><asp:Literal ID="ChangeError" runat="server" Mode="Encode" /></div>
            <asp:Button ID="SaveChanges" runat="server" ClientIDMode="Static" Text="Save Changes" CssClass="auth-button" OnClick="SaveChanges_Click" CausesValidation="false" data-action="change" data-loading="Saving changes..." />
            <p id="firstChangeStatus" class="auth-message" role="status" tabindex="-1" hidden="hidden"></p>
            <p class="auth-dialog__note"><svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg> Update your temporary password to continue securely.</p>
            <p id="firstLoginPreviewNotice" class="auth-preview-note" hidden="hidden">Design preview only. Firebase is not connected and no password will be saved.</p>
            <a id="exitPasswordPreview" class="auth-link auth-preview-back" href="Login.aspx" hidden="hidden">Back to Login</a>
        </dialog>
        <noscript><p class="auth-noscript">Enable JavaScript for password visibility controls and the secure first-login dialog.</p></noscript>
    </form>
    <footer class="auth-public-footer"><span>&copy; <%= DateTime.Now.Year %> Jordan University of Science and Technology.</span><span>University Internship Management System.</span></footer>

<script src="../Js_project/firebase-client.js?v=20261024-link-only-all-roles" defer="defer"></script>
<script src="../Js_project/Auth/auth.js?v=20261024-link-only-all-roles" defer="defer"></script>
</body>
</html>
