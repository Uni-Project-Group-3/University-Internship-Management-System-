using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static JObject BoardTask(string id, string placementId = "PLACE-1", string studentId = "ST-1", string status = "Submitted")
    {
        return new JObject { ["id"] = id, ["title"] = id, ["description"] = "offline fixture", ["placementId"] = placementId,
            ["studentId"] = studentId, ["assignedBy"] = "SUP-1", ["status"] = status, ["submissions"] = new JArray() };
    }

    private static async Task TaskBoardTests()
    {
        var settings = EnabledTaskSettings();
        var company = UIMS.Tasks.TaskActor.FromVerifiedProfile("auth-company", "companySupervisor",
            JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\"}"));
        var student = UIMS.Tasks.TaskActor.FromVerifiedProfile("auth-student", "student",
            JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-1\"}"));
        var db = TaskDbWithPlacement();
        db.Records["tasks/TASK-A"] = BoardTask("TASK-A", status: "Submitted");
        db.Records["tasks/TASK-B"] = BoardTask("TASK-B", status: "Completed");
        db.Records["tasks/TASK-C"] = BoardTask("TASK-C", status: "In Progress");

        var loaded = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "GET", "board",
            new JObject { ["placementId"] = "PLACE-1" }, CancellationToken.None);
        Assert((string)loaded["placementId"] == "PLACE-1" && (bool)loaded["canMove"]);
        Assert(((JArray)loaded["columns"]).Count == 4 && ((JArray)loaded["tasks"]).Count == 3);
        Assert(((JArray)loaded["tasks"]).All(x => (string)x["columnId"] == "todo")); // Legacy records default to To do.
        Assert(!(bool)(await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "GET", "board",
            new JObject { ["placementId"] = "PLACE-1" }, CancellationToken.None))["canMove"]);

        var moved = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "move",
            new JObject { ["placementId"] = "PLACE-1", ["taskId"] = "TASK-A", ["columnId"] = "inProgress", ["beforeTaskId"] = JValue.CreateNull(), ["operationId"] = "op-move-a" }, CancellationToken.None);
        Assert((bool)moved["success"] && !(bool)moved["duplicate"]);
        Assert((string)((JObject)db.Records["tasks/TASK-A"])["status"] == "Submitted"); // Board movement never changes lifecycle status.
        Assert((string)((JObject)db.Records["taskBoards/PLACE-1"])["positions"]["TASK-A"]["columnId"] == "inProgress");

        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "move",
            new JObject { ["placementId"] = "PLACE-1", ["taskId"] = "TASK-B", ["columnId"] = "inProgress", ["beforeTaskId"] = "TASK-A", ["operationId"] = "op-move-b" }, CancellationToken.None);
        var board = (JObject)db.Records["taskBoards/PLACE-1"];
        Assert((int)board["positions"]["TASK-B"]["order"] < (int)board["positions"]["TASK-A"]["order"]);
        await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "move",
            new JObject { ["placementId"] = "PLACE-1", ["taskId"] = "TASK-A", ["columnId"] = "inProgress", ["beforeTaskId"] = "TASK-B", ["operationId"] = "op-reorder-a" }, CancellationToken.None);
        board = (JObject)db.Records["taskBoards/PLACE-1"];
        Assert((int)board["positions"]["TASK-A"]["order"] < (int)board["positions"]["TASK-B"]["order"]);

        int savedVersion = db.Requests;
        var retry = await UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "move",
            new JObject { ["placementId"] = "PLACE-1", ["taskId"] = "TASK-A", ["columnId"] = "inProgress", ["beforeTaskId"] = "TASK-B", ["operationId"] = "op-reorder-a" }, CancellationToken.None);
        Assert((bool)retry["duplicate"] && db.Requests == savedVersion + 3); // Placement + task + board receipt reads; no board write.
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, company, settings).MoveBoardTaskAsync("PLACE-1", "TASK-A", "todo", null, "op-reorder-a", CancellationToken.None));
        Assert((string)((JObject)db.Records["tasks/TASK-B"])["status"] == "Completed");

        await Reject<UIMS.Services.FirebaseException>(() => UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, company, settings), "POST", "move",
            new JObject { ["placementId"] = "PLACE-1", ["taskId"] = "TASK-A", ["columnId"] = "not-a-column", ["operationId"] = "op-invalid" }, CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => UIMS.Tasks.TasksApiHandler.DispatchAsync(OfflineTaskService(db, student, settings), "POST", "move",
            new JObject { ["placementId"] = "PLACE-1", ["taskId"] = "TASK-A", ["columnId"] = "todo", ["operationId"] = "op-student" }, CancellationToken.None));
        var otherCompany = UIMS.Tasks.TaskActor.FromVerifiedProfile("auth-company-2", "companySupervisor",
            JObject.Parse("{\"role\":\"companySupervisor\",\"isActive\":true,\"companySupervisorId\":\"SUP-2\",\"companyId\":\"CO-2\"}"));
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, otherCompany, settings).LoadBoardAsync("PLACE-1", CancellationToken.None));
        var otherStudent = UIMS.Tasks.TaskActor.FromVerifiedProfile("auth-student-2", "student",
            JObject.Parse("{\"role\":\"student\",\"isActive\":true,\"studentId\":\"ST-2\"}"));
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, otherStudent, settings).LoadBoardAsync("PLACE-1", CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, company, settings).MoveBoardTaskAsync("PLACE-1", "TASK-FOREIGN", "todo", null, "op-foreign", CancellationToken.None));
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, company, settings).MoveBoardTaskAsync("PLACE-1", "TASK-A", "inProgress", "TASK-A", "op-self", CancellationToken.None));

        var collision = new TaskBoardRaceHandler();
        var raceA = new UIMS.Tasks.TaskService(key => settings.ContainsKey(key) ? settings[key] : null, new HttpClient(collision), company, token => Task.FromResult("isolated-test-token"));
        var raceB = new UIMS.Tasks.TaskService(key => settings.ContainsKey(key) ? settings[key] : null, new HttpClient(collision), company, token => Task.FromResult("isolated-test-token"));
        var races = await Task.WhenAll(BoardMoveStatus(raceA, "op-race-a"), BoardMoveStatus(raceB, "op-race-b"));
        Assert(races.Count(x => x == 200) == 1 && races.Count(x => x == 409) == 1 && collision.BoardWrites == 1);
        Assert((string)collision.Task["status"] == "Submitted");
        var positionsBefore = ((JObject)db.Records["taskBoards/PLACE-1"])["positions"].DeepClone();
        await OfflineTaskService(db, company, settings).ReorderColumnsAsync("PLACE-1", new JArray("done", "review", "inProgress", "todo"), "column-reorder", CancellationToken.None);
        var reordered = await OfflineTaskService(db, company, settings).LoadBoardAsync("PLACE-1", CancellationToken.None);
        Assert((string)reordered["columns"][0]["id"] == "done");
        Assert((string)reordered["tasks"].First(t => (string)t["id"] == "TASK-C")["columnId"] == "todo");
        Assert(JToken.DeepEquals(positionsBefore["TASK-A"], db.Records["taskBoards/PLACE-1"]["positions"]["TASK-A"]));
        Assert((bool)(await OfflineTaskService(db, company, settings).ReorderColumnsAsync("PLACE-1", new JArray("done", "review", "inProgress", "todo"), "column-reorder", CancellationToken.None))["duplicate"]);
        await Reject<UIMS.Services.FirebaseException>(() => OfflineTaskService(db, student, settings).ReorderColumnsAsync("PLACE-1", new JArray("done", "review", "inProgress", "todo"), "denied-columns", CancellationToken.None));
        Assert(db.AllRequestsAuthenticated);
    }

    private static async Task<int> BoardMoveStatus(UIMS.Tasks.TaskService service, string operationId)
    {
        try { await service.MoveBoardTaskAsync("PLACE-1", "TASK-RACE", "review", null, operationId, CancellationToken.None); return 200; }
        catch (UIMS.Services.FirebaseException error) { return error.StatusCode; }
    }

    private sealed class TaskBoardRaceHandler : HttpMessageHandler
    {
        private readonly object sync = new object();
        private readonly TaskCompletionSource<bool> bothBoardReads = new TaskCompletionSource<bool>();
        private int boardReads;
        private int boardVersion;
        public int BoardWrites;
        public JObject Task = BoardTask("TASK-RACE");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            string path = request.RequestUri.AbsolutePath.Trim('/');
            if (path.EndsWith(".json", StringComparison.Ordinal)) path = path.Substring(0, path.Length - 5);
            if (request.Method == HttpMethod.Get)
            {
                if (path == "taskBoards/PLACE-1")
                {
                    int version; JObject snapshot;
                    lock (sync) { version = boardVersion; snapshot = new JObject(); }
                    if (Interlocked.Increment(ref boardReads) == 2) bothBoardReads.TrySetResult(true);
                    await bothBoardReads.Task.ConfigureAwait(false);
                    return Versioned(snapshot, "\"b" + version + "\"");
                }
                if (path == "placements/PLACE-1") return Versioned(JObject.Parse("{\"studentId\":\"ST-1\",\"companySupervisorId\":\"SUP-1\",\"companyId\":\"CO-1\",\"status\":\"confirmed\"}"), "\"p\"");
                if (path == "tasks/TASK-RACE") return Versioned((JObject)Task.DeepClone(), "\"t\"");
                if (path == "tasks") return Versioned(new JObject { ["TASK-RACE"] = Task.DeepClone() }, "\"all\"");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            if (request.Method == HttpMethod.Put && path == "taskBoards/PLACE-1")
            {
                var expected = request.Headers.Contains("if-match") ? request.Headers.GetValues("if-match").FirstOrDefault() : null;
                var body = JObject.Parse(await request.Content.ReadAsStringAsync().ConfigureAwait(false));
                lock (sync)
                {
                    if (expected != "\"b" + boardVersion + "\"") return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);
                    boardVersion++; BoardWrites++;
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToString(Formatting.None)) };
                }
            }
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }

        private static HttpResponseMessage Versioned(JToken data, string etag)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(data.ToString(Formatting.None)) };
            response.Headers.TryAddWithoutValidation("ETag", etag);
            return response;
        }
    }
}
