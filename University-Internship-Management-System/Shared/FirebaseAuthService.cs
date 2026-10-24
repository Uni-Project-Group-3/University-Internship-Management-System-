using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    internal sealed class FirebaseAuthService
    {
        private static readonly HttpClient SharedClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private readonly HttpClient client;
        private readonly string apiKey;

        public FirebaseAuthService()
            : this(SharedClient, FirebaseSignInService.ApiKey())
        {
        }

        internal FirebaseAuthService(HttpClient client, string apiKey)
        {
            this.client = client;
            this.apiKey = apiKey;
        }

        public async Task CompletePasswordResetAsync(string code, string password)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(code) || code.Length > 2048)
                throw new StudentManagementException("This password link is invalid. Request a new one from Forgot Password.");
            string url = "https://identitytoolkit.googleapis.com/v1/accounts:resetPassword?key=" + Uri.EscapeDataString(apiKey);
            using (var content = new StringContent(JsonConvert.SerializeObject(new { oobCode = code, newPassword = password }), Encoding.UTF8, "application/json"))
            using (var response = await client.PostAsync(url, content).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    throw new StudentManagementException("This link may have expired or been used, or the password was rejected. Request a new link from Forgot Password and try again.");
                // Only Firebase's successfully redeemed code identifies the account to update.
                // Never trust an email or UID supplied in the browser URL.
                var completed = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                string email = (string)completed["email"];
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var gateway = new StudentFirebaseGateway();
                    var account = await gateway.FindByEmailAsync(email);
                    if (account != null)
                    {
                        var profile = await gateway.ReadAsync("users/" + StudentManagementService.Key(account.Uid));
                        if (profile is JObject && profile.Value<bool?>("mustChangePassword") == true)
                            await gateway.PatchAsync("users/" + StudentManagementService.Key(account.Uid), new { mustChangePassword = false });
                    }
                }
            }
        }

        public static bool IsReservedEmail(string email)
        {
            var domain = new System.Net.Mail.MailAddress(email).Host.ToLowerInvariant();
            return domain == "example.com" || domain == "example.net" || domain == "example.org" ||
                domain == "localhost" || domain.EndsWith(".localhost") || domain.EndsWith(".invalid") ||
                domain.EndsWith(".test") || domain.EndsWith(".example");
        }

        // Coordinator-only callers need explicit acceptance, unlike the public anti-enumeration flow.
        public async Task SendAccountSetupEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Firebase email is not configured.");
            if (string.IsNullOrWhiteSpace(email) || IsReservedEmail(email))
                throw new StudentManagementException("This address cannot receive email. Update the university record with a real email address to receive the password setup link.");
            string url = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + Uri.EscapeDataString(apiKey);
            string payload = JsonConvert.SerializeObject(new { requestType = "PASSWORD_RESET", email });
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            using (var response = await client.PostAsync(url, content).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    throw new StudentManagementException("The account is saved, but Firebase did not accept the password setup email. Check the registered email and retry in a minute.");
            }
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Firebase password recovery is not configured.");
            }

            string url = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" +
                Uri.EscapeDataString(apiKey);
            string payload = JsonConvert.SerializeObject(new { requestType = "PASSWORD_RESET", email });
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            using (var response = await client.PostAsync(url, content).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode || (int)response.StatusCode == 429)
                {
                    return;
                }

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                string code = (string)JObject.Parse(body)["error"]?["message"];
                // Mask account-specific responses, including throttling, to avoid enumeration.
                switch (code)
                {
                    case "EMAIL_NOT_FOUND":
                    case "USER_NOT_FOUND":
                    case "USER_DISABLED":
                    case "TOO_MANY_ATTEMPTS_TRY_LATER":
                    case "RESET_PASSWORD_EXCEED_LIMIT":
                    case "QUOTA_EXCEEDED":
                        return;
                    default:
                        throw new HttpRequestException("Password recovery is temporarily unavailable.");
                }
            }
        }
    }
}
