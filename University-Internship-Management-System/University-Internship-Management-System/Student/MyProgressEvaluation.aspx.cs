using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Student
{
    public partial class MyProgressEvaluation : Page
    {
        private const string DemoStudentId = "illustrative-student";
        private ExamResult[] publishedExams;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            NavName.Text = Session["FullName"] as string ?? "Student Portal";
            BindDemoProgress();
            BindPublishedExams();
        }

        protected void ToggleExamDemo_Click(object sender, EventArgs e)
        {
            ViewState["ExamDemo"] = !(ViewState["ExamDemo"] as bool? == true);
            ViewState.Remove("SelectedExam");
            BindPublishedExams();
        }

        protected void Exams_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "ViewExam")
            {
                return;
            }

            ViewState["SelectedExam"] = Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture);
            BindExamDetails();
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void BindDemoProgress()
        {
            const int requiredHours = 160;
            var timeEntries = new[]
            {
                new { Hours = 32, Approved = true },
                new { Hours = 24, Approved = true },
                new { Hours = 16, Approved = true },
                new { Hours = 8, Approved = false }
            };
            var tasks = new[]
            {
                new
                {
                    Title = "Document the onboarding workflow", Reference = "TASK-01 · Week 1",
                    StatusKey = "evaluated", Status = "Evaluated", Badge = "success",
                    Feedback = "Clear documentation and a useful process diagram.",
                    NextStep = "Evaluation completed; hours are approved separately.", NoteClass = "",
                    History = "03 Oct 2026: submitted. 05 Oct 2026: evaluated."
                },
                new
                {
                    Title = "Revise the API test cases", Reference = "TASK-02 · Week 2",
                    StatusKey = "revision", Status = "Revision required", Badge = "danger",
                    Feedback = "Add invalid-input and timeout scenarios before resubmitting.",
                    NextStep = "Action needed: revise and resubmit through My Tasks.", NoteClass = "revision-note",
                    History = "10 Oct 2026: submitted. 12 Oct 2026: returned for revision."
                },
                new
                {
                    Title = "Build the weekly activity summary", Reference = "TASK-03 · Week 3",
                    StatusKey = "submitted", Status = "Submitted", Badge = "warning",
                    Feedback = "Awaiting company supervisor review.", NextStep = "No evaluation recorded yet.", NoteClass = "",
                    History = "18 Oct 2026: submitted. Evaluation pending."
                },
                new
                {
                    Title = "Prepare a handover checklist", Reference = "TASK-04 · Week 4",
                    StatusKey = "assigned", Status = "Assigned", Badge = "",
                    Feedback = "No feedback available yet.", NextStep = "No submission recorded.", NoteClass = "",
                    History = "19 Oct 2026: assigned. No submission or evaluation history yet."
                }
            };

            int approvedHours = timeEntries.Where(entry => entry.Approved && entry.Hours > 0).Sum(entry => entry.Hours);
            int percent = requiredHours > 0 ? Math.Min(100, (int)(approvedHours * 100m / requiredHours)) : 0;
            ApprovedHours.Text = approvedHours.ToString(CultureInfo.InvariantCulture);
            HoursApprovalStatus.Text = ApprovedHours.Text;
            PendingHours.Text = timeEntries.Where(entry => !entry.Approved && entry.Hours > 0)
                .Sum(entry => entry.Hours).ToString(CultureInfo.InvariantCulture);
            RequiredHours.Text = requiredHours.ToString(CultureInfo.InvariantCulture);
            RemainingHours.Text = Math.Max(0, requiredHours - approvedHours).ToString(CultureInfo.InvariantCulture);
            AssignedTasks.Text = tasks.Length.ToString(CultureInfo.InvariantCulture);
            SubmittedTasks.Text = tasks.Count(task => task.StatusKey == "submitted").ToString(CultureInfo.InvariantCulture);
            EvaluatedTasks.Text = tasks.Count(task => task.StatusKey == "evaluated").ToString(CultureInfo.InvariantCulture);
            RevisionTasks.Text = tasks.Count(task => task.StatusKey == "revision").ToString(CultureInfo.InvariantCulture);
            HoursPercent.Text = percent.ToString(CultureInfo.InvariantCulture);
            HoursProgress.Attributes["value"] = HoursPercent.Text;
            Tasks.DataSource = tasks;
            Tasks.DataBind();
        }

        private void BindPublishedExams()
        {
            bool preview = ViewState["ExamDemo"] as bool? == true;
            // Production must query the authenticated student's permitted records server-side.
            publishedExams = GetIllustrativeExams(preview)
                .Where(exam => exam.StudentId == DemoStudentId && exam.Published
                    && exam.MaximumGrade.HasValue && exam.Grade.HasValue
                    && exam.MaximumGrade.Value > 0 && exam.Grade.Value >= 0
                    && exam.Grade.Value <= exam.MaximumGrade.Value)
                .ToArray();

            EvaluatedExams.Text = publishedExams.Length.ToString(CultureInfo.InvariantCulture)
                + (preview ? " · demo" : string.Empty);
            ExamDemoNotice.Visible = preview;
            ToggleExamDemo.Text = preview ? "Close illustrative preview" : "Preview illustrative exam results";
            NoExams.Visible = publishedExams.Length == 0;
            ExamList.Visible = publishedExams.Length > 0;
            Exams.DataSource = publishedExams;
            Exams.DataBind();
            BindExamDetails();
        }

        private void BindExamDetails()
        {
            string selectedId = ViewState["SelectedExam"] as string;
            var exam = publishedExams.FirstOrDefault(result => result.Id == selectedId);
            ExamDetails.Visible = exam != null;
            SelectExamNotice.Visible = exam == null;
            if (exam == null)
            {
                return;
            }

            ExamTitle.Text = exam.Title;
            ExamDescription.Text = string.IsNullOrWhiteSpace(exam.Description) ? "No exam description is available." : exam.Description;
            ExamDate.Text = exam.ExamDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            ExamSupervisor.Text = exam.CompanySupervisor;
            ExamGrade.Text = exam.Grade.Value.ToString("0.##", CultureInfo.InvariantCulture)
                + " / " + exam.MaximumGrade.Value.ToString("0.##", CultureInfo.InvariantCulture);
            CompanyFeedback.Text = string.IsNullOrWhiteSpace(exam.CompanyFeedback)
                ? "Company feedback has not been submitted yet." : exam.CompanyFeedback;
            bool hasAcademicFeedback = !string.IsNullOrWhiteSpace(exam.AcademicFeedback);
            AcademicFeedback.Text = hasAcademicFeedback
                ? exam.AcademicFeedback : "Academic feedback has not been submitted yet.";
            AcademicFeedbackStatus.Text = hasAcademicFeedback ? "Feedback available" : "Awaiting feedback";
            AcademicFeedbackStatus.CssClass = "workspace-badge " + (hasAcademicFeedback ? "success" : "warning");
        }

        private static ExamResult[] GetIllustrativeExams(bool preview)
        {
            if (!preview)
            {
                return new ExamResult[0];
            }

            // UI examples only: no real exam records or official grading policy were supplied.
            return new[]
            {
                new ExamResult
                {
                    Id = "example-a", StudentId = DemoStudentId, Published = true,
                    Title = "Illustrative assessment A", ExamDate = new DateTime(2026, 10, 12),
                    Description = "Fictional practical assessment used only to demonstrate the result layout.",
                    CompanySupervisor = "Sample Company Supervisor", Grade = 18m, MaximumGrade = 24m,
                    CompanyFeedback = "Example feedback: explain the reasoning behind your test cases more clearly.",
                    AcademicFeedback = "Example academic feedback: connect your approach to the methods discussed in class."
                },
                new ExamResult
                {
                    Id = "example-b", StudentId = DemoStudentId, Published = true,
                    Title = "Illustrative assessment B", ExamDate = new DateTime(2026, 10, 19),
                    Description = string.Empty, CompanySupervisor = "Sample Company Supervisor",
                    Grade = 7.5m, MaximumGrade = 10m, CompanyFeedback = string.Empty,
                    AcademicFeedback = string.Empty
                }
            };
        }

        private sealed class ExamResult
        {
            public string Id { get; set; }
            public string StudentId { get; set; }
            public bool Published { get; set; }
            public string Title { get; set; }
            public DateTime ExamDate { get; set; }
            public string Description { get; set; }
            public string CompanySupervisor { get; set; }
            public decimal? Grade { get; set; }
            public decimal? MaximumGrade { get; set; }
            public string CompanyFeedback { get; set; }
            public string AcademicFeedback { get; set; }
        }
    }
}
