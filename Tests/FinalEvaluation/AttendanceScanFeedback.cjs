const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const root=path.resolve(__dirname,'../../University-Internship-Management-System');
const source=fs.readFileSync(path.join(root,'Js_project/Company/verify-attendance.js'),'utf8');
const elements=new Map();
const el=id=>{if(!elements.has(id))elements.set(id,{dataset:{},hidden:true,textContent:'',value:'',dispatchEvent(){}});return elements.get(id)};
let timers=new Map(),next=0,calls=0,resolveRequest,rejectRequest,stops=0;
const context=vm.createContext({document:{getElementById:el},setTimeout(fn,ms){assert.equal(ms,1500);timers.set(++next,fn);return next},clearTimeout(id){timers.delete(id)},Date,Event:class{},console,
 scannerDiagnostic(){},stopCamera(){stops++},showStudent(){},showNotice(){},renderRequests(){},renderRecords(){},loadSupervisor(){},api(){calls++;return new Promise((resolve,reject)=>{resolveRequest=resolve;rejectRequest=reject})}});
vm.runInContext('let saving=false,lastToken="",lastTokenAt=0,dataVersion=0,supervisorData=null,loading=false,refreshAfterLoad=false;'+source.slice(source.indexOf('let scanFeedbackTimer'),source.indexOf('function showStudent(')),context);
const run=code=>vm.runInContext(code,context);
function expire(){const callbacks=[...timers.values()];timers.clear();callbacks.forEach(fn=>fn());}
(async()=>{
 let pending=run('processToken("UIMSATT1:first")');assert.equal(calls,1);assert.equal(el('scanFeedback').dataset.state,'checking');
 await run('processToken("UIMSATT1:second")');assert.equal(calls,1,'single in-flight request');
 resolveRequest({action:'checkIn'});await pending;assert.equal(el('scanFeedbackTitle').textContent,'Check-in recorded successfully');assert.equal(stops,1);expire();assert(el('scanFeedback').hidden);
 pending=run('processToken("UIMSATT1:checkout")');resolveRequest({action:'checkOut'});await pending;assert.equal(el('scanFeedbackTitle').textContent,'Check-out recorded successfully');expire();
 pending=run('processToken("invalid-qr")');rejectRequest(new Error('QR has expired. Request a new QR.'));await pending;
 assert.equal(el('scanFeedbackDescription').textContent,'QR has expired. Request a new QR.');assert.equal(stops,2,'validation errors never stop camera');
 const before=calls;await run('processToken("another-qr")');assert.equal(calls,before,'pause submissions during error feedback');expire();assert(el('scanFeedback').hidden);assert.equal(run('scanFeedbackBlocking'),false);
 pending=run('processToken("another-qr")');assert.equal(calls,before+1,'can scan next QR without reopening');rejectRequest(new Error('Not assigned'));await pending;expire();
 await run('processToken("another-qr")');assert.equal(calls,before+1,'same QR throttled');
 run('setScanFeedback("error",null,"<img onerror=alert(1)>")');assert.equal(el('scanFeedbackDescription').textContent,'<img onerror=alert(1)>');run('setScanFeedback("idle")');assert.equal(timers.size,0,'old timer cancelled');
 assert(source.indexOf('setScanFeedback("success", result.action)')>source.indexOf('const result = await api("scan", { token: token })'));
 console.log('PASS: backend-confirmed check-in/out, 1500ms feedback, actual error text, active camera on validation error, automatic recovery, in-flight lock, duplicate throttle, safe text and timer cancellation. Mocked API only.');
})().catch(e=>{console.error(e);process.exitCode=1});
