/* UI interactions only. Academic drafts and company reviews come from authorized server services. */
(function () {
    'use strict';
    const by = id => document.getElementById(id);
    const all = selector => [...document.querySelectorAll(selector)];
    function initialize() {
        const dialog = by('evaluationDialog');
        let view = by('academicActiveView')?.value || 'details', previousView = view, dirty = false, opener;
        const baseline = all('#evaluationDialog input[type="number"],#evaluationDialog textarea').map(field => [field, field.value]);
        const show = next => {
            view = ['details', 'editor', 'discard', 'confirmation'].includes(next) ? next : 'editor';
            if (by('academicActiveView')) by('academicActiveView').value = view;
            all('#evaluationDialog [data-view]').forEach(el => el.hidden = el.dataset.view !== view);
            all('#evaluationDialog [data-footer]').forEach(el => el.hidden = el.dataset.footer !== view);
            by('evaluationDialogTitle').textContent = view === 'details' ? 'Student details & company evaluation' : view === 'discard' ? 'Unsaved academic changes' : view === 'confirmation' ? 'Confirm academic submission' : 'Academic evaluation draft';
        };
        const open = next => {
            if (!dialog.open) { opener = document.activeElement; dialog.showModal(); }
            show(next);
        };
        const close = () => {
            if (dirty) { previousView = view; show('discard'); return; }
            dialog.close(); dirty = false; opener?.focus();
        };
        all('#evaluationDialog [data-close],#closeEvaluationDialog').forEach(button => button.addEventListener('click', close));
        dialog.addEventListener('cancel', event => { event.preventDefault(); close(); });
        dialog.addEventListener('input', () => { dirty = true; });
        by('continueEvaluation').addEventListener('click', () => show(previousView));
        by('discardEvaluation').addEventListener('click', () => {
            baseline.forEach(([field, value]) => { field.value = value; }); dirty = false; close();
        });
        all('[data-academic-view]').forEach(button => button.addEventListener('click', () => show(button.dataset.academicView)));
        all('[data-preview]').forEach(button => {
            button.disabled = !by('academicSelectedInternship')?.value;
            button.addEventListener('click', () => open(button.dataset.preview === 'details' ? 'details' : 'editor'));
        });
        by('academicComments')?.addEventListener('input', () => {
            by('commentsCounter').textContent = by('academicComments').value.length + ' / 2000';
        });
        if (by('commentsCounter') && by('academicComments')) by('commentsCounter').textContent = by('academicComments').value.length + ' / 2000';
        // Enablement is supplied by the server. Browser values never authorize submission.
        by('reviewEvaluation').addEventListener('click', () => {
            if (by('reviewEvaluation').disabled) return;
            by('confirmationComments').textContent = by('academicComments').value;
            show('confirmation');
        });
        by('backToEvaluation').addEventListener('click', () => show('editor'));
        const filter = () => {
            const query = by('evaluationSearch').value.trim().toLowerCase(), status = by('evaluationStatus').value;
            let count = 0;
            all('[data-academic-row]').forEach(row => {
                const visible = row.textContent.toLowerCase().includes(query) && (!status || row.dataset.status === status);
                row.hidden = !visible; if (visible) count++;
            });
            document.querySelector('.evaluation-empty').hidden = count > 0;
        };
        by('evaluationSearch').addEventListener('input', filter); by('evaluationStatus').addEventListener('change', filter);
        by('clearEvaluationFilters').addEventListener('click', () => { by('evaluationSearch').value = ''; by('evaluationStatus').value = ''; filter(); });
        const count = all('[data-academic-row]').length;
        if (count) {
            document.querySelector('.evaluation-stats strong').textContent = count;
            document.querySelector('.evaluation-panel-footer span').textContent = count + ' assigned internships';
        }
        filter();
        if (by('academicSelectedInternship')?.value) open(view);
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true });
    else initialize();
})();
