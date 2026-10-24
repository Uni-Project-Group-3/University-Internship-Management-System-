/* Original-page demo bindings. Layout and server controls remain in the ASPX. */
(function() {
    'use strict';
    // Demo-only behavior is opt-in. A missing demo session must not hijack normal page navigation.
    if (!window.UimsDemo?.enabled()) return;
    let binding;
    function initialize() {
        binding?.abort();
        binding = new AbortController();
        const signal = binding.signal, demo = window.UimsDemo;
        let view = demo.view(), selected = null, render = () => { };
        if (!view || view.user.mustChangePassword || view.user.role !== document.body.dataset.demoRole) {
            location.href = '../Auth/Login.aspx?demo=1';
            return;
        }
        const by = id => document.getElementById(id) || document.querySelector('[id$="_' + id + '"]');
        const all = selector => [...document.querySelectorAll(selector)];
        const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
        const text = (id, value) => { const element = by(id); if (element) element.textContent = value ?? '—'; };
        const value = id => by(id)?.value?.trim() || '';
        const set = (id, value) => { if (by(id)) by(id).value = value ?? ''; };
        const options = (id, records, placeholder = 'All') => {
            const element = by(id); if (!element) return;
            const previous = element.value;
            element.innerHTML = '<option value="">' + escape(placeholder) + '</option>' + records.map(record => '<option value="' + escape(record.id) + '">' + escape(record.name) + '</option>').join('');
            if (records.some(record => record.id === previous)) element.value = previous;
        };
        const on = (id, type, handler) => by(id)?.addEventListener(type, handler, { signal });
        const click = (id, handler) => on(id, 'click', event => { event.preventDefault(); try { handler(event); } catch (exception) { error(exception, event.currentTarget.closest('dialog,.modal,.offcanvas')); } });
        const button = (label, action, id, className = 'row-action') => {
            const paths = { edit: 'M4 16v4h4L20 8l-4-4L4 16Z', delete: 'M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13', reset: 'M13 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10ZM8 12H2m2 0v4', status: 'M12 2v10M6 5a9 9 0 1 0 12 0', assign: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8M20 8v6m-3-3h6' };
            const icon = className.includes('action-btn') && paths[action];
            const content = icon ? '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="' + paths[action] + '"/></svg>' : escape(label);
            return '<button type="button" title="' + escape(label) + '" aria-label="' + escape(label) + '" class="' + className + '" data-uims="' + action + '" data-record="' + escape(id) + '">' + content + '</button>';
        };
        const cells = values => '<tr>' + values.map(cell => '<td>' + cell + '</td>').join('') + '</tr>';
        const fill = (selector, records, row) => {
            const element = document.querySelector(selector);
            if (!element) return;
            const emptyState = element.id === 'taskList' ? element.querySelector('#noTasksMsg') : null;
            element.innerHTML = records.map(row).join('');
            if (emptyState) element.append(emptyState);
        };
        const messageElement = () => by('uimsPageNotice') || document.querySelector('main .preview-note,main .completion-notice,main .companies-notice,main .evaluation-notice,main .monitor-notice,main .workspace-note,#messagesAvailability,#traineesAvailability,#accountAvailability') || by('pageNotice') || by('profileMessage') || by('pnlMessage');
        function notice(message) {
            let element = messageElement();
            if (!element) { element = document.createElement('p'); element.id = 'uimsPageNotice'; element.className = 'uims-feedback'; element.setAttribute('role', 'status'); (document.querySelector('main') || document.querySelector('.content')).prepend(element); }
            element.hidden = false; element.textContent = message;
        }
        function error(error, scope) {
            let element = scope?.querySelector('[data-uims-error]');
            if (scope && !element) { element = document.createElement('p'); element.dataset.uimsError = ''; element.className = 'uims-error'; element.setAttribute('role', 'alert'); (scope.querySelector('.modal-body') || scope).append(element); }
            if (element) element.textContent = error.message; else notice(error.message);
        }
        const dirty = new WeakSet(), openers = new WeakMap();
        let beforeClose = null;
        function open(id) {
            const dialog = by(id); if (!dialog) throw Error('Missing original popup: ' + id);
            const alreadyOpen = dialog.tagName === 'DIALOG' ? dialog.open : dialog.classList.contains('show');
            if (!alreadyOpen) { openers.set(dialog, document.activeElement); dirty.delete(dialog); }
            dialog.querySelectorAll('[data-uims-error],.uims-discard').forEach(el => el.remove());
            if (dialog.tagName === 'DIALOG') { if (!dialog.open) dialog.showModal(); }
            else if (dialog.classList.contains('offcanvas')) bootstrap.Offcanvas.getOrCreateInstance(dialog).show();
            else bootstrap.Modal.getOrCreateInstance(dialog).show();
            dialog.querySelector('input:not([readonly]):not([disabled]),textarea:not([disabled]),select:not([disabled])')?.focus();
        }
        function close(id, force = false) {
            const dialog = by(id); if (!dialog) return;
            if (!force && beforeClose?.(id) === false) return;
            if (dirty.has(dialog) && !force) {
                if (dialog.querySelector('.uims-discard')) return;
                const prompt = document.createElement('div'); prompt.className = 'uims-discard'; prompt.setAttribute('role', 'alert');
                prompt.innerHTML = '<p>Discard unsaved changes?</p><button type="button" data-keep>Keep editing</button><button type="button" data-discard>Discard changes</button>';
                (dialog.querySelector('.modal-footer') || dialog).append(prompt);
                prompt.querySelector('[data-keep]').onclick = () => prompt.remove();
                prompt.querySelector('[data-discard]').onclick = () => { prompt.remove(); close(id, true); };
                prompt.querySelector('button').focus(); return;
            }
            dirty.delete(dialog);
            if (dialog.tagName === 'DIALOG') dialog.close();
            else if (dialog.classList.contains('offcanvas')) bootstrap.Offcanvas.getInstance(dialog)?.hide();
            else bootstrap.Modal.getInstance(dialog)?.hide();
            openers.get(dialog)?.focus();
        }
        function transact(action, payload, popup) {
            try {
                const result = demo.transaction(action, payload); view = demo.view();
                if (popup) close(popup, true);
                return { ok: true, result };
            } catch (exception) { error(exception, popup ? by(popup) : null); return { ok: false }; }
        }
        function valid(ids, popup) {
            const invalid = ids.map(by).find(field => field && (!field.checkValidity() || field.required && !field.value.trim()));
            if (!invalid) return true;
            invalid.reportValidity(); error(Error('Check the highlighted field.'), by(popup)); return false;
        }
        const actions = {};
        document.addEventListener('click', event => {
            const control = event.target.closest('[data-uims]'); if (!control) return;
            event.preventDefault();
            try { actions[control.dataset.uims]?.(control.dataset.record, control); } catch (exception) { error(exception); }
        }, { signal });
        all('dialog,.modal,.offcanvas').forEach(dialog => {
            const changed = () => dirty.add(dialog);
            dialog.addEventListener('input', changed, { signal }); dialog.addEventListener('change', changed, { signal });
            dialog.addEventListener('cancel', event => { event.preventDefault(); close(dialog.id); }, { signal });
            dialog.addEventListener('hide.bs.modal', event => { if (dirty.has(dialog)) { event.preventDefault(); close(dialog.id); } }, { signal });
            dialog.addEventListener('hidden.bs.modal', () => openers.get(dialog)?.focus(), { signal });
            dialog.addEventListener('click', event => {
                if (event.defaultPrevented) return;
                if (event.target.closest('[data-bs-dismiss], [data-close], [data-completion-close], [data-profile-cancel], .popup-close, .monitor-close, .evaluation-close, .companies-close, .placements-close, [data-close-exam]')) {
                    event.preventDefault(); event.stopPropagation(); close(dialog.id);
                } else if (event.target === dialog && dialog.tagName === 'DIALOG') {
                    const rect = dialog.getBoundingClientRect();
                    if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) close(dialog.id);
                }
            }, { signal });
        });
        function detail(student, id, selector) {
            const progress = view.progress.find(item => item.id === student); if (!progress) throw Error('This student is not available to this demo identity.');
            const target = document.querySelector(selector);
            if (target) target.innerHTML = '<h3>' + escape(progress.name) + '</h3><dl>' + [['University ID', progress.universityId], ['Department', progress.department], ['Company', progress.company?.name], ['Company Supervisor', progress.companySupervisor?.name], ['Academic Supervisor', progress.supervisor?.name], ['Training status', progress.placement?.completionStatus || 'Not placed'], ['Approved / Required hours', progress.approvedHours + ' / ' + progress.requiredHours], ['Tasks completed', progress.completedTasks + ' / ' + progress.tasks.length], ['Risk indicators', progress.risks.join(', ') || 'None'], ['Confirmed at UTC', progress.placement?.studentConfirmedAt], ['Disable deadline UTC', progress.placement?.accountDisableAt], ['Certificate', progress.placement?.certificateId]].map(([label, content]) => '<dt>' + escape(label) + '</dt><dd>' + escape(content || '—') + '</dd>').join('') + '</dl>' + progress.tasks.map(task => '<article><h4>' + escape(task.title) + '</h4><p>' + escape(task.description) + '</p><p>' + escape(task.deadline) + ' · ' + escape(task.status) + '</p><p>' + escape(task.feedback) + '</p>' + view.submissions.filter(sub => sub.taskId === task.id).map(sub => '<p>Submitted ' + escape(sub.createdAt) + ': ' + escape(sub.description) + ' · ' + escape(sub.fileName) + '</p>').join('') + '</article>').join('') + progress.evaluations.map(evaluation => '<article><h4>' + escape(evaluation.kind) + ' evaluation · ' + escape(evaluation.status) + '</h4><p>' + escape(evaluation.comments) + '</p></article>').join('');
            selected = student; open(id);
        }
        function shell() {
            all('[id$="lblUserName"],[id$="NavName"]').forEach(el => el.textContent = view.user.name);
            document.addEventListener('click', event => {
                const element = event.target.closest('a,button,input');
                if (element && (/log\s*out/i.test(element.textContent) || /logout/i.test(element.id))) {
                    event.preventDefault(); event.stopImmediatePropagation(); demo.logout(); location.href = '../Auth/Login.aspx?demo=1';
                }
            }, { signal, capture: true });
            all('[id*=Logout],[id*=logout]').forEach(el => el.disabled = false);
            if (!document.querySelector('script[src*="dashboard-navigation.js"]')) {
                const sidebar = document.querySelector('aside.sidebar,aside.dash-sidebar,aside[id$="Sidebar"]');
                const toggle = document.querySelector('.menu-btn,.dash-menu-btn');
                const main = document.querySelector('.main,.dash-main');
                const media = matchMedia('(max-width:991.98px)');
                const setOpen = show => {
                    const active = media.matches && show;
                    document.body.classList.toggle('sidebar-open', active); toggle?.setAttribute('aria-expanded', String(active));
                    if (sidebar) sidebar.inert = media.matches && !active;
                    if (main) main.inert = active;
                    document.querySelector('.sidebar-backdrop,.dash-sidebar-backdrop')?.toggleAttribute('hidden', !active);
                    if (active) sidebar?.querySelector('button,a[href]')?.focus();
                };
                window.toggleSidebar = () => setOpen(!document.body.classList.contains('sidebar-open'));
                if (toggle) { toggle.onclick = null; toggle.addEventListener('click', window.toggleSidebar, { signal }); }
                all('.sidebar-close,.dash-sidebar-close,.legacy-sidebar-close,.sidebar-backdrop,.dash-sidebar-backdrop').forEach(el => el.addEventListener('click', () => { setOpen(false); toggle?.focus(); }, { signal }));
                document.addEventListener('keydown', event => { if (event.key === 'Escape' && document.body.classList.contains('sidebar-open')) { setOpen(false); toggle?.focus(); } }, { signal });
                media.addEventListener('change', () => setOpen(false), { signal }); setOpen(false);
            }
            const topSearch = document.querySelector('.topbar input,.dash-topbar input');
            const search = document.querySelector('main input[type="search"],.content input[type="search"],#taskSearch,#searchInput');
            if (topSearch && !search) { topSearch.disabled = false; topSearch.addEventListener('keydown', event => { if (event.key !== 'Enter') return; event.preventDefault(); const route = { admin: 'MonitorPlacementsProgress.aspx', student: 'ExploreOpportunities.aspx', company: 'MyTrainees.aspx', academic: 'StudentProgressMonitor.aspx' }[view.user.role]; location.href = route + '?search=' + encodeURIComponent(topSearch.value.trim()); }, { signal }); }
            if (topSearch && search && topSearch !== search) { topSearch.disabled = false; topSearch.addEventListener('input', () => { search.value = topSearch.value; search.dispatchEvent(new Event('input')); }, { signal }); }
        }

        function notifications() {
            const items = [];
            if (view.user.role === 'student') { view.applications.forEach(a => items.push('Application: ' + a.status)); view.placements.filter(p => p.completionStatus === 'PendingStudentConfirmation').forEach(() => items.push('Coordinator approval received. Confirm completion in the Student Dashboard.')); }
            if (view.user.role === 'company') items.push(view.applications.filter(a => a.status === 'pending').length + ' pending applications', view.tasks.filter(t => t.status === 'submitted').length + ' task submissions awaiting review');
            if (view.user.role === 'academic') items.push(view.progress.filter(s => s.evaluations.some(e => e.kind === 'company' && e.status === 'submitted') && !s.evaluations.some(e => e.kind === 'academic' && e.status === 'submitted')).length + ' assigned students awaiting academic evaluation');
            if (view.user.role === 'admin') items.push(view.messages.filter(m => m.status === 'unread').length + ' unread contact messages', view.progress.filter(s => s.placement?.completionStatus === 'PendingStudentConfirmation').length + ' pending student confirmations');
            let dialog = by('originalNotifications');
            if (!dialog) { dialog = document.createElement('dialog'); dialog.id = 'originalNotifications'; dialog.className = 'uims-notifications'; dialog.setAttribute('aria-label', 'Demo notifications'); document.forms[0].append(dialog); }
            dialog.innerHTML = '<h2>Notifications · local demo</h2>' + items.map(item => '<p>' + escape(item) + '</p>').join('') + (items.length ? '' : '<p>No notifications.</p>') + '<button type="button" data-notification-close>Close</button>';
            dialog.querySelector('button').onclick = () => dialog.close(); dialog.showModal();
        }
        all('.topbar button,.dash-topbar button').forEach(el => { if (!/notification/i.test(el.getAttribute('aria-label') || '') && !el.querySelector('.bi-bell')) return; el.disabled = false; el.title = 'Local demo notifications'; el.addEventListener('click', event => { event.preventDefault(); event.stopImmediatePropagation(); view = demo.view(); if (view) notifications(); }, { signal, capture: true }); });
        shell();


        // ================= Manage Opportunities page =================
        let tab = 'all', toastTimer = null;
        const statusKey = s => String(s || '').toLowerCase() === 'closed' ? 'closed' : 'open';
        const applicantsOf = id => view.applications.filter(a => a.opportunityId === id);
        const acceptedOf = o => Number(o?.acceptedCount) || applicantsOf(o?.id).filter(a => a.status === 'accepted').length;
        const companyName = () => view.user.companyName || view.opportunities.find(o => o.companyName)?.companyName || 'Your company';
        const initials = name => String(name || '').split(/\s+/).filter(Boolean).slice(0, 2).map(word => word[0].toUpperCase()).join('') || 'CO';
        const plural = (n, word) => n + ' ' + word + (n === 1 ? '' : 's');
        const toDate = s => { if (!s) return null; const d = new Date(String(s).slice(0, 10) + 'T00:00:00'); return isNaN(d) ? null : d; };
        const formatDate = d => d ? d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : '—';
        const daysUntil = s => { const d = toDate(s); if (!d) return null; const today = new Date(); today.setHours(0, 0, 0, 0); return Math.round((d - today) / 86400000); };
        const deadlineInfo = (s, closed) => {
            const n = daysUntil(s);
            if (n === null) return { label: 'No deadline', cls: '' };
            if (closed) return { label: 'Deadline ' + formatDate(toDate(s)), cls: '' };
            if (n < 0) return { label: 'Deadline passed', cls: 'is-late' };
            if (n === 0) return { label: 'Closes today', cls: 'is-soon' };
            if (n <= 7) return { label: plural(n, 'day') + ' left', cls: 'is-soon' };
            return { label: 'Apply by ' + formatDate(toDate(s)), cls: '' };
        };
        const checked = name => document.querySelector('[name="' + name + '"]:checked')?.value || '';

        function toast(message, kind = 'success') {
            const element = by('pageToast'); if (!element) return;
            text('toastText', message);
            const icon = by('toastIcon'); if (icon) icon.className = 'bi ' + (kind === 'success' ? 'bi-check-circle-fill' : kind === 'error' ? 'bi-x-circle-fill' : 'bi-info-circle-fill');
            element.classList.remove('is-success', 'is-error', 'is-neutral', 'show'); void element.offsetWidth;
            element.classList.add('is-' + kind, 'show');
            clearTimeout(toastTimer); toastTimer = setTimeout(() => element.classList.remove('show'), 3200);
        }

        function card(o) {
            const closed = statusKey(o.status) === 'closed';
            const seats = Number(o.seats) || 0, accepted = acceptedOf(o), apps = applicantsOf(o.id).length;
            const pct = seats ? Math.min(100, Math.round(accepted / seats * 100)) : 0;
            const deadline = deadlineInfo(o.deadline, closed);
            const name = o.companyName || companyName();
            const meta = [['bi-geo-alt', o.location], ['bi-laptop', o.workMode || 'On-site'], ['bi-hourglass-split', o.duration ? o.duration + ' wks' : ''], ['bi-clock-history', o.requiredHours ? o.requiredHours + ' h' : '']]
                .filter(item => item[1]).map(([icon, label]) => '<span><i class="bi ' + icon + '"></i>' + escape(label) + '</span>').join('');
            return '<div class="col-md-6 col-xxl-4"><article class="opp-card' + (closed ? ' is-closed' : '') + '">'
                + '<div class="opp-top"><span class="status ' + (closed ? 'neutral' : 'success') + '">' + (closed ? 'Closed' : 'Open') + '</span>'
                + '<span class="opp-deadline ' + deadline.cls + '"><i class="bi bi-calendar-event"></i>' + escape(deadline.label) + '</span></div>'
                + '<div class="opp-head"><div class="opp-logo">' + escape(initials(name)) + '</div><div class="opp-head-text"><h3 class="opp-title">' + escape(o.title) + '</h3><div class="opp-company">' + escape(name) + '</div></div></div>'
                + '<div class="opp-meta">' + meta + '</div>'
                + '<p class="opp-desc">' + escape(o.description || '') + '</p>'
                + (o.department ? '<div class="opp-majors"><span class="tag"><i class="bi bi-mortarboard"></i>' + escape(o.department) + '</span></div>' : '')
                + '<div class="opp-seats"><div class="seats-row"><span><strong>' + accepted + '</strong> of ' + (seats || '—') + ' seats filled' + (seats && accepted >= seats ? '<span class="full-flag">Full</span>' : '') + '</span>'
                + '<span class="text-secondary"><i class="bi bi-people"></i>' + plural(apps, 'applicant') + '</span></div>'
                + '<div class="progress-slim"><span class="' + (pct >= 100 ? 'success' : '') + '" style="width:' + pct + '%"></span></div></div>'
                + '<div class="opp-actions"><button type="button" class="btn-ghost" data-uims="edit" data-record="' + escape(o.id) + '" aria-label="Edit ' + escape(o.title) + '"><i class="bi bi-pencil"></i> Edit</button>'
                + (closed ? '' : '<button type="button" class="btn-ghost danger" data-uims="close" data-record="' + escape(o.id) + '" aria-label="Close ' + escape(o.title) + '"><i class="bi bi-lock"></i> Close</button>') + '</div>'
                + '</article></div>';
        }

        by('majorPicker').innerHTML = demo.departments.map((department, index) => '<li><input type="radio" name="demoOpportunityDepartment" id="oppMajor' + index + '" value="' + escape(department) + '"><label for="oppMajor' + index + '">' + escape(department) + '</label></li>').join('');

        render = () => {
            const query = value('searchBox').toLowerCase();
            const counts = { all: view.opportunities.length, open: 0, closed: 0 };
            view.opportunities.forEach(o => counts[statusKey(o.status)]++);
            all('#statusTabs [data-count]').forEach(el => el.textContent = counts[el.dataset.count.toLowerCase()] ?? 0);
            const records = view.opportunities.filter(o => (tab === 'all' || statusKey(o.status) === tab) && (!query || [o.title, o.location, o.department].some(field => String(field || '').toLowerCase().includes(query))));
            records.sort((a, b) => value('sortBy') === 'title' ? String(a.title).localeCompare(String(b.title)) : value('sortBy') === 'deadline' ? String(a.deadline || '9999').localeCompare(String(b.deadline || '9999')) : value('sortBy') === 'applicants' ? applicantsOf(b.id).length - applicantsOf(a.id).length : (Date.parse(b.createdAt) || 0) - (Date.parse(a.createdAt) || 0));
            fill('#oppGrid', records, card);
            const open = view.opportunities.filter(o => statusKey(o.status) === 'open');
            text('statTotal', counts.all);
            text('statOpen', counts.open);
            text('statSeats', open.reduce((sum, o) => sum + Math.max(0, (Number(o.seats) || 0) - acceptedOf(o)), 0));
            text('statApplicants', open.reduce((sum, o) => sum + applicantsOf(o.id).length, 0));
            const empty = records.length === 0;
            by('emptyState').hidden = !empty; by('oppGrid').hidden = empty;
            if (empty) {
                if (query) { text('emptyTitle', 'No matches found'); text('emptyText', 'Nothing matches “' + value('searchBox') + '”. Try a different title, location or specialization.'); }
                else if (tab !== 'all') { text('emptyTitle', 'No ' + tab + ' opportunities'); text('emptyText', tab === 'open' ? 'Publish a new internship to start receiving applications.' : 'Opportunities you close will appear here.'); }
                else { text('emptyTitle', 'No opportunities here yet'); text('emptyText', 'Post your first internship so JUST students can start applying.'); }
            }
            text('resultCount', counts.all ? 'Showing ' + records.length + ' of ' + counts.all : '');
        };

        function charCounts() {
            all('#oppModal .char-count[data-for]').forEach(el => {
                const field = by(el.dataset.for); if (!field) return;
                const max = Number(field.maxLength) || 0, length = field.value.length;
                el.textContent = max > 0 ? length + ' / ' + max : '';
                el.classList.toggle('near-limit', max > 0 && length >= max * .9);
            });
        }
        function dateHint() {
            const hint = by('dateHint'), output = by('dateHintText'); if (!hint || !output) return;
            const start = toDate(value('oppStart')), deadline = toDate(value('oppDeadline')), weeks = Number(value('oppDuration'));
            let message = 'Training weeks begin on company acceptance. Attendance is available at any hour during that period.', warning = false;
            if (start && deadline && deadline > start) { message = 'The application deadline must be on or before the start date.'; warning = true; }
            else if (deadline && !selected && daysUntil(value('oppDeadline')) < 0) { message = 'The application deadline cannot be in the past.'; warning = true; }
            else if (weeks > 0) { message = 'Training lasts ' + plural(weeks, 'week') + ' from company acceptance. Attendance is available at any hour.'; }
            output.textContent = message; hint.classList.toggle('is-warning', warning);
            const icon = hint.querySelector('i'); if (icon) icon.className = 'bi ' + (warning ? 'bi-exclamation-triangle' : 'bi-calendar-check');
        }
        function placeholder(id, content, fallback) { const el = by(id); if (!el) return; el.textContent = content || fallback; el.classList.toggle('is-placeholder', !content); }
        function preview() {
            const o = view.opportunities.find(item => item.id === selected);
            placeholder('pvTitle', value('oppTitle'), 'Opportunity title');
            placeholder('pvDesc', value('oppDescription'), 'A short description of the internship appears here.');
            placeholder('pvLocation', value('oppLocation'), 'Location');
            text('pvMode', checked('workMode') || 'On-site');
            text('pvDuration', value('oppDuration') || '—');
            text('pvHours', value('oppRequiredHours') || '—');
            const department = checked('demoOpportunityDepartment');
            by('pvMajors').innerHTML = department ? '<span class="tag"><i class="bi bi-mortarboard"></i>' + escape(department) + '</span>' : '<span class="tag tag-empty">No specialization selected</span>';
            const seats = Number(value('oppSeats')) || 0, accepted = o ? acceptedOf(o) : 0;
            text('pvSeats', seats || '—'); text('pvAccepted', accepted); text('pvApplicants', o ? applicantsOf(o.id).length : 0);
            const bar = by('pvBar'); if (bar) bar.style.width = (seats ? Math.min(100, accepted / seats * 100) : 0) + '%';
            const closed = o && statusKey(o.status) === 'closed', status = by('pvStatus');
            if (status) { status.textContent = closed ? 'Closed' : 'Open'; status.className = 'status ' + (closed ? 'neutral' : 'success'); }
            text('pvDeadline', value('oppDeadline') ? formatDate(toDate(value('oppDeadline'))) : '—');
            const name = o?.companyName || companyName(); text('pvCompany', name); text('pvLogo', initials(name));
            dateHint(); charCounts();
        }
        function editor(id) {
            selected = id; const o = view.opportunities.find(o => o.id === id);
            for (const [field, key] of Object.entries({ oppTitle: 'title', oppDescription: 'description', oppRequirements: 'requirements', oppLocation: 'location', oppSeats: 'seats', oppDeadline: 'deadline', oppStart: 'start', oppDuration: 'duration', oppRequiredHours: 'requiredHours' })) set(field, o?.[key] ?? ({ oppSeats: 1, oppDuration: 8, oppRequiredHours: 160 }[field] || ''));
            all('[name="demoOpportunityDepartment"]').forEach(el => el.checked = el.value === (o?.department || demo.departments[0]));
            all('[name="workMode"]').forEach(el => el.checked = el.value === (o?.workMode || 'On-site'));
            text('oppModalTitle', id ? 'Edit opportunity' : 'New opportunity');
            text('oppModalSub', id ? 'Update the details below. Changes are visible to students right away.' : 'Fill in the details below. Students in the selected specialization will see it right away.');
            text('saveOppText', id ? 'Save changes' : 'Publish opportunity');
            const icon = by('oppModalIcon'); if (icon) icon.className = 'bi ' + (id ? 'bi-pencil-square' : 'bi-briefcase');
            open('oppModal'); preview();
        }
        all('[data-action="new"]').forEach(el => el.addEventListener('click', () => editor(null), { signal })); actions.edit = editor;
        all('#oppModal input,#oppModal textarea').forEach(el => { el.addEventListener('input', preview, { signal }); el.addEventListener('change', preview, { signal }); });
        all('[data-step]').forEach(el => el.addEventListener('click', () => {
            const input = by(el.dataset.target); if (!input) return;
            const min = input.min === '' ? -Infinity : Number(input.min), max = input.max === '' ? Infinity : Number(input.max);
            input.value = Math.min(max, Math.max(min, (Number(input.value) || 0) + Number(el.dataset.step)));
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }, { signal }));
        click('saveOpp', () => {
            if (!valid(['oppTitle', 'oppDescription', 'oppSeats', 'oppLocation', 'oppDeadline', 'oppStart', 'oppDuration', 'oppRequiredHours'], 'oppModal')) return;
            const editing = !!selected;
            const outcome = transact('opportunity', { id: selected, title: value('oppTitle'), description: value('oppDescription'), requirements: value('oppRequirements'), department: checked('demoOpportunityDepartment'), location: value('oppLocation'), requiredHours: value('oppRequiredHours'), seats: value('oppSeats'), deadline: value('oppDeadline'), start: value('oppStart'), duration: value('oppDuration'), workMode: checked('workMode') }, 'oppModal');
            if (outcome.ok) toast(editing ? 'Opportunity updated.' : 'Opportunity published.');
        });
        actions.close = id => {
            selected = id; const o = view.opportunities.find(o => o.id === id); text('closeSub', o?.title);
            const pending = applicantsOf(id).filter(a => a.status === 'pending').length, line = by('closePendingLine');
            if (line) { line.hidden = !pending; const span = line.querySelector('span:last-child'); if (span) span.textContent = plural(pending, 'pending application') + ' still ' + (pending === 1 ? 'needs' : 'need') + ' your review.'; }
            open('closeModal');
        };
        click('confirmClose', () => { if (transact('closeOpportunity', { id: selected }, 'closeModal').ok) toast('Opportunity closed.', 'neutral'); });
        all('#statusTabs [data-status]').forEach(el => el.addEventListener('click', () => { tab = el.dataset.status.toLowerCase(); all('#statusTabs button').forEach(button => { button.classList.toggle('active', button === el); button.setAttribute('aria-selected', String(button === el)); }); render(); }, { signal })); on('searchBox', 'input', render); on('sortBy', 'change', render);

        ['oppTitle', 'oppDescription', 'oppSeats', 'oppLocation', 'oppDeadline', 'oppStart', 'oppDuration', 'oppRequiredHours'].forEach(id => by(id).required = true);

        render();
        window.addEventListener('storage', event => { if (event.key === 'uims.demo.v1') { view = demo.view(); if (view && !view.user.mustChangePassword && view.user.role === document.body.dataset.demoRole) render(); else location.href = '../Auth/Login.aspx?demo=1'; } }, { signal });
        window.addEventListener('focus', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
        window.addEventListener('pageshow', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();