using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class LiveReadDatabase : IApplicationDatabase
    {
        internal readonly Dictionary<string, JToken> Rows = new Dictionary<string, JToken>();
        internal int Reads;
        internal bool Active = true;
        public Task<bool> IsActiveAuthenticationUidAsync(string uid) { Reads++; return Task.FromResult(Active); }
        public Task<JToken> ReadAsync(string path) { Reads++; JToken row; return Task.FromResult(Rows.TryGetValue(path, out row) ? row.DeepClone() : null); }
        public Task<JToken> ReadEqualAsync(string path, string field, string value)
        {
            Reads++; var result = new JObject();
            foreach (var row in Rows.Where(x => x.Key.StartsWith(path + "/") && !x.Key.Substring(path.Length + 1).Contains("/")))
                if ((string)row.Value[field] == value) result[row.Key.Substring(path.Length + 1)] = row.Value.DeepClone();
            return Task.FromResult<JToken>(result);
        }
        public Task<StudentVersion> ReadVersionAsync(string path) { throw new Exception("Unexpected write preparation"); }
        public Task<bool> CompareWriteAsync(string path, object value, string etag) { throw new Exception("Unexpected write"); }
    }
    private static HttpContext LiveContext(string role)
    {
        var context = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
        var ticket = new FormsAuthenticationTicket(2, "test-uid", DateTime.Now.AddMinutes(-1), DateTime.Now.AddMinutes(10), false, role);
        context.User = new GenericPrincipal(new FormsIdentity(ticket), new[] { role }); return context;
    }
    private static LiveReadDatabase LiveDatabase(string role)
    {
        var db = new LiveReadDatabase();
        db.Rows["users/test-uid"] = JObject.Parse("{role:'" + role + "',isActive:true,accountSetupStatus:'complete',studentId:'test-student',companyId:'test-company',companySupervisorId:'test-supervisor',fullName:'Fictional offline user'}");
        db.Rows["placements/test-placement"] = JObject.Parse("{status:'confirmed',studentId:'test-student',studentUid:'test-uid',companyId:'test-company',companySupervisorId:'test-supervisor',academicSupervisorId:'test-uid'}");
        return db;
    }
    private static Task<LiveEvaluationSources> LiveSource(HttpContext context, string role, LiveReadDatabase db)
    { return FinalEvaluationFirebaseIntegration.CreateSourcesAsync(context, role, db, new InMemoryFinalEvaluationRepository(new[] { Policy() })); }
    private static async Task RunLiveIntegrationTests()
    {
        await Test("live adapter rejects anonymous identity before privileged I/O", async () => {
            var db = LiveDatabase("student"); var context = LiveContext("student"); context.User = new GenericPrincipal(new GenericIdentity("test-uid"), new[] { "student" });
            await Reject<UnauthorizedAccessException>(() => LiveSource(context, "student", db)); Assert(db.Reads == 0);
        });
        await Test("live adapter checks signed role before privileged I/O", async () => {
            var db = LiveDatabase("student"); await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("student"), "companySupervisor", db)); Assert(db.Reads == 0);
        });
        await Test("live adapter rejects disabled Authentication user", async () => {
            var db = LiveDatabase("student"); db.Active = false; await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("student"), "student", db));
        });
        await Test("live adapter rejects inactive profile", async () => {
            var db = LiveDatabase("student"); db.Rows["users/test-uid"]["isActive"] = false; await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("student"), "student", db));
        });
        await Test("live Company reader excludes another company and rechecks assignment", async () => {
            var db = LiveDatabase("companySupervisor"); db.Rows["placements/foreign"] = db.Rows["placements/test-placement"].DeepClone(); db.Rows["placements/foreign"]["companyId"] = "other-company";
            var source = await LiveSource(LiveContext("companySupervisor"), "companySupervisor", db); Assert(source.Placements.Length == 1);
            Assert((await source.DetailsAsync("test-placement")).Contains("test-student"));
            db.Rows["placements/test-placement"]["companySupervisorId"] = "other-supervisor";
            await Reject<UnauthorizedAccessException>(() => source.DetailsAsync("test-placement"));
        });
        await Test("live Student reader excludes mismatching Firebase UID", async () => {
            var db = LiveDatabase("student"); db.Rows["placements/test-placement"]["studentUid"] = "other-uid";
            Assert((await LiveSource(LiveContext("student"), "student", db)).Placements.Length == 0);
        });
        await Test("live Academic reader honors revoked assignment", async () => {
            var db = LiveDatabase("academicSupervisor"); db.Rows["supervisorAssignments/test-student"] = JObject.Parse("{studentId:'test-student',academicSupervisorId:'test-uid',active:false}");
            Assert((await LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", db)).Placements.Length == 0);
        });
        await Test("live Coordinator role does not imply global scope", async () => {
            var db = LiveDatabase("coordinator"); await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", db));
        });
        await Test("live reader never invents policy or task publication", async () => {
            var source = await LiveSource(LiveContext("student"), "student", LiveDatabase("student"));
            Assert(string.IsNullOrEmpty(source.Placements.Single().PolicyId));
            Assert(await ((IStudentVisibleTaskReader)source).GetAsync("test-placement") == null);
            await Reject<UnauthorizedAccessException>(() => source.GetAsync("other-placement"));
        });
        await Test("live attendance reader separates approved pending and rejected hours", async () => {
            var db = LiveDatabase("student");
            db.Rows["uimsAttendance/students/test-student"] = JObject.Parse("{status:'active',firebaseUid:'test-uid',activeAssignmentId:'test-assignment'}");
            db.Rows["uimsAttendance/trainingAssignments/test-assignment"] = JObject.Parse("{status:'active',studentId:'test-student',companyId:'test-company',companySupervisorId:'test-supervisor',requiredHours:120}");
            var rows = new JObject(); long start = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();
            foreach (string status in new[] { "approved", "pending", "rejected" })
                rows[status] = new JObject { ["studentId"] = "test-student", ["assignmentId"] = "test-assignment", ["companyId"] = "test-company", ["companySupervisorId"] = "test-supervisor", ["status"] = "completed", ["checkInAt"] = start, ["checkOutAt"] = start + 3600000, ["workedMinutes"] = 60, ["hoursApprovalStatus"] = status, ["hoursApprovedById"] = "test-supervisor" };
            db.Rows["uimsAttendance/attendance/test-assignment"] = rows;
            var source = await LiveSource(LiveContext("student"), "student", db);
            Assert((await ((IApprovedTrainingHoursReader)source).GetAsync("test-placement")).Value.Sum(x => x.Hours) == 1);
            Assert((await source.GetPendingHoursAsync("test-placement")).Value == 1);
            Assert((await ((IRequiredTrainingHoursReader)source).GetAsync("test-placement")).Value == 120);
        });
        await Test("live attendance reader rejects conflicting required hours", async () => {
            var db = LiveDatabase("student"); db.Rows["placements/test-placement"]["requiredHours"] = 100;
            db.Rows["uimsAttendance/students/test-student"] = JObject.Parse("{status:'active',firebaseUid:'test-uid',activeAssignmentId:'test-assignment'}");
            db.Rows["uimsAttendance/trainingAssignments/test-assignment"] = JObject.Parse("{status:'active',studentId:'test-student',companyId:'test-company',companySupervisorId:'test-supervisor',requiredHours:120}");
            var source = await LiveSource(LiveContext("student"), "student", db);
            await Reject<EvaluationIntegrationUnavailableException>(() => ((IRequiredTrainingHoursReader)source).GetAsync("test-placement"));
        });
    }
}
