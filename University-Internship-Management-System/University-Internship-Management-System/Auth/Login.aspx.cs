using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using University_Internship_Management_System.Services;

namespace University_Internship_Management_System.Auth
{
    public partial class Login : Page
    {
        protected bool PasswordChangeRequired { get; private set; }
        protected string IssuedTicket { get; private set; }
        protected string DashboardUrl { get; private set; }
        protected bool CompletionPending { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            Response.Headers["X-Frame-Options"] = "DENY";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "same-origin";
            if (!IsPostBack)
            {
                string token = Guid.NewGuid().ToString("N");
                CsrfToken.Value = System.Web.Security.MachineKey.Protect(System.Text.Encoding.UTF8.GetBytes(token), "Uims.Login.Csrf") is byte[] bytes
                    ? Convert.ToBase64String(bytes) : "";
                Session["Uims.Csrf." + token] = true;
            }
        }
        protected void SignIn_Click(object sender, EventArgs e)
        {
            if (!ValidRequest() || !ValidEmail()) { Password.Text = ""; return; }
            if (string.IsNullOrEmpty(Password.Text)) { PasswordError.Text = "Enter your password."; return; }
            RegisterAsyncTask(new PageAsyncTask(SignInAsync));
        }
        protected void SaveChanges_Click(object sender, EventArgs e)
        {
            PasswordChangeRequired = true;
            if (!ValidRequest()) { CurrentPassword.Text = ""; NewPassword.Text = ""; ConfirmPassword.Text = ""; return; }
            RegisterAsyncTask(new PageAsyncTask(ChangePasswordAsync));
        }
        private async Task ChangePasswordAsync()
        {
            string handle = SessionService.Handle(Request);
            TabSession session = null;
            bool locked = false, passwordUpdated = false;
            try
            {
                var identity = await SessionService.VerifyAsync(handle);
                session = SessionService.Get(handle);
                await session.RefreshLock.WaitAsync(); locked = true;
                SessionService.Get(handle);
                var account = await new UserAccountService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).GetActiveAsync(identity);
                SessionService.RequireCurrent(session, account);
                if (account.CredentialState != "ready") throw new UnauthorizedAccessException();
                if (account.MustChangePassword)
                {
                    if (!PasswordCompletion.IsValid(session.PendingPasswordCompletion, account))
                    {
                        if (NewPassword.Text == CurrentPassword.Text) throw new ArgumentException("Choose a different new password.");
                        var tokens = await new FirebaseRestAuthenticationService().ReauthenticateAndChangePasswordAsync(account.Uid,
                            account.Email, CurrentPassword.Text, NewPassword.Text, ConfirmPassword.Text);
                        passwordUpdated = true;
                        session.IdToken = tokens.IdToken; session.RefreshToken = tokens.RefreshToken;
                        session.TokenExpiresUtc = tokens.ExpiresUtc;
                        identity = await new FirebaseTokenVerifier().VerifyAsync(tokens.IdToken);
                        var current = await new UserAccountService(new FirebaseRealtimeDatabaseRepository(tokens.IdToken)).GetActiveAsync(identity);
                        if (current.SecurityVersion != account.SecurityVersion || current.PasswordPolicyVersion != account.PasswordPolicyVersion || current.CredentialState != "ready")
                            throw new UnauthorizedAccessException();
                        session.PendingPasswordCompletion = PasswordCompletion.Create(current);
                        SessionService.RevokeOtherTabs(account.Uid, handle);
                        LoginDiagnostics.Write("FirebasePasswordUpdateConfirmed");
                    }
                    await new FirebaseRealtimeDatabaseRepository(session.IdToken).SavePasswordCompletionAsync(account, session.PendingPasswordCompletion);
                    account = await new UserAccountService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).GetActiveAsync(identity);
                    if (account.MustChangePassword) throw new UnauthorizedAccessException();
                }
                IssuedTicket = SessionService.Create(identity, session.IdToken, account.SecurityVersion, session.RefreshToken);
                SessionService.Logout(handle);
                PasswordChangeRequired = false;
                DashboardUrl = ResolveUrl("~/" + RoleAuthorizationService.Dashboard(account));
                LoginDiagnostics.Write("FirstPasswordChangeFinalized");
            }
            catch (ArgumentException)
            { ChangeError.Text = "Use a different password with 12–128 characters, uppercase, lowercase, number and symbol. Passwords must match."; }
            catch (UnauthorizedAccessException)
            {
                CompletionPending = session?.PendingPasswordCompletion != null;
                ChangeError.Text = CompletionPending ? "Your password changed, but completion remains restricted. Retry completion after the policy/storage issue is resolved."
                    : passwordUpdated ? "Your Firebase password changed, but account finalization was not accepted. Sign in with the NEW password; access remains restricted."
                    : "Current password or session was not accepted. Sign in again if your session expired. Access remains restricted.";
                LoginDiagnostics.Write(passwordUpdated || CompletionPending ? "PasswordChangedCompletionRestricted" : "PasswordReauthenticationRejected");
            }
            catch (Exception)
            {
                CompletionPending = session?.PendingPasswordCompletion != null;
                ChangeError.Text = CompletionPending
                    ? "Your Firebase password was changed, but completion could not be saved. Keep this tab open and retry completion after the scoped database rule is approved. Do not enter another password."
                    : "The password operation could not be completed. Access remains restricted. If your password changed, sign in with it and retry.";
                LoginDiagnostics.Write("FirstPasswordCompletionFailedAccessRestricted");
            }
            finally
            {
                if (locked) session.RefreshLock.Release();
                CurrentPassword.Text = ""; NewPassword.Text = ""; ConfirmPassword.Text = "";
            }
        }
        private async Task SignInAsync()
        {
            string stage = "FirebaseRestAuthenticationRejected";
            try
            {
                var tokens = await new FirebaseRestAuthenticationService().SignInAsync(Email.Text.Trim(), Password.Text);
                Password.Text = "";
                stage = "FirebaseUidVerificationFailure";
                var identity = await new FirebaseTokenVerifier().VerifyAsync(tokens.IdToken);
                if (identity.Uid != tokens.Uid || !string.Equals(identity.Email, tokens.Email, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException();
                stage = "ProfileResolutionFailure";
                var account = await new UserAccountService(new FirebaseRealtimeDatabaseRepository(tokens.IdToken)).GetActiveAsync(identity);
                string previous = SessionService.Handle(Request);
                if (!string.IsNullOrEmpty(previous)) SessionService.Logout(previous);
                stage = "TabTicketCreationFailure";
                IssuedTicket = SessionService.Create(identity, tokens.IdToken, account.SecurityVersion, tokens.RefreshToken);
                LoginDiagnostics.Write("TabTicketCreated");
                PasswordChangeRequired = account.MustChangePassword;
                LoginDiagnostics.Write(PasswordChangeRequired ? "FirstLoginRestriction" : "LoginAuthorizedPendingDashboardReadiness");
                DashboardUrl = ResolveUrl("~/" + RoleAuthorizationService.Dashboard(account));
                AuthMessage.Text = PasswordChangeRequired
                    ? "Your identity was verified. Change your password to continue."
                    : "Your Firebase identity and UIMS profile were verified. The dashboard remains protected by its live-data readiness check.";
                form1.Attributes["data-password-required"] = PasswordChangeRequired ? "true" : "false";
            }
            catch (UnauthorizedAccessException)
            {
                LoginDiagnostics.Write(stage);
                AuthMessage.Text = "Sign-in was not accepted. Check your credentials or contact the coordinator about your account profile.";
            }
            catch (Exception)
            {
                LoginDiagnostics.Write(stage + "Unavailable");
                AuthMessage.Text = "Sign-in is temporarily unavailable. Please try again. No account changes were made.";
            }
            finally { Password.Text = ""; }
        }
        private bool ValidRequest()
        {
            try
            {
                if (!Request.IsSecureConnection || !Gateway.IsSameOrigin(Request)) throw new UnauthorizedAccessException();
                var data = System.Web.Security.MachineKey.Unprotect(Convert.FromBase64String(CsrfToken.Value), "Uims.Login.Csrf");
                string token = System.Text.Encoding.UTF8.GetString(data);
                if (Session["Uims.Csrf." + token] == null) throw new UnauthorizedAccessException();
                return true;
            }
            catch { AuthMessage.Text = "Your sign-in form expired. Reload this page and try again."; return false; }
        }
        private bool ValidEmail()
        {
            try { var email = new MailAddress(Email.Text.Trim()); if (email.Address != Email.Text.Trim() || email.Address.Length > 254) throw new FormatException(); return true; }
            catch { EmailError.Text = "Enter a valid email address."; return false; }
        }
    }
}
