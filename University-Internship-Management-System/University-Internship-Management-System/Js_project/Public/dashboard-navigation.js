(() => {
    'use strict';
    const sidebar = document.getElementById('dashboardSidebar');
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
