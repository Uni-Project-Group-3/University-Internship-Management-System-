using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using UIMS.Attendance;

namespace University_Internship_Management_System.Student
{
    public partial class MyAttendance : Page
    {
        private readonly AttendanceService attendanceService = new AttendanceService();
        private AttendancePage attendanceRequest;
        protected string AttendanceCsrfToken { get { return attendanceRequest.AttendanceCsrfToken; } }
        protected bool LocalDemoAllowed { get { return attendanceRequest.LocalDemoAllowed; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            attendanceRequest = new AttendancePage(this);
            attendanceRequest.Initialize();
            // طلب JavaScript يعالج في C# دون إعادة إرسال واجهة الصفحة كاملة.
            if (attendanceRequest.IsApiRequest)
                RegisterAsyncTask(new PageAsyncTask(() => attendanceRequest.Execute(HandleStudentAction)));
        }

        private async Task<JObject> HandleStudentAction()
        {
            using (attendanceService.ReuseRequestReads())
            {
                string action = attendanceRequest.Action;

                string studentId = await attendanceRequest.ResolveIdentityAsync("student", action);
                string uid = attendanceRequest.VerifiedUid ?? "";
                var student = await attendanceService.Identity("student", studentId, uid, LocalDemoAllowed,
                    action == "load" ? attendanceRequest.VerifiedProfile : null);
                await attendanceRequest.StartLoad("student", studentId, student);

                JObject data;
                switch (action)
                {
                    case "qr":
                        data = await LoadIssuedQr(studentId);
                        break;
                    case "requestEarly":
                        await RequestEarlyDeparture(studentId);
                        data = await LoadAttendance(studentId, student);
                        break;
                    case "load":
                        data = await LoadAttendance(studentId, student);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown student attendance action.");
                }
                return attendanceRequest.CompleteData(data, studentId);
            }
        }

        private Task<JObject> LoadAttendance(string studentId, JObject student)
        {
            Func<JObject, Task> profileReady = attendanceRequest.IsStreaming
                ? (Func<JObject, Task>)(profile => attendanceRequest.SendProgress("profile", profile)) : null;
            return LoadAttendanceData(attendanceService, studentId, student, profileReady);
        }

        // تجهيز حقول العرض هنا؛ التحقق والحسابات تبقى داخل خدمة الحضور.
        public static async Task<JObject> LoadAttendanceData(AttendanceService service, string studentId,
            JObject student = null, Func<JObject, Task> profileReady = null)
        {
            var state = await service.LoadStudentAttendance(studentId, student, profileReady);
            if (string.IsNullOrEmpty(state.AssignmentId))
                return new JObject { ["student"] = state.Student, ["hasAssignment"] = false,
                    ["today"] = state.Date, ["history"] = new JArray() };

            return new JObject
            {
                ["student"] = state.Student, ["hasAssignment"] = true, ["assignment"] = state.Assignment,
                ["company"] = state.Company, ["supervisor"] = state.Supervisor, ["today"] = state.Date,
                ["schedule"] = state.Schedule, ["record"] = AttendanceService.ForDisplay(state.Record), ["earlyRequest"] = state.Request,
                ["history"] = new JArray(state.AttendanceHistory.Properties().Where(p => p.Value is JObject)
                    .OrderByDescending(p => p.Name).Select(p => p.Value)),
                ["approvedMinutes"] = state.ApprovedMinutes, ["canCheckIn"] = state.CanCheckIn,
                ["canCheckOut"] = state.CanCheckOut, ["canRequestEarly"] = state.CanRequestEarly,
                ["actionsEnabled"] = AttendanceService.Flag(state.Settings, "enabled")
            };
        }

        private Task<JObject> LoadIssuedQr(string studentId)
        {
            return attendanceService.ReadIssuedQr(studentId, Request.Form["attendanceAction"], LocalDemoAllowed);
        }

        private async Task RequestEarlyDeparture(string studentId)
        {
            await attendanceService.CheckLocalStudent(studentId, null, LocalDemoAllowed);
            await attendanceService.RequestEarly(studentId, Request.Form["reason"]);
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (attendanceRequest != null && attendanceRequest.IsApiRequest) attendanceRequest.WriteResponse();
            else base.Render(writer);
        }
    }
}
