(function () {
    'use strict';
    const by = id => document.getElementById(id), states = { draft: 'Draft', pendingAcademicReview: 'Awaiting academic review', companyRevisionRequested: 'Company revision requested', pendingAdminReview: 'Academically approved — awaiting final admin approval.', academicRevisionRequested: 'Returned to academic supervisor by admin', completed: 'Training completed', completionConsistencyPending: 'Completion verification requires administration review' };
    let items = [], role = '', loading = false, activeRefresh = null;
    const placementScope = new URLSearchParams(location.search).get('placementId');
    const embedded = new URLSearchParams(location.search).get('embedded') === '1' && !!placementScope;
    if (embedded) document.body.classList.add('final-embedded');
    const node = (tag, text, cls) => { const el = document.createElement(tag); if (text != null) el.textContent = String(text); if (cls) el.className = cls; if (tag === 'button') el.type = 'button'; return el; };
    async function api(action, id, input) {
        const response = await fetch('FinalReportsApi.ashx?action=' + action + (id ? '&placementId=' + encodeURIComponent(id) : ''), { method: input ? 'POST' : 'GET', cache: 'no-store', credentials: 'same-origin', signal: AbortSignal.timeout(60000), headers: input ? { 'Content-Type': 'application/json' } : {}, body: input ? JSON.stringify(input) : undefined });
        const result = await response.json(); if (!response.ok) throw Error(result.message || 'The report could not be verified.'); return result;
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
        if (first) { first.focus(); first.scrollIntoView({ block: 'center' }); } return !first;
    }
    async function load() {
        if (loading) return; loading = true;
        try { const data = await api('list'); items = placementScope ? data.items.filter(item => item.placementId === placementScope) : data.items; role = data.role; render(); by('finalNotice').className = ''; by('finalNotice').textContent = placementScope && !items.length ? 'This placement is unavailable or not authorized for your current account.' : 'Loaded authorized persisted Firebase reports.'; if (role === 'student') await loadStudentProgress(); if (activeRefresh) await activeRefresh(); }
        catch (error) { try { const connection = await api('connection'); by('finalNotice').textContent = connection.connected ? 'Firebase is connected. ' + error.message : error.message; } catch (_) { by('finalNotice').textContent = error.message; } by('finalNotice').className = 'error'; }
        finally { loading = false; }
    }
    async function loadStudentProgress() {
        let section = by('studentProgress');
        if (!section) { section = node('section'); section.id = 'studentProgress'; by('finalRecords').before(section); }
        try {
            const response = await fetch('TasksApi.ashx', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(60000) });
            const result = await response.json(); if (!response.ok) throw Error(result.message || 'Task progress is unavailable.');
            const tasks = result.tasks || []; section.replaceChildren(node('h2', 'Your task progress'));
            const grid = node('div', null, 'student-progress-grid');
            for (const task of tasks) { const card = node('article', null, 'report-card'), open = node('button', 'View task progress'); card.append(node('h3', task.title), node('p', task.status, 'report-state')); open.onclick = () => { const d = popup(task.title), close = node('button', 'Close'); d.append(node('p', task.description), node('p', task.instructions || ''), node('p', 'Current status: ' + task.status)); if (task.grade != null) d.append(node('p', 'Released company grade: ' + task.grade + ' / ' + (task.maximumScore || 100))); close.onclick = () => d.close(); d.append(close); d.addEventListener('close', () => d.remove(), { once: true }); d.showModal(); }; card.append(open); grid.append(card); }
            if (!tasks.length) section.append(node('p', 'No tasks are assigned to your authorized placement.')); section.append(grid);
        } catch (error) { section.replaceChildren(node('p', error.message, 'error')); }
    }
    function render() {
        const host = by('finalRecords'); host.replaceChildren(); const search = by('finalSearch').value.toLowerCase(), filter = by('finalFilter').value;
        const visible = items.filter(i => (!filter || i.status === filter) && (!search || (i.studentId + ' ' + i.placementId).toLowerCase().includes(search)));
        for (const item of visible) { const card = node('article', null, 'report-card'); card.append(node('h2', item.studentId || 'Your training'), node('p', item.placementId), node('span', states[item.status] || item.status, 'report-state')); if (role !== 'student') card.append(node('p', item.approvedTrainingMinutes == null ? item.eligibilityBlocker || 'Attendance unavailable' : item.approvedTrainingMinutes + ' / 18,000 approved minutes')); const open = node('button', 'View report'); open.onclick = () => show(item.placementId); card.append(open); host.append(card); }
        if (!visible.length) host.append(node('p', 'No reports or eligible assigned placements match this view. Missing assignments must be resolved by the coordinator.'));
        if (role === 'student') visible.forEach((item, index) => { const card = host.children[index]; card.insertBefore(node('p', item.approvedTrainingMinutes == null ? 'Approved attendance is unavailable for this placement.' : (item.approvedTrainingMinutes / 60).toFixed(2) + ' / 300 approved training hours'), card.lastChild); });
        const navigation = document.querySelector('.final-workspace header a');
        navigation.href = ({ student: 'Student/StudentDashboard.aspx', companySupervisor: 'Company/CompanyDashboard.aspx', academicSupervisor: 'Academic/AcademicDashboard.aspx', coordinator: 'Coordinator/CoordinatorDashboard.aspx' })[role] || 'Public/home.aspx'; navigation.textContent = '← Back to your workspace';
        const summary = by('finalSummary'); summary.replaceChildren(); for (const [label, count] of [['Assigned records', items.length], ['Awaiting final approval', items.filter(i => i.status === 'pendingAdminReview').length], ['Finally completed', items.filter(i => i.status === 'completed').length]]) { const card = node('article'); card.append(node('p', label), node('strong', count)); summary.append(card); }
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
            body.append(node('p', 'Placement: ' + id));
            const flow = node('ol', null, 'evaluation-flow');
            ['Company report', 'Academic review', 'Admin approval', 'Student result'].forEach((label, index) => { const item = node('li', label); if (index === ({ draft: 0, companyRevisionRequested: 0, pendingAcademicReview: 1, academicRevisionRequested: 1, pendingAdminReview: 2, completed: 3 })[view.status]) item.className = 'current'; flow.append(item); }); body.append(flow);
            const close = node('button', 'Close'); close.onclick = async () => { if (saving || (dirty && !await confirm('Discard unsaved changes?', 'Saved report revisions will remain in Firebase.'))) return; d.close(); }; actions.append(close);
            if (embedded) close.hidden = true;
            if (embedded) { editorDirtyListener(); }
            function editorDirtyListener() { d.addEventListener('input', () => { window.parent.postMessage({ type: 'uims-evaluation-dirty', placementId: id }, location.origin); }); }
            d.oncancel = event => { event.preventDefault(); close.click(); };
            const completed = view.status === 'completed';
            function appendOutcome(v) { if (!v.completed || !v.outcome) return; const outcome = node('section'); outcome.append(node('h3', 'Final grade: ' + v.outcome.finalGrade + ' / 100'), node('p', v.notification?.message || 'Training completion approved'), node('p', 'Completion notice: ' + (v.outcome.completionReport?.fileName || 'Unavailable')), node('p', 'Filename recorded only — not uploaded.', 'file-note')); body.append(outcome); }
            if (role === 'student') {
                appendOutcome(view);
                const refresh = async () => { if (!d.open) return; const next = await api('detail', id); view = next; status.textContent = states[next.status] || next.status; body.replaceChildren(node('p', 'Placement: ' + id)); appendOutcome(next); };
                activeRefresh = refresh; d.addEventListener('close', () => { if (activeRefresh === refresh) activeRefresh = null; d.remove(); }, { once: true }); d.showModal(); return;
            }
            body.append(node('p', view.approvedTrainingMinutes == null ? view.eligibilityBlocker : view.approvedTrainingMinutes + ' approved minutes. Required: 18,000.'));
            const editor = node('section'); body.append(editor);
            if (role === 'companySupervisor' && ['draft', 'companyRevisionRequested'].includes(view.status)) {
                field(editor, 'Company evaluation percentage (0–100)', 'percentage', draft.percentage, 'number', true);
                field(editor, 'Company report and comments', 'comments', draft.comments, 'textarea', true);
                field(editor, 'Company final report filename — not uploaded', 'fileName', draft.attachment?.fileName, 'text', true);
                field(editor, 'Response to revision feedback', 'companyResponse', draft.companyResponse, 'textarea', view.status === 'companyRevisionRequested');
                const weekly = node('section', null, 'evaluation-weekly'), fetchButton = node('button', 'Fetch this student’s weekly reports'), summary = node('p', weeklyIds.length + ' weekly report(s) selected'), choices = node('div', null, 'evaluation-weekly-choices');
                weekly.append(node('h3', 'Weekly reports included in the final report'), node('p', 'Select reports from this placement. Their current revision summaries are preserved with the submitted final report.'), fetchButton, summary, choices); editor.append(weekly);
                fetchButton.onclick = async () => { if (saving) return; fetchButton.disabled = true; choices.replaceChildren(node('p', 'Loading reports from Firebase...')); try { const data = await api('weeklyReports', id); choices.replaceChildren(); if (!data.reports.length) choices.append(node('p', 'No submitted weekly reports exist for this student and placement. You can complete the final report without adding reports.')); for (const report of data.reports) { const label = node('label', null, 'weekly-report-choice'), check = node('input'); check.type = 'checkbox'; check.checked = weeklyIds.includes(report.reportId); const description = node('span'); description.append(node('strong', 'Week ' + report.weekNumber + ' · ' + report.status), node('small', (report.weekStart || '') + ' — ' + (report.weekEnd || '')), node('p', report.companyComments || 'No company summary provided.')); check.onchange = () => { weeklyIds = check.checked ? [...weeklyIds.filter(x => x !== report.reportId), report.reportId] : weeklyIds.filter(x => x !== report.reportId); dirty = true; pendingRequest = null; summary.textContent = weeklyIds.length + ' weekly report(s) selected'; if (embedded) window.parent.postMessage({ type: 'uims-evaluation-dirty', placementId: id }, location.origin); }; label.append(check, description); choices.append(label); } } catch (e) { choices.replaceChildren(node('p', e.message, 'error')); } finally { fetchButton.disabled = false; } };
                editor.addEventListener('input', () => { dirty = true; pendingRequest = null; });
                addAction('Save report', 'save'); if (view.eligible && r.draft) addAction('Submit saved report to academic supervisor', 'submit'); else if (!r.draft) body.append(node('p', 'Save your report first. Submission is enabled after verifying 300 approved hours.'));
            }
            if (revision) { const report = node('section'); report.append(node('h3', 'Current submitted revision'), node('p', r.currentRevisionId), node('p', 'Company percentage: ' + revision.percentage + ' / 100'), node('p', revision.comments), node('p', 'Filename only: ' + revision.attachment?.fileName), node('p', 'Company response: ' + (revision.companyResponse || 'None'))); body.append(report); }
            function weeklyHistory(host, reports) { if (!reports?.length) return; const box = node('section', null, 'evaluation-weekly-snapshots'); box.append(node('h3', 'Included weekly report snapshots')); for (const report of reports) { const card = node('article'); card.append(node('strong', 'Week ' + report.weekNumber + ' · ' + report.status), node('p', 'Report: ' + report.reportId + ' · Revision: ' + report.revisionId), node('p', report.companyComments || 'No summary'), node('small', report.weekStart + ' — ' + report.weekEnd)); box.append(card); } host.append(box); }
            weeklyHistory(body, revision?.weeklyReports);
            if (role === 'academicSupervisor' && ['pendingAcademicReview', 'academicRevisionRequested'].includes(view.status)) { field(editor, 'Academic feedback / response to admin', 'feedback', '', 'textarea', view.status === 'academicRevisionRequested'); addAction('Approve current revision', 'academicApprove'); addAction('Return to company for revision', 'companyRevision'); }
            if (role === 'coordinator' && view.status === 'pendingAdminReview' && !r.finalization) { field(editor, 'Admin feedback', 'feedback', '', 'textarea'); field(editor, 'Explicit approved final grade (0–100)', 'finalGrade', revision?.percentage, 'number', true); field(editor, 'Student completion notice filename — not uploaded', 'fileName', '', 'text', true); addAction('Final approval and training completion', 'finalApprove'); addAction('Return to academic supervisor', 'adminCorrection'); }
            if (role === 'coordinator' && r.finalization && !completed) addAction('Reconcile interrupted finalization', 'reconcile');
            if (completed) { body.append(node('p', 'Completed records are read-only.')); appendOutcome({ completed: true, outcome: r.outcome, notification: r.studentNotification }); }
            const history = node('section', null, 'final-history'); history.append(node('h3', 'Immutable revisions and review history'));
            for (const [key, rev] of Object.entries(r.revisions || {})) { const card = node('article'); card.append(node('strong', key), node('p', rev.comments), node('p', 'Company response: ' + (rev.companyResponse || 'None')), node('p', 'Filename: ' + rev.attachment?.fileName + ' — not uploaded')); weeklyHistory(card, rev.weeklyReports); history.append(card); }
            for (const category of ['academicReviews', 'adminReviews']) for (const review of Object.values(r[category] || {})) { const card = node('article'); card.append(node('strong', category === 'academicReviews' ? 'Academic review' : 'Administration review'), node('p', review.action + ' · ' + review.revisionId), node('p', review.feedback || 'No feedback'), node('p', review.at)); history.append(card); }
            body.append(history); d.addEventListener('close', () => d.remove(), { once: true }); d.showModal();
            function addAction(label, action) {
                const button = node('button', label); actions.append(button);
                button.onclick = async () => {
                    if (saving) return;
                    const input = { revisionId: r.currentRevisionId || '', expectedDraftVersion: r.draftVersion || '' }; const controls = [...editor.querySelectorAll('input[name],textarea[name]')];
                    const feedback = editor.querySelector('[name=feedback]'); if (feedback) feedback.required = ['companyRevision', 'adminCorrection'].includes(action) || view.status === 'academicRevisionRequested';
                    const relevant = action === 'adminCorrection' ? controls.filter(c => c.name === 'feedback') : controls;
                    const temporarilyDisabled = controls.filter(c => !relevant.includes(c)); temporarilyDisabled.forEach(c => c.disabled = true);
                    const valid = validate(editor); temporarilyDisabled.forEach(c => c.disabled = false); if (!valid) return;
                    if (action === 'submit' && dirty) { error.textContent = 'Save your report changes before submitting.'; return; }
                    relevant.forEach(c => input[c.name] = c.value);
                    if (action === 'save') input.weeklyReportIds = weeklyIds;
                    if (action === 'submit') for (const key of Object.keys(input)) if (!['revisionId', 'expectedDraftVersion'].includes(key)) delete input[key];
                    if (!await confirm(label + '?', 'This decision will be saved to Firebase for the current report revision. Filename-only attachments do not upload a file.')) return;
                    const signature = JSON.stringify({ action, input }); if (!pendingRequest || pendingRequest.signature !== signature) pendingRequest = { signature, id: crypto.randomUUID() }; input.requestId = pendingRequest.id;
                    saving = true; actions.querySelectorAll('button').forEach(b => b.disabled = true);
                    try { await api(action, id, input); dirty = false; if (embedded) window.parent.postMessage({ type: 'uims-evaluation-saved', placementId: id }, location.origin); d.close(); if (!embedded) await load(); await show(id); }
                    catch (e) { error.textContent = e.message + ' Retrying unchanged fields reuses the same requestId.'; }
                    finally { saving = false; actions.querySelectorAll('button').forEach(b => b.disabled = false); }
                };
            }
        } catch (error) { by('finalNotice').textContent = error.message; if (embedded) { const retry = node('button', 'Retry evaluation'); retry.onclick = () => { by('finalNotice').textContent = 'Loading authorized evaluation...'; show(id); }; by('finalRecords').replaceChildren(retry); } }
    }
    by('finalSearch').oninput = render; by('finalFilter').onchange = render; by('finalReload').onclick = load;
    if (embedded) { show(placementScope); }
    else { load(); setInterval(() => { if (!document.hidden && !document.querySelector('dialog[open] input,dialog[open] textarea')) load(); }, 5000); window.addEventListener('focus', load); }
}());
