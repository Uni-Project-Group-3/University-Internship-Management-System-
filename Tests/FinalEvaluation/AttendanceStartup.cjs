// Execute each whole production attendance script against its current page IDs.
// No browser, network or Firebase access. Missing legacy drawer controls are intentional.
const fs = require('fs'), path = require('path'), vm = require('vm'), assert = require('assert');
const root = path.resolve(__dirname, '../../University-Internship-Management-System');
function element(id) {
  const classes = new Set();
  return { id, hidden: false, disabled: false, textContent: '', value: '', open: false, inert: false, dataset: {}, children: [],
    classList: { contains: c => classes.has(c), add: c => classes.add(c), remove: c => classes.delete(c), toggle(c, v) { if (v) classes.add(c); else classes.delete(c); } },
    addEventListener() {}, setAttribute() {}, removeAttribute() {}, append(...items) { this.children.push(...items); }, replaceChildren(...items) { this.children = items; }, focus() {}, close() {},
    querySelector() { return null; }, querySelectorAll() { return []; }, getContext() { return {}; } };
}
async function run(role, canonical, authenticated, success = false) {
  const page = role === 'Student' ? 'MyAttendance' : 'VerifyAttendance';
  const script = role === 'Student' ? 'my-attendance' : 'verify-attendance';
  const markup = fs.readFileSync(path.join(root, role, page + '.aspx'), 'utf8');
  const ids = new Map([...markup.matchAll(/\bid="([^"]+)"/g)].map(m => [m[1], element(m[1])]));
  const sidebar = ids.get('sidebar'); if (canonical) sidebar.classList.add('workspace-canonical-sidebar');
  const menu = element('menu'), main = element('main'), oldClose = canonical ? null : element('oldClose');
  let requests = 0;
  if (ids.has('historyFilter')) ids.get('historyFilter').value = 'all';
  const fixture = { student: { name: 'Offline student', studentNumber: 'S' }, company: { name: 'Offline company' }, supervisor: { name: 'Offline supervisor' }, hasAssignment: true, assignment: { requiredHours: 300 }, schedule: {}, record: {}, earlyRequest: {}, history: [], approvedMinutes: 2405, canResetToday: true, students: [{ id: 'S', name: 'Offline student' }], requests: [], records: [], today: '2026-10-24' };
  fixture.actionsEnabled = true; fixture.schedule = { assignmentId: 'A', isTrainingDay: true };
  fixture.record = { status: 'completed', hoursApprovalStatus: 'pending', workedMinutes: 4, checkInAt: 1, checkOutAt: 240001 };
  fixture.records = [{ studentName: 'Other student', studentId: 'OTHER', attendanceDate: fixture.today, status: 'absent' }, { studentName: 'Offline student', studentId: 'S', attendanceDate: fixture.today, status: 'completed', hoursApprovalStatus: 'pending', workedMinutes: 4, checkInAt: 1, checkOutAt: 240001 }];
  ids.get('attendanceBootstrap').textContent = JSON.stringify({ csrf: 'offline-csrf', localDemo: false });
  const context = vm.createContext({ console, URL, URLSearchParams, AbortController, TextDecoder,
    setTimeout, clearTimeout, setInterval: () => 0, clearInterval, performance,
    location: { hostname: 'localhost' }, sessionStorage: { getItem() { return null; } },
    document: { body: element('body'), getElementById(id) { return ids.get(id) || null; }, createElement: element,
      querySelector(s) { return ({ '.main': main, '.menu-btn': menu, '.legacy-sidebar-close': oldClose })[s] || null; },
      querySelectorAll() { return []; }, addEventListener() {} },
    window: { location: { href: 'https://localhost/' + role + '/' + page }, addEventListener() {},
      matchMedia() { return { matches: true, addEventListener() {} }; },
      UimsFirebase: { async getIdToken() { if (authenticated) return 'offline-test-token'; throw Error('Sign in with your Firebase account to load attendance.'); } } },
    async fetch(url, options) {
      requests++;
      assert(options.method === 'POST' && options.credentials === 'same-origin');
      assert.strictEqual(new URLSearchParams(options.body).get('action'), 'load');
      return { ok: success, status: success ? 200 : 401, headers: { get() { return 'application/json'; } },
        async json() { return success ? { success: true, data: fixture } : { success: false, message: 'Offline authentication response' }; } };
    }
  });
  const source = fs.readFileSync(path.join(root, 'Js_project', role, script + '.js'), 'utf8');
  vm.runInContext(source, context, { filename: script + '.js' });
  await new Promise(resolve => setTimeout(resolve, 20));
  assert.strictEqual(requests, 1, role + ' must reach its backend load request');
  assert.strictEqual(ids.get('connectionStatus').textContent, success ? 'Firebase connected' : 'Connection / setup required');
  if (!success) assert.strictEqual(ids.get('pageNotice').textContent, 'Offline authentication response');
  if (success && role === 'Student') { assert.strictEqual(ids.get('approvedHours').textContent, '40.08 h'); assert(!ids.get('resetTodayTools').hidden); assert(ids.get('scheduleHelp').textContent.includes('4 minute(s) awaiting')); }
  if (success && role === 'Company') {
    assert.strictEqual(ids.get('assignedStudents').textContent, 'Assigned trainees: Offline student');
    const rows = ids.get('recordRows').children;
    assert.strictEqual(rows[0].children[1].textContent, 'Offline student', 'Pending hours must sort before absent rows');
    assert.strictEqual(rows[0].children.at(-1).children[0].textContent, 'Approve hours');
    assert(ids.get('pendingApprovalSummary').textContent.startsWith('1 completed'));
    ids.get('approvalFilter').value = 'pending'; vm.runInContext('renderRecords()', context); assert.strictEqual(ids.get('recordRows').children.length, 1);
    ids.get('attendanceSearch').value = 'S'; vm.runInContext('renderRecords()', context); assert.strictEqual(ids.get('recordRows').children[0].children[1].textContent, 'Offline student');
  }
  if (canonical) assert(!main.inert && !sidebar.inert, 'Legacy navigation must not lock the canonical drawer/content');
}
(async () => {
  for (const role of ['Student', 'Company']) for (const canonical of [true, false]) for (const auth of [true, false]) for (const success of [true, false]) await run(role, canonical, auth, success);
  console.log('PASS 16 full-script startup cases: student/company, canonical/legacy navigation, bearer/server-session fallback. Backend load reached; no hanging Connecting state. All fetch calls mocked.');
})().catch(error => { console.error(error); process.exitCode = 1; });
