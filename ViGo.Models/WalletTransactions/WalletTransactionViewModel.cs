using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.Bookings;
using ViGo.Models.Wallets;

namespace ViGo.Models.WalletTransactions
{
    public class WalletTransactionViewModel
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public double Amount { get; set; }
        public Guid? BookingId { get; set; }
        public Guid? BookingDetailId { get; set; }
        public string? ExternalTransactionId { get; set; }
        public WalletTransactionType Type { get; set; }
        public WalletTransactionStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public BookingViewModel? Booking { get; set; }
        public BookingDetailViewModel? BookingDetail { get; set; }
        public WalletViewModel Wallet { get; set; } = null!;

        public WalletTransactionViewModel(WalletTransaction walletTransaction, WalletViewModel wallet)
        {
            Id = walletTransaction.Id;
            WalletId = walletTransaction.WalletId;
            Amount = walletTransaction.Amount;
            BookingId = walletTransaction.BookingId;
            BookingDetailId = walletTransaction.BookingDetailId;
            ExternalTransactionId = walletTransaction.ExternalTransactionId;
            Type = walletTransaction.Type;
            Status = walletTransaction.Status;
            CreatedTime = walletTransaction.CreatedTime;
            CreatedBy = walletTransaction.CreatedBy;
            UpdatedTime = walletTransaction.UpdatedTime;
            UpdatedBy = walletTransaction.UpdatedBy;
            IsDeleted = walletTransaction.IsDeleted;
            Wallet = wallet;
        }

        public WalletTransactionViewModel(WalletTransaction walletTransaction, WalletViewModel wallet, BookingViewModel booking)
        {
            Id = walletTransaction.Id;
            WalletId = walletTransaction.WalletId;
            Amount = walletTransaction.Amount;
            BookingId = walletTransaction.BookingId;
            BookingDetailId = walletTransaction.BookingDetailId;
            ExternalTransactionId = walletTransaction.ExternalTransactionId;
            Type = walletTransaction.Type;
            Status = walletTransaction.Status;
            CreatedTime = walletTransaction.CreatedTime;
            CreatedBy = walletTransaction.CreatedBy;
            UpdatedTime = walletTransaction.UpdatedTime;
            UpdatedBy = walletTransaction.UpdatedBy;
            IsDeleted = walletTransaction.IsDeleted;
            Wallet = wallet;
            Booking = booking;
        }

        public WalletTransactionViewModel(WalletTransaction walletTransaction, WalletViewModel wallet, BookingViewModel? booking, BookingDetailViewModel? bookingDetail)
        {
            Id = walletTransaction.Id;
            WalletId = walletTransaction.WalletId;
            Amount = walletTransaction.Amount;
            BookingId = walletTransaction.BookingId;
            BookingDetailId = walletTransaction.BookingDetailId;
            ExternalTransactionId = walletTransaction.ExternalTransactionId;
            Type = walletTransaction.Type;
            Status = walletTransaction.Status;
            CreatedTime = walletTransaction.CreatedTime;
            CreatedBy = walletTransaction.CreatedBy;
            UpdatedTime = walletTransaction.UpdatedTime;
            UpdatedBy = walletTransaction.UpdatedBy;
            IsDeleted = walletTransaction.IsDeleted;
            Wallet = wallet;
            Booking = booking;
            BookingDetail = bookingDetail;
        }
    }
}
