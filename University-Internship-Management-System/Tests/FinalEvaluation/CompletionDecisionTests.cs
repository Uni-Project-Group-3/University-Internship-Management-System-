using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class DecisionGuard : ICompletionDecisionGuard
    {
        internal bool Current = true, MissingLease;
        internal int Acquisitions;
        internal Action OnCheck;
        internal Action<CompletionReadiness> InspectEvidence;
        public Task<ICompletionDecisionLease> AcquireAsync(string id) { Acquisitions++; return Task.FromResult<ICompletionDecisionLease>(MissingLease ? null : new Lease(this)); }
        private sealed class Lease : ICompletionDecisionLease
        {
            private readonly DecisionGuard guard;
            internal Lease(DecisionGuard guard) { this.guard = guard; }
            public Task<bool> IsCurrentAsync(CompletionReadiness evidence) { guard.OnCheck?.Invoke(); guard.InspectEvidence?.Invoke(evidence); return Task.FromResult(guard.Current); }
            public void Dispose() { }
        }
    }
    private sealed class DecisionStore : ICompletionDecisionRepository
    {
        internal readonly ICompletionDecisionRepository Inner;
        internal int Writes, Reads;
        internal bool FailWrite, LoseAcknowledgement;
        internal Func<CompletionDecisionWrite, string, Task<CompletionReview>> Save;
        internal Func<string, Task<RecordSnapshot<CompletionReview>>> Read;
        internal DecisionStore(ICompletionDecisionRepository inner) { Inner = inner; }
        public Task<RecordSnapshot<CompletionReview>> GetCompletionReviewSnapshotAsync(string id) { Reads++; return Read == null ? Inner.GetCompletionReviewSnapshotAsync(id) : Read(id); }
        public async Task<CompletionReview> ApplyCompletionDecisionAsync(CompletionDecisionWrite write, string version)
        {
            Interlocked.Increment(ref Writes);
            if (FailWrite) throw new RepositoryException("Simulated decision write failure.");
            var saved = Save == null ? await Inner.ApplyCompletionDecisionAsync(write, version) : await Save(write, version);
            if (LoseAcknowledgement) { LoseAcknowledgement = false; throw new RepositoryException("Simulated lost acknowledgment."); }
            return saved;
        }
    }
    private static EvaluationPolicy DecisionPolicy(bool reject = false)
    {
        var policy = AcademicSubmissionPolicy(); policy.PassingThreshold = 70;
        policy.CompletionDecisions = new CompletionDecisionPolicy {
            Approve = new CompletionDecisionRule { Enabled = true }, RequestChanges = new CompletionDecisionRule { Enabled = true },
            Reject = new CompletionDecisionRule { Enabled = reject } };
        return policy;
    }
    private static CompletionDecisionWorkflow DecisionFlow(IFinalEvaluationRepository repo, ReadinessSources sources = null, DecisionGuard guard = null,
        CoordinatorAccess access = null, string uid = "fictional-coordinator", ICompletionDecisionRepository store = null)
    {
        sources = sources ?? new ReadinessSources(); access = access ?? new CoordinatorAccess();
        return new CompletionDecisionWorkflow(repo, new User { UserId = uid }, access, ReadinessFlow(repo, sources, access, uid), guard ?? new DecisionGuard(), store);
    }
    private static CompletionDecisionRequest DecisionRequest(CompletionDecisionKind kind = CompletionDecisionKind.Approve, string version = "null_etag", string reason = "")
    { return new CompletionDecisionRequest { InternshipId = "fictional-internship", ExpectedStudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1", ExpectedVersion = version, DecisionId = Guid.NewGuid().ToString("N"), Kind = kind, Reason = reason, Confirmed = true }; }
    private static async Task RunDecisionTests()
    {
        await Test("decision conflict retry rejects inconsistent current state and audit instead of acknowledging success", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var store = new DecisionStore(repo);
            store.Save = async (write, version) => {
                var saved = await repo.ApplyCompletionDecisionAsync(write, version);
                saved.Status = CompletionStatus.ChangesRequested; // Simulate an invalid read acknowledgment, not a stored mutation.
                store.Read = id => Task.FromResult(new RecordSnapshot<CompletionReview>(saved, "invalid-acknowledgment-version"));
                throw new ConflictException();
            };
            await Reject<ArgumentException>(() => DecisionFlow(repo, store: store).DecideAsync(DecisionRequest()));
            Assert(store.Writes == 1 && (await repo.GetCompletionReviewAsync("fictional-internship")).DecisionHistory.Count == 1);
        });
        await Test("decision approval persists Coordinator timestamp policy evidence and one audit event without a PDF", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var before = DateTimeOffset.UtcNow; var request = DecisionRequest();
            var result = await DecisionFlow(repo).DecideAsync(request); var record = await repo.GetCompletionReviewAsync("fictional-internship");
            Assert(result.Review.Status == CompletionStatus.Approved && record.ReviewerId == "fictional-coordinator" && record.PolicyId == "fictional-policy" && record.PolicyVersion == "test-v1");
            Assert(record.ReviewedAtUtc >= before && record.ReviewedAtUtc == record.UpdatedAtUtc && record.DecisionHistory.Count == 1 && record.DecisionHistory[0].DecidedAtUtc == record.ReviewedAtUtc);
            Assert(record.DecisionHistory[0].DecisionId == request.DecisionId && record.DecisionHistory[0].ApprovedHours == 10 && record.DecisionHistory[0].EvidenceDigest.Length == 64);
            Assert(await repo.GetCompletionDocumentAsync("fictional-internship", "any-document") == null);
        });
        await Test("decision approval blocks missing Company evaluation", async () => { var repo = ReadinessRepo(DecisionPolicy(), missingCompany: true); await Reject<ArgumentException>(() => DecisionFlow(repo).DecideAsync(DecisionRequest())); Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null); });
        await Test("decision approval blocks missing Academic evaluation", async () => { await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy(), missingAcademic: true)).DecideAsync(DecisionRequest())); });
        await Test("decision approval blocks Draft evaluations", async () => { await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy(), draft: true)).DecideAsync(DecisionRequest())); });
        await Test("decision rechecks latest hours and excludes unapproved hours after an earlier ready view", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var source = new ReadinessSources(); var flow = DecisionFlow(repo, source);
            Assert((await flow.LoadAsync("fictional-internship")).Readiness.IsReady); source.Entries[0].Hours = 9;
            await Reject<ArgumentException>(() => flow.DecideAsync(DecisionRequest())); Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null);
        });
        await Test("decision approval blocks missing required tasks", async () => { await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy()), new ReadinessSources { MissingTasks = true }).DecideAsync(DecisionRequest())); });
        await Test("decision approval blocks missing required reports", async () => { await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy()), new ReadinessSources { MissingReports = true }).DecideAsync(DecisionRequest())); });
        await Test("decision approval blocks incomplete tasks and reports", async () => { var source = new ReadinessSources(); source.Tasks.CompletedIds = new string[0]; source.Reports.CompletedIds = new string[0]; await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy()), source).DecideAsync(DecisionRequest())); });
        await Test("decision missing unapproved and unspecified completion policies fail closed", async () => {
            await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(missingPolicy: true)).DecideAsync(DecisionRequest()));
            var p = DecisionPolicy(); p.IsApproved = false; await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(p)).DecideAsync(DecisionRequest()));
            await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(AcademicSubmissionPolicy())).DecideAsync(DecisionRequest()));
        });
        await Test("decision non-Coordinator student anonymous and scope denial happen before reads or lease acquisition", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var source = new ReadinessSources(); var guard = new DecisionGuard(); var store = new DecisionStore(repo);
            await Reject<UnauthorizedAccessException>(() => DecisionFlow(repo, source, guard, new CoordinatorAccess { Role = false }, "fictional-student", store).DecideAsync(DecisionRequest()));
            await Reject<UnauthorizedAccessException>(() => DecisionFlow(repo, source, guard, new CoordinatorAccess { Scope = false }, store: store).DecideAsync(DecisionRequest()));
            await Reject<UnauthorizedAccessException>(() => DecisionFlow(repo, source, guard, uid: null, store: store).DecideAsync(DecisionRequest()));
            Assert(source.Reads == 0 && guard.Acquisitions == 0 && store.Reads == 0 && store.Writes == 0);
        });
        await Test("decision requires explicit confirmation and protects input snapshot from caller mutation", async () => {
            var request = DecisionRequest(); request.Confirmed = false; await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy())).DecideAsync(request));
            var repo = ReadinessRepo(DecisionPolicy()); request = DecisionRequest(); var guard = new DecisionGuard { OnCheck = () => { request.Reason = "caller mutation"; request.Kind = CompletionDecisionKind.Reject; } };
            var saved = await DecisionFlow(repo, guard: guard).DecideAsync(request); Assert(saved.Review.Status == CompletionStatus.Approved && saved.Review.Feedback == "");
        });
        await Test("decision approval validates policy-required reason and supporting notes", async () => {
            var p = DecisionPolicy(); p.CompletionDecisions.Approve.RequireReason = true; p.CompletionDecisions.Approve.RequireSupportingNotes = true;
            var repo = ReadinessRepo(p); var flow = DecisionFlow(repo); var request = DecisionRequest(); await Reject<ArgumentException>(() => flow.DecideAsync(request));
            request.Reason = "Verified all requirements"; await Reject<ArgumentException>(() => flow.DecideAsync(request)); request.SupportingNotes = "Authoritative sources checked";
            Assert((await flow.DecideAsync(request)).Review.Status == CompletionStatus.Approved);
        });
        await Test("decision Request Changes always requires a clear reason", async () => { await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy())).DecideAsync(DecisionRequest(CompletionDecisionKind.RequestChanges, reason: "  "))); });
        await Test("decision Request Changes records actual blockers without unlocking evaluations or changing hours", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var source = new ReadinessSources(); source.Entries[0].Hours = 2;
            var before = await repo.GetPercentageAsync("fictional-internship"); var saved = await DecisionFlow(repo, source).DecideAsync(DecisionRequest(CompletionDecisionKind.RequestChanges, reason: "Complete approved hours"));
            Assert(saved.Review.Status == CompletionStatus.ChangesRequested && saved.Review.DecisionHistory.Single().ReadinessBlockers.Any(x => x.Contains("insufficient")));
            Assert((await repo.GetPercentageAsync("fictional-internship")).Version == before.Version && (await repo.GetPercentageAsync("fictional-internship")).Value.Current.SubmittedAtUtc.HasValue && source.Entries[0].Hours == 2);
        });
        await Test("decision Reject is denied unless explicitly enabled by approved policy", async () => {
            await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy())).DecideAsync(DecisionRequest(CompletionDecisionKind.Reject, reason: "Policy reason")));
            var p = DecisionPolicy(); p.CompletionDecisions.Reject = null;
            await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(p)).DecideAsync(DecisionRequest(CompletionDecisionKind.Reject, reason: "Policy reason")));
        });
        await Test("decision permitted rejection requires a reason and stays distinct from Request Changes", async () => {
            var repo = ReadinessRepo(DecisionPolicy(true)); var flow = DecisionFlow(repo); var request = DecisionRequest(CompletionDecisionKind.Reject);
            await Reject<ArgumentException>(() => flow.DecideAsync(request)); request.Reason = "Explicit fictional policy rejection reason";
            var result = await flow.DecideAsync(request); Assert(result.Review.Status == CompletionStatus.Rejected && result.Review.DecisionHistory.Single().Kind == CompletionDecisionKind.Reject);
            var version = (await repo.GetCompletionReviewSnapshotAsync("fictional-internship")).Version;
            await Reject<ConflictException>(() => flow.DecideAsync(DecisionRequest(version: version)));
        });
        await Test("decision Changes Requested history appends exactly and later approval preserves creation time", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var flow = DecisionFlow(repo); var a = await flow.DecideAsync(DecisionRequest(CompletionDecisionKind.RequestChanges, reason: "First request"));
            var version = (await repo.GetCompletionReviewSnapshotAsync("fictional-internship")).Version;
            var b = await flow.DecideAsync(DecisionRequest(CompletionDecisionKind.RequestChanges, version, "Second request"));
            version = (await repo.GetCompletionReviewSnapshotAsync("fictional-internship")).Version;
            var c = await flow.DecideAsync(DecisionRequest(version: version));
            Assert(c.Review.DecisionHistory.Count == 3 && c.Review.DecisionHistory[0].Reason == "First request" && c.Review.DecisionHistory[1].Reason == "Second request" && c.Review.CreatedAtUtc == a.Review.CreatedAtUtc);
            Assert(c.Review.DecisionHistory[2].DecidedAtUtc >= c.Review.DecisionHistory[1].DecidedAtUtc && c.Review.Status == CompletionStatus.Approved);
            Assert(c.Review.DecisionHistory[0].PreviousStatus == null && c.Review.DecisionHistory[1].PreviousStatus == CompletionStatus.ChangesRequested && c.Review.DecisionHistory[2].PreviousStatus == CompletionStatus.ChangesRequested);
            c.Review.Status = CompletionStatus.ChangesRequested;
            var invalid = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }, new[] { SubmittedCompanyFixture() }, null, new[] { c.Review });
            await Reject<ArgumentException>(() => DecisionFlow(invalid).LoadAsync("fictional-internship"));
        });
        await Test("decision stale revision cannot replace a newer current review", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var flow = DecisionFlow(repo);
            await flow.DecideAsync(DecisionRequest(CompletionDecisionKind.RequestChanges, reason: "Newer review"));
            await Reject<ConflictException>(() => flow.DecideAsync(DecisionRequest())); Assert((await repo.GetCompletionReviewAsync("fictional-internship")).DecisionHistory.Count == 1);
        });
        await Test("decision concurrent different attempts have exactly one conditional-write winner", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var store = new DecisionStore(repo); int arrived = 0; var gate = new TaskCompletionSource<bool>();
            store.Save = async (write, version) => { if (Interlocked.Increment(ref arrived) == 2) gate.TrySetResult(true); await gate.Task; return await repo.ApplyCompletionDecisionAsync(write, version); };
            var flow = DecisionFlow(repo, store: store); Func<Task<bool>> attempt = async () => { try { await flow.DecideAsync(DecisionRequest()); return true; } catch (ConflictException) { return false; } };
            var results = await Task.WhenAll(attempt(), attempt()); Assert(results.Count(x => x) == 1 && (await repo.GetCompletionReviewAsync("fictional-internship")).DecisionHistory.Count == 1);
        });
        await Test("decision concurrent identical retries acknowledge one persisted audit event", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var store = new DecisionStore(repo); int arrived = 0; var gate = new TaskCompletionSource<bool>();
            store.Save = async (write, version) => { if (Interlocked.Increment(ref arrived) == 2) gate.TrySetResult(true); await gate.Task; return await repo.ApplyCompletionDecisionAsync(write, version); };
            var flow = DecisionFlow(repo, store: store); var request = DecisionRequest(); var results = await Task.WhenAll(flow.DecideAsync(request), flow.DecideAsync(request));
            Assert(results.Count(x => x.AlreadyRecorded) == 1 && (await repo.GetCompletionReviewAsync("fictional-internship")).DecisionHistory.Count == 1);
        });
        await Test("decision duplicate approval with new ID and all overwrites of Approved are blocked", async () => {
            var repo = ReadinessRepo(DecisionPolicy(true)); var flow = DecisionFlow(repo); await flow.DecideAsync(DecisionRequest());
            var before = await repo.GetCompletionReviewSnapshotAsync("fictional-internship");
            foreach (var kind in new[] { CompletionDecisionKind.Approve, CompletionDecisionKind.RequestChanges, CompletionDecisionKind.Reject }) await Reject<ConflictException>(() => flow.DecideAsync(DecisionRequest(kind, before.Version, "Attempt overwrite")));
            Assert((await repo.GetCompletionReviewSnapshotAsync("fictional-internship")).Version == before.Version);
        });
        await Test("decision exact retry returns saved acknowledgment without another write or history event", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var store = new DecisionStore(repo); var flow = DecisionFlow(repo, store: store); var request = DecisionRequest();
            await flow.DecideAsync(request); var before = await repo.GetCompletionReviewSnapshotAsync("fictional-internship"); var result = await flow.DecideAsync(request);
            Assert(result.AlreadyRecorded && store.Writes == 1 && result.Review.DecisionHistory.Count == 1 && (await repo.GetCompletionReviewSnapshotAsync("fictional-internship")).Version == before.Version);
        });
        await Test("decision reused idempotency ID with different payload is a conflict", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var flow = DecisionFlow(repo); var request = DecisionRequest(); await flow.DecideAsync(request); request.Reason = "Different request";
            await Reject<ConflictException>(() => flow.DecideAsync(request));
        });
        await Test("decision failed persistence throws without returning success or creating history", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var store = new DecisionStore(repo) { FailWrite = true };
            await Reject<RepositoryException>(() => DecisionFlow(repo, store: store).DecideAsync(DecisionRequest())); Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null);
        });
        await Test("decision lost acknowledgment retry confirms actual saved history without a second write", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var store = new DecisionStore(repo) { LoseAcknowledgement = true }; var flow = DecisionFlow(repo, store: store); var request = DecisionRequest();
            await Reject<RepositoryException>(() => flow.DecideAsync(request)); var retried = await flow.DecideAsync(request); Assert(retried.AlreadyRecorded && store.Writes == 1 && retried.Review.DecisionHistory.Count == 1);
        });
        await Test("decision missing or stale authoritative guard blocks writes", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var noGuard = new CompletionDecisionWorkflow(repo, new User(), new CoordinatorAccess(), ReadinessFlow(repo), null);
            Assert(!noGuard.IsConfigured); await Reject<InvalidOperationException>(() => noGuard.DecideAsync(DecisionRequest()));
            await Reject<InvalidOperationException>(() => DecisionFlow(repo, guard: new DecisionGuard { MissingLease = true }).DecideAsync(DecisionRequest()));
            await Reject<ConflictException>(() => DecisionFlow(repo, guard: new DecisionGuard { Current = false }).DecideAsync(DecisionRequest()));
            Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null);
        });
        await Test("decision rechecks role immediately before persistence", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var access = new CoordinatorAccess(); var guard = new DecisionGuard { OnCheck = () => access.Role = false };
            await Reject<UnauthorizedAccessException>(() => DecisionFlow(repo, guard: guard, access: access).DecideAsync(DecisionRequest())); Assert(await repo.GetCompletionReviewAsync("fictional-internship") == null);
        });
        await Test("decision missing untrusted or stale requirement data never permits approval", async () => {
            foreach (var source in new[] { new ReadinessSources { Trusted = false }, new ReadinessSources { DataVersion = "stale" }, new ReadinessSources { MissingHours = true } }) await Reject<ArgumentException>(() => DecisionFlow(ReadinessRepo(DecisionPolicy()), source).DecideAsync(DecisionRequest()));
        });
        await Test("decision policy reference tampering and oversized notes are rejected", async () => {
            var flow = DecisionFlow(ReadinessRepo(DecisionPolicy())); var request = DecisionRequest(); request.PolicyVersion = "other"; await Reject<ConflictException>(() => flow.DecideAsync(request));
            request = DecisionRequest(); request.ExpectedStudentId = "changed-student"; await Reject<ConflictException>(() => flow.DecideAsync(request));
            request = DecisionRequest(); request.SupportingNotes = new string('x', 1001); await Reject<ArgumentException>(() => flow.DecideAsync(request));
        });
        await Test("decision freshness adapter cannot mutate the evidence persisted by the workflow", async () => {
            var repo = ReadinessRepo(DecisionPolicy()); var guard = new DecisionGuard { InspectEvidence = evidence => { evidence.Placement.StudentId = "adapter-mutation"; evidence.Evaluations.PercentageEvaluation.Current.Comments = "adapter-mutation"; evidence.ApplicablePolicy.Version = "adapter-mutation"; } };
            var saved = await DecisionFlow(repo, guard: guard).DecideAsync(DecisionRequest()); Assert(saved.Review.StudentId == "fictional-student" && saved.Review.PolicyVersion == "test-v1");
        });
        await Test("decision historical Approved record is protected without inventing missing history", async () => {
            var baseRepo = ReadinessRepo(DecisionPolicy()); var academic = (await baseRepo.GetAcademicAsync("fictional-internship")).Value;
            var review = new CompletionReview { InternshipId = "fictional-internship", StudentId = "fictional-student", ReviewerId = "historic-coordinator", PolicyId = "fictional-policy", PolicyVersion = "test-v1", Status = CompletionStatus.Approved };
            var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }, new[] { SubmittedCompanyFixture() }, null, new[] { review });
            var version = (await repo.GetCompletionReviewSnapshotAsync("fictional-internship")).Version; await Reject<ConflictException>(() => DecisionFlow(repo).DecideAsync(DecisionRequest(version: version)));
            Assert((await repo.GetCompletionReviewAsync("fictional-internship")).DecisionHistory.Count == 0);
        });
        await Test("decision Firebase conditional write stores current state and audit atomically with server timestamps using mocked HTTP", async () => {
            string savedJson = "null", version = "null_etag"; int puts = 0; var academic = CompleteAcademicDraft(); academic.Status = EvaluationStatus.Submitted; academic.SubmittedAtUtc = DateTimeOffset.UtcNow;
            var handler = new Handler { Respond = request => {
                var path = request.RequestUri.AbsolutePath;
                if (request.Method == HttpMethod.Get) {
                    if (path.StartsWith("/completionReviews/")) return Response(savedJson, version);
                    return Response(JsonConvert.SerializeObject(path.StartsWith("/evaluationPolicies/") ? (object)DecisionPolicy() : path.StartsWith("/finalEvaluationReviewsV2/") ? (object)ModernValue() : null));
                }
                Assert(request.Method == HttpMethod.Put && path == "/completionReviews/fictional-internship.json" && string.Join("", request.Headers.GetValues("if-match")) == version);
                savedJson = request.Content.ReadAsStringAsync().GetAwaiter().GetResult(); var body = JObject.Parse(savedJson);
                Assert((string)body["status"] == "Approved" && body["decisionHistory"].Count() == 1 && (string)body["reviewerId"] == "fictional-coordinator" && DateTimeOffset.Parse((string)body["reviewedAtUtc"]) > DateTimeOffset.UtcNow.AddMinutes(-1));
                puts++; version = "\"saved-review\""; return Response("null");
            } };
            var repo = Rest(handler); var flow = DecisionFlow(repo); var input = DecisionRequest(); await flow.DecideAsync(input); var retry = await flow.DecideAsync(input);
            Assert(retry.AlreadyRecorded && puts == 1);
        });
        await Test("decision Firebase 412 and HTTP failure never acknowledge approval", async () => {
            var academic = CompleteAcademicDraft(); academic.Status = EvaluationStatus.Submitted; academic.SubmittedAtUtc = DateTimeOffset.UtcNow;
            HttpStatusCode status = HttpStatusCode.PreconditionFailed;
            var handler = new Handler { Respond = request => {
                if (request.Method == HttpMethod.Put) return new HttpResponseMessage(status);
                string path = request.RequestUri.AbsolutePath;
                if (path.StartsWith("/completionReviews/")) return Response("null", "null_etag");
                return Response(JsonConvert.SerializeObject(path.StartsWith("/evaluationPolicies/") ? (object)DecisionPolicy() : path.StartsWith("/finalEvaluationReviewsV2/") ? (object)ModernValue() : null));
            } };
            await Reject<ConflictException>(() => DecisionFlow(Rest(handler)).DecideAsync(DecisionRequest())); status = HttpStatusCode.ServiceUnavailable;
            await Reject<RepositoryException>(() => DecisionFlow(Rest(handler)).DecideAsync(DecisionRequest()));
        });
    }
}
