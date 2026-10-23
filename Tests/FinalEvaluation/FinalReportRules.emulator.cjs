const fs=require('fs'),path=require('path'),cp=require('child_process'),http=require('http'),assert=require('assert');
const java=['C:/Program Files/Android/Android Studio/jbr/bin/java.exe','C:/Program Files/Android/Android Studio/jre/bin/java.exe'].find(fs.existsSync);
const jar=path.join(process.env.USERPROFILE,'.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar');
if(!java||!fs.existsSync(jar)){console.error('BLOCKED: Firebase Database Emulator requires an installed Java runtime and emulator JAR.');process.exit(2);}
const port=19093,ns='demo-final-report-workflow';
function request(method,target,value,token){return new Promise((resolve,reject)=>{const req=http.request({hostname:'127.0.0.1',port,path:target+(target.includes('?')?'&':'?')+'ns='+ns,method,headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})}},res=>{let s='';res.on('data',b=>s+=b);res.on('end',()=>resolve({status:res.statusCode,body:s}));});req.on('error',reject);req.end(value==null?undefined:JSON.stringify(value));});}
const processJar=cp.spawn(java,['-jar',jar,'--host','127.0.0.1','--port',String(port)],{stdio:['ignore','pipe','pipe'],windowsHide:true});let log='';processJar.stdout.on('data',b=>log+=b);processJar.stderr.on('data',b=>log+=b);
(async()=>{let ready=false;for(let i=0;i<100;i++){try{await request('GET','/.json');ready=true;break;}catch{await new Promise(r=>setTimeout(r,100));}}assert(ready,'Emulator failed to start');const rules=JSON.parse(fs.readFileSync(path.resolve(__dirname,'../../University-Internship-Management-System/final-report-rules.fragment.json'),'utf8'));rules.rules['.read']=false;rules.rules['.write']=false;assert.strictEqual((await request('PUT','/.settings/rules.json',rules,'owner')).status,200);
for(const target of ['/finalEvaluationWorkflows/P.json','/finalTrainingOutcomes/P.json']){assert.strictEqual((await request('GET',target)).status,401);assert.strictEqual((await request('PUT',target,{status:'completed'})).status,401);}
const uid='isolated-student',token=Buffer.from(JSON.stringify({alg:'none',typ:'JWT'})).toString('base64url')+'.'+Buffer.from(JSON.stringify({sub:uid,user_id:uid,iat:Math.floor(Date.now()/1000),exp:Math.floor(Date.now()/1000)+3600,aud:ns,iss:'https://securetoken.google.com/'+ns,firebase:{sign_in_provider:'custom',identities:{}}})).toString('base64url')+'.';
for(const [target,value] of [['/users/'+uid+'.json',{studentId:'ST',role:'student',isActive:true}],['/notifications/ST/N.json',{type:'trainingCompletion',studentId:'ST',placementId:'P',requestId:'REQ',message:'Congratulations'}],['/finalEvaluationWorkflows/P.json',{status:'pendingAdminReview',finalization:{requestId:'REQ'}}]])assert.strictEqual((await request('PUT',target,value,'owner')).status,200);
assert.strictEqual((await request('GET','/notifications/ST/N.json',null,token)).status,401);
assert.strictEqual((await request('PUT','/finalEvaluationWorkflows/P/status.json','completed','owner')).status,200);
assert.strictEqual((await request('GET','/notifications/ST/N.json',null,token)).status,401);
assert.strictEqual((await request('GET','/finalEvaluationWorkflows/P.json',null,token)).status,401);
assert.strictEqual((await request('PUT','/notifications/ST/N.json',{read:true},token)).status,401);
console.log('PASS isolated Firebase Emulator: workflow/outcomes/notifications stay private even after completion; authorized visibility uses server-filtered APIs only. No production access.');
})().catch(e=>{console.error(e.message);process.exitCode=1;}).finally(()=>processJar.kill());
