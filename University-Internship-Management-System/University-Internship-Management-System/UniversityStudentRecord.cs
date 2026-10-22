using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace University_Internship_Management_System
{
    public class UniversityStudentRecord
    {
        public string studentId { get; set; }
        public string fullName { get; set; }
        public string email { get; set; }
        public string universityId { get; set; }
        public string collegeId { get; set; }
        public string majorId { get; set; }
        public int requiredTrainingHours { get; set; }
        public bool isEligible { get; set; }
    }
}