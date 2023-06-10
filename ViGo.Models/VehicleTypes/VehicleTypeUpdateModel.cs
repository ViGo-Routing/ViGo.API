using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.VehicleTypes
{
    public class VehicleTypeUpdateModel
    {
        public string? Name { get; set; }
        public short? Slot { get; set; }
        public VehicleSubType? Type { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
