using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.BackgroundTasks;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;
using ViGo.Utilities.Payments;

namespace ViGo.Services
{
    public class PaymentServices : BaseServices
    {
        public PaymentServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        #region VnPay
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
                Guid bookingId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef").Split("PhongNT@")[0]);
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
                        _logger.LogInformation("Booking has been paid successfully!! BookingId={0}, VNPay TransactionId={1}", bookingId, vnPayTransactionId);
                    }
                    else
                    {
                        //throw new ApplicationException("Thanh toán VNPay lỗi! Mã lỗi: " + vnpResponseCode);
                        message = "Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: " + vnpResponseCode;

                        _logger.LogInformation("Booking failed to be paid!! " +
                            "BookingId={0}, VNPay TransactionId={1}, ResponseCode={3}",
                            bookingId, vnPayTransactionId, vnpResponseCode);
                    }

                }
                else
                {
                    message = "Có lỗi xảy ra trong quá trình xử lý!";

                    _logger.LogInformation("Invalid signature!! BookingId={0}, InputData={1}",
                        bookingId, requestRawUrl);

                    //throw new ApplicationException("Đã có lỗi xảy ra trong quá trình xử lý đơn thanh toán!!");
                }
            }
            else
            {
                message = "Thông tin không hợp lệ!";
            }
            return message;
        }

        public async Task<(string code, string message)> VnPayPaymentIpnAsync(
            string requestRawUrl,
            IQueryCollection vnPayData,
            IBackgroundTaskQueue backgroundTaskQueue,
            IServiceScopeFactory serviceScopeFactory,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Begin VNPay IPN...");
            (string code, string message) = (string.Empty, string.Empty);

            Booking? booking = null;
            string? fcmToken = null;

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
                    Guid bookingId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef").Split("PhongNT@")[0]);
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
                        booking = await work.Bookings.GetAsync(bookingId, cancellationToken: cancellationToken);
                        if (booking is null)
                        {
                            //throw new ApplicationException("Không tìm thấy chuyến đi! Vui lòng kiểm tra lại...");
                            code = "01";
                            message = "Booking is not found!!";
                        }
                        else
                        {
                            if (booking.PriceAfterDiscount.HasValue &&
                                booking.PriceAfterDiscount.Value != vnpAmount)
                            {
                                code = "04";
                                message = "Invalid amount!";
                            }
                            else
                            {
                                if (booking.Status == BookingStatus.UNPAID)
                                {
                                    // Get SYSTEM WALLET
                                    Wallet systemWallet = await work.Wallets.GetAsync(w =>
                                        w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                                    if (systemWallet is null)
                                    {
                                        throw new Exception("Chưa có ví dành cho hệ thống!!");
                                    }

                                    WalletTransaction systemTransaction_Add = new WalletTransaction
                                    {
                                        WalletId = systemWallet.Id,
                                        Amount = vnpAmount,
                                        BookingId = bookingId,
                                        Type = WalletTransactionType.BOOKING_TOPUP_BY_MOMO,
                                        //Status = WalletTransactionStatus.SUCCESSFULL,
                                        CreatedBy = booking.CustomerId,
                                        UpdatedBy = booking.CustomerId
                                    };

                                    Wallet wallet = await work.Wallets.GetAsync(w => w.UserId.Equals(booking.CustomerId), cancellationToken: cancellationToken);
                                    if (wallet == null)
                                    {
                                        throw new ApplicationException("Không tìm thấy ví của người dùng!");
                                    }

                                    WalletTransaction walletTransaction_Topup = new WalletTransaction
                                    {
                                        WalletId = wallet.Id,
                                        Amount = vnpAmount,
                                        BookingId = bookingId,
                                        Type = WalletTransactionType.BOOKING_TOPUP_BY_MOMO,
                                        //Status = WalletTransactionStatus.SUCCESSFULL,
                                        CreatedBy = booking.CustomerId,
                                        UpdatedBy = booking.CustomerId
                                    };
                                    WalletTransaction walletTransaction_Paid = new WalletTransaction
                                    {
                                        WalletId = wallet.Id,
                                        Amount = vnpAmount,
                                        BookingId = bookingId,
                                        Type = WalletTransactionType.BOOKING_PAID_BY_VNPAY,
                                        //Status = WalletTransactionStatus.SUCCESSFULL,
                                        CreatedBy = booking.CustomerId,
                                        UpdatedBy = booking.CustomerId
                                    };

                                    if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
                                    {
                                        systemTransaction_Add.Status = WalletTransactionStatus.SUCCESSFULL;
                                        //systemTransaction_Add.Amount += vnpAmount;

                                        walletTransaction_Topup.Status = WalletTransactionStatus.SUCCESSFULL;
                                        walletTransaction_Paid.Status = WalletTransactionStatus.SUCCESSFULL;

                                        booking.Status = BookingStatus.PENDING_MAPPING;
                                        booking.PaymentMethod = PaymentMethod.VNPAY;

                                        _logger.LogInformation("Booking has been paid successfully!! BookingId={0}, VNPay TransactionId={1}", bookingId, vnPayTransactionId);
                                    }
                                    else
                                    {
                                        //throw new ApplicationException("Thanh toán VNPay lỗi! Mã lỗi: " + vnpResponseCode);
                                        systemTransaction_Add.Status = WalletTransactionStatus.FAILED;
                                        walletTransaction_Topup.Status = WalletTransactionStatus.FAILED;
                                        walletTransaction_Paid.Status = WalletTransactionStatus.FAILED;

                                        booking.PaymentMethod = PaymentMethod.VNPAY;

                                        _logger.LogInformation("Booking failed to be paid!! " +
                                            "BookingId={0}, VNPay TransactionId={1}, ResponseCode={3}",
                                            bookingId, vnPayTransactionId, vnpResponseCode);
                                    }

                                    await work.Bookings.UpdateAsync(booking,
                                            isManuallyAssignTracking: true);

                                    // Add Wallet Transaction
                                    await work.WalletTransactions.InsertAsync(systemTransaction_Add,
                                        isManuallyAssignTracking: true, cancellationToken: cancellationToken);
                                    await work.WalletTransactions.InsertAsync(walletTransaction_Topup,
                                        isManuallyAssignTracking: true, cancellationToken: cancellationToken);
                                    await work.WalletTransactions.InsertAsync(walletTransaction_Paid,
                                        isManuallyAssignTracking: true, cancellationToken: cancellationToken);

                                    await work.SaveChangesAsync(cancellationToken);

                                    // TODO Code

                                    // Run trip mapping
                                    await backgroundTaskQueue.QueueBackGroundWorkItemAsync(async token =>
                                    {
                                        await using (var scope = serviceScopeFactory.CreateAsyncScope())
                                        {
                                            IUnitOfWork work = new UnitOfWork(scope.ServiceProvider);
                                            TripMappingServices tripMappingServices = new TripMappingServices(work, _logger);
                                            await tripMappingServices.MapBooking(booking, _logger);
                                        }
                                    });

                                    // Send notification to user
                                    User user = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

                                    fcmToken = user.FcmToken;
                                    if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                                    {
                                        if (systemTransaction_Add.Status == WalletTransactionStatus.SUCCESSFULL)
                                        {
                                            await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thành công",
                                            "Quý khách đã thực hiện thanh toán đơn đặt chuyến đi bằng VNPay thành công!!", cancellationToken: cancellationToken);

                                            // Send data to mobile application
                                            await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, new Dictionary<string, string>()
                                                {
                                                    { "bookingId", booking.Id.ToString() },
                                                    { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                                                    { "isSuccess", "true" },
                                                    { "message", "Thanh toán bằng VNPay thành công!" }
                                                }, cancellationToken);
                                        }
                                        else
                                        {
                                            // FAILED
                                            await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thất bại",
                                           "Giao dịch thực hiện thanh toán đơn đặt chuyến đi bằng VNPay của quý khách đã bị thất bại!!", cancellationToken: cancellationToken);

                                            // Send data to mobile application
                                            await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, new Dictionary<string, string>()
                                                {
                                                    { "bookingId", booking.Id.ToString() },
                                                    { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                                                    { "isSuccess", "false" },
                                                    { "message", "Thanh toán bằng VNPay thất bại!" }
                                                }, cancellationToken);
                                        }

                                    }

                                    code = "00";
                                    message = "Confirm success";
                                }
                                else
                                {
                                    code = "02";
                                    message = "Booking has been already paid!!";
                                }
                            }

                        }

                    }
                    else
                    {
                        code = "97";
                        message = "Invalid checksum";

                        _logger.LogInformation("Invalid signature!! BookingId={0}, InputData={1}",
                            bookingId, requestRawUrl);

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

                if (booking != null && fcmToken != null
                    && !string.IsNullOrEmpty(fcmToken))
                {
                    await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thất bại",
                                           "Giao dịch thực hiện thanh toán đơn đặt chuyến đi bằng VNPay của quý khách đã bị thất bại!!", cancellationToken: cancellationToken);

                    // Send data to mobile application
                    await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, new Dictionary<string, string>()
                    {
                        { "bookingId", booking.Id.ToString() },
                        { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                        { "isSuccess", "false" },
                        { "message", "Thanh toán bằng VNPay thất bại!" }
                    }, cancellationToken);
                }

                return ("99", "Unknown Error: " + ex.GeneratorErrorMessage());
            }

            _logger.LogInformation("VNPay IPN: Code: {code}, message: {message}", code, message);
            return (code, message);
        }

        public string GenerateVnPayTestPaymentUrl(HttpContext context, Guid bookingId, double amount)
        {
            string vnpReturnUrl = ViGoConfiguration.VnPayReturnUrl(context);
            string vnpPaymentUrl = ViGoConfiguration.VnPayPaymentUrl;
            string vnpTmnCode = ViGoConfiguration.VnPayTmnCode;
            string vnpHashSecret = ViGoConfiguration.VnPayHashSecret;
            string vnpApiVersion = ViGoConfiguration.VnPayApiVersion;

            // Generate fake information
            //Guid bookingDetailId = Guid.Parse("EAC6836E-1CF0-4064-B01C-BD5F1B298EBC");

            VnPayLibrary vnPay = new VnPayLibrary();

            vnPay.AddRequestData("vnp_Version", vnpApiVersion);
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", vnpTmnCode);
            vnPay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());

            vnPay.AddRequestData("vnp_CreateDate", DateTimeUtilities.GetDateTimeVnNow()
                .ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", "VND");
            vnPay.AddRequestData("vnp_IpAddr", context.GetClientIpAddress());
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_OrderInfo", "Thanh toan chuyen di:" + bookingId);
            vnPay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnPay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
            //vnPay.AddRequestData("vnp_TxnRef", bookingDetailId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày
            vnPay.AddRequestData("vnp_TxnRef", bookingId.ToString() + "PhongNT@" + Guid.NewGuid().ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            string paymentUrl = vnPay.CreateRequestUrl(vnpPaymentUrl, vnpHashSecret);

            return paymentUrl;
        }
        #endregion
    }
}
