using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using University_Internship_Management_System.Shared;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class EvaluationIntegrationUnavailableException : InvalidOperationException
    { public EvaluationIntegrationUnavailableException(string message) : base(message) { } }

    // Request-scoped reads only. Missing university authorization/requirements are never inferred.
    public static class FinalEvaluationFirebaseIntegration
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
        public static Func<HttpContext, ICoordinatorEvaluationAccess> CoordinatorScopeFactory { get; set; }
        public static Func<HttpContext, IRequiredTasksReader> RequiredTasksFactory { get; set; }
        public static Func<HttpContext, IRequiredReportsReader> RequiredReportsFactory { get; set; }
        public static Func<HttpContext, IStudentVisibleTaskReader> PublishedTasksFactory { get; set; }
        public static Func<HttpContext, ICompletionEligibilityReader> EligibilityFactory { get; set; }
        public static Func<HttpContext, IPrivateCompletionReportStorage> PrivateReportStorageFactory { get; set; }

        public static void Register()
        {
            if (PercentagePageIntegration.Factory == null) PercentagePageIntegration.Factory = PercentageAsync;
            if (CoordinatorReviewPageIntegration.AsyncFactory == null) CoordinatorReviewPageIntegration.AsyncFactory = CoordinatorAsync;
            if (StudentCompletionPageIntegration.Factory == null) StudentCompletionPageIntegration.Factory = StudentAsync;
        }
        private static string SignedUid(HttpContext context, string role)
        {
            var identity = context?.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired ||
                FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != role) throw new UnauthorizedAccessException();
            return FirebaseKey.Require(identity.Name);
        }
        private static async Task<LiveEvaluationSources> ProductionAsync(HttpContext context, string role)
        {
            SignedUid(context, role); // Never make an administrative request for an anonymous/browser role.
            var config = new FirebaseConfiguration(); config.EnsureEnabled(); config.EnsureAuthenticationEnabled();
            try { await new GoogleAdcAccessTokenProvider().GetAccessTokenAsync().ConfigureAwait(false); }
            catch (RepositoryException) { throw new EvaluationIntegrationUnavailableException("Firebase server credentials are unavailable. Configure authorized Application Default Credentials outside the project and restart IIS Express."); }
            var gateway = new StudentFirebaseGateway(config);
            var repository = new FirebaseFinalEvaluationRepository(Client, new GoogleAdcAccessTokenProvider(), new FirebaseFinalEvaluationConfiguration(config));
            try { return await CreateSourcesAsync(context, role, new FirebaseApplicationDatabase(gateway), repository).ConfigureAwait(false); }
            catch (UnauthorizedAccessException) { throw; }
            catch (EvaluationIntegrationUnavailableException) { throw; }
            catch (Exception) { throw new EvaluationIntegrationUnavailableException("Authorized Firebase records could not be read. Verify server permissions, query indexes, profile mappings and database configuration. No records were changed."); }
        }
        // Injectable I/O is for isolated tests and trusted server composition, never HTTP input.
        public static async Task<LiveEvaluationSources> CreateSourcesAsync(HttpContext context, string role, IApplicationDatabase database, IFinalEvaluationRepository repository)
        {
            string uid = SignedUid(context, role);
            if (database == null || repository == null) throw new ArgumentNullException();
            if (!await database.IsActiveAuthenticationUidAsync(uid).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            var profile = await database.ReadAsync("users/" + FirebaseKey.Require(uid)).ConfigureAwait(false) as JObject;
            FirebaseLoginResult account;
            try { account = FirebaseSignInService.ProfileResult(uid, profile); }
            catch (FirebaseLoginException) { throw new UnauthorizedAccessException(); }
            if (account.Role != role || account.MustChangePassword) throw new UnauthorizedAccessException();
            var source = new LiveEvaluationSources(uid, role, profile, database, repository, context);
            await source.InitializeAsync().ConfigureAwait(false);
            return source;
        }
        private static async Task<PercentagePageContext> PercentageAsync(HttpContext context, bool academic)
        {
            var source = await ProductionAsync(context, academic ? "academicSupervisor" : "companySupervisor").ConfigureAwait(false);
            return new PercentagePageContext { UserId = source.UserId, Placements = source.Placements,
                Workflow = new PercentageEvaluationWorkflow(source.Repository, source, source, source),
                WriteBlocker = "Read-only Firebase access. Saving and submission remain disabled until live-write approval and a trusted evaluation/completion concurrency guard are connected.",
                ReadReportsAsync = academic ? (Func<string, Task<string>>)null : source.ReportsAsync,
                ReadDetailsAsync = source.DetailsAsync };
        }
        private static async Task<CoordinatorReviewPageContext> CoordinatorAsync(HttpContext context)
        {
            var source = await ProductionAsync(context, "coordinator").ConfigureAwait(false);
            var workflow = new CoordinatorReadinessWorkflow(source.Repository, source, source, source, source, source, source, source);
            return new CoordinatorReviewPageContext(workflow, source, source, source.Placements) { ReadCompletionAsync = source.CompletionAsync };
        }
        private static async Task<StudentCompletionPageContext> StudentAsync(HttpContext context)
        {
            var source = await ProductionAsync(context, "student").ConfigureAwait(false);
            if (source.Placements.Length > 1) throw new EvaluationIntegrationUnavailableException("More than one active placement is linked to this student. An authorized coordinator must resolve the ambiguity before progress can be displayed.");
            if (source.Placements.Length == 1 && (string.IsNullOrWhiteSpace(source.Placements[0].PolicyId) || string.IsNullOrWhiteSpace(source.Placements[0].PolicyVersion)))
                throw new EvaluationIntegrationUnavailableException("Your placement was found, but it has no explicit evaluation policy reference or existing evaluation to establish that reference. No policy or completion requirements were invented.");
            var storage = PrivateReportStorageFactory?.Invoke(context);
            var reports = storage == null ? null : new CompletionReportService(source.Repository, source, source, null, null, storage);
            var workflow = new StudentCompletionWorkflow(source.Repository, source, source, source, source, source, source, source, source, reports);
            return new StudentCompletionPageContext(workflow, source.Placements.SingleOrDefault()?.InternshipId, source.DisplayName);
        }
    }

    public sealed class LiveEvaluationSources : IAuthenticatedUser, IPercentageEvaluationAccess,
        ICompletionPlacementReader, IStudentCompletionPlacementReader, ICoordinatorEvaluationAccess, ICompletionReportAccess,
        IApprovedTrainingHoursReader, IRequiredTrainingHoursReader, IStudentPendingTrainingHoursReader,
        IRequiredTasksReader, IRequiredReportsReader, ICompletionEligibilityReader,
        IStudentVisibleTaskReader, IStudentOverallProgressReader, IStudentCompletionReportLocator
    {
        private readonly string role;
        private readonly JObject profile;
        private readonly IApplicationDatabase database;
        private readonly HttpContext context;
        private readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
        private readonly Dictionary<string, Task<HourEvidence>> hourReads = new Dictionary<string, Task<HourEvidence>>(StringComparer.Ordinal);
        private ICoordinatorEvaluationAccess coordinatorScope;
        public string UserId { get; private set; }
        public string DisplayName { get { return Text(profile, "fullName"); } }
        public IFinalEvaluationRepository Repository { get; private set; }
        public CompletionPlacement[] Placements { get { return bindings.Values.Select(x => x.Placement).ToArray(); } }
        private sealed class Binding { internal string Key; internal JObject Raw; internal CompletionPlacement Placement; }
        private sealed class HourEvidence { internal TrainingHourEntry[] Entries; internal decimal Required, Pending; }
        internal LiveEvaluationSources(string uid, string role, JObject profile, IApplicationDatabase database, IFinalEvaluationRepository repository, HttpContext context)
        { UserId = uid; this.role = role; this.profile = profile; this.database = database; Repository = repository; this.context = context; }
        private static string Text(JToken value, string key) { return value?[key]?.Type == JTokenType.String ? (string)value[key] : ""; }
        private string StudentId { get { return Text(profile, "studentId"); } }
        private string CompanyId { get { return Text(profile, "companyId"); } }
        private string SupervisorId { get { return Text(profile, "companySupervisorId") != "" ? Text(profile, "companySupervisorId") : Text(profile, "supervisorId") != "" ? Text(profile, "supervisorId") : Text(profile, "id"); } }
        private bool Active(JObject row) { var status = Text(row, "status"); return status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) || status.Equals("active", StringComparison.OrdinalIgnoreCase); }
        private async Task MergeAsync(Dictionary<string, JObject> rows, string collection, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var data = await database.ReadEqualAsync(collection, field, value).ConfigureAwait(false) as JObject;
            if (data == null) return;
            foreach (var item in data.Properties()) if (item.Value is JObject && Text(item.Value, field) == value) rows[item.Name] = (JObject)item.Value;
        }
        internal async Task InitializeAsync()
        {
            var candidates = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (role == "companySupervisor")
            {
                if (CompanyId == "" || SupervisorId == "") throw new EvaluationIntegrationUnavailableException("The verified Company profile needs its existing companyId and supervisor ID mapping.");
                await MergeAsync(candidates, "placements", "companySupervisorId", SupervisorId).ConfigureAwait(false);
                if (SupervisorId != UserId) await MergeAsync(candidates, "placements", "companySupervisorId", UserId).ConfigureAwait(false);
            }
            else if (role == "academicSupervisor")
            {
                await MergeAsync(candidates, "placements", "academicSupervisorId", UserId).ConfigureAwait(false);
                var assigned = await database.ReadEqualAsync("supervisorAssignments", "academicSupervisorId", UserId).ConfigureAwait(false) as JObject;
                if (assigned != null) foreach (var item in assigned.Properties())
                    if (item.Value is JObject && item.Value.Value<bool?>("active") == true && Text(item.Value, "academicSupervisorId") == UserId)
                        await MergeAsync(candidates, "placements", "studentId", Text(item.Value, "studentId")).ConfigureAwait(false);
            }
            else if (role == "student")
            {
                if (StudentId == "") throw new EvaluationIntegrationUnavailableException("The verified Student profile is missing its university studentId mapping.");
                await MergeAsync(candidates, "placements", "studentId", StudentId).ConfigureAwait(false);
                await MergeAsync(candidates, "placements", "studentUid", UserId).ConfigureAwait(false);
                await MergeAsync(candidates, "placements", "studentProfileId", StudentId).ConfigureAwait(false);
            }
            else if (role == "coordinator")
            {
                coordinatorScope = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory?.Invoke(context);
                if (coordinatorScope == null || !await coordinatorScope.IsCoordinatorAsync(UserId).ConfigureAwait(false))
                    throw new EvaluationIntegrationUnavailableException("Verified Coordinator login is available, but authorized internship scope is not connected. Coordinator role alone does not grant access to all placements.");
                // The trusted scope integration must enumerate its placement IDs; no broad root read.
                var scope = coordinatorScope as ICoordinatorPlacementScope;
                if (scope == null) throw new EvaluationIntegrationUnavailableException("The Coordinator scope adapter must provide authorized placement IDs.");
                foreach (string key in await scope.GetPlacementIdsAsync(UserId).ConfigureAwait(false))
                    if (await coordinatorScope.CanReviewInternshipAsync(UserId, key).ConfigureAwait(false))
                    { var row = await database.ReadAsync("placements/" + FirebaseKey.Require(key)).ConfigureAwait(false) as JObject; if (row != null) candidates[key] = row; }
            }
            foreach (var item in candidates)
            {
                if (!Active(item.Value) || !await OwnsAsync(item.Value).ConfigureAwait(false)) continue;
                string id = Text(item.Value, "internshipId"); if (id == "") id = item.Key;
                FirebaseKey.Require(id);
                if (role == "coordinator" && !await coordinatorScope.CanReviewInternshipAsync(UserId, id).ConfigureAwait(false)) continue;
                string student = Text(item.Value, "studentId"); if (student == "") student = Text(item.Value, "studentProfileId");
                if (student == "") throw new EvaluationIntegrationUnavailableException("An authorized placement has no student ID. Its evaluation cannot be safely linked.");
                FirebaseKey.Require(student);
                string policyId = Text(item.Value, "policyId"), policyVersion = Text(item.Value, "policyVersion");
                var percentageRepository = Repository as IPercentageEvaluationRepository;
                var evaluation = percentageRepository == null ? null : (await percentageRepository.GetPercentageAsync(id).ConfigureAwait(false)).Value;
                if (evaluation != null)
                {
                    PercentageEvaluationWorkflow.Validate(evaluation);
                    if (evaluation.StudentId != student || evaluation.InternshipId != id ||
                        (policyId != "" && policyId != evaluation.PolicyId) || (policyVersion != "" && policyVersion != evaluation.PolicyVersion))
                        throw new EvaluationIntegrationUnavailableException("Evaluation and placement identity/policy references disagree. An authorized migration or correction is required.");
                    policyId = evaluation.PolicyId; policyVersion = evaluation.PolicyVersion;
                }
                if (bindings.ContainsKey(id)) throw new EvaluationIntegrationUnavailableException("Multiple placements map to the same internship ID. Review is blocked until the mapping is resolved.");
                var placement = new CompletionPlacement { InternshipId = id, StudentId = student, PolicyId = policyId, PolicyVersion = policyVersion,
                    DisplayName = await NameAsync(item.Value, student).ConfigureAwait(false) };
                bindings.Add(id, new Binding { Key = item.Key, Raw = item.Value, Placement = placement });
            }
        }
        private async Task<string> NameAsync(JObject placement, string student)
        {
            if (role == "student") return DisplayName == "" ? student : DisplayName;
            string uid = Text(placement, "studentUid");
            if (uid != "")
            {
                var row = await database.ReadAsync("users/" + FirebaseKey.Require(uid)).ConfigureAwait(false) as JObject;
                if (row != null && FirebaseSignInService.CanonicalRole(Text(row, "role")) == "student" && Text(row, "studentId") == student)
                    return Text(row, "fullName") == "" ? student : Text(row, "fullName");
            }
            var rows = await database.ReadEqualAsync("users", "studentId", student).ConfigureAwait(false) as JObject;
            var matches = rows?.Properties().Where(x => x.Value is JObject && Text(x.Value, "studentId") == student && FirebaseSignInService.CanonicalRole(Text(x.Value, "role")) == "student").ToArray();
            return matches?.Length == 1 && Text(matches[0].Value, "fullName") != "" ? Text(matches[0].Value, "fullName") : student;
        }
        private async Task<bool> OwnsAsync(JObject row)
        {
            if (role == "companySupervisor") return Text(row, "companyId") == CompanyId && (Text(row, "companySupervisorId") == SupervisorId || Text(row, "companySupervisorId") == UserId);
            if (role == "student") return (Text(row, "studentId") == StudentId || Text(row, "studentProfileId") == StudentId) && (Text(row, "studentUid") == "" || Text(row, "studentUid") == UserId);
            if (role == "coordinator") return coordinatorScope != null;
            if (role != "academicSupervisor") return false;
            string student = Text(row, "studentId"); if (student == "") return false;
            var assignment = await database.ReadAsync("supervisorAssignments/" + FirebaseKey.Require(student)).ConfigureAwait(false) as JObject;
            string placed = Text(row, "academicSupervisorId");
            if (assignment != null && assignment.HasValues)
                return assignment.Value<bool?>("active") == true && Text(assignment, "studentId") == student && Text(assignment, "academicSupervisorId") == UserId && (placed == "" || placed == UserId);
            return placed == UserId;
        }
        private async Task<Binding> RequireAsync(string id)
        {
            Binding value;
            if (!bindings.TryGetValue(FirebaseKey.Require(id), out value)) throw new UnauthorizedAccessException();
            var row = await database.ReadAsync("placements/" + FirebaseKey.Require(value.Key)).ConfigureAwait(false) as JObject;
            if (row == null || !Active(row) || !await OwnsAsync(row).ConfigureAwait(false) ||
                Text(row, "studentUid") != Text(value.Raw, "studentUid") || Text(row, "studentId") != Text(value.Raw, "studentId") || Text(row, "studentProfileId") != Text(value.Raw, "studentProfileId") ||
                Text(row, "internshipId") != Text(value.Raw, "internshipId") || Text(row, "companyId") != Text(value.Raw, "companyId") || Text(row, "companySupervisorId") != Text(value.Raw, "companySupervisorId") ||
                Text(row, "policyId") != Text(value.Raw, "policyId") || Text(row, "policyVersion") != Text(value.Raw, "policyVersion")) throw new UnauthorizedAccessException();
            if (role == "coordinator" && !await coordinatorScope.CanReviewInternshipAsync(UserId, id).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            return value;
        }
        public async Task<CompletionPlacement> GetAsync(string id) { return (await RequireAsync(id).ConfigureAwait(false)).Placement; }
        public async Task<bool> CanCompanyAsync(string uid, CompletionPlacement p) { if (uid != UserId || role != "companySupervisor") return false; await RequireAsync(p.InternshipId).ConfigureAwait(false); return true; }
        public async Task<bool> CanAcademicAsync(string uid, CompletionPlacement p) { if (uid != UserId || role != "academicSupervisor") return false; await RequireAsync(p.InternshipId).ConfigureAwait(false); return true; }
        public Task<bool> IsCoordinatorAsync(string uid) { return Task.FromResult(uid == UserId && role == "coordinator" && coordinatorScope != null); }
        public async Task<bool> CanReviewInternshipAsync(string uid, string id) { if (uid != UserId || role != "coordinator") return false; await RequireAsync(id).ConfigureAwait(false); return true; }
        public async Task<bool> IsStudentOwnerAsync(string uid, string id, string student) { if (uid != UserId || role != "student") return false; var b = await RequireAsync(id).ConfigureAwait(false); return student == null || student == b.Placement.StudentId; }
        public Task<bool> CanGenerateAsync(string uid, string id) { return Task.FromResult(false); } // No live write approval/private issuance adapter.
        public async Task<bool> IsEligibleAsync(CompletionPlacement p) { var data = await GetEligibilityAsync(p.InternshipId).ConfigureAwait(false); return data != null && data.IsTrusted && data.Value == true && data.StudentId == p.StudentId && data.PolicyId == p.PolicyId && data.PolicyVersion == p.PolicyVersion; }
        public async Task<TrustedCompletionData<bool?>> GetEligibilityAsync(string id) { await RequireAsync(id).ConfigureAwait(false); var source = FinalEvaluationFirebaseIntegration.EligibilityFactory?.Invoke(context); return source == null ? null : await source.GetEligibilityAsync(id).ConfigureAwait(false); }
        public async Task<CompletionReview> CompletionAsync(string id) { var b = await RequireAsync(id).ConfigureAwait(false); var review = await Repository.GetCompletionReviewAsync(id).ConfigureAwait(false); if (review != null && (review.StudentId != b.Placement.StudentId || review.InternshipId != id)) throw new UnauthorizedAccessException(); return review; }
        public async Task<string> DetailsAsync(string id)
        {
            var b = await RequireAsync(id).ConfigureAwait(false);
            return "Student: " + b.Placement.DisplayName + " | Student ID: " + b.Placement.StudentId + " | Placement: " + b.Key + " | Internship: " + id +
                " | Company: " + Text(b.Raw, "companyId") + " | Company Supervisor: " + Text(b.Raw, "companySupervisorId") + " | Academic Supervisor: " + Text(b.Raw, "academicSupervisorId") +
                " | Start: " + (Text(b.Raw, "startDate") == "" ? "Unavailable" : Text(b.Raw, "startDate")) + " | Policy: " + (b.Placement.PolicyId == "" ? "Not linked" : b.Placement.PolicyId + "/" + b.Placement.PolicyVersion);
        }
        public async Task<string> ReportsAsync(string id)
        {
            var b = await RequireAsync(id).ConfigureAwait(false);
            var rows = await database.ReadEqualAsync("weeklyReports", "placementId", b.Key).ConfigureAwait(false) as JObject;
            var result = new List<string>();
            if (rows != null) foreach (var item in rows.Properties())
            {
                var row = item.Value as JObject;
                if (row == null || Text(row, "placementId") != b.Key || Text(row, "studentId") != b.Placement.StudentId || Text(row, "companyId") != Text(b.Raw, "companyId") ||
                    (Text(row, "companySupervisorId") != SupervisorId && Text(row, "companySupervisorId") != UserId)) continue;
                result.Add("Report " + item.Name + ": " + Text(row, "title") + " | Status: " + Text(row, "status"));
            }
            await RequireAsync(id).ConfigureAwait(false);
            return result.Count == 0 ? "No authorized weekly reports were found for this placement." : string.Join(Environment.NewLine, result);
        }
        private TrustedCompletionData<T> Data<T>(Binding b, T value) { return new TrustedCompletionData<T> { IsTrusted = true, InternshipId = b.Placement.InternshipId, StudentId = b.Placement.StudentId, PolicyId = b.Placement.PolicyId, PolicyVersion = b.Placement.PolicyVersion, Value = value }; }
        private Task<HourEvidence> HoursAsync(string id) { Task<HourEvidence> result; if (!hourReads.TryGetValue(id, out result)) hourReads[id] = result = LoadHoursAsync(id); return result; }
        private async Task<HourEvidence> LoadHoursAsync(string id)
        {
            var b = await RequireAsync(id).ConfigureAwait(false);
            if (bindings.Values.Count(x => x.Placement.StudentId == b.Placement.StudentId) != 1) throw new EvaluationIntegrationUnavailableException("Attendance cannot be linked uniquely to this placement.");
            var student = await database.ReadAsync("uimsAttendance/students/" + FirebaseKey.Require(b.Placement.StudentId)).ConfigureAwait(false) as JObject;
            string assignmentId = Text(student, "activeAssignmentId");
            if (student == null || Text(student, "status") != "active" || assignmentId == "" || (role == "student" && Text(student, "firebaseUid") != UserId)) throw new EvaluationIntegrationUnavailableException("Trusted attendance identity/active assignment is unavailable.");
            var assignment = await database.ReadAsync("uimsAttendance/trainingAssignments/" + FirebaseKey.Require(assignmentId)).ConfigureAwait(false) as JObject;
            if (assignment == null || Text(assignment, "status") != "active" || Text(assignment, "studentId") != b.Placement.StudentId || Text(assignment, "companyId") != Text(b.Raw, "companyId") || Text(assignment, "companySupervisorId") != Text(b.Raw, "companySupervisorId")) throw new EvaluationIntegrationUnavailableException("Attendance and placement mappings disagree.");
            decimal required = assignment.Value<decimal?>("requiredHours") ?? -1;
            decimal? placedRequired = b.Raw.Value<decimal?>("requiredHours");
            if (required <= 0 || (placedRequired.HasValue && placedRequired != required)) throw new EvaluationIntegrationUnavailableException("Required hours are missing or conflicting across the active assignment and placement.");
            var rows = await database.ReadAsync("uimsAttendance/attendance/" + FirebaseKey.Require(assignmentId)).ConfigureAwait(false) as JObject;
            var entries = new List<TrainingHourEntry>(); decimal pending = 0; long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (rows != null) foreach (var item in rows.Properties())
            {
                var row = item.Value as JObject; if (row == null) continue;
                long start = UIMS.Attendance.AttendanceService.Number(row, "checkInAt"), end = UIMS.Attendance.AttendanceService.Number(row, "checkOutAt"), minutes = UIMS.Attendance.AttendanceService.Number(row, "workedMinutes");
                if (Text(row, "studentId") != b.Placement.StudentId || Text(row, "assignmentId") != assignmentId || Text(row, "companyId") != Text(assignment, "companyId") || Text(row, "companySupervisorId") != Text(assignment, "companySupervisorId") ||
                    (Text(row, "status") != "completed" && Text(row, "status") != "earlyDeparture") || start <= 0 || end <= start || end > now || minutes <= 0 || minutes != (end - start) / 60000) continue;
                if (Text(row, "hoursApprovalStatus") == "approved" && Text(row, "hoursApprovedById") == Text(assignment, "companySupervisorId")) entries.Add(new TrainingHourEntry { EntryId = item.Name, Hours = minutes / 60m, IsApproved = true });
                else if (Text(row, "hoursApprovalStatus") == "pending") pending += minutes / 60m;
            }
            await RequireAsync(id).ConfigureAwait(false);
            return new HourEvidence { Entries = entries.ToArray(), Required = required, Pending = pending };
        }
        async Task<TrustedCompletionData<TrainingHourEntry[]>> IApprovedTrainingHoursReader.GetAsync(string id) { var b = await RequireAsync(id).ConfigureAwait(false); return Data(b, (await HoursAsync(id).ConfigureAwait(false)).Entries); }
        async Task<TrustedCompletionData<decimal?>> IRequiredTrainingHoursReader.GetAsync(string id) { var b = await RequireAsync(id).ConfigureAwait(false); return Data<decimal?>(b, (await HoursAsync(id).ConfigureAwait(false)).Required); }
        public async Task<TrustedCompletionData<decimal?>> GetPendingHoursAsync(string id) { var b = await RequireAsync(id).ConfigureAwait(false); return Data<decimal?>(b, (await HoursAsync(id).ConfigureAwait(false)).Pending); }
        async Task<TrustedCompletionData<CompletionChecklist>> IRequiredTasksReader.GetAsync(string id) { await RequireAsync(id).ConfigureAwait(false); var source = FinalEvaluationFirebaseIntegration.RequiredTasksFactory?.Invoke(context); return source == null ? null : await source.GetAsync(id).ConfigureAwait(false); }
        async Task<TrustedCompletionData<CompletionChecklist>> IRequiredReportsReader.GetAsync(string id) { await RequireAsync(id).ConfigureAwait(false); var source = FinalEvaluationFirebaseIntegration.RequiredReportsFactory?.Invoke(context); return source == null ? null : await source.GetAsync(id).ConfigureAwait(false); }
        async Task<TrustedCompletionData<StudentTaskRecord[]>> IStudentVisibleTaskReader.GetAsync(string id) { await RequireAsync(id).ConfigureAwait(false); var source = FinalEvaluationFirebaseIntegration.PublishedTasksFactory?.Invoke(context); return source == null ? null : await source.GetAsync(id).ConfigureAwait(false); }
        Task<TrustedCompletionData<decimal?>> IStudentOverallProgressReader.GetAsync(string id) { return Task.FromResult<TrustedCompletionData<decimal?>>(null); }
        public async Task<string> GetIssuedDocumentIdAsync(string id, string student)
        {
            var b = await RequireAsync(id).ConfigureAwait(false); if (student != b.Placement.StudentId) throw new UnauthorizedAccessException();
            var rows = await database.ReadAsync("completionDocuments/" + FirebaseKey.Require(id)).ConfigureAwait(false) as JObject;
            var latest = rows?.Properties().Where(x => x.Value is JObject && Text(x.Value, "studentId") == student && Text(x.Value, "internshipId") == id && Text(x.Value, "status") == "Issued")
                .OrderByDescending(x => Text(x.Value, "issuedAtUtc")).FirstOrDefault();
            return latest?.Name;
        }
    }
    public interface ICoordinatorPlacementScope : ICoordinatorEvaluationAccess
    { Task<string[]> GetPlacementIdsAsync(string coordinatorUid); }
}
