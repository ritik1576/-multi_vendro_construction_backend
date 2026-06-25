using System;

namespace InframartAPI_New.Helpers
{
    public static class TimezoneHelper
    {
        public static DateTime ConvertToIst(DateTime utcDateTime)
        {
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            try
            {
                var timeZoneId = OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata";
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTime(utc, tz);
            }
            catch
            {
                return utc.AddHours(5).AddMinutes(30);
            }
        }

        public static DateTime? ConvertToIst(DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue) return null;
            return ConvertToIst(utcDateTime.Value);
        }
    }
}
