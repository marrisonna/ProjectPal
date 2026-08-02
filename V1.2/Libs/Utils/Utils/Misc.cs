using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace Utils
{
    public class Misc
    {

        public static string RemoveInvalidCharacters(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                return "";

            bool spaceAddedLastTime = false;
            StringBuilder result = new StringBuilder(inputText.Length);
            foreach (char c in inputText)
            {
                int cInt = (int)c;
                if (cInt >= 32)
                {
                    result.Append(c);
                    spaceAddedLastTime = false;
                }
                else
                    if (!spaceAddedLastTime)
                    {
                        spaceAddedLastTime = true;
                        result.Append(' ');
                    }
            }
            return result.ToString();
        }

        static public DateTime AddBusinessDays(DateTime startDate, double daysToAdd)
        {
            return AddBusinessDays(startDate, daysToAdd > 0 ? (int)(daysToAdd + 0.99) : (int)(daysToAdd-0.99), null);
        }

        static public DateTime AddBusinessDays(DateTime startDate, int daysToAdd)
        {
            return AddBusinessDays(startDate, daysToAdd, null);
        }

        static public DateTime GoodBusinessDay(DateTime startDate)
        {
            return GoodBusinessDay(startDate, null);
        }

        static public DateTime GoodBusinessDay(DateTime startDate, HashSet<DateTime> holidays)
        {
            DateTime result = startDate.Date;
            while ((holidays != null && holidays.Contains(result)) ||
                    !IsBusinessDay(result))
                result = result.AddDays(1);
            return result;
        }

        static public DateTime AddBusinessDays(DateTime startDate, int daysToAdd, HashSet<DateTime> holidays)
        {
            DateTime result = GoodBusinessDay(startDate.Date);
            int daysLeftToAdd = daysToAdd;
            while (daysLeftToAdd > 0)
            {
                result = result.AddDays(1);
                while ((holidays != null && holidays.Contains(result)) ||
                    !IsBusinessDay(result))
                    result = result.AddDays(1);
                daysLeftToAdd--;
            }

            while (daysLeftToAdd < 0)
            {
                result = result.AddDays(-1);
                while ((holidays != null && holidays.Contains(result)) ||
                    !IsBusinessDay(result))
                    result = result.AddDays(-1);
                daysLeftToAdd++;
            }
            return result;
        }

        static public int DiffBusinessDays(DateTime endDate, DateTime startDate)
        {
            int multiplier=1;
            if(startDate > endDate)
            {
                DateTime tmp = startDate;
                startDate = endDate;
                endDate = tmp;
                multiplier = -1;
            }
            DateTime currentDate = startDate.Date;
            DateTime finalDate = endDate.Date;
            int dayCount = 0;
            while (startDate < finalDate)
            {
                while (!IsBusinessDay(startDate))
                    startDate = startDate.AddDays(1);
                if (startDate < finalDate)
                {
                    dayCount++;
                    startDate = startDate.AddDays(1);
                }
            }
            return multiplier * dayCount;
        }


        static public bool IsBusinessDay(DateTime theDate)
        {
            if (theDate.DayOfWeek == DayOfWeek.Saturday ||
                theDate.DayOfWeek == DayOfWeek.Sunday)
                return false;
            return true;
        }

        #region Gets the build date and time (by reading the COFF header)

        // http://msdn.microsoft.com/en-us/library/ms680313 

        struct _IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        };

        static public DateTime BuildDateTime
        {
            get
            {
                Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (File.Exists(assembly.Location))
                {
                    var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
                    using (var fileStream = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.Position = 0x3C;
                        fileStream.Read(buffer, 0, 4);
                        fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset 
                        fileStream.Read(buffer, 0, 4); // "PE\0\0" 
                        fileStream.Read(buffer, 0, buffer.Length);
                    }
                    var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));

                        return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(coffHeader.TimeDateStamp * TimeSpan.TicksPerSecond));
                    }
                    finally
                    {
                        pinnedBuffer.Free();
                    }
                }
                return new DateTime();
            }
        }

        #endregion


    }
}
