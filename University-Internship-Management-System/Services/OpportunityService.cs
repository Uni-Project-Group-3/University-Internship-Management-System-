using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Models;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    /// <summary>
    /// Realtime Database access for opportunities. No process-local collection or
    /// sample records are used. The actor is always resolved from the verified
    /// Firebase Authentication UID and its current /users/{uid} profile.
    /// </summary>
    public sealed class OpportunityService
    {
        private readonly StudentFirebaseGateway database;

        public OpportunityService() : this(new StudentFirebaseGateway()) { }
        public OpportunityService(StudentFirebaseGateway database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<IList<Opportunity>> GetByCompanyAsync(string verifiedUid)
        {
            var actor = await ResolveActorAsync(verifiedUid, "companySupervisor").ConfigureAwait(false);
            var rows = await database.ReadEqualAsync("opportunities", "companyId", actor.CompanyId).ConfigureAwait(false) as JObject;
            return ReadRows(rows).OrderByDescending(x => x.CreatedAt).ToList();
        }

        public async Task<IList<Opportunity>> GetPublishedForStudentAsync(string verifiedUid)
        {
            var actor = await ResolveActorAsync(verifiedUid, "student").ConfigureAwait(false);
            // Query the two status spellings already used by imported records. This
            // avoids downloading every opportunity and keeps legacy "open" rows visible.
            var published = await database.ReadEqualAsync("opportunities", "status", "published").ConfigureAwait(false) as JObject;
            var open = await database.ReadEqualAsync("opportunities", "status", "open").ConfigureAwait(false) as JObject;
            var rows = new JObject();
            if (published != null) foreach (var item in published.Properties()) rows[item.Name] = item.Value.DeepClone();
            if (open != null) foreach (var item in open.Properties()) if (rows[item.Name] == null) rows[item.Name] = item.Value.DeepClone();
            DateTime today = DateTime.UtcNow.Date;
            return ReadRows(rows).Where(x => IsPublished(x.Status) && x.Deadline.Date >= today &&
                x.Majors.Count > 0 && x.Majors.Any(m => string.Equals(m, actor.Department, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(x => x.Deadline).ToList();
        }

        public async Task<Opportunity> GetByIdAsync(string verifiedUid, string id)
        {
            var actor = await ResolveActorAsync(verifiedUid, null).ConfigureAwait(false);
            var value = await database.ReadAsync("opportunities/" + Key(id)).ConfigureAwait(false) as JObject;
            if (value == null) return null;
            if (actor.Role == "companySupervisor" && !string.Equals((string)value["companyId"], actor.CompanyId, StringComparison.Ordinal))
                throw new FirebaseException(403, "This opportunity belongs to another company.");
            if (actor.Role != "companySupervisor" && actor.Role != "student" && actor.Role != "coordinator")
                throw new FirebaseException(403, "Your account cannot view opportunities.");
            return Map(id, value);
        }

        public async Task<string> CreateAsync(string verifiedUid, Opportunity input)
        {
            var actor = await ResolveActorAsync(verifiedUid, "companySupervisor").ConfigureAwait(false);
            Validate(input, true);
            string id = "OP-" + Guid.NewGuid().ToString("N");
            var record = ToRecord(input, id, actor.CompanyId, actor.CompanyName);
            var snapshot = await database.ReadVersionAsync("opportunities/" + id).ConfigureAwait(false);
            if (snapshot.Value != null || string.IsNullOrWhiteSpace(snapshot.ETag))
                throw new FirebaseException(409, "Could not reserve a unique opportunity identifier.");
            if (!await database.CompareWriteAsync("opportunities/" + id, record, snapshot.ETag).ConfigureAwait(false))
                throw new FirebaseException(409, "Opportunity creation conflicted. Retry the operation.");
            return id;
        }

        public async Task UpdateAsync(string verifiedUid, Opportunity input)
        {
            var actor = await ResolveActorAsync(verifiedUid, "companySupervisor").ConfigureAwait(false);
            Validate(input, false);
            string id = Key(input.Id);
            var snapshot = await database.ReadVersionAsync("opportunities/" + id).ConfigureAwait(false);
            var current = snapshot.Value;
            if (current == null || !string.Equals((string)current["companyId"], actor.CompanyId, StringComparison.Ordinal))
                throw new FirebaseException(404, "Opportunity not found for this company.");

            int accepted = current.Value<int?>("acceptedCount") ?? 0;
            if (input.Seats < accepted)
                throw new FirebaseException(400, "Seats cannot be lower than the number of accepted students.");
            ApplyEditableFields(current, input);
            if (string.IsNullOrWhiteSpace(snapshot.ETag) ||
                !await database.CompareWriteAsync("opportunities/" + id, current, snapshot.ETag).ConfigureAwait(false))
                throw new FirebaseException(409, "Opportunity changed while you were editing it. Reload and retry.");
        }

        public async Task CloseAsync(string verifiedUid, string id)
        {
            var actor = await ResolveActorAsync(verifiedUid, "companySupervisor").ConfigureAwait(false);
            string key = Key(id);
            var snapshot = await database.ReadVersionAsync("opportunities/" + key).ConfigureAwait(false);
            if (snapshot.Value == null || !string.Equals((string)snapshot.Value["companyId"], actor.CompanyId, StringComparison.Ordinal))
                throw new FirebaseException(404, "Opportunity not found for this company.");
            snapshot.Value["status"] = "closed";
            snapshot.Value["updatedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(snapshot.ETag) ||
                !await database.CompareWriteAsync("opportunities/" + key, snapshot.Value, snapshot.ETag).ConfigureAwait(false))
                throw new FirebaseException(409, "Opportunity changed while closing it. Reload and retry.");
        }

        private async Task<Actor> ResolveActorAsync(string uid, string requiredRole)
        {
            if (string.IsNullOrWhiteSpace(uid)) throw new FirebaseException(401, "A verified Firebase Authentication UID is required.");
            if (!await database.IsActiveAuthenticationUidAsync(uid).ConfigureAwait(false))
                throw new FirebaseException(401, "The Firebase Authentication account is unavailable or disabled.");
            string profileKey = StudentManagementService.Key(uid);
            var profile = await database.ReadAsync("users/" + profileKey).ConfigureAwait(false) as JObject;
            var result = FirebaseSignInService.ProfileResult(uid, profile);
            if (requiredRole != null && !string.Equals(result.Role, requiredRole, StringComparison.Ordinal))
                throw new FirebaseException(403, "The authenticated account does not have the required role.");
            var actor = new Actor
            {
                Role = result.Role,
                CompanyId = (string)profile["companyId"] ?? "",
                CompanyName = (string)profile["companyName"] ?? (string)profile["organizationName"] ?? "",
                Department = (string)profile["department"] ?? ""
            };
            if (actor.Role == "companySupervisor" && actor.CompanyId.Length == 0)
                throw new FirebaseException(403, "The company supervisor profile is not linked to a company.");
            return actor;
        }

        private static IEnumerable<Opportunity> ReadRows(JObject rows)
        {
            if (rows == null) return Enumerable.Empty<Opportunity>();
            return rows.Properties().Where(x => x.Value is JObject).Select(x => Map(x.Name, (JObject)x.Value));
        }

        private static Opportunity Map(string key, JObject row)
        {
            DateTime start, deadline, created;
            DateTime.TryParse((string)row["startDate"] ?? (string)row["start"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out start);
            DateTime.TryParse((string)row["deadline"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out deadline);
            DateTime.TryParse((string)row["createdAt"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out created);
            var majors = row["majors"] as JArray;
            if (majors == null && !string.IsNullOrWhiteSpace((string)row["department"])) majors = new JArray((string)row["department"]);
            return new Opportunity
            {
                Id = (string)row["id"] ?? key,
                CompanyId = (string)row["companyId"] ?? "",
                CompanyName = (string)row["companyName"] ?? "",
                Title = (string)row["title"] ?? "",
                Description = (string)row["description"] ?? "",
                Requirements = (string)row["requirements"] ?? "",
                Majors = majors == null ? new List<string>() : majors.Values<string>().Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                Seats = row.Value<int?>("seats") ?? 0,
                AcceptedCount = row.Value<int?>("acceptedCount") ?? 0,
                ApplicantsCount = row.Value<int?>("applicantsCount") ?? 0,
                Location = (string)row["location"] ?? "",
                WorkMode = (string)row["workMode"] ?? "",
                StartDate = start,
                DurationWeeks = row.Value<int?>("durationWeeks") ?? row.Value<int?>("duration") ?? 0,
                RequiredHours = row.Value<int?>("requiredHours") ?? 0,
                Deadline = deadline,
                Status = (string)row["status"] ?? "",
                CreatedAt = created
            };
        }

        private static JObject ToRecord(Opportunity value, string id, string companyId, string companyName)
        {
            return new JObject
            {
                ["id"] = id,
                ["companyId"] = companyId,
                ["companyName"] = companyName,
                ["title"] = value.Title.Trim(),
                ["description"] = value.Description.Trim(),
                ["requirements"] = value.Requirements ?? "",
                ["majors"] = JArray.FromObject(value.Majors ?? new List<string>()),
                ["seats"] = value.Seats,
                ["acceptedCount"] = 0,
                ["applicantsCount"] = 0,
                ["location"] = value.Location.Trim(),
                ["workMode"] = value.WorkMode.Trim(),
                ["startDate"] = value.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["durationWeeks"] = value.DurationWeeks,
                ["requiredHours"] = value.RequiredHours,
                ["deadline"] = value.Deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["status"] = "published",
                ["createdAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
        }

        private static void ApplyEditableFields(JObject current, Opportunity input)
        {
            current["title"] = input.Title.Trim();
            current["description"] = input.Description.Trim();
            current["requirements"] = input.Requirements ?? "";
            if (current["majors"] != null || current["department"] == null)
                current["majors"] = JArray.FromObject(input.Majors ?? new List<string>());
            if (current["department"] != null)
                current["department"] = (input.Majors ?? new List<string>()).FirstOrDefault() ?? "";
            current["location"] = input.Location.Trim();
            current["workMode"] = input.WorkMode.Trim();
            string start = input.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (current["start"] != null && current["startDate"] == null) current["start"] = start;
            else current["startDate"] = start;
            if (current["duration"] != null && current["durationWeeks"] == null) current["duration"] = input.DurationWeeks;
            else current["durationWeeks"] = input.DurationWeeks;
            current["requiredHours"] = input.RequiredHours;
            current["seats"] = input.Seats;
            current["deadline"] = input.Deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            current["updatedAt"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        private static void Validate(Opportunity value, bool creating)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Title) || value.Title.Trim().Length > 180 ||
                string.IsNullOrWhiteSpace(value.Description) || value.Description.Trim().Length > 12000 ||
                string.IsNullOrWhiteSpace(value.Location) || value.Location.Trim().Length > 180 ||
                string.IsNullOrWhiteSpace(value.WorkMode) || value.Seats < 1 || value.DurationWeeks < 1 ||
                value.RequiredHours < 1 || value.Majors == null || value.Majors.Count == 0 ||
                value.Majors.Any(string.IsNullOrWhiteSpace) || value.Deadline == default(DateTime) || value.StartDate == default(DateTime) ||
                value.Deadline.Date < DateTime.UtcNow.Date || value.Deadline.Date > value.StartDate.Date)
                throw new FirebaseException(400, "Complete the opportunity fields with valid majors, dates, seats, duration and training hours.");
            if (!creating && string.IsNullOrWhiteSpace(value.Id))
                throw new FirebaseException(400, "An existing opportunity ID is required for updates.");
        }

        private static bool IsPublished(string status)
        {
            return string.Equals(status, "published", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "open", StringComparison.OrdinalIgnoreCase);
        }

        private static string Key(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 160 || value.IndexOfAny(new[] { '.', '#', '$', '[', ']', '/' }) >= 0)
                throw new FirebaseException(400, "The opportunity identifier is invalid.");
            return value;
        }

        private sealed class Actor
        {
            public string Role { get; set; }
            public string CompanyId { get; set; }
            public string CompanyName { get; set; }
            public string Department { get; set; }
        }
    }
}
