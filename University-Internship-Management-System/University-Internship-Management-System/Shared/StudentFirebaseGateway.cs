using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    public sealed class StudentFirebaseGateway : IStudentDatabase, ICompanyAuthentication
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        private static readonly object Initialization = new object();

        private static FirebaseAuth Authentication
        {
            get
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    lock (Initialization)
                    {
                        if (FirebaseApp.DefaultInstance == null)
                            FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.GetApplicationDefault() });
                    }
                }
                return FirebaseAuth.DefaultInstance;
            }
        }

        private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, object value = null)
        {
            Uri baseUri;
            string configured = ConfigurationManager.AppSettings["FirebaseDatabaseUrl"];
            if (!Uri.TryCreate(configured, UriKind.Absolute, out baseUri) || baseUri.Scheme != "https" ||
                !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.UserInfo))
                throw new InvalidOperationException("Configure a valid HTTPS FirebaseDatabaseUrl.");
            var credential = (await GoogleCredential.GetApplicationDefaultAsync()).CreateScoped(
                "https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
            string token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            var request = new HttpRequestMessage(method, configured.TrimEnd('/') + "/" + path + ".json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (value != null) request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            return request;
        }

        private static async Task<JToken> ReadBodyAsync(HttpResponseMessage response)
        {
            // Never include remote response bodies or bearer tokens in user-visible errors.
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(json) ? JValue.CreateNull() : JToken.Parse(json);
        }

        public async Task<JToken> ReadAsync(string path)
        {
            using (var request = await RequestAsync(HttpMethod.Get, path))
            using (var response = await Client.SendAsync(request))
                return await ReadBodyAsync(response);
        }

        public async Task WriteAsync(string path, object value)
        {
            using (var request = await RequestAsync(HttpMethod.Put, path, value))
            using (var response = await Client.SendAsync(request))
                response.EnsureSuccessStatusCode();
        }

        public async Task PatchAsync(string path, object value)
        {
            using (var request = await RequestAsync(new HttpMethod("PATCH"), path, value))
            using (var response = await Client.SendAsync(request))
                response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string path)
        {
            using (var request = await RequestAsync(HttpMethod.Delete, path))
            using (var response = await Client.SendAsync(request))
                response.EnsureSuccessStatusCode();
        }

        public async Task<StudentVersion> ReadVersionAsync(string path)
        {
            using (var request = await RequestAsync(HttpMethod.Get, path))
            {
                request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
                using (var response = await Client.SendAsync(request))
                {
                    var value = await ReadBodyAsync(response);
                    // Echo Firebase's raw version header; its unquoted value can be
                    // discarded by the typed .NET ETag parser. Never assume a missing
                    // header means the existing operation is a null node.
                    return new StudentVersion { Value = value as JObject, ETag = FirebaseDatabaseVersion.ReadETag(response) };
                }
            }
        }

        public async Task<bool> CompareWriteAsync(string path, object value, string etag)
        {
            using (var request = await RequestAsync(HttpMethod.Put, path, value))
            {
                request.Headers.TryAddWithoutValidation("if-match", etag);
                using (var response = await Client.SendAsync(request))
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
            try { return Map(await Authentication.GetUserAsync(uid)); }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { return null; }
        }

        public async Task<StudentLoginAccount> FindByEmailAsync(string email)
        {
            try { return Map(await Authentication.GetUserByEmailAsync(email)); }
            catch (FirebaseAuthException error) when (error.AuthErrorCode == AuthErrorCode.UserNotFound) { return null; }
        }

        public async Task CreateAsync(string uid, string email, string name, string password)
        {
            await Authentication.CreateUserAsync(new UserRecordArgs
            {
                Uid = uid, Email = email, DisplayName = name, Password = password,
                Disabled = true, EmailVerified = false
            });
        }

        public async Task UpdateAsync(string uid, bool disabled, string password = null)
        {
            var args = new UserRecordArgs { Uid = uid, Disabled = disabled };
            if (password != null) args.Password = password;
            await Authentication.UpdateUserAsync(args);
        }

        async Task IStudentAuthentication.DeleteAsync(string uid)
        {
            try { await Authentication.DeleteUserAsync(uid); }
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
