using ViGo.Models.FarePolicies;

namespace ViGo.Models.Fares
{
    public class FareUpdateModel
    {
        public Guid Id { get; set; }
        //public Guid VehicleTypeId { get; set; }
        public double? BaseDistance { get; set; }
        public double? BasePrice { get; set; }

        public IList<FarePolicyListItemModel>? FarePolicies { get; set; }
    }
}
