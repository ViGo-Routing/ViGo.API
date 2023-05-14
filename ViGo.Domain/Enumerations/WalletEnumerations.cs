using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum WalletStatus
    {
        INACTIVE = -1,
        ACTIVE = 1
    }

    public enum WalletType
    {
        PERSONAL = 1,
        SYSTEM = 2
    }

    public enum WalletTransactionType
    {
        MOMO_TOPUP = 1,
        BOOKING_PAID = 2,
        BOOING_REFUND = 3,
        BOOKING_PAID_BY_MOMO = 4,
        BOOKING_REFUND_MOMO = 5,
        TRIP_INCOME = 6,
        PAY_FOR_DRIVER = 7
    }

    public enum WalletTransactionStatus
    {
        PENDING = 0,
        SUCCESSFULL = 1,
        FAILED = -1
    }
}
