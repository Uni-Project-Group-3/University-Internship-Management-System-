(() => {
    'use strict';
    const dialog = document.getElementById('completionDialog');
    let opener;
    const titles = {pending: 'Confirm your completion', grace: 'Certificate & account access', record: 'Student details'};
    document.querySelectorAll('[data-completion-open]').forEach(button => button.addEventListener('click', () => {
        opener = button;
        const view = button.dataset.completionOpen;
        document.querySelectorAll('[data-completion-view]').forEach(section => { section.hidden = section.dataset.completionView !== view; });
        document.getElementById('completionDialogTitle').textContent = titles[view];
        const confirm = document.getElementById('previewConfirmCompletion');
        if (confirm) { confirm.disabled = false; document.getElementById('completionConfirmMessage').textContent = ''; }
        dialog.showModal();
        dialog.scrollTop = 0;
    }));
    dialog.querySelectorAll('[data-completion-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
    dialog.addEventListener('close', () => opener?.focus());
    dialog.addEventListener('click', event => {
        const rect = dialog.getBoundingClientRect();
        if (event.target === dialog && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom)) dialog.close();
    });
    dialog.addEventListener('keydown', event => {
        if (event.key !== 'Tab') return;
        const elements = [...dialog.querySelectorAll('button:not([disabled]), select:not([disabled])')].filter(element => element.getClientRects().length);
        const first = elements[0];
        const last = elements[elements.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });
    document.getElementById('previewConfirmCompletion')?.addEventListener('click', event => {
        event.currentTarget.disabled = true;
        document.getElementById('completionConfirmMessage').textContent = 'Preview only: no confirmation was recorded and no deadline was started. Use Preview certificate access on the dashboard to inspect the next layout.';
    });
    const state = document.getElementById('completionRecordState');
    const descriptions = {
        training: 'Training and academic evaluations are in progress.',
        pending: 'Coordinator approval is awaiting student acknowledgment. The account stays enabled and the one-hour period has not started.',
        grace: 'After confirmed completion, the student has 60 minutes for secure certificate access. No actual confirmation or deadline is loaded in this preview.',
        disabled: 'This layout represents confirmed account deactivation. Firebase must confirm the operation before the real account is reported disabled. Academic records and certificate metadata are retained.'
    };
    state?.addEventListener('change', () => {
        document.getElementById('completionRecordBadge').textContent = state.selectedOptions[0].textContent;
        document.getElementById('completionRecordDescription').textContent = descriptions[state.value];
        document.getElementById('completionRecordPreview').classList.toggle('completion-record--completed', ['grace', 'disabled'].includes(state.value));
    });
    const search = document.getElementById('completionSearch');
    const filter = document.getElementById('completionStatusFilter');
    function updateEmptyState() {
        document.getElementById('completionEmptyTitle').textContent = search.value.trim() || filter.value ? 'No connected records to filter' : 'Student records are not connected';
    }
    search?.addEventListener('input', updateEmptyState);
    search?.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
    filter?.addEventListener('change', updateEmptyState);
    document.getElementById('completionClearFilters')?.addEventListener('click', () => { search.value = ''; filter.value = ''; updateEmptyState(); search.focus(); });
})();
