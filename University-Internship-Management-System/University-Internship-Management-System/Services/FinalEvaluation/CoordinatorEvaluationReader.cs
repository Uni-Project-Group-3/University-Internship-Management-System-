using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public interface ICoordinatorEvaluationAccess
    {
        Task<bool> IsCoordinatorAsync(string userId);
        Task<bool> CanReviewInternshipAsync(string userId, string internshipId);
    }
    public sealed class CoordinatorSubmittedEvaluations
    {
        public CompanyFinalEvaluation Company { get; internal set; }
        public AcademicFinalEvaluation Academic { get; internal set; }
        public PercentageEvaluation PercentageEvaluation { get; internal set; }
        public bool LegacyRecordsDetected { get; internal set; }
    }
    public interface ICoordinatorEvaluationReader
    {
        Task<CoordinatorSubmittedEvaluations> GetSubmittedAsync(string internshipId);
    }
    // Handoff only: no readiness computation, completion decisions, notifications or write API.
    public sealed class CoordinatorEvaluationReader : ICoordinatorEvaluationReader
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICoordinatorEvaluationAccess access;
        public CoordinatorEvaluationReader(IFinalEvaluationRepository repository, IAuthenticatedUser user, ICoordinatorEvaluationAccess access)
        { this.repository = repository ?? throw new ArgumentNullException("repository"); this.user = user ?? throw new ArgumentNullException("user"); this.access = access; }
        public async Task<CoordinatorSubmittedEvaluations> GetSubmittedAsync(string internshipId)
        {
            FirebaseKey.Require(internshipId); string uid = user.UserId;
            if (access == null || string.IsNullOrWhiteSpace(uid) || !await access.IsCoordinatorAsync(uid).ConfigureAwait(false) ||
                !await access.CanReviewInternshipAsync(uid, internshipId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            var company = await repository.GetCompanyAsync(internshipId).ConfigureAwait(false);
            var academic = await repository.GetAcademicAsync(internshipId).ConfigureAwait(false);
            var percentageRepository = repository as IPercentageEvaluationRepository;
            var percentage = percentageRepository == null ? null : (await percentageRepository.GetPercentageAsync(internshipId)).Value;
            if (percentage != null) PercentageEvaluationWorkflow.Validate(percentage);
            if (percentage != null && percentage.InternshipId != internshipId) throw new RepositoryException("Mismatched evaluation identity.");
            // Old forms remain archived at their original paths and are not current review approvals.
            return new CoordinatorSubmittedEvaluations { PercentageEvaluation = PercentageEvaluationWorkflow.Clone(percentage), LegacyRecordsDetected = company.Value != null || academic.Value != null };
        }
    }
}
