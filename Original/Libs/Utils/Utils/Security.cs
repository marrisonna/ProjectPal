using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public class Security
    {

        private const string c_prefix = "encypted";

        public static object startDate;

        public static string Encrypt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (value.Length < c_prefix.Length || value.Substring(0, c_prefix.Length) != c_prefix)
                return c_prefix + EncDec.Encrypt(value, (string)startDate);
            return value; // already encypted - shouldn't happen
        }


        public static byte[] Encrypt(byte[] value)
        {
            if (value == null || value.Length == 0)
                return value;
            return value;
        }


        public static string Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (value.Length < c_prefix.Length || value.Substring(0, c_prefix.Length) != c_prefix)
                return value; // already decrypted - shouldn't happen
            return EncDec.Decrypt(value.Substring(c_prefix.Length), (string)startDate);
        }


        public static byte[] Decrypt(byte[] value)
        {
            if (value == null || value.Length == 0)
                return value;
            return value;
        }
    }
}
