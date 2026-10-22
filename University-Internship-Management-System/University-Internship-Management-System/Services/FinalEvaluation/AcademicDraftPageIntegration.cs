using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class AcademicDraftPageContext
    {
        public AcademicDraftWorkflow Workflow { get; private set; }
        public AcademicDraftPlacement[] Placements { get; private set; }
        public string UserId { get; private set; }
        public bool IsDevelopment { get; private set; }
        public AcademicDraftPageContext(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            IInternshipAssignmentAuthorizer assignments, ICompanyFinalEvaluationAccess access, IEnumerable<AcademicDraftPlacement> placements,
            IAcademicSubmissionRequirements submissionRequirements = null)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
            UserId = user.UserId; Placements = placements.ToArray();
            Workflow = new AcademicDraftWorkflow(repository, user, assignments, access, Placements, submissionRequirements);
        }
        internal void MarkDevelopment() { IsDevelopment = true; }
    }
    public static class AcademicDraftPageIntegration
    {
        // Trusted request-level server composition only. Nothing is registered by default.
        public static Func<HttpContext, AcademicDraftPageContext> Factory { get; set; }
        public static AcademicDraftPageContext Resolve(HttpContext context)
        {
            if (Factory != null) return Factory(context);
            if (!context.IsDebuggingEnabled || !context.Request.IsLocal || Environment.GetEnvironmentVariable("UIMS_ACADEMIC_DRAFT_IN_MEMORY") != "1") return null;
            const string key = "FinalEvaluation.AcademicDraft.DevelopmentContext";
            var existing = context.Session[key] as AcademicDraftPageContext;
            if (existing != null) return existing;
            string[] ids = { "dev-academic-internship", "dev-academic-company-draft", "dev-academic-company-missing", "dev-academic-locked" };
            var placements = ids.Select(id => new AcademicDraftPlacement { InternshipId = id, StudentId = id + "-student", StudentName = "Fictional " + id,
                StudentNumber = "DEV-ONLY", CompanyName = "Fictional test company", PolicyId = "dev-academic-policy", PolicyVersion = "test-v1" }).ToArray();
            var policy = new EvaluationPolicy { PolicyId = "dev-academic-policy", Version = "test-v1", IsApproved = true, AcademicCommentsRequired = true,
                CompanyCriteria = new Dictionary<string, CriterionPolicy> { { "companyWork", new CriterionPolicy { Label = "Company work (test only)", Minimum = 0, Maximum = 10 } } },
                AcademicCriteria = new Dictionary<string, CriterionPolicy> {
                    { "analysis", new CriterionPolicy { Label = "Academic analysis (test only)", Minimum = 1, Maximum = 6, Required = true } },
                    { "reflection", new CriterionPolicy { Label = "Academic reflection (test only)", Minimum = -1, Maximum = 9, Required = true } } } };
            var companies = new[] { 0, 1 }.Select(index => new CompanyFinalEvaluation { InternshipId = ids[index], StudentId = placements[index].StudentId,
                EvaluatorId = "dev-company-fixture", PolicyId = policy.PolicyId, PolicyVersion = policy.Version,
                Status = index == 0 ? EvaluationStatus.Submitted : EvaluationStatus.Draft,
                Feedback = index == 0 ? "Fictional submitted company feedback" : "CONFIDENTIAL_DRAFT_MUST_NOT_APPEAR",
                SubmittedAtUtc = index == 0 ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
                Scores = new Dictionary<string, decimal?> { { "companyWork", 8 } },
                CriterionFeedback = new Dictionary<string, string> { { "companyWork", "Fictional criterion feedback" } } }).ToArray();
            // A historical read-only fixture remains separate from the tested submission flow.
            var locked = new AcademicFinalEvaluation { InternshipId = ids[3], StudentId = placements[3].StudentId, EvaluatorId = "dev-academic-supervisor",
                PolicyId = policy.PolicyId, PolicyVersion = policy.Version, Status = EvaluationStatus.Submitted, SubmittedAtUtc = DateTimeOffset.UtcNow,
                Feedback = "Fictional read-only academic record", Scores = new Dictionary<string, decimal?> { { "analysis", 4 } } };
            var access = new DevelopmentAccess(ids);
            var result = new AcademicDraftPageContext(new InMemoryFinalEvaluationRepository(new[] { policy }, companies, new[] { locked }),
                new DevelopmentUser(), access, access, placements, new DevelopmentSubmissionRequirements(ids));
            result.MarkDevelopment(); context.Session[key] = result; return result;
        }
        private sealed class DevelopmentUser : IAuthenticatedUser { public string UserId { get { return "dev-academic-supervisor"; } } }
        private sealed class DevelopmentSubmissionRequirements : IAcademicSubmissionRequirements
        {
            private readonly string[] ids;
            public DevelopmentSubmissionRequirements(string[] ids) { this.ids = ids; }
            public Task<bool> IsEligibleAsync(string id) { return Task.FromResult(ids.Contains(id)); }
            public Task<bool> ArePolicyRequirementsSatisfiedAsync(AcademicFinalEvaluation candidate, EvaluationPolicy policy)
            { return Task.FromResult(ids.Contains(candidate.InternshipId) && policy.PolicyId == "dev-academic-policy" && policy.Version == "test-v1"); }
        }
        private sealed class DevelopmentAccess : ICompanyFinalEvaluationAccess, IInternshipAssignmentAuthorizer
        {
            private readonly string[] ids;
            public DevelopmentAccess(string[] ids) { this.ids = ids; }
            public Task<bool> IsAcademicSupervisorAsync(string uid) { return Task.FromResult(uid == "dev-academic-supervisor"); }
            public Task<bool> IsAssignedAcademicSupervisorAsync(string uid, string id) { return Task.FromResult(uid == "dev-academic-supervisor" && ids.Contains(id)); }
            public Task<bool> CanAccessAsync(string uid, string id, string student, string capability)
            { return Task.FromResult(uid == "dev-academic-supervisor" && ids.Contains(id) && (student == null || student == id + "-student") && (capability == "academicEvaluation.read" || capability == "academicEvaluation.write")); }
            public Task<bool> IsCompanySupervisorAsync(string uid) { return Task.FromResult(false); }
            public Task<bool> IsAssignedCompanySupervisorAsync(string uid, string id, string student) { return Task.FromResult(false); }
            public Task<bool> IsEligibleAsync(string id) { return Task.FromResult(false); }
        }
    }
}
