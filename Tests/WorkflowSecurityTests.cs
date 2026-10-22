using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Services;
class WorkflowSecurityTests {
 static int count;
 static void Check(string name,Action test){test();Console.WriteLine("PASS "+name);count++;}
 static void Denied<T>(Action test) where T:Exception {try{test();throw new Exception("Expected "+typeof(T).Name);}catch(T){}}
 static JObject Input(){return JObject.Parse(@"{'id':'','requestId':'0123456789abcdef0123456789abcdef','title':'Intern','description':'Training','requirements':'C#','location':'Amman','workMode':'hybrid','capacity':2,'eligibleProgramIds':['CS'],'applicationDeadline':'2099-12-01','startDate':'2099-12-02','durationWeeks':8,'requiredHours':240,'status':'published'}");}
 sealed class Handler:HttpMessageHandler {
  public HttpStatusCode Status=HttpStatusCode.OK;public int Calls;public string Body,Tag,Host;
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){Calls++;Body=await r.Content.ReadAsStringAsync();Tag=string.Join("",r.Headers.GetValues("If-Match"));Host=r.RequestUri.Host;return new HttpResponseMessage(Status){Content=new StringContent("{}")};}
 }
 static UimsAccount Account(){return new UimsAccount{Uid="verified-uid",ProfileId="verified-profile",Role="company",CompanyId="company-1"};}
 static void Main(){
 Check("valid opportunity",()=>RolePortalService.ValidateOpportunityInput(Input()));
 foreach(var field in new[]{"uid","companyId","role","createdBy","canApply","companyVerifiedByUniversity"}){var k=field;Check("reject caller identity/policy "+k,()=>{var p=Input();p[k]="forged";Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});}
 Check("deadline order",()=>{var p=Input();p["startDate"]="2099-11-01";Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 Check("invalid date",()=>{var p=Input();p["startDate"]="2099-02-31";Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 Check("past published deadline",()=>{var p=Input();p["applicationDeadline"]="2001-01-01";Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 Check("fractional capacity",()=>{var p=Input();p["capacity"]=1.5;Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 Check("duplicate programs",()=>{var p=Input();p["eligibleProgramIds"]=new JArray("CS","CS");Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 Check("invalid request id",()=>{var p=Input();p["requestId"]="../x";Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 Check("unsupported status",()=>{var p=Input();p["status"]="accepted";Denied<ArgumentException>(()=>RolePortalService.ValidateOpportunityInput(p));});
 var handler=new Handler();var repo=new FirebaseRealtimeDatabaseRepository("test-token",new HttpClient(handler));var doc=new JObject{["id"]="opp-1",["companyId"]="company-1"};
 Check("student cannot write opportunity",()=>{var a=Account();a.Role="student";Denied<UnauthorizedAccessException>(()=>repo.WriteCompanyOpportunityAsync(a,"opp-1",doc,"v1").GetAwaiter().GetResult());});
 Check("foreign company rejected before HTTP",()=>{var a=Account();a.CompanyId="company-2";Denied<UnauthorizedAccessException>(()=>repo.WriteCompanyOpportunityAsync(a,"opp-1",doc,"v1").GetAwaiter().GetResult());if(handler.Calls!=0)throw new Exception("HTTP called");});
 Check("mandatory password restriction",()=>{var a=Account();a.MustChangePassword=true;Denied<UnauthorizedAccessException>(()=>repo.WriteCompanyOpportunityAsync(a,"opp-1",doc,"v1").GetAwaiter().GetResult());});
 Check("missing verified UID denied",()=>{var a=Account();a.Uid=null;Denied<UnauthorizedAccessException>(()=>repo.WriteCompanyOpportunityAsync(a,"opp-1",doc,"v1").GetAwaiter().GetResult());});
 Check("missing version denied",()=>Denied<ArgumentException>(()=>repo.WriteCompanyOpportunityAsync(Account(),"opp-1",doc,null).GetAwaiter().GetResult()));
 Check("conditional write uses configured project",()=>{repo.WriteCompanyOpportunityAsync(Account(),"opp-1",doc,"v1").GetAwaiter().GetResult();if(handler.Tag!="v1"||handler.Host!="intership-managment-system-default-rtdb.europe-west1.firebasedatabase.app")throw new Exception("Wrong target/version");});
 Check("concurrent update rejected",()=>{handler.Status=HttpStatusCode.PreconditionFailed;Denied<AccountConflictException>(()=>repo.WriteCompanyOpportunityAsync(Account(),"opp-1",doc,"v1").GetAwaiter().GetResult());});
 Check("Rules denial remains denial",()=>{handler.Status=HttpStatusCode.Unauthorized;Denied<StudentDataReadException>(()=>repo.WriteCompanyOpportunityAsync(Account(),"opp-1",doc,"v1").GetAwaiter().GetResult());});
 Console.WriteLine(count+" workflow security checks passed");
 }
}