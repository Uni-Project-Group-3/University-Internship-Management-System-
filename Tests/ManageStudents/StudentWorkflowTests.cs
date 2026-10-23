using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

internal static class StudentWorkflowTests
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static void Main() { Run().GetAwaiter().GetResult(); }
    static async Task Run()
    {
        var db = new MemoryDatabase(); var auth = new MemoryAuthentication(); var mail = new NoMail();
        var service = new StudentManagementService(db, auth, mail);
        var university = JObject.FromObject(new { studentId="9000001", fullName="Isolated Student", email="isolated@example.invalid", universityId="TEST", collegeId="IT", majorId="CS", requiredTrainingHours=300, isEligible=true });
        await db.WriteAsync("universityRecords/students/9000001", university);
        await db.WriteAsync("academicHistory/retained", new { value=true });
        await db.WriteAsync("users/coordinator", new { role="coordinator", isActive=true });
        auth.Accounts["coordinator"] = new StudentLoginAccount { Uid="coordinator", Email="coordinator@example.invalid" };
        Check(await service.RequireCoordinatorAsync("coordinator") == "coordinator", "active coordinator authorized");
        auth.Accounts["coordinator"].Disabled=true;
        try { await service.RequireCoordinatorAsync("coordinator"); throw new Exception("authorization accepted disabled account"); } catch (UnauthorizedAccessException) { Check(true,"disabled coordinator rejected"); }
        auth.Accounts["coordinator"].Disabled=false;
        Check((await service.LookupAsync("9000001")).fullName=="Isolated Student", "university ID lookup populates record");
        try { await service.LookupAsync("9000002"); throw new Exception("missing record accepted"); } catch (StudentManagementException) { Check(true,"missing university record rejected"); }
        string password = StudentManagementService.GeneratePassword();
        Check(password.Length >= 12 && password != StudentManagementService.GeneratePassword(), "temporary passwords generated independently");
        string uid = await service.CreateAsync("9000001", "", password, "coordinator", false);
        Check(auth.Accounts.ContainsKey(uid) && !auth.Accounts[uid].Disabled, "Confirm creates and enables Authentication login");
        var profile = await db.ReadAsync("users/"+uid);
        Check((string)profile["studentId"]=="9000001" && (string)profile["accountSetupStatus"]=="complete" && (bool)profile["mustChangePassword"], "Confirm saves matching complete platform profile");
        Check((await service.ListAsync()).Count==1, "list refresh includes created student");
        int creates=auth.Creates;
        try { await service.CreateAsync("9000001","",password,"coordinator",false); throw new Exception("duplicate accepted"); } catch(StudentManagementException) { Check(auth.Creates==creates,"duplicate account rejected without another Authentication create"); }
        auth.FailDelete=true;
        try { await service.DeleteAsync(uid,"coordinator"); throw new Exception("delete failure ignored"); } catch(InvalidOperationException) { Check(auth.Accounts.ContainsKey(uid) && (await db.ReadAsync("users/"+uid)).Type!=JTokenType.Null,"failed Authentication delete retains profile for retry"); }
        auth.FailDelete=false;
        await service.DeleteAsync(uid,"coordinator");
        Check(!auth.Accounts.ContainsKey(uid) && (await db.ReadAsync("users/"+uid)).Type==JTokenType.Null,"Delete removes Authentication account and platform profile");
        Check((await service.ListAsync()).Count==0,"list refresh excludes deleted student");
        Check(JToken.DeepEquals(university, await db.ReadAsync("universityRecords/students/9000001")) && (bool)(await db.ReadAsync("academicHistory/retained"))["value"],"Delete preserves university records and academic history");
        for(int cycle=0;cycle<3;cycle++)
        {
            uid=await service.CreateAsync("9000001","",StudentManagementService.GeneratePassword(),"coordinator",false);
            await service.DeleteAsync(uid,"coordinator");
            Check((string)(await db.ReadAsync("studentAccountOperations/"+uid))["state"]=="deleted" && (await service.ListAsync()).Count==0,"original add-delete cycle " +(cycle+1)+" tracks deletion and refreshes list");
        }
        await db.WriteAsync("studentAccountOperations/"+uid,new { kind="create",state="complete",studentId="9000001",universityId="TEST",email="isolated@example.invalid" });
        await service.CreateAsync("9000001","",StudentManagementService.GeneratePassword(),"coordinator",false);
        Check((await service.ListAsync()).Count==1,"re-add recovers historical marker when login and profile are absent");
        Console.WriteLine("All isolated Manage Students workflow checks passed; no network or email used.");
    }
    sealed class NoMail : IStudentCredentialMailer
    {
        public void ValidateConfiguration() { throw new Exception("unexpected mail"); }
        public Task SendWelcomeAsync(string a,string b,string c,string d) { throw new Exception("unexpected mail"); }
        public Task SendResetAsync(string a,string b,string c) { throw new Exception("unexpected mail"); }
    }
    sealed class MemoryAuthentication : IStudentAuthentication
    {
        public readonly Dictionary<string,StudentLoginAccount> Accounts=new Dictionary<string,StudentLoginAccount>();
        public int Creates; public bool FailDelete;
        public Task<StudentLoginAccount> FindAsync(string uid) { StudentLoginAccount a; Accounts.TryGetValue(uid,out a); return Task.FromResult(a); }
        public Task<StudentLoginAccount> FindByEmailAsync(string email) { return Task.FromResult(Accounts.Values.FirstOrDefault(a=>a.Email==email)); }
        public Task CreateAsync(string uid,string email,string name,string password) { Creates++; Accounts.Add(uid,new StudentLoginAccount { Uid=uid,Email=email,Disabled=true }); return Task.CompletedTask; }
        public Task UpdateAsync(string uid,bool disabled,string password=null) { Accounts[uid].Disabled=disabled; return Task.CompletedTask; }
        public Task DeleteAsync(string uid) { if(FailDelete) throw new InvalidOperationException("mock failure"); Accounts.Remove(uid); return Task.CompletedTask; }
        public Task<string> ResetLinkAsync(string email) { return Task.FromResult("mock"); }
    }
    sealed class MemoryDatabase : IStudentDatabase
    {
        public bool FailDelete;
        readonly JObject root=new JObject(); readonly Dictionary<string,int> versions=new Dictionary<string,int>();
        JToken Get(string path) { JToken n=root; foreach(var key in path.Split('/')) { n=(n as JObject)?[key]; if(n==null)return JValue.CreateNull(); } return n.DeepClone(); }
        void Set(string path,JToken value) { var keys=path.Split('/'); JObject n=root; for(int i=0;i<keys.Length-1;i++) { if(!(n[keys[i]] is JObject))n[keys[i]]=new JObject(); n=(JObject)n[keys[i]]; } if(value==null||value.Type==JTokenType.Null)n.Remove(keys.Last()); else n[keys.Last()]=value.DeepClone(); versions[path]=versions.ContainsKey(path)?versions[path]+1:1; }
        string Tag(string path) { return Get(path).Type==JTokenType.Null ? "null_etag" : (versions.ContainsKey(path)?versions[path].ToString():"null_etag"); }
        public Task<JToken> ReadAsync(string path) { return Task.FromResult(Get(path)); }
        public Task WriteAsync(string path,object value) { Set(path,JToken.FromObject(value)); return Task.CompletedTask; }
        public Task PatchAsync(string path,object value) { var n=Get(path) as JObject ?? new JObject(); foreach(var p in JObject.FromObject(value).Properties())n[p.Name]=p.Value; Set(path,n); return Task.CompletedTask; }
        public Task DeleteAsync(string path) { if(FailDelete) throw new InvalidOperationException("mock database delete failure"); Set(path,null); return Task.CompletedTask; }
        public Task<StudentVersion> ReadVersionAsync(string path) { return Task.FromResult(new StudentVersion { Value=Get(path) as JObject,ETag=Tag(path) }); }
        public Task<bool> CompareWriteAsync(string path,object value,string etag) { if(Tag(path)!=etag)return Task.FromResult(false); Set(path,JToken.FromObject(value)); return Task.FromResult(true); }
    }
}