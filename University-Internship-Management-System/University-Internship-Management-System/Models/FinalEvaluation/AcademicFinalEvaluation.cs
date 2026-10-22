namespace University_Internship_Management_System.Models.FinalEvaluation
{
    public sealed class AcademicFinalEvaluation : EvaluationRecord
    {
        // Optional additive field; existing academic records remain compatible.
        public System.Collections.Generic.Dictionary<string, string> CriterionFeedback { get; set; }
            = new System.Collections.Generic.Dictionary<string, string>();
    }
}
