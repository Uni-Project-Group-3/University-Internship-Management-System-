(function () {
    'use strict';
    const api = '../TaskExamsApi.ashx', $ = id => document.getElementById(id), notice = $('notice'), list = $('examList'), panel = $('attemptPanel');
    let exam = null, attempt = null, answers = {}, saveTimer = null, busy = false;
    function message(text, kind) { notice.textContent = text; notice.className = 'notice' + (kind ? ' ' + kind : ''); }
    async function call(action, data, method) {
        const response = await fetch(api + (action.indexOf('&') >= 0 ? '?action=' : '?action=') + action, { method: method || (data ? 'POST' : 'GET'), credentials: 'same-origin', cache: 'no-store', headers: data ? { 'Content-Type': 'application/json', 'Accept': 'application/json' } : { 'Accept': 'application/json' }, body: data ? JSON.stringify(data) : undefined });
        const result = await response.json(); if (!response.ok) throw new Error(result.message || 'Exam request failed.'); return result;
    }
    function text(el, value) { el.textContent = value == null ? '' : String(value); }
    function deadlineDate(value) { const d = new Date(value); return isNaN(d.getTime()) ? 'Unavailable' : d.toLocaleString(); }
    async function load() {
        try {
            const exams = await call('list'); list.replaceChildren(); const entries = Object.entries(exams || {});
            if (!entries.length) { const p = document.createElement('p'); p.className = 'panel'; p.textContent = 'No published exams are available for your assigned placement.'; list.appendChild(p); return; }
            for (const [id, item] of entries) {
                const card = document.createElement('article'); card.className = 'exam-card'; const title = document.createElement('h2'); title.textContent = item.versionData?.title || 'Internship exam'; card.appendChild(title);
                const detail = document.createElement('p'); detail.textContent = 'Task ' + item.taskId + ' · ' + (item.versionData?.durationMinutes || 0) + ' minutes · ' + (item.versionData?.attemptLimit || 1) + ' attempt(s)'; card.appendChild(detail);
                const availability = document.createElement('p'); availability.textContent = 'Available ' + deadlineDate(item.versionData?.opensAt) + ' — ' + deadlineDate(item.versionData?.closesAt); card.appendChild(availability);
                const actions = document.createElement('div'); actions.className = 'actions'; const my = await call('myAttempts&examId=' + encodeURIComponent(id)); const old = Object.values(my || {}).sort((a, b) => String(b.startedAt).localeCompare(String(a.startedAt)))[0];
                if (old && old.status === 'inProgress') { const b = document.createElement('button'); b.className = 'primary'; b.textContent = 'Continue exam'; b.onclick = () => openExam(id, old.id); actions.appendChild(b); }
                else if (old && (old.status === 'submitted' || old.status === 'expired')) { const result = document.createElement('span'); result.className = 'score'; result.textContent = old.score == null ? 'Submitted' : 'Score ' + old.score + ' / ' + old.maximumScore; actions.appendChild(result); }
                else { const b = document.createElement('button'); b.className = 'primary'; b.textContent = 'Start exam'; b.onclick = () => startExam(id); actions.appendChild(b); }
                card.appendChild(actions); list.appendChild(card);
            }
            message('Published exams loaded from Firebase.', 'success');
        } catch (e) { message(e.message, 'error'); }
    }
    async function startExam(id) {
        try { exam = await call('exam&examId=' + encodeURIComponent(id)); attempt = await call('start', { examId: id }); answers = attempt.answers || {}; renderAttempt(); }
        catch (e) { message(e.message, 'error'); }
    }
    async function openExam(id, attemptId) {
        try { [exam, attempt] = await Promise.all([call('exam&examId=' + encodeURIComponent(id)), call('attempt&examId=' + encodeURIComponent(id) + '&attemptId=' + encodeURIComponent(attemptId))]); answers = attempt.answers || {}; renderAttempt(); }
        catch (e) { message(e.message, 'error'); }
    }
    function renderAttempt() {
        const v = exam.versionData || {}; panel.replaceChildren(); panel.classList.remove('hidden'); const h = document.createElement('h2'); h.textContent = v.title || 'Exam'; panel.appendChild(h);
        const info = document.createElement('p'); info.textContent = (v.instructions || '') + ' · Server deadline: ' + deadlineDate(attempt.deadlineAt); panel.appendChild(info);
        const order = v.questionOrder || [], qs = v.questions || {};
        order.forEach((id, index) => {
            const q = qs[id]; if (!q) return; const box = document.createElement('article'); box.className = 'question'; const title = document.createElement('h3'); title.textContent = (index + 1) + '. ' + q.prompt + ' (' + q.points + ' points)'; box.appendChild(title);
            if (q.imageFileId) { const img = document.createElement('img'); img.className = 'question-image'; img.src = api + '?action=image&fileId=' + encodeURIComponent(q.imageFileId); img.alt = 'Question illustration'; box.appendChild(img); }
            const choices = q.type === 'multipleChoice' ? q.options || [] : [{ id: 'true', text: 'True' }, { id: 'false', text: 'False' }];
            choices.forEach(choice => { const label = document.createElement('label'); label.className = 'option'; const input = document.createElement('input'); input.type = 'radio'; input.name = 'answer-' + id; input.value = choice.id; input.checked = q.type === 'multipleChoice' ? answers[id] === choice.id : String(answers[id]) === choice.id; input.disabled = attempt.status !== 'inProgress'; input.onchange = () => { answers[id] = q.type === 'multipleChoice' ? choice.id : choice.id === 'true'; scheduleSave(); }; label.append(input, document.createTextNode(choice.text)); box.appendChild(label); });
            panel.appendChild(box);
        });
        const actions = document.createElement('div'); actions.className = 'actions'; const save = document.createElement('button'); save.className = 'secondary'; save.textContent = 'Save answers'; save.disabled = attempt.status !== 'inProgress'; save.onclick = () => saveAnswers(false); actions.appendChild(save); const submit = document.createElement('button'); submit.className = 'primary'; submit.textContent = attempt.status === 'inProgress' ? 'Submit exam' : 'Exam submitted'; submit.disabled = attempt.status !== 'inProgress'; submit.onclick = () => saveAnswers(true); actions.appendChild(submit); panel.appendChild(actions);
        if (attempt.status !== 'inProgress') { const score = document.createElement('p'); score.className = 'score'; score.textContent = attempt.score == null ? 'Submitted' : 'Score ' + attempt.score + ' / ' + attempt.maximumScore; panel.appendChild(score); }
        panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    function scheduleSave() { clearTimeout(saveTimer); saveTimer = setTimeout(() => saveAnswers(false), 800); }
    async function saveAnswers(submit) {
        if (!attempt || attempt.status !== 'inProgress') return;
        if (busy) { clearTimeout(saveTimer); saveTimer = setTimeout(() => saveAnswers(submit), 500); return; }
        busy = true; clearTimeout(saveTimer);
        try { attempt = await call(submit ? 'submit' : 'answers', { examId: exam.id, attemptId: attempt.id, answers: answers }); const completed = attempt.status !== 'inProgress'; message(completed ? (attempt.status === 'expired' ? 'Time expired. The server graded the last saved answers.' : 'Exam answers submitted and graded by the server.') : 'Answers saved to Firebase.', 'success'); if (submit || completed) { renderAttempt(); await load(); } }
        catch (e) { message(e.message, 'error'); } finally { busy = false; }
    }
    load();
}());
