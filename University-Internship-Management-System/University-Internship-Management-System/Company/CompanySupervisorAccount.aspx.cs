using System;
using System.Globalization;
using System.Web;
using University_Internship_Management_System.Services;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Company
{
    public partial class CompanySupervisorAccount : Page
    {
        private UimsAccount Account => Context.Items["Uims.Account"] as UimsAccount ?? throw new UnauthorizedAccessException();

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            NavName.Text = HttpUtility.HtmlEncode(Account.DisplayName);
            LoadTrustedProfile();

            ValidationError.Text = string.Empty;
            ProfileMessage.Text = string.Empty;
            foreach (var field in new[] { FullName, Phone })
            {
                field.Attributes.Remove("aria-invalid");
            }
        }

        protected void EditProfile_Click(object sender, EventArgs e)
        {
            ProfileMessage.Text = "Profile editing is unavailable until scoped write permissions are configured.";
        }

        protected void SaveProfile_Click(object sender, EventArgs e)
        {
            throw new UnauthorizedAccessException("Profile writes are not configured.");
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
            SessionService.Logout(SessionService.Handle(Request));
            Response.Redirect("~/Auth/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void LoadTrustedProfile()
        {
            ViewState["DisplayedProfile"] = new[] { Account.DisplayName, string.Empty };
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
            OverviewEmail.Text = Account.Email;
            DisplayName.Text = profile[0];
            DisplayPhone.Text = string.IsNullOrEmpty(profile[1]) ? "Not provided" : profile[1];
            DisplayEmail.Text = Account.Email;
            EditEmail.Text = Account.Email;
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
            EditProfile.Visible = true;
            EditProfile.Enabled = false;
            EditProfile.ToolTip = "Contact editing requires scoped Firebase write permissions.";
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
