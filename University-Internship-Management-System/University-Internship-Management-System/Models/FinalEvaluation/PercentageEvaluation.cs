using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace University_Internship_Management_System.Models.FinalEvaluation
{
    public sealed class ReviewKindConverter : StringEnumConverter { public ReviewKindConverter() { AllowIntegerValues = false; } }
    [JsonConverter(typeof(ReviewKindConverter))]
    public enum AcademicReviewKind { Approve, RequestRevision }
    public sealed class CompanyPercentageVersion
    {
        [JsonProperty(Required = Required.Always)]
        public string VersionId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Number { get; set; }
        public decimal? Percentage { get; set; }
        public string Comments { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string CompanySupervisorId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public DateTimeOffset? SubmittedAtUtc { get; set; }
    }
    public sealed class AcademicReviewDecision
    {
        [JsonProperty(Required = Required.Always)]
        public string DecisionId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string CompanyVersionId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public AcademicReviewKind Kind { get; set; }
        public string Reason { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string AcademicSupervisorId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public DateTimeOffset DecidedAtUtc { get; set; }
    }
    // Single CAS boundary: current version, immutable submitted versions and review history.
    // Legacy academic scores are retained at their original path, never converted to approval.
    public sealed class PercentageEvaluation
    {
        [JsonProperty(Required = Required.Always)]
        public int SchemaVersion { get; set; } = 2;
        [JsonProperty(Required = Required.Always)]
        public string InternshipId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string StudentId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string PolicyId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string PolicyVersion { get; set; }
        [JsonProperty(Required = Required.Always)]
        public CompanyPercentageVersion Current { get; set; }
        public List<CompanyPercentageVersion> Submissions { get; set; } = new List<CompanyPercentageVersion>();
        public List<AcademicReviewDecision> Reviews { get; set; } = new List<AcademicReviewDecision>();
    }
}
