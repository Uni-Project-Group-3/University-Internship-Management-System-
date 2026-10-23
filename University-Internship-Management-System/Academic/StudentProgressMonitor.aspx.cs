using System;
using System.Collections.Generic;
using System.Net;
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

        protected void Page_Init(object sender, EventArgs e) { Session["FirebasePageBinding"] = true; ViewStateUserKey = Session.SessionID; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var identity = Context.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                Response.Redirect("~/Auth/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadAssignedStudents();
            }
        }

        private void LoadAssignedStudents()
        {
            try
            {
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string assignmentsJson = DownloadSafe(client, "assignments.json");
                    string usersJson = DownloadSafe(client, "users.json");
                    string placementsJson = DownloadSafe(client, "placements.json");
                    string tasksJson = DownloadSafe(client, "tasks.json");
                    string attendanceJson = DownloadSafe(client, "uimsAttendance/attendance.json");

                    var allAssignments = ParseJson<Dictionary<string, AssignmentData>>(serializer, assignmentsJson) ?? new Dictionary<string, AssignmentData>();
                    var users = ParseJson<Dictionary<string, UserData>>(serializer, usersJson) ?? new Dictionary<string, UserData>();
                    var placements = ParseJson<Dictionary<string, PlacementData>>(serializer, placementsJson) ?? new Dictionary<string, PlacementData>();
                    var tasks = ParseJson<Dictionary<string, TaskData>>(serializer, tasksJson) ?? new Dictionary<string, TaskData>();
                    var attendance = ParseJson<Dictionary<string, Dictionary<string, AttendanceRecord>>>(serializer, attendanceJson) ?? new Dictionary<string, Dictionary<string, AttendanceRecord>>();

                    List<string> myStudentIds = new List<string>();
                    foreach (var a in allAssignments.Values)
                    {
                        if (a != null && a.academicSupervisorId == CurrentSupervisorId && !myStudentIds.Contains(a.studentId)) myStudentIds.Add(a.studentId);
                    }

                    int activeInternships = 0, completedTasks = 0;
                    List<StudentRow> tableRows = new List<StudentRow>();

                    foreach (string sId in myStudentIds)
                    {
                        StudentRow row = new StudentRow { StudentId = sId, StudentName = "Unknown", Major = "N/A", RequiredHours = 300, HasDelays = false };
                        foreach (var u in users.Values)
                        {
                            if (u != null && u.role == "student" && u.studentId == sId) { row.StudentName = u.fullName; row.Major = u.majorId ?? u.department ?? "N/A"; break; }
                        }

                        bool isActive = false;
                        foreach (var p in placements.Values)
                        {
                            if (p != null && p.studentId == sId)
                            {
                                row.RequiredHours = p.requiredHours > 0 ? p.requiredHours : 300;
                                if (p.status == "active") { activeInternships++; isActive = true; }
                            }
                        }

                        int totalApprovedMinutes = 0;
                        foreach (var pAtt in attendance.Values)
                        {
                            if (pAtt == null) continue;
                            foreach (var day in pAtt.Values)
                            {
                                if (day != null && day.studentId == sId && day.hoursApprovalStatus == "approved") totalApprovedMinutes += day.workedMinutes;
                            }
                        }
                        row.ApprovedHours = totalApprovedMinutes / 60;

                        int totalTasks = 0, compTasks = 0;
                        foreach (var t in tasks.Values)
                        {
                            if (t != null && t.studentId == sId)
                            {
                                totalTasks++;
                                if (t.status == "Completed") { compTasks++; completedTasks++; }
                                else if (!string.IsNullOrEmpty(t.dueDate) && DateTime.TryParse(t.dueDate, out DateTime dDate) && dDate < DateTime.Now) row.HasDelays = true;
                            }
                        }

                        if (totalTasks > 0) row.TaskPercentage = (int)Math.Round((double)compTasks / totalTasks * 100);
                        if (isActive && row.ApprovedHours == 0 && row.TaskPercentage == 0) row.HasDelays = true;
                        tableRows.Add(row);
                    }

                    rptStudents.DataSource = tableRows; rptStudents.DataBind();
                    lblAssignedStudents.Text = myStudentIds.Count.ToString();
                    lblActiveInternships.Text = activeInternships.ToString();
                    lblCompletedTasks.Text = completedTasks.ToString();
                }
            }
            catch (Exception ex) { lblMessage.Text = "Error loading data: " + ex.Message; }
        }

        protected void rptStudents_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                string studentId = e.CommandArgument.ToString();
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
                            string compJson = DownloadSafe(client, $"companies/{compId}.json");
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
                                    SubmissionLink = (t.submissions != null && t.submissions.Count > 0) ? t.submissions[0].link : ""
                                });
                            }
                        }

                        if (studentTasks.Count > 0) { rptTasks.DataSource = studentTasks; rptTasks.DataBind(); rptTasks.Visible = true; lblNoTasks.Visible = false; }
                        else { rptTasks.Visible = false; lblNoTasks.Visible = true; }

                        string feedbackJson = DownloadSafe(client, $"academicFeedback/{studentId}.json");
                        txtFeedback.Text = (!string.IsNullOrEmpty(feedbackJson) && feedbackJson != "null") ? feedbackJson.Trim('"') : "";
                    }
                }
                catch { }

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
            using (var client = new AuthenticatedFirebaseClient())
            {
                var assignments = Newtonsoft.Json.Linq.JObject.Parse(client.DownloadString(FirebaseUrl + "assignments.json"));
                if (!System.Linq.Enumerable.Any(assignments.Properties(), p => (string)p.Value["studentId"] == studentId && (string)p.Value["academicSupervisorId"] == CurrentSupervisorId && (bool?)p.Value["active"] != false))
                    throw new System.Web.HttpException(403, "Not assigned.");
            }
        }

        private string DownloadSafe(WebClient client, string path) { return client.DownloadString(FirebaseUrl + path); }
        private T ParseJson<T>(JavaScriptSerializer serializer, string json) { if (string.IsNullOrEmpty(json) || json == "null") return default(T); return serializer.Deserialize<T>(json); }

        public class AssignmentData { public string academicSupervisorId { get; set; } public string studentId { get; set; } }
        public class UserData { public string fullName { get; set; } public string role { get; set; } public string studentId { get; set; } public string department { get; set; } public string majorId { get; set; } public string userId { get; set; } }
        public class PlacementData { public string studentId { get; set; } public string status { get; set; } public int requiredHours { get; set; } public string companyId { get; set; } public string companySupervisorId { get; set; } }
        public class TaskData { public string studentId { get; set; } public string status { get; set; } public string title { get; set; } public string dueDate { get; set; } public int? score { get; set; } public int? maximumScore { get; set; } public List<SubmissionData> submissions { get; set; } }
        public class SubmissionData { public string link { get; set; } }
        public class AttendanceRecord { public string studentId { get; set; } public string hoursApprovalStatus { get; set; } public int workedMinutes { get; set; } }
        public class StudentRow { public string StudentId { get; set; } public string StudentName { get; set; } public string Major { get; set; } public int ApprovedHours { get; set; } public int RequiredHours { get; set; } public int TaskPercentage { get; set; } public bool HasDelays { get; set; } }
        public class TaskRow { public string Title { get; set; } public string Status { get; set; } public string Score { get; set; } public string SubmissionLink { get; set; } }
    }
}