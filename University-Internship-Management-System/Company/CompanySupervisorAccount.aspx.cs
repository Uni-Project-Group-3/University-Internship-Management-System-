using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Company
{
    public partial class CompanySupervisorAccount : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            if (!Context.User.Identity.IsAuthenticated || !Context.User.IsInRole("companySupervisor"))
            {
                Response.Redirect("../Auth/Login.aspx", true);
                return;
            }

            if (!IsPostBack)
                RegisterAsyncTask(new PageAsyncTask(LoadProfileAsync));

            ValidationError.Text = string.Empty;
            ProfileMessage.Text = string.Empty;
            foreach (var field in new[] { FullName, Phone }) field.Attributes.Remove("aria-invalid");
        }

        protected void EditProfile_Click(object sender, EventArgs e)
        {
            LoadEditingFields();
            SetEditing(true);
            Page.ClientScript.RegisterStartupScript(GetType(), "openCompanySupervisorProfileDialog",
                "document.getElementById('profileEditDialog').showModal();", true);
        }

        protected void SaveProfile_Click(object sender, EventArgs e)
        {
            if (!EditProfileFields.Visible || !SaveProfile.Visible) return;

            string name = Regex.Replace(FullName.Text.Trim(), @"\s+", " ");
            string phone = Phone.Text.Trim();
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

            RegisterAsyncTask(new PageAsyncTask(() => SaveProfileAsync(name, phone)));
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                string companyName = string.Empty;
                string companyId = Text(profile, "companyId");
                if (companyId.Length > 0)
                {
                    string token = Session["Uims.FirebaseIdToken"] as string;
                    var company = await FirebaseSignInService.ReadDatabasePathAsync("companies/" + StudentManagementService.Key(companyId), token) as JObject;
                    companyName = Text(company, "name");
                }

                ViewState["DisplayedProfile"] = new[]
                {
                    Text(profile, "fullName"), Text(profile, "phone"), Text(profile, "email"), companyName
                };
                ViewState["ProfileLoaded"] = true;
                LoadEditingFields();
                SetEditing(false);
                NavName.Text = Text(profile, "fullName");
                CompanySummary.Text = string.IsNullOrWhiteSpace(companyName) ? "Company not linked" : companyName;
                AccountConnection.Text = "Your information is loaded from Firebase Realtime Database.";
                BindDisplayedProfile();
            }
            catch (FirebaseLoginException error)
            {
                ViewState["ProfileLoaded"] = false;
                AccountConnection.Text = error.Message;
                EditProfile.Enabled = false;
                BindDisplayedProfile();
            }
            catch (Exception)
            {
                ViewState["ProfileLoaded"] = false;
                AccountConnection.Text = "Your Firebase profile could not be loaded. Sign in again or contact your coordinator.";
                EditProfile.Enabled = false;
                BindDisplayedProfile();
            }
        }

        private async Task SaveProfileAsync(string name, string phone)
        {
            try
            {
                await GetVerifiedProfileAsync();
                string uid = Context.User.Identity.Name;
                string token = Session["Uims.FirebaseIdToken"] as string;
                await FirebaseSignInService.PatchDatabasePathAsync("users/" + StudentManagementService.Key(uid), new
                {
                    fullName = name,
                    phone = phone
                }, token);

                var displayed = ViewState["DisplayedProfile"] as string[] ?? new string[4];
                if (displayed.Length < 4) Array.Resize(ref displayed, 4);
                displayed[0] = name;
                displayed[1] = phone;
                ViewState["DisplayedProfile"] = displayed;
                ViewState["ProfileLoaded"] = true;
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

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

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
            if (!string.Equals(verified.Uid, uid, StringComparison.Ordinal) || verified.Role != "companySupervisor")
                throw new HttpException(403, "A verified company supervisor account is required.");
            return verified.Profile;
        }

        private void LoadEditingFields()
        {
            var profile = ViewState["DisplayedProfile"] as string[] ?? new string[4];
            if (profile.Length < 4) Array.Resize(ref profile, 4);
            FullName.Text = profile[0] ?? string.Empty;
            Phone.Text = profile[1] ?? string.Empty;
        }

        private void BindDisplayedProfile()
        {
            var profile = ViewState["DisplayedProfile"] as string[] ?? new string[4];
            if (profile.Length < 4) Array.Resize(ref profile, 4);
            string name = profile[0] ?? string.Empty;
            ProfileName.Text = string.IsNullOrWhiteSpace(name) ? "Company Supervisor" : name;
            ProfileInitials.Text = GetInitials(name);
            OverviewEmail.Text = Fallback(profile[2]);
            DisplayName.Text = Fallback(name);
            DisplayPhone.Text = Fallback(profile[1]);
            DisplayEmail.Text = Fallback(profile[2]);
            EditEmail.Text = Fallback(profile[2]);
            CompanyName.Text = Fallback(profile[3]);
            CompanySummary.Text = string.IsNullOrWhiteSpace(profile[3]) ? "Company not linked" : profile[3];
            EditingHint.Text = EditProfileFields.Visible
                ? "Update your name and contact phone. Your account email and company association are protected."
                : "Your account details are loaded from Firebase Realtime Database.";
            EditingStatus.Text = EditProfileFields.Visible
                ? "Editing"
                : (ViewState["ProfileLoaded"] as bool? == true ? "Connected" : "Unavailable");
        }

        private void SetEditing(bool editing)
        {
            ReadProfile.Visible = true;
            EditProfileFields.Visible = editing;
            FullName.ReadOnly = !editing;
            Phone.ReadOnly = !editing;
            EditProfile.Visible = !editing;
            SaveProfile.Visible = editing;
            CancelEdit.Visible = editing;
            if (editing) FullName.Focus();
        }

        private void ShowValidationError(System.Web.UI.WebControls.TextBox field, string message)
        {
            ValidationError.Text = message;
            field.Attributes["aria-invalid"] = "true";
            field.Focus();
        }

        private static string Text(JObject value, string field)
        {
            return (string)value?[field] ?? string.Empty;
        }

        private static string Fallback(string value, string fallback = "Not provided")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string GetInitials(string name)
        {
            var parts = (name ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "CS";
            string initials = StringInfo.GetNextTextElement(parts[0]);
            if (parts.Length > 1) initials += StringInfo.GetNextTextElement(parts[parts.Length - 1]);
            return initials.ToUpperInvariant();
        }
    }
}
