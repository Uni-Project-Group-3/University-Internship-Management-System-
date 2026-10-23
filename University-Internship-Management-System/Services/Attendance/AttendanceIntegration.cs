using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;

namespace UIMS.Attendance
{
    // Business setup errors can be isolated in a roster. Transport, authorization and
    // cancellation failures must propagate instead of looking like an empty roster.
    public sealed class AttendanceSetupException : InvalidOperationException
    {
        public AttendanceSetupException(string message) : base(message) { }
    }

    internal sealed class AttendanceProvisioningWrite
    {
        internal string Path, Uid;
        internal JToken Value;
        internal static readonly System.Threading.AsyncLocal<AttendanceProvisioningWrite> Current = new System.Threading.AsyncLocal<AttendanceProvisioningWrite>();
        internal static void Validate(string path, object value, string etag)
        {
            var scope = Current.Value;
            var parts = path.Split('/');
            var context = System.Web.HttpContext.Current;
            if (scope == null || scope.Path != path || !JToken.DeepEquals(scope.Value, JToken.FromObject(value)) ||
                parts.Length != 3 || parts[0] != "uimsAttendance" ||
                (parts[1] != "students" && parts[1] != "trainingAssignments" && parts[1] != "provisioningClaims") ||
                string.IsNullOrWhiteSpace(etag) || etag == "*" || context == null || context.Request.HttpMethod != "POST" ||
                context.User?.Identity?.IsAuthenticated != true || context.User.Identity.Name != scope.Uid || !AttendanceDeployment.AllowedWriteTransport(context.Request))
                throw new FirebaseException(403, "Attendance provisioning requires its validated, conditional confirmation write scope.");
            new AttendanceConfiguration().EnsureWritesAllowed();
        }
    }

    public static class AttendanceDeployment
    {
        public static bool AllowedWriteTransport(bool local, bool secure, string host, string allowedHosts)
        {
            if (local && (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "[::1]" || host == "::1")) return true;
            return secure && !string.IsNullOrWhiteSpace(host) && (allowedHosts ?? "").Split(';')
                .Any(h => string.Equals(h.Trim(), host, StringComparison.OrdinalIgnoreCase));
        }
        public static bool AllowedWriteTransport(System.Web.HttpRequest request)
        {
            // Never trust arbitrary forwarded headers. TLS must terminate in IIS or a
            // separately configured trusted proxy that presents HTTPS to ASP.NET.
            return request != null && AllowedWriteTransport(request.IsLocal, request.IsSecureConnection, request.Url.Host,
                System.Configuration.ConfigurationManager.AppSettings["AttendanceAllowedHttpsHosts"]);
        }
    }

    public static class AttendanceAcademicSupervisor
    {
        public static async Task<string> Resolve(string studentId, string uid,
            Func<string, Task<JToken>> read, Func<string, string, string, Task<JToken>> query)
        {
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var identities = new System.Collections.Generic.HashSet<string>(new[] { studentId }, StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(uid)) identities.Add(uid);
            var profiles = await query("users", "studentId", studentId).ConfigureAwait(false) as JObject;
            var studentProfiles = (profiles ?? new JObject()).Properties().Where(p => p.Value is JObject && FirebaseSignInService.CanonicalRole((string)p.Value["role"]) == "student").ToList();
            if (studentProfiles.Count(p => p.Value.Value<bool?>("isActive") != false) > 1)
                throw new AttendanceSetupException("Multiple active user identities share this student ID. Coordinator reconciliation is required.");
            foreach (var profile in studentProfiles)
            {
                identities.Add(profile.Name); // users keys are the Firebase UIDs used by Login.
                string assigned = (string)profile.Value["academicSupervisorId"];
                if (!string.IsNullOrWhiteSpace(assigned)) ids.Add(assigned);
            }
            foreach (string source in new[] { "supervisorAssignments", "assignments" })
                foreach (string identity in identities)
                {
                    var rows = await query(source, "studentId", identity).ConfigureAwait(false) as JObject;
                    if (rows == null) continue;
                    foreach (var row in rows.Properties().Select(p => p.Value as JObject).Where(x => x != null && x.Value<bool?>("active") != false))
                        if (!string.IsNullOrWhiteSpace((string)row["academicSupervisorId"])) ids.Add((string)row["academicSupervisorId"]);
                }
            if (ids.Count != 1) throw new AttendanceSetupException("The current academic supervisor is missing or conflicting. Ask the coordinator to reconcile supervisorAssignments and the student profile.");
            string id = ids.Single(); AttendanceFirebase.CheckKey(id);
            var actor = await read("users/" + id).ConfigureAwait(false) as JObject;
            if (actor == null || FirebaseSignInService.CanonicalRole((string)actor["role"]) != "academicSupervisor" ||
                actor.Value<bool?>("isActive") != true || actor.Value<bool?>("isEligible") == false)
                throw new AttendanceSetupException("The assigned academic supervisor is missing, inactive, or ineligible.");
            return id;
        }
    }

    /// <summary>Durable, retryable placement provisioning; never writes attendance or hours.</summary>
    public sealed class AttendanceProvisioner
    {
        private readonly IApplicationDatabase db;
        private const string Root = "uimsAttendance/";
        public AttendanceProvisioner(IApplicationDatabase database) { db = database ?? throw new ArgumentNullException(nameof(database)); }
        private static string Text(JToken o, string f) { return AttendanceService.Text(o, f); }
        private static void Require(bool condition, string message) { if (!condition) throw new AttendanceSetupException(message); }
        private static bool Present(JToken v) { return v != null && v.Type != JTokenType.Null && (!(v is JValue) || ((JValue)v).Value != null); }

        public async Task<string> Ensure(string placementId, string verifiedUid, JObject proposedPlacement = null, bool validateOnly = false)
        {
            AttendanceFirebase.CheckKey(placementId);
            var p = proposedPlacement ?? await db.ReadAsync("placements/" + placementId).ConfigureAwait(false) as JObject;
            Require(p != null && (Text(p, "status") == "confirmed" || Text(p, "status") == "active") && !AttendanceService.Flag(p, "isTestData"), "A confirmed, non-test placement is required.");
            string student = Text(p, "studentId"), company = Text(p, "companyId"), supervisor = Text(p, "companySupervisorId");
            foreach (string id in new[] { student, company, supervisor, verifiedUid }) AttendanceFirebase.CheckKey(id);
            Require(Text(p, "studentUid") == verifiedUid && await db.IsActiveAuthenticationUidAsync(verifiedUid).ConfigureAwait(false), "The placement UID must match an enabled verified Firebase account.");
            var profile = await db.ReadAsync("users/" + verifiedUid).ConfigureAwait(false) as JObject;
            Require(profile != null && Text(profile, "studentId") == student && FirebaseSignInService.CanonicalRole(Text(profile, "role")) == "student" &&
                profile.Value<bool?>("isActive") == true && !AttendanceService.Flag(profile, "mustChangePassword") &&
                (Text(profile, "accountSetupStatus") == "" || Text(profile, "accountSetupStatus") == "complete"), "Complete the active student account setup before attendance provisioning.");
            string academic = await AttendanceAcademicSupervisor.Resolve(student, verifiedUid, db.ReadAsync, db.ReadEqualAsync).ConfigureAwait(false);
            Require(Text(p, "academicSupervisorId") == academic, "The placement academic supervisor conflicts with the current coordinator assignment.");
            var companyActor = await db.ReadAsync(Root + "companies/" + company).ConfigureAwait(false) as JObject;
            var supervisorActor = await db.ReadAsync(Root + "companySupervisors/" + supervisor).ConfigureAwait(false) as JObject;
            Require(Text(companyActor, "status") == "active" && Text(supervisorActor, "status") == "active" && Text(supervisorActor, "companyId") == company,
                "The attendance company and supervisor must already exist, be active, and match the placement.");
            string supervisorUid = Text(supervisorActor, "firebaseUid");
            Require(supervisorUid != "" && await db.IsActiveAuthenticationUidAsync(supervisorUid).ConfigureAwait(false), "The company supervisor needs an enabled Firebase Authentication account.");
            var supervisorProfile = await db.ReadAsync("users/" + supervisorUid).ConfigureAwait(false) as JObject;
            Require(FirebaseSignInService.CanonicalRole(Text(supervisorProfile, "role")) == "companySupervisor" && supervisorProfile.Value<bool?>("isActive") == true &&
                Text(supervisorProfile, "companyId") == company && (Text(supervisorProfile, "companySupervisorId") == supervisor || Text(supervisorProfile, "supervisorId") == supervisor || Text(supervisorProfile, "id") == supervisor), "The company supervisor profile and attendance mapping disagree.");
            var placements = await db.ReadEqualAsync("placements", "studentId", student).ConfigureAwait(false) as JObject ?? new JObject();
            Require(!placements.Properties().Any(x => x.Name != placementId && (Text(x.Value, "status") == "active" || Text(x.Value, "status") == "confirmed")), "Multiple confirmed placements require coordinator review.");
            var all = await db.ReadAsync(Root + "trainingAssignments").ConfigureAwait(false) as JObject ?? new JObject();
            all = new JObject(all.Properties().Where(x => Text(x.Value, "studentId") == student).Select(x => new JProperty(x.Name, x.Value.DeepClone())));
            var linked = all.Properties().Where(x => Text(x.Value, "placementId") == placementId).ToList();
            var compatible = all.Properties().Where(x => Text(x.Value, "placementId") == "" && Text(x.Value, "status") == "active" && Text(x.Value, "companyId") == company && Text(x.Value, "companySupervisorId") == supervisor).ToList();
            Require(linked.Count <= 1 && (linked.Count == 1 || compatible.Count <= 1), "Attendance assignments are ambiguous; historical IDs were preserved.");
            string aid = linked.Count == 1 ? linked[0].Name : compatible.Count == 1 ? compatible[0].Name : placementId;
            AttendanceFirebase.CheckKey(aid);
            Require(!all.Properties().Any(x => x.Name != aid && Text(x.Value, "status") == "active"), "Another active attendance assignment requires review.");
            var existing = await db.ReadAsync(Root + "trainingAssignments/" + aid).ConfigureAwait(false) as JObject;
            var expected = new JObject { ["assignmentId"] = aid, ["placementId"] = placementId, ["studentId"] = student, ["companyId"] = company,
                ["companySupervisorId"] = supervisor, ["academicSupervisorId"] = academic, ["status"] = "active", ["isTestData"] = false };
            foreach (string field in new[] { "startDate", "endDate", "requiredHours" }) expected[field] = Present(p[field]) && (p[field].Type != JTokenType.String || !string.IsNullOrWhiteSpace((string)p[field])) ? p[field].DeepClone() : existing?[field]?.DeepClone();
            DateTime start, end;
            Require(DateTime.TryParseExact(Text(expected, "startDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start), "The placement needs an approved training start date.");
            if (Text(expected, "endDate") == "" && AttendanceService.Number(p, "durationWeeks") > 0 && AttendanceService.Number(p, "durationWeeks") <= 260)
                expected["endDate"] = start.AddDays(AttendanceService.Number(p, "durationWeeks") * 7 - 1).ToString("yyyy-MM-dd");
            Require(DateTime.TryParseExact(Text(expected, "endDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end) && end >= start && AttendanceService.Number(expected, "requiredHours") > 0, "The placement needs approved end date/duration and required hours.");
            ValidateMerge(existing, expected);
            var actor = await db.ReadAsync(Root + "students/" + student).ConfigureAwait(false) as JObject;
            var actorExpected = new JObject { ["firebaseUid"] = verifiedUid, ["status"] = "active", ["studentNumber"] = student, ["activeAssignmentId"] = aid,
                ["name"] = Text(actor, "name") != "" ? Text(actor, "name") : Text(profile, "fullName") != "" ? Text(profile, "fullName") : student };
            ValidateMerge(actor, actorExpected);
            var calendar = await db.ReadAsync(Root + "schedules/" + aid).ConfigureAwait(false) as JObject;
            Require(calendar != null && calendar.HasValues, "Publish an approved calendar at uimsAttendance/schedules/" + aid + " before completing attendance setup. No dates were invented.");
            foreach (var day in calendar.Properties()) ValidateSchedule(aid, day.Name, day.Value as JObject, start, end);
            var claimExpected = new JObject { ["placementId"] = placementId, ["assignmentId"] = aid, ["studentId"] = student, ["firebaseUid"] = verifiedUid };
            var existingClaim = await db.ReadAsync(Root + "provisioningClaims/" + student).ConfigureAwait(false) as JObject;
            ValidateMerge(existingClaim, claimExpected);
            bool complete = expected.Properties().All(f => JToken.DeepEquals(existing?[f.Name], f.Value)) && actorExpected.Properties().All(f => JToken.DeepEquals(actor?[f.Name], f.Value)) && claimExpected.Properties().All(f => JToken.DeepEquals(existingClaim?[f.Name], f.Value));
            if (validateOnly || complete) return aid;
            Require(JToken.DeepEquals(p, await db.ReadAsync("placements/" + placementId).ConfigureAwait(false)), "The placement changed during provisioning. Retry to revalidate its bindings.");
            // A student-scoped immutable claim prevents two concurrent placements selecting different IDs.
            await MergeMissing(Root + "provisioningClaims/" + student, claimExpected, verifiedUid).ConfigureAwait(false);
            await MergeMissing(Root + "trainingAssignments/" + aid, expected, verifiedUid).ConfigureAwait(false);
            await MergeMissing(Root + "students/" + student, actorExpected, verifiedUid).ConfigureAwait(false);
            // Re-read the authoritative setup: retries recover partial writes without replacing history.
            ValidateMerge(await db.ReadAsync(Root + "trainingAssignments/" + aid).ConfigureAwait(false) as JObject, expected, true);
            ValidateMerge(await db.ReadAsync(Root + "students/" + student).ConfigureAwait(false) as JObject, actorExpected, true);
            return aid;
        }
        public static void ValidatePlacementBinding(JObject placement, JObject assignment, string uid)
        {
            Require(placement != null && (Text(placement, "status") == "confirmed" || Text(placement, "status") == "active") &&
                !AttendanceService.Flag(placement, "isTestData") && Text(placement, "studentUid") == uid,
                "The confirmed placement is unavailable, inactive, or has a conflicting UID.");
            foreach (string field in new[] { "studentId", "companyId", "companySupervisorId", "academicSupervisorId" })
                Require(Text(placement, field) != "" && Text(placement, field) == Text(assignment, field), "The placement and attendance assignment disagree at " + field + ". Coordinator reconciliation is required.");
        }

        public static void ValidateSchedule(string aid, string date, JObject row, DateTime start, DateTime end)
        {
            DateTime day;
            Require(row != null && DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day) && day >= start && day <= end &&
                Text(row, "assignmentId") == aid && Text(row, "attendanceDate") == date && Text(row, "timeZone") == "Asia/Amman" && AttendanceService.Number(row, "version") > 0 && row["isTrainingDay"]?.Type == JTokenType.Boolean, "An existing attendance calendar entry is invalid; it was preserved.");
            if (!AttendanceService.Flag(row, "isTrainingDay")) return;
            long a = AttendanceService.Number(row, "scheduledStartAt"), b = AttendanceService.Number(row, "scheduledEndAt");
            Require(a > 0 && b > a, "The approved calendar needs valid start/end timestamps.");
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
                Require(new[] { a, b }.All(t => TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(t), zone).ToString("yyyy-MM-dd") == date), "Calendar timestamps do not match the Jordan date.");
            }
            catch (ArgumentOutOfRangeException) { throw new AttendanceSetupException("Calendar timestamps are outside the supported range."); }
        }
        private static void ValidateMerge(JObject current, JObject expected, bool requireAll = false)
        {
            foreach (var field in expected.Properties())
                Require(!Present(current?[field.Name]) ? !requireAll : JToken.DeepEquals(current[field.Name], field.Value), "Attendance setup conflicts at field " + field.Name + ". Existing values were preserved.");
        }
        private async Task MergeMissing(string path, JObject expected, string verifiedUid)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var snapshot = await db.ReadVersionAsync(path).ConfigureAwait(false);
                ValidateMerge(snapshot.Value, expected);
                var next = snapshot.Value == null ? new JObject() : (JObject)snapshot.Value.DeepClone();
                foreach (var f in expected.Properties()) if (!Present(next[f.Name])) next[f.Name] = f.Value.DeepClone();
                if (JToken.DeepEquals(snapshot.Value, next)) return;
                Require(!string.IsNullOrWhiteSpace(snapshot.ETag) && snapshot.ETag != "*", "A conditional Firebase version is required for provisioning.");
                var prior = AttendanceProvisioningWrite.Current.Value;
                AttendanceProvisioningWrite.Current.Value = new AttendanceProvisioningWrite { Path = path, Value = next.DeepClone(), Uid = verifiedUid };
                try { if (await db.CompareWriteAsync(path, next, snapshot.ETag).ConfigureAwait(false)) return; }
                finally { AttendanceProvisioningWrite.Current.Value = prior; }
            }
            throw new AttendanceSetupException("Attendance setup changed concurrently. Retry confirmation to finish safely.");
        }
    }
}
