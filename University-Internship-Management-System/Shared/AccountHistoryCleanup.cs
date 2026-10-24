using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    // Account deletion must also remove relationships keyed by business IDs. Otherwise a
    // fresh Firebase login with the same student/staff/company ID inherits the old account.
    internal static class AccountHistoryCleanup
    {
        public static Task StudentAsync(IStudentDatabase db, string uid, string studentId)
        { return ApplyAsync(db, root => RemoveTraining(root, uid, studentId, null)); }

        public static Task CompanySupervisorAsync(IStudentDatabase db, string uid, string companyId)
        {
            return ApplyAsync(db, root => {
                bool lastSupervisor = !Rows(root, "users").Any(p => p.Name != uid && Text(p.Value, "companyId") == companyId &&
                    FirebaseSignInService.CanonicalRole(Text(p.Value, "role")) == "companySupervisor");
                RemoveTraining(root, uid, null, lastSupervisor ? companyId : null);
            });
        }

        public static Task CompanyAsync(IStudentDatabase db, string companyId)
        { return ApplyAsync(db, root => RemoveTraining(root, null, null, companyId)); }

        public static Task AcademicAsync(IStudentDatabase db, string uid)
        {
            return ApplyAsync(db, root => {
                var ids = Set(uid);
                foreach (string branch in new[] { "assignments", "supervisorAssignments", "academicFinalEvaluations", "academicFeedback" })
                    RemoveRows(root, branch, p => References(p.Value, ids, "academicSupervisorId", "supervisorId", "evaluatorId", "reviewerId", "authorUid"));
                // These are other students' records. Detach the deleted supervisor, without
                // removing the students, their company work, or another supervisor's reviews.
                foreach (string branch in new[] { "users", "placements", "groups", "weeklyReports", "uimsAttendance/trainingAssignments" })
                    foreach (var row in Rows(root, branch).ToArray()) ClearReferences(row.Value, ids, "academicSupervisorId", "supervisorUid");
                foreach (var group in Rows(root, "groups")) ClearReferences(group.Value, ids, "supervisorId");
                foreach (var evaluation in Rows(root, "finalEvaluationReviewsV2"))
                    RemoveNestedReviews(evaluation.Value, ids);
                foreach (var report in Rows(root, "weeklyReportReviews")) RemoveNestedReviews(report.Value, ids);
                RemoveRows(root, "notifications", p => ids.Contains(p.Name) || References(p.Value, ids, "userId", "uid", "recipientUid", "ownerUid"));
            });
        }

        private static async Task ApplyAsync(IStudentDatabase db, Action<JObject> change)
        {
            // One conditional root commit makes the cleanup atomic across all established
            // collections. Retry against a fresh snapshot; never overwrite concurrent work.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var snapshot = await db.ReadVersionAsync("").ConfigureAwait(false);
                if (snapshot.Value == null) return;
                if (string.IsNullOrWhiteSpace(snapshot.ETag)) throw new StudentManagementException("Account history could not be versioned. Retry deletion.");
                var next = (JObject)snapshot.Value.DeepClone(); change(next);
                if (JToken.DeepEquals(next, snapshot.Value)) return;
                if (await db.CompareWriteAsync("", next, snapshot.ETag).ConfigureAwait(false)) return;
            }
            throw new StudentManagementException("Related records changed during deletion. The account remains disabled; retry deletion to finish cleanup.");
        }

        private static void RemoveTraining(JObject root, string uid, string studentId, string companyId)
        {
            var uids = Set(uid); var students = Set(studentId); var companies = Set(companyId);
            var supervisors = Set(uid);
            if (!string.IsNullOrEmpty(uid))
            {
                var profile = At(root, "users")?[uid]; Add(supervisors, Text(profile, "companySupervisorId"));
                foreach (string branch in new[] { "companySupervisors", "uimsAttendance/companySupervisors" })
                    foreach (var row in Rows(root, branch))
                        if (Text(row.Value, "firebaseUid") == uid || row.Name == uid) Add(supervisors, row.Name);
            }
            if (students.Count > 0)
            {
                // Imported accounts may have had a different Auth UID before recreation.
                foreach (var row in Rows(root, "users")) if (students.Contains(Text(row.Value, "studentId"))) Add(uids, row.Name);
                foreach (string branch in new[] { "applications", "placements", "assignments", "supervisorAssignments" })
                    foreach (var row in Rows(root, branch)) if (students.Contains(Text(row.Value, "studentId"))) Add(uids, Text(row.Value, "studentUid"));
            }
            Func<JToken, bool> own = value => References(value, students, "studentId", "studentProfileId") ||
                References(value, uids, "studentUid", "ownerUid", "userId", "uid", "createdByUid") ||
                References(value, companies, "companyId") || (students.Count == 0 && References(value, supervisors, "companySupervisorId", "supervisorId", "assignedBy", "ownerUid", "createdBy"));
            var placements = Set(); var opportunities = Set(); var applications = Set(); var tasks = Set(); var exams = Set(); var reports = Set();
            Collect(root, "opportunities", opportunities, own);
            Collect(root, "applications", applications, v => own(v) || References(v, opportunities, "opportunityId"));
            Collect(root, "placements", placements, v => own(v) || References(v, applications, "applicationId") || References(v, opportunities, "opportunityId"));
            Func<JToken, bool> training = value => own(value) || References(value, placements, "placementId", "internshipId") ||
                References(value, applications, "applicationId") || References(value, opportunities, "opportunityId");
            Collect(root, "tasks", tasks, training);
            Collect(root, "taskExams", exams, v => training(v) || References(v, tasks, "taskId"));
            Collect(root, "weeklyReports", reports, training);
            Func<JToken, bool> related = value => training(value) || References(value, tasks, "taskId", "assignmentId") || References(value, exams, "examId") || References(value, reports, "reportId");
            var affectedStudents = Set(Rows(root, "placements").Where(p => placements.Contains(p.Name)).Select(p => Text(p.Value, "studentId")).ToArray());
            foreach (string branch in new[] { "opportunities", "applications", "placements", "tasks", "taskExams", "weeklyReports" })
                RemoveRows(root, branch, p => related(p.Value));
            foreach (string branch in new[] { "taskBoards", "companyFinalEvaluations", "academicFinalEvaluations", "finalEvaluationReviewsV2", "completionReviews", "completionDocuments", "finalEvaluations", "certificates", "completionConfirmations" })
                RemoveRows(root, branch, p => placements.Contains(p.Name) || students.Contains(p.Name) || uids.Contains(p.Name) || related(p.Value));
            foreach (string branch in new[] { "taskExamAttempts", "taskExamAnswerKeys", "submissions", "taskSubmissions", "files", "fileAttachments", "taskFiles", "examFiles", "weeklyReportReviews", "uimsAttendance/provisioningClaims", "academicFeedback", "academicHistory", "notifications", "placementClaims", "assignments", "supervisorAssignments" })
            {
                RemoveRows(root, branch, p => students.Contains(p.Name) || uids.Contains(p.Name) || placements.Contains(p.Name) || tasks.Contains(p.Name) || exams.Contains(p.Name) || reports.Contains(p.Name) || related(p.Value));
                RemoveDescendants(At(root, branch), related, students, uids);
            }

            var attendanceAssignments = Set();
            Collect(root, "uimsAttendance/trainingAssignments", attendanceAssignments, training);
            foreach (string branch in new[] { "uimsAttendance/trainingAssignments", "uimsAttendance/attendance", "uimsAttendance/schedules", "uimsAttendance/earlyCheckoutRequests", "attendance" })
                RemoveRows(root, branch, p => attendanceAssignments.Contains(p.Name) || related(p.Value) || References(p.Value, attendanceAssignments, "assignmentId"));
            RemoveRows(root, "uimsAttendance/students", p => students.Contains(p.Name) || uids.Contains(Text(p.Value, "firebaseUid")) || References(p.Value, attendanceAssignments, "activeAssignmentId"));
            foreach (string branch in new[] { "companySupervisors", "uimsAttendance/companySupervisors" })
                RemoveRows(root, branch, p => (students.Count == 0 && supervisors.Contains(p.Name)) || References(p.Value, uids, "firebaseUid") || References(p.Value, companies, "companyId"));
            RemoveRows(root, "uimsAttendance/companies", p => companies.Contains(p.Name));
            if (students.Count == 0)
                foreach (var company in Rows(root, "companies")) ClearReferences(company.Value, supervisors, "companySupervisorId", "supervisorUid", "ownerUid", "firebaseUid");
            foreach (var group in Rows(root, "groups"))
                foreach (string id in students) (group.Value["students"] as JObject)?.Remove(id);

            // A remaining student account must not keep a pointer/status for a removed placement.
            foreach (var user in Rows(root, "users"))
            {
                if (FirebaseSignInService.CanonicalRole(Text(user.Value, "role")) != "student" || uids.Contains(user.Name)) continue;
                var oldPlacements = Rows(root, "placements").Where(p => Text(p.Value, "studentId") == Text(user.Value, "studentId")).ToArray();
                bool hadRemovedPlacement = affectedStudents.Contains(Text(user.Value, "studentId")) ||
                    References(user.Value, placements, "placementId", "activePlacementId", "internshipId");
                if (hadRemovedPlacement && oldPlacements.Length == 0)
                    foreach (string field in new[] { "placementId", "activePlacementId", "internshipId", "companyId", "companyName", "companySupervisorId", "completionStatus", "confirmedAt", "disableAt", "certificateId" }) (user.Value as JObject)?.Remove(field);
            }
        }

        private static void RemoveDescendants(JToken value, Func<JToken, bool> match, HashSet<string> students, HashSet<string> uids)
        {
            var array = value as JArray;
            if (array != null) { foreach (var item in array.ToArray()) if (match(item)) item.Remove(); else RemoveDescendants(item, match, students, uids); return; }
            var obj = value as JObject; if (obj == null) return;
            foreach (var p in obj.Properties().ToArray())
            {
                if (!(p.Value is JObject) && !(p.Value is JArray)) continue;
                if (students.Contains(p.Name) || uids.Contains(p.Name) || match(p.Value)) p.Remove();
                else RemoveDescendants(p.Value, match, students, uids);
            }
        }

        private static void RemoveNestedReviews(JToken value, HashSet<string> ids)
        {
            var array = value as JArray;
            if (array != null)
            {
                foreach (var item in array.ToArray()) if (References(item, ids, "academicSupervisorId", "reviewerId", "reviewerUid", "authorUid")) item.Remove(); else RemoveNestedReviews(item, ids);
                return;
            }
            var obj = value as JObject; if (obj == null) return;
            foreach (var p in obj.Properties().ToArray())
                if (p.Value is JObject && References(p.Value, ids, "academicSupervisorId", "reviewerId", "reviewerUid", "authorUid")) p.Remove(); else RemoveNestedReviews(p.Value, ids);
        }
        private static void ClearReferences(JToken value, HashSet<string> ids, params string[] fields)
        { var obj = value as JObject; if (obj == null) return; foreach (string field in fields) if (ids.Contains(Text(obj, field))) obj.Remove(field); }
        private static string Text(JToken value, string field) { return value is JObject ? StudentManagementService.Text(value, field) : ""; }
        private static bool References(JToken value, HashSet<string> ids, params string[] fields) { return ids.Count > 0 && fields.Any(f => ids.Contains(Text(value, f))); }
        private static HashSet<string> Set(params string[] values) { return new HashSet<string>(values.Where(v => !string.IsNullOrEmpty(v)), StringComparer.Ordinal); }
        private static void Add(HashSet<string> values, string value) { if (!string.IsNullOrEmpty(value)) values.Add(value); }
        private static JToken At(JObject root, string path) { JToken value = root; foreach (string key in path.Split('/')) value = (value as JObject)?[key]; return value; }
        private static IEnumerable<JProperty> Rows(JObject root, string path) { return (At(root, path) as JObject ?? new JObject()).Properties().ToArray(); }
        private static void Collect(JObject root, string branch, HashSet<string> ids, Func<JToken, bool> match) { foreach (var row in Rows(root, branch)) if (match(row.Value)) { Add(ids, row.Name); Add(ids, Text(row.Value, "id")); } }
        private static void RemoveRows(JObject root, string branch, Func<JProperty, bool> match) { foreach (var row in Rows(root, branch)) if (match(row)) row.Remove(); }
    }
}
