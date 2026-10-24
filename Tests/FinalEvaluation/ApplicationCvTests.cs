using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
internal static partial class Program {
 private static async Task RunApplicationCvTests() {
  await Test("CV attachments are limited to the owner student and receiving company", () => {
   var application=new JObject {["studentUid"]="UID-1",["companyId"]="CO-1"};
   Assert(ApplicationCv.CanDownload("student","UID-1",new JObject(),application));
   Assert(!ApplicationCv.CanDownload("student","UID-OTHER",new JObject(),application));
   Assert(ApplicationCv.CanDownload("companySupervisor","CS-1",new JObject {["companyId"]="CO-1"},application));
   Assert(!ApplicationCv.CanDownload("companySupervisor","CS-2",new JObject {["companyId"]="CO-2"},application));
   Assert(!ApplicationCv.CanDownload("academicSupervisor","AC-1",new JObject {["companyId"]="CO-1"},application));
   Assert(!ApplicationCv.CanDownload("coordinator","ADMIN",new JObject(),application));
   return Task.CompletedTask;
  });
  await Test("CV attachment access fails closed for inactive, missing and unbound accounts", () => {
   var application=new JObject {["studentUid"]="UID-1",["companyId"]="CO-1"};
   Assert(!ApplicationCv.CanDownload("student","UID-1",new JObject {["isActive"]=false},application));
   Assert(!ApplicationCv.CanDownload("student","UID-1",null,application));
   Assert(!ApplicationCv.CanDownload("companySupervisor","CS-1",new JObject(),application));
   Assert(!ApplicationCv.CanDownload("student","",new JObject(),new JObject()));
   Assert(!ApplicationCv.CanDownload("student","UID-1",new JObject(),null));
   return Task.CompletedTask;
  });
 }
}
