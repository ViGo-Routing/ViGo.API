namespace ViGo.Models.Bookings
{
    public class BookingCreateModel
    {
        //public override Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        //public Guid StartRouteStationId { get; set; }
        //public Guid EndRouteStationId { get; set; }
        public Guid CustomerRouteId { get; set; }
        //public TimeSpan? StartTime { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? DaysOfWeek { get; set; }
        //public TimeOnly CustomerDesiredPickupTime { get; set; }
        public double TotalPrice { get; set; }
        public double PriceAfterDiscount { get; set; }
        //public PaymentMethod PaymentMethod { get; set; }
        public bool IsShared { get; set; }
        public double Duration { get; set; }
        public double Distance { get; set; }
        //public Guid? PromotionId { get; set; }
        public Guid VehicleTypeId { get; set; }
        //public BookingType Type { get; set; } = BookingType.ONE_WAY;
        public double? RoundTripTotalPrice { get; set; }
        //public TimeOnly? CustomerRoundTripDesiredPickupTime { get; set; } = null;
        //public BookingStatus Status { get; set; } = BookingStatus.UNPAID;
    }
}
