using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum SettingType
    {
        DEFAULT = 0,
        TRIP = 1,
        PENALTY = 2,
        ROUTE_ROUTINE = 3,
        PRICING = 4
    }

    public enum SettingDataType
    {
        DEFAULT = 0,
        INTEGER = 1,
        DOUBLE = 2,
        TIME = 3
    }

    public enum SettingDataUnit
    {
        DEFAULT = 0,
        PERCENT = 1,
        MINUTES = 2,
        HOURS = 3,
        DAYS = 4,
        METERS = 5,
        KILOMETERS = 6,
        TURN = 7,
        TIME = 8,
        MB = 9
    }
}
