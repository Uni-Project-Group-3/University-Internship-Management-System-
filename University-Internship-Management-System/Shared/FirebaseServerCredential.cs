using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace University_Internship_Management_System.Shared
{
    // Reuse Google's scoped credential and its token cache. Business data is never cached here.
    public static class FirebaseServerCredential
    {
        private static readonly Lazy<Task<GoogleCredential>> Credential = new Lazy<Task<GoogleCredential>>(Load);
        private static async Task<GoogleCredential> Load()
        {
            return (await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false)).CreateScoped(
                "https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
        }
        public static async Task<string> AccessTokenAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var credential = await Credential.Value.ConfigureAwait(false);
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellation).ConfigureAwait(false);
        }
    }
}
