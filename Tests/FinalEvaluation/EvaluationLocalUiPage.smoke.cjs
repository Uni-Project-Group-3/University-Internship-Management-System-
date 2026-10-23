// Default-disabled application only. Never registers fixtures or writes Firebase.
const {assert,Client,form}=require('./PercentagePageTestClient.cjs');
(async()=>{
 let count=0; const check=(name,result)=>{assert(result,name);count++;console.log('PASS '+name)};
 for(const role of ['company','academic']){
  const client=new Client(process.argv[2]); let html=await client.page(role);
  check(role+' explains missing integration rather than claiming a failed Firebase connection',html.includes('Supervisor access integration is unavailable')&&!html.includes('Firebase connection not configured'));
  check(role+' has no unauthorized Actions rows',!html.includes('data-evaluation-row="true"'));
  check(role+' status options match the business workflow',['Not Started','Draft','Submitted','Revision Requested','Approved'].every(status=>html.includes('<option>'+status+'</option>')));
  check(role+' statistics remain unknown without an authorized context',(html.match(/>—<|>—\s*</g)||[]).length>=4);
  check(role+' search and clear controls remain available',['traineeSearch','evaluationFilter','clearEvaluationFilters'].every(id=>html.includes('id="'+id+'"')));
  let data=form(html);data.set('openEvaluation','out-of-scope');data.set('role',role);html=await client.page(role,data);
  check(role+' forged selection cannot unlock missing integration',html.includes('No authorized students are connected')&&!html.includes('Saved and confirmed by the repository'));
  data=form(html);data.set(role==='company'?'SaveDraft':'ApproveReview','Confirm');data.set('selectedInternship','out-of-scope');data.set('ConfirmAction','on');html=await client.page(role,data);
  check(role+' forged write is not reported successful',html.includes('Supervisor access integration is unavailable')&&!html.includes('Saved and confirmed by the repository'));
  html=await client.page(role);check(role+' refresh keeps truthful unknown state',html.includes('Supervisor access integration is unavailable')&&!html.includes('data-evaluation-row="true"'));
  if(role==='company')check('Company card labels and Actions heading match the workflow',['Assigned Trainees','Company Drafts','Awaiting Academic Review','Academic Approvals','<th>Actions</th>'].every(label=>html.includes(label))&&!html.includes('<option>Midterm evaluation</option>'));
 }
 console.log(count+' evaluation local UI Web Forms checks passed. No Firebase operations.');
})().catch(error=>{console.error(error);process.exit(1)});
