using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public interface IAcademicSubmissionRepository
    {
        Task SubmitAcademicAsync(AcademicFinalEvaluation candidate, string expectedVersion);
    }
    // Trusted integration only. No invented hours/tasks/eligibility or browser assertions.
    public interface IAcademicSubmissionRequirements
    {
        Task<bool> IsEligibleAsync(string internshipId);
        Task<bool> ArePolicyRequirementsSatisfiedAsync(AcademicFinalEvaluation candidate, EvaluationPolicy policy);
    }
    public interface IAcademicSubmissionService
    {
        Task SubmitAsync(AcademicFinalEvaluation candidate, string expectedVersion);
    }
    public static class AcademicSubmissionValidation
    {
        public static void Validate(AcademicFinalEvaluation candidate, EvaluationPolicy policy)
        {
            if (policy == null || !policy.IsApproved || !policy.AcademicCommentsRequired.HasValue)
                throw new ArgumentException("An approved policy with an explicit academic comment requirement is required.");
            FinalEvaluationValidation.Evaluation(candidate, policy, false, true);
            if (policy.AcademicCommentsRequired.Value && string.IsNullOrWhiteSpace(candidate.Feedback))
                throw new ArgumentException("Academic comments are required by the policy.");
            if ((candidate.Feedback ?? "").Length > 2000) throw new ArgumentException("Academic comments cannot exceed 2,000 characters.");
            if (candidate.CriterionFeedback == null) throw new ArgumentException("Academic criterion feedback must be an object.");
            foreach (var item in candidate.CriterionFeedback)
                if (!policy.AcademicCriteria.ContainsKey(item.Key) || (item.Value ?? "").Length > 1000)
                    throw new ArgumentException("Unknown academic feedback criterion or invalid feedback length.");
            foreach (var criterion in policy.AcademicCriteria)
            {
                string feedback;
                if (criterion.Value.RequireFeedback && (!candidate.CriterionFeedback.TryGetValue(criterion.Key, out feedback) || string.IsNullOrWhiteSpace(feedback)))
                    throw new ArgumentException("Academic criterion feedback required for " + (criterion.Value.Label ?? criterion.Key) + ".");
            }
        }
    }
    public sealed class AcademicSubmissionService : IAcademicSubmissionService
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICompanyFinalEvaluationAccess access;
        private readonly IAcademicSubmissionRequirements requirements;
        public AcademicSubmissionService(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            ICompanyFinalEvaluationAccess access, IAcademicSubmissionRequirements requirements)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository");
            this.user = user ?? throw new ArgumentNullException("user"); this.access = access; this.requirements = requirements;
        }
        public Task SubmitAsync(AcademicFinalEvaluation candidate, string expectedVersion)
        { throw new NotSupportedException("Independent academic scoring is retired. Use Academic Review decisions."); }
    }
}
