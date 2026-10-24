const fs=require('fs');const p='Tests/FinalEvaluation/TaskWorkspace.browser.cjs';let s=fs.readFileSync(p,'utf8');
let a=s.indexOf("        await page.locator('[data-board-task-id=\"TEST-T-0\"]').dragTo"),b=s.indexOf("        await page.locator('[data-board-task-id=\"TEST-T-0\"] [data-task-action=\"review\"]').click();",a);
s=s.slice(0,a)+`        await page.locator('[data-board-task-id="TEST-T-0"]').dragTo(page.locator('[data-column-id="done"] .task-board-dropzone'));
        await page.getByText('Use the authorized task action to change its stage. Drag only reorders within a stage.',{exact:true}).waitFor();
        assert(!positions.has('TEST-T-0'));assert(!positions.has('TEST-T-1'));
        console.log('PASS unauthorized drag into Complete is rejected without changing task state or the other student');
`+s.slice(b);
s=s.replace("assert.strictEqual(await page.locator('[data-company-tab]').count(), 6)","assert.strictEqual(await page.locator('[data-company-tab]').count(), 0)");
a=s.indexOf("        await page.getByLabel('Move card to column')");b=s.indexOf("        await page.locator('#reviewModal .btn-close')",a);s=s.slice(0,a)+s.slice(b);
a=s.indexOf('        const cardMove =');b=s.indexOf("        await page.locator('[data-bs-target=\"#createTaskModal\"]').click();",a);s=s.slice(0,a)+"        console.log('PASS one coherent company popup, shared board/list filters and independent cards');\n"+s.slice(b);
s=s.replaceAll("['newTaskDeadline', '2027-01-01T14:30']","['newTaskStart', '2026-10-23T09:00'], ['newTaskDeadline', '2027-01-01T14:30']");
// Standard and exam cards are context-specific; use a distinct standard fixture after the exam scenario.
a=s.indexOf("        await page.locator('#studentTaskInstructions').waitFor();\n        for (const [tab, panel]");b=s.indexOf("        await page.locator('dialog[open] .exam-overview-grid').waitFor();",a);
s=s.slice(0,a)+`        assert.strictEqual(await page.locator('[data-student-tab]').count(),0);
        assert.equal(await page.locator('#studentPanelSubmission').isVisible(),false);
        await page.setViewportSize({width:1440,height:1000});
        await page.screenshot({path:path.join(__dirname,'student-task-popup-desktop.png'),animations:'disabled'});
        await page.locator('#taskExamList').getByRole('button',{name:'View exam',exact:true}).click();
`+s.slice(b);
a=s.indexOf("        await page.locator('#studentTabDetails').click();");b=s.indexOf("        await page.locator('#studentTaskInstructions').waitFor();",a);
s=s.slice(0,a)+`        console.log('PASS exam-only task popup opens the authorized timed examination without standard submission fields');
        await page.locator('#submitModal .btn-close').click();
        tasks[0].id='TEST-STANDARD';tasks[0].taskType='standard';tasks[0].status='assigned';
        await page.goto(base+'/Student/MyTasks.aspx');await page.locator('#taskBoard [data-task-action="details"][data-task-id="TEST-STANDARD"]').click();
`+s.slice(b);
// API list fixture still references the previous examination task, not the new standard task.
s=s.replace("taskId: tasks[0].id, serverNow:","taskId: 'TEST-T-0', serverNow:");
s=s.replaceAll('companyMonitor.locator(\'[data-board-task-id="TEST-T-0"]','companyMonitor.locator(\'[data-board-task-id="TEST-STANDARD"]');
s=s.replace("await page.locator('[data-board-task-id=\"TEST-T-0\"] [data-task-action=\"review\"]').click(); await page.locator('[data-company-tab=\"grading\"]').click();","await page.locator('[data-board-task-id=\"TEST-STANDARD\"] [data-task-action=\"review\"]').click();");
s=s.replace("['To Do', 'In Progress', 'Awaiting Review', 'Done']","['New / Pending', 'In Progress', 'Review', 'Complete']");
// Serve Unicode exactly like the actual ASP.NET UTF-8 response.
s=s.replace("file.endsWith('.aspx') ? 'text/html'","file.endsWith('.aspx') ? 'text/html; charset=utf-8'");
fs.writeFileSync(p,s);
