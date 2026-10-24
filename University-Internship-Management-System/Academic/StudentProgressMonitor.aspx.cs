using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Academic
{
    public partial class StudentProgressMonitor : Page
    {
        private static string FirebaseUrl { get { return AuthenticatedFirebaseClient.DatabaseUrl; } }
        private string CurrentSupervisorId { get { return User.Identity.Name; } }
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> requestReads = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        protected void Page_Init(object sender, EventArgs e) { Session["FirebasePageBinding"] = true; ViewStateUserKey = Session.SessionID; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var identity = Context.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired)
            {
                Response.Redirect("~/Auth/Login.aspx");
                return;
            }

            if (FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != "academicSupervisor")
                throw new System.Web.HttpException(403, "Academic supervisor access is required.");

            if (!IsPostBack)
            {
                LoadAssignedStudents();
            }
        }

        // Request-local parallel reads: no stale cross-account or cross-request cache.
        private Dictionary<string, JObject> ReadMonitorSources(params string[] paths)
        {
            var reads = paths.Select(path => System.Threading.Tasks.Task.Run(() => {
                using (var client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                    return new KeyValuePair<string, JObject>(path, JsonObject(DownloadSafe(client, path + ".json")));
            })).ToArray();
            System.Threading.Tasks.Task.WhenAll(reads).GetAwaiter().GetResult();
            return reads.ToDictionary(read => read.Result.Key, read => read.Result.Value);
        }

        private static string Value(JToken token, string key) { return (string)token?[key] ?? ""; }

        private void LoadAssignedStudents()
        {
            try
            {
                var sources = ReadMonitorSources("assignments", "users", "placements", "tasks", "uimsAttendance/attendance", "uimsAttendance/trainingAssignments", "supervisorAssignments", "groups");
                var users = sources["users"]; var placements = sources["placements"]; var tasks = sources["tasks"];
                var ids = AcademicAssignmentService.AssignedStudentIds(CurrentSupervisorId, users, sources["groups"], sources["supervisorAssignments"], sources["assignments"], placements);
                var taskGroups = tasks.Properties().Where(x => x.Value is JObject).GroupBy(x => Value(x.Value, "studentId")).ToDictionary(g => g.Key, g => g.ToArray());
                var placementGroups = placements.Properties().Where(x => x.Value is JObject).GroupBy(x => Value(x.Value, "studentId")).ToDictionary(g => g.Key, g => g.ToArray());
                int active = 0, completed = 0; var rows = new List<StudentRow>();
                foreach (string id in ids)
                {
                    var profile = users.Properties().First(p => Value(p.Value, "studentId") == id && FirebaseSignInService.CanonicalRole(Value(p.Value, "role")) == "student" && (bool?)p.Value["isActive"] == true);
                    var ownPlacements = placementGroups.ContainsKey(id) ? placementGroups[id].Where(p => string.IsNullOrEmpty(Value(p.Value, "studentUid")) || Value(p.Value, "studentUid") == profile.Name).ToArray() : new JProperty[0];
                    var placementIds = new HashSet<string>(ownPlacements.Select(p => p.Name));
                    var ownTasks = taskGroups.ContainsKey(id) ? taskGroups[id].Where(t => Value(t.Value, "placementId").Length == 0 || placementIds.Contains(Value(t.Value, "placementId"))).ToArray() : new JProperty[0];
                    int done = ownTasks.Count(t => Value(t.Value, "status").Equals("Completed", StringComparison.OrdinalIgnoreCase)); completed += done;
                    active += ownPlacements.Count(p => new[] { "active", "confirmed" }.Contains(Value(p.Value, "status").ToLowerInvariant()));
                    long minutes = 0; var seen = new HashSet<string>();
                    foreach (var placement in ownPlacements)
                    {
                        string assignmentId = Value(placement.Value, "trainingAssignmentId");
                        if (assignmentId.Length == 0) assignmentId = Value(placement.Value, "attendanceAssignmentId");
                        if (assignmentId.Length == 0)
                        {
                            var links = sources["uimsAttendance/trainingAssignments"].Properties().Where(a => Value(a.Value, "placementId") == placement.Name).ToArray();
                            if (links.Length == 1) assignmentId = links[0].Name;
                        }
                        if (assignmentId.Length == 0 || !seen.Add(assignmentId)) continue;
                        var assignment = sources["uimsAttendance/trainingAssignments"][assignmentId];
                        string company = Value(placement.Value, "companyId");
                        if (Value(assignment, "studentId") != id || Value(assignment, "companyId") != company || (Value(assignment, "placementId").Length > 0 && Value(assignment, "placementId") != placement.Name)) continue;
                        minutes += UIMS.Attendance.ApprovedTrainingMinutes.Calculate(sources["uimsAttendance/attendance"][assignmentId] as JObject ?? new JObject(), assignmentId, id, company, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    }
                    int required = ownPlacements.Select(p => (int?)p.Value["requiredHours"] ?? 0).DefaultIfEmpty(0).Max();
                    rows.Add(new StudentRow { StudentId = id, StudentName = Value(profile.Value, "fullName"), Major = Value(profile.Value, "majorId").Length > 0 ? Value(profile.Value, "majorId") : Value(profile.Value, "department"), RequiredHours = required > 0 ? required : 300, ApprovedHours = (int)(minutes / 60), TaskPercentage = ownTasks.Length > 0 ? (int)Math.Round(100d * done / ownTasks.Length) : 0, HasDelays = ownTasks.Any(t => !Value(t.Value, "status").Equals("Completed", StringComparison.OrdinalIgnoreCase) && DateTimeOffset.TryParse(Value(t.Value, "dueDate"), out var due) && due < DateTimeOffset.UtcNow) });
                }
                rptStudents.DataSource = rows; rptStudents.DataBind();
                lblAssignedStudents.Text = rows.Count.ToString(); lblActiveInternships.Text = active.ToString(); lblCompletedTasks.Text = completed.ToString();
            }
            catch (Exception ex) { Trace.Warn("StudentProgressMonitor", ex.GetType().Name); lblMessage.Text = "Student progress could not be loaded from Firebase. Refresh to retry."; }
        }

        protected void rptStudents_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                string studentId = e.CommandArgument.ToString();
                ReadMonitorSources("users", "groups", "supervisorAssignments", "assignments", "placements", "tasks", "companies", "uimsAttendance/attendance");
                RequireStudentScope(studentId);
                hfCurrentStudentId.Value = studentId;
                lblFeedbackMsg.Text = "";

                try
                {
                    using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                    {
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        string usersJson = DownloadSafe(client, "users.json");
                        var users = ParseJson<Dictionary<string, UserData>>(serializer, usersJson) ?? new Dictionary<string, UserData>();

                        lblDetName.Text = studentId;
                        foreach (var u in users.Values)
                        {
                            if (u != null && u.role == "student" && u.studentId == studentId) { lblDetName.Text = u.fullName; break; }
                        }

                        string placementsJson = DownloadSafe(client, "placements.json");
                        var placements = ParseJson<Dictionary<string, PlacementData>>(serializer, placementsJson) ?? new Dictionary<string, PlacementData>();
                        int reqHours = 300;
                        string compId = "", compSupId = "";

                        foreach (var p in placements.Values)
                        {
                            if (p != null && p.studentId == studentId)
                            {
                                reqHours = p.requiredHours > 0 ? p.requiredHours : 300;
                                compId = p.companyId ?? "";
                                compSupId = p.companySupervisorId ?? "";
                                break;
                            }
                        }
                        lblDetReqHours.Text = reqHours.ToString();

                        lblDetCompany.Text = "Not Assigned";
                        if (!string.IsNullOrEmpty(compId))
                        {
                            string compJson = JsonObject(DownloadSafe(client, "companies.json"))[compId]?.ToString() ?? "null";
                            if (!string.IsNullOrEmpty(compJson) && compJson != "null")
                            {
                                var compData = serializer.Deserialize<Dictionary<string, object>>(compJson);
                                if (compData != null && compData.ContainsKey("name")) lblDetCompany.Text = compData["name"].ToString();
                            }
                        }

                        lblDetCompSup.Text = "Not Assigned";
                        if (!string.IsNullOrEmpty(compSupId))
                        {
                            foreach (var kvp in users)
                            {
                                if (kvp.Key == compSupId || (kvp.Value != null && kvp.Value.userId == compSupId))
                                {
                                    lblDetCompSup.Text = kvp.Value.fullName; break;
                                }
                            }
                        }

                        string attendanceJson = DownloadSafe(client, "uimsAttendance/attendance.json");
                        var attendance = ParseJson<Dictionary<string, Dictionary<string, AttendanceRecord>>>(serializer, attendanceJson) ?? new Dictionary<string, Dictionary<string, AttendanceRecord>>();
                        int totalApprovedMinutes = 0;
                        foreach (var pAtt in attendance.Values)
                        {
                            if (pAtt == null) continue;
                            foreach (var day in pAtt.Values)
                            {
                                if (day != null && day.studentId == studentId && day.hoursApprovalStatus == "approved") totalApprovedMinutes += day.workedMinutes;
                            }
                        }
                        lblDetAppHours.Text = (totalApprovedMinutes / 60).ToString();

                        string tasksJson = DownloadSafe(client, "tasks.json");
                        var tasks = ParseJson<Dictionary<string, TaskData>>(serializer, tasksJson) ?? new Dictionary<string, TaskData>();
                        List<TaskRow> studentTasks = new List<TaskRow>();
                        foreach (var t in tasks.Values)
                        {
                            if (t != null && t.studentId == studentId)
                            {
                                studentTasks.Add(new TaskRow
                                {
                                    Title = t.title ?? "Untitled",
                                    Status = t.status ?? "Pending",
                                    Score = t.score != null ? t.score.ToString() + " / " + (t.maximumScore ?? 100).ToString() : "Not Graded",
                                    SubmissionLink = UIMS.Tasks.TaskSubmissionHistory.Read(t.submissions).Records.Select(r => (string)r["link"]).LastOrDefault() ?? ""
                                });
                            }
                        }

                        if (studentTasks.Count > 0) { rptTasks.DataSource = studentTasks; rptTasks.DataBind(); rptTasks.Visible = true; lblNoTasks.Visible = false; }
                        else { rptTasks.Visible = false; lblNoTasks.Visible = true; }

                        string feedbackJson = DownloadSafe(client, $"academicFeedback/{studentId}.json");
                        txtFeedback.Text = (!string.IsNullOrEmpty(feedbackJson) && feedbackJson != "null") ? feedbackJson.Trim('"') : "";
                    }
                }
                catch (Exception ex) { Trace.Warn("StudentProgressDetails", ex.GetType().Name); lblFeedbackMsg.Text = "Some student details could not be loaded. Close and retry."; }

                ScriptManager.RegisterStartupScript(this, GetType(), "OpenModal", "document.getElementById('studentDetails').showModal();", true);
            }
        }

        protected void btnSaveFeedback_Click(object sender, EventArgs e)
        {
            string studentId = hfCurrentStudentId.Value;
            RequireStudentScope(studentId);
            if (string.IsNullOrEmpty(studentId)) return;

            try
            {
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    client.UploadString(FirebaseUrl + $"academicFeedback/{studentId}.json", "PUT", new JavaScriptSerializer().Serialize(txtFeedback.Text.Trim()));
                }
                lblFeedbackMsg.ForeColor = System.Drawing.Color.Green; lblFeedbackMsg.Text = "Feedback saved.";
                ScriptManager.RegisterStartupScript(this, GetType(), "KeepOpen", "document.getElementById('studentDetails').showModal();", true);
            }
            catch (Exception ex)
            {
                lblFeedbackMsg.ForeColor = System.Drawing.Color.Red; lblFeedbackMsg.Text = "Error: " + ex.Message;
                ScriptManager.RegisterStartupScript(this, GetType(), "KeepOpen", "document.getElementById('studentDetails').showModal();", true);
            }
        }

        private void RequireStudentScope(string studentId)
        {
            University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(studentId);
            {
                var sources = ReadMonitorSources("users", "groups", "supervisorAssignments", "assignments", "placements");
                var ids = AcademicAssignmentService.AssignedStudentIds(CurrentSupervisorId,
                    sources["users"], sources["groups"], sources["supervisorAssignments"], sources["assignments"], sources["placements"]);
                if (!ids.Contains(studentId)) throw new System.Web.HttpException(403, "Not assigned.");
            }
        }

        private static JObject JsonObject(string json) { return string.IsNullOrEmpty(json) || json == "null" ? new JObject() : JObject.Parse(json); }

        private string DownloadSafe(WebClient client, string path) { return requestReads.GetOrAdd(path, key => client.DownloadString(FirebaseUrl + key)); }
        private T ParseJson<T>(JavaScriptSerializer serializer, string json) { if (string.IsNullOrEmpty(json) || json == "null") return default(T); return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json); }

        public class AssignmentData { public string academicSupervisorId { get; set; } public string studentId { get; set; } }
        public class UserData { public string fullName { get; set; } public string role { get; set; } public string studentId { get; set; } public string department { get; set; } public string majorId { get; set; } public string userId { get; set; } }
        public class PlacementData { public string studentId { get; set; } public string status { get; set; } public int requiredHours { get; set; } public string companyId { get; set; } public string companySupervisorId { get; set; } }
        public class TaskData { public string studentId { get; set; } public string status { get; set; } public string title { get; set; } public string dueDate { get; set; } public decimal? score { get; set; } public decimal? maximumScore { get; set; } public JToken submissions { get; set; } }
        public class SubmissionData { public string link { get; set; } }
        public class AttendanceRecord { public string studentId { get; set; } public string hoursApprovalStatus { get; set; } public int workedMinutes { get; set; } }
        public class StudentRow { public string StudentId { get; set; } public string StudentName { get; set; } public string Major { get; set; } public int ApprovedHours { get; set; } public int RequiredHours { get; set; } public int TaskPercentage { get; set; } public bool HasDelays { get; set; } }
        public class TaskRow { public string Title { get; set; } public string Status { get; set; } public string Score { get; set; } public string SubmissionLink { get; set; } }
    }
}
