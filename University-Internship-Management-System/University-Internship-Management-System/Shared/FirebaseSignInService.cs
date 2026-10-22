using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    public sealed class FirebaseLoginResult
    {
        public string Uid { get; set; }
        public string Role { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public sealed class FirebaseLoginException : Exception
    {
        public FirebaseLoginException(string message) : base(message) { }
    }

    public sealed class FirebaseSignInService
    {
        private static readonly HttpClient SharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        private readonly HttpClient client;
        private readonly string apiKey;
        private readonly Func<string, Task<string>> verify;
        private readonly Func<string, Task<JObject>> profile;

        public FirebaseSignInService() : this(SharedClient, ApiKey(), VerifyAsync, ReadProfileAsync) { }

        public FirebaseSignInService(HttpClient client, string apiKey,
            Func<string, Task<string>> verify, Func<string, Task<JObject>> profile)
        {
            this.client = client; this.apiKey = apiKey; this.verify = verify; this.profile = profile;
        }

        public static string ApiKey()
        {
            return Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ??
                ConfigurationManager.AppSettings["FirebaseWebApiKey"];
        }

        public async Task<FirebaseLoginResult> SignInAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new FirebaseLoginException("Login needs the Firebase Web API key configured on the server.");
            using (var content = new StringContent(JsonConvert.SerializeObject(new
            {
                email = email.Trim(), password, returnSecureToken = true
            }), Encoding.UTF8, "application/json"))
            using (var response = await client.PostAsync(
                "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + Uri.EscapeDataString(apiKey), content))
            {
                var body = JObject.Parse(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    string code = (string)body["error"]?["message"] ?? "";
                    if (code.StartsWith("API_KEY_INVALID") || code.StartsWith("CONFIGURATION_NOT_FOUND") || code.StartsWith("OPERATION_NOT_ALLOWED"))
                        throw new FirebaseLoginException("Check the Firebase Web API key and enable Email/Password sign-in for this project.");
                    if (code.StartsWith("TOO_MANY_ATTEMPTS") || (int)response.StatusCode == 429)
                        throw new FirebaseLoginException("Too many sign-in attempts. Wait a minute and try again.");
                    throw new FirebaseLoginException("Sign-in failed. Check the email and password, or contact your coordinator if the account is disabled.");
                }
                string idToken = (string)body["idToken"];
                string localId = (string)body["localId"];
                if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(localId))
                    throw new FirebaseLoginException("Firebase returned an incomplete sign-in response.");
                string uid = await verify(idToken);
                if (!string.Equals(uid, localId, StringComparison.Ordinal))
                    throw new FirebaseLoginException("The sign-in response could not be verified.");
                return ProfileResult(uid, await profile(uid));
            }
        }

        public static FirebaseLoginResult ProfileResult(string uid, JObject value)
        {
            if (value == null)
                throw new FirebaseLoginException("Your email and password were accepted, but your database profile was not found. The profile must be at users/" + uid + " using the exact Authentication UID.");
            if (value.Value<bool?>("isActive") != true)
                throw new FirebaseLoginException("Your profile exists, but isActive is not the boolean true. Profile: users/" + uid);
            string setupStatus = StudentManagementService.Text(value, "accountSetupStatus");
            if (setupStatus != "" && setupStatus != "complete")
                throw new FirebaseLoginException("Your account setup is unfinished. Profile: users/" + uid + ". Setup status: " + setupStatus);
            if (StudentManagementService.Text(value, "deletionStatus") == "pending")
                throw new FirebaseLoginException("Your account is pending deletion. Contact your coordinator.");
            string role = CanonicalRole(StudentManagementService.Text(value, "role"));
            if (role == null) throw new FirebaseLoginException("Your account has no supported role. Contact your coordinator.");
            return new FirebaseLoginResult { Uid = uid, Role = role, MustChangePassword = value.Value<bool?>("mustChangePassword") == true };
        }

        public static string CanonicalRole(string role)
        {
            switch (role)
            {
                case "coordinator": case "student": return role;
                case "academicSupervisor": case "academic_supervisor": case "academic": return "academicSupervisor";
                case "companySupervisor": case "company_supervisor": case "company": return "companySupervisor";
                default: return null;
            }
        }

        public static string Destination(string role)
        {
            switch (role)
            {
                case "coordinator": return "~/Coordinator/ManageStudents.aspx";
                case "student": return "~/Student/StudentDashboard.aspx";
                case "academicSupervisor": return "~/Academic/AcademicDashboard.aspx";
                case "companySupervisor": return "~/Company/CompanyDashboard.aspx";
                default: throw new FirebaseLoginException("Account role is unavailable.");
            }
        }

        private static async Task<string> VerifyAsync(string idToken)
        {
            var verified = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, true);
            return verified.Uid;
        }

        public static async Task<JObject> ReadProfileAsync(string uid)
        {
            return await new StudentFirebaseGateway().ReadAsync("users/" + StudentManagementService.Key(uid)) as JObject;
        }
    }
}
