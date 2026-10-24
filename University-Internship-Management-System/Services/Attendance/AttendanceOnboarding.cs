using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;

namespace UIMS.Attendance
{
    // Called only after verified student ownership/academic checks. This never creates
    // accounts, calendars, companies or supervisors, and cannot reconcile existing conflicts.
    public sealed class AttendanceOnboarding
    {
        private readonly IApplicationDatabase database;
        public AttendanceOnboarding(IApplicationDatabase database) { this.database=database; }
        private static string T(JToken n,string k) { return n is JObject && n[k]?.Type==JTokenType.String ? (string)n[k] : ""; }
        private static JObject O(JToken n) { return n as JObject ?? new JObject(); }
        public static JObject Build(JObject existing,string uid,string studentId,string placementId,JObject placement,JObject profile)
        {
            if(studentId=="2023301")throw new FirebaseException(409,"Student 2023301 is frozen pending the coordinator decision. No attendance setup was changed.");
            foreach(string key in new[]{uid,studentId,placementId,T(placement,"companyId"),T(placement,"companySupervisorId"),T(placement,"academicSupervisorId")})AttendanceFirebase.CheckKey(key);
            if(T(profile,"studentId")!=studentId || (bool?)profile["isActive"]!=true || FirebaseSignInService.CanonicalRole(T(profile,"role"))!="student" || T(placement,"studentId")!=studentId || T(placement,"studentUid")!=uid)
                throw new FirebaseException(409,"The verified profile and placement identity must agree before attendance setup.");
            string companyId=T(placement,"companyId"),supervisorId=T(placement,"companySupervisorId");
            var companies=O(existing["companies"]);var supervisors=O(existing["companySupervisors"]);
            if(T(companies[companyId],"status")!="active" || T(supervisors[supervisorId],"status")!="active" || T(supervisors[supervisorId],"companyId")!=companyId)
                throw new FirebaseException(409,"The accepted offer needs an existing active attendance company and its matching supervisor; no identities were invented.");
            DateTime first,last;string start=T(placement,"startDate"),end=T(placement,"endDate");
            if(!DateTime.TryParseExact(start,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out first) || !DateTime.TryParseExact(end,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out last) || first>last || AttendanceService.Number(placement,"requiredHours")<=0)
                throw new FirebaseException(409,"Approved placement dates and required hours are prerequisites for attendance. No dates or hours were inferred.");
            var actor=O(O(existing["students"])[studentId]);string assignmentId=T(actor,"activeAssignmentId");
            var assignments=O(existing["trainingAssignments"]);
            var matching=assignments.Properties().Where(p=>T(p.Value,"studentId")==studentId && T(p.Value,"placementId")==placementId).ToArray();
            if(matching.Length>1)throw new FirebaseException(409,"Multiple attendance assignments match this placement; coordinator review is required.");
            if(assignmentId.Length==0)assignmentId=matching.Length==1?matching[0].Name:placementId;
            AttendanceFirebase.CheckKey(assignmentId);
            if(actor.HasValues && (T(actor,"status")!="active" || T(actor,"firebaseUid")!=uid || T(actor,"activeAssignmentId")!=assignmentId))
                throw new FirebaseException(409,"An existing attendance actor is inactive or linked to another UID/assignment. No actor was replaced.");
            var assignment=O(assignments[assignmentId]);
            if(assignment.HasValues && (T(assignment,"studentId")!=studentId || T(assignment,"placementId")!=placementId || T(assignment,"companyId")!=companyId || T(assignment,"companySupervisorId")!=supervisorId || T(assignment,"academicSupervisorId")!=T(placement,"academicSupervisorId") || T(assignment,"status")!="active" || T(assignment,"startDate")!=start || T(assignment,"endDate")!=end || AttendanceService.Number(assignment,"requiredHours")!=AttendanceService.Number(placement,"requiredHours")))
                throw new FirebaseException(409,"The existing attendance assignment conflicts with the approved placement. Resolve it explicitly; no history or calendar was changed.");
            var schedule=O(O(existing["schedules"])[assignmentId]);
            if(!schedule.HasValues)throw new FirebaseException(409,"An approved persisted attendance calendar is required at uimsAttendance/schedules/"+assignmentId+" before offer confirmation. No claim, placement or calendar was created.");
            foreach(var entry in schedule.Properties())
            {
                var day=entry.Value as JObject;DateTime parsed;
                if(day==null || !DateTime.TryParseExact(entry.Name,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed) || parsed<first || parsed>last || T(day,"assignmentId")!=assignmentId || T(day,"attendanceDate")!=entry.Name || T(day,"timeZone")!="Asia/Amman" || AttendanceService.Number(day,"version")<=0 || day["isTrainingDay"]?.Type!=JTokenType.Boolean)
                    throw new FirebaseException(409,"The persisted calendar contains a date, assignment or timezone inconsistency. Request the approved calendar correction; no replacement schedule was generated.");
                if((bool)day["isTrainingDay"])
                {
                    long from=AttendanceService.Number(day,"scheduledStartAt"),to=AttendanceService.Number(day,"scheduledEndAt");
                    try {var zone=TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");if(from<=0 || to<=from || TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(from),zone).ToString("yyyy-MM-dd")!=entry.Name || TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(to),zone).ToString("yyyy-MM-dd")!=entry.Name)throw new ArgumentOutOfRangeException();}
                    catch(ArgumentOutOfRangeException){throw new FirebaseException(409,"The persisted calendar's shift timestamps are invalid; no schedule was changed.");}
                }
            }
            var copy=(JObject)existing.DeepClone();
            foreach(string branch in new[]{"students","trainingAssignments","provisioningClaims"})if(!(copy[branch] is JObject))copy[branch]=new JObject();
            if(!actor.HasValues)copy["students"][studentId]=new JObject{["studentId"]=studentId,["studentNumber"]=studentId,["firebaseUid"]=uid,["activeAssignmentId"]=assignmentId,["status"]="active",["name"]=T(profile,"fullName"),["fullName"]=T(profile,"fullName"),["email"]=T(profile,"email"),["isTestData"]=false};
            if(!assignment.HasValues)copy["trainingAssignments"][assignmentId]=new JObject{["assignmentId"]=assignmentId,["studentId"]=studentId,["placementId"]=placementId,["companyId"]=companyId,["companySupervisorId"]=supervisorId,["academicSupervisorId"]=T(placement,"academicSupervisorId"),["status"]="active",["startDate"]=start,["endDate"]=end,["requiredHours"]=placement["requiredHours"].DeepClone(),["isTestData"]=false};
            var claim=O(O(copy["provisioningClaims"])[placementId]);
            if(claim.HasValues && (T(claim,"assignmentId")!=assignmentId || T(claim,"studentId")!=studentId || T(claim,"firebaseUid")!=uid || T(claim,"placementId")!=placementId))throw new FirebaseException(409,"Attendance provisioning claim conflicts with this identity. No setup was replaced.");
            if(!claim.HasValues)copy["provisioningClaims"][placementId]=new JObject{["assignmentId"]=assignmentId,["studentId"]=studentId,["firebaseUid"]=uid,["placementId"]=placementId};
            return copy;
        }
        public async Task<bool> ValidateAsync(string uid,string studentId,string placementId,JObject placement,JObject profile)
        { var state=await database.ReadVersionAsync("uimsAttendance");var original=state.Value ?? new JObject();return !JToken.DeepEquals(original,Build(original,uid,studentId,placementId,placement,profile)); }
        public async Task EnsureAsync(string uid,string studentId,string placementId,JObject placement,JObject profile)
        {
            for(int attempt=0;attempt<3;attempt++)
            {
                var state=await database.ReadVersionAsync("uimsAttendance");var original=state.Value ?? new JObject();
                var next=Build(original,uid,studentId,placementId,placement,profile);
                if(JToken.DeepEquals(original,next))return;
                if(string.IsNullOrWhiteSpace(state.ETag))throw new FirebaseException(409,"Attendance provisioning requires a current ETag. Retry after checking server read permissions.");
                // One exact-ETag PUT keeps actor, assignment and durable claim together;
                // schedules, attendance and all other fields are preserved from that read.
                if(await database.CompareWriteAsync("uimsAttendance",next,state.ETag))return;
            }
            throw new FirebaseException(409,"Attendance setup changed concurrently. Retry the same confirmation to resume; no duplicate assignment is required.");
        }
    }
}
