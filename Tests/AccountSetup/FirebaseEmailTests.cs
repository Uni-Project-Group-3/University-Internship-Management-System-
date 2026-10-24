using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
class FirebaseEmailTests
{
    class Handler : HttpMessageHandler
    {
        public HttpStatusCode Status=HttpStatusCode.OK;
        public int Calls; public string Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        { Calls++; Body=await request.Content.ReadAsStringAsync();return new HttpResponseMessage(Status){Content=new StringContent("{}")}; }
    }
    static void Check(bool value,string text){if(!value)throw new Exception(text);Console.WriteLine("PASS "+text);}
    static void Main(){Run().GetAwaiter().GetResult();}
    static async Task Run()
    {
        var type=Assembly.LoadFrom("University-Internship-Management-System.dll").GetType("University_Internship_Management_System.Shared.FirebaseAuthService");
        var handler=new Handler();
        var service=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{new HttpClient(handler),"fake-test-key"},null);
        var method=type.GetMethod("SendAccountSetupEmailAsync");
        Func<string,Task> send=email=>(Task)method.Invoke(service,new object[]{email});
        await send("person@uims-mail-check.org");Check(handler.Calls==1 && handler.Body.Contains("PASSWORD_RESET") && !handler.Body.Contains("password\""),"setup email uses Firebase reset link without a password in payload");
        handler.Status=(HttpStatusCode)429;bool rejected=false;
        try{await send("person@uims-mail-check.org");}catch(Exception){rejected=true;}Check(rejected,"throttled setup email is not reported as sent");
        handler.Status=HttpStatusCode.BadRequest;rejected=false;
        try{await send("person@uims-mail-check.org");}catch(Exception){rejected=true;}Check(rejected,"Firebase rejection preserves retry behavior");
        int calls=handler.Calls;rejected=false;
        try{await send("sample@example.invalid");}catch(Exception){rejected=true;}Check(rejected&&handler.Calls==calls,"sample email rejected before network request");
        handler.Status=HttpStatusCode.OK; await (Task)type.GetMethod("CompletePasswordResetAsync").Invoke(service,new object[]{"fake-one-time-code","TestOnlyPassword123!"});
        Check(handler.Body.Contains("oobCode") && handler.Body.Contains("newPassword"),"custom reset UI exchanges code and password with Firebase");
        handler.Status=HttpStatusCode.BadRequest; rejected=false;
        try { await (Task)type.GetMethod("CompletePasswordResetAsync").Invoke(service,new object[]{"expired-code","TestOnlyPassword123!"}); } catch(Exception) { rejected=true; }
        Check(rejected,"expired or reused reset link is rejected");
        handler.Status=(HttpStatusCode)429; await (Task)type.GetMethod("SendPasswordResetEmailAsync").Invoke(service,new object[]{"missing@example.com"});
        // Public recovery masks known account errors, exercised with the response below in production tests.
    }
}
