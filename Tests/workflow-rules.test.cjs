'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..'),rules=JSON.parse(fs.readFileSync(path.join(root,'firebase.workflow.rules.json'))),baseline=JSON.parse(fs.readFileSync(path.join(root,'firebase.workflow.baseline.rules.json')));
const compare=structuredClone(rules);compare.rules.opportunities=baseline.rules.opportunities;assert.deepEqual(compare,baseline);
assert.ok(rules.rules.opportunities.$opportunityId['.read'].startsWith('('+baseline.rules.opportunities.$opportunityId['.read']+') || '));
console.log('PASS diff: only scoped opportunity child permission/validation changed');

if(process.argv.includes('--compare-live')||process.argv.includes('--verify-live')){
 const mode=process.argv.includes('--verify-live')?'--verify-live':'--compare-live',file=process.argv[process.argv.indexOf(mode)+1];
 if(!file)throw Error('Rules snapshot path required.');
 const live=JSON.parse(fs.readFileSync(file,'utf8'));
 if(mode==='--verify-live')assert.deepEqual(live,rules,'Live Rules differ from tested draft');
 else assert.ok(require('node:util').isDeepStrictEqual(live,baseline)||require('node:util').isDeepStrictEqual(live,rules),'Unexpected live Rules; do not overwrite team changes.');
 console.log('PASS live Rules comparison');process.exit(0);
}
if(process.argv.includes('--static-only'))process.exit(0);

const host=process.env.FIREBASE_DATABASE_EMULATOR_HOST;if(!/^127\.0\.0\.1:\d+$/.test(host||''))throw Error('Local emulator required; live database is never used.');
const project='demo-uims-workflows',ns=project+'-default-rtdb',now=Math.floor(Date.now()/1000);
const token=uid=>[Buffer.from(JSON.stringify({alg:'none',typ:'JWT'})).toString('base64url'),Buffer.from(JSON.stringify({iss:'https://securetoken.google.com/'+project,aud:project,sub:uid,user_id:uid,iat:now,exp:now+3600,auth_time:now,firebase:{sign_in_provider:'password',identities:{}}})).toString('base64url'),''].join('.');
async function req(route,uid,method='GET',body){const url=new URL('http://'+host+'/'+route+'.json');url.searchParams.set('ns',ns);const headers={};if(uid==='owner')headers.Authorization='Bearer owner';else if(uid)url.searchParams.set('auth',token(uid));return fetch(url,{method,headers,body:body===undefined?undefined:JSON.stringify(body),signal:AbortSignal.timeout(10000)});}
let count=0;
async function check(name,route,uid,allow,method='GET',body){const r=await req(route,uid,method,body);if(allow)assert.equal(r.status,200,name+' HTTP '+r.status+' '+await r.text());else assert.ok([401,403].includes(r.status),name+' HTTP '+r.status);count++;console.log('PASS '+name);}
(async()=>{
const users={cs:{firebaseUid:'company',role:'companySupervisor',companyId:'c1',accountStatus:'active'},cs2:{firebaseUid:'other',role:'companySupervisor',companyId:'c2',accountStatus:'active'},st:{firebaseUid:'student',role:'student',accountStatus:'active'},off:{firebaseUid:'disabled',role:'companySupervisor',companyId:'c1',accountStatus:'disabled'},as:{firebaseUid:'academic',role:'academicSupervisor',programId:'CS-BSC',accountStatus:'active'},co:{firebaseUid:'coordinator',role:'coordinator',accountStatus:'active'}};
assert.equal((await req('','owner','PUT',{users,usersByUid:{company:'cs',other:'cs2',student:'st',disabled:'off',academic:'as',coordinator:'co'},cycle3Demo:{secret:{answerKey:'private'}}})).status,200);
const compile=await req('.settings/rules','owner','PUT',rules);assert.equal(compile.status,200,'Rules compile: '+await compile.text());
const record={id:'OPP-test',companyId:'c1',createdBy:'cs',creationRequestId:'0123456789abcdef0123456789abcdef',title:'Intern',description:'Training',requirements:'C#',location:'Amman',workMode:'hybrid',capacity:2,status:'published',eligibleProgramIds:['CS-BSC'],applicationDeadline:'2099-12-01',startDate:'2099-12-02',durationWeeks:8,requiredHours:240,updatedAt:'2099-01-01T00:00:00Z',publishedAt:'2099-01-01T00:00:00Z',applicationPolicy:'eligible_students',canApply:true};
await check('company missing-record read for conditional create','opportunities/OPP-test','company',true);
await check('company create own opportunity','opportunities/OPP-test','company',true,'PUT',record);
await check('company own opportunity read','opportunities/OPP-test','company',true);
await check('company update own opportunity','opportunities/OPP-test','company',true,'PUT',{...record,title:'Updated'});
for(const uid of ['other','student','disabled','academic','coordinator',null,'unmapped'])await check('unauthorized write denied '+uid,'opportunities/OPP-test',uid,false,'PUT',record);
await check('company deletion denied','opportunities/OPP-test','company',false,'DELETE');
for(const [field,value] of [['companyId','c2'],['createdBy','st'],['canApply',false],['applicationPolicy','public'],['unexpected','secret'],['capacity',0],['capacity',1.5],['status','approved'],['eligibleProgramIds',['UNKNOWN']]])await check('protected/invalid '+field,'opportunities/OPP-test','company',false,'PUT',{...record,[field]:value});
for(const field of ['createdBy','creationRequestId','publishedAt']){const changed={...record};delete changed[field];await check('audit field deletion denied '+field,'opportunities/OPP-test','company',false,'PUT',changed);}
for(const node of ['users/cs/role','usersByUid/company','tasks/t1','applications/a1','cycle3Demo/secret','examAnswers/a','taskGrades/g'])await check('unrelated writes denied '+node,node,'company',false,'PUT',{changed:true});
for(const uid of ['company','student','academic','coordinator'])await check('exam key denied '+uid,'cycle3Demo/secret/answerKey',uid,false);
await check('foreign private opportunity denied','opportunities/foreign','other',true,'PUT',{...record,id:'foreign',companyId:'c2',createdBy:'cs2',status:'closed'});
await check('foreign private read denied','opportunities/foreign','company',false);
await check('anonymous root denied','',null,false);
console.log(count+' opportunity Rules checks passed');
})().catch(e=>{console.error(e.message);process.exitCode=1;});
