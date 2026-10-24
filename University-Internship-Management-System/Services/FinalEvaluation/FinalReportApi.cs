using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    // Existing role-specific URLs use the single Firebase-backed report workflow.
    // No redirects, fixture factories, or duplicate approval state machines.
    public static class FinalReportPageRoute
    {
        public static void TransferIfEnabled(System.Web.UI.Page page, string expectedRole)
        {
            if (!string.Equals(WebConfigurationManager.AppSettings["FinalReportWorkflowEnabled"], "true", StringComparison.OrdinalIgnoreCase)) return;
            var identity = HttpContext.Current.User?.Identity as FormsIdentity;
            if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired || FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != expectedRole)
                throw new HttpException(403, "Sign in with the authorized account for this workspace.");
            if (page.Request.HttpMethod != "GET") throw new HttpException(409, "Reload the current evaluation before submitting a review.");
            page.Server.Transfer("~/FinalReports.aspx", false);
        }
    }
    public sealed class FinalReportApi : HttpTaskAsyncHandler
    {
        private static bool Flag(string key) { return string.Equals(WebConfigurationManager.AppSettings[key], "true", StringComparison.OrdinalIgnoreCase); }
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8"; context.Response.Cache.SetNoStore(); context.Response.SuppressFormsAuthenticationRedirect = true;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            try
            {
                var identity = context.User?.Identity as FormsIdentity;
                if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired) throw new FirebaseException(401, "Sign in using your existing university account.");
                var role = FirebaseSignInService.CanonicalRole(identity.Ticket.UserData);
                if (role == null || !context.User.IsInRole(role)) throw new FirebaseException(403, "The verified account role is unavailable.");
                if (context.Request.HttpMethod == "GET" && context.Request.QueryString["action"] == "connection")
                {
                    var gateway = new StudentFirebaseGateway();
                    if (!await gateway.IsActiveAuthenticationUidAsync(identity.Name)) throw new FirebaseException(401, "The current Firebase account is not active.");
                    var profile = await gateway.ReadAsync("users/" + StudentManagementService.Key(identity.Name)) as JObject;
                    var verified = FirebaseSignInService.ProfileResult(identity.Name, profile);
                    if (verified.Role != role) throw new FirebaseException(403, "Current Firebase role no longer matches this session.");
                    context.Response.Write(new JObject { ["connected"] = true, ["workflowEnabled"] = Flag("FinalReportWorkflowEnabled") && Flag("FinalReportWorkflowSchemaValidated"), ["writesEnabled"] = Flag("FinalReportWorkflowWritesEnabled"), ["attendanceEnabled"] = Flag("AttendanceEnabled"), ["message"] = "Firebase is connected. Final-report activation is separate from database connectivity." }.ToString(Formatting.None));
                    return;
                }
                if (!Flag("FinalReportWorkflowEnabled") || !Flag("FinalReportWorkflowSchemaValidated")) throw new FirebaseException(503, "Final report workflow is awaiting verified private Rules, indexes and placement-to-attendance mappings. No workflow settings were enabled automatically.");
                var workflow = new FinalReportWorkflow(new FirebaseApplicationDatabase(new StudentFirebaseGateway()));
                string id = context.Request.QueryString["placementId"], action = context.Request.QueryString["action"] ?? "list";
                JObject result;
                if (context.Request.HttpMethod == "GET") result = action == "list" ? await workflow.ListAsync(identity.Name, role) : action == "detail" ? await workflow.ReadAsync(identity.Name, role, id) : action == "weeklyReports" ? await workflow.WeeklyReportsAsync(identity.Name, role, id) : throw new FirebaseException(400, "Unsupported report query.");
                else if (context.Request.HttpMethod == "POST")
                {
                    if (!Flag("FinalReportWorkflowWritesEnabled")) throw new FirebaseException(503, "Final report writes have not been authorized and enabled.");
                    Uri origin; if (!Uri.TryCreate(context.Request.Headers["Origin"], UriKind.Absolute, out origin) || origin.Scheme != context.Request.Url.Scheme || !string.Equals(origin.Authority, context.Request.Url.Authority, StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(403, "Use the same university site to submit this action.");
                    if (!(context.Request.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(415, "Use JSON; file contents are not accepted.");
                    if (context.Request.ContentLength < 0 || context.Request.ContentLength > 24576) throw new FirebaseException(413, "The final report request is too large.");
                    string body; using (var reader = new StreamReader(context.Request.InputStream)) body = await reader.ReadToEndAsync();
                    if (body.Length > 24576) throw new FirebaseException(413, "The final report request is too large.");
                    var input = JObject.Parse(body);
                    var allowed = new[] { "requestId", "revisionId", "percentage", "comments", "companyResponse", "fileName", "feedback", "finalGrade", "expectedDraftVersion", "weeklyReportIds" };
                    foreach (var p in input.Properties()) if (Array.IndexOf(allowed, p.Name) < 0 || (p.Value is JContainer && p.Name != "weeklyReportIds")) throw new FirebaseException(400, "Unsupported report field; attachment bytes and paths are not accepted.");
                    if (action == "reconcile") { await workflow.Reconcile(identity.Name, role, id); result = await workflow.ReadAsync(identity.Name, role, id); }
                    else result = await workflow.ActAsync(identity.Name, role, id, action, input);
                }
                else throw new FirebaseException(405, "Use GET or POST.");
                context.Response.Write(result.ToString(Formatting.None));
            }
            catch (FirebaseException e) { context.Response.StatusCode = e.StatusCode; context.Response.TrySkipIisCustomErrors = true; context.Response.Write(new JObject { ["message"] = e.Message }.ToString(Formatting.None)); }
            catch (JsonException) { context.Response.StatusCode = 400; context.Response.Write("{\"message\":\"Invalid report JSON.\"}"); }
            catch (Exception) { context.Response.StatusCode = 503; context.Response.Write("{\"message\":\"The report service could not verify persistence. Reload the report; retry the same request to reconcile an interrupted operation.\"}"); }
        }
    }
}
