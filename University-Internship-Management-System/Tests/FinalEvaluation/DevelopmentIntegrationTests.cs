using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private static async Task RunDevelopmentIntegrationTests()
    {
        await Test("development end-to-end uses one repository from empty drafts through approval and private student PDF", async () => {
            const string id = "fictional-internship";
            var policy = DecisionPolicy(); policy.CompletionReport = new CompletionReportPolicy { Enabled = true };
            var repo = new InMemoryFinalEvaluationRepository(new[] { policy });
            var company = new CompanyDraftWorkflow(repo, new User(), new Assignment { Allowed = true }, new[] { Placement() }, new SubmissionAccess());
            Assert((await company.LoadAsync(id)).Record.Value == null);
            await Save(company, null, "", "");
            var companyDraft = await company.LoadAsync(id);
            await Save(company, companyDraft.Record.Version, "4", "Fictional company final comments");
            companyDraft = await company.LoadAsync(id);
            var staleCompany = companyDraft.Record.Version;
            await company.SubmitAsync(id, staleCompany, policy.PolicyId, policy.Version,
                new Dictionary<string, string> { { "testCriterion", "4" } }, new Dictionary<string, string>(), "Fictional company final comments");
            Assert((await company.LoadAsync(id)).IsSubmitted);
            await Reject<ConflictException>(() => Save(company, staleCompany, "5"));

            var academic = AcademicSubmitFlow(repo);
            Assert((await academic.ReviewCompanyAsync(id)).Evaluation.Scores["testCriterion"] == 4);
            Assert((await academic.LoadAsync(id)).Record.Value == null);
            await AcademicSave(academic, null, "", "");
            var academicDraft = await academic.LoadAsync(id);
            var scores = new Dictionary<string, string> { { "analysis", "4" }, { "summary", "1" } };
            await academic.SaveAsync(id, academicDraft.Record.Version, policy.PolicyId, policy.Version, scores, "Fictional academic final comments");
            academicDraft = await academic.LoadAsync(id);
            await academic.SubmitAsync(id, academicDraft.Record.Version, policy.PolicyId, policy.Version, scores, "Fictional academic final comments");
            Assert((await academic.LoadAsync(id)).IsSubmitted);
            await Reject<ConflictException>(() => AcademicSave(academic, academicDraft.Record.Version));

            var both = await CoordinatorReader(repo).GetSubmittedAsync(id);
            Assert(both.Company.Status == EvaluationStatus.Submitted && both.Academic.Status == EvaluationStatus.Submitted);
            var flow = DecisionFlow(repo);
            Assert((await flow.LoadAsync(id)).Readiness.IsReady);
            var request = DecisionRequest();
            var approval = await flow.DecideAsync(request);
            Assert(approval.Review.Status == CompletionStatus.Approved);
            var beforeReport = JsonConvert.SerializeObject(await repo.GetCompletionReviewAsync(id));
            var fixture = new ReportFixture { Repo = repo, Metadata = new ReportMetadata(repo) };
            fixture.Data.Value = new VerifiedCompletionReportData { IsTrusted = true, InternshipId = id, StudentId = "fictional-student",
                PolicyId = policy.PolicyId, PolicyVersion = policy.Version, ApprovedAtUtc = approval.Review.ReviewedAtUtc,
                StudentName = "Fictional End-to-End Student", StudentNumber = "TEST-E2E", OrganizationName = "Fictional Organization",
                PeriodStart = new DateTime(2026, 1, 1), PeriodEnd = new DateTime(2026, 2, 1), ApprovedHours = 10 };
            var sources = new StudentSources { Required = 10 };
            Assert((await StudentFlow(fixture, sources).LoadAsync(id)).Status == CompletionStatus.Approved);
            Assert(!(await StudentFlow(fixture, sources).LoadAsync(id)).ReportAvailable);
            var document = await fixture.Service().GenerateAsync(id);
            sources.DocumentId = document.DocumentId;
            Assert((await StudentFlow(fixture, sources).LoadAsync(id)).ReportAvailable);
            var download = await StudentFlow(fixture, sources).DownloadAsync(id);
            Assert(download.ContentType == "application/pdf" && CompletionReportValidation.Hash(download.Content) == document.Sha256);
            Assert(beforeReport == JsonConvert.SerializeObject(await repo.GetCompletionReviewAsync(id)));
            Assert((await repo.GetCompletionReviewAsync(id)).DecisionHistory.Count == 1);
            await Reject<UnauthorizedAccessException>(() => StudentFlow(fixture, sources, "unrelated-student").DownloadAsync(id));
        });
        await Test("privileged repository does not authorize service-account identity without trusted adapters", async () => {
            var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() });
            var serviceIdentity = new User { UserId = "fictional-service-account" };
            await Reject<UnauthorizedAccessException>(() => new CoordinatorEvaluationReader(repo, serviceIdentity, null).GetSubmittedAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => new CoordinatorReadinessWorkflow(repo, serviceIdentity, null, null, null, null, null, null).ReviewAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => new AcademicSubmissionService(repo, serviceIdentity, null, null).SubmitAsync(CompleteAcademicDraft(), "null_etag"));
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value == null && await repo.GetCompletionReviewAsync("fictional-internship") == null);
        });
    }
}
