/* Server-issued tab handle. No browser-defined role or stored Firebase token. */
(() => {
 'use strict';
 const root=new URL('../../',document.currentScript.src),key='uims.tab.ticket';
 const gateway=new URL('Auth/Gateway.ashx',root),login=new URL('Auth/Login.aspx',root);
 let generation=0;const listeners=new Set();
 function clear(){generation++;listeners.forEach(dispose=>{try{dispose();}catch{}});listeners.clear();sessionStorage.removeItem(key);}
 async function request(action,payload={},parameters={}){
  const current=generation,url=new URL(gateway);url.searchParams.set('action',action);
  Object.entries(parameters).forEach(([name,value])=>url.searchParams.set(name,String(value)));
  const response=await fetch(url,{method:'POST',credentials:'omit',cache:'no-store',headers:{'Content-Type':'application/json',Authorization:'Bearer '+(sessionStorage.getItem(key)||'')},body:JSON.stringify(payload)});
  const body=await response.json();if(current!==generation)throw Error('The tab identity changed.');
  if(response.status===401){clear();throw Error('Your session expired. Please sign in again.');}
  if(!response.ok)throw Error(body.message||'This operation is unavailable.');return body;
 }
 function navigate(destination){
  const url=new URL(destination,location.href);if(url.origin!==root.origin||!url.pathname.startsWith(root.pathname))throw Error('Invalid navigation target.');
  const handle=sessionStorage.getItem(key);if(!handle){location.assign(login);return;}
  const form=document.createElement('form');form.method='POST';form.action=url.href;
  const field=document.createElement('input');field.type='hidden';field.name='UimsTabTicket';field.value=handle;form.append(field);document.body.append(form);form.submit();
 }
 async function logout(){document.body.inert=true;try{await request('logout');}finally{clear();document.querySelector('.content,main')?.replaceChildren();location.replace(login);}}
 window.UimsTab={request,navigate,logout,studentData:section=>request('studentData',{}, {section}),disposeOnIdentityChange:dispose=>listeners.add(dispose)};
 document.addEventListener('click',event=>{
  const link=event.target.closest('a,button,input');if(!link)return;
  if(/logout/i.test(link.id)||link.hasAttribute?.('data-uims-logout')||/\b(?:log\s*out|sign\s*out)\s*$/i.test((link.textContent||'').trim())){event.preventDefault();event.stopImmediatePropagation();logout();return;}
  if(link.tagName!=='A'||event.ctrlKey||event.metaKey||event.shiftKey||link.target==='_blank')return;
  const target=new URL(link.getAttribute('href')||'#',location.href);
  if(target.origin===root.origin&&/\/(Student|Company|Academic|Coordinator)\//i.test(target.pathname)){event.preventDefault();event.stopImmediatePropagation();navigate(target);}
 },true);
 function bindTicket(form){
  if(!sessionStorage.getItem(key)||form.tagName!=='FORM')return;
  const target=new URL(form.action||location.href,location.href);
  if(target.origin!==root.origin||!target.pathname.startsWith(root.pathname))return;
  let field=form.querySelector('[name=UimsTabTicket]');
  if(!field){field=document.createElement('input');field.type='hidden';field.name='UimsTabTicket';form.append(field);}
  field.value=sessionStorage.getItem(key);
 }
 function bindForms(){document.querySelectorAll('form').forEach(bindTicket);document.querySelectorAll('a,button').forEach(el=>{if(/\b(?:log\s*out|sign\s*out)\s*$/i.test((el.textContent||'').trim())){el.dataset.uimsLogout='';el.removeAttribute('aria-disabled');el.removeAttribute('disabled');el.removeAttribute('title');if(el.tagName==='A'&&!el.getAttribute('href'))el.href=login.href;}});}
 document.addEventListener('submit',event=>bindTicket(event.target));
 if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',bindForms);else bindForms();
 if(window.Sys?.Application)window.Sys.Application.add_load(bindForms);
 window.addEventListener('pageshow',async()=>{
  if(!/\/(Student|Company|Academic|Coordinator)\//i.test(location.pathname))return;
  document.body.inert=true;try{await request('session');document.body.inert=false;}catch{clear();location.replace(login);}
 });
})();
