using University_Internship_Management_System.Services.FinalEvaluation;
namespace University_Internship_Management_System.Academic
{
    public partial class FinalAcademicEvaluation : PercentageEvaluationPage
    {
        protected override bool Academic { get { return true; } }
        protected override bool UseUnifiedFinalReportRoute { get { return false; } }
    }
}
