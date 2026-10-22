using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.UI;
using University_Internship_Management_System.Services.FinalEvaluation;
namespace University_Internship_Management_System.Company
{
    public partial class ReportsFinalEvaluation : PercentageEvaluationPage
    {
        protected override bool Academic { get { return false; } }
        protected string AssignedTraineesCount { get; private set; } = "—";
        protected string CompanyDraftCount { get; private set; } = "—";
        protected string AwaitingReviewCount { get; private set; } = "—";
        protected string AcademicApprovalCount { get; private set; } = "—";

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Runs after the existing page workflow, including confirmed postback writes.
            RegisterAsyncTask(new PageAsyncTask(LoadStatisticsAsync));
        }
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var context = await PercentagePageIntegration.Resolve(Context, false);
                if (context?.Workflow == null || string.IsNullOrWhiteSpace(context.UserId) || context.Placements == null) return;
                int assigned = 0, drafts = 0, awaiting = 0, approved = 0;
                foreach (var placement in context.Placements)
                {
                    try
                    {
                        var record = await context.Workflow.LoadAsync(placement.InternshipId, false);
                        assigned++;
                        switch (PercentageEvaluationWorkflow.Status(record.Value))
                        {
                            case "Draft": drafts++; break;
                            case "Submitted": awaiting++; break;
                            case "Approved": approved++; break;
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (ArgumentException) { } // Same invalid/legacy rows excluded by the existing overview.
                }
                AssignedTraineesCount = assigned.ToString(CultureInfo.InvariantCulture);
                CompanyDraftCount = drafts.ToString(CultureInfo.InvariantCulture);
                AwaitingReviewCount = awaiting.ToString(CultureInfo.InvariantCulture);
                AcademicApprovalCount = approved.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception) { /* Unverified counts remain unavailable, never fabricated zeros. */ }
        }
    }
}
