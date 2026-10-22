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
            if (Factory != null) return Factory(context);
            if (!context.IsDebuggingEnabled || !context.Request.IsLocal ||
                Environment.GetEnvironmentVariable("UIMS_COMPANY_DRAFT_IN_MEMORY") != "1") return null;
            const string key = "FinalEvaluation.CompanyDraft.DevelopmentContext";
            var existing = context.Session[key] as CompanyDraftPageContext;
            if (existing != null) return existing;
            var placement = new CompanyDraftPlacement { InternshipId = "dev-company-internship", StudentId = "dev-company-student",
                StudentName = "Lina Haddad", StudentNumber = "DEMO-2101", PolicyId = "dev-company-policy", PolicyVersion = "test-v1" };
            var placements = new[] { placement,
                new CompanyDraftPlacement { InternshipId = "demo-omar", StudentId = "dev-omar-student", StudentName = "Omar Khalil", StudentNumber = "DEMO-2102", PolicyId = placement.PolicyId, PolicyVersion = placement.PolicyVersion },
                new CompanyDraftPlacement { InternshipId = "demo-noor", StudentId = "dev-noor-student", StudentName = "Noor Saleh", StudentNumber = "DEMO-2103", PolicyId = placement.PolicyId, PolicyVersion = placement.PolicyVersion } };
            var policy = new EvaluationPolicy { PolicyId = placement.PolicyId, Version = placement.PolicyVersion, IsApproved = true, CompanyCommentsRequired = true,
                CompanyCriteria = new Dictionary<string, CriterionPolicy> {
                    { "practical", new CriterionPolicy { Label = "Practical work (test policy only)", Minimum = -2, Maximum = 11, Required = true } },
                    { "communication", new CriterionPolicy { Label = "Communication (test policy only)", Minimum = 1, Maximum = 6, Required = true } } } };
            // This branch can instantiate only an in-memory repository, never the Firebase repository.
            var submitted = new CompanyFinalEvaluation { InternshipId = "demo-omar", StudentId = "dev-omar-student", EvaluatorId = "dev-company-evaluator",
                PolicyId = policy.PolicyId, PolicyVersion = policy.Version, Status = EvaluationStatus.Submitted, SubmittedAtUtc = DateTimeOffset.UtcNow,
                Feedback = "Fictional submitted evaluation for Omar.", Scores = new Dictionary<string, decimal?> { { "practical", 7 }, { "communication", 4 } } };
            var draft = new CompanyFinalEvaluation { InternshipId = placement.InternshipId, StudentId = placement.StudentId, EvaluatorId = "dev-company-evaluator",
                PolicyId = policy.PolicyId, PolicyVersion = policy.Version, Feedback = "", Scores = new Dictionary<string, decimal?>() };
            var result = new CompanyDraftPageContext(new InMemoryFinalEvaluationRepository(new[] { policy }, new[] { draft, submitted }),
                new DevelopmentUser(), new DevelopmentAssignment(placements), placements, new DevelopmentSubmissionAccess(placements));
            result.MarkDevelopment(); context.Session[key] = result;
            return result;
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
