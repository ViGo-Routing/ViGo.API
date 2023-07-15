using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ViGo.Utilities.Configuration;

namespace ViGo.Utilities.Payments
{
    public class VnPayLibrary
    {
        private SortedList<string, string> requestData =
            new SortedList<string, string>(new VnPayComparer());
        private SortedList<string, string> responseData =
            new SortedList<string, string>(new VnPayComparer());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            if (responseData.TryGetValue(key, out string? value))
            {
                return value;
            }
            return string.Empty;
        }

        #region Request
        public string CreatePaymentRequestUrl(string baseUrl, string vnpHashSecret)
        {
            StringBuilder data = new StringBuilder();

            foreach (KeyValuePair<string, string> keyValue in requestData)
            {
                if (!string.IsNullOrEmpty(keyValue.Value))
                {
                    data.Append(WebUtility.UrlEncode(keyValue.Key) +
                        "=" + WebUtility.UrlEncode(keyValue.Value)
                        + "&");
                }
            }

            string queryString = data.ToString();
            baseUrl += "?" + queryString;
            string signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }
            string vnpSecureHash = signData.HmacSHA512(vnpHashSecret);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;

            return baseUrl;
        }

        #endregion

        #region Response Process
        public bool IsValidSignature(string inputHash, string secretKey)
        {
            string responseRaw = GetResponseData();
            string myChecksum = responseRaw.HmacSHA512(secretKey);
            return myChecksum.Equals(
                inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            if (responseData.ContainsKey("vnp_SecureHashType"))
            {
                responseData.Remove("vnp_SecureHashType");
            }
            if (responseData.ContainsKey("vnp_SecureHash"))
            {
                responseData.Remove("vnp_SecureHash");
            }
            foreach (KeyValuePair<string, string> keyValue in responseData)
            {
                if (!string.IsNullOrEmpty(keyValue.Value))
                {
                    data.Append(WebUtility.UrlEncode(keyValue.Key)
                        + "=" + WebUtility.UrlEncode(keyValue.Value)
                        + "&");
                }
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
        #endregion

    }

    public class VnPayComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y)
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            CompareInfo vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }

    public class VnPayQueryRequest
    {
        [JsonProperty("vnp_RequestId")]
        public string RequestId { get; set; }

        [JsonProperty("vnp_Version")]
        public string ApiVersion { get; set; }

        [JsonProperty("vnp_Command")]
        public string Command { get; set; }

        [JsonProperty("vnp_TmnCode")]
        public string TmnCode { get; set; }

        [JsonProperty("vnp_TxnRef")]
        public string TxnRef { get; set; }

        [JsonProperty("vnp_OrderInfo")]
        public string OrderInfo { get; set; }

        [JsonProperty("vnp_TransactionNo")]
        public long TransactionNo { get; set; }

        [JsonProperty("vnp_TransactionDate")]
        public string TransactionDate { get; set; }

        [JsonProperty("vnp_CreateDate")]
        public string CreateDate { get; set; }

        [JsonProperty("vnp_IpAddr")]
        public string IpAddress { get; set; }

        [JsonProperty("vnp_SecureHash")]
        public string SecureHash { get; set; }

        public VnPayQueryRequest(string txnRef, long transactionNo,
            DateTime transactionTime,
            HttpContext httpContext)
        {
            RequestId = Guid.NewGuid().ToString();
            ApiVersion = ViGoConfiguration.VnPayApiVersion;
            Command = "querydr";
            TmnCode = ViGoConfiguration.VnPayTmnCode;
            TxnRef = txnRef;
            OrderInfo = "Tra cuu ket qua giao dich";
            TransactionNo = transactionNo;
            TransactionDate = transactionTime.ToString("yyyyMMddHHmmss");
            CreateDate = DateTimeUtilities.GetDateTimeVnNow().ToString("yyyyMMddHHmmss");
            IpAddress = httpContext.GetClientIpAddress();
            SecureHash = getChecksumData(ViGoConfiguration.VnPayHashSecret);
        }

        private string getChecksumData(string secretKey)
        {
            string rawData = RequestId + "|" + ApiVersion + "|" + Command + "|" +
                TmnCode + "|" + TxnRef + "|" + TransactionDate + "|" + CreateDate + "|" +
                IpAddress + "|" + OrderInfo;
            return rawData.HmacSHA256(secretKey);
        }
    }

    public class VnPayQueryResponse
    {
        [JsonProperty("vnp_ResponseId")]
        public string ResponseId { get; set; }

        [JsonProperty("vnp_Command")]
        public string Command { get; set; }

        [JsonProperty("vnp_TmnCode")]
        public string TmnCode { get; set; }

        [JsonProperty("vnp_TxnRef")]
        public string TxnRef { get; set; }

        [JsonProperty("vnp_Amount")]
        public long Amount { get; set; }

        [JsonProperty("vnp_OrderInfo")]
        public string OrderInfo { get; set; }

        [JsonProperty("vnp_ResponseCode")]
        public string ResponseCode { get; set; }

        [JsonProperty("vnp_Message")]
        public string Message { get; set; }

        [JsonProperty("vnp_BankCode")]
        public string BankCode { get; set; }

        [JsonProperty("vnp_PayDate")]
        public string PayDate { get; set; }

        [JsonProperty("vnp_TransactionNo")]
        public long TransactionNo { get; set; }

        [JsonProperty("vnp_TransactionType")]
        public string TransactionType { get; set; }

        [JsonProperty("vnp_TransactionStatus")]
        public string TransactionStatus { get; set; }

        [JsonProperty("vnp_PromotionCode")]
        public string PromotionCode { get; set; }

        [JsonProperty("vnp_PromotionAmount")]
        public long PromotionAmount { get; set; }

        [JsonProperty("vnp_SecureHash")]
        public string SecureHash { get; set; }

        public VnPayQueryResponse()
        {

        }

        private string getChecksumData(string secretKey)
        {
            string rawData = ResponseId + "|" + Command + "|" + ResponseCode + "|" +
                Message + "|" + TmnCode + "|" + TxnRef + "|" + Amount + "|" + BankCode
                 + "|" + PayDate + "|" + TransactionNo + "|" + TransactionType + "|" +
                 TransactionStatus + "|" + OrderInfo + "|" + PromotionCode
                  + "|" + PromotionAmount;
            string checkSum = rawData.HmacSHA256(secretKey);

            return checkSum;
        }

        public bool IsValidResponse(string secretKey)
        {
            return SecureHash.Equals(getChecksumData(secretKey));
        }

    }

    public class VnPayQueryViGoResponse
    {
        public string ResponseId { get; set; }

        public string Command { get; set; }

        //public string TmnCode { get; set; }

        public string TxnRef { get; set; }

        public long Amount { get; set; }

        public string OrderInfo { get; set; }

        public string ResponseCode { get; set; }

        public string Message { get; set; }

        public string BankCode { get; set; }

        public string PayDate { get; set; }

        public long TransactionNo { get; set; }

        public string TransactionType { get; set; }

        public string TransactionStatus { get; set; }

        public string PromotionCode { get; set; }

        public long PromotionAmount { get; set; }


        public VnPayQueryViGoResponse(VnPayQueryResponse response)
        {
            ResponseId = response.ResponseId;
            Command = response.Command;
            //TmnCode = response.TmnCode;
            TxnRef = response.TxnRef;
            Amount = response.Amount;
            OrderInfo = response.OrderInfo;
            ResponseCode = response.ResponseCode;
            Message = response.Message;
            BankCode = response.BankCode;
            PayDate = response.PayDate;
            TransactionNo = response.TransactionNo;
            TransactionType = response.TransactionType;
            TransactionStatus = response.TransactionStatus;
            PromotionCode = response.PromotionCode;
            PromotionAmount = response.PromotionAmount;

        }

    }

}
