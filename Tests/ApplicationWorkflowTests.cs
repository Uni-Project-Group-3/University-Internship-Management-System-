using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Services;
class ApplicationWorkflowTests{
 static int count;static void Check(string name,Action a){a();Console.WriteLine("PASS "+name);count++;}
 static void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
 static void Reject<T>(Action a)where T:Exception{try{a();throw new Exception("Expected rejection");}catch(T){}}
 static void Main(){
 var memory=new TrainingWorkflowTests.Memory();memory.Data["students"]["s1"]["placementId"]=null;memory.Data["placements"]=new JObject();
 memory.Data["opportunities"]=JObject.Parse(@"{'o1':{'id':'o1','companyId':'c1','title':'Intern','status':'published','canApply':true,'eligibleProgramIds':['CS'],'applicationDeadline':'2099-12-01','startDate':'2099-12-02','durationWeeks':8,'requiredHours':240,'capacity':1}}");
 var student=new UimsAccount{Uid="u1",ProfileId="s1",Role="student",DisplayName="Student"};
 var company=new UimsAccount{Uid="uc",ProfileId="cs",Role="company",CompanyId="c1"};
 var service=new ApplicationWorkflowService(new FirebaseRealtimeDatabaseRepository("fixture-token",new HttpClient(memory)));
 Func<JObject> input=()=>new JObject{["opportunityId"]="o1",["phone"]="+962777777777",["cvLink"]="https://example.test/cv.pdf",["portfolioLink"]="",["motivation"]="C#",["confirmed"]=true};
 Check("stable IDs depend on verified student and selected opportunity",()=>{Assert(ApplicationWorkflowService.ApplicationId("s1","o1")==ApplicationWorkflowService.ApplicationId("s1","o1"));Assert(ApplicationWorkflowService.ApplicationId("s1","o1")!=ApplicationWorkflowService.ApplicationId("s2","o1"));});
 Check("browser student ID override rejected",()=>{var p=input();p["studentId"]="s2";Reject<ArgumentException>(()=>service.ApplyAsync(student,p).GetAwaiter().GetResult());});
 Check("unverified UID rejected",()=>Reject<UnauthorizedAccessException>(()=>service.ApplyAsync(new UimsAccount{Role="student",ProfileId="s1"},input()).GetAwaiter().GetResult()));
 Check("wrong role cannot submit",()=>Reject<UnauthorizedAccessException>(()=>service.ApplyAsync(company,input()).GetAwaiter().GetResult()));
 Check("unconfirmed information rejected",()=>{var p=input();p["confirmed"]=false;Reject<ArgumentException>(()=>service.ApplyAsync(student,p).GetAwaiter().GetResult());});
 Check("unsafe CV link rejected",()=>{var p=input();p["cvLink"]="javascript:alert(1)";Reject<ArgumentException>(()=>service.ApplyAsync(student,p).GetAwaiter().GetResult());});
 Check("wrong academic program rejected",()=>{var s=(JObject)memory.Data["students"]["s1"].DeepClone();s["programId"]="AI";Reject<ArgumentException>(()=>ApplicationWorkflowService.RequireEligible(s,(JObject)memory.Data["opportunities"]["o1"],"o1"));});
 string id=null;
 Check("student creates real-shaped owned application and unique index",()=>{id=(string)service.ApplyAsync(student,input()).GetAwaiter().GetResult()["id"];var a=memory.Get("applications/"+id);Assert((string)a["studentId"]=="s1");Assert((string)a["companyId"]=="c1");Assert((string)memory.Get("applicationIndex/s1/o1")==id);});
 Check("duplicate submission rejected",()=>Reject<ArgumentException>(()=>service.ApplyAsync(student,input()).GetAwaiter().GetResult()));
 Check("foreign company decision denied",()=>Reject<UnauthorizedAccessException>(()=>service.ReviewAsync(new UimsAccount{Uid="other",ProfileId="other",Role="company",CompanyId="c2"},new JObject{["id"]=id,["status"]="accepted",["feedback"]="",["expectedUpdatedAt"]=(string)memory.Get("applications/"+id)["lastUpdatedAt"]}).GetAwaiter().GetResult()));
 Check("own company accepts pending application",()=>{service.ReviewAsync(company,new JObject{["id"]=id,["status"]="accepted",["feedback"]="",["expectedUpdatedAt"]=(string)memory.Get("applications/"+id)["lastUpdatedAt"]}).GetAwaiter().GetResult();Assert((string)memory.Get("applications/"+id)["reviewedBy"]=="cs");});
 Check("student confirms placement and seat atomically",()=>{var r=service.ConfirmAsync(student,new JObject{["id"]=id}).GetAwaiter().GetResult();Assert((string)r["id"]=="PL-s1");Assert((string)memory.Get("students/s1/placementId")=="PL-s1");Assert((string)memory.Get("applications/"+id)["status"]=="confirmed");Assert((int)memory.Get("placements/PL-s1")["approvedHours"]==0);Assert((int)memory.Get("placements/PL-s1")["requiredHours"]==240);Assert((string)memory.Get("opportunitySeats/o1/0")["studentId"]=="s1");});
 Check("confirmation retry is idempotent",()=>{int writes=memory.Writes;service.ConfirmAsync(student,new JObject{["id"]=id}).GetAwaiter().GetResult();Assert(memory.Writes==writes);});
 Check("placed student cannot apply to second internship",()=>Reject<ArgumentException>(()=>service.ApplyAsync(student,input()).GetAwaiter().GetResult()));
 Console.WriteLine(count+" isolated application workflow checks passed; emulator concurrency permissions not asserted.");
 }}
