(function () {
    'use strict';
    const by = id => document.getElementById(id), notice = by('traineesAvailability');
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    let records = [], page = 0, loading = false, selected = null;
    document.querySelector('.trainees-table').style.minWidth = '780px';
    document.querySelector('.trainees-table-wrap').style.overflowX = 'auto';
    const labels = { training: 'Currently training', completed: 'Completed training' };
    function render() {
        const query = by('traineeSearch').value.trim().toLowerCase();
        const filtered = records.filter(r => (!query || (r.name + ' ' + r.studentId).toLowerCase().includes(query)) && (!by('internshipFilter').value || r.opportunityId === by('internshipFilter').value) && (!by('statusFilter').value || r.status === by('statusFilter').value));
        const pages = Math.max(1, Math.ceil(filtered.length / 10)); page = Math.min(page, pages - 1);
        document.querySelector('.trainees-table tbody').innerHTML = filtered.slice(page * 10, page * 10 + 10).map(r => '<tr><td><div class="trainee-person"><span class="trainee-avatar" aria-hidden="true">' + escape(r.name.split(/\s+/).filter(Boolean).slice(0,2).map(n=>n[0]).join('')) + '</span><div><strong>' + escape(r.name) + '</strong><small>' + escape(r.studentId) + '</small></div></div></td><td>' + escape(r.internship || 'Internship') + '</td><td>' + escape([r.startDate, r.endDate].filter(Boolean).join(' – ') || 'Dates not recorded') + '</td><td>' + r.completedTasks + ' / ' + r.taskCount + ' tasks<div class="trainee-task-progress"><span style="width:' + Math.min(100,Math.max(0,r.taskCount?100*r.completedTasks/r.taskCount:0)) + '%"></span></div></td><td><span class="trainee-state ' + (r.status==='completed'?'completed':'') + '">' + escape(labels[r.status]) + '</span></td><td><button type="button" data-trainee="' + escape(r.id) + '">View details</button></td></tr>').join('') || '<tr><td colspan="6"><div class="trainees-empty"><h3>No matching trainees</h3><p>Students appear after company acceptance and student confirmation.</p></div></td></tr>';
        document.querySelector('.trainees-list-footer > span').textContent = filtered.length + ' placements · Page ' + (page + 1) + ' / ' + pages;
        const buttons = document.querySelectorAll('.trainees-pagination button'); buttons[0].disabled = page === 0; buttons[1].disabled = page >= pages - 1;
        document.querySelector('.trainees-pagination span').textContent = String(page + 1);
    }
    function details(id) {
        selected = records.find(r => r.id === id); if (!selected) return;
        const r = selected;
        document.querySelectorAll('.trainees-info dd').forEach((el, i) => el.textContent = [r.name, r.studentId, r.department || 'Not recorded', r.internship || 'Internship', 'Your company workspace', [r.startDate, r.endDate].filter(Boolean).join(' – ') || 'Dates not recorded'][i]);
        by('traineeProgressSummary').textContent = r.completedTasks + ' / ' + r.taskCount + ' completed tasks. ' + (r.requiredHours ? r.requiredHours + ' required training hours.' : 'Required hours not recorded.');
        by('companyTraineeDialog').showModal();
    }
    async function refresh() {
        if (loading || document.hidden) return; loading = true;
        try {
            const response = await fetch('../TasksApi.ashx?action=trainees', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(30000) });
            const data = await response.json(); if (!response.ok) throw Error(data.message || 'Unable to load company trainees.');
            records = data.trainees; const chosen = by('internshipFilter').value;
            const internships = new Map(records.map(r => [r.opportunityId, r.internship || 'Internship']));
            by('internshipFilter').innerHTML = '<option value="">All internships</option>' + [...internships].map(([id, title]) => '<option value="' + escape(id) + '">' + escape(title) + '</option>').join('');
            if (internships.has(chosen)) by('internshipFilter').value = chosen;
            document.querySelectorAll('.trainees-stat strong').forEach((el, i) => { el.textContent = [data.statistics.total, data.statistics.training, data.statistics.completed, data.statistics.assignedTasks][i]; el.removeAttribute('aria-label'); });
            document.querySelector('.trainees-filters').disabled = false; document.querySelector('.trainees-data-badge').textContent = 'Live Firebase records';
            notice.textContent = 'Connected to Firebase. Only confirmed placements at your company are shown.'; render();
            if (by('companyTraineeDialog').open && selected) { const id = selected.id; const latest = records.find(r => r.id === id); if (!latest) by('companyTraineeDialog').close(); }
        } catch (error) {
            notice.textContent = error.message + ' Retrying automatically.';
            document.querySelector('.trainees-data-badge').textContent = 'Connection interrupted';
            document.querySelectorAll('.trainees-stat strong').forEach(el => el.textContent = '—');
            document.querySelector('.trainees-table tbody').innerHTML = '<tr><td colspan="6">Trainee records are temporarily unavailable. Retrying the authorized Firebase service…</td></tr>';
            document.querySelector('.trainees-filters').disabled = true;
        } finally { loading = false; }
    }
    by('statusFilter').innerHTML = '<option value="">All statuses</option><option value="training">Currently training</option><option value="completed">Completed training</option>';
    ['traineeSearch', 'internshipFilter', 'statusFilter'].forEach(id => by(id).addEventListener(id === 'traineeSearch' ? 'input' : 'change', () => { page = 0; render(); }));
    document.querySelector('.trainees-table tbody').addEventListener('click', event => { const button = event.target.closest('[data-trainee]'); if (button) details(button.dataset.trainee); });
    document.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => by('companyTraineeDialog').close()));
    document.querySelectorAll('.trainees-pagination button').forEach((button, i) => button.addEventListener('click', () => { page += i ? 1 : -1; render(); }));
    // Hours are not derived from task completion. Use the existing authorized attendance workflow.
    by('saveApprovedHours').parentElement.querySelectorAll('label,p,button').forEach(el => { if (el.id !== 'traineeProgressSummary') el.hidden = true; });
    function node(tag, text, cls) { const el = document.createElement(tag); if (text != null) el.textContent = String(text); if (cls) el.className = cls; return el; }
    function workspace(title) {
        const dialog = node('dialog', null, 'trainee-workflow-dialog'), heading = node('header'), h = node('h2', title), close = node('button', 'Back to trainee', 'trainee-workflow-back'), body = node('section'); close.type = 'button';
        heading.append(h, close); dialog.append(heading, body); document.body.append(dialog);
        async function leave() {
            const frame = body.querySelector('iframe');
            if (frame?.dataset.dirty === 'true') {
                if (dialog.querySelector('.trainee-leave-confirm')) return;
                const confirm = node('dialog', null, 'trainee-leave-confirm'), title = node('h3', 'Close this evaluation?'), text = node('p', 'Unsaved entries will be discarded. Saved revisions remain in Firebase.'), cancel = node('button', 'Continue editing'), accept = node('button', 'Discard and close'); cancel.type = accept.type = 'button'; confirm.append(title, text, cancel, accept); document.body.append(confirm); cancel.onclick = () => confirm.close(); accept.onclick = () => { confirm.close(); dialog.close(); }; confirm.addEventListener('close', () => confirm.remove(), { once: true }); confirm.showModal(); cancel.focus(); return;
            }
            dialog.close();
        }
        close.onclick = leave; dialog.addEventListener('cancel', event => { event.preventDefault(); leave(); }); dialog.addEventListener('close', () => { dialog.remove(); by('companyTraineeDialog').querySelector('[data-close]')?.focus(); }, { once: true }); dialog.showModal(); return { dialog, body };
    }
    by('viewTraineeProgress').onclick = async () => {
        if (!selected) return; const trainee = { ...selected }, view = workspace('Training progress · ' + trainee.name), status = node('p', 'Loading authorized placement records…'); view.body.append(status);
        try {
            const [tasksResponse, evaluationResponse] = await Promise.all([
                fetch('../TasksApi.ashx', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(60000) }),
                fetch('../FinalReportsApi.ashx?action=detail&placementId=' + encodeURIComponent(trainee.placementId), { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(60000) })
            ]);
            const data = await tasksResponse.json(); if (!tasksResponse.ok) throw Error(data.message || 'Task progress unavailable.');
            const evaluation = await evaluationResponse.json(); if (!view.dialog.open) return;
            const tasks = (data.tasks || []).filter(task => task.placementId === trainee.placementId && task.studentId === trainee.studentId);
            status.textContent = trainee.studentId + ' · ' + trainee.internship;
            const stats = node('div', null, 'trainee-progress-stats');
            for (const [label, value] of [['Assigned tasks', tasks.length], ['Completed tasks', tasks.filter(task => String(task.status).toLowerCase() === 'completed').length], ['Approved training', evaluationResponse.ok && evaluation.approvedTrainingMinutes != null ? (evaluation.approvedTrainingMinutes / 60).toFixed(2) + ' / 300 hours' : 'Unavailable']]) { const card = node('article'); card.append(node('strong', value), node('span', label)); stats.append(card); } view.body.append(stats);
            if (!evaluationResponse.ok) view.body.append(node('p', evaluation.message || 'Attendance verification is unavailable.', 'trainee-workflow-error'));
            else view.body.append(node('p', evaluation.eligibilityBlocker || (evaluation.eligible ? 'Eligible for company final report submission.' : '300 approved hours are required before final report submission.')));
            for (const task of tasks) { const card = node('article', null, 'trainee-progress-task'); card.append(node('h3', task.title), node('span', task.status, 'trainee-state'), node('p', task.description || ''), node('p', 'Deadline: ' + (task.dueDate || 'Not recorded'))); if (task.grade != null) card.append(node('p', 'Company grade: ' + task.grade + ' / ' + (task.maximumScore ?? 'Not recorded'))); view.body.append(card); }
            if (!tasks.length) view.body.append(node('p', 'No tasks are linked to this student and placement.'));
        } catch (error) { if (view.dialog.open) status.textContent = error.message; }
    };
    by('openTraineeEvaluation').onclick = () => {
        if (!selected) return; const trainee = { ...selected }, view = workspace('Evaluate trainee · ' + trainee.name), frame = document.createElement('iframe');
        frame.title = 'Final evaluation for student ' + trainee.studentId; frame.className = 'trainee-evaluation-frame'; frame.src = '../FinalReports.aspx?embedded=1&placementId=' + encodeURIComponent(trainee.placementId);
        const onMessage = event => { if (event.origin !== location.origin || event.source !== frame.contentWindow || event.data?.placementId !== trainee.placementId) return; if (event.data.type === 'uims-evaluation-dirty') frame.dataset.dirty = 'true'; if (event.data.type === 'uims-evaluation-saved') { frame.dataset.dirty = 'false'; refresh(); } };
        window.addEventListener('message', onMessage); view.dialog.addEventListener('close', () => window.removeEventListener('message', onMessage), { once: true }); view.body.append(frame);
    };
    window.addEventListener('focus', refresh); document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    refresh(); setInterval(refresh, 5000);
}());
