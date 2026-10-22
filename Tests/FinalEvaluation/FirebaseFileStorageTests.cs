using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using UIMS.Tasks;

internal static partial class Program
{
    private sealed class IsolatedStorageHandler : HttpMessageHandler
    {
        public readonly Dictionary<string, byte[]> Objects = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public int Uploads, Downloads, Deletes, Requests;
        public bool FailUpload, InterruptUpload, DuplicateUpload;
        public bool Authenticated = true;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            Requests++;
            Authenticated &= request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "offline-storage-token";
            if (request.Method == HttpMethod.Post)
            {
                Uploads++;
                if (InterruptUpload) throw new TaskCanceledException("simulated interrupted upload");
                if (FailUpload) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                string objectPath = QueryValue(request.RequestUri.Query, "name");
                if (DuplicateUpload) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));
                var bytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                Objects[objectPath] = bytes;
                string json = new JObject { ["name"] = objectPath, ["size"] = bytes.Length, ["generation"] = "1" }.ToString();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            }
            string path = request.RequestUri.AbsolutePath;
            int marker = path.IndexOf("/o/", StringComparison.Ordinal);
            string key = marker < 0 ? "" : Uri.UnescapeDataString(path.Substring(marker + 3));
            if (request.Method == HttpMethod.Get && request.RequestUri.Query.Contains("alt=media"))
            {
                Downloads++;
                byte[] value;
                if (!Objects.TryGetValue(key, out value)) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(value) });
            }
            if (request.Method == HttpMethod.Delete)
            {
                Deletes++; Objects.Remove(key);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }
        private static string QueryValue(string query, string name)
        {
            string token = name + "="; int start = query.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            start += token.Length; int end = query.IndexOf('&', start);
            return Uri.UnescapeDataString(query.Substring(start, end < 0 ? query.Length - start : end - start));
        }
    }

    private static Dictionary<string, string> StorageSettings(bool enabled = true)
    {
        return new Dictionary<string, string> {
            { "FirebaseEnabled", enabled ? "true" : "false" }, { "FirebaseStorageEnabled", enabled ? "true" : "false" },
            { "FirebaseStorageLiveWritesEnabled", enabled ? "true" : "false" }, { "FirebaseStorageBucket", "offline-bucket.test" }
        };
    }
    private static UIMS.Services.FirebaseFileStorageService OfflineStorage(IsolatedStorageHandler handler, Dictionary<string, string> settings)
    {
        return new UIMS.Services.FirebaseFileStorageService(key => settings.ContainsKey(key) ? settings[key] : null,
            new HttpClient(handler), cancellation => Task.FromResult("offline-storage-token"));
    }
    private static byte[] FictionalPdf() { return Encoding.ASCII.GetBytes("%PDF-1.7\nfictional test attachment"); }

    private static async Task FirebaseStorageTransportTests()
    {
        var settings = StorageSettings(); var handler = new IsolatedStorageHandler();
        var storage = OfflineStorage(handler, settings);
        string fileId1 = FirebaseFileStorageService.CreateFileId(), fileId2 = FirebaseFileStorageService.CreateFileId();
        Assert(fileId1.Length == 48 && fileId1.All(c => "0123456789abcdef".IndexOf(c) >= 0) && fileId1 != fileId2);
        string path = "uims/task-attachments/place-1/task-1/fictional-opaque-id";
        var stored = await storage.UploadAsync(path, "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None);
        Assert(stored.ObjectPath == path && stored.FileName == "guide.pdf" && stored.SizeBytes == FictionalPdf().Length && stored.Sha256.Length == 64);
        Assert(handler.Authenticated && handler.Uploads == 1 && handler.Objects.ContainsKey(path));
        var downloaded = await storage.DownloadAsync(path, CancellationToken.None);
        Assert(downloaded.SequenceEqual(FictionalPdf()) && handler.Downloads == 1);
        await storage.DeleteObjectAsync(path, CancellationToken.None);
        Assert(handler.Deletes == 1 && handler.Objects.Count == 0);
        await storage.DeleteObjectAsync(path, CancellationToken.None);
    }

    private static async Task FirebaseStorageValidationTests()
    {
        var handler = new IsolatedStorageHandler(); var storage = OfflineStorage(handler, StorageSettings());
        await Reject<FirebaseException>(() => storage.UploadAsync("uims/a/b/id", "bad.exe", "application/octet-stream", new byte[] { 1 }, CancellationToken.None));
        await Reject<FirebaseException>(() => storage.UploadAsync("uims/a/b/id", "bad.pdf", "text/plain", FictionalPdf(), CancellationToken.None));
        await Reject<FirebaseException>(() => storage.UploadAsync("uims/a/b/id", "bad.pdf", "application/pdf", new byte[] { 0, 1, 2 }, CancellationToken.None));
        await Reject<FirebaseException>(() => storage.UploadAsync("uims/a/b/id", "fake.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new byte[] { 0x50, 0x4b, 0x03, 0x04, 1, 2 }, CancellationToken.None));
        await Reject<FirebaseException>(() => storage.UploadAsync("uims/a/b/id", "big.pdf", "application/pdf", new byte[FirebaseFileStorageService.MaximumFileBytes + 1], CancellationToken.None));
        Assert(handler.Requests == 0);
    }

    private static async Task FirebaseStorageDuplicateAndInterruptedTests()
    {
        var duplicate = new IsolatedStorageHandler { DuplicateUpload = true };
        duplicate.Objects["uims/a/b/same"] = Encoding.ASCII.GetBytes("older object remains untouched");
        await Reject<FirebaseException>(() => OfflineStorage(duplicate, StorageSettings()).UploadAsync("uims/a/b/same", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(duplicate.Uploads == 1 && duplicate.Deletes == 0 && Encoding.ASCII.GetString(duplicate.Objects["uims/a/b/same"]) == "older object remains untouched");
        var interrupted = new IsolatedStorageHandler { InterruptUpload = true };
        await Reject<TaskCanceledException>(() => OfflineStorage(interrupted, StorageSettings()).UploadAsync("uims/a/b/partial", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(interrupted.Uploads == 1 && interrupted.Deletes == 1);
    }

    private static async Task FirebaseStorageDisabledTests()
    {
        var handler = new IsolatedStorageHandler(); var disabled = OfflineStorage(handler, StorageSettings(false));
        await Reject<FirebaseException>(() => disabled.UploadAsync("uims/a/b/id", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        await Reject<FirebaseException>(() => disabled.DownloadAsync("uims/a/b/id", CancellationToken.None));
        Assert(handler.Requests == 0);
        var storageOff = StorageSettings(); storageOff["FirebaseStorageEnabled"] = "false";
        await Reject<FirebaseException>(() => OfflineStorage(handler, storageOff).UploadAsync("uims/a/b/id", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(handler.Requests == 0);
        var writesOff = StorageSettings(); writesOff["FirebaseStorageLiveWritesEnabled"] = "false";
        await Reject<FirebaseException>(() => OfflineStorage(handler, writesOff).UploadAsync("uims/a/b/id", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(handler.Requests == 0);
        var missingBucket = StorageSettings(); missingBucket["FirebaseStorageBucket"] = "";
        await Reject<FirebaseException>(() => OfflineStorage(handler, missingBucket).UploadAsync("uims/a/b/id", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(handler.Requests == 0);
    }

    private static async Task TaskFileAuthorizationAndMetadataTests()
    {
        var settings = EnabledTaskSettings();
        settings["FirebaseStorageEnabled"] = "true"; settings["FirebaseStorageLiveWritesEnabled"] = "true"; settings["FirebaseStorageBucket"] = "offline-bucket.test";
        var db = new TaskDatabaseHandler();
        db.Records["placements/place-1"] = JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\",\"status\":\"confirmed\"}");
        db.Records["tasks/task-1"] = JObject.Parse("{\"id\":\"task-1\",\"studentId\":\"ST-1\",\"placementId\":\"place-1\",\"assignedBy\":\"SUP-1\",\"title\":\"Fictional task\",\"status\":\"In Progress\",\"submissions\":[]}");
        var storageHandler = new IsolatedStorageHandler(); var fileStorage = OfflineStorage(storageHandler, settings);
        Func<string, string> setting = key => settings.ContainsKey(key) ? settings[key] : null;
        var company = TaskActor.FromVerifiedProfile("company-uid", "companySupervisor", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\"}"));
        var student = TaskActor.FromVerifiedProfile("student-uid", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var companyService = new TaskService(setting, new HttpClient(db), company, ct => Task.FromResult("isolated-test-token"), fileStorage);
        var uploaded = await companyService.UploadTaskAttachmentAsync("task-1", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None);
        string resourceId = (string)uploaded["attachment"]["id"];
        Assert(!string.IsNullOrEmpty(resourceId) && (string)db.Records["tasks/task-1"]["attachmentIds"][0] == resourceId);
        var metadata = (JObject)db.Records["fileAttachments/" + resourceId];
        Assert((string)metadata["status"] == "ready" && (string)metadata["purpose"] == "taskAttachment" && metadata["storagePath"] != null);

        var studentService = new TaskService(setting, new HttpClient(db), student, ct => Task.FromResult("isolated-test-token"), fileStorage);
        var listed = await studentService.ListTaskFilesAsync("task-1", CancellationToken.None);
        Assert(((JArray)listed["files"]).Count == 1 && (string)((JObject)listed["files"][0])["id"] == resourceId);
        var studentDownload = await studentService.DownloadTaskFileAsync(resourceId, CancellationToken.None);
        Assert(studentDownload.Content.SequenceEqual(FictionalPdf()));

        var wrongStudent = TaskActor.FromVerifiedProfile("student-2", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-2\"}"));
        var unauthorized = new TaskService(setting, new HttpClient(db), wrongStudent, ct => Task.FromResult("isolated-test-token"), fileStorage);
        int downloadsBefore = storageHandler.Downloads;
        await Reject<FirebaseException>(() => unauthorized.DownloadTaskFileAsync(resourceId, CancellationToken.None));
        Assert(storageHandler.Downloads == downloadsBefore);

        var submitted = await studentService.SubmitWithAttachmentAsync("task-1", "Fictional work", "https://example.test/work", "work.pdf", "application/pdf", FictionalPdf(), CancellationToken.None);
        string submissionFileId = (string)submitted["attachment"]["id"];
        var task = (JObject)db.Records["tasks/task-1"];
        Assert((string)task["status"] == "Submitted" && ((JArray)task["submissions"]).Count == 1);
        Assert(((JArray)task["submissions"][0]["attachmentIds"]).Values<string>().Single() == submissionFileId);
        Assert((string)db.Records["fileAttachments/" + submissionFileId]["status"] == "ready");
        var companyView = await companyService.LoadAsync("companySupervisor", CancellationToken.None);
        var companySubmission = ((JArray)companyView["submissions"]).Single(row => (string)row["taskId"] == "task-1");
        Assert(((JArray)companySubmission["attachmentIds"]).Values<string>().Contains(submissionFileId));
        Assert((await companyService.DownloadTaskFileAsync(submissionFileId, CancellationToken.None)).Content.SequenceEqual(FictionalPdf()));

        var wrongCompany = TaskActor.FromVerifiedProfile("company-2", "companySupervisor", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-2\"}"));
        var wrongCompanyService = new TaskService(setting, new HttpClient(db), wrongCompany, ct => Task.FromResult("isolated-test-token"), fileStorage);
        int uploadsBefore = storageHandler.Uploads;
        await Reject<FirebaseException>(() => wrongCompanyService.UploadTaskAttachmentAsync("task-1", "other.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(storageHandler.Uploads == uploadsBefore);
    }

    private static async Task FailedStorageUploadDoesNotCreateMetadata()
    {
        var settings = EnabledTaskSettings();
        settings["FirebaseStorageEnabled"] = "true"; settings["FirebaseStorageLiveWritesEnabled"] = "true"; settings["FirebaseStorageBucket"] = "offline-bucket.test";
        var db = new TaskDatabaseHandler();
        db.Records["placements/place-1"] = JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"status\":\"confirmed\"}");
        db.Records["tasks/task-1"] = JObject.Parse("{\"id\":\"task-1\",\"studentId\":\"ST-1\",\"placementId\":\"place-1\",\"assignedBy\":\"SUP-1\",\"status\":\"Assigned\"}");
        var handler = new IsolatedStorageHandler { FailUpload = true };
        var fileStorage = OfflineStorage(handler, settings);
        var company = TaskActor.FromVerifiedProfile("company-uid", "companySupervisor", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\"}"));
        var service = new TaskService(key => settings.ContainsKey(key) ? settings[key] : null, new HttpClient(db), company,
            ct => Task.FromResult("isolated-test-token"), fileStorage);
        await Reject<FirebaseException>(() => service.UploadTaskAttachmentAsync("task-1", "guide.pdf", "application/pdf", FictionalPdf(), CancellationToken.None));
        Assert(!db.Records.Keys.Any(path => path.StartsWith("fileAttachments/", StringComparison.Ordinal)));
        Assert(((JObject)db.Records["tasks/task-1"])["attachmentIds"] == null);
    }

    private static async Task RunFirebaseFileStorageTests()
    {
        await Test("private Firebase Storage transport uploads, downloads and deletes only through isolated HTTP doubles", FirebaseStorageTransportTests);
        await Test("file size, extension, MIME and signature validation rejects unsafe uploads before network", FirebaseStorageValidationTests);
        await Test("duplicate and interrupted object uploads use create-only semantics and attempt safe cleanup", FirebaseStorageDuplicateAndInterruptedTests);
        await Test("disabled Firebase Storage blocks reads and writes before any request", FirebaseStorageDisabledTests);
        await Test("task file metadata links to verified task relationships and denies other students and companies", TaskFileAuthorizationAndMetadataTests);
        await Test("failed Storage upload never creates ready Realtime Database metadata", FailedStorageUploadDoesNotCreateMetadata);
    }
}
