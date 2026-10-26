using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Company
{
    // Company: add, edit and close opportunities (Firebase: /opportunities)
    public partial class ManageOpportunities : InternshipPage
    {
        protected override string Role { get { return "companySupervisor"; } }
        string CompanyId { get { return S(Profile["companyId"]); } }

        protected override JObject BuildView()
        {
            return UimsData.CompanyView(Db, Uid, Profile);
        }

        protected override void Handle(string action, JObject data)
        {
            if (action == "opportunity") Save(data);
            else if (action == "closeOpportunity") Close(S(data["id"]));
            else base.Handle(action, data);
        }

        // ================= Add / Edit =================
        void Save(JObject d)
        {
            string title = S(d["title"]).Trim();
            string description = S(d["description"]).Trim();
            string location = S(d["location"]).Trim();
            // One or more specializations; older clients still send a single "department".
            var departments = ((d["departments"] as JArray) ?? new JArray(S(d["department"])))
                .Select(x => S(x).Trim()).Where(x => x != "").Distinct().Take(20).ToList();
            if (title == "" || description == "" || location == "" || departments.Count == 0)
                throw new AppError("Title, description, specialization and location are required.");

            int seats, duration, hours;
            DateTime start, deadline;
            if (!int.TryParse(S(d["seats"]), out seats) || seats < 1 || seats > 50 ||
                !int.TryParse(S(d["duration"]), out duration) || duration < 1 || duration > 52 ||
                !int.TryParse(S(d["requiredHours"]), out hours) || hours < 1 ||
                !DateTime.TryParseExact(S(d["start"]), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start) ||
                !DateTime.TryParseExact(S(d["deadline"]), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out deadline))
                throw new AppError("Enter valid dates, seats, duration and required training hours.");
            if (deadline > start)
                throw new AppError("The application deadline must be on or before the start date.");

            var fields = new JObject
            {
                ["title"] = title,
                ["description"] = description,
                ["requirements"] = S(d["requirements"]).Trim(),
                ["majors"] = new JArray(departments),
                ["location"] = location,
                ["workMode"] = S(d["workMode"]) == "" ? "On-site" : S(d["workMode"]),
                ["seats"] = seats,
                ["durationWeeks"] = duration,
                ["requiredHours"] = hours,
                ["startDate"] = S(d["start"]),
                ["deadline"] = S(d["deadline"]),
                ["updatedAt"] = FirebaseDb.Now
            };

            UIMS.Attendance.AttendanceOnboarding.ApplyTrainingDefaults(fields);
            string id = S(d["id"]);
            if (id == "")
            {
                if (string.CompareOrdinal(S(d["deadline"]), UimsData.Today) < 0)
                    throw new AppError("The application deadline cannot be in the past.");
                id = FirebaseDb.NewId("OP");
                fields["id"] = id;
                fields["companyId"] = CompanyId;
                fields["companyName"] = S(Profile["companyName"]);
                fields["createdAt"] = FirebaseDb.Now;
                fields["status"] = "published";
                fields["acceptedCount"] = 0;
                fields["applicantsCount"] = 0;
                Db.Set("opportunities/" + id, fields);
            }
            else
            {
                var current = Own(id);
                if (seats < Int(current["acceptedCount"]))
                    throw new AppError("Seats cannot be fewer than the students already accepted.");
                Db.Update("opportunities/" + id, fields);
            }
        }

        // ================= Close =================
        void Close(string id)
        {
            Own(id);
            Db.Update("opportunities/" + id, new JObject { ["status"] = "closed", ["closedAt"] = FirebaseDb.Now });
        }

        // The opportunity must belong to this company
        JObject Own(string id)
        {
            var opportunity = Db.GetObject("opportunities/" + FirebaseDb.Key(id));
            if (opportunity == null || S(opportunity["companyId"]) != CompanyId)
                throw new AppError("Opportunity not found.");
            return opportunity;
        }
    }
}
