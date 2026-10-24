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
    let creationPlan = null, recoveryListLoaded = false, fileChecks = 0;
    const creationProgress = new Map();
    function showCreationProgress() {
        let host = by('taskCreationProgress');
        if (!host) { host = document.createElement('section'); host.id = 'taskCreationProgress'; host.className = 'creation-progress'; host.setAttribute('role', 'status'); host.setAttribute('aria-live', 'polite'); by('createTaskModal').querySelector('.modal-body').prepend(host); }
        host.replaceChildren(); host.hidden = !creationPlan;
        if (!creationPlan) { if (by('creationFileReselection')) by('creationFileReselection').hidden = true; return; }
        const title = document.createElement('strong'); title.textContent = 'Assignment batch ' + creationPlan.payload.batchId;
        const note = document.createElement('p'); note.textContent = 'This plan is saved in Firebase. Resume after reopening this page; only unsaved file bytes need reselection.';
        const list = document.createElement('ul');
        for (const [placementId, item] of creationProgress) { const li = document.createElement('li'); li.textContent = (state.students.find(s => s.placementId === placementId)?.name || placementId) + ' · Task: ' + (item.task || 'Pending') + ' · Resources: ' + (item.resources || 'Pending') + (creationPlan.payload.taskType === 'exam' ? ' · Exam: ' + (item.exam || 'Pending') : ''); list.append(li); }
        host.append(title, note, list);
    }
    async function describeFile(file, id, questionId) {
        const bytes = await file.arrayBuffer(), digest = await crypto.subtle.digest('SHA-256', bytes);
        return {id,name:file.name,size:file.size,type:file.type,sha256:[...new Uint8Array(digest)].map(value => value.toString(16).padStart(2, '0')).join(''),...(questionId ? {questionId} : {})};
    }
    function applyCreationOperation(operation) {
        creationPlan.operation = operation; creationPlan.persisted = true;
        for (const row of operation.progress || []) creationProgress.set(row.placementId, {task:row.taskSaved ? 'Saved (' + row.taskId + ')' : 'Pending',resources:row.resourcesReady ? 'Saved / none required' : 'Needs files',exam:row.examPublished ? 'Published' : 'Pending',server:row});
        window.TaskComposer?.applyOperation(operation); showCreationProgress(); renderReselections();
    }
    function renderReselections() {
        let host = by('creationFileReselection');
        if (!host) { host = document.createElement('section'); host.id = 'creationFileReselection'; host.className = 'creation-progress'; by('createTaskModal').querySelector('.modal-body').prepend(host); }
        host.replaceChildren(); host.hidden = !creationPlan; if (!creationPlan) return;
        const rows = creationPlan.operation?.progress || [], required = [];
        (creationPlan.persistedPlan.resources || []).forEach((file,index) => { if (rows.some(row => !row.resourceFiles?.[file.id])) required.push({manifest:file,current:creationPlan.files[index],assign:selected => creationPlan.files[index] = selected}); });
        (creationPlan.persistedPlan.questionImages || []).forEach(file => { if (rows.some(row => !row.examPublished && !row.images?.[file.questionId])) required.push({manifest:file,current:creationPlan.questionFiles.get(file.questionId),assign:selected => {creationPlan.questionFiles.set(file.questionId,selected);TaskComposer.setQuestionFile(file.questionId,selected);}}); });
        if (!required.length) { host.hidden = true; return; }
        const heading = document.createElement('strong'); heading.textContent = 'Files required for unfinished steps'; host.append(heading);
        for (const item of required) {
            const label = document.createElement('label'); label.textContent = item.manifest.name + (item.current ? ' — selected for retry' : ' — reselect original file');
            if (!item.current) {
                const input = document.createElement('input'), error = document.createElement('span'); input.type = 'file'; input.className = 'form-control'; input.setAttribute('aria-label','Reselect ' + item.manifest.name); error.className = 'field-error';
                input.onchange = async () => {
                    const file = input.files[0]; if (!file) return; fileChecks++; by('publishTaskBtn').disabled = true;
                    try { const actual = await describeFile(file,item.manifest.id,item.manifest.questionId); if (actual.sha256 !== item.manifest.sha256 || actual.name !== item.manifest.name || actual.size !== item.manifest.size) throw Error('Select the original file with the same name and content.'); item.assign(file); error.textContent = ''; label.firstChild.textContent = item.manifest.name + ' — selected for retry'; }
                    catch (failure) { input.value = ''; error.textContent = failure.message; }
                    finally { fileChecks--; by('publishTaskBtn').disabled = busy || fileChecks > 0; }
                }; label.append(input,error);
            }
            host.append(label);
        }
    }
    async function loadRecoveryOperations(force = false) {
        if (recoveryListLoaded && !force) return;
        try {
            const result = await request('creationOperations',{},'GET'); recoveryListLoaded = true;
            let host = by('creationRecoveryList'); if (!host) { host = document.createElement('section'); host.id = 'creationRecoveryList'; host.className = 'creation-progress'; by('taskBoardToolbar').before(host); }
            host.replaceChildren(); host.hidden = !result.operations?.length;
            for (const operation of result.operations || []) {
                const button = document.createElement('button'); button.type = 'button'; button.className = 'btn btn-light'; button.textContent = 'Resume creation: ' + operation.title;
                button.onclick = async () => {
                    if (busy) return; button.disabled = true;
                    try {
                        const saved = await request('creationOperation',{batchId:operation.batchId},'GET'), payload = saved.plan.payload;
                        if (creationPlan && creationPlan.payload.batchId !== operation.batchId && !await TaskDialogs.confirm('Switch recovery plan?', 'Both plans remain saved. Unsaved file selections may need to be reselected.', 'Switch plan')) return;
                        TaskComposer.reset(); creationBatch = payload.batchId; selectedPlacements = new Set(payload.placementIds); creationComplete = false; createDirty = true; allowCreateClose = false;
                        creationPlan = {payload,persistedPlan:saved.plan,files:[],questionFiles:new Map(),persisted:true,operation:saved};
                        const localDate = value => {const date = new Date(value);return new Date(date.getTime()-date.getTimezoneOffset()*60000).toISOString().slice(0,16);};
                        for (const [id,value] of Object.entries({newTaskTitle:payload.title,newTaskDescription:payload.description,newTaskCriteria:payload.criteria,newTaskMaximum:payload.maximum,newTaskStart:localDate(payload.startsAt),newTaskDeadline:localDate(payload.deadline)})) by(id).value = value;
                        renderStudents(); TaskComposer.restore(saved); applyCreationOperation(saved); by('publishTaskBtn').textContent = 'Retry incomplete creation'; by('publishTaskBtn').disabled = false; by('createTaskError').hidden = true; modal('createTaskModal').show();
                    } catch (error) { message(error.message,true); } finally { button.disabled = false; }
                }; host.append(button);
            }
        } catch (error) { message('Saved creation operations could not be loaded: ' + error.message,true); }
    }
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
        window.TaskComposer?.setRecovery(!!creationPlan);
    }
    by('studentSearch').addEventListener('input', renderStudents);
    by('selectAllStudents').onclick = () => { selectedPlacements = new Set(state.students.map(s => s.placementId)); createDirty = true; renderStudents(); };
    by('clearStudents').onclick = () => { selectedPlacements.clear(); createDirty = true; renderStudents(); };
    by('createTaskModal').addEventListener('input', () => { createDirty = true; });
    by('createTaskModal').addEventListener('hidden.bs.modal', () => {
        if (!creationComplete) return;
        creationComplete = false; creationBatch = null; creationPlan = null; creationProgress.clear(); showCreationProgress(); createDirty = false; selectedPlacements.clear(); completedResourceUploads.clear();
        ['newTaskTitle', 'newTaskDescription', 'newTaskCriteria', 'newTaskStart', 'newTaskDeadline', 'createTaskResources', 'studentSearch'].forEach(id => by(id).value = '');
        by('configureTaskExam').checked = false; by('taskExamBuilder').replaceChildren(); by('taskExamBuilder').hidden = true; by('publishTaskBtn').disabled = false; by('publishTaskBtn').textContent = 'Create task'; window.TaskComposer?.reset(); renderStudents();
    });
    by('createTaskModal').addEventListener('hide.bs.modal', async event => {
        if (allowCreateClose || !createDirty) { allowCreateClose = false; return; }
        event.preventDefault();
        if (busy) return;
        if (creationPlan) {
            if (await TaskDialogs.confirm('Pause incomplete creation?', 'Saved tasks and exams will remain. Use Resume creation on this page, including after reopening it, to continue the saved Firebase plan. Unsaved files may need to be reselected.', 'Pause and keep plan')) { allowCreateClose = true; modal('createTaskModal').hide(); }
            return;
        }
        if (await TaskDialogs.confirm('Discard unsaved changes?', 'Your unsaved task fields will be cleared. Saved assignments and exams remain available.', 'Discard changes')) {
            createDirty = false; creationBatch = null; selectedPlacements.clear(); completedResourceUploads.clear();
            ['newTaskTitle', 'newTaskDescription', 'newTaskCriteria', 'newTaskStart', 'newTaskDeadline', 'createTaskResources'].forEach(id => by(id).value = '');
            by('taskExamBuilder').replaceChildren(); by('taskExamBuilder').hidden = true; window.TaskComposer?.reset(); renderStudents(); allowCreateClose = true; modal('createTaskModal').hide();
        }
    });
    document.querySelectorAll('.task-timezone').forEach(el => el.textContent = 'Timezone: ' + Intl.DateTimeFormat().resolvedOptions().timeZone);
    async function request(action, data, requestedMethod) {
        const method = requestedMethod || (action ? 'POST' : 'GET');
        let url = api + (action ? '?action=' + encodeURIComponent(action) : '');
        if (action === 'creationOperation' && method === 'GET') url += '&batchId=' + encodeURIComponent(data.batchId);
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
            xhr.timeout = 65000; xhr.ontimeout = () => reject(new Error('File upload timed out. Retry the same plan to reconcile saved files.'));
            xhr.onerror = () => reject(new Error('File upload interrupted. Your saved tasks remain available. Retry when connected.'));
            xhr.onload = () => { try { const result = JSON.parse(xhr.responseText); xhr.status >= 200 && xhr.status < 300 ? resolve(result) : reject(new Error(result.message || ('Task upload failed (' + xhr.status + ').'))); } catch { reject(new Error('The task service returned an unreadable response.')); } };
            xhr.send(form);
        });
    }
    async function loadTaskFiles(taskId) {
        const host=by('companyTaskAttachments');host.textContent='Loading authorized files…';
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
    function dateLabel(value) { const date = new Date(value); return isNaN(date.getTime()) ? (value || 'Not set') : date.toLocaleString(); }
    function additionalFilters(task) { return !task.archived || by('statusFilter').value === 'archived'; }

    function companyCard(task,board){const source=state.tasks.find(t=>t.id===task.id)||task,exam=examStates.get(task.id)||source.taskType==='exam',due=Date.parse(source.deadline||task.dueDate),left=due-Date.now()-serverOffset;return '<article class="task-card'+(board?' task-board-card':'')+'" data-status="'+escapeHtml(task.status)+'"'+(board?' draggable="'+!!boardState?.canMove+'" data-board-task-id="'+escapeHtml(task.id)+'"':'')+'><div class="task-board-card-head"><h3>'+escapeHtml(task.title)+'</h3><span class="status-badge">'+escapeHtml(statusLabel(task.status))+'</span></div><p class="task-desc">'+escapeHtml(source.description)+'</p><p><span class="task-avatar">'+escapeHtml((source.studentName||'Student').split(' ').map(n=>n[0]).slice(0,2).join(''))+'</span> '+escapeHtml(source.studentName||task.studentId)+'</p><div class="task-card-dates"><span>Start: '+escapeHtml(dateLabel(source.startsAt||source.createdAt))+'</span><span>Due: '+escapeHtml(dateLabel(source.deadline||task.dueDate))+'</span><span class="'+(left<0?'task-overdue':'')+'">'+(task.status==='completed'?'Completed':task.status==='submitted'?'Submitted for review':Number.isFinite(left)?left<0?'Overdue':Math.ceil(left/3600000)+' hours remaining':'No deadline supplied')+'</span></div><span class="task-type-badge">'+(exam?'Exam':'Standard Task')+'</span>'+(task.status==='submitted'?'<p class="task-review-notice">Submission ready for supervisor review</p>':'')+(task.status==='completed'?'<p class="task-approved-notice">Approved</p>':'')+(source.attachmentIds?.length?'<small>Resources attached</small>':'')+'<div class="task-action"><button type="button" class="btn btn-primary" data-task-action="review" data-task-id="'+escapeHtml(task.id)+'">'+(task.status==='submitted'?'Review submission':'View Details')+'</button></div></article>';}

    function render() {
        const nameFilter=by('taskSearch'),selectedTitle=nameFilter.value;nameFilter.replaceChildren(new Option('All Tasks',''));[...new Set(state.tasks.map(t=>t.title))].sort().forEach(t=>nameFilter.add(new Option(t,t.toLowerCase())));nameFilter.value=selectedTitle;
        by('heroTaskCount').textContent=state.tasks.filter(t=>t.status!=='completed'&&!t.archived).length;by('heroExamCount').textContent=examStates.size;

        const query = (by('taskSearch')?.value || '').trim().toLowerCase();
        const filter = by('statusFilter')?.value || '';
        const tasks = state.tasks.filter(task => additionalFilters(task) && (!query || task.title.toLowerCase() === query) && (!filter || filter === 'all' || task.status === filter));
        const list = by('taskList'); list.querySelectorAll('.task-card').forEach(card => card.remove());
        const markup=tasks.map(task=>companyCard(task,false)).join('');
        if (markup) by('noTasksMsg').insertAdjacentHTML('beforebegin', markup);
        const empty = by('noTasksMsg');
        if (empty) {
            empty.style.display = tasks.length ? 'none' : 'block';
            empty.querySelector('h4').textContent = state.tasks.length ? 'No tasks match these filters' : 'No tasks assigned yet';
            empty.querySelector('p').textContent = state.tasks.length ? 'Clear the search or choose All Statuses to see tasks.' : 'New assignments from your supervisor will appear here.';
        }
        by('taskCount').textContent = tasks.length + ' tasks';
        document.querySelectorAll('.stat-details h3').forEach((el, i) => { el.textContent = [state.tasks.length,state.tasks.filter(t=>t.status==='assigned').length,state.tasks.filter(t=>['inprogress','revision'].includes(t.status)).length,state.tasks.filter(t=>t.status==='submitted').length,state.tasks.filter(t=>t.status==='completed').length][i] ?? 0; });
        const select = by('newTaskStudent'); const previous = select.value;
        select.innerHTML = '<option value="">' + (state.students.length ? 'Choose trainee' : 'No active trainee placements loaded from Firebase') + '</option>' + state.students.map(student => '<option value="' + escapeHtml(student.placementId) + '">' + escapeHtml(student.name) + ' (' + escapeHtml(student.id) + ')</option>').join('');
        if (state.students.some(student => student.placementId === previous)) select.value = previous;
        else if (state.students.length === 1) select.value = state.students[0].placementId;
        renderStudents();
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
        if (!boardState || !boardState.columns.length) { TaskDialogs.renderBoardShell(host, { loading: loadingTasks, error: boardLoadError }); return; }
        const query = (by('taskSearch')?.value || '').trim().toLowerCase(), filter = by('statusFilter')?.value || '';
        const selected = state.students.find(student => student.placementId === boardState.placementId);
        const tasks = state.tasks.filter(task => additionalFilters(task) && (!query || task.title.toLowerCase() === query) && (!filter || filter === 'all' || task.status === filter)).map(task => ({
            ...task, order: boardState.tasks.find(card => card.id === task.id)?.order || 0,
            columnId: ({assigned:'todo',inprogress:'inProgress',revision:'inProgress',submitted:'review',completed:'done',archived:'done'})[task.status] || 'todo'
        }));
        host.innerHTML = '<div class="task-board-columns">' + boardState.columns.map(column => {
            const cards = tasks.filter(task => task.columnId === column.id).sort((a, b) => a.order - b.order || a.id.localeCompare(b.id));
            return '<section class="task-board-column" data-column-id="' + escapeHtml(column.id) + '"><header><h5>' + escapeHtml(column.title) + '</h5><span>' + cards.length + '</span></header><div class="task-board-dropzone">' + cards.map(task=>companyCard({...state.tasks.find(t=>t.id===task.id),...task},true)).join('') + (cards.length ? '' : '<div class="task-board-column-empty">Drop a task here</div>') + '</div></section>';
        }).join('') + '</div>';
        if (!boardState.canMove) host.insertAdjacentHTML('afterbegin', '<p class="task-board-help">This board is read-only for your role.</p>');
        host.querySelectorAll('[data-board-task-id]').forEach(card => { const exam = examStates.get(card.dataset.boardTaskId); if (exam) { const badge = document.createElement('small'); badge.textContent = 'Exam · ' + (exam.reviewState || exam.status); card.append(badge); } const task = state.tasks.find(t => t.id === card.dataset.boardTaskId); if (task?.extension?.status === 'pending') { const badge = document.createElement('p'); badge.className = 'task-overdue'; badge.textContent = 'Extension request awaiting decision'; card.append(badge); } });
        if (boardState.canMove) host.querySelectorAll('[data-board-task-id]').forEach(card => {
            const task = state.tasks.find(item => item.id === card.dataset.boardTaskId); if (!task || task.archived) { card.draggable = false; return; }
            const controls = document.createElement('div'); controls.className = 'card-move-controls'; const select = document.createElement('select'); select.setAttribute('aria-label', 'Destination column for ' + task.title);
            boardState.columns.forEach(column => {const option=new Option(column.title,column.id);option.disabled=column.id!==card.closest('[data-column-id]').dataset.columnId;select.add(option);}); select.value = card.closest('[data-column-id]').dataset.columnId;
            const move = document.createElement('button'); move.type = 'button'; move.className = 'btn btn-light btn-sm'; move.textContent = 'Move';
            move.onclick = async () => { if (boardBusy || select.value === card.closest('[data-column-id]').dataset.columnId) return; if (!await TaskDialogs.confirm('Move this card?', 'Move ' + task.title + ' to ' + select.selectedOptions[0].text + '? Workflow status still follows the task actions.', 'Move card')) return; boardBusy = true; move.disabled = true; try { await request('move', { taskId: task.id, placementId: task.placementId, columnId: select.value, operationId: operationId() }); await refreshBoard(); } catch (error) { message(error.message, true); } finally { boardBusy = false; move.disabled = false; } };
            controls.append(select, move); move.hidden=true;select.hidden=true;
            for(const [label,delta]of [['Move up',-1],['Move down',1]]){const button=document.createElement('button');button.type='button';button.className='btn btn-light btn-sm';button.textContent=label;const siblings=[...card.parentElement.querySelectorAll('[data-board-task-id]')].filter(el=>state.tasks.find(t=>t.id===el.dataset.boardTaskId)?.placementId===task.placementId),index=siblings.indexOf(card);button.disabled=index+delta<0||index+delta>=siblings.length;button.onclick=async()=>{if(boardBusy)return;boardBusy=true;button.disabled=true;try{const before=delta<0?siblings[index-1]?.dataset.boardTaskId:siblings[index+2]?.dataset.boardTaskId;await request('move',{placementId:task.placementId,taskId:task.id,columnId:card.closest('[data-column-id]').dataset.columnId,beforeTaskId:before||null,operationId:operationId()});await refreshBoard();}catch(e){message(e.message,true);}finally{boardBusy=false;}};controls.append(button);}card.append(controls);
        });

        host.querySelectorAll('.task-board-column').forEach(column => { column.querySelector('.task-board-dropzone').scrollTop = scrollPositions.get(column.dataset.columnId) || 0; });
    }
    async function refreshBoard() {
        try {
            const placements = [...new Set([...state.students.map(student => student.placementId), ...state.tasks.map(task => task.placementId)].filter(Boolean))];
            const boards = await Promise.all(placements.map(placementId => request('board', { placementId }, 'GET')));
            boardState = { canMove: boards.every(board => board.canMove), columns: boards[0]?.columns || [{id:'todo'},{id:'inProgress'},{id:'review'},{id:'done'}], tasks: boards.flatMap(board => board.tasks.map(task => ({...task, placementId:board.placementId}))) };
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
    function selectReviewTab(){const isExam=state.tasks.find(task => task.id === selectedId)?.taskType === 'exam'||examStates.has(selectedId);document.querySelectorAll('[data-company-panel]').forEach(p=>{p.hidden=isExam?p.dataset.companyPanel!=='exam':p.dataset.companyPanel==='exam';});by('approveTaskBtn').hidden=isExam;by('requestRevisionBtn').hidden=isExam;}
    by('reviewModal').addEventListener('hide.bs.modal', event => { if (allowReviewClose || busy || JSON.stringify([by('gradeInput').value, by('feedbackInput').value]) === reviewBaseline) return; event.preventDefault(); TaskDialogs.confirm('Discard unsaved review?', 'Your unsaved grade and feedback will be discarded.', 'Discard review').then(confirmed => { if (confirmed) { allowReviewClose = true; modal('reviewModal').hide(); } }); });
    async function renderTaskExams(taskId) {
        const host = by('companyTaskExams'); host.textContent = 'Loading authorized examinations…';
        try { const response = await fetch('../TaskExamsApi.ashx?action=list', { cache: 'no-store', credentials: 'same-origin' }); const exams = await response.json(); if (!response.ok) throw Error(exams.message || 'Exam service unavailable.'); if (selectedId !== taskId) return; host.replaceChildren(); for (const exam of Object.values(exams).filter(item => item.taskId === taskId)) { const card = document.createElement('article'); card.className = 'exam-review-card'; const title = document.createElement('h6'); title.textContent = exam.draft?.title || exam.title || 'Task examination'; const button = document.createElement('button'); button.type = 'button'; button.className = 'btn btn-primary'; button.textContent = 'Review exam and result visibility'; button.disabled = exam.status !== 'published'; button.onclick = () => window.CompanyExamResults.open(exam.id); card.append(title, button); host.append(card); } if (!host.children.length) host.textContent = 'No published examination linked to this task.'; } catch (error) { host.textContent = error.message; }
    }
    let refreshingWorkspace;
    function refresh() { return refreshingWorkspace || (refreshingWorkspace = loadWorkspace().finally(() => { refreshingWorkspace = null; })); }
    async function loadExamStates() { try { const response = await fetch('../TaskExamsApi.ashx?action=list', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(10000) }); if (response.ok) examStates = new Map(Object.values(await response.json()).map(exam => [exam.taskId, exam])); } catch (_) { /* Task records remain usable during an exam service outage. */ } }
    async function loadWorkspace() {
        loadingTasks = true;
        try { state = await request(''); loadExamStates().then(() => { if (!busy && !boardBusy) render(); }); if (state.serverNow) serverOffset = Date.parse(state.serverNow) - Date.now(); boardLoadError = ''; loadingTasks = false; render(); connection('Connected to Firebase /tasks using your verified account.'); await refreshBoard(); loadRecoveryOperations(); if (by('reviewModal').classList.contains('show')) { const task = state.tasks.find(item => item.id === selectedId); if (task) { const canReview = task.status === 'submitted' && state.submissions.some(item => item.taskId === task.id); by('approveTaskBtn').disabled = !canReview; by('requestRevisionBtn').disabled = !canReview; by('gradeInput').disabled = !canReview; by('feedbackInput').disabled = !canReview; } } if (window.CompanyExamResults) await CompanyExamResults.refresh().catch(error => message(error.message, true)); return true; }
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
        if (busy || fileChecks) return;
        if (!creationPlan && window.TaskComposer && !TaskComposer.validate()) return;
        let firstInvalid = null;
        for (const id of ['newTaskTitle', 'newTaskDescription', 'newTaskDeadline', 'newTaskMaximum', 'newTaskCriteria']) {
            const field = by(id); const valid = field.checkValidity() && field.value.trim().length > 0;
            field.classList.toggle('is-invalid', !valid); field.setAttribute('aria-invalid', String(!valid));
            let error = by(id + 'Error'); if (!error) { error = document.createElement('span'); error.id = id + 'Error'; error.className = 'field-error'; field.after(error); field.setAttribute('aria-describedby', error.id); }
            error.textContent = valid ? '' : (field.validationMessage || 'This field is required.'); if (!valid && !firstInvalid) firstInvalid = field;
        }
        by('studentSelectionError').textContent = selectedPlacements.size ? '' : 'Select at least one authorized student.';
        if (firstInvalid || !selectedPlacements.size) { const field = firstInvalid || by('studentSearch'); field.scrollIntoView({ block: 'center' }); field.focus(); return; }
        const payload = creationPlan?.payload || { startsAt: new Date(by('newTaskStart').value).toISOString(), taskType: by('configureTaskExam').checked ? 'exam' : 'standard', title: by('newTaskTitle').value.trim(), description: by('newTaskDescription').value.trim(), deadline: new Date(by('newTaskDeadline').value).toISOString(), placementIds: [...selectedPlacements], maximum: Number(by('newTaskMaximum').value), criteria: by('newTaskCriteria').value.trim(), batchId: creationBatch || (creationBatch = operationId()) };
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
        if (!await TaskDialogs.confirm(creationPlan ? 'Resume incomplete creation?' : 'Create assignments?', 'Create ' + selectedPlacements.size + ' independent student task(s)?' + (by('configureTaskExam').checked ? ' The configured examination will be published for each student.' : ''), 'Create tasks')) return;
        busy = true; by('publishTaskBtn').disabled = true; by('publishTaskBtn').textContent = 'Saving tasks…'; by('createTaskError').hidden = true; window.TaskComposer?.setBusy(true);
        try {
            if (!creationPlan) {
                const files = [...by('createTaskResources').files], questionFiles = window.TaskComposer?.questionFiles() || [];
                const resources = await Promise.all(files.map((file,index) => describeFile(file,'resource-' + index)));
                const questionImages = await Promise.all(questionFiles.map(item => describeFile(item.file,item.questionId,item.questionId)));
                const persistedPlan = {payload,exam:window.TaskComposer?.snapshot() || null,resources,questionImages};
                creationPlan = {payload,files,questionFiles:new Map(questionFiles.map(item => [item.questionId,item.file])),persistedPlan,persisted:false};
                for (const id of payload.placementIds) creationProgress.set(id, {});
            }
            window.TaskComposer?.setRecovery(true); showCreationProgress();
            const operation = creationPlan.persisted ? await request('creationOperation',{batchId:payload.batchId},'GET') : await request('beginCreation',creationPlan.persistedPlan);
            applyCreationOperation(operation);
            const known = operation.progress.filter(row => row.taskSaved).map(row => ({success:true,id:row.taskId,placementId:row.placementId}));
            const missing = payload.placementIds.filter(id => !known.some(row => row.placementId === id));
            const result = missing.length ? await request('bulkCreate', {...creationPlan.payload,placementIds:missing}) : {success:true,results:[]}; result.results = [...known,...(result.results || [])];
            const completed = [], failures = [];
            for (const placementId of payload.placementIds) {
                const row = result.results?.find(r => r.placementId === placementId), progress = creationProgress.get(placementId);
                if (!row?.success || !row.id) { progress.task = 'Retry required: ' + (row?.message || 'No save acknowledgment'); failures.push(progress.task); continue; }
                completed.push(row); progress.task = 'Saved (' + row.id + ')';
                try {
                    for (const [index,manifest] of (creationPlan.persistedPlan.resources || []).entries()) {
                        if (progress.server?.resourceFiles?.[manifest.id]) continue;
                        const file = creationPlan.files[index];
                        if (!file) throw new Error('Reselect resource: ' + manifest.name);
                        if (completedResourceUploads.get(row.id)?.has(file)) continue;
                        await upload('attach', {taskId:row.id},file);
                        if (!completedResourceUploads.has(row.id)) completedResourceUploads.set(row.id,new Set());
                        completedResourceUploads.get(row.id).add(file);
                    }
                    progress.resources = creationPlan.files.length ? 'Saved' : 'None required';
                } catch (error) { progress.resources = 'Retry required: ' + error.message; failures.push(progress.resources); }
                showCreationProgress();
            }
            if (payload.taskType === 'exam') {
                if (!window.TaskComposer) throw new Error('The exam builder is unavailable. Saved assignments remain; reload only after recording their IDs.');
                const exams = await TaskComposer.createExams(completed, payload.batchId, (task, status) => { creationProgress.get(task.placementId).exam = status; showCreationProgress(); });
                for (const exam of exams) if (!exam.success) failures.push(exam.message);
            }
            if (failures.length || completed.length !== payload.placementIds.length) throw new Error('Creation is incomplete. Saved records were preserved. Review each student above, then retry this same plan. ' + failures.join(' '));
            const verified = await request('checkpointCreation',{batchId:payload.batchId}); applyCreationOperation(verified);
            if (verified.status !== 'complete') throw Error('The backend still reports unfinished task, file or exam steps.');
            creationComplete = true; createDirty = false; allowCreateClose = true; modal('createTaskModal').hide();
            message(completed.length + ' independent task(s)' + (payload.taskType === 'exam' ? ' and published examination(s)' : '') + ' saved to Firebase.', false);
            await refresh(); await loadRecoveryOperations(true);
        } catch (error) {
            if (creationPlan?.persisted) { try { applyCreationOperation(await request('checkpointCreation',{batchId:payload.batchId})); } catch (_) { /* Reload reconciliation remains available even if the checkpoint response is lost. */ } }
            by('createTaskError').hidden = false; by('createTaskError').textContent = (error.name === 'TimeoutError' ? 'The response timed out; some records may already be saved.' : error.message) + ' Retry this saved operation, or resume it after reopening the page. Unsaved files may need reselection.';
            showCreationProgress(); by('createTaskError').scrollIntoView({ block: 'center' });
        } finally { busy = false; window.TaskComposer?.setBusy(false); by('publishTaskBtn').disabled = creationComplete; by('publishTaskBtn').textContent = creationPlan && !creationComplete ? 'Retry incomplete creation' : 'Create task'; }
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
    by('taskBoard')?.addEventListener('dragstart', event => {
        const card = event.target.closest('[data-board-task-id]');
        if (!card || !boardState?.canMove) { event.preventDefault(); return; }
        draggedTaskId = card.dataset.boardTaskId; event.dataTransfer.effectAllowed = 'move'; event.dataTransfer.setData('text/plain', draggedTaskId);
    });
    // Native HTML dragging needs explicit edge scrolling inside overflow lanes.
    let dragScrollFrame = 0, dragPoint = null;
    function stopDragScroll() { cancelAnimationFrame(dragScrollFrame); dragScrollFrame = 0; dragPoint = null; }
    function scrollWhileDragging() {
        if (!draggedTaskId || !dragPoint) { stopDragScroll(); return; }
        const host = by('taskBoard'), zone = dragPoint.zone;
        const edgeSpeed = (position, start, end) => position < start + 48 ? -Math.ceil((start + 48 - position) / 5) : position > end - 48 ? Math.ceil((position - end + 48) / 5) : 0;
        if (zone?.isConnected) { const box = zone.getBoundingClientRect(); zone.scrollTop += edgeSpeed(dragPoint.y, box.top, box.bottom); }
        const box = host.getBoundingClientRect(); host.scrollLeft += edgeSpeed(dragPoint.x, box.left, box.right);
        dragScrollFrame = requestAnimationFrame(scrollWhileDragging);
    }
    by('taskBoard').addEventListener('dragover', event => {
        if (!draggedTaskId) return;
        dragPoint = { x: event.clientX, y: event.clientY, zone: event.target.closest('.task-board-dropzone') };
        if (!dragScrollFrame) dragScrollFrame = requestAnimationFrame(scrollWhileDragging);
    });
    by('taskBoard').addEventListener('dragleave', event => { if (!by('taskBoard').contains(event.relatedTarget)) stopDragScroll(); });
    by('taskBoard').addEventListener('dragend', () => { stopDragScroll(); draggedTaskId = null; });
    by('taskBoard').addEventListener('drop', stopDragScroll);
    window.addEventListener('blur', stopDragScroll);
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
        if(payload.columnId !== (({assigned:'todo',inprogress:'inProgress',revision:'inProgress',submitted:'review',completed:'done'})[movedTask?.status]||'todo')){message('Use the authorized task action to change its stage. Drag only reorders within a stage.',true);draggedTaskId=null;return;}
        boardBusy = true;
        try {
            try { await request('move', payload); }
            catch (error) { if (error instanceof TypeError) await request('move', payload); else throw error; }
            await refreshBoard(); message('Task position saved to Firebase. Task workflow status was not changed.', false);
        } catch (error) { message(error.message, true); await refreshBoard(); }
        finally { boardBusy = false; draggedTaskId = null; }
    });
    by('taskSearch')?.addEventListener('change', render); by('statusFilter')?.addEventListener('change', render);
    by('createTaskModal')?.addEventListener('show.bs.modal', refresh);
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
    window.setInterval(() => { if (!document.hidden) refresh(); }, 5000);
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(refresh);
    by('taskBoard').addEventListener('task-board-retry', refresh);
    by('taskList').hidden = true; document.querySelectorAll('.stat-details h3').forEach(el => el.textContent = '—'); renderBoard(); refresh();
})();
