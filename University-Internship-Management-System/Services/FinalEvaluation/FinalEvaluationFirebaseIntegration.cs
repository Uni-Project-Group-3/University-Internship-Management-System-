using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
    // Safe failure categories only: never attach a UID, email, token, or database payload.
    public sealed class EvaluationAuthorizationFailureException : UnauthorizedAccessException
    {
        public string ReasonCode { get; private set; }
        public EvaluationAuthorizationFailureException(string reasonCode) : base("Evaluation access is not authorized.") { ReasonCode = reasonCode; }
    }

    // Request-scoped reads only. Missing university authorization/requirements are never inferred.
    public static class FinalEvaluationFirebaseIntegration
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly GoogleAdcAccessTokenProvider Tokens = new GoogleAdcAccessTokenProvider();
        private const string RequestBudgetKey = "UIMS.FinalEvaluation.FirebaseReadBudget";
        private static readonly TimeSpan PageReadBudget = TimeSpan.FromSeconds(35);
        // The production adapter is opt-in until the proposed assignment contract is approved
        // and populated. Explicit trusted host factories retain precedence.
        public static Func<HttpContext, IApplicationDatabase, ICoordinatorEvaluationAccess> CoordinatorScopeFactory { get; set; }
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
            if (CoordinatorScopeFactory == null) CoordinatorScopeFactory = (context, database) =>
                string.Equals(System.Configuration.ConfigurationManager.AppSettings["CoordinatorAssignmentScopeEnabled"], "true", StringComparison.OrdinalIgnoreCase)
                    ? (ICoordinatorEvaluationAccess)new FirebaseCoordinatorPlacementScope(context, database)
                    : null;
        }
        private static string SignedUid(HttpContext context, string role)
        {
            var identity = context?.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired ||
                FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != role) throw new EvaluationAuthorizationFailureException("signed_identity_or_role");
            return FirebaseKey.Require(identity.Name);
        }
        internal static CancellationToken RequestBudget(HttpContext context)
        {
            var budget = context.Items[RequestBudgetKey] as CancellationTokenSource;
            if (budget == null)
            {
                budget = CancellationTokenSource.CreateLinkedTokenSource(context.Response.ClientDisconnectedToken);
                budget.CancelAfter(PageReadBudget);
                context.Items[RequestBudgetKey] = budget;
                context.AddOnRequestCompleted(_ => budget.Dispose());
            }
            return budget.Token;
        }
        private static async Task<LiveEvaluationSources> ProductionAsync(HttpContext context, string role)
        {
            SignedUid(context, role); // Never make an administrative request for an anonymous/browser role.
            var config = new FirebaseConfiguration(); config.EnsureEnabled(); config.EnsureAuthenticationEnabled();
            var requestCancellation = RequestBudget(context);
            var gateway = new StudentFirebaseGateway(config, Tokens, requestCancellation);
            var repository = new FirebaseFinalEvaluationRepository(Client, Tokens, new FirebaseFinalEvaluationConfiguration(config),
                TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(8), requestCancellation);
            try { return await CreateSourcesAsync(context, role, new FirebaseApplicationDatabase(gateway), repository).ConfigureAwait(false); }
            catch (UnauthorizedAccessException) { throw; }
            catch (EvaluationIntegrationUnavailableException) { throw; }
            catch (RepositoryException error) { throw new EvaluationIntegrationUnavailableException(error.Message); }
            catch (Exception) { throw new EvaluationIntegrationUnavailableException("Authorized Firebase records could not be read. Verify server permissions, query indexes, profile mappings and database configuration. No records were changed."); }
        }
        // Injectable I/O is for isolated tests and trusted server composition, never HTTP input.
        public static async Task<LiveEvaluationSources> CreateSourcesAsync(HttpContext context, string role, IApplicationDatabase database, IFinalEvaluationRepository repository)
        {
            string uid = SignedUid(context, role);
            if (database == null || repository == null) throw new ArgumentNullException();
            if (!await database.IsActiveAuthenticationUidAsync(uid).ConfigureAwait(false)) throw new EvaluationAuthorizationFailureException("firebase_auth_uid");
            var profile = await database.ReadAsync("users/" + FirebaseKey.Require(uid)).ConfigureAwait(false) as JObject;
            if (profile == null) throw new EvaluationAuthorizationFailureException("profile_missing");
            if (profile.Value<bool?>("isActive") != true) throw new EvaluationAuthorizationFailureException("profile_inactive");
            string setupStatus = StudentManagementService.Text(profile, "accountSetupStatus");
            if (setupStatus != "" && setupStatus != "complete") throw new EvaluationAuthorizationFailureException("profile_setup_incomplete");
            if (StudentManagementService.Text(profile, "deletionStatus") == "pending") throw new EvaluationAuthorizationFailureException("profile_deletion_pending");
            string profileRole = FirebaseSignInService.CanonicalRole(StudentManagementService.Text(profile, "role"));
            if (profileRole == null || profileRole != role) throw new EvaluationAuthorizationFailureException("profile_role_mismatch");
            if (profile.Value<bool?>("mustChangePassword") == true) throw new EvaluationAuthorizationFailureException("password_change_required");
            var source = new LiveEvaluationSources(uid, role, profile, database, repository, context);
            await source.InitializeAsync().ConfigureAwait(false);
            return source;
        }
        private static async Task<PercentagePageContext> PercentageAsync(HttpContext context, bool academic)
        {
            var source = await ProductionAsync(context, academic ? "academicSupervisor" : "companySupervisor").ConfigureAwait(false);
            return new PercentagePageContext { UserId = source.UserId, Placements = source.Placements,
                Workflow = new PercentageEvaluationWorkflow(source.Repository, source, source, source),
                WriteBlocker = academic
                    ? "Academic approval and revision decisions are disabled in live Firebase until live-write approval, an approved production policy, validated schema, and the trusted cross-workflow concurrency guard are connected."
                    : "Read-only Firebase access. Saving and submission remain disabled until live-write approval and a trusted evaluation/completion concurrency guard are connected.",
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
        private string resolvedCompanyId;
        private string resolvedSupervisorId;
        public string UserId { get; private set; }
        public string DisplayName { get { return Text(profile, "fullName"); } }
        public IFinalEvaluationRepository Repository { get; private set; }
        public CompletionPlacement[] Placements { get { return bindings.Values.Select(x => x.Placement).ToArray(); } }
        private sealed class Binding { internal string Key; internal JObject Raw; internal CompletionPlacement Placement; }
        private sealed class HourEvidence { internal TrainingHourEntry[] Entries; internal decimal Required, Pending; }
        internal LiveEvaluationSources(string uid, string role, JObject profile, IApplicationDatabase database, IFinalEvaluationRepository repository, HttpContext context)
        {
            UserId = uid; this.role = role; this.profile = profile; this.database = database; Repository = repository; this.context = context;
            resolvedCompanyId = Text(profile, "companyId");
            resolvedSupervisorId = Text(profile, "companySupervisorId");
        }
        private static string Text(JToken value, string key) { return value?[key]?.Type == JTokenType.String ? (string)value[key] : ""; }
        private string StudentId { get { return Text(profile, "studentId"); } }
        private string CompanyId { get { return resolvedCompanyId ?? ""; } }
        private string SupervisorId { get { return resolvedSupervisorId ?? ""; } }
        private bool Active(JObject row) { var status = Text(row, "status"); return status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) || status.Equals("active", StringComparison.OrdinalIgnoreCase); }
        private async Task MergeAsync(Dictionary<string, JObject> rows, string collection, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var data = await ReadEqualOrIndexedCollectionAsync(collection, field, value).ConfigureAwait(false);
            if (data == null) return;
            foreach (var item in data.Properties()) if (item.Value is JObject && Text(item.Value, field) == value) rows[item.Name] = (JObject)item.Value;
        }
        private async Task<JObject> ReadEqualOrIndexedCollectionAsync(string collection, string field, string value)
        {
            try { return await database.ReadEqualAsync(collection, field, value).ConfigureAwait(false) as JObject; }
            catch (FirebaseQueryIndexException)
            {
                // The privileged server read stays inside this adapter; filter every row by the
                // exact same field/value before returning anything to the role-scoped workflow.
                var all = await database.ReadAsync(collection).ConfigureAwait(false) as JObject;
                if (all == null) return new JObject();
                var matching = new JObject();
                foreach (var item in all.Properties())
                    if (item.Value is JObject && string.Equals(Text(item.Value, field), value, StringComparison.Ordinal))
                        matching[item.Name] = item.Value.DeepClone();
                return matching;
            }
        }
        internal async Task InitializeAsync()
        {
            var candidates = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (role == "companySupervisor")
            {
                await ResolveCompanySupervisorMappingAsync().ConfigureAwait(false);
                if (CompanyId == "" || SupervisorId == "") throw new EvaluationIntegrationUnavailableException("The verified Company profile needs its existing companyId and supervisor ID mapping.");
                await MergeAsync(candidates, "placements", "companySupervisorId", SupervisorId).ConfigureAwait(false);
                if (SupervisorId != UserId) await MergeAsync(candidates, "placements", "companySupervisorId", UserId).ConfigureAwait(false);
            }
            else if (role == "academicSupervisor")
            {
                await MergeAsync(candidates, "placements", "academicSupervisorId", UserId).ConfigureAwait(false);
                var assigned = await ReadEqualOrIndexedCollectionAsync("supervisorAssignments", "academicSupervisorId", UserId).ConfigureAwait(false);
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
                coordinatorScope = FinalEvaluationFirebaseIntegration.CoordinatorScopeFactory?.Invoke(context, database);
                if (coordinatorScope == null || !await coordinatorScope.IsCoordinatorAsync(UserId).ConfigureAwait(false))
                {
                    bool scopeEnabled = string.Equals(System.Configuration.ConfigurationManager.AppSettings["CoordinatorAssignmentScopeEnabled"], "true", StringComparison.OrdinalIgnoreCase);
                    string category = scopeEnabled ? "factory_or_profile_unavailable" : "feature_flag_disabled";
                    System.Diagnostics.Trace.TraceWarning("UIMS Coordinator scope resolution stopped. category={0}", category);
                    throw new EvaluationIntegrationUnavailableException(scopeEnabled
                        ? "Coordinator scope could not be verified. Confirm the active Coordinator profile and explicit assignment source. No placements were loaded."
                        : "Coordinator assignment scope is disabled. After the account and approved explicit assignments are verified, enable CoordinatorAssignmentScopeEnabled. No placements were loaded.");
                }
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
                    DisplayName = await NameAsync(item.Value, student).ConfigureAwait(false),
                    CompanyName = await CompanyNameAsync(item.Value).ConfigureAwait(false), PlacementId = item.Key,
                    StartDate = Text(item.Value, "startDate") == "" ? Text(item.Value, "start") : Text(item.Value, "startDate"),
                    EndDate = Text(item.Value, "endDate") == "" ? Text(item.Value, "end") : Text(item.Value, "endDate") };
                bindings.Add(id, new Binding { Key = item.Key, Raw = item.Value, Placement = placement });
            }
        }
        private async Task ResolveCompanySupervisorMappingAsync()
        {
            // Some existing UIMS modules map an authenticated Company UID through
            // /companySupervisors/{id}.firebaseUid when users/{uid} has no explicit
            // companySupervisorId. Accept only one active, exact UID match and keep
            // the profile's companyId authoritative when it is already present.
            if (CompanyId != "" && SupervisorId != "") return;

            if (SupervisorId != "")
            {
                var supervisor = await database.ReadAsync("companySupervisors/" + FirebaseKey.Require(SupervisorId)).ConfigureAwait(false) as JObject;
                if (supervisor == null || !string.Equals(Text(supervisor, "status"), "active", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Text(supervisor, "firebaseUid"), UserId, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException();
                string mappedCompany = Text(supervisor, "companyId");
                if (CompanyId != "" && mappedCompany != "" && !string.Equals(CompanyId, mappedCompany, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException();
                if (CompanyId == "") resolvedCompanyId = mappedCompany;
                return;
            }

            var supervisors = await database.ReadAsync("companySupervisors").ConfigureAwait(false) as JObject;
            var matches = supervisors == null ? new List<JProperty>() : supervisors.Properties().Where(item =>
                item.Value is JObject && string.Equals(Text(item.Value, "firebaseUid"), UserId, StringComparison.Ordinal) &&
                string.Equals(Text(item.Value, "status"), "active", StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count != 1) throw new UnauthorizedAccessException();
            string mappedSupervisorId = FirebaseKey.Require(matches[0].Name);
            string companyId = Text(matches[0].Value, "companyId");
            if (companyId == "") throw new EvaluationIntegrationUnavailableException("The verified Company Supervisor record is missing its companyId mapping.");
            resolvedSupervisorId = mappedSupervisorId;
            if (CompanyId == "") resolvedCompanyId = companyId;
            else if (!string.Equals(CompanyId, companyId, StringComparison.Ordinal)) throw new UnauthorizedAccessException();
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
            var rows = await ReadEqualOrIndexedCollectionAsync("users", "studentId", student).ConfigureAwait(false);
            var matches = rows?.Properties().Where(x => x.Value is JObject && Text(x.Value, "studentId") == student && FirebaseSignInService.CanonicalRole(Text(x.Value, "role")) == "student").ToArray();
            return matches?.Length == 1 && Text(matches[0].Value, "fullName") != "" ? Text(matches[0].Value, "fullName") : "Name unavailable";
        }
        private async Task<string> CompanyNameAsync(JObject placement)
        {
            string companyId = Text(placement, "companyId");
            string placementName = Text(placement, "companyName");
            if (companyId == "") return placementName == "" ? "Unavailable" : placementName;
            var company = await database.ReadAsync("companies/" + FirebaseKey.Require(companyId)).ConfigureAwait(false) as JObject;
            string name = Text(company, "name");
            if (name == "") name = Text(company, "companyName");
            if (name == "") name = Text(company, "organizationName");
            return name == "" ? (placementName == "" ? "Unavailable" : placementName) : name;
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
                " | Company: " + b.Placement.CompanyName + " | Company Supervisor: " + Text(b.Raw, "companySupervisorId") + " | Academic Supervisor: " + Text(b.Raw, "academicSupervisorId") +
                " | Start: " + (Text(b.Raw, "startDate") == "" ? "Unavailable" : Text(b.Raw, "startDate")) + " | Policy: " + (b.Placement.PolicyId == "" ? "Not linked" : b.Placement.PolicyId + "/" + b.Placement.PolicyVersion);
        }
        public async Task<string> ReportsAsync(string id)
        {
            var b = await RequireAsync(id).ConfigureAwait(false);
            var rows = await ReadEqualOrIndexedCollectionAsync("weeklyReports", "placementId", b.Key).ConfigureAwait(false);
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
        async Task<TrustedCompletionData<StudentTaskRecord[]>> IStudentVisibleTaskReader.GetAsync(string id)
        {
            var binding = await RequireAsync(id).ConfigureAwait(false);
            var configured = FinalEvaluationFirebaseIntegration.PublishedTasksFactory?.Invoke(context);
            if (configured != null) return await configured.GetAsync(id).ConfigureAwait(false);
            if (role != "student") return null;

            // Keep the Tasks module opt-in: its read contract must be explicitly enabled and
            // schema-validated before this page asks TaskService to contact Firebase.
            var settings = System.Web.Configuration.WebConfigurationManager.AppSettings;
            if (!string.Equals(settings["TasksEnabled"], "true", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(settings["FirebaseTaskSchemaValidated"], "true", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals((settings["FirebaseTaskRootPath"] ?? "tasks").Trim('/'), "tasks", StringComparison.Ordinal)) return null;

            var actor = UIMS.Tasks.TaskActor.FromVerifiedProfile(UserId, role, profile);
            var taskService = new UIMS.Tasks.TaskService(actor);
            var payload = await taskService.LoadAsync("student", FinalEvaluationFirebaseIntegration.RequestBudget(context)).ConfigureAwait(false);
            return Data(binding, StudentTaskServiceProjection.Map(payload, binding.Placement, binding.Key));
        }
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
    // The task module is authoritative for student scope and status transitions. This
    // allowlist deliberately omits submissions, attachments, and unpublished result data.
    public static class StudentTaskServiceProjection
    {
        public static StudentTaskRecord[] Map(JObject payload, CompletionPlacement placement, string placementKey)
        {
            if (payload == null || placement == null || string.IsNullOrWhiteSpace(placement.StudentId) || string.IsNullOrWhiteSpace(placementKey) ||
                !string.Equals((string)payload["role"], "student", StringComparison.Ordinal) ||
                !string.Equals((string)payload["identity"], placement.StudentId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("The task service identity does not match the authorized placement.");
            var rows = payload["tasks"] as JArray;
            if (rows == null) throw new InvalidOperationException("The trusted Tasks service returned an invalid task list.");
            var result = new List<StudentTaskRecord>();
            foreach (var token in rows)
            {
                var row = token as JObject;
                if (row == null) throw new InvalidOperationException("The trusted Tasks service returned an invalid task record.");
                string studentId = (string)row["studentId"] ?? "";
                if (!string.Equals(studentId, placement.StudentId, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException("A task record is not linked to the authorized Student.");
                string linkedPlacement = (string)row["placementId"] ?? "";
                if (!string.IsNullOrWhiteSpace(linkedPlacement) && !string.Equals(linkedPlacement, placementKey, StringComparison.Ordinal)) continue;
                string status = ((string)row["status"] ?? "").ToLowerInvariant();
                StudentTaskStatus mapped;
                switch (status)
                {
                    case "assigned": mapped = StudentTaskStatus.Assigned; break;
                    case "inprogress": mapped = StudentTaskStatus.InProgress; break;
                    case "submitted": mapped = StudentTaskStatus.Submitted; break;
                    case "revision": mapped = StudentTaskStatus.Revision; break;
                    case "completed": mapped = StudentTaskStatus.Evaluated; break;
                    default: throw new InvalidOperationException("The trusted Tasks service returned an unsupported task status.");
                }
                bool published = mapped == StudentTaskStatus.Evaluated || mapped == StudentTaskStatus.Revision;
                result.Add(new StudentTaskRecord
                {
                    Id = (string)row["id"], Title = (string)row["title"], Status = mapped,
                    IsVisibleToStudent = true, IsResultPublished = published,
                    Grade = mapped == StudentTaskStatus.Evaluated ? row.Value<decimal?>("score") : null,
                    // TaskService's DTO has a fallback maximum when the source omits it; don't
                    // present that fallback as authoritative per-task grade data.
                    MaximumGrade = null,
                    StudentVisibleFeedback = published ? (string)row["feedback"] : null
                });
            }
            return result.ToArray();
        }
    }

    public interface ICoordinatorPlacementScope : ICoordinatorEvaluationAccess
    { Task<string[]> GetPlacementIdsAsync(string coordinatorUid); }

    /// <summary>
    /// Read-only adapter for the proposed, explicit coordinatorAssignments/{uid}/{placementId}
    /// contract. It is only registered behind CoordinatorAssignmentScopeEnabled and never
    /// derives scope from university/college fields or coordinator role alone.
    /// </summary>
    public sealed class FirebaseCoordinatorPlacementScope : ICoordinatorPlacementScope
    {
        private readonly string trustedUid;
        private readonly IApplicationDatabase database;
        private readonly Dictionary<string, string> placementByAuthorizedId = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool loaded;

        public FirebaseCoordinatorPlacementScope(HttpContext context, IApplicationDatabase database)
        {
            var identity = context?.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired ||
                FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != "coordinator")
                throw new EvaluationAuthorizationFailureException("signed_identity_or_role");
            trustedUid = FirebaseKey.Require(identity.Name);
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task<bool> IsCoordinatorAsync(string coordinatorUid)
        {
            if (!string.Equals(trustedUid, coordinatorUid, StringComparison.Ordinal)) return false;
            if (!await database.IsActiveAuthenticationUidAsync(trustedUid).ConfigureAwait(false)) return false;
            var profile = await database.ReadAsync("users/" + FirebaseKey.Require(trustedUid)).ConfigureAwait(false) as JObject;
            if (profile == null || profile.Value<bool?>("isActive") != true || profile.Value<bool?>("mustChangePassword") == true) return false;
            string setup = String(profile, "accountSetupStatus");
            if (setup != "" && setup != "complete") return false;
            if (String(profile, "deletionStatus") == "pending") return false;
            return FirebaseSignInService.CanonicalRole(String(profile, "role")) == "coordinator";
        }

        public async Task<string[]> GetPlacementIdsAsync(string coordinatorUid)
        {
            if (!await IsCoordinatorAsync(coordinatorUid).ConfigureAwait(false)) throw new EvaluationAuthorizationFailureException("coordinator_profile_invalid");
            if (!string.Equals(trustedUid, coordinatorUid, StringComparison.Ordinal)) throw new EvaluationAuthorizationFailureException("coordinator_scope_uid_mismatch");
            var assignments = await database.ReadAsync("coordinatorAssignments/" + FirebaseKey.Require(trustedUid)).ConfigureAwait(false) as JObject;
            placementByAuthorizedId.Clear();
            if (assignments == null || !assignments.HasValues) { loaded = true; return new string[0]; }

            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in assignments.Properties())
            {
                string placementKey;
                try { placementKey = FirebaseKey.Require(property.Name); }
                catch (Exception) { throw new EvaluationIntegrationUnavailableException("A Coordinator assignment contains an invalid placement reference."); }
                if (!seenKeys.Add(placementKey)) throw new EvaluationIntegrationUnavailableException("Duplicate Coordinator placement assignments were found.");
                var assignment = property.Value as JObject;
                if (assignment == null || assignment.Value<bool?>("isActive") != true)
                    throw new EvaluationIntegrationUnavailableException("A Coordinator assignment is missing or inactive; scope resolution was stopped.");
                string assignedBy = String(assignment, "assignedBy");
                if (!ValidAssignedBy(assignedBy) || !ValidAssignedAt(assignment["assignedAt"]))
                    throw new EvaluationIntegrationUnavailableException("A Coordinator assignment is missing valid audit fields.");
                string declaredUid = String(assignment, "coordinatorUid");
                if (declaredUid != "" && !string.Equals(declaredUid, trustedUid, StringComparison.Ordinal))
                    throw new EvaluationIntegrationUnavailableException("A Coordinator assignment conflicts with its parent account.");

                var placement = await database.ReadAsync("placements/" + placementKey).ConfigureAwait(false) as JObject;
                if (placement == null) throw new EvaluationIntegrationUnavailableException("A Coordinator assignment references a missing placement.");
                string status = String(placement, "status");
                if (!status.Equals("active", StringComparison.OrdinalIgnoreCase) && !status.Equals("confirmed", StringComparison.OrdinalIgnoreCase))
                    throw new EvaluationIntegrationUnavailableException("A Coordinator assignment references an inactive or ineligible placement.");
                string declaredPlacementCoordinator = String(placement, "coordinatorUid");
                if (declaredPlacementCoordinator != "" && !string.Equals(declaredPlacementCoordinator, trustedUid, StringComparison.Ordinal))
                    throw new EvaluationIntegrationUnavailableException("The placement contains a conflicting Coordinator assignment.");
                string internshipId = String(placement, "internshipId");
                if (internshipId == "") internshipId = placementKey;
                try { FirebaseKey.Require(internshipId); }
                catch (Exception) { throw new EvaluationIntegrationUnavailableException("A Coordinator placement has an invalid internship reference."); }
                if (placementByAuthorizedId.ContainsKey(placementKey) || placementByAuthorizedId.ContainsKey(internshipId))
                    throw new EvaluationIntegrationUnavailableException("Conflicting Coordinator assignments resolve to the same internship.");
                placementByAuthorizedId.Add(placementKey, placementKey);
                if (internshipId != placementKey) placementByAuthorizedId.Add(internshipId, placementKey);
            }
            loaded = true;
            return seenKeys.ToArray();
        }

        public async Task<bool> CanReviewInternshipAsync(string coordinatorUid, string internshipId)
        {
            if (!loaded || !string.Equals(trustedUid, coordinatorUid, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(internshipId)) return false;
            if (!await IsCoordinatorAsync(coordinatorUid).ConfigureAwait(false)) return false;
            string placementKey;
            if (!placementByAuthorizedId.TryGetValue(internshipId, out placementKey)) return false;
            var assignment = await database.ReadAsync("coordinatorAssignments/" + FirebaseKey.Require(trustedUid) + "/" + FirebaseKey.Require(placementKey)).ConfigureAwait(false) as JObject;
            if (assignment == null || assignment.Value<bool?>("isActive") != true || !ValidAssignedBy(String(assignment, "assignedBy")) || !ValidAssignedAt(assignment["assignedAt"])) return false;
            string declaredUid = String(assignment, "coordinatorUid");
            if (declaredUid != "" && !string.Equals(declaredUid, trustedUid, StringComparison.Ordinal)) return false;
            var placement = await database.ReadAsync("placements/" + FirebaseKey.Require(placementKey)).ConfigureAwait(false) as JObject;
            if (placement == null) return false;
            string status = String(placement, "status");
            string declaredCoordinator = String(placement, "coordinatorUid");
            string currentInternshipId = String(placement, "internshipId");
            if (currentInternshipId == "") currentInternshipId = placementKey;
            return (status.Equals("active", StringComparison.OrdinalIgnoreCase) || status.Equals("confirmed", StringComparison.OrdinalIgnoreCase)) &&
                (declaredCoordinator == "" || string.Equals(declaredCoordinator, trustedUid, StringComparison.Ordinal)) &&
                (string.Equals(internshipId, placementKey, StringComparison.Ordinal) || string.Equals(internshipId, currentInternshipId, StringComparison.Ordinal));
        }

        private static string String(JObject value, string key)
        { return value?[key]?.Type == JTokenType.String ? (string)value[key] : ""; }

        private static bool ValidAssignedBy(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            try { FirebaseKey.Require(value); return true; } catch (Exception) { return false; }
        }

        private static bool ValidAssignedAt(JToken value)
        {
            if (value == null) return false;
            if (value.Type == JTokenType.Date) return true;
            if (value.Type == JTokenType.Integer)
            {
                long millis;
                if (!long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out millis)) return false;
                try { DateTimeOffset.FromUnixTimeMilliseconds(millis); return true; } catch (ArgumentOutOfRangeException) { return false; }
            }
            DateTimeOffset parsed;
            return value.Type == JTokenType.String && DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed);
        }
    }
}
