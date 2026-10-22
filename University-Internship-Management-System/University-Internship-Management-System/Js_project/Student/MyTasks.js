(function () {
    'use strict';
    const api = '../TasksApi.ashx?role=student';
    let state = { tasks: [], submissions: [] }, selectedId = null, busy = false;
    const by = id => document.getElementById(id);
    const escapeHtml = value => String(value == null ? '' : value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const message = (value, error) => {
        const notice = by('pageNotice');
        if (notice) { notice.hidden = false; notice.textContent = value; notice.classList.toggle('text-danger', !!error); notice.classList.toggle('text-success', !error); }
    };
    const connection = value => { const status = document.querySelector('.preview-note'); if (status) status.textContent = value; };
    const modal = id => window.bootstrap && bootstrap.Modal.getOrCreateInstance(by(id));
    async function request(action, data) {
        const response = await fetch(api + (action ? '&action=' + encodeURIComponent(action) : ''), {
            method: action ? 'POST' : 'GET', cache: 'no-store', headers: action ? { 'Content-Type': 'application/json' } : {},
            body: action ? JSON.stringify(data || {}) : undefined
        });
        let result;
        try { result = await response.json(); } catch (_) { throw new Error('Firebase returned an unreadable response.'); }
        if (!response.ok) throw new Error(result.message || ('Firebase request failed (' + response.status + ').'));
        return result;
    }
    function statusLabel(status) { return ({ assigned: 'Assigned', inprogress: 'In progress', submitted: 'Awaiting review', revision: 'Revision requested', completed: 'Completed' })[status] || status || 'Unknown'; }
    function dateLabel(value) { const date = new Date(value); return isNaN(date.getTime()) ? (value || 'No deadline') : date.toLocaleDateString(); }
    function render() {
        const query = (by('taskSearch')?.value || '').trim().toLowerCase();
        const filter = by('taskFilter')?.value || '';
        const tasks = state.tasks.filter(task => (!query || (task.title + ' ' + task.description).toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter || filter === 'assigned' && task.status === 'inprogress'));
        const list = by('taskList');
        list.querySelectorAll('.task-card').forEach(card => card.remove());
        const markup = tasks.map(task => {
            const latest = state.submissions.filter(sub => sub.taskId === task.id).slice(-1)[0];
            const action = task.status === 'revision' ? 'Resubmit' : task.status === 'inprogress' ? 'Continue task' : task.status === 'assigned' ? 'Start task' : 'View details';
            const actionName = task.status === 'revision' || task.status === 'inprogress' ? 'submit' : task.status === 'assigned' ? 'start' : 'details';
            return '<article class="task-card" data-status="' + escapeHtml(task.status) + '"><div class="task-info"><div class="task-head"><h3 class="task-title">' + escapeHtml(task.title) + '</h3><span class="status-badge">' + escapeHtml(statusLabel(task.status)) + '</span></div><p class="task-desc">' + escapeHtml(task.description) + '</p>' +
                (task.feedback ? '<div class="feedback-note"><strong>Supervisor feedback:</strong> ' + escapeHtml(task.feedback) + '</div>' : '') +
                '<div class="task-meta"><span><i class="bi bi-calendar-event" aria-hidden="true"></i> ' + escapeHtml(dateLabel(task.deadline)) + '</span><span>' + (task.score == null ? 'Not graded' : 'Score ' + escapeHtml(task.score) + ' / ' + escapeHtml(task.maximum)) + '</span>' + (latest ? '<span>Submission ' + escapeHtml(latest.revision) + '</span>' : '') + '</div></div><div class="task-action"><button type="button" class="btn btn-primary submit-btn" data-task-action="' + actionName + '" data-task-id="' + escapeHtml(task.id) + '">' + action + '</button></div></article>';
        }).join('');
        if (markup) by('noTasksMsg').insertAdjacentHTML('beforebegin', markup);
        const empty = by('noTasksMsg');
        if (empty) {
            empty.style.display = tasks.length ? 'none' : 'block';
            empty.querySelector('h4').textContent = state.tasks.length ? 'No tasks match these filters' : 'No tasks assigned yet';
            empty.querySelector('p').textContent = state.tasks.length ? 'Clear the search or choose All Statuses to see tasks.' : 'Assigned tasks will appear here once your supervisor adds them.';
        }
        document.querySelectorAll('.stat-details h3').forEach((el, i) => { el.textContent = [state.tasks.length, state.tasks.filter(t => t.status === 'inprogress').length, state.tasks.filter(t => t.status === 'submitted').length, state.tasks.filter(t => t.status === 'revision').length][i] ?? 0; });
    }
    function openSubmit(task, readOnly) {
        selectedId = task.id;
        const submissions = state.submissions.filter(sub => sub.taskId === task.id);
        const latest = submissions.slice(-1)[0];
        by('submitModal').querySelector('.modal-title').textContent = task.title + (task.score == null ? '' : ' · Institution evaluation');
        const evaluation = by('studentTaskEvaluation');
        const hasScore = task.score !== null && task.score !== undefined && task.score !== '';
        evaluation.hidden = !hasScore;
        if (hasScore) {
            by('studentTaskScore').textContent = task.score;
            by('studentTaskMaximum').textContent = task.maximum || 100;
            by('studentTaskFeedback').textContent = task.feedback || 'No additional supervisor feedback.';
        } else {
            by('studentTaskScore').textContent = '';
            by('studentTaskMaximum').textContent = '';
            by('studentTaskFeedback').textContent = '';
        }
        by('studentSubmissionHistory').innerHTML = submissions.map(sub => '<p>Revision ' + escapeHtml(sub.revision) + ' · ' + escapeHtml(sub.createdAt) + '<br>' + escapeHtml(sub.description) + (sub.link ? ' · <a href="' + escapeHtml(sub.link) + '" target="_blank" rel="noopener">Open work</a>' : '') + '</p>').join('') || '';
        by('workDesc').value = readOnly ? (latest?.description || task.description) : '';
        by('workLink').value = readOnly ? (latest?.link || '') : '';
        by('workDesc').readOnly = !!readOnly; by('workLink').readOnly = !!readOnly; by('workFile').disabled = !!readOnly;
        by('confirmSubmitBtn').hidden = !!readOnly; by('modalError').textContent = '';
        modal('submitModal')?.show();
    }
    async function refresh() {
        try { state = await request(''); render(); connection('Connected to Firebase /tasks. No-login task mode is active.'); return true; }
        catch (error) {
            message(error.message, true); connection('Could not load task records from Firebase.');
            const list = by('taskList'); list.querySelectorAll('.task-card').forEach(card => card.remove());
            const empty = by('noTasksMsg'); if (empty) { empty.style.display = 'block'; empty.querySelector('h4').textContent = 'Tasks unavailable'; empty.querySelector('p').textContent = 'Check the Firebase connection and Rules, then retry.'; }
            return false;
        }
    }
    document.addEventListener('click', async event => {
        const logout = event.target.closest('#btnLogout');
        if (logout) { event.preventDefault(); location.href = '../Default.aspx'; return; }
        const button = event.target.closest('[data-task-action]');
        if (!button || busy) return;
        event.preventDefault(); const task = state.tasks.find(item => item.id === button.dataset.taskId); if (!task) return;
        try {
            if (button.dataset.taskAction === 'details') { openSubmit(task, true); return; }
            if (button.dataset.taskAction === 'start') {
                busy = true; button.disabled = true; await request('start', { assignmentId: task.id }); await refresh(); message('Task started and saved to Firebase.', false); return;
            }
            openSubmit(task, false);
        } catch (error) { message(error.message, true); }
        finally { busy = false; }
    });
    by('confirmSubmitBtn')?.addEventListener('click', async () => {
        if (busy || !selectedId) return;
        const description = by('workDesc').value.trim(), link = by('workLink').value.trim();
        if (!description || !link) { by('modalError').textContent = 'Add a description and a work link before submitting.'; return; }
        busy = true; by('confirmSubmitBtn').disabled = true; by('modalError').textContent = '';
        try {
            await request('submit', { assignmentId: selectedId, description, link, fileName: by('workFile').files[0]?.name || '' });
            modal('submitModal')?.hide(); await refresh(); message('Submission saved to Firebase and sent for supervisor review.', false);
        } catch (error) { by('modalError').textContent = error.message; }
        finally { busy = false; by('confirmSubmitBtn').disabled = false; }
    });
    by('taskSearch')?.addEventListener('input', render); by('taskFilter')?.addEventListener('change', render);
    window.addEventListener('focus', refresh); document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    window.setInterval(() => { if (!document.hidden) refresh(); }, 30000);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    refresh();
})();
