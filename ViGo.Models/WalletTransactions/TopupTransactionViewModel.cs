using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.WalletTransactions
{
    public class TopupTransactionViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public WalletTransactionType Type { get; set; }
        public WalletTransactionStatus Status { get; set; }
        public string OrderUrl { get; set; }
        //public string? ZaloPayTransactionToken { get; set; }

        public TopupTransactionViewModel(WalletTransaction walletTransaction,
            Guid userId,
            string orderUrl /*,
             string? zaloPayTransactionToken*/)
        {
            if (walletTransaction.Type != WalletTransactionType.TOPUP)
            {
                throw new InvalidOperationException();
            }

            Id = walletTransaction.Id;
            UserId = userId;
            Amount = walletTransaction.Amount;
            PaymentMethod = walletTransaction.PaymentMethod;
            Type = walletTransaction.Type;
            Status = walletTransaction.Status;
            OrderUrl = orderUrl;
            //ZaloPayTransactionToken = zaloPayTransactionToken;
        }
    }
}
