using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Privileged persistence capability; application callers must use the authorized service.
    public interface ICompanySubmissionRepository
    {
        Task SubmitCompanyAsync(CompanyFinalEvaluation candidate, string expectedVersion);
    }
    // Real adapters must resolve these answers from trusted authentication/placement records.
    // Eligibility is deliberately not calculated from invented hours, dates, tasks or grades here.
    public interface ICompanyFinalEvaluationAccess
    {
        Task<bool> IsCompanySupervisorAsync(string userId);
        Task<bool> IsAssignedCompanySupervisorAsync(string userId, string internshipId, string studentId);
        Task<bool> IsEligibleAsync(string internshipId);
        Task<bool> IsAcademicSupervisorAsync(string userId);
        Task<bool> IsAssignedAcademicSupervisorAsync(string userId, string internshipId);
    }
    public interface ICompanySubmissionService
    {
        Task SubmitAsync(CompanyFinalEvaluation candidate, string expectedVersion);
    }
    public interface ISubmittedCompanyEvaluationReader
    {
        Task<CompanyFinalEvaluation> GetSubmittedAsync(string internshipId);
    }
    public static class CompanySubmissionValidation
    {
        public static void Validate(CompanyFinalEvaluation candidate, EvaluationPolicy policy)
        {
            if (policy == null || !policy.IsProductionApproved || !policy.CompanyCommentsRequired.HasValue)
                throw new ArgumentException("An approved policy with an explicit company comment requirement is required.");
            FinalEvaluationValidation.Evaluation(candidate, policy, true, true);
            if (policy.CompanyCommentsRequired.Value && string.IsNullOrWhiteSpace(candidate.Feedback))
                throw new ArgumentException("General evaluation comments are required by the policy.");
            if ((candidate.Feedback ?? "").Length > 3000) throw new ArgumentException("General comments cannot exceed 3,000 characters.");
            if (candidate.CriterionFeedback == null) throw new ArgumentException("Criterion feedback must be an object.");
            foreach (var feedback in candidate.CriterionFeedback)
            {
                if (!policy.CompanyCriteria.ContainsKey(feedback.Key) || (feedback.Value ?? "").Length > 1000)
                    throw new ArgumentException("Unknown criterion or invalid feedback length.");
            }
            foreach (var criterion in policy.CompanyCriteria)
            {
                string feedback;
                if (criterion.Value.RequireFeedback && (!candidate.CriterionFeedback.TryGetValue(criterion.Key, out feedback) || string.IsNullOrWhiteSpace(feedback)))
                    throw new ArgumentException("Criterion feedback required for " + (criterion.Value.Label ?? criterion.Key) + ".");
            }
        }
    }
    public sealed class CompanySubmissionService : ICompanySubmissionService
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICompanyFinalEvaluationAccess access;
        public CompanySubmissionService(IFinalEvaluationRepository repository, IAuthenticatedUser user, ICompanyFinalEvaluationAccess access)
        { this.repository = repository ?? throw new ArgumentNullException("repository"); this.user = user ?? throw new ArgumentNullException("user"); this.access = access; }
        public async Task SubmitAsync(CompanyFinalEvaluation candidate, string expectedVersion)
        {
            if (candidate == null) throw new ArgumentNullException("candidate");
            var copy = JsonConvert.DeserializeObject<CompanyFinalEvaluation>(JsonConvert.SerializeObject(candidate));
            string uid = user.UserId;
            if (access == null || string.IsNullOrWhiteSpace(uid) || copy.EvaluatorId != uid ||
                !await access.IsCompanySupervisorAsync(uid).ConfigureAwait(false) ||
                !await access.IsAssignedCompanySupervisorAsync(uid, copy.InternshipId, copy.StudentId).ConfigureAwait(false))
                throw new UnauthorizedAccessException("A verified assigned Company Supervisor is required.");
            if (!await access.IsEligibleAsync(copy.InternshipId).ConfigureAwait(false))
                throw new ArgumentException("The internship is not eligible for final evaluation.");
            if (string.IsNullOrWhiteSpace(expectedVersion)) throw new ConflictException();
            var current = await repository.GetCompanyAsync(copy.InternshipId).ConfigureAwait(false);
            if (current.Value == null || current.Version != expectedVersion) throw new ConflictException();
            FinalEvaluationValidation.UnchangedIdentity(current.Value, copy);
            var policy = await repository.GetPolicyAsync(copy.PolicyId).ConfigureAwait(false);
            CompanySubmissionValidation.Validate(copy, policy);
            var writer = repository as ICompanySubmissionRepository;
            if (writer == null) throw new RepositoryException("Company submission persistence is not configured.");
            await writer.SubmitCompanyAsync(copy, expectedVersion).ConfigureAwait(false);
        }
    }
    public sealed class SubmittedCompanyEvaluationReader : ISubmittedCompanyEvaluationReader
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICompanyFinalEvaluationAccess access;
        public SubmittedCompanyEvaluationReader(IFinalEvaluationRepository repository, IAuthenticatedUser user, ICompanyFinalEvaluationAccess access)
        { this.repository = repository ?? throw new ArgumentNullException("repository"); this.user = user ?? throw new ArgumentNullException("user"); this.access = access; }
        public async Task<CompanyFinalEvaluation> GetSubmittedAsync(string internshipId)
        {
            FirebaseKey.Require(internshipId);
            string uid = user.UserId;
            if (access == null || string.IsNullOrWhiteSpace(uid) || !await access.IsAcademicSupervisorAsync(uid).ConfigureAwait(false) ||
                !await access.IsAssignedAcademicSupervisorAsync(uid, internshipId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            var record = await repository.GetCompanyAsync(internshipId).ConfigureAwait(false);
            return record.Value != null && record.Value.Status == EvaluationStatus.Submitted ? record.Value : null;
        }
    }
}
