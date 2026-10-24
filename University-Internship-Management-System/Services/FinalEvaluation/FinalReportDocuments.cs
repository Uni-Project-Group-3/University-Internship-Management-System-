using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
namespace UIMS.Services
{
    public sealed partial class FinalReportWorkflow
    {
        public FirebaseFileStorageService ReportStorage { private get; set; }
        private FirebaseFileStorageService DocumentStorage { get { return ReportStorage ?? (ReportStorage = new FirebaseFileStorageService(k => System.Configuration.ConfigurationManager.AppSettings[k])); } }
        private JObject DocumentSource(JObject record, string revisionId)
        {
            var source = revisionId == "draft" ? record["draft"] as JObject : record["revisions"]?[Key(revisionId)] as JObject;
            if (source == null) throw new FirebaseException(404, "Save the report before generating its PDF.");
            return source;
        }
        public async Task<JObject> GenerateDocumentAsync(string uid, string role, string placementId, string revisionId)
        {
            var access = await Authorize(uid, role, Key(placementId));
            if (access.Role != "companySupervisor" && access.Role != "academicSupervisor" && access.Role != "coordinator") throw new FirebaseException(403, "Only assigned report supervisors may generate this private PDF.");
            // Apply the same state restrictions as the detail endpoint, including coordinator access.
            await ReadAsync(uid, role, placementId);
            var node = await db.ReadVersionAsync(Path(placementId)); var record = O(node.Value); var source = DocumentSource(record, revisionId);
            if (revisionId == "draft" && access.Role != "companySupervisor") throw new FirebaseException(403, "Only the assigned company supervisor may access the draft document.");
            var evidence = (JObject)source.DeepClone(); evidence.Remove("attachment"); string digest = Hash(new JObject { ["layout"] = "one-page-v1", ["evidence"] = evidence });
            var existing = source["attachment"] as JObject;
            if (T(existing, "uploadStatus") == "ready" && T(existing, "sourceHash") == digest) return await ReadAsync(uid, role, placementId);
            byte[] bytes = RenderFinalPdf(access, source, revisionId);
            string objectPath = "uims/final-reports/" + placementId + "/" + Guid.NewGuid().ToString("N") + ".pdf";
            var stored = await DocumentStorage.UploadAsync(objectPath, "final-evaluation-" + access.Student + ".pdf", "application/pdf", bytes, CancellationToken.None);
            source["attachment"] = new JObject { ["fileName"] = stored.FileName, ["contentType"] = stored.ContentType, ["storageProvider"] = stored.Provider, ["storagePath"] = stored.ObjectPath, ["sizeBytes"] = stored.SizeBytes, ["sha256"] = stored.Sha256, ["sourceHash"] = digest, ["uploadStatus"] = "ready", ["generatedAt"] = Now(), ["generatedBy"] = uid };
            // Never overwrite a concurrent review/save. An uncertain upload remains private rather than deleting a possibly committed file.
            await Authorize(uid, role, placementId);
            bool committed;
            try { committed = await db.CompareWriteAsync(Path(placementId), record, node.ETag); }
            catch { var persisted = O(await db.ReadAsync(Path(placementId))); var persistedSource = DocumentSource(persisted, revisionId); if (T(persistedSource["attachment"], "storagePath") == stored.ObjectPath) return await ReadAsync(uid, role, placementId); throw; }
            if (!committed) throw new FirebaseException(409, "Report changed while generating its PDF. Refresh and generate again; the previous document remains intact.");
            return await ReadAsync(uid, role, placementId);
        }
        public async Task<UIMS.Tasks.TaskFileDownload> DownloadDocumentAsync(string uid, string role, string placementId, string revisionId)
        {
            var access = await Authorize(uid, role, Key(placementId));
            if (access.Role == "student") throw new FirebaseException(403, "Assessment details are private to the assigned supervisors.");
            await ReadAsync(uid, role, placementId);
            if (revisionId == "draft" && access.Role != "companySupervisor") throw new FirebaseException(403, "Draft document access is restricted to its company supervisor.");
            var record = O(await db.ReadAsync(Path(placementId))); var source = DocumentSource(record, revisionId); var attachment = O(source["attachment"]);
            string objectPath = T(attachment, "storagePath");
            if (T(attachment, "uploadStatus") != "ready" || T(attachment, "contentType") != "application/pdf" || !objectPath.StartsWith("uims/final-reports/" + placementId + "/", StringComparison.Ordinal)) throw new FirebaseException(404, "Generate this report PDF first.");
            // A draft becomes an immutable revision on submission. Submission timestamps alone must not invalidate its existing PDF.
            byte[] bytes = await DocumentStorage.DownloadAsync(objectPath, T(attachment, "storageProvider"), CancellationToken.None);
            if (bytes.LongLength != (long?)attachment["sizeBytes"] || University_Internship_Management_System.Services.FinalEvaluation.CompletionReportValidation.Hash(bytes) != T(attachment, "sha256")) throw new FirebaseException(409, "The PDF failed integrity verification.");
            University_Internship_Management_System.Services.FinalEvaluation.CompletionReportPdfRenderer.ValidatePdf(bytes);
            await Authorize(uid, role, placementId);
            return new UIMS.Tasks.TaskFileDownload { FileName = T(attachment, "fileName"), ContentType = "application/pdf", Content = bytes };
        }
        private byte[] RenderFinalPdf(Access access, JObject source, string revisionId)
        {
            var doc = new Document(); doc.Info.Title = "Final Internship Evaluation"; doc.Info.Author = "JUST Internship Management System";
            doc.Styles[StyleNames.Normal].Font.Name = "Segoe UI"; doc.Styles[StyleNames.Normal].Font.Size = 10;
            doc.Styles[StyleNames.Normal].ParagraphFormat.SpaceAfter = 6;
            var section = doc.AddSection(); section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(2);
            string logo = System.Web.Hosting.HostingEnvironment.MapPath("~/Assets/shared/images/just-logo.png"); if (logo == null) logo = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets/shared/images/just-logo.png");
            if (File.Exists(logo)) { var image=section.AddImage("base64:"+Convert.ToBase64String(File.ReadAllBytes(logo)));image.Width=Unit.FromCentimeter(1.4);image.LockAspectRatio=true; }
            var title=section.AddParagraph("Final Internship Evaluation");title.Format.Font.Size=23;title.Format.Font.Bold=true;title.Format.Font.Color=Color.Parse("#1F3D78");
            void TextLine(string text) { if (text.Any(c => c >= '\u0590' && c <= '\u109f' || c >= '\ufb1d' && c <= '\ufeff')) { var image=section.AddImage("base64:"+Convert.ToBase64String(UIMS.Tasks.WeeklyReportService.ShapeReportText(text,17)));image.Width=Unit.FromCentimeter(16.5);image.LockAspectRatio=true; } else section.AddParagraph(text); }
            TextLine("Student: " + (string.IsNullOrWhiteSpace(access.StudentName) ? "Name unavailable" : access.StudentName.Length > 80 ? access.StudentName.Substring(0,77)+"..." : access.StudentName) + " | Student ID: " + access.Student);
            TextLine("Academic supervisor: " + (string.IsNullOrWhiteSpace(access.AcademicName) ? "Name unavailable" : access.AcademicName));
            section.AddParagraph("Generated: " + TimeZoneInfo.ConvertTime(clock(),TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time")).ToString("yyyy-MM-dd HH:mm:ss zzz") + " Asia/Amman");
            section.AddParagraph(revisionId == "draft" ? "Draft assessment snapshot - not final approval" : "Submitted revision assessment snapshot - approval is recorded separately in UIMS");
            var score=section.AddParagraph((source["gradeCalculation"] == null ? "Saved company assessment: " : "Calculated company assessment: ") + T(source,"percentage") + " / 100");score.Format.Font.Size=17;score.Format.Font.Bold=true;
            var calculation=O(source["gradeCalculation"]); section.AddParagraph(T(calculation,"explanation"));
            var snapshot=O(source["sourceSnapshot"]);var tasks=snapshot["tasks"] as JArray??new JArray();var exams=snapshot["exams"] as JArray??new JArray();var weekly=source["weeklyReports"] as JArray??new JArray();
            string Short(string value,int max=90){return value.Length<=max?value:value.Substring(0,max-3)+"...";}
            void Heading(string text){var p=section.AddParagraph(text);p.Format.Font.Bold=true;p.Format.Font.Size=12;p.Format.Font.Color=Color.Parse("#1F3D78");p.Format.SpaceBefore=10;p.Format.KeepWithNext=true;}
            void Summary(string label,string value){var p=section.AddParagraph();p.AddFormattedText(label+": ",TextFormat.Bold);p.AddText(value);}
            decimal approvedMinutes;bool known=decimal.TryParse(T(snapshot,"approvedTrainingMinutes"),out approvedMinutes);
            Heading("Training overview");
            TextLine("Company: "+(T(snapshot,"companyName").Length==0?"Name unavailable":Short(T(snapshot,"companyName"))));
            Summary("Approved hours",known?(approvedMinutes/60m).ToString("0.00")+" / "+(RequiredMinutes/60)+" hours":"Unverified");
            Summary("Remaining hours",known?(Math.Max(0,RequiredMinutes-approvedMinutes)/60m).ToString("0.00")+" hours":"Unverified");
            Summary("Hours progress",known?Math.Min(100,approvedMinutes/RequiredMinutes*100m).ToString("0.0")+"%":"Unverified");
            Heading("Recorded achievement");
            var grid=section.AddTable();grid.Borders.Width=.3;grid.Borders.Color=Color.Parse("#D8DEE8");grid.TopPadding=grid.BottomPadding=Unit.FromPoint(6);grid.AddColumn(Unit.FromCentimeter(8));grid.AddColumn(Unit.FromCentimeter(4));grid.AddColumn(Unit.FromCentimeter(5));
            var header=grid.AddRow();header.Shading.Color=Color.Parse("#1F3D78");header.Format.Font.Color=Colors.White;header.Cells[0].AddParagraph("Evidence");header.Cells[1].AddParagraph("Available");header.Cells[2].AddParagraph("Completed / released");
            void Row(string label,int total,int done){var r=grid.AddRow();r.Cells[0].AddParagraph(label);r.Cells[1].AddParagraph(total.ToString());r.Cells[2].AddParagraph(done.ToString());}
            Row("Assigned tasks",tasks.Count,tasks.Count(t=>T(t,"status").Equals("completed",StringComparison.OrdinalIgnoreCase)));
            Row("Submitted exam attempts",exams.Count,exams.Count(e=>T(e,"resultStatus")=="released"));
            Row("Weekly reports",weekly.Count,weekly.Count(w=>T(w,"status")=="approved"));
            Heading("Assessment calculation");
            var items=calculation["items"] as JArray??new JArray();
            foreach(string kind in new[]{"task","exam"}){var grades=items.Where(x=>T(x,"kind")==kind).ToArray();Summary(kind=="task"?"Graded ordinary tasks":"Released examinations",grades.Length==0?"No eligible marks":grades.Length+" item(s), mean "+grades.Average(x=>(decimal)x["percentage"]).ToString("0.00")+" / 100");}
            Summary("Included / excluded",items.Count+" / "+(T(calculation,"excludedCount").Length==0?"Not recorded":T(calculation,"excludedCount")));
            section.AddParagraph("Equal average of eligible assessments normalized to 100. Exam-linked tasks count once. Missing or unreleased results are excluded, not zero.");
            Heading("Supervisor review");
            if(!string.IsNullOrWhiteSpace(T(source,"companyResponse")))TextLine(Short(T(source,"companyResponse"),200));
            section.AddParagraph("Academic and administrative decisions remain recorded in UIMS. This assessment summary does not certify training completion.");
            var note=section.AddParagraph("One-page summary. Full task records, individual marks and immutable revisions remain available to authorized supervisors in UIMS.");note.Format.Font.Size=8;note.Format.Font.Color=Colors.DimGray;
            var footer=section.Footers.Primary.AddParagraph("JUST | Confidential supervisor assessment | Page ");footer.AddPageField();footer.AddText(" of ");footer.AddNumPagesField();footer.Format.Font.Size=8;
            var renderer=new PdfDocumentRenderer {Document=doc};renderer.RenderDocument();using(var output=new MemoryStream()){renderer.PdfDocument.Save(output,false);var bytes=output.ToArray();University_Internship_Management_System.Services.FinalEvaluation.CompletionReportPdfRenderer.ValidatePdf(bytes);return bytes;}
        }
    }
}
