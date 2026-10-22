using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Company
{
    public partial class CompanySupervisorAccount : Page
    {
        private const string DemoEmail = "supervisor@example.edu";

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            NavName.Text = Session["FullName"] as string ?? "Company Portal";
            if (!IsPostBack || ViewState["DisplayedProfile"] == null)
            {
                LoadDemoProfile();
            }

            ValidationError.Text = string.Empty;
            ProfileMessage.Text = string.Empty;
            foreach (var field in new[] { FullName, Phone })
            {
                field.Attributes.Remove("aria-invalid");
            }
        }

        protected void EditProfile_Click(object sender, EventArgs e)
        {
            LoadEditingFields();
            SetEditing(true);
        }

        protected void SaveProfile_Click(object sender, EventArgs e)
        {
            if (!EditProfileFields.Visible || !SaveProfile.Visible)
            {
                return;
            }

            string name = Regex.Replace(FullName.Text.Trim(), @"\s+", " ");
            string phone = Phone.Text.Trim();
            if (name.Length < 2 || name.Length > 80 || !Regex.IsMatch(name, @"\p{L}"))
            {
                ShowValidationError(FullName, "Enter a full name between 2 and 80 characters, including at least one letter.");
                return;
            }

            if (phone.Length > 0 && !Regex.IsMatch(phone, @"\A(?=(?:[^0-9]*[0-9]){7})[+0-9() -]{7,25}\z"))
            {
                ShowValidationError(Phone, "Enter a phone number with at least 7 digits and no more than 25 characters, or leave it blank.");
                return;
            }

            ViewState["DisplayedProfile"] = new[] { name, phone };
            LoadEditingFields();
            SetEditing(false);
            ProfileMessage.Text = "Preview updated on this page only. No account information was saved to Firebase. A new visit restores the demonstration profile.";
        }

        protected void CancelEdit_Click(object sender, EventArgs e)
        {
            LoadEditingFields();
            SetEditing(false);
            ProfileMessage.Text = "Unsaved changes discarded. The previously displayed profile has been restored.";
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            BindDisplayedProfile();
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void LoadDemoProfile()
        {
            ViewState["DisplayedProfile"] = new[] { "Omar Hassan", string.Empty };
            LoadEditingFields();
            SetEditing(false);
        }

        private void LoadEditingFields()
        {
            var profile = (string[])ViewState["DisplayedProfile"];
            FullName.Text = profile[0];
            Phone.Text = profile[1];
        }

        private void BindDisplayedProfile()
        {
            var profile = (string[])ViewState["DisplayedProfile"];
            ProfileName.Text = profile[0];
            ProfileInitials.Text = GetInitials(profile[0]);
            OverviewEmail.Text = DemoEmail;
            DisplayName.Text = profile[0];
            DisplayPhone.Text = string.IsNullOrEmpty(profile[1]) ? "Not provided" : profile[1];
            DisplayEmail.Text = DemoEmail;
            EditEmail.Text = DemoEmail;
            EditingHint.Text = EditProfileFields.Visible
                ? "Update your permitted personal details."
                : "Your personal details, shown in read-only mode.";
            EditingStatus.Text = EditProfileFields.Visible ? "Editing preview" : "Read-only";
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
            if (editing)
            {
                FullName.Focus();
            }
        }

        private void ShowValidationError(TextBox field, string message)
        {
            ValidationError.Text = message;
            field.Attributes["aria-invalid"] = "true";
            field.Focus();
        }

        private static string GetInitials(string name)
        {
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = StringInfo.GetNextTextElement(parts[0]);
            if (parts.Length > 1)
            {
                initials += StringInfo.GetNextTextElement(parts[parts.Length - 1]);
            }

            return initials.ToUpperInvariant();
        }
    }
}
