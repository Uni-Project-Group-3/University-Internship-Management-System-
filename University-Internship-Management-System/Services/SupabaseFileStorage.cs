using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UIMS.Services
{
    // Provider transport only. Domain services authorize ownership before invoking it.
    public sealed partial class FirebaseFileStorageService
    {
        private readonly Func<string> supabaseSecretProvider;
        private static readonly HttpClient SupabaseTransport = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
        private readonly HttpClient supabaseClient;
        private string StorageSetting(string key) { return Environment.GetEnvironmentVariable("UIMS_" + key) ?? setting(key); }
        public string UploadProvider
        {
            get
            {
                string provider = setting("FileStorageProvider");
                if (string.IsNullOrWhiteSpace(provider)) provider = Environment.GetEnvironmentVariable("UIMS_FileStorageProvider");
                return string.IsNullOrWhiteSpace(provider) ? "firebase" : provider.Trim().ToLowerInvariant();
            }
        }
        private bool UseSupabase { get { return string.Equals(UploadProvider, "supabase", StringComparison.OrdinalIgnoreCase); } }
        private Uri SupabaseUri(string route)
        {
            Uri root;
            if (!Uri.TryCreate(StorageSetting("SupabaseStorageUrl"), UriKind.Absolute, out root) || root.Scheme != "https" || !root.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) || root.AbsolutePath != "/" || root.Query.Length != 0 || root.Fragment.Length != 0 || root.UserInfo.Length != 0 || !root.IsDefaultPort)
                throw new FirebaseException(503, "Configure the server-side Supabase Storage HTTPS project URL.");
            return new Uri(root, "storage/v1/" + route);
        }
        private string SupabaseBucket()
        {
            string bucket = StorageSetting("SupabaseStorageBucket");
            if (string.IsNullOrWhiteSpace(bucket) || bucket.Any(c => !(char.IsLetterOrDigit(c) || c == '-')))
                throw new FirebaseException(503, "Configure the private Supabase Storage bucket on the server.");
            return bucket;
        }
        private HttpRequestMessage SupabaseRequest(HttpMethod method, string route, bool write)
        {
            if (write && !IsTrue(StorageSetting("SupabaseStorageLiveWritesEnabled"))) throw new FirebaseException(503, "Supabase file writes are disabled on this server.");
            string secret = supabaseSecretProvider();
            if (string.IsNullOrWhiteSpace(secret) || !(secret.StartsWith("sb_secret_", StringComparison.Ordinal) || secret.StartsWith("eyJ", StringComparison.Ordinal)))
                throw new FirebaseException(503, "Set UIMS_SUPABASE_SECRET_KEY in the server environment to a Supabase secret or legacy service-role key, then restart IIS Express.");
            if (secret.StartsWith("eyJ", StringComparison.Ordinal))
            {
                try
                {
                    string payload = secret.Split('.')[1].Replace('-', '+').Replace('_', '/');
                    payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
                    if ((string)JObject.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload)))["role"] != "service_role") throw new Exception();
                }
                catch { throw new FirebaseException(503, "Supabase Storage requires a server secret or service-role key; public keys are not accepted."); }
            }
            var request = new HttpRequestMessage(method, SupabaseUri(route));
            request.Headers.Add("apikey", secret);
            // Legacy service-role keys are JWTs; new secret keys authenticate via apikey.
            if (secret.StartsWith("eyJ", StringComparison.Ordinal)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            return request;
        }
        private string SupabaseObject(string path, bool authenticated = false)
        {
            ValidateObjectPath(path);
            if (path.Contains("\\") || path.Contains("%") || path.Split('/').Any(s => string.IsNullOrWhiteSpace(s))) throw new FirebaseException(400, "Invalid private Storage object path.");
            return "object/" + (authenticated ? "authenticated/" : "") + SupabaseBucket() + "/" + string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
        }
        private static async Task SupabaseResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            // Never reflect provider bodies, headers, tokens, or request URLs.
            int code = (int)response.StatusCode;
            await Task.CompletedTask;
            throw new FirebaseException(code >= 500 ? 502 : code, "Supabase private file operation failed (HTTP " + code + "). Check the configured bucket and server permissions.");
        }
        private async Task<JObject> SupabaseAvailability(CancellationToken cancellation)
        {
            using (var request = SupabaseRequest(HttpMethod.Get, "bucket/" + SupabaseBucket(), true))
            using (var response = await supabaseClient.SendAsync(request, cancellation).ConfigureAwait(false))
            {
                await SupabaseResponse(response).ConfigureAwait(false);
                JObject bucket;
                try { bucket = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)); }
                catch { throw new FirebaseException(502, "Invalid Supabase bucket verification response."); }
                if ((string)bucket["id"] != SupabaseBucket() || (bool?)bucket["public"] != false) throw new FirebaseException(409, "Supabase Storage requires the configured bucket to be private.");
                return new JObject { ["available"] = true, ["provider"] = "supabase", ["verification"] = "readOnlyPrivateBucket", ["writesVerified"] = false };
            }
        }
        private async Task<StoredFile> SupabaseUpload(string path, string name, string type, byte[] bytes, CancellationToken cancellation)
        {
            name = SanitizeFileName(name); type = ValidateFile(name, type, bytes);
            await SupabaseAvailability(cancellation).ConfigureAwait(false);
            using (var request = SupabaseRequest(HttpMethod.Post, SupabaseObject(path), true))
            {
                request.Headers.Add("x-upsert", "false");
                request.Content = new ByteArrayContent(bytes); request.Content.Headers.ContentType = new MediaTypeHeaderValue(type);
                using (var response = await supabaseClient.SendAsync(request, cancellation).ConfigureAwait(false))
                {
                    await SupabaseResponse(response).ConfigureAwait(false);
                    return new StoredFile { Provider = "supabase", ObjectPath = path, FileName = name, ContentType = type, SizeBytes = bytes.LongLength, Sha256 = Sha256(bytes) };
                }
            }
            // Unknown upload outcomes are retained, never delete an object after an ambiguous POST.
        }
        public async Task<byte[]> DownloadAsync(string path, string provider, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(provider) || provider == "firebase") return await DownloadAsync(path, cancellation).ConfigureAwait(false);
            if (provider != "supabase") throw new FirebaseException(409, "Unsupported file storage provider.");
            using (var request = SupabaseRequest(HttpMethod.Get, SupabaseObject(path, true), false))
            using (var response = await supabaseClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
            {
                await SupabaseResponse(response).ConfigureAwait(false);
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[81920]; int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                    { if (output.Length + count > MaximumFileBytes) throw new FirebaseException(413, "Stored file exceeds the download size limit."); output.Write(buffer, 0, count); }
                    return output.ToArray();
                }
            }
        }
        public async Task DeleteObjectAsync(string path, string provider, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(provider) || provider == "firebase") { await DeleteFirebaseObjectAsync(path, cancellation).ConfigureAwait(false); return; }
            if (provider != "supabase") throw new FirebaseException(409, "Unsupported file storage provider.");
            using (var request = SupabaseRequest(HttpMethod.Delete, SupabaseObject(path), true))
            using (var response = await supabaseClient.SendAsync(request, cancellation).ConfigureAwait(false))
            { if (response.StatusCode != HttpStatusCode.NotFound) await SupabaseResponse(response).ConfigureAwait(false); }
        }
    }
}
