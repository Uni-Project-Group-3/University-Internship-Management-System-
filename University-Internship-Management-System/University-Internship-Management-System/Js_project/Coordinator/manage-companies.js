/* Firebase-backed coordinator company directory. No browser demo data or passwords in storage. */
(() => {
    'use strict';
    const by = id => document.getElementById(id);
    const all = selector => [...document.querySelectorAll(selector)];
    const text = (id, value) => { if (by(id)) by(id).textContent = value || ''; };
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const value = id => by(id)?.value.trim() || '';
    const guid = () => crypto.randomUUID().replaceAll('-', '');
    let companies = [], selected = null, supervisor = null, formId = '', requestId = '', formVersion = '', editingUid = '', page = 0, pending = '', busy = false;
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
        const element = by('companyPageNotice'); element.textContent = message; element.setAttribute('role', error ? 'alert' : 'status');
    }
    all('dialog').forEach(dialog => {
        dialog.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
        dialog.addEventListener('close', () => {
            if (dialog.id === 'accountConfirmation') { by('companyCredentialPassword').value = ''; by('companyCredentialEmail').value = ''; }
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
        return user.isActive ? 'Active' : 'Disabled';
    }
    const button = (label, action, id, uid = '') => `<button type="button" class="companies-text-button" data-company-action="${action}" data-company="${escape(id)}" data-uid="${escape(uid)}">${escape(label)}</button>`;
    function render() {
        const query = value('companySearch').toLowerCase(), status = value('companyStatus'), account = value('accountStatus');
        let rows = companies.flatMap(c => (c.supervisors.length ? c.supervisors : [null]).map(u => ({c,u}))).filter(({c,u}) => {
            const search = [c.name,c.industry,c.email,u?.fullName,u?.email].join(' ').toLowerCase();
            return (!query || search.includes(query)) && (status === 'archived' ? c.isArchived : !c.isArchived && (!status || c.isActive === (status === 'active'))) &&
                (!account || (account === 'password' ? u?.mustChangePassword : account === 'active' ? u?.isActive && accountStatus(u) === 'Active' : u && !u.isActive));
        });
        const sort = by('companySort').selectedIndex;
        rows.sort((a,b) => sort === 2 ? a.c.industry.localeCompare(b.c.industry) || a.c.name.localeCompare(b.c.name) : a.c.name.localeCompare(b.c.name) * (sort === 1 ? -1 : 1));
        const size = Number(value('companyPageSize')) || 10, pages = Math.max(1, Math.ceil(rows.length/size)); page = Math.min(page,pages-1);
        by('companyRows').innerHTML = rows.slice(page*size,(page+1)*size).map(({c,u}) => `<tr><td><strong>${escape(c.name)}</strong><br><small>${escape(c.industry || '—')}</small></td><td>${escape(c.email || '—')}</td><td><span class="companies-badge">${c.isArchived ? 'Archived' : c.accountSetupStatus === 'pending' ? 'Setup pending' : c.isActive ? 'Active' : 'Inactive'}</span></td><td>${escape(u?.fullName || 'No supervisor')}</td><td>${escape(u?.email || '—')}</td><td>${escape(accountStatus(u))}${u?.mustChangePassword ? '<br><small>Password change required</small>' : ''}</td><td>${c.isArchived ? button('Restore company','restoreCompany',c.id) : c.pendingForm ? button('Resume setup','resume',c.id) : (u ? button('Actions','actions',c.id,u.uid) : button('Add supervisor','addSupervisor',c.id)) + button('Edit company','edit',c.id,u?.uid) + button(c.isActive?'Deactivate company':'Activate company',c.isActive?'deactivateCompany':'activateCompany',c.id) + (u ? button('Add supervisor','addSupervisor',c.id) : '') + button('Archive company','archiveCompany',c.id)}</td></tr>`).join('');
        const counts = [companies.length,companies.filter(c=>c.isActive).length,companies.filter(c=>!c.isActive).length,companies.reduce((sum,c)=>sum+c.supervisors.length,0)];
        all('.companies-stats article > strong').forEach((element,i)=>element.textContent=counts[i]);
        const archived=companies.filter(c=>c.isArchived).length;
        document.querySelector('.companies-stats article small').textContent=archived ? `${archived} archived / ${companies.length-archived} current` : 'All university partners';
        document.querySelector('.companies-empty').hidden = rows.length > 0;
        text('companyEmptyTitle',companies.length ? 'No matching companies' : 'Your next partnership starts here');
        text('companyEmptyMessage',companies.length ? 'Change the filters or add a company.' : 'Add a company and its first supervisor to get started.');
        text('companyPageSummary',`${rows.length} records · Page ${page+1} / ${pages}`);
        text('companyPageNumber',`${page+1} / ${pages}`);
        by('companyPrevious').disabled = page === 0; by('companyNext').disabled = page >= pages-1;
        working(busy);
    }
    const fields = {name:'companyName',industry:'industry',email:'officialEmail',phone:'companyPhone',website:'companyWebsite',address:'companyAddress',description:'companyDescription',supervisorName:'supervisorName',supervisorEmail:'supervisorEmail',supervisorPhone:'supervisorPhone'};
    function editor(c = null, u = null, resume = false) {
        selected=c; supervisor=u;
        let initial = {...(c || {}),supervisorName:u?.fullName,supervisorEmail:u?.email,supervisorPhone:u?.phone};
        if (resume && c.pendingForm) initial = Object.fromEntries(Object.entries(c.pendingForm).map(([key,v])=>[key[0].toLowerCase()+key.slice(1),v]));
        formId = c?.id || guid(); requestId = resume ? initial.requestId : guid(); formVersion=c?.revision || ''; editingUid = resume ? initial.supervisorUid || '' : u?.uid || '';
        Object.entries(fields).forEach(([key,id])=>by(id).value=initial[key] || '');
        text('editorTitle',resume ? 'Resume company setup' : c ? u ? 'Edit company & supervisor' : 'Add company supervisor' : 'Add company & supervisor');
        text('editorNotice','Enter company details manually. New supervisors receive a temporary password here; no email is sent.');
        text('companyValidation',''); by('saveCompany').disabled=false; by('saveCompany').textContent=c ? 'Save changes' : 'Create company'; open('companyEditor');
    }
    function valid() { return [...by('companyEditor').querySelectorAll('input,textarea,select')].every(field=>field.reportValidity()); }
    async function save() {
        if (busy || !valid()) return;
        working(true); text('companyValidation','Saving…');
        try {
            const form={id:formId,requestId,version:formVersion,supervisorUid:editingUid}; Object.entries(fields).forEach(([key,id])=>form[key]=value(id));
            const result=await api('save',{form}); close('companyEditor'); notice(result.message); if(result.temporaryPassword) credentials(result); await load();
        } catch(error) { text('companyValidation',error.message); }
        finally {working(false);}
    }
    function target(c,u) {
        document.querySelectorAll('.companies-account-target').forEach(element=>{
            element.querySelector('strong').textContent=u?.fullName || c.name;
            element.querySelector('p').textContent=u?.email || c.email || 'Company record';
            const badge=element.querySelector('.companies-badge'); if(badge)badge.textContent=accountStatus(u);
        });
    }
    function actions(c,u) {
        selected=c;supervisor=u;target(c,u);text('accountTitle',u.fullName);
        const pendingSetup=u.accountSetupStatus==='pending'||u.deletionStatus==='pending';
        by('accountPreview').querySelector('[data-account-action="enable"]').disabled=u.isActive||pendingSetup;
        by('accountPreview').querySelector('[data-account-action="disable"]').disabled=!u.isActive||pendingSetup;
        open('accountPreview');
    }
    function confirmation(action,c,u) {
        selected=c;supervisor=u;pending=action;target(c,u);by('companyCredentials').hidden=true;
        const labels={disable:'Disable supervisor?',enable:'Enable supervisor?',delete:'Delete supervisor account?',activateCompany:'Activate company?',deactivateCompany:'Deactivate company?',archiveCompany:'Archive company?',restoreCompany:'Restore company?'};
        text('confirmationTitle',labels[action]||'Company details');
        let message={disable:'Disable this supervisor login in Firebase. The company and other supervisors stay unchanged.',enable:'Enable this supervisor login in Firebase.',delete:'Permanently delete this supervisor login and users profile. The company, other supervisors and internship history will remain.',activateCompany:'Mark the company as active. Supervisor login statuses stay unchanged.',deactivateCompany:'Mark the company as inactive. Supervisor login statuses stay unchanged.',archiveCompany:'Archive this company and disable all its linked supervisor logins. Company records and internship history will be retained. You can restore the company later.',restoreCompany:'Restore this company as inactive. Supervisor accounts stay disabled until you enable them individually.'}[action];
        if(action==='details') message=[c.name,c.industry,c.email,c.phone,c.website,c.address,c.description,`Supervisor: ${u.fullName}`,u.email,u.phone,`Account: ${accountStatus(u)}`,`First-login password change: ${u.mustChangePassword?'Required':'No'}`].filter(Boolean).join('\n');
        text('confirmationDescription',message);by('confirmAccountAction').hidden=action==='details';by('confirmAccountAction').disabled=false;
        by('confirmAccountAction').textContent=action==='delete'?'Delete account':'Confirm';open('accountConfirmation');
    }
    function credentials(result) {
        pending='';text('confirmationTitle','Sign-in details');text('confirmationDescription',result.message+' Copy the password before closing; it cannot be viewed again.');by('confirmAccountAction').hidden=true;
        document.querySelector('#accountConfirmation .companies-account-target strong').textContent='Supervisor sign-in';
        document.querySelector('#accountConfirmation .companies-account-target p').textContent=result.email;
        text('companyCopyStatus','');
        by('companyCredentialEmail').value=result.email;by('companyCredentialPassword').value=result.temporaryPassword;by('companyCredentials').hidden=false;open('accountConfirmation');
    }
    all('[data-open="addCompany"]').forEach(button=>button.addEventListener('click',()=>editor()));
    document.querySelector('[data-open="accountPreview"]').addEventListener('click',()=>notice('Choose Actions beside a supervisor in the directory.'));
    by('companyRows').addEventListener('click',event=>{
        const button=event.target.closest('[data-company-action]');if(!button||busy)return;
        const c=companies.find(c=>c.id===button.dataset.company),u=c?.supervisors.find(u=>u.uid===button.dataset.uid);if(!c)return;
        switch(button.dataset.companyAction){case 'resume':editor(c,u,true);break;case 'edit':editor(c,u);break;case 'addSupervisor':editor(c);break;case 'actions':actions(c,u);break;default:confirmation(button.dataset.companyAction,c,null);}
    });
    all('[data-account-action]').forEach(button=>button.addEventListener('click',()=>{
        close('accountPreview'); if(button.dataset.accountAction==='edit')editor(selected,supervisor);else confirmation(button.dataset.accountAction,selected,supervisor);
    }));
    by('confirmAccountAction').addEventListener('click',async()=>{
        if(busy||!pending)return;working(true);
        try{const result=await api(pending,{companyId:selected.id,uid:supervisor?.uid||''});close('accountConfirmation');notice(result.message);if(result.temporaryPassword)credentials(result);await load();}
        catch(error){text('confirmationDescription',error.message);}
        finally{working(false);}
    });
    by('backToActions').addEventListener('click',()=>close('accountConfirmation'));
    by('copyCompanyPassword').addEventListener('click',async()=>{
        try{await navigator.clipboard.writeText(by('companyCredentialPassword').value);text('companyCopyStatus','Copied.');}
        catch{by('companyCredentialPassword').select();text('companyCopyStatus','Press Ctrl+C to copy.');}
    });
    by('saveCompany').addEventListener('click',save);
    by('validateCompany').addEventListener('click',()=>{if(valid())text('companyValidation','Fields look valid. Save to create or update the company.');});
    ['companySearch','companyStatus','accountStatus','companySort','companyPageSize'].forEach(id=>by(id).addEventListener(id==='companySearch'?'input':'change',()=>{page=0;render();}));
    by('resetCompanyFilters').addEventListener('click',()=>{['companySearch','companyStatus','accountStatus','dashboardSearch'].forEach(id=>by(id).value='');by('companySort').selectedIndex=0;page=0;render();});
    by('companyPrevious').addEventListener('click',()=>{page--;render();});by('companyNext').addEventListener('click',()=>{page++;render();});
    by('refreshCompanies').addEventListener('click',()=>load().then(()=>notice('Directory refreshed.')).catch(error=>notice(error.message,true)));
    const search=by('dashboardSearch');search.dataset.searchTarget='companySearch';
    search.addEventListener('input',()=>{by('companySearch').value=search.value;page=0;render();});
    document.forms[0].addEventListener('submit',event=>{event.preventDefault();if(by('companyEditor').open)save();});
    load().then(()=>notice('Directory connected. Add companies manually; temporary passwords are shown here and no email is sent.')).catch(error=>notice(error.message,true));
})();
