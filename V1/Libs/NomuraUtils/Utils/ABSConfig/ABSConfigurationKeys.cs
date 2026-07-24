using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Configuration
{
    /// <summary>
    /// I AM NOT SURE IF THIS IS THE RIGHT PLACE TO STORE KEYS- REASON BEING THIS IS A SHARED COMPONENT
    /// AND AS NEW KEYS KEEP ON GETTTING ADDED, MORE RELEASES WILL HAVE TO BE MADE WHICH IS NOT ADVISABLE.
    /// </summary>
    public class ABSConfigurationKeys
    {
        //Database Keys in ABSSystemConfig DB
        public const string MatrixDatabase = "MatrixDB";
        public const string DealsDatabase = "DealsDB";

        //Log Directory Keys in ABSSystemConfig DB - User by the Logger
        public const string LogDirectory = "LogDir";
        public const string AlternateLogDirectory = "AltLogDir";

        //Logger's config file directories
        public const string LoggerConfigurationDirectory = "LogConfigDir";
        public const string AlternateLoggerConfigurationDirectory = "AltLogConfigDir";



    }
}
