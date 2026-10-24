const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert/strict');
const source=fs.readFileSync(path.resolve(__dirname,'../../University-Internship-Management-System/Js_project/firebase-client.js'),'utf8');
const wrapper=source.slice(source.indexOf('    const originalFetch ='),source.indexOf('    if (form) { const other'));
(async()=>{
 for(const page of ['Student/MyAttendance','Company/VerifyAttendance.aspx']){
  let captured;
  const context=vm.createContext({URL,Headers,Request,location:{href:'https://localhost:44372/'+page,pathname:'/'+page},endpoint:new URL('https://localhost:44372/FirebaseApi.ashx'),load:()=>({tabTicket:'signed-tab-session'}),getIdToken:async()=>{throw Error('Attendance must not refresh through this wrapper');},window:{fetch:async(input,options)=>{captured=options;return{ok:true};},dispatchEvent(){}},Event});
  vm.runInContext(wrapper,context);
  await context.window.fetch('https://localhost:44372/'+page+'?attendanceApi=1',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/x-www-form-urlencoded'}});
  assert.equal(captured.headers.get('X-UIMS-Session'),'signed-tab-session');
  assert.equal(captured.headers.get('Content-Type'),'application/x-www-form-urlencoded');
  assert.equal(captured.credentials,'same-origin');
  await context.window.fetch('https://another.invalid/Company/VerifyAttendance?attendanceApi=1',{method:'POST'});
  assert.equal(captured.headers,undefined,'Never send a session to another origin');
 }
 console.log('PASS student/company attendance carries signed tab session; same-origin restriction and existing request headers preserved. No network.');
})().catch(e=>{console.error(e);process.exitCode=1;});
