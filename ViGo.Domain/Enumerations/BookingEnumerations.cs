using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum BookingStatus : short
    {
        UNPAID = -2,
        PENDING_MAPPING = -1,
        STARTED = 0,
        COMPLETED = 1,
        CANCELLED_BY_BOOKER = 2,
        PENDING_REFUND = 3,
        COMPLETED_REFUND = 4
    }

    public enum BookingDetailStatus : short
    {
        ASSIGNED = 0,
        ARRIVE_AT_PICKUP = 1,
        GOING = 2,
        ARRIVE_AT_DROPOFF = 3,
        CANCELLED = -1,
        PENDING = -2,
        PENDING_REFUND = 4,
        COMPLETED_REFUND = 5
    }
}
