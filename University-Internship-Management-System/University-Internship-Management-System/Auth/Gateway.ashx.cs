using System;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using University_Internship_Management_System.Services;

namespace University_Internship_Management_System.Auth
{
    public sealed class Gateway : HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            try
            {
                if (!context.Request.IsSecureConnection || context.Request.HttpMethod != "POST" || !IsSameOrigin(context.Request)) throw new UnauthorizedAccessException();
                string ticket = SessionService.Handle(context.Request), action = context.Request.QueryString["action"];
                if (action == "logout") { SessionService.Logout(ticket); Write(context, new { success = true }); return; }
                var identity = await SessionService.VerifyAsync(ticket);
                var session = SessionService.Get(ticket);
                var account = await new UserAccountService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).GetActiveAsync(identity);
                SessionService.RequireCurrent(session, account);
                if (action == "session")
                {
                    Write(context, new { uid = account.Uid, displayName = account.DisplayName,
                        mustChangePassword = account.MustChangePassword, serverUtc = DateTime.UtcNow.ToString("o") });
                    return;
                }
                if (account.MustChangePassword) throw new UnauthorizedAccessException();
                if (action == "roleData")
                {
                    Write(context, await new RolePortalService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).LoadAsync(account, context.Request.QueryString["page"]));
                    return;
                }
                if (action == "studentData")
                {
                    if (account.Role != "student") throw new UnauthorizedAccessException();
                    string section = context.Request.QueryString["section"];
                    Write(context, await new StudentPortalService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).LoadAsync(account, section));
                    return;
                }
                if (action == "studentProfileSave")
                {
                    if (account.Role != "student") throw new UnauthorizedAccessException();
                    if (context.Request.ContentLength > 32768) throw new ArgumentException("The request is too large.");
                    JObject input;
                    using (var reader = new StreamReader(context.Request.InputStream)) input = JObject.Parse(await reader.ReadToEndAsync());
                    await new StudentPortalService(new FirebaseRealtimeDatabaseRepository(session.IdToken)).UpdateContactAsync(account, input);
                    Write(context, new { success = true });
                    return;
                }
                // Account administration is not exposed through this end-user endpoint.
                context.Response.StatusCode = 503;
                Write(context, new { message = "This operation is not available. No account or database changes were made." });
            }
            catch (UnauthorizedAccessException)
            {
                context.Response.StatusCode = 401;
                Write(context, new { message = "Your session is invalid or expired. Please sign in again." });
            }
            catch (StudentDataReadException)
            {
                context.Response.StatusCode = 403;
                Write(context, new { message = "Firebase has not permitted this student operation. The coordinator must review the scoped permissions; no local fallback is used." });
            }
            catch (ArgumentException error)
            {
                context.Response.StatusCode = 400;
                Write(context, new { message = error.Message });
            }
            catch (JsonException)
            {
                context.Response.StatusCode = 400;
                Write(context, new { message = "Invalid request format." });
            }
            catch (Exception)
            {
                context.Response.StatusCode = 503;
                Write(context, new { message = "Identity verification is temporarily unavailable. Please try again." });
            }
        }
        internal static bool IsSameOrigin(HttpRequest request)
        {
            Uri origin;
            return Uri.TryCreate(request.Headers["Origin"], UriKind.Absolute, out origin) &&
                string.Equals(origin.GetLeftPart(UriPartial.Authority), request.Url.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
        }
        private static void Write(HttpContext context, object value)
        {
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.Write(JsonConvert.SerializeObject(value));
        }
        public override bool IsReusable => false;
    }
}
