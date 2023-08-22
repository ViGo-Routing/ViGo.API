using Newtonsoft.Json;
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

        public override Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public short Slot { get; set; }
        public VehicleSubType Type { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual ICollection<Booking> Bookings { get; set; }
        [JsonIgnore]
        public virtual ICollection<Fare> Fares { get; set; }
        [JsonIgnore]
        public virtual ICollection<Promotion> Promotions { get; set; }
        [JsonIgnore]
        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
