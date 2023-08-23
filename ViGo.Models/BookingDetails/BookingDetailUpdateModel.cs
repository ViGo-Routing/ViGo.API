using ViGo.Domain.Enumerations;

namespace ViGo.Models.BookingDetails
{
    public class BookingDetailUpdateModel
    {
    }

    public class BookingDetailUpdateStatusModel
    {
        public Guid BookingDetailId { get; set; }
        public BookingDetailStatus Status { get; set; }
        public DateTime? Time { get; set; }
    }

    public class BookingDetailAssignDriverModel
    {
        public Guid BookingDetailId { get; set; }
        public Guid DriverId { get; set; }

    }
}
