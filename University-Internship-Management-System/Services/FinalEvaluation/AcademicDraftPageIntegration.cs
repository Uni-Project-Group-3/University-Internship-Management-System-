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
            // No runtime demo users or in-memory business records.
            return Factory == null ? null : Factory(context);
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
