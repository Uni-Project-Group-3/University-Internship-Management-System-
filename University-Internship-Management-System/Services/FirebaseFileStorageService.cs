using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;

namespace UIMS.Services
{
    /// <summary>Private Firebase Storage transport. Authorization to task records is enforced by TaskService.</summary>
    public sealed class FirebaseFileStorageService
    {
        public const int MaximumFileBytes = 10 * 1024 * 1024;
        private const string StorageScope = "https://www.googleapis.com/auth/devstorage.read_write";
        private static readonly Lazy<Task<GoogleCredential>> Credential = new Lazy<Task<GoogleCredential>>(() => GoogleCredential.GetApplicationDefaultAsync());
        private readonly Func<string, string> setting;
        private readonly HttpClient client;
        private readonly Func<CancellationToken, Task<string>> tokenProvider;

        public FirebaseFileStorageService(Func<string, string> setting, HttpClient client = null,
            Func<CancellationToken, Task<string>> tokenProvider = null)
        {
            this.setting = setting ?? throw new ArgumentNullException(nameof(setting));
            this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            this.tokenProvider = tokenProvider ?? GetStorageTokenAsync;
        }

        public async Task<StoredFile> UploadAsync(string objectPath, string fileName, string contentType, byte[] bytes, CancellationToken cancellation)
        {
            EnsureEnabled(true);
            ValidateObjectPath(objectPath);
            string safeName = SanitizeFileName(fileName);
            string expectedType = ValidateFile(safeName, contentType, bytes);
            string hash = Sha256(bytes);
            var uri = new Uri("https://storage.googleapis.com/upload/storage/v1/b/" + Uri.EscapeDataString(Bucket()) +
                "/o?uploadType=media&name=" + Uri.EscapeDataString(objectPath) + "&ifGenerationMatch=0");
            try
            {
                using (var request = await AuthorizedRequestAsync(HttpMethod.Post, uri, cancellation).ConfigureAwait(false))
                {
                    request.Content = new ByteArrayContent(bytes);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(expectedType);
                    using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.PreconditionFailed || response.StatusCode == HttpStatusCode.Conflict)
                            throw new FirebaseException(409, "This unique Storage object already exists; no file was overwritten.");
                        await EnsureResponseAsync(response, "upload").ConfigureAwait(false);
                        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        JObject result;
                        try { result = JObject.Parse(json); }
                        catch { throw new FirebaseException(502, "Firebase Storage returned an invalid upload response."); }
                        if ((string)result["name"] != objectPath || (long?)result["size"] != bytes.LongLength)
                            throw new FirebaseException(502, "Firebase Storage did not confirm the uploaded object and size.");
                        return new StoredFile { ObjectPath = objectPath, FileName = safeName, ContentType = expectedType,
                            SizeBytes = bytes.LongLength, Sha256 = hash, Generation = (string)result["generation"] };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The server may have accepted the upload before the client disconnected. Compensate by deleting
                // this server-generated, non-guessable object name; if that fails it remains private and unlinked.
                try { await DeleteObjectAsync(objectPath, CancellationToken.None).ConfigureAwait(false); } catch { }
                throw;
            }
            catch (HttpRequestException)
            {
                try { await DeleteObjectAsync(objectPath, CancellationToken.None).ConfigureAwait(false); } catch { }
                throw new FirebaseException(502, "Firebase Storage upload failed. Any partial object cleanup was attempted.");
            }
            catch (FirebaseException error)
            {
                // 409/412 means create-only detected a pre-existing object. Never delete it.
                if (error.StatusCode != 409 && error.StatusCode != 412)
                    try { await DeleteObjectAsync(objectPath, CancellationToken.None).ConfigureAwait(false); } catch { }
                throw;
            }
        }

        public async Task<byte[]> DownloadAsync(string objectPath, CancellationToken cancellation)
        {
            EnsureEnabled(false);
            ValidateObjectPath(objectPath);
            var uri = ObjectUri(objectPath, "?alt=media");
            using (var request = await AuthorizedRequestAsync(HttpMethod.Get, uri, cancellation).ConfigureAwait(false))
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
            {
                await EnsureResponseAsync(response, "download").ConfigureAwait(false);
                if (response.Content.Headers.ContentLength > MaximumFileBytes) throw new FirebaseException(413, "The stored file exceeds the download size limit.");
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[81920]; int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                    {
                        if (output.Length + read > MaximumFileBytes) throw new FirebaseException(413, "The stored file exceeds the download size limit.");
                        output.Write(buffer, 0, read);
                    }
                    return output.ToArray();
                }
            }
        }

        public async Task DeleteObjectAsync(string objectPath, CancellationToken cancellation)
        {
            EnsureEnabled(true);
            ValidateObjectPath(objectPath);
            using (var request = await AuthorizedRequestAsync(HttpMethod.Delete, ObjectUri(objectPath, ""), cancellation).ConfigureAwait(false))
            using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) return;
                await EnsureResponseAsync(response, "cleanup").ConfigureAwait(false);
            }
        }

        public static string CreateFileId()
        {
            var bytes = new byte[24];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public static string SanitizeFileName(string value)
        {
            string name = Path.GetFileName((value ?? "").Replace('\\', '/'));
            name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (name.Length == 0 || name.Length > 160 || name == "." || name == "..") throw new FirebaseException(400, "Choose a valid file name (up to 160 characters).");
            return name;
        }

        public static string ValidateFile(string fileName, string contentType, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) throw new FirebaseException(400, "Choose a non-empty file.");
            if (bytes.Length > MaximumFileBytes) throw new FirebaseException(413, "Files are limited to 10 MiB.");
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            string expected;
            switch (ext)
            {
                case ".pdf": expected = "application/pdf"; break;
                case ".doc": expected = "application/msword"; break;
                case ".docx": expected = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; break;
                case ".xlsx": expected = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; break;
                case ".pptx": expected = "application/vnd.openxmlformats-officedocument.presentationml.presentation"; break;
                case ".txt": expected = "text/plain"; break;
                case ".png": expected = "image/png"; break;
                case ".jpg": case ".jpeg": expected = "image/jpeg"; break;
                default: throw new FirebaseException(415, "Allowed file types are PDF, DOCX, XLSX, PPTX, TXT, PNG and JPEG.");
            }
            if (!string.Equals((contentType ?? "").Split(';')[0].Trim(), expected, StringComparison.OrdinalIgnoreCase))
                throw new FirebaseException(415, "The file content type does not match its extension.");
            bool signature = ext == ".pdf" ? Starts(bytes, "%PDF-") : ext == ".doc" ? Starts(bytes, new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 }) : ext == ".png" ? Starts(bytes, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) :
                ext == ".jpg" || ext == ".jpeg" ? bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff :
                ext == ".txt" ? IsUtf8Text(bytes) : IsOfficePackage(bytes, ext);
            if (!signature) throw new FirebaseException(415, "The file signature does not match the allowed file type.");
            return expected;
        }

        private void EnsureEnabled(bool write)
        {
            if (!IsTrue(setting("FirebaseEnabled")) || !IsTrue(setting("FirebaseStorageEnabled")))
                throw new FirebaseException(503, "Firebase Storage is not configured; no Storage request was sent.");
            if (write && !IsTrue(setting("FirebaseStorageLiveWritesEnabled")))
                throw new FirebaseException(503, "Firebase Storage writes are disabled in this environment.");
            Bucket();
        }
        private string Bucket()
        {
            string bucket = setting("FirebaseStorageBucket");
            if (string.IsNullOrWhiteSpace(bucket) || bucket.Length > 222 || bucket.Contains("/") || bucket.Contains("?") || bucket.Contains("#"))
                throw new FirebaseException(503, "Configure the Firebase Storage bucket centrally in Web.config.");
            return bucket.Trim();
        }
        private async Task<HttpRequestMessage> AuthorizedRequestAsync(HttpMethod method, Uri uri, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            string token;
            try { token = await tokenProvider(cancellation).ConfigureAwait(false); }
            catch (Exception error) when (!(error is FirebaseException) && !(error is OperationCanceledException))
            { throw new FirebaseException(503, "Firebase Storage is not connected to trusted Google credentials on this server. For local development, run 'gcloud auth application-default login' using an account with Storage Object Admin access to the configured bucket, or set GOOGLE_APPLICATION_CREDENTIALS to a service-account JSON file stored outside the project, then restart IIS Express."); }
            if (string.IsNullOrWhiteSpace(token)) throw new FirebaseException(503, "Trusted server credentials did not provide a Storage access token.");
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }
        private static async Task<string> GetStorageTokenAsync(CancellationToken cancellation)
        {
            var credential = (await Credential.Value.ConfigureAwait(false)).CreateScoped(StorageScope);
            cancellation.ThrowIfCancellationRequested();
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync().ConfigureAwait(false);
        }
        private Uri ObjectUri(string objectPath, string query)
        { return new Uri("https://storage.googleapis.com/storage/v1/b/" + Uri.EscapeDataString(Bucket()) + "/o/" + Uri.EscapeDataString(objectPath) + query); }
        private static void ValidateObjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length > 500 || path.StartsWith("/", StringComparison.Ordinal) || path.Contains("..") || path.Any(char.IsControl))
                throw new FirebaseException(400, "Invalid private Storage object path.");
        }
        private static bool IsTrue(string value) { return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase); }
        private static bool Starts(byte[] data, string value) { return Starts(data, Encoding.ASCII.GetBytes(value)); }
        private static bool Starts(byte[] data, byte[] signature) { return data.Length >= signature.Length && signature.SequenceEqual(data.Take(signature.Length)); }
        private static bool IsUtf8Text(byte[] bytes)
        {
            if (bytes.Any(b => b == 0)) return false;
            try { new UTF8Encoding(false, true).GetString(bytes); return true; } catch (DecoderFallbackException) { return false; }
        }
        private static bool IsOfficePackage(byte[] bytes, string extension)
        {
            if (!Starts(bytes, new byte[] { 0x50, 0x4b, 0x03, 0x04 })) return false;
            string requiredEntry = extension == ".docx" ? "word/document.xml" : extension == ".xlsx" ? "xl/workbook.xml" : "ppt/presentation.xml";
            try
            {
                using (var stream = new MemoryStream(bytes, false))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                    return archive.Entries.Any(entry => string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase)) &&
                        archive.Entries.Any(entry => string.Equals(entry.FullName, requiredEntry, StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidDataException) { return false; }
            catch (ArgumentException) { return false; }
        }
        private static string Sha256(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private static async Task EnsureResponseAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode) return;
            int status = (int)response.StatusCode;
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new FirebaseException(403, "Firebase Storage denied the " + operation + ". Check trusted server IAM and Storage Rules; no public access was attempted.");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                if (string.Equals(operation, "upload", StringComparison.OrdinalIgnoreCase))
                    throw new FirebaseException(404, "The configured Firebase Storage bucket was not found. Create the bucket in Firebase Console or update FirebaseStorageBucket in Web.config to the exact bucket name shown under Storage > Files.");
                if (string.Equals(operation, "download", StringComparison.OrdinalIgnoreCase))
                    throw new FirebaseException(404, "The requested file does not exist in Firebase Storage.");
                throw new FirebaseException(404, "The Firebase Storage bucket or requested object was not found during " + operation + ".");
            }
            throw new FirebaseException(status >= 500 ? 502 : status, "Firebase Storage " + operation + " failed (HTTP " + status.ToString(CultureInfo.InvariantCulture) + ").");
        }
    }

    public sealed class StoredFile
    {
        public string ObjectPath { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; }
        public string Generation { get; set; }
    }
}
