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
    const sameName = (a, b) => String(a || '').replace(/\s+/g, ' ').trim().toLowerCase() === String(b || '').replace(/\s+/g, ' ').trim().toLowerCase();
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
            const action = node('td'), tools = node('div', null, 'group-actions'), button = node('button', 'View / edit', 'assignment-secondary'), remove = node('button', 'Delete', 'assignment-danger');
            button.type = 'button'; button.onclick = () => open(g);
            remove.type = 'button'; remove.setAttribute('aria-label', 'Delete group ' + g.name); remove.onclick = () => deleteGroup(g);
            tools.append(button, remove); action.append(tools); row.append(action); host.append(row);
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
        by('assignmentRenameRow').hidden = !g; by('assignmentDelete').hidden = !g; by('groupRename').value = g?.name || '';
        by('assignmentSetup').hidden = !!g; by('assignmentSummary').hidden = !g; by('assignmentCurrent').hidden = !g;
        by('assignmentStudentsTitle').textContent = g ? 'Add students' : '03 \u00a0 Students';
        by('assignmentStudentsHint').textContent = g ? 'Only students from this major who are not in another group or assigned to another supervisor are listed.' : 'Students already in another group or assigned to another supervisor cannot be selected.';
        if (g) {
            const members = memberIds(g);
            const owner = data.professors.find(x => x.id === g.supervisorId);
            by('summaryProfessor').textContent = professorName(g.supervisorId) + (owner && !owner.official ? ' (not in university records)' : '');
            by('summaryMajor').textContent = majorName(g.department);
            by('summaryCount').textContent = members.length;
            by('assignmentCurrentTitle').textContent = 'Current students (' + members.length + ')';
            const list = by('assignmentCurrentList');
            list.replaceChildren();
            for (const id of members) {
                const s = data.students.find(x => x.id === id), row = node('div', null, 'assignment-student assignment-member'), info = node('span');
                info.append(node('strong', s?.name || id), node('small', 'Student ID: ' + id));
                const remove = node('button', 'Remove', 'assignment-remove');
                remove.type = 'button'; remove.setAttribute('aria-label', 'Remove ' + (s?.name || id) + ' from this group');
                remove.onclick = () => removeStudent(id, s?.name || id);
                row.append(info, remove); list.append(row);
            }
            if (!members.length) list.append(node('p', 'No students in this group yet.', 'assignment-empty'));
        }
        options(by('assignmentProfessor'), data.professors.filter(p => !p.conflict || p.id === g?.supervisorId).map(p => ({ id: p.id, name: p.name + (p.conflict ? ' - review assignments required' : !p.official ? ' - not in university records' : '') })), 'Select academic supervisor');
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
        by('assignmentMajorHint').textContent = !p ? 'Choose the supervisor first.' : !p.official ? 'This account is not in university records, so it cannot receive students. Delete this group, or delete the account from Manage Academic Supervisors.' : p.conflict ? 'This supervisor has conflicting majors. Review existing assignments before adding students.' : p.department ? 'This supervisor is responsible for ' + majorName(p.department) + '.' : 'The selected major will become this supervisor\'s responsibility.';
        students();
    }
    function blocked(s) {
        const p = professor();
        if (!p || p.conflict || !p.official || s.conflict) return true;
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
        if (!host.children.length) host.append(node('p', !enabled ? 'Choose a supervisor and major to see students.' : professor() && !professor().official ? 'This supervisor is not in university records, so no students can be added. Delete this group or the account instead.' : group && !query ? 'No more available students in this major.' : 'No students match this major and search.', 'assignment-empty'));
        by('studentCount').textContent = selected.size + ' selected';
        by('assignmentSave').disabled = busy || (!!group && !selected.size);
    }
    async function removeStudent(id, name) {
        if (busy || !group || !window.confirm('Remove ' + name + ' from ' + (group.name || 'this group') + '? The student will no longer have this academic supervisor.')) return;
        busy = true; by('assignmentFields').inert = true; by('assignmentError').textContent = '';
        try {
            const result = await api({ action: 'remove', groupId: group.id, studentId: id });
            if (!result.removed) throw Error('The server did not confirm the removal. Refresh and check the group.');
            const groupId = group.id;
            await load();
            notice(name + ' was removed from the group.');
            busy = false; by('assignmentFields').inert = false;
            const fresh = data.groups.find(g => g.id === groupId);
            if (fresh) open(fresh); else dialog.close();
        } catch (e) { by('assignmentError').textContent = e.message; }
        finally { busy = false; by('assignmentFields').inert = false; }
    }
    // Rename and delete write straight to the database, then the table reloads from it.
    async function manage(payload, confirmText) {
        if (busy || (confirmText && !window.confirm(confirmText))) return null;
        busy = true; by('assignmentFields').inert = true; by('assignmentError').textContent = '';
        try {
            const result = await api(payload);
            if (!result.saved) throw Error('The server did not confirm this change. Refresh and check the group.');
            await load();
            notice(result.message || 'Saved.');
            return result;
        } catch (e) {
            if (dialog.open) by('assignmentError').textContent = e.message; else notice(e.message);
            return null;
        } finally { busy = false; by('assignmentFields').inert = false; }
    }
    async function deleteGroup(g) {
        const count = memberIds(g).length;
        const text = 'Delete the group "' + g.name + '" (' + g.id + ')?' + (count ? ' Its ' + count + ' student(s) will be unlinked from ' + professorName(g.supervisorId) + '.' : '') + ' This cannot be undone.';
        if (await manage({ action: 'deleteGroup', groupId: g.id }, text) && dialog.open) dialog.close();
    }
    by('assignmentRename').onclick = async () => {
        const name = by('groupRename').value.replace(/\s+/g, ' ').trim();
        if (!name) { by('assignmentError').textContent = 'Enter a group name.'; return; }
        if (sameName(name, group.name)) { by('assignmentError').textContent = 'The name is unchanged.'; return; }
        const groupId = group.id;
        if (await manage({ action: 'rename', groupId, name })) { const fresh = data.groups.find(g => g.id === groupId); if (fresh) open(fresh); }
    };
    by('assignmentDelete').onclick = () => deleteGroup(group);
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
        const name = group ? group.name : by('assignmentName').value.replace(/\s+/g, ' ').trim() || (majorName(major) + ' - ' + p.name).slice(0, 120);
        if (!group && data.groups.some(g => g.supervisorId === p.id && g.department === major && sameName(g.name, name))) { error.textContent = p.name + ' already has a group named "' + name + '" in this major. Open that group and add the students there.'; return; }
        const payload = { groupId: group?.id || '', name, department: major, supervisorId: p.id, studentIds: [...selected].sort() };
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
