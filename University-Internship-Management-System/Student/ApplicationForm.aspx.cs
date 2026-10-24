using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Student
{
    // Student: apply to an opportunity (Firebase: /applications)
    public partial class ApplicationForm : InternshipPage
    {
        protected override string Role { get { return "student"; } }

        protected override JObject BuildView()
        {
            return UimsData.StudentView(Db, Uid, Profile);
        }

        protected override void Handle(string action, JObject data)
        {
            if (action == "apply") Apply(data);
            else base.Handle(action, data);
        }

        void Apply(JObject d)
        {
            string studentId = S(Profile["studentId"]);
            string major = S(Profile["department"]) != "" ? S(Profile["department"]) : S(Profile["majorId"]);
            if (studentId == "") throw new AppError("Your account has no student number.");

            string opportunityId = FirebaseDb.Key(S(d["opportunityId"]));
            var opportunity = Db.GetObject("opportunities/" + opportunityId);
            if (opportunity == null || !UimsData.IsOpenFor(opportunity, major))
                throw new AppError("This opportunity is not open for your specialization.");

            var mine = Db.List("applications").Where(a => S(a["studentId"]) == studentId).ToList();
            if (mine.Any(a => S(a["opportunityId"]) == opportunityId))
                throw new AppError("You already applied to this opportunity.");
            if (mine.Any(a => S(a["status"]) == "confirmed"))
                throw new AppError("You already confirmed an internship offer.");

            string phone = S(d["phone"]).Trim();
            string cvLink = S(d["cvLink"]).Trim();
            string portfolio = S(d["portfolioLink"]).Trim();
            if (phone == "") throw new AppError("Enter your phone number.");
            if (!IsUrl(cvLink)) throw new AppError("Enter a valid CV link (https://...).");
            if (portfolio != "" && !IsUrl(portfolio)) throw new AppError("Enter a valid portfolio link.");

            string id = FirebaseDb.NewId("APP");
            var application = new JObject
            {
                ["id"] = id,
                ["opportunityId"] = opportunityId,
                ["companyId"] = S(opportunity["companyId"]),
                ["companyName"] = S(opportunity["companyName"]),
                ["studentId"] = studentId,
                ["studentUid"] = Uid,
                ["studentName"] = S(Profile["fullName"]),
                ["phone"] = phone,
                ["cvLink"] = cvLink,
                ["portfolioLink"] = portfolio,
                ["motivation"] = S(d["motivation"]).Trim(),
                ["reason"] = "",
                ["status"] = "pending",
                ["createdAt"] = FirebaseDb.Now
            };

            Db.Update("", new JObject
            {
                ["applications/" + id] = application,
                ["opportunities/" + opportunityId + "/applicantsCount"] = Int(opportunity["applicantsCount"]) + 1
            });
        }

        static bool IsUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https");
        }
    }
}