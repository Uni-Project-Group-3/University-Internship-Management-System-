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
        const dashboard = document.querySelector('.completion-dashboard');
        const panel = by('studentDetailsPanel');
        const setDetails = visible => {
            if (panel) panel.hidden = !visible;
            dashboard?.classList.toggle('details-closed', !visible);
        };
        // Selection visibility is UI state only; it never grants access to a record.
        const selectedRow = document.querySelector('[data-completion-row][aria-selected="true"]');
        setDetails(!!selectedRow);
        if (by('showStudentDetails')) by('showStudentDetails').disabled = !selectedRow;
        by('showStudentDetails')?.addEventListener('click', () => { setDetails(true); panel?.focus(); });
        if (by('detailsVisible')?.value === 'true' && selectedRow) panel?.focus({ preventScroll: true });
        by('closeStudentDetails')?.addEventListener('click', () => {
            setDetails(false);
            by('completionSearch')?.focus();
        });
        const search = by('completionSearch'), navbarSearch = by('navbarSearch'), academic = by('academicFilter'), completion = by('completionFilter'), period = by('periodFilter');
        const filter = () => {
            const rows = [...document.querySelectorAll('[data-completion-row]')];
            let count = 0;
            rows.forEach(row => {
                const visible = row.textContent.toLowerCase().includes((search?.value || '').trim().toLowerCase()) &&
                    (!academic?.value || row.dataset.academic === academic.value) &&
                    (!completion?.value || row.dataset.completion === completion.value) &&
                    (!period?.value || row.dataset.period === period.value);
                row.hidden = !visible;
                if (visible) count++;
            });
            if (by('completionResultCount')) by('completionResultCount').textContent = rows.length ? count + ' of ' + rows.length : '';
            if (by('noCompletionMatches')) by('noCompletionMatches').hidden = !rows.length || count > 0;
        };
        [search, academic, completion, period].forEach(control => control?.addEventListener(control === search ? 'input' : 'change', filter));
        navbarSearch?.addEventListener('input', () => { if (search) search.value = navbarSearch.value; filter(); });
        by('clearCompletionFilters')?.addEventListener('click', () => {
            [search, navbarSearch, academic, completion, period].forEach(control => { if (control) control.value = ''; });
            filter(); search?.focus();
        });
        filter();
        const tabs = [...document.querySelectorAll('[data-review-tab]')];
        const activate = tab => {
            tabs.forEach(item => {
                const selected = item === tab;
                item.setAttribute('aria-selected', String(selected)); item.tabIndex = selected ? 0 : -1;
                const content = by(item.dataset.reviewTab); if (content) content.hidden = !selected;
            });
        };
        tabs.forEach((tab, index) => {
            tab.addEventListener('click', () => activate(tab));
            tab.addEventListener('keydown', event => {
                let target;
                if (event.key === 'ArrowRight') target = tabs[(index + 1) % tabs.length];
                if (event.key === 'ArrowLeft') target = tabs[(index + tabs.length - 1) % tabs.length];
                if (event.key === 'Home') target = tabs[0];
                if (event.key === 'End') target = tabs[tabs.length - 1];
                if (target) { event.preventDefault(); activate(target); target.focus(); }
            });
        });
        if (by('historyEmpty')) by('historyEmpty').hidden = !!by('decisionHistory')?.children.length;
        by('reviewForm')?.addEventListener('submit', () => {
            if (by('completionLoading')) by('completionLoading').hidden = false;
            document.querySelector('.review-page')?.setAttribute('aria-busy', 'true');
            // Preserve submitted button names; disable only after the browser serializes the form.
            window.setTimeout(() => document.querySelectorAll('.review-page button[type=submit], .review-page input[type=submit]').forEach(button => { button.disabled = true; }), 0);
        });
        by('approvalModal')?.addEventListener('shown.bs.modal', () => by('confirmApproval')?.focus());

    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
})();
