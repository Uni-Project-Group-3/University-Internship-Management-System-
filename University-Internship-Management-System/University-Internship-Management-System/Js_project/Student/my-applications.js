
/* Original-page demo bindings. Layout and server controls remain in the ASPX. */
(function () {
    'use strict';
    if (!window.UimsDemo?.enabled()) { location.replace('../Auth/Login.aspx?demo=1'); return; }
    let binding;
    function initialize() {
        binding?.abort();
        binding = new AbortController();
        const signal = binding.signal, demo = window.UimsDemo;
        let view = demo.view(), selected = null, render = () => {};
        if (!view || view.user.mustChangePassword || view.user.role !== document.body.dataset.demoRole) {
            location.href = '../Auth/Login.aspx?demo=1';
            return;
        }
        const by = id => document.getElementById(id) || document.querySelector('[id$="_' + id + '"]');
        const all = selector => [...document.querySelectorAll(selector)];
        const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
        const text = (id, value) => { const element = by(id); if (element) element.textContent = value ?? '—'; };
        const value = id => by(id)?.value?.trim() || '';
        const set = (id, value) => { if (by(id)) by(id).value = value ?? ''; };
        const options = (id, records, placeholder = 'All') => {
            const element = by(id); if (!element) return;
            const previous = element.value;
            element.innerHTML = '<option value="">' + escape(placeholder) + '</option>' + records.map(record => '<option value="' + escape(record.id) + '">' + escape(record.name) + '</option>').join('');
            if (records.some(record => record.id === previous)) element.value = previous;
        };
        const on = (id, type, handler) => by(id)?.addEventListener(type, handler, {signal});
        const click = (id, handler) => on(id, 'click', event => { event.preventDefault(); try { handler(event); } catch(exception) { error(exception,event.currentTarget.closest('dialog,.modal,.offcanvas')); } });
        const button = (label, action, id, className = 'row-action') => {
            const paths={edit:'M4 16v4h4L20 8l-4-4L4 16Z',delete:'M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13',reset:'M13 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10ZM8 12H2m2 0v4',status:'M12 2v10M6 5a9 9 0 1 0 12 0',assign:'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8M20 8v6m-3-3h6'};
            const icon=className.includes('action-btn')&&paths[action];
            const content=icon?'<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="'+paths[action]+'"/></svg>':escape(label);
            return '<button type="button" title="'+escape(label)+'" aria-label="'+escape(label)+'" class="' + className + '" data-uims="' + action + '" data-record="' + escape(id) + '">' + content + '</button>';
        };
        const cells = values => '<tr>' + values.map(cell => '<td>' + cell + '</td>').join('') + '</tr>';
        const fill = (selector, records, row) => {
            const element = document.querySelector(selector);
            if(!element)return;
            const emptyState=element.id==='taskList'?element.querySelector('#noTasksMsg'):null;
            element.innerHTML=records.map(row).join('');
            if(emptyState)element.append(emptyState);
        };
        const messageElement = () => by('uimsPageNotice') || document.querySelector('main .preview-note,main .completion-notice,main .companies-notice,main .evaluation-notice,main .monitor-notice,main .workspace-note,#messagesAvailability,#traineesAvailability,#accountAvailability') || by('pageNotice') || by('profileMessage') || by('pnlMessage');
        function notice(message) {
            let element = messageElement();
            if (!element) { element = document.createElement('p'); element.id='uimsPageNotice'; element.className = 'uims-feedback'; element.setAttribute('role','status'); (document.querySelector('main') || document.querySelector('.content')).prepend(element); }
            element.hidden = false; element.textContent = message;
        }
        function error(error, scope) {
            let element = scope?.querySelector('[data-uims-error]');
            if (scope && !element) { element = document.createElement('p'); element.dataset.uimsError = ''; element.className = 'uims-error'; element.setAttribute('role','alert'); (scope.querySelector('.modal-body') || scope).append(element); }
            if (element) element.textContent = error.message; else notice(error.message);
        }
        const dirty = new WeakSet(), openers = new WeakMap();
        let beforeClose=null;
        function open(id) {
            const dialog = by(id); if (!dialog) throw Error('Missing original popup: ' + id);
            const alreadyOpen=dialog.tagName==='DIALOG'?dialog.open:dialog.classList.contains('show');
            if(!alreadyOpen){openers.set(dialog, document.activeElement);dirty.delete(dialog);}
            dialog.querySelectorAll('[data-uims-error],.uims-discard').forEach(el => el.remove());
            if (dialog.tagName === 'DIALOG') { if (!dialog.open) dialog.showModal(); }
            else if (dialog.classList.contains('offcanvas')) bootstrap.Offcanvas.getOrCreateInstance(dialog).show();
            else bootstrap.Modal.getOrCreateInstance(dialog).show();
            dialog.querySelector('input:not([readonly]):not([disabled]),textarea:not([disabled]),select:not([disabled])')?.focus();
        }
        function close(id, force = false) {
            const dialog = by(id); if (!dialog) return;
            if(!force&&beforeClose?.(id)===false)return;
            if (dirty.has(dialog) && !force) {
                if (dialog.querySelector('.uims-discard')) return;
                const prompt = document.createElement('div'); prompt.className = 'uims-discard'; prompt.setAttribute('role','alert');
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
                render(); notice('Saved in local demo storage. No Firebase operation or email was sent.');
                return {ok:true, result};
            } catch (exception) { error(exception, popup ? by(popup) : null); return {ok:false}; }
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
        }, {signal});
        all('dialog,.modal,.offcanvas').forEach(dialog => {
            const changed = () => dirty.add(dialog);
            dialog.addEventListener('input', changed, {signal}); dialog.addEventListener('change', changed, {signal});
            dialog.addEventListener('cancel', event => { event.preventDefault(); close(dialog.id); }, {signal});
            dialog.addEventListener('hide.bs.modal', event => { if (dirty.has(dialog)) { event.preventDefault(); close(dialog.id); } }, {signal});
            dialog.addEventListener('hidden.bs.modal', () => openers.get(dialog)?.focus(), {signal});
            dialog.addEventListener('click', event => {
                if(event.defaultPrevented)return;
                if (event.target.closest('[data-bs-dismiss], [data-close], [data-completion-close], [data-profile-cancel], .popup-close, .monitor-close, .evaluation-close, .companies-close, .placements-close, [data-close-exam]')) {
                    event.preventDefault(); event.stopPropagation(); close(dialog.id);
                } else if (event.target === dialog && dialog.tagName === 'DIALOG') {
                    const rect = dialog.getBoundingClientRect();
                    if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) close(dialog.id);
                }
            }, {signal});
        });
        function detail(student, id, selector) {
            const progress = view.progress.find(item => item.id === student); if (!progress) throw Error('This student is not available to this demo identity.');
            const target = document.querySelector(selector);
            if (target) target.innerHTML = '<h3>' + escape(progress.name) + '</h3><dl>' + [['University ID',progress.universityId],['Department',progress.department],['Company',progress.company?.name],['Company Supervisor',progress.companySupervisor?.name],['Academic Supervisor',progress.supervisor?.name],['Training status',progress.placement?.completionStatus || 'Not placed'],['Approved / Required hours',progress.approvedHours + ' / ' + progress.requiredHours],['Tasks completed',progress.completedTasks + ' / ' + progress.tasks.length],['Risk indicators',progress.risks.join(', ') || 'None'],['Confirmed at UTC',progress.placement?.studentConfirmedAt],['Disable deadline UTC',progress.placement?.accountDisableAt],['Certificate',progress.placement?.certificateId]].map(([label,content]) => '<dt>' + escape(label) + '</dt><dd>' + escape(content || '—') + '</dd>').join('') + '</dl>' + progress.tasks.map(task => '<article><h4>' + escape(task.title) + '</h4><p>' + escape(task.description) + '</p><p>' + escape(task.deadline) + ' · ' + escape(task.status) + '</p><p>' + escape(task.feedback) + '</p>' + view.submissions.filter(sub => sub.taskId === task.id).map(sub => '<p>Submitted ' + escape(sub.createdAt) + ': ' + escape(sub.description) + ' · ' + escape(sub.fileName) + '</p>').join('') + '</article>').join('') + progress.evaluations.map(evaluation => '<article><h4>' + escape(evaluation.kind) + ' evaluation · ' + escape(evaluation.status) + '</h4><p>' + escape(evaluation.comments) + '</p></article>').join('');
            selected = student; open(id);
        }
        function shell() {
            all('[id$="lblUserName"],[id$="NavName"]').forEach(el => el.textContent = view.user.name);
            document.addEventListener('click', event => {
                const element = event.target.closest('a,button,input');
                if (element && (/log\s*out/i.test(element.textContent) || /logout/i.test(element.id))) {
                    event.preventDefault(); event.stopImmediatePropagation(); demo.logout(); location.href = '../Auth/Login.aspx?demo=1';
                }
            }, {signal,capture:true});
            all('[id*=Logout],[id*=logout]').forEach(el => el.disabled = false);
            if (!document.querySelector('script[src*="dashboard-navigation.js"]')) {
                const sidebar = document.querySelector('aside.sidebar,aside.dash-sidebar,aside[id$="Sidebar"]');
                const toggle = document.querySelector('.menu-btn,.dash-menu-btn');
                const main = document.querySelector('.main,.dash-main');
                const media = matchMedia('(max-width:991.98px)');
                const setOpen = show => {
                    const active = media.matches && show;
                    document.body.classList.toggle('sidebar-open',active); toggle?.setAttribute('aria-expanded',String(active));
                    if (sidebar) sidebar.inert = media.matches && !active;
                    if (main) main.inert = active;
                    document.querySelector('.sidebar-backdrop,.dash-sidebar-backdrop')?.toggleAttribute('hidden',!active);
                    if (active) sidebar?.querySelector('button,a[href]')?.focus();
                };
                window.toggleSidebar = () => setOpen(!document.body.classList.contains('sidebar-open'));
                if (toggle) { toggle.onclick = null; toggle.addEventListener('click',window.toggleSidebar,{signal}); }
                all('.sidebar-close,.dash-sidebar-close,.legacy-sidebar-close,.sidebar-backdrop,.dash-sidebar-backdrop').forEach(el => el.addEventListener('click',()=>{setOpen(false);toggle?.focus();},{signal}));
                document.addEventListener('keydown',event=>{if(event.key==='Escape'&&document.body.classList.contains('sidebar-open')){setOpen(false);toggle?.focus();}},{signal});
                media.addEventListener('change',()=>setOpen(false),{signal}); setOpen(false);
            }
            const topSearch = document.querySelector('.topbar input,.dash-topbar input');
            const search = document.querySelector('main input[type="search"],.content input[type="search"],#taskSearch,#searchInput');
            if(topSearch&&!search){topSearch.disabled=false;topSearch.addEventListener('keydown',event=>{if(event.key!=='Enter')return;event.preventDefault();const route={admin:'MonitorPlacementsProgress.aspx',student:'ExploreOpportunities.aspx',company:'MyTrainees.aspx',academic:'StudentProgressMonitor.aspx'}[view.user.role];location.href=route+'?search='+encodeURIComponent(topSearch.value.trim());},{signal});}
            if (topSearch && search && topSearch !== search) { topSearch.disabled=false;topSearch.addEventListener('input',()=>{search.value=topSearch.value;search.dispatchEvent(new Event('input'));},{signal}); }
        }

        function notifications() {
            const items=[];
            if(view.user.role==='student'){view.applications.forEach(a=>items.push('Application: '+a.status));view.placements.filter(p=>p.completionStatus==='PendingStudentConfirmation').forEach(()=>items.push('Coordinator approval received. Confirm completion in the Student Dashboard.'));}
            if(view.user.role==='company')items.push(view.applications.filter(a=>a.status==='pending').length+' pending applications',view.tasks.filter(t=>t.status==='submitted').length+' task submissions awaiting review');
            if(view.user.role==='academic')items.push(view.progress.filter(s=>s.evaluations.some(e=>e.kind==='company'&&e.status==='submitted')&&!s.evaluations.some(e=>e.kind==='academic'&&e.status==='submitted')).length+' assigned students awaiting academic evaluation');
            if(view.user.role==='admin')items.push(view.messages.filter(m=>m.status==='unread').length+' unread contact messages',view.progress.filter(s=>s.placement?.completionStatus==='PendingStudentConfirmation').length+' pending student confirmations');
            let dialog=by('originalNotifications');
            if(!dialog){dialog=document.createElement('dialog');dialog.id='originalNotifications';dialog.className='uims-notifications';dialog.setAttribute('aria-label','Demo notifications');document.forms[0].append(dialog);}
            dialog.innerHTML='<h2>Notifications · local demo</h2>'+items.map(item=>'<p>'+escape(item)+'</p>').join('')+(items.length?'':'<p>No notifications.</p>')+'<button type="button" data-notification-close>Close</button>';
            dialog.querySelector('button').onclick=()=>dialog.close();dialog.showModal();
        }
        all('.topbar button,.dash-topbar button').forEach(el=>{if(!/notification/i.test(el.getAttribute('aria-label')||'')&&!el.querySelector('.bi-bell'))return;el.disabled=false;el.title='Local demo notifications';el.addEventListener('click',event=>{event.preventDefault();event.stopImmediatePropagation();view=demo.view();if(view)notifications();},{signal,capture:true});});
        shell();
        
        options('statusFilter',['pending','accepted','rejected','confirmed'].map(id=>({id,name:id[0].toUpperCase()+id.slice(1)})));
        render=()=>{
            const records=view.applications.filter(application=>!value('statusFilter')||application.status===value('statusFilter'));
            fill('.application-list',records,application=>{const opportunity=view.opportunities.find(o=>o.id===application.opportunityId);return '<article class="application-card" data-id="'+escape(application.id)+'"><div class="application-top"><div><h3>'+escape(opportunity?.title||'Internship')+'</h3><p class="company-name">'+escape(view.companies.find(c=>c.id===application.companyId)?.name)+'</p></div><span class="status-badge">'+escape(application.status)+'</span></div><div class="application-meta">Applied '+escape(application.createdAt)+'</div><p class="decision-note">'+escape(application.reason||'Awaiting company review')+'</p><div class="application-actions">'+button('View details','details',application.id,'details-button')+(application.status==='accepted'&&!view.placements.length?button('Confirm offer','confirm',application.id,'confirm-button'):'')+'</div></article>';});
            text('resultCount','('+records.length+')');by('emptyState').hidden=records.length>0;
            all('.summary-card strong,.summary-card .summary-value').forEach((el,index)=>el.textContent=[view.applications.length,view.applications.filter(a=>a.status==='pending').length,view.applications.filter(a=>['accepted','confirmed'].includes(a.status)).length,view.applications.filter(a=>a.status==='rejected').length][index]??0);
        };
        actions.details=id=>{selected=id;const application=view.applications.find(a=>a.id===id);text('detailsTitle',view.opportunities.find(o=>o.id===application.opportunityId)?.title||'Internship');text('detailsCompany',view.companies.find(c=>c.id===application.companyId)?.name);text('detailsStatus',application.status);text('detailsDecision',application.reason||'Awaiting review');text('detailsMeta','Submitted '+application.createdAt+' · '+application.motivation);open('applicationModal');};
        actions.confirm=id=>{selected=id;const application=view.applications.find(a=>a.id===id);text('selectedOffer',view.opportunities.find(o=>o.id===application.opportunityId)?.title+' at '+view.companies.find(c=>c.id===application.companyId)?.name);open('confirmModal');};
        click('confirmOffer',()=>transact('confirmOffer',{id:selected},'confirmModal'));on('statusFilter','change',render);

        render();
        notice('DEMO — original interface connected to local browser storage. No Firebase or real authentication.');
        window.addEventListener('storage', event => { if (event.key === 'uims.demo.v1') { view=demo.view(); if(view&&!view.user.mustChangePassword&&view.user.role===document.body.dataset.demoRole)render();else location.href='../Auth/Login.aspx?demo=1'; } }, {signal});
        window.addEventListener('focus',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx?demo=1';},{signal});
        window.addEventListener('pageshow',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx?demo=1';},{signal});
    }
    if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',initialize,{once:true});else initialize();
    if(window.Sys?.WebForms?.PageRequestManager)Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();
