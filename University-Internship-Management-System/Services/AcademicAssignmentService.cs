using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    public sealed class AcademicAssignmentService
    {
        private readonly IStudentDatabase db;
        private readonly Func<string, Task<bool>> activeAccount;
        public AcademicAssignmentService() { var gateway = new StudentFirebaseGateway(); db = gateway; activeAccount = gateway.IsActiveAuthenticationUidAsync; }
        public AcademicAssignmentService(IStudentDatabase database, Func<string, Task<bool>> account) { db = database; activeAccount = account; }
        private static JObject O(JToken value) { return value as JObject ?? new JObject(); }
        private static string T(JToken value, string key) { return (string)value?[key] ?? ""; }
        private static string Key(string value) { if (string.IsNullOrWhiteSpace(value) || value.Length > 180 || value.Any(c => ".#$[]/\\".Contains(c) || char.IsControl(c))) throw new FirebaseException(400, "Invalid record identifier."); return value; }
        public static string Department(JToken p) { return Normalize(T(p, "departmentId").Length > 0 ? T(p, "departmentId") : T(p, "majorId").Length > 0 ? T(p, "majorId") : T(p, "department")); }
        private static string Normalize(string value) { return string.Concat((value ?? "").Where(char.IsLetterOrDigit)).ToLowerInvariant(); }
        private static bool Active(JToken value) { return (bool?)value?["isActive"] != false && (bool?)value?["active"] != false && !new[] { "inactive", "archived", "ended", "reassigned", "disabled", "deleted" }.Contains(T(value, "status").ToLowerInvariant()); }
        private static string Name(JToken p) { return System.Text.RegularExpressions.Regex.Replace(T(p, "fullName"), @"\s*\(Demo\)\s*", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim(); }
        private static string SupervisorId(JProperty p) { return T(p.Value, "academicSupervisorId").Length > 0 ? T(p.Value, "academicSupervisorId") : p.Name; }
        private async Task Authorize(string uid)
        {
            if (!await activeAccount(Key(uid))) throw new FirebaseException(401, "Sign in with an active account.");
            var profile = await db.ReadAsync("users/" + uid);
            if (FirebaseSignInService.ProfileResult(uid, O(profile)).Role != "coordinator") throw new FirebaseException(403, "Coordinator access is required.");
        }
        public async Task<JObject> LoadAsync(string uid)
        {
            await Authorize(uid);
            var usersTask = db.ReadAsync("users"); var groupsTask = db.ReadAsync("groups"); var assignmentsTask = db.ReadAsync("supervisorAssignments");
            await Task.WhenAll(usersTask, groupsTask, assignmentsTask);
            var users = O(usersTask.Result); var groups = O(groupsTask.Result); var assignments = O(assignmentsTask.Result);
            var professors = new JArray(); var students = new JArray(); var departments = new JObject();
            foreach (var p in users.Properties().Where(p => Active(p.Value)))
            {
                string role = FirebaseSignInService.CanonicalRole(T(p.Value, "role")), department = Department(p.Value);
                if (department.Length == 0 || (role != "student" && role != "academicSupervisor")) continue;
                string raw = T(p.Value, "departmentId"); if (raw.Length == 0) raw = T(p.Value, "majorId"); if (raw.Length == 0) raw = T(p.Value, "department");
                departments[department] = StudentManagementService.DisplayName(raw);
                if (role == "academicSupervisor") professors.Add(new JObject { ["id"] = SupervisorId(p), ["uid"] = p.Name, ["name"] = Name(p.Value), ["department"] = department });
                else if (T(p.Value, "studentId").Length > 0)
                {
                    string studentId = T(p.Value, "studentId");
                    var memberships = groups.Properties().Where(g => Active(g.Value) && (bool?)g.Value["students"]?[studentId] == true).Select(g => g.Name).ToArray();
                    var ids = assignments.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId).Select(a => T(a.Value, "academicSupervisorId")).Where(x => x.Length > 0).Concat(new[] { T(p.Value, "academicSupervisorId") }.Where(x => x.Length > 0)).Distinct().ToArray();
                    students.Add(new JObject { ["id"] = studentId, ["name"] = Name(p.Value), ["department"] = department, ["groupIds"] = new JArray(memberships), ["supervisorIds"] = new JArray(ids), ["conflict"] = ids.Length > 1 || memberships.Length > 1 });
                }
            }
            var rows = new JArray(groups.Properties().Where(g => Active(g.Value)).Select(g => new JObject { ["id"] = g.Name, ["name"] = T(g.Value, "name").Length > 0 ? T(g.Value, "name") : g.Name, ["department"] = Normalize(T(g.Value, "department")), ["supervisorId"] = g.Value["academicSupervisorId"], ["students"] = g.Value["students"]?.DeepClone() ?? new JObject() }));
            return new JObject { ["departments"] = departments, ["professors"] = professors, ["students"] = students, ["groups"] = rows };
        }
        public async Task<JObject> SaveAsync(string uid, JObject input)
        {
            await Authorize(uid);
            string requestId = Key(T(input, "requestId"));
            var selected = input["studentIds"] as JArray ?? new JArray();
            if (selected.Any(x => x.Type != JTokenType.String) || selected.Count > 200 || selected.Select(x => (string)x).Distinct().Count() != selected.Count) throw new FirebaseException(400, "Choose up to 200 distinct students.");
            var canonical = new JObject { ["actor"] = uid, ["groupId"] = T(input, "groupId"), ["name"] = T(input, "name").Trim(), ["department"] = Normalize(T(input, "department")), ["supervisorId"] = T(input, "supervisorId"), ["studentIds"] = new JArray(selected.Select(x => Key((string)x)).OrderBy(x => x, StringComparer.Ordinal)) };
            string digest; using (var sha = SHA256.Create()) digest = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString(Formatting.None))));
            string operationPath = "academicAssignmentOperations/" + requestId;
            var operation = O(await db.ReadAsync(operationPath));
            if (operation.Count > 0 && T(operation, "digest") != digest) throw new FirebaseException(409, "This request ID was already used for different assignment content.");
            if (T(operation, "status") == "complete") return new JObject { ["groupId"] = operation["groupId"], ["saved"] = true };
            string groupId = T(canonical, "groupId"); if (groupId.Length == 0) groupId = "GRP-" + requestId; Key(groupId);
            var lockVersion = await db.ReadVersionAsync("academicAssignmentOperations/current");
            if (lockVersion.Value != null && T(lockVersion.Value, "requestId") != requestId) throw new FirebaseException(409, "Another assignment is pending. Complete/retry its original request before starting another one.");
            bool locked = lockVersion.Value != null;
            if (!locked) locked = await db.CompareWriteAsync("academicAssignmentOperations/current", new JObject { ["requestId"] = requestId, ["actorUid"] = uid, ["digest"] = digest }, lockVersion.ETag);
            if (!locked) throw new FirebaseException(409, "Another coordinator is saving assignments. Refresh and retry.");
            var currentLock = O(await db.ReadAsync("academicAssignmentOperations/current"));
            if (T(currentLock, "digest") != digest || T(currentLock, "actorUid") != uid) throw new FirebaseException(409, "The pending operation belongs to different content or another account.");
            bool patchAttempted = false;
            try
            {
                var users = O(await db.ReadAsync("users")); var groups = O(await db.ReadAsync("groups")); var assignments = O(await db.ReadAsync("supervisorAssignments")); var legacy = O(await db.ReadAsync("assignments")); var placements = O(await db.ReadAsync("placements"));
                var group = groups[groupId] as JObject; string department = T(canonical, "department"), supervisorId = T(canonical, "supervisorId");
                if (group != null && Normalize(T(group, "department")) != department) throw new FirebaseException(400, "The group's department cannot be changed during assignment.");
                if (department.Length == 0) throw new FirebaseException(400, "Select the department.");
                var professorRows = users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "academicSupervisor" && SupervisorId(p) == supervisorId && Active(p.Value)).ToArray();
                if (professorRows.Length != 1 || Department(professorRows[0].Value) != department) throw new FirebaseException(400, "Choose an active professor from the group's department.");
                var professor = professorRows[0];
                var members = O(group?["students"]).Properties().Where(p => (bool?)p.Value == true).Select(p => p.Name).Union(selected.Select(x => (string)x)).ToArray();
                if (members.Length == 0) throw new FirebaseException(400, "Select at least one student.");
                if (group != null && T(group, "academicSupervisorId").Length > 0 && T(group, "academicSupervisorId") != supervisorId) throw new FirebaseException(409, "This group already has a professor. Reassignment requires a separate explicit review.");
                var update = new JObject(); string now = DateTimeOffset.UtcNow.ToString("o"); int added = 0;
                foreach (string studentId in members)
                {
                    Key(studentId);
                    var profiles = users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "student" && T(p.Value, "studentId") == studentId && Active(p.Value)).ToArray();
                    if (profiles.Length != 1 || Department(profiles[0].Value) != department) throw new FirebaseException(400, "A selected or existing group student does not belong to this department or has an invalid account.");
                    var student = profiles[0];
                    foreach (string field in new[] { "universityId", "collegeId" }) if (T(student.Value, field).Length > 0 && T(professor.Value, field).Length > 0 && T(student.Value, field) != T(professor.Value, field)) throw new FirebaseException(400, "Student and professor university/college do not match.");
                    if (groups.Properties().Any(g => g.Name != groupId && Active(g.Value) && (bool?)g.Value["students"]?[studentId] == true)) throw new FirebaseException(409, "A selected student already belongs to another group.");
                    var links = assignments.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId).Concat(legacy.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId)).ToArray();
                    var ownedPlacements = placements.Properties().Where(p => T(p.Value, "studentId") == studentId || T(p.Value, "studentProfileId") == studentId).ToArray();
                    if (links.Any(a => T(a.Value, "academicSupervisorId").Length > 0 && T(a.Value, "academicSupervisorId") != supervisorId) || (T(student.Value, "academicSupervisorId").Length > 0 && T(student.Value, "academicSupervisorId") != supervisorId) || ownedPlacements.Any(p => T(p.Value, "academicSupervisorId").Length > 0 && T(p.Value, "academicSupervisorId") != supervisorId)) throw new FirebaseException(409, "A selected student already has another professor. Existing assignments were not overwritten.");
                    if ((bool?)group?["students"]?[studentId] == true && T(group, "academicSupervisorId") == supervisorId && T(student.Value, "academicSupervisorId") == supervisorId && links.Length > 0 && ownedPlacements.All(p => T(p.Value, "status") == "completed" || T(p.Value, "academicSupervisorId") == supervisorId)) continue;
                    added++;
                    string linkId = assignments.Properties().Where(a => T(a.Value, "studentId") == studentId && Active(a.Value)).Select(a => a.Name).FirstOrDefault() ?? "ASG-" + studentId;
                    var link = O(assignments[linkId]); link = (JObject)link.DeepClone(); link["id"] = linkId; link["studentId"] = studentId; link["academicSupervisorId"] = supervisorId; link["active"] = true; link["isActive"] = true; link["groupId"] = groupId; link["department"] = department; link["assignedBy"] = uid; link["assignedAt"] = now;
                    update["supervisorAssignments/" + Key(linkId)] = link;
                    update["users/" + student.Name + "/academicSupervisorId"] = supervisorId;
                    foreach (var p in ownedPlacements) if (T(p.Value, "status") != "completed") update["placements/" + p.Name + "/academicSupervisorId"] = supervisorId;
                    update["groups/" + groupId + "/students/" + studentId] = true;
                }
                if (added == 0) throw new FirebaseException(409, "These students are already assigned to this group. No duplicate assignments were created.");
                string name = T(canonical, "name"); if (name.Length == 0) name = groupId; if (name.Length > 120) throw new FirebaseException(400, "Group name is limited to 120 characters.");
                update["groups/" + groupId + "/name"] = name; update["groups/" + groupId + "/department"] = department; update["groups/" + groupId + "/academicSupervisorId"] = supervisorId; update["groups/" + groupId + "/updatedAt"] = now;
                if (group == null) update["groups/" + groupId + "/createdAt"] = now;
                update[operationPath] = new JObject { ["requestId"] = requestId, ["digest"] = digest, ["actorUid"] = uid, ["groupId"] = groupId, ["status"] = "complete", ["at"] = now, ["studentIds"] = selected.DeepClone() };
                update["academicAssignmentOperations/current"] = JValue.CreateNull();
                await Authorize(uid); patchAttempted = true;
                // Atomic multi-location PATCH; never a root PUT or independent per-student writes.
                await db.PatchAsync("", update);
                return new JObject { ["saved"] = true, ["groupId"] = groupId, ["added"] = added };
            }
            catch
            {
                var persisted = O(await db.ReadAsync(operationPath));
                if (T(persisted, "digest") == digest && T(persisted, "status") == "complete") return new JObject { ["saved"] = true, ["groupId"] = persisted["groupId"] };
                // Before a commit attempt, release only our reservation. After an ambiguous
                // network failure keep it reserved so retrying the same request reconciles safely.
                if (!patchAttempted) { var own = await db.ReadVersionAsync("academicAssignmentOperations/current"); if (T(own.Value, "requestId") == requestId && T(own.Value, "digest") == digest) await db.CompareWriteAsync("academicAssignmentOperations/current", null, own.ETag); }
                throw;
            }
        }
    }
}
