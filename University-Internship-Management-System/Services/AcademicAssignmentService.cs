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
        private static string RawDepartment(JToken p) { return T(p, "majorId").Length > 0 ? T(p, "majorId") : T(p, "departmentId").Length > 0 ? T(p, "departmentId") : T(p, "department"); }
        public static string Department(JToken p) { return Normalize(RawDepartment(p)); }
        private static string Normalize(string value) { return string.Concat((value ?? "").Where(char.IsLetterOrDigit)).ToLowerInvariant(); }
        private static bool Active(JToken value) { return (bool?)value?["isActive"] != false && (bool?)value?["active"] != false && !new[] { "inactive", "archived", "ended", "reassigned", "disabled", "deleted" }.Contains(T(value, "status").ToLowerInvariant()); }
        private static bool ReadyProfile(JToken p) { return Active(p) && (bool?)p?["isActive"] == true && T(p, "deletionStatus") != "pending" && (T(p, "accountSetupStatus").Length == 0 || T(p, "accountSetupStatus") == "complete"); }
        private static string Name(JToken p) { return System.Text.RegularExpressions.Regex.Replace(T(p, "fullName"), @"\s*\(Demo\)\s*", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim(); }
        private static string GroupName(string value) { return System.Text.RegularExpressions.Regex.Replace(value ?? "", @"\s+", " ").Trim(); }
        private static bool DuplicateGroup(JObject users, JObject groups, string groupId, string name, string department, string supervisorId)
        {
            return groups.Properties().Any(g => g.Name != groupId && Active(g.Value) && string.Equals(GroupName(T(g.Value, "name")), name, StringComparison.OrdinalIgnoreCase)
                && Normalize(T(g.Value, "department")) == department && CanonicalSupervisor(users, T(g.Value, "academicSupervisorId")) == supervisorId);
        }
        private static string SupervisorId(JProperty p) { return T(p.Value, "academicSupervisorId").Length > 0 ? T(p.Value, "academicSupervisorId") : p.Name; }
        private static string CanonicalSupervisor(JObject users, string id)
        {
            var matches = users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "academicSupervisor" && (p.Name == id || SupervisorId(p) == id)).ToArray();
            return matches.Length == 1 ? SupervisorId(matches[0]) : id;
        }
        private static string[] Claims(JObject users, JObject groups, JObject assignments, JObject legacy, JProperty student)
        {
            string id = T(student.Value, "studentId");
            return assignments.Properties().Concat(legacy.Properties()).Where(a => Active(a.Value) && T(a.Value, "studentId") == id).Select(a => T(a.Value, "academicSupervisorId"))
                .Concat(groups.Properties().Where(g => Active(g.Value) && (bool?)g.Value["students"]?[id] == true).Select(g => T(g.Value, "academicSupervisorId")))
                .Concat(new[] { T(student.Value, "academicSupervisorId") }).Where(x => x.Length > 0).Select(x => CanonicalSupervisor(users, x)).Distinct().ToArray();
        }
        private static string[] ResponsibleDepartments(JObject users, JObject groups, JObject assignments, JObject legacy, JProperty professor)
        {
            string id = SupervisorId(professor);
            return new[] { Department(professor.Value), Normalize(T(professor.Value, "assignedMajorId")) }
                .Concat(groups.Properties().Where(g => Active(g.Value) && CanonicalSupervisor(users, T(g.Value, "academicSupervisorId")) == id).Select(g => Normalize(T(g.Value, "department"))))
                .Concat(users.Properties().Where(s => Active(s.Value) && FirebaseSignInService.CanonicalRole(T(s.Value, "role")) == "student" && Claims(users, groups, assignments, legacy, s).Contains(id)).Select(s => Department(s.Value)))
                .Where(d => d.Length > 0).Distinct().ToArray();
        }
        // Shared by the monitor's roster and its detail/feedback authorization checks.
        public static string[] AssignedStudentIds(string supervisorUid, JObject users, JObject groups, JObject assignments, JObject legacy, JObject placements = null)
        {
            var professor = users.Properties().FirstOrDefault(p => p.Name == supervisorUid && ReadyProfile(p.Value) && FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "academicSupervisor");
            if (professor == null) return new string[0];
            string id = SupervisorId(professor);
            return users.Properties().Where(p => ReadyProfile(p.Value) && FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "student" && T(p.Value, "studentId").Length > 0)
                .Where(p => {
                    string studentId = T(p.Value, "studentId");
                    var placementClaims = (placements ?? new JObject()).Properties()
                        .Where(x => Active(x.Value) && new[] { "active", "confirmed", "completed" }.Contains(T(x.Value, "status").ToLowerInvariant())
                            && (T(x.Value, "studentId") == studentId || T(x.Value, "studentProfileId") == studentId)
                            && (T(x.Value, "studentUid").Length == 0 || T(x.Value, "studentUid") == p.Name))
                        .Select(x => CanonicalSupervisor(users, T(x.Value, "academicSupervisorId")));
                    var claims = Claims(users, groups, assignments, legacy, p).Concat(placementClaims).Where(x => x.Length > 0).Distinct().ToArray();
                    return claims.Length == 1 && claims[0] == id;
                })
                .Select(p => T(p.Value, "studentId")).Distinct().ToArray();
        }
        private async Task Authorize(string uid)
        {
            if (!await activeAccount(Key(uid))) throw new FirebaseException(401, "Sign in with an active account.");
            var profile = await db.ReadAsync("users/" + uid);
            try { if (FirebaseSignInService.ProfileResult(uid, O(profile)).Role != "coordinator") throw new FirebaseException(403, "Coordinator access is required."); }
            catch (FirebaseLoginException) { throw new FirebaseException(403, "An active, fully configured coordinator profile is required."); }
        }
        public async Task<JObject> LoadAsync(string uid)
        {
            await Authorize(uid);
            var usersTask = db.ReadAsync("users"); var groupsTask = db.ReadAsync("groups"); var assignmentsTask = db.ReadAsync("supervisorAssignments"); var legacyTask = db.ReadAsync("assignments"); var placementsTask = db.ReadAsync("placements"); var recordsTask = db.ReadAsync("universityRecords/academicSupervisors");
            await Task.WhenAll(usersTask, groupsTask, assignmentsTask, legacyTask, placementsTask, recordsTask);
            var records = O(recordsTask.Result);
            var users = O(usersTask.Result); var groups = O(groupsTask.Result); var assignments = O(assignmentsTask.Result); var legacy = O(legacyTask.Result); var placements = O(placementsTask.Result);
            var professors = new JArray(); var students = new JArray(); var departments = new JObject();
            foreach (var p in users.Properties().Where(p => ReadyProfile(p.Value)))
            {
                string role = FirebaseSignInService.CanonicalRole(T(p.Value, "role")), department = Department(p.Value);
                if (role != "student" && role != "academicSupervisor") continue;
                if (department.Length > 0) departments[department] = StudentManagementService.DisplayName(RawDepartment(p.Value));
                if (role == "academicSupervisor")
                {
                    var majors = ResponsibleDepartments(users, groups, assignments, legacy, p);
                    professors.Add(new JObject { ["id"] = SupervisorId(p), ["uid"] = p.Name, ["name"] = Name(p.Value), ["department"] = majors.Length == 1 ? majors[0] : "", ["conflict"] = majors.Length > 1, ["official"] = AcademicSupervisorService.Official(p.Value, records), ["universityId"] = T(p.Value, "universityId"), ["collegeId"] = T(p.Value, "collegeId") });
                }
                else if (department.Length > 0 && T(p.Value, "studentId").Length > 0)
                {
                    string studentId = T(p.Value, "studentId");
                    var memberships = groups.Properties().Where(g => Active(g.Value) && (bool?)g.Value["students"]?[studentId] == true).Select(g => g.Name).ToArray();
                    var ids = Claims(users, groups, assignments, legacy, p).Concat(placements.Properties().Where(x => Active(x.Value) && T(x.Value, "status") != "completed" && (T(x.Value, "studentId") == studentId || T(x.Value, "studentProfileId") == studentId)).Select(x => CanonicalSupervisor(users, T(x.Value, "academicSupervisorId")))).Where(x => x.Length > 0).Distinct().ToArray();
                    bool duplicate = users.Properties().Count(x => Active(x.Value) && FirebaseSignInService.CanonicalRole(T(x.Value, "role")) == "student" && T(x.Value, "studentId") == studentId) != 1;
                    students.Add(new JObject { ["id"] = studentId, ["name"] = Name(p.Value), ["department"] = department, ["universityId"] = T(p.Value, "universityId"), ["collegeId"] = T(p.Value, "collegeId"), ["groupIds"] = new JArray(memberships), ["supervisorIds"] = new JArray(ids), ["conflict"] = duplicate || ids.Length > 1 || memberships.Length > 1 });
                }
            }
            var rows = new JArray(groups.Properties().Where(g => Active(g.Value)).Select(g => new JObject { ["id"] = g.Name, ["name"] = T(g.Value, "name").Length > 0 ? T(g.Value, "name") : g.Name, ["department"] = Normalize(T(g.Value, "department")), ["supervisorId"] = CanonicalSupervisor(users, T(g.Value, "academicSupervisorId")), ["students"] = g.Value["students"]?.DeepClone() ?? new JObject() }));
            return new JObject { ["departments"] = departments, ["professors"] = professors, ["students"] = students, ["groups"] = rows };
        }
        public async Task<JObject> RemoveAsync(string uid, JObject input)
        {
            await Authorize(uid);
            string groupId = Key(T(input, "groupId")), studentId = Key(T(input, "studentId"));
            var pending = await db.ReadAsync("academicAssignmentOperations/current");
            if (pending != null && pending.Type != JTokenType.Null) throw new FirebaseException(409, "Another assignment is pending. Retry after it completes.");
            var users = O(await db.ReadAsync("users")); var groups = O(await db.ReadAsync("groups")); var assignments = O(await db.ReadAsync("supervisorAssignments")); var legacy = O(await db.ReadAsync("assignments")); var placements = O(await db.ReadAsync("placements"));
            var group = groups[groupId] as JObject;
            if (group == null || !Active(group)) throw new FirebaseException(404, "This group no longer exists. Refresh the groups list.");
            if ((bool?)group["students"]?[studentId] != true) throw new FirebaseException(409, "This student is no longer in the group. Refresh the groups list.");
            string supervisorId = CanonicalSupervisor(users, T(group, "academicSupervisorId")), now = DateTimeOffset.UtcNow.ToString("o");
            Func<JToken, bool> sameSupervisor = value => T(value, "academicSupervisorId").Length > 0 && CanonicalSupervisor(users, T(value, "academicSupervisorId")) == supervisorId;
            var update = new JObject();
            update["groups/" + groupId + "/students/" + studentId] = JValue.CreateNull();
            update["groups/" + groupId + "/updatedAt"] = now;
            // Retire the supervisor links this group created; other supervisors' records stay untouched.
            foreach (var pair in new[] { new { Root = "supervisorAssignments", Rows = assignments }, new { Root = "assignments", Rows = legacy } })
                foreach (var link in pair.Rows.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId && sameSupervisor(a.Value)))
                {
                    update[pair.Root + "/" + Key(link.Name) + "/active"] = false; update[pair.Root + "/" + Key(link.Name) + "/isActive"] = false;
                    update[pair.Root + "/" + Key(link.Name) + "/removedBy"] = uid; update[pair.Root + "/" + Key(link.Name) + "/removedAt"] = now;
                }
            foreach (var student in users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "student" && T(p.Value, "studentId") == studentId && sameSupervisor(p.Value)))
                update["users/" + Key(student.Name) + "/academicSupervisorId"] = JValue.CreateNull();
            foreach (var placement in placements.Properties().Where(x => Active(x.Value) && T(x.Value, "status") != "completed" && (T(x.Value, "studentId") == studentId || T(x.Value, "studentProfileId") == studentId) && sameSupervisor(x.Value)))
                update["placements/" + Key(placement.Name) + "/academicSupervisorId"] = JValue.CreateNull();
            await Authorize(uid);
            // Atomic multi-location PATCH, matching SaveAsync.
            await db.PatchAsync("", update);
            Notify.Send(Notify.Student(studentId), "Supervision group changed", "You were removed from " + (T(group, "name").Length > 0 ? T(group, "name") : "your supervision group") + ". The coordinator will assign your academic supervisor.", "Student/StudentDashboard.aspx", "assignment");
            return new JObject { ["removed"] = true, ["groupId"] = groupId, ["studentId"] = studentId };
        }
        public async Task<JObject> SaveAsync(string uid, JObject input)
        {
            await Authorize(uid);
            string requestId = Key(T(input, "requestId"));
            if (requestId == "current") throw new FirebaseException(400, "Reserved request identifier.");
            if (!(input["studentIds"] is JArray)) throw new FirebaseException(400, "Select students from the list.");
            var selected = input["studentIds"] as JArray ?? new JArray();
            if (selected.Any(x => x.Type != JTokenType.String) || selected.Count > 200 || selected.Select(x => (string)x).Distinct().Count() != selected.Count) throw new FirebaseException(400, "Choose up to 200 distinct students.");
            var canonical = new JObject { ["actor"] = uid, ["groupId"] = T(input, "groupId"), ["name"] = GroupName(T(input, "name")), ["department"] = Normalize(T(input, "department")), ["supervisorId"] = T(input, "supervisorId"), ["studentIds"] = new JArray(selected.Select(x => Key((string)x)).OrderBy(x => x, StringComparer.Ordinal)) };
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
                if (group != null && !Active(group)) throw new FirebaseException(409, "This group is inactive. Refresh the groups list.");
                if (group != null && Normalize(T(group, "department")) != department) throw new FirebaseException(400, "The group's department cannot be changed during assignment.");
                if (department.Length == 0) throw new FirebaseException(400, "Select the department.");
                var professorRows = users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "academicSupervisor" && SupervisorId(p) == supervisorId && ReadyProfile(p.Value)).ToArray();
                if (professorRows.Length != 1) throw new FirebaseException(400, "Choose an active academic supervisor.");
                var professor = professorRows[0];
                if (!AcademicSupervisorService.Official(professor.Value, O(await db.ReadAsync("universityRecords/academicSupervisors")))) throw new FirebaseException(400, "This supervisor account is not in university records, so it cannot receive students. Delete it from Manage Academic Supervisors and create it from the official staff ID.");
                if (ResponsibleDepartments(users, groups, assignments, legacy, professor).Any(d => d != department)) throw new FirebaseException(400, "This supervisor is already responsible for another major. One supervisor can supervise only one major.");
                var members = O(group?["students"]).Properties().Where(p => (bool?)p.Value == true).Select(p => p.Name).Union(selected.Select(x => (string)x)).ToArray();
                if (members.Length == 0) throw new FirebaseException(400, "Select at least one student.");
                if (group == null && T(canonical, "name").Length > 0 && DuplicateGroup(users, groups, groupId, T(canonical, "name"), department, supervisorId)) throw new FirebaseException(409, "This supervisor already has a group with this name in this major. Open that group and add the students there.");
                if (group != null && T(group, "academicSupervisorId").Length > 0 && CanonicalSupervisor(users, T(group, "academicSupervisorId")) != supervisorId) throw new FirebaseException(409, "This group already has a professor. Reassignment requires a separate explicit review.");
                var update = new JObject(); string now = DateTimeOffset.UtcNow.ToString("o"); int added = 0;
                foreach (string studentId in members)
                {
                    Key(studentId);
                    var profiles = users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "student" && T(p.Value, "studentId") == studentId && ReadyProfile(p.Value)).ToArray();
                    if (profiles.Length != 1 || Department(profiles[0].Value) != department) throw new FirebaseException(400, "A selected or existing group student does not belong to this department or has an invalid account.");
                    var student = profiles[0];
                    foreach (string field in new[] { "universityId", "collegeId" }) if (T(student.Value, field).Length > 0 && T(professor.Value, field).Length > 0 && T(student.Value, field) != T(professor.Value, field)) throw new FirebaseException(400, "Student and professor university/college do not match.");
                    if (groups.Properties().Any(g => g.Name != groupId && Active(g.Value) && (bool?)g.Value["students"]?[studentId] == true)) throw new FirebaseException(409, "A selected student already belongs to another group.");
                    var links = assignments.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId).Concat(legacy.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId)).ToArray();
                    var ownedPlacements = placements.Properties().Where(p => Active(p.Value) && T(p.Value, "status") != "completed" && (T(p.Value, "studentId") == studentId || T(p.Value, "studentProfileId") == studentId)).ToArray();
                    if (links.Any(a => T(a.Value, "academicSupervisorId").Length > 0 && CanonicalSupervisor(users, T(a.Value, "academicSupervisorId")) != supervisorId) || (T(student.Value, "academicSupervisorId").Length > 0 && CanonicalSupervisor(users, T(student.Value, "academicSupervisorId")) != supervisorId) || ownedPlacements.Any(p => T(p.Value, "academicSupervisorId").Length > 0 && CanonicalSupervisor(users, T(p.Value, "academicSupervisorId")) != supervisorId)) throw new FirebaseException(409, "A selected student already has another professor. Existing assignments were not overwritten.");
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
                update["users/" + professor.Name + "/assignedMajorId"] = department;
                if (group == null) update["groups/" + groupId + "/createdAt"] = now;
                update[operationPath] = new JObject { ["requestId"] = requestId, ["digest"] = digest, ["actorUid"] = uid, ["groupId"] = groupId, ["status"] = "complete", ["at"] = now, ["studentIds"] = selected.DeepClone() };
                update["academicAssignmentOperations/current"] = JValue.CreateNull();
                await Authorize(uid); patchAttempted = true;
                // Atomic multi-location PATCH; never a root PUT or independent per-student writes.
                await db.PatchAsync("", update);
                foreach (var studentId in selected.Select(x => (string)x))
                    Notify.Send(Notify.Student(studentId), "Academic supervisor assigned", Name(professor.Value) + " is now your academic supervisor (" + name + ").", "Student/StudentDashboard.aspx", "assignment");
                Notify.Send(Notify.User(professor.Name), "Students added to your group", added + " student(s) were added to " + name + ".", "Academic/StudentProgressMonitor.aspx", "assignment");
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
        // Rename or delete a whole group. Every change is one atomic
        // multi-location PATCH, so the group and the student's supervisor links stay in sync.
        public async Task<JObject> ManageAsync(string uid, JObject input)
        {
            await Authorize(uid);
            string action = T(input, "action"), groupId = Key(T(input, "groupId"));
            if (O(await db.ReadAsync("academicAssignmentOperations/current")).Count > 0) throw new FirebaseException(409, "Another assignment is still saving. Refresh and retry.");
            var users = O(await db.ReadAsync("users")); var groups = O(await db.ReadAsync("groups")); var assignments = O(await db.ReadAsync("supervisorAssignments")); var legacy = O(await db.ReadAsync("assignments")); var placements = O(await db.ReadAsync("placements"));
            var group = groups[groupId] as JObject;
            if (group == null || !Active(group)) throw new FirebaseException(404, "This group no longer exists. Refresh the groups list.");
            string supervisorId = CanonicalSupervisor(users, T(group, "academicSupervisorId")), department = Normalize(T(group, "department")), now = DateTimeOffset.UtcNow.ToString("o");
            var update = new JObject(); string message; string[] notify = new string[0];
            if (action == "rename")
            {
                string name = GroupName(T(input, "name"));
                if (name.Length == 0 || name.Length > 120) throw new FirebaseException(400, "Enter a group name of up to 120 characters.");
                if (DuplicateGroup(users, groups, groupId, name, department, supervisorId)) throw new FirebaseException(409, "This supervisor already has another group with this name in this major.");
                update["groups/" + groupId + "/name"] = name; update["groups/" + groupId + "/updatedAt"] = now;
                message = "Group name saved.";
            }
            else if (action == "deleteGroup")
            {
                var members = O(group["students"]).Properties().Where(p => p.Value.Type == JTokenType.Boolean && (bool)p.Value).Select(p => Key(p.Name)).ToArray();
                update["groups/" + groupId] = JValue.CreateNull();
                Func<JToken, bool> sameSupervisor = value => supervisorId.Length > 0 && T(value, "academicSupervisorId").Length > 0 && CanonicalSupervisor(users, T(value, "academicSupervisorId")) == supervisorId;
                foreach (string studentId in members)
                {
                    // Keep the supervisor link when the student is still in another group of the same supervisor.
                    if (supervisorId.Length == 0 || groups.Properties().Any(g => g.Name != groupId && Active(g.Value) && (bool?)g.Value["students"]?[studentId] == true && CanonicalSupervisor(users, T(g.Value, "academicSupervisorId")) == supervisorId)) continue;
                    // Same retirement as RemoveAsync: deactivate links, clear the student's and placement's supervisor.
                    foreach (var pair in new[] { new { Root = "supervisorAssignments", Rows = assignments }, new { Root = "assignments", Rows = legacy } })
                        foreach (var link in pair.Rows.Properties().Where(a => Active(a.Value) && T(a.Value, "studentId") == studentId && sameSupervisor(a.Value)))
                        {
                            update[pair.Root + "/" + Key(link.Name) + "/active"] = false; update[pair.Root + "/" + Key(link.Name) + "/isActive"] = false;
                            update[pair.Root + "/" + Key(link.Name) + "/removedBy"] = uid; update[pair.Root + "/" + Key(link.Name) + "/removedAt"] = now;
                        }
                    foreach (var student in users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "student" && T(p.Value, "studentId") == studentId && sameSupervisor(p.Value)))
                        update["users/" + Key(student.Name) + "/academicSupervisorId"] = JValue.CreateNull();
                    foreach (var placement in placements.Properties().Where(x => Active(x.Value) && T(x.Value, "status") != "completed" && (T(x.Value, "studentId") == studentId || T(x.Value, "studentProfileId") == studentId) && sameSupervisor(x.Value)))
                        update["placements/" + Key(placement.Name) + "/academicSupervisorId"] = JValue.CreateNull();
                }
                // A supervisor with no remaining group no longer owns a major through groups.
                if (supervisorId.Length > 0 && !groups.Properties().Any(g => g.Name != groupId && Active(g.Value) && CanonicalSupervisor(users, T(g.Value, "academicSupervisorId")) == supervisorId))
                    foreach (var prof in users.Properties().Where(p => FirebaseSignInService.CanonicalRole(T(p.Value, "role")) == "academicSupervisor" && SupervisorId(p) == supervisorId && T(p.Value, "assignedMajorId").Length > 0))
                        update["users/" + Key(prof.Name) + "/assignedMajorId"] = JValue.CreateNull();
                notify = members;
                message = members.Length == 0 ? "Group deleted." : "Group deleted. Its " + members.Length + " student(s) are no longer linked to this supervisor.";
            }
            else throw new FirebaseException(400, "Unsupported group action.");
            await Authorize(uid);
            await db.PatchAsync("", update);
            foreach (string studentId in notify)
                Notify.Send(Notify.Student(studentId), "Supervision group changed", "Your supervision group " + (T(group, "name").Length > 0 ? T(group, "name") : groupId) + " was deleted. The coordinator will assign your academic supervisor.", "Student/StudentDashboard.aspx", "assignment");
            return new JObject { ["saved"] = true, ["groupId"] = groupId, ["message"] = message };
        }
    }
}
