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
        public async Task SetupForSupervisorAsync(string supervisorId, string studentId, AttendanceFirebase store)
        {
            AttendanceFirebase.CheckKey(studentId);
            var actor = AttendanceConfiguration.WriteRequest.Value;
            if (actor == null || actor.Role != "company" || actor.Id != supervisorId || actor.Action != "setupAttendance")
                throw new FirebaseException(403, "A verified assigned supervisor is required for attendance setup.");
            var placements = await database.ReadEqualAsync("placements", "studentId", studentId) as JObject ?? new JObject();
            var matches = placements.Properties().Where(p => T(p.Value, "companySupervisorId") == supervisorId &&
                new[] { "active", "confirmed" }.Contains(T(p.Value, "status"))).ToArray();
            if (matches.Length != 1) throw new FirebaseException(409, "Exactly one active placement assigned to you is required.");
            string placementId = matches[0].Name;
            var placement = (JObject)matches[0].Value.DeepClone();
            string uid = T(placement, "studentUid");
            AttendanceFirebase.CheckKey(uid);
            var profile = await database.ReadAsync("users/" + uid) as JObject;
            if (profile == null || !await database.IsActiveAuthenticationUidAsync(uid))
                throw new FirebaseException(409, "The student's verified account must be active.");
            if (T(placement, "trainingStartBasis") != "companyAcceptance" || T(placement, "endDate").Length == 0)
            {
                var application = await database.ReadAsync("applications/" + T(placement, "applicationId")) as JObject;
                if (application == null || T(application, "studentId") != studentId || T(application, "companySupervisorId") != supervisorId)
                    throw new FirebaseException(409, "The accepted application could not be verified.");
                foreach (var field in TrainingPeriod(application, placement).Properties()) placement[field.Name] = field.Value.DeepClone();
            }
            ApplyTrainingDefaults(placement);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var original = new JObject();
                foreach (string branch in new[] { "students", "trainingAssignments", "provisioningClaims", "companies", "companySupervisors", "schedules" })
                    original[branch] = (await store.Read(branch)).Data;
                if (T(original["companySupervisors"]?[supervisorId], "firebaseUid") != actor.Uid)
                    throw new FirebaseException(403, "The supervisor account does not match attendance setup.");
                var input = (JObject)original.DeepClone();
                foreach (var assignment in ((JObject)input["trainingAssignments"]).Properties().Where(p =>
                    T(p.Value, "studentId") == studentId && T(p.Value, "placementId") == placementId))
                {
                    var row = (JObject)assignment.Value;
                    if (T(row, "startDate") != T(placement, "startDate") || T(row, "endDate") != T(placement, "endDate"))
                    {
                        var history = (await store.Read("attendance/" + assignment.Name)).Data;
                        if (history.Properties().Any(p => AttendanceService.Number(p.Value, "checkInAt") > 0 || AttendanceService.Number(p.Value, "checkOutAt") > 0))
                            throw new FirebaseException(409, "This training already has attendance. The coordinator must review its period before changing dates.");
                    }
                    row["periodVerifiedFromAcceptance"] = Timestamp(placement, "acceptedAt").Length > 0;
                    foreach (string field in new[] { "acceptedAt", "durationWeeks", "startDate", "endDate", "attendanceMode", "trainingStartBasis", "timeZone" })
                        if (placement[field] != null) row[field] = placement[field].DeepClone();
                }
                var next = Build(input, uid, studentId, placementId, placement, profile);
                var pending = new System.Collections.Generic.Dictionary<string, JObject>();
                foreach (string branch in new[] { "trainingAssignments", "students", "provisioningClaims" })
                    foreach (var entry in ((JObject)next[branch]).Properties())
                        if (!JToken.DeepEquals(original[branch]?[entry.Name], entry.Value)) pending[branch + "/" + entry.Name] = (JObject)entry.Value.DeepClone();
                actor.ProvisioningRecords = pending;
                bool retry = false;
                try
                {
                    foreach (var entry in pending)
                    {
                        var current = await store.Read(entry.Key);
                        string[] parts = entry.Key.Split('/');
                        var before = original[parts[0]]?[parts[1]] as JObject ?? new JObject();
                        if (!JToken.DeepEquals(current.Data, before)) { retry = true; break; }
                        if (!await store.SaveIfUnchanged(entry.Key, entry.Value, current.Etag)) { retry = true; break; }
                    }
                }
                finally { actor.ProvisioningRecords = null; }
                if (!retry) return;
            }
            throw new FirebaseException(409, "Attendance setup changed concurrently. Retry to finish the existing setup.");
        }
        private static string T(JToken n,string k) { return n is JObject && n[k]?.Type==JTokenType.String ? (string)n[k] : ""; }
        private static string Timestamp(JToken n, string key)
        {
            var value = n is JObject ? n[key] : null;
            return value?.Type == JTokenType.Date ? value.ToObject<DateTimeOffset>().ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) : T(n, key);
        }
        private static JObject O(JToken n) { return n as JObject ?? new JObject(); }
        public static void ApplyTrainingDefaults(JObject record)
        {
            record["attendanceMode"] = "flexible";
            record["trainingStartBasis"] = "companyAcceptance";
            record["timeZone"] = "Asia/Amman";
        }
        public static JObject TrainingPeriod(JObject application, JObject opportunity)
        {
            long weeks = AttendanceService.Number(application, "durationWeeks");
            if (weeks == 0) weeks = AttendanceService.Number(opportunity, "durationWeeks");
            if (weeks == 0) weeks = AttendanceService.Number(opportunity, "duration");
            DateTimeOffset accepted;
            string acceptedAt = Timestamp(application, "acceptedAt");
            if (acceptedAt.Length == 0) acceptedAt = Timestamp(application, "decidedAt");
            if (weeks < 1 || weeks > 52 || !DateTimeOffset.TryParse(acceptedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out accepted))
                throw new FirebaseException(409, "A valid company acceptance date and duration of 1 to 52 weeks are required.");
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            var first = TimeZoneInfo.ConvertTime(accepted, zone).Date;
            var period = new JObject { ["acceptedAt"] = accepted.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                ["durationWeeks"] = weeks, ["startDate"] = first.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["endDate"] = first.AddDays(weeks * 7 - 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["attendanceMode"] = "flexible" };
            ApplyTrainingDefaults(period);
            return period;
        }
        // Read-only roster: accepted trainees remain visible before attendance provisioning.
        // This does not grant QR access or create an attendance identity.
        public static void AppendPendingPlacements(JArray roster, JObject placements, JObject users, string supervisorId, JObject supervisor)
        {
            foreach (var entry in placements.Properties())
            {
                var p=entry.Value;string assigned=T(p,"companySupervisorId");if(assigned.Length==0)assigned=T(p,"supervisorId");
                if(assigned!=supervisorId || T(p,"companyId")!=T(supervisor,"companyId") || !new[]{"active","confirmed"}.Contains(T(p,"status")))continue;
                string id=T(p,"studentId"),uid=T(p,"studentUid");
                if(id.Length==0 || uid.Length==0 || roster.Any(s=>T(s,"id")==id))continue;
                var student=users[uid];
                if(T(student,"studentId")!=id || (bool?)student?["isActive"]!=true || FirebaseSignInService.CanonicalRole(T(student,"role"))!="student")continue;
                string name=T(student,"fullName");if(name.Length==0)name=T(student,"displayName");if(name.Length==0)name=id;
                string reason=string.IsNullOrWhiteSpace(T(p,"endDate"))?"Training end date is missing.":"Attendance assignment and calendar need setup.";
                roster.Add(new JObject{["id"]=id,["name"]=name,["placementId"]=entry.Name,["qrEligible"]=false,["attendanceSetupRequired"]=true,["setupMessage"]=reason});
            }
        }
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
            // No shift calendar is required for flexible attendance. Existing calendars are still validated and preserved.
            foreach(var entry in T(placement,"attendanceMode")=="flexible" ? new JObject().Properties() : schedule.Properties())
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
            if (!assignment.HasValues) ((JObject)copy["trainingAssignments"][assignmentId])["periodVerifiedFromAcceptance"] = Timestamp(placement, "acceptedAt").Length > 0;
            if (!assignment.HasValues)
                foreach (string field in new[] { "acceptedAt", "durationWeeks", "attendanceMode", "trainingStartBasis", "timeZone" })
                    if (placement[field] != null) copy["trainingAssignments"][assignmentId][field] = placement[field].DeepClone();
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
