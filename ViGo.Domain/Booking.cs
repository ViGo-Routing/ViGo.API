using Newtonsoft.Json;
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

        public override Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        //public Guid StartRouteStationId { get; set; }
        //public Guid EndRouteStationId { get; set; }

        public Guid CustomerRouteId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
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
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual User Customer { get; set; } = null!;
        //[JsonIgnore]
        //public virtual RouteStation EndRouteStation { get; set; } = null!;
        [JsonIgnore]
        public virtual Promotion? Promotion { get; set; }
        //[JsonIgnore]
        //public virtual RouteStation StartRouteStation { get; set; } = null!;
        [JsonIgnore]
        public virtual Route CustomerRoute { get; set; } = null!;

        [JsonIgnore]
        public virtual VehicleType VehicleType { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        [JsonIgnore]
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
