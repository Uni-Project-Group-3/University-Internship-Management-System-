// Exercises the actual authentication client with isolated responses; no external requests permitted.
const http = require('http'), fs = require('fs'), path = require('path'), assert = require('assert');
const { chromium } = require(path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const script = fs.readFileSync(path.resolve(__dirname, '../../University-Internship-Management-System/Js_project/firebase-client.js'));
const server = http.createServer((req, res) => {
    if (req.url === '/Js_project/firebase-client.js') { res.setHeader('Content-Type', 'application/javascript'); return res.end(script); }
    res.setHeader('Content-Type', 'text/html'); res.end('<div id="loginContent"><form class="auth-layout"><label>Email<input type="email" id="Email" /></label><label>Password<input type="password" id="Password" /></label><button id="SignIn" data-action="login">Sign In</button></form></div><script src="/Js_project/firebase-client.js"></script>');
});
(async () => {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); const base = 'http://127.0.0.1:' + server.address().port;
    const executablePath = [process.env.PROGRAMFILES + '/Google/Chrome/Application/chrome.exe', process.env['PROGRAMFILES(X86)'] + '/Microsoft/Edge/Application/msedge.exe'].find(fs.existsSync);
    const browser = await chromium.launch({ executablePath, headless: true });
    try {
        const context = await browser.newContext(); let signIns = 0, refreshes = 0;
        await context.route('**/*', route => {
            if (!route.request().url().startsWith(base + '/')) return route.abort();
            if (!route.request().url().endsWith('/FirebaseApi.ashx')) return route.continue();
            const action = route.request().postDataJSON().action;
            if (action === 'configuration') return route.fulfill({ json: { success: true, data: { message: 'Isolated configuration' } } });
            if (action === 'signIn') signIns++; if (action === 'refresh') refreshes++;
            return route.fulfill({ status: 429, headers: { 'Retry-After': '2' }, json: { success: false, message: 'Temporary provider limit.', retryAfterSeconds: 2 } });
        });
        const page = await context.newPage(); const errors = []; page.on('pageerror', e => errors.push(e.message));
        await page.goto(base + '/Auth/Login.aspx');
        const fixtureSession = JSON.stringify({ idToken: 'isolated-token', refreshToken: 'isolated-refresh', expiresAt: Date.now() + 3600000 });
        await page.evaluate(value => sessionStorage.setItem('uims.firebase.authentication', value), fixtureSession);
        await page.locator('#Email').fill('fixture@example.test'); await page.locator('#Password').fill('isolated-password');
        await page.locator('#SignIn').click(); await page.getByRole('button', { name: /Sign In \(\d\d:\d\d\)/ }).waitFor();
        assert.strictEqual(await page.locator('#firebaseConnectionStatus').isVisible(), false);
        await page.evaluate(() => { for (let i = 0; i < 30; i++) document.querySelector('form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })); });
        assert.strictEqual(signIns, 1); assert.strictEqual(await page.locator('#Password').inputValue(), 'isolated-password');
        assert.strictEqual(await page.evaluate(() => sessionStorage.getItem('uims.firebase.authentication')), fixtureSession);
        await page.getByRole('button', { name: 'Sign In', exact: true }).waitFor(); assert.strictEqual(signIns, 1);
        assert.strictEqual(await page.locator('#firebaseConnectionStatus').isVisible(), false);
        console.log('PASS repeated submits send one request; cooldown counts down and recovers without automatic retries or losing fields/session');
        await page.evaluate(() => { const value = JSON.parse(sessionStorage.getItem('uims.firebase.authentication')); value.expiresAt = 0; sessionStorage.setItem('uims.firebase.authentication', JSON.stringify(value)); });
        await page.evaluate(async () => { await Promise.allSettled(Array.from({ length: 20 }, () => window.UimsFirebase.getIdToken())); await window.UimsFirebase.getIdToken().catch(() => {}); });
        assert.strictEqual(refreshes, 1); assert(await page.evaluate(() => !!sessionStorage.getItem('uims.firebase.authentication')));
        assert.deepStrictEqual(errors, []); console.log('PASS concurrent refreshes share one request and provider backoff retains the session without repeated calls');
        const resume = await context.newPage();
        await resume.addInitScript(value => sessionStorage.setItem('uims.firebase.authentication', value), fixtureSession);
        await resume.goto(base + '/Auth/Login.aspx'); await resume.getByRole('button', { name: /Sign In \(\d\d:\d\d\)/ }).waitFor();
        assert.strictEqual(refreshes, 2); assert.strictEqual(signIns, 1);
        assert.strictEqual(await resume.evaluate(() => sessionStorage.getItem('uims.firebase.authentication')), fixtureSession);
        await resume.getByRole('button', { name: 'Sign In', exact: true }).waitFor(); assert.strictEqual(refreshes, 2);
        assert.strictEqual(await resume.locator('#firebaseConnectionStatus').isVisible(), false);
        console.log('PASS saved session resumes with one refresh and no password request; provider throttle does not trigger a retry loop or erase the session');
        const second = await context.newPage(); await second.goto(base + '/Auth/Login.aspx?account=new');
        assert.strictEqual(await second.evaluate(() => sessionStorage.getItem('uims.firebase.authentication')), null);
        let taskHeaders = [];
        await context.route('**/TasksApi.ashx', route => { taskHeaders.push(route.request().headers()['x-uims-session']); return route.fulfill({ json: { tasks: [] } }); });
        await page.evaluate(() => { const data = JSON.parse(sessionStorage.getItem('uims.firebase.authentication')); data.tabTicket = 'isolated-company-signed-ticket'; data.expiresAt = Date.now() + 3600000; sessionStorage.setItem('uims.firebase.authentication', JSON.stringify(data)); });
        await second.evaluate(() => sessionStorage.setItem('uims.firebase.authentication', JSON.stringify({ idToken: 'isolated-token', tabTicket: 'isolated-student-signed-ticket', expiresAt: Date.now() + 3600000 })));
        await page.evaluate(() => fetch('/TasksApi.ashx'));
        await second.evaluate(() => fetch('/TasksApi.ashx'));
        assert.deepStrictEqual(taskHeaders, ['isolated-company-signed-ticket', 'isolated-student-signed-ticket']);
        assert.strictEqual(await page.evaluate(() => JSON.parse(sessionStorage.getItem('uims.firebase.authentication')).tabTicket), 'isolated-company-signed-ticket');
        console.log('PASS separate tabs attach their own signed session tickets; opening another account preserves the first tab session');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(() => server.close());
