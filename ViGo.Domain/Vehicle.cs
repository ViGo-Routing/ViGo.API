using System;
using System.Collections.Generic;

namespace ViGo.Domain
{
    public partial class Vehicle
    {
        public override Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public Guid VehicleTypeId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual VehicleType VehicleType { get; set; } = null!;
    }
}
