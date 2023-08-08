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

    public enum RouteType : short
    {
        //SPECIFIC_ROUTE_SPECIFIC_TIME = 0,
        //SPECIFIC_ROUTE_EVERY_TIME = 1,
        //EVERY_ROUTE_SPECIFIC_TIME = 2,
        //EVERY_ROUTE_EVERY_TIME = 3
        ONE_WAY = 0,
        ROUND_TRIP = 1
    }

    public enum RoutineType : short
    {
        RANDOMLY = 0,
        WEEKLY = 1,
        MONTHLY = 2,
        QUARTERLY = 3,
        YEARLY = 4,
        NO_ROUTINE = 5
    }

    public enum RouteStationStatus : short
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
