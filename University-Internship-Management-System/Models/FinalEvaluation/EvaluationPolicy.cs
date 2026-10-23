using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace University_Internship_Management_System.Models.FinalEvaluation
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EvaluationStatus { Draft, Submitted }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompletionStatus { Pending, UnderReview, Approved, Rejected, ChangesRequested }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DocumentStatus { Pending, Issued, Revoked }

    public abstract class EvaluationRecord
    {
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string InternshipId { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string StudentId { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string EvaluatorId { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string PolicyId { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string PolicyVersion { get; set; }
        public Dictionary<string, decimal?> Scores { get; set; } = new Dictionary<string, decimal?>();
        public string Feedback { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public DateTimeOffset? SubmittedAtUtc { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public EvaluationStatus Status { get; set; }
    }

    public sealed class CriterionPolicy
    {
        public bool RequireFeedback { get; set; }
        public string Label { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public decimal Minimum { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public decimal Maximum { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool Required { get; set; }
        public decimal? Weight { get; set; }
    }

    public sealed class EvaluationPolicy
    {
        // Absent means confidential; this grants disclosure of pass/fail only, never forms or notes.
        public bool? StudentFinalOutcomeDisclosureAllowed { get; set; }
        public CompletionReportPolicy CompletionReport { get; set; }
        public CompletionDecisionPolicy CompletionDecisions { get; set; }
        // Missing approval or an unspecified final-comment rule fails closed for submission.
        public bool IsApproved { get; set; }
        // Development/test policies can be marked approved for isolated workflow testing,
        // but must never authorize official evaluation, completion, or disclosure decisions.
        public bool IsTestPolicy { get; set; }
        [JsonIgnore]
        public bool IsProductionApproved { get { return IsApproved && !IsTestPolicy; } }
        public bool? CompanyCommentsRequired { get; set; }
        public bool? AcademicCommentsRequired { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string PolicyId { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public string Version { get; set; }
        public Dictionary<string, CriterionPolicy> CompanyCriteria { get; set; } = new Dictionary<string, CriterionPolicy>();
        public Dictionary<string, CriterionPolicy> AcademicCriteria { get; set; } = new Dictionary<string, CriterionPolicy>();
        public decimal? RequiredHours { get; set; }
        public decimal? PassingThreshold { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
    public sealed class CompletionReportPolicy
    {
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool Enabled { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool RequireUniversityName { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool RequireCollegeName { get; set; }
    }

    public sealed class CompletionDecisionPolicy
    {
        public CompletionDecisionRule Approve { get; set; }
        public CompletionDecisionRule RequestChanges { get; set; }
        public CompletionDecisionRule Reject { get; set; }
    }
    public sealed class CompletionDecisionRule
    {
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool Enabled { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool RequireReason { get; set; }
        [JsonProperty(Required = Newtonsoft.Json.Required.Always)]
        public bool RequireSupportingNotes { get; set; }
    }
}
