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
        private const string FirebaseUrl = "https://unviersity-data-default-rtdb.firebaseio.com/";

        // حساب المشرف الأكاديمي الحالي (للتجربة)
        private string currentSupervisorId = "AS001";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadAssignedStudents();
            }
        }

        private void LoadAssignedStudents()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string assignJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");
                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                    string hoursJson = client.DownloadString(FirebaseUrl + "trainingHours.json");
                    string tasksJson = client.DownloadString(FirebaseUrl + "tasks.json");

                    var assignments = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(assignJson ?? "{}");
                    var users = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(usersJson ?? "{}");
                    var hours = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(hoursJson ?? "{}");
                    var tasks = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(tasksJson ?? "{}");

                    List<StudentRow> studentsList = new List<StudentRow>();
                    int activeCount = 0;
                    int completedTasksCount = 0;

                    // 1. إيجاد الطلاب التابعين لهذا المشرف فقط
                    foreach (var assign in assignments)
                    {
                        if (assign.Value != null &&
                            assign.Value.ContainsKey("academicSupervisorId") &&
                            assign.Value["academicSupervisorId"] == currentSupervisorId)
                        {
                            string sId = assign.Value.ContainsKey("studentId") ? assign.Value["studentId"] : "";

                            if (sId != "" && users.ContainsKey(sId))
                            {
                                activeCount++;
                                var studentData = users[sId];
                                StudentRow row = new StudentRow();
                                row.StudentId = sId;
                                row.StudentName = studentData.ContainsKey("fullName") ? studentData["fullName"] : "Unknown";
                                row.Major = studentData.ContainsKey("major") ? studentData["major"] : "Not Specified";

                                int reqHours = 300;
                                if (studentData.ContainsKey("requiredTrainingHours")) int.TryParse(studentData["requiredTrainingHours"], out reqHours);
                                row.RequiredHours = reqHours;

                                // حساب الساعات المعتمدة لهذا الطالب
                                int appHours = 0;
                                foreach (var h in hours)
                                {
                                    if (h.Value != null && h.Value.ContainsKey("studentId") && h.Value["studentId"] == sId)
                                    {
                                        if (h.Value.ContainsKey("status") && h.Value["status"] == "Approved")
                                        {
                                            int hr = 0;
                                            if (h.Value.ContainsKey("hours")) int.TryParse(h.Value["hours"], out hr);
                                            appHours += hr;
                                        }
                                    }
                                }
                                row.ApprovedHours = appHours;

                                // حساب مهام الطالب
                                int totalT = 0;
                                int compT = 0;
                                foreach (var t in tasks)
                                {
                                    if (t.Value != null && t.Value.ContainsKey("studentId") && t.Value["studentId"] == sId)
                                    {
                                        totalT++;
                                        if (t.Value.ContainsKey("status") && (t.Value["status"] == "Evaluated" || t.Value["status"] == "Completed"))
                                        {
                                            compT++;
                                            completedTasksCount++;
                                        }
                                    }
                                }

                                row.TaskPercentage = totalT > 0 ? (compT * 100) / totalT : 0;
                                studentsList.Add(row);
                            }
                        }
                    }

                    // ربط البيانات بالشاشة
                    rptStudents.DataSource = studentsList;
                    rptStudents.DataBind();

                    lblAssignedStudents.Text = studentsList.Count.ToString();
                    lblActiveInternships.Text = activeCount.ToString();
                    lblCompletedTasks.Text = completedTasksCount.ToString();
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading data: " + ex.Message;
            }
        }

        protected void rptStudents_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                string studentId = e.CommandArgument.ToString();
                hfCurrentStudentId.Value = studentId;

                try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = System.Text.Encoding.UTF8;
                        JavaScriptSerializer serializer = new JavaScriptSerializer();

                        // جلب اسم الطالب
                        string usersJson = client.DownloadString(FirebaseUrl + "users/" + studentId + ".json");
                        var studentData = serializer.Deserialize<Dictionary<string, string>>(usersJson ?? "{}");
                        lblDetName.Text = studentData != null && studentData.ContainsKey("fullName") ? studentData["fullName"] : studentId;

                        // تصفير الرسالة وحقل النص القديم
                        txtFeedback.Text = "";
                        lblFeedbackMsg.Text = "";

                        // جلب الملاحظات القديمة (إن وجدت)
                        string notesJson = client.DownloadString(FirebaseUrl + "academicNotes.json");
                        var notes = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(notesJson ?? "{}");
                        foreach (var n in notes)
                        {
                            if (n.Value != null && n.Value.ContainsKey("studentId") && n.Value["studentId"] == studentId)
                            {
                                txtFeedback.Text = n.Value.ContainsKey("note") ? n.Value["note"] : "";
                            }
                        }
                    }

                    // أمر لفتح النافذة المنبثقة
                    ScriptManager.RegisterStartupScript(this, GetType(), "ShowModal", "document.getElementById('studentDetails').showModal();", true);
                }
                catch { }
            }
        }

        protected void btnSaveFeedback_Click(object sender, EventArgs e)
        {
            string studentId = hfCurrentStudentId.Value;
            string feedback = txtFeedback.Text.Trim();

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(feedback)) return;

            try
            {
                string noteId = "NOTE_" + DateTime.Now.Ticks;
                var newNote = new Dictionary<string, string>
                {
                    { "noteId", noteId },
                    { "studentId", studentId },
                    { "academicSupervisorId", currentSupervisorId },
                    { "note", feedback },
                    { "createdAt", DateTime.Now.ToString("yyyy-MM-dd") }
                };

                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string jsonData = new JavaScriptSerializer().Serialize(newNote);
                    client.UploadString(FirebaseUrl + "academicNotes/" + noteId + ".json", "PUT", jsonData);
                }

                lblFeedbackMsg.Text = "Feedback saved successfully!";
                ScriptManager.RegisterStartupScript(this, GetType(), "KeepModalOpen", "document.getElementById('studentDetails').showModal();", true);
            }
            catch (Exception ex)
            {
                lblFeedbackMsg.Text = "Error saving: " + ex.Message;
                lblFeedbackMsg.ForeColor = System.Drawing.Color.Red;
            }
        }

        public class StudentRow
        {
            public string StudentId { get; set; }
            public string StudentName { get; set; }
            public string Major { get; set; }
            public int ApprovedHours { get; set; }
            public int RequiredHours { get; set; }
            public int TaskPercentage { get; set; }
        }
    }
}