using System;
using System.Collections.Generic;
using System.Text;
using Utilities.Logging;

namespace Utilities.Helpers
{
    public class DateHelper
    {
        public enum PeriodType { Monthly, Quarterly };

        public static string DateDescription(PeriodType periodType, DateTime periodEnd)
        {
            if (periodType == PeriodType.Monthly)
                return periodEnd.ToString("MMM-yyyy");

            int quarter = 1 + (periodEnd.Month - 1) / 3;
            return "Q" + quarter + " " + periodEnd.ToString("yyyy");

        }
        
        

        public static DateTime QuarterEnd(DateTime inputDate)
        {
            int year = inputDate.Year;
            int month = (((int)((inputDate.Month - 1) / 3)) + 1) * 3;
            int day = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, day);
        }

        public static DateTime MonthEnd(DateTime inputDate)
        {
            int year = inputDate.Year;
            int month = inputDate.Month;
            int day = DateTime.DaysInMonth(year, month);
            return new DateTime(year, month, day);
        }

        public static string BloombergToFingalHolidayCalendar(string bloombergHolidayCalendarName)
        {
            for ( int i = 0; i < s_holidayLookup.Length / 2; i++)
            {
                if (s_holidayLookup[i * 2 + 1] == bloombergHolidayCalendarName)
                    return s_holidayLookup[i * 2];
            }
            return "";
        }

        public static int MonthDiff(DateTime end, DateTime start)
        {
            return end.Year * 12 + end.Month - start.Year * 12 - start.Month;
        }  

        private static string[] s_holidayLookup = {
            "VIE","AS",
            "LON","C5",
            "LON","EN",
            "PAR","F3",
            "PAR","F4",
            "LON","GB",
            "FRA","GE",
            "AMS","H6",
            "MIB","H7",
            "MIL","IT",
            "TOK","JN",
            "LUX","LX",
            "AMS","NL",
            "LIS","PO",
            "MAD","SP",
            "ZUR","SZ",
            "TGT","TE",
            "MAD","U3",
            "MAD","U3",
            "MIL","U4",
            "LIS","U6",
            "DUB","U7",
            "FRA","U8",
            "BRU","U9",
            "BRU","BE",
            "NY","US",
            "MEL","AU",
            "MEL","AX",
            "MEL","U5"};


    }
}
