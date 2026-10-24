// Isolated QR rendering/error feedback; no network or production writes.
const fs=require('fs'),path=require('path'),assert=require('assert');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.PROGRAMFILES+'/Google/Chrome/Application/chrome.exe',headless:true});
 try{
  const page=await browser.newPage();await page.route('**/*',r=>r.abort());
  await page.setContent('<button id="open">Show QR</button><dialog id="qrDialog"><h2 id="qrTitle"></h2><div id="qrCode"></div><strong id="qrCountdown"></strong><p id="qrDescription"></p></dialog>');
  const root=path.resolve(__dirname,'../../University-Internship-Management-System');
  await page.addScriptTag({content:fs.readFileSync(path.join(root,'Js_project/Student/attendance-qrcodegen.js'),'utf8')});
  const source=fs.readFileSync(path.join(root,'Js_project/Student/my-attendance.js'),'utf8');
  const functions=source.slice(source.indexOf('async function showQr('),source.indexOf('document.getElementById("qrDialog").addEventListener("close"'));
  await page.addScriptTag({content:`let loading=false,saving=false,attendance={canCheckIn:true,canCheckOut:true,student:{name:'Offline student'},company:{name:'Offline company'}},qrTimer,qrDeadline,qrAction;let mode='missing';function openDialog(){document.getElementById('qrDialog').showModal();}function showNotice(){}function renderStudent(){document.getElementById('open').disabled=false;}async function api(){if(mode==='missing')throw new Error('No current QR. Ask your supervisor to issue one, then try again.');return {token:'UIMSATT1:P:'+'A'.repeat(240)+'.'+'B'.repeat(43),expiresAt:Date.now()+120000,serverNow:Date.now()};}${functions}`});
  await page.evaluate(()=>showQr('checkIn',document.getElementById('open')));
  assert(await page.locator('#qrDialog').evaluate(d=>d.open));assert((await page.locator('#qrDescription').textContent()).includes('supervisor to issue'));assert.strictEqual(await page.locator('#qrCode svg').count(),0);
  await page.evaluate(()=>{document.getElementById('qrDialog').close();mode='ready';return showQr('checkIn',document.getElementById('open'));});
  assert(await page.locator('#qrDialog').evaluate(d=>d.open));assert.strictEqual(await page.locator('#qrCode svg').count(),1);assert((await page.locator('#qrCountdown').textContent()).includes('Expires in'));
  await page.addScriptTag({content:fs.readFileSync(path.join(root,'Js_project/Company/attendance-jsqr.js'),'utf8')});
  const decoded=await page.evaluate(async()=>{
    const svg=document.querySelector('#qrCode svg'), image=new Image();
    image.src='data:image/svg+xml;base64,'+btoa(new XMLSerializer().serializeToString(svg));
    await image.decode();
    const canvas=document.createElement('canvas');canvas.width=canvas.height=600;
    const context=canvas.getContext('2d');context.fillStyle='white';context.fillRect(0,0,600,600);
    context.drawImage(image,40,40,520,520);
    const pixels=context.getImageData(0,0,600,600);
    return jsQR(pixels.data,pixels.width,pixels.height,{inversionAttempts:'attemptBoth'})?.data;
  });
  assert.strictEqual(decoded,'UIMSATT1:P:'+'A'.repeat(240)+'.'+'B'.repeat(43),'The company decoder must read the exact student token');
  const scannerSource=fs.readFileSync(path.join(root,'Js_project/Company/verify-attendance.js'),'utf8');
  const scanFrame=scannerSource.slice(scannerSource.indexOf('async function scanFrame('),scannerSource.indexOf('document.getElementById("openCamera").addEventListener'));
  await page.addScriptTag({content:`let cameraStream=null,cameraVersion=1,nativeQrDetector=null,lastFrameAt=0,scanScaleIndex=0,frameId;const scannerDiagnosticsEnabled=false;const video=document.createElement('video'),scanCanvas=document.createElement('canvas'),scanContext=scanCanvas.getContext('2d',{willReadFrequently:true});let scannedTokens=[];function scannerDiagnostic(){}function stopCamera(){cameraVersion++;cameraStream?.getTracks().forEach(t=>t.stop());cameraStream=null;}function processToken(token){scannedTokens.push(token);}window.requestAnimationFrame=()=>0;${scanFrame}`});
  await page.evaluate(async()=>{
    const image=new Image();image.src='data:image/svg+xml;base64,'+btoa(new XMLSerializer().serializeToString(document.querySelector('#qrCode svg')));await image.decode();
    const feed=document.createElement('canvas');feed.width=1280;feed.height=720;
    const context=feed.getContext('2d');context.fillStyle='white';context.fillRect(0,0,1280,720);context.drawImage(image,410,130,460,460);
    cameraStream=feed.captureStream(10);video.srcObject=cameraStream;video.muted=true;video.playsInline=true;await video.play();
    if(video.readyState<2 || !video.videoWidth)throw new Error('Synthetic video has no readable frame');
    await scanFrame(300);await scanFrame(600);stopCamera();
  });
  assert((await page.evaluate(()=>scannedTokens)).every(token=>token===decoded));
  assert((await page.evaluate(()=>scannedTokens.length))>=1,'The real company frame loop must decode a student QR from a video stream');
  await page.evaluate(()=>clearInterval(qrTimer));
  console.log('PASS student QR SVG + company decoder + real scanner frame loop on synthetic video; server countdown and visible failures. All network blocked.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
