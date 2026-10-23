using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    public sealed class UniversityAcademicRecord
    {
        public string StaffId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UniversityId { get; set; }
        public string CollegeId { get; set; }
        public string DepartmentId { get; set; }
    }
    public sealed class AcademicSupervisorService
    {
        private readonly IStudentDatabase db;
        private readonly ICompanyAuthentication auth;
        private readonly Func<DateTimeOffset> now;
        public AcademicSupervisorService(IStudentDatabase db, ICompanyAuthentication auth, Func<DateTimeOffset> now = null)
        { this.db=db; this.auth=auth; this.now=now ?? (()=>DateTimeOffset.UtcNow); }
        private static string T(JToken value,string field) { return StudentManagementService.Text(value,field); }
        private static string Key(string value) { return StudentManagementService.Key(value); }
        private static bool Academic(JToken value) { return new[]{"academicSupervisor","academic_supervisor","academic"}.Contains(T(value,"role")); }
        public static string ManagedUid(string university,string staff)
        {
            using(var hash=SHA256.Create()) return "academic_"+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(university+":"+staff))).Replace("-", "").ToLowerInvariant().Substring(0,40);
        }
        public async Task<UniversityAcademicRecord> LookupAsync(string staffId)
        {
            if(!Regex.IsMatch(staffId ?? "", "\\A[A-Za-z0-9_-]{1,40}\\z")) throw new StudentManagementException("Enter a valid staff ID, for example A1001.");
            var record=await db.ReadAsync("universityRecords/academicSupervisors/"+Key(staffId)) as JObject;
            if(record==null) throw new StudentManagementException("Academic supervisor not found in university records.");
            if(T(record,"staffId")!=staffId || new[]{"fullName","email","universityId","collegeId","departmentId"}.Any(field=>string.IsNullOrWhiteSpace(T(record,field))))
                throw new StudentManagementException("The university record is incomplete or its staff ID does not match. Ask the university to correct it.");
            foreach(string flag in new[]{"isEligible","isActive"})
                if(record[flag]!=null && (record[flag].Type!=JTokenType.Boolean || record.Value<bool?>(flag)!=true))
                    throw new StudentManagementException("This university record is inactive or ineligible for account creation.");
            string email=T(record,"email").Trim();
            try { if(email.Length>254 || new MailAddress(email).Address!=email) throw new FormatException(); }
            catch { throw new StudentManagementException("The official academic supervisor email is invalid."); }
            return new UniversityAcademicRecord {StaffId=staffId,FullName=T(record,"fullName").Trim(),Email=email,
                UniversityId=T(record,"universityId"),CollegeId=T(record,"collegeId"),DepartmentId=T(record,"departmentId")};
        }
        private async Task<JObject> RequireAcademicAsync(string uid)
        {
            var value=await db.ReadAsync("users/"+Key(uid)) as JObject;
            if(!Academic(value)) throw new StudentManagementException("Academic supervisor account not found. Refresh the directory.");
            return value;
        }
        private async Task<JObject> AcquireAsync(string uid,string action,string actor)
        {
            var version=await db.ReadVersionAsync("academicAccountOperations/"+Key(uid));
            var previous=version.Value ?? new JObject();
            DateTimeOffset until;
            if(DateTimeOffset.TryParse(T(previous,"leaseUntilUtc"),out until) && until>now()) throw new StudentManagementException("An account operation is already running. Wait briefly and retry.");
            if(T(previous,"kind")=="create" && T(previous,"state")=="pending" && action!="create" && action!="delete")
                throw new StudentManagementException("Finish this supervisor's pending setup before changing the account.");
            var operation=(JObject)previous.DeepClone();operation["kind"]=action;operation["state"]="pending";
            operation["actor"]=actor;operation["leaseUntilUtc"]=now().AddMinutes(10).ToString("o");
            if(!await db.CompareWriteAsync("academicAccountOperations/"+Key(uid),operation,version.ETag)) throw new StudentManagementException("The account changed during this request. Refresh and retry.");
            return operation;
        }
        private async Task CompleteAsync(string uid,JObject op)
        {op["state"]="complete";op["leaseUntilUtc"]=null;op["completedAtUtc"]=now().ToString("o");await db.WriteAsync("academicAccountOperations/"+Key(uid),op);}
        private async Task ReleaseAsync(string uid)
        {try{await db.PatchAsync("academicAccountOperations/"+Key(uid),new{leaseUntilUtc=(string)null});}catch{}}
        private JObject Profile(UniversityAcademicRecord record)
        {
            return new JObject{["staffId"]=record.StaffId,["fullName"]=record.FullName,["email"]=record.Email,["universityId"]=record.UniversityId,
                ["collegeId"]=record.CollegeId,["departmentId"]=record.DepartmentId,["majorId"]=record.DepartmentId,["role"]="academicSupervisor"};
        }
        public async Task<string> CreateAsync(UniversityAcademicRecord expected,string password,string actor)
        {
            if(expected==null) throw new StudentManagementException("Find the supervisor in university records first.");
            var record=await LookupAsync(expected.StaffId);
            if(!JToken.DeepEquals(JObject.FromObject(expected),JObject.FromObject(record))) throw new StudentManagementException("The official record changed. Find the supervisor again before confirming.");
            if(string.IsNullOrEmpty(password)||password.Length<12) throw new StudentManagementException("Generate a temporary password first.");
            string uid=ManagedUid(record.UniversityId,record.StaffId);
            var users=await db.ReadAsync("users") as JObject ?? new JObject();
            if(users.Properties().Any(p=>p.Name!=uid && Academic(p.Value) && T(p.Value,"staffId")==record.StaffId && T(p.Value,"universityId")==record.UniversityId))
                throw new StudentManagementException("This academic supervisor already has an account.");
            var previous=await db.ReadAsync("academicAccountOperations/"+Key(uid)) as JObject;
            if(T(previous,"kind")=="create"&&T(previous,"state")=="complete") throw new StudentManagementException("This academic supervisor already has an account.");
            bool resume=T(previous,"kind")=="create"&&T(previous,"state")=="pending";
            var existing=users[uid] as JObject;
            if(existing!=null && (!resume || !Academic(existing) || T(existing,"staffId")!=record.StaffId || T(existing,"universityId")!=record.UniversityId))
                throw new StudentManagementException("An existing profile cannot be replaced by this setup.");
            var account=await auth.FindAsync(uid);
            if(account!=null && (!resume || !string.Equals(account.Email,record.Email,StringComparison.OrdinalIgnoreCase)))
                throw new StudentManagementException("An existing Firebase login cannot be adopted by this setup.");
            var emailAccount=await auth.FindByEmailAsync(record.Email);
            if(emailAccount!=null&&emailAccount.Uid!=uid) throw new StudentManagementException("The official email already has a login. Review that account instead of creating a duplicate.");
            if(resume && (T(previous,"staffId")!=record.StaffId || T(previous,"universityId")!=record.UniversityId || !string.Equals(T(previous,"email"),record.Email,StringComparison.OrdinalIgnoreCase)))
                throw new StudentManagementException("The official record changed during pending setup. Manual review is required.");
            var op=await AcquireAsync(uid,"create",actor);
            try
            {
                op["staffId"]=record.StaffId;op["universityId"]=record.UniversityId;op["email"]=record.Email;
                await db.WriteAsync("academicAccountOperations/"+Key(uid),op);
                // Recheck the official source after acquiring the lease, before any Auth change.
                var fresh=await LookupAsync(record.StaffId);
                if(!JToken.DeepEquals(JObject.FromObject(record),JObject.FromObject(fresh))) throw new StudentManagementException("The official record changed. Find the supervisor again.");
                if(!resume || op.Value<bool?>("historyCleaned") != true)
                {
                    op["historyCleaned"]=false;await db.WriteAsync("academicAccountOperations/"+Key(uid),op);
                    await AccountHistoryCleanup.AcademicAsync(db,uid);
                    op["historyCleaned"]=true;await db.WriteAsync("academicAccountOperations/"+Key(uid),op);
                }
                if(account==null) await auth.CreateAsync(uid,record.Email,record.FullName,password);
                else await auth.UpdateAsync(uid,true,password);
                var profile=Profile(record);profile["isActive"]=false;profile["mustChangePassword"]=true;profile["accountSetupStatus"]="pending";
                if(existing==null)
                {
                    profile["createdBy"]=actor;profile["createdAtUtc"]=now().ToString("o");
                    if(!await db.CompareWriteAsync("users/"+Key(uid),profile,"null_etag")) throw new StudentManagementException("A profile appeared during setup. Retry this same lookup.");
                }
                else await db.PatchAsync("users/"+Key(uid),profile);
                await auth.UpdateProfileAsync(uid,record.Email,record.FullName);
                await auth.UpdateAsync(uid,false);
                await db.PatchAsync("users/"+Key(uid),new{isActive=true,accountSetupStatus="complete"});
                await CompleteAsync(uid,op);return uid;
            }
            finally{await ReleaseAsync(uid);}
        }
        private static bool ActiveGroup(JToken group,string uid)
        {
            return (T(group,"supervisorId")==uid||T(group,"academicSupervisorId")==uid||T(group,"supervisorUid")==uid) && group.Value<bool?>("isActive")!=false;
        }
        private async Task<bool> HasAssignmentsAsync(string uid)
        {
            var users=await db.ReadAsync("users") as JObject??new JObject();
            var groups=await db.ReadAsync("groups") as JObject??new JObject();
            return users.Properties().Any(p=>T(p.Value,"role")=="student"&&T(p.Value,"academicSupervisorId")==uid&&p.Value.Value<bool?>("isActive")!=false)
                ||groups.Properties().Any(p=>ActiveGroup(p.Value,uid));
        }
        public async Task<JObject> DirectoryAsync()
        {
            var users=await db.ReadAsync("users") as JObject??new JObject();
            var groups=await db.ReadAsync("groups") as JObject??new JObject();
            var supervisors=new JArray();
            foreach(var p in users.Properties().Where(p=>Academic(p.Value)))
            {
                string dept=T(p.Value,"departmentId");if(dept=="")dept=T(p.Value,"majorId");
                supervisors.Add(new JObject{["uid"]=p.Name,["staffId"]=T(p.Value,"staffId"),["fullName"]=T(p.Value,"fullName"),["email"]=T(p.Value,"email"),
                    ["universityId"]=T(p.Value,"universityId"),["collegeId"]=T(p.Value,"collegeId"),["departmentId"]=dept,["department"]=StudentManagementService.DisplayName(dept),
                    ["isActive"]=p.Value.Value<bool?>("isActive")==true,["mustChangePassword"]=p.Value.Value<bool?>("mustChangePassword")==true,
                    ["accountSetupStatus"]=T(p.Value,"accountSetupStatus"),["deletionStatus"]=T(p.Value,"deletionStatus"),
                    ["groupCount"]=groups.Properties().Count(g=>ActiveGroup(g.Value,p.Name)),
                    ["studentCount"]=users.Properties().Count(s=>T(s.Value,"role")=="student"&&T(s.Value,"academicSupervisorId")==p.Name&&s.Value.Value<bool?>("isActive")!=false)});
            }
            return new JObject{["supervisors"]=supervisors};
        }
        public async Task<string> ActAsync(string uid,string action,string actor)
        {
            if(!new[]{"enable","disable","delete","sync"}.Contains(action)) throw new StudentManagementException("Unsupported supervisor action.");
            if(uid==actor) throw new StudentManagementException("You cannot change your own account here.");
            var profile=await RequireAcademicAsync(uid);
            if(action!="delete"&&(T(profile,"accountSetupStatus")=="pending"||T(profile,"deletionStatus")=="pending")) throw new StudentManagementException("Finish pending account setup or deletion first.");
            var op=await AcquireAsync(uid,action,actor);
            try
            {
                profile=await RequireAcademicAsync(uid);
                if(action=="delete")
                {
                    if(await HasAssignmentsAsync(uid)) throw new StudentManagementException("Reassign this supervisor's active students and groups before deleting the account.");
                    await db.PatchAsync("users/"+Key(uid),new{isActive=false,deletionStatus="pending"});
                    await auth.DeleteAsync(uid);
                    await AccountHistoryCleanup.AcademicAsync(db,uid);
                    await db.DeleteAsync("users/"+Key(uid));
                }
                else
                {
                    var account=await auth.FindAsync(uid);if(account==null) throw new StudentManagementException("The Firebase login is missing. Review this account before continuing.");
                    if(action=="disable") {await db.PatchAsync("users/"+Key(uid),new{isActive=false});await auth.UpdateAsync(uid,true);}
                    else if(action=="enable")
                    {
                        var official=await LookupAsync(T(profile,"staffId"));
                        string department=T(profile,"departmentId");if(department=="")department=T(profile,"majorId");
                        if(official.UniversityId!=T(profile,"universityId")||official.CollegeId!=T(profile,"collegeId")||official.DepartmentId!=department||!string.Equals(official.Email,account.Email,StringComparison.OrdinalIgnoreCase)) throw new StudentManagementException("The official record changed. Sync the university record before enabling access.");
                        await auth.UpdateAsync(uid,false);await db.PatchAsync("users/"+Key(uid),new{isActive=true});
                    }
                    else
                    {
                        var official=await LookupAsync(T(profile,"staffId"));
                        if(official.UniversityId!=T(profile,"universityId")) throw new StudentManagementException("The university changed for this staff ID. Manual account review is required.");
                        if((official.DepartmentId!=T(profile,"departmentId")||official.CollegeId!=T(profile,"collegeId"))&&await HasAssignmentsAsync(uid))
                            throw new StudentManagementException("Reassign active students and groups before changing this supervisor's department or college.");
                        await auth.UpdateProfileAsync(uid,official.Email,official.FullName);
                        await db.PatchAsync("users/"+Key(uid),Profile(official));
                    }
                }
                await CompleteAsync(uid,op);
                return action=="delete"?"Supervisor account, old assignments and linked supervisor history deleted. Official university records were retained.":action=="sync"?"Supervisor details refreshed from university records.":"Supervisor account status updated.";
            }
            finally{await ReleaseAsync(uid);}
        }
    }
}
