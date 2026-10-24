using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public sealed class DemoTrainingReport
    { public string Id { get; set; } public string Title { get; set; } public string Status { get; set; } public string Body { get; set; } }
    public static class DevelopmentDemoSeed
    {
        public static EvaluationPolicy Policy()
        {
            return new EvaluationPolicy { PolicyId = "local-fictional-policy", Version = "test-v2", IsApproved = true,
                PassingThreshold = 70, RequiredHours = 10, StudentFinalOutcomeDisclosureAllowed = true,
                CompletionDecisions = new CompletionDecisionPolicy { Approve = new CompletionDecisionRule { Enabled = true }, RequestChanges = new CompletionDecisionRule { Enabled = true }, Reject = new CompletionDecisionRule { Enabled = true } },
                CompletionReport = new CompletionReportPolicy { Enabled = true }, UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        }
        public static CompletionPlacement[] Placements()
        {
            return new[] {
                new CompletionPlacement { InternshipId = "dev-company-internship", StudentId = "local-lina", DisplayName = "Lina Haddad (fictional)", CompanyName = "DEMO Northstar Training", PolicyId = "local-fictional-policy", PolicyVersion = "test-v2" },
                new CompletionPlacement { InternshipId = "demo-omar", StudentId = "local-omar", DisplayName = "Omar Khalil (fictional)", CompanyName = "DEMO Northstar Training", PolicyId = "local-fictional-policy", PolicyVersion = "test-v2" },
                new CompletionPlacement { InternshipId = "demo-noor", StudentId = "local-noor", DisplayName = "Noor Saleh (fictional)", CompanyName = "DEMO Northstar Training", PolicyId = "local-fictional-policy", PolicyVersion = "test-v2" } };
        }
        public static TrainingHourEntry[] Hours(string id) { return new[] { new TrainingHourEntry { EntryId = id + "-approved", Hours = 10, IsApproved = true }, new TrainingHourEntry { EntryId = id + "-pending", Hours = 50, IsApproved = false } }; }
        public static StudentTaskRecord[] Tasks(string id)
        {
            return new[] {
                new StudentTaskRecord { Id = id + "-published-task", Title = "DEMO published documentation task", Status = StudentTaskStatus.Evaluated, IsVisibleToStudent = true, IsResultPublished = true, Grade = 4, MaximumGrade = 7, StudentVisibleFeedback = "DEMO published task feedback. Independent of final evaluation." },
                new StudentTaskRecord { Id = id + "-unpublished-task", Title = "DEMO task awaiting publication", Status = StudentTaskStatus.Submitted, IsVisibleToStudent = true, IsResultPublished = false, Grade = 9, MaximumGrade = 10, StudentVisibleFeedback = "PRIVATE_UNPUBLISHED_DEMO_FEEDBACK" },
                new StudentTaskRecord { Id = id + "-hidden-task", Title = "PRIVATE_HIDDEN_DEMO_TASK", Status = StudentTaskStatus.Assigned, IsVisibleToStudent = false, StudentVisibleFeedback = "PRIVATE_HIDDEN_DEMO_FEEDBACK" } };
        }
        public static DemoTrainingReport[] Reports(string id) { return new[] { new DemoTrainingReport { Id = id + "-report", Title = "DEMO training progress report", Status = "Submitted", Body = "Fictional report: prepared documentation and reviewed a sample training exercise. No real student or attachment." } }; }
        public static CompletionChecklist TaskRequirements(string id) { return new CompletionChecklist { RequiredIds = new[] { id + "-published-task" }, CompletedIds = new[] { id + "-published-task" } }; }
        public static CompletionChecklist ReportRequirements(string id) { return new CompletionChecklist { RequiredIds = new[] { id + "-report" }, CompletedIds = new[] { id + "-report" } }; }
        public static JObject Definition()
        {
            var placements = Placements();
            var value = new { label = "DEMO ONLY — fictional test policy and records; not official university requirements", fictional = true,
                users = new[] { new { id = "local-company", role = "CompanySupervisor", name = "DEMO Company Supervisor" }, new { id = "local-academic", role = "AcademicSupervisor", name = "DEMO Academic Supervisor" }, new { id = "local-coordinator", role = "Coordinator", name = "DEMO University Coordinator" } }
                    .Concat(placements.Select((p, i) => new { id = "local-student-" + (char)('A' + i), role = "Student", name = p.DisplayName })).ToArray(),
                placements = placements,
                assignments = placements.Select(p => new { p.InternshipId, p.StudentId, companySupervisorId = "local-company", academicSupervisorId = "local-academic", coordinatorId = "local-coordinator", eligible = true }),
                policy = Policy(),
                training = placements.Select(p => new { p.InternshipId, requiredHours = 10, entries = Hours(p.InternshipId), tasks = Tasks(p.InternshipId), reports = Reports(p.InternshipId), requiredTasks = TaskRequirements(p.InternshipId), requiredReports = ReportRequirements(p.InternshipId), organization = "DEMO Northstar Training", start = "2026-01-01", end = "2026-02-01" }) };
            using (var reader = new JsonTextReader(new StringReader(JsonConvert.SerializeObject(value, FirebaseFinalEvaluationRepository.Json))) { DateParseHandling = DateParseHandling.None }) return JObject.Load(reader);
        }
        public static DevelopmentDemoDataset Create(string datasetId)
        {
            var policy = Policy(); var memory = new InMemoryFinalEvaluationRepository(new[] { policy });
            var source = new PercentageDevelopmentHub.Sources(); var guard = new PercentageDevelopmentHub.LocalGuard();
            var company = new PercentageEvaluationWorkflow(memory, new PercentageDevelopmentHub.User("local-company"), source, source, guard);
            var academic = new PercentageEvaluationWorkflow(memory, new PercentageDevelopmentHub.User("local-academic"), source, source, guard);
            var result = new DevelopmentDemoDataset { Owner = FirebaseDevelopmentDemoRepository.OwnerMarker, DatasetId = datasetId, SchemaVersion = 1, CreatedAtUtc = DateTimeOffset.UtcNow, Definition = Definition() };
            var placements = Placements();
            for (int i = 0; i < placements.Length; i++)
            {
                var p = placements[i];
                company.SaveDraftAsync(p.InternshipId, "null_etag", i == 0 ? (decimal?)null : 82 + i, i == 0 ? "" : "DEMO Company submitted comments for " + p.DisplayName).GetAwaiter().GetResult();
                if (i > 0)
                {
                    var draft = memory.GetPercentageAsync(p.InternshipId).GetAwaiter().GetResult();
                    company.SubmitAsync(p.InternshipId, draft.Version, true).GetAwaiter().GetResult();
                    if (i == 2) { var submitted = memory.GetPercentageAsync(p.InternshipId).GetAwaiter().GetResult(); academic.ReviewAsync(p.InternshipId, submitted.Version, submitted.Value.Current.VersionId, AcademicReviewKind.Approve, "DEMO accepted current Company submission", Guid.NewGuid().ToString("N"), true).GetAwaiter().GetResult(); }
                }
                result.Evaluations[p.InternshipId] = memory.GetPercentageAsync(p.InternshipId).GetAwaiter().GetResult().Value;
                result.Completions[p.InternshipId] = new CompletionReview { InternshipId = p.InternshipId, StudentId = p.StudentId, PolicyId = p.PolicyId, PolicyVersion = p.PolicyVersion,
                    Status = CompletionStatus.Pending, CreatedAtUtc = result.CreatedAtUtc, UpdatedAtUtc = result.CreatedAtUtc };
            }
            return result;
        }
    }
}
