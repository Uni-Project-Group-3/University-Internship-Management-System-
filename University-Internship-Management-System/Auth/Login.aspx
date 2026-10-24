

<%@ Page Title="Sign In | UIMS" Language="C#" AutoEventWireup="true" Async="true" EnableViewState="false" CodeBehind="Login.aspx.cs" Inherits="University_Internship_Management_System.Auth.Login" %>
<!DOCTYPE html>
<html lang="en">
<head id="Head1" runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="robots" content="noindex, nofollow" />
    <title>Sign In | UIMS</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/auth/auth.css" rel="stylesheet" /><link href="../Assets/auth/login-card.css?v=20261024-reference-pulse" rel="stylesheet" />
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
        <main class="app">
    <section class="stage" aria-label="JUST university access card">
      <div class="orbits" aria-hidden="true"></div><div class="grid-dots" aria-hidden="true"></div>
<div class="card-workspace" id="workspace">
        <div class="card-aura" aria-hidden="true"></div>
        <div class="float-sleeve">
          <div class="card-tilt" id="cardTilt">
            <div class="card-flipper" id="cardFlipper">
              <article class="id-card front" aria-label="JUST university ID card">
                <div class="card-top">
                  <div class="card-logo">
                    <div class="logo-disc"><img src="../Assets/shared/images/just-logo.png" alt=""/></div>
                    <div><strong>JUST</strong><small>JORDAN UNIVERSITY OF SCIENCE &amp; TECHNOLOGY</small></div>
                  </div>
                  <span class="card-ribbon">JUST · UNIVERSITY ACCESS</span>
                </div>
                <div class="card-body">
                  <img class="card-watermark" src="../Assets/shared/images/just-logo.png" alt="" aria-hidden="true"/>
                  <div class="card-section">CAMPUS ACCESS · INTERNSHIPS · UNIVERSITY SERVICES</div>
                  <div class="student-title">UNIVERSITY CARD</div><span class="reference-chip" aria-hidden="true"></span>
                  <div class="card-values">
                    <div class="card-detail">
                      <div class="detail-field" id="previewEmailField"><label>UNIVERSITY EMAIL</label><span class="value placeholder" id="previewEmail">student@just.edu.jo</span></div>
                      <div class="detail-field" id="previewPasswordField"><label>ACCESS PASSWORD</label><span class="value dots placeholder" id="previewPassword">••••••••</span></div>
                    </div>
                    <div class="student-photo" aria-hidden="true">
                      <svg viewBox="0 0 100 115" xmlns="http://www.w3.org/2000/svg" role="presentation"><defs><linearGradient id="g" x2="1" y2="1"><stop stop-color="#83a9da"/><stop offset="1" stop-color="#acc8eb"/></linearGradient></defs><circle cx="50" cy="42" r="22" fill="url(#g)"/><path d="M6 115c0-31 18-48 44-48s44 17 44 48" fill="url(#g)"/><circle cx="79" cy="16" r="28" fill="#fff" opacity=".25"/></svg>
                    </div>
                  </div>
                  <div class="card-footer"><span class="chip"><b></b> SECURE UNIVERSITY ACCESS</span><span class="micro">JUST · UIMS</span></div>
                </div>
              </article>
              <article class="id-card back" aria-label="University card verification">
                <div class="back-contact">JORDAN UNIVERSITY OF SCIENCE AND TECHNOLOGY · www.just.edu.jo</div>
                <div class="magnetic-strip" aria-hidden="true"></div>
                <div class="verification-row"><div><span>AUTHORIZED ACCESS</span><div class="signature-strip" aria-hidden="true"></div></div><div><span>STATUS</span><div class="security-chip" aria-hidden="true"></div></div></div>
                <div class="back-center"><img src="../Assets/shared/images/just-logo.png" alt="JUST university logo"/><strong>JORDAN UNIVERSITY OF<br/>SCIENCE AND TECHNOLOGY</strong><small id="cardAuthStatus" role="status" aria-live="polite">Secure sign-in</small></div>
                <div class="back-foot"><span>JUST · INTERNSHIP MANAGEMENT</span><span>CONTACTLESS ACCESS</span></div>
              </article>
            </div>
          </div>
        </div>
      </div>
    </section>
<section class="login-panel" aria-label="Sign in"><div class="login-center" id="loginContent">
                <div class="auth-fields">
                    <div class="field auth-field">
                        <label for="Email">Email address</label>
                        <div class="input-wrap auth-input-wrap">
                            <svg class="auth-icon" aria-hidden="true"><use href="#icon-mail" /></svg>
                            <asp:TextBox ID="Email" runat="server" ClientIDMode="Static" TextMode="Email" CssClass="auth-input" MaxLength="254" autocomplete="username" placeholder="Enter your email address" aria-describedby="emailError" />
                        </div>
                        <span id="emailError" class="auth-field-error" aria-live="polite"><asp:Literal ID="EmailError" runat="server" Mode="Encode" /></span>
                    </div>
                    <div class="field auth-field">
                        <label for="Password">Password</label>
                        <div class="input-wrap auth-input-wrap">
                            <svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg>
                            <asp:TextBox ID="Password" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="auth-input" autocomplete="current-password" placeholder="Enter your password" aria-describedby="passwordError" />
                            <button class="auth-eye" type="button" data-toggle="Password" aria-controls="Password" aria-label="Show password" aria-pressed="false"><svg class="auth-icon" aria-hidden="true"><use href="#icon-eye" /></svg></button>
                        </div>
                        <span id="passwordError" class="auth-field-error" aria-live="polite"><asp:Literal ID="PasswordError" runat="server" Mode="Encode" /></span>
                    </div>
                    <div class="auth-options"><button id="ForgotPassword" type="button" class="auth-link" aria-haspopup="dialog" aria-controls="forgotPasswordDialog">Forgot Password?</button></div>
                    <div id="authMessage" class="auth-message" role="status" aria-live="polite"><asp:Literal ID="AuthMessage" runat="server" Mode="Encode" /></div>
                    <asp:Button ID="SignIn" runat="server" ClientIDMode="Static" Text="Login" CssClass="auth-button" OnClick="SignIn_Click" CausesValidation="false" data-action="login" data-loading="Signing in..." />
                </div>
</div></section></main>
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
        </dialog>
        <noscript><p class="auth-noscript">Enable JavaScript for password visibility controls and the secure first-login dialog.</p></noscript>
    </form>
    <footer class="auth-public-footer"><span>&copy; <%= DateTime.Now.Year %> Jordan University of Science and Technology.</span><span>University Internship Management System.</span></footer>

<script src="../Js_project/Auth/login-card.js?v=20261024-reference-pulse" defer="defer"></script>
<script src="../Js_project/firebase-client.js?v=20261024-merged-link-flow" defer="defer"></script>
<script src="../Js_project/Auth/auth.js?v=20261024-merged-link-flow" defer="defer"></script>
</body>
</html>
