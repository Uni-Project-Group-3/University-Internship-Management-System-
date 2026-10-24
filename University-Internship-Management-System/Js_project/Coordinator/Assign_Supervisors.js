(function () {
    'use strict';
    const by = id => document.getElementById(id), dialog = by('assignmentDialog');
    let data = null, group = null, selected = new Set(), busy = false, retry = null;
    const node = (tag, text, className) => {
        const e = document.createElement(tag);
        if (text != null) e.textContent = String(text);
        if (className) e.className = className;
        return e;
    };
    const majorName = id => data.departments[id] || id;
    const professor = () => data.professors.find(p => p.id === by('assignmentProfessor').value);
    const professorName = id => data.professors.find(p => p.id === id)?.name || id || 'Unassigned';
    const notice = text => { by('assignmentNotice').textContent = text; };
    const memberIds = g => Object.keys(g?.students || {}).filter(id => g.students[id] === true);
    function options(control, rows, placeholder) {
        control.replaceChildren(new Option(placeholder, ''));
        rows.forEach(r => control.append(new Option(r.name || r.id, r.id)));
    }
    async function api(input) {
        const response = await fetch('../AcademicAssignmentsApi.ashx', {
            method: input ? 'POST' : 'GET', credentials: 'same-origin', cache: 'no-store',
            signal: AbortSignal.timeout(90000), headers: input ? { 'Content-Type': 'application/json' } : {},
            body: input ? JSON.stringify(input) : undefined
        });
        let result;
        try { result = await response.json(); } catch (_) { throw Error('The server response could not be verified. Refresh or retry the same request.'); }
        if (!response.ok) throw Error(result.message || 'The assignment request failed.');
        return result;
    }
    function render() {
        const host = by('assignmentRows');
        host.replaceChildren();
        for (const g of data.groups.filter(g => !by('departmentFilter').value || g.department === by('departmentFilter').value)) {
            const row = node('tr'), title = node('td');
            title.append(node('strong', g.name, 'group-name'), node('small', g.id, 'group-id'));
            row.append(title, node('td', majorName(g.department)), node('td', Object.values(g.students || {}).filter(v => v === true).length), node('td', professorName(g.supervisorId)));
            const action = node('td'), button = node('button', 'View / add students', 'assignment-secondary');
            button.type = 'button'; button.onclick = () => open(g);
            action.append(button); row.append(action); host.append(row);
        }
        if (!host.children.length) {
            const row = node('tr'), cell = node('td', 'No groups yet in this view. Add a group to assign students.');
            cell.colSpan = 5; row.append(cell); host.append(row);
        }
    }
    async function load() {
        const fresh = await api();
        data = fresh;
        const filter = by('departmentFilter').value;
        options(by('departmentFilter'), Object.entries(data.departments).map(([id, name]) => ({ id, name })), 'All majors');
        if (data.departments[filter]) by('departmentFilter').value = filter;
        render(); by('newAssignment').disabled = false;
    }
    function open(g) {
        group = g; selected = new Set(); retry = null;
        by('assignmentError').textContent = ''; by('assignmentName').value = g?.name || '';
        by('assignmentName').readOnly = !!g; by('studentSearch').value = '';
        by('assignmentTitle').textContent = g ? (g.name || 'Group students') : 'Add student group';
        by('assignmentSave').textContent = g ? 'Add selected students' : 'Create group';
        by('assignmentIntro').textContent = g ? 'Review the students in this group and add more from the same major.' : 'Each supervisor is responsible for one major. Select students from that major to join the group.';
        by('assignmentSetup').hidden = !!g; by('assignmentSummary').hidden = !g; by('assignmentCurrent').hidden = !g;
        by('assignmentStudentsTitle').textContent = g ? 'Add students' : '03 \u00a0 Students';
        by('assignmentStudentsHint').textContent = g ? 'Only students from this major who are not in another group or assigned to another supervisor are listed.' : 'Students already in another group or assigned to another supervisor cannot be selected.';
        if (g) {
            const members = memberIds(g);
            by('summaryProfessor').textContent = professorName(g.supervisorId);
            by('summaryMajor').textContent = majorName(g.department);
            by('summaryCount').textContent = members.length;
            by('assignmentCurrentTitle').textContent = 'Current students (' + members.length + ')';
            const list = by('assignmentCurrentList');
            list.replaceChildren();
            for (const id of members) {
                const s = data.students.find(x => x.id === id), row = node('div', null, 'assignment-student assignment-member'), info = node('span');
                info.append(node('strong', s?.name || id), node('small', 'Student ID: ' + id));
                row.append(info); list.append(row);
            }
            if (!members.length) list.append(node('p', 'No students in this group yet.', 'assignment-empty'));
        }
        options(by('assignmentProfessor'), data.professors.filter(p => !p.conflict || p.id === g?.supervisorId).map(p => ({ id: p.id, name: p.name + (p.conflict ? ' - review assignments required' : '') })), 'Select academic supervisor');
        by('assignmentProfessor').value = g?.supervisorId || '';
        by('assignmentProfessor').disabled = !!g;
        majors(); dialog.showModal(); dialog.scrollTop = 0;
    }
    function majors() {
        selected.clear(); by('assignmentError').textContent = '';
        const p = professor();
        const available = p && !p.conflict ? Object.entries(data.departments).filter(([id]) => !p.department || p.department === id).map(([id, name]) => ({ id, name })) : [];
        options(by('assignmentDepartment'), available, 'Select university major');
        by('assignmentDepartment').disabled = !p || p.conflict || !!group;
        // Even an existing responsibility is chosen explicitly after the supervisor.
        by('assignmentDepartment').value = group?.department || '';
        by('assignmentMajorHint').textContent = !p ? 'Choose the supervisor first.' : p.conflict ? 'This supervisor has conflicting majors. Review existing assignments before adding students.' : p.department ? 'This supervisor is responsible for ' + majorName(p.department) + '.' : 'The selected major will become this supervisor\'s responsibility.';
        students();
    }
    function blocked(s) {
        const p = professor();
        if (!p || p.conflict || s.conflict) return true;
        if (['universityId', 'collegeId'].some(f => s[f] && p[f] && s[f] !== p[f])) return true;
        return s.groupIds.some(id => id !== group?.id) || s.supervisorIds.some(id => id !== p.id) || s.groupIds.includes(group?.id);
    }
    function students() {
        const host = by('assignmentStudents'), major = by('assignmentDepartment').value, query = by('studentSearch').value.trim().toLowerCase();
        const enabled = !!major && !!professor() && !professor().conflict;
        by('studentSearch').disabled = !enabled; by('studentSelectAll').disabled = !enabled;
        host.replaceChildren();
        if (enabled) {
            // An existing group lists only the students that can still join it.
            for (const s of data.students.filter(s => s.department === major && (!group || !blocked(s)) && [s.name, s.id].join(' ').toLowerCase().includes(query))) {
                const label = node('label', null, 'assignment-student'), check = document.createElement('input');
                check.type = 'checkbox'; check.disabled = blocked(s); check.checked = selected.has(s.id);
                check.onchange = () => { by('assignmentError').textContent = ''; if (check.checked) selected.add(s.id); else selected.delete(s.id); students(); };
                const info = node('span');
                info.append(node('strong', s.name || s.id), node('small', 'Student ID: ' + s.id));
                const reason = s.groupIds.includes(group?.id) ? 'Already in this group' : blocked(s) ? 'Unavailable - existing assignment or account mismatch' : 'Available';
                info.append(node('small', reason, blocked(s) ? 'assignment-unavailable' : 'assignment-available'));
                label.append(check, info); host.append(label);
            }
        }
        if (!host.children.length) host.append(node('p', !enabled ? 'Choose a supervisor and major to see students.' : group && !query ? 'No more available students in this major.' : 'No students match this major and search.', 'assignment-empty'));
        by('studentCount').textContent = selected.size + ' selected';
        by('assignmentSave').disabled = busy || (!!group && !selected.size);
    }
    async function close() {
        if (busy) return;
        dialog.close();
    }
    by('assignmentSave').onclick = async () => {
        if (busy) return;
        const p = professor(), major = by('assignmentDepartment').value, error = by('assignmentError');
        error.textContent = '';
        if (!p || p.conflict) { error.textContent = 'Choose an eligible academic supervisor first.'; by('assignmentProfessor').focus(); return; }
        if (!major) { error.textContent = 'Select the university major.'; by('assignmentDepartment').focus(); return; }
        if (!selected.size) { error.textContent = 'Select at least one available student.'; return; }
        const payload = { groupId: group?.id || '', name: by('assignmentName').value.trim() || (majorName(major) + ' - ' + p.name).slice(0, 120), department: major, supervisorId: p.id, studentIds: [...selected].sort() };
        const signature = JSON.stringify(payload);
        if (!retry || retry.signature !== signature) retry = { signature, id: crypto.randomUUID() };
        payload.requestId = retry.id;
        busy = true; by('assignmentSave').disabled = true; by('assignmentFields').inert = true;
        try {
            const saved = await api(payload);
            if (!saved.saved) throw Error('The server did not confirm this assignment. Retry the same selections.');
            dialog.close();
            notice('Group saved. Refreshing assignments...');
            try { await load(); notice('Group saved. The selected students are assigned to their academic supervisor.'); }
            catch (e) { by('newAssignment').disabled = true; notice('The group was saved, but the table could not refresh. Use Refresh before adding another group. ' + e.message); }
        } catch (e) { error.textContent = e.message; }
        finally { busy = false; by('assignmentSave').disabled = false; by('assignmentFields').inert = false; }
    };
    by('assignmentProfessor').onchange = majors;
    by('assignmentDepartment').onchange = () => { selected.clear(); by('assignmentError').textContent = ''; students(); };
    by('studentSearch').oninput = students;
    by('studentSelectAll').onclick = () => {
        // Select all matching students, including those outside the current search.
        by('assignmentError').textContent = '';
        data.students.filter(s => s.department === by('assignmentDepartment').value && !blocked(s)).forEach(s => selected.add(s.id)); students();
    };
    by('studentClear').onclick = () => { selected.clear(); students(); };
    by('assignmentClose').onclick = close; by('assignmentCancel').onclick = close;
    dialog.oncancel = e => { e.preventDefault(); close(); };
    by('newAssignment').onclick = () => open(null); by('departmentFilter').onchange = render;
    by('refreshAssignments').onclick = async () => {
        if (busy) return;
        by('refreshAssignments').disabled = true;
        try { await load(); notice('Groups refreshed.'); } catch (e) { notice(e.message); }
        finally { by('refreshAssignments').disabled = false; }
    };
    load().then(() => notice('')).catch(e => notice('Groups could not be loaded. ' + e.message));
}());
