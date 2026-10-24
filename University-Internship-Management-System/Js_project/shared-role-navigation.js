(() => {
 'use strict';
 const sidebar=document.querySelector('[data-uims-sidebar-role]');if(!sidebar)return;
 document.body.classList.add('uims-role-shell');
 const backdrop=document.querySelector('.uims-unified-backdrop'),close=sidebar.querySelector('[data-sidebar-close]');
 const button=document.createElement('button');button.type='button';button.className='uims-sidebar-toggle';button.textContent='☰ Menu';button.setAttribute('aria-controls',sidebar.id);button.setAttribute('aria-expanded','false');document.body.append(button);
 const mobile=matchMedia('(max-width:1000px)');let previousFocus;
 function setOpen(open){open=mobile.matches&&open;if(open)previousFocus=document.activeElement;document.body.classList.toggle('uims-navigation-open',open);button.setAttribute('aria-expanded',String(open));sidebar.inert=mobile.matches&&!open;if(backdrop)backdrop.setAttribute('aria-hidden',String(!open));if(open)close?.focus();else if(previousFocus&&mobile.matches)previousFocus.focus();}
 button.addEventListener('click',()=>setOpen(true));close?.addEventListener('click',e=>{e.preventDefault();e.stopImmediatePropagation();setOpen(false);},true);backdrop?.addEventListener('click',()=>setOpen(false));
 document.addEventListener('keydown',e=>{if(!document.body.classList.contains('uims-navigation-open'))return;if(e.key==='Escape'){e.preventDefault();e.stopImmediatePropagation();setOpen(false);}if(e.key==='Tab'){const items=[...sidebar.querySelectorAll('a[href],button:not([disabled])')].filter(el=>el.getClientRects().length);const first=items[0],last=items.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}},true);
 mobile.addEventListener('change',()=>setOpen(false));setOpen(false);
 // Legacy scripts can keep their page behavior, but cannot operate a second drawer.
 document.addEventListener('click',e=>{const toggle=e.target.closest('.menu-btn,.dash-menu-btn,[data-sidebar-toggle]');if(toggle){e.preventDefault();e.stopImmediatePropagation();setOpen(!document.body.classList.contains('uims-navigation-open'));}},true);
 sidebar.querySelector('[data-uims-logout]')?.addEventListener('click',async e=>{
  e.preventDefault();e.stopImmediatePropagation();
  if(window.UimsFirebase){await window.UimsFirebase.logout();return;}
  const link=e.currentTarget,login=new URL(link.href),api=new URL('../FirebaseApi.ashx',login);
  let auth;try{auth=JSON.parse(sessionStorage.getItem('uims.firebase.authentication')||'null');}catch{}
  try{const response=await fetch(api,{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json',...(auth?.tabTicket?{'X-UIMS-Session':auth.tabTicket}:{})},body:JSON.stringify({action:'logout'}),signal:AbortSignal.timeout(15000)});if(!response.ok)throw Error('Sign-out failed');sessionStorage.removeItem('uims.firebase.authentication');location.replace(login.href);}catch{link.textContent='Sign out failed — retry';}
 },true);
})();
