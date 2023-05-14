using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Booking
    {
        public Booking()
        {
            BookingDetails = new HashSet<BookingDetail>();
            WalletTransactions = new HashSet<WalletTransaction>();
        }

        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public Guid StartRouteStationId { get; set; }
        public Guid EndRouteStationId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? DaysOfWeek { get; set; }
        public double? TotalPrice { get; set; }
        public double? PriceAfterDiscount { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public bool IsShared { get; set; }
        public double? Duration { get; set; }
        public double? Distance { get; set; }
        public Guid? PromotionId { get; set; }
        public Guid VehicleTypeId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
        public virtual RouteStation EndRouteStation { get; set; } = null!;
        public virtual Promotion? Promotion { get; set; }
        public virtual RouteStation StartRouteStation { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual VehicleType VehicleType { get; set; } = null!;
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
