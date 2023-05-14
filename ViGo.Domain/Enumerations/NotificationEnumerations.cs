using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum NotificationStatus
    {
        INACTIVE = -1,
        ACTIVE = 1,
    }

    public enum NotificationType
    {
        SPECIFIC_USER = 0,
        BOOKER = 1,
        DRIVER = 2,
        BOOKER_AND_DRIVER = 3,
        ADMIN = 4
    }
}
