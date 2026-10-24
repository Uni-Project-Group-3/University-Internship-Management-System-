using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UIMS.Tasks
{
    public sealed partial class TaskService
    {
        public async Task<JObject> LoadTraineesAsync(CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            if (actor.CompanyId.Length == 0) throw new UIMS.Services.FirebaseException(403, "A verified company mapping is required.");
            var placementRead = ReadAt("placements", "", cancellation); var userRead = ReadAt("users", "", cancellation);
            var opportunityRead = ReadAt("opportunities", "", cancellation); var taskRead = ReadData("", cancellation);
            await Task.WhenAll(placementRead, userRead, opportunityRead, taskRead).ConfigureAwait(false);
            var placements = (await placementRead.ConfigureAwait(false)).Data; var users = (await userRead.ConfigureAwait(false)).Data;
            var opportunities = (await opportunityRead.ConfigureAwait(false)).Data; var tasks = await taskRead.ConfigureAwait(false);
            var result = new JArray(); var linked = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in Rows(placements))
            {
                var placement = item.Value as JObject;
                if (placement == null || !OwnCompanyPlacement(placement)) continue;
                string status = Text(placement, "status").ToLowerInvariant();
                if (!new[] { "active", "confirmed", "completed" }.Contains(status)) continue;
                string applicationId = Text(placement, "applicationId"); if (applicationId.Length > 0) linked.Add(applicationId);
                string studentId = Text(placement, "studentId");
                var profile = FindStudentProfile(users, studentId, Text(placement, "studentUid")); if (profile == null) continue;
                string opportunityId = Text(placement, "opportunityId"); var opportunity = opportunities[opportunityId] as JObject;
                if (opportunity != null && Text(opportunity, "companyId") != actor.CompanyId) continue;
                var ownTasks = Rows(tasks).Where(p => Text(p.Value, "placementId") == item.Name && Text(p.Value, "assignedBy") == actor.CompanySupervisorId).ToList();
                bool completed = status == "completed" || Text(placement, "completionStatus").Equals("Completed", StringComparison.OrdinalIgnoreCase);
                result.Add(new JObject { ["id"] = item.Name, ["placementId"] = item.Name, ["studentId"] = studentId,
                    ["name"] = FirstText(profile, "fullName", "displayName", "name", "studentId"), ["department"] = FirstText(profile, "department", "majorId"),
                    ["opportunityId"] = opportunityId, ["internship"] = FirstText(opportunity, "title", "name"),
                    ["startDate"] = Text(placement, "startDate"), ["endDate"] = Text(placement, "endDate"),
                    ["status"] = completed ? "completed" : "training", ["taskCount"] = ownTasks.Count,
                    ["completedTasks"] = ownTasks.Count(p => Text(p.Value, "status").Equals("Completed", StringComparison.OrdinalIgnoreCase)),
                    ["requiredHours"] = placement["requiredHours"]?.DeepClone(), ["academicSupervisorId"] = Text(placement, "academicSupervisorId") });
            }
            return new JObject { ["trainees"] = result, ["companyId"] = actor.CompanyId,
                ["statistics"] = new JObject { ["total"] = result.Count, ["training"] = result.Count(p => (string)p["status"] == "training"), ["completed"] = result.Count(p => (string)p["status"] == "completed"), ["assignedTasks"] = result.Sum(p => (int)p["taskCount"]) },
                ["serverNow"] = DateTimeOffset.UtcNow.ToString("o") };
        }
        private bool OwnCompanyPlacement(JToken placement)
        { return Text(placement, "companySupervisorId") == actor.CompanySupervisorId && actor.CompanyId.Length > 0 && Text(placement, "companyId") == actor.CompanyId; }

        public async Task<JObject> LoadTraineeProgressAsync(string placementId, CancellationToken cancellation)
        {
            Ensure(false, "companySupervisor");
            if (string.IsNullOrWhiteSpace(placementId) || placementId.Any(c => ".#$[]/\\".Contains(c))) throw new UIMS.Services.FirebaseException(400, "Invalid placement.");
            var placement = (await ReadAt("placements", placementId, cancellation).ConfigureAwait(false)).Data;
            if (!OwnCompanyPlacement(placement)) throw new UIMS.Services.FirebaseException(403, "This placement is not assigned to your company supervisor account.");
            if (!new[] { "active", "confirmed", "completed" }.Contains(Text(placement, "status").ToLowerInvariant())) throw new UIMS.Services.FirebaseException(409, "This placement is not confirmed or active.");
            var studentId = Text(placement, "studentId");
            var tasks = await ReadData("", cancellation).ConfigureAwait(false);
            var rows = new JArray(Rows(tasks).Where(p => Text(p.Value, "placementId") == placementId && Text(p.Value, "studentId") == studentId && Text(p.Value, "assignedBy") == actor.CompanySupervisorId).Select(p => p.Value.DeepClone()));
            long? minutes = null; string blocker = "";
            string assignmentId = FirstText(placement, "trainingAssignmentId", "attendanceAssignmentId");
            var mappings = (await ReadAt("uimsAttendance", "trainingAssignments", cancellation).ConfigureAwait(false)).Data;
            if (assignmentId.Length == 0)
            {
                var matches = Rows(mappings).Where(p => Text(p.Value, "placementId") == placementId).Select(p => p.Name).ToArray();
                if (matches.Length == 1) assignmentId = matches[0];
            }
            var assignment = assignmentId.Length > 0 ? mappings[assignmentId] : null;
            if (assignment == null) blocker = "No verified attendance assignment is linked to this placement yet.";
            else if (!new[] { "active", "completed" }.Contains(Text(assignment, "status").ToLowerInvariant())) blocker = "The linked attendance assignment is not active or completed.";
            else if (Text(assignment, "studentId") != studentId || Text(assignment, "companyId") != actor.CompanyId || (Text(assignment, "placementId").Length > 0 && Text(assignment, "placementId") != placementId)) blocker = "The attendance assignment does not match this student and company.";
            else
            {
                var allPlacements = (await ReadAt("placements", "", cancellation).ConfigureAwait(false)).Data;
                if (Rows(allPlacements).Any(p => p.Name != placementId && (Text(p.Value, "trainingAssignmentId") == assignmentId || Text(p.Value, "attendanceAssignmentId") == assignmentId))) blocker = "The attendance assignment is linked to multiple placements.";
                else
                {
                    var attendance = (await ReadAt("uimsAttendance", "attendance/" + assignmentId, cancellation).ConfigureAwait(false)).Data as JObject ?? new JObject();
                    minutes = UIMS.Attendance.ApprovedTrainingMinutes.Calculate(attendance, assignmentId, studentId, actor.CompanyId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
            }
            return new JObject { ["tasks"] = rows, ["approvedTrainingMinutes"] = minutes, ["eligibilityBlocker"] = blocker, ["requiredTrainingMinutes"] = 18000 };
        }
    }
}
