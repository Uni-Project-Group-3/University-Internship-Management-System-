
/* Original-page demo bindings. Layout and server controls remain in the ASPX. */
(function () {
    'use strict';
    if (!window.UimsLive?.enabled()) return;
    let binding;
    function initialize() {
        binding?.abort();
        binding = new AbortController();
        const signal = binding.signal, demo = window.UimsLive;
        let view = demo.view(), selected = null, render = () => {};
        if (!view || view.user.mustChangePassword || view.user.role !== document.body.dataset.portal) {
            location.href = '../Auth/Login.aspx';
            return;
        }
        const by = id => document.getElementById(id) || document.querySelector('[id$="_' + id + '"]');
        const all = selector => [...document.querySelectorAll(selector)];
        const displayValue=value=>typeof value==='string'&&/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(value)?new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'short',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).format(new Date(value))+' Jordan Time (UTC+3)':value;
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
            if (target) target.innerHTML = '<h3>' + escape(progress.name) + '</h3><dl>' + [['University ID',progress.universityId],['Department',progress.department],['Company',progress.company?.name],['Company Supervisor',progress.companySupervisor?.name],['Academic Supervisor',progress.supervisor?.name],['Training status',progress.placement?.completionStatus || 'Not placed'],['Approved / Required hours',progress.approvedHours + ' / ' + progress.requiredHours],['Tasks completed',progress.completedTasks + ' / ' + progress.tasks.length],['Risk indicators',progress.risks.join(', ') || 'None'],['Confirmed at (Jordan Time)',progress.placement?.studentConfirmedAt],['Disable deadline (Jordan Time)',progress.placement?.accountDisableAt],['Certificate',progress.placement?.certificateId]].map(([label,content]) => '<dt>' + escape(label) + '</dt><dd>' + escape(displayValue(content) || '—') + '</dd>').join('') + '</dl>' + progress.tasks.map(task => '<article><h4>' + escape(task.title) + '</h4><p>' + escape(task.description) + '</p><p>' + escape(displayValue(task.deadline)) + ' · ' + escape(task.status) + '</p><p>' + escape(task.feedback) + '</p>' + view.submissions.filter(sub => sub.taskId === task.id).map(sub => '<p>Submitted ' + escape(displayValue(sub.createdAt)) + ': ' + escape(sub.description) + ' · ' + escape(sub.fileName) + '</p>').join('') + '</article>').join('') + progress.evaluations.map(evaluation => '<article><h4>' + escape(evaluation.kind) + ' evaluation · ' + escape(evaluation.status) + '</h4><p>' + escape(evaluation.comments) + '</p></article>').join('');
            selected = student; open(id);
        }
        function shell() {
            all('[id$="lblUserName"],[id$="NavName"]').forEach(el => el.textContent = view.user.name);
            document.addEventListener('click', event => {
                const element = event.target.closest('a,button,input');
                if (element && (/log\s*out/i.test(element.textContent) || /logout/i.test(element.id))) {
                    event.preventDefault(); event.stopImmediatePropagation(); demo.logout(); location.href = '../Auth/Login.aspx';
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
        
        let tab='all';
        by('majorPicker').innerHTML=demo.departments.map(department=>'<label class="major-chip"><input type="radio" name="demoOpportunityDepartment" value="'+escape(department)+'">'+escape(department)+'</label>').join('');
        render=()=>{
            const records=view.opportunities.filter(o=>(tab==='all'||o.status===tab)&&(!value('searchBox')||o.title.toLowerCase().includes(value('searchBox').toLowerCase())));
            records.sort((a,b)=>value('sortBy')==='title'?a.title.localeCompare(b.title):value('sortBy')==='deadline'?String(a.deadline||'9999').localeCompare(String(b.deadline||'9999')):value('sortBy')==='applicants'?view.applications.filter(x=>x.opportunityId===b.id).length-view.applications.filter(x=>x.opportunityId===a.id).length:(Date.parse(b.createdAt)||0)-(Date.parse(a.createdAt)||0));
            fill('#oppGrid',records,o=>'<div class="col-md-6 col-xl-4"><article class="opp-card"><div class="opp-top"><span class="status">'+escape(o.status)+'</span></div><h3 class="opp-title">'+escape(o.title)+'</h3><p class="opp-desc">'+escape(o.description||'')+'</p><p>'+escape(o.department)+' · '+escape(o.location)+' · '+escape(o.workMode||'On-site')+'</p><p>'+view.applications.filter(a=>a.opportunityId===o.id).length+' applications · '+escape(o.seats||'Not specified')+' seats</p><div class="opp-actions">'+button('Edit','edit',o.id,'btn-ghost')+(o.status==='open'?button('Close opportunity','close',o.id,'btn-ghost'):'')+'</div></article></div>');
            text('statTotal',view.opportunities.length);text('statOpen',view.opportunities.filter(o=>o.status==='open').length);text('statSeats',view.opportunities.filter(o=>o.status==='open').reduce((sum,o)=>sum+(Number(o.seats)||0),0));text('statApplicants',view.applications.length);by('emptyState').hidden=records.length>0;
        };
        function editor(id){selected=id;const o=view.opportunities.find(o=>o.id===id);for(const [field,key]of Object.entries({oppTitle:'title',oppDescription:'description',oppRequirements:'requirements',oppLocation:'location',oppSeats:'seats',oppDeadline:'deadline',oppStart:'start',oppDuration:'duration',oppRequiredHours:'requiredHours'}))set(field,o?.[key]??({oppSeats:1,oppDuration:8,oppRequiredHours:160}[field]||''));all('[name="demoOpportunityDepartment"]').forEach(el=>el.checked=el.value===(o?.department||demo.departments[0]));all('[name="workMode"]').forEach(el=>el.checked=el.value===(o?.workMode||'On-site'));text('oppModalTitle',id?'Edit opportunity':'Create opportunity');open('oppModal');preview();}
        function preview(){text('pvTitle',value('oppTitle'));text('pvDesc',value('oppDescription'));text('pvLocation',value('oppLocation'));text('pvMode',document.querySelector('[name="workMode"]:checked')?.value);text('pvDuration',value('oppDuration')+' weeks');text('pvMajors',document.querySelector('[name="demoOpportunityDepartment"]:checked')?.value);text('pvSeats',value('oppSeats'));}
        all('[data-action="new"]').forEach(el=>el.addEventListener('click',()=>editor(null),{signal}));actions.edit=editor;
        all('#oppModal input,#oppModal textarea').forEach(el=>el.addEventListener('input',preview,{signal}));
        all('[data-step]').forEach(el=>el.addEventListener('click',()=>{const input=by(el.dataset.target);input.value=Math.min(Number(input.max),Math.max(Number(input.min),Number(input.value)+Number(el.dataset.step)));preview();},{signal}));
        click('saveOpp',()=>{if(!valid(['oppTitle','oppDescription','oppSeats','oppLocation','oppDeadline','oppStart','oppDuration','oppRequiredHours'],'oppModal'))return;transact('opportunity',{id:selected,title:value('oppTitle'),description:value('oppDescription'),requirements:value('oppRequirements'),department:document.querySelector('[name="demoOpportunityDepartment"]:checked')?.value,location:value('oppLocation'),requiredHours:value('oppRequiredHours'),seats:value('oppSeats'),deadline:value('oppDeadline'),start:value('oppStart'),duration:value('oppDuration'),workMode:document.querySelector('[name="workMode"]:checked')?.value},'oppModal');});
        actions.close=id=>{selected=id;text('closeSub',view.opportunities.find(o=>o.id===id)?.title);open('closeModal');};click('confirmClose',()=>transact('closeOpportunity',{id:selected},'closeModal'));
        all('#statusTabs [data-status]').forEach(el=>el.addEventListener('click',()=>{tab=el.dataset.status.toLowerCase();all('#statusTabs button').forEach(button=>button.classList.toggle('active',button===el));render();},{signal}));on('searchBox','input',render);on('sortBy','change',render);

        ['oppTitle','oppDescription','oppSeats','oppLocation','oppDeadline','oppStart','oppDuration','oppRequiredHours'].forEach(id=>by(id).required=true);

        render();
        notice('Live data integration is pending. No local fallback is enabled.');
        window.addEventListener('focus',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx';},{signal});
        window.addEventListener('pageshow',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx';},{signal});
    }
    if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',initialize,{once:true});else initialize();
    if(window.Sys?.WebForms?.PageRequestManager)Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();

/* Live role records: original page containers, server-verified identity, no local fallback. */
(() => {
'use strict';
const config={"page":"Company/ManageOpportunities","js":"Company/manage-opportunities","css":"company/manage-opportunities","source":"opportunities","table":"#oppGrid","search":"searchBox","filters":{},"sort":"sortBy","cards":true,"cells":["title","description","location","status"],"stats":{"statTotal":"total","statOpen":"published","statSeats":"capacity","statApplicants":"applications"}};
let binding;
async function init(){
 if(!window.UimsTab)return;
 binding?.abort(); binding=new AbortController();const signal=binding.signal;
 const by=id=>document.getElementById(id),all=s=>[...document.querySelectorAll(s)];
 const host=document.querySelector(config.table);if(!host)return;
 const escape=v=>String(v??'Not recorded').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const text=(id,v)=>{if(by(id))by(id).textContent=v;};
 const date=v=>{if(!v)return 'Not recorded';if(/^\d{4}-\d{2}-\d{2}$/.test(v))return v;const d=new Date(v);return Number.isNaN(+d)?'Not recorded':new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'short',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).format(d)+' Jordan Time (UTC+3)';};
 let notice=by('pageNotice')||document.querySelector('[data-role-live-status]');if(!notice){notice=document.createElement('p');notice.dataset.roleLiveStatus='';notice.className='role-live-status';notice.setAttribute('role','status');document.querySelector('main,.content,.app-content')?.prepend(notice);}
 notice.hidden=false;notice.textContent='Loading authorized Firebase records…';
 const blocked='This operation requires an authorized live write integration. No changes have been made.';
 // Existing management controls remain visible with an explicit unavailable state.
 all('button').filter(b=>!b.closest('.sidebar,.dash-sidebar,.topbar')&&!/close|menu|logout|previous|next|reset|clear/i.test(b.id+' '+b.getAttribute('aria-label')+' '+b.textContent)).forEach(b=>{b.disabled=true;b.title=blocked;});
 try{
 const data=await UimsTab.request('roleData',{}, {page:config.page});
 if(signal.aborted)return;
 const ready=k=>data.states[k]==='ready',rows=k=>ready(k)?data[k]:[];
 if(!ready(config.source))throw Error('Firebase did not permit the required records. No demonstration records are shown.');
 text('lblUserName',data.user.name);text('lblInitials',data.user.name.split(/\s+/).slice(0,2).map(x=>x[0]).join(''));
 all('[id*=Logout],[id*=logout]').forEach(b=>{b.disabled=false;b.removeAttribute('title');});
 const placement=r=>config.source==='placements'?r:rows('placements').find(p=>p.id===r.placementId&&p.studentId===r.id);
 const opportunity=r=>rows('opportunities').find(o=>o.id===r.opportunityId);
 const tasks=r=>ready('tasks')?rows('tasks').filter(t=>t.placementId===placement(r)?.id):null;
 const hours=r=>{const p=placement(r);return p&&Number.isFinite(p.approvedHours)&&Number.isFinite(p.requiredHours)?p.approvedHours+' / '+p.requiredHours+' h':'Not recorded';};
 const value=(r,key)=>{
 if(key.startsWith('@date:'))return date(r[key.slice(6)]);
 const p=placement(r);
 switch(key){case '@student':return (r.fullName||r.studentId||r.id)+' · '+(r.studentNumber||r.id);
 case '@companyName':return r.name+' · '+(r.industry||'');
 case '@opportunity':return opportunity(r)?.title||r.opportunityId||'Not recorded';
 case '@period':return (r.startDate||'Not recorded')+' – '+(r.endDate||'Not recorded');
 case '@company':return rows('companies').find(c=>c.id===p?.companyId)?.name||p?.companyId||'Not placed';
 case '@status':return p?.status||(r.placementId&&!ready('placements')?'Unavailable':'not_placed');
 case '@hours':return hours(r);
 case '@tasks':{const t=tasks(r);return t?t.filter(x=>x.status==='reviewed').length+' reviewed / '+t.length:'Unavailable';}
 case '@risk':{const t=tasks(r);return t?t.some(x=>x.status!=='reviewed'&&x.dueDate&&x.dueDate<new Intl.DateTimeFormat('en-CA',{timeZone:'Asia/Amman',year:'numeric',month:'2-digit',day:'2-digit'}).format(new Date(data.serverUtc)))?'Overdue task':'No overdue tasks':'Not assessed';}
 case '@companySupervisor':return p?.companySupervisorId||'Not recorded';
 case '@confirmation':return 'Not available';case '@deadline':return 'Not available';case '@certificate':return 'Not available';case '@restricted':return 'Not authorized for this view';case '@supervisorNames':return ready('companySupervisors')?rows('companySupervisors').filter(s=>s.companyId===r.id).map(s=>s.fullName).join(', ')||'Not recorded':'Unavailable';case '@supervisorEmails':return ready('companySupervisors')?rows('companySupervisors').filter(s=>s.companyId===r.id).map(s=>s.email).join(', ')||'Not recorded':'Unavailable';case '@supervisorStatuses':return ready('companySupervisors')?rows('companySupervisors').filter(s=>s.companyId===r.id).map(s=>s.accountStatus||'Not recorded').join(', ')||'Not recorded':'Unavailable';
 default:return r[key]??'Not recorded';}
 };
 let active='',page=0;
 const filters=Object.entries(config.filters||{});
 filters.forEach(([id,key])=>{const el=by(id);if(!el)return;el.replaceChildren(new Option('All',''));[...new Set(rows(config.source).map(r=>String(value(r,key))))].sort().forEach(v=>el.add(new Option(v,v)));});
 all('fieldset').forEach(f=>f.disabled=false);all('#evaluationFilter,#evaluationStatus').forEach(el=>{el.disabled=true;el.title='Private evaluation status is not available in this read-only view.';});if(by('accountStatus')){by('accountStatus').disabled=true;by('accountStatus').title='Protected account status is not included in the public supervisor directory.';}all('#placementSort option[value=tasks],#placementSort option[value=risk]').forEach(el=>{el.disabled=true;el.title='This view has no authorized task or risk dataset.';});
 function filtered(){let list=rows(config.source).filter(r=>(!by(config.search)?.value||JSON.stringify(r).toLowerCase().includes(by(config.search).value.toLowerCase()))&&filters.every(([id,key])=>!by(id)?.value||String(value(r,key))===by(id).value));
 if(active&&active!=='all')list=list.filter(r=>active==='pending'?['submitted','under_review'].includes(r.status):active==='open'?r.status==='published':active==='closed'?r.status!=='published':r.status===active);
 const sort=by(config.sort)?.value||'';return list.sort((a,b)=>sort==='hours'?(placement(b)?.approvedHours??-1)-(placement(a)?.approvedHours??-1):sort==='applicants'?rows('applications').filter(r=>r.opportunityId===b.id).length-rows('applications').filter(r=>r.opportunityId===a.id).length:sort.includes('deadline')?String(a.applicationDeadline||'').localeCompare(String(b.applicationDeadline||'')):/newest|oldest/i.test(sort)?String(a.submittedAt||a.createdAt||a.publishedAt||'').localeCompare(String(b.submittedAt||b.createdAt||b.publishedAt||''))*(/newest/i.test(sort)?-1:1):String(a.fullName||a.name||a.title||a.studentId||a.id).localeCompare(String(b.fullName||b.name||b.title||b.studentId||b.id))*(sort.includes('desc')||sort.includes('Z to A')?-1:1));}
 let dialog=by('roleLiveDetails');if(!dialog){dialog=document.createElement('dialog');dialog.id='roleLiveDetails';dialog.className='role-live-details';dialog.setAttribute('aria-label','Record details');dialog.innerHTML='<header><h2>Record details</h2><button type="button" aria-label="Close record details">×</button></header><div></div>';document.forms[0].append(dialog);}
 dialog.querySelector('button').addEventListener('click',()=>dialog.close(),{signal});
 function details(r){const body=dialog.querySelector('div');body.replaceChildren();const dl=document.createElement('dl');Object.entries(r).forEach(([key,v])=>{const dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=key.replace(/([A-Z])/g,' $1');dd.textContent=/At$/.test(key)?date(v):Array.isArray(v)?v.join(', '):String(v);dl.append(dt,dd);});body.append(dl);dialog.showModal();dialog.querySelector('button').focus();}
 function render(){const list=filtered(),hasPaging=all('[aria-label="Next page"],#nextPlacements').length>0,size=hasPaging?(Number(by(config.pageSize)?.value)||10):Math.max(1,list.length);page=Math.min(page,Math.max(0,Math.ceil(list.length/size)-1));const visible=list.slice(page*size,(page+1)*size);
 host.innerHTML=visible.map(r=>config.cards?'<div class="col-md-6 col-xl-4"><article class="opp-card"><h3>'+escape(r.title)+'</h3><p>'+escape(r.description)+'</p><p>'+escape(r.location)+' · '+escape(r.status)+'</p><p>Apply by '+escape(date(r.applicationDeadline))+'</p><button type="button" data-live-record="'+escape(r.id)+'" class="row-action">View details</button> <button type="button" data-edit-opportunity="'+escape(r.id)+'" class="row-action">Edit</button></article></div>':'<tr>'+config.cells.map(k=>'<td>'+escape(value(r,k))+'</td>').join('')+'<td><button type="button" class="row-action" data-live-record="'+escape(r.id)+'">View details</button></td></tr>').join('')|| (config.cards?'<p>No matching records.</p>':'<tr><td colspan="'+(config.cells.length+1)+'">No matching records.</td></tr>');
 const result=list.length+' matching records · Page '+(page+1)+' of '+Math.max(1,Math.ceil(list.length/size));['resultsStatus','placementResults','recordCount'].forEach(id=>text(id,result));all('.trainees-card-footer > span,.messages-card-footer > span,.companies-pagination > span').forEach(el=>el.textContent=result);all('.trainees-pagination > span,.companies-pagination > div > span,.monitor-pagination > div > span').forEach(el=>{el.textContent='Page '+(page+1)+' / '+Math.max(1,Math.ceil(list.length/size));el.removeAttribute('aria-label');});
 all('[aria-label="Previous page"],#previousPlacements').forEach(b=>b.disabled=page===0);all('[aria-label="Next page"],#nextPlacements').forEach(b=>b.disabled=(page+1)*size>=list.length);
 if(by('emptyState'))by('emptyState').hidden=true;if(by('noResultsMsg'))by('noResultsMsg').hidden=true;all('.completion-empty,.companies-empty,.monitor-empty').filter(el=>!el.closest('dialog')).forEach(el=>el.hidden=true);
 }
 host.addEventListener('click',e=>{const b=e.target.closest('[data-live-record]');if(b){e.preventDefault();const record=rows(config.source).find(r=>r.id===b.dataset.liveRecord);if(record)details(record);}},{signal});
 [config.search,config.sort,config.pageSize,...filters.map(([id])=>id)].filter(Boolean).forEach(id=>by(id)?.addEventListener('input',()=>{page=0;render();},{signal}));
 all('[aria-label="Previous page"],#previousPlacements').forEach(b=>b.addEventListener('click',()=>{page--;render();},{signal}));all('[aria-label="Next page"],#nextPlacements').forEach(b=>b.addEventListener('click',()=>{page++;render();},{signal}));
 all('#statusTabs [data-status]').forEach(b=>{b.disabled=false;b.removeAttribute('title');b.addEventListener('click',()=>{active=b.dataset.status.toLowerCase();all('#statusTabs [data-status]').forEach(tab=>tab.classList.toggle('active',tab===b));page=0;render();},{signal});});
 all('#resetFilters,#clearPlacements,#resetCompanyFilters,#completionClearFilters,[data-clear-filters]').forEach(b=>{b.disabled=false;b.addEventListener('click',()=>{filters.forEach(([id])=>{if(by(id))by(id).value='';});if(by(config.search))by(config.search).value='';active='';page=0;render();},{signal});});
 const count=kind=>{const list=rows(config.source);switch(kind){case 'total':return list.length;case 'pending':return list.filter(r=>['submitted','under_review'].includes(r.status)).length;case 'capacity':return ready('opportunities')?rows('opportunities').reduce((n,r)=>n+(Number(r.capacity)||0),0):'Unavailable';case 'applications':return ready('applications')?rows('applications').length:'Unavailable';case 'active':case 'completed':return ready('placements')?rows('placements').filter(r=>r.status===kind).length:'Unavailable';default:return list.filter(r=>r.status===kind).length;}};
 Object.entries(config.stats||{}).forEach(([id,kind])=>text(id,count(kind)));
 all('[data-count]').forEach(el=>{const key=el.dataset.count.toLowerCase();el.textContent=key==='all'?rows(config.source).length:key==='open'?count('published'):key==='closed'?rows(config.source).filter(r=>r.status!=='published').length:count(key);});
 if(by('statSeats')){const card=by('statSeats').closest('.stat-card');const label=card?.querySelector('.stat-label');if(label)label.textContent='Configured capacity';const note=card?.querySelector('.stat-note');if(note)note.textContent='Recorded capacity across your opportunities';}
 
 const traineeStats=all('.trainees-stat strong');if(traineeStats.length){[rows('placements').length,rows('placements').filter(r=>r.status==='active').length,rows('placements').filter(r=>r.status==='completed').length,'Unavailable'].forEach((v,i)=>{traineeStats[i].textContent=v;traineeStats[i].removeAttribute('aria-label');});}
 const companyStats=all('.companies-stats article > strong');if(companyStats.length){[rows('companies').length,rows('companies').filter(r=>r.accountStatus==='active').length,rows('companies').filter(r=>r.accountStatus&&r.accountStatus!=='active').length,'Unavailable'].forEach((v,i)=>companyStats[i].textContent=v);}
 all('table caption').forEach(el=>el.textContent='Authorized live Firebase records for the current account.');

 const legacyStats=all('.stat-details h3');if(legacyStats.length===3){const source=rows(config.source);let metrics;if(config.source==='academicSupervisors')metrics=[source.length,source.filter(s=>s.active===true).length,new Set(source.map(s=>s.programId).filter(Boolean)).size];else if(config.page.endsWith('Assign_Supervisors')){const assigned=source.filter(s=>rows('academicSupervisors').some(a=>a.id===s.academicSupervisorId&&a.programId===s.programId));metrics=[source.length,ready('academicSupervisors')?assigned.length:'Unavailable',ready('academicSupervisors')?source.length-assigned.length:'Unavailable'];}if(metrics)metrics.forEach((v,i)=>legacyStats[i].textContent=v);} const stat=all('.monitor-stat strong');if(stat.length){stat[0].textContent=rows('students').length;stat[1].textContent=ready('placements')?rows('placements').filter(r=>r.status==='active').length:'Unavailable';stat[2].textContent=ready('tasks')?rows('tasks').filter(r=>r.status==='submitted').length:'Unavailable';stat[3].textContent=ready('tasks')?rows('students').filter(r=>value(r,'@risk')==='Overdue task').length:'Unavailable';}
 all('.trainees-notice,.messages-notice,.completion-notice,.companies-notice,.evaluation-notice,.demo-note,.preview-note,#placementMessage,#pageNotice').filter(el=>!el.closest('dialog')).forEach(el=>el.textContent='Live Firebase records. Management writes and private evaluations are not enabled in this read-only integration.');
 all('.monitor-chip,.trainees-data-badge,.messages-disconnected').forEach(el=>el.textContent='Live Firebase');
 notice.textContent='Live Firebase records loaded for '+data.user.name+'. '+(Object.values(data.states).some(v=>v!=='ready')?'Some related reads are unavailable. ':'')+'Changes requiring additional permissions remain unavailable.';

 // Bind the existing Bootstrap studio; no second form or local provider.
 const modalEl=by('oppModal'),saveButton=by('saveOpp');
 if(modalEl&&saveButton&&window.bootstrap){
  const modal=bootstrap.Modal.getOrCreateInstance(modalEl);
  let editing=null,dirty=false,saving=false,requestId='';
  let feedback=modalEl.querySelector('[data-live-save-feedback]');
  if(!feedback){feedback=document.createElement('p');feedback.dataset.liveSaveFeedback='';feedback.setAttribute('role','status');modalEl.querySelector('.modal-body').prepend(feedback);}
  const fields={title:'oppTitle',description:'oppDescription',requirements:'oppRequirements',location:'oppLocation',capacity:'oppSeats',applicationDeadline:'oppDeadline',startDate:'oppStart',durationWeeks:'oppDuration',requiredHours:'oppRequiredHours'};
  const uuid=()=>crypto.randomUUID().replace(/-/g,'');
  const programs=Array.isArray(data.programs)?data.programs:[];
  by('majorPicker').innerHTML=programs.map(p=>'<li><input type="checkbox" id="program-'+escape(p.id)+'" value="'+escape(p.id)+'"><label for="program-'+escape(p.id)+'">'+escape(p.nameEn||p.id)+'</label></li>').join('');
  const selected=()=>all('#majorPicker input:checked').map(el=>el.value);
  const preview=()=>{text('pvTitle',by('oppTitle').value||'Opportunity title');text('pvDesc',by('oppDescription').value||'Internship description');text('pvLocation',by('oppLocation').value||'Location');text('pvDeadline',by('oppDeadline').value||'—');text('pvSeats',by('oppSeats').value||'1');text('pvDuration',by('oppDuration').value||'8');text('pvMode',modalEl.querySelector('[name="workMode"]:checked')?.value||'On-site');text('pvAccepted','0');text('pvApplicants',editing?rows('applications').filter(a=>a.opportunityId===editing.id).length:0);by('pvMajors').textContent=selected().map(id=>programs.find(p=>p.id===id)?.nameEn||id).join(', ')||'No majors selected';};
  const open=r=>{editing=r||null;requestId=uuid();dirty=false;feedback.textContent='';discard.hidden=true;
   Object.entries(fields).forEach(([key,id])=>{by(id).value=r?.[key]??({capacity:1,durationWeeks:8,requiredHours:160}[key]??'');});
   all('#majorPicker input').forEach(el=>el.checked=(r?.eligibleProgramIds||[]).includes(el.value));
   all('#workMode input').forEach(el=>el.checked=el.value===({onsite:'On-site',hybrid:'Hybrid',remote:'Remote'}[r?.workMode]||'On-site'));
   text('oppModalTitle',r?'Edit opportunity':'New opportunity');text('saveOppText',r?'Save changes':'Publish opportunity');
   all('#oppModal button,#oppModal input,#oppModal textarea').forEach(el=>{el.disabled=false;el.removeAttribute('title');});
   preview();modal.show();};
  let discard=modalEl.querySelector('[data-live-discard]');if(!discard){discard=document.createElement('button');discard.dataset.liveDiscard='';discard.type='button';discard.className='btn-ghost lg';discard.textContent='Discard changes';modalEl.querySelector('.modal-footer').prepend(discard);}discard.hidden=true;
  discard.addEventListener('click',()=>{dirty=false;modal.hide();},{signal});
  modalEl.addEventListener('hide.bs.modal',e=>{if(saving||dirty){e.preventDefault();feedback.textContent=saving?'Saving to Firebase. Please wait.':'You have unsaved changes. Continue editing or choose Discard changes.';discard.hidden=saving;}},{signal});
  modalEl.addEventListener('shown.bs.modal',()=>by('oppTitle').focus(),{signal});
  modalEl.addEventListener('input',()=>{dirty=true;preview();},{signal});
  modalEl.addEventListener('click',e=>{const step=e.target.closest('[data-step]');if(step){const input=by(step.dataset.target);input.value=Math.min(Number(input.max)||5000,Math.max(Number(input.min)||1,(Number(input.value)||0)+Number(step.dataset.step)));dirty=true;preview();}},{signal});
  all('[data-action="new"]').forEach(b=>{b.disabled=false;b.removeAttribute('title');b.addEventListener('click',()=>open(null),{signal});});
  host.addEventListener('click',e=>{const b=e.target.closest('[data-edit-opportunity]');if(b){e.preventDefault();const r=rows('opportunities').find(r=>r.id===b.dataset.editOpportunity);if(r)open(r);}},{signal});
  saveButton.addEventListener('click',async()=>{
   if(saving)return;
   const input={id:editing?.id||'',requestId,expectedUpdatedAt:editing?.updatedAt||null,status:editing?.status==='closed'?'closed':'published',eligibleProgramIds:selected(),workMode:({'On-site':'onsite',Hybrid:'hybrid',Remote:'remote'}[modalEl.querySelector('[name="workMode"]:checked')?.value])};
   Object.entries(fields).forEach(([key,id])=>input[key]=['capacity','durationWeeks','requiredHours'].includes(key)?Number(by(id).value):by(id).value.trim());
   if(!input.title||!input.description||!input.requirements||!input.location||!input.applicationDeadline||!input.startDate||!input.eligibleProgramIds.length){feedback.textContent='Complete the required fields and choose at least one specialization.';return;}
   if(input.startDate<input.applicationDeadline){feedback.textContent='Training cannot start before the application deadline.';by('oppStart').focus();return;}
   saving=true;saveButton.disabled=true;feedback.textContent='Saving securely to Firebase…';
   try{await UimsTab.request('companyOpportunitySave',input);dirty=false;saving=false;modal.hide();await init();}
   catch(error){feedback.textContent=error.message||'Save failed. Your entries have been preserved. No local fallback was used.';saving=false;saveButton.disabled=false;}
  },{signal});
 }

 render();
 }catch(error){notice.textContent=error.message;host.replaceChildren();const p=document.createElement(config.cards?'p':'tr');p.textContent='Records unavailable. Please retry after signing in.';host.append(p);}
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
if(window.Sys?.Application)window.Sys.Application.add_load(init);
})();
