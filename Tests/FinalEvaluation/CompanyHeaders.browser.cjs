// CSS-only rendering of real page markup. Scripts/remote requests are disabled; no Firebase I/O.
const fs=require('fs'),path=require('path'),http=require('http'),assert=require('assert');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const root=path.resolve(__dirname,'../../University-Internship-Management-System');
const server=http.createServer((req,res)=>{const file=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname);if(!file.startsWith(root+path.sep)){res.writeHead(404);return res.end();}try{let content=fs.readFileSync(file);if(file.endsWith('.aspx'))content=content.toString().replace(/<%@[^%]*%>/g,'').replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi,'');res.setHeader('Content-Type',file.endsWith('.css')?'text/css':file.endsWith('.png')?'image/png':'text/html');res.end(content);}catch{res.writeHead(404);res.end();}});
(async()=>{await new Promise(r=>server.listen(0,'127.0.0.1',r));const base='http://127.0.0.1:'+server.address().port,browser=await chromium.launch({executablePath:process.env.PROGRAMFILES+'/Google/Chrome/Application/chrome.exe',headless:true});try{
const page=await browser.newPage({viewport:{width:1500,height:1000},reducedMotion:'reduce'});await page.route('**/*',r=>new URL(r.request().url()).origin===base?r.continue():r.abort());const folder=path.join(__dirname,'../Reports/CompanyHeaders');fs.mkdirSync(folder,{recursive:true});
for(const name of ['ManageTasks','VerifyAttendance','WeeklyReports']){
await page.setViewportSize({width:1500,height:1000});await page.goto(base+'/Company/'+name+'.aspx');const hero=page.locator('.company-reference-hero');assert.strictEqual(await hero.count(),1);
const style=await hero.evaluate(el=>{const s=getComputedStyle(el);return{background:s.backgroundImage,radius:s.borderRadius,color:getComputedStyle(el.querySelector('h1')).color};});assert(style.background.includes('linear-gradient'));assert.strictEqual(style.radius,'28px');assert.strictEqual(style.color,'rgb(255, 255, 255)');await hero.screenshot({path:path.join(folder,name+'.png')});
await page.setViewportSize({width:390,height:844});assert((await hero.boundingBox()).width<=390);await hero.screenshot({path:path.join(folder,name+'-mobile.png')});
}
console.log('PASS three company headers: actual CSS gradient, white heading, rounded corners and mobile width. No scripts or Firebase requests.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;}).finally(()=>server.close());
