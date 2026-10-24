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
    public sealed partial class FinalReportWorkflow
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
        private sealed class Access { public string Uid, Role, Student, StudentName, StudentEmail, AcademicName, CompanySupervisor, Academic; public JObject Profile, Placement; }
        private async Task<Access> Authorize(string uid, string ticketRole, string placementId)
        {
            Key(uid); Key(placementId);
            if (!await db.IsActiveAuthenticationUidAsync(uid)) throw new FirebaseException(401, "An active verified account is required.");
            var profile = O(await db.ReadAsync("users/" + uid)); var login = FirebaseSignInService.ProfileResult(uid, profile);
            if (login.MustChangePassword) throw new FirebaseException(403, "Complete the required password change before reviewing final reports.");
            if (login.Role != FirebaseSignInService.CanonicalRole(ticketRole)) throw new FirebaseException(403, "The current account role changed.");
            var p = O(await db.ReadAsync("placements/" + placementId));
            string studentId = T(p, "studentId"), company = T(p, "companyId"), supervisor = T(p, "companySupervisorId"), academic = T(p, "academicSupervisorId");
            if (supervisor.Length == 0) supervisor = T(p, "supervisorId");
            if (studentId.Length == 0 || company.Length == 0 || supervisor.Length == 0) throw new FirebaseException(409, "The placement is missing its student, company or company supervisor. The coordinator must complete these existing relationships.");
            string status = T(p, "status").ToLowerInvariant(); if (!new[] { "active", "confirmed", "completed" }.Contains(status)) throw new FirebaseException(409, "The placement is not confirmed or active.");
            var allProfiles = O(await db.ReadAsync("users"));
            var students = new JObject(allProfiles.Properties().Where(entry => T(entry.Value, "studentId") == studentId).Select(entry => new JProperty(entry.Name, entry.Value.DeepClone())));
            if (!students.HasValues) { var student = O(await db.ReadAsync("users/" + Key(studentId))); if (student.HasValues) students[studentId] = student; }
            string studentUid = T(p, "studentUid");
            var studentProfile = students.Properties().FirstOrDefault(x => (studentUid.Length == 0 || x.Name == studentUid) && FirebaseSignInService.CanonicalRole(T(x.Value, "role")) == "student" && (bool?)x.Value["isActive"] == true);
            if (studentProfile == null) throw new FirebaseException(409, "The placement student has no active existing profile.");
            string profileAcademic = T(studentProfile.Value, "academicSupervisorId");
            var allAssignments = O(await db.ReadAsync("supervisorAssignments"));
            var assignments = new JObject(allAssignments.Properties().Where(entry => T(entry.Value, "studentId") == studentId).Select(entry => new JProperty(entry.Name, entry.Value.DeepClone())));
            var currentAssignments = assignments.Properties().Where(x => (bool?)x.Value["isActive"] != false && !new[] { "inactive", "ended", "reassigned" }.Contains(T(x.Value, "status").ToLowerInvariant())).Select(x => T(x.Value, "academicSupervisorId")).Where(x => x.Length > 0).Distinct().ToArray();
            // Resolve a missing placement field only from the student's existing current
            // assignment. Never select an arbitrary professor or persist a guessed mapping.
            if (academic.Length == 0)
            {
                var assignedIds = currentAssignments.Concat(new[] { profileAcademic }).Where(x => x.Length > 0).Distinct().ToArray();
                if (assignedIds.Length != 1) throw new FirebaseException(409, "No single current academic supervisor is assigned to this student. Assign the student through Assign Supervisors before final evaluation.");
                academic = assignedIds[0];
            }
            if ((profileAcademic.Length > 0 && profileAcademic != academic) || currentAssignments.Any(x => x != academic)) throw new FirebaseException(409, "Current academic assignment and placement disagree. Reconcile the assignment before review.");
            var academicProfiles = allProfiles.Properties().Where(entry => entry.Name == academic || T(entry.Value, "academicSupervisorId") == academic)
                .Select(entry => entry.Value).Where(value => FirebaseSignInService.CanonicalRole(T(value, "role")) == "academicSupervisor" && (bool?)value["isActive"] == true && (T(value, "accountSetupStatus").Length == 0 || T(value, "accountSetupStatus") == "complete")).ToArray();
            if (academicProfiles.Length != 1) throw new FirebaseException(409, "The current academic supervisor must have exactly one active completed account. The coordinator must resolve the existing assignment.");
            var academicProfile = academicProfiles[0];
            string actorSupervisor = T(profile, "companySupervisorId"); if (actorSupervisor.Length == 0) actorSupervisor = T(profile, "supervisorId");
            if (actorSupervisor.Length == 0 && login.Role == "companySupervisor") { var map = O(await db.ReadAsync("companySupervisors")); actorSupervisor = map.Properties().Where(x => T(x.Value, "firebaseUid") == uid && T(x.Value, "status") == "active").Select(x => x.Name).SingleOrDefault() ?? ""; }
            string actorAcademic = T(profile, "academicSupervisorId");
            bool allowed = login.Role == "coordinator" || (login.Role == "companySupervisor" && actorSupervisor == supervisor && T(profile, "companyId") == company) ||
                (login.Role == "academicSupervisor" && (uid == academic || actorAcademic == academic)) || (login.Role == "student" && (uid == studentProfile.Name || T(profile, "studentId") == studentId));
            if (!allowed) throw new FirebaseException(403, "This placement is not assigned to your current account.");
            return new Access { Uid = uid, Role = login.Role, Placement = p, Profile = profile, Student = studentId, StudentEmail = T(studentProfile.Value, "email"), StudentName = T(studentProfile.Value, "fullName").Length > 0 ? T(studentProfile.Value, "fullName") : T(studentProfile.Value, "displayName"), AcademicName = T(academicProfile, "fullName").Length > 0 ? T(academicProfile, "fullName") : T(academicProfile, "displayName"), CompanySupervisor = supervisor, Academic = academic };
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
        public async Task<JObject> SourceSummaryAsync(string uid, string role, string placementId)
        {
            var a = await Authorize(uid, role, Key(placementId));
            if (a.Role != "companySupervisor") throw new FirebaseException(403, "Only the assigned company supervisor can generate this report summary.");
            var profiles = O(await db.ReadAsync("users"));
            var names = profiles.Properties().Where(p => T(p.Value, "studentId") == a.Student && T(p.Value, "role") == "student").Select(p => T(p.Value, "fullName")).Where(n => n.Length > 0).Distinct().ToArray();
            string name = names.Length == 1 ? names[0] : a.Student;
            var tasks = O(await db.ReadAsync("tasks")); var taskRows = new JArray();
            foreach (var entry in tasks.Properties()) if (T(entry.Value, "placementId") == placementId && T(entry.Value, "studentId") == a.Student)
                taskRows.Add(new JObject { ["taskId"] = entry.Name, ["title"] = entry.Value["title"], ["status"] = entry.Value["status"], ["score"] = entry.Value["score"] ?? entry.Value["grade"], ["maximumScore"] = entry.Value["maximumScore"], ["taskType"] = entry.Value["taskType"], ["examId"] = entry.Value["examId"] });
            var exams = O(await db.ReadAsync("taskExams")); var examRows = new JArray();
            foreach (var exam in exams.Properties().Where(e => T(e.Value, "placementId") == placementId))
                foreach (var attempt in O(await db.ReadAsync("taskExamAttempts/" + Key(exam.Name) + "/" + Key(a.Student))).Properties())
                {
                    if (!new[] { "submitted", "expired" }.Contains(T(attempt.Value, "status"))) continue;
                    var row = new JObject { ["examId"] = exam.Name, ["taskId"] = exam.Value["taskId"], ["attemptId"] = attempt.Name, ["submittedAt"] = attempt.Value["submittedAt"], ["title"] = exam.Value["title"], ["status"] = attempt.Value["status"], ["resultStatus"] = attempt.Value["resultStatus"] };
                    if (T(attempt.Value, "resultStatus") == "released") { row["score"] = attempt.Value["score"]; row["maximumScore"] = attempt.Value["maximumScore"]; }
                    examRows.Add(row);
                }
            var gradeItems = new JArray(); int excluded = 0;
            bool IncludeGrade(JToken item, string kind, string id)
            {
                decimal score, maximum;
                if (!decimal.TryParse(T(item, "score"), NumberStyles.Number, CultureInfo.InvariantCulture, out score) || !decimal.TryParse(T(item, "maximumScore"), NumberStyles.Number, CultureInfo.InvariantCulture, out maximum) || maximum <= 0 || score < 0 || score > maximum) return false;
                gradeItems.Add(new JObject { ["kind"] = kind, ["id"] = id, ["title"] = item["title"], ["score"] = score, ["maximumScore"] = maximum, ["percentage"] = score / maximum * 100m }); return true;
            }
            var linkedTasks = new System.Collections.Generic.HashSet<string>(exams.Properties().Where(e => T(e.Value, "placementId") == placementId).Select(e => T(e.Value, "taskId")).Where(id => id.Length > 0), StringComparer.Ordinal);
            foreach (var task in taskRows)
            {
                if (linkedTasks.Contains(T(task, "taskId")) || T(task, "examId").Length > 0 || T(task, "taskType").Equals("exam", StringComparison.OrdinalIgnoreCase)) { ((JObject)task).Remove("score"); ((JObject)task).Remove("maximumScore"); continue; }
                if (!T(task, "status").Equals("completed", StringComparison.OrdinalIgnoreCase) || !IncludeGrade(task, "task", T(task, "taskId"))) excluded++;
            }
            foreach (var group in examRows.GroupBy(e => T(e, "examId")))
            {
                var attempts = group.ToArray();
                if (attempts.Length > 1 && attempts.Any(e => !DateTimeOffset.TryParse(T(e, "submittedAt"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ignored))) { excluded++; continue; }
                var latest = attempts.OrderByDescending(e => { DateTimeOffset stamp; return DateTimeOffset.TryParse(T(e, "submittedAt"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out stamp) ? stamp : DateTimeOffset.MinValue; }).First();
                if (T(latest, "resultStatus") != "released" || !IncludeGrade(latest, "exam", group.Key)) excluded++;
            }
            decimal? suggested = gradeItems.Count == 0 ? (decimal?)null : Math.Round(gradeItems.Average(x => (decimal)x["percentage"]), 2, MidpointRounding.AwayFromZero);
            var calculation = new JObject { ["method"] = "equal-assessment-average-v1", ["percentage"] = suggested, ["items"] = gradeItems, ["includedCount"] = gradeItems.Count, ["excludedCount"] = excluded, ["provisional"] = excluded > 0, ["explanation"] = "Each completed ordinary task and latest released exam counts once after normalization to 100. Linked exam tasks are excluded. Missing/unreleased results are not zero grades. Final academic/admin approval remains separate." };
            var weekly = (JArray)(await WeeklyReportsAsync(uid, role, placementId))["reports"];
            long? minutes = null; string blocker = null; try { minutes = await Minutes(placementId, a); } catch (FirebaseException e) { blocker = e.Message; }
            var lines = new System.Collections.Generic.List<string> { "Training report for " + name + " | Student ID: " + a.Student, "Generated: " + Now(), "Approved training minutes: " + (minutes.HasValue ? minutes.ToString() : "Unverified"), "Tasks recorded: " + taskRows.Count, "Submitted examination attempts: " + examRows.Count, "Weekly reports available: " + weekly.Count, "Calculated assessment percentage: " + (suggested.HasValue ? suggested.Value.ToString("0.00", CultureInfo.InvariantCulture) + "/100" : "Unavailable - no eligible grades"), "Calculation: equal average of " + gradeItems.Count + " assessments; " + excluded + " excluded" };
            foreach (var task in taskRows) lines.Add("Task: " + T(task, "title") + " | " + T(task, "status"));
            foreach (var exam in examRows) lines.Add("Exam: " + T(exam, "title") + " | " + (T(exam, "resultStatus") == "released" ? T(exam, "score") + "/" + T(exam, "maximumScore") : "Result awaiting release"));
            string comments = string.Join("\n", lines); if (comments.Length > 4800) comments = comments.Substring(0, 4700) + "\nAdditional activity is available in the source records.";
            return new JObject { ["studentId"] = a.Student, ["studentName"] = name, ["companyName"] = a.Placement["companyName"], ["academicSupervisorName"] = a.AcademicName, ["trainingStart"] = a.Placement["startDate"], ["trainingEnd"] = a.Placement["endDate"], ["requiredTrainingMinutes"] = RequiredMinutes, ["placementId"] = placementId, ["comments"] = comments, ["gradeCalculation"] = calculation, ["tasks"] = taskRows, ["exams"] = examRows, ["weeklyReports"] = weekly, ["approvedTrainingMinutes"] = minutes, ["eligibilityBlocker"] = blocker, ["generatedAt"] = Now(), ["fileName"] = "final-training-report-" + a.Student + ".pdf" };
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
                if (T(record,"studentName").Length == 0) record["studentName"] = access.StudentName;
                if (action == "save" || action == "submit")
                {
                    if (access.Role != "companySupervisor") throw new FirebaseException(403, "Only the assigned company supervisor prepares this report.");
                    State(record, access.Role, "draft", "companyRevisionRequested");
                    if (T(input, "expectedDraftVersion") != T(record, "draftVersion")) throw new FirebaseException(409, "The saved draft changed in another request. Reload before saving or submitting.");
                    if (action == "save")
                    {
                        JObject generatedSource = null;
                        if ((bool?)input["useCalculatedGrade"] == true)
                        {
                            generatedSource = await SourceSummaryAsync(uid, role, placementId);
                            var calculated = generatedSource["gradeCalculation"]?["percentage"];
                            if (calculated == null || calculated.Type == JTokenType.Null) throw new FirebaseException(409, "No completed approved tasks or released exam grades are available to calculate a percentage.");
                            input["percentage"] = calculated.DeepClone(); input["comments"] = generatedSource["comments"].DeepClone(); input["fileName"] = generatedSource["fileName"].DeepClone(); input["weeklyReportIds"] = new JArray(((JArray)generatedSource["weeklyReports"]).Select(w => w["reportId"].DeepClone()));
                        }
                        var snapshots = await WeeklySnapshots(uid, role, placementId, input["weeklyReportIds"]) ?? (record["draft"]?["weeklyReports"] as JArray)?.DeepClone() ?? new JArray();
                        record["draft"] = new JObject { ["percentage"] = Grade(input, "percentage"), ["comments"] = Text(input, "comments", 5000, true), ["companyResponse"] = Text(input, "companyResponse", 3000), ["attachment"] = Attachment(Text(input, "fileName", 180, true)), ["updatedAt"] = Now() };
                        record["draft"]["weeklyReports"] = snapshots;
                        if (generatedSource != null) { record["draft"]["sourceSnapshot"] = generatedSource; record["draft"]["gradeCalculation"] = generatedSource["gradeCalculation"].DeepClone(); }
                        record["draftVersion"] = requestId;
                    }
                    else
                    {
                        long minutes = await Minutes(placementId, access); var draft = O(record["draft"]);
                        if (draft.HasValues) { draft["studentId"] = access.Student; draft["studentName"] = access.StudentName; }
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
                    long minutes = action == "academicApprove" ? await Minutes(placementId, access) : 0;
                    var reviews = O(record["academicReviews"]); reviews[requestId] = new JObject { ["revisionId"] = record["currentRevisionId"], ["actorUid"] = uid, ["academicSupervisorId"] = access.Academic, ["action"] = action, ["feedback"] = feedback, ["academicGrade"] = action == "academicApprove" ? (JToken)Grade(input, "academicGrade") : null, ["at"] = Now(), ["approvedTrainingMinutes"] = minutes }; record["academicReviews"] = reviews;
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
                    if (action == "adminCorrection") { record["status"] = "companyRevisionRequested"; record["draft"] = record["revisions"][T(record, "currentRevisionId")].DeepClone(); record["draft"]["companyResponse"] = ""; record["draftVersion"] = "return-" + requestId; record.Remove("academicApprovalId"); }
                    else
                    {
                        long minutes = await Minutes(placementId, access);
                        // Older clients may still supply a notice filename; new clients need no filename input.
                        string completionFileName = Text(input, "fileName", 180);
                        if (completionFileName.Length == 0) completionFileName = "training-certificate-" + access.Student + ".pdf";
                        record["finalization"] = new JObject { ["requestId"] = requestId, ["actorUid"] = uid, ["revisionId"] = record["currentRevisionId"], ["grade"] = Grade(input, "finalGrade"), ["attachment"] = Attachment(completionFileName), ["approvedTrainingMinutes"] = minutes, ["preparedAt"] = Now(), ["phase"] = "prepared", ["certificate"] = new JObject { ["fileName"] = "training-certificate-" + access.Student + ".pdf", ["contentType"] = "application/pdf", ["generationStatus"] = "serverGenerated", ["templateVersion"] = 1, ["studentName"] = access.StudentName, ["companyName"] = T(access.Placement,"companyName"), ["startDate"] = access.Placement["startDate"], ["endDate"] = access.Placement["endDate"] } };
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
            await Minutes(placementId, a);
            var approval = r["academicReviews"]?[T(r, "academicApprovalId")];
            if (T(approval, "academicSupervisorId") != a.Academic || T(approval, "revisionId") != T(r, "currentRevisionId")) throw new FirebaseException(409, "Assignment changed during finalization. Completion remains blocked.");
            string operation = T(f, "requestId"); string notificationId = "final-training-" + placementId;
            var outcome = new JObject { ["placementId"] = placementId, ["studentId"] = a.Student, ["finalGrade"] = f["grade"], ["gradeMaximum"] = 100, ["revisionId"] = f["revisionId"], ["requestId"] = operation, ["approvedBy"] = f["actorUid"], ["approvedAt"] = f["preparedAt"], ["completionReport"] = f["attachment"].DeepClone(), ["certificate"] = f["certificate"]?.DeepClone() };
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
            a = await Authorize(uid, role, placementId); await Minutes(placementId, a);
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
            if (a.Role == "coordinator" && !new[] { "pendingAdminReview", "academicRevisionRequested", "completed" }.Contains(status) && !O(r["adminReviews"]).HasValues) throw new FirebaseException(403, "Administration can only access academically approved reports and their correction history.");
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
                return new JObject { ["placementId"] = placementId, ["studentId"] = a.Student, ["studentName"] = a.StudentName, ["status"] = status, ["completed"] = status == "completed", ["approvedTrainingMinutes"] = minutes, ["requiredTrainingMinutes"] = RequiredMinutes,
                    ["outcome"] = published == null ? null : new JObject { ["finalGrade"] = published["finalGrade"], ["gradeMaximum"] = published["gradeMaximum"], ["approvedAt"] = published["approvedAt"], ["completionReport"] = published["completionReport"]?.DeepClone(), ["certificate"] = published["certificate"]?.DeepClone() },
                    ["notification"] = status != "completed" ? null : new JObject { ["id"] = r["studentNotification"]?["id"], ["message"] = r["studentNotification"]?["message"], ["createdAt"] = r["studentNotification"]?["createdAt"] } };
            }
            return new JObject { ["placementId"] = placementId, ["studentId"] = a.Student, ["studentName"] = a.StudentName, ["academicSupervisorId"] = a.Academic, ["academicSupervisorName"] = a.AcademicName, ["role"] = a.Role, ["status"] = status, ["record"] = r, ["approvedTrainingMinutes"] = minutes, ["requiredTrainingMinutes"] = RequiredMinutes, ["eligible"] = minutes >= RequiredMinutes, ["eligibilityBlocker"] = blocker };
        }
        public async Task<JObject> ListAsync(string uid, string role)
        {
            // This read-only cache belongs to this list request only. Mutations/detail requests
            // keep using fresh authoritative reads and the existing CAS protections.
            var cache = new ListReadDatabase(db);
            if (!await cache.IsActiveAuthenticationUidAsync(uid)) throw new FirebaseException(401, "An active verified account is required.");
            var profile = O(await cache.ReadAsync("users/" + Key(uid)));
            var verified = FirebaseSignInService.ProfileResult(uid, profile);
            if (verified.Role != FirebaseSignInService.CanonicalRole(role)) throw new FirebaseException(403, "The current account role changed.");
            var placements = O(await cache.ReadAsync("placements")); var output = new JArray();
            var issues = new System.Collections.Concurrent.ConcurrentBag<string>();
            string canonicalRole = FirebaseSignInService.CanonicalRole(role);
            string currentCompany = T(profile, "companyId");
            var reader = new FinalReportWorkflow(cache, clock);
            using (var limit = new System.Threading.SemaphoreSlim(4))
            {
            var reads = placements.Properties().Select(async p =>
            {
                await limit.WaitAsync();
                try
                {
                    var view = await reader.ReadAsync(uid, role, p.Name);
                    var canonical = FirebaseSignInService.CanonicalRole(role); string state = T(view, "status");
                    if (canonical == "coordinator" && !new[] { "pendingAdminReview", "academicRevisionRequested", "completed" }.Contains(state) && !O(view["record"]?["adminReviews"]).HasValues) return null;
                    if (canonical == "academicSupervisor" && state == "draft") return null;
                    return view;
                }
                catch (FirebaseException e) when (e.StatusCode == 403 || e.StatusCode == 409) {
                    // Report relationship failures only within this actor's company;
                    // never disclose another company's student/placement details.
                    if (e.StatusCode == 409 && canonicalRole == "companySupervisor" && currentCompany.Length > 0 && T(p.Value,"companyId") == currentCompany)
                        issues.Add("A company training record cannot be reviewed: " + e.Message);
                    return null;
                }
                finally { limit.Release(); }
            }).ToArray();
            foreach (var view in await Task.WhenAll(reads)) if (view != null) output.Add(view);
            }
            if (canonicalRole == "companySupervisor" && currentCompany.Length > 0) {
                var workflows = O(await cache.ReadAsync("finalEvaluationWorkflows"));
                if (workflows.Properties().Any(w => T(w.Value,"companyId") == currentCompany && placements[w.Name] == null))
                    issues.Add("A saved company report has no linked training placement. Restore or reconcile its original placement through the coordinator; the saved report has not been deleted.");
            }
            return new JObject { ["items"] = output, ["issues"] = new JArray(issues.Distinct()), ["role"] = FirebaseSignInService.CanonicalRole(role), ["serverNow"] = Now() };
        }
        private sealed class ListReadDatabase : IApplicationDatabase
        {
            private readonly IApplicationDatabase inner;
            private readonly System.Collections.Generic.Dictionary<string, Task<JToken>> reads = new System.Collections.Generic.Dictionary<string, Task<JToken>>(StringComparer.Ordinal);
            private readonly System.Collections.Generic.Dictionary<string, Task<bool>> identities = new System.Collections.Generic.Dictionary<string, Task<bool>>(StringComparer.Ordinal);
            private readonly object gate = new object();
            internal ListReadDatabase(IApplicationDatabase source) { inner = source; }
            public Task<JToken> ReadAsync(string path)
            {
                lock (gate)
                {
                    Task<JToken> result;
                    if (reads.TryGetValue(path, out result)) return Clone(result);
                    // Reuse already requested parent collections (users and placements) without new GETs.
                    int slash = path.LastIndexOf('/');
                    while (slash > 0)
                    {
                        string parent = path.Substring(0, slash);
                        if (reads.TryGetValue(parent, out result)) { var relative = path.Substring(slash + 1); result = Child(result, relative); reads[path] = result; return Clone(result); }
                        slash = path.LastIndexOf('/', slash - 1);
                    }
                    result = inner.ReadAsync(path); reads[path] = result; return Clone(result);
                }
            }
            private static async Task<JToken> Child(Task<JToken> parent, string path) { var value = await parent; foreach (var key in path.Split('/')) value = value?[key]; return value; }
            private static async Task<JToken> Clone(Task<JToken> read) { return (await read)?.DeepClone(); }
            public Task<bool> IsActiveAuthenticationUidAsync(string uid) { lock(gate){Task<bool> value;if(!identities.TryGetValue(uid,out value))identities[uid]=value=inner.IsActiveAuthenticationUidAsync(uid);return value;} }
            public Task<JToken> ReadEqualAsync(string path,string field,string value) { return inner.ReadEqualAsync(path,field,value); }
            public Task<StudentVersion> ReadVersionAsync(string path) { throw new InvalidOperationException("List snapshots cannot authorize writes."); }
            public Task<bool> CompareWriteAsync(string path,object value,string etag) { throw new InvalidOperationException("List snapshots are read-only."); }
        }
    }
}
