using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.HttpContextUtilities;
using ViGo.Models.Notifications;
using ViGo.Models.WalletTransactions;
using ViGo.Utilities;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;
using ViGo.Utilities.Payments;

namespace ViGo.Services
{
    public partial class PaymentServices
    {
        #region VnPay
        private async Task<(TopupTransactionViewModel, string)> CreateVnPayTopupTransactionAsync(
            TopupTransactionCreateModel model, WalletTransaction walletTransaction,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            if (model.PaymentMethod != PaymentMethod.VNPAY)
            {
                throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
            }

            string vnpReturnUrl = ViGoConfiguration.VnPayReturnUrl(httpContext);
            string vnpPaymentUrl = ViGoConfiguration.VnPayPaymentUrl;
            string vnpTmnCode = ViGoConfiguration.VnPayTmnCode;
            string vnpHashSecret = ViGoConfiguration.VnPayHashSecret;
            string vnpApiVersion = ViGoConfiguration.VnPayApiVersion;

            // Generate fake information
            //Guid bookingDetailId = Guid.Parse("EAC6836E-1CF0-4064-B01C-BD5F1B298EBC");

            string refId = HashingUtilities.ToBase64String(walletTransaction.Id);
            string txnRef = walletTransaction.CreatedTime
                .ToString("yyyyMMddHHmmss") + "_" + refId;

            VnPayLibrary vnPay = new VnPayLibrary();

            vnPay.AddRequestData("vnp_Version", vnpApiVersion);
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", vnpTmnCode);
            vnPay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());

            vnPay.AddRequestData("vnp_CreateDate", walletTransaction.CreatedTime
                .ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_IpAddr", httpContext.GetClientIpAddress());
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_OrderInfo", "Topup tài khoản");
            vnPay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnPay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
            //vnPay.AddRequestData("vnp_TxnRef", bookingDetailId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày
            vnPay.AddRequestData("vnp_TxnRef", txnRef); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            string paymentUrl = vnPay.CreatePaymentRequestUrl(vnpPaymentUrl, vnpHashSecret);

            //walletTransaction.ExternalTransactionId = txnRef;
            //await work.SaveChangesAsync(cancellationToken);

            return (new TopupTransactionViewModel(walletTransaction, model.UserId.Value, paymentUrl, null),
                txnRef);
        }

        public async Task<string> VnPayPaymentCallbackAsync(
            string requestRawUrl,
            IQueryCollection vnPayData,
            CancellationToken cancellationToken)
        {
            string message = string.Empty;
            _logger.LogInformation("Begin VNPay Callback, URL={url}", requestRawUrl);

            if (vnPayData.Any())
            {
                string vnpHashSecret = ViGoConfiguration.VnPayHashSecret;

                VnPayLibrary vnPay = new VnPayLibrary();
                foreach (string s in vnPayData.Keys)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnPay.AddResponseData(s, vnPayData[s]);
                    }
                }

                //Guid bookingId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef"));
                Guid transactionId = HashingUtilities.FromBase64String(vnPay.GetResponseData("vnp_TxnRef").Substring(15));

                //Guid transactionId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef").Split("-")[1]);
                long vnPayTransactionId = Convert.ToInt64(vnPay.GetResponseData("vnp_TransactionNo"));
                string vnpResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
                string vnpTransactionStatus = vnPay.GetResponseData("vnp_TransactionStatus");
                string vnpSecureHash = vnPay.GetResponseData("vnp_SecureHash");
                string terminalId = vnPay.GetResponseData("vnp_TmnCode");
                long vnpAmount = Convert.ToInt64(vnPay.GetResponseData("vnp_Amount")) / 100;
                string bankCode = vnPay.GetResponseData("vnp_BankCode");

                bool checkSignature = vnPay.IsValidSignature(vnpSecureHash, vnpHashSecret);

                if (checkSignature)
                {

                    if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
                    {
                        message = "Giao dịch được thực hiện hành công!";
                        _logger.LogInformation("Transaction has been paid successfully!! WalletTransactionId={0}, VNPay TransactionId={1}", transactionId, vnPayTransactionId);
                    }
                    else
                    {
                        //throw new ApplicationException("Thanh toán VNPay lỗi! Mã lỗi: " + vnpResponseCode);
                        message = "Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: " + vnpResponseCode;

                        _logger.LogInformation("Transaction failed to be paid!! " +
                            "WalletTransactionId={0}, VNPay TransactionId={1}, ResponseCode={3}",
                            transactionId, vnPayTransactionId, vnpResponseCode);
                    }

                }
                else
                {
                    message = "Có lỗi xảy ra trong quá trình xử lý!";

                    _logger.LogInformation("Invalid signature!! WalletTransactionId={0}, InputData={1}",
                        transactionId, requestRawUrl);

                    //throw new ApplicationException("Đã có lỗi xảy ra trong quá trình xử lý đơn thanh toán!!");
                }
            }
            else
            {
                message = "Thông tin không hợp lệ!";
            }
            return message;
        }

        public async Task<(string, string, Guid?)> VnPayPaymentIpnAsync(
            string requestRawUrl,
            IQueryCollection vnPayData,
            //IBackgroundTaskQueue backgroundTaskQueue,
            //IServiceScopeFactory serviceScopeFactory,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== Begin VNPay IPN... ======");
            (string code, string message) = (string.Empty, string.Empty);

            WalletTransaction? walletTransaction = null;
            Wallet? wallet = null;
            string? fcmToken = null;
            User? user = null;
            Guid? returnUserId = null;

            NotificationCreateModel notification = new NotificationCreateModel()
            {
                Type = NotificationType.SPECIFIC_USER,
            };

            try
            {
                if (vnPayData.Any())
                {
                    string vnpHashSecret = ViGoConfiguration.VnPayHashSecret;

                    VnPayLibrary vnPay = new VnPayLibrary();
                    foreach (string s in vnPayData.Keys)
                    {
                        if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                        {
                            vnPay.AddResponseData(s, vnPayData[s]);
                        }
                    }

                    //Guid bookingId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef"));
                    //Guid transactionId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef").Split("-")[1]);
                    Guid transactionId = HashingUtilities.FromBase64String(vnPay.GetResponseData("vnp_TxnRef").Substring(15));

                    long vnPayTransactionId = Convert.ToInt64(vnPay.GetResponseData("vnp_TransactionNo"));
                    string vnpResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
                    string vnpTransactionStatus = vnPay.GetResponseData("vnp_TransactionStatus");
                    string vnpSecureHash = vnPay.GetResponseData("vnp_SecureHash");
                    string terminalId = vnPay.GetResponseData("vnp_TmnCode");
                    long vnpAmount = Convert.ToInt64(vnPay.GetResponseData("vnp_Amount")) / 100;
                    string bankCode = vnPay.GetResponseData("vnp_BankCode");

                    bool checkSignature = vnPay.IsValidSignature(vnpSecureHash, vnpHashSecret);
                    if (checkSignature)
                    {
                        walletTransaction = await work.WalletTransactions.GetAsync(transactionId, cancellationToken: cancellationToken);
                        if (walletTransaction is null)
                        {
                            //throw new ApplicationException("Không tìm thấy chuyến đi! Vui lòng kiểm tra lại...");
                            code = "01";
                            message = "Transaction is not found!!";
                        }
                        else
                        {
                            if (walletTransaction.Amount != vnpAmount)
                            {
                                code = "04";
                                message = "Invalid amount!";
                            }
                            else
                            {
                                if (walletTransaction.Status == WalletTransactionStatus.PENDING)
                                {
                                    wallet = await work.Wallets.GetAsync(walletTransaction.WalletId, cancellationToken: cancellationToken);
                                    if (wallet is null)
                                    {
                                        throw new ApplicationException("Không tìm thấy ví của người dùng!");
                                    }

                                    if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
                                    {
                                        //systemTransaction_Add.Status = WalletTransactionStatus.SUCCESSFULL;
                                        //systemTransaction_Add.Amount += vnpAmount;

                                        walletTransaction.ExternalTransactionId += "_VnPay_" + vnPayTransactionId;

                                        walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;
                                        wallet.Balance += walletTransaction.Amount;

                                        // TODO Code
                                        // Check for Failed Canceled Fee withdrawal

                                        _logger.LogInformation("Topup has been paid successfully!! WalletTransactionId={0}, VNPay TransactionId={1}", walletTransaction.Id, vnPayTransactionId);

                                    }
                                    else
                                    {
                                        //throw new ApplicationException("Thanh toán VNPay lỗi! Mã lỗi: " + vnpResponseCode);
                                        //systemTransaction_Add.Status = WalletTransactionStatus.FAILED;
                                        //walletTransaction_Topup.Status = WalletTransactionStatus.FAILED;
                                        //walletTransaction_Paid.Status = WalletTransactionStatus.FAILED;

                                        //booking.PaymentMethod = PaymentMethod.VNPAY;
                                        walletTransaction.Status = WalletTransactionStatus.FAILED;

                                        _logger.LogInformation("Topup failed to be paid!! " +
                                            "WalletTransactionId={0}, VNPay TransactionId={1}, ResponseCode={2}",
                                            walletTransaction.Id, vnPayTransactionId, vnpResponseCode);
                                    }

                                    await work.WalletTransactions.UpdateAsync(walletTransaction, isManuallyAssignTracking: true);
                                    await work.Wallets.UpdateAsync(wallet, isManuallyAssignTracking: true);

                                    // Add Wallet Transaction
                                    //await work.WalletTransactions.InsertAsync(systemTransaction_Add,
                                    //    isManuallyAssignTracking: true, cancellationToken: cancellationToken);
                                    //await work.WalletTransactions.InsertAsync(walletTransaction_Topup,
                                    //    isManuallyAssignTracking: true, cancellationToken: cancellationToken);
                                    //await work.WalletTransactions.InsertAsync(walletTransaction_Paid,
                                    //    isManuallyAssignTracking: true, cancellationToken: cancellationToken);

                                    await work.SaveChangesAsync(cancellationToken);

                                    // TODO Code

                                    // Run trip mapping
                                    //await backgroundTaskQueue.QueueBackGroundWorkItemAsync(async token =>
                                    //{
                                    //    await using (var scope = serviceScopeFactory.CreateAsyncScope())
                                    //    {
                                    //        IUnitOfWork work = new UnitOfWork(scope.ServiceProvider);
                                    //        TripMappingServices tripMappingServices = new TripMappingServices(work, _logger);
                                    //        await tripMappingServices.MapBooking(booking, _logger);
                                    //    }
                                    //});

                                    // Send notification to user
                                    user = await work.Users.GetAsync(wallet.UserId, cancellationToken: cancellationToken);

                                    fcmToken = user.FcmToken;

                                    _logger.LogInformation("User FCM: " + fcmToken);
                                    notification.UserId = user.Id;

                                    if (walletTransaction.Status == WalletTransactionStatus.SUCCESSFULL)
                                    {
                                        returnUserId = user.Id;
                                    }

                                    if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                                    {
                                        _logger.LogInformation("User Notification!!");
                                        _logger.LogInformation("Wallet Transaction Status: " + walletTransaction.Status.ToString());

                                        if (walletTransaction.Status == WalletTransactionStatus.SUCCESSFULL)
                                        {
                                            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                                            {
                                                {"action", NotificationAction.TransactionDetail },
                                                    { "walletTransactionId", walletTransaction.Id.ToString() },
                                                    { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                                                    { "isSuccess", "true" },
                                                    { "message", "Thanh toán bằng VNPay thành công!" }
                                            };

                                            notification.Title = "Thanh toán bằng VNPay thành công";
                                            notification.Description = "Quý khách đã thực hiện thanh toán topup bằng VNPay thành công!!";

                                            //await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thành công",
                                            //"Quý khách đã thực hiện thanh toán topup bằng VNPay thành công!!", data: dataToSend,
                                            //    cancellationToken: cancellationToken);
                                            
                                            //// Send data to mobile application
                                            //await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, dataToSend, cancellationToken);

                                            await notificationServices.CreateFirebaseNotificationAsync(notification, fcmToken,
                                                dataToSend, cancellationToken);
                                        }
                                        else
                                        {
                                            // FAILED
                                            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                                            {
                                                {"action", NotificationAction.TransactionDetail },
                                                { "walletTransactionId", walletTransaction.Id.ToString() },
                                                { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                                                { "isSuccess", "false" },
                                                { "message", "Thanh toán bằng VNPay thất bại!" }
                                            };

                                            notification.Title = "Thanh toán bằng VNPay thất bại";
                                            notification.Description = "Giao dịch thực hiện thanh toán topup bằng VNPay của quý khách đã bị thất bại!!";

                                           // await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thất bại",
                                           //"Giao dịch thực hiện thanh toán topup bằng VNPay của quý khách đã bị thất bại!!", data: dataToSend,
                                           //cancellationToken: cancellationToken);

                                           // // Send data to mobile application
                                           // await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, dataToSend, cancellationToken);

                                            await notificationServices.CreateFirebaseNotificationAsync(
                                                notification, fcmToken, dataToSend, cancellationToken);
                                        }

                                    }

                                    code = "00";
                                    message = "Confirm success";
                                }
                                else
                                {
                                    code = "02";
                                    message = "Transaction has been already paid!!";
                                }
                            }

                        }

                    }
                    else
                    {
                        code = "97";
                        message = "Invalid checksum";

                        _logger.LogInformation("Invalid signature!! WalletTransactionId={0}, InputData={1}",
                            transactionId, requestRawUrl);

                        //throw new ApplicationException("Đã có lỗi xảy ra trong quá trình xử lý đơn thanh toán!!");
                    }
                }
                else
                {
                    code = "99";
                    message = "Input data required!!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("VNPay IPN Error!! Error description: {exception}, RawURL: {rawUrl}",
                    ex.GeneratorErrorMessage(), requestRawUrl);

                if (walletTransaction != null && fcmToken != null
                    && !string.IsNullOrEmpty(fcmToken) && user != null)
                {
                    _logger.LogInformation("Send notification when exception...");
                   Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                    {
                        {"action", NotificationAction.TransactionDetail },
                        { "walletTransactionId", walletTransaction.Id.ToString() },
                        { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                        { "isSuccess", "false" },
                        { "message", "Thanh toán bằng VNPay thất bại!" }
                    };

                    notification.UserId = user.Id;

                    notification.Title = "Thanh toán bằng VNPay thất bại";
                    notification.Description = "Giao dịch thực hiện thanh toán topup bằng VNPay của quý khách đã bị thất bại!!";

                    //await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thất bại",
                    //                       "Giao dịch thực hiện thanh toán topup bằng VNPay của quý khách đã bị thất bại!!", data: dataToSend, 
                    //                       cancellationToken: cancellationToken);

                    //// Send data to mobile application
                    //await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, dataToSend, cancellationToken);

                    await notificationServices.CreateFirebaseNotificationAsync(notification, fcmToken, dataToSend, cancellationToken);
                }

                return ("99", "Unknown Error: " + ex.GeneratorErrorMessage(), returnUserId);
            }

            _logger.LogInformation("VNPay IPN: Code: {code}, message: {message}", code, message);
            return (code, message, returnUserId);
        }

        public async Task<VnPayQueryViGoResponse>
            GetVnPayTransactionStatus(Guid walletTransactionId,
            HttpContext httpContext, CancellationToken cancellationToken)
        {
            WalletTransaction? walletTransaction = await work.WalletTransactions
                .GetAsync(walletTransactionId, cancellationToken: cancellationToken);
            if (walletTransaction is null ||
                string.IsNullOrEmpty(walletTransaction.ExternalTransactionId)
                || walletTransaction.PaymentMethod != PaymentMethod.VNPAY)
            {
                throw new ApplicationException("Giao dịch không tồn tại!!");
            }

            string[] external = walletTransaction.ExternalTransactionId.Split("_VnPay_");
            string vnPayTransactionRef = external[0];
            long vnPayTransactionNo = Convert.ToInt64(external[1]);


            VnPayQueryRequest vnPayQueryRequest = new VnPayQueryRequest(
                vnPayTransactionRef, vnPayTransactionNo, walletTransaction.CreatedTime, httpContext);

            VnPayQueryResponse response = await HttpClientUtilities
                .SendRequestAsync<VnPayQueryResponse, VnPayQueryRequest>(
                ViGoConfiguration.VnPayQueryUrl, HttpMethod.Post,
                body: vnPayQueryRequest, cancellationToken: cancellationToken);

            //if (!response.IsValidResponse(ViGoConfiguration.VnPayHashSecret))
            //{
            //    throw new ApplicationException("Checksum không hợp lệ!!");
            //}
            return new VnPayQueryViGoResponse(response);
        }

        //public async Task<string> GenerateVnPayPaymentUrlAsync(TopupTransactionCreateModel model,
        //    PaymentMethod paymentMethod, HttpContext context,
        //    CancellationToken cancellationToken)
        //{
        //    if (!IdentityUtilities.IsAdmin())
        //    {
        //        model.UserId = IdentityUtilities.GetCurrentUserId();
        //    }
        //    else if (!model.UserId.HasValue)
        //    {
        //        throw new ApplicationException("Thiếu thông tin người dùng!");
        //    }

        //    User? user = await work.Users.GetAsync(model.UserId.Value, cancellationToken: cancellationToken);
        //    if (user is null || (user.Role != UserRole.CUSTOMER &&
        //        user.Role != UserRole.DRIVER))
        //    {
        //        throw new ApplicationException("Thông tin người dùng không hợp lệ!");
        //    }

        //    if (model.Amount < 1000)
        //    {
        //        throw new ApplicationException("Giá trị nạp tiền phải lớn hơn 1.000VND!");
        //    }

        //    //if (!Enum.IsDefined(model.PaymentMethod))
        //    //{
        //    //    throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
        //    //}

        //    Wallet wallet = await work.Wallets.GetAsync(w =>
        //        w.UserId.Equals(model.UserId.Value), cancellationToken: cancellationToken);
        //    WalletTransaction walletTransaction = new WalletTransaction
        //    {
        //        WalletId = wallet.Id,
        //        Amount = model.Amount,
        //        PaymentMethod = paymentMethod,
        //        Type = WalletTransactionType.TOPUP,
        //        Status = WalletTransactionStatus.PENDING
        //    };

        //    await work.WalletTransactions.InsertAsync(walletTransaction,
        //        cancellationToken: cancellationToken);

        //    string vnpReturnUrl = ViGoConfiguration.VnPayReturnUrl(context);
        //    string vnpPaymentUrl = ViGoConfiguration.VnPayPaymentUrl;
        //    string vnpTmnCode = ViGoConfiguration.VnPayTmnCode;
        //    string vnpHashSecret = ViGoConfiguration.VnPayHashSecret;
        //    string vnpApiVersion = ViGoConfiguration.VnPayApiVersion;

        //    // Generate fake information
        //    //Guid bookingDetailId = Guid.Parse("EAC6836E-1CF0-4064-B01C-BD5F1B298EBC");

        //    string refId = HashingUtilities.ToBase64String(walletTransaction.Id);
        //    string txnRef = DateTimeUtilities.GetDateTimeVnNow()
        //        .ToString("yyyyMMddHHmmss") + "_" + refId;

        //    VnPayLibrary vnPay = new VnPayLibrary();

        //    vnPay.AddRequestData("vnp_Version", vnpApiVersion);
        //    vnPay.AddRequestData("vnp_Command", "pay");
        //    vnPay.AddRequestData("vnp_TmnCode", vnpTmnCode);
        //    vnPay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());

        //    vnPay.AddRequestData("vnp_CreateDate", DateTimeUtilities.GetDateTimeVnNow()
        //        .ToString("yyyyMMddHHmmss"));
        //    vnPay.AddRequestData("vnp_CurrCode", "VND");
        //    vnPay.AddRequestData("vnp_IpAddr", context.GetClientIpAddress());
        //    vnPay.AddRequestData("vnp_Locale", "vn");
        //    vnPay.AddRequestData("vnp_OrderInfo", "Topup tài khoản");
        //    vnPay.AddRequestData("vnp_OrderType", "other"); //default value: other

        //    vnPay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
        //    //vnPay.AddRequestData("vnp_TxnRef", bookingDetailId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày
        //    vnPay.AddRequestData("vnp_TxnRef", txnRef); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

        //    string paymentUrl = vnPay.CreateRequestUrl(vnpPaymentUrl, vnpHashSecret);

        //    walletTransaction.ExternalTransactionId = txnRef;
        //    await work.SaveChangesAsync(cancellationToken);

        //    return paymentUrl;
        //}
        #endregion
    }
}
