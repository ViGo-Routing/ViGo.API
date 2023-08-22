using Newtonsoft.Json;

namespace ViGo.Domain
{
    public partial class Fare
    {
        public Fare()
        {
            FarePolicies = new HashSet<FarePolicy>();
        }

        public override Guid Id { get; set; }
        public Guid VehicleTypeId { get; set; }
        public double BaseDistance { get; set; }
        public double BasePrice { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual VehicleType VehicleType { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<FarePolicy> FarePolicies { get; set; }
    }
}
