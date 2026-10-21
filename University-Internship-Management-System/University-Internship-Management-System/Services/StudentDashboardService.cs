using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public enum DashboardReadState { Unavailable, Ready, PermissionDenied, SchemaMismatch, NotPlaced }

    public sealed class DashboardItem
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Status { get; set; }
        public string DateLabel { get; set; }
        internal DateTime SortDate { get; set; }
    }

    public sealed class StudentDashboardData
    {
        public DashboardReadState ApplicationsState { get; set; }
        public DashboardReadState PlacementState { get; set; }
        public DashboardReadState TasksState { get; set; }
        public int? ApplicationCount { get; set; }
        public int? ReviewCount { get; set; }
        public int? PendingTasks { get; set; }
        public int? ReviewedTasks { get; set; }
        public int? TotalTasks { get; set; }
        public decimal? RequiredHours { get; set; }
        public decimal? ApprovedHours { get; set; }
        public string PlacementStatus { get; set; }
        public List<DashboardItem> Applications { get; } = new List<DashboardItem>();
        public List<DashboardItem> Deadlines { get; } = new List<DashboardItem>();
    }

    public sealed class StudentDashboardService
    {
        private readonly FirebaseRealtimeDatabaseRepository database;
        public StudentDashboardService(FirebaseRealtimeDatabaseRepository database) { this.database = database; }

        public async Task<StudentDashboardData> LoadAsync(UimsAccount account)
        {
            if (account == null || account.Role != "student" || account.MustChangePassword) throw new UnauthorizedAccessException();
            var result = new StudentDashboardData();
            await Task.WhenAll(LoadApplicationsAsync(account, result), LoadTrainingAsync(account, result)).ConfigureAwait(false);
            return result;
        }

        private async Task LoadApplicationsAsync(UimsAccount account, StudentDashboardData result)
        {
            try
            {
                var records = await database.ReadStudentRecordsAsync("applications", account).ConfigureAwait(false);
                var rows = records.Properties().Select(item => (JObject)item.Value).ToArray();
                if (rows.Any(row => !new[] { "accepted", "under_review", "submitted", "rejected", "withdrawn" }.Contains(Text(row, "status"))))
                    throw new InvalidDataException("Unknown application status.");
                var recent = rows.OrderByDescending(row => Timestamp(row["submittedAt"])).ThenBy(row => Text(row, "id")).Take(3);
                foreach (var row in recent)
                {
                    string opportunityId = Text(row, "opportunityId"), companyId = Text(row, "companyId");
                    FirebaseRealtimeDatabaseRepository.ValidateSegment(opportunityId);
                    FirebaseRealtimeDatabaseRepository.ValidateSegment(companyId);
                    JObject opportunity = null;
                    try { opportunity = await database.ReadAsync("opportunities", opportunityId).ConfigureAwait(false) as JObject; }
                    catch (System.Net.Http.HttpRequestException) { }
                    if (opportunity != null && (Text(opportunity, "id") != opportunityId || Text(opportunity, "companyId") != companyId || Text(opportunity, "status") != "published"))
                        throw new InvalidDataException("Opportunity relationship mismatch.");
                    result.Applications.Add(new DashboardItem { Title = opportunity == null ? "Application " + Text(row, "id") : Text(opportunity, "title"),
                        Subtitle = opportunity == null ? companyId : Text(opportunity, "companyNameSnapshot") ?? companyId,
                        Status = Text(row, "status"), DateLabel = JordanTime.Format(Timestamp(row["submittedAt"])) });
                }
                result.ApplicationCount = rows.Length;
                result.ReviewCount = rows.Count(row => Text(row, "status") == "under_review");
                result.ApplicationsState = DashboardReadState.Ready;
                LoginDiagnostics.Write("StudentDashboardApplicationsVerified");
            }
            catch (Exception error) when (IsDataFailure(error))
            {
                result.Applications.Clear(); result.ApplicationsState = Failure(error);
            }
        }

        private async Task LoadTrainingAsync(UimsAccount account, StudentDashboardData result)
        {
            string placementId;
            try
            {
                var student = await database.ReadStudentProfileAsync(account).ConfigureAwait(false);
                placementId = Text(student, "placementId");
                if (string.IsNullOrWhiteSpace(placementId))
                {
                    result.PlacementState = result.TasksState = DashboardReadState.NotPlaced;
                    result.PendingTasks = result.ReviewedTasks = result.TotalTasks = 0;
                    return;
                }
                var placement = await database.ReadStudentPlacementAsync(account, placementId).ConfigureAwait(false);
                result.PlacementStatus = Text(placement, "status");
                result.RequiredHours = Hours(placement["requiredHours"]);
                result.ApprovedHours = Hours(placement["approvedHours"]);
                result.PlacementState = DashboardReadState.Ready;
                LoginDiagnostics.Write("StudentDashboardPlacementVerified");
            }
            catch (Exception error) when (IsDataFailure(error))
            {
                result.RequiredHours = result.ApprovedHours = null;
                result.PlacementState = result.TasksState = Failure(error);
                return;
            }
            try
            {
                var records = await database.ReadPlacementTasksAsync(account, placementId).ConfigureAwait(false);
                var rows = records.Properties().Select(item => (JObject)item.Value).ToArray();
                if (rows.Any(row => !new[] { "todo", "in_progress", "submitted", "reviewed" }.Contains(Text(row, "status"))))
                    throw new InvalidDataException("Unknown task status.");
                var pending = rows.Where(row => Text(row, "status") != "reviewed").Select(row => new DashboardItem {
                    Title = Text(row, "title") ?? "Task " + Text(row, "id"), Subtitle = Text(row, "status"), Status = Text(row, "status"),
                    DateLabel = CalendarDate(row["dueDate"]).ToString("dd MMM yyyy", CultureInfo.InvariantCulture), SortDate = CalendarDate(row["dueDate"]) });
                result.Deadlines.AddRange(pending.OrderBy(item => item.SortDate).ThenBy(item => item.Title).Take(3));
                result.TotalTasks = rows.Length;
                result.ReviewedTasks = rows.Count(row => Text(row, "status") == "reviewed");
                result.PendingTasks = result.TotalTasks - result.ReviewedTasks;
                result.TasksState = DashboardReadState.Ready;
                LoginDiagnostics.Write("StudentDashboardTasksVerified");
            }
            catch (Exception error) when (IsDataFailure(error))
            { result.Deadlines.Clear(); result.TasksState = Failure(error); }
        }

        private static string Text(JObject record, string key)
        {
            var token = record[key];
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type != JTokenType.String) throw new InvalidDataException("Invalid string field.");
            return (string)token;
        }
        private static decimal Hours(JToken token)
        {
            if (token == null || token.Type != JTokenType.Integer && token.Type != JTokenType.Float) throw new InvalidDataException("Hours are not recorded.");
            decimal hours = (decimal)token;
            if (hours < 0) throw new InvalidDataException("Invalid hours.");
            return hours;
        }
        private static DateTime Timestamp(JToken token)
        {
            if (token?.Type == JTokenType.Date) return token.ToObject<DateTimeOffset>().UtcDateTime;
            DateTimeOffset parsed;
            string value = token?.Type == JTokenType.String ? (string)token : null;
            if (value == null || !value.Contains("T") || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                throw new InvalidDataException("Invalid timestamp.");
            return parsed.UtcDateTime;
        }
        private static DateTime CalendarDate(JToken token)
        {
            DateTime parsed;
            string value = token?.Type == JTokenType.Date ? token.ToObject<DateTime>().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : (string)token;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)) throw new InvalidDataException("Invalid task date.");
            return parsed.Date;
        }
        private static bool IsDataFailure(Exception error) => error is System.Net.Http.HttpRequestException || error is TaskCanceledException ||
            error is InvalidDataException || error is JsonException || error is ArgumentException || error is OverflowException;
        private static DashboardReadState Failure(Exception error)
        {
            if (error is StudentDataReadException read && (read.HttpStatus == 401 || read.HttpStatus == 403)) return DashboardReadState.PermissionDenied;
            if (error is InvalidDataException || error is JsonException || error is ArgumentException || error is OverflowException) return DashboardReadState.SchemaMismatch;
            return DashboardReadState.Unavailable;
        }
    }
}
