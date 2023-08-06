using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.WalletTransactions;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.BackgroundTasks;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;
using ViGo.Utilities.Payments;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public partial class PaymentServices : UseNotificationServices
    {
        private delegate Task<(TopupTransactionViewModel, string, string)> internalCreateTopupTransaction
            (TopupTransactionCreateModel model, WalletTransaction walletTransaction,
            HttpContext httpContext, CancellationToken cancellationToken);

        public PaymentServices(IUnitOfWork work, ILogger logger) 
            : base(work, logger)
        {
        }

        public async Task<(TopupTransactionViewModel?, string)> 
            CreateTopUpTransactionRequest(TopupTransactionCreateModel model,
                //PaymentMethod paymentMethod,
                HttpContext httpContext,
                CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                model.UserId = IdentityUtilities.GetCurrentUserId();
            }
            else if (!model.UserId.HasValue)
            {
                throw new ApplicationException("Thiếu thông tin người dùng!");
            }

            User? user = await work.Users.GetAsync(model.UserId.Value, cancellationToken: cancellationToken);
            if (user is null || (user.Role != UserRole.CUSTOMER &&
                user.Role != UserRole.DRIVER))
            {
                throw new ApplicationException("Thông tin người dùng không hợp lệ!");
            }

            if (model.Amount < 1000)
            {
                throw new ApplicationException("Giá trị nạp tiền phải lớn hơn 1.000VND!");
            }

            if (!Enum.IsDefined(model.PaymentMethod) || 
                (model.PaymentMethod != PaymentMethod.VNPAY 
                /* && model.PaymentMethod != PaymentMethod.ZALO*/))
            {
                throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
            }

            Wallet wallet = await work.Wallets.GetAsync(w =>
                w.UserId.Equals(model.UserId.Value), cancellationToken: cancellationToken);
            WalletTransaction walletTransaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod,
                Type = WalletTransactionType.TOPUP,
                Status = WalletTransactionStatus.PENDING
            };

            await work.WalletTransactions.InsertAsync(walletTransaction,
                cancellationToken: cancellationToken);

            internalCreateTopupTransaction createTopupTransaction;

            switch (model.PaymentMethod)
            {
                //case PaymentMethod.ZALO:
                //    createTopupTransaction = CreateZaloTopupTransactionAsync;
                //    break;
                case PaymentMethod.VNPAY:
                    createTopupTransaction = CreateVnPayTopupTransactionAsync;
                    break;
                default:
                    throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
            }

            (TopupTransactionViewModel topupViewModel, string externalTransactionId,
                string clientIpAddress) = await createTopupTransaction(model,
                walletTransaction, httpContext, cancellationToken);
            walletTransaction.ExternalTransactionId = externalTransactionId;
            await work.SaveChangesAsync(cancellationToken);

            return (topupViewModel, clientIpAddress);
        }

    }
}
