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
    <form id="form1" runat="server" class="auth-layout" novalidate="novalidate">
        <asp:HiddenField ID="CsrfToken" runat="server" ClientIDMode="Static" />
        <section class="auth-campus" aria-labelledby="campusTitle">
            <img class="auth-campus__image" src="../Assets/auth/just-campus.jpg" alt="The library courtyard at Jordan University of Science and Technology" fetchpriority="high" />
            <div class="auth-campus__overlay"></div>
            <a class="auth-campus__wordmark" href="../Default.aspx" aria-label="UIMS home">UIMS<span>JUST INTERNSHIPS</span></a>
            <div class="auth-campus__story">
                <span class="auth-eyebrow">KNOWLEDGE. EXPERIENCE. POSSIBILITY.</span>
                <h1 id="campusTitle">Your Future <br />Starts Here<span>.</span></h1>
                <p>University Internship Management System</p>
                <div class="auth-campus__rule"></div>
                <span class="auth-campus__location">Jordan University of Science and Technology &middot; Irbid, Jordan</span>
            </div>
            <div class="auth-campus__credit">Photo: <a href="https://commons.wikimedia.org/wiki/File:Jordan_University_of_Science_and_Technology%27s_Library.jpg" target="_blank" rel="noopener noreferrer">Fawwaz Dawod</a> &middot; <a href="https://creativecommons.org/licenses/by-sa/3.0/" target="_blank" rel="noopener noreferrer">CC BY-SA 3.0</a> &middot; Display cropped with overlay</div>
        </section>
        <main class="auth-panel">
            <div class="auth-panel__inner" id="loginContent">
                <a class="auth-brand" href="https://www.just.edu.jo/" target="_blank" rel="noopener noreferrer">
                    <img src="../Assets/shared/images/just-logo.png" width="72" height="72" alt="Official JUST emblem" />
                    <span><strong>Jordan University of<br />Science and Technology</strong><span lang="ar" dir="rtl">&#1580;&#1575;&#1605;&#1593;&#1577; &#1575;&#1604;&#1593;&#1604;&#1608;&#1605; &#1608;&#1575;&#1604;&#1578;&#1603;&#1606;&#1608;&#1604;&#1608;&#1580;&#1610;&#1575; &#1575;&#1604;&#1571;&#1585;&#1583;&#1606;&#1610;&#1577;</span></span>
                </a>
                <div class="auth-welcome">
                    <span class="auth-eyebrow">YOUR INTERNSHIP JOURNEY</span>
                    <h2>Welcome Back</h2>
                    <p>Sign in to connect your learning<br class="auth-desktop-break" /> with real-world experience.</p>
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
                    <div class="auth-options"><asp:Button ID="ForgotPassword" runat="server" Text="Forgot Password?" CssClass="auth-link" OnClick="ForgotPassword_Click" CausesValidation="false" data-action="reset" data-loading="Sending..." /></div>
                    <div id="authMessage" class="auth-message" role="status" aria-live="polite"><asp:Literal ID="AuthMessage" runat="server" Mode="Encode" /></div>
                    <asp:Button ID="SignIn" runat="server" ClientIDMode="Static" Text="Sign In" CssClass="auth-button" OnClick="SignIn_Click" CausesValidation="false" data-action="login" data-loading="Signing in..." />
                    <p class="auth-account-note">Use the account provided by your administrator.</p>
                </div>
                <footer class="auth-footer"><svg class="auth-icon" aria-hidden="true"><use href="#icon-shield" /></svg><span>Authorized university users only</span></footer>
            </div>
            <span class="auth-panel__copyright">&copy; <%= DateTime.Now.Year %> JUST &middot; University Internship Management System</span>
        </main>
        <dialog id="firstLoginDialog" class="auth-dialog" aria-labelledby="changeTitle" aria-describedby="changeDescription">
            <div class="auth-dialog__icon"><svg class="auth-icon" aria-hidden="true"><use href="#icon-shield" /></svg></div>
            <span class="auth-eyebrow">SECURE YOUR ACCOUNT</span>
            <h2 id="changeTitle">A fresh start.<br />A password of your own.</h2>
            <p id="changeDescription">For your first sign-in, replace your temporary password to continue to your workspace.</p>
            <div class="auth-field">
                <label for="NewPassword">New password</label>
                <div class="auth-input-wrap">
                    <svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg>
                    <asp:TextBox ID="NewPassword" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="auth-input" autocomplete="new-password" placeholder="Create a strong password" aria-describedby="passwordRules strengthText" />
                    <button class="auth-eye" type="button" data-toggle="NewPassword" aria-controls="NewPassword" aria-label="Show new password" aria-pressed="false"><svg class="auth-icon" aria-hidden="true"><use href="#icon-eye" /></svg></button>
                </div>
                <div class="auth-strength" id="strengthMeter" role="meter" aria-label="Password strength" aria-valuemin="0" aria-valuemax="4" aria-valuenow="0"><span></span><span></span><span></span><span></span></div>
                <span id="strengthText" class="auth-hint" aria-live="polite">Password strength</span>
                <ul id="passwordRules" class="auth-password-rules"><li data-rule="length">At least 12 characters</li><li data-rule="case">Uppercase and lowercase letters</li><li data-rule="number">At least one number</li><li data-rule="symbol">At least one symbol</li></ul>
            </div>
            <div class="auth-field">
                <label for="ConfirmPassword">Confirm password</label>
                <div class="auth-input-wrap">
                    <svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg>
                    <asp:TextBox ID="ConfirmPassword" runat="server" ClientIDMode="Static" TextMode="Password" CssClass="auth-input" autocomplete="new-password" placeholder="Re-enter your new password" aria-describedby="matchText" />
                    <button class="auth-eye" type="button" data-toggle="ConfirmPassword" aria-controls="ConfirmPassword" aria-label="Show confirmation password" aria-pressed="false"><svg class="auth-icon" aria-hidden="true"><use href="#icon-eye" /></svg></button>
                </div>
                <span id="matchText" class="auth-hint" aria-live="polite"></span>
            </div>
            <div class="auth-message" role="alert"><asp:Literal ID="ChangeError" runat="server" Mode="Encode" /></div>
            <asp:Button ID="SaveChanges" runat="server" ClientIDMode="Static" Text="Save Changes" CssClass="auth-button" OnClick="SaveChanges_Click" CausesValidation="false" data-action="change" data-loading="Saving changes..." />
            <p class="auth-dialog__note"><svg class="auth-icon" aria-hidden="true"><use href="#icon-lock" /></svg> Your password must be updated before continuing.</p>
        </dialog>
        <noscript><p class="auth-noscript">Enable JavaScript for password visibility controls and the secure first-login dialog.</p></noscript>
    </form>
    <script src="../Js_project/Auth/auth.js" defer="defer"></script>
</body>
</html>
