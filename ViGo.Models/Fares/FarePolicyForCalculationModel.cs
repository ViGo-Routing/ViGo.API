using ViGo.Domain;

namespace ViGo.Models.Fares
{
    public class FareForCalculationModel
    {
        public double MinimumBaseDistance { get; set; }

        public double MinimumBasePrice { get; set; }

        public IList<FarePolicyForCalculationModel>
            FarePolicies
        { get; set; }

        public FareForCalculationModel(Fare fare,
            IList<FarePolicy> farePolicies)
        {
            MinimumBaseDistance = fare.BaseDistance;
            MinimumBasePrice = fare.BasePrice;

            FarePolicies = (from policy in farePolicies.OrderBy(p => p.MinDistance)
                            select new FarePolicyForCalculationModel(policy))
                           .ToList();
        }

    }

    public class FarePolicyForCalculationModel
    {
        public double? MinDistanceBoundary { get; set; }
        public double? MaxDistanceBoundary { get; set; }
        public double? PricePerKm { get; set; }

        public FarePolicyForCalculationModel(FarePolicy farePolicy)
        {
            MinDistanceBoundary = farePolicy.MinDistance;
            MaxDistanceBoundary = farePolicy.MaxDistance;
            PricePerKm = farePolicy.PricePerKm;
        }
    }
}
