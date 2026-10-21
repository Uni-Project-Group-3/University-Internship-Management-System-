using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    // Uses Google's signed-token verifier and Firebase's public signing keys. No administrator identity.
    public sealed class FirebaseTokenVerifier : IFirebaseIdentityVerifier
    {
        private const string KeysUrl = "https://www.googleapis.com/service_accounts/v1/jwk/securetoken@system.gserviceaccount.com";
        private static readonly SemaphoreSlim KeysLock = new SemaphoreSlim(1, 1);
        private static HashSet<string> keyIds = new HashSet<string>();
        private static DateTime keysExpireUtc;
        // Allow small issuer/server clock skew; token expiration and business deadlines have no added grace.
        private static readonly TimeSpan IssuedAtTolerance = TimeSpan.FromSeconds(60);
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public async Task<VerifiedIdentity> VerifyAsync(string idToken)
        {
            try
            {
                var parts = Parse(idToken);
                ValidateClaims(parts.Header, parts.Payload, DateTime.UtcNow);
                string kid = (string)parts.Header["kid"];
                await RequireSigningKeyAsync(kid).ConfigureAwait(false);
                var options = new SignedTokenVerificationOptions
                {
                    CertificatesUrl = KeysUrl + "?keyId=" + Uri.EscapeDataString(kid),
                    ExpiryClockTolerance = TimeSpan.Zero, IssuedAtClockTolerance = IssuedAtTolerance
                };
                options.TrustedIssuers.Add("https://securetoken.google.com/" + FirebaseConfiguration.ProjectId);
                options.TrustedAudiences.Add(FirebaseConfiguration.ProjectId);
                await JsonWebSignature.VerifySignedTokenAsync(idToken, options).ConfigureAwait(false);
                // The live Auth lookup checks the current identity, disabled state and revocation epoch.
                var result = await FirebaseHttp.PostIdentityAsync("lookup", new { idToken }).ConfigureAwait(false);
                var users = result["users"] as JArray;
                var user = users?.Count == 1 ? users[0] as JObject : null;
                string uid = (string)parts.Payload["sub"];
                long authTime = (long)parts.Payload["auth_time"];
                long validSince;
                if (user == null || (string)user["localId"] != uid || (bool?)user["disabled"] == true ||
                    long.TryParse((string)user["validSince"], out validSince) && authTime < validSince)
                    throw new UnauthorizedAccessException();
                return new VerifiedIdentity { Uid = uid, Email = (string)user["email"],
                    ExpiresUtc = Epoch.AddSeconds((long)parts.Payload["exp"]) };
            }
            catch (UnauthorizedAccessException) { LoginDiagnostics.Write("FirebaseUidVerificationFailure"); throw; }
            catch (InvalidJwtException error)
            {
                string reason;
                switch (error.Message)
                {
                    case "JWT is not yet valid.": reason = "TokenIssuedInFuture"; break;
                    case "JWT has expired.": reason = "TokenExpired"; break;
                    case "JWT invalid, unable to verify signature.": reason = "TokenSignatureInvalid"; break;
                    case "JWT contains untrusted 'aud' claim.": reason = "TokenAudienceInvalid"; break;
                    default: reason = "TokenLibraryValidationRejected"; break;
                }
                throw LoginDiagnostics.Reject(reason);
            }
            catch (JsonException) { throw new UnauthorizedAccessException("The Firebase token is invalid."); }
            catch (FormatException) { throw new UnauthorizedAccessException("The Firebase token is invalid."); }
        }

        public static void ValidateClaims(JObject header, JObject payload, DateTime utcNow)
        {
            long now = (long)(utcNow.ToUniversalTime() - Epoch).TotalSeconds;
            string uid = payload["sub"]?.Type == JTokenType.String ? (string)payload["sub"] : null;
            if ((string)header["alg"] != "RS256" || header["kid"]?.Type != JTokenType.String || string.IsNullOrEmpty((string)header["kid"]))
                throw LoginDiagnostics.Reject("TokenHeaderInvalid");
            if (payload["aud"]?.Type != JTokenType.String || (string)payload["aud"] != FirebaseConfiguration.ProjectId)
                throw LoginDiagnostics.Reject("TokenAudienceInvalid");
            if ((string)payload["iss"] != "https://securetoken.google.com/" + FirebaseConfiguration.ProjectId)
                throw LoginDiagnostics.Reject("TokenIssuerInvalid");
            if (string.IsNullOrEmpty(uid) || uid.Length > 128) throw LoginDiagnostics.Reject("TokenSubjectInvalid");
            if (payload["exp"]?.Type != JTokenType.Integer || (long)payload["exp"] <= now) throw LoginDiagnostics.Reject("TokenExpired");
            if (payload["iat"]?.Type != JTokenType.Integer || (long)payload["iat"] <= 0) throw LoginDiagnostics.Reject("TokenIssuedAtInvalid");
            if ((long)payload["iat"] > now + (long)IssuedAtTolerance.TotalSeconds) throw LoginDiagnostics.Reject("TokenIssuedInFuture");
            if (payload["auth_time"]?.Type != JTokenType.Integer || (long)payload["auth_time"] <= 0) throw LoginDiagnostics.Reject("TokenAuthTimeInvalid");
            if ((long)payload["auth_time"] > now + (long)IssuedAtTolerance.TotalSeconds) throw LoginDiagnostics.Reject("TokenAuthTimeInFuture");
        }

        private static (JObject Header, JObject Payload) Parse(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 16000) throw new UnauthorizedAccessException();
            var parts = token.Split('.');
            if (parts.Length != 3 || parts.Any(string.IsNullOrEmpty)) throw new UnauthorizedAccessException();
            return (Decode(parts[0]), Decode(parts[1]));
        }
        private static JObject Decode(string part)
        {
            string value = part.Replace('-', '+').Replace('_', '/');
            value = value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');
            return JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(value)),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        private static async Task RequireSigningKeyAsync(string kid)
        {
            await KeysLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (keysExpireUtc <= DateTime.UtcNow)
                {
                    using (var response = await FirebaseHttp.Client.GetAsync(KeysUrl).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var keys = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        keyIds = new HashSet<string>(((JArray)keys["keys"]).Select(key => (string)key["kid"]));
                        var age = response.Headers.CacheControl?.MaxAge ?? TimeSpan.FromMinutes(5);
                        keysExpireUtc = DateTime.UtcNow.Add(age > TimeSpan.FromHours(6) ? TimeSpan.FromHours(6) : age);
                    }
                }
                if (!keyIds.Contains(kid)) throw LoginDiagnostics.Reject("TokenSigningKeyUnknown");
            }
            finally { KeysLock.Release(); }
        }
    }
}
