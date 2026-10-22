using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Tasks;

internal static partial class Program
{
    private static async Task RunTaskExamTests()
    {
        await Test("exam lifecycle keeps answer keys private, grades server-side and resumes the same attempt", ExamLifecycleOffline);
        await Test("exam question images use private Firebase Storage and authorized file metadata links", ExamQuestionImageOffline);
        await Test("exam authorization, attempt limits, availability and draft concurrency are enforced", ExamAuthorizationOffline);
        await Test("disabled exam configuration performs no Firebase or Storage requests", ExamDisabledOffline);
    }

    private static Dictionary<string, string> ExamSettings(bool enabled)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["FirebaseEnabled"] = enabled ? "true" : "false", ["FirebaseProjectId"] = "offline-test", ["FirebaseDatabaseUrl"] = "https://offline-test-default-rtdb.europe-west1.firebasedatabase.app",
            ["FirebaseApiKey"] = "", ["FirebaseAuthenticationEnabled"] = "true", ["FirebaseLiveWritesEnabled"] = "true", ["FirebaseSchemaValidated"] = "true", ["FirebaseTaskSchemaValidated"] = "true",
            ["TaskExamsEnabled"] = "true", ["TaskExamsLiveWritesEnabled"] = "true", ["TaskExamsSchemaValidated"] = "true", ["FirebaseStorageEnabled"] = "false", ["FirebaseStorageLiveWritesEnabled"] = "false", ["FirebaseStorageBucket"] = ""
        };
    }
    private static TaskActor ExamCompanyActor(string company = "CO-1", string supervisor = "CS-1")
    { return TaskActor.FromVerifiedProfile("uid-" + supervisor, "companySupervisor", new JObject { ["isActive"] = true, ["role"] = "companySupervisor", ["companySupervisorId"] = supervisor, ["companyId"] = company }); }
    private static TaskActor ExamStudentActor(string student = "ST-1")
    { return TaskActor.FromVerifiedProfile("uid-" + student, "student", new JObject { ["isActive"] = true, ["role"] = "student", ["studentId"] = student }); }
    private static TaskExamService ExamService(ExamDatabase database, TaskActor actor, bool enabled = true, UIMS.Services.FirebaseFileStorageService storage = null)
    {
        var config = ExamSettings(enabled); return new TaskExamService(k => config.ContainsKey(k) ? config[k] : "", new HttpClient(database), actor, token => Task.FromResult("isolated-test-token"), storage);
    }
    private static UIMS.Services.FirebaseFileStorageService ExamStorage(FakeExamStorage storage)
    { var settings = ExamSettings(true); settings["FirebaseStorageEnabled"] = "true"; settings["FirebaseStorageLiveWritesEnabled"] = "true"; settings["FirebaseStorageBucket"] = "offline-bucket"; return new UIMS.Services.FirebaseFileStorageService(k => settings.ContainsKey(k) ? settings[k] : "", new HttpClient(storage), token => Task.FromResult("isolated-storage-token")); }
    private static void SeedExamRelationships(ExamDatabase db)
    {
        db.Set("placements/PL-1", new JObject { ["id"] = "PL-1", ["status"] = "confirmed", ["studentId"] = "ST-1", ["companyId"] = "CO-1", ["companySupervisorId"] = "CS-1" });
        db.Set("placements/PL-2", new JObject { ["id"] = "PL-2", ["status"] = "confirmed", ["studentId"] = "ST-2", ["companyId"] = "CO-2", ["companySupervisorId"] = "CS-2" });
        db.Set("tasks/T-1", new JObject { ["id"] = "T-1", ["placementId"] = "PL-1", ["studentId"] = "ST-1", ["assignedBy"] = "CS-1", ["companyId"] = "CO-1" });
        db.Set("tasks/T-2", new JObject { ["id"] = "T-2", ["placementId"] = "PL-2", ["studentId"] = "ST-2", ["assignedBy"] = "CS-2", ["companyId"] = "CO-2" });
    }
    private static JObject ExamDraft(string opens = null, string closes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new JObject { ["placementId"] = "PL-1", ["taskId"] = "T-1", ["title"] = "Offline Test Exam", ["instructions"] = "Select the correct answer.",
            ["opensAt"] = opens ?? now.AddMinutes(-1).ToString("o"), ["closesAt"] = closes ?? now.AddHours(1).ToString("o"), ["durationMinutes"] = 20, ["attemptLimit"] = 1,
            ["questions"] = new JArray(new JObject { ["id"] = "Q1", ["type"] = "multipleChoice", ["prompt"] = "Two plus two?", ["points"] = 2,
                ["options"] = new JArray(new JObject { ["id"] = "A", ["text"] = "3" }, new JObject { ["id"] = "B", ["text"] = "4" }), ["correctOptionId"] = "B" },
                new JObject { ["id"] = "Q2", ["type"] = "trueFalse", ["prompt"] = "The sky is blue.", ["points"] = 1, ["correctAnswer"] = true }) };
    }
    private static async Task ExamLifecycleOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var company = ExamService(db, ExamCompanyActor());
        var created = await company.CreateDraftAsync(ExamDraft(), CancellationToken.None); string examId = (string)created["id"];
        Assert((string)db.Get("taskExams/" + examId + "/status") == "draft");
        var published = await company.PublishAsync(examId, CancellationToken.None); Assert((int)published["version"] == 1);
        var publicNode = db.Get("taskExams/" + examId + "/versions/1"); var keys = db.Get("taskExamAnswerKeys/" + examId + "/1");
        Assert(publicNode["questions"]["Q1"]["correctOptionId"] == null && publicNode["questions"]["Q2"]["correctAnswer"] == null);
        Assert((string)keys["Q1"]["correctOptionId"] == "B" && (bool)keys["Q2"]["correctAnswer"]);
        var student = ExamService(db, ExamStudentActor()); var list = await student.ListAsync(CancellationToken.None);
        Assert(list[examId] != null && list.ToString().IndexOf("correctOptionId", StringComparison.Ordinal) < 0 && list.ToString().IndexOf("correctAnswer", StringComparison.Ordinal) < 0);
        var exam = await student.GetStudentExamAsync(examId, CancellationToken.None); Assert(exam["versionData"]["questions"]["Q1"]["options"].HasValues);
        var attempt = await student.StartAttemptAsync(examId, CancellationToken.None); var duplicate = await student.StartAttemptAsync(examId, CancellationToken.None);
        Assert((string)attempt["id"] == (string)duplicate["id"]);
        var result = await student.SaveAnswersAsync(examId, (string)attempt["id"], new JObject { ["Q1"] = "B", ["Q2"] = true }, true, CancellationToken.None);
        Assert((decimal)result["score"] == 3m && (decimal)result["maximumScore"] == 3m && result["gradedAt"] != null);
        var idempotent = await student.SaveAnswersAsync(examId, (string)attempt["id"], new JObject { ["Q1"] = "A" }, true, CancellationToken.None);
        Assert((decimal)idempotent["score"] == 3m && (string)idempotent["status"] == "submitted");
        var resumed = await student.GetMyAttemptAsync(examId, (string)attempt["id"], CancellationToken.None); Assert((string)resumed["answers"]["Q1"] == "B" && resumed["score"] != null);
        await Reject<UIMS.Services.FirebaseException>(() => student.StartAttemptAsync(examId, CancellationToken.None));
        Assert(db.RequestCount > 0 && db.Writes > 0);
    }
    private static async Task ExamQuestionImageOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var storage = new FakeExamStorage(); var storageService = ExamStorage(storage);
        var company = ExamService(db, ExamCompanyActor(), true, storageService); var created = await company.CreateDraftAsync(ExamDraft(), CancellationToken.None); string id = (string)created["id"];
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3 };
        var meta = await company.UploadQuestionImageAsync(id, "Q1", "question.png", "image/png", png, CancellationToken.None);
        Assert((string)meta["purpose"] == "examQuestionImage" && (string)db.Get("fileAttachments/" + (string)meta["id"] + "/status") == "ready");
        Assert(storage.UploadCount == 1 && (string)meta["storagePath"] == "uims/exam-question-images/PL-1/" + id + "/" + (string)meta["id"]);
        var draft = ExamDraft(); ((JObject)draft["questions"][0])["imageFileId"] = (string)meta["id"];
        await company.SaveDraftAsync(id, draft, "1", CancellationToken.None); await company.PublishAsync(id, CancellationToken.None);
        var student = ExamService(db, ExamStudentActor(), true, storageService);
        var download = await student.DownloadQuestionImageAsync((string)meta["id"], CancellationToken.None);
        Assert(download.Content.SequenceEqual(png) && download.ContentType == "image/png");
        await Reject<UIMS.Services.FirebaseException>(() => ExamService(db, ExamStudentActor("ST-2"), true, storageService).DownloadQuestionImageAsync((string)meta["id"], CancellationToken.None));
    }
    private static async Task ExamAuthorizationOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var company = ExamService(db, ExamCompanyActor());
        var foreignDraft = ExamDraft(); foreignDraft["placementId"] = "PL-2"; foreignDraft["taskId"] = "T-2";
        await Reject<UIMS.Services.FirebaseException>(() => company.CreateDraftAsync(foreignDraft, CancellationToken.None));
        var created = await company.CreateDraftAsync(ExamDraft(), CancellationToken.None); string id = (string)created["id"];
        await Reject<UIMS.Services.FirebaseException>(() => company.SaveDraftAsync(id, ExamDraft(), "0", CancellationToken.None));
        await company.PublishAsync(id, CancellationToken.None);
        await Reject<UIMS.Services.FirebaseException>(() => company.SaveDraftAsync(id, ExamDraft(), "1", CancellationToken.None));
        var otherStudent = ExamService(db, ExamStudentActor("ST-2")); Assert((await otherStudent.ListAsync(CancellationToken.None))[id] == null);
        var student = ExamService(db, ExamStudentActor()); var attempt = await student.StartAttemptAsync(id, CancellationToken.None);
        var retry = await student.StartAttemptAsync(id, CancellationToken.None); Assert((string)attempt["id"] == (string)retry["id"]);
        await Reject<UIMS.Services.FirebaseException>(() => student.SaveAnswersAsync(id, (string)attempt["id"], new JObject { ["fakeQuestion"] = "B" }, false, CancellationToken.None));
    }
    private static async Task ExamDisabledOffline()
    {
        var db = new ExamDatabase(); var svc = ExamService(db, ExamStudentActor(), false);
        await Reject<UIMS.Services.FirebaseException>(() => svc.ListAsync(CancellationToken.None)); Assert(db.RequestCount == 0);
    }
    private sealed class ExamDatabase : HttpMessageHandler
    {
        private readonly JObject root = new JObject(); private readonly Dictionary<string, int> versions = new Dictionary<string, int>();
        public int RequestCount { get; private set; } public int Writes { get; private set; }
        public void Set(string path, JObject value) { SetAt(path, value); versions[path] = versions.ContainsKey(path) ? versions[path] + 1 : 1; }
        public JToken Get(string path) { var t = At(path); return t == null ? null : t.DeepClone(); }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++; if (request.Headers.Authorization == null || request.Headers.Authorization.Parameter != "isolated-test-token") throw new InvalidOperationException("Test request omitted the isolated bearer token.");
            string path = Uri.UnescapeDataString(request.RequestUri.AbsolutePath.TrimStart('/')); if (path.EndsWith(".json", StringComparison.Ordinal)) path = path.Substring(0, path.Length - 5);
            path = path.Replace("%2F", "/"); JToken current = At(path); var match = request.Headers.Contains("if-match") ? request.Headers.GetValues("if-match").FirstOrDefault() : null;
            string currentTag = versions.ContainsKey(path) ? "\"v" + versions[path] + "\"" : "\"null_etag\"";
            if (match != null && match != currentTag && !(match == "null_etag" && current == null)) return Task.FromResult(Response(HttpStatusCode.PreconditionFailed, "{\"error\":\"etag\"}", currentTag));
            if (request.Method == HttpMethod.Get) return Task.FromResult(Response(HttpStatusCode.OK, current == null ? "null" : current.ToString(Newtonsoft.Json.Formatting.None), currentTag));
            Writes++;
            string raw = request.Content.ReadAsStringAsync().GetAwaiter().GetResult(); JToken incoming = JToken.Parse(raw);
            if (request.Method.Method == "PATCH" && current is JObject && incoming is JObject) { var merged = (JObject)current.DeepClone(); merged.Merge((JObject)incoming); incoming = merged; }
            SetAt(path, incoming); versions[path] = versions.ContainsKey(path) ? versions[path] + 1 : 1;
            return Task.FromResult(Response(HttpStatusCode.OK, incoming.ToString(Newtonsoft.Json.Formatting.None), "\"v" + versions[path] + "\""));
        }
        private HttpResponseMessage Response(HttpStatusCode status, string text, string tag)
        { var r = new HttpResponseMessage(status) { Content = new StringContent(text) }; r.Headers.TryAddWithoutValidation("ETag", tag); return r; }
        private JToken At(string path)
        { JToken t = root; foreach (var part in path.Split('/')) { var o = t as JObject; if (o == null || o[part] == null) return null; t = o[part]; } return t; }
        private void SetAt(string path, JToken value)
        { var parts = path.Split('/'); JObject o = root; for (int i = 0; i < parts.Length - 1; i++) { if (!(o[parts[i]] is JObject)) o[parts[i]] = new JObject(); o = (JObject)o[parts[i]]; } o[parts[parts.Length - 1]] = value == null ? JValue.CreateNull() : value.DeepClone(); }
    }
    private sealed class FakeExamStorage : HttpMessageHandler
    {
        public int UploadCount { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null || request.Headers.Authorization.Parameter != "isolated-storage-token") throw new InvalidOperationException("Storage mock did not receive the isolated token.");
            if (request.Method == HttpMethod.Post)
            {
                UploadCount++; string name = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query)["name"];
                var bytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new JObject { ["name"] = name, ["size"] = bytes.Length, ["generation"] = "offline-generation-1" }.ToString()) };
            }
            if (request.Method == HttpMethod.Get) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3 }) };
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
