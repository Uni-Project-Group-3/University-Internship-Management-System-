using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;

internal static partial class Program
{
    private static async Task<int> ExportDatabaseReadOnly(string configPath,string destination)
    {
        var xml=new XmlDocument();xml.Load(configPath);
        var settings=xml.SelectNodes("/configuration/appSettings/add").Cast<XmlElement>().ToDictionary(e=>e.GetAttribute("key"),e=>e.GetAttribute("value"));
        Func<string,string> get=k=>settings.ContainsKey(k)?settings[k]:null;
        var config=new FirebaseConfiguration(get("FirebaseEnabled"),get("FirebaseProjectId"),get("FirebaseDatabaseUrl"),get("FirebaseApiKey"),get("FirebaseLiveWritesEnabled"),get("FirebaseTaskSchemaValidated"),get("FirebaseSchemaValidated"),get("FirebaseAuthenticationEnabled"));
        config.EnsureEnabled();
        if(File.Exists(destination) || File.Exists(destination+".manifest.json"))throw new InvalidOperationException("Export target already exists.");
        var data=await new StudentFirebaseGateway(config).ReadAsync("");
        if(!(data is JObject))throw new InvalidOperationException("The database root is not an object; no export written.");
        var bytes=new System.Text.UTF8Encoding(false).GetBytes(data.ToString(Newtonsoft.Json.Formatting.Indented));
        using(var stream=new FileStream(destination,FileMode.CreateNew,FileAccess.Write))stream.Write(bytes,0,bytes.Length);
        string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
        var manifest=new JObject{["projectId"]=config.ProjectId,["databaseUrl"]=config.DatabaseUrl,["observedAtUtc"]=DateTimeOffset.UtcNow.ToString("o"),["sha256"]=hash,["byteCount"]=bytes.Length,["collectionCount"]=((JObject)data).Count,["dataChanged"]=false,["productionWrites"]=0,["scope"]="Full Realtime Database root only. Firebase Authentication accounts and Storage bytes are separate."};
        File.WriteAllText(destination+".manifest.json",manifest.ToString(),new System.Text.UTF8Encoding(false));
        Console.WriteLine(new JObject{["exportCreated"]=true,["collections"]=((JObject)data).Count,["bytes"]=bytes.Length,["productionWrites"]=0}.ToString());return 0;
    }
    private static Task AttendanceOnboardingBoundaryTests()
    {
        string aid="PLC-OFFLINE";
        var profile=JObject.Parse("{\"role\":\"student\",\"studentId\":\"S1\",\"isActive\":true,\"fullName\":\"Offline fixture\"}");
        var placement=JObject.Parse("{\"studentId\":\"S1\",\"studentUid\":\"uid1\",\"companyId\":\"C1\",\"companySupervisorId\":\"CS1\",\"academicSupervisorId\":\"AS1\",\"startDate\":\"2026-12-01\",\"endDate\":\"2027-03-22\",\"requiredHours\":300}");
        var root=JObject.Parse("{\"companies\":{\"C1\":{\"status\":\"active\"}},\"companySupervisors\":{\"CS1\":{\"status\":\"active\",\"companyId\":\"C1\"}},\"schedules\":{},\"attendance\":{\"protected\":{\"minutes\":2405}}}");
        root["schedules"][aid]=new JObject{["2026-12-01"]=new JObject{["assignmentId"]=aid,["attendanceDate"]="2026-12-01",["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=false}};
        Func<JObject,JObject> build=r=>UIMS.Attendance.AttendanceOnboarding.Build(r,"uid1","S1",aid,placement,profile);
        var original=root.ToString();var next=build(root);
        Assert(root.ToString()==original && JToken.DeepEquals(root["schedules"],next["schedules"]) && JToken.DeepEquals(root["attendance"],next["attendance"]));
        Assert(JToken.DeepEquals(next,build(next)));
        Action<JObject> rejected=r=>{try{build(r);}catch(FirebaseException){return;}throw new Exception("Unsafe onboarding accepted.");};
        var missing=(JObject)root.DeepClone();missing["schedules"]=new JObject();rejected(missing);
        var wrong=(JObject)next.DeepClone();wrong["students"]["S1"]["firebaseUid"]="other";rejected(wrong);
        wrong=(JObject)next.DeepClone();wrong["trainingAssignments"][aid]["academicSupervisorId"]="OTHER";rejected(wrong);
        wrong=(JObject)next.DeepClone();wrong["provisioningClaims"][aid]["studentId"]="OTHER";rejected(wrong);
        wrong=(JObject)root.DeepClone();wrong["schedules"][aid]["2026-12-01"]["timeZone"]="UTC";rejected(wrong);
        try{UIMS.Attendance.AttendanceOnboarding.Build(root,"uid1","2023301",aid,placement,profile);throw new Exception("Frozen student accepted.");}catch(FirebaseException){}
        return Task.CompletedTask;
    }
    private static async Task<int> StudentIntegrationAudit(string configPath, string reportPath)
    {
        var xml=new XmlDocument();xml.Load(configPath);
        var settings=xml.SelectNodes("/configuration/appSettings/add").Cast<XmlElement>().ToDictionary(e=>e.GetAttribute("key"),e=>e.GetAttribute("value"));
        Func<string,string> get=k=>settings.ContainsKey(k)?settings[k]:null;
        var config=new FirebaseConfiguration(get("FirebaseEnabled"),get("FirebaseProjectId"),get("FirebaseDatabaseUrl"),get("FirebaseApiKey"),get("FirebaseLiveWritesEnabled"),get("FirebaseTaskSchemaValidated"),get("FirebaseSchemaValidated"),get("FirebaseAuthenticationEnabled"));
        config.EnsureAuthenticationEnabled();
        var gateway=new StudentFirebaseGateway(config);
        var root=new JObject();
        foreach(var path in new[]{"users","placements","applications","opportunities","assignments","supervisorAssignments","companySupervisors","companies","uimsAttendance/students","uimsAttendance/trainingAssignments","uimsAttendance/schedules","uimsAttendance/attendance","universityRecords/students"})
        {
            var token=await gateway.ReadAsync(path);
            var parts=path.Split('/');if(parts.Length==1)root[path]=token;else {if(!(root[parts[0]] is JObject))root[parts[0]]=new JObject();root[parts[0]][parts[1]]=token;}
        }
        var users=root["users"] as JObject ?? new JObject();var metadata=new JObject();
        // One metadata lookup per existing student-profile UID; never list/export passwords.
        foreach(var profile in users.Properties().Where(p=>p.Value is JObject && (string)p.Value["role"]=="student"))
            metadata[profile.Name]=await gateway.ReadAuthenticationMetadataAsync(profile.Name);
        var report=ReconcileStudentSnapshot(root,metadata,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        string probeUid=metadata.Properties().Where(p=>(string)p.Value["state"]=="enabled").Select(p=>p.Name).FirstOrDefault();
        if(probeUid!=null)
        {
            // Controlled comparison: DB-only OAuth scopes must not be reused for Auth.
            // This is a metadata lookup, never a user creation/sign-in/update operation.
            using(var client=new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(30)})
            using(var request=new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post,"https://identitytoolkit.googleapis.com/v1/projects/"+config.ProjectId+"/accounts:lookup"))
            {
                request.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",await FirebaseServerCredential.AccessTokenAsync(System.Threading.CancellationToken.None));
                request.Content=new System.Net.Http.StringContent(new JObject{["localId"]=new JArray(probeUid)}.ToString(),System.Text.Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(request))
                {
                    string reason="";
                    if(!response.IsSuccessStatusCode){try{var body=JObject.Parse(await response.Content.ReadAsStringAsync());if(body.SelectTokens("error.details[*].reason").Any(v=>v.ToString()=="ACCESS_TOKEN_SCOPE_INSUFFICIENT"))reason="ACCESS_TOKEN_SCOPE_INSUFFICIENT";}catch(Newtonsoft.Json.JsonException){}}
                    report["authScopeComparison"]=StudentAuthAuditFailure.Describe((int)response.StatusCode,reason,config.ProjectId,"controlledDatabaseTokenProbe");
                    report["authScopeComparison"]["isOriginalReportedRequest"]=false;
                }
            }
        }
        report["projectId"]=config.ProjectId;report["databaseUrl"]=config.DatabaseUrl;report["evidence"]="liveReadOnlySequentialReadsNotAtomicSnapshot";report["observedAtUtc"]=DateTimeOffset.UtcNow.ToString("o");
        using(var writer=new StreamWriter(new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write),new System.Text.UTF8Encoding(false)))writer.Write(report.ToString());
        Console.WriteLine(new JObject{["students"]=report["studentCount"],["counts"]=report["counts"],["productionWrites"]=0,["reportCreated"]=true}.ToString());
        return 0;
    }

    private static JObject ReconcileStudentSnapshot(JObject root,JObject metadata,long now)
    {
        Func<JToken,string,string> text=(n,k)=>n is JObject && n[k]?.Type==JTokenType.String?(string)n[k]:"";
        Func<JToken,IEnumerable<JProperty>> rows=n=>(n as JObject ?? new JObject()).Properties();
        var users=root["users"] as JObject ?? new JObject();var attendance=root["uimsAttendance"] as JObject ?? new JObject();
        var ids=new HashSet<string>(rows(users).Where(p=>text(p.Value,"role")=="student").Select(p=>text(p.Value,"studentId")).Where(s=>s.Length>0));
        foreach(var p in rows(attendance["students"]))ids.Add(p.Name);
        foreach(var p in rows(root["placements"]))if(text(p.Value,"studentId").Length>0)ids.Add(text(p.Value,"studentId"));
        var result=new JArray();
        foreach(string sid in ids.OrderBy(s=>s,StringComparer.Ordinal))
        {
            var blockers=new JArray();var paths=new JArray();var profile=rows(users).Where(p=>text(p.Value,"role")=="student" && text(p.Value,"studentId")==sid).ToArray();
            string uid=profile.Length==1?profile[0].Name:"";var auth=uid.Length>0?metadata[uid] as JObject:null;
            if(profile.Length!=1)blockers.Add("Missing or ambiguous users profile/studentId mapping");
            if(auth==null || (bool?)auth["verified"]!=true)blockers.Add("Auth existence/enabled status unverified");
            else if(text(auth,"state")=="missing")blockers.Add("Profile UID not found in Auth; approved account mapping or official provisioning required");
            else if(text(auth,"state")!="enabled")blockers.Add("Auth account "+text(auth,"state"));
            if(profile.Length==1){paths.Add("users/"+uid);if((bool?)profile[0].Value["isActive"]!=true)blockers.Add("Application profile is inactive");
                if((bool?)auth?["verified"]==true && text(auth,"state")!="missing" && !string.Equals(text(auth,"email"),text(profile[0].Value,"email"),StringComparison.OrdinalIgnoreCase))blockers.Add("Auth/profile email mismatch");}
            var placements=rows(root["placements"]).Where(p=>text(p.Value,"studentId")==sid).ToArray();
            var applications=new JArray();
            foreach(var app in rows(root["applications"]).Where(p=>text(p.Value,"studentId")==sid || uid.Length>0 && text(p.Value,"studentId")==uid))
            {
                string opportunityId=text(app.Value,"opportunityId");var opportunity=(root["opportunities"] as JObject)?[opportunityId];
                paths.Add("applications/"+app.Name);
                applications.Add(new JObject{["path"]="applications/"+app.Name,["status"]=text(app.Value,"status"),["companyId"]=text(app.Value,"companyId"),["opportunityPath"]="opportunities/"+opportunityId,["offerStartDate"]=text(opportunity,"startDate"),["offerDurationWeeks"]=opportunity?["durationWeeks"]?.DeepClone(),["offerRequiredHours"]=opportunity?["requiredHours"]?.DeepClone()});
            }
            if(placements.Length==0)blockers.Add("Missing placement");else if(placements.Length>1)blockers.Add("Multiple placements require review");
            var academics=new HashSet<string>();
            Action<string> addAcademic=id=>{if(id.Length>0)academics.Add(id);};
            if(profile.Length==1)addAcademic(text(profile[0].Value,"academicSupervisorId"));
            foreach(string collection in new[]{"assignments","supervisorAssignments"})foreach(var a in rows(root[collection]).Where(p=>text(p.Value,"studentId")==sid && (bool?)p.Value["active"]!=false && (bool?)p.Value["isActive"]!=false)){paths.Add(collection+"/"+a.Name);addAcademic(text(a.Value,"academicSupervisorId"));}
            foreach(var p in placements)
            {
                paths.Add("placements/"+p.Name);addAcademic(text(p.Value,"academicSupervisorId"));
                if(!new[]{"active","confirmed","completed"}.Contains(text(p.Value,"status")))blockers.Add("Placement is not confirmed/active/completed");
                if(uid.Length>0 && text(p.Value,"studentUid").Length>0 && text(p.Value,"studentUid")!=uid)blockers.Add("Placement UID differs from existing users profile; Auth proof required before repair");
                string company=text(p.Value,"companyId"),supervisor=text(p.Value,"companySupervisorId");
                if(!((root["companies"] as JObject)?.ContainsKey(company) ?? false))blockers.Add("Placement company is missing");
                var supervisorRecord=(root["companySupervisors"] as JObject)?[supervisor];
                if(supervisorRecord==null || text(supervisorRecord,"companyId")!=company)blockers.Add("Placement company supervisor is missing or mismatched");
                string application=text(p.Value,"applicationId");var app=(root["applications"] as JObject)?[application];
                if(app==null || !new[]{"accepted","confirmed"}.Contains(text(app,"status")))blockers.Add("Placement accepted/confirmed application is missing or inconsistent");
            }
            var actor=(attendance["students"] as JObject)?[sid];string assignmentId=text(actor,"activeAssignmentId");
            var assignment=(attendance["trainingAssignments"] as JObject)?[assignmentId] as JObject;
            if(actor==null)blockers.Add("Missing attendance actor");
            else {paths.Add("uimsAttendance/students/"+sid);if(text(actor,"status")!="active")blockers.Add("Attendance actor inactive");if(uid.Length>0 && text(actor,"firebaseUid")!=uid)blockers.Add("Attendance UID mapping mismatch; do not auto-reassign");}
            long minutes=0;int scheduleCount=0;
            if(assignment==null)blockers.Add("Missing attendance assignment");
            else
            {
                paths.Add("uimsAttendance/trainingAssignments/"+assignmentId);addAcademic(text(assignment,"academicSupervisorId"));
                if(text(assignment,"studentId")!=sid || text(assignment,"status")!="active")blockers.Add("Attendance assignment ownership/status mismatch");
                if(!placements.Any(p=>p.Name==text(assignment,"placementId")))blockers.Add("Attendance assignment placement mismatch");
                var schedules=(attendance["schedules"] as JObject)?[assignmentId] as JObject;scheduleCount=schedules?.Count ?? 0;
                paths.Add("uimsAttendance/schedules/"+assignmentId);
                if(scheduleCount==0)blockers.Add("Missing approved schedule");
                else
                {
                    foreach(var entry in schedules.Properties())
                    {
                        var day=entry.Value as JObject;
                        // Shift validation at a synthetic later time checks structure only,
                        // not whether this student may attend on the current date.
                        if(!UIMS.Attendance.AttendanceService.CanDeriveScheduleHistory(day,assignment,assignmentId,entry.Name,new DateTimeOffset(2099,1,1,0,0,0,TimeSpan.Zero).ToUnixTimeMilliseconds()))
                        {blockers.Add("Calendar contains structurally invalid or out-of-assignment dates");break;}
                    }
                    blockers.Add("Existing calendar approval requires business confirmation; structure alone is not approval");
                }
                minutes=UIMS.Attendance.ApprovedTrainingMinutes.Calculate((attendance["attendance"] as JObject)?[assignmentId] as JObject,assignmentId,sid,text(assignment,"companyId"),now);
            }
            if(academics.Count>1)blockers.Add("Conflicting academic supervisor");else if(academics.Count==0)blockers.Add("Missing academic supervisor");
            bool frozen=sid=="2023301";if(frozen)blockers.Add("Frozen pending coordinator decision");
            result.Add(new JObject{["studentId"]=sid,["uid"]=uid,["loginEmail"]=profile.Length==1?text(profile[0].Value,"email"):null,["auth"]=auth?.DeepClone() ?? new JObject{["state"]="unverified",["verified"]=false},["paths"]=paths,["applications"]=applications,["placementCount"]=placements.Length,["scheduleEntries"]=scheduleCount,["approvedMinutes"]=minutes,["approvedHours"]=Math.Round(minutes/60m,2),["academicSupervisorIds"]=new JArray(academics.OrderBy(s=>s)),["blockers"]=new JArray(blockers.Values<string>().Distinct()),["ready"]=blockers.Count==0,["existingSetupCandidate"]=blockers.Values<string>().All(b=>b=="Existing calendar approval requires business confirmation; structure alone is not approval"),["frozen"]=frozen,["repairApplied"]=false});
        }
        return new JObject{["studentCount"]=ids.Count,["students"]=result,["counts"]=new JObject{["ready"]=result.Count(r=>(bool)r["ready"]),["existingSetupCandidatesPendingCalendarApproval"]=result.Count(r=>(bool)r["existingSetupCandidate"]),["profileUidMissingFromAuth"]=result.Count(r=>(string)r["auth"]["state"]=="missing"),["authVerifiedEnabled"]=result.Count(r=>(bool?)r["auth"]["verified"]==true && (string)r["auth"]["state"]=="enabled"),["authUnverified"]=result.Count(r=>(bool?)r["auth"]["verified"]!=true),["academicConflicts"]=result.Count(r=>r["blockers"].Values<string>().Contains("Conflicting academic supervisor")),["missingPlacement"]=result.Count(r=>(int)r["placementCount"]==0)},["productionWrites"]=0,["calendarApprovalVerified"]=false};
    }
}
