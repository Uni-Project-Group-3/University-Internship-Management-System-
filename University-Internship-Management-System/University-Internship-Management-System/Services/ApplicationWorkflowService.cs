using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    // Commands receive identity only from the verified Gateway account, never from input JSON.
    public sealed class ApplicationWorkflowService
    {
        private readonly FirebaseRealtimeDatabaseRepository database;
        public ApplicationWorkflowService(FirebaseRealtimeDatabaseRepository database) { this.database = database; }
        private static string Text(JObject input,string key,int max,bool required=true)
        {
            if(input[key]?.Type!=JTokenType.String) throw new ArgumentException("Invalid "+key+".");
            string value=((string)input[key]).Trim();
            if(value.Length>max || required&&value.Length==0)throw new ArgumentException("Invalid "+key+".");
            return value;
        }
        private static void Fields(JObject input,params string[] allowed)
        {
            if(input==null||input.Properties().Any(p=>!allowed.Contains(p.Name)))throw new ArgumentException("Unexpected command fields.");
        }
        private static void Require(UimsAccount account,string role)
        {
            if(account==null||account.Role!=role||account.MustChangePassword||string.IsNullOrEmpty(account.Uid)||string.IsNullOrEmpty(account.ProfileId))throw new UnauthorizedAccessException();
        }
        public static string ApplicationId(string studentId,string opportunityId)
        {
            FirebaseRealtimeDatabaseRepository.ValidateSegment(studentId);FirebaseRealtimeDatabaseRepository.ValidateSegment(opportunityId);
            using(var sha=System.Security.Cryptography.SHA256.Create())
                return "APP-"+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(studentId+"\n"+opportunityId))).Replace("-","").ToLowerInvariant();
        }
        public async Task<JObject> ApplyAsync(UimsAccount account,JObject input)
        {
            Require(account,"student");Fields(input,"opportunityId","phone","cvLink","portfolioLink","motivation","confirmed");
            if(input["confirmed"]?.Type!=JTokenType.Boolean||!(bool)input["confirmed"])throw new ArgumentException("Confirm that your information is accurate.");
            string oid=Text(input,"opportunityId",128),phone=Text(input,"phone",25),cv=Text(input,"cvLink",500),portfolio=Text(input,"portfolioLink",500,false),statement=Text(input,"motivation",1000,false);
            Https(cv);if(portfolio.Length>0)Https(portfolio);
            if(!System.Text.RegularExpressions.Regex.IsMatch(phone,@"^[+0-9() .-]{7,25}$"))throw new ArgumentException("Enter a valid phone number.");
            var student=await database.ReadStudentProfileAsync(account);
            if(!string.IsNullOrEmpty((string)student["placementId"]))throw new ArgumentException("Your account already has a placement. It cannot confirm or apply for a second internship.");
            var owned=await database.ReadStudentRecordsAsync("applications",account);
            if(owned.Properties().Any(p=>(string)p.Value["opportunityId"]==oid))throw new ArgumentException("You already applied to this opportunity.");
            var opportunity=await database.ReadAsync("opportunities",oid) as JObject;
            RequireEligible(student,opportunity,oid);
            string id=ApplicationId(account.ProfileId,oid),stamp=DateTime.UtcNow.ToString("o");
            var record=new JObject {["id"]=id,["studentId"]=account.ProfileId,["opportunityId"]=oid,["companyId"]=(string)opportunity["companyId"],["status"]="submitted",
                ["applicantProgramId"]=(string)student["programId"],["studentStatement"]=statement,["phone"]=phone,["attachments"]=new JObject{["cvLink"]=cv,["portfolioLink"]=portfolio},
                ["submittedAt"]=stamp,["appliedAt"]=stamp,["lastUpdatedAt"]=stamp};
            var patch=new JObject{["applications/"+id]=record,["applicationIndex/"+account.ProfileId+"/"+oid]=id};
            await database.CommitApplicationAsync(account,patch);
            return new JObject{["success"]=true,["id"]=id};
        }
        public static void RequireEligible(JObject student,JObject opportunity,string oid)
        {
            if(opportunity==null||(string)opportunity["id"]!=oid||(string)opportunity["status"]!="published"||opportunity["canApply"]?.Type!=JTokenType.Boolean||!(bool)opportunity["canApply"])
                throw new ArgumentException("This opportunity is not accepting applications.");
            if(!(opportunity["eligibleProgramIds"] is JArray programs)||!programs.Any(p=>(string)p==(string)student["programId"]))throw new ArgumentException("This opportunity is not available for your academic program.");
            string deadline=(string)opportunity["applicationDeadline"];
            if(deadline==null||JordanTime.ParseInput(deadline,"23:59")<=DateTime.UtcNow)throw new ArgumentException("The application deadline has passed.");
        }
        private static void Https(string value){Uri uri;if(!Uri.TryCreate(value,UriKind.Absolute,out uri)||uri.Scheme!="https"||!string.IsNullOrEmpty(uri.UserInfo))throw new ArgumentException("Use a complete HTTPS link without credentials.");}
        public async Task<JObject> ReviewAsync(UimsAccount account,JObject input)
        {
            Require(account,"company");Fields(input,"id","status","feedback","expectedUpdatedAt");
            string id=Text(input,"id",128),status=Text(input,"status",20),feedback=Text(input,"feedback",400,status=="rejected");
            if(status!="accepted"&&status!="rejected")throw new ArgumentException("Choose Accept or Reject.");
            var version=await database.ReadVersionAsync("applications",id);
            var record=version.Value as JObject;
            if(record==null||(string)record["id"]!=id||(string)record["companyId"]!=account.CompanyId)throw new UnauthorizedAccessException();
            if(!new[]{"submitted","under_review"}.Contains((string)record["status"]))throw new ArgumentException("Only pending applications can be reviewed.");
            if((string)record["lastUpdatedAt"]!=(string)input["expectedUpdatedAt"])throw new AccountConflictException();
            var opportunity=await database.ReadAsync("opportunities",(string)record["opportunityId"]) as JObject;
            if(opportunity==null||(string)opportunity["companyId"]!=account.CompanyId)throw new UnauthorizedAccessException();
            record["status"]=status;record["reviewNotes"]=feedback;record["reviewedBy"]=account.ProfileId;record["lastUpdatedAt"]=DateTime.UtcNow.ToString("o");
            await database.WriteApplicationReviewAsync(account,id,record,version.ETag);
            return new JObject{["success"]=true,["id"]=id};
        }
        public async Task<JObject> ConfirmAsync(UimsAccount account,JObject input)
        {
            Require(account,"student");Fields(input,"id");string id=Text(input,"id",128);
            var student=await database.ReadStudentProfileAsync(account);
            string pid="PL-"+account.ProfileId;
            if(!string.IsNullOrEmpty((string)student["placementId"]))
            {
                var prior=await database.ReadStudentPlacementAsync(account,(string)student["placementId"]);
                if((string)prior["applicationId"]==id)return new JObject{["success"]=true,["id"]=(string)prior["id"]};
                throw new ArgumentException("You already have a confirmed internship.");
            }
            var application=await database.ReadAsync("applications",id) as JObject;
            if(application==null||(string)application["studentId"]!=account.ProfileId||(string)application["status"]!="accepted")throw new UnauthorizedAccessException();
            string oid=(string)application["opportunityId"];var opportunity=await database.ReadAsync("opportunities",oid) as JObject;
            if(opportunity==null||(string)opportunity["companyId"]!=(string)application["companyId"])throw new UnauthorizedAccessException();
            if(opportunity["requiredHours"]?.Type!=JTokenType.Integer||(int)opportunity["requiredHours"]<=0 ||
                opportunity["durationWeeks"]?.Type!=JTokenType.Integer||opportunity["capacity"]?.Type!=JTokenType.Integer)
                throw new ArgumentException("The company must configure training dates, duration and required hours before confirmation.");
            DateTime start;if(!DateTime.TryParseExact((string)opportunity["startDate"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out start))throw new ArgumentException("Training dates are not configured.");
            string academic=(string)student["academicSupervisorId"],supervisor=(string)application["reviewedBy"];
            if(string.IsNullOrWhiteSpace(academic)||string.IsNullOrWhiteSpace(supervisor))throw new ArgumentException("Verified academic and company supervisor assignments are required.");
            string stamp=DateTime.UtcNow.ToString("o");
            for(int seat=0;seat<Math.Min(50,(int)opportunity["capacity"]);seat++)
            {
                var placement=new JObject {["id"]=pid,["studentId"]=account.ProfileId,["applicationId"]=id,["opportunityId"]=oid,["companyId"]=(string)application["companyId"],
                    ["academicSupervisorId"]=academic,["companySupervisorId"]=supervisor,["startDate"]=start.ToString("yyyy-MM-dd"),["endDate"]=start.AddDays(7*(int)opportunity["durationWeeks"]).ToString("yyyy-MM-dd"),
                    ["status"]="active",["createdAt"]=stamp,["requiredHours"]=(int)opportunity["requiredHours"],["approvedHours"]=0,["seatIndex"]=seat};
                var patch=new JObject {["placements/"+pid]=placement,["students/"+account.ProfileId+"/placementId"]=pid,
                    ["applications/"+id+"/status"]="confirmed",["applications/"+id+"/placementId"]=pid,["applications/"+id+"/confirmedAt"]=stamp,
                    ["opportunitySeats/"+oid+"/"+seat]=new JObject{["studentId"]=account.ProfileId,["placementId"]=pid,["applicationId"]=id}};
                try { await database.CommitApplicationAsync(account,patch);return new JObject{["success"]=true,["id"]=pid}; }
                catch(StudentDataReadException error) when(error.HttpStatus==401||error.HttpStatus==403) { }
            }
            throw new ArgumentException("No seat could be confirmed, or the scoped placement permissions are unavailable. Your existing records were not changed.");
        }
    }
}
