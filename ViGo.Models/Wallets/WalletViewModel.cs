using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Users;

namespace ViGo.Models.Wallets
{
    public class WalletViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double Balance { get; set; }
        public WalletType Type { get; set; }
        public WalletStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public UserViewModel User { get; set; }

        public WalletViewModel(Wallet wallet, UserViewModel user)
        {
            Id = wallet.Id;
            UserId = wallet.UserId;
            Balance = wallet.Balance;
            Type = wallet.Type;
            Status = wallet.Status;
            CreatedBy = wallet.UserId;
            CreatedTime = wallet.CreatedTime;
            UpdatedBy = wallet.UserId;
            IsDeleted = wallet.IsDeleted;
            User = user;
        }
    }
}
