using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    public interface IApplicationDatabase
    {
        Task<JToken> ReadAsync(string path);
        Task<JToken> ReadEqualAsync(string path, string field, string value);
        Task<StudentVersion> ReadVersionAsync(string path);
        Task<bool> CompareWriteAsync(string path, object value, string etag);
        Task<bool> IsActiveAuthenticationUidAsync(string uid);
    }

    internal sealed class FirebaseApplicationDatabase : IApplicationDatabase
    {
        private readonly StudentFirebaseGateway gateway;
        public FirebaseApplicationDatabase(StudentFirebaseGateway gateway) { this.gateway = gateway; }
        public Task<JToken> ReadAsync(string path) { return gateway.ReadAsync(path); }
        public Task<JToken> ReadEqualAsync(string path, string field, string value) { return gateway.ReadEqualAsync(path, field, value); }
        public Task<StudentVersion> ReadVersionAsync(string path) { return gateway.ReadVersionAsync(path); }
        public Task<bool> CompareWriteAsync(string path, object value, string etag) { return gateway.CompareWriteAsync(path, value, etag); }
        public Task<bool> IsActiveAuthenticationUidAsync(string uid) { return gateway.IsActiveAuthenticationUidAsync(uid); }
    }

    /// <summary>Authorized Firebase application and placement lifecycle operations.</summary>
    public sealed class ApplicationWorkflowService
    {
        private readonly IApplicationDatabase database;
        private readonly Func<bool> attendanceWritesAllowed;
        public ApplicationWorkflowService() : this(new FirebaseApplicationDatabase(new StudentFirebaseGateway())) { }
        public ApplicationWorkflowService(StudentFirebaseGateway gateway) : this(new FirebaseApplicationDatabase(gateway)) { }
        public ApplicationWorkflowService(IApplicationDatabase database, Func<bool> attendanceWritesAllowed = null) { this.database = database ?? throw new ArgumentNullException(nameof(database)); this.attendanceWritesAllowed=attendanceWritesAllowed ?? (()=>string.Equals(System.Configuration.ConfigurationManager.AppSettings["AttendanceLiveWritesEnabled"],"true",StringComparison.OrdinalIgnoreCase)); }

        public async Task<JObject> LoadStudentAsync(string uid)
        {
            var actor = await ResolveAsync(uid, "student").ConfigureAwait(false);
            var apps = await database.ReadEqualAsync("applications", "studentId", actor.StudentId).ConfigureAwait(false) as JObject ?? new JObject();
            if (!string.Equals(actor.StudentId, uid, StringComparison.Ordinal)) Merge(apps, await database.ReadEqualAsync("applications", "studentId", uid).ConfigureAwait(false) as JObject);
            var placements = await database.ReadEqualAsync("placements", "studentId", actor.StudentId).ConfigureAwait(false) as JObject ?? new JObject();
            if (!string.Equals(actor.StudentId, uid, StringComparison.Ordinal)) Merge(placements, await database.ReadEqualAsync("placements", "studentId", uid).ConfigureAwait(false) as JObject);
            Merge(placements, await database.ReadEqualAsync("placements", "studentProfileId", actor.StudentId).ConfigureAwait(false) as JObject);
            var published = await database.ReadEqualAsync("opportunities", "status", "published").ConfigureAwait(false) as JObject ?? new JObject();
            var open = await database.ReadEqualAsync("opportunities", "status", "open").ConfigureAwait(false) as JObject ?? new JObject();
            foreach (var p in open.Properties()) if (published[p.Name] == null) published[p.Name] = p.Value.DeepClone();
            DateTime today = DateTime.UtcNow.Date;
            var choices = new JObject();
            foreach (var p in published.Properties())
            {
                var o = p.Value as JObject; if (o == null) continue;
                string department = (string)actor.Profile["department"] ?? "";
                bool major = string.Equals((string)o["department"], department, StringComparison.OrdinalIgnoreCase) ||
                    (o["majors"] as JArray)?.Any(x => string.Equals((string)x, department, StringComparison.OrdinalIgnoreCase)) == true;
                DateTime deadline;
                if (major && DateTime.TryParse((string)o["deadline"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out deadline) && deadline.Date >= today)
                    choices[p.Name] = WithId(p.Name, o);
            }
            foreach (var app in apps.Properties())
            {
                var item = app.Value as JObject; var id = (string)item?["opportunityId"];
                if (id != null && choices[id] == null)
                {
                    var opportunity = await database.ReadAsync("opportunities/" + Key(id)).ConfigureAwait(false) as JObject;
                    if (opportunity != null) choices[id] = WithId(id, opportunity);
                }
                if (item != null && item["id"] == null) item["id"] = app.Name;
            }
            var companies = new JObject();
            foreach (var p in choices.Properties()) { var o = (JObject)p.Value; var cid = (string)o["companyId"]; if (!string.IsNullOrWhiteSpace(cid) && companies[cid] == null) companies[cid] = new JObject { ["id"] = cid, ["name"] = (string)o["companyName"] ?? "" }; }
            var publicProfile = (JObject)actor.Profile.DeepClone(); publicProfile["role"] = actor.Role;
            return new JObject {
                ["user"] = publicProfile.DeepClone(), ["users"] = new JArray(publicProfile.DeepClone()),
                ["companies"] = new JArray(companies.Properties().Select(p => p.Value.DeepClone())), ["opportunities"] = new JArray(choices.Properties().Select(p => p.Value.DeepClone())),
                ["applications"] = new JArray(apps.Properties().Where(p => p.Value is JObject).Select(p => WithId(p.Name, (JObject)p.Value))),
                ["placements"] = new JArray(placements.Properties().Where(p => p.Value is JObject).Select(p => WithId(p.Name, (JObject)p.Value))),
                ["tasks"] = new JArray(), ["submissions"] = new JArray(), ["progress"] = new JArray(), ["messages"] = new JArray()
            };
        }

        public async Task<JObject> LoadCompanyAsync(string uid)
        {
            var actor = await ResolveAsync(uid, "companySupervisor").ConfigureAwait(false);
            var apps = await database.ReadEqualAsync("applications", "companyId", actor.CompanyId).ConfigureAwait(false) as JObject ?? new JObject();
            var opportunities = await database.ReadEqualAsync("opportunities", "companyId", actor.CompanyId).ConfigureAwait(false) as JObject ?? new JObject();
            var company = await database.ReadAsync("companies/" + Key(actor.CompanyId)).ConfigureAwait(false) as JObject;
            if (company != null && company["id"] == null) company["id"] = actor.CompanyId;
            var students = new JArray();
            foreach (string studentId in apps.Properties().Select(p => (string)(p.Value as JObject)?["studentId"]).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
            {
                var profiles = await database.ReadEqualAsync("users", "studentId", studentId).ConfigureAwait(false) as JObject;
                if (profiles != null) foreach (var profile in profiles.Properties()) if (profile.Value is JObject) students.Add(profile.Value.DeepClone());
                if (profiles == null || !profiles.HasValues)
                {
                    var legacyUidProfile = await database.ReadAsync("users/" + Key(studentId)).ConfigureAwait(false) as JObject;
                    if (legacyUidProfile != null && FirebaseSignInService.CanonicalRole((string)legacyUidProfile["role"]) == "student") students.Add(legacyUidProfile.DeepClone());
                }
            }
            var publicProfile = (JObject)actor.Profile.DeepClone(); publicProfile["role"] = actor.Role;
            return new JObject {
                ["user"] = publicProfile, ["users"] = students, ["companies"] = company == null ? new JArray() : new JArray(company),
                ["opportunities"] = new JArray(opportunities.Properties().Where(p => p.Value is JObject).Select(p => WithId(p.Name, (JObject)p.Value))),
                ["applications"] = new JArray(apps.Properties().Where(p => p.Value is JObject).Select(p => WithId(p.Name, (JObject)p.Value))),
                ["placements"] = new JArray(), ["tasks"] = new JArray(), ["submissions"] = new JArray(), ["progress"] = new JArray(), ["messages"] = new JArray()
            };
        }

        public async Task<string> ApplyAsync(string uid, JObject input)
        {
            var actor = await ResolveAsync(uid, "student").ConfigureAwait(false);
            string opportunityId = Key((string)input?["opportunityId"]);
            string phone = ((string)input["phone"] ?? "").Trim();
            string cvLink = ((string)input["cvLink"] ?? "").Trim();
            if (phone.Length == 0 || phone.Length > 80 || cvLink.Length == 0 || cvLink.Length > 2048 || !Uri.TryCreate(cvLink, UriKind.Absolute, out var cv) || (cv.Scheme != "https" && cv.Scheme != "http"))
                throw new FirebaseException(400, "Provide a valid phone number and CV link.");
            var opportunity = await database.ReadAsync("opportunities/" + opportunityId).ConfigureAwait(false) as JObject;
            if (opportunity == null || !IsOpenAndEligible(opportunity, actor.Profile)) throw new FirebaseException(409, "This opportunity is closed, expired, or not eligible for your department.");
            var prior = await database.ReadEqualAsync("applications", "studentId", actor.StudentId).ConfigureAwait(false) as JObject;
            if (prior?.Properties().Any(p => string.Equals((string)(p.Value as JObject)?["opportunityId"], opportunityId, StringComparison.Ordinal)) == true)
                throw new FirebaseException(409, "An application for this opportunity already exists.");
            var placements = await database.ReadEqualAsync("placements", "studentId", actor.StudentId).ConfigureAwait(false) as JObject;
            if (placements?.HasValues == true) throw new FirebaseException(409, "A placement already exists for this student.");
            string appId = "APP-" + Hash(actor.StudentId + "|" + opportunityId).Substring(0, 28);
            var existing = await database.ReadVersionAsync("applications/" + appId).ConfigureAwait(false);
            if (existing.Value != null) throw new FirebaseException(409, "This application already exists.");
            var record = new JObject {
                ["id"] = appId, ["studentId"] = actor.StudentId, ["studentUid"] = uid,
                ["studentName"] = (string)actor.Profile["fullName"] ?? (string)actor.Profile["name"] ?? "",
                ["opportunityId"] = opportunityId, ["companyId"] = (string)opportunity["companyId"],
                ["companyName"] = (string)opportunity["companyName"] ?? "", ["status"] = "pending",
                ["phone"] = phone, ["cvLink"] = cvLink, ["portfolioLink"] = (string)input["portfolioLink"] ?? "",
                ["motivation"] = ((string)input["motivation"] ?? "").Trim(), ["reason"] = "",
                ["createdAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            if (string.IsNullOrWhiteSpace(existing.ETag) || !await database.CompareWriteAsync("applications/" + appId, record, existing.ETag).ConfigureAwait(false))
                throw new FirebaseException(409, "The application could not be saved because another request changed it. Reload and retry.");
            return appId;
        }

        public async Task DecideAsync(string uid, string applicationId, string decision, string reason)
        {
            var actor = await ResolveAsync(uid, "companySupervisor").ConfigureAwait(false);
            string id = Key(applicationId);
            decision = (decision ?? "").Trim().ToLowerInvariant();
            if (decision != "accepted" && decision != "rejected") throw new FirebaseException(400, "Choose accept or reject.");
            if (reason != null && reason.Length > 4000) throw new FirebaseException(400, "The decision note is too long.");
            var app = await database.ReadVersionAsync("applications/" + id).ConfigureAwait(false);
            if (app.Value == null || (string)app.Value["companyId"] != actor.CompanyId) throw new FirebaseException(404, "Application not found for this company.");
            if (string.Equals((string)app.Value["status"], decision, StringComparison.OrdinalIgnoreCase)) return;
            if (!string.Equals((string)app.Value["status"], "pending", StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(409, "This application has already been reviewed.");
            if (decision == "accepted")
            {
                string opportunityId = Key((string)app.Value["opportunityId"]);
                var opportunity = await database.ReadVersionAsync("opportunities/" + opportunityId).ConfigureAwait(false);
                if (opportunity.Value == null || (string)opportunity.Value["companyId"] != actor.CompanyId) throw new FirebaseException(409, "The linked company opportunity could not be verified.");
                int seats = opportunity.Value.Value<int?>("seats") ?? 0;
                var reservations = opportunity.Value["acceptedApplications"] as JObject ?? new JObject();
                var current = await database.ReadEqualAsync("applications", "opportunityId", opportunityId).ConfigureAwait(false) as JObject;
                if (current != null)
                {
                    foreach (var p in current.Properties())
                    {
                        string state = (string)(p.Value as JObject)?["status"];
                        if ((string.Equals(state, "accepted", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "confirmed", StringComparison.OrdinalIgnoreCase)) && reservations[p.Name] == null) reservations[p.Name] = true;
                    }
                }
                int accepted = reservations.Properties().Count();
                bool alreadyReserved = reservations[id] != null;
                if (!alreadyReserved && (seats < 1 || accepted >= seats)) throw new FirebaseException(409, "All available seats are filled.");
                if (!alreadyReserved)
                {
                    reservations[id] = true;
                    opportunity.Value["acceptedApplications"] = reservations;
                    opportunity.Value["acceptedCount"] = accepted + 1;
                    opportunity.Value["updatedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    if (string.IsNullOrWhiteSpace(opportunity.ETag) || !await database.CompareWriteAsync("opportunities/" + opportunityId, opportunity.Value, opportunity.ETag).ConfigureAwait(false))
                        throw new FirebaseException(409, "The opportunity changed during review. Reload and retry.");
                }
            }
            app.Value["status"] = decision;
            app.Value["reason"] = (reason ?? "").Trim();
            app.Value["decidedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            app.Value["companySupervisorId"] = actor.CompanySupervisorId;
            if (string.IsNullOrWhiteSpace(app.ETag) || !await database.CompareWriteAsync("applications/" + id, app.Value, app.ETag).ConfigureAwait(false))
                throw new FirebaseException(409, "The application changed during review. Reload before retrying.");
        }

        public async Task<string> ConfirmOfferAsync(string uid, string applicationId)
        {
            var actor = await ResolveAsync(uid, "student").ConfigureAwait(false);
            string id = Key(applicationId);
            var app = await database.ReadVersionAsync("applications/" + id).ConfigureAwait(false);
            if (app.Value == null || (string)app.Value["studentId"] != actor.StudentId) throw new FirebaseException(404, "Accepted application not found for this student.");
            bool alreadyConfirmed=string.Equals((string)app.Value["status"], "confirmed", StringComparison.OrdinalIgnoreCase);
            if (!alreadyConfirmed && !string.Equals((string)app.Value["status"], "accepted", StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(409, "Only an accepted application can be confirmed.");
            if(actor.StudentId=="2023301")throw new FirebaseException(409,"Student 2023301 is frozen pending the coordinator's academic assignment decision; no records were changed.");
            var academicIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (string collection in new[] { "supervisorAssignments", "assignments" })
                foreach (string studentKey in new[] { actor.StudentId, uid }.Distinct(StringComparer.Ordinal))
                {
                    var links = await database.ReadEqualAsync(collection,"studentId",studentKey).ConfigureAwait(false) as JObject;
                    foreach (var link in (links ?? new JObject()).Properties().Select(p=>p.Value as JObject).Where(p=>p!=null && (bool?)p["active"]!=false && (bool?)p["isActive"]!=false))
                    {
                        string academic = (string)link["academicSupervisorId"];
                        if (!string.IsNullOrWhiteSpace(academic)) academicIds.Add(academic);
                    }
                }
            if (academicIds.Count > 1) throw new FirebaseException(409,"Active academic-supervisor assignments disagree. The coordinator must reconcile them before placement confirmation; no claim or placement was created.");
            string assignedAcademicId = academicIds.FirstOrDefault();
            string profileAcademicId = (string)actor.Profile["academicSupervisorId"] ?? (string)actor.Profile["supervisorId"];
            if (!string.IsNullOrWhiteSpace(profileAcademicId) && !string.IsNullOrWhiteSpace(assignedAcademicId) && !string.Equals(profileAcademicId, assignedAcademicId, StringComparison.Ordinal))
                throw new FirebaseException(409, "The student profile and supervisor assignment disagree. Ask the coordinator to reconcile the assignment.");
            string academicId = assignedAcademicId ?? profileAcademicId;
            if (string.IsNullOrWhiteSpace(academicId)) throw new FirebaseException(409, "Assign an academic supervisor to this student before confirming the placement.");
            string placementId = alreadyConfirmed ? (string)app.Value["placementId"] ?? "PLC-"+id : "PLC-" + id;
            var existingForStudent = await database.ReadEqualAsync("placements", "studentId", actor.StudentId).ConfigureAwait(false) as JObject;
            Merge(existingForStudent = existingForStudent ?? new JObject(), await database.ReadEqualAsync("placements", "studentId", uid).ConfigureAwait(false) as JObject);
            Merge(existingForStudent, await database.ReadEqualAsync("placements", "studentProfileId", actor.StudentId).ConfigureAwait(false) as JObject);
            var priorForApplication = existingForStudent.Properties().FirstOrDefault(p => string.Equals(p.Name, placementId, StringComparison.Ordinal) ||
                string.Equals((string)(p.Value as JObject)?["applicationId"], id, StringComparison.Ordinal));
            if (priorForApplication != null) placementId = priorForApplication.Name;
            if (existingForStudent.Properties().Any(p => !string.Equals(p.Name, placementId, StringComparison.Ordinal) &&
                !string.Equals((string)(p.Value as JObject)?["applicationId"], id, StringComparison.Ordinal)))
                throw new FirebaseException(409, "A placement already exists for this student. Ask the coordinator to resolve the existing placement before confirming another offer.");
            string claimPath = "placementClaims/" + Key(actor.StudentId);
            var claim = await database.ReadVersionAsync(claimPath).ConfigureAwait(false);
            if (claim.Value != null && (string)claim.Value["applicationId"] != id)
                throw new FirebaseException(409, "This student already has a confirmed placement.");
            if (claim.Value != null && !string.IsNullOrWhiteSpace((string)claim.Value["placementId"])) placementId = (string)claim.Value["placementId"];
            // Validate the source offer before reserving a claim. Failed prerequisites
            // must not leave a partial reservation for a nonexistent opportunity.
            var acceptedOpportunity = await database.ReadAsync("opportunities/" + Key((string)app.Value["opportunityId"])).ConfigureAwait(false) as JObject;
            if (acceptedOpportunity == null) throw new FirebaseException(409,"The accepted opportunity record is unavailable. No placement claim was created.");
            foreach (var prior in existingForStudent.Properties().Select(p=>p.Value as JObject).Where(p=>p!=null))
                if (!string.IsNullOrWhiteSpace((string)prior["academicSupervisorId"]) && (string)prior["academicSupervisorId"]!=academicId)
                    throw new FirebaseException(409,"The existing placement and academic assignment disagree. Ask the coordinator to reconcile them; no records were overwritten.");
            var plannedPlacement = await database.ReadVersionAsync("placements/"+placementId).ConfigureAwait(false);
            if(plannedPlacement.Value!=null && (string)plannedPlacement.Value["applicationId"]!=id)throw new FirebaseException(409,"The existing placement belongs to another application; no attendance setup was created.");
            var placementForAttendance=plannedPlacement.Value;
            if(placementForAttendance==null)
            {
                string start=(string)acceptedOpportunity["startDate"] ?? (string)acceptedOpportunity["start"] ?? "";
                string end=(string)acceptedOpportunity["endDate"] ?? "";
                int duration=acceptedOpportunity.Value<int?>("durationWeeks") ?? acceptedOpportunity.Value<int?>("duration") ?? 0;
                DateTime startDate;
                if(end.Length==0 && duration>0 && duration<=104 && DateTime.TryParseExact(start,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out startDate))end=startDate.AddDays(duration*7-1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
                placementForAttendance=new JObject{["id"]=placementId,["studentId"]=actor.StudentId,["studentUid"]=uid,["companyId"]=app.Value["companyId"],
                    ["companySupervisorId"]=app.Value["companySupervisorId"],["academicSupervisorId"]=academicId,["startDate"]=start,["endDate"]=end,
                    ["requiredHours"]=acceptedOpportunity.Value<int?>("requiredHours") ?? 0};
            }
            var onboarding=new UIMS.Attendance.AttendanceOnboarding(database);
            bool attendanceChange=await onboarding.ValidateAsync(uid,actor.StudentId,placementId,placementForAttendance,actor.Profile).ConfigureAwait(false);
            if(alreadyConfirmed && !attendanceChange)return placementId;
            if(!attendanceWritesAllowed())throw new FirebaseException(503,"Attendance provisioning writes are disabled in this environment. Ask the administrator to approve attendance writes; no placement or claim was created.");
            if (claim.Value == null)
            {
                var claimRecord = new JObject { ["studentId"] = actor.StudentId, ["applicationId"] = id, ["placementId"] = placementId, ["claimedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) };
                if (string.IsNullOrWhiteSpace(claim.ETag) || !await database.CompareWriteAsync(claimPath, claimRecord, claim.ETag).ConfigureAwait(false))
                {
                    claim = await database.ReadVersionAsync(claimPath).ConfigureAwait(false);
                    if ((string)claim.Value?["applicationId"] != id || (string)claim.Value?["placementId"] != placementId) throw new FirebaseException(409, "Another offer was confirmed at the same time. Retry with the current placement claim.");
                }
            }
            var existingPlacement = await database.ReadVersionAsync("placements/" + placementId).ConfigureAwait(false);
            if (existingPlacement.Value != null && !string.Equals((string)existingPlacement.Value["applicationId"], id, StringComparison.Ordinal))
                throw new FirebaseException(409, "The placement identifier is already linked to a different application.");
            if (existingPlacement.Value == null)
            {
                var opportunity = acceptedOpportunity;
                var placement = new JObject {
                    ["id"] = placementId, ["applicationId"] = id, ["opportunityId"] = (string)app.Value["opportunityId"],
                    ["studentId"] = actor.StudentId, ["studentUid"] = uid, ["companyId"] = (string)app.Value["companyId"],
                    ["companySupervisorId"] = (string)app.Value["companySupervisorId"] ?? "",
                    ["academicSupervisorId"] = academicId, ["status"] = "confirmed",
                    ["startDate"] = (string)opportunity["startDate"] ?? (string)opportunity["start"] ?? "",
                    ["durationWeeks"] = opportunity.Value<int?>("durationWeeks") ?? opportunity.Value<int?>("duration") ?? 0,
                    ["requiredHours"] = opportunity.Value<int?>("requiredHours") ?? 0,
                    ["createdAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                };
                placement["endDate"]=placementForAttendance["endDate"];
                if (string.IsNullOrWhiteSpace(existingPlacement.ETag) || !await database.CompareWriteAsync("placements/" + placementId, placement, existingPlacement.ETag).ConfigureAwait(false))
                    throw new FirebaseException(409, "The placement is being created by another request. Retry to finish confirmation.");
            }
            await onboarding.EnsureAsync(uid,actor.StudentId,placementId,placementForAttendance,actor.Profile).ConfigureAwait(false);
            if(alreadyConfirmed)return placementId;
            app.Value["status"] = "confirmed";
            app.Value["placementId"] = placementId;
            app.Value["confirmedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            if (!await database.CompareWriteAsync("applications/" + id, app.Value, app.ETag).ConfigureAwait(false))
            {
                var current = await database.ReadAsync("applications/" + id).ConfigureAwait(false) as JObject;
                if ((string)current?["status"] != "confirmed" || (string)current?["placementId"] != placementId)
                    throw new FirebaseException(409, "The placement exists, but application confirmation must be retried.");
            }
            return placementId;
        }

        private async Task<Actor> ResolveAsync(string uid, string requiredRole)
        {
            if (string.IsNullOrWhiteSpace(uid) || !await database.IsActiveAuthenticationUidAsync(uid).ConfigureAwait(false)) throw new FirebaseException(401, "A verified active Firebase Authentication account is required.");
            var profile = await database.ReadAsync("users/" + StudentManagementService.Key(uid)).ConfigureAwait(false) as JObject;
            var result = FirebaseSignInService.ProfileResult(uid, profile);
            if (result.Role != requiredRole) throw new FirebaseException(403, "The account does not have permission for this application workflow.");
            var actor = new Actor { Profile = profile, Role = result.Role,
                StudentId = (string)profile["studentId"] ?? "",
                CompanyId = (string)profile["companyId"] ?? "",
                CompanySupervisorId = (string)profile["companySupervisorId"] ?? (string)profile["supervisorId"] ?? (string)profile["id"] ?? "" };
            if (requiredRole == "student" && actor.StudentId.Length == 0) throw new FirebaseException(403, "The student profile is not linked to a university student ID.");
            if (requiredRole == "companySupervisor" && (actor.CompanyId.Length == 0 || actor.CompanySupervisorId.Length == 0)) throw new FirebaseException(403, "The company supervisor profile must be linked to a company and supervisor ID.");
            return actor;
        }

        private static bool IsOpenAndEligible(JObject opportunity, JObject profile)
        {
            string status = (string)opportunity["status"] ?? "";
            if (!string.Equals(status, "open", StringComparison.OrdinalIgnoreCase) && !string.Equals(status, "published", StringComparison.OrdinalIgnoreCase)) return false;
            DateTime deadline; if (!DateTime.TryParse((string)opportunity["deadline"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out deadline) || deadline.Date < DateTime.UtcNow.Date) return false;
            string department = (string)profile["department"] ?? "";
            return string.Equals((string)opportunity["department"], department, StringComparison.OrdinalIgnoreCase) ||
                (opportunity["majors"] as JArray)?.Any(x => string.Equals((string)x, department, StringComparison.OrdinalIgnoreCase)) == true;
        }
        private static JObject WithId(string id, JObject value) { var clone = (JObject)value.DeepClone(); clone["id"] = id; if (clone["status"] != null) clone["status"] = clone["status"].ToString().ToLowerInvariant(); return clone; }
        private static void Merge(JObject target, JObject source) { if (source == null) return; foreach (var p in source.Properties()) if (target[p.Name] == null) target[p.Name] = p.Value.DeepClone(); }
        private static string Hash(string value) { using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2"))); }
        private static string Key(string value) { if (string.IsNullOrWhiteSpace(value) || value.Length > 180 || value.IndexOfAny(new[] { '.', '#', '$', '[', ']', '/' }) >= 0) throw new FirebaseException(400, "The record identifier is invalid."); return value; }
        private sealed class Actor { public JObject Profile; public string Role, StudentId, CompanyId, CompanySupervisorId; }
    }
}
