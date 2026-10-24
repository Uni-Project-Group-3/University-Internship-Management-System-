// Actual login markup/scripts, isolated API responses only. No live authentication requests.
const fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const root=path.resolve(__dirname,'../../University-Internship-Management-System');
function markup(required=false){
 let html=fs.readFileSync(path.join(root,'Auth/Login.aspx'),'utf8').replace(/<%@[^%]*%>/g,'').replace(/<%=.*?%>/g,'2026');
 const attr=(s,k)=>new RegExp('\\b'+k+'="([^"]*)"','i').exec(s)?.[1]||'';
 html=html.replace(/<asp:Literal\b[^>]*\/>/gi,'').replace(/<asp:(TextBox|Button|HiddenField)\b([^>]*?)\/>/gi,(_,kind,a)=>{
  const id=attr(a,'ID'),type=kind==='Button'?'submit':kind==='HiddenField'?'hidden':attr(a,'TextMode').toLowerCase()||'text';
  return '<input id="'+id+'" name="'+id+'" type="'+type+'" class="'+attr(a,'CssClass')+'" value="'+(kind==='Button'?attr(a,'Text'):kind==='HiddenField'?'isolated-csrf':'')+'" '+a.replace(/\b(?:ID|TextMode|Text|CssClass|ClientIDMode|runat|OnClick|CausesValidation)="[^"]*"/gi,'')+'>';
 });
 html=html.replace(/\brunat="server"/g,'').replace(/(["'])~\//g,'$1/').replace('class="auth-layout"','class="auth-layout" data-password-required="'+required+'"');
 return html;
}
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.PROGRAMFILES+'/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const context=await browser.newContext({viewport:{width:1440,height:1000}}),errors=[];
  let outcome='failure',role='student',signIns=0,resetRequests=0,logouts=0,navigation,disableAnimation=false;
  await context.route('**/*',async route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.hostname!=='uims-login.test')return route.abort();
   if(url.pathname==='/FirebaseApi.ashx'){
    const body=request.postDataJSON();
    if(body.action==='identity')return route.fulfill({json:{success:true,data:{role,profileId:'isolated-profile'}}});
    if(body.action==='logout'){logouts++;assert.strictEqual(request.headers()['x-uims-session'],'isolated-ticket');return route.fulfill({json:{success:true,data:{signedOut:true}}});}
    assert.strictEqual(body.action,'signIn');signIns++;
    await new Promise(resolve=>setTimeout(resolve,180));
    if(outcome==='failure')return route.fulfill({status:401,json:{success:false,message:'The email or password is incorrect.'}});
    return route.fulfill({json:{success:true,data:{mustChangePassword:outcome==='firstLogin',idToken:'isolated-token',refreshToken:'isolated-refresh',tabTicket:'isolated-ticket',expiresIn:1800,role,profileId:'isolated-profile'}}});
   }
   if(url.pathname==='/Auth/Login.aspx'){
    if(url.searchParams.get('action')==='reset'){resetRequests++;assert(request.postData().includes('csrfToken=isolated-csrf'));return route.fulfill({json:{success:true,message:'Request received'}});}
    return route.fulfill({contentType:'text/html',body:markup(url.searchParams.get('change')==='required')});
   }
   if(disableAnimation && url.pathname.endsWith('/Auth/login-card.js'))return route.fulfill({contentType:'application/javascript',body:'/* Optional animation unavailable */'});
   if(/Dashboard\.aspx$/.test(url.pathname)){navigation={url:url.pathname,method:request.method(),body:request.postData()};return route.fulfill({contentType:'text/html',body:'<main>Authorized workspace<button id="testLogout">Logout</button></main><script src="/Js_project/firebase-client.js"></script>'});}
   const file=path.resolve(root,'.'+url.pathname);if(!file.startsWith(root+path.sep))return route.abort();
   if(!fs.existsSync(file))return route.fulfill({status:404,body:''});
   return route.fulfill({contentType:file.endsWith('.js')?'application/javascript':file.endsWith('.css')?'text/css':'image/png',body:fs.readFileSync(file)});
  });
  const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));
  const open=()=>page.goto('https://uims-login.test/Auth/Login.aspx?account=new');
  await open();
  await page.locator('#SignIn').click();assert.strictEqual(signIns,0);await page.locator('#emailError').getByText('Enter a valid email address.').waitFor();
  await page.locator('#Email').fill('person@example.test');await page.locator('#Password').fill('NeverMirrorThis!9');
  assert.strictEqual(await page.locator('#previewEmail').textContent(),'person@example.test');
  assert.match(await page.locator('#previewPassword').textContent(),/^•+$/);
  assert(!await page.locator('body').textContent().then(t=>t.includes('NeverMirrorThis!9')));
  await page.getByRole('button',{name:'Show password',exact:true}).click();
  assert.strictEqual(await page.locator('#Password').getAttribute('type'),'text');assert.match(await page.locator('#previewPassword').textContent(),/^•+$/);
  await page.getByRole('button',{name:'Hide password',exact:true}).click();
  await page.locator('#Password').press('Enter');
  await page.locator('#cardFlipper.flipped').waitFor();
  await page.evaluate(()=>{for(let i=0;i<10;i++)document.querySelector('.auth-layout').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));});
  await page.locator('#SignIn:enabled').waitFor();assert.strictEqual(signIns,1);
  assert.strictEqual(await page.locator('#Email').inputValue(),'person@example.test');
  assert.strictEqual(await page.locator('#cardFlipper').evaluate(e=>e.classList.contains('flipped')),false);
  assert((await page.locator('#firebaseConnectionStatus').textContent()).includes('incorrect'));
  assert(!await page.evaluate(()=>JSON.stringify({...sessionStorage,...localStorage})).then(t=>t.includes('NeverMirrorThis!9')));
  await page.locator('#ForgotPassword').click();await page.locator('#SendResetLink').click();await page.locator('#resetConfirmation:visible').waitFor();assert.strictEqual(resetRequests,1);
  await page.getByRole('button',{name:'Close password reset'}).click();
  await open();
  fs.mkdirSync(path.resolve(__dirname,'../../Artifacts/Login-UI'),{recursive:true});
  await page.screenshot({path:path.resolve(__dirname,'../../Artifacts/Login-UI/desktop.png'),fullPage:true});
  for(const width of [320,390,768]){
   await page.setViewportSize({width,height:900});
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'No horizontal overflow at '+width);
   assert(await page.locator('#SignIn').isVisible());
  }
  await page.setViewportSize({width:390,height:844});await page.screenshot({path:path.resolve(__dirname,'../../Artifacts/Login-UI/mobile.png'),fullPage:true});
  for(const [userRole,destination] of [['student','Student/StudentDashboard.aspx'],['companySupervisor','Company/CompanyDashboard.aspx'],['academicSupervisor','Academic/AcademicDashboard.aspx'],['coordinator','Coordinator/CoordinatorDashboard.aspx']]){
   role=userRole;outcome='success';await open();
   await page.locator('#Email').fill('person@example.test');await page.locator('#Password').fill('NeverMirrorThis!9');
   await Promise.all([page.waitForURL('**/'+destination),page.locator('#SignIn').click()]);
   assert.strictEqual(navigation.method,'POST');assert(navigation.body.includes('__FirebaseToken=isolated-token')&&navigation.body.includes('__UimsSession=isolated-ticket'));
   const stored=await page.evaluate(()=>JSON.parse(sessionStorage.getItem('uims.firebase.authentication')));assert.strictEqual(stored.tabTicket,'isolated-ticket');assert(!JSON.stringify(stored).includes('NeverMirrorThis'));
  }
  const other=await context.newPage();await other.goto('https://uims-login.test/Auth/Login.aspx?account=new');
  await other.evaluate(()=>sessionStorage.setItem('uims.firebase.authentication',JSON.stringify({tabTicket:'other-tab-ticket',refreshToken:'other-refresh',idToken:'other-token',expiresAt:Date.now()+1000000})));
  const beforeLogout=signIns;
  await Promise.all([page.waitForURL('**/Auth/Login.aspx?account=new'),page.locator('#testLogout').click()]);
  assert.strictEqual(logouts,1);assert.strictEqual(signIns,beforeLogout);assert.strictEqual(await page.evaluate(()=>sessionStorage.getItem('uims.firebase.authentication')),null);
  assert.strictEqual(await page.locator('#Email').inputValue(),'');assert.strictEqual(await page.locator('#Password').inputValue(),'');
  assert.strictEqual(await other.evaluate(()=>JSON.parse(sessionStorage.getItem('uims.firebase.authentication')).tabTicket),'other-tab-ticket');await other.close();
  role='student';await page.locator('#Email').fill('another-person@example.test');await page.locator('#Password').fill('AnotherIsolated!9');
  await Promise.all([page.waitForURL('**/Student/StudentDashboard.aspx'),page.locator('#SignIn').click()]);assert.strictEqual(signIns,beforeLogout+1);
  role='coordinator';
  await page.emulateMedia({reducedMotion:'reduce'});await open();
  await page.locator('#Email').fill('person@example.test');await page.locator('#Password').fill('NeverMirrorThis!9');
  await Promise.all([page.waitForURL('**/Coordinator/CoordinatorDashboard.aspx'),page.locator('#SignIn').click()]);
  disableAnimation=true;await open();await page.locator('#Email').fill('person@example.test');await page.locator('#Password').fill('NeverMirrorThis!9');
  await Promise.all([page.waitForURL('**/Coordinator/CoordinatorDashboard.aspx'),page.locator('#SignIn').click()]);disableAnimation=false;
  outcome='firstLogin';await open();await page.locator('#Email').fill('person@example.test');await page.locator('#Password').fill('NeverMirrorThis!9');
  await Promise.all([page.waitForURL('**/Auth/Login.aspx?change=required'),page.locator('#SignIn').click()]);await page.locator('#firstLoginDialog[open]').waitFor();
  assert.strictEqual(await page.evaluate(()=>sessionStorage.getItem('uims.firebase.authentication')),null);
  assert.deepStrictEqual(errors,[]);
  console.log('PASS animated login: live email, masked card, keyboard, duplicate guard, failure recovery, reset, all four role redirects, existing tab session, reduced motion, first-login password change, 320/390/768 layouts. All APIs mocked; no production writes.');
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
