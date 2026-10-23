(() => {
    'use strict';
    const endpoint = new URL('../FirebaseApi.ashx', document.currentScript.src);
    const storageKey = 'uims.firebase.authentication';
    const form = document.querySelector('.auth-layout');
    let busy = false, refreshing, cooldownUntil = 0, cooldownTimer, authRetryAt = 0;
    function cooldown(seconds) {
        cooldownUntil = performance.now() + Math.max(1, Math.min(3600, Number(seconds) || 60)) * 1000;
        clearInterval(cooldownTimer);
        const tick = () => {
            const left = Math.max(0, Math.ceil((cooldownUntil - performance.now()) / 1000)), button = document.getElementById('SignIn');
            if (button) { button.disabled = busy || left > 0; button.textContent = left ? 'Sign In (' + String(Math.floor(left / 60)).padStart(2, '0') + ':' + String(left % 60).padStart(2, '0') + ')' : 'Sign In'; }
            if (!left) clearInterval(cooldownTimer);
        }; tick(); cooldownTimer = setInterval(tick, 1000);
    }
    function load() { try { return JSON.parse(sessionStorage.getItem(storageKey) || 'null'); } catch { sessionStorage.removeItem(storageKey); return null; } }
    function save(data) {
        const lifetime = Number(data.expiresIn);
        if (!data.idToken || !data.refreshToken || !Number.isFinite(lifetime) || lifetime <= 0) throw Error('Firebase returned an incomplete authentication session.');
        sessionStorage.setItem(storageKey, JSON.stringify({ idToken: data.idToken, refreshToken: data.refreshToken, tabTicket: data.tabTicket, expiresAt: Date.now() + Math.min(lifetime, 1800) * 1000 }));
    }
    async function request(action, payload = {}) {
        if (['signIn', 'refresh', 'identity'].includes(action) && performance.now() < authRetryAt) {
            const error = Error('Authentication requests are temporarily paused. Your existing session is retained.');
            error.status = 429; error.retryAfterSeconds = Math.max(1, Math.ceil((authRetryAt - performance.now()) / 1000)); throw error;
        }
        let auth = load();
        if (auth && !['configuration', 'signIn', 'refresh'].includes(action) && auth.expiresAt < Date.now() + 90000) {
            if (!refreshing) refreshing = request('refresh', { refreshToken: auth.refreshToken }).then(save).finally(() => { refreshing = null; });
            await refreshing; auth = load();
        }
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 65000);
        try {
            const response = await fetch(endpoint, { method: 'POST', credentials: 'same-origin', cache: 'no-store', signal: controller.signal,
                headers: { 'Content-Type': 'application/json', ...(auth?.idToken && !['signIn', 'configuration', 'refresh'].includes(action) ? { Authorization: 'Bearer ' + auth.idToken } : {}) },
                body: JSON.stringify({ ...payload, action }) });
            const responseText = await response.text();
            let result;
            try { result = JSON.parse(responseText); }
            catch (_) {
                if (response.redirected || /^\s*(?:<!doctype\s+html|<html)/i.test(responseText)) {
                    throw Error(response.redirected ? 'The sign-in API redirected to an HTML page. Reload the page and try again; the server must return a JSON authentication response.' : 'The sign-in service returned an HTML page instead of JSON (HTTP ' + response.status + '). Check the local server response.');
                }
                throw Error('The Firebase API returned an invalid response (HTTP ' + response.status + '). Check the IIS Express output and server logs.');
            }
            if (!response.ok || !result.success) {
                if (response.status === 401 && action !== 'signIn') sessionStorage.removeItem(storageKey);
                const error = Error(result.message || 'The operation could not be completed.');
                error.status = response.status; error.retryAfterSeconds = Number(result.retryAfterSeconds || response.headers.get('Retry-After')) || 60;
                if (response.status === 429 && ['signIn', 'refresh', 'identity'].includes(action)) authRetryAt = performance.now() + Math.max(1, Math.min(3600, error.retryAfterSeconds)) * 1000;
                throw error;
            }
            return result.data;
        } catch (error) { if (error.name === 'AbortError') throw Error('The request timed out. No successful operation was confirmed.'); throw error; }
        finally { clearTimeout(timeout); }
    }
    function status(message) {
        let node = document.getElementById('firebaseConnectionStatus');
        if (!node) { node = document.createElement('p'); node.id = 'firebaseConnectionStatus'; node.className = 'auth-message'; node.setAttribute('role', 'status'); (document.getElementById('loginContent') || document.body).append(node); }
        node.textContent = message;
        node.hidden = !message;
    }
    function showError(error) {
        if (error.status === 429) { status(''); if (form) cooldown(error.retryAfterSeconds); return; }
        status(error.message);
    }
    function schemaTools() {
        if (document.getElementById('firebaseSchemaInspect')) return;
        const mount = document.getElementById('loginContent') || document.querySelector('main');
        const label = document.createElement('label'); label.textContent = 'Collection name (leave empty to list existing root names)'; label.htmlFor = 'firebaseSchemaCollection';
        const input = document.createElement('input'); input.id = label.htmlFor; input.className = 'auth-input'; input.maxLength = 180;
        const button = document.createElement('button'); button.type = 'button'; button.id = 'firebaseSchemaInspect'; button.className = 'auth-button'; button.textContent = 'Inspect schema (read only)';
        const output = document.createElement('pre'); output.id = 'firebaseSchemaResult'; output.style.cssText = 'white-space:pre-wrap;overflow-wrap:anywhere;max-height:24rem;overflow:auto'; output.setAttribute('role', 'status');
        mount.append(label, input, button, output);
        button.addEventListener('click', async () => {
            button.disabled = true; output.textContent = 'Reading authorized collection metadata…';
            try { const collection = input.value.trim(); output.textContent = JSON.stringify(await request('inspectSchema', collection ? { collection } : {}), null, 2); }
            catch (error) { output.textContent = error.message; }
            finally { button.disabled = false; }
        });
    }
    async function acceptSignIn(data) {
        if (data.mustChangePassword) {
            sessionStorage.removeItem(storageKey);
            const url = new URL('Auth/Login.aspx?change=required', new URL('./', endpoint));
            location.assign(url.href);
            return;
        }
        save(data);
        const password = document.getElementById('Password'); if (password) password.value = '';
        status('Firebase identity verified: ' + data.role + ' / ' + data.profileId + '.');
        const old = document.getElementById('firebaseSchemaInspect');
        if (data.role !== 'admin' && old) ['firebaseSchemaInspect', 'firebaseSchemaCollection', 'firebaseSchemaResult'].forEach(id => document.getElementById(id)?.remove());
        if (form) await navigate(dashboards[data.role]);
    }
    if (form) form.addEventListener('submit', async event => {
        if (event.submitter && event.submitter.dataset.action !== 'login') return;
        event.preventDefault(); event.stopImmediatePropagation();
        if (busy) return;
        if (performance.now() < cooldownUntil) return;
        const email = document.getElementById('Email'), password = document.getElementById('Password');
        if (!email.value.trim() || !email.checkValidity() || !password.value) { status('Enter a valid email and password.'); return; }
        busy = true; const button = document.getElementById('SignIn'); button.disabled = true; form.setAttribute('aria-busy', 'true');
        status('Verifying Firebase Authentication and the UID/profile mapping…');
        try { await acceptSignIn(await request('signIn', { email: email.value.trim(), password: password.value })); }
        catch (error) { showError(error); }
        finally { busy = false; button.disabled = performance.now() < cooldownUntil; form.removeAttribute('aria-busy'); }
    }, true);
    const dashboards = {
        student: 'Student/StudentDashboard.aspx',
        companySupervisor: 'Company/CompanyDashboard.aspx',
        academicSupervisor: 'Academic/AcademicDashboard.aspx',
        coordinator: 'Coordinator/CoordinatorDashboard.aspx'
    };
    async function getIdToken() {
        let auth = load();
        if (!auth) throw Error('Sign in with your Firebase account to load attendance.');
        if (auth.expiresAt < Date.now() + 90000) {
            if (!refreshing) refreshing = request('refresh', { refreshToken: auth.refreshToken }).then(save).finally(() => { refreshing = null; });
            await refreshing; auth = load();
        }
        return auth.idToken;
    }
    async function navigate(target) {
        const url = new URL(target, new URL('./', endpoint));
        if (url.origin !== endpoint.origin) throw Error('Invalid application navigation target.');
        if (!load()) { location.assign(url.href); return; }
        const bootstrap = document.getElementById('attendanceBootstrap');
        if (bootstrap && JSON.parse(bootstrap.textContent).localDemo && /\/(Student\/MyAttendance|Company\/VerifyAttendance)(?:\.aspx)?$/i.test(url.pathname)) { location.assign(url.href); return; }
        const token = await getIdToken();
        const post = document.createElement('form'); post.method = 'post'; post.action = url.href;
        const input = document.createElement('input'); input.type = 'hidden'; input.name = '__FirebaseToken'; input.value = token;
        post.append(input);
        const ticket = load()?.tabTicket;
        if (ticket) { const session = document.createElement('input'); session.type = 'hidden'; session.name = '__UimsSession'; session.value = ticket; post.append(session); }
        document.body.append(post); post.submit();
    }
    document.addEventListener('click', event => {
        const anchor = event.target.closest('a[href]');
        if (!anchor || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || anchor.target === '_blank') return;
        if (/log\s*out/i.test(anchor.textContent) || /logout/i.test(anchor.id)) {
            event.preventDefault(); event.stopImmediatePropagation(); window.UimsFirebase.logout(); return;
        }
        const url = new URL(anchor.href, location.href);
        if (url.origin === endpoint.origin && /\/Auth\/Login(?:\.aspx)?$/i.test(url.pathname)) sessionStorage.removeItem(storageKey);
        if (url.origin !== endpoint.origin || !/\/(Student|Company|Academic|Coordinator)\/[^/]+(?:\.aspx)?$/i.test(url.pathname)) return;
        event.preventDefault(); navigate(url.href).catch(showError);
    });
    if (!form && /Dashboard(?:\.aspx)?$/i.test(location.pathname)) {
        document.querySelectorAll('main tbody').forEach(body => { body.replaceChildren(); const row = body.insertRow(); const cell = row.insertCell(); cell.colSpan = 20; cell.textContent = 'Live dashboard records are pending verified schema mapping.'; });
        document.querySelectorAll('main .list-row').forEach(row => row.remove());
        const notice = document.createElement('p'); notice.className = 'alert alert-info'; notice.setAttribute('role', 'status'); notice.textContent = 'Your Firebase identity is verified. Dashboard business data is not yet mapped; live writes remain disabled.';
        document.querySelector('main')?.prepend(notice);
        request('identity').then(data => { if (data.role === 'admin') schemaTools(); }).catch(showError);
    }
    window.UimsFirebase = Object.freeze({ request, acceptSignIn, getIdToken, navigate, async logout() {
        try { await request('logout'); } catch (_) { /* local tab tokens are still cleared below */ }
        sessionStorage.removeItem(storageKey);
        location.href = new URL('Auth/Login.aspx', new URL('./', endpoint)).href;
    } });
    async function restoreSession() {
        if (new URLSearchParams(location.search).get('account') === 'new') { sessionStorage.removeItem(storageKey); return; }
        const auth = load();
        if (!auth?.refreshToken || new URLSearchParams(location.search).get('change') === 'required' || document.querySelector('[data-password-required="true"]')) return;
        busy = true; const button = document.getElementById('SignIn'); button.disabled = true;
        status('Restoring your session…');
        try { await acceptSignIn(await request('refresh', { refreshToken: auth.refreshToken })); }
        catch (error) { showError(error); }
        finally { busy = false; button.disabled = performance.now() < cooldownUntil; }
    }
    // A login page with no saved session needs no Firebase request until the user submits.
    if (form) restoreSession();
    const originalFetch = window.fetch.bind(window);
    window.fetch = async function (input, options = {}) {
        const url = new URL(input instanceof Request ? input.url : input, location.href);
        if (url.origin === endpoint.origin && /\/(?:TasksApi|TaskExamsApi|FirebaseApi)\.ashx$/i.test(url.pathname)) {
            if (/\/(?:TasksApi|TaskExamsApi)\.ashx$/i.test(url.pathname)) { const role = /^\/Company\//i.test(location.pathname) ? 'companySupervisor' : /^\/Student\//i.test(location.pathname) ? 'student' : null; if (role) { const headers = new Headers(options.headers || (input instanceof Request ? input.headers : undefined)); headers.set('X-UIMS-Role', role); options = { ...options, headers }; } }
            if (load()?.tabTicket && /\/(?:TasksApi|TaskExamsApi)\.ashx$/i.test(url.pathname)) await getIdToken();
            const ticket = load()?.tabTicket;
            if (ticket) { const headers = new Headers(options.headers || (input instanceof Request ? input.headers : undefined)); headers.set('X-UIMS-Session', ticket); options = { ...options, headers }; }
        }
        const response = await originalFetch(input, options);
        if (response.ok && (options.method || (input instanceof Request ? input.method : 'GET')).toUpperCase() === 'POST' && /\/(?:TasksApi|TaskExamsApi)\.ashx$/i.test(url.pathname) && url.searchParams.get('action') !== 'answers') window.dispatchEvent(new Event('task-data-changed'));
        return response;
    };
    if (form) { const other = document.createElement('a'); other.href = new URL('Auth/Login.aspx?account=new', new URL('./', endpoint)).href; other.target = '_blank'; other.rel = 'noopener'; other.textContent = 'Sign in to another account in a separate tab'; (document.getElementById('loginContent') || form).append(other); }
})();
