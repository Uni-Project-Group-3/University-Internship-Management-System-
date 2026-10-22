using System;
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

    public sealed class GoogleAdcAccessTokenProvider : IAccessTokenProvider
    {
        private readonly Lazy<Task<GoogleCredential>> credential = new Lazy<Task<GoogleCredential>>(LoadAsync);
        private static async Task<GoogleCredential> LoadAsync()
        {
            // GOOGLE_APPLICATION_CREDENTIALS points to a local file outside the repository.
            // ADC also supports an attached service account on Google-hosted infrastructure.
            var value = await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false);
            return value.CreateScoped("https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
        }
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var value = await credential.Value.ConfigureAwait(false);
                return await value.UnderlyingCredential.GetAccessTokenForRequestAsync().ConfigureAwait(false);
            }
            catch (Exception) { throw new RepositoryException("Server application credentials are unavailable or could not be refreshed."); }
        }
    }
}
