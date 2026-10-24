/* Firebase-backed coordinator company directory. No browser demo data or passwords in storage. */
(() => {
    'use strict';
    const by = id => document.getElementById(id);
    const all = selector => [...document.querySelectorAll(selector)];
    const text = (id, value) => { if (by(id)) by(id).textContent = value || ''; };
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const displayName=value=>String(value ?? "").replace(/\s*\(Demo\)/gi, "").replace(/\bDemo\s+/gi, "").trim();
    const value = id => by(id)?.value.trim() || '';
    const guid = () => crypto.randomUUID().replaceAll('-', '');
    let companies = [], selected = null, supervisor = null, formId = '', requestId = '', formVersion = '', editingUid = '', page = 0, pending = '', busy = false, retryEmail = null;
    const openers = new WeakMap();
    function working(active) {
        busy=active;
        all('[data-company-action], [data-open="addCompany"], #saveCompany, #confirmAccountAction, #refreshCompanies').forEach(button=>button.disabled=active);
    }
    function open(id) {
        const dialog = by(id);
        if (!dialog.open) { openers.set(dialog, document.activeElement); dialog.showModal(); }
    }
    function close(id) { if (by(id).open) by(id).close(); }
    function notice(message, error = false) {
        const element = by('companyPageNotice'); element.textContent = message; element.hidden=!message; element.setAttribute('role', error ? 'alert' : 'status');clearTimeout(notice.timer);if(message&&!error)notice.timer=setTimeout(()=>{element.hidden=true;element.textContent='';},5000);
    }
    all('dialog').forEach(dialog => {
        dialog.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
        dialog.addEventListener('close', () => {

            openers.get(dialog)?.focus();
        });
    });
    async function api(action, fields = {}) {
        const response = await fetch(location.pathname + '?api=1', {
            method: 'POST', credentials: 'same-origin', headers: {'Content-Type':'application/json', 'X-UIMS-CSRF': by('companyCsrf').value},
            body: JSON.stringify({action, ...fields})
        });
        if (response.status === 403) throw Error('Your coordinator sign-in expired. Sign in again.');
        const data = await response.json();
        if (!response.ok || data.error) throw Error(data.error || 'The request failed. Refresh and retry.');
        return data;
    }
    async function load() {
        const data = await api('list'); companies = data.companies || []; render();
    }
    function accountStatus(user) {
        if (!user) return 'Unassigned';
        if (user.deletionStatus === 'pending') return 'Deletion pending';
        if (user.accountSetupStatus === 'pending') return 'Setup pending';
        return user.isActive ? 'Active' : 'Deactivated';
    }
    const icons={disable:'<path d="M12 3v9M6 6a9 9 0 1 0 12 0"/>',enable:'<path d="M12 3v9M6 6a9 9 0 1 0 12 0"/>',details:'<path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>',actions:'<circle cx="12" cy="12" r="9"/><path d="M12 11v6m0-10v1"/>',edit:'<path d="m15 5 4 4M4 20l4-1L20 7l-4-4L4 15v5Z"/>',addSupervisor:'<circle cx="9" cy="8" r="3"/><path d="M3 21v-3a6 6 0 0 1 12 0v3m4-13v8m-4-4h8"/>',deactivateCompany:'<path d="M12 3v9M6 6a9 9 0 1 0 12 0"/>',activateCompany:'<path d="M12 3v9M6 6a9 9 0 1 0 12 0"/>',restoreCompany:'<path d="M3 11a9 9 0 1 1 2 7M3 4v7h7"/>',resume:'<path d="M3 11a9 9 0 1 1 2 7M3 4v7h7"/>'};
    const button = (label, action, id, uid = '') => `<button type="button" class="companies-text-button" data-company-action="${action}" data-company="${escape(id)}" data-uid="${escape(uid)}" title="${escape(label)}" aria-label="${escape(label)}"><svg viewBox="0 0 24 24" aria-hidden="true">${icons[action]||icons.actions}</svg></button>`;
    function render() {
        const query = value('companySearch').toLowerCase(), status = value('companyStatus'), account = value('accountStatus');
        let rows = companies.flatMap(c => (c.supervisors.length ? c.supervisors : [null]).map(u => ({c,u}))).filter(({c,u}) => {
            const search = [c.name,c.industry,c.email,u?.fullName,u?.email].join(' ').toLowerCase();
            return (!query || search.includes(query)) && (status === 'archived' ? c.isArchived : (!status || (status==='inactive' ? !c.isActive||c.isArchived : c.isActive&&!c.isArchived))) &&
                (!account || (account === 'password' ? u?.mustChangePassword : account === 'active' ? u?.isActive && accountStatus(u) === 'Active' : u && !u.isActive));
        });
        const sort = by('companySort').selectedIndex;
        rows.sort((a,b) => sort === 2 ? a.c.industry.localeCompare(b.c.industry) || a.c.name.localeCompare(b.c.name) : a.c.name.localeCompare(b.c.name) * (sort === 1 ? -1 : 1));
        const size = Number(value('companyPageSize')) || 15, pages = Math.max(1, Math.ceil(rows.length/size)); page = Math.min(page,pages-1);
        by('companyRows').innerHTML = rows.slice(page*size,(page+1)*size).map(({c,u}) => `<tr><td><strong>${escape(displayName(c.name))}</strong><br><small>${escape(c.industry || '—')}</small></td><td>${escape(c.email || '—')}</td><td><span class="companies-badge">${c.isArchived ? 'Archived' : c.accountSetupStatus === 'pending' ? 'Setup pending' : c.isActive ? 'Active' : 'Deactivated'}</span></td><td>${escape(displayName(u?.fullName || 'No supervisor'))}</td><td>${escape(u?.email || '—')}</td><td>${escape(accountStatus(u))}${u?.mustChangePassword ? '<br><small>Password change required</small>' : ''}</td><td>${c.isArchived ? button('Restore company','restoreCompany',c.id) : c.pendingForm ? button('Resume setup','resume',c.id) : (u ? button('View details','details',c.id,u.uid) : button('Add supervisor','addSupervisor',c.id)) + button('Edit company','edit',c.id,u?.uid) + button(c.isActive?'Deactivate company':'Reactivate company',c.isActive?'deactivateCompany':'activateCompany',c.id) + (u ? button('Add supervisor','addSupervisor',c.id) : '')}</td></tr>`).join('');
        const counts = [companies.length,companies.filter(c=>c.isActive).length,companies.filter(c=>!c.isActive).length,companies.reduce((sum,c)=>sum+c.supervisors.length,0)];
        all('.companies-stats article > strong').forEach((element,i)=>element.textContent=counts[i]);
        const archived=companies.filter(c=>c.isArchived).length;
        document.querySelector('.companies-stats article small').textContent=archived ? `${archived} archived / ${companies.length-archived} current` : 'All university partners';
        document.querySelector('.companies-empty').hidden = rows.length > 0;
        text('companyEmptyTitle',companies.length ? 'No matching companies' : 'Your next partnership starts here');
        text('companyEmptyMessage',companies.length ? 'Change the filters or add a company.' : 'Add a company and its first supervisor to get started.');
        text('companyPageSummary',rows.length ? `${page*size+1}–${Math.min((page+1)*size,rows.length)} of ${rows.length} records` : 'No records');
        text('companyPageNumber',`${page+1} / ${pages}`);
        by('companyPageNumbers').innerHTML=Array.from({length:pages},(_,i)=>'<button type="button" data-company-page="'+i+'" '+(i===page?'aria-current="page"':'')+'>'+(i+1)+'</button>').join('');
        by('companyPrevious').disabled = page === 0; by('companyNext').disabled = page >= pages-1;
        working(busy);
    }
    const fields = {name:'companyName',industry:'industry',email:'officialEmail',phone:'companyPhone',website:'companyWebsite',address:'companyAddress',description:'companyDescription',supervisorName:'supervisorName',supervisorEmail:'supervisorEmail',supervisorPhone:'supervisorPhone'};
    function editor(c = null, u = null, resume = false) {
        selected=c; supervisor=u; retryEmail=null;
        let initial = {...(c || {}),supervisorName:u?.fullName,supervisorEmail:u?.email,supervisorPhone:u?.phone};
        if (resume && c.pendingForm) initial = Object.fromEntries(Object.entries(c.pendingForm).map(([key,v])=>[key[0].toLowerCase()+key.slice(1),v]));
        formId = c?.id || guid(); requestId = resume ? initial.requestId : guid(); formVersion=c?.revision || ''; editingUid = resume ? initial.supervisorUid || '' : u?.uid || '';
        Object.entries(fields).forEach(([key,id])=>by(id).value=initial[key] || '');
        text('editorTitle',resume ? 'Resume company setup' : c ? u ? 'Edit company & supervisor' : 'Add company supervisor' : 'Add company & supervisor');
        text('editorNotice','New supervisors receive a password setup link at the supervisor email below. Existing-account edits do not change the password.');
        text('companyValidation',''); by('saveCompany').disabled=false; by('saveCompany').textContent=(u && !resume) ? 'Save changes' : 'Confirm & Send'; open('companyEditor');
    }
    function valid() { return [...by('companyEditor').querySelectorAll('input,textarea,select')].every(field=>field.reportValidity()); }
    async function save() {
        if (busy || !valid()) return;
        working(true); text('companyValidation','Saving…');
        try {
            if (retryEmail) {
                const result=await api('sendSetup',retryEmail);
                retryEmail=null;close('companyEditor');notice(result.message);await load();return;
            }
            const form={id:formId,requestId,version:formVersion,supervisorUid:editingUid}; Object.entries(fields).forEach(([key,id])=>form[key]=value(id));
            const result=await api('save',{form});
            if(result.requiresSetupEmail && !result.emailSent){
                retryEmail={companyId:formId,uid:result.uid};text('companyValidation',result.message);by('saveCompany').textContent='Retry Send';
            }else{close('companyEditor');notice(result.message);}
            await load();
        } catch(error) { text('companyValidation',error.message); }
        finally {working(false);}
    }
    function target(c,u) {
        document.querySelectorAll('.companies-account-target').forEach(element=>{
            element.querySelector('strong').textContent=displayName(u?.fullName || c.name);
            element.querySelector('p').textContent=u?.email || c.email || 'Company record';
            const badge=element.querySelector('.companies-badge'); if(badge)badge.textContent=accountStatus(u);
        });
    }
    function actions(c,u) {
        selected=c;supervisor=u;target(c,u);text('accountTitle',displayName(u.fullName));
        const pendingSetup=u.accountSetupStatus==='pending'||u.deletionStatus==='pending';




        open('accountPreview');
    }
    function confirmation(action,c,u) {
        selected=c;supervisor=u;pending=action;target(c,u);
        const labels={sendSetup:'Send password setup link?',disable:'Disable supervisor?',enable:'Enable supervisor?',delete:'Delete supervisor account?',activateCompany:'Activate company?',deactivateCompany:'Deactivate company?',archiveCompany:'Archive company?',restoreCompany:'Restore company?',deleteCompany:'Delete company and history?'};
        text('confirmationTitle',labels[action]||'Company details');
        let message={sendSetup:'Send a Firebase password setup link to this supervisor’s registered email address.',disable:'Deactivate this supervisor account. The company and other supervisors stay unchanged.',enable:'Reactivate this supervisor account.',delete:'Permanently delete this supervisor login and users profile. Its linked internship history will also be removed. Other supervisor accounts will remain. If this is the last supervisor, the company starts with no previous internship history.',activateCompany:'Reactivate this company and restore supervisor access suspended by this company action. Accounts that were already disabled remain disabled.',deactivateCompany:'Deactivate this company and block every linked supervisor login. Existing placements, records and approved hours remain unchanged.',archiveCompany:'Archive this company and disable all its linked supervisor logins. Company records and internship history will be retained. You can restore the company later.',deleteCompany:'Permanently delete this company, all its supervisor logins and linked internship history. Recreating the company starts fresh. This cannot be undone.',restoreCompany:'Restore this company as inactive. Supervisor accounts stay disabled until you enable them individually.'}[action];
        if(action==='details') message=[displayName(c.name),c.industry,c.email,c.phone,c.website,c.address,c.description,`Supervisor: ${displayName(u.fullName)}`,u.email,u.phone,`Account: ${accountStatus(u)}`,'Password setup: email link'].filter(Boolean).join('\n');
        text('confirmationDescription',message);by('confirmAccountAction').hidden=action==='details';by('confirmAccountAction').disabled=false;
        by('confirmAccountAction').textContent=action==='delete'?'Delete account':action==='deleteCompany'?'Delete company':'Confirm';open('accountConfirmation');
    }
    all('[data-open="addCompany"]').forEach(button=>button.addEventListener('click',()=>editor()));
    document.querySelector('[data-open="accountPreview"]')?.addEventListener('click',()=>notice('Choose Actions beside a supervisor in the directory.'));
    by('companyRows').addEventListener('click',event=>{
        const button=event.target.closest('[data-company-action]');if(!button||busy)return;
        const c=companies.find(c=>c.id===button.dataset.company),u=c?.supervisors.find(u=>u.uid===button.dataset.uid);if(!c)return;
        switch(button.dataset.companyAction){case 'resume':editor(c,u,true);break;case 'edit':editor(c,u);break;case 'addSupervisor':editor(c);break;case 'actions':actions(c,u);break;case 'details':case 'disable':case 'enable':confirmation(button.dataset.companyAction,c,u);break;default:confirmation(button.dataset.companyAction,c,null);}
    });
    all('[data-account-action]').forEach(button=>button.addEventListener('click',()=>{
        close('accountPreview'); if(button.dataset.accountAction==='edit')editor(selected,supervisor);else confirmation(button.dataset.accountAction,selected,supervisor);
    }));
    by('confirmAccountAction').addEventListener('click',async()=>{
        if(busy||!pending)return;working(true);
        try{const result=await api(pending,{companyId:selected.id,uid:supervisor?.uid||''});close('accountConfirmation');notice(result.message);await load();}
        catch(error){text('confirmationDescription',error.message);}
        finally{working(false);}
    });
    by('backToActions').addEventListener('click',()=>close('accountConfirmation'));

    by('saveCompany').addEventListener('click',save);

    ['companySearch','companyStatus','accountStatus','companySort','companyPageSize'].forEach(id=>by(id).addEventListener(id==='companySearch'?'input':'change',()=>{page=0;render();}));
    by('resetCompanyFilters').addEventListener('click',()=>{['companySearch','companyStatus','accountStatus','dashboardSearch'].forEach(id=>by(id).value='');by('companySort').selectedIndex=0;page=0;render();});
    by('companyPrevious').addEventListener('click',()=>{page--;render();});by('companyNext').addEventListener('click',()=>{page++;render();});
    by('companyPageNumbers').addEventListener('click',event=>{const button=event.target.closest('[data-company-page]');if(button){page=Number(button.dataset.companyPage);render();}});
    let refreshing=false;
    async function refreshDirectory(){if(refreshing||busy||document.hidden||document.querySelector('dialog[open]'))return;refreshing=true;try{await load();}catch(error){notice(error.message,true);}finally{refreshing=false;}}
    setInterval(refreshDirectory,60000);document.addEventListener('visibilitychange',()=>{if(!document.hidden)refreshDirectory();});
    const search=by('dashboardSearch');search.dataset.searchTarget='companySearch';
    search.addEventListener('input',()=>{by('companySearch').value=search.value;page=0;render();});
    document.forms[0].addEventListener('submit',event=>{event.preventDefault();if(by('companyEditor').open)save();});
    load().then(()=>notice('')).catch(error=>notice(error.message,true));
})();
