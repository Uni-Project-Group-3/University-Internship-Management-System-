using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private const string OfflineFirebaseRoot = "https://uims-offline-tests-default-rtdb.firebaseio.com/";
    private static FirebaseFinalEvaluationConfiguration OfflineFirebaseConfiguration() { return new FirebaseFinalEvaluationConfiguration(OfflineFirebaseRoot); }
    private static int passed;
    private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
    private static async Task Reject<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static async Task Test(string name, Func<Task> test) { await test(); passed++; Console.WriteLine("PASS " + name); }
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
            int version; if (!versions.TryGetValue(path, out version)) version = 0;
            Newtonsoft.Json.Linq.JToken current; records.TryGetValue(path, out current);
            string expected = current == null ? "null_etag" : "\"v" + version + "\"";
            if (!string.Equals(expected, etag, StringComparison.Ordinal)) return Task.FromResult(false);
            records[path] = value as Newtonsoft.Json.Linq.JToken ?? Newtonsoft.Json.Linq.JToken.FromObject(value);
            versions[path] = version + 1; return Task.FromResult(true);
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
        var service = new UIMS.Services.ApplicationWorkflowService(db);
        string id = await service.ApplyAsync("student-auth", Newtonsoft.Json.Linq.JObject.Parse("{\"opportunityId\":\"OP-9\",\"phone\":\"0790000000\",\"cvLink\":\"https://example.test/cv\"}"));
        Assert((string)(await db.ReadAsync("applications/" + id) as Newtonsoft.Json.Linq.JObject)?["studentId"] == "ST-101");
        await Reject<UIMS.Services.FirebaseException>(() => service.ApplyAsync("student-auth", Newtonsoft.Json.Linq.JObject.Parse("{\"opportunityId\":\"OP-9\",\"phone\":\"0790000000\",\"cvLink\":\"https://example.test/cv\"}")));
        await Reject<UIMS.Services.FirebaseException>(() => service.DecideAsync("other-company-auth", id, "accepted", ""));
        await service.DecideAsync("company-auth", id, "accepted", "Accepted for the placement");
        Assert((string)(await db.ReadAsync("applications/" + id) as Newtonsoft.Json.Linq.JObject)?["status"] == "accepted");
        string placementId = await service.ConfirmOfferAsync("student-auth", id);
        Assert((string)(await db.ReadAsync("placements/" + placementId) as Newtonsoft.Json.Linq.JObject)?["studentId"] == "ST-101");
        Assert((string)(await db.ReadAsync("applications/" + id) as Newtonsoft.Json.Linq.JObject)?["status"] == "confirmed");
        Assert(await service.ConfirmOfferAsync("student-auth", id) == placementId);
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
                if (path == "tasks" || path == "users" || path == "placements")
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
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "review", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["reviewAction"] = "revision", ["feedback"] = "Please revise" }, CancellationToken.None);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "POST", "submit", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["description"] = "Revised fictional submission", ["link"] = "https://example.test/revision" }, CancellationToken.None);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "review", new Newtonsoft.Json.Linq.JObject { ["assignmentId"] = taskId, ["reviewAction"] = "grade", ["score"] = 92, ["feedback"] = "Passed" }, CancellationToken.None);
        var final = db.Records["tasks/" + taskId] as Newtonsoft.Json.Linq.JObject;
        Assert((string)final["status"] == "Completed" && (int)final["score"] == 92 && ((Newtonsoft.Json.Linq.JArray)final["submissions"]).Count == 2);
        var studentView = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "GET", null, null, CancellationToken.None);
        var companyView = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "GET", null, null, CancellationToken.None);
        Assert(((Newtonsoft.Json.Linq.JArray)studentView["tasks"]).Any(x => (string)x["id"] == taskId));
        Assert(((Newtonsoft.Json.Linq.JArray)companyView["tasks"]).Any(x => (string)x["id"] == taskId));
        Assert(db.AllRequestsAuthenticated && db.Requests > 0);

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

    private static int Main()
    {
        try { Run().GetAwaiter().GetResult(); Console.WriteLine(passed + " tests passed. No network requests or Firebase writes were made."); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static async Task Run()
    {
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
        await Test("Firebase disabled blocks all service transports before HTTP", FirebaseDisabledBlocksNetwork);
        await Test("opportunity reads fail closed before Firebase transport when disabled", OpportunityServiceHonorsDisabledFirebase);
        await Test("applications, company decisions, and placement confirmation preserve authorization and idempotency", ApplicationPlacementLifecycleUsesAuthorizedRecords);
        await Test("task API resolves roles and executes authorized task lifecycle with mocked Firebase REST", TaskApiOfflineTests);
        await Test("task submissions preserve array and keyed-object history and enforce ownership/concurrency", TaskSubmissionCompatibilityTests);
        await Test("task board positions persist independently with authorization, retries, ordering and ETag conflicts", TaskBoardTests);
        await RunFirebaseFileStorageTests();
        await RunTaskExamTests();
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
