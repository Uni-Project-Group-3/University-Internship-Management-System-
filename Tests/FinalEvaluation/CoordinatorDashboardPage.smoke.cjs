// Runs only against the disabled real application. Never seeds data or contacts Firebase.
const {assert, Client, form}=require('./PercentagePageTestClient.cjs');
(async()=>{
 const client=new Client(process.argv[2]); let count=0;
 const check=(name,condition)=>{assert(condition,name); count++; console.log('PASS '+name);};
 if(process.argv.includes('--http-probe')){
  const url=new URL('/Coordinator/FinalCompletionReview.aspx',client.origin);const response=await fetch(url);const html=await response.text();
  check('unauthenticated Coordinator request is denied and renders an inline accessible explanation',response.status===403&&html.includes('completion-error')&&html.includes('No student or evaluation records are displayed'));
  check('persistent information banner is removed without removing the authorization error',!html.includes('integrationNotice')&&!html.includes('Verified review. Decisions require configured trusted integrations')&&html.includes('role="alert"'));
  check('no student data, statistics or decision controls are fabricated for unauthorized requests',!html.includes('Yazan Khaled')&&!html.includes('Fictional')&&html.includes('<strong>—</strong>')&&/id="approveButton"[^>]*disabled/.test(html));
  check('placement filter remains disabled when no authorized placement data is available',/id="periodFilter"[^>]*disabled/.test(html)&&html.includes('Placement period options appear only when trusted start and end dates are available'));
  check('shared Coordinator sidebar and cache-versioned stylesheet/script render',html.includes('id="coordinatorSidebar"')&&html.includes('final-completion-review.css?v=20261024-coordinator-review-v2')&&html.includes('final-completion-review.js?v=20261024-coordinator-review-v2'));
  const cssPath=html.match(/href="([^"]*final-completion-review\.css\?v=20261024-coordinator-review-v2)"/)?.[1];assert(cssPath,'Coordinator CSS reference missing');const cssResponse=await fetch(new URL(cssPath,url));const css=await cssResponse.text();
  check('IIS serves the updated Coordinator stylesheet with error and responsive selectors',cssResponse.status===200&&cssResponse.headers.get('content-type').includes('text/css')&&css.includes('.completion-error')&&css.includes('.completion-dashboard')&&css.includes('@media(max-width:640px)'));
  const jsPath=html.match(/src="([^"]*final-completion-review\.js\?v=20261024-coordinator-review-v2)"/)?.[1];assert(jsPath,'Coordinator interaction script reference missing');const jsResponse=await fetch(new URL(jsPath,url));const js=await jsResponse.text();
  check('IIS serves the updated filter and selection behavior',jsResponse.status===200&&js.includes("row.dataset.period === period.value")&&js.includes("navbarSearch?.addEventListener('input'")&&js.includes("setDetails(!!selectedRow)"));
  console.log(count+' Coordinator read-only IIS probes passed. No Firebase operations or writes.');return;
 }
 let html=await client.page('coordinator');
 check('page compiles and renders all four statistic labels',['Ready for Final Review','In Review','Completed Internships','Returned for Revision'].every(x=>html.includes(x)));
 check('no authorized data is represented as unavailable rather than fictional students',html.includes('No authorized Firebase records are available')&&!html.includes('Roaa Ajlouni')&&!html.includes('Lina Al-Khatib'));
 check('all four counts are unknown without trusted source',(html.match(/<strong>—<\/strong>/g)||[]).length===4);
 check('search and both supported filters are rendered',['completionSearch','academicFilter','completionFilter','clearCompletionFilters'].every(id=>html.includes('id="'+id+'"')));
 check('unsupported period filtering explains missing source',/id="periodFilter"[^>]*disabled/.test(html)&&html.includes('Placement periods are unavailable'));
 check('accessible Evaluation Documents and History tabs exist',['evaluationTab','documentsTab','historyTab'].every(id=>html.includes('id="'+id+'"'))&&html.includes('role="tablist"'));
 check('approval and report generation remain disabled',/id="approveButton"[^>]*disabled/.test(html)&&/id="generateCompletionReport"[^>]*disabled/.test(html));
 check('policy-dependent changes and rejection are absent',!html.includes('name="requestCompletionChanges"')&&!html.includes('name="rejectCompletion"'));
 check('Coordinator PDF download remains disabled with explanation',html.includes('no authorized Coordinator download adapter')&&html.includes('pdfUnavailable'));
 let data=form(html); data.set('openPlacement','out-of-scope');data.set('role','coordinator');
 html=await client.page('coordinator',data);
 check('forged table selection cannot enable a trusted context',html.includes('No authorized Firebase records are available')&&!html.includes('Completion decision saved and confirmed'));
 data=form(html);data.set('generateCompletionReport','Generate');
 html=await client.page('coordinator',data);
 check('forged PDF generation never reports success',!html.includes('Completion Report generated/verified'));
 html=await client.page('coordinator');
 check('refresh preserves fail-closed state',html.includes('No authorized Firebase records are available')&&html.includes('Official Firebase review is disabled'));
 console.log(count+' Coordinator dashboard Web Forms checks passed. No Firebase writes.');
})().catch(error=>{console.error(error);process.exit(1);});
