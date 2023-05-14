using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class BookingDetail
    {
        public BookingDetail()
        {
            Reports = new HashSet<Report>();
            WalletTransactions = new HashSet<WalletTransaction>();
        }

        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public Guid? DriverId { get; set; }
        public Guid CustomerRouteId { get; set; }
        public Guid? DriverRouteId { get; set; }
        public DateTimeOffset? AssignedTime { get; set; }
        public DateTime? Date { get; set; }
        public double? Price { get; set; }
        public double? PriceAfterDiscount { get; set; }
        public double? DriverWage { get; set; }
        public DateTimeOffset? BeginTime { get; set; }
        public DateTimeOffset? ArriveAtPickupTime { get; set; }
        public DateTimeOffset? PickupTime { get; set; }
        public DateTimeOffset? DropoffTime { get; set; }
        public short? Rate { get; set; }
        public string? Feedback { get; set; }
        public BookingDetailStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }

        public virtual Booking Booking { get; set; } = null!;
        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual Route CustomerRoute { get; set; } = null!;
        public virtual User? Driver { get; set; }
        public virtual Route? DriverRoute { get; set; }
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual ICollection<Report> Reports { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
