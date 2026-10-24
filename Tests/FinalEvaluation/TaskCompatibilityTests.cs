using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using UIMS.Tasks;

internal static partial class Program
{
    private static JObject TaskFixture(string id, string status, JToken submissions, string student = "ST-1", string placement = "PLACE-1", string assignedBy = "SUP-1")
    {
        return new JObject
        {
            ["id"] = id, ["title"] = "Offline fixture", ["description"] = "Fixture only",
            ["studentId"] = student, ["placementId"] = placement, ["assignedBy"] = assignedBy,
            ["status"] = status, ["dueDate"] = "2026-12-01", ["maximumScore"] = 100,
            ["submissions"] = submissions == null ? JValue.CreateNull() : submissions.DeepClone()
        };
    }

    private static TaskDatabaseHandler TaskDbWithPlacement(string student = "ST-1", string supervisor = "SUP-1", string company = "CO-1")
    {
        var db = new TaskDatabaseHandler();
        db.Records["placements/PLACE-1"] = new JObject
        {
            ["studentId"] = student, ["companySupervisorId"] = supervisor,
            ["companyId"] = company, ["status"] = "confirmed"
        };
        return db;
    }

    private static async Task<int> StatusCode(Func<Task> operation)
    {
        try { await operation(); return 200; }
        catch (FirebaseException error) { return error.StatusCode; }
    }

    private static async Task TaskSubmissionCompatibilityTests()
    {
        await TaskBulkAndExtensionTests();
        await TaskCreationRecoveryTests();
        var settings = EnabledTaskSettings();
        var student = TaskActor.FromVerifiedProfile("auth-student-1", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var company = TaskActor.FromVerifiedProfile("auth-company-1", "companySupervisor", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\"}"));

        // Array history: retain every historical object and append the next numeric revision.
        var arrayDb = TaskDbWithPlacement();
        var arrayOriginal = JArray.Parse("[{\"revision\":1,\"description\":\"first\",\"unknownLegacyField\":\"keep\"},{\"description\":\"legacy without number\"}]");
        arrayDb.Records["tasks/array-task"] = TaskFixture("array-task", "Revision Needed", arrayOriginal);
        await OfflineTaskService(arrayDb, student, settings).SubmitAsync("array-task", "second work", "https://example.test/array", "", CancellationToken.None);
        var arraySaved = (JArray)((JObject)arrayDb.Records["tasks/array-task"])["submissions"];
        Assert(arraySaved.Count == 3 && (int)arraySaved[0]["revision"] == 1);
        Assert((string)arraySaved[0]["unknownLegacyField"] == "keep" && arraySaved[1]["revision"] == null);
        Assert((int)arraySaved[2]["revision"] == 3 && (string)((JObject)arrayDb.Records["tasks/array-task"])["status"] == "Submitted");

        // Keyed object history: keep the object representation, old keys and nested values.
        var objectDb = TaskDbWithPlacement();
        var objectOriginal = JObject.Parse("{\"1\":{\"revision\":1,\"description\":\"first\",\"legacy\":true},\"3\":{\"description\":\"third\"}}");
        objectDb.Records["tasks/object-task"] = TaskFixture("object-task", "Revision Needed", objectOriginal);
        await OfflineTaskService(objectDb, student, settings).SubmitAsync("object-task", "fourth work", "https://example.test/object", "", CancellationToken.None);
        var objectSaved = (JObject)((JObject)objectDb.Records["tasks/object-task"])["submissions"];
        Assert(objectSaved.Properties().Select(x => x.Name).SequenceEqual(new[] { "1", "3", "4" }));
        Assert((bool)objectSaved["1"]["legacy"] && (string)objectSaved["3"]["description"] == "third");
        Assert((int)objectSaved["4"]["revision"] == 4);
        var objectView = await OfflineTaskService(objectDb, student, settings).LoadAsync("student", CancellationToken.None);
        var visibleRevisions = ((JArray)objectView["submissions"]).Where(x => (string)x["taskId"] == "object-task")
            .Select(x => (int)x["revision"]).ToArray();
        Assert(visibleRevisions.SequenceEqual(new[] { 1, 3, 4 }));

        // Review accepts both supported representations and leaves submission history intact.
        var arrayReviewDb = TaskDbWithPlacement();
        arrayReviewDb.Records["tasks/review-array"] = TaskFixture("review-array", "Submitted", arrayOriginal);
        await OfflineTaskService(arrayReviewDb, company, settings).ReviewAsync("review-array", "grade", 91, "Good", CancellationToken.None);
        Assert(((JArray)((JObject)arrayReviewDb.Records["tasks/review-array"])["submissions"]).Count == 2);
        Assert((string)((JObject)arrayReviewDb.Records["tasks/review-array"])["status"] == "Completed");
        var objectReviewDb = TaskDbWithPlacement();
        objectReviewDb.Records["tasks/review-object"] = TaskFixture("review-object", "Submitted", objectOriginal);
        await OfflineTaskService(objectReviewDb, company, settings).ReviewAsync("review-object", "revision", 0, "Please revise", CancellationToken.None, DateTimeOffset.UtcNow.AddDays(1).ToString("o"));
        Assert(((JObject)((JObject)objectReviewDb.Records["tasks/review-object"])["submissions"]).Count == 2);
        Assert((string)((JObject)objectReviewDb.Records["tasks/review-object"])["status"] == "Revision Needed");

        // Malformed and duplicate history fails closed before a write.
        var malformedDb = TaskDbWithPlacement();
        malformedDb.Records["tasks/malformed"] = TaskFixture("malformed", "Revision Needed", JValue.CreateString("not a history"));
        var malformedBefore = malformedDb.Records["tasks/malformed"].DeepClone();
        Assert(await StatusCode(() => OfflineTaskService(malformedDb, student, settings).SubmitAsync("malformed", "work", "https://example.test", "", CancellationToken.None)) == 409);
        Assert(JToken.DeepEquals(malformedBefore, malformedDb.Records["tasks/malformed"]));
        var duplicateArrayDb = TaskDbWithPlacement();
        duplicateArrayDb.Records["tasks/duplicate-array"] = TaskFixture("duplicate-array", "Submitted", JArray.Parse("[{\"revision\":1},{\"revision\":1}]"));
        var duplicateArrayBefore = duplicateArrayDb.Records["tasks/duplicate-array"].DeepClone();
        Assert(await StatusCode(() => OfflineTaskService(duplicateArrayDb, company, settings).ReviewAsync("duplicate-array", "grade", 50, "", CancellationToken.None)) == 409);
        Assert(JToken.DeepEquals(duplicateArrayBefore, duplicateArrayDb.Records["tasks/duplicate-array"]));
        var duplicateObjectDb = TaskDbWithPlacement();
        duplicateObjectDb.Records["tasks/duplicate-object"] = TaskFixture("duplicate-object", "Revision Needed", JObject.Parse("{\"1\":{\"revision\":1},\"01\":{\"revision\":1}}"));
        var duplicateObjectBefore = duplicateObjectDb.Records["tasks/duplicate-object"].DeepClone();
        Assert(await StatusCode(() => OfflineTaskService(duplicateObjectDb, student, settings).SubmitAsync("duplicate-object", "work", "https://example.test", "", CancellationToken.None)) == 409);
        Assert(JToken.DeepEquals(duplicateObjectBefore, duplicateObjectDb.Records["tasks/duplicate-object"]));

        // Read/list filtering validates placement ownership and explicit task/student consistency.
        var accessDb = TaskDbWithPlacement();
        accessDb.Records["placements/PLACE-OTHER"] = new JObject { ["studentId"] = "ST-2", ["companySupervisorId"] = "SUP-2", ["companyId"] = "CO-2", ["status"] = "confirmed" };
        accessDb.Records["tasks/foreign-student"] = TaskFixture("foreign-student", "Assigned", new JArray(), "ST-1", "PLACE-OTHER", "SUP-1");
        accessDb.Records["tasks/foreign-company"] = TaskFixture("foreign-company", "Assigned", new JArray(), "ST-2", "PLACE-OTHER", "SUP-1");
        var studentView = await OfflineTaskService(accessDb, student, settings).LoadAsync("student", CancellationToken.None);
        var companyView = await OfflineTaskService(accessDb, company, settings).LoadAsync("companySupervisor", CancellationToken.None);
        Assert(!((JArray)studentView["tasks"]).Any(x => (string)x["id"] == "foreign-student"));
        Assert(!((JArray)companyView["tasks"]).Any(x => (string)x["id"] == "foreign-company"));
        Assert(await StatusCode(() => OfflineTaskService(accessDb, company, settings).ReviewAsync("foreign-company", "grade", 50, "", CancellationToken.None)) == 403);

        // Two server instances read the same ETag. One conditional update must win; the other conflicts.
        var raceHandler = new TaskRaceHandler();
        var raceStudentA = TaskActor.FromVerifiedProfile("auth-student-1", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var raceStudentB = TaskActor.FromVerifiedProfile("auth-student-1", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var raceServiceA = new TaskService(key => settings.ContainsKey(key) ? settings[key] : null, new HttpClient(raceHandler), raceStudentA, token => Task.FromResult("isolated-test-token"));
        var raceServiceB = new TaskService(key => settings.ContainsKey(key) ? settings[key] : null, new HttpClient(raceHandler), raceStudentB, token => Task.FromResult("isolated-test-token"));
        var raceResults = await Task.WhenAll(
            StatusCode(() => raceServiceA.StartAsync("race-task", CancellationToken.None)),
            StatusCode(() => raceServiceB.StartAsync("race-task", CancellationToken.None)));
        Assert(raceResults.Count(x => x == 200) == 1 && raceResults.Count(x => x == 409) == 1);
        Assert((string)raceHandler.TaskRecord["status"] == "In Progress" && raceHandler.TaskWrites == 1);
    }

    private static async Task TaskCreationRecoveryTests()
    {
        var db = TaskDbWithPlacement(); var settings = EnabledTaskSettings();
        db.Records["users/one"] = JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}");
        var company = TaskActor.FromVerifiedProfile("company-uid", "companySupervisor", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\"}"));
        var student = TaskActor.FromVerifiedProfile("student-uid", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var service = OfflineTaskService(db, company, settings);
        string hash = new string('a',64);
        var payload = new JObject { ["batchId"]="RECOVERY-TEST",["placementIds"]=new JArray("PLACE-1"),["title"]="Recoverable task",["description"]="Requirements",["criteria"]="Instructions",["maximum"]=10,["taskType"]="standard",["startsAt"]=DateTimeOffset.UtcNow.ToString("o"),["deadline"]=DateTimeOffset.UtcNow.AddDays(1).ToString("o") };
        var plan = new JObject { ["payload"]=payload,["resources"]=new JArray(new JObject {["id"]="resource-0",["name"]="work.txt",["size"]=10,["sha256"]=hash}),["questionImages"]=new JArray() };
        var started = await service.BeginCreationAsync(plan,CancellationToken.None);
        Assert((string)started["status"]=="incomplete" && !(bool)started["progress"][0]["taskSaved"]);
        Assert(!db.Records.Keys.Any(key=>key.StartsWith("tasks/")));
        var restarted = OfflineTaskService(db,company,settings);
        Assert((await restarted.ListCreationOperationsAsync(CancellationToken.None))["operations"].Count()==1);
        Assert(JToken.DeepEquals(JToken.Parse(plan.ToString()),(await restarted.CreationOperationAsync("RECOVERY-TEST",false,CancellationToken.None))["plan"]));
        var duplicate = await restarted.BeginCreationAsync(plan,CancellationToken.None); Assert((string)duplicate["batchId"]=="RECOVERY-TEST");
        var changed = (JObject)plan.DeepClone();changed["payload"]["title"]="Changed";
        await Reject<FirebaseException>(()=>restarted.BeginCreationAsync(changed,CancellationToken.None));
        int before = db.Requests;
        await Reject<FirebaseException>(()=>OfflineTaskService(db,student,settings).CreationOperationAsync("RECOVERY-TEST",false,CancellationToken.None));
        await Reject<FirebaseException>(()=>OfflineTaskService(db,student,settings).ListCreationOperationsAsync(CancellationToken.None));
        Assert(db.Requests==before);
        var wrong = TaskActor.FromVerifiedProfile("other-company-uid","companySupervisor",JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-2\",\"companyId\":\"CO-2\"}"));
        await Reject<FirebaseException>(()=>OfflineTaskService(db,wrong,settings).CreationOperationAsync("RECOVERY-TEST",false,CancellationToken.None));
        var created = await restarted.BulkCreateAsync(payload,CancellationToken.None);string taskId=(string)created["results"][0]["id"];
        var partial = await restarted.CreationOperationAsync("RECOVERY-TEST",true,CancellationToken.None);
        Assert((bool)partial["progress"][0]["taskSaved"] && (string)partial["status"]=="incomplete");
        // A resource must be both ready and linked to the exact task, with the original fingerprint.
        db.Records["fileAttachments/file-recovery"] = new JObject {["taskId"]=taskId,["purpose"]="taskAttachment",["status"]="ready",["fileName"]="work.txt",["sizeBytes"]=10,["sha256"]=hash};
        Assert((string)(await restarted.CreationOperationAsync("RECOVERY-TEST",true,CancellationToken.None))["status"]=="incomplete");
        ((JObject)db.Records["tasks/"+taskId])["attachmentIds"]=new JArray("file-recovery");
        var finished = await OfflineTaskService(db,company,settings).CreationOperationAsync("RECOVERY-TEST",true,CancellationToken.None);
        Assert((string)finished["status"]=="complete" && (string)finished["progress"][0]["resourceFiles"]["resource-0"]=="file-recovery");
        Assert(!(await restarted.ListCreationOperationsAsync(CancellationToken.None))["operations"].HasValues);
        Assert(db.Records.Keys.Count(key=>key.StartsWith("tasks/"))==1);
        var examPayload = (JObject)payload.DeepClone();examPayload["batchId"]="RECOVERY-EXAM";examPayload["taskType"]="exam";
        var examPlan = new JObject {["payload"]=examPayload,["exam"]=ExamDraft(),["questionImages"]=new JArray(new JObject {["questionId"]="Q1",["name"]="question.png",["size"]=11,["sha256"]=hash})};
        await restarted.BeginCreationAsync(examPlan,CancellationToken.None);
        var examTask = await restarted.BulkCreateAsync(examPayload,CancellationToken.None);string examTaskId=(string)examTask["results"][0]["id"];
        var waitingExam = await restarted.CreationOperationAsync("RECOVERY-EXAM",false,CancellationToken.None);string examId=(string)waitingExam["progress"][0]["examId"];
        Assert((string)waitingExam["status"]=="incomplete");
        db.Records["taskExams/"+examId]=new JObject {["taskId"]=examTaskId,["placementId"]="PLACE-1",["createdBy"]="SUP-1",["status"]="draft",["draft"]=new JObject {["questions"]=new JObject {["Q1"]=new JObject()}}};
        db.Records["fileAttachments/image-recovery"]=new JObject {["taskId"]=examTaskId,["examId"]=examId,["questionId"]="Q1",["purpose"]="examQuestionImage",["status"]="ready",["sha256"]=hash,["sizeBytes"]=11};
        var uploadedImage = await restarted.CreationOperationAsync("RECOVERY-EXAM",true,CancellationToken.None);
        Assert((string)uploadedImage["progress"][0]["images"]["Q1"]=="image-recovery" && (string)uploadedImage["status"]=="incomplete");
        ((JObject)db.Records["taskExams/"+examId])["status"]="published";
        Assert((string)(await restarted.CreationOperationAsync("RECOVERY-EXAM",true,CancellationToken.None))["status"]=="incomplete");
        db.Records["taskExams/"+examId]["draft"]["questions"]["Q1"]["imageFileId"]="image-recovery";
        Assert((string)(await OfflineTaskService(db,company,settings).CreationOperationAsync("RECOVERY-EXAM",true,CancellationToken.None))["status"]=="complete");
        // Completion cannot be supplied by the caller: removing ready metadata makes reconciliation incomplete.
        ((JObject)db.Records["fileAttachments/file-recovery"])["status"]="pending";
        Assert((string)(await restarted.CreationOperationAsync("RECOVERY-TEST",false,CancellationToken.None))["status"]=="incomplete");
    }

    private static async Task TaskBulkAndExtensionTests()
    {
        var db = TaskDbWithPlacement(); var settings = EnabledTaskSettings();
        db.Records["placements/PLACE-2"] = JObject.Parse("{\"studentId\":\"ST-2\",\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\",\"status\":\"confirmed\"}");
        db.Records["users/one"] = JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}");
        db.Records["users/two"] = JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-2\"}");
        var company = TaskActor.FromVerifiedProfile("company-uid", "companySupervisor", JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\"}"));
        var student = TaskActor.FromVerifiedProfile("student-uid", "student", JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var service = OfflineTaskService(db, company, settings);
        var input = new JObject { ["batchId"] = "BATCH-TEST", ["placementIds"] = new JArray("PLACE-1", "PLACE-2"), ["title"] = "Independent tasks", ["description"] = "Requirements", ["criteria"] = "Instructions", ["deadline"] = DateTimeOffset.UtcNow.AddDays(1).ToString("o"), ["maximum"] = 20 };
        var created = await service.BulkCreateAsync(input, CancellationToken.None); Assert((bool)created["success"] && created["results"].Count() == 2);
        string first = (string)created["results"][0]["id"], second = (string)created["results"][1]["id"];
        Assert(first != second && (string)db.Records["tasks/" + first]["studentId"] == "ST-1" && (string)db.Records["tasks/" + second]["studentId"] == "ST-2");
        var retry = await OfflineTaskService(db, company, settings).BulkCreateAsync(input, CancellationToken.None); Assert((bool)retry["success"] && (string)retry["results"][0]["id"] == first && db.Records.Keys.Count(k => k.StartsWith("tasks/")) == 2);
        var changed = (JObject)input.DeepClone(); changed["title"] = "Changed retry"; Assert(!(bool)(await service.BulkCreateAsync(changed, CancellationToken.None))["success"]);
        var firstRow = (JObject)db.Records["tasks/" + first]; firstRow["status"] = "Submitted"; firstRow["submissions"] = new JArray(new JObject { ["revision"] = 1, ["description"] = "Original submission" }); firstRow["boardPosition"] = new JObject { ["columnId"] = "custom", ["order"] = 7 };
        var secondRow = (JObject)db.Records["tasks/" + second]; secondRow["score"] = 12; secondRow["status"] = "Completed";
        var secondBefore = secondRow.DeepClone();
        var edit = new JObject { ["taskId"] = first, ["all"] = true, ["title"] = "Updated instructions", ["description"] = "Revised", ["criteria"] = "Acceptance", ["deadline"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o"), ["resubmit"] = true };
        var edited = await service.EditAsync(edit, CancellationToken.None); Assert(edited["affected"].Count() == 1 && edited["excluded"].Count() == 1 && JToken.DeepEquals(secondBefore, db.Records["tasks/" + second]));
        firstRow = (JObject)db.Records["tasks/" + first]; Assert((string)firstRow["status"] == "Revision Needed" && (string)firstRow["submissions"][0]["description"] == "Original submission" && firstRow["instructionVersions"]["1"] != null && (int)firstRow["boardPosition"]["order"] == 7);
        firstRow["instructionVersions"] = new JArray(JValue.CreateNull(), new JObject { ["title"] = "Historical instructions", ["legacy"] = "preserve" });
        firstRow["instructionVersion"] = 2;
        var nextEdit = (JObject)edit.DeepClone(); nextEdit["all"] = false; nextEdit["resubmit"] = false;
        await service.EditAsync(nextEdit, CancellationToken.None);
        firstRow = (JObject)db.Records["tasks/" + first];
        Assert((string)firstRow["instructionVersions"]["1"]["legacy"] == "preserve" && firstRow["instructionVersions"]["2"] != null);
        firstRow["dueDate"] = DateTimeOffset.UtcNow.AddHours(-1).ToString("o");
        var studentService = OfflineTaskService(db, student, settings);
        Assert(await StatusCode(() => studentService.SubmitAsync(first, "Late", "https://example.test", "", CancellationToken.None)) == 409);
        var extension = new JObject { ["taskId"] = first, ["reason"] = "More time required", ["deadline"] = DateTimeOffset.UtcNow.AddDays(3).ToString("o") };
        await studentService.ExtensionAsync(extension, CancellationToken.None);
        Assert(await StatusCode(() => studentService.ExtensionAsync(extension, CancellationToken.None)) == 409);
        await service.ExtensionAsync(new JObject { ["taskId"] = first, ["approve"] = false, ["reason"] = "Final decision" }, CancellationToken.None);
        Assert(await StatusCode(() => studentService.ExtensionAsync(extension, CancellationToken.None)) == 409);
        Assert((string)db.Records["tasks/" + first]["extension"]["status"] == "rejected");
        Assert(JToken.DeepEquals(secondBefore, db.Records["tasks/" + second]));
        var overdue = TaskFixture("extension-approved", "In Progress", new JArray()); overdue["dueDate"] = DateTimeOffset.UtcNow.AddDays(-1).ToString("o"); db.Records["tasks/extension-approved"] = overdue;
        var approvalRequest = new JObject { ["taskId"] = "extension-approved", ["reason"] = "Awaiting required resource", ["deadline"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o") };
        await studentService.ExtensionAsync(approvalRequest, CancellationToken.None);
        await service.ExtensionAsync(new JObject { ["taskId"] = "extension-approved", ["approve"] = true, ["deadline"] = DateTimeOffset.UtcNow.AddDays(1).ToString("o") }, CancellationToken.None);
        await OfflineTaskService(db, student, settings).SubmitAsync("extension-approved", "Work after approval", "https://example.test/approved", "", CancellationToken.None);
        Assert((string)db.Records["tasks/extension-approved"]["status"] == "Submitted" && db.Records["tasks/extension-approved"]["extension"]["oldDeadline"] != null);
        var partial = (JObject)input.DeepClone(); partial["placementIds"] = new JArray("PLACE-1", "UNAUTHORIZED");
        var partialResult = await service.BulkCreateAsync(partial, CancellationToken.None); Assert(!(bool)partialResult["success"] && (bool)partialResult["results"][0]["reused"] && !(bool)partialResult["results"][1]["success"]);
        var beforeArchive = db.Records["tasks/" + first].DeepClone();
        await service.ArchiveAsync(first, CancellationToken.None);
        Assert(JToken.DeepEquals(beforeArchive["submissions"], db.Records["tasks/" + first]["submissions"]) && db.Records["tasks/" + first]["archivedAt"] != null);
        Assert((bool)(await service.ArchiveAsync(first, CancellationToken.None))["duplicate"]);
        Assert(await StatusCode(() => studentService.ArchiveAsync(first, CancellationToken.None)) == 403);
    }

    private sealed class TaskRaceHandler : HttpMessageHandler
    {
        private readonly object sync = new object();
        private readonly TaskCompletionSource<bool> bothReads = new TaskCompletionSource<bool>();
        private int taskReads;
        private int taskVersion;
        public int TaskWrites;
        public JObject TaskRecord = new JObject
        {
            ["id"] = "race-task", ["title"] = "Race fixture", ["studentId"] = "ST-1",
            ["placementId"] = "PLACE-1", ["assignedBy"] = "SUP-1", ["status"] = "Assigned",
            ["submissions"] = new JArray()
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            string path = request.RequestUri.AbsolutePath.Trim('/');
            if (path.EndsWith(".json", StringComparison.Ordinal)) path = path.Substring(0, path.Length - 5);
            if (request.Method == HttpMethod.Get)
            {
                JToken snapshot; int version;
                lock (sync)
                {
                    if (path == "tasks/race-task") { snapshot = TaskRecord.DeepClone(); version = taskVersion; }
                    else if (path == "placements/PLACE-1") { snapshot = JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"status\":\"confirmed\"}"); version = 0; }
                    else return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                if (path == "tasks/race-task" && Interlocked.Increment(ref taskReads) == 2) bothReads.TrySetResult(true);
                if (path == "tasks/race-task") await bothReads.Task.ConfigureAwait(false);
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(snapshot.ToString(Formatting.None)) };
                response.Headers.TryAddWithoutValidation("ETag", "\"v" + version + "\"");
                return response;
            }
            if (request.Method == HttpMethod.Put && path == "tasks/race-task")
            {
                string expected = request.Headers.Contains("if-match") ? request.Headers.GetValues("if-match").FirstOrDefault() : null;
                string body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                lock (sync)
                {
                    if (expected != "\"v" + taskVersion + "\"") return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
                    TaskRecord = JObject.Parse(body);
                    taskVersion++; TaskWrites++;
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }
    }
}
