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
    private static async Task RunTaskExamTests(bool includeStorage = true)
    {
        await Test("exam lifecycle keeps answer keys private, grades server-side and resumes the same attempt", ExamLifecycleOffline);
        if (includeStorage) await Test("exam question images use private Firebase Storage and authorized file metadata links", ExamQuestionImageOffline);
        await Test("exam authorization, attempt limits, availability and draft concurrency are enforced", ExamAuthorizationOffline);
        await Test("disabled exam configuration performs no Firebase or Storage requests", ExamDisabledOffline);
        await Test("exam server windows, effective expiry and unreleased receipts ignore late answer changes", ExamWindowAndExpiryOffline);
        await Test("exam definitions reject invalid questions, duplicate options and unowned images before publication", ExamValidationOffline);
        await Test("task composer exam creation retries preserve the same draft and published exam", ExamComposerRetryOffline);
        await Test("student exams support verified UID placement links without exposing another student's examination", ExamStudentUidLinksOffline);
        await Test("exam review controls hide and re-release results and grant audited independent retakes", ExamReviewControlsOffline);
    }
    private static async Task ExamReviewControlsOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var company = ExamService(db, ExamCompanyActor()); var student = ExamService(db, ExamStudentActor());
        string id = (string)(await company.CreateDraftAsync(ExamDraft(), CancellationToken.None))["id"]; await company.PublishAsync(id, CancellationToken.None);
        var versionBefore = db.Get("taskExams/" + id + "/versions/1");
        var first = await student.StartAttemptAsync(id, CancellationToken.None); string attemptId = (string)first["id"];
        Assert((string)db.Get("tasks/T-1/status") == "In Progress");
        await student.SaveAnswersAsync(id, attemptId, new JObject { ["Q1"] = "B", ["Q2"] = true }, true, CancellationToken.None);
        Assert((string)db.Get("tasks/T-1/status") == "Submitted");
        var companyPending = (await company.ListAsync(CancellationToken.None))[id];
        Assert((int)companyPending["pendingReviewCount"] == 1 && (string)companyPending["reviewState"] == "Awaiting supervisor review");
        await company.ReleaseResultAsync(id, "ST-1", attemptId, "Reviewed", CancellationToken.None);
        Assert((decimal)(await student.GetMyAttemptAsync(id, attemptId, CancellationToken.None))["score"] == 3m && (string)db.Get("tasks/T-1/status") == "Completed");
        await company.HideResultAsync(id, "ST-1", attemptId, CancellationToken.None);
        var hidden = await student.GetMyAttemptAsync(id, attemptId, CancellationToken.None); Assert(hidden["score"] == null && hidden["feedback"] == null && hidden["releasedAt"] == null);
        Assert((string)(await company.ListAsync(CancellationToken.None))[id]["reviewState"] == "Completed · Result hidden");
        await Reject<UIMS.Services.FirebaseException>(() => student.HideResultAsync(id, "ST-1", attemptId, CancellationToken.None));
        await company.ReleaseResultAsync(id, "ST-1", attemptId, "Reviewed", CancellationToken.None);
        Assert((decimal)(await student.GetMyAttemptAsync(id, attemptId, CancellationToken.None))["score"] == 3m);
        var historicalAnswers = db.Get("taskExamAttempts/" + id + "/ST-1/" + attemptId + "/answers");
        string due = DateTimeOffset.UtcNow.AddHours(2).ToString("o");
        await company.RequestExamRevisionAsync(id, "ST-1", attemptId, "Explain your work again", due, CancellationToken.None);
        await company.RequestExamRevisionAsync(id, "ST-1", attemptId, "Duplicate", due, CancellationToken.None);
        Assert((string)db.Get("tasks/T-1/status") == "Revision Needed" && db.Get("taskExams/" + id + "/retakeAuthorizations/ST-1").Count() == 1);
        Assert(JToken.DeepEquals(historicalAnswers, db.Get("taskExamAttempts/" + id + "/ST-1/" + attemptId + "/answers")) && JToken.DeepEquals(versionBefore, db.Get("taskExams/" + id + "/versions/1")));
        Assert((bool)(await student.EligibilityAsync(id, CancellationToken.None))["eligible"]);
        var next = await student.StartAttemptAsync(id, CancellationToken.None); Assert((string)next["id"] != attemptId);
        await student.SaveAnswersAsync(id, (string)next["id"], new JObject { ["Q2"] = true }, true, CancellationToken.None);
        Assert((string)db.Get("tasks/T-1/status") == "Submitted");
        Assert(!(bool)(await student.EligibilityAsync(id, CancellationToken.None))["eligible"]);
        Assert((await student.GetMyAttemptAsync(id, attemptId, CancellationToken.None))["score"] == null);
    }

    private static async Task ExamStudentUidLinksOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db);
        var placement = (JObject)db.Get("placements/PL-1"); placement["studentId"] = "uid-ST-1"; db.Set("placements/PL-1", placement);
        var task = (JObject)db.Get("tasks/T-1"); task["studentId"] = "uid-ST-1"; task["title"] = "Student linked task"; db.Set("tasks/T-1", task);
        var company = ExamService(db, ExamCompanyActor()); var created = await company.CreateDraftAsync(ExamDraft(), CancellationToken.None); string id = (string)created["id"];
        await company.PublishAsync(id, CancellationToken.None);
        var student = ExamService(db, ExamStudentActor()); var list = await student.ListAsync(CancellationToken.None);
        Assert(list[id] != null && (string)list[id]["taskTitle"] == "Student linked task" && list[id]["versionData"]["questions"] == null);
        Assert((await ExamService(db, ExamStudentActor("ST-2")).ListAsync(CancellationToken.None))[id] == null);
        var authorizedExam = (JObject)db.Get("taskExams/" + id);
        var wrongCreator = (JObject)authorizedExam.DeepClone(); wrongCreator["createdBy"] = "UNRELATED-SUPERVISOR";
        db.Set("taskExams/" + id, wrongCreator);
        Assert((await student.ListAsync(CancellationToken.None))[id] == null);
        await Reject<UIMS.Services.FirebaseException>(() => student.StartAttemptAsync(id, CancellationToken.None));
        db.Set("taskExams/" + id, authorizedExam);
        var attempt = await student.StartAttemptAsync(id, CancellationToken.None);
        var pending = await student.SaveAnswersAsync(id, (string)attempt["id"], new JObject { ["Q1"] = "B" }, true, CancellationToken.None);
        Assert(pending["score"] == null && pending["maximumScore"] == null && pending["feedback"] == null && pending["releasedAt"] == null);
    }

    private static async Task ExamComposerRetryOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var company = ExamService(db, ExamCompanyActor());
        var input = ExamDraft(); input["creationKey"] = "task-batch-retry-1";
        var first = await company.CreateDraftAsync(input, CancellationToken.None); var id = (string)first["id"];
        int writes = db.Writes;
        var duplicate = await company.CreateDraftAsync(input, CancellationToken.None);
        Assert((string)duplicate["id"] == id && writes == db.Writes);
        await company.PublishAsync(id, CancellationToken.None); writes = db.Writes;
        duplicate = await company.CreateDraftAsync(input, CancellationToken.None);
        Assert((string)duplicate["id"] == id && (string)duplicate["exam"]["status"] == "published" && writes == db.Writes);
        var other = ExamDraft(); other["creationKey"] = "task-batch-retry-2";
        var next = await company.CreateDraftAsync(other, CancellationToken.None);
        Assert((string)next["id"] != id);
        await Reject<UIMS.Services.FirebaseException>(() => ExamService(db, ExamCompanyActor("CO-2", "CS-2")).CreateDraftAsync(input, CancellationToken.None));
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
        await Reject<UIMS.Services.FirebaseException>(() => student.GetStudentExamAsync(examId, CancellationToken.None));
        var attempt = await student.StartAttemptAsync(examId, CancellationToken.None); var duplicate = await student.StartAttemptAsync(examId, CancellationToken.None);
        var exam = await student.GetStudentExamAsync(examId, CancellationToken.None); Assert(exam["versionData"]["questions"]["Q1"]["options"].HasValues);
        Assert((string)attempt["id"] == (string)duplicate["id"]);
        var result = await student.SaveAnswersAsync(examId, (string)attempt["id"], new JObject { ["Q1"] = "B", ["Q2"] = true }, true, CancellationToken.None);
        Assert(result["score"] == null && result["maximumScore"] == null && (string)result["resultStatus"] == "pendingApproval");
        Assert(db.Get("taskExamAttempts/" + examId + "/ST-1/" + (string)attempt["id"] + "/score") == null);
        var idempotent = await student.SaveAnswersAsync(examId, (string)attempt["id"], new JObject { ["Q1"] = "A" }, true, CancellationToken.None);
        Assert(idempotent["score"] == null && (string)idempotent["status"] == "submitted");
        var resumed = await student.GetMyAttemptAsync(examId, (string)attempt["id"], CancellationToken.None); Assert((string)resumed["answers"]["Q1"] == "B" && resumed["score"] == null);
        await Reject<UIMS.Services.FirebaseException>(() => student.ReleaseResultAsync(examId, "ST-1", (string)attempt["id"], "", CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => ExamService(db, ExamCompanyActor("CO-2", "CS-2")).ReleaseResultAsync(examId, "ST-1", (string)attempt["id"], "", CancellationToken.None));
        var review = await company.ListAttemptsAsync(examId, CancellationToken.None);
        Assert((decimal)review["ST-1"][(string)attempt["id"]]["score"] == 3m);
        await company.ReleaseResultAsync(examId, "ST-1", (string)attempt["id"], "Well done", CancellationToken.None);
        var released = await student.GetMyAttemptAsync(examId, (string)attempt["id"], CancellationToken.None);
        Assert((decimal)released["score"] == 3m && (decimal)released["maximumScore"] == 3m && (string)released["feedback"] == "Well done" && released["releasedAt"] != null);
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
        await Reject<UIMS.Services.FirebaseException>(() => student.DownloadQuestionImageAsync((string)meta["id"], CancellationToken.None));
        await student.StartAttemptAsync(id, CancellationToken.None);
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
    private static async Task ExamWindowAndExpiryOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var company = ExamService(db, ExamCompanyActor());
        var future = ExamDraft(DateTimeOffset.UtcNow.AddHours(1).ToString("o"), DateTimeOffset.UtcNow.AddHours(2).ToString("o"));
        string lockedId = (string)(await company.CreateDraftAsync(future, CancellationToken.None))["id"];
        await company.PublishAsync(lockedId, CancellationToken.None); var student = ExamService(db, ExamStudentActor());
        Assert(!(bool)(await student.EligibilityAsync(lockedId, CancellationToken.None))["eligible"]);
        await Reject<UIMS.Services.FirebaseException>(() => student.StartAttemptAsync(lockedId, CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => student.GetStudentExamAsync(lockedId, CancellationToken.None));
        var summary = (await student.ListAsync(CancellationToken.None))[lockedId]; Assert(summary["versionData"]["questions"] == null && summary["serverNow"] != null);
        string examId = (string)(await company.CreateDraftAsync(ExamDraft(), CancellationToken.None))["id"]; await company.PublishAsync(examId, CancellationToken.None);
        var attempt = await student.StartAttemptAsync(examId, CancellationToken.None); string attemptId = (string)attempt["id"];
        await student.SaveAnswersAsync(examId, attemptId, new JObject { ["Q1"] = "B" }, false, CancellationToken.None);
        string path = "taskExamAttempts/" + examId + "/ST-1/" + attemptId;
        var persisted = (JObject)db.Get(path); persisted["deadlineAt"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("o"); db.Set(path, persisted);
        var expired = await student.SaveAnswersAsync(examId, attemptId, new JObject { ["Q1"] = "A", ["Q2"] = true }, true, CancellationToken.None);
        Assert((string)expired["status"] == "expired" && expired["score"] == null);
        var saved = db.Get(path); Assert((string)saved["answers"]["Q1"] == "B" && saved["answers"]["Q2"] == null);
        var companyView = await company.ListAttemptsAsync(examId, CancellationToken.None); Assert((decimal)companyView["ST-1"][attemptId]["score"] == 2m);
        await company.ReleaseResultAsync(examId, "ST-1", attemptId, "Original saved answers graded", CancellationToken.None);
        var result = await student.GetMyAttemptAsync(examId, attemptId, CancellationToken.None); Assert((decimal)result["score"] == 2m);
        await company.ReleaseResultAsync(examId, "ST-1", attemptId, "Duplicate must not replace feedback", CancellationToken.None);
        Assert((string)(await student.GetMyAttemptAsync(examId, attemptId, CancellationToken.None))["feedback"] == "Original saved answers graded");
        string closedId = (string)(await company.CreateDraftAsync(ExamDraft(), CancellationToken.None))["id"]; await company.PublishAsync(closedId, CancellationToken.None);
        await student.StartAttemptAsync(closedId, CancellationToken.None);
        var closedVersion = (JObject)db.Get("taskExams/" + closedId + "/versions/1"); closedVersion["closesAt"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("o"); db.Set("taskExams/" + closedId + "/versions/1", closedVersion);
        Assert(!(bool)(await student.EligibilityAsync(closedId, CancellationToken.None))["eligible"]);
        await Reject<UIMS.Services.FirebaseException>(() => student.StartAttemptAsync(closedId, CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => student.GetStudentExamAsync(closedId, CancellationToken.None));
    }
    private static async Task ExamValidationOffline()
    {
        var db = new ExamDatabase(); SeedExamRelationships(db); var service = ExamService(db, ExamCompanyActor());
        var empty = ExamDraft(); empty["questions"] = new JArray();
        var duplicate = ExamDraft(); duplicate["questions"][0]["options"][1]["text"] = "3";
        var points = ExamDraft(); points["questions"][0]["points"] = 0;
        var key = ExamDraft(); key["questions"][0]["correctOptionId"] = "invalid";
        var window = ExamDraft(); window["closesAt"] = window["opensAt"];
        foreach (var draft in new[] { empty, duplicate, points, key, window }) await Reject<UIMS.Services.FirebaseException>(() => service.CreateDraftAsync(draft, CancellationToken.None));
        Assert(db.Writes == 0);
        var image = ExamDraft(); image["questions"][0]["imageFileId"] = "missing-image";
        var created = await service.CreateDraftAsync(image, CancellationToken.None);
        await Reject<UIMS.Services.FirebaseException>(() => service.PublishAsync((string)created["id"], CancellationToken.None));
        Assert((string)db.Get("taskExams/" + (string)created["id"] + "/status") == "draft");
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
            if (match != null && request.Method != HttpMethod.Put) return Task.FromResult(Response(HttpStatusCode.BadRequest, "{\"error\":\"Conditional requests require PUT\"}", null));
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
