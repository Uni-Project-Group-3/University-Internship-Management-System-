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
    public sealed class TaskExamsApiHandler : HttpTaskAsyncHandler
    {
        public override bool IsReusable { get { return false; } }
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.Cache.SetNoStore(); context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.ContentType = "application/json; charset=utf-8";
            try
            {
                // Short-circuit before UID/profile resolution: disabled integration must not make any request.
                if (!string.Equals(WebConfigurationManager.AppSettings["FirebaseEnabled"], "true", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(WebConfigurationManager.AppSettings["TaskExamsEnabled"], "true", StringComparison.OrdinalIgnoreCase))
                    throw new UIMS.Services.FirebaseException(503, "Firebase connection not configured for exams. Exam data is unavailable until the centralized Firebase settings are enabled.");
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.Response.ClientDisconnectedToken))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(60)); var cancellation = timeout.Token;
                    string method = context.Request.HttpMethod;
                    string action = context.Request.QueryString["action"];
                    var actor = await TaskActorResolver.ResolveAsync(context, cancellation).ConfigureAwait(false);
                    var service = new TaskExamService(actor);
                    if (method == "GET")
                    {
                        JObject result;
                        if (action == "list") result = await service.ListAsync(cancellation).ConfigureAwait(false);
                        else if (action == "exam") result = actor.Role == "companySupervisor" ? await service.GetCompanyExamAsync(context.Request.QueryString["examId"], cancellation).ConfigureAwait(false) : await service.GetStudentExamAsync(context.Request.QueryString["examId"], cancellation).ConfigureAwait(false);
                        else if (action == "attempts") result = await service.ListAttemptsAsync(context.Request.QueryString["examId"], cancellation).ConfigureAwait(false);
                        else if (action == "attempt") result = await service.GetMyAttemptAsync(context.Request.QueryString["examId"], context.Request.QueryString["attemptId"], cancellation).ConfigureAwait(false);
                        else if (action == "myAttempts") result = await service.ListMyAttemptsAsync(context.Request.QueryString["examId"], cancellation).ConfigureAwait(false);
                        else if (action == "image")
                        {
                            var file = await service.DownloadQuestionImageAsync(context.Request.QueryString["fileId"], cancellation).ConfigureAwait(false);
                            context.Response.Clear(); context.Response.ContentType = file.ContentType; context.Response.AddHeader("X-Content-Type-Options", "nosniff");
                            context.Response.AddHeader("Content-Disposition", "inline; filename*=UTF-8''" + Uri.EscapeDataString(file.FileName));
                            context.Response.AddHeader("Content-Length", file.Content.Length.ToString(CultureInfo.InvariantCulture));
                            await context.Response.OutputStream.WriteAsync(file.Content, 0, file.Content.Length, cancellation).ConfigureAwait(false); return;
                        }
                        else throw new UIMS.Services.FirebaseException(400, "Unknown exam action.");
                        await WriteJsonAsync(context, result, cancellation).ConfigureAwait(false); return;
                    }
                    if (method != "POST") throw new UIMS.Services.FirebaseException(405, "Unsupported exam request method.");
                    ValidateSameOrigin(context);
                    string contentType = context.Request.ContentType == null ? "" : context.Request.ContentType.Split(';')[0].Trim();
                    if (contentType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                    {
                        if (context.Request.ContentLength < 0 || context.Request.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes + 128 * 1024)
                            throw new UIMS.Services.FirebaseException(413, "The image upload exceeds the permitted request size.");
                        var file = context.Request.Files["file"];
                        if (file == null || file.ContentLength <= 0) throw new UIMS.Services.FirebaseException(400, "Choose a question image.");
                        if (file.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes) throw new UIMS.Services.FirebaseException(413, "Images are limited to 10 MiB.");
                        var bytes = await ReadUploadAsync(file, cancellation).ConfigureAwait(false);
                        var metadata = await service.UploadQuestionImageAsync(context.Request.Form["examId"], context.Request.Form["questionId"], file.FileName, file.ContentType, bytes, cancellation).ConfigureAwait(false);
                        await WriteJsonAsync(context, metadata, cancellation).ConfigureAwait(false); return;
                    }
                    if (!contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase)) throw new UIMS.Services.FirebaseException(415, "Exam updates must use JSON or multipart image upload.");
                    if (context.Request.ContentLength < 0 || context.Request.ContentLength > 512 * 1024) throw new UIMS.Services.FirebaseException(413, "Exam request is too large.");
                    JObject body;
                    using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) body = JObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(false));
                    JObject response;
                    switch (action)
                    {
                        case "create": response = await service.CreateDraftAsync(body, cancellation).ConfigureAwait(false); break;
                        case "save": response = await service.SaveDraftAsync((string)body["examId"], body["draft"] as JObject, body["revision"] == null ? null : body["revision"].ToString(), cancellation).ConfigureAwait(false); break;
                        case "publish": response = await service.PublishAsync((string)body["examId"], cancellation).ConfigureAwait(false); break;
                        case "start": response = await service.StartAttemptAsync((string)body["examId"], cancellation).ConfigureAwait(false); break;
                        case "answers": response = await service.SaveAnswersAsync((string)body["examId"], (string)body["attemptId"], body["answers"] as JObject, false, cancellation).ConfigureAwait(false); break;
                        case "submit": response = await service.SaveAnswersAsync((string)body["examId"], (string)body["attemptId"], body["answers"] as JObject, true, cancellation).ConfigureAwait(false); break;
                        default: throw new UIMS.Services.FirebaseException(400, "Unknown exam action.");
                    }
                    await WriteJsonAsync(context, response, cancellation).ConfigureAwait(false);
                }
            }
            catch (UIMS.Services.FirebaseException ex) { WriteError(context, ex.StatusCode, ex.Message); }
            catch (OperationCanceledException) { WriteError(context, 504, "Exam request timed out."); }
            catch (JsonException) { WriteError(context, 400, "Invalid exam request JSON."); }
            catch (Exception error) { System.Diagnostics.Trace.TraceWarning("Task exams API failed: {0}", error.GetType().Name); WriteError(context, 503, "Exam service is unavailable. Check the disabled/live Firebase settings, trusted server credentials and network connectivity."); }
        }
        private static void ValidateSameOrigin(HttpContext context)
        {
            Uri origin; string value = context.Request.Headers["Origin"];
            if (!Uri.TryCreate(value, UriKind.Absolute, out origin) || origin.Scheme != context.Request.Url.Scheme || !string.Equals(origin.Authority, context.Request.Url.Authority, StringComparison.OrdinalIgnoreCase))
                throw new UIMS.Services.FirebaseException(403, "Exam updates must come from this UIMS site.");
        }
        private static async Task<byte[]> ReadUploadAsync(HttpPostedFile file, CancellationToken cancellation)
        {
            using (var output = new MemoryStream())
            {
                var buffer = new byte[81920]; int read;
                while ((read = await file.InputStream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                { if (output.Length + read > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes) throw new UIMS.Services.FirebaseException(413, "Images are limited to 10 MiB."); await output.WriteAsync(buffer, 0, read, cancellation).ConfigureAwait(false); }
                return output.ToArray();
            }
        }
        private static async Task WriteJsonAsync(HttpContext context, JToken value, CancellationToken cancellation)
        { context.Response.StatusCode = 200; await context.Response.Output.WriteAsync(value.ToString(Formatting.None)).ConfigureAwait(false); }
        private static void WriteError(HttpContext context, int status, string message)
        { context.Response.StatusCode = status; context.Response.TrySkipIisCustomErrors = true; context.Response.ContentType = "application/json; charset=utf-8"; context.Response.Write(new JObject { ["success"] = false, ["message"] = message }.ToString(Formatting.None)); }
    }
}
