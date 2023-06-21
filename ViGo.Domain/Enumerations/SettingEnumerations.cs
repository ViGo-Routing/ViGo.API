using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum SettingType : short
    {
        DEFAULT = 0,
        TRIP = 1,
        PENALTY = 2,
        ROUTE_ROUTINE = 3,
        PRICING = 4
    }

    public enum SettingDataType : short
    {
        DEFAULT = 0,
        INTEGER = 1,
        DOUBLE = 2,
        TIME = 3
    }

    public enum SettingDataUnit : short
    {
        DEFAULT = 0,
        PERCENT = 1,
        MINUTES = 2,
        HOURS = 3,
        DAYS = 4,
        METERS = 5,
        KILOMETERS = 6,
        TURN = 7,
        TIME = 8,
        MB = 9
    }

    public static class SettingKeys
    {
        public static string NightTripExtraFeeCar = "NightTripExtraFeeCar";
        public static string NightTripExtraFeeBike = "NightTripExtraFeeBike";

        public static string TicketsDiscount_10 = "10TicketsDiscount";
        public static string TicketsDiscount_25 = "25TicketsDiscount";
        public static string TicketsDiscount_50 = "50TicketsDiscount";

        public static string WeeklyTicketsDiscount_2 = "2WeeklyTicketsDiscount";
        public static string MonthlyTicketsDiscount_2 = "2MonthlyTicketsDiscount";
        public static string QuarterlyTicketsDiscount_2 = "2QuarterlyTicketsDiscount";

        public static string DriverWagePercent = "DriverWagePercent";
    }
}
