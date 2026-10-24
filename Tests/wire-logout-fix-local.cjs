const fs=require('fs');
const p='University-Internship-Management-System/Coordinator/Assign_Supervisors.aspx';
let s=fs.readFileSync(p,'utf8');
if(!s.includes('src="../Js_project/firebase-client.js'))s=s.replace('<script src="../Js_project/workflow-dialogs.js">','<script src="../Js_project/firebase-client.js"></script><script src="../Js_project/workflow-dialogs.js">');
fs.writeFileSync(p,s);
