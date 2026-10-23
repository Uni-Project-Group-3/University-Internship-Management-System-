const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const file = require('node:path').resolve(__dirname, '../../University-Internship-Management-System/Js_project/Company/verify-attendance.js');
const source = fs.readFileSync(file, 'utf8');
const start = source.indexOf('async function api('), end = source.indexOf('\nfunction connected(', start);
assert(start >= 0 && end > start);
const missing = 'Sign in with your Firebase account to load attendance.';
const ok = data => ({ok:true, headers:{get:()=> 'application/json'}, json:async()=>({success:true,data:data || {student:{name:'Isolated test'}}})});
const denied = (message='Sign in required',status=401) => ({ok:false,status,headers:{get:()=> 'application/json'},json:async()=>({success:false,message})});
function setup({token=async()=>{throw Error(missing)},respond=async()=>ok(),demo=false}={}) {
 const calls=[];
 const sandbox={scannerDiagnostic:()=>{},URL,URLSearchParams,AbortController,TextDecoder,setTimeout:()=>1,clearTimeout:()=>{},
  document:{getElementById:id=>{assert.equal(id,'attendanceBootstrap');return {textContent:JSON.stringify({csrf:'isolated-csrf',localDemo:demo})}}},
  window:{location:{href:'http://localhost:55462/Company/VerifyAttendance.aspx'},UimsFirebase:{getIdToken:token}},
  fetch:async(url,options)=>{calls.push({url,options});assert.equal(new URL(url).origin,'http://localhost:55462');assert.equal(options.credentials,'same-origin');assert.equal(options.method,'POST');assert.equal(options.body.get('csrf'),'isolated-csrf');return respond(url,options,calls.length)}
 };
 const context=vm.createContext(sandbox);
 vm.runInContext('let serverSessionVerified=false;\n'+source.slice(start,end),context);
 return {context,calls,api:(...args)=>context.api(...args),verified:()=>vm.runInContext('serverSessionVerified',context)};
}
let passed=0;
async function test(name,run){await run();passed++;console.log('PASS '+name)}
(async()=>{
 await test('existing bearer load preserves Authorization, CSRF and credentials',async()=>{const t=setup({token:async()=>'isolated-id-token'});await t.api('load');assert.equal(t.calls[0].options.headers.Authorization,'Bearer isolated-id-token');assert.equal(t.verified(),false)});
 await test('existing bearer QR works without server-session preflight',async()=>{const t=setup({token:async()=>'isolated-id-token'});await t.api('qr',{attendanceAction:'checkIn'});assert.equal(t.calls.length,1)});
 await test('missing browser token permits read-only server-authenticated load attempt',async()=>{const t=setup();await t.api('load');assert.equal(t.calls.length,1);assert.equal(t.calls[0].options.headers.Authorization,undefined);assert.equal(t.calls[0].options.body.get('action'),'load');assert.equal(t.verified(),true)});
 await test('missing token blocks QR before successful server load',async()=>{const t=setup();await assert.rejects(t.api('qr'),{message:missing});assert.equal(t.calls.length,0)});
 await test('missing token blocks early departure before successful server load',async()=>{const t=setup();await assert.rejects(t.api('requestEarly',{reason:'Isolated'}),{message:missing});assert.equal(t.calls.length,0)});
 await test('successful server load permits subsequent QR request to server validation',async()=>{const t=setup();await t.api('load');await t.api('qr',{attendanceAction:'checkOut'});assert.equal(t.calls.length,2);assert.equal(t.calls[1].options.body.get('attendanceAction'),'checkOut');assert.equal(t.calls[1].options.headers.Authorization,undefined)});
 await test('successful server load permits early request with unchanged body',async()=>{const t=setup();await t.api('load');await t.api('requestEarly',{reason:'Isolated reason'});assert.equal(t.calls[1].options.body.get('reason'),'Isolated reason')});
 await test('server 401 is shown and does not authorize sensitive requests',async()=>{const t=setup({respond:async()=>denied('No server Firebase token')});await assert.rejects(t.api('load'),{message:'No server Firebase token'});await assert.rejects(t.api('qr'),{message:missing});assert.equal(t.calls.length,1);assert.equal(t.verified(),false)});
 await test('setup failure remains a setup failure without retry or bypass',async()=>{const t=setup({respond:async()=>denied('Attendance integration is disabled.',503)});await assert.rejects(t.api('load'),{message:'Attendance integration is disabled.'});assert.equal(t.calls.length,1);assert.equal(t.verified(),false)});
 await test('expired token refresh failure does not downgrade to server-session request',async()=>{const t=setup({token:async()=>{throw Error('Refresh rejected')}});await assert.rejects(t.api('load'),{message:'Refresh rejected'});assert.equal(t.calls.length,0)});
 await test('missing shared authentication helper does not silently fall back',async()=>{const t=setup();t.context.window.UimsFirebase=undefined;await assert.rejects(t.api('load'));assert.equal(t.calls.length,0)});
 await test('malformed empty token is rejected before fetch',async()=>{const t=setup({token:async()=>''});await assert.rejects(t.api('load'),/incomplete/);assert.equal(t.calls.length,0)});
 await test('server 403 clears prior verification and blocks next sensitive request',async()=>{const t=setup({respond:async(u,o,n)=>n===1?ok():denied('Forbidden',403)});await t.api('load');await assert.rejects(t.api('qr'),{message:'Forbidden'});await assert.rejects(t.api('requestEarly'),{message:missing});assert.equal(t.calls.length,2);assert.equal(t.verified(),false)});
 await test('failed manual refresh invalidates earlier server-session verification',async()=>{const t=setup({respond:async(u,o,n)=>n===1?ok():denied()});await t.api('load');await assert.rejects(t.api('load'));await assert.rejects(t.api('qr'));assert.equal(t.calls.length,2)});
 await test('partial progressive identity never authorizes actions before final success',async()=>{const lines=[JSON.stringify({type:'identity',data:{name:'Isolated'}}),JSON.stringify({success:true,data:{student:{name:'Isolated'}}})];let i=0;const t=setup({respond:async()=>({ok:true,headers:{get:()=> 'application/x-ndjson'},body:{getReader:()=>({read:async()=>i<lines.length?{done:false,value:new TextEncoder().encode(lines[i++]+'\n')}:{done:true},releaseLock(){}})}})});let partial=0;await t.api('load',null,()=>{partial++;assert.equal(t.verified(),false)});assert.equal(partial,1);assert.equal(t.verified(),true);assert.equal(t.calls.length,1)});
 await test('stream ending in error never verifies server session',async()=>{const line=JSON.stringify({success:false,message:'Stream denied'})+'\n';let read=false;const t=setup({respond:async()=>({ok:true,headers:{get:()=> 'application/x-ndjson'},body:{getReader:()=>({read:async()=>read?{done:true}:(read=true,{done:false,value:new TextEncoder().encode(line)}),releaseLock(){}})}})});await assert.rejects(t.api('load',null,()=>{}),{message:'Stream denied'});assert.equal(t.verified(),false)});
 await test('legacy server-approved demo path remains unchanged',async()=>{const t=setup({demo:true,token:async()=>{throw Error('Token should not be requested')}});await t.api('load');assert.equal(t.calls.length,1);assert.equal(t.verified(),false)});
 await test('network failure clears server-session verification',async()=>{const t=setup({respond:async(u,o,n)=>{if(n===1)return ok();throw Error('Isolated network failure')}});await t.api('load');await assert.rejects(t.api('qr'),/Cannot reach/);assert.equal(t.verified(),false)});
 for (const action of ['scan','issueQr','review']) {
 await test('supervisor '+action+' blocked before server load and accepted for server validation after it',async()=>{const t=setup();await assert.rejects(t.api(action));assert.equal(t.calls.length,0);await t.api('load');await t.api(action);assert.equal(t.calls.length,2)});
 }
 console.log(`RESULT ${passed} passed; 0 failed. All fetch calls are mocks; no network or Firebase access.`);
})().catch(e=>{console.error(e);process.exitCode=1});
