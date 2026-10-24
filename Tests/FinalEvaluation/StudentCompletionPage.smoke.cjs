// Local-only Student completion/download checks. No official Firebase access.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
let target = new URL('/Student/MyProgressEvaluation.aspx', process.argv[2] || 'http://localhost:61873');
assert(['localhost', '127.0.0.1'].includes(target.hostname));
const disabled = process.argv.includes('--disabled');
const httpProbe = process.argv.includes('--http-probe');
let cookie = '', count = 0;
const decode = v => v.replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&amp;/g, '&');
const attrs = tag => Object.fromEntries([...tag.matchAll(/([\w:-]+)="([^"]*)"/g)].map(m => [m[1], decode(m[2])]));
const form = html => { const fields = new URLSearchParams(); for (const m of html.matchAll(/<input\b[^>]*>/g)) { const a = attrs(m[0]); if (a.type === 'hidden') fields.set(a.name, a.value || ''); } return fields; };
const card = (html, id) => html.match(new RegExp('<section[^>]*aria-labelledby="' + id + '"[^>]*>([\\s\\S]*?)</section>'))?.[1] || '';
const metric = (html, name) => html.match(new RegExp('data-uims-progress="' + name + '"[^>]*>([^<]*)</span>'))?.[1] || '';
const button = html => html.includes('id="DownloadCompletionReport"');
const approved = html => /class="workspace-badge warning">Approved<\/span>/.test(card(html, 'completionTitle'));
function privateSafe(html) { for (const value of ['CONFIDENTIAL_', 'PRIVATE_UNPUBLISHED', 'PRIVATE_HIDDEN', 'completion-reports/', 'StorageObjectPath', 'DecisionHistory', 'demo-store.js', 'data-demo-role']) assert(!html.includes(value), value); }
async function response(fields, id) {
    if (id !== undefined) target.search = id ? '?internshipId=' + encodeURIComponent(id) : '';
    const result = await fetch(target, { method: fields ? 'POST' : 'GET', headers: { Cookie: cookie, ...(fields ? { 'Content-Type': 'application/x-www-form-urlencoded' } : {}) }, body: fields?.toString() });
    target = new URL(result.url); if (result.headers.getSetCookie().length) cookie = result.headers.getSetCookie().map(c => c.split(';')[0]).join('; '); return result;
}
async function page(fields, id, expected = 200) { const r = await response(fields, id); const html = await r.text(); assert.equal(r.status, expected, html.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').slice(0, 2100)); privateSafe(html); return html; }
async function download(html, id, expected = 200) {
    const fields = form(html); fields.set('DownloadCompletionReport', 'Download Completion Report'); const r = await response(fields, id); const bytes = Buffer.from(await r.arrayBuffer());
    assert.equal(r.status, expected, bytes.toString('utf8').slice(0, 1200));
    if (expected === 200) {
        assert.equal(r.headers.get('content-type'), 'application/pdf'); assert.equal(bytes.subarray(0, 4).toString(), '%PDF'); assert(/%%EOF\s*$/.test(bytes.toString('latin1')), 'No HTML may be appended to the PDF');
        assert.equal(Number(r.headers.get('content-length')), bytes.length); assert(/attachment; filename="internship-completion-report-[0-9a-f]{32}\.pdf"/.test(r.headers.get('content-disposition')));
        assert(/no-store/.test(r.headers.get('cache-control'))); assert.equal(r.headers.get('x-content-type-options'), 'nosniff'); return bytes;
    }
    assert(r.headers.get('content-type').startsWith('text/plain')); assert(!bytes.toString().includes('<html')); assert(!bytes.toString().includes('completion-reports/')); return bytes;
}
const pass = message => { count++; console.log('PASS ' + message); };
(async () => {
    if (httpProbe) {
        const r = await response(); const html = await r.text();
        assert.equal(r.status, 403, 'Unauthenticated Student page should deny access.');
        assert(!html.includes('student-progress-toolbar') && !html.includes('student-access-status') && html.includes('student-hour-cards') && html.includes('student-completion-facts') && html.includes('Task data unavailable'));
        assert(!html.includes('Authorized read-only student completion information. Overall completion progress is not supplied by a trusted source. Student-visible task results are unavailable.'));
        assert(!html.includes('id="DownloadCompletionReport"'), 'Unauthorized view must not expose a report download.');
        assert(html.includes('id="RefreshProgress"') && html.includes('id="taskFilter"') && html.includes('StudentDashboard.aspx') && html.includes('MyTasks.aspx') && html.includes('disabled="disabled"'));
        const asset = html.match(/href="([^"]*my-progress-evaluation\.css\?v=20261024-student-task-data-v2)"/);
        assert(asset, 'Versioned page stylesheet must be referenced by rendered HTML.');
        const cssResponse = await fetch(new URL(asset[1], target)); const css = await cssResponse.text();
        assert.equal(cssResponse.status, 200); assert(cssResponse.headers.get('content-type').includes('text/css'));
        assert(css.includes('.student-task-empty') && css.includes('.student-hour-cards') && css.includes('.student-completion-facts') && css.includes('.student-access-error'));
        const script = html.match(/src="([^"]*my-progress-evaluation\.js\?v=20261024-student-progress-ui)"/);
        assert(script, 'Versioned Student interaction script must be present.');
        const jsResponse = await fetch(new URL(script[1], target)); const js = await jsResponse.text();
        assert.equal(jsResponse.status, 200); assert(js.includes("by('taskFilter')?.addEventListener('change', filter)") && js.includes("addEventListener('submit'"));
        pass('IIS unauthenticated page denies access while rendering the updated fail-closed UI');
        pass('rendered page loads versioned Student CSS and JavaScript from IIS with the expected selectors and handlers');
        console.log(count + ' read-only Student IIS probes passed. No Firebase operations.'); return;
    }
    let html = await page();
    if (disabled) {
        assert(html.includes('Official Firebase student access is disabled') && !button(html) && metric(html, 'ApprovedHours') === 'Unknown'); pass('default Student page fails closed with no demo identity or invented hours');
        assert(metric(html, 'EvaluatedExams') === 'Published task grades unavailable' && !html.includes('No published task grades available')); pass('unavailable task source does not pretend to be a verified empty grade list');
        assert(!html.includes('id="HoursProgress"')); pass('missing approved-hour data does not render an indeterminate progress bar');
        assert(!html.includes('id="ToggleExamDemo"') && !html.includes('id="examDialog"')); pass('unsupported legacy exam controls are not rendered');
        assert(html.includes('id="RefreshProgress"')); const refresh = form(html); refresh.set('RefreshProgress', 'Refresh progress'); html = await page(refresh); assert(html.includes('Official Firebase student access is disabled') && metric(html,'ApprovedHours') === 'Unknown'); pass('Refresh re-runs the authorized page flow without inventing records');

        await download(html, undefined, 503); pass('forged download postback is denied without trusted integrations');
        html = await page(undefined, 'dev-student-ready'); assert(!button(html) && !html.includes('Fictional published task')); pass('development internship ID cannot enable official Student access');
        const data = form(html); data.set('studentId', 'dev-student-owner'); data.set('role', 'student'); data.set('frontendDemo', '1'); html = await page(data); assert(!button(html) && metric(html, 'ApprovedHours') === 'Unknown'); pass('browser identity role and demo fields cannot authorize access');
        console.log(count + ' default-disabled Student Web Forms checks passed.'); return;
    }
    assert(html.includes('Isolated in-memory student test mode.') && approved(html)); pass('authorized student sees actual Approved completion');
    assert(metric(html, 'ApprovedHours') === '10' && metric(html, 'RequiredHours') === '10' && metric(html, 'HoursPercent') === '100%'); pass('only approved hours contribute to trusted hour progress');
    assert(html.includes('Overall completion progress: 100%') && html.includes('2026-10-21 10:00:00Z')); pass('trusted overall progress and actual approval date render');
    assert(html.includes('Published task grade: 4 / 7') && html.includes('Published task feedback &lt;plain text&gt;') && html.includes('Task feedback has not been published.') && !html.includes('999')); pass('only published student-visible task grades and encoded feedback appear');
    assert(html.includes('Published Task Grades') && metric(html, 'EvaluatedExams') === '1' && !html.includes('Published exam results')); pass('published task-grade card counts only authorized published grades');
    assert(button(html) && html.includes('A verified completion report is available.')); pass('Download appears only after Approved and PDF integrity verification');
    const pdf = await download(html); pass('owner receives authenticated PDF attachment with exact length no-store and nosniff headers');
    html = await page(); const again = await download(html); assert.equal(crypto.createHash('sha256').update(pdf).digest('hex'), crypto.createHash('sha256').update(again).digest('hex')); pass('refresh and repeated download retrieve the same saved PDF without HTML suffix');
    html = await page(); const identity = form(html); identity.set('studentId', 'other-student'); identity.set('internshipId', 'other-internship'); identity.set('role', 'Coordinator'); identity.set('documentId', 'forged'); identity.set('DownloadCompletionReport', 'Download Completion Report');
    const responseWithForgedFields = await response(identity); assert.equal(responseWithForgedFields.status, 200); const ownBytes = Buffer.from(await responseWithForgedFields.arrayBuffer()); assert.equal(crypto.createHash('sha256').update(ownBytes).digest('hex'), crypto.createHash('sha256').update(pdf).digest('hex')); pass('browser identity and document fields cannot select another student report');
    html = await page(undefined, 'dev-student-no-report'); assert(approved(html) && !button(html) && html.includes('PDF has not yet been made available.')); pass('Approved without generated PDF stays Approved with a separate availability message');
    await download(html, undefined, 404); pass('forged download for an ungenerated PDF returns not found');
    html = await page(form(html)); assert(approved(html) && !button(html)); pass('postback preserves Approved status independently of PDF absence');
    html = await page(undefined, 'dev-student-pending'); assert(!approved(html) && !button(html) && card(html, 'completionTitle').includes('Pending')); pass('Pending completion never displays a working Download');
    assert(metric(html, 'HoursPercent') === '50%' && html.includes('Overall completion progress: 30%')); pass('overall progress is supplied separately rather than invented from hours');
    await download(html, undefined, 409); pass('forged download before approval returns conflict');
    html = await page(undefined, 'dev-student-corrupt'); assert(approved(html) && !button(html) && html.includes('failed integrity validation')); pass('corrupt PDF disables availability without reverting Approved');
    await download(html, undefined, 503); pass('corrupt PDF download returns safe service-unavailable error');
    html = await page(undefined, 'dev-student-missing-object'); assert(approved(html) && !button(html)); await download(html, undefined, 503); pass('missing private object does not expose storage paths or send a false PDF');
    html = await page(undefined, 'dev-student-untrusted'); assert(approved(html) && metric(html, 'ApprovedHours') === 'Unknown' && metric(html, 'RequiredHours') === 'Unknown' && metric(html, 'AssignedTasks') === 'Unknown' && html.includes('Overall completion progress: Not supplied')); pass('untrusted team inputs remain unknown while authoritative Approved status remains visible');
    assert(metric(html, 'EvaluatedExams') === 'Published task grades unavailable'); pass('missing trusted grades are distinct from verified empty task grades');
    html = await page(undefined, 'another-student-internship', 403); assert(!html.includes('Fictional published task') && !button(html)); pass('another student internship is denied before protected page data renders');
    await download(html, undefined, 403); pass('unauthorized download returns forbidden without PDF or HTML page');
    html = await page(undefined, 'dev-student-ready'); for (const value of form(html).values()) { let decoded = ''; try { decoded = Buffer.from(value, 'base64').toString(); } catch {} assert(!decoded.includes('PRIVATE_') && !decoded.includes('completion-reports/')); } pass('HTML and hidden postback state contain no confidential evaluations or storage references');
    assert(html.includes('id="sidebar"') && html.includes('id="form1"') && html.includes('my-progress-evaluation.css?v=20261024-student-task-data-v2') && html.includes('my-progress-evaluation.js?v=20261024-student-progress-ui')); pass('existing Sidebar form and versioned page assets remain present');
    assert(!html.includes('Authorized read-only student completion information. Overall completion progress is not supplied by a trusted source. Student-visible task results are unavailable.')); pass('old blue information banner copy is absent');
    assert(html.includes('class="student-hour-cards"') && html.includes('student-completion-facts') && html.includes('id="taskFilter"') && html.includes('id="TaskEmptyState"')); pass('progress, completion and task empty-state UI renders in the page');
    console.log(count + ' isolated Student Web Forms/download checks passed. No real Firebase writes.');
})().catch(error => { console.error(error); process.exitCode = 1; });
