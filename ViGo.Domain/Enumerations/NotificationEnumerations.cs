using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
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
        public static readonly string Schedule = "schedule";
        public static readonly string AvailableBookingDetails = "availableBookingDetails";
    }
}
