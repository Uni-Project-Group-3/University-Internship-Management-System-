const fs=require('fs'),root='University-Internship-Management-System/';for(const [file,method,hostId]of [['Js_project/Student/MyTasks.js','loadAttachments','studentTaskAttachments'],['Js_project/Company/ManageTasks.js','loadTaskFiles','companyTaskAttachments']]){
let s=fs.readFileSync(root+file,'utf8');const start=s.indexOf('    async function '+method+'('),end=s.indexOf('    function statusLabel',start);if(start<0||end<0)throw Error(file);
s=s.slice(0,start)+`    async function ${method}(taskId) {
        const host=by('${hostId}');host.textContent='Loading authorized files…';
        try{const result=await request('attachments',{taskId},'GET');if(selectedId!==taskId)return;host.replaceChildren();
            for(const [purpose,label]of [['taskAttachment','Company Resources'],['submissionAttachment','Student Submission Files']]){
                const group=document.createElement('section'),heading=document.createElement('h6');heading.textContent=label;group.append(heading);
                const files=result.files.filter(f=>purpose==='taskAttachment'?f.purpose==='taskAttachment':f.purpose!=='taskAttachment');
                if(!files.length){const empty=document.createElement('p');empty.className='text-muted';empty.textContent='No files provided.';group.append(empty);}
                for(const file of files){const row=document.createElement('div'),name=document.createElement('span'),button=document.createElement('button');row.className='task-file-row';name.textContent=file.fileName+' · '+Math.ceil(file.sizeBytes/1024)+' KB';button.type='button';button.className='btn btn-light';button.textContent='Download';
                    button.onclick=async()=>{button.disabled=true;try{const response=await fetch('../TasksApi.ashx?action=download&fileId='+encodeURIComponent(file.id),{credentials:'same-origin',cache:'no-store'});if(!response.ok)throw Error('Authorized download failed. Please retry.');const url=URL.createObjectURL(await response.blob()),link=document.createElement('a');link.href=url;link.download=file.fileName;link.click();setTimeout(()=>URL.revokeObjectURL(url),10000);}catch(e){const error=document.createElement('p');error.className='field-error';error.textContent=e.message;row.append(error);}finally{button.disabled=false;}};row.append(name,button);group.append(row);
                }host.append(group);
            }
        }catch(e){host.textContent=e.message;}
    }
`+s.slice(end);if(file.includes('Student'))s=s.replace("state.tasks.filter(t=>t.examId||t.taskType==='exam').length","state.tasks.filter(t=>t.examId||t.taskType==='exam'||window.StudentTaskExams?.hasTask(t.id)).length");fs.writeFileSync(root+file,s);}
let p=root+'Js_project/Company/TaskComposer.js',s=fs.readFileSync(p,'utf8');s=s.replace("    const saved = new Map();",`    const preview=document.createElement('button');preview.type='button';preview.className='btn btn-light';preview.textContent='Preview examination';panels[2].append(preview);
    preview.onclick=()=>{if(!validExam())return;const content=document.createElement('div');for(const [index,q]of questions.entries()){const box=document.createElement('section'),title=document.createElement('h3');title.textContent=(index+1)+'. '+q.prompt+' · '+q.points+' points';box.append(title);if(q.file){const img=document.createElement('img'),url=URL.createObjectURL(q.file);img.src=url;img.alt='Question illustration';img.style.maxWidth='100%';img.onload=()=>URL.revokeObjectURL(url);box.append(img);}for(const text of q.type==='multipleChoice'?q.options:['True','False']){const line=document.createElement('p');line.textContent=text;box.append(line);}content.append(box);}TaskDialogs.open('Examination preview',content,{confirm:'Done',cancel:'Back to builder'});};
    const saved = new Map();`);
// Preview validates with the same existing question validator.
s=s.replace('if(!validExam())return;','if(!by("configureTaskExam").checked||!validateThrough(2))return;');fs.writeFileSync(p,s);
