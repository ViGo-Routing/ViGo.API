using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Domain;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Payments;
using ViGo.Utilities;
using ViGo.Models.WalletTransactions;
using System.Net.Http;

namespace ViGo.Services
{
    public partial class PaymentServices
    {
        #region ZaloPay

        private async Task<(TopupTransactionViewModel, string)> CreateZaloTopupTransactionAsync(
            TopupTransactionCreateModel model, WalletTransaction walletTransaction,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            if (model.PaymentMethod != PaymentMethod.ZALO)
            {
                throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
            }

            // Call to ZaloPay to create order
            ZaloPayOrderCreateModel zaloCreateOrder = new ZaloPayOrderCreateModel(
                model, walletTransaction.Id, httpContext);
            ZaloPayCreateOrderResponse? createOrderResponse =
                await HttpClientUtilities.SendRequestAsync<ZaloPayCreateOrderResponse, ZaloPayOrderCreateModel>(
                    ViGoConfiguration.ZaloPayApiUrl + "/create",
                    HttpMethod.Post,
                    body: zaloCreateOrder, cancellationToken: cancellationToken
                    );

            if (createOrderResponse != null)
            {
                if (createOrderResponse.ReturnCode == 1)
                {

                    //walletTransaction.ExternalTransactionId = zaloCreateOrder.AppTransactionId;
                    //await work.SaveChangesAsync(cancellationToken);

                    return (new TopupTransactionViewModel(walletTransaction,
                        model.UserId.Value, createOrderResponse.OrderUrl,
                        createOrderResponse.ZaloPayTransactionToken), zaloCreateOrder.AppTransactionId);
                }

                throw new ApplicationException("Tạo đơn hàng trên hệ thống ZaloPay không thành công!" +
                    "\nChi tiết: " + createOrderResponse.ReturnMessage);
            }

            throw new ApplicationException("Tạo đơn hàng trên hệ thống ZaloPay không thành công!");
        }

        public async Task<ZaloPayCallbackResponse> ZaloPayCallback(
            ZaloPayCallbackModel model,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("==== ZALOPAY CALL BACK ====");
            ZaloPayCallbackResponse response = new ZaloPayCallbackResponse();

            bool isValidCallback = ZaloPayHelper.VerifyCallback(model.Data, model.Mac);

            if (!isValidCallback)
            {
                _logger.LogError("ZaloPay Callback: Invalid Mac!");
                response.ReturnCode = -1;
                response.ReturnMessage = "Mac không hợp lệ!";
            }
            else
            {
                ZaloPayCallbackData data = JsonConvert.DeserializeObject<ZaloPayCallbackData>(model.Data);

                string appTransactionId = data.AppTransactionId;
                Guid transactionId = HashingUtilities.FromBase64String(appTransactionId.Substring(7));

                // Get WalletTransaction
                WalletTransaction walletTransaction = await work.WalletTransactions.GetAsync(transactionId,
                    cancellationToken: cancellationToken);

                if (walletTransaction.PaymentMethod != PaymentMethod.ZALO)
                {
                    _logger.LogError("ZaloPay Callback: Invalid Payment Method!");
                    response.ReturnCode = -1;
                    response.ReturnMessage = "Phương thức thanh toán không hợp lệ!";
                    //throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
                }
                if (walletTransaction.Status != WalletTransactionStatus.PENDING)
                {
                    _logger.LogError("ZaloPay Callback: Invalid Status!");
                    response.ReturnCode = -1;
                    response.ReturnMessage = "Trạng thái giao dịch không hợp lệ!";
                    //throw new ApplicationException("Trạng thái giao dịch không hợp lệ!!");
                }

                walletTransaction.ExternalTransactionId += "_ZaloPay_" + data.ZaloPayTransactionId.ToString();
                walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;

                Wallet wallet = await work.Wallets.GetAsync(walletTransaction.WalletId, cancellationToken: cancellationToken);
                wallet.Balance += walletTransaction.Amount;

                // TODO Code
                // Check for Failed Canceled Fee withdrawal

                await work.WalletTransactions.UpdateAsync(walletTransaction, isManuallyAssignTracking: true);
                await work.Wallets.UpdateAsync(wallet, isManuallyAssignTracking: true);

                await work.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("ZaloPay Callback: Success!");
                response.ReturnCode = 1;
                response.ReturnMessage = "success";
            }

            return response;
        }

        public async Task<ZaloPayQueryViGoResponse> ZaloPayGetOrderStatus(
            Guid walletTransactionId, HttpContext context,
            CancellationToken cancellationToken)
        {
            WalletTransaction? walletTransaction = await work.WalletTransactions
                .GetAsync(walletTransactionId, cancellationToken: cancellationToken);
            if (walletTransaction is null ||
               string.IsNullOrEmpty(walletTransaction.ExternalTransactionId)
               || !walletTransaction.ExternalTransactionId.Contains("ZaloPay"))
            {
                throw new ApplicationException("Giao dịch không tồn tại!!");
            }

            string appTransactionId = walletTransaction.ExternalTransactionId.Split("_ZaloPay_")[0];
            ZaloPayQueryModel model = new ZaloPayQueryModel(appTransactionId, context);

            ZaloPayQueryResponse response = await HttpClientUtilities
                .SendRequestAsync<ZaloPayQueryResponse, ZaloPayQueryModel>(ViGoConfiguration.ZaloPayApiUrl + "/query",
                HttpMethod.Post, body: model, cancellationToken: cancellationToken);

            return new ZaloPayQueryViGoResponse(response);
        }

        #endregion
    }
}
