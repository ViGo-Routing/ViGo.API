using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class VehicleType
    {
        public VehicleType()
        {
            Bookings = new HashSet<Booking>();
            Fares = new HashSet<Fare>();
            Promotions = new HashSet<Promotion>();
            Vehicles = new HashSet<Vehicle>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public short Slot { get; set; }
        public VehicleSubType Type { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Fare> Fares { get; set; }
        public virtual ICollection<Promotion> Promotions { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
