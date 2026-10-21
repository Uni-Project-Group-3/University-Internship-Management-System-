using System;
using System.Linq;

namespace UIMS.Coordinator
{
    public partial class CoordinatorDashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // ---- Role guard ----
            // Enable this once Login.aspx stores the user's role in Session["Role"]
            // after signing in with Firebase Authentication.
            // if (Session["Role"] as string != "Coordinator")
            // {
            //     Response.Redirect("~/Login.aspx");
            //     return;
            // }

            if (!IsPostBack)
            {
                LoadHeader();
                LoadStats();
            }
        }

        private void LoadHeader()
        {
            string fullName = Session["FullName"] as string ?? "Sara Nimer";
            string firstName = fullName.Split(' ')[0];

            lblUserName.Text = ShortName(fullName);
            lblInitials.Text = Initials(fullName);
            lblDate.Text = DateTime.Now.ToString("ddd, MMM dd, hh:mm tt").ToUpper();
            lblGreeting.Text = Greeting() + ", " + firstName + ".";
        }

        private void LoadStats()
        {
            // TODO: replace these sample numbers with counts read from Firebase.
            lblStudents.Text = "128";
            lblCompanies.Text = "37";
            lblOpportunities.Text = "52";
            lblAwaiting.Text = "04";
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }

        // ---------- helpers ----------
        private static string Greeting()
        {
            int h = DateTime.Now.Hour;
            if (h < 12) return "Good morning";
            if (h < 18) return "Good afternoon";
            return "Good evening";
        }

        private static string Initials(string fullName)
        {
            return string.Concat(fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Take(2)
                                         .Select(p => char.ToUpper(p[0])));
        }

        // "Ahmad Saleem" -> "Ahmad S."
        private static string ShortName(string fullName)
        {
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[0] + " " + parts[parts.Length - 1][0] + "." : fullName;
        }
    }
}
