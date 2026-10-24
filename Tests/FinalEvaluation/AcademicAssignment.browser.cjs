const fs = require('fs'), path = require('path'), http = require('http'), assert = require('assert');
const { chromium } = require(path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const root = path.resolve(__dirname, '../../University-Internship-Management-System');
const server = http.createServer((req, res) => {
    const f = path.resolve(root, '.' + new URL(req.url, 'http://localhost').pathname);
    if (!f.startsWith(root + path.sep)) return res.end();
    try {
        let b = fs.readFileSync(f);
        if (f.endsWith('.aspx')) b = b.toString().replace(/<%@[^%]*%>/g, '').replace(/<%=[\s\S]*?%>/g, '');
        res.setHeader('Content-Type', f.endsWith('.js') ? 'application/javascript' : f.endsWith('.css') ? 'text/css' : 'text/html'); res.end(b);
    } catch { res.writeHead(404); res.end(); }
});
(async () => {
    await new Promise(r => server.listen(0, '127.0.0.1', r));
    const base = 'http://127.0.0.1:' + server.address().port;
    const browser = await chromium.launch({ executablePath: process.env.PROGRAMFILES + '/Google/Chrome/Application/chrome.exe', headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } }), errors = [];
        page.on('pageerror', e => errors.push(e.message));
        const fixture = { departments: { cs: 'Computer Science', se: 'Software Engineering' }, groups: [], professors: [{ id: 'AC', name: 'CS Professor', department: 'cs' }, { id: 'FREE', name: 'New Professor', department: '' }, { id: 'OTHER', name: 'SE Professor', department: 'se' }], students: [
            { id: 'S1', name: 'First Student', department: 'cs', groupIds: [], supervisorIds: [] },
            { id: 'S2', name: 'Second Student', department: 'cs', groupIds: [], supervisorIds: [] },
            { id: 'S3', name: 'Wrong Major', department: 'se', groupIds: [], supervisorIds: [] },
            { id: 'S4', name: 'Assigned Student', department: 'cs', groupIds: ['OTHER'], supervisorIds: ['OTHER'] }
        ] };
        let writes = 0, failedPayload;
        await page.route('**/*', route => {
            const u = new URL(route.request().url());
            if (u.origin !== base) return route.abort();
            if (!u.pathname.endsWith('AcademicAssignmentsApi.ashx')) return route.continue();
            if (route.request().method() !== 'POST') return route.fulfill({ json: fixture });
            writes++; const p = route.request().postDataJSON();
            assert.deepStrictEqual(p.studentIds, ['S1', 'S2']); assert.strictEqual(p.supervisorId, 'AC'); assert.strictEqual(p.department, 'cs');
            if (writes === 1) { failedPayload = p; return route.fulfill({ status: 503, json: { message: 'Test connection failure. Retry unchanged selections.' } }); }
            assert.strictEqual(p.requestId, failedPayload.requestId);
            fixture.groups = [{ id: 'GRP-test', name: p.name, department: 'cs', supervisorId: 'AC', students: { S1: true, S2: true } }];
            fixture.students.slice(0, 2).forEach(s => { s.groupIds = ['GRP-test']; s.supervisorIds = ['AC']; });
            return route.fulfill({ json: { saved: true, groupId: 'GRP-test' } });
        });
        await page.goto(base + '/Coordinator/Assign_Supervisors.aspx');
        const add = page.getByRole('button', { name: '+ Add group', exact: true });
        await add.click();
        assert(await page.locator('#assignmentDepartment').isDisabled()); assert(await page.locator('#studentSearch').isDisabled());
        await page.locator('#assignmentSave').click(); await page.getByText('Choose an eligible academic supervisor first.').waitFor();
        await page.locator('#assignmentProfessor').selectOption('FREE');
        assert.strictEqual(await page.locator('#assignmentDepartment option').count(), 3);
        await page.locator('#assignmentDepartment').selectOption('se');
        assert.strictEqual(await page.locator('.assignment-student').count(), 1);
        await page.locator('#assignmentProfessor').selectOption('AC');
        assert.strictEqual(await page.locator('#assignmentDepartment option').count(), 2);
        await page.locator('#assignmentDepartment').selectOption('cs');
        assert.strictEqual(await page.locator('.assignment-student').count(), 3);
        assert.strictEqual(await page.locator('.assignment-student input:disabled').count(), 1);
        await page.locator('#assignmentSave').click(); await page.getByText('Select at least one available student.').waitFor();
        await page.locator('#studentSearch').fill('S1'); assert.strictEqual(await page.locator('.assignment-student').count(), 1);
        await page.locator('#studentSearch').fill(''); await page.locator('#studentSelectAll').click();
        assert.strictEqual(await page.locator('.assignment-student input:checked').count(), 2);
        await page.screenshot({ path: path.join(process.env.TEMP, 'uims-academic-group-desktop.png') });
        await page.locator('#assignmentSave').click(); await page.getByText('Test connection failure. Retry unchanged selections.').waitFor();
        assert.strictEqual(writes, 1); assert(await page.locator('#assignmentDialog').isVisible());
        await page.locator('#assignmentSave').click(); await page.getByText('Group saved. The selected students are assigned to their academic supervisor.').waitFor();
        assert.strictEqual(writes, 2); assert.strictEqual(await page.locator('#assignmentRows tr').count(), 1);
        await page.getByRole('button', { name: 'View / add students' }).click();
        assert(await page.locator('#assignmentProfessor').isDisabled()); assert(await page.locator('#assignmentDepartment').isDisabled());
        assert.strictEqual(await page.locator('.assignment-student input:disabled').count(), 3);
        await page.locator('#assignmentClose').click();
        await page.locator('#departmentFilter').selectOption('se'); await page.getByText('No groups yet in this view. Add a group to assign students.').waitFor();
        await page.setViewportSize({ width: 390, height: 844 }); await add.click();
        await page.locator('#assignmentProfessor').selectOption('AC'); await page.locator('#assignmentDepartment').selectOption('cs');
        const box = await page.locator('#assignmentDialog').boundingBox(); assert(box.width <= 390 && box.x >= 0);
        assert(await page.locator('#assignmentDialog').evaluate(e => e.scrollWidth <= e.clientWidth));
        await page.screenshot({ path: path.join(process.env.TEMP, 'uims-academic-group-mobile.png') });
        assert.deepStrictEqual(errors, []);
        console.log('PASS isolated group UI: supervisor-first ordering, single-major selection, student names/IDs/search, assignment locks, bulk selection, validation, retry ID reuse, persisted reload, group management, major filter and mobile popup. No real Firebase writes.');
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; }).finally(() => server.close());
