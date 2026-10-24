using Newtonsoft.Json;
using System;
namespace University_Internship_Management_System.Models.FinalEvaluation
{
    public sealed class CompletionDocument
    {
        [JsonProperty(Required = Required.Always)]
        public string InternshipId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string DocumentId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string StudentId { get; set; }
        public string IssuerId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string PolicyId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string PolicyVersion { get; set; }
        public string DocumentType { get; set; }
        public string StorageObjectPath { get; set; }
        public string StorageProvider { get; set; }
        public string Sha256 { get; set; }
        [JsonProperty(Required = Required.Always)]
        public DocumentStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? IssuedAtUtc { get; set; }
        public long? ByteLength { get; set; }
        public string ContentType { get; set; }
        public DateTimeOffset? CompletionApprovedAtUtc { get; set; }
    }
}
