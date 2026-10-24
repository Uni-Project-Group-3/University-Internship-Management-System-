/* Server-rendered Manage Students. No demo identity, records, credentials or mutations. */
(function () {
    'use strict';
    function setup() {
        const by = id => document.getElementById(id);
        let state = {};
        const displayName = value => String(value ?? "").replace(/\s*\(Demo\)/gi, "").replace(/\bDemo\s+/gi, "").trim();
        try { state = JSON.parse(by('studentPageState')?.value || '{}'); } catch (_) { }
        const editor = by('studentEditor');
        const details = by('studentDetailsDialog');
        let posting = false;
        document.forms[0].addEventListener('submit', () => { posting = true; });
        window.addEventListener('beforeunload', event => {
            if (state.unsentDetails && !posting) { event.preventDefault(); event.returnValue = ''; }
        });
        const confirmLeaving = () => !state.unsentDetails || window.confirm('Account details have not been emailed. Close anyway?');
        editor?.addEventListener('cancel', event => { if (!confirmLeaving()) event.preventDefault(); });
        editor?.addEventListener('click', event => {
            const rect = editor.getBoundingClientRect();
            const dismiss = event.target.closest('[data-close]') || (event.target === editor && (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom));
            if (dismiss && !confirmLeaving()) { event.preventDefault(); event.stopImmediatePropagation(); }
        }, true);
        const opener = new WeakMap();
        function open(dialog) {
            if (!dialog || dialog.open) return;
            opener.set(dialog, document.activeElement);
            dialog.showModal();
        }
        document.querySelectorAll('dialog').forEach(dialog => {
            dialog.querySelectorAll('[data-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
            dialog.addEventListener('close', () => opener.get(dialog)?.focus());
            dialog.addEventListener('click', event => {
                if (event.target !== dialog) return;
                const rect = dialog.getBoundingClientRect();
                if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
            });
        });
        const deletion = by('studentDeleteDialog');
        document.querySelectorAll('[data-delete-student]').forEach(button => {
            button.addEventListener('click', () => {
                by('studentDeleteUid').value = button.dataset.deleteStudent;
                by('studentDeleteName').textContent = displayName(button.dataset.studentName);

                open(deletion);
            });
        });
        deletion?.addEventListener('close', () => { by('studentDeleteUid').value = ''; });
        const add = by('addStudentButton');
        if (add) {
            add.disabled = !state.canManage;
            add.addEventListener('click', () => open(editor));
        }
        if (state.openEditor && state.canManage) open(editor);
        if (state.openDetails && state.canManage) {
            details?.querySelectorAll('dd').forEach(el => el.textContent = displayName(el.textContent));
            open(details);
        }

        const department = by('studentDepartmentFilter');
        const supervisor = by('studentSupervisorFilter');
        const completion = by('completionStatusFilter');
        const search = by('completionSearch');
        const rows = [...document.querySelectorAll('#studentsGrid tr[data-student-row]')];
        let page = 0;
        rows.forEach(row => {
            [0,4].forEach(i => { if(row.cells[i]) row.cells[i].textContent = displayName(row.cells[i].textContent); });
            const button = row.querySelector('[data-delete-student]');
            if(button) button.title=button.ariaLabel='Delete '+displayName(button.dataset.studentName);
        });
        supervisor?.querySelectorAll('option').forEach(option=>option.textContent=displayName(option.textContent));
        const storageKey = 'uims.studentDirectory.ui';
        try { const saved=JSON.parse(sessionStorage.getItem(storageKey)||'null'); if(saved) { page=saved.page||0; by('studentPageSize').value=saved.size||'15'; [department,supervisor,completion,search].forEach((el,i)=>{if(el)el.value=saved.filters[i]||'';}); } } catch(_) {}
        const saveUI=()=>{try{sessionStorage.setItem(storageKey,JSON.stringify({page,size:by('studentPageSize').value,filters:[department,supervisor,completion,search].map(el=>el?.value||'')}));}catch(_) {}};
        function filter() {
            const query = search?.value.trim().toLowerCase() || '';
            const visible=rows.filter(row=>(!query || (row.dataset.search || '').includes(query)) &&
                (!department?.value || row.dataset.department===department.value) &&
                (!supervisor?.value || row.dataset.supervisor===supervisor.value) &&
                (!completion?.value || row.dataset.completion===completion.value));
            const size=Number(by('studentPageSize').value)||15,pages=Math.max(1,Math.ceil(visible.length/size));
            page=Math.max(0,Math.min(page,pages-1));
            const current=new Set(visible.slice(page*size,(page+1)*size)); rows.forEach(row=>row.hidden=!current.has(row));
            by('studentPageSummary').textContent=visible.length ? (page*size+1)+'–'+Math.min((page+1)*size,visible.length)+' of '+visible.length+' students' : 'No students';
            by('studentFilterSummary').textContent='';
            by('studentPageNumbers').innerHTML=Array.from({length:pages},(_,i)=>'<button type="button" data-student-page="'+i+'" '+(i===page?'aria-current="page"':'')+'>'+(i+1)+'</button>').join('');
            by('studentPrevious').disabled=page===0;by('studentNext').disabled=page===pages-1;
            if(by('studentNoMatches'))by('studentNoMatches').hidden=rows.length===0||visible.length>0;
            saveUI();
        }
        const resetFilter=()=>{page=0;filter();};
        [department, supervisor, completion].forEach(control => control?.addEventListener('change', resetFilter));
        search?.addEventListener('input', resetFilter);
        search?.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); filter(); } });
        const topSearch = by('dashboardSearch');
        topSearch?.addEventListener('input', () => { if (search) search.value = topSearch.value; resetFilter(); });
        topSearch?.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
        by('completionClearFilters')?.addEventListener('click', () => {
            [department, supervisor, completion, search, topSearch].forEach(control => { if (control) control.value = ''; });
            filter();
        });
        by('studentPageSize').addEventListener('change',resetFilter);
        by('studentPrevious').addEventListener('click',()=>{page--;filter();});
        by('studentNext').addEventListener('click',()=>{page++;filter();});
        by('studentPageNumbers').addEventListener('click',event=>{const button=event.target.closest('[data-student-page]');if(button){page=Number(button.dataset.studentPage);filter();}});
        filter();
        let checking=false;
        async function refreshDirectory(){
            if(checking||document.hidden||document.querySelector('dialog[open]')||!state.canManage)return;
            checking=true;
            try { const url=new URL(location.href);url.searchParams.set('directory','1');
                const response=await fetch(url,{credentials:'same-origin',cache:'no-store'});
                if(!response.ok)throw Error('Directory could not update.');
                const current=await response.json();
                if(!Array.isArray(current))throw Error('Directory could not update.');
                const message=by('studentListMessage');if(message?.dataset.error==='true'){message.textContent='';delete message.dataset.error;}
                if(JSON.stringify(current)!==state.directorySnapshot&&!document.querySelector('dialog[open]')){saveUI();location.reload();}
            } catch(_) { const message=by('studentListMessage');if(message){message.textContent='Automatic update unavailable. Retrying shortly.';message.dataset.error='true';} }
            finally{checking=false;}
        }
        setInterval(refreshDirectory,60000);
        document.addEventListener('visibilitychange',()=>{if(!document.hidden)refreshDirectory();});

        by('studentUniversityId')?.addEventListener('input', () => {
            ['studentName', 'studentDepartment', 'studentEmail', 'studentEmail'].forEach(id => { if (by(id)) by(id).value = ''; });
            if (by('studentLookupMessage')) by('studentLookupMessage').textContent = '';
            if (by('studentPasswordStatus')) by('studentPasswordStatus').textContent = '';
            ['saveStudentButton'].forEach(id => { if (by(id)) by(id).disabled = true; });
        });
        by('studentUniversityId')?.addEventListener('keydown', event => {
            if (event.key === 'Enter') { event.preventDefault(); by('findStudentButton')?.click(); }
        });
        // Buttons keep their ASP.NET postback handlers. Server-side leases handle duplicate requests.
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', setup, {once:true});
    else setup();
})();
