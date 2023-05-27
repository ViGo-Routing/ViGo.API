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

        public override Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public Guid? DriverId { get; set; }
        public Guid CustomerRouteId { get; set; }
        public Guid? DriverRouteId { get; set; }
        public DateTime? AssignedTime { get; set; }
        public DateTime? Date { get; set; }
        public double? Price { get; set; }
        public double? PriceAfterDiscount { get; set; }
        public double? DriverWage { get; set; }
        public TimeSpan? BeginTime { get; set; }
        public DateTime? ArriveAtPickupTime { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
        public short? Rate { get; set; }
        public string? Feedback { get; set; }
        public BookingDetailStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }

        public virtual Booking Booking { get; set; } = null!;
        public virtual User? Driver { get; set; }
        public virtual Route CustomerRoute { get; set; }
        public virtual Route? DriverRoute { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
