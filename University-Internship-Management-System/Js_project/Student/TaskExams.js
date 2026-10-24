(function () {
    'use strict';
    const embedded = !!document.getElementById('taskExamWorkspace');
    const api = '../TaskExamsApi.ashx', $ = id => document.getElementById(embedded ? ({ notice: 'taskExamNotice', examList: 'taskExamList' }[id] || id) : id);
    let exam, attempt, answers = {}, busy = false, saveTimer, timer, anchor, saveQueue = Promise.resolve();
    let refreshDetails = null;
    let taskFilter = new URLSearchParams(location.search).get('taskId'), loadVersion = 0;
    let indexedTasks = new Set();
    const notice = $('notice');
    function message(value, error) { notice.textContent = value; notice.className = 'notice ' + (error ? 'error' : 'success'); }
    const now = () => anchor ? anchor.server + performance.now() - anchor.local : null;
    async function call(action, data) {
        const sent = performance.now();
        const response = await fetch(api + '?action=' + action, { method: data ? 'POST' : 'GET', credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(65000), headers: data ? { 'Content-Type': 'application/json' } : {}, body: data ? JSON.stringify(data) : undefined });
        const result = await response.json(); if (!response.ok) throw new Error(result.message || 'Exam request failed.');
        if (result.serverNow) anchor = { server: Date.parse(result.serverNow) + (performance.now() - sent) / 2, local: performance.now() };
        return result;
    }
    const date = value => new Date(value).toLocaleString(undefined, { timeZoneName: 'short' });
    function countdown(ms) { const s = Math.max(0, Math.ceil(ms / 1000)), d = Math.floor(s / 86400); return (d ? d + 'd ' : '') + [Math.floor(s / 3600) % 24, Math.floor(s / 60) % 60, s % 60].map(n => String(n).padStart(2, '0')).join(' : '); }
    const resultLabel = item => item.resultStatus === 'released' ? 'Score ' + item.score + ' / ' + item.maximumScore + (item.feedback ? ' · ' + item.feedback : '') + ' · Released ' + date(item.releasedAt) : item.resultStatus === 'revisionRequested' ? 'Not complete. Your supervisor requested a new examination attempt. Check the new deadline before starting.' : item.resultStatus === 'approvedHidden' ? 'Your result is currently hidden by your company supervisor.' : 'Exam submitted successfully. Your result is awaiting supervisor approval.';
    function element(tag, text, className) { const el = document.createElement(tag); if (tag === 'button') el.type = 'button'; if (text) el.textContent = text; if (className) el.className = className; return el; }
    function dialog(title, large) { const el = element('dialog', '', 'workspace-dialog student-exam-popup' + (large ? ' workspace-exam-attempt' : '')); const h = element('h2', title); h.id = 'exam-title-' + Math.random().toString(36).slice(2); el.setAttribute('aria-labelledby', h.id); el.append(h); const parent = document.querySelector('.modal.show'), trap = parent && window.bootstrap?.Modal.getInstance(parent)?._focustrap; trap?.deactivate(); el.addEventListener('close', () => { if (!document.querySelector('dialog[open]')) trap?.activate(); }); document.body.append(el); return el; }
    async function load() {
        const version = ++loadVersion, filter = taskFilter;
        try {
            const items = await call('list'); if (version !== loadVersion) return; indexedTasks = new Set(Object.values(items || {}).filter(x=>x?.versionData).map(x=>x.taskId)); window.dispatchEvent(new Event('task-exam-index')); $('examList').replaceChildren();
            for (const [id, item] of Object.entries(items || {})) {
                if (!item?.versionData || (filter && item.taskId !== filter)) continue;
                const card = element('article', '', 'exam-card'), button = element('button', 'View exam', 'primary');
                const v = item.versionData, trusted = Date.parse(item.serverNow), state = trusted < Date.parse(v.opensAt) ? 'Scheduled' : trusted >= Date.parse(v.closesAt) ? 'Closed' : 'Open';
                button.onclick = () => view(id); card.append(element('h2', v.title), element('p', item.taskTitle || 'Assigned task'), element('span', state, 'badge'), element('p', 'Opens: ' + date(v.opensAt) + '\nCloses: ' + date(v.closesAt)), element('p', v.durationMinutes + ' minutes · ' + v.attemptLimit + ' attempt(s)'), button); $('examList').append(card);
            }
            if (!$('examList').children.length) $('examList').textContent = 'No published exams are available for your placement.';
            if (notice.textContent.startsWith('Loading')) message('Your published examinations are loaded from Firebase. Results remain private until your company supervisor releases them.');
        } catch (e) { if (version === loadVersion) { $('examList').replaceChildren(); message(e.message, true); } }
    }
    async function view(id) {
        try {
            const items = await call('list'), item = items[id]; if (!item) throw new Error('Exam is no longer available.');
            if (item.serverNow) anchor = { server: Date.parse(item.serverNow), local: performance.now() };
            let attempts = Object.values(await call('myAttempts&examId=' + encodeURIComponent(id))).sort((a, b) => String(b.startedAt).localeCompare(String(a.startedAt)));
            let latest = attempts[0]; const v = item.versionData, popup = dialog(v.title), details = element('div', '', 'exam-overview');
            details.append(element('p', 'Task: ' + (item.taskTitle || item.taskId), 'exam-task-caption'), element('p', v.instructions || 'Read every question carefully before submitting.'));
            const metrics = element('dl', '', 'exam-overview-grid');
            for (const [label, value] of [['Opening', date(v.opensAt)], ['Closing', date(v.closesAt)], ['Duration', v.durationMinutes + ' minutes'], ['Questions', v.questionCount], ['Total points', v.totalPoints], ['Allowed attempts', v.attemptLimit]]) { const cell = element('div'); cell.append(element('dt', label), element('dd', String(value))); metrics.append(cell); }
            details.append(metrics);
            const clock = element('div', '', 'exam-timer'), status = element('p'), actions = element('div', '', 'workspace-dialog-actions'), close = element('button', 'Close', 'secondary'), start = element('button', '🔒 Start exam', 'primary exam-locked'); start.disabled = true; clock.setAttribute('role', 'timer');
            status.className = 'exam-result-notice';
            if (latest && latest.status !== 'inProgress') { status.textContent = resultLabel(latest); message(resultLabel(latest)); }
            const availability = element('div', '', 'exam-start-gate'), clockLabel = element('span', 'Time until exam opens', 'exam-gate-label');
            availability.append(clockLabel, clock, start);
            let checking = false, eligible = false, checkingEligibility = false, lastCheck = -Infinity;
            start.onclick = async () => {
                if (checking) return; checking = true; start.disabled = true;
                try { if (latest?.status !== 'inProgress' && !await TaskDialogs.confirm('Start examination?', 'Starting consumes an allowed attempt. The server timer continues even if you close the popup.', 'Start exam')) return; attempt = await call('start', { examId: id }); exam = await call('exam&examId=' + encodeURIComponent(id)); answers = attempt.answers || {}; finish(); renderAttempt(); }
                catch (e) { status.textContent = e.message; } finally { checking = false; }
            };
            function tick() {
                const trusted = now(), left = Date.parse(v.opensAt) - trusted, closed = trusted >= Date.parse(v.closesAt), exhausted = latest?.status !== 'inProgress' && attempts.length >= v.attemptLimit;
                start.disabled = checking || !eligible || trusted == null || left > 0 || closed || exhausted;
                start.classList.toggle('exam-locked', start.disabled);
                if (left <= 0 && !closed && !exhausted && !eligible && !checkingEligibility && performance.now() - lastCheck > 10000) {
                    checkingEligibility = true; lastCheck = performance.now();
                    call('eligibility&examId=' + encodeURIComponent(id)).then(result => { eligible = result.eligible; }).catch(e => { eligible = false; status.textContent = e.message; }).finally(() => { checkingEligibility = false; });
                }
                start.textContent = closed ? 'Exam closed' : exhausted ? 'Attempt limit reached' : left > 0 ? '🔒 Start exam' : latest?.status === 'inProgress' ? 'Continue exam' : 'Start exam';
                clock.textContent = trusted == null ? 'Synchronizing server time…' : left > 0 ? countdown(left) : closed ? 'Closed' : 'Open now';
                clockLabel.textContent = trusted == null ? 'Verifying server time' : left > 0 ? 'Time until exam opens' : closed ? 'The examination window has ended' : exhausted ? 'No attempts remaining' : eligible ? 'Your exam is ready' : 'Confirming eligibility with the server';
            }
            function finish() { refreshDetails = null; clearInterval(interval); popup.close(); popup.remove(); }
            close.onclick = finish; popup.addEventListener('cancel', e => { e.preventDefault(); finish(); }); actions.append(close); popup.append(details, availability, status, actions); popup.showModal();
            const interval = setInterval(tick, 1000); tick();
            refreshDetails = async () => { if (checking || !popup.open) return; const [items, records] = await Promise.all([call('list'), call('myAttempts&examId=' + encodeURIComponent(id))]); if (items[id]?.versionData) Object.assign(v, items[id].versionData); attempts = Object.values(records || {}).sort((a, b) => String(b.startedAt).localeCompare(String(a.startedAt))); latest = attempts[0]; status.textContent = latest && latest.status !== 'inProgress' ? resultLabel(latest) : ''; if (status.textContent) message(status.textContent); eligible = false; lastCheck = -Infinity; tick(); };
        } catch (e) { message(e.message, true); }
    }
    function renderAttempt() {
        clearInterval(timer); document.getElementById('takeExamDialog')?.remove();
        const popup = dialog(exam.versionData.title, true); popup.id = 'takeExamDialog';
        const clock = element('div', '', 'exam-timer'), progress = element('p'), status = element('p'), content = element('div', '', 'workspace-dialog-body'), nav = element('nav');
        clock.setAttribute('role', 'timer'); nav.setAttribute('aria-label', 'Question navigation'); const v = exam.versionData, order = v.questionOrder || [];
        let currentQuestion = 0;
        const completion = element('progress', '', 'exam-completion'); completion.max = Math.max(1, order.length); completion.setAttribute('aria-label', 'Answered questions');
        const update = () => { progress.textContent = Object.keys(answers).length + ' / ' + order.length + ' answered'; completion.value = Object.keys(answers).length; [...nav.children].forEach((button, index) => { button.classList.toggle('answered', Object.prototype.hasOwnProperty.call(answers, order[index])); button.setAttribute('aria-label', 'Question ' + (index + 1) + (Object.prototype.hasOwnProperty.call(answers, order[index]) ? ', answered' : ', unanswered')); }); };
        const selectQuestion = index => { currentQuestion = index; [...content.children].forEach((box, i) => { box.hidden = i !== index; }); [...nav.children].forEach((button, i) => button.setAttribute('aria-current', String(i === index))); };
        order.forEach((id, index) => {
            const q = v.questions[id], box = element('article', '', 'question'); box.id = 'question-' + id; box.append(element('h3', (index + 1) + '. ' + q.prompt + ' · ' + q.points + ' points'));
            const jump = element('button', String(index + 1), 'secondary'); jump.onclick = () => { selectQuestion(index); previous.disabled = index === 0; next.disabled = index === order.length - 1; }; nav.append(jump);
            if (q.imageFileId) { const image = element('img', '', 'question-image'); image.src = api + '?action=image&fileId=' + encodeURIComponent(q.imageFileId); image.alt = 'Question ' + (index + 1) + ' illustration'; box.append(image); }
            for (const option of q.type === 'multipleChoice' ? q.options : [{ id: 'true', text: 'True' }, { id: 'false', text: 'False' }]) {
                const label = element('label', '', 'option'), input = element('input'); input.type = 'radio'; input.name = 'answer-' + id; input.checked = String(answers[id]) === option.id; input.disabled = attempt.status !== 'inProgress';
                input.onchange = () => { answers[id] = q.type === 'trueFalse' ? option.id === 'true' : option.id; update(); clearTimeout(saveTimer); saveTimer = setTimeout(() => save(false, status), 700); };
                label.append(input, document.createTextNode(option.text)); box.append(label);
            }
            content.append(box);
        });
        const actions = element('div', '', 'workspace-dialog-actions'), close = element('button', 'Save and close', 'secondary'), submit = element('button', 'Submit exam', 'primary'); submit.disabled = attempt.status !== 'inProgress';
        close.onclick = async () => { if (attempt.status === 'inProgress' && !await TaskDialogs.confirm('Save and return to the task?', 'Your answers will be saved. The examination timer continues while you are away.', 'Save and return')) return; if (await save(false, status)) { clearInterval(timer); popup.close(); } };
        submit.onclick = async () => { if (submit.disabled) return; submit.disabled = true; try { if (await TaskDialogs.confirm('Are you sure you want to submit your exam?', Math.max(0, order.length - Object.keys(answers).length) + ' unanswered question(s). Submission locks your answers.', 'Confirm submission')) { content.querySelectorAll('input').forEach(el => el.disabled = true); await save(true, status); } } finally { if (attempt.status === 'inProgress' && now() < Date.parse(attempt.deadlineAt)) { submit.disabled = false; content.querySelectorAll('input').forEach(el => el.disabled = false); } } };
        const previous = element('button', 'Previous', 'secondary'), next = element('button', 'Next', 'primary'); previous.disabled = true; next.disabled = order.length <= 1;
        const step = delta => { selectQuestion(Math.max(0, Math.min(order.length - 1, currentQuestion + delta))); previous.disabled = currentQuestion === 0; next.disabled = currentQuestion === order.length - 1; };
        previous.onclick = () => step(-1); next.onclick = () => step(1);
        actions.append(previous, next, close, submit); popup.append(clock, progress, completion, nav, status, content, actions); update(); selectQuestion(0); popup.showModal(); popup.oncancel = e => { e.preventDefault(); close.click(); };
        if (attempt.status !== 'inProgress') {
            // A finalized attempt is a receipt, not an editable examination.
            clock.remove(); progress.remove(); completion.remove(); nav.remove(); content.remove();
            previous.remove(); next.remove(); submit.remove();
            status.textContent = resultLabel(attempt); close.textContent = 'Close'; close.onclick = () => popup.close();
            const examId = exam.id, attemptId = attempt.id;
            const refreshResult = async () => {
                if (!popup.open) return;
                const records = await call('myAttempts&examId=' + encodeURIComponent(examId));
                const current = records[attemptId];
                if (!popup.open || !current) return;
                attempt = current; status.textContent = resultLabel(current); message(resultLabel(current));
            };
            refreshDetails = refreshResult;
            popup.addEventListener('close', () => { if (refreshDetails === refreshResult) refreshDetails = null; }, { once: true });
            return;
        }
        let expiring = false;
        const tick = async () => { const left = Date.parse(attempt.deadlineAt) - now(); clock.textContent = countdown(left); if (left <= 0 && !expiring) { expiring = true; content.querySelectorAll('input').forEach(el => el.disabled = true); submit.disabled = true; if (!await save(true, status)) expiring = false; } };
        timer = setInterval(tick, 1000); tick();
    }
    async function save(finalize, status) {
        const operation = saveQueue.then(() => persist(finalize, status));
        saveQueue = operation.catch(() => false);
        return operation;
    }
    async function persist(finalize, status) {
        if (attempt.status !== 'inProgress') return true;
        busy = true; clearTimeout(saveTimer);
        try {
            attempt = await call(finalize ? 'submit' : 'answers', { examId: exam.id, attemptId: attempt.id, answers: { ...answers } }); status.textContent = attempt.status === 'inProgress' ? 'Answers saved.' : resultLabel(attempt);
            if (attempt.status !== 'inProgress') { clearInterval(timer); renderAttempt(); message(resultLabel(attempt)); await load(); } return true;
        } catch (e) { status.textContent = 'Save failed: ' + e.message + ' Your answers remain on this page. Retry when connected.'; return false; } finally { busy = false; }
    }
    window.StudentTaskExams = Object.freeze({ hasTask: id => indexedTasks.has(id), index: load, showTask(id) { taskFilter = id; $('examList').replaceChildren(); notice.textContent = 'Loading examination from Firebase…'; return load(); }, async refresh() { if (refreshDetails) await refreshDetails(); else if (canRefresh()) await load(); } });
    if (!embedded) load();
    const canRefresh = () => !document.querySelector('dialog[open]') && (!embedded || (!document.getElementById('studentPanelExam').hidden && document.getElementById('submitModal').classList.contains('show')));
    window.addEventListener('focus', () => { if (canRefresh()) load(); });
    if (!embedded) TaskDialogs.watch(() => window.StudentTaskExams.refresh(), () => !busy);
}());
