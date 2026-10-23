'use strict';
const fs=require('fs'),path=require('path'),{spawnSync}=require('child_process');
const root=path.resolve(process.argv[2]||'University-Internship-Management-System');let checked=0,failed=0;
function scan(dir){for(const entry of fs.readdirSync(dir,{withFileTypes:true})){if(entry.isDirectory()){if(!['bin','obj','node_modules','.git','packages'].includes(entry.name))scan(path.join(dir,entry.name));}else if(entry.name.endsWith('.js')){const file=path.join(dir,entry.name),r=spawnSync(process.execPath,['--check',file],{encoding:'utf8',timeout:30000});checked++;if(r.status!==0){failed++;console.log('FAIL '+path.relative(root,file));console.log(r.stderr||r.error?.message||'Syntax check failed.');}}}}
scan(root);console.log(`${failed?'FAIL':'PASS'} JavaScript syntax: ${checked} files checked; ${failed} failed.`);process.exitCode=failed?1:0;
