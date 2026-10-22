(function () {
    'use strict';
    const api = '../TasksApi.ashx?role=company';
    let state = { tasks: [], submissions: [], students: [] }, selectedId = null, busy = false;
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
        select.innerHTML = '<option value="">Choose trainee</option>' + state.students.map(student => '<option value="' + escapeHtml(student.id) + '">' + escapeHtml(student.name) + ' (' + escapeHtml(student.id) + ')</option>').join('');
        if (state.students.some(student => student.id === previous)) select.value = previous;
        else if (state.students.length === 1) select.value = state.students[0].id;
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
        modal('reviewModal')?.show();
    }
    async function refresh() {
        try { state = await request(''); render(); connection('Connected to Firebase /tasks. No-login task mode is active.'); return true; }
        catch (error) {
            message(error.message, true); connection('Could not load task records from Firebase.');
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
        const payload = { title: by('newTaskTitle').value.trim(), description: by('newTaskDescription').value.trim(), deadline: by('newTaskDeadline').value, studentId: by('newTaskStudent').value, maximum: Number(by('newTaskMaximum').value), criteria: by('newTaskCriteria').value.trim() };
        if (!payload.title || !payload.description || !payload.deadline || !payload.studentId || !payload.criteria || !Number.isInteger(payload.maximum)) { by('createTaskError').hidden = false; by('createTaskError').textContent = 'Complete all task fields and choose a confirmed trainee.'; return; }
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
    by('taskSearch')?.addEventListener('input', render); by('statusFilter')?.addEventListener('change', render);
    window.addEventListener('focus', refresh); document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    window.setInterval(() => { if (!document.hidden) refresh(); }, 30000);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    refresh();
})();
