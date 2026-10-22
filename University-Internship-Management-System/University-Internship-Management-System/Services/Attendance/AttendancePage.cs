using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.SessionState;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: PreApplicationStartMethod(typeof(UIMS.Attendance.AttendanceSessionModule), "Register")]

namespace UIMS.Attendance
{
    // القراءة هنا تخص Session فقط؛ صلاحيات Firebase وقواعد الحفظ تبقى داخل الخدمة.
    public sealed class AttendanceSessionModule : IHttpModule
    {
        public static void Register() { HttpApplication.RegisterModule(typeof(AttendanceSessionModule)); }
        public void Init(HttpApplication application)
        {
            application.PostMapRequestHandler += (sender, args) =>
            {
                var context = ((HttpApplication)sender).Context;
                if ((!(context.Handler is University_Internship_Management_System.Student.MyAttendance) &&
                    !(context.Handler is University_Internship_Management_System.Company.VerifyAttendance)) || context.Request.HttpMethod != "POST" ||
                    context.Request.QueryString["attendanceApi"] != "1") return;
                string action = context.Request.Form["action"];
                if (action == "load" || action == "qr" || action == "scan" || action == "requestEarly" || action == "review" || action == "issueQr")
                    context.SetSessionStateBehavior(SessionStateBehavior.ReadOnly);
                // identity وnewTest يحتاجان تعديل Session، فيبقى لهما القفل الافتراضي.
            };
        }
        public void Dispose() { }
    }

    // مساعد مشترك للحماية وردود JSON؛ الصفحتان ترثان Page مباشرة.
    public sealed class AttendancePage
    {
        private readonly Page page;
        private HttpRequest Request { get { return page.Request; } }
        private HttpResponse Response { get { return page.Response; } }
        private HttpSessionState Session { get { return page.Session; } }
        private TraceContext Trace { get { return page.Trace; } }
        public string AttendanceCsrfToken { get; private set; }
        public bool LocalDemoAllowed { get; private set; }
        public bool IsApiRequest { get { return isApi; } }
        public string Action { get { return Request.Form["action"] ?? ""; } }
        private string jsonResponse;
        private bool isApi;
        private bool streaming;
        private readonly System.Threading.SemaphoreSlim streamLock = new System.Threading.SemaphoreSlim(1, 1);

        public AttendancePage(Page page) { this.page = page; }

        public void Initialize()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["X-Frame-Options"] = "DENY";
            // وضع التطوير محصور بنفس الجهاز، ولا يمكن تفعيله من رابط أو حقل بالمتصفح.
            LocalDemoAllowed = (AttendanceFirebase.Setting("UIMS_ATTENDANCE_LOCAL_TEST") == "true" ||
                AttendanceFirebase.Setting("UIMS_ATTENDANCE_LOCAL_DEMO") == "true") &&
                Request.IsLocal && Request.Url.IsLoopback && string.IsNullOrEmpty(Request.Headers["X-Forwarded-For"]);
            isApi = Request.QueryString["attendanceApi"] == "1";
            if (Session["Attendance.Csrf"] == null && !isApi)
            {
                byte[] bytes = new byte[32];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                Session["Attendance.Csrf"] = Convert.ToBase64String(bytes);
            }
            AttendanceCsrfToken = (string)Session["Attendance.Csrf"];
        }

        // طلب تحميل واحد، ورسائل JSON صغيرة؛ لا نفتح طلبات Firebase إضافية للتدرج.
        public async Task SendProgress(string type, JObject data)
        {
            await streamLock.WaitAsync();
            try
            {
                if (!Response.IsClientConnected) throw new TaskCanceledException();
                Response.Write(JsonConvert.SerializeObject(new { type = type, data = data }) + "\n");
                Response.Flush();
            }
            finally { streamLock.Release(); }
        }

        // الهوية ترسل بعد تحقق السيرفر؛ بقية البيانات تبقى غير مكتملة حتى الرد النهائي.
        public async Task StartLoad(string role, string id, JObject actor)
        {
            streaming = Action == "load" && Request.Form["stream"] == "1";
            if (!streaming) return;
            Response.ContentType = "application/x-ndjson";
            Response.BufferOutput = false;
            if (LocalDemoAllowed) Response.Headers["X-Attendance-Session"] = Session.IsReadOnly ? "read-only" : "write";
            await SendProgress("identity", new JObject
            {
                [role == "student" ? "student" : "supervisor"] = actor,
                ["identityId"] = id, ["localDemo"] = LocalDemoAllowed,
                ["demoSessions"] = (Session["Attendance.DemoSessions"] as JArray ?? new JArray()).DeepClone()
            });
        }

        public bool IsStreaming { get { return streaming; } }

        public JObject CompleteData(JObject data, string id)
        {
            data["localDemo"] = LocalDemoAllowed;
            data["identityId"] = id;
            if (LocalDemoAllowed) data["demoSessions"] = (Session["Attendance.DemoSessions"] as JArray ?? new JArray()).DeepClone();
            // لا نغير توقيت إصدار QR حتى تبقى صلاحيته 120 ثانية.
            if (data["serverNow"] == null) data["serverNow"] = AttendanceService.Now();
            return data;
        }

        public void RememberDemoSession(JObject session)
        {
            var sessions = Session["Attendance.DemoSessions"] as JArray ?? new JArray();
            sessions.Add(session);
            var studentIds = session["studentIds"] as JArray;
            if (studentIds != null && studentIds.Count > 1)
            {
                var second = (JObject)session.DeepClone();
                second["studentId"] = studentIds[1];
                second["assignmentId"] = session["secondAssignmentId"];
                second["label"] = (string)session["label"] + " Student 2";
                sessions.Add(second);
            }
            Session["Attendance.DemoSessions"] = sessions;
            Session["Attendance.Local.student"] = (string)session["studentId"];
            Session["Attendance.Local.company"] = (string)session["supervisorId"];
        }

        public async Task Execute(Func<Task<JObject>> handleAction)
        {
            var timing = LocalDemoAllowed ? new AttendanceTiming() : null;
            AttendanceTiming.Current.Value = timing;
            try
            {
                if (Request.HttpMethod != "POST" || string.IsNullOrEmpty(AttendanceCsrfToken) || Request.Form["csrf"] != AttendanceCsrfToken)
                {
                    Response.StatusCode = 403;
                    throw new InvalidOperationException("This request is not authorized. Reload the attendance page.");
                }
                string origin = Request.Headers["Origin"];
                if (!string.IsNullOrEmpty(origin) && origin != Request.Url.GetLeftPart(UriPartial.Authority))
                {
                    Response.StatusCode = 403;
                    throw new InvalidOperationException("Cross-origin attendance requests are not allowed.");
                }
                string action = Request.Form["action"] ?? "";
                if (action == "identity" && !LocalDemoAllowed)
                    throw new InvalidOperationException("Identity switching is available in localhost demo mode only.");
                JObject data = await handleAction();
                jsonResponse = JsonConvert.SerializeObject(new { success = true, data = data });
            }
            catch (InvalidOperationException error) { WriteError(error.Message, 400); }
            catch (HttpRequestException) { WriteError("Firebase could not complete the request. Check server access and permissions. No successful save was confirmed.", 502); }
            catch (TaskCanceledException) { WriteError("Firebase timed out. Refresh to check the stored state before retrying.", 504); }
            catch (Exception error)
            {
                Trace.Warn("Attendance", "Attendance request failed: " + error.GetType().Name);
                WriteError("Attendance request failed. No successful operation was confirmed. Check the attendance server setup.", 500);
            }
            finally
            {
                if (LocalDemoAllowed)
                {
                    if (streaming)
                    {
                        // الهيدر أرسل مع أول دفعة، فنضع قياسات الطلب النهائية في آخر رسالة محلية.
                        var result = JObject.Parse(jsonResponse ?? "{\"success\":false}");
                        result["timing"] = new JObject { ["serverTiming"] = timing.Header(), ["requests"] = JObject.Parse(timing.Details()) };
                        jsonResponse = result.ToString(Formatting.None);
                    }
                    else
                    {
                        Response.Headers["Server-Timing"] = timing.Header();
                        Response.Headers["X-Attendance-Requests"] = timing.Details();
                        Response.Headers["X-Attendance-Session"] = Session.IsReadOnly ? "read-only" : "write";
                    }
                }
                AttendanceTiming.Current.Value = null;
            }
        }

        public string ResolveIdentity(string role, string action)
        {
            if (LocalDemoAllowed)
            {
                // إعداد تجربة ثابت من السيرفر، وليس تسجيل دخول أو معرّفًا من المتصفح.
                string testFile = AttendanceFirebase.Setting("UIMS_ATTENDANCE_TEST_SESSION_FILE");
                if (!string.IsNullOrWhiteSpace(testFile))
                {
                    if (action == "identity") throw new InvalidOperationException("Local test identity is configured on the server.");
                    var test = JObject.Parse(File.ReadAllText(testFile));
                    string configuredId = (string)test[role == "student" ? "studentId" : "supervisorId"];
                    AttendanceFirebase.CheckKey(configuredId);
                    return configuredId;
                }
                string key = "Attendance.Local." + role;
                if (action == "identity")
                {
                    string chosen = Request.Form["identityId"];
                    string[] permitted = role == "student"
                        ? new[] { "att_demo_student_a", "att_demo_student_b", "att_demo_student_unassigned", "att_demo_student_live_20261021_01" }
                        : new[] { "att_demo_supervisor_a", "att_demo_supervisor_b" };
                    bool allowed = Array.IndexOf(permitted, chosen) >= 0;
                    var sessions = Session["Attendance.DemoSessions"] as JArray;
                    if (sessions != null)
                        foreach (JObject session in sessions)
                            if ((string)session[role == "student" ? "studentId" : "supervisorId"] == chosen)
                            {
                                allowed = true;
                                // اختيار طالب تجربة سابقة يختار مشرفها في التبويب الآخر بعد تحديثه.
                                if (role == "student") Session["Attendance.Local.company"] = (string)session["supervisorId"];
                            }
                    if (!allowed) throw new InvalidOperationException("Invalid local test identity.");
                    Session[key] = chosen;
                }
                return Session[key] as string ?? (role == "student" ? "att_demo_student_a" : "att_demo_supervisor_a");
            }
            // هذه القيم يضعها زميل المصادقة بعد التحقق الحقيقي على السيرفر، وليس من المتصفح.
            string uid = Session["Uims.VerifiedFirebaseUid"] as string;
            if (Session["Uims.PasswordChangeRequired"] as bool? == true)
                throw new InvalidOperationException("Complete the required password change before using attendance.");
            string loggedRole = Session["Uims.Role"] as string;
            string id = Session[role == "student" ? "Attendance.StudentId" : "Attendance.SupervisorId"] as string;
            if (string.IsNullOrWhiteSpace(uid) || !string.Equals(loggedRole, role, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Verified sign-in is required. For localhost test records only, configure UIMS_ATTENDANCE_LOCAL_DEMO on the server.");
            return id;
        }

        private void WriteError(string message, int status)
        {
            if (!streaming && Response.StatusCode < 400) Response.StatusCode = status;
            Response.TrySkipIisCustomErrors = true;
            jsonResponse = JsonConvert.SerializeObject(new { success = false, message = message });
        }

        public void WriteResponse()
        {
            if (!streaming) Response.ContentType = "application/json";
            Response.Write((jsonResponse ?? "{\"success\":false,\"message\":\"Attendance request was not completed.\"}") + (streaming ? "\n" : ""));
        }
    }
}
