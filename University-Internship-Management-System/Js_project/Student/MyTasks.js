(function () {
    'use strict';
    const api = '../TasksApi.ashx';
    let state = { tasks: [], submissions: [] }, boardState = null, selectedId = null, busy = false, viewMode = 'board';
    const by = id => document.getElementById(id);
    let boardLoadError = '', loadingTasks = true, serverOffset = 0, refreshPending = null, editing = false, allowClose = false;
    const now = () => Date.now() + serverOffset;
    const escapeHtml = value => String(value == null ? '' : value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const message = (value, error) => {
        const notice = by('pageNotice');
        if (notice) { notice.hidden = false; notice.textContent = value; notice.classList.toggle('text-danger', !!error); notice.classList.toggle('text-success', !error); }
        if (notice) { notice.dataset.tone = error ? 'error' : 'success'; notice.setAttribute('role', error ? 'alert' : 'status'); }
    };
    const connection = value => { const status = document.querySelector('.preview-note'); if (status) status.textContent = value; };
    const modal = id => window.bootstrap && bootstrap.Modal.getOrCreateInstance(by(id));
    function selectTaskTab(name) {
        document.querySelectorAll('[data-student-tab]').forEach(button => { const active = button.dataset.studentTab === name; button.setAttribute('aria-selected', String(active)); button.tabIndex = active ? 0 : -1; });
        document.querySelectorAll('[data-student-panel]').forEach(panel => { panel.hidden = panel.dataset.studentPanel !== name; });
        by('confirmSubmitBtn').hidden = name !== 'submission' || !editing || by('workDesc').readOnly;
        if (name === 'exam') window.StudentTaskExams?.showTask(selectedId);
    }
    document.querySelector('.student-task-tabs')?.addEventListener('click', event => { const button = event.target.closest('[data-student-tab]'); if (button) selectTaskTab(button.dataset.studentTab); });
    document.querySelector('.student-task-tabs')?.addEventListener('keydown', event => {
        const tabs = [...document.querySelectorAll('[data-student-tab]')], index = tabs.indexOf(document.activeElement);
        if (index < 0 || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault(); const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
        tabs[next].focus(); selectTaskTab(tabs[next].dataset.studentTab);
    });
    by('taskFilter')?.add(new Option('Archived', 'archived'));
    async function request(action, data, requestedMethod) {
        const method = requestedMethod || (action ? 'POST' : 'GET');
        let url = api + (action ? '?action=' + encodeURIComponent(action) : '');
        if (action === 'board' && method === 'GET') url += '&placementId=' + encodeURIComponent((data || {}).placementId || '');
        if (action === 'attachments' && method === 'GET') url += '&taskId=' + encodeURIComponent((data || {}).taskId || '');
        const response = await fetch(url, {
            signal: AbortSignal.timeout(65000),
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
    function statusLabel(status) { return ({ assigned: 'New', inprogress: 'In progress', submitted: 'Awaiting review', revision: 'Not complete', completed: 'Completed' })[status] || status || 'Unknown'; }
    function dateLabel(value) { const date = new Date(value); return isNaN(date.getTime()) ? (value || 'No deadline') : date.toLocaleString(); }
    function decorateTaskStates(host) {
        host.querySelectorAll('[data-task-id]').forEach(button => {
            const task = state.tasks.find(t => t.id === button.dataset.taskId), card = button.closest('.task-card,.task-board-card');
            if (!task || !card || card.querySelector('.task-overdue') || ['submitted', 'completed'].includes(task.status)) return;
            const deadline = Date.parse(task.deadline.length === 10 ? task.deadline + 'T23:59:59Z' : task.deadline);
            if (now() >= deadline) { const status = document.createElement('p'); status.className = 'task-overdue'; status.textContent = task.extension?.status === 'pending' ? 'Overdue · Extension pending' : task.extension?.status === 'rejected' ? 'Overdue · Extension rejected (final)' : 'Overdue · Request an extension to submit'; card.append(status); }
        });
    }
    function render() {
        const query = (by('taskSearch')?.value || '').trim().toLowerCase();
        const filter = by('taskFilter')?.value || '';
        const tasks = state.tasks.filter(task => (!task.archived || filter === 'archived') && (!query || (task.title + ' ' + task.description).toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter));
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
        decorateTaskStates(list);
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
            if (!placements.length) select.add(new Option(boardLoadError ? 'Placements unavailable' : 'No linked task placements', ''));
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
        host.setAttribute('aria-busy', 'false');
        if (!boardState || !boardState.columns.length) { TaskDialogs.renderBoardShell(host, { loading: loadingTasks, error: boardLoadError }); return; }
        const query = (by('taskSearch')?.value || '').trim().toLowerCase(), filter = by('taskFilter')?.value || '';
        const tasks = boardState.tasks.filter(task => (!query || (task.title + ' ' + task.description).toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter));
        host.innerHTML = '<div class="task-board-columns">' + boardState.columns.map(column => {
            const cards = tasks.filter(task => task.columnId === column.id).sort((a, b) => a.order - b.order || a.id.localeCompare(b.id));
            return '<section class="task-board-column" data-column-id="' + escapeHtml(column.id) + '"><header><h5>' + escapeHtml(column.title) + '</h5><span>' + cards.length + '</span></header><div class="task-board-dropzone">' + cards.map(task =>
                '<article class="task-board-card task-board-card-readonly"><div class="task-board-card-head"><strong>' + escapeHtml(task.title) + '</strong><span class="status-badge">' + escapeHtml(statusLabel(task.status)) + '</span></div><p>' + escapeHtml(task.description) + '</p><small>Due ' + escapeHtml(dateLabel(task.dueDate)) + '</small><button type="button" class="btn btn-light" data-task-action="details" data-task-id="' + escapeHtml(task.id) + '">View task</button></article>'
            ).join('') + (cards.length ? '' : '<div class="task-board-column-empty">No tasks in this column</div>') + '</div></section>';
        }).join('') + '</div>';
        decorateTaskStates(host);
    }
    async function refreshBoard() {
        const placementId = by('boardPlacementSelect')?.value;
        if (!placementId) { boardState = null; renderBoard(); return false; }
        try { boardState = await request('board', { placementId }, 'GET'); boardLoadError = ''; renderBoard(); return true; }
        catch (error) { boardState = null; boardLoadError = error.message; loadingTasks = false; renderBoard(); message(error.message, true); return false; }
    }
    function openSubmit(task, readOnly) {
        selectedId = task.id;
        editing = !readOnly; allowClose = false;
        let information = by('studentTaskInstructions');
        if (!information) { information = document.createElement('section'); information.id = 'studentTaskInstructions'; information.className = 'feedback-note'; by('submitModal').querySelector('.modal-body').prepend(information); }
        information.innerHTML = '<h6>Task instructions</h6><p style="white-space:pre-wrap">' + escapeHtml(task.description) + '</p><p style="white-space:pre-wrap">' + escapeHtml(task.evaluationCriteria || task.instructions || '') + '</p><p>Deadline: ' + escapeHtml(dateLabel(task.deadline)) + ' · Maximum grade: ' + escapeHtml(task.maximum) + '</p>';
        let action = by('studentTaskWorkAction');
        if (!action) { action = document.createElement('button'); action.id = 'studentTaskWorkAction'; action.type = 'button'; action.className = 'btn btn-primary'; by('submitModal').querySelector('.modal-footer').prepend(action); }
        action.hidden = !readOnly || task.archived || !['assigned', 'inprogress', 'revision'].includes(task.status) || task.score != null;
        action.textContent = task.status === 'assigned' ? 'Start task' : task.status === 'revision' ? 'Prepare resubmission' : 'Continue task';
        action.onclick = async () => {
            if (busy) return;
            if (task.status === 'assigned' && !await TaskDialogs.confirm('Start this task?', 'Mark this assignment as In progress and open your submission workspace?', 'Start task')) return;
            busy = true; action.disabled = true;
            try { if (task.status === 'assigned') { await request('start', { assignmentId: task.id }); await refresh(); } openSubmit(state.tasks.find(item => item.id === task.id) || task, false); }
            catch (error) { by('modalError').textContent = error.message; }
            finally { busy = false; action.disabled = false; }
        };
        let examLink = by('studentTaskExamLink');
        if (!examLink) { examLink = document.createElement('button'); examLink.type = 'button'; examLink.id = 'studentTaskExamLink'; examLink.className = 'btn btn-light'; by('submitModal').querySelector('.modal-footer').prepend(examLink); }
        examLink.textContent = 'View exam'; examLink.onclick = () => selectTaskTab('exam');
        const submissions = state.submissions.filter(sub => sub.taskId === task.id);
        const latest = submissions.slice(-1)[0];
        by('submitModal').querySelector('.modal-title').textContent = task.title + (task.score == null ? '' : ' · Company task grade');
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
        for (const [version, instruction] of Object.entries(task.instructionVersions || {}).filter(([, value]) => value)) { const details = document.createElement('details'), heading = document.createElement('summary'), text = document.createElement('p'); heading.textContent = 'Previous instructions · version ' + version; text.textContent = instruction.title + '\n' + instruction.description + '\n' + instruction.evaluationCriteria; details.append(heading, text); by('studentSubmissionHistory').append(details); }
        for (const notification of task.notifications || []) { const p = document.createElement('p'); p.textContent = notification.message; by('studentSubmissionHistory').append(p); }
        by('workDesc').value = readOnly ? (latest?.description || task.description) : '';
        by('workLink').value = readOnly ? (latest?.link || '') : '';
        if (!readOnly) by('workFile').value = '';
        by('workDesc').readOnly = !!readOnly; by('workLink').readOnly = !!readOnly; by('workFile').disabled = !!readOnly;
        by('confirmSubmitBtn').hidden = !!readOnly; by('modalError').textContent = '';
        let extensionButton = by('requestTaskExtension');
        if (!extensionButton) { extensionButton = document.createElement('button'); extensionButton.id = 'requestTaskExtension'; extensionButton.type = 'button'; extensionButton.className = 'btn btn-light'; by('submitModal').querySelector('.modal-footer').prepend(extensionButton); }
        const overdue = now() >= new Date(task.deadline.length === 10 ? task.deadline + 'T23:59:59Z' : task.deadline).getTime();
        if (overdue) action.hidden = true;
        if (overdue) editing = false;
        extensionButton.hidden = task.archived || !overdue || task.score != null || task.status === 'submitted' || task.status === 'completed';
        extensionButton.disabled = task.extension?.status === 'pending' || task.extension?.status === 'rejected';
        extensionButton.textContent = task.extension?.status === 'pending' ? 'Extension pending' : task.extension?.status === 'rejected' ? 'Extension rejected · final' : 'Request extension';
        if (overdue) { by('confirmSubmitBtn').hidden = true; by('modalError').textContent = 'Overdue. An approved extension is required before submitting.'; }
        extensionButton.onclick = async () => {
            const body = document.createElement('div'); body.innerHTML = '<label>Reason<textarea name="reason" required maxlength="3000"></textarea></label><label>Proposed new deadline<input name="deadline" type="datetime-local" required></label><p>Timezone: ' + escapeHtml(Intl.DateTimeFormat().resolvedOptions().timeZone) + '</p>';
            if (!await TaskDialogs.open('Request deadline extension', body, { confirm: 'Send request' })) return;
            try { await request('extension', { taskId: task.id, reason: body.querySelector('textarea').value, deadline: new Date(body.querySelector('input').value).toISOString() }); allowClose = true; modal('submitModal').hide(); await refresh(); message('Extension request sent to your company supervisor.', false); } catch (error) { by('modalError').textContent = error.message; }
        };
        selectTaskTab(readOnly ? 'details' : 'submission');
        modal('submitModal')?.show();
        loadAttachments(task.id);
    }
    let workspaceLoading;
    function refresh() { return workspaceLoading || (workspaceLoading = loadWorkspace().finally(() => { workspaceLoading = null; })); }
    async function loadWorkspace() {
        loadingTasks = true;
        try { const examRefresh = window.StudentTaskExams ? StudentTaskExams.refresh().catch(error => message(error.message, true)) : Promise.resolve(); state = await request(''); if (state.serverNow) serverOffset = Date.parse(state.serverNow) - Date.now(); boardLoadError = ''; loadingTasks = false; render(); connection('Connected to Firebase /tasks using your verified account.' + (state.identity ? ' Student ID: ' + state.identity : '')); await Promise.all([refreshBoard(), examRefresh]); return true; }
        catch (error) {
            state = { tasks: [], submissions: [] }; boardState = null; boardLoadError = error.message; loadingTasks = false; render();
            message(error.message, true); connection('Could not load task records from Firebase.');
            document.querySelectorAll('.stat-details h3').forEach(el => el.textContent = '—');
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
                if (!await TaskDialogs.confirm('Start this task?', 'Mark this assignment as In progress?', 'Start task')) return;
                busy = true; button.disabled = true; await request('start', { assignmentId: task.id }); await refresh(); openSubmit(state.tasks.find(item => item.id === task.id) || task, false); message('Task started and saved to Firebase.', false); return;
            }
            openSubmit(task, false);
        } catch (error) { message(error.message, true); }
        finally { busy = false; }
    });
    by('confirmSubmitBtn')?.addEventListener('click', async () => {
        if (busy || !selectedId) return;
        const description = by('workDesc').value.trim(), link = by('workLink').value.trim();
        by('workDesc').required = true;
        if (!TaskDialogs.validate(by('studentPanelSubmission'))) { by('modalError').textContent = 'Complete the highlighted submission fields.'; return; }
        if (!description) { by('modalError').textContent = 'Describe your completed work before submitting.'; by('workDesc').focus(); return; }
        if (link && !/^https?:\/\//i.test(link)) { by('modalError').textContent = 'Use a complete HTTP or HTTPS work link, or leave it empty.'; by('workLink').focus(); return; }
        if (!await TaskDialogs.confirm('Submit task for review?', 'Your company supervisor will review this submission. Further changes require a revision request.', 'Submit work')) return;
        if (busy) return;
        busy = true; by('confirmSubmitBtn').disabled = true; by('modalError').textContent = '';
        try {
            const file = by('workFile').files[0];
            if (file && file.size > 10 * 1024 * 1024) throw new Error('Files are limited to 10 MiB.');
            if (file) await upload('submit', { assignmentId: selectedId, description, link }, file);
            else await request('submit', { assignmentId: selectedId, description, link, fileName: '' });
            allowClose = true; modal('submitModal')?.hide(); await refresh(); message('Submission saved to Firebase and sent for supervisor review.', false);
        } catch (error) { by('modalError').textContent = error.message; }
        finally { busy = false; by('confirmSubmitBtn').disabled = false; }
    });
    by('boardModeButton')?.addEventListener('click', () => { viewMode = 'board'; by('boardModeButton').classList.add('btn-primary'); by('boardModeButton').classList.remove('btn-outline-primary'); by('listModeButton').classList.remove('btn-primary'); by('listModeButton').classList.add('btn-outline-primary'); by('boardModeButton').setAttribute('aria-pressed', 'true'); by('listModeButton').setAttribute('aria-pressed', 'false'); render(); });
    by('listModeButton')?.addEventListener('click', () => { viewMode = 'list'; by('listModeButton').classList.add('btn-primary'); by('listModeButton').classList.remove('btn-outline-primary'); by('boardModeButton').classList.remove('btn-primary'); by('boardModeButton').classList.add('btn-outline-primary'); by('boardModeButton').setAttribute('aria-pressed', 'false'); by('listModeButton').setAttribute('aria-pressed', 'true'); render(); });
    by('boardPlacementSelect')?.addEventListener('change', refreshBoard);
    by('taskSearch')?.addEventListener('input', render); by('taskFilter')?.addEventListener('change', render);
    by('taskFilter')?.addEventListener('change', () => { if (by('taskFilter').value === 'archived') by('listModeButton').click(); });
    function idleRefresh() { if (document.hidden || busy || by('submitModal').classList.contains('show')) return; if (!refreshPending) refreshPending = refresh().finally(() => { refreshPending = null; }); }
    by('submitModal').addEventListener('hide.bs.modal', event => {
        if (busy && !allowClose) { event.preventDefault(); return; }
        if (allowClose || !editing || !(by('workDesc').value.trim() || by('workLink').value.trim() || by('workFile').files.length)) return;
        event.preventDefault(); TaskDialogs.confirm('Discard unfinished submission?', 'Your unsent work will be discarded.', 'Discard').then(confirmed => { if (confirmed) { allowClose = true; modal('submitModal').hide(); } });
    });
    window.addEventListener('focus', idleRefresh); document.addEventListener('visibilitychange', idleRefresh);
    TaskDialogs.watch(refresh, () => !busy);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    by('taskBoard').addEventListener('task-board-retry', refresh);
    by('taskList').hidden = true; document.querySelectorAll('.stat-details h3').forEach(el => el.textContent = '—'); renderBoard(); refresh();
})();
