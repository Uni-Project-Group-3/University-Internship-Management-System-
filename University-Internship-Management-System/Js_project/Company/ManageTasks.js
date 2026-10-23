(function () {
    'use strict';
    const api = '../TasksApi.ashx';
    let state = { tasks: [], submissions: [], students: [] }, boardState = null, selectedId = null, busy = false, boardBusy = false, viewMode = 'board', draggedTaskId = null, examStates = new Map();
    const by = id => document.getElementById(id);
    let boardLoadError = '', loadingTasks = true, serverOffset = 0;
    let reviewBaseline = '', allowReviewClose = false;
    const escapeHtml = value => String(value == null ? '' : value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const message = (value, error) => {
        const notice = by('pageNotice');
        if (notice) { notice.hidden = false; notice.textContent = value; notice.classList.toggle('text-danger', !!error); notice.classList.toggle('text-success', !error); }
        if (notice) { notice.dataset.tone = error ? 'error' : 'success'; notice.setAttribute('role', error ? 'alert' : 'status'); }
    };
    const connection = value => { const status = document.querySelector('.preview-note'); if (status) status.textContent = value; };
    const modal = id => window.bootstrap && bootstrap.Modal.getOrCreateInstance(by(id));
    by('statusFilter').add(new Option('Archived', 'archived'));
    let selectedPlacements = new Set(), creationBatch = null, createDirty = false, allowCreateClose = false, creationComplete = false, editingTask = false;
    const completedResourceUploads = new Map();
    function renderStudents() {
        const query = by('studentSearch').value.trim().toLowerCase();
        by('studentOptions').replaceChildren(); by('studentChips').replaceChildren();
        for (const student of state.students) {
            if (selectedPlacements.has(student.placementId)) {
                const chip = document.createElement('button'); chip.type = 'button'; chip.textContent = student.name + ' ×'; chip.setAttribute('aria-label', 'Remove ' + student.name);
                chip.onclick = () => { selectedPlacements.delete(student.placementId); createDirty = true; renderStudents(); }; by('studentChips').append(chip);
            }
            if (query && !student.name.toLowerCase().includes(query)) continue;
            const label = document.createElement('label'), input = document.createElement('input'); input.type = 'checkbox'; input.checked = selectedPlacements.has(student.placementId);
            input.onchange = () => { input.checked ? selectedPlacements.add(student.placementId) : selectedPlacements.delete(student.placementId); createDirty = true; renderStudents(); };
            label.append(input, document.createTextNode(student.name)); by('studentOptions').append(label);
        }
        if (!by('studentOptions').children.length) by('studentOptions').textContent = state.students.length ? 'No matching students.' : 'No eligible placements are available.';
        by('selectedStudentCount').textContent = selectedPlacements.size + ' selected';
    }
    by('studentSearch').addEventListener('input', renderStudents);
    by('selectAllStudents').onclick = () => { selectedPlacements = new Set(state.students.map(s => s.placementId)); createDirty = true; renderStudents(); };
    by('clearStudents').onclick = () => { selectedPlacements.clear(); createDirty = true; renderStudents(); };
    by('createTaskModal').addEventListener('input', () => { createDirty = true; });
    by('createTaskModal').addEventListener('hidden.bs.modal', () => {
        if (!creationComplete) return;
        creationComplete = false; creationBatch = null; createDirty = false; selectedPlacements.clear(); completedResourceUploads.clear();
        ['newTaskTitle', 'newTaskDescription', 'newTaskCriteria', 'newTaskDeadline', 'createTaskResources', 'studentSearch'].forEach(id => by(id).value = '');
        by('configureTaskExam').checked = false; by('taskExamBuilder').replaceChildren(); by('taskExamBuilder').hidden = true; by('publishTaskBtn').disabled = false; by('publishTaskBtn').textContent = 'Create task'; window.TaskComposer?.reset(); renderStudents();
    });
    by('createTaskModal').addEventListener('hide.bs.modal', async event => {
        if (allowCreateClose || !createDirty) { allowCreateClose = false; return; }
        event.preventDefault();
        if (busy) return;
        if (await TaskDialogs.confirm('Discard unsaved changes?', 'Your unsaved task fields will be cleared. Saved assignments and exams remain available.', 'Discard changes')) {
            createDirty = false; creationBatch = null; selectedPlacements.clear(); completedResourceUploads.clear();
            ['newTaskTitle', 'newTaskDescription', 'newTaskCriteria', 'newTaskDeadline', 'createTaskResources'].forEach(id => by(id).value = '');
            by('taskExamBuilder').replaceChildren(); by('taskExamBuilder').hidden = true; window.TaskComposer?.reset(); renderStudents(); allowCreateClose = true; modal('createTaskModal').hide();
        }
    });
    document.querySelectorAll('.task-timezone').forEach(el => el.textContent = 'Timezone: ' + Intl.DateTimeFormat().resolvedOptions().timeZone);
    async function request(action, data, requestedMethod) {
        const method = requestedMethod || (action ? 'POST' : 'GET');
        let url = api + (action ? '?action=' + encodeURIComponent(action) : '');
        if (action === 'board' && method === 'GET') url += '&placementId=' + encodeURIComponent((data || {}).placementId || '');
        if (action === 'attachments' && method === 'GET') url += '&taskId=' + encodeURIComponent((data || {}).taskId || '');
        const response = await fetch(url, {
            method, cache: 'no-store', credentials: 'same-origin', signal: AbortSignal.timeout(65000), headers: method === 'POST' ? { 'Content-Type': 'application/json' } : {},
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
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest(); xhr.open('POST', api + '?action=' + encodeURIComponent(action));
            xhr.upload.onprogress = event => { if (by('createTaskModal').classList.contains('show')) by('publishTaskBtn').textContent = event.lengthComputable ? 'Uploading ' + Math.round(event.loaded * 100 / event.total) + '%' : 'Uploading…'; };
            xhr.onerror = () => reject(new Error('File upload interrupted. Your saved tasks remain available. Retry when connected.'));
            xhr.onload = () => { try { const result = JSON.parse(xhr.responseText); xhr.status >= 200 && xhr.status < 300 ? resolve(result) : reject(new Error(result.message || ('Task upload failed (' + xhr.status + ').'))); } catch { reject(new Error('The task service returned an unreadable response.')); } };
            xhr.send(form);
        });
    }
    async function loadTaskFiles(taskId) {
        const host = by('companyTaskAttachments'); if (!host) return;
        host.textContent = 'Loading authorized files…';
        try {
            const result = await request('attachments', { taskId }, 'GET');
            host.innerHTML = result.files.length ? result.files.map(file => '<div><a href="../TasksApi.ashx?action=download&fileId=' + encodeURIComponent(file.id) + '">' + escapeHtml(file.fileName) + '</a> <span class="text-muted">(' + escapeHtml(Math.ceil(file.sizeBytes / 1024)) + ' KB · ' + escapeHtml(file.purpose === 'taskAttachment' ? 'Task resource' : 'Student submission') + ')</span></div>').join('') : '<span class="text-muted">No files attached yet.</span>';
        } catch (error) { host.textContent = error.message; }
    }
    function statusLabel(status) { return ({ assigned: 'New', inprogress: 'In progress', submitted: 'Awaiting review', revision: 'Not complete', completed: 'Completed' })[status] || status || 'Unknown'; }
    function dateLabel(value) { const date = new Date(value); return isNaN(date.getTime()) ? (value || 'Not set') : date.toLocaleString(); }
    function additionalFilters(task) {
        const batch = by('batchFilter').value, deadline = by('deadlineFilter').value, original = state.tasks.find(t => t.id === task.id) || task, due = Date.parse(original.deadline || task.dueDate), now = Date.now() + serverOffset;
        const student = by('boardPlacementSelect').value;
        return (!student || student === 'all' || original.placementId === student) && (!original.archived || by('statusFilter').value === 'archived') && (batch === 'all' || original.batchId === batch) && (deadline === 'all' || deadline === 'overdue' && due < now && !['submitted', 'completed'].includes(task.status) || deadline === 'week' && due >= now && due <= now + 7 * 86400000);
    }
    function render() {
        const query = (by('taskSearch')?.value || '').trim().toLowerCase();
        const filter = by('statusFilter')?.value || '';
        const tasks = state.tasks.filter(task => additionalFilters(task) && (!query || (task.title + ' ' + task.description + ' ' + task.studentName).toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter));
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
        document.querySelectorAll('.stat-details h3').forEach((el, i) => { el.textContent = [state.tasks.length, state.tasks.filter(t => t.status === 'submitted').length, state.tasks.filter(t => t.status === 'revision').length, state.tasks.filter(t => t.status === 'completed').length][i] ?? 0; });
        const select = by('newTaskStudent'); const previous = select.value;
        select.innerHTML = '<option value="">' + (state.students.length ? 'Choose trainee' : 'No active trainee placements loaded from Firebase') + '</option>' + state.students.map(student => '<option value="' + escapeHtml(student.placementId) + '">' + escapeHtml(student.name) + ' (' + escapeHtml(student.id) + ')</option>').join('');
        if (state.students.some(student => student.placementId === previous)) select.value = previous;
        else if (state.students.length === 1) select.value = state.students[0].placementId;
        renderStudents();
        const batches = by('batchFilter'), currentBatch = batches.value;
        batches.replaceChildren(new Option('All assignment batches', 'all'));
        [...new Set(state.tasks.map(t => t.batchId).filter(Boolean))].forEach((batch, i) => batches.add(new Option('Assignment batch ' + (i + 1) + ' · ' + batch.slice(0, 8), batch)));
        if ([...batches.options].some(o => o.value === currentBatch)) batches.value = currentBatch;
        const boardSelect = by('boardPlacementSelect');
        if (boardSelect) {
            const boardPrevious = boardSelect.value;
            boardSelect.innerHTML = '<option value="all">All eligible students</option>' + state.students.map(student => '<option value="' + escapeHtml(student.placementId) + '">' + escapeHtml(student.name) + ' (' + escapeHtml(student.id) + ')</option>').join('');
            if (boardPrevious === 'all' || state.students.some(student => student.placementId === boardPrevious)) boardSelect.value = boardPrevious;
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
        const query = (by('taskSearch')?.value || '').trim().toLowerCase(), filter = by('statusFilter')?.value || '';
        const selected = state.students.find(student => student.placementId === boardState.placementId);
        const tasks = boardState.tasks.filter(task => {
            const text = (task.title + ' ' + task.description + ' ' + (selected?.name || '')).toLowerCase();
            return additionalFilters(task) && (!query || text.includes(query) || (state.tasks.find(t => t.id === task.id)?.studentName || '').toLowerCase().includes(query)) && (!filter || filter === 'all' || task.status === filter);
        });
        host.innerHTML = '<div class="task-board-columns">' + boardState.columns.map(column => {
            const cards = tasks.filter(task => task.columnId === column.id).sort((a, b) => a.order - b.order || a.id.localeCompare(b.id));
            return '<section class="task-board-column" data-column-id="' + escapeHtml(column.id) + '"><header><h5>' + escapeHtml(column.title) + '</h5><span>' + cards.length + '</span></header><div class="task-board-dropzone">' + cards.map(task =>
                '<article class="task-board-card" draggable="' + (boardState.canMove ? 'true' : 'false') + '" data-board-task-id="' + escapeHtml(task.id) + '"><div class="task-board-card-head"><strong>' + escapeHtml(task.title) + '</strong><span class="status-badge">' + escapeHtml(statusLabel(task.status)) + '</span></div><p><span class="task-avatar">' + escapeHtml((state.tasks.find(t => t.id === task.id)?.studentName || task.studentId || 'T').split(' ').map(n => n[0]).slice(0, 2).join('')) + '</span>' + escapeHtml(state.tasks.find(t => t.id === task.id)?.studentName || selected?.name || task.studentId || 'Trainee') + '</p><small class="' + (Date.parse(task.dueDate) < Date.now() && !['submitted', 'completed'].includes(task.status) ? 'task-overdue' : '') + '">Due ' + escapeHtml(dateLabel(task.dueDate)) + '</small><p class="small">' + (task.status === 'submitted' ? 'Submission ready for review' : task.status === 'completed' ? 'Graded' : 'Awaiting submission') + (state.tasks.find(t => t.id === task.id)?.attachmentIds?.length ? ' · 📎 Resources' : '') + '</p><button type="button" class="btn btn-light" data-task-action="review" data-task-id="' + escapeHtml(task.id) + '">' + (task.status === 'submitted' ? 'Review submission' : 'View details') + '</button></article>'
            ).join('') + (cards.length ? '' : '<div class="task-board-column-empty">Drop a task here</div>') + '</div></section>';
        }).join('') + '</div>';
        if (!boardState.canMove) host.insertAdjacentHTML('afterbegin', '<p class="task-board-help">This board is read-only for your role.</p>');
        host.querySelectorAll('[data-board-task-id]').forEach(card => { const exam = examStates.get(card.dataset.boardTaskId); if (exam) { const badge = document.createElement('small'); badge.textContent = 'Exam · ' + (exam.reviewState || exam.status); card.append(badge); } const task = state.tasks.find(t => t.id === card.dataset.boardTaskId); if (task?.extension?.status === 'pending') { const badge = document.createElement('p'); badge.className = 'task-overdue'; badge.textContent = 'Extension request awaiting decision'; card.append(badge); } });
        if (boardState.canMove) host.querySelectorAll('[data-board-task-id]').forEach(card => {
            const task = state.tasks.find(item => item.id === card.dataset.boardTaskId); if (!task) return;
            const controls = document.createElement('div'); controls.className = 'card-move-controls'; const select = document.createElement('select'); select.setAttribute('aria-label', 'Destination column for ' + task.title);
            boardState.columns.forEach(column => select.add(new Option(column.title, column.id))); select.value = card.closest('[data-column-id]').dataset.columnId;
            const move = document.createElement('button'); move.type = 'button'; move.className = 'btn btn-light btn-sm'; move.textContent = 'Move';
            move.onclick = async () => { if (boardBusy || select.value === card.closest('[data-column-id]').dataset.columnId) return; if (!await TaskDialogs.confirm('Move this card?', 'Move ' + task.title + ' to ' + select.selectedOptions[0].text + '? Workflow status still follows the task actions.', 'Move card')) return; boardBusy = true; move.disabled = true; try { await request('move', { taskId: task.id, placementId: task.placementId, columnId: select.value, operationId: operationId() }); await refreshBoard(); } catch (error) { message(error.message, true); } finally { boardBusy = false; move.disabled = false; } };
            controls.append(select, move); card.append(controls);
        });
        if (boardState.canMove && boardState.placementId !== 'all') host.querySelectorAll('.task-board-column header').forEach((header, index) => {
            for (const [label, delta] of [['←', -1], ['→', 1]]) {
                const button = document.createElement('button'); button.className = 'btn btn-light btn-sm'; button.type = 'button'; button.textContent = label; button.setAttribute('aria-label', 'Move ' + boardState.columns[index].title + (delta < 0 ? ' left' : ' right')); button.disabled = index + delta < 0 || index + delta >= boardState.columns.length;
                button.onclick = async () => { if (boardBusy) return; boardBusy = true; const ids = boardState.columns.map(c => c.id); [ids[index], ids[index + delta]] = [ids[index + delta], ids[index]]; try { await request('columns', { placementId: boardState.placementId, columnIds: ids, operationId: operationId() }); await refreshBoard(); } catch (error) { message(error.message, true); } finally { boardBusy = false; } }; header.append(button);
            }
        });
    }
    async function refreshBoard() {
        const placementId = by('boardPlacementSelect')?.value;
        if (!placementId) { boardState = null; renderBoard(); return false; }
        try {
            if (placementId === 'all') {
                const boards = await Promise.all(state.students.map(student => request('board', { placementId: student.placementId }, 'GET')));
                boardState = { placementId: 'all', canMove: boards.every(b => b.canMove), columns: [...new Map(boards.flatMap(b => b.columns).map(c => [c.id, c])).values()], tasks: boards.flatMap(b => b.tasks.map(t => ({ ...t, placementId: b.placementId }))) };
            } else boardState = await request('board', { placementId }, 'GET');
            boardLoadError = ''; renderBoard(); return true;
        }
        catch (error) { boardState = null; boardLoadError = error.message; loadingTasks = false; renderBoard(); message(error.message, true); return false; }
    }
    function operationId() {
        return window.crypto && typeof window.crypto.randomUUID === 'function' ? window.crypto.randomUUID() : 'move-' + Date.now() + '-' + Math.random().toString(36).slice(2);
    }
    function openReview(task) {
        selectedId = task.id;
        let fullDetails = by('taskFullDetails'); if (!fullDetails) { fullDetails = document.createElement('section'); fullDetails.id = 'taskFullDetails'; by('reviewModal').querySelector('.modal-body').prepend(fullDetails); }
        fullDetails.replaceChildren();
        const historyHost = by('companyTaskHistory'); historyHost.replaceChildren();
        for (const text of [task.description, 'Instructions: ' + (task.criteria || 'No additional instructions.'), 'Deadline: ' + dateLabel(task.deadline), 'Instruction version: ' + (task.instructionVersion || 1), ...state.submissions.filter(s => s.taskId === task.id).map(s => 'Submission ' + s.revision + ' · ' + dateLabel(s.createdAt) + '\n' + s.description), ...(task.notifications || []).map(n => n.message)]) { const p = document.createElement('p'); p.textContent = text; fullDetails.append(p); }
        for (const [version, instructions] of Object.entries(task.instructionVersions || {}).filter(([, value]) => value)) { const details = document.createElement('details'), summary = document.createElement('summary'), text = document.createElement('p'); summary.textContent = 'Previous instructions · version ' + version; text.textContent = instructions.title + '\n' + instructions.description + '\n' + instructions.evaluationCriteria + '\nDeadline: ' + dateLabel(instructions.dueDate); text.style.whiteSpace = 'pre-line'; details.append(summary, text); fullDetails.append(details); }
        [...fullDetails.children].slice(4).forEach(node => historyHost.append(node));
        if (!historyHost.children.length) historyHost.textContent = 'No previous submissions or instruction changes.';
        if (boardState?.canMove && task.placementId) {
            const label = document.createElement('label'), select = document.createElement('select'), move = document.createElement('button'); label.textContent = 'Move card to column'; label.className = 'form-label'; select.className = 'form-select'; boardState.columns.forEach(column => select.add(new Option(column.title, column.id))); move.type = 'button'; move.className = 'btn btn-light mt-2'; move.textContent = 'Move card';
            move.onclick = async () => { if (boardBusy || !await TaskDialogs.confirm('Move task card?', 'Save this card in ' + select.selectedOptions[0].text + '?', 'Move card')) return; boardBusy = true; move.disabled = true; try { await request('move', { placementId: task.placementId, taskId: task.id, columnId: select.value, operationId: operationId() }); await refreshBoard(); by('modalErrorContainer').textContent = 'Card position saved.'; } catch (error) { by('modalErrorContainer').textContent = error.message; } finally { boardBusy = false; move.disabled = false; } }; label.append(select); fullDetails.append(label, move);
        }
        const submission = state.submissions.filter(sub => sub.taskId === task.id).slice(-1)[0];
        const details = document.querySelector('.submission-details-box');
        details.querySelector('p').textContent = submission ? submission.description : 'No student submission has been saved yet.';
        const link = details.querySelector('span');
        link.textContent = submission ? [submission.link, submission.fileName].filter(Boolean).join(' · ') || 'No link or attachment' : 'No submission';
        if (submission?.link) { const safeLink = document.createElement('a'); safeLink.href = submission.link; safeLink.target = '_blank'; safeLink.rel = 'noopener'; safeLink.textContent = submission.link; link.replaceChildren(safeLink); }
        by('reviewModal').querySelector('.modal-title').textContent = task.title + ' · ' + task.studentName;
        by('gradeInput').max = task.maximum; by('gradeInput').value = task.score == null ? '' : task.score;
        by('feedbackInput').value = task.feedback || '';
        reviewBaseline = JSON.stringify([by('gradeInput').value, by('feedbackInput').value]); allowReviewClose = false;
        const canReview = task.status === 'submitted' && !!submission;
        by('gradeInput').disabled = !canReview; by('feedbackInput').disabled = !canReview;
        by('requestRevisionBtn').disabled = !canReview; by('approveTaskBtn').disabled = !canReview;
        document.querySelector('label[for="gradeInput"]').textContent = 'Task score (maximum ' + task.maximum + ')';
        by('modalErrorContainer').textContent = canReview ? '' : 'The task can be graded after the student submits work.';
        by('companyTaskFile').value = '';
        by('companyTaskFile').disabled = task.score != null || task.archived;
        by('uploadTaskFileBtn').disabled = task.score != null || task.archived;
        editButton.disabled = task.score != null || task.archived; archiveButton.disabled = !!task.archived; extensionButton.disabled = !!task.archived || task.extension?.status !== 'pending';
        loadTaskFiles(task.id);
        renderTaskExams(task.id);
        selectReviewTab('details');
        modal('reviewModal')?.show();
    }
    function selectReviewTab(name) {
        document.querySelectorAll('[data-company-tab]').forEach(button => { button.setAttribute('aria-selected', String(button.dataset.companyTab === name)); button.tabIndex = button.dataset.companyTab === name ? 0 : -1; });
        document.querySelectorAll('[data-company-panel]').forEach(panel => { panel.hidden = panel.dataset.companyPanel !== name; });
        by('approveTaskBtn').hidden = name !== 'grading'; by('requestRevisionBtn').hidden = name !== 'grading';
    }
    document.querySelector('.company-task-tabs').addEventListener('click', event => { const tab = event.target.closest('[data-company-tab]'); if (tab) selectReviewTab(tab.dataset.companyTab); });
    document.querySelectorAll('[data-company-tab]').forEach(button => { const name = button.dataset.companyTab, panel = document.querySelector('[data-company-panel="' + name + '"]'); button.id = 'company-tab-' + name; button.setAttribute('aria-controls', 'company-panel-' + name); panel.id = 'company-panel-' + name; panel.setAttribute('role', 'tabpanel'); panel.setAttribute('aria-labelledby', button.id); });
    document.querySelector('.company-task-tabs').addEventListener('keydown', event => { const tabs = [...document.querySelectorAll('[data-company-tab]')], index = tabs.indexOf(document.activeElement); if (index < 0 || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return; event.preventDefault(); const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length; tabs[next].focus(); selectReviewTab(tabs[next].dataset.companyTab); });
    by('reviewModal').addEventListener('hide.bs.modal', event => { if (allowReviewClose || busy || JSON.stringify([by('gradeInput').value, by('feedbackInput').value]) === reviewBaseline) return; event.preventDefault(); TaskDialogs.confirm('Discard unsaved review?', 'Your unsaved grade and feedback will be discarded.', 'Discard review').then(confirmed => { if (confirmed) { allowReviewClose = true; modal('reviewModal').hide(); } }); });
    async function renderTaskExams(taskId) {
        const host = by('companyTaskExams'); host.textContent = 'Loading authorized examinations…';
        try { const response = await fetch('../TaskExamsApi.ashx?action=list', { cache: 'no-store', credentials: 'same-origin' }); const exams = await response.json(); if (!response.ok) throw Error(exams.message || 'Exam service unavailable.'); if (selectedId !== taskId) return; host.replaceChildren(); for (const exam of Object.values(exams).filter(item => item.taskId === taskId)) { const card = document.createElement('article'); card.className = 'exam-review-card'; const title = document.createElement('h6'); title.textContent = exam.draft?.title || exam.title || 'Task examination'; const button = document.createElement('button'); button.type = 'button'; button.className = 'btn btn-primary'; button.textContent = 'Review exam and result visibility'; button.disabled = exam.status !== 'published'; button.onclick = () => window.CompanyExamResults.open(exam.id); card.append(title, button); host.append(card); } if (!host.children.length) host.textContent = 'No published examination linked to this task.'; } catch (error) { host.textContent = error.message; }
    }
    let refreshingWorkspace;
    function refresh() { return refreshingWorkspace || (refreshingWorkspace = loadWorkspace().finally(() => { refreshingWorkspace = null; })); }
    async function loadExamStates() { try { const response = await fetch('../TaskExamsApi.ashx?action=list', { credentials: 'same-origin', cache: 'no-store' }); if (response.ok) examStates = new Map(Object.values(await response.json()).map(exam => [exam.taskId, exam])); } catch (_) { /* Task records remain usable during an exam service outage. */ } }
    async function loadWorkspace() {
        loadingTasks = true;
        try { [state] = await Promise.all([request(''), loadExamStates()]); if (state.serverNow) serverOffset = Date.parse(state.serverNow) - Date.now(); boardLoadError = ''; loadingTasks = false; render(); connection('Connected to Firebase /tasks using your verified account.'); await refreshBoard(); if (by('reviewModal').classList.contains('show')) { const task = state.tasks.find(item => item.id === selectedId); if (task) { const canReview = task.status === 'submitted' && state.submissions.some(item => item.taskId === task.id); by('approveTaskBtn').disabled = !canReview; by('requestRevisionBtn').disabled = !canReview; by('gradeInput').disabled = !canReview; by('feedbackInput').disabled = !canReview; } } if (window.CompanyExamResults) await CompanyExamResults.refresh().catch(error => message(error.message, true)); return true; }
        catch (error) {
            state = { tasks: [], submissions: [], students: [] };
            boardState = null; boardLoadError = error.message; loadingTasks = false;
            message(error.message, true); connection('Could not load task records from Firebase.');
            render();
            const list = by('taskList'); list.querySelectorAll('.task-card').forEach(card => card.remove());
            const empty = by('noTasksMsg'); if (empty) { empty.style.display = 'block'; empty.querySelector('h4').textContent = 'Tasks unavailable'; empty.querySelector('p').textContent = 'Check the Firebase connection and Rules, then retry.'; }
            document.querySelectorAll('.stat-details h3').forEach(el => el.textContent = '—');
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
        if (window.TaskComposer && !TaskComposer.validate()) return;
        let firstInvalid = null;
        for (const id of ['newTaskTitle', 'newTaskDescription', 'newTaskDeadline', 'newTaskMaximum', 'newTaskCriteria']) {
            const field = by(id); const valid = field.checkValidity() && field.value.trim().length > 0;
            field.classList.toggle('is-invalid', !valid); field.setAttribute('aria-invalid', String(!valid));
            let error = by(id + 'Error'); if (!error) { error = document.createElement('span'); error.id = id + 'Error'; error.className = 'field-error'; field.after(error); field.setAttribute('aria-describedby', error.id); }
            error.textContent = valid ? '' : (field.validationMessage || 'This field is required.'); if (!valid && !firstInvalid) firstInvalid = field;
        }
        by('studentSelectionError').textContent = selectedPlacements.size ? '' : 'Select at least one authorized student.';
        if (firstInvalid || !selectedPlacements.size) { const field = firstInvalid || by('studentSearch'); field.scrollIntoView({ block: 'center' }); field.focus(); return; }
        const payload = { title: by('newTaskTitle').value.trim(), description: by('newTaskDescription').value.trim(), deadline: new Date(by('newTaskDeadline').value).toISOString(), placementIds: [...selectedPlacements], maximum: Number(by('newTaskMaximum').value), criteria: by('newTaskCriteria').value.trim(), batchId: creationBatch || (creationBatch = operationId()) };
        const missingTaskFields = !payload.title || !payload.description || !payload.deadline || !payload.criteria || !Number.isInteger(payload.maximum) || payload.maximum < 1 || payload.maximum > 1000;
        if (missingTaskFields) { by('createTaskError').hidden = false; by('createTaskError').textContent = 'Complete the title, description, deadline and evaluation criteria. The maximum score must be a whole number from 1 to 1000.'; return; }
        if (!selectedPlacements.size) {
            by('createTaskError').hidden = false;
            by('createTaskError').textContent = state.students.length
                ? 'Choose an active trainee placement from the Firebase list.'
                : 'No active trainee placement for this company was returned from Firebase. Check /placements, the supervisor assignment and Firebase Rules, then refresh.';
            return;
        }
        if (window.TaskComposer) {
            busy = true; by('publishTaskBtn').disabled = true;
            try { await TaskComposer.checkAvailability(); }
            catch (error) { by('createTaskError').hidden = false; by('createTaskError').textContent = error.message; by('createTaskError').scrollIntoView({ block: 'center' }); return; }
            finally { busy = false; by('publishTaskBtn').disabled = false; }
        }
        if (!await TaskDialogs.confirm('Create assignments?', 'Create ' + selectedPlacements.size + ' independent student task(s)?' + (by('configureTaskExam').checked ? ' The configured examination will be published for each student.' : ''), 'Create tasks')) return;
        busy = true; by('publishTaskBtn').disabled = true; by('publishTaskBtn').textContent = 'Saving tasks…'; by('createTaskError').hidden = true; window.TaskComposer?.setBusy(true);
        try {
            const result = await request('bulkCreate', payload), completed = result.results.filter(r => r.success);
            for (const task of completed) for (const file of by('createTaskResources').files) {
                if (completedResourceUploads.get(task.id)?.has(file)) continue;
                if (file.size > 10 * 1024 * 1024) throw new Error('Task saved. Resource exceeds 10 MiB: ' + file.name);
                await upload('attach', { taskId: task.id }, file);
                if (!completedResourceUploads.has(task.id)) completedResourceUploads.set(task.id, new Set());
                completedResourceUploads.get(task.id).add(file);
            }
            await refresh();
            if (!result.success) throw new Error('Some assignments were saved. Retry with the same selections to finish safely. ' + result.results.filter(r => !r.success).map(r => r.message).join(' '));
            if (window.TaskComposer) {
                const includesExam = by('configureTaskExam').checked;
                await TaskComposer.createExams(completed, payload.batchId);
                creationComplete = true; createDirty = false; allowCreateClose = true; modal('createTaskModal').hide();
                message(completed.length + ' task(s)' + (includesExam ? ' and examination(s)' : '') + ' saved to Firebase.', false);
                await refresh();
            } else if (by('configureTaskExam').checked) {
                const host = by('taskExamBuilder'); host.hidden = false; host.replaceChildren();
                for (const task of completed) {
                    const section = document.createElement('details'), heading = document.createElement('summary'), frame = document.createElement('iframe');
                    heading.textContent = 'Exam · ' + (state.students.find(s => s.placementId === task.placementId)?.name || task.placementId);
                    frame.title = heading.textContent; frame.src = 'ManageExams.aspx?embedded=1&taskId=' + encodeURIComponent(task.id) + '&placementId=' + encodeURIComponent(task.placementId);
                    section.append(heading, frame); section.open = completed.length === 1; host.append(section);
                }
                host.scrollIntoView({ block: 'start', behavior: 'smooth' });
                by('publishTaskBtn').textContent = 'Assignments saved';
            } else { creationComplete = true; createDirty = false; allowCreateClose = true; modal('createTaskModal').hide(); }
            creationBatch = null;
        }
        catch (error) { by('createTaskError').hidden = false; by('createTaskError').textContent = error.name === 'TimeoutError' ? 'The save request timed out. Keep this popup open and retry to continue the same assignments safely.' : error.message; by('createTaskError').scrollIntoView({ block: 'center' }); }
        finally { busy = false; window.TaskComposer?.setBusy(false); by('publishTaskBtn').disabled = creationComplete; by('publishTaskBtn').textContent = 'Create task'; }
    });
    async function review(reviewAction) {
        if (busy || !selectedId) return;
        const payload = { assignmentId: selectedId, reviewAction, score: Number(by('gradeInput').value), feedback: by('feedbackInput').value.trim() };
        if (reviewAction === 'grade' && !TaskDialogs.validate(by('gradeInput').parentElement)) { by('modalErrorContainer').textContent = 'Enter a valid grade before approving.'; return; }
        if (reviewAction === 'revision') { await editTask(state.tasks.find(t => t.id === selectedId), true); return; }
        if (!await TaskDialogs.confirm('Approve task grade?', 'Save this company task score and lock the task instructions?', 'Approve grade')) return;
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
    by('boardPlacementSelect')?.addEventListener('change', () => { render(); refreshBoard(); });
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
        const movedTask = state.tasks.find(task => task.id === draggedTaskId), targetTask = state.tasks.find(task => task.id === target?.dataset.boardTaskId);
        const payload = { placementId: movedTask?.placementId || boardState.placementId, taskId: draggedTaskId, columnId: column.dataset.columnId,
            beforeTaskId: targetTask?.placementId === movedTask?.placementId ? targetTask?.id : null, operationId: operationId() };
        boardBusy = true;
        try {
            try { await request('move', payload); }
            catch (error) { if (error instanceof TypeError) await request('move', payload); else throw error; }
            await refreshBoard(); message('Task position saved to Firebase. Task workflow status was not changed.', false);
        } catch (error) { message(error.message, true); await refreshBoard(); }
        finally { boardBusy = false; draggedTaskId = null; }
    });
    by('taskSearch')?.addEventListener('input', render); by('statusFilter')?.addEventListener('change', render);
    by('deadlineFilter').addEventListener('change', render); by('batchFilter').addEventListener('change', render);
    by('statusFilter').addEventListener('change', () => { if (by('statusFilter').value === 'archived') by('listModeButton').click(); });
    async function editTask(task, resubmit = false) {
        if (!task || task.score != null) { message('Graded task instructions are locked.', true); return; }
        if (editingTask) return; editingTask = true;
        try {
        const content = document.createElement('div');
        content.innerHTML = '<label>Title<input name="title" required maxlength="150"></label><label>Description<textarea name="description" required maxlength="3000"></textarea></label><label>Instructions<input name="criteria" required maxlength="500"></label><label>New deadline<input name="deadline" type="datetime-local" required></label><label>Apply to<select name="scope"><option value="one">This student only</option>' + (task.batchId ? '<option value="all">All ungraded students in this batch</option>' : '') + '</select></label><label>Submitted work<select name="submission"><option value="keep">Keep current submission under review</option><option value="resubmit">Request a new submission</option></select></label><p>Graded instances are excluded. Existing submissions and instruction versions are preserved.</p>';
        const field = n => content.querySelector('[name="' + n + '"]');
        ['title', 'description', 'criteria'].forEach(n => field(n).value = task[n] || '');
        const date = new Date(task.deadline); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); if (!isNaN(date)) field('deadline').value = date.toISOString().slice(0, 16);
        field('submission').value = resubmit ? 'resubmit' : 'keep';
        const resourceLabel = document.createElement('label'), resources = document.createElement('input'); resourceLabel.textContent = 'Add resources to affected tasks'; resources.type = 'file'; resources.multiple = true; resources.accept = '.pdf,.docx,.xlsx,.pptx,.txt,.png,.jpg,.jpeg'; resourceLabel.append(resources); content.append(resourceLabel);
        const group = task.batchId ? state.tasks.filter(t => t.batchId === task.batchId) : [task];
        const deadlines = document.createElement('details'), heading = document.createElement('summary'); heading.textContent = 'Individual student deadlines'; deadlines.append(heading);
        group.filter(t => t.score == null).forEach(t => { const label = document.createElement('label'), input = document.createElement('input'); label.textContent = t.studentName; input.type = 'datetime-local'; input.dataset.taskDeadline = t.id; label.append(input); deadlines.append(label); }); content.append(deadlines);
        if (!await TaskDialogs.open('Edit task · ' + task.studentName, content, { confirm: 'Review changes' })) return;
        const targets = field('scope').value === 'all' ? group : [task], affected = targets.filter(t => t.score == null), excluded = targets.filter(t => t.score != null);
        if (!await TaskDialogs.confirm('Confirm task changes', 'Affected: ' + affected.map(t => t.studentName).join(', ') + '. Excluded (graded): ' + (excluded.map(t => t.studentName).join(', ') || 'None') + '. ' + field('submission').selectedOptions[0].text + '. Default deadline: ' + field('deadline').value, 'Save changes')) return;
        const individual = Object.fromEntries([...content.querySelectorAll('[data-task-deadline]')].filter(el => el.value).map(el => [el.dataset.taskDeadline, new Date(el.value).toISOString()]));
        try {
            const result = await request('edit', { taskId: task.id, title: field('title').value, description: field('description').value, criteria: field('criteria').value, deadline: new Date(field('deadline').value).toISOString(), deadlines: individual, all: field('scope').value === 'all', resubmit: field('submission').value === 'resubmit' });
            for (const taskId of result.affected) for (const file of resources.files) await upload('attach', { taskId }, file);
            await refresh();
            const failures = (result.failed || []).map(item => (state.tasks.find(t => t.id === item.taskId)?.studentName || item.taskId) + ': ' + item.message);
            const outcome = result.affected.length + ' updated; ' + result.excluded.length + ' graded instances excluded.' + (failures.length ? ' Not updated: ' + failures.join(' ') : '');
            message(outcome, failures.length > 0); by('modalErrorContainer').textContent = outcome;
            if (!failures.length) modal('reviewModal').hide();
        }
        catch (error) { message(error.message, true); }
        } finally { editingTask = false; }
    }
    const editButton = document.createElement('button'); editButton.type = 'button'; editButton.className = 'btn btn-light'; editButton.textContent = 'Edit task'; editButton.onclick = () => editTask(state.tasks.find(t => t.id === selectedId)); by('reviewModal').querySelector('.modal-footer').prepend(editButton);
    const archiveButton = document.createElement('button'); archiveButton.type = 'button'; archiveButton.className = 'btn btn-outline-danger'; archiveButton.textContent = 'Archive task';
    archiveButton.onclick = async () => { const task = state.tasks.find(t => t.id === selectedId); if (!task || task.archived || busy) return; if (!await TaskDialogs.confirm('Archive this task?', 'Move this student’s task to read-only history? Submissions, grades, resources and board positions are preserved.', 'Archive task')) return; busy = true; archiveButton.disabled = true; try { await request('archive', { taskId: task.id }); modal('reviewModal').hide(); await refresh(); message('Task archived. Choose the Archived status in List view to see its history.', false); } catch (error) { by('modalErrorContainer').textContent = error.message; } finally { busy = false; archiveButton.disabled = false; } }; by('reviewModal').querySelector('.modal-footer').prepend(archiveButton);
    const extensionButton = document.createElement('button'); extensionButton.type = 'button'; extensionButton.className = 'btn btn-light'; extensionButton.textContent = 'Review extension';
    extensionButton.onclick = async () => {
        const task = state.tasks.find(t => t.id === selectedId); if (task?.extension?.status !== 'pending') { by('modalErrorContainer').textContent = 'No pending extension request.'; return; }
        const body = document.createElement('div'), reason = document.createElement('p'); reason.textContent = task.extension.reason; body.append(reason);
        body.insertAdjacentHTML('beforeend', '<label>Decision<select name="decision"><option value="approve">Approve</option><option value="reject">Reject permanently</option></select></label><label>Final deadline<input type="datetime-local" name="deadline"></label><label>Decision reason<textarea name="reason" maxlength="3000"></textarea></label>');
        const updateRequirements = () => { const approve = body.querySelector('select').value === 'approve'; body.querySelector('input').required = approve; body.querySelector('textarea').required = !approve; }; body.querySelector('select').onchange = updateRequirements; updateRequirements();
        if (!await TaskDialogs.open('Extension request · ' + task.studentName, body, { confirm: 'Review decision' })) return;
        const approve = body.querySelector('select').value === 'approve', date = body.querySelector('input').value;
        if (approve && !date) { message('A final deadline is required.', true); return; }
        if (!await TaskDialogs.confirm('Confirm extension decision?', approve ? 'Reopen this student’s submission until the selected deadline.' : 'Rejection is final. The student cannot request another extension.', approve ? 'Approve' : 'Reject permanently')) return;
        try { await request('extension', { taskId: task.id, approve, deadline: date ? new Date(date).toISOString() : '', reason: body.querySelector('textarea').value }); await refresh(); message('Extension decision saved.', false); } catch (error) { message(error.message, true); }
    }; by('reviewModal').querySelector('.modal-footer').prepend(extensionButton);
    by('taskBoard').addEventListener('dragstart', e => e.target.closest('.task-board-card')?.classList.add('dragging'));
    by('taskBoard').addEventListener('dragend', () => document.querySelectorAll('.dragging,.drop-target').forEach(el => el.classList.remove('dragging', 'drop-target')));
    by('taskBoard').addEventListener('dragover', e => { document.querySelectorAll('.drop-target').forEach(el => el.classList.remove('drop-target')); e.target.closest('.task-board-column')?.classList.add('drop-target'); });
    window.addEventListener('focus', refresh); document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    TaskDialogs.watch(refresh, () => !busy && !boardBusy && !by('createTaskModal').classList.contains('show'));
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    by('taskBoard').addEventListener('task-board-retry', refresh);
    by('taskList').hidden = true; document.querySelectorAll('.stat-details h3').forEach(el => el.textContent = '—'); renderBoard(); refresh();
})();
