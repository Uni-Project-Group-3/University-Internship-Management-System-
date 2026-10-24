using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;
class LinkOnlyTests {
 class Handler:HttpMessageHandler {public int Calls;public bool Reject;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c){Calls++;return Task.FromResult(new HttpResponseMessage(Reject?HttpStatusCode.BadRequest:HttpStatusCode.OK){Content=new StringContent(Reject?"{}":"{\"localId\":\"student\",\"idToken\":\"fake\",\"refreshToken\":\"fake\",\"expiresIn\":\"3600\"}")});}}
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Main(){Run().GetAwaiter().GetResult();}
 static async Task Run(){
 var h=new Handler();var profile=new JObject{["role"]="student",["isActive"]=true,["mustChangePassword"]=true};
 var config=new UIMS.Services.FirebaseConfiguration("true","test","https://test.firebaseio.com","fake",authenticationEnabled:"true");
 var svc=new FirebaseSignInService(new HttpClient(h),"fake",t=>Task.FromResult("student"),u=>Task.FromResult(profile),config);
 var result=await svc.SignInAsync("test@example.invalid","same-password");Check(!result.MustChangePassword&&result.Role=="student"&&h.Calls==1,"successful login ignores stale first-login flag without timestamp heuristics");
 result=await svc.VerifySessionAsync("fake");Check(!result.MustChangePassword,"verified session uses the same link-only policy");
 h.Reject=true;bool denied=false;try{await svc.SignInAsync("test@example.invalid","wrong");}catch(FirebaseLoginException){denied=true;}Check(denied,"wrong password remains rejected");
 foreach(var role in new[]{"student","academicSupervisor","companySupervisor","coordinator"}){var p=(JObject)profile.DeepClone();p["role"]=role;var mapped=FirebaseSignInService.ProfileResult("user",p);Check(!mapped.MustChangePassword&&mapped.Profile.Value<bool?>("mustChangePassword")==false,role+" uses link-only routing in result and profile");}
 profile["isActive"]=false;denied=false;try{FirebaseSignInService.ProfileResult("student",profile);}catch(FirebaseLoginException){denied=true;}Check(denied,"inactive account remains rejected");
 }
}
