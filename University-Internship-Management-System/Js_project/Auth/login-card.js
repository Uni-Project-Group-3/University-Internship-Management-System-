(() => {
    'use strict';
    const workspace = document.getElementById('workspace');
    if (!workspace) return;
    const email = document.getElementById('Email'), password = document.getElementById('Password');
    const flipper = document.getElementById('cardFlipper'), tilt = document.getElementById('cardTilt');
    const status = document.getElementById('cardAuthStatus');
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
    let started = 0, phase = 'idle';
    const pause = ms => new Promise(resolve => setTimeout(resolve, reduced.matches ? 0 : ms));
    function preview() {
        const value = email.value.trim(), count = password.value.length;
        document.getElementById('previewEmail').textContent = value || 'University email';
        document.getElementById('previewEmail').classList.toggle('placeholder', !value);
        // Only a count is mirrored. The password itself never enters another DOM node.
        document.getElementById('previewPassword').textContent = count ? '•'.repeat(Math.min(count, 18)) : '••••••••';
    }
    [email, password].forEach(input => {
        input.addEventListener('input', preview); input.addEventListener('change', preview);
        input.addEventListener('focus', () => {
            document.getElementById(input === email ? 'previewEmailField' : 'previewPasswordField').classList.add('is-active');
        });
        input.addEventListener('blur', () => document.getElementById(input === email ? 'previewEmailField' : 'previewPasswordField').classList.remove('is-active'));
    });
    workspace.addEventListener('pointermove', event => {
        if (reduced.matches || event.pointerType === 'touch' || phase !== 'idle') return;
        const bounds = workspace.getBoundingClientRect();
        tilt.style.setProperty('--rx', ((.5 - (event.clientY - bounds.top) / bounds.height) * 9) + 'deg');
        tilt.style.setProperty('--ry', (((event.clientX - bounds.left) / bounds.width - .5) * 12) + 'deg');
    });
    workspace.addEventListener('pointerleave', () => { tilt.style.setProperty('--rx', '0deg'); tilt.style.setProperty('--ry', '0deg'); });
    window.UimsLoginAnimation = Object.freeze({
        begin() {
            phase = 'loading'; started = performance.now(); preview();
            workspace.classList.remove('is-error', 'is-success'); workspace.classList.add('is-scanning');
            tilt.style.setProperty('--rx', '0deg'); tilt.style.setProperty('--ry', '0deg');
            flipper.classList.add('flipped'); status.textContent = 'Signing in…';
        },
        async success() {
            if (phase !== 'loading') this.begin();
            await pause(Math.max(0, 850 - (performance.now() - started)));
            workspace.classList.remove('is-scanning'); workspace.classList.add('is-success');
            status.textContent = 'Signed in'; phase = 'success'; preview();
            await pause(280);
        },
        async error() {
            await pause(Math.max(0, 400 - (performance.now() - started)));
            workspace.classList.remove('is-scanning'); workspace.classList.add('is-error');
            flipper.classList.remove('flipped'); status.textContent = 'Sign-in unsuccessful';
            await pause(450); phase = 'idle'; workspace.classList.remove('is-error');
        }
    });
    // Native Web Forms posting still works if the enhanced authentication client is unavailable.
    document.querySelector('.auth-layout').addEventListener('submit', event => {
        if (!event.defaultPrevented && (!event.submitter || event.submitter.dataset.action === 'login') && email.validity.valid && email.value && password.value)
            window.UimsLoginAnimation.begin();
    });
    window.addEventListener('pageshow', () => {
        phase = 'idle'; flipper.classList.remove('flipped'); workspace.classList.remove('is-scanning', 'is-success', 'is-error'); preview();
    });
    preview();
})();
