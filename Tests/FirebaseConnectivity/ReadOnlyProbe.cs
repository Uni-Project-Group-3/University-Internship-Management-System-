using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using UIMS.Tasks;
using University_Internship_Management_System.Services.FinalEvaluation;

internal static class FirebaseConnectivityProbe
{
    private static int Main(string[] args) { try { Run(args).GetAwaiter().GetResult(); return 0; } catch (Exception e) { Console.WriteLine("FAIL " + e.GetType().Name + ": " + e.Message); return 1; } }
    private static void Assert(bool valid, string label) { if (!valid) throw new Exception(label); Console.WriteLine("PASS " + label); }
    private static async Task Run(string[] args)
    {
        if (args.Length == 0) { await Offline(); return; }
        foreach (string uid in args)
        {
            var context = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
            context.User = new GenericPrincipal(new GenericIdentity(uid, "Probe"), new string[0]);
            var data = await LiveFirebaseData.OpenAsync(context);
            var placements = data.Placements(); int evaluations = 0;
            foreach (var p in placements)
            {
                if ((await data.GetPercentageAsync(p.InternshipId)).Value != null) evaluations++;
                var hours = await ((IApprovedTrainingHoursReader)data).GetAsync(p.InternshipId);
                if (hours.Value.Any(h => h.Hours < 0)) throw new Exception("Invalid hours");
            }
            Console.WriteLine("PASS LIVE " + data.Role + ": " + placements.Length + " placements, " + evaluations + " evaluations");
            if (data.Role == "coordinator")
            {
                var workflow = new CoordinatorReadinessWorkflow(data, data, data, data, data, data, data, data);
                var decisions = new CompletionDecisionWorkflow(data, data, data, workflow, data);
                int count = 0; foreach (var p in placements) { await decisions.LoadAsync(p.InternshipId); count++; }
                Console.WriteLine("PASS LIVE coordinator readiness and completion records: " + count);
                var gateway = new University_Internship_Management_System.Shared.StudentFirebaseGateway();
                var directory = await new University_Internship_Management_System.Shared.AcademicSupervisorService(gateway, gateway).DirectoryAsync();
                Console.WriteLine("PASS LIVE academic supervisor directory: " + ((JArray)directory["supervisors"]).Count);
                continue;
            }
            context.User = new GenericPrincipal(new System.Web.Security.FormsIdentity(new System.Web.Security.FormsAuthenticationTicket(1, data.UserId, DateTime.Now, DateTime.Now.AddMinutes(10), false, data.Role)), new[] { data.Role });
            var actor = await TaskActorResolver.ResolveAsync(context, CancellationToken.None, true);
            if (data.Role == "student" || data.Role == "companySupervisor")
            {
                await new TaskService(actor).LoadAsync(data.Role, CancellationToken.None);
                Console.WriteLine("PASS LIVE " + data.Role + " tasks");
                await new TaskExamService(actor).ListAsync(CancellationToken.None);
                Console.WriteLine("PASS LIVE " + data.Role + " exams");
                var attendance = new UIMS.Attendance.AttendanceService();
                await attendance.Identity(data.Role == "student" ? "student" : "company", data.Role == "student" ? actor.StudentId : actor.CompanySupervisorId, data.UserId, false);
                Console.WriteLine("PASS LIVE " + data.Role + " attendance identity");
                if (data.Role == "student")
                {
                    var completion = new StudentCompletionWorkflow(data, data, data, data, data, data, data, data, data, null);
                    foreach (var p in placements) await completion.LoadAsync(p.InternshipId);
                    Console.WriteLine("PASS LIVE student progress and completion view");
                }
            }
            var reports = new WeeklyReportService(actor);
            if (data.Role == "student") await reports.ListStudentReportsAsync(CancellationToken.None);
            else if (data.Role == "companySupervisor") await reports.ListCompanyReportsAsync(CancellationToken.None);
            else await reports.ListAcademicReportsAsync(CancellationToken.None);
            Console.WriteLine("PASS LIVE " + data.Role + " weekly reports");
        }
        Console.WriteLine("Read-only probe completed. No write methods called.");
    }
    private static async Task Offline()
    {
        var root = JObject.Parse(@"{
          'users':{'student-owner':{'role':'student','studentId':'S1','isActive':true},'academic-owner':{'role':'academicSupervisor','isActive':true},'coordinator':{'role':'coordinator','isActive':true,'universityId':'U1'}},
          'placements':{'P1':{'id':'P1','studentId':'S1','studentUid':'student-owner','studentName':'Student','academicSupervisorId':'stale-academic','policyId':'POL','policyVersion':'v1','requiredHours':300,'status':'active'},'P2':{'id':'P2','studentId':'S2','studentUid':'other','policyId':'POL','policyVersion':'v1'}},
          'assignments':{'A1':{'studentId':'S1','academicSupervisorId':'academic-owner','active':true}},
          'tasks':{'T1':{'id':'T1','placementId':'P1','studentId':'S1','status':'Completed','score':90,'maximumScore':100,'feedback':'private'},'T2':{'id':'T2','placementId':'P2','studentId':'S2','status':'Completed'}},
          'uimsAttendance':{'trainingAssignments':{'A1':{'placementId':'P1','studentId':'S1'}},'attendance':{'A1':{'D1':{'assignmentId':'A1','attendanceDate':'2026-10-01','studentId':'S1','workedMinutes':480,'hoursApprovalStatus':'approved'},'D2':{'assignmentId':'A1','attendanceDate':'2026-10-02','studentId':'S1','workedMinutes':120,'hoursApprovalStatus':'pending'},'D3':{'assignmentId':'A1','attendanceDate':'2026-10-03','studentId':'S1','workedMinutes':300,'hoursApprovalStatus':'rejected'}}}}
        }");
        var data = new LiveFirebaseData(); typeof(LiveFirebaseData).GetField("root", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(data, root);
        Action<string> user = uid => typeof(LiveFirebaseData).GetProperty("UserId").SetValue(data, uid);
        user("student-owner");
        Assert(data.Placements().Length == 1 && data.Placements()[0].InternshipId == "P1", "student placement ownership excludes other students");
        Assert(!await data.IsStudentOwnerAsync("student-owner", "P2", null), "known foreign placement cannot bypass student ownership");
        var ledger = await ((IApprovedTrainingHoursReader)data).GetAsync("P1");
        Assert(ledger.Value.Where(x => x.IsApproved).Sum(x => x.Hours) == 8, "approved hours exclude pending and rejected entries");
        Assert((await data.GetPendingHoursAsync("P1")).Value == 2, "pending hours exclude rejected entries");
        var tasks = await ((IStudentVisibleTaskReader)data).GetAsync("P1");
        Assert(tasks.Value.Length == 1 && !tasks.Value[0].IsResultPublished, "student task source excludes foreign and unpublished grades");
        var reports = await ((IRequiredReportsReader)data).GetAsync("P1");
        Assert(!reports.IsTrusted && reports.Value.RequiredIds == null, "missing report policy blocks approval rather than inventing zero requirements");
        user("academic-owner"); Assert(data.Placements().Length == 1, "current assignment overrides stale placement supervisor");
        user("coordinator"); Assert(data.Placements().Length == 0, "coordinator scope fails closed when university evidence is missing");
        user("student-owner"); root["users"]["student-owner"]["studentId"] = ""; root["placements"]["P2"]["studentId"] = "";
        Assert(!await data.IsStudentOwnerAsync("student-owner", "P2", null), "empty student IDs never grant ownership");
        Console.WriteLine("9 connectivity adapter checks passed. No network or writes.");
    }
}
