/* Server-rendered Manage Students. No demo identity, records, credentials or mutations. */
(function () {
    'use strict';
    function setup() {
        const by = id => document.getElementById(id);
        let state = {};
        try { state = JSON.parse(by('studentPageState')?.value || '{}'); } catch (_) { }
        const editor = by('studentEditor');
        const details = by('studentDetailsDialog');
        const opener = new WeakMap();
        function open(dialog) {
            if (!dialog || dialog.open) return;
            opener.set(dialog, document.activeElement);
            dialog.showModal();
        }
        document.querySelectorAll('dialog').forEach(dialog => {
            dialog.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
            dialog.addEventListener('close', () => opener.get(dialog)?.focus());
            dialog.addEventListener('click', event => {
                if (event.target !== dialog) return;
                const rect = dialog.getBoundingClientRect();
                if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
            });
        });
        const deletion = by('studentDeleteDialog');
        document.querySelectorAll('[data-delete-student]').forEach(button => {
            button.addEventListener('click', () => {
                by('studentDeleteUid').value = button.dataset.deleteStudent;
                by('studentDeleteName').textContent = button.dataset.studentName;
                open(deletion);
            });
        });
        deletion?.addEventListener('close', () => { by('studentDeleteUid').value = ''; });
        const add = by('addStudentButton');
        if (add) {
            add.disabled = !state.canManage;
            add.addEventListener('click', () => open(editor));
        }
        if (state.openEditor && state.canManage) open(editor);
        if (state.openDetails && state.canManage) open(details);

        const department = by('studentDepartmentFilter');
        const supervisor = by('studentSupervisorFilter');
        const completion = by('completionStatusFilter');
        const search = by('completionSearch');
        const rows = [...document.querySelectorAll('#studentsGrid tr[data-student-row]')];
        function filter() {
            const query = search?.value.trim().toLowerCase() || '';
            let count = 0;
            rows.forEach(row => {
                const visible = (!query || (row.dataset.search || '').includes(query)) &&
                    (!department?.value || row.dataset.department === department.value) &&
                    (!supervisor?.value || row.dataset.supervisor === supervisor.value) &&
                    (!completion?.value || row.dataset.completion === completion.value);
                row.hidden = !visible;
                if (visible) count++;
            });
            if (by('studentFilterSummary')) by('studentFilterSummary').textContent = state.canManage ? `${count} of ${rows.length} students shown` : '';
            if (by('studentNoMatches')) by('studentNoMatches').hidden = rows.length === 0 || count > 0;
        }
        [department, supervisor, completion].forEach(control => control?.addEventListener('change', filter));
        search?.addEventListener('input', filter);
        search?.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); filter(); } });
        const topSearch = by('dashboardSearch');
        topSearch?.addEventListener('input', () => { if (search) search.value = topSearch.value; filter(); });
        topSearch?.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
        by('completionClearFilters')?.addEventListener('click', () => {
            [department, supervisor, completion, search, topSearch].forEach(control => { if (control) control.value = ''; });
            filter();
        });
        filter();

        by('studentUniversityId')?.addEventListener('input', () => {
            ['studentName', 'studentDepartment', 'studentEmail', 'studentTemporaryPassword'].forEach(id => { if (by(id)) by(id).value = ''; });
            if (by('studentLookupMessage')) by('studentLookupMessage').textContent = '';
            if (by('studentPasswordStatus')) by('studentPasswordStatus').textContent = '';
            ['generateStudentPassword', 'saveStudentButton'].forEach(id => { if (by(id)) by(id).disabled = true; });
        });
        by('studentUniversityId')?.addEventListener('keydown', event => {
            if (event.key === 'Enter') { event.preventDefault(); by('findStudentButton')?.click(); }
        });
        by('copyStudentPassword')?.addEventListener('click', async () => {
            const field = by('studentTemporaryPassword');
            if (!field?.value) return;
            try {
                if (!navigator.clipboard?.writeText) throw new Error();
                await navigator.clipboard.writeText(field.value);
                by('studentPasswordStatus').textContent = 'Copied.';
            } catch (_) {
                field.focus(); field.select();
                by('studentPasswordStatus').textContent = 'Press Ctrl+C to copy.';
            }
        });
        // Buttons keep their ASP.NET postback handlers. Server-side leases handle duplicate requests.
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', setup, {once:true});
    else setup();
})();
