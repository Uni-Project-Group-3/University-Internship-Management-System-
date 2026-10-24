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
    private static async Task<int> PrepareCurrentAttendance(string configPath)
    {
        var xml=new XmlDocument();xml.Load(configPath);var values=xml.SelectNodes("/configuration/appSettings/add").Cast<XmlElement>().ToDictionary(x=>x.GetAttribute("key"),x=>x.GetAttribute("value"));Func<string,string> setting=k=>values.ContainsKey(k)?values[k]:null;
        var config=new FirebaseConfiguration(setting("FirebaseEnabled"),setting("FirebaseProjectId"),setting("FirebaseDatabaseUrl"),setting("FirebaseApiKey"),setting("FirebaseLiveWritesEnabled"),setting("FirebaseTaskSchemaValidated"),setting("FirebaseSchemaValidated"),setting("FirebaseAuthenticationEnabled"));
        var gateway=new StudentFirebaseGateway(config);var placements=await gateway.ReadAsync("placements") as JObject??new JObject();var users=await gateway.ReadAsync("users") as JObject??new JObject();
        var found=placements.Properties().Where(x=>(string)x.Value["studentId"]=="2023005"&&new[]{"active","confirmed"}.Contains((string)x.Value["status"])).ToArray();
        if(found.Length!=1)throw new InvalidOperationException("Expected exactly one current placement for the authorized student.");
        string id=found[0].Name;var placement=(JObject)found[0].Value.DeepClone();string uid=(string)placement["studentUid"];var profile=users[uid] as JObject;
        if(!await gateway.IsActiveAuthenticationUidAsync(uid))throw new InvalidOperationException("Student Authentication account is not active.");
        placement["startDate"]="2026-10-25";placement["endDate"]="2026-12-20";
        var state=await gateway.ReadVersionAsync("uimsAttendance");var original=state.Value??new JObject();var prepared=(JObject)original.DeepClone();
        if(prepared["schedules"]?[id]!=null)throw new InvalidOperationException("A calendar already exists; no overwrite was prepared.");
        var calendar=new JObject();var zone=TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
        for(var day=new DateTime(2026,10,25);day<=new DateTime(2026,12,20);day=day.AddDays(1)){
            string date=day.ToString("yyyy-MM-dd");calendar[date]=new JObject{["assignmentId"]=id,["attendanceDate"]=date,["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=true,["scheduledStartAt"]=new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(day,zone)).ToUnixTimeMilliseconds(),["scheduledEndAt"]=new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).AddMilliseconds(-1),zone)).ToUnixTimeMilliseconds()};
        }
        if(!(prepared["schedules"] is JObject))prepared["schedules"]=new JObject();prepared["schedules"][id]=calendar;
        prepared=UIMS.Attendance.AttendanceOnboarding.Build(prepared,uid,"2023005",id,placement,profile);
        var patch=new JObject{["placements/"+id]=placement};
        foreach(string branch in new[]{"students","trainingAssignments","schedules","provisioningClaims"})foreach(var entry in ((JObject)prepared[branch]).Properties())if(original[branch]?[entry.Name]==null)patch["uimsAttendance/"+branch+"/"+entry.Name]=entry.Value.DeepClone();
        string folder=Path.GetFullPath("Tests/Reports/Attendance-2023005-20261025");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"before-placement.json"),found[0].Value.ToString());File.WriteAllText(Path.Combine(folder,"before-attendance.json"),original.ToString());
        var instructions=new System.Text.StringBuilder("Import each numbered JSON only at the exact Firebase child path below. Do not import at the database root. Stop if an absent attendance child was created meanwhile.\n\n");int index=0;
        foreach(var entry in patch.Properties()){string file=(++index).ToString("00")+"-"+entry.Name.Split('/').Reverse().Skip(1).First()+".json";File.WriteAllText(Path.Combine(folder,file),entry.Value.ToString());instructions.AppendLine(entry.Name+" <- "+file);}
        File.WriteAllText(Path.Combine(folder,"IMPORT-PATHS.txt"),instructions.ToString());
        Console.WriteLine("Prepared "+patch.Count+" exact child records. Start 2026-10-25; end 2026-12-20; required hours preserved: "+placement["requiredHours"]+". No attendance/check-ins/hours were generated. Output: "+folder);
        try{config.EnsureWritesAllowed();Console.WriteLine("Gateway writes available; no apply operation was requested.");}catch(FirebaseException){Console.WriteLine("BLOCKED live apply: configured general write/schema gates remain disabled. Import files prepared; no settings bypassed.");}
        return 0;
    }
    private static async Task<int> AttendanceSetupKnownPlacement(string configPath, bool apply)
    {
        var xml=new XmlDocument();xml.Load(configPath);
        var values=xml.SelectNodes("/configuration/appSettings/add").Cast<XmlElement>().ToDictionary(x=>x.GetAttribute("key"),x=>x.GetAttribute("value"));
        Func<string,string> setting=k=>values.ContainsKey(k)?values[k]:null;
        var config=new FirebaseConfiguration(setting("FirebaseEnabled"),setting("FirebaseProjectId"),setting("FirebaseDatabaseUrl"),setting("FirebaseApiKey"),setting("FirebaseLiveWritesEnabled"),setting("FirebaseTaskSchemaValidated"),setting("FirebaseSchemaValidated"),setting("FirebaseAuthenticationEnabled"));
        var db=new StudentFirebaseGateway(config);
        const string studentId="2023005",placementId="PLC-APP-20261024171210-6EB70F";
        var p=await db.ReadAsync("placements/"+placementId) as JObject;
        if(p==null || (string)p["studentId"]!=studentId || !new[]{"active","confirmed"}.Contains((string)p["status"]) || (string)p["startDate"]!="2026-10-24" || (string)p["endDate"]!="2026-12-19" || (int?)p["requiredHours"]!=300)throw new InvalidOperationException("Persisted placement dates/identity do not match the approved 24 October–19 December training period.");
        string studentUid=(string)p["studentUid"],companyId=(string)p["companyId"],supervisorId=(string)p["companySupervisorId"];
        var users=await db.ReadAsync("users") as JObject??new JObject();
        var student=users[studentUid] as JObject;
        var supervisors=users.Properties().Where(u=>FirebaseSignInService.CanonicalRole((string)u.Value["role"])=="companySupervisor" && (bool?)u.Value["isActive"]==true && (string)u.Value["companyId"]==companyId && ((string)u.Value["companySupervisorId"]==supervisorId || (string)u.Value["supervisorId"]==supervisorId || u.Name==supervisorId)).ToArray();
        if(student==null || (string)student["studentId"]!=studentId || (bool?)student["isActive"]!=true || supervisors.Length!=1)throw new InvalidOperationException("The approved student and company supervisor identities are not unique and active.");
        if(!await db.IsActiveAuthenticationUidAsync(studentUid)||!await db.IsActiveAuthenticationUidAsync(supervisors[0].Name))throw new InvalidOperationException("Both existing Authentication accounts must be active.");
        var company=await db.ReadAsync("companies/"+companyId) as JObject;
        if(company==null)throw new InvalidOperationException("The existing company record is missing.");
        var state=await db.ReadVersionAsync("uimsAttendance");var original=state.Value??new JObject();var next=(JObject)original.DeepClone();
        Action<string,string,JObject> addMissing=(branch,id,record)=>{
            if(!(next[branch] is JObject))next[branch]=new JObject();
            var existing=next[branch][id] as JObject;
            if(existing!=null){foreach(var field in record.Properties())if(!JToken.DeepEquals(existing[field.Name],field.Value))throw new InvalidOperationException("An existing "+branch+" record conflicts; it was not overwritten.");}
            else if(next[branch][id]!=null)throw new InvalidOperationException("Unexpected existing record shape.");
            else next[branch][id]=record;
        };
        addMissing("companies",companyId,new JObject{["name"]=company["name"],["status"]="active"});
        addMissing("companySupervisors",supervisorId,new JObject{["name"]=supervisors[0].Value["fullName"],["firebaseUid"]=supervisors[0].Name,["companyId"]=companyId,["status"]="active",["isTestData"]=false});
        addMissing("schedules",placementId,BuildApprovedAttendanceCalendar(placementId));
        next=UIMS.Attendance.AttendanceOnboarding.Build(next,studentUid,studentId,placementId,p,student);
        Console.WriteLine("PLAN existing student/company/supervisor only; approved dates 2026-10-24 through 2026-12-19; Sunday–Thursday 08:00–18:00 Asia/Amman; calendar days="+((JObject)next["schedules"][placementId]).Count+"; attendance enabled="+((bool?)next["settings"]?["enabled"]==true)+"; changed="+!JToken.DeepEquals(original,next));
        if(!apply){
            string folder=Path.GetFullPath(Path.Combine("Tests","Reports","AttendanceSetup-2023005"));Directory.CreateDirectory(folder);
            var instructions=new System.Text.StringBuilder("Firebase Realtime Database: import each file ONLY at its exact child path. These child records were absent when prepared. Do not import at the database root or at uimsAttendance. If a child now exists, stop and reconcile instead of overwriting it.\n\n");
            int index=0;
            foreach(string branch in new[]{"companies","companySupervisors","schedules","students","trainingAssignments","provisioningClaims"})
                foreach(var entry in (next[branch] as JObject??new JObject()).Properties())
                    if(original[branch]?[entry.Name]==null){string name=(++index).ToString("00")+"-"+branch+".json";File.WriteAllText(Path.Combine(folder,name),entry.Value.ToString());instructions.AppendLine("uimsAttendance/"+branch+"/"+entry.Name+"  <-  "+name);}
            File.WriteAllText(Path.Combine(folder,"IMPORT-PATHS.txt"),instructions.ToString());
            Console.WriteLine("Prepared "+index+" child import files: "+folder);Console.WriteLine("GET only. No writes.");return 0;
        }
        if(JToken.DeepEquals(original,next)){Console.WriteLine("Already configured. No writes.");return 0;}
        string backup=Path.Combine(Path.GetTempPath(),"uims-attendance-before-setup-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(backup,original.ToString());
        if(string.IsNullOrWhiteSpace(state.ETag)||!await db.CompareWriteAsync("uimsAttendance",next,state.ETag))throw new InvalidOperationException("Concurrent change detected. No overwrite; inspect the saved backup before retrying.");
        var saved=await db.ReadAsync("uimsAttendance") as JObject;
        if(!JToken.DeepEquals(next,saved))throw new InvalidOperationException("Setup write was acknowledged, but readback changed. Reconcile before retrying.");
        Console.WriteLine("PASS exact-ETag setup persisted and GET readback matched. Existing attendance/history preserved. Backup: "+backup);return 0;
    }
    private static JObject BuildApprovedAttendanceCalendar(string assignmentId)
    {
        var calendar=new JObject();var zone=TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
        for(var day=new DateTime(2026,10,24);day<=new DateTime(2026,12,19);day=day.AddDays(1))
        {
            string date=day.ToString("yyyy-MM-dd");bool training=day.DayOfWeek!=DayOfWeek.Friday&&day.DayOfWeek!=DayOfWeek.Saturday;
            var row=new JObject{["assignmentId"]=assignmentId,["attendanceDate"]=date,["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=training};
            if(training){row["scheduledStartAt"]=new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(day.AddHours(8),zone)).ToUnixTimeMilliseconds();row["scheduledEndAt"]=new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(day.AddHours(18),zone)).ToUnixTimeMilliseconds();}
            calendar[date]=row;
        }return calendar;
    }
    private static async Task<int> AttendanceLinkReadOnly(string configPath, string studentId)
    {
        var xml = new XmlDocument(); xml.Load(configPath);
        var values = xml.SelectNodes("/configuration/appSettings/add").Cast<XmlElement>().ToDictionary(x => x.GetAttribute("key"), x => x.GetAttribute("value"));
        Func<string,string> setting = key => values.ContainsKey(key) ? values[key] : null;
        var config = new FirebaseConfiguration(setting("FirebaseEnabled"), setting("FirebaseProjectId"), setting("FirebaseDatabaseUrl"), setting("FirebaseApiKey"), setting("FirebaseLiveWritesEnabled"), setting("FirebaseTaskSchemaValidated"), setting("FirebaseSchemaValidated"), setting("FirebaseAuthenticationEnabled"));
        config.EnsureEnabled(); var gateway = new StudentFirebaseGateway(config);
        var users = await gateway.ReadAsync("users") as JObject ?? new JObject();
        var placements = await gateway.ReadAsync("placements") as JObject ?? new JObject();
        var supervisors = await gateway.ReadAsync("uimsAttendance/companySupervisors") as JObject ?? new JObject();
        var assignments = await gateway.ReadAsync("uimsAttendance/trainingAssignments") as JObject ?? new JObject();
        var student = await gateway.ReadAsync("uimsAttendance/students/" + StudentManagementService.Key(studentId)) as JObject;
        Console.WriteLine("READ ONLY student=" + studentId + "; active profiles=" + users.Properties().Count(p => (string)p.Value["studentId"] == studentId && (bool?)p.Value["isActive"] == true) + "; attendance student present=" + (student != null));
        foreach (var p in placements.Properties().Where(p => (string)p.Value["studentId"] == studentId))
        {
            string company = (string)p.Value["companyId"], sup = (string)p.Value["companySupervisorId"];
            var profiles = users.Properties().Where(u => (string)u.Value["companyId"] == company && FirebaseSignInService.CanonicalRole((string)u.Value["role"]) == "companySupervisor").ToArray();
            Console.WriteLine("placement=" + p.Name + "; status=" + p.Value["status"] + "; start=" + p.Value["startDate"] + "; end=" + p.Value["endDate"] + "; requiredHours=" + p.Value["requiredHours"] + "; academic assigned=" + !string.IsNullOrWhiteSpace((string)p.Value["academicSupervisorId"]) + "; company profiles=" + profiles.Length + "; direct attendance supervisor=" + (sup != null && supervisors[sup] != null) + "; UID matched attendance supervisors=" + supervisors.Properties().Count(s => profiles.Any(u => u.Name == (string)s.Value["firebaseUid"]) && (string)s.Value["companyId"] == company) + "; linked attendance assignments=" + assignments.Properties().Count(a => (string)a.Value["placementId"] == p.Name));
        }
        Console.WriteLine("GET only. No Firebase writes."); return 0;
    }
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
