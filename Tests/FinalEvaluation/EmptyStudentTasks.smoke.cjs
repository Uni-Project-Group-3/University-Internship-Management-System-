// Runs the real page script with a small DOM/HTTP test double. No browser or Firebase writes.
const fs = require('fs');
const vm = require('vm');
const path = require('path');
const assert = require('assert/strict');
const script = fs.readFileSync(path.resolve(__dirname, '../../University-Internship-Management-System/Js_project/Student/MyTasks.js'), 'utf8');
function element() {
  const children = new Map();
  return { value: '', hidden: false, textContent: '', innerHTML: '', style: {}, classList: { toggle() {}, add() {}, remove() {} },
    querySelector(selector) { if (!children.has(selector)) children.set(selector, element()); return children.get(selector); },
    querySelectorAll() { return []; }, addEventListener() {}, setAttribute() {}, insertAdjacentHTML() {} };
}
async function page(responses) {
  const nodes = new Map(), counts = Array.from({length: 4}, element), events = {};
  counts.forEach((el, i) => { el.textContent = i ? '1' : '4'; });
  const by = id => { if (!nodes.has(id)) nodes.set(id, element()); return nodes.get(id); };
  const doc = { getElementById: by, querySelector: () => by('connection'), querySelectorAll: () => counts, addEventListener() {}, hidden: false };
  const window = { addEventListener: (name, fn) => { events[name] = fn; }, setInterval() {} };
  let index = 0;
  vm.runInNewContext(script, { document: doc, window, URL, fetch: async () => responses[Math.min(index++, responses.length - 1)], console });
  const settle = () => new Promise(resolve => setImmediate(resolve));
  await settle();
  return {by, counts, refresh: async () => { await events.focus(); await settle(); }};
}
const ok = tasks => ({ok: true, status: 200, redirected: false, json: async () => ({tasks, submissions: []})});
const rejected = status => ({ok: false, status, redirected: false, json: async () => { throw new SyntaxError('HTML'); }});
(async () => {
  const empty = await page([ok([])]);
  assert.deepEqual(empty.counts.map(c => c.textContent), [0, 0, 0, 0]);
  assert.match(empty.by('taskBoard').innerHTML, /No tasks assigned yet/);
  const failed = await page([rejected(500)]);
  assert.deepEqual(failed.counts.map(c => c.textContent), ['—', '—', '—', '—']);
  assert.equal(failed.by('taskList').hidden, false);
  assert.match(failed.by('pageNotice').textContent, /HTTP 500/);
  const expired = await page([rejected(401)]);
  assert.match(expired.by('pageNotice').textContent, /sign-in expired/);
  const redirected = await page([{...rejected(200), redirected: true, url: 'http://localhost/Auth/Login.aspx?ReturnUrl=TasksApi.ashx'}]);
  assert.match(redirected.by('pageNotice').textContent, /sign-in expired/);
  const recovered = await page([rejected(503), ok([])]);
  await recovered.refresh();
  assert.deepEqual(recovered.counts.map(c => c.textContent), [0, 0, 0, 0]);
  assert.equal(recovered.by('pageNotice').hidden, true);
  console.log('PASS empty accounts show zero; failures clear counts; expired sessions explain sign-in; retries recover.');
})().catch(error => { console.error(error); process.exitCode = 1; });
