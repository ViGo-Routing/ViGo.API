using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.WalletTransactions;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;

namespace ViGo.Services
{
    public class WalletTransactionServices : BaseServices
    {
        public WalletTransactionServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<WalletTransactionViewModel>> GetAllWalletTransactionsAsync(
            Guid walletId,
            PaginationParameter pagination,
            HttpContext context, CancellationToken cancellationToken)
        {
            Wallet wallet = await work.Wallets.GetAsync(walletId, cancellationToken: cancellationToken);
            if (!IdentityUtilities.IsAdmin())
            {
                if (!wallet.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!!");
                }
            }

            IEnumerable<WalletTransaction> walletTransactions = await
                work.WalletTransactions.GetAllAsync(
                    query => query.Where(wt => wt.WalletId.Equals(walletId)),
                    cancellationToken: cancellationToken);

            walletTransactions = walletTransactions.OrderBy(t => t.CreatedTime);

            int totalRecords = walletTransactions.Count();
            if (totalRecords == 0)
            {
                return (new List<WalletTransactionViewModel>()).ToPagedEnumerable(
                    pagination.PageNumber, pagination.PageSize, totalRecords, context, true);
            }

            walletTransactions = walletTransactions.OrderByDescending(w => w.CreatedTime)
                .ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            //IEnumerable<Guid> walletsId = walletTransactions.Select(x => x.WalletId);
            //IEnumerable<Wallet> wallets = await work.Wallets.GetAllAsync(q => q.Where(r => walletsId.Contains(r.Id)), cancellationToken : cancellationToken);
            //IEnumerable<Guid> usersId = wallets.Select(q => q.UserId);
            //IEnumerable<User> users = await work.Users.GetAllAsync(q => q.Where(u => usersId.Contains(u.Id)), cancellationToken: cancellationToken);
            //IEnumerable<UserViewModel> userViewModels = from user in users
            //                                            select new UserViewModel(user);
            //IEnumerable<WalletViewModel> walletViewModels = from wallet in wallets
            //                                                join cus in userViewModels
            //                                                    on wallet.UserId equals cus.Id
            //                                                select new WalletViewModel(wallet, cus);

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
                                                                                      //join walletView in walletViewModels
                                                                                      //on walletTransaction.WalletId equals walletView.Id
                                                                                  select new WalletTransactionViewModel(walletTransaction/*, walletView*/);

            return walletTransactionViewModels.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize, totalRecords, context);

        }

        public async Task<WalletTransactionViewModel> GetTransactionAsync(Guid walletTransactionId,
            CancellationToken cancellationToken)
        {
            WalletTransaction walletTransaction = await work.WalletTransactions.GetAsync(walletTransactionId,
                cancellationToken: cancellationToken);
            if (walletTransaction is null)
            {
                throw new ApplicationException("Giao dịch không tồn tại!!");
            }

            if (!IdentityUtilities.IsAdmin())
            {
                Wallet wallet = await work.Wallets.GetAsync(walletTransaction.WalletId,
                cancellationToken: cancellationToken);
                if (!wallet.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!!");
                }
            }

            return new WalletTransactionViewModel(walletTransaction);
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

        public async Task<BookingDetailTransactions> GetBookingDetailTransactionsAsync(
            Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            Guid currentUserId = IdentityUtilities.GetCurrentUserId();
            Wallet wallet = await work.Wallets.GetAsync(
                w => w.UserId.Equals(currentUserId), cancellationToken: cancellationToken);

            IEnumerable<WalletTransaction> walletTransactions = await
                work.WalletTransactions.GetAllAsync(
                    query => query.Where(wt => wt.WalletId.Equals(wallet.Id) && wt.BookingDetailId.HasValue 
                    && wt.BookingDetailId.Value.Equals(bookingDetailId)),
                    cancellationToken: cancellationToken);

            walletTransactions = walletTransactions.OrderByDescending(w => w.CreatedTime);

            IEnumerable<WalletTransactionViewModel> walletTransactionViewModels = from walletTransaction in walletTransactions
                                                                                      //join walletView in walletViewModels
                                                                                      //on walletTransaction.WalletId equals walletView.Id
                                                                                  select new WalletTransactionViewModel(walletTransaction/*, walletView*/);

            double total = walletTransactions.Sum(t =>
            {
                if (!IdentityUtilities.IsAdmin())
                {
                    switch (t.Type)
                    {
                        case WalletTransactionType.TRIP_INCOME:
                        case WalletTransactionType.TOPUP:
                        case WalletTransactionType.CANCEL_REFUND:
                        case WalletTransactionType.TRIP_PICK_REFUND:
                        case WalletTransactionType.BOOKING_REFUND:
                            return t.Amount;
                        case WalletTransactionType.TRIP_PAID:
                        case WalletTransactionType.CANCEL_FEE:
                        case WalletTransactionType.BOOKING_PAID:
                        case WalletTransactionType.TRIP_PICK:
                            return -(t.Amount);
                        default:
                            return t.Amount;
                    }
                }
                else
                {
                    // Admin
                    switch (t.Type)
                    {
                        //case WalletTransactionType.TRIP_INCOME:
                        //case WalletTransactionType.TOPUP:
                        case WalletTransactionType.CANCEL_REFUND:
                        case WalletTransactionType.TRIP_PICK_REFUND:
                        case WalletTransactionType.BOOKING_REFUND:
                        case WalletTransactionType.TRIP_PAID:
                            return -(t.Amount);
                        case WalletTransactionType.CANCEL_FEE:
                        case WalletTransactionType.BOOKING_PAID:
                        case WalletTransactionType.TRIP_PICK:
                            return (t.Amount);
                        default:
                            return t.Amount;
                    }
                }

            });

            return new BookingDetailTransactions(bookingDetailId, walletTransactionViewModels, total);

        }
    }
}
