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
        internal bool RequireQueryIndexes;
        internal string FailReadPath;
        public Task<bool> IsActiveAuthenticationUidAsync(string uid) { Reads++; return Task.FromResult(Active); }
        public Task<JToken> ReadAsync(string path)
        {
            if (string.Equals(path, FailReadPath, StringComparison.Ordinal)) throw new InvalidOperationException("Isolated read failure.");
            Reads++; JToken row;
            if (Rows.TryGetValue(path, out row)) return Task.FromResult(row.DeepClone());
            var result = new JObject(); string prefix = path.TrimEnd('/') + "/";
            foreach (var item in Rows.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal) && !x.Key.Substring(prefix.Length).Contains("/")))
                result[item.Key.Substring(prefix.Length)] = item.Value.DeepClone();
            return Task.FromResult<JToken>(result.HasValues ? result : null);
        }
        public Task<JToken> ReadEqualAsync(string path, string field, string value)
        {
            if (RequireQueryIndexes) throw new FirebaseQueryIndexException();
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
    private static async Task PageFactoriesRecoverWhenApplicationStartWasMissed()
    {
        var percentage = PercentagePageIntegration.Factory;
        var coordinator = CoordinatorReviewPageIntegration.Factory;
        var coordinatorAsync = CoordinatorReviewPageIntegration.AsyncFactory;
        var student = StudentCompletionPageIntegration.Factory;
        try
        {
            PercentagePageIntegration.Factory = null;
            CoordinatorReviewPageIntegration.Factory = null;
            CoordinatorReviewPageIntegration.AsyncFactory = null;
            StudentCompletionPageIntegration.Factory = null;

            await Reject<UnauthorizedAccessException>(() => PercentagePageIntegration.Resolve(null, false));
            await Reject<UnauthorizedAccessException>(() => CoordinatorReviewPageIntegration.ResolveAsync(null));
            await Reject<UnauthorizedAccessException>(() => StudentCompletionPageIntegration.ResolveAsync(null));
            Assert(PercentagePageIntegration.Factory != null);
            Assert(CoordinatorReviewPageIntegration.AsyncFactory != null);
            Assert(StudentCompletionPageIntegration.Factory != null);
        }
        finally
        {
            PercentagePageIntegration.Factory = percentage;
            CoordinatorReviewPageIntegration.Factory = coordinator;
            CoordinatorReviewPageIntegration.AsyncFactory = coordinatorAsync;
            StudentCompletionPageIntegration.Factory = student;
        }
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
    private static async Task<T> Capture<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T error) { return error; }
        throw new Exception("Expected " + typeof(T).Name + ".");
    }
    private static async Task RunLiveIntegrationTests()
    {
        await Test("Firebase page factories self-register if Application_Start was missed, then still require trusted identity", PageFactoriesRecoverWhenApplicationStartWasMissed);
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
        await Test("Academic authorization failures expose safe categories without identity values", async () => {
            var wrongRole = LiveDatabase("student");
            var error = await Capture<EvaluationAuthorizationFailureException>(() => LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", wrongRole));
            Assert(error.ReasonCode == "profile_role_mismatch" && !error.Message.Contains("test-uid"));
            var missingProfile = LiveDatabase("academicSupervisor"); missingProfile.Rows.Remove("users/test-uid");
            error = await Capture<EvaluationAuthorizationFailureException>(() => LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", missingProfile));
            Assert(error.ReasonCode == "profile_missing" && !error.Message.Contains("test-uid"));
        });
        await Test("live Company reader excludes another company and rechecks assignment", async () => {
            var db = LiveDatabase("companySupervisor"); db.Rows["placements/foreign"] = db.Rows["placements/test-placement"].DeepClone(); db.Rows["placements/foreign"]["companyId"] = "other-company";
            var source = await LiveSource(LiveContext("companySupervisor"), "companySupervisor", db); Assert(source.Placements.Length == 1);
            Assert((await source.DetailsAsync("test-placement")).Contains("test-student"));
            db.Rows["placements/test-placement"]["companySupervisorId"] = "other-supervisor";
            await Reject<UnauthorizedAccessException>(() => source.DetailsAsync("test-placement"));
        });
        await Test("live Company reader resolves a missing profile supervisor ID through one active UID mapping", async () => {
            var db = LiveDatabase("companySupervisor");
            ((JObject)db.Rows["users/test-uid"]).Property("companySupervisorId").Remove();
            db.Rows["companySupervisors/test-supervisor"] = JObject.Parse("{firebaseUid:'test-uid',companyId:'test-company',status:'active'}");
            var source = await LiveSource(LiveContext("companySupervisor"), "companySupervisor", db);
            Assert(source.Placements.Length == 1 && source.Placements[0].InternshipId == "test-placement");
        });
        await Test("live Company reader rejects duplicate, inactive, and company-conflicting supervisor mappings", async () => {
            var duplicate = LiveDatabase("companySupervisor"); ((JObject)duplicate.Rows["users/test-uid"]).Property("companySupervisorId").Remove();
            duplicate.Rows["companySupervisors/one"] = JObject.Parse("{firebaseUid:'test-uid',companyId:'test-company',status:'active'}");
            duplicate.Rows["companySupervisors/two"] = JObject.Parse("{firebaseUid:'test-uid',companyId:'test-company',status:'active'}");
            await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("companySupervisor"), "companySupervisor", duplicate));

            var inactive = LiveDatabase("companySupervisor"); ((JObject)inactive.Rows["users/test-uid"]).Property("companySupervisorId").Remove();
            inactive.Rows["companySupervisors/test-supervisor"] = JObject.Parse("{firebaseUid:'test-uid',companyId:'test-company',status:'inactive'}");
            await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("companySupervisor"), "companySupervisor", inactive));

            var conflict = LiveDatabase("companySupervisor"); ((JObject)conflict.Rows["users/test-uid"]).Property("companySupervisorId").Remove();
            conflict.Rows["companySupervisors/test-supervisor"] = JObject.Parse("{firebaseUid:'test-uid',companyId:'other-company',status:'active'}");
            await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("companySupervisor"), "companySupervisor", conflict));
        });
        await Test("live Student reader excludes mismatching Firebase UID", async () => {
            var db = LiveDatabase("student"); db.Rows["placements/test-placement"]["studentUid"] = "other-uid";
            Assert((await LiveSource(LiveContext("student"), "student", db)).Placements.Length == 0);
        });
        await Test("live Academic reader honors revoked assignment", async () => {
            var db = LiveDatabase("academicSupervisor"); db.Rows["supervisorAssignments/test-student"] = JObject.Parse("{studentId:'test-student',academicSupervisorId:'test-uid',active:false}");
            Assert((await LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", db)).Placements.Length == 0);
        });
        await Test("live Academic reader requires current supervisor assignment and resolves company name from its linked record", async () => {
            var db = LiveDatabase("academicSupervisor");
            db.Rows["supervisorAssignments/test-student"] = JObject.Parse("{studentId:'test-student',academicSupervisorId:'test-uid',active:true}");
            db.Rows["companies/test-company"] = JObject.Parse("{name:'Fictional Company Record'}");
            var source = await LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", db);
            Assert(source.Placements.Length == 1 && source.Placements[0].StudentId == "test-student" && source.Placements[0].DisplayName == "Name unavailable" && source.Placements[0].CompanyName == "Fictional Company Record");

            db.Rows["users/test-student-uid"] = JObject.Parse("{role:'student',isActive:true,studentId:'test-student',fullName:'Fictional Assigned Student'}");
            db.Rows["placements/test-placement"]["studentUid"] = "test-student-uid";
            source = await LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", db);
            Assert(source.Placements.Single().DisplayName == "Fictional Assigned Student");

            db.Rows["supervisorAssignments/test-student"]["academicSupervisorId"] = "another-supervisor";
            Assert((await LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", db)).Placements.Length == 0);
        });
        await Test("live Company and Academic readers safely fall back when Firebase query indexes are missing", async () => {
            var companyDatabase = LiveDatabase("companySupervisor"); companyDatabase.RequireQueryIndexes = true;
            companyDatabase.Rows["weeklyReports/test-report"] = JObject.Parse("{placementId:'test-placement',studentId:'test-student',companyId:'test-company',companySupervisorId:'test-supervisor',status:'Submitted',title:'Fictional test report'}");
            var company = await LiveSource(LiveContext("companySupervisor"), "companySupervisor", companyDatabase);
            Assert(company.Placements.Length == 1);
            Assert((await company.ReportsAsync("test-placement")).Contains("Fictional test report"));
            var academicDatabase = LiveDatabase("academicSupervisor"); academicDatabase.RequireQueryIndexes = true;
            academicDatabase.Rows["supervisorAssignments/test-student"] = JObject.Parse("{studentId:'test-student',academicSupervisorId:'test-uid',active:true}");
            Assert((await LiveSource(LiveContext("academicSupervisor"), "academicSupervisor", academicDatabase)).Placements.Length == 1);
        });
        await Test("live Coordinator role does not imply global scope", async () => {
            var db = LiveDatabase("coordinator"); await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", db));
        });
        await Test("explicit Coordinator assignment exposes only its active placement", async () => {
            var db = LiveDatabase("coordinator");
            db.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true,assignedAt:'2026-10-24T10:00:00Z',assignedBy:'trusted-admin'}");
            var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                var source = await LiveSource(LiveContext("coordinator"), "coordinator", db);
                Assert(source.Placements.Length == 1 && source.Placements[0].InternshipId == "test-placement");
                Assert(await source.CanReviewInternshipAsync("test-uid", "test-placement"));
                await Reject<UnauthorizedAccessException>(() => source.CanReviewInternshipAsync("test-uid", "foreign-placement"));
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
        });
        await Test("Coordinator scope with no explicit assignments returns no placements", async () => {
            var db = LiveDatabase("coordinator"); var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                var source = await LiveSource(LiveContext("coordinator"), "coordinator", db); Assert(source.Placements.Length == 0);
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
        });
        await Test("Coordinator scope rejects inactive and malformed assignments", async () => {
            var inactive = LiveDatabase("coordinator"); inactive.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:false,assignedAt:'2026-10-24T10:00:00Z',assignedBy:'trusted-admin'}");
            var missingAudit = LiveDatabase("coordinator"); missingAudit.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true}");
            var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", inactive));
                await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", missingAudit));
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
        });
        await Test("Coordinator scope rejects missing or inactive placements and conflicting assignment", async () => {
            var missing = LiveDatabase("coordinator"); missing.Rows["coordinatorAssignments/test-uid/no-such-placement"] = JObject.Parse("{isActive:true,assignedAt:1792836000000,assignedBy:'trusted-admin'}");
            var inactivePlacement = LiveDatabase("coordinator"); inactivePlacement.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true,assignedAt:1792836000000,assignedBy:'trusted-admin'}"); inactivePlacement.Rows["placements/test-placement"]["status"] = "pending";
            var conflict = LiveDatabase("coordinator"); conflict.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true,assignedAt:1792836000000,assignedBy:'trusted-admin'}"); conflict.Rows["placements/test-placement"]["coordinatorUid"] = "other-coordinator";
            var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", missing));
                await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", inactivePlacement));
                await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", conflict));
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
        });
        await Test("Coordinator scope rejects cross-Coordinator and wrong-role access", async () => {
            var db = LiveDatabase("coordinator"); db.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true,assignedAt:'2026-10-24T10:00:00Z',assignedBy:'trusted-admin'}");
            var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                var source = await LiveSource(LiveContext("coordinator"), "coordinator", db);
                Assert(!await source.CanReviewInternshipAsync("other-coordinator", "test-placement"));
                await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("student"), "coordinator", LiveDatabase("coordinator")));
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
        });
        await Test("Coordinator scope rejects inactive Coordinator profiles and duplicate internship mappings", async () => {
            var inactiveUser = LiveDatabase("coordinator"); inactiveUser.Rows["users/test-uid"]["isActive"] = false;
            inactiveUser.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true,assignedAt:'2026-10-24T10:00:00Z',assignedBy:'trusted-admin'}");
            var duplicateInternship = LiveDatabase("coordinator");
            duplicateInternship.Rows["coordinatorAssignments/test-uid/test-placement"] = JObject.Parse("{isActive:true,assignedAt:'2026-10-24T10:00:00Z',assignedBy:'trusted-admin'}");
            duplicateInternship.Rows["coordinatorAssignments/test-uid/second-placement"] = JObject.Parse("{isActive:true,assignedAt:'2026-10-24T10:00:00Z',assignedBy:'trusted-admin'}");
            duplicateInternship.Rows["placements/second-placement"] = JObject.Parse("{status:'confirmed',internshipId:'test-placement',studentId:'another-student'}");
            var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                await Reject<UnauthorizedAccessException>(() => LiveSource(LiveContext("coordinator"), "coordinator", inactiveUser));
                await Reject<EvaluationIntegrationUnavailableException>(() => LiveSource(LiveContext("coordinator"), "coordinator", duplicateInternship));
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
        });
        await Test("Coordinator scope fails closed on assignment read failure", async () => {
            var db = LiveDatabase("coordinator"); db.FailReadPath = "coordinatorAssignments/test-uid"; var prior = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory;
            try {
                FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = (context, database) => new FirebaseCoordinatorPlacementScope(context, database);
                await Reject<InvalidOperationException>(() => LiveSource(LiveContext("coordinator"), "coordinator", db));
            } finally { FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory = prior; }
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
