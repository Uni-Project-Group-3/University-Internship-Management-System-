using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class DevelopmentDemoDataset
    {
        public string Owner { get; set; }
        public string DatasetId { get; set; }
        public int SchemaVersion { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public JObject Definition { get; set; }
        public Dictionary<string, PercentageEvaluation> Evaluations { get; set; } = new Dictionary<string, PercentageEvaluation>();
        public Dictionary<string, CompletionReview> Completions { get; set; } = new Dictionary<string, CompletionReview>();
        public Dictionary<string, Dictionary<string, CompletionDocument>> Documents { get; set; } = new Dictionary<string, Dictionary<string, CompletionDocument>>();
    }
    public interface IDevelopmentDemoDocuments { Task<string> LatestDocumentAsync(string internshipId); }

    // Privileged repository, NOT user authorization. Its only URI is one fixed owned demo dataset.
    // Evaluation, completion and audit mutations share the same root ETag/CAS boundary.
    public sealed class FirebaseDevelopmentDemoRepository : IFinalEvaluationRepository, IPercentageEvaluationRepository,
        ICompletionDecisionRepository, ICompletionDocumentMetadataRepository, ICompletionDecisionGuard, IDevelopmentDemoDocuments
    {
        public const string Namespace = "_devTests/roaaFinalEvaluationDemo/v1";
        public const string OwnerMarker = "UIMS_ROAA_FINAL_EVALUATION_FICTIONAL_V1";
        private readonly HttpClient client;
        private readonly IAccessTokenProvider tokens;
        private readonly Uri uri;
        private readonly string datasetId;
        private readonly SemaphoreSlim mutation = new SemaphoreSlim(1, 1);
        private string leasedVersion;
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings {
            ContractResolver = FirebaseFinalEvaluationRepository.Json.ContractResolver, MissingMemberHandling = MissingMemberHandling.Error,
            DateParseHandling = DateParseHandling.None, Converters = { new Newtonsoft.Json.Converters.StringEnumConverter { AllowIntegerValues = false } } };
        public string DatasetPath { get { return Namespace + "/sessions/" + datasetId; } }
        public FirebaseDevelopmentDemoRepository(HttpClient client, IAccessTokenProvider tokens, FirebaseFinalEvaluationConfiguration configuration, string datasetId)
        {
            if (configuration == null || configuration.DatabaseRoot.AbsoluteUri != FirebaseFinalEvaluationConfiguration.DevelopmentDatabaseUrl) throw new ArgumentException("Only the exact owner's development Firebase is permitted.");
            Guid parsed;
            if (!Guid.TryParseExact(datasetId, "N", out parsed) || parsed == Guid.Empty) throw new ArgumentException("A generated dataset identifier is required.");
            this.client = client ?? throw new ArgumentNullException("client"); this.tokens = tokens ?? throw new ArgumentNullException("tokens"); this.datasetId = datasetId;
            uri = new Uri(configuration.DatabaseRoot, DatasetPath + ".json");
        }
        public static void VerifyExternalDevelopmentCredentials()
        {
            const string expected = @"C:\Users\user\FirebaseSecrets\uims-roaa-service-account.json";
            try
            {
                var configured = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (string.IsNullOrWhiteSpace(configured)) configured = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", EnvironmentVariableTarget.User);
                if (!string.Equals(Path.GetFullPath(configured ?? ""), expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
                using (var reader = File.OpenText(expected)) using (var json = new JsonTextReader(reader))
                {
                    var credential = JObject.Load(json);
                    if ((string)credential["project_id"] != "uims-roa-a-development" || (string)credential["type"] != "service_account" || (string)credential["token_uri"] != "https://oauth2.googleapis.com/token") throw new InvalidOperationException();
                }
                // Process-only local demo composition; never changes Windows User/Machine settings.
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", expected);
            }
            catch { throw new RepositoryException("The expected external development credentials/ADC configuration are unavailable or do not match the development project."); }
        }
        private async Task<RecordSnapshot<DevelopmentDemoDataset>> Request(string method, DevelopmentDemoDataset value = null, string etag = null)
        {
            using (var request = new HttpRequestMessage(new HttpMethod(method), uri))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAccessTokenAsync().ConfigureAwait(false));
                request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
                if (etag != null) request.Headers.TryAddWithoutValidation("if-match", etag);
                if (value != null) request.Content = new StringContent(JsonConvert.SerializeObject(value, Json), Encoding.UTF8, "application/json");
                try
                {
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.PreconditionFailed) throw new ConflictException();
                        if (!response.IsSuccessStatusCode) throw new RepositoryException("Development Firebase request could not be confirmed.");
                        IEnumerable<string> versions; var version = response.Headers.TryGetValues("ETag", out versions) ? versions.FirstOrDefault() : null;
                        if (string.IsNullOrWhiteSpace(version)) throw new RepositoryException("Development Firebase version acknowledgment is missing.");
                        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var dataset = JsonConvert.DeserializeObject<DevelopmentDemoDataset>(body, Json);
                        if (dataset != null) Validate(dataset);
                        return new RecordSnapshot<DevelopmentDemoDataset>(dataset, version);
                    }
                }
                catch (JsonException) { throw new RepositoryException("Invalid development dataset contract."); }
                catch (HttpRequestException) { throw new RepositoryException("Development Firebase connection failed."); }
                catch (TaskCanceledException) { throw new RepositoryException("Development Firebase request timed out."); }
            }
        }
        private void Validate(DevelopmentDemoDataset value)
        {
            if (value.Owner != OwnerMarker || value.DatasetId != datasetId || value.SchemaVersion != 1 || value.Definition == null || value.Evaluations == null || value.Completions == null || value.Documents == null ||
                Canonical(value.Definition).ToString(Formatting.None) != Canonical(DevelopmentDemoSeed.Definition()).ToString(Formatting.None)) throw new RepositoryException("This is not the expected owned fictional dataset. No automatic overwrite is allowed.");
            try
            {
                foreach (var pair in value.Evaluations) { PercentageEvaluationWorkflow.Validate(pair.Value); if (pair.Key != pair.Value.InternshipId || !DevelopmentDemoSeed.Placements().Any(p => p.InternshipId == pair.Key && p.StudentId == pair.Value.StudentId && p.PolicyId == pair.Value.PolicyId && p.PolicyVersion == pair.Value.PolicyVersion)) throw new ArgumentException(); }
                foreach (var pair in value.Completions) { FinalEvaluationValidation.Completion(pair.Value); if (pair.Key != pair.Value.InternshipId || !DevelopmentDemoSeed.Placements().Any(p => p.InternshipId == pair.Key && p.StudentId == pair.Value.StudentId && p.PolicyId == pair.Value.PolicyId && p.PolicyVersion == pair.Value.PolicyVersion)) throw new ArgumentException(); }
                foreach (var intern in value.Documents) foreach (var doc in intern.Value) { CompletionReportValidation.Metadata(doc.Value); if (doc.Key != doc.Value.DocumentId || intern.Key != doc.Value.InternshipId) throw new ArgumentException(); }
            }
            catch (ArgumentException) { throw new RepositoryException("Invalid development dataset identity/history."); }
        }
        private static JToken Canonical(JToken value)
        {
            // RTDB omits null and empty collections and normalizes numeric formatting.
            if (value is JObject) return new JObject(((JObject)value).Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Canonical(p.Value))).Where(p => p.Value.Type != JTokenType.Null && (!(p.Value is JContainer) || ((JContainer)p.Value).Count > 0)));
            if (value is JArray) return new JArray(((JArray)value).Select(Canonical));
            if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer) return new JValue(Convert.ToDecimal(((JValue)value).Value, CultureInfo.InvariantCulture).ToString("G29", CultureInfo.InvariantCulture));
            return value.DeepClone();
        }
        public async Task<RecordSnapshot<DevelopmentDemoDataset>> InspectAsync()
        { var s = await Request("GET").ConfigureAwait(false); if (leasedVersion != null && leasedVersion != s.Version) throw new ConflictException(); return s; }
        public async Task SeedAsync()
        {
            var existing = await InspectAsync().ConfigureAwait(false);
            if (existing.Value != null) return; // Idempotent: never resets saved workflow state.
            var seed = DevelopmentDemoSeed.Create(datasetId);
            try { await Request("PUT", seed, existing.Version).ConfigureAwait(false); }
            catch (ConflictException) { if ((await InspectAsync().ConfigureAwait(false)).Value == null) throw; }
        }
        // Administrative API only. The reset script requires typed confirmation and supplies
        // the exact inspected ETag; neither the UI nor a browser endpoint calls this method.
        public async Task DeleteExactDatasetAsync(string expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(expectedVersion)) throw new ConflictException();
            var old = await InspectAsync().ConfigureAwait(false);
            if (old.Value == null || old.Version != expectedVersion) throw new ConflictException();
            await Request("DELETE", null, expectedVersion).ConfigureAwait(false);
            if ((await InspectAsync().ConfigureAwait(false)).Value != null) throw new RepositoryException("Exact demo dataset deletion was not confirmed.");
        }
        private async Task<RecordSnapshot<DevelopmentDemoDataset>> Required()
        { var value = await InspectAsync().ConfigureAwait(false); if (value.Value == null) throw new RepositoryException("The owned demo dataset is missing; run the seed process."); return value; }
        private async Task Commit(DevelopmentDemoDataset data, string expected)
        {
            Validate(data); var saved = await Request("PUT", data, expected).ConfigureAwait(false);
            if (saved.Value == null) throw new RepositoryException("Dataset persistence was not acknowledged.");
            if (leasedVersion != null) leasedVersion = saved.Version;
        }
        public async Task<RecordSnapshot<PercentageEvaluation>> GetPercentageAsync(string id)
        { FirebaseKey.Require(id); var root = await Required().ConfigureAwait(false); PercentageEvaluation value; root.Value.Evaluations.TryGetValue(id, out value); return new RecordSnapshot<PercentageEvaluation>(value, root.Version); }
        public async Task SavePercentageAsync(PercentageEvaluation value, string expectedVersion)
        { var copy = PercentageEvaluationWorkflow.Clone(value); var root = await Required().ConfigureAwait(false); if (root.Version != expectedVersion) throw new ConflictException(); PercentageEvaluation old; root.Value.Evaluations.TryGetValue(copy.InternshipId, out old); PercentageHistory.Check(old, copy); root.Value.Evaluations[copy.InternshipId] = copy; await Commit(root.Value, expectedVersion).ConfigureAwait(false); }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { FirebaseKey.Require(id); return Task.FromResult(new RecordSnapshot<CompanyFinalEvaluation>(null, "null_etag")); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { FirebaseKey.Require(id); return Task.FromResult(new RecordSnapshot<AcademicFinalEvaluation>(null, "null_etag")); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation value) { throw new NotSupportedException("Legacy scoring is retired."); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation value, string version) { throw new NotSupportedException("Legacy scoring is retired."); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation value) { throw new NotSupportedException("Independent Academic scoring is retired."); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation value, string version) { throw new NotSupportedException("Independent Academic scoring is retired."); }
        public async Task<EvaluationPolicy> GetPolicyAsync(string id) { await Required().ConfigureAwait(false); return id == DevelopmentDemoSeed.Policy().PolicyId ? DevelopmentDemoSeed.Policy() : null; }
        public async Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string id)
        { FirebaseKey.Require(id); var root = await Required().ConfigureAwait(false); CompletionReview value; root.Value.Completions.TryGetValue(id, out value); return new RecordSnapshot<CompletionReview>(value, root.Version); }
        public async Task<CompletionReview> GetCompletionReviewAsync(string id) { return (await GetCompletionReviewSnapshotAsync(id).ConfigureAwait(false)).Value; }
        public async Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite write, string expectedVersion)
        { var root = await Required().ConfigureAwait(false); if (root.Version != expectedVersion) throw new ConflictException(); CompletionReview old; root.Value.Completions.TryGetValue(write.InternshipId, out old); var saved = CompletionDecisionValidation.Apply(old, write, DevelopmentDemoSeed.Policy()); root.Value.Completions[write.InternshipId] = saved; await Commit(root.Value, expectedVersion).ConfigureAwait(false); return PercentageEvaluationWorkflow.Clone(saved); }
        public async Task<CompletionDocument> GetCompletionDocumentAsync(string id, string doc)
        { FirebaseKey.Require(id); FirebaseKey.Require(doc); var root = await Required().ConfigureAwait(false); Dictionary<string,CompletionDocument> docs; CompletionDocument value = null; if (root.Value.Documents.TryGetValue(id, out docs)) docs.TryGetValue(doc, out value); return value; }
        public async Task<string> LatestDocumentAsync(string id)
        { FirebaseKey.Require(id); var root = await Required().ConfigureAwait(false); Dictionary<string,CompletionDocument> docs; return root.Value.Documents.TryGetValue(id, out docs) ? docs.Values.Where(d => d.Status == DocumentStatus.Issued).OrderByDescending(d => d.IssuedAtUtc).Select(d => d.DocumentId).FirstOrDefault() : null; }
        public async Task CreateIssuedCompletionReportAsync(CompletionDocument value)
        {
            var copy = PercentageEvaluationWorkflow.Clone(value); CompletionReportValidation.Metadata(copy); var root = await Required().ConfigureAwait(false); CompletionReview review;
            if (!root.Value.Completions.TryGetValue(copy.InternshipId, out review) || review.Status != CompletionStatus.Approved || review.StudentId != copy.StudentId || review.PolicyId != copy.PolicyId || review.PolicyVersion != copy.PolicyVersion || review.ReviewedAtUtc != copy.CompletionApprovedAtUtc) throw new ConflictException();
            Dictionary<string,CompletionDocument> docs; if (!root.Value.Documents.TryGetValue(copy.InternshipId, out docs)) root.Value.Documents[copy.InternshipId] = docs = new Dictionary<string,CompletionDocument>();
            if (docs.ContainsKey(copy.DocumentId)) throw new ConflictException(); docs.Add(copy.DocumentId, copy); await Commit(root.Value, root.Version).ConfigureAwait(false);
        }
        public async Task<ICompletionDecisionLease> AcquireAsync(string id)
        {
            FirebaseKey.Require(id); await mutation.WaitAsync().ConfigureAwait(false);
            try { leasedVersion = (await Required().ConfigureAwait(false)).Version; return new Lease(this); }
            catch { mutation.Release(); throw; }
        }
        private sealed class Lease : ICompletionDecisionLease
        {
            private readonly FirebaseDevelopmentDemoRepository repository; internal Lease(FirebaseDevelopmentDemoRepository repository) { this.repository = repository; }
            public async Task<bool> IsCurrentAsync(CompletionReadiness value) { try { await repository.Required().ConfigureAwait(false); return true; } catch (ConflictException) { return false; } }
            public void Dispose() { repository.leasedVersion = null; repository.mutation.Release(); }
        }
    }
}
