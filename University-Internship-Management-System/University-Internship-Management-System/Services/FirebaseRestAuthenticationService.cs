using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public sealed class FirebaseLoginTokens
    {
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public string Uid { get; set; }
        public string Email { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }
    public sealed class FirebaseRestAuthenticationService
    {
        private readonly HttpClient client;
        public FirebaseRestAuthenticationService(HttpClient client = null) { this.client = client ?? FirebaseHttp.Client; }
        public async Task<FirebaseLoginTokens> SignInAsync(string email, string password)
        {
            using (var content = new StringContent(JsonConvert.SerializeObject(new { email, password, returnSecureToken = true }), Encoding.UTF8, "application/json"))
            {
                var result = await SendAsync("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword", content).ConfigureAwait(false);
                return ParseTokens(result, false);
            }
        }
        public async Task<FirebaseLoginTokens> ReauthenticateAndChangePasswordAsync(string expectedUid, string email,
            string currentPassword, string newPassword, string confirmation, IFirebaseIdentityVerifier verifier = null)
        {
            AccountLifecycleService.ValidatePassword(newPassword, confirmation);
            var identityVerifier = verifier ?? new FirebaseTokenVerifier();
            var authenticated = await SignInAsync(email, currentPassword).ConfigureAwait(false);
            var identity = await identityVerifier.VerifyAsync(authenticated.IdToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(expectedUid) || identity.Uid != expectedUid || authenticated.Uid != expectedUid ||
                !string.Equals(identity.Email, email, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException();
            var changed = await ChangePasswordAsync(authenticated.IdToken, newPassword).ConfigureAwait(false);
            var updated = await identityVerifier.VerifyAsync(changed.IdToken).ConfigureAwait(false);
            if (changed.Uid != expectedUid || updated.Uid != expectedUid ||
                !string.Equals(updated.Email, identity.Email, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException();
            // Credential change is distinct from UIMS first-login policy. This method never changes profile flags.
            return changed;
        }

        public async Task<FirebaseLoginTokens> ChangePasswordAsync(string idToken, string password)
        {
            using (var content = new StringContent(JsonConvert.SerializeObject(new { idToken, password, returnSecureToken = true }), Encoding.UTF8, "application/json"))
                return ParseTokens(await SendAsync("https://identitytoolkit.googleapis.com/v1/accounts:update", content).ConfigureAwait(false), false);
        }

        public async Task<FirebaseLoginTokens> RefreshAsync(string refreshToken)
        {
            using (var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "grant_type", "refresh_token" }, { "refresh_token", refreshToken } }))
                return ParseTokens(await SendAsync("https://securetoken.googleapis.com/v1/token", content).ConfigureAwait(false), true);
        }
        private async Task<JObject> SendAsync(string endpoint, HttpContent content)
        {
            using (var response = await client.PostAsync(endpoint + "?key=" + Uri.EscapeDataString(FirebaseConfiguration.WebApiKey), content).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) await LoginDiagnostics.HttpFailureAsync(response, "FirebaseRestAuthenticationRejected").ConfigureAwait(false);
                if ((int)response.StatusCode == 400 || (int)response.StatusCode == 401 || (int)response.StatusCode == 403)
                    throw new UnauthorizedAccessException("Sign-in was not accepted. Check your credentials or contact the coordinator.");
                if ((int)response.StatusCode == 429) throw new HttpRequestException("Too many sign-in attempts. Please try again later.");
                if (!response.IsSuccessStatusCode) throw new HttpRequestException("Firebase authentication is temporarily unavailable.");
                return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }
        public static FirebaseLoginTokens ParseTokens(JObject result, bool refresh)
        {
            int seconds;
            string token = (string)result[refresh ? "id_token" : "idToken"], renewal = (string)result[refresh ? "refresh_token" : "refreshToken"],
                uid = (string)result[refresh ? "user_id" : "localId"];
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(renewal) || string.IsNullOrWhiteSpace(uid) ||
                !int.TryParse((string)result[refresh ? "expires_in" : "expiresIn"], out seconds) || seconds <= 0 || seconds > 86400)
                throw new UnauthorizedAccessException("Firebase returned an incomplete identity.");
            return new FirebaseLoginTokens { IdToken = token, RefreshToken = renewal, Uid = uid,
                Email = (string)result["email"], ExpiresUtc = DateTime.UtcNow.AddSeconds(seconds) };
        }
    }
}
