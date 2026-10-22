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
            : this(SharedClient, Environment.GetEnvironmentVariable("FIREBASE_API_KEY"))
        {
        }

        internal FirebaseAuthService(HttpClient client, string apiKey)
        {
            this.client = client;
            this.apiKey = apiKey;
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
