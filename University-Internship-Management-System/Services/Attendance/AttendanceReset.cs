using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UIMS.Attendance
{
    public partial class AttendanceService
    {
        // Resets are local-only unless AttendanceResetAllowRemote=true lets a deployed server offer them.
        // Identity, own-record, today-only and archive checks apply either way.
        public static bool IsLocalResetRequest(HttpContext context)
        {
            if (context == null || context.Request.HttpMethod != "POST") return false;
            if (System.Configuration.ConfigurationManager.AppSettings["AttendanceResetAllowRemote"] == "true") return true;
            if (!context.Request.IsLocal) return false;
            string host = context.Request.Url.Host;
            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "::1" || host == "[::1]";
        }

        public bool CanOfferStudentReset { get { return configuration.StudentResetEnabled && configuration.AuthenticatedWritesEnabled && IsLocalResetRequest(HttpContext.Current); } }

        private void RequireResetActor(string studentId, string uid)
        {
            var actor = AttendanceConfiguration.WriteRequest.Value;
            if (configuration.AuthenticatedWritesEnabled && IsLocalResetRequest(HttpContext.Current) &&
                actor != null && actor.Role == "company" && actor.Uid == uid && !string.IsNullOrWhiteSpace(uid) &&
                !string.IsNullOrWhiteSpace(actor.Id) && (actor.Action == "supervisorResetPreview" || actor.Action == "supervisorResetToday")) return;
            if (!CanOfferStudentReset || actor == null || actor.Role != "student" || actor.Id != studentId || actor.Uid != uid ||
                (actor.Action != "resetPreview" && actor.Action != "resetToday") || string.IsNullOrWhiteSpace(uid))
                throw new UIMS.Services.FirebaseException(403, "Attendance reset is available only for your own account in enabled local testing.");
        }

        private static string ResetVersion(JObject row)
        {
            using (var hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(row.ToString(Formatting.None))));
        }

        private async Task<AttendanceContext> ResetContext(string studentId, string uid)
        {
            RequireResetActor(studentId, uid);
            var actor = AttendanceConfiguration.WriteRequest.Value;
            bool supervisor = actor.Role == "company";
            await Identity(supervisor ? "company" : "student", supervisor ? actor.Id : studentId, uid, false);
            await CheckEnabled();
            var c = await Context(studentId, supervisor ? actor.Id : null);
            CheckFlexibleAttendanceDate(c);
            CheckRecord(c);
            // Legacy requests live in a separate conditional record. Never silently reset half of a two-record workflow.
            if (c.Request != null && c.Request.HasValues)
                throw new UIMS.Services.FirebaseException(409, "An early check-out request is linked to today. Ask your supervisor to reconcile it before resetting.");
            return c;
        }

        private static void ValidateResetRecord(AttendanceContext c, JObject row)
        {
            if (Number(row, "checkInAt") <= 0) throw new UIMS.Services.FirebaseException(409, "There is no recorded attendance to reset today. Refresh attendance.");
            if (Text(row, "studentId") != Text(c.Assignment, "studentId") || Text(row, "assignmentId") != c.AssignmentId ||
                Text(row, "attendanceDate") != c.Date || Text(row, "companyId") != Text(c.Assignment, "companyId") ||
                Text(row, "companySupervisorId") != Text(c.Assignment, "companySupervisorId"))
                throw new UIMS.Services.FirebaseException(409, "Today's record does not match your current assignment. No data was changed.");
            if (row["resetAttempts"] != null && !(row["resetAttempts"] is JObject))
                throw new UIMS.Services.FirebaseException(409, "The attendance archive is inconsistent. No data was changed.");
            if ((row["resetAttempts"] as JObject)?.Count >= 50)
                throw new UIMS.Services.FirebaseException(409, "The local test reset limit has been reached. Existing attempts were preserved.");
        }

        public async Task<JObject> PreviewTodayReset(string studentId, string uid)
        {
            var c = await ResetContext(studentId, uid);
            var snapshot = await database.Read("attendance/" + c.AssignmentId + "/" + c.Date);
            ValidateResetRecord(c, snapshot.Data);
            long approved = ApprovedTrainingMinutes.Calculate(new JObject { [c.Date] = snapshot.Data.DeepClone() }, c.AssignmentId,
                studentId, Text(c.Assignment, "companyId"), Now());
            return new JObject { ["date"] = c.Date, ["expectedVersion"] = ResetVersion(snapshot.Data), ["approvedMinutesRemoved"] = approved };
        }

        public async Task ResetToday(string studentId, string uid, string expectedVersion, string expectedDate)
        {
            var c = await ResetContext(studentId, uid);
            if (expectedDate != c.Date || string.IsNullOrWhiteSpace(expectedVersion))
                throw new UIMS.Services.FirebaseException(409, "The reset confirmation expired. Refresh attendance and try again.");
            string path = "attendance/" + c.AssignmentId + "/" + c.Date;
            var snapshot = await database.Read(path);
            ValidateResetRecord(c, snapshot.Data);
            if (ResetVersion(snapshot.Data) != expectedVersion)
                throw new UIMS.Services.FirebaseException(409, "Attendance changed after confirmation. Refresh and review it again.");
            var row = (JObject)snapshot.Data.DeepClone();
            var previous = (JObject)row.DeepClone();
            previous.Remove("resetAttempts");
            var archives = row["resetAttempts"] as JObject ?? new JObject();
            long now = Now();
            archives["reset_" + Guid.NewGuid().ToString("N")] = new JObject {
                ["resetAt"] = now, ["resetByUid"] = uid, ["previousRecord"] = previous
            };
            // Keep exact prior record (including consumed QR state) in the server-side archive, and clear only this live attempt.
            foreach (string field in new[] { "recordId", "checkInAt", "checkOutAt", "checkInVerifiedById", "checkOutVerifiedById",
                "hoursApprovedAt", "hoursApprovedById", "approvedMinutes", "lateMinutes", "isLate", "isEarlyDeparture", "qrState" }) row.Remove(field);
            row["resetAttempts"] = archives;
            row["status"] = "notCheckedIn";
            row["hoursApprovalStatus"] = "pending";
            row["workedMinutes"] = 0;
            row["isTestData"] = Flag(c.Student, "isTestData");
            row["updatedAt"] = now;
            row["lastResetAt"] = now;
            row["lastResetByUid"] = uid;
            // Report snapshots and files are intentionally never written by reset. Their live hours views may reflect the new ledger total.
            // Never retry over a newer scan/approval or reset an old day after midnight.
            if (Today() != c.Date)
                throw new UIMS.Services.FirebaseException(409, "The attendance date changed. Attendance was not reset.");
            if (!await Save(path, row, snapshot.Etag))
                throw new UIMS.Services.FirebaseException(409, "Attendance changed during reset. Refresh to check the saved state.");
        }
    }
}
