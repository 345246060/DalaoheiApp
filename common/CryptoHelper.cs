using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace common
{
    public class CryptoHelper
    {
        private static byte[] IV = { 12, 34, 56, 78, 90, 102, 114, 126 }; // This can be random, but for simplicity, we're using a fixed value here.

        public static string Encrypt(string plainText, string key)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputArray = Encoding.UTF8.GetBytes(plainText);
                des.Key = Encoding.UTF8.GetBytes(key.Substring(0, 8)); // DES requires 8 byte key
                des.IV = IV;

                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputArray, 0, inputArray.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText, string key)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputArray = Convert.FromBase64String(cipherText);
                des.Key = Encoding.UTF8.GetBytes(key.Substring(0, 8)); // DES requires 8 byte key
                des.IV = IV;

                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputArray, 0, inputArray.Length);
                    cs.FlushFinalBlock();
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}
