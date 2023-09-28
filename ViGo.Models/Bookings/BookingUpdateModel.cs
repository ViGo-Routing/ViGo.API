namespace ViGo.Models.Bookings
{
    public class BookingUpdateModel
    {
        public Guid BookingId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? DaysOfWeek { get; set; }
        public double TotalPrice { get; set; }
        public double AdditionalFare { get; set; }
        public double PriceAfterDiscount { get; set; }
        public bool IsShared { get; set; }
        public double Duration { get; set; }
        public double Distance { get; set; }
        //public Guid? PromotionId { get; set; }
        //public Guid VehicleTypeId { get; set; }
        public double? RoundTripTotalPrice { get; set; }
        public double? RoundTripAdditionalFare { get; set; }
    }
}
