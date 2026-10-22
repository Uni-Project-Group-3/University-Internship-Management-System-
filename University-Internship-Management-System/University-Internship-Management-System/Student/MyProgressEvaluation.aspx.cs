using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Student
{
    public partial class MyProgressEvaluation : Page
    {
        // رابط قاعدة البيانات الخاص بكِ
        private const string FirebaseUrl = "https://unviersity-data-default-rtdb.firebaseio.com/";

        // سنفترض أننا نعرض بيانات الطالب أحمد (ST001) لتجربة الصفحة
        private string currentStudentId = "ST001";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadStudentProgress();
            }
        }

        private void LoadStudentProgress()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    // 1. جلب البيانات من Firebase
                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                    string hoursJson = client.DownloadString(FirebaseUrl + "trainingHours.json");
                    string tasksJson = client.DownloadString(FirebaseUrl + "tasks.json");

                    var users = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(usersJson ?? "{}");
                    var allHours = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(hoursJson ?? "{}");
                    var allTasks = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(tasksJson ?? "{}");

                    // 2. عرض اسم الطالب والساعات المطلوبة منه
                    int requiredHours = 300; // قيمة افتراضية
                    if (users != null && users.ContainsKey(currentStudentId))
                    {
                        var studentData = users[currentStudentId];
                        NavName.Text = studentData.ContainsKey("fullName") ? studentData["fullName"] : "Student";

                        if (studentData.ContainsKey("requiredTrainingHours"))
                        {
                            int.TryParse(studentData["requiredTrainingHours"], out requiredHours);
                        }
                    }

                    RequiredHours.Text = requiredHours.ToString();

                    // ================= 3. حساب ساعات التدريب =================
                    int approvedHours = 0;
                    int pendingHours = 0;

                    if (allHours != null)
                    {
                        foreach (var record in allHours)
                        {
                            var hourData = record.Value;
                            if (hourData != null && hourData.ContainsKey("studentId") && hourData["studentId"] == currentStudentId)
                            {
                                int hours = 0;
                                if (hourData.ContainsKey("hours"))
                                {
                                    int.TryParse(hourData["hours"], out hours);
                                }

                                string status = hourData.ContainsKey("status") ? hourData["status"] : "";

                                if (status == "Approved")
                                {
                                    approvedHours = approvedHours + hours;
                                }
                                else if (status == "Pending")
                                {
                                    pendingHours = pendingHours + hours;
                                }
                            }
                        }
                    }

                    // حساب الساعات المتبقية والنسبة المئوية
                    int remainingHours = requiredHours - approvedHours;
                    if (remainingHours < 0) remainingHours = 0; // حتى لا يظهر رقم بالسالب

                    int hoursPercentage = 0;
                    if (requiredHours > 0)
                    {
                        hoursPercentage = (approvedHours * 100) / requiredHours;
                        if (hoursPercentage > 100) hoursPercentage = 100;
                    }

                    // ربط أرقام الساعات بواجهة الـ HTML
                    ApprovedHours.Text = approvedHours.ToString();
                    RemainingHours.Text = remainingHours.ToString();
                    HoursApprovalStatus.Text = approvedHours.ToString();
                    PendingHours.Text = pendingHours.ToString();

                    HoursPercent.Text = hoursPercentage.ToString();
                    HoursProgress.Attributes["value"] = hoursPercentage.ToString(); // شريط التقدم

                    // ================= 4. حساب وتصنيف المهام =================
                    int totalAssigned = 0;
                    int totalSubmitted = 0;
                    int totalEvaluated = 0;
                    int totalRevision = 0;

                    List<TaskDisplay> studentTasks = new List<TaskDisplay>();

                    if (allTasks != null)
                    {
                        foreach (var t in allTasks)
                        {
                            var taskData = t.Value;
                            if (taskData != null && taskData.ContainsKey("studentId") && taskData["studentId"] == currentStudentId)
                            {
                                totalAssigned++; // عداد إجمالي المهام

                                string status = taskData.ContainsKey("status") ? taskData["status"] : "Assigned";

                                // تصنيف المهام للإحصائيات العلوية
                                if (status == "Submitted") totalSubmitted++;
                                else if (status == "Evaluated" || status == "Completed") totalEvaluated++;
                                else if (status == "Revision Needed") totalRevision++;

                                // تجهيز المهمة لعرضها في الجدول السفلي
                                TaskDisplay newTask = new TaskDisplay();
                                newTask.Title = taskData.ContainsKey("title") ? taskData["title"] : "Untitled Task";
                                newTask.Reference = t.Key;
                                newTask.Status = status;
                                newTask.History = "Deadline: " + (taskData.ContainsKey("deadline") ? taskData["deadline"] : "N/A");

                                // تحديد لون الشارة (Badge) بناءً على الحالة
                                if (status == "Completed" || status == "Evaluated") newTask.Badge = "success";
                                else if (status == "Revision Needed" || status == "Overdue") newTask.Badge = "danger";
                                else newTask.Badge = "warning";

                                newTask.Feedback = "Pending supervisor review.";
                                newTask.NextStep = " Keep up the good work.";
                                newTask.NoteClass = "text-muted";

                                studentTasks.Add(newTask);
                            }
                        }
                    }

                    // عرض إحصائيات المهام
                    AssignedTasks.Text = totalAssigned.ToString();
                    SubmittedTasks.Text = totalSubmitted.ToString();
                    EvaluatedTasks.Text = totalEvaluated.ToString();
                    RevisionTasks.Text = totalRevision.ToString();

                    // عرض جدول المهام
                    Tasks.DataSource = studentTasks;
                    Tasks.DataBind();

                    // ================= 5. تجهيز قسم الامتحانات (التقييمات) =================
                    // في هذه المرحلة سنضع قيماً مبدئية للامتحانات لتشغيل الواجهة
                    EvaluatedExams.Text = "0";
                    NoExams.Visible = true;
                    ExamList.Visible = false;
                }
            }
            catch (Exception)
            {
                // في حال وجود خطأ، نضع قيماً صفرية لحماية الصفحة من الانهيار
                ApprovedHours.Text = "0";
                RequiredHours.Text = "0";
                RemainingHours.Text = "0";
                HoursPercent.Text = "0";
            }
        }

        // ================= زر عرض وتجربة تفاصيل الامتحانات (Demo) =================
        protected void ToggleExamDemo_Click(object sender, EventArgs e)
        {
            // هذا الزر مبرمج في تصميمك لإظهار إخفاء قائمة الامتحانات التجريبية
            if (ExamList.Visible == false)
            {
                NoExams.Visible = false;
                ExamList.Visible = true;
                ExamDemoNotice.Visible = true;

                // إنشاء امتحان تجريبي واحد لعرضه
                List<ExamDisplay> demoExams = new List<ExamDisplay>();
                ExamDisplay exam1 = new ExamDisplay();
                exam1.Id = "EXAM1";
                exam1.Title = "Midterm Technical Evaluation";
                exam1.ExamDate = DateTime.Now.AddDays(-10);
                exam1.CompanySupervisor = "Khaled Company Sup";
                exam1.Grade = 95.5;
                exam1.MaximumGrade = 100;
                demoExams.Add(exam1);

                Exams.DataSource = demoExams;
                Exams.DataBind();
                EvaluatedExams.Text = "1";
            }
            else
            {
                NoExams.Visible = true;
                ExamList.Visible = false;
                ExamDemoNotice.Visible = false;
                EvaluatedExams.Text = "0";
            }
        }

        // فتح النافذة المنبثقة لتفاصيل الامتحان
        protected void Exams_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "ViewExam")
            {
                // تعبئة البيانات الوهمية في النافذة المنبثقة
                ExamTitle.Text = "Midterm Technical Evaluation";
                ExamDescription.Text = "General evaluation of frontend skills and commitment.";
                ExamDate.Text = DateTime.Now.AddDays(-10).ToString("dd MMM yyyy");
                ExamSupervisor.Text = "Khaled Company Sup";
                ExamGrade.Text = "95.5 / 100";
                CompanyFeedback.Text = "Excellent performance during the first half of the internship.";
                AcademicFeedbackStatus.Text = "Feedback added";
                AcademicFeedbackStatus.CssClass = "workspace-badge success";
                AcademicFeedback.Text = "Great job, keep it up!";

                ExamDetails.Visible = true;

                // أمر جافاسكربت لفتح النافذة
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowExamModal", "document.getElementById('examDialog').showModal();", true);
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Response.Redirect("~/Auth/Login.aspx", false);
        }

        // ================= كلاسات مساعدة لتنظيم بيانات الجدول =================
        public class TaskDisplay
        {
            public string Title { get; set; }
            public string Reference { get; set; }
            public string Status { get; set; }
            public string StatusKey { get; set; }
            public string History { get; set; }
            public string Badge { get; set; }
            public string Feedback { get; set; }
            public string NextStep { get; set; }
            public string NoteClass { get; set; }
        }

        public class ExamDisplay
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public DateTime ExamDate { get; set; }
            public string CompanySupervisor { get; set; }
            public double Grade { get; set; }
            public double MaximumGrade { get; set; }
        }
    }
}