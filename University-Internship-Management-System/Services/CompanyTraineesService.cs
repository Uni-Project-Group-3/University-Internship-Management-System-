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
    }
}
