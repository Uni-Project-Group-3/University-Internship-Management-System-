// Runs only against the disabled real application. Never seeds data or contacts Firebase.
const {assert, Client, form}=require('./PercentagePageTestClient.cjs');
(async()=>{
 const client=new Client(process.argv[2]); let count=0;
 const check=(name,condition)=>{assert(condition,name); count++; console.log('PASS '+name);};
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
