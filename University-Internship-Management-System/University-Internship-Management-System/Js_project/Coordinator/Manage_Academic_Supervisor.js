/* Official university-record lookup and Firebase academic supervisor accounts. */
(() => {
    'use strict';
    const by=id=>document.getElementById(id), all=selector=>[...document.querySelectorAll(selector)];
    const escape=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const text=(id,value)=>by(id).textContent=value||'';
    const editor=bootstrap.Modal.getOrCreateInstance(by('addSupervisorModal'));
    const confirmation=bootstrap.Modal.getOrCreateInstance(by('deleteModal'));
    const details=bootstrap.Modal.getOrCreateInstance(by('academicDetailsModal'));
    let records=[], candidate=null, selected=null, pending='', busy=false;
    function notice(message,error=false){text('pageNotice',message);by('pageNotice').hidden=false;by('pageNotice').classList.toggle('alert-danger',error);}
    function modalError(message){text('modalError',message);by('modalError').hidden=!message;}
    function working(active){busy=active;by('academicStaffId').disabled=active;all('[data-academic-action],#findAcademic,#generateAcademicPassword,#saveSupBtn,#confirmStatusBtn,#refreshAcademic,#addAcademicButton').forEach(button=>button.disabled=active);if(!active){by('generateAcademicPassword').disabled=!candidate||candidate.created;by('saveSupBtn').disabled=!candidate||candidate.created;}}
    async function api(action,data={}){
        const response=await fetch(location.pathname+'?api=1',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','X-UIMS-CSRF':by('academicCsrf').value},body:JSON.stringify({action,...data})});
        if(response.status===403)throw Error('Your coordinator sign-in expired. Sign in again.');
        const result=await response.json();if(!response.ok||result.error)throw Error(result.error||'The request failed. Refresh and retry.');return result;
    }
    function clearCandidate(){candidate=null;['supName','supEmail','supDept','supUniversity','supCollege','academicPassword'].forEach(id=>by(id).value='');modalError('');text('academicLookupMessage','');text('academicCopyStatus','');by('generateAcademicPassword').disabled=true;by('saveSupBtn').disabled=true;}
    function status(record){return record.deletionStatus==='pending'?'Deletion pending':record.accountSetupStatus==='pending'?'Setup pending':record.isActive?'Active':'Inactive';}
    function button(label,action,uid,icon){return `<button type="button" class="action-btn" title="${escape(label)}" aria-label="${escape(label)}" data-academic-action="${action}" data-uid="${escape(uid)}"><i class="bi ${icon}" aria-hidden="true"></i></button>`;}
    function render(){
        const query=by('searchInput').value.trim().toLowerCase(),department=by('deptFilter').value;
        const visible=records.filter(r=>(!query||[r.fullName,r.staffId,r.email,r.department].join(' ').toLowerCase().includes(query))&&(!department||department==='all'||r.departmentId===department));
        by('academicRows').innerHTML=visible.map(r=>`<tr><td><div class="d-flex align-items-center gap-3"><div class="table-avatar">${escape(r.fullName.split(/\s+/).slice(0,2).map(part=>part[0]).join(''))}</div><div><div class="fw-bold text-dark supervisor-name">${escape(r.fullName)}</div><div class="text-muted small">ID: ${escape(r.staffId||'Unavailable')} · ${escape(r.email)}</div></div></div></td><td><span class="dept-badge">${escape(r.department)}</span></td><td><span class="badge bg-light text-dark border">${r.groupCount} Groups</span><div class="text-muted small">${r.studentCount} active students</div></td><td><span class="status-badge ${r.isActive?'success':'danger'}">${escape(status(r))}</span>${r.mustChangePassword?'<div class="text-muted small">Password change required</div>':''}</td><td class="text-end"><div class="action-buttons">${button('View details','details',r.uid,'bi-eye')}${r.accountSetupStatus==='pending'?button('Resume setup','resume',r.uid,'bi-arrow-clockwise'):button('Sync university record','sync',r.uid,'bi-arrow-repeat')+button(r.isActive?'Deactivate':'Activate',r.isActive?'disable':'enable',r.uid,r.isActive?'bi-person-x':'bi-person-check')}${button('Delete account','delete',r.uid,'bi-trash')}</div></td></tr>`).join('');
        by('noResultsMsg').hidden=visible.length>0;text('noResultsMsg',records.length?'No supervisors match your search.':'No academic supervisor accounts yet. Look up an official staff ID to create one.');
        const counts=[records.length,records.filter(r=>r.isActive&&status(r)==='Active').length,new Set(records.map(r=>r.departmentId).filter(Boolean)).size];
        all('.stat-details h3').forEach((element,i)=>element.textContent=counts[i]);
        working(busy);
    }
    async function load(){const result=await api('list');records=result.supervisors||[];const current=by('deptFilter').value;by('deptFilter').innerHTML='<option value="all">All Departments</option>'+[...new Map(records.map(r=>[r.departmentId,r.department])).entries()].sort((a,b)=>a[1].localeCompare(b[1])).map(([id,name])=>`<option value="${escape(id)}">${escape(name)}</option>`).join('');by('deptFilter').value=[...by('deptFilter').options].some(o=>o.value===current)?current:'all';render();}
    async function lookup(){if(busy)return;clearCandidate();working(true);text('academicLookupMessage','Looking up official university records...');try{const result=await api('lookup',{staffId:by('academicStaffId').value.trim()});candidate={...result.record,created:false};by('supName').value=candidate.fullName;by('supEmail').value=candidate.email;by('supDept').value=candidate.departmentId;by('supUniversity').value=candidate.universityId;by('supCollege').value=candidate.collegeId;by('academicPassword').value=result.temporaryPassword;text('academicLookupMessage',result.message);}catch(error){text('academicLookupMessage','');modalError(error.message);}finally{working(false);}}
    by('addAcademicButton').addEventListener('click',()=>{clearCandidate();by('academicStaffId').value='';text('addSupervisorModalTitle','Add Academic Supervisor');editor.show();});
    by('academicStaffId').addEventListener('input',clearCandidate);
    by('academicStaffId').addEventListener('keydown',event=>{if(event.key==='Enter'){event.preventDefault();lookup();}});
    by('findAcademic').addEventListener('click',lookup);
    by('generateAcademicPassword').addEventListener('click',async()=>{if(busy||!candidate||candidate.created)return;working(true);try{const result=await api('generate',{staffId:candidate.staffId});by('academicPassword').value=result.temporaryPassword;}catch(error){modalError(error.message);}finally{working(false);}});
    by('copyAcademicPassword').addEventListener('click',async()=>{if(!by('academicPassword').value)return;try{await navigator.clipboard.writeText(by('academicPassword').value);text('academicCopyStatus','Copied.');}catch{by('academicPassword').select();text('academicCopyStatus','Press Ctrl+C to copy.');}});
    by('saveSupBtn').addEventListener('click',async()=>{if(busy||!candidate||candidate.created)return;working(true);modalError('');try{const result=await api('create',{staffId:by('academicStaffId').value.trim()});candidate.created=true;by('academicPassword').value=result.temporaryPassword;text('academicLookupMessage',result.message+' Copy the password before closing.');notice(result.message);await load();}catch(error){modalError(error.message);}finally{working(false);}});
    by('academicRows').addEventListener('click',event=>{
        const button=event.target.closest('[data-academic-action]');if(!button||busy)return;
        selected=records.find(r=>r.uid===button.dataset.uid);if(!selected)return;pending=button.dataset.academicAction;
        if(pending==='resume'){clearCandidate();by('academicStaffId').value=selected.staffId;editor.show();lookup();return;}
        if(pending==='details'){
            text('academicDetailsTitle',selected.fullName);text('academicDetailsText',`Staff ID: ${selected.staffId}\nEmail: ${selected.email}\nUniversity: ${selected.universityId}\nCollege: ${selected.collegeId}\nDepartment: ${selected.department}\nGroups: ${selected.groupCount}\nActive students: ${selected.studentCount}\nAccount: ${status(selected)}\nPassword change: ${selected.mustChangePassword?'Required':'No'}`);details.show();return;
        }
        const titles={enable:'Activate academic supervisor?',disable:'Deactivate academic supervisor?',delete:'Delete academic supervisor account?',sync:'Sync official university record?'};
        text('deleteModalTitle',titles[pending]);text('statusTarget',`${selected.fullName} (${selected.staffId}) — ${selected.email}`);
        text('academicActionDescription',{enable:'Enable this supervisor in Firebase after checking the current official record.',disable:'Disable this supervisor login. University records, groups and history stay unchanged.',delete:'Permanently delete the Firebase login and users profile. Official university records and academic history will remain. Active students or groups must be reassigned first.',sync:'Refresh the name, email and department from the official university record. You cannot enter replacement official details manually.'}[pending]);
        by('confirmStatusBtn').textContent=pending==='delete'?'Delete account':'Confirm';text('academicActionError','');confirmation.show();
    });
    by('confirmStatusBtn').addEventListener('click',async()=>{if(busy||!selected)return;working(true);try{const result=await api(pending,{uid:selected.uid});confirmation.hide();notice(result.message);await load();}catch(error){text('academicActionError',error.message);}finally{working(false);}});
    by('refreshAcademic').addEventListener('click',()=>load().then(()=>notice('Supervisor directory refreshed.')).catch(error=>notice(error.message,true)));
    by('searchInput').addEventListener('input',render);by('searchInput').addEventListener('keydown',event=>{if(event.key==='Enter')event.preventDefault();});by('deptFilter').addEventListener('change',render);
    by('addSupervisorModal').addEventListener('hidden.bs.modal',()=>{by('academicPassword').value='';text('academicCopyStatus','');clearCandidate();});
    document.forms[0].addEventListener('submit',event=>{if(event.submitter?.id==='btnLogout')return;event.preventDefault();});
    // Preserve the existing responsive sidebar without demo login interception.
    const sidebar=by('sidebar'),menu=document.querySelector('.menu-btn'),backdrop=document.querySelector('.sidebar-backdrop');
    const toggle=open=>{document.body.classList.toggle('sidebar-open',open);menu.setAttribute('aria-expanded',String(open));};
    menu.addEventListener('click',()=>toggle(!document.body.classList.contains('sidebar-open')));document.querySelector('.sidebar-close').addEventListener('click',()=>toggle(false));backdrop.addEventListener('click',()=>toggle(false));
    load().then(()=>notice('Connected to Firebase. Add supervisors using official staff IDs; no email is sent.')).catch(error=>notice(error.message,true));
})();
