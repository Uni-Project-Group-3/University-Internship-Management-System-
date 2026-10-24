
(function(){
 /* ===== CONFIG – edit here =====
    photo: image URL ('' shows a marked placeholder) · links: [{label:'LinkedIn',url:'https://…'}] (leave [] if none) */
 const CONFIG={
  home:'home.aspx',   // <- your homepage URL
  links:{login:'../Auth/Login.aspx',contact:'../Contact.aspx'},   // same targets as the homepage
  team:[
    {
      "name": "Yasmeen Talafha",
      "role": "Computer Science",
      "bio": "",
      "photo": "../Assets/public/images/team/yasmeen-talafha.png",
      "alt": "Portrait of Yasmeen Talafha",
      "links": [
        {
          "label": "Portfolio",
          "url": "https://yasmeenht.github.io/Project1/"
        }
      ],
      "placeholder": false
    },
    {
      "name": "Deyaa Alwardat",
      "role": "Computer Science",
      "bio": "",
      "photo": "../Assets/public/images/team/deyaa-alwardat.png",
      "alt": "Portrait of Deyaa Alwardat",
      "links": [
        {
          "label": "Portfolio",
          "url": "https://deyaa-alwardat.github.io/Deyaa_Alwardat_Personal_Portfolio/"
        }
      ],
      "placeholder": false
    },
    {
      "name": "Roa’a Basil Ajlouni",
      "role": "Computer Science",
      "bio": "",
      "photo": "../Assets/public/images/team/roaa-basil-ajlouni.png",
      "alt": "Portrait of Roa’a Basil Ajlouni",
      "links": [
        {
          "label": "Portfolio",
          "url": "https://roaaajlouni365-a11y.github.io/Portfolio/"
        }
      ],
      "placeholder": false
    },
    {
      "name": "Anas Ghawanmeh",
      "role": "Computer Information Systems",
      "bio": "",
      "photo": "../Assets/public/images/team/anas-ghawanmeh.png",
      "alt": "Portrait of Anas Ghawanmeh",
      "links": [
        {
          "label": "Portfolio",
          "url": "https://aghawanmeh857-alt.github.io/My-Portfolio/"
        }
      ],
      "placeholder": false
    },
    {
      "name": "Mohammad Mustafa",
      "role": "Computer Science",
      "bio": "",
      "photo": "../Assets/public/images/team/mohammad-mustafa.png",
      "alt": "Portrait of Mohammad Mustafa",
      "links": [
        {
          "label": "Portfolio",
          "url": "https://moazzam9444-wq.github.io/Portfolio/"
        }
      ],
      "placeholder": false
    },
    {
      "name": "Tariq Bataineh",
      "role": "Computer Science",
      "bio": "",
      "photo": "",
      "alt": "",
      "links": [],
      "placeholder": false
    },
    {
      "name": "Eiad Mansour",
      "role": "Computer Science",
      "bio": "",
      "photo": "",
      "alt": "",
      "links": [],
      "placeholder": false
    }
  ]
 };
 const $=s=>document.querySelector(s),$$=s=>[...document.querySelectorAll(s)];
 const RM=()=>matchMedia('(prefers-reduced-motion:reduce)').matches;
 const esc=t=>String(t).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
 const tints=['#b9cfe6,#dfe6ee','#c9d6e6,#e9e4d2','#aac3dc,#d4dde6','#d6dccf,#b7cbe0','#bfd0e4,#e4dcc6','#a9c6df,#e7ebef'];
 const sil='<svg viewBox="0 0 100 120" fill="#0b2c5a" aria-hidden="true"><circle cx="50" cy="38" r="22"/><path d="M6 120c0-30 20-48 44-48s44 18 44 48z"/></svg>';
 const T=CONFIG.team,n=T.length,story=$('#story'),tilesEl=$('#tiles'),info=$('#info');
 $('#n').textContent=String(n).padStart(2,'0');story.style.setProperty('--n',n);
 const pic=m=>m.photo?`<img src="${esc(m.photo)}" alt="${esc(m.alt||'')}" loading="lazy">`:`<div class="ph">${sil}<span class="stamp">PHOTO PLACEHOLDER</span></div>`;
 const bg=i=>`linear-gradient(170deg,${tints[i%tints.length]})`;
 const soc=m=>m.links&&m.links.length?m.links.map(l=>`<li><a href="${esc(l.url)}" target="_blank" rel="noopener noreferrer">${esc(l.label)}</a></li>`).join(''):(m.placeholder?'<li><span>Social links appear here</span></li>':'');
 if(RM()){document.documentElement.classList.add('rm');setTimeout(()=>{const b=$('#play');if(b)b.onclick=()=>{document.documentElement.classList.remove('rm');forced=true;initStage();$('#story').scrollIntoView()}});
  $('#flat').innerHTML='<li class="play"><div><p>Your device asks for reduced motion, so this is the still version.</p><button type="button" class="btn pri" id="play" style="margin-top:14px">Play the animated version</button></div></li>'+T.map((m,i)=>`<li><div class="fp" style="--bg:${bg(i)}">${pic(m)}</div><div><span class="eyebrow">${esc(m.role)}</span><h3>${esc(m.name)}</h3><p>${esc(m.bio)}</p><ul class="soc">${soc(m)}</ul></div></li>`).join('')}
 let forced=false;function initStage(){document.documentElement.classList.add('live');
 tilesEl.innerHTML=T.map((m,i)=>`<button type="button" class="tile" style="--bg:${bg(i)}" aria-label="${esc(m.name)}, ${esc(m.role)}">${pic(m)}</button>`).join('');
 const tiles=[...tilesEl.children],arc=$('#arc'),key=$('#key'),cap=$('#cap');
 const L=(a,b,t)=>a+(b-a)*t,C=(x,a=0,b=1)=>Math.min(b,Math.max(a,x)),E=t=>t<.5?4*t*t*t:1-Math.pow(-2*t+2,3)/2;
 const rnd=(i,k)=>{const x=Math.sin(i*127.1+k*311.7)*43758.5453;return x-Math.floor(x)};
 const CAPS=[['01 · Introduction','Seven people.'],['02 · Transformation','One gateway.'],['03 · Meet the team','Every stone holds the arch.']];
 let W,H,mob,R,tw,th,phase=-1,act=-2;
 const size=()=>{W=innerWidth;H=innerHeight;mob=W<900;R=mob?Math.min(W*.36,H*.27):Math.min(W*.25,H*.46);tw=R*(mob?.38:.32);th=tw*1.3;tiles.forEach(t=>{t.style.width=tw+'px';t.style.height=th+'px';t.style.setProperty('--tw',tw+'px')})};
 const show=i=>{const m=T[i];$('#irole').textContent=m.role;$('#iname').textContent=m.name;$('#ibio').textContent=m.bio;$('#isoc').innerHTML=soc(m);$('#pos').textContent=String(i+1).padStart(2,'0')+' / '+String(n).padStart(2,'0');info.classList.remove('sw');void info.offsetWidth;info.classList.add('sw')};
 function frame(t){
  const s=-story.getBoundingClientRect().top/H,g=C((s-.9)/1.3),ex=E(C((s-2.1)/.5)),e=(s-2.5)/.8;
  const cx=L(W/2,mob?W/2:W*.64,ex),cy=mob?H*L(.55,.46,ex):H*.84;
  arc.setAttribute('d',`M${cx-R} ${cy}A${R} ${R} 0 0 1 ${cx+R} ${cy}`);const len=Math.PI*R,ga=E(C((g-.25)/.75));
  arc.style.strokeDasharray=len;arc.style.strokeDashoffset=len*(1-ga);arc.style.opacity=.55*C(ga*3);
  key.setAttribute('cx',cx);key.setAttribute('cy',cy-R);key.style.opacity=C((ga-.9)*10);
  tiles.forEach((el,i)=>{
   const gi=E(C(g*1.7-i*.1)),th0=Math.PI-i*Math.PI/(n-1),d=.65+.7*rnd(i,4);
   const sx=W*(.1+.8*rnd(i,1))+Math.sin(t/1700+i*1.9)*16*(1-gi),sy=H*(.2+.5*rnd(i,2))+Math.cos(t/2100+i*2.3)*12*(1-gi);
   let x=L(sx,cx+R*Math.cos(th0),gi),y=L(sy,cy-R*Math.sin(th0),gi),r=L((rnd(i,3)-.5)*50,90-th0*180/Math.PI,gi),sc=L(d,1,gi);
   const a=E(C(1.4-Math.abs(e-i)*1.8)),pw=R*(mob?.9:.6);
   x=L(x,cx,a);y=L(y,cy-R*(mob?.5:.38),a);r=L(r,0,a);sc=L(sc,pw/tw,a);
   const b=(1-gi)*(1.15-d)*6;
   el.style.transform=`translate3d(${x-tw/2}px,${y-th/2}px,0) rotate(${r}deg) scale(${sc})`;
   el.style.zIndex=a>0?50:Math.round(sc*10);el.style.filter=b>.2?`blur(${b.toFixed(1)}px)`:'none';el.classList.toggle('on',a>.6)});
  const ph=s<1.2?0:s<2.4?1:2;if(ph!==phase){phase=ph;$('#capk').textContent=CAPS[ph][0];$('#capt').textContent=CAPS[ph][1];cap.classList.remove('sw');void cap.offsetWidth;cap.classList.add('sw')}
  cap.style.opacity=mob&&ph===2?0:1;
  const na=e>-.5?C(Math.round(e),0,n-1):-1;if(na!==act){act=na;if(na>=0)show(na);tiles.forEach((el,i)=>el.setAttribute('aria-pressed',i===na))}
  const io=C((e+.4)*3);info.style.opacity=io;info.style.pointerEvents=io>.5?'auto':'none';{const tp=cy+R*.12+tw*.4;info.style.top=mob?tp+'px':'';info.style.maxHeight=mob?(H-tp-10)+'px':''}
  if(vis)requestAnimationFrame(frame)}
 const go=i=>{const top=scrollY+story.getBoundingClientRect().top;scrollTo({top:top+(2.5+.8*i)*H,behavior:RM()&&!forced?'auto':'smooth'})};
 tiles.forEach((el,i)=>{el.onclick=()=>go(i);el.onkeydown=ev=>{const d=ev.key==='ArrowRight'?1:ev.key==='ArrowLeft'?-1:0;if(d){ev.preventDefault();tiles[(i+d+n)%n].focus()}}});
 $('#prev').onclick=()=>go((Math.max(act,0)-1+n)%n);$('#next').onclick=()=>go((act+1)%n);
 let vis=false;size();addEventListener('resize',size);
 new IntersectionObserver(es=>{const v=es[0].isIntersecting;if(v&&!vis){vis=true;requestAnimationFrame(frame)}vis=v}).observe(story)}
 if(!RM())initStage();
 /* links back to the homepage and shared targets */
 const toast=m=>{const t=$('#toast');t.textContent=m;t.classList.add('show');clearTimeout(toast.h);toast.h=setTimeout(()=>t.classList.remove('show'),3200)};
 $$('[data-home]').forEach(a=>a.href=CONFIG.home+a.dataset.home);
 document.addEventListener('click',e=>{const a=e.target.closest('[data-link]');if(!a)return;e.preventDefault();const u=CONFIG.links[a.dataset.link];if(u)location.href=u;else toast('The '+a.dataset.link+' link is not connected yet (CONFIG.links.'+a.dataset.link+').')});
})();
