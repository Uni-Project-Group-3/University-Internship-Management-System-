using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    // One CAS-protected aggregate per existing placement. No attendance, account or grade source is fabricated.
    public sealed class FinalReportWorkflow
    {
        public const long RequiredMinutes = 18000;
        private readonly IApplicationDatabase db;
        private readonly Func<DateTimeOffset> clock;
        public FinalReportWorkflow(IApplicationDatabase database, Func<DateTimeOffset> now = null) { db = database; clock = now ?? (() => DateTimeOffset.UtcNow); }
        private static string T(JToken o, string k) { return (string)o?[k] ?? ""; }
        private static JObject O(JToken value) { return value as JObject ?? new JObject(); }
        private static string Key(string value) { if (string.IsNullOrWhiteSpace(value) || value.Length > 180 || value.Any(c => char.IsControl(c) || ".#$[]/\\?%".Contains(c))) throw new FirebaseException(400, "Invalid report or placement identifier."); return value; }
        private string Now() { return clock().ToUniversalTime().ToString("o"); }
        private static string Hash(JToken input) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(input).ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant(); }
        private static JToken Canonical(JToken v) { if (v is JObject) return new JObject(((JObject)v).Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Canonical(p.Value)))); if (v is JArray) return new JArray(((JArray)v).Select(Canonical)); return v.DeepClone(); }
        private static string Text(JObject input, string name, int max, bool required = false) { string value = T(input, name).Trim(); if (value.Length > max || (required && value.Length == 0)) throw new FirebaseException(400, "Enter " + name + " (maximum " + max + " characters)."); return value; }
        private static decimal Grade(JObject input, string name) { decimal value; if (!decimal.TryParse(T(input, name), NumberStyles.Number, CultureInfo.InvariantCulture, out value) || value < 0 || value > 100) throw new FirebaseException(400, name + " must be between 0 and 100."); return value; }
        private static JObject Attachment(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 180 || name.IndexOfAny("/\\:\0".ToCharArray()) >= 0 || name.Any(char.IsControl)) throw new FirebaseException(400, "Enter a filename only, without a filesystem path.");
            return new JObject { ["fileName"] = name, ["storageProvider"] = null, ["storagePath"] = null, ["uploadStatus"] = "nameOnly" };
        }
        private sealed class Access { public string Uid, Role, Student, CompanySupervisor, Academic; public JObject Profile, Placement; }
        private async Task<Access> Authorize(string uid, string ticketRole, string placementId)
        {
            Key(uid); Key(placementId);
            if (!await db.IsActiveAuthenticationUidAsync(uid)) throw new FirebaseException(401, "An active verified account is required.");
            var profile = O(await db.ReadAsync("users/" + uid)); var login = FirebaseSignInService.ProfileResult(uid, profile);
            if (login.MustChangePassword) throw new FirebaseException(403, "Complete the required password change before reviewing final reports.");
            if (login.Role != FirebaseSignInService.CanonicalRole(ticketRole)) throw new FirebaseException(403, "The current account role changed.");
            var p = O(await db.ReadAsync("placements/" + placementId));
            string studentId = T(p, "studentId"), company = T(p, "companyId"), supervisor = T(p, "companySupervisorId"), academic = T(p, "academicSupervisorId");
            if (studentId.Length == 0 || company.Length == 0 || supervisor.Length == 0 || academic.Length == 0) throw new FirebaseException(409, "The placement requires existing student, company and both supervisor assignments.");
            string status = T(p, "status").ToLowerInvariant(); if (!new[] { "active", "confirmed", "completed" }.Contains(status)) throw new FirebaseException(409, "The placement is not confirmed or active.");
            var allProfiles = O(await db.ReadAsync("users"));
            var academicProfile = allProfiles.Properties().Where(entry => entry.Name == academic || T(entry.Value, "academicSupervisorId") == academic)
                .Select(entry => entry.Value).FirstOrDefault(value => FirebaseSignInService.CanonicalRole(T(value, "role")) == "academicSupervisor" && (bool?)value["isActive"] == true && (T(value, "accountSetupStatus").Length == 0 || T(value, "accountSetupStatus") == "complete"));
            if (academicProfile == null) throw new FirebaseException(409, "The current academic supervisor has no active completed account. The coordinator must resolve the existing assignment.");
            var students = new JObject(allProfiles.Properties().Where(entry => T(entry.Value, "studentId") == studentId).Select(entry => new JProperty(entry.Name, entry.Value.DeepClone())));
            if (!students.HasValues) { var student = O(await db.ReadAsync("users/" + Key(studentId))); if (student.HasValues) students[studentId] = student; }
            var studentProfile = students.Properties().FirstOrDefault(x => FirebaseSignInService.CanonicalRole(T(x.Value, "role")) == "student" && (bool?)x.Value["isActive"] == true);
            if (studentProfile == null) throw new FirebaseException(409, "The placement student has no active existing profile.");
            string profileAcademic = T(studentProfile.Value, "academicSupervisorId");
            var allAssignments = O(await db.ReadAsync("supervisorAssignments"));
            var assignments = new JObject(allAssignments.Properties().Where(entry => T(entry.Value, "studentId") == studentId).Select(entry => new JProperty(entry.Name, entry.Value.DeepClone())));
            var currentAssignments = assignments.Properties().Where(x => (bool?)x.Value["isActive"] != false && !new[] { "inactive", "ended", "reassigned" }.Contains(T(x.Value, "status").ToLowerInvariant())).Select(x => T(x.Value, "academicSupervisorId")).Where(x => x.Length > 0).Distinct().ToArray();
            if ((profileAcademic.Length > 0 && profileAcademic != academic) || currentAssignments.Any(x => x != academic)) throw new FirebaseException(409, "Current academic assignment and placement disagree. Reconcile the assignment before review.");
            string actorSupervisor = T(profile, "companySupervisorId"); if (actorSupervisor.Length == 0) actorSupervisor = T(profile, "supervisorId");
            if (actorSupervisor.Length == 0 && login.Role == "companySupervisor") { var map = O(await db.ReadAsync("companySupervisors")); actorSupervisor = map.Properties().Where(x => T(x.Value, "firebaseUid") == uid && T(x.Value, "status") == "active").Select(x => x.Name).SingleOrDefault() ?? ""; }
            string actorAcademic = T(profile, "academicSupervisorId");
            bool allowed = login.Role == "coordinator" || (login.Role == "companySupervisor" && actorSupervisor == supervisor && T(profile, "companyId") == company) ||
                (login.Role == "academicSupervisor" && (uid == academic || actorAcademic == academic)) || (login.Role == "student" && (uid == studentProfile.Name || T(profile, "studentId") == studentId));
            if (!allowed) throw new FirebaseException(403, "This placement is not assigned to your current account.");
            return new Access { Uid = uid, Role = login.Role, Placement = p, Profile = profile, Student = studentId, CompanySupervisor = supervisor, Academic = academic };
        }
        private async Task<long> Minutes(string placementId, Access a)
        {
            string assignmentId = T(a.Placement, "trainingAssignmentId"); if (assignmentId.Length == 0) assignmentId = T(a.Placement, "attendanceAssignmentId");
            if (assignmentId.Length == 0)
            {
                var allMappings = O(await db.ReadAsync("uimsAttendance/trainingAssignments"));
                var mappings = new JObject(allMappings.Properties().Where(p => T(p.Value, "placementId") == placementId).Select(p => new JProperty(p.Name, p.Value.DeepClone())));
                var ids = mappings.Properties().Select(x => x.Name).ToArray(); if (ids.Length == 1) assignmentId = ids[0];
            }
            if (assignmentId.Length == 0) throw new FirebaseException(409, "Missing verified placement-to-attendance assignment link. No hours were assumed.");
            var assignment = O(await db.ReadAsync("uimsAttendance/trainingAssignments/" + Key(assignmentId)));
            var placementLinks = O(await db.ReadAsync("placements"));
            if (placementLinks.Properties().Any(entry => entry.Name != placementId && (T(entry.Value, "trainingAssignmentId") == assignmentId || T(entry.Value, "attendanceAssignmentId") == assignmentId)))
                throw new FirebaseException(409, "The attendance assignment is linked to multiple placements. Reconcile the existing links before approval.");
            if (T(assignment, "studentId") != a.Student || T(assignment, "companyId") != T(a.Placement, "companyId") ||
                (T(assignment, "placementId").Length > 0 && T(assignment, "placementId") != placementId) || !new[] { "active", "completed" }.Contains(T(assignment, "status"))) throw new FirebaseException(409, "The attendance assignment does not belong to this placement/student/company.");
            var rows = O(await db.ReadAsync("uimsAttendance/attendance/" + assignmentId));
            return UIMS.Attendance.ApprovedTrainingMinutes.Calculate(rows, assignmentId, a.Student, T(a.Placement, "companyId"), clock().ToUnixTimeMilliseconds());
        }
        private async Task<long> Eligible(string id, Access a) { long minutes = await Minutes(id, a); if (minutes < RequiredMinutes) throw new FirebaseException(409, "At least 18,000 approved minutes (300 hours) are required. Verified: " + minutes + " minutes."); return minutes; }
        private static string Path(string id) { return "finalEvaluationWorkflows/" + Key(id); }
        private static void Revision(JObject record, JObject input) { if (T(record, "currentRevisionId") != T(input, "revisionId")) throw new FirebaseException(409, "This revision is outdated. Reload the persisted report."); }
        private static void State(JObject record, string role, params string[] allowed) { if (!allowed.Contains(T(record, "status"))) throw new FirebaseException(409, "This action is not allowed from the current report state."); }
        private void Audit(JObject r, Access a, string requestId, string action, string previous)
        {
            var history = O(r["audit"]); history[requestId] = new JObject { ["actorUid"] = a.Uid, ["role"] = a.Role, ["revisionId"] = r["currentRevisionId"], ["previousState"] = previous, ["nextState"] = r["status"], ["action"] = action, ["at"] = Now() }; r["audit"] = history;
        }
        public async Task<JObject> WeeklyReportsAsync(string uid, string role, string placementId)
        {
            var access = await Authorize(uid, role, Key(placementId));
            if (access.Role != "companySupervisor") throw new FirebaseException(403, "Only the assigned company supervisor can select weekly reports.");
            var records = O(await db.ReadAsync("weeklyReports")); var reports = new JArray();
            foreach (var entry in records.Properties())
            {
                var report = entry.Value;
                if (T(report, "placementId") != placementId || T(report, "studentId") != access.Student || T(report, "companyId") != T(access.Placement, "companyId") || T(report, "companySupervisorId") != access.CompanySupervisor) continue;
                string revisionId = T(report, "currentRevisionId"); var revision = report["revisions"]?[revisionId];
                if (revision == null || revisionId.Length == 0) continue;
                reports.Add(new JObject { ["reportId"] = entry.Name, ["revisionId"] = revisionId, ["weekNumber"] = report["weekNumber"], ["weekStart"] = report["weekStart"], ["weekEnd"] = report["weekEnd"], ["status"] = report["status"], ["companyComments"] = revision["companyComments"] ?? report["companyComments"], ["fileName"] = revision["fileName"], ["submittedAt"] = revision["submittedAt"] });
            }
            return new JObject { ["reports"] = reports };
        }
        private async Task<JArray> WeeklySnapshots(string uid, string role, string placementId, JToken requested)
        {
            if (requested == null) return null;
            var ids = requested as JArray;
            if (ids == null || ids.Count > 52 || ids.Any(x => x.Type != JTokenType.String) || ids.Select(x => (string)x).Distinct().Count() != ids.Count) throw new FirebaseException(400, "Select up to 52 distinct weekly reports.");
            var available = (JArray)(await WeeklyReportsAsync(uid, role, placementId))["reports"]; var result = new JArray();
            foreach (var id in ids) { Key((string)id); var report = available.FirstOrDefault(r => T(r, "reportId") == (string)id); if (report == null) throw new FirebaseException(403, "A selected weekly report does not belong to this authorized student and placement."); result.Add(report.DeepClone()); }
            return result;
        }
        public async Task<JObject> ActAsync(string uid, string role, string placementId, string action, JObject input)
        {
            input = (JObject)input.DeepClone(); Key(placementId); string requestId = Key(Text(input, "requestId", 100, true));
            string digest = Hash(new JObject { ["actorUid"] = uid, ["action"] = action, ["input"] = input });
            var access = await Authorize(uid, role, placementId);
            for (int retry = 0; retry < 3; retry++)
            {
                var node = await db.ReadVersionAsync(Path(placementId)); var record = node.Value == null ? null : (JObject)node.Value.DeepClone();
                var prior = record?["requests"]?[requestId];
                if (prior != null)
                {
                    if (T(prior, "digest") != digest) throw new FirebaseException(409, "This requestId was already used with different content.");
                    if (record["finalization"] != null && T(record, "status") != "completed") await Reconcile(uid, role, placementId);
                    return await ReadAsync(uid, role, placementId);
                }
                if (record != null && (T(record, "status") == "completed" || record["finalization"] != null)) throw new FirebaseException(409, "This report is completed or finalization is in progress. Completed reports are read-only.");
                if (record != null && (T(record, "studentId") != access.Student || T(record, "companyId") != T(access.Placement, "companyId"))) throw new FirebaseException(409, "Stored report identity no longer matches the placement.");
                if (record == null)
                {
                    if (action != "save" || access.Role != "companySupervisor") throw new FirebaseException(409, "The company must prepare the final report first.");
                    record = new JObject { ["id"] = placementId, ["placementId"] = placementId, ["studentId"] = access.Student, ["companyId"] = access.Placement["companyId"], ["status"] = "draft", ["requiredTrainingMinutes"] = RequiredMinutes, ["createdAt"] = Now() };
                }
                string previous = T(record, "status");
                record["companySupervisorId"] = access.CompanySupervisor; record["academicSupervisorId"] = access.Academic;
                if (action == "save" || action == "submit")
                {
                    if (access.Role != "companySupervisor") throw new FirebaseException(403, "Only the assigned company supervisor prepares this report.");
                    State(record, access.Role, "draft", "companyRevisionRequested");
                    if (T(input, "expectedDraftVersion") != T(record, "draftVersion")) throw new FirebaseException(409, "The saved draft changed in another request. Reload before saving or submitting.");
                    if (action == "save")
                    {
                        var snapshots = await WeeklySnapshots(uid, role, placementId, input["weeklyReportIds"]) ?? (record["draft"]?["weeklyReports"] as JArray)?.DeepClone() ?? new JArray();
                        record["draft"] = new JObject { ["percentage"] = Grade(input, "percentage"), ["comments"] = Text(input, "comments", 5000, true), ["companyResponse"] = Text(input, "companyResponse", 3000), ["attachment"] = Attachment(Text(input, "fileName", 180, true)), ["updatedAt"] = Now() };
                        record["draft"]["weeklyReports"] = snapshots;
                        record["draftVersion"] = requestId;
                    }
                    else
                    {
                        long minutes = await Eligible(placementId, access); var draft = O(record["draft"]);
                        if (!draft.HasValues) throw new FirebaseException(400, "Save the report before submitting.");
                        if (previous == "companyRevisionRequested" && string.IsNullOrWhiteSpace(T(draft, "companyResponse"))) throw new FirebaseException(400, "A company response is required for resubmission.");
                        string revisionId = "revision-" + requestId; var revisions = O(record["revisions"]); var revision = (JObject)draft.DeepClone(); revision["id"] = revisionId; revision["submittedAt"] = Now(); revision["submittedBy"] = uid; revision["approvedTrainingMinutes"] = minutes; revisions[revisionId] = revision;
                        record["revisions"] = revisions; record["currentRevisionId"] = revisionId; record["approvedTrainingMinutes"] = minutes; record["status"] = "pendingAcademicReview"; record.Remove("draft");
                    }
                }
                else if (action == "academicApprove" || action == "companyRevision")
                {
                    if (access.Role != "academicSupervisor") throw new FirebaseException(403, "Only the currently assigned academic supervisor may review this report.");
                    Revision(record, input); State(record, access.Role, "pendingAcademicReview", "academicRevisionRequested");
                    string feedback = Text(input, "feedback", 4000, action == "companyRevision" || previous == "academicRevisionRequested");
                    long minutes = action == "academicApprove" ? await Eligible(placementId, access) : 0;
                    var reviews = O(record["academicReviews"]); reviews[requestId] = new JObject { ["revisionId"] = record["currentRevisionId"], ["actorUid"] = uid, ["academicSupervisorId"] = access.Academic, ["action"] = action, ["feedback"] = feedback, ["at"] = Now(), ["approvedTrainingMinutes"] = minutes }; record["academicReviews"] = reviews;
                    record["status"] = action == "academicApprove" ? "pendingAdminReview" : "companyRevisionRequested";
                    if (action == "academicApprove") record["academicApprovalId"] = requestId;
                    else { record["draft"] = record["revisions"][T(record, "currentRevisionId")].DeepClone(); record["draft"]["companyResponse"] = ""; record["draftVersion"] = "return-" + requestId; record.Remove("academicApprovalId"); }
                }
                else if (action == "adminCorrection" || action == "finalApprove")
                {
                    if (access.Role != "coordinator") throw new FirebaseException(403, "Only the authorized admin/coordinator may give the final decision.");
                    Revision(record, input); State(record, access.Role, "pendingAdminReview");
                    var approval = record["academicReviews"]?[T(record, "academicApprovalId")];
                    if (approval == null || T(approval, "revisionId") != T(record, "currentRevisionId") || T(approval, "action") != "academicApprove" || (action == "finalApprove" && T(approval, "academicSupervisorId") != access.Academic)) throw new FirebaseException(409, "Fresh academic approval of the current revision and current academic assignment is required.");
                    string feedback = Text(input, "feedback", 4000, action == "adminCorrection"); var reviews = O(record["adminReviews"]);
                    reviews[requestId] = new JObject { ["revisionId"] = record["currentRevisionId"], ["actorUid"] = uid, ["action"] = action, ["feedback"] = feedback, ["at"] = Now() }; record["adminReviews"] = reviews;
                    if (action == "adminCorrection") { record["status"] = "academicRevisionRequested"; record.Remove("academicApprovalId"); }
                    else
                    {
                        long minutes = await Eligible(placementId, access);
                        record["finalization"] = new JObject { ["requestId"] = requestId, ["actorUid"] = uid, ["revisionId"] = record["currentRevisionId"], ["grade"] = Grade(input, "finalGrade"), ["attachment"] = Attachment(Text(input, "fileName", 180, true)), ["approvedTrainingMinutes"] = minutes, ["preparedAt"] = Now(), ["phase"] = "prepared" };
                    }
                }
                else throw new FirebaseException(400, "Unsupported final report action.");
                record["updatedAt"] = Now(); Audit(record, access, requestId, action, previous);
                var requests = O(record["requests"]); requests[requestId] = new JObject { ["digest"] = digest, ["at"] = Now() }; record["requests"] = requests;
                bool saved;
                try { saved = await db.CompareWriteAsync(Path(placementId), record, node.ETag); }
                catch (Exception)
                {
                    var persisted = O(await db.ReadAsync(Path(placementId)));
                    if (T(persisted["requests"]?[requestId], "digest") != digest) throw;
                    saved = true;
                }
                if (!saved) { if (retry < 2) continue; throw new FirebaseException(409, "Another review changed this report. Reload before deciding."); }
                if (action == "finalApprove") await Reconcile(uid, role, placementId);
                return await ReadAsync(uid, role, placementId);
            }
            throw new FirebaseException(409, "The report changed. Reload.");
        }
        public async Task Reconcile(string uid, string role, string placementId)
        {
            var a = await Authorize(uid, role, placementId); if (a.Role != "coordinator") throw new FirebaseException(403, "Admin reconciliation is required.");
            var n = await db.ReadVersionAsync(Path(placementId)); var r = n.Value; if (r == null || T(r, "status") == "completed") return;
            var f = r["finalization"] as JObject; if (f == null) throw new FirebaseException(409, "There is no pending finalization.");
            await Eligible(placementId, a);
            var approval = r["academicReviews"]?[T(r, "academicApprovalId")];
            if (T(approval, "academicSupervisorId") != a.Academic || T(approval, "revisionId") != T(r, "currentRevisionId")) throw new FirebaseException(409, "Assignment changed during finalization. Completion remains blocked.");
            string operation = T(f, "requestId"); string notificationId = "final-training-" + placementId;
            var outcome = new JObject { ["placementId"] = placementId, ["studentId"] = a.Student, ["finalGrade"] = f["grade"], ["gradeMaximum"] = 100, ["revisionId"] = f["revisionId"], ["requestId"] = operation, ["approvedBy"] = f["actorUid"], ["approvedAt"] = f["preparedAt"], ["completionReport"] = f["attachment"].DeepClone() };
            var notification = new JObject { ["id"] = notificationId, ["studentId"] = a.Student, ["placementId"] = placementId, ["requestId"] = operation, ["type"] = "trainingCompletion", ["message"] = "Congratulations! You have successfully completed your field training.", ["createdAt"] = f["preparedAt"], ["read"] = false };
            await Materialize("finalTrainingOutcomes/" + placementId, outcome, operation);
            await Materialize("notifications/" + Key(a.Student) + "/" + Key(notificationId), notification, operation);
            var p = await db.ReadVersionAsync("placements/" + placementId);
            if (T(p.Value, "studentId") != a.Student || T(p.Value, "companyId") != T(r, "companyId") || T(p.Value, "academicSupervisorId") != a.Academic || T(p.Value, "companySupervisorId") != a.CompanySupervisor) throw new FirebaseException(409, "Placement assignment changed. Finalization is blocked.");
            if (T(p.Value, "finalEvaluationRequestId") != operation)
            {
                if (T(p.Value, "status") == "completed" || T(p.Value, "finalEvaluationRequestId").Length > 0) throw new FirebaseException(409, "An existing completion outcome must not be overwritten.");
                p.Value["status"] = "completed"; p.Value["completionStatus"] = "Completed"; p.Value["finalEvaluationRequestId"] = operation; p.Value["finalEvaluationRevisionId"] = r["currentRevisionId"]; p.Value["finalGrade"] = f["grade"]; p.Value["completedAt"] = f["preparedAt"];
                if (!await db.CompareWriteAsync("placements/" + placementId, p.Value, p.ETag)) throw new FirebaseException(409, "Placement changed; retry finalization with the same request.");
            }
            a = await Authorize(uid, role, placementId); await Eligible(placementId, a);
            n = await db.ReadVersionAsync(Path(placementId)); r = n.Value;
            if (T(r, "status") == "completed") return;
            if (T(r["finalization"], "requestId") != operation) throw new FirebaseException(409, "Finalization conflict.");
            r["status"] = "completed"; r["outcome"] = outcome; r["studentNotification"] = notification; r["completedAt"] = Now(); r["finalization"]["phase"] = "complete";
            Audit(r, a, "complete-" + operation, "finalized", "pendingAdminReview");
            try { if (!await db.CompareWriteAsync(Path(placementId), r, n.ETag)) throw new FirebaseException(409, "Completion acknowledgment changed; retry reconciliation."); }
            catch (Exception) { var persisted = O(await db.ReadAsync(Path(placementId))); if (T(persisted, "status") != "completed" || T(persisted["finalization"], "requestId") != operation) throw; }
        }
        private async Task Materialize(string path, JObject value, string requestId)
        {
            var old = await db.ReadVersionAsync(path);
            if (old.Value != null) { if (T(old.Value, "requestId") == requestId && ContainsExpected(old.Value, value)) return; throw new FirebaseException(409, "A different or incomplete completion record already exists."); }
            try { if (!await db.CompareWriteAsync(path, value, old.ETag)) throw new FirebaseException(409, "Completion record changed. Retry reconciliation."); }
            catch (Exception) { var saved = O(await db.ReadAsync(path)); if (T(saved, "requestId") != requestId || !ContainsExpected(saved, value)) throw; }
        }
        private static bool ContainsExpected(JToken actual, JToken expected)
        {
            if (expected == null || expected.Type == JTokenType.Null) return actual == null || actual.Type == JTokenType.Null;
            if (expected is JObject) return actual is JObject && ((JObject)expected).Properties().All(p => ContainsExpected(actual[p.Name], p.Value));
            return JToken.DeepEquals(actual, expected);
        }
        public async Task<JObject> ReadAsync(string uid, string role, string placementId)
        {
            var a = await Authorize(uid, role, placementId); var r = O(await db.ReadAsync(Path(placementId)));
            string status = T(r, "status"); if (status.Length == 0) status = "draft";
            if (a.Role == "coordinator" && !new[] { "pendingAdminReview", "academicRevisionRequested", "completed" }.Contains(status)) throw new FirebaseException(403, "Administration can only access academically approved reports and their correction history.");
            if (status == "completed")
            {
                string operation = T(r["finalization"], "requestId");
                var outcome = await db.ReadAsync("finalTrainingOutcomes/" + placementId);
                var notification = await db.ReadAsync("notifications/" + Key(a.Student) + "/" + Key("final-training-" + placementId));
                if (operation.Length == 0 || T(outcome, "requestId") != operation || T(notification, "requestId") != operation || !ContainsExpected(outcome, r["outcome"]) || T(a.Placement, "finalEvaluationRequestId") != operation || T(a.Placement, "status") != "completed") status = "completionConsistencyPending";
            }
            long? minutes = null; string blocker = null; try { minutes = await Minutes(placementId, a); } catch (FirebaseException e) { blocker = e.Message; }
            if (a.Role == "student")
            {
                var published = status == "completed" ? r["outcome"] : null;
                return new JObject { ["placementId"] = placementId, ["status"] = status, ["completed"] = status == "completed", ["approvedTrainingMinutes"] = minutes, ["requiredTrainingMinutes"] = RequiredMinutes,
                    ["outcome"] = published == null ? null : new JObject { ["finalGrade"] = published["finalGrade"], ["gradeMaximum"] = published["gradeMaximum"], ["approvedAt"] = published["approvedAt"], ["completionReport"] = published["completionReport"]?.DeepClone() },
                    ["notification"] = status != "completed" ? null : new JObject { ["id"] = r["studentNotification"]?["id"], ["message"] = r["studentNotification"]?["message"], ["createdAt"] = r["studentNotification"]?["createdAt"] } };
            }
            return new JObject { ["placementId"] = placementId, ["studentId"] = a.Student, ["role"] = a.Role, ["status"] = status, ["record"] = r, ["approvedTrainingMinutes"] = minutes, ["requiredTrainingMinutes"] = RequiredMinutes, ["eligible"] = minutes >= RequiredMinutes, ["eligibilityBlocker"] = blocker };
        }
        public async Task<JObject> ListAsync(string uid, string role)
        {
            var placements = O(await db.ReadAsync("placements")); var output = new JArray();
            foreach (var p in placements.Properties())
            {
                try
                {
                    var view = await ReadAsync(uid, role, p.Name);
                    var canonical = FirebaseSignInService.CanonicalRole(role); string state = T(view, "status");
                    if (canonical == "coordinator" && !new[] { "pendingAdminReview", "academicRevisionRequested", "completed" }.Contains(state)) continue;
                    if (canonical == "academicSupervisor" && state == "draft") continue;
                    output.Add(view);
                }
                catch (FirebaseException e) when (e.StatusCode == 403 || e.StatusCode == 409) { }
            }
            return new JObject { ["items"] = output, ["role"] = FirebaseSignInService.CanonicalRole(role), ["serverNow"] = Now() };
        }
    }
}
