using System.Web.UI;

namespace University_Internship_Management_System.Public
{
    public partial class home : Page
    {
        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);
            Response.Cache.SetNoStore();
        }
    }
}
