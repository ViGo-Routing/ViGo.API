namespace ViGo.Domain.Enumerations
{
    public enum BookingStatus : short
    {
        //UNPAID = -2,
        //PENDING_MAPPING = -1,
        //STARTED = 0,
        //COMPLETED = 1,
        //CANCELLED_BY_BOOKER = 2,
        //PENDING_REFUND = 3,
        //COMPLETED_REFUND = 4
        DRAFT = 0,
        CONFIRMED = 1,
        COMPLETED = 2,
        CANCELED_BY_BOOKER = -1
    }

    public enum BookingDetailStatus : short
    {
        ASSIGNED = 0,
        GOING_TO_PICKUP = 5,
        ARRIVE_AT_PICKUP = 1,
        GOING_TO_DROPOFF = 6,
        ARRIVE_AT_DROPOFF = 3,
        CANCELLED = -1,
        PENDING_ASSIGN = -2,
        //PENDING_REFUND = 4,
        //COMPLETED_REFUND = 5
        PENDING_PAID = -3,
        COMPLETED = 4
    }

    public enum BookingType : short
    {
        ONE_WAY = 0,
        ROUND_TRIP = 1
    }

    public enum BookingDetailType : short
    {
        MAIN_ROUTE = 0,
        ROUND_TRIP_ROUTE = 1
    }
}
