using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using UIMS.Attendance;

namespace University_Internship_Management_System.Company
{
    public partial class VerifyAttendance : Page
    {
        private readonly AttendanceService attendanceService = new AttendanceService();
        private AttendancePage attendanceRequest;
        protected string AttendanceCsrfToken { get { return attendanceRequest.AttendanceCsrfToken; } }
        protected bool LocalDemoAllowed { get { return attendanceRequest.LocalDemoAllowed; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            attendanceRequest = new AttendancePage(this);
            attendanceRequest.Initialize();
            if (attendanceRequest.IsApiRequest)
                RegisterAsyncTask(new PageAsyncTask(() => attendanceRequest.Execute(HandleSupervisorAction)));
        }

        private async Task<JObject> HandleSupervisorAction()
        {
            using (attendanceService.ReuseRequestReads())
            {
                string action = attendanceRequest.Action;
                if (action == "newTest")
                    throw new InvalidOperationException("Local test setup requires the local student page.");

                string supervisorId = await attendanceRequest.ResolveIdentityAsync("company", action);
                string uid = attendanceRequest.VerifiedUid ?? "";
                var supervisor = await attendanceService.Identity("company", supervisorId, uid, LocalDemoAllowed);
                await attendanceRequest.StartLoad("company", supervisorId, supervisor);

                JObject data;
                switch (action)
                {
                    case "issueQr":
                        data = await IssueSelectedQr(supervisorId);
                        break;
                    case "scan":
                        data = await ScanAttendance(supervisorId);
                        break;
                    case "review":
                        await ReviewAttendance(supervisorId);
                        data = await LoadAttendance(supervisorId, supervisor);
                        break;
                    case "load":
                    case "identity":
                        data = await LoadAttendance(supervisorId, supervisor);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown supervisor attendance action.");
                }
                return attendanceRequest.CompleteData(data, supervisorId);
            }
        }

        private Task<JObject> LoadAttendance(string supervisorId, JObject supervisor)
        {
            Func<JObject, Task> studentReady = attendanceRequest.IsStreaming
                ? (Func<JObject, Task>)(student => attendanceRequest.SendProgress("student", student)) : null;
            return LoadAttendanceData(attendanceService, supervisorId, supervisor, studentReady);
        }

        // الخدمة ترجع بيانات المتدربين المسموحين فقط؛ الصفحة ترتب رد العرض.
        public static async Task<JObject> LoadAttendanceData(AttendanceService service, string supervisorId,
            JObject supervisor = null, Func<JObject, Task> studentReady = null)
        {
            supervisor = supervisor ?? await service.LoadSupervisor(supervisorId);
            var groups = await service.LoadSupervisorAttendance(supervisorId, supervisor, studentReady);
            return new JObject { ["supervisor"] = supervisor, ["today"] = AttendanceService.Today(),
                ["students"] = groups[0], ["requests"] = groups[1], ["records"] = groups[2], ["setupWarnings"] = groups.Length > 3 ? groups[3] : new JArray() };
        }

        private Task<JObject> ScanAttendance(string supervisorId)
        {
            // فحص الرمز والدخول والخروج والحفظ الآمن تنفذها الخدمة المشتركة.
            return attendanceService.ScanIssuedQr(supervisorId, Request.Form["token"], LocalDemoAllowed);
        }

        private async Task ReviewAttendance(string supervisorId)
        {
            if (LocalDemoAllowed)
            {
                var selected = await attendanceService.LoadSupervisorAttendance(supervisorId, null, null);
                var student = selected[0].OfType<JObject>().FirstOrDefault(row =>
                    (string)row["assignmentId"] == Request.Form["assignmentId"]);
                if (student == null) throw new InvalidOperationException("Choose one of your test students.");
                await attendanceService.CheckLocalStudent((string)student["id"], supervisorId, true);
            }
            await attendanceService.Review(supervisorId, Request.Form["assignmentId"],
                Request.Form["date"], Request.Form["decision"]);
        }

        private Task<JObject> IssueSelectedQr(string supervisorId)
        {
            JArray students;
            try { students = JArray.Parse(Request.Form["studentIds"] ?? "[]"); }
            catch (Newtonsoft.Json.JsonException) { throw new InvalidOperationException("Invalid student selection."); }
            return attendanceService.IssueStudentQrs(supervisorId, students,
                Request.Form["attendanceAction"], LocalDemoAllowed, attendanceRequest.VerifiedUid ?? "");
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (attendanceRequest != null && attendanceRequest.IsApiRequest) attendanceRequest.WriteResponse();
            else base.Render(writer);
        }
    }
}
