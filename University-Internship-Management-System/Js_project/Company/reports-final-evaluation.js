/* Company drafts are loaded and saved by Web Forms; this file owns interactions only. */
(function () {
    'use strict';
    let binding;
    function initialize() {
        binding?.abort();
        binding = new AbortController();
        const signal = binding.signal;
        const by = id => document.getElementById(id) || document.querySelector('[id$="_' + id + '"]');
        const all = selector => [...document.querySelectorAll(selector)];
        const dialog = by('traineeDialog');
        const submissionDialog = by('submissionDialog');
        by('ReviewSubmission')?.addEventListener('click', () => {
            if (by('ReviewSubmission').disabled) return;
            dialog?.close();
            if (submissionDialog && !submissionDialog.open) submissionDialog.showModal();
        }, { signal });
        const returnToEvaluation = () => {
            submissionDialog?.close();
            if (dialog && !dialog.open) dialog.showModal();
        };
        by('closeSubmissionDialog')?.addEventListener('click', returnToEvaluation, { signal });
        submissionDialog?.addEventListener('cancel', event => { event.preventDefault(); returnToEvaluation(); }, { signal });
        let dirty = !!document.querySelector('.workspace-error')?.textContent.trim();
        let opener;
        const setView = view => {
            const field = by('ActiveView');
            if (field) field.value = view;
            all('#traineeDialog [data-workspace-view]').forEach(panel => panel.hidden = panel.dataset.workspaceView !== view);
            if (view !== 'trainees' && dialog && by('InternDetails')) {
                if (!dialog.open) { opener = document.activeElement; dialog.showModal(); }
            }
        };
        const close = () => {
            if (dirty && !window.confirm('Discard unsaved changes?')) return false;
            dirty = false;
            dialog?.close();
            if (by('ActiveView')) by('ActiveView').value = 'trainees';
            opener?.focus();
            return true;
        };
        all('[data-open-view]').forEach(button => button.addEventListener('click', event => {
            event.preventDefault();
            if (button.dataset.openView === 'trainees') close(); else setView(button.dataset.openView);
        }, { signal }));
        dialog?.addEventListener('input', () => { dirty = true; }, { signal });
        dialog?.addEventListener('cancel', event => { event.preventDefault(); close(); }, { signal });
        dialog?.addEventListener('click', event => {
            if (event.target === dialog) {
                const rect = dialog.getBoundingClientRect();
                if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) close();
            }
        }, { signal });
        by('CancelEvaluation')?.addEventListener('click', event => { event.preventDefault(); close(); }, { signal });
        all('[data-target-view]').forEach(link => link.addEventListener('click', event => {
            if (dirty && !window.confirm('Discard unsaved changes and open another trainee?')) event.preventDefault();
        }, { signal }));
        const filter = () => {
            const query = by('traineeSearch')?.value.trim().toLowerCase() || '';
            const status = by('evaluationFilter')?.value || 'all';
            if (by('SearchValue')) by('SearchValue').value = query;
            if (by('StatusValue')) by('StatusValue').value = status;
            let count = 0;
            all('[data-trainee-row]').forEach(row => {
                const visible = row.textContent.toLowerCase().includes(query) && (status === 'all' || row.dataset.status === status);
                row.hidden = !visible; if (visible) count++;
            });
            if (by('noTraineeMatches')) by('noTraineeMatches').hidden = count > 0;
            if (by('traineeSearchStatus')) by('traineeSearchStatus').textContent = count + ' trainees shown';
        };
        if (by('traineeSearch')) by('traineeSearch').value = by('SearchValue')?.value || '';
        if (by('evaluationFilter')) by('evaluationFilter').value = by('StatusValue')?.value || 'all';
        by('traineeSearch')?.addEventListener('input', filter, { signal });
        by('evaluationFilter')?.addEventListener('change', filter, { signal });
        all('[data-clear-filters]').forEach(button => button.addEventListener('click', () => {
            if (by('traineeSearch')) by('traineeSearch').value = '';
            if (by('evaluationFilter')) by('evaluationFilter').value = 'all';
            filter();
        }, { signal }));
        all('[data-close-report]').forEach(button => button.addEventListener('click', () => {
            if (by('ReportDetailsPanel')) by('ReportDetailsPanel').hidden = true;
            if (by('ReportMode')) by('ReportMode').value = '';
        }, { signal }));
        all('[data-review-report]').forEach(button => button.addEventListener('click', () => {
            if (by('ReportFeedbackPanel')) by('ReportFeedbackPanel').hidden = false;
            if (by('ReportMode')) by('ReportMode').value = 'review';
        }, { signal }));
        const media = window.matchMedia('(max-width:991.98px)');
        const sidebar = by('sidebar');
        const toggle = document.querySelector('.menu-btn');
        const setSidebar = open => {
            const active = media.matches && open;
            document.body.classList.toggle('sidebar-open', active);
            toggle?.setAttribute('aria-expanded', String(active));
            if (sidebar) sidebar.inert = media.matches && !active;
        };
        toggle?.addEventListener('click', () => setSidebar(!document.body.classList.contains('sidebar-open')), { signal });
        document.querySelector('.sidebar-close')?.addEventListener('click', () => setSidebar(false), { signal });
        document.querySelector('.sidebar-backdrop')?.addEventListener('click', () => setSidebar(false), { signal });
        media.addEventListener('change', () => setSidebar(false), { signal });
        setSidebar(false);
        filter();
        setView(by('ActiveView')?.value || 'trainees');
        // SaveDraft and PreviewDraft retain their ASP.NET postback handlers. No optimistic success or client persistence.
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true });
    else initialize();
    if (window.Sys?.WebForms?.PageRequestManager) {
        const manager = Sys.WebForms.PageRequestManager.getInstance();
        manager.add_endRequest((sender, args) => {
            if (args.get_error()) {
                const message = byError();
                if (message) message.textContent = 'The request could not be completed. Your entered values are unchanged. Check the saved draft before retrying.';
                args.set_errorHandled(true);
            }
            initialize();
        });
    }
    function byError() { return document.querySelector('.workspace-error'); }
})();
