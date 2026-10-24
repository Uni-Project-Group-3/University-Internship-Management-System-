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
    function emptyAssignmentMessage() { return state.identity ? 'No tasks are assigned to student ' + state.identity + '. Confirm that your supervisor selected this student account.' : 'Assigned tasks will appear here once your supervisor adds them.'; }
    const modal = id => window.bootstrap && bootstrap.Modal.getOrCreateInstance(by(id));
    function selectTaskTab() {
        const task=state.tasks.find(t=>t.id===selectedId),isExam=!!(task?.examId||task?.taskType==='exam'||window.StudentTaskExams?.hasTask(task?.id));
        document.querySelectorAll('[data-student-panel]').forEach(panel=>{panel.hidden=isExam?panel.dataset.studentPanel!=='exam':panel.dataset.studentPanel==='exam';});
        by('confirmSubmitBtn').hidden=isExam||!editing||by('workDesc').readOnly;
        if(isExam){by('studentTaskWorkAction').hidden=true;by('requestTaskExtension').hidden=true;}
        if(isExam)window.StudentTaskExams?.showTask(selectedId);
    }
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
        if (response.status === 401 || (response.redirected && /\/Auth\/Login(?:\.aspx)?(?:[/?]|$)/i.test(new URL(response.url).pathname)))
            throw new Error('Your sign-in expired. Sign in again to load your tasks.');
        let result;
        try { result = await response.json(); } catch (_) { throw new Error('The task service returned an unexpected response (HTTP ' + response.status + '). Refresh or sign in again.'); }
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
        const host=by('studentTaskAttachments');host.textContent='Loading authorized files…';
        try{const result=await request('attachments',{taskId},'GET');if(selectedId!==taskId)return;host.replaceChildren();
            for(const [purpose,label]of [['taskAttachment','Company Resources'],['submissionAttachment','Student Submission Files']]){
                const group=document.createElement('section'),heading=document.createElement('h6');heading.textContent=label;group.append(heading);
                const files=result.files.filter(f=>purpose==='taskAttachment'?f.purpose==='taskAttachment':f.purpose!=='taskAttachment');
                if(!files.length){const empty=document.createElement('p');empty.className='text-muted';empty.textContent='No files provided.';group.append(empty);}
                for(const file of files){const row=document.createElement('div'),name=document.createElement('span'),button=document.createElement('button');row.className='task-file-row';name.textContent=file.fileName+' · '+Math.ceil(file.sizeBytes/1024)+' KB';button.type='button';button.className='btn btn-light';button.textContent='Download';
                    button.onclick=async()=>{button.disabled=true;try{const response=await fetch('../TasksApi.ashx?action=download&fileId='+encodeURIComponent(file.id),{credentials:'same-origin',cache:'no-store'});if(!response.ok)throw Error('Authorized download failed. Please retry.');const url=URL.createObjectURL(await response.blob()),link=document.createElement('a');link.href=url;link.download=file.fileName;link.click();setTimeout(()=>URL.revokeObjectURL(url),10000);}catch(e){const error=document.createElement('p');error.className='field-error';error.textContent=e.message;row.append(error);}finally{button.disabled=false;}};row.append(name,button);group.append(row);
                }host.append(group);
            }
        }catch(e){host.textContent=e.message;}
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

    function syncTaskTitles(){const select=by('taskSearch'),old=select.value,titles=[...new Set(state.tasks.map(t=>t.title))].sort();select.replaceChildren(new Option('All Tasks',''));titles.forEach(t=>select.add(new Option(t,t.toLowerCase())));if(titles.some(t=>t.toLowerCase()===old))select.value=old;by('heroTaskCount').textContent=state.tasks.filter(t=>t.status!=='completed'&&!t.archived).length;by('heroExamCount').textContent=state.tasks.filter(t=>t.examId||t.taskType==='exam'||window.StudentTaskExams?.hasTask(t.id)).length;}
    function premiumCard(task){const exam=!!(task.examId||task.taskType==='exam'||window.StudentTaskExams?.hasTask(task.id)),pending=task.status==='assigned',working=['inprogress','revision'].includes(task.status),due=Date.parse(task.deadline||task.dueDate),left=due-now();return '<article class="task-card task-board-card task-board-card-readonly" data-status="'+escapeHtml(task.status)+'"><div class="task-board-card-head"><h3>'+escapeHtml(task.title)+'</h3><span class="status-badge">'+escapeHtml(statusLabel(task.status))+'</span></div><p class="task-desc">'+escapeHtml(task.description)+'</p><div class="task-card-dates"><span>Start: '+escapeHtml(dateLabel(task.startsAt||task.createdAt))+'</span><span>Due: '+escapeHtml(dateLabel(task.deadline||task.dueDate))+'</span><span class="'+(left<0?'task-overdue':'')+'">'+(task.status==='completed'?'Completed':task.status==='submitted'?'Submitted for review':Number.isFinite(left)?left<0?'Overdue':Math.ceil(left/3600000)+' hours remaining':'No deadline supplied')+'</span></div><span class="task-type-badge">'+(exam?'Exam':'Standard Task')+'</span>'+(task.status==='submitted'?'<p class="task-review-notice">Awaiting Supervisor Review</p>':'')+(task.status==='completed'?'<p class="task-approved-notice">Approved'+(!exam&&task.score!=null?' · '+escapeHtml(task.score)+' / '+escapeHtml(task.maximum):'')+'</p>':'')+(task.attachmentIds?.length?'<small>Attachments available</small>':'')+'<div class="task-action"><button type="button" class="btn btn-light" data-task-action="details" data-task-id="'+escapeHtml(task.id)+'">'+(working?'Continue Task':'View')+'</button>'+(pending?'<button type="button" class="btn btn-primary" data-task-action="start" data-task-id="'+escapeHtml(task.id)+'">Accept Task</button>':'')+'</div></article>';}

    function matchesFilters(task) {
        const title = (by('taskSearch')?.value || '').toLowerCase();
        const status = by('taskFilter')?.value || '';
        return (!task.archived || status === 'archived') && (!title || task.title.toLowerCase() === title) &&
            (!status || status === 'all' || task.status === status);
    }

    function render() {
        syncTaskTitles();
        const tasks = state.tasks.filter(matchesFilters);
        const list = by('taskList');
        list.querySelectorAll('.task-card').forEach(card => card.remove());
        const markup = tasks.map(task => premiumCard(task)).join('');
        if (markup) by('noTasksMsg').insertAdjacentHTML('beforebegin', markup);
        decorateTaskStates(list);
        const empty = by('noTasksMsg');
        if (empty) {
            empty.style.display = tasks.length ? 'none' : 'block';
            empty.querySelector('h4').textContent = state.tasks.length ? 'No tasks match these filters' : 'No tasks assigned yet';
            empty.querySelector('p').textContent = state.tasks.length ? 'Choose All Tasks or All Statuses to see tasks.' : emptyAssignmentMessage();
        }
        document.querySelectorAll('.stat-details h3').forEach((el, i) => { el.textContent = [state.tasks.length,state.tasks.filter(t=>t.status==='assigned').length,state.tasks.filter(t=>['inprogress','revision'].includes(t.status)).length,state.tasks.filter(t=>t.status==='submitted').length,state.tasks.filter(t=>t.status==='completed').length][i] ?? 0; });
        const board = by('taskBoard'), listHost = by('taskList');
        if (board && listHost) { board.hidden = viewMode !== 'board'; listHost.hidden = viewMode === 'board'; }
        renderBoard();
    }
    function renderBoard() {
        const host = by('taskBoard'); if (!host) return;
        const scrollPositions = new Map([...host.querySelectorAll('.task-board-column')].map(column => [column.dataset.columnId, column.querySelector('.task-board-dropzone')?.scrollTop || 0]));
        if(boardState){boardState.columns=[{id:'todo',title:'New / Pending'},{id:'inProgress',title:'In Progress'},{id:'review',title:'Review'},{id:'done',title:'Complete'}];boardState.tasks.forEach(t=>t.columnId=({assigned:'todo',inprogress:'inProgress',revision:'inProgress',submitted:'review',completed:'done'})[t.status]||'todo');}
        if (viewMode !== 'board') { host.hidden = true; return; }
        host.hidden = false;
        host.setAttribute('aria-busy', 'false');
        if (!boardState || !boardState.columns.length) { TaskDialogs.renderBoardShell(host, { loading: loadingTasks, error: boardLoadError }); if (!loadingTasks && !boardLoadError && !state.tasks.length) host.querySelector('.board-state-notice span').textContent = emptyAssignmentMessage(); return; }
        const tasks = state.tasks.filter(matchesFilters).map(task => ({ ...task, ...boardState.tasks.find(card => card.id === task.id), columnId: ({assigned:'todo',inprogress:'inProgress',revision:'inProgress',submitted:'review',completed:'done',archived:'done'})[task.status] || 'todo', order: boardState.tasks.find(card => card.id === task.id)?.order || 0 }));
        host.innerHTML = '<div class="task-board-columns">' + boardState.columns.map(column => {
            const cards = tasks.filter(task => task.columnId === column.id).sort((a, b) => a.order - b.order || a.id.localeCompare(b.id));
            return '<section class="task-board-column" data-column-id="' + escapeHtml(column.id) + '"><header><h5>' + escapeHtml(column.title) + '</h5><span>' + cards.length + '</span></header><div class="task-board-dropzone">' + cards.map(task => premiumCard({...state.tasks.find(t=>t.id===task.id),...task})).join('') + (cards.length ? '' : '<div class="task-board-column-empty">No tasks in this column</div>') + '</div></section>';
        }).join('') + '</div>';
        decorateTaskStates(host);
        host.querySelectorAll('.task-board-column').forEach(column => { column.querySelector('.task-board-dropzone').scrollTop = scrollPositions.get(column.dataset.columnId) || 0; });
    }
    async function refreshBoard() {
        const placements = [...new Set(state.tasks.map(task => task.placementId).filter(Boolean))];
        if (!placements.length) { boardState = null; renderBoard(); return false; }
        try { const boards = await Promise.all(placements.map(placementId => request('board', { placementId }, 'GET')));
            boardState = { canMove: false, columns: boards[0].columns, tasks: boards.flatMap(board => board.tasks) }; boardLoadError = ''; renderBoard(); return true; }
        catch (error) { boardState = null; boardLoadError = error.message; loadingTasks = false; renderBoard(); message(error.message, true); return false; }
    }
    function openSubmit(task, readOnly) {
        selectedId = task.id;
        editing = !readOnly; allowClose = false;
        let information = by('studentTaskInstructions');
        if (!information) { information = document.createElement('section'); information.id = 'studentTaskInstructions'; information.className = 'feedback-note'; by('submitModal').querySelector('.modal-body').prepend(information); }
        information.innerHTML = '<div class="task-detail-metrics">Start: ' + escapeHtml(dateLabel(task.startsAt || task.createdAt)) + ' · Deadline: ' + escapeHtml(dateLabel(task.deadline)) + '</div><h6>Task instructions</h6><p style="white-space:pre-wrap">' + escapeHtml(task.description) + '</p><p style="white-space:pre-wrap">' + escapeHtml(task.criteria || task.evaluationCriteria || task.instructions || '') + '</p><p>Deadline: ' + escapeHtml(dateLabel(task.deadline)) + ' · Maximum grade: ' + escapeHtml(task.maximum) + '</p>';
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
        examLink.hidden = true; examLink.textContent = 'View exam'; examLink.onclick = () => selectTaskTab('exam');
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
        by('workDesc').value = readOnly ? (latest?.description || '') : '';
        by('workLink').value = readOnly ? (latest?.link || '') : '';
        if (!readOnly) by('workFile').value = '';
        by('workDesc').readOnly = !!readOnly; by('workLink').readOnly = !!readOnly; by('workFile').disabled = !!readOnly;
        by('confirmSubmitBtn').hidden = !!readOnly; by('modalError').textContent = '';
        let extensionButton = by('requestTaskExtension');
        if (!extensionButton) { extensionButton = document.createElement('button'); extensionButton.id = 'requestTaskExtension'; extensionButton.type = 'button'; extensionButton.className = 'btn btn-light'; by('submitModal').querySelector('.modal-footer').prepend(extensionButton); }
        const overdue = now() >= new Date(task.deadline.length === 10 ? task.deadline + 'T23:59:59Z' : task.deadline).getTime();
        extensionButton.hidden = task.archived || !overdue || task.score != null || task.status === 'submitted' || task.status === 'completed';
        extensionButton.disabled = task.extension?.status === 'pending' || task.extension?.status === 'rejected';
        extensionButton.textContent = task.extension?.status === 'pending' ? 'Extension pending' : task.extension?.status === 'rejected' ? 'Extension rejected · final' : 'Request extension';
        if (overdue) { by('confirmSubmitBtn').hidden = true; by('modalError').textContent = 'Overdue. An approved extension is required before submitting.'; }
        extensionButton.onclick = async () => {
            const body = document.createElement('div'); body.innerHTML = '<label>Reason<textarea name="reason" required maxlength="3000"></textarea></label><label>Proposed new deadline<input name="deadline" type="datetime-local" required></label><p>Timezone: ' + escapeHtml(Intl.DateTimeFormat().resolvedOptions().timeZone) + '</p>';
            if (!await TaskDialogs.open('Request deadline extension', body, { confirm: 'Send request' })) return;
            try { await request('extension', { taskId: task.id, reason: body.querySelector('textarea').value, deadline: new Date(body.querySelector('input').value).toISOString() }); allowClose = true; modal('submitModal').hide(); await refresh(); message('Extension request sent to your company supervisor.', false); } catch (error) { by('modalError').textContent = error.message; }
        };
        selectTaskTab();
        modal('submitModal')?.show();
        loadAttachments(task.id);
    }
    let workspaceLoading;
    function refresh() { return workspaceLoading || (workspaceLoading = loadWorkspace().finally(() => { workspaceLoading = null; })); }
    async function loadWorkspace() {
        loadingTasks = true;
        try { const examRefresh = window.StudentTaskExams ? StudentTaskExams.refresh().catch(error => message(error.message, true)) : Promise.resolve(); state = await request(''); if (!Array.isArray(state.tasks) || !Array.isArray(state.submissions)) throw new Error('The task service returned an invalid task list. Refresh and try again.'); if (state.serverNow) serverOffset = Date.parse(state.serverNow) - Date.now(); boardLoadError = ''; loadingTasks = false; render(); connection('Connected to Firebase /tasks using your verified account.' + (state.identity ? ' Student ID: ' + state.identity : '')); await Promise.all([refreshBoard(), examRefresh]); return true; }
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
                if (!await workflowConfirm('Start this task?', 'The task will be saved as in progress for your supervisor.', 'Start task')) return;
                busy = true; button.disabled = true; await request('start', { assignmentId: task.id }); await refresh(); message('Task started and saved to Firebase.', false); return;
            }
            openSubmit(task, false);
        } catch (error) { message(error.message, true); }
        finally { busy = false; }
    });
    by('confirmSubmitBtn')?.addEventListener('click', async () => {
        if (busy || !selectedId) return;
        const description = by('workDesc').value.trim(), link = by('workLink').value.trim();
        let firstInvalid = null;
        for (const [id, value] of [['workDesc', description || by('workFile').files.length], ['workLink', link || by('workFile').files.length]]) {
            const field = by(id); let error = by(id + 'Validation');
            if (!error) { error = document.createElement('p'); error.id = id + 'Validation'; error.className = 'field-error'; field.after(error); field.setAttribute('aria-describedby', error.id); }
            field.classList.toggle('is-invalid', !value); field.setAttribute('aria-invalid', String(!value)); error.textContent = value ? '' : 'This field is required.';
            if (!value && !firstInvalid) firstInvalid = field;
        }
        if (firstInvalid) { firstInvalid.focus(); return; }
        if (!await workflowConfirm('Submit your work?', 'Send this submission to your company supervisor for review?', 'Submit for review')) return;
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
    by('taskSearch')?.addEventListener('change', render); by('taskFilter')?.addEventListener('change', render);
    function idleRefresh() { if (document.hidden || busy || by('submitModal').classList.contains('show')) return; if (!refreshPending) refreshPending = refresh().finally(() => { refreshPending = null; }); }
    by('submitModal').addEventListener('hide.bs.modal', event => {
        if (busy && !allowClose) { event.preventDefault(); return; }
        if (allowClose || !editing || !(by('workDesc').value.trim() || by('workLink').value.trim() || by('workFile').files.length)) return;
        event.preventDefault(); TaskDialogs.confirm('Discard unfinished submission?', 'Your unsent work will be discarded.', 'Discard').then(confirmed => { if (confirmed) { allowClose = true; modal('submitModal').hide(); } });
    });
    window.StudentTaskExams?.index(); window.addEventListener('task-exam-index',()=>{render();});
    window.addEventListener('focus', idleRefresh); document.addEventListener('visibilitychange', idleRefresh);
    TaskDialogs.watch(refresh, () => !busy);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    by('taskBoard').addEventListener('task-board-retry', refresh);
    by('taskList').hidden = true; document.querySelectorAll('.stat-details h3').forEach(el => el.textContent = '—'); renderBoard(); refresh();
})();
