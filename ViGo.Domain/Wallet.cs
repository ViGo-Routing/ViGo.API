using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Wallet
    {
        public Wallet()
        {
            WalletTransactions = new HashSet<WalletTransaction>();
        }

        public override Guid Id { get; set; }
        public Guid UserId { get; set; }
        public double Balance { get; set; }
        public WalletType Type { get; set; }
        public WalletStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; }
    }
}
