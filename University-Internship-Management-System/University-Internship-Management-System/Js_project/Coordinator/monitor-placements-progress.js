(() => {
    'use strict';

    const byId = id => document.getElementById(id);
    const drawer = byId('placementDetails');
    const controls = ['placementSearch', 'departmentFilter', 'placementCompanyFilter', 'placementStatusFilter', 'placementSort', 'placementPageSize', 'clearPlacements'];
    let students = [];
    let page = 1;
    let detailOpener;
    const escape = value => String(value ?? '').replace(/[&<>"']/g, character => ({'&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;'}[character]));
    const badge = (text, tone = '') => `<span class="placements-badge ${tone}">${escape(text)}</span>`;
    const meter = (value, label) => `<div class="placements-meter" role="progressbar" aria-label="${escape(label)}" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${value}"><span style="width:${value}%"></span></div>`;
    const statusTone = status => status === 'Completed' ? 'is-success' : status === 'Active' ? 'is-info' : 'is-warning';

    function filteredStudents() {
        const search = byId('placementSearch').value.trim().toLowerCase();
        const filtered = students.filter(student =>
            (!search || [student.name, student.universityId, student.company, student.companySupervisor, student.academicSupervisor].some(value => value.toLowerCase().includes(search))) &&
            (!byId('departmentFilter').value || student.department === byId('departmentFilter').value) &&
            (!byId('placementCompanyFilter').value || student.company === byId('placementCompanyFilter').value) &&
            (!byId('placementStatusFilter').value || student.placement.status === byId('placementStatusFilter').value));
        const sort = byId('placementSort').value;
        return filtered.sort((left, right) => {
            if (sort === 'hours') return right.placement.approvedHours - left.placement.approvedHours || left.name.localeCompare(right.name);
            if (sort === 'tasks') return right.taskPercent - left.taskPercent || left.name.localeCompare(right.name);
            if (sort === 'risk') return Number(Boolean(right.risks.length)) - Number(Boolean(left.risks.length)) || left.name.localeCompare(right.name);
            return sort === 'name-desc' ? right.name.localeCompare(left.name) : left.name.localeCompare(right.name);
        });
    }

    function renderStudents() {
        const filtered = filteredStudents();
        const pageSize = Number(byId('placementPageSize').value);
        const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
        page = Math.min(page, pages);
        const start = (page - 1) * pageSize;
        byId('placementRows').innerHTML = filtered.slice(start, start + pageSize).map(student => `<tr>
            <td data-label="Student / ID"><div class="placements-student"><span class="placements-avatar" aria-hidden="true">${escape(student.name.split(' ').map(name => name[0]).slice(0, 2).join(''))}</span><div><strong>${escape(student.name)}</strong><small>${escape(student.universityId)}</small></div></div></td>
            <td data-label="Department">${escape(student.department)}</td><td data-label="Company">${escape(student.company)}</td>
            <td data-label="Company supervisor">${escape(student.companySupervisor)}</td><td data-label="Academic supervisor">${escape(student.academicSupervisor)}</td>
            <td data-label="Training status">${badge(student.placement.status, statusTone(student.placement.status))}</td>
            <td data-label="Approved / Required"><div><strong>${student.placement.approvedHours} / ${student.placement.requiredHours} h</strong>${meter(student.hoursPercent, `Approved hours for ${student.name}`)}<small>${student.hoursPercent}% hours</small></div></td>
            <td data-label="Task completion"><div><strong>${student.completed} / ${student.tasks.length} tasks</strong>${meter(student.taskPercent, `Task completion for ${student.name}`)}<small>${student.taskPercent}% tasks</small></div></td>
            <td data-label="Progress indicators">${badge(student.risks.length ? 'Needs attention' : 'No flags', student.risks.length ? 'is-warning' : 'is-neutral')}<small>${escape(student.risks.join(' · ') || 'Hours and tasks tracked separately')}</small></td>
            <td data-label="Actions"><button type="button" class="placements-detail-button" data-student="${escape(student.id)}" aria-label="View details for ${escape(student.name)}">View details <span aria-hidden="true">&#8599;</span></button></td></tr>`).join('');
        byId('recordCount').textContent = `${filtered.length} students`;
        byId('placementMessage').hidden = filtered.length > 0;
        byId('placementMessage').textContent = 'No placements match your filters. Try a different search or clear the filters.';
        byId('placementResults').textContent = filtered.length ? `Showing ${start + 1}-${Math.min(start + pageSize, filtered.length)} of ${filtered.length} students` : '0 students found';
        byId('placementPageLabel').textContent = `${page} / ${pages}`;
        byId('previousPlacements').disabled = page === 1;
        byId('nextPlacements').disabled = page === pages;
    }

    function evaluationMarkup(title, evaluation) {
        return `<article class="placements-evaluation"><h4>${title}</h4>${evaluation ? `<strong>${escape(evaluation.result)}</strong><small>${escape(evaluation.scale)}</small><p>${escape(evaluation.feedback)}</p>` : '<p>No evaluation submitted yet.</p>'}</article>`;
    }

    function showDetails(student, opener) {
        const placement = student.placement;
        byId('placementDetailsTitle').textContent = student.name;
        byId('detailStudentId').textContent = `University ID ${student.universityId}`;
        byId('placementDetailsBody').innerHTML = `<section class="placements-detail-section"><h3>Student information</h3><dl class="placements-info">${[['Department',student.department],['Company',student.company],['Company supervisor',student.companySupervisor],['Academic supervisor',student.academicSupervisor],['Internship status',placement.status]].map(([label,value]) => `<div><dt>${label}</dt><dd>${escape(value)}</dd></div>`).join('')}</dl></section>
            <section class="placements-detail-section"><h3>Training hours</h3><div class="placements-hour-cards">${[['Required',placement.requiredHours],['Approved',placement.approvedHours],['Remaining',Math.max(0,placement.requiredHours-placement.approvedHours)]].map(([label,value]) => `<div><span>${label}</span><strong>${value}<small> h</small></strong></div>`).join('')}</div><div class="placements-progress-label"><span>Approved hours</span><strong>${student.hoursPercent}%</strong></div>${meter(student.hoursPercent, 'Approved training hours')}<p class="placements-detail-note">Only approved hours count. Completed hours do not imply final internship approval.</p></section>
            <section class="placements-detail-section"><h3>Tasks &amp; submissions</h3><div class="placements-progress-label"><span>${student.completed} of ${student.tasks.length} tasks completed</span><strong>${student.taskPercent}%</strong></div>${meter(student.taskPercent, 'Task completion')}<div class="placements-tasks">${student.tasks.map(task => { const overdue = task.status !== 'Completed' && task.deadline < student.snapshot; return `<article><div><h4>${escape(task.title)}</h4><small>Deadline: ${escape(task.deadline)} · ${task.submittedOn ? `Submitted: ${escape(task.submittedOn)}` : 'Not submitted'}</small></div><div>${badge(task.status,task.status === 'Completed' ? 'is-success' : 'is-info')}${overdue ? badge('Overdue','is-danger') : ''}</div></article>`; }).join('')}</div></section>
            <section class="placements-detail-section"><h3>Evaluations &amp; feedback</h3>${evaluationMarkup('Company evaluation',student.evaluations.company)}${evaluationMarkup('Academic evaluation',student.evaluations.academic)}</section>
            <section class="placements-detail-section"><h3>Progress &amp; follow-up</h3>${badge(placement.status,statusTone(placement.status))}<ul class="placements-risk-list">${student.risks.length ? student.risks.map(risk => `<li>${escape(risk)}</li>`).join('') : '<li>No risk flags recorded.</li>'}</ul><p class="placements-detail-note">Last updated: ${escape(student.snapshot)}. Monitoring is read only. Final approval is handled separately in Final Completion Review.</p></section>`;
        detailOpener = opener;
        drawer.showModal();
        drawer.scrollTop = 0;
    }

    controls.forEach(id => byId(id).addEventListener(id === 'placementSearch' ? 'input' : 'change', () => { page = 1; renderStudents(); }));
    byId('placementSearch').addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
    byId('clearPlacements').addEventListener('click', () => {
        byId('placementSearch').value = '';
        ['departmentFilter','placementCompanyFilter','placementStatusFilter','placementSort'].forEach(id => { byId(id).selectedIndex = 0; });
        page = 1;
        renderStudents();
    });
    byId('previousPlacements').addEventListener('click', () => { page--; renderStudents(); });
    byId('nextPlacements').addEventListener('click', () => { page++; renderStudents(); });
    byId('placementRows').addEventListener('click', event => {
        const button = event.target.closest('[data-student]');
        if (button) showDetails(students.find(student => student.id === button.dataset.student), button);
    });
    byId('closePlacementDetails').addEventListener('click', () => drawer.close());
    drawer.addEventListener('close', () => detailOpener?.focus());
    drawer.addEventListener('click', event => {
        const rect = drawer.getBoundingClientRect();
        if (event.target === drawer && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom)) drawer.close();
    });

    renderStudents();
    byId('recordCount').textContent = 'Data not connected';
    byId('placementMessage').textContent = 'Student records will appear here after Firebase is connected.';
    byId('placementResults').textContent = 'Student data is not connected';
    byId('placementPageLabel').textContent = '—';
    controls.forEach(id => { byId(id).disabled = true; });
})();
