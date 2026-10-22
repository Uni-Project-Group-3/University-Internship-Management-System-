using System;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Company
{
    public partial class ReportsFinalEvaluation : Page
    {
        private const string DemoCompanyId = "demo-company";
        private const string DemoSupervisorId = "demo-supervisor";

        private Trainee[] assignedTrainees;
        private Trainee selectedTrainee;
        private Criterion[] criterionDefinitions;
        private bool evaluationReady;

        protected void Page_Init(object sender, EventArgs e)
        {
            assignedTrainees = GetDemoTrainees()
                .Where(t => t.CompanyId == DemoCompanyId &&
                            t.SupervisorId == DemoSupervisorId)
                .ToArray();

            Intern.DataSource = assignedTrainees;
            Intern.DataTextField = "Name";
            Intern.DataValueField = "Id";
            Intern.DataBind();

            Intern.Items.Insert(0,
                new ListItem("Choose a trainee", ""));

            selectedTrainee =
                FindTrainee(Request.Form[Intern.UniqueID]);

            string config =
                Request.Form[CriteriaConfig.UniqueID] == "weighted"
                    ? "weighted" : "points";

            criterionDefinitions = GetCriteria(config);

            Criteria.DataSource = criterionDefinitions;
            Criteria.DataBind();

            PopulateReports();
            SetEditingPermissions();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            NavName.Text = Session["FullName"] as string ?? "Company Portal";
            Title = "Reports & Final Evaluation | UIMS";

            ReportPreview.Text = "";
            FinalPreview.Text = "";
            EvaluationError.Text = "";

            selectedTrainee = FindTrainee(Intern.SelectedValue);

            BindIntern();
            BindOverview();
            SetEditingPermissions();
        }

        protected void Intern_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectTrainee(Intern.SelectedValue);
            ActiveView.Value = "reports";
        }

        protected void Trainees_ItemCommand(
            object source,
            RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Reports" &&
                e.CommandName != "Evaluation" &&
                e.CommandName != "Details")
                return;

            string id = Convert.ToString(
                e.CommandArgument,
                CultureInfo.InvariantCulture);

            SelectTrainee(id);
            ActiveView.Value = e.CommandName.ToLowerInvariant();
        }

        protected void Reports_ItemCommand(
            object source,
            RepeaterCommandEventArgs e)
        {
            if (selectedTrainee == null)
                return;

            if (e.CommandName != "Read" &&
                e.CommandName != "Review")
                return;

            string id = Convert.ToString(
                e.CommandArgument,
                CultureInfo.InvariantCulture);

            if (Report.Items.FindByValue(id) == null)
                return;

            Report.SelectedValue = id;
            ReportFeedback.Text = "";
            ReportMode.Value =
                e.CommandName == "Review" ? "review" : "read";

            ActiveView.Value = "reports";
            BindReportExcerpt();
        }

        protected void Report_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReportFeedback.Text = "";
            ActiveView.Value = "reports";
            BindReportExcerpt();
        }

        protected void PreviewReport_Click(object sender, EventArgs e)
        {
            ActiveView.Value = "reports";
            ReportMode.Value = "review";

            if (selectedTrainee == null ||
                Report.Items.FindByValue(Report.SelectedValue) == null)
                return;

            string feedback = ReportFeedback.Text.Trim();

            if (feedback.Length == 0 || feedback.Length > 2000)
            {
                ReportPreview.Text =
                    "Enter report feedback between 1 and 2,000 characters.";
                return;
            }

            string decision = ReportOutcome.SelectedValue;

            // Supports both the original and updated ASPX options.
            string outcome;

            switch (decision)
            {
                case "accepted":
                    outcome = "Accepted";
                    break;

                case "rejected":
                    outcome = "Rejected";
                    break;

                case "corrections":
                    outcome = "Corrections requested";
                    break;

                default:
                    outcome = "Reviewed";
                    break;
            }

            if (sender == SaveReportReview)
            {
                ViewState["Review." + selectedTrainee.Id + "." + Report.SelectedValue] = new[] { outcome, feedback };
                BindReports();
                BindOverview();
            }

            ReportPreview.Text =
                "Review preview only. No Firebase data was changed.\n" +
                "Report: " + Report.SelectedItem.Text + "\n" +
                "Decision: " + outcome + "\n" +
                "Feedback: " + feedback;
        }

        protected void CriteriaConfig_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            ActiveView.Value = "evaluation";

            if (!CanEditEvaluation())
                return;

            ViewState.Remove("Ready." + selectedTrainee.Id);
            ApplyDraft(null);

            FinalPreview.Text =
                "Demo configuration changed. Unsaved values were cleared.";
        }

        protected void SaveDraft_Click(object sender, EventArgs e)
        {
            ActiveView.Value = "evaluation";

            EvaluationDraft draft;

            if (!CanEditEvaluation() ||
                !TryReadDraft(false, out draft))
                return;

            ViewState["Draft." + selectedTrainee.Id] = draft;
            ViewState.Remove("Ready." + selectedTrainee.Id);

            FinalStatus.Text = "Draft · demo";

            FinalPreview.Text =
                "Draft stored temporarily in this page. " +
                "Nothing was saved to Firebase.";

            BindOverview();
        }

        protected void PreviewDraft_Click(object sender, EventArgs e)
        {
            ActiveView.Value = "evaluation";

            EvaluationDraft draft;

            if (!CanEditEvaluation() ||
                !TryReadDraft(true, out draft))
                return;

            ViewState["Draft." + selectedTrainee.Id] = draft;

            evaluationReady = true;

            BindEvaluationPreview(draft);

            ViewState["Ready." + selectedTrainee.Id] = true;
            FinalStatus.Text = "Ready for Submission · demo";

            FinalPreview.Text =
                "Evaluation preview completed. " +
                "Official submission requires backend integration.";

            BindOverview();
        }

        protected void CancelEvaluation_Click(object sender, EventArgs e)
        {
            SelectTrainee(Intern.SelectedValue);
            ActiveView.Value = "trainees";
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Frontend demo rendering only; this flag never grants authorization.
            if (Request.QueryString["frontendDemo"] == "1")
            {
                InternDetails.Visible = true;
                ReportSection.Visible = true;
                ReportDetailsPanel.Visible = true;
                ReportFeedbackPanel.Visible = true;
                EvaluationPreview.Visible = true;
                NoReports.Visible = true;
                Criteria.Visible = false;
                Assessments.Visible = false;
                FinalComments.Enabled = true;
                SaveDraft.Enabled = true;
                PreviewDraft.Enabled = true;
                ConfirmSubmission.Enabled = true;
                return;
            }

            string view = ActiveView.Value;

            if (selectedTrainee == null ||
                (view != "reports" &&
                 view != "evaluation" &&
                 view != "details"))
            {
                view = "trainees";
            }

            ActiveView.Value = view;
            SelectInternMessage.Visible = false;
            ReviewSubmission.Disabled = selectedTrainee == null || IsSubmitted(selectedTrainee) || !(ViewState["Ready." + selectedTrainee.Id] as bool? == true);

            // Original single-view behavior.
            SetHidden(OverviewView, false);
            SetHidden(InternDetails, view == "trainees");

            SetHidden(ReportsView, view != "reports");
            SetHidden(EvaluationView, view != "evaluation");
            SetHidden(DetailsView, view != "details");

            SetHidden(ReportDetailsPanel,
                ReportMode.Value != "read" &&
                ReportMode.Value != "review");

            SetHidden(ReportFeedbackPanel,
                ReportMode.Value != "review");
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void ConfirmSubmission_Click(object sender, EventArgs e)
        {
            ActiveView.Value = "evaluation";
            EvaluationDraft current;
            if (!CanEditEvaluation() || !TryReadDraft(true, out current)) return;
            EvaluationDraft preview = GetDraft(selectedTrainee);
            if (!(ViewState["Ready." + selectedTrainee.Id] as bool? == true) || preview == null ||
                preview.Configuration != current.Configuration || preview.Comments != current.Comments ||
                !preview.Scores.SequenceEqual(current.Scores) || !preview.Feedback.SequenceEqual(current.Feedback))
            {
                EvaluationError.Text = "The evaluation changed. Preview it again before confirming.";
                return;
            }
            ViewState["Submitted." + selectedTrainee.Id] = DateTime.UtcNow;
            ViewState.Remove("Ready." + selectedTrainee.Id);
            SetEditingPermissions();
            BindIntern();
            BindOverview();
            FinalPreview.Text = "Preview submitted and locked for this page visit only. Nothing was sent to an Academic Supervisor or Firebase. Internship completion remains subject to Coordinator approval.";
        }

        private bool IsSubmitted(Trainee trainee)
        {
            return trainee != null && (trainee.Status == "Submitted" || ViewState["Submitted." + trainee.Id] is DateTime);
        }

        private void SelectTrainee(string id)
        {
            selectedTrainee = FindTrainee(id);
            ReportMode.Value = "";

            Intern.SelectedValue =
                selectedTrainee == null ? "" : selectedTrainee.Id;

            ReportFeedback.Text = "";
            ReportPreview.Text = "";
            FinalPreview.Text = "";
            EvaluationError.Text = "";

            PopulateReports();

            if (selectedTrainee != null)
            {
                EvaluationDraft draft = GetDraft(selectedTrainee);

                CriteriaConfig.SelectedValue =
                    draft == null ? "points" : draft.Configuration;

                criterionDefinitions =
                    GetCriteria(CriteriaConfig.SelectedValue);

                Criteria.DataSource = criterionDefinitions;
                Criteria.DataBind();

                ApplyDraft(draft);
            }

            SetEditingPermissions();
            BindIntern();
            BindOverview();
        }

        private void BindOverview()
        {
            TraineeTotal.Text =
                assignedTrainees.Length.ToString(
                    CultureInfo.InvariantCulture);

            PendingReports.Text =
                assignedTrainees.Sum(t => Math.Max(0, t.PendingReports -
                    (ViewState["Review." + t.Id + ".report-b"] is string[] &&
                     ((string[])ViewState["Review." + t.Id + ".report-b"])[0] == "Reviewed" ? 1 : 0)))
                    .ToString(CultureInfo.InvariantCulture);

            DraftTotal.Text =
                assignedTrainees.Count(t => !IsSubmitted(t))
                    .ToString(CultureInfo.InvariantCulture);

            SubmittedTotal.Text =
                assignedTrainees.Count(t => IsSubmitted(t))
                    .ToString(CultureInfo.InvariantCulture);

            Trainees.DataSource = assignedTrainees.Select(t =>
            {
                string status = GetEvaluationStatus(t);

                return new
                {
                    t.Id,
                    t.Name,
                    t.StudentNumber,
                    t.University,
                    t.Major,

                    Reports = t.ReportCount == 0
                        ? "No reports"
                        : Math.Max(0, t.PendingReports -
                            (ViewState["Review." + t.Id + ".report-b"] is string[] &&
                             ((string[])ViewState["Review." + t.Id + ".report-b"])[0] == "Reviewed" ? 1 : 0)) + " pending / " +
                          t.ReportCount + " total",

                    Status = status + " · demo",

                    StatusKey = status == "Not Started"
                        ? "not-started"
                        : status == "Ready for Submission"
                            ? "ready"
                            : status.ToLowerInvariant(),

                    Action = status == "Submitted"
                        ? "View submitted evaluation"
                        : status == "Not Started"
                            ? "Start evaluation"
                            : "Continue draft"
                };
            });

            Trainees.DataBind();
        }

        private void BindIntern()
        {
            InternDetails.Visible = selectedTrainee != null;
            SelectInternMessage.Visible = selectedTrainee == null;

            EvaluationPreview.Visible = false;

            if (selectedTrainee == null)
                return;

            SelectedName.Text =
                selectedTrainee.Name + " · " +
                selectedTrainee.StudentNumber;

            int pendingTasks = Math.Max(0,
                selectedTrainee.AssignedTasks -
                selectedTrainee.CompletedTasks);

            TaskCount.Text =
                selectedTrainee.AssignedTasks + " / " +
                selectedTrainee.CompletedTasks + " / " +
                pendingTasks;

            int approvedHours =
                selectedTrainee.ApprovedHourEntries
                    .Where(h => h > 0)
                    .Sum();

            int progress = selectedTrainee.RequiredHours > 0
                ? Math.Min(100, (int)(
                    approvedHours * 100m /
                    selectedTrainee.RequiredHours))
                : 0;

            Hours.Text =
                approvedHours + " / " +
                selectedTrainee.RequiredHours;

            ProgressPercent.Text =
                progress.ToString(CultureInfo.InvariantCulture);

            TrainingProgress.Attributes["value"] =
                ProgressPercent.Text;

            TaskSummary.Text =
                "Task feedback · demo: " +
                selectedTrainee.TaskFeedback;

            AcademicRecipient.Text =
                selectedTrainee.AcademicSupervisor;

            SubmissionDate.Text =
                IsSubmitted(selectedTrainee)
                    ? (ViewState["Submitted." + selectedTrainee.Id] is DateTime
                        ? ((DateTime)ViewState["Submitted." + selectedTrainee.Id]).ToString("dd MMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture) + " · preview"
                        : "18 Oct 2026 · fictional record")
                    : "Not submitted";

            FinalStatus.Text =
                GetEvaluationStatus(selectedTrainee) + " · demo";

            BindReports();
            BindAssessments();

            if (IsSubmitted(selectedTrainee))
            {
                EvaluationDraft draft = GetDraft(selectedTrainee);
                ApplyDraft(draft);
                BindEvaluationPreview(draft);
            }
        }

        private void PopulateReports()
        {
            Report.Items.Clear();

            if (selectedTrainee == null ||
                selectedTrainee.ReportCount == 0)
                return;

            Report.Items.Add(new ListItem(
                "Illustrative progress note", "report-a"));

            if (selectedTrainee.ReportCount > 1)
            {
                Report.Items.Add(new ListItem(
                    "Illustrative activity summary", "report-b"));
            }
        }

        private void BindReports()
        {
            bool hasReports = selectedTrainee.ReportCount > 0;

            NoReports.Visible = !hasReports;
            ReportSection.Visible = hasReports;

            var reports = new[]
            {
                new
                {
                    Id = "report-a",
                    Title = "Illustrative progress note",
                    Submitted = "03 Oct 2026 · demo",
                    ReviewStatus = "Reviewed · demo"
                },
                new
                {
                    Id = "report-b",
                    Title = "Illustrative activity summary",
                    Submitted = "10 Oct 2026 · demo",
                    ReviewStatus = "Pending review · demo"
                }
            };

            Reports.DataSource =
                reports.Take(selectedTrainee.ReportCount).Select(report => new
                {
                    report.Id, report.Title, report.Submitted,
                    ReviewStatus = ViewState["Review." + selectedTrainee.Id + "." + report.Id] is string[]
                        ? ((string[])ViewState["Review." + selectedTrainee.Id + "." + report.Id])[0] + " · preview"
                        : report.ReviewStatus
                });

            Reports.DataBind();

            BindReportExcerpt();
        }

        private void BindReportExcerpt()
        {
            bool second = Report.SelectedValue == "report-b";

            ReportExistingFeedback.Text = second
                ? "No supervisor review has been recorded."
                : "Clear overview of the onboarding work.";

            var savedReview = ViewState["Review." + selectedTrainee.Id + "." + Report.SelectedValue] as string[];
            if (savedReview != null) ReportExistingFeedback.Text = savedReview[1];

            ReportExcerpt.Text = second
                ? "Illustrative excerpt: the trainee prepared " +
                  "API test cases and documented technical activities."
                : "Illustrative excerpt: the trainee completed " +
                  "environment setup and onboarding documentation.";
        }

        private void BindAssessments()
        {
            bool available =
                selectedTrainee.Status != "Not Started";

            var assessments = new[]
            {
                new
                {
                    Title = "Illustrative practical assessment",
                    Date = "12 Oct 2026 · demo",

                    Grade = IsSubmitted(selectedTrainee)
                        ? "9 / 12"
                        : "18 / 24",

                    CompanyFeedback =
                        "Example feedback: improve technical explanations.",

                    AcademicFeedback =
                        "Academic feedback does not change company grades."
                }
            };

            Assessments.DataSource = available
                ? assessments
                : assessments.Take(0);

            Assessments.DataBind();

            AssessmentCount.Text = available ? "1" : "0";
            NoAssessments.Visible = !available;
        }

        private void SetEditingPermissions()
        {
            bool locked =
                selectedTrainee != null &&
                IsSubmitted(selectedTrainee);

            CriteriaConfig.Enabled = !locked;
            FinalComments.ReadOnly = locked;
            SaveDraft.Enabled = !locked;
            PreviewDraft.Enabled = !locked;

            foreach (RepeaterItem item in Criteria.Items)
            {
                TextBox score =
                    item.FindControl("Score") as TextBox;

                TextBox feedback =
                    item.FindControl("CriterionFeedback") as TextBox;

                if (score != null)
                    score.ReadOnly = locked;

                if (feedback != null)
                    feedback.ReadOnly = locked;
            }
        }

        private void ApplyDraft(EvaluationDraft draft)
        {
            for (int i = 0; i < Criteria.Items.Count; i++)
            {
                RepeaterItem item = Criteria.Items[i];

                TextBox score =
                    (TextBox)item.FindControl("Score");

                TextBox feedback =
                    (TextBox)item.FindControl("CriterionFeedback");

                score.Text =
                    draft == null ? "" : draft.Scores[i];

                feedback.Text =
                    draft == null ? "" : draft.Feedback[i];
            }

            FinalComments.Text =
                draft == null ? "" : draft.Comments;
        }

        private bool TryReadDraft(
            bool requireComplete,
            out EvaluationDraft draft)
        {
            draft = new EvaluationDraft
            {
                Configuration = CriteriaConfig.SelectedValue,
                Scores = new string[criterionDefinitions.Length],
                Feedback = new string[criterionDefinitions.Length],
                Comments = FinalComments.Text.Trim()
            };

            EvaluationError.Text = "";

            for (int i = 0; i < criterionDefinitions.Length; i++)
            {
                RepeaterItem item = Criteria.Items[i];

                string scoreText =
                    ((TextBox)item.FindControl("Score"))
                        .Text.Trim();

                string feedback =
                    ((TextBox)item.FindControl("CriterionFeedback"))
                        .Text.Trim();

                Criterion criterion = criterionDefinitions[i];

                decimal score;

                if (scoreText.Length == 0 && requireComplete)
                {
                    EvaluationError.Text =
                        "Score required for " + criterion.Name;
                    return false;
                }

                if (scoreText.Length > 0)
                {
                    bool valid = decimal.TryParse(
                        scoreText,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out score);

                    if (!valid ||
                        score < criterion.Minimum ||
                        score > criterion.Maximum)
                    {
                        EvaluationError.Text =
                            "Invalid score for " + criterion.Name;
                        return false;
                    }
                }

                if (feedback.Length > 1000)
                {
                    EvaluationError.Text =
                        "Feedback cannot exceed 1,000 characters.";
                    return false;
                }

                draft.Scores[i] = scoreText;
                draft.Feedback[i] = feedback;
            }

            if (draft.Comments.Length > 3000 ||
                (requireComplete && draft.Comments.Length == 0))
            {
                EvaluationError.Text =
                    "General comments are required for a complete preview.";
                return false;
            }

            return true;
        }

        private void BindEvaluationPreview(EvaluationDraft draft)
        {
            if (draft == null)
                return;

            EvaluationPreview.Visible = true;

            bool complete =
                draft.Scores.All(s => !string.IsNullOrWhiteSpace(s));

            PreviewResult.Text = complete
                ? CalculateDemoResult(draft)
                : "Incomplete demonstration evaluation.";

            PreviewComments.Text = draft.Comments;

            PreviewCriteria.DataSource =
                criterionDefinitions.Select((criterion, i) => new
                {
                    criterion.Name,

                    Score = draft.Scores[i] + " / " +
                        criterion.Maximum.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture),

                    Feedback = string.IsNullOrWhiteSpace(
                        draft.Feedback[i])
                            ? "No feedback provided."
                            : draft.Feedback[i]
                });

            PreviewCriteria.DataBind();
        }

        private string CalculateDemoResult(EvaluationDraft draft)
        {
            decimal[] scores = draft.Scores
                .Select(s => decimal.Parse(
                    s,
                    CultureInfo.InvariantCulture))
                .ToArray();

            if (draft.Configuration == "weighted")
            {
                decimal totalWeight =
                    criterionDefinitions.Sum(
                        c => c.Weight.GetValueOrDefault());

                decimal weightedScore =
                    criterionDefinitions.Select((c, i) =>
                        scores[i] / c.Maximum *
                        c.Weight.GetValueOrDefault())
                        .Sum();

                decimal result = totalWeight > 0
                    ? weightedScore / totalWeight
                    : 0;

                return "Illustrative weighted index: " +
                    result.ToString("0.000", CultureInfo.InvariantCulture) +
                    ". Not an official result.";
            }

            decimal earned = scores.Sum();
            decimal maximum =
                criterionDefinitions.Sum(c => c.Maximum);

            return "Illustrative point total: " +
                earned.ToString("0.##", CultureInfo.InvariantCulture) +
                " / " +
                maximum.ToString("0.##", CultureInfo.InvariantCulture) +
                ". Not an official result.";
        }

        private static void SetHidden(
            WebControl control,
            bool hidden)
        {
            if (hidden)
            {
                control.Attributes["hidden"] = "hidden";
                return;
            }

            control.Attributes.Remove("hidden");
        }

        private Trainee FindTrainee(string id)
        {
            if (assignedTrainees == null ||
                string.IsNullOrEmpty(id))
                return null;

            return assignedTrainees.FirstOrDefault(t => t.Id == id);
        }

        private bool CanEditEvaluation()
        {
            if (selectedTrainee != null &&
                !IsSubmitted(selectedTrainee))
                return true;

            EvaluationError.Text =
                "Select a trainee with an editable evaluation.";
            return false;
        }

        private string GetEvaluationStatus(Trainee trainee)
        {
            if (IsSubmitted(trainee))
                return "Submitted";

            if ((evaluationReady || ViewState["Ready." + trainee.Id] as bool? == true) &&
                selectedTrainee != null &&
                selectedTrainee.Id == trainee.Id)
                return "Ready for Submission";

            return GetDraft(trainee) == null
                ? "Not Started"
                : "Draft";
        }

        private EvaluationDraft GetDraft(Trainee trainee)
        {
            if (trainee == null)
                return null;

            EvaluationDraft saved =
                ViewState["Draft." + trainee.Id] as EvaluationDraft;

            if (saved != null)
                return saved;

            if (trainee.Status == "Not Started")
                return null;

            bool submitted = IsSubmitted(trainee);

            return new EvaluationDraft
            {
                Configuration = "points",

                Scores = submitted
                    ? new[] { "4", "3", "2" }
                    : new[] { "3", "", "" },

                Feedback = new[]
                {
                    "Example: careful practical work.",
                    "",
                    ""
                },

                Comments = submitted
                    ? "Fictional submitted evaluation."
                    : ""
            };
        }

        private static Criterion[] GetCriteria(string configuration)
        {
            bool weighted = configuration == "weighted";

            return new[]
            {
                new Criterion
                {
                    Id = "technical",
                    Name = "Technical Skills · Example",
                    Description = "Practical technical abilities.",
                    Minimum = 0,
                    Maximum = weighted ? 10 : 5,
                    Weight = weighted ? (decimal?)0.5m : null
                },
                new Criterion
                {
                    Id = "communication",
                    Name = "Communication · Example",
                    Description = "Communication and teamwork.",
                    Minimum = 0,
                    Maximum = weighted ? 5 : 4,
                    Weight = weighted ? (decimal?)0.3m : null
                },
                new Criterion
                {
                    Id = "commitment",
                    Name = "Commitment · Example",
                    Description = "Reliability and commitment.",
                    Minimum = 0,
                    Maximum = weighted ? 4 : 3,
                    Weight = weighted ? (decimal?)0.2m : null
                }
            };
        }

        private static Trainee[] GetDemoTrainees()
        {
            return new[]
            {
                new Trainee
                {
                    Id = "demo-lina",
                    Name = "Lina Haddad",
                    StudentNumber = "DEMO-2101",
                    University = "JUST · demo",
                    Major = "Software Engineering",
                    CompanyId = DemoCompanyId,
                    SupervisorId = DemoSupervisorId,
                    AcademicSupervisor = "Dr. Sara Ali · Demo",
                    Status = "Draft",
                    ReportCount = 2,
                    PendingReports = 1,
                    AssignedTasks = 4,
                    CompletedTasks = 1,
                    ApprovedHourEntries = new[] { 32, 24, 16 },
                    RequiredHours = 160,
                    TaskFeedback = "Add missing test cases."
                },
                new Trainee
                {
                    Id = "demo-omar",
                    Name = "Omar Khalil",
                    StudentNumber = "DEMO-2102",
                    University = "JUST · demo",
                    Major = "Computer Engineering",
                    CompanyId = DemoCompanyId,
                    SupervisorId = DemoSupervisorId,
                    AcademicSupervisor = "Dr. Rami Nasser · Demo",
                    Status = "Submitted",
                    ReportCount = 1,
                    PendingReports = 0,
                    AssignedTasks = 4,
                    CompletedTasks = 3,
                    ApprovedHourEntries = new[] { 40, 40, 40 },
                    RequiredHours = 160,
                    TaskFeedback = "Good documentation."
                },
                new Trainee
                {
                    Id = "demo-noor",
                    Name = "Noor Saleh",
                    StudentNumber = "DEMO-2103",
                    University = "Illustrative university",
                    Major = "Computer Science",
                    CompanyId = DemoCompanyId,
                    SupervisorId = DemoSupervisorId,
                    AcademicSupervisor = "Dr. Sara Ali · Demo",
                    Status = "Not Started",
                    ReportCount = 0,
                    PendingReports = 0,
                    AssignedTasks = 2,
                    CompletedTasks = 0,
                    ApprovedHourEntries = new int[0],
                    RequiredHours = 120,
                    TaskFeedback = "No feedback available."
                }
            };
        }

        private sealed class Trainee
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string StudentNumber { get; set; }
            public string University { get; set; }
            public string Major { get; set; }
            public string CompanyId { get; set; }
            public string SupervisorId { get; set; }
            public string AcademicSupervisor { get; set; }
            public string Status { get; set; }
            public int ReportCount { get; set; }
            public int PendingReports { get; set; }
            public int AssignedTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int[] ApprovedHourEntries { get; set; }
            public int RequiredHours { get; set; }
            public string TaskFeedback { get; set; }
        }

        private sealed class Criterion
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Minimum { get; set; }
            public decimal Maximum { get; set; }
            public decimal? Weight { get; set; }

            public string WeightLabel
            {
                get
                {
                    return Weight.HasValue
                        ? Weight.Value.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture) + " · example"
                        : "Not used";
                }
            }
        }

        [Serializable]
        private sealed class EvaluationDraft
        {
            public string Configuration { get; set; }
            public string[] Scores { get; set; }
            public string[] Feedback { get; set; }
            public string Comments { get; set; }
        }
    }
}
