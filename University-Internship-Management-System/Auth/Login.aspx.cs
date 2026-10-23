using System;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.Security;
using FirebaseAdmin.Auth;
using System.Linq;
using Newtonsoft.Json;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Auth
{
    public partial class Login : Page
    {
        private const string ConfigurationMessage =
            "Sign-in is not available yet. Please contact your system administrator.";

        private bool IsResetRequest => Request.QueryString["action"] == "reset";

        protected bool PasswordChangeRequired { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["X-Frame-Options"] = "DENY";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "same-origin";
            if (IsResetRequest)
            {
                RegisterAsyncTask(new PageAsyncTask(SendResetLinkAsync));
                return;
            }

            // Reuse an existing authenticated cookie instead of requesting the password again.
            var signedIn = Context.User?.Identity as FormsIdentity;
            var role = signedIn != null && signedIn.IsAuthenticated && !signedIn.Ticket.Expired
                ? FirebaseSignInService.CanonicalRole(signedIn.Ticket.UserData) : null;
            if (!IsPostBack && role != null && PendingTicket() == null && Request.QueryString["change"] != "required" && Request.QueryString["account"] != "new")
            {
                RedirectTo(FirebaseSignInService.Destination(role));
                return;
            }

            PasswordChangeRequired = PendingTicket() != null;
            form1.Attributes["data-password-required"] = PasswordChangeRequired ? "true" : "false";
            form1.DefaultButton = PasswordChangeRequired ? SaveChanges.ID : SignIn.ID;

            if (IsPostBack)
            {
                return;
            }

            if (Session["Uims.CsrfToken"] == null)
            {
                using (var random = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[32];
                    random.GetBytes(bytes);
                    Session["Uims.CsrfToken"] = Convert.ToBase64String(bytes);
                }
            }

            CsrfToken.Value = (string)Session["Uims.CsrfToken"];
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = Session.SessionID;
        }

        protected async void SignIn_Click(object sender, EventArgs e)
        {
            if (!ValidateRequest() || !ValidateEmail()) return;
            if (string.IsNullOrEmpty(Password.Text))
            {
                PasswordError.Text = "Enter your password.";
                return;
            }
            try
            {
                EnsureSecureTransport();
                var blockedUntil = Session["Uims.LoginBlockedUntil"] as DateTime?;
                if (blockedUntil.HasValue && blockedUntil.Value > DateTime.UtcNow)
                    throw new FirebaseLoginException("Please wait before trying to sign in again.", 429, Math.Max(1, (int)Math.Ceiling((blockedUntil.Value - DateTime.UtcNow).TotalSeconds)));
                var result = await new FirebaseSignInService().SignInAsync(Email.Text, Password.Text);
                FormsAuthentication.SignOut();
                if (result.MustChangePassword)
                {
                    WriteCookie(".UIMS.FirstLogin", result.Uid, "password-change", 10);
                    Session.Clear();
                    Session.Abandon();
                    RedirectTo("~/Auth/Login.aspx?change=required");
                    return;
                }
                CompleteSignIn(result);
            }
            catch (Exception error)
            {
                var loginError = error as FirebaseLoginException;
                if (loginError?.StatusCode == 429)
                    Session["Uims.LoginBlockedUntil"] = DateTime.UtcNow.AddSeconds(Math.Max(1, loginError.RetryAfterSeconds));
                else if (loginError?.StatusCode == 401)
                {
                    int attempts = (Session["Uims.LoginAttempts"] as int? ?? 0) + 1;
                    Session["Uims.LoginAttempts"] = attempts >= 5 ? 0 : attempts;
                    if (attempts >= 5) Session["Uims.LoginBlockedUntil"] = DateTime.UtcNow.AddMinutes(1);
                }
                AuthMessage.Text = error is FirebaseLoginException ? error.Message :
                    "Sign-in could not be completed. Check the connection and Firebase server configuration.";
                Trace.Warn("Login", error.GetType().Name);
            }
            finally { Password.Text = ""; }
        }

        protected async void SaveChanges_Click(object sender, EventArgs e)
        {
            if (!ValidateRequest()) return;
            var pending = PendingTicket();
            if (pending == null)
            {
                ChangeError.Text = "Please sign in again before changing your password.";
                return;
            }
            string password = NewPassword.Text;
            if (password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            {
                ChangeError.Text = "Use at least 12 characters, uppercase and lowercase letters, a number, and a symbol.";
                return;
            }
            if (password != ConfirmPassword.Text)
            {
                ChangeError.Text = "Passwords do not match.";
                return;
            }
            try
            {
                EnsureSecureTransport();
                var profile = await FirebaseSignInService.ReadProfileAsync(pending.Name);
                var result = FirebaseSignInService.ProfileResult(pending.Name, profile);
                var account = await FirebaseAuth.DefaultInstance.GetUserAsync(pending.Name);
                if (account.Disabled) throw new FirebaseLoginException("This account is disabled. Contact your coordinator.");
                if (result.MustChangePassword)
                {
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs { Uid = pending.Name, Password = password });
                    await new StudentFirebaseGateway().PatchAsync("users/" + StudentManagementService.Key(pending.Name),
                        new { mustChangePassword = false });
                }
                result.MustChangePassword = false;
                CompleteSignIn(result);
            }
            catch (Exception error)
            {
                ChangeError.Text = error is FirebaseLoginException ? error.Message :
                    "The password change could not be completed. Retry before leaving this page.";
                Trace.Warn("FirstLogin", error.GetType().Name);
            }
            finally { NewPassword.Text = ""; ConfirmPassword.Text = ""; }
        }

        private FormsAuthenticationTicket PendingTicket()
        {
            try
            {
                string value = Request.Cookies[".UIMS.FirstLogin"]?.Value;
                if (string.IsNullOrEmpty(value)) return null;
                var ticket = FormsAuthentication.Decrypt(value);
                return ticket != null && !ticket.Expired && ticket.UserData == "password-change" ? ticket : null;
            }
            catch { return null; }
        }

        private void WriteCookie(string name, string uid, string data, int minutes)
        {
            var ticket = new FormsAuthenticationTicket(2, uid, DateTime.Now,
                DateTime.Now.AddMinutes(minutes), false, data, FormsAuthentication.FormsCookiePath);
            Response.Cookies.Add(new HttpCookie(name, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true, Secure = Request.IsSecureConnection, SameSite = SameSiteMode.Lax,
                Path = FormsAuthentication.FormsCookiePath
            });
        }

        private void CompleteSignIn(FirebaseLoginResult result)
        {
            WriteCookie(FormsAuthentication.FormsCookieName, result.Uid, result.Role, 30);
            Response.Cookies.Add(new HttpCookie(".UIMS.FirstLogin", "")
            {
                Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true, Secure = Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax, Path = FormsAuthentication.FormsCookiePath
            });
            Session.Clear();
            Session.Abandon();
            string destination = LoginRedirectPolicy.CoordinatorPage(result.Role, Request.QueryString["ReturnUrl"]) ?? FirebaseSignInService.Destination(result.Role);
            RedirectTo(destination);
        }

        private void RedirectTo(string path)
        {
            // Only fixed application routes are used; never redirect to a supplied URL.
            Response.Redirect(ResolveUrl(path), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void EnsureSecureTransport()
        {
            if (!Request.IsSecureConnection && !Request.IsLocal)
                throw new FirebaseLoginException("Use HTTPS to sign in. HTTP is allowed only on this development computer.");
        }

        private async Task SendResetLinkAsync()
        {
            if (Request.HttpMethod != "POST")
            {
                WriteResetResponse(false, "Use the password reset form to continue.", 405);
                return;
            }

            string expected = Session["Uims.CsrfToken"] as string;
            if (string.IsNullOrEmpty(expected) || expected != Request.Form["csrfToken"])
            {
                WriteResetResponse(false, "Your session expired. Reload this page and try again.", 403);
                return;
            }

            string email = (Request.Form["email"] ?? "").Trim();
            if (!IsValidEmail(email))
            {
                WriteResetResponse(false, "Enter a valid email address.", 400);
                return;
            }

            var lastAttempt = Session["Uims.LastPasswordReset"] as DateTime?;
            if (lastAttempt.HasValue && DateTime.UtcNow - lastAttempt.Value < TimeSpan.FromSeconds(60))
            {
                WriteResetResponse(false, "Please wait a minute before requesting another reset link.", 429);
                return;
            }

            Session["Uims.LastPasswordReset"] = DateTime.UtcNow;
            try
            {
                await new FirebaseAuthService().SendPasswordResetEmailAsync(email);
                WriteResetResponse(true,
                    "If an account exists for this email, password reset instructions will be sent.", 200);
            }
            catch (Exception error) when (error is HttpRequestException || error is TaskCanceledException ||
                error is InvalidOperationException || error is JsonException)
            {
                WriteResetResponse(false,
                    "Password recovery is temporarily unavailable. Please wait a minute and try again.", 503);
            }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (!IsResetRequest)
            {
                base.Render(writer);
            }
        }

        private void WriteResetResponse(bool success, string message, int status)
        {
            Response.StatusCode = status;
            Response.TrySkipIisCustomErrors = true;
            if (status == 429)
            {
                Response.Headers["Retry-After"] = "60";
            }
            Response.ContentType = "application/json";
            Response.Write(JsonConvert.SerializeObject(new { success, message }));
        }

        private bool ValidateRequest()
        {
            string expected = Session["Uims.CsrfToken"] as string;
            if (!string.IsNullOrEmpty(expected) && expected == CsrfToken.Value)
            {
                return true;
            }

            AuthMessage.Text = "Your sign-in session expired. Reload this page and try again.";
            return false;
        }

        private bool ValidateEmail()
        {
            string email = Email.Text.Trim();
            if (IsValidEmail(email))
            {
                Email.Text = email;
                return true;
            }

            EmailError.Text = "Enter a valid email address.";
            Email.Attributes["aria-invalid"] = "true";
            return false;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                return email.Length <= 254 && new MailAddress(email).Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
