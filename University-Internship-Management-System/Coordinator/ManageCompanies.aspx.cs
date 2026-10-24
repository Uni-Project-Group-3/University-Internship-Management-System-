using System;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Web.UI;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    public partial class ManageCompanies : Page
    {
        private const string CsrfKey = "ManageCompanies.Csrf";
        private bool api;
        private bool authorized;
        private string apiResult;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "same-origin";
            api = Request.QueryString["api"] == "1";
            if (!api && Session[CsrfKey] == null)
            {
                var bytes = new byte[32];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                Session[CsrfKey] = Convert.ToBase64String(bytes);
            }
            companyCsrf.Value = Session[CsrfKey] as string ?? "";
            RegisterAsyncTask(new PageAsyncTask(ProcessAsync));
        }

        private async System.Threading.Tasks.Task ProcessAsync()
        {
            try
            {
                if (!Request.IsAuthenticated) throw new UnauthorizedAccessException();
                var gateway = new StudentFirebaseGateway(new UIMS.Services.FirebaseConfiguration("true",
                    new UIMS.Services.FirebaseConfiguration().ProjectId,
                    new UIMS.Services.FirebaseConfiguration().DatabaseUrl,
                    new UIMS.Services.FirebaseConfiguration().ApiKey,
                    writes: "true", schemaValidated: "true", authenticationEnabled:
                    new UIMS.Services.FirebaseConfiguration().AuthenticationEnabled ? "true" : "false"));
                var coordinator = new StudentManagementService(gateway, gateway, new StudentCredentialMailer());
                string actor = await coordinator.RequireCoordinatorAsync(User.Identity.Name);
                authorized = true;
                if (!api) return;
                Response.SuppressFormsAuthenticationRedirect = true;
                if (Request.HttpMethod != "POST") { Respond(405, new { error = "Use the page controls to send this request." }); return; }
                string token = Session[CsrfKey] as string;
                if (string.IsNullOrEmpty(token) || !string.Equals(token, Request.Headers["X-UIMS-CSRF"], StringComparison.Ordinal))
                { Respond(409, new { error = "Your page session expired. Refresh the page before trying again." }); return; }
                if (Request.ContentLength > 18000) throw new StudentManagementException("The company form is too large.");
                string json;
                using (var reader = new StreamReader(Request.InputStream)) json = await reader.ReadToEndAsync();
                if (json.Length > 18000) throw new StudentManagementException("The company form is too large.");
                var request = JObject.Parse(json);
                string action = (string)request["action"];
                var service = new CompanyManagementService(gateway, gateway);
                JObject result;
                if (action == "list") result = await service.DirectoryAsync();
                else if (action == "save")
                {
                    result = await service.SaveAsync(request["form"]?.ToObject<CompanyInput>(), actor);
                    if (result.Value<bool?>("requiresSetupEmail") == true)
                    {
                        try
                        {
                            await new FirebaseAuthService().SendAccountSetupEmailAsync((string)result["email"]);
                            result["emailSent"] = true;
                            result["message"] = "Company supervisor saved. Firebase accepted the password setup email for " + (string)result["email"] + ". Check inbox and spam, then sign in with that email.";
                        }
                        catch (Exception error)
                        {
                            result["emailSent"] = false;
                            result["message"] = error is StudentManagementException ? error.Message : "The account is saved, but email delivery was not confirmed. Use Retry Send.";
                        }
                    }
                }
                else if (action == "sendSetup")
                {
                    string uid = (string)request["uid"], companyId = (string)request["companyId"];
                    var profile = await gateway.ReadAsync("users/" + StudentManagementService.Key(uid)) as JObject;
                    if (profile == null || (string)profile["role"] != "companySupervisor" || (string)profile["companyId"] != companyId)
                        throw new StudentManagementException("Select a company supervisor from the directory.");
                    var account = await gateway.FindAsync(uid);
                    if (account == null || account.Disabled || profile.Value<bool?>("isActive") != true)
                        throw new StudentManagementException("Activate the supervisor account before sending its password setup link.");
                    await new FirebaseAuthService().SendAccountSetupEmailAsync(account.Email);
                    result = new JObject { ["emailSent"] = true, ["message"] = "Firebase accepted the password setup email for " + account.Email + ". Check inbox and spam." };
                }
                else result = await service.ActAsync((string)request["companyId"], (string)request["uid"], action, actor);
                Respond(200, result);
            }
            catch (UnauthorizedAccessException)
            {
                if (api) Respond(403, new { error = "Sign in with an active coordinator account." });
                else
                {
                    Response.Redirect(ResolveUrl("~/Auth/Login.aspx") + "?ReturnUrl=" + HttpUtility.UrlEncode(Request.RawUrl), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
            catch (Exception error)
            {
                Trace.Warn("ManageCompanies", error.GetType().Name);
                string message = "The request could not be completed. Refresh the directory or retry the same form; pending setup can be resumed.";
                if (error is StudentManagementException) message = error.Message;
                else if (error is JsonException) message = "Check the company form and try again.";
                else if ((error as FirebaseAuthException)?.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
                    message = "That email already has a Firebase login. Choose a different supervisor email.";
                if (api) Respond(400, new { error = message });
                else { Response.StatusCode = 503; throw new HttpException(503, "Company management is temporarily unavailable. Retry shortly."); }
            }
        }
        private void Respond(int status, object data)
        {
            Response.StatusCode = status;
            Response.TrySkipIisCustomErrors = true;
            Response.SuppressFormsAuthenticationRedirect = true;
            apiResult = JsonConvert.SerializeObject(data);
        }
        protected override void Render(HtmlTextWriter writer)
        {
            if (api)
            {
                Response.ContentType = "application/json";
                writer.Write(apiResult ?? "{\"error\":\"The request was not processed. Refresh and retry.\"}");
                return;
            }
            if (authorized) base.Render(writer);
        }
    }
}
