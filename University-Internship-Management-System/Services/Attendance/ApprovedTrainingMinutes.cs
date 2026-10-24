using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UIMS.Attendance
{
    public static class ApprovedTrainingMinutes
    {
        public static long Calculate(JObject records, string assignmentId, string studentId, string companyId, long now)
        {
            if (records == null) return 0;
            var valid = new List<Tuple<long, long, long>>();
            foreach (var p in records.Properties())
            {
                var r = p.Value as JObject; if (r == null) continue;
                Func<string, string> text = key => (string)r[key] ?? "";
                long start, end, minutes;
                if (text("assignmentId") != assignmentId || text("studentId") != studentId || text("companyId") != companyId ||
                    text("hoursApprovalStatus") != "approved" || text("hoursApprovedById").Length == 0 || text("hoursApprovedById") != text("companySupervisorId") ||
                    !(text("status") == "completed" || text("status") == "earlyDeparture") ||
                    !long.TryParse(text("checkInAt"), out start) || !long.TryParse(text("checkOutAt"), out end) || !long.TryParse(text("workedMinutes"), out minutes) ||
                    start <= 0 || end <= start || end > now || minutes <= 0 || minutes > 1440 || minutes != (end - start) / 60000) continue;
                try
                {
                    var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
                    if (TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(start), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != p.Name ||
                        TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(end), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != p.Name) continue;
                }
                catch (ArgumentOutOfRangeException) { continue; }
                valid.Add(Tuple.Create(start, end, minutes));
            }
            // Ambiguous duplicate/overlapping sessions are excluded, never counted twice.
            long total = 0;
            foreach (var row in valid) if (!valid.Any(other => !ReferenceEquals(row, other) && row.Item1 < other.Item2 && other.Item1 < row.Item2)) total = checked(total + row.Item3);
            return total;
        }
    }
}
