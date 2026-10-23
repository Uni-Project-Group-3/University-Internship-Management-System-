using University_Internship_Management_System.Shared;
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
        private static string FirebaseUrl { get { return AuthenticatedFirebaseClient.DatabaseUrl; } }

        protected void Page_Init(object sender, EventArgs e) { Session["FirebasePageBinding"] = true; ViewStateUserKey = Session.SessionID; }
        protected void Page_Load(object sender, EventArgs e)
        {
            AuthenticatedFirebaseClient.RequireRole(Context, "coordinator");
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
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                    string groupsJson = "";
                    try { groupsJson = client.DownloadString(FirebaseUrl + "groups.json"); } catch { }

                    var users = string.IsNullOrEmpty(usersJson) || usersJson == "null" ? new Dictionary<string, UserData>() : serializer.Deserialize<Dictionary<string, UserData>>(usersJson);
                    var groups = string.IsNullOrEmpty(groupsJson) || groupsJson == "null" ? new Dictionary<string, CreatedGroupData>() : serializer.Deserialize<Dictionary<string, CreatedGroupData>>(groupsJson);

                    List<GroupRow> allGroups = new List<GroupRow>();
                    int totalGroups = 0, assignedGroups = 0, unassignedGroups = 0;

                    foreach (var g in groups)
                    {
                        GroupRow row = new GroupRow
                        {
                            GroupId = g.Key,
                            Department = g.Value.department,
                            SupervisorId = g.Value.academicSupervisorId ?? "",
                            StudentCount = 0,
                            StudentNames = "",
                            IsAssigned = !string.IsNullOrEmpty(g.Value.academicSupervisorId)
                        };

                        // 1. جلب اسم المشرف
                        row.SupervisorName = "Not assigned";
                        if (row.IsAssigned)
                        {
                            foreach (var u in users)
                            {
                                if (u.Value.role == "academicSupervisor" && u.Value.academicSupervisorId == row.SupervisorId)
                                {
                                    row.SupervisorName = u.Value.fullName;
                                    break;
                                }
                            }
                        }

                        // 2. جلب الطلاب
                        if (g.Value.students != null && g.Value.students.Count > 0)
                        {
                            row.StudentCount = g.Value.students.Count;
                            List<string> sNames = new List<string>();
                            foreach (var studentKey in g.Value.students.Keys)
                            {
                                foreach (var u in users)
                                {
                                    if (u.Value.role == "student" && u.Value.studentId == studentKey)
                                    {
                                        sNames.Add(u.Value.fullName);
                                        break;
                                    }
                                }
                            }
                            row.StudentNames = sNames.Count > 0 ? string.Join(", ", sNames) : "Unknown Students";
                        }
                        else
                        {
                            row.StudentNames = "No students assigned yet";
                        }

                        allGroups.Add(row);
                        totalGroups++;
                        if (row.IsAssigned) assignedGroups++; else unassignedGroups++;
                    }

                    rptGroups.DataSource = allGroups;
                    rptGroups.DataBind();

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
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                    string groupsJson = "";
                    try { groupsJson = client.DownloadString(FirebaseUrl + "groups.json"); } catch { }

                    var users = string.IsNullOrEmpty(usersJson) || usersJson == "null" ? new Dictionary<string, UserData>() : serializer.Deserialize<Dictionary<string, UserData>>(usersJson);
                    var groups = string.IsNullOrEmpty(groupsJson) || groupsJson == "null" ? new Dictionary<string, CreatedGroupData>() : serializer.Deserialize<Dictionary<string, CreatedGroupData>>(groupsJson);

                    ddlSupervisors.Items.Clear();
                    ddlSupervisors.Items.Add(new ListItem("Choose Supervisor...", ""));
                    ddlUnassignedStudents.Items.Clear();
                    ddlUnassignedStudents.Items.Add(new ListItem("Select a student to add...", ""));

                    List<string> assignedStudentIds = new List<string>();
                    foreach (var g in groups.Values)
                    {
                        if (g.students != null)
                        {
                            foreach (var s in g.students.Keys) assignedStudentIds.Add(s);
                        }
                    }

                    foreach (var user in users)
                    {
                        if (user.Value == null) continue;

                        if (user.Value.role == "academicSupervisor" && !string.IsNullOrEmpty(user.Value.academicSupervisorId))
                        {
                            string text = user.Value.fullName + " (" + user.Value.department + ")";
                            ddlSupervisors.Items.Add(new ListItem(text, user.Value.academicSupervisorId));
                        }
                        else if (user.Value.role == "student" && !string.IsNullOrEmpty(user.Value.studentId))
                        {
                            if (!assignedStudentIds.Contains(user.Value.studentId))
                            {
                                string dept = user.Value.majorId ?? user.Value.department ?? "Unknown";
                                ddlUnassignedStudents.Items.Add(new ListItem(user.Value.fullName + " - " + dept, user.Value.studentId));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        protected void rptGroups_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string groupId = e.CommandArgument.ToString();
            if (e.CommandName == "ViewStudents")
            {
                hfSelectedGroupId.Value = groupId;
                lblModalGroupName.Text = groupId;
                LoadDropdowns();
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowView", "var m = new bootstrap.Modal(document.getElementById('viewStudentsModal')); m.show();", true);
            }
            else if (e.CommandName == "AssignSupervisor")
            {
                hfAssignGroupId.Value = groupId;
                txtAssignGroupName.Text = groupId;
                LoadDropdowns();
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowAssign", "var m = new bootstrap.Modal(document.getElementById('assignModal')); m.show();", true);
            }
            else if (e.CommandName == "DeleteGroup")
            {
                hfDeleteGroupId.Value = groupId;
                lblDeleteGroupName.Text = groupId;
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowDel", "var m = new bootstrap.Modal(document.getElementById('deleteGroupModal')); m.show();", true);
            }
        }

        protected void btnCreateGroup_Click(object sender, EventArgs e)
        {
            string groupName = newGroupName.Text.Trim();
            string department = newGroupDept.SelectedValue;
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(department)) return;
            University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(groupName);

            try
            {
                var newGroup = new CreatedGroupData { department = department, createdAt = DateTime.Now.ToString("o"), academicSupervisorId = "" };
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.UploadString(FirebaseUrl + "groups/" + groupName + ".json", "PUT", new JavaScriptSerializer().Serialize(newGroup));
                }
                newGroupName.Text = "";
                newGroupDept.SelectedIndex = 0;
                LoadGroupsFromFirebase();

                ScriptManager.RegisterStartupScript(this, GetType(), "CloseCreate", "var modalEl = document.getElementById('addGroupModal'); var modal = bootstrap.Modal.getInstance(modalEl); if(modal) { modal.hide(); document.body.classList.remove('modal-open'); var backdrop = document.querySelector('.modal-backdrop'); if(backdrop) backdrop.remove(); }", true);
            }
            catch (Exception ex) { lblFirebaseStatus.Text = "Error creating group: " + ex.Message; }
        }

        protected void btnAddStudentToGroup_Click(object sender, EventArgs e)
        {
            string studentId = Request.Form[ddlUnassignedStudents.UniqueID];
            string groupId = hfSelectedGroupId.Value;
            if (!string.IsNullOrEmpty(groupId)) University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(groupId);
            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(groupId)) return;

            try
            {
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    client.UploadString(FirebaseUrl + $"groups/{groupId}/students/{studentId}.json", "PUT", "true");

                    string groupJson = client.DownloadString(FirebaseUrl + $"groups/{groupId}.json");
                    var groupData = serializer.Deserialize<CreatedGroupData>(groupJson);

                    if (groupData != null && !string.IsNullOrEmpty(groupData.academicSupervisorId))
                    {
                        AssignStudentToSupervisorInBackend(client, serializer, studentId, groupData.academicSupervisorId);
                    }
                }
                LoadGroupsFromFirebase();
                LoadDropdowns();

                ScriptManager.RegisterStartupScript(this, GetType(), "CloseView", "var modalEl = document.getElementById('viewStudentsModal'); var modal = bootstrap.Modal.getInstance(modalEl); if(modal) { modal.hide(); document.body.classList.remove('modal-open'); var backdrop = document.querySelector('.modal-backdrop'); if(backdrop) backdrop.remove(); }", true);
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.Text = "Error adding student: " + ex.Message;
                lblFirebaseStatus.CssClass = "alert alert-danger d-block";
            }
        }

        protected void btnConfirmAssign_Click(object sender, EventArgs e)
        {
            string supervisorId = Request.Form[ddlSupervisors.UniqueID];
            string groupId = hfAssignGroupId.Value;
            if (!string.IsNullOrEmpty(groupId)) University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(groupId);
            if (string.IsNullOrEmpty(supervisorId)) return;

            try
            {
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    client.UploadString(FirebaseUrl + $"groups/{groupId}/academicSupervisorId.json", "PUT", $"\"{supervisorId}\"");

                    string groupJson = client.DownloadString(FirebaseUrl + $"groups/{groupId}.json");
                    var groupData = serializer.Deserialize<CreatedGroupData>(groupJson);

                    if (groupData != null && groupData.students != null)
                    {
                        foreach (var studentId in groupData.students.Keys)
                        {
                            AssignStudentToSupervisorInBackend(client, serializer, studentId, supervisorId);
                        }
                    }
                }
                LoadGroupsFromFirebase();
                LoadDropdowns();

                ScriptManager.RegisterStartupScript(this, GetType(), "CloseAssign", "var modalEl = document.getElementById('assignModal'); var modal = bootstrap.Modal.getInstance(modalEl); if(modal) { modal.hide(); document.body.classList.remove('modal-open'); var backdrop = document.querySelector('.modal-backdrop'); if(backdrop) backdrop.remove(); }", true);
            }
            catch (Exception ex)
            {
                lblFirebaseStatus.Text = "Error assigning supervisor: " + ex.Message;
                lblFirebaseStatus.CssClass = "alert alert-danger d-block";
            }
        }

        protected void btnDeleteGroupConfirm_Click(object sender, EventArgs e)
        {
            string groupId = hfDeleteGroupId.Value;
            if (!string.IsNullOrEmpty(groupId)) University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(groupId);
            if (string.IsNullOrEmpty(groupId)) return;

            try
            {
                using (WebClient client = new AuthenticatedFirebaseClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    string groupJson = "";
                    try { groupJson = client.DownloadString(FirebaseUrl + $"groups/{groupId}.json"); } catch { }

                    var groupData = string.IsNullOrEmpty(groupJson) || groupJson == "null" ? null : serializer.Deserialize<CreatedGroupData>(groupJson);

                    if (groupData != null && groupData.students != null)
                    {
                        string usersJson = client.DownloadString(FirebaseUrl + "users.json");
                        var users = serializer.Deserialize<Dictionary<string, UserData>>(usersJson);

                        foreach (var studentId in groupData.students.Keys)
                        {
                            try { client.UploadString(FirebaseUrl + $"supervisorAssignments/ASG-{studentId}.json", "DELETE", ""); } catch { }

                            foreach (var u in users)
                            {
                                if (u.Value.role == "student" && u.Value.studentId == studentId)
                                {
                                    try { client.UploadString(FirebaseUrl + $"users/{u.Key}/academicSupervisorId.json", "DELETE", ""); } catch { }
                                    break;
                                }
                            }
                        }
                    }

                    try { client.UploadString(FirebaseUrl + $"groups/{groupId}.json", "DELETE", ""); } catch { }
                }
                LoadGroupsFromFirebase();
                LoadDropdowns();

                ScriptManager.RegisterStartupScript(this, GetType(), "CloseDel", "var modalEl = document.getElementById('deleteGroupModal'); var modal = bootstrap.Modal.getInstance(modalEl); if(modal) { modal.hide(); document.body.classList.remove('modal-open'); var backdrop = document.querySelector('.modal-backdrop'); if(backdrop) backdrop.remove(); }", true);
            }
            catch (Exception ex) { lblFirebaseStatus.Text = "Error deleting group: " + ex.Message; }
        }

        private void AssignStudentToSupervisorInBackend(WebClient client, JavaScriptSerializer serializer, string studentId, string supervisorId)
        {
            University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(studentId);
            University_Internship_Management_System.Services.FinalEvaluation.FirebaseKey.Require(supervisorId);
            var newAssignment = new AssignmentData
            {
                id = "ASG-" + studentId,
                studentId = studentId,
                academicSupervisorId = supervisorId,
                active = true
            };
            client.UploadString(FirebaseUrl + $"supervisorAssignments/ASG-{studentId}.json", "PUT", serializer.Serialize(newAssignment));
            client.UploadString(FirebaseUrl + $"assignments/ASG-{studentId}.json", "PUT", serializer.Serialize(newAssignment));

            string usersJson = client.DownloadString(FirebaseUrl + "users.json");
            var users = serializer.Deserialize<Dictionary<string, UserData>>(usersJson);
            if (users != null)
            {
                foreach (var u in users)
                {
                    if (u.Value.role == "student" && u.Value.studentId == studentId)
                    {
                        client.UploadString(FirebaseUrl + $"users/{u.Key}/academicSupervisorId.json", "PUT", $"\"{supervisorId}\"");
                        break;
                    }
                }
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Response.Redirect("~/Auth/Login.aspx", false);
        }

        public class UserData { public string fullName { get; set; } public string role { get; set; } public string department { get; set; } public string majorId { get; set; } public string studentId { get; set; } public string academicSupervisorId { get; set; } }
        public class AssignmentData { public string id { get; set; } public string studentId { get; set; } public string academicSupervisorId { get; set; } public bool active { get; set; } }
        public class CreatedGroupData { public string department { get; set; } public string createdAt { get; set; } public string academicSupervisorId { get; set; } public Dictionary<string, bool> students { get; set; } }
        public class GroupRow { public string GroupId { get; set; } public string Department { get; set; } public int StudentCount { get; set; } public string StudentNames { get; set; } public string SupervisorId { get; set; } public string SupervisorName { get; set; } public bool IsAssigned { get; set; } }
    }
}