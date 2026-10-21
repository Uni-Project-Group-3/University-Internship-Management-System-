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
            { "opportunities", "id companyId title description location workMode capacity status applicationDeadline eligibleProgramIds publishedAt requirements" },
            { "applications", "id studentId opportunityId companyId status submittedAt appliedAt reviewNotes studentStatement" },
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
            // Coordinator task bulk reads and private evaluation/grade records are deliberately absent.
            return output;
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
