using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    internal static class PercentageHistory
    {
        internal static void Check(PercentageEvaluation old, PercentageEvaluation value)
        {
            PercentageEvaluationWorkflow.Validate(value);
            if (old == null) { if (value.Current.Number != 1 || value.Submissions.Count != 0 || value.Reviews.Count != 0) throw new ConflictException(); return; }
            if (old.InternshipId != value.InternshipId || old.StudentId != value.StudentId || old.PolicyId != value.PolicyId || old.PolicyVersion != value.PolicyVersion) throw new ConflictException();
            if (value.Current.CompanySupervisorId != old.Current.CompanySupervisorId || value.Submissions.Count > old.Submissions.Count + 1 || value.Reviews.Count > old.Reviews.Count + 1) throw new ConflictException();
            if (value.Submissions.Count < old.Submissions.Count || value.Reviews.Count < old.Reviews.Count ||
                JsonConvert.SerializeObject(old.Submissions) != JsonConvert.SerializeObject(value.Submissions.Take(old.Submissions.Count)) ||
                JsonConvert.SerializeObject(old.Reviews) != JsonConvert.SerializeObject(value.Reviews.Take(old.Reviews.Count))) throw new ConflictException();
            if (old.Current.SubmittedAtUtc.HasValue && old.Current.VersionId == value.Current.VersionId && JsonConvert.SerializeObject(old.Current) != JsonConvert.SerializeObject(value.Current)) throw new ConflictException();
            if (!old.Current.SubmittedAtUtc.HasValue && (old.Current.VersionId != value.Current.VersionId || old.Current.Number != value.Current.Number || value.Reviews.Count != old.Reviews.Count)) throw new ConflictException();
            if (old.Current.SubmittedAtUtc.HasValue && value.Submissions.Count != old.Submissions.Count) throw new ConflictException();
            if (old.Current.VersionId != value.Current.VersionId && (!old.Current.SubmittedAtUtc.HasValue || PercentageEvaluationWorkflow.CurrentReview(old)?.Kind != AcademicReviewKind.RequestRevision || value.Current.Number != old.Current.Number + 1 || value.Current.SubmittedAtUtc.HasValue)) throw new ConflictException();
        }
    }
    public sealed partial class InMemoryFinalEvaluationRepository : IPercentageEvaluationRepository
    {
        public Task<RecordSnapshot<PercentageEvaluation>> GetPercentageAsync(string id) { return Read<PercentageEvaluation>("percentage/" + FirebaseKey.Require(id)); }
        public Task SavePercentageAsync(PercentageEvaluation value, string expectedVersion)
        {
            lock (gate)
            {
                Check(); var copy = Clone(value); var key = "percentage/" + FirebaseKey.Require(copy.InternshipId); object existing;
                records.TryGetValue(key, out existing);
                if ((existing == null ? "null_etag" : versions[key]) != expectedVersion) throw new ConflictException();
                if (existing == null && (records.ContainsKey("company/" + copy.InternshipId) || records.ContainsKey("academic/" + copy.InternshipId))) throw new ConflictException();
                PercentageHistory.Check(existing as PercentageEvaluation, copy); records[key] = copy; versions[key] = Guid.NewGuid().ToString("N"); return Task.CompletedTask;
            }
        }
    }
    public sealed partial class FirebaseFinalEvaluationRepository : IPercentageEvaluationRepository
    {
        public async Task<RecordSnapshot<PercentageEvaluation>> GetPercentageAsync(string id)
        {
            var result = await ReadAsync<PercentageEvaluation>("finalEvaluationReviewsV2/" + Key(id));
            if (result.Value != null) { PercentageEvaluationWorkflow.Validate(result.Value); if (result.Value.InternshipId != id) throw new RepositoryException("Mismatched evaluation identity."); }
            return result;
        }
        public async Task SavePercentageAsync(PercentageEvaluation value, string expectedVersion)
        {
            var copy = PercentageEvaluationWorkflow.Clone(value);
            if (string.IsNullOrWhiteSpace(expectedVersion)) throw new ConflictException();
            var old = await GetPercentageAsync(copy.InternshipId);
            if (old.Version != expectedVersion) throw new ConflictException();
            if (old.Value == null && ((await GetCompanyAsync(copy.InternshipId)).Value != null || (await GetAcademicAsync(copy.InternshipId)).Value != null)) throw new ConflictException();
            PercentageHistory.Check(old.Value, copy);
            using (var request = new HttpRequestMessage(HttpMethod.Put, new Uri(root, "finalEvaluationReviewsV2/" + Key(copy.InternshipId) + ".json")))
            {
                request.Headers.TryAddWithoutValidation("if-match", expectedVersion);
                request.Content = new StringContent(JsonConvert.SerializeObject(copy, Json), Encoding.UTF8, "application/json");
                using (var response = await SendAsync(request)) { }
            }
        }
    }
}
