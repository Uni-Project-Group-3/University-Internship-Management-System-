// Actual IIS HTTP checks; no mock authentication, credentials or Firebase writes.
const assert = require('node:assert/strict');
const origin = new URL(process.argv[2] || 'http://localhost:61879');
assert(['localhost', '127.0.0.1'].includes(origin.hostname));
(async () => {
  let checks = 0;
  for (const page of ['Company/ReportsFinalEvaluation', 'Academic/FinalAcademicEvaluation', 'Coordinator/FinalCompletionReview', 'Student/MyProgressEvaluation']) {
    const response = await fetch(new URL(page, origin), { redirect: 'manual' });
    const html = await response.text();
    const cookie = response.headers.getSetCookie().map(value => value.split(';')[0]).join('; ');
    assert.equal(response.status, 403, page); checks++;
    assert(/Access denied/i.test(html), page); checks++;
    assert(!html.includes('Fictional offline user'), page); checks++;
    const fields = new URLSearchParams();
    for (const match of html.matchAll(/<input\b[^>]*>/g)) {
      const attrs = Object.fromEntries([...match[0].matchAll(/([\w:-]+)="([^"]*)"/g)].map(x => [x[1], x[2]]));
      if (attrs.type === 'hidden' && attrs.name) fields.set(attrs.name, attrs.value || '');
    }
    fields.set('role', 'coordinator'); fields.set('supervisorId', 'test-uid'); fields.set('openEvaluation', 'test-placement');
    const post = await fetch(new URL(page, origin), { method: 'POST', body: fields, headers: { Cookie: cookie }, redirect: 'manual' });
    assert.equal(post.status, 403, page + ' forged browser identity postback'); checks++;
    assert(/Access denied/i.test(await post.text()), page); checks++;
    console.log('PASS ' + page + ': anonymous GET and forged-identity POST denied');
  }
  console.log(checks + ' actual IIS access checks passed. Authenticated browser workflows remain unverified.');
})().catch(error => { console.error(error); process.exitCode = 1; });
