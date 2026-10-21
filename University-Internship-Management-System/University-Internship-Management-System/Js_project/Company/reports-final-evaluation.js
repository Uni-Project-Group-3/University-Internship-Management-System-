
/* Original-page demo bindings. Layout and server controls remain in the ASPX. */
(function () {
    'use strict';
    if (!window.UimsLive?.enabled()) return;
    let binding;
    function initialize() {
        binding?.abort();
        binding = new AbortController();
        const signal = binding.signal, demo = window.UimsLive;
        let view = demo.view(), selected = null, render = () => {};
        if (!view || view.user.mustChangePassword || view.user.role !== document.body.dataset.portal) {
            location.href = '../Auth/Login.aspx';
            return;
        }
        const by = id => document.getElementById(id) || document.querySelector('[id$="_' + id + '"]');
        const all = selector => [...document.querySelectorAll(selector)];
        const displayValue=value=>typeof value==='string'&&/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(value)?new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'short',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).format(new Date(value))+' Jordan Time (UTC+3)':value;
        const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
        const text = (id, value) => { const element = by(id); if (element) element.textContent = value ?? '—'; };
        const value = id => by(id)?.value?.trim() || '';
        const set = (id, value) => { if (by(id)) by(id).value = value ?? ''; };
        const options = (id, records, placeholder = 'All') => {
            const element = by(id); if (!element) return;
            const previous = element.value;
            element.innerHTML = '<option value="">' + escape(placeholder) + '</option>' + records.map(record => '<option value="' + escape(record.id) + '">' + escape(record.name) + '</option>').join('');
            if (records.some(record => record.id === previous)) element.value = previous;
        };
        const on = (id, type, handler) => by(id)?.addEventListener(type, handler, {signal});
        const click = (id, handler) => on(id, 'click', event => { event.preventDefault(); try { handler(event); } catch(exception) { error(exception,event.currentTarget.closest('dialog,.modal,.offcanvas')); } });
        const button = (label, action, id, className = 'row-action') => {
            const paths={edit:'M4 16v4h4L20 8l-4-4L4 16Z',delete:'M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13',reset:'M13 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10ZM8 12H2m2 0v4',status:'M12 2v10M6 5a9 9 0 1 0 12 0',assign:'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8M20 8v6m-3-3h6'};
            const icon=className.includes('action-btn')&&paths[action];
            const content=icon?'<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="'+paths[action]+'"/></svg>':escape(label);
            return '<button type="button" title="'+escape(label)+'" aria-label="'+escape(label)+'" class="' + className + '" data-uims="' + action + '" data-record="' + escape(id) + '">' + content + '</button>';
        };
        const cells = values => '<tr>' + values.map(cell => '<td>' + cell + '</td>').join('') + '</tr>';
        const fill = (selector, records, row) => {
            const element = document.querySelector(selector);
            if(!element)return;
            const emptyState=element.id==='taskList'?element.querySelector('#noTasksMsg'):null;
            element.innerHTML=records.map(row).join('');
            if(emptyState)element.append(emptyState);
        };
        const messageElement = () => by('uimsPageNotice') || document.querySelector('main .preview-note,main .completion-notice,main .companies-notice,main .evaluation-notice,main .monitor-notice,main .workspace-note,#messagesAvailability,#traineesAvailability,#accountAvailability') || by('pageNotice') || by('profileMessage') || by('pnlMessage');
        function notice(message) {
            let element = messageElement();
            if (!element) { element = document.createElement('p'); element.id='uimsPageNotice'; element.className = 'uims-feedback'; element.setAttribute('role','status'); (document.querySelector('main') || document.querySelector('.content')).prepend(element); }
            element.hidden = false; element.textContent = message;
        }
        function error(error, scope) {
            let element = scope?.querySelector('[data-uims-error]');
            if (scope && !element) { element = document.createElement('p'); element.dataset.uimsError = ''; element.className = 'uims-error'; element.setAttribute('role','alert'); (scope.querySelector('.modal-body') || scope).append(element); }
            if (element) element.textContent = error.message; else notice(error.message);
        }
        const dirty = new WeakSet(), openers = new WeakMap();
        let beforeClose=null;
        function open(id) {
            const dialog = by(id); if (!dialog) throw Error('Missing original popup: ' + id);
            const alreadyOpen=dialog.tagName==='DIALOG'?dialog.open:dialog.classList.contains('show');
            if(!alreadyOpen){openers.set(dialog, document.activeElement);dirty.delete(dialog);}
            dialog.querySelectorAll('[data-uims-error],.uims-discard').forEach(el => el.remove());
            if (dialog.tagName === 'DIALOG') { if (!dialog.open) dialog.showModal(); }
            else if (dialog.classList.contains('offcanvas')) bootstrap.Offcanvas.getOrCreateInstance(dialog).show();
            else bootstrap.Modal.getOrCreateInstance(dialog).show();
            dialog.querySelector('input:not([readonly]):not([disabled]),textarea:not([disabled]),select:not([disabled])')?.focus();
        }
        function close(id, force = false) {
            const dialog = by(id); if (!dialog) return;
            if(!force&&beforeClose?.(id)===false)return;
            if (dirty.has(dialog) && !force) {
                if (dialog.querySelector('.uims-discard')) return;
                const prompt = document.createElement('div'); prompt.className = 'uims-discard'; prompt.setAttribute('role','alert');
                prompt.innerHTML = '<p>Discard unsaved changes?</p><button type="button" data-keep>Keep editing</button><button type="button" data-discard>Discard changes</button>';
                (dialog.querySelector('.modal-footer') || dialog).append(prompt);
                prompt.querySelector('[data-keep]').onclick = () => prompt.remove();
                prompt.querySelector('[data-discard]').onclick = () => { prompt.remove(); close(id, true); };
                prompt.querySelector('button').focus(); return;
            }
            dirty.delete(dialog);
            if (dialog.tagName === 'DIALOG') dialog.close();
            else if (dialog.classList.contains('offcanvas')) bootstrap.Offcanvas.getInstance(dialog)?.hide();
            else bootstrap.Modal.getInstance(dialog)?.hide();
            openers.get(dialog)?.focus();
        }
        function transact(action, payload, popup) {
            try {
                const result = demo.transaction(action, payload); view = demo.view();
                if (popup) close(popup, true);
                render(); notice('Saved in local demo storage. No Firebase operation or email was sent.');
                return {ok:true, result};
            } catch (exception) { error(exception, popup ? by(popup) : null); return {ok:false}; }
        }
        function valid(ids, popup) {
            const invalid = ids.map(by).find(field => field && (!field.checkValidity() || field.required && !field.value.trim()));
            if (!invalid) return true;
            invalid.reportValidity(); error(Error('Check the highlighted field.'), by(popup)); return false;
        }
        const actions = {};
        document.addEventListener('click', event => {
            const control = event.target.closest('[data-uims]'); if (!control) return;
            event.preventDefault();
            try { actions[control.dataset.uims]?.(control.dataset.record, control); } catch (exception) { error(exception); }
        }, {signal});
        all('dialog,.modal,.offcanvas').forEach(dialog => {
            const changed = () => dirty.add(dialog);
            dialog.addEventListener('input', changed, {signal}); dialog.addEventListener('change', changed, {signal});
            dialog.addEventListener('cancel', event => { event.preventDefault(); close(dialog.id); }, {signal});
            dialog.addEventListener('hide.bs.modal', event => { if (dirty.has(dialog)) { event.preventDefault(); close(dialog.id); } }, {signal});
            dialog.addEventListener('hidden.bs.modal', () => openers.get(dialog)?.focus(), {signal});
            dialog.addEventListener('click', event => {
                if(event.defaultPrevented)return;
                if (event.target.closest('[data-bs-dismiss], [data-close], [data-completion-close], [data-profile-cancel], .popup-close, .monitor-close, .evaluation-close, .companies-close, .placements-close, [data-close-exam]')) {
                    event.preventDefault(); event.stopPropagation(); close(dialog.id);
                } else if (event.target === dialog && dialog.tagName === 'DIALOG') {
                    const rect = dialog.getBoundingClientRect();
                    if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) close(dialog.id);
                }
            }, {signal});
        });
        function detail(student, id, selector) {
            const progress = view.progress.find(item => item.id === student); if (!progress) throw Error('This student is not available to this demo identity.');
            const target = document.querySelector(selector);
            if (target) target.innerHTML = '<h3>' + escape(progress.name) + '</h3><dl>' + [['University ID',progress.universityId],['Department',progress.department],['Company',progress.company?.name],['Company Supervisor',progress.companySupervisor?.name],['Academic Supervisor',progress.supervisor?.name],['Training status',progress.placement?.completionStatus || 'Not placed'],['Approved / Required hours',progress.approvedHours + ' / ' + progress.requiredHours],['Tasks completed',progress.completedTasks + ' / ' + progress.tasks.length],['Risk indicators',progress.risks.join(', ') || 'None'],['Confirmed at (Jordan Time)',progress.placement?.studentConfirmedAt],['Disable deadline (Jordan Time)',progress.placement?.accountDisableAt],['Certificate',progress.placement?.certificateId]].map(([label,content]) => '<dt>' + escape(label) + '</dt><dd>' + escape(displayValue(content) || '—') + '</dd>').join('') + '</dl>' + progress.tasks.map(task => '<article><h4>' + escape(task.title) + '</h4><p>' + escape(task.description) + '</p><p>' + escape(displayValue(task.deadline)) + ' · ' + escape(task.status) + '</p><p>' + escape(task.feedback) + '</p>' + view.submissions.filter(sub => sub.taskId === task.id).map(sub => '<p>Submitted ' + escape(displayValue(sub.createdAt)) + ': ' + escape(sub.description) + ' · ' + escape(sub.fileName) + '</p>').join('') + '</article>').join('') + progress.evaluations.map(evaluation => '<article><h4>' + escape(evaluation.kind) + ' evaluation · ' + escape(evaluation.status) + '</h4><p>' + escape(evaluation.comments) + '</p></article>').join('');
            selected = student; open(id);
        }
        function shell() {
            all('[id$="lblUserName"],[id$="NavName"]').forEach(el => el.textContent = view.user.name);
            document.addEventListener('click', event => {
                const element = event.target.closest('a,button,input');
                if (element && (/log\s*out/i.test(element.textContent) || /logout/i.test(element.id))) {
                    event.preventDefault(); event.stopImmediatePropagation(); demo.logout(); location.href = '../Auth/Login.aspx';
                }
            }, {signal,capture:true});
            all('[id*=Logout],[id*=logout]').forEach(el => el.disabled = false);
            if (!document.querySelector('script[src*="dashboard-navigation.js"]')) {
                const sidebar = document.querySelector('aside.sidebar,aside.dash-sidebar,aside[id$="Sidebar"]');
                const toggle = document.querySelector('.menu-btn,.dash-menu-btn');
                const main = document.querySelector('.main,.dash-main');
                const media = matchMedia('(max-width:991.98px)');
                const setOpen = show => {
                    const active = media.matches && show;
                    document.body.classList.toggle('sidebar-open',active); toggle?.setAttribute('aria-expanded',String(active));
                    if (sidebar) sidebar.inert = media.matches && !active;
                    if (main) main.inert = active;
                    document.querySelector('.sidebar-backdrop,.dash-sidebar-backdrop')?.toggleAttribute('hidden',!active);
                    if (active) sidebar?.querySelector('button,a[href]')?.focus();
                };
                window.toggleSidebar = () => setOpen(!document.body.classList.contains('sidebar-open'));
                if (toggle) { toggle.onclick = null; toggle.addEventListener('click',window.toggleSidebar,{signal}); }
                all('.sidebar-close,.dash-sidebar-close,.legacy-sidebar-close,.sidebar-backdrop,.dash-sidebar-backdrop').forEach(el => el.addEventListener('click',()=>{setOpen(false);toggle?.focus();},{signal}));
                document.addEventListener('keydown',event=>{if(event.key==='Escape'&&document.body.classList.contains('sidebar-open')){setOpen(false);toggle?.focus();}},{signal});
                media.addEventListener('change',()=>setOpen(false),{signal}); setOpen(false);
            }
            const topSearch = document.querySelector('.topbar input,.dash-topbar input');
            const search = document.querySelector('main input[type="search"],.content input[type="search"],#taskSearch,#searchInput');
            if(topSearch&&!search){topSearch.disabled=false;topSearch.addEventListener('keydown',event=>{if(event.key!=='Enter')return;event.preventDefault();const route={admin:'MonitorPlacementsProgress.aspx',student:'ExploreOpportunities.aspx',company:'MyTrainees.aspx',academic:'StudentProgressMonitor.aspx'}[view.user.role];location.href=route+'?search='+encodeURIComponent(topSearch.value.trim());},{signal});}
            if (topSearch && search && topSearch !== search) { topSearch.disabled=false;topSearch.addEventListener('input',()=>{search.value=topSearch.value;search.dispatchEvent(new Event('input'));},{signal}); }
        }

        function notifications() {
            const items=[];
            if(view.user.role==='student'){view.applications.forEach(a=>items.push('Application: '+a.status));view.placements.filter(p=>p.completionStatus==='PendingStudentConfirmation').forEach(()=>items.push('Coordinator approval received. Confirm completion in the Student Dashboard.'));}
            if(view.user.role==='company')items.push(view.applications.filter(a=>a.status==='pending').length+' pending applications',view.tasks.filter(t=>t.status==='submitted').length+' task submissions awaiting review');
            if(view.user.role==='academic')items.push(view.progress.filter(s=>s.evaluations.some(e=>e.kind==='company'&&e.status==='submitted')&&!s.evaluations.some(e=>e.kind==='academic'&&e.status==='submitted')).length+' assigned students awaiting academic evaluation');
            if(view.user.role==='admin')items.push(view.messages.filter(m=>m.status==='unread').length+' unread contact messages',view.progress.filter(s=>s.placement?.completionStatus==='PendingStudentConfirmation').length+' pending student confirmations');
            let dialog=by('originalNotifications');
            if(!dialog){dialog=document.createElement('dialog');dialog.id='originalNotifications';dialog.className='uims-notifications';dialog.setAttribute('aria-label','Demo notifications');document.forms[0].append(dialog);}
            dialog.innerHTML='<h2>Notifications · local demo</h2>'+items.map(item=>'<p>'+escape(item)+'</p>').join('')+(items.length?'':'<p>No notifications.</p>')+'<button type="button" data-notification-close>Close</button>';
            dialog.querySelector('button').onclick=()=>dialog.close();dialog.showModal();
        }
        all('.topbar button,.dash-topbar button').forEach(el=>{if(!/notification/i.test(el.getAttribute('aria-label')||'')&&!el.querySelector('.bi-bell'))return;el.disabled=false;el.title='Local demo notifications';el.addEventListener('click',event=>{event.preventDefault();event.stopImmediatePropagation();view=demo.view();if(view)notifications();},{signal,capture:true});});
        shell();
        
        if(!by('InternDetails')){const url=new URL(location.href);if(url.searchParams.get('frontendDemo')!=='1'){url.searchParams.set('frontendDemo','1');location.replace(url);return;}notice('The original report panels were not rendered. Rebuild the project and reload.');return;}
        let active='reports',reportId=null,reviewing=false;
        document.forms[0].addEventListener('submit',event=>event.preventDefault(),{signal});
        ['Intern','Report','CriteriaConfig'].forEach(id=>by(id)?.removeAttribute('onchange'));
        const reportText=(id,content)=>all('[data-uims-report="'+id+'"]').forEach(el=>el.textContent=content??'—');
        const current=()=>view.progress.find(student=>student.id===selected);
        function showSection(section){if(section==='trainees'){close('traineeDialog');return;}active=section;all('[data-workspace-view]').forEach(el=>el.hidden=el.dataset.workspaceView!==section);open('traineeDialog');}
        render=()=>{
            const records=view.progress.filter(student=>[student.name,student.universityId,student.department].join(' ').toLowerCase().includes(value('traineeSearch').toLowerCase())&&(!value('evaluationFilter')||(student.evaluations.find(e=>e.kind==='company')?.status||'not-started')===value('evaluationFilter')));
            fill('.trainee-table tbody',records,student=>cells([escape(student.name)+'<small>'+escape(student.universityId)+' · '+escape(student.department)+'</small>',String(view.submissions.filter(sub=>sub.studentId===student.id&&!sub.review).length)+' pending reviews',escape(student.evaluations.find(e=>e.kind==='company')?.status||'Not started'),button('View Reports','reports',student.id)+button('Evaluation','evaluation',student.id)+button('View Details','details',student.id)]));
            reportText('TraineeTotal',view.students.length);reportText('PendingReports',view.submissions.filter(sub=>!sub.review).length);reportText('DraftTotal',view.evaluations.filter(e=>e.kind==='company'&&e.status==='draft').length);reportText('SubmittedTotal',view.evaluations.filter(e=>e.kind==='company'&&e.status==='submitted').length);
            text('traineeSearchStatus',records.length+' trainees');if(by('noTraineeMatches'))by('noTraineeMatches').hidden=records.length>0;
            const student=current();if(!student)return;
            const evaluation=student.evaluations.find(e=>e.kind==='company');
            reportText('SelectedName',student.name);set('Intern',student.id);reportText('TaskCount',student.tasks.length+' / '+student.completedTasks+' / '+(student.tasks.length-student.completedTasks));reportText('AssessmentCount','Not configured');reportText('Hours',student.approvedHours+' / '+student.requiredHours);reportText('ProgressPercent',student.requiredHours?Math.min(100,Math.round(student.approvedHours/student.requiredHours*100)):0);reportText('TaskSummary',student.tasks.map(task=>task.title+': '+task.status).join(' · ')||'No tasks assigned');
            fill('[aria-label="Assessment summary table"] tbody',[],()=> '');
            const submissions=view.submissions.filter(sub=>sub.studentId===student.id);
            fill('.reports-table tbody',submissions,sub=>cells([escape(view.tasks.find(t=>t.id===sub.taskId)?.title||'Submitted work')+button('View / Review','report',sub.id),escape(displayValue(sub.createdAt)),escape(sub.review?.outcome||'Submitted'),escape(sub.fileName||'No filename provided')]));
            if(by('NoReports'))by('NoReports').hidden=submissions.length>0;
            options('Report',submissions.map(sub=>({id:sub.id,name:view.tasks.find(t=>t.id===sub.taskId)?.title+' · '+sub.createdAt})),'Choose submitted work');set('Report',reportId);
            const report=submissions.find(sub=>sub.id===reportId);by('ReportDetailsPanel').hidden=!report;by('ReportFeedbackPanel').hidden=!reviewing;
            by('openReportLink').disabled=!report?.link;by('downloadReportSummary').disabled=!report;
            if(report){reportText('ReportExcerpt',report.description+' · '+report.link+' · '+report.fileName);reportText('ReportExistingFeedback',report.review?.feedback||'No review yet');}
            text('FinalStatus',evaluation?.status||'Not started');reportText('AcademicRecipient',student.supervisor?.name||'Not assigned');reportText('SubmissionDate',evaluation?.submittedAt||'Not submitted');by('EvaluationView').querySelector('.workspace-note').textContent='Official university criteria are not configured. This connected demo saves narrative feedback only; no official score or weighting is invented.';by('EvaluationPreview').querySelector('p').textContent='Narrative demonstration — no official score.';
            const submitted=evaluation?.status==='submitted';['FinalComments','SaveDraft','PreviewDraft','ReviewSubmission','ConfirmSubmission','CancelEvaluation'].forEach(id=>{if(by(id))by(id).disabled=submitted;});
            by('FinalComments').readOnly=submitted;by('CriteriaConfig').innerHTML='<option>Narrative demo — official criteria pending</option>';by('CriteriaConfig').disabled=true;by('CriteriaConfig').title='Official criteria are not configured; the connected demo saves narrative evaluations only.';text('demoCalculation','No official criteria or score is assumed. Narrative demonstration only.');
        };
        function selectStudent(id,section){selected=id;reportId=null;reviewing=false;set('FinalComments',current()?.evaluations.find(e=>e.kind==='company')?.comments||'');render();showSection(section);}
        for(const section of ['reports','evaluation','details'])actions[section]=id=>selectStudent(id,section);
        options('Intern',view.students.map(student=>({id:student.id,name:student.name})),'Choose trainee');
        on('Intern','change',()=>{if(dirty.has(by('traineeDialog'))){set('Intern',selected);error(Error('Save your draft or close and discard changes before selecting another trainee.'),by('traineeDialog'));return;}selectStudent(value('Intern'),active);});
        all('[data-open-view]').forEach(el=>el.addEventListener('click',event=>{event.preventDefault();showSection(el.dataset.openView);},{signal}));
        actions.report=id=>{reportId=id;reviewing=true;const sub=view.submissions.find(sub=>sub.id===id);set('ReportFeedback',sub.review?.feedback||'');set('ReportOutcome',sub.review?.outcome||'reviewed');render();};
        on('Report','change',()=>actions.report(value('Report')));
        ['ReportFeedback','ReportOutcome','PreviewReport','SaveReportReview'].forEach(id=>{if(by(id))by(id).disabled=false;});
        click('PreviewReport',()=>reportText('ReportPreview',value('ReportOutcome')+' — '+value('ReportFeedback')));
        click('SaveReportReview',()=>transact('reviewReport',{id:reportId,feedback:value('ReportFeedback'),outcome:value('ReportOutcome')}));
        all('[data-close-report]').forEach(el=>el.addEventListener('click',()=>{reportId=null;render();},{signal}));
        all('[data-review-report]').forEach(el=>el.addEventListener('click',()=>{reviewing=true;render();},{signal}));
        click('SaveDraft',()=>{if(transact('evaluation',{studentId:selected,comments:value('FinalComments'),submit:'no'}).ok){dirty.delete(by('traineeDialog'));text('finalPreviewMessage','Draft saved in shared local demo state.');}});
        function preview(){if(!value('FinalComments')){error(Error('Enter evaluation comments.'),by('traineeDialog'));return false;}reportText('PreviewComments',value('FinalComments'));reportText('AcademicRecipient',current().supervisor?.name||'No Academic Supervisor assigned');reportText('SubmissionDate','Set when submitted');return true;}
        click('PreviewDraft',()=>{if(preview()){by('EvaluationPreview').hidden=false;text('finalPreviewMessage','Review the narrative evaluation, then choose Submit.');}});
        click('ReviewSubmission',()=>{if(!preview())return;close('traineeDialog',true);by('ConfirmSubmission').disabled=false;by('ConfirmSubmission').value='Confirm demo submission';text('submissionDescription','Submit the final company narrative for '+current().name+' to '+(current().supervisor?.name||'the assigned Academic Supervisor')+'? It becomes read-only after submission.');by('submissionDialog').querySelector('.workspace-note').textContent='This updates shared local demo records across roles. No email or Firebase operation is performed.';open('submissionDialog');dirty.add(by('submissionDialog'));});
        click('ConfirmSubmission',()=>{if(transact('evaluation',{studentId:selected,comments:value('FinalComments'),submit:'yes'},'submissionDialog').ok){render();showSection('evaluation');}});
        click('closeSubmissionDialog',()=>{close('submissionDialog',true);open('traineeDialog');if(value('FinalComments')!==(current()?.evaluations.find(e=>e.kind==='company')?.comments||''))dirty.add(by('traineeDialog'));});click('CancelEvaluation',()=>close('traineeDialog'));
        options('evaluationFilter',[{id:'not-started',name:'Not started'},{id:'draft',name:'Draft'},{id:'submitted',name:'Submitted'}]);
        on('traineeSearch','input',render);on('evaluationFilter','change',render);all('[data-clear-filters]').forEach(el=>el.addEventListener('click',()=>{set('traineeSearch','');set('evaluationFilter','');render();},{signal}));
        const initialStudent=new URLSearchParams(location.search).get('student');if(initialStudent&&view.students.some(s=>s.id===initialStudent))selectStudent(initialStudent,'evaluation');


        text('reportsTitle','Submitted work & reports');
        click('openReportLink',()=>{const report=view.submissions.find(sub=>sub.id===reportId);if(report?.link&&/^https?:\/\//i.test(report.link))window.open(report.link,'_blank','noopener');});
        click('downloadReportSummary',()=>{const report=view.submissions.find(sub=>sub.id===reportId);if(!report)throw Error('Choose a submission first.');const content='UIMS DEMONSTRATION SUBMISSION SUMMARY\n'+current().name+'\n'+report.createdAt+'\n'+report.description+'\nLink: '+report.link+'\nSelected filename: '+report.fileName+'\nFile bytes are not uploaded or included.';const url=URL.createObjectURL(new Blob([content],{type:'text/plain'})),link=document.createElement('a');link.href=url;link.download='uims-demo-submission.txt';link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});
        render();
        notice('Live data integration is pending. No local fallback is enabled.');
        window.addEventListener('focus',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx';},{signal});
        window.addEventListener('pageshow',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx';},{signal});
    }
    if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',initialize,{once:true});else initialize();
    if(window.Sys?.WebForms?.PageRequestManager)Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();

/* Live role records: original page containers, server-verified identity, no local fallback. */
(() => {
'use strict';
const config={"page":"Company/ReportsFinalEvaluation","js":"Company/reports-final-evaluation","css":"company/reports-final-evaluation","source":"placements","table":".trainee-table tbody","search":"traineeSearch","filters":{},"reports":true,"cells":["studentId","@reports","@evaluation"]};
let binding;
async function init(){
 if(!window.UimsTab)return;
 binding?.abort(); binding=new AbortController();const signal=binding.signal;
 const by=id=>document.getElementById(id),all=s=>[...document.querySelectorAll(s)];
 const host=document.querySelector(config.table);if(!host)return;
 const escape=v=>String(v??'Not recorded').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const text=(id,v)=>{if(by(id))by(id).textContent=v;};
 const date=v=>{if(!v)return 'Not recorded';if(/^\d{4}-\d{2}-\d{2}$/.test(v))return v;const d=new Date(v);return Number.isNaN(+d)?'Not recorded':new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'short',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).format(d)+' Jordan Time (UTC+3)';};
 let notice=by('pageNotice')||document.querySelector('[data-role-live-status]');if(!notice){notice=document.createElement('p');notice.dataset.roleLiveStatus='';notice.className='role-live-status';notice.setAttribute('role','status');document.querySelector('main,.content,.app-content')?.prepend(notice);}
 notice.hidden=false;notice.textContent='Loading authorized Firebase records…';
 const blocked='This operation requires an authorized live write integration. No changes have been made.';
 // Existing management controls remain visible with an explicit unavailable state.
 all('button').filter(b=>!b.closest('.sidebar,.dash-sidebar,.topbar')&&!/close|menu|logout|previous|next|reset|clear/i.test(b.id+' '+b.getAttribute('aria-label')+' '+b.textContent)).forEach(b=>{b.disabled=true;b.title=blocked;});
 try{
 const data=await UimsTab.request('roleData',{}, {page:config.page});
 if(signal.aborted)return;
 const ready=k=>data.states[k]==='ready',rows=k=>ready(k)?data[k]:[];
 if(!ready(config.source))throw Error('Firebase did not permit the required records. No demonstration records are shown.');
 text('lblUserName',data.user.name);text('lblInitials',data.user.name.split(/\s+/).slice(0,2).map(x=>x[0]).join(''));
 all('[id*=Logout],[id*=logout]').forEach(b=>{b.disabled=false;b.removeAttribute('title');});
 const placement=r=>config.source==='placements'?r:rows('placements').find(p=>p.id===r.placementId&&p.studentId===r.id);
 const opportunity=r=>rows('opportunities').find(o=>o.id===r.opportunityId);
 const tasks=r=>ready('tasks')?rows('tasks').filter(t=>t.placementId===placement(r)?.id):null;
 const hours=r=>{const p=placement(r);return p&&Number.isFinite(p.approvedHours)&&Number.isFinite(p.requiredHours)?p.approvedHours+' / '+p.requiredHours+' h':'Not recorded';};
 const value=(r,key)=>{
 if(key.startsWith('@date:'))return date(r[key.slice(6)]);
 const p=placement(r);
 switch(key){case '@student':return (r.fullName||r.studentId||r.id)+' · '+(r.studentNumber||r.id);
 case '@companyName':return r.name+' · '+(r.industry||'');
 case '@opportunity':return opportunity(r)?.title||r.opportunityId||'Not recorded';
 case '@period':return (r.startDate||'Not recorded')+' – '+(r.endDate||'Not recorded');
 case '@company':return rows('companies').find(c=>c.id===p?.companyId)?.name||p?.companyId||'Not placed';
 case '@status':return p?.status||(r.placementId&&!ready('placements')?'Unavailable':'not_placed');
 case '@hours':return hours(r);
 case '@tasks':{const t=tasks(r);return t?t.filter(x=>x.status==='reviewed').length+' reviewed / '+t.length:'Unavailable';}
 case '@risk':{const t=tasks(r);return t?t.some(x=>x.status!=='reviewed'&&x.dueDate&&x.dueDate<data.serverUtc.slice(0,10))?'Overdue task':'No overdue tasks':'Not assessed';}
 case '@companySupervisor':return p?.companySupervisorId||'Not recorded';
 case '@reports':return ready('progressReports')?rows('progressReports').filter(x=>x.placementId===r.id).length+' report records':'Unavailable';case '@evaluation':return 'Evaluation access is not configured';case '@confirmation':return 'Not available';case '@deadline':return 'Not available';case '@certificate':return 'Not available';case '@restricted':return 'Not authorized for this view';case '@supervisorNames':return ready('companySupervisors')?rows('companySupervisors').filter(s=>s.companyId===r.id).map(s=>s.fullName).join(', ')||'Not recorded':'Unavailable';case '@supervisorEmails':return ready('companySupervisors')?rows('companySupervisors').filter(s=>s.companyId===r.id).map(s=>s.email).join(', ')||'Not recorded':'Unavailable';case '@supervisorStatuses':return ready('companySupervisors')?rows('companySupervisors').filter(s=>s.companyId===r.id).map(s=>s.accountStatus||'Not recorded').join(', ')||'Not recorded':'Unavailable';
 default:return r[key]??'Not recorded';}
 };
 let active='',page=0;
 const filters=Object.entries(config.filters||{});
 filters.forEach(([id,key])=>{const el=by(id);if(!el)return;el.replaceChildren(new Option('All',''));[...new Set(rows(config.source).map(r=>String(value(r,key))))].sort().forEach(v=>el.add(new Option(v,v)));});
 all('fieldset').forEach(f=>f.disabled=false);all('#evaluationFilter,#evaluationStatus').forEach(el=>{el.disabled=true;el.title='Private evaluation status is not available in this read-only view.';});if(by('accountStatus')){by('accountStatus').disabled=true;by('accountStatus').title='Protected account status is not included in the public supervisor directory.';}all('#placementSort option[value=tasks],#placementSort option[value=risk]').forEach(el=>{el.disabled=true;el.title='This view has no authorized task or risk dataset.';});
 function filtered(){let list=rows(config.source).filter(r=>(!by(config.search)?.value||JSON.stringify(r).toLowerCase().includes(by(config.search).value.toLowerCase()))&&filters.every(([id,key])=>!by(id)?.value||String(value(r,key))===by(id).value));
 if(active&&active!=='all')list=list.filter(r=>active==='pending'?['submitted','under_review'].includes(r.status):active==='open'?r.status==='published':active==='closed'?r.status!=='published':r.status===active);
 const sort=by(config.sort)?.value||'';return list.sort((a,b)=>sort==='hours'?(placement(b)?.approvedHours??-1)-(placement(a)?.approvedHours??-1):sort==='applicants'?rows('applications').filter(r=>r.opportunityId===b.id).length-rows('applications').filter(r=>r.opportunityId===a.id).length:sort.includes('deadline')?String(a.applicationDeadline||'').localeCompare(String(b.applicationDeadline||'')):/newest|oldest/i.test(sort)?String(a.submittedAt||a.createdAt||a.publishedAt||'').localeCompare(String(b.submittedAt||b.createdAt||b.publishedAt||''))*(/newest/i.test(sort)?-1:1):String(a.fullName||a.name||a.title||a.studentId||a.id).localeCompare(String(b.fullName||b.name||b.title||b.studentId||b.id))*(sort.includes('desc')||sort.includes('Z to A')?-1:1));}
 let dialog=by('roleLiveDetails');if(!dialog){dialog=document.createElement('dialog');dialog.id='roleLiveDetails';dialog.className='role-live-details';dialog.setAttribute('aria-label','Record details');dialog.innerHTML='<header><h2>Record details</h2><button type="button" aria-label="Close record details">×</button></header><div></div>';document.forms[0].append(dialog);}
 dialog.querySelector('button').addEventListener('click',()=>dialog.close(),{signal});
 function details(r){const body=dialog.querySelector('div');body.replaceChildren();const dl=document.createElement('dl');Object.entries(r).forEach(([key,v])=>{const dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=key.replace(/([A-Z])/g,' $1');dd.textContent=/At$/.test(key)?date(v):Array.isArray(v)?v.join(', '):String(v);dl.append(dt,dd);});body.append(dl);if(config.reports){rows('progressReports').filter(report=>report.placementId===r.id&&report.studentId===r.studentId).forEach(report=>{const heading=document.createElement('h3'),summary=document.createElement('p');heading.textContent=report.period||report.id;summary.textContent=(report.summary||'')+'\nStatus: '+report.status+'\nReported hours (not approved): '+(report.hoursReported??'Not recorded')+'\nCompany comments: '+(report.mentorComments||'Not recorded');body.append(heading,summary);});}dialog.showModal();dialog.querySelector('button').focus();}
 function render(){const list=filtered(),hasPaging=all('[aria-label="Next page"],#nextPlacements').length>0,size=hasPaging?(Number(by(config.pageSize)?.value)||10):Math.max(1,list.length);page=Math.min(page,Math.max(0,Math.ceil(list.length/size)-1));const visible=list.slice(page*size,(page+1)*size);
 host.innerHTML=visible.map(r=>config.cards?'<div class="col-md-6 col-xl-4"><article class="opp-card"><h3>'+escape(r.title)+'</h3><p>'+escape(r.description)+'</p><p>'+escape(r.location)+' · '+escape(r.status)+'</p><p>Apply by '+escape(date(r.applicationDeadline))+'</p><button type="button" data-live-record="'+escape(r.id)+'" class="row-action">View details</button></article></div>':'<tr>'+config.cells.map(k=>'<td>'+escape(value(r,k))+'</td>').join('')+'<td><button type="button" class="row-action" data-live-record="'+escape(r.id)+'">View details</button></td></tr>').join('')|| (config.cards?'<p>No matching records.</p>':'<tr><td colspan="'+(config.cells.length+1)+'">No matching records.</td></tr>');
 const result=list.length+' matching records · Page '+(page+1)+' of '+Math.max(1,Math.ceil(list.length/size));['resultsStatus','placementResults','recordCount'].forEach(id=>text(id,result));
 all('[aria-label="Previous page"],#previousPlacements').forEach(b=>b.disabled=page===0);all('[aria-label="Next page"],#nextPlacements').forEach(b=>b.disabled=(page+1)*size>=list.length);
 if(by('emptyState'))by('emptyState').hidden=true;if(by('noResultsMsg'))by('noResultsMsg').hidden=true;all('.completion-empty,.companies-empty,.monitor-empty').filter(el=>!el.closest('dialog')).forEach(el=>el.hidden=true);if(by('noResultsMsg'))by('noResultsMsg').hidden=true;
 }
 host.addEventListener('click',e=>{const b=e.target.closest('[data-live-record]');if(b){e.preventDefault();const record=rows(config.source).find(r=>r.id===b.dataset.liveRecord);if(record)details(record);}},{signal});
 [config.search,config.sort,config.pageSize,...filters.map(([id])=>id)].filter(Boolean).forEach(id=>by(id)?.addEventListener('input',()=>{page=0;render();},{signal}));
 all('[aria-label="Previous page"],#previousPlacements').forEach(b=>b.addEventListener('click',()=>{page--;render();},{signal}));all('[aria-label="Next page"],#nextPlacements').forEach(b=>b.addEventListener('click',()=>{page++;render();},{signal}));
 all('#statusTabs [data-status]').forEach(b=>{b.disabled=false;b.addEventListener('click',()=>{active=b.dataset.status;page=0;render();},{signal});});
 all('#resetFilters,#clearPlacements,#resetCompanyFilters,#completionClearFilters,[data-clear-filters]').forEach(b=>{b.disabled=false;b.addEventListener('click',()=>{filters.forEach(([id])=>{if(by(id))by(id).value='';});if(by(config.search))by(config.search).value='';active='';page=0;render();},{signal});});
 const count=kind=>{const list=rows(config.source);switch(kind){case 'total':return list.length;case 'pending':return list.filter(r=>['submitted','under_review'].includes(r.status)).length;case 'capacity':return ready('opportunities')?rows('opportunities').reduce((n,r)=>n+(Number(r.capacity)||0),0):'Unavailable';case 'applications':return ready('applications')?rows('applications').length:'Unavailable';case 'active':case 'completed':return ready('placements')?rows('placements').filter(r=>r.status===kind).length:'Unavailable';default:return list.filter(r=>r.status===kind).length;}};
 Object.entries(config.stats||{}).forEach(([id,kind])=>text(id,count(kind)));
 all('[data-uims-report="TraineeTotal"]').forEach(el=>el.textContent=rows('placements').length);all('[data-uims-report="PendingReports"]').forEach(el=>el.textContent=ready('progressReports')?rows('progressReports').filter(r=>r.status==='submitted').length:'Unavailable'); const legacyStats=all('.stat-details h3');if(legacyStats.length===3){const source=rows(config.source);let metrics;if(config.source==='academicSupervisors')metrics=[source.length,source.filter(s=>s.active===true).length,new Set(source.map(s=>s.programId).filter(Boolean)).size];else if(config.page.endsWith('Assign_Supervisors')){const assigned=source.filter(s=>rows('academicSupervisors').some(a=>a.id===s.academicSupervisorId&&a.programId===s.programId));metrics=[source.length,ready('academicSupervisors')?assigned.length:'Unavailable',ready('academicSupervisors')?source.length-assigned.length:'Unavailable'];}if(metrics)metrics.forEach((v,i)=>legacyStats[i].textContent=v);} const stat=all('.monitor-stat strong');if(stat.length){stat[0].textContent=rows('students').length;stat[1].textContent=ready('placements')?rows('placements').filter(r=>r.status==='active').length:'Unavailable';stat[2].textContent=ready('tasks')?rows('tasks').filter(r=>r.status==='submitted').length:'Unavailable';stat[3].textContent=ready('tasks')?rows('students').filter(r=>value(r,'@risk')==='Overdue task').length:'Unavailable';}
 all('.trainees-notice,.messages-notice,.completion-notice,.companies-notice,.evaluation-notice,.demo-note,.preview-note,#placementMessage,#pageNotice').filter(el=>!el.closest('dialog')).forEach(el=>el.textContent='Live Firebase records. Management writes and private evaluations are not enabled in this read-only integration.');
 all('.monitor-chip,.trainees-data-badge,.messages-disconnected').forEach(el=>el.textContent='Live Firebase');
 notice.textContent='Live Firebase records loaded for '+data.user.name+'. '+(Object.values(data.states).some(v=>v!=='ready')?'Some related reads are unavailable. ':'')+'Changes requiring additional permissions remain unavailable.';
 render();
 }catch(error){notice.textContent=error.message;host.replaceChildren();const p=document.createElement(config.cards?'p':'tr');p.textContent='Records unavailable. Please retry after signing in.';host.append(p);}
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
if(window.Sys?.Application)window.Sys.Application.add_load(init);
})();
