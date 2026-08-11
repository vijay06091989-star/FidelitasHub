using FidelitasHub.Models;

namespace FidelitasHub.Helpers
{
    public static class ShiftTimeHelper
    {
        private const string EasternTimeZoneId = "Eastern Standard Time";

        //==================================================
        // CHECK WHETHER US EASTERN DST IS ACTIVE
        // FOR A PARTICULAR ATTENDANCE DATE
        //==================================================
        public static bool IsDstActive(DateTime attendanceDate)
        {
            try
            {
                var easternTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        EasternTimeZoneId);

                // Noon is deliberately used so that we are
                // safely away from the 2:00 AM DST transition.
                var easternDate =
                    DateTime.SpecifyKind(
                        attendanceDate.Date.AddHours(12),
                        DateTimeKind.Unspecified);

                return easternTimeZone.IsDaylightSavingTime(
                    easternDate);
            }
            catch
            {
                // If the Windows timezone cannot be found,
                // safely fall back to standard time.
                return false;
            }
        }

        //==================================================
        // GET APPLICABLE SHIFT START TIME
        //==================================================
        public static TimeSpan GetStartTime(
            Shift shift,
            DateTime attendanceDate)
        {
            if (shift.DstApplicable &&
                shift.DstStartTime.HasValue &&
                IsDstActive(attendanceDate))
            {
                return shift.DstStartTime.Value;
            }

            return shift.StandardStartTime;
        }

        //==================================================
        // GET APPLICABLE SHIFT END TIME
        //==================================================
        public static TimeSpan GetEndTime(
            Shift shift,
            DateTime attendanceDate)
        {
            if (shift.DstApplicable &&
                shift.DstEndTime.HasValue &&
                IsDstActive(attendanceDate))
            {
                return shift.DstEndTime.Value;
            }

            return shift.StandardEndTime;
        }
    }
}