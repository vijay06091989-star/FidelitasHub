using System;

namespace FidelitasHub.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime GetIST()
        {
            TimeZoneInfo istZone;

            try
            {
                // Windows
                istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                // Linux
                istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
        }
    }
}