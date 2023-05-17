using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ViGo.Utilities.Configuration;

namespace ViGo.Utilities
{
    public static class SecurityUtilities
    {
        public static string Encrypt(this string stringToEncrypt)
        {
            /*
             * 
             * Encryption Logic:
             * 
             * Pass clear text it will return the ciphered text
             * 
            */
            string passPhrase = ViGoConfiguration.SecurityPassPhrase; // can be any string

            string saltValue = ViGoConfiguration.SecuritySalt;  // can be any string

            string hashAlgorithm = ViGoConfiguration.SecurityAlgorithm; // either "MD5"  or "SHA1"

            int passwordIterations = ViGoConfiguration.SecurityPasswordIterations; // can be any number

            string initVector = ViGoConfiguration.SecurityInitVector; // must be 16 bytes (Characters)

            int keySize = ViGoConfiguration.SecurityKeySize; // can be 192 or 128

            /*All the above values must be same in both encryption and decryption functions  */

            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);

            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(stringToEncrypt.ToString());

            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, saltValueBytes, hashAlgorithm, passwordIterations);

            byte[] keyBytes = password.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();

            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);

            MemoryStream memoryStream = new MemoryStream();

            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);

            cryptoStream.FlushFinalBlock();

            byte[] cipherTextBytes = memoryStream.ToArray();

            memoryStream.Close();

            cryptoStream.Close();

            string cipherText = Convert.ToBase64String(cipherTextBytes);

            return cipherText;
        }

        public static string Decrypt(this string stringToDecrypt)
        {
            /*
             * 
             * Decryption logic:
             * 
             * Pass ciphered text to this function, it will return the clear text
             * 
            */

            string passPhrase = ViGoConfiguration.SecurityPassPhrase; // can be any string

            string saltValue = ViGoConfiguration.SecuritySalt;  // can be any string

            string hashAlgorithm = ViGoConfiguration.SecurityAlgorithm; // either "MD5"  or "SHA1"

            int passwordIterations = ViGoConfiguration.SecurityPasswordIterations; // can be any number

            string initVector = ViGoConfiguration.SecurityInitVector; // must be 16 bytes (Characters)

            int keySize = ViGoConfiguration.SecurityKeySize; // can be 192 or 128

            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);

            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            byte[] cipherTextBytes = Convert.FromBase64String(stringToDecrypt.ToString());

            PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, saltValueBytes, hashAlgorithm, passwordIterations);

            byte[] keyBytes = password.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();

            symmetricKey.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);

            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);

            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

            memoryStream.Close();

            cryptoStream.Close();

            string plainText = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);

            return plainText;
        }
    }
}
