using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UIMS.Tasks
{
    public sealed class WeeklyReportsApiHandler : HttpTaskAsyncHandler
    {
        public override bool IsReusable { get { return false; } }
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.Cache.SetNoStore();
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.ContentType = "application/json; charset=utf-8";
            try
            {
                // Disabled means no identity lookup, token acquisition, Storage call, or database request.
                if (!Enabled("FirebaseEnabled") || !Enabled("WeeklyReportsEnabled"))
                    throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured for weekly reports. No Firebase or Storage request was sent.");
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.Response.ClientDisconnectedToken))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(90)); var cancellation = timeout.Token;
                    string method = context.Request.HttpMethod, action = context.Request.QueryString["action"] ?? "";
                    if (method == "POST") ValidateSameOrigin(context);
                    if (method != "GET" && method != "POST") throw new UIMS.Services.FirebaseException(405, "Unsupported weekly report request method.");
                    var actor = await TaskActorResolver.ResolveAsync(context, cancellation, allowAcademicSupervisor: true).ConfigureAwait(false);
                    var service = new WeeklyReportService(actor);
                    if (method == "GET")
                    {
                        if (action == "placements") await WriteJsonAsync(context, await service.ListCompanyPlacementsAsync(cancellation).ConfigureAwait(false)).ConfigureAwait(false);
                        else if (action == "companyReports") await WriteJsonAsync(context, await service.ListCompanyReportsAsync(cancellation).ConfigureAwait(false)).ConfigureAwait(false);
                        else if (action == "academicReports") await WriteJsonAsync(context, await service.ListAcademicReportsAsync(cancellation).ConfigureAwait(false)).ConfigureAwait(false);
                        else if (action == "studentReports") await WriteJsonAsync(context, await service.ListStudentReportsAsync(cancellation).ConfigureAwait(false)).ConfigureAwait(false);
                        else if (action == "report")
                        {
                            string id = context.Request.QueryString["reportId"];
                            var result = actor.Role == "companySupervisor" ? await service.GetCompanyReportAsync(id, cancellation).ConfigureAwait(false) :
                                actor.Role == "academicSupervisor" ? await service.GetAcademicReportAsync(id, cancellation).ConfigureAwait(false) :
                                throw new UIMS.Services.FirebaseException(403, "Students may view their own report status only.");
                            await WriteJsonAsync(context, result).ConfigureAwait(false);
                        }
                        else if (action == "download")
                        {
                            var file = await service.DownloadAsync(context.Request.QueryString["reportId"], context.Request.QueryString["revisionId"], cancellation).ConfigureAwait(false);
                            context.Response.Clear(); context.Response.ContentType = file.ContentType;
                            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
                            context.Response.AddHeader("Content-Disposition", "attachment; filename*=UTF-8''" + Uri.EscapeDataString(file.FileName));
                            context.Response.AddHeader("Content-Length", file.Content.Length.ToString(CultureInfo.InvariantCulture));
                            await context.Response.OutputStream.WriteAsync(file.Content, 0, file.Content.Length, cancellation).ConfigureAwait(false); return;
                        }
                        else throw new UIMS.Services.FirebaseException(400, "Unknown weekly report action.");
                        return;
                    }

                    string contentType = context.Request.ContentType == null ? "" : context.Request.ContentType.Split(';')[0].Trim();
                    if (contentType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                    {
                        if (context.Request.ContentLength < 0 || context.Request.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes + 128 * 1024)
                            throw new UIMS.Services.FirebaseException(413, "Weekly Word uploads are limited to 10 MiB plus form overhead.");
                        var file = context.Request.Files["file"];
                        if (file == null || file.ContentLength <= 0) throw new UIMS.Services.FirebaseException(400, "Choose a Word .docx report.");
                        if (file.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes) throw new UIMS.Services.FirebaseException(413, "Weekly reports are limited to 10 MiB.");
                        byte[] bytes = await ReadUploadAsync(file, cancellation).ConfigureAwait(false);
                        JObject result;
                        if (action == "submit")
                        {
                            int week; if (!int.TryParse(context.Request.Form["weekNumber"], NumberStyles.None, CultureInfo.InvariantCulture, out week)) throw new UIMS.Services.FirebaseException(400, "Choose a training week number.");
                            result = await service.SubmitAsync(context.Request.Form["placementId"], week, context.Request.Form["weekStart"], context.Request.Form["weekEnd"],
                                file.FileName, file.ContentType, bytes, context.Request.Form["companyComments"], cancellation).ConfigureAwait(false);
                        }
                        else if (action == "resubmit") result = await service.ResubmitAsync(context.Request.Form["reportId"], file.FileName, file.ContentType, bytes, context.Request.Form["companyComments"], cancellation).ConfigureAwait(false);
                        else throw new UIMS.Services.FirebaseException(400, "Unknown weekly report upload action.");
                        await WriteJsonAsync(context, result).ConfigureAwait(false); return;
                    }
                    if (!contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(415, "Weekly report reviews must use JSON or the supported DOCX multipart upload.");
                    if (context.Request.ContentLength < 0 || context.Request.ContentLength > 32768) throw new UIMS.Services.FirebaseException(413, "Weekly report review request is too large.");
                    JObject body;
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) body = JObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(false));
                    if (action != "review") throw new UIMS.Services.FirebaseException(400, "Unknown weekly report action.");
                    var review = await service.ReviewAsync((string)body["reportId"], (string)body["revisionId"], (string)body["decision"], (string)body["feedback"], (string)body["requestId"], cancellation).ConfigureAwait(false);
                    await WriteJsonAsync(context, review).ConfigureAwait(false);
                }
            }
            catch (UIMS.Services.FirebaseException error) { WriteError(context, error.StatusCode, error.Message); }
            catch (OperationCanceledException) { WriteError(context, 504, "Weekly report request timed out. Refresh before retrying to check its status."); }
            catch (JsonException) { WriteError(context, 400, "Invalid weekly report JSON request."); }
            catch (Exception error)
            {
                System.Diagnostics.Trace.TraceWarning("Weekly reports API failed: {0}", error.GetType().Name);
                WriteError(context, 503, "Weekly reports are unavailable. Check the centralized Firebase, Storage, trusted credentials, and Rules configuration.");
            }
        }
        private static bool Enabled(string key) { return string.Equals(WebConfigurationManager.AppSettings[key], "true", StringComparison.OrdinalIgnoreCase); }
        private static void ValidateSameOrigin(HttpContext context)
        {
            Uri origin; string raw = context.Request.Headers["Origin"];
            if (!Uri.TryCreate(raw, UriKind.Absolute, out origin) || origin.Scheme != context.Request.Url.Scheme || !string.Equals(origin.Authority, context.Request.Url.Authority, StringComparison.OrdinalIgnoreCase))
                throw new UIMS.Services.FirebaseException(403, "Weekly report updates must come from this UIMS site.");
        }
        private static async Task<byte[]> ReadUploadAsync(HttpPostedFile file, CancellationToken cancellation)
        {
            using (var output = new MemoryStream())
            {
                var buffer = new byte[81920]; int read;
                while ((read = await file.InputStream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                {
                    if (output.Length + read > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes) throw new UIMS.Services.FirebaseException(413, "Weekly reports are limited to 10 MiB.");
                    await output.WriteAsync(buffer, 0, read, cancellation).ConfigureAwait(false);
                }
                return output.ToArray();
            }
        }
        private static async Task WriteJsonAsync(HttpContext context, JToken value)
        { context.Response.StatusCode = 200; await context.Response.Output.WriteAsync(value.ToString(Formatting.None)).ConfigureAwait(false); }
        private static void WriteError(HttpContext context, int status, string message)
        {
            context.Response.StatusCode = status; context.Response.TrySkipIisCustomErrors = true; context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Write(new JObject { ["success"] = false, ["message"] = message }.ToString(Formatting.None));
        }
    }
}
