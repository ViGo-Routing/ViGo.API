using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum ReportStatus
    {
        PENDING = 0,
        PROCESSED = 1,
        DENIED = -1
    }

    public enum ReportType
    {
        DRIVER_NOT_COMING = 1,
        BOOKER_NOT_COMING = 2
    }
}
