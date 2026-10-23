(function () {
    'use strict';
    const by = id => document.getElementById(id), modal = by('createTaskModal');
    const sections = [...modal.querySelectorAll('.task-compose-section')];
    const titles = ['Task details', 'Assign students', 'Exam (optional)', 'Resources', 'Review'];
    const panels = [sections[0], sections[1], sections[3], sections[2]];
    panels[2].querySelector('h6').firstChild.textContent = '03 ';
    panels[3].querySelector('h6').firstChild.textContent = '04 ';
    const review = document.createElement('section'); review.className = 'task-compose-section'; panels.push(review);
    const grid = modal.querySelector('.task-compose-grid'); panels.forEach(p => grid.append(p));
    const tabs = document.createElement('nav'); tabs.className = 'compose-tabs'; tabs.setAttribute('aria-label', 'Task creation steps');
    const footer = modal.querySelector('.modal-footer');
    const back = document.createElement('button'), next = document.createElement('button');
    back.type = next.type = 'button'; back.className = 'btn btn-light'; next.className = 'btn custom-btn-primary'; back.textContent = 'Back'; next.textContent = 'Next →';
    footer.insertBefore(back, by('publishTaskBtn')); footer.append(next);
    modal.querySelector('.modal-body').prepend(tabs);
    let step = 0, questions = [], previewUrls = [], saving = false;
    const questionId = () => 'Q-' + crypto.randomUUID();
    function go(index) {
        step = index; panels.forEach((p, n) => p.hidden = n !== step);
        [...tabs.children].forEach((b, n) => { b.classList.toggle('active', n === step); b.setAttribute('aria-current', n === step ? 'step' : 'false'); });
        back.hidden = !step; next.hidden = step === 4; by('publishTaskBtn').hidden = step !== 4;
        if (step === 4) showReview(); modal.querySelector('.modal-body').scrollTop = 0;
    }
    titles.forEach((title, index) => { const b = document.createElement('button'); b.type = 'button'; b.textContent = title; b.onclick = () => { if (!saving && (index <= step || validateThrough(index - 1))) go(index); }; tabs.append(b); });
    back.onclick = () => { if (!saving) go(step - 1); };
    next.onclick = () => { if (!saving && validateThrough(step)) go(step + 1); };
    const exam = document.createElement('div'); exam.id = 'composerExamFields'; exam.hidden = true;
    exam.innerHTML = '<div class="compose-exam-settings"><label>Exam title *<input id="composeExamTitle" maxlength="180" class="form-control" /></label><label>Exam instructions<textarea id="composeExamInstructions" maxlength="4000" class="form-control" rows="2"></textarea></label><div class="row"><label class="col-md-6">Opening date &amp; time *<input id="composeExamOpens" type="datetime-local" class="form-control" /></label><label class="col-md-6">Closing date &amp; time *<input id="composeExamCloses" type="datetime-local" class="form-control" /></label></div><small id="composeExamZone"></small><div class="row"><label class="col-md-6">Duration (minutes) *<input id="composeExamDuration" type="number" min="1" max="600" value="30" class="form-control" /></label><label class="col-md-6">Allowed attempts *<input id="composeExamAttempts" type="number" min="1" max="10" value="1" class="form-control" /></label></div></div><div class="compose-question-toolbar"><h6>Questions</h6><button type="button" id="composeAddQuestion" class="btn btn-light">+ Add question</button></div><div id="composeQuestions"></div><p id="composeExamError" class="field-error" role="alert"></p>';
    panels[2].append(exam); by('composeExamZone').textContent = 'Timezone: ' + Intl.DateTimeFormat().resolvedOptions().timeZone;
    panels[2].querySelector('.task-compose-exam small').textContent = 'Configure the complete exam here. Each student receives an independent examination and attempt history.';
    by('configureTaskExam').addEventListener('change', () => { exam.hidden = !by('configureTaskExam').checked; if (!by('composeExamTitle').value) by('composeExamTitle').value = by('newTaskTitle').value; });
    function control(tag, label, value, change) { const wrap = document.createElement('label'), el = document.createElement(tag); wrap.textContent = label; el.className = 'form-control'; el.value = value; el.setAttribute('aria-label', label); el.oninput = () => change(el.value); wrap.append(el); return { wrap, el }; }
    function button(text, fn, danger) { const b = document.createElement('button'); b.type = 'button'; b.className = 'btn btn-sm ' + (danger ? 'btn-outline-danger' : 'btn-light'); b.textContent = text; b.onclick = fn; return b; }
    function changed() { modal.dispatchEvent(new Event('input', { bubbles: true })); }
    function renderQuestions() {
        by('composeAddQuestion').disabled = questions.length >= 100;
        previewUrls.forEach(URL.revokeObjectURL); previewUrls = [];
        const host = by('composeQuestions'); host.replaceChildren();
        questions.forEach((q, index) => {
            const card = document.createElement('article'); card.className = 'compose-question';
            const heading = document.createElement('div'); heading.className = 'compose-question-toolbar';
            const name = document.createElement('strong'); name.textContent = 'Question ' + (index + 1); heading.append(name);
            for (const delta of [-1, 1]) { const b = button(delta < 0 ? '↑' : '↓', () => { [questions[index], questions[index + delta]] = [questions[index + delta], questions[index]]; changed(); renderQuestions(); }); b.disabled = index + delta < 0 || index + delta >= questions.length; b.setAttribute('aria-label', delta < 0 ? 'Move question up' : 'Move question down'); heading.append(b); }
            heading.append(button('Remove', async () => { if (await TaskDialogs.confirm('Remove question?', 'Remove this question from the unsaved exam?', 'Remove')) { questions.splice(index, 1); changed(); renderQuestions(); } }, true)); card.append(heading);
            const type = control('select', 'Answer format', '', value => { q.type = value; q.answer = value === 'trueFalse' ? 'true' : 0; changed(); renderQuestions(); });
            for (const [value, text] of [['multipleChoice', 'Multiple choice'], ['trueFalse', 'True / False']]) type.el.add(new Option(text, value)); type.el.value = q.type; card.append(type.wrap);
            const prompt = control('textarea', 'Question text *', q.prompt, v => q.prompt = v); prompt.el.maxLength = 4000; prompt.el.rows = 2; card.append(prompt.wrap);
            const points = control('input', 'Question points *', q.points, v => q.points = Number(v)); points.el.type = 'number'; points.el.min = '.01'; points.el.max = '1000'; points.el.step = '.01'; card.append(points.wrap);
            if (q.type === 'multipleChoice') {
                q.options.forEach((value, n) => {
                    const row = document.createElement('div'); row.className = 'compose-option';
                    const radio = document.createElement('input'); radio.type = 'radio'; radio.name = 'answer-' + q.id; radio.checked = q.answer === n; radio.setAttribute('aria-label', 'Option ' + (n + 1) + ' is correct'); radio.onchange = () => { q.answer = n; changed(); };
                    const text = control('input', 'Answer option ' + (n + 1), value, v => q.options[n] = v); text.el.maxLength = 1000; row.append(radio, text.wrap);
                    for (const delta of [-1, 1]) { const b = button(delta < 0 ? '↑' : '↓', () => { [q.options[n], q.options[n + delta]] = [q.options[n + delta], q.options[n]]; if (q.answer === n) q.answer += delta; else if (q.answer === n + delta) q.answer = n; changed(); renderQuestions(); }); b.disabled = n + delta < 0 || n + delta >= q.options.length; b.setAttribute('aria-label', 'Move option ' + (n + 1) + (delta < 0 ? ' up' : ' down')); row.append(b); }
                    const remove = button('×', () => { q.options.splice(n, 1); q.answer = q.answer === n ? 0 : q.answer > n ? q.answer - 1 : q.answer; changed(); renderQuestions(); }, true); remove.disabled = q.options.length <= 2; remove.setAttribute('aria-label', 'Remove option ' + (n + 1)); row.append(remove); card.append(row);
                });
                const add = button('+ Add option', () => { q.options.push(''); changed(); renderQuestions(); }); add.disabled = q.options.length >= 8; card.append(add);
            } else { const answer = control('select', 'Correct answer', '', v => q.answer = v); answer.el.add(new Option('True', 'true')); answer.el.add(new Option('False', 'false')); answer.el.value = q.answer; card.append(answer.wrap); }
            const imageLabel = document.createElement('label'); imageLabel.textContent = 'Question image (optional, PNG / JPEG, up to 10 MiB)';
            const image = document.createElement('input'); image.type = 'file'; image.accept = 'image/png,image/jpeg'; image.className = 'form-control'; image.setAttribute('aria-label', 'Question image');
            const error = document.createElement('p'); error.className = 'field-error'; error.id = 'composer-error-' + q.id;
            image.onchange = () => { const file = image.files[0]; if (!file) return; if (!['image/png', 'image/jpeg'].includes(file.type) || !file.size || file.size > 10 * 1024 * 1024) { error.textContent = 'Choose a non-empty PNG or JPEG up to 10 MiB.'; image.value = ''; return; } q.file = file; changed(); renderQuestions(); }; imageLabel.append(image); card.append(imageLabel);
            if (q.file) { const preview = document.createElement('img'); preview.src = URL.createObjectURL(q.file); previewUrls.push(preview.src); preview.alt = 'Question ' + (index + 1) + ' image preview'; preview.className = 'compose-image-preview'; card.append(preview, button('Remove image', () => { q.file = null; changed(); renderQuestions(); }, true)); }
            card.append(error); host.append(card);
        });
        if (!questions.length) host.textContent = 'Add your first question to start building the examination.';
    }
    by('composeAddQuestion').onclick = () => { questions.push({ id: questionId(), type: 'multipleChoice', prompt: '', points: 1, options: ['', ''], answer: 0, file: null }); changed(); renderQuestions(); };
    function fieldValid(id, message) { const field = by(id), valid = field.value.trim() && field.checkValidity(); field.classList.toggle('is-invalid', !valid); field.setAttribute('aria-invalid', String(!valid)); let error = by(id + 'Error'); if (!error) { error = document.createElement('span'); error.id = id + 'Error'; error.className = 'field-error'; field.after(error); field.setAttribute('aria-describedby', error.id); } error.textContent = valid ? '' : message; if (!valid) field.focus(); return !!valid; }
    function validateExam() {
        if (!by('configureTaskExam').checked) return true;
        let valid = true;
        for (const id of ['composeExamTitle', 'composeExamOpens', 'composeExamCloses', 'composeExamDuration', 'composeExamAttempts']) valid = fieldValid(id, 'Enter a valid value for this setting.') && valid;
        let message = '';
        if (new Date(by('composeExamCloses').value) <= new Date(by('composeExamOpens').value)) { message = 'Closing time must be later than opening time.'; valid = false; }
        if (!questions.length) { message = 'Add at least one question.'; valid = false; }
        questions.forEach(q => { const options = q.options.map(s => s.trim().toLowerCase()); const error = !q.prompt.trim() ? 'Enter question text.' : !(q.points > 0 && q.points <= 1000) ? 'Points must be from 0.01 to 1000.' : q.type === 'multipleChoice' && (options.length < 2 || options.some(s => !s) || new Set(options).size !== options.length) ? 'Enter at least two distinct, non-empty answer options.' : ''; by('composer-error-' + q.id).textContent = error; valid = !error && valid; });
        by('composeExamError').textContent = message; return valid;
    }
    function validateThrough(index) {
        if (index >= 0) { let valid = true; for (const id of ['newTaskTitle', 'newTaskDescription', 'newTaskCriteria', 'newTaskDeadline', 'newTaskMaximum']) valid = fieldValid(id, 'Complete this field with a valid value.') && valid; if (!valid) { go(0); return false; } }
        if (index >= 1 && !by('studentChips').children.length) { by('studentSelectionError').textContent = 'Select at least one authorized student.'; go(1); by('studentSearch').focus(); return false; }
        if (index >= 2 && !validateExam()) { go(2); return false; }
        if (index >= 3) for (const file of by('createTaskResources').files) { if (!file.size || file.size > 10 * 1024 * 1024 || !/\.(pdf|docx|xlsx|pptx|txt|png|jpe?g)$/i.test(file.name)) { by('createTaskError').hidden = false; by('createTaskError').textContent = 'Choose supported, non-empty resources up to 10 MiB each.'; go(3); return false; } }
        return true;
    }
    function showReview() {
        review.replaceChildren(); const h = document.createElement('h6'); h.textContent = 'Review assignment'; review.append(h);
        const items = [['Title', by('newTaskTitle').value], ['Deadline', by('newTaskDeadline').value.replace('T', ' ')], ['Maximum grade', by('newTaskMaximum').value], ['Students', [...by('studentChips').children].map(c => c.textContent.replace(' ×', '')).join(', ')], ['Resources', [...by('createTaskResources').files].map(f => f.name).join(', ') || 'None'], ['Exam', by('configureTaskExam').checked ? questions.length + ' questions · ' + by('composeExamDuration').value + ' minutes · ' + questions.reduce((sum, q) => sum + q.points, 0) + ' points' : 'None']];
        for (const [label, text] of items) { const p = document.createElement('p'), strong = document.createElement('strong'); strong.textContent = label + ': '; p.append(strong, document.createTextNode(text)); review.append(p); }
        questions.forEach((q, i) => { const p = document.createElement('p'); p.textContent = (i + 1) + '. ' + q.prompt + ' · ' + q.points + ' points'; review.append(p); });
    }
    function draft(images) { return { title: by('composeExamTitle').value.trim(), instructions: by('composeExamInstructions').value.trim(), opensAt: new Date(by('composeExamOpens').value).toISOString(), closesAt: new Date(by('composeExamCloses').value).toISOString(), durationMinutes: Number(by('composeExamDuration').value), attemptLimit: Number(by('composeExamAttempts').value), questions: questions.map(q => ({ id: q.id, type: q.type, prompt: q.prompt.trim(), points: q.points, imageFileId: images[q.id] || '', ...(q.type === 'multipleChoice' ? { options: q.options.map((text, i) => ({ id: 'option-' + (i + 1), text: text.trim() })), correctOptionId: 'option-' + (q.answer + 1) } : { correctAnswer: q.answer === 'true' }) })) }; }
    async function call(action, data) { const r = await fetch('../TaskExamsApi.ashx?action=' + action, { method: 'POST', credentials: 'same-origin', signal: AbortSignal.timeout(65000), headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }); const result = await r.json(); if (!r.ok) throw new Error(result.message || 'Exam operation failed.'); return result; }
    const saved = new Map();
    async function createExams(tasks, batchId) {
        if (!by('configureTaskExam').checked) return;
        for (const task of tasks) {
            by('publishTaskBtn').textContent = 'Saving examination…';
            let state = saved.get(task.id);
            const created = await call('create', { ...draft({}), taskId: task.id, placementId: task.placementId, creationKey: batchId });
            if (!state) state = { id: created.id, images: {} };
            state.revision = created.exam.draftRevision; state.published = created.exam.status === 'published';
            for (const [id, q] of Object.entries(created.exam.draft?.questions || {})) if (q.imageFileId) state.images[id] = q.imageFileId;
            saved.set(task.id, state);
            if (state.published) continue;
            for (const q of questions) if (q.file && !state.images[q.id]) {
                const form = new FormData(); form.append('examId', state.id); form.append('questionId', q.id); form.append('file', q.file);
                const metadata = await new Promise((resolve, reject) => { const xhr = new XMLHttpRequest(); xhr.open('POST', '../TaskExamsApi.ashx?action=image'); xhr.upload.onprogress = e => { by('publishTaskBtn').textContent = e.lengthComputable ? 'Uploading image ' + Math.round(e.loaded * 100 / e.total) + '%' : 'Uploading image…'; }; xhr.onerror = () => reject(new Error('Image upload interrupted. Retry to continue.')); xhr.onload = () => { try { const result = JSON.parse(xhr.responseText); xhr.status >= 200 && xhr.status < 300 ? resolve(result) : reject(new Error(result.message || 'Image upload failed.')); } catch { reject(new Error('Image upload returned an unreadable response.')); } }; xhr.send(form); }); state.images[q.id] = metadata.id;
            }
            const updated = await call('save', { examId: state.id, revision: state.revision, draft: draft(state.images) }); state.revision = updated.draftRevision;
            by('publishTaskBtn').textContent = 'Publishing examination…'; await call('publish', { examId: state.id }); state.published = true;
        }
    }
    async function checkAvailability() {
        const urls = [];
        if (by('configureTaskExam').checked) urls.push('../TaskExamsApi.ashx?action=list');
        if (by('createTaskResources').files.length || (by('configureTaskExam').checked && questions.some(q => q.file))) urls.push('../TasksApi.ashx?action=storageAvailability');
        for (const url of urls) { const response = await fetch(url, { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(65000) }); if (!response.ok) { const error = await response.json(); throw new Error(error.message || 'The required Firebase service is unavailable. Nothing new was created.'); } }
    }
    window.TaskComposer = { validate: () => validateThrough(3), checkAvailability, createExams, setBusy(value) { saving = value; tabs.querySelectorAll('button').forEach(b => b.disabled = value); back.disabled = next.disabled = value; panels.forEach(p => p.querySelectorAll('input,textarea,select,button').forEach(el => el.disabled = value)); }, reset() { saved.clear(); questions = []; previewUrls.forEach(URL.revokeObjectURL); previewUrls = []; by('composeExamTitle').value = ''; by('composeExamInstructions').value = ''; by('composeExamOpens').value = ''; by('composeExamCloses').value = ''; exam.hidden = true; renderQuestions(); go(0); } };
    modal.classList.add('task-compose-wizard'); modal.addEventListener('show.bs.modal', () => { if (!saving) go(0); });
    renderQuestions(); go(0);
}());
