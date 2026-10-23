// UI-only fixtures exercise the real page/scripts. No live Firebase requests are permitted.
const fs = require('fs'), path = require('path'), http = require('http'), assert = require('assert');
const { chromium } = require(path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const root = path.resolve(__dirname, '../../University-Internship-Management-System');
const students = [{ id: 'TEST-ST-1', name: 'UI Test Student One', placementId: 'TEST-PL-1' }, { id: 'TEST-ST-2', name: 'UI Test Student Two', placementId: 'TEST-PL-2' }];
const tasks = students.map((s, i) => ({ id: 'TEST-T-' + i, title: 'UI fixture task', description: 'UI-only fixture', studentId: s.id, studentName: s.name, placementId: s.placementId, status: 'assigned', maximum: 10, deadline: new Date(Date.now() + 86400000).toISOString(), batchId: 'TEST-BATCH' }));
const columns = [{ id: 'todo', title: 'To Do' }, { id: 'progress', title: 'In Progress' }, { id: 'review', title: 'Awaiting Review' }, { id: 'done', title: 'Done' }];
const server = http.createServer((req, res) => {
    const url = new URL(req.url, 'http://localhost');
    const file = path.resolve(root, '.' + decodeURIComponent(url.pathname));
    if (!file.startsWith(root + path.sep) || !/\.(aspx|css|js|png|jpg|svg|woff2?)$/i.test(file)) { res.writeHead(404); return res.end(); }
    try { let content = fs.readFileSync(file); if (file.endsWith('.aspx')) content = content.toString().replace(/<%@[^%]*%>/g, ''); res.setHeader('Content-Type', file.endsWith('.js') ? 'application/javascript' : file.endsWith('.css') ? 'text/css' : file.endsWith('.aspx') ? 'text/html' : 'application/octet-stream'); res.end(content); } catch (_) { res.writeHead(404); res.end(); }
});
(async () => {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const base = 'http://127.0.0.1:' + server.address().port;
    const executablePath = [process.env.PROGRAMFILES + '/Google/Chrome/Application/chrome.exe', process.env['PROGRAMFILES(X86)'] + '/Microsoft/Edge/Application/msedge.exe'].find(fs.existsSync);
    const browser = await chromium.launch({ executablePath, headless: true });
    try {
        const context = await browser.newContext();
        // Prove server-feed synchronization independently of same-browser BroadcastChannel hints.
        await context.addInitScript(() => { window.BroadcastChannel = undefined; });
        let creates = 0, starts = 0, examOpen = false, openingAt = null, examClosed = false, storedAttempt = null, failTasks = false, failStorage = false, imageUploads = 0, resourceUploads = 0, answerSaveInFlight = false, changeVersion = 0; const positions = new Map(), composedExams = new Map(), submissions = [];
        await context.route('**/*', async route => {
            const req = route.request(), url = new URL(req.url());
            if (url.origin !== base) return route.abort();
            if (url.pathname.endsWith('/TasksApi.ashx')) {
                if (failTasks) return route.fulfill({ status: 503, json: { message: 'Task connection unavailable during UI test.' } });
                const action = url.searchParams.get('action'); let body = {};
                if (action === 'changes') { await new Promise(resolve => setTimeout(resolve, 250)); return route.fulfill({ json: { version: changeVersion, changed: Number(url.searchParams.get('version')) !== changeVersion, connected: true } }).catch(() => {}); }
                if (!action) body = { tasks, submissions, students };
                else if (action === 'board') body = { placementId: url.searchParams.get('placementId'), canMove: true, columns, tasks: tasks.filter(t => t.placementId === url.searchParams.get('placementId')).map(t => ({ ...t, columnId: positions.get(t.id) || 'todo', dueDate: t.deadline, order: 1 })) };
                else if (action === 'move') { const payload = req.postDataJSON(); assert.strictEqual(payload.placementId, tasks.find(t => t.id === payload.taskId).placementId); positions.set(payload.taskId, payload.columnId); body = { success: true }; }
                else if (action === 'bulkCreate') { const input = req.postDataJSON(); assert.strictEqual(input.description, 'UI test requirements'); assert.strictEqual(input.criteria, 'UI test instructions'); assert.strictEqual(input.maximum, 10); creates++; body = { success: true, results: input.placementIds.map((id, i) => ({ success: true, id: 'UI-CREATED-' + i, placementId: id })) }; }
                else if (action === 'attachments') body = { files: [] };
                else if (action === 'start') { tasks.find(t => t.id === req.postDataJSON().assignmentId).status = 'inprogress'; changeVersion++; body = { success: true }; }
                else if (action === 'submit') { const input = req.postDataJSON(); assert.strictEqual(input.description, 'Completed student work'); assert.strictEqual(input.link, ''); tasks.find(t => t.id === input.assignmentId).status = 'submitted'; submissions.push({ taskId: input.assignmentId, revision: 1, description: input.description, createdAt: new Date().toISOString() }); changeVersion++; body = { success: true }; }
                else if (action === 'attach') { resourceUploads++; body = { id: 'UI-RESOURCE-' + resourceUploads }; }
                else if (action === 'storageAvailability') return route.fulfill({ status: failStorage ? 404 : 200, json: failStorage ? { message: 'Configured Storage bucket is unavailable.' } : { available: true } });
                return route.fulfill({ json: body });
            }
            if (url.pathname.endsWith('/TaskExamsApi.ashx')) {
                let body = {};
                const action = url.searchParams.get('action');
                if (action === 'create') { const payload = req.postDataJSON(), id = 'UI-COMPOSED-' + payload.taskId; assert(payload.creationKey); assert.strictEqual(payload.questions.length, 2); if (!composedExams.has(id)) composedExams.set(id, { id, status: 'draft', draftRevision: 1, draft: payload }); return route.fulfill({ json: { id, exam: composedExams.get(id) } }); }
                if (action === 'image' && req.method() === 'POST') { imageUploads++; return route.fulfill({ json: { id: 'UI-IMAGE-' + imageUploads, status: 'ready' } }); }
                if (action === 'save') { const payload = req.postDataJSON(), exam = composedExams.get(payload.examId); assert(exam); assert(payload.draft.questions[0].imageFileId); assert.strictEqual(payload.draft.questions[1].type, 'trueFalse'); exam.draft = payload.draft; exam.draftRevision++; return route.fulfill({ json: { draftRevision: exam.draftRevision } }); }
                if (action === 'publish') { const exam = composedExams.get(req.postDataJSON().examId); assert(exam.draft.questions[0].imageFileId); exam.status = 'published'; return route.fulfill({ json: { version: 1 } }); }
                const definition = { title: 'UI examination fixture', instructions: 'UI-only fixture', opensAt: new Date(openingAt || Date.now() + (examOpen ? -3600000 : 3600000)).toISOString(), closesAt: new Date(Date.now() + (examClosed ? -1000 : 7200000)).toISOString(), durationMinutes: 30, attemptLimit: 1, totalPoints: 2, questionCount: 1 };
                const receipt = () => { const copy = { ...storedAttempt }; if (copy.resultStatus !== 'released') { delete copy.score; delete copy.maximumScore; delete copy.feedback; delete copy.releasedAt; } return copy; };
                if (action === 'list') body = { 'UI-EXAM': { id: 'UI-EXAM', status: 'published', title: definition.title, taskId: tasks[0].id, serverNow: new Date().toISOString(), versionData: definition } };
                if (action === 'myAttempts') body = storedAttempt ? { 'UI-ATTEMPT': receipt() } : {};
                if (action === 'eligibility') body = { eligible: examOpen, serverNow: new Date().toISOString() };
                if (action === 'start') { starts++; storedAttempt = { id: 'UI-ATTEMPT', examId: 'UI-EXAM', studentId: students[0].id, status: 'inProgress', startedAt: new Date().toISOString(), deadlineAt: new Date(Date.now() + 1800000).toISOString(), serverNow: new Date().toISOString(), answers: {} }; body = receipt(); }
                if (action === 'exam') body = { id: 'UI-EXAM', versionData: { ...definition, questionOrder: ['Q1'], questions: { Q1: { prompt: 'UI test question', type: 'trueFalse', points: 2 } } } };
                if (action === 'answers' || action === 'submit') { if (action === 'answers') { answerSaveInFlight = true; await new Promise(resolve => setTimeout(resolve, 1000)); answerSaveInFlight = false; } else assert.strictEqual(answerSaveInFlight, false, 'Final submit must wait for the current answer save'); storedAttempt.answers = req.postDataJSON().answers; if (action === 'submit') { storedAttempt.status = 'submitted'; storedAttempt.resultStatus = 'pendingApproval'; storedAttempt.score = 2; storedAttempt.maximumScore = 2; tasks[0].status = 'submitted'; changeVersion++; } body = receipt(); }
                if (action === 'attempts') body = { [students[0].id]: { 'UI-ATTEMPT': { ...storedAttempt, answerKey: { Q1: { correctAnswer: true } } } } };
                if (action === 'release') { storedAttempt.resultStatus = 'released'; storedAttempt.feedback = req.postDataJSON().feedback; storedAttempt.releasedAt = new Date().toISOString(); tasks[0].status = 'completed'; changeVersion++; body = receipt(); }
                if (action === 'hideResult') { storedAttempt.resultStatus = 'approvedHidden'; changeVersion++; body = receipt(); }
                if (action === 'revision') { const input = req.postDataJSON(); assert(input.reason.trim()); assert(new Date(input.deadline) > new Date()); storedAttempt.resultStatus = 'revisionRequested'; body = { success: true }; }
                return route.fulfill({ json: body });
            }
            return route.continue();
        });
        const page = await context.newPage(), errors = []; page.on('pageerror', e => errors.push(e.message));
        await page.goto(base + '/Company/ManageTasks.aspx');
        await page.locator('.task-board-card').first().waitFor(); assert.strictEqual(await page.locator('.task-board-card').count(), 2);
        await page.locator('[data-board-task-id="TEST-T-0"]').dragTo(page.locator('[data-column-id="progress"] .task-board-dropzone'));
        await page.locator('[data-column-id="progress"] [data-board-task-id="TEST-T-0"]').waitFor(); assert(!positions.has('TEST-T-1'));
        console.log('PASS drag-and-drop sends the correct individual placement and keeps the other student card independent');
        await page.locator('[data-board-task-id="TEST-T-0"] [data-task-action="review"]').click(); await page.locator('#reviewModal.show').waitFor();
        assert.strictEqual(await page.locator('[data-company-tab]').count(), 6);
        await page.screenshot({ path: path.join(__dirname, 'company-task-review-desktop.png'), animations: 'disabled' });
        await page.getByLabel('Move card to column').selectOption('review'); await page.getByRole('button', { name: 'Move card', exact: true }).click(); await page.locator('dialog[open]').getByRole('button', { name: 'Move card', exact: true }).click(); await page.locator('[data-column-id="review"] [data-board-task-id="TEST-T-0"]').waitFor();
        await page.locator('#reviewModal .btn-close').click(); await page.locator('#reviewModal').waitFor({ state: 'hidden' });
        await page.locator('#listModeButton').click(); assert.strictEqual(await page.locator('#taskList .task-card').count(), 2);
        await page.locator('#boardPlacementSelect').selectOption('TEST-PL-1'); assert.strictEqual(await page.locator('#taskList .task-card').count(), 1);
        await page.locator('#statusFilter').selectOption('completed'); assert.strictEqual(await page.locator('#taskList .task-card').count(), 0);
        await page.locator('#statusFilter').selectOption('assigned'); assert.strictEqual(await page.locator('#taskList .task-card').count(), 1);
        await page.locator('#statusFilter').selectOption('all'); await page.locator('#boardPlacementSelect').selectOption('all');
        await page.screenshot({ path: path.join(__dirname, 'company-task-list-desktop.png'), animations: 'disabled' });
        await page.locator('#boardModeButton').click();
        const cardMove = page.locator('[data-board-task-id="TEST-T-0"] .card-move-controls'); await cardMove.locator('select').selectOption('todo'); await cardMove.getByRole('button', { name: 'Move', exact: true }).click(); await page.locator('dialog[open]').getByRole('button', { name: 'Move card', exact: true }).click(); await page.locator('[data-column-id="todo"] [data-board-task-id="TEST-T-0"]').waitFor();
        console.log('PASS company popup tabs, database-derived student/status filters, card grid and confirmed move controls');
        await page.locator('[data-bs-target="#createTaskModal"]').click(); await page.locator('#createTaskModal.show').waitFor();
        await page.getByLabel('Instructions & assessment criteria').waitFor();
        await page.getByLabel('Maximum grade').waitFor();
        await page.locator('#createTaskModal .modal-footer').getByRole('button', { name: 'Next →', exact: true }).click(); assert.strictEqual(creates, 0); assert.strictEqual(await page.locator('#newTaskTitle').getAttribute('aria-invalid'), 'true');
        for (const [id, value] of [['newTaskTitle', 'UI test task'], ['newTaskDescription', 'UI test requirements'], ['newTaskCriteria', 'UI test instructions'], ['newTaskDeadline', '2027-01-01T14:30']]) await page.locator('#' + id).fill(value);
        await page.locator('#createTaskModal .modal-footer').getByRole('button', { name: 'Next →', exact: true }).click();
        await page.locator('#selectAllStudents').click(); assert.strictEqual(await page.locator('#studentChips button').count(), 2);
        await page.locator('#studentChips button').first().click(); assert.strictEqual(await page.locator('#studentChips button').count(), 1);
        await page.locator('#studentSearch').fill('One'); assert.strictEqual(await page.locator('#studentOptions label').count(), 1);
        await page.locator('#clearStudents').click(); await page.locator('#selectAllStudents').click();
        await page.locator('#studentSearch').focus(); await page.keyboard.press('Escape'); await page.locator('dialog[open]').waitFor(); await page.locator('dialog[open] button').first().click(); assert.strictEqual(await page.locator('#newTaskTitle').inputValue(), 'UI test task');
        for (let i = 0; i < 3; i++) await page.locator('#createTaskModal .modal-footer').getByRole('button', { name: 'Next →', exact: true }).click();
        await page.locator('#publishTaskBtn').click(); await page.locator('dialog[open]').waitFor(); await page.locator('dialog[open] button').last().click(); await page.locator('#createTaskModal').waitFor({ state: 'hidden' }); assert.strictEqual(creates, 1);
        console.log('PASS real task modal validation, focus, searchable multiselect, chips, bulk confirmation and independent board cards');
        await page.setViewportSize({ width: 390, height: 844 }); await page.locator('[data-bs-target="#createTaskModal"]').click(); await page.locator('#createTaskModal.show').waitFor();
        const box = await page.locator('#createTaskModal .modal-dialog').boundingBox(); assert(box.width <= 390); await page.screenshot({ path: path.join(__dirname, 'task-modal-mobile.png'), animations: 'disabled' });
        console.log('PASS mobile task popup fits 390px viewport');
        await page.setViewportSize({ width: 1440, height: 1000 }); await page.screenshot({ path: path.join(__dirname, 'task-modal-desktop.png'), animations: 'disabled' });
        await page.setViewportSize({ width: 390, height: 844 });
        await page.goto(base + '/Company/ManageTasks.aspx'); await page.locator('.task-board-card').first().waitFor();
        await page.locator('[data-bs-target="#createTaskModal"]').click();
        for (const [id, value] of [['newTaskTitle', 'UI test task'], ['newTaskDescription', 'UI test requirements'], ['newTaskCriteria', 'UI test instructions'], ['newTaskDeadline', '2027-01-01T14:30']]) await page.locator('#' + id).fill(value);
        const nextStep = () => page.locator('#createTaskModal .modal-footer').getByRole('button', { name: 'Next →', exact: true }).click();
        await nextStep(); await page.locator('#selectAllStudents').click(); await nextStep(); await page.locator('#configureTaskExam').check();
        await page.locator('#composeExamTitle').fill('UI composed exam'); await page.locator('#composeExamOpens').fill('2027-01-01T10:00'); await page.locator('#composeExamCloses').fill('2027-01-01T12:00');
        await nextStep(); await page.getByText('Add at least one question.', { exact: true }).waitFor();
        await page.locator('#composeAddQuestion').click(); const firstQuestion = page.locator('.compose-question').first();
        await firstQuestion.getByLabel('Question text *', { exact: true }).fill('What does the image show?'); await firstQuestion.getByLabel('Answer option 1', { exact: true }).fill('Correct option'); await firstQuestion.getByLabel('Answer option 2', { exact: true }).fill('Other option');
        await firstQuestion.getByLabel('Question image', { exact: true }).setInputFiles({ name: 'question.png', mimeType: 'image/png', buffer: Buffer.from([137,80,78,71,13,10,26,10,1,2,3]) });
        await firstQuestion.locator('.compose-image-preview').waitFor(); await page.locator('#composeAddQuestion').click(); const secondQuestion = page.locator('.compose-question').nth(1);
        await secondQuestion.getByLabel('Answer format', { exact: true }).selectOption('trueFalse'); await secondQuestion.getByLabel('Question text *', { exact: true }).fill('The statement is true.');
        await page.screenshot({ path: path.join(__dirname, 'task-exam-builder-mobile.png'), animations: 'disabled' });
        await nextStep(); await page.locator('#createTaskResources').setInputFiles({ name: 'instructions.txt', mimeType: 'text/plain', buffer: Buffer.from('Isolated UI fixture resource') }); await nextStep();
        failStorage = true; await page.locator('#publishTaskBtn').click(); await page.getByText('Configured Storage bucket is unavailable.', { exact: true }).waitFor(); assert.strictEqual(creates, 1); assert.strictEqual(await page.locator('#newTaskTitle').inputValue(), 'UI test task'); failStorage = false;
        await page.locator('#publishTaskBtn').click(); await page.locator('dialog[open]').waitFor(); await page.locator('dialog[open] button').last().click(); await page.locator('#createTaskModal').waitFor({ state: 'hidden' });
        assert.strictEqual(composedExams.size, 2); assert.strictEqual(imageUploads, 2); assert.strictEqual(resourceUploads, 2); assert([...composedExams.values()].every(e => e.status === 'published'));
        console.log('PASS one task wizard creates independent exams, MCQ/True-False questions, image uploads and resources for both students');
        await page.goto(base + '/Student/MyExams.aspx'); await page.getByRole('button', { name: 'View exam', exact: true }).click();
        await page.locator('dialog[open] .exam-timer').waitFor(); assert(await page.getByRole('button', { name: '🔒 Start exam', exact: true }).isDisabled()); assert(/\d{2} : \d{2} : \d{2}/.test(await page.locator('.exam-timer').textContent())); assert.strictEqual(starts, 0);
        await page.screenshot({ path: path.join(__dirname, 'exam-locked-mobile.png'), animations: 'disabled' });
        console.log('PASS exam details popup uses server countdown, locked start, no pre-opening start request');
        await page.getByRole('button', { name: 'Close', exact: true }).click(); examClosed = true;
        await page.getByRole('button', { name: 'View exam', exact: true }).click(); assert(await page.getByRole('button', { name: 'Exam closed', exact: true }).isDisabled()); assert.strictEqual(starts, 0);
        await page.getByRole('button', { name: 'Close', exact: true }).click(); examClosed = false; examOpen = true; openingAt = Date.now() + 2000;
        await page.getByRole('button', { name: 'View exam', exact: true }).click(); assert(await page.getByRole('button', { name: '🔒 Start exam', exact: true }).isDisabled());
        await page.getByRole('button', { name: 'Start exam', exact: true }).click();
        await page.locator('dialog[open]').last().getByRole('button', { name: 'Start exam', exact: true }).click();
        assert(page.url().includes('/Student/MyExams.aspx'));
        console.log('PASS closed exam prevents starting; countdown unlocks after server confirmation without page navigation');
        await page.locator('#takeExamDialog').waitFor(); const savingAnswer = page.waitForRequest(req => req.url().includes('action=answers')); await page.getByLabel('True', { exact: true }).check(); await savingAnswer;
        const examCompanyMonitor = await context.newPage(); examCompanyMonitor.on('pageerror', error => errors.push(error.message));
        await examCompanyMonitor.goto(base + '/Company/ManageTasks.aspx'); await examCompanyMonitor.locator('.task-board-card').first().waitFor();
        const liveReview = await context.newPage(); liveReview.on('pageerror', error => errors.push(error.message));
        await liveReview.goto(base + '/Company/ManageExams.aspx'); await liveReview.getByRole('button', { name: 'Review and release results', exact: true }).click();
        await liveReview.getByText('No submitted examination attempts.', { exact: true }).waitFor();
        await page.getByRole('button', { name: 'Submit exam', exact: true }).click(); await page.getByRole('button', { name: 'Confirm submission', exact: true }).click();
        await page.getByText('Exam submitted successfully. Your result is awaiting supervisor approval.', { exact: true }).first().waitFor(); assert.strictEqual(storedAttempt.resultStatus, 'pendingApproval');
        assert(!await page.locator('#takeExamDialog').textContent().then(t => t.includes('Score 2')));
        console.log('PASS take-exam popup answers, progress, submission confirmation and pending-result message');
        await examCompanyMonitor.locator('[data-board-task-id="TEST-T-0"] .status-badge').filter({ hasText: 'Awaiting review' }).waitFor();
        await liveReview.getByText('Awaiting supervisor review · Result hidden', { exact: true }).waitFor();
        await liveReview.getByPlaceholder('Optional feedback to student').fill('Live result feedback');
        await liveReview.getByRole('button', { name: 'Approve and release result', exact: true }).click(); await liveReview.getByRole('button', { name: 'Approve and release', exact: true }).click();
        await page.locator('#takeExamDialog').getByText(/Score 2 \/ 2.*Live result feedback/).waitFor();
        await liveReview.getByRole('button', { name: 'Hide result', exact: true }).click(); await liveReview.locator('dialog[open]').last().getByRole('button', { name: 'Hide result', exact: true }).click();
        await page.locator('#takeExamDialog').getByText('Your result is currently hidden by your company supervisor.', { exact: true }).waitFor();
        assert(!await page.locator('#takeExamDialog').textContent().then(text => text.includes('Score 2 / 2') || text.includes('Live result feedback')));
        assert(!await page.locator('#notice').textContent().then(text => text.includes('Score 2 / 2') || text.includes('Live result feedback')));
        await liveReview.close(); await examCompanyMonitor.close();
        storedAttempt.resultStatus = 'pendingApproval'; tasks[0].status = 'assigned'; changeVersion++;
        console.log('PASS server change feed updates company board/review after exam submission and shows/hides the score in the still-open student submission popup without reload or BroadcastChannel');
        await page.goto(base + '/Company/ManageExams.aspx'); await page.getByRole('button', { name: 'Review and release results', exact: true }).click();
        await page.getByPlaceholder('Optional feedback to student').fill('UI test feedback'); await page.getByRole('button', { name: 'Approve and release result', exact: true }).click(); await page.getByRole('button', { name: 'Approve and release', exact: true }).click(); await page.getByRole('button', { name: 'Result released', exact: true }).waitFor();
        await page.goto(base + '/Student/MyExams.aspx'); await page.getByRole('button', { name: 'View exam', exact: true }).click(); await page.locator('dialog[open]').getByText(/Score 2 \/ 2/).waitFor();
        console.log('PASS supervisor review, explicit release confirmation, feedback and released student result');
        await page.goto(base + '/Company/ManageExams.aspx'); await page.getByRole('button', { name: 'Review and release results', exact: true }).click(); await page.getByRole('button', { name: 'Hide result', exact: true }).click(); await page.locator('dialog[open]').last().getByRole('button', { name: 'Hide result', exact: true }).click(); await page.getByRole('button', { name: 'Show result', exact: true }).waitFor();
        await page.goto(base + '/Student/MyExams.aspx'); await page.getByRole('button', { name: 'View exam', exact: true }).click(); await page.locator('dialog[open]').getByText('Your result is currently hidden by your company supervisor.', { exact: true }).waitFor(); assert(!await page.locator('dialog[open]').textContent().then(text => text.includes('Score 2 / 2')));
        console.log('PASS supervisor can hide a released result and the student receipt/UI no longer shows its score');
        await page.goto(base + '/Company/ManageExams.aspx'); await page.getByRole('button', { name: 'Review and release results', exact: true }).click();
        await page.getByRole('button', { name: 'Request new attempt', exact: true }).click();
        let revisionDialog = page.locator('dialog[open]').last();
        await revisionDialog.getByRole('button', { name: 'Review request', exact: true }).click(); assert.strictEqual(await revisionDialog.locator('textarea').getAttribute('aria-invalid'), 'true');
        await revisionDialog.locator('textarea').fill('Review the unanswered question'); await revisionDialog.locator('input').fill('2000-01-01T12:00');
        await revisionDialog.getByRole('button', { name: 'Review request', exact: true }).click(); assert.strictEqual(await revisionDialog.locator('input').getAttribute('aria-invalid'), 'true');
        const newDue = new Date(Date.now() + 86400000); newDue.setMinutes(newDue.getMinutes() - newDue.getTimezoneOffset()); await revisionDialog.locator('input').fill(newDue.toISOString().slice(0, 16));
        await revisionDialog.getByRole('button', { name: 'Review request', exact: true }).click(); await page.locator('dialog[open]').last().getByRole('button', { name: 'Request revision', exact: true }).click();
        await page.getByText('Not complete · New attempt requested', { exact: true }).waitFor(); assert.strictEqual(storedAttempt.resultStatus, 'revisionRequested');
        console.log('PASS examination revision popup validates required reason and future deadline, confirms and marks the previous result for revision');
        tasks[0].status = 'assigned'; changeVersion++; // Reset only the isolated fixture for the independent task-submission scenario below.
        const companyMonitor = await context.newPage(); companyMonitor.on('pageerror', error => errors.push(error.message)); await companyMonitor.goto(base + '/Company/ManageTasks.aspx'); await companyMonitor.locator('.task-board-card').first().waitFor();
        await page.goto(base + '/Student/MyTasks.aspx');
        await page.locator('#taskBoard [data-task-action="details"][data-task-id="TEST-T-0"]').click();
        await page.locator('#studentTaskInstructions').waitFor();
        for (const [tab, panel] of [['studentTabSubmission', 'studentPanelSubmission'], ['studentTabFiles', 'studentPanelFiles'], ['studentTabHistory', 'studentPanelHistory'], ['studentTabDetails', 'studentPanelDetails']]) { await page.locator('#' + tab).click(); assert(await page.locator('#' + panel).isVisible()); }
        await page.locator('#studentTabDetails').focus(); await page.keyboard.press('ArrowRight'); assert(await page.locator('#studentPanelSubmission').isVisible()); await page.keyboard.press('ArrowLeft');
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.screenshot({ path: path.join(__dirname, 'student-task-popup-desktop.png'), animations: 'disabled' });
        await page.locator('#studentTabExam').click(); await page.locator('#taskExamList').getByRole('button', { name: 'View exam', exact: true }).click();
        await page.locator('dialog[open] .exam-overview-grid').waitFor(); assert(page.url().includes('/Student/MyTasks.aspx'));
        await page.setViewportSize({ width: 390, height: 844 });
        assert((await page.locator('dialog[open]').boundingBox()).width <= 390);
        await page.screenshot({ path: path.join(__dirname, 'student-task-exam-popup-mobile.png'), animations: 'disabled' });
        await page.locator('dialog[open]').getByRole('button', { name: 'Close', exact: true }).click();
        storedAttempt = null;
        await page.locator('#taskExamList').getByRole('button', { name: 'View exam', exact: true }).click();
        await page.locator('dialog[open]').getByRole('button', { name: 'Start exam', exact: true }).click();
        await page.locator('dialog[open]').last().getByRole('button', { name: 'Start exam', exact: true }).click();
        await page.locator('#takeExamDialog').getByLabel('True', { exact: true }).check();
        await page.locator('#takeExamDialog').getByRole('button', { name: 'Save and close', exact: true }).click();
        await page.locator('dialog[open]').last().getByRole('button', { name: 'Save and return', exact: true }).click();
        await page.locator('#takeExamDialog').waitFor({ state: 'hidden' });
        assert.strictEqual(await page.locator('#submitModal').isVisible(), true);
        assert.strictEqual(storedAttempt.answers.Q1, true);
        await page.locator('#studentTabDetails').click();
        console.log('PASS student task tabs open the authorized exam in a responsive nested popup without leaving the task');
        await page.locator('#studentTaskInstructions').waitFor(); await page.locator('#studentTaskWorkAction').click(); await page.locator('dialog[open]').last().getByRole('button', { name: 'Start task', exact: true }).click();
        await page.locator('#confirmSubmitBtn').waitFor({ state: 'visible' });
        await page.locator('#workDesc').fill('Unsent work'); await page.locator('#submitModal .btn-close').click();
        await page.locator('dialog[open]').waitFor(); await page.locator('dialog[open] button').first().click();
        assert.strictEqual(await page.locator('#workDesc').inputValue(), 'Unsent work');
        await page.locator('#workDesc').fill('Completed student work'); await page.locator('#confirmSubmitBtn').click();
        await page.locator('dialog[open] button').last().click(); await page.locator('#submitModal').waitFor({ state: 'hidden' });
        assert.strictEqual(tasks[0].status, 'submitted'); assert.strictEqual(tasks[1].status, 'assigned');
        await companyMonitor.locator('[data-board-task-id="TEST-T-0"] .status-badge').filter({ hasText: 'Awaiting review' }).waitFor(); await companyMonitor.close();
        console.log('PASS student submission updates the open company board automatically without manual refresh');
        console.log('PASS student board starts task, displays instructions, retains unsent work and confirms description-only submission independently');
        await page.goto(base + '/Company/ManageTasks.aspx'); await page.locator('[data-board-task-id="TEST-T-0"] [data-task-action="review"]').click(); await page.locator('[data-company-tab="grading"]').click(); await page.locator('#approveTaskBtn').click(); assert.strictEqual(await page.locator('#gradeInput').getAttribute('aria-invalid'), 'true'); assert(await page.locator('#gradeInput + .field-error').isVisible());
        console.log('PASS missing task grade stays in the popup with a red field and inline validation');
        failTasks = true; await page.setViewportSize({ width: 1600, height: 1000 });
        for (const role of ['Company/ManageTasks', 'Student/MyTasks']) {
            await page.goto(base + '/' + role + '.aspx'); await page.locator('#taskBoard .board-state-error').waitFor();
            assert.strictEqual(await page.locator('#taskBoard .task-board-column').count(), 4);
            assert.deepStrictEqual(await page.locator('#taskBoard .task-board-column h5').allTextContents(), ['To Do', 'In Progress', 'Awaiting Review', 'Done']);
            assert.strictEqual(await page.locator('#taskBoard .task-board-card').count(), 0);
            assert.strictEqual(await page.locator('#taskList').isVisible(), false);
            assert((await page.locator('.stat-details h3').allTextContents()).every(value => value === '—'));
            const boxes = await page.locator('#taskBoard .task-board-column').evaluateAll(nodes => nodes.map(node => node.getBoundingClientRect().top)); assert(boxes.every(top => top === boxes[0]));
            await page.screenshot({ path: path.join(__dirname, role.startsWith('Company') ? 'company-board-unavailable.png' : 'student-board-unavailable.png'), animations: 'disabled' });
            if (role.startsWith('Company')) { failTasks = false; await page.getByRole('button', { name: 'Retry connection' }).click(); await page.locator('.task-board-card').first().waitFor(); failTasks = true; }
        }
        console.log('PASS four aligned Trello columns remain visible for both roles on Firebase failure, with unknown counts and working retry');
        assert.deepStrictEqual(errors, []); console.log('PASS no browser JavaScript errors; all external traffic blocked');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(() => server.close());
