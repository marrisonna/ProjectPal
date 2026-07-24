using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Helpers
{
    public class TotoroHelper
    {
        //  Ideally need a less hard coded way to do this, 
        //  but this will do to start off with.
        public static string MapToTotoroSector(string dbSector)
        {
            for (int i = 0; i < s_totoroSectorMap.Length / 2; i++)
            {
                if (dbSector == s_totoroSectorMap[i * 2])
                    return s_totoroSectorMap[i * 2 + 1];
            }
            return "";
        }

        private static string[] s_totoroSectorMap = {
            "UK Prime","Prime RMBS",
            "UK Non-conform","Non Conforming RMBS",
            "Dutch RMBS","Prime RMBS",
            "Italian RMBS","Prime RMBS",
            "Spanish RMBS","Prime RMBS",
            "Autos","Autos",
            "UK credit cards","Credit Cards",
            "Paneuro CMBS","CMBS",
            "UK CMBS","CMBS",
            "Italian Leases","Other ABS",
            "UK buy-to-let","Non Conforming RMBS",
            "Other","Other ABS",
            "Spanish SME CLO","Other ABS",
            "French RMBS","Prime RMBS",
            "Portuguese RMBS","Prime RMBS",
            "German RMBS","Prime RMBS",
            "Australian RMBS","Prime RMBS",
            "Irish RMBS","Prime RMBS",
            "Greek RMBS","Prime RMBS",
            "Dutch SME","Other ABS"
            };

    }
}
