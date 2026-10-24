const fs=require('fs'),path=require('path'),crypto=require('crypto');
async function main(){
 const folder=path.join(process.env.LOCALAPPDATA,'UIMS-Backups');
 const before=JSON.parse(fs.readFileSync(path.join(folder,'before-training-reset-2026-10-23T14-07-21-034Z.json')));
 const removed=JSON.parse(fs.readFileSync(path.join(folder,'training-reset-patch-2026-10-23T14-07-21-034Z.json')));
 const xml=fs.readFileSync('University-Internship-Management-System/Web.config','utf8'),setting=k=>xml.match(new RegExp('<add\\s+key="'+k+'"\\s+value="([^"]*)"'))?.[1];
 const c=JSON.parse(fs.readFileSync(process.env.GOOGLE_APPLICATION_CREDENTIALS,'utf8'));if(c.project_id!==setting('FirebaseProjectId'))throw Error('Project mismatch');
 const b=x=>Buffer.from(JSON.stringify(x)).toString('base64url'),now=Math.floor(Date.now()/1000);
 const input=b({alg:'RS256',typ:'JWT'})+'.'+b({iss:c.client_email,scope:'https://www.googleapis.com/auth/firebase.database https://www.googleapis.com/auth/userinfo.email',aud:'https://oauth2.googleapis.com/token',iat:now,exp:now+3600});
 const tr=await fetch('https://oauth2.googleapis.com/token',{method:'POST',body:new URLSearchParams({grant_type:'urn:ietf:params:oauth:grant-type:jwt-bearer',assertion:input+'.'+crypto.sign('RSA-SHA256',Buffer.from(input),c.private_key).toString('base64url')})});if(!tr.ok)throw Error('Credential exchange '+tr.status);
 const token=(await tr.json()).access_token,base=setting('FirebaseDatabaseUrl').replace(/\/$/,''),headers={Authorization:'Bearer '+token,'Content-Type':'application/json'};
 async function read(){const r=await fetch(base+'/.json',{headers});if(!r.ok)throw Error('Read '+r.status);return r.json();}
 function canonical(v){if(Array.isArray(v))return v.map(canonical);if(v&&typeof v==='object')return Object.fromEntries(Object.keys(v).sort().map(k=>[k,canonical(v[k])]));return v;}
 const current=await read(),patch={},conflicts=[];const at=(o,p)=>p.split('/').reduce((v,k)=>v?.[k],o),eq=(a,b)=>JSON.stringify(canonical(a))===JSON.stringify(canonical(b));
 function missing(p,old,live){if(live==null){patch[p]=old;return;}if(eq(old,live))return;if(old&&live&&typeof old==='object'&&typeof live==='object'&&!Array.isArray(old)&&!Array.isArray(live)){for(const [k,v]of Object.entries(old))missing(p+'/'+k,v,live[k]);}else conflicts.push(p);}
 for(const [p,reset]of Object.entries(removed)){
 const old=at(before,p),live=at(current,p);if(old==null)continue;
 if(reset===null)missing(p,old,live);else if(eq(live,reset))patch[p]=old;else if(!eq(live,old))conflicts.push(p);
 }
 // Restore seat reservations only for applications restored by this operation.
 for(const [id,o]of Object.entries(before.opportunities||{}))for(const [app,v]of Object.entries(o.acceptedApplications||{}))if(before.applications?.[app]&&(current.applications?.[app]||patch['applications']?.[app]||patch['applications/'+app]))missing('opportunities/'+id+'/acceptedApplications/'+app,v,current.opportunities?.[id]?.acceptedApplications?.[app]);
 console.log('Project='+c.project_id+'; restore paths='+Object.keys(patch).length+'; conflicts='+conflicts.length);
 if(conflicts.length){console.log('Conflicting paths:',conflicts.join(', '));throw Error('No writes: current data conflicts with backup.');}
 for(const k of ['applications','placements','tasks','taskExams','taskExamAttempts','completionReviews'])console.log(k+': backup='+Object.keys(before[k]||{}).length+'; current='+Object.keys(current[k]||{}).length);
 if(!process.argv.includes('--apply'))return;
 const stamp=Date.now();fs.writeFileSync(path.join(folder,'before-restore-'+stamp+'.json'),JSON.stringify(current));fs.writeFileSync(path.join(folder,'restore-patch-'+stamp+'.json'),JSON.stringify(patch));
 if(!eq(current,await read()))throw Error('Database changed; no write performed.');
 const r=await fetch(base+'/.json',{method:'PATCH',headers,body:JSON.stringify(patch)});if(!r.ok)throw Error('Restore response '+r.status+'; check persisted state before retry.');
 const after=await read();for(const [p,v]of Object.entries(patch))if(!eq(at(after,p),v))throw Error('Verification failed '+p);
 const expected=structuredClone(current);for(const [p,v]of Object.entries(patch)){const keys=p.split('/'),last=keys.pop();let obj=expected;for(const k of keys)obj=obj[k]??={};obj[last]=v;}
 if(!eq(expected,after))throw Error('Unexpected/concurrent data change detected after restore; backups retained.');
 console.log('VERIFIED restoration persisted; only planned training paths changed. Account profiles retained with restored training status. Storage/Auth untouched.');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
