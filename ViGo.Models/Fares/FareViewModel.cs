using ViGo.Domain;
using ViGo.Models.FarePolicies;
using ViGo.Models.VehicleTypes;

namespace ViGo.Models.Fares
{
    public class FareViewModel
    {
        public Guid Id { get; set; }
        public Guid VehicleTypeId { get; set; }
        public double BaseDistance { get; set; }
        public double BasePrice { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public VehicleTypeViewModel VehicleType { get; set; }
        public IEnumerable<FarePolicyViewModel>? FarePolicies { get; set; }

        public FareViewModel(Fare fare, VehicleTypeViewModel vehicleType)
        {
            Id = fare.Id;
            VehicleTypeId = fare.VehicleTypeId;
            BaseDistance = fare.BaseDistance;
            BasePrice = fare.BasePrice;
            CreatedTime = fare.CreatedTime;
            CreatedBy = fare.CreatedBy;
            UpdatedTime = fare.UpdatedTime;
            UpdatedBy = fare.UpdatedBy;
            IsDeleted = fare.IsDeleted;
            VehicleType = vehicleType;
        }

        public FareViewModel(Fare fare, VehicleTypeViewModel vehicleType,
            IEnumerable<FarePolicyViewModel>? farePolicies)
            : this(fare, vehicleType)
        {
            FarePolicies = farePolicies;
        }
    }
}
