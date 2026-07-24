using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Helpers
{
    public static class IListExtension
    {
        public static bool IsNotNullOrEmpty<T>(this IList<T> list)
        {
            return (list != null && list.Count > 0);
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return (list == null || list.Count == 0);
        }
    }
}