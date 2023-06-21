using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Domain.Enumerations
{
    public enum WalletStatus : short
    {
        INACTIVE = -1,
        ACTIVE = 1
    }

    public enum WalletType : short
    {
        PERSONAL = 1,
        SYSTEM = 2
    }

    public enum WalletTransactionType : short
    {
        MOMO_TOPUP = 1,
        BOOKING_PAID = 2,
        BOOKING_REFUND = 3,
        
        TRIP_INCOME = 4,
        PAY_FOR_DRIVER = 5,
        BOOKING_PAID_BY_MOMO = 6,
        BOOKING_REFUND_MOMO = 7,
        BOOKING_PAID_BY_VNPAY = 8,
        BOOKING_REFUND_VNPAY = 9,

        BOOKING_TOPUP_BY_MOMO = 10,
        BOOKING_TOPUP_BY_VNPAY = 12
    }

    public enum WalletTransactionStatus : short
    {
        PENDING = 0,
        SUCCESSFULL = 1,
        FAILED = -1
    }
}
