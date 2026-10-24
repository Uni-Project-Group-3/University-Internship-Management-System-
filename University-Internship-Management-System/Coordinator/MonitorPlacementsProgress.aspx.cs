using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.UI;
using System.Threading.Tasks;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    public partial class MonitorPlacementsProgress : Page
    {
        private readonly University_Internship_Management_System.Shared.StudentFirebaseGateway gateway = new University_Internship_Management_System.Shared.StudentFirebaseGateway();

        protected void Page_Load(object sender, EventArgs e)
        {
            var identity=Context.User?.Identity as FormsIdentity;
            if(identity==null||!identity.IsAuthenticated||FirebaseSignInService.CanonicalRole(identity.Ticket.UserData)!="coordinator") throw new System.Web.HttpException(403,"Coordinator access required.");
            if (!IsPostBack)
            {
                RegisterAsyncTask(new PageAsyncTask(LoadDashboardData));
            }
        }

        private async Task LoadDashboardData()
        {
            try
            {
                using (WebClient client = new WebClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    var gateway = new StudentFirebaseGateway();
                    var identity = Context.User.Identity as FormsIdentity;
                    if (!await gateway.IsActiveAuthenticationUidAsync(identity.Name) || FirebaseSignInService.ProfileResult(identity.Name, await gateway.ReadAsync("users/" + StudentManagementService.Key(identity.Name)) as Newtonsoft.Json.Linq.JObject).Role != "coordinator") throw new UnauthorizedAccessException();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();

                    string usersJson = (await gateway.ReadAsync("users")).ToString(Newtonsoft.Json.Formatting.None);
                    string placementsJson = (await gateway.ReadAsync("placements")).ToString(Newtonsoft.Json.Formatting.None);
                    string companiesJson = (await gateway.ReadAsync("companies")).ToString(Newtonsoft.Json.Formatting.None);
                    string tasksJson = (await gateway.ReadAsync("tasks")).ToString(Newtonsoft.Json.Formatting.None);
                    string attendanceJson = (await gateway.ReadAsync("uimsAttendance/attendance")).ToString(Newtonsoft.Json.Formatting.None);

                    var users = ParseJson<Dictionary<string, UserData>>(serializer, usersJson) ?? new Dictionary<string, UserData>();
                    var placements = ParseJson<Dictionary<string, PlacementData>>(serializer, placementsJson) ?? new Dictionary<string, PlacementData>();
                    var companies = ParseJson<Dictionary<string, CompanyData>>(serializer, companiesJson) ?? new Dictionary<string, CompanyData>();
                    var tasks = ParseJson<Dictionary<string, TaskData>>(serializer, tasksJson) ?? new Dictionary<string, TaskData>();
                    var attendance = ParseJson<Dictionary<string, Dictionary<string, AttendanceRecord>>>(serializer, attendanceJson) ?? new Dictionary<string, Dictionary<string, AttendanceRecord>>();

                    int totalStudentsCount = 0;
                    int activeInternshipsCount = 0;
                    int globalCompletedTasks = 0;
                    int atRiskCount = 0;

                    foreach (var u in users.Values)
                    {
                        if (u != null && u.role == "student") totalStudentsCount++;
                    }

                    foreach (var t in tasks.Values)
                    {
                        if (t != null && t.status == "Completed") globalCompletedTasks++;
                    }

                    List<PlacementRow> tableRows = new List<PlacementRow>();

                    foreach (var placementKvp in placements)
                    {
                        string placementId = placementKvp.Key;
                        var pData = placementKvp.Value;
                        if (pData == null) continue;

                        if (pData.status == "active") activeInternshipsCount++;

                        PlacementRow row = new PlacementRow
                        {
                            StudentId = pData.studentId ?? "N/A",
                            TrainingStatus = pData.status ?? "Pending",
                            RequiredHours = pData.requiredHours,
                            ApprovedHours = 0,
                            TaskPercentage = 0,
                            StudentName = "Unknown",
                            Major = "N/A",
                            Company = "Unknown Company",
                            CompanySupervisor = "Not Assigned",
                            AcademicSupervisor = "Not Assigned"
                        };

                        if (!string.IsNullOrEmpty(pData.companyId) && companies.ContainsKey(pData.companyId))
                        {
                            row.Company = companies[pData.companyId].name;
                        }

                        foreach (var u in users)
                        {
                            if (u.Value == null) continue;

                            if (u.Value.role == "student" && u.Value.studentId == pData.studentId)
                            {
                                row.StudentName = u.Value.fullName;
                                row.Major = u.Value.majorId ?? u.Value.department ?? "N/A";
                            }
                            else if (u.Value.role == "companySupervisor" && u.Key == pData.companySupervisorId)
                            {
                                row.CompanySupervisor = u.Value.fullName;
                            }
                            else if (u.Value.role == "academicSupervisor" && (u.Key == pData.academicSupervisorId || u.Value.academicSupervisorId == pData.academicSupervisorId))
                            {
                                row.AcademicSupervisor = u.Value.fullName;
                            }
                        }

                        int studentTotalTasks = 0;
                        int studentCompletedTasks = 0;
                        foreach (var t in tasks.Values)
                        {
                            if (t != null && t.placementId == placementId)
                            {
                                studentTotalTasks++;
                                if (t.status == "Completed") studentCompletedTasks++;
                            }
                        }
                        if (studentTotalTasks > 0)
                        {
                            row.TaskPercentage = (int)Math.Round((double)studentCompletedTasks / studentTotalTasks * 100);
                        }

                        int totalApprovedMinutes = 0;
                        foreach (var assignment in attendance.Values)
                        {
                            if (assignment == null) continue;
                            foreach (var dayRecord in assignment.Values)
                            {
                                if (dayRecord != null && dayRecord.studentId == pData.studentId && dayRecord.hoursApprovalStatus == "approved")
                                {
                                    totalApprovedMinutes += dayRecord.workedMinutes;
                                }
                            }
                        }
                        row.ApprovedHours = totalApprovedMinutes / 60;

                        if (pData.status == "active" && row.ApprovedHours == 0 && row.TaskPercentage == 0)
                        {
                            atRiskCount++;
                        }

                        tableRows.Add(row);
                    }

                    rptPlacements.DataSource = tableRows;
                    rptPlacements.DataBind();

                    lblTotalStudents.Text = totalStudentsCount.ToString();
                    lblActiveInternships.Text = activeInternshipsCount.ToString();
                    lblCompletedTasks.Text = globalCompletedTasks.ToString();
                    lblRiskInternships.Text = atRiskCount.ToString();

                    lblMessage.Text = "";
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading placement data: " + ex.Message;
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Response.Redirect("~/Auth/Login.aspx", false);
        }

        private string DownloadSafe(WebClient client, string path)
        {
            return System.Threading.Tasks.Task.Run(() => gateway.ReadAsync(path.EndsWith(".json", StringComparison.Ordinal) ? path.Substring(0, path.Length - 5) : path)).GetAwaiter().GetResult().ToString(Newtonsoft.Json.Formatting.None);
        }

        private T ParseJson<T>(JavaScriptSerializer serializer, string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null") return default(T);
            return serializer.Deserialize<T>(json);
        }

        public class UserData { public string fullName { get; set; } public string role { get; set; } public string studentId { get; set; } public string academicSupervisorId { get; set; } public string majorId { get; set; } public string department { get; set; } }
        public class PlacementData { public string studentId { get; set; } public string companyId { get; set; } public string companySupervisorId { get; set; } public string academicSupervisorId { get; set; } public string status { get; set; } public int requiredHours { get; set; } }
        public class CompanyData { public string name { get; set; } }
        public class TaskData { public string studentId { get; set; } public string placementId { get; set; } public string status { get; set; } }
        public class AttendanceRecord { public string studentId { get; set; } public string hoursApprovalStatus { get; set; } public int workedMinutes { get; set; } }

        public class PlacementRow
        {
            public string StudentName { get; set; }
            public string StudentId { get; set; }
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