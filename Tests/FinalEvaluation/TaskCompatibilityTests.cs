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
        await OfflineTaskService(objectReviewDb, company, settings).ReviewAsync("review-object", "revision", 0, "Please revise", CancellationToken.None);
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
