
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
        const company=view.user.role==='company';
        const rich=text=>escape(text).replace(/\*\*([^*]+)\*\*/g,'<strong>$1</strong>').replace(/_([^_]+)_/g,'<em>$1</em>').replace(/\n/g,'<br>');
        function ammanUtc(input){if(!input)return '';const parts=input.split(/[-T:]/).map(Number),target=Date.UTC(parts[0],parts[1]-1,parts[2],parts[3],parts[4]);let guess=target;for(let i=0;i<3;i++){const fields=Object.fromEntries(new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',second:'2-digit',hourCycle:'h23'}).formatToParts(new Date(guess)).filter(p=>p.type!=='literal').map(p=>[p.type,Number(p.value)]));guess+=target-Date.UTC(fields.year,fields.month-1,fields.day,fields.hour,fields.minute,fields.second);}return new Date(guess).toISOString();}

        const uiText={
    "create": "+ Create Task",
    "list": "List view",
    "board": "Board view",
    "group": "Group tasks",
    "drafts": "Drafts",
    "reports": "Weekly reports",
    "clear": "Clear filters",
    "new": "New",
    "progress": "In Progress",
    "review": "In Review",
    "revision": "Revision Requested",
    "completed": "Completed",
    "view": "View",
    "results": "Results",
    "allStudents": "All students",
    "allWeeks": "All weeks",
    "allPriorities": "All priorities",
    "allStatuses": "All statuses",
    "low": "Low",
    "medium": "Medium",
    "high": "High",
    "next": "Next",
    "back": "Back",
    "cancel": "Cancel",
    "save": "Save",
    "submit": "Submit",
    "publish": "Publish result",
    "overview": "Overview",
    "checklist": "Checklist",
    "attachments": "Attachments",
    "exam": "Exam",
    "comments": "Comments",
    "activity": "Activity",
    "start": "Start work",
    "submitWork": "Submit Work",
    "reviewWork": "Review submission",
    "newTask": "Create training task",
    "info": "Task Details",
    "assign": "Assign Students",
    "settings": "Exam Configuration",
    "questions": "Visual Question Builder",
    "summary": "Review & Publish",
    "title": "Task Name",
    "description": "Short Task Instructions (optional)",
    "week": "Training week",
    "priority": "Priority",
    "deadline": "Deadline",
    "category": "Category",
    "criteria": "Evaluation criteria",
    "maximum": "Practical task maximum",
    "startDate": "Start date",
    "links": "Reference links (one per line)",
    "checklistHelp": "Checklist (one item per line)",
    "files": "Attachments · filenames only in demo",
    "selectAll": "Select all",
    "clearSelection": "Clear selection",
    "includeExam": "Add Exam",
    "examTitle": "Exam Name",
    "instructions": "Instructions",
    "count": "Number of questions",
    "marks": "Total marks",
    "passing": "Passing percentage",
    "schedule": "Start date & time · Jordan Time (UTC+3)",
    "end": "Availability closes · Jordan Time (UTC+3)",
    "duration": "Duration (minutes)",
    "attempts": "Allowed attempts",
    "policy": "End-time policy",
    "finish": "Allow started attempts to finish",
    "close": "Stop at availability end",
    "saveDraft": "Save as Draft",
    "createAssign": "Create & Assign Task",
    "question": "Question",
    "addQuestion": "+ Add question",
    "preview": "Preview exam",
    "duplicate": "Duplicate",
    "delete": "Delete",
    "up": "Up",
    "down": "Down",
    "choice": "Choice",
    "addChoice": "+ Add choice",
    "correct": "Correct answer",
    "questionText": "Question text",
    "questionMarks": "Question marks",
    "type": "Type",
    "mcq": "Multiple choice",
    "tf": "True / False",
    "empty": "No matching tasks",
    "overdue": "Overdue",
    "unread": "Unread",
    "pending": "Result pending Company review",
    "startExam": "Start Exam",
    "locked": "Exam locked until scheduled start",
    "ready": "Exam ready · demo clock",
    "expired": "Exam availability expired",
    "hidden": "Hidden",
    "published": "Published",
    "pass": "Pass",
    "fail": "Fail",
    "feedback": "Feedback",
    "sendReport": "Send Weekly Report",
    "student": "Student",
    "history": "Submission history",
    "noExam": "No exam attached",
    "noData": "No records yet",
    "practical": "Practical score",
    "score": "Score",
    "link": "Work link",
    "work": "Work description",
    "file": "File · metadata only",
    "saveComment": "Add comment",
    "confirm": "Confirm",
    "discard": "Discard unsaved changes?",
    "confirmSubmit": "Submit this attempt? Unanswered questions earn zero marks.",
    "revisionAction": "Request revision",
    "grade": "Evaluate practical work",
    "send": "Send report",
    "reportPreview": "Weekly report preview",
    "reportNote": "Snapshots preserve one student’s tasks and hours. Sending to Academic is independent of student grade publication.",
    "openingDate": "Opening Date",
    "openingTime": "Opening Time",
    "closingDate": "Closing Date",
    "closingTime": "Closing Time",
    "completedQuestions": "Completed Questions",
    "remainingQuestions": "Remaining Questions",
    "assignedMarks": "Assigned Marks",
    "remainingMarks": "Remaining Marks",
    "image": "Question image (optional)",
    "imageHint": "Drop JPEG, PNG or WebP here, or choose an image. Maximum 2 MB. Local demo storage only.",
    "chooseImage": "Choose image",
    "replaceImage": "Replace image",
    "removeImage": "Remove image",
    "viewImage": "Full-size image",
    "removeOption": "Remove option",
    "extendExam": "Allow this exam to finish after task closing",
    "examPolicyNote": "The exam window ends after its configured duration. Practical submissions still close at the task closing time.",
    "questionOf": "Question",
    "of": "of",
    "questionInvalid": "Complete the statement, at least two options, one correct answer and positive marks.",
    "marksInvalid": "Question count and assigned marks must match the exam configuration.",
    "windowInvalid": "Opening must be earlier than closing.",
    "examWindowInvalid": "Exam must start during task availability. Allow extension explicitly if it ends after the task.",
    "taskLocked": "Task actions are not available outside the opening and closing period.",
    "imageInvalid": "Choose a valid JPEG, PNG or WebP image up to 2 MB.",
    "imageLoading": "Preparing image…"
};
        const workspaceTimezone='Asia/Amman';
        const validQuestion=q=>!!q.text?.trim()&&q.choices?.length>=2&&q.choices.every(c=>c.trim())&&Number.isInteger(q.correct)&&q.correct>=0&&q.correct<q.choices.length&&Number.isFinite(Number(q.marks))&&Number(q.marks)>0;
        const imageUrlCache=new Map();let imageUploads=0;
        function imageDatabase(){return new Promise((resolve,reject)=>{const request=indexedDB.open('uims.cycle3.images',1);request.onupgradeneeded=()=>request.result.createObjectStore('images');request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(Error('Image storage unavailable.'));});}
        async function imageBlob(id,blob){const db=await imageDatabase();try{return await new Promise((resolve,reject)=>{const tx=db.transaction('images',blob?'readwrite':'readonly'),store=tx.objectStore('images'),request=blob?store.put(blob,id):store.get(id);let result;request.onsuccess=()=>result=request.result;tx.oncomplete=()=>resolve(result);tx.onerror=()=>reject(tx.error);tx.onabort=()=>reject(tx.error);});}finally{db.close();}}
        async function hydrateImages(){for(const img of by('c3DialogBody').querySelectorAll('img[data-image-id]')){try{let url=imageUrlCache.get(img.dataset.imageId);if(!url){const blob=await imageBlob(img.dataset.imageId);if(!blob){img.alt='Image not available in this browser';continue;}url=URL.createObjectURL(blob);imageUrlCache.set(img.dataset.imageId,url);}if(img.isConnected)img.src=url;}catch(err){if(img.isConnected)img.alt=err.message;}}}
        function questionImage(q,editable=false){return '<div class="c3-question-media" '+(editable?'data-image-drop="'+q.id+'"':'')+'>'+(q.image?'<img data-image-id="'+escape(q.image.id)+'" alt="'+escape(ui('image'))+'" loading="lazy" /><div class="c3-actions">'+cbutton(ui('viewImage'),'viewImage',q.image.id)+(editable?cbutton(ui('replaceImage'),'pickImage',q.id)+cbutton(ui('removeImage'),'removeImage',q.id):'')+'</div>':editable?'<div class="c3-image-empty"><i class="bi bi-image" aria-hidden="true"></i><p>'+escape(ui('imageHint'))+'</p>'+cbutton(ui('chooseImage'),'pickImage',q.id)+'</div>':'')+(editable?'<input type="file" accept="image/jpeg,image/png,image/webp" data-image-file="'+q.id+'" hidden /><p data-image-error role="alert"></p>':'')+'</div>';}
        async function uploadQuestionImage(file,questionId){
            if(imageUploads){showError(Error(ui('imageLoading')));return;}
            syncWizard();imageUploads++;by('c3DialogActions').querySelectorAll('button').forEach(el=>el.disabled=true);const q=wizard.exam.questions.find(q=>q.id===questionId),panel=by('c3DialogBody').querySelector('[data-image-drop="'+questionId+'"]');
            const status=panel.querySelector('[data-image-error]');status.textContent=ui('imageLoading');
            try{
                if(!file||file.size>2097152||!['image/jpeg','image/png','image/webp'].includes(file.type))throw Error(ui('imageInvalid'));
                const bytes=new Uint8Array(await file.slice(0,12).arrayBuffer());
                const mime=bytes[0]===255&&bytes[1]===216&&bytes[2]===255?'image/jpeg':bytes[0]===137&&bytes[1]===80&&bytes[2]===78&&bytes[3]===71&&bytes[4]===13&&bytes[5]===10&&bytes[6]===26&&bytes[7]===10?'image/png':String.fromCharCode(...bytes.slice(0,4))==='RIFF'&&String.fromCharCode(...bytes.slice(8,12))==='WEBP'?'image/webp':'';
                if(mime!==file.type)throw Error(ui('imageInvalid'));
                const bitmap=await createImageBitmap(file);if(bitmap.width*bitmap.height>40000000){bitmap.close();throw Error(ui('imageInvalid'));}
                const scale=Math.min(1,1600/Math.max(bitmap.width,bitmap.height)),canvas=document.createElement('canvas');canvas.width=Math.round(bitmap.width*scale);canvas.height=Math.round(bitmap.height*scale);canvas.getContext('2d').drawImage(bitmap,0,0,canvas.width,canvas.height);bitmap.close();
                const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/webp',.86));if(!blob)throw Error(ui('imageInvalid'));
                
                const image=demo.transaction('c3RegisterImage',{mime:blob.type,size:blob.size,width:canvas.width,height:canvas.height});
                await imageBlob(image.id,blob);syncWizard();wizard.exam.questions.find(item=>item.id===questionId).image=image;wizardScreen();modified=true;
            }catch(err){status.textContent=err.message;}finally{imageUploads--;by('c3DialogActions').querySelectorAll('button').forEach(el=>el.disabled=imageUploads>0);}
        }

        let list=false,mode='',tab='overview',targetId='',wizard=null,step=0,questionIndex=0,modified=false,opener=null,previewBack=null;
        const ui=key=>uiText[key]||key;
        const labels=()=>[['assigned',ui('new')],['in-progress',ui('progress')],['submitted',ui('review')],['revision',ui('revision')],['completed',ui('completed')]];
        const cbutton=(label,action,id='',primary=false)=>'<button type="button" data-c3="'+action+'" data-id="'+escape(id)+'"'+(primary?' class="c3-primary"':'')+'>'+escape(label)+'</button>';
        const badge=(label,kind='')=>'<span class="c3-badge '+kind+'">'+escape(label)+'</span>';
        const date=stamp=>stamp?new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',year:'numeric',month:'short',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).format(new Date(stamp)):'—';
        const field=(name,label,val='',type='text',extra='')=>'<label class="c3-field '+(type==='textarea'?'wide':'')+'">'+escape(ui(label))+(type==='textarea'?'<textarea name="'+name+'" '+extra+'>'+escape(val)+'</textarea>':'<input name="'+name+'" type="'+type+'" value="'+escape(val)+'" '+extra+' />')+'</label>';
        const currentTask=()=>view.tasks.find(t=>t.id===targetId);
        const groupOf=t=>view.taskGroups.find(g=>g.id===t?.groupId);
        const studentName=id=>view.users.find(u=>u.id===id)?.name||id;
        const initials=name=>(name||'?').split(' ').slice(0,2).map(x=>x[0]).join('');
        const getValue=name=>by('c3DialogBody').querySelector('[name="'+name+'"]')?.value||'';
        const lines=text=>text.split('\n').map(s=>s.trim()).filter(Boolean);
        function refresh(){view=demo.view();if(!view||view.user.mustChangePassword){location.href='../Auth/Login.aspx';return false;}return true;}
        function act(action,payload){const result=demo.transaction(action,payload);refresh();render();return result;}
        function showError(err){by('c3Error').hidden=false;by('c3Error').textContent=err.message;by('c3Error').scrollIntoView({block:'nearest'});}
        function modal(title,html,buttons,newMode=mode){
            const dialog=by('c3Dialog');if(!dialog.open)opener=document.activeElement;
            mode=newMode;by('c3DialogTitle').textContent=title;by('c3DialogBody').innerHTML=html;by('c3DialogActions').innerHTML=buttons;by('c3Error').hidden=true;dialog.dir='ltr';modified=false;
            if(!dialog.open)dialog.showModal();
            queueMicrotask(hydrateImages);by('c3DialogBody').scrollTop=0;
            by('c3DialogBody').querySelector('input,textarea,button,select')?.focus();
            if(newMode==='details'&&tab==='exam')queueMicrotask(tick);
        }
        function confirmAction(message){return new Promise(resolve=>{const d=by('c3Confirm');by('c3ConfirmText').textContent=message;by('c3ConfirmTitle').textContent=ui('confirm');by('c3ConfirmCancel').textContent=ui('cancel');by('c3ConfirmYes').textContent=ui('confirm');d.dir='ltr';d.showModal();let settled=false;const finish=result=>{if(settled)return;settled=true;d.close();resolve(result);};by('c3ConfirmYes').onclick=()=>finish(true);by('c3ConfirmCancel').onclick=()=>finish(false);d.oncancel=e=>{e.preventDefault();finish(false);};});}
        async function dismiss(){if(imageUploads){showError(Error(ui('imageLoading')));return;}if(modified&&!await confirmAction(ui('discard')))return;by('c3Dialog').close();mode='';modified=false;opener?.focus();}
        on('c3Close','click',dismiss);on('c3Dialog','cancel',e=>{e.preventDefault();dismiss();});
        on('c3DialogBody','input',()=>modified=true);on('c3DialogBody','change',()=>modified=true);
        window.addEventListener('beforeunload',event=>{if(modified){event.preventDefault();event.returnValue='';}},{signal});
        const available=view.progress.filter(p=>p.placement?.completionStatus==='InTraining'&&p.status==='active');
        function renderControls(){
            options('c3Student',available.map(p=>({id:p.id,name:p.name})),ui('allStudents'));by('c3Student').hidden=!company;
            options('c3Week',[...new Set(view.tasks.map(t=>String(t.week||1)))].sort((a,b)=>Number(a)-Number(b)).map(id=>({id,name:ui('week')+' '+id})),ui('allWeeks'));
            options('c3Priority',['low','medium','high'].map(id=>({id,name:ui(id)})),ui('allPriorities'));
            options('c3Status',labels().map(([id,name])=>({id,name})),ui('allStatuses'));
            by('c3Create').hidden=!company;by('c3Drafts').hidden=!company;by('c3Reports').hidden=!company;by('c3GroupLabel').hidden=!company;
            document.querySelectorAll('[data-t]').forEach(el=>el.textContent=ui(el.dataset.t));
            text('c3Heading',company?('Training Tasks Board'):('My Training Tasks'));
            text('c3Subtitle','Focused work, independent submissions and thoughtful feedback.');
            document.documentElement.lang='en';document.documentElement.dir='ltr';
            by('cycle3Workspace').dir='ltr';
            by('taskSearch').placeholder='Search tasks…';by('c3View').textContent=ui(list?'board':'list');
            document.querySelector('.user-chip .name').textContent=view.user.name;
        }
        render=()=>{
            if(!view)return;renderControls();
            const search=value('taskSearch').toLowerCase();let tasks=view.tasks.filter(t=>(!search||(t.title+' '+studentName(t.studentId)).toLowerCase().includes(search))&&(!value('c3Student')||t.studentId===value('c3Student'))&&(!value('c3Week')||String(t.week||1)===value('c3Week'))&&(!value('c3Priority')||(t.priority||'medium')===value('c3Priority'))&&(!value('c3Status')||t.status===value('c3Status'))).sort((a,b)=>(Date.parse(a.deadline)-Date.parse(b.deadline))*(value('c3Sort')==='desc'?-1:1));
            by('c3Stats').innerHTML=[[view.tasks.length,'Assignments'],[view.tasks.filter(t=>t.status==='submitted').length,ui('review')],[view.tasks.filter(t=>t.status==='revision').length,ui('revision')],[view.tasks.filter(t=>t.status==='completed').length,ui('completed')]].map(([n,label])=>'<div class="c3-stat"><span>'+escape(label)+'</span><strong>'+n+'</strong></div>').join('');
            if(company&&by('c3Group').checked){const groups=new Map();tasks.forEach(t=>{const key=(t.groupId||t.id)+':'+t.status;if(!groups.has(key))groups.set(key,[]);groups.get(key).push(t);});tasks=[...groups.values()].map(items=>({...items[0],members:items}));}
            by('taskList').classList.toggle('c3-list',list);
            by('taskList').innerHTML=labels().map(([status,label])=>{const records=tasks.filter(t=>t.status===status);return '<section class="c3-column" data-status="'+status+'" aria-label="'+escape(label)+'"><h2>'+escape(label)+' '+badge(records.length)+'</h2>'+records.map(t=>{
                const g=groupOf(t),overdue=t.status!=='completed'&&Date.parse(t.deadline)<Date.parse(demo.now()),completed=t.checklist?.filter(c=>c.done).length||0;
                return '<article class="c3-card" draggable="'+(!t.members&&['assigned','in-progress'].includes(t.status))+'" data-task="'+t.id+'"><div class="c3-actions">'+badge(ui(t.priority||'medium'),t.priority||'medium')+badge(ui('week')+' '+(t.week||1))+'</div><h3>'+escape(t.title)+'</h3><p>'+escape(date(t.deadline))+'</p>'+(overdue?badge(ui('overdue'),'high'):'')+(g?.exam?' '+badge(ui('exam')):'')+(g?.attachments.length?' '+badge('↗ '+g.attachments.length):'')+(t.status==='completed'?' '+badge(ui(t.gradePublished||!t.groupId?'published':'hidden')):'')+'<p>'+escape(t.members?t.members.length+' '+ui('student'):studentName(t.studentId))+'</p><p>☑ '+completed+' / '+(t.checklist?.length||0)+(t.readAt||company?'':' · '+ui('unread'))+'</p><div class="c3-actions"><span class="c3-initials">'+escape(initials(studentName(t.studentId)))+'</span>'+cbutton(ui('view'),'details',t.id)+ (company?cbutton(ui('results'),'results',t.groupId||t.id):'')+'</div></article>';
            }).join('')+(!records.length?'<p class="c3-empty">'+escape(ui('empty'))+'</p>':'')+'</section>';}).join('');
            by('noTasksMsg').hidden=tasks.length>0;
        };
        function beginWizard(draft){
            wizard=draft?JSON.parse(JSON.stringify(draft)):{requestId:crypto.randomUUID(),studentIds:[],priority:'medium',week:1,maximum:10,checklist:[],links:[],attachments:[],exam:null};
            wizard.opensAt ||= wizard.startDate?new Date(wizard.startDate).toISOString():'';
            wizard.closesAt ||= wizard.deadline?new Date(wizard.deadline).toISOString():'';
            wizard.timezone ||= workspaceTimezone;
            if(draft)wizard.draftId=draft.id;
            step=0;wizardScreen();
        }
        function syncWizard(){
            if(mode!=='wizard')return;
            if(step===0){
                wizard.title=getValue('title');wizard.description=getValue('description');
                wizard.opensAt=getValue('open_date')&&getValue('open_time')?ammanUtc(getValue('open_date')+'T'+getValue('open_time')):'';
                wizard.closesAt=getValue('close_date')&&getValue('close_time')?ammanUtc(getValue('close_date')+'T'+getValue('close_time')):'';
                wizard.deadline=wizard.closesAt;wizard.startDate=wizard.opensAt;
            }
            if(step===1)wizard.studentIds=[...by('c3DialogBody').querySelectorAll('[data-student]:checked')].map(el=>el.dataset.student);
            if(step===2){
                if(!by('c3IncludeExam').checked){wizard.examDraft=wizard.exam||wizard.examDraft;wizard.exam=null;}
                else{const e=wizard.exam||wizard.examDraft||{questions:[]};for(const key of ['title','questionCount','totalMarks','passing','duration','attempts'])e[key]=getValue('exam_'+key);
                    e.startsAt=getValue('exam_date')&&getValue('exam_time')?ammanUtc(getValue('exam_date')+'T'+getValue('exam_time')):'';
                    e.endsAt=e.startsAt?new Date(Date.parse(e.startsAt)+Number(e.duration)*60000).toISOString():'';e.policy='close';wizard.allowExamAfterClosing=by('c3ExtendExam').checked;wizard.exam=e;
                }
            }
            if(step===3&&wizard.exam){wizard.exam.questions=[...by('c3DialogBody').querySelectorAll('[data-question]')].map(el=>({id:el.dataset.question,type:el.querySelector('[data-qtype]').value,text:el.querySelector('[data-qtext]').value,marks:Number(el.querySelector('[data-qmarks]').value),choices:[...el.querySelectorAll('[data-choice]')].map(c=>c.value),correct:Number(el.querySelector('input[type=radio]:checked')?.value??-1),image:wizard.exam.questions.find(q=>q.id===el.dataset.question)?.image||null}));}
        }
        const ammanInput=iso=>iso?new Intl.DateTimeFormat('sv-SE',{timeZone:'Asia/Amman',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hour12:false}).format(new Date(iso)).replace(' ','T'):'';
        const selectField=(name,label,val,records)=>'<label class="c3-field">'+escape(ui(label))+'<select name="'+name+'">'+records.map(([id,label])=>'<option value="'+id+'"'+(String(val)===id?' selected':'')+'>'+escape(label)+'</option>').join('')+'</select></label>';
        function wizardScreen(){
            const activeSteps=wizard.exam?[0,1,2,3,4]:[0,1,2,4];
            let body='<nav class="c3-steps" aria-label="'+escape(ui('newTask'))+'">'+['info','assign','settings','questions','summary'].map((key,index)=>'<span class="'+(index===step?'active':'')+(index===3&&!wizard.exam?' optional':'')+'" '+(index===step?'aria-current="step"':'')+'><b>'+(index+1)+'</b>'+escape(ui(key))+'</span>').join('')+'</nav>';
            const dates=(prefix,stamp,opening)=>{const local=ammanInput(stamp).split('T');return field(prefix+'_date',opening?'openingDate':'closingDate',local[0]||'','date','required')+field(prefix+'_time',opening?'openingTime':'closingTime',local[1]||'','time','required');};
            if(step===0)body+='<div class="c3-studio-intro"><span class="c3-studio-icon"><i class="bi bi-clipboard-check" aria-hidden="true"></i></span><div><h3>'+escape(ui('info'))+'</h3><p>'+escape('A clear brief and a focused availability window.')+'</p></div></div><div class="c3-grid c3-task-fields">'+field('title','title',wizard.title,'text','required maxlength="160"')+field('description','description',wizard.description,'textarea','maxlength="3000"')+'<div class="c3-window-group">'+dates('open',wizard.opensAt,true)+dates('close',wizard.closesAt,false)+'</div></div>';
            if(step===1)body+='<input id="c3TraineeSearch" type="search" placeholder="'+escape(ui('allStudents'))+'" aria-label="'+escape(ui('student'))+'" /><div class="c3-actions">'+cbutton(ui('selectAll'),'selectAll')+cbutton(ui('clearSelection'),'clearSelection')+'</div><div id="c3SelectedChips" class="c3-chips"></div><div class="c3-trainees">'+available.map(p=>'<label data-trainee-label="'+escape((p.name+' '+p.universityId).toLowerCase())+'"><input type="checkbox" data-student="'+p.id+'" '+(wizard.studentIds.includes(p.id)?'checked':'')+' /><span class="c3-initials">'+escape(initials(p.name))+'</span><span>'+escape(p.name)+'<small>'+escape(p.universityId+' · '+p.department)+'</small></span></label>').join('')+'</div>'+(!available.length?'<p>'+escape(ui('noData'))+'</p>':'');
            if(step===2){const e=wizard.exam||wizard.examDraft||{title:'',questionCount:1,totalMarks:100,passing:50,duration:30,attempts:1,questions:[]},local=ammanInput(e.startsAt).split('T');body+='<label class="c3-exam-toggle"><input type="checkbox" id="c3IncludeExam" '+(wizard.exam?'checked':'')+' /><span><strong>'+escape(ui('includeExam'))+'</strong><small>'+escape(ui('examPolicyNote'))+'</small></span></label><div id="c3ExamFields" '+(!wizard.exam?'hidden':'')+'><div class="c3-grid">'+field('exam_title','examTitle',e.title,'text','required')+field('exam_duration','duration',e.duration,'number','min="1" max="240" required')+field('exam_date','openingDate',local[0]||'','date','required')+field('exam_time','openingTime',local[1]||'','time','required')+field('exam_questionCount','count',e.questionCount,'number','min="1" max="100" required')+field('exam_totalMarks','marks',e.totalMarks,'number','min="1" required')+field('exam_passing','passing',e.passing,'number','min="1" max="100" required')+field('exam_attempts','attempts',e.attempts,'number','min="1" max="5" required')+'</div><label class="c3-extend"><input type="checkbox" id="c3ExtendExam" '+(wizard.allowExamAfterClosing?'checked':'')+' />'+escape(ui('extendExam'))+'</label></div>';}
            if(step===3)body+=questionBuilder();
            if(step===4)body+=summaryHtml(wizard);
            body='<div class="c3-studio-meta"><span>'+escape(company?view.user.name:'')+'</span><span>Jordan Time (UTC+3)</span></div>'+body;
            modal(ui('newTask'),body,cbutton(ui('cancel'),'dismiss')+cbutton(ui('saveDraft'),'saveDraft')+(step?cbutton(ui('back'),'previousStep'):'')+(step<4?cbutton(ui('next'),'nextStep','',true):cbutton(ui('createAssign'),'createTask','',true)),'wizard');
            modified=true;if(step===1)updateChips();hydrateImages();
        }
        function updateChips(){by('c3SelectedChips').innerHTML=[...by('c3DialogBody').querySelectorAll('[data-student]:checked')].map(el=>badge(studentName(el.dataset.student))).join('');}
        function questionBuilder(){
            const e=wizard.exam,completed=e.questions.filter(validQuestion).length,assigned=e.questions.reduce((sum,q)=>sum+(Number(q.marks)||0),0);
            const metrics=[[ui('completedQuestions'),completed],[ui('remainingQuestions'),Math.max(0,Number(e.questionCount)-completed)],[ui('assignedMarks'),assigned],[ui('remainingMarks'),Number(e.totalMarks)-assigned]];
            return '<div class="c3-builder-summary">'+metrics.map(([label,n])=>'<div><small>'+escape(label)+'</small><strong>'+n+'</strong></div>').join('')+'</div><div class="c3-builder-tools">'+cbutton(ui('addQuestion'),'addQuestion')+cbutton(ui('preview'),'previewExam')+'</div>'+e.questions.map((q,index)=>'<article class="c3-question" data-question="'+q.id+'"><div class="c3-question-top"><span class="c3-question-number">'+(index+1)+'</span><strong>'+escape(ui('questionOf'))+' '+(index+1)+' '+escape(ui('of'))+' '+e.questionCount+'</strong><div class="c3-actions">'+cbutton(ui('duplicate'),'duplicateQuestion',q.id)+cbutton('↑','questionUp',q.id)+cbutton('↓','questionDown',q.id)+cbutton(ui('delete'),'deleteQuestion',q.id)+'</div></div><div class="c3-question-layout"><div><label class="c3-field">'+escape(ui('type'))+'<select data-qtype><option value="mcq" '+(q.type==='mcq'?'selected':'')+'>'+escape(ui('mcq'))+'</option><option value="tf" '+(q.type==='tf'?'selected':'')+'>'+escape(ui('tf'))+'</option></select></label><label class="c3-field">'+escape(ui('questionText'))+'<textarea data-qtext required placeholder="'+escape(ui('questionText'))+'">'+escape(q.text)+'</textarea></label><label class="c3-field">'+escape(ui('questionMarks'))+'<input data-qmarks type="number" min="0.01" step="0.01" value="'+q.marks+'" required /></label></div>'+questionImage(q,true)+'</div><div class="c3-options"><small>'+escape(ui('correct'))+'</small>'+q.choices.map((c,i)=>'<div class="c3-choice-row"><label class="c3-choice"><input type="radio" name="correct_'+q.id+'" aria-label="'+escape(ui('correct'))+' '+(i+1)+'" value="'+i+'" '+(q.correct===i?'checked':'')+' /><span>'+String.fromCharCode(65+i)+'</span><input type="text" data-choice value="'+escape(c)+'" '+(q.type==='tf'?'readonly':'')+' aria-label="'+escape(ui('choice'))+' '+(i+1)+'" required /></label>'+(q.type==='mcq'?'<button type="button" data-c3="removeChoice" data-id="'+q.id+':'+i+'" aria-label="'+escape(ui('removeOption'))+'" '+(q.choices.length<=2?'disabled':'')+'>×</button>':'')+'</div>').join('')+(q.type==='mcq'?cbutton(ui('addChoice'),'addChoice',q.id):'')+'</div><p class="c3-question-error" data-question-error role="status">'+(!validQuestion(q)?escape(ui('questionInvalid')):'')+'</p></article>').join('');
        }
        function updateBuilderMetrics(){
            if(mode!=='wizard'||step!==3)return;syncWizard();const e=wizard.exam,done=e.questions.filter(validQuestion).length,marks=e.questions.reduce((sum,q)=>sum+(Number(q.marks)||0),0);
            const values=[done,Math.max(0,Number(e.questionCount)-done),marks,Number(e.totalMarks)-marks];by('c3DialogBody').querySelectorAll('.c3-builder-summary strong').forEach((el,index)=>el.textContent=values[index]);
            by('c3DialogBody').querySelectorAll('[data-question]').forEach(el=>{const q=e.questions.find(q=>q.id===el.dataset.question);el.querySelector('[data-question-error]').textContent=validQuestion(q)?'':ui('questionInvalid');});
        }
        function validateStudio(){
            if(!wizard.opensAt||!wizard.closesAt||Date.parse(wizard.opensAt)>=Date.parse(wizard.closesAt))throw Error(ui('windowInvalid'));
            if(wizard.exam&&step>=2){const e=wizard.exam;if(!e.startsAt||Date.parse(e.startsAt)<Date.parse(wizard.opensAt)||Date.parse(e.startsAt)>=Date.parse(wizard.closesAt)||!wizard.allowExamAfterClosing&&Date.parse(e.endsAt)>Date.parse(wizard.closesAt))throw Error(ui('examWindowInvalid'));
                if(step>=3&&(e.questions.length!==Number(e.questionCount)||e.questions.some(q=>!validQuestion(q))||Math.abs(e.questions.reduce((sum,q)=>sum+Number(q.marks),0)-Number(e.totalMarks))>0.00001)){
                    by('c3DialogBody').querySelectorAll('[data-question]').forEach(el=>{const q=e.questions.find(q=>q.id===el.dataset.question);el.querySelector('[data-question-error]').textContent=validQuestion(q)?'':ui('questionInvalid');});throw Error(ui('marksInvalid'));
                }
            }
        }
        function summaryHtml(g){return '<div class="c3-publish-summary"><span class="c3-studio-icon"><i class="bi bi-check2-circle" aria-hidden="true"></i></span><h3>'+escape(g.title)+'</h3><p>'+escape(g.description||'')+'</p><dl><div><dt>'+escape(ui('student'))+'</dt><dd>'+g.studentIds.length+'</dd></div><div><dt>'+escape(ui('openingDate'))+'</dt><dd>'+escape(date(g.opensAt))+'</dd></div><div><dt>'+escape(ui('closingDate'))+'</dt><dd>'+escape(date(g.closesAt))+'</dd></div></dl></div>'+(g.exam?'<div class="c3-panel"><h3>'+escape(g.exam.title)+'</h3><p>'+g.exam.questions.length+' '+escape(ui('questions'))+' · '+g.exam.totalMarks+' '+escape(ui('marks'))+' · '+g.exam.duration+' '+escape(ui('duration'))+'</p><p>'+escape(date(g.exam.startsAt))+' — '+escape(date(g.exam.endsAt))+' · '+g.exam.passing+'%</p>'+(g.allowExamAfterClosing?'<p>'+escape(ui('extendExam'))+'</p>':'')+'</div>':'<p>'+escape(ui('noExam'))+'</p>');}
        function openDetails(id,selectedTab='overview'){
            refresh();targetId=id;tab=selectedTab;const t=currentTask();if(!t)throw Error('Assignment unavailable.');if(!company)act('c3Read',{id});const g=groupOf(t),attempt=view.examAttempts.filter(a=>a.taskId===id).at(-1);
            let content='';
            if(tab==='overview')content='<div class="c3-panel">'+(t.opensAt?'<p>'+escape(ui('openingDate'))+': '+escape(date(t.opensAt))+' · '+escape(ui('closingDate'))+': '+escape(date(t.closesAt))+'</p>':'')+'<p>'+rich(t.description)+'</p><p>'+escape(ui('student'))+': '+escape(studentName(t.studentId))+'</p><p>'+escape(view.companies.find(c=>c.id===t.companyId)?.name)+' · '+escape(studentName(t.assignedBy||view.users.find(u=>u.role==='company'&&u.companyId===t.companyId)?.id))+'</p><p>'+escape(ui('deadline'))+': '+escape(date(t.deadline))+' · '+escape(ui('week'))+' '+(t.week||1)+'</p><p>'+escape(t.criteria)+'</p><p>'+escape(ui('practical'))+': '+(t.score===undefined?'—':escape(t.score)+' / '+t.maximum)+'</p><p>'+escape(t.feedback||'')+'</p></div>'+submissionHistory(t);
            if(tab==='checklist')content=(t.checklist||[]).map((c,index)=>'<label class="c3-answer"><input type="checkbox" data-check-index="'+index+'" '+(c.done?'checked':'')+' '+(company?'disabled':'')+' />'+escape(c.text)+'</label>').join('')||'<p>'+escape(ui('noData'))+'</p>';
            if(tab==='attachments')content=(g?.attachments||[]).map(a=>'<p>↗ '+escape(a.name)+' · '+Math.round(a.size/1024)+' KB <small>'+escape(ui('file'))+'</small></p>').join('')+(g?.links||[]).filter(url=>/^https?:\/\//i.test(url)).map(url=>'<p><a href="'+escape(url)+'" target="_blank" rel="noopener noreferrer">'+escape(url)+'</a></p>').join('')||'<p>'+escape(ui('noData'))+'</p>';
            if(tab==='comments')content='<ol class="c3-history">'+view.taskActivity.filter(a=>a.taskId===id&&a.message).map(a=>'<li><strong>'+escape(studentName(a.authorId))+'</strong> · '+escape(date(a.at))+'<p>'+escape(a.message)+'</p></li>').join('')+'</ol>'+field('comment','comments','','textarea')+cbutton(ui('saveComment'),'comment');
            if(tab==='activity')content='<ol class="c3-history">'+view.taskActivity.filter(a=>a.taskId===id).map(a=>'<li>'+escape(date(a.at))+' · '+escape(a.message)+'</li>').join('')+'</ol>';
            if(tab==='exam')content=examPanel(t,g,attempt);
            if(!company&&(t.opensAt&&Date.parse(demo.now())<Date.parse(t.opensAt)||t.closesAt&&Date.parse(demo.now())>=Date.parse(t.closesAt)))content+='<p class="c3-window-notice">'+escape(ui('taskLocked'))+'</p>';
            const tools=!company&&(!t.opensAt||Date.parse(demo.now())>=Date.parse(t.opensAt))&&(!t.closesAt||Date.parse(demo.now())<Date.parse(t.closesAt))&&['assigned','in-progress','revision'].includes(t.status)?(t.status==='assigned'?cbutton(ui('start'),'startWork',id):'')+cbutton(ui('submitWork'),'submission',id,true):company&&t.status==='submitted'?cbutton(ui('reviewWork'),'review',id,true):'';
            modal(t.title,'<div class="c3-tabs">'+['overview','checklist','attachments','exam','comments','activity'].map(key=>'<button type="button" class="'+(tab===key?'active':'')+'" data-c3="tab" data-id="'+key+'">'+escape(ui(key))+'</button>').join('')+'</div>'+content,cbutton(ui('cancel'),'dismiss')+tools,'details');
        }
        function submissionHistory(t){return '<h3>'+escape(ui('history'))+'</h3>'+view.submissions.filter(s=>s.taskId===t.id).map(s=>'<div class="c3-panel"><small>'+escape(date(s.createdAt))+'</small><p>'+escape(s.description)+'</p><p>'+escape(s.fileName)+'</p>'+(s.link&&/^https?:\/\//i.test(s.link)?'<a href="'+escape(s.link)+'" target="_blank" rel="noopener noreferrer">'+escape(s.link)+'</a>':'')+'</div>').join('');}
        function examPanel(t,g,attempt){if(!g?.exam)return '<p>'+escape(ui('noExam'))+'</p>';const e=g.exam;return '<div class="c3-panel"><h3>'+escape(e.title)+'</h3><p>'+escape(e.instructions)+'</p><div class="c3-grid"><p>'+escape(ui('count'))+': '+e.questionCount+' · '+escape(ui('marks'))+': '+e.totalMarks+'</p><p>'+escape(ui('passing'))+': '+e.passing+'%</p><p>'+escape(ui('schedule'))+': '+escape(date(e.startsAt))+'</p><p>'+escape(ui('duration'))+': '+e.duration+'</p></div><div id="c3ExamState" role="status"></div><div class="c3-countdown" id="c3Countdown" aria-label="Exam countdown"></div>'+(attempt?.status==='submitted'?'<p>'+escape(attempt.published||company?(ui(attempt.passed?'pass':'fail')+' · '+attempt.score+' / '+attempt.maximum+' · '+attempt.percentage.toFixed(1)+'%'):ui('pending'))+'</p>':'')+(!company?'<button type="button" id="c3StartExam" data-c3="startExam" class="c3-primary">'+escape(ui('startExam'))+'</button>':'')+'</div>';}
        function examScreen(){
            refresh();const t=currentTask(),attempt=view.examAttempts.filter(a=>a.taskId===t.id&&a.status==='active').at(-1);
            if(!attempt){openDetails(t.id,'exam');return;}
            const questions=attempt.questions;questionIndex=Math.max(0,Math.min(questionIndex,questions.length-1));const q=questions[questionIndex];
            modal(groupOf(t).exam.title,'<div class="c3-actions"><span>'+escape(ui('question'))+' '+(questionIndex+1)+' / '+questions.length+'</span><strong class="c3-countdown" id="c3AttemptTimer"></strong></div><div class="c3-exam-layout"><nav class="c3-navigator" aria-label="Questions">'+questions.map((q,i)=>'<button type="button" data-c3="goQuestion" data-id="'+i+'" class="'+(attempt.answers[q.id]!==undefined?'answered ':'')+(i===questionIndex?'active':'')+'">'+(i+1)+'</button>').join('')+'</nav><section><h3>'+escape(q.text)+'</h3>'+questionImage(q)+badge(q.marks+' '+ui('marks'))+q.choices.map((choice,i)=>'<label class="c3-answer"><input type="radio" name="examAnswer" data-answer="'+i+'" data-question-id="'+q.id+'" '+(attempt.answers[q.id]===i?'checked':'')+' />'+escape(choice)+'</label>').join('')+'<p class="c3-muted">'+('Answers are saved to local demo storage as you select them.')+'</p></section></div>',cbutton(ui('back'),'prevQuestion')+cbutton(ui('next'),'nextQuestion')+cbutton(ui('submit'),'submitExam','',true),'exam');
        }
        function results(id){refresh();const tasks=view.tasks.filter(t=>t.groupId===id||t.id===id);modal(ui('results'),'<div class="c3-results">'+tasks.map(t=>{const attempts=view.examAttempts.filter(a=>a.taskId===t.id&&a.status==='submitted');return '<article class="c3-result"><span class="c3-initials">'+escape(initials(studentName(t.studentId)))+'</span><h3>'+escape(studentName(t.studentId))+'</h3><p>'+escape(labels().find(([s])=>s===t.status)?.[1])+'</p><p>'+escape(ui('practical'))+': '+(t.score??'—')+' / '+t.maximum+'</p>'+attempts.map(a=>'<p>'+escape(ui('exam'))+': '+a.score+' / '+a.maximum+' · '+a.percentage.toFixed(1)+'% · '+escape(ui(a.passed?'pass':'fail'))+'</p>').join('')+badge(ui(t.gradePublished?'published':'hidden'))+'<div class="c3-actions">'+cbutton(ui('reviewWork'),'review',t.id)+cbutton(ui('publish'),'publish',t.id)+'</div></article>';}).join('')+'</div>',cbutton(ui('cancel'),'dismiss'),'results');}
        function review(id){targetId=id;const t=currentTask(),g=groupOf(t);const attempts=view.examAttempts.filter(a=>a.taskId===id&&a.status==='submitted');modal(ui('reviewWork')+' · '+studentName(t.studentId),submissionHistory(t)+'<div class="c3-panel">'+(t.status==='submitted'?field('score','score',t.score??'','number','min="0" max="'+t.maximum+'" required')+field('feedback','feedback',t.feedback||'','textarea','required'):'<p>'+escape(ui('practical'))+': '+(t.score??'—')+' / '+t.maximum+'</p><p>'+escape(t.feedback||'')+'</p>')+'</div>'+attempts.map(a=>'<div class="c3-panel"><h3>'+escape(g?.exam?.title)+' · '+a.score+' / '+a.maximum+' · '+escape(ui(a.passed?'pass':'fail'))+'</h3><p>'+escape(date(a.submittedAt))+'</p>'+(g?.exam?.questions||[]).map(q=>'<p><strong>'+escape(q.text)+'</strong><br>'+escape(ui('choice'))+': '+escape(q.choices[a.answers[q.id]]??'—')+' · '+escape(ui('correct'))+': '+escape(q.choices[q.correct])+'</p>').join('')+'</div>').join(''),cbutton(ui('cancel'),'dismiss')+(t.status==='submitted'?cbutton(ui('revisionAction'),'requestRevision',id)+cbutton(ui('grade'),'grade',id,true):'')+cbutton(ui('publish'),'publish',id),'review');}
        function reportScreen(){modal(ui('reportPreview'),'<p>'+escape(ui('reportNote'))+'</p><div class="c3-grid">'+selectField('reportStudent','student',available[0]?.id||'',available.map(p=>[p.id,p.name]))+field('reportWeek','week',1,'number','min="1" max="52"')+field('reportFeedback','feedback','','textarea','required')+'</div><div id="c3ReportPreview"></div>'+view.weeklyReports.map(r=>'<div class="c3-panel"><strong>'+escape(studentName(r.studentId))+' · '+escape(ui('week'))+' '+r.week+'</strong><p>'+escape(date(r.sentAt))+' · '+escape(r.status)+'</p><p>'+escape(r.feedback)+'</p><p>'+escape(r.academicFeedback||'')+'</p></div>').join(''),cbutton(ui('cancel'),'dismiss')+cbutton(ui('preview'),'reportPreview')+cbutton(ui('send'),'sendReport','',true),'report');}
        function reportPreview(){const studentId=getValue('reportStudent'),week=Number(getValue('reportWeek')),p=view.progress.find(p=>p.id===studentId),tasks=view.tasks.filter(t=>t.studentId===studentId&&Number(t.week||1)===week);if(!p)throw Error('Choose a trainee.');return '<div class="c3-panel"><h3>'+escape(p.name)+'</h3><p>'+escape(p.supervisor?.name||'No Academic Supervisor assigned')+' · '+p.approvedHours+' approved hours (all dates)</p>'+tasks.map(t=>'<p>'+escape(t.title)+' · '+escape(t.status)+' · '+(t.score??'—')+' / '+t.maximum+'</p>'+view.examAttempts.filter(a=>a.taskId===t.id&&a.status==='submitted').map(a=>'<p>'+escape(ui('exam'))+': '+a.score+' / '+a.maximum+' · '+escape(ui(a.passed?'pass':'fail'))+'</p>').join('')).join('')+'<p>'+escape(getValue('reportFeedback'))+'</p></div>';}
        const handlers={
            removeChoice:id=>{syncWizard();const [qid,index]=id.split(':');const q=wizard.exam.questions.find(q=>q.id===qid);if(q.choices.length<=2)return;q.choices.splice(Number(index),1);q.correct=q.correct===Number(index)?-1:q.correct>Number(index)?q.correct-1:q.correct;wizardScreen();},
            pickImage:id=>by('c3DialogBody').querySelector('[data-image-file="'+id+'"]').click(),removeImage:id=>{syncWizard();wizard.exam.questions.find(q=>q.id===id).image=null;wizardScreen();},viewImage:async id=>{const blob=await imageBlob(id);if(!blob)throw Error('Image unavailable.');const image=by('c3ImageViewer');image.querySelector('img').src=URL.createObjectURL(blob);image.showModal();},
            formatBold:()=>formatDescription('**','**'),formatItalic:()=>formatDescription('_','_'),formatBullet:()=>formatDescription('\n- ',''),
            dismiss,details:id=>openDetails(id),tab:key=>openDetails(targetId,key),results,review,
            startWork:id=>{act('c3Move',{id,status:'in-progress'});openDetails(id);},
            submission:id=>{targetId=id;const t=currentTask();modal(ui('submitWork')+' · '+t.title,field('work','work','','textarea','required')+field('link','link','','url')+'<label class="c3-field">'+escape(ui('file'))+'<input type="file" id="c3WorkFile" /></label>',cbutton(ui('cancel'),'dismiss')+cbutton(ui('submit'),'submitWork',id,true),'submission');},
            submitWork:async id=>{if(!getValue('work').trim())throw Error('Describe your submitted work.');if(!await confirmAction(ui('submitWork')+'?'))return;act('submitTask',{id,description:getValue('work'),link:getValue('link'),fileName:by('c3WorkFile').files[0]?.name||''});modified=false;openDetails(id);},
            comment:()=>{act('c3Comment',{id:targetId,message:getValue('comment')});openDetails(targetId,'comments');},
            previousStep:()=>{syncWizard();step=step===4&&!wizard.exam?2:step-1;wizardScreen();},
            nextStep:()=>{syncWizard();const fields=[...by('c3DialogBody').querySelectorAll('input[required],textarea[required]')].filter(el=>!el.closest('[hidden]'));if(fields.some(el=>!el.reportValidity()))return;if(step===1&&!wizard.studentIds.length)throw Error('Select at least one trainee.');if(step===0||step===2||step===3)validateStudio();step=step===2&&!wizard.exam?4:step+1;wizardScreen();},
            selectAll:()=>{by('c3DialogBody').querySelectorAll('[data-student]').forEach(el=>{if(!el.closest('[hidden]'))el.checked=true;});updateChips();modified=true;},clearSelection:()=>{by('c3DialogBody').querySelectorAll('[data-student]').forEach(el=>el.checked=false);updateChips();modified=true;},
            saveDraft:()=>{syncWizard();act('c3Create',{...wizard,draft:true});modified=false;by('c3Dialog').close();mode='';},
            createTask:async()=>{syncWizard();validateStudio();if(!await confirmAction(ui('createAssign')+'?'))return;act('c3Create',{...wizard,draft:false});modified=false;by('c3Dialog').close();mode='';},
            editDraft:id=>beginWizard(view.taskGroups.find(g=>g.id===id)),
            addQuestion:()=>{syncWizard();wizard.exam.questions.push({id:crypto.randomUUID(),type:'mcq',text:'',marks:1,choices:['',''],correct:0});wizardScreen();modified=true;},
            duplicateQuestion:id=>{syncWizard();const q=wizard.exam.questions.find(q=>q.id===id);wizard.exam.questions.push({...JSON.parse(JSON.stringify(q)),id:crypto.randomUUID()});wizardScreen();modified=true;},
            deleteQuestion:async id=>{syncWizard();if(!await confirmAction(ui('delete')+'?'))return;wizard.exam.questions=wizard.exam.questions.filter(q=>q.id!==id);wizardScreen();modified=true;},
            questionUp:id=>moveQuestion(id,-1),questionDown:id=>moveQuestion(id,1),
            addChoice:id=>{syncWizard();wizard.exam.questions.find(q=>q.id===id).choices.push('');wizardScreen();modified=true;},
            previewExam:()=>{syncWizard();previewBack=step;modal(ui('preview'),'<p class="c3-muted">'+escape('Supervisor preview; this does not create an attempt.')+'</p>'+wizard.exam.questions.map((q,i)=>'<article class="c3-question"><h3>'+(i+1)+'. '+escape(q.text)+'</h3>'+questionImage(q)+q.choices.map(c=>'<p>○ '+escape(c)+'</p>').join('')+'</article>').join(''),cbutton(ui('back'),'backToWizard'),'preview');},backToWizard:()=>{step=previewBack;wizardScreen();modified=true;},
            startExam:()=>{act('c3StartExam',{id:targetId});questionIndex=0;examScreen();},
            goQuestion:id=>{questionIndex=Number(id);examScreen();},prevQuestion:()=>{questionIndex--;examScreen();},nextQuestion:()=>{questionIndex++;examScreen();},
            submitExam:async()=>{if(!await confirmAction(ui('confirmSubmit')))return;const attempt=view.examAttempts.find(a=>a.taskId===targetId&&a.status==='active');if(!attempt){openDetails(targetId,'exam');return;}act('c3SubmitExam',{id:targetId,attemptId:attempt.id});modified=false;openDetails(targetId,'exam');},
            requestRevision:async id=>{if(!await confirmAction(ui('revisionAction')+'?'))return;act('reviewTask',{id,status:'revision',feedback:getValue('feedback')});modified=false;openDetails(id);},
            grade:async id=>{if(!await confirmAction(ui('grade')+'?'))return;act('reviewTask',{id,status:'completed',score:getValue('score'),feedback:getValue('feedback')});modified=false;review(id);},
            publish:async id=>{if(!await confirmAction(ui('publish')+' · '+studentName(view.tasks.find(t=>t.id===id)?.studentId)+'?'))return;act('c3Publish',{id});modified=false;review(id);},
            reportPreview:()=>{by('c3ReportPreview').innerHTML=reportPreview();},sendReport:async()=>{if(!getValue('reportFeedback').trim())throw Error('Feedback is required.');by('c3ReportPreview').innerHTML=reportPreview();if(!await confirmAction(ui('send')+'?'))return;act('c3Weekly',{studentId:getValue('reportStudent'),week:Number(getValue('reportWeek')),feedback:getValue('reportFeedback')});modified=false;reportScreen();}
        };
        function formatDescription(before,after){const field=by('c3DialogBody').querySelector('[name="description"]'),start=field.selectionStart,end=field.selectionEnd;field.setRangeText(before+field.value.slice(start,end)+after,start,end,'select');field.focus();modified=true;}
        function moveQuestion(id,delta){syncWizard();const list=wizard.exam.questions,index=list.findIndex(q=>q.id===id),next=index+delta;if(next<0||next>=list.length)return;[list[index],list[next]]=[list[next],list[index]];wizardScreen();modified=true;}
        document.addEventListener('click',async event=>{const control=event.target.closest('[data-c3]');if(!control)return;event.preventDefault();if(control.disabled)return;if(imageUploads){showError(Error(ui('imageLoading')));return;}control.disabled=true;try{await handlers[control.dataset.c3]?.(control.dataset.id);}catch(err){if(by('c3Dialog').open)showError(err);else notice(err.message);}finally{if(control.isConnected)control.disabled=false;}},{signal});
        on('c3DialogBody','change',event=>{
            const el=event.target;
            try{
                if(el.dataset.imageFile){uploadQuestionImage(el.files[0],el.dataset.imageFile);return;}
                if(el.id==='c3Attachments'){wizard.attachments=[...el.files].map(f=>({name:f.name,size:f.size}));by('c3AttachmentNames').textContent=wizard.attachments.map(a=>a.name).join(', ');}
                if(el.id==='c3IncludeExam')by('c3ExamFields').hidden=!el.checked;
                if(el.dataset.student)updateChips();
                if(el.hasAttribute('data-qtype')){syncWizard();const q=wizard.exam.questions.find(q=>q.id===el.closest('[data-question]').dataset.question);q.choices=q.type==='tf'?['True','False']:['',''];q.correct=0;wizardScreen();modified=true;}
                if(el.hasAttribute('data-check-index')){act('c3Checklist',{id:targetId,index:Number(el.dataset.checkIndex),done:el.checked});modified=false;}
                if(mode==='wizard'&&step===3&&!el.hasAttribute('data-qtype'))updateBuilderMetrics();
                if(el.hasAttribute('data-answer')){const attempt=view.examAttempts.find(a=>a.taskId===targetId&&a.status==='active');act('c3SaveAnswer',{id:targetId,attemptId:attempt.id,questionId:el.dataset.questionId,answer:Number(el.dataset.answer)});modified=false;examScreen();}
            }catch(err){showError(err);}
        });
        on('c3DialogBody','input',event=>{if(mode==='wizard'&&step===3)updateBuilderMetrics();if(event.target.id==='c3TraineeSearch'){const q=event.target.value.toLowerCase();by('c3DialogBody').querySelectorAll('[data-trainee-label]').forEach(el=>el.hidden=!el.dataset.traineeLabel.includes(q));}});
        click('c3Create',()=>beginWizard());click('c3Drafts',()=>modal(ui('drafts'),view.taskGroups.filter(g=>g.status==='draft').map(g=>'<div class="c3-panel"><h3>'+escape(g.title)+'</h3>'+cbutton(ui('view'),'editDraft',g.id)+'</div>').join('')||'<p>'+escape(ui('noData'))+'</p>',cbutton(ui('cancel'),'dismiss'),'drafts'));
        click('c3Reports',reportScreen);click('c3View',()=>{list=!list;render();});click('c3Clear',()=>{['taskSearch','c3Student','c3Week','c3Priority','c3Status'].forEach(id=>set(id,''));render();});
        
        ['taskSearch','c3Student','c3Week','c3Priority','c3Status','c3Sort','c3Group'].forEach(id=>on(id,id==='taskSearch'?'input':'change',render));
        let dragged='';on('taskList','dragstart',event=>{const card=event.target.closest('[data-task]');if(card){dragged=card.dataset.task;event.dataTransfer.setData('text/plain',dragged);}});
        on('taskList','dragover',event=>{const column=event.target.closest('[data-status]');if(column&&['assigned','in-progress'].includes(column.dataset.status)){event.preventDefault();column.classList.add('c3-drop');}});
        on('taskList','dragleave',event=>event.target.closest('[data-status]')?.classList.remove('c3-drop'));
        on('taskList','drop',event=>{event.preventDefault();const column=event.target.closest('[data-status]');all('.c3-drop').forEach(el=>el.classList.remove('c3-drop'));if(!column)return;try{act('c3Move',{id:dragged||event.dataTransfer.getData('text/plain'),status:column.dataset.status});}catch(err){notice(err.message);}});
        function tick(){
            if(!by('c3Dialog').open)return;const now=Date.parse(demo.now());
            if(mode==='details'){const t=currentTask(),open=(!t?.opensAt||now>=Date.parse(t.opensAt))&&(!t?.closesAt||now<Date.parse(t.closesAt));by('c3DialogActions').querySelectorAll('[data-c3="submission"],[data-c3="startWork"]').forEach(el=>el.disabled=!open);}
            if(mode==='details'&&tab==='exam'){
                const t=currentTask(),e=groupOf(t)?.exam;if(!e)return;
                const attempts=view.examAttempts.filter(a=>a.taskId===targetId),active=attempts.find(a=>a.status==='active'),remaining=Date.parse(e.startsAt)-now;
                text('c3ExamState',ui(remaining>0?'locked':now>=Date.parse(e.endsAt)&&!active?'expired':'ready'));
                text('c3Countdown',remaining>0?Math.floor(remaining/86400000)+'d '+new Date(remaining).toISOString().slice(11,19):'');
                const start=by('c3StartExam');if(start)start.disabled=!active&&(remaining>0||now>=Date.parse(e.endsAt)||attempts.length>=e.attempts);
            }
            if(mode==='exam'){
                const attempt=view.examAttempts.find(a=>a.taskId===targetId&&a.status==='active');if(!attempt)return;
                const ms=Math.max(0,Date.parse(attempt.expiresAt)-now);text('c3AttemptTimer',Math.floor(ms/60000)+':'+String(Math.floor(ms/1000)%60).padStart(2,'0'));
                if(!ms){refresh();modified=false;openDetails(targetId,'exam');}
            }
        }
        document.forms[0].addEventListener('submit',event=>{if(by('c3Dialog').open){event.preventDefault();event.stopPropagation();}},{signal});
        on('c3ImageClose','click',()=>{const viewer=by('c3ImageViewer');URL.revokeObjectURL(viewer.querySelector('img').src);viewer.close();});
        on('c3DialogBody','dragover',event=>{if(event.target.closest('[data-image-drop]'))event.preventDefault();});
        on('c3DialogBody','drop',event=>{const zone=event.target.closest('[data-image-drop]');if(zone){event.preventDefault();uploadQuestionImage(event.dataTransfer.files[0],zone.dataset.imageDrop);}});
        signal.addEventListener('abort',()=>imageUrlCache.forEach(url=>URL.revokeObjectURL(url)),{once:true});
        const timer=setInterval(tick,1000);signal.addEventListener('abort',()=>clearInterval(timer),{once:true});
        render();tick();
        notice('Live data integration is pending. No local fallback is enabled.');
        window.addEventListener('focus',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx';},{signal});
        window.addEventListener('pageshow',()=>{const current=demo.current();if(!current||current.mustChangePassword)location.href='../Auth/Login.aspx';},{signal});
    }
    if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',initialize,{once:true});else initialize();
    if(window.Sys?.WebForms?.PageRequestManager)Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initialize);
})();

/* Live Student adapter: authenticated C# REST projection, no local data fallback. */
(() => {
 'use strict';
 const start=async()=>{
 const by=id=>document.getElementById(id)||document.querySelector('[id$="_'+id+'"]');
 const text=(id,value)=>{const el=by(id);if(el)el.textContent=value??'—';};
 const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const fmt=value=>{if(!value)return 'Not recorded';if(/^\d{4}-\d{2}-\d{2}$/.test(value))return value;const d=new Date(value);return Number.isNaN(d.valueOf())?'Not recorded':new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Amman',dateStyle:'medium',timeStyle:'short',hourCycle:'h23'}).format(d)+' Jordan Time';};
 const note=message=>{let el=by('pageNotice')||by('profileAvailability')||document.querySelector('main > .preview-notice,main > .sample-note');if(!el){el=document.createElement('p');el.id='pageNotice';(document.querySelector('main,.content')||document.body).prepend(el);}el.hidden=false;el.setAttribute('role','status');el.textContent=message;};
 const set=(id,value)=>{if(by(id))by(id).value=value??'';};
 const error=()=>note('Firebase data could not be loaded. Refresh to retry; no demonstration records are shown.');
 const open=id=>{const el=by(id);if(!el)return;if(el.tagName==='DIALOG')el.showModal();else bootstrap.Modal.getOrCreateInstance(el).show();};
 const state=(data,key)=>data.states[key]==='permission_denied'?'The student-only '+key+' read permission has not been published.':data.states[key]==='schema_mismatch'?'The '+key+' record relationships need verification.':'The '+key+' data is unavailable.';
 const opts=(id,values,label)=>{if(!by(id))return;by(id).innerHTML='<option value="">'+esc(label)+'</option>'+[...new Set(values.filter(Boolean))].map(v=>'<option value="'+esc(v)+'">'+esc(v)+'</option>').join('');};
 const modalButton=(id,label)=>'<button type="button" class="details-button" data-live-id="'+esc(id)+'">'+esc(label)+'</button>';
 // Keep each page's existing sidebar markup and CSS; no layout component is introduced.
 const sidebar=document.querySelector('aside.sidebar,aside[id="sidebar"]'),toggle=document.querySelector('.menu-btn,.dash-menu-btn');
 if(toggle&&!location.pathname.toLowerCase().includes('/profile')){toggle.removeAttribute('onclick');window.toggleSidebar=()=>{const isOpen=!document.body.classList.contains('sidebar-open');document.body.classList.toggle('sidebar-open',isOpen);sidebar?.classList.toggle('open',isOpen);toggle.setAttribute('aria-expanded',String(isOpen));};toggle.addEventListener('click',window.toggleSidebar);document.querySelectorAll('.sidebar-close,.sidebar-backdrop').forEach(el=>el.addEventListener('click',()=>{document.body.classList.remove('sidebar-open');sidebar?.classList.remove('open');toggle.setAttribute('aria-expanded','false');}));}
 document.querySelectorAll('[data-live-close]').forEach(el=>el.addEventListener('click',()=>el.closest('dialog')?.close()));
 by('taskList').replaceChildren();
 try {
 const data=await window.UimsTab.studentData('tasks');
 document.querySelectorAll('[id$="lblUserName"],[id$="NavName"],.user-chip .name').forEach(el=>{el.textContent=data.user.name||data.user.email;});
 
 ['c3Create','c3Student','c3Week','c3GroupLabel','c3Drafts','c3Reports'].forEach(id=>{if(by(id))by(id).hidden=true;});text('c3Heading','My Training Tasks');text('c3Subtitle','Your current placement tasks, loaded from Firebase.');
 const tasks=data.tasks,labels=[['todo','New'],['in_progress','In Progress'],['submitted','In Review'],['revision_requested','Revision Requested'],['reviewed','Reviewed']];let list=false,opener;
 opts('c3Priority',[...new Set(tasks.map(t=>t.priority))],'All priorities');by('c3Status').innerHTML='<option value="">All statuses</option>'+labels.map(([id,label])=>'<option value="'+id+'">'+label+'</option>').join('');
 const render=()=>{if(!['ready','not_placed'].includes(data.states.tasks)){by('taskList').replaceChildren();by('c3Stats').innerHTML='<div class="c3-stat"><span>Task records</span><strong>—</strong></div>';by('noTasksMsg').hidden=false;text('noTasksMsg',state(data,'tasks'));return;}const q=by('taskSearch').value.toLowerCase(),status=by('c3Status').value,priority=by('c3Priority').value;const rows=tasks.filter(t=>(!q||[t.title,t.description].join(' ').toLowerCase().includes(q))&&(!status||t.status===status)&&(!priority||t.priority===priority)).sort((a,b)=>(a.dueDate||'').localeCompare(b.dueDate||'')*(by('c3Sort').value==='desc'?-1:1));by('taskList').classList.toggle('c3-list',list);by('taskList').innerHTML=labels.map(([id,label])=>'<section class="c3-column" aria-label="'+esc(label)+'"><h2>'+label+' <span class="c3-badge">'+rows.filter(t=>t.status===id).length+'</span></h2>'+rows.filter(t=>t.status===id).map(t=>'<article class="c3-card"><span class="c3-badge">'+esc(t.priority||'Not specified')+'</span><h3>'+esc(t.title)+'</h3><p>Due '+esc(fmt(t.dueDate))+'</p><p>Assigned by '+esc(t.assignedBy)+'</p>'+modalButton(t.id,'View task')+'</article>').join('')+'</section>').join('');by('noTasksMsg').hidden=rows.length>0;by('c3Stats').innerHTML=[['Assigned',tasks.length],['Pending',tasks.filter(t=>t.status!=='reviewed').length],['In Review',tasks.filter(t=>t.status==='submitted').length],['Reviewed',tasks.filter(t=>t.status==='reviewed').length]].map(([label,value])=>'<div class="c3-stat"><span>'+label+'</span><strong>'+value+'</strong></div>').join('');};
 by('taskList').addEventListener('click',event=>{const task=tasks.find(t=>t.id===event.target.closest('[data-live-id]')?.dataset.liveId);if(!task)return;opener=event.target;const o=data.opportunities.find(row=>row.id===data.placement?.opportunityId);text('c3DialogTitle',task.title);text('c3DialogHint','CURRENT PLACEMENT / '+task.status.replaceAll('_',' '));by('c3DialogBody').innerHTML='<div class="c3-grid"><section class="c3-panel"><h3>Overview</h3><p>'+esc(task.description)+'</p><p>Company: '+esc(o?.companyNameSnapshot||data.placement?.companyId)+'</p><p>Assigned by: '+esc(task.assignedBy)+'</p><p>Deadline: '+esc(fmt(task.dueDate))+'</p></section><section class="c3-panel"><h3>Submissions & exams</h3><p>Task evidence: '+esc(Array.isArray(task.evidenceLinks)?task.evidenceLinks.join(', '):'No evidence links recorded')+'</p><p>Secure submission writes are not enabled for this record yet. Cycle 3 exams are not linked to this placement; no exam or unpublished grade is exposed.</p></section></div>';by('c3DialogActions').innerHTML='<button type="button" data-live-close>Close</button>';by('c3DialogActions').querySelector('button').onclick=()=>by('c3Dialog').close();open('c3Dialog');});by('c3Close').onclick=()=>by('c3Dialog').close();by('c3Dialog').addEventListener('close',()=>opener?.focus());['taskSearch','c3Priority','c3Status','c3Sort'].forEach(id=>by(id).addEventListener(id==='taskSearch'?'input':'change',render));by('c3View').onclick=()=>{list=!list;text('c3View',list?'Board view':'List view');render();};by('c3Clear').onclick=()=>{['taskSearch','c3Priority','c3Status'].forEach(id=>set(id,''));render();};render();note(['ready','not_placed'].includes(data.states.tasks)?'Current-placement tasks only. Training-hour approval remains separate.':state(data,'tasks'));

 }catch(exception){error();}
 };
 if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',start,{once:true});else start();
})();
