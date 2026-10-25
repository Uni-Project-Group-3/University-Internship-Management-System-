using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    public partial class CoordinatorAccountProfile : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            if (!Context.User.Identity.IsAuthenticated || !Context.User.IsInRole("coordinator"))
            {
                Response.Redirect("../Auth/Login.aspx", true);
                return;
            }

            if (!IsPostBack)
                RegisterAsyncTask(new PageAsyncTask(LoadProfileAsync));

            ValidationError.Text = string.Empty;
            ProfileMessage.Text = string.Empty;
            foreach (var field in new[] { FullName, Phone, Office }) field.Attributes.Remove("aria-invalid");
        }

        protected void EditProfile_Click(object sender, EventArgs e)
        {
            LoadEditingFields();
            SetEditing(true);
            Page.ClientScript.RegisterStartupScript(GetType(), "openCoordinatorProfileDialog",
                "document.getElementById('profileEditDialog').showModal();", true);
        }

        protected void SaveProfile_Click(object sender, EventArgs e)
        {
            if (!EditProfileFields.Visible || !SaveProfile.Visible) return;

            string name = Regex.Replace(FullName.Text.Trim(), @"\s+", " ");
            string phone = Phone.Text.Trim();
            string office = Office.Text.Trim();
            if (name.Length < 2 || name.Length > 80 || !Regex.IsMatch(name, @"\p{L}"))
            {
                ShowValidationError(FullName, "Enter a full name between 2 and 80 characters, including at least one letter.");
                return;
            }
            if (phone.Length > 0 && !Regex.IsMatch(phone, @"\A(?=(?:[^0-9]*[0-9]){7})[+0-9() .-]{7,25}\z"))
            {
                ShowValidationError(Phone, "Enter a phone number with at least 7 digits and no more than 25 characters, or leave it blank.");
                return;
            }
            if (office.Length > 100)
            {
                ShowValidationError(Office, "Office location must be 100 characters or fewer.");
                return;
            }

            RegisterAsyncTask(new PageAsyncTask(() => SaveProfileAsync(name, phone, office)));
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                if (profile == null) return;
                var universityProfile = await new StudentFirebaseGateway()
                    .ReadAsync("users/DEMO_UID_COORDINATOR_001") as JObject;
                var universityDetails = ReadUniversityDetails(universityProfile);
                string coordinatorId = First(profile, "id", "coordinatorId", "staffId");
                if (string.IsNullOrWhiteSpace(coordinatorId))
                    coordinatorId = First(universityProfile, "id", "coordinatorId", "staffId");
                ViewState["DisplayedProfile"] = new[]
                {
                    First(profile, "fullName", "displayName"), Text(profile, "phone"), Text(profile, "officeLocation"),
                    Text(profile, "email"), universityDetails[0],
                    universityDetails[1], string.Empty,
                    coordinatorId
                };
                LoadEditingFields();
                SetEditing(false);
                BindDisplayedProfile();
                NavName.Text = Text(profile, "fullName");
                AccountConnection.Text = "Your information is loaded from Firebase Realtime Database.";
            }
            catch (Exception)
            {
                AccountConnection.Text = "Your Firebase profile could not be loaded. Sign in again or contact your administrator.";
                EditProfile.Enabled = false;
            }
        }

        private static string[] ReadUniversityDetails(JObject profile)
        {
            if (profile == null)
                throw new InvalidOperationException("The university information record was not found.");
            string university = (string)UniversityField(profile, "University") ??
                First(profile, "universityName", "university", "universityId");
            string department = (string)UniversityField(UniversityField(profile, "College") as JObject, "department");
            return new[] { university, department ?? Join(First(profile, "collegeName", "collegeId"),
                First(profile, "departmentName", "department", "departmentId")) };
        }

        private static JToken UniversityField(JObject profile, string name)
        {
            if (profile == null) return null;
            foreach (var field in profile.Properties())
                if (string.Equals(field.Name.Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return field.Value;
            return null;
        }

        private async Task SaveProfileAsync(string name, string phone, string office)
        {
            try
            {
                if (await GetVerifiedProfileAsync() == null) return;
                string uid = Context.User.Identity.Name;
                string token = Session["Uims.FirebaseIdToken"] as string;
                await FirebaseSignInService.PatchDatabasePathAsync("users/" + StudentManagementService.Key(uid), new
                {
                    fullName = name,
                    phone = phone,
                    officeLocation = office
                }, token);

                var displayed = ViewState["DisplayedProfile"] as string[] ?? new string[8];
                if (displayed.Length < 8) Array.Resize(ref displayed, 8);
                displayed[0] = name;
                displayed[1] = phone;
                displayed[2] = office;
                ViewState["DisplayedProfile"] = displayed;
                LoadEditingFields();
                SetEditing(false);
                NavName.Text = name;
                ProfileMessage.Text = "Your profile changes were saved to Firebase Realtime Database.";
                BindDisplayedProfile();
            }
            catch (Exception)
            {
                ProfileMessage.Text = "Your changes could not be saved. No successful Firebase update was confirmed; check the connection and try again.";
                BindDisplayedProfile();
            }
        }

        protected void CancelEdit_Click(object sender, EventArgs e)
        {
            LoadEditingFields();
            SetEditing(false);
            ProfileMessage.Text = "Unsaved changes discarded.";
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            BindDisplayedProfile();
        }

        protected void btnLogout_Click(object sender, EventArgs e) { University_Internship_Management_System.Shared.LoginSession.End(Context); Session.Clear(); Session.Abandon(); Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"), false); Context.ApplicationInstance.CompleteRequest(); }

        private async Task<JObject> GetVerifiedProfileAsync()
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
            if (!string.Equals(verified.Uid, uid, StringComparison.Ordinal) || verified.Role != "coordinator")
                throw new HttpException(403, "A verified coordinator account is required.");
            return verified.Profile;
        }

        private void LoadEditingFields()
        {
            var profile = ViewState["DisplayedProfile"] as string[] ?? new string[8];
            if (profile.Length < 8) Array.Resize(ref profile, 8);
            FullName.Text = profile[0] ?? string.Empty;
            Phone.Text = profile[1] ?? string.Empty;
            Office.Text = profile[2] ?? string.Empty;
        }

        private void BindDisplayedProfile()
        {
            var profile = ViewState["DisplayedProfile"] as string[] ?? new string[8];
            if (profile.Length < 8) Array.Resize(ref profile, 8);
            string name = profile[0] ?? string.Empty;
            ProfileName.Text = Fallback(name, "Coordinator");
            ProfileInitials.Text = GetInitials(name);
            OverviewEmail.Text = Fallback(profile[3]);
            DisplayName.Text = Fallback(name);
            DisplayPhone.Text = Fallback(profile[1]);
            DisplayOffice.Text = Fallback(profile[2]);
            DisplayEmail.Text = Fallback(profile[3]);
            EditEmail.Text = Fallback(profile[3]);
            UniversityName.Text = Fallback(profile[4]);
            UniversitySummary.Text = UniversityName.Text;
            CollegeDepartment.Text = Join(profile[5], profile[6]);
            CoordinatorId.Text = Fallback(profile[7]);
            EditingHint.Text = "Update your permitted contact details. Your email, university, department and coordinator ID are protected.";
            EditingStatus.Text = "Connected";
        }

        private void SetEditing(bool editing)
        {
            ReadProfile.Visible = false;
            EditProfileFields.Visible = true;
            FullName.ReadOnly = false;
            Phone.ReadOnly = false;
            Office.ReadOnly = false;
            EditProfile.Visible = false;
            SaveProfile.Visible = true;
            CancelEdit.Visible = false;
        }

        private void ShowValidationError(TextBox field, string message)
        {
            ValidationError.Text = message;
            field.Attributes["aria-invalid"] = "true";
            field.Focus();
        }

        private static string Text(JObject value, string field)
        {
            return (string)value?[field] ?? string.Empty;
        }

        private static string First(JObject value, params string[] fields)
        {
            foreach (string field in fields)
            {
                string result = Text(value, field);
                if (result.Length > 0) return result;
            }
            return string.Empty;
        }

        private static string Join(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first)) return Fallback(second);
            if (string.IsNullOrWhiteSpace(second)) return first;
            return first + " / " + second;
        }

        private static string Fallback(string value, string fallback = "Not provided")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string GetInitials(string name)
        {
            var parts = (name ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "UC";
            string initials = StringInfo.GetNextTextElement(parts[0]);
            if (parts.Length > 1) initials += StringInfo.GetNextTextElement(parts[parts.Length - 1]);
            return initials.ToUpperInvariant();
        }
    }
}
