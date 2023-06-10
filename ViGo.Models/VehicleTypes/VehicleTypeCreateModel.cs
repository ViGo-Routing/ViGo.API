using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.VehicleTypes
{
    public class VehicleTypeCreateModel
    {
        public string Name { get; set; } = null!;
        public short Slot { get; set; }
        public VehicleSubType Type { get; set; }

    }
}
