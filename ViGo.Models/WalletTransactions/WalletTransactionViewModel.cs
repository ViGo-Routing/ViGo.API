﻿using ViGo.Domain;
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
        public PaymentMethod PaymentMethod { get; set; }
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
        public WalletViewModel? Wallet { get; set; } = null!;

        public WalletTransactionViewModel(WalletTransaction walletTransaction)
        {
            Id = walletTransaction.Id;
            WalletId = walletTransaction.WalletId;
            Amount = walletTransaction.Amount;
            BookingDetailId = walletTransaction.BookingDetailId;
            ExternalTransactionId = walletTransaction.ExternalTransactionId;
            Type = walletTransaction.Type;
            Status = walletTransaction.Status;
            CreatedTime = walletTransaction.CreatedTime;
            CreatedBy = walletTransaction.CreatedBy;
            UpdatedTime = walletTransaction.UpdatedTime;
            UpdatedBy = walletTransaction.UpdatedBy;
            IsDeleted = walletTransaction.IsDeleted;
            PaymentMethod = walletTransaction.PaymentMethod;
            BookingId = walletTransaction.BookingId;
            //Wallet = wallet;
        }

        public WalletTransactionViewModel(WalletTransaction walletTransaction, WalletViewModel wallet)
            : this(walletTransaction)
        {
            //Id = walletTransaction.Id;
            //WalletId = walletTransaction.WalletId;
            //Amount = walletTransaction.Amount;
            //BookingDetailId = walletTransaction.BookingDetailId;
            //ExternalTransactionId = walletTransaction.ExternalTransactionId;
            //Type = walletTransaction.Type;
            //Status = walletTransaction.Status;
            //CreatedTime = walletTransaction.CreatedTime;
            //CreatedBy = walletTransaction.CreatedBy;
            //UpdatedTime = walletTransaction.UpdatedTime;
            //UpdatedBy = walletTransaction.UpdatedBy;
            //IsDeleted = walletTransaction.IsDeleted;
            Wallet = wallet;
        }

        public WalletTransactionViewModel(WalletTransaction walletTransaction, WalletViewModel wallet, BookingViewModel booking)
            : this(walletTransaction, wallet)
        {
            //Id = walletTransaction.Id;
            //WalletId = walletTransaction.WalletId;
            //Amount = walletTransaction.Amount;
            //BookingDetailId = walletTransaction.BookingDetailId;
            //ExternalTransactionId = walletTransaction.ExternalTransactionId;
            //Type = walletTransaction.Type;
            //Status = walletTransaction.Status;
            //CreatedTime = walletTransaction.CreatedTime;
            //CreatedBy = walletTransaction.CreatedBy;
            //UpdatedTime = walletTransaction.UpdatedTime;
            //UpdatedBy = walletTransaction.UpdatedBy;
            //IsDeleted = walletTransaction.IsDeleted;
            //Wallet = wallet;
            Booking = booking;
        }

        public WalletTransactionViewModel(WalletTransaction walletTransaction, WalletViewModel wallet, BookingViewModel? booking, BookingDetailViewModel? bookingDetail)
            : this(walletTransaction, wallet)
        {
            //Id = walletTransaction.Id;
            //WalletId = walletTransaction.WalletId;
            //Amount = walletTransaction.Amount;
            //BookingDetailId = walletTransaction.BookingDetailId;
            //ExternalTransactionId = walletTransaction.ExternalTransactionId;
            //Type = walletTransaction.Type;
            //Status = walletTransaction.Status;
            //CreatedTime = walletTransaction.CreatedTime;
            //CreatedBy = walletTransaction.CreatedBy;
            //UpdatedTime = walletTransaction.UpdatedTime;
            //UpdatedBy = walletTransaction.UpdatedBy;
            //IsDeleted = walletTransaction.IsDeleted;
            //Wallet = wallet;
            Booking = booking;
            BookingDetail = bookingDetail;
        }
    }

    public class BookingDetailTransactions
    {
        public Guid BookingDetailId { get; set; }
        public IEnumerable<WalletTransactionViewModel> Transactions { get; set; }
        public double TotalAmount { get; set; }

        public BookingDetailTransactions(Guid bookingDetailId,
            IEnumerable<WalletTransactionViewModel> transactions,
            double totalAmount)
        {
            BookingDetailId = bookingDetailId;
            Transactions = transactions;
            TotalAmount = totalAmount;
        }
    }
}
