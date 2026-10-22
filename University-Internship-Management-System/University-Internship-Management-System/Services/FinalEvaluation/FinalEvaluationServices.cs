using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public interface ICompanyEvaluationService
    {
        Task<RecordSnapshot<CompanyFinalEvaluation>> GetAsync(string internshipId);
        Task CreateDraftAsync(CompanyFinalEvaluation draft);
        Task UpdateDraftAsync(CompanyFinalEvaluation draft, string expectedVersion);
    }
    public interface IAcademicEvaluationService
    {
        Task<RecordSnapshot<AcademicFinalEvaluation>> GetAsync(string internshipId);
        Task CreateDraftAsync(AcademicFinalEvaluation draft);
        Task UpdateDraftAsync(AcademicFinalEvaluation draft, string expectedVersion);
    }
    public interface ICompletionReviewService { Task<CompletionReview> GetAsync(string internshipId); }
    public interface ICompletionDocumentService { Task<CompletionDocument> GetAsync(string internshipId, string documentId); }

    public abstract class AuthorizedModuleService
    {
        protected readonly IFinalEvaluationRepository Repository;
        private readonly IAuthenticatedUser user;
        private readonly IInternshipAssignmentAuthorizer assignments;
        protected AuthorizedModuleService(IFinalEvaluationRepository repository, IAuthenticatedUser user, IInternshipAssignmentAuthorizer assignments)
        {
            Repository = repository ?? throw new ArgumentNullException("repository");
            this.user = user ?? throw new ArgumentNullException("user");
            this.assignments = assignments ?? throw new ArgumentNullException("assignments");
        }
        protected async Task<string> Authorize(string id, string studentId, string capability)
        {
            FirebaseKey.Require(id);
            string uid = user.UserId;
            if (string.IsNullOrWhiteSpace(uid) || !await assignments.CanAccessAsync(uid, id, studentId, capability).ConfigureAwait(false))
                throw new UnauthorizedAccessException("Authenticated internship assignment is required.");
            return uid;
        }
        protected async Task<T> Prepare<T>(T value, bool company) where T : EvaluationRecord
        {
            if (value == null) throw new ArgumentNullException("value");
            var copy = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
            string uid = await Authorize(copy.InternshipId, copy.StudentId, company ? "companyEvaluation.write" : "academicEvaluation.write").ConfigureAwait(false);
            if (copy.EvaluatorId != uid) throw new UnauthorizedAccessException("Evaluator must be the authenticated assigned user.");
            var policy = await Repository.GetPolicyAsync(copy.PolicyId).ConfigureAwait(false);
            FinalEvaluationValidation.Evaluation(copy, policy, company);
            return copy;
        }
    }
    public sealed class CompanyEvaluationService : AuthorizedModuleService, ICompanyEvaluationService
    {
        public CompanyEvaluationService(IFinalEvaluationRepository repository, IAuthenticatedUser user, IInternshipAssignmentAuthorizer assignments) : base(repository, user, assignments) { }
        public async Task<RecordSnapshot<CompanyFinalEvaluation>> GetAsync(string id)
        {
            await Authorize(id, null, "companyEvaluation.read").ConfigureAwait(false);
            return await Repository.GetCompanyAsync(id).ConfigureAwait(false);
        }
        public async Task CreateDraftAsync(CompanyFinalEvaluation value) { await Repository.CreateCompanyDraftAsync(await Prepare(value, true).ConfigureAwait(false)).ConfigureAwait(false); }
        public async Task UpdateDraftAsync(CompanyFinalEvaluation value, string version) { await Repository.UpdateCompanyDraftAsync(await Prepare(value, true).ConfigureAwait(false), version).ConfigureAwait(false); }
    }
    public sealed class AcademicEvaluationService : AuthorizedModuleService, IAcademicEvaluationService
    {
        public AcademicEvaluationService(IFinalEvaluationRepository repository, IAuthenticatedUser user, IInternshipAssignmentAuthorizer assignments) : base(repository, user, assignments) { }
        public async Task<RecordSnapshot<AcademicFinalEvaluation>> GetAsync(string id)
        {
            await Authorize(id, null, "academicEvaluation.read").ConfigureAwait(false);
            return await Repository.GetAcademicAsync(id).ConfigureAwait(false);
        }
        public Task CreateDraftAsync(AcademicFinalEvaluation value) { throw new NotSupportedException("Independent academic scoring is retired. Use Academic Review decisions."); }
        public Task UpdateDraftAsync(AcademicFinalEvaluation value, string version) { throw new NotSupportedException("Independent academic scoring is retired. Use Academic Review decisions."); }
    }
    public sealed class CompletionReviewService : AuthorizedModuleService, ICompletionReviewService
    {
        public CompletionReviewService(IFinalEvaluationRepository repository, IAuthenticatedUser user, IInternshipAssignmentAuthorizer assignments) : base(repository, user, assignments) { }
        public async Task<CompletionReview> GetAsync(string id)
        {
            await Authorize(id, null, "completionReview.read").ConfigureAwait(false);
            var value = await Repository.GetCompletionReviewAsync(id).ConfigureAwait(false);
            if (value != null) FinalEvaluationValidation.Completion(value);
            return value;
        }
    }
    public sealed class CompletionDocumentService : AuthorizedModuleService, ICompletionDocumentService
    {
        public CompletionDocumentService(IFinalEvaluationRepository repository, IAuthenticatedUser user, IInternshipAssignmentAuthorizer assignments) : base(repository, user, assignments) { }
        public async Task<CompletionDocument> GetAsync(string id, string documentId)
        {
            await Authorize(id, null, "completionDocument.read").ConfigureAwait(false);
            var value = await Repository.GetCompletionDocumentAsync(id, documentId).ConfigureAwait(false);
            if (value != null) FinalEvaluationValidation.Document(value);
            return value;
        }
    }
}
