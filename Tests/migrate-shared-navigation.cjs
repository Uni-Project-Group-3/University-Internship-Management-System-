const fs=require('fs'),path=require('path');
const root=path.resolve('University-Internship-Management-System');
const roles=['Student','Company','Academic','Coordinator'];
for(const role of roles){let s=fs.readFileSync(path.join('C:/Users/user/AppData/Local/Temp',role+'Sidebar (1).ascx'),'utf8');
s=s.replace('<small>Internships</small>','<small>Internship Management System</small>').replace('width="43" height="43" alt="JUST logo"','width="88" height="88" alt="Jordan University of Science and Technology official logo"');
s=s.replace(/<div class="uims-side-role">[^<]*<\/div>/,'<div class="uims-side-role">'+({Student:'Student Workspace',Company:'Company Supervisor Workspace',Academic:'Academic Supervisor Workspace',Coordinator:'Coordinator Workspace'}[role])+'</div>');
// Existing sign-out API remains the sole session-ending operation.
s=s.replace('class="uims-side-link uims-side-logout"','class="uims-side-link uims-side-logout" data-uims-logout');
fs.writeFileSync(path.join(root,'Shared',role+'Sidebar.ascx'),s);
}
fs.copyFileSync('C:/Users/user/AppData/Local/Temp/RoleSidebarBase (1).cs',path.join(root,'Shared/RoleSidebarBase.cs'));
const modified=[],removedIds=[];
for(const role of roles)for(const name of fs.readdirSync(path.join(root,role)).filter(n=>n.endsWith('.aspx'))){const p=path.join(root,role,name);let s=fs.readFileSync(p,'utf8');if(!/<(?:html|asp:Content)\b/i.test(s))continue;
let sidebarId='sidebar',count=0;
s=s.replace(/<%@\s*Register\b[^%]*Src="~\/Shared\/(?:Sidebar|StudentSidebar|CompanySidebar|AcademicSidebar|CoordinatorSidebar)\.ascx"[^%]*%>\s*/gi,'');
s=s.replace(/<shared:Sidebar\b[^>]*\/\s*>/gi,()=>{count++;return ''});
// Match aside blocks without crossing a containing content element.
s=s.replace(/<aside\b[^>]*>[\s\S]*?<\/aside>/gi,m=>{if(!/class="[^"]*(?:sidebar|side-nav)/i.test(m))return m;sidebarId=m.match(/\bid="([^"]+)"/)?.[1]||sidebarId;removedIds.push(...[...m.matchAll(/\bID="([^"]+)"[^>]*runat="server"/gi)].map(x=>role+'/'+name+':'+x[1]));count++;return '';});
// A few pages use a nav instead of an aside.
if(!count)s=s.replace(/<nav\b[^>]*class="[^"]*sidebar[^>]*>[\s\S]*?<\/nav>/gi,()=>{count++;return ''});
s=s.replace(/<div\b[^>]*class="[^"]*(?:sidebar-backdrop|dash-backdrop|wr-backdrop)[^"]*"[^>]*>\s*<\/div>/gi,'');
const register='<%@ Register Src="~/Shared/'+role+'Sidebar.ascx" TagPrefix="just" TagName="RoleSidebar" %>\n';
s=s.replace(/(<%@\s*Page[\s\S]*?%>)/i,'$1\n'+register);
const control='<just:RoleSidebar ID="SharedRoleNavigation" runat="server" SidebarId="'+sidebarId+'" />';
if(/<body\b/i.test(s)){s=s.replace(/(<body\b[^>]*>)/i,'$1');const form=/<form\b[^>]*runat="server"[^>]*>/i;if(form.test(s))s=s.replace(form,m=>m+'\n'+control);else s=s.replace(/<body\b[^>]*>/i,m=>m+'\n'+control);}
else s=s.replace(/(<asp:Content\b[^>]*ContentPlaceHolderID="MainContent"[^>]*>)/i,'$1\n'+control);
if(!s.includes(control))throw Error('Cannot insert sidebar '+p);
const css='<link rel="stylesheet" href="<%= ResolveUrl("~/Assets/shared/just-theme.css") %>" />';
const js='<script src="<%= ResolveUrl("~/Js_project/shared-role-navigation.js") %>" defer></script>';
if(/<\/head>/i.test(s))s=s.replace(/<\/head>/i,css+'\n'+js+'\n</head>');else s=s.replace(control,css+'\n'+js+'\n'+control);
fs.writeFileSync(p,s);modified.push(role+'/'+name);
}
// Global theme on public/auth pages too; no change to page behavior.
for(const name of fs.readdirSync(root).filter(n=>n.endsWith('.aspx')).map(n=>n).concat(fs.readdirSync(path.join(root,'Auth')).filter(n=>n.endsWith('.aspx')).map(n=>'Auth/'+n))){const p=path.join(root,name);let s=fs.readFileSync(p,'utf8');if(/<\/head>/i.test(s)&&!s.includes('just-theme.css')){s=s.replace(/<\/head>/i,'<link rel="stylesheet" href="<%= ResolveUrl("~/Assets/shared/just-theme.css") %>" />\n</head>');fs.writeFileSync(p,s);modified.push(name);}}
let proj=fs.readFileSync(path.join(root,'University-Internship-Management-System.csproj'),'utf8');
proj=proj.replace(/\s*<Content Include="Shared\\Sidebar.ascx"\s*\/>/g,'').replace(/\s*<Compile Include="Shared\\Sidebar.ascx(?:\.designer)?\.cs">[\s\S]*?<\/Compile>/g,'');
for(const file of ['Shared/RoleSidebarBase.cs',...roles.map(r=>'Shared/'+r+'Sidebar.ascx'),'Assets/shared/just-theme.css','Js_project/shared-role-navigation.js']){const f=file.replaceAll('/','\\'),tag=file.endsWith('.cs')?'Compile':'Content';if(!proj.includes('Include="'+f+'"'))proj=proj.replace('</Project>','<ItemGroup><'+tag+' Include="'+f+'" /></ItemGroup>\n</Project>');}
fs.writeFileSync(path.join(root,'University-Internship-Management-System.csproj'),proj);
fs.writeFileSync('Tests/shared-navigation-migration.json',JSON.stringify({modified,removedIds},null,2));console.log('Updated pages',modified.length,'; removed sidebar controls',JSON.stringify(removedIds));
