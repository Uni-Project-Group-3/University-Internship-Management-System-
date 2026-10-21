using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Web;
using System.Web.Caching;

namespace University_Internship_Management_System.Services
{
    public sealed class TabSession
    {
        public string IdToken { get; set; }
        internal string RefreshToken { get; set; }
        internal string PendingPasswordCompletion { get; set; }
        internal DateTime TokenExpiresUtc { get; set; }
        internal SemaphoreSlim RefreshLock = new SemaphoreSlim(1, 1);
        public string Uid { get; set; }
        public string SecurityVersion { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }

    public static class SessionService
    {
        private const string Prefix = "Uims.Tab.";
        public static string Create(VerifiedIdentity identity, string token, string securityVersion = "", string refreshToken = null)
        {
            byte[] bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            string handle = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            DateTime expiry = DateTime.UtcNow.AddMinutes(30);
            if (refreshToken == null && identity.ExpiresUtc < expiry) expiry = identity.ExpiresUtc;
            HttpRuntime.Cache.Insert(Prefix + handle, new TabSession { Uid = identity.Uid, IdToken = token, ExpiresUtc = expiry, SecurityVersion = securityVersion, RefreshToken = refreshToken, TokenExpiresUtc = identity.ExpiresUtc },
                null, expiry, Cache.NoSlidingExpiration);
            return handle;
        }

        public static TabSession Get(string handle)
        {
            if (string.IsNullOrEmpty(handle) || handle.Length != 43) throw LoginDiagnostics.Reject("TabTicketAuthorizationFailure");
            var session = HttpRuntime.Cache[Prefix + handle] as TabSession;
            if (session == null || session.ExpiresUtc <= DateTime.UtcNow) throw new UnauthorizedAccessException();
            return session;
        }
        public static async Task<VerifiedIdentity> VerifyAsync(string handle, IFirebaseIdentityVerifier verifier = null, FirebaseRestAuthenticationService auth = null)
        {
            var session = Get(handle);
            await session.RefreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Get(handle);
                if (session.TokenExpiresUtc <= DateTime.UtcNow.AddMinutes(2))
                {
                    if (string.IsNullOrEmpty(session.RefreshToken)) throw new UnauthorizedAccessException();
                    var tokens = await (auth ?? new FirebaseRestAuthenticationService()).RefreshAsync(session.RefreshToken).ConfigureAwait(false);
                    if (tokens.Uid != session.Uid) throw new UnauthorizedAccessException();
                    var refreshed = await (verifier ?? new FirebaseTokenVerifier()).VerifyAsync(tokens.IdToken).ConfigureAwait(false);
                    if (refreshed.Uid != session.Uid) throw new UnauthorizedAccessException();
                    session.IdToken = tokens.IdToken; session.RefreshToken = tokens.RefreshToken;
                    session.TokenExpiresUtc = refreshed.ExpiresUtc;
                }
                var identity = await (verifier ?? new FirebaseTokenVerifier()).VerifyAsync(session.IdToken).ConfigureAwait(false);
                if (identity.Uid != session.Uid) throw new UnauthorizedAccessException();
                Get(handle);
                return identity;
            }
            catch (UnauthorizedAccessException) { Logout(handle); throw; }
            finally { session.RefreshLock.Release(); }
        }
        public static void RequireCurrent(TabSession session, UimsAccount account)
        {
            if (session.Uid != account.Uid || session.SecurityVersion != account.SecurityVersion)
                throw new UnauthorizedAccessException();
        }
        internal static void RevokeOtherTabs(string uid, string currentHandle)
        {
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
                if (entry.Key is string key && key.StartsWith(Prefix, StringComparison.Ordinal) && key != Prefix + currentHandle &&
                    entry.Value is TabSession other && other.Uid == uid) HttpRuntime.Cache.Remove(key);
        }
        public static void Logout(string handle) { if (!string.IsNullOrEmpty(handle)) HttpRuntime.Cache.Remove(Prefix + handle); }
        public static string Handle(HttpRequest request)
        {
            string header = request.Headers["Authorization"];
            return header != null && header.StartsWith("Bearer ", StringComparison.Ordinal) ? header.Substring(7) : request.Form["UimsTabTicket"];
        }
    }
}
