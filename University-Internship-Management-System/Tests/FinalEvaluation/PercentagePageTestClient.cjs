const assert=require('node:assert/strict');
const routes={company:'/Company/ReportsFinalEvaluation',academic:'/Academic/FinalAcademicEvaluation',coordinator:'/Coordinator/FinalCompletionReview',student:'/Student/MyProgressEvaluation'};
const decode=s=>s.replace(/&quot;/g,'"').replace(/&#39;|&apos;/g,"'").replace(/&lt;/g,'<').replace(/&gt;/g,'>').replace(/&amp;/g,'&');
const attrs=s=>Object.fromEntries([...s.matchAll(/([\w:-]+)="([^"]*)"/g)].map(m=>[m[1],decode(m[2])]));
function form(html){const data=new URLSearchParams();for(const m of html.matchAll(/<input\b[^>]*>/gi)){const a=attrs(m[0]);if(a.name&&!['submit','button'].includes(a.type)&&(a.type!=='checkbox'||/checked=/.test(m[0])))data.set(a.name,a.value||'');}for(const m of html.matchAll(/<textarea\b([^>]*)>([\s\S]*?)<\/textarea>/gi)){const a=attrs(m[1]);data.set(a.name,decode(m[2]).replace(/^\r?\n/,''));}return data;}
class Client{
 constructor(origin){this.origin=new URL(origin||'http://localhost:61873');assert(['localhost','127.0.0.1'].includes(this.origin.hostname));this.cookie='';this.targets={};}
 async page(role,data,query,expected=200){const url=query!==undefined?new URL(routes[role]+query,this.origin):this.targets[role]||new URL(routes[role],this.origin);const r=await fetch(url,{method:data?'POST':'GET',headers:{Cookie:this.cookie,'Content-Type':'application/x-www-form-urlencoded'},body:data?.toString()});this.targets[role]=new URL(r.url);if(r.headers.getSetCookie().length){const jar=new Map(this.cookie.split('; ').filter(Boolean).map(c=>[c.slice(0,c.indexOf('=')),c.slice(c.indexOf('=')+1)]));for(const c of r.headers.getSetCookie()){const pair=c.split(';')[0];jar.set(pair.slice(0,pair.indexOf('=')),pair.slice(pair.indexOf('=')+1));}this.cookie=[...jar].map(([k,v])=>k+'='+v).join('; ');}const html=await r.text();assert.equal(r.status,expected,html.replace(/<[^>]+>/g,' ').slice(0,1000));return html;}
 async open(role,html,action='openEvaluation',id='dev-company-internship',expected=200){const d=form(html);d.set(action,id);return this.page(role,d,undefined,expected);}
 async click(role,html,button,fields={}){const d=form(html);d.set(button,button);for(const[k,v]of Object.entries(fields))d.set(k,v);return this.page(role,d);}
 async submitted(){let c=await this.page('company');c=await this.open('company',c);c=await this.click('company',c,'SaveDraft',{FinalPercentage:'80',FinalComments:'Fictional submitted company comments'});return this.click('company',c,'ConfirmSubmission',{ConfirmAction:'on'});}
 async ready(){await this.submitted();let a=await this.page('academic');a=await this.open('academic',a);await this.click('academic',a,'ApproveReview',{ConfirmAction:'on'});return this.page('coordinator');}
}
const value=(h,id)=>form(h).get(id)||(id==='FinalPercentage' ? h.match(/id="FinalPercentageDisplay"[^>]*>([0-9.]+)%/)?.[1] : '')||'';
module.exports={assert,Client,form,value};
