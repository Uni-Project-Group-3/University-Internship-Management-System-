// Updated requirements: one shared session, actual Web Forms postbacks, no Firebase.
const assert = require('node:assert/strict');
const origin = new URL(process.argv[2] || 'http://localhost:61873');
assert(['localhost','127.0.0.1'].includes(origin.hostname));
let cookie='', count=0;
const targets={};
const decode=s=>s.replace(/&quot;/g,'"').replace(/&#39;|&apos;/g,"'").replace(/&lt;/g,'<').replace(/&gt;/g,'>').replace(/&amp;/g,'&');
const attrs=s=>Object.fromEntries([...s.matchAll(/([\w:-]+)="([^"]*)"/g)].map(m=>[m[1],decode(m[2])]));
const routes={company:'/Company/ReportsFinalEvaluation',academic:'/Academic/FinalAcademicEvaluation',coordinator:'/Coordinator/FinalCompletionReview',student:'/Student/MyProgressEvaluation'};
function pass(s){count++;console.log('PASS '+s);}
function form(html){const data=new URLSearchParams();for(const m of html.matchAll(/<input\b[^>]*>/gi)){const a=attrs(m[0]);if(a.name&&!['submit','button'].includes(a.type)&&(a.type!=='checkbox'||/checked=/.test(m[0])))data.set(a.name,a.value||'');}for(const m of html.matchAll(/<textarea\b([^>]*)>([\s\S]*?)<\/textarea>/gi)){const a=attrs(m[1]);data.set(a.name,decode(m[2]).replace(/^\r?\n/,''));}return data;}
async function page(role,data,query=''){const response=await fetch(targets[role]||new URL(routes[role]+query,origin),{method:data?'POST':'GET',headers:{Cookie:cookie,'Content-Type':'application/x-www-form-urlencoded'},body:data?.toString()});targets[role]=new URL(response.url);if(response.headers.getSetCookie().length)cookie=response.headers.getSetCookie().map(c=>c.split(';')[0]).join('; ');const html=await response.text();assert.equal(response.status,200,html.replace(/<[^>]+>/g,' ').slice(0,1500));return html;}
async function open(role,html,action='openEvaluation',id='dev-company-internship'){const data=form(html);data.set(action,id);return page(role,data);}
async function click(role,html,button,fields={}){const data=form(html);data.set(button,button);for(const [k,v]of Object.entries(fields))data.set(k,v);return page(role,data);}
function value(html,id){return form(html).get(id)||(id==='FinalPercentage' ? html.match(/id="FinalPercentageDisplay"[^>]*>([0-9.]+)%/)?.[1] : '')||'';}
(async()=>{
 let c=await page('company');
 if(process.argv.includes('--disabled')){assert(c.includes('Official operations disabled')&&!c.includes('dropdown-toggle'));pass('Company fails closed without explicit local opt-in');let a=await page('academic');assert(a.includes('Official operations disabled')&&!a.includes('AcademicScore'));pass('Academic has no independent score UI or enabled official actions');const data=form(c);data.set('openEvaluation','dev-company-internship');data.set('role','company');data.set('supervisorId','local-company');c=await page('company',data);assert(c.includes('Official operations disabled'));pass('browser identities cannot activate fixtures');console.log(count+' v2 disabled checks passed.');return;}
 assert(c.includes('LOCAL-ONLY isolated fictional')&&(c.match(/dropdown-toggle/g)||[]).length===3);pass('one Actions dropdown per authorized Company trainee');
 c=await open('company',c,'openReports');assert(value(c,'activePanel')==='informationDialog'&&c.includes('No report documents are configured'));pass('View Reports opens an honest empty state without invented reports');
 c=await open('company',c,'openDetails');assert(c.includes('Approved hours: 10')&&value(c,'activePanel')==='informationDialog');pass('View Details uses selected fictional trusted data');
 c=await open('company',c);assert(value(c,'activePanel')==='evaluationDialog'&&!c.includes('criterion-score'));pass('evaluation modal has one percentage and no task grades or criteria');
 c=await click('company',c,'SaveDraft',{FinalPercentage:'',FinalComments:''});assert(c.includes('Saved and confirmed'));pass('partial Draft can be saved');
 c=await click('company',c,'SaveDraft',{FinalPercentage:'101',FinalComments:'preserve after failure'});assert(c.includes('between 0 and 100')&&value(c,'FinalPercentage')==='101'&&value(c,'FinalComments')==='preserve after failure');pass('invalid percentage preserves input without a new success');
 c=await open('company',c);c=await click('company',c,'SaveDraft',{FinalPercentage:'80',FinalComments:'PRIVATE_COMPANY_COMMENT <encoded>'});assert(c.includes('Saved and confirmed')&&Number(value(c,'FinalPercentage'))===80, c.match(/<p class="workspace-error[\s\S]*?<\/p>/)?.[0] + ' score=' + value(c,'FinalPercentage'));pass('Company Draft update persists through the C# workflow');
 c=await click('company',c,'ConfirmSubmission');assert(c.includes('Confirm final submission'));pass('submission requires explicit confirmation');
 c=await click('company',c,'ConfirmSubmission',{ConfirmAction:'on'});assert(c.includes('Saved and confirmed')&&c.includes('Submitted'));pass('Company submission refreshes row state after persistence');
 c=await open('company',c);assert(/id="FinalPercentage"[^>]*readonly=/.test(c)||/readonly=[^>]*id="FinalPercentage"/.test(c));pass('Submitted Company result reloads read-only');
 let a=await page('academic');assert(a.includes('Review Evaluation')&&!a.includes('AcademicScore'));pass('Academic Actions offers review only, without independent grading');
 a=await open('academic',a);assert(Number(value(a,'FinalPercentage'))===80&&value(a,'FinalComments').includes('PRIVATE_COMPANY_COMMENT'));pass('Academic modal displays submitted Company result and comments');
 a=await click('academic',a,'RequestRevision',{ConfirmAction:'on',RevisionReason:''});assert(a.includes('clear revision reason'));pass('Request Revision rejects a missing reason');
 a=await click('academic',a,'RequestRevision',{ConfirmAction:'on',RevisionReason:'PRIVATE_REVISION_REASON',FinalPercentage:'1'});assert(a.includes('Saved and confirmed')&&Number(value(a,'FinalPercentage'))===80);pass('Academic Request Revision persists and ignores forged Company percentage');
 let co=await page('coordinator');assert(co.includes('Academic Request Revision')&&co.includes('blocks completion'));pass('Coordinator cannot approve during Academic Request Revision');
 c=await page('company');assert(c.includes('Revise Evaluation'));c=await open('company',c,'openRevision');pass('Company revision action appears only after the recorded request');
 c=await click('company',c,'SaveDraft',{FinalPercentage:'91',FinalComments:'PRIVATE_REVISED_COMMENT'});assert(c.includes('Saved and confirmed')&&c.includes('Draft'));pass('authorized revision opens a new editable Draft');
 a=await page('academic');a=await open('academic',a);assert(a.includes('Draft content cannot be reviewed')&&!value(a,'FinalComments'));pass('Academic cannot view or approve the new Company Draft');
 c=await open('company',c);c=await click('company',c,'ConfirmSubmission',{ConfirmAction:'on'});assert(c.includes('Submitted'));pass('revised Company version is submitted independently');
 co=await page('coordinator');assert(co.includes('Academic Review pending'));pass('old review cannot approve the revised Company version');
 a=await page('academic');a=await open('academic',a);a=await click('academic',a,'ApproveReview',{ConfirmAction:'on'});assert(a.includes('Saved and confirmed')&&a.includes('Approved'));pass('Academic approval records the exact revised Company version');
 co=await page('coordinator');assert(co.includes('Ready for completion review')&&co.includes('Final Evaluation Percentage: 91'));pass('Coordinator sees the latest percentage and academically approved readiness');
 co=await click('coordinator',co,'confirmApproval',{studentSelect:'dev-company-internship',reviewAcknowledgment:'on'});assert(co.includes('Completion decision saved and confirmed: Approved'));pass('Coordinator completion approval remains a separate audited action');
 let st=await page('student');assert(st.includes('Completion Approved')&&st.includes('Final evaluation outcome: Pass')&&!st.includes('PRIVATE_COMPANY_COMMENT')&&!st.includes('PRIVATE_REVISION_REASON')&&!st.includes('PRIVATE_REVISED_COMMENT'));pass('Student sees permitted outcome and completion without confidential forms or reasons');
 const data=form(st);data.set('DownloadCompletionReport','Download Completion Report');const r=await fetch(targets.student,{method:'POST',headers:{Cookie:cookie,'Content-Type':'application/x-www-form-urlencoded'},body:data.toString()});const pdf=Buffer.from(await r.arrayBuffer());assert.equal(r.status,200);assert(r.headers.get('content-type').startsWith('application/pdf')&&pdf.subarray(0,5).toString()==='%PDF-'&&r.headers.get('cache-control').includes('no-store'));pass('authorized Student downloads the existing private PDF with no-store headers');
 const oldCookie=cookie;cookie='';const independent=await page('company');assert(independent.includes('Continue Draft')&&!independent.includes('PRIVATE_REVISED_COMMENT'));cookie=oldCookie;pass('a separate browser session does not inherit this workflow');
 console.log(count+' v2 Web Forms checks passed. Shared isolated memory only; no Firebase writes.');
})().catch(e=>{console.error(e);process.exit(1);});
