using ViGo.Domain.Enumerations;

namespace ViGo.Models.WalletTransactions
{
    public class TopupTransactionCreateModel
    {
        public Guid? UserId { get; set; }
        public double Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }
}
