using Newtonsoft.Json.Linq;
using System.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Company
{
    // Company: review applications and accept or reject them (Firebase: /applications)
    public partial class ApplicationsReview : InternshipPage
    {
        protected override string Role { get { return "companySupervisor"; } }

        protected override JObject BuildView()
        {
            return UimsData.CompanyView(Db, Uid, Profile);
        }

        protected override void Handle(string action, JObject data)
        {
            if (action == "decision") Decide(data);
            else base.Handle(action, data);
        }

        void Decide(JObject d)
        {
            string id = FirebaseDb.Key(S(d["id"]));
            string status = S(d["status"]);
            string reason = S(d["reason"]).Trim();
            if (status != "accepted" && status != "rejected") throw new AppError("Unknown decision.");
            if (status == "rejected" && reason.Length < 3) throw new AppError("Write a reason for the student.");

            var application = Db.GetObject("applications/" + id);
            if (application == null || S(application["companyId"]) != S(Profile["companyId"]))
                throw new AppError("Application not found.");
            if (S(application["status"]) != "pending")
                throw new AppError("This application has already been decided.");

            var updates = new JObject
            {
                ["applications/" + id + "/status"] = status,
                ["applications/" + id + "/reason"] = reason,
                ["applications/" + id + "/decidedAt"] = FirebaseDb.Now,
                ["applications/" + id + "/companySupervisorId"] = S(Profile["companySupervisorId"])
            };

            if (status == "accepted")
            {
                string opportunityId = FirebaseDb.Key(S(application["opportunityId"]));
                var opportunity = Db.GetObject("opportunities/" + opportunityId);
                if (opportunity == null) throw new AppError("The opportunity no longer exists.");

                var acceptedApplication = (JObject)application.DeepClone();
                acceptedApplication["decidedAt"] = updates["applications/" + id + "/decidedAt"];
                foreach (var field in UIMS.Attendance.AttendanceOnboarding.TrainingPeriod(acceptedApplication, opportunity).Properties())
                    updates["applications/" + id + "/" + field.Name] = field.Value.DeepClone();

                int accepted = Db.List("applications").Count(a => S(a["opportunityId"]) == opportunityId &&
                    (S(a["status"]) == "accepted" || S(a["status"]) == "confirmed"));
                if (accepted >= Int(opportunity["seats"]))
                    throw new AppError("All seats for this opportunity are filled.");

                updates["opportunities/" + opportunityId + "/acceptedCount"] = accepted + 1;
                updates["opportunities/" + opportunityId + "/acceptedApplications/" + id] = true;
            }

            Db.Update("", updates); // one atomic write
        }
    }
}