// Shared navigation presentation only. Page filters and data workflows are untouched.
(function () {
    'use strict';
    const toggle = document.querySelector('.shared-workspace-shell .workspace-menu-toggle');
    if (!toggle) return;
    const sidebar = document.getElementById(toggle.getAttribute('aria-controls'));
    const close = sidebar && sidebar.querySelector('[data-sidebar-close]');
    const backdrop = document.querySelector('.shared-workspace-shell .uims-unified-backdrop');
    const main = document.querySelector('.shared-workspace-shell .dash-main');
    if (!sidebar || !close || !backdrop || !main) return;
    const mobile = window.matchMedia('(max-width: 991.98px)');
    function setOpen(requested, restoreFocus) {
        const open = Boolean(requested && mobile.matches);
        document.body.classList.toggle('workspace-sidebar-open', open);
        toggle.setAttribute('aria-expanded', String(open));
        sidebar.inert = mobile.matches && !open;
        main.inert = open;
        if (open) close.focus({ preventScroll: true });
        else if (restoreFocus && mobile.matches) toggle.focus({ preventScroll: true });
    }
    toggle.addEventListener('click', () => setOpen(toggle.getAttribute('aria-expanded') !== 'true', true));
    close.addEventListener('click', () => setOpen(false, true));
    backdrop.addEventListener('click', () => setOpen(false, true));
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && toggle.getAttribute('aria-expanded') === 'true') setOpen(false, true);
    });
    sidebar.addEventListener('keydown', event => {
        if (event.key !== 'Tab' || toggle.getAttribute('aria-expanded') !== 'true') return;
        const items = [...sidebar.querySelectorAll('a[href],button:not([disabled])')].filter(item => item.getClientRects().length);
        const first = items[0], last = items[items.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });
    mobile.addEventListener('change', () => setOpen(false, false));
    setOpen(false, false);
}());
