using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum StationType : short
    {
        OTHER = 0,
        METRO = 1
    }

    public enum StationStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }
}
