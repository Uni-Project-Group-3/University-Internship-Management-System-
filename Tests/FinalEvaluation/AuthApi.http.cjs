// Local IIS Express response-contract check. No credentials, login attempts or business writes.
const https = require('https'), assert = require('assert');
const origin = 'https://localhost:44372';
function request(path, method, body, headers = {}) {
    return new Promise((resolve, reject) => {
        const url = new URL(path, origin);
        assert.strictEqual(url.origin, origin);
        // Trust only this local development endpoint for this test; application TLS settings are unchanged.
        const req = https.request(url, { method, rejectUnauthorized: false, headers: { Origin: origin, 'Content-Type': 'application/json', ...headers } }, response => {
            let data = ''; response.setEncoding('utf8'); response.on('data', chunk => data += chunk);
            response.on('end', () => resolve({ status: response.statusCode, type: response.headers['content-type'], location: response.headers.location, data }));
        }); req.on('error', reject); req.setTimeout(15000, () => req.destroy(new Error('Local API probe timed out'))); req.end(body && JSON.stringify(body));
    });
}
(async () => {
    for (const [path, method, body, status] of [
        ['/FirebaseApi.ashx', 'POST', { action: 'configuration' }, 200],
        ['/FirebaseApi.ashx', 'POST', { action: 'identity' }, 401],
        ['/FirebaseApi.ashx', 'GET', null, 405],
        ['/TasksApi.ashx', 'GET', null, 401],
        ['/TasksApi.ashx?action=changes&version=0', 'GET', null, 401],
        ['/TaskExamsApi.ashx?action=list', 'GET', null, 401]
    ]) {
        const response = await request(path, method, body);
        console.log(method + ' ' + path + ': status=' + response.status + '; contentType=' + response.type + '; redirected=' + !!response.location);
        assert.strictEqual(response.status, status); assert(!response.location); assert(response.type.includes('application/json')); JSON.parse(response.data);
    }
    for (const path of ['/TasksApi.ashx', '/TaskExamsApi.ashx?action=list']) {
        const response = await request(path, 'GET', null, { 'X-UIMS-Session': 'invalid-forged-tab-ticket' });
        assert.strictEqual(response.status, 401); assert(!response.location); JSON.parse(response.data);
    }
    console.log('PASS forged tab tickets are rejected by the actual server with HTTP 401.');
    console.log('PASS actual IIS Express APIs preserve JSON error statuses; no sign-in credentials or database writes sent.');
})().catch(error => { console.error(error.message); process.exitCode = 1; });
