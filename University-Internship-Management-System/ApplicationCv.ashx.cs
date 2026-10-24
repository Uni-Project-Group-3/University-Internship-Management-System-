using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;
public sealed class ApplicationCv : HttpTaskAsyncHandler {
 public override async Task ProcessRequestAsync(HttpContext context) {
  context.Response.Cache.SetNoStore();context.Response.TrySkipIisCustomErrors=true;
  try {
   var identity=context.User == null ? null : context.User.Identity as FormsIdentity;
   if(identity==null || !identity.IsAuthenticated || identity.Ticket.Expired)throw new HttpException(401,"Sign in to access this attachment.");
   var gateway=new StudentFirebaseGateway();string uid=identity.Name;
   if(!await gateway.IsActiveAuthenticationUidAsync(uid))throw new HttpException(403,"This account is unavailable.");
   var profile=await gateway.ReadAsync("users/"+StudentManagementService.Key(uid)) as JObject;
   if(profile==null || (bool?)profile["isActive"]==false)throw new HttpException(403,"This account is unavailable.");
   string role=FirebaseSignInService.CanonicalRole((string)profile["role"]);
   var storage=new UIMS.Services.FirebaseFileStorageService(key=>ConfigurationManager.AppSettings[key]);
   if(context.Request.HttpMethod=="POST") {
    if(role!="student" || context.Request.Headers["X-UIMS-Attachment"]!="upload")throw new HttpException(403,"Student attachment upload is required.");
    var file=context.Request.Files["cv"];
    if(file==null || file.ContentLength<1 || file.ContentLength>UIMS.Services.FirebaseFileStorageService.MaximumFileBytes)throw new HttpException(400,"Choose a CV file up to 10 MiB.");
    string name=UIMS.Services.FirebaseFileStorageService.SanitizeFileName(file.FileName),ext=Path.GetExtension(name).ToLowerInvariant();
    string mime=ext==".pdf"?"application/pdf":ext==".doc"?"application/msword":ext==".docx"?"application/vnd.openxmlformats-officedocument.wordprocessingml.document":null;
    if(mime==null)throw new HttpException(400,"Choose a PDF, DOC or DOCX CV.");
    byte[] bytes;using(var output=new MemoryStream()){await file.InputStream.CopyToAsync(output);bytes=output.ToArray();}
    string id=UIMS.Services.FirebaseFileStorageService.CreateFileId();var stored=await storage.UploadAsync("uims/application-cvs/"+StudentManagementService.Key(uid)+"/"+id,name,mime,bytes,context.Response.ClientDisconnectedToken);
    try {await gateway.WriteAsync("applicationAttachments/"+id,new { ownerUid=uid, fileName=stored.FileName, objectPath=stored.ObjectPath, provider=stored.Provider, contentType=stored.ContentType, sha256=stored.Sha256 });}
    catch {await storage.DeleteObjectAsync(stored.ObjectPath,stored.Provider,System.Threading.CancellationToken.None);throw;}
    context.Response.ContentType="application/json";context.Response.Write(JsonConvert.SerializeObject(new { attachmentId=id,fileName=stored.FileName }));return;
   }
   if(context.Request.HttpMethod!="GET")throw new HttpException(405,"This method is unavailable.");
   string applicationId=StudentManagementService.Key(context.Request.QueryString["applicationId"]);
   var application=await gateway.ReadAsync("applications/"+applicationId) as JObject;
   if(!CanDownload(role,uid,profile,application))throw new HttpException(403,"This application is outside your account.");
   var attachment=await gateway.ReadAsync("applicationAttachments/"+StudentManagementService.Key((string)application["cvAttachmentId"])) as JObject;
   if(attachment==null || (string)attachment["ownerUid"]!=(string)application["studentUid"])throw new HttpException(404,"CV attachment unavailable.");
   byte[] content=await storage.DownloadAsync((string)attachment["objectPath"],(string)attachment["provider"],context.Response.ClientDisconnectedToken);
   using(var sha=System.Security.Cryptography.SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(content)).Replace("-","").ToLowerInvariant();if(!string.Equals(hash,(string)attachment["sha256"],StringComparison.OrdinalIgnoreCase))throw new HttpException(503,"CV attachment unavailable.");}
   context.Response.ContentType=(string)attachment["contentType"];context.Response.AddHeader("X-Content-Type-Options","nosniff");context.Response.AddHeader("Content-Disposition","attachment; filename*=UTF-8''"+Uri.EscapeDataString((string)attachment["fileName"]));context.Response.BinaryWrite(content);
  }catch(Exception error){context.Response.StatusCode=error is HttpException?((HttpException)error).GetHttpCode():error is UIMS.Services.FirebaseException?((UIMS.Services.FirebaseException)error).StatusCode:503;context.Response.SuppressFormsAuthenticationRedirect=true;context.Response.ContentType="application/json";context.Response.Write(JsonConvert.SerializeObject(new { error=error is HttpException?error.Message:"The private CV service is unavailable. Your file has not been attached. Please try again." }));}
 }
 public static bool CanDownload(string role,string uid,JObject profile,JObject application) {
  if(profile==null || application==null || string.IsNullOrWhiteSpace(uid) || (bool?)profile["isActive"]==false)return false;
  if(role=="student")return (string)application["studentUid"]==uid;
  return role=="companySupervisor" && !string.IsNullOrWhiteSpace((string)profile["companyId"]) && (string)application["companyId"]==(string)profile["companyId"];
 }
 public override bool IsReusable {get{return false;}}
}
