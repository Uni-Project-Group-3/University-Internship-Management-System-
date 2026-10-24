// Isolated API fixtures only. Never contacts Firebase or uploads a real file.
const fs = require('fs'), path = require('path'), http = require('http'), assert = require('assert');
const { chromium } = require(path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const root = path.resolve(__dirname, '../../University-Internship-Management-System');
const server = http.createServer((req, res) => {
  const file = path.resolve(root, '.' + new URL(req.url, 'http://localhost').pathname);
  if (!file.startsWith(root + path.sep)) { res.writeHead(404); return res.end(); }
  try {
    let data = fs.readFileSync(file);
    if (file.endsWith('.aspx')) data = data.toString().replace(/<%@[^%]*%>/g, '').replace(/<script runat="server">[\s\S]*?<\/script>/g, '');
    res.setHeader('Content-Type', file.endsWith('.js') ? 'application/javascript' : file.endsWith('.css') ? 'text/css' : 'text/html');
    res.end(data);
  } catch { res.writeHead(404); res.end(); }
});
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const base = 'http://127.0.0.1:' + server.address().port;
  const browser = await chromium.launch({ executablePath: process.env.PROGRAMFILES + '/Google/Chrome/Application/chrome.exe', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    let reports = Object.fromEntries(['submitted', 'resubmitted', 'approved', 'revisionRequested'].map((status, index) => ['R' + index, { studentId: 'ST' + index, weekNumber: 1, status }]));
    let fail = false;
    await page.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin !== base) return route.abort();
      if (!url.pathname.endsWith('WeeklyReportsApi.ashx')) return route.continue();
      assert.strictEqual(route.request().method(), 'GET');
      if (fail) return route.fulfill({ status: 503, json: { message: 'Isolated service error' } });
      return route.fulfill({ json: url.searchParams.get('action') === 'placements'
        ? { placements: [{ studentName: 'Test trainee', studentId: 'ST0', placementId: 'P0' }] }
        : { reports } });
    });
    await page.goto(base + '/Company/WeeklyReports.aspx');
    await page.waitForFunction(() => document.querySelectorAll('.wr-report').length === 4);
    assert.strictEqual(await page.locator('.wr-summary').count(), 0);
    await page.screenshot({ path: path.join(process.env.TEMP, 'uims-company-weekly-desktop.png'), fullPage: true });
    await page.getByRole('link', { name: 'Submit a report', exact: true }).click();
    assert(page.url().endsWith('#weeklySubmit'));
    await page.locator('#submitReport').click();
    await page.waitForFunction(() => document.getElementById('wrNotice').textContent === 'Choose a Word .docx report.');
    reports.R0.status = 'approved';
    await page.locator('#refreshCompany').click();
    await page.waitForFunction(() => document.querySelectorAll('.wr-badge.approved').length === 2);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.evaluate(() => window.scrollTo(0, 0));
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await page.screenshot({ path: path.join(process.env.TEMP, 'uims-company-weekly-mobile.png'), fullPage: true });
    reports = {};
    await page.locator('#refreshCompany').click();
    await page.getByText('No weekly reports have been submitted yet.', { exact: true }).waitFor();
    fail = true;
    await page.locator('#refreshCompany').click();
    await page.waitForFunction(() => document.getElementById('wrNotice').textContent === 'Isolated service error');
    assert(await page.locator('#wrNotice').isHidden());
    assert.deepStrictEqual(errors, []);
    console.log('PASS: report history without summary cards, updated statuses, empty/error states, submit navigation, file validation, and mobile layout. No Firebase writes.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(() => server.close());
