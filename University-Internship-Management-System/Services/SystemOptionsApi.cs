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
    // GET ?list=key: active options of one list (any signed-in user).
    // GET: every list (coordinator). POST: create/update/delete lists and options (coordinator).
    public sealed class SystemOptionsApi : HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8"; context.Response.Cache.SetNoStore(); context.Response.SuppressFormsAuthenticationRedirect = true; context.Response.TrySkipIisCustomErrors = true;
            try
            {
                var identity = context.User?.Identity as FormsIdentity;
                if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired) throw new FirebaseException(401, "Sign in to load the options.");
                bool coordinator = FirebaseSignInService.CanonicalRole(identity.Ticket.UserData) == "coordinator";
                var service = new SystemOptionsService(); JObject result;
                if (context.Request.HttpMethod == "GET")
                {
                    string list = context.Request.QueryString["list"];
                    if (!string.IsNullOrEmpty(list)) result = await service.ListAsync(identity.Name, list);
                    else if (coordinator) result = await service.AllAsync(identity.Name);
                    else throw new FirebaseException(403, "Coordinator access is required.");
                }
                else if (context.Request.HttpMethod == "POST")
                {
                    if (!coordinator) throw new FirebaseException(403, "Coordinator access is required.");
                    Uri origin;
                    if (!Uri.TryCreate(context.Request.Headers["Origin"], UriKind.Absolute, out origin) || origin.Authority != context.Request.Url.Authority || origin.Scheme != context.Request.Url.Scheme) throw new FirebaseException(403, "Use the same university site to change options.");
                    if (!(context.Request.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(415, "JSON is required.");
                    if (context.Request.ContentLength < 0 || context.Request.ContentLength > 4000) throw new FirebaseException(413, "Options request too large.");
                    string body; using (var reader = new StreamReader(context.Request.InputStream)) body = await reader.ReadToEndAsync();
                    if (body.Length > 4000) throw new FirebaseException(413, "Options request too large.");
                    var input = JObject.Parse(body);
                    foreach (var field in input.Properties()) if (Array.IndexOf(new[] { "action", "list", "key", "name", "label", "order", "active" }, field.Name) < 0 || field.Value is JContainer) throw new FirebaseException(400, "Unsupported options field.");
                    string action = (string)input["action"], list = (string)input["list"] ?? "", key = (string)input["key"] ?? "";
                    if (action == "saveList") result = await service.SaveListAsync(identity.Name, list, (string)input["name"]);
                    else if (action == "deleteList") result = await service.DeleteListAsync(identity.Name, list);
                    else if (action == "saveOption") result = await service.SaveOptionAsync(identity.Name, list, key, (string)input["label"], (int?)input["order"], (bool?)input["active"] != false);
                    else if (action == "deleteOption") result = await service.DeleteOptionAsync(identity.Name, list, key);
                    else throw new FirebaseException(400, "Unsupported options action.");
                }
                else throw new FirebaseException(405, "Use GET or POST.");
                context.Response.Write(result.ToString(Formatting.None));
            }
            catch (FirebaseException error) { context.Response.StatusCode = error.StatusCode; context.Response.Write(new JObject { ["message"] = error.Message }.ToString(Formatting.None)); }
            catch (JsonException) { context.Response.StatusCode = 400; context.Response.Write("{\"message\":\"Invalid options JSON.\"}"); }
            catch (Exception) { context.Response.StatusCode = 503; context.Response.Write("{\"message\":\"Firebase could not be reached. Your change was not confirmed; refresh and check before retrying.\"}"); }
        }
    }
}
