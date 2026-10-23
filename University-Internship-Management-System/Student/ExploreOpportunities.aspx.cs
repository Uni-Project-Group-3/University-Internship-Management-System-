using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Student
{
    // Student: sees the open opportunities for their specialization (read only)
    public partial class ExploreOpportunities : InternshipPage
    {
        protected override string Role { get { return "student"; } }

        protected override JObject BuildView()
        {
            return UimsData.StudentView(Db, Uid, Profile);
        }
    }
}