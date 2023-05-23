﻿using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class WalletTransaction
    {
        public override Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public double Amount { get; set; }
        public Guid? BookingId { get; set; }
        public Guid? BookingDetailId { get; set; }
        public WalletTransactionType Type { get; set; }
        public WalletTransactionStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Booking? Booking { get; set; }
        public virtual BookingDetail? BookingDetail { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }
}