using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;

internal static partial class Program
{
    private sealed class FinalDb : IApplicationDatabase
    {
        internal readonly Dictionary<string, JObject> Values = new Dictionary<string, JObject>();
        private readonly Dictionary<string, int> versions = new Dictionary<string, int>();
        internal string LoseResponseAt, FailBeforeAt;
        internal int Writes;
        internal readonly Dictionary<string,int> ReadCounts = new Dictionary<string,int>();
        internal void Seed(string path, JObject value) { Values[path] = (JObject)value.DeepClone(); versions[path] = versions.ContainsKey(path) ? versions[path] + 1 : 1; }
        public Task<bool> IsActiveAuthenticationUidAsync(string uid) { return Task.FromResult(Values.ContainsKey("users/" + uid)); }
        public Task<JToken> ReadAsync(string path)
        {
            lock (Values)
            {
                ReadCounts[path] = ReadCounts.ContainsKey(path) ? ReadCounts[path] + 1 : 1;
                JObject value; if (Values.TryGetValue(path, out value)) return Task.FromResult<JToken>(value.DeepClone());
                var map = new JObject(); foreach (var pair in Values.Where(p => p.Key.StartsWith(path + "/", StringComparison.Ordinal) && !p.Key.Substring(path.Length + 1).Contains('/'))) map[pair.Key.Substring(path.Length + 1)] = pair.Value.DeepClone();
                return Task.FromResult<JToken>(map.HasValues ? map : null);
            }
        }
        public async Task<JToken> ReadEqualAsync(string path, string field, string value) { var map = await ReadAsync(path) as JObject ?? new JObject(); return new JObject(map.Properties().Where(p => (string)p.Value[field] == value).Select(p => new JProperty(p.Name, p.Value.DeepClone()))); }
        public Task<StudentVersion> ReadVersionAsync(string path) { lock (Values) { JObject value; Values.TryGetValue(path, out value); return Task.FromResult(new StudentVersion { Value = value == null ? null : (JObject)value.DeepClone(), ETag = value == null ? "null_etag" : "v" + versions[path] }); } }
        public Task<bool> CompareWriteAsync(string path, object value, string etag)
        {
            lock (Values)
            {
                if (FailBeforeAt == path) { FailBeforeAt = null; throw new System.IO.IOException("isolated transport failure"); }
                string current = Values.ContainsKey(path) ? "v" + versions[path] : "null_etag"; if (etag != current) return Task.FromResult(false);
                Seed(path, value as JObject ?? JObject.FromObject(value)); Writes++;
                if (LoseResponseAt == path) { LoseResponseAt = null; throw new System.IO.IOException("isolated lost acknowledgment"); }
                return Task.FromResult(true);
            }
        }
    }
    private static readonly DateTimeOffset FinalNow = new DateTimeOffset(2030, 5, 1, 12, 0, 0, TimeSpan.Zero);
    private static FinalDb FinalSeed(long minutes = 18000)
    {
        var db = new FinalDb();
        db.Seed("users/company", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companyId\":\"CO\",\"companySupervisorId\":\"SUP\"}"));
        db.Seed("users/academic", JObject.Parse("{\"role\":\"academicSupervisor\",\"isActive\":true}"));
        db.Seed("users/otherAcademic", JObject.Parse("{\"role\":\"academicSupervisor\",\"isActive\":true}"));
        db.Seed("users/admin", JObject.Parse("{\"role\":\"admin\",\"isActive\":true}"));
        db.Seed("users/student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST\",\"academicSupervisorId\":\"academic\"}"));
        db.Seed("placements/P", JObject.Parse("{\"studentId\":\"ST\",\"companyId\":\"CO\",\"companySupervisorId\":\"SUP\",\"academicSupervisorId\":\"academic\",\"status\":\"confirmed\",\"trainingAssignmentId\":\"AT\"}"));
        db.Seed("uimsAttendance/trainingAssignments/AT", JObject.Parse("{\"studentId\":\"ST\",\"companyId\":\"CO\",\"status\":\"active\",\"placementId\":\"P\"}"));
        var rows = new JObject(); long remaining = minutes;
        for (int i = 0; remaining > 0; i++) { long length = Math.Min(600, remaining); var start = FinalNow.AddDays(-50 + i).Date.AddHours(6); var stamp = new DateTimeOffset(start, TimeSpan.Zero).ToUnixTimeMilliseconds(); string day = start.ToString("yyyy-MM-dd"); rows[day] = new JObject { ["studentId"] = "ST", ["assignmentId"] = "AT", ["companyId"] = "CO", ["companySupervisorId"] = "SUP", ["hoursApprovedById"] = "SUP", ["hoursApprovalStatus"] = "approved", ["status"] = "completed", ["checkInAt"] = stamp, ["checkOutAt"] = stamp + length * 60000, ["workedMinutes"] = length }; remaining -= length; }
        db.Seed("uimsAttendance/attendance/AT", rows); return db;
    }
    private static FinalReportWorkflow FinalService(FinalDb db) { return new FinalReportWorkflow(db, () => FinalNow); }
    private static JObject FInput(string id, string revision = null) { return new JObject { ["requestId"] = id, ["revisionId"] = revision ?? "", ["expectedDraftVersion"] = "save" }; }
    private static JObject FDraft(string id = "save") { return new JObject { ["requestId"] = id, ["percentage"] = 88, ["comments"] = "Company report <script>unsafe()</script>", ["fileName"] = "company-final.pdf" }; }
    private static async Task<string> Submitted(FinalReportWorkflow w) { await w.ActAsync("company", "companySupervisor", "P", "save", FDraft()); var v = await w.ActAsync("company", "companySupervisor", "P", "submit", FInput("submit")); return (string)v["record"]["currentRevisionId"]; }
    private static async Task<string> AcademicallyApproved(FinalReportWorkflow w) { string revision = await Submitted(w); var input = FInput("academic-approve", revision); input["feedback"] = "Private academic review"; await w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", input); return revision; }
    private static JObject FApprove(string revision) { var input = FInput("admin-final", revision); input["finalGrade"] = 91.5m; input["fileName"] = "completion-notice.pdf"; input["feedback"] = "Private admin notes"; return input; }
    private static async Task RunFinalReportTests()
    {
        await Test("final list reuses request reads but observes changes on subsequent requests", async () => {
            var db = FinalSeed(11); var placement=(JObject)await db.ReadAsync("placements/P");db.Seed("placements/P2",placement);db.ReadCounts.Clear();
            var service=FinalService(db);await service.ListAsync("company","companySupervisor");
            Assert(db.ReadCounts["users"] == 1 && db.ReadCounts["placements"] == 1 && db.ReadCounts["supervisorAssignments"] == 1 && db.Writes == 0);
            var profile=(JObject)await db.ReadAsync("users/student");profile["fullName"]="Updated student name";db.Seed("users/student",profile);
            var next=await service.ListAsync("company","companySupervisor");Assert((string)next["items"][0]["studentName"]=="Updated student name");
        });
        await Test("final private PDF generation persists provider and supports idempotent authorized downloads", async () => {
            var db = FinalSeed(); var handler = new IsolatedStorageHandler(); var workflow = FinalService(db); workflow.ReportStorage = OfflineStorage(handler, StorageSettings());
            db.Seed("tasks/T-PDF", new JObject { ["studentId"]="ST", ["placementId"]="P", ["title"]="Completed training task", ["status"]="Completed", ["score"]=88, ["maximumScore"]=100 });
            var draftInput=FDraft();draftInput["useCalculatedGrade"]=true;
            await workflow.ActAsync("company", "companySupervisor", "P", "save", draftInput);
            var result = await workflow.GenerateDocumentAsync("company", "companySupervisor", "P", "draft");
            Assert((string)result["record"]["draft"]["attachment"]["uploadStatus"] == "ready" && handler.Uploads == 1);
            await workflow.GenerateDocumentAsync("company", "companySupervisor", "P", "draft"); Assert(handler.Uploads == 1);
            var pdf = await workflow.DownloadDocumentAsync("company", "companySupervisor", "P", "draft"); Assert(pdf.ContentType == "application/pdf");
            University_Internship_Management_System.Services.FinalEvaluation.CompletionReportPdfRenderer.ValidatePdf(pdf.Content);
            using(var memory=new System.IO.MemoryStream(pdf.Content))using(var document=PdfSharp.Pdf.IO.PdfReader.Open(memory,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))Assert(document.PageCount==1);
            var preview = Environment.GetEnvironmentVariable("UIMS_FINAL_PDF_PREVIEW"); if (!string.IsNullOrWhiteSpace(preview)) System.IO.File.WriteAllBytes(preview, pdf.Content);
            await Reject<FirebaseException>(() => workflow.DownloadDocumentAsync("student", "student", "P", "draft"));
            await Reject<FirebaseException>(() => workflow.DownloadDocumentAsync("academic", "academicSupervisor", "P", "draft"));
            var revision = await workflow.ActAsync("company", "companySupervisor", "P", "submit", FInput("submit-pdf"));
            var academic = await workflow.DownloadDocumentAsync("academic", "academicSupervisor", "P", (string)revision["record"]["currentRevisionId"]);
            Assert(pdf.Content.SequenceEqual(academic.Content));
        });
        await Test("final report list displays student identity and academic name from authorized profiles", async () => {
            var db = FinalSeed(11); var profile = (JObject)await db.ReadAsync("users/student"); profile["fullName"] = "Assigned student"; profile["privateNote"] = "DO_NOT_DISCLOSE"; db.Seed("users/student", profile);
            profile = (JObject)await db.ReadAsync("users/academic"); profile["fullName"] = "Assigned academic"; db.Seed("users/academic", profile);
            var view = await FinalService(db).ReadAsync("company", "companySupervisor", "P");
            Assert((string)view["studentName"] == "Assigned student" && (string)view["studentId"] == "ST" && (string)view["academicSupervisorName"] == "Assigned academic" && !view.ToString().Contains("DO_NOT_DISCLOSE") && db.Writes == 0);
        });
        await Test("automatic final grade normalizes task and latest released exam without double counting", async () => {
            var db = FinalSeed(11); var service = FinalService(db);
            db.Seed("tasks/T", new JObject { ["studentId"] = "ST", ["placementId"] = "P", ["title"] = "Ordinary task", ["status"] = "Completed", ["score"] = 8, ["maximumScore"] = 10 });
            db.Seed("tasks/TE", new JObject { ["studentId"] = "ST", ["placementId"] = "P", ["title"] = "Linked exam task", ["status"] = "Completed", ["score"] = 100, ["maximumScore"] = 100 });
            db.Seed("taskExams/E", new JObject { ["placementId"] = "P", ["taskId"] = "TE", ["title"] = "Assessment" });
            db.Seed("taskExamAttempts/E/ST", new JObject { ["A"] = new JObject { ["status"] = "submitted", ["resultStatus"] = "released", ["score"] = 18, ["maximumScore"] = 20 } });
            var source = await service.SourceSummaryAsync("company", "companySupervisor", "P");
            Assert((decimal)source["gradeCalculation"]["percentage"] == 85m && (int)source["gradeCalculation"]["includedCount"] == 2);
            var input = FDraft(); input["percentage"] = 1; input["useCalculatedGrade"] = true;
            var saved = await service.ActAsync("company", "companySupervisor", "P", "save", input);
            Assert((decimal)saved["record"]["draft"]["percentage"] == 85m && saved["record"]["draft"]["sourceSnapshot"] != null);
            Assert((string)saved["status"] == "draft");
        });
        await Test("automatic final grade supports exams only and excludes a newer unreleased attempt", async () => {
            var db = FinalSeed(11); var service = FinalService(db);
            db.Seed("taskExams/E", new JObject { ["placementId"] = "P", ["title"] = "Assessment" });
            db.Seed("taskExamAttempts/E/ST", new JObject { ["A"] = new JObject { ["status"] = "submitted", ["resultStatus"] = "released", ["score"] = 18, ["maximumScore"] = 20, ["submittedAt"] = "2030-04-01T10:00:00Z" } });
            var source = await service.SourceSummaryAsync("company", "companySupervisor", "P"); Assert((decimal)source["gradeCalculation"]["percentage"] == 90m);
            db.Seed("taskExamAttempts/E/ST", new JObject { ["A"] = new JObject { ["status"] = "submitted", ["resultStatus"] = "released", ["score"] = 18, ["maximumScore"] = 20, ["submittedAt"] = "2030-04-01T10:00:00Z" }, ["B"] = new JObject { ["status"] = "submitted", ["resultStatus"] = "pendingApproval", ["score"] = 998877, ["submittedAt"] = "2030-04-02T10:00:00Z" } });
            source = await service.SourceSummaryAsync("company", "companySupervisor", "P"); Assert(source["gradeCalculation"]["percentage"].Type == JTokenType.Null && (int)source["gradeCalculation"]["excludedCount"] == 1 && !source.ToString().Contains("998877"));
        });
        await Test("automatic final grade supports tasks only and does not fabricate an empty grade", async () => {
            var db = FinalSeed(11); var service = FinalService(db);
            var none = await service.SourceSummaryAsync("company", "companySupervisor", "P"); Assert(none["gradeCalculation"]["percentage"].Type == JTokenType.Null);
            db.Seed("tasks/T", new JObject { ["studentId"] = "ST", ["placementId"] = "P", ["status"] = "Completed", ["score"] = 4, ["maximumScore"] = 5 });
            var source = await service.SourceSummaryAsync("company", "companySupervisor", "P"); Assert((decimal)source["gradeCalculation"]["percentage"] == 80m);
        });
        await Test("final source summary is automatic read-only and excludes private exam grades", async () => {
            var db = FinalSeed(11); var service = FinalService(db);
            db.Seed("tasks/T", new JObject { ["studentId"] = "ST", ["placementId"] = "P", ["title"] = "Verified task", ["status"] = "completed" });
            db.Seed("taskExams/E", new JObject { ["placementId"] = "P", ["title"] = "Verified exam" });
            db.Seed("taskExamAttempts/E/ST", new JObject { ["A"] = new JObject { ["status"] = "submitted", ["resultStatus"] = "pendingApproval", ["score"] = 998877, ["answers"] = "PRIVATE_ANSWER" } });
            var summary = await service.SourceSummaryAsync("company", "companySupervisor", "P");
            Assert((long)summary["approvedTrainingMinutes"] == 11 && summary["tasks"].Count() == 1 && summary["exams"].Count() == 1);
            Assert(!summary.ToString().Contains("998877") && !summary.ToString().Contains("PRIVATE_ANSWER") && db.Writes == 0);
            await Reject<FirebaseException>(() => service.SourceSummaryAsync("student", "student", "P"));
        });

        await Test("final report weekly selection is placement-scoped, server-snapshotted, immutable and private until final approval", async () => {
            var db = FinalSeed(); var w = FinalService(db);
            var weekly = JObject.Parse("{\"placementId\":\"P\",\"studentId\":\"ST\",\"companyId\":\"CO\",\"companySupervisorId\":\"SUP\",\"weekNumber\":1,\"status\":\"approved\",\"currentRevisionId\":\"W1\",\"revisions\":{\"W1\":{\"companyComments\":\"Original weekly summary\",\"fileName\":\"weekly.docx\"}}}");
            db.Seed("weeklyReports/WR", weekly); var foreign = (JObject)weekly.DeepClone(); foreign["studentId"] = "OTHER"; db.Seed("weeklyReports/FOREIGN", foreign);
            Assert(((JArray)(await w.WeeklyReportsAsync("company", "companySupervisor", "P"))["reports"]).Count == 1);
            await Reject<FirebaseException>(() => w.WeeklyReportsAsync("student", "student", "P"));
            var bad = FDraft("bad-weekly"); bad["weeklyReportIds"] = new JArray("FOREIGN"); await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", bad)); Assert(db.Writes == 0);
            var draft = FDraft(); draft["weeklyReportIds"] = new JArray("WR"); await w.ActAsync("company", "companySupervisor", "P", "save", draft);
            weekly["revisions"]["W1"]["companyComments"] = "Changed later"; db.Seed("weeklyReports/WR", weekly);
            var submitted = await w.ActAsync("company", "companySupervisor", "P", "submit", FInput("submit")); string rev = (string)submitted["record"]["currentRevisionId"];
            var academic = await w.ReadAsync("academic", "academicSupervisor", "P"); Assert((string)academic["record"]["revisions"][rev]["weeklyReports"][0]["companyComments"] == "Original weekly summary");
            var student = await w.ReadAsync("student", "student", "P"); Assert(!student.ToString().Contains("Original weekly summary") && student["outcome"].Type == JTokenType.Null);
            await w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", FInput("academic-weekly", rev)); await w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)); Assert((bool)(await w.ReadAsync("student", "student", "P"))["completed"]);
        });
        await Test("final reports reject 17,999 minutes, accept exactly 18,000 and preserve minute precision", async () => {
            foreach (var minutes in new long[] { 17999, 18000, 18001 }) { var db = FinalSeed(minutes); var w = FinalService(db); await w.ActAsync("company", "companySupervisor", "P", "save", FDraft()); if (minutes < 18000) await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "submit", FInput("submit"))); else { var v = await w.ActAsync("company", "companySupervisor", "P", "submit", FInput("submit")); Assert((long)v["record"]["approvedTrainingMinutes"] == minutes); } }
        });
        await Test("approved attendance excludes pending, rejected, incomplete, wrong assignment, wrong company and duplicate date keys", () => {
            var db = FinalSeed(18000); var rows = db.Values["uimsAttendance/attendance/AT"]; var first = rows.Properties().First(); var copy = (JObject)first.Value.DeepClone(); rows["duplicate"] = copy;
            Assert(UIMS.Attendance.ApprovedTrainingMinutes.Calculate(rows, "AT", "ST", "CO", FinalNow.ToUnixTimeMilliseconds()) == 18000);
            foreach (var key in new[] { "hoursApprovalStatus", "assignmentId", "companyId", "hoursApprovedById", "status" }) { var broken = (JObject)rows.DeepClone(); broken[first.Name][key] = "invalid"; Assert(UIMS.Attendance.ApprovedTrainingMinutes.Calculate(broken, "AT", "ST", "CO", FinalNow.ToUnixTimeMilliseconds()) == 17400); }
            var incomplete = (JObject)rows.DeepClone(); incomplete[first.Name]["checkOutAt"] = 0; Assert(UIMS.Attendance.ApprovedTrainingMinutes.Calculate(incomplete, "AT", "ST", "CO", FinalNow.ToUnixTimeMilliseconds()) == 17400); return Task.CompletedTask;
        });
        await Test("final report authorization rejects students writing, wrong company and unassigned academics", async () => {
            var db = FinalSeed(); var w = FinalService(db); await Reject<FirebaseException>(() => w.ActAsync("student", "student", "P", "save", FDraft())); await Submitted(w);
            await Reject<FirebaseException>(() => w.ReadAsync("otherAcademic", "academicSupervisor", "P"));
            db.Values["users/company"]["companyId"] = "OTHER"; await Reject<FirebaseException>(() => w.ReadAsync("company", "companySupervisor", "P"));
        });
        await Test("company and academic revision cycles preserve immutable previous revisions and require both feedback and response", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await Submitted(w);
            for (int i = 0; i < 3; i++) { var request = FInput("return-" + i, rev); await Reject<FirebaseException>(() => w.ActAsync("academic", "academicSupervisor", "P", "companyRevision", request)); request["feedback"] = "Revise report " + i; await w.ActAsync("academic", "academicSupervisor", "P", "companyRevision", request); await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "submit", FInput("resubmit-" + i))); var draft = FDraft("save-" + i); draft["companyResponse"] = "Addressed feedback " + i; draft["expectedDraftVersion"] = "return-return-" + i; await w.ActAsync("company", "companySupervisor", "P", "save", draft); var v = await w.ActAsync("company", "companySupervisor", "P", "submit", new JObject { ["requestId"] = "resubmit-" + i, ["expectedDraftVersion"] = "save-" + i }); rev = (string)v["record"]["currentRevisionId"]; Assert(((JObject)v["record"]["revisions"]).Count == i + 2); }
        });
        await Test("admin queue requires academic approval; admin corrections go through academics and company changes need fresh approval", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await Submitted(w); Assert(((JArray)(await w.ListAsync("admin", "admin"))["items"]).Count == 0);
            await Reject<FirebaseException>(() => w.ReadAsync("admin", "admin", "P")); await w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", FInput("a", rev));
            var correction = FInput("correction", rev); correction["feedback"] = "Academic clarification needed"; await w.ActAsync("admin", "admin", "P", "adminCorrection", correction);
            await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", FDraft("bad")));
            var approved = FInput("a2", rev); approved["feedback"] = "Clarified"; await w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", approved);
            correction["requestId"] = "correction2"; await w.ActAsync("admin", "admin", "P", "adminCorrection", correction);
            var toCompany = FInput("back-company", rev); toCompany["feedback"] = "Company changes needed"; await w.ActAsync("academic", "academicSupervisor", "P", "companyRevision", toCompany);
            var draft = FDraft("save-new"); draft["companyResponse"] = "Changed"; draft["expectedDraftVersion"] = "return-back-company"; await w.ActAsync("company", "companySupervisor", "P", "save", draft); var newView = await w.ActAsync("company", "companySupervisor", "P", "submit", new JObject { ["requestId"] = "submit-new", ["expectedDraftVersion"] = "save-new" });
            await Reject<FirebaseException>(() => w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", FInput("stale", rev))); Assert((string)newView["record"]["status"] == "pendingAcademicReview");
        });
        await Test("duplicate request IDs reconcile lost responses and different content fails without duplicate history", async () => {
            var db = FinalSeed(); var w = FinalService(db); db.LoseResponseAt = "finalEvaluationWorkflows/P"; await w.ActAsync("company", "companySupervisor", "P", "save", FDraft()); await w.ActAsync("company", "companySupervisor", "P", "save", FDraft()); Assert(((JObject)db.Values["finalEvaluationWorkflows/P"]["audit"]).Count == 1); var altered = FDraft(); altered["comments"] = "changed"; await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", altered));
        });
        await Test("student projections exclude private grades/reviews until final approval; final outcome is immutable and filename-only", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await AcademicallyApproved(w); var student = await w.ReadAsync("student", "student", "P"); Assert(student["outcome"].Type == JTokenType.Null && !student.ToString().Contains("Private academic")); Assert((string)db.Values["placements/P"]["status"] == "confirmed");
            await w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)); var after = await w.ReadAsync("student", "student", "P"); Assert((bool)after["completed"] && (decimal)after["outcome"]["finalGrade"] == 91.5m && (string)after["outcome"]["completionReport"]["uploadStatus"] == "nameOnly"); Assert(!after.ToString().Contains("Private") && !after.ToString().Contains("actorUid") && !after.ToString().Contains("Company report"));
            int writes = db.Writes; await w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)); Assert(writes == db.Writes && db.Values.Keys.Count(x => x.StartsWith("notifications/")) == 1); await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", FDraft("reopen")));
            var reloaded = await FinalService(db).ReadAsync("student", "student", "P"); Assert((bool)reloaded["completed"]);
        });
        await Test("interrupted finalization is not completed and resumes exactly once without duplicate grade or notification", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await AcademicallyApproved(w); db.FailBeforeAt = "notifications/ST/final-training-P";
            await Reject<System.IO.IOException>(() => w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)));
            Assert(!(bool)(await w.ReadAsync("student", "student", "P"))["completed"]); await w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)); Assert((bool)(await w.ReadAsync("student", "student", "P"))["completed"]); Assert(db.Values.Keys.Count(k => k.StartsWith("finalTrainingOutcomes/")) == 1);
        });
        await Test("hours and relationship changes block academic/final approvals; no fabricated attendance bindings", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await Submitted(w); db.Seed("uimsAttendance/attendance/AT", new JObject()); await Reject<FirebaseException>(() => w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", FInput("a", rev)));
            db = FinalSeed(); w = FinalService(db); rev = await AcademicallyApproved(w); db.Seed("uimsAttendance/attendance/AT", new JObject()); await Reject<FirebaseException>(() => w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)));
            db = FinalSeed(); w = FinalService(db); rev = await AcademicallyApproved(w); db.Values["users/student"]["academicSupervisorId"] = "otherAcademic"; await Reject<FirebaseException>(() => w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)));
            db = FinalSeed(); db.Values["placements/P"].Remove("trainingAssignmentId"); db.Values["uimsAttendance/trainingAssignments/AT"].Remove("placementId"); var blocked = await FinalService(db).ReadAsync("company", "companySupervisor", "P"); Assert(!(bool)blocked["eligible"] && ((string)blocked["eligibilityBlocker"]).Contains("Missing"));
        });
        await Test("concurrent conflicting academic reviews produce one decision and never overwrite the winner", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await Submitted(w);
            var back = FInput("concurrent-return", rev); back["feedback"] = "Revise";
            var actions = new Func<Task>[] { () => w.ActAsync("academic", "academicSupervisor", "P", "academicApprove", FInput("concurrent-approve", rev)), () => FinalService(db).ActAsync("academic", "academicSupervisor", "P", "companyRevision", back) };
            int[] results = await Task.WhenAll(actions.Select(action => Task.Run(async () => { try { await action(); return 1; } catch (FirebaseException) { return 0; } })));
            Assert(results.Sum() == 1 && ((JObject)db.Values["finalEvaluationWorkflows/P"]["academicReviews"]).Count == 1);
        });
        await Test("stale company draft versions cannot overwrite newer saved report content", async () => {
            var db = FinalSeed(); var w = FinalService(db); await w.ActAsync("company", "companySupervisor", "P", "save", FDraft());
            var stale = FDraft("stale-save"); stale["comments"] = "Outdated browser tab"; await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", stale));
            var next = FDraft("new-save"); next["expectedDraftVersion"] = "save"; next["comments"] = "Latest draft"; await w.ActAsync("company", "companySupervisor", "P", "save", next);
            await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "submit", FInput("stale-submit"))); Assert((string)db.Values["finalEvaluationWorkflows/P"]["draft"]["comments"] == "Latest draft");
        });
        await Test("lost final placement acknowledgment reconciles; missing completion materializations hide all student success", async () => {
            var db = FinalSeed(); var w = FinalService(db); string rev = await AcademicallyApproved(w); db.LoseResponseAt = "placements/P";
            await Reject<System.IO.IOException>(() => w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)));
            Assert(!(bool)(await w.ReadAsync("student", "student", "P"))["completed"]); await w.ActAsync("admin", "admin", "P", "finalApprove", FApprove(rev)); Assert((bool)(await w.ReadAsync("student", "student", "P"))["completed"]);
            db.Values.Remove("finalTrainingOutcomes/P"); var hidden = await w.ReadAsync("student", "student", "P"); Assert(!(bool)hidden["completed"] && hidden["outcome"].Type == JTokenType.Null && hidden["notification"].Type == JTokenType.Null);
        });
        await Test("filename validation rejects paths and grade bounds use existing 0 to 100 range", async () => { var db = FinalSeed(); var w = FinalService(db); var bad = FDraft(); bad["fileName"] = "C:\\private\\report.pdf"; await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", bad)); bad = FDraft(); bad["percentage"] = 101; await Reject<FirebaseException>(() => w.ActAsync("company", "companySupervisor", "P", "save", bad)); Assert(db.Writes == 0); });
    }
}
