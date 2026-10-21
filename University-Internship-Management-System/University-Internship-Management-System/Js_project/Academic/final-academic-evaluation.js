(() => {
    'use strict';

    const byId = id => document.getElementById(id);
    const dialog = byId('evaluationDialog');
    const comments = byId('academicComments');
    const menu = document.querySelector('.evaluation-preview-menu');
    let opener;
    let view = 'details';
    let previousView = 'editor';
    let savedComments = '';
    let hasPreviewDraft = false;
    const isDirty = () => comments.value !== savedComments;
    const titles = {details: 'Student details', editor: 'Evaluate student', confirmation: 'Confirm evaluation submission', submitted: 'View submitted evaluation', discard: 'Unsaved changes'};

    function showView(nextView) {
        view = nextView;
        dialog.querySelectorAll('[data-view]').forEach(section => { section.hidden = section.dataset.view !== view; });
        dialog.querySelectorAll('[data-footer]').forEach(section => { section.hidden = section.dataset.footer !== view; });
        byId('evaluationDialogTitle').textContent = view === 'editor' && hasPreviewDraft ? 'Edit preview draft' : titles[view];
        byId('closeEvaluationDialog').focus();
        dialog.scrollTop = 0;
    }

    function updateComments() {
        byId('commentsCounter').textContent = `${comments.value.length} / 2000`;
        const invalid = comments.value.length > 0 && comments.value.trim().length < 10;
        byId('academicCommentsError').textContent = invalid ? 'Use at least 10 characters for this preview.' : '';
        comments.setAttribute('aria-invalid', String(invalid));
        byId('reviewEvaluation').disabled = comments.value.trim().length < 10;
        byId('draftPreviewMessage').textContent = '';
    }

    document.querySelectorAll('[data-preview]').forEach(button => button.addEventListener('click', () => {
        opener = button;
        menu.open = false;
        const target = button.dataset.preview;
        if (target === 'evaluate' || target === 'draft') {
            comments.value = savedComments;
            updateComments();
            byId('previewDraftStatus').textContent = hasPreviewDraft ? 'Draft preview' : 'Not Started';
            if (target === 'draft' && !hasPreviewDraft) byId('draftPreviewMessage').textContent = 'No preview draft kept yet. You can enter comments below and keep them temporarily.';
        }
        dialog.showModal();
        showView(target === 'evaluate' || target === 'draft' ? 'editor' : target);
    }));

    function requestClose() {
        if ((view === 'editor' || view === 'confirmation') && isDirty()) {
            previousView = view;
            showView('discard');
            return;
        }
        if (view === 'discard') {
            showView(previousView);
            return;
        }
        dialog.close();
    }

    byId('closeEvaluationDialog').addEventListener('click', requestClose);
    dialog.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', requestClose));
    dialog.addEventListener('cancel', event => { event.preventDefault(); requestClose(); });
    dialog.addEventListener('keydown', event => {
        if (event.key !== 'Tab') return;
        const focusable = [...dialog.querySelectorAll('button:not([disabled]), input:not([disabled]), textarea:not([disabled])')]
            .filter(element => element.getClientRects().length);
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });
    dialog.addEventListener('close', () => {
        if (opener) menu.querySelector('summary').focus();
    });
    dialog.addEventListener('click', event => {
        const rect = dialog.getBoundingClientRect();
        if (event.target === dialog && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom)) requestClose();
    });
    comments.addEventListener('input', updateComments);
    byId('savePreviewDraft').addEventListener('click', () => {
        savedComments = comments.value;
        hasPreviewDraft = true;
        byId('previewDraftStatus').textContent = 'Draft preview';
        byId('evaluationDialogTitle').textContent = 'Edit preview draft';
        byId('draftPreviewMessage').textContent = 'Preview draft kept in memory for this page only. Reloading clears it. Nothing was saved to the server.';
    });
    byId('reviewEvaluation').addEventListener('click', () => {
        if (comments.value.trim().length < 10) {
            updateComments();
            comments.focus();
            return;
        }
        byId('confirmationComments').textContent = comments.value;
        showView('confirmation');
    });
    byId('backToEvaluation').addEventListener('click', () => showView('editor'));
    byId('continueEvaluation').addEventListener('click', () => { showView(previousView); comments.focus(); });
    byId('discardEvaluation').addEventListener('click', () => { comments.value = savedComments; dialog.close(); });
    window.addEventListener('beforeunload', event => {
        if (isDirty()) { event.preventDefault(); event.returnValue = ''; }
    });

    function updateEmptyState() {
        const filtered = byId('evaluationSearch').value.trim() || byId('evaluationStatus').value;
        byId('evaluationEmptyTitle').textContent = filtered ? 'Student data is not connected' : 'Ready for your academic review';
        byId('evaluationEmptyText').textContent = filtered
            ? 'Search and filter results will be available after assigned students are connected.'
            : 'Your assigned students will appear here once the account and data connection are configured.';
    }
    byId('evaluationSearch').addEventListener('input', updateEmptyState);
    byId('evaluationSearch').addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
    byId('evaluationStatus').addEventListener('change', updateEmptyState);
    byId('clearEvaluationFilters').addEventListener('click', () => {
        byId('evaluationSearch').value = '';
        byId('evaluationStatus').value = '';
        updateEmptyState();
        byId('evaluationSearch').focus();
    });
    document.addEventListener('click', event => { if (!menu.contains(event.target)) menu.open = false; });
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && menu.open) { menu.open = false; menu.querySelector('summary').focus(); } });
    updateComments();
})();
