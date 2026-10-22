using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Explicitly constructed local fake. Never selected as a fallback for Firebase failures.
    public sealed partial class InMemoryFinalEvaluationRepository : IFinalEvaluationRepository, ICompanySubmissionRepository, IAcademicSubmissionRepository, ICompletionDecisionRepository, ICompletionDocumentMetadataRepository
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, object> records = new Dictionary<string, object>();
        private readonly Dictionary<string, string> versions = new Dictionary<string, string>();
        private readonly Dictionary<string, EvaluationPolicy> policies = new Dictionary<string, EvaluationPolicy>();
        public bool SimulateFailure { get; set; }
        public Task CreateIssuedCompletionReportAsync(CompletionDocument document)
        {
            lock (gate)
            {
                Check(); var copy = Clone(document); CompletionReportValidation.Metadata(copy);
                string key = "document/" + copy.InternshipId + "/" + copy.DocumentId;
                if (records.ContainsKey(key)) throw new ConflictException();
                object value;
                if (!records.TryGetValue("review/" + copy.InternshipId, out value)) throw new ConflictException();
                var review = (CompletionReview)value;
                if (review.Status != CompletionStatus.Approved || review.StudentId != copy.StudentId || review.PolicyId != copy.PolicyId || review.PolicyVersion != copy.PolicyVersion || review.ReviewedAtUtc != copy.CompletionApprovedAtUtc) throw new ConflictException();
                Seed(key, copy); return Task.CompletedTask;
            }
        }
        public Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string id)
        { return Read<CompletionReview>("review/" + FirebaseKey.Require(id)); }
        public Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite decision, string expectedVersion)
        {
            lock (gate)
            {
                Check(); string key = "review/" + FirebaseKey.Require(decision.InternshipId);
                object old; bool exists = records.TryGetValue(key, out old);
                if (string.IsNullOrWhiteSpace(expectedVersion) || (exists ? versions[key] : "null_etag") != expectedVersion) throw new ConflictException();
                EvaluationPolicy policy; policies.TryGetValue(decision.PolicyId, out policy);
                var saved = CompletionDecisionValidation.Apply(exists ? Clone((CompletionReview)old) : null, decision, policy);
                records[key] = Clone(saved); versions[key] = Guid.NewGuid().ToString("N");
                return Task.FromResult(Clone(saved));
            }
        }
        public InMemoryFinalEvaluationRepository(IEnumerable<EvaluationPolicy> policies = null,
            IEnumerable<CompanyFinalEvaluation> companies = null, IEnumerable<AcademicFinalEvaluation> academics = null,
            IEnumerable<CompletionReview> reviews = null, IEnumerable<CompletionDocument> documents = null)
        {
            if (policies != null) foreach (var p in policies) this.policies.Add(FirebaseKey.Require(p.PolicyId), Clone(p));
            if (companies != null) foreach (var v in companies) Seed("company/" + FirebaseKey.Require(v.InternshipId), v);
            if (academics != null) foreach (var v in academics) Seed("academic/" + FirebaseKey.Require(v.InternshipId), v);
            if (reviews != null) foreach (var v in reviews) Seed("review/" + FirebaseKey.Require(v.InternshipId), v);
            if (documents != null) foreach (var v in documents) Seed("document/" + FirebaseKey.Require(v.InternshipId) + "/" + FirebaseKey.Require(v.DocumentId), v);
        }
        private void Seed<T>(string path, T value) { records.Add(path, Clone(value)); versions.Add(path, Guid.NewGuid().ToString("N")); }
        private static T Clone<T>(T value) { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, FirebaseFinalEvaluationRepository.Json), FirebaseFinalEvaluationRepository.Json); }
        private void Check() { if (SimulateFailure) throw new RepositoryException("Simulated local repository failure."); }
        private Task<RecordSnapshot<T>> Read<T>(string key)
        {
            lock (gate)
            {
                Check(); object value;
                return Task.FromResult(records.TryGetValue(key, out value) ? new RecordSnapshot<T>(Clone((T)value), versions[key]) : new RecordSnapshot<T>(default(T), "null_etag"));
            }
        }
        private Task Save<T>(string key, T value, string version, bool create) where T : EvaluationRecord
        {
            lock (gate)
            {
                Check(); var copy = Clone(value); FinalEvaluationValidation.Draft(copy);
                EvaluationPolicy policy; policies.TryGetValue(copy.PolicyId, out policy);
                FinalEvaluationValidation.Evaluation(copy, policy, copy is CompanyFinalEvaluation);
                object old;
                if (create)
                {
                    if (records.ContainsKey(key)) throw new ConflictException();
                    copy.CreatedAtUtc = DateTimeOffset.UtcNow;
                }
                else
                {
                    if (!records.TryGetValue(key, out old) || versions[key] != version) throw new ConflictException();
                    FinalEvaluationValidation.UnchangedIdentity((EvaluationRecord)old, copy);
                    copy.CreatedAtUtc = ((EvaluationRecord)old).CreatedAtUtc;
                }
                copy.UpdatedAtUtc = DateTimeOffset.UtcNow;
                records[key] = copy; versions[key] = Guid.NewGuid().ToString("N");
                return Task.CompletedTask;
            }
        }
        public Task SubmitCompanyAsync(CompanyFinalEvaluation candidate, string expectedVersion)
        { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task SubmitAcademicAsync(AcademicFinalEvaluation candidate, string expectedVersion)
        { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string id) { return Read<CompanyFinalEvaluation>("company/" + FirebaseKey.Require(id)); }
        public Task CreateCompanyDraftAsync(CompanyFinalEvaluation v) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task UpdateCompanyDraftAsync(CompanyFinalEvaluation v, string version) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string id) { return Read<AcademicFinalEvaluation>("academic/" + FirebaseKey.Require(id)); }
        public Task CreateAcademicDraftAsync(AcademicFinalEvaluation v) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public Task UpdateAcademicDraftAsync(AcademicFinalEvaluation v, string version) { throw new NotSupportedException("Legacy score writes are retired. Use the versioned company percentage and academic review workflow."); }
        public async Task<CompletionReview> GetCompletionReviewAsync(string id) { return (await Read<CompletionReview>("review/" + FirebaseKey.Require(id))).Value; }
        public async Task<CompletionDocument> GetCompletionDocumentAsync(string id, string documentId) { return (await Read<CompletionDocument>("document/" + FirebaseKey.Require(id) + "/" + FirebaseKey.Require(documentId))).Value; }
        public Task<EvaluationPolicy> GetPolicyAsync(string id)
        {
            lock (gate) { Check(); EvaluationPolicy p; policies.TryGetValue(FirebaseKey.Require(id), out p); return Task.FromResult(Clone(p)); }
        }
    }
}
