using System;
using System.Collections.Generic;
using System.Linq;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    public static class FinalEvaluationValidation
    {
        public static void Evaluation(EvaluationRecord value, EvaluationPolicy policy, bool company, bool final = false)
        {
            Draft(value);
            if (policy == null || value.PolicyId != policy.PolicyId || value.PolicyVersion != policy.Version)
                throw new ArgumentException("The referenced policy and version must exist.");
            var criteria = company ? policy.CompanyCriteria : policy.AcademicCriteria;
            if (criteria == null || (final && criteria.Count == 0)) throw new ArgumentException("Policy criteria are not configured.");
            foreach (var criterion in criteria)
            {
                FirebaseKey.Require(criterion.Key);
                if (criterion.Value == null || criterion.Value.Minimum > criterion.Value.Maximum || criterion.Value.Weight < 0)
                    throw new ArgumentException("Invalid criterion policy.");
                decimal? score;
                if (final && criterion.Value.Required && (!value.Scores.TryGetValue(criterion.Key, out score) || !score.HasValue))
                    throw new ArgumentException("Required criterion is missing.");
            }
            foreach (var score in value.Scores)
            {
                CriterionPolicy criterion;
                if (!criteria.TryGetValue(score.Key, out criterion) ||
                    (score.Value.HasValue && (score.Value < criterion.Minimum || score.Value > criterion.Maximum)))
                    throw new ArgumentException("Unknown criterion or score outside policy range.");
            }
        }
        public static void Draft(EvaluationRecord value)
        {
            if (value == null) throw new ArgumentNullException("value");
            FirebaseKey.Require(value.InternshipId); FirebaseKey.Require(value.StudentId);
            FirebaseKey.Require(value.EvaluatorId); FirebaseKey.Require(value.PolicyId); FirebaseKey.Require(value.PolicyVersion);
            if (value.Status != EvaluationStatus.Draft || value.SubmittedAtUtc.HasValue)
                throw new ConflictException();
            if (value.Scores == null) throw new ArgumentException("Scores must be an object.");
            foreach (string key in value.Scores.Keys) FirebaseKey.Require(key);
        }
        public static void UnchangedIdentity(EvaluationRecord current, EvaluationRecord next)
        {
            if (current == null || current.Status != EvaluationStatus.Draft || current.SubmittedAtUtc.HasValue ||
                current.InternshipId != next.InternshipId || current.StudentId != next.StudentId ||
                current.EvaluatorId != next.EvaluatorId || current.PolicyId != next.PolicyId || current.PolicyVersion != next.PolicyVersion)
                throw new ConflictException();
        }
        // Guards for later phases only; no submit or approve operation is exposed.
        public static void EvaluationTransition(EvaluationStatus from, EvaluationStatus to)
        {
            if (from != EvaluationStatus.Draft || to != EvaluationStatus.Submitted) throw new ConflictException();
        }
        public static void CompletionTransition(CompletionStatus from, CompletionStatus to)
        {
            if (!((from == CompletionStatus.Pending && to == CompletionStatus.UnderReview) ||
                ((from == CompletionStatus.Pending || from == CompletionStatus.UnderReview || from == CompletionStatus.ChangesRequested) &&
                (to == CompletionStatus.Approved || to == CompletionStatus.Rejected || to == CompletionStatus.ChangesRequested))))
                throw new ConflictException();
        }
        public static void DocumentTransition(DocumentStatus from, DocumentStatus to)
        {
            if (!((from == DocumentStatus.Pending && to == DocumentStatus.Issued) ||
                (from == DocumentStatus.Issued && to == DocumentStatus.Revoked))) throw new ConflictException();
        }
        public static void Completion(CompletionReview value)
        {
            if (value == null) throw new ArgumentNullException("value");
            FirebaseKey.Require(value.InternshipId); FirebaseKey.Require(value.StudentId);
            FirebaseKey.Require(value.PolicyId); FirebaseKey.Require(value.PolicyVersion);
            if (!Enum.IsDefined(typeof(CompletionStatus), value.Status)) throw new ArgumentException("Invalid completion status.");
            if (value.Status != CompletionStatus.Pending) FirebaseKey.Require(value.ReviewerId);
            // Older records may have no history. Never synthesize one. When history exists,
            // require it to agree with the authoritative current decision before acknowledging retries.
            if (value.DecisionHistory != null && value.DecisionHistory.Count > 0)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var audit in value.DecisionHistory)
                {
                    if (audit == null || !Enum.IsDefined(typeof(CompletionDecisionKind), audit.Kind) || audit.DecidedAtUtc == default(DateTimeOffset) ||
                        audit.ReadinessBlockers == null || string.IsNullOrWhiteSpace(audit.EvidenceDigest)) throw new ArgumentException("Invalid completion decision history.");
                    FirebaseKey.Require(audit.DecisionId); FirebaseKey.Require(audit.CoordinatorId); FirebaseKey.Require(audit.PolicyId); FirebaseKey.Require(audit.PolicyVersion);
                    if (!ids.Add(audit.DecisionId)) throw new ArgumentException("Duplicate completion decision history ID.");
                }
                var last = value.DecisionHistory.Last();
                var expected = last.Kind == CompletionDecisionKind.Approve ? CompletionStatus.Approved : last.Kind == CompletionDecisionKind.Reject ? CompletionStatus.Rejected : CompletionStatus.ChangesRequested;
                if (value.Status != expected || value.ReviewerId != last.CoordinatorId || value.PolicyId != last.PolicyId || value.PolicyVersion != last.PolicyVersion ||
                    value.ReviewedAtUtc != last.DecidedAtUtc || value.UpdatedAtUtc != last.DecidedAtUtc || value.Feedback != last.Reason)
                    throw new ArgumentException("Completion state and decision history disagree.");
            }
        }
        public static void Document(CompletionDocument value)
        {
            if (value == null) throw new ArgumentNullException("value");
            FirebaseKey.Require(value.InternshipId); FirebaseKey.Require(value.DocumentId); FirebaseKey.Require(value.StudentId);
            FirebaseKey.Require(value.PolicyId); FirebaseKey.Require(value.PolicyVersion);
            if (!Enum.IsDefined(typeof(DocumentStatus), value.Status) || string.IsNullOrWhiteSpace(value.DocumentType))
                throw new ArgumentException("Invalid document metadata.");
            if (value.Status == DocumentStatus.Issued)
            {
                FirebaseKey.Require(value.IssuerId);
                if (!value.IssuedAtUtc.HasValue || string.IsNullOrWhiteSpace(value.StorageObjectPath))
                    throw new ArgumentException("Issued document metadata is incomplete.");
            }
        }
    }
}
