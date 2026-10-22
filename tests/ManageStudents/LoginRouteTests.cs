using System;
using University_Internship_Management_System.Shared;

internal static class LoginRouteTests
{
    internal static void Run()
    {
        foreach(string page in new[]{"ManageStudents","ManageCompanies","Manage_Academic_Supervisor"})
            foreach(string prefix in new[]{"/","~/"})
                foreach(string suffix in new[]{"",".aspx"})
                    Assert(LoginRedirectPolicy.CoordinatorPage("coordinator",prefix+"Coordinator/"+page+suffix)=="~/Coordinator/"+page+".aspx","safe route not restored");
        foreach(string bad in new[]{"https://evil.example/Coordinator/ManageStudents","//evil.example","javascript:alert(1)","/Coordinator/../Auth/Login.aspx","/Coordinator/Manage_Academic_Supervisor?api=1","/%2f%2fevil.example","/Coordinator/Unknown.aspx"})
            Assert(LoginRedirectPolicy.CoordinatorPage("coordinator",bad)==null,"unsafe route accepted");
        foreach(string role in new[]{"student","academicSupervisor","companySupervisor","admin",""})
            Assert(LoginRedirectPolicy.CoordinatorPage(role,"/Coordinator/Manage_Academic_Supervisor")==null,"wrong role accepted");
        Console.WriteLine("PASS sign-in return routes: fixed coordinator pages, external/traversal rejection, and role isolation");
    }
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
}
