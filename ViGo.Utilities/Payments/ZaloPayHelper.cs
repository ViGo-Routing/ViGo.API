using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.WalletTransactions;
using ViGo.Utilities.Configuration;

namespace ViGo.Utilities.Payments
{
    public static class ZaloPayHelper
    {
        public static bool VerifyCallback(string data, string requestMac)
        {
            try
            {
                string mac = HashingUtilities.HmacSHA256(data, ViGoConfiguration.ZaloPayKey2);
                return requestMac.Equals(mac);
            } catch
            {
                return false;
            }
        }
    }

    public class ZaloPayOrderCreateModel
    {
        [JsonProperty("app_id")]

        public int AppId { get; set; }

        [JsonProperty("app_user")]
        public string AppUser { get; set; }

        [JsonProperty("app_trans_id")]
        public string AppTransactionId { get; set; }

        [JsonProperty("app_time")]
        public long AppTime { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("embed_data")]
        public string EmbedData { get; set; }

        [JsonProperty("bank_code")]
        public string BankCode { get; private set; }

        [JsonProperty("mac")]

        public string Mac { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        public ZaloPayOrderCreateModel(TopupTransactionCreateModel transaction,
            Guid id, HttpContext context) 
        {
            //string transactionId = Convert.ToBase64String(id.ToByteArray());
            string transactionId = HashingUtilities.ToBase64String(id);

            AppId = ViGoConfiguration.ZaloPayAppId;
            AppTransactionId = DateTimeUtilities.GetDateTimeVnNow().ToString("yyMMdd")
                + "_" + transactionId;
            AppUser = transaction.UserId.HasValue ? transaction.UserId.Value.ToString() : "";
            AppTime = DateTimeUtilities.GetTimeStamp();
            Amount = (long)transaction.Amount;
            Item = ZaloPayItem.GenerateItem(transaction.Amount);
            //Item = "[]";

            Description = "Topup - Vigo - Thanh toán yêu cầu Topup";
            EmbedData = ZaloPayEmbedData.GenerateEmbedData(context);
            //EmbedData = "{}";

            BankCode = "zalopayapp";

            Mac = Hmac512Mac();
            Title = "Thanh toán Topup - ViGo";
        }

        public string GetMacData()
        {
            return AppId + "|" + AppTransactionId + "|" + AppUser + "|" + Amount + "|" + AppTime + "|" + EmbedData + "|" + Item;
        }

        public string Hmac512Mac()
        {
            return GetMacData().HmacSHA256(ViGoConfiguration.ZaloPayKey1);
        }
    }

    public class ZaloPayCallbackModel
    {
        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("mac")]
        public string Mac { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

    }

    public class ZaloPayCallbackResponse
    {
        [JsonProperty("return_code")]
        public int ReturnCode { get; set; }

        [JsonProperty("return_message")]
        public string ReturnMessage { get; set; }

        public ZaloPayCallbackResponse()
        {
        }

        public ZaloPayCallbackResponse(int returnCode, string returnMessage)
        {
            ReturnCode = returnCode;
            ReturnMessage = returnMessage;
        }
    }

    public class ZaloPayCallbackData
    {
        [JsonProperty("app_id")]
        public int AppId { get; set; }

        [JsonProperty("app_user")]
        public string AppUser { get; set; }

        [JsonProperty("app_trans_id")]
        public string AppTransactionId { get; set; }

        [JsonProperty("app_time")]
        public long AppTime { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("item")]
        public string Item { get; set; }

        [JsonProperty("embed_data")]
        public string EmbedData { get; set; }

        [JsonProperty("zp_trans_id")]
        public long ZaloPayTransactionId { get; set; }

        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("channel")]
        public int Channel { get; set; }

        [JsonProperty("merchant_user_id")]
        public string MerchantUserId { get; set; }

        [JsonProperty("user_fee_amount")]
        public long UserFeeAmount { get; set; }

        [JsonProperty("discount_amount")]
        public long DiscountAmount { get; set; }
    }

    public class ZaloPayQueryModel
    {
        [JsonProperty("app_id")]
        public int AppId { get; set; }

        [JsonProperty("app_trans_id")]
        public string AppTransactionId { get; set; }

        [JsonProperty("mac")]
        public string Mac { get; set; }

        public ZaloPayQueryModel(string transactionId, HttpContext httpContext)
        {
            string appTransactionId = transactionId;

            AppId = ViGoConfiguration.ZaloPayAppId;
            AppTransactionId = appTransactionId;

            Mac = Hmac512Mac();
        }

        public string GetMacData()
        {
            return AppId + "|" + AppTransactionId + "|" + ViGoConfiguration.ZaloPayKey1;
        }

        public string Hmac512Mac()
        {
            return GetMacData().HmacSHA256(ViGoConfiguration.ZaloPayKey1);
        }
    }

    public class ZaloPayQueryResponse
    {
        [JsonProperty("return_code")]
        public int ReturnCode { get; set; }

        [JsonProperty("return_message")]
        public string ReturnMessage { get; set; }

        [JsonProperty("sub_return_code")]
        public int SubReturnCode { get; set; }

        [JsonProperty("sub_return_message")]
        public string SubReturnMessage { get; set; }

        [JsonProperty("is_processing")]
        public bool IsProcessing { get; set; }

        [JsonProperty("amount")]
        public bool Amount { get; set; }

        [JsonProperty("zp_trans_token")]
        public string ZaloPayTransactionToken { get; set; }
    }

    public class ZaloPayQueryViGoResponse
    {
        public int ReturnCode { get; set; }

        public string ReturnMessage { get; set; }

        public int SubReturnCode { get; set; }

        public string SubReturnMessage { get; set; }

        public bool IsProcessing { get; set; }

        public bool Amount { get; set; }

        public string ZaloPayTransactionToken { get; set; }

        public ZaloPayQueryViGoResponse(ZaloPayQueryResponse response)
        {
            ReturnCode = response.ReturnCode;
            ReturnMessage = response.ReturnMessage;
            SubReturnCode = response.SubReturnCode;
            SubReturnMessage = response.SubReturnMessage;
            IsProcessing = response.IsProcessing;
            Amount = response.Amount;
            ZaloPayTransactionToken = response.ZaloPayTransactionToken;
        }
    }

    public class ZaloPayEmbedData
    {
        [JsonProperty("redirecturl")]
        public string RedirectUrl { get; private set; }

        public static string GenerateEmbedData(HttpContext httpContext)
        {
            ZaloPayEmbedData embedData = new ZaloPayEmbedData()
            {
                RedirectUrl = ViGoConfiguration.ZaloPayCallback(httpContext)
            };
            return JsonConvert.SerializeObject(embedData);
        }
    }

    public class ZaloPayItem
    {
        public string Name { get; private set; }

        public static string GenerateItem(double amount)
        {
            IEnumerable<ZaloPayItem> items = new List<ZaloPayItem>
            {
                new ZaloPayItem()
                {
                    Name = "Topup " + amount + "VND"
                }
            };
            return JsonConvert.SerializeObject(items);
        }
    }

    public class ZaloPayCreateOrderResponse
    {
        [JsonProperty("return_code")]
        public int ReturnCode { get; set; }

        [JsonProperty("return_message")]
        public string ReturnMessage { get; set; }

        [JsonProperty("sub_return_code")]
        public int SubReturnCode { get; set; }

        [JsonProperty("sub_return_message")]
        public string SubReturnMessage { get; set; }

        [JsonProperty("order_url")]
        public string OrderUrl { get; set; }

        [JsonProperty("zp_trans_token")]
        public string ZaloPayTransactionToken { get; set; }

    }
}
