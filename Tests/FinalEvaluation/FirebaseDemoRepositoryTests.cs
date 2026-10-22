using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private sealed class DemoTransport
    {
        internal readonly string Id = Guid.NewGuid().ToString("N");
        internal JObject Data;
        internal int Writes, Deletes, Revision;
        internal bool Deny, LoseAcknowledgment;
        internal FirebaseDevelopmentDemoRepository Repository()
        {
            var h = new Handler { Respond = request => {
                Assert(request.RequestUri.AbsoluteUri == OfflineFirebaseRoot + FirebaseDevelopmentDemoRepository.Namespace + "/sessions/" + Id + ".json");
                if (Deny) return new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("not exposed") };
                var tag = "\"" + Revision + "\"";
                if (request.Method != HttpMethod.Get) {
                    Assert(request.Headers.GetValues("if-match").Single() == tag);
                    if (request.Method == HttpMethod.Delete) { Deletes++; Data = null; }
                    else { Writes++; Data = JsonConvert.DeserializeObject<JObject>(request.Content.ReadAsStringAsync().GetAwaiter().GetResult(), new JsonSerializerSettings { DateParseHandling = DateParseHandling.None }); Prune(Data); }
                    Revision++;
                    if (LoseAcknowledgment) return Response("null", null);
                }
                return Response(Data == null ? "null" : Data.ToString(), "\"" + Revision + "\"");
            }};
            return new FirebaseDevelopmentDemoRepository(new HttpClient(h), new Token(), OfflineFirebaseConfiguration(), Id);
        }
        private static void Prune(JToken value)
        {
            if (value is JObject) foreach (var p in ((JObject)value).Properties().ToArray()) { Prune(p.Value); if (p.Value.Type == JTokenType.Null || p.Value is JContainer && !p.Value.HasValues) p.Remove(); }
            else if (value is JArray) foreach (var item in (JArray)value) Prune(item);
        }
    }
    private static async Task RunFirebaseDemoTests()
    {
        await Test("scoped demo seed survives RTDB empty/null normalization and is idempotent", async () => {
            var t = new DemoTransport(); var r = t.Repository(); await r.SeedAsync(); await r.SeedAsync();
            var s = await r.InspectAsync(); Assert(t.Writes == 1 && s.Value.Evaluations.Count == 3 && s.Value.Completions.Count == 3);
        });
        await Test("fresh repository rehydrates persisted fictional dataset", async () => {
            var t = new DemoTransport(); await t.Repository().SeedAsync(); var r = t.Repository();
            Assert((await r.GetPercentageAsync("demo-noor")).Value.Reviews.Single().Kind == AcademicReviewKind.Approve);
        });
        await Test("stale aggregate write cannot modify scoped dataset", async () => {
            var t = new DemoTransport(); var r = t.Repository(); await r.SeedAsync(); var s = await r.GetPercentageAsync("dev-company-internship");
            s.Value.Current.Comments = "fictional update"; await Reject<ConflictException>(() => r.SavePercentageAsync(s.Value, "stale")); Assert(t.Writes == 1);
        });
        await Test("wrong demo owner never overwritten by seed", async () => {
            var t = new DemoTransport(); var r = t.Repository(); await r.SeedAsync(); t.Data["owner"] = "other-owner";
            await Reject<RepositoryException>(() => r.SeedAsync()); Assert(t.Writes == 1);
        });
        await Test("permission failure reports no successful seed", async () => {
            var t = new DemoTransport { Deny = true }; await Reject<RepositoryException>(() => t.Repository().SeedAsync()); Assert(t.Writes == 0);
        });
        await Test("missing write acknowledgment is not success", async () => {
            var t = new DemoTransport { LoseAcknowledgment = true }; await Reject<RepositoryException>(() => t.Repository().SeedAsync());
        });
        await Test("reset deletes only exact owned root with current ETag (mock transport)", async () => {
            var t = new DemoTransport(); var r = t.Repository(); await r.SeedAsync();
            await Reject<ConflictException>(() => r.DeleteExactDatasetAsync("stale")); Assert(t.Deletes == 0);
            await r.DeleteExactDatasetAsync((await r.InspectAsync()).Version); Assert(t.Deletes == 1 && (await r.InspectAsync()).Value == null);
        });
        await Test("RTDB omitted empty approval blockers remain valid, missing decision kind denied", () => {
            var audit = JsonConvert.DeserializeObject<CompletionDecisionAudit>("{\"kind\":\"Approve\"}"); Assert(audit.ReadinessBlockers != null && audit.ReadinessBlockers.Length == 0);
            try { JsonConvert.DeserializeObject<CompletionDecisionAudit>("{}"); throw new Exception("Missing kind accepted"); } catch (JsonSerializationException) { }
            return Task.CompletedTask;
        });
        await Test("demo dataset identifier rejects traversal before HTTP", () => {
            try { new FirebaseDevelopmentDemoRepository(new HttpClient(), new Token(), new FirebaseFinalEvaluationConfiguration(), "../official"); throw new Exception("Accepted traversal"); } catch (ArgumentException) { }
            return Task.CompletedTask;
        });
    }
}
