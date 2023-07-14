using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.WalletTransactions
{
    public class TopupTransactionCreateModel
    {
        public Guid? UserId { get; set; }
        public double Amount { get; set; }
        //public PaymentMethod? PaymentMethod { get; set; }
    }
}
