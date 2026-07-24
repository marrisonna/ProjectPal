using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Helpers
{
    public class StringHelper
    {
        public static string GetField(string source, int fieldNumber, char delimiter)
        {
            for (int i = 0; i < fieldNumber; i++)
            {
                int delimIndex1 = source.IndexOf(delimiter);
                if (delimIndex1 == -1)
                    return "";
                source = source.Substring(delimIndex1 + 1);
            }
            int delimIndex2 = source.IndexOf(delimiter);
            if (delimIndex2 == -1)
                return source;
            return source.Substring(0, delimIndex2);
        }



        public static string GetStringAsString(string value)
        {
            if (value == null)
                return "";
            return value.ToString();
        }

        public static string GetDateAsString(DateTime? value)
        {
            if (!value.HasValue)
                return "";
            return value.Value.ToString("dd-MMM-yyyy");
        }
        public static string GetDoubleAsString(double? value)
        {
            if (!value.HasValue)
                return "";
            return value.Value.ToString();
        }
        public static string GetDoubleAsString(double? value, string format)
        {
            if (!value.HasValue)
                return "";
            return value.Value.ToString(format);
        }

        public static double? GetStringAsDouble(string value)
        {
            double? result = null;
            if (value != null && value != "")
                result = Convert.ToDouble(value.Replace(",", "").Trim());
            return result;
        }

        public static string GetIntAsString(int? value)
        {
            if (!value.HasValue)
                return "";
            return value.ToString();
        }

        public static int? GetStringAsInt(string value)
        {
            int? result = null;
            if (value != null && value != "")
                result = Convert.ToInt32(value.Trim());
            return result;
        }

        public static DateTime? GetStringAsDate(string value)
        {
            DateTime? result = null;
            if (value != null && value != "")
                result = Convert.ToDateTime(value.Trim());
            return result;
        }
        
        
    }
}
