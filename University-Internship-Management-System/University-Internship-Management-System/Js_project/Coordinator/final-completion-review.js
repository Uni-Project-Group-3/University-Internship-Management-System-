/* UI only. Authorization, readiness, confirmation and decision persistence are server controlled. No browser evaluation storage or documents. */
(function () {
    'use strict';
    function initialize() {
        const by = id => document.getElementById(id);
        const sidebar = by('coordinatorSidebar'), backdrop = by('sidebarBackdrop'), opener = by('openSidebar');
        const close = () => { sidebar?.classList.remove('is-open'); document.body.classList.remove('sidebar-open'); opener?.setAttribute('aria-expanded', 'false'); backdrop?.setAttribute('aria-hidden', 'true'); };
        opener?.addEventListener('click', () => { sidebar?.classList.add('is-open'); document.body.classList.add('sidebar-open'); opener.setAttribute('aria-expanded', 'true'); backdrop?.setAttribute('aria-hidden', 'false'); });
        by('closeSidebar')?.addEventListener('click', close); backdrop?.addEventListener('click', close);
        document.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });
        by('navbarSearch')?.addEventListener('input', event => {
            const query = event.target.value.trim().toLowerCase();
            document.querySelectorAll('#studentSelect option').forEach(option => { option.hidden = !option.textContent.toLowerCase().includes(query); });
        });
        by('approvalModal')?.addEventListener('shown.bs.modal', () => by('confirmApproval')?.focus());

    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
})();
