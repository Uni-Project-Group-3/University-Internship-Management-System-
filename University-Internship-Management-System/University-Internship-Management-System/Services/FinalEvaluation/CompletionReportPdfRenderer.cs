using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Pdf.IO;
using DColor = System.Drawing.Color;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Windows GDI build, pinned PDFsharp/MigraDoc 6.2.4 (MIT). No HTML parser, URLs,
    // external resources, script execution, branding, signatures or certificate claims.
    public sealed class CompletionReportPdfRenderer : ICompletionReportPdfRenderer
    {
        private const string FontName = "Segoe UI";
        public byte[] Render(VerifiedCompletionReportData data, string documentId, DateTimeOffset generatedAtUtc)
        {
            if (data == null) throw new ArgumentNullException("data");
            CompletionReportValidation.DocumentId(documentId);
            using (var family = new FontFamily(FontName)) { if (family.Name != FontName) throw new InvalidOperationException("The configured report font is unavailable."); }
            var document = new Document();
            document.Info.Title = "Internship Completion Report";
            document.Info.Subject = "Approved internship completion record";
            var normal = document.Styles[StyleNames.Normal]; normal.Font.Name = FontName; normal.Font.Size = 10.5;
            normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
            var section = document.AddSection(); section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(2.2);
            section.PageSetup.TopMargin = Unit.FromCentimeter(2); section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
            var eyebrow = section.AddParagraph("COMPLETION RECORD"); eyebrow.Format.Font.Size = 9; eyebrow.Format.Font.Color = Colors.DimGray; eyebrow.Format.SpaceAfter = 10;
            var title = section.AddParagraph("Internship Completion Report"); title.Format.Font.Size = 22; title.Format.Font.Bold = true; title.Format.SpaceAfter = 10;
            section.AddParagraph("Summary of the approved internship completion record.");
            var identifier = section.AddParagraph("Report ID: " + documentId + "\nGenerated (UTC): " + generatedAtUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
            identifier.Format.Font.Size = 9; identifier.Format.Font.Color = Colors.DimGray; identifier.Format.SpaceAfter = 18;
            var table = section.AddTable(); table.Borders.Width = 0; table.AddColumn(Unit.FromCentimeter(4.2)); table.AddColumn(Unit.FromCentimeter(12.4));
            var header = table.AddRow(); header.HeadingFormat = true; header.Shading.Color = Colors.LightGray;
            header.Cells[0].AddParagraph("Field"); header.Cells[1].AddParagraph("Verified information"); header.Format.Font.Bold = true;
            Add(table, "Student name", data.StudentName); Add(table, "Student number", data.StudentNumber);
            Add(table, "Internship ID", data.InternshipId); Add(table, "Organization", data.OrganizationName);
            Add(table, "Training period", data.PeriodStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " to " + data.PeriodEnd.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Add(table, "Approved hours", data.ApprovedHours.Value.ToString("0.############################", CultureInfo.InvariantCulture));
            Add(table, "Completion status", "Approved"); Add(table, "Approved on (UTC)", data.ApprovedAtUtc.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(data.UniversityName)) Add(table, "University", data.UniversityName);
            if (!string.IsNullOrWhiteSpace(data.CollegeName)) Add(table, "College", data.CollegeName);
            Add(table, "Policy reference", data.PolicyId + " / " + data.PolicyVersion);
            var footer = section.Footers.Primary.AddParagraph("Generated from verified completion data.   |   Page ");
            footer.AddPageField(); footer.AddText(" of "); footer.AddNumPagesField(); footer.Format.Font.Size = 8; footer.Format.Font.Color = Colors.DimGray;
            var pdf = new PdfDocumentRenderer { Document = document }; pdf.RenderDocument();
            using (var stream = new MemoryStream()) { pdf.PdfDocument.Save(stream, false); var bytes = stream.ToArray(); ValidatePdf(bytes); return bytes; }
        }
        private static void Add(Table table, string label, string text)
        {
            CompletionReportValidation.Text(text, label, true);
            var row = table.AddRow(); row.TopPadding = row.BottomPadding = Unit.FromPoint(9);
            row.Borders.Bottom.Width = 0.4; row.Borders.Bottom.Color = Colors.LightGray;
            row.Cells[0].AddParagraph(label).Format.Font.Color = Colors.DimGray;
            // MigraDoc does not shape complex scripts. Use Windows' native shaping for
            // those field values only, embedded as a high-resolution in-memory image.
            // Latin/Greek/Cyrillic remain searchable text with embedded font subsets.
            if (text.Any(c => (c >= '\u0590' && c <= '\u109f') || (c >= '\ufb1d' && c <= '\ufeff')) || text.Split(' ').Any(word => word.Length > 64))
            {
                var image = row.Cells[1].AddImage("base64:" + Convert.ToBase64String(Shape(text)));
                image.Width = Unit.FromCentimeter(12); image.LockAspectRatio = true;
            }
            else row.Cells[1].AddParagraph(text); // Literal text, never HTML/markup.
        }
        private static byte[] Shape(string text)
        {
            const int width = 1134; const float dpi = 240;
            using (var font = new System.Drawing.Font(FontName, 10.5f, GraphicsUnit.Point))
            using (var format = new StringFormat { FormatFlags = text.Any(c => c >= '\u0590' && c <= '\u08ff') ? StringFormatFlags.DirectionRightToLeft : 0 })
            using (var measure = new Bitmap(1, 1))
            {
                measure.SetResolution(dpi, dpi);
                int height;
                using (var graphics = Graphics.FromImage(measure)) height = (int)Math.Ceiling(graphics.MeasureString(text, font, width - 8, format).Height) + 8;
                using (var bitmap = new Bitmap(width, height))
                {
                    bitmap.SetResolution(dpi, dpi);
                    using (var graphics = Graphics.FromImage(bitmap))
                    { graphics.Clear(DColor.White); graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; graphics.DrawString(text, font, Brushes.Black, new RectangleF(4, 4, width - 8, height - 8), format); }
                    using (var stream = new MemoryStream()) { bitmap.Save(stream, ImageFormat.Png); return stream.ToArray(); }
                }
            }
        }
        public static void ValidatePdf(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 100 || bytes.Length > 10 * 1024 * 1024 || bytes[0] != '%' || bytes[1] != 'P' || bytes[2] != 'D' || bytes[3] != 'F')
                throw new RepositoryException("A valid completion report PDF is required.");
            try { using (var stream = new MemoryStream(bytes, false)) using (var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import)) if (pdf.PageCount < 1) throw new InvalidDataException(); }
            catch (Exception) { throw new RepositoryException("Completion report PDF validation failed."); }
        }
    }
}
