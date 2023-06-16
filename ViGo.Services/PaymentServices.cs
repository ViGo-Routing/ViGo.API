using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.Payments;

namespace ViGo.Services
{
    public class PaymentServices : BaseServices
    {

        public PaymentServices(IUnitOfWork work) : base(work)
        {
        }

        #region VnPay
        public async Task<Booking?> VnPayPaymentConfirmAsync(IQueryCollection vnPayData, 
            CancellationToken cancellationToken)
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

                Guid bookingId = Guid.Parse(vnPay.GetResponseData("vnp_TxnRef"));
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
                        Booking booking = await work.Bookings.GetAsync(bookingId, cancellationToken: cancellationToken);
                        if (booking == null)
                        {
                            throw new ApplicationException("Không tìm thấy chuyến đi! Vui lòng kiểm tra lại...");
                        }

                        Wallet wallet = await work.Wallets.GetAsync(w => w.UserId.Equals(booking.CustomerId), cancellationToken: cancellationToken);
                        if (wallet == null)
                        {
                            throw new ApplicationException("Không tìm thấy ví của người dùng!");
                        }

                        WalletTransaction walletTransaction = new WalletTransaction
                        {
                            WalletId = wallet.Id,
                            Amount = vnpAmount,
                            BookingId = bookingId,
                            Type = WalletTransactionType.BOOKING_PAID_BY_VNPAY,
                            Status = WalletTransactionStatus.SUCCESSFULL
                        };

                        booking.Status = BookingStatus.PENDING_MAPPING;
                        booking.PaymentMethod = PaymentMethod.VNPAY;

                        await work.Bookings.UpdateAsync(booking, 
                            isManuallyAssignTracking: true);
                        await work.WalletTransactions.InsertAsync(walletTransaction, 
                            isManuallyAssignTracking: true, cancellationToken: cancellationToken);

                        await work.SaveChangesAsync(cancellationToken);

                        return booking;
                    } else
                    {
                        throw new ApplicationException("Thanh toán lỗi! Mã lỗi: " + vnpResponseCode);
                    }
                } else
                {
                    throw new ApplicationException("Đã có lỗi xảy ra trong quá trình xử lý đơn thanh toán!!");
                }
            }
            return null;
        }

        public string GenerateVnPayTestPaymentUrl(HttpContext context)
        {
            string vnpReturnUrl = ViGoConfiguration.VnPayReturnUrl(context);
            string vnpPaymentUrl = ViGoConfiguration.VnPayPaymentUrl;
            string vnpTmnCode = ViGoConfiguration.VnPayTmnCode;
            string vnpHashSecret = ViGoConfiguration.VnPayHashSecret;
            string vnpApiVersion = ViGoConfiguration.VnPayApiVersion;

            // Generate fake information
            Guid bookingDetailId = Guid.Parse("1e92cc88-e5e5-417f-85ef-f731d1ccb17a");
            double amount = 100000;

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
            vnPay.AddRequestData("vnp_OrderInfo", "Thanh toan chuyen di:" + bookingDetailId);
            vnPay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnPay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
            vnPay.AddRequestData("vnp_TxnRef", bookingDetailId.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            string paymentUrl = vnPay.CreateRequestUrl(vnpPaymentUrl, vnpHashSecret);

            return paymentUrl;
        }
        #endregion
    }
}
