using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Coordinator
{
    public partial class Assign_Supervisors : Page
    {
        // رابط قاعدة البيانات
        private static string FirebaseUrl { get { var firebase = new UIMS.Services.FirebaseConfiguration(); firebase.EnsureEnabled(); return firebase.DatabaseUrl + "/"; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadGroupsFromFirebase();
                LoadDropdowns();
            }
        }

        private void LoadGroupsFromFirebase()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    // 1. قراءة البيانات من Firebase
                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                    string assignmentsJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");
                    string groupsJson = client.DownloadString(FirebaseUrl + "groups.json");

                    // 2. تحويل البيانات من نص إلى قواميس (Dictionaries) مع التأكد من أنها ليست فارغة
                    var users = new Dictionary<string, UserData>();
                    if (!string.IsNullOrEmpty(usersJson) && usersJson != "null")
                    {
                        users = serializer.Deserialize<Dictionary<string, UserData>>(usersJson);
                    }

                    var assignments = new Dictionary<string, AssignmentData>();
                    if (!string.IsNullOrEmpty(assignmentsJson) && assignmentsJson != "null")
                    {
                        assignments = serializer.Deserialize<Dictionary<string, AssignmentData>>(assignmentsJson);
                    }

                    var createdGroups = new Dictionary<string, CreatedGroupData>();
                    if (!string.IsNullOrEmpty(groupsJson) && groupsJson != "null")
                    {
                        createdGroups = serializer.Deserialize<Dictionary<string, CreatedGroupData>>(groupsJson);
                    }

                    // 3. فصل المستخدمين إلى قواميس منفصلة (للطلاب والمشرفين) باستخدام حلقات تكرار بسيطة
                    var supervisors = new Dictionary<string, UserData>();
                    var students = new Dictionary<string, UserData>();

                    foreach (var user in users)
                    {
                        if (user.Value != null)
                        {
                            if (University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(user.Value.role) == "academicSupervisor")
                            {
                                supervisors.Add(user.Key, user.Value);
                            }
                            else if (University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(user.Value.role) == "student")
                            {
                                students.Add(user.Key, user.Value);
                            }
                        }
                    }

                    // 4. تجهيز القائمة النهائية التي ستُعرض في الجدول
                    List<GroupRow> allGroups = new List<GroupRow>();

                    // أولاً: إضافة المجموعات الفارغة (التي تم إنشاؤها للتو)
                    foreach (var g in createdGroups)
                    {
                        GroupRow emptyGroup = new GroupRow();
                        emptyGroup.GroupId = g.Key;
                        emptyGroup.Department = g.Value.department; // القسم الموجود في Firebase
                        emptyGroup.StudentCount = 0;
                        emptyGroup.StudentNames = "No students assigned yet";
                        emptyGroup.SupervisorId = "";
                        emptyGroup.SupervisorName = "Not assigned";
                        emptyGroup.IsAssigned = false;

                        allGroups.Add(emptyGroup);
                    }

                    // 5. تجميع التعيينات يدوياً (كل مجموعة نضع بداخلها تعيينات الطلاب التابعين لها)
                    var assignmentGroups = new Dictionary<string, List<AssignmentData>>();
                    foreach (var assign in assignments)
                    {
                        if (assign.Value != null && !string.IsNullOrEmpty(assign.Value.groupId))
                        {
                            string gId = assign.Value.groupId;
                            // إذا لم تكن المجموعة موجودة في القاموس، ننشئ لها قائمة جديدة
                            if (!assignmentGroups.ContainsKey(gId))
                            {
                                assignmentGroups.Add(gId, new List<AssignmentData>());
                            }
                            // إضافة التعيين لقائمة المجموعة
                            assignmentGroups[gId].Add(assign.Value);
                        }
                    }

                    // 6. المرور على المجموعات المجمعة لدمج بيانات الطلاب والمشرف
                    foreach (var group in assignmentGroups)
                    {
                        string currentGroupId = group.Key;
                        string supervisorId = "";
                        string supervisorName = "Not assigned";
                        string defaultDept = "Not specified";
                        int studentCount = 0;
                        List<string> studentNamesList = new List<string>();

                        // المرور على تعيينات الطلاب داخل هذه المجموعة
                        foreach (var assignment in group.Value)
                        {
                            string sId = assignment.studentId;

                            // إذا كان الطالب موجوداً
                            if (students.ContainsKey(sId))
                            {
                                UserData studentData = students[sId];
                                studentNamesList.Add(studentData.fullName);
                                studentCount++;

                                // أخذ اسم القسم من أول طالب
                                if (defaultDept == "Not specified")
                                {
                                    if (!string.IsNullOrEmpty(studentData.college))
                                    {
                                        defaultDept = studentData.college;
                                    }
                                    else if (!string.IsNullOrEmpty(studentData.department))
                                    {
                                        defaultDept = studentData.department;
                                    }
                                }
                            }

                            // سحب رقم المشرف
                            if (supervisorId == "" && !string.IsNullOrEmpty(assignment.academicSupervisorId))
                            {
                                supervisorId = assignment.academicSupervisorId;
                            }
                        }

                        // جلب اسم المشرف
                        if (supervisorId != "" && supervisors.ContainsKey(supervisorId))
                        {
                            supervisorName = supervisors[supervisorId].fullName;
                        }

                        // دمج الأسماء في نص واحد
                        string finalStudentNames = string.Join(", ", studentNamesList);

                        // 7. التحقق هل المجموعة موجودة في القائمة النهائية لتحديثها أو إضافتها كجديدة
                        bool groupFound = false;
                        foreach (var existingGroup in allGroups)
                        {
                            if (existingGroup.GroupId == currentGroupId)
                            {
                                existingGroup.StudentCount = studentCount;
                                existingGroup.StudentNames = finalStudentNames;
                                existingGroup.SupervisorId = supervisorId;
                                existingGroup.SupervisorName = supervisorName;
                                existingGroup.IsAssigned = (supervisorId != "");
                                groupFound = true;
                                break; // نوقف البحث لأننا وجدناها
                            }
                        }

                        if (!groupFound) // إذا لم نجدها في المجموعات الفارغة، ننشئ لها صفاً جديداً
                        {
                            GroupRow newRow = new GroupRow();
                            newRow.GroupId = currentGroupId;
                            newRow.Department = defaultDept;
                            newRow.StudentCount = studentCount;
                            newRow.StudentNames = finalStudentNames;
                            newRow.SupervisorId = supervisorId;
                            newRow.SupervisorName = supervisorName;
                            newRow.IsAssigned = (supervisorId != "");

                            allGroups.Add(newRow);
                        }
                    }

                    // 8. عرض البيانات في الجدول
                    rptGroups.DataSource = allGroups;
                    rptGroups.DataBind();

                    // 9. حساب الإحصائيات للبطاقات العلوية باستخدام حلقات تكرار بسيطة
                    int totalGroups = 0;
                    int assignedGroups = 0;
                    int unassignedGroups = 0;

                    foreach (var g in allGroups)
                    {
                        totalGroups++;
                        if (g.IsAssigned == true)
                        {
                            assignedGroups++;
                        }
                        else
                        {
                            unassignedGroups++;
                        }
                    }

                    lblTotalGroups.Text = totalGroups.ToString();
                    lblAssignedGroups.Text = assignedGroups.ToString();
                    lblUnassignedGroups.Text = unassignedGroups.ToString();

                    lblFirebaseStatus.CssClass = "alert alert-success d-block";
                    lblFirebaseStatus.Text = "Data loaded successfully.";
                }
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.CssClass = "alert alert-danger d-block";
                lblFirebaseStatus.Text = "Error loading data: " + ex.Message;
            }
        }

        private void LoadDropdowns()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                    string assignmentsJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");

                    var users = new Dictionary<string, UserData>();
                    if (!string.IsNullOrEmpty(usersJson) && usersJson != "null")
                    {
                        users = serializer.Deserialize<Dictionary<string, UserData>>(usersJson);
                    }

                    var assignments = new Dictionary<string, AssignmentData>();
                    if (!string.IsNullOrEmpty(assignmentsJson) && assignmentsJson != "null")
                    {
                        assignments = serializer.Deserialize<Dictionary<string, AssignmentData>>(assignmentsJson);
                    }

                    // مسح القوائم لتجهيزها
                    ddlSupervisors.Items.Clear();
                    ddlSupervisors.Items.Add(new ListItem("Choose Supervisor...", ""));

                    ddlUnassignedStudents.Items.Clear();
                    ddlUnassignedStudents.Items.Add(new ListItem("Select a student to add...", ""));

                    // تجميع أرقام الطلاب المعينين في قائمة لنستثنيهم لاحقاً
                    List<string> assignedStudentIds = new List<string>();
                    foreach (var assign in assignments)
                    {
                        if (assign.Value != null)
                        {
                            assignedStudentIds.Add(assign.Value.studentId);
                        }
                    }

                    // المرور على كل المستخدمين لتعبئة القوائم
                    foreach (var user in users)
                    {
                        if (user.Value != null)
                        {
                            // إذا كان مشرفاً، أضفه لقائمة المشرفين
                            if (user.Value.role == "AcademicSupervisor")
                            {
                                string text = user.Value.fullName + " (" + user.Value.department + ")";
                                ddlSupervisors.Items.Add(new ListItem(text, user.Key));
                            }
                            // إذا كان طالباً ولم يتم تعيينه مسبقاً، أضفه لقائمة الطلاب
                            else if (user.Value.role == "Student")
                            {
                                bool isAssigned = false;
                                foreach (var id in assignedStudentIds)
                                {
                                    if (id == user.Key)
                                    {
                                        isAssigned = true;
                                        break;
                                    }
                                }

                                if (!isAssigned) // الطالب غير معين
                                {
                                    string dept = user.Value.major;
                                    if (string.IsNullOrEmpty(dept)) dept = user.Value.college;

                                    string text = user.Value.fullName + " - " + dept;
                                    ddlUnassignedStudents.Items.Add(new ListItem(text, user.Key));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        protected void rptGroups_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string groupId = e.CommandArgument.ToString();

            if (e.CommandName == "ViewStudents")
            {
                hfSelectedGroupId.Value = groupId;
                lblModalGroupName.Text = groupId;
                LoadDropdowns();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowViewModal", "var m = new bootstrap.Modal(document.getElementById('viewStudentsModal')); m.show();", true);
            }
            else if (e.CommandName == "AssignSupervisor")
            {
                hfAssignGroupId.Value = groupId;
                txtAssignGroupName.Text = groupId;
                LoadDropdowns();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowAssignModal", "var m = new bootstrap.Modal(document.getElementById('assignModal')); m.show();", true);
            }
            else if (e.CommandName == "DeleteGroup")
            {
                hfDeleteGroupId.Value = groupId;
                lblDeleteGroupName.Text = groupId;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowDeleteModal", "var m = new bootstrap.Modal(document.getElementById('deleteGroupModal')); m.show();", true);
            }
        }

        protected void btnCreateGroup_Click(object sender, EventArgs e)
        {
            string groupName = newGroupName.Text.Trim();
            string department = newGroupDept.SelectedValue;

            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(department))
            {
                lblFirebaseStatus.CssClass = "alert alert-warning d-block";
                lblFirebaseStatus.Text = "Please fill all group fields.";
                return;
            }

            try
            {
                var newGroup = new CreatedGroupData();
                newGroup.department = department;
                newGroup.createdAt = DateTime.Now.ToString("o");

                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string jsonData = serializer.Serialize(newGroup);

                    new UIMS.Services.FirebaseConfiguration().EnsureWritesAllowed(); client.UploadString(FirebaseUrl + "groups/" + groupName + ".json", "PUT", jsonData);
                }

                newGroupName.Text = "";
                newGroupDept.SelectedIndex = 0;
                LoadGroupsFromFirebase();
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.Text = "Error creating group: " + ex.Message;
            }
        }

        protected void btnAddStudentToGroup_Click(object sender, EventArgs e)
        {
            string studentId = ddlUnassignedStudents.SelectedValue;
            string groupId = hfSelectedGroupId.Value;

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(groupId)) return;

            try
            {
                string supervisorId = "";

                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string assignJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");

                    if (!string.IsNullOrEmpty(assignJson) && assignJson != "null")
                    {
                        var assignments = serializer.Deserialize<Dictionary<string, AssignmentData>>(assignJson);

                        // البحث عن المشرف الحالي للمجموعة لتعيينه للطالب الجديد تلقائياً
                        foreach (var a in assignments)
                        {
                            if (a.Value != null && a.Value.groupId == groupId)
                            {
                                if (!string.IsNullOrEmpty(a.Value.academicSupervisorId))
                                {
                                    supervisorId = a.Value.academicSupervisorId;
                                    break;
                                }
                            }
                        }
                    }

                    // إنشاء تعيين جديد وإرساله
                    var newAssignment = new AssignmentData();
                    newAssignment.studentId = studentId;
                    newAssignment.academicSupervisorId = supervisorId;
                    newAssignment.groupId = groupId;
                    newAssignment.active = true;

                    string jsonData = serializer.Serialize(newAssignment);
                    new UIMS.Services.FirebaseConfiguration().EnsureWritesAllowed(); client.UploadString(FirebaseUrl + "supervisorAssignments/" + studentId + ".json", "PUT", jsonData);
                }

                LoadGroupsFromFirebase();
                LoadDropdowns();
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.Text = "Error adding student: " + ex.Message;
            }
        }

        protected void btnConfirmAssign_Click(object sender, EventArgs e)
        {
            string supervisorId = ddlSupervisors.SelectedValue;
            string groupId = hfAssignGroupId.Value;

            if (string.IsNullOrEmpty(supervisorId)) return;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string assignJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");
                    if (!string.IsNullOrEmpty(assignJson) && assignJson != "null")
                    {
                        var assignments = serializer.Deserialize<Dictionary<string, AssignmentData>>(assignJson);

                        // تحديث كل التعيينات التابعة لهذه المجموعة
                        foreach (var a in assignments)
                        {
                            if (a.Value != null && a.Value.groupId == groupId)
                            {
                                a.Value.academicSupervisorId = supervisorId;
                                string updateData = serializer.Serialize(a.Value);
                                new UIMS.Services.FirebaseConfiguration().EnsureWritesAllowed(); (new UIMS.Services.FirebaseConfiguration()).EnsureWritesAllowed(); client.UploadString(FirebaseUrl + "supervisorAssignments/" + a.Key + ".json", "PUT", updateData);
                            }
                        }
                    }
                }
                LoadGroupsFromFirebase();
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.Text = "Error assigning supervisor: " + ex.Message;
            }
        }

        protected void btnDeleteGroupConfirm_Click(object sender, EventArgs e)
        {
            string groupId = hfDeleteGroupId.Value;
            if (string.IsNullOrEmpty(groupId)) return;

            try
            {
                using (WebClient client = new WebClient())
                {
                    // 1. حذف المجموعة الأساسية
                    try
                    {
                        new UIMS.Services.FirebaseConfiguration().EnsureWritesAllowed(); (new UIMS.Services.FirebaseConfiguration()).EnsureWritesAllowed(); client.UploadString(FirebaseUrl + "groups/" + groupId + ".json", "DELETE", "");
                    }
                    catch { }

                    // 2. فك ارتباط الطلاب وحذف تعييناتهم
                    client.Encoding = System.Text.Encoding.UTF8;
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string assignJson = client.DownloadString(FirebaseUrl + "supervisorAssignments.json");

                    if (!string.IsNullOrEmpty(assignJson) && assignJson != "null")
                    {
                        var assignments = serializer.Deserialize<Dictionary<string, AssignmentData>>(assignJson);

                        foreach (var a in assignments)
                        {
                            if (a.Value != null && a.Value.groupId == groupId)
                            {
                                new UIMS.Services.FirebaseConfiguration().EnsureWritesAllowed(); (new UIMS.Services.FirebaseConfiguration()).EnsureWritesAllowed(); client.UploadString(FirebaseUrl + "supervisorAssignments/" + a.Key + ".json", "DELETE", "");
                            }
                        }
                    }
                }
                LoadGroupsFromFirebase();
                LoadDropdowns();
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.Text = "Error deleting group: " + ex.Message;
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Response.Redirect("~/Auth/Login.aspx", false);
        }

        // --- Data Classes ---
        public class UserData { public string fullName { get; set; } public string role { get; set; } public string college { get; set; } public string department { get; set; } public string major { get; set; } }
        public class AssignmentData { public string studentId { get; set; } public string academicSupervisorId { get; set; } public string groupId { get; set; } public bool active { get; set; } }
        public class CreatedGroupData { public string department { get; set; } public string createdAt { get; set; } }
        public class GroupRow { public string GroupId { get; set; } public string Department { get; set; } public int StudentCount { get; set; } public string StudentNames { get; set; } public string SupervisorId { get; set; } public string SupervisorName { get; set; } public bool IsAssigned { get; set; } }
    }
}