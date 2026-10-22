using System;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
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

            PasswordChangeRequired = Session["Uims.PasswordChangeRequired"] as bool? == true;
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

        protected void SignIn_Click(object sender, EventArgs e)
        {
            if (!ValidateRequest() || !ValidateEmail())
            {
                return;
            }

            if (string.IsNullOrEmpty(Password.Text))
            {
                PasswordError.Text = "Enter your password.";
                Password.Attributes["aria-invalid"] = "true";
                return;
            }

            // No authentication session is issued without the project's authorization service.
            AuthMessage.Text = ConfigurationMessage;
        }

        protected void SaveChanges_Click(object sender, EventArgs e)
        {
            if (!ValidateRequest() || !PasswordChangeRequired)
            {
                AuthMessage.Text = "Please sign in before changing your password.";
                return;
            }

            string password = NewPassword.Text;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password,
                @"\A(?=.{12,})(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9\s]).*\z"))
            {
                ChangeError.Text = "Use at least 12 characters, uppercase and lowercase letters, a number, and a symbol.";
                return;
            }

            if (password != ConfirmPassword.Text)
            {
                ChangeError.Text = "Passwords do not match.";
                return;
            }

            ChangeError.Text = ConfigurationMessage;
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
