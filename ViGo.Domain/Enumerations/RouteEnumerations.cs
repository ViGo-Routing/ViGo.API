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

    public enum RoutineType : short
    {
        RANDOMLY = 0,
        WEEKLY = 1,
        MONTHLY = 2,
        QUARTERLY = 3,
        YEARLY = 4
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
