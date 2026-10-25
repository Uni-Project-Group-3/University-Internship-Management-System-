/* One drawer and sign-out path for every workspace role. */
(() => {
 'use strict';
 const asset = document.currentScript.src;
 const api = new URL('../FirebaseApi.ashx', asset);
 const login = new URL('../Auth/Login.aspx?account=new', asset);
 let signingOut = false;
 const isLogout = el => el && (el.hasAttribute('data-workspace-logout') || /logout/i.test(el.id || '') || /^log\s*out$/i.test((el.textContent || el.value || '').trim()));
 async function logout(control) {
  if(signingOut)return;signingOut=true;
  control.setAttribute('aria-busy','true');
  try {
   if(window.UimsFirebase?.logout){await window.UimsFirebase.logout();return;}
   let session;try{session=JSON.parse(sessionStorage.getItem('uims.firebase.authentication')||'null');}catch{}
   const response=await fetch(api,{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json',...(session?.tabTicket?{'X-UIMS-Session':session.tabTicket}:{})},body:JSON.stringify({action:'logout'}),signal:AbortSignal.timeout(15000)});
   const result=await response.json();if(!response.ok||!result.success)throw Error('Sign out failed');
   sessionStorage.removeItem('uims.firebase.authentication');
   location.replace(login.href);
  } catch {
   let feedback=document.getElementById('workspaceLogoutFeedback');if(!feedback){feedback=document.createElement('p');feedback.id='workspaceLogoutFeedback';feedback.setAttribute('role','alert');feedback.className='workspace-logout-feedback';document.body.append(feedback)}
   feedback.textContent='Could not sign out. Check your connection and try again.';
   signingOut=false;control.removeAttribute('aria-busy');
  }
 }
 // Window capture runs before old page/demo sign-out handlers on document.
 window.addEventListener('click',e=>{const el=e.target.closest?.('a,button,input[type="submit"]');if(isLogout(el)){e.preventDefault();e.stopImmediatePropagation();logout(el);}},true);
 function init(){
  const sidebar=document.querySelector('.workspace-canonical-sidebar');if(!sidebar)return;
  document.body.classList.add('workspace-canonical-shell');
  document.querySelectorAll('button[id*=Logout],button[id*=logout],input[id*=Logout],input[id*=logout]').forEach(el=>{el.disabled=false;el.removeAttribute('title')});
  const mobile=matchMedia('(max-width:991.98px)');
  let toggle=document.querySelector('.workspace-canonical-toggle');if(!toggle){toggle=document.createElement('button');toggle.type='button';toggle.className='workspace-canonical-toggle';toggle.textContent='☰ Menu';toggle.setAttribute('aria-label','Open navigation');document.body.append(toggle)}
  if(!sidebar.id)sidebar.id='workspaceSidebar';toggle.setAttribute('aria-controls',sidebar.id);
  let backdrop=document.querySelector('.workspace-canonical-backdrop');if(!backdrop){backdrop=document.createElement('button');backdrop.type='button';backdrop.className='workspace-canonical-backdrop';backdrop.setAttribute('aria-label','Close navigation');document.body.append(backdrop)}
  let previousFocus;
  function setOpen(open){open=Boolean(mobile.matches&&open);if(open)previousFocus=document.activeElement;document.body.classList.toggle('workspace-nav-open',open);toggle.setAttribute('aria-expanded',String(open));sidebar.inert=mobile.matches&&!open;backdrop.hidden=!open;if(open)sidebar.querySelector('[data-workspace-close]')?.focus();else if(mobile.matches&&previousFocus)previousFocus.focus();}
  window.addEventListener('click',e=>{const el=e.target.closest?.('.workspace-canonical-toggle,.menu-btn,.dash-menu-btn,.workspace-menu-toggle,.uims-sidebar-toggle,[data-sidebar-toggle],[data-workspace-close],.workspace-canonical-backdrop,.sidebar-backdrop,.dash-sidebar-backdrop');if(!el)return;e.preventDefault();e.stopImmediatePropagation();setOpen(el.matches('[data-workspace-close],.workspace-canonical-backdrop,.sidebar-backdrop,.dash-sidebar-backdrop')?false:!document.body.classList.contains('workspace-nav-open'));},true);
  window.addEventListener('keydown',e=>{if(!document.body.classList.contains('workspace-nav-open'))return;if(e.key==='Escape'){e.preventDefault();e.stopImmediatePropagation();setOpen(false)}if(e.key==='Tab'){const items=[...sidebar.querySelectorAll('a[href],button:not([disabled])')].filter(el=>el.getClientRects().length);const first=items[0],last=items.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last?.focus()}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first?.focus()}}},true);
  const current=location.pathname.replace(/\.aspx$/i,'').toLowerCase();sidebar.querySelectorAll('a.workspace-nav-link[href]').forEach(el=>{const url=new URL(el.href);const active=url.pathname.replace(/\.aspx$/i,'').toLowerCase()===current;el.classList.toggle('active',active);if(active)el.setAttribute('aria-current','page');else el.removeAttribute('aria-current')});
  sidebar.addEventListener('click',e=>{if(e.target.closest('a[href]')&&!isLogout(e.target.closest('a')))setOpen(false)});
  mobile.addEventListener('change',()=>setOpen(false));setOpen(false);
  window.addEventListener('pageshow',e=>{if(e.persisted)location.reload()});
 }
 if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();

/* Shared list and dialog presentation behavior. Records remain in their original DOM. */
(() => {
 'use strict';
 const eye='<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></svg>';
 function icons(){document.querySelectorAll('.workspace-nav-primary a').forEach(el=>{if(/^task exams$/i.test(el.textContent.trim()))el.remove();});document.querySelectorAll('button,a').forEach(el=>{if(/^view details$/i.test(el.textContent.trim())&&!el.querySelector('svg')){el.setAttribute('aria-label','View details');el.title='View details';el.classList.add('workspace-details-icon');el.innerHTML=eye;}})}
 function dismiss(dialog){const cancel=new Event('cancel',{cancelable:true});if(dialog.dispatchEvent(cancel))dialog.close();}
 document.addEventListener('click',e=>{const d=e.target;if(d instanceof HTMLDialogElement&&d.open){const r=d.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dismiss(d);}});
 document.addEventListener('workspace:saved',()=>{if(window.TaskDialogs?.activeSurface())return;document.querySelectorAll('dialog[open]').forEach(dismiss);document.querySelectorAll('.modal.show').forEach(m=>window.bootstrap?.Modal.getInstance(m)?.hide());});
 const tables=new WeakMap();
 function paginate(table){
  if(tables.has(table)||!table.tHead||!table.tBodies.length)return;
  const parent=table.closest('section,.panel,.companies-panel,.monitor-panel,.messages-card,.wr-card')||table.parentElement.parentElement;
  if(parent.querySelector('.management-pagination,.companies-pagination,.workspace-pagination,.pager,.pagination,.monitor-pagination,.messages-pagination,.trainees-pagination,#pageInfo,[id$="PageNumbers"]'))return;
  const pager=document.createElement('div');pager.className='workspace-pagination';pager.setAttribute('aria-label','Table pagination');pager.innerHTML='<span role="status" aria-live="polite"></span><div><label>Rows per page <select aria-label="Rows per page"><option>15</option><option>25</option><option>50</option></select></label><button type="button" aria-label="Previous page">←</button><nav aria-label="Table pages"></nav><button type="button" aria-label="Next page">→</button></div>';
  const wrap=table.closest('.table-responsive,.companies-table-wrap,.monitor-table-wrap,.monitor-table-scroll,.messages-table-wrap,.completion-table-wrap')||table;
  wrap.after(pager);let page=1;const select=pager.querySelector('select'),summary=pager.firstElementChild,buttons=pager.querySelectorAll('button'),nav=pager.querySelector('nav');let updating=false;
  function update(reset=false){if(updating)return;updating=true;if(reset)page=1;const rows=[...table.tBodies].flatMap(b=>[...b.rows]).filter(r=>r.cells.length>1);rows.forEach(r=>{if(r.hasAttribute('data-workspace-page-hidden')){r.style.display=r.dataset.workspacePreviousDisplay||'';delete r.dataset.workspacePageHidden;delete r.dataset.workspacePreviousDisplay;}});const visible=rows.filter(r=>!r.hidden&&r.style.display!=='none'),size=Number(select.value),pages=Math.max(1,Math.ceil(visible.length/size));page=Math.min(page,pages);visible.forEach((r,i)=>{if(i<(page-1)*size||i>=page*size){r.dataset.workspacePreviousDisplay=r.style.display;r.dataset.workspacePageHidden='';r.style.display='none';}});summary.textContent=visible.length?((page-1)*size+1)+'–'+Math.min(page*size,visible.length)+' of '+visible.length+' records':'No records';buttons[0].disabled=page===1;buttons[1].disabled=page===pages;nav.replaceChildren();for(let i=Math.max(1,page-2);i<=Math.min(pages,page+2);i++){const b=document.createElement('button');b.type='button';b.textContent=i;if(i===page)b.setAttribute('aria-current','page');b.onclick=()=>{page=i;update()};nav.append(b)}updating=false;}
  buttons[0].onclick=()=>{page--;update()};buttons[1].onclick=()=>{page++;update()};select.onchange=()=>update(true);parent.addEventListener('input',e=>{if(!pager.contains(e.target))setTimeout(()=>update(true),0)});parent.addEventListener('change',e=>{if(!pager.contains(e.target))setTimeout(()=>update(true),0)});new MutationObserver(()=>update()).observe(table,{childList:true,subtree:true});tables.set(table,update);update();
 }
 function scan(){icons();document.querySelectorAll('table').forEach(paginate)}
 let scheduled=false;const observer=new MutationObserver(records=>{for(const r of records){const el=r.target.nodeType===1?r.target:r.target.parentElement;const status=el?.closest?.('[role="status"],.text-success,.alert-success,.notice.success,[data-tone="success"]');const text=status?.textContent||'';if(status&&/\b(successfully|saved|created successfully|submitted successfully|updated successfully)\b/i.test(text)&&!/(no saved|unsaved|not saved|not been saved|not yet saved|could not|failed|unsuccessful|unavailable)/i.test(text)){if(window.TaskDialogs?.activeSurface())continue;const dialog=status.closest('dialog[open]');if(dialog)dismiss(dialog);const modal=status.closest('.modal.show');if(modal)window.bootstrap?.Modal.getInstance(modal)?.hide();if(!dialog&&!modal){const open=[...document.querySelectorAll('dialog[open]')].at(-1);if(open)dismiss(open);document.querySelectorAll('.modal.show').forEach(m=>window.bootstrap?.Modal.getInstance(m)?.hide());}}}if(!scheduled){scheduled=true;requestAnimationFrame(()=>{scheduled=false;scan()})}});
 function init(){scan();observer.observe(document.body,{childList:true,subtree:true,characterData:true});}
 if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();

document.addEventListener("click",event=>{const modal=event.target;if(modal?.matches?.(".modal.show")){const box=modal.querySelector(".modal-dialog,.modal-content");if(!box)return;const r=box.getBoundingClientRect();if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom){const instance=window.bootstrap?.Modal.getInstance(modal);if(instance)instance.hide();else modal.querySelector("[data-bs-dismiss],[data-dismiss],[data-close],.btn-close")?.click();}}});

(() => {
 'use strict';const managed=new WeakMap();
 function attach(host){if(managed.has(host)||!host.children.length||![...host.children].some(e=>e.matches('article,.col-lg-4,.col-md-6,[class*="col-"]')))return;const pager=document.createElement('div');pager.className='workspace-pagination';pager.innerHTML='<span role="status" aria-live="polite"></span><div><label>Rows per page <select aria-label="Rows per page"><option>15</option><option>25</option><option>50</option></select></label><button type="button" aria-label="Previous page">←</button><nav aria-label="List pages"></nav><button type="button" aria-label="Next page">→</button></div>';host.after(pager);let page=1;const select=pager.querySelector('select'),buttons=pager.querySelectorAll('button'),nav=pager.querySelector('nav');
 function update(reset=false){if(reset)page=1;const rows=[...host.children].filter(e=>e.matches('article,.col-lg-4,.col-md-6,[class*="col-"]'));rows.forEach(e=>{if(e.dataset.workspaceCardHidden!==undefined){e.style.display=e.dataset.workspaceCardDisplay||'';delete e.dataset.workspaceCardHidden;delete e.dataset.workspaceCardDisplay}});const visible=rows.filter(e=>!e.hidden&&e.style.display!=='none'),size=Number(select.value),pages=Math.max(1,Math.ceil(visible.length/size));page=Math.min(page,pages);visible.forEach((e,i)=>{if(i<(page-1)*size||i>=page*size){e.dataset.workspaceCardHidden='';e.dataset.workspaceCardDisplay=e.style.display;e.style.display='none'}});pager.firstElementChild.textContent=visible.length?((page-1)*size+1)+'–'+Math.min(page*size,visible.length)+' of '+visible.length+' records':'No records';buttons[0].disabled=page===1;buttons[1].disabled=page===pages;nav.replaceChildren();for(let i=Math.max(1,page-2);i<=Math.min(pages,page+2);i++){const b=document.createElement('button');b.type='button';b.textContent=i;if(i===page)b.setAttribute('aria-current','page');b.onclick=()=>{page=i;update()};nav.append(b)}}
 buttons[0].onclick=()=>{page--;update()};buttons[1].onclick=()=>{page++;update()};select.onchange=()=>update(true);new MutationObserver(()=>update()).observe(host,{childList:true});document.addEventListener('input',e=>{if(!pager.contains(e.target))setTimeout(()=>update(true),0)});document.addEventListener('change',e=>{if(!pager.contains(e.target))setTimeout(()=>update(true),0)});managed.set(host,update);update();}
 function scan(){document.querySelectorAll('#opportunitiesList,.wr-report-list,#examList').forEach(attach)}
 if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',scan);else scan();let pending=false;new MutationObserver(()=>{if(!pending){pending=true;requestAnimationFrame(()=>{pending=false;scan()})}}).observe(document.documentElement,{childList:true,subtree:true});
})();

(()=>{document.addEventListener('DOMContentLoaded',()=>{document.querySelectorAll('a,button').forEach(el=>{if(!/^change password$/i.test(el.textContent.trim()))return;const link=document.createElement('a');link.className=el.className;link.innerHTML=el.innerHTML;link.href=new URL('../Auth/Login.aspx?account=new&reset=1',document.querySelector('script[src*="workspace-navigation-unified"]').src);link.target='_blank';link.rel='noopener';link.setAttribute('aria-label','Change password using a secure email link (opens a new tab)');el.replaceWith(link)});});})();
/* In-app notifications: load the shared bell once on every workspace page. */
(() => { const src = document.currentScript && document.currentScript.src; if (!src || document.querySelector('script[data-uims-notifications]')) return; const tag = document.createElement('script'); tag.src = new URL('notifications.js?v=20261025', src).href; tag.dataset.uimsNotifications = '1'; document.head.appendChild(tag); })();
