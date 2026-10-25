using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;
namespace UIMS.Services
{
    public sealed class AcademicAssignmentsApi : HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8"; context.Response.Cache.SetNoStore(); context.Response.SuppressFormsAuthenticationRedirect = true; context.Response.TrySkipIisCustomErrors = true;
            try
            {
                var identity = context.User?.Identity as FormsIdentity;
                if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired) throw new FirebaseException(401, "Sign in with your coordinator account.");
                if (FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) != "coordinator") throw new FirebaseException(403, "Coordinator access is required.");
                var service = new AcademicAssignmentService(); JObject result;
                if (context.Request.HttpMethod == "GET") result = await service.LoadAsync(identity.Name);
                else if (context.Request.HttpMethod == "POST")
                {
                    Uri origin;
                    if (!Uri.TryCreate(context.Request.Headers["Origin"], UriKind.Absolute, out origin) || origin.Authority != context.Request.Url.Authority || origin.Scheme != context.Request.Url.Scheme) throw new FirebaseException(403, "Use the same university site to save assignments.");
                    if (!(context.Request.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(415, "JSON is required.");
                    if (context.Request.ContentLength < 0 || context.Request.ContentLength > 24000) throw new FirebaseException(413, "Assignment request too large.");
                    string body; using (var reader = new StreamReader(context.Request.InputStream)) body = await reader.ReadToEndAsync();
                    if (body.Length > 24000) throw new FirebaseException(413, "Assignment request too large.");
                    var input = JObject.Parse(body);
                    if ((string)input["action"] == "rename" || (string)input["action"] == "deleteGroup")
                    {
                        foreach (var field in input.Properties()) if (Array.IndexOf(new[] { "action", "groupId", "name" }, field.Name) < 0 || field.Value.Type != JTokenType.String) throw new FirebaseException(400, "Unsupported assignment field.");
                        result = await service.ManageAsync(identity.Name, input);
                    }
                    else if ((string)input["action"] == "remove")
                    {
                        foreach (var field in input.Properties()) if (Array.IndexOf(new[] { "action", "groupId", "studentId" }, field.Name) < 0 || field.Value.Type != JTokenType.String) throw new FirebaseException(400, "Unsupported assignment field.");
                        result = await service.RemoveAsync(identity.Name, input);
                    }
                    else
                    {
                        foreach (var field in input.Properties()) if (Array.IndexOf(new[] { "requestId", "groupId", "name", "department", "supervisorId", "studentIds" }, field.Name) < 0 || (field.Name != "studentIds" && field.Value is JContainer)) throw new FirebaseException(400, "Unsupported assignment field.");
                        result = await service.SaveAsync(identity.Name, input);
                    }
                }
                else throw new FirebaseException(405, "Use GET or POST.");
                context.Response.Write(result.ToString(Formatting.None));
            }
            catch (FirebaseException error) { context.Response.StatusCode = error.StatusCode; context.Response.Write(new JObject { ["message"] = error.Message }.ToString(Formatting.None)); }
            catch (JsonException) { context.Response.StatusCode = 400; context.Response.Write("{\"message\":\"Invalid assignment JSON.\"}"); }
            catch (Exception) { context.Response.StatusCode = 503; context.Response.Write("{\"message\":\"Assignment persistence could not be verified. Retry unchanged selections to reconcile the same request.\"}"); }
        }
    }
}
