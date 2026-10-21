using System;
using System.Globalization;

namespace University_Internship_Management_System.Services
{
    public static class JordanTime
    {
        public const string Label = "Jordan Time (UTC+3)";
        private static TimeZoneInfo Zone
        {
            get
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman"); }
                catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time"); }
            }
        }
        public static DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        public static string Format(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone)
            .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        public static DateTime ParseInput(string date, string time)
        {
            DateTime local = DateTime.ParseExact(date + " " + time, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None);
            if (Zone.IsInvalidTime(local) || Zone.IsAmbiguousTime(local)) throw new ArgumentException("Choose an unambiguous Jordan time.");
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);
        }
        public static void RequireOpen(DateTime openingUtc, DateTime closingUtc)
        {
            if (openingUtc >= closingUtc || DateTime.UtcNow < openingUtc || DateTime.UtcNow >= closingUtc)
                throw new UnauthorizedAccessException("This action is outside its scheduled availability.");
        }
    }
}
