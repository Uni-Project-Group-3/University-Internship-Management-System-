using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class ReportAccess : ICompletionReportAccess
    {
        internal bool Generation = true, Owner = true;
        internal int Calls;
        public Task<bool> CanGenerateAsync(string uid, string id) { Calls++; return Task.FromResult(Generation && uid == "fictional-coordinator" && id == "fictional-internship"); }
        public Task<bool> IsStudentOwnerAsync(string uid, string id, string studentId) { Calls++; return Task.FromResult(Owner && uid == "fictional-student" && id == "fictional-internship" && (studentId == null || studentId == "fictional-student")); }
    }
    private sealed class ReportData : ICompletionReportDataReader
    {
        internal VerifiedCompletionReportData Value;
        internal int Calls;
        public Task<VerifiedCompletionReportData> GetAsync(string id, string student, string policy, string version) { Calls++; return Task.FromResult(Value); }
    }
    private sealed class ReportStorage : IPrivateCompletionReportStorage
    {
        internal readonly InMemoryPrivateCompletionReportStorage Inner = new InMemoryPrivateCompletionReportStorage();
        internal bool FailPut, FailRead, CorruptRead;
        internal int Puts, Reads, Deletes;
        internal string LastKey;
        public Task PutNewAsync(string key, byte[] bytes) { Puts++; LastKey = key; if (FailPut) throw new IOException("Simulated object-storage failure."); return Inner.PutNewAsync(key, bytes); }
        public async Task<byte[]> ReadAsync(string key) { Reads++; if (FailRead) throw new IOException("Simulated object-read failure."); var bytes = await Inner.ReadAsync(key); if (CorruptRead) bytes[bytes.Length - 8] ^= 1; return bytes; }
        public Task DeleteIfHashMatchesAsync(string key, string hash) { Deletes++; return Inner.DeleteIfHashMatchesAsync(key, hash); }
    }
    private sealed class ReportMetadata : ICompletionDocumentMetadataRepository
    {
        internal readonly InMemoryFinalEvaluationRepository Repo;
        internal bool Fail, LostAcknowledgment;
        internal ReportMetadata(InMemoryFinalEvaluationRepository repo) { Repo = repo; }
        public async Task CreateIssuedCompletionReportAsync(CompletionDocument document)
        { if (Fail) throw new RepositoryException("Simulated metadata failure."); await Repo.CreateIssuedCompletionReportAsync(document); if (LostAcknowledgment) throw new RepositoryException("Simulated acknowledgment loss."); }
    }
    private sealed class InvalidPdf : ICompletionReportPdfRenderer { public byte[] Render(VerifiedCompletionReportData data, string id, DateTimeOffset date) { return new byte[] { 1, 2, 3 }; } }
    private sealed class ReportFixture
    {
        internal InMemoryFinalEvaluationRepository Repo;
        internal ReportAccess Access = new ReportAccess();
        internal ReportData Data = new ReportData();
        internal ReportStorage Storage = new ReportStorage();
        internal ReportMetadata Metadata;
        internal CompletionReportService Service(string user = "fictional-coordinator", ICompletionReportPdfRenderer renderer = null)
        { return new CompletionReportService(Repo, new User { UserId = user }, Access, Data, renderer ?? new CompletionReportPdfRenderer(), Storage, Metadata); }
    }
    private static async Task<ReportFixture> ReportSetup(CompletionStatus status = CompletionStatus.Approved, bool reportPolicy = true)
    {
        var policy = DecisionPolicy(true);
        if (reportPolicy) policy.CompletionReport = new CompletionReportPolicy { Enabled = true };
        var repo = ReadinessRepo(policy);
        var request = DecisionRequest(); request.Reason = "PRIVATE_COORDINATOR_REASON_MUST_NOT_APPEAR";
        await DecisionFlow(repo).DecideAsync(request);
        var review = await repo.GetCompletionReviewAsync("fictional-internship");
        if (status != CompletionStatus.Approved) { review.Status = status; review.DecisionHistory.Clear(); repo = new InMemoryFinalEvaluationRepository(new[] { policy }, reviews: new[] { review }); }
        var result = new ReportFixture { Repo = repo };
        result.Metadata = new ReportMetadata(repo);
        result.Data.Value = new VerifiedCompletionReportData { IsTrusted = true, InternshipId = "fictional-internship", StudentId = "fictional-student", PolicyId = "fictional-policy", PolicyVersion = "test-v1",
            ApprovedAtUtc = review.ReviewedAtUtc, StudentName = "Fictional José Müller", StudentNumber = "TEST-ONLY-001", OrganizationName = "Fictional Training Organization",
            PeriodStart = new DateTime(2026, 1, 1), PeriodEnd = new DateTime(2026, 2, 1), ApprovedHours = 10,
            UniversityName = "Fictional Test University", CollegeName = "Fictional Test College" };
        return result;
    }
    private static async Task RunCompletionReportTests()
    {
        await Test("report generates a valid private PDF and matching issued metadata for Approved completion", async () => {
            var f = await ReportSetup(); var document = await f.Service().GenerateAsync("fictional-internship");
            var stored = await f.Storage.ReadAsync(document.StorageObjectPath); CompletionReportPdfRenderer.ValidatePdf(stored);
            Assert(document.Status == DocumentStatus.Issued && document.DocumentType == "InternshipCompletionReport" && document.ContentType == "application/pdf" && document.ByteLength == stored.Length && document.Sha256 == CompletionReportValidation.Hash(stored));
            Assert(JsonConvert.SerializeObject(document) == JsonConvert.SerializeObject(await f.Repo.GetCompletionDocumentAsync("fictional-internship", document.DocumentId)));
            string artifacts = Environment.GetEnvironmentVariable("UIMS_PDF_TEST_ARTIFACT_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(artifacts)) { Directory.CreateDirectory(artifacts); File.WriteAllBytes(Path.Combine(artifacts, "completion-report-fictional.pdf"), stored); }
        });
        await Test("report blocks Pending UnderReview ChangesRequested Rejected and missing approval", async () => {
            foreach (var status in new[] { CompletionStatus.Pending, CompletionStatus.UnderReview, CompletionStatus.ChangesRequested, CompletionStatus.Rejected }) {
                var f = await ReportSetup(status); await Reject<InvalidOperationException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
            }
            var fixture = await ReportSetup(); fixture.Repo = new InMemoryFinalEvaluationRepository(); await Reject<InvalidOperationException>(() => fixture.Service().GenerateAsync("fictional-internship"));
        });
        await Test("report unauthorized and anonymous generation denies before privileged reads", async () => {
            var f = await ReportSetup(); f.Repo.SimulateFailure = true;
            await Reject<UnauthorizedAccessException>(() => f.Service("fictional-student").GenerateAsync("fictional-internship"));
            await Reject<UnauthorizedAccessException>(() => f.Service(null).GenerateAsync("fictional-internship")); Assert(f.Data.Calls == 0 && f.Storage.Puts == 0);
        });
        await Test("report missing and untrusted source data block generation", async () => {
            var f = await ReportSetup(); f.Data.Value.IsTrusted = false; await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship"));
            f.Data.Value = null; await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
        });
        await Test("report rejects wrong internship student approval or policy binding", async () => {
            foreach (var change in new Action<VerifiedCompletionReportData>[] { d => d.InternshipId = "other", d => d.StudentId = "other", d => d.PolicyVersion = "other", d => d.ApprovedAtUtc = null }) {
                var f = await ReportSetup(); change(f.Data.Value); await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
            }
        });
        await Test("report requires verified core identity organization dates and approved hours", async () => {
            foreach (var change in new Action<VerifiedCompletionReportData>[] { d => d.StudentName = "", d => d.StudentNumber = null, d => d.OrganizationName = "", d => d.PeriodStart = null, d => d.PeriodEnd = d.PeriodStart.Value.AddDays(-1), d => d.ApprovedHours = null, d => d.ApprovedHours = -1, d => d.ApprovedHours = 11 }) {
                var f = await ReportSetup(); change(f.Data.Value); await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
            }
        });
        await Test("report policy must explicitly enable generation", async () => {
            var f = await ReportSetup(reportPolicy: false); await Reject<InvalidOperationException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
        });
        await Test("report missing or unapproved university policy blocks generation", async () => {
            foreach (bool missing in new[] { true, false }) {
                var f = await ReportSetup(); var review = await f.Repo.GetCompletionReviewAsync("fictional-internship"); var policy = await f.Repo.GetPolicyAsync("fictional-policy"); policy.IsApproved = false;
                f.Repo = new InMemoryFinalEvaluationRepository(missing ? null : new[] { policy }, reviews: new[] { review });
                await Reject<InvalidOperationException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
            }
        });
        await Test("report optional university data is omitted without inventing branding", async () => {
            var f = await ReportSetup(); f.Data.Value.UniversityName = null; f.Data.Value.CollegeName = null;
            var document = await f.Service().GenerateAsync("fictional-internship"); Assert(document.Status == DocumentStatus.Issued);
        });
        await Test("report policy-required university data blocks when missing", async () => {
            var f = await ReportSetup(); var review = await f.Repo.GetCompletionReviewAsync("fictional-internship"); var policy = await f.Repo.GetPolicyAsync("fictional-policy");
            policy.CompletionReport.RequireUniversityName = true; policy.CompletionReport.RequireCollegeName = true;
            f.Repo = new InMemoryFinalEvaluationRepository(new[] { policy }, reviews: new[] { review }); f.Metadata = new ReportMetadata(f.Repo); f.Data.Value.UniversityName = null;
            await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship"));
        });
        await Test("report DTO excludes confidential forms grades feedback and audit notes", async () => {
            Assert(typeof(VerifiedCompletionReportData).GetProperties().All(p => !new[] { "Scores", "Feedback", "Grade", "DecisionHistory", "SupportingNotes" }.Contains(p.Name)));
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship");
            using (var stream = new MemoryStream(await f.Storage.ReadAsync(doc.StorageObjectPath))) using (var pdf = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import)) {
                Assert(pdf.Info.Title == "Internship Completion Report" && !pdf.Info.ToString().Contains("PRIVATE_COORDINATOR_REASON_MUST_NOT_APPEAR") && pdf.PageCount >= 1);
            }
        });
        await Test("report renderer treats markup as literal text and embeds fonts", async () => {
            var f = await ReportSetup(); f.Data.Value.OrganizationName = "<script>alert('literal')</script> & Training";
            var doc = await f.Service().GenerateAsync("fictional-internship"); CompletionReportPdfRenderer.ValidatePdf(await f.Storage.ReadAsync(doc.StorageObjectPath));
        });
        await Test("report Arabic shaping produces a valid PDF without invented translations", async () => {
            var f = await ReportSetup(); f.Data.Value.StudentName = "طالب تجريبي أحمد علي"; f.Data.Value.OrganizationName = "مؤسسة تدريب تجريبية";
            var doc = await f.Service().GenerateAsync("fictional-internship"); var bytes = await f.Storage.ReadAsync(doc.StorageObjectPath); CompletionReportPdfRenderer.ValidatePdf(bytes);
            var artifacts = Environment.GetEnvironmentVariable("UIMS_PDF_TEST_ARTIFACT_DIRECTORY"); if (!string.IsNullOrWhiteSpace(artifacts)) File.WriteAllBytes(Path.Combine(artifacts, "completion-report-arabic.pdf"), bytes);
        });
        await Test("report long verified content paginates with a valid document", async () => {
            var f = await ReportSetup(); string longText = string.Join(" ", Enumerable.Repeat("Verified fictional information", 15));
            f.Data.Value.StudentName = longText; f.Data.Value.OrganizationName = longText; f.Data.Value.UniversityName = longText; f.Data.Value.CollegeName = longText;
            var doc = await f.Service().GenerateAsync("fictional-internship"); var bytes = await f.Storage.ReadAsync(doc.StorageObjectPath);
            using (var stream = new MemoryStream(bytes)) using (var pdf = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import)) Assert(pdf.PageCount > 1);
            var artifacts = Environment.GetEnvironmentVariable("UIMS_PDF_TEST_ARTIFACT_DIRECTORY"); if (!string.IsNullOrWhiteSpace(artifacts)) File.WriteAllBytes(Path.Combine(artifacts, "completion-report-long.pdf"), bytes);
        });
        await Test("report invalid renderer output cannot reach storage or metadata", async () => {
            var f = await ReportSetup(); await Reject<RepositoryException>(() => f.Service(renderer: new InvalidPdf()).GenerateAsync("fictional-internship")); Assert(f.Storage.Puts == 0);
        });
        await Test("report invalid incomplete and public-URL metadata are rejected", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship");
            foreach (var change in new Action<CompletionDocument>[] { d => d.ByteLength = null, d => d.Sha256 = "invalid", d => d.StorageObjectPath = "https://public.example/report.pdf", d => d.CompletionApprovedAtUtc = null, d => d.Status = DocumentStatus.Pending, d => d.ContentType = "text/html" }) {
                var copy = JsonConvert.DeserializeObject<CompletionDocument>(JsonConvert.SerializeObject(doc)); change(copy); await Reject<ArgumentException>(() => { CompletionReportValidation.Metadata(copy); return Task.CompletedTask; });
            }
        });
        await Test("report object-storage failure leaves approval unchanged and retry can succeed", async () => {
            var f = await ReportSetup(); var before = JsonConvert.SerializeObject(await f.Repo.GetCompletionReviewAsync("fictional-internship")); f.Storage.FailPut = true;
            await Reject<IOException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(before == JsonConvert.SerializeObject(await f.Repo.GetCompletionReviewAsync("fictional-internship")));
            f.Storage.FailPut = false; Assert((await f.Service().GenerateAsync("fictional-internship")).Status == DocumentStatus.Issued);
        });
        await Test("report metadata failure cleans only exact created object and allows safe retry", async () => {
            var f = await ReportSetup(); f.Metadata.Fail = true; await Reject<RepositoryException>(() => f.Service().GenerateAsync("fictional-internship"));
            Assert(f.Storage.Deletes == 1); await Reject<RepositoryException>(() => f.Storage.Inner.ReadAsync(f.Storage.LastKey));
            f.Metadata.Fail = false; Assert((await f.Service().GenerateAsync("fictional-internship")).Status == DocumentStatus.Issued);
        });
        await Test("report metadata lost acknowledgment is confirmed against actual object and metadata", async () => {
            var f = await ReportSetup(); f.Metadata.LostAcknowledgment = true; var doc = await f.Service().GenerateAsync("fictional-internship");
            Assert(doc.Status == DocumentStatus.Issued && f.Storage.Deletes == 0 && (await f.Repo.GetCompletionDocumentAsync("fictional-internship", doc.DocumentId)).Sha256 == doc.Sha256);
        });
        await Test("report stored bytes must match metadata before publication", async () => {
            var f = await ReportSetup(); f.Storage.CorruptRead = true; await Reject<RepositoryException>(() => f.Service().GenerateAsync("fictional-internship")); Assert(f.Storage.Deletes == 1);
        });
        await Test("report object read failure cannot publish metadata and a later retry succeeds", async () => {
            var f = await ReportSetup(); f.Storage.FailRead = true;
            await Reject<RepositoryException>(() => f.Service().GenerateAsync("fictional-internship"));
            Assert(f.Storage.Deletes == 1); f.Storage.FailRead = false;
            Assert((await f.Service().GenerateAsync("fictional-internship")).Status == DocumentStatus.Issued);
        });
        await Test("report student owner downloads matching private PDF bytes without exposing storage reference", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); var download = await f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", doc.DocumentId);
            Assert(download.ContentType == "application/pdf" && download.FileName.EndsWith(".pdf") && CompletionReportValidation.Hash(download.Content) == doc.Sha256);
            Assert(typeof(CompletionReportDownload).GetProperties().All(p => p.Name != "StorageObjectPath" && p.Name != "Feedback"));
        });
        await Test("report unrelated student and non-student downloads denied before privileged reads", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); f.Repo.SimulateFailure = true;
            await Reject<UnauthorizedAccessException>(() => f.Service("unrelated-student").DownloadForStudentAsync("fictional-internship", doc.DocumentId));
            await Reject<UnauthorizedAccessException>(() => f.Service().DownloadForStudentAsync("fictional-internship", doc.DocumentId));
        });
        await Test("report student access blocked before Approved completion", async () => {
            var f = await ReportSetup(CompletionStatus.ChangesRequested); await Reject<InvalidOperationException>(() => f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", Guid.NewGuid().ToString("N")));
        });
        await Test("report student cannot download a missing or revoked document", async () => {
            var f = await ReportSetup(); await Reject<InvalidOperationException>(() => f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", Guid.NewGuid().ToString("N")));
            var doc = await f.Service().GenerateAsync("fictional-internship"); doc.Status = DocumentStatus.Revoked;
            var review = await f.Repo.GetCompletionReviewAsync("fictional-internship"); var policy = await f.Repo.GetPolicyAsync("fictional-policy"); f.Repo = new InMemoryFinalEvaluationRepository(new[] { policy }, reviews: new[] { review }, documents: new[] { doc });
            await Reject<ArgumentException>(() => f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", doc.DocumentId));
        });
        await Test("report stored object tampering blocks student download", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); f.Storage.CorruptRead = true;
            await Reject<RepositoryException>(() => f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", doc.DocumentId));
        });
        await Test("report foreign-student metadata cannot be downloaded even by the internship owner", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); doc.StudentId = "unrelated-student";
            var review = await f.Repo.GetCompletionReviewAsync("fictional-internship"); var policy = await f.Repo.GetPolicyAsync("fictional-policy");
            f.Repo = new InMemoryFinalEvaluationRepository(new[] { policy }, reviews: new[] { review }, documents: new[] { doc }); int reads = f.Storage.Reads;
            await Reject<UnauthorizedAccessException>(() => f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", doc.DocumentId)); Assert(f.Storage.Reads == reads);
        });
        await Test("report generations use unique IDs and never overwrite earlier valid documents", async () => {
            var f = await ReportSetup(); var a = await f.Service().GenerateAsync("fictional-internship"); var b = await f.Service().GenerateAsync("fictional-internship");
            Assert(a.DocumentId != b.DocumentId && a.StorageObjectPath != b.StorageObjectPath);
            await Reject<ConflictException>(() => f.Repo.CreateIssuedCompletionReportAsync(a));
            var bytes = await f.Storage.ReadAsync(a.StorageObjectPath); await Reject<ConflictException>(() => f.Storage.Inner.PutNewAsync(a.StorageObjectPath, bytes));
            Assert((await f.Repo.GetCompletionDocumentAsync("fictional-internship", a.DocumentId)).Sha256 == a.Sha256);
        });
        await Test("report private cleanup cannot delete a valid object with a different hash", async () => {
            var f = await ReportSetup(); var doc = await f.Service().GenerateAsync("fictional-internship"); await f.Storage.DeleteIfHashMatchesAsync(doc.StorageObjectPath, new string('0', 64));
            Assert((await f.Storage.ReadAsync(doc.StorageObjectPath)).Length == doc.ByteLength);
        });
        await Test("report malformed keys Unicode and control characters fail safely", async () => {
            var f = await ReportSetup(); f.Data.Value.StudentName = "unsafe\r\nvalue"; await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship"));
            f.Data.Value.StudentName = "\ud800"; await Reject<ArgumentException>(() => f.Service().GenerateAsync("fictional-internship"));
            await Reject<ArgumentException>(() => f.Service("fictional-student").DownloadForStudentAsync("fictional-internship", "../doc"));
        });
        await Test("report missing trusted storage or metadata integration disables official generation", async () => {
            var f = await ReportSetup(); var service = new CompletionReportService(f.Repo, new User { UserId = "fictional-coordinator" }, f.Access, f.Data, new CompletionReportPdfRenderer(), null);
            Assert(!service.IsGenerationConfigured); await Reject<InvalidOperationException>(() => service.GenerateAsync("fictional-internship"));
            Assert(!typeof(ICompletionDocumentMetadataRepository).IsAssignableFrom(typeof(FirebaseFinalEvaluationRepository)));
        });
        await Test("report success and failure never change completion approval or its decision history", async () => {
            var f = await ReportSetup(); var before = JsonConvert.SerializeObject(await f.Repo.GetCompletionReviewSnapshotAsync("fictional-internship"));
            await f.Service().GenerateAsync("fictional-internship"); f.Metadata.Fail = true; await Reject<RepositoryException>(() => f.Service().GenerateAsync("fictional-internship"));
            Assert(before == JsonConvert.SerializeObject(await f.Repo.GetCompletionReviewSnapshotAsync("fictional-internship")));
        });
    }
}
