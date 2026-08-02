namespace Security
{
    // Ported from the original C++/CLI Security.dll (Original\Libs\Security) so that
    // Utils\SecurityFunctions.cs keeps working unchanged - same namespace, class and
    // method signatures as the native SecurityInternal::Encrypt/Decrypt algorithms.
    public static class Functions
    {
        public static string Encrypt(string s)
        {
            char[] result = new char[s.Length];
            int lastChar = 13;

            for (int i = 0; i < s.Length; i++)
            {
                int c = s[i];
                if (c >= 32 && c < 127)
                {
                    int newLastChar = c;
                    c += lastChar;
                    int d = (c - 32) % 95;
                    c = d + 32;
                    lastChar = newLastChar;
                }
                result[i] = (char)c;
            }

            return new string(result);
        }

        public static string Decrypt(string s)
        {
            char[] result = new char[s.Length];
            int lastChar = 13;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= 32 && c < 127)
                {
                    int d = c - 32 - lastChar;
                    d = d % 95;
                    if (d < 0)
                        d += 95;
                    c = (char)(d + 32);
                    lastChar = c;
                }
                result[i] = c;
            }

            return new string(result);
        }

        public static void EncryptBytes(byte[] s)
        {
            byte lastChar = 13;

            for (int i = 0; i < s.Length; i++)
            {
                byte c = s[i];
                byte newLastChar = c;
                byte a = (byte)i;

                c = (byte)((c ^ lastChar) ^ a);
                s[i] = c;

                lastChar = newLastChar;
            }
        }

        public static void DecryptBytes(byte[] s)
        {
            byte lastChar = 13;

            for (int i = 0; i < s.Length; i++)
            {
                byte c = s[i];
                byte a = (byte)i;

                c = (byte)((c ^ lastChar) ^ a);
                s[i] = c;

                lastChar = c;
            }
        }
    }
}
