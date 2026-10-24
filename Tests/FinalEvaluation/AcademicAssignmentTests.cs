using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;
internal static partial class Program
{
    private sealed class AssignmentDb : IStudentDatabase
    {
        internal JObject Root = new JObject(); private readonly Dictionary<string,int> versions = new Dictionary<string,int>(); internal bool LoseResponse;
        private JToken Get(string path) { JToken n=Root; foreach(string key in path.Split('/').Where(x=>x.Length>0)) n=(n as JObject)?[key]; return n?.DeepClone() ?? JValue.CreateNull(); }
        private void Put(string path,JToken value) { var keys=path.Split('/');var n=Root;for(int i=0;i<keys.Length-1;i++){if(!(n[keys[i]] is JObject))n[keys[i]]=new JObject();n=(JObject)n[keys[i]];}if(value==null||value.Type==JTokenType.Null)n.Remove(keys.Last());else n[keys.Last()]=value.DeepClone();versions[path]=versions.ContainsKey(path)?versions[path]+1:1; }
        public Task<JToken> ReadAsync(string p){return Task.FromResult(Get(p));}
        public Task WriteAsync(string p,object v){Put(p,JToken.FromObject(v));return Task.CompletedTask;}
        public Task DeleteAsync(string p){Put(p,null);return Task.CompletedTask;}
        public Task PatchAsync(string p,object v){foreach(var x in JObject.FromObject(v).Properties())Put(x.Name,x.Value);if(LoseResponse){LoseResponse=false;throw new Exception("lost acknowledgement");}return Task.CompletedTask;}
        public Task<StudentVersion> ReadVersionAsync(string p){return Task.FromResult(new StudentVersion{Value=Get(p) as JObject,ETag=versions.ContainsKey(p)?versions[p].ToString():"null_etag"});}
        public async Task<bool> CompareWriteAsync(string p,object v,string etag){var n=await ReadVersionAsync(p);if(n.ETag!=etag)return false;Put(p,v==null?null:JToken.FromObject(v));return true;}
    }
    private static async Task RunAcademicAssignmentTests()
    {
        await Test("department assignment validates professors/students, bulk persistence, duplicate and lost-response handling",async()=>{
            var db=new AssignmentDb();await db.WriteAsync("users/admin",new {role="coordinator",isActive=true});
            await db.WriteAsync("users/prof",new {role="academicSupervisor",isActive=true,academicSupervisorId="AC",departmentId="computerScience",fullName="Professor"});
            await db.WriteAsync("users/other",new {role="academicSupervisor",isActive=true,academicSupervisorId="OTHER",departmentId="softwareEngineering"});
            foreach(string id in new[]{"S1","S2"})await db.WriteAsync("users/"+id,new {role="student",isActive=true,studentId=id,majorId="computerScience",fullName="Student (Demo)"});
            await db.WriteAsync("users/S3",new {role="student",isActive=true,studentId="S3",majorId="softwareEngineering"});await db.WriteAsync("placements/P",new {studentId="S1",status="active",unrelated="preserved"});
            var service=new AcademicAssignmentService(db,uid=>Task.FromResult(true));
            Func<string,string[],string,JObject> input=(prof,ids,request)=>new JObject{["requestId"]=request,["name"]="Group",["department"]="computerScience",["supervisorId"]=prof,["studentIds"]=new JArray(ids)};
            await Reject<FirebaseException>(()=>service.SaveAsync("admin",input("OTHER",new[]{"S1"},"wrong-prof")));
            await Reject<FirebaseException>(()=>service.SaveAsync("admin",input("AC",new[]{"S3"},"wrong-student")));
            db.LoseResponse=true;var payload=input("AC",new[]{"S1","S2"},"bulk");var saved=await service.SaveAsync("admin",payload);Assert((bool)saved["saved"]);
            Assert((string)(await db.ReadAsync("users/S1/academicSupervisorId"))=="AC" && (string)(await db.ReadAsync("placements/P/academicSupervisorId"))=="AC");Assert((string)(await db.ReadAsync("placements/P/unrelated"))=="preserved");
            await service.SaveAsync("admin",payload);Assert((await db.ReadAsync("supervisorAssignments") as JObject).Count==2);
            await Reject<FirebaseException>(()=>service.SaveAsync("admin",input("AC",new[]{"S1"},"bulk")));
            await Reject<FirebaseException>(()=>service.SaveAsync("admin",input("AC",new[]{"S1"},"duplicate")));
            await Reject<FirebaseException>(()=>service.LoadAsync("prof"));var view=await service.LoadAsync("admin");Assert(!view["students"].ToString().Contains("(Demo)"));
        });

        await Test("new supervisor chooses one major, additional members persist, second major is rejected", async () => {
            var db = await GroupFixture(); var service = new AcademicAssignmentService(db, uid => Task.FromResult(true));
            var before = await service.LoadAsync("admin"); Assert(before["professors"].Any(p => (string)p["id"] == "AC" && (string)p["department"] == ""));
            var first = await service.SaveAsync("admin", GroupInput("create", "S1")); string groupId = (string)first["groupId"];
            Assert((string)(await db.ReadAsync("users/prof/assignedMajorId")) == "computerscience");
            var add = GroupInput("add", "S2"); add["groupId"] = groupId; await service.SaveAsync("admin", add);
            Assert(((JObject)await db.ReadAsync("groups/" + groupId + "/students")).Count == 2);
            var wrong = GroupInput("second-major", "S3"); wrong["department"] = "softwareEngineering";
            await Reject<FirebaseException>(() => service.SaveAsync("admin", wrong));
            Assert(await db.ReadAsync("groups/GRP-second-major") is JValue);
            var view = await service.LoadAsync("admin"); Assert((string)view["professors"].Single(p => (string)p["id"] == "AC")["department"] == "computerscience");
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", (JObject)db.Root["users"], (JObject)db.Root["groups"], (JObject)db.Root["supervisorAssignments"], new JObject()).OrderBy(x => x).SequenceEqual(new[] { "S1", "S2" }));
        });
        await Test("group validation rejects cross-college, disabled accounts, conflicting legacy links and occupied groups without partial writes", async () => {
            foreach (string scenario in new[] { "college", "disabled", "unfinished", "deleting", "legacy", "group", "empty", "reserved" }) {
                var db = await GroupFixture(); var service = new AcademicAssignmentService(db, uid => Task.FromResult(true));
                if (scenario == "college") { await db.WriteAsync("users/prof/collegeId", "C1"); await db.WriteAsync("users/S2/collegeId", "C2"); }
                if (scenario == "disabled") await db.WriteAsync("users/S2/isActive", false);
                if (scenario == "unfinished") await db.WriteAsync("users/S2/accountSetupStatus", "pending");
                if (scenario == "deleting") await db.WriteAsync("users/S2/deletionStatus", "pending");
                if (scenario == "legacy") await db.WriteAsync("assignments/old", new { studentId = "S2", academicSupervisorId = "OTHER", active = true });
                if (scenario == "group") await db.WriteAsync("groups/existing", new { department = "computerScience", academicSupervisorId = "OTHER", students = new { S2 = true } });
                var input = GroupInput(scenario, "S1", "S2");
                if (scenario == "empty") input["studentIds"] = new JArray();
                if (scenario == "reserved") input["requestId"] = "current";
                await Reject<FirebaseException>(() => service.SaveAsync("admin", input));
                Assert(db.Root["supervisorAssignments"] == null && db.Root["users"]["S1"]["academicSupervisorId"] == null);
                Assert(db.Root["academicAssignmentOperations"]?["current"] == null);
            }
        });
        await Test("academic monitor resolves UID aliases, ignores inactive links and excludes conflicting student ownership", async () => {
            var db = await GroupFixture();
            await db.WriteAsync("assignments/old", new { studentId = "S1", academicSupervisorId = "prof", active = true });
            await db.WriteAsync("supervisorAssignments/current", new { studentId = "S1", academicSupervisorId = "AC", active = true });
            var users = (JObject)db.Root["users"]; var groups = new JObject(); var current = (JObject)db.Root["supervisorAssignments"]; var legacy = (JObject)db.Root["assignments"];
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", users, groups, current, legacy).SequenceEqual(new[] { "S1" }));
            legacy["stale"] = JObject.FromObject(new { studentId = "S1", academicSupervisorId = "OTHER", active = false });
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", users, groups, current, legacy).Length == 1);
            legacy["stale"]["active"] = true;
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", users, groups, current, legacy).Length == 0);
            Assert(AcademicAssignmentService.AssignedStudentIds("admin", users, groups, current, new JObject()).Length == 0);
        });
        await Test("academic monitor includes placement assignments without groups and rejects conflicting or wrong UID placements", async () => {
            var db = await GroupFixture(); var users = (JObject)db.Root["users"];
            var placements = new JObject { ["P1"] = JObject.FromObject(new { studentId = "S1", studentUid = "S1", academicSupervisorId = "prof", status = "confirmed" }) };
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", users, new JObject(), new JObject(), new JObject(), placements).SequenceEqual(new[] { "S1" }));
            placements["P1"]["studentUid"] = "other-account";
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", users, new JObject(), new JObject(), new JObject(), placements).Length == 0);
            placements["P1"]["studentUid"] = "S1";
            var current = new JObject { ["A"] = JObject.FromObject(new { studentId = "S1", academicSupervisorId = "OTHER", isActive = true }) };
            Assert(AcademicAssignmentService.AssignedStudentIds("prof", users, new JObject(), current, new JObject(), placements).Length == 0);
            Assert(AcademicAssignmentService.AssignedStudentIds("admin", users, new JObject(), new JObject(), new JObject(), placements).Length == 0);
        });
        await Test("assignment reservations prevent competing writes and a non-coordinator cannot save", async () => {
            var db = await GroupFixture(); var service = new AcademicAssignmentService(db, uid => Task.FromResult(true));
            await db.WriteAsync("academicAssignmentOperations/current", new { requestId = "another", actorUid = "admin", digest = "pending" });
            await Reject<FirebaseException>(() => service.SaveAsync("admin", GroupInput("blocked", "S1")));
            Assert(db.Root["supervisorAssignments"] == null);
            await Reject<FirebaseException>(() => service.SaveAsync("prof", GroupInput("forbidden", "S1")));
            await Reject<FirebaseException>(() => new AcademicAssignmentService(db, uid => Task.FromResult(false)).SaveAsync("admin", GroupInput("inactive", "S1")));
        });
    }

    private static JObject GroupInput(string request, params string[] ids) {
        return new JObject { ["requestId"] = request, ["name"] = "CS Group", ["department"] = "computerScience", ["supervisorId"] = "AC", ["studentIds"] = new JArray(ids) };
    }
    private static async Task<AssignmentDb> GroupFixture() {
        var db = new AssignmentDb();
        await db.WriteAsync("users/admin", new { role = "coordinator", isActive = true });
        await db.WriteAsync("users/prof", new { role = "academicSupervisor", academicSupervisorId = "AC", fullName = "Professor", isActive = true });
        await db.WriteAsync("users/other", new { role = "academicSupervisor", academicSupervisorId = "OTHER", isActive = true });
        foreach (string id in new[] { "S1", "S2", "S3" }) await db.WriteAsync("users/" + id, new { role = "student", studentId = id, fullName = "Student " + id, majorId = id == "S3" ? "softwareEngineering" : "computerScience", isActive = true });
        return db;
    }
}
