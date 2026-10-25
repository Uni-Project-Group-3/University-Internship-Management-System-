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
            window.showApplicationMessage?.(message);
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


        // ================= Application Form page =================
        const opportunityId = new URLSearchParams(location.search).get('opportunity');
        const opportunity = view.opportunities.find(o => o.id === opportunityId);
        const company = opportunity ? view.companies.find(c => c.id === opportunity.companyId) : null;
        const initials = name => String(name || '').split(/\s+/).filter(Boolean).slice(0, 2).map(word => word[0].toUpperCase()).join('') || 'CO';
        const formatDate = s => { if (!s) return '—'; const d = new Date(String(s).slice(0, 10) + 'T00:00:00'); return isNaN(d) ? String(s) : d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }); };
        const alreadyApplied = !!opportunity && (view.applications || []).some(a => a.opportunityId === opportunityId && (!a.studentId || a.studentId === view.user.id || a.studentId === view.user.universityId || a.studentId === view.user.studentId));
        if (by('previewNotice')) by('previewNotice').hidden = true;
        text('submitLabel', 'Submit application');
        by('applicationFields').disabled = !opportunity || alreadyApplied; by('invalidOpportunity').hidden = !!opportunity;
        if (by('alreadyApplied')) by('alreadyApplied').hidden = !alreadyApplied;
        if (opportunity) {
            const name = company?.name || opportunity.companyName || '';
            text('opportunityHeading', opportunity.title); text('companyName', name); text('opportunityLocation', [opportunity.location, opportunity.workMode].filter(Boolean).join(' · ') || '—'); text('opportunityMajor', opportunity.department);
            const weeks = opportunity.duration || opportunity.durationWeeks;
            text('opportunityDuration', [weeks ? weeks + ' weeks' : '', opportunity.requiredHours ? opportunity.requiredHours + ' hours' : ''].filter(Boolean).join(' · ') || '—');
            text('opportunityDeadline', formatDate(opportunity.deadline));
            const mark = by('opportunityMark'); if (mark && name) mark.textContent = initials(name);
        }
        options('major', [{ id: view.user.department, name: view.user.department }], 'Your department'); set('major', view.user.department);
        set('fullName', view.user.name); set('studentNumber', view.user.universityId); set('email', view.user.email); set('phone', view.user.phone);
        ['fullName', 'studentNumber', 'email'].forEach(id => by(id).readOnly = true);
        on('motivation', 'input', () => { const length = by('motivation').value.length; text('characterCount', length + ' / 1000'); by('characterCount').classList.toggle('near-limit', length >= 900); });
        on('applicationForm', 'submit', async event => {
            event.preventDefault(); if (!opportunity || alreadyApplied || !valid(['fullName', 'studentNumber', 'email', 'phone', 'major', 'cvAttachment', 'portfolioLink', 'motivation', 'confirmInformation'])) return;
            const useProfileCv=by('cvAttachment').dataset.profileCv==='true',cvFile=by('cvAttachment').files[0];if(!useProfileCv&&(!cvFile||cvFile.size>10*1024*1024||!/\.(pdf|docx?)$/i.test(cvFile.name))){notice('Choose a PDF, DOC or DOCX CV up to 10 MiB.');return;}
            if(by('applicationFields').dataset.submitting)return;by('applicationFields').dataset.submitting='true';
            try{const upload=new FormData();if(!useProfileCv)upload.append('cv',cvFile);const response=await fetch('../ApplicationCv.ashx',{method:'POST',credentials:'same-origin',headers:{'X-UIMS-Attachment':useProfileCv?'profile':'upload'},body:upload,signal:AbortSignal.timeout(60000)});const reply=await response.json();if(!response.ok||!reply.attachmentId)throw Error(reply.error||'CV upload failed.');set('cvAttachmentId',reply.attachmentId);set('cvLink',reply.fileName);}catch(e){notice(e.message);delete by('applicationFields').dataset.submitting;return;}
            delete by('applicationFields').dataset.submitting;
            by('applicationFields').querySelectorAll('[data-uims-error]').forEach(el => el.remove());
            const result = transact('apply', { opportunityId, motivation: value('motivation'), phone: value('phone'), cvLink: value('cvLink'), cvAttachmentId: value('cvAttachmentId'), portfolioLink: value('portfolioLink') });
            if (!result.ok) { const message = by('uimsPageNotice')?.textContent; if (message) { const box = document.createElement('p'); box.dataset.uimsError = ''; box.className = 'uims-error'; box.setAttribute('role', 'alert'); box.textContent = message; by('applicationFields').querySelector('.review-section .form-actions')?.before(box); } return; }
            by('applicationFields').disabled = true; by('applicationFields').hidden = true; by('previewResult').hidden = false;
            const heading = by('previewResult').querySelector('h2'); if (heading) heading.textContent = 'Application submitted';
            text('previewSummary', 'Your application to ' + (opportunity.title || 'this opportunity') + (company?.name ? ' at ' + company.name : '') + ' was submitted. The company can now review your application and attached CV.');
            by('previewResult').querySelector('p:not([id])').textContent = 'Your CV is stored privately and is available to the receiving company.';
            by('editApplication').textContent = 'View My Applications';
            by('previewResult').focus(); window.scrollTo({ top: 0, behavior: 'smooth' });
        });
        click('editApplication', () => location.href = 'MyApplications.aspx');

        render();
        window.dispatchEvent(new Event('uims:form-filled'));
        window.addEventListener('storage', event => { if (event.key === 'uims.demo.v1') { view = demo.view(); if (view && !view.user.mustChangePassword && view.user.role === document.body.dataset.demoRole) render(); else location.href = '../Auth/Login.aspx?demo=1'; } }, { signal });
        window.addEventListener('focus', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
        window.addEventListener('pageshow', () => { const current = demo.current(); if (!current || current.mustChangePassword) location.href = '../Auth/Login.aspx?demo=1'; }, { signal });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true }); else initialize();
    if (window.Sys?.WebForms?.PageRequestManager) Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();

/* Form progress, checklist and character counter. Display only: it reads the fields and never
   submits, saves or navigates, so it is safe with or without the demo store. */
(function() {
    'use strict';
    function start() {
        const $ = id => document.getElementById(id);
        window.showApplicationMessage = message => {
            let dialog = $('applicationMessage');
            if (!dialog) {
                dialog = document.createElement('dialog'); dialog.id = 'applicationMessage';
                dialog.className = 'application-message'; dialog.setAttribute('aria-labelledby', 'applicationMessageTitle');
                dialog.innerHTML = '<h2 id="applicationMessageTitle">Application notice</h2><p></p><button type="button">OK</button>';
                dialog.querySelector('button').onclick = () => dialog.close(); document.body.append(dialog);
            }
            dialog.querySelector('p').textContent = message;
            if (!dialog.open) dialog.showModal();
            dialog.querySelector('button').focus();
        };
        const cv = $('cvAttachment');
        if (cv) {
            const wrapper = cv.closest('.input-icon'); wrapper?.classList.add('cv-file-picker');
            wrapper?.querySelector('i')?.remove();
            cv.classList.add('cv-native-input');
            const choose = document.createElement('button'); choose.type = 'button'; choose.className = 'cv-choose-button';
            choose.textContent = 'Attach CV file'; choose.onclick = () => cv.click();
            const name = document.createElement('span'); name.className = 'cv-selected-name'; name.setAttribute('aria-live', 'polite');
            cv.before(choose); cv.after(name);
            cv.addEventListener('change', () => { name.textContent = cv.files[0]?.name || ''; choose.textContent = cv.files.length ? 'Change CV file' : 'Attach CV file'; });
            // A CV saved in the student's profile is attached automatically; the file picker becomes optional.
            fetch('../ApplicationCv.ashx?profileCv=1', { credentials: 'same-origin', cache: 'no-store' }).then(r => r.ok ? r.json() : null).then(info => {
                if (!info?.hasCv) return;
                const card = document.createElement('div'); card.className = 'cv-profile-card'; card.setAttribute('role', 'status');
                const icon = document.createElement('span'); icon.className = 'cv-profile-icon'; icon.setAttribute('aria-hidden', 'true'); icon.textContent = 'CV';
                const textBox = document.createElement('div'), title = document.createElement('strong'), file = document.createElement('span'), hint = document.createElement('small');
                title.textContent = 'Your profile CV'; file.textContent = info.fileName; hint.textContent = 'Attached automatically from your profile.';
                textBox.append(title, file, hint);
                const swap = document.createElement('button'); swap.type = 'button'; swap.className = 'cv-profile-swap';
                const useProfile = on => { cv.dataset.profileCv = String(on); cv.required = !on; wrapper.hidden = on; card.classList.toggle('is-off', !on); swap.textContent = on ? 'Use a different file' : 'Use my profile CV'; hint.textContent = on ? 'Attached automatically from your profile.' : 'Not attached. The file you choose below is used instead.'; if (on) { cv.value = ''; name.textContent = ''; choose.textContent = 'Attach CV file'; } cv.dispatchEvent(new Event('change', { bubbles: true })); };
                swap.onclick = () => useProfile(cv.dataset.profileCv !== 'true');
                card.append(icon, textBox, swap); wrapper.before(card); useProfile(true);
            }).catch(() => { /* Without the profile check the student attaches a file as before. */ });
        }
        const ok = id => { const el = $(id); return !!el && el.value.trim() !== '' && el.checkValidity(); };
        const steps = {
            personal: () => ['fullName', 'studentNumber', 'email', 'phone', 'major'].every(ok),
            supporting: () => ($('cvAttachment')?.dataset.profileCv === 'true' || ok('cvAttachment')) && ($('portfolioLink')?.checkValidity() ?? true),
            review: () => !!$('confirmInformation')?.checked
        };
        const titles = ['Let’s get started', 'Good start', 'Almost there', 'Ready to submit'];
        const fields = $('applicationFields');
        function update() {
            let done = 0;
            const locked = !!fields?.disabled && !!$('previewResult')?.hidden;
            for (const [key, test] of Object.entries(steps)) {
                const complete = !locked && test(); if (complete) done++;
                document.querySelector('.form-section[data-step="' + key + '"]')?.classList.toggle('is-complete', complete);
                document.querySelector('#checklist [data-check="' + key + '"]')?.classList.toggle('is-done', complete);
            }
            const pct = Math.round(done / 3 * 100);
            $('progressRing')?.style.setProperty('--value', pct);
            if ($('progressPercent')) $('progressPercent').textContent = pct + '%';
            if ($('progressTitle')) $('progressTitle').textContent = titles[done];
            if ($('progressText')) $('progressText').textContent = done + ' of 3 steps complete';
            const counter = $('characterCount'), motivation = $('motivation');
            if (counter && motivation) { counter.textContent = motivation.value.length + ' / 1000'; counter.classList.toggle('near-limit', motivation.value.length >= 900); }
        }
        fields?.addEventListener('input', update);
        fields?.addEventListener('change', update);
        fields?.addEventListener('focusout', event => { if (event.target.matches?.('.form-control,.form-select')) event.target.classList.add('is-touched'); });
        window.addEventListener('uims:form-filled', update);
        update();
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
})();
