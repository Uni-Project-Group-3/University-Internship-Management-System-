using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
namespace University_Internship_Management_System.Models.FinalEvaluation
{
    public sealed class CompletionReview
    {
        [JsonProperty(Required = Required.Always)]
        public string InternshipId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string StudentId { get; set; }
        public string ReviewerId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string PolicyId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string PolicyVersion { get; set; }
        [JsonProperty(Required = Required.Always)]
        public CompletionStatus Status { get; set; }
        public string Feedback { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public DateTimeOffset? ReviewedAtUtc { get; set; }
        public List<CompletionDecisionAudit> DecisionHistory { get; set; } = new List<CompletionDecisionAudit>();
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompletionDecisionKind { Approve, RequestChanges, Reject }
    public sealed class CompletionDecisionAudit
    {
        public string DecisionId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public CompletionDecisionKind Kind { get; set; }
        public CompletionStatus? PreviousStatus { get; set; }
        public string CoordinatorId { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
        public string Reason { get; set; }
        public string SupportingNotes { get; set; }
        public DateTimeOffset DecidedAtUtc { get; set; }
        // RTDB omits empty arrays. An approval legitimately has no blockers.
        public string[] ReadinessBlockers { get; set; } = new string[0];
        public decimal? ApprovedHours { get; set; }
        public decimal? RequiredHours { get; set; }
        public string EvidenceDigest { get; set; }
    }
}
