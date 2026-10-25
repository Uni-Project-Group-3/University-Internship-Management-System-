(function () {
    'use strict';
    const by = id => document.getElementById(id), states = { draft: 'Draft', pendingAcademicReview: 'Awaiting academic review', companyRevisionRequested: 'Company revision requested', pendingAdminReview: 'Academically approved \u2014 awaiting final admin approval.', academicRevisionRequested: 'Returned to academic supervisor by admin', completed: 'Training completed', completionConsistencyPending: 'Completion verification requires administration review' };
    let items = [], role = '', loading = false, activeRefresh = null;
    const placementScope = new URLSearchParams(location.search).get('placementId');
    const embedded = new URLSearchParams(location.search).get('embedded') === '1' && !!placementScope;
    if (embedded) document.body.classList.add('final-embedded');
    const node = (tag, text, cls) => { const el = document.createElement(tag); if (text != null) el.textContent = String(text); if (cls) el.className = cls; if (tag === 'button') el.type = 'button'; return el; };
    async function api(action, id, input) {
        const response = await fetch('FinalReportsApi.ashx?action=' + action + (id ? '&placementId=' + encodeURIComponent(id) : ''), { method: input ? 'POST' : 'GET', cache: 'no-store', credentials: 'same-origin', signal: AbortSignal.timeout(60000), headers: input ? { 'Content-Type': 'application/json' } : {}, body: input ? JSON.stringify(input) : undefined });
        const result = await response.json(); if(response.status===401)throw Error('Your sign-in session is unavailable or expired. Sign in again in this tab, reopen the report and retry. No automatic write retry was performed.'); if (!response.ok) throw Error(result.message || 'The report could not be verified.'); return result;
    }
    function popup(title) { const d = node('dialog', null, 'final-dialog'); d.append(node('h2', title)); document.body.append(d); return d; }
    async function confirm(title, detail) {
        const d = popup(title), text = node('p', detail), actions = node('div', null, 'dialog-actions'), cancel = node('button', 'Cancel'), yes = node('button', 'Confirm'); actions.append(cancel, yes); d.append(text, actions); d.showModal();
        return new Promise(resolve => { const end = value => { d.close(); d.remove(); resolve(value); }; cancel.onclick = () => end(false); yes.onclick = () => end(true); d.oncancel = event => { event.preventDefault(); end(false); }; });
    }
    function field(host, label, name, value, type = 'text', required = false) { const wrap = node('label', label + (required ? ' *' : '')), control = node(type === 'textarea' ? 'textarea' : 'input'); if (type !== 'textarea') control.type = type; control.name = name; control.value = value ?? ''; control.required = required; control.maxLength = type === 'textarea' ? 5000 : 180; control.placeholder = ({ comments: 'Summarize training achievements, responsibilities and company feedback...', companyResponse: 'Explain how you addressed the requested changes...', feedback: 'Enter feedback for this report revision...', fileName: 'Example: final-training-report.pdf' })[name] || ''; if (type === 'number') { control.min = 0; control.max = 100; control.step = '.01'; } wrap.append(control); host.append(wrap); return control; }
    function validate(host) {
        let first;
        host.querySelectorAll('input,textarea').forEach(el => { if (el.disabled) return; const valid = el.checkValidity() && (!el.required || el.value.trim().length > 0); el.classList.toggle('invalid', !valid); el.setAttribute('aria-invalid', String(!valid)); let error = el.nextElementSibling; if (!error?.classList.contains('field-error')) { error = node('span', null, 'field-error'); el.after(error); } error.textContent = valid ? '' : el.validationMessage || 'This field is required.'; if (!valid && !first) first = el; });
        if (first) { const details=first.closest('details');if(details)details.open=true;first.focus(); first.scrollIntoView({ block: 'center' }); } return !first;
    }
    async function load() {
        if (loading) return; loading = true;
        try { const data = await api('list'); items = placementScope ? data.items.filter(item => item.placementId === placementScope) : data.items; role = data.role; render(); by('finalNotice').className = ''; by('finalNotice').textContent = placementScope && !items.length ? 'This placement is unavailable or not authorized for your current account.' : ''; if (role === 'student') await loadStudentProgress(); if (activeRefresh) await activeRefresh(); }
        catch (error) { try { const connection = await api('connection'); by('finalNotice').textContent = connection.connected ? 'Firebase is connected. ' + error.message : error.message; } catch (_) { by('finalNotice').textContent = error.message; } by('finalNotice').className = 'error'; }
        finally { loading = false; }
    }
    async function loadStudentProgress() {
        let section = by('studentProgress');
        if (!section) { section = node('section'); section.id = 'studentProgress'; by('finalRecords').before(section); }
        try {
            const response = await fetch('TasksApi.ashx', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(60000) });
            const result = await response.json(); if(response.status===401)throw Error('Your sign-in session is unavailable or expired. Sign in again in this tab, reopen the report and retry. No automatic write retry was performed.'); if (!response.ok) throw Error(result.message || 'Task progress is unavailable.');
            const tasks = result.tasks || []; section.replaceChildren(node('h2', 'Your task progress'));
            const grid = node('div', null, 'student-progress-grid');
            for (const task of tasks) { const card = node('article', null, 'report-card'), open = node('button', 'View task progress'); card.append(node('h3', task.title), node('p', task.status, 'report-state')); open.onclick = () => { const d = popup(task.title), close = node('button', 'Close'); d.append(node('p', task.description), node('p', task.instructions || ''), node('p', 'Current status: ' + task.status)); if (task.grade != null) d.append(node('p', 'Released company grade: ' + task.grade + ' / ' + (task.maximumScore || 100))); close.onclick = () => d.close(); d.append(close); d.addEventListener('close', () => d.remove(), { once: true }); d.showModal(); }; card.append(open); grid.append(card); }
            if (!tasks.length) section.append(node('p', 'No tasks are assigned to your authorized placement.')); section.append(grid);
        } catch (error) { section.replaceChildren(node('p', error.message, 'error')); }
    }
    function render() {
        const host = by('finalRecords'); host.replaceChildren(); const search = by('finalSearch').value.toLowerCase(), filter = by('finalFilter').value;
        const visible = items.filter(i => (!filter || i.status === filter) && (!search || (i.studentName + ' ' + i.studentId + ' ' + i.placementId).toLowerCase().includes(search)));
        function hours(minutes){return (Number(minutes)/60).toLocaleString(undefined,{maximumFractionDigits:2});}
        for (const item of visible) {
            const card=node('article',null,'report-card final-student-card');
            const name=item.studentName||'Student name unavailable',head=node('div',null,'final-card-head'),who=node('div',null,'final-card-who');
            who.append(node('h2',name),node('p','Student ID: '+(item.studentId||'Unavailable'),'student-id'));
            head.append(node('span',name.trim().split(/\s+/).slice(0,2).map(w=>w.charAt(0)).join('').toUpperCase(),'final-avatar'),who);card.append(node('span',states[item.status]||item.status,'report-state final-state-'+String(item.status||'').toLowerCase()),head);
            if(role!=='student'){
                const details=node('dl',null,'final-card-details');
                for(const [label,value] of [['Academic supervisor',item.academicSupervisorName||item.academicSupervisorId||'Unavailable'],['Approved hours',item.approvedTrainingMinutes==null?'Unavailable':hours(item.approvedTrainingMinutes)+' / '+hours(item.requiredTrainingMinutes||18000)+' h'],['Remaining hours',item.approvedTrainingMinutes==null?'Unavailable':hours(Math.max(0,(item.requiredTrainingMinutes||18000)-item.approvedTrainingMinutes))+' h']]){const row=node('div');row.append(node('dt',label),node('dd',value));details.append(row);}card.append(details);
                if(item.approvedTrainingMinutes!=null){const required=item.requiredTrainingMinutes||18000,percent=Math.min(100,Math.round(item.approvedTrainingMinutes/required*100)),bar=node('div',null,'final-progress'),fill=node('span');fill.style.width=percent+'%';bar.append(fill);bar.setAttribute('role','progressbar');bar.setAttribute('aria-valuemin','0');bar.setAttribute('aria-valuemax','100');bar.setAttribute('aria-valuenow',String(percent));bar.setAttribute('aria-label','Approved training hours');const progress=node('div',null,'final-progress-wrap');progress.append(bar,node('span',percent+'% of required hours approved','final-progress-label'));card.append(progress);}
                card.append(node('p',item.eligibilityBlocker||(item.eligible?'Hours requirement met. Review and approval are still required.':'You can prepare a draft. Final submission requires the remaining approved hours.'),'final-card-help'));
            }
            const open=node('button',role==='companySupervisor'&&(!item.record||!Object.keys(item.record).length)?'Prepare report':'View report');open.onclick=()=>show(item.placementId);card.append(open);host.append(card);
        }
        if (!visible.length) host.append(node('p', 'No reports or eligible assigned placements match this view. Missing assignments must be resolved by the coordinator.'));
        if (role === 'student') visible.forEach((item, index) => { const card = host.children[index]; card.insertBefore(node('p', item.approvedTrainingMinutes == null ? 'Approved attendance is unavailable for this placement.' : (item.approvedTrainingMinutes / 60).toFixed(2) + ' / 300 approved training hours'), card.lastChild); });
        const summary = by('finalSummary'); summary.replaceChildren(); for (const [label, count] of [['Assigned records', items.length], ['Awaiting final approval', items.filter(i => i.status === 'pendingAdminReview').length], ['Finally completed', items.filter(i => i.status === 'completed').length]]) { const card = node('article', null, 'final-stat'); card.append(node('p', label), node('strong', count)); summary.append(card); }
    }
    async function show(id) {
        try {
            let view = await api('detail', id), r = view.record || {}, revision = r.revisions?.[r.currentRevisionId], draft = r.draft || revision || {}, saving = false, dirty = false, pendingRequest = null;
            let weeklyIds = (draft.weeklyReports || []).map(report => report.reportId);
            if (embedded) role = view.role || role;
            const d = embedded ? node('section', null, 'final-inline-report') : popup('Final training report'), body = node('div'), status = node('p', states[view.status] || view.status, 'report-state'), error = node('p', '', 'error'), actions = node('div', null, 'dialog-actions');
            if (embedded) {
                const host = by('finalRecords'); host.replaceChildren(d);
                d.showModal = () => { d.open = true; by('finalNotice').textContent = ''; };
                d.close = () => { d.open = false; d.dispatchEvent(new Event('close')); d.remove(); };
                d.append(node('h2', 'Company final evaluation'));
            }
            d.append(status, body, error, actions);
            body.append(node('h3',view.studentName||'Student report'),node('p','Student ID: '+(view.studentId||'Unavailable'),'student-id'));
            const flow = node('ol', null, 'evaluation-flow');
            ['Company report', 'Academic review', 'Admin approval', 'Student result'].forEach((label, index) => { const item = node('li', label); if (index === ({ draft: 0, companyRevisionRequested: 0, pendingAcademicReview: 1, academicRevisionRequested: 1, pendingAdminReview: 2, completed: 3 })[view.status]) item.className = 'current'; flow.append(item); }); body.append(flow);
            const close = node('button', 'Close'); close.onclick = async () => { if (saving || (dirty && !await confirm('Discard unsaved changes?', 'Saved report revisions will remain in Firebase.'))) return; d.close(); }; actions.append(close);
            if (embedded) close.hidden = true;
            if (embedded) { editorDirtyListener(); }
            function editorDirtyListener() { d.addEventListener('input', () => { window.parent.postMessage({ type: 'uims-evaluation-dirty', placementId: id }, location.origin); }); }
            d.oncancel = event => { event.preventDefault(); close.click(); };
            const completed = view.status === 'completed';
            function appendOutcome(v) { if (!v.completed || !v.outcome) return; const outcome = node('section'); outcome.append(node('h3', 'Final grade: ' + v.outcome.finalGrade + ' / 100'), node('p', v.notification?.message || 'Training completion approved'), node('p', 'Completion notice: ' + (v.outcome.completionReport?.fileName || 'Unavailable')), node('p', 'Filename recorded only \u2014 not uploaded.', 'file-note')); body.append(outcome); }
            if (role === 'student') {
                appendOutcome(view);
                const refresh = async () => { if (!d.open) return; const next = await api('detail', id); view = next; status.textContent = states[next.status] || next.status; body.replaceChildren(node('p', 'Placement: ' + id)); appendOutcome(next); };
                activeRefresh = refresh; d.addEventListener('close', () => { if (activeRefresh === refresh) activeRefresh = null; d.remove(); }, { once: true }); d.showModal(); return;
            }
            body.append(node('p', view.approvedTrainingMinutes == null ? view.eligibilityBlocker : 'Approved training: '+(view.approvedTrainingMinutes/60).toFixed(2)+' / '+((view.requiredTrainingMinutes||18000)/60).toFixed(0)+' hours.'),node('p','Academic supervisor: '+(view.academicSupervisorName||view.academicSupervisorId||'Unavailable')));
            const editor = node('section'); body.append(editor); let useCalculatedGrade = !!draft.gradeCalculation;
            let automaticLoading = false, automaticReady = !!r.draft;
            if (role === 'companySupervisor' && ['draft', 'companyRevisionRequested'].includes(view.status)) {
                editor.classList.add('automatic-report-editor');
                const overview=node('div',null,'automatic-report-overview'),gradeBlock=node('div'),sourceBlock=node('div');
                gradeBlock.append(node('span','Calculated assessment grade','automatic-label'));
                const gradeNumber=node('strong',draft.percentage==null?'Unavailable':Number(draft.percentage).toFixed(2)+' / 100','automatic-grade-number');gradeBlock.append(gradeNumber);
                const autoStatus=node('p','Report fields are generated from authorized student records.','automatic-report-status');sourceBlock.append(node('strong','Automatic report'),autoStatus);overview.append(gradeBlock,sourceBlock);editor.append(overview);
                const percentage=field(editor,'Calculated percentage','percentage',draft.percentage,'number',true);percentage.readOnly=true;percentage.parentElement.hidden=true;
                const filename=field(editor,'Generated document reference','fileName',draft.attachment?.fileName||'final-training-report-'+view.studentId+'.pdf','text',true);filename.parentElement.hidden=true;
                const preview=node('details',null,'automatic-report-details');preview.append(node('summary','Report summary and assessment details'));editor.append(preview);
                const comments=field(preview,'Generated report summary','comments',draft.comments,'textarea',true);comments.readOnly=true;
                const breakdown=node('div',null,'automatic-grade-breakdown');preview.append(breakdown);
                const observations=node('details',null,'automatic-report-details');observations.append(node('summary',view.status==='companyRevisionRequested'?'Required response to requested revision':'Optional supervisor observations'));editor.append(observations);if(view.status==='companyRevisionRequested')observations.open=true;const response=field(observations,view.status==='companyRevisionRequested'?'Response to requested revision':'Additional company observations (optional)','companyResponse',draft.companyResponse,'textarea',view.status==='companyRevisionRequested');response.rows=3;response.placeholder=view.status==='companyRevisionRequested'?'Explain how you addressed the requested changes...':'Optional observations about the student\u2019s training progress...';
                const weekly=node('details',null,'evaluation-weekly'),summary=node('summary',(draft.weeklyReports||[]).length+' weekly report(s) included'),choices=node('div',null,'evaluation-weekly-choices');weekly.append(summary,choices);editor.append(weekly);
                function showWeekly(reports){choices.replaceChildren();weeklyIds=(reports||[]).map(x=>x.reportId);summary.textContent=weeklyIds.length+' weekly report(s) included';(reports||[]).forEach(report=>choices.append(node('p','Week '+report.weekNumber+' | '+report.status+' | '+(report.weekStart||'')+' - '+(report.weekEnd||''))));if(!weeklyIds.length)choices.append(node('p','No available weekly reports for this placement.'));}
                function showGrades(grade){breakdown.replaceChildren();if(!grade){breakdown.append(node('p','Saved evaluation. Refresh student data to calculate the assessment breakdown.'));return;}(grade.items||[]).forEach(item=>breakdown.append(node('p',item.kind+' | '+(item.title||item.id)+' | '+item.score+' / '+item.maximumScore+' = '+Number(item.percentage).toFixed(2)+'%')));breakdown.append(node('p',(grade.excludedCount||0)+' assessment(s) excluded because a valid approved/released grade is unavailable.'));}
                showWeekly(draft.weeklyReports);showGrades(draft.gradeCalculation);
                const refresh=node('button','Refresh student data', 'automatic-refresh');refresh.type='button';editor.append(refresh);
                async function fillAutomatically(manual){if(saving||automaticLoading)return;automaticLoading=true;automaticReady=false;refresh.disabled=true;actions.querySelectorAll('button').forEach(b=>{if(b!==close)b.disabled=true;});autoStatus.textContent='Loading current tasks, examinations, reports and approved hours...';try{const source=await api('sourceSummary',id);if(!d.open&&d.isConnected===false)return;const grade=source.gradeCalculation||{};percentage.value=grade.percentage==null?'':grade.percentage;gradeNumber.textContent=grade.percentage==null?'No released grades':Number(grade.percentage).toFixed(2)+' / 100';comments.value=source.comments||'';filename.value=source.fileName;useCalculatedGrade=true;automaticReady=grade.percentage!=null;showGrades(grade);showWeekly(source.weeklyReports);autoStatus.textContent=grade.percentage==null?'No eligible grades yet. A percentage will not be invented.':(grade.provisional?'Calculated from available grades; some assessments are excluded.':'Calculated from approved tasks and released examination results.');if(manual){dirty=true;pendingRequest=null;}error.textContent=grade.percentage==null?'Saving requires at least one valid approved task grade or released exam result.':'';}catch(e){error.textContent='Student records could not be loaded. '+e.message;autoStatus.textContent='Refresh student data to retry.';}finally{automaticLoading=false;refresh.disabled=false;actions.querySelectorAll('button').forEach(b=>{if(b!==close)b.disabled=false;});}}
                refresh.onclick=()=>fillAutomatically(true);
                editor.addEventListener('input',()=>{dirty=true;pendingRequest=null;});
                addAction('Generate and save report PDF','save');if(view.eligible&&r.draft)addAction('Submit saved report to academic supervisor','submit');
                if(!view.eligible)body.append(node('p','Draft preparation is available. Final submission requires '+((view.requiredTrainingMinutes||18000)/60)+' approved hours.','automatic-report-status'));
                if(!r.draft){automaticLoading=true;setTimeout(()=>{automaticLoading=false;fillAutomatically(false);},0);}
            }
            if(role!=='student'&&(r.draft||revision)){
                const documentRevision=r.draft?'draft':r.currentRevisionId,documentSource=r.draft||revision;
                const pdfSection=node('section',null,'final-pdf-actions');pdfSection.append(node('h3','Private assessment PDF'));
                const generatePdf=node('button','Generate assessment PDF');generatePdf.type='button';generatePdf.onclick=async()=>{if(saving||automaticLoading||dirty){error.textContent='Save current changes before generating the PDF.';return;}generatePdf.disabled=true;try{await api('generatePdf',id,{revisionId:documentRevision,requestId:crypto.randomUUID()});d.close();await show(id);}catch(e){error.textContent=e.message;}finally{generatePdf.disabled=false;}};pdfSection.append(generatePdf);
                if(documentSource.attachment?.uploadStatus==='ready'){const download=node('a','Download PDF with grade details');download.href='FinalReportsApi.ashx?action=download&placementId='+encodeURIComponent(id)+'&revisionId='+encodeURIComponent(documentRevision);pdfSection.append(download);}else pdfSection.append(node('p','PDF generation uses the saved report and approved/released assessment grades.'));
                body.append(pdfSection);
            }
            if (revision) {
                // Readable summary of the submitted report: grade, dates and text, with the internal revision id only as a small reference.
                const report = node('section', null, 'final-revision-card'), facts = node('dl', null, 'final-revision-facts');
                for (const [label, value, big] of [['Company grade', (revision.percentage ?? '—') + ' / 100', true], ['Submitted', revision.submittedAt ? new Date(revision.submittedAt).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' }) : '—'], ['Attachment', revision.attachment?.fileName || 'None']]) { const row = node('div', null, big ? 'is-big' : ''); row.append(node('dt', label), node('dd', value)); facts.append(row); }
                report.append(node('h3', 'Submitted company report'), facts);
                if (revision.comments) { report.append(node('h4', 'Report summary'), node('p', revision.comments, 'final-revision-text')); }
                if (revision.companyResponse) { report.append(node('h4', 'Company response'), node('p', revision.companyResponse, 'final-revision-text')); }
                report.append(node('small', 'Reference: ' + r.currentRevisionId, 'final-revision-ref'));
                body.append(report);
            }
            function weeklyHistory(host, reports) { if (!reports?.length) return; const box = node('section', null, 'evaluation-weekly-snapshots'); box.append(node('h3', 'Included weekly report snapshots')); for (const report of reports) { const card = node('article'); card.append(node('strong', 'Week ' + report.weekNumber + ' \u00b7 ' + report.status), node('p', 'Report: ' + report.reportId + ' \u00b7 Revision: ' + report.revisionId), node('p', report.companyComments || 'No summary'), node('small', report.weekStart + ' \u2014 ' + report.weekEnd)); box.append(card); } host.append(box); }
            weeklyHistory(body, revision?.weeklyReports);
            if (role === 'academicSupervisor' && ['pendingAcademicReview', 'academicRevisionRequested'].includes(view.status)) { field(editor, 'Academic feedback / response to admin', 'feedback', '', 'textarea', view.status === 'academicRevisionRequested'); addAction('Approve current revision', 'academicApprove'); addAction('Return to company for revision', 'companyRevision'); }
            if (role === 'coordinator' && view.status === 'pendingAdminReview' && !r.finalization) { field(editor, 'Admin feedback', 'feedback', '', 'textarea'); field(editor, 'Explicit approved final grade (0\u2013100)', 'finalGrade', revision?.percentage, 'number', true); field(editor, 'Student completion notice filename \u2014 not uploaded', 'fileName', '', 'text', true); addAction('Final approval and training completion', 'finalApprove'); addAction('Return to academic supervisor', 'adminCorrection'); }
            if (role === 'coordinator' && r.finalization && !completed) addAction('Reconcile interrupted finalization', 'reconcile');
            if (completed) { body.append(node('p', 'Completed records are read-only.')); appendOutcome({ completed: true, outcome: r.outcome, notification: r.studentNotification }); }
            const history = node('section', null, 'final-history'); history.append(node('h3', 'Immutable revisions and review history'));
            for (const [key, rev] of Object.entries(r.revisions || {})) { const card = node('article'); card.append(node('strong', key), node('p', rev.comments), node('p', 'Company response: ' + (rev.companyResponse || 'None')), node('p', 'Filename: ' + rev.attachment?.fileName + ' \u2014 not uploaded')); weeklyHistory(card, rev.weeklyReports); history.append(card); }
            for (const category of ['academicReviews', 'adminReviews']) for (const review of Object.values(r[category] || {})) { const card = node('article'); card.append(node('strong', category === 'academicReviews' ? 'Academic review' : 'Administration review'), node('p', review.action + ' \u00b7 ' + review.revisionId), node('p', review.feedback || 'No feedback'), node('p', review.at)); history.append(card); }
            body.append(history); d.addEventListener('close', () => d.remove(), { once: true }); d.showModal();
            function addAction(label, action) {
                const button = node('button', label); actions.append(button);
                button.onclick = async () => {
                    if (saving || automaticLoading) return;
                    if(role==='companySupervisor'&&['save','submit'].includes(action)&&!automaticReady){error.textContent='Load valid student assessment grades before saving. Use Refresh student data.';return;}
                    const input = { revisionId: r.currentRevisionId || '', expectedDraftVersion: r.draftVersion || '' }; const controls = [...editor.querySelectorAll('input[name],textarea[name]')];
                    const feedback = editor.querySelector('[name=feedback]'); if (feedback) feedback.required = ['companyRevision', 'adminCorrection'].includes(action) || view.status === 'academicRevisionRequested';
                    const relevant = action === 'adminCorrection' ? controls.filter(c => c.name === 'feedback') : controls;
                    const temporarilyDisabled = controls.filter(c => !relevant.includes(c)); temporarilyDisabled.forEach(c => c.disabled = true);
                    const valid = validate(editor); temporarilyDisabled.forEach(c => c.disabled = false); if (!valid) return;
                    if (action === 'submit' && dirty) { error.textContent = 'Save your report changes before submitting.'; return; }
                    relevant.forEach(c => input[c.name] = c.value);
                    if (action === 'save') { input.weeklyReportIds = weeklyIds; input.useCalculatedGrade = useCalculatedGrade; }
                    if (action === 'submit') for (const key of Object.keys(input)) if (!['revisionId', 'expectedDraftVersion'].includes(key)) delete input[key];
                    if (!await confirm(label + '?', 'This decision will be saved to Firebase for the current report revision. Assessment PDFs are generated from the saved report through private storage.')) return;
                    const signature = JSON.stringify({ action, input }); if (!pendingRequest || pendingRequest.signature !== signature) pendingRequest = { signature, id: crypto.randomUUID() }; input.requestId = pendingRequest.id;
                    saving = true; actions.querySelectorAll('button').forEach(b => b.disabled = true);
                    try { await api(action, id, input); dirty = false; if(action==='save'){try{await api('generatePdf',id,{revisionId:'draft',requestId:crypto.randomUUID()});}catch(pdfError){error.textContent='Report saved, but PDF generation failed. Do not save again; use Generate assessment PDF after reopening. '+pdfError.message;saving=false;actions.querySelectorAll('button').forEach(b=>b.disabled=false);return;}} if (embedded) window.parent.postMessage({ type: 'uims-evaluation-saved', placementId: id }, location.origin); d.close(); if (!embedded) await load(); await show(id); }
                    catch (e) { error.textContent = e.message + ' Retrying unchanged fields reuses the same requestId.'; }
                    finally { saving = false; actions.querySelectorAll('button').forEach(b => b.disabled = false); }
                };
            }
        } catch (error) { by('finalNotice').textContent = error.message; if (embedded) { const retry = node('button', 'Retry evaluation'); retry.onclick = () => { by('finalNotice').textContent = 'Loading authorized evaluation...'; show(id); }; by('finalRecords').replaceChildren(retry); } }
    }
    by('finalSearch').oninput = render; by('finalFilter').onchange = render; by('finalReload').onclick = load;
    if (embedded) { show(placementScope); }
    else { load(); setInterval(() => { if (!document.hidden && !document.querySelector('dialog[open] input,dialog[open] textarea')) load(); }, 30000); window.addEventListener('focus', load); }
}());
