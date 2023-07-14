using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Utilities
{
    public static class HashingUtilities
    {
        public static string HmacSHA512(this string inputData, string key)
        {
            StringBuilder hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        public static string HmacSHA256(this string inputData, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            byte[] hashMessage = new HMACSHA256(keyBytes).ComputeHash(inputBytes);

            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }

        //public static bool ValidateHmacSHA512(this string inputHashToValidate,
        //    string rawData, string key)
        //{
        //    string checksum = rawData.HmacSHA512(key);
        //    return checksum.Equals(inputHashToValidate, StringComparison.InvariantCultureIgnoreCase);
        //}

        public static string ToBase64String(Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray())
                .Substring(0, 22)
                .Replace("/", "_")
                .Replace("+", "-");
        }

        public static Guid FromBase64String(string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || 
                base64String.Length != 22)
            {
                throw new FormatException("Input string was not in a correct format!!");
            }

            return new Guid(Convert.FromBase64String(base64String
                .Replace("_", "/")
                .Replace("-", "+") + "=="));
        }
    }
}
