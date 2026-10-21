using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public sealed class RoleDashboardData
    {
        public int?[] Counts { get; } = new int?[4];
        public List<DashboardItem> First { get; } = new List<DashboardItem>();
        public List<DashboardItem> Second { get; } = new List<DashboardItem>();
        public string Status { get; set; } = "Some dashboard reads are unavailable. No demonstration values are displayed.";
    }
    public sealed class RoleDashboardService
    {
        private readonly FirebaseRealtimeDatabaseRepository database;
        public RoleDashboardService(FirebaseRealtimeDatabaseRepository database) { this.database = database; }
        public async Task<RoleDashboardData> LoadAsync(UimsAccount account)
        {
            if (account == null || account.MustChangePassword || !new[] { "company", "academic", "admin" }.Contains(account.Role)) throw new UnauthorizedAccessException();
            var result = new RoleDashboardData();
            if (account.Role == "company") await LoadCompanyAsync(account, result);
            else if (account.Role == "academic") await LoadAcademicAsync(account, result);
            else await LoadCoordinatorAsync(account, result);
            if (result.Counts.All(value => value.HasValue)) result.Status = "Live Firebase dashboard records verified for your account. Task review and hours do not grant final completion approval.";
            LoginDiagnostics.Write("RoleDashboardReadCompleted");
            return result;
        }
        private async Task LoadCompanyAsync(UimsAccount account, RoleDashboardData result)
        {
            var opportunities = await ReadAsync(account, "opportunities");
            if (opportunities != null) result.Counts[0] = opportunities.Count(row => (string)row["status"] == "published");
            var applications = await ReadAsync(account, "applications");
            if (applications != null)
            {
                result.Counts[1] = applications.Count(row => new[] { "submitted", "under_review" }.Contains((string)row["status"]));
                foreach (var row in applications.OrderByDescending(row => Timestamp(row["submittedAt"])).Take(3))
                {
                    var opportunity = opportunities?.FirstOrDefault(item => (string)item["id"] == (string)row["opportunityId"]);
                    result.First.Add(new DashboardItem { Title = "Student " + Text(row, "studentId"), Subtitle = (string)opportunity?["title"] ?? Text(row, "opportunityId"), Status = Text(row, "status"), DateLabel = DateLabel(row["submittedAt"]) });
                }
            }
            var placements = await ReadAsync(account, "placements");
            if (placements == null) return;
            result.Counts[2] = placements.Count(row => (string)row["status"] == "active");
            var groups = await Task.WhenAll(placements.Select(placement => ReadAsync(account, "tasks", Text(placement, "id"))));
            if (groups.Any(group => group == null)) return;
            var tasks = groups.SelectMany(group => group).Where(row => (string)row["status"] == "submitted").ToArray();
            result.Counts[3] = tasks.Length;
            result.Second.AddRange(tasks.OrderBy(row => (string)row["dueDate"]).Take(3).Select(row => new DashboardItem { Title = Text(row, "title"), Subtitle = "Placement " + Text(row, "placementId"), Status = "submitted", DateLabel = Text(row, "dueDate") }));
        }
        private async Task LoadAcademicAsync(UimsAccount account, RoleDashboardData result)
        {
            var students = await ReadAsync(account, "students");
            if (students == null || string.IsNullOrWhiteSpace(account.ProgramId) || students.Any(row => (string)row["programId"] != account.ProgramId)) return;
            result.Counts[0] = students.Length;
            var placementTasks = students.Where(row => !string.IsNullOrWhiteSpace((string)row["placementId"])).Select(async student => {
                var placement = await ReadPlacementAsync(account, student);
                if (placement == null) return null;
                var tasks = await ReadAsync(account, "tasks", Text(placement, "id"));
                return tasks == null ? null : new { Student = student, Placement = placement, Tasks = tasks };
            });
            var records = await Task.WhenAll(placementTasks);
            if (records.Any(row => row == null)) return;
            result.Counts[1] = records.Count(row => (string)row.Placement["status"] == "active");
            result.Counts[2] = records.Count(row => row.Tasks.Any(task => (string)task["status"] != "reviewed" && IsOverdue((string)task["dueDate"])));
            // No grading/publication configuration is assumed; evaluation-due stays unavailable.
            result.First.AddRange(students.Take(3).Select(student => {
                var record = records.FirstOrDefault(item => (string)item.Student["id"] == (string)student["id"]);
                return new DashboardItem { Title = Text(student, "fullName"), Subtitle = record == null ? "No current placement" : "Company " + Text(record.Placement, "companyId") + " · " + Hours(record.Placement), Status = record == null ? "not_placed" : Text(record.Placement, "status"), DateLabel = Text(student, "studentNumber") };
            }));
            result.Second.AddRange(records.SelectMany(item => item.Tasks.Where(task => (string)task["status"] != "reviewed" && IsOverdue((string)task["dueDate"])).Select(task => new DashboardItem { Title = Text(task, "title"), Subtitle = Text(item.Student, "fullName"), Status = "overdue", DateLabel = Text(task, "dueDate") })).Take(3));
        }
        private async Task LoadCoordinatorAsync(UimsAccount account, RoleDashboardData result)
        {
            var students = await ReadAsync(account, "students"); if (students != null) result.Counts[0] = students.Length;
            var companies = await ReadAsync(account, "companies"); if (companies != null) result.Counts[1] = companies.Length;
            var opportunities = await ReadAsync(account, "opportunities"); if (opportunities != null) result.Counts[2] = opportunities.Count(row => (string)row["status"] == "published");
            var completions = await ReadAsync(account, "completionRecords");
            // Never infer readiness from hours or final scores. Only explicit awaiting-review states count.
            if (completions != null) { result.Counts[3] = completions.Count(row => new[] { "pending_review", "awaiting_approval" }.Contains((string)row["status"])); result.First.AddRange(completions.Take(3).Select(row => new DashboardItem { Title = "Student " + Text(row, "studentId"), Subtitle = "Placement " + Text(row, "placementId"), Status = Text(row, "status"), DateLabel = DateLabel(row["completedAt"]) })); }
            var messages = await ReadAsync(account, "contactMessages");
            if (messages != null) result.Second.AddRange(messages.Take(3).Select(row => new DashboardItem { Title = Text(row, "subject"), Subtitle = Text(row, "email"), Status = Text(row, "status"), DateLabel = "" }));
        }
        private async Task<JObject> ReadPlacementAsync(UimsAccount account, JObject student)
        {
            try {
                string id = Text(student, "placementId"); FirebaseRealtimeDatabaseRepository.ValidateSegment(id);
                var record = await database.ReadAsync("placements", id) as JObject;
                if (record == null || Text(record, "id") != id || Text(record, "studentId") != Text(student, "id") || Text(record, "academicSupervisorId") != account.ProfileId) return null;
                return record;
            } catch (Exception error) when (DataFailure(error)) { return null; }
        }
        private async Task<JObject[]> ReadAsync(UimsAccount account, string collection, string placement = null)
        {
            try { var rows = await database.ReadRoleDashboardRecordsAsync(account, collection, placement); LoginDiagnostics.Write("RoleDashboard" + collection + "ReadVerified"); return rows.Properties().Select(item => { var row = (JObject)item.Value.DeepClone(); row["id"] = item.Name; return row; }).ToArray(); }
            catch (Exception error) when (DataFailure(error)) { LoginDiagnostics.Write("RoleDashboardReadUnavailable"); return null; }
        }
        private static bool DataFailure(Exception error) => error is HttpRequestException || error is TaskCanceledException || error is InvalidDataException || error is JsonException || error is ArgumentException;
        private static string Text(JObject row, string field) => (string)row[field] ?? "Not recorded";
        private static DateTimeOffset Timestamp(JToken token) { DateTimeOffset value; return DateTimeOffset.TryParse(Convert.ToString(token, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value) ? value : DateTimeOffset.MinValue; }
        private static string DateLabel(JToken token) { var date = Timestamp(token); return date == DateTimeOffset.MinValue ? "Date not recorded" : JordanTime.Format(date.UtcDateTime); }
        private static bool IsOverdue(string date) { DateTime parsed; return DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed) && parsed.Date < JordanTime.LocalNow.Date; }
        private static string Hours(JObject placement) { decimal required, approved; return decimal.TryParse((string)placement["requiredHours"], NumberStyles.Number, CultureInfo.InvariantCulture, out required) && decimal.TryParse((string)placement["approvedHours"], NumberStyles.Number, CultureInfo.InvariantCulture, out approved) && required >= 0 && approved >= 0 ? approved + " / " + required + " h" : "Hours not recorded"; }
        public static string Render(IEnumerable<DashboardItem> records, string empty)
        {
            var items = records.ToArray();
            if (items.Length == 0) return "<p class='stat-note'>" + HttpUtility.HtmlEncode(empty) + "</p>";
            return string.Concat(items.Select(item => "<div class='list-row'><div class='row-body'><div class='row-title'>" + HttpUtility.HtmlEncode(item.Title) + "</div><div class='row-sub'>" + HttpUtility.HtmlEncode(item.Subtitle) + "</div></div><div class='row-meta'><span class='status info'>" + HttpUtility.HtmlEncode(item.Status?.Replace('_',' ')) + "</span><div class='when'>" + HttpUtility.HtmlEncode(item.DateLabel) + "</div></div></div>"));
        }
    }
}
