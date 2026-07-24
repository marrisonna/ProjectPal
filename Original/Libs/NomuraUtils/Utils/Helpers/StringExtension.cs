using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Utilities.Helpers
{
    public static class StringExtension
    {
        public static bool IsNotNullOrEmpty(this string val)
        {
            return string.IsNullOrEmpty(val) == false;
        }

        public static bool IsNullOrEmpty(this string val)
        {
            return string.IsNullOrEmpty(val);
        }

        public static void AppendFormatLine(this StringBuilder str, string formatStr, params object[] parameters )
        {
            str.AppendFormat(formatStr, parameters);
            str.AppendLine();
        }

        public static bool EqualsIgnoreCase(this string str, string otherStr)
        {
            return
                str.IsNotNullOrEmpty() && 
                otherStr.IsNotNullOrEmpty() &&
                str.ToUpperInvariant().Equals(otherStr.ToUpperInvariant());
        }
    }
}
