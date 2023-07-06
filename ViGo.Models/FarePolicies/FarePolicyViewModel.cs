using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;

namespace ViGo.Models.FarePolicies
{
    public class FarePolicyViewModel
    {
        public Guid Id { get; set; }
        public Guid FareId { get; set; }
        public double MinDistance { get; set; }
        public double? MaxDistance { get; set; }
        public double PricePerKm { get; set; }
        //public DateTime CreatedTime { get; set; }
        //public Guid CreatedBy { get; set; }
        //public DateTime UpdatedTime { get; set; }
        //public Guid UpdatedBy { get; set; }
        //public bool IsDeleted { get; set; }

        public FarePolicyViewModel(FarePolicy farePolicy)
        {
            Id = farePolicy.Id;
            FareId = farePolicy.FareId;
            MinDistance = farePolicy.MinDistance;
            MaxDistance = farePolicy.MaxDistance;
            PricePerKm = farePolicy.PricePerKm;
        }

    }

    public class FarePolicyListItemModel
    {
        public double MinDistance { get; set; }
        public double? MaxDistance { get; set; }
        public double PricePerKm { get; set; }

        //public FarePolicyListItemModel
    }
}
