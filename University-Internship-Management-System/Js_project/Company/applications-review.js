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


        // ================= Applications Review page =================
        let tab = 'all', toastTimer = null;
        const studentOf = application => view.users.find(u => u.id === application?.studentId);
        const opportunityOf = application => view.opportunities.find(o => o.id === application?.opportunityId);
        const isAccepted = status => status === 'accepted' || status === 'confirmed';
        const group = status => isAccepted(status) ? 'accepted' : status === 'rejected' ? 'rejected' : 'pending';
        const isClosed = o => String(o?.status || '').toLowerCase() === 'closed';
        const acceptedFor = id => view.applications.filter(a => a.opportunityId === id && isAccepted(a.status)).length;
        const initials = name => String(name || '').split(/\s+/).filter(Boolean).slice(0, 2).map(word => word[0].toUpperCase()).join('') || '--';
        const tone = key => { let hash = 0; for (const c of String(key || '')) hash = (hash * 31 + c.charCodeAt(0)) | 0; return 'tone-' + Math.abs(hash) % 4; };
        const plural = (n, word) => n + ' ' + word + (n === 1 ? '' : 's');
        const toDate = s => { if (!s) return null; const d = new Date(String(s).length <= 10 ? s + 'T00:00:00' : s); return isNaN(d) ? null : d; };
        const formatDate = s => { const d = toDate(s); return d ? d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : '—'; };
        const relative = s => { const d = toDate(s); if (!d) return ''; const today = new Date(); today.setHours(0, 0, 0, 0); const day = new Date(d); day.setHours(0, 0, 0, 0); const n = Math.round((today - day) / 86400000); return n <= 0 ? 'Today' : n === 1 ? 'Yesterday' : n < 7 ? n + ' days ago' : n < 30 ? plural(Math.floor(n / 7), 'week') + ' ago' : plural(Math.floor(n / 30), 'month') + ' ago'; };
        const statusPill = status => { const s = String(status || 'pending').toLowerCase(); const map = { pending: ['warning', 'Pending'], accepted: ['success', 'Accepted'], confirmed: ['info', 'Confirmed'], rejected: ['danger', 'Rejected'] }; const [cls, label] = map[s] || ['neutral', s]; return '<span class="status ' + cls + '">' + escape(label) + '</span>'; };
        const safeUrl = url => /^https?:\/\//i.test(String(url || '')) ? String(url) : '';
        const actionButton = (action, id, cls, icon, label, iconOnly) => '<button type="button" class="' + cls + '" data-uims="' + action + '" data-record="' + escape(id) + '" title="' + escape(label) + '" aria-label="' + escape(label) + '"><i class="bi ' + icon + '"></i>' + (iconOnly ? '' : ' ' + escape(label)) + '</button>';

        function toast(message, kind = 'success') {
            const element = by('reviewToast'); if (!element) return;
            text('toastText', message);
            const icon = by('toastIcon'); if (icon) icon.className = 'bi ' + (kind === 'success' ? 'bi-check-circle-fill' : kind === 'error' ? 'bi-x-circle-fill' : 'bi-info-circle-fill');
            element.classList.remove('is-success', 'is-error', 'is-neutral', 'show'); void element.offsetWidth;
            element.classList.add('is-' + kind, 'show');
            clearTimeout(toastTimer); toastTimer = setTimeout(() => element.classList.remove('show'), 3200);
        }

        function row(application) {
            const student = studentOf(application), opportunity = opportunityOf(application), pending = application.status === 'pending';
            return '<tr class="' + (pending ? 'is-pending' : '') + '" data-uims="details" data-record="' + escape(application.id) + '">'
                + '<td data-label="Student"><div class="student-cell"><span class="avatar ' + tone(student?.id) + '">' + escape(initials(student?.name)) + '</span><div class="min-w-0"><div class="student-name">' + escape(student?.name || 'Unknown student') + '</div><div class="student-sub">' + escape([student?.universityId, student?.department].filter(Boolean).join(' · ')) + '</div></div></div></td>'
                + '<td data-label="Opportunity"><div><div class="opp-cell">' + escape(opportunity?.title || '—') + (isClosed(opportunity) ? '<span class="closed-flag">Closed</span>' : '') + '</div>' + (opportunity?.location ? '<div class="opp-cell-sub">' + escape(opportunity.location) + '</div>' : '') + '</div></td>'
                + '<td data-label="Applied" class="date-cell"><div><div class="date-main">' + escape(formatDate(application.createdAt)) + '</div><div class="date-sub">' + escape(relative(application.createdAt)) + '</div></div></td>'
                + '<td data-label="Status">' + statusPill(application.status) + '</td>'
                + '<td class="actions-cell"><div class="row-actions">' + actionButton('details', application.id, 'icon-action', 'bi-eye', 'View application', true)
                + (pending ? actionButton('accept', application.id, 'btn-soft success', 'bi-check-lg', 'Accept') + actionButton('reject', application.id, 'btn-ghost danger', 'bi-x-lg', 'Reject') : '')
                + '</div></td></tr>';
        }

        function summary() {
            const box = by('oppSummary'); if (!box) return;
            const id = value('oppFilter'), o = view.opportunities.find(item => item.id === id);
            if (!id || id === 'all' || !o) { box.hidden = true; box.innerHTML = ''; return; }
            const seats = Number(o.seats) || 0, accepted = acceptedFor(o.id), pct = seats ? Math.min(100, Math.round(accepted / seats * 100)) : 0;
            const apps = view.applications.filter(a => a.opportunityId === o.id), pending = apps.filter(a => a.status === 'pending').length;
            const meta = [['bi-geo-alt', o.location], ['bi-laptop', o.workMode], ['bi-mortarboard', o.department], ['bi-calendar-event', o.deadline ? 'Apply by ' + formatDate(o.deadline) : '']].filter(item => item[1]).map(([icon, label]) => '<span><i class="bi ' + icon + '"></i>' + escape(label) + '</span>').join('');
            box.innerHTML = '<div class="summary-main"><div class="summary-logo"><i class="bi bi-briefcase"></i></div><div class="min-w-0">'
                + '<div class="summary-title"><h2>' + escape(o.title) + '</h2>' + (isClosed(o) ? '<span class="status neutral">Closed</span>' : '<span class="status success">Open</span>') + '</div>'
                + '<div class="summary-meta">' + meta + '<span><i class="bi bi-inbox"></i>' + plural(apps.length, 'applicant') + ' · ' + pending + ' pending</span></div></div></div>'
                + '<div class="summary-seats"><div class="seats-row"><span><strong>' + accepted + '</strong> of ' + (seats || '—') + ' seats filled' + (seats && accepted >= seats ? ' <span class="full-flag">Full</span>' : '') + '</span><a class="summary-link" href="ManageOpportunities.aspx">Manage <i class="bi bi-arrow-right"></i></a></div>'
                + '<div class="progress-slim"><span class="' + (pct >= 100 ? 'success' : '') + '" style="width:' + pct + '%"></span></div></div>';
            box.hidden = false;
        }

        options('oppFilter', view.opportunities.map(o => ({ id: o.id, name: o.title })), 'All opportunities');
        render = () => {
            const query = value('searchBox').toLowerCase(), opp = value('oppFilter');
            const scoped = view.applications.filter(application => !opp || opp === 'all' || application.opportunityId === opp);
            const counts = { all: scoped.length, pending: 0, accepted: 0, rejected: 0 };
            scoped.forEach(application => counts[group(application.status)]++);
            all('#statusTabs [data-count]').forEach(el => el.textContent = counts[el.dataset.count.toLowerCase()] ?? 0);
            const records = scoped.filter(application => { const student = studentOf(application); return (tab === 'all' || group(application.status) === tab) && (!query || [student?.name, student?.universityId, student?.department].join(' ').toLowerCase().includes(query)); });
            const nameOf = application => studentOf(application)?.name || '';
            records.sort((a, b) => value('sortBy') === 'name' ? nameOf(a).localeCompare(nameOf(b)) : String(b.createdAt || '').localeCompare(String(a.createdAt || '')) * (value('sortBy') === 'oldest' ? -1 : 1));
            fill('#appRows', records, row);
            const accepted = view.applications.filter(a => isAccepted(a.status)), confirmed = view.applications.filter(a => a.status === 'confirmed').length, pending = view.applications.filter(a => a.status === 'pending').length;
            text('statPending', pending); text('heroPending', pending);
            text('statAccepted', accepted.length);
            text('statAcceptedNote', accepted.length ? plural(confirmed, 'student') + ' confirmed' : 'No offers yet');
            text('statRejected', view.applications.filter(a => a.status === 'rejected').length);
            text('statSeats', view.opportunities.filter(o => !isClosed(o)).reduce((sum, o) => sum + Math.max(0, (Number(o.seats) || 0) - acceptedFor(o.id)), 0));
            const empty = records.length === 0;
            by('emptyState').hidden = !empty;
            document.querySelector('.app-table')?.classList.toggle('is-empty', empty);
            if (empty) {
                if (!view.applications.length) { text('emptyTitle', 'No applications yet'); text('emptyText', 'When JUST students apply to your opportunities, they will appear here.'); }
                else if (query) { text('emptyTitle', 'No matches found'); text('emptyText', 'Nothing matches “' + value('searchBox') + '”. Try a name, university ID or major.'); }
                else { text('emptyTitle', 'No applications match'); text('emptyText', 'Try another opportunity, status, or search word.'); }
            }
            text('resultCount', scoped.length ? 'Showing ' + records.length + ' of ' + scoped.length : '');
            summary();
        };

        actions.details = id => {
            selected = id; const application = view.applications.find(a => a.id === id); if (!application) throw Error('Application not found.');
            const student = studentOf(application), opportunity = opportunityOf(application);
            const avatar = by('drawerAvatar'); if (avatar) { avatar.textContent = initials(student?.name); avatar.className = 'avatar lg ' + tone(student?.id); }
            text('drawerName', student?.name || 'Unknown student');
            text('drawerSub', [student?.universityId, student?.department].filter(Boolean).join(' · '));
            by('drawerStatus').innerHTML = statusPill(application.status);
            const contacts = [student?.email ? '<a href="mailto:' + escape(student.email) + '"><i class="bi bi-envelope"></i>' + escape(student.email) + '</a>' : '', application.phone ? '<a href="tel:' + escape(application.phone) + '"><i class="bi bi-telephone"></i>' + escape(application.phone) + '</a>' : ''].filter(Boolean).join('') || '<span><i class="bi bi-dash-circle"></i>No contact details</span>';
            const file = (label, url, icon, cls) => { const link = safeUrl(url); return link ? '<a class="file-item" href="' + escape(link) + '" target="_blank" rel="noopener noreferrer"><span class="file-icon ' + cls + '"><i class="bi ' + icon + '"></i></span><span><strong>' + label + '</strong><small>' + escape(link) + '</small></span><i class="bi bi-box-arrow-up-right"></i></a>' : url ? '<div class="file-item"><span class="file-icon ' + cls + '"><i class="bi ' + icon + '"></i></span><span><strong>' + label + '</strong><small>' + escape(url) + '</small></span></div>' : '<div class="file-item is-missing"><span class="file-icon"><i class="bi ' + icon + '"></i></span><span><strong>' + label + '</strong><small>Not provided</small></span></div>'; };
            let decision = '';
            if (isAccepted(application.status)) decision = '<div class="decision-box success"><i class="bi bi-check-circle-fill"></i><div><strong>' + (application.status === 'confirmed' ? 'Accepted and confirmed by the student' : 'Accepted — waiting for the student to confirm') + '</strong>' + (application.decidedAt ? '<div>' + escape(formatDate(application.decidedAt)) + '</div>' : '') + '</div></div>';
            else if (application.status === 'rejected') decision = '<div class="decision-box danger"><i class="bi bi-x-circle-fill"></i><div><strong>Rejected</strong>' + (application.reason ? '<blockquote>' + escape(application.reason) + '</blockquote>' : '') + '</div></div>';
            by('drawerBody').innerHTML =
                '<section class="drawer-section"><h3><i class="bi bi-briefcase"></i> Application</h3><dl class="info-grid"><dt>Opportunity</dt><dd>' + escape(opportunity?.title || '—') + '</dd><dt>Applied</dt><dd>' + escape(formatDate(application.createdAt)) + ' <span class="text-secondary fw-normal">· ' + escape(relative(application.createdAt)) + '</span></dd>' + (opportunity?.location ? '<dt>Location</dt><dd>' + escape(opportunity.location) + '</dd>' : '') + '</dl></section>'
                + '<section class="drawer-section"><h3><i class="bi bi-person-lines-fill"></i> Contact</h3><div class="contact-list">' + contacts + '</div></section>'
                + '<section class="drawer-section"><h3><i class="bi bi-chat-quote"></i> Motivation</h3>' + (application.motivation ? '<p class="cover-letter">' + escape(application.motivation) + '</p>' : '<p class="text-secondary mb-0">The student did not add a motivation letter.</p>') + '</section>'
                + '<section class="drawer-section"><h3><i class="bi bi-paperclip"></i> Documents</h3><div class="file-list">' + file('CV', application.cvLink, 'bi-file-earmark-pdf', 'pdf') + file('Portfolio', application.portfolioLink, 'bi-globe2', '') + '</div></section>'
                + (decision ? '<section class="drawer-section"><h3><i class="bi bi-flag"></i> Decision</h3>' + decision + '</section>' : '');
            const seats = Number(opportunity?.seats) || 0, full = seats && acceptedFor(opportunity.id) >= seats;
            by('drawerFooter').innerHTML = application.status === 'pending' ? (full ? '<div class="footer-warning"><i class="bi bi-exclamation-triangle"></i> All seats for this opportunity are filled.</div>' : '') + '<div class="footer-buttons">' + actionButton('reject', id, 'btn-ghost lg danger', 'bi-x-lg', 'Reject') + actionButton('accept', id, 'btn-primary-pill success', 'bi-check-lg', 'Accept') + '</div>' : '';
            open('appDrawer');
        };
        actions.accept = id => {
            selected = id; close('appDrawer', true);
            const application = view.applications.find(a => a.id === id), student = studentOf(application), opportunity = opportunityOf(application);
            const seats = Number(opportunity?.seats) || 0, accepted = opportunity ? acceptedFor(opportunity.id) : 0, after = accepted + 1, full = seats && accepted >= seats;
            const pct = seats ? Math.min(100, Math.round(after / seats * 100)) : 0;
            text('acceptSub', opportunity?.title || '');
            by('acceptSeats').innerHTML = '<div class="seat-person"><span class="avatar ' + tone(student?.id) + '">' + escape(initials(student?.name)) + '</span><div class="min-w-0"><strong>' + escape(student?.name || 'Student') + '</strong><small>' + escape([student?.universityId, student?.department].filter(Boolean).join(' · ')) + '</small></div></div>'
                + (seats ? '<div class="seat-row"><span>Seats after accepting</span><span><strong>' + Math.min(after, seats) + '</strong> of ' + seats + '</span></div><div class="progress-slim"><span class="' + (pct >= 100 ? 'success' : '') + '" style="width:' + pct + '%"></span></div>'
                    + (full ? '<div class="seat-note is-full"><i class="bi bi-exclamation-triangle"></i> All seats are already filled.</div>' : '<div class="seat-note"><i class="bi bi-check2"></i> ' + plural(Math.max(0, seats - after), 'seat') + ' will remain open.</div>') : '<div class="seat-row"><span>Seats</span><span>Not specified</span></div>');
            by('confirmAccept').disabled = !!full;
            open('acceptModal');
        };
        actions.reject = id => {
            selected = id; close('appDrawer', true);
            const application = view.applications.find(a => a.id === id);
            text('rejectSub', [studentOf(application)?.name, opportunityOf(application)?.title].filter(Boolean).join(' · '));
            set('rejectMessage', ''); text('rejectCount', '0 / 400'); by('rejectCount').classList.remove('near-limit');
            all('[name="rejectReason"]').forEach(el => el.checked = false); by('reasonError').hidden = true; by('messageError').hidden = true;
            open('rejectModal');
        };
        click('confirmAccept', () => { const name = studentOf(view.applications.find(a => a.id === selected))?.name; if (transact('decision', { id: selected, status: 'accepted', reason: 'Accepted after company review' }, 'acceptModal').ok) toast((name || 'Student') + ' accepted. They will be asked to confirm.'); });
        click('confirmReject', () => { const reason = document.querySelector('[name="rejectReason"]:checked')?.value, message = value('rejectMessage'); if (!reason || !message) { text('reasonError', reason ? '' : 'Choose a rejection reason.'); by('reasonError').hidden = !!reason; text('messageError', message ? '' : 'Write a message for the student.'); by('messageError').hidden = !!message; return; } if (transact('decision', { id: selected, status: 'rejected', reason: reason + ' — ' + message }, 'rejectModal').ok) toast('Application rejected. The student can read your reason.', 'neutral'); });
        on('rejectMessage', 'input', () => { const length = by('rejectMessage').value.length; text('rejectCount', length + ' / 400'); by('rejectCount').classList.toggle('near-limit', length >= 360); if (length) by('messageError').hidden = true; });
        all('[name="rejectReason"]').forEach(el => el.addEventListener('change', () => { by('reasonError').hidden = true; }, { signal }));
        all('#statusTabs [data-status]').forEach(el => el.addEventListener('click', () => { tab = el.dataset.status.toLowerCase(); all('#statusTabs button').forEach(button => { button.classList.toggle('active', button === el); button.setAttribute('aria-selected', String(button === el)); }); render(); }, { signal }));
        on('searchBox', 'input', render); on('oppFilter', 'change', render); on('sortBy', 'change', render);

        render();
        window.addEventListener('storage', event => { if (event.key === 'uims.demo.v1') { view = demo.view(); if (view && !view.user.mustChangePassword && view.user.role === document.body.dataset.demoRole) render(); else location.href = '../Auth/Login.aspx?demo=1'; } }, { signal });
        window.addEventListener('focus', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
        window.addEventListener('pageshow', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();