using System;
using System.Web;
using System.Web.UI;

namespace University_Internship_Management_System.Coordinator
{
    public partial class ManageStudents : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }
    }
}
