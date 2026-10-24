using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using UIMS.Attendance;

internal static partial class Program
{
    private sealed class ResetWorker : System.Web.Hosting.SimpleWorkerRequest
    {
        private readonly bool remote;
        internal ResetWorker(bool remote = false) : base("/", System.IO.Path.GetTempPath(), "MyAttendance.aspx", "", new System.IO.StringWriter()) { this.remote = remote; }
        public override string GetHttpVerbName() => "POST";
        public override string GetRemoteAddress() => remote ? "192.0.2.25" : "127.0.0.1";
        public override string GetLocalAddress() => "127.0.0.1";
        public override string GetServerName() => "localhost";
    }
    private sealed class ResetDatabase : AttendanceFirebase
    {
        internal readonly Dictionary<string, JObject> Rows = new Dictionary<string, JObject>();
        internal int Writes;
        internal bool Conflict;
        public override Task<AttendanceSnapshot> Read(string path) => Task.FromResult(new AttendanceSnapshot {
            Data = Rows.ContainsKey(path) ? (JObject)Rows[path].DeepClone() : new JObject(), Etag = "offline-etag"
        });
        public override Task<bool> SaveIfUnchanged(string path, JObject value, string etag)
        {
            Assert(etag == "offline-etag");
            if (Conflict) return Task.FromResult(false);
            Writes++; Rows[path] = (JObject)value.DeepClone(); return Task.FromResult(true);
        }
    }
    private static async Task RunAttendanceResetTests()
    {
        var previous = HttpContext.Current;
        var field = typeof(AttendanceConfiguration).GetField("WriteRequest", BindingFlags.Static | BindingFlags.NonPublic);
        object slot = field.GetValue(null);
        var property = slot.GetType().GetProperty("Value"); object priorActor = property.GetValue(slot);
        void Actor(string action, string uid = "uid-S", string student = "S", string role = "student")
        {
            object actor = Activator.CreateInstance(property.PropertyType, true);
            foreach (var v in new[] { Tuple.Create("Action", action), Tuple.Create("Uid", uid), Tuple.Create("Id", student), Tuple.Create("Role", role) })
                property.PropertyType.GetField(v.Item1, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(actor, v.Item2);
            property.SetValue(slot, actor);
        }
        var settings = new Dictionary<string, string> { ["AttendanceStudentResetEnabled"] = "true", ["AttendanceProductionWritesEnabled"] = "true" };
        string day = AttendanceService.Today(), path = "attendance/A/" + day;
        var db = new ResetDatabase();
        db.Rows["settings"] = new JObject { ["enabled"] = true };
        db.Rows["students/S"] = new JObject { ["status"] = "active", ["firebaseUid"] = "uid-S", ["activeAssignmentId"] = "A" };
        db.Rows["trainingAssignments/A"] = new JObject { ["status"] = "active", ["studentId"] = "S", ["companyId"] = "C", ["companySupervisorId"] = "CS", ["placementId"] = "P", ["requiredHours"] = 300, ["startDate"] = day, ["endDate"] = day };
        db.Rows["companies/C"] = new JObject { ["status"] = "active" };
        db.Rows["companySupervisors/CS"] = new JObject { ["status"] = "active", ["companyId"] = "C" };
        db.Rows["schedules/A/" + day] = new JObject { ["assignmentId"] = "A", ["attendanceDate"] = day, ["timeZone"] = "Asia/Amman", ["version"] = 1, ["isTrainingDay"] = true,
            ["scheduledStartAt"] = DateTimeOffset.Parse(day + "T09:00:00+03:00").ToUnixTimeMilliseconds(), ["scheduledEndAt"] = DateTimeOffset.Parse(day + "T17:00:00+03:00").ToUnixTimeMilliseconds() };
        long end = AttendanceService.Now() - 1000;
        var original = new JObject { ["studentId"] = "S", ["assignmentId"] = "A", ["companyId"] = "C", ["companySupervisorId"] = "CS", ["attendanceDate"] = day,
            ["isTestData"] = false, ["status"] = "completed", ["checkInAt"] = end - 120000, ["checkOutAt"] = end, ["workedMinutes"] = 2,
            ["hoursApprovalStatus"] = "approved", ["hoursApprovedById"] = "CS", ["qrState"] = new JObject { ["checkIn"] = new JObject { ["nonce"] = "offline-old-nonce" } }, ["historicalNote"] = "preserve" };
        db.Rows[path] = (JObject)original.DeepClone();
        var service = new AttendanceService(db, () => Enumerable.Repeat((byte)7, 32).ToArray(), new AttendanceConfiguration(k => settings.ContainsKey(k) ? settings[k] : null));
        try
        {
            HttpContext.Current = new HttpContext(new ResetWorker());
            Actor("resetPreview");
            await Test("reset preview shows actual approved minutes and never writes", async () => {
                var preview = await service.PreviewTodayReset("S", "uid-S"); Assert((long)preview["approvedMinutesRemoved"] == 2 && db.Writes == 0);
            });
            await Test("reset uses the same verified flexible date as QR without requiring fixed shift timestamps", async () => {
                string schedulePath = "schedules/A/" + day;
                var priorSchedule = (JObject)db.Rows[schedulePath].DeepClone();
                db.Rows[schedulePath].Remove("scheduledStartAt"); db.Rows[schedulePath].Remove("scheduledEndAt");
                db.Rows[schedulePath]["isTrainingDay"] = false;
                var preview = await service.PreviewTodayReset("S", "uid-S");
                Assert((long)preview["approvedMinutesRemoved"] == 2 && db.Writes == 0);
                db.Rows[schedulePath]["version"] = 0;
                await Reject<InvalidOperationException>(() => service.PreviewTodayReset("S", "uid-S"));
                db.Rows[schedulePath] = priorSchedule;
                string endDate = (string)db.Rows["trainingAssignments/A"]["endDate"];
                db.Rows["trainingAssignments/A"]["endDate"] = "2000-01-01";
                await Reject<InvalidOperationException>(() => service.PreviewTodayReset("S", "uid-S"));
                db.Rows["trainingAssignments/A"]["endDate"] = endDate;
                Assert(db.Writes == 0);
            });
            await Test("reset rejects disabled flag, remote callers, wrong role, foreign UID and foreign student", async () => {
                settings["AttendanceStudentResetEnabled"] = "false"; await Reject<UIMS.Services.FirebaseException>(() => service.PreviewTodayReset("S", "uid-S")); settings["AttendanceStudentResetEnabled"] = "true";
                HttpContext.Current = new HttpContext(new ResetWorker(true)); await Reject<UIMS.Services.FirebaseException>(() => service.PreviewTodayReset("S", "uid-S")); HttpContext.Current = new HttpContext(new ResetWorker());
                Actor("resetPreview", role: "company"); await Reject<UIMS.Services.FirebaseException>(() => service.PreviewTodayReset("S", "uid-S"));
                Actor("resetPreview", uid: "foreign"); await Reject<UIMS.Services.FirebaseException>(() => service.PreviewTodayReset("S", "uid-S"));
                Actor("resetPreview"); await Reject<UIMS.Services.FirebaseException>(() => service.PreviewTodayReset("OTHER", "uid-S")); Assert(db.Writes == 0);
            });
            await Test("reset blocks existing early requests without touching reports", async () => {
                db.Rows["earlyCheckoutRequests/A/" + day] = new JObject { ["status"] = "pending" }; await Reject<UIMS.Services.FirebaseException>(() => service.PreviewTodayReset("S", "uid-S")); db.Rows.Remove("earlyCheckoutRequests/A/" + day); Assert(db.Writes == 0);
            });
            await Test("reset rejects stale confirmation, date rollover and conditional-write conflict", async () => {
                var preview = await service.PreviewTodayReset("S", "uid-S"); Actor("resetToday");
                await Reject<UIMS.Services.FirebaseException>(() => service.ResetToday("S", "uid-S", (string)preview["expectedVersion"], "2000-01-01"));
                db.Rows[path]["updatedAt"] = 123; await Reject<UIMS.Services.FirebaseException>(() => service.ResetToday("S", "uid-S", (string)preview["expectedVersion"], day)); db.Rows[path].Remove("updatedAt");
                db.Conflict = true; await Reject<UIMS.Services.FirebaseException>(() => service.ResetToday("S", "uid-S", (string)preview["expectedVersion"], day)); db.Conflict = false; Assert(db.Writes == 0);
            });
            await Test("reset atomically preserves exact previous record, removes old QR, and never touches another day or schedule", async () => {
                Actor("resetPreview"); var preview = await service.PreviewTodayReset("S", "uid-S"); Actor("resetToday");
                db.Rows["reports-preservation-canary"] = new JObject { ["reportId"] = "offline-existing-report", ["approvedTrainingMinutes"] = 2405 };
                var report = db.Rows["reports-preservation-canary"].ToString();
                var schedule = db.Rows["schedules/A/" + day].ToString(); db.Rows["attendance/A/2000-01-01"] = new JObject { ["workedMinutes"] = 2405 };
                await service.ResetToday("S", "uid-S", (string)preview["expectedVersion"], day);
                var row = db.Rows[path]; Assert(db.Writes == 1 && row["checkInAt"] == null && row["checkOutAt"] == null && row["qrState"] == null && (long)row["workedMinutes"] == 0);
                var archived = (JObject)((JObject)row["resetAttempts"]).Properties().Single().Value["previousRecord"];
                Assert(JToken.DeepEquals(archived, original)); Assert(db.Rows["schedules/A/" + day].ToString() == schedule && (int)db.Rows["attendance/A/2000-01-01"]["workedMinutes"] == 2405);
                Assert(AttendanceService.ForDisplay(row)["resetAttempts"] == null && db.Rows["reports-preservation-canary"].ToString() == report);
                await Reject<UIMS.Services.FirebaseException>(() => service.ResetToday("S", "uid-S", (string)preview["expectedVersion"], day)); Assert(db.Writes == 1);
            });
            await Test("reset permits a fresh supervisor-scanned check-in and invalidates that token after the next reset", async () => {
                var qr = await service.CreateQr("S", "checkIn", false, "uid-S");
                await service.Scan("CS", (string)qr["token"], false);
                Assert(AttendanceService.Number(db.Rows[path], "checkInAt") > 0);
                Actor("resetPreview"); var preview = await service.PreviewTodayReset("S", "uid-S"); Actor("resetToday");
                await service.ResetToday("S", "uid-S", (string)preview["expectedVersion"], day);
                Assert(((JObject)db.Rows[path]["resetAttempts"]).Count == 2);
                int writes = db.Writes;
                await Reject<InvalidOperationException>(() => service.Scan("CS", (string)qr["token"], false));
                Assert(db.Writes == writes);
            });
            await Test("assigned supervisor reset clears today's attempt, archives it, rejects foreign supervision and allows a new check-in", async () => {
                db.Rows[path] = (JObject)original.DeepClone();
                db.Rows["companySupervisors/CS"]["firebaseUid"] = "uid-CS";
                db.Rows["companySupervisors/OTHER"] = new JObject { ["status"]="active", ["companyId"]="C", ["firebaseUid"]="uid-OTHER" };
                Actor("supervisorResetPreview", "uid-OTHER", "OTHER", "company");
                int before = db.Writes;
                await Reject<InvalidOperationException>(() => service.PreviewTodayReset("S", "uid-OTHER")); Assert(db.Writes == before);
                Actor("supervisorResetPreview", "uid-CS", "CS", "company");
                var preview = await service.PreviewTodayReset("S", "uid-CS"); Assert((long)preview["approvedMinutesRemoved"] == 2);
                Actor("supervisorResetToday", "uid-CS", "CS", "company");
                await service.ResetToday("S", "uid-CS", (string)preview["expectedVersion"], day);
                Assert(db.Rows[path]["checkInAt"] == null && db.Rows[path]["checkOutAt"] == null && db.Rows[path]["qrState"] == null);
                Assert((string)db.Rows[path]["lastResetByUid"] == "uid-CS" && ((JObject)db.Rows[path]["resetAttempts"]).Count == 1);
                var qr = await service.CreateQr("S", "checkIn", false, "uid-S"); await service.Scan("CS", (string)qr["token"], false);
                Assert(AttendanceService.Number(db.Rows[path], "checkInAt") > 0);
            });
        }
        finally { property.SetValue(slot, priorActor); HttpContext.Current = previous; }
    }
}
