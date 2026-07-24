using System;
using System.Collections.Generic;
using System.Text;
using Utilities.Configuration;

namespace Utilities.Logging
{
    internal static class LoggerConfiguration
    {
        #region constants
        private const string LogDirectory = "LogDirectory";
        private const string AlternateLogDirectory = "AlternateLogDirectory";
        private const string LoggerConfigDirectory = "LoggerConfigDirectory";
        private const string AlternateLoggerConfigDirectory = "AlternateLoggerConfigDirectory";

        #endregion 

        #region Properties
        static internal  List<String>  LogFilePaths
        {
            get
            {
                //Obtain the log directories using the Config Class
                
                List<string> alternativepaths = new List<string>();

                string logPathSetting = ABSConfig.GetValue(LogDirectory);
                if (!string.IsNullOrEmpty(logPathSetting))
                {
                    string[] logPaths = logPathSetting.Split(new char[] { '*' });
                    foreach (string logPath in logPaths)
                    {
                        alternativepaths.Add(logPath.Trim());
                    }

                }

                string legacyAlternateLogDirectory = ABSConfig.GetValue(AlternateLogDirectory);
                if (!string.IsNullOrEmpty(legacyAlternateLogDirectory))
                {
                    alternativepaths.Add(ABSConfig.GetValue(AlternateLogDirectory).Trim());
                }
                
                //the default path
                alternativepaths.Add("c:\\logs");
                alternativepaths.Add(".");

              

                Logger.Debug("Possible log paths are:-");
                foreach (string path in alternativepaths)
                {
                    Logger.Debug("Log path: '{0}'",path);
                }
                
                return alternativepaths;                

            }
        }       
      

        static internal List<String> LoggerConfigFilePaths
        {
            get
            {
                List<string> configurationFilePaths = new List<string>();

                //Obtain the log directories using the Config Class
                if (!String.Equals(ABSConfig.GetValue(LoggerConfigDirectory), String.Empty))
                {
                    configurationFilePaths.Add(ABSConfig.GetValue(LoggerConfigDirectory));
                }

                if (!String.Equals(ABSConfig.GetValue(AlternateLoggerConfigDirectory), String.Empty))
                {
                    configurationFilePaths.Add(ABSConfig.GetValue(AlternateLoggerConfigDirectory));
                }

              
                return configurationFilePaths;
            }
        }

        static internal string RollingAppenderName
        {
            get
            {
                return "RollingFile";
            }
        }
        #endregion

    }
}
