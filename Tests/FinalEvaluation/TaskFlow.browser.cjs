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
    try { let content = fs.readFileSync(file); if (file.endsWith('.aspx')) content = content.toString().replace(/<script runat="server">[\s\S]*?<\/script>/g,'').replace(/<%@[^%]*%>/g, ''); res.setHeader('Content-Type', file.endsWith('.js') ? 'application/javascript' : file.endsWith('.css') ? 'text/css' : file.endsWith('.aspx') ? 'text/html; charset=utf-8' : 'application/octet-stream'); res.end(content); } catch (_) { res.writeHead(404); res.end(); }
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
        let failExamReadiness = false; let examSaveCalls = 0; let partialTasksOnce = false, losePublishAckOnce = false, failPublishOnce = false; const creationBatches = [];
        const operations = new Map(), assignments = new Map(), uploadedResources = new Map(), uploadedImages = new Map();
        const multipartField = (req,name) => { const match = req.postDataBuffer().toString().match(new RegExp('name="' + name + '"\\r\\n\\r\\n([^\\r]+)')); return match?.[1]; };
        function recovery(batchId) {
            const plan = operations.get(batchId).plan;
            const progress = plan.payload.placementIds.map(placementId => {
                const id = assignments.get(batchId + '|' + placementId) || 'BULK-' + batchId + '-' + placementId, examId = 'UI-COMPOSED-' + id;
                const resourceFiles = uploadedResources.get(id) || {};
                return {placementId,taskId:id,taskSaved:assignments.has(batchId+'|'+placementId),resourceFiles,resourcesReady:(plan.resources||[]).every(file=>resourceFiles[file.id]),examId,examPublished:composedExams.get(examId)?.status==='published',images:uploadedImages.get(examId)||{}};
            });
            return {batchId,plan,progress,status:progress.every(row=>row.taskSaved&&row.resourcesReady&&(plan.payload.taskType!=='exam'||row.examPublished))?'complete':'incomplete'};
        }
        let creates = 0, starts = 0, examOpen = false, openingAt = null, examClosed = false, storedAttempt = null, failTasks = false, failStorage = false, imageUploads = 0, resourceUploads = 0, answerSaveInFlight = false, changeVersion = 0; const positions = new Map(), composedExams = new Map(), submissions = [];
        await context.route('**/*', async route => {
            const req = route.request(), url = new URL(req.url());
            if (url.origin !== base) return route.abort();
            if (url.pathname.endsWith('/TasksApi.ashx')) {
                if (failTasks) return route.fulfill({ status: 503, json: { message: 'Task connection unavailable during UI test.' } });
                const action = url.searchParams.get('action'); let body = {};
                if (action === 'changes') { await new Promise(resolve => setTimeout(resolve, 250)); return route.fulfill({ json: { version: changeVersion, changed: Number(url.searchParams.get('version')) !== changeVersion, connected: true } }).catch(() => {}); }
                if (!action) body = { tasks: (req.headers().referer || '').includes('/Student/') ? tasks.filter(task => task.studentId === students[0].id) : tasks, submissions, students };
                else if (action === 'beginCreation') { const plan=req.postDataJSON(); if(!operations.has(plan.payload.batchId))operations.set(plan.payload.batchId,{plan:structuredClone(plan),status:'incomplete'});body=recovery(plan.payload.batchId); }
                else if (action === 'creationOperations') body = {operations:[...operations.entries()].filter(([,record])=>record.status!=='complete').map(([batchId,record])=>({batchId,title:record.plan.payload.title}))};
                else if (action === 'creationOperation') body = recovery(url.searchParams.get('batchId'));
                else if (action === 'checkpointCreation') { const batchId=req.postDataJSON().batchId;body=recovery(batchId);operations.get(batchId).status=body.status; }
                else if (action === 'board') body = { placementId: url.searchParams.get('placementId'), canMove: true, columns, tasks: tasks.filter(t => t.placementId === url.searchParams.get('placementId')).map(t => ({ ...t, columnId: positions.get(t.id) || 'todo', dueDate: t.deadline, order: 1 })) };
                else if (action === 'move') { const payload = req.postDataJSON(); assert.strictEqual(payload.placementId, tasks.find(t => t.id === payload.taskId).placementId); positions.set(payload.taskId, payload.columnId); body = { success: true }; }
                else if (action === 'bulkCreate') { const input=req.postDataJSON();assert.strictEqual(input.description,'UI test requirements');assert.strictEqual(input.criteria,'UI test instructions');creates++;creationBatches.push(input.batchId);body={success:!partialTasksOnce,results:input.placementIds.map((placementId,index)=>{if(partialTasksOnce&&index===1)return {success:false,placementId,message:'Isolated second assignment failure'};const id='BULK-'+input.batchId+'-'+placementId;assignments.set(input.batchId+'|'+placementId,id);return {success:true,id,placementId};})};partialTasksOnce=false; }
                else if (action === 'attachments') body = { files: [] };
                else if (action === 'start') { tasks.find(t => t.id === req.postDataJSON().assignmentId).status = 'inprogress'; changeVersion++; body = { success: true }; }
                else if (action === 'submit') { const input = req.postDataJSON(); assert.strictEqual(input.description, 'Completed student work'); assert.strictEqual(input.link, 'https://example.test/submitted-work'); tasks.find(t => t.id === input.assignmentId).status = 'submitted'; submissions.push({ taskId: input.assignmentId, revision: 1, description: input.description, createdAt: new Date().toISOString() }); changeVersion++; body = { success: true }; }
                else if (action === 'attach') { resourceUploads++;const id=multipartField(req,'taskId');assert(id);uploadedResources.set(id,{'resource-0':'UI-RESOURCE-'+resourceUploads});body={id:'UI-RESOURCE-'+resourceUploads}; }
                else if (action === 'storageAvailability') return route.fulfill({ status: failStorage ? 404 : 200, json: failStorage ? { message: 'Configured Storage bucket is unavailable.' } : { available: true } });
                return route.fulfill({ json: body });
            }
            if (url.pathname.endsWith('/TaskExamsApi.ashx')) {
                let body = {};
                const action = url.searchParams.get('action');
                if (action === 'readiness') { assert.strictEqual(req.method(),'GET');assert.strictEqual(url.searchParams.get('images'),'true');return route.fulfill({status:failExamReadiness?503:200,json:failExamReadiness?{code:'EXAM_DEPENDENCY_DISABLED',message:'Exam changes are disabled (TaskExamsLiveWritesEnabled=false). The existing Firebase connection is enabled.'}:{ready:true,compatible:true}}); }
                if (action === 'create') { const payload = req.postDataJSON(), id = 'UI-COMPOSED-' + payload.taskId; assert(payload.creationKey); assert.strictEqual(payload.questions.length, 2); if (!composedExams.has(id)) composedExams.set(id, { id, status: 'draft', draftRevision: 1, draft: payload }); return route.fulfill({ json: { id, exam: composedExams.get(id) } }); }
                if (action === 'image' && req.method() === 'POST') { imageUploads++;const examId=multipartField(req,'examId'),questionId=multipartField(req,'questionId');assert(examId&&questionId);uploadedImages.set(examId,{...(uploadedImages.get(examId)||{}),[questionId]:'UI-IMAGE-'+imageUploads});return route.fulfill({json:{id:'UI-IMAGE-'+imageUploads,status:'ready'}}); }
                if (action === 'save') { examSaveCalls++; const payload = req.postDataJSON(), exam = composedExams.get(payload.examId); assert(exam); assert(payload.draft.questions[0].imageFileId); assert.strictEqual(payload.draft.questions[1].type, 'trueFalse'); exam.draft = payload.draft; exam.draftRevision++; return route.fulfill({ json: { draftRevision: exam.draftRevision } }); }
                if (action === 'publish') { const exam = composedExams.get(req.postDataJSON().examId); assert(exam.draft.questions[0].imageFileId); if (failPublishOnce && exam.id.endsWith('2')) { failPublishOnce = false; return route.fulfill({ status: 503, json: { message: 'Isolated publication failure' } }); } exam.status = 'published'; if (losePublishAckOnce) { losePublishAckOnce = false; return route.abort('failed'); } return route.fulfill({ json: { version: 1 } }); }
                const definition = { title: 'UI examination fixture', instructions: 'UI-only fixture', opensAt: new Date(openingAt || Date.now() + (examOpen ? -3600000 : 3600000)).toISOString(), closesAt: new Date(Date.now() + (examClosed ? -1000 : 7200000)).toISOString(), durationMinutes: 30, attemptLimit: 1, totalPoints: 2, questionCount: 1 };
                const receipt = () => { const copy = { ...storedAttempt }; if (copy.resultStatus !== 'released') { delete copy.score; delete copy.maximumScore; delete copy.feedback; delete copy.releasedAt; } return copy; };
                if (action === 'list') body = { 'UI-EXAM': { id: 'UI-EXAM', status: 'published', title: definition.title, taskId: 'TEST-T-0', serverNow: new Date().toISOString(), versionData: definition } };
                if (action === 'myAttempts') body = storedAttempt ? { 'UI-ATTEMPT': receipt() } : {};
                if (action === 'eligibility') body = { eligible: examOpen, serverNow: new Date().toISOString() };
                if (action === 'start') { starts++; storedAttempt = { id: 'UI-ATTEMPT', examId: 'UI-EXAM', studentId: students[0].id, status: 'inProgress', startedAt: new Date().toISOString(), deadlineAt: new Date(Date.now() + 1800000).toISOString(), serverNow: new Date().toISOString(), answers: {} }; body = receipt(); }
                if (action === 'exam' && composedExams.has(url.searchParams.get('examId'))) return route.fulfill({json:composedExams.get(url.searchParams.get('examId'))});
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

        const output=path.resolve(__dirname,'../Reports/TaskFlow'); fs.mkdirSync(output,{recursive:true});
        await page.setViewportSize({width:1440,height:1000});
        await page.goto(base+'/Company/ManageTasks.aspx'); await page.locator('.task-board-card').first().waitFor();
        await page.locator('[data-bs-target="#createTaskModal"]').click();
        assert.equal(await page.locator('.compose-tabs button').count(),3);
        for(const [id,value] of [['newTaskTitle','API integration task'],['newTaskDescription','Build and document the application endpoints.'],['newTaskCriteria','Correctness, clarity and test coverage.'],['newTaskStart','2030-01-01T09:00'],['newTaskDeadline','2030-01-08T17:00']])await page.locator('#'+id).fill(value);
        await page.locator('#createTaskModal').evaluate(el=>Promise.all(el.getAnimations({subtree:true}).map(a=>a.finished.catch(()=>{}))));
        await page.screenshot({path:path.join(output,'company-task-details.png')});
        const next=()=>page.locator('#createTaskModal .modal-footer').getByRole('button',{name:'Next →',exact:true}).click();
        await next(); await page.locator('#selectAllStudents').click(); await page.locator('#configureTaskExam').check();
        await page.locator('#composeExamTitle').fill('API fundamentals'); await page.locator('#composeExamOpens').fill('2030-01-08T09:00'); await page.locator('#composeExamCloses').fill('2030-01-08T11:00');
        await page.locator('#composeAddQuestion').click(); await page.getByLabel('Question text *',{exact:true}).fill('Which method reads a resource?');
        await page.getByLabel('Answer option 1',{exact:true}).fill('GET'); await page.getByLabel('Answer option 2',{exact:true}).fill('POST');
        await page.locator('#composeAddQuestion').click(); await page.locator('.compose-question[open]').getByLabel('Answer format').selectOption('trueFalse');
        await page.locator('.compose-question[open]').getByLabel('Question text *',{exact:true}).fill('An API can return JSON.');
        assert.equal(await page.locator('.compose-question[open]').count(),1);
        await page.getByRole('button',{name:'Preview examination',exact:true}).click();
        await page.locator('.task-flow-header').getByRole('heading',{name:'Examination preview',exact:true}).waitFor();
        assert.equal(await page.locator('dialog[open]').count(),0); assert.equal(await page.locator('.modal.show').count(),1);
        assert.equal(await page.locator('[data-task-flow-active="true"]:visible').count(),1);
        await page.getByRole('button',{name:'Back to builder',exact:true}).click();
        assert.equal(await page.locator('#newTaskTitle').inputValue(),'API integration task'); assert.equal(await page.locator('.compose-question').count(),2);
        await page.locator('#createTaskModal .modal-body').evaluate(el=>{el.scrollTop=0;});
        await page.screenshot({path:path.join(output,'company-students-exam.png')});
        await next(); await page.locator('#publishTaskBtn').waitFor({state:'visible'});
        await page.screenshot({path:path.join(output,'company-task-review.png')}); assert.equal(creates,0);
        tasks[0].taskType='exam'; examOpen=true;
        await page.goto(base+'/Student/MyTasks.aspx'); await page.getByRole('button',{name:'View',exact:true}).first().click();
        await page.locator('#taskExamList').getByRole('button',{name:'View exam',exact:true}).click();
        await page.locator('.task-flow-surface .exam-overview').waitFor();
        assert.equal(await page.locator('dialog[open]').count(),0); assert.equal(await page.locator('.modal.show').count(),1);
        await page.screenshot({path:path.join(output,'student-exam-overview.png')});
        await page.getByRole('button',{name:'Start exam',exact:true}).click();
        assert.equal(starts,0); assert.equal(await page.locator('dialog[open]').count(),0);
        await page.locator('.workspace-confirmation .workspace-dialog-actions').getByRole('button',{name:'Start exam',exact:true}).click();
        await page.waitForURL('**/Student/MyExams.aspx?examId=UI-EXAM&resume=1');
        await page.locator('#takeExamDialog[open]').waitFor(); assert.equal(starts,1,'Resume URL must never start another attempt'); assert(!/d /.test(await page.locator('#takeExamDialog .exam-timer').innerText()),'Resume must synchronize server time');
        assert.equal(await page.locator('.task-flow-fullscreen').count(),1); assert.equal(await page.locator('dialog[open]').count(),1);
        await page.getByLabel('True',{exact:true}).check(); await page.screenshot({path:path.join(output,'student-exam-full-page.png')});
        await page.setViewportSize({width:390,height:844});
        assert((await page.locator('#takeExamDialog').boundingBox()).width <= 390);
        await page.screenshot({path:path.join(output,'student-exam-mobile.png')}); await page.setViewportSize({width:1440,height:1000});
        await page.getByRole('button',{name:'Submit exam',exact:true}).click();
        assert.equal(await page.locator('dialog[open]').count(),1);
        await page.getByRole('button',{name:'Confirm submission',exact:true}).click();
        await page.locator('#takeExamDialog').getByText(/awaiting supervisor approval/).waitFor();
        await page.goto(base+'/Company/ManageTasks.aspx'); await page.locator('[data-board-task-id="TEST-T-0"] [data-task-action="review"]').click();
        await page.getByRole('button',{name:'Review exam and result visibility',exact:true}).click();
        await page.locator('.exam-answer-list details').first().waitFor();
        assert.equal(await page.locator('dialog[open]').count(),0); assert.equal(await page.locator('.modal.show').count(),1);
        await page.locator('.exam-answer-list summary').first().click();
        await page.screenshot({path:path.join(output,'company-exam-review.png')});
        await page.getByRole('button',{name:'Approve and release result',exact:true}).click();
        assert.equal(await page.locator('dialog[open]').count(),0);
        await page.locator('.workspace-confirmation .workspace-dialog-actions').getByRole('button',{name:'Cancel',exact:true}).click();
        assert.equal(storedAttempt.resultStatus,'pendingApproval');
        await page.setViewportSize({width:390,height:844});
        const bounds=await page.locator('[data-task-flow-active="true"]').boundingBox(); assert(bounds.width<=390 && bounds.x>=0);
        await page.screenshot({path:path.join(output,'company-exam-review-mobile.png')});
        tasks[0].id='TEST-STANDARD'; tasks[0].taskType='standard'; tasks[0].status='assigned';
        await page.setViewportSize({width:1440,height:1000}); await page.goto(base+'/Student/MyTasks.aspx');
        await page.locator('#taskBoard [data-task-action="details"][data-task-id="TEST-STANDARD"]').click();
        await page.locator('#studentTaskWorkAction').click(); assert.equal(await page.locator('dialog[open]').count(),0);
        await page.locator('.workspace-confirmation .workspace-dialog-actions').getByRole('button',{name:'Start task',exact:true}).click();
        await page.locator('#workDesc').fill('Completed student work'); await page.locator('#workLink').fill('https://example.test/submitted-work');
        await page.screenshot({path:path.join(output,'student-task-work.png')});
        await page.locator('#submitModal .btn-close').click();
        await page.locator('.workspace-confirmation .workspace-dialog-actions').getByRole('button',{name:'Cancel',exact:true}).click();
        assert.equal(await page.locator('#workDesc').inputValue(),'Completed student work','Returning from confirmation must preserve the draft');
        await page.locator('#confirmSubmitBtn').click(); assert.equal(await page.locator('dialog[open]').count(),0,'Standard task submission must remain in the same window');
        await page.locator('.workspace-confirmation .workspace-dialog-actions button.primary').click();
        await page.locator('#submitModal').waitFor({state:'hidden'}); assert.equal(tasks[0].status,'submitted');
        assert.deepEqual(errors,[]);console.log('PASS three-step creation, question accordion, draft preservation, single-window preview/review/confirmations, dedicated exam navigation/resume without duplicate attempts, submission and mobile layout. All APIs isolated.');
    } finally { await browser.close(); }
})().catch(error=>{console.error(error);process.exitCode=1;}).finally(()=>server.close());

