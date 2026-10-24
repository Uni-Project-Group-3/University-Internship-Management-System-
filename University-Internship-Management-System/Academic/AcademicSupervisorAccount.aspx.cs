using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Academic
{
    public partial class AcademicSupervisorAccount : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            if (!Context.User.Identity.IsAuthenticated || !Context.User.IsInRole("academicSupervisor"))
            {
                Response.Redirect("../Auth/Login.aspx", true);
                return;
            }

            if (!IsPostBack)
                RegisterAsyncTask(new PageAsyncTask(LoadProfileAsync));
        }

        private async System.Threading.Tasks.Task LoadProfileAsync()
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                BindProfile(profile, Context.User.Identity.Name);
                SetStatus("Your account information was loaded from Firebase Realtime Database.");
            }
            catch (FirebaseLoginException error)
            {
                SetStatus(error.Message);
            }
            catch (Exception)
            {
                SetStatus("Your account could not be loaded from Firebase. Sign in again or contact your coordinator.");
            }
        }

        protected void SaveAcademicProfile_Click(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(SaveProfileAsync));
        }

        private async System.Threading.Tasks.Task SaveProfileAsync()
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                string phone = supervisorPhone.Value.Trim();
                string office = supervisorOffice.Value.Trim();
                string hours = supervisorOfficeHours.Value.Trim();

                if (phone.Length > 25 || (phone.Length > 0 && !Regex.IsMatch(phone, @"\A[+0-9() .-]{7,25}\z")))
                {
                    SetStatus("Enter a valid phone number (7 to 25 characters).");
                    return;
                }
                if (office.Length > 120 || hours.Length > 200)
                {
                    SetStatus("Office location must be at most 120 characters and office hours at most 200 characters.");
                    return;
                }

                string uid = Context.User.Identity.Name;
                string token = Session["Uims.FirebaseIdToken"] as string;
                await FirebaseSignInService.PatchDatabasePathAsync("users/" + StudentManagementService.Key(uid), new
                {
                    phone = phone,
                    officeLocation = office,
                    officeHours = hours
                }, token);

                BindProfile(profile, Context.User.Identity.Name);
                supervisorPhone.Value = phone;
                supervisorOffice.Value = office;
                supervisorOfficeHours.Value = hours;
                SetStatus("Your contact information was saved to Firebase Realtime Database.");
            }
            catch (FirebaseLoginException error)
            {
                SetStatus(error.Message);
            }
            catch (Exception)
            {
                SetStatus("Your changes could not be saved to Firebase. No successful update was confirmed.");
            }
        }

        private async System.Threading.Tasks.Task<JObject> GetVerifiedProfileAsync()
        {
            string uid = Context.User.Identity.Name;
            string token = Session["Uims.FirebaseIdToken"] as string;
            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(token))
            {
                FormsAuthentication.SignOut();
                Response.Redirect("../Auth/Login.aspx", true);
                return null;
            }

            var verified = await new FirebaseSignInService().VerifySessionAsync(token);
            if (!string.Equals(verified.Uid, uid, StringComparison.Ordinal) || verified.Role != "academicSupervisor")
                throw new HttpException(403, "A verified academic supervisor account is required.");
            return verified.Profile;
        }

        private void BindProfile(JObject profile, string firebaseUid)
        {
            if (profile == null)
                throw new FirebaseLoginException("The signed-in Firebase account has no profile at /users/" +
                    StudentManagementService.Key(firebaseUid) + ". The account ID is known, but profile fields cannot be loaded.", 404);

            string name = Text(profile, "fullName");
            string email = Text(profile, "email");
            string department = Text(profile, "departmentId");
            if (department.Length == 0) department = Text(profile, "majorId");

            litSummaryName.Text = Encoded(name, "Academic Supervisor");
            litSummaryDetails.Text = Encoded(department) + (email.Length > 0 ? " · " + Encoded(email) : "");
            litFullName.Text = Encoded(name);
            litCollegeId.Text = Encoded(Text(profile, "collegeId"));
            litDepartment.Text = Encoded(department);
            litEmail.Text = Encoded(email);

            supervisorPhone.Value = Text(profile, "phone");
            supervisorOffice.Value = Text(profile, "officeLocation");
            supervisorOfficeHours.Value = Text(profile, "officeHours");
        }

        private static string Text(JObject profile, string field)
        {
            return profile == null ? string.Empty : (string)profile[field] ?? string.Empty;
        }

        private static string Encoded(string value, string fallback = "Not provided")
        {
            return HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? fallback : value);
        }

        private void SetStatus(string message)
        {
            litStatus.Text = "<strong>" + HttpUtility.HtmlEncode(message) + "</strong>";
        }
    }
}
