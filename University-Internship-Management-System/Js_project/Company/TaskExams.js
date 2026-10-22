(function () {
    'use strict';
    const api = '../TaskExamsApi.ashx';
    const $ = id => document.getElementById(id);
    const notice = $('notice');
    let questions = [];
    const uid = () => (window.crypto && crypto.randomUUID ? crypto.randomUUID() : 'q-' + Date.now() + '-' + Math.random().toString(36).slice(2));
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
                if (q.type === 'multipleChoice') { item.options = q.options.split('\n').map((text, n) => ({ id: 'option-' + (n + 1), text: text.trim() })).filter(o => o.text); item.correctOptionId = 'option-' + Number(q.answer || 1); }
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
            const del = document.createElement('button'); del.type = 'button'; del.className = 'danger'; del.textContent = 'Delete'; del.onclick = () => { questions.splice(index, 1); renderQuestions(); }; controls.appendChild(del); head.appendChild(controls); card.appendChild(head);
            const type = document.createElement('select'); type.setAttribute('aria-label', 'Question type'); [['multipleChoice', 'Multiple choice'], ['trueFalse', 'True / false']].forEach(([v, t]) => { const o = document.createElement('option'); o.value = v; o.textContent = t; type.appendChild(o); }); type.value = q.type; type.onchange = () => { q.type = type.value; q.answer = q.type === 'trueFalse' ? 'true' : '1'; renderQuestions(); }; card.appendChild(type);
            const prompt = document.createElement('textarea'); prompt.rows = 2; prompt.maxLength = 4000; prompt.placeholder = 'Question prompt'; prompt.value = q.prompt; prompt.oninput = () => q.prompt = prompt.value; card.appendChild(prompt);
            const points = document.createElement('input'); points.type = 'number'; points.min = '0.01'; points.max = '1000'; points.step = '0.01'; points.value = q.points; points.setAttribute('aria-label', 'Question points'); points.oninput = () => q.points = points.value; card.appendChild(points);
            if (q.type === 'multipleChoice') {
                const options = document.createElement('textarea'); options.rows = 4; options.placeholder = 'One answer option per line (2–8 options)'; options.value = q.options; options.oninput = () => q.options = options.value; card.appendChild(options);
                const correct = document.createElement('input'); correct.type = 'number'; correct.min = '1'; correct.max = '8'; correct.value = q.answer; correct.setAttribute('aria-label', 'Correct option number'); correct.title = 'Correct option number, starting at 1'; correct.oninput = () => q.answer = correct.value; card.appendChild(correct);
            } else {
                const answer = document.createElement('select'); answer.setAttribute('aria-label', 'Correct true/false answer'); [['true', 'Correct answer: True'], ['false', 'Correct answer: False']].forEach(([v, t]) => { const o = document.createElement('option'); o.value = v; o.textContent = t; answer.appendChild(o); }); answer.value = q.answer; answer.onchange = () => q.answer = answer.value; card.appendChild(answer);
            }
            const upload = document.createElement('input'); upload.type = 'file'; upload.accept = '.jpg,.jpeg,.png,image/jpeg,image/png'; upload.setAttribute('aria-label', 'Optional question image');
            const uploadButton = document.createElement('button'); uploadButton.type = 'button'; uploadButton.className = 'secondary'; uploadButton.textContent = q.imageFileId ? 'Replace question image' : 'Upload question image';
            uploadButton.onclick = async () => { if (!upload.files.length) { message('Choose an image first.', 'error'); return; } try { await save(); const form = new FormData(); form.append('file', upload.files[0]); form.append('examId', $('examId').value); form.append('questionId', q.id); const response = await fetch(api + '?action=image', { method: 'POST', credentials: 'same-origin', body: form }); const data = await response.json(); if (!response.ok) throw new Error(data.message || 'Image upload failed.'); q.imageFileId = data.id; await save(); message('Question image uploaded to Firebase Storage and linked to the draft.', 'success'); renderQuestions(); } catch (e) { message(e.message, 'error'); } };
            const imageRow = document.createElement('div'); imageRow.className = 'question-controls'; imageRow.append(upload, uploadButton); if (q.imageFileId) { const img = document.createElement('img'); img.className = 'question-image'; img.src = api + '?action=image&fileId=' + encodeURIComponent(q.imageFileId); img.alt = 'Question image preview'; imageRow.appendChild(img); } card.appendChild(imageRow);
            host.appendChild(card);
        });
    }
    function fields() { return { placementId: $('placementId').value.trim(), taskId: $('taskId').value.trim(), ...packDraft() }; }
    async function save() {
        const payload = fields();
        if (!$('examId').value) {
            const created = await call('create', payload); $('examId').value = created.id; $('draftRevision').value = created.exam.draftRevision || 1;
            message('Draft saved in Firebase. Exam ID: ' + created.id, 'success'); await loadList(); return created;
        }
        const result = await call('save', { examId: $('examId').value, revision: $('draftRevision').value, draft: payload });
        $('draftRevision').value = result.draftRevision; message('Draft saved in Firebase.', 'success'); await loadList(); return result;
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
            if (!entries.length) { const p = document.createElement('p'); p.className = 'muted'; p.textContent = 'No authorized exams are available.'; host.appendChild(p); return; }
            entries.forEach(([id, exam]) => { const card = document.createElement('article'); card.className = 'exam-card'; const h = document.createElement('h3'); h.textContent = exam.draft?.title || exam.title || id; card.appendChild(h); const p = document.createElement('p'); p.textContent = (exam.status || 'draft') + ' · Task ' + (exam.taskId || ''); card.appendChild(p); const edit = document.createElement('button'); edit.type = 'button'; edit.className = 'secondary'; edit.textContent = exam.status === 'published' ? 'View exam' : 'Edit draft'; edit.onclick = async () => { try { const detail = await call('exam&examId=' + encodeURIComponent(id)); loadDraft(detail); window.scrollTo({ top: 0, behavior: 'smooth' }); } catch (e) { message(e.message, 'error'); } }; card.appendChild(edit); if (exam.status === 'published') { const results = document.createElement('button'); results.type = 'button'; results.className = 'secondary'; results.textContent = 'View attempts'; results.onclick = async () => { try { const attempts = await call('attempts&examId=' + encodeURIComponent(id)); const summary = Object.values(attempts || {}).flatMap(students => Object.values(students || {})); const completed = summary.filter(a => a.status === 'submitted' || a.status === 'expired'); message(completed.length + ' submitted attempt(s); scores are calculated by the server.', 'success'); card.querySelector('.attempt-summary')?.remove(); const box = document.createElement('div'); box.className = 'attempt-summary'; completed.forEach(a => { const line = document.createElement('p'); line.textContent = 'Student ' + a.studentId + ' · ' + a.status + ' · ' + a.score + ' / ' + a.maximumScore; box.appendChild(line); }); card.appendChild(box); } catch (e) { message(e.message, 'error'); } }; card.appendChild(results); } host.appendChild(card); });
        } catch (e) { message(e.message, 'error'); }
    }
    $('newExam').onclick = () => { $('examForm').reset(); $('examId').value = ''; $('draftRevision').value = '0'; questions = []; renderQuestions(); $('editorTitle').textContent = 'Create exam draft'; $('examForm').querySelectorAll('input,textarea,select,button').forEach(el => el.disabled = false); };
    $('addQuestion').onclick = () => { questions.push({ id: uid(), type: 'multipleChoice', prompt: '', points: 1, options: 'Option 1\nOption 2', answer: '1', imageFileId: '' }); renderQuestions(); };
    $('saveDraft').onclick = async () => { try { await save(); } catch (e) { message(e.message, 'error'); } };
    $('publishExam').onclick = async () => { try { await save(); const result = await call('publish', { examId: $('examId').value }); message('Exam version ' + result.version + ' is published in Firebase.', 'success'); loadDraft({ id: $('examId').value, placementId: $('placementId').value, taskId: $('taskId').value, status: 'published', publishedVersion: result.version, draft: packDraft() }); await loadList(); } catch (e) { message(e.message, 'error'); } };
    $('examForm').addEventListener('submit', e => e.preventDefault());
    renderQuestions(); loadList();
}());
