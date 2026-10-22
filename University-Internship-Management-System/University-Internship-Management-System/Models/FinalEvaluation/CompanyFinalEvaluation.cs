using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace University_Internship_Management_System.Models.FinalEvaluation
{
    public class CompanyFinalEvaluation : EvaluationRecord
    {
        // Optional additive company-only field; absent fields in Phase 0 records remain valid.
        public Dictionary<string, string> CriterionFeedback { get; set; } = new Dictionary<string, string>();
    }
}
