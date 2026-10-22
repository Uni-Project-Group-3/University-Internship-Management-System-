using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Display data must be verified, current and bound to the exact approval and policy.
    // Deliberately no grades, evaluation forms, feedback, signatures, logos or seals.
    public sealed class VerifiedCompletionReportData
    {
        public bool IsTrusted { get; set; }
        public string InternshipId { get; set; }
        public string StudentId { get; set; }
        public string PolicyId { get; set; }
        public string PolicyVersion { get; set; }
        public DateTimeOffset? ApprovedAtUtc { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string OrganizationName { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public decimal? ApprovedHours { get; set; }
        public string UniversityName { get; set; }
        public string CollegeName { get; set; }
    }
    public interface ICompletionReportAccess
    {
        Task<bool> CanGenerateAsync(string userId, string internshipId);
        // Must check trusted Student role and ownership; null studentId means resolve ownership
        // from authoritative placement data BEFORE any privileged completion/metadata reads.
        Task<bool> IsStudentOwnerAsync(string userId, string internshipId, string studentId);
    }
    public interface ICompletionReportDataReader
    { Task<VerifiedCompletionReportData> GetAsync(string internshipId, string studentId, string policyId, string policyVersion); }
    public interface ICompletionReportPdfRenderer
    { byte[] Render(VerifiedCompletionReportData data, string documentId, DateTimeOffset generatedAtUtc); }
    public interface IPrivateCompletionReportStorage
    {
        // Private, atomic create-if-absent. Never overwrite and never return public URLs.
        Task PutNewAsync(string objectKey, byte[] pdf);
        Task<byte[]> ReadAsync(string objectKey);
        // Cleanup only this attempt's exact object/hash, never a parent or unrelated object.
        Task DeleteIfHashMatchesAsync(string objectKey, string sha256);
    }
    public interface ICompletionDocumentMetadataRepository
    { Task CreateIssuedCompletionReportAsync(CompletionDocument document); }
    public sealed class CompletionReportDownload
    {
        public byte[] Content { get; internal set; }
        public string ContentType { get { return "application/pdf"; } }
        public string FileName { get; internal set; }
    }
    public sealed class CompletionReportService
    {
        private readonly IFinalEvaluationRepository repository;
        private readonly IAuthenticatedUser user;
        private readonly ICompletionReportAccess access;
        private readonly ICompletionReportDataReader data;
        private readonly ICompletionReportPdfRenderer renderer;
        private readonly IPrivateCompletionReportStorage storage;
        private readonly ICompletionDocumentMetadataRepository metadata;
        public bool IsGenerationConfigured { get { return data != null && renderer != null && storage != null && metadata != null && user != null && access != null; } }
        public CompletionReportService(IFinalEvaluationRepository repository, IAuthenticatedUser user, ICompletionReportAccess access,
            ICompletionReportDataReader data, ICompletionReportPdfRenderer renderer, IPrivateCompletionReportStorage storage,
            ICompletionDocumentMetadataRepository metadata = null)
        {
            this.repository = repository ?? throw new ArgumentNullException("repository"); this.user = user; this.access = access;
            this.data = data; this.renderer = renderer; this.storage = storage; this.metadata = metadata ?? repository as ICompletionDocumentMetadataRepository;
        }
        private async Task Authorize(string id, bool generation, string studentId = null)
        {
            FirebaseKey.Require(id);
            if (user == null || string.IsNullOrWhiteSpace(user.UserId) || access == null ||
                !(generation ? await access.CanGenerateAsync(user.UserId, id).ConfigureAwait(false) : await access.IsStudentOwnerAsync(user.UserId, id, studentId).ConfigureAwait(false))) throw new UnauthorizedAccessException();
        }
        private async Task<CompletionReview> Approved(string id)
        {
            var review = await repository.GetCompletionReviewAsync(id).ConfigureAwait(false);
            if (review == null || review.InternshipId != id || review.Status != CompletionStatus.Approved || !review.ReviewedAtUtc.HasValue || review.ReviewedAtUtc == default(DateTimeOffset))
                throw new InvalidOperationException("An authoritative Approved completion decision with an approval date is required.");
            FinalEvaluationValidation.Completion(review);
            var policy = await repository.GetPolicyAsync(review.PolicyId).ConfigureAwait(false);
            if (policy == null || !policy.IsApproved || policy.PolicyId != review.PolicyId || policy.Version != review.PolicyVersion || policy.CompletionReport == null || !policy.CompletionReport.Enabled)
                throw new InvalidOperationException("An applicable approved policy explicitly enabling completion reports is required.");
            return review;
        }
        public async Task<CompletionDocument> GenerateAsync(string internshipId)
        {
            await Authorize(internshipId, true).ConfigureAwait(false);
            if (!IsGenerationConfigured) throw new InvalidOperationException("Trusted report data, PDF rendering and private document storage integrations are not connected.");
            var review = await Approved(internshipId).ConfigureAwait(false);
            var policy = await repository.GetPolicyAsync(review.PolicyId).ConfigureAwait(false);
            var provided = await data.GetAsync(internshipId, review.StudentId, review.PolicyId, review.PolicyVersion).ConfigureAwait(false);
            var snapshot = JsonConvert.DeserializeObject<VerifiedCompletionReportData>(JsonConvert.SerializeObject(provided));
            CompletionReportValidation.Data(snapshot, review, policy);
            string documentId = Guid.NewGuid().ToString("N"); var generatedAt = DateTimeOffset.UtcNow;
            var bytes = renderer.Render(snapshot, documentId, generatedAt);
            CompletionReportPdfRenderer.ValidatePdf(bytes);
            var document = new CompletionDocument { InternshipId = internshipId, DocumentId = documentId, StudentId = review.StudentId,
                IssuerId = user.UserId, PolicyId = review.PolicyId, PolicyVersion = review.PolicyVersion, DocumentType = CompletionReportValidation.ReportType,
                StorageObjectPath = CompletionReportValidation.ObjectKey(internshipId, documentId), ContentType = "application/pdf", Sha256 = CompletionReportValidation.Hash(bytes), ByteLength = bytes.LongLength,
                Status = DocumentStatus.Issued, CreatedAtUtc = generatedAt, IssuedAtUtc = generatedAt, CompletionApprovedAtUtc = review.ReviewedAtUtc };
            CompletionReportValidation.Metadata(document);
            // Recheck permission and approval before publishing; approval is terminal in this module.
            await Authorize(internshipId, true).ConfigureAwait(false);
            var latest = await Approved(internshipId).ConfigureAwait(false);
            if (latest.StudentId != review.StudentId || latest.ReviewedAtUtc != review.ReviewedAtUtc || latest.PolicyId != review.PolicyId || latest.PolicyVersion != review.PolicyVersion) throw new ConflictException();
            await storage.PutNewAsync(document.StorageObjectPath, (byte[])bytes.Clone()).ConfigureAwait(false);
            try
            {
                VerifyBytes(document, await storage.ReadAsync(document.StorageObjectPath).ConfigureAwait(false));
                await metadata.CreateIssuedCompletionReportAsync(document).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A metadata acknowledgment may be lost after commit. Confirm the actual record
                // before claiming success OR deleting anything. Uncertain outcomes stay private.
                CompletionDocument persisted;
                try { persisted = await repository.GetCompletionDocumentAsync(internshipId, documentId).ConfigureAwait(false); }
                catch (Exception) { throw new RepositoryException("Report persistence could not be confirmed. Private storage cleanup requires reconciliation."); }
                if (persisted != null)
                {
                    CompletionReportValidation.Metadata(persisted);
                    if (JsonConvert.SerializeObject(persisted) != JsonConvert.SerializeObject(document)) throw new ConflictException();
                    VerifyBytes(persisted, await storage.ReadAsync(persisted.StorageObjectPath).ConfigureAwait(false));
                    return persisted;
                }
                try { await storage.DeleteIfHashMatchesAsync(document.StorageObjectPath, document.Sha256).ConfigureAwait(false); }
                catch (Exception) { /* A private orphan is safer than deleting a possibly unrelated object. */ }
                throw new RepositoryException("Report generation was not published. Retry after the storage or metadata failure is resolved.");
            }
            return document;
        }
        public async Task<CompletionReportDownload> DownloadForStudentAsync(string internshipId, string documentId)
        {
            await Authorize(internshipId, false).ConfigureAwait(false);
            CompletionReportValidation.DocumentId(documentId);
            if (storage == null) throw new InvalidOperationException("Private document storage is not connected.");
            var review = await Approved(internshipId).ConfigureAwait(false);
            await Authorize(internshipId, false, review.StudentId).ConfigureAwait(false);
            var document = await repository.GetCompletionDocumentAsync(internshipId, documentId).ConfigureAwait(false);
            CompletionReportValidation.Metadata(document);
            if (document.InternshipId != internshipId || document.DocumentId != documentId || document.StudentId != review.StudentId || document.PolicyId != review.PolicyId ||
                document.PolicyVersion != review.PolicyVersion || document.CompletionApprovedAtUtc != review.ReviewedAtUtc) throw new UnauthorizedAccessException();
            var bytes = await storage.ReadAsync(document.StorageObjectPath).ConfigureAwait(false); VerifyBytes(document, bytes);
            await Authorize(internshipId, false, review.StudentId).ConfigureAwait(false);
            return new CompletionReportDownload { Content = (byte[])bytes.Clone(), FileName = "internship-completion-report-" + documentId + ".pdf" };
        }
        private static void VerifyBytes(CompletionDocument document, byte[] bytes)
        {
            CompletionReportPdfRenderer.ValidatePdf(bytes);
            if (document.ByteLength != bytes.LongLength || document.Sha256 != CompletionReportValidation.Hash(bytes)) throw new RepositoryException("Stored report does not match its metadata.");
        }
    }
    public static class CompletionReportValidation
    {
        public const string ReportType = "InternshipCompletionReport";
        public static string Hash(byte[] value) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(value)).Replace("-", "").ToLowerInvariant(); }
        public static void DocumentId(string id) { if (id == null || !Regex.IsMatch(id, "\\A[0-9a-f]{32}\\z")) throw new ArgumentException("A valid report identifier is required."); }
        public static string ObjectKey(string id, string documentId) { FirebaseKey.Require(id); DocumentId(documentId); return "completion-reports/" + Uri.EscapeDataString(id) + "/" + documentId + ".pdf"; }
        public static void Metadata(CompletionDocument document)
        {
            if (document == null) throw new InvalidOperationException("An issued completion report is not available.");
            FinalEvaluationValidation.Document(document); DocumentId(document.DocumentId);
            if (document.Status != DocumentStatus.Issued || document.DocumentType != ReportType || document.ContentType != "application/pdf" ||
                document.StorageObjectPath != ObjectKey(document.InternshipId, document.DocumentId) || document.Sha256 == null || !Regex.IsMatch(document.Sha256, "\\A[0-9a-f]{64}\\z") ||
                !document.ByteLength.HasValue || document.ByteLength <= 0 || !document.CompletionApprovedAtUtc.HasValue || document.CompletionApprovedAtUtc == default(DateTimeOffset) ||
                document.CreatedAtUtc == default(DateTimeOffset) || document.CreatedAtUtc < document.CompletionApprovedAtUtc || document.IssuedAtUtc != document.CreatedAtUtc) throw new ArgumentException("Completion report metadata is incomplete or invalid.");
        }
        public static void Data(VerifiedCompletionReportData value, CompletionReview review, EvaluationPolicy policy)
        {
            if (value == null || !value.IsTrusted || value.InternshipId != review.InternshipId || value.StudentId != review.StudentId || value.PolicyId != review.PolicyId ||
                value.PolicyVersion != review.PolicyVersion || value.ApprovedAtUtc != review.ReviewedAtUtc || policy == null || !policy.IsApproved || policy.PolicyId != review.PolicyId || policy.Version != review.PolicyVersion || policy.CompletionReport == null || !policy.CompletionReport.Enabled)
                throw new ArgumentException("Verified report data for the exact approved internship and policy is required.");
            Text(value.StudentName, "Student name", true); Text(value.StudentNumber, "Student number", true); Text(value.OrganizationName, "Internship organization", true);
            Text(value.UniversityName, "University name", policy.CompletionReport.RequireUniversityName); Text(value.CollegeName, "College name", policy.CompletionReport.RequireCollegeName);
            if (!value.PeriodStart.HasValue || !value.PeriodEnd.HasValue || value.PeriodStart == default(DateTime) || value.PeriodEnd == default(DateTime) || value.PeriodEnd.Value.Date < value.PeriodStart.Value.Date) throw new ArgumentException("A verified valid internship period is required.");
            if (!value.ApprovedHours.HasValue || value.ApprovedHours < 0) throw new ArgumentException("Verified approved training hours are required.");
            var audit = review.DecisionHistory == null ? null : review.DecisionHistory.LastOrDefault();
            if (audit != null && audit.ApprovedHours.HasValue && audit.ApprovedHours != value.ApprovedHours) throw new ArgumentException("Report hours must match the approved completion decision evidence.");
        }
        public static void Text(string value, string label, bool required)
        {
            if (required && string.IsNullOrWhiteSpace(value)) throw new ArgumentException(label + " is required from a trusted source.");
            if (value == null) return;
            if (value.Length > 512 || value.Any(c => char.IsControl(c) || (c >= '\u202a' && c <= '\u202e') || (c >= '\u2066' && c <= '\u2069')))
                throw new ArgumentException(label + " contains unsupported length or control characters.");
            for (int i = 0; i < value.Length; i++) if (char.IsSurrogate(value[i])) { if (!char.IsHighSurrogate(value[i]) || i + 1 >= value.Length || !char.IsLowSurrogate(value[++i])) throw new ArgumentException(label + " contains invalid Unicode."); }
        }
    }
}
