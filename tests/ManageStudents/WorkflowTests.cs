using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

internal static class WorkflowTests
{
    private static int passed;
    private static readonly DateTimeOffset Clock = DateTimeOffset.Parse("2026-10-22T10:00:00Z");
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Reject(Func<Task> action) { try { await action(); } catch { return; } throw new Exception("Expected rejection"); }
    private static async Task Case(string name, Func<Task> action) { await action(); passed++; Console.WriteLine("PASS " + name); }
    public static int Main() { try { Run().GetAwaiter().GetResult(); CompanyWorkflowTests.Run().GetAwaiter().GetResult(); AcademicWorkflowTests.Run().GetAwaiter().GetResult(); LoginRouteTests.Run(); Console.WriteLine(passed + " workflow checks passed; no network calls."); return 0; } catch (Exception e) { Console.WriteLine("FAIL " + e); return 1; } }

    private static async Task Run()
    {
        await Case("Firebase version headers survive .NET parsing unchanged", async () => {
            foreach (string tag in new[] { "firebase-version=", "\"quoted-version\"", "null_etag" })
            {
                using (var response = new HttpResponseMessage())
                {
                    response.Headers.TryAddWithoutValidation("ETag", tag);
                    Assert(FirebaseDatabaseVersion.ReadETag(response) == tag, "version altered or discarded");
                }
            }
            using (var missing = new HttpResponseMessage())
                await Reject(() => { FirebaseDatabaseVersion.ReadETag(missing); return Task.CompletedTask; });
        });
        await Case("coordinator identity and current role", async () => {
            var f = new Fixture(); Assert(await f.Service.RequireCoordinatorAsync("coordinator1") == "coordinator1", "role");
            await Reject(() => f.Service.RequireCoordinatorAsync("academic1"));
            await f.Db.PatchAsync("users/coordinator1", new { isActive = false });
            await Reject(() => f.Service.RequireCoordinatorAsync("coordinator1"));
        });
        await Case("eligibility, complete record and matching student ID", async () => {
            var f = new Fixture(); Assert((await f.Service.LookupAsync("2023001")).universityId == "just", "university");
            await Reject(() => f.Service.LookupAsync("../users"));
            await f.Db.PatchAsync("universityRecords/students/2023001", new { isEligible = false });
            await Reject(() => f.Service.LookupAsync("2023001")); Assert(f.Auth.Created == 0, "unexpected account");
        });
        await Case("mail configuration fails before mutations", async () => {
            var f = new Fixture(); f.Mail.Configured = false;
            await Reject(() => f.Create()); Assert(f.Auth.Created == 0 && f.Db.Mutations == 0, "mutated without mail config");
        });
        await Case("explicit no-email creation saves active student without sending mail", async () => {
            var f = new Fixture(); f.Mail.Configured = false;
            string password = StudentManagementService.GeneratePassword();
            string uid = await f.Service.CreateAsync("2023001", "", password, "coordinator1", false);
            var profile = (JObject)await f.Db.ReadAsync("users/" + uid);
            Assert((string)profile["role"] == "student" && (bool)profile["isActive"], "student not active");
            Assert((bool)profile["mustChangePassword"] && (string)profile["accountSetupStatus"] == "complete", "setup flags");
            Assert(!f.Auth.Accounts[uid].Disabled && f.Auth.Passwords[uid] == password, "login mismatch");
            Assert(f.Mail.Welcome == 0 && !f.Db.Root.ToString().Contains(password), "email sent or password stored");
            var operation = (JObject)await f.Db.ReadAsync("studentAccountOperations/" + uid);
            Assert((string)operation["credentialDelivery"] == "coordinator" && operation.Value<bool?>("mailAccepted") != true, "false email claim");
            await Reject(() => f.Service.CreateAsync("2023001", "", password, "coordinator1", false));
            Assert(f.Auth.Created == 1, "duplicate account");
        });
        await Case("no-email activation retry uses current temporary password", async () => {
            var f = new Fixture(); f.Mail.Configured = false; f.Auth.FailActivation = true;
            await Reject(() => f.Service.CreateAsync("2023001", "", "A9temporaryPwd", "coordinator1", false));
            string uid = await f.Service.CreateAsync("2023001", "", "B9replacementPwd", "coordinator1", false);
            Assert(f.Auth.Created == 1 && f.Auth.Passwords[uid] == "B9replacementPwd" && !f.Auth.Accounts[uid].Disabled, "retry mismatch");
            Assert(f.Mail.Welcome == 0, "email sent on retry");
        });
        await Case("department and active supervisor validation", async () => {
            var f = new Fixture(); await f.Db.PatchAsync("users/academic1", new { majorId = "computerScience" });
            await Reject(() => f.Create()); Assert(f.Auth.Created == 0, "wrong supervisor accepted");
        });
        await Case("creation without an initial supervisor", async () => {
            var f = new Fixture();
            await f.Db.DeleteAsync("users/academic1");
            string uid = await f.Service.CreateAsync("2023001", "", "A9temporaryPwd", "coordinator1");
            var profile = (JObject)await f.Db.ReadAsync("users/" + uid);
            Assert((string)profile["academicSupervisorId"] == "", "unexpected assignment");
            Assert((bool)profile["isActive"] && f.Mail.Welcome == 1, "unassigned creation failed");
            Assert((await f.Service.ListAsync()).Single().Supervisor == "Unassigned", "list assignment");
        });
        await Case("creation, matching IDs, email and no stored password", async () => {
            var f = new Fixture(); string uid = await f.Create();
            var p = (JObject)await f.Db.ReadAsync("users/" + uid);
            Assert((string)p["studentId"] == "2023001" && (string)p["universityId"] == "just", "ID mapping");
            Assert((string)p["academicSupervisorId"] == "academic1" && (bool)p["mustChangePassword"], "profile fields");
            Assert((bool)p["isActive"] && !f.Auth.Accounts[uid].Disabled, "inactive after success");
            Assert(f.Mail.Welcome == 1 && f.Mail.Password == f.Auth.Passwords[uid], "password mismatch");
            Assert(!f.Db.Root.ToString().Contains(f.Mail.Password), "password stored in database");
            await Reject(() => f.Create()); Assert(f.Auth.Created == 1, "duplicate creation");
        });
        await Case("failed mail keeps account disabled and retry recovers", async () => {
            var f = new Fixture(); f.Mail.FailOnce = true; await Reject(() => f.Create());
            Assert(f.Auth.Accounts.Values.Single(a => a.Uid.StartsWith("student_")).Disabled, "enabled before delivery");
            await f.Create(); Assert(f.Auth.Created == 1 && !f.Auth.Accounts.Values.Single(a => a.Uid.StartsWith("student_")).Disabled, "retry duplicated account");
        });
        await Case("lost authentication reply recovers with deterministic UID", async () => {
            var f = new Fixture(); f.Auth.LoseCreateReply = true; await Reject(() => f.Create());
            await f.Create(); Assert(f.Auth.Created == 1, "ambiguous create duplicated account");
        });
        await Case("activation retry does not resend accepted credentials", async () => {
            var f = new Fixture(); f.Auth.FailActivation = true; await Reject(() => f.Create());
            string password = f.Mail.Password; await f.Create("Z9differentPwd");
            Assert(f.Mail.Welcome == 1 && f.Auth.Passwords.Values.Single() == password, "accepted password changed");
        });
        await Case("unrelated login is never adopted on retry", async () => {
            var f = new Fixture(); string uid = StudentManagementService.ManagedUid("just", "2023001");
            f.Auth.Accounts[uid] = new StudentLoginAccount {Uid=uid, Email="student@example.com", Disabled=true};
            await Reject(() => f.Create()); await Reject(() => f.Create()); Assert(f.Mail.Welcome == 0, "adopted login");
        });
        await Case("live operation lease prevents duplicate processing", async () => {
            var f = new Fixture(); string uid = StudentManagementService.ManagedUid("just", "2023001");
            f.Db.Set("studentAccountOperations/" + uid, JObject.FromObject(new {kind="create",state="pending",studentId="2023001",universityId="just",email="student@example.com",leaseUntilUtc=Clock.AddMinutes(3).ToString("o")}));
            await Reject(() => f.Create()); Assert(f.Auth.Created == 0, "lease ignored");
        });
        await Case("password reset targets real Authentication email", async () => {
            var f = new Fixture(); string uid = await f.Create(); f.Auth.Accounts[uid].Email = "actual@example.com";
            await f.Service.ResetPasswordAsync(uid); Assert(f.Mail.ResetEmail == "actual@example.com", "wrong reset target");
            await Reject(() => f.Service.ResetPasswordAsync("coordinator1"));
        });
        await Case("deletion retry preserves academic data", async () => {
            var f = new Fixture(); string uid = await f.Create();
            f.Db.Set("evaluations/history", new JObject { ["studentUid"] = uid, ["score"] = 88 });
            f.Db.FailProfileDeleteOnce = true; await Reject(() => f.Service.DeleteAsync(uid, "coordinator1"));
            Assert(!f.Auth.Accounts.ContainsKey(uid), "login not deleted");
            await f.Service.DeleteAsync(uid, "coordinator1");
            Assert((await f.Db.ReadAsync("users/" + uid)).Type == JTokenType.Null, "profile not deleted");
            Assert((await f.Db.ReadAsync("evaluations/history")).Type == JTokenType.Object, "academic data deleted");
            Assert((await f.Db.ReadAsync("universityRecords/students/2023001")).Type == JTokenType.Object, "university data deleted");
            await Reject(() => f.Service.DeleteAsync("coordinator1", "coordinator1"));
        });
        await Case("delete a no-email student removes login and profile and retains records", async () => {
            var f = new Fixture(); f.Mail.Configured = false;
            string uid = await f.Service.CreateAsync("2023001", "", "A9temporaryPwd", "coordinator1", false);
            f.Db.Set("placements/history", JObject.FromObject(new { studentUid = uid, completionStatus = "InTraining" }));
            await f.Service.DeleteAsync(uid, "coordinator1");
            Assert(!f.Auth.Accounts.ContainsKey(uid), "login retained");
            Assert((await f.Db.ReadAsync("users/" + uid)).Type == JTokenType.Null, "profile retained");
            Assert((await f.Db.ReadAsync("universityRecords/students/2023001")).Type == JTokenType.Object, "official record removed");
            Assert((await f.Db.ReadAsync("placements/history")).Type == JTokenType.Object, "history removed");
            Assert((await f.Db.ReadAsync("users/coordinator1")).Type == JTokenType.Object, "coordinator removed");
        });
        await Case("completion mapping never guesses missing or ambiguous placement", () => {
            var users = new JObject(); var p = new JObject { ["role"]="student", ["isActive"]=true };
            Assert(StudentManagementService.ToListRecord("s1",p,users,new JObject()).CompletionStatus=="Not placed", "missing placement");
            var placements = JObject.Parse("{a:{studentUid:'s1',completionStatus:'PendingStudentConfirmation'}}");
            Assert(StudentManagementService.ToListRecord("s1",p,users,placements).CompletionStatus=="Pending Student Confirmation", "pending mapping");
            placements["b"] = placements["a"].DeepClone();
            Assert(StudentManagementService.ToListRecord("s1",p,users,placements).CompletionStatus=="Status unavailable", "ambiguous mapping");
            return Task.CompletedTask;
        });
        await Case("password generator and invalid Firebase keys", async () => {
            for(int i=0;i<300;i++) { string p=StudentManagementService.GeneratePassword(); Assert(p.Length==12 && p.Any(char.IsUpper) && p.Any(char.IsLower) && p.Any(char.IsDigit), "weak generation"); }
            await Reject(() => { StudentManagementService.Key("../users"); return Task.CompletedTask; });
        });
    }

    private sealed class Fixture
    {
        public FakeDb Db = new FakeDb(); public FakeAuth Auth = new FakeAuth(); public FakeMail Mail = new FakeMail();
        public StudentManagementService Service;
        public Fixture()
        {
            Db.Set("users/coordinator1", JObject.Parse("{fullName:'Coordinator',role:'coordinator',isActive:true}"));
            Db.Set("users/academic1", JObject.Parse("{fullName:'Academic',role:'academicSupervisor',isActive:true,majorId:'softwareEngineering',universityId:'just',collegeId:'cit'}"));
            Db.Set("universityRecords/students/2023001", JObject.Parse("{studentId:'2023001',fullName:'Student',email:'student@example.com',universityId:'just',collegeId:'cit',majorId:'softwareEngineering',requiredTrainingHours:240,isEligible:true}"));
            Auth.Accounts["coordinator1"] = new StudentLoginAccount{Uid="coordinator1",Email="coordinator@example.com"};
            Auth.Accounts["academic1"] = new StudentLoginAccount{Uid="academic1",Email="academic@example.com"};
            Service=new StudentManagementService(Db,Auth,Mail,()=>Clock);
        }
        public Task<string> Create(string password="A7validSecret") { return Service.CreateAsync("2023001","academic1",password,"coordinator1"); }
    }
    private sealed class FakeDb : IStudentDatabase
    {
        public JObject Root = new JObject(); public int Mutations; public bool FailProfileDeleteOnce;
        private JToken Get(string path) { JToken n=Root; foreach(var part in path.Split('/')) { n=(n as JObject)?[Uri.UnescapeDataString(part)]; if(n==null)return JValue.CreateNull(); } return n; }
        public void Set(string path,JToken value) { var parts=path.Split('/'); var n=Root; for(int i=0;i<parts.Length-1;i++){string k=Uri.UnescapeDataString(parts[i]);if(!(n[k] is JObject))n[k]=new JObject();n=(JObject)n[k];} n[Uri.UnescapeDataString(parts.Last())]=value.DeepClone(); }
        private string Tag(string path) { var n=Get(path); return n.Type==JTokenType.Null ? "null_etag" : n.ToString(Newtonsoft.Json.Formatting.None); }
        public Task<JToken> ReadAsync(string path) { return Task.FromResult(Get(path).DeepClone()); }
        public Task WriteAsync(string path,object value) { Mutations++;Set(path,JToken.FromObject(value)); return Task.CompletedTask; }
        public Task PatchAsync(string path,object value) { var n=Get(path) as JObject ?? new JObject();n=(JObject)n.DeepClone();foreach(var p in JObject.FromObject(value).Properties()){if(p.Value.Type==JTokenType.Null)n.Remove(p.Name);else n[p.Name]=p.Value.DeepClone();}return WriteAsync(path,n); }
        public Task DeleteAsync(string path) { if(path.StartsWith("users/")&&FailProfileDeleteOnce){FailProfileDeleteOnce=false;throw new TimeoutException();} Mutations++;Set(path,JValue.CreateNull());return Task.CompletedTask; }
        public Task<StudentVersion> ReadVersionAsync(string path) { return Task.FromResult(new StudentVersion{Value=Get(path).DeepClone() as JObject,ETag=Tag(path)}); }
        public async Task<bool> CompareWriteAsync(string path,object value,string tag) { if(Tag(path)!=tag)return false;await WriteAsync(path,value);return true; }
    }
    private sealed class FakeAuth : IStudentAuthentication
    {
        public Dictionary<string,StudentLoginAccount> Accounts=new Dictionary<string,StudentLoginAccount>();
        public Dictionary<string,string> Passwords=new Dictionary<string,string>();
        public int Created; public bool LoseCreateReply,FailActivation;
        public Task<StudentLoginAccount> FindAsync(string uid) { StudentLoginAccount a;Accounts.TryGetValue(uid,out a);return Task.FromResult(a); }
        public Task<StudentLoginAccount> FindByEmailAsync(string email) { return Task.FromResult(Accounts.Values.FirstOrDefault(a=>a.Email==email)); }
        public Task CreateAsync(string uid,string email,string name,string password) { if(Accounts.ContainsKey(uid))throw new Exception("duplicate");Created++;Accounts[uid]=new StudentLoginAccount{Uid=uid,Email=email,Disabled=true};Passwords[uid]=password;if(LoseCreateReply){LoseCreateReply=false;throw new TimeoutException();}return Task.CompletedTask; }
        public Task UpdateAsync(string uid,bool disabled,string password=null) { if(!disabled&&FailActivation){FailActivation=false;throw new TimeoutException();}Accounts[uid].Disabled=disabled;if(password!=null)Passwords[uid]=password;return Task.CompletedTask; }
        public Task DeleteAsync(string uid) { Accounts.Remove(uid);Passwords.Remove(uid);return Task.CompletedTask; }
        public Task<string> ResetLinkAsync(string email) { return Task.FromResult("https://example.com/reset?test=1"); }
    }
    private sealed class FakeMail : IStudentCredentialMailer
    {
        public bool Configured=true,FailOnce; public int Welcome; public string Password,ResetEmail;
        public void ValidateConfiguration(){if(!Configured)throw new StudentManagementException("No mail");}
        public Task SendWelcomeAsync(string email,string name,string studentId,string password) { if(FailOnce){FailOnce=false;throw new TimeoutException();}Welcome++;Password=password;return Task.CompletedTask; }
        public Task SendResetAsync(string email,string name,string link) { ResetEmail=email;return Task.CompletedTask; }
    }
}
