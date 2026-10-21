(() => {
    'use strict';

    const editor = document.getElementById('companyEditor');
    const actions = document.getElementById('accountPreview');
    const confirmation = document.getElementById('accountConfirmation');
    const fields = [...document.querySelectorAll('[data-company-field]')];
    const validation = document.getElementById('companyValidation');
    const search = document.getElementById('companySearch');
    const filters = ['companyStatus', 'accountStatus', 'companySort'].map(id => document.getElementById(id));
    const returnFocus = new WeakMap();
    const actionCopy = {
        details: ['Supervisor details', 'The selected supervisor\'s contact details, account status, password-change requirement, and company relationship will be displayed here.', 'View details'],
        reset: ['Reset supervisor password?', 'A trusted server operation will issue a new temporary password, revoke existing sessions, and require a password change before dashboard access. This does not send an ordinary Forgot Password email.', 'Reset password'],
        disable: ['Disable supervisor account?', 'This supervisor will lose sign-in and protected access. The company, other supervisors, and academic records will remain unchanged.', 'Disable account'],
        enable: ['Enable supervisor account?', 'The existing account will be reactivated with the same identity and company link. Any pending password-change requirement will remain in effect.', 'Enable account'],
        delete: ['Delete supervisor account?', 'Sign-in access will be removed for this supervisor. Required historical references will be retained. This action will not delete the company or internship history.', 'Delete account']
    };

    function openDialog(dialog, opener) {
        returnFocus.set(dialog, opener);
        dialog.showModal();
    }

    function openEditor(edit, opener) {
        document.getElementById('editorTitle').textContent = edit ? 'Edit company & supervisor' : 'Add company & supervisor';
        document.getElementById('saveCompany').textContent = edit ? 'Save changes' : 'Create company';
        document.getElementById('editorNotice').textContent = edit
            ? 'Layout preview: no record selected, so fields are not prefilled. Saving is unavailable.'
            : 'Design preview only. This form does not create or update records.';
        fields.forEach(field => { field.value = ''; field.removeAttribute('aria-invalid'); });
        validation.textContent = '';
        validation.removeAttribute('data-valid');
        openDialog(editor, opener);
    }

    document.querySelectorAll('[data-open]').forEach(button => button.addEventListener('click', () => {
        if (button.dataset.open === 'addCompany') openEditor(false, button);
        else openDialog(actions, button);
    }));

    document.querySelectorAll('.companies-modal').forEach(dialog => {
        dialog.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
        dialog.addEventListener('close', () => returnFocus.get(dialog)?.focus());
        dialog.addEventListener('click', event => {
            const bounds = dialog.getBoundingClientRect();
            if (event.target === dialog && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) dialog.close();
        });
    });

    document.querySelectorAll('[data-account-action]').forEach(button => button.addEventListener('click', () => {
        if (button.dataset.accountAction === 'edit') {
            openEditor(true, button);
            return;
        }
        const [title, description, label] = actionCopy[button.dataset.accountAction];
        document.getElementById('confirmationTitle').textContent = title;
        document.getElementById('confirmationDescription').textContent = description;
        document.getElementById('confirmAccountAction').textContent = label;
        openDialog(confirmation, button);
    }));
    document.getElementById('backToActions').addEventListener('click', () => confirmation.close());

    document.getElementById('validateCompany').addEventListener('click', () => {
        const invalid = fields.filter(field => !field.checkValidity() || (field.required && !field.value.trim()));
        fields.forEach(field => field.setAttribute('aria-invalid', invalid.includes(field) ? 'true' : 'false'));
        validation.dataset.valid = String(invalid.length === 0);
        validation.textContent = invalid.length
            ? 'Check the highlighted fields. Company name, supervisor name, and a valid supervisor email are required. Use a full website URL when provided.'
            : 'Form validation complete. Nothing was saved; Firebase integration is not connected.';
        invalid[0]?.focus();
    });
    fields.forEach(field => {
        field.addEventListener('input', () => {
            field.removeAttribute('aria-invalid');
            validation.textContent = '';
        });
        field.addEventListener('keydown', event => { if (event.key === 'Enter' && field.tagName !== 'TEXTAREA') event.preventDefault(); });
    });

    function updateEmptyState() {
        const filtered = search.value.trim() || filters.slice(0, 2).some(filter => filter.value);
        document.getElementById('companyEmptyTitle').textContent = filtered ? 'Directory data is not connected' : 'Your next partnership starts here';
        document.getElementById('companyEmptyMessage').textContent = filtered
            ? 'Search and filter results will be available after company records are connected.'
            : 'Company profiles and their linked supervisors will appear here once the database is connected.';
    }
    search.addEventListener('input', updateEmptyState);
    search.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
    filters.forEach(filter => filter.addEventListener('change', updateEmptyState));
    document.getElementById('resetCompanyFilters').addEventListener('click', () => {
        search.value = '';
        filters.forEach(filter => { filter.selectedIndex = 0; });
        updateEmptyState();
        search.focus();
    });
})();
