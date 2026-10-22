using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    // A projection of existing records, never an alternative database or authorization source.
    public sealed class RolePortalService
    {
        private readonly FirebaseRealtimeDatabaseRepository database;
        public RolePortalService(FirebaseRealtimeDatabaseRepository database) { this.database = database; }
        private static readonly Dictionary<string, string> Pages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Company/ManageTasks", "company" }, { "Company/ReportsFinalEvaluation", "company" }, { "Company/ApplicationsReview", "company" }, { "Company/MyTrainees", "company" }, { "Company/ManageOpportunities", "company" },
            { "Academic/StudentProgressMonitor", "academic" }, { "Coordinator/ManageStudents", "admin" }, { "Coordinator/ManageCompanies", "admin" },
            { "Coordinator/MonitorPlacementsProgress", "admin" }, { "Coordinator/ContactMessages", "admin" },
            { "Company/CompanySupervisorAccount", "company" }, { "Academic/AcademicSupervisorAccount", "academic" }, { "Coordinator/CoordinatorAccountProfile", "admin" },
            { "Academic/FinalAcademicEvaluation", "academic" }, { "Coordinator/Manage_Academic_Supervisor", "admin" }, { "Coordinator/Assign_Supervisors", "admin" }, { "Coordinator/FinalCompletionReview", "admin" }
        };
        public static bool Supports(UimsAccount account, string route)
        {
            if (account == null || account.MustChangePassword || route == null) return false;
            route = route.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase) ? route.Substring(0, route.Length - 5) : route;
            return Pages.TryGetValue(route, out var role) && role == account.Role;
        }
        private static readonly Dictionary<string, string> Fields = new Dictionary<string, string> {
            { "academicSupervisors", "id fullName email programId departmentId active maxStudentLoad" },
            { "companySupervisors", "id fullName email companyId accountStatus" },
            { "students", "id fullName studentNumber programId academicSupervisorId placementId accountStatus" },
            { "companies", "id name city industry email accountStatus isVerified" },
            { "opportunities", "id companyId title description location workMode capacity status applicationDeadline eligibleProgramIds publishedAt requirements startDate durationWeeks requiredHours updatedAt" },
            { "applications", "id studentId opportunityId companyId status submittedAt appliedAt reviewNotes studentStatement lastUpdatedAt attachments applicantProgramId reviewedBy placementId confirmedAt" },
            { "placements", "id studentId companyId opportunityId academicSupervisorId companySupervisorId status startDate endDate requiredHours approvedHours" },
            { "progressReports", "id placementId studentId period summary status submittedAt hoursReported mentorComments" },
            { "tasks", "id placementId title description status dueDate assignedAt priority" },
            { "contactMessages", "id name email subject message status createdAt" },
            { "completionRecords", "id studentId placementId status completedAt" }
        };
        public async Task<JObject> LoadAsync(UimsAccount account, string route)
        {
            if (!Supports(account, route)) throw new UnauthorizedAccessException();
            string page = route.Substring(route.LastIndexOf('/') + 1).Replace(".aspx", "");
            var output = new JObject { ["user"] = new JObject { ["id"] = account.ProfileId, ["name"] = account.DisplayName, ["role"] = account.Role, ["email"] = account.Email, ["companyId"] = account.CompanyId }, ["states"] = new JObject(), ["serverUtc"] = DateTime.UtcNow.ToString("o") };
            if (page == "CompanySupervisorAccount" || page == "AcademicSupervisorAccount" || page == "CoordinatorAccountProfile")
            {
                var profile = await database.ReadAsync("users", account.ProfileId) as JObject;
                if (profile == null || !UserAccountService.HasMatchingUid(profile, account.Uid) || !UserAccountService.HasActiveStatus(profile)) throw new UnauthorizedAccessException();
                foreach (string field in new[] { "phone", "office", "officeHours", "programId", "companyId" }) if (profile[field] is JValue) output["user"][field] = profile[field].DeepClone();
                return output;
            }
            var collections = new List<string>();
            if (account.Role == "company") collections.AddRange(new[] { "opportunities", "applications", "placements" });
            else if (account.Role == "academic") collections.Add("students");
            else if (page == "ContactMessages") collections.Add("contactMessages");
            else if (page == "ManageCompanies") collections.AddRange(new[] { "companies", "companySupervisors" });
            else if (page == "Manage_Academic_Supervisor") collections.AddRange(new[] { "academicSupervisors", "students" });
            else if (page == "Assign_Supervisors") collections.AddRange(new[] { "academicSupervisors", "students" });
            else collections.AddRange(new[] { "students", "companies", "placements", "completionRecords" });
            foreach (string collection in collections) await ReadCollectionAsync(account, output, collection);
            if (page == "ReportsFinalEvaluation" && (string)output["states"]["placements"] == "ready") await ReadReportsAsync(account, output);
            if (account.Role == "academic") await ReadAcademicPlacementsAsync(account, output);
            if ((account.Role == "academic" || page == "ManageTasks") && (string)output["states"]["placements"] == "ready") await ReadTasksAsync(account, output);
            if (page == "ManageOpportunities")
            {
                var catalog = await database.ReadAsync("academicCatalog", "programs") as JToken;
                output["programs"] = catalog is JArray list ? list : new JArray((catalog as JObject)?.Properties().Select(p => p.Value) ?? Enumerable.Empty<JToken>());
            }
            if(account.Role=="academic"){
                try{output["weeklyReports"]=new JArray((await database.ReadTrainingQueryAsync(account,"weeklyReports")).Properties().Select(p=>p.Value.DeepClone()));output["states"]["weeklyReports"]="ready";}
                catch(Exception error) when(ReadFailure(error)){output["weeklyReports"]=new JArray();output["states"]["weeklyReports"]=error is StudentDataReadException?"permission_denied":"unavailable";}
            }
            // Coordinator task bulk reads and private evaluation/grade records are deliberately absent.
            return output;
        }

        // Identity and organization always come from the verified server-side account.
        public async Task<JObject> SaveOpportunityAsync(UimsAccount account, JObject input)
        {
            if (account == null || account.Role != "company" || account.MustChangePassword ||
                string.IsNullOrWhiteSpace(account.Uid) || string.IsNullOrWhiteSpace(account.CompanyId))
                throw new UnauthorizedAccessException();
            ValidateOpportunityInput(input);
            bool creating = string.IsNullOrEmpty((string)input["id"]);
            string id = creating ? "OPP-" + (string)input["requestId"] : (string)input["id"];
            FirebaseRealtimeDatabaseRepository.ValidateSegment(id);
            var version = await database.ReadVersionAsync("opportunities", id).ConfigureAwait(false);
            var existing = version.Value as JObject;
            if (existing != null && (string)existing["companyId"] != account.CompanyId) throw new UnauthorizedAccessException();
            if (creating && existing != null)
            {
                if ((string)existing["creationRequestId"] == (string)input["requestId"] && (string)existing["createdBy"] == account.ProfileId)
                    return new JObject { ["id"] = id, ["success"] = true, ["alreadySaved"] = true };
                throw new AccountConflictException();
            }
            if (!creating && existing == null) throw new ArgumentException("The opportunity no longer exists.");
            if (!creating && (string)input["expectedUpdatedAt"] != (string)existing["updatedAt"])
                throw new AccountConflictException();
            var catalog = await database.ReadAsync("academicCatalog", "programs").ConfigureAwait(false);
            var programs = catalog is JArray a ? a.OfType<JObject>() : (catalog as JObject)?.Properties().Select(p => p.Value).OfType<JObject>();
            var valid = programs?.Select(p => (string)p["id"]).ToArray() ?? new string[0];
            if (((JArray)input["eligibleProgramIds"]).Any(p => !valid.Contains((string)p)))
                throw new ArgumentException("Choose existing university programs.");
            var record = existing == null ? new JObject {
                ["id"] = id, ["companyId"] = account.CompanyId, ["createdBy"] = account.ProfileId,
                ["creationRequestId"] = (string)input["requestId"], ["publishedAt"] = DateTime.UtcNow.ToString("o"),
                ["applicationPolicy"] = "eligible_students", ["canApply"] = true
            } : (JObject)existing.DeepClone();
            foreach (string field in new[] { "title", "description", "requirements", "location", "workMode", "capacity",
                "eligibleProgramIds", "applicationDeadline", "startDate", "durationWeeks", "requiredHours", "status" })
                record[field] = input[field].DeepClone();
            record["applicationClosesAt"] = new DateTimeOffset(JordanTime.ParseInput((string)input["applicationDeadline"], "23:59")).ToUnixTimeMilliseconds();
            record["updatedAt"] = DateTime.UtcNow.ToString("o");
            await database.WriteCompanyOpportunityAsync(account, id, record, version.ETag).ConfigureAwait(false);
            return new JObject { ["id"] = id, ["success"] = true };
        }

        public static void ValidateOpportunityInput(JObject input)
        {
            string[] fields = { "id", "requestId", "expectedUpdatedAt", "title", "description", "requirements", "location",
                "workMode", "capacity", "eligibleProgramIds", "applicationDeadline", "startDate", "durationWeeks", "requiredHours", "status" };
            if (input == null || input.Properties().Any(p => !fields.Contains(p.Name)))
                throw new ArgumentException("Unexpected opportunity fields.");
            foreach (var limit in new[] { new { Field = "title", Max = 100 }, new { Field = "description", Max = 1000 },
                new { Field = "requirements", Max = 600 }, new { Field = "location", Max = 60 } })
                if (input[limit.Field]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)input[limit.Field]) ||
                    ((string)input[limit.Field]).Length > limit.Max) throw new ArgumentException("Enter valid " + limit.Field + ".");
            foreach (var range in new[] { new { Field = "capacity", Min = 1, Max = 50 }, new { Field = "durationWeeks", Min = 1, Max = 52 },
                new { Field = "requiredHours", Min = 1, Max = 5000 } })
                if (input[range.Field]?.Type != JTokenType.Integer || (long)input[range.Field] < range.Min ||
                    (long)input[range.Field] > range.Max) throw new ArgumentException("Enter valid " + range.Field + ".");
            if (!(input["eligibleProgramIds"] is JArray ids) || ids.Count == 0 || ids.Count > 100 ||
                ids.Any(p => p.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)p)) ||
                ids.Select(p => (string)p).Distinct().Count() != ids.Count) throw new ArgumentException("Choose at least one valid specialization.");
            if (!new[] { "onsite", "hybrid", "remote" }.Contains((string)input["workMode"]) ||
                !new[] { "published", "closed" }.Contains((string)input["status"])) throw new ArgumentException("Invalid opportunity mode or status.");
            if (!System.Text.RegularExpressions.Regex.IsMatch((string)input["requestId"] ?? "", "^[a-f0-9]{32}$"))
                throw new ArgumentException("Reload the opportunity form.");
            DateTime deadline, start;
            if (!DateTime.TryParseExact((string)input["applicationDeadline"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out deadline) ||
                !DateTime.TryParseExact((string)input["startDate"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out start) || start < deadline)
                throw new ArgumentException("Use valid dates; training cannot start before the application deadline.");
            if ((string)input["status"] == "published" && JordanTime.ParseInput((string)input["applicationDeadline"], "23:59") <= DateTime.UtcNow)
                throw new ArgumentException("A published opportunity needs a future Jordan Time deadline.");
        }

        private async Task ReadCollectionAsync(UimsAccount account, JObject output, string collection)
        {
            try {
                var records = await database.ReadRoleDashboardRecordsAsync(account, collection);
                output[collection] = Project(records, collection); output["states"][collection] = "ready";
            } catch (Exception error) when (ReadFailure(error)) { output["states"][collection] = "unavailable"; }
        }
        private async Task ReadAcademicPlacementsAsync(UimsAccount account, JObject output)
        {
            if ((string)output["states"]["students"] != "ready") { output["states"]["placements"] = "unavailable"; return; }
            var records = new JObject();
            try {
                foreach (JObject student in output["students"]) {
                    string id = (string)student["placementId"]; if (string.IsNullOrWhiteSpace(id)) continue;
                    FirebaseRealtimeDatabaseRepository.ValidateSegment(id);
                    var placement = await database.ReadAsync("placements", id) as JObject;
                    if (placement == null || (string)placement["id"] != id || (string)placement["studentId"] != (string)student["id"] || (string)placement["academicSupervisorId"] != account.ProfileId) throw new InvalidDataException();
                    records[id] = placement;
                }
                output["placements"] = Project(records, "placements"); output["states"]["placements"] = "ready";
            } catch (Exception error) when (ReadFailure(error)) { output["states"]["placements"] = "unavailable"; }
        }
        private async Task ReadReportsAsync(UimsAccount account, JObject output)
        {
            var records = new JObject();
            try {
                foreach (JObject placement in output["placements"]) {
                    var reports = await database.ReadRoleDashboardRecordsAsync(account, "progressReports", (string)placement["id"]);
                    foreach (var item in reports.Properties()) {
                        if ((string)item.Value["studentId"] != (string)placement["studentId"]) throw new InvalidDataException();
                        records[item.Name] = item.Value.DeepClone();
                    }
                }
                output["progressReports"] = Project(records, "progressReports"); output["states"]["progressReports"] = "ready";
            } catch (Exception error) when (ReadFailure(error)) { output["states"]["progressReports"] = "unavailable"; }
        }
        private async Task ReadTasksAsync(UimsAccount account, JObject output)
        {
            var records = new JObject();
            try {
                foreach (JObject placement in output["placements"]) {
                    var tasks = await database.ReadRoleDashboardRecordsAsync(account, "tasks", (string)placement["id"]);
                    foreach (var item in tasks.Properties()) records[item.Name] = item.Value.DeepClone();
                }
                output["tasks"] = Project(records, "tasks"); output["states"]["tasks"] = "ready";
            } catch (Exception error) when (ReadFailure(error)) { output["states"]["tasks"] = "unavailable"; }
        }
        internal static JArray Project(JObject records, string collection)
        {
            if (!Fields.TryGetValue(collection, out var fields)) throw new UnauthorizedAccessException();
            var result = new JArray();
            foreach (var item in records.Properties()) {
                if (!(item.Value is JObject row) || row["id"] != null && (string)row["id"] != item.Name) throw new InvalidDataException();
                var safe = new JObject { ["id"] = item.Name };
                foreach (string field in fields.Split(' ')) if (row[field] != null && field != "id") {
                    // Only the program-ID array is accepted; nested objects cannot smuggle private records.
                    if (field == "eligibleProgramIds" && row[field] is JArray ids && ids.All(id => id.Type == JTokenType.String)) safe[field] = ids.DeepClone();
                    else if (row[field] is JValue) safe[field] = row[field].DeepClone();
                }
                result.Add(safe);
            }
            return result;
        }
        private static bool ReadFailure(Exception error) => error is HttpRequestException || error is TaskCanceledException || error is InvalidDataException || error is JsonException || error is ArgumentException;
    }
}
