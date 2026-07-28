using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBProjectPal
{

    public enum PriorityValue { _5_High=5, _4_MedHigh=4, _3_Med=3, _2_MedLow=2, _1_Low=1, _0_Closed=0, _0_Cancelled=-1 }



    public enum TaskTypeValue { Enhancement, Maintenance, NewDevelopment, Other, Support, Infrastructure }

    public enum StatusValue { Cancelled=0, Closed=1, InProgress=2, NotStarted=3, Support=4, Tentative=5, Ready=6}

   public enum EffortTypeValue { ManDays, Duration }

    public class Enums
    {
        static public int PriorityValueAsInt(PriorityValue v)
        {
            return (int)v;
        }
    }

}
