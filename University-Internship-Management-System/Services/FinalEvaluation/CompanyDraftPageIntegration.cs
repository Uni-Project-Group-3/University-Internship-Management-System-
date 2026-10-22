using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class CompanyDraftPageContext
    {
        public CompanyDraftWorkflow Workflow { get; private set; }
        public CompanyDraftPlacement[] Placements { get; private set; }
        public string UserId { get; private set; }
        public bool IsDevelopment { get; private set; }
        public CompanyDraftPageContext(IFinalEvaluationRepository repository, IAuthenticatedUser user,
            IInternshipAssignmentAuthorizer authorizer, IEnumerable<CompanyDraftPlacement> placements, ICompanyFinalEvaluationAccess submissionAccess = null)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
            UserId = user.UserId;
            Placements = placements.ToArray();
            Workflow = new CompanyDraftWorkflow(repository, user, authorizer, Placements, submissionAccess);
        }
        internal void MarkDevelopment() { IsDevelopment = true; }
    }

    public static class CompanyDraftPageIntegration
    {
        // Register ONLY in trusted server composition after authentication/placement adapters exist.
        // No query parameter, role, session display name or browser demo identity configures this factory.
        public static Func<HttpContext, CompanyDraftPageContext> Factory { get; set; }
        public static CompanyDraftPageContext Resolve(HttpContext context)
        {
            // No runtime demo users or in-memory business records.
            return Factory == null ? null : Factory(context);
        }
        private sealed class DevelopmentUser : IAuthenticatedUser { public string UserId { get { return "dev-company-evaluator"; } } }
        private sealed class DevelopmentSubmissionAccess : ICompanyFinalEvaluationAccess
        {
            private readonly CompanyDraftPlacement[] placements;
            internal DevelopmentSubmissionAccess(CompanyDraftPlacement[] placements) { this.placements = placements; }
            public Task<bool> IsCompanySupervisorAsync(string uid) { return Task.FromResult(uid == "dev-company-evaluator"); }
            public Task<bool> IsAssignedCompanySupervisorAsync(string uid, string id, string student) { return Task.FromResult(uid == "dev-company-evaluator" && placements.Any(p => p.InternshipId == id && p.StudentId == student)); }
            public Task<bool> IsEligibleAsync(string id) { return Task.FromResult(placements.Any(p => p.InternshipId == id)); }
            public Task<bool> IsAcademicSupervisorAsync(string uid) { return Task.FromResult(false); }
            public Task<bool> IsAssignedAcademicSupervisorAsync(string uid, string id) { return Task.FromResult(false); }
        }
        private sealed class DevelopmentAssignment : IInternshipAssignmentAuthorizer
        {
            private readonly CompanyDraftPlacement[] placements;
            internal DevelopmentAssignment(CompanyDraftPlacement[] placements) { this.placements = placements; }
            public Task<bool> CanAccessAsync(string userId, string internshipId, string studentId, string capability)
            {
                return Task.FromResult(userId == "dev-company-evaluator" && placements.Any(p => p.InternshipId == internshipId &&
                    (studentId == null || studentId == p.StudentId)) &&
                    (capability == "companyEvaluation.read" || capability == "companyEvaluation.write"));
            }
        }
    }
}
