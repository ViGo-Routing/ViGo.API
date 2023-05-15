using System;
using System.Collections.Generic;

namespace ViGo.Domain
{
    public partial class FarePolicy
    {
        public override Guid Id { get; set; }
        public Guid FareId { get; set; }
        public double? MinDistance { get; set; }
        public double? MaxDistance { get; set; }
        public double? PricePerKm { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual Fare Fare { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
    }
}
