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
    // GET: the signed-in user's latest notifications and unread count. POST {id}: mark one read; POST {}: mark all read.
    public sealed class NotificationsApi : HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8"; context.Response.Cache.SetNoStore(); context.Response.SuppressFormsAuthenticationRedirect = true; context.Response.TrySkipIisCustomErrors = true;
            try
            {
                var identity = context.User?.Identity as FormsIdentity;
                if (identity == null || !identity.IsAuthenticated || identity.Ticket.Expired) throw new FirebaseException(401, "Sign in to see notifications.");
                var db = new StudentFirebaseGateway();
                if (!await db.IsActiveAuthenticationUidAsync(StudentManagementService.Key(identity.Name))) throw new FirebaseException(401, "Sign in with an active account.");
                if (context.Request.HttpMethod == "GET") { context.Response.Write((await Notify.ListAsync(db, identity.Name)).ToString(Formatting.None)); return; }
                if (context.Request.HttpMethod != "POST") throw new FirebaseException(405, "Use GET or POST.");
                Uri origin;
                if (!Uri.TryCreate(context.Request.Headers["Origin"], UriKind.Absolute, out origin) || origin.Authority != context.Request.Url.Authority) throw new FirebaseException(403, "Use the same university site.");
                if (context.Request.ContentLength > 1000) throw new FirebaseException(413, "Request too large.");
                string body; using (var reader = new StreamReader(context.Request.InputStream)) body = await reader.ReadToEndAsync();
                var input = string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
                await Notify.MarkReadAsync(db, identity.Name, (string)input["id"]);
                context.Response.Write("{\"saved\":true}");
            }
            catch (FirebaseException error) { context.Response.StatusCode = error.StatusCode; context.Response.Write(new JObject { ["message"] = error.Message }.ToString(Formatting.None)); }
            catch (JsonException) { context.Response.StatusCode = 400; context.Response.Write("{\"message\":\"Invalid request.\"}"); }
            catch (Exception) { context.Response.StatusCode = 503; context.Response.Write("{\"message\":\"Notifications are unavailable right now.\"}"); }
        }
    }
}
