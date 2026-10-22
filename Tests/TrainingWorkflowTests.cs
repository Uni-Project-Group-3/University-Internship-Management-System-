using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Services;
public class TrainingWorkflowTests
{
 static int count;
 static void Check(string n,Action a){a();Console.WriteLine("PASS "+n);count++;}
 static void Assert(bool b){if(!b)throw new Exception("Assertion failed");}
 static void Reject<T>(Action a) where T:Exception{try{a();throw new Exception("Expected rejection");}catch(T){}}
 public sealed class Memory:HttpMessageHandler {
  public JObject Data=JObject.Parse(@"{'students':{'s1':{'id':'s1','programId':'CS','academicSupervisorId':'as','placementId':'p1'},'s2':{'id':'s2','programId':'CS','academicSupervisorId':'as','placementId':'p2'}},'academicStudentIndex':{'as':{'s1':true,'s2':true}},'placements':{'p1':{'id':'p1','studentId':'s1','companyId':'c1','academicSupervisorId':'as','status':'active','approvedHours':120,'requiredHours':240},'p2':{'id':'p2','studentId':'s2','companyId':'c1','academicSupervisorId':'as','status':'active','approvedHours':80,'requiredHours':240}}}");
  public List<string> Reads=new List<string>();public int Writes;
  public Memory(){Data["users"]=JObject.Parse(@"{'s1':{'firebaseUid':'u-student','role':'student','accountStatus':'active','displayName':'Active Student One'},'s2':{'firebaseUid':'u2','role':'student','accountStatus':'active','displayName':'Active Student Two'}}");Data["usersByUid"]=JObject.Parse(@"{'u-student':'s1','u2':'s2'}");}
  public JToken Get(string path){JToken t=Data;foreach(var k in path.Split(new[]{'/'},StringSplitOptions.RemoveEmptyEntries)){t=t?[k];if(t==null)return JValue.CreateNull();}return t;}
  void Set(string path,JToken v){var parts=path.Split('/');JObject node=Data;for(int i=0;i<parts.Length-1;i++){if(!(node[parts[i]] is JObject))node[parts[i]]=new JObject();node=(JObject)node[parts[i]];}node[parts.Last()]=v.DeepClone();}
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct){
   string path=Uri.UnescapeDataString(r.RequestUri.AbsolutePath).Trim('/');if(path.EndsWith(".json"))path=path.Substring(0,path.Length-5);path=path.Trim('/');
   if(r.Method==HttpMethod.Get){Reads.Add(path);var value=Get(path).DeepClone();var query=System.Web.HttpUtility.ParseQueryString(r.RequestUri.Query);if(query["orderBy"]!=null){string field=(string)JToken.Parse(query["orderBy"]),owner=(string)JToken.Parse(query["equalTo"]);value=value is JObject rows?new JObject(rows.Properties().Where(p=>(string)p.Value[field]==owner).Select(p=>new JProperty(p.Name,p.Value.DeepClone()))):new JObject();}var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(value.ToString(Newtonsoft.Json.Formatting.None))};response.Headers.TryAddWithoutValidation("ETag","\"1\"");return response;}
   Writes++;var body=JToken.Parse(await r.Content.ReadAsStringAsync());if(r.Method==HttpMethod.Put)Set(path,body);else if(r.Method.Method=="PATCH")foreach(var p in ((JObject)body).Properties())Set((path.Length>0?path+"/":"")+p.Name,p.Value);else throw new Exception("Unexpected method");return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")};
  }
 }
 static UimsAccount Company=>new UimsAccount{Uid="u-company",ProfileId="cs",Role="company",CompanyId="c1",DisplayName="Supervisor"};
 static UimsAccount Student=>new UimsAccount{Uid="u-student",ProfileId="s1",Role="student",DisplayName="Student"};
 static JObject TaskInput(bool exam=false){
  var now=DateTime.UtcNow;var p=new JObject{["requestId"]=Guid.NewGuid().ToString(),["title"]="C# Fundamentals",["description"]="Complete the work",["studentIds"]=new JArray("s1","s2"),["opensAt"]=now.AddMinutes(-2).ToString("o"),["closesAt"]=now.AddDays(3).ToString("o"),["draft"]=false};
  if(exam)p["exam"]=new JObject{["title"]="Exam",["questionCount"]=3,["totalMarks"]=100,["passing"]=50,["duration"]=30,["attempts"]=2,["startsAt"]=now.AddMinutes(-1).ToString("o"),["questions"]=new JArray(new JObject{["id"]="q1",["type"]="mcq",["text"]="One",["marks"]=49,["choices"]=new JArray("A","B"),["correct"]=0},new JObject{["id"]="q2",["type"]="tf",["text"]="Two",["marks"]=1,["choices"]=new JArray("True","False"),["correct"]=0},new JObject{["id"]="q3",["type"]="mcq",["text"]="Three",["marks"]=50,["choices"]=new JArray("A","B"),["correct"]=0})};
  return p;
 }
 static void Main(){
  var memory=new Memory();var service=new TrainingWorkflowService(new FirebaseRealtimeDatabaseRepository("fixture-token",new HttpClient(memory)));
  Func<UimsAccount,string,JObject,JObject> act=(a,op,p)=>service.ExecuteAsync(a,op,p).GetAwaiter().GetResult();
  Func<UimsAccount,JObject> load=a=>service.LoadAsync(a).GetAwaiter().GetResult();
  var input=TaskInput();string gid=null,tid=null;
  Check("inactive trainee excluded from picker and blocked before writes",()=>{memory.Data["users"]["s2"]["accountStatus"]="disabled";int writes=memory.Writes;Assert(!((JArray)load(Company)["progress"]).OfType<JObject>().Any(p=>(string)p["id"]=="s2"));Reject<ArgumentException>(()=>act(Company,"c3Create",TaskInput()));Assert(memory.Writes==writes);memory.Data["users"]["s2"]["accountStatus"]="active";});
  Check("unmapped trainee blocked before writes",()=>{((JObject)memory.Data["usersByUid"]).Remove("u2");int writes=memory.Writes;Reject<ArgumentException>(()=>act(Company,"c3Create",TaskInput()));Assert(memory.Writes==writes);memory.Data["usersByUid"]["u2"]="s2";});
  Check("active trainee names from trusted accounts",()=>Assert((string)((JArray)load(Company)["progress"]).OfType<JObject>().Single(p=>(string)p["id"]=="s1")["name"]=="Active Student One"));
  Check("company creates independent assignments",()=>{gid=(string)act(Company,"c3Create",input)["id"];tid=gid+"-s1";Assert(memory.Get("cycle3Demo/taskAssignments/"+tid)["studentId"].Value<string>()=="s1");Assert(memory.Get("cycle3Demo/taskAssignments/"+gid+"-s2")["status"].Value<string>()=="assigned");});
  Check("repeated create is idempotent",()=>{int writes=memory.Writes;Assert((string)act(Company,"c3Create",input)["id"]==gid);Assert(memory.Writes==writes);});
  Check("student cannot create tasks",()=>Reject<UnauthorizedAccessException>(()=>act(Student,"c3Create",TaskInput())));
  Check("caller company override rejected",()=>{var p=TaskInput();p["companyId"]="c2";Reject<UnauthorizedAccessException>(()=>act(Company,"c3Create",p));});
  Check("foreign trainee rejected before writes",()=>{var p=TaskInput();p["studentIds"]=new JArray("foreign");int writes=memory.Writes;Reject<ArgumentException>(()=>act(Company,"c3Create",p));Assert(memory.Writes==writes);});
  Check("student opening only marks read",()=>{act(Student,"c3Read",new JObject{["id"]=tid});Assert((string)memory.Get("cycle3Demo/taskAssignments/"+tid)["status"]=="assigned");});
  Check("student submits owned work",()=>{act(Student,"submitTask",new JObject{["id"]=tid,["description"]="Completed C# work",["link"]="https://example.test/work",["fileName"]=""});Assert((string)memory.Get("cycle3Demo/taskAssignments/"+tid)["status"]=="submitted");});
  Check("other student cannot submit same assignment",()=>{var a=Student;a.ProfileId="s2";a.Uid="u2";Reject<UnauthorizedAccessException>(()=>act(a,"submitTask",new JObject{["id"]=tid,["description"]="Forged",["link"]=""}));});
  Check("company grades practical separately",()=>{act(Company,"reviewTask",new JObject{["id"]=tid,["status"]="completed",["score"]="8",["feedback"]="Good work"});Assert((decimal)memory.Get("cycle3Demo/taskGrades/"+tid)["score"]==8);});
  Check("unpublished practical score omitted from student projection",()=>{memory.Reads.Clear();var view=load(Student);var row=((JArray)view["tasks"]).OfType<JObject>().Single(t=>(string)t["id"]==tid);Assert(row["score"]==null);Assert(!memory.Reads.Any(r=>r.StartsWith("cycle3Demo/taskGrades/")));});
  Check("per-student publication",()=>{act(Company,"c3Publish",new JObject{["id"]=tid});Assert((bool)memory.Get("cycle3Demo/taskAssignments/"+tid)["gradePublished"]);Assert(!(bool)memory.Get("cycle3Demo/taskAssignments/"+gid+"-s2")["gradePublished"]);Assert((decimal)((JArray)load(Student)["tasks"]).OfType<JObject>().Single(t=>(string)t["id"]==tid)["score"]==8);});
  var ep=TaskInput(true);string egid=null,etid=null,first=null,second=null;
  Check("company creates consistent attached exam",()=>{egid=(string)act(Company,"c3Create",ep)["id"];etid=egid+"-s1";Assert(memory.Get("cycle3Demo/taskGroups/"+egid)["exam"]["questions"]==null);Assert(memory.Get("cycle3Demo/examKeys/"+egid)["answers"]["q1"].Value<int>()==0);});
  Check("questions unavailable before starting",()=>{var v=load(Student);var g=((JArray)v["taskGroups"]).OfType<JObject>().Single(t=>(string)t["id"]==egid);Assert(((JArray)g["exam"]["questions"]).Count==0);});
  Check("trusted start creates server expiry",()=>{first=(string)act(Student,"c3StartExam",new JObject{["id"]=etid})["id"];Assert((long)memory.Get("cycle3Demo/examAttempts/"+first)["expiresAtMs"]>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());});
  Check("student questions contain no answer key",()=>{memory.Reads.Clear();var v=load(Student);var questions=((JArray)v["examAttempts"]).OfType<JObject>().Single(t=>(string)t["id"]==first)["questions"] as JArray;Assert(questions.Count==3);Assert(!questions.OfType<JObject>().Any(q=>q["correct"]!=null));Assert(!memory.Reads.Any(r=>r.StartsWith("cycle3Demo/examKeys/")));});
  act(Student,"c3SaveAnswer",new JObject{["id"]=etid,["attemptId"]=first,["questionId"]="q1",["answer"]=0});act(Student,"c3SubmitExam",new JObject{["id"]=etid,["attemptId"]=first});
  Check("49 percent fails after trusted company grading",()=>{load(Company);var r=memory.Get("cycle3Demo/examResults/"+first);Assert((decimal)r["score"]==49);Assert(!(bool)r["passed"]);});
  Check("submitted result remains hidden from student",()=>{var at=((JArray)load(Student)["examAttempts"]).OfType<JObject>().Single(x=>(string)x["id"]==first);Assert(at["score"]==null);Assert(!(bool)at["published"]);});
  Check("exam publication is independent of practical grade",()=>{act(Company,"c3Publish",new JObject{["id"]=etid});var at=((JArray)load(Student)["examAttempts"]).OfType<JObject>().Single(x=>(string)x["id"]==first);Assert((decimal)at["score"]==49);Assert(!(bool)memory.Get("cycle3Demo/taskAssignments/"+etid)["gradePublished"]);});
  second=(string)act(Student,"c3StartExam",new JObject{["id"]=etid})["id"];foreach(string qid in new[]{"q1","q2"})act(Student,"c3SaveAnswer",new JObject{["id"]=etid,["attemptId"]=second,["questionId"]=qid,["answer"]=0});act(Student,"c3SubmitExam",new JObject{["id"]=etid,["attemptId"]=second});
  Check("50 percent passes",()=>{load(Company);var r=memory.Get("cycle3Demo/examResults/"+second);Assert((decimal)r["score"]==50);Assert((bool)r["passed"]);});
  Check("new attempt is not implicitly published",()=>{var at=((JArray)load(Student)["examAttempts"]).OfType<JObject>().Single(x=>(string)x["id"]==second);Assert(at["score"]==null);Assert(!(bool)at["published"]);});
  Check("attempt limit enforced",()=>Reject<ArgumentException>(()=>act(Student,"c3StartExam",new JObject{["id"]=etid})));
  Check("submitted answers cannot be changed",()=>Reject<ArgumentException>(()=>act(Student,"c3SaveAnswer",new JObject{["id"]=etid,["attemptId"]=first,["questionId"]="q1",["answer"]=1})));
  Check("future exam remains locked by server",()=>{var p=TaskInput(true);p["exam"]["startsAt"]=DateTime.UtcNow.AddDays(1).ToString("o");string id=(string)act(Company,"c3Create",p)["id"];Reject<ArgumentException>(()=>act(Student,"c3StartExam",new JObject{["id"]=id+"-s1",["now"]="2099-01-01"}));});
  string reportId=null;
  Check("report snapshots real student's hours and results",()=>{reportId=(string)act(Company,"c3Weekly",new JObject{["studentId"]="s1",["week"]=1,["feedback"]="Weekly report"})["id"];var report=memory.Get("cycle3Demo/weeklyReports/"+reportId);Assert((string)report["studentId"]=="s1");Assert((decimal)report["approvedHours"]==120);Assert(!report.ToString().Contains("s2"));});
  Check("student projection never contains private weekly reports",()=>Assert(((JArray)load(Student)["weeklyReports"]).Count==0));
  Check("academic reads assigned report and can add feedback only",()=>{var a=new UimsAccount{Uid="ua",ProfileId="as",Role="academic",ProgramId="CS",DisplayName="Academic"};var before=memory.Get("cycle3Demo/weeklyReports/"+reportId)["tasks"].DeepClone();Assert(((JArray)load(a)["weeklyReports"]).Count==1);act(a,"c3AcademicComment",new JObject{["id"]=reportId,["message"]="Reviewed"});Assert(JToken.DeepEquals(before,memory.Get("cycle3Demo/weeklyReports/"+reportId)["tasks"]));});
  Check("foreign academic cannot comment",()=>{var a=new UimsAccount{Uid="ux",ProfileId="other",Role="academic",ProgramId="CS"};Reject<UnauthorizedAccessException>(()=>act(a,"c3AcademicComment",new JObject{["id"]=reportId,["message"]="Forged"}));});
  Check("inconsistent exam marks rejected",()=>{var p=TaskInput(true);p["exam"]["totalMarks"]=99;Reject<ArgumentException>(()=>act(Company,"c3Create",p));});
  Check("forged image signature rejected",()=>Reject<ArgumentException>(()=>service.ImageAsync(Company,"c3RegisterImage",new JObject{["requestId"]=Guid.NewGuid().ToString(),["data"]=Convert.ToBase64String(new byte[40])}).GetAwaiter().GetResult()));
  Console.WriteLine(count+" isolated training workflow checks passed; no live Firebase writes and no Rules assertions.");
 }
}
