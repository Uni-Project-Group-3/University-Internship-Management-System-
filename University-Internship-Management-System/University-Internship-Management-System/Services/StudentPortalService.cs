using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    // A projection of authorized main-UIMS records. Cycle 3 identities/grades are never guessed.
    public sealed class StudentPortalService
    {
        private readonly FirebaseRealtimeDatabaseRepository database;
        public StudentPortalService(FirebaseRealtimeDatabaseRepository database) { this.database = database; }

        public async Task<JObject> LoadAsync(UimsAccount account, string section)
        {
            if (account == null || account.Role != "student" || account.MustChangePassword) throw new UnauthorizedAccessException();
            if (!new[] { "profile", "applications", "opportunities", "application", "tasks", "progress" }.Contains(section)) throw new ArgumentException("Unknown student section.");
            var states = new JObject();
            var result = new JObject { ["states"] = states, ["serverUtc"] = DateTime.UtcNow.ToString("o"),
                ["user"] = new JObject { ["name"] = account.DisplayName, ["email"] = account.Email },
                ["applications"] = new JArray(), ["opportunities"] = new JArray(), ["tasks"] = new JArray() };
            var student = await OptionalAsync(() => ReadStudentAsync(account), states, "profile") as JObject;
            if (student != null)
            {
                result["student"] = Project(student, "id", "fullName", "studentNumber", "email", "programId", "phone", "city", "portfolioUrl", "cvLink", "academicYear", "gpa");
                var programs = await OptionalAsync(() => database.ReadAsync("academicCatalog", "programs"), states, "catalog");
                if (programs != null)
                {
                    var entries = programs is JArray array ? array.Children<JObject>() : (programs as JObject)?.Properties().Select(item => item.Value).OfType<JObject>();
                    var program = entries?.FirstOrDefault(item => (string)item["id"] == (string)student["programId"]);
                    result["student"]["department"] = (string)program?["nameEn"] ?? (string)student["programId"];
                }
                string placementId = (string)student["placementId"];
                if (!string.IsNullOrWhiteSpace(placementId))
                {
                    var placement = await OptionalAsync(async () => await ReadPlacementAsync(account, placementId), states, "placement") as JObject;
                    if (placement != null)
                    {
                        result["placement"] = Project(placement, "id", "studentId", "companyId", "opportunityId", "academicSupervisorId", "companySupervisorId", "status", "startDate", "endDate", "requiredHours", "approvedHours");
                        var tasks = await OptionalAsync(async () => await ReadTasksAsync(account, placementId), states, "tasks") as JObject;
                        if (tasks != null) result["tasks"] = Rows(tasks, "id", "placementId", "title", "description", "assignedBy", "dueDate", "status", "priority", "assignedAt", "evidenceLinks");
                    }
                }
                else { states["placement"] = "not_placed"; states["tasks"] = "not_placed"; }
            }
            var applications = await OptionalAsync(async () => await database.ReadStudentRecordsAsync("applications", account), states, "applications") as JObject;
            if (applications != null) result["applications"] = Rows(applications, "id", "opportunityId", "companyId", "status", "submittedAt", "appliedAt", "reviewNotes", "studentStatement", "attachments");
            if (section == "opportunities" || section == "application")
            {
                var opportunities = await OptionalAsync(async () => await database.ReadPublishedOpportunitiesAsync(account), states, "opportunities") as JObject;
                if (opportunities != null) result["opportunities"] = Rows(opportunities, OpportunityFields);
            }
            else
            {
                var ids = ((JArray)result["applications"]).Select(item => (string)item["opportunityId"])
                    .Concat(new[] { (string)result["placement"]?["opportunityId"] }).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct();
                foreach (string id in ids)
                {
                    FirebaseRealtimeDatabaseRepository.ValidateSegment(id);
                    var opportunity = await OptionalAsync(() => database.ReadAsync("opportunities", id), states, "opportunityDetails") as JObject;
                    if (opportunity != null)
                    {
                        if ((string)opportunity["id"] != id || (string)opportunity["status"] != "published") throw new InvalidDataException("Opportunity identity mismatch.");
                        ((JArray)result["opportunities"]).Add(Project(opportunity, OpportunityFields));
                    }
                }
            }
            LoginDiagnostics.Write("StudentPortalReadCompleted");
            foreach (var state in states.Properties().Where(item => (string)item.Value == "ready")) LoginDiagnostics.Write("StudentPortal" + state.Name + "ReadVerified");
            return result;
        }

        public async Task UpdateContactAsync(UimsAccount account, JObject input)
        {
            if (account == null || account.Role != "student" || account.MustChangePassword) throw new UnauthorizedAccessException();
            if (input == null || input.Count != 3 || input.Properties().Any(item => item.Name != "phone" && item.Name != "city" && item.Name != "portfolioUrl") || input.Properties().Any(item => item.Value.Type != JTokenType.String))
                throw new ArgumentException("Only phone, city and portfolio URL can be edited.");
            string phone = ((string)input["phone"]).Trim(), city = ((string)input["city"]).Trim(), portfolio = ((string)input["portfolioUrl"]).Trim();
            if (phone.Length > 25 || phone.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[+0-9() .-]{7,25}$")) throw new ArgumentException("Enter a valid phone number.");
            if (city.Length > 80 || portfolio.Length > 2000) throw new ArgumentException("The contact information is too long.");
            Uri url;
            if (portfolio.Length > 0 && (!Uri.TryCreate(portfolio, UriKind.Absolute, out url) || url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo))) throw new ArgumentException("Use a complete HTTPS portfolio URL without embedded credentials.");
            await database.ReadStudentProfileAsync(account).ConfigureAwait(false);
            await database.UpdateStudentContactAsync(account, new JObject { ["phone"] = phone, ["city"] = city, ["portfolioUrl"] = portfolio }).ConfigureAwait(false);
            LoginDiagnostics.Write("StudentContactUpdateConfirmed");
        }

        private async Task<JObject> ReadPlacementAsync(UimsAccount account, string placementId)
        {
            var placement = await database.ReadStudentPlacementAsync(account, placementId).ConfigureAwait(false);
            foreach (string field in new[] { "requiredHours", "approvedHours" })
                if (placement[field] == null || placement[field].Type != JTokenType.Integer && placement[field].Type != JTokenType.Float || (decimal)placement[field] < 0) throw new InvalidDataException("Invalid recorded hours.");
            return placement;
        }
        private async Task<JObject> ReadTasksAsync(UimsAccount account, string placementId)
        {
            var tasks = await database.ReadPlacementTasksAsync(account, placementId).ConfigureAwait(false);
            if (tasks.Properties().Any(item => !new[] { "todo", "in_progress", "submitted", "reviewed" }.Contains((string)item.Value["status"]))) throw new InvalidDataException("Unknown task status.");
            return tasks;
        }
        private async Task<JToken> ReadStudentAsync(UimsAccount account) => await database.ReadStudentProfileAsync(account);
        private static readonly string[] OpportunityFields = { "id", "companyId", "title", "companyNameSnapshot", "description", "eligibleProgramIds", "location", "workMode", "capacity", "status", "applicationPolicy", "canApply", "applicationDeadline", "requirements", "workHoursPerWeek" };
        private static JObject Project(JObject record, params string[] fields)
        {
            var output = new JObject();
            foreach (string field in fields) if (record[field] != null) output[field] = record[field].DeepClone();
            return output;
        }
        private static JArray Rows(JObject records, params string[] fields)
        {
            return new JArray(records.Properties().Select(item => { var row = Project((JObject)item.Value, fields); row["id"] = item.Name; return row; }));
        }
        private static async Task<JToken> OptionalAsync(Func<Task<JToken>> read, JObject states, string key)
        {
            try { var value = await read().ConfigureAwait(false); states[key] = "ready"; return value; }
            catch (StudentDataReadException error) { states[key] = error.HttpStatus == 401 || error.HttpStatus == 403 ? "permission_denied" : "unavailable"; return null; }
            catch (Exception error) when (error is HttpRequestException || error is TaskCanceledException || error is JsonException || error is InvalidDataException || error is OverflowException)
            { states[key] = error is InvalidDataException || error is JsonException || error is OverflowException ? "schema_mismatch" : "unavailable"; return null; }
        }
    }
}
