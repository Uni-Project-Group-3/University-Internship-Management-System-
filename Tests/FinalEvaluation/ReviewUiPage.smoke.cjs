// Actual ASP.NET postbacks in isolated memory; no browser visual testing or Firebase.
const {assert,Client,form,value}=require('./PercentagePageTestClient.cjs');
let n=0;function pass(s){n++;console.log('PASS '+s);}
(async()=>{
 const c=new Client(process.argv[2]);await c.submitted();let a=await c.page('academic');
 for(const [id,label,count] of [['AssignedStudentCount','Assigned Students',3],['CompanyDraftCount','Company Drafts',0],['AwaitingReviewCount','Awaiting Academic Review',1],['AcademicApprovalCount','Academic Approvals',0]]){
  assert(a.includes(label));assert(new RegExp('data-stat="'+id+'"[^>]*>\\s*'+count+'\\s*<').test(a));
 }assert(!a.includes('Authorized students:'));pass('four dynamic authorized statistics replace raw text');
 assert(a.includes('<th>Student</th>')&&a.includes('<th>Company</th>')&&a.includes('<th>Company Evaluation</th>')&&a.includes('<th>Academic Review</th>'));
 assert(a.includes('Student ID: local-lina')&&a.includes('DEMO Northstar Training')&&a.includes('Company Evaluation'));
 assert(a.includes('Not Started')||a.includes('Draft')||a.includes('Submitted'));pass('Academic rows show the selected student identity, linked company and separate evaluation/review statuses');
 assert(a.includes('id="informationContent"'));pass('student details modal has the target used by its structured-content interaction');
 a=await c.open('academic',a);assert(a.includes('Company Final Percentage')&&Number(value(a,'FinalPercentage'))===80&&!a.includes('name="FinalPercentage"'));pass('Western percentage is display-only with no editable input');
 assert(a.includes('Pending Review')&&a.includes('id="revisionReasonSection" hidden'));pass('pending review and initially hidden revision reason');
 a=await c.click('academic',a,'RequestRevision',{ConfirmAction:'on',RevisionReason:'   '});assert(a.includes('clear revision reason')&&value(a,'reviewChoice')==='revision');pass('server requires revision reason and restores selected action on failure');
 a=await c.open('academic',a);a=await c.click('academic',a,'ApproveReview',{ConfirmAction:'on',RevisionReason:'Not part of an approval'});
 assert(a.includes('Reviewed UTC:')&&a.includes('View Review Decision')&&!a.includes('name="RevisionReason"')&&!a.includes('name="academicReviewChoice"'));pass('approved decision timestamp and read-only review render');
 assert(/data-stat="AcademicApprovalCount"[^>]*>\s*1\s*</.test(a)&&/data-stat="AwaitingReviewCount"[^>]*>\s*0\s*</.test(a));pass('statistics update only after persisted Academic approval');
 a=await c.page('academic');assert(/data-stat="AcademicApprovalCount"[^>]*>\s*1\s*</.test(a));pass('approval statistics survive refresh');
 const s=await c.page('student');assert(s.includes('Published Task Grades')&&!s.includes('Published exam results')&&!s.includes('FinalPercentage'));pass('Student uses task-grade terminology without confidential form fields');
 console.log(n+' review UI Web Forms checks passed.');
})().catch(e=>{console.error(e);process.exit(1);});
