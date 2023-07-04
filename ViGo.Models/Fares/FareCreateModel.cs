using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.Fares
{
    public class FareCreateModel
    {
        public Guid VehicleTypeId { get; set; }
        public double BaseDistance { get; set; }
        public double BasePrice { get; set; }
    }
}
