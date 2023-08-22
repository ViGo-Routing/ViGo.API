namespace ViGo.Models.FarePolicies
{
    public class FarePolicyCreateModel
    {
        public double MinDistance { get; set; }
        public double? MaxDistance { get; set; }
        public double PricePerKm { get; set; }
    }
}
