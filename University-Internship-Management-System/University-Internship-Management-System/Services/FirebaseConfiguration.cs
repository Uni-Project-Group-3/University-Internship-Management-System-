using System;
using System.Configuration;
namespace UIMS.Services
{
    public sealed class FirebaseConfiguration
    {
        public bool Enabled { get; private set; }
        public bool LiveWritesEnabled { get; private set; }
        public bool TaskSchemaValidated { get; private set; }
        public string ProjectId { get; private set; }
        public string DatabaseUrl { get; private set; }
        public string ApiKey { get; private set; }
        public FirebaseConfiguration() : this(ConfigurationManager.AppSettings["FirebaseEnabled"], ConfigurationManager.AppSettings["FirebaseProjectId"], ConfigurationManager.AppSettings["FirebaseDatabaseUrl"], Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ?? ConfigurationManager.AppSettings["FirebaseApiKey"], ConfigurationManager.AppSettings["FirebaseLiveWritesEnabled"], ConfigurationManager.AppSettings["FirebaseTaskSchemaValidated"]) { }
        public FirebaseConfiguration(string enabled, string projectId, string url, string apiKey, string writes = "false", string schema = "false")
        {
            Enabled = string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase);
            LiveWritesEnabled = string.Equals(writes, "true", StringComparison.OrdinalIgnoreCase);
            TaskSchemaValidated = string.Equals(schema, "true", StringComparison.OrdinalIgnoreCase);
            ProjectId = projectId; DatabaseUrl = (url ?? "").TrimEnd('/'); ApiKey = apiKey;
        }
        public void EnsureEnabled()
        {
            if (!Enabled) throw new FirebaseException(503, "Firebase connection not configured. Integration is disabled.");
            Uri url;
            if (string.IsNullOrWhiteSpace(ProjectId) || string.IsNullOrWhiteSpace(ApiKey) || !Uri.TryCreate(DatabaseUrl, UriKind.Absolute, out url) || url.Scheme != "https" || !url.IsDefaultPort || url.UserInfo != "" || url.Query != "" || url.Fragment != "" || url.AbsolutePath != "/" || !(url.Host.EndsWith(".firebaseio.com", StringComparison.OrdinalIgnoreCase) || url.Host.EndsWith(".firebasedatabase.app", StringComparison.OrdinalIgnoreCase)))
                throw new FirebaseException(503, "Configure the matching Firebase project, HTTPS database root and Web API key.");
        }
        public void EnsureWritesAllowed()
        {
            EnsureEnabled();
            if (!LiveWritesEnabled || !TaskSchemaValidated) throw new FirebaseException(503, "Live writes are disabled until schema, authorization and development approval are validated.");
        }
    }
    public sealed class FirebaseException : Exception
    {
        public int StatusCode { get; private set; }
        public FirebaseException(int status, string message) : base(message) { StatusCode = status; }
    }
}
