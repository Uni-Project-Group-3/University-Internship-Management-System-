using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using University_Internship_Management_System.Models.FinalEvaluation;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static partial class Program
{
    private static int passed;
    private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
    private static async Task Reject<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static async Task Test(string name, Func<Task> test) { await test(); passed++; Console.WriteLine("PASS " + name); }
    private static EvaluationPolicy Policy()
    {
        var criteria = new Dictionary<string, CriterionPolicy> { { "testCriterion", new CriterionPolicy { Minimum = 1, Maximum = 7, Required = true } } };
        return new EvaluationPolicy { PolicyId = "fictional-policy", Version = "test-v1", CompanyCriteria = criteria, AcademicCriteria = criteria };
    }
    private static CompanyFinalEvaluation Draft(string id = "fictional-internship")
    {
        return new CompanyFinalEvaluation { InternshipId = id, StudentId = "fictional-student", EvaluatorId = "fictional-evaluator", PolicyId = "fictional-policy", PolicyVersion = "test-v1", Scores = new Dictionary<string, decimal?> { { "testCriterion", 3 } } };
    }
    private sealed class User : IAuthenticatedUser { public string UserId { get; set; } = "fictional-evaluator"; }
    private sealed class Assignment : IInternshipAssignmentAuthorizer
    {
        public bool Allowed;
        public Task<bool> CanAccessAsync(string user, string internship, string student, string capability) { return Task.FromResult(Allowed); }
    }
    private sealed class Token : IAccessTokenProvider { public Task<string> GetAccessTokenAsync() { return Task.FromResult("local-test-token"); } }
    private sealed class Handler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { return Task.FromResult(Respond(request)); }
    }
    private static HttpResponseMessage Response(string json, string etag = "\"test-version\"")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        if (etag != null) response.Headers.TryAddWithoutValidation("ETag", etag);
        return response;
    }
    private static FirebaseFinalEvaluationRepository Rest(Handler handler)
    {
        return new FirebaseFinalEvaluationRepository(new HttpClient(handler), new Token(), new FirebaseFinalEvaluationConfiguration());
    }
    private static int Main()
    {
        try { Run().GetAwaiter().GetResult(); Console.WriteLine(passed + " tests passed. No network requests or Firebase writes were made."); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static async Task Run()
    {
        await RunFirebaseDemoTests();
        await RunPercentageRequirementsTests();
        await RunPercentageSecurityTests();
        await RunReadinessTests();
        await RunDecisionTests();
        await RunCompletionReportTests();
        await RunStudentCompletionTests();
    }
}
