(() => {
    'use strict';
    const endpoint = new URL('../FirebaseApi.ashx', document.currentScript.src);
    const storageKey = 'uims.firebase.authentication';
    const form = document.querySelector('.auth-layout');
    let busy = false, refreshing;
    function load() { try { return JSON.parse(sessionStorage.getItem(storageKey) || 'null'); } catch { sessionStorage.removeItem(storageKey); return null; } }
    function save(data) {
        const lifetime = Number(data.expiresIn);
        if (!data.idToken || !data.refreshToken || !Number.isFinite(lifetime) || lifetime <= 0) throw Error('Firebase returned an incomplete authentication session.');
        sessionStorage.setItem(storageKey, JSON.stringify({ idToken: data.idToken, refreshToken: data.refreshToken, expiresAt: Date.now() + lifetime * 1000 }));
    }
    async function request(action, payload = {}) {
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
                    throw Error('The Firebase API returned an HTML page (HTTP ' + response.status + '). Start the integration project containing FirebaseApi.ashx, then try again.');
                }
                throw Error('The Firebase API returned an invalid response (HTTP ' + response.status + '). Check the IIS Express output and server logs.');
            }
            if (!response.ok || !result.success) {
                if (response.status === 401 && action !== 'signIn') sessionStorage.removeItem(storageKey);
                throw Error(result.message || 'The operation could not be completed.');
            }
            return result.data;
        } catch (error) { if (error.name === 'AbortError') throw Error('The request timed out. No successful operation was confirmed.'); throw error; }
        finally { clearTimeout(timeout); }
    }
    function status(message) {
        let node = document.getElementById('firebaseConnectionStatus');
        if (!node) { node = document.createElement('p'); node.id = 'firebaseConnectionStatus'; node.className = 'auth-message'; node.setAttribute('role', 'status'); (document.getElementById('loginContent') || document.body).append(node); }
        node.textContent = message;
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
        const email = document.getElementById('Email'), password = document.getElementById('Password');
        if (!email.value.trim() || !email.checkValidity() || !password.value) { status('Enter a valid email and password.'); return; }
        busy = true; const button = document.getElementById('SignIn'); button.disabled = true; form.setAttribute('aria-busy', 'true');
        sessionStorage.removeItem(storageKey); status('Verifying Firebase Authentication and the UID/profile mapping…');
        try { await acceptSignIn(await request('signIn', { email: email.value.trim(), password: password.value })); }
        catch (error) { status(error.message); }
        finally { busy = false; button.disabled = false; form.removeAttribute('aria-busy'); }
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
        const bootstrap = document.getElementById('attendanceBootstrap');
        if (bootstrap && JSON.parse(bootstrap.textContent).localDemo && /\/(Student\/MyAttendance|Company\/VerifyAttendance)(?:\.aspx)?$/i.test(url.pathname)) { location.assign(url.href); return; }
        const token = await getIdToken();
        const post = document.createElement('form'); post.method = 'post'; post.action = url.href;
        const input = document.createElement('input'); input.type = 'hidden'; input.name = '__FirebaseToken'; input.value = token;
        post.append(input); document.body.append(post); post.submit();
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
        event.preventDefault(); navigate(url.href).catch(error => status(error.message));
    });
    if (!form && /Dashboard(?:\.aspx)?$/i.test(location.pathname)) {
        document.querySelectorAll('main tbody').forEach(body => { body.replaceChildren(); const row = body.insertRow(); const cell = row.insertCell(); cell.colSpan = 20; cell.textContent = 'Live dashboard records are pending verified schema mapping.'; });
        document.querySelectorAll('main .list-row').forEach(row => row.remove());
        const notice = document.createElement('p'); notice.className = 'alert alert-info'; notice.setAttribute('role', 'status'); notice.textContent = 'Your Firebase identity is verified. Dashboard business data is not yet mapped; live writes remain disabled.';
        document.querySelector('main')?.prepend(notice);
        request('identity').then(data => { if (data.role === 'admin') schemaTools(); }).catch(error => status(error.message));
    }
    window.UimsFirebase = Object.freeze({ request, acceptSignIn, getIdToken, navigate, async logout() {
        try { await request('logout'); } catch (_) { /* local tab tokens are still cleared below */ }
        sessionStorage.removeItem(storageKey);
        location.href = new URL('Auth/Login.aspx', new URL('./', endpoint)).href;
    } });
    if (form) request('configuration').then(data => status(data.message)).catch(error => status(error.message));
})();
