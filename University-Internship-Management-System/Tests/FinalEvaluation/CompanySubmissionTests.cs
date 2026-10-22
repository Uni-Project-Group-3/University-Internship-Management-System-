using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class SubmissionAccess : ICompanyFinalEvaluationAccess
    {
        public bool CompanyRole = true, CompanyAssigned = true, Eligible = true, AcademicRole, AcademicAssigned;
        public Task<bool> IsCompanySupervisorAsync(string id) { return Task.FromResult(CompanyRole); }
        public Task<bool> IsAssignedCompanySupervisorAsync(string uid, string id, string student) { return Task.FromResult(CompanyAssigned); }
        public Task<bool> IsEligibleAsync(string id) { return Task.FromResult(Eligible); }
        public Task<bool> IsAcademicSupervisorAsync(string id) { return Task.FromResult(AcademicRole); }
        public Task<bool> IsAssignedAcademicSupervisorAsync(string uid, string id) { return Task.FromResult(AcademicAssigned); }
    }
    private static EvaluationPolicy ApprovedPolicy()
    {
        var p = Policy(); p.IsApproved = true; p.CompanyCommentsRequired = true; return p;
    }
    private static CompanyFinalEvaluation CompleteDraft()
    {
        var draft = Draft(); draft.Feedback = "fictional final comments"; return draft;
    }
    private static InMemoryFinalEvaluationRepository SubmissionRepo(EvaluationPolicy policy = null)
    { return new InMemoryFinalEvaluationRepository(new[] { policy ?? ApprovedPolicy() }, new[] { CompleteDraft() }); }
    private static CompanySubmissionService Submission(IFinalEvaluationRepository repository, SubmissionAccess access = null)
    { return new CompanySubmissionService(repository, new User(), access ?? new SubmissionAccess()); }

    private static async Task RunCompanySubmissionTests()
    {
        await Test("submission transitions existing Draft with server timestamp and immutable creation time", async () =>
        {
            var repo = SubmissionRepo(); var before = await repo.GetCompanyAsync("fictional-internship");
            var start = DateTimeOffset.UtcNow; await Submission(repo).SubmitAsync(before.Value, before.Version);
            var after = await repo.GetCompanyAsync("fictional-internship");
            Assert(after.Value.Status == EvaluationStatus.Submitted && after.Value.SubmittedAtUtc >= start && after.Value.SubmittedAtUtc <= DateTimeOffset.UtcNow);
            Assert(after.Value.CreatedAtUtc == before.Value.CreatedAtUtc && after.Version != before.Version && before.Value.Status == EvaluationStatus.Draft);
        });
        await Test("submission requires all policy required scores and comments", async () =>
        {
            var repo = SubmissionRepo(); var snap = await repo.GetCompanyAsync("fictional-internship");
            snap.Value.Scores.Clear(); await Reject<ArgumentException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Scores["testCriterion"] = 3; snap.Value.Feedback = " ";
            await Reject<ArgumentException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value.Status == EvaluationStatus.Draft);
        });
        await Test("submission honors optional scores and explicit optional comments", async () =>
        {
            var p = ApprovedPolicy(); p.CompanyCommentsRequired = false; p.CompanyCriteria["testCriterion"].Required = false;
            var repo = SubmissionRepo(p); var snap = await repo.GetCompanyAsync("fictional-internship"); snap.Value.Scores.Clear(); snap.Value.Feedback = "";
            await Submission(repo).SubmitAsync(snap.Value, snap.Version);
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value.Status == EvaluationStatus.Submitted);
        });
        await Test("submission rejects out of range and unknown criteria and requires configured feedback", async () =>
        {
            var p = ApprovedPolicy(); p.CompanyCriteria["testCriterion"].RequireFeedback = true;
            var repo = SubmissionRepo(p); var snap = await repo.GetCompanyAsync("fictional-internship");
            snap.Value.Scores["testCriterion"] = 8; await Reject<ArgumentException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Scores["testCriterion"] = 3; snap.Value.Scores["unknown"] = 3; await Reject<ArgumentException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.Scores.Remove("unknown"); await Reject<ArgumentException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            snap.Value.CriterionFeedback["testCriterion"] = "required feedback"; await Submission(repo).SubmitAsync(snap.Value, snap.Version);
        });
        await Test("submission rejects missing unapproved or incomplete policy configuration", async () =>
        {
            foreach (var p in new[] { Policy(), new EvaluationPolicy { PolicyId = "fictional-policy", Version = "test-v1", IsApproved = true } })
            {
                var repo = SubmissionRepo(p); var snap = await repo.GetCompanyAsync("fictional-internship");
                await Reject<ArgumentException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            }
            var missing = new InMemoryFinalEvaluationRepository(companies: new[] { CompleteDraft() });
            var current = await missing.GetCompanyAsync("fictional-internship");
            await Reject<ArgumentException>(() => Submission(missing).SubmitAsync(current.Value, current.Version));
        });
        await Test("submission denies wrong role unassigned supervisor and spoofed evaluator", async () =>
        {
            var repo = SubmissionRepo(); var snap = await repo.GetCompanyAsync("fictional-internship");
            await Reject<UnauthorizedAccessException>(() => Submission(repo, new SubmissionAccess { CompanyRole = false }).SubmitAsync(snap.Value, snap.Version));
            await Reject<UnauthorizedAccessException>(() => Submission(repo, new SubmissionAccess { CompanyAssigned = false }).SubmitAsync(snap.Value, snap.Version));
            snap.Value.EvaluatorId = "browser-spoof";
            await Reject<UnauthorizedAccessException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
        });
        await Test("submission fails closed without adapters and without eligibility", async () =>
        {
            var repo = SubmissionRepo(); var snap = await repo.GetCompanyAsync("fictional-internship");
            await Reject<UnauthorizedAccessException>(() => new CompanySubmissionService(repo, new User(), null).SubmitAsync(snap.Value, snap.Version));
            await Reject<ArgumentException>(() => Submission(repo, new SubmissionAccess { Eligible = false }).SubmitAsync(snap.Value, snap.Version));
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value.Status == EvaluationStatus.Draft);
        });
        await Test("submission requires a saved Draft and prevents duplicate submission and later edits", async () =>
        {
            var empty = new InMemoryFinalEvaluationRepository(new[] { ApprovedPolicy() });
            await Reject<ConflictException>(() => Submission(empty).SubmitAsync(CompleteDraft(), "fictional-version"));
            var repo = SubmissionRepo(); var snap = await repo.GetCompanyAsync("fictional-internship");
            await Submission(repo).SubmitAsync(snap.Value, snap.Version);
            await Reject<ConflictException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            var locked = await repo.GetCompanyAsync("fictional-internship");
            await Reject<ConflictException>(() => repo.UpdateCompanyDraftAsync(snap.Value, locked.Version));
            await Reject<ConflictException>(() => repo.CreateCompanyDraftAsync(snap.Value));
        });
        await Test("stale submission preserves newer Draft and simultaneous submission has one winner", async () =>
        {
            var repo = SubmissionRepo(); var snap = await repo.GetCompanyAsync("fictional-internship");
            var newer = CompleteDraft(); newer.Feedback = "newer"; await repo.UpdateCompanyDraftAsync(newer, snap.Version);
            await Reject<ConflictException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            var latest = await repo.GetCompanyAsync("fictional-internship");
            var attempts = Enumerable.Range(0, 2).Select(i => Task.Run(async () => {
                try { await Submission(repo).SubmitAsync(latest.Value, latest.Version); return true; }
                catch (ConflictException) { return false; }
            })).ToArray();
            Assert((await Task.WhenAll(attempts)).Count(x => x) == 1);
        });
        await Test("assigned Academic Supervisor reads Submitted only through read-only handoff", async () =>
        {
            var repo = SubmissionRepo(); var access = new SubmissionAccess { AcademicRole = true, AcademicAssigned = true };
            var reader = new SubmittedCompanyEvaluationReader(repo, new User { UserId = "fictional-academic" }, access);
            Assert(await reader.GetSubmittedAsync("fictional-internship") == null);
            var snap = await repo.GetCompanyAsync("fictional-internship"); await Submission(repo).SubmitAsync(snap.Value, snap.Version);
            var read = await reader.GetSubmittedAsync("fictional-internship"); Assert(read.Status == EvaluationStatus.Submitted);
            read.Feedback = "local mutation"; Assert((await reader.GetSubmittedAsync("fictional-internship")).Feedback != "local mutation");
        });
        await Test("students and unassigned academics cannot read confidential company evaluations", async () =>
        {
            var repo = SubmissionRepo();
            repo.SimulateFailure = true; // Denial must occur before any privileged repository read.
            await Reject<UnauthorizedAccessException>(() => new SubmittedCompanyEvaluationReader(repo, new User { UserId = "fictional-student" }, new SubmissionAccess { AcademicRole = false, AcademicAssigned = true }).GetSubmittedAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => new SubmittedCompanyEvaluationReader(repo, new User { UserId = "fictional-academic" }, new SubmissionAccess { AcademicRole = true, AcademicAssigned = false }).GetSubmittedAsync("fictional-internship"));
        });
        await Test("submission repository failures do not return success or mutate the stored Draft", async () =>
        {
            var repo = SubmissionRepo(); var snap = await repo.GetCompanyAsync("fictional-internship");
            repo.SimulateFailure = true; await Reject<RepositoryException>(() => Submission(repo).SubmitAsync(snap.Value, snap.Version));
            repo.SimulateFailure = false;
            Assert((await repo.GetCompanyAsync("fictional-internship")).Value.Status == EvaluationStatus.Draft);
            await Reject<RepositoryException>(() => Submission(new WriteFailureRepository(repo)).SubmitAsync(snap.Value, snap.Version));
        });
        await Test("Firebase submission uses exact ETag and server-set timestamp; HTTP 412 remains a conflict", async () =>
        {
            int puts = 0; bool fail = false, unavailable = false;
            var handler = new Handler { Respond = request => {
                if (request.Method == HttpMethod.Get) return Response(JsonConvert.SerializeObject(request.RequestUri.AbsolutePath.StartsWith("/evaluationPolicies/") ? (object)ApprovedPolicy() : CompleteDraft()));
                Assert(request.Method == HttpMethod.Put && string.Join("", request.Headers.GetValues("if-match")) == "\"test-version\"");
                var json = Newtonsoft.Json.Linq.JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert((string)json["status"] == "Submitted" && json["submittedAtUtc"] != null && DateTimeOffset.Parse((string)json["submittedAtUtc"]) > DateTimeOffset.UtcNow.AddMinutes(-1));
                puts++;
                if (unavailable) return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                return fail ? new HttpResponseMessage(HttpStatusCode.PreconditionFailed) : Response("null");
            } };
            await Submission(Rest(handler)).SubmitAsync(CompleteDraft(), "\"test-version\""); Assert(puts == 1);
            fail = true; await Reject<ConflictException>(() => Submission(Rest(handler)).SubmitAsync(CompleteDraft(), "\"test-version\""));
            unavailable = true; await Reject<RepositoryException>(() => Submission(Rest(handler)).SubmitAsync(CompleteDraft(), "\"test-version\""));
        });
    }
}
