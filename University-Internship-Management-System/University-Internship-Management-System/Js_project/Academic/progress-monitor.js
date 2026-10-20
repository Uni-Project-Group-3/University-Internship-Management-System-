(() => {
    'use strict';

    const drawer = document.getElementById('studentDetails');
    const preview = document.getElementById('previewDetails');
    const search = document.getElementById('studentSearch');
    const filters = ['majorFilter', 'companyFilter', 'statusFilter', 'riskFilter'].map(id => document.getElementById(id));

    preview.addEventListener('click', () => drawer.showModal());
    document.getElementById('closeDetails').addEventListener('click', () => drawer.close());
    drawer.addEventListener('click', event => {
        const bounds = drawer.getBoundingClientRect();
        if (event.target === drawer && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) {
            drawer.close();
        }
    });
    drawer.addEventListener('close', () => preview.focus());

    function updateEmptyState() {
        const hasFilters = search.value.trim() || filters.some(filter => filter.value);
        document.getElementById('emptyTitle').textContent = hasFilters ? 'Student data is not connected' : 'Ready for your students';
        document.getElementById('emptyMessage').textContent = hasFilters
            ? 'Search and filter results will be available once assigned student records are connected.'
            : 'Assigned students and their verified progress will appear here once Firebase is connected.';
    }

    search.addEventListener('input', updateEmptyState);
    filters.forEach(filter => filter.addEventListener('change', updateEmptyState));
    document.getElementById('resetFilters').addEventListener('click', () => {
        search.value = '';
        filters.forEach(filter => { filter.value = ''; });
        updateEmptyState();
        search.focus();
    });
})();
