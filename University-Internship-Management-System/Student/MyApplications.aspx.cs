using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Student
{
    // Student: follow applications and confirm one accepted offer (creates a placement)
    public partial class MyApplications : InternshipPage
    {
        protected override string Role { get { return "student"; } }

        protected override JObject BuildView()
        {
            return UimsData.StudentView(Db, Uid, Profile);
        }

        protected override void Handle(string action, JObject data)
        {
            if (action == "confirmOffer") Confirm(data);
            else base.Handle(action, data);
        }

        void Confirm(JObject d)
        {
            string id = FirebaseDb.Key(S(d["id"]));
            string studentId = FirebaseDb.Key(S(Profile["studentId"]));

            var application = Db.GetObject("applications/" + id);
            if (application == null || !UimsData.OwnsStudentRecord(application, Uid, studentId))
                throw new AppError("Application not found.");
            if (S(application["status"]) != "accepted")
                throw new AppError("Only an accepted offer can be confirmed.");
            // A leftover claim alone is not a training placement. Check the actual
            // owned placements, including legacy records, before blocking confirmation.
            var existingPlacements = Db.List("placements").Where(p => UimsData.OwnsStudentRecord(p, Uid, studentId));
            if (existingPlacements.Any(p => !new[] { "cancelled", "canceled", "withdrawn", "rejected", "deleted" }
                .Contains(S(p["status"]).ToLowerInvariant())))
                throw new AppError("You already have a training placement. Open your training details or contact the coordinator if it is no longer active.");

            string opportunityId = FirebaseDb.Key(S(application["opportunityId"]));
            var opportunity = Db.GetObject("opportunities/" + opportunityId) ?? new JObject();
            string supervisorId = S(application["companySupervisorId"]);
            string placementId = "PLC-" + id;
            string now = FirebaseDb.Now;

            var updates = new JObject
            {
                ["placements/" + placementId] = new JObject
                {
                    ["id"] = placementId,
                    ["applicationId"] = id,
                    ["opportunityId"] = opportunityId,
                    ["companyId"] = S(application["companyId"]),
                    ["companySupervisorId"] = supervisorId,
                    ["supervisorId"] = supervisorId,
                    ["academicSupervisorId"] = S(Profile["academicSupervisorId"]),
                    ["studentId"] = studentId,
                    ["studentProfileId"] = studentId,
                    ["studentUid"] = Uid,
                    ["requiredHours"] = Int(opportunity["requiredHours"]),
                    ["durationWeeks"] = Int(opportunity["durationWeeks"]),
                    ["startDate"] = S(opportunity["startDate"]),
                    ["status"] = "active",
                    ["createdAt"] = now
                },
                ["placementClaims/" + studentId] = new JObject
                {
                    ["applicationId"] = id,
                    ["placementId"] = placementId,
                    ["studentId"] = studentId,
                    ["studentUid"] = Uid,
                    ["claimedAt"] = now
                },
                ["applications/" + id + "/status"] = "confirmed",
                ["applications/" + id + "/confirmedAt"] = now,
                ["applications/" + id + "/placementId"] = placementId
            };

            // The student's other accepted offers: release their seats for the other companies
            var others = Db.List("applications").Where(a => UimsData.OwnsStudentRecord(a, Uid, studentId)
                && S(a["id"]) != id && S(a["status"]) == "accepted");
            foreach (var other in others)
            {
                string otherId = FirebaseDb.Key(S(other["id"]));
                string otherOpportunity = FirebaseDb.Key(S(other["opportunityId"]));
                var op = Db.GetObject("opportunities/" + otherOpportunity) ?? new JObject();
                updates["applications/" + otherId + "/status"] = "withdrawn";
                updates["applications/" + otherId + "/reason"] = "The student confirmed another internship offer.";
                updates["opportunities/" + otherOpportunity + "/acceptedApplications/" + otherId] = JValue.CreateNull();
                updates["opportunities/" + otherOpportunity + "/acceptedCount"] = Math.Max(0, Int(op["acceptedCount"]) - 1);
            }

            Db.Update("", updates); // one atomic write
        }
    }
}
