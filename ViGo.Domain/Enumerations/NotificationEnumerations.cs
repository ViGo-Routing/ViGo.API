﻿namespace ViGo.Domain.Enumerations
{
    public enum NotificationStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }

    public enum NotificationType : short
    {
        SPECIFIC_USER = 0,
        BOOKER = 1,
        DRIVER = 2,
        BOOKER_AND_DRIVER = 3,
        ADMIN = 4
    }

    public static class NotificationAction
    {
        public static readonly string TransactionDetail = "payment";
        public static readonly string BookingDetail = "bookingDetail";
        public static readonly string Booking = "booking";
        public static readonly string Profile = "profile";
        public static readonly string Login = "login";
        public static readonly string Schedule = "schedule";
        public static readonly string AvailableBookingDetails = "availableBookingDetails";
        public static readonly string Report = "report";
        public static readonly string Chat = "chat";
    }
}
