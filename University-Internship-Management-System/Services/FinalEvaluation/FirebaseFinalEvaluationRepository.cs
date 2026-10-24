using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed partial class FirebaseFinalEvaluationRepository : IFinalEvaluationRepository, ICompanySubmissionRepository, IAcademicSubmissionRepository, ICompletionDecisionRepository
    {
        private readonly HttpClient client;
        private readonly IAccessTokenProvider tokens;
        private readonly Uri root;
        private readonly FirebaseFinalEvaluationConfiguration configuration;
        private readonly TimeSpan tokenTimeout;
        private readonly TimeSpan requestTimeout;
        private readonly CancellationToken requestCancellation;
        internal static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false } },
            DateParseHandling = DateParseHandling.DateTimeOffset,
            MissingMemberHandling = MissingMemberHandling.Error,
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter { AllowIntegerValues = false } }
        };
        public FirebaseFinalEvaluationRepository(HttpClient client, IAccessTokenProvider tokens, FirebaseFinalEvaluationConfiguration configuration)
            : this(client, tokens, configuration, TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15)) { }
        public FirebaseFinalEvaluationRepository(HttpClient client, IAccessTokenProvider tokens, FirebaseFinalEvaluationConfiguration configuration,
            TimeSpan tokenTimeout, TimeSpan requestTimeout)
            : this(client, tokens, configuration, tokenTimeout, requestTimeout, CancellationToken.None) { }
        public FirebaseFinalEvaluationRepository(HttpClient client, IAccessTokenProvider tokens, FirebaseFinalEvaluationConfiguration configuration,
            TimeSpan tokenTimeout, TimeSpan requestTimeout, CancellationToken requestCancellation)
        {
            this.client = client ?? throw new ArgumentNullException("client");
            this.tokens = tokens ?? throw new ArgumentNullException("tokens");
            this.configuration = configuration ?? throw new ArgumentNullException("configuration");
            if (tokenTimeout <= TimeSpan.Zero || requestTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException();
            this.tokenTimeout = tokenTimeout;
            this.requestTimeout = requestTimeout;
            this.requestCancellation = requestCancellation;
            root = this.configuration.DatabaseRoot;
        }
        private static string Key(string value) { return Uri.EscapeDataString(FirebaseKey.Require(value)); }
        private async Task<RecordSnapshot<T>> ReadAsync<T>(string path)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(root, path + ".json")))
            {
                request.Headers.Add("X-Firebase-ETag", "true");
                using (var response = await SendAsync(request).ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    try
                    {
                        T value = JsonConvert.DeserializeObject<T>(body, Json);
                        ValidateRead(value, path);
                        // Firebase's unquoted null_etag is not exposed by .NET's typed ETag parser.
                        System.Collections.Generic.IEnumerable<string> etags;
                        string version = response.Headers.TryGetValues("ETag", out etags) ? etags.FirstOrDefault() : null;
                        if (string.IsNullOrWhiteSpace(version)) throw new RepositoryException("Firebase did not return a version token.");
                        return new RecordSnapshot<T>(value, version);
                    }
                    catch (JsonException) { throw new RepositoryException("Firebase returned an invalid record contract."); }
                }
            }
        }
        private static void ValidateRead<T>(T value, string path)
        {
            if (object.ReferenceEquals(value, null)) return;
            try
            {
                var evaluation = value as EvaluationRecord;
                if (evaluation != null)
                {
                    FirebaseKey.Require(evaluation.StudentId); FirebaseKey.Require(evaluation.EvaluatorId);
                    FirebaseKey.Require(evaluation.PolicyId); FirebaseKey.Require(evaluation.PolicyVersion);
                    if (evaluation.Scores == null || !Enum.IsDefined(typeof(EvaluationStatus), evaluation.Status)) throw new ArgumentException();
                    foreach (string key in evaluation.Scores.Keys) FirebaseKey.Require(key);
                    if (!path.EndsWith("/" + Key(evaluation.InternshipId), StringComparison.Ordinal)) throw new ArgumentException();
                }
                var policy = value as EvaluationPolicy;
                if (policy != null && (policy.CompanyCriteria == null || policy.AcademicCriteria == null ||
                    string.IsNullOrWhiteSpace(policy.Version) || !path.EndsWith("/" + Key(policy.PolicyId), StringComparison.Ordinal))) throw new ArgumentException();
                var review = value as CompletionReview;
                if (review != null)
                {
                    FinalEvaluationValidation.Completion(review);
                    if (!path.EndsWith("/" + Key(review.InternshipId), StringComparison.Ordinal)) throw new ArgumentException();
                }
                var document = value as CompletionDocument;
                if (document != null)
                {
                    FinalEvaluationValidation.Document(document);
                    if (path != "completionDocuments/" + Key(document.InternshipId) + "/" + Key(document.DocumentId)) throw new ArgumentException();
                }
            }
            catch (ArgumentException) { throw new RepositoryException("Firebase returned an invalid record contract."); }
        }
        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            try
            {
                configuration.EnsureEnabled();
                if (request.Method != HttpMethod.Get) configuration.EnsureWritesAllowed();
                var tokenTimer = FirebaseReadDiagnostics.Start();
                string token;
                try
                {
                    using (var tokenCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestCancellation))
                    {
                        tokenCancellation.CancelAfter(tokenTimeout);
                        var cancellable = tokens as ICancellableAccessTokenProvider;
                        token = cancellable == null
                            ? await tokens.GetAccessTokenAsync().ConfigureAwait(false)
                            : await cancellable.GetAccessTokenAsync(tokenCancellation.Token).ConfigureAwait(false);
                    }
                    FirebaseReadDiagnostics.Finish("oauth.token", tokenTimer, "success");
                }
                catch (OperationCanceledException) { FirebaseReadDiagnostics.Finish("oauth.token", tokenTimer, "timeout_or_cancelled"); throw; }
                catch (Exception) { FirebaseReadDiagnostics.Finish("oauth.token", tokenTimer, "failed"); throw; }
                if (string.IsNullOrWhiteSpace(token)) throw new RepositoryException("An authenticated server token is required.");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var requestTimer = FirebaseReadDiagnostics.Start();
                HttpResponseMessage response;
                try
                {
                    using (var requestTimeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestCancellation))
                    {
                        requestTimeoutCancellation.CancelAfter(requestTimeout);
                        response = await client.SendAsync(request, requestTimeoutCancellation.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { FirebaseReadDiagnostics.Finish("firebase.database.request", requestTimer, "timeout_or_cancelled"); throw; }
                catch (HttpRequestException) { FirebaseReadDiagnostics.Finish("firebase.database.request", requestTimer, "network_error"); throw; }
                FirebaseReadDiagnostics.Finish("firebase.database.request", requestTimer, "http_" + (int)response.StatusCode);
                if (response.StatusCode == HttpStatusCode.PreconditionFailed) { response.Dispose(); throw new ConflictException(); }
                if (!response.IsSuccessStatusCode)
                {
                    int status = (int)response.StatusCode; response.Dispose();
                    if (status == 401 || status == 403) throw new RepositoryException("Firebase denied the authenticated read. Check the service account's database permissions and Firebase configuration.");
                    throw new RepositoryException("Firebase request failed (HTTP " + status + ").");
                }
                return response;
            }
            catch (HttpRequestException) { throw new RepositoryException("Firebase connection failed."); }
            catch (OperationCanceledException error) { throw new RepositoryException("Firebase request timed out or was canceled.", error); }
        }
        private async Task WriteDraftAsync<T>(string path, T value, string expected, bool create) where T : EvaluationRecord
        {
            // Capture the input before awaits; callers cannot mutate a pending save.
            T copy = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, Json), Json);
            FinalEvaluationValidation.Draft(copy);
            if (!create)
            {
                if (string.IsNullOrWhiteSpace(expected)) throw new ArgumentException("Expected version is required.");
                var current = await ReadAsync<T>(path).ConfigureAwait(false);
                if (current.Version != expected) throw new ConflictException();
                FinalEvaluationValidation.UnchangedIdentity(current.Value, copy);
                copy.CreatedAtUtc = current.Value.CreatedAtUtc;
            }
            else copy.CreatedAtUtc = DateTimeOffset.UtcNow;
            copy.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var policy = await GetPolicyAsync(copy.PolicyId).ConfigureAwait(false);
            FinalEvaluationValidation.Evaluation(copy, policy, copy is CompanyFinalEvaluation);
            using (var request = new HttpRequestMessage(HttpMethod.Put, new Uri(root, path + ".json")))
            {
                // The final PUT, including draft updates, is a server-enforced compare-and-swap.
                request.Headers.TryAddWithoutValidation("if-match", create ? "null_etag" : expected);
                request.Content = new StringContent(JsonConvert.SerializeObject(copy, Json), Encoding.UTF8, "application/json");
                using (var response = await SendAsync(request).ConfigureAwait(false)) { }
            }
        }
        public Task SubmitCompanyAsync(CompanyFinalEvaluation candidate, string expectedVersion)
        { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task SubmitAcademicAsync(AcademicFinalEvaluation candidate, string expectedVersion)
        { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { return ReadAsync<CompanyFinalEvaluation>("companyFinalEvaluations/" + Key(id)); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation value) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation value, string version) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { return ReadAsync<AcademicFinalEvaluation>("academicFinalEvaluations/" + Key(id)); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation value) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation value, string version) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public async Task<CompletionReview> GetCompletionReviewAsync(string id) { return (await ReadAsync<CompletionReview>("completionReviews/" + Key(id)).ConfigureAwait(false)).Value; }
        public Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string id)
        { return ReadAsync<CompletionReview>("completionReviews/" + Key(id)); }
        public async Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite decision, string expectedVersion)
        {
            if (decision == null) throw new ArgumentNullException("decision");
            if (string.IsNullOrWhiteSpace(expectedVersion)) throw new ConflictException();
            string path = "completionReviews/" + Key(decision.InternshipId);
            var current = await ReadAsync<CompletionReview>(path).ConfigureAwait(false);
            if (current.Version != expectedVersion) throw new ConflictException();
            var policy = await GetPolicyAsync(decision.PolicyId).ConfigureAwait(false);
            var saved = CompletionDecisionValidation.Apply(current.Value, decision, policy);
            using (var request = new HttpRequestMessage(HttpMethod.Put, new Uri(root, path + ".json")))
            {
                // Current state AND its appended audit history change atomically at this exact path.
                request.Headers.TryAddWithoutValidation("if-match", expectedVersion);
                request.Content = new StringContent(JsonConvert.SerializeObject(saved, Json), Encoding.UTF8, "application/json");
                using (var response = await SendAsync(request).ConfigureAwait(false)) { }
            }
            return saved;
        }
        public async Task<CompletionDocument> GetCompletionDocumentAsync(string id, string documentId) { return (await ReadAsync<CompletionDocument>("completionDocuments/" + Key(id) + "/" + Key(documentId)).ConfigureAwait(false)).Value; }
        public async Task<EvaluationPolicy> GetPolicyAsync(string id) { return (await ReadAsync<EvaluationPolicy>("evaluationPolicies/" + Key(id)).ConfigureAwait(false)).Value; }
    }
}
