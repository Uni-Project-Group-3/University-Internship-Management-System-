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
        // Injectable delivery boundary for isolated tests. SMTP is used only by the live API.
        public Func<string, byte[], string, Task> CertificateSender { private get; set; }
        public Func<string, string> CertificateSettings { private get; set; }
        private string MailSetting(string name)
        {
            if (CertificateSettings != null) return CertificateSettings(name);
            // Visual Studio/IIS Express can retain the environment from before Windows user settings were saved.
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) : value;
        }
        public async Task<UIMS.Tasks.TaskFileDownload> DownloadCertificateAsync(string uid, string role, string placementId)
        {
            var access = await Authorize(uid, role, Key(placementId));
            var view = await ReadAsync(uid, role, placementId);
            if (T(view, "status") != "completed") throw new FirebaseException(409, "A completion certificate is available only after final administration approval and completed finalization.");
            var record = O(await db.ReadAsync(Path(placementId))); var outcome = O(record["outcome"]);
            var certificate = O(outcome["certificate"]);
            if (!certificate.HasValues) throw new FirebaseException(404, "This historical completion does not contain an issued certificate.");
            var doc = new Document(); doc.Info.Title = "Field Training Completion Certificate";
            doc.Info.Author = "JUST Internship Management System";
            doc.Styles[StyleNames.Normal].Font.Name = "Segoe UI"; doc.Styles[StyleNames.Normal].Font.Size = 12;
            // Landscape certificate: navy frame, centered content, gold accents and signature lines.
            var section = doc.AddSection(); section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.TopMargin = section.PageSetup.BottomMargin = Unit.FromCentimeter(1.2);
            section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(1.4);
            Color navy = Color.Parse("#1F3D78"), gold = Color.Parse("#B8892D"), muted = Color.Parse("#5E6C84");
            var frame = section.AddTable(); frame.Borders.Color = navy; frame.Borders.Width = Unit.FromPoint(3);
            frame.TopPadding = frame.BottomPadding = Unit.FromCentimeter(0.6); frame.LeftPadding = frame.RightPadding = Unit.FromCentimeter(1.2);
            frame.AddColumn(Unit.FromCentimeter(26.9));
            var frameRow = frame.AddRow(); frameRow.Height = Unit.FromCentimeter(18.4); frameRow.HeightRule = MigraDoc.DocumentObjectModel.Tables.RowHeightRule.Exactly;
            frameRow.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
            var cell = frameRow.Cells[0];
            Paragraph Put(string text, double size, bool bold, Color color, double spaceAfter) {
                var paragraph = cell.AddParagraph(); paragraph.Format.Alignment = ParagraphAlignment.Center; paragraph.Format.SpaceAfter = Unit.FromPoint(spaceAfter);
                // Arabic and other RTL text is shaped into an image by the existing report shaper, scaled to the requested size.
                if (text.Any(c => c >= '\u0590' && c <= '\u109f' || c >= '\ufb1d' && c <= '\ufeff')) { var shaped = paragraph.AddImage("base64:" + Convert.ToBase64String(UIMS.Tasks.WeeklyReportService.ShapeReportText(text, .45 + 16 * 10.5 / size))); shaped.Width = Unit.FromCentimeter(14); shaped.LockAspectRatio = true; return paragraph; }
                var run = paragraph.AddFormattedText(text); run.Font.Size = size; run.Font.Bold = bold; run.Font.Color = color; return paragraph;
            }
            Paragraph Columns(string[] values, double size, bool bold, Color color, double spaceAfter, double[] stops) {
                var paragraph = cell.AddParagraph(); paragraph.Format.SpaceAfter = Unit.FromPoint(spaceAfter); paragraph.Format.Font.Size = size; paragraph.Format.Font.Bold = bold; paragraph.Format.Font.Color = color;
                foreach (double stop in stops) paragraph.Format.AddTabStop(Unit.FromCentimeter(stop), TabAlignment.Center);
                for (int i = 0; i < values.Length; i++) { paragraph.AddTab(); paragraph.AddText(values[i]); }
                return paragraph;
            }
            var logo = System.Web.Hosting.HostingEnvironment.MapPath("~/Assets/shared/images/JUST-Logo-01.png") ?? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets/shared/images/JUST-Logo-01.png");
            if (File.Exists(logo)) { var logoLine = cell.AddParagraph(); logoLine.Format.Alignment = ParagraphAlignment.Center; logoLine.Format.SpaceAfter = Unit.FromPoint(6); var image = logoLine.AddImage("base64:" + Convert.ToBase64String(File.ReadAllBytes(logo))); image.Width = Unit.FromCentimeter(2.2); image.LockAspectRatio = true; }
            Put("JORDAN UNIVERSITY OF SCIENCE AND TECHNOLOGY", 11, true, navy, 2);
            Put("University Internship Management System", 9, false, muted, 14);
            Put("CERTIFICATE OF COMPLETION", 30, true, navy, 2);
            Put("F I E L D   T R A I N I N G", 12, true, gold, 16);
            Put("This is to certify that", 12, false, muted, 6);
            Put(T(certificate,"studentName").Length>0?T(certificate,"studentName"):string.IsNullOrWhiteSpace(access.StudentName)?access.Student:access.StudentName, 26, true, navy, 4);
            Put("Student ID: " + access.Student, 11, false, muted, 10);
            Put("has successfully completed the approved field training requirements" + (T(certificate,"companyName").Length > 0 ? " at " + T(certificate,"companyName") : "") + ".", 12, false, navy, 12);
            var divider = cell.AddParagraph(); divider.Format.LeftIndent = Unit.FromCentimeter(8); divider.Format.RightIndent = Unit.FromCentimeter(8); divider.Format.SpaceAfter = Unit.FromPoint(12);
            divider.Format.Borders.Bottom.Width = Unit.FromPoint(1.2); divider.Format.Borders.Bottom.Color = gold;
            DateTimeOffset date; string issued = DateTimeOffset.TryParse(T(outcome,"approvedAt"), out date) ? date.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : "";
            string hours = ((decimal)record["finalization"]["approvedTrainingMinutes"]/60m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " hours";
            double[] three = { 4.1, 12.25, 20.4 };
            Columns(new[] { "APPROVED TRAINING", "FINAL GRADE", "ISSUED ON" }, 8, true, muted, 2, three);
            Columns(new[] { hours, T(outcome,"finalGrade") + " / 100", issued }, 13, true, navy, 26, three);
            double[] two = { 6.5, 18 };
            Columns(new[] { "____________________________", "____________________________" }, 10, false, muted, 2, two);
            Columns(new[] { "Academic Supervisor", "Internship Coordinator" }, 10, true, navy, 8, two);
            Put("Approved through academic review and final administration approval.", 8.5, false, muted, 0);
            var footer=section.Footers.Primary.AddParagraph("UIMS verification reference: "+placementId+" / "+T(outcome,"requestId"));footer.Format.Font.Size=7;footer.Format.Font.Color=muted;footer.Format.Alignment=ParagraphAlignment.Center;
            var renderer=new PdfDocumentRenderer{Document=doc};renderer.RenderDocument();byte[] bytes;
            using(var stream=new MemoryStream()){renderer.PdfDocument.Save(stream,false);bytes=stream.ToArray();}
            University_Internship_Management_System.Services.FinalEvaluation.CompletionReportPdfRenderer.ValidatePdf(bytes);
            await Authorize(uid,role,placementId);
            return new UIMS.Tasks.TaskFileDownload{FileName=T(certificate,"fileName"),ContentType="application/pdf",Content=bytes};
        }

        public async Task<JObject> SendCertificateAsync(string uid,string role,string placementId)
        {
            var access=await Authorize(uid,role,Key(placementId));
            if(access.Role!="coordinator")throw new FirebaseException(403,"Only administration may send the completion certificate.");
            var file=await DownloadCertificateAsync(uid,role,placementId);
            var node=await db.ReadVersionAsync(Path(placementId));var record=O(node.Value);var previous=O(record["certificateEmail"]);
            if(new[]{"sent","sending","deliveryUnconfirmed"}.Contains(T(previous,"status")))return previous;
            string recipient=access.StudentEmail;
            var missing=new[]{"UIMS_SMTP_HOST","UIMS_SMTP_FROM","UIMS_SMTP_USERNAME","UIMS_SMTP_PASSWORD"}.Where(k=>string.IsNullOrWhiteSpace(MailSetting(k))).ToArray();
            bool configured=CertificateSender!=null||missing.Length==0;
            System.Net.Mail.MailAddress address=null;try{address=new System.Net.Mail.MailAddress(recipient);}catch(FormatException){}catch(ArgumentException){}
            int port;bool validPort=int.TryParse(MailSetting("UIMS_SMTP_PORT")??"587",out port)&&port>0&&port<=65535;
            bool valid=address!=null&&!address.Host.EndsWith(".invalid",StringComparison.OrdinalIgnoreCase)&&configured&&validPort;
            string blocker=address==null?"The student's saved profile email is missing or invalid. Update the student's email in Manage Students.":address.Host.EndsWith(".invalid",StringComparison.OrdinalIgnoreCase)?"The student's saved profile still uses an @example.invalid address. Update that student's email in Manage Students; changing the sender address does not change the recipient.":!configured?"The web server cannot read these SMTP settings: "+string.Join(", ",missing)+". Save them under the Windows account running the site.":!validPort?"UIMS_SMTP_PORT must be a valid port number, for example 587.":"";
            var delivery=new JObject{["status"]=valid?"sending":"blocked",["requestId"]=record["finalization"]?["requestId"],["updatedAt"]=Now(),["actorUid"]=uid,["message"]=valid?"Certificate email delivery started.":blocker};
            record["certificateEmail"]=delivery;
            // Claim once before external delivery. Uncertain SMTP responses never cause automatic resend.
            if(!await db.CompareWriteAsync(Path(placementId),record,node.ETag))throw new FirebaseException(409,"Certificate delivery changed. Reload its persisted status.");
            if(!valid)return delivery;
            try {
                await Authorize(uid,role,placementId);
                if(CertificateSender!=null)await CertificateSender(recipient,file.Content,file.FileName);
                else using(var mail=new System.Net.Mail.MailMessage())using(var smtp=new System.Net.Mail.SmtpClient(MailSetting("UIMS_SMTP_HOST").Trim(),port)){
                    mail.From=new System.Net.Mail.MailAddress(MailSetting("UIMS_SMTP_FROM").Trim(),"JUST Internship Coordination");mail.To.Add(address);
                    mail.Subject="Your field training completion certificate";mail.Body="Congratulations! You have successfully completed your field training. Your completion certificate is attached and is also available in your UIMS account.";
                    mail.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(file.Content),file.FileName,"application/pdf"));
                    string password=MailSetting("UIMS_SMTP_PASSWORD");if(MailSetting("UIMS_SMTP_HOST").Trim().Equals("smtp.gmail.com",StringComparison.OrdinalIgnoreCase))password=string.Concat(password.Where(c=>!char.IsWhiteSpace(c)));
                    smtp.UseDefaultCredentials=false;smtp.EnableSsl=true;smtp.Credentials=new System.Net.NetworkCredential(MailSetting("UIMS_SMTP_USERNAME").Trim(),password);
                    var sending=smtp.SendMailAsync(mail);if(await Task.WhenAny(sending,Task.Delay(TimeSpan.FromSeconds(30)))!=sending){var observation=sending.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);throw new TimeoutException();}await sending;
                }
                delivery["status"]="sent";delivery["message"]="Certificate email accepted by the mail server. Inbox delivery is not guaranteed.";
            }catch(System.Net.Mail.SmtpException smtpError) when((int)smtpError.StatusCode>=400&&(int)smtpError.StatusCode<=599){delivery["status"]="rejected";delivery["message"]="The mail server rejected delivery (SMTP "+(int)smtpError.StatusCode+"). Verify the sender account, its current app password and the student's saved recipient email, then retry.";}
            catch(Exception){delivery["status"]="deliveryUnconfirmed";delivery["message"]="Email delivery could not be confirmed. Check the mail server before resending; no automatic resend was attempted.";}
            delivery["updatedAt"]=Now();
            var latest=await db.ReadVersionAsync(Path(placementId));var saved=O(latest.Value);
            if(T(saved["certificateEmail"],"status")!="sending")throw new FirebaseException(409,"Email delivery status changed; reload before retrying.");
            saved["certificateEmail"]=delivery;
            if(!await db.CompareWriteAsync(Path(placementId),saved,latest.ETag))throw new FirebaseException(409,"Email status could not be acknowledged. Check delivery before retrying.");
            return delivery;
        }
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
