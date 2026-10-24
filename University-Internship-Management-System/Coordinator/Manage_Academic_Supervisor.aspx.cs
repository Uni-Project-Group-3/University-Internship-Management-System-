using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    public partial class Manage_Academic_Supervisor : Page
    {
        private const string CsrfKey="AcademicSupervisor.Csrf",CandidateKey="AcademicSupervisor.Candidate";
        private bool api,authorized;
        private string result;
        public sealed class Candidate
        {
            public UniversityAcademicRecord Record {get;set;}
            public string Password {get;set;}
            public DateTimeOffset ExpiresUtc {get;set;}
            public bool Created {get;set;}
            public bool EmailSent {get;set;}
        }
        protected void Page_Load(object sender,EventArgs e)
        {
            Response.Cache.SetNoStore();Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Headers["X-Content-Type-Options"]="nosniff";Response.Headers["Referrer-Policy"]="same-origin";
            api=Request.QueryString["api"]=="1";
            if(!api&&Session[CsrfKey]==null){var bytes=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(bytes);Session[CsrfKey]=Convert.ToBase64String(bytes);}
            academicCsrf.Value=Session[CsrfKey] as string??"";
            RegisterAsyncTask(new PageAsyncTask(ProcessAsync));
        }
        private async Task ProcessAsync()
        {
            try
            {
                if(!Request.IsAuthenticated)throw new UnauthorizedAccessException();
                var gateway=new StudentFirebaseGateway(new UIMS.Services.FirebaseConfiguration("true",
                    new UIMS.Services.FirebaseConfiguration().ProjectId,
                    new UIMS.Services.FirebaseConfiguration().DatabaseUrl,
                    new UIMS.Services.FirebaseConfiguration().ApiKey,
                    writes: "true", schemaValidated: "true", authenticationEnabled:
                    new UIMS.Services.FirebaseConfiguration().AuthenticationEnabled ? "true" : "false"));
                string actor=await new StudentManagementService(gateway,gateway,new StudentCredentialMailer()).RequireCoordinatorAsync(User.Identity.Name);
                authorized=true;if(!api)return;
                if(Request.HttpMethod!="POST"){Reply(405,new{error="Use the page controls to send this request."});return;}
                string csrf=Session[CsrfKey] as string;
                if(string.IsNullOrEmpty(csrf)||!string.Equals(csrf,Request.Headers["X-UIMS-CSRF"],StringComparison.Ordinal)){Reply(409,new{error="Your page session expired. Refresh before trying again."});return;}
                if(Request.ContentLength>4000)throw new StudentManagementException("The request is too large.");
                string json;using(var reader=new StreamReader(Request.InputStream))json=await reader.ReadToEndAsync();
                if(json.Length>4000)throw new StudentManagementException("The request is too large.");
                var request=JObject.Parse(json);string action=(string)request["action"];
                var service=new AcademicSupervisorService(gateway,gateway);
                if(action=="list"){Reply(200,await service.DirectoryAsync());return;}
                if(action=="lookup")
                {
                    Session.Remove(CandidateKey);
                    var record=await service.LookupAsync(((string)request["staffId"]??"").Trim());
                    var candidate=new Candidate{Record=record,Password=StudentManagementService.GeneratePassword(),ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(15)};
                    Store(candidate);Reply(200,new{record,message="Official record found. Confirm to create the Firebase account."});return;
                }
                if(action=="generate"||action=="create")
                {
                    var candidate=ReadCandidate((string)request["staffId"]);
                    if(action=="generate")
                    {
                        if(candidate.Created)throw new StudentManagementException("This account was already created. Its password cannot be regenerated here.");
                        candidate.Password=StudentManagementService.GeneratePassword();Store(candidate);
                        Reply(200,new{message="Account password is managed through the email setup link."});return;
                    }
                    if(!candidate.Created){await service.CreateAsync(candidate.Record,candidate.Password,actor);candidate.Created=true;Store(candidate);}
                    if(candidate.EmailSent)throw new StudentManagementException("Account details have already been sent.");
                    try { await new FirebaseAuthService().SendAccountSetupEmailAsync(candidate.Record.Email); }
                    catch(StudentManagementException error)
                    {
                        Reply(200,new{created=true,emailSent=false,message=error.Message});return;
                    }
                    catch(Exception)
                    {
                        Reply(200,new{created=true,emailSent=false,message="Email delivery was not confirmed. The account is saved. Use Retry Send; a retry may send a duplicate."});return;
                    }
                    candidate.EmailSent=true;candidate.Password=null;Store(candidate);
                    Reply(200,new{created=true,emailSent=true,message="Academic supervisor account saved. Firebase accepted a password setup email for " + candidate.Record.Email + ". Check inbox and spam, then sign in using this same email address."});return;
                }
                Reply(200,new{message=await service.ActAsync((string)request["uid"],action,actor)});
            }
            catch(UnauthorizedAccessException)
            {
                if(api)Reply(403,new{error="Sign in with an active coordinator account."});
                else{Response.Redirect(ResolveUrl("~/Auth/Login.aspx")+"?ReturnUrl="+HttpUtility.UrlEncode(Request.RawUrl),false);Context.ApplicationInstance.CompleteRequest();}
            }
            catch(Exception error)
            {
                Trace.Warn("AcademicSupervisors",error.GetType().Name);
                string message="The request could not be completed. Refresh or retry the same lookup; pending setup is recoverable.";
                if(error is StudentManagementException)message=error.Message;
                else if(error is JsonException)message="Check the request and try again.";
                else if((error as FirebaseAuthException)?.AuthErrorCode==AuthErrorCode.EmailAlreadyExists)message="The official email already has a Firebase login. Review that account first.";
                if(api)Reply(400,new{error=message});else throw new HttpException(503,"Academic supervisor management is temporarily unavailable.");
            }
        }
        private Candidate ReadCandidate(string staffId)
        {
            string json=Session[CandidateKey] as string;if(json!=null&&json.StartsWith("{")){Session.Remove(CandidateKey);json=null;}var candidate=string.IsNullOrEmpty(json)?null:JsonConvert.DeserializeObject<Candidate>(Encoding.UTF8.GetString(MachineKey.Unprotect(Convert.FromBase64String(json), CandidateKey, Session.SessionID)));
            if(candidate==null||candidate.Record==null||candidate.ExpiresUtc<=DateTimeOffset.UtcNow||candidate.Record.StaffId!=(staffId??"").Trim())
                throw new StudentManagementException("Find this staff ID in university records again before confirming.");
            return candidate;
        }
        private void Store(Candidate candidate){Session[CandidateKey]=Convert.ToBase64String(MachineKey.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(candidate)), CandidateKey, Session.SessionID));}
        private void Reply(int status,object data)
        {
            Response.StatusCode=status;Response.TrySkipIisCustomErrors=true;Response.SuppressFormsAuthenticationRedirect=true;
            result=JsonConvert.SerializeObject(data,new JsonSerializerSettings{ContractResolver=new CamelCasePropertyNamesContractResolver()});
        }
        protected override void Render(HtmlTextWriter writer)
        {
            if(api){Response.ContentType="application/json";writer.Write(result??"{\"error\":\"Refresh the page and retry.\"}");return;}
            if(authorized)base.Render(writer);
        }
        protected void btnLogout_Click(object sender,EventArgs e) { University_Internship_Management_System.Shared.LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }
    }
}
