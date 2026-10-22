using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Supplied by trusted server placement adapters; never built from form/query identity fields.
    public sealed class CompanyDraftPlacement
    {
        public string InternshipId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
    }

    public sealed class CompanyDraftState
    {
        public CompanyDraftPlacement Placement { get; internal set; }
        public EvaluationPolicy Policy { get; internal set; }
        public RecordSnapshot<CompanyFinalEvaluation> Record { get; internal set; }
        public bool IsSubmitted { get { return Record.Value != null && Record.Value.Status == EvaluationStatus.Submitted; } }
    }

    public sealed class CompanyDraftWorkflow
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly ICompanyEvaluationService service;
        private readonly IAuthenticatedUser user;
        private readonly CompanyDraftPlacement[] placements;
        private readonly ICompanySubmissionService submission;
        public bool SubmissionConfigured { get { return submission != null; } }
        public CompanyDraftWorkflow(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            IInternshipAssignmentAuthorizer authorizer, IEnumerable<CompanyDraftPlacement> placements, ICompanyFinalEvaluationAccess submissionAccess = null)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository");
            this.user = user ?? throw new ArgumentNullException("user");
            service = new CompanyEvaluationService(repository, user, authorizer);
            if (submissionAccess != null) submission = new CompanySubmissionService(repository, user, submissionAccess);
            // Copy trusted request context; browser values cannot become assignment records.
            this.placements = (placements ?? throw new ArgumentNullException("placements"))
                .Select(p => new CompanyDraftPlacement { InternshipId = p.InternshipId, StudentId = p.StudentId,
                    StudentName = p.StudentName, StudentNumber = p.StudentNumber, PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion }).ToArray();
        }
        public async Task<CompanyDraftState> LoadAsync(string internshipId)
        {
            var placement = placements.SingleOrDefault(p => p.InternshipId == internshipId);
            if (placement == null) throw new UnauthorizedAccessException("Internship is not assigned to this account.");
            var record = await service.GetAsync(internshipId).ConfigureAwait(false);
            if (record.Value != null && (record.Value.StudentId != placement.StudentId || record.Value.EvaluatorId != user.UserId))
                throw new UnauthorizedAccessException("Evaluation does not belong to the assigned evaluator and student.");
            string policyId = record.Value == null ? placement.PolicyId : record.Value.PolicyId;
            string policyVersion = record.Value == null ? placement.PolicyVersion : record.Value.PolicyVersion;
            var policy = await repository.GetPolicyAsync(policyId).ConfigureAwait(false);
            if (policy == null || policy.Version != policyVersion || policy.CompanyCriteria == null || policy.CompanyCriteria.Count == 0)
                throw new ArgumentException("The assigned evaluation policy is unavailable or has no company criteria.");
            // Validate the configured ranges even when displaying an unanswered form.
            FinalEvaluationValidation.Evaluation(new CompanyFinalEvaluation { InternshipId = internshipId,
                StudentId = placement.StudentId, EvaluatorId = user.UserId, PolicyId = policyId, PolicyVersion = policyVersion }, policy, true);
            return new CompanyDraftState { Placement = placement, Policy = policy, Record = record };
        }
        public async Task SaveAsync(string internshipId, string expectedVersion, string policyId, string policyVersion,
            IDictionary<string, string> scores, IDictionary<string, string> feedback, string comments)
        {
            var value = await PrepareCandidateAsync(internshipId, expectedVersion, policyId, policyVersion, scores, feedback, comments).ConfigureAwait(false);
            if (expectedVersion == null) await service.CreateDraftAsync(value).ConfigureAwait(false);
            else await service.UpdateDraftAsync(value, expectedVersion).ConfigureAwait(false);
        }
        public async Task SubmitAsync(string internshipId, string expectedVersion, string policyId, string policyVersion,
            IDictionary<string, string> scores, IDictionary<string, string> feedback, string comments)
        {
            if (submission == null) throw new UnauthorizedAccessException("Trusted submission adapters are not connected.");
            if (string.IsNullOrWhiteSpace(expectedVersion)) throw new ConflictException();
            var value = await PrepareCandidateAsync(internshipId, expectedVersion, policyId, policyVersion, scores, feedback, comments).ConfigureAwait(false);
            await submission.SubmitAsync(value, expectedVersion).ConfigureAwait(false);
        }
        private async Task<CompanyFinalEvaluation> PrepareCandidateAsync(string internshipId, string expectedVersion, string policyId, string policyVersion,
            IDictionary<string, string> scores, IDictionary<string, string> feedback, string comments)
        {
            var state = await LoadAsync(internshipId).ConfigureAwait(false);
            if (state.IsSubmitted || (expectedVersion == null ? state.Record.Value != null : state.Record.Value == null || state.Record.Version != expectedVersion))
                throw new ConflictException();
            if (state.Policy.PolicyId != policyId || state.Policy.Version != policyVersion) throw new ConflictException();
            if (scores == null || feedback == null) throw new ArgumentException("Evaluation form values are required.");
            var value = new CompanyFinalEvaluation { InternshipId = internshipId, StudentId = state.Placement.StudentId,
                EvaluatorId = user.UserId, PolicyId = state.Policy.PolicyId, PolicyVersion = state.Policy.Version,
                Feedback = comments ?? "", Status = EvaluationStatus.Draft };
            if (value.Feedback.Length > 3000) throw new ArgumentException("General comments cannot exceed 3,000 characters.");
            foreach (var score in scores)
            {
                if (!state.Policy.CompanyCriteria.ContainsKey(score.Key)) throw new ArgumentException("Unknown evaluation criterion.");
                decimal parsed;
                if (string.IsNullOrWhiteSpace(score.Value)) value.Scores[score.Key] = null;
                else if (decimal.TryParse(score.Value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out parsed)) value.Scores[score.Key] = parsed;
                else throw new ArgumentException("Enter a valid numeric score for " + (state.Policy.CompanyCriteria[score.Key].Label ?? score.Key) + ".");
            }
            foreach (var item in feedback)
            {
                if (!state.Policy.CompanyCriteria.ContainsKey(item.Key)) throw new ArgumentException("Unknown feedback criterion.");
                if ((item.Value ?? "").Length > 1000) throw new ArgumentException("Criterion feedback cannot exceed 1,000 characters.");
                value.CriterionFeedback[item.Key] = item.Value ?? "";
            }
            FinalEvaluationValidation.Evaluation(value, state.Policy, true);
            return value;
        }
    }
}
