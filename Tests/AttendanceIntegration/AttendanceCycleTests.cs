using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using UIMS.Attendance;
using UIMS.Services;
using University_Internship_Management_System.Shared;

sealed class MemoryFirebase : AttendanceFirebase {
 public override Task ValidatePlacementBinding(JObject assignment,string uid) { return Task.CompletedTask; }
 public override Task ValidateStudentEligibility(string id,string uid) { return Task.CompletedTask; }
 public string FailedStudent; public bool TransportFailure;
 public override Task<JObject> ReadConfirmedPlacements(string field,string value) { return Task.FromResult(new JObject()); }
 public override Task<JObject> ReadAcademicSupervisor(string studentId,string uid,string expectedId) { if(expectedId!="AC")throw new AttendanceSetupException("Missing academic mapping");return Task.FromResult(new JObject{["id"]="AC",["name"]="Academic test"}); }

 public readonly JObject Root = new JObject(); readonly object gate = new object();
 public int Writes, Reads, Revision, Conflicts; public bool LoseAcknowledgment; public Barrier SaveBarrier;
 public void Put(string path,JObject value) { lock(gate) { JObject n=Root; var parts=path.Split('/'); for(int i=0;i<parts.Length-1;i++){ if(!(n[parts[i]] is JObject))n[parts[i]]=new JObject(); n=(JObject)n[parts[i]]; } n[parts.Last()]=value.DeepClone(); Revision++; } }
 public JObject Peek(string path) { lock(gate) { JToken n=Root; foreach(var key in path.Split('/'))n=(n as JObject)?[key]; return (JObject)(n as JObject ?? new JObject()).DeepClone(); } }
 public override Task<AttendanceSnapshot> Read(string path) { lock(gate){ Reads++; if(path=="students/"+FailedStudent){if(TransportFailure)throw new HttpRequestException("Shared outage");throw new AttendanceSetupException("Invalid student mapping");} return Task.FromResult(new AttendanceSnapshot{Data=Peek(path),Etag="\""+Revision+"\""}); } }
 public override Task<bool> SaveIfUnchanged(string path,JObject row,string etag) {
  if(SaveBarrier!=null && !SaveBarrier.SignalAndWait(5000))throw new Exception("Test barrier timeout");
  lock(gate){if(Conflicts>0){Conflicts--;return Task.FromResult(false);} if(etag!="\""+Revision+"\"")return Task.FromResult(false);Put(path,row);Writes++;if(LoseAcknowledgment)throw new HttpRequestException("Isolated lost acknowledgment");return Task.FromResult(true);}
 }
}
sealed class Fixture {
 public readonly MemoryFirebase Db=new MemoryFirebase(); public readonly AttendanceService Service;
 public readonly string Student="s_"+Guid.NewGuid().ToString("N"), Supervisor="p_"+Guid.NewGuid().ToString("N"), Assignment="a_"+Guid.NewGuid().ToString("N");
 public readonly string Company="c_"+Guid.NewGuid().ToString("N");
 public string Date=AttendanceService.Today(); public long Now=AttendanceService.Now();
 public string RecordPath {get{return "attendance/"+Assignment+"/"+Date;}}
 public static readonly byte[] Key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
 public Fixture(){ Service=new AttendanceService(Db,()=>Key); Db.Put("settings",new JObject{["enabled"]=true});
  Db.Put("students/"+Student,new JObject{["name"]="Isolated student",["status"]="active",["firebaseUid"]="student-uid",["activeAssignmentId"]=Assignment});
  Db.Put("companies/"+Company,new JObject{["name"]="Isolated company",["status"]="active"});
  Db.Put("companySupervisors/"+Supervisor,new JObject{["name"]="Isolated supervisor",["status"]="active",["companyId"]=Company,["firebaseUid"]="supervisor-uid"});
  Db.Put("trainingAssignments/"+Assignment,new JObject{["studentId"]=Student,["companyId"]=Company,["companySupervisorId"]=Supervisor,["status"]="active",["startDate"]=Date,["endDate"]=Date,["requiredHours"]=160,["academicSupervisorId"]="AC"});
  Db.Put("schedules/"+Assignment+"/"+Date,new JObject{["assignmentId"]=Assignment,["attendanceDate"]=Date,["timeZone"]="Asia/Amman",["version"]=1,["isTrainingDay"]=true,["scheduledStartAt"]=Now-600000,["scheduledEndAt"]=Now+600000});
 }
 public async Task<string> Issue(string action="checkIn") { var r=await Service.IssueStudentQrs(Supervisor,new JArray(Student),action,false,"supervisor-uid"); if(((JArray)r["issued"]).Count!=1)throw new InvalidOperationException("Issue rejected: "+r["failed"]); return (string)(await Service.ReadIssuedQr(Student,action,false))["token"]; }
 public JObject BoundRow(){return new JObject{["studentId"]=Student,["assignmentId"]=Assignment,["companyId"]=Company,["companySupervisorId"]=Supervisor,["attendanceDate"]=Date};}
 public void CheckedIn(){ var r=BoundRow(); r["checkInAt"]=AttendanceService.Now()-125000; r["status"]="checkedIn"; r["hoursApprovalStatus"]="pending";Db.Put(RecordPath,r); }
 public void ShiftEnded(){var s=Db.Peek("schedules/"+Assignment+"/"+Date);s["scheduledEndAt"]=AttendanceService.Now()-1000;Db.Put("schedules/"+Assignment+"/"+Date,s);}
 public JObject Payload(){long n=AttendanceService.Now();return new JObject{["studentId"]=Student,["assignmentId"]=Assignment,["companyId"]=Company,["supervisorId"]=Supervisor,["date"]=Date,["action"]="checkIn",["version"]=1,["issuedAt"]=n,["expiresAt"]=n+120000,["localDemo"]=false,["nonce"]=Guid.NewGuid().ToString("N")};}
}
static partial class Program {
 static int passed,failed;
 static void Assert(bool x){if(!x)throw new Exception("Assertion failed");}
 static async Task Test(string name,Func<Task> run){HttpContext.Current=null;try{await run();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.GetType().Name+" "+e.Message);}}
 static async Task Reject(Func<Task> run){try{await run();}catch(InvalidOperationException){return;}catch(FirebaseException){return;}throw new Exception("Expected safe rejection");}
 static AttendanceService Service(Fixture f){return new AttendanceService(f.Db,()=>Fixture.Key);}
 static async Task Run(){ await IntegrationTests();  await Test("verified unassigned students receive own profile without database writes",async()=>{foreach(string id in new[]{"student-a","student-b","student-c","student-d","student-e"}){var f=new Fixture();var p=new JObject{["studentId"]=id,["role"]="student",["isActive"]=true,["accountSetupStatus"]="complete",["fullName"]="Verified own name"};var actor=await f.Service.Identity("student",id,"verified-own-uid",false,p);var view=await University_Internship_Management_System.Student.MyAttendance.LoadAttendanceData(f.Service,id,actor);Assert(!(bool)view["hasAssignment"]&&(string)view["student"]["studentNumber"]==id&&f.Db.Writes==0&&((JArray)view["history"]).Count==0);}});
  await Test("missing actor never bypasses old identity validation",async()=>{var f=new Fixture();await Reject(()=>f.Service.Identity("student","missing","uid",false));});
  await Test("display-only mapping rejects another business ID",async()=>{var f=new Fixture();var p=new JObject{["studentId"]="other",["role"]="student",["isActive"]=true,["accountSetupStatus"]="complete"};await Reject(()=>f.Service.Identity("student","missing","uid",false,p));});
  await Test("display-only mapping rejects unverified UID",async()=>{var f=new Fixture();var p=new JObject{["studentId"]="missing",["role"]="student",["isActive"]=true,["accountSetupStatus"]="complete"};await Reject(()=>f.Service.Identity("student","missing","",false,p));});
  await Test("display-only mapping rejects inactive or incomplete accounts",async()=>{foreach(string mode in new[]{"inactive","demoOnly","passwordChange","wrongRole"}){var f=new Fixture();var p=new JObject{["studentId"]="missing",["role"]=mode=="wrongRole"?"companySupervisor":"student",["isActive"]=mode!="inactive",["accountSetupStatus"]=mode=="demoOnly"?"demoOnly":"complete",["mustChangePassword"]=mode=="passwordChange"};await Reject(()=>f.Service.Identity("student","missing","uid",false,p));}});
  await Test("display fallback never overrides an existing UID mismatch",async()=>{var f=new Fixture();var p=new JObject{["studentId"]=f.Student,["role"]="student",["isActive"]=true,["accountSetupStatus"]="complete"};await Reject(()=>f.Service.Identity("student",f.Student,"wrong-uid",false,p));});
  await Test("display fallback never revives an inactive attendance actor",async()=>{var f=new Fixture();var a=f.Db.Peek("students/"+f.Student);a["status"]="inactive";f.Db.Put("students/"+f.Student,a);var p=new JObject{["studentId"]=f.Student,["role"]="student",["isActive"]=true,["accountSetupStatus"]="complete"};await Reject(()=>f.Service.Identity("student",f.Student,"student-uid",false,p));});
  await Test("unassigned students cannot issue or request attendance",async()=>{var f=new Fixture();await Reject(()=>f.Service.RequestEarly("missing","Reason"));await Reject(()=>f.Service.ReadIssuedQr("missing","checkIn",false));Assert(f.Db.Writes==0);});
  await Test("real UID mapping remains mandatory",async()=>{var f=new Fixture();await f.Service.Identity("student",f.Student,"student-uid",false);await Reject(()=>f.Service.Identity("student",f.Student,"other-uid",false));});
  await Test("unapproved test identity cannot bypass Login",async()=>{var f=new Fixture();var s=f.Db.Peek("students/"+f.Student);s["isTestData"]=true;f.Db.Put("students/"+f.Student,s);await Reject(()=>f.Service.Identity("student",f.Student,"student-uid",false));});
  await Test("missing assignment remains a safe empty page",async()=>{var f=new Fixture();var s=f.Db.Peek("students/"+f.Student);s.Remove("activeAssignmentId");f.Db.Put("students/"+f.Student,s);var v=await University_Internship_Management_System.Student.MyAttendance.LoadAttendanceData(f.Service,f.Student);Assert(!(bool)v["hasAssignment"]);});
  await Test("issuance persists only state and no attendance event",async()=>{var f=new Fixture();var t=await f.Issue();var row=f.Db.Peek(f.RecordPath);Assert(row["qrState"]["checkIn"]["status"].ToString()=="issued"&&row["checkInAt"]==null&&f.Db.Writes==1);Assert(!f.Db.Root.ToString().Contains(t)&&row["qrState"]["checkIn"]["signature"]==null&&t.StartsWith("UIMSATT1:P:"));});
  await Test("fresh service instance recovers same token without Cache",async()=>{var f=new Fixture();var t=await f.Issue();Assert(t==(string)(await Service(f).ReadIssuedQr(f.Student,"checkIn",false))["token"]);});
  await Test("new service instance can consume persisted QR",async()=>{var f=new Fixture();var t=await f.Issue();var r=await Service(f).ScanIssuedQr(f.Supervisor,t,false);Assert((long)r["record"]["checkInAt"]>0&&r["record"]["qrState"]==null);});
  await Test("QR metadata excluded from page and history",async()=>{var f=new Fixture();await f.Issue();var v=await University_Internship_Management_System.Student.MyAttendance.LoadAttendanceData(f.Service,f.Student);Assert(v["record"]["qrState"]==null&&((JArray)v["history"]).Count==0);});
  await Test("HMAC rejects edited token",async()=>{var f=new Fixture();var t=await f.Issue();int n=t.IndexOf('.')+3;var changed=t.Substring(0,n)+(t[n]=='A'?'B':'A')+t.Substring(n+1);await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,changed,false));Assert(f.Db.Peek(f.RecordPath)["checkInAt"]==null);});
  await Test("different server signing key cannot validate token",async()=>{var f=new Fixture();var t=await f.Issue();var other=new AttendanceService(f.Db,()=>Enumerable.Repeat((byte)42,32).ToArray());await Reject(()=>other.ScanIssuedQr(f.Supervisor,t,false));});
  await Test("reissue invalidates earlier nonce",async()=>{var f=new Fixture();var a=await f.Issue();var b=await f.Issue();Assert(a!=b);await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,a,false));await f.Service.ScanIssuedQr(f.Supervisor,b,false);});
  await Test("expired persisted QR is rejected",async()=>{var f=new Fixture();var t=await f.Issue();var r=f.Db.Peek(f.RecordPath);r["qrState"]["checkIn"]["issuedAt"]=AttendanceService.Now()-121000;r["qrState"]["checkIn"]["expiresAt"]=AttendanceService.Now()-1000;f.Db.Put(f.RecordPath,r);await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,t,false));});
  await Test("wrong supervisor cannot consume",async()=>{var f=new Fixture();var t=await f.Issue();await Reject(()=>f.Service.ScanIssuedQr("other-supervisor",t,false));Assert(f.Db.Peek(f.RecordPath)["checkInAt"]==null);});
  await Test("schedule change invalidates existing QR",async()=>{var f=new Fixture();var t=await f.Issue();var s=f.Db.Peek("schedules/"+f.Assignment+"/"+f.Date);s["version"]=2;f.Db.Put("schedules/"+f.Assignment+"/"+f.Date,s);await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,t,false));});
  await Test("check-in and QR consumption share one ETag write",async()=>{var f=new Fixture();var t=await f.Issue();int before=f.Db.Writes;await f.Service.ScanIssuedQr(f.Supervisor,t,false);var r=f.Db.Peek(f.RecordPath);Assert(f.Db.Writes==before+1&&(long)r["checkInAt"]>0&&(string)r["qrState"]["checkIn"]["status"]=="consumed");await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,t,false));});
  await Test("duplicate check-in cannot be reissued",async()=>{var f=new Fixture();var t=await f.Issue();await f.Service.ScanIssuedQr(f.Supervisor,t,false);await Reject(()=>f.Issue());});
  await Test("ETag conflict retries without duplicate event",async()=>{var f=new Fixture();var t=await f.Issue();f.Db.Conflicts=1;await f.Service.ScanIssuedQr(f.Supervisor,t,false);Assert(f.Db.Writes==2);});
  await Test("exhausted conflicts do not partially consume QR",async()=>{var f=new Fixture();var t=await f.Issue();f.Db.Conflicts=3;await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,t,false));var r=f.Db.Peek(f.RecordPath);Assert((string)r["qrState"]["checkIn"]["status"]=="issued"&&r["checkInAt"]==null);});
  await Test("two concurrent scans save one event",async()=>{var f=new Fixture();var t=await f.Issue();f.Db.SaveBarrier=new Barrier(2);Func<Task<bool>> scan=async()=>{try{await Service(f).ScanIssuedQr(f.Supervisor,t,false);return true;}catch(InvalidOperationException){return false;}};var r=await Task.WhenAll(Task.Run(scan),Task.Run(scan));Assert(r.Count(x=>x)==1&&f.Db.Writes==2);});
  await Test("lost acknowledgment leaves event and consumption consistent",async()=>{var f=new Fixture();var t=await f.Issue();f.Db.LoseAcknowledgment=true;try{await f.Service.ScanIssuedQr(f.Supervisor,t,false);throw new Exception("Expected transport failure");}catch(HttpRequestException){}f.Db.LoseAcknowledgment=false;var r=f.Db.Peek(f.RecordPath);Assert((long)r["checkInAt"]>0&&(string)r["qrState"]["checkIn"]["status"]=="consumed");await Reject(()=>f.Service.ScanIssuedQr(f.Supervisor,t,false));});
  await Test("flexible checkout uses actual minutes and requires explicit hours approval",async()=>{var f=new Fixture();f.CheckedIn();var r=await f.Service.ScanIssuedQr(f.Supervisor,await f.Issue("checkOut"),false);Assert((string)r["record"]["status"]=="completed"&&(long)r["record"]["workedMinutes"]==2);Assert((await f.Service.LoadStudentAttendance(f.Student,null,null)).ApprovedMinutes==0);await f.Service.Review(f.Supervisor,f.Assignment,f.Date,"approveHours");Assert((await f.Service.LoadStudentAttendance(f.Student,null,null)).ApprovedMinutes==2);});
  await Test("normal checkout and pending hours remain valid",async()=>{var f=new Fixture();f.CheckedIn();f.ShiftEnded();var r=await f.Service.ScanIssuedQr(f.Supervisor,await f.Issue("checkOut"),false);Assert((string)r["record"]["status"]=="completed"&&(string)r["record"]["hoursApprovalStatus"]=="pending"&&(long)r["record"]["workedMinutes"]==2);await Reject(()=>f.Issue("checkOut"));});
  await Test("invalid key prevents persistence",async()=>{var f=new Fixture();var service=new AttendanceService(f.Db,()=>null);await Reject(()=>service.IssueStudentQrs(f.Supervisor,new JArray(f.Student),"checkIn",false,"supervisor-uid"));Assert(f.Db.Writes==0);});
  await Test("no approved manifest means no write scope",()=>{var c=new AttendanceConfiguration(k=>null);return Reject(()=>{c.EnsureWriteScope("attendance/a/2026-10-23",new JObject{["isTestData"]=true});return Task.CompletedTask;});});
  await Test("integration manifest restricts actor UID and exact paths",()=>{var dir=Path.Combine(Path.GetTempPath(),"scope-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path="attendance/test-assignment/"+AttendanceService.Today();string backup=Path.Combine(dir,"backup.json"),mf=Path.Combine(dir,"manifest.json");File.WriteAllText(backup,new JObject{["projectId"]="just-project-4af15",["snapshots"]=new JObject{[path]=new JObject{["existed"]=false}}}.ToString());File.WriteAllText(mf,new JObject{["projectId"]="just-project-4af15",["databaseUrl"]="https://just-project-4af15-default-rtdb.firebaseio.com",["students"]=new JObject{["test-student"]="verified-test-uid"},["supervisors"]=new JObject{["test-supervisor"]="verified-supervisor-uid"},["assignments"]=new JArray("test-assignment"),["companies"]=new JArray("test-company"),["allowedWrites"]=new JArray(path),["backupFile"]=backup}.ToString());var c=new AttendanceConfiguration(k=>k=="AttendanceIntegrationManifestFile"?mf:k=="FirebaseProjectId"?"just-project-4af15":k=="FirebaseDatabaseUrl"?"https://just-project-4af15-default-rtdb.firebaseio.com":null);Assert(c.IsAuthorizedIntegrationActor("student","test-student","verified-test-uid"));Assert(!c.IsAuthorizedIntegrationActor("student","test-student","wrong-uid"));var row=new JObject{["assignmentId"]="test-assignment",["attendanceDate"]=AttendanceService.Today(),["studentId"]="test-student",["companySupervisorId"]="test-supervisor",["companyId"]="test-company",["isTestData"]=true};c.EnsureWriteScope(path,row);Reject(()=>{c.EnsureWriteScope(path+"-other",row);return Task.CompletedTask;}).GetAwaiter().GetResult();row["isTestData"]=false;Reject(()=>{c.EnsureWriteScope(path,row);return Task.CompletedTask;}).GetAwaiter().GetResult();return Task.CompletedTask;});
 }
 static int Main(){try{Run().GetAwaiter().GetResult();}catch(Exception e){failed++;Console.WriteLine(e.GetType().Name+" "+e.Message);}Console.WriteLine("RESULT "+passed+" passed; "+failed+" failed; mocked Firebase only.");return failed==0?0:1;}
}
