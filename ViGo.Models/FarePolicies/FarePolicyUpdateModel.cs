using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.FarePolicies
{
    public class FarePolicyUpdateModel
    {
        public Guid Id { get; set; }
        public double PricePerKm { get; set; }
    }
}
