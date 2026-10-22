using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

internal static class CompanyWorkflowTests
{
    private static int passed;
    private static readonly DateTimeOffset Clock = DateTimeOffset.Parse("2026-10-22T10:00:00Z");
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Reject(Func<Task> work) { try { await work(); } catch { return; } throw new Exception("Expected rejection"); }
    private static async Task Case(string name, Func<Task> work) { await work(); passed++; Console.WriteLine("PASS company: " + name); }
    private sealed class Fixture
    {
        public Db Database = new Db(); public Auth Accounts = new Auth(); public CompanyManagementService Service;
        public CompanyInput Form = new CompanyInput { Id = new string('a',32), RequestId = new string('b',32), Name = "Example Company", Industry = "Software", Email = "office@example.com", Website = "https://example.com", SupervisorName = "Supervisor", SupervisorEmail = "supervisor@example.com" };
        public Fixture() { Service = new CompanyManagementService(Database,Accounts,()=>Clock); }
        public Task<JObject> Save() { return Service.SaveAsync(Form,"coordinator"); }
        public string Uid { get { return "company_" + new string('b',32); } }
        public void PrepareEdit()
        {
            Form.Version = (string)Database.Get("companies/"+Form.Id)["revision"];
            Form.RequestId = Guid.NewGuid().ToString("N"); Form.SupervisorUid = Uid;
        }
    }
    internal static async Task Run()
    {
        await Case("manual creation links companies, Auth and users without storing passwords", async()=>{
            var f = new Fixture(); var result=await f.Save(); string password=(string)result["temporaryPassword"];
            var profile=f.Database.Get("users/"+f.Uid);
            Assert((string)profile["role"]=="companySupervisor"&&(string)profile["companyId"]==f.Form.Id,"link/role");
            Assert((bool)profile["isActive"]&&(bool)profile["mustChangePassword"]&&!f.Accounts.Users[f.Uid].Disabled,"flags");
            Assert(f.Accounts.Passwords[f.Uid]==password&&!f.Database.Root.ToString().Contains(password),"password leaked/mismatch");
            Assert((bool)f.Database.Get("companies/"+f.Form.Id)["isActive"],"company inactive");
            Assert(((JArray)(await f.Service.DirectoryAsync())["companies"]).Count==1,"directory");
            await f.Save();Assert(f.Accounts.Created==1,"duplicate request created login");
        });
        await Case("invalid form and duplicate email have no mutations",async()=>{
            var f=new Fixture();f.Form.SupervisorEmail="broken";await Reject(()=>f.Save());Assert(f.Database.Mutations==0,"invalid write");
            f.Form.SupervisorEmail="existing@example.com";f.Accounts.Users["other"]=new StudentLoginAccount{Uid="other",Email=f.Form.SupervisorEmail};
            await Reject(()=>f.Save());Assert(f.Accounts.Created==0&&f.Database.Mutations==0,"adopted email");
            f.Form.SupervisorEmail="valid@example.com";f.Form.Website="javascript:alert(1)";await Reject(()=>f.Save());
        });
        await Case("lost Auth create response recovers with one account and current password",async()=>{
            var f=new Fixture();f.Accounts.LoseCreateReply=true;await Reject(()=>f.Save());var result=await f.Save();
            Assert(f.Accounts.Created==1&&f.Accounts.Passwords[f.Uid]==(string)result["temporaryPassword"],"retry password");
            Assert((bool)f.Database.Get("companies/"+f.Form.Id)["isActive"],"recovered company not active");
        });
        await Case("pending setup appears with resume data and blocks unrelated actions",async()=>{
            var f=new Fixture();f.Accounts.LoseCreateReply=true;await Reject(()=>f.Save());
            var company=(await f.Service.DirectoryAsync())["companies"][0];Assert(company["pendingForm"]!=null,"no recovery form");
            await Reject(()=>f.Service.ActAsync(f.Form.Id,"","activateCompany","coordinator"));await f.Save();
        });
        await Case("edit synchronizes supervisor email and preserves unrelated data",async()=>{
            var f=new Fixture();await f.Save();await f.Database.PatchAsync("users/"+f.Uid,new {customHistory="retained"});f.PrepareEdit();
            f.Form.Name="Changed Company";f.Form.SupervisorEmail="new@example.com";f.Form.SupervisorName="Updated Name";await f.Save();
            Assert(f.Accounts.Users[f.Uid].Email==f.Form.SupervisorEmail,"Auth email stale");
            Assert((string)f.Database.Get("users/"+f.Uid)["email"]==f.Form.SupervisorEmail,"profile email stale");
            Assert((string)f.Database.Get("users/"+f.Uid)["customHistory"]=="retained","history overwritten");
        });
        await Case("stale company edit is rejected before changing Auth",async()=>{
            var f=new Fixture();await f.Save();f.PrepareEdit();await f.Database.PatchAsync("companies/"+f.Form.Id,new {revision="changed"});
            f.Form.SupervisorEmail="wrong@example.com";await Reject(()=>f.Save());Assert(f.Accounts.Users[f.Uid].Email!="wrong@example.com","stale changed auth");
        });
        await Case("company status and supervisor login status are separate",async()=>{
            var f=new Fixture();await f.Save();await f.Service.ActAsync(f.Form.Id,"","deactivateCompany","coordinator");
            Assert(!(bool)f.Database.Get("companies/"+f.Form.Id)["isActive"]&&!f.Accounts.Users[f.Uid].Disabled,"company toggled account");
            await f.Service.ActAsync(f.Form.Id,f.Uid,"disable","coordinator");Assert(f.Accounts.Users[f.Uid].Disabled&&!(bool)f.Database.Get("users/"+f.Uid)["isActive"],"disable failed");
            await f.Service.ActAsync(f.Form.Id,f.Uid,"enable","coordinator");Assert(!f.Accounts.Users[f.Uid].Disabled,"enable failed");
            Assert(!(bool)f.Database.Get("companies/"+f.Form.Id)["isActive"],"account toggled company");
        });
        await Case("removed password-reset action is rejected",async()=>{
            var f=new Fixture();await f.Save();string password=f.Accounts.Passwords[f.Uid];
            await Reject(()=>f.Service.ActAsync(f.Form.Id,f.Uid,"reset","coordinator"));
            Assert(f.Accounts.Passwords[f.Uid]==password,"removed action changed credentials");
        });
        await Case("supervisor deletion preserves company, other supervisors and placement history",async()=>{
            var f=new Fixture();await f.Save();f.Database.Set("placements/history",new JObject{["companyId"]=f.Form.Id,["companySupervisorId"]=f.Uid});
            f.Form.RequestId=new string('c',32);f.Form.Version=(string)f.Database.Get("companies/"+f.Form.Id)["revision"];f.Form.SupervisorEmail="second@example.com";await f.Save();
            string second="company_"+new string('c',32);await f.Service.ActAsync(f.Form.Id,f.Uid,"delete","coordinator");
            Assert(!f.Accounts.Users.ContainsKey(f.Uid)&&f.Database.Get("users/"+f.Uid).Type==JTokenType.Null,"not deleted");
            Assert(f.Accounts.Users.ContainsKey(second)&&f.Database.Get("companies/"+f.Form.Id).Type==JTokenType.Object&&f.Database.Get("placements/history").Type==JTokenType.Object,"history/other account removed");
        });
        await Case("partial deletion can retry after Auth account is already gone",async()=>{
            var f=new Fixture();await f.Save();f.Database.FailDelete=true;await Reject(()=>f.Service.ActAsync(f.Form.Id,f.Uid,"delete","coordinator"));
            await f.Service.ActAsync(f.Form.Id,f.Uid,"delete","coordinator");Assert(f.Database.Get("users/"+f.Uid).Type==JTokenType.Null,"retry profile remains");
        });
        await Case("cross-company and non-company account changes are rejected",async()=>{
            var f=new Fixture();await f.Save();f.Database.Set("users/student",new JObject{["role"]="student",["companyId"]=f.Form.Id});
            await Reject(()=>f.Service.ActAsync(f.Form.Id,"student","delete","coordinator"));
            f.Database.Set("companies/other",new JObject());await Reject(()=>f.Service.ActAsync("other",f.Uid,"disable","coordinator"));
            Assert(!f.Accounts.Users[f.Uid].Disabled,"wrong account changed");
        });
        await Case("archiving disables linked accounts and restore does not silently grant access",async()=>{
            var f=new Fixture();await f.Save();f.Database.Set("placements/history",new JObject{["companyId"]=f.Form.Id});
            await f.Service.ActAsync(f.Form.Id,"","archiveCompany","coordinator");
            Assert((bool)f.Database.Get("companies/"+f.Form.Id)["isArchived"]&&f.Accounts.Users[f.Uid].Disabled,"archive incomplete");
            await Reject(()=>f.Service.ActAsync(f.Form.Id,f.Uid,"enable","coordinator"));
            await f.Service.ActAsync(f.Form.Id,"","restoreCompany","coordinator");
            Assert(!(bool)f.Database.Get("companies/"+f.Form.Id)["isArchived"]&&f.Accounts.Users[f.Uid].Disabled,"restore granted login access");
            Assert(f.Database.Get("placements/history").Type==JTokenType.Object,"archive removed history");
        });
        await Case("live lease prevents concurrent mutation",async()=>{
            var f=new Fixture();await f.Save();await f.Database.PatchAsync("companyAccountOperations/"+f.Form.Id,new {leaseUntilUtc=Clock.AddMinutes(1).ToString("o")});
            await Reject(()=>f.Service.ActAsync(f.Form.Id,f.Uid,"delete","coordinator"));Assert(f.Accounts.Users.ContainsKey(f.Uid),"lease ignored");
        });
        Console.WriteLine(passed+" company workflow checks passed; no network calls.");
    }
    internal sealed class Db : IStudentDatabase
    {
        public JObject Root=new JObject(); public int Mutations; public bool FailDelete;
        public JToken Get(string path) {JToken n=Root;foreach(var part in path.Split('/')){n=(n as JObject)?[Uri.UnescapeDataString(part)];if(n==null)return JValue.CreateNull();}return n;}
        public void Set(string path,JToken value){var parts=path.Split('/');var n=Root;for(int i=0;i<parts.Length-1;i++){string key=Uri.UnescapeDataString(parts[i]);if(!(n[key] is JObject))n[key]=new JObject();n=(JObject)n[key];}n[Uri.UnescapeDataString(parts.Last())]=value.DeepClone();}
        private string Tag(string path){var n=Get(path);return n.Type==JTokenType.Null?"null_etag":n.ToString(Newtonsoft.Json.Formatting.None);}
        public Task<JToken> ReadAsync(string path){return Task.FromResult(Get(path).DeepClone());}
        public Task WriteAsync(string path,object value){Mutations++;Set(path,JToken.FromObject(value));return Task.CompletedTask;}
        public Task PatchAsync(string path,object value){var n=(JObject)(Get(path) as JObject??new JObject()).DeepClone();foreach(var p in JObject.FromObject(value).Properties()){if(p.Value.Type==JTokenType.Null)n.Remove(p.Name);else n[p.Name]=p.Value.DeepClone();}return WriteAsync(path,n);}
        public Task DeleteAsync(string path){if(FailDelete){FailDelete=false;throw new TimeoutException();}Mutations++;Set(path,JValue.CreateNull());return Task.CompletedTask;}
        public Task<StudentVersion> ReadVersionAsync(string path){return Task.FromResult(new StudentVersion{Value=Get(path) as JObject,ETag=Tag(path)});}
        public Task<bool> CompareWriteAsync(string path,object value,string etag){if(Tag(path)!=etag)return Task.FromResult(false);Mutations++;Set(path,JToken.FromObject(value));return Task.FromResult(true);}
    }
    internal sealed class Auth : ICompanyAuthentication
    {
        public Dictionary<string,StudentLoginAccount> Users=new Dictionary<string,StudentLoginAccount>();
        public Dictionary<string,string> Passwords=new Dictionary<string,string>();public int Created;public bool LoseCreateReply,FailPassword;
        public Task<StudentLoginAccount> FindAsync(string uid){StudentLoginAccount user;Users.TryGetValue(uid,out user);return Task.FromResult(user);}
        public Task<StudentLoginAccount> FindByEmailAsync(string email){return Task.FromResult(Users.Values.FirstOrDefault(u=>string.Equals(u.Email,email,StringComparison.OrdinalIgnoreCase)));}
        public Task CreateAsync(string uid,string email,string name,string password){if(Users.ContainsKey(uid))throw new Exception("duplicate");Users[uid]=new StudentLoginAccount{Uid=uid,Email=email,Disabled=true};Passwords[uid]=password;Created++;if(LoseCreateReply){LoseCreateReply=false;throw new TimeoutException();}return Task.CompletedTask;}
        public Task UpdateAsync(string uid,bool disabled,string password=null){if(password!=null&&FailPassword){FailPassword=false;throw new TimeoutException();}Users[uid].Disabled=disabled;if(password!=null)Passwords[uid]=password;return Task.CompletedTask;}
        public Task UpdateProfileAsync(string uid,string email,string name){Users[uid].Email=email;return Task.CompletedTask;}
        public Task DeleteAsync(string uid){Users.Remove(uid);Passwords.Remove(uid);return Task.CompletedTask;}
        public Task<string> ResetLinkAsync(string email){return Task.FromResult("https://example.com/reset");}
    }
}
