using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.UI;
using University_Internship_Management_System.Services.FinalEvaluation;
namespace University_Internship_Management_System.Company
{
    public partial class ReportsFinalEvaluation : PercentageEvaluationPage
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            AddSidebarLogout();
        }

        private void AddSidebarLogout()
        {
            var sidebar = FindControlRecursive(this, "companySidebar") as System.Web.UI.UserControl;
            var slot = sidebar == null ? null : FindControlRecursive(sidebar, "LogoutSlot") as System.Web.UI.WebControls.PlaceHolder;
            if (slot == null || slot.Controls.Count != 0) return;
            var logout = new System.Web.UI.WebControls.LinkButton
            {
                ID = "btnLogout",
                Text = "Logout",
                CausesValidation = false,
                CssClass = "uims-side-link uims-side-logout"
            };
            logout.Click += btnLogout_Click;
            slot.Controls.Add(logout);
        }

        private static System.Web.UI.Control FindControlRecursive(System.Web.UI.Control parent, string id)
        {
            if (parent == null) return null;
            if (string.Equals(parent.ID, id, StringComparison.Ordinal)) return parent;
            foreach (System.Web.UI.Control child in parent.Controls)
            {
                var match = FindControlRecursive(child, id);
                if (match != null) return match;
            }
            return null;
        }
        protected override bool Academic { get { return false; } }
        protected string AssignedTraineesCount { get; private set; } = "—";
        protected string CompanyDraftCount { get; private set; } = "—";
        protected string AwaitingReviewCount { get; private set; } = "—";
        protected string AcademicApprovalCount { get; private set; } = "—";

        protected override void BindStatistics(int? assigned, int? drafts, int? awaiting, int? approved)
        {
            AssignedTraineesCount = Count(assigned);
            CompanyDraftCount = Count(drafts);
            AwaitingReviewCount = Count(awaiting);
            AcademicApprovalCount = Count(approved);
        }
        private static string Count(int? value) { return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "—"; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Runs after the existing page workflow, including confirmed postback writes.
            // Statistics remain unavailable until an authorized Firebase context exists.
        }

    }
}
