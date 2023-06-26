using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Bookings;
using ViGo.Models.Users;
using ViGo.Models.Wallets;
using ViGo.Models.WalletTransactions;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class WalletTransactionServices : BaseServices
    {
        public WalletTransactionServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<WalletTransactionViewModel>> GetAllWalletTransaction(PaginationParameter pagination, HttpContext context, CancellationToken cancellationToken)
        {
            IEnumerable<WalletTransaction> walletTransactions = await work.WalletTransactions.GetAllAsync(cancellationToken: cancellationToken);
            int totalRecords = walletTransactions.Count();
            walletTransactions = walletTransactions.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> walletsId = walletTransactions.Select(x => x.WalletId);
            IEnumerable<Wallet> wallets = await work.Wallets.GetAllAsync(q => q.Where(r => walletsId.Contains(r.Id)), cancellationToken : cancellationToken);
            IEnumerable<Guid> usersId = wallets.Select(q => q.UserId);
            IEnumerable<User> users = await work.Users.GetAllAsync(q => q.Where(u => usersId.Contains(u.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userViewModels = from user in users
                                                        select new UserViewModel(user);
            IEnumerable<WalletViewModel> walletViewModels = from wallet in wallets
                                                            join cus in userViewModels
                                                                on wallet.UserId equals cus.Id
                                                            select new WalletViewModel(wallet, cus);

            //IEnumerable<Guid> bookingsId = walletTransactions.Where(n => n.BookingId.HasValue).Select(x => x.BookingId.Value);
            //IEnumerable<Booking> bookings = await work.Bookings.GetAllAsync(q => q.Where(r => bookingsId.Contains(r.Id)), cancellationToken: cancellationToken);

            //IEnumerable<Guid> bookingDetailsId = walletTransactions.Where(n => n.BookingDetailId.HasValue).Select(x => x.BookingDetailId.Value);
            //IEnumerable<Booking> bookingDetails = await work.Bookings.GetAllAsync(q => q.Where(r => bookingDetailsId.Contains(r.Id)), cancellationToken: cancellationToken);

            //IList<WalletTransactionViewModel> walletTransactionModels = new List<WalletTransactionViewModel>();
            //foreach (WalletTransaction walletTransaction in walletTransactions)
            //{
            //    BookingViewModel? bookingModel = null;
            //    if (walletTransaction.BookingId.HasValue)
            //    {
            //        Booking booking = bookings.SingleOrDefault(x => x.Id.Equals(walletTransaction.BookingId.Value));
            //        bookingModel = new BookingViewModel(booking);
            //    }
            //}
            IEnumerable<WalletTransactionViewModel> walletTransactionViewModels = from walletTransaction in walletTransactions
                                                                                  join walletView in walletViewModels
                                                                                    on walletTransaction.WalletId equals walletView.Id
                                                                                  select new WalletTransactionViewModel(walletTransaction, walletView);

            return walletTransactionViewModels.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize, totalRecords, context);

        }

        //public async Task<WalletTransactionViewModel> CreateTransactionAsync(Guid walletId, WalletTransactionCreateModel walletCreate, CancellationToken cancellationToken)
        //{
        //    Wallet wallet = await work.Wallets.GetAsync(walletId, cancellationToken: cancellationToken);

        //    WalletTransaction walletTransaction = new WalletTransaction
        //    {
        //        WalletId = walletId,
        //        Amount = (Double)walletCreate.Amount,
        //        BookingId = walletCreate.BookingId,
        //        BookingDetailId = walletCreate.BookingDetailId,
        //        ExternalTransactionId = walletCreate.ExternalTransactionId,
        //        Type = walletCreate.Type,
        //        Status = walletCreate.Status,
        //        IsDeleted = false,
        //    };

        //    if(walletTransaction.Type == WalletTransactionType.MOMO_TOPUP
        //       || walletTransaction.Type == WalletTransactionType.TRIP_INCOME
        //       || walletTransaction.Type == WalletTransactionType.BOOKING_REFUND
        //       || walletTransaction.Type == WalletTransactionType.BOOKING_REFUND_MOMO
        //       || walletTransaction.Type == WalletTransactionType.BOOKING_REFUND_VNPAY
        //       || walletTransaction.Type == WalletTransactionType.BOOKING_TOPUP_BY_MOMO
        //       || walletTransaction.Type == WalletTransactionType.BOOKING_TOPUP_BY_VNPAY
        //}
    }
}
