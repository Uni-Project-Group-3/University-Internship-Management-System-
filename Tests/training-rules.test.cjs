'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=path.resolve(__dirname,'..'),rules=JSON.parse(fs.readFileSync(path.join(dir,'firebase.training.rules.json'))),baseline=JSON.parse(fs.readFileSync(path.join(dir,'firebase.internship.rules.json')));
const accountDiff=structuredClone(rules.rules.users);for(const leaf of ['accountStatus','role','firebaseUid','displayName'])delete accountDiff.$profileId[leaf];assert.deepEqual(accountDiff,baseline.rules.users);assert.ok(rules.rules.usersByUid.$uid['.read'].startsWith('('+baseline.rules.usersByUid.$uid['.read']+') || '));const uidDiff=structuredClone(rules.rules.usersByUid);uidDiff.$uid['.read']=baseline.rules.usersByUid.$uid['.read'];assert.deepEqual(uidDiff,baseline.rules.usersByUid);
const copy=structuredClone(rules);delete copy.rules.cycle3Demo;copy.rules.users=baseline.rules.users;copy.rules.usersByUid=baseline.rules.usersByUid;assert.deepEqual(copy,baseline);assert.equal(rules.rules.users.$profileId['.read'],baseline.rules.users.$profileId['.read']);for(const leaf of ['accountStatus','role','firebaseUid','displayName'])assert.ok(rules.rules.users.$profileId[leaf]['.read'].includes("child('accountStatus').val() == 'active'"));assert.equal(rules.rules['.read'],false);assert.equal(rules.rules['.write'],false);console.log('PASS diff: scoped Cycle 3 and four owned active-trainee account leaves only; no root permission');
if(process.argv.includes('--compare-live')||process.argv.includes('--verify-live')){
 const mode=process.argv.includes('--verify-live')?'--verify-live':'--compare-live',file=process.argv[process.argv.indexOf(mode)+1];
 if(!file)throw Error('Rules snapshot path required.');
 const live=JSON.parse(fs.readFileSync(file,'utf8'));
 const same=require('node:util').isDeepStrictEqual;
 if(mode==='--verify-live')assert.ok(same(live,rules),'Published Rules differ from tested draft');
 else {const candidates=['firebase.workflow.baseline.rules.json','firebase.workflow.rules.json','firebase.internship.rules.json','firebase.training.previous.rules.json','firebase.training.rules.json'].map(name=>JSON.parse(fs.readFileSync(path.join(dir,name),'utf8')));assert.ok(candidates.some(candidate=>same(live,candidate)),'Unexpected live Rules; no publication allowed.');}
 console.log('PASS live Rules comparison');process.exit(0);
}
if(process.argv.includes('--static-only'))process.exit(0);
const host=process.env.FIREBASE_DATABASE_EMULATOR_HOST;if(!/^127\.0\.0\.1:\d+$/.test(host||''))throw Error('Local emulator required; never run against live data.');
const project='demo-uims-training',ns=project+'-default-rtdb',seconds=Math.floor(Date.now()/1000);
const jwt=uid=>[Buffer.from(JSON.stringify({alg:'none',typ:'JWT'})).toString('base64url'),Buffer.from(JSON.stringify({iss:'https://securetoken.google.com/'+project,aud:project,sub:uid,user_id:uid,iat:seconds,exp:seconds+3600,auth_time:seconds,firebase:{sign_in_provider:'password',identities:{}}})).toString('base64url'),''].join('.');
async function req(route,uid,method='GET',body,query={}){const u=new URL('http://'+host+'/'+route+'.json');u.searchParams.set('ns',ns);const headers={};if(uid==='owner')headers.Authorization='Bearer owner';else if(uid)u.searchParams.set('auth',jwt(uid));for(const [k,v]of Object.entries(query))u.searchParams.set(k,JSON.stringify(v));return fetch(u,{method,headers,body:body===undefined?undefined:JSON.stringify(body),signal:AbortSignal.timeout(10000)});}
let count=0;async function check(name,route,uid,allow,method='GET',body,query){const r=await req(route,uid,method,body,query),text=await r.text();if(allow)assert.equal(r.status,200,name+' HTTP '+r.status+' '+text);else assert.ok([401,403].includes(r.status),name+' HTTP '+r.status+' '+text);count++;console.log('PASS '+name);}
const base=(id,student)=>({id,liveVersion:1,companyId:'c1',...(student?{studentId:student,placementId:student==='s1'?'p1':'p2',academicSupervisorId:'as'}:{})});
function create(id,exam=false){
 const now=Date.now(),g={...base(id),createdBy:'cs',createdByName:'Supervisor',requestId:'fixture',title:'C# Fundamentals',description:'Work',studentIds:['s1','s2'],assignees:{s1:true,s2:true},opensAt:new Date(now-60000).toISOString(),closesAt:new Date(now+3600000).toISOString(),opensAtMs:now-60000,closesAtMs:now+3600000,status:'published',priority:'medium',week:1,maximum:10,allowExamAfterClosing:false,createdAt:new Date(now).toISOString()};
 const patch={['taskGroups/'+id]:g};for(const sid of ['s1','s2'])patch['taskAssignments/'+id+'-'+sid]={...base(id+'-'+sid,sid),groupId:id,status:'assigned',isRead:false,gradePublished:false,attemptCount:0,createdAt:new Date(now).toISOString()};
 if(exam){g.exam={id,title:'Exam',instructions:'Answer',questionCount:1,totalMarks:100,passing:50,startsAtMs:now+60000,endsAtMs:now+120000,duration:1,attempts:1,startsAt:new Date(now+60000).toISOString(),endsAt:new Date(now+120000).toISOString(),policy:'close'};
 patch['examKeys/'+id]={...base(id),groupId:id,answers:{q1:0}};
 patch['examQuestions/'+id]={...base(id),groupId:id,questions:[{id:'q1',type:'mcq',text:'Question',marks:100,choices:['A','B']}],byId:{q1:{choiceCount:2,marks:100}}};}
 return patch;
}
function start(gid){const tid=gid+'-s1',id=tid+'-1',now=Date.now();return {id,patch:{['examAttempts/'+id]:{...base(id,'s1'),taskId:tid,groupId:gid,number:1,status:'active',startedAtMs:now,expiresAtMs:now+30000,startedAt:new Date(now).toISOString()},['taskAssignments/'+tid+'/attemptCount']:1,['taskAssignments/'+tid+'/lastAttemptId']:id}};}
(async()=>{
const fixture={usersByUid:{company:'cs',other:'cs2',student:'s1',student2:'s2',academic:'as',foreignAcademic:'as2',disabled:'off'},users:{cs:{firebaseUid:'company',role:'companySupervisor',accountStatus:'active',companyId:'c1'},cs2:{firebaseUid:'other',role:'companySupervisor',accountStatus:'active',companyId:'c2'},s1:{firebaseUid:'student',role:'student',accountStatus:'active'},s2:{firebaseUid:'student2',role:'student',accountStatus:'active'},as:{firebaseUid:'academic',role:'academicSupervisor',accountStatus:'active',programId:'CS'},as2:{firebaseUid:'foreignAcademic',role:'academicSupervisor',accountStatus:'active',programId:'CS'},off:{firebaseUid:'disabled',role:'student',accountStatus:'disabled'}},students:{s1:{id:'s1',academicSupervisorId:'as',programId:'CS',placementId:'p1'},s2:{id:'s2',academicSupervisorId:'as',programId:'CS',placementId:'p2'}},placements:{p1:{id:'p1',studentId:'s1',companyId:'c1',academicSupervisorId:'as',status:'active',approvedHours:120,requiredHours:240},p2:{id:'p2',studentId:'s2',companyId:'c1',academicSupervisorId:'as',status:'active',approvedHours:80,requiredHours:240}}};
assert.equal((await req('','owner','PUT',fixture)).status,200);const compiled=await req('.settings/rules','owner','PUT',rules);assert.equal(compiled.status,200,'Rules compile: '+await compiled.text());
await check('company reads active own trainee status','users/s1/accountStatus','company',true);
await check('company reads active own trainee UID mapping','usersByUid/student','company',true);
await check('company cannot read full student account','users/s1','company',false);
await check('foreign company cannot read trainee status','users/s1/accountStatus','other',false);
await req('users/s2/accountStatus','owner','PUT','disabled');
await check('inactive trainee metadata denied','users/s2/accountStatus','company',false);
await check('inactive trainee mapping denied','usersByUid/student2','company',false);
await check('inactive trainee task create denied','cycle3Demo','company',false,'PATCH',create('LIVE-inactive'));
await req('users/s2/accountStatus','owner','PUT','active');
await req('usersByUid/student2','owner','DELETE');
await check('unmapped trainee task create denied','cycle3Demo','company',false,'PATCH',create('LIVE-unmapped'));
await req('usersByUid/student2','owner','PUT','s2');
await check('company atomic multi-student task create','cycle3Demo','company',true,'PATCH',create('LIVE-g1'));
await check('own assignment query','cycle3Demo/taskAssignments','student',true,'GET',undefined,{orderBy:'studentId',equalTo:'s1'});
await check('foreign student query denied','cycle3Demo/taskAssignments','student',false,'GET',undefined,{orderBy:'studentId',equalTo:'s2'});
await check('company owned query','cycle3Demo/taskAssignments','company',true,'GET',undefined,{orderBy:'companyId',equalTo:'c1'});
await check('cross-company query denied','cycle3Demo/taskAssignments','other',false,'GET',undefined,{orderBy:'companyId',equalTo:'c1'});
await check('unfiltered training assignments denied','cycle3Demo/taskAssignments','company',false);
for(const uid of [null,'disabled','other'])await check('assignment read denied '+uid,'cycle3Demo/taskAssignments/LIVE-g1-s1',uid,false);
await check('student cannot self-publish','cycle3Demo/taskAssignments/LIVE-g1-s1/gradePublished','student',false,'PUT',true);
await check('student read flag is separate from status','cycle3Demo/taskAssignments/LIVE-g1-s1/isRead','student',true,'PUT',true);
await check('student cannot reassign record','cycle3Demo/taskAssignments/LIVE-g1-s1/studentId','student',false,'PUT','s2');
await check('submission status without submission denied','cycle3Demo/taskAssignments/LIVE-g1-s1/status','student',false,'PUT','submitted');
const submission={...base('SUB-1','s1'),taskId:'LIVE-g1-s1',description:'Work',link:'https://example.test/work',createdAt:new Date().toISOString(),revision:false};
await check('owned submission plus state is atomic','cycle3Demo','student',true,'PATCH',{'taskSubmissions/SUB-1':submission,'taskAssignments/LIVE-g1-s1/status':'submitted','taskAssignments/LIVE-g1-s1/lastSubmissionId':'SUB-1'});
await check('other student cannot inspect submission','cycle3Demo/taskSubmissions/SUB-1','student2',false);
const grade={...base('LIVE-g1-s1','s1'),taskId:'LIVE-g1-s1',score:8,maximum:10,feedback:'Reviewed',reviewedBy:'cs',reviewedAt:new Date().toISOString()};
await check('company practical evaluation','cycle3Demo','company',true,'PATCH',{'taskGrades/LIVE-g1-s1':grade,'taskAssignments/LIVE-g1-s1/status':'completed'});
await check('hidden practical result denied','cycle3Demo/taskGrades/LIVE-g1-s1','student',false);
await check('company publishes one practical result','cycle3Demo/taskAssignments/LIVE-g1-s1/gradePublished','company',true,'PUT',true);
await check('published own practical score readable','cycle3Demo/taskGrades/LIVE-g1-s1','student',true);
await check('foreign practical result denied','cycle3Demo/taskGrades/LIVE-g1-s1','student2',false);
await check('company atomic exam create','cycle3Demo','company',true,'PATCH',create('LIVE-g2',true));
await check('answer key never student-readable','cycle3Demo/examKeys/LIVE-g2','student',false);
await check('questions locked before schedule','cycle3Demo/examQuestions/LIVE-g2','student',false);
await check('start before schedule denied','cycle3Demo','student',false,'PATCH',start('LIVE-g2').patch);
const now=Date.now();await req('cycle3Demo/taskGroups/LIVE-g2/exam','owner','PATCH',{startsAtMs:now-1000,endsAtMs:now+59000});
const attempt=start('LIVE-g2');await check('start authorized at server time','cycle3Demo','student',true,'PATCH',attempt.patch);
await check('authorized questions contain no key','cycle3Demo/examQuestions/LIVE-g2','student',true);
await check('own answer saves','cycle3Demo/examAttempts/'+attempt.id+'/answers/q1','student',true,'PUT',0);
await check('invalid answer index denied','cycle3Demo/examAttempts/'+attempt.id+'/answers/q1','student',false,'PUT',2);
await check('other student answer access denied','cycle3Demo/examAttempts/'+attempt.id,'student2',false);
await check('attempt expiry cannot be changed','cycle3Demo/examAttempts/'+attempt.id+'/expiresAtMs','student',false,'PUT',Date.now()+86400000);
await check('student cannot forge result','cycle3Demo/examResults/'+attempt.id,'student',false,'PUT',{score:100});
await check('submit freezes saved answers','cycle3Demo','student',true,'PATCH',{['examAttempts/'+attempt.id+'/status']:'submitted',['examAttempts/'+attempt.id+'/submittedAtMs']:Date.now()});
await check('answer after submit denied','cycle3Demo/examAttempts/'+attempt.id+'/answers/q1','student',false,'PUT',1);
const result={...base(attempt.id,'s1'),taskId:'LIVE-g2-s1',score:100,maximum:100,percentage:100,passed:true,gradedAt:new Date().toISOString(),gradedBy:'cs'};
await check('company stores private graded result','cycle3Demo/examResults/'+attempt.id,'company',true,'PUT',result);
await check('unpublished exam score denied','cycle3Demo/examResults/'+attempt.id,'student',false);
await check('company per-attempt publication','cycle3Demo/taskAssignments/LIVE-g2-s1/publishedAttempts/'+attempt.id,'company',true,'PUT',true);
await check('published exam result readable','cycle3Demo/examResults/'+attempt.id,'student',true);
await check('student never gets answer key after publication','cycle3Demo/examKeys/LIVE-g2','student',false);
await check('published exam does not grant foreign access','cycle3Demo/examResults/'+attempt.id,'student2',false);
const report={...base('RPT-1','s1'),week:1,feedback:'Weekly review',tasks:[{id:'LIVE-g2-s1',score:100}],approvedHours:120,requiredHours:240,sentBy:'cs',sentAt:new Date().toISOString(),status:'sent'};
await check('company sends student-specific report and index','cycle3Demo','company',true,'PATCH',{'weeklyReports/RPT-1':report,'weeklyReportIndex/as/s1/RPT-1':true});
await check('company cannot rewrite sent report status','cycle3Demo/weeklyReports/RPT-1/status','company',false,'PUT','sent');
await check('academic cannot reset report to sent','cycle3Demo/weeklyReports/RPT-1/status','academic',false,'PUT','sent');
await check('authorized academic receives report','cycle3Demo/weeklyReports/RPT-1','academic',true);
await check('foreign academic report denied','cycle3Demo/weeklyReports/RPT-1','foreignAcademic',false);
await check('student cannot read private report scores','cycle3Demo/weeklyReports/RPT-1','student',false);
await check('academic feedback leaves only','cycle3Demo/weeklyReports/RPT-1','academic',true,'PATCH',{academicFeedback:'Reviewed',reviewedBy:'as',reviewedAt:new Date().toISOString(),status:'reviewed'});
await check('academic cannot alter company grades','cycle3Demo/weeklyReports/RPT-1/tasks/0/score','academic',false,'PUT',0);
await check('root read remains denied','','student',false);
await check('root write remains denied','','student',false,'PATCH',{'users/s1/role':'admin'});
console.log(count+' training Rules checks passed');
})().catch(error=>{console.error(error.message);process.exitCode=1;});
