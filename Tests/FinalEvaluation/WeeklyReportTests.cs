using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIMS.Tasks;

internal static partial class Program
{
    private static async Task RunWeeklyReportTests()
    {
        await Test("academic review persists without enabling Storage or company report uploads", async () => {
            var db = new WeeklyReportDb(); SeedWeekly(db);
            var report = await NewWeeklyReport(WeeklyService(db, WeeklyCompanyActor()));
            var settings = WeeklySettings(); settings["WeeklyReportsLiveWritesEnabled"] = "false"; settings["WeeklyReportReviewsEnabled"] = "true"; settings["FirebaseStorageEnabled"] = settings["FirebaseStorageLiveWritesEnabled"] = "false";
            var reviewed = await WeeklyService(db, WeeklyAcademicActor(), settings).ReviewAsync((string)report["reportId"], (string)report["currentRevisionId"], "approved", "Reviewed exact revision", Guid.NewGuid().ToString("D"), CancellationToken.None);
            Assert((string)reviewed["report"]["status"] == "approved");
            await Reject<UIMS.Services.FirebaseException>(() => NewWeeklyReport(WeeklyService(db, WeeklyCompanyActor(), settings)));
        });
        await Test("academic weekly roster reads current supervisorAssignments and rejects conflicting assignments", async () => {
            var db = new WeeklyReportDb();
            db.Set("supervisorAssignments/CURRENT", new JObject { ["studentId"] = "ST-1", ["academicSupervisorId"] = "AC-1", ["isActive"] = true });
            db.Set("users/U-ST1", new JObject { ["role"] = "student", ["studentId"] = "ST-1", ["academicSupervisorId"] = "AC-1", ["isActive"] = true });
            var service = WeeklyService(db, WeeklyAcademicActor());
            var data = await service.ListAcademicReportsAsync(CancellationToken.None);
            Assert(data["assignedStudents"].Count() == 1);
            db.Set("supervisorAssignments/CONFLICT", new JObject { ["studentId"] = "ST-1", ["academicSupervisorId"] = "AC-2", ["isActive"] = true });
            data = await service.ListAcademicReportsAsync(CancellationToken.None);
            Assert(data["assignedStudents"].Count() == 0);
        });
        await Test("weekly reports persist company DOCX upload, review history and immutable resubmission revisions", WeeklyReportLifecycleOffline);
        await Test("weekly report authorization denies students, other companies and unassigned academic supervisors", WeeklyReportAuthorizationOffline);
        await Test("weekly report duplicate placement-week writes are conditional and retry-safe", WeeklyReportDuplicateConcurrencyOffline);
        await Test("weekly report downloads are scoped to company and currently assigned academic supervisor", WeeklyReportDownloadOffline);
        await Test("failed weekly report metadata writes clean only the new private upload and never publish an incomplete report", WeeklyReportMetadataFailureCleanupOffline);
        await Test("disabled weekly reports make no database, token or Storage requests", WeeklyReportDisabledOffline);
        await Test("weekly report validation rejects invalid dates, MIME, DOCX structure and oversize data before upload", WeeklyReportValidationOffline);
    }

    private static Dictionary<string, string> WeeklySettings(bool enabled = true)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["FirebaseEnabled"] = enabled ? "true" : "false", ["WeeklyReportsEnabled"] = "true", ["WeeklyReportsLiveWritesEnabled"] = "true", ["WeeklyReportsSchemaValidated"] = "true",
            ["FirebaseProjectId"] = "offline-test", ["FirebaseDatabaseUrl"] = "https://offline-test-default-rtdb.europe-west1.firebasedatabase.app", ["FirebaseAuthenticationEnabled"] = "true",
            ["FirebaseLiveWritesEnabled"] = "true", ["FirebaseSchemaValidated"] = "true", ["FirebaseTaskSchemaValidated"] = "true",
            ["FirebaseStorageEnabled"] = "true", ["FirebaseStorageLiveWritesEnabled"] = "true", ["FirebaseStorageBucket"] = "offline-bucket.test"
        };
    }
    private static TaskActor WeeklyCompanyActor(string companyId = "CO-1", string supervisorId = "CS-1")
    { return TaskActor.FromVerifiedProfile("uid-" + supervisorId, "companySupervisor", new JObject { ["role"] = "companySupervisor", ["isActive"] = true, ["companySupervisorId"] = supervisorId, ["companyId"] = companyId }); }
    private static TaskActor WeeklyAcademicActor(string uid = "AC-1")
    { return TaskActor.FromVerifiedProfile(uid, "academicSupervisor", new JObject { ["role"] = "academicSupervisor", ["isActive"] = true }, null, true); }
    private static TaskActor WeeklyStudentActor(string studentId = "ST-1")
    { return TaskActor.FromVerifiedProfile("uid-" + studentId, "student", new JObject { ["role"] = "student", ["isActive"] = true, ["studentId"] = studentId }); }
    private static WeeklyReportService WeeklyService(WeeklyReportDb db, TaskActor actor, Dictionary<string, string> settings = null,
        IDictionary<string, byte[]> files = null, Action<string> beforeUpload = null, Action<string> beforeDelete = null)
    {
        settings = settings ?? WeeklySettings(); files = files ?? new Dictionary<string, byte[]>(StringComparer.Ordinal);
        Func<string, string> readSetting = key => settings.ContainsKey(key) ? settings[key] : null;
        Func<string, string, string, byte[], CancellationToken, Task<UIMS.Services.StoredFile>> upload = (path, name, mime, bytes, token) =>
        {
            beforeUpload?.Invoke(path);
            files[path] = bytes.ToArray();
            string sha256; using (var sha = SHA256.Create()) sha256 = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            return Task.FromResult(new UIMS.Services.StoredFile { ObjectPath = path, FileName = Path.GetFileName(name), ContentType = mime, SizeBytes = bytes.LongLength, Sha256 = sha256, Generation = "offline-generation" });
        };
        Func<string, CancellationToken, Task<byte[]>> download = (path, token) => Task.FromResult(files.ContainsKey(path) ? files[path].ToArray() : null);
        Func<string, CancellationToken, Task> delete = (path, token) => { beforeDelete?.Invoke(path); files.Remove(path); return Task.FromResult(0); };
        return new WeeklyReportService(readSetting, new HttpClient(db), actor, token => Task.FromResult("isolated-weekly-token"), null, upload, download, delete);
    }
    private static void SeedWeekly(WeeklyReportDb db)
    {
        db.Set("placements/PL-1", new JObject { ["studentId"] = "ST-1", ["companyId"] = "CO-1", ["companyName"] = "Fictional Company", ["companySupervisorId"] = "CS-1", ["status"] = "confirmed" });
        db.Set("placements/PL-2", new JObject { ["studentId"] = "ST-2", ["companyId"] = "CO-2", ["companySupervisorId"] = "CS-2", ["status"] = "active" });
        db.Set("assignments/A-1", new JObject { ["studentId"] = "ST-1", ["academicSupervisorId"] = "AC-1", ["active"] = true });
        db.Set("assignments/A-2", new JObject { ["studentId"] = "ST-2", ["academicSupervisorId"] = "AC-2", ["active"] = true });
        db.Set("users/U-ST1", new JObject { ["role"] = "student", ["studentId"] = "ST-1", ["academicSupervisorId"] = "AC-1", ["isActive"] = true });
        db.Set("users/U-ST2", new JObject { ["role"] = "student", ["studentId"] = "ST-2", ["academicSupervisorId"] = "AC-2", ["isActive"] = true });
        db.Set("tasks/T-1", new JObject { ["placementId"] = "PL-1", ["studentId"] = "ST-1", ["title"] = "Task Alpha", ["status"] = "Completed", ["grade"] = 8, ["maximumScore"] = 10 });
        db.Set("taskExams/E-1", new JObject { ["placementId"] = "PL-1", ["status"] = "published" });
        db.Set("taskExamAttempts/E-1/ST-1/ATT-1", new JObject { ["studentId"] = "ST-1", ["status"] = "submitted", ["score"] = 9, ["maximumScore"] = 10, ["answers"] = new JObject { ["Q1"] = "answer-key-must-not-leak" } });
        db.Set("uimsAttendance/students/ST-1", new JObject { ["status"] = "active", ["activeAssignmentId"] = "TA-1" });
        db.Set("uimsAttendance/trainingAssignments/TA-1", new JObject { ["studentId"] = "ST-1", ["companySupervisorId"] = "CS-1", ["status"] = "active" });
        db.Set("uimsAttendance/attendance/TA-1/2026-10-21", new JObject { ["studentId"] = "ST-1", ["assignmentId"] = "TA-1", ["status"] = "completed", ["hoursApprovalStatus"] = "approved", ["workedMinutes"] = 90 });
    }
    private static byte[] WeeklyDocx()
    {
        using (var memory = new MemoryStream())
        {
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            {
                foreach (string entryName in new[] { "[Content_Types].xml", "word/document.xml" })
                { var entry = archive.CreateEntry(entryName); entry.LastWriteTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero); using (var writer = new StreamWriter(entry.Open())) writer.Write("<root>fictional test report</root>"); }
            }
            return memory.ToArray();
        }
    }
    private static string WeeklyMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private static async Task<JObject> NewWeeklyReport(WeeklyReportService service, string placementId = "PL-1", int week = 1)
    { var response = await service.SubmitAsync(placementId, week, "2026-10-19", "2026-10-25", "week.docx", WeeklyMime, WeeklyDocx(), "Strong progress this week.", CancellationToken.None); return (JObject)response["report"]; }

    private static async Task WeeklyReportLifecycleOffline()
    {
        var db = new WeeklyReportDb(); SeedWeekly(db); var files = new Dictionary<string, byte[]>(StringComparer.Ordinal); var company = WeeklyService(db, WeeklyCompanyActor(), null, files);
        var report = await NewWeeklyReport(company); string id = (string)report["reportId"], revision1 = (string)report["currentRevisionId"];
        Assert(id.StartsWith("WR-", StringComparison.Ordinal) && (string)db.Get("weeklyReports/" + id + "/status") == "submitted");
        Assert((string)db.Get("weeklyReports/" + id + "/revisions/" + revision1 + "/contentType") == WeeklyMime && (string)db.Get("fileAttachments/" + (string)db.Get("weeklyReports/" + id + "/revisions/" + revision1 + "/fileId") + "/status") == "ready");
        var academic = WeeklyService(db, WeeklyAcademicActor(), null, files); var detail = await academic.GetAcademicReportAsync(id, CancellationToken.None);
        Assert((int)detail["progress"]["tasks"].Count() == 1 && (decimal)detail["progress"]["examResults"][0]["score"] == 9m);
        Assert((long)detail["progress"]["approvedTrainingMinutes"] == 90 && detail.ToString().IndexOf("answer-key-must-not-leak", StringComparison.Ordinal) < 0);
        var academicList = await academic.ListAcademicReportsAsync(CancellationToken.None);
        Assert((string)academicList["assignedStudents"][0]["studentId"] == "ST-1" && (string)academicList["assignedStudents"][0]["placements"][0]["companyName"] == "Fictional Company");
        var review = await academic.ReviewAsync(id, revision1, "revisionRequested", "Please add the supervisor's weekly summary.", Guid.NewGuid().ToString("D"), CancellationToken.None);
        Assert((string)review["report"]["status"] == "revisionRequested" && (string)review["review"]["decision"] == "revisionRequested");
        var afterRequest = await company.GetCompanyReportAsync(id, CancellationToken.None); Assert(afterRequest["reviews"].Children().Count() == 1);
        var resubmitted = await company.ResubmitAsync(id, "week-revised.docx", WeeklyMime, WeeklyDocx(), "Updated report.", CancellationToken.None);
        var report2 = (JObject)resubmitted["report"]; string revision2 = (string)report2["currentRevisionId"];
        Assert((string)report2["status"] == "resubmitted" && revision1 != revision2 && report2["revisions"][revision1] != null && report2["revisions"][revision2] != null);
        await Reject<UIMS.Services.FirebaseException>(() => academic.ReviewAsync(id, revision1, "approved", "Stale review", Guid.NewGuid().ToString("D"), CancellationToken.None));
        var final = await academic.ReviewAsync(id, revision2, "approved", "Approved after review.", Guid.NewGuid().ToString("D"), CancellationToken.None);
        Assert((string)final["report"]["status"] == "approved" && (await company.GetCompanyReportAsync(id, CancellationToken.None))["reviews"].Children().Count() == 2);
        var student = WeeklyService(db, WeeklyStudentActor(), null, files); var studentList = await student.ListStudentReportsAsync(CancellationToken.None);
        Assert((string)studentList["reports"][id]["status"] == "approved" && studentList.ToString().IndexOf("storagePath", StringComparison.Ordinal) < 0);
        Assert(files.Count == 2 && files.Values.All(v => v.SequenceEqual(WeeklyDocx())));
    }
    private static async Task WeeklyReportAuthorizationOffline()
    {
        var db = new WeeklyReportDb(); SeedWeekly(db); var files = new Dictionary<string, byte[]>();
        await Reject<UIMS.Services.FirebaseException>(() => Task.FromResult(TaskActor.FromVerifiedProfile("AC-1", "academicSupervisor", new JObject { ["role"] = "academicSupervisor", ["isActive"] = true })));
        await Reject<UIMS.Services.FirebaseException>(() => NewWeeklyReport(WeeklyService(db, WeeklyStudentActor(), null, files)));
        var report = await NewWeeklyReport(WeeklyService(db, WeeklyCompanyActor(), null, files)); string id = (string)report["reportId"];
        await Reject<UIMS.Services.FirebaseException>(() => NewWeeklyReport(WeeklyService(db, WeeklyCompanyActor("CO-2", "CS-2"), null, files), "PL-1", 2));
        await Reject<UIMS.Services.FirebaseException>(() => WeeklyService(db, WeeklyAcademicActor("AC-2"), null, files).GetAcademicReportAsync(id, CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => WeeklyService(db, WeeklyAcademicActor("AC-2"), null, files).ReviewAsync(id, (string)report["currentRevisionId"], "approved", "No", Guid.NewGuid().ToString("D"), CancellationToken.None));
        Assert(db.Writes == 2); // exactly the file metadata and one report create
    }
    private static async Task WeeklyReportDuplicateConcurrencyOffline()
    {
        var db = new WeeklyReportDb(); SeedWeekly(db); var files = new Dictionary<string, byte[]>(); var svc1 = WeeklyService(db, WeeklyCompanyActor(), null, files); var svc2 = WeeklyService(db, WeeklyCompanyActor(), null, files);
        var first = NewWeeklyReport(svc1, "PL-1", 4); var second = NewWeeklyReport(svc2, "PL-1", 4); var results = await Task.WhenAll(Capture(first), Capture(second));
        Assert(results.Count(x => x is JObject) == 1 && results.Count(x => x is Exception) == 1);
        Assert(db.Get("weeklyReports/" + (string)((JObject)results.First(x => x is JObject))["reportId"] + "/revisions") is JObject);
        Assert(db.Get("weeklyReports").Children().Count() == 1);
    }
    private static async Task<object> Capture(Task<JObject> task) { try { return await task; } catch (Exception e) { return e; } }
    private static async Task WeeklyReportDownloadOffline()
    {
        var db = new WeeklyReportDb(); SeedWeekly(db); var files = new Dictionary<string, byte[]>(); var report = await NewWeeklyReport(WeeklyService(db, WeeklyCompanyActor(), null, files));
        string id = (string)report["reportId"], revision = (string)report["currentRevisionId"];
        var company = await WeeklyService(db, WeeklyCompanyActor(), null, files).DownloadAsync(id, revision, CancellationToken.None); Assert(company.Content.SequenceEqual(WeeklyDocx()));
        var academic = await WeeklyService(db, WeeklyAcademicActor(), null, files).DownloadAsync(id, revision, CancellationToken.None); Assert(academic.Content.SequenceEqual(WeeklyDocx()));
        await Reject<UIMS.Services.FirebaseException>(() => WeeklyService(db, WeeklyStudentActor(), null, files).DownloadAsync(id, revision, CancellationToken.None));
        db.Set("assignments/A-1", new JObject { ["studentId"] = "ST-1", ["academicSupervisorId"] = "AC-2", ["active"] = true });
        await Reject<UIMS.Services.FirebaseException>(() => WeeklyService(db, WeeklyAcademicActor(), null, files).DownloadAsync(id, revision, CancellationToken.None));
    }
    private static async Task WeeklyReportMetadataFailureCleanupOffline()
    {
        foreach (string failedPrefix in new[] { "fileAttachments/", "weeklyReports/" })
        {
            var db = new WeeklyReportDb { FailWritePrefix = failedPrefix }; SeedWeekly(db); var files = new Dictionary<string, byte[]>(); var deleted = new List<string>();
            await Reject<UIMS.Services.FirebaseException>(() => NewWeeklyReport(WeeklyService(db, WeeklyCompanyActor(), null, files, null, path => deleted.Add(path))));
            var reports = db.Get("weeklyReports") as JObject; var attachments = db.Get("fileAttachments") as JObject;
            Assert((reports == null || !reports.Properties().Any()) && (attachments == null || !attachments.Properties().Any()));
            Assert(files.Count == 0 && deleted.Count == 1 && deleted[0].StartsWith("uims/weekly-reports/", StringComparison.Ordinal));
        }
    }
    private static async Task WeeklyReportDisabledOffline()
    {
        var db = new WeeklyReportDb(); SeedWeekly(db); int tokenCalls = 0, uploads = 0;
        var service = new WeeklyReportService(k => WeeklySettings(false).ContainsKey(k) ? WeeklySettings(false)[k] : null, new HttpClient(db), WeeklyCompanyActor(), ct => { tokenCalls++; return Task.FromResult("unused"); }, null,
            (path, name, mime, bytes, ct) => { uploads++; return Task.FromResult(new UIMS.Services.StoredFile()); }, null);
        await Reject<UIMS.Services.FirebaseException>(() => service.ListCompanyPlacementsAsync(CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => NewWeeklyReport(service));
        Assert(db.RequestCount == 0 && tokenCalls == 0 && uploads == 0);
    }
    private static async Task WeeklyReportValidationOffline()
    {
        var db = new WeeklyReportDb(); SeedWeekly(db); var files = new Dictionary<string, byte[]>(); int uploads = 0;
        var service = WeeklyService(db, WeeklyCompanyActor(), null, files, _ => uploads++);
        await Reject<UIMS.Services.FirebaseException>(() => service.SubmitAsync("PL-1", 1, "bad-date", "2026-10-25", "x.docx", WeeklyMime, WeeklyDocx(), "", CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => service.SubmitAsync("PL-1", 1, "2026-10-19", "2026-10-25", "x.docx", "text/plain", WeeklyDocx(), "", CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => service.SubmitAsync("PL-1", 1, "2026-10-19", "2026-10-25", "x.docx", WeeklyMime, new byte[] { 0x50, 0x4b, 0, 1 }, "", CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => service.SubmitAsync("PL-1", 1, "2026-10-19", "2026-10-25", "x.docx", WeeklyMime, new byte[UIMS.Services.FirebaseFileStorageService.MaximumFileBytes + 1], "", CancellationToken.None));
        Assert(uploads == 0 && files.Count == 0 && db.Writes == 0);
    }

    private sealed class WeeklyReportDb : HttpMessageHandler
    {
        private readonly JObject root = new JObject(); private readonly Dictionary<string, int> versions = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly object gate = new object(); public int RequestCount { get; private set; } public int Writes { get; private set; } public string FailWritePrefix { get; set; }
        public void Set(string path, JObject value) { lock (gate) { Put(path, value); Bump(path); } }
        public JToken Get(string path) { lock (gate) return GetAt(path)?.DeepClone(); }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (gate)
            {
                RequestCount++; var uri = request.RequestUri; string rawPath = uri.AbsolutePath.Trim('/');
                if (rawPath.EndsWith(".json", StringComparison.Ordinal)) rawPath = rawPath.Substring(0, rawPath.Length - 5);
                string path = string.Join("/", rawPath.Split('/').Select(Uri.UnescapeDataString));
                if (request.Method == HttpMethod.Get && uri.Query.Length > 0)
                {
                    var query = HttpUtility.ParseQueryString(uri.Query); string field = JsonConvert.DeserializeObject<string>(query["orderBy"] ?? "\"\""); string equal = JsonConvert.DeserializeObject<string>(query["equalTo"] ?? "\"\"");
                    var source = GetAt(path) as JObject ?? new JObject(); var filtered = new JObject(); foreach (var p in source.Properties()) if (p.Value is JObject && ((string)p.Value[field] ?? "") == equal) filtered[p.Name] = p.Value.DeepClone();
                    return Task.FromResult(Reply(filtered, null));
                }
                var target = GetAt(path); string etag = Etag(path);
                if (request.Method == HttpMethod.Get) return Task.FromResult(Reply(target, etag));
                if (request.Method == HttpMethod.Put && !string.IsNullOrEmpty(FailWritePrefix) && path.StartsWith(FailWritePrefix, StringComparison.Ordinal))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                Writes++;
                IEnumerable<string> match; string requested = request.Headers.TryGetValues("if-match", out match) ? match.FirstOrDefault() : null;
                if (requested == "null_etag" ? target != null : requested != null && requested != etag) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.PreconditionFailed));
                if (request.Method == HttpMethod.Delete) { Remove(path); Bump(path); return Task.FromResult(Reply(null, Etag(path))); }
                var body = request.Content == null ? new JObject() : JObject.Parse(request.Content.ReadAsStringAsync().Result);
                if (request.Method.Method == "PATCH")
                {
                    var obj = target as JObject ?? new JObject(); foreach (var property in body.Properties()) obj[property.Name] = property.Value.DeepClone(); body = obj;
                }
                Put(path, body); Bump(path); return Task.FromResult(Reply(body, Etag(path)));
            }
        }
        private HttpResponseMessage Reply(JToken value, string etag)
        { var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(value == null ? "null" : value.ToString(Formatting.None)) }; if (etag != null) response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(etag); return response; }
        private string Etag(string path) { int version; if (!versions.TryGetValue(path, out version)) version = 0; return "\"v" + version.ToString(CultureInfo.InvariantCulture) + "\""; }
        private void Bump(string path) { versions[path] = versions.ContainsKey(path) ? versions[path] + 1 : 1; }
        private JToken GetAt(string path) { JToken item = root; foreach (string part in path.Split('/').Where(p => p.Length > 0)) { var obj = item as JObject; if (obj == null || obj[part] == null) return null; item = obj[part]; } return item; }
        private void Put(string path, JToken value) { string[] p = path.Split('/').Where(x => x.Length > 0).ToArray(); JObject obj = root; for (int i = 0; i < p.Length - 1; i++) { if (!(obj[p[i]] is JObject)) obj[p[i]] = new JObject(); obj = (JObject)obj[p[i]]; } obj[p[p.Length - 1]] = value == null ? JValue.CreateNull() : value.DeepClone(); }
        private void Remove(string path) { string[] p = path.Split('/').Where(x => x.Length > 0).ToArray(); JObject obj = root; for (int i = 0; i < p.Length - 1; i++) { obj = obj[p[i]] as JObject; if (obj == null) return; } obj.Remove(p[p.Length - 1]); }
    }
}
