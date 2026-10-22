using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace University_Internship_Management_System.Shared
{
    public sealed class StudentCredentialMailer : IStudentCredentialMailer
    {
        private static string Setting(string name) { return Environment.GetEnvironmentVariable(name); }

        public void ValidateConfiguration()
        {
            foreach (string key in new[] { "UIMS_SMTP_HOST", "UIMS_SMTP_FROM", "UIMS_SMTP_USERNAME", "UIMS_SMTP_PASSWORD", "UIMS_LOGIN_URL" })
                if (string.IsNullOrWhiteSpace(Setting(key)))
                    throw new StudentManagementException("Credential email is not configured. Configure the UIMS_SMTP_* settings and UIMS_LOGIN_URL on the server.");
            new MailAddress(Setting("UIMS_SMTP_FROM"));
            Uri login;
            if (!Uri.TryCreate(Setting("UIMS_LOGIN_URL"), UriKind.Absolute, out login) ||
                (login.Scheme != "https" && !(login.IsLoopback && login.Scheme == "http")) ||
                !string.IsNullOrEmpty(login.UserInfo))
                throw new StudentManagementException("UIMS_LOGIN_URL must be the site's HTTPS sign-in URL (HTTP localhost is allowed for development).");
            int port;
            if (!int.TryParse(Setting("UIMS_SMTP_PORT") ?? "587", out port) || port < 1 || port > 65535)
                throw new StudentManagementException("UIMS_SMTP_PORT is invalid.");
        }

        public Task SendWelcomeAsync(string email, string name, string studentId, string password)
        {
            return SendAsync(email, "Your university internship account",
                "Hello " + name + ",\n\nYour university internship account is ready.\n\n" +
                "Student ID: " + studentId + "\nSign-in email: " + email +
                "\nTemporary password: " + password + "\nSign in: " + Setting("UIMS_LOGIN_URL") +
                "\n\nChange this temporary password on your first sign-in. " +
                "If you receive more than one welcome message after a setup retry, use the latest one.\n\nUniversity Internship Coordination");
        }

        public Task SendResetAsync(string email, string name, string link)
        {
            return SendAsync(email, "Reset your internship account password",
                "Hello " + name + ",\n\nYour university coordinator requested a password reset.\n" +
                "Use this secure link to choose a new password:\n\n" + link +
                "\n\nIf you did not expect this message, contact your coordinator.\n\nUniversity Internship Coordination");
        }

        private async Task SendAsync(string email, string subject, string body)
        {
            ValidateConfiguration();
            using (var message = new MailMessage())
            using (var smtp = new SmtpClient(Setting("UIMS_SMTP_HOST"), int.Parse(Setting("UIMS_SMTP_PORT") ?? "587")))
            {
                message.From = new MailAddress(Setting("UIMS_SMTP_FROM"), "University Internship Coordination");
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(Setting("UIMS_SMTP_USERNAME"), Setting("UIMS_SMTP_PASSWORD"));
                smtp.EnableSsl = true;
                var send = smtp.SendMailAsync(message);
                if (await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(35))) != send)
                {
                    // Disposing the client cancels the outstanding network operation.
                    // Observe any eventual exception without treating timeout as successful delivery.
                    var observation = send.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException("Email sending timed out; delivery was not confirmed.");
                }
                await send; // SMTP acceptance is not a guarantee of arrival in the inbox.
            }
        }
    }
}
