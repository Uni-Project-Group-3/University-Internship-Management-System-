using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Security;
using Newtonsoft.Json.Linq;

namespace UIMS.Attendance
{
    public class AttendanceService
    {
        private readonly AttendanceFirebase database = new AttendanceFirebase();
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

        public async Task<JObject> CreateLocalDemoSession(string scenario, bool localDemo)
        {
            if (!localDemo || !AttendanceConfiguration.IsLocalDevelopment(HttpContext.Current)) throw new InvalidOperationException("Training test creation is available on localhost test mode only.");
            if (scenario != "early" && scenario != "normal" && scenario != "local") throw new InvalidOperationException("Choose a supported training test scenario.");
            await CheckEnabled(); // نقرأ الإعداد فقط؛ لا نفعّل Firebase من زر التجربة.
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
            string date = localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            long start = localNow.ToUnixTimeMilliseconds();
            long midnight = new DateTimeOffset(localNow.Date.AddDays(1), localNow.Offset).ToUnixTimeMilliseconds();
            // الجلسة الجديدة تنتهي قبل منتصف الليل؛ لا نغير ساعة السيرفر أو جدول جلسة سابقة.
            long end = Math.Min(start + (scenario == "normal" ? 5 : scenario == "local" ? 240 : 20) * 60000, midnight - 1);
            string prefix = "att_demo_session_" + Guid.NewGuid().ToString("N");
            string student = prefix + "_student", supervisor = prefix + "_supervisor", company = prefix + "_company";
            string assignment = prefix + "_assignment", academic = prefix + "_academic";
            string label = localNow.ToString("MM-dd HH:mm:ss") + (scenario == "early" ? " Early (20 min)" : scenario == "local" ? " Local test (4 hours)" : " Normal (5 min)");
            var nodes = new Dictionary<string, JObject>
            {
                ["companies/" + company] = new JObject { ["name"] = "Local Test Company", ["status"] = "active", ["isTestData"] = true },
                ["academicSupervisors/" + academic] = new JObject { ["name"] = "Local Test Academic", ["status"] = "active", ["isTestData"] = true },
                ["companySupervisors/" + supervisor] = new JObject { ["name"] = "Test Supervisor " + label, ["companyId"] = company, ["status"] = "active", ["isTestData"] = true },
                ["trainingAssignments/" + assignment] = new JObject
                {
                    ["studentId"] = student, ["companyId"] = company, ["companySupervisorId"] = supervisor, ["academicSupervisorId"] = academic,
                    ["status"] = "active", ["isTestData"] = true, ["requiredHours"] = scenario == "early" ? 120 : 240,
                    ["startDate"] = date, ["endDate"] = date, ["timeZone"] = "Asia/Amman", ["trainingTitle"] = "Local training test - " + label
                },
                ["students/" + student] = new JObject { ["name"] = "Test Student " + label, ["studentNumber"] = prefix, ["major"] = "Software Engineering", ["status"] = "active", ["isTestData"] = true, ["activeAssignmentId"] = assignment },
                ["schedules/" + assignment + "/" + date] = new JObject
                {
                    ["assignmentId"] = assignment, ["attendanceDate"] = date, ["isTestData"] = true, ["isTrainingDay"] = true,
                    ["version"] = 1, ["timeZone"] = "Asia/Amman", ["scheduledStartAt"] = start, ["scheduledEndAt"] = end,
                    ["scheduledStart"] = localNow.ToString("HH:mm"), ["scheduledEnd"] = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(end), zone).ToString("HH:mm")
                }
            };
            // طالب ثانٍ لنفس الشركة والمشرف حتى نجرب تحديد الكل برمزين مختلفين.
            string secondStudent = prefix + "_student2", secondAssignment = prefix + "_assignment2";
            var secondAccount = (JObject)nodes["students/" + student].DeepClone();
            secondAccount["name"] = "Test Student 2 " + label;
            secondAccount["studentNumber"] = secondStudent;
            secondAccount["activeAssignmentId"] = secondAssignment;
            nodes["students/" + secondStudent] = secondAccount;
            var secondTraining = (JObject)nodes["trainingAssignments/" + assignment].DeepClone();
            secondTraining["studentId"] = secondStudent;
            nodes["trainingAssignments/" + secondAssignment] = secondTraining;
            var secondSchedule = (JObject)nodes["schedules/" + assignment + "/" + date].DeepClone();
            secondSchedule["assignmentId"] = secondAssignment;
            nodes["schedules/" + secondAssignment + "/" + date] = secondSchedule;
            // هذه سجلات جديدة مستقلة؛ نحفظها معًا لتقليل انتظار التجهيز، ولا نكتب أي حضور.
            await Task.WhenAll(nodes.Select(async node =>
            {
                // null_etag يضمن الإنشاء فقط؛ لا نستبدل أي سجل سابق حتى عند التصادم.
                if (!await Save(node.Key, node.Value, "null_etag"))
                    throw new InvalidOperationException("Test ID conflict. No existing record was replaced. Create a new test.");
            }));
            return new JObject { ["studentId"] = student, ["studentIds"] = new JArray(student, secondStudent), ["secondAssignmentId"] = secondAssignment, ["supervisorId"] = supervisor, ["assignmentId"] = assignment, ["label"] = label, ["date"] = date, ["startAt"] = start, ["endAt"] = end };
        }

        public async Task<JObject> Identity(string role, string id, string verifiedUid, bool localDemo)
        {
            if (role != "student" && role != "company") throw new InvalidOperationException("Invalid attendance role.");
            AttendanceFirebase.CheckKey(id);
            var actor = await Get((role == "student" ? "students/" : "companySupervisors/") + id);
            if (!actor.HasValues || Text(actor, "status") != "active")
                throw new InvalidOperationException("This attendance identity is missing or inactive.");
            if (localDemo && !Flag(actor, "isTestData"))
                throw new InvalidOperationException("Local testing only permits labelled test records.");
            if (!localDemo && (string.IsNullOrWhiteSpace(verifiedUid) || Text(actor, "firebaseUid") != verifiedUid || Flag(actor, "isTestData")))
                throw new InvalidOperationException("The verified login UID is not mapped to this attendance identity.");
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
            if (c.Record.HasValues && (Text(c.Record, "assignmentId") != c.AssignmentId ||
                Text(c.Record, "studentId") != Text(c.Assignment, "studentId") || Text(c.Record, "attendanceDate") != c.Date ||
                Text(c.Record, "companyId") != Text(c.Assignment, "companyId") ||
                Text(c.Record, "companySupervisorId") != Text(c.Assignment, "companySupervisorId")))
                throw new InvalidOperationException("The stored attendance record does not match the current assignment. No data was overwritten.");
        }

        public static void CheckAction(AttendanceContext c, string action, long now)
        {
            CheckSchedule(c);
            CheckRecord(c);
            if (action == "checkIn")
            {
                if (Number(c.Record, "checkInAt") > 0 || Number(c.Record, "checkOutAt") > 0)
                    throw new InvalidOperationException("Attendance is already recorded or requires review.");
                if (now < Number(c.Schedule, "scheduledStartAt") - 5 * 60000)
                    throw new InvalidOperationException("Check-in opens five minutes before your scheduled start.");
                if (now >= Number(c.Schedule, "scheduledEndAt"))
                    throw new InvalidOperationException("Check-in is closed because today's training shift has ended.");
                return;
            }
            if (action != "checkOut") throw new InvalidOperationException("Invalid attendance action.");
            if (Number(c.Record, "checkInAt") <= 0 || Number(c.Record, "checkOutAt") > 0)
                throw new InvalidOperationException("Check-out requires an existing check-in and cannot be repeated.");
            if (now <= Number(c.Record, "checkInAt")) throw new InvalidOperationException("Check-out must be after check-in.");
            if (now < Number(c.Schedule, "scheduledEndAt") &&
                (Text(c.Request, "status") != "approved" || Text(c.Request, "studentId") != Text(c.Assignment, "studentId") ||
                 Text(c.Request, "assignmentId") != c.AssignmentId || Text(c.Request, "attendanceDate") != c.Date ||
                 Text(c.Request, "companySupervisorId") != Text(c.Assignment, "companySupervisorId") ||
                 Text(c.Request, "companyId") != Text(c.Assignment, "companyId") ||
                 Text(c.Request, "reviewedById") != Text(c.Assignment, "companySupervisorId")))
                throw new InvalidOperationException("Early check-out requires approval from the assigned supervisor.");
        }

        private async Task CheckEnabled()
        {
            var settings = await Get("settings");
            if (!Flag(settings, "enabled"))
                throw new InvalidOperationException("Attendance actions are disabled in Firebase settings. Enable them only after server access and identities are configured.");
        }

        private async Task<JObject> History(string assignmentId, string studentId, JObject records = null, JObject schedules = null)
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
                if (!(row.Value is JObject) || Text(row.Value, "assignmentId") != assignmentId ||
                    Text(row.Value, "studentId") != studentId || Text(row.Value, "attendanceDate") != row.Name ||
                    !DateTime.TryParseExact(row.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out recordDate))
                    row.Remove();
            }
            // الغياب يستنتج من يوم تدريب انتهى فعلاً، وليس من كل يوم فارغ.
            foreach (var entry in schedules.Properties())
            {
                var schedule = entry.Value;
                if (!(schedule is JObject) || schedule["isTrainingDay"] == null || schedule["isTrainingDay"].Type != JTokenType.Boolean) continue;
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
            return records;
        }

        // نحافظ على توقيع الاستدعاء السابق؛ التدرج اختياري لصفحات الحضور فقط.
        public async Task<AttendanceContext> LoadStudentAttendance(string id, JObject loadedStudent, Func<JObject, Task> profileReady)
        {
            // الهوية المحملة تأتي من تحقق السيرفر في نفس الطلب، وليس من JSON المتصفح.
            var student = loadedStudent ?? await Get("students/" + id);
            if (Text(student, "activeAssignmentId") == "")
                return new AttendanceContext { Student = student, Date = Today() };
            var c = await Context(id, null, student, true, profileReady: profileReady);
            var history = await History(c.AssignmentId, id, c.HistoryRecords, c.HistorySchedules);
            long approvedMinutes = 0;
            foreach (var item in history.Properties())
                if (Text(item.Value, "hoursApprovalStatus") == "approved" && Text(item.Value, "studentId") == id &&
                    Text(item.Value, "assignmentId") == c.AssignmentId && Number(item.Value, "checkOutAt") > Number(item.Value, "checkInAt") &&
                    Text(item.Value, "companyId") == Text(c.Assignment, "companyId") &&
                    Text(item.Value, "hoursApprovedById") != "" && Text(item.Value, "hoursApprovedById") == Text(item.Value, "companySupervisorId") &&
                    (Text(item.Value, "status") == "completed" || Text(item.Value, "status") == "earlyDeparture") &&
                    Number(item.Value, "checkOutAt") <= Now() && Number(item.Value, "workedMinutes") > 0 &&
                    Number(item.Value, "checkInAt") > 0 && Number(item.Value, "workedMinutes") ==
                    (Number(item.Value, "checkOutAt") - Number(item.Value, "checkInAt")) / 60000)
                    approvedMinutes += Number(item.Value, "workedMinutes");
            bool checkIn = false, checkOut = false;
            try { CheckAction(c, "checkIn", Now()); checkIn = true; } catch (InvalidOperationException) { }
            try { CheckAction(c, "checkOut", Now()); checkOut = true; } catch (InvalidOperationException) { }
            bool enabled = Flag(c.Settings, "enabled");
            bool requestEarly = false;
            try
            {
                CheckSchedule(c); CheckRecord(c);
                requestEarly = Number(c.Record, "checkInAt") > 0 && Number(c.Record, "checkInAt") <= Now() &&
                    Number(c.Record, "checkOutAt") == 0 && Now() < Number(c.Schedule, "scheduledEndAt") && !c.Request.HasValues;
            }
            catch (InvalidOperationException) { }
            c.AttendanceHistory = history;
            c.ApprovedMinutes = approvedMinutes;
            c.CanCheckIn = enabled && checkIn;
            c.CanCheckOut = enabled && checkOut;
            c.CanRequestEarly = enabled && requestEarly;
            return c;
        }

        public async Task<JObject> CreateQr(string studentId, string action, bool localDemo)
        {
            var enabledRead = CheckEnabled();
            var contextRead = Context(studentId, null);
            await Task.WhenAll(enabledRead, contextRead);
            var c = await contextRead;
            long now = Now();
            CheckAction(c, action, now);
            var payload = new JObject
            {
                ["studentId"] = studentId, ["assignmentId"] = c.AssignmentId,
                ["companyId"] = Text(c.Assignment, "companyId"), ["supervisorId"] = Text(c.Assignment, "companySupervisorId"),
                ["date"] = c.Date, ["action"] = action, ["version"] = Number(c.Schedule, "version"),
                ["issuedAt"] = now, ["expiresAt"] = now + 120000, ["localDemo"] = localDemo,
                ["nonce"] = Guid.NewGuid().ToString("N")
            };
            // حماية وتشفير الرمز بمفتاح ASP.NET؛ لا يوجد مفتاح سري داخل JavaScript.
            byte[] protectedBytes = MachineKey.Protect(Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)), "UIMS.Attendance.QR.v1");
            return new JObject { ["token"] = "UIMSATT1:" + HttpServerUtility.UrlTokenEncode(protectedBytes), ["expiresAt"] = now + 120000, ["serverNow"] = now };
        }

        private static string IssuedQrKey(string assignmentId, string action)
        {
            return "Uims.Attendance.IssuedQr:" + assignmentId + ":" + Today() + ":" + action;
        }

        // هذا الحاجز يخص التجربة بدون Login؛ لا يسمح باستعمال حسابات أو تكليفات حقيقية.
        private static void CheckLocalTestContext(AttendanceContext context, bool localTest)
        {
            if (localTest && (!Flag(context.Student, "isTestData") || !Flag(context.Assignment, "isTestData") ||
                !Flag(context.Company, "isTestData") || !Flag(context.Supervisor, "isTestData") ||
                !Flag(context.Schedule, "isTestData")))
                throw new InvalidOperationException("Local testing permits isolated test accounts and schedules only.");
        }

        public async Task<JObject> IssueStudentQrs(string supervisorId, JArray selectedStudents, string action, bool localTest, string verifiedUid = "")
        {
            if (action != "checkIn" && action != "checkOut") throw new InvalidOperationException("Choose check-in or check-out.");
            var ids = selectedStudents.Select(value => value.Type == JTokenType.String ? (string)value : "").Distinct().ToList();
            if (ids.Count == 0 || ids.Count > 200) throw new InvalidOperationException("Select between 1 and 200 students.");
            var issued = new JArray(); var failed = new JArray();
            foreach (string studentId in ids)
            {
                try
                {
                    await Identity("company", supervisorId, verifiedUid, localTest);
                    var context = await Context(studentId, supervisorId);
                    CheckLocalTestContext(context, localTest);
                    var qr = await CreateQr(studentId, action, localTest);
                    var entry = (JObject)qr.DeepClone();
                    // مرجع عشوائي قصير للكاميرا؛ البيانات المحمية تظل على السيرفر.
                    var randomBytes = new byte[32];
                    using (var random = RandomNumberGenerator.Create()) random.GetBytes(randomBytes);
                    string shortToken = "UIMSATT1:S:" + BitConverter.ToString(randomBytes).Replace("-", "");
                    entry["protectedToken"] = qr["token"];
                    entry["token"] = shortToken;
                    HttpRuntime.Cache.Insert("Uims.Attendance.ShortQr:" + shortToken, entry, null,
                        DateTimeOffset.FromUnixTimeMilliseconds(Number(qr, "expiresAt")).UtcDateTime,
                        Cache.NoSlidingExpiration);
                    entry["studentId"] = studentId; entry["supervisorId"] = supervisorId;
                    entry["assignmentId"] = context.AssignmentId; entry["action"] = action;
                    // الرمز مؤقت في ذاكرة السيرفر 120 ثانية؛ لا نضيف مسارات أو حقولاً لقاعدة البيانات.
                    HttpRuntime.Cache.Insert(IssuedQrKey(context.AssignmentId, action), entry, null,
                        DateTimeOffset.FromUnixTimeMilliseconds(Number(qr, "expiresAt")).UtcDateTime,
                        Cache.NoSlidingExpiration);
                    issued.Add(new JObject { ["studentId"] = studentId, ["name"] = Text(context.Student, "name"),
                        ["expiresAt"] = qr["expiresAt"] });
                }
                catch (InvalidOperationException error)
                {
                    failed.Add(new JObject { ["studentId"] = studentId, ["message"] = error.Message });
                }
            }
            return new JObject { ["issued"] = issued, ["failed"] = failed };
        }

        public async Task<JObject> ReadIssuedQr(string studentId, string action, bool localTest)
        {
            var context = await Context(studentId, null);
            CheckLocalTestContext(context, localTest);
            await CheckEnabled();
            CheckAction(context, action, Now());
            var issued = HttpRuntime.Cache[IssuedQrKey(context.AssignmentId, action)] as JObject;
            if (issued == null || Number(issued, "expiresAt") <= Now() || Text(issued, "studentId") != studentId)
                throw new InvalidOperationException("No current QR. Ask your supervisor to issue one, then try again.");
            var result = (JObject)issued.DeepClone();
            result.Remove("protectedToken");
            result["serverNow"] = Now();
            return result;
        }

        public async Task<JObject> ScanIssuedQr(string supervisorId, string token, bool localTest)
        {
            var payload = ReadQr(token);
            string action = Text(payload, "action"), assignmentId = Text(payload, "assignmentId");
            var issued = HttpRuntime.Cache[IssuedQrKey(assignmentId, action)] as JObject;
            if (issued == null || Text(issued, "token") != token || Text(issued, "supervisorId") != supervisorId ||
                Text(issued, "studentId") != Text(payload, "studentId"))
                throw new InvalidOperationException("This QR was not issued by your supervisor or has been replaced.");
            var context = await Context(Text(payload, "studentId"), supervisorId);
            CheckLocalTestContext(context, localTest);
            // Scan يعيد التحقق من الوقت والملكية وETag قبل أي حفظ؛ وجود الرمز لا يتجاوزها.
            var result = await Scan(supervisorId, token, localTest);
            if (ReferenceEquals(HttpRuntime.Cache[IssuedQrKey(assignmentId, action)], issued))
                HttpRuntime.Cache.Remove(IssuedQrKey(assignmentId, action));
            return result;
        }

        public async Task CheckLocalStudent(string studentId, string supervisorId, bool localTest)
        {
            if (localTest) CheckLocalTestContext(await Context(studentId, supervisorId), true);
        }

        public static JObject ReadQr(string token)
        {
            if (token != null && token.StartsWith("UIMSATT1:S:", StringComparison.Ordinal))
            {
                var entry = token.Length == 75 ? HttpRuntime.Cache["Uims.Attendance.ShortQr:" + token] as JObject : null;
                if (entry == null || Number(entry, "expiresAt") <= Now())
                    throw new InvalidOperationException("QR expired. Ask your supervisor to issue a new one.");
                token = Text(entry, "protectedToken");
            }
            if (token == null || !token.StartsWith("UIMSATT1:", StringComparison.Ordinal) || token.Length > 4096)
                throw new InvalidOperationException("This is not a UIMS attendance QR.");
            try
            {
                byte[] bytes = HttpServerUtility.UrlTokenDecode(token.Substring(9));
                byte[] plain = MachineKey.Unprotect(bytes, "UIMS.Attendance.QR.v1");
                var data = JObject.Parse(Encoding.UTF8.GetString(plain));
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
            var qr = ReadQr(token);
            if (Flag(qr, "localDemo") != localDemo) throw new InvalidOperationException("Local test QR tokens cannot be used outside local test mode.");
            var enabledRead = CheckEnabled();
            // سجل الحضور يقرأ مرة واحدة داخل حلقة ETag الحديثة، ويعاد فقط عند التعارض.
            var contextRead = Context(Text(qr, "studentId"), supervisorId, readRecord: false);
            await Task.WhenAll(enabledRead, contextRead);
            var c = await contextRead;
            if (Text(qr, "assignmentId") != c.AssignmentId || Text(qr, "companyId") != Text(c.Assignment, "companyId") ||
                Text(qr, "supervisorId") != supervisorId || Text(qr, "date") != c.Date || Number(qr, "version") != Number(c.Schedule, "version"))
                throw new InvalidOperationException("This QR does not match the active assignment, company, supervisor, or schedule.");
            string path = "attendance/" + c.AssignmentId + "/" + c.Date;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var snapshot = await database.Read(path);
                c.Record = snapshot.Data;
                ReadQr(token);
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
                row["isTestData"] = Flag(c.Student, "isTestData");
                row["updatedAt"] = now;
                if (row["createdAt"] == null) row["createdAt"] = now;
                if (action == "checkIn")
                {
                    row["checkInAt"] = now; row["checkInVerifiedById"] = supervisorId;
                    row["lateMinutes"] = Math.Max(0, (now - Number(c.Schedule, "scheduledStartAt")) / 60000);
                    row["isLate"] = now > Number(c.Schedule, "scheduledStartAt");
                    row["status"] = "checkedIn"; row["workedMinutes"] = 0;
                }
                else
                {
                    row["checkOutAt"] = now; row["checkOutVerifiedById"] = supervisorId;
                    row["workedMinutes"] = (now - Number(row, "checkInAt")) / 60000;
                    bool early = now < Number(c.Schedule, "scheduledEndAt");
                    row["isEarlyDeparture"] = early; row["status"] = early ? "earlyDeparture" : "completed";
                    if (early) row["earlyCheckoutRequestId"] = Text(c.Request, "requestId");
                }
                // التوقيت والعملية نفسها يكتبان معاً بشرط ETag؛ طلبان متزامنان لا ينجحان معاً.
                if (await Save(path, row, snapshot.Etag))
                    return new JObject { ["student"] = c.Student, ["company"] = c.Company, ["supervisor"] = c.Supervisor, ["action"] = action, ["record"] = row };
            }
            throw new InvalidOperationException("The record changed during scanning. Refresh and try a new QR.");
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
            var supervisor = loadedSupervisor ?? await Get("companySupervisors/" + id);
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
                        var groupStudents = new JArray(); var groupRequests = new JArray(); var groupRecords = new JArray();
                        var student = await Get("students/" + Text(entry.Value, "studentId"));
                        if (Text(student, "activeAssignmentId") != entry.Name || Text(student, "status") != "active" || AttendanceConfiguration.IsLocalDevelopment(HttpContext.Current) && !Flag(student, "isTestData"))
                            return new[] { groupStudents, groupRequests, groupRecords };
                        groupStudents.Add(new JObject { ["id"] = Text(entry.Value, "studentId"), ["name"] = Text(student, "name"), ["assignmentId"] = entry.Name, ["requiredHours"] = Number(entry.Value, "requiredHours") });
                        var historyRead = History(entry.Name, Text(entry.Value, "studentId"));
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
                    minutes <= 0 || minutes != (end - start) / 60000 || (Text(row, "status") != "completed" && Text(row, "status") != "earlyDeparture"))
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
