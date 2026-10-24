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
    private sealed class PercentageAccess : IPercentageEvaluationAccess
    {
        internal bool Company = true, Academic = true, Eligible = true;
        public Task<bool> CanCompanyAsync(string uid, CompletionPlacement p) { return Task.FromResult(Company && uid == "fictional-evaluator" && p.InternshipId == "fictional-internship" && p.StudentId == "fictional-student"); }
        public Task<bool> CanAcademicAsync(string uid, CompletionPlacement p) { return Task.FromResult(Academic && uid == "fictional-academic" && p.InternshipId == "fictional-internship" && p.StudentId == "fictional-student"); }
        public Task<bool> IsEligibleAsync(CompletionPlacement p) { return Task.FromResult(Eligible); }
    }
    private static PercentageEvaluation ModernValue(bool submitted = true, bool approved = true)
    {
        var version = new CompanyPercentageVersion { VersionId = Guid.NewGuid().ToString("N"), Number = 1, Percentage = 80, Comments = "Fictional company comments", CompanySupervisorId = "fictional-evaluator", UpdatedAtUtc = DateTimeOffset.UtcNow };
        if (submitted) version.SubmittedAtUtc = version.UpdatedAtUtc;
        var value = new PercentageEvaluation { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1", Current = version };
        if (submitted) { value.Submissions.Add(PercentageEvaluationWorkflow.Clone(version)); if (approved) value.Reviews.Add(new AcademicReviewDecision { DecisionId = Guid.NewGuid().ToString("N"), CompanyVersionId = version.VersionId, Kind = AcademicReviewKind.Approve, AcademicSupervisorId = "fictional-academic", DecidedAtUtc = DateTimeOffset.UtcNow }); }
        return value;
    }
    private static InMemoryFinalEvaluationRepository ModernSeed(EvaluationPolicy policy, bool missingPolicy = false, bool missingCompany = false, bool missingAcademic = false, bool draft = false)
    {
        var repo = new InMemoryFinalEvaluationRepository(missingPolicy ? null : new[] { policy });
        if (missingCompany) return repo;
        var value = ModernValue(!draft, !missingAcademic); var initial = PercentageEvaluationWorkflow.Clone(value);
        initial.Current.SubmittedAtUtc = null; initial.Submissions.Clear(); initial.Reviews.Clear();
        repo.SavePercentageAsync(initial, "null_etag").GetAwaiter().GetResult();
        if (!draft) {
            var submitted = PercentageEvaluationWorkflow.Clone(value); submitted.Reviews.Clear();
            repo.SavePercentageAsync(submitted, repo.GetPercentageAsync(value.InternshipId).GetAwaiter().GetResult().Version).GetAwaiter().GetResult();
            if (value.Reviews.Count > 0) repo.SavePercentageAsync(value, repo.GetPercentageAsync(value.InternshipId).GetAwaiter().GetResult().Version).GetAwaiter().GetResult();
        }
        return repo;
    }
    private static PercentageEvaluationWorkflow PercentageFlow(IFinalEvaluationRepository repo, bool academic = false, PercentageAccess access = null, string uid = null, DecisionGuard guard = null)
    { return new PercentageEvaluationWorkflow(repo, new User { UserId = uid ?? (academic ? "fictional-academic" : "fictional-evaluator") }, access ?? new PercentageAccess(), new ReadinessSources(), guard ?? new DecisionGuard()); }
    private static Task SavePercentage(PercentageEvaluationWorkflow flow, string version, decimal? score = 80, string comments = "Fictional final comments", bool revision = false)
    { return flow.SaveDraftAsync("fictional-internship", version, score, comments, revision); }
    private static async Task ReviewPercentage(InMemoryFinalEvaluationRepository repo, AcademicReviewKind kind = AcademicReviewKind.Approve, string reason = "")
    { var s = await repo.GetPercentageAsync("fictional-internship"); await PercentageFlow(repo, true).ReviewAsync(s.Value.InternshipId, s.Version, s.Value.Current.VersionId, kind, reason, Guid.NewGuid().ToString("N"), true); }
    private static async Task RunPercentageRequirementsTests()
    {
        await Test("evaluation statistics count only supplied authorized snapshots and current review states", () => {
            var draft = ModernValue(false, false); var submitted = ModernValue(true, false); var approved = ModernValue(true, true);
            var revision = ModernValue(true, true); revision.Reviews[0].Kind = AcademicReviewKind.RequestRevision;
            var stats = EvaluationPageStatistics.From(new[] { null, draft, submitted, approved, revision });
            Assert(stats.Assigned == 5 && stats.Drafts == 1 && stats.AwaitingReview == 1 && stats.Approved == 1);
            var empty = EvaluationPageStatistics.From(new PercentageEvaluation[0]);
            Assert(empty.Assigned == 0 && empty.Drafts == 0 && empty.Approved == 0);
            return Task.CompletedTask;
        });
        await Test("Company statistic bindings display verified counts and restore unknown on unavailable data", () => {
            var page = new University_Internship_Management_System.Company.ReportsFinalEvaluation();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var bind = typeof(PercentageEvaluationPage).GetMethod("BindStatistics", flags);
            bind.Invoke(page, new object[] { (int?)4, (int?)2, (int?)1, (int?)1 });
            var property = page.GetType().GetProperty("AssignedTraineesCount", flags);
            Assert((string)property.GetValue(page) == "4");
            bind.Invoke(page, new object[] { null, null, null, null });
            Assert((string)property.GetValue(page) == "—");
            return Task.CompletedTask;
        });
        await Test("access denial clears Academic percentage reason timestamp and mutation controls", () => {
            var page = new University_Internship_Management_System.Academic.FinalAcademicEvaluation();
            foreach (var id in new[] { "FinalPercentage", "FinalComments", "RevisionReason" }) page.Controls.Add(new System.Web.UI.WebControls.TextBox { ID = id, Text = "PRIVATE_VALUE" });
            foreach (var id in new[] { "SelectedName", "FinalStatus", "SubmissionTime", "ReviewStatus", "FinalOutcome", "OperationMessage", "InformationText", "InformationTitle", "CompanyPercentageDisplay", "ReviewTimestamp", "RecordedReviewReason", "EvaluationError" }) page.Controls.Add(new System.Web.UI.WebControls.Literal { ID = id, Text = "PRIVATE_VALUE" });
            foreach (var id in new[] { "selectedInternship", "evaluationTicket", "activePanel", "reviewChoice" }) page.Controls.Add(new System.Web.UI.WebControls.HiddenField { ID = id, Value = "PRIVATE_VALUE" });
            foreach (var id in new[] { "SaveDraft", "ConfirmSubmission", "ApproveReview", "RequestRevision" }) page.Controls.Add(new System.Web.UI.WebControls.Button { ID = id, Enabled = true });
            page.Controls.Add(new System.Web.UI.WebControls.CheckBox { ID = "ConfirmAction", Checked = true, Enabled = true });
            page.Controls.Add(new System.Web.UI.WebControls.Panel { ID = "AcademicReviewPanel", Visible = true });
            typeof(PercentageEvaluationPage).GetMethod("ClearEditor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(page, null);
            Assert(page.Controls.OfType<System.Web.UI.WebControls.Literal>().All(field => field.Text == ""));
            Assert(page.Controls.OfType<System.Web.UI.WebControls.HiddenField>().All(field => field.Value == ""));
            Assert(page.Controls.OfType<System.Web.UI.WebControls.TextBox>().All(field => field.Text == "" && field.ReadOnly));
            Assert(page.Controls.OfType<System.Web.UI.WebControls.Button>().All(field => !field.Enabled));
            Assert(!((System.Web.UI.WebControls.CheckBox)page.FindControl("ConfirmAction")).Checked);
            return Task.CompletedTask;
        });

        foreach (var score in new decimal?[] { null, 0, 0.25m, 100 })
            await Test("percentage Draft accepts " + (score?.ToString() ?? "unanswered"), async () => { var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }); await SavePercentage(PercentageFlow(repo), "null_etag", score, ""); Assert((await repo.GetPercentageAsync("fictional-internship")).Value.Current.Percentage == score); });
        foreach (var score in new[] { -0.01m, 100.01m })
            await Test("percentage rejects out-of-range " + score, async () => { var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }); await Reject<ArgumentException>(() => SavePercentage(PercentageFlow(repo), "null_etag", score)); Assert((await repo.GetPercentageAsync("fictional-internship")).Value == null); });
        await Test("percentage loads missing and updates Draft across workflow instances with detached snapshots", async () => {
            var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }); Assert((await PercentageFlow(repo).LoadAsync("fictional-internship")).Value == null);
            await SavePercentage(PercentageFlow(repo), "null_etag", 40); var first = await repo.GetPercentageAsync("fictional-internship"); first.Value.Current.Comments = "caller mutation";
            Assert((await repo.GetPercentageAsync("fictional-internship")).Value.Current.Comments != "caller mutation");
            await SavePercentage(PercentageFlow(repo), first.Version, 90); Assert((await repo.GetPercentageAsync("fictional-internship")).Value.Current.Percentage == 90);
        });
        await Test("percentage stale Draft update cannot overwrite newer save", async () => { var repo = ModernSeed(DecisionPolicy(), draft:true); var s = await repo.GetPercentageAsync("fictional-internship"); await SavePercentage(PercentageFlow(repo), s.Version); await Reject<ConflictException>(() => SavePercentage(PercentageFlow(repo), s.Version, 99)); });
        await Test("percentage requires saved Draft and explicit submission confirmation", async () => {
            var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }); await Reject<ConflictException>(() => PercentageFlow(repo).SubmitAsync("fictional-internship", "null_etag", true));
            await SavePercentage(PercentageFlow(repo), "null_etag"); var s = await repo.GetPercentageAsync("fictional-internship"); await Reject<ArgumentException>(() => PercentageFlow(repo).SubmitAsync("fictional-internship", s.Version, false));
        });
        foreach (var score in new decimal?[] { null, 80 })
            await Test("percentage submission requires percentage and comments " + score, async () => { var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }); await SavePercentage(PercentageFlow(repo), "null_etag", score, ""); var s = await repo.GetPercentageAsync("fictional-internship"); await Reject<ArgumentException>(() => PercentageFlow(repo).SubmitAsync("fictional-internship", s.Version, true)); });
        await Test("percentage submission archives exact result with server UTC and locks edits", async () => {
            var repo = ModernSeed(DecisionPolicy(), draft:true); var s = await repo.GetPercentageAsync("fictional-internship"); var before = DateTimeOffset.UtcNow;
            await PercentageFlow(repo).SubmitAsync("fictional-internship", s.Version, true); s = await repo.GetPercentageAsync("fictional-internship");
            Assert(s.Value.Current.SubmittedAtUtc >= before && s.Value.Submissions.Count == 1 && s.Value.Current.Percentage == 80);
            await Reject<ConflictException>(() => SavePercentage(PercentageFlow(repo), s.Version)); await Reject<ConflictException>(() => PercentageFlow(repo).SubmitAsync("fictional-internship", s.Version, true));
        });
        foreach (bool academic in new[] { false, true })
            await Test("percentage denies wrong role and scope before protected data " + academic, async () => { var repo = ModernSeed(DecisionPolicy()); repo.SimulateFailure = true; await Reject<UnauthorizedAccessException>(() => PercentageFlow(repo, academic, uid:"unrelated-user").LoadAsync("fictional-internship", academic)); await Reject<UnauthorizedAccessException>(() => PercentageFlow(repo, academic).LoadAsync("another-internship", academic)); });
        await Test("percentage missing trusted guard prevents Draft writes", async () => { var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }); var flow = new PercentageEvaluationWorkflow(repo, new User(), new PercentageAccess(), new ReadinessSources()); await Reject<UnauthorizedAccessException>(() => SavePercentage(flow, "null_etag")); });
        await Test("Company editability follows Not Started Draft Revision Requested Submitted Approved and finalized states", async () => {
            var policy = DecisionPolicy();
            var notStarted = new InMemoryFinalEvaluationRepository(new[] { policy });
            var state = await PercentageFlow(notStarted).GetCompanyWriteStateAsync("fictional-internship");
            Assert(state.EvaluationStatus == "Not Started" && state.CanEditDraft && !state.CanSubmit);

            var draft = ModernSeed(policy, draft: true);
            state = await PercentageFlow(draft).GetCompanyWriteStateAsync("fictional-internship");
            Assert(state.EvaluationStatus == "Draft" && state.CanEditDraft && state.CanSubmit);

            var revision = ModernSeed(policy, missingAcademic: true);
            await ReviewPercentage(revision, AcademicReviewKind.RequestRevision, "Fictional revision reason");
            state = await PercentageFlow(revision).GetCompanyWriteStateAsync("fictional-internship", true);
            Assert(state.EvaluationStatus == "Revision Requested" && state.CanEditDraft && !state.CanSubmit);
            state = await PercentageFlow(revision).GetCompanyWriteStateAsync("fictional-internship", false);
            Assert(!state.CanEditDraft && !state.CanSubmit);

            var submitted = ModernSeed(policy, missingAcademic: true);
            state = await PercentageFlow(submitted).GetCompanyWriteStateAsync("fictional-internship");
            Assert(state.EvaluationStatus == "Submitted" && !state.CanEditDraft && !state.CanSubmit);

            var approved = ModernSeed(policy);
            state = await PercentageFlow(approved).GetCompanyWriteStateAsync("fictional-internship");
            Assert(state.EvaluationStatus == "Approved" && !state.CanEditDraft && !state.CanSubmit);

            foreach (var finalStatus in new[] { CompletionStatus.Approved, CompletionStatus.Rejected })
            {
                var completed = new InMemoryFinalEvaluationRepository(new[] { policy }, reviews: new[] {
                    new CompletionReview { InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1", Status = finalStatus }
                });
                state = await PercentageFlow(completed).GetCompanyWriteStateAsync("fictional-internship");
                Assert(state.CompletionFinalized && !state.CanEditDraft && !state.CanSubmit);
                await Reject<ConflictException>(() => SavePercentage(PercentageFlow(completed), "null_etag"));
            }
        });
        await Test("Company editability denies unassigned users and test policy cannot authorize official submission", async () => {
            var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() });
            await Reject<UnauthorizedAccessException>(() => PercentageFlow(repo, uid: "other-company-supervisor").GetCompanyWriteStateAsync("fictional-internship"));
            var testPolicy = DecisionPolicy(); testPolicy.IsTestPolicy = true;
            repo = new InMemoryFinalEvaluationRepository(new[] { testPolicy });
            var state = await PercentageFlow(repo).GetCompanyWriteStateAsync("fictional-internship");
            Assert(state.CanEditDraft && !state.CanSubmit);
            await SavePercentage(PercentageFlow(repo), "null_etag", 80, "Fictional test draft");
            var saved = await repo.GetPercentageAsync("fictional-internship");
            Assert(saved.Value.Current.Percentage == 80 && !saved.Value.Current.SubmittedAtUtc.HasValue);
            await Reject<ArgumentException>(() => PercentageFlow(repo).SubmitAsync("fictional-internship", saved.Version, true));
        });
        await Test("percentage ineligible internship cannot submit or be academically approved", async () => { var repo = ModernSeed(DecisionPolicy(), draft:true); var s = await repo.GetPercentageAsync("fictional-internship"); await Reject<UnauthorizedAccessException>(() => PercentageFlow(repo, access:new PercentageAccess { Eligible=false }).SubmitAsync("fictional-internship", s.Version, true)); });
        await Test("percentage missing policy blocks Draft persistence", async () => { var repo = new InMemoryFinalEvaluationRepository(); await Reject<ArgumentException>(() => SavePercentage(PercentageFlow(repo), "null_etag")); });
        await Test("legacy records cannot be silently migrated or treated as academic approval", async () => { var repo = new InMemoryFinalEvaluationRepository(new[] { DecisionPolicy() }, new[] { SubmittedCompanyFixture() }, new[] { CompleteAcademicDraft() }); await Reject<ArgumentException>(() => PercentageFlow(repo).LoadAsync("fictional-internship")); Assert(!(await ReadinessFlow(repo).ReviewAsync("fictional-internship")).IsReady); });
        await Test("all old repository score write APIs are retired and old reads remain unchanged", async () => {
            var old = CompleteAcademicDraft(); var repo = new InMemoryFinalEvaluationRepository(academics:new[] { old });
            await Reject<NotSupportedException>(() => repo.CreateAcademicDraftAsync(old)); await Reject<NotSupportedException>(() => repo.UpdateAcademicDraftAsync(old,"old")); await Reject<NotSupportedException>(() => repo.SubmitAcademicAsync(old,"old"));
            await Reject<NotSupportedException>(() => repo.CreateCompanyDraftAsync(Draft())); await Reject<NotSupportedException>(() => AcademicSubmitService(repo).SubmitAsync(old,"old"));
            Assert((await repo.GetAcademicAsync(old.InternshipId)).Value.Scores["analysis"] == old.Scores["analysis"]);
        });
        await Test("Academic approval references current submitted version and cannot modify company result", async () => { var repo = ModernSeed(DecisionPolicy(),missingAcademic:true); var before = JsonConvert.SerializeObject((await repo.GetPercentageAsync("fictional-internship")).Value.Current); await ReviewPercentage(repo); var v=(await repo.GetPercentageAsync("fictional-internship")).Value; Assert(v.Reviews.Single().CompanyVersionId==v.Current.VersionId && JsonConvert.SerializeObject(v.Current)==before && await repo.GetCompletionReviewAsync("fictional-internship")==null); });
        await Test("Academic Request Revision requires a reason and confirmation", async () => { var repo=ModernSeed(DecisionPolicy(),missingAcademic:true); var s=await repo.GetPercentageAsync("fictional-internship"); await Reject<ArgumentException>(()=>PercentageFlow(repo,true).ReviewAsync("fictional-internship",s.Version,s.Value.Current.VersionId,AcademicReviewKind.RequestRevision," ","decision",true)); await Reject<ArgumentException>(()=>PercentageFlow(repo,true).ReviewAsync("fictional-internship",s.Version,s.Value.Current.VersionId,AcademicReviewKind.Approve,"","decision",false)); });
        await Test("Academic cannot approve Draft or superseded company version", async () => { var repo=ModernSeed(DecisionPolicy(),draft:true); await Reject<ConflictException>(()=>ReviewPercentage(repo)); repo=ModernSeed(DecisionPolicy(),missingAcademic:true); var s=await repo.GetPercentageAsync("fictional-internship"); await Reject<ConflictException>(()=>PercentageFlow(repo,true).ReviewAsync("fictional-internship",s.Version,"superseded",AcademicReviewKind.Approve,"","decision",true)); });
        await Test("Academic stale and duplicate decisions are conflicts with one audit entry", async () => { var repo=ModernSeed(DecisionPolicy(),missingAcademic:true); var s=await repo.GetPercentageAsync("fictional-internship"); await ReviewPercentage(repo); await Reject<ConflictException>(()=>PercentageFlow(repo,true).ReviewAsync("fictional-internship",s.Version,s.Value.Current.VersionId,AcademicReviewKind.Approve,"","stale",true)); await Reject<ConflictException>(()=>ReviewPercentage(repo)); Assert((await repo.GetPercentageAsync("fictional-internship")).Value.Reviews.Count==1); });
        await Test("repeated authorized revision cycles preserve all submissions and review decisions", async () => {
            var repo=ModernSeed(DecisionPolicy(),missingAcademic:true); var original=(await repo.GetPercentageAsync("fictional-internship")).Value.Current.VersionId;
            for(int i=0;i<3;i++) { await ReviewPercentage(repo,AcademicReviewKind.RequestRevision,"Clarify fictional result"); var s=await repo.GetPercentageAsync("fictional-internship"); Assert(!(await ReadinessFlow(repo).ReviewAsync("fictional-internship")).IsReady); await SavePercentage(PercentageFlow(repo),s.Version,81+i,"Revised comments",true); s=await repo.GetPercentageAsync("fictional-internship"); Assert(PercentageEvaluationWorkflow.CurrentReview(s.Value)==null && s.Value.Current.Number==i+2); await PercentageFlow(repo).SubmitAsync("fictional-internship",s.Version,true); }
            await ReviewPercentage(repo); var value=(await repo.GetPercentageAsync("fictional-internship")).Value; Assert(value.Submissions.Count==4 && value.Reviews.Count==4 && value.Submissions[0].VersionId==original && value.Submissions[0].Percentage==80);
        });
        await Test("Company cannot open revision without an Academic Request Revision", async()=>{var repo=ModernSeed(DecisionPolicy());var s=await repo.GetPercentageAsync("fictional-internship");await Reject<ConflictException>(()=>SavePercentage(PercentageFlow(repo),s.Version,90,"override",true));});
        await Test("history tampering and immutable submitted result changes are rejected",async()=>{var repo=ModernSeed(DecisionPolicy());var s=await repo.GetPercentageAsync("fictional-internship");s.Value.Reviews.Clear();await Reject<ConflictException>(()=>repo.SavePercentageAsync(s.Value,s.Version));s=await repo.GetPercentageAsync("fictional-internship");s.Value.Current.Percentage=99;await Reject<ArgumentException>(()=>repo.SavePercentageAsync(s.Value,s.Version));});
        await Test("Coordinator requires current Academic approval and approved passing threshold",async()=>{var repo=ModernSeed(DecisionPolicy(),missingAcademic:true);Assert(!(await ReadinessFlow(repo).ReviewAsync("fictional-internship")).IsReady);await ReviewPercentage(repo);Assert((await ReadinessFlow(repo).ReviewAsync("fictional-internship")).IsReady);var p=DecisionPolicy();p.PassingThreshold=null;Assert(Blocked(await ReadinessFlow(ModernSeed(p)).ReviewAsync("fictional-internship"),"threshold"));p.PassingThreshold=90;Assert(Blocked(await ReadinessFlow(ModernSeed(p)).ReviewAsync("fictional-internship"),"below"));});
        await Test("Firebase aggregate writes carry exact ETag and transport failures never acknowledge success",async()=>{
            string body="null"; string version="null_etag"; int puts=0;
            var handler=new Handler { Respond=request=>{if(request.Method==HttpMethod.Get)return Response(request.RequestUri.AbsolutePath.StartsWith("/finalEvaluationReviewsV2/")?body:"null",version);Assert(request.Method==HttpMethod.Put&&request.RequestUri.AbsolutePath=="/finalEvaluationReviewsV2/fictional-internship.json"&&string.Join("",request.Headers.GetValues("if-match"))==version);body=request.Content.ReadAsStringAsync().GetAwaiter().GetResult();puts++;version="\"saved\"";return Response("null");}};
            var repo=Rest(handler);await repo.SavePercentageAsync(ModernValue(false,false),"null_etag");Assert(puts==1&&(await repo.GetPercentageAsync("fictional-internship")).Version==version);await Reject<ConflictException>(()=>repo.SavePercentageAsync(ModernValue(false,false),"null_etag"));
        });
        await Test("shared repository end-to-end percentage approval completion and private Student PDF",async()=>{
            var p=DecisionPolicy();p.CompletionReport=new CompletionReportPolicy { Enabled=true };p.StudentFinalOutcomeDisclosureAllowed=true;
            var repo=new InMemoryFinalEvaluationRepository(new[]{p});await SavePercentage(PercentageFlow(repo),"null_etag");var s=await repo.GetPercentageAsync("fictional-internship");await PercentageFlow(repo).SubmitAsync("fictional-internship",s.Version,true);await ReviewPercentage(repo);await DecisionFlow(repo).DecideAsync(DecisionRequest());
            var review=await repo.GetCompletionReviewAsync("fictional-internship");var fixture=new ReportFixture { Repo=repo,Metadata=new ReportMetadata(repo) };fixture.Data.Value=new VerifiedCompletionReportData { IsTrusted=true,InternshipId="fictional-internship",StudentId="fictional-student",PolicyId=p.PolicyId,PolicyVersion=p.Version,ApprovedAtUtc=review.ReviewedAtUtc,StudentName="Fictional Student",StudentNumber="TEST",OrganizationName="Fictional Organization",PeriodStart=new DateTime(2026,1,1),PeriodEnd=new DateTime(2026,2,1),ApprovedHours=10 };
            var doc=await fixture.Service().GenerateAsync("fictional-internship");var source=new StudentSources {DocumentId=doc.DocumentId};var student=StudentFlow(fixture,source);var view=await student.LoadAsync("fictional-internship");Assert(view.Status==CompletionStatus.Approved&&view.FinalOutcome=="Pass"&&view.ReportAvailable);Assert(CompletionReportValidation.Hash((await student.DownloadAsync("fictional-internship")).Content)==doc.Sha256);await Reject<UnauthorizedAccessException>(()=>StudentFlow(fixture,source,"other").DownloadAsync("fictional-internship"));
            s=await repo.GetPercentageAsync("fictional-internship");await Reject<ConflictException>(()=>SavePercentage(PercentageFlow(repo),s.Version,90,"late edit",true));
        });
    }
}
