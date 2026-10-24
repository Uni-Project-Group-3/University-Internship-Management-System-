const fs=require('fs');const p='Tests/SharedNavigation.browser.cjs';let s=fs.readFileSync(p,'utf8');
s=s.replace("assert.deepEqual(errors,[]);await page.close();",`await page.setViewportSize({width:1440,height:900});
await page.route('**/FirebaseApi.ashx',async route=>{assert.equal(JSON.parse(route.request().postData()).action,'logout');assert.equal(route.request().headers()['x-uims-session'],'isolated-signed-tab');await route.fulfill({contentType:'application/json',body:JSON.stringify({success:true,data:{signedOut:true}})});});
await page.evaluate(()=>sessionStorage.setItem('uims.firebase.authentication',JSON.stringify({tabTicket:'isolated-signed-tab'})));
await page.locator('[data-uims-logout]').click();await page.waitForURL('**/Auth/Login.aspx?account=new');assert.equal(await page.evaluate(()=>sessionStorage.getItem('uims.firebase.authentication')),null);
assert.deepEqual(errors,[]);await page.close();`);
s=s.replace('top-layer task popup. External','top-layer task popup, existing logout API and cleared tab session. External');fs.writeFileSync(p,s);
