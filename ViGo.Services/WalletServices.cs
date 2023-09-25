using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Users;
using ViGo.Models.Wallets;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;

namespace ViGo.Services
{
    public class WalletServices : BaseServices
    {
        public WalletServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }


        //public async Task<Wallet> CreateWalletAsync(Guid userId)
        //{
        //    Wallet wallet = new Wallet
        //    {
        //        Id = Guid.NewGuid(),
        //        UserId = userId,
        //        Balance = 0,
        //        Type = WalletType.SYSTEM,
        //        Status = WalletStatus.ACTIVE,
        //        CreatedBy = userId,
        //        UpdatedBy = userId,
        //        IsDeleted = false,
        //    };

        //    await work.Wallets.InsertAsync(wallet);
        //    await work.SaveChangesAsync();
        //    return wallet;
        //}

        public async Task<IPagedEnumerable<WalletViewModel>> GetAllWalletsAsync(
            PaginationParameter pagination, HttpContext context, CancellationToken cancellationToken)
        {
            //IEnumerable<Wallet> wallets = await work.Wallets.GetAllAsync(q => q.Where(
            //    items => items.Type.Equals(WalletType.PERSONAL)), cancellationToken: cancellationToken);

            IEnumerable<Wallet> wallets = await work.Wallets.GetAllAsync(cancellationToken: cancellationToken);
            int totalRecords = wallets.Count();
            wallets = wallets.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> userIds = wallets.Select(w => w.UserId);
            IEnumerable<User> users = await work.Users.GetAllAsync(e => e.Where(r => userIds.Contains(r.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userView = from user in users
                                                  select new UserViewModel(user);
            IEnumerable<WalletViewModel> listWallet = from wallet in wallets
                                                      join cus in userView
                                                        on wallet.UserId equals cus.Id
                                                      select new WalletViewModel(wallet, cus);

            return listWallet.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize, totalRecords, context);
        }
        public async Task<WalletViewModel> GetWalletByUserId(Guid userId, CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                userId = IdentityUtilities.GetCurrentUserId();
            }

            Wallet wallet = await work.Wallets.GetAsync(q => q.UserId.Equals(userId), cancellationToken: cancellationToken);

            if (wallet is null)
            {
                throw new ApplicationException("Ví của người dùng không tồn tại!!");
            }

            Guid userID = wallet.UserId;
            User user = await work.Users.GetAsync(userID, cancellationToken: cancellationToken);
            UserViewModel userView = new UserViewModel(user);
            WalletViewModel walletView = new WalletViewModel(wallet, userView);

            return walletView;
        }

        public async Task<WalletViewModel> UpdateWalletStatusById(Guid id,
            WalletUpdateModel walletUpdate, CancellationToken cancellationToken)
        {
            var currentWallet = await work.Wallets.GetAsync(id,
                cancellationToken: cancellationToken);

            if (currentWallet is null)
            {
                throw new ApplicationException("Ví của người dùng không tồn tại!!");
            }

            currentWallet.Status = walletUpdate.Status;

            await work.Wallets.UpdateAsync(currentWallet);
            await work.SaveChangesAsync();

            Guid userID = currentWallet.UserId;
            User user = await work.Users.GetAsync(userID, cancellationToken: cancellationToken);
            UserViewModel userView = new UserViewModel(user);
            WalletViewModel walletView = new WalletViewModel(currentWallet, userView);
            return walletView;
        }

        public async Task<SystemWalletAnalysisModel> GetSystemWalletAnalysisAsync(
            CancellationToken cancellationToken)
        {
            Wallet systemWallet = await work.Wallets.GetAsync(
                w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);

            IEnumerable<WalletTransaction> walletTransactions = await work.WalletTransactions
                .GetAllAsync(query => query.Where(
                    t => t.WalletId.Equals(systemWallet.Id)), cancellationToken: cancellationToken);

            SystemWalletAnalysisModel analysisModel = new SystemWalletAnalysisModel()
            {
                TotalAmount = systemWallet.Balance,
                TotalTransactions = walletTransactions.Count(),
                //TotalTripPaidAmount = walletTransactions.Where(t => t.Type == WalletTransactionType.TRIP_PAID)
                //    .Sum(t => t.Amount),
                //TotalCancelFeeAmount = walletTransactions.Where(t => t.Type == WalletTransactionType.CANCEL_FEE)
                //    .Sum(t => t.Amount),
                TotalProfits = await CalculateTotalProfitAsync(walletTransactions, cancellationToken),
            };

            return analysisModel;
        }

        //public async Task<IEnumerable<SystemWalletAnalysises>> 
        //    GetSystemWalletAnalysisesAsync(SystemWalletAnalysisRequest request,
        //        CancellationToken cancellationToken)
        //{
        //    Wallet systemWallet = await work.Wallets.GetAsync(
        //        w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);

        //    IEnumerable<WalletTransaction> walletTransactions = await work.WalletTransactions
        //        .GetAllAsync(query => query.Where(
        //            t => t.WalletId.Equals(systemWallet.Id)), cancellationToken: cancellationToken);

        //    List<SystemWalletAnalysises> analysises = new List<SystemWalletAnalysises>();

        //    switch (request.AnalysisType)
        //    {
        //        case AnalysisType.WEEK:
        //            IEnumerable<DateOnly> weekDates = DateTimeUtilities.GetCurrentWeekDates();
        //            walletTransactions = walletTransactions.Where(t =>
        //                DateOnly.FromDateTime(t.CreatedTime) >= weekDates.First()
        //                && DateOnly.FromDateTime(t.CreatedTime) <= weekDates.Last());

        //            foreach (DateOnly date in weekDates)
        //            {
        //                IEnumerable<WalletTransaction> transactions = walletTransactions
        //                    .Where(t => DateOnly.FromDateTime(t.CreatedTime) == date);
        //                SystemWalletAnalysises analysis = new SystemWalletAnalysises
        //                {
        //                    Time = date.ToString(),
        //                };

        //                if (transactions.Any())
        //                {
        //                    SystemWalletAnalysisModel model = new SystemWalletAnalysisModel
        //                    {
        //                        TotalTransactions = transactions.Count(),
        //                        TotalProfits = await CalculateTotalProfitAsync(transactions, cancellationToken),
        //                    };
        //                    analysis.Analysis = model;
        //                } else
        //                {
        //                    analysis.Analysis = new SystemWalletAnalysisModel
        //                    {
        //                        TotalTransactions = 0,
        //                        TotalProfits = 0
        //                    };
        //                }

        //                analysises.Add(analysis);
        //            }
        //            break;
        //        case AnalysisType.MONTH:
        //            IEnumerable<DateOnly> monthDates = DateTimeUtilities.GetCurrentMonthDates();
        //            walletTransactions = walletTransactions.Where(t =>
        //                DateOnly.FromDateTime(t.CreatedTime) >= monthDates.First()
        //                && DateOnly.FromDateTime(t.CreatedTime) <= monthDates.Last());

        //            foreach (DateOnly date in monthDates)
        //            {
        //                IEnumerable<WalletTransaction> transactions = walletTransactions
        //                    .Where(t => DateOnly.FromDateTime(t.CreatedTime) == date);
        //                SystemWalletAnalysises analysis = new SystemWalletAnalysises
        //                {
        //                    Time = date.ToString(),
        //                };

        //                if (transactions.Any())
        //                {
        //                    SystemWalletAnalysisModel model = new SystemWalletAnalysisModel
        //                    {
        //                        TotalTransactions = transactions.Count(),
        //                        TotalProfits = await CalculateTotalProfitAsync(transactions, cancellationToken),
        //                    };
        //                    analysis.Analysis = model;
        //                }
        //                else
        //                {
        //                    analysis.Analysis = new SystemWalletAnalysisModel
        //                    {
        //                        TotalTransactions = 0,
        //                        TotalProfits = 0
        //                    };
        //                }
        //                analysises.Add(analysis);
        //            }
        //            break;
        //        case AnalysisType.YEAR:
        //            break;
        //        case AnalysisType.CUSTOM_RANGE:
        //            break;
        //    }

        //    return analysises;
        //}

        #region Private
        private async Task<double> CalculateTotalProfitAsync(
            IEnumerable<WalletTransaction> transactions,
            CancellationToken cancellationToken)
        {
            if (transactions.Any())
            {
                double tripPickTotal = transactions.Where(
                    t => t.Type == WalletTransactionType.TRIP_PICK).Sum(t => t.Amount);

                double tripPickRefundTotal = transactions.Where(
                    t => t.Type == WalletTransactionType.TRIP_PICK_REFUND).Sum(t => t.Amount);

                IEnumerable<WalletTransaction> cancelRefundTransactions =
                    transactions.Where(t => t.Type == WalletTransactionType.CANCEL_REFUND);
                double cancelRefundProfit = 0;
                foreach (WalletTransaction cancel in cancelRefundTransactions)
                {
                    if (cancel.BookingDetailId.HasValue)
                    {
                        BookingDetail bookingDetail = await work.BookingDetails
                            .GetAsync(cancel.BookingDetailId.Value,
                            includeDeleted: true,
                            cancellationToken: cancellationToken);
                        cancelRefundProfit += bookingDetail.Price.Value - cancel.Amount;
                    }
                }

                double profit = tripPickTotal - tripPickRefundTotal + cancelRefundProfit;
                return profit;
            }
            else
            {
                return 0;
            }
        }
        #endregion
    }
}
