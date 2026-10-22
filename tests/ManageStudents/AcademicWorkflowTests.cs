using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

internal static class AcademicWorkflowTests
{
    private static int passed;
    private static readonly DateTimeOffset Clock=DateTimeOffset.Parse("2026-10-22T10:00:00Z");
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static async Task Reject(Func<Task> action){try{await action();}catch{return;}throw new Exception("Expected rejection");}
    private static async Task Case(string name,Func<Task> action){await action();passed++;Console.WriteLine("PASS academic: "+name);}
    private sealed class Fixture
    {
        public CompanyWorkflowTests.Db Db=new CompanyWorkflowTests.Db();public CompanyWorkflowTests.Auth Auth=new CompanyWorkflowTests.Auth();
        public AcademicSupervisorService Service;
        public Fixture(){Db.Set("universityRecords/academicSupervisors/A1001",JObject.Parse("{staffId:'A1001',fullName:'Dr. Sara Hassan',email:'sara.academic@example.com',universityId:'just',collegeId:'cit',departmentId:'softwareEngineering'}"));Service=new AcademicSupervisorService(Db,Auth,()=>Clock);}
        public string Uid {get{return AcademicSupervisorService.ManagedUid("just","A1001");}}
        public async Task<string> Create(string password="A9temporaryPwd"){return await Service.CreateAsync(await Service.LookupAsync("A1001"),password,"coordinator");}
    }
    internal static async Task Run()
    {
        await Case("official staff schema is read from exact university path",async()=>{
            var f=new Fixture();var r=await f.Service.LookupAsync("A1001");Assert(r.StaffId=="A1001"&&r.DepartmentId=="softwareEngineering","wrong source");
            await Reject(()=>f.Service.LookupAsync("A1002"));await Reject(()=>f.Service.LookupAsync("../users"));
            f.Db.Set("universityRecords/academicSupervisors/A1001",new JObject{["staffId"]="different"});await Reject(()=>f.Service.LookupAsync("A1001"));Assert(f.Auth.Created==0,"unexpected account");
        });
        await Case("official records reject inactive eligibility and malformed email",async()=>{
            var f=new Fixture();await f.Db.PatchAsync("universityRecords/academicSupervisors/A1001",new{isActive=false});await Reject(()=>f.Create());
            await f.Db.PatchAsync("universityRecords/academicSupervisors/A1001",new{isActive=true,email="broken"});await Reject(()=>f.Create());
            Assert(f.Auth.Created==0,"ineligible record provisioned");
        });
        await Case("client cannot replace official staff details or stale lookup",async()=>{
            var f=new Fixture();var expected=await f.Service.LookupAsync("A1001");expected.FullName="Tampered";await Reject(()=>f.Service.CreateAsync(expected,"A9temporaryPwd","coordinator"));
            expected=await f.Service.LookupAsync("A1001");await f.Db.PatchAsync("universityRecords/academicSupervisors/A1001",new{departmentId="computerScience"});
            await Reject(()=>f.Service.CreateAsync(expected,"A9temporaryPwd","coordinator"));Assert(f.Auth.Created==0,"stale record used");
        });
        await Case("creation links Authentication UID, official staff ID and academic role",async()=>{
            var f=new Fixture();string uid=await f.Create();var p=f.Db.Get("users/"+uid);
            Assert(uid==f.Uid&&(string)p["staffId"]=="A1001"&&(string)p["role"]=="academicSupervisor","identity mapping");
            Assert((bool)p["isActive"]&&(bool)p["mustChangePassword"]&&!f.Auth.Users[uid].Disabled,"flags");
            Assert((string)p["departmentId"]=="softwareEngineering"&&(string)p["majorId"]=="softwareEngineering","assignment compatibility");
            Assert(!f.Db.Root.ToString().Contains("A9temporaryPwd"),"password stored");
            await Reject(()=>f.Create());Assert(f.Auth.Created==1,"duplicate account");
        });
        await Case("existing login and duplicate official email are not adopted",async()=>{
            var f=new Fixture();f.Auth.Users["unrelated"]=new StudentLoginAccount{Uid="unrelated",Email="sara.academic@example.com"};await Reject(()=>f.Create());Assert(f.Auth.Created==0&&f.Db.Mutations==0,"email adopted");
            f=new Fixture();f.Auth.Users[f.Uid]=new StudentLoginAccount{Uid=f.Uid,Email="sara.academic@example.com"};await Reject(()=>f.Create());await Reject(()=>f.Create());Assert(f.Db.Mutations==0,"unowned UID adopted");
        });
        await Case("lost Auth response resumes without a duplicate or stale password",async()=>{
            var f=new Fixture();f.Auth.LoseCreateReply=true;await Reject(()=>f.Create());await f.Create("B9replacementPwd");
            Assert(f.Auth.Created==1&&f.Auth.Passwords[f.Uid]=="B9replacementPwd"&&!f.Auth.Users[f.Uid].Disabled,"recovery failed");
        });
        await Case("academic supervisors are compatible with student assignment matching",async()=>{
            var f=new Fixture();await f.Create();var students=new StudentManagementService(f.Db,f.Auth,null);var list=await students.SupervisorsAsync();
            Assert(list.Count==1&&StudentManagementService.SupervisorMatches(list[0],new University_Internship_Management_System.UniversityStudentRecord{universityId="just",collegeId="cit",majorId="softwareEngineering"}),"department matcher");
        });
        await Case("deactivate and activate synchronize profile and Auth",async()=>{
            var f=new Fixture();await f.Create();await f.Service.ActAsync(f.Uid,"disable","coordinator");Assert(f.Auth.Users[f.Uid].Disabled&&!(bool)f.Db.Get("users/"+f.Uid)["isActive"],"disable");
            await f.Service.ActAsync(f.Uid,"enable","coordinator");Assert(!f.Auth.Users[f.Uid].Disabled&&(bool)f.Db.Get("users/"+f.Uid)["isActive"],"enable");
            await f.Db.PatchAsync("universityRecords/academicSupervisors/A1001",new{departmentId="computerScience"});await Reject(()=>f.Service.ActAsync(f.Uid,"enable","coordinator"));
        });
        await Case("sync takes updated details only from official source and preserves history",async()=>{
            var f=new Fixture();await f.Create();await f.Db.PatchAsync("users/"+f.Uid,new{customHistory="retained",mustChangePassword=false});
            await f.Db.PatchAsync("universityRecords/academicSupervisors/A1001",new{fullName="Updated Name",email="new.academic@example.com",departmentId="computerScience"});
            await f.Service.ActAsync(f.Uid,"sync","coordinator");Assert(f.Auth.Users[f.Uid].Email=="new.academic@example.com","Auth email stale");
            var p=f.Db.Get("users/"+f.Uid);Assert((string)p["departmentId"]=="computerScience"&&(string)p["customHistory"]=="retained"&&!(bool)p["mustChangePassword"],"sync overwrote unrelated fields");
        });
        await Case("active assignments prevent department moves and deletion",async()=>{
            var f=new Fixture();await f.Create();f.Db.Set("users/student",new JObject{["role"]="student",["isActive"]=true,["academicSupervisorId"]=f.Uid});
            await Reject(()=>f.Service.ActAsync(f.Uid,"delete","coordinator"));await f.Db.PatchAsync("universityRecords/academicSupervisors/A1001",new{departmentId="computerScience"});
            await Reject(()=>f.Service.ActAsync(f.Uid,"sync","coordinator"));Assert(f.Auth.Users.ContainsKey(f.Uid),"assigned login deleted");
            f.Db.Set("users/student",JValue.CreateNull());f.Db.Set("groups/active",new JObject{["supervisorId"]=f.Uid});await Reject(()=>f.Service.ActAsync(f.Uid,"delete","coordinator"));
        });
        await Case("deletion retry removes login/profile and preserves university/history",async()=>{
            var f=new Fixture();await f.Create();f.Db.Set("groups/history",new JObject{["supervisorId"]=f.Uid,["isActive"]=false});f.Db.FailDelete=true;
            await Reject(()=>f.Service.ActAsync(f.Uid,"delete","coordinator"));await f.Service.ActAsync(f.Uid,"delete","coordinator");
            Assert(!f.Auth.Users.ContainsKey(f.Uid)&&f.Db.Get("users/"+f.Uid).Type==JTokenType.Null,"profile remains");
            Assert(f.Db.Get("universityRecords/academicSupervisors/A1001").Type==JTokenType.Object&&f.Db.Get("groups/history").Type==JTokenType.Object,"history removed");
        });
        await Case("directory counts real group/student references",async()=>{
            var f=new Fixture();await f.Create();f.Db.Set("groups/active",new JObject{["academicSupervisorId"]=f.Uid});f.Db.Set("groups/old",new JObject{["supervisorId"]=f.Uid,["isActive"]=false});
            f.Db.Set("users/student",new JObject{["role"]="student",["isActive"]=true,["academicSupervisorId"]=f.Uid});
            var item=(await f.Service.DirectoryAsync())["supervisors"][0];Assert((int)item["groupCount"]==1&&(int)item["studentCount"]==1,"fabricated counts");
        });
        await Case("wrong roles, self changes and concurrent leases are rejected",async()=>{
            var f=new Fixture();await f.Create();f.Db.Set("users/student",new JObject{["role"]="student"});await Reject(()=>f.Service.ActAsync("student","delete","coordinator"));
            await Reject(()=>f.Service.ActAsync(f.Uid,"disable",f.Uid));await f.Db.PatchAsync("academicAccountOperations/"+f.Uid,new{leaseUntilUtc=Clock.AddMinutes(1).ToString("o")});
            await Reject(()=>f.Service.ActAsync(f.Uid,"disable","coordinator"));Assert(!f.Auth.Users[f.Uid].Disabled,"lease ignored");
        });
        Console.WriteLine(passed+" academic workflow checks passed; no network calls.");
    }
}
