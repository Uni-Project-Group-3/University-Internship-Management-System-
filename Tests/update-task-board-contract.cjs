const fs=require('fs');const p='Tests/FinalEvaluation/TaskBoardTests.cs';let s=fs.readFileSync(p,'utf8');
s=s.replace('BoardTask("TASK-B", status: "Completed")','BoardTask("TASK-B", status: "Submitted")');
s=s.replace('Assert(((JArray)loaded["tasks"]).All(x => (string)x["columnId"] == "todo")); // Legacy records default to To do.','Assert((string)loaded["tasks"].First(x => (string)x["id"] == "TASK-A")["columnId"] == "review");\n        Assert((string)loaded["tasks"].First(x => (string)x["id"] == "TASK-C")["columnId"] == "inProgress");\n        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, company, settings).MoveBoardTaskAsync("PLACE-1", "TASK-A", "done", null, "forbidden-completion", CancellationToken.None));');
s=s.replaceAll('["columnId"] = "inProgress"','["columnId"] = "review"').replace('== "inProgress");\r\n\r\n        await UIMS','== "review");\r\n\r\n        await UIMS');
s=s.replace(')["columnId"] == "inProgress");',')["columnId"] == "review");');
s=s.replace(')["status"] == "Completed");',')["status"] == "Submitted");');
s=s.replace('Assert((string)reordered["columns"][0]["id"] == "done");','Assert((string)reordered["columns"][0]["id"] == "todo"); // Rendering always keeps the four workflow columns fixed.');
s=s.replace('== "TASK-C")["columnId"] == "todo"','== "TASK-C")["columnId"] == "inProgress"');
fs.writeFileSync(p,s);
