/* UIMS local frontend demonstration. Never use this provider as production authorization. */
(function (scope) {
    'use strict';
    const key = 'uims.demo.v1';
    const departments = ['Computer Science','Artificial Intelligence','Cybersecurity','Computer Information Systems'];
    const initial = () => ({version:1, revision:0, clockOffset:0,
        users:[
            {id:'admin-1',name:'Demo Coordinator',email:'admin@demo.uims',role:'admin',status:'active',mustChangePassword:false},
            {id:'student-1',name:'Ahmad Ali',email:'student@demo.uims',role:'student',department:departments[0],universityId:'20230001',academicSupervisorId:'academic-1',status:'active',mustChangePassword:true},
            {id:'academic-1',name:'Dr. Lina Ahmad',email:'academic@demo.uims',role:'academic',department:departments[0],status:'active',mustChangePassword:false},
            {id:'company-1',name:'Omar Hassan',email:'company@demo.uims',role:'company',companyId:'company-north',status:'active',mustChangePassword:false}],
        companies:[{id:'company-north',name:'North Tech',email:'training@example.com',status:'active'}],
        opportunities:[{id:'opp-web',companyId:'company-north',title:'Web Development Intern',department:departments[0],location:'Amman',status:'open',requiredHours:160}],
        applications:[],placements:[],tasks:[],submissions:[],evaluations:[],hours:[],messages:[],groups:[]});

    // Cycle 3 uses the existing local demo state. These checks are simulation, not server authorization.
    function cycleState(s) {
        for (const key of ['taskGroups','examAttempts','weeklyReports','taskActivity','questionImages']) s[key] ||= [];
    }
    function finishAttempt(s, attempt) {
        if (attempt.status !== 'active') return;
        const group=s.taskGroups.find(g=>g.id===attempt.groupId),exam=group.exam;
        attempt.score=exam.questions.reduce((sum,q)=>sum+(Number(attempt.answers[q.id])===q.correct?q.marks:0),0);
        attempt.maximum=exam.totalMarks;attempt.percentage=attempt.score/attempt.maximum*100;
        attempt.passed=attempt.percentage>=exam.passing;attempt.status='submitted';attempt.submittedAt=now(s);
    }
    function cycleAction(s,u,action,p) {
        cycleState(s);
        const roles=allowed=>requireRole(s,allowed);
        const get=(list,id)=>{const item=s[list].find(x=>x.id===id);if(!item)throw Error('Record unavailable.');return item;};
        const monitor=studentId=>{const student=get('users',studentId),placement=s.placements.find(x=>x.studentId===studentId);return u&&(u.role==='student'&&u.id===studentId||u.role==='company'&&placement?.companyId===u.companyId||u.role==='academic'&&student.academicSupervisorId===u.id&&student.department===u.department);};
        const task=()=>{const t=get('tasks',p.id);if(!monitor(t.studentId)||u.role==='company'&&t.companyId!==u.companyId)throw Error('Assignment is not available to this identity.');return t;};
        const log=(t,message)=>s.taskActivity.push({id:id('activity'),taskId:t.id,studentId:t.studentId,authorId:u.id,message,at:now(s)});
        if(action==='c3RegisterImage') {
            roles(['company']);
            if(!['image/jpeg','image/png','image/webp'].includes(p.mime)||!(p.size>0&&p.size<=2097152)||!(p.width>0&&p.height>0))throw Error('Invalid question image.');
            const image={id:id('question-image'),companyId:u.companyId,mime:p.mime,size:p.size,width:p.width,height:p.height,createdAt:now(s)};
            s.questionImages.push(image);return image;
        }
        if(action==='c3Create') {
            roles(['company']);
            const existing=s.taskGroups.find(g=>g.requestId===p.requestId&&g.companyId===u.companyId);
            if(existing&&!p.draftId)return existing.id;
            const group=p.draftId?get('taskGroups',p.draftId):{id:id('task-group'),requestId:required(p.requestId,'Request ID'),companyId:u.companyId,authorId:u.id};
            if(group.companyId!==u.companyId||group.status==='assigned')throw Error('Only your own unpublished drafts can be changed.');
            const students=[...new Set(p.studentIds||[])];
            if(students.some(studentId=>!s.placements.some(pl=>pl.studentId===studentId&&pl.companyId===u.companyId&&pl.completionStatus==='InTraining')||!s.users.some(x=>x.id===studentId&&x.status==='active')))throw Error('Select confirmed active company trainees only.');
            const modern=!!(p.opensAt||p.closesAt||p.timezone);
            const title=required(p.title,'Task title'),description=modern?String(p.description||'').trim():required(p.description,'Description');
            if(modern&&!p.draft){
                if(!Number.isFinite(Date.parse(p.opensAt))||!Number.isFinite(Date.parse(p.closesAt))||Date.parse(p.opensAt)>=Date.parse(p.closesAt))throw Error('Task opening must be earlier than closing.');
            }
            if(!p.draft&&(!students.length||!Number.isFinite(Date.parse(p.deadline))||!(Number(p.maximum)>0&&Number.isFinite(Number(p.maximum)))))throw Error('Select trainees, a deadline and a positive practical maximum.');
            if(!['low','medium','high'].includes(p.priority)||!(Number(p.week)>=1&&Number(p.week)<=52&&Number.isInteger(Number(p.week))))throw Error('Choose a priority and week from 1 to 52.');
            if(p.startDate&&(!Number.isFinite(Date.parse(p.startDate))||Date.parse(p.startDate)>Date.parse(p.deadline)))throw Error('Start date must precede deadline.');
            const links=(p.links||[]).filter(Boolean);if(links.some(url=>!/^https?:\/\//i.test(url)))throw Error('Reference links must use HTTPS or HTTP.');
            let exam=null;
            if(p.exam){
                const e=p.exam,questions=(e.questions||[]).map(q=>({id:q.id||id('question'),type:q.type,text:String(q.text||'').trim(),choices:q.type==='tf'?['True','False']:(q.choices||[]).map(String),correct:Number(q.correct),marks:Number(q.marks),image:q.image||null}));
                if(questions.some(q=>q.image&&!s.questionImages.some(img=>img.id===q.image.id&&img.companyId===u.companyId)))throw Error('Question image is not owned by your company.');
                if(!p.draft){
                    if(modern&&(Date.parse(e.startsAt)<Date.parse(p.opensAt)||Date.parse(e.startsAt)>=Date.parse(p.closesAt)||!p.allowExamAfterClosing&&Date.parse(e.endsAt)>Date.parse(p.closesAt)))throw Error('Exam must fit task availability, or explicitly allow it to finish after task closing.');
                    if(!e.title?.trim()||!Number.isFinite(Date.parse(e.startsAt))||!(Number(e.duration)>=1&&Number(e.duration)<=240)||!(Number(e.attempts)>=1&&Number(e.attempts)<=5)||Number(e.attempts)%1||!['finish','close'].includes(e.policy))throw Error('Complete exam schedule, duration, attempts and availability policy.');
                    if(!(Number(e.passing)>=1&&Number(e.passing)<=100)||!(Number(e.totalMarks)>0&&Number.isFinite(Number(e.totalMarks))))throw Error('Set valid total marks and passing percentage.');
                    if(!questions.length||questions.length>100||questions.length!==Number(e.questionCount))throw Error('Configured question count must match the question builder.');
                    if(questions.some(q=>!q.text||!['mcq','tf'].includes(q.type)||q.choices.length<2||q.choices.some(c=>!c.trim())||!Number.isInteger(q.correct)||q.correct<0||q.correct>=q.choices.length||!(q.marks>0&&Number.isFinite(q.marks))))throw Error('Complete every question, its choices, one correct answer and marks.');
                    if(Math.abs(questions.reduce((sum,q)=>sum+q.marks,0)-Number(e.totalMarks))>0.00001)throw Error('Question marks must equal total exam marks.');
                    if(!Number.isFinite(Date.parse(e.endsAt))||Date.parse(e.endsAt)<=Date.parse(e.startsAt))throw Error('Exam end must be later than its start.');
                }
                exam={title:e.title,instructions:e.instructions||'',startsAt:e.startsAt,endsAt:e.endsAt,duration:Number(e.duration),attempts:Number(e.attempts),policy:e.policy,passing:Number(e.passing),totalMarks:Number(e.totalMarks),questionCount:Number(e.questionCount),questions};
            }
            Object.assign(group,{title,description,opensAt:p.opensAt?new Date(p.opensAt).toISOString():group.opensAt||'',closesAt:p.closesAt?new Date(p.closesAt).toISOString():group.closesAt||'',timezone:p.timezone||group.timezone||'Asia/Amman',allowExamAfterClosing:!!p.allowExamAfterClosing,week:Number(p.week),category:p.category||'',priority:p.priority,deadline:p.deadline,startDate:p.startDate||'',maximum:Number(p.maximum)||10,criteria:p.criteria||'',checklist:(p.checklist||[]).filter(Boolean),attachments:(p.attachments||[]).map(a=>({name:String(a.name),size:Number(a.size)||0})),links,exam,studentIds:students,status:p.draft?'draft':'assigned',updatedAt:now(s)});
            if(!p.draftId)s.taskGroups.push(group);
            if(!p.draft)students.forEach(studentId=>{
                const t={id:id('task'),groupId:group.id,studentId,companyId:u.companyId,assignedBy:u.id,title,description,opensAt:group.opensAt,closesAt:group.closesAt,deadline:group.deadline,maximum:group.maximum,criteria:group.criteria,status:'assigned',week:group.week,priority:group.priority,checklist:group.checklist.map(text=>({text,done:false})),createdAt:now(s)};
                s.tasks.push(t);log(t,'Task assigned');
            });
            return group.id;
        }
        if(action==='c3StartExam'||action==='c3SaveAnswer'||action==='c3SubmitExam') {
            roles(['student']);const t=task(),group=get('taskGroups',t.groupId),exam=group.exam;
            if(!exam)throw Error('No attached exam.');
            const attempts=s.examAttempts.filter(a=>a.taskId===t.id),active=attempts.find(a=>a.status==='active');
            if(action==='c3StartExam') {
                if(active)return active.id;
                if(Date.parse(now(s))<Date.parse(exam.startsAt)||Date.parse(now(s))>=Date.parse(exam.endsAt))throw Error('Exam is outside its scheduled availability.');
                if(attempts.length>=exam.attempts)throw Error('No attempts remaining.');
                const end=Date.parse(now(s))+exam.duration*60000;
                const attempt={id:id('attempt'),groupId:group.id,taskId:t.id,studentId:u.id,status:'active',startedAt:now(s),expiresAt:new Date(exam.policy==='close'?Math.min(end,Date.parse(exam.endsAt)):end).toISOString(),answers:{},published:false};
                s.examAttempts.push(attempt);log(t,'Exam attempt started');return attempt.id;
            }
            const attempt=get('examAttempts',p.attemptId);
            if(attempt.taskId!==t.id||attempt.studentId!==u.id)throw Error('Attempt does not belong to this assignment.');
            if(attempt.status==='submitted'){if(action==='c3SubmitExam')return attempt.id;throw Error('This attempt is finalized.');}
            if(Date.parse(now(s))>=Date.parse(attempt.expiresAt)){finishAttempt(s,attempt);return attempt.id;}
            if(action==='c3SaveAnswer'){
                const q=exam.questions.find(q=>q.id===p.questionId);if(!q||!Number.isInteger(Number(p.answer))||Number(p.answer)<0||Number(p.answer)>=q.choices.length)throw Error('Choose a valid answer.');
                attempt.answers[q.id]=Number(p.answer);attempt.savedAt=now(s);return attempt.id;
            }
            finishAttempt(s,attempt);log(t,'Exam submitted; company review pending');return attempt.id;
        }
        if(action==='c3Publish') {
            roles(['company']);const t=task();
            if(t.status!=='completed'&&!s.examAttempts.some(a=>a.taskId===t.id&&a.status==='submitted'))throw Error('No evaluated result to publish.');
            t.gradePublished=true;t.publishedAt=now(s);
            s.examAttempts.filter(a=>a.taskId===t.id&&a.status==='submitted').forEach(a=>{a.published=true;a.publishedAt=now(s);});log(t,'Results published to student');return;
        }
        if(action==='c3Move') {
            roles(['company','student']);const t=task();
            if(!['assigned','in-progress'].includes(t.status)||!['assigned','in-progress'].includes(p.status))throw Error('Submission, review and completion require their dedicated workflow.');
            if(u.role==='company'&&p.status==='assigned')throw Error('Work already started cannot be reset by drag-and-drop.');
            if(t.opensAt&&Date.parse(now(s))<Date.parse(t.opensAt)||t.closesAt&&Date.parse(now(s))>=Date.parse(t.closesAt))throw Error('Task actions are outside the configured availability.');
            t.status=p.status;log(t,'Status: '+p.status);return;
        }
        if(action==='c3Read'||action==='c3Checklist'||action==='c3Comment') {
            roles(['student','company','academic']);const t=task();
            if(action==='c3Read'){if(u.role==='student')t.readAt ||= now(s);return;}
            if(action==='c3Comment'){log(t,required(p.message,'Comment'));return;}
            if(u.role!=='student')throw Error('Only the assigned student can update this checklist.');
            if(t.opensAt&&Date.parse(now(s))<Date.parse(t.opensAt)||t.closesAt&&Date.parse(now(s))>=Date.parse(t.closesAt))throw Error('Task actions are outside the configured availability.');
            if(!t.checklist?.[p.index])throw Error('Checklist item unavailable.');t.checklist[p.index].done=!!p.done;return;
        }
        if(action==='c3Weekly') {
            roles(['company']);const student=get('users',p.studentId),placement=s.placements.find(pl=>pl.studentId===student.id&&pl.companyId===u.companyId),recipient=s.users.find(x=>x.id===student.academicSupervisorId&&x.role==='academic'&&x.status==='active'&&x.department===student.department);
            if(!placement||!recipient)throw Error('A confirmed trainee and assigned active Academic Supervisor are required.');
            if(!(Number(p.week)>=1&&Number(p.week)<=52))throw Error('Choose week 1–52.');
            const existing=s.weeklyReports.find(r=>r.studentId===student.id&&r.week===Number(p.week)&&r.companyId===u.companyId);if(existing)return existing.id;
            const tasks=s.tasks.filter(t=>t.studentId===student.id&&Number(t.week||1)===Number(p.week));
            const report={id:id('weekly'),studentId:student.id,companyId:u.companyId,senderId:u.id,recipientId:recipient.id,week:Number(p.week),sentAt:now(s),status:'sent',feedback:required(p.feedback,'Weekly feedback'),snapshot:{tasks:JSON.parse(JSON.stringify(tasks)),attempts:JSON.parse(JSON.stringify(s.examAttempts.filter(a=>tasks.some(t=>t.id===a.taskId)&&a.status==='submitted'))),revisions:s.submissions.filter(sub=>tasks.some(t=>t.id===sub.taskId)),activity:s.taskActivity.filter(a=>tasks.some(t=>t.id===a.taskId)),approvedHours:s.hours.filter(h=>h.studentId===student.id&&h.approved).reduce((sum,h)=>sum+h.hours,0)}};
            s.weeklyReports.push(report);return report.id;
        }
        if(action==='c3AcademicFeedback'){
            roles(['academic']);const report=get('weeklyReports',p.id);if(report.recipientId!==u.id||!monitor(report.studentId))throw Error('This report is not assigned to you.');
            report.academicFeedback=required(p.comments,'Academic feedback');report.reviewedAt=now(s);report.reviewedBy=u.id;report.status='reviewed';return;
        }
        throw Error('Unknown Cycle 3 operation.');
    }
    function cycleView(s,u,tasks) {
        cycleState(s);
        const ids=new Set(tasks.map(t=>t.id));
        const groups=s.taskGroups.filter(g=>u.role==='company'?g.companyId===u.companyId:tasks.some(t=>t.groupId===g.id)).map(g=>{
            const safe={...g};if(u.role==='student')safe.studentIds=g.studentIds.filter(id=>id===u.id);
            if(g.exam){safe.exam={...g.exam};delete safe.exam.questions;if(u.role==='company')safe.exam.questions=g.exam.questions;}
            return safe;
        });
        const attempts=s.examAttempts.filter(a=>ids.has(a.taskId)).map(a=>{
            if(u.role!=='student')return {...a};
            const safe={id:a.id,taskId:a.taskId,groupId:a.groupId,studentId:a.studentId,status:a.status,startedAt:a.startedAt,expiresAt:a.expiresAt,submittedAt:a.submittedAt,answers:a.answers,published:a.published};
            if(a.status==='active')safe.questions=s.taskGroups.find(g=>g.id===a.groupId).exam.questions.map(({correct,...q})=>q);
            if(a.published)Object.assign(safe,{score:a.score,maximum:a.maximum,percentage:a.percentage,passed:a.passed});return safe;
        });
        return {taskGroups:groups,examAttempts:attempts,taskActivity:s.taskActivity.filter(a=>ids.has(a.taskId)),weeklyReports:s.weeklyReports.filter(r=>u.role==='company'?r.companyId===u.companyId:u.role==='academic'?r.recipientId===u.id&&s.users.some(st=>st.id===r.studentId&&st.academicSupervisorId===u.id&&st.department===u.department):u.role==='admin')};
    }

    function load() {
        const raw = localStorage.getItem(key);
        if (!raw) { const state=initial(); localStorage.setItem(key,JSON.stringify(state)); return state; }
        const state=JSON.parse(raw);
        if(state.version!==1 || !Array.isArray(state.users)) throw Error('Demo storage is incompatible. Export it before resetting.');
        state.groups ||= []; cycleState(state);
        return state;
    }
    const now = state => new Date(Date.now()+state.clockOffset).toISOString();
    function expire(state) {
        const current=now(state);
        state.placements.forEach(p=>{ if(p.accountDisableAt && p.accountDisableAt<=current) {
            const user=state.users.find(u=>u.id===p.studentId);
            if(user && user.status!=='deleted') user.status='disabled';
            p.accountDisabledAt ||= current;
        }});
    }
    function read(){
        const s=load();cycleState(s);
        const expired=s.examAttempts.filter(a=>a.status==='active'&&Date.parse(now(s))>=Date.parse(a.expiresAt));
        const overdue=s.placements.some(p=>p.accountDisableAt&&p.accountDisableAt<=now(s)&&(!p.accountDisabledAt||s.users.some(u=>u.id===p.studentId&&u.status==='active')));
        expired.forEach(a=>finishAttempt(s,a));expire(s);
        if(expired.length||overdue)localStorage.setItem(key,JSON.stringify(s));
        return s;
    }
    function actor(s){ const id=sessionStorage.getItem('uims.demo.user');return s.users.find(u=>u.id===id&&u.status==='active'); }
    function requireRole(s,roles){const u=actor(s);if(!u||!roles.includes(u.role)||u.mustChangePassword)throw Error('Choose an active permitted demo identity and complete its password-change simulation.');return u;}
    const id = prefix => prefix+'-'+crypto.randomUUID();
    const required = (value,label) => { if(!String(value||'').trim())throw Error(label+' is required.');return String(value).trim(); };
    function transaction(action, payload={}) {
        const s=read();const u=actor(s);let result;
        const find=(list,value)=>{const record=s[list].find(r=>r.id===value);if(!record)throw Error('Record no longer exists. Reload and try again.');return record;};
        const placementFor=studentId=>s.placements.find(p=>p.studentId===studentId);
        const canMonitor=studentId=>{const student=find('users',studentId);return u.role==='admin'||u.role==='student'&&u.id===studentId||u.role==='academic'&&student.academicSupervisorId===u.id&&student.department===u.department||u.role==='company'&&placementFor(studentId)?.companyId===u.companyId;};
        if(action.startsWith('c3')){result=cycleAction(s,u,action,payload);
        }else if(action==='reviewReport'){
            requireRole(s,['company']);const submission=find('submissions',payload.id),task=find('tasks',submission.taskId);
            if(task.companyId!==u.companyId)throw Error('This submission belongs to a different company.');
            if(!['reviewed','corrections'].includes(payload.outcome))throw Error('Choose a review outcome.');
            submission.review={feedback:required(payload.feedback,'Report feedback'),outcome:payload.outcome,authorId:u.id,reviewedAt:now(s)};
        }else if(action==='saveCompanySupervisor'){
            requireRole(s,['admin']);
            const email=required(payload.supervisorEmail,'Supervisor email').toLowerCase();
            if(!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)||s.users.some(x=>x.email===email&&x.id!==payload.supervisorId&&x.status!=='deleted'))throw Error('Use a unique valid supervisor email.');
            const companyName=required(payload.name,'Company name'),supervisorName=required(payload.supervisorName,'Supervisor name');
            const company=payload.id?find('companies',payload.id):{id:id('company'),status:'active'};
            const supervisor=payload.supervisorId?find('users',payload.supervisorId):{id:id('company-user'),companyId:company.id,role:'company',status:'active',mustChangePassword:true};
            if(supervisor.role!=='company'||supervisor.companyId!==company.id)throw Error('Supervisor does not belong to this company.');
            company.name=companyName;if(payload.email!==undefined)company.email=payload.email;
            ['industry','phone','address','website','description'].forEach(key=>{if(payload[key]!==undefined)company[key]=payload[key];});
            supervisor.name=supervisorName;supervisor.email=email;if(payload.supervisorPhone!==undefined)supervisor.phone=payload.supervisorPhone;
            if(!payload.id)s.companies.push(company);if(!payload.supervisorId)s.users.push(supervisor);result=company.id;
        }else if(action==='academicFeedback'){
            requireRole(s,['academic']);if(!canMonitor(payload.studentId))throw Error('Student is not assigned to you.');
            const student=find('users',payload.studentId);student.academicFeedback={comments:required(payload.comments,'Academic feedback'),authorId:u.id,updatedAt:now(s)};
        }else if(action==='createGroup'){
            requireRole(s,['admin']);s.groups ||= [];
            const groupName=required(payload.name,'Group name');if(!departments.includes(payload.department)||s.groups.some(g=>g.name.toLowerCase()===groupName.toLowerCase()))throw Error('Use a unique group name and valid department.');
            s.groups.push({id:id('group'),name:groupName,department:payload.department,studentIds:[]});
        }else if(['addGroupStudent','assignGroup','deleteGroup'].includes(action)){
            requireRole(s,['admin']);const group=find('groups',payload.id);
            if(action==='deleteGroup')s.groups=s.groups.filter(g=>g.id!==group.id);
            if(action==='addGroupStudent'){
                const student=find('users',payload.studentId);
                if(student.role!=='student'||student.department!==group.department||student.status!=='active'||s.groups.some(g=>g.studentIds.includes(student.id)))throw Error('Choose an active, ungrouped student in the same department.');
                group.studentIds.push(student.id);if(group.supervisorId)student.academicSupervisorId=group.supervisorId;
            }
            if(action==='assignGroup'){
                const supervisor=find('users',payload.supervisorId);if(supervisor.role!=='academic'||supervisor.status!=='active'||supervisor.department!==group.department)throw Error('Choose an active supervisor in this department.');
                group.supervisorId=supervisor.id;group.studentIds.forEach(studentId=>find('users',studentId).academicSupervisorId=supervisor.id);
            }
        }else if(action==='contact'){
            const email=required(payload.email,'Email');if(!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email))throw Error('Enter a valid email.');
            s.messages.push({id:id('message'),name:required(payload.name,'Name'),email,subject:required(payload.subject,'Subject'),message:required(payload.message,'Message'),status:'unread',createdAt:now(s)});
        }else if(action==='readMessage'){requireRole(s,['admin']);const message=find('messages',payload.id);if(message.status==='unread')message.status='read';
        }else if(action==='replyMessage'){
            requireRole(s,['admin']);const message=find('messages',payload.id);message.reply=required(payload.reply,'Reply');message.status='replied';message.repliedAt=now(s);message.repliedBy=u.id;
        }else if(action==='changePassword'){
            if(!u)throw Error('Select a demo account.');
            if(!/^(?=.{12,})(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9\s]).*$/.test(payload.password)||payload.password!==payload.confirm)throw Error('Use matching passwords with 12+ characters, uppercase, lowercase, a number and a symbol.');
            u.mustChangePassword=false;result=u.id;
        }else if(action==='createUser'){
            requireRole(s,['admin']);const email=required(payload.email,'Email').toLowerCase();
            if(!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)||s.users.some(x=>x.email===email&&x.status!=='deleted'))throw Error('Use a unique valid email.');
            if(!['student','academic','company'].includes(payload.role))throw Error('Select a supported role.');
            if(['student','academic'].includes(payload.role)&&!departments.includes(payload.department))throw Error('Choose a department.');
            if(payload.role==='company')find('companies',payload.companyId);
            result={id:id(payload.role),name:required(payload.name,'Name'),email,role:payload.role,department:payload.department||'',companyId:payload.companyId||'',status:'active',mustChangePassword:true,universityId:payload.universityId||''};s.users.push(result);
        }else if(action==='editUser'||action==='resetUser'||action==='statusUser'||action==='deleteUser'){
            requireRole(s,['admin']);const target=find('users',payload.id);if(target.status==='deleted'&&action!=='deleteUser')throw Error('This demo account has been deleted.');if(target.id===u.id)throw Error('Manage another account.');
            if(action==='editUser'){target.name=required(payload.name,'Name');if(payload.phone!==undefined)target.phone=payload.phone;if(payload.universityId!==undefined)target.universityId=payload.universityId;if(payload.email){const email=payload.email.trim().toLowerCase();if(!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)||s.users.some(x=>x.id!==target.id&&x.email===email))throw Error('Use a unique valid email.');target.email=email;}}
            if(action==='resetUser')target.mustChangePassword=true;
            if(action==='statusUser'){if(target.role==='student')throw Error('Student account closure follows completion; no manual toggle.');target.status=target.status==='active'?'disabled':'active';}
            if(action==='deleteUser'){target.status='deleted';(s.groups||[]).forEach(group=>{if(group.supervisorId===target.id)delete group.supervisorId;});s.users.filter(x=>x.academicSupervisorId===target.id).forEach(x=>delete x.academicSupervisorId);}
        }else if(action==='profile'){
            requireRole(s,['student','company','academic','admin']);u.name=required(payload.name,'Name');u.phone=payload.phone||'';u.office=payload.office||'';['city','portfolioUrl','cvFileName','officeHours'].forEach(key=>{if(payload[key]!==undefined)u[key]=payload[key];});
        }else if(action==='company'){
            requireRole(s,['admin']);const company=payload.id?find('companies',payload.id):{id:id('company'),status:'active'};company.name=required(payload.name,'Company name');company.email=payload.email||'';if(!payload.id)s.companies.push(company);
        }else if(action==='assign'){
            requireRole(s,['admin']);const student=find('users',payload.studentId),supervisor=find('users',payload.supervisorId);
            if(student.role!=='student'||supervisor.role!=='academic'||student.department!==supervisor.department||supervisor.status!=='active')throw Error('Choose an active Academic Supervisor in the same department.');student.academicSupervisorId=supervisor.id;
        }else if(action==='opportunity'){
            requireRole(s,['company']);const opportunity=payload.id?find('opportunities',payload.id):{id:id('opportunity'),companyId:u.companyId,status:'open',createdAt:now(s)};
            if(opportunity.companyId!==u.companyId)throw Error('Different company.');if(!departments.includes(payload.department)||!Number.isFinite(Number(payload.requiredHours))||Number(payload.requiredHours)<=0)throw Error('Choose a department and positive required hours.');opportunity.title=required(payload.title,'Title');opportunity.department=payload.department;opportunity.location=payload.location||'';['description','requirements','seats','deadline','start','duration','workMode'].forEach(key=>{if(payload[key]!==undefined)opportunity[key]=payload[key];});opportunity.requiredHours=Number(payload.requiredHours)||160;if(!payload.id)s.opportunities.push(opportunity);
        }else if(action==='closeOpportunity'){
            requireRole(s,['company']);const opportunity=find('opportunities',payload.id);if(opportunity.companyId!==u.companyId)throw Error('Different company.');opportunity.status='closed';
        }else if(action==='apply'){
            requireRole(s,['student']);const opportunity=find('opportunities',payload.opportunityId);
            if(opportunity.status!=='open'||opportunity.department!==u.department)throw Error('This opportunity is not open to your department.');
            if(s.applications.some(a=>a.studentId===u.id&&a.opportunityId===opportunity.id))throw Error('You already applied.');
            result={id:id('application'),studentId:u.id,companyId:opportunity.companyId,opportunityId:opportunity.id,status:'pending',motivation:required(payload.motivation,'Motivation'),createdAt:now(s),phone:payload.phone||u.phone||'',cvLink:payload.cvLink||'',portfolioLink:payload.portfolioLink||''};s.applications.push(result);
        }else if(action==='decision'){
            requireRole(s,['company']);const application=find('applications',payload.id);if(application.companyId!==u.companyId||application.status!=='pending')throw Error('Application is unavailable for review.');
            if(!['accepted','rejected'].includes(payload.status))throw Error('Choose a decision.');application.reason=required(payload.reason,'Decision reason');application.status=payload.status;
        }else if(action==='confirmOffer'){
            requireRole(s,['student']);const application=find('applications',payload.id);if(application.studentId!==u.id||application.status!=='accepted'||placementFor(u.id))throw Error('Only one accepted placement can be confirmed.');
            application.status='confirmed';const opportunity=find('opportunities',application.opportunityId);result={id:id('placement'),studentId:u.id,companyId:application.companyId,opportunityId:opportunity.id,requiredHours:opportunity.requiredHours,completionStatus:'InTraining'};s.placements.push(result);
        }else if(action==='task'){
            requireRole(s,['company']);const placement=find('placements',payload.placementId);if(placement.companyId!==u.companyId||placement.completionStatus!=='InTraining')throw Error('Select an active company trainee.');
            if(!payload.deadline||isNaN(Date.parse(payload.deadline)))throw Error('Set a valid deadline.');
            result={id:id('task'),studentId:placement.studentId,companyId:u.companyId,title:required(payload.title,'Task title'),description:required(payload.description,'Description'),deadline:payload.deadline,maximum:Number(payload.maximum),criteria:payload.criteria||'',status:'assigned'};
            if(!(result.maximum>0)||!Number.isFinite(result.maximum))throw Error('Set a positive maximum score for this demonstration task.');s.tasks.push(result);
        }else if(action==='submitTask'){
            requireRole(s,['student']);const task=find('tasks',payload.id);if(placementFor(u.id)?.completionStatus!=='InTraining')throw Error('Work submission requires an active internship.');if(task.studentId!==u.id||!['assigned','in-progress','revision'].includes(task.status))throw Error('This task cannot be submitted.');
            if(task.opensAt&&Date.parse(now(s))<Date.parse(task.opensAt)||task.closesAt&&Date.parse(now(s))>=Date.parse(task.closesAt))throw Error('Task actions are outside the configured availability.');
            if(payload.link&&!/^https?:\/\//i.test(payload.link))throw Error('Use an HTTP or HTTPS link.');
            s.submissions.push({id:id('submission'),taskId:task.id,studentId:u.id,description:required(payload.description,'Work description'),link:payload.link||'',fileName:payload.fileName||'',createdAt:now(s)});task.status='submitted';if(task.groupId){cycleState(s);s.taskActivity.push({id:id('activity'),taskId:task.id,studentId:u.id,authorId:u.id,message:'Work submitted: '+payload.description,at:now(s)});}
        }else if(action==='reviewTask'){
            requireRole(s,['company']);const task=find('tasks',payload.id);if(task.companyId!==u.companyId||task.status!=='submitted')throw Error('Task is not awaiting your review.');if(!['revision','completed'].includes(payload.status))throw Error('Choose a valid review decision.');task.feedback=required(payload.feedback,'Feedback');
            if(payload.status==='revision')task.status='revision';else{const score=Number(payload.score);if(payload.score===''||!Number.isFinite(score)||score<0||score>task.maximum)throw Error('Score is outside this task maximum.');task.score=score;task.status='completed';if(task.groupId)task.gradePublished=false;}
            if(task.groupId){cycleState(s);s.taskActivity.push({id:id('activity'),taskId:task.id,studentId:task.studentId,authorId:u.id,message:payload.status+': '+task.feedback,at:now(s)});}
        }else if(action==='hours'){
            requireRole(s,['company']);const placement=find('placements',payload.placementId);if(placement.companyId!==u.companyId||placement.completionStatus!=='InTraining')throw Error('Select an active trainee.');if(!/^\d{4}-\d{2}-\d{2}$/.test(payload.date)||isNaN(Date.parse(payload.date))||new Date(payload.date).toISOString().slice(0,10)!==payload.date)throw Error('Choose a valid training date.');const hours=Number(payload.hours);if(!(hours>0&&hours<=24))throw Error('Enter 0–24 hours for a dated demonstration entry.');if(s.hours.some(h=>h.studentId===placement.studentId&&h.date===payload.date))throw Error('An entry already exists for this date.');s.hours.push({id:id('hours'),studentId:placement.studentId,companyId:u.companyId,date:required(payload.date,'Date'),hours,approved:true,approvedBy:u.id});
        }else if(action==='evaluation'){
            requireRole(s,['company','academic']);if(!canMonitor(payload.studentId))throw Error('Student is not assigned to you.');const placement=placementFor(payload.studentId);if(!placement)throw Error('No placement.');
            if(u.role==='academic'&&!s.evaluations.some(e=>e.studentId===payload.studentId&&e.kind==='company'&&e.status==='submitted'))throw Error('Company final evaluation must be submitted first.');
            const kind=u.role;let evaluation=s.evaluations.find(e=>e.studentId===payload.studentId&&e.kind===kind);if(evaluation&&evaluation.authorId!==u.id)throw Error('Only the author may edit this draft.');if(evaluation?.status==='submitted')throw Error('Submitted evaluation is read-only.');
            if(!evaluation){evaluation={id:id('evaluation'),studentId:payload.studentId,kind,authorId:u.id};s.evaluations.push(evaluation);}evaluation.comments=required(payload.comments,'Evaluation comments');evaluation.status=payload.submit==='yes'?'submitted':'draft';if(evaluation.status==='submitted')evaluation.submittedAt=now(s);
        }else if(action==='approveCompletion'){
            requireRole(s,['admin']);const placement=find('placements',payload.id);if(placement.completionStatus!=='InTraining')throw Error('Completion already reviewed.');
            if(!['company','academic'].every(kind=>s.evaluations.some(e=>e.studentId===placement.studentId&&e.kind===kind&&e.status==='submitted')))throw Error('Both final evaluations must be submitted.');
            if(s.hours.filter(h=>h.studentId===placement.studentId&&h.approved).reduce((sum,h)=>sum+h.hours,0)<placement.requiredHours||s.tasks.some(t=>t.studentId===placement.studentId&&t.status!=='completed'))throw Error('Approved hours and completed tasks are required.');
            placement.completionStatus='PendingStudentConfirmation';placement.completionApprovedAt=now(s);placement.reviewNotes=payload.notes||'';
        }else if(action==='confirmCompletion'){
            requireRole(s,['student']);const placement=find('placements',payload.id);if(placement.studentId!==u.id||placement.completionStatus!=='PendingStudentConfirmation')throw Error('No completion awaiting your confirmation.');
            placement.completionStatus='Completed';placement.studentConfirmedAt=now(s);placement.accountDisableAt=new Date(Date.parse(placement.studentConfirmedAt)+3600000).toISOString();placement.certificateId=id('DEMO-CERT');
        }else if(action==='advanceClock'){requireRole(s,['admin']);if(!(Number(payload.minutes)>0)||Number(payload.minutes)>525600)throw Error('Advance by 1 to 525600 minutes.');s.clockOffset+=Number(payload.minutes)*60000;if(!Number.isFinite(s.clockOffset))throw Error('Invalid test interval.');expire(s);
        }else throw Error('Unknown demonstration operation.');
        s.revision++;localStorage.setItem(key,JSON.stringify(s));scope.dispatchEvent?.(new Event('uims-demo-changed'));return result;
    }

    function view() {
        const state=read(),user=actor(state);if(!user)return null;
        const placementFor=studentId=>state.placements.find(p=>p.studentId===studentId);
        const students=state.users.filter(student=>student.role==='student'&&student.status!=='deleted'&&(user.role==='admin'||user.role==='student'&&student.id===user.id||user.role==='academic'&&student.academicSupervisorId===user.id&&student.department===user.department||user.role==='company'&&placementFor(student.id)?.companyId===user.companyId));
        const ids=new Set(students.map(student=>student.id));
        const visibleTask=task=>{const safe={...task};if(user.role==='student'&&task.groupId&&!task.gradePublished)delete safe.score;return safe;};
        const progress=students.map(student=>{
            const placement=placementFor(student.id),tasks=state.tasks.filter(task=>task.studentId===student.id);
            tasks.forEach((task,index)=>tasks[index]=visibleTask(task));
            const hours=state.hours.filter(hour=>hour.studentId===student.id&&hour.approved).reduce((sum,hour)=>sum+(Number.isFinite(Number(hour.hours))?Number(hour.hours):0),0);
            const risks=[];if(tasks.some(task=>task.status!=='completed'&&Date.parse(task.deadline)<Date.parse(now(state))))risks.push('Overdue tasks');if(tasks.some(task=>task.status==='revision'))risks.push('Revision requested');
            const company=state.companies.find(c=>c.id===placement?.companyId),supervisor=state.users.find(a=>a.id===student.academicSupervisorId),companySupervisor=state.users.find(a=>a.role==='company'&&a.companyId===placement?.companyId&&a.status==='active');
            return {...student,placement,company,supervisor,companySupervisor,tasks,approvedHours:hours,requiredHours:Number(placement?.requiredHours)||0,completedTasks:tasks.filter(task=>task.status==='completed').length,risks,evaluations:state.evaluations.filter(e=>e.studentId===student.id&&(user.role!=='student'||e.published===true))};
        });
        return {...cycleView(state,user,state.tasks.filter(t=>ids.has(t.studentId))),user,students,progress,groups:state.groups||[],companies:state.companies,users:state.users,
            opportunities:state.opportunities.filter(o=>user.role==='admin'||user.role==='company'&&o.companyId===user.companyId||user.role==='student'&&o.department===user.department&&(o.status==='open'||state.applications.some(a=>a.studentId===user.id&&a.opportunityId===o.id))),
            placements:state.placements.filter(p=>ids.has(p.studentId)),tasks:state.tasks.filter(t=>ids.has(t.studentId)).map(visibleTask),
            applications:state.applications.filter(a=>user.role==='admin'||user.role==='company'&&a.companyId===user.companyId||user.role==='student'&&a.studentId===user.id),
            submissions:state.submissions.filter(sub=>ids.has(sub.studentId)),evaluations:state.evaluations.filter(e=>ids.has(e.studentId)&&(user.role!=='student'||e.published===true)),messages:user.role==='admin'?state.messages:[]};
    }
    const dashboards={admin:'Coordinator/CoordinatorDashboard.aspx',student:'Student/StudentDashboard.aspx',company:'Company/CompanyDashboard.aspx',academic:'Academic/AcademicDashboard.aspx'};
    scope.UimsDemo={view,read,transaction,departments,dashboards,now:()=>now(read()),current:()=>actor(read()),login(id){const s=read(),u=s.users.find(x=>x.id===id&&x.status==='active');if(!u)throw Error('Demo account unavailable.');sessionStorage.setItem('uims.demo.user',id);sessionStorage.setItem('uims.demo.enabled','true');return u;},logout(){sessionStorage.removeItem('uims.demo.user');sessionStorage.removeItem('uims.demo.enabled');},enabled:()=>sessionStorage.getItem('uims.demo.enabled')==='true',export:()=>JSON.stringify(read(),null,2)};
})(window);
