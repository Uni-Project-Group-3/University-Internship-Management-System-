using System;
using System.Collections.Generic;
using System.Linq;
using UIMS.Models;

namespace UIMS.Services
{
    /// <summary>
    /// Data access for internship opportunities.
    ///
    /// For now the data lives in memory (it resets when the site restarts)
    /// so the page can be built and demoed before Firebase is wired up.
    /// To switch to Firebase, keep these method names and replace the body
    /// of each method with the matching Firebase call — the page itself
    /// does not need to change.
    /// </summary>
    public static class OpportunityService
    {
        private static readonly object _lock = new object();
        private static readonly List<Opportunity> _items = Seed();

        public static List<Opportunity> GetByCompany(string companyId)
        {
            lock (_lock)
            {
                return _items.Where(o => o.CompanyId == companyId)
                             .OrderByDescending(o => o.CreatedAt)
                             .ToList();
            }
        }

        public static Opportunity GetById(string id)
        {
            lock (_lock)
            {
                return _items.FirstOrDefault(o => o.Id == id);
            }
        }

        public static void Add(Opportunity opp)
        {
            lock (_lock)
            {
                opp.Id = Guid.NewGuid().ToString("N");
                opp.CreatedAt = DateTime.UtcNow;
                opp.Status = OpportunityStatus.Open;
                _items.Add(opp);
            }
        }

        public static void Update(Opportunity opp)
        {
            lock (_lock)
            {
                int i = _items.FindIndex(o => o.Id == opp.Id);
                if (i >= 0) _items[i] = opp;
            }
        }

        public static void SetStatus(string id, string status)
        {
            lock (_lock)
            {
                var opp = _items.FirstOrDefault(o => o.Id == id);
                if (opp != null) opp.Status = status;
            }
        }

        // ---------- sample data ----------
        private static List<Opportunity> Seed()
        {
            const string company = "orange-jo";
            const string name = "Orange Jordan";
            DateTime today = University_Internship_Management_System.Services.JordanTime.LocalNow.Date;

            return new List<Opportunity>
            {
                new Opportunity
                {
                    Id = "opp1", CompanyId = company, CompanyName = name,
                    Title = "Frontend Developer Intern",
                    Description = "Build and polish customer-facing web pages for Orange Money using modern JavaScript.",
                    Requirements = "HTML, CSS, JavaScript; basic Git; a portfolio or GitHub link.",
                    Majors = new List<string> { "Computer Science", "Software Engineering" },
                    Seats = 4, AcceptedCount = 2, ApplicantsCount = 11,
                    Location = "Amman", WorkMode = "Hybrid",
                    StartDate = today.AddDays(20), DurationWeeks = 8, Deadline = today.AddDays(9),
                    CreatedAt = today.AddDays(-6)
                },
                new Opportunity
                {
                    Id = "opp2", CompanyId = company, CompanyName = name,
                    Title = "Network Operations Intern",
                    Description = "Support the NOC team in monitoring, troubleshooting, and documenting the core network.",
                    Requirements = "Networking fundamentals (CCNA level), Linux basics.",
                    Majors = new List<string> { "Network Engineering & Security", "Computer Engineering" },
                    Seats = 3, AcceptedCount = 3, ApplicantsCount = 7,
                    Location = "Amman", WorkMode = "On-site",
                    StartDate = today.AddDays(12), DurationWeeks = 8, Deadline = today.AddDays(2),
                    CreatedAt = today.AddDays(-14)
                },
                new Opportunity
                {
                    Id = "opp3", CompanyId = company, CompanyName = name,
                    Title = "Data Analyst Intern",
                    Description = "Analyze customer usage data and build Power BI dashboards for the marketing team.",
                    Requirements = "SQL, Excel; Python or Power BI is a plus.",
                    Majors = new List<string> { "Data Science & AI", "Computer Information Systems" },
                    Seats = 2, AcceptedCount = 0, ApplicantsCount = 5,
                    Location = "Irbid", WorkMode = "Remote",
                    StartDate = today.AddDays(30), DurationWeeks = 10, Deadline = today.AddDays(15),
                    CreatedAt = today.AddDays(-2)
                },
                new Opportunity
                {
                    Id = "opp4", CompanyId = company, CompanyName = name,
                    Title = "Cybersecurity Intern",
                    Description = "Assist the SOC team with log review, phishing analysis, and security awareness material.",
                    Requirements = "Security fundamentals, Linux, curiosity for incident response.",
                    Majors = new List<string> { "Cybersecurity" },
                    Seats = 2, AcceptedCount = 2, ApplicantsCount = 9,
                    Location = "Amman", WorkMode = "On-site",
                    StartDate = today.AddDays(-20), DurationWeeks = 8, Deadline = today.AddDays(-30),
                    Status = OpportunityStatus.Closed,
                    CreatedAt = today.AddDays(-40)
                }
            };
        }
    }
}
