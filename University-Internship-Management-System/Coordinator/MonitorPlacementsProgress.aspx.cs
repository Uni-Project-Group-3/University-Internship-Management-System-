using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace University_Internship_Management_System.Coordinator
{
    public partial class MonitorPlacementsProgress : Page
    {
        private static string FirebaseUrl { get { var firebase = new UIMS.Services.FirebaseConfiguration(); firebase.EnsureEnabled(); return firebase.DatabaseUrl + "/"; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadAllPlacements();
            }
        }

        private void LoadAllPlacements()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    // 1. تحميل كافة الملفات المطلوبة من قاعدة البيانات مع حماية try-catch لتجنب الأخطاء
                    string usersJson = "{}";
                    string assignJson = "{}";
                    string hoursJson = "{}";
                    string tasksJson = "{}";

                    try { usersJson = client.DownloadString(FirebaseUrl + "users.json") ?? "{}"; } catch { }
                    try { assignJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json") ?? "{}"; } catch { }
                    try { hoursJson = client.DownloadString(FirebaseUrl + "trainingHours.json") ?? "{}"; } catch { }
                    try { tasksJson = client.DownloadString(FirebaseUrl + "tasks.json") ?? "{}"; } catch { }

                    if (usersJson == "null") usersJson = "{}";
                    if (assignJson == "null") assignJson = "{}";
                    if (hoursJson == "null") hoursJson = "{}";
                    if (tasksJson == "null") tasksJson = "{}";

                    var users = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(usersJson);
                    var assignments = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(assignJson);
                    var hours = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(hoursJson);
                    var tasks = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(tasksJson);

                    List<PlacementRow> placementsList = new List<PlacementRow>();

                    // عدادات الإحصائيات
                    int totalStudents = 0;
                    int activeInternships = 0;
                    int totalCompletedTasks = 0;
                    int riskInternships = 0;

                    if (users != null)
                    {
                        // 2. الدوران على كل المستخدمين لاستخراج "الطلاب" فقط
                        foreach (var user in users)
                        {
                            var uData = user.Value;
                            if (uData != null && uData.ContainsKey("role") && University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(uData["role"]) == "student")
                            {
                                totalStudents++;
                                string sId = user.Key;

                                PlacementRow row = new PlacementRow();
                                row.StudentId = sId;
                                row.StudentName = uData.ContainsKey("fullName") ? uData["fullName"] : "Unknown";
                                row.Major = uData.ContainsKey("major") ? uData["major"] : "Not Specified";
                                row.Company = uData.ContainsKey("trainingCompany") ? uData["trainingCompany"] : "Unassigned";
                                row.CompanySupervisor = "N/A"; // افتراضي
                                row.TrainingStatus = uData.ContainsKey("status") ? uData["status"] : "Active";

                                int reqHours = 300;
                                if (uData.ContainsKey("requiredTrainingHours")) int.TryParse(uData["requiredTrainingHours"], out reqHours);
                                row.RequiredHours = reqHours;

                                // 3. جلب المشرف الأكاديمي للطالب من ملف supervisorAssignments
                                row.AcademicSupervisor = "Not Assigned";
                                if (assignments != null)
                                {
                                    foreach (var assign in assignments)
                                    {
                                        if (assign.Value != null && assign.Value.ContainsKey("studentId") && assign.Value["studentId"] == sId)
                                        {
                                            string supId = assign.Value.ContainsKey("academicSupervisorId") ? assign.Value["academicSupervisorId"] : "";
                                            // جلب اسم المشرف بدل رقمه
                                            if (users.ContainsKey(supId) && users[supId].ContainsKey("fullName"))
                                            {
                                                row.AcademicSupervisor = users[supId]["fullName"];
                                            }
                                            else
                                            {
                                                row.AcademicSupervisor = supId;
                                            }
                                            break; // وجدنا المشرف، لا داعي لإكمال البحث
                                        }
                                    }
                                }

                                // 4. حساب الساعات المعتمدة
                                int appHours = 0;
                                if (hours != null)
                                {
                                    foreach (var h in hours)
                                    {
                                        if (h.Value != null && h.Value.ContainsKey("studentId") && h.Value["studentId"] == sId && h.Value.ContainsKey("status") && h.Value["status"] == "Approved")
                                        {
                                            int hr = 0;
                                            if (h.Value.ContainsKey("hours")) int.TryParse(h.Value["hours"], out hr);
                                            appHours += hr;
                                        }
                                    }
                                }
                                row.ApprovedHours = appHours;

                                if (appHours > 0 && appHours < reqHours) activeInternships++; // طالب نشط
                                if (appHours == 0) riskInternships++; // طالب لم ينجز أي ساعة (تحذير للمنسق)

                                // 5. حساب نسبة إنجاز المهام
                                int totalT = 0;
                                int compT = 0;
                                if (tasks != null)
                                {
                                    foreach (var t in tasks)
                                    {
                                        if (t.Value != null && t.Value.ContainsKey("studentId") && t.Value["studentId"] == sId)
                                        {
                                            totalT++;
                                            if (t.Value.ContainsKey("status") && (t.Value["status"] == "Evaluated" || t.Value["status"] == "Completed"))
                                            {
                                                compT++;
                                                totalCompletedTasks++;
                                            }
                                        }
                                    }
                                }
                                row.TaskPercentage = totalT > 0 ? (compT * 100) / totalT : 0;

                                placementsList.Add(row);
                            }
                        }
                    }

                    // 6. ربط البيانات وعرض الإحصائيات
                    lblTotalStudents.Text = totalStudents.ToString();
                    lblActiveInternships.Text = activeInternships.ToString();
                    lblCompletedTasks.Text = totalCompletedTasks.ToString();
                    lblRiskInternships.Text = riskInternships.ToString();

                    rptPlacements.DataSource = placementsList;
                    rptPlacements.DataBind();
                    lblMessage.Text = ""; // مسح الخطأ إن وجد
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading placement data: " + ex.Message;
            }
        }

        // كلاس مساعد لترتيب بيانات الجدول
        public class PlacementRow
        {
            public string StudentId { get; set; }
            public string StudentName { get; set; }
            public string Major { get; set; }
            public string Company { get; set; }
            public string CompanySupervisor { get; set; }
            public string AcademicSupervisor { get; set; }
            public string TrainingStatus { get; set; }
            public int ApprovedHours { get; set; }
            public int RequiredHours { get; set; }
            public int TaskPercentage { get; set; }
        }
    }
}