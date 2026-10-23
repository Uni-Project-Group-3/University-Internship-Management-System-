using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using UIMS.Attendance;
using UIMS.Services;
using UIMS.Tasks;
using University_Internship_Management_System.Shared;

sealed class PlacementDb : IApplicationDatabase
{
    internal readonly JObject Root = new JObject(); readonly object gate = new object();
    internal int Revision, Writes, FailAfter = -1, Conflicts;
    internal bool AuthEnabled = true;
    internal JToken At(string path) { lock (gate) { JToken n = Root; foreach (string key in path.Split('/')) n = (n as JObject)?[key]; return n?.DeepClone(); } }
    internal void Put(string path, JToken value) { lock (gate) { var parts = path.Split('/'); JObject n = Root; foreach (string key in parts.Take(parts.Length - 1)) { if (!(n[key] is JObject)) n[key] = new JObject(); n = (JObject)n[key]; } n[parts.Last()] = value?.DeepClone(); Revision++; } }
    public Task<JToken> ReadAsync(string path) { return Task.FromResult(At(path)); }
    public Task<JToken> ReadEqualAsync(string path, string field, string value) { var source = At(path) as JObject ?? new JObject(); return Task.FromResult<JToken>(new JObject(source.Properties().Where(p => (string)p.Value[field] == value).Select(p => new JProperty(p.Name, p.Value.DeepClone())))); }
    string ETag(string path)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
            return "\"" + Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(At(path)?.ToString(Newtonsoft.Json.Formatting.None) ?? "null"))) + "\"";
    }
    public Task<StudentVersion> ReadVersionAsync(string path) { lock (gate) return Task.FromResult(new StudentVersion { Value = At(path) as JObject, ETag = ETag(path) }); }
    public async Task<bool> CompareWriteAsync(string path, object value, string etag)
    {
        await Task.Yield();
        lock (gate) {
            if (FailAfter == Writes) throw new HttpRequestException("Isolated interrupted write");
            if (Conflicts > 0) { Conflicts--; return false; }
            if (etag != ETag(path)) return false;
            Put(path, JToken.FromObject(value)); Writes++; return true;
        }
    }
    public Task<bool> IsActiveAuthenticationUidAsync(string uid) { return Task.FromResult(AuthEnabled && (uid == "uid-S" || uid == "uid-CS")); }
    internal PlacementDb()
    {
        string today = AttendanceService.Today();
        Put("placements/P", new JObject { ["studentId"] = "S", ["studentUid"] = "uid-S", ["companyId"] = "C", ["companySupervisorId"] = "CS", ["academicSupervisorId"] = "AC", ["status"] = "confirmed", ["startDate"] = today, ["endDate"] = today, ["requiredHours"] = 300, ["applicationId"] = "APP" });
        Put("applications/APP", new JObject { ["studentId"] = "S", ["status"] = "confirmed", ["placementId"] = "P" });
        Put("users/uid-S", new JObject { ["studentId"] = "S", ["role"] = "student", ["isActive"] = true, ["accountSetupStatus"] = "complete" });
        Put("users/uid-CS", new JObject { ["companyId"] = "C", ["companySupervisorId"] = "CS", ["role"] = "companySupervisor", ["isActive"] = true });
        Put("users/AC", new JObject { ["role"] = "academicSupervisor", ["isActive"] = true });
        Put("supervisorAssignments/S", new JObject { ["studentId"] = "S", ["academicSupervisorId"] = "AC", ["active"] = true });
        Put("uimsAttendance/companies/C", new JObject { ["status"] = "active" });
        Put("uimsAttendance/companySupervisors/CS", new JObject { ["companyId"] = "C", ["status"] = "active", ["firebaseUid"] = "uid-CS" });
        Put("uimsAttendance/schedules/P/" + today, new JObject { ["assignmentId"] = "P", ["attendanceDate"] = today, ["timeZone"] = "Asia/Amman", ["version"] = 7, ["isTrainingDay"] = true, ["scheduledStartAt"] = AttendanceService.Now() - 60000, ["scheduledEndAt"] = AttendanceService.Now() + 60000 });
        Put("uimsAttendance/attendance/HISTORY/2026-01-01", new JObject { ["workedMinutes"] = 480, ["hoursApprovalStatus"] = "approved" });
    }
}

static partial class Program
{
    static PlacementDb NewOfferDb()
    {
        var db = new PlacementDb(); string today = AttendanceService.Today();
        var calendar = (JObject)db.At("uimsAttendance/schedules/P/" + today);
        calendar["assignmentId"] = "PLC-APP";
        db.Put("placements", new JObject());
        db.Put("applications/APP", new JObject { ["studentId"] = "S", ["studentUid"] = "uid-S", ["status"] = "accepted", ["companyId"] = "C", ["companySupervisorId"] = "CS", ["opportunityId"] = "O" });
        db.Put("opportunities/O", new JObject { ["startDate"] = today, ["endDate"] = today, ["requiredHours"] = 300 });
        db.Put("uimsAttendance/schedules", new JObject { ["PLC-APP"] = new JObject { [today] = calendar } });
        return db;
    }
    static async Task IntegrationTests()
    {
        await Test("complete isolated placement-to-QR-to-approved-report cycle uses elapsed minutes", async () => {
            var db = NewOfferDb(); string placementId = await new ApplicationWorkflowService(db).ConfirmOfferAsync("uid-S", "APP");
            Assert(placementId == "PLC-APP" && (string)db.At("applications/APP/status") == "confirmed" && (string)db.At("uimsAttendance/students/S/activeAssignmentId") == placementId);
            var memory = new MemoryFirebase(); memory.Root.Merge(db.At("uimsAttendance"));
            memory.Put("settings", new JObject { ["enabled"] = true });
            var attendance = new AttendanceService(memory, () => Fixture.Key);
            var roster = await attendance.LoadSupervisorAttendance("CS", null, null); Assert(roster[0].Count == 1);
            await attendance.Identity("student", "S", "uid-S", false);
            await attendance.IssueStudentQrs("CS", new JArray("S"), "checkIn", false, "uid-CS");
            string checkIn = (string)(await attendance.ReadIssuedQr("S", "checkIn", false))["token"];
            await attendance.ScanIssuedQr("CS", checkIn, false);
            await Reject(() => attendance.ScanIssuedQr("CS", checkIn, false));
            Console.WriteLine("Isolated full-cycle test: waiting 61 seconds for one actual elapsed minute; no Firebase network.");
            await Task.Delay(61000);
            await attendance.IssueStudentQrs("CS", new JArray("S"), "checkOut", false, "uid-CS");
            string checkOut = (string)(await attendance.ReadIssuedQr("S", "checkOut", false))["token"];
            await attendance.ScanIssuedQr("CS", checkOut, false);
            Assert((await attendance.LoadStudentAttendance("S", null, null)).ApprovedMinutes == 0);
            await attendance.Review("CS", placementId, AttendanceService.Today(), "approveHours");
            long minutes = (await attendance.LoadStudentAttendance("S", null, null)).ApprovedMinutes;
            Assert(minutes == 1);
            db.Put("uimsAttendance", memory.Root);
            var progress = await ReportProgress(ReportService(db), new JObject { ["placementId"] = placementId, ["studentId"] = "S" });
            Assert((long)progress["approvedTrainingMinutes"] == minutes && (long)db.At("uimsAttendance/attendance/HISTORY/2026-01-01/workedMinutes") == 480);
        });
        await Test("accepted new placement without approved calendar fails before any writes", async () => {
            var db = NewOfferDb(); db.Put("uimsAttendance/schedules", new JObject());
            await Reject(() => new ApplicationWorkflowService(db).ConfirmOfferAsync("uid-S", "APP"));
            Assert(db.Writes == 0 && (string)db.At("applications/APP/status") == "accepted" && !((JObject)db.At("placements")).HasValues);
        });
        await Test("new placement confirmation recovers each interrupted provisioning stage without duplicates", async () => {
            for (int stage = 1; stage <= 4; stage++) {
                var db = NewOfferDb(); db.FailAfter = stage; var history = db.At("uimsAttendance/attendance");
                try { await new ApplicationWorkflowService(db).ConfirmOfferAsync("uid-S", "APP"); throw new Exception("Expected interrupted confirmation"); } catch (HttpRequestException) { }
                db.FailAfter = -1;
                Assert(await new ApplicationWorkflowService(db).ConfirmOfferAsync("uid-S", "APP") == "PLC-APP");
                Assert(((JObject)db.At("placements")).Count == 1 && ((JObject)db.At("uimsAttendance/trainingAssignments")).Count == 1 && JToken.DeepEquals(history, db.At("uimsAttendance/attendance")));
            }
        });
        await Test("complete legacy mappings still restore a missing durable provisioning claim", async () => {
            var db = new PlacementDb(); var provisioner = new AttendanceProvisioner(db);
            await provisioner.Ensure("P", "uid-S"); db.Put("uimsAttendance/provisioningClaims", new JObject());
            var assignment = db.At("uimsAttendance/trainingAssignments/P"); var actor = db.At("uimsAttendance/students/S"); var history = db.At("uimsAttendance/attendance");
            await provisioner.Ensure("P", "uid-S");
            Assert((string)db.At("uimsAttendance/provisioningClaims/S/placementId") == "P" && JToken.DeepEquals(assignment, db.At("uimsAttendance/trainingAssignments/P")) && JToken.DeepEquals(actor, db.At("uimsAttendance/students/S")) && JToken.DeepEquals(history, db.At("uimsAttendance/attendance")));
        });
        await Test("confirmed retry provisions durable mapping and preserves approved history", async () => {
            var db = new PlacementDb(); var old = db.At("uimsAttendance/attendance");
            Assert(await new ApplicationWorkflowService(db).ConfirmOfferAsync("uid-S", "APP") == "P");
            int writes = db.Writes; await new ApplicationWorkflowService(db).ConfirmOfferAsync("uid-S", "APP");
            Assert(db.Writes == writes && JToken.DeepEquals(old, db.At("uimsAttendance/attendance")) && (string)db.At("uimsAttendance/students/S/activeAssignmentId") == "P");
        });
        await Test("parallel confirmations select one durable assignment", async () => {
            var db = new PlacementDb(); await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => new AttendanceProvisioner(db).Ensure("P", "uid-S")));
            Assert(((JObject)db.At("uimsAttendance/trainingAssignments")).Count == 1 && (string)db.At("uimsAttendance/students/S/activeAssignmentId") == "P");
        });
        await Test("partial provisioning recovers on retry without replacing history", async () => {
            var db = new PlacementDb { FailAfter = 1 }; try { await new AttendanceProvisioner(db).Ensure("P", "uid-S"); throw new Exception("Expected interrupted write"); } catch (HttpRequestException) { }
            db.FailAfter = -1; await new AttendanceProvisioner(db).Ensure("P", "uid-S"); Assert((string)db.At("uimsAttendance/students/S/activeAssignmentId") == "P");
        });
        await Test("compatible legacy assignment ID is reused", async () => {
            var db = new PlacementDb(); var p = (JObject)db.At("placements/P"); p["assignmentId"] = "LEGACY"; p["placementId"] = "P"; p["status"] = "active"; p.Remove("studentUid"); p.Remove("applicationId");
            db.Put("uimsAttendance/trainingAssignments/LEGACY", p); var schedule = (JObject)db.At("uimsAttendance/schedules/P/" + AttendanceService.Today()); schedule["assignmentId"] = "LEGACY"; db.Put("uimsAttendance/schedules/LEGACY/" + AttendanceService.Today(), schedule);
            Assert(await new AttendanceProvisioner(db).Ensure("P", "uid-S") == "LEGACY" && db.At("uimsAttendance/trainingAssignments/P") == null);
        });
        foreach (string mode in new[] { "uid", "inactive", "academicConflict", "academicInactive", "company", "missingCalendar", "missingHours", "pointer", "assignmentConflict", "calendarZone" })
            await Test("provisioning fails before writes: " + mode, async () => {
                var db = new PlacementDb();
                if (mode == "uid") db.AuthEnabled = false;
                if (mode == "inactive") db.Put("users/uid-S/isActive", false);
                if (mode == "academicConflict") db.Put("assignments/conflict", new JObject { ["studentId"] = "S", ["academicSupervisorId"] = "OTHER", ["active"] = true });
                if (mode == "academicInactive") db.Put("users/AC/isActive", false);
                if (mode == "company") db.Put("uimsAttendance/companySupervisors/CS/companyId", "OTHER");
                if (mode == "missingCalendar") db.Put("uimsAttendance/schedules/P", new JObject());
                if (mode == "missingHours") db.Put("placements/P/requiredHours", 0);
                if (mode == "pointer") db.Put("uimsAttendance/students/S", new JObject { ["activeAssignmentId"] = "OTHER" });
                if (mode == "assignmentConflict") db.Put("uimsAttendance/trainingAssignments/P", new JObject { ["studentId"] = "OTHER" });
                if (mode == "calendarZone") db.Put("uimsAttendance/schedules/P/" + AttendanceService.Today() + "/timeZone", "UTC");
                await Reject(() => new AttendanceProvisioner(db).Ensure("P", "uid-S")); Assert(db.Writes == 0);
            });
        await Test("revoked or rebound placements cannot authorize attendance", () => {
            var db = new PlacementDb(); var placement = (JObject)db.At("placements/P"); var assignment = (JObject)placement.DeepClone();
            AttendanceProvisioner.ValidatePlacementBinding(placement, assignment, "uid-S");
            placement["status"] = "cancelled"; Reject(() => { AttendanceProvisioner.ValidatePlacementBinding(placement, assignment, "uid-S"); return Task.CompletedTask; }).GetAwaiter().GetResult();
            placement["status"] = "confirmed"; placement["companyId"] = "OTHER"; Reject(() => { AttendanceProvisioner.ValidatePlacementBinding(placement, assignment, "uid-S"); return Task.CompletedTask; }).GetAwaiter().GetResult();
            return Task.CompletedTask;
        });
        await Test("HTTPS transport requires exact configured host; forwarded headers not accepted", () => {
            Assert(AttendanceDeployment.AllowedWriteTransport(true, false, "localhost", ""));
            Assert(AttendanceDeployment.AllowedWriteTransport(false, true, "uims.example.edu", "uims.example.edu"));
            Assert(!AttendanceDeployment.AllowedWriteTransport(false, false, "uims.example.edu", "uims.example.edu"));
            Assert(!AttendanceDeployment.AllowedWriteTransport(false, true, "evil.example", "uims.example.edu"));
            Assert(!AttendanceDeployment.AllowedWriteTransport(true, false, "uims.example.edu", "uims.example.edu")); return Task.CompletedTask;
        });
        await Test("one invalid student is isolated; valid supervisor roster remains usable", async () => {
            var f = new Fixture(); f.Db.Put("trainingAssignments/BAD", new JObject { ["studentId"] = "bad", ["companyId"] = f.Company, ["companySupervisorId"] = f.Supervisor, ["status"] = "active" }); f.Db.FailedStudent = "bad";
            var groups = await f.Service.LoadSupervisorAttendance(f.Supervisor, null, null); Assert(groups[0].Count == 1 && groups[3].Count == 1);
        });
        await Test("shared transport outage is propagated, never hidden as invalid student", async () => {
            var f = new Fixture(); f.Db.FailedStudent = f.Student; f.Db.TransportFailure = true;
            try { await f.Service.LoadSupervisorAttendance(f.Supervisor, null, null); throw new Exception("Expected shared failure"); } catch (HttpRequestException) { }
        });
        await Test("weekly report roster isolates an academic setup failure", async () => {
            var db = new PlacementDb(); db.Put("placements/BAD", new JObject { ["studentId"] = "BAD", ["companyId"] = "C", ["companySupervisorId"] = "CS", ["academicSupervisorId"] = "MISSING", ["status"] = "confirmed" });
            var result = await ReportService(db).ListCompanyPlacementsAsync(CancellationToken.None);
            Assert(((JArray)result["placements"]).Count == 1 && ((JArray)result["setupWarnings"]).Count == 1);
        });
        await Test("academic resolver supports coordinator records keyed by Firebase UID", async () => {
            var db = new PlacementDb(); db.Put("supervisorAssignments/S/studentId", "uid-S");
            Assert(await AttendanceAcademicSupervisor.Resolve("S", null, db.ReadAsync, db.ReadEqualAsync) == "AC");
        });
        await Test("weekly Firebase authorization outage propagates instead of returning an empty list", async () => {
            var actor = TaskActor.FromVerifiedProfile("uid-CS", "companySupervisor", new JObject { ["role"] = "companySupervisor", ["isActive"] = true, ["companyId"] = "C", ["companySupervisorId"] = "CS" });
            var service = new WeeklyReportService(k => k == "WeeklyReportFileProvider" ? "FirebaseStorage" : k == "FirebaseDatabaseUrl" ? "https://offline-test-default-rtdb.firebaseio.com" : "true", new HttpClient(new DeniedReportHandler()), actor, ct => Task.FromResult("offline"));
            try { await service.ListCompanyPlacementsAsync(CancellationToken.None); throw new Exception("Expected permission outage"); } catch (FirebaseException error) { Assert(error.StatusCode == 503); }
        });
        await Test("weekly report uses historical placement instead of current student pointer", async () => {
            var db = new PlacementDb(); await new AttendanceProvisioner(db).Ensure("P", "uid-S");
            db.Put("uimsAttendance/students/S/activeAssignmentId", "NEW"); db.Put("uimsAttendance/trainingAssignments/P/status", "completed");
            string day = AttendanceService.Today(); long end = AttendanceService.Now() - 1000;
            var row = new JObject { ["assignmentId"] = "P", ["studentId"] = "S", ["companyId"] = "C", ["companySupervisorId"] = "CS", ["attendanceDate"] = day,
                ["status"] = "completed", ["hoursApprovalStatus"] = "approved", ["hoursApprovedById"] = "CS", ["checkInAt"] = end - 120000, ["checkOutAt"] = end, ["workedMinutes"] = 2 };
            db.Put("uimsAttendance/attendance/P/" + day, row);
            var service = ReportService(db); var report = new JObject { ["placementId"] = "P", ["studentId"] = "S" };
            Assert((long)(await ReportProgress(service, report))["approvedTrainingMinutes"] == 2);
            row["hoursApprovedById"] = "OTHER"; db.Put("uimsAttendance/attendance/P/" + day, row);
            Assert((long)(await ReportProgress(service, report))["approvedTrainingMinutes"] == 0);
            db.Put("uimsAttendance/trainingAssignments/P/placementId", "OTHER"); await Reject(() => ReportProgress(service, report));
        });
    }
    static WeeklyReportService ReportService(PlacementDb db)
    {
        var actor = TaskActor.FromVerifiedProfile("uid-CS", "companySupervisor", new JObject { ["role"] = "companySupervisor", ["isActive"] = true, ["companyId"] = "C", ["companySupervisorId"] = "CS" });
        return new WeeklyReportService(k => k == "WeeklyReportFileProvider" ? "FirebaseStorage" : k == "FirebaseDatabaseUrl" ? "https://offline-test-default-rtdb.firebaseio.com" : "true", new HttpClient(new ReportHandler(db)), actor, ct => Task.FromResult("offline-test-token"));
    }
    static Task<JObject> ReportProgress(WeeklyReportService service, JObject report) { return (Task<JObject>)typeof(WeeklyReportService).GetMethod("GetAcademicProgressAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(service, new object[] { report, CancellationToken.None }); }
    sealed class DeniedReportHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("{}") }); }
    }
    sealed class ReportHandler : HttpMessageHandler
    {
        readonly PlacementDb db; internal ReportHandler(PlacementDb database) { db = database; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method != HttpMethod.Get) throw new Exception("Report progress must be read only");
            string path = request.RequestUri.AbsolutePath.Trim('/'); path = path.Substring(0, path.Length - 5);
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query); JToken value;
            if (query["orderBy"] != null) value = await db.ReadEqualAsync(path, Newtonsoft.Json.JsonConvert.DeserializeObject<string>(query["orderBy"]), Newtonsoft.Json.JsonConvert.DeserializeObject<string>(query["equalTo"])); else value = await db.ReadAsync(path);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(value?.ToString() ?? "null") };
        }
    }
}
