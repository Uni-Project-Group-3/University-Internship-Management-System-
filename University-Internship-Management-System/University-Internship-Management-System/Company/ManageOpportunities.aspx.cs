using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using UIMS.Models;
using UIMS.Services;

namespace UIMS.Company
{
    public partial class ManageOpportunities : System.Web.UI.Page
    {
        // Specializations offered by the Faculty of Computer & Information Technology at JUST.
        // Keep this list in one place so the student "Explore Opportunities" page can match on it.
        public static readonly string[] Majors =
        {
            "Computer Science",
            "Computer Engineering",
            "Software Engineering",
            "Computer Information Systems",
            "Cybersecurity",
            "Data Science & AI",
            "Network Engineering & Security"
        };

        private const string DateFormat = "yyyy-MM-dd";   // format used by <input type="date">

        private string CompanyId => Session["CompanyId"] as string ?? "orange-jo";
        private string CompanyName => Session["CompanyName"] as string ?? "Orange Jordan";

        private string CurrentFilter
        {
            get { return ViewState["Filter"] as string ?? "All"; }
            set { ViewState["Filter"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // ---- Role guard ----
            // Enable once Login.aspx stores the role in Session["Role"].
            // if (Session["Role"] as string != "Company")
            // {
            //     Response.Redirect("~/Login.aspx");
            //     return;
            // }

            LoadHeader();

            if (!IsPostBack)
            {
                cblMajors.DataSource = Majors;
                cblMajors.DataBind();
                BindOpportunities();
            }
        }

        // =====================================================
        //  List + stats
        // =====================================================
        private void BindOpportunities()
        {
            List<Opportunity> all = OpportunityService.GetByCompany(CompanyId);
            List<Opportunity> open = all.Where(o => o.IsOpen).ToList();

            // stats
            lblTotal.Text = all.Count.ToString("00");
            lblOpen.Text = open.Count.ToString("00");
            lblSeatsLeft.Text = open.Sum(o => o.SeatsLeft).ToString("00");
            lblApplicants.Text = open.Sum(o => o.ApplicantsCount).ToString("00");

            litCountAll.Text = all.Count.ToString();
            litCountOpen.Text = open.Count.ToString();
            litCountClosed.Text = (all.Count - open.Count).ToString();

            // filter by tab
            IEnumerable<Opportunity> list = all;
            if (CurrentFilter == OpportunityStatus.Open) list = open;
            else if (CurrentFilter == OpportunityStatus.Closed) list = all.Where(o => !o.IsOpen);

            // filter by search text
            string q = txtSearch.Text.Trim();
            if (q.Length > 0)
            {
                list = list.Where(o =>
                    Contains(o.Title, q) || Contains(o.Location, q) ||
                    o.Majors.Any(m => Contains(m, q)));
            }

            List<Opportunity> result = list.ToList();
            rptOpportunities.DataSource = result;
            rptOpportunities.DataBind();
            pnlEmpty.Visible = result.Count == 0;

            tabAll.CssClass = CurrentFilter == "All" ? "active" : "";
            tabOpen.CssClass = CurrentFilter == OpportunityStatus.Open ? "active" : "";
            tabClosed.CssClass = CurrentFilter == OpportunityStatus.Closed ? "active" : "";
        }

        protected void Filter_Command(object sender, CommandEventArgs e)
        {
            CurrentFilter = e.CommandArgument.ToString();
            BindOpportunities();
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            BindOpportunities();
        }

        // =====================================================
        //  Card buttons (Edit / Close / Reopen)
        // =====================================================
        protected void rptOpportunities_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            string id = e.CommandArgument.ToString();
            Opportunity opp = OpportunityService.GetById(id);

            // A supervisor can only touch their own company's opportunities
            if (opp == null || opp.CompanyId != CompanyId) return;

            if (e.CommandName == "EditOpp")
            {
                FillForm(opp);
                litModalTitle.Text = "Edit opportunity";
                OpenModal();
            }
            else if (e.CommandName == "ToggleStatus")
            {
                if (opp.IsOpen)
                {
                    OpportunityService.SetStatus(id, OpportunityStatus.Closed);
                    ShowMessage("\"" + Encode(opp.Title) + "\" is closed. Students can no longer apply — you can reopen it anytime.");
                }
                else
                {
                    OpportunityService.SetStatus(id, OpportunityStatus.Open);
                    ShowMessage("\"" + Encode(opp.Title) + "\" is open again for applications.");
                }
            }

            BindOpportunities();
        }

        // =====================================================
        //  Add / Edit form
        // =====================================================
        protected void btnNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            litModalTitle.Text = "New opportunity";
            OpenModal();
            BindOpportunities();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Page.Validate("opp");
            if (!Page.IsValid)
            {
                OpenModal();            // keep the form open so the errors are visible
                BindOpportunities();
                return;
            }

            bool isNew = string.IsNullOrEmpty(hfOppId.Value);
            Opportunity opp;

            if (isNew)
            {
                opp = new Opportunity
                {
                    CompanyId = CompanyId,
                    CompanyName = CompanyName
                };
            }
            else
            {
                opp = OpportunityService.GetById(hfOppId.Value);
                if (opp == null || opp.CompanyId != CompanyId) return;
            }

            opp.Title = txtTitle.Text.Trim();
            opp.Description = txtDescription.Text.Trim();
            opp.Requirements = txtRequirements.Text.Trim();
            opp.Majors = cblMajors.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList();
            opp.Seats = int.Parse(txtSeats.Text);
            opp.WorkMode = rblWorkMode.SelectedValue;
            opp.Location = txtLocation.Text.Trim();
            opp.Deadline = ParseDate(txtDeadline.Text).Value;
            opp.StartDate = ParseDate(txtStartDate.Text).Value;
            opp.DurationWeeks = int.Parse(txtDuration.Text);

            if (isNew)
            {
                OpportunityService.Add(opp);
                ShowMessage("\"" + Encode(opp.Title) + "\" is published. Students in the selected majors can now apply.");
            }
            else
            {
                OpportunityService.Update(opp);
                ShowMessage("Changes to \"" + Encode(opp.Title) + "\" are saved.");
            }

            ClearForm();
            BindOpportunities();
        }

        private void FillForm(Opportunity o)
        {
            hfOppId.Value = o.Id;
            txtTitle.Text = o.Title;
            txtDescription.Text = o.Description;
            txtRequirements.Text = o.Requirements;
            foreach (ListItem item in cblMajors.Items)
                item.Selected = o.Majors.Contains(item.Value);
            txtSeats.Text = o.Seats.ToString();
            rblWorkMode.SelectedValue = o.WorkMode;
            txtLocation.Text = o.Location;
            txtDeadline.Text = o.Deadline.ToString(DateFormat);
            txtStartDate.Text = o.StartDate.ToString(DateFormat);
            txtDuration.Text = o.DurationWeeks.ToString();
        }

        private void ClearForm()
        {
            hfOppId.Value = "";
            txtTitle.Text = txtDescription.Text = txtRequirements.Text = txtLocation.Text = "";
            cblMajors.ClearSelection();
            txtSeats.Text = "1";
            rblWorkMode.SelectedIndex = 0;
            txtDeadline.Text = DateTime.Today.AddDays(14).ToString(DateFormat);
            txtStartDate.Text = DateTime.Today.AddDays(30).ToString(DateFormat);
            txtDuration.Text = "8";
        }

        // =====================================================
        //  Server-side validation (business rules)
        // =====================================================
        protected void cvMajors_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = cblMajors.Items.Cast<ListItem>().Any(i => i.Selected);
        }

        // Seats cannot be lowered below the number of students already accepted
        protected void cvSeatsAccepted_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = true;
            if (string.IsNullOrEmpty(hfOppId.Value)) return;

            Opportunity existing = OpportunityService.GetById(hfOppId.Value);
            int seats;
            if (existing != null && int.TryParse(txtSeats.Text, out seats) && seats < existing.AcceptedCount)
            {
                cvSeatsAccepted.ErrorMessage = existing.AcceptedCount + " students are already accepted — seats can't be lower than that.";
                args.IsValid = false;
            }
        }

        protected void cvDates_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = true;
            DateTime? deadline = ParseDate(txtDeadline.Text);
            DateTime? start = ParseDate(txtStartDate.Text);

            if (deadline == null || start == null)
            {
                cvDates.ErrorMessage = "Enter valid dates.";
                args.IsValid = false;
            }
            else if (string.IsNullOrEmpty(hfOppId.Value) && deadline.Value < DateTime.Today)
            {
                cvDates.ErrorMessage = "The application deadline can't be in the past.";
                args.IsValid = false;
            }
            else if (deadline.Value > start.Value)
            {
                cvDates.ErrorMessage = "The application deadline must be on or before the start date.";
                args.IsValid = false;
            }
        }

        // =====================================================
        //  Helpers used by the Repeater markup
        // =====================================================
        protected string MajorTags(object majors)
        {
            var list = majors as IEnumerable<string>;
            if (list == null) return "";
            return string.Concat(list.Select(m => "<span class=\"tag\">" + Encode(m) + "</span>"));
        }

        protected int SeatsPercent(object accepted, object seats)
        {
            int a = Convert.ToInt32(accepted), s = Convert.ToInt32(seats);
            return s <= 0 ? 0 : Math.Min(100, a * 100 / s);
        }

        protected string SeatsBarCss(object accepted, object seats)
        {
            return Convert.ToInt32(accepted) >= Convert.ToInt32(seats) ? "success" : "";
        }

        // =====================================================
        //  Misc
        // =====================================================
        private void LoadHeader()
        {
            string fullName = Session["FullName"] as string ?? "Mohammad Haddad";
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            lblUserName.Text = parts.Length > 1 ? parts[0] + " " + parts[parts.Length - 1][0] + "." : fullName;
            lblInitials.Text = string.Concat(parts.Take(2).Select(p => char.ToUpper(p[0])));
            lblCompany.Text = Encode(CompanyName);
        }

        private void OpenModal()
        {
            // Runs after the page (and Bootstrap) finish loading
            ClientScript.RegisterStartupScript(GetType(), "openModal",
                "document.addEventListener('DOMContentLoaded', showOppModal);", true);
        }

        private void ShowMessage(string html)
        {
            litMessage.Text = html;
            pnlMessage.Visible = true;
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime d;
            return DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)
                ? d : (DateTime?)null;
        }

        private static bool Contains(string text, string q)
        {
            return text != null && text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Encode(string s) => HttpUtility.HtmlEncode(s);

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }
    }
}
