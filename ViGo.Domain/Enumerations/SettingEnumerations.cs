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
        //public static string NightTripExtraFeeCar_Key = "NightTripExtraFeeCar";
        //public static string NightTripExtraFeeCar_Description = "Phụ phí ban đêm - Xe hơi";
        public static string NightTripExtraFeeBike_Key = "NightTripExtraFeeBike";
        public static string NightTripExtraFeeBike_Description = "Phụ phí ban đêm - Xe máy";

        //public static string TicketsDiscount_10_Key = "10TicketsDiscount";
        //public static string TicketsDiscount_10_Description = "Giảm giá cho chuyến đi từ 10 cuốc trở lên";
        //public static string TicketsDiscount_25_Key = "25TicketsDiscount";
        //public static string TicketsDiscount_25_Description = "Giảm giá cho chuyến đi từ 25 cuốc trở lên";
        //public static string TicketsDiscount_50_Key = "50TicketsDiscount";
        //public static string TicketsDiscount_50_Description = "Giảm giá cho chuyến đi từ 50 cuốc trở lên";

        public static string WeeklyTicketsDiscount_2_Key = "2WeeklyTicketsDiscount";
        public static string WeeklyTicketsDiscount_5_Key = "5WeeklyTicketsDiscount";
        public static string WeeklyTicketsDiscount_2_Description = "Giảm giá cho hành trình từ 2 tuần trở lên";
        public static string WeeklyTicketsDiscount_5_Description = "Giảm giá cho hành trình từ 5 tuần trở lên";
        public static string MonthlyTicketsDiscount_2_Key = "2MonthlyTicketsDiscount";
        public static string MonthlyTicketsDiscount_4_Key = "4MonthlyTicketsDiscount";
        public static string MonthlyTicketsDiscount_6_Key = "6MonthlyTicketsDiscount";
        public static string MonthlyTicketsDiscount_2_Description = "Giảm giá cho hành trình từ 2 tháng trở lên";
        public static string MonthlyTicketsDiscount_4_Description = "Giảm giá cho hành trình từ 4 tháng trở lên";
        public static string MonthlyTicketsDiscount_6_Description = "Giảm giá cho hành trình từ 6 tháng trở lên";
        //public static string QuarterlyTicketsDiscount_2_Key = "2QuarterlyTicketsDiscount";
        //public static string QuarterlyTicketsDiscount_2_Description = "Giảm giá cho hành trình từ 2 quý trở lên";

        public static string DriverWagePercent_Key = "DriverWagePercent";
        public static string DriverWagePercent_Description = "Phần trăm chiết khấu dành cho tài xế";
        public static string TripMustStartBefore_Key = "TripMustStartBefore";
        public static string TripMustStartBefore_Description = "Thời gian sớm nhất mà tài xế có thể bắt đầu chuyến đi";
    }
}
