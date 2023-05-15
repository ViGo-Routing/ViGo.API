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

        public static DateTimeOffset GetDateTimeVnNow()
        {
            return TimeZoneInfo.ConvertTime(
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                TimeZoneInfo.FindSystemTimeZoneById(vnTimeZoneString));
        }
    }
}
