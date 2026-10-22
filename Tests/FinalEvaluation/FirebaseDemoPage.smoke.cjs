// Explicit live development test. Writes ONLY through the gated, owned demo repository.
const {assert,Client,form,value}=require('./PercentagePageTestClient.cjs');
const c=new Client(process.argv[2]||'http://localhost:61876');let n=0;
function pass(s){console.log('PASS '+s);n++;}
(async()=>{
 let h=await c.page('company');
 assert(h.includes('LOCAL-ONLY Firebase DEMO'));
 const dataset=h.match(/_devTests\/roaaFinalEvaluationDemo\/v1\/sessions\/[a-f0-9]{32}/)[0];
 console.log('Owned fictional dataset: '+dataset);pass('explicit Firebase demo gate and isolated dataset');
 h=await c.open('company',h);assert(value(h,'FinalPercentage')==='');pass('seeded partial Company Draft loads');
 h=await c.click('company',h,'SaveDraft',{FinalPercentage:'83',FinalComments:'Fictional persistent HTTP draft'});assert(h.includes('Saved and confirmed'));pass('Draft write acknowledged');
 h=await c.page('company');h=await c.open('company',h);assert(Number(value(h,'FinalPercentage'))===83);pass('Draft survives GET refresh');
 h=await c.click('company',h,'SaveDraft',{FinalPercentage:'86',FinalComments:'Fictional persistent HTTP update'});assert(h.includes('Saved and confirmed'));pass('Draft update acknowledged');
 h=await c.click('company',h,'ConfirmSubmission',{ConfirmAction:'on'});assert(h.includes('Saved and confirmed'));pass('Company submission persisted');
 let a=await c.page('academic');a=await c.open('academic',a);assert(Number(value(a,'FinalPercentage'))===86);pass('Academic reads exact submitted percentage');
 a=await c.click('academic',a,'RequestRevision',{ConfirmAction:'on',RevisionReason:''});assert(a.includes('clear revision reason'));pass('missing revision reason denied');
 a=await c.click('academic',a,'RequestRevision',{ConfirmAction:'on',RevisionReason:'Fictional revision request'});assert(a.includes('Saved and confirmed'));pass('Academic revision persisted');
 h=await c.page('company');h=await c.open('company',h,'openRevision');h=await c.click('company',h,'SaveDraft',{FinalPercentage:'92',FinalComments:'Fictional revised result'});assert(h.includes('Saved and confirmed'));pass('new revision Draft persisted');
 h=await c.click('company',h,'ConfirmSubmission',{ConfirmAction:'on'});assert(h.includes('Saved and confirmed'));pass('revised Company version submitted');
 a=await c.page('academic');a=await c.open('academic',a);a=await c.click('academic',a,'ApproveReview',{ConfirmAction:'on'});assert(a.includes('Saved and confirmed'));pass('exact revised version academically approved');
 let co=await c.page('coordinator');assert(co.includes('Ready for completion review'));pass('Coordinator readiness reads persistent approved evaluation');
 co=await c.click('coordinator',co,'confirmApproval',{studentSelect:'dev-company-internship',reviewAcknowledgment:'on'});assert(co.includes('Completion decision saved and confirmed: Approved'));pass('audited Coordinator approval persisted');
 co=await c.click('coordinator',co,'generateCompletionReport',{studentSelect:'dev-company-internship'});assert(co.includes('Completion Report generated/verified through the private service.'));pass('explicit private PDF generation completed');
 let st=await c.page('student');assert(st.includes('Completion Approved')&&!st.includes('Fictional revised result')&&!st.includes('Fictional revision request'));pass('Student receives completion without confidential evaluation');
 const d=form(st);d.set('DownloadCompletionReport','Download Completion Report');const r=await fetch(c.targets.student,{method:'POST',headers:{Cookie:c.cookie,'Content-Type':'application/x-www-form-urlencoded'},body:d.toString()});const bytes=Buffer.from(await r.arrayBuffer());assert.equal(r.status,200);assert(bytes.subarray(0,5).toString()==='%PDF-');pass('authorized private PDF download');
 st=await c.page('student');const choice=form(st);choice.set('__EVENTTARGET','DemoStudentChoice');choice.set('DemoStudentChoice','1');st=await c.page('student',choice);assert(st.includes('Omar')&&!st.includes('Completion Approved'));pass('fictional Student selector switches ownership scope');
 co=await c.page('coordinator');const select=form(co);select.set('studentSelect','demo-omar');select.set('__EVENTTARGET','studentSelect');co=await c.page('coordinator',select);
 co=await c.click('coordinator',co,'requestCompletionChanges',{studentSelect:'demo-omar',reviewAcknowledgment:'on',decisionReason:''});assert(co.includes('clear decision reason'));pass('Coordinator cannot request changes without reason');
 co=await c.click('coordinator',co,'requestCompletionChanges',{studentSelect:'demo-omar',reviewAcknowledgment:'on',decisionReason:'Fictional missing review; follow up with assigned supervisor.'});assert(co.includes('Completion decision saved and confirmed: ChangesRequested'));pass('reasoned Request Changes persisted');
 co=await c.page('coordinator',undefined,'?internshipId=demo-omar');co=await c.click('coordinator',co,'rejectCompletion',{studentSelect:'demo-omar',reviewAcknowledgment:'on',decisionReason:'Fictional policy-permitted test rejection.'});assert(co.includes('Completion decision saved and confirmed: Rejected'));pass('explicit demo policy permits audited rejection');
 co=await c.page('coordinator');assert(co.includes('Saved decision: Rejected')&&co.includes('RequestChanges'));pass('terminal decision and prior audit survive refresh');
 st=await c.page('student');assert(!st.includes('id="DownloadCompletionReport"')&&!st.includes('Fictional policy-permitted test rejection'));pass('rejected Student cannot download or see confidential reason');
 h=await c.page('company');assert(h.includes(dataset));h=await c.open('company',h);assert(Number(value(h,'FinalPercentage'))===92);pass('final aggregate reloads from owned persistent dataset');
 console.log(n+' live Firebase Web Forms checks passed. No official paths; no cleanup/reset executed.');
})().catch(e=>{console.error(e);process.exit(1);});
