using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class FirebaseFinalEvaluationConfiguration
    {
        private readonly UIMS.Services.FirebaseConfiguration firebase;
        public Uri DatabaseRoot { get; private set; }
        public bool Enabled { get { return firebase.Enabled; } }
        public bool SchemaValidated { get; private set; }
        public FirebaseFinalEvaluationConfiguration() : this(new UIMS.Services.FirebaseConfiguration()) { }
        public FirebaseFinalEvaluationConfiguration(UIMS.Services.FirebaseConfiguration configuration)
        {
            firebase = configuration ?? throw new ArgumentNullException(nameof(configuration));
            SchemaValidated = string.Equals(System.Configuration.ConfigurationManager.AppSettings["FirebaseFinalEvaluationSchemaValidated"], "true", StringComparison.OrdinalIgnoreCase);
            Uri root;
            if (!Uri.TryCreate(firebase.DatabaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out root) || root.Scheme != "https" ||
                !string.IsNullOrEmpty(root.UserInfo) || root.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(root.Query) || !string.IsNullOrEmpty(root.Fragment) || !root.IsDefaultPort ||
                !(root.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) || root.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Configure the shared HTTPS FirebaseDatabaseUrl in Web.config.");
            DatabaseRoot = root;
        }
        // Explicit root injection supports isolated tests with a fake HttpMessageHandler.
        public FirebaseFinalEvaluationConfiguration(string databaseRoot) : this(new UIMS.Services.FirebaseConfiguration("true", "offline-test", databaseRoot, "offline-test-key", "true", "true", "true")) { SchemaValidated = true; }
        public void EnsureEnabled() { firebase.EnsureEnabled(); }
        public void EnsureWritesAllowed() { firebase.EnsureWritesAllowed(); if (!SchemaValidated) throw new UIMS.Services.FirebaseException(503, "Final-evaluation Firebase writes are disabled until its existing schema is validated."); }
    }

    internal static class FirebaseReadDiagnostics
    {
        internal static Stopwatch Start() { return Stopwatch.StartNew(); }
        internal static void Finish(string operation, Stopwatch timer, string result)
        {
            Trace.TraceInformation("UIMS Firebase read operation={0} result={1} elapsedMs={2}", operation, result, timer.ElapsedMilliseconds);
        }
    }

    public sealed class GoogleAdcAccessTokenProvider : ICancellableAccessTokenProvider
    {
        private static readonly TimeSpan TokenTimeout = TimeSpan.FromSeconds(12);
        private readonly SemaphoreSlim credentialLock = new SemaphoreSlim(1, 1);
        private GoogleCredential credential;

        private async Task<GoogleCredential> GetCredentialAsync(CancellationToken cancellationToken)
        {
            if (credential != null) return credential;
            await credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (credential == null)
                {
                    // ADC is loaded only on the server. The caller's bounded token also limits
                    // metadata/OAuth credential discovery when this machine uses remote ADC.
                    var value = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken).ConfigureAwait(false);
                    credential = value.CreateScoped("https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
                }
                return credential;
            }
            finally { credentialLock.Release(); }
        }

        public Task<string> GetAccessTokenAsync()
        {
            return GetAccessTokenAsync(CancellationToken.None);
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var timer = FirebaseReadDiagnostics.Start();
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(TokenTimeout);
                try
                {
                    var value = await GetCredentialAsync(timeout.Token).ConfigureAwait(false);
                    var token = await value.UnderlyingCredential.GetAccessTokenForRequestAsync(null, timeout.Token).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(token)) throw new RepositoryException("Google OAuth did not return an access token.");
                    FirebaseReadDiagnostics.Finish("oauth.token", timer, "success");
                    return token;
                }
                catch (OperationCanceledException error)
                {
                    FirebaseReadDiagnostics.Finish("oauth.token", timer, "timeout_or_cancelled");
                    throw new RepositoryException("Google OAuth token retrieval timed out or was canceled.", error);
                }
                catch (RepositoryException)
                {
                    FirebaseReadDiagnostics.Finish("oauth.token", timer, "failed");
                    throw;
                }
                catch (Exception error)
                {
                    FirebaseReadDiagnostics.Finish("oauth.token", timer, "credentials_or_network_error");
                    throw new RepositoryException("Server application credentials are unavailable or could not be refreshed.", error);
                }
            }
        }
    }
}
