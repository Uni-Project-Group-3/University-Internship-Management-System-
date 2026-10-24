const fs=require('fs'),path=require('path'),cp=require('child_process');const root=path.resolve('University-Internship-Management-System');
const themePath=path.join(root,'Assets/shared/just-theme.css'),s=fs.readFileSync(themePath,'utf8');
const importLine=s.slice(0,s.indexOf(':root')),tokens=s.slice(s.indexOf(':root {')+8,s.indexOf('\n}\n'));
const sidebar=s.slice(s.indexOf('.uims-fixed-sidebar.uims-fixed-sidebar'))
 .replace('body.uims-role-shell .final-workspace>header>a {display:none}\n','')
 .replace('body.uims-role-shell dialog {max-height:92dvh}dialog::backdrop {background:rgb(24 32 58 / 48%)}\n','');
// Keep new design variables inside navigation only; never replace another page's palette.
fs.writeFileSync(path.join(root,'Assets/shared/role-sidebar.css'),importLine+'.uims-fixed-sidebar,.uims-sidebar-toggle {\n'+tokens+'\n}\nbody.uims-role-shell {--uims-sidebar-width:272px}\n'+sidebar);
const taskRules=s.slice(s.indexOf('body {'),s.indexOf('.uims-fixed-sidebar.uims-fixed-sidebar')).replaceAll('body','body.uims-tasks-theme');
fs.writeFileSync(themePath,importLine+'body.uims-tasks-theme {\n'+tokens+'\n}\n'+taskRules+'body.uims-tasks-theme dialog{max-height:92dvh}body.uims-tasks-theme dialog::backdrop{background:rgb(24 32 58 / 48%)}\n');
const tasks=new Set(['Student/MyTasks.aspx','Company/ManageTasks.aspx']);
function walk(d){return fs.readdirSync(d,{withFileTypes:true}).filter(e=>!['bin','obj'].includes(e.name)).flatMap(e=>e.isDirectory()?walk(path.join(d,e.name)):[path.join(d,e.name)]);}
for(const p of walk(root).filter(p=>/\.(aspx|Master)$/i.test(p))){const rel=path.relative(root,p).replaceAll('\\','/');let h=fs.readFileSync(p,'utf8');const role=h.includes('<just:RoleSidebar')||rel==='FinalReports.aspx';
 h=h.replace(/\s*<link\b[^>]*href="[^"]*just-theme\.css"[^>]*\/>/g,'');
 const prefix=path.relative(path.dirname(p),root).replaceAll('\\','/'),base=prefix?prefix+'/':'';
 if(role&&!h.includes('role-sidebar.css'))h=h.replace('</head>','<link rel="stylesheet" href="'+base+'Assets/shared/role-sidebar.css" />\n</head>');
 if(tasks.has(rel)){h=h.replace(/<body\b([^>]*)>/i,(m,attrs)=>attrs.includes('class=')?m.replace(/class="([^"]*)"/,'class="$1 uims-tasks-theme"'):'<body class="uims-tasks-theme"'+attrs+'>');h=h.replace('</head>','<link rel="stylesheet" href="'+base+'Assets/shared/just-theme.css" />\n</head>');}
 fs.writeFileSync(p,h);
}
// Undo only our canonical-token consolidation. These four files were untouched before the navigation change.
for(const file of ['Assets/shared/colors.css','Assets/public/home.css','Assets/company/TaskExams.css','Assets/student/TaskExams.css'])fs.writeFileSync(path.join(root,file),cp.execFileSync('git',['show','HEAD:University-Internship-Management-System/'+file]));
const proj=path.join(root,'University-Internship-Management-System.csproj');let xml=fs.readFileSync(proj,'utf8');if(!xml.includes('Include="Assets\\shared\\role-sidebar.css"'))xml=xml.replace('</Project>','<ItemGroup><Content Include="Assets\\shared\\role-sidebar.css" /></ItemGroup>\n</Project>');fs.writeFileSync(proj,xml);
console.log('Task theme limited to Student/MyTasks.aspx and Company/ManageTasks.aspx. Other pages load sidebar styles only; original shared/public/exam CSS restored.');
