using System;
using System.Collections.Generic;

namespace UIMS.Models
{
    /// <summary>
    /// An internship opportunity posted by a company supervisor.
    /// Field names match the Firebase document so the same class can be
    /// serialized to / from the database later.
    /// </summary>
    public class Opportunity
    {
        public string Id { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }

        /// <summary>Majors (specializations) allowed to see and apply.</summary>
        public List<string> Majors { get; set; } = new List<string>();

        public int Seats { get; set; }
        public int AcceptedCount { get; set; }
        public int ApplicantsCount { get; set; }

        public string Location { get; set; }
        public string WorkMode { get; set; }          // On-site | Hybrid | Remote

        public DateTime StartDate { get; set; }
        public int DurationWeeks { get; set; }
        public int RequiredHours { get; set; }
        public DateTime Deadline { get; set; }

        public string Status { get; set; } = OpportunityStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int SeatsLeft => Math.Max(0, Seats - AcceptedCount);
        public bool IsOpen => string.Equals(Status, OpportunityStatus.Open, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(Status, "published", StringComparison.OrdinalIgnoreCase);
    }

    public static class OpportunityStatus
    {
        public const string Open = "Open";
        public const string Closed = "Closed";
    }
}
