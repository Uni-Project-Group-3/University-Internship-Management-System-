(function () {
    'use strict';
    const api = '../TaskExamsApi.ashx';
    const $ = id => document.getElementById(id);
    const notice = $('notice') || $('modalErrorContainer');
    let questions = [];
    const uid = () => (window.crypto && crypto.randomUUID ? crypto.randomUUID() : 'q-' + Date.now() + '-' + Math.random().toString(36).slice(2));
    let saving = false;
    let activeResultRefresh = null;
    window.CompanyExamResults = Object.freeze({ open: reviewResults, refresh: async () => { if (activeResultRefresh) await activeResultRefresh(); } });
    if (!$('examForm')) return;
    const params = new URLSearchParams(location.search);
    if (params.has('taskId')) { $('taskId').value = params.get('taskId'); $('placementId').value = params.get('placementId') || ''; }
    if (params.get('embedded') === '1') { document.querySelector('.exam-list').hidden = true; document.querySelector('.exam-header').hidden = true; document.querySelector('.exam-layout').style.display = 'block'; }
    const timezone = document.createElement('p'); timezone.className = 'muted'; timezone.textContent = 'All times: ' + Intl.DateTimeFormat().resolvedOptions().timeZone; $('opensAt').parentElement.before(timezone);
    function message(text, kind) { notice.textContent = text; notice.className = 'notice' + (kind ? ' ' + kind : ''); }
    async function call(action, data, method) {
        const split = action.indexOf('&'); const name = split < 0 ? action : action.slice(0, split); const query = split < 0 ? '' : '&' + action.slice(split + 1);
        const response = await fetch(api + '?action=' + encodeURIComponent(name) + query, {
            method: method || (data ? 'POST' : 'GET'), credentials: 'same-origin', cache: 'no-store',
            headers: data ? { 'Content-Type': 'application/json', 'Accept': 'application/json' } : { 'Accept': 'application/json' },
            body: data ? JSON.stringify(data) : undefined
        });
        const result = await response.json(); if (!response.ok) throw new Error(result.message || 'Exam request failed.'); return result;
    }
    function timeValue(id) { const value = $(id).value; return value ? new Date(value).toISOString() : ''; }
    function packDraft() {
        return { title: $('title').value.trim(), instructions: $('instructions').value.trim(), opensAt: timeValue('opensAt'), closesAt: timeValue('closesAt'), durationMinutes: Number($('duration').value), attemptLimit: Number($('attemptLimit').value),
            questions: questions.map(q => {
                const item = { id: q.id || uid(), type: q.type, prompt: q.prompt, points: Number(q.points), imageFileId: q.imageFileId || '' };
                if (q.type === 'multipleChoice') { item.options = q.options.split('\n').map((text, n) => ({ id: 'option-' + (n + 1), text: text.trim() })); item.correctOptionId = 'option-' + Number(q.answer || 1); }
                else item.correctAnswer = q.answer === 'true';
                return item;
            }) };
    }
    function renderQuestions() {
        const host = $('questions'); host.replaceChildren();
        questions.forEach((q, index) => {
            const card = document.createElement('article'); card.className = 'question';
            const head = document.createElement('div'); head.className = 'question-title';
            const title = document.createElement('h4'); title.textContent = 'Question ' + (index + 1); head.appendChild(title);
            const controls = document.createElement('div'); controls.className = 'question-controls';
            [['↑', -1], ['↓', 1]].forEach(([text, move]) => { const b = document.createElement('button'); b.type = 'button'; b.className = 'small-button'; b.textContent = text; b.setAttribute('aria-label', move < 0 ? 'Move question up' : 'Move question down'); b.disabled = index + move < 0 || index + move >= questions.length; b.onclick = () => { [questions[index], questions[index + move]] = [questions[index + move], questions[index]]; renderQuestions(); }; controls.appendChild(b); });
            const del = document.createElement('button'); del.type = 'button'; del.className = 'danger'; del.textContent = 'Delete'; del.onclick = async () => { if (await TaskDialogs.confirm('Remove question?', 'Remove question ' + (index + 1) + ' from this draft?', 'Remove question')) { questions.splice(index, 1); renderQuestions(); } }; controls.appendChild(del); head.appendChild(controls); card.appendChild(head);
            const type = document.createElement('select'); type.setAttribute('aria-label', 'Question type'); [['multipleChoice', 'Multiple choice'], ['trueFalse', 'True / false']].forEach(([v, t]) => { const o = document.createElement('option'); o.value = v; o.textContent = t; type.appendChild(o); }); type.value = q.type; type.onchange = () => { q.type = type.value; q.answer = q.type === 'trueFalse' ? 'true' : '1'; renderQuestions(); }; card.appendChild(type);
            const prompt = document.createElement('textarea'); prompt.required = true; prompt.setAttribute('aria-label', 'Question ' + (index + 1) + ' text'); prompt.rows = 2; prompt.maxLength = 4000; prompt.placeholder = 'Question prompt'; prompt.value = q.prompt; prompt.oninput = () => q.prompt = prompt.value; card.appendChild(prompt);
            const points = document.createElement('input'); points.required = true; points.type = 'number'; points.min = '0.01'; points.max = '1000'; points.step = '0.01'; points.value = q.points; points.setAttribute('aria-label', 'Question points'); points.oninput = () => q.points = points.value; card.appendChild(points);
            if (q.type === 'multipleChoice') {
                const values = q.options.split('\n');
                values.forEach((value, n) => {
                    const row = document.createElement('div'); row.className = 'question-controls';
                    const correct = document.createElement('input'); correct.type = 'radio'; correct.name = 'correct-' + q.id; correct.checked = Number(q.answer) === n + 1; correct.setAttribute('aria-label', 'Option ' + (n + 1) + ' is correct'); correct.onchange = () => q.answer = String(n + 1);
                    const text = document.createElement('input'); text.required = true; text.maxLength = 1000; text.value = value; text.setAttribute('aria-label', 'Answer option ' + (n + 1)); text.oninput = () => { values[n] = text.value; q.options = values.join('\n'); };
                    row.append(correct, text);
                    [['↑', -1], ['↓', 1]].forEach(([label, delta]) => { const move = document.createElement('button'); move.type = 'button'; move.className = 'secondary'; move.textContent = label; move.setAttribute('aria-label', 'Move option ' + (n + 1) + (delta < 0 ? ' up' : ' down')); move.disabled = n + delta < 0 || n + delta >= values.length; move.onclick = () => { const selected = Number(q.answer) - 1; [values[n], values[n + delta]] = [values[n + delta], values[n]]; q.answer = String((selected === n ? n + delta : selected === n + delta ? n : selected) + 1); q.options = values.join('\n'); renderQuestions(); }; row.append(move); });
                    const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'danger'; remove.textContent = '×'; remove.setAttribute('aria-label', 'Remove option ' + (n + 1)); remove.disabled = values.length <= 2; remove.onclick = () => { const selected = Number(q.answer) - 1; values.splice(n, 1); q.answer = String(selected === n ? 1 : selected > n ? selected : selected + 1); q.options = values.join('\n'); renderQuestions(); }; row.append(remove); card.append(row);
                });
                const add = document.createElement('button'); add.type = 'button'; add.className = 'secondary'; add.textContent = 'Add answer option'; add.disabled = values.length >= 8; add.onclick = () => { q.options += '\n'; renderQuestions(); }; card.append(add);
            } else {
                const answer = document.createElement('select'); answer.setAttribute('aria-label', 'Correct true/false answer'); [['true', 'Correct answer: True'], ['false', 'Correct answer: False']].forEach(([v, t]) => { const o = document.createElement('option'); o.value = v; o.textContent = t; answer.appendChild(o); }); answer.value = q.answer; answer.onchange = () => q.answer = answer.value; card.appendChild(answer);
            }
            const upload = document.createElement('input'); upload.type = 'file'; upload.accept = '.jpg,.jpeg,.png,image/jpeg,image/png'; upload.setAttribute('aria-label', 'Optional question image');
            const uploadButton = document.createElement('button'); uploadButton.type = 'button'; uploadButton.className = 'secondary'; uploadButton.textContent = q.imageFileId ? 'Replace question image' : 'Upload question image';
            uploadButton.onclick = async () => {
                const file = upload.files[0]; if (!file) { message('Choose an image first.', 'error'); return; }
                if (!['image/png', 'image/jpeg'].includes(file.type) || file.size > 10 * 1024 * 1024) { message('Choose a PNG or JPEG image up to 10 MiB.', 'error'); return; }
                uploadButton.disabled = true;
                try { await save(); const form = new FormData(); form.append('file', file); form.append('examId', $('examId').value); form.append('questionId', q.id);
                    const data = await new Promise((resolve, reject) => { const xhr = new XMLHttpRequest(); xhr.open('POST', api + '?action=image'); xhr.withCredentials = true; xhr.upload.onprogress = event => { uploadButton.textContent = event.lengthComputable ? 'Uploading ' + Math.round(event.loaded * 100 / event.total) + '%' : 'Uploading…'; }; xhr.onerror = () => reject(new Error('Image upload interrupted. Retry when connected.')); xhr.onload = () => { try { const result = JSON.parse(xhr.responseText); xhr.status >= 200 && xhr.status < 300 ? resolve(result) : reject(new Error(result.message || 'Upload failed.')); } catch (_) { reject(new Error('Unreadable upload response.')); } }; xhr.send(form); });
                    q.imageFileId = data.id; await save(); message('Question image uploaded and linked to the draft.', 'success'); renderQuestions();
                } catch (e) { message(e.message, 'error'); } finally { uploadButton.disabled = false; }
            };
            const imageRow = document.createElement('div'); imageRow.className = 'question-controls'; imageRow.append(upload, uploadButton); if (q.imageFileId) { const img = document.createElement('img'); img.className = 'question-image'; img.src = api + '?action=image&fileId=' + encodeURIComponent(q.imageFileId); img.alt = 'Question image preview'; imageRow.appendChild(img); } card.appendChild(imageRow);
            if (q.imageFileId) { const removeImage = document.createElement('button'); removeImage.type = 'button'; removeImage.className = 'secondary'; removeImage.textContent = 'Remove image'; removeImage.onclick = () => { q.imageFileId = ''; renderQuestions(); }; card.append(removeImage); }
            const error = document.createElement('p'); error.className = 'field-error'; error.id = 'question-error-' + q.id; card.append(error);
            host.appendChild(card);
        });
    }
    function fields() { return { placementId: $('placementId').value.trim(), taskId: $('taskId').value.trim(), ...packDraft() }; }
    async function save() {
        if (saving) throw new Error('A draft save is already in progress.');
        if (!$('examForm').reportValidity()) throw new Error('Complete the highlighted exam settings.');
        let invalid = false;
        questions.forEach(q => { const values = q.options.split('\n').map(v => v.trim().toLowerCase()); const error = !q.prompt.trim() ? 'Enter question text.' : !(Number(q.points) > 0 && Number(q.points) <= 1000) ? 'Points must be from 0.01 to 1000.' : q.type === 'multipleChoice' && (values.length < 2 || values.length > 8 || values.some(v => !v) || new Set(values).size !== values.length) ? 'Provide 2–8 distinct non-empty answer options.' : ''; const host = $('question-error-' + q.id); if (host) host.textContent = error; invalid ||= !!error; });
        if (!questions.length || invalid) throw new Error(questions.length ? 'Correct the question errors below.' : 'Add at least one question.');
        if (new Date($('closesAt').value) <= new Date($('opensAt').value)) throw new Error('Closing time must be later than opening time.');
        saving = true;
        try {
        const payload = fields();
        if (!$('examId').value) {
            const created = await call('create', payload); $('examId').value = created.id; $('draftRevision').value = created.exam.draftRevision || 1;
            message('Draft saved in Firebase. Exam ID: ' + created.id, 'success'); await loadList(); return created;
        }
        const result = await call('save', { examId: $('examId').value, revision: $('draftRevision').value, draft: payload });
        $('draftRevision').value = result.draftRevision; message('Draft saved in Firebase.', 'success'); await loadList(); return result;
        } finally { saving = false; }
    }
    function loadDraft(exam) {
        const d = exam.draft || {}; $('examId').value = exam.id || ''; $('draftRevision').value = exam.draftRevision || 1; $('placementId').value = exam.placementId || ''; $('taskId').value = exam.taskId || '';
        $('title').value = d.title || ''; $('instructions').value = d.instructions || ''; $('duration').value = d.durationMinutes || 30; $('attemptLimit').value = d.attemptLimit || 1;
        const local = v => { if (!v) return ''; const date = new Date(v); date.setMinutes(date.getMinutes() - date.getTimezoneOffset()); return date.toISOString().slice(0, 16); };
        $('opensAt').value = local(d.opensAt); $('closesAt').value = local(d.closesAt);
        const qmap = d.questions || {}; questions = (d.questionOrder || Object.keys(qmap)).map(id => { const q = qmap[id] || {}; const options = q.options || []; const answer = q.type === 'trueFalse' ? String(q.correctAnswer) : String(Math.max(1, options.findIndex(o => o.id === q.correctOptionId) + 1)); return { id: q.id || id, type: q.type || 'multipleChoice', prompt: q.prompt || '', points: q.points || 1, options: options.map(o => o.text).join('\n') || 'Option 1\nOption 2', answer, imageFileId: q.imageFileId || '' }; });
        renderQuestions(); $('editorTitle').textContent = exam.status === 'published' ? 'Published exam (read-only)' : 'Edit exam draft';
        const locked = exam.status === 'published'; $('examForm').querySelectorAll('input,textarea,select,button').forEach(el => { if (el.id !== 'examId' && el.id !== 'draftRevision') el.disabled = locked; }); $('publishExam').disabled = locked; $('saveDraft').disabled = locked;
    }
    async function loadList() {
        try {
            const items = await call('list'); const host = $('examList'); host.replaceChildren(); const entries = Object.entries(items || {});
            if (notice.textContent.startsWith('Loading')) message('Connected to Firebase. Showing your authorized examinations.', 'success');
            if (!entries.length) { const p = document.createElement('p'); p.className = 'muted'; p.textContent = 'No authorized exams are available.'; host.appendChild(p); return; }
            entries.forEach(([id, exam]) => {
                const card = document.createElement('article'); card.className = 'exam-card';
                const heading = document.createElement('h3'); heading.textContent = exam.draft?.title || exam.title || id;
                const status = document.createElement('p'); status.textContent = (exam.status || 'draft') + ' · Task ' + (exam.taskId || '') + (exam.reviewState ? ' · ' + exam.reviewState + (exam.pendingReviewCount ? ' (' + exam.pendingReviewCount + ')' : '') : '');
                const edit = document.createElement('button'); edit.type = 'button'; edit.className = 'secondary'; edit.textContent = exam.status === 'published' ? 'View exam' : 'Edit draft';
                edit.onclick = async () => { try { const detail = await call('exam&examId=' + encodeURIComponent(id)); loadDraft(detail); window.scrollTo({ top: 0, behavior: 'smooth' }); } catch (e) { message(e.message, 'error'); } };
                card.append(heading, status, edit);
                if (exam.status === 'published') { const results = document.createElement('button'); results.type = 'button'; results.className = 'primary'; results.textContent = 'Review and release results'; results.onclick = () => reviewResults(id); card.append(results); }
                host.append(card);
            });
        } catch (e) { message(e.message, 'error'); }
    }
    $('newExam').onclick = async () => { if (($('title').value.trim() || questions.length) && !await TaskDialogs.confirm('Start a new exam?', 'Unsaved changes in the current editor will be discarded. Saved Firebase drafts remain available.', 'Start new exam')) return; $('examForm').reset(); $('examId').value = ''; $('draftRevision').value = '0'; $('taskId').value = params.get('taskId') || ''; $('placementId').value = params.get('placementId') || ''; questions = []; renderQuestions(); $('editorTitle').textContent = 'Create exam draft'; $('examForm').querySelectorAll('input,textarea,select,button').forEach(el => el.disabled = false); };
    $('addQuestion').onclick = () => { questions.push({ id: uid(), type: 'multipleChoice', prompt: '', points: 1, options: 'Option 1\nOption 2', answer: '1', imageFileId: '' }); renderQuestions(); };
    $('saveDraft').onclick = async () => { try { await save(); } catch (e) { message(e.message, 'error'); } };
    $('publishExam').onclick = async () => { if (!await TaskDialogs.confirm('Publish examination?', 'Publish this immutable exam version for the assigned student?', 'Publish exam')) return; $('publishExam').disabled = true; try { await save(); const result = await call('publish', { examId: $('examId').value }); message('Exam version ' + result.version + ' is published in Firebase.', 'success'); loadDraft({ id: $('examId').value, placementId: $('placementId').value, taskId: $('taskId').value, status: 'published', publishedVersion: result.version, draft: packDraft() }); await loadList(); } catch (e) { message(e.message, 'error'); $('publishExam').disabled = false; } };
    $('examForm').addEventListener('submit', e => e.preventDefault());
    renderQuestions(); loadList();
    TaskDialogs.watch(() => Promise.all([loadList(), activeResultRefresh ? activeResultRefresh() : Promise.resolve()]), () => !saving);
    async function reviewResults(examId) {
        try {
            const body = document.createElement('div');
            let refreshing = false;
            async function renderResults() {
            if (refreshing) return; refreshing = true;
            try {
            const [attempts, definition] = await Promise.all([call('attempts&examId=' + encodeURIComponent(examId)), call('exam&examId=' + encodeURIComponent(examId))]);
            const drafts = new Map([...body.querySelectorAll('[data-review-attempt]')].map(card => [card.dataset.reviewAttempt, card.querySelector('textarea').value])); body.replaceChildren();
            for (const student of Object.values(attempts || {})) for (const attempt of Object.values(student || {})) {
                if (!['submitted', 'expired'].includes(attempt.status)) continue;
                const card = document.createElement('article'), title = document.createElement('h3'); title.textContent = attempt.studentId + ' · ' + attempt.score + ' / ' + attempt.maximumScore;
                const answers = document.createElement('pre'); answers.style.whiteSpace = 'pre-wrap'; answers.textContent = Object.entries(attempt.answerKey || {}).map(([id, key]) => { const question = definition.versions?.[attempt.version]?.questions?.[id] || definition.draft?.questions?.[id]; const label = value => question?.options?.find(option => option.id === value)?.text || String(value ?? 'Unanswered'); return (question?.prompt || id) + '\nStudent: ' + label(attempt.answers?.[id]) + ' · Correct: ' + label(key.correctOptionId ?? key.correctAnswer); }).join('\n\n');
                const feedback = document.createElement('textarea'); feedback.maxLength = 3000; feedback.placeholder = 'Optional feedback to student'; feedback.setAttribute('aria-label', 'Result feedback'); feedback.value = drafts.get(attempt.id) ?? attempt.feedback ?? ''; card.dataset.reviewAttempt = attempt.id;
                const status = document.createElement('p'); status.className = 'exam-review-state'; status.textContent = attempt.resultStatus === 'released' ? 'Completed · Result visible to student' : attempt.resultStatus === 'approvedHidden' ? 'Completed · Result hidden' : attempt.resultStatus === 'revisionRequested' ? 'Not complete · New attempt requested' : 'Awaiting supervisor review · Result hidden';
                const release = document.createElement('button'); release.className = 'primary'; release.type = 'button'; release.textContent = attempt.resultStatus === 'released' ? 'Result released' : attempt.resultStatus === 'approvedHidden' ? 'Show result' : 'Approve and release result'; release.disabled = ['released', 'revisionRequested'].includes(attempt.resultStatus);
                const hide = document.createElement('button'); hide.type = 'button'; hide.className = 'secondary'; hide.textContent = 'Hide result'; hide.disabled = attempt.resultStatus !== 'released';
                release.onclick = async () => { if (!await TaskDialogs.confirm('Release examination result?', 'The student will see the original calculated score and your feedback. This action does not change the grade.', 'Approve and release')) return; release.disabled = true; try { await call('release', { examId, studentId: attempt.studentId, attemptId: attempt.id, feedback: feedback.value }); release.textContent = 'Result released'; feedback.disabled = true; hide.disabled = false; status.textContent = 'Completed · Result visible to student'; window.dispatchEvent(new Event('task-data-changed')); } catch (e) { release.disabled = false; message(e.message, 'error'); } };
                hide.onclick = async () => { if (!await TaskDialogs.confirm('Hide examination result?', 'The calculated score and review history will be preserved. The student will no longer receive the score.', 'Hide result')) return; hide.disabled = true; try { await call('hideResult', { examId, studentId: attempt.studentId, attemptId: attempt.id }); release.disabled = false; release.textContent = 'Show result'; status.textContent = 'Completed · Result hidden'; window.dispatchEvent(new Event('task-data-changed')); } catch (e) { hide.disabled = false; message(e.message, 'error'); } };
                const revision = document.createElement('button'); revision.type = 'button'; revision.className = 'secondary'; revision.textContent = 'Request new attempt'; revision.disabled = attempt.resultStatus === 'revisionRequested';
                revision.onclick = async () => {
                    const form = document.createElement('div'); form.innerHTML = '<label>Reason for returning the exam<textarea name="reason" required maxlength="3000"></textarea></label><label>New deadline<input name="deadline" type="datetime-local" required></label><p>A new independent attempt will be allowed. Previous answers and grades remain in the history.</p>';
                    const dueControl = form.querySelector('input');
                    dueControl.oninput = () => dueControl.setCustomValidity('');
                    if (!await TaskDialogs.open('Request examination revision', form, { confirm: 'Review request', validate: () => {
                        dueControl.setCustomValidity(new Date(dueControl.value).getTime() > Date.now() ? '' : 'Choose a new deadline in the future.');
                        return dueControl.checkValidity();
                    } })) return;
                    if (!await TaskDialogs.confirm('Return exam for a new attempt?', 'The result will be hidden and the task marked Not complete until a new submission is reviewed.', 'Request revision')) return;
                    revision.disabled = true;
                    try { await call('revision', { examId, studentId: attempt.studentId, attemptId: attempt.id, reason: form.querySelector('textarea').value, deadline: new Date(form.querySelector('input').value).toISOString() }); release.disabled = true; hide.disabled = true; status.textContent = 'Not complete · New attempt requested'; window.dispatchEvent(new Event('task-data-changed')); }
                    catch (e) { revision.disabled = false; message(e.message, 'error'); }
                };
                card.append(title, status, answers, feedback, release, hide, revision); card.className = 'exam-review-card'; body.append(card);
            }
            if (!body.children.length) body.textContent = 'No submitted examination attempts.';
            } finally { refreshing = false; }
            }
            await renderResults();
            const update = async () => { if (body.isConnected && document.querySelectorAll('dialog[open]').length === 1 && document.activeElement?.tagName !== 'TEXTAREA') await renderResults(); };
            activeResultRefresh = update;
            try { await TaskDialogs.open('Review examination results', body, { cancel: 'Close', confirm: 'Done' }); } finally { if (activeResultRefresh === update) activeResultRefresh = null; }
        } catch (e) { message(e.message, 'error'); }
    }
}());
