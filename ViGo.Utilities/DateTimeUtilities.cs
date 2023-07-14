using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Utilities
{
    public static class DateTimeUtilities
    {
        private static string vnTimeZoneString = "SE Asia Standard Time";

        public static DateTime GetDateTimeVnNow()
        {
            return TimeZoneInfo.ConvertTime(
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                TimeZoneInfo.FindSystemTimeZoneById(vnTimeZoneString));
        }

        public static DateTime ToDateTime(DateOnly date, TimeOnly time)
        {
            return date.ToDateTime(time);
        }

        public static TimeSpan CalculateTripEndTime(TimeSpan beginTime, double duration)
        {
            return beginTime.Add(new TimeSpan(0, (int)Math.Ceiling(duration), 0));
        }

        public static long GetTimeStamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        public static long GetTimeStamp()
        {
            return GetTimeStamp(GetDateTimeVnNow());
        }
    }
}
