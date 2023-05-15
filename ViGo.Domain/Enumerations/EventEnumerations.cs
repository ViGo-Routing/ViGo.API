using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum EventType : short
    {
        BOOKING_MAPPED = 0,
        BOOKING_PAID = 1,
        BOOKING_REFUND = 2,
        BOOKING_DETAIL_REFUND = 3,
        PROMOTION = 4,
        START_TRIP = 5,
        DRIVER_ARRIVED_AT_PICKUP = 6,
        PICKED_UP = 7,
        ARRIVED_AT_DROPOFF = 8,
        HAS_NEW_RATING = 9,
        HAS_TRIP_IN_DAY = 10,
        REFUND_COMPLETED = 11,
        NEARLY_BAN = 12,
        BAN_DRIVER = 13,
        HAS_TRIP_SUDDENLY = 14,
        TRIP_INCOME = 15,
        NEW_REPORT = 16,
        CANCELLED_BY_BOOKER = 17,
        SUBMIT_REGISTRATION_SUCCESSFULLY = 18,
        VERIFY_EMAIL = 19,
        REJECT_DRIVER_REGISTRATION = 20,
        VERIFY_PHONE = 21
    }

    public enum EventStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }
}
