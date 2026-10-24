/* Official university-record lookup and Firebase academic supervisor accounts. */
(() => {
    'use strict';
    const by=id=>document.getElementById(id), all=selector=>[...document.querySelectorAll(selector)];
    const escape=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const text=(id,value)=>by(id).textContent=value||'';
    const editor=bootstrap.Modal.getOrCreateInstance(by('addSupervisorModal'));
    const confirmation=bootstrap.Modal.getOrCreateInstance(by('deleteModal'));
    const details=bootstrap.Modal.getOrCreateInstance(by('academicDetailsModal'));
    let records=[], candidate=null, selected=null, pending='', busy=false, page=0;
    const displayName=value=>String(value ?? "").replace(/\s*\(Demo\)/gi, "").replace(/\bDemo\s+/gi, "").trim();
    function notice(message,error=false){text('pageNotice',message);by('pageNotice').hidden=!message;by('pageNotice').classList.toggle('alert-danger',error);by('pageNotice').setAttribute('role',error?'alert':'status');clearTimeout(notice.timer);if(message&&!error)notice.timer=setTimeout(()=>{by('pageNotice').hidden=true;text('pageNotice','');},5000);}
    function modalError(message){text('modalError',message);by('modalError').hidden=!message;}
    function working(active){busy=active;by('academicStaffId').disabled=active;all('[data-academic-action],#findAcademic,#saveSupBtn,#confirmStatusBtn,#refreshAcademic,#addAcademicButton').forEach(button=>button.disabled=active);if(!active){by('saveSupBtn').disabled=!candidate||candidate.emailSent;by('saveSupBtn').textContent=candidate?.created&&!candidate.emailSent?'Retry Send':'Confirm & Send';}}
    async function api(action,data={}){
        const response=await fetch(location.pathname+'?api=1',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','X-UIMS-CSRF':by('academicCsrf').value},body:JSON.stringify({action,...data})});
        if(response.status===403)throw Error('Your coordinator sign-in expired. Sign in again.');
        const result=await response.json();if(!response.ok||result.error)throw Error(result.error||'The request failed. Refresh and retry.');return result;
    }
    function clearCandidate(){candidate=null;['supName','supEmail','supDept','supUniversity','supCollege'].forEach(id=>by(id).value='');modalError('');text('academicLookupMessage','');by('saveSupBtn').disabled=true;}
    function status(record){return record.deletionStatus==='pending'?'Deletion pending':record.accountSetupStatus==='pending'?'Setup pending':record.isActive?'Active':'Inactive';}
    function button(label,action,uid,icon){const svg={ 'bi-eye':'<path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>', 'bi-trash':'<path d="M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7m4-7v7"/>', 'bi-arrow-clockwise':'<path d="M3 11a9 9 0 1 1 2 7M3 4v7h7"/>'};return `<button type="button" class="action-btn" title="${escape(label)}" aria-label="${escape(label)}" data-academic-action="${action}" data-uid="${escape(uid)}"><svg viewBox="0 0 24 24" aria-hidden="true">${svg[icon]||svg['bi-eye']}</svg></button>`;}
    function render(){
        const query=by('searchInput').value.trim().toLowerCase(),department=by('deptFilter').value;
        const visible=records.filter(r=>(!query||[r.fullName,r.staffId,r.email,r.department].join(' ').toLowerCase().includes(query))&&(!department||department==='all'||r.departmentId===department));
        const size=Number(by('academicPageSize').value)||15,pages=Math.max(1,Math.ceil(visible.length/size));page=Math.max(0,Math.min(page,pages-1));
        by('academicRows').innerHTML=visible.slice(page*size,(page+1)*size).map(r=>`<tr><td><div class="d-flex align-items-center gap-3"><div class="table-avatar">${escape(r.fullName.split(/\s+/).slice(0,2).map(part=>part[0]).join(''))}</div><div><div class="fw-bold text-dark supervisor-name">${escape(displayName(r.fullName))}</div><div class="text-muted small">ID: ${escape(r.staffId||'Unavailable')} · ${escape(r.email)}</div></div></div></td><td><span class="dept-badge">${escape(r.department)}</span></td><td><span class="badge bg-light text-dark border">${r.groupCount} Groups</span><div class="text-muted small">${r.studentCount} active students</div></td><td><span class="status-badge ${r.isActive?'success':'danger'}">${escape(status(r))}</span>${r.mustChangePassword?'<div class="text-muted small">Password change required</div>':''}</td><td class="text-end"><div class="action-buttons">${button('View details','details',r.uid,'bi-eye')}${r.accountSetupStatus==='pending'?button('Resume setup','resume',r.uid,'bi-arrow-clockwise')+button('Delete academic supervisor','delete',r.uid,'bi-trash'):button('Delete academic supervisor','delete',r.uid,'bi-trash')}</div></td></tr>`).join('');
        text('academicPageSummary',visible.length ? (page*size+1)+'–'+Math.min((page+1)*size,visible.length)+' of '+visible.length+' supervisors':'No supervisors');
        by('academicPageNumbers').innerHTML=Array.from({length:pages},(_,i)=>'<button type="button" data-academic-page="'+i+'" '+(i===page?'aria-current="page"':'')+'>'+(i+1)+'</button>').join('');
        by('academicPrevious').disabled=page===0;by('academicNext').disabled=page===pages-1;
        by('noResultsMsg').hidden=visible.length>0;text('noResultsMsg',records.length?'No supervisors match your search.':'No academic supervisor accounts yet. Look up an official staff ID to create one.');
        const counts=[records.length,records.filter(r=>r.isActive&&status(r)==='Active').length,new Set(records.map(r=>r.departmentId).filter(Boolean)).size];
        all('.stat-details h3').forEach((element,i)=>element.textContent=counts[i]);
        working(busy);
    }
    async function load(){const result=await api('list');records=result.supervisors||[];const current=by('deptFilter').value;by('deptFilter').innerHTML='<option value="all">All Departments</option>'+[...new Map(records.map(r=>[r.departmentId,r.department])).entries()].sort((a,b)=>a[1].localeCompare(b[1])).map(([id,name])=>`<option value="${escape(id)}">${escape(name)}</option>`).join('');by('deptFilter').value=[...by('deptFilter').options].some(o=>o.value===current)?current:'all';render();}
    async function lookup(){if(busy)return;clearCandidate();working(true);text('academicLookupMessage','Looking up official university records...');try{const result=await api('lookup',{staffId:by('academicStaffId').value.trim()});candidate={...result.record,created:false};by('supName').value=candidate.fullName;by('supEmail').value=candidate.email;by('supDept').value=candidate.departmentId;by('supUniversity').value=candidate.universityId;by('supCollege').value=candidate.collegeId;text('academicLookupMessage',result.message);}catch(error){text('academicLookupMessage','');modalError(error.message);}finally{working(false);}}
    by('addAcademicButton').addEventListener('click',()=>{clearCandidate();by('academicStaffId').value='';text('addSupervisorModalTitle','Add Academic Supervisor');editor.show();});
    by('academicStaffId').addEventListener('input',clearCandidate);
    by('academicStaffId').addEventListener('keydown',event=>{if(event.key==='Enter'){event.preventDefault();lookup();}});
    by('findAcademic').addEventListener('click',lookup);
    by('saveSupBtn').addEventListener('click',async()=>{
        if(busy||!candidate||candidate.emailSent)return;
        working(true);modalError('');
        try{
            const result=await api('create',{staffId:candidate.staffId});
            candidate.created=result.created;candidate.emailSent=result.emailSent;

            text('academicLookupMessage',result.message);
            if(!result.emailSent)modalError(result.message);
            notice(result.message,!result.emailSent);await load();
        }catch(error){modalError(error.message);}finally{working(false);}
    });
    const unsent=()=>candidate?.created&&!candidate.emailSent;
    by('addSupervisorModal').addEventListener('hide.bs.modal',event=>{
        if(busy || (unsent()&&!window.confirm('Account details have not been emailed. The password setup email has not been sent. Close anyway?')))event.preventDefault();
    });
    window.addEventListener('beforeunload',event=>{if(unsent()){event.preventDefault();event.returnValue='';}});
    by('academicRows').addEventListener('click',event=>{
        const button=event.target.closest('[data-academic-action]');if(!button||busy)return;
        selected=records.find(r=>r.uid===button.dataset.uid);if(!selected)return;pending=button.dataset.academicAction;
        if(pending==='resume'){clearCandidate();by('academicStaffId').value=selected.staffId;editor.show();lookup();return;}
        if(pending==='details'){
            text('academicDetailsTitle',displayName(selected.fullName));text('academicDetailsText',`Staff ID: ${selected.staffId}\nEmail: ${selected.email}\nUniversity: ${selected.universityId}\nCollege: ${selected.collegeId}\nDepartment: ${selected.department}\nGroups: ${selected.groupCount}\nActive students: ${selected.studentCount}\nAccount: ${status(selected)}\nPassword change: ${selected.mustChangePassword?'Required':'No'}`);details.show();return;
        }
        const titles={enable:'Activate academic supervisor?',disable:'Deactivate academic supervisor?',delete:'Delete academic supervisor account?',sync:'Sync official university record?'};
        text('deleteModalTitle',titles[pending]);text('statusTarget',`${displayName(selected.fullName)} (${selected.staffId}) — ${selected.email}`);
        text('academicActionDescription',{enable:'Restore sign-in for this supervisor after checking the current official record.',disable:'Disable this supervisor login. University records, groups and history stay unchanged.',delete:'Permanently delete the Firebase login and users profile. Old supervisor links and supervisor history will also be cleared. Official university records remain. Active students or groups must be reassigned first.',sync:'Refresh the name, email and department from the official university record. You cannot enter replacement official details manually.'}[pending]);
        by('confirmStatusBtn').textContent=pending==='delete'?'Delete account':'Confirm';text('academicActionError','');confirmation.show();
    });
    by('confirmStatusBtn').addEventListener('click',async()=>{if(busy||!selected)return;working(true);try{const result=await api(pending,{uid:selected.uid});confirmation.hide();notice(result.message);await load();}catch(error){text('academicActionError',error.message);}finally{working(false);}});
    const resetPage=()=>{page=0;render();};
    by('academicPageSize').addEventListener('change',resetPage);
    by('academicPrevious').addEventListener('click',()=>{page--;render();});by('academicNext').addEventListener('click',()=>{page++;render();});
    by('academicPageNumbers').addEventListener('click',event=>{const button=event.target.closest('[data-academic-page]');if(button){page=Number(button.dataset.academicPage);render();}});
    let refreshing=false;
    async function refreshDirectory(){if(refreshing||busy||document.hidden||document.querySelector('.modal.show'))return;refreshing=true;try{await load();}catch(error){notice(error.message,true);}finally{refreshing=false;}}
    setInterval(refreshDirectory,60000);document.addEventListener('visibilitychange',()=>{if(!document.hidden)refreshDirectory();});
    by('searchInput').addEventListener('input',resetPage);by('searchInput').addEventListener('keydown',event=>{if(event.key==='Enter')event.preventDefault();});by('deptFilter').addEventListener('change',resetPage);
    by('addSupervisorModal').addEventListener('hidden.bs.modal',()=>{clearCandidate();});
    document.forms[0].addEventListener('submit',event=>{if(event.submitter?.id==='btnLogout')return;event.preventDefault();});
    // Preserve the existing responsive sidebar without demo login interception.
    const sidebar=by('sidebar'),menu=document.querySelector('.menu-btn'),backdrop=document.querySelector('.sidebar-backdrop');
    const toggle=open=>{document.body.classList.toggle('sidebar-open',open);menu.setAttribute('aria-expanded',String(open));};
    menu.addEventListener('click',()=>toggle(!document.body.classList.contains('sidebar-open')));document.querySelector('.sidebar-close').addEventListener('click',()=>toggle(false));backdrop.addEventListener('click',()=>toggle(false));
    load().then(()=>notice('')).catch(error=>notice(error.message,true));
})();
