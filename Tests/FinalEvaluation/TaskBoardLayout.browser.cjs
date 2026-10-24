// Offline geometry and scrolling tests against the actual task pages and scripts.
const fs = require('fs'), path = require('path'), http = require('http'), assert = require('assert');
const { chromium } = require(path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const root = path.resolve(__dirname, '../../University-Internship-Management-System');
const server = http.createServer((req, res) => {
    const file = path.resolve(root, '.' + new URL(req.url, 'http://localhost').pathname);
    if (!file.startsWith(root + path.sep)) { res.writeHead(404); return res.end(); }
    try { let body = fs.readFileSync(file); if (file.endsWith('.aspx')) body = body.toString().replace(/<script runat="server">[\s\S]*?<\/script>/g, '').replace(/<%@[^%]*%>/g, ''); res.setHeader('Content-Type', file.endsWith('.js') ? 'application/javascript' : file.endsWith('.css') ? 'text/css' : 'text/html'); res.end(body); } catch { res.writeHead(404); res.end(); }
});
(async () => {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const base = 'http://127.0.0.1:' + server.address().port;
    const browser = await chromium.launch({ executablePath: process.env.PROGRAMFILES + '/Google/Chrome/Application/chrome.exe', headless: true });
    try {
        let tasks = [], studentRole = false, moves = [];
        const student = { id: 'ST-OFFLINE', name: 'Offline student', placementId: 'PL-OFFLINE' };
        const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
        await context.route('**/*', async route => {
            const req = route.request(), url = new URL(req.url());
            if (url.origin !== base) return route.abort();
            if (url.pathname.endsWith('TaskExamsApi.ashx')) return route.fulfill({ json: {} });
            if (!url.pathname.endsWith('TasksApi.ashx')) return route.continue();
            const action = url.searchParams.get('action'); let body = {};
            if (!action) body = { tasks, students: [student], submissions: [], identity: studentRole ? student.id : 'SUP-OFFLINE', serverNow: new Date().toISOString() };
            if (action === 'board') body = { placementId: student.placementId, canMove: !studentRole, columns: [{ id: 'todo' }, { id: 'inProgress' }, { id: 'review' }, { id: 'done' }], tasks: tasks.map((t, index) => ({ ...t, order: index * 1024 })) };
            if (action === 'changes') { await new Promise(resolve => setTimeout(resolve, 100)); body = { changed: false, version: 1, connected: true }; }
            if (action === 'move') { moves.push(req.postDataJSON()); body = { success: true }; }
            return route.fulfill({ json: body }).catch(() => {});
        });
        const page = await context.newPage(), errors = []; page.on('pageerror', error => errors.push(error.message));
        const sizes = new Map();
        for (const role of ['Company/ManageTasks', 'Student/MyTasks']) {
            studentRole = role.startsWith('Student');
            for (const count of [0, 5, 20, 50]) {
                tasks = ['assigned', 'inprogress', 'submitted', 'completed'].flatMap((status, lane) => Array.from({ length: count }, (_, n) => ({ id: 'TASK-' + lane + '-' + n, title: 'Task ' + lane + ' / ' + n, description: 'Detailed instructions remain readable without truncation.', status, studentId: student.id, studentName: student.name, placementId: student.placementId, taskType: 'standard', startsAt: new Date().toISOString(), deadline: new Date(Date.now() + 86400000).toISOString(), maximum: 100 })));
                await page.goto(base + '/' + role + '.aspx');
                await page.locator('#taskBoard .task-board-column').first().waitFor();
                if (count) await page.locator('#taskBoard .task-card').first().waitFor();
                if (!count && studentRole) await page.waitForFunction(id => document.querySelector('#taskBoard .board-state-notice')?.textContent.includes('No tasks are assigned to student ' + id), student.id);
                await page.waitForFunction(() => !document.getElementById('taskBoard').querySelector('.board-state-loading'));
                const lanes = await page.locator('#taskBoard .task-board-column').evaluateAll(nodes => nodes.map(node => ({ height: node.getBoundingClientRect().height, color: getComputedStyle(node).backgroundColor, count: node.querySelectorAll('.task-card').length })));
                assert.strictEqual(await page.locator('#taskBoardToolbar select').count(),2);
                assert.strictEqual(await page.locator('#boardPlacementSelect,#batchFilter,#deadlineFilter').count(),0);
                const toolbar = await page.locator('#taskBoardToolbar').evaluate(host => { const controls=[...host.querySelectorAll('.task-filter-field,.task-view-toggle')].map(node=>node.getBoundingClientRect());return {aligned:controls.every(control=>Math.abs(control.bottom-controls[0].bottom)<2),adjacent:controls[2].left-controls[1].right<=16,fits:controls.every(control=>control.left>=host.getBoundingClientRect().left&&control.right<=host.getBoundingClientRect().right)}; });
                assert(toolbar.aligned&&toolbar.adjacent&&toolbar.fits);
                assert.strictEqual(lanes.length, 4); assert(lanes.every(lane => lane.color === 'rgb(255, 255, 255)'));
                assert(lanes.every(lane => Math.abs(lane.height - lanes[0].height) < 1));
                if (!sizes.has(role)) sizes.set(role, lanes[0].height);
                assert(Math.abs(sizes.get(role) - lanes[0].height) < 1, 'Card count must never grow the lane');
                assert(lanes.every(lane => lane.count === count));
                if (count >= 20) {
                    const scroll = await page.locator('#taskBoard .task-board-column').evaluateAll(nodes => {
                        const zones = nodes.map(node => node.querySelector('.task-board-dropzone'));
                        const header = nodes[0].querySelector('header'), top = header.getBoundingClientRect().top;
                        zones[0].scrollTop = 350;
                        return { first: zones[0].scrollTop, other: zones[1].scrollTop, headerStable: top === header.getBoundingClientRect().top, overflow: zones.every(zone => zone.scrollHeight > zone.clientHeight) };
                    });
                    assert(scroll.first > 0 && scroll.other === 0 && scroll.headerStable && scroll.overflow);
                }
                if (count === 5) {
                    const card = page.locator('#taskBoard .task-card').first();
                    const geometry = await card.evaluate(node => {
                        const group = node.querySelector('.task-action'), buttons = [...group.querySelectorAll('button')].map(button => button.getBoundingClientRect()), box = group.getBoundingClientRect();
                        return { font: getComputedStyle(node.querySelector('h3')).fontFamily, padding: parseFloat(getComputedStyle(node).padding), align: getComputedStyle(node).textAlign, heights: buttons.map(b => b.height), centered: Math.abs((buttons[0].left + buttons.at(-1).right) / 2 - (box.left + box.right) / 2) < 2, contained: buttons.every(b => b.left >= box.left - 1 && b.right <= box.right + 1) };
                    });
                    assert(geometry.font.includes('Manrope'), geometry.font);
                    assert(geometry.padding <= 12 && geometry.align === 'left' && geometry.centered && geometry.contained, JSON.stringify(geometry));
                    assert(geometry.heights.every(height => height === 36));
                    await page.locator('#listModeButton').click();
                    const listStyle = await page.locator('#taskList .task-card').first().evaluate(node => {
                        const group = node.querySelector('.task-action').getBoundingClientRect(), buttons = [...node.querySelectorAll('.task-action button')].map(button => button.getBoundingClientRect());
                        return { font: getComputedStyle(node.querySelector('h3')).fontFamily, padding: parseFloat(getComputedStyle(node).padding), align: getComputedStyle(node).textAlign, centered: Math.abs((buttons[0].left + buttons.at(-1).right) / 2 - (group.left + group.right) / 2) < 2, equalHeight: buttons.every(button => button.height === 36) };
                    });
                    assert(listStyle.padding <= 12 && listStyle.align === 'left' && listStyle.centered && listStyle.equalHeight, JSON.stringify(listStyle));
                    await page.locator('#boardModeButton').click();

                }
                console.log('PASS ' + role + ': ' + count + ' cards per lane; fixed equal white columns, compact aligned cards and independent scrolling');
            }
            await page.setViewportSize({ width: 390, height: 844 });
            const mobileToolbar = await page.locator('#taskBoardToolbar').evaluate(host => {const boxes=[...host.querySelectorAll('.task-filter-field,.task-view-toggle')].map(node=>node.getBoundingClientRect());return boxes.every((a,index)=>boxes.slice(index+1).every(b=>a.right<=b.left||b.right<=a.left||a.bottom<=b.top||b.bottom<=a.top));});assert(mobileToolbar);
            const responsive = await page.locator('#taskBoard').evaluate(host => ({ scrolls: host.scrollWidth > host.clientWidth, pageFits: document.documentElement.scrollWidth <= innerWidth + 1 }));
            assert(responsive.scrolls && responsive.pageFits);
            await page.locator('#taskBoard').evaluate(host => { host.scrollLeft = host.scrollWidth; });
            assert(await page.locator('#taskBoard').evaluate(host => host.scrollLeft > 0));
            await page.locator('#taskBoard').screenshot({ path: path.join(__dirname, studentRole ? 'student-fixed-board-mobile.png' : 'company-fixed-board-mobile.png'), animations: 'disabled' });
            await page.setViewportSize({ width: 1600, height: 1000 });
        }
        // Company drag edge scrolling and persistent same-stage reorder request.
        studentRole = false; await page.goto(base + '/Company/ManageTasks.aspx');
        const first = page.locator('[data-board-task-id="TASK-0-0"]'); await first.waitFor();
        await first.dispatchEvent('dragstart', { dataTransfer: await page.evaluateHandle(() => new DataTransfer()) });
        const zone = page.locator('[data-column-id="todo"] .task-board-dropzone'), box = await zone.boundingBox();
        await zone.dispatchEvent('dragover', { clientX: box.x + 30, clientY: box.y + box.height - 8, dataTransfer: await page.evaluateHandle(() => new DataTransfer()) });
        await page.waitForFunction(() => document.querySelector('[data-column-id="todo"] .task-board-dropzone').scrollTop > 100);
        await zone.evaluate(node => { node.scrollTop = 700; });
        await page.locator('[data-board-task-id="TASK-0-4"]').dispatchEvent('drop', { dataTransfer: await page.evaluateHandle(() => new DataTransfer()) });
        await page.waitForFunction(() => document.getElementById('pageNotice').textContent.includes('position saved'));
        assert.strictEqual(moves.length, 1); assert.strictEqual(moves[0].columnId, 'todo'); assert.strictEqual(moves[0].beforeTaskId, 'TASK-0-4'); assert.strictEqual(moves[0].placementId, student.placementId);
        await first.dispatchEvent('dragend');
        assert.deepStrictEqual(errors, []);
        console.log('PASS drag edge autoscroll and same-stage Firebase reorder request inside a vertically scrolled lane; external traffic blocked');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(() => server.close());
