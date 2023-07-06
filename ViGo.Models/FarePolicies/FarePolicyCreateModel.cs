using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.FarePolicies
{
    public class FarePolicyCreateModel
    {
        public double MinDistance { get; set; }
        public double? MaxDistance { get; set; }
        public double PricePerKm { get; set; }
    }
}
