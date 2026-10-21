using System;
using System.Web.UI;

namespace University_Internship_Management_System.Student
{
    public partial class MyApplications : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
        }
    }
}
