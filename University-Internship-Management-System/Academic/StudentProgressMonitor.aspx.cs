using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Academic
{
    public partial class StudentProgressMonitor : Page
    {
        private const string FirebaseUrl = "https://just-project-4af15-default-rtdb.firebaseio.com/";

        // الاعتماد على Session["FullName"] أو الـ ID التجريبي كقيمة افتراضية متوافقة مع الداشبورد
        private string CurrentSupervisorId
        {
            get
            {
                // إذا كان هناك ID مخزن مسبقاً في الجلسة
                if (Session["UserId"] != null) return Session["UserId"].ToString();

                // قيمة افتراضية متوافقة مع الـ Demo الحالي في النظام
                return "AS-DEMO-001";
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Role Guard المتبع في المشروع (تم تركيبه ليتوافق مع أسلوبهم)
            // if (Session["Role"] as string != "Academic" && Session["UserRole"] as string != "academicSupervisor")
            // {
            //     Response.Redirect("~/Auth/Login.aspx");
            //     return;
            // }

            if (!IsPostBack)
            {
                LoadAssignedStudents();
            }
        }

        private void LoadAssignedStudents()
        {
            try
            {
                using (WebClient client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string assignmentsJson = DownloadSafe(client, "supervisorAssignments.json");
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
                        if (a != null && a.academicSupervisorId == CurrentSupervisorId && !myStudentIds.Contains(a.studentId))
                        {
                            myStudentIds.Add(a.studentId);
                        }
                    }

                    int activeInternships = 0;
                    int completedTasks = 0;
                    List<StudentRow> tableRows = new List<StudentRow>();

                    foreach (string sId in myStudentIds)
                    {
                        StudentRow row = new StudentRow { StudentId = sId, StudentName = "Unknown", Major = "N/A", RequiredHours = 300, HasDelays = false };

                        foreach (var u in users.Values)
                        {
                            if (u != null && u.role == "student" && u.studentId == sId)
                            {
                                row.StudentName = u.fullName;
                                row.Major = u.majorId ?? u.department ?? "N/A";
                                break;
                            }
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
                        foreach (var placementAttendance in attendance.Values)
                        {
                            if (placementAttendance == null) continue;
                            foreach (var day in placementAttendance.Values)
                            {
                                if (day != null && day.studentId == sId && day.hoursApprovalStatus == "approved")
                                {
                                    totalApprovedMinutes += day.workedMinutes;
                                }
                            }
                        }
                        row.ApprovedHours = totalApprovedMinutes / 60;

                        int totalTasks = 0;
                        int compTasks = 0;
                        foreach (var t in tasks.Values)
                        {
                            if (t != null && t.studentId == sId)
                            {
                                totalTasks++;
                                if (t.status == "Completed")
                                {
                                    compTasks++;
                                    completedTasks++;
                                }
                                else if (!string.IsNullOrEmpty(t.dueDate))
                                {
                                    DateTime dDate;
                                    if (DateTime.TryParse(t.dueDate, out dDate) && dDate < DateTime.Now)
                                    {
                                        row.HasDelays = true;
                                    }
                                }
                            }
                        }
                        if (totalTasks > 0) row.TaskPercentage = (int)Math.Round((double)compTasks / totalTasks * 100);

                        if (isActive && row.ApprovedHours == 0 && row.TaskPercentage == 0) row.HasDelays = true;

                        tableRows.Add(row);
                    }

                    rptStudents.DataSource = tableRows;
                    rptStudents.DataBind();

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
                hfCurrentStudentId.Value = studentId;
                lblFeedbackMsg.Text = "";

                try
                {
                    using (WebClient client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
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
                        foreach (var p in placements.Values)
                        {
                            if (p != null && p.studentId == studentId)
                            {
                                reqHours = p.requiredHours > 0 ? p.requiredHours : 300;
                                break;
                            }
                        }
                        lblDetReqHours.Text = reqHours.ToString();

                        string attendanceJson = DownloadSafe(client, "uimsAttendance/attendance.json");
                        var attendance = ParseJson<Dictionary<string, Dictionary<string, AttendanceRecord>>>(serializer, attendanceJson) ?? new Dictionary<string, Dictionary<string, AttendanceRecord>>();
                        int totalApprovedMinutes = 0;
                        foreach (var placementAttendance in attendance.Values)
                        {
                            if (placementAttendance == null) continue;
                            foreach (var day in placementAttendance.Values)
                            {
                                if (day != null && day.studentId == studentId && day.hoursApprovalStatus == "approved")
                                {
                                    totalApprovedMinutes += day.workedMinutes;
                                }
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
                                string subLink = "";
                                if (t.submissions != null && t.submissions.Count > 0)
                                {
                                    subLink = t.submissions[0].link ?? "";
                                }

                                studentTasks.Add(new TaskRow
                                {
                                    Title = t.title ?? "Untitled",
                                    Status = t.status ?? "Pending",
                                    Score = t.score != null ? t.score.ToString() + " / " + (t.maximumScore ?? 100).ToString() : "Not Graded",
                                    SubmissionLink = subLink
                                });
                            }
                        }

                        if (studentTasks.Count > 0)
                        {
                            rptTasks.DataSource = studentTasks;
                            rptTasks.DataBind();
                            rptTasks.Visible = true;
                            lblNoTasks.Visible = false;
                        }
                        else
                        {
                            rptTasks.Visible = false;
                            lblNoTasks.Visible = true;
                        }

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
            string feedback = txtFeedback.Text.Trim();
            if (string.IsNullOrEmpty(studentId)) return;

            try
            {
                using (WebClient client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    client.UploadString(FirebaseUrl + $"academicFeedback/{studentId}.json", "PUT", new JavaScriptSerializer().Serialize(feedback));
                }
                lblFeedbackMsg.ForeColor = System.Drawing.Color.Green;
                lblFeedbackMsg.Text = "Feedback saved successfully.";
                ScriptManager.RegisterStartupScript(this, GetType(), "KeepOpen", "document.getElementById('studentDetails').showModal();", true);
            }
            catch (Exception ex)
            {
                lblFeedbackMsg.ForeColor = System.Drawing.Color.Red;
                lblFeedbackMsg.Text = "Error saving feedback: " + ex.Message;
                ScriptManager.RegisterStartupScript(this, GetType(), "KeepOpen", "document.getElementById('studentDetails').showModal();", true);
            }
        }

        private string DownloadSafe(WebClient client, string path) { try { return client.DownloadString(FirebaseUrl + path); } catch { return ""; } }
        private T ParseJson<T>(JavaScriptSerializer serializer, string json) { if (string.IsNullOrEmpty(json) || json == "null") return default(T); return serializer.Deserialize<T>(json); }

        public class AssignmentData { public string academicSupervisorId { get; set; } public string studentId { get; set; } }
        public class UserData { public string fullName { get; set; } public string role { get; set; } public string studentId { get; set; } public string department { get; set; } public string majorId { get; set; } }
        public class PlacementData { public string studentId { get; set; } public string status { get; set; } public int requiredHours { get; set; } }

        public class TaskData
        {
            public string studentId { get; set; }
            public string status { get; set; }
            public string title { get; set; }
            public string dueDate { get; set; }
            public int? score { get; set; }
            public int? maximumScore { get; set; }
            public List<SubmissionData> submissions { get; set; }
        }
        public class SubmissionData { public string link { get; set; } }

        public class AttendanceRecord { public string studentId { get; set; } public string hoursApprovalStatus { get; set; } public int workedMinutes { get; set; } }
        public class StudentRow { public string StudentId { get; set; } public string StudentName { get; set; } public string Major { get; set; } public int ApprovedHours { get; set; } public int RequiredHours { get; set; } public int TaskPercentage { get; set; } public bool HasDelays { get; set; } }

        public class TaskRow { public string Title { get; set; } public string Status { get; set; } public string Score { get; set; } public string SubmissionLink { get; set; } }
    }
}