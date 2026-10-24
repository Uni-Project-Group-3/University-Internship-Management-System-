using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class RepositoryException : Exception
    {
        public RepositoryException(string message, Exception inner = null) : base(message, inner) { }
    }
    public sealed class FirebaseQueryIndexException : Exception
    {
        public FirebaseQueryIndexException() : base("Firebase requires an index for this query.") { }
    }
    public sealed class ConflictException : Exception
    {
        public ConflictException() : base("Record exists, is no longer a draft, or changed concurrently. Reload before saving.") { }
    }
    public sealed class RecordSnapshot<T>
    {
        public T Value { get; private set; }
        public string Version { get; private set; }
        public RecordSnapshot(T value, string version) { Value = value; Version = version; }
    }
    public interface IFinalEvaluationRepository
    {
        Task<RecordSnapshot<CompanyFinalEvaluation>> GetCompanyAsync(string internshipId);
        Task CreateCompanyDraftAsync(CompanyFinalEvaluation value);
        Task UpdateCompanyDraftAsync(CompanyFinalEvaluation value, string expectedVersion);
        Task<RecordSnapshot<AcademicFinalEvaluation>> GetAcademicAsync(string internshipId);
        Task CreateAcademicDraftAsync(AcademicFinalEvaluation value);
        Task UpdateAcademicDraftAsync(AcademicFinalEvaluation value, string expectedVersion);
        Task<CompletionReview> GetCompletionReviewAsync(string internshipId);
        Task<CompletionDocument> GetCompletionDocumentAsync(string internshipId, string documentId);
        Task<EvaluationPolicy> GetPolicyAsync(string policyId);
    }
    public interface IAuthenticatedUser { string UserId { get; } }
    // Implement using trusted server-side placement records, never request parameters or demo IDs.
    public interface IInternshipAssignmentAuthorizer
    {
        Task<bool> CanAccessAsync(string userId, string internshipId, string studentId, string capability);
    }
    public interface IAccessTokenProvider { Task<string> GetAccessTokenAsync(); }
    public interface ICancellableAccessTokenProvider : IAccessTokenProvider
    {
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
    }

    public static class FirebaseKey
    {
        public static string Require(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > 768)
                throw new ArgumentException("A nonempty Firebase key of at most 768 UTF-8 bytes is required.");
            foreach (char c in value)
                if (c < 32 || c == 127 || ".#$[]/".IndexOf(c) >= 0)
                    throw new ArgumentException("Invalid Firebase key.");
            return value;
        }
    }
}
