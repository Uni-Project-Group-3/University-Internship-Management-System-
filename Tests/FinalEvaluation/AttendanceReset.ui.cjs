// Offline tests for the real student reset event handlers. No browser, network, or Firebase access.
const fs = require('fs'), path = require('path'), vm = require('vm'), assert = require('assert');
const file = path.resolve(__dirname, '../../University-Internship-Management-System/Js_project/Student/my-attendance.js');
const source = fs.readFileSync(file, 'utf8');
const handlers = source.slice(source.indexOf('// Local reset still uses'));
assert(handlers.startsWith('// Local reset still uses'));
function fixture(mode = 'success') {
  const elements = new Map(), calls = [], notices = [], renders = [];
  function element(id) {
    if (!elements.has(id)) elements.set(id, { disabled: false, hidden: true, textContent: '', open: false, events: {},
      addEventListener(name, fn) { this.events[name] = fn; }, replaceChildren() {},
      close() { this.open = false; this.events.close?.call(this); } });
    return elements.get(id);
  }
  const context = vm.createContext({ console, clearInterval() {}, document: { getElementById: element },
    dataVersion: 0, lastOpenedIssuedQr: "old-qr", loading: false, saving: false, attendance: { canResetToday: true }, qrTimer: null,
    openDialog(id) { element(id).open = true; }, showNotice(message, error) { notices.push({ message, error }); },
    renderStudent(data) { renders.push(data); },
    async api(action, body) {
      calls.push({ action, body });
      if (mode === 'previewError') throw Error('Linked weekly report');
      if (action === 'resetPreview') return { date: '2026-10-24', expectedVersion: 'offline-version', approvedMinutesRemoved: 11 };
      if (mode === 'conflict') throw Error('Attendance changed during reset');
      return { hasAssignment: true, canResetToday: false, approvedMinutes: 2405 };
    }
  });
  vm.runInContext(handlers, context);
  const click = id => element(id).events.click.call(element(id));
  return { element, calls, notices, renders, context, click };
}
(async () => {
  let f = fixture();
  await f.click('resetTodayButton');
  assert(f.element('resetTodayDialog').open);
  assert(f.element('resetTodayImpact').textContent.includes('11 approved minute'));
  assert.strictEqual(f.calls.length, 1, 'Preview must not perform reset');
  f.element('resetTodayDialog').close(); await f.click('confirmResetToday');
  assert.strictEqual(f.calls.length, 1, 'Cancel must discard confirmation');
  await f.click('resetTodayButton'); await f.click('confirmResetToday');
  assert.strictEqual(f.calls.at(-1).action, 'resetToday');
  assert.strictEqual(f.calls.at(-1).body.expectedVersion, 'offline-version');
  assert.strictEqual(f.calls.at(-1).body.expectedDate, '2026-10-24');
  assert.strictEqual(f.renders.length, 1); assert(!f.element('resetTodayDialog').open);
  assert(f.notices.at(-1).message.includes('new check-in QR'));
  f = fixture('previewError'); await f.click('resetTodayButton');
  assert(!f.element('resetTodayDialog').open && f.notices[0].error);
  f = fixture('conflict'); await f.click('resetTodayButton'); await f.click('confirmResetToday');
  assert.strictEqual(f.renders.length, 0); assert(!f.element('resetTodayError').hidden);
  assert(f.element('confirmResetToday').disabled); await f.click('confirmResetToday');
  assert.strictEqual(f.calls.length, 2, 'Uncertain/conflicting reset must not be retried automatically');
  f = fixture(); f.context.attendance.canResetToday = false; await f.click('resetTodayButton');
  assert.strictEqual(f.calls.length, 0);
  f.context.attendance.canResetToday = true; f.context.loading = true; await f.click('resetTodayButton');
  assert.strictEqual(f.calls.length, 1, 'Background refresh must not swallow reset preview');
  assert(f.element('resetTodayDialog').open);
  assert.strictEqual(f.context.dataVersion, 1, 'Ignore older load responses');
  await f.click('confirmResetToday');
  assert.strictEqual(f.calls.length, 2, 'Confirmation works during an in-flight background read');
  assert.strictEqual(f.context.dataVersion, 2);
  assert.strictEqual(f.context.lastOpenedIssuedQr, '', 'New QR may open after reset');
  console.log('PASS reset preview, explicit confirmation, cancel, updated totals, blocked identity state, report denial, concurrency feedback and no automatic retry. Offline only.');
})().catch(error => { console.error(error); process.exitCode = 1; });
