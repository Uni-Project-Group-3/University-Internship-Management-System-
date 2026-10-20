using System;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Web;
using System.Web.UI;

namespace University_Internship_Management_System.Auth
{
    public partial class Login : Page
    {
        private const string ConfigurationMessage =
            "Sign-in is not available yet. Please contact your system administrator.";

        protected bool PasswordChangeRequired { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["X-Frame-Options"] = "DENY";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "same-origin";
            PasswordChangeRequired = Session["Uims.PasswordChangeRequired"] as bool? == true;
            form1.Attributes["data-password-required"] = PasswordChangeRequired ? "true" : "false";

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

        protected void ForgotPassword_Click(object sender, EventArgs e)
        {
            if (!ValidateRequest() || !ValidateEmail())
            {
                return;
            }

            AuthMessage.Text = "Password recovery is not configured yet. Please contact your system administrator.";
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
            try
            {
                if (email.Length <= 254 && new MailAddress(email).Address == email)
                {
                    Email.Text = email;
                    return true;
                }
            }
            catch (FormatException)
            {
            }
            catch (ArgumentException)
            {
            }

            EmailError.Text = "Enter a valid email address.";
            Email.Attributes["aria-invalid"] = "true";
            return false;
        }
    }
}
