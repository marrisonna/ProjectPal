using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.Logging
{
    internal static class LoggerHelper
    {
        internal static LogLevel MapLogLevel(string logLevel)
        {
            if (String.Equals("FATAL", logLevel.ToUpper()))
            {
                return LogLevel.FATAL;
            }
            else if (String.Equals("ERROR", logLevel.ToUpper()))
            {
                return LogLevel.ERROR;
            }
            else if (String.Equals("DEBUG", logLevel.ToUpper()))
            {
                return LogLevel.DEBUG;
            }
            else if (String.Equals("TRACE", logLevel.ToUpper()))
            {
                // TODO implement TRACE logging
                return LogLevel.TRACE;
            }
            else if (String.Equals("INFO", logLevel.ToUpper()))
            {
                return LogLevel.INFO;
            }
            else
            {
                return LogLevel.WARN;
            }
        }
    }
}
