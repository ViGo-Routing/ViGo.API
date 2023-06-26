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
        MOMO_TOPUP = 1, //nạp vô
        BOOKING_PAID = 2, // wallet cus
        BOOKING_REFUND = 3, 
        
        TRIP_INCOME = 4, //dri receive 
        PAY_FOR_DRIVER = 5, //wallet system
        BOOKING_PAID_BY_MOMO = 6, // cus paid
        BOOKING_REFUND_MOMO = 7, 
        BOOKING_PAID_BY_VNPAY = 8, //cus paid
        BOOKING_REFUND_VNPAY = 9,

        BOOKING_TOPUP_BY_MOMO = 10,//nạp vô xong trừ booking
        BOOKING_TOPUP_BY_VNPAY = 12//nạp vô xong trừ booking
    }

    public enum WalletTransactionStatus : short
    {
        PENDING = 0,
        SUCCESSFULL = 1,
        FAILED = -1
    }
}
