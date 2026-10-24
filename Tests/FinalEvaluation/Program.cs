using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private const string OfflineFirebaseRoot = "https://uims-offline-tests-default-rtdb.firebaseio.com/";
    private static FirebaseFinalEvaluationConfiguration OfflineFirebaseConfiguration() { return new FirebaseFinalEvaluationConfiguration(OfflineFirebaseRoot); }
    private static int passed;
    private static int failed;
    private sealed class AttendancePostWorker : System.Web.Hosting.SimpleWorkerRequest
    {
        internal AttendancePostWorker() : base("/", System.IO.Path.GetTempPath(), "MyAttendance.aspx", "", new System.IO.StringWriter()) { }
        public override string GetHttpVerbName() => "POST";
        public override string GetRemoteAddress() => "127.0.0.1";
        public override string GetLocalAddress() => "127.0.0.1";
    }
    private static async Task AuthenticatedAttendanceScopeTests()
    {
        var previous=System.Web.HttpContext.Current;
        var config=new UIMS.Attendance.AttendanceConfiguration(k=>k=="AttendanceProductionWritesEnabled"?"true":null);
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var field=typeof(UIMS.Attendance.AttendanceConfiguration).GetField("WriteRequest",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        object slot=field.GetValue(null); var property=slot.GetType().GetProperty("Value"); object old=property.GetValue(slot);
        var actorType=typeof(UIMS.Attendance.AttendanceConfiguration).GetNestedType("VerifiedWriteRequest",System.Reflection.BindingFlags.NonPublic);
        var actor=Activator.CreateInstance(actorType,true);
        Action<string,string> set=(key,value)=>actorType.GetField(key,flags).SetValue(actor,value);
        string today=UIMS.Attendance.AttendanceService.Today();
        var row=new JObject{["assignmentId"]="A",["attendanceDate"]=today,["studentId"]="S",["companyId"]="C",["companySupervisorId"]="CS",["isTestData"]=true};
        try {
            System.Web.HttpContext.Current=new System.Web.HttpContext(new AttendancePostWorker());
            set("Uid","verified-S");set("Role","student");set("Id","S");set("Action","qr"); property.SetValue(slot,actor);
            config.EnsureWriteScope("attendance/A/"+today,row);
            set("Id","OTHER"); await Reject<UIMS.Services.FirebaseException>(()=>{config.EnsureWriteScope("attendance/A/"+today,row);return Task.CompletedTask;});
            set("Id","S");set("Action","scan");await Reject<UIMS.Services.FirebaseException>(()=>{config.EnsureWriteScope("attendance/A/"+today,row);return Task.CompletedTask;});
            set("Role","company");set("Id","CS");set("Uid","verified-CS");config.EnsureWriteScope("attendance/A/"+today,row);
            set("Id","OTHER");await Reject<UIMS.Services.FirebaseException>(()=>{config.EnsureWriteScope("attendance/A/"+today,row);return Task.CompletedTask;});
        } finally {property.SetValue(slot,old);System.Web.HttpContext.Current=previous;}
    }
    private sealed class QrMemoryDatabase : UIMS.Attendance.AttendanceFirebase
    {
        internal readonly Dictionary<string,JObject> Rows = new Dictionary<string,JObject>();
        internal int Writes;
        internal int RejectNextSaves;
        public override Task<UIMS.Attendance.AttendanceSnapshot> Read(string path) => Task.FromResult(new UIMS.Attendance.AttendanceSnapshot { Data = Rows.ContainsKey(path) ? (JObject)Rows[path].DeepClone() : new JObject(), Etag = "offline" });
        public override Task<bool> SaveIfUnchanged(string path, JObject data, string etag) { if (RejectNextSaves > 0) { RejectNextSaves--; return Task.FromResult(false); } Writes++; Rows[path] = (JObject)data.DeepClone(); return Task.FromResult(true); }
    }
    private static async Task StudentQrIssuanceTests()
    {
        var db = new QrMemoryDatabase(); string day = UIMS.Attendance.AttendanceService.Today();
        string path = "attendance/A/" + day;
        db.Rows["settings"] = new JObject { ["enabled"] = true };
        db.Rows["students/S"] = new JObject { ["status"] = "active", ["firebaseUid"] = "uid-S", ["activeAssignmentId"] = "A" };
        db.Rows["trainingAssignments/A"] = new JObject { ["status"]="active", ["studentId"]="S", ["companyId"]="C", ["companySupervisorId"]="CS", ["requiredHours"]=300, ["startDate"]=day, ["endDate"]=day };
        db.Rows["companies/C"] = new JObject { ["status"]="active" };
        db.Rows["companySupervisors/CS"] = new JObject { ["status"]="active", ["companyId"]="C", ["firebaseUid"]="uid-CS" };
        db.Rows["schedules/A/"+day] = new JObject { ["assignmentId"]="A", ["attendanceDate"]=day, ["timeZone"]="Asia/Amman", ["version"]=1, ["isTrainingDay"]=true, ["scheduledStartAt"]=DateTimeOffset.Parse(day+"T09:00:00+03:00").ToUnixTimeMilliseconds(), ["scheduledEndAt"]=DateTimeOffset.Parse(day+"T16:00:00+03:00").ToUnixTimeMilliseconds() };
        db.Rows[path] = new JObject { ["assignmentId"]="A", ["studentId"]="S", ["companyId"]="C", ["companySupervisorId"]="CS", ["attendanceDate"]=day, ["isTestData"]=true, ["historicalNote"]="preserve", ["approvedMinutes"]=2405 };
        var original = (JObject)db.Rows[path].DeepClone();
        // Existing verified accounts can retain historical test labels in authenticated operation.
        foreach(string labelled in new[]{"students/S","trainingAssignments/A","companies/C","companySupervisors/CS","schedules/A/"+day}) db.Rows[labelled]["isTestData"]=true;
        var isolatedService = new UIMS.Attendance.AttendanceService(db, () => Enumerable.Repeat((byte)7,32).ToArray());
        await Reject<UIMS.Services.FirebaseException>(()=>isolatedService.CreateQr("S","checkIn",false,"uid-S")); Assert(db.Writes==0);
        var service = new UIMS.Attendance.AttendanceService(db, () => Enumerable.Repeat((byte)7,32).ToArray(), new UIMS.Attendance.AttendanceConfiguration(key=>key=="AttendanceProductionWritesEnabled"?"true":null));
        await Reject<UIMS.Services.FirebaseException>(()=>service.CreateQr("S","checkIn",false,"foreign-uid")); Assert(db.Writes==0);
        await Reject<InvalidOperationException>(()=>service.CreateQr("S","checkOut",false,"uid-S")); Assert(db.Writes==0);
        var qr = await service.CreateQr("S","checkIn",false,"uid-S");
        Assert(!string.IsNullOrWhiteSpace((string)qr["token"]) && (long)qr["expiresAt"]-(long)qr["serverNow"]==120000);
        Assert((long)db.Rows[path]["approvedMinutes"]==2405 && (string)db.Rows[path]["historicalNote"]=="preserve" && db.Rows[path]["checkInAt"]==null);
        string first=(string)qr["token"];
        qr=await service.CreateQr("S","checkIn",false,"uid-S"); Assert(first!=(string)qr["token"]);
        var issued = await service.IssueStudentQrs("CS",new JArray("S"),"checkIn",false,"uid-CS");
        Assert(((JArray)issued["issued"]).Count==1 && ((JArray)issued["failed"]).Count==0 && db.Rows[path]["checkInAt"]==null);
        foreach(var property in original.Properties()) Assert(JToken.DeepEquals(property.Value,db.Rows[path][property.Name]));
        // A separate historical row must remain byte-for-byte equivalent throughout the cycle.
        string oldPath="attendance/A/2020-01-01";
        db.Rows[oldPath]=new JObject { ["workedMinutes"]=2405, ["hoursApprovalStatus"]="approved", ["checkInAt"]=1000, ["checkOutAt"]=144301000 };
        var history=(JObject)db.Rows[oldPath].DeepClone();
        qr=await service.ReadIssuedQr("S","checkIn",false);
        await Reject<UIMS.Services.FirebaseException>(()=>service.Scan("OTHER",(string)qr["token"],false));
        // Replace just the signature with another syntactically valid signature.
        string valid=(string)qr["token"], tampered=valid.Substring(0,valid.LastIndexOf('.')+1)+System.Web.HttpServerUtility.UrlTokenEncode(new byte[32]);
        await Reject<InvalidOperationException>(()=>service.Scan("CS",tampered,false));
        db.RejectNextSaves=1;
        var scanned=await service.Scan("CS",valid,false);
        Assert((string)scanned["action"]=="checkIn" && (string)scanned["record"]["status"]=="checkedIn" && scanned["record"]["qrState"]==null);
        long checkIn=(long)db.Rows[path]["checkInAt"];
        Assert(checkIn>0 && db.Rows[path]["checkOutAt"]==null);
        await Reject<InvalidOperationException>(()=>service.Scan("CS",valid,false));
        // Simulate elapsed server time in the isolated database; no production clock or data changes.
        db.Rows[path]["checkInAt"]=UIMS.Attendance.AttendanceService.Now()-125000;
        checkIn=(long)db.Rows[path]["checkInAt"];
        qr=await service.CreateQr("S","checkOut",false,"uid-S");
        scanned=await service.Scan("CS",(string)qr["token"],false);
        var record=(JObject)scanned["record"];
        Assert((string)scanned["action"]=="checkOut" && (string)record["status"]=="completed");
        Assert((long)record["checkInAt"]==checkIn && (long)record["workedMinutes"]==((long)record["checkOutAt"]-checkIn)/60000 && (long)record["workedMinutes"]==2);
        Assert((string)record["hoursApprovalStatus"]=="pending" && (long)record["approvedMinutes"]==2405);
        await Reject<InvalidOperationException>(()=>service.Scan("CS",(string)qr["token"],false));
        Assert(JToken.DeepEquals(history,db.Rows[oldPath]));
    }
    private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
    private static async Task Reject<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static async Task Test(string name, Func<Task> test)
    {
        try { await test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name); Console.WriteLine(error); }
    }
    private static EvaluationPolicy Policy()
    {
        var criteria = new Dictionary<string, CriterionPolicy> { { "testCriterion", new CriterionPolicy { Minimum = 1, Maximum = 7, Required = true } } };
        return new EvaluationPolicy { PolicyId = "fictional-policy", Version = "test-v1", CompanyCriteria = criteria, AcademicCriteria = criteria };
    }
    private static CompanyFinalEvaluation Draft(string id = "fictional-internship")
    {
        return new CompanyFinalEvaluation { InternshipId = id, StudentId = "fictional-student", EvaluatorId = "fictional-evaluator", PolicyId = "fictional-policy", PolicyVersion = "test-v1", Scores = new Dictionary<string, decimal?> { { "testCriterion", 3 } } };
    }
    private sealed class User : IAuthenticatedUser { public string UserId { get; set; } = "fictional-evaluator"; }
    private sealed class Assignment : IInternshipAssignmentAuthorizer
    {
        public bool Allowed;
        public Task<bool> CanAccessAsync(string user, string internship, string student, string capability) { return Task.FromResult(Allowed); }
    }
    private sealed class Token : IAccessTokenProvider { public Task<string> GetAccessTokenAsync() { return Task.FromResult("local-test-token"); } }
    private sealed class Handler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { return Task.FromResult(Respond(request)); }
    }
    private static HttpResponseMessage Response(string json, string etag = "\"test-version\"")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        if (etag != null) response.Headers.TryAddWithoutValidation("ETag", etag);
        return response;
    }
    private static FirebaseFinalEvaluationRepository Rest(Handler handler)
    {
        return new FirebaseFinalEvaluationRepository(new HttpClient(handler), new Token(), OfflineFirebaseConfiguration());
    }
    private static async Task FirebaseDisabledBlocksNetwork()
    {
        int requests = 0;
        var handler = new Handler { Respond = request => { requests++; return Response("{}"); } };
        var client = new HttpClient(handler);
        var settings = new Dictionary<string, string>
        {
            { "FirebaseEnabled", "false" }, { "FirebaseAuthenticationEnabled", "false" },
            { "FirebaseProjectId", "offline-test" }, { "FirebaseDatabaseUrl", OfflineFirebaseRoot },
            { "FirebaseApiKey", "offline-test-key" }, { "FirebaseLiveWritesEnabled", "false" },
            { "FirebaseSchemaValidated", "false" }, { "FirebaseTaskSchemaValidated", "false" },
            { "TasksEnabled", "false" }, { "TasksNoAuthenticationEnabled", "false" },
            { "TasksLiveWritesEnabled", "false" }
        };
        var taskService = new UIMS.Tasks.TaskService(key => settings.ContainsKey(key) ? settings[key] : null, client, null, token => Task.FromResult("offline-test-token"));
        try { await taskService.LoadAsync("student", CancellationToken.None); throw new Exception("Disabled task request was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 503); }
        Assert(requests == 0);
        try { await taskService.LoadBoardAsync("offline-placement", CancellationToken.None); throw new Exception("Disabled board request was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 503); }
        Assert(requests == 0);
        var disabledFirebase = new UIMS.Services.FirebaseConfiguration("false", "offline-test", OfflineFirebaseRoot, "offline-test-key", "false", "false", "false", "false");
        var signIn = new University_Internship_Management_System.Shared.FirebaseSignInService(client, "offline-test-key", token => Task.FromResult("uid"), uid => Task.FromResult(new Newtonsoft.Json.Linq.JObject()), disabledFirebase);
        try { await signIn.SignInAsync("student@example.test", "unused"); throw new Exception("Disabled sign-in request was accepted."); }
        catch (University_Internship_Management_System.Shared.FirebaseLoginException) { }
        Assert(requests == 0);
        var gateway = new University_Internship_Management_System.Shared.StudentFirebaseGateway(disabledFirebase);
        try { await gateway.ReadAsync("users/offline-uid"); throw new Exception("Disabled privileged database read was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 503); }
        Assert(requests == 0);
        var evaluation = new FirebaseFinalEvaluationRepository(client, new Token(), new FirebaseFinalEvaluationConfiguration(disabledFirebase));
        try { await evaluation.GetCompanyAsync("offline-internship"); throw new Exception("Disabled evaluation read was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 503); }
        Assert(requests == 0);
        var attendanceSettings = new Dictionary<string, string>
        {
            { "FirebaseEnabled", "false" }, { "FirebaseDatabaseUrl", OfflineFirebaseRoot },
            { "AttendanceEnabled", "false" }, { "AttendanceSchemaValidated", "false" },
            { "AttendanceLiveWritesEnabled", "false" }
        };
        var attendance = new UIMS.Attendance.AttendanceFirebase(client,
            new UIMS.Attendance.AttendanceConfiguration(key => attendanceSettings.ContainsKey(key) ? attendanceSettings[key] : null),
            () => true);
        try { await attendance.Read("attendance/att_demo_student/2026-10-22"); throw new Exception("Disabled attendance read was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 503); }
        Assert(requests == 0);
    }

    private static async Task OpportunityServiceHonorsDisabledFirebase()
    {
        var configuration = new UIMS.Services.FirebaseConfiguration("false", "offline-test",
            OfflineFirebaseRoot, "offline-test-key", "false", "false", "false", "false");
        var service = new UIMS.Services.OpportunityService(new University_Internship_Management_System.Shared.StudentFirebaseGateway(configuration));
        try { await service.GetPublishedForStudentAsync("fictional-auth-uid"); throw new Exception("Disabled Firebase opportunity read was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 503); }
    }

    private sealed class ApplicationMemoryDatabase : UIMS.Services.IApplicationDatabase
    {
        private readonly Dictionary<string, Newtonsoft.Json.Linq.JToken> records = new Dictionary<string, Newtonsoft.Json.Linq.JToken>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> versions = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> active = new HashSet<string>(StringComparer.Ordinal);
        public bool LoseAttendanceAcknowledgmentOnce;
        public bool ConflictAttendanceOnce;
        public void Enable(string uid) { active.Add(uid); }
        public void Seed(string path, string json) { records[path] = Newtonsoft.Json.Linq.JToken.Parse(json); }
        public Task<bool> IsActiveAuthenticationUidAsync(string uid) { return Task.FromResult(active.Contains(uid)); }
        public Task<Newtonsoft.Json.Linq.JToken> ReadAsync(string path)
        {
            Newtonsoft.Json.Linq.JToken value;
            if (records.TryGetValue(path, out value)) return Task.FromResult(value.DeepClone());
            var result = new Newtonsoft.Json.Linq.JObject(); string prefix = path.TrimEnd('/') + "/";
            foreach (var pair in records.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal)))
                result[pair.Key.Substring(prefix.Length)] = pair.Value.DeepClone();
            return Task.FromResult<Newtonsoft.Json.Linq.JToken>(result.HasValues ? result : null);
        }
        public async Task<Newtonsoft.Json.Linq.JToken> ReadEqualAsync(string path, string field, string value)
        {
            var all = await ReadAsync(path) as Newtonsoft.Json.Linq.JObject;
            var result = new Newtonsoft.Json.Linq.JObject();
            if (all != null) foreach (var p in all.Properties())
                if (string.Equals((string)(p.Value as Newtonsoft.Json.Linq.JObject)?[field], value, StringComparison.Ordinal)) result[p.Name] = p.Value.DeepClone();
            return result.HasValues ? result : null;
        }
        public Task<University_Internship_Management_System.Shared.StudentVersion> ReadVersionAsync(string path)
        {
            Newtonsoft.Json.Linq.JToken value; records.TryGetValue(path, out value);
            int version; if (!versions.TryGetValue(path, out version)) version = 0;
            return Task.FromResult(new University_Internship_Management_System.Shared.StudentVersion { Value = value as Newtonsoft.Json.Linq.JObject, ETag = value == null ? "null_etag" : "\"v" + version + "\"" });
        }
        public Task<bool> CompareWriteAsync(string path, object value, string etag)
        {
            if(path=="uimsAttendance" && ConflictAttendanceOnce){ConflictAttendanceOnce=false;return Task.FromResult(false);}
            int version; if (!versions.TryGetValue(path, out version)) version = 0;
            Newtonsoft.Json.Linq.JToken current; records.TryGetValue(path, out current);
            string expected = current == null ? "null_etag" : "\"v" + version + "\"";
            if (!string.Equals(expected, etag, StringComparison.Ordinal)) return Task.FromResult(false);
            records[path] = value as Newtonsoft.Json.Linq.JToken ?? Newtonsoft.Json.Linq.JToken.FromObject(value);
            versions[path] = version + 1;
            if(path=="uimsAttendance" && LoseAttendanceAcknowledgmentOnce){LoseAttendanceAcknowledgmentOnce=false;throw new UIMS.Services.FirebaseException(503,"Offline simulated lost attendance acknowledgement; retry the same operation.");}
            return Task.FromResult(true);
        }
    }

    private static async Task ApplicationPlacementLifecycleUsesAuthorizedRecords()
    {
        var db = new ApplicationMemoryDatabase();
        db.Enable("student-auth"); db.Enable("company-auth"); db.Enable("other-student-auth"); db.Enable("other-company-auth");
        db.Seed("users/student-auth", "{\"role\":\"Student\",\"isActive\":true,\"studentId\":\"ST-101\",\"department\":\"Computer Science\",\"academicSupervisorId\":\"AS-4\",\"fullName\":\"Fictional Student\"}");
        db.Seed("users/company-auth", "{\"role\":\"CompanySupervisor\",\"isActive\":true,\"companyId\":\"CO-9\",\"companySupervisorId\":\"CS-3\"}");
        db.Seed("users/other-student-auth", "{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-202\",\"department\":\"Computer Science\",\"academicSupervisorId\":\"AS-4\"}");
        db.Seed("users/other-company-auth", "{\"role\":\"companySupervisor\",\"isActive\":true,\"companyId\":\"CO-10\",\"companySupervisorId\":\"CS-10\"}");
        db.Seed("opportunities/OP-9", "{\"id\":\"OP-9\",\"companyId\":\"CO-9\",\"companyName\":\"Example Co\",\"status\":\"published\",\"department\":\"Computer Science\",\"deadline\":\"2030-01-01\",\"startDate\":\"2030-02-01\",\"durationWeeks\":12,\"requiredHours\":240,\"seats\":2,\"acceptedCount\":0}");
        var service = new UIMS.Services.ApplicationWorkflowService(db,()=>true);
        string id = await service.ApplyAsync("student-auth", Newtonsoft.Json.Linq.JObject.Parse("{\"opportunityId\":\"OP-9\",\"phone\":\"0790000000\",\"cvLink\":\"https://example.test/cv\"}"));
        Assert((string)(await db.ReadAsync("applications/" + id) as Newtonsoft.Json.Linq.JObject)?["studentId"] == "ST-101");
        await Reject<UIMS.Services.FirebaseException>(() => service.ApplyAsync("student-auth", Newtonsoft.Json.Linq.JObject.Parse("{\"opportunityId\":\"OP-9\",\"phone\":\"0790000000\",\"cvLink\":\"https://example.test/cv\"}")));
        await Reject<UIMS.Services.FirebaseException>(() => service.DecideAsync("other-company-auth", id, "accepted", ""));
        await service.DecideAsync("company-auth", id, "accepted", "Accepted for the placement");
        Assert((string)(await db.ReadAsync("applications/" + id) as Newtonsoft.Json.Linq.JObject)?["status"] == "accepted");
        await Reject<UIMS.Services.FirebaseException>(()=>service.ConfirmOfferAsync("student-auth",id));
        Assert(await db.ReadAsync("placementClaims/ST-101")==null && await db.ReadAsync("placements/PLC-"+id)==null);
        db.Seed("assignments/conflict","{\"studentId\":\"ST-101\",\"academicSupervisorId\":\"AS-OTHER\",\"active\":true}");
        await Reject<UIMS.Services.FirebaseException>(()=>service.ConfirmOfferAsync("student-auth",id));
        Assert(await db.ReadAsync("placementClaims/ST-101")==null);
        db.Seed("assignments/conflict","{\"studentId\":\"ST-101\",\"academicSupervisorId\":\"AS-OTHER\",\"active\":false}");
        string plannedId="PLC-"+id;
        var state=new JObject{["companies"]=new JObject{["CO-9"]=new JObject{["status"]="active"}},["companySupervisors"]=new JObject{["CS-3"]=new JObject{["status"]="active",["companyId"]="CO-9"}},
            ["schedules"]=new JObject{[plannedId]=new JObject{["2030-02-01"]=new JObject{["assignmentId"]=plannedId,["attendanceDate"]="2030-02-01",["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=true,["scheduledStartAt"]=DateTimeOffset.Parse("2030-02-01T09:00:00+03:00").ToUnixTimeMilliseconds(),["scheduledEndAt"]=DateTimeOffset.Parse("2030-02-01T16:00:00+03:00").ToUnixTimeMilliseconds()}}},["attendance"]=new JObject{["unchanged"]=new JObject{["minutes"]=2405}}};
        db.Seed("uimsAttendance",state.ToString());
        db.ConflictAttendanceOnce=true;db.LoseAttendanceAcknowledgmentOnce=true;
        await Reject<UIMS.Services.FirebaseException>(()=>service.ConfirmOfferAsync("student-auth",id));
        Assert((string)(await db.ReadAsync("applications/"+id))["status"]=="accepted");
        string placementId = await service.ConfirmOfferAsync("student-auth", id);
        Assert((string)(await db.ReadAsync("placements/" + placementId) as Newtonsoft.Json.Linq.JObject)?["studentId"] == "ST-101");
        Assert((string)(await db.ReadAsync("applications/" + id) as Newtonsoft.Json.Linq.JObject)?["status"] == "confirmed");
        Assert(await service.ConfirmOfferAsync("student-auth", id) == placementId);
        Assert(await new UIMS.Services.ApplicationWorkflowService(db,()=>false).ConfirmOfferAsync("student-auth",id)==placementId);
        var attendance=await db.ReadAsync("uimsAttendance");Assert((string)attendance["students"]["ST-101"]["firebaseUid"]=="student-auth" && (string)attendance["provisioningClaims"][plannedId]["assignmentId"]==plannedId);
        Assert(JToken.DeepEquals(state["schedules"],attendance["schedules"]) && JToken.DeepEquals(state["attendance"],attendance["attendance"]));
        ((JObject)attendance["provisioningClaims"]).Remove(plannedId);db.Seed("uimsAttendance",attendance.ToString());
        Assert(await service.ConfirmOfferAsync("student-auth",id)==plannedId && (await db.ReadAsync("uimsAttendance"))["provisioningClaims"][plannedId]!=null);
        await Reject<UIMS.Services.FirebaseException>(() => service.ConfirmOfferAsync("other-student-auth", id));
        await Reject<UIMS.Services.FirebaseException>(() => service.DecideAsync("company-auth", id, "rejected", "late"));
    }
    private sealed class TaskDatabaseHandler : HttpMessageHandler
    {
        public readonly Dictionary<string, Newtonsoft.Json.Linq.JToken> Records = new Dictionary<string, Newtonsoft.Json.Linq.JToken>(StringComparer.Ordinal);
        public int Requests;
        public bool AllRequestsAuthenticated = true;
        private readonly Dictionary<string, int> versions = new Dictionary<string, int>(StringComparer.Ordinal);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            Requests++;
            AllRequestsAuthenticated &= request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "isolated-test-token";
            string path = request.RequestUri.AbsolutePath.Trim('/');
            if (path.EndsWith(".json", StringComparison.Ordinal)) path = path.Substring(0, path.Length - 5);
            if (request.Method == HttpMethod.Get)
            {
                Newtonsoft.Json.Linq.JToken value;
                if (path == "tasks" || path == "users" || path == "placements" || path == "taskExams" || path == "fileAttachments" || (path.StartsWith("taskExamAnswerKeys/creationOperations/", StringComparison.Ordinal) && !Records.ContainsKey(path)))
                {
                    var all = new Newtonsoft.Json.Linq.JObject();
                    string prefix = path + "/";
                    foreach (var pair in Records.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal))) all[pair.Key.Substring(prefix.Length)] = pair.Value.DeepClone();
                    value = all;
                }
                else Records.TryGetValue(path, out value);
                var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(value == null ? "null" : value.ToString(Formatting.None)) };
                int version; if (!versions.TryGetValue(path, out version)) version = 0;
                result.Headers.TryAddWithoutValidation("ETag", "\"v" + version + "\"");
                return result;
            }
            if (request.Method == HttpMethod.Put)
            {
                string expected = request.Headers.Contains("if-match") ? request.Headers.GetValues("if-match").FirstOrDefault() : null;
                int version; if (!versions.TryGetValue(path, out version)) version = 0;
                bool exists = Records.ContainsKey(path);
                if ((expected == "null_etag" && exists) || (expected != null && expected != "null_etag" && expected != "\"v" + version + "\""))
                    return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
                Records[path] = Newtonsoft.Json.Linq.JToken.Parse(await request.Content.ReadAsStringAsync());
                versions[path] = version + 1;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") };
            }
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }
    }

    private static Dictionary<string, string> EnabledTaskSettings()
    {
        return new Dictionary<string, string>
        {
            { "FirebaseEnabled", "true" }, { "FirebaseAuthenticationEnabled", "true" },
            { "FirebaseProjectId", "offline-task-tests" }, { "FirebaseDatabaseUrl", OfflineFirebaseRoot },
            { "FirebaseApiKey", "offline-test-key" }, { "FirebaseLiveWritesEnabled", "true" },
            { "FirebaseSchemaValidated", "true" }, { "FirebaseTaskSchemaValidated", "true" },
            { "TasksEnabled", "true" }, { "TasksLiveWritesEnabled", "true" }, { "FirebaseTaskRootPath", "tasks" }
        };
    }

    private static UIMS.Tasks.TaskService OfflineTaskService(TaskDatabaseHandler handler, UIMS.Tasks.TaskActor actor, Dictionary<string, string> settings)
    {
        return new UIMS.Tasks.TaskService(key => settings.ContainsKey(key) ? settings[key] : null,
            new HttpClient(handler), actor, token => Task.FromResult("isolated-test-token"));
    }

    private static async Task TaskApiOfflineTests()
    {
        var studentProfile = Newtonsoft.Json.Linq.JObject.Parse("{\"role\":\"Student\",\"isActive\":true,\"studentId\":\"ST-1\",\"fullName\":\"Fictional Student\"}");
        var companyProfile = Newtonsoft.Json.Linq.JObject.Parse("{\"role\":\"CompanySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\"}");
        var student = UIMS.Tasks.TaskActor.FromVerifiedProfile("student-uid", "student", studentProfile);
        var company = UIMS.Tasks.TaskActor.FromVerifiedProfile("company-uid", "companySupervisor", companyProfile);
        Assert(student.Uid == "student-uid" && student.StudentId == "ST-1" && student.Role == "student");
        Assert(company.Uid == "company-uid" && company.CompanySupervisorId == "SUP-1" && company.CompanyId == "CO-1");
        var mappedCompany = UIMS.Tasks.TaskActor.FromVerifiedProfile("company-uid", "company", new Newtonsoft.Json.Linq.JObject { ["role"] = "companySupervisor", ["isActive"] = true, ["companyId"] = "CO-1" }, "SUP-MAPPED");
        Assert(mappedCompany.CompanySupervisorId == "SUP-MAPPED");
        await Reject<UIMS.Services.FirebaseException>(() => Task.FromResult(UIMS.Tasks.TaskActor.FromVerifiedProfile("inactive", "student", new Newtonsoft.Json.Linq.JObject { ["role"] = "student", ["studentId"] = "ST-X", ["isActive"] = false })));
        await Reject<UIMS.Services.FirebaseException>(() => Task.FromResult(UIMS.Tasks.TaskActor.FromVerifiedProfile("student-uid", "companySupervisor", studentProfile)));

        var settings = EnabledTaskSettings();
        var db = new TaskDatabaseHandler();
        db.Records["placements/place-1"] = Newtonsoft.Json.Linq.JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\",\"status\":\"confirmed\"}");
        db.Records["users/student-uid"] = studentProfile;
        var created = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "create",
            Newtonsoft.Json.Linq.JObject.Parse("{\"title\":\"Fictional task\",\"description\":\"Offline test\",\"deadline\":\"2026-12-01\",\"placementId\":\"place-1\",\"criteria\":\"Acceptance criteria\",\"maximum\":100}"), CancellationToken.None);
        string taskId = (string)created["id"];
        var assigned = db.Records["tasks/" + taskId] as Newtonsoft.Json.Linq.JObject;
        Assert(!string.IsNullOrWhiteSpace(taskId) && (string)assigned["studentId"] == "ST-1" && (string)assigned["assignedBy"] == "SUP-1" && (string)assigned["placementId"] == "place-1");
        await Reject<UIMS.Services.FirebaseException>(() => UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "POST", "create", new Newtonsoft.Json.Linq.JObject(), CancellationToken.None));
        var wrongStudent = UIMS.Tasks.TaskActor.FromVerifiedProfile("student-2", "student", new Newtonsoft.Json.Linq.JObject { ["role"] = "student", ["isActive"] = true, ["studentId"] = "ST-2" });
        await Reject<UIMS.Services.FirebaseException>(() => UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, wrongStudent, settings), "POST", "start", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId }, CancellationToken.None));
        var wrongCompany = UIMS.Tasks.TaskActor.FromVerifiedProfile("company-2", "companySupervisor", new Newtonsoft.Json.Linq.JObject { ["role"] = "companySupervisor", ["isActive"] = true, ["companySupervisorId"] = "SUP-2" });
        await Reject<UIMS.Services.FirebaseException>(() => UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, wrongCompany, settings), "POST", "review", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["reviewAction"] = "grade" }, CancellationToken.None));

        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "POST", "start", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId }, CancellationToken.None);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "POST", "submit", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["description"] = "First fictional submission", ["link"] = "https://example.test/first" }, CancellationToken.None);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "review", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["reviewAction"] = "revision", ["feedback"] = "Please revise", ["deadline"] = DateTimeOffset.UtcNow.AddDays(1).ToString("o") }, CancellationToken.None);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "POST", "submit", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["description"] = "Revised fictional submission", ["link"] = "https://example.test/revision" }, CancellationToken.None);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "review", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["reviewAction"] = "grade", ["score"] = 92, ["feedback"] = "Passed" }, CancellationToken.None);
        var final = db.Records["tasks/" + taskId] as Newtonsoft.Json.Linq.JObject;
        Assert((string)final["status"] == "Completed" && (int)final["score"] == 92 && ((Newtonsoft.Json.Linq.JArray)final["submissions"]).Count == 2);
        var studentView = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "GET", null, null, CancellationToken.None);
        var companyView = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "GET", null, null, CancellationToken.None);
        Assert(((Newtonsoft.Json.Linq.JArray)studentView["tasks"]).Any(x => (string)x["id"] == taskId));
        Assert(((Newtonsoft.Json.Linq.JArray)companyView["tasks"]).Any(x => (string)x["id"] == taskId));
        Assert(db.AllRequestsAuthenticated && db.Requests > 0);
        db.Records["placements/other-company"] = Newtonsoft.Json.Linq.JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-OTHER\",\"status\":\"confirmed\"}");
        db.Records["placements/unconfirmed"] = Newtonsoft.Json.Linq.JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\",\"status\":\"pending\"}");
        var trainees = await OfflineTaskService(db, company, settings).LoadTraineesAsync(CancellationToken.None);
        Assert(trainees["trainees"].Count() == 1 && (int)trainees["statistics"]["total"] == 1 && (int)trainees["statistics"]["assignedTasks"] == 1);
        Assert((string)trainees["trainees"][0]["placementId"] == "place-1" && (int)trainees["trainees"][0]["completedTasks"] == 1);
        var eligible = await OfflineTaskService(db, company, settings).LoadAsync("companySupervisor", CancellationToken.None);
        Assert(eligible["students"].Count() == 1 && (string)eligible["students"][0]["placementId"] == "place-1");
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, student, settings).LoadTraineesAsync(CancellationToken.None));


        var denied = new Handler { Respond = request => new HttpResponseMessage(HttpStatusCode.Forbidden) };
        // Exercise the service's Rules-denial translation with an isolated HTTP response.
        var deniedDirect = new UIMS.Tasks.TaskService(key => settings.ContainsKey(key) ? settings[key] : null,
            new HttpClient(denied), student, token => Task.FromResult("isolated-test-token"));
        try { await UIMS.Tasks.TasksApiHandler.DispatchAsync(deniedDirect, "GET", null, null, CancellationToken.None); throw new Exception("Firebase permission denial was accepted."); }
        catch (UIMS.Services.FirebaseException error) { Assert(error.StatusCode == 403); }
    }

    private static async Task LoginUsesTrustedAuthResponseUid()
    {
        int requests = 0;
        var handler = new Handler
        {
            Respond = request =>
            {
                requests++;
                Assert(request.RequestUri.AbsolutePath.EndsWith("/accounts:signInWithPassword", StringComparison.Ordinal));
                return Response("{\"idToken\":\"offline-id-token\",\"localId\":\"offline-auth-uid\",\"refreshToken\":\"offline-refresh\",\"expiresIn\":\"3600\"}");
            }
        };
        int tokenLookups = 0;
        var configuration = new UIMS.Services.FirebaseConfiguration("false", "offline-test", OfflineFirebaseRoot,
            "offline-test-key", "false", "false", "false", "true");
        var service = new University_Internship_Management_System.Shared.FirebaseSignInService(
            new HttpClient(handler), "offline-test-key", token => { tokenLookups++; return Task.FromResult("offline-auth-uid"); },
            uid => Task.FromResult(new Newtonsoft.Json.Linq.JObject
            { ["isActive"] = true, ["role"] = "coordinator", ["accountSetupStatus"] = "complete" }), configuration);
        var result = await service.SignInAsync("coord@example.test", "not-a-real-password");
        Assert(result.Uid == "offline-auth-uid" && result.Role == "coordinator");
        Assert(requests == 1 && tokenLookups == 0);
    }

    private static async Task AuthenticationTransientFailuresKeepTheirStatus()
    {
        var config = new UIMS.Services.FirebaseConfiguration("false", "offline-test", OfflineFirebaseRoot, "offline-key", "false", "false", "false", "true");
        foreach (var status in new[] { 400, 429, 503 })
        {
            int requests = 0;
            var handler = new Handler { Respond = request => { requests++; var response = new HttpResponseMessage((System.Net.HttpStatusCode)status) { Content = new StringContent("{\"error\":{\"message\":\"" + (status == 503 ? "UNAVAILABLE" : "TOO_MANY_ATTEMPTS_TRY_LATER") + "\"}}") }; response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(120)); return response; } };
            var service = new University_Internship_Management_System.Shared.FirebaseSignInService(new HttpClient(handler), "offline-key", token => Task.FromResult("test-uid"), uid => Task.FromResult(new Newtonsoft.Json.Linq.JObject()), config);
            foreach (var refresh in new[] { false, true })
            {
                try { if (refresh) await service.RefreshAsync("isolated-refresh"); else await service.SignInAsync("user@example.test", "isolated-password"); throw new Exception("Transient error was accepted."); }
                catch (University_Internship_Management_System.Shared.FirebaseLoginException error)
                { Assert(error.StatusCode == (status == 503 ? 503 : 429)); Assert(error.RetryAfterSeconds == (status == 503 ? 0 : 120)); }
            }
            Assert(requests == 2); // One provider request per action, no automatic credential retries.
        }
    }

    private static int Main(string[] args)
    {
        if(args.Length==3 && args[0]=="--database-export-readonly")
        {try{return ExportDatabaseReadOnly(args[1],args[2]).GetAwaiter().GetResult();}catch(Exception error){Console.WriteLine("Read-only export blocked: "+error.GetType().Name+". No production writes.");return 2;}}
        if (args.Length == 3 && args[0] == "--student-integration-readonly")
        {
            try { return StudentIntegrationAudit(args[1],args[2]).GetAwaiter().GetResult(); }
            catch(Exception error) { Console.WriteLine("Read-only audit blocked: " + error.GetType().Name + ". No account status inferred; no production writes. Inspect authorized server diagnostics.");return 2; }
        }
        if (args.Length == 2 && args[0] == "--final-report-readonly-preflight") return FinalReportPreflight(args[1]).GetAwaiter().GetResult();
        try { Run().GetAwaiter().GetResult(); Console.WriteLine(passed + " tests passed; " + failed + " failed. No network requests or Firebase writes were made."); return failed == 0 ? 0 : 1; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static async Task Run()
    {
        await RunAcademicAssignmentTests();
        await Test("existing Firebase role aliases normalize without schema changes", () => {
            Func<string, string> normalize = University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole;
            Assert(normalize("Student") == "student");
            Assert(normalize("AcademicSupervisor") == "academicSupervisor");
            Assert(normalize("academic_supervisor") == "academicSupervisor");
            Assert(normalize("company") == "companySupervisor");
            Assert(normalize("admin") == "coordinator");
            Assert(normalize("unknown") == null);
            return Task.CompletedTask;
        });
        await Test("Firebase login endpoint uses ASP.NET async handler and does not block login requests", () => {
            var handler = typeof(UIMS.FirebaseApiHandler);
            Assert(typeof(System.Web.HttpTaskAsyncHandler).IsAssignableFrom(handler));
            var process = handler.GetMethod("ProcessRequestAsync", new[] { typeof(System.Web.HttpContext) });
            Assert(process != null && process.ReturnType == typeof(Task));
            return Task.CompletedTask;
        });
        await Test("sign-in trusts Firebase-issued UID and avoids a duplicate auth lookup", LoginUsesTrustedAuthResponseUid);
        await Test("authentication throttling preserves 429 Retry-After and transient refresh errors never masquerade as revoked sessions", AuthenticationTransientFailuresKeepTheirStatus);
        await Test("Firebase disabled blocks all service transports before HTTP", FirebaseDisabledBlocksNetwork);
        await Test("opportunity reads fail closed before Firebase transport when disabled", OpportunityServiceHonorsDisabledFirebase);
        await Test("applications, company decisions, and placement confirmation preserve authorization and idempotency", ApplicationPlacementLifecycleUsesAuthorizedRecords);
        await Test("task API resolves roles and executes authorized task lifecycle with mocked Firebase REST", TaskApiOfflineTests);
        await Test("expired task sessions return JSON 401 without a login-page redirect", async () => {
            var output = new System.IO.StringWriter();
            var context = new System.Web.HttpContext(new System.Web.HttpRequest("", "http://localhost/TasksApi.ashx", ""), new System.Web.HttpResponse(output));
            context.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(""), new string[0]);
            await new UIMS.Tasks.TasksApiHandler().ProcessRequestAsync(context);
            Assert(context.Response.StatusCode == 401 && context.Response.SuppressFormsAuthenticationRedirect);
            Assert(context.Response.ContentType == "application/json" && (string)Newtonsoft.Json.Linq.JObject.Parse(output.ToString())["message"] == "Your sign-in expired. Sign in again to load your tasks.");
        });
        await Test("recreated student without placements receives empty task arrays", async () => {
            var db = new TaskDatabaseHandler();
            var student = UIMS.Tasks.TaskActor.FromVerifiedProfile("new-student-uid", "student", Newtonsoft.Json.Linq.JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"NEW-STUDENT\"}"));
            db.Records["tasks/foreign"] = TaskFixture("foreign", "Assigned", null, "OTHER-STUDENT", "DELETED-PLACEMENT");
            var result = await OfflineTaskService(db, student, EnabledTaskSettings()).LoadAsync("student", CancellationToken.None);
            Assert(((Newtonsoft.Json.Linq.JArray)result["tasks"]).Count == 0 && ((Newtonsoft.Json.Linq.JArray)result["submissions"]).Count == 0);
        });
        await Test("task submissions preserve array and keyed-object history and enforce ownership/concurrency", TaskSubmissionCompatibilityTests);
        await Test("task board positions persist independently with authorization, retries, ordering and ETag conflicts", TaskBoardTests);
        await RunFirebaseFileStorageTests();
        await Test("logout expires current identity, role and first-login cookies without expiring another role", () => {
            var oldContext=System.Web.HttpContext.Current;var oldPrincipal=System.Threading.Thread.CurrentPrincipal;
            try {
                var context=new System.Web.HttpContext(new AttendancePostWorker());System.Web.HttpContext.Current=context;
                context.User=new System.Security.Principal.GenericPrincipal(new System.Web.Security.FormsIdentity(new System.Web.Security.FormsAuthenticationTicket(2,"current-student",DateTime.Now,DateTime.Now.AddMinutes(30),false,"student")),new[]{"student"});
                University_Internship_Management_System.Shared.LoginSession.End(context);
                foreach(string name in University_Internship_Management_System.Shared.LoginSession.CookieNames("student")){var cookie=context.Response.Cookies[name];Assert(cookie!=null && cookie.Value=="" && cookie.Expires<DateTime.Now && cookie.HttpOnly);}
                Assert(!context.Response.Cookies.AllKeys.Contains(".UIMS.Role.companySupervisor") && !context.User.Identity.IsAuthenticated);
            } finally {System.Web.HttpContext.Current=oldContext;System.Threading.Thread.CurrentPrincipal=oldPrincipal;}
            return Task.CompletedTask;
        });
        await Test("authenticated attendance admits linked historical test labels but denies foreign student and supervisor writes and student scans", AuthenticatedAttendanceScopeTests);
        await Test("student and supervisor issue signed QR without recording attendance or altering approved minutes; foreign UID and premature checkout rejected", StudentQrIssuanceTests);
        await Test("supervisor QR actions allow before/after shift hours while enforcing training days and check-in/out order", () =>
        {
            string day=UIMS.Attendance.AttendanceService.Today();
            long start=DateTimeOffset.Parse(day+"T09:00:00+03:00").ToUnixTimeMilliseconds(),end=DateTimeOffset.Parse(day+"T16:00:00+03:00").ToUnixTimeMilliseconds();
            var context=new UIMS.Attendance.AttendanceContext{AssignmentId="A1",Date=day,Assignment=new JObject{["studentId"]="S1",["companyId"]="C1",["companySupervisorId"]="CS1",["startDate"]=day,["endDate"]=day},Record=new JObject(),Schedule=new JObject{["assignmentId"]="A1",["attendanceDate"]=day,["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=true,["scheduledStartAt"]=start,["scheduledEndAt"]=end}};
            UIMS.Attendance.AttendanceService.CheckAction(context,"checkIn",start-3600000);
            UIMS.Attendance.AttendanceService.CheckAction(context,"checkIn",end+3600000);
            Action<string> denied=action=>{try{UIMS.Attendance.AttendanceService.CheckAction(context,action,UIMS.Attendance.AttendanceService.Now());}catch(InvalidOperationException){return;}throw new Exception("Invalid attendance transition accepted.");};
            denied("checkOut");denied("invalid");
            context.Record=new JObject{["assignmentId"]="A1",["attendanceDate"]=day,["studentId"]="S1",["companyId"]="C1",["companySupervisorId"]="CS1",["checkInAt"]=UIMS.Attendance.AttendanceService.Now()-60000};
            UIMS.Attendance.AttendanceService.CheckAction(context,"checkOut",UIMS.Attendance.AttendanceService.Now());denied("checkIn");
            context.Schedule["isTrainingDay"]=false;denied("checkOut");
            return Task.CompletedTask;
        });
        await Test("attendance onboarding preserves history/calendar, rejects cross-student conflicts, freezes 2023301 and is idempotent", AttendanceOnboardingBoundaryTests);
        await Test("Auth audit keeps permission failures unverified and reconciliation does not infer enabled users", () =>
        {
            var denied=UIMS.Services.StudentAuthAuditFailure.Describe(403,"","offline-project","PermissionDenied");Assert(!(bool)denied["verified"] && (string)denied["state"]=="unverified" && (string)denied["requiredPermission"]=="firebaseauth.users.get");
            Assert((string)UIMS.Services.StudentAuthAuditFailure.Describe(403,"ACCESS_TOKEN_SCOPE_INSUFFICIENT","offline","PermissionDenied")["category"]=="oauthScope");
            var snapshot=JObject.Parse("{\"users\":{\"uid1\":{\"studentId\":\"S1\",\"role\":\"student\",\"isActive\":true}},\"placements\":{},\"uimsAttendance\":{\"students\":{\"2023301\":{\"status\":\"active\"}}}}");
            var before=snapshot.ToString();var report=ReconcileStudentSnapshot(snapshot,new JObject{["uid1"]=denied},DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Assert((int)report["studentCount"]==2 && (int)report["counts"]["ready"]==0 && (int)report["counts"]["authUnverified"]==2 && snapshot.ToString()==before);
            Assert(report["students"].OfType<JObject>().Single(r=>(string)r["studentId"]=="2023301").Value<bool>("frozen"));
            return Task.CompletedTask;
        });
        await Test("attendance absence derivation rejects malformed, future and out-of-assignment calendars without writes", () =>
        {
            var assignment=JObject.Parse("{\"startDate\":\"2026-10-01\",\"endDate\":\"2026-10-31\"}");
            long start=DateTimeOffset.Parse("2026-10-23T09:00:00+03:00").ToUnixTimeMilliseconds();
            long end=DateTimeOffset.Parse("2026-10-23T16:00:00+03:00").ToUnixTimeMilliseconds();
            var schedule=new JObject{["assignmentId"]="TA-OFFLINE",["attendanceDate"]="2026-10-23",["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=true,["scheduledStartAt"]=start,["scheduledEndAt"]=end};
            Func<JObject,JObject,long,bool> eligible=(s,a,n)=>UIMS.Attendance.AttendanceService.CanDeriveScheduleHistory(s,a,"TA-OFFLINE","2026-10-23",n);
            Assert(eligible(schedule,assignment,end+1));Assert(!eligible(schedule,assignment,end));Assert(!eligible(schedule,assignment,start-86400000));
            foreach(var field in new[]{"assignmentId","attendanceDate","timeZone","version","scheduledStartAt","scheduledEndAt","isTrainingDay"})
            {var broken=(JObject)schedule.DeepClone();broken[field]="invalid";Assert(!eligible(broken,assignment,end+1));}
            var outside=(JObject)assignment.DeepClone();outside["startDate"]="2026-10-24";Assert(!eligible(schedule,outside,end+1));
            var dayOff=(JObject)schedule.DeepClone();dayOff["isTrainingDay"]=false;dayOff.Remove("scheduledStartAt");dayOff.Remove("scheduledEndAt");Assert(eligible(dayOff,assignment,end+1));
            Assert(!eligible(schedule,null,end+1));return Task.CompletedTask;
        });
        await RunTaskExamTests();
        await RunFinalReportTests();
        await RunWeeklyReportTests();
        await RunFirebaseDemoTests();
        await RunPercentageRequirementsTests();
        await RunPercentageSecurityTests();
        await RunReadinessTests();
        await RunDecisionTests();
        await RunCompletionReportTests();
        await RunStudentCompletionTests();
    }
}
