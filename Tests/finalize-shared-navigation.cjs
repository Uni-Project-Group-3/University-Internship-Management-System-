const fs=require('fs'),path=require('path'),root=path.resolve('University-Internship-Management-System');
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).filter(e=>!['bin','obj','packages','Scripts'].includes(e.name)).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);}
for(const p of walk(root).filter(p=>/\.(aspx|Master)$/i.test(p))){let s=fs.readFileSync(p,'utf8');if(/<\/head>/i.test(s)&&!s.includes('just-theme.css')){s=s.replace(/<\/head>/i,'<link rel="stylesheet" href="<%= ResolveUrl("~/Assets/shared/just-theme.css") %>" />\n</head>');fs.writeFileSync(p,s);}}
// Compatibility import for pages/styles that still load the old palette entry point.
fs.writeFileSync(path.join(root,'Assets/shared/colors.css'),'/* Compatibility entry point: canonical tokens live in just-theme.css. */\n@import url("./just-theme.css");\n');
const test='Tests/SharedNavigation.browser.cjs';let s=fs.readFileSync(test,'utf8').replaceAll("name:'☰ Menu'","name:/Menu/").replace("res.setHeader('Content-Type','text/html')","res.setHeader('Content-Type','text/html; charset=utf-8')");fs.writeFileSync(test,s);
// Transferred final-evaluation pages share the same role navigation. Embedded popups do not render a second shell.
const final=path.join(root,'FinalReports.aspx');s=fs.readFileSync(final,'utf8');s=s.replace('<!DOCTYPE html>',`<script runat="server">
protected override void OnInit(EventArgs e) {
 base.OnInit(e);
 if (Request.QueryString["embedded"] == "1") return;
 string role = University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole((Context.User?.Identity as System.Web.Security.FormsIdentity)?.Ticket.UserData);
 string name = role == "student" ? "Student" : role == "companySupervisor" ? "Company" : role == "academicSupervisor" ? "Academic" : role == "coordinator" ? "Coordinator" : null;
 if (name != null) FinalRoleNavigation.Controls.Add(LoadControl("~/Shared/" + name + "Sidebar.ascx"));
}
</script>
<!DOCTYPE html>`).replace('<body><form runat="server">','<body><form runat="server"><asp:PlaceHolder ID="FinalRoleNavigation" runat="server" />').replace('</head>','<script src="<%= ResolveUrl("~/Js_project/shared-role-navigation.js") %>" defer></script>\n</head>');fs.writeFileSync(final,s);
