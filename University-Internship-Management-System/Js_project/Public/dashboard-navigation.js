// Real sign-out for coordinator menus using this shared navigation script.
document.addEventListener('click', async event => {
    const link = event.target.closest('[data-uims-logout]');
    if (!link) return;
    event.preventDefault();
    if (link.dataset.busy === 'true') return;
    link.dataset.busy = 'true';
    try {
        const url = new URL('../FirebaseApi.ashx', link.href);
        const response = await fetch(url, {method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json'},body:JSON.stringify({action:'logout'})});
        const result = await response.json();
        if (!response.ok || !result.success) throw Error('Sign out failed');
        sessionStorage.removeItem('uims.firebase.authentication');
        location.assign(link.href);
    } catch { window.alert('Could not log out. Please try again.'); link.dataset.busy = 'false'; }
});
(() => {
    'use strict';
    if(document.querySelector('.workspace-canonical-sidebar'))return;
    const sidebar = document.getElementById('dashboardSidebar');
    if (!sidebar) {
        const legacySidebar = document.getElementById('sidebar');
        const legacyMain = document.querySelector('.main');
        const menu = document.querySelector('.menu-btn');
        const close = document.querySelector('.legacy-sidebar-close');
        if (!legacySidebar || !menu || !close) return;
        const mobile = window.matchMedia('(max-width: 991.98px)');
        function setLegacyOpen(open, restoreFocus = true) {
            open = Boolean(open && mobile.matches);
            document.body.classList.toggle('sidebar-open', open);
            menu.setAttribute('aria-expanded', String(open));
            legacySidebar.inert = mobile.matches && !open;
            if (legacyMain) legacyMain.inert = open;
            if (open) close.focus();
            else if (restoreFocus && mobile.matches) menu.focus();
        }
        window.toggleSidebar = () => setLegacyOpen(!document.body.classList.contains('sidebar-open'));
        close.addEventListener('click', () => setLegacyOpen(false));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && document.body.classList.contains('sidebar-open')) setLegacyOpen(false);
        });
        legacySidebar.addEventListener('keydown', event => {
            if (!mobile.matches || !document.body.classList.contains('sidebar-open') || event.key !== 'Tab') return;
            const elements = [...legacySidebar.querySelectorAll('a[href], button')].filter(element => element.getClientRects().length);
            const first = elements[0], last = elements[elements.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        });
        mobile.addEventListener('change', () => setLegacyOpen(false, false));
        setLegacyOpen(false, false);
        return;
    }
    const toggle = document.getElementById('dashboardMenuToggle');
    const backdrop = document.querySelector('.dash-sidebar-backdrop');
    const main = document.querySelector('.dash-main');
    const profile = document.querySelector('.dash-profile');
    const mobile = window.matchMedia('(max-width: 991.98px)');
    function setOpen(open, restoreFocus = true) {
        const active = open && mobile.matches;
        document.body.classList.toggle('dash-sidebar-open', active);
        toggle.setAttribute('aria-expanded', String(active));
        toggle.setAttribute('aria-label', active ? 'Close navigation' : 'Open navigation');
        backdrop.hidden = !active;
        main.inert = active;
        sidebar.inert = mobile.matches && !active;
        if (active) sidebar.querySelector('[data-sidebar-close]').focus();
        else if (restoreFocus && mobile.matches) toggle.focus();
    }
    toggle.addEventListener('click', () => setOpen(toggle.getAttribute('aria-expanded') !== 'true'));
    document.querySelectorAll('[data-sidebar-close]').forEach(button => button.addEventListener('click', () => setOpen(false)));
    sidebar.querySelectorAll('a').forEach(link => link.addEventListener('click', () => setOpen(false, false)));
    mobile.addEventListener('change', () => setOpen(false, false));
    sidebar.addEventListener('keydown', event => {
        if (!mobile.matches || toggle.getAttribute('aria-expanded') !== 'true' || event.key !== 'Tab') return;
        const elements = [...sidebar.querySelectorAll('a[href], button')].filter(element => element.getClientRects().length);
        const first = elements[0];
        const last = elements[elements.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        if (toggle.getAttribute('aria-expanded') === 'true') { event.preventDefault(); setOpen(false); }
        else if (profile.open) { profile.open = false; profile.querySelector('summary').focus(); }
    });
    document.addEventListener('click', event => { if (!profile.contains(event.target)) profile.open = false; });
    const search = document.getElementById('dashboardSearch');
    const target = document.getElementById(search.dataset.searchTarget);
    if (target) {
        const sync = () => { search.value = target.value; search.disabled = target.disabled; };
        search.addEventListener('input', () => { target.value = search.value; target.dispatchEvent(new Event('input', {bubbles: true})); });
        search.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
        target.addEventListener('input', sync);
        new MutationObserver(sync).observe(target, {attributes: true, attributeFilter: ['disabled']});
        document.addEventListener('click', event => { if (event.target.closest('button')) queueMicrotask(sync); });
        sync();
    } else search.disabled = true;
    setOpen(false, false);
})();
