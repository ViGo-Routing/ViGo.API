using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.WalletTransactions
{
    public class WalletTransactionCreateModel
    {
        public Guid WalletId { get; set; }
        public double Amount { get; set; }
        public Guid? BookingId { get; set; }
        public Guid? BookingDetailId { get; set; }
        public string? ExternalTransactionId { get; set; }
        public WalletTransactionType Type { get; set; }
        public WalletTransactionStatus Status { get; set; }
        public bool IsDeleted { get; set; }
    }
}
