using System;
using System.Collections.Generic;
using System.Text;
using Security;

namespace Utils
{
    public class SecurityFunctions
    {

        private const string c_prefix = "encypted:";
        private static int c_prefixLength = c_prefix.Length;
        private static byte[] c_prefixBytes;

        static SecurityFunctions()
        {
            c_prefixBytes = new byte[c_prefixLength];
            for (int i = 0; i < c_prefix.Length; i++)
            {
                c_prefixBytes[i] = (byte)c_prefix[i];
            }
        }

        public static bool EncryptionEnabled
        {
            get { return m_encryptionEnabled; }
            set { m_encryptionEnabled = value; }
        }

        private static bool m_encryptionEnabled = true;

        public static object startDate;

        public static string Encrypt(string value)
        {

            if (string.IsNullOrEmpty(value) || !m_encryptionEnabled)
                return value;
            if (value.Length < c_prefixLength || value.Substring(0, c_prefixLength) != c_prefix)
                return c_prefix + Security.Functions.Encrypt(value);
            return value; // already encypted - shouldn't happen
        }


        public static byte[] Encrypt(byte[] value)
        {
            if (value == null || value.Length == 0)
                return value;

            if (!IsEncrypted(value))
            {
                byte[] copy = new byte[value.Length];
                value.CopyTo(copy, 0);
                Security.Functions.EncryptBytes(copy);
                return AddEncryptPrefix(copy);
            }
            return value; // already encypted - shouldn't happen
        }

        private static byte[] AddEncryptPrefix(byte[] value)
        {
            byte[] result = new byte[value.Length + c_prefixLength];
            int pos = 0;
            for (int i = 0; i < c_prefixLength; i++)
                result[pos++] = c_prefixBytes[i];

            for (int i = 0; i < value.Length; i++)
                result[pos++] = value[i];

            return result;
        }



        private static bool IsEncrypted(byte[] value)
        {
            if (value.Length < c_prefixLength)
                return false;

            for (int i = 0; i < c_prefixLength; i++)
                if (value[i] != c_prefixBytes[i])
                    return false;

            return true;
        }

        public static string Decrypt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length < c_prefix.Length || value.Substring(0, c_prefix.Length) != c_prefix)
                return value; // already decrypted - shouldn't happen
            return Security.Functions.Decrypt(value.Substring(c_prefix.Length));
        }


        public static byte[] Decrypt(byte[] value)
        {
            if (value == null || value.Length == 0)
                return value;
            if (!IsEncrypted(value))
                return value;

            int dataLength = value.Length - c_prefixLength;
            byte[] dataPart = new byte[dataLength];
            for (int i = 0; i < dataLength; i++)
            {
                dataPart[i] = value[i + c_prefixLength];
            }
            //value.CopyTo(dataPart, c_prefixLength);

            Security.Functions.DecryptBytes(dataPart);
            return dataPart;
        }
    }
}
