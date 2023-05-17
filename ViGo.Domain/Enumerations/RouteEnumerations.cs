using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum RouteStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1
    }

    public enum RouteStationStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }

    public enum StationStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }

    public enum RouteRoutineStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }
}
