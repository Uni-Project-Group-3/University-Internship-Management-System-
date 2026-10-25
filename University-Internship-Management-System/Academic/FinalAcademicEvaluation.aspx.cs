using University_Internship_Management_System.Services.FinalEvaluation;
namespace University_Internship_Management_System.Academic
{
    public partial class FinalAcademicEvaluation : PercentageEvaluationPage
    {
        protected override bool Academic { get { return true; } }
        // Reviews the company's final report in the shared workflow (FinalReports.aspx), where it then reaches the coordinator.
        protected override bool UseUnifiedFinalReportRoute { get { return true; } }
    }
}
