using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    // In-app notifications at userNotifications/{uid}/{id} = { title, message, link, kind, createdAt, read }.
    // Sending is best effort and runs in the background, so a notification can never fail the action that caused it.
    public static class Notify
    {
        private static string T(JToken value, string key) { return (string)value?[key] ?? ""; }
        private static string Role(JProperty user) { return FirebaseSignInService.CanonicalRole(T(user.Value, "role")); }
        private static bool Active(JProperty user) { return (bool?)user.Value["isActive"] != false; }

        public static Func<JProperty, bool> Student(string studentIdOrUid)
        { return u => !string.IsNullOrEmpty(studentIdOrUid) && Role(u) == "student" && (u.Name == studentIdOrUid || T(u.Value, "studentId") == studentIdOrUid); }
        public static Func<JProperty, bool> AcademicSupervisor(string id)
        { return u => !string.IsNullOrEmpty(id) && Role(u) == "academicSupervisor" && (u.Name == id || T(u.Value, "academicSupervisorId") == id || T(u.Value, "staffId") == id); }
        public static Func<JProperty, bool> CompanySupervisors(string companyId)
        { return u => !string.IsNullOrEmpty(companyId) && Role(u) == "companySupervisor" && T(u.Value, "companyId") == companyId; }
        public static Func<JProperty, bool> User(string uid)
        { return u => !string.IsNullOrEmpty(uid) && u.Name == uid; }
        public static Func<JProperty, bool> WithRole(string role)
        { return u => Role(u) == role; }

        // link is relative to the site root, for example "Student/MyTasks.aspx".
        public static void Send(Func<JProperty, bool> recipients, string title, string message, string link, string kind, string exceptUid = null)
        {
            if (recipients == null || string.IsNullOrWhiteSpace(title)) return;
            Func<CancellationToken, Task> work = async cancellation =>
            {
                try
                {
                    var db = new StudentFirebaseGateway();
                    var users = await db.ReadAsync("users").ConfigureAwait(false) as JObject ?? new JObject();
                    string now = DateTimeOffset.UtcNow.ToString("o");
                    foreach (var user in users.Properties().Where(u => Active(u) && u.Name != exceptUid && recipients(u)).ToList())
                    {
                        string id = "N" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString("N").Substring(0, 8);
                        await db.WriteAsync("userNotifications/" + StudentManagementService.Key(user.Name) + "/" + id, new JObject
                        {
                            ["title"] = Limit(title, 120), ["message"] = Limit(message, 400), ["link"] = link ?? "", ["kind"] = kind ?? "info",
                            ["createdAt"] = now, ["read"] = false
                        }).ConfigureAwait(false);
                    }
                }
                catch (Exception error) { System.Diagnostics.Trace.TraceWarning("Notification not sent: " + error.GetType().Name); }
            };
            try { HostingEnvironment.QueueBackgroundWorkItem(work); }
            catch (InvalidOperationException) { Task.Run(() => work(CancellationToken.None)); }
        }

        private static string Limit(string value, int length) { value = (value ?? "").Trim(); return value.Length <= length ? value : value.Substring(0, length - 1) + "…"; }

        public static async Task<JObject> ListAsync(IStudentDatabase db, string uid)
        {
            var all = await db.ReadAsync("userNotifications/" + StudentManagementService.Key(uid)).ConfigureAwait(false) as JObject ?? new JObject();
            var items = all.Properties().OrderByDescending(p => p.Name, StringComparer.Ordinal).Take(40)
                .Select(p => new JObject { ["id"] = p.Name, ["title"] = T(p.Value, "title"), ["message"] = T(p.Value, "message"), ["link"] = T(p.Value, "link"), ["kind"] = T(p.Value, "kind"), ["createdAt"] = T(p.Value, "createdAt"), ["read"] = (bool?)p.Value["read"] == true });
            return new JObject { ["items"] = new JArray(items), ["unread"] = all.Properties().Count(p => (bool?)p.Value["read"] != true) };
        }

        public static async Task MarkReadAsync(IStudentDatabase db, string uid, string id)
        {
            string root = "userNotifications/" + StudentManagementService.Key(uid);
            if (!string.IsNullOrEmpty(id)) { await db.PatchAsync(root + "/" + StudentManagementService.Key(id), new { read = true }).ConfigureAwait(false); return; }
            var all = await db.ReadAsync(root).ConfigureAwait(false) as JObject ?? new JObject();
            var update = new JObject();
            foreach (var p in all.Properties().Where(p => (bool?)p.Value["read"] != true)) update[p.Name + "/read"] = true;
            if (update.Count > 0) await db.PatchAsync(root, update).ConfigureAwait(false);
        }
    }
}
