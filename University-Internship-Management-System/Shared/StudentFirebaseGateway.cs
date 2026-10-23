using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Services.FinalEvaluation;

namespace University_Internship_Management_System.Shared
{
    public sealed class StudentFirebaseGateway : IStudentDatabase, ICompanyAuthentication
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private static readonly GoogleAdcAccessTokenProvider DefaultTokens = new GoogleAdcAccessTokenProvider();
        private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(8);
        private static readonly TimeSpan AuthenticationLookupTimeout = TimeSpan.FromSeconds(15);
        private static readonly object Initialization = new object();
        private readonly UIMS.Services.FirebaseConfiguration firebaseConfiguration;
        private readonly IAccessTokenProvider tokens;
        private readonly CancellationToken requestCancellation;
        public StudentFirebaseGateway() : this(new UIMS.Services.FirebaseConfiguration(), DefaultTokens, CancellationToken.None) { }
        public StudentFirebaseGateway(UIMS.Services.FirebaseConfiguration configuration)
            : this(configuration, DefaultTokens, CancellationToken.None) { }
        public StudentFirebaseGateway(UIMS.Services.FirebaseConfiguration configuration, IAccessTokenProvider tokenProvider)
            : this(configuration, tokenProvider, CancellationToken.None) { }
        public StudentFirebaseGateway(UIMS.Services.FirebaseConfiguration configuration, IAccessTokenProvider tokenProvider, CancellationToken cancellationToken)
        {
            firebaseConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            tokens = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            requestCancellation = cancellationToken;
        }
        private CancellationTokenSource CreateOperationCancellation()
        {
            var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellation);
            timeout.CancelAfter(OperationTimeout);
            return timeout;
        }

        private FirebaseAuth Authentication
        {
            get
            {
                firebaseConfiguration.EnsureAuthenticationEnabled();
                if (FirebaseApp.DefaultInstance == null)
                {
                    lock (Initialization)
                    {
                        if (FirebaseApp.DefaultInstance == null)
                            FirebaseApp.Create(new AppOptions
                            {
                                Credential = GoogleCredential.GetApplicationDefault(),
                                ProjectId = firebaseConfiguration.ProjectId
                            });
                    }
                }
                return FirebaseAuth.DefaultInstance;
            }
        }

        private async Task<FirebaseAuth> AuthenticationAsync(CancellationToken cancellationToken)
        {
            firebaseConfiguration.EnsureAuthenticationEnabled();
            var app = FirebaseApp.DefaultInstance;
            if (app != null) return FirebaseAuth.GetAuth(app);

            // Load ADC asynchronously so an IIS request can cancel local credential discovery
            // or metadata-server access instead of blocking before FirebaseAuth receives a token.
            var timer = FirebaseReadDiagnostics.Start();
            GoogleCredential credential;
            try
            {
                credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken).ConfigureAwait(false);
                FirebaseReadDiagnostics.Finish("firebase.auth.credentials", timer, "success");
            }
            catch (OperationCanceledException)
            {
                FirebaseReadDiagnostics.Finish("firebase.auth.credentials", timer, "timeout_or_cancelled");
                throw;
            }
            catch
            {
                FirebaseReadDiagnostics.Finish("firebase.auth.credentials", timer, "credentials_or_network_error");
                throw;
            }

            lock (Initialization)
            {
                app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                        ProjectId = firebaseConfiguration.ProjectId
                    });
                }
                return FirebaseAuth.GetAuth(app);
            }
        }

        private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, object value, CancellationToken cancellationToken)
        {
            var firebase = firebaseConfiguration;
            firebase.EnsureEnabled();
            if (method != HttpMethod.Get) firebase.EnsureWritesAllowed();
            Uri baseUri;
            string configured = firebase.DatabaseUrl;
            if (!Uri.TryCreate(configured, UriKind.Absolute, out baseUri) || baseUri.Scheme != "https" ||
                !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.UserInfo))
                throw new InvalidOperationException("Configure a valid HTTPS FirebaseDatabaseUrl.");
            var cancellable = tokens as ICancellableAccessTokenProvider;
            string token = cancellable == null
                ? await tokens.GetAccessTokenAsync().ConfigureAwait(false)
                : await cancellable.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var request = new HttpRequestMessage(method, configured.TrimEnd('/') + "/" + path + ".json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (value != null) request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            return request;
        }
        private Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, object value = null)
        { return RequestAsync(method, path, value, CancellationToken.None); }

        private static async Task<JToken> ReadBodyAsync(HttpResponseMessage response, bool indexedQuery = false)
        {
            // Never include remote response bodies or bearer tokens in user-visible errors.
            if (!response.IsSuccessStatusCode)
            {
                int status = (int)response.StatusCode;
                if (indexedQuery && status == 400)
                {
                    string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (errorBody.IndexOf("Index not defined", StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new FirebaseQueryIndexException();
                }
                if (status == 401 || status == 403) throw new RepositoryException("Firebase denied the authenticated read (HTTP " + status + "). Verify the service account's database permission scope.");
                throw new RepositoryException("Firebase read failed (HTTP " + status + ").");
            }
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(json) ? JValue.CreateNull() : JToken.Parse(json);
        }

        public async Task<JToken> ReadAsync(string path)
        {
            var timer = FirebaseReadDiagnostics.Start();
            using (var timeout = CreateOperationCancellation())
            {
                try
                {
                    using (var request = await RequestAsync(HttpMethod.Get, path, null, timeout.Token).ConfigureAwait(false))
                    using (var response = await Client.SendAsync(request, timeout.Token).ConfigureAwait(false))
                    {
                        var body = await ReadBodyAsync(response).ConfigureAwait(false);
                        FirebaseReadDiagnostics.Finish("firebase.database.read", timer, "http_" + (int)response.StatusCode);
                        return body;
                    }
                }
                catch (OperationCanceledException error) { FirebaseReadDiagnostics.Finish("firebase.database.read", timer, "timeout_or_cancelled"); throw new RepositoryException("Firebase read timed out or was canceled.", error); }
                catch (RepositoryException error) { FirebaseReadDiagnostics.Finish("firebase.database.read", timer, error.Message.Contains("denied") ? "permission_denied" : "request_failed"); throw; }
                catch (HttpRequestException error) { FirebaseReadDiagnostics.Finish("firebase.database.read", timer, "network_or_permission_error"); throw new RepositoryException("Firebase read failed. Check connectivity and database permissions.", error); }
            }
        }

        public async Task<JToken> ReadEqualAsync(string path, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("A query field is required.", nameof(field));
            var timer = FirebaseReadDiagnostics.Start();
            using (var timeout = CreateOperationCancellation())
            {
                try
                {
                    using (var request = await RequestAsync(HttpMethod.Get, path, null, timeout.Token).ConfigureAwait(false))
                    {
                        string query = "?orderBy=" + Uri.EscapeDataString(JsonConvert.SerializeObject(field)) +
                            "&equalTo=" + Uri.EscapeDataString(JsonConvert.SerializeObject(value ?? ""));
                        request.RequestUri = new Uri(request.RequestUri.AbsoluteUri + query, UriKind.Absolute);
                        using (var response = await Client.SendAsync(request, timeout.Token).ConfigureAwait(false))
                        {
                            var body = await ReadBodyAsync(response, true).ConfigureAwait(false);
                            FirebaseReadDiagnostics.Finish("firebase.database.query", timer, "http_" + (int)response.StatusCode);
                            return body;
                        }
                    }
                }
                catch (OperationCanceledException error) { FirebaseReadDiagnostics.Finish("firebase.database.query", timer, "timeout_or_cancelled"); throw new RepositoryException("Firebase query timed out or was canceled.", error); }
                catch (FirebaseQueryIndexException) { FirebaseReadDiagnostics.Finish("firebase.database.query", timer, "index_required"); throw; }
                catch (RepositoryException error) { FirebaseReadDiagnostics.Finish("firebase.database.query", timer, error.Message.Contains("denied") ? "permission_denied" : "request_failed"); throw; }
                catch (HttpRequestException error) { FirebaseReadDiagnostics.Finish("firebase.database.query", timer, "network_or_permission_error"); throw new RepositoryException("Firebase query failed. Check connectivity and database permissions.", error); }
            }
        }

        // Confirms that the UID in the signed Forms ticket still exists and is enabled in Firebase Auth.
        public async Task<bool> IsActiveAuthenticationUidAsync(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid)) return false;
            var timer = FirebaseReadDiagnostics.Start();
            using (var timeout = CreateOperationCancellation())
            try
            {
                timeout.CancelAfter(AuthenticationLookupTimeout);
                var auth = await AuthenticationAsync(timeout.Token).ConfigureAwait(false);
                var user = await auth.GetUserAsync(uid, timeout.Token).ConfigureAwait(false);
                FirebaseReadDiagnostics.Finish("firebase.auth.get_user", timer, "success");
                return !user.Disabled;
            }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { FirebaseReadDiagnostics.Finish("firebase.auth.get_user", timer, "user_not_found"); return false; }
            catch (OperationCanceledException error) { FirebaseReadDiagnostics.Finish("firebase.auth.get_user", timer, "timeout_or_cancelled"); throw new RepositoryException("Firebase Authentication lookup timed out or was canceled.", error); }
            catch (UIMS.Services.FirebaseException) { FirebaseReadDiagnostics.Finish("firebase.auth.get_user", timer, "configuration_or_policy_error"); throw; }
            catch (FirebaseAuthException error) { FirebaseReadDiagnostics.Finish("firebase.auth.get_user", timer, "firebase_auth_error"); throw new RepositoryException("Firebase Authentication lookup failed. Verify Admin SDK access and network configuration.", error); }
            catch (Exception error) { FirebaseReadDiagnostics.Finish("firebase.auth.get_user", timer, "network_or_credentials_error"); throw new RepositoryException("Firebase Authentication lookup failed. Verify server credentials and network connectivity.", error); }
        }

        public async Task WriteAsync(string path, object value)
        {
            using (var request = await RequestAsync(HttpMethod.Put, path, value).ConfigureAwait(false))
            using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                response.EnsureSuccessStatusCode();
        }

        public async Task PatchAsync(string path, object value)
        {
            using (var request = await RequestAsync(new HttpMethod("PATCH"), path, value).ConfigureAwait(false))
            using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string path)
        {
            using (var request = await RequestAsync(HttpMethod.Delete, path).ConfigureAwait(false))
            using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                response.EnsureSuccessStatusCode();
        }

        public async Task<StudentVersion> ReadVersionAsync(string path)
        {
            using (var request = await RequestAsync(HttpMethod.Get, path).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
                using (var response = await Client.SendAsync(request))
                {
                    var value = await ReadBodyAsync(response).ConfigureAwait(false);
                    // Echo Firebase's raw version header; its unquoted value can be
                    // discarded by the typed .NET ETag parser. Never assume a missing
                    // header means the existing operation is a null node.
                    return new StudentVersion { Value = value as JObject, ETag = FirebaseDatabaseVersion.ReadETag(response) };
                }
            }
        }

        public async Task<bool> CompareWriteAsync(string path, object value, string etag)
        {
            using (var request = await RequestAsync(HttpMethod.Put, path, value).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("if-match", etag);
                    using (var response = await Client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) return false;
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
        }

        private static StudentLoginAccount Map(UserRecord user)
        {
            return new StudentLoginAccount { Uid = user.Uid, Email = user.Email, Disabled = user.Disabled };
        }

        public async Task<StudentLoginAccount> FindAsync(string uid)
        {
            try { return Map(await Authentication.GetUserAsync(uid).ConfigureAwait(false)); }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { return null; }
        }

        public async Task<StudentLoginAccount> FindByEmailAsync(string email)
        {
            try { return Map(await Authentication.GetUserByEmailAsync(email).ConfigureAwait(false)); }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { return null; }
        }

        public async Task CreateAsync(string uid, string email, string name, string password)
        {
            await Authentication.CreateUserAsync(new UserRecordArgs
            {
                Uid = uid, Email = email, DisplayName = name, Password = password,
                Disabled = true, EmailVerified = false
            }).ConfigureAwait(false);
        }

        public async Task UpdateAsync(string uid, bool disabled, string password = null)
        {
            var args = new UserRecordArgs { Uid = uid, Disabled = disabled };
            if (password != null) args.Password = password;
            await Authentication.UpdateUserAsync(args).ConfigureAwait(false);
        }

        async Task IStudentAuthentication.DeleteAsync(string uid)
        {
            try { await Authentication.DeleteUserAsync(uid).ConfigureAwait(false); }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { }
        }

        public Task UpdateProfileAsync(string uid, string email, string name)
        {
            return Authentication.UpdateUserAsync(new UserRecordArgs { Uid = uid, Email = email, DisplayName = name });
        }
        public Task<string> ResetLinkAsync(string email)
        {
            return Authentication.GeneratePasswordResetLinkAsync(email);
        }
    }
}
