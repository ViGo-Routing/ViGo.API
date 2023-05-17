using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum UserRole : short
    {
        ADMIN = 0,
        CUSTOMER = 1,
        DRIVER = 2
    }

    public enum UserStatus : short
    {
        PENDING = 0,
        ACTIVE = 1,
        INACTIVE = -1,
        VERIFIED = 2,
        REJECTED = -2
    }

    public enum UserLicenseType : short
    {
        IDENTIFICATION = 1,
        DRIVER_LICENSE = 2,
        VEHICLE_REGISTRATION = 3
    }
}
