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

        protected override void BindStatistics(int? assigned, int? drafts, int? awaiting, int? approved)
        {
            AssignedTraineesCount = Count(assigned);
            CompanyDraftCount = Count(drafts);
            AwaitingReviewCount = Count(awaiting);
            AcademicApprovalCount = Count(approved);
        }
        private static string Count(int? value) { return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "—"; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Runs after the existing page workflow, including confirmed postback writes.
            // Statistics remain unavailable until an authorized Firebase context exists.
        }

    }
}
