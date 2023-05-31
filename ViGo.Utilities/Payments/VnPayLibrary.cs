using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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
        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
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
}
