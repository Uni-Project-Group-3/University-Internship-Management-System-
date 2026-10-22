(function () {
    'use strict';
    const api = '../TasksApi.ashx';
    let state = { tasks: [], submissions: [] }, boardState = null, selectedId = null, busy = false, viewMode = 'board';
    const by = id => document.getElementById(id);
    const escapeHtml = value => String(value == null ? '' : value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const message = (value, error) => {
        const notice = by('pageNotice');
        if (notice) { notice.hidden = false; notice.textContent = value; notice.classList.toggle('text-danger', !!error); notice.classList.toggle('text-success', !error); }
    };
    const connection = value => { const status = document.querySelector('.preview-note'); if (status) status.textContent = value; };
    const modal = id => window.bootstrap && bootstrap.Modal.getOrCreateInstance(by(id));
    async function request(action, data, requestedMethod) {
        const method = requestedMethod || (action ? 'POST' : 'GET');
        let url = api + (action ? '?action=' + encodeURIComponent(action) : '');
        if (action === 'board' && method === 'GET') url += '&placementId=' + encodeURIComponent((data || {}).placementId || '');
        if (action === 'attachments' && method === 'GET') url += '&taskId=' + encodeURIComponent((data || {}).taskId || '');
        const response = await fetch(url, {
            method, cache: 'no-store', credentials: 'same-origin', headers: method === 'POST' ? { 'Content-Type': 'application/json' } : {},
            body: method === 'POST' ? JSON.stringify(data || {}) : undefined
        });
        let result;
        try { result = await response.json(); } catch (_) { throw new Error('Firebase returned an unreadable response.'); }
        if (!response.ok) throw new Error(result.message || ('Firebase request failed (' + response.status + ').'));
        return result;
    }
    async function upload(action, fields, file) {
        const form = new FormData();
        Object.keys(fields).forEach(key => form.append(key, fields[key] == null ? '' : String(fields[key])));
        form.append('file', file, file.name);
        const response = await fetch(api + '?action=' + encodeURIComponent(action), { method: 'POST', cache: 'no-store', credentials: 'same-origin', body: form });
        let result; try { result = await response.json(); } catch (_) { throw new Error('The task service returned an unreadable response.'); }
        if (!response.ok) throw new Error(result.message || ('Task upload failed (' + response.status + ').'));
        return result;
    }
    async function loadAttachments(taskId) {
        const host = by('studentTaskAttachments'); if (!host) return;
        host.textContent = 'Loading authorized task files…';
        try {
            const result = await request('attachments', { taskId }, 'GET');
            host.innerHTML = result.files.length ? result.files.map(file => '<div><a href="../TasksApi.ashx?action=download&fileId=' + encodeURIComponent(file.id) + '">' + escapeHtml(file.fileName) + '</a> <span class="text-muted">(' + escapeHtml(Math.ceil(file.sizeBytes / 1024)) + ' KB · ' + escapeHtml(file.purpose === 'taskAttachment' ? 'Task resource' : 'Submission') + ')</span></div>').join('') : '<span class="text-muted">No files attached to this task.</span>';
        } catch (error) { host.textContent = error.message; }
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
        const select = by('boardPlacementSelect');
        if (select) {
            const previous = select.value;
            const placements = Array.from(new Map(state.tasks.filter(task => task.placementId).map(task => [task.placementId, task])).entries());
            select.innerHTML = placements.map(([id]) => '<option value="' + escapeHtml(id) + '">' + escapeHtml(id) + '</option>').join('');
            if (placements.some(([id]) => id === previous)) select.value = previous;
            else if (placements.length) select.value = placements[0][0];
        }
        const board = by('taskBoard'), listHost = by('taskList');
        if (board && listHost) { board.hidden = viewMode !== 'board'; listHost.hidden = viewMode === 'board'; }
        renderBoard();
    }
    function renderBoard() {
        const host = by('taskBoard'); if (!host) return;
        if (viewMode !== 'board') { host.hidden = true; return; }
        host.hidden = false;
        if (!boardState) { host.innerHTML = '<div class="task-board-empty">Your placement task board will appear here when linked tasks are available.</div>'; return; }
        const query = (by('taskSearch')?.value || '').trim().toLowerCase(), filter = by('taskFilter')?.value || '';
        const tasks = boardState.tasks.filter(task => (!query || (task.title + ' ' + task.description).toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter || filter === 'assigned' && task.status === 'inprogress'));
        host.innerHTML = '<div class="task-board-columns">' + boardState.columns.map(column => {
            const cards = tasks.filter(task => task.columnId === column.id).sort((a, b) => a.order - b.order || a.id.localeCompare(b.id));
            return '<section class="task-board-column" data-column-id="' + escapeHtml(column.id) + '"><header><h5>' + escapeHtml(column.title) + '</h5><span>' + cards.length + '</span></header><div class="task-board-dropzone">' + cards.map(task =>
                '<article class="task-board-card task-board-card-readonly"><div class="task-board-card-head"><strong>' + escapeHtml(task.title) + '</strong><span class="status-badge">' + escapeHtml(statusLabel(task.status)) + '</span></div><p>' + escapeHtml(task.description) + '</p><small>Due ' + escapeHtml(dateLabel(task.dueDate)) + '</small><button type="button" class="btn btn-light" data-task-action="details" data-task-id="' + escapeHtml(task.id) + '">View task</button></article>'
            ).join('') + (cards.length ? '' : '<div class="task-board-column-empty">No tasks in this column</div>') + '</div></section>';
        }).join('') + '</div>';
    }
    async function refreshBoard() {
        const placementId = by('boardPlacementSelect')?.value;
        if (!placementId) { boardState = null; renderBoard(); return false; }
        try { boardState = await request('board', { placementId }, 'GET'); renderBoard(); return true; }
        catch (error) { boardState = null; renderBoard(); message(error.message, true); return false; }
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
        by('studentTaskAttachments').textContent = 'Loading authorized task files…';
        by('workDesc').value = readOnly ? (latest?.description || task.description) : '';
        by('workLink').value = readOnly ? (latest?.link || '') : '';
        if (!readOnly) by('workFile').value = '';
        by('workDesc').readOnly = !!readOnly; by('workLink').readOnly = !!readOnly; by('workFile').disabled = !!readOnly;
        by('confirmSubmitBtn').hidden = !!readOnly; by('modalError').textContent = '';
        modal('submitModal')?.show();
        loadAttachments(task.id);
    }
    async function refresh() {
        try { state = await request(''); render(); connection('Connected to Firebase /tasks using your verified account.'); await refreshBoard(); return true; }
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
            const file = by('workFile').files[0];
            if (file && file.size > 10 * 1024 * 1024) throw new Error('Files are limited to 10 MiB.');
            if (file) await upload('submit', { assignmentId: selectedId, description, link }, file);
            else await request('submit', { assignmentId: selectedId, description, link, fileName: '' });
            modal('submitModal')?.hide(); await refresh(); message('Submission saved to Firebase and sent for supervisor review.', false);
        } catch (error) { by('modalError').textContent = error.message; }
        finally { busy = false; by('confirmSubmitBtn').disabled = false; }
    });
    by('boardModeButton')?.addEventListener('click', () => { viewMode = 'board'; by('boardModeButton').classList.add('btn-primary'); by('boardModeButton').classList.remove('btn-outline-primary'); by('listModeButton').classList.remove('btn-primary'); by('listModeButton').classList.add('btn-outline-primary'); by('boardModeButton').setAttribute('aria-pressed', 'true'); by('listModeButton').setAttribute('aria-pressed', 'false'); render(); });
    by('listModeButton')?.addEventListener('click', () => { viewMode = 'list'; by('listModeButton').classList.add('btn-primary'); by('listModeButton').classList.remove('btn-outline-primary'); by('boardModeButton').classList.remove('btn-primary'); by('boardModeButton').classList.add('btn-outline-primary'); by('boardModeButton').setAttribute('aria-pressed', 'false'); by('listModeButton').setAttribute('aria-pressed', 'true'); render(); });
    by('boardPlacementSelect')?.addEventListener('change', refreshBoard);
    by('taskSearch')?.addEventListener('input', render); by('taskFilter')?.addEventListener('change', render);
    window.addEventListener('focus', refresh); document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    window.setInterval(() => { if (!document.hidden) refresh(); }, 30000);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    refresh();
})();
