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
        Check((string)profile["studentId"]=="9000001" && (string)profile["accountSetupStatus"]=="complete" && !(bool)profile["mustChangePassword"], "Confirm saves matching complete platform profile");
        Check((await service.ListAsync()).Count==1, "list refresh includes created student");
        int creates=auth.Creates;
        try { await service.CreateAsync("9000001","",password,"coordinator",false); throw new Exception("duplicate accepted"); } catch(StudentManagementException) { Check(auth.Creates==creates,"duplicate account rejected without another Authentication create"); }
        await db.WriteAsync("placements/P1",new {id="P1",studentId="9000001",studentUid=uid,companyId="C1",status="active"});
        await db.WriteAsync("applications/APP1",new {studentId="9000001",placementId="P1"});
        await db.WriteAsync("tasks/T1",new {placementId="P1",studentId="9000001"});
        await db.WriteAsync("taskExams/E1",new {placementId="P1"});
        await db.WriteAsync("taskExamAnswerKeys/E1",new {answer="private"});
        await db.WriteAsync("taskExamAttempts/SHARED/OWN",new {studentId="9000001",score=90});
        await db.WriteAsync("taskExamAttempts/SHARED/OTHER",new {studentId="other",score=80});
        await db.WriteAsync("completionReviews/P1",new {studentId="9000001"});
        await db.WriteAsync("completionDocuments/P1/doc",new {storageObjectPath="private.pdf"});
        await db.WriteAsync("placementClaims/9000001",new {placementId="P1"});
        await db.WriteAsync("assignments/A1",new {studentId="9000001"});
        await db.WriteAsync("groups/G1/students/9000001",true);
        await db.WriteAsync("uimsAttendance/trainingAssignments/TA1",new {studentId="9000001",placementId="P1"});
        await db.WriteAsync("uimsAttendance/attendance/TA1/day",new {workedMinutes=480});
        await db.WriteAsync("uimsAttendance/students/9000001",new {activeAssignmentId="TA1"});
        await db.WriteAsync("placements/OTHER",new {studentId="other",companyId="C1"});
        auth.FailDelete=true;
        try { await service.DeleteAsync(uid,"coordinator"); throw new Exception("delete failure ignored"); } catch(InvalidOperationException) { Check(auth.Accounts.ContainsKey(uid) && (await db.ReadAsync("users/"+uid)).Type!=JTokenType.Null,"failed Authentication delete retains profile for retry"); }
        auth.FailDelete=false;
        await service.DeleteAsync(uid,"coordinator");
        Check(!auth.Accounts.ContainsKey(uid) && (await db.ReadAsync("users/"+uid)).Type==JTokenType.Null,"Delete removes Authentication account and platform profile");
        Check((await service.ListAsync()).Count==0,"list refresh excludes deleted student");
        foreach(string path in new[]{"placements/P1","applications/APP1","tasks/T1","taskExams/E1","taskExamAnswerKeys/E1","taskExamAttempts/SHARED/OWN","completionReviews/P1","completionDocuments/P1","placementClaims/9000001","assignments/A1","groups/G1/students/9000001","uimsAttendance/trainingAssignments/TA1","uimsAttendance/attendance/TA1","uimsAttendance/students/9000001"}) Check((await db.ReadAsync(path)).Type==JTokenType.Null,"delete clears "+path);
        Check((await db.ReadAsync("placements/OTHER")).Type!=JTokenType.Null && (await db.ReadAsync("taskExamAttempts/SHARED/OTHER")).Type!=JTokenType.Null,"deleting one student preserves other students and shared exam attempts");
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
        await service.DeleteAsync(uid,"coordinator");
        await db.WriteAsync("placements/LEGACY",new {studentId="9000001",companyId="old"});
        uid=await service.CreateAsync("9000001","",StudentManagementService.GeneratePassword(),"coordinator",false);
        Check((await db.ReadAsync("placements/LEGACY")).Type==JTokenType.Null,"recreation cleans pre-fix history keyed by stable student ID");
        await db.WriteAsync("tasks/RETRY",new {studentId="9000001"});db.FailCleanupCommits=3;
        try { await service.DeleteAsync(uid,"coordinator");throw new Exception("cleanup conflict ignored"); }catch(StudentManagementException){Check((await db.ReadAsync("users/"+uid)).Type!=JTokenType.Null && (await db.ReadAsync("tasks/RETRY")).Type!=JTokenType.Null,"failed atomic cleanup preserves pending profile and history for retry");}
        await service.DeleteAsync(uid,"coordinator");Check((await db.ReadAsync("tasks/RETRY")).Type==JTokenType.Null,"retry completes cleanup after Auth was already deleted");
        await SupervisorDeletionChecks();
        Console.WriteLine("All isolated account deletion/recreation checks passed; no network or email used.");
    }
    static async Task SupervisorDeletionChecks()
    {
        var db=new MemoryDatabase();var auth=new MemoryAuthentication();
        var academic=new AcademicSupervisorService(db,auth);
        await db.WriteAsync("universityRecords/academicSupervisors/A1",new {staffId="A1",fullName="Test Academic",email="academic@example.invalid",universityId="U",collegeId="IT",departmentId="CS",isActive=true});
        var official=await academic.LookupAsync("A1");
        string academicUid=await academic.CreateAsync(official,StudentManagementService.GeneratePassword(),"coordinator");
        await db.WriteAsync("assignments/OLD",new {studentId="S1",academicSupervisorId=academicUid,active=false});
        await db.WriteAsync("placements/OLD",new {studentId="S1",academicSupervisorId=academicUid,companyId="keep"});
        await db.WriteAsync("finalEvaluationReviewsV2/OLD",new {reviews=new[]{new {academicSupervisorId=academicUid,kind="Approve"},new {academicSupervisorId="other-academic",kind="Approve"}}});
        await academic.ActAsync(academicUid,"delete","coordinator");
        Check((await db.ReadAsync("assignments/OLD")).Type==JTokenType.Null && (await db.ReadAsync("placements/OLD"))["academicSupervisorId"]==null,"academic deletion removes old assignments and detaches existing placements");
        Check(((JArray)(await db.ReadAsync("finalEvaluationReviewsV2/OLD"))["reviews"]).Count==1 && (await db.ReadAsync("placements/OLD")).Type!=JTokenType.Null,"academic cleanup preserves student company work and other academic reviews");
        await academic.CreateAsync(official,StudentManagementService.GeneratePassword(),"coordinator");
        Check((await academic.DirectoryAsync())["supervisors"].Count()==1 && (await db.ReadAsync("assignments/OLD")).Type==JTokenType.Null,"recreated academic supervisor has no old assignment history");

        var companies=new CompanyManagementService(db,auth);string companyId=new string('a',32);
        await db.WriteAsync("companies/"+companyId,new {name="Company",revision="v1",isActive=true});
        foreach(string uid in new[]{"company-one","company-two"}) {await db.WriteAsync("users/"+uid,new {role="companySupervisor",companyId,isActive=true});auth.Accounts[uid]=new StudentLoginAccount{Uid=uid,Email=uid+"@example.invalid"};}
        await db.WriteAsync("companySupervisors/CS1",new {firebaseUid="company-one",companyId});
        await db.WriteAsync("placements/COMP1",new {studentId="S1",companyId,companySupervisorId="CS1"});
        await db.WriteAsync("tasks/COMP1",new {placementId="COMP1",studentId="S1"});
        await db.WriteAsync("placements/COMP2",new {studentId="S2",companyId,companySupervisorId="company-two"});
        await db.WriteAsync("opportunities/SHARED",new {companyId,title="Shared company opportunity"});
        await companies.ActAsync(companyId,"company-one","delete","coordinator");
        Check((await db.ReadAsync("placements/COMP1")).Type==JTokenType.Null && (await db.ReadAsync("tasks/COMP1")).Type==JTokenType.Null && (await db.ReadAsync("companySupervisors/CS1")).Type==JTokenType.Null,"company supervisor deletion clears aliases and own training history");
        Check((await db.ReadAsync("placements/COMP2")).Type!=JTokenType.Null && (await db.ReadAsync("users/company-two")).Type!=JTokenType.Null && (await db.ReadAsync("opportunities/SHARED")).Type!=JTokenType.Null,"deleting one company supervisor preserves another supervisor and shared company records");
        await db.WriteAsync("users/STUDENT",new {role="student",studentId="S2",completionStatus="Completed",companyId});
        await companies.ActAsync(companyId,null,"deleteCompany","coordinator");
        Check((await db.ReadAsync("companies/"+companyId)).Type==JTokenType.Null && (await db.ReadAsync("users/company-two")).Type==JTokenType.Null && !auth.Accounts.ContainsKey("company-two"),"company deletion removes company and linked supervisor logins");
        Check((await db.ReadAsync("placements/COMP2")).Type==JTokenType.Null && (await db.ReadAsync("opportunities/SHARED")).Type==JTokenType.Null && (await db.ReadAsync("users/STUDENT"))["completionStatus"]==null,"company deletion clears linked company history and stale student status");
        var form=new CompanyInput {Id=companyId,RequestId=new string('b',32),Name="Recreated company",SupervisorName="Fresh supervisor",SupervisorEmail="fresh@example.invalid"};
        await companies.SaveAsync(form,"coordinator");
        Check((await db.ReadAsync("companies/"+companyId)).Type!=JTokenType.Null && (await db.ReadAsync("placements/COMP2")).Type==JTokenType.Null && (await db.ReadAsync("opportunities/SHARED")).Type==JTokenType.Null,"recreated company starts without previous placements or opportunities");
        string freshUid="company_"+form.RequestId;
        await db.WriteAsync("placements/LAST",new {studentId="S3",companyId});
        await companies.ActAsync(companyId,freshUid,"delete","coordinator");
        Check((await db.ReadAsync("placements/LAST")).Type==JTokenType.Null && (await db.ReadAsync("companies/"+companyId)).Type!=JTokenType.Null,"deleting the last company supervisor also clears company-level history");
    }
    sealed class NoMail : IStudentCredentialMailer
    {
        public void ValidateConfiguration() { throw new Exception("unexpected mail"); }
        public Task SendWelcomeAsync(string a,string b,string c,string d) { throw new Exception("unexpected mail"); }
        public Task SendResetAsync(string a,string b,string c) { throw new Exception("unexpected mail"); }
    }
    sealed class MemoryAuthentication : ICompanyAuthentication
    {
        public readonly Dictionary<string,StudentLoginAccount> Accounts=new Dictionary<string,StudentLoginAccount>();
        public int Creates; public bool FailDelete;
        public Task<StudentLoginAccount> FindAsync(string uid) { StudentLoginAccount a; Accounts.TryGetValue(uid,out a); return Task.FromResult(a); }
        public Task<StudentLoginAccount> FindByEmailAsync(string email) { return Task.FromResult(Accounts.Values.FirstOrDefault(a=>a.Email==email)); }
        public Task CreateAsync(string uid,string email,string name,string password) { Creates++; Accounts.Add(uid,new StudentLoginAccount { Uid=uid,Email=email,Disabled=true }); return Task.CompletedTask; }
        public Task UpdateAsync(string uid,bool disabled,string password=null) { Accounts[uid].Disabled=disabled; return Task.CompletedTask; }
        public Task DeleteAsync(string uid) { if(FailDelete) throw new InvalidOperationException("mock failure"); Accounts.Remove(uid); return Task.CompletedTask; }
        public Task UpdateProfileAsync(string uid,string email,string name) { Accounts[uid].Email=email; return Task.CompletedTask; }
        public Task<string> ResetLinkAsync(string email) { return Task.FromResult("mock"); }
    }
    sealed class MemoryDatabase : IStudentDatabase
    {
        public bool FailDelete; public int FailCleanupCommits; private int revision;
        readonly JObject root=new JObject(); readonly Dictionary<string,int> versions=new Dictionary<string,int>();
        JToken Get(string path) { if(path=="")return root.DeepClone(); JToken n=root; foreach(var key in path.Split('/')) { n=(n as JObject)?[key]; if(n==null)return JValue.CreateNull(); } return n.DeepClone(); }
        void Set(string path,JToken value) { revision++; if(path==""){root.RemoveAll();foreach(var item in ((JObject)value).Properties().ToArray())root[item.Name]=item.Value.DeepClone();return;} var keys=path.Split('/'); JObject n=root; for(int i=0;i<keys.Length-1;i++) { if(!(n[keys[i]] is JObject))n[keys[i]]=new JObject(); n=(JObject)n[keys[i]]; } if(value==null||value.Type==JTokenType.Null)n.Remove(keys.Last()); else n[keys.Last()]=value.DeepClone(); versions[path]=versions.ContainsKey(path)?versions[path]+1:1; }
        string Tag(string path) { return Get(path).Type==JTokenType.Null ? "null_etag" : revision.ToString(); }
        public Task<JToken> ReadAsync(string path) { return Task.FromResult(Get(path)); }
        public Task WriteAsync(string path,object value) { Set(path,JToken.FromObject(value)); return Task.CompletedTask; }
        public Task PatchAsync(string path,object value) { var n=Get(path) as JObject ?? new JObject(); foreach(var p in JObject.FromObject(value).Properties())n[p.Name]=p.Value; Set(path,n); return Task.CompletedTask; }
        public Task DeleteAsync(string path) { if(FailDelete) throw new InvalidOperationException("mock database delete failure"); Set(path,null); return Task.CompletedTask; }
        public Task<StudentVersion> ReadVersionAsync(string path) { return Task.FromResult(new StudentVersion { Value=Get(path) as JObject,ETag=Tag(path) }); }
        public Task<bool> CompareWriteAsync(string path,object value,string etag) { if(path==""&&FailCleanupCommits>0){FailCleanupCommits--;return Task.FromResult(false);} if(Tag(path)!=etag)return Task.FromResult(false); Set(path,JToken.FromObject(value)); return Task.FromResult(true); }
    }
}
