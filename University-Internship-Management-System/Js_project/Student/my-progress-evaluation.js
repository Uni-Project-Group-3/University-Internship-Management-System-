/* UI interactions only. Student identity, completion state and PDF authorization are server controlled. */
(function () {
    'use strict';
    function initialize() {
        const by = id => document.getElementById(id), sidebar = by('sidebar'), toggle = document.querySelector('.menu-btn'), media = matchMedia('(max-width:991.98px)');
        const setOpen = show => { const active = media.matches && show; document.body.classList.toggle('sidebar-open', active); toggle?.setAttribute('aria-expanded', String(active)); if (sidebar) sidebar.inert = media.matches && !active; const main = document.querySelector('.main'); if (main) main.inert = active; if (active) sidebar?.querySelector('button,a[href]')?.focus(); };
        toggle?.addEventListener('click', () => setOpen(!document.body.classList.contains('sidebar-open')));
        document.querySelectorAll('.sidebar-close,.sidebar-backdrop').forEach(button => button.addEventListener('click', () => { setOpen(false); toggle?.focus(); }));
        document.addEventListener('keydown', event => { if (event.key === 'Escape') setOpen(false); });
        media.addEventListener('change', () => setOpen(false)); setOpen(false);
        const filter = () => { if (!document.querySelector('[data-task-status]')) return; const status = by('taskFilter')?.value || ''; let shown = 0; document.querySelectorAll('[data-task-status]').forEach(row => { row.hidden = !!status && status !== 'all' && row.dataset.taskStatus !== status; if (!row.hidden) shown++; }); if (by('taskFilterStatus')) by('taskFilterStatus').textContent = shown + ' student-visible tasks shown'; };
        by('taskFilter')?.addEventListener('change', filter); filter();
        const dialog = by('examDialog'); document.querySelectorAll('[data-close-exam]').forEach(button => button.addEventListener('click', () => dialog?.close()));
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
})();
