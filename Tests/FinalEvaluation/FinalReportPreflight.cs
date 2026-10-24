using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;

internal static partial class Program
{
    private static async Task<int> FinalReportPreflight(string configPath)
    {
        try
        {
            var xml = new XmlDocument(); xml.Load(configPath); var values = xml.SelectNodes("/configuration/appSettings/add").Cast<XmlElement>().ToDictionary(x => x.GetAttribute("key"), x => x.GetAttribute("value"));
            string overrideName = xml.DocumentElement["appSettings"]?.GetAttribute("file");
            if (!string.IsNullOrWhiteSpace(overrideName))
            {
                string overridePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configPath)), overrideName);
                if (File.Exists(overridePath)) { var local = new XmlDocument(); local.Load(overridePath); foreach (XmlElement entry in local.SelectNodes("/appSettings/add")) values[entry.GetAttribute("key")] = entry.GetAttribute("value"); }
            }
            Func<string, string> setting = key => values.ContainsKey(key) ? values[key] : null;
            foreach (string flag in new[] { "FirebaseEnabled", "FirebaseAuthenticationEnabled", "FirebaseLiveWritesEnabled", "FirebaseSchemaValidated", "AttendanceEnabled", "AttendanceSchemaValidated", "FinalReportWorkflowEnabled", "FinalReportWorkflowSchemaValidated", "FinalReportWorkflowWritesEnabled" }) Console.WriteLine(flag + "=" + (setting(flag) ?? "absent/disabled"));
            string credentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(credentialPath) || !File.Exists(credentialPath)) { Console.WriteLine("BLOCKED: service-account environment path is unavailable to this process."); return 2; }
            var credential = JObject.Parse(File.ReadAllText(credentialPath));
            if ((string)credential["project_id"] != setting("FirebaseProjectId")) { Console.WriteLine("BLOCKED: configured project and service account do not match. No network requests made."); return 2; }
            var config = new FirebaseConfiguration(setting("FirebaseEnabled"), setting("FirebaseProjectId"), setting("FirebaseDatabaseUrl"), setting("FirebaseApiKey"), setting("FirebaseLiveWritesEnabled"), setting("FirebaseTaskSchemaValidated"), setting("FirebaseSchemaValidated"), setting("FirebaseAuthenticationEnabled"));
            config.EnsureEnabled();
            var gateway = new StudentFirebaseGateway(config);
            var placements = await gateway.ReadAsync("placements") as JObject ?? new JObject();
            var assignments = await gateway.ReadAsync("uimsAttendance/trainingAssignments") as JObject ?? new JObject();
            int linked = placements.Properties().Count(p => !string.IsNullOrWhiteSpace((string)p.Value["trainingAssignmentId"]) || !string.IsNullOrWhiteSpace((string)p.Value["attendanceAssignmentId"]) || assignments.Properties().Count(a => (string)a.Value["placementId"] == p.Name) == 1);
            Console.WriteLine("PASS: actual configured Firebase GET succeeded. Placements=" + placements.Count + "; attendance assignments=" + assignments.Count + "; explicit placement-attendance links=" + linked + ". No records written.");
            var rules = await gateway.ReadAsync(".settings/rules") as JObject;
            var rootRules = rules?["rules"] as JObject;
            Console.WriteLine("Rules read: root read denied=" + ((bool?)rootRules?[".read"] == false) + "; root write denied=" + ((bool?)rootRules?[".write"] == false) + "; workflow rules node present=" + (rootRules?["finalEvaluationWorkflows"] != null) + "; assignment index present=" + (rootRules?["uimsAttendance"]?["trainingAssignments"]?[".indexOn"]?.ToString().Contains("placementId") == true));
            bool privateRules = rootRules != null && (bool?)rootRules[".read"] == false && (bool?)rootRules[".write"] == false && new[] { "finalEvaluationWorkflows", "finalTrainingOutcomes", "notifications" }.All(root => PrivateRulesNode(rootRules[root]));
            int bindingsValid = 0, atThreshold = 0;
            foreach (var p in placements.Properties())
            {
                string id = (string)p.Value["trainingAssignmentId"] ?? (string)p.Value["attendanceAssignmentId"];
                if (string.IsNullOrWhiteSpace(id)) id = assignments.Properties().Where(a => (string)a.Value["placementId"] == p.Name).Select(a => a.Name).SingleOrDefault();
                var assignment = id == null ? null : assignments[id];
                if (assignment == null || (string)assignment["studentId"] != (string)p.Value["studentId"] || (string)assignment["companyId"] != (string)p.Value["companyId"]) continue;
                bindingsValid++;
                var rows = await gateway.ReadAsync("uimsAttendance/attendance/" + StudentManagementService.Key(id)) as JObject;
                long minutes = UIMS.Attendance.ApprovedTrainingMinutes.Calculate(rows, id, (string)p.Value["studentId"], (string)p.Value["companyId"], DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                if (minutes >= FinalReportWorkflow.RequiredMinutes) atThreshold++;
            }
            Console.WriteLine("Verified private inherited Rules=" + privateRules + "; valid existing attendance bindings=" + bindingsValid + "; placements with at least 18,000 valid approved minutes=" + atThreshold + ". Direct-path reads require no new query indexes.");
            if (!privateRules || bindingsValid != placements.Count) return 2;
            var taskRecords = await gateway.ReadAsync("tasks") as JObject ?? new JObject();
            var weeklyRecords = await gateway.ReadAsync("weeklyReports") as JObject ?? new JObject();
            bool taskShape = taskRecords.Properties().All(p => p.Value is JObject && p.Value["title"] != null && (p.Value["studentId"] != null || p.Value["placementId"] != null));
            bool reportShape = weeklyRecords.Properties().All(p => p.Value is JObject && p.Value["placementId"] != null && p.Value["studentId"] != null);
            bool modulePrivate = new[] { "tasks", "taskBoards", "weeklyReports", "weeklyReportReviews" }.All(root => PrivateRulesNode(rootRules[root]));
            Console.WriteLine("Actual module schema read: task records=" + taskRecords.Count + "; weekly reports=" + weeklyRecords.Count + "; task contract=" + taskShape + "; report contract=" + reportShape + "; inherited private module Rules=" + modulePrivate);
            foreach (var module in new[] { "tasks", "taskBoards", "weeklyReports", "weeklyReportReviews" }) Console.WriteLine("Existing " + module + " top read=" + rootRules[module]?[".read"] + "; top write=" + rootRules[module]?[".write"] + "; nested conditional grants=" + (rootRules[module]?.SelectTokens("$..['.read']").Count(t => t.Type == JTokenType.String) ?? 0));
            if (!modulePrivate)
            {
                var proposed = (JObject)rules.DeepClone();
                foreach (var module in new[] { "tasks", "weeklyReports" }) { if (proposed["rules"][module] == null) proposed["rules"][module] = new JObject(); proposed["rules"][module][".read"] = false; proposed["rules"][module][".write"] = false; }
                string folder = Path.GetFullPath("Tests/FinalEvaluation");
                File.WriteAllText(Path.Combine(folder, "firebase-module-rules-before.PRIVATE.json"), rules.ToString());
                File.WriteAllText(Path.Combine(folder, "firebase-module-rules.proposed.json"), proposed.ToString());
                Console.WriteLine("Prepared rules proposal: only tasks/weeklyReports top read/write become false; no deployment performed.");
            }
            if (!taskShape || !reportShape) return 2;
            Func<string, string> readSettings = key => new[] { "TasksEnabled", "FirebaseTaskSchemaValidated", "WeeklyReportsEnabled", "WeeklyReportsSchemaValidated" }.Contains(key) ? "true" : setting(key);
            var profiles = await gateway.ReadAsync("users") as JObject ?? new JObject();
            bool assignmentIssues = false;
            JProperty coordinator = null;
            foreach (var candidate in profiles.Properties().Where(p => FirebaseSignInService.CanonicalRole((string)p.Value["role"]) == "coordinator" && (bool?)p.Value["isActive"] == true))
                if (await gateway.IsActiveAuthenticationUidAsync(candidate.Name)) { coordinator = candidate; break; }
            if (coordinator != null && await gateway.IsActiveAuthenticationUidAsync(coordinator.Name))
            {
                var assignmentService = new AcademicAssignmentService(gateway, gateway.IsActiveAuthenticationUidAsync);
                var assignmentView = await assignmentService.LoadAsync(coordinator.Name);
                var groups = assignmentView["groups"] as JArray; var professors = assignmentView["professors"] as JArray; var roster = assignmentView["students"] as JArray;
                int incompatible = 0, profileMismatch = 0, placementMismatch = 0, groupedCount = 0;
                foreach (var group in groups)
                {
                    string supervisor = (string)group["supervisorId"] ?? "";
                    if (supervisor.Length == 0) continue;
                    var professor = professors.FirstOrDefault(p => (string)p["id"] == supervisor);
                    if (professor == null || (string)professor["department"] != (string)group["department"]) { incompatible++; Console.WriteLine("ASSIGNMENT ISSUE group=" + group["id"] + "; groupDepartment=" + group["department"] + "; professorId=" + supervisor + "; professorDepartment=" + (professor?["department"] ?? "missing")); }
                    foreach (var member in ((group["students"] as JObject) ?? new JObject()).Properties().Where(p => (bool?)p.Value == true))
                    {
                        groupedCount++; var student = roster.FirstOrDefault(p => (string)p["id"] == member.Name);
                        if (student == null || (string)student["department"] != (string)group["department"]) { incompatible++; Console.WriteLine("ASSIGNMENT ISSUE group=" + group["id"] + "; studentId=" + member.Name + "; studentDepartment=" + (student?["department"] ?? "missing") + "; groupDepartment=" + group["department"]); }
                        var profile = profiles.Properties().FirstOrDefault(p => (string)p.Value["studentId"] == member.Name);
                        if ((string)profile?.Value["academicSupervisorId"] != supervisor) profileMismatch++;
                        placementMismatch += placements.Properties().Count(p => (string)p.Value["studentId"] == member.Name && (string)p.Value["status"] != "completed" && (string)p.Value["academicSupervisorId"] != supervisor);
                        foreach (var p in placements.Properties().Where(p => (string)p.Value["studentId"] == member.Name && (string)p.Value["status"] != "completed" && (string)p.Value["academicSupervisorId"] != supervisor)) Console.WriteLine("ASSIGNMENT ISSUE placement=" + p.Name + "; studentId=" + member.Name + "; placementProfessor=" + p.Value["academicSupervisorId"] + "; groupProfessor=" + supervisor);
                    }
                }
                Console.WriteLine("Actual assignment audit GET-only: groups=" + groups.Count + "; professors=" + professors.Count + "; students=" + roster.Count + "; grouped students=" + groupedCount + "; department incompatibilities=" + incompatible + "; Manage Students profile mismatches=" + profileMismatch + "; active placement supervisor mismatches=" + placementMismatch + ". Existing records were not repaired or changed.");
                assignmentIssues = incompatible + profileMismatch + placementMismatch > 0;
            }
            else Console.WriteLine("Assignment API live coordinator check BLOCKED: no matching active coordinator Firebase Auth/profile pair was found. No account was created or modified.");
            int verifiedCompanies = 0;
            foreach (var user in profiles.Properties().Where(p => p.Value is JObject && FirebaseSignInService.CanonicalRole((string)p.Value["role"]) == "companySupervisor" && (bool?)p.Value["isActive"] == true && ((string)p.Value["accountSetupStatus"] == "complete" || p.Value["accountSetupStatus"] == null)))
            {
                if (!await gateway.IsActiveAuthenticationUidAsync(user.Name)) continue;
                string mapped = (string)user.Value["companySupervisorId"];
                if (string.IsNullOrWhiteSpace(mapped)) { var supervisorMap = await gateway.ReadAsync("companySupervisors") as JObject ?? new JObject(); mapped = supervisorMap.Properties().Where(p => (string)p.Value["firebaseUid"] == user.Name && (string)p.Value["status"] == "active").Select(p => p.Name).SingleOrDefault(); }
                var actor = UIMS.Tasks.TaskActor.FromVerifiedProfile(user.Name, "companySupervisor", (JObject)user.Value, mapped);
                using (var client = new System.Net.Http.HttpClient(new ModuleReadOnlyHandler()) { Timeout = TimeSpan.FromSeconds(60) })
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew(); var tasksService = new UIMS.Tasks.TaskService(readSettings, client, actor);
                    var data = await tasksService.LoadAsync("companySupervisor", System.Threading.CancellationToken.None);
                    var firstPlacement = data["students"].FirstOrDefault();
                    if (firstPlacement != null) { var board = await tasksService.LoadBoardAsync((string)firstPlacement["placementId"], System.Threading.CancellationToken.None); Console.WriteLine("PASS actual authorized task board read: columns=" + board["columns"].Count() + "; cards=" + board["tasks"].Count() + ". No card moved."); }
                    var trainees = await tasksService.LoadTraineesAsync(System.Threading.CancellationToken.None);
                    var reports = await new UIMS.Tasks.WeeklyReportService(readSettings, client, actor).ListCompanyReportsAsync(System.Threading.CancellationToken.None);
                    Console.WriteLine("Verified existing company read: authorized tasks=" + data["tasks"].Count() + "; eligible task placements=" + data["students"].Count() + "; trainee rows=" + trainees["trainees"].Count() + "; weekly report rows=" + reports["reports"].Count() + "; combined elapsedMs=" + timer.ElapsedMilliseconds + ". GET-only transport enforced."); verifiedCompanies++;
                }
            }
            Console.WriteLine("Existing authenticated company accounts verified=" + verifiedCompanies);
            foreach (string role in new[] { "academicSupervisor", "student" })
            {
                JProperty user = null;
                foreach (var candidate in profiles.Properties().Where(p => p.Value is JObject && FirebaseSignInService.CanonicalRole((string)p.Value["role"]) == role && (bool?)p.Value["isActive"] == true))
                    if (await gateway.IsActiveAuthenticationUidAsync(candidate.Name)) { user = candidate; break; }
                if (user == null) { Console.WriteLine("No active account available for read verification: " + role); continue; }
                var actor = UIMS.Tasks.TaskActor.FromVerifiedProfile(user.Name, role, (JObject)user.Value, null, role == "academicSupervisor");
                using (var client = new System.Net.Http.HttpClient(new ModuleReadOnlyHandler()) { Timeout = TimeSpan.FromSeconds(90) })
                {
                    var reports = new UIMS.Tasks.WeeklyReportService(readSettings, client, actor);
                    var result = role == "academicSupervisor" ? await reports.ListAcademicReportsAsync(System.Threading.CancellationToken.None) : await reports.ListStudentReportsAsync(System.Threading.CancellationToken.None);
                    Console.WriteLine("Verified actual " + role + " weekly-report read: reports=" + result["reports"].Count() + "; assigned students=" + (result["assignedStudents"]?.Count() ?? 0) + ". GET-only transport enforced.");
                }
            }
            if (verifiedCompanies == 0) return 2;
            if (assignmentIssues) { Console.WriteLine("FAIL existing assignment consistency: see ASSIGNMENT ISSUE lines. No automatic repair was performed."); return 1; }
            return 0;
        }
        catch (FirebaseException e) { Console.WriteLine("BLOCKED: Firebase status=" + e.StatusCode + "; " + e.Message); return 2; }
        catch (Exception e) { Console.WriteLine("BLOCKED: read-only preflight failed: " + e.GetType().Name + ". No credentials or remote response bodies printed."); return 2; }
    }
    private static bool PrivateRulesNode(JToken node)
    {
        if (node == null) return true;
        if (!(node is JObject)) return false;
        foreach (var p in ((JObject)node).Properties())
        {
            if (p.Name == ".read" || p.Name == ".write") { if (p.Value.Type != JTokenType.Boolean || (bool)p.Value) return false; }
            else if (!p.Name.StartsWith(".") && !PrivateRulesNode(p.Value)) return false;
        }
        return true;
    }
    private sealed class ModuleReadOnlyHandler : System.Net.Http.DelegatingHandler
    {
        internal ModuleReadOnlyHandler() : base(new System.Net.Http.HttpClientHandler { MaxConnectionsPerServer = 12 }) { }
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellation)
        { if (request.Method != System.Net.Http.HttpMethod.Get) throw new InvalidOperationException("Live writes are forbidden in this diagnostic."); return base.SendAsync(request, cancellation); }
    }
}
