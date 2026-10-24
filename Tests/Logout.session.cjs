const vm=require('vm'),fs=require('fs'),assert=require('assert');
async function scenario(expired,fail){
 let saved=JSON.stringify({idToken:'token',refreshToken:'refresh',tabTicket:'signed-tab',expiresAt:Date.now()+(expired?-1000:600000)}),calls=[],destination,click;
 const document={currentScript:{src:'https://localhost/Js_project/firebase-client.js'},querySelector:()=>null,getElementById:()=>null,addEventListener:(name,fn,capture)=>{if(name==='click'){assert(capture);click=fn;}}};
 const context={document,location:{href:'https://localhost/Company/ManageTasks',pathname:'/Company/ManageTasks',search:'',replace:u=>destination=u},sessionStorage:{getItem:()=>saved,setItem:(k,v)=>saved=v,removeItem:()=>saved=null},URL,URLSearchParams,Headers,Request,AbortController,performance,setTimeout,clearTimeout,setInterval,clearInterval,Event,fetch:async(u,o)=>{calls.push(JSON.parse(o.body).action);assert.equal(new Headers(o.headers).get('X-UIMS-Session'),'signed-tab');if(fail)throw Error('offline');return {ok:true,text:async()=>JSON.stringify({success:true,data:{signedOut:true}})};}};
 context.window=context;context.window.dispatchEvent=()=>{};
 vm.runInNewContext(fs.readFileSync('University-Internship-Management-System/Js_project/firebase-client.js','utf8'),context);
 await context.UimsFirebase.logout();
 assert.deepEqual(calls,['logout']);assert.equal(saved,null);assert(destination.endsWith('Auth/Login.aspx?account=new'));
 assert(click);
}
(async()=>{await scenario(false,false);await scenario(true,false);await scenario(true,true);console.log('PASS logout clears tab session, sends its signed ticket, skips refresh even when expired, and avoids auto-login after success or network failure.');})().catch(e=>{console.error(e);process.exitCode=1;});
