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
        if (view.connectionError) notice(view.connectionError);
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
                render(); notice('Saved to Firebase.');
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


        // ================= Explore Opportunities page =================
        const grid = by('opportunitiesList');
        const companyOf = o => view.companies.find(c => c.id === o?.companyId);
        const companyName = o => companyOf(o)?.name || o?.companyName || 'Company';
        const initials = name => String(name || '').split(/\s+/).filter(Boolean).slice(0, 2).map(word => word[0].toUpperCase()).join('') || 'CO';
        const tone = key => { let hash = 0; for (const c of String(key || '')) hash = (hash * 31 + c.charCodeAt(0)) | 0; return 'tone-' + Math.abs(hash) % 4; };
        const plural = (n, word) => n + ' ' + word + (n === 1 ? '' : 's');
        const toDate = s => { if (!s) return null; const d = new Date(String(s).slice(0, 10) + 'T00:00:00'); return isNaN(d) ? null : d; };
        const formatDate = (s, opts = { day: '2-digit', month: 'short', year: 'numeric' }) => { const d = toDate(s); return d ? d.toLocaleDateString('en-GB', opts) : '—'; };
        const daysUntil = s => { const d = toDate(s); if (!d) return null; const today = new Date(); today.setHours(0, 0, 0, 0); return Math.round((d - today) / 86400000); };
        const applicationFor = id => view.applications?.find(a => a.opportunityId === id && a.studentId === view.user.id) || view.applications?.find(a => a.opportunityId === id && !a.studentId);
        const deadlineInfo = o => {
            if (applicationFor(o.id)) return { cls: 'is-applied', icon: 'bi-check2-circle', label: 'Applied', banner: 'You already applied to this opportunity. Track it in My Applications.' };
            const n = daysUntil(o.deadline);
            if (n === null) return { cls: '', icon: 'bi-calendar-event', label: 'Open', banner: '' };
            if (n < 0) return { cls: 'is-late', icon: 'bi-x-circle', label: 'Closed', banner: 'The application deadline has passed.' };
            if (n === 0) return { cls: 'is-soon', icon: 'bi-alarm', label: 'Closes today', banner: 'Applications close today — apply soon.' };
            if (n <= 7) return { cls: 'is-soon', icon: 'bi-alarm', label: plural(n, 'day') + ' left', banner: 'Applications close in ' + plural(n, 'day') + ' (' + formatDate(o.deadline) + ').' };
            return { cls: '', icon: 'bi-calendar-event', label: 'Apply by ' + formatDate(o.deadline, { day: '2-digit', month: 'short' }), banner: 'Apply by ' + formatDate(o.deadline) + '.' };
        };
        const seatsLeft = o => { const seats = Number(o.seats); if (!seats) return null; return Math.max(0, seats - (Number(o.acceptedCount) || 0)); };
        const durationOf = o => o.duration || o.durationWeeks;
        const modeIcon = mode => /remote/i.test(mode || '') ? 'bi-house' : /hybrid/i.test(mode || '') ? 'bi-arrow-left-right' : 'bi-building';
        const facts = o => {
            const left = seatsLeft(o), weeks = durationOf(o); return '<dl class="opportunity-facts">'
                + '<div><dt><i class="bi bi-hourglass-split"></i> Duration</dt><dd>' + escape(weeks ? plural(Number(weeks), 'week') : '—') + '</dd></div>'
                + '<div><dt><i class="bi bi-people"></i> Seats left</dt><dd>' + escape(left === null ? '—' : plural(left, 'student')) + '</dd></div>'
                + '<div><dt><i class="bi bi-calendar-event"></i> Deadline</dt><dd>' + (o.deadline ? '<time datetime="' + escape(o.deadline) + '">' + escape(formatDate(o.deadline)) + '</time>' : '—') + '</dd></div>'
                + '<div><dt><i class="bi bi-clock-history"></i> Training hours</dt><dd>' + escape(o.requiredHours ? o.requiredHours + ' hours' : '—') + '</dd></div></dl>';
        };
        const isOpen = o => String(o.status || '').toLowerCase() !== 'closed';
        const filtersActive = () => !!(value('searchInput') || value('majorFilter') || value('locationFilter'));

        function card(o) {
            const name = companyName(o), dl = deadlineInfo(o);
            return '<div class="col-md-6 col-xxl-4 opportunity-item" data-id="' + escape(o.id) + '"><article class="opportunity-card">'
                + '<div class="card-top"><div class="company-heading"><span class="company-mark ' + tone(o.companyId || name) + '" aria-hidden="true">' + escape(initials(name)) + '</span><div class="min-w-0"><p class="company-name">' + escape(name) + '</p><p class="company-location"><i class="bi bi-geo-alt"></i> ' + escape(o.location || '—') + '</p></div></div>'
                + '<span class="deadline-badge ' + dl.cls + '"><i class="bi ' + dl.icon + '"></i> ' + escape(dl.label) + '</span></div>'
                + '<h3>' + escape(o.title) + '</h3>'
                + '<div class="card-tags">' + (o.department ? '<span class="specialization"><i class="bi bi-mortarboard"></i> ' + escape(o.department) + '</span>' : '') + '<span class="mode-tag"><i class="bi ' + modeIcon(o.workMode) + '"></i> ' + escape(o.workMode || 'On-site') + '</span></div>'
                + '<p class="opportunity-description">' + escape(o.description || 'View internship details and apply.') + '</p>'
                + facts(o)
                + '<button type="button" class="details-button" data-uims="details" data-record="' + escape(o.id) + '">View details <i class="bi bi-arrow-right"></i></button>'
                + '</article></div>';
        }

        const majors = [...new Set([view.user.department, ...view.opportunities.map(o => o.department)].filter(Boolean))];
        options('majorFilter', majors.map(name => ({ id: name, name })), 'All specializations');
        options('locationFilter', [...new Set(view.opportunities.map(o => o.location).filter(Boolean))].map(name => ({ id: name, name })), 'All locations');
        if (by('sampleNote')) by('sampleNote').hidden = true;
        if (by('modalNote')) by('modalNote').hidden = true;

        render = () => {
            if (!grid) return;
            const query = value('searchInput').toLowerCase();
            const openList = view.opportunities.filter(isOpen);
            const records = openList.filter(o => (!query || [o.title, companyName(o)].join(' ').toLowerCase().includes(query)) && (!value('majorFilter') || o.department === value('majorFilter')) && (!value('locationFilter') || o.location === value('locationFilter')));
            records.sort((a, b) => value('sortBy') === 'title' ? String(a.title).localeCompare(String(b.title)) : value('sortBy') === 'newest' ? (Date.parse(b.createdAt) || 0) - (Date.parse(a.createdAt) || 0) : String(a.deadline || '9999').localeCompare(String(b.deadline || '9999')));
            grid.innerHTML = records.map(card).join('');
            text('resultCount', records.length);
            by('emptyState').hidden = records.length > 0;
            by('resetFilters').hidden = !filtersActive();
            text('heroOpen', openList.length);
            text('heroSoon', openList.filter(o => { const n = daysUntil(o.deadline); return n !== null && n >= 0 && n <= 7; }).length);
            text('heroApplied', (view.applications || []).filter(a => !a.studentId || a.studentId === view.user.id).length);
        };
        actions.details = id => {
            const o = view.opportunities.find(item => item.id === id); if (!o) throw Error('This opportunity is no longer available.');
            const name = companyName(o), dl = deadlineInfo(o), applied = applicationFor(o.id);
            text('modalTitle', o.title); text('modalCompany', name);
            const mark = by('modalMark'); if (mark) { mark.textContent = initials(name); mark.className = 'company-mark lg ' + tone(o.companyId || name); }
            by('modalMajor').innerHTML = o.department ? '<i class="bi bi-mortarboard"></i> ' + escape(o.department) : '';
            by('modalLocation').innerHTML = '<i class="bi bi-geo-alt"></i> ' + escape(o.location || '—') + ' · ' + escape(o.workMode || 'On-site');
            const banner = by('modalDeadline'); if (banner) { banner.hidden = !dl.banner; banner.className = 'deadline-banner ' + dl.cls; banner.innerHTML = '<i class="bi ' + dl.icon + '"></i> ' + escape(dl.banner); }
            by('modalFacts').innerHTML = facts(o);
            by('modalDetails').innerHTML = '<h4>About the internship</h4><p>' + escape(o.description || 'No description provided.') + '</p>'
                + (o.requirements ? '<h4>Requirements</h4><ul>' + String(o.requirements).split(/\r?\n|;|•/).map(item => item.trim()).filter(Boolean).map(item => '<li>' + escape(item) + '</li>').join('') + '</ul>' : '');
            const apply = by('applyLink');
            if (apply) {
                const late = daysUntil(o.deadline) !== null && daysUntil(o.deadline) < 0;
                apply.classList.toggle('is-applied', !!applied);
                apply.removeAttribute('aria-disabled');
                if (applied) { apply.href = 'MyApplications.aspx'; apply.innerHTML = '<i class="bi bi-check2-circle"></i> View my application'; }
                else { apply.href = 'ApplicationForm.aspx?opportunity=' + encodeURIComponent(id); apply.innerHTML = 'Apply now <i class="bi bi-arrow-right"></i>'; if (late) apply.setAttribute('aria-disabled', 'true'); }
            }
            open('opportunityModal');
        };
        for (const id of ['searchInput', 'majorFilter', 'locationFilter', 'sortBy']) on(id, id === 'searchInput' ? 'input' : 'change', render);
        const reset = () => { ['searchInput', 'majorFilter', 'locationFilter'].forEach(id => set(id, '')); const top = by('navbarSearch'); if (top) top.value = ''; render(); };
        click('resetFilters', reset); click('emptyReset', reset);
        set('searchInput', new URLSearchParams(location.search).get('search') || '');

        render();
        window.addEventListener('storage', event => { if (event.key === 'uims.demo.v1') { view = demo.view(); if (view && !view.user.mustChangePassword && view.user.role === document.body.dataset.demoRole) render(); else location.href = '../Auth/Login.aspx?demo=1'; } }, { signal });
        window.addEventListener('focus', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
        window.addEventListener('pageshow', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();

/* Sample-card behaviour when the demo store is not active: filters, counters and the details popup
   work on the cards that are already in the page. It never navigates or submits anything. */
(function() {
    'use strict';
    if (window.UimsDemo?.enabled()) return;
    function start() {
        const $ = id => document.getElementById(id);
        const items = [...document.querySelectorAll('#opportunitiesList .opportunity-item')];
        if (!items.length) return;
        const toDate = s => { const d = new Date(String(s || '').slice(0, 10) + 'T00:00:00'); return isNaN(d) ? null : d; };
        const daysUntil = s => { const d = toDate(s); if (!d) return null; const t = new Date(); t.setHours(0, 0, 0, 0); return Math.round((d - t) / 86400000); };
        const read = (item, selector) => item.querySelector(selector)?.textContent.trim() || '';
        items.forEach(item => {
            const badge = item.querySelector('.deadline-badge'); const n = daysUntil(badge?.dataset.deadline);
            if (!badge || n === null) return;
            if (n < 0) { badge.classList.add('is-late'); badge.innerHTML = '<i class="bi bi-x-circle"></i> Closed'; }
            else if (n <= 7) { badge.classList.add('is-soon'); badge.innerHTML = '<i class="bi bi-alarm"></i> ' + (n === 0 ? 'Closes today' : n + ' day' + (n === 1 ? '' : 's') + ' left'); }
        });
        const soon = items.filter(item => { const n = daysUntil(item.querySelector('.deadline-badge')?.dataset.deadline); return n !== null && n >= 0 && n <= 7; }).length;
        if ($('heroOpen')) $('heroOpen').textContent = items.length;
        if ($('heroSoon')) $('heroSoon').textContent = soon;
        function render() {
            const q = ($('searchInput')?.value || '').trim().toLowerCase(), major = $('majorFilter')?.value || '', place = $('locationFilter')?.value || '';
            const visible = items.filter(item => {
                const ok = (!q || (read(item, 'h3') + ' ' + read(item, '.company-name')).toLowerCase().includes(q)) && (!major || item.dataset.major === major) && (!place || item.dataset.location === place);
                item.hidden = !ok; return ok;
            });
            const sort = $('sortBy')?.value, list = $('opportunitiesList');
            visible.sort((a, b) => sort === 'title' ? read(a, 'h3').localeCompare(read(b, 'h3')) : String(a.querySelector('time')?.getAttribute('datetime') || '9999').localeCompare(String(b.querySelector('time')?.getAttribute('datetime') || '9999')));
            if (sort !== 'newest') visible.forEach(item => list.append(item));
            if ($('resultCount')) $('resultCount').textContent = visible.length;
            if ($('emptyState')) $('emptyState').hidden = visible.length > 0;
            if ($('resetFilters')) $('resetFilters').hidden = !(q || major || place);
        }
        const reset = () => { ['searchInput', 'majorFilter', 'locationFilter'].forEach(id => { if ($(id)) $(id).value = ''; }); render(); };
        $('searchInput')?.addEventListener('input', render);
        ['majorFilter', 'locationFilter', 'sortBy'].forEach(id => $(id)?.addEventListener('change', render));
        $('resetFilters')?.addEventListener('click', reset);
        $('emptyReset')?.addEventListener('click', reset);
        $('navbarSearch')?.addEventListener('input', event => { if ($('searchInput')) { $('searchInput').value = event.target.value; render(); } });
        $('opportunityModal')?.addEventListener('show.bs.modal', event => {
            const card = event.relatedTarget?.closest('.opportunity-card'); if (!card) return;
            const set = (id, html) => { if ($(id)) $(id).innerHTML = html; };
            const mark = card.querySelector('.company-mark');
            if ($('modalMark') && mark) { $('modalMark').textContent = mark.textContent; $('modalMark').className = 'company-mark lg ' + ([...mark.classList].find(c => c.startsWith('tone-')) || 'tone-0'); }
            if ($('modalTitle')) $('modalTitle').textContent = read(card, 'h3');
            if ($('modalCompany')) $('modalCompany').textContent = read(card, '.company-name');
            set('modalMajor', card.querySelector('.specialization')?.innerHTML || '');
            set('modalLocation', card.querySelector('.company-location')?.innerHTML || '');
            set('modalFacts', card.querySelector('.opportunity-facts')?.outerHTML || '');
            set('modalDetails', '<h4>About the internship</h4>' + (card.querySelector('.opportunity-details')?.innerHTML || ''));
            const badge = card.querySelector('.deadline-badge'), banner = $('modalDeadline');
            if (banner) { const n = daysUntil(badge?.dataset.deadline); banner.hidden = n === null || n > 7; banner.className = 'deadline-banner ' + (n !== null && n < 0 ? 'is-late' : 'is-soon'); banner.innerHTML = badge ? badge.innerHTML : ''; }
        });
        render();
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
})();