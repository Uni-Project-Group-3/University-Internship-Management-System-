const fs=require('fs'),path=require('path'),crypto=require('crypto');const root=path.resolve('University-Internship-Management-System');const read=p=>fs.readFileSync(path.join(root,p),'utf8'),write=(p,s)=>fs.writeFileSync(path.join(root,p),s);
const protectedFiles=['Shared/StudentSidebar.ascx','Shared/CompanySidebar.ascx','Shared/AcademicSidebar.ascx','Shared/CoordinatorSidebar.ascx','Shared/RoleSidebarBase.cs','Assets/shared/role-sidebar.css','Js_project/shared-role-navigation.js','Web.config'];fs.writeFileSync('Tests/task-scope-baseline.json',JSON.stringify(Object.fromEntries(protectedFiles.filter(p=>fs.existsSync(path.join(root,p))).map(p=>[p,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex')])),null,2));
for(const [role,file,filter,title]of [['Student','MyTasks','taskFilter','My Tasks & Exams'],['Company','ManageTasks','statusFilter','Manage Tasks & Exams']]){
let s=read(role+'/'+file+'.aspx');
s=s.replace(/<div class="page-header[^>]*>[\s\S]*?<\/div>/,'<section class="tasks-premium-hero"><div><span class="tasks-role-badge">JUST / '+(role==='Student'?'STUDENT':'COMPANY SUPERVISOR')+' WORKSPACE</span><h1>'+title+'</h1><p>'+(role==='Student'?'Track your assignments, examinations and progress in one place.':'Create tasks, assign students, monitor progress and release results.')+'</p></div><div class="tasks-hero-metrics"><span><strong id="heroTaskCount">—</strong> active tasks</span><span><strong id="heroExamCount">—</strong> exam tasks</span></div></section>');
s=s.replace(/<input[^>]*id="taskSearch"[^>]*\/>/,'<label for="taskSearch">Task Name</label><select id="taskSearch" aria-label="Task Name"><option value="">All Tasks</option></select>');
s=s.replace(/<i class="bi bi-search"[^>]*>\s*<\/i>/,'');
s=s.replace(/<div class="stats-grid">[\s\S]*?(?=\s*<!-- SEARCH|\s*<div class="toolbar)/,`<div class="stats-grid tasks-premium-stats">${['Total Tasks','New / Pending','In Progress','Awaiting Review','Complete'].map((t,i)=>'<article class="stat-card"><span class="tasks-stat-symbol" aria-hidden="true">'+['≡','◷','▷','◎','✓'][i]+'</span><div class="stat-details"><h3>—</h3><span>'+t+'</span></div></article>').join('')}</div>\n`);
s=s.replace(/<nav class="(?:student|company)-task-tabs"[\s\S]*?<\/nav>/g,'');
s=s.replace(/\s*role="tabpanel"|\s*aria-labelledby="studentTab[^"]*"/g,'').replace(/(<section\b[^>]*data-(?:student|company)-panel="[^"]*")[^>]*>/g,'$1>');
s=s.replace('</head>','<link href="../Assets/tasks-premium.css?v=20261023" rel="stylesheet" />\n</head>');
write(role+'/'+file+'.aspx',s);
}
let s=read('Js_project/Student/MyTasks.js');
let start=s.indexOf('    function selectTaskTab('),end=s.indexOf("    by('taskFilter')?.add",start);
s=s.slice(0,start)+`    function selectTaskTab() {
        const task=state.tasks.find(t=>t.id===selectedId),isExam=!!(task?.examId||task?.taskType==='exam');
        document.querySelectorAll('[data-student-panel]').forEach(panel=>{panel.hidden=isExam?panel.dataset.studentPanel!=='exam':panel.dataset.studentPanel==='exam';});
        by('confirmSubmitBtn').hidden=isExam||!editing||by('workDesc').readOnly;
        if(isExam)window.StudentTaskExams?.showTask(selectedId);
    }
`+s.slice(end);
s=s.replace("(task.title + ' ' + task.description).toLowerCase().includes(query)","task.title.toLowerCase() === query");
s=s.replace("        const query = (by('taskSearch')?.value || '').trim().toLowerCase();","        syncTaskTitles();\n        const query = (by('taskSearch')?.value || '').trim().toLowerCase();");
s=s.replace(/\[state.tasks.length, state.tasks.filter\(t => t.status === 'inprogress'\).length, state.tasks.filter\(t => t.status === 'submitted'\).length, state.tasks.filter\(t => t.status === 'revision'\).length\]/,"[state.tasks.length,state.tasks.filter(t=>t.status==='assigned').length,state.tasks.filter(t=>['inprogress','revision'].includes(t.status)).length,state.tasks.filter(t=>t.status==='submitted').length,state.tasks.filter(t=>t.status==='completed').length]");
start=s.indexOf('        const markup = tasks.map(');end=s.indexOf("        if (markup)",start);s=s.slice(0,start)+"        const markup = tasks.map(task => premiumCard(task)).join('');\n"+s.slice(end);
start=s.indexOf('cards.map(task =>');end=s.indexOf(").join('') + (cards.length",start);s=s.slice(0,start)+"cards.map(task => premiumCard({...state.tasks.find(t=>t.id===task.id),...task}))"+s.slice(end+1);
s=s.replace("selectTaskTab(readOnly ? 'details' : 'submission');","selectTaskTab();").replace("examLink.textContent = 'View exam';","examLink.hidden = true; examLink.textContent = 'View exam';");
s=s.replace("by('taskSearch')?.addEventListener('input', render)","by('taskSearch')?.addEventListener('change', render)");
// Keep the instructions/metadata section in one coherent popup, rather than replacing it later.
start=s.indexOf("        let taskInfo = by('studentTaskInstructions');");end=s.indexOf("        by('workDesc').value",start);if(start>=0)s=s.slice(0,start)+s.slice(end);
s=s.replace("'<h6>Task instructions</h6>","'<div class=\"task-detail-metrics\">Start: ' + escapeHtml(dateLabel(task.startsAt || task.createdAt)) + ' · Deadline: ' + escapeHtml(dateLabel(task.deadline)) + '</div><h6>Task instructions</h6>");
const helpers=`
    function syncTaskTitles(){const select=by('taskSearch'),old=select.value,titles=[...new Set(state.tasks.map(t=>t.title))].sort();select.replaceChildren(new Option('All Tasks',''));titles.forEach(t=>select.add(new Option(t,t.toLowerCase())));if(titles.some(t=>t.toLowerCase()===old))select.value=old;by('heroTaskCount').textContent=state.tasks.filter(t=>t.status!=='completed'&&!t.archived).length;by('heroExamCount').textContent=state.tasks.filter(t=>t.examId||t.taskType==='exam').length;}
    function premiumCard(task){const exam=!!(task.examId||task.taskType==='exam'),pending=task.status==='assigned',working=['inprogress','revision'].includes(task.status),due=Date.parse(task.deadline||task.dueDate),left=due-now();return '<article class="task-card task-board-card task-board-card-readonly" data-status="'+escapeHtml(task.status)+'"><div class="task-board-card-head"><h3>'+escapeHtml(task.title)+'</h3><span class="status-badge">'+escapeHtml(statusLabel(task.status))+'</span></div><p class="task-desc">'+escapeHtml(task.description)+'</p><div class="task-card-dates"><span>Start: '+escapeHtml(dateLabel(task.startsAt||task.createdAt))+'</span><span>Due: '+escapeHtml(dateLabel(task.deadline||task.dueDate))+'</span><span class="'+(left<0?'task-overdue':'')+'">'+(Number.isFinite(left)?left<0?'Overdue':Math.ceil(left/3600000)+' hours remaining':'No deadline supplied')+'</span></div><span class="task-type-badge">'+(exam?'Exam':'Standard Task')+'</span>'+(task.status==='submitted'?'<p class="task-review-notice">Awaiting Supervisor Review</p>':'')+(task.status==='completed'?'<p class="task-approved-notice">Approved'+(!exam&&task.score!=null?' · '+escapeHtml(task.score)+' / '+escapeHtml(task.maximum):'')+'</p>':'')+(task.attachmentIds?.length?'<small>Attachments available</small>':'')+'<div class="task-action"><button type="button" class="btn btn-light" data-task-action="details" data-task-id="'+escapeHtml(task.id)+'">'+(working?'Continue Task':'View Details')+'</button>'+(pending?'<button type="button" class="btn btn-primary" data-task-action="start" data-task-id="'+escapeHtml(task.id)+'">Accept Task</button>':'')+'</div></article>';}
`;
s=s.replace('    function render() {',helpers+'\n    function render() {');
// Dedicated resource and submission sections; download through the existing authorized backend.
s=s.replace("host.innerHTML = result.files.length ? result.files.map(file =>", "host.innerHTML = result.files.length ? result.files.map(file =>");
s=s.replace("'<div><a href=\"../TasksApi.ashx?action=download&fileId='", "'<div class=\"task-file-row\"><span>' + escapeHtml(file.purpose === 'taskAttachment' ? 'Company resource' : 'Student submission') + '</span><a class=\"btn btn-light\" href=\"../TasksApi.ashx?action=download&fileId='");
s=s.replace("+ escapeHtml(file.fileName) + '</a>","+ 'Download · ' + escapeHtml(file.fileName) + '</a>");
write('Js_project/Student/MyTasks.js',s);
// Company: one scrollable review instead of tabbed fragments.
s=read('Js_project/Company/ManageTasks.js');start=s.indexOf('    function selectReviewTab(');end=s.indexOf("    by('reviewModal').addEventListener('hide.bs.modal'",start);
s=s.slice(0,start)+`    function selectReviewTab(){const isExam=examStates.has(selectedId);document.querySelectorAll('[data-company-panel]').forEach(p=>{p.hidden=isExam?p.dataset.companyPanel!=='exam':p.dataset.companyPanel==='exam';});by('approveTaskBtn').hidden=isExam;by('requestRevisionBtn').hidden=isExam;}
`+s.slice(end);
s=s.replace("(task.title + ' ' + task.studentName).toLowerCase().includes(query)","task.title.toLowerCase() === query").replace("(task.title + ' ' + task.description + ' ' + task.studentName).toLowerCase().includes(query)","task.title.toLowerCase() === query").replace("(task.title + ' ' + task.description).toLowerCase().includes(query)","task.title.toLowerCase() === query");
s=s.replace('    function render() {',`    function render() {
        const nameFilter=by('taskSearch'),selectedTitle=nameFilter.value;nameFilter.replaceChildren(new Option('All Tasks',''));[...new Set(state.tasks.map(t=>t.title))].sort().forEach(t=>nameFilter.add(new Option(t,t.toLowerCase())));nameFilter.value=selectedTitle;
        by('heroTaskCount').textContent=state.tasks.filter(t=>t.status!=='completed'&&!t.archived).length;by('heroExamCount').textContent=examStates.size;
`);
s=s.replace("by('taskSearch')?.addEventListener('input', render)","by('taskSearch')?.addEventListener('change', render)");
// Column order is fixed; task ordering remains available.
start=s.indexOf("        if (boardState.canMove && boardState.placementId !== 'all')");end=s.indexOf('\n    }\n    async function refreshBoard',start);if(start>=0)s=s.slice(0,start)+s.slice(end);
write('Js_project/Company/ManageTasks.js',s);
console.log('Task-only premium markup and single-popup behavior applied. Sidebar files and Firebase configuration untouched.');
