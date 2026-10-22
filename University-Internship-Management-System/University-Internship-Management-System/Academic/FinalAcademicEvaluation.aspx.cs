using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Academic
{
    public partial class FinalAcademicEvaluation : Page
    {
        private const string FirebaseUrl = "https://unviersity-data-default-rtdb.firebaseio.com/";
        private string currentSupervisorId = "AS001"; // رقم المشرف الأكاديمي الحالي

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadEvaluations();
            }
        }

        private void LoadEvaluations()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    // 1. سحب البيانات من فايربيس (مع معالجة حالة إذا كان الملف مش موجود أصلاً)
                    string assignJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");
                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");

                    // استخدمنا try-catch صغيرة هون عشان لو ملف التقييمات لسا ما انخلق في فايربيس ما يضرب الكود
                    string evalJson = "{}";
                    try { evalJson = client.DownloadString(FirebaseUrl + "finalAcademicEvaluations.json"); } catch { }

                    // تحويل الـ JSON إلى قواميس (Dictionaries)
                    // في حال كان فايربيس يرجع "null" كنص، بنحوله لـ "{}" عشان ما يضرب
                    if (assignJson == "null") assignJson = "{}";
                    if (usersJson == "null") usersJson = "{}";
                    if (evalJson == "null") evalJson = "{}";

                    var assignments = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(assignJson);
                    var users = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(usersJson);
                    var evaluations = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(evalJson);

                    List<EvalRow> displayList = new List<EvalRow>();
                    int totalMyStudents = 0;
                    int submittedCount = 0;

                    // 2. حماية: نتأكد إن assignments مش null قبل ما نلف عليها
                    if (assignments != null)
                    {
                        foreach (var assign in assignments)
                        {
                            if (assign.Value != null &&
                                assign.Value.ContainsKey("academicSupervisorId") &&
                                assign.Value["academicSupervisorId"] == currentSupervisorId)
                            {
                                string sId = assign.Value.ContainsKey("studentId") ? assign.Value["studentId"] : "";

                                // حماية: نتأكد إن users مش null وإن الطالب موجود
                                if (sId != "" && users != null && users.ContainsKey(sId))
                                {
                                    totalMyStudents++;
                                    var studentData = users[sId];

                                    EvalRow row = new EvalRow();
                                    row.StudentId = sId;
                                    row.StudentName = studentData.ContainsKey("fullName") ? studentData["fullName"] : "Unknown";
                                    row.Major = studentData.ContainsKey("major") ? studentData["major"] : "Not Specified";

                                    row.Status = "Pending";
                                    row.StatusStyle = "background-color: #ffeeba; color: #856404; padding: 4px 8px; border-radius: 4px; font-weight: bold;";

                                    // 3. حماية: نتأكد إن evaluations مش null قبل ما نلف عليها
                                    if (evaluations != null)
                                    {
                                        foreach (var eval in evaluations)
                                        {
                                            if (eval.Value != null &&
                                                eval.Value.ContainsKey("studentId") && eval.Value["studentId"] == sId &&
                                                eval.Value.ContainsKey("academicSupervisorId") && eval.Value["academicSupervisorId"] == currentSupervisorId)
                                            {
                                                row.Status = "Submitted";
                                                row.StatusStyle = "background-color: #d4edda; color: #155724; padding: 4px 8px; border-radius: 4px; font-weight: bold;";
                                                submittedCount++;
                                                break;
                                            }
                                        }
                                    }

                                    displayList.Add(row);
                                }
                            }
                        }
                    }

                    // 4. عرض الأرقام والجدول
                    lblTotalStudents.Text = totalMyStudents.ToString();
                    lblSubmitted.Text = submittedCount.ToString();
                    lblPending.Text = (totalMyStudents - submittedCount).ToString();

                    rptEvaluations.DataSource = displayList;
                    rptEvaluations.DataBind();

                    lblMessage.Text = ""; // مسح رسالة الخطأ إذا نجح التحميل
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading evaluations: " + ex.Message;
            }
        }

        protected void rptEvaluations_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "Evaluate")
            {
                string studentId = e.CommandArgument.ToString();
                hfEvalStudentId.Value = studentId;

                try
                {
                    // جلب اسم الطالب لعرضه في النافذة
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = System.Text.Encoding.UTF8;
                        string userJson = client.DownloadString(FirebaseUrl + "users/" + studentId + ".json");
                        var studentData = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(userJson ?? "{}");

                        lblEvalName.Text = studentData != null && studentData.ContainsKey("fullName") ? studentData["fullName"] : studentId;
                    }

                    // تفريغ الحقول القديمة
                    ddlFinalDecision.SelectedIndex = 0;
                    txtEvalComments.Text = "";
                    lblModalMsg.Text = "";

                    // فتح النافذة
                    ScriptManager.RegisterStartupScript(this, GetType(), "ShowEvalModal", "document.getElementById('evaluationDialog').showModal();", true);
                }
                catch { }
            }
        }

        protected void btnSubmitEval_Click(object sender, EventArgs e)
        {
            string studentId = hfEvalStudentId.Value;
            string decision = ddlFinalDecision.SelectedValue;
            string comments = txtEvalComments.Text.Trim();

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(decision))
            {
                lblModalMsg.Text = "Please select a final decision.";
                lblModalMsg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "KeepEvalModal", "document.getElementById('evaluationDialog').showModal();", true);
                return;
            }

            try
            {
                // حفظ التقييم في عقدة جديدة (نستخدم رقم الطالب كـ ID للتقييم لضمان تقييم واحد لكل طالب)
                string evalId = "EVAL_" + studentId;

                var finalEval = new Dictionary<string, string>
                {
                    { "evaluationId", evalId },
                    { "studentId", studentId },
                    { "academicSupervisorId", currentSupervisorId },
                    { "finalDecision", decision },
                    { "academicComments", comments },
                    { "submittedAt", DateTime.Now.ToString("yyyy-MM-dd") }
                };

                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string jsonData = new JavaScriptSerializer().Serialize(finalEval);

                    // استخدام PUT لإضافة التقييم أو تعديله إذا كان موجوداً
                    client.UploadString(FirebaseUrl + "finalAcademicEvaluations/" + evalId + ".json", "PUT", jsonData);
                }

                // إغلاق النافذة وتحديث الجدول ليظهر أن الطالب أصبح Submitted
                LoadEvaluations();
                ScriptManager.RegisterStartupScript(this, GetType(), "CloseEvalModal", "document.getElementById('evaluationDialog').close();", true);
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error saving evaluation: " + ex.Message;
            }
        }

        // كلاس مساعد لترتيب بيانات الجدول
        public class EvalRow
        {
            public string StudentId { get; set; }
            public string StudentName { get; set; }
            public string Major { get; set; }
            public string Status { get; set; }
            public string StatusStyle { get; set; }
        }
    }
}