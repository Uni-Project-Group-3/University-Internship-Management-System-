using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json.Linq;

namespace UIMS.Attendance
{
    public partial class AttendanceService
    {
        private readonly AttendanceFirebase database;
        private readonly Func<byte[]> qrSigningKey;
        private readonly AttendanceConfiguration configuration;
        public AttendanceService() : this(new AttendanceFirebase()) { }
        // حقن الاتصال يسمح باختبار القواعد دون الوصول إلى Firebase الحقيقي.
        public AttendanceService(AttendanceFirebase database) : this(database, LoadQrSigningKey) { }
        public AttendanceService(AttendanceFirebase database, Func<byte[]> qrSigningKey) : this(database, qrSigningKey, new AttendanceConfiguration()) { }
        public AttendanceService(AttendanceFirebase database, Func<byte[]> qrSigningKey, AttendanceConfiguration configuration)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.qrSigningKey = qrSigningKey ?? throw new ArgumentNullException(nameof(qrSigningKey));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private static byte[] LoadQrSigningKey()
        {
            string path = System.Configuration.ConfigurationManager.AppSettings["AttendanceQrSigningKeyFile"] ??
                AttendanceFirebase.Setting("UIMS_ATTENDANCE_QR_SIGNING_KEY_FILE");
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.Path.IsPathRooted(path))
                    throw new InvalidOperationException();
                string root = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath ?? AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = System.IO.Path.GetFullPath(path);
                if (fullPath.StartsWith(System.IO.Path.GetFullPath(root).TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException();
                byte[] key = System.IO.File.ReadAllBytes(fullPath);
                if (key.Length != 32) throw new InvalidOperationException();
                return key;
            }
            catch (Exception error) when (error is InvalidOperationException || error is System.IO.IOException ||
                error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException)
            {
                throw new UIMS.Services.FirebaseException(503, "Configure the attendance QR signing key outside the website before issuing QR codes.");
            }
        }

        public static JObject ForDisplay(JObject record)
        {
            var result = record == null ? new JObject() : (JObject)record.DeepClone();
            result.Remove("qrState");
            result.Remove("resetAttempts");
            return result;
        }
        public static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        public static string Text(JToken value, string field)
        {
            var item = value is JObject ? value[field] : null;
            return item != null && item.Type == JTokenType.String ? (string)item : "";
        }
        public static long Number(JToken value, string field)
        {
            var item = value is JObject ? value[field] : null;
            long number;
            return item is JValue && long.TryParse(item.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ? number : 0;
        }
        public static bool Flag(JToken value, string field)
        {
            var item = value is JObject ? value[field] : null;
            return item != null && item.Type == JTokenType.Boolean && (bool)item;
        }
        public static string Today()
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private Dictionary<string, Task<JObject>> requestReads;

        // إعادة استخدام داخل طلب HTTP واحد فقط؛ لا تبقى بيانات أو صلاحيات مخزنة بين الطلبات.
        public IDisposable ReuseRequestReads()
        {
            if (requestReads != null) throw new InvalidOperationException("Attendance read scope is already active.");
            requestReads = new Dictionary<string, Task<JObject>>();
            return new ReadScope(() => requestReads = null);
        }
        private sealed class ReadScope : IDisposable
        {
            private readonly Action release;
            internal ReadScope(Action release) { this.release = release; }
            public void Dispose() { release(); }
        }
        private async Task<JObject> ReadData(string path) { return (await database.Read(path)).Data; }
        private async Task<JObject> Get(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("An attendance database path is required.");
            foreach (string key in path.Split('/')) AttendanceFirebase.CheckKey(key);
            if (requestReads == null) return await ReadData(path);
            Task<JObject> read;
            lock (requestReads)
            {
                if (!requestReads.TryGetValue(path, out read)) requestReads[path] = read = ReadData(path);
            }
            // التاريخ يضيف حالات غياب للعرض؛ لا نسمح له بتغيير نسخة القراءة المشتركة.
            return (JObject)(await read).DeepClone();
        }
        private async Task<bool> Save(string path, JObject data, string etag)
        {
            bool saved = await database.SaveIfUnchanged(path, data, etag);
            if (saved && requestReads != null)
                lock (requestReads)
                    foreach (string key in requestReads.Keys.Where(key => key == path ||
                        key.StartsWith(path + "/", StringComparison.Ordinal) || path.StartsWith(key + "/", StringComparison.Ordinal)).ToList())
                        requestReads.Remove(key);
            return saved;
        }

        public Task<JObject> CreateLocalDemoSession(string scenario, bool localDemo)
        {
            throw new UIMS.Services.FirebaseException(501, "New sample identities are disabled. Use the existing attendance records in Firebase.");
        }

        public Task<JObject> Identity(string role, string id, string verifiedUid, bool localDemo)
        {
            return Identity(role, id, verifiedUid, localDemo, null);
        }

        public async Task<JObject> Identity(string role, string id, string verifiedUid, bool localDemo, JObject verifiedProfile)
        {
            if (role != "student" && role != "company") throw new InvalidOperationException("Invalid attendance role.");
            AttendanceFirebase.CheckKey(id);
            var actor = await Get((role == "student" ? "students/" : "companySupervisors/") + id);
            if (!actor.HasValues && role == "student" && !localDemo && !string.IsNullOrWhiteSpace(verifiedUid) &&
                verifiedProfile != null && Text(verifiedProfile, "studentId") == id &&
                University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(verifiedProfile, "role")) == "student" &&
                Flag(verifiedProfile, "isActive") && !Flag(verifiedProfile, "mustChangePassword") &&
                (Text(verifiedProfile, "accountSetupStatus") == "" || Text(verifiedProfile, "accountSetupStatus") == "complete"))
            {
                // عرض حساب الطالب الموثق فقط؛ لا ننشئ تكليفًا أو سجل حضور أو نعطي صلاحية مسح.
                return new JObject { ["name"] = Text(verifiedProfile, "fullName"), ["studentNumber"] = id,
                    ["firebaseUid"] = verifiedUid, ["status"] = "active", ["attendanceSetupRequired"] = true };
            }
            if (!actor.HasValues || Text(actor, "status") != "active")
                throw new InvalidOperationException("This attendance identity is missing or inactive.");
            if (localDemo && !Flag(actor, "isTestData"))
                throw new InvalidOperationException("Local testing only permits labelled test records.");
            if (!localDemo)
            {
                if (string.IsNullOrWhiteSpace(verifiedUid))
                    throw new UIMS.Services.FirebaseException(401, "A verified Firebase login is required for attendance.");
                // لا نصحح UID تلقائيًا؛ اختلاف الربط يحتاج مراجعة المسؤول دون كشف بيانات الحساب.
                if (Text(actor, "firebaseUid") != verifiedUid)
                    throw new UIMS.Services.FirebaseException(403, "Your Firebase login is verified, but its UID does not match the attendance record. Ask the project administrator to review the attendance UID mapping. No data was changed.");
                if (Flag(actor, "isTestData") && !configuration.AuthenticatedWritesEnabled && !configuration.IsAuthorizedIntegrationActor(role, id, verifiedUid))
                    throw new UIMS.Services.FirebaseException(403, "This test attendance identity is not approved for the verified Firebase login.");
            }
            return actor;
        }

        private async Task<AttendanceContext> Context(string studentId, string supervisorId, JObject loadedStudent = null, bool loadHistory = false, bool readRecord = true, bool readRequest = true, Func<JObject, Task> profileReady = null)
        {
            AttendanceFirebase.CheckKey(studentId);
            var c = new AttendanceContext { Student = loadedStudent ?? await Get("students/" + studentId), Date = Today() };
            if (Text(c.Student, "status") != "active") throw new InvalidOperationException("Student account is inactive or missing.");
            c.AssignmentId = Text(c.Student, "activeAssignmentId");
            if (c.AssignmentId == "") throw new InvalidOperationException("No active training assignment is linked to this student.");
            AttendanceFirebase.CheckKey(c.AssignmentId);
            // للعرض فقط: المعرّف جاء من طالب تحقق منه السيرفر. لا نرسل أي بيانات تكليف قبل فحص ملكيته.
            // هذه نفس القراءات، لكن نبدأ المستقل منها دون انتظار رد التكليف.
            // التكليف على المسار الحرج للشركة والمشرف، لذلك نبدأه أولاً.
            var assignmentRead = Get("trainingAssignments/" + c.AssignmentId);
            var earlySchedule = loadHistory ? Get("schedules/" + c.AssignmentId) : null;
            var earlyRecords = loadHistory ? Get("attendance/" + c.AssignmentId) : null;
            var earlyRequest = loadHistory ? Get("earlyCheckoutRequests/" + c.AssignmentId + "/" + c.Date) : null;
            var earlySettings = loadHistory ? Get("settings") : null;
            try
            {
                c.Assignment = await assignmentRead;
                if (AttendanceConfiguration.IsLocalDevelopment(HttpContext.Current) && !Flag(c.Assignment, "isTestData"))
                    throw new UIMS.Services.FirebaseException(403, "Local attendance testing only permits labelled test assignments.");
                if (Text(c.Assignment, "status") != "active" || Text(c.Assignment, "studentId") != studentId)
                    throw new InvalidOperationException("The student's training assignment is not active or does not match.");
                string companyId = Text(c.Assignment, "companyId");
                string assignedSupervisor = Text(c.Assignment, "companySupervisorId");
                AttendanceFirebase.CheckKey(companyId);
                AttendanceFirebase.CheckKey(assignedSupervisor);
                if (supervisorId != null && supervisorId != assignedSupervisor)
                    throw new InvalidOperationException("You are not authorized to supervise this student's assignment.");
                // القراءات المستقلة تعمل معًا لتقليل انتظار الصفحة، دون تغيير ترتيب الكتابة.
                var companyRead = Get("companies/" + companyId);
                var supervisorRead = Get("companySupervisors/" + assignedSupervisor);
                // العرض يقرأ التاريخ مرة واحدة ويستخرج يوم الحضور منه؛ عمليات الحفظ تبقى بقراءات حديثة منفصلة.
                var scheduleRead = earlySchedule ?? Get("schedules/" + c.AssignmentId + "/" + c.Date);
                var recordRead = earlyRecords ?? (readRecord ? Get("attendance/" + c.AssignmentId + "/" + c.Date) : Task.FromResult(new JObject()));
                var requestRead = earlyRequest ?? (readRequest ? Get("earlyCheckoutRequests/" + c.AssignmentId + "/" + c.Date) : Task.FromResult(new JObject()));
                var settingsRead = earlySettings ?? Task.FromResult(new JObject());
                await Task.WhenAll(companyRead, supervisorRead);
                c.Company = await companyRead;
                c.Supervisor = await supervisorRead;
                if (Text(c.Company, "status") != "active" || Text(c.Supervisor, "status") != "active" || Text(c.Supervisor, "companyId") != companyId)
                    throw new InvalidOperationException("The assigned company or supervisor is inactive or inconsistent.");
                if (Number(c.Assignment, "requiredHours") <= 0)
                    throw new InvalidOperationException("The assignment must specify its required training hours.");
                if (loadHistory && profileReady != null)
                    await profileReady(new JObject
                    {
                        ["student"] = c.Student, ["hasAssignment"] = true, ["assignment"] = c.Assignment,
                        ["company"] = c.Company, ["supervisor"] = c.Supervisor, ["today"] = c.Date
                    });
                await Task.WhenAll(scheduleRead, recordRead, requestRead, settingsRead);
                var schedules = await scheduleRead;
                var records = await recordRead;
                c.Schedule = loadHistory ? (JObject)(schedules[c.Date] as JObject ?? new JObject()).DeepClone() : schedules;
                c.Record = loadHistory ? (JObject)(records[c.Date] as JObject ?? new JObject()).DeepClone() : records;
                if (loadHistory)
                {
                    c.HistoryRecords = records;
                    c.HistorySchedules = schedules;
                    c.Settings = await settingsRead;
                }
                c.Request = await requestRead;
                return c;
            }
            finally
            {
                if (loadHistory)
                {
                    // لا نترك أخطاء مهام مستقلة غير مراقبة عند فشل التحقق من التكليف.
                    try { await Task.WhenAll(earlySchedule, earlyRecords, earlyRequest, earlySettings); }
                    catch { /* الخطأ الأصلي يعالج في الصفحة؛ لا نرسل بيانات جزئية غير متحققة. */ }
                }
            }
        }

        public static void CheckSchedule(AttendanceContext c)
        {
            if (c == null || c.Schedule == null || c.Assignment == null)
                throw new InvalidOperationException("The training assignment or schedule is incomplete.");
            if (c.Date != Today() || !c.Schedule.HasValues || !Flag(c.Schedule, "isTrainingDay"))
                throw new InvalidOperationException("No scheduled training shift is available today.");
            if (Text(c.Schedule, "assignmentId") != c.AssignmentId || Text(c.Schedule, "attendanceDate") != c.Date ||
                Text(c.Schedule, "timeZone") != "Asia/Amman" || Number(c.Schedule, "version") <= 0 ||
                Number(c.Schedule, "scheduledStartAt") <= 0 || Number(c.Schedule, "scheduledEndAt") <= Number(c.Schedule, "scheduledStartAt"))
                throw new InvalidOperationException("The training schedule is incomplete or inconsistent.");
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
                foreach (string field in new[] { "scheduledStartAt", "scheduledEndAt" })
                    if (TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(Number(c.Schedule, field)), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != c.Date)
                        throw new InvalidOperationException("The schedule timestamps do not belong to its Asia/Amman attendance date.");
            }
            catch (ArgumentOutOfRangeException) { throw new InvalidOperationException("The schedule timestamps are invalid."); }
            string start = Text(c.Assignment, "startDate"), end = Text(c.Assignment, "endDate");
            DateTime startDate, endDate;
            if (!DateTime.TryParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate) ||
                !DateTime.TryParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate) || startDate > endDate)
                throw new InvalidOperationException("The assignment training dates are missing or invalid.");
            if (string.CompareOrdinal(c.Date, start) < 0 || string.CompareOrdinal(c.Date, end) > 0)
                throw new InvalidOperationException("The training assignment is outside its permitted date range.");
        }

        private static void CheckRecord(AttendanceContext c)
        {
            if (c == null || c.Record == null || c.Assignment == null)
                throw new InvalidOperationException("The attendance context is incomplete.");
            if (c.Record.HasValues && (Text(c.Record, "assignmentId") != c.AssignmentId ||
                Text(c.Record, "studentId") != Text(c.Assignment, "studentId") || Text(c.Record, "attendanceDate") != c.Date ||
                Text(c.Record, "companyId") != Text(c.Assignment, "companyId") ||
                Text(c.Record, "companySupervisorId") != Text(c.Assignment, "companySupervisorId")))
                throw new InvalidOperationException("The stored attendance record does not match the current assignment. No data was overwritten.");
            foreach (string field in new[] { "checkInAt", "checkOutAt" })
            {
                var value = c.Record[field];
                long timestamp;
                if (value != null && value.Type != JTokenType.Null &&
                    ((value.Type != JTokenType.Integer && value.Type != JTokenType.String) ||
                     !long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out timestamp)))
                    throw new InvalidOperationException("The stored attendance times are invalid. No data was overwritten.");
            }
            long checkIn = Number(c.Record, "checkInAt"), checkOut = Number(c.Record, "checkOutAt");
            if (checkIn < 0 || checkOut < 0 || checkIn > Now() || checkOut > Now() ||
                checkOut > 0 && (checkIn <= 0 || checkOut <= checkIn))
                throw new InvalidOperationException("The stored attendance times are inconsistent. No data was overwritten.");
        }

        public static void CheckAction(AttendanceContext c, string action, long now)
        {
            CheckFlexibleAttendanceDate(c);
            CheckRecord(c);
            if (action == "checkIn")
            {
                if (Number(c.Record, "checkInAt") > 0 || Number(c.Record, "checkOutAt") > 0)
                    throw new InvalidOperationException("Attendance is already recorded or requires review.");
                // المشرف يحدد وقت الحضور الفعلي؛ يوم التدريب والتكليف ما زالا مطلوبين.
                return;
            }
            if (action != "checkOut") throw new InvalidOperationException("Invalid attendance action.");
            if (Number(c.Record, "checkInAt") <= 0 || Number(c.Record, "checkOutAt") > 0)
                throw new InvalidOperationException("Check-out requires an existing check-in and cannot be repeated.");
            if (now <= Number(c.Record, "checkInAt")) throw new InvalidOperationException("Check-out must be after check-in.");
            // إصدار رمز الخروج ومسحه من المشرف يغنيان عن طلب مغادرة مبكرة منفصل.
        }

        // Supervisor-issued QR attendance is available on every date within the
        // approved training period. Shift/day-off settings do not authorize actors.
        public static void CheckFlexibleAttendanceDate(AttendanceContext c)
        {
            if (c == null || c.Assignment == null || c.Schedule == null || c.Date != Today() ||
                Text(c.Schedule, "assignmentId") != c.AssignmentId || Text(c.Schedule, "attendanceDate") != c.Date ||
                Text(c.Schedule, "timeZone") != "Asia/Amman" || Number(c.Schedule, "version") <= 0)
                throw new InvalidOperationException("A verified attendance date and assignment are required.");
            DateTime start, end;
            if (!DateTime.TryParseExact(Text(c.Assignment,"startDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start) ||
                !DateTime.TryParseExact(Text(c.Assignment,"endDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out end) || start > end ||
                string.CompareOrdinal(c.Date, start.ToString("yyyy-MM-dd")) < 0 || string.CompareOrdinal(c.Date, end.ToString("yyyy-MM-dd")) > 0)
                throw new InvalidOperationException("Attendance is outside the approved training period.");
        }

        public JObject CurrentIssuedQr(AttendanceContext context, bool localTest = false)
        {
            string action = Number(context.Record,"checkInAt") > 0 ? "checkOut" : "checkIn";
            try
            {
                CheckAction(context, action, Now());
                var state = IssuedState(context.Record, action); CheckIssuedState(state);
                CheckQrContext(state, context, Text(context.Assignment,"companySupervisorId"), localTest);
                return new JObject { ["action"] = action, ["issuedAt"] = state["issuedAt"], ["expiresAt"] = state["expiresAt"] };
            }
            catch (InvalidOperationException) { return null; }
        }

        private async Task CheckEnabled()
        {
            var settings = await Get("settings");
            if (!Flag(settings, "enabled"))
                throw new InvalidOperationException("Attendance actions are disabled in Firebase settings. Enable them only after server access and identities are configured.");
        }

        private async Task<JObject> History(string assignmentId, string studentId, JObject records = null, JObject schedules = null, JObject assignment = null)
        {
            if (records == null || schedules == null)
            {
                var recordsRead = Get("attendance/" + assignmentId);
                var schedulesRead = Get("schedules/" + assignmentId);
                await Task.WhenAll(recordsRead, schedulesRead);
                records = await recordsRead;
                schedules = await schedulesRead;
            }
            // نستبعد الربط غير الصحيح من العرض فقط؛ لا نحذف أو نصلح بيانات Firebase تلقائيًا.
            foreach (var row in records.Properties().ToList())
            {
                DateTime recordDate;
                if (row.Value is JObject && row.Value["qrState"] is JObject && Number(row.Value, "checkInAt") == 0 && Number(row.Value, "checkOutAt") == 0)
                { row.Remove(); continue; }
                if (!(row.Value is JObject) || Text(row.Value, "assignmentId") != assignmentId ||
                    Text(row.Value, "studentId") != studentId || Text(row.Value, "attendanceDate") != row.Name ||
                    !DateTime.TryParseExact(row.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out recordDate) ||
                    assignment != null && (Text(row.Value, "companyId") != Text(assignment, "companyId") ||
                        Text(row.Value, "companySupervisorId") != Text(assignment, "companySupervisorId")))
                    row.Remove();
            }
            // الغياب يستنتج من يوم تدريب انتهى فعلاً، وليس من كل يوم فارغ.
            foreach (var entry in schedules.Properties())
            {
                var schedule = entry.Value;
                if (!CanDeriveScheduleHistory(schedule as JObject, assignment, assignmentId, entry.Name, Now())) continue;
                if (records[entry.Name] is JObject || Text(schedule, "assignmentId") != assignmentId ||
                    Text(schedule, "attendanceDate") != entry.Name || string.CompareOrdinal(entry.Name, Today()) > 0) continue;
                bool training = Flag(schedule, "isTrainingDay");
                if (training && (Number(schedule, "scheduledEndAt") <= 0 || Number(schedule, "scheduledEndAt") >= Now())) continue;
                records[entry.Name] = new JObject
                {
                    ["assignmentId"] = assignmentId, ["studentId"] = studentId, ["attendanceDate"] = entry.Name,
                    ["status"] = training ? "absent" : "dayOff", ["workedMinutes"] = 0,
                    ["hoursApprovalStatus"] = "notApplicable", ["derivedFromSchedule"] = true
                };
            }
            foreach (var record in records.Properties())
                if (record.Value is JObject) { ((JObject)record.Value).Remove("qrState"); ((JObject)record.Value).Remove("resetAttempts"); }
            return records;
        }

        // Display-only derivation must meet the same calendar/date boundaries as QR use.
        // A malformed or unapproved/missing calendar is never evidence of absence.
        public static bool CanDeriveScheduleHistory(JObject schedule, JObject assignment, string assignmentId, string date, long now)
        {
            DateTime day, first, last;
            if (schedule == null || assignment == null || string.IsNullOrWhiteSpace(assignmentId) ||
                Text(schedule, "assignmentId") != assignmentId || Text(schedule, "attendanceDate") != date ||
                Text(schedule, "timeZone") != "Asia/Amman" || Number(schedule, "version") <= 0 ||
                schedule["isTrainingDay"]?.Type != JTokenType.Boolean ||
                !DateTime.TryParseExact(date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out day) ||
                !DateTime.TryParseExact(Text(assignment,"startDate"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out first) ||
                !DateTime.TryParseExact(Text(assignment,"endDate"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out last) ||
                first > last || day < first || day > last) return false;
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
                string today = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(now),zone).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
                if (string.CompareOrdinal(date,today)>0) return false;
                if (!Flag(schedule,"isTrainingDay")) return true;
                long start = Number(schedule,"scheduledStartAt"), end = Number(schedule,"scheduledEndAt");
                return start > 0 && end > start && end < now &&
                    TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(start),zone).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)==date &&
                    TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(end),zone).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)==date;
            }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        // نحافظ على توقيع الاستدعاء السابق؛ التدرج اختياري لصفحات الحضور فقط.
        public async Task<AttendanceContext> LoadStudentAttendance(string id, JObject loadedStudent, Func<JObject, Task> profileReady)
        {
            AttendanceFirebase.CheckKey(id);
            // الهوية المحملة تأتي من تحقق السيرفر في نفس الطلب، وليس من JSON المتصفح.
            var student = loadedStudent ?? await Get("students/" + id);
            if (Text(student, "status") != "active")
                throw new InvalidOperationException("Student account is inactive or missing.");
            if (Text(student, "activeAssignmentId") == "")
                return new AttendanceContext { Student = student, Date = Today() };
            var c = await Context(id, null, student, true, profileReady: profileReady);
            var history = await History(c.AssignmentId, id, c.HistoryRecords, c.HistorySchedules, c.Assignment);
            long approvedMinutes = ApprovedTrainingMinutes.Calculate(history, c.AssignmentId, id, Text(c.Assignment, "companyId"), Now());
            bool checkIn = false, checkOut = false;
            try { CheckAction(c, "checkIn", Now()); checkIn = true; } catch (InvalidOperationException) { }
            try { CheckAction(c, "checkOut", Now()); checkOut = true; } catch (InvalidOperationException) { }
            bool enabled = Flag(c.Settings, "enabled");
            c.AttendanceHistory = history;
            c.ApprovedMinutes = approvedMinutes;
            c.CanCheckIn = enabled && checkIn;
            c.CanCheckOut = enabled && checkOut;
            c.CanRequestEarly = false; // الخروج المرن يتم برمز يصدره المشرف مباشرة.
            return c;
        }

        // Issuing a token never records check-in/out; only the assigned supervisor can scan it.
        public async Task<JObject> CreateQr(string studentId, string action, bool localDemo, string verifiedUid = "")
        {
            if (action != "checkIn" && action != "checkOut") throw new InvalidOperationException("Choose check-in or check-out.");
            await Identity("student", studentId, verifiedUid, localDemo);
            await CheckEnabled();
            var context = await Context(studentId, null);
            CheckLocalTestContext(context, localDemo);
            string path = "attendance/" + context.AssignmentId + "/" + context.Date;
            for (int retry = 0; retry < 3; retry++)
            {
                var snapshot = await database.Read(path);
                context.Record = snapshot.Data;
                long now = Now();
                CheckAction(context, action, now);
                byte[] nonce = new byte[16];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(nonce);
                var state = new JObject
                {
                    ["studentId"] = studentId, ["assignmentId"] = context.AssignmentId,
                    ["companyId"] = Text(context.Assignment, "companyId"),
                    ["supervisorId"] = Text(context.Assignment, "companySupervisorId"),
                    ["date"] = context.Date, ["action"] = action, ["version"] = Number(context.Schedule, "version"),
                    ["issuedAt"] = now, ["expiresAt"] = now + 120000, ["localDemo"] = localDemo,
                    ["nonce"] = BitConverter.ToString(nonce).Replace("-", ""), ["status"] = "issued"
                };
                string token = PersistentToken(state);
                var row = (JObject)snapshot.Data.DeepClone();
                if (row["qrState"] != null && !(row["qrState"] is JObject))
                    throw new InvalidOperationException("Stored QR state is inconsistent. No data was overwritten.");
                row["assignmentId"] = context.AssignmentId; row["studentId"] = studentId;
                row["companyId"] = Text(context.Assignment, "companyId");
                row["companySupervisorId"] = Text(context.Assignment, "companySupervisorId");
                row["attendanceDate"] = context.Date; row["isTestData"] = Flag(context.Student, "isTestData");
                if (row["qrState"] == null) row["qrState"] = new JObject();
                row["qrState"][action] = state;
                if (await Save(path, row, snapshot.Etag))
                    return new JObject { ["token"] = token, ["expiresAt"] = state["expiresAt"], ["serverNow"] = now };
            }
            throw new InvalidOperationException("Attendance changed while issuing QR. Refresh and try again.");
        }

        // هذا الحاجز يخص التجربة بدون Login؛ لا يسمح باستعمال حسابات أو تكليفات حقيقية.
        private static void CheckLocalTestContext(AttendanceContext context, bool localTest)
        {
            if ((localTest || Flag(context.Student, "isTestData")) && (!Flag(context.Student, "isTestData") || !Flag(context.Assignment, "isTestData") ||
                !Flag(context.Company, "isTestData") || !Flag(context.Supervisor, "isTestData") ||
                !Flag(context.Schedule, "isTestData")))
                throw new InvalidOperationException("Local testing permits isolated test accounts and schedules only.");
        }

        public async Task<JObject> IssueStudentQrs(string supervisorId, JArray selectedStudents, string action, bool localTest, string verifiedUid = "")
        {
            if (action != "checkIn" && action != "checkOut") throw new InvalidOperationException("Choose check-in or check-out.");
            if (selectedStudents == null || selectedStudents.Count == 0 || selectedStudents.Count > 200)
                throw new InvalidOperationException("Select between 1 and 200 students.");
            var ids = selectedStudents.Select(value => value.Type == JTokenType.String ? (string)value : "").Distinct().ToList();
            await Identity("company", supervisorId, verifiedUid, localTest);
            await CheckEnabled();
            var issued = new JArray(); var failed = new JArray();
            foreach (string studentId in ids)
            {
                try
                {
                    var context = await Context(studentId, supervisorId);
                    CheckLocalTestContext(context, localTest);
                    string path = "attendance/" + context.AssignmentId + "/" + context.Date;
                    bool saved = false;
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        var snapshot = await database.Read(path);
                        context.Record = snapshot.Data;
                        long now = Now();
                        CheckAction(context, action, now);
                        var randomBytes = new byte[16];
                        using (var random = RandomNumberGenerator.Create()) random.GetBytes(randomBytes);
                        var state = new JObject
                        {
                            ["studentId"] = studentId, ["assignmentId"] = context.AssignmentId,
                            ["companyId"] = Text(context.Assignment, "companyId"), ["supervisorId"] = supervisorId,
                            ["date"] = context.Date, ["action"] = action, ["version"] = Number(context.Schedule, "version"),
                            ["issuedAt"] = now, ["expiresAt"] = now + 120000, ["localDemo"] = localTest,
                            ["nonce"] = BitConverter.ToString(randomBytes).Replace("-", ""), ["status"] = "issued"
                        };
                        // نتحقق من المفتاح قبل الحفظ؛ لا نترك إصدارًا لا يستطيع السيرفر عرضه.
                        PersistentToken(state);
                        var row = (JObject)snapshot.Data.DeepClone();
                        row["assignmentId"] = context.AssignmentId; row["studentId"] = studentId;
                        row["companyId"] = Text(context.Assignment, "companyId");
                        row["companySupervisorId"] = supervisorId; row["attendanceDate"] = context.Date;
                        row["isTestData"] = Flag(context.Student, "isTestData");
                        if (row["qrState"] != null && !(row["qrState"] is JObject))
                            throw new InvalidOperationException("Stored QR state is inconsistent. No data was overwritten.");
                        if (row["qrState"] == null) row["qrState"] = new JObject();
                        row["qrState"][action] = state;
                        // الإصدار والحضور يشتركان في نفس ETag، فلا يعيد إصدار متزامن رمزًا مستهلكًا.
                        if (!await Save(path, row, snapshot.Etag)) continue;
                        saved = true;
                        issued.Add(new JObject { ["studentId"] = studentId, ["name"] = Text(context.Student, "name"), ["expiresAt"] = state["expiresAt"] });
                        break;
                    }
                    if (!saved) throw new InvalidOperationException("Attendance changed while issuing QR. Refresh and try again.");
                }
                catch (InvalidOperationException error) { failed.Add(new JObject { ["studentId"] = studentId, ["message"] = error.Message }); }
            }
            return new JObject { ["issued"] = issued, ["failed"] = failed };
        }

        public async Task<JObject> ReadIssuedQr(string studentId, string action, bool localTest)
        {
            var context = await Context(studentId, null);
            CheckLocalTestContext(context, localTest);
            await CheckEnabled();
            CheckAction(context, action, Now());
            var state = IssuedState(context.Record, action);
            CheckIssuedState(state);
            CheckQrContext(state, context, Text(context.Assignment, "companySupervisorId"), localTest);
            return new JObject { ["token"] = PersistentToken(state), ["expiresAt"] = state["expiresAt"], ["serverNow"] = Now() };
        }

        public Task<JObject> ScanIssuedQr(string supervisorId, string token, bool localTest)
        {
            return Scan(supervisorId, token, localTest);
        }

        private static JObject IssuedState(JObject record, string action)
        {
            var group = record?["qrState"] as JObject;
            var state = group?[action] as JObject;
            if (state == null) throw new InvalidOperationException("No current QR. Ask your supervisor to issue one, then try again.");
            return state;
        }

        private static void CheckIssuedState(JObject state)
        {
            if (Text(state, "status") != "issued") throw new InvalidOperationException("This QR has already been used or replaced.");
            long now = Now(), issued = Number(state, "issuedAt"), expires = Number(state, "expiresAt");
            if (issued <= 0 || issued > now + 5000 || expires <= now || expires - issued != 120000)
                throw new InvalidOperationException("QR expired. Ask your supervisor to issue a new one.");
        }

        private byte[] QrSignature(JObject state)
        {
            var claims = new JObject();
            foreach (string field in new[] { "studentId", "assignmentId", "companyId", "supervisorId", "date", "action", "version", "issuedAt", "expiresAt", "localDemo", "nonce" })
            {
                if (state[field] == null) throw new InvalidOperationException("The issued QR state is incomplete.");
                claims[field] = state[field].DeepClone();
            }
            byte[] key = qrSigningKey();
            if (key == null || key.Length != 32) throw new UIMS.Services.FirebaseException(503, "The attendance QR signing key is invalid.");
            using (var hmac = new HMACSHA256(key))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(claims.ToString(Newtonsoft.Json.Formatting.None)));
        }

        private string PersistentToken(JObject state)
        {
            var locator = new JArray(Text(state, "assignmentId"), Text(state, "date"), Text(state, "action"), Text(state, "nonce"));
            return "UIMSATT1:P:" + HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(locator.ToString(Newtonsoft.Json.Formatting.None))) + "." +
                HttpServerUtility.UrlTokenEncode(QrSignature(state));
        }

        private static JObject ParsePersistentToken(string token)
        {
            if (token == null || token.Length > 2048 || !token.StartsWith("UIMSATT1:P:", StringComparison.Ordinal))
                throw new InvalidOperationException("This QR is invalid or belongs to an older server version. Ask your supervisor to issue a new one.");
            try
            {
                var parts = token.Substring(11).Split('.');
                if (parts.Length != 2) throw new FormatException();
                var locator = JArray.Parse(Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(parts[0])));
                if (locator.Count != 4 || locator.Any(x => x.Type != JTokenType.String)) throw new FormatException();
                string assignment = (string)locator[0], date = (string)locator[1], action = (string)locator[2], nonce = (string)locator[3];
                AttendanceFirebase.CheckKey(assignment);
                DateTime parsed; Guid random;
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed) ||
                    (action != "checkIn" && action != "checkOut") || !Guid.TryParseExact(nonce, "N", out random) || random == Guid.Empty)
                    throw new FormatException();
                byte[] signature = HttpServerUtility.UrlTokenDecode(parts[1]);
                if (signature == null || signature.Length != 32) throw new FormatException();
                return new JObject { ["assignmentId"] = assignment, ["date"] = date, ["action"] = action, ["nonce"] = nonce, ["signature"] = parts[1] };
            }
            catch (Exception error) when (error is ArgumentException || error is FormatException || error is Newtonsoft.Json.JsonException)
            { throw new InvalidOperationException("Invalid or modified attendance QR."); }
        }

        private void VerifyPersistentToken(JObject locator, JObject state, string supervisorId)
        {
            CheckIssuedState(state);
            if (Text(state, "supervisorId") != supervisorId)
                throw new UIMS.Services.FirebaseException(403, "This QR does not belong to your supervision.");
            if (Text(locator, "assignmentId") != Text(state, "assignmentId") || Text(locator, "date") != Text(state, "date") ||
                Text(locator, "action") != Text(state, "action") || Text(locator, "nonce") != Text(state, "nonce"))
                throw new InvalidOperationException("This QR has been replaced. Ask your supervisor to issue a new one.");
            byte[] supplied = HttpServerUtility.UrlTokenDecode(Text(locator, "signature")), expected = QrSignature(state);
            int difference = 0;
            for (int i = 0; i < expected.Length; i++) difference |= supplied[i] ^ expected[i];
            if (difference != 0) throw new InvalidOperationException("Invalid or modified attendance QR.");
        }

        public async Task CheckLocalStudent(string studentId, string supervisorId, bool localTest)
        {
            if (localTest) CheckLocalTestContext(await Context(studentId, supervisorId), true);
        }

        public static JObject ReadQr(string token)
        {
            if (token == null || !token.StartsWith("UIMSATT1:", StringComparison.Ordinal) || token.Length > 4096)
                throw new InvalidOperationException("This is not a UIMS attendance QR.");
            try
            {
                byte[] bytes = HttpServerUtility.UrlTokenDecode(token.Substring(9));
                byte[] plain = MachineKey.Unprotect(bytes, "UIMS.Attendance.QR.v1");
                var data = JObject.Parse(Encoding.UTF8.GetString(plain));
                foreach (string field in new[] { "studentId", "assignmentId", "companyId", "supervisorId" })
                    AttendanceFirebase.CheckKey(Text(data, field));
                DateTime date;
                Guid nonce;
                if ((Text(data, "action") != "checkIn" && Text(data, "action") != "checkOut") ||
                    !DateTime.TryParseExact(Text(data, "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
                    !Guid.TryParseExact(Text(data, "nonce"), "N", out nonce) || nonce == Guid.Empty ||
                    Number(data, "version") <= 0 || data["localDemo"] == null || data["localDemo"].Type != JTokenType.Boolean)
                    throw new InvalidOperationException("The attendance QR contains incomplete or invalid data.");
                long now = Now(), issued = Number(data, "issuedAt"), expires = Number(data, "expiresAt");
                if (issued > now + 5000 || issued <= 0 || expires <= now || expires - issued != 120000)
                    throw new InvalidOperationException("This QR has expired. Ask the student to generate another one.");
                return data;
            }
            catch (InvalidOperationException) { throw; }
            // الرمز المشوّه قد يفشل عند فك Base64؛ نرفضه كخطأ تحقق بدل خطأ سيرفر عام.
            catch (Exception error) when (error is CryptographicException || error is ArgumentException ||
                error is FormatException || error is Newtonsoft.Json.JsonException)
            { throw new InvalidOperationException("Invalid or modified attendance QR."); }
        }

        public async Task<JObject> Scan(string supervisorId, string token, bool localDemo)
        {
            var locator = ParsePersistentToken(token);
            if (Text(locator, "date") != Today()) throw new InvalidOperationException("This QR is not for today's training.");
            string initialPath = "attendance/" + Text(locator, "assignmentId") + "/" + Text(locator, "date");
            var initial = await database.Read(initialPath);
            var qr = IssuedState(initial.Data, Text(locator, "action"));
            VerifyPersistentToken(locator, qr, supervisorId);
            var enabledRead = CheckEnabled();
            var contextRead = Context(Text(qr, "studentId"), supervisorId, readRecord: false);
            await Task.WhenAll(enabledRead, contextRead);
            var c = await contextRead;
            CheckLocalTestContext(c, localDemo);
            CheckQrContext(qr, c, supervisorId, localDemo);
            string path = "attendance/" + c.AssignmentId + "/" + c.Date;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var snapshot = await database.Read(path);
                c.Record = snapshot.Data;
                qr = IssuedState(c.Record, Text(locator, "action"));
                VerifyPersistentToken(locator, qr, supervisorId);
                CheckQrContext(qr, c, supervisorId, localDemo);
                long now = Now();
                string action = Text(qr, "action");
                CheckAction(c, action, now);
                var row = (JObject)c.Record.DeepClone();
                row["recordId"] = Text(row, "recordId") == "" ? "att_" + Guid.NewGuid().ToString("N") : Text(row, "recordId");
                row["assignmentId"] = c.AssignmentId;
                row["studentId"] = Text(qr, "studentId");
                row["companyId"] = Text(c.Assignment, "companyId");
                row["companySupervisorId"] = supervisorId;
                row["academicSupervisorId"] = Text(c.Assignment, "academicSupervisorId");
                row["attendanceDate"] = c.Date;
                row["scheduleVersion"] = Number(c.Schedule, "version");
                row["scheduledStartAt"] = Number(c.Schedule, "scheduledStartAt");
                row["scheduledEndAt"] = Number(c.Schedule, "scheduledEndAt");
                row["hoursApprovalStatus"] = "pending";
                row["attendanceMode"] = "flexible";
                row["isTestData"] = Flag(c.Student, "isTestData");
                row["updatedAt"] = now;
                if (row["createdAt"] == null) row["createdAt"] = now;
                if (action == "checkIn")
                {
                    row["checkInAt"] = now; row["checkInVerifiedById"] = supervisorId;
                    row["lateMinutes"] = 0;
                    row["isLate"] = false;
                    row["status"] = "checkedIn"; row["workedMinutes"] = 0;
                }
                else
                {
                    row["checkOutAt"] = now; row["checkOutVerifiedById"] = supervisorId;
                    row["workedMinutes"] = (now - Number(row, "checkInAt")) / 60000;
                    row["isEarlyDeparture"] = false; row["status"] = "completed";
                }
                // استهلاك الرمز والحضور في نفس السجل ونفس الكتابة المشروطة؛ لا توجد كتابة ثانية قد تفشل جزئيًا.
                var consumed = (JObject)row["qrState"][action];
                consumed["status"] = "consumed"; consumed["consumedAt"] = now; consumed["consumedById"] = supervisorId;
                if (await Save(path, row, snapshot.Etag))
                    return new JObject { ["student"] = c.Student, ["company"] = c.Company, ["supervisor"] = c.Supervisor, ["action"] = action, ["record"] = ForDisplay(row) };
            }
            throw new InvalidOperationException("The record changed during scanning. Refresh and try a new QR.");
        }

        private static void CheckQrContext(JObject qr, AttendanceContext context, string supervisorId, bool localDemo)
        {
            if (Flag(qr, "localDemo") != localDemo || Text(qr, "studentId") != Text(context.Assignment, "studentId") ||
                Text(qr, "assignmentId") != context.AssignmentId || Text(qr, "companyId") != Text(context.Assignment, "companyId") ||
                Text(qr, "supervisorId") != supervisorId || Text(qr, "date") != context.Date ||
                Number(qr, "version") != Number(context.Schedule, "version"))
                throw new InvalidOperationException("This QR does not match the active assignment, company, supervisor, or schedule.");
        }

        public async Task RequestEarly(string studentId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
                throw new InvalidOperationException("Enter a reason between 1 and 500 characters.");
            var enabledRead = CheckEnabled();
            // طلب الخروج يقرأ مع ETag قبل الإنشاء؛ لا نعيد قراءته ضمن Context.
            var contextRead = Context(studentId, null, readRequest: false);
            await Task.WhenAll(enabledRead, contextRead);
            var c = await contextRead;
            CheckSchedule(c);
            CheckRecord(c);
            if (Number(c.Record, "checkInAt") <= 0 || Number(c.Record, "checkInAt") > Now() || Number(c.Record, "checkOutAt") > 0 || Now() >= Number(c.Schedule, "scheduledEndAt"))
                throw new InvalidOperationException("Early departure can only be requested after check-in and before the shift ends.");
            string path = "earlyCheckoutRequests/" + c.AssignmentId + "/" + c.Date;
            var snapshot = await database.Read(path);
            if (snapshot.Data.HasValues) throw new InvalidOperationException("An early departure request already exists for today.");
            var row = new JObject
            {
                ["requestId"] = "att_early_" + Guid.NewGuid().ToString("N"), ["assignmentId"] = c.AssignmentId,
                ["studentId"] = studentId, ["companyId"] = Text(c.Assignment, "companyId"),
                ["companySupervisorId"] = Text(c.Assignment, "companySupervisorId"), ["attendanceDate"] = c.Date,
                ["isTestData"] = Flag(c.Student, "isTestData"),
                ["reason"] = reason.Trim(), ["status"] = "pending", ["requestedAt"] = Now()
            };
            if (!await Save(path, row, snapshot.Etag)) throw new InvalidOperationException("A request was already saved. Refresh the page.");
        }

        private async Task<JObject> AuthorizedAssignment(string supervisorId, string assignmentId)
        {
            AttendanceFirebase.CheckKey(supervisorId);
            AttendanceFirebase.CheckKey(assignmentId);
            var assignmentRead = Get("trainingAssignments/" + assignmentId);
            var supervisorRead = Get("companySupervisors/" + supervisorId);
            await Task.WhenAll(assignmentRead, supervisorRead);
            var assignment = await assignmentRead;
            var supervisor = await supervisorRead;
            if (AttendanceConfiguration.IsLocalDevelopment(HttpContext.Current) && (!Flag(assignment, "isTestData") || !Flag(supervisor, "isTestData")))
                throw new UIMS.Services.FirebaseException(403, "Local attendance testing only permits labelled test assignments and supervisors.");
            if (Text(supervisor, "status") != "active" || Text(assignment, "status") != "active" ||
                Text(assignment, "companySupervisorId") != supervisorId || Text(assignment, "companyId") != Text(supervisor, "companyId"))
                throw new InvalidOperationException("This assignment is not authorized for your supervisor account.");
            string companyId = Text(assignment, "companyId");
            AttendanceFirebase.CheckKey(companyId);
            var companyRead = Get("companies/" + companyId);
            var studentRead = Get("students/" + Text(assignment, "studentId"));
            await Task.WhenAll(companyRead, studentRead);
            if (Text(await companyRead, "status") != "active")
                throw new InvalidOperationException("The assigned company is inactive or missing.");
            var student = await studentRead;
            if (Text(student, "status") != "active" || Text(student, "activeAssignmentId") != assignmentId)
                throw new InvalidOperationException("The student no longer has this active assignment.");
            return assignment;
        }

        // قراءة بيانات فقط؛ توثيق الهوية والصلاحيات يبقى مطلوبًا في معالج طلب الصفحة.
        public Task<JObject> LoadSupervisor(string id)
        {
            AttendanceFirebase.CheckKey(id);
            return Get("companySupervisors/" + id);
        }

        public async Task<JArray[]> LoadSupervisorAttendance(string id, JObject loadedSupervisor, Func<JObject, Task> studentReady)
        {
            AttendanceFirebase.CheckKey(id);
            var supervisor = loadedSupervisor ?? await Get("companySupervisors/" + id);
            if (Text(supervisor, "status") != "active")
                throw new InvalidOperationException("The company supervisor is inactive or missing.");
            string companyId = Text(supervisor, "companyId");
            AttendanceFirebase.CheckKey(companyId);
            if (Text(await Get("companies/" + companyId), "status") != "active")
                throw new InvalidOperationException("The supervisor's company is inactive or missing.");
            var assignments = await Get("trainingAssignments");
            var students = new JArray(); var requests = new JArray(); var records = new JArray();
            // نحدد التكليفات المسموحة أولاً، ونحمّل أربعة طلاب كحد أقصى بالتوازي.
            var allowed = assignments.Properties().Where(entry =>
                Text(entry.Value, "companySupervisorId") == id && Text(entry.Value, "companyId") == Text(supervisor, "companyId") &&
                Text(entry.Value, "status") == "active" && (!AttendanceConfiguration.IsLocalDevelopment(HttpContext.Current) || Flag(entry.Value, "isTestData"))).ToList();
            using (var slots = new System.Threading.SemaphoreSlim(4))
            {
                var groups = await Task.WhenAll(allowed.Select(async entry =>
                {
                    await slots.WaitAsync();
                    try
                    {
                        AttendanceFirebase.CheckKey(entry.Name);
                        var groupStudents = new JArray(); var groupRequests = new JArray(); var groupRecords = new JArray();
                        var student = await Get("students/" + Text(entry.Value, "studentId"));
                        if (Text(student, "activeAssignmentId") != entry.Name || Text(student, "status") != "active" || AttendanceConfiguration.IsLocalDevelopment(HttpContext.Current) && !Flag(student, "isTestData"))
                            return new[] { groupStudents, groupRequests, groupRecords };
                        groupStudents.Add(new JObject { ["id"] = Text(entry.Value, "studentId"), ["name"] = Text(student, "name"), ["assignmentId"] = entry.Name, ["requiredHours"] = Number(entry.Value, "requiredHours") });
                        var historyRead = History(entry.Name, Text(entry.Value, "studentId"), assignment: (JObject)entry.Value);
                        var earlyRead = Get("earlyCheckoutRequests/" + entry.Name);
                        try
                        {
                            if (studentReady != null) await studentReady(new JObject { ["student"] = groupStudents[0] });
                        }
                        catch
                        {
                            // إذا أغلق المتصفح الرد، نراقب المهام التي بدأت بالفعل دون إرسال بيانات أخرى.
                            try { await Task.WhenAll(historyRead, earlyRead); } catch { }
                            throw;
                        }
                        await Task.WhenAll(historyRead, earlyRead);
                        var history = await historyRead;
                        foreach (var record in history.Properties())
                        {
                            if (!(record.Value is JObject)) continue;
                            var row = (JObject)record.Value.DeepClone(); row["studentName"] = Text(student, "name");
                            row["assignmentId"] = entry.Name; row["attendanceDate"] = record.Name;
                            // نعرض السجلات التاريخية أيضاً حتى يمكن اعتماد ساعاتها أو متابعة الخروج الناقص.
                            groupRecords.Add(row);
                        }
                        var early = await earlyRead;
                        foreach (var request in early.Properties())
                            if (request.Value is JObject && Text(request.Value, "status") == "pending" &&
                                Text(request.Value, "assignmentId") == entry.Name && Text(request.Value, "attendanceDate") == request.Name &&
                                Text(request.Value, "studentId") == Text(entry.Value, "studentId") && Text(request.Value, "companySupervisorId") == id &&
                                Text(request.Value, "companyId") == Text(supervisor, "companyId"))
                            {
                                var row = (JObject)request.Value.DeepClone(); row["studentName"] = Text(student, "name");
                                row["assignmentId"] = entry.Name; row["attendanceDate"] = request.Name; groupRequests.Add(row);
                            }
                        return new[] { groupStudents, groupRequests, groupRecords };
                    }
                    finally { slots.Release(); }
                }));
                foreach (var group in groups)
                {
                    foreach (var item in group[0]) students.Add(item);
                    foreach (var item in group[1]) requests.Add(item);
                    foreach (var item in group[2]) records.Add(item);
                }
            }
            return new[] { students, requests, records };
        }

        public async Task Review(string supervisorId, string assignmentId, string date, string decision)
        {
            if (date == null) throw new InvalidOperationException("Attendance date is required.");
            DateTime parsed;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
                throw new InvalidOperationException("Invalid attendance date.");
            if (string.CompareOrdinal(date, Today()) > 0)
                throw new InvalidOperationException("Future attendance records cannot be reviewed.");
            var enabledRead = CheckEnabled();
            var assignmentRead = AuthorizedAssignment(supervisorId, assignmentId);
            await Task.WhenAll(enabledRead, assignmentRead);
            var assignment = await assignmentRead;
            bool hours = decision == "approveHours";
            if (!hours && decision != "approved" && decision != "rejected") throw new InvalidOperationException("Invalid review action.");
            string path = (hours ? "attendance/" : "earlyCheckoutRequests/") + assignmentId + "/" + date;
            var snapshot = await database.Read(path); var row = snapshot.Data;
            if (Text(row, "assignmentId") != assignmentId || Text(row, "attendanceDate") != date || Text(row, "studentId") != Text(assignment, "studentId") ||
                Text(row, "companyId") != Text(assignment, "companyId") || Text(row, "companySupervisorId") != supervisorId)
                throw new InvalidOperationException("The record does not match your authorized assignment.");
            if (hours)
            {
                long start = Number(row, "checkInAt"), end = Number(row, "checkOutAt"), minutes = Number(row, "workedMinutes");
                if (Text(row, "hoursApprovalStatus") != "pending" || start <= 0 || end <= start || end > Now() ||
                    minutes <= 0 || minutes != (end - start) / 60000 || !HasValidWorkedTime(row, date) ||
                    (Text(row, "status") != "completed" && Text(row, "status") != "earlyDeparture"))
                    throw new InvalidOperationException("Only pending, completed records with valid actual worked minutes may be approved.");
                row["hoursApprovalStatus"] = "approved"; row["hoursApprovedAt"] = Now(); row["hoursApprovedById"] = supervisorId;
            }
            else
            {
                if (Text(row, "status") != "pending" || date != Today()) throw new InvalidOperationException("Only today's pending early departure requests may be reviewed.");
                var c = await Context(Text(assignment, "studentId"), supervisorId, readRequest: false);
                CheckSchedule(c);
                CheckRecord(c);
                if (Number(c.Record, "checkInAt") <= 0 || Number(c.Record, "checkInAt") > Now() || Number(c.Record, "checkOutAt") > 0 || Now() >= Number(c.Schedule, "scheduledEndAt"))
                    throw new InvalidOperationException("Early departure review is only available during an active shift.");
                row["status"] = decision; row["reviewedAt"] = Now(); row["reviewedById"] = supervisorId;
            }
            if (!await Save(path, row, snapshot.Etag))
                throw new InvalidOperationException("This record was already changed. Refresh before reviewing it again.");
        }

        private static bool HasValidWorkedTime(JToken row, string date)
        {
            long start = Number(row, "checkInAt"), end = Number(row, "checkOutAt"), minutes = Number(row, "workedMinutes");
            if (start <= 0 || end <= start || end > Now() || minutes <= 0 || minutes != (end - start) / 60000) return false;
            try
            {
                // لا نعتمد مدة ناتجة عن تواريخ مختلفة عن يوم سجل الحضور.
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
                return TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(start), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == date &&
                    TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(end), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == date;
            }
            catch (ArgumentOutOfRangeException) { return false; }
        }
    }

    public class AttendanceContext
    {
        public string AssignmentId { get; set; }
        public string Date { get; set; }
        public JObject Student { get; set; }
        public JObject Assignment { get; set; }
        public JObject Company { get; set; }
        public JObject Supervisor { get; set; }
        public JObject Schedule { get; set; }
        public JObject Record { get; set; }
        public JObject Request { get; set; }
        // بيانات عرض لنفس الطلب فقط، وليست حقولاً جديدة في Firebase أو ذاكرة مؤقتة مشتركة.
        internal JObject HistoryRecords { get; set; }
        internal JObject HistorySchedules { get; set; }
        internal JObject Settings { get; set; }
        public JObject AttendanceHistory { get; set; }
        public long ApprovedMinutes { get; set; }
        public bool CanCheckIn { get; set; }
        public bool CanCheckOut { get; set; }
        public bool CanRequestEarly { get; set; }
    }
}
