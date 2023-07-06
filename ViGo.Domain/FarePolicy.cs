using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ViGo.Domain
{
    public partial class FarePolicy
    {
        public override Guid Id { get; set; }
        public Guid FareId { get; set; }
        public double MinDistance { get; set; }
        public double? MaxDistance { get; set; }
        public double PricePerKm { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual Fare Fare { get; set; } = null!;
    }
}
