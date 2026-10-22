using System;
using System.Web.UI;

namespace University_Internship_Management_System.Academic
{
    public partial class AcademicSupervisorAccount : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
        }
    }
}
