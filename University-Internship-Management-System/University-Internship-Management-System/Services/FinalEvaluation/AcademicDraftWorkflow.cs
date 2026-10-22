using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class AcademicDraftPlacement
    {
        public string InternshipId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string CompanyName { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
    }
    public sealed class AcademicDraftState
    {
        public AcademicDraftPlacement Placement { get; internal set; }
        public RecordSnapshot<AcademicFinalEvaluation> Record { get; internal set; }
        public EvaluationPolicy Policy { get; internal set; }
        public bool IsSubmitted { get { return Record.Value != null && Record.Value.Status == EvaluationStatus.Submitted; } }
    }
    public sealed class AcademicCompanyReview
    {
        public CompanyFinalEvaluation Evaluation { get; internal set; }
        public EvaluationPolicy OriginalPolicy { get; internal set; }
    }
    public sealed class AcademicDraftWorkflow
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICompanyFinalEvaluationAccess access;
        private readonly IAcademicEvaluationService academic;
        private readonly ISubmittedCompanyEvaluationReader companyReader;
        private readonly AcademicDraftPlacement[] placements;
        private readonly IAcademicSubmissionService submission;
        public bool SubmissionConfigured { get { return submission != null; } }
        public AcademicDraftWorkflow(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            IInternshipAssignmentAuthorizer assignments, ICompanyFinalEvaluationAccess access, IEnumerable<AcademicDraftPlacement> placements,
            IAcademicSubmissionRequirements submissionRequirements = null)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository");
            this.user = user ?? throw new ArgumentNullException("user"); this.access = access;
            academic = new AcademicEvaluationService(repository, user, assignments);
            companyReader = new SubmittedCompanyEvaluationReader(repository, user, access);
            if (submissionRequirements != null) submission = new AcademicSubmissionService(repository, user, access, submissionRequirements);
            this.placements = (placements ?? throw new ArgumentNullException("placements")).Select(p => new AcademicDraftPlacement {
                InternshipId = p.InternshipId, StudentId = p.StudentId, StudentName = p.StudentName,
                StudentNumber = p.StudentNumber, CompanyName = p.CompanyName, PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion }).ToArray();
        }
        private async Task<AcademicDraftPlacement> AuthorizeAsync(string internshipId)
        {
            FirebaseKey.Require(internshipId);
            var placement = placements.SingleOrDefault(p => p.InternshipId == internshipId);
            string uid = user.UserId;
            if (placement == null || access == null || string.IsNullOrWhiteSpace(uid) ||
                !await access.IsAcademicSupervisorAsync(uid).ConfigureAwait(false) ||
                !await access.IsAssignedAcademicSupervisorAsync(uid, internshipId).ConfigureAwait(false))
                throw new UnauthorizedAccessException("A verified assigned Academic Supervisor is required.");
            return placement;
        }
        public async Task<AcademicCompanyReview> ReviewCompanyAsync(string internshipId)
        {
            var placement = await AuthorizeAsync(internshipId).ConfigureAwait(false);
            var record = await companyReader.GetSubmittedAsync(internshipId).ConfigureAwait(false);
            if (record == null) return new AcademicCompanyReview();
            if (record.StudentId != placement.StudentId) throw new RepositoryException("Company evaluation student does not match the assigned internship.");
            var policy = await repository.GetPolicyAsync(record.PolicyId).ConfigureAwait(false);
            return new AcademicCompanyReview { Evaluation = record,
                OriginalPolicy = policy != null && policy.Version == record.PolicyVersion ? policy : null };
        }
        public async Task<AcademicDraftState> LoadAsync(string internshipId)
        {
            var placement = await AuthorizeAsync(internshipId).ConfigureAwait(false);
            var record = await academic.GetAsync(internshipId).ConfigureAwait(false);
            if (record.Value != null && (record.Value.StudentId != placement.StudentId || record.Value.EvaluatorId != user.UserId))
                throw new UnauthorizedAccessException("Academic evaluation does not belong to the assigned supervisor and student.");
            string policyId = record.Value == null ? placement.PolicyId : record.Value.PolicyId;
            string version = record.Value == null ? placement.PolicyVersion : record.Value.PolicyVersion;
            var policy = await repository.GetPolicyAsync(policyId).ConfigureAwait(false);
            if (policy == null || !policy.IsApproved || policy.Version != version || policy.AcademicCriteria == null || policy.AcademicCriteria.Count == 0)
                throw new ArgumentException("An applicable approved academic evaluation policy is required.");
            FinalEvaluationValidation.Evaluation(new AcademicFinalEvaluation { InternshipId = internshipId, StudentId = placement.StudentId,
                EvaluatorId = user.UserId, PolicyId = policyId, PolicyVersion = version }, policy, false);
            return new AcademicDraftState { Placement = placement, Record = record, Policy = policy };
        }
        public async Task SaveAsync(string internshipId, string expectedVersion, string policyId, string policyVersion,
            IDictionary<string, string> scores, string comments, IDictionary<string, string> feedback = null)
        {
            var value = await PrepareAsync(internshipId, expectedVersion, policyId, policyVersion, scores, comments, feedback).ConfigureAwait(false);
            if (expectedVersion == null) await academic.CreateDraftAsync(value).ConfigureAwait(false);
            else await academic.UpdateDraftAsync(value, expectedVersion).ConfigureAwait(false);
        }
        public async Task SubmitAsync(string internshipId, string expectedVersion, string policyId, string policyVersion,
            IDictionary<string, string> scores, string comments, IDictionary<string, string> feedback = null)
        {
            if (submission == null) throw new UnauthorizedAccessException("Trusted academic submission integrations are not connected.");
            if (string.IsNullOrWhiteSpace(expectedVersion)) throw new ConflictException();
            var value = await PrepareAsync(internshipId, expectedVersion, policyId, policyVersion, scores, comments, feedback).ConfigureAwait(false);
            await submission.SubmitAsync(value, expectedVersion).ConfigureAwait(false);
        }
        private async Task<AcademicFinalEvaluation> PrepareAsync(string internshipId, string expectedVersion, string policyId, string policyVersion,
            IDictionary<string, string> scores, string comments, IDictionary<string, string> feedback)
        {
            var state = await LoadAsync(internshipId).ConfigureAwait(false);
            if (state.IsSubmitted || (expectedVersion == null ? state.Record.Value != null : state.Record.Value == null || state.Record.Version != expectedVersion) ||
                state.Policy.PolicyId != policyId || state.Policy.Version != policyVersion) throw new ConflictException();
            if (scores == null) throw new ArgumentException("Academic scores must be an object.");
            var value = new AcademicFinalEvaluation { InternshipId = internshipId, StudentId = state.Placement.StudentId,
                EvaluatorId = user.UserId, PolicyId = state.Policy.PolicyId, PolicyVersion = state.Policy.Version, Feedback = comments ?? "" };
            if (value.Feedback.Length > 2000) throw new ArgumentException("Academic comments cannot exceed 2,000 characters.");
            foreach (var score in scores)
            {
                if (!state.Policy.AcademicCriteria.ContainsKey(score.Key)) throw new ArgumentException("Unknown academic criterion.");
                decimal parsed;
                if (string.IsNullOrWhiteSpace(score.Value)) value.Scores[score.Key] = null;
                else if (decimal.TryParse(score.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed))
                    value.Scores[score.Key] = parsed;
                else throw new ArgumentException("Enter a numeric score for " + (state.Policy.AcademicCriteria[score.Key].Label ?? score.Key) + ".");
            }
            FinalEvaluationValidation.Evaluation(value, state.Policy, false);
            if (feedback != null) foreach (var item in feedback)
            {
                if (!state.Policy.AcademicCriteria.ContainsKey(item.Key) || (item.Value ?? "").Length > 1000)
                    throw new ArgumentException("Unknown academic feedback criterion or feedback exceeds 1,000 characters.");
                value.CriterionFeedback[item.Key] = item.Value ?? "";
            }
            return value;
        }
    }
}
