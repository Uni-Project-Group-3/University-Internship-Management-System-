(function () {
    'use strict';
    const api = '../TasksApi.ashx';
    let state = { tasks: [], submissions: [], students: [] }, boardState = null, selectedId = null, busy = false, boardBusy = false, viewMode = 'board', draggedTaskId = null;
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
    async function loadTaskFiles(taskId) {
        const host = by('companyTaskAttachments'); if (!host) return;
        host.textContent = 'Loading authorized files…';
        try {
            const result = await request('attachments', { taskId }, 'GET');
            host.innerHTML = result.files.length ? result.files.map(file => '<div><a href="../TasksApi.ashx?action=download&fileId=' + encodeURIComponent(file.id) + '">' + escapeHtml(file.fileName) + '</a> <span class="text-muted">(' + escapeHtml(Math.ceil(file.sizeBytes / 1024)) + ' KB · ' + escapeHtml(file.purpose === 'taskAttachment' ? 'Task resource' : 'Student submission') + ')</span></div>').join('') : '<span class="text-muted">No files attached yet.</span>';
        } catch (error) { host.textContent = error.message; }
    }
    function statusLabel(status) { return ({ assigned: 'Assigned', inprogress: 'In progress', submitted: 'Awaiting review', revision: 'Revision requested', completed: 'Completed' })[status] || status || 'Unknown'; }
    function dateLabel(value) { const date = new Date(value); return isNaN(date.getTime()) ? (value || 'Not set') : date.toLocaleDateString(); }
    function render() {
        const query = (by('taskSearch')?.value || '').trim().toLowerCase();
        const filter = by('statusFilter')?.value || '';
        const tasks = state.tasks.filter(task => (!query || (task.title + ' ' + task.description + ' ' + task.studentName).toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter || filter === 'assigned' && task.status === 'inprogress'));
        const list = by('taskList'); list.querySelectorAll('.task-card').forEach(card => card.remove());
        const markup = tasks.map(task => {
            const latest = state.submissions.filter(sub => sub.taskId === task.id).slice(-1)[0];
            return '<article class="task-card" data-status="' + escapeHtml(task.status) + '"><div class="task-info"><div class="task-head"><h3 class="task-title">' + escapeHtml(task.title) + '</h3><span class="status-badge">' + escapeHtml(statusLabel(task.status)) + '</span></div><p>' + escapeHtml(task.studentName) + '</p><p class="task-desc">' + escapeHtml(task.description) + '</p><div class="task-meta"><span>Deadline ' + escapeHtml(dateLabel(task.deadline)) + '</span><span>' + (latest ? 'Revision ' + escapeHtml(latest.revision) + ' submitted' : 'No submission yet') + '</span>' + (task.score == null ? '' : '<span>Score ' + escapeHtml(task.score) + ' / ' + escapeHtml(task.maximum) + '</span>') + '</div>' + (task.feedback ? '<p class="feedback-note">' + escapeHtml(task.feedback) + '</p>' : '') + '</div><div class="task-action"><button type="button" class="btn btn-primary review-btn" data-task-action="review" data-task-id="' + escapeHtml(task.id) + '">' + (task.status === 'submitted' ? 'Review submission' : 'View details') + '</button></div></article>';
        }).join('');
        if (markup) by('noTasksMsg').insertAdjacentHTML('beforebegin', markup);
        const empty = by('noTasksMsg');
        if (empty) {
            empty.style.display = tasks.length ? 'none' : 'block';
            empty.querySelector('h4').textContent = state.tasks.length ? 'No tasks match these filters' : 'No tasks assigned yet';
            empty.querySelector('p').textContent = state.tasks.length ? 'Clear the search or choose All Statuses to see tasks.' : 'New assignments from your supervisor will appear here.';
        }
        by('taskCount').textContent = tasks.length + ' tasks';
        document.querySelectorAll('.stat-details h3').forEach((el, i) => { el.textContent = [state.tasks.length, state.tasks.filter(t => t.status === 'submitted').length, state.tasks.filter(t => t.status === 'completed').length, state.tasks.filter(t => t.status === 'revision').length][i] ?? 0; });
        const select = by('newTaskStudent'); const previous = select.value;
        select.innerHTML = '<option value="">' + (state.students.length ? 'Choose trainee' : 'No active trainee placements loaded from Firebase') + '</option>' + state.students.map(student => '<option value="' + escapeHtml(student.placementId) + '">' + escapeHtml(student.name) + ' (' + escapeHtml(student.id) + ')</option>').join('');
        if (state.students.some(student => student.placementId === previous)) select.value = previous;
        else if (state.students.length === 1) select.value = state.students[0].placementId;
        const boardSelect = by('boardPlacementSelect');
        if (boardSelect) {
            const boardPrevious = boardSelect.value;
            boardSelect.innerHTML = state.students.map(student => '<option value="' + escapeHtml(student.placementId) + '">' + escapeHtml(student.name) + ' (' + escapeHtml(student.id) + ')</option>').join('');
            if (state.students.some(student => student.placementId === boardPrevious)) boardSelect.value = boardPrevious;
            else if (state.students.length) boardSelect.value = state.students[0].placementId;
        }
        const board = by('taskBoard'), listHost = by('taskList');
        if (board && listHost) { board.hidden = viewMode !== 'board'; listHost.hidden = viewMode === 'board'; }
        renderBoard();
    }
    function renderBoard() {
        const host = by('taskBoard'); if (!host) return;
        if (viewMode !== 'board') { host.hidden = true; return; }
        host.hidden = false;
        if (!boardState) { host.innerHTML = '<div class="task-board-empty">Choose an active trainee placement to load its board.</div>'; return; }
        const query = (by('taskSearch')?.value || '').trim().toLowerCase(), filter = by('statusFilter')?.value || '';
        const selected = state.students.find(student => student.placementId === boardState.placementId);
        const tasks = boardState.tasks.filter(task => {
            const text = (task.title + ' ' + task.description + ' ' + (selected?.name || '')).toLowerCase();
            return (!query || text.includes(query)) && (!filter || filter === 'all' || task.status === filter || filter === 'assigned' && task.status === 'inprogress');
        });
        host.innerHTML = '<div class="task-board-columns">' + boardState.columns.map(column => {
            const cards = tasks.filter(task => task.columnId === column.id).sort((a, b) => a.order - b.order || a.id.localeCompare(b.id));
            return '<section class="task-board-column" data-column-id="' + escapeHtml(column.id) + '"><header><h5>' + escapeHtml(column.title) + '</h5><span>' + cards.length + '</span></header><div class="task-board-dropzone">' + cards.map(task =>
                '<article class="task-board-card" draggable="' + (boardState.canMove ? 'true' : 'false') + '" data-board-task-id="' + escapeHtml(task.id) + '"><div class="task-board-card-head"><strong>' + escapeHtml(task.title) + '</strong><span class="status-badge">' + escapeHtml(statusLabel(task.status)) + '</span></div><p>' + escapeHtml(selected?.name || task.studentId || 'Trainee') + '</p><small>Due ' + escapeHtml(dateLabel(task.dueDate)) + '</small><button type="button" class="btn btn-light" data-task-action="review" data-task-id="' + escapeHtml(task.id) + '">' + (task.status === 'submitted' ? 'Review submission' : 'View details') + '</button></article>'
            ).join('') + (cards.length ? '' : '<div class="task-board-column-empty">Drop a task here</div>') + '</div></section>';
        }).join('') + '</div>';
        if (!boardState.canMove) host.insertAdjacentHTML('afterbegin', '<p class="task-board-help">This board is read-only for your role.</p>');
    }
    async function refreshBoard() {
        const placementId = by('boardPlacementSelect')?.value;
        if (!placementId) { boardState = null; renderBoard(); return false; }
        try { boardState = await request('board', { placementId }, 'GET'); renderBoard(); return true; }
        catch (error) { boardState = null; renderBoard(); message(error.message, true); return false; }
    }
    function operationId() {
        return window.crypto && typeof window.crypto.randomUUID === 'function' ? window.crypto.randomUUID() : 'move-' + Date.now() + '-' + Math.random().toString(36).slice(2);
    }
    function openReview(task) {
        selectedId = task.id;
        const submission = state.submissions.filter(sub => sub.taskId === task.id).slice(-1)[0];
        const details = document.querySelector('.submission-details-box');
        details.querySelector('p').textContent = submission ? submission.description : 'No student submission has been saved yet.';
        const link = details.querySelector('span');
        link.textContent = submission ? [submission.link, submission.fileName].filter(Boolean).join(' · ') || 'No link or attachment' : 'No submission';
        if (submission?.link) { const safeLink = document.createElement('a'); safeLink.href = submission.link; safeLink.target = '_blank'; safeLink.rel = 'noopener'; safeLink.textContent = submission.link; link.replaceChildren(safeLink); }
        by('reviewModal').querySelector('.modal-title').textContent = task.title + ' · ' + task.studentName;
        by('gradeInput').max = task.maximum; by('gradeInput').value = task.score == null ? '' : task.score;
        by('feedbackInput').value = task.feedback || '';
        const canReview = task.status === 'submitted';
        by('gradeInput').disabled = !canReview; by('feedbackInput').disabled = !canReview;
        by('requestRevisionBtn').disabled = !canReview; by('approveTaskBtn').disabled = !canReview;
        document.querySelector('label[for="gradeInput"]').textContent = 'Task score (maximum ' + task.maximum + ')';
        by('modalErrorContainer').textContent = canReview ? '' : 'The task can be graded after the student submits work.';
        by('companyTaskFile').value = '';
        by('companyTaskFile').disabled = false;
        by('uploadTaskFileBtn').disabled = false;
        loadTaskFiles(task.id);
        modal('reviewModal')?.show();
    }
    let refreshInFlight = false;
    async function refresh() {
        if (refreshInFlight || boardBusy || document.querySelector(".dragging")) return false;
        refreshInFlight = true;
        try { state = await request(''); render(); connection('Connected to Firebase /tasks using your verified account.'); await refreshBoard(); refreshInFlight = false; return true; }
        catch (error) { refreshInFlight = false;
            state = { tasks: [], submissions: [], students: [] };
            message(error.message, true); connection('Could not load task records from Firebase.');
            render();
            const list = by('taskList'); list.querySelectorAll('.task-card').forEach(card => card.remove());
            const empty = by('noTasksMsg'); if (empty) { empty.style.display = 'block'; empty.querySelector('h4').textContent = 'Tasks unavailable'; empty.querySelector('p').textContent = 'Check the Firebase connection and Rules, then retry.'; }
            by('taskCount').textContent = '— tasks'; return false;
        }
    }
    document.addEventListener('click', async event => {
        const logout = event.target.closest('#btnLogout');
        if (logout) { event.preventDefault(); location.href = '../Default.aspx'; return; }
        const button = event.target.closest('[data-task-action]');
        if (button) {
            event.preventDefault(); const task = state.tasks.find(item => item.id === button.dataset.taskId); if (task) openReview(task);
        }
    });
    by('publishTaskBtn')?.addEventListener('click', async () => {
        if (busy) return;
        const selectedPlacement = state.students.find(student => student.placementId === by('newTaskStudent').value);
        const payload = { title: by('newTaskTitle').value.trim(), description: by('newTaskDescription').value.trim(), deadline: by('newTaskDeadline').value, placementId: selectedPlacement?.placementId || '', maximum: Number(by('newTaskMaximum').value), criteria: by('newTaskCriteria').value.trim() };
        const missingTaskFields = !payload.title || !payload.description || !payload.deadline || !payload.criteria || !Number.isInteger(payload.maximum) || payload.maximum < 1 || payload.maximum > 1000;
        if (missingTaskFields) { by('createTaskError').hidden = false; by('createTaskError').textContent = 'Complete the title, description, deadline and evaluation criteria. The maximum score must be a whole number from 1 to 1000.'; return; }
        if (!selectedPlacement) {
            by('createTaskError').hidden = false;
            by('createTaskError').textContent = state.students.length
                ? 'Choose an active trainee placement from the Firebase list.'
                : 'No active trainee placement for this company was returned from Firebase. Check /placements, the supervisor assignment and Firebase Rules, then refresh.';
            return;
        }
        busy = true; by('publishTaskBtn').disabled = true; by('createTaskError').hidden = true;
        try { await request('create', payload); modal('createTaskModal')?.hide(); by('newTaskTitle').value = ''; by('newTaskDescription').value = ''; by('newTaskCriteria').value = ''; const refreshed = await refresh(); message(refreshed ? 'Task added and displayed below. The assigned student will see it on My Tasks.' : 'Task was saved to Firebase, but the list could not refresh. Reload to confirm it.', !refreshed); }
        catch (error) { by('createTaskError').hidden = false; by('createTaskError').textContent = error.message; }
        finally { busy = false; by('publishTaskBtn').disabled = false; }
    });
    async function review(reviewAction) {
        if (busy || !selectedId) return;
        const payload = { assignmentId: selectedId, reviewAction, score: Number(by('gradeInput').value), feedback: by('feedbackInput').value.trim() };
        if (reviewAction === 'grade' && by('gradeInput').value === '') { by('modalErrorContainer').textContent = 'Enter a grade before approving.'; return; }
        busy = true; by('requestRevisionBtn').disabled = true; by('approveTaskBtn').disabled = true; by('modalErrorContainer').textContent = '';
        try { await request('review', payload); modal('reviewModal')?.hide(); await refresh(); message(reviewAction === 'grade' ? 'Grade saved to Firebase.' : 'Revision request saved to Firebase.', false); }
        catch (error) { by('modalErrorContainer').textContent = error.message; }
        finally { busy = false; by('requestRevisionBtn').disabled = false; by('approveTaskBtn').disabled = false; }
    }
    by('requestRevisionBtn')?.addEventListener('click', () => review('revision'));
    by('approveTaskBtn')?.addEventListener('click', () => review('grade'));
    by('uploadTaskFileBtn')?.addEventListener('click', async () => {
        if (busy || !selectedId) return;
        const file = by('companyTaskFile').files[0];
        if (!file) { by('modalErrorContainer').textContent = 'Choose a task resource to upload.'; return; }
        if (file.size > 10 * 1024 * 1024) { by('modalErrorContainer').textContent = 'Files are limited to 10 MiB.'; return; }
        busy = true; by('uploadTaskFileBtn').disabled = true; by('modalErrorContainer').textContent = '';
        try {
            await upload('attach', { taskId: selectedId }, file);
            by('companyTaskFile').value = '';
            await loadTaskFiles(selectedId);
            await refresh();
            message('Task resource uploaded and linked in Firebase.', false);
        } catch (error) { by('modalErrorContainer').textContent = error.message; }
        finally { busy = false; by('uploadTaskFileBtn').disabled = false; }
    });
    by('boardModeButton')?.addEventListener('click', () => { viewMode = 'board'; by('boardModeButton').classList.add('btn-primary'); by('boardModeButton').classList.remove('btn-outline-primary'); by('listModeButton').classList.remove('btn-primary'); by('listModeButton').classList.add('btn-outline-primary'); by('boardModeButton').setAttribute('aria-pressed', 'true'); by('listModeButton').setAttribute('aria-pressed', 'false'); render(); });
    by('listModeButton')?.addEventListener('click', () => { viewMode = 'list'; by('listModeButton').classList.add('btn-primary'); by('listModeButton').classList.remove('btn-outline-primary'); by('boardModeButton').classList.remove('btn-primary'); by('boardModeButton').classList.add('btn-outline-primary'); by('boardModeButton').setAttribute('aria-pressed', 'false'); by('listModeButton').setAttribute('aria-pressed', 'true'); render(); });
    by('boardPlacementSelect')?.addEventListener('change', refreshBoard);
    by('taskBoard')?.addEventListener('dragstart', event => {
        const card = event.target.closest('[data-board-task-id]');
        if (!card || !boardState?.canMove) { event.preventDefault(); return; }
        draggedTaskId = card.dataset.boardTaskId; event.dataTransfer.effectAllowed = 'move'; event.dataTransfer.setData('text/plain', draggedTaskId);
    });
    by('taskBoard')?.addEventListener('dragover', event => { if (event.target.closest('.task-board-column')) { event.preventDefault(); event.dataTransfer.dropEffect = 'move'; } });
    by('taskBoard')?.addEventListener('drop', async event => {
        const column = event.target.closest('.task-board-column');
        if (!column || !draggedTaskId || !boardState?.canMove || boardBusy) return;
        event.preventDefault();
        const target = event.target.closest('[data-board-task-id]');
        if (target && target.dataset.boardTaskId === draggedTaskId) { draggedTaskId = null; return; }
        const payload = { placementId: boardState.placementId, taskId: draggedTaskId, columnId: column.dataset.columnId,
            beforeTaskId: target && target.dataset.boardTaskId !== draggedTaskId ? target.dataset.boardTaskId : null, operationId: operationId() };
        boardBusy = true;
        try {
            try { await request('move', payload); }
            catch (error) { if (error instanceof TypeError) await request('move', payload); else throw error; }
            await refreshBoard(); message('Task position saved to Firebase. Task workflow status was not changed.', false);
        } catch (error) { message(error.message, true); await refreshBoard(); }
        finally { boardBusy = false; draggedTaskId = null; }
    });
    by('taskSearch')?.addEventListener('input', render); by('statusFilter')?.addEventListener('change', render);
    by('createTaskModal')?.addEventListener('show.bs.modal', refresh);
    window.addEventListener('focus', refresh); document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    window.setInterval(() => { if (!document.hidden) refresh(); }, 5000);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    refresh();
})();
