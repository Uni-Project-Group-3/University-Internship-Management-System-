'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'..'),rules=JSON.parse(fs.readFileSync(path.join(root,'firebase.internship.rules.json'))),baseline=JSON.parse(fs.readFileSync(path.join(root,'firebase.internship.baseline.rules.json')));
const copy=structuredClone(rules);for(const node of ['applications','students','placements','opportunities'])copy.rules[node]=baseline.rules[node];delete copy.rules.applicationIndex;delete copy.rules.opportunitySeats;assert.deepEqual(copy,baseline);console.log('PASS diff: application/placement scopes only; all other permissions preserved');
if(process.argv.includes('--static-only'))process.exit(0);
const host=process.env.FIREBASE_DATABASE_EMULATOR_HOST;if(!/^127\.0\.0\.1:\d+$/.test(host||''))throw Error('Local emulator required.');
const project='demo-uims-internship',now=Math.floor(Date.now()/1000),ns=project+'-default-rtdb';
const token=uid=>[Buffer.from(JSON.stringify({alg:'none',typ:'JWT'})).toString('base64url'),Buffer.from(JSON.stringify({iss:'https://securetoken.google.com/'+project,aud:project,sub:uid,user_id:uid,iat:now,exp:now+3600,auth_time:now,firebase:{sign_in_provider:'password',identities:{}}})).toString('base64url'),''].join('.');
async function req(route,uid,method='GET',body){const url=new URL('http://'+host+'/'+route+'.json');url.searchParams.set('ns',ns);const headers={};if(uid==='owner')headers.Authorization='Bearer owner';else if(uid)url.searchParams.set('auth',token(uid));return fetch(url,{method,headers,body:body===undefined?undefined:JSON.stringify(body),signal:AbortSignal.timeout(10000)});}
let count=0;async function check(name,route,uid,allow,method='GET',body){const r=await req(route,uid,method,body),text=await r.text();if(allow)assert.equal(r.status,200,name+' HTTP '+r.status+' '+text);else assert.ok([401,403].includes(r.status),name+' HTTP '+r.status+' '+text);count++;console.log('PASS '+name);}
const aid=s=>'APP-'+crypto.createHash('sha256').update(s+'\n'+'o1').digest('hex');
const app=s=>({id:aid(s),studentId:s,opportunityId:'o1',companyId:'c1',status:'submitted',applicantProgramId:'CS-BSC',studentStatement:'Training',phone:'+962777777777',attachments:{cvLink:'https://example.test/cv.pdf',portfolioLink:''},submittedAt:'2026-10-22T00:00:00Z',appliedAt:'2026-10-22T00:00:00Z',lastUpdatedAt:'2026-10-22T00:00:00Z'});
const apply=s=>({['applications/'+aid(s)]:app(s),['applicationIndex/'+s+'/o1']:aid(s)});
function confirm(s){const pid='PL-'+s,application=aid(s);return {['placements/'+pid]:{id:pid,studentId:s,applicationId:application,opportunityId:'o1',companyId:'c1',academicSupervisorId:'as',companySupervisorId:'cs',startDate:'2099-12-02',endDate:'2100-01-27',status:'active',createdAt:'2026-10-22T00:00:00Z',requiredHours:240,approvedHours:0,seatIndex:0},['students/'+s+'/placementId']:pid,['applications/'+application+'/status']:'confirmed',['applications/'+application+'/placementId']:pid,['applications/'+application+'/confirmedAt']:'2026-10-22T00:00:00Z',['opportunitySeats/o1/0']:{studentId:s,placementId:pid,applicationId:application}};}
(async()=>{
const fixture={usersByUid:{u1:'s1',u2:'s2',u3:'s3',company:'cs',other:'cs2',academic:'as',coordinator:'co',disabled:'off'},users:{s1:{firebaseUid:'u1',accountStatus:'active',role:'student'},s2:{firebaseUid:'u2',accountStatus:'active',role:'student'},s3:{firebaseUid:'u3',accountStatus:'active',role:'student'},cs:{firebaseUid:'company',accountStatus:'active',role:'companySupervisor',companyId:'c1'},cs2:{firebaseUid:'other',accountStatus:'active',role:'companySupervisor',companyId:'c2'},as:{firebaseUid:'academic',accountStatus:'active',role:'academicSupervisor',programId:'CS-BSC'},co:{firebaseUid:'coordinator',accountStatus:'active',role:'coordinator'},off:{firebaseUid:'disabled',accountStatus:'disabled',role:'student'}},students:{s1:{id:'s1',programId:'CS-BSC',academicSupervisorId:'as'},s2:{id:'s2',programId:'CS-BSC',academicSupervisorId:'as'},s3:{id:'s3',programId:'AI-BSC',academicSupervisorId:'as'}},opportunities:{o1:{id:'o1',companyId:'c1',status:'published',canApply:true,eligibleProgramIds:['CS-BSC'],applicationClosesAt:Date.now()+86400000,applicationDeadline:'2099-12-01',startDate:'2099-12-02',durationWeeks:8,requiredHours:240,capacity:1}},cycle3Demo:{examKeys:{private:{correct:1}}}};
assert.equal((await req('','owner','PUT',fixture)).status,200);const compile=await req('.settings/rules','owner','PUT',rules);assert.equal(compile.status,200,'Rules compile: '+await compile.text());
await check('application without atomic index denied','applications/'+aid('s1'),'u1',false,'PUT',app('s1'));
for(const link of ['http://example.test/cv','https:///','https://example.test/space x','https://example.test/new\nline','https://example.test/tab\tx']){
 const bad=apply('s1');bad['applications/'+aid('s1')].attachments.cvLink=link;
 await check('unsafe application link denied','', 'u1',false,'PATCH',bad);
}
await check('own application and index atomic create','','u1',true,'PATCH',apply('s1'));
await check('own duplicate rejected','','u1',false,'PATCH',apply('s1'));
await check('foreign student submit denied','','u2',false,'PATCH',apply('s1'));
await check('wrong program submit denied','','u3',false,'PATCH',apply('s3'));
for(const uid of [null,'disabled','company','academic','coordinator'])await check('unauthorized application submit '+uid,'',uid,false,'PATCH',apply('s2'));
await check('own application read','applications/'+aid('s1'),'u1',true);
await check('company related application read','applications/'+aid('s1'),'company',true);
await check('foreign student application read denied','applications/'+aid('s1'),'u2',false);
await check('foreign company review denied','applications/'+aid('s1'),'other',false,'PATCH',{status:'accepted',reviewedBy:'cs2',reviewNotes:'',lastUpdatedAt:'2026-10-22T01:00:00Z'});
await check('student cannot self accept','applications/'+aid('s1'),'u1',false,'PATCH',{status:'accepted'});
await check('company accept owned pending application','applications/'+aid('s1'),'company',true,'PATCH',{status:'accepted',reviewedBy:'cs',reviewNotes:'',lastUpdatedAt:'2026-10-22T01:00:00Z'});
await check('company cannot change decision ownership','applications/'+aid('s1'),'company',false,'PATCH',{studentId:'s2',status:'accepted'});
await check('company cannot grant internship directly','placements/PL-s1','company',false,'PUT',confirm('s1')['placements/PL-s1']);
await check('partial placement denied','placements/PL-s1','u1',false,'PUT',confirm('s1')['placements/PL-s1']);
await check('pointer only denied','students/s1/placementId','u1',false,'PUT','PL-s1');
await check('confirmation cannot invent approved hours','', 'u1',false,'PATCH',{...confirm('s1'),'placements/PL-s1':{...confirm('s1')['placements/PL-s1'],approvedHours:240}});
await check('second student applies','','u2',true,'PATCH',apply('s2'));
await check('second application accepted','applications/'+aid('s2'),'company',true,'PATCH',{status:'accepted',reviewedBy:'cs',reviewNotes:'',lastUpdatedAt:'2026-10-22T01:00:00Z'});
const results=await Promise.all(['s1','s2'].map((s,i)=>req('','u'+(i+1),'PATCH',confirm(s))));
assert.equal(results.filter(r=>r.status===200).length,1,'Exactly one concurrent confirmation must win');assert.equal(results.filter(r=>[401,403].includes(r.status)).length,1);count++;console.log('PASS concurrent confirmation cannot overbook capacity');
const all=await (await req('','owner')).json(),winner=all.students.s1.placementId?'s1':'s2',loser=winner==='s1'?'s2':'s1';
assert.ok(!all.students[loser].placementId);assert.ok(!all.placements['PL-'+loser]);assert.equal(all.applications[aid(loser)].status,'accepted');assert.equal(all.opportunitySeats.o1['0'].studentId,winner);count++;console.log('PASS rejected atomic confirmation leaves no partial records');
await check('placed student cannot reapply','',winner==='s1'?'u1':'u2',false,'PATCH',apply(winner));
for(const node of ['users/s1/role','usersByUid/u1','cycle3Demo/examKeys/private','taskGrades/x','examAnswers/x'])await check('unrelated write denied '+node,node,'u1',false,'PUT',{changed:true});
console.log(count+' application/placement Rules checks passed');
})().catch(e=>{console.error(e.message);process.exitCode=1;});
