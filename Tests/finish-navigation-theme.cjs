const fs=require('fs'),path=require('path');const root=path.resolve('University-Internship-Management-System');
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);}
const canonical=fs.readFileSync(path.join(root,'Assets/shared/just-theme.css'),'utf8');const tokens=new Set([...canonical.matchAll(/(--[\w-]+)\s*:/g)].map(m=>m[1]));let styles=0;
for(const p of walk(path.join(root,'Assets')).filter(p=>p.endsWith('.css')&&!p.endsWith('just-theme.css'))){let s=fs.readFileSync(p,'utf8');const before=s;s=s.replace(/:root\s*\{([^{}]*)\}/g,(all,body)=>':root{'+body.replace(/(--[\w-]+)\s*:[^;{}]+;?/g,(declaration,key)=>tokens.has(key)?'':declaration)+'}');if(s!==before){fs.writeFileSync(p,s);styles++;}}
for(const role of ['Student','Company','Academic','Coordinator']){const p=path.join(root,'Shared',role+'Sidebar.ascx');let s=fs.readFileSync(p,'utf8').replace('~/Auth/Logout.aspx','~/Auth/Login.aspx?account=new');fs.writeFileSync(p,s);}
// Retire only the unused original control code; its markup was already removed.
for(const f of ['Shared/Sidebar.ascx.cs','Shared/Sidebar.ascx.designer.cs'])if(fs.existsSync(path.join(root,f)))fs.unlinkSync(path.join(root,f));
console.log('Consolidated canonical token definitions in',styles,'stylesheets.');
