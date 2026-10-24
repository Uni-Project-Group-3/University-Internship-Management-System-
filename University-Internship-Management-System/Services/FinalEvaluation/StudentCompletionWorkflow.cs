using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public interface IStudentCompletionPlacementReader { Task<CompletionPlacement> GetAsync(string internshipId); }
    public interface IStudentVisibleTaskReader { Task<TrustedCompletionData<StudentTaskRecord[]>> GetAsync(string internshipId); }
    // Optional hours-team adapter: pending must be derived from explicit pending statuses,
    // never inferred from !IsApproved (which can include rejected entries).
    public interface IStudentPendingTrainingHoursReader
    { Task<TrustedCompletionData<decimal?>> GetPendingHoursAsync(string internshipId); }
    public interface IStudentOverallProgressReader { Task<TrustedCompletionData<decimal?>> GetAsync(string internshipId); }
    public interface IStudentCompletionReportLocator
    { Task<string> GetIssuedDocumentIdAsync(string internshipId, string studentId); }
    public enum StudentTaskStatus { Assigned, InProgress, Submitted, Evaluated, Revision }
    // These adapters belong to the placement/hours/tasks teams. Publication is explicit;
    // unpublished grades/feedback are never copied into the returned student projection.
    public sealed class StudentTaskRecord
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public StudentTaskStatus Status { get; set; }
        public bool IsVisibleToStudent { get; set; }
        public bool IsResultPublished { get; set; }
        public decimal? Grade { get; set; }
        public decimal? MaximumGrade { get; set; }
        public string StudentVisibleFeedback { get; set; }
    }
    public sealed class StudentVisibleTask
    {
        public string Title { get; internal set; }
        public string Reference { get; internal set; }
        public string StatusKey { get; internal set; }
        public string Status { get; internal set; }
        public string Badge { get; internal set; }
        public string Feedback { get; internal set; }
        public string NextStep { get; internal set; }
        public string NoteClass { get; internal set; }
        public string History { get; internal set; }
    }
    public sealed class StudentCompletionView
    {
        public string FinalOutcome { get; internal set; } = "Not disclosed by policy";
        public CompletionStatus? Status { get; internal set; }
        public DateTimeOffset? ApprovedAtUtc { get; internal set; }
        public decimal? ApprovedHours { get; internal set; }
        public decimal? RequiredHours { get; internal set; }
        public decimal? PendingHours { get; internal set; }
        public decimal? RemainingHours { get { return ApprovedHours.HasValue && RequiredHours.HasValue ? Math.Max(0, RequiredHours.Value - ApprovedHours.Value) : (decimal?)null; } }
        public decimal? HoursPercent { get; internal set; }
        public decimal? OverallPercent { get; internal set; }
        public StudentVisibleTask[] Tasks { get; internal set; } = new StudentVisibleTask[0];
        public bool TasksVerified { get; internal set; }
        public int PublishedTaskGradeCount { get; internal set; }
        public string PublishedTaskGradesText { get { return !TasksVerified ? "Published task grades unavailable" : PublishedTaskGradeCount > 0 ? PublishedTaskGradeCount.ToString(CultureInfo.InvariantCulture) : "No published task grades available"; } }
        public string[] Notices { get; internal set; } = new string[0];
        public bool ReportAvailable { get; internal set; }
        public string ReportMessage { get; internal set; }
    }
    public sealed class StudentReportUnavailableException : Exception
    {
        public int HttpStatus { get; private set; }
        public StudentReportUnavailableException(int status, string message) : base(message) { HttpStatus = status; }
    }
    public sealed class StudentCompletionWorkflow
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICompletionReportAccess access;
        private readonly IStudentCompletionPlacementReader placements;
        private readonly IApprovedTrainingHoursReader hours;
        private readonly IRequiredTrainingHoursReader required;
        private readonly IStudentVisibleTaskReader tasks;
        private readonly IStudentOverallProgressReader overall;
        private readonly IStudentCompletionReportLocator locator;
        private readonly CompletionReportService reports;
        public StudentCompletionWorkflow(IFinalEvaluationRepository repository, IAuthenticatedUser user, ICompletionReportAccess access,
            IStudentCompletionPlacementReader placements, IApprovedTrainingHoursReader hours, IRequiredTrainingHoursReader required,
            IStudentVisibleTaskReader tasks, IStudentOverallProgressReader overall, IStudentCompletionReportLocator locator, CompletionReportService reports)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository"); this.user = user; this.access = access;
            this.placements = placements; this.hours = hours; this.required = required; this.tasks = tasks; this.overall = overall; this.locator = locator; this.reports = reports;
        }
        private async Task<CompletionPlacement> Authorize(string id)
        {
            FirebaseKey.Require(id);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId) || access == null || !await access.IsStudentOwnerAsync(user.UserId, id, null).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            if (placements == null) throw new InvalidOperationException("Trusted student placement integration is unavailable.");
            var placement = await placements.GetAsync(id).ConfigureAwait(false);
            if (placement == null || placement.InternshipId != id || string.IsNullOrWhiteSpace(placement.StudentId) || string.IsNullOrWhiteSpace(placement.PolicyId) || string.IsNullOrWhiteSpace(placement.PolicyVersion)) throw new InvalidOperationException("Trusted internship placement is unavailable.");
            if (!await access.IsStudentOwnerAsync(user.UserId, id, placement.StudentId).ConfigureAwait(false)) throw new UnauthorizedAccessException();
            return placement;
        }
        private async Task<CompletionReview> Review(string id, CompletionPlacement placement)
        {
            var review = await repository.GetCompletionReviewAsync(id).ConfigureAwait(false);
            if (review != null)
            {
                if (review.InternshipId != id || review.StudentId != placement.StudentId) throw new UnauthorizedAccessException();
                FinalEvaluationValidation.Completion(review);
            }
            return review;
        }
        public async Task<StudentCompletionView> LoadAsync(string internshipId)
        {
            var placement = await Authorize(internshipId).ConfigureAwait(false);
            var review = await Review(internshipId, placement).ConfigureAwait(false);
            var view = new StudentCompletionView { Status = review == null ? (CompletionStatus?)null : review.Status,
                ApprovedAtUtc = review != null && review.Status == CompletionStatus.Approved ? review.ReviewedAtUtc : null };
            var notices = new List<string>();
            try
            {
                var policy = await repository.GetPolicyAsync(placement.PolicyId);
                if (policy != null && policy.IsProductionApproved && policy.PolicyId == placement.PolicyId && policy.Version == placement.PolicyVersion && policy.StudentFinalOutcomeDisclosureAllowed == true)
                {
                    var source = repository as IPercentageEvaluationRepository;
                    var evaluation = source == null ? null : (await source.GetPercentageAsync(internshipId)).Value;
                    if (evaluation != null)
                    {
                        PercentageEvaluationWorkflow.Validate(evaluation, placement);
                        var decision = PercentageEvaluationWorkflow.CurrentReview(evaluation);
                        view.FinalOutcome = decision?.Kind == AcademicReviewKind.Approve && evaluation.Current.SubmittedAtUtc.HasValue && policy.PassingThreshold >= 0 && policy.PassingThreshold <= 100
                            ? evaluation.Current.Percentage >= policy.PassingThreshold ? "Pass" : "Fail" : "Cannot yet be determined";
                    }
                    else view.FinalOutcome = "Cannot yet be determined";
                }
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { view.FinalOutcome = "Cannot yet be determined"; }
            try
            {
                var ledger = hours == null ? null : await hours.GetAsync(internshipId).ConfigureAwait(false);
                if (!Matches(ledger, placement) || ledger.Value == null || ledger.Value.Any(e => e == null || e.Hours < 0 || string.IsNullOrWhiteSpace(e.EntryId)) || ledger.Value.Select(e => e.EntryId).Distinct().Count() != ledger.Value.Length)
                    notices.Add("Trusted approved-hours data is unavailable.");
                else view.ApprovedHours = ledger.Value.Where(e => e.IsApproved).Sum(e => e.Hours);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { notices.Add("Approved training hours could not be verified."); }
            try
            {
                var target = required == null ? null : await required.GetAsync(internshipId).ConfigureAwait(false);
                if (!Matches(target, placement) || !target.Value.HasValue || target.Value < 0) notices.Add("Trusted required-hours data is unavailable.");
                else view.RequiredHours = target.Value;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { notices.Add("Required training hours could not be verified."); }
            try
            {
                var pendingSource = hours as IStudentPendingTrainingHoursReader;
                var pending = pendingSource == null ? null : await pendingSource.GetPendingHoursAsync(internshipId).ConfigureAwait(false);
                if (!Matches(pending, placement) || !pending.Value.HasValue || pending.Value < 0)
                    notices.Add("Pending attendance hours are unavailable; a trusted pending-status source is required. Rejected entries are not pending hours.");
                else view.PendingHours = pending.Value;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { notices.Add("Pending attendance hours could not be verified."); }
            if (view.RequiredHours > 0 && view.ApprovedHours.HasValue) view.HoursPercent = view.ApprovedHours >= view.RequiredHours ? 100 : view.ApprovedHours.Value / view.RequiredHours.Value * 100;
            try
            {
                var progress = overall == null ? null : await overall.GetAsync(internshipId).ConfigureAwait(false);
                if (!Matches(progress, placement) || !progress.Value.HasValue || progress.Value < 0 || progress.Value > 100) notices.Add("Overall completion progress is not supplied by a trusted source.");
                else view.OverallPercent = progress.Value;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { notices.Add("Overall completion progress could not be verified."); }
            try
            {
                var records = tasks == null ? null : await tasks.GetAsync(internshipId).ConfigureAwait(false);
                if (!Matches(records, placement) || records.Value == null || records.Value.Any(t => t == null || !Enum.IsDefined(typeof(StudentTaskStatus), t.Status))) notices.Add("Student-visible task results are unavailable.");
                else { view.TasksVerified = true; view.PublishedTaskGradeCount = records.Value.Count(t => t.IsVisibleToStudent && t.IsResultPublished && t.Grade.HasValue); view.Tasks = records.Value.Where(t => t.IsVisibleToStudent).Select(t => new StudentVisibleTask {
                    Title = t.Title, Reference = t.Id, StatusKey = t.Status.ToString().ToLowerInvariant(), Status = t.Status == StudentTaskStatus.Revision ? "Revision required" : t.Status == StudentTaskStatus.InProgress ? "In progress" : t.Status.ToString(),
                    Badge = t.Status == StudentTaskStatus.Evaluated ? "success" : t.Status == StudentTaskStatus.Revision ? "danger" : "warning", Feedback = t.IsResultPublished ? (t.StudentVisibleFeedback ?? "No student-visible feedback supplied.") : "Task feedback has not been published.",
                    NextStep = t.IsResultPublished && t.Grade.HasValue ? "Published task grade: " + t.Grade.Value.ToString(CultureInfo.InvariantCulture) + (t.MaximumGrade.HasValue ? " / " + t.MaximumGrade.Value.ToString(CultureInfo.InvariantCulture) : "") : "No published task grade supplied.",
                    NoteClass = "", History = "Task history integration is not connected." }).ToArray(); }
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception) { notices.Add("Student-visible task results could not be verified."); }
            if (view.Status == CompletionStatus.Approved)
            {
                try { await DownloadAsync(internshipId).ConfigureAwait(false); view.ReportAvailable = true; view.ReportMessage = "A verified completion report is available."; }
                catch (UnauthorizedAccessException) { throw; }
                catch (StudentReportUnavailableException error) { view.ReportMessage = error.Message; }
            }
            else view.ReportMessage = "Report download requires an Approved completion decision.";
            view.Notices = notices.ToArray(); return view;
        }
        public async Task<CompletionReportDownload> DownloadAsync(string internshipId)
        {
            var placement = await Authorize(internshipId).ConfigureAwait(false);
            var review = await Review(internshipId, placement).ConfigureAwait(false);
            if (review == null || review.Status != CompletionStatus.Approved) throw new StudentReportUnavailableException(409, "Completion is not Approved. Report download is unavailable.");
            if (locator == null || reports == null) throw new StudentReportUnavailableException(503, "Trusted report access integration is unavailable.");
            try
            {
                string documentId = await locator.GetIssuedDocumentIdAsync(internshipId, placement.StudentId).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(documentId)) throw new StudentReportUnavailableException(404, "Completion is Approved. The PDF has not yet been made available.");
                // Reuses the existing owner-only, metadata- and hash-validated download contract.
                return await reports.DownloadForStudentAsync(internshipId, documentId).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (StudentReportUnavailableException) { throw; }
            catch (InvalidOperationException) { throw new StudentReportUnavailableException(404, "Completion is Approved. The PDF is not currently available."); }
            catch (Exception) { throw new StudentReportUnavailableException(503, "Completion is Approved, but the PDF is unavailable or failed integrity validation. Please try again later."); }
        }
        private static bool Matches<T>(TrustedCompletionData<T> value, CompletionPlacement placement)
        { return value != null && value.IsTrusted && value.InternshipId == placement.InternshipId && value.StudentId == placement.StudentId && value.PolicyId == placement.PolicyId && value.PolicyVersion == placement.PolicyVersion; }
    }
}
