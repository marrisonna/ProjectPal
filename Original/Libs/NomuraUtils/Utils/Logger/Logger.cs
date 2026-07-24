using System;
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.IO;
using log4net;
using log4net.Config;
using log4net.Appender;
using System.Reflection;
using System.Xml;

namespace Utilities.Logging
{
    public enum LogLevel { TRACE, DEBUG, INFO, WARN, ERROR, FATAL };

    //<summary>
    // A common logger class for common people
    // The logger is currently configured to use
    // a config file with a fixed logger name.
    // if you want to separate out the logger name then please use
    // CustomLogger
    // usage:
    //  See unit test cases
    // </summary>
    public class Logger
    {
        #region Constants
        const string LoggerName = "ApplicationLogger";
        #endregion

        #region "member variables/properties"

        private static ILogWrapper m_Log4NetLogger;
        private static string m_logFileName = String.Empty;
        private static string m_logFilePath = String.Empty;
        private static string m_loggerConfigFileFullName = String.Empty; //Name includes the path        
        private static string m_applicationName = String.Empty;
        private static string m_userName = String.Empty;
        private static DateTime m_logStartTime = DateTime.Now;
        # endregion

        #region private properties
        /// <summary>
        /// 
        /// </summary>
        private static string ApplicationName
        {
            get
            {
                if (m_applicationName == String.Empty)
                {
                    m_applicationName = Utilities.Helpers.CommonHelper.ApplicationName;
                }
                return m_applicationName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static string UserName
        {
            get
            {
                if (m_userName == String.Empty)
                {
                    m_userName = System.Environment.UserName;
                }
                return m_userName;
            }
        }

        #endregion

        #region "public properties"
        public static string CurrentLogFilePath
        {
            get
            {
                return m_logFilePath;
            }
        }
        #endregion

        #region Public Methods


        // <summary>
        // Returns true if log level is IsDebugEnabled 
        // </summary>
        public static bool IsTraceEnabled()
        {
            return GetLogger().IsTraceEnabled;
        }

        // <summary>
        // Returns true if log level is IsDebugEnabled 
        // </summary>
        public static bool IsDebugEnabled()
        {
            return GetLogger().IsDebugEnabled;
        }

        // <summary>
        // Returns true if log level is INFO or lower
        // </summary>
        public static bool IsInfoEnabled()
        {
            return GetLogger().IsInfoEnabled;
        }

        // <summary>
        // Returns true if log level is warn or lower
        // </summary>
        public static bool IsWarnEnabled()
        {
            return GetLogger().IsWarnEnabled;
        }

        // <summary>
        // Returns true if log level is ERROR or lower
        // </summary>
        public static bool IsErrorEnabled()
        {
            return GetLogger().IsErrorEnabled;
        }

        // <summary>
        // Returns true if log level is IsFatalEnabled or lower
        // </summary>
        public static bool IsFatalEnabled()
        {
            return GetLogger().IsFatalEnabled;
        }

        // <summary>
        // Dumps IsTraceEnabled message
        // </summary>
        public static void Trace(string formatString, params object[] parameters)
        {
            Log(LogLevel.TRACE, formatString, parameters);
        }

        // <summary>
        // Dumps IsDebugEnabled message
        // </summary>
        public static void Debug(string formatString, params object[] parameters)
        {
            Log(LogLevel.DEBUG, formatString, parameters);
        }

        // <summary>
        // Dumps IsInfoEnabled/IsTraceEnabled message
        // </summary>
        public static void Info(string formatString, params object[] parameters)
        {
            Log(LogLevel.INFO, formatString, parameters);
        }

        // <summary>
        // Dumps message with IsWarnEnabled log level
        // </summary>
        public static void Warn(string formatString, params object[] parameters)
        {
            Log(LogLevel.WARN, formatString, parameters);
        }

        // <summary>
        // Dumps message with IsErrorEnabled log level
        // </summary>
        public static void Error(string formatString, params object[] parameters)
        {
            Log(LogLevel.ERROR, formatString, parameters);
        }

        // <summary>
        // Dumps message with IsWarnEnabled log level
        // </summary>
        public static void Fatal(string formatString, params object[] parameters)
        {
            Log(LogLevel.FATAL, formatString, parameters);
        }

        /// <summary>
        /// !!!Depricated!!!
        /// Dumps Exception message with IsWarnEnabled log level
        /// use this when catching exceptions
        /// </summary>
        [Obsolete("Use the version of this method that requires a message.")]
        public static void WarnException(Exception t)
        {
            if (GetLogger().IsWarnEnabled)
            {
                Log(LogLevel.WARN, t, "");
            }
        }

        /// <summary>
        /// Dumps Exception message with IsWarnEnabled log level
        /// use this when catching exceptions
        /// </summary>
        public static void WarnException(Exception t,string formatString,params object[] parameters)
        {
            if(GetLogger().IsWarnEnabled)
            {
                Log(LogLevel.WARN,t,formatString,parameters);
            }
        }

        /// <summary>
        /// !!!Depricated!!!
        /// Dumps Exception message with IsErrorEnabled log level
        /// use this when catching exceptions
        /// </summary>
        [Obsolete("Use the version of this method that requires a message.")]
        public static void ErrorException(Exception t)
        {
            if (GetLogger().IsErrorEnabled)
            {
                Log(LogLevel.ERROR, t, "");
            }
        }

        /// <summary>
        /// Dumps Exception message with IsErrorEnabled log level
        /// use this when catching exceptions
        /// </summary>
        public static void ErrorException(Exception t,string formatString,params object[] parameters)
        {
            if(GetLogger().IsErrorEnabled)
            {
                Log(LogLevel.ERROR,t,formatString,parameters);
            }
        }

        // <summary>
        /// !!!Depricated!!!
        // Dumps Exception message with IsFatalEnabled log level
        // use this when catching exceptions
        // </summary>
        [Obsolete("Use the version of this method that requires a message.")]
        public static void FatalException(Exception t)
        {
            if (GetLogger().IsFatalEnabled)
            {
                Log(LogLevel.FATAL, t, "");
            }
        }

        /// <summary>
        /// Dumps Exception message with IsFatalEnabled log level
        /// use this when catching exceptions
        /// </summary>
        public static void FatalException(Exception t, string formatString, params object[] parameters)
        {
            if(GetLogger().IsFatalEnabled)
            {
                Log(LogLevel.FATAL,t,formatString,parameters);
            }
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // Use this to throw exceptions.
        // Depending on the log level it shall optionally log 
        // but shall always throw the exception
        // </summary>
        public static void TraceThrow(Exception t)
        {
            if (GetLogger().IsTraceEnabled)
            {
                Log(LogLevel.TRACE, t, "");
            }
            throw t;
        }

        //// <summary>
        //// Optionally Logs exception and rethrows it
        //// Use this to throw exceptions.
        //// Depending on the log level it shall optionally log 
        //// but shall always throw the exception
        //// </summary>
        //public static void TraceThrow(Exception t,string formatString,params object[] parameters)
        //{
        //    if(GetLogger().IsDebugEnabled)
        //    {
        //        Log(LogLevel.DEBUG,t,formatString,parameters);
        //    }
        //    throw t;
        //}

        // <summary>
        // Optionally Logs exception and rethrows it
        // Use this to throw exceptions.
        // Depending on the log level it shall optionally log 
        // but shall always throw the exception
        // </summary>
        public static void DebugThrow(Exception t)
        {
            if (GetLogger().IsDebugEnabled)
            {
                Log(LogLevel.DEBUG, t, "");
            }
            throw t;
        }

        //// <summary>
        //// Optionally Logs exception and rethrows it
        //// Use this to throw exceptions.
        //// Depending on the log level it shall optionally log 
        //// but shall always throw the exception
        //// </summary>
        //public static void DebugThrow(Exception t,string formatString,params object[] parameters)
        //{
        //    if(GetLogger().IsDebugEnabled)
        //    {
        //        Log(LogLevel.DEBUG,t, formatString, parameters);
        //    }
        //    throw t;
        //}

        // <summary>
        // Optionally Logs exception and rethrows it
        // Use this to throw exceptions.
        // Depending on the log level it shall optionally log 
        // but shall always throw the exception
        // </summary>
        public static void InfoThrow(Exception t)
        {
            if (GetLogger().IsInfoEnabled)
            {
                Log(LogLevel.INFO, t, "");
            }
            throw t;
        }

        //// <summary>
        //// Optionally Logs exception and rethrows it
        //// </summary>
        //public static void InfoThrow(Exception t,string formatString,params object[] parameters)
        //{
        //    if(GetLogger().IsInfoEnabled)
        //    {
        //        Log(LogLevel.INFO,t,formatString,parameters);
        //    }
        //    throw t;
        //}

        // <summary>
        // Optionally Logs exception and rethrows it
        // Use this to throw exceptions.
        // Depending on the log level it shall optionally log 
        // but shall always throw the exception
        // </summary>
        public static void WarnThrow(Exception t)
        {
            if (GetLogger().IsWarnEnabled)
            {
                Log(LogLevel.WARN, t, "");
            }
            throw t;
        }


        //// <summary>
        //// Optionally Logs exception and rethrows it
        //// </summary>
        //public static void WarnThrow(Exception t,string formatString,params object[] parameters)
        //{
        //    if(GetLogger().IsWarnEnabled)
        //    {
        //        Log(LogLevel.WARN,t,formatString,parameters);
        //    }
        //    throw t;
        //}

        // <summary>
        // Optionally Logs exception and rethrows it
        // Use this to throw exceptions.
        // Depending on the log level it shall optionally log 
        // but shall always throw the exception
        // </summary>
        public static void ErrorThrow(Exception t)
        {
            if (GetLogger().IsErrorEnabled)
            {
                Log(LogLevel.ERROR, t, "");
            }
            throw t;
        }

        //// <summary>
        //// Optionally Logs exception and rethrows it
        //// </summary>
        //public static void ErrorThrow(Exception t,string formatString,params object[] parameters)
        //{
        //    if(GetLogger().IsErrorEnabled)
        //    {
        //        Log(LogLevel.ERROR,t,formatString,parameters);
        //    }
        //    throw t;
        //}


        // <summary>
        // Optionally Logs exception and rethrows it
        // Use this to throw exceptions.
        // Depending on the log level it shall optionally log 
        // but shall always throw the exception
        // </summary>
        public static void FatalThrow(Exception t)
        {
            if (GetLogger().IsFatalEnabled)
            {
                Log(LogLevel.FATAL, t, "");
            }
            throw t;
        }

        //// <summary>
        //// Optionally Logs exception and rethrows it
        //// </summary>
        //public static void FatalThrow(Exception t,string formatString,params object[] parameters)
        //{
        //    if(GetLogger().IsFatalEnabled)
        //    {
        //        Log(LogLevel.FATAL,t,formatString,parameters);
        //    }
        //    throw t;
        //}

        // <summary>
        // Traces the method details if the IsDebugEnabled level is enabled
        // </summary>
        public static void TraceMethodEntry()
        {
            if (GetLogger().IsTraceEnabled)
            {
                Log(LogLevel.TRACE, GetTraceContextStart("Method Entry - "));
            }
        }

        // <summary>
        // Traces the method details if the IsDebugEnabled level is enabled
        // </summary>
        public static void TraceMethodExit()
        {
            if (GetLogger().IsTraceEnabled)
            {
                Log(LogLevel.TRACE, GetTraceContextStart("Method Exit - "));
            }
        }

        // <summary>
        // Traces the method details if the IsDebugEnabled level is enabled
        // </summary>
        public static void DebugMethodEntry()
        {
            if (GetLogger().IsDebugEnabled)
            {
                Log(LogLevel.DEBUG, GetTraceContextStart("Method Entry - "));
            }
        }

        // <summary>
        // Traces the method details if the IsDebugEnabled level is enabled
        // </summary>
        public static void DebugMethodExit()
        {
            if (GetLogger().IsDebugEnabled)
            {
                Log(LogLevel.DEBUG, GetTraceContextStart("Method Exit - "));
            }
        }

        // <summary>
        // Traces the method details if the IsInfoEnabled level is enabled
        // </summary>
        public static void InfoMethodEntry()
        {
            if (GetLogger().IsInfoEnabled)
            {
                Log(LogLevel.INFO, GetTraceContextStart("Method Entry - "));
            }
        }

        // <summary>
        // Traces the method details if the IsInfoEnabled level is enabled
        // </summary>
        public static void InfoMethodExit()
        {
            if (GetLogger().IsInfoEnabled)
            {
                Log(LogLevel.INFO, GetTraceContextStart("Method Exit - "));
            }
        }

        // <summary>
        // Traces the method details if the IsWarnEnabled level is enabled
        // </summary>
        public static void WarnMethodEntry()
        {
            if (GetLogger().IsWarnEnabled)
            {
                Log(LogLevel.WARN, GetTraceContextStart("Method Entry - "));
            }
        }

        // <summary>
        // Traces the method details if the IsWarnEnabled level is enabled
        // </summary>
        public static void WarnMethodExit()
        {
            if (GetLogger().IsWarnEnabled)
            {
                Log(LogLevel.WARN, GetTraceContextStart("Method Exit - "));
            }
        }

        // <summary>
        // Traces the method details if the IsErrorEnabled level is enabled
        // </summary>
        public static void ErrorMethodEntry()
        {
            if (GetLogger().IsErrorEnabled)
            {
                Log(LogLevel.ERROR, GetTraceContextStart("Method Entry - "));
            }
        }

        // <summary>
        // Traces the method details if the IsErrorEnabled level is enabled
        // </summary>
        public static void ErrorMethodExit()
        {
            if (GetLogger().IsErrorEnabled)
            {
                Log(LogLevel.ERROR, GetTraceContextStart("Method Exit - "));
            }
        }

        // <summary>
        // Traces the method details if the IsFatalEnabled level is enabled
        // </summary>
        public static void FatalMethodEntry()
        {
            if (GetLogger().IsFatalEnabled)
            {
                Log(LogLevel.FATAL, GetTraceContextStart("Method Entry - "));
            }
        }

        // <summary>
        // Traces the method details if the IsFatalEnabled level is enabled
        // </summary>
        public static void FatalMethodExit()
        {
            if (GetLogger().IsFatalEnabled)
            {
                Log(LogLevel.FATAL, GetTraceContextStart("Method Exit - "));
            }
        }

        #endregion

        # region "Private/internal constructors"
        //internal Logger(string applicationConfigurationFileName,string loggerName)
        //{
        //    log4net.Config.DOMConfigurator.Configure(new FileInfo(applicationConfigurationFileName));
        //    LoggerName = loggerName;
        //    m_Log4NetLogger = LogManager.GetLogger(LoggerName);
        //}


        //internal Logger()
        //{
        //    m_Log4NetLogger = LogManager.GetLogger(Logger.LoggerName);

        //    FormLogFileName();

        //    GetFunctionalLogger();                                         
        //}


        #endregion

        #region "private/internal helpers"


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static ILogWrapper GetLogger()
        {

            if (m_Log4NetLogger == null)
            {
                //m_Log4NetLogger = LogManager.GetLogger(Logger.LoggerName);

                m_Log4NetLogger = new StandardLogger().Logger;

                FormLogFileName();

                GetFunctionalLogger();
            }
            return m_Log4NetLogger;
        }


        /// <summary>
        /// Gets the trace context start.
        /// </summary>
        /// <returns> Gets the trace context start</returns>
        private static string GetTraceContextStart(string messageBeforeMethodName)
        {
            StackFrame traceContext = null;
            StringBuilder messageToReturn = null;
            try
            {
                traceContext = new StackFrame(2);
                messageToReturn = new StringBuilder();
                string clazzName = traceContext.GetMethod().ReflectedType.FullName;
                string methodName = traceContext.GetMethod().Name;
                messageToReturn.AppendFormat("{0} [{1}].[{2}]", messageBeforeMethodName, clazzName, methodName);
                string fileName = traceContext.GetFileName();
                int lineNumber = traceContext.GetFileLineNumber();
                if (fileName != null && fileName.Trim().Length > 0)
                {
                    fileName = Path.GetFileName(fileName);
                    messageToReturn.AppendLine();
                    messageToReturn.AppendFormat("[Source File Name {0}   Line Number {1}]", fileName, lineNumber);
                    messageToReturn.AppendLine();
                }

                //System.Reflection.ParameterInfo[] parameters = traceContext.GetMethod().GetParameters();
                //if(parameters != null)
                //{
                //    foreach(var param in parameters)
                //    {
                //        messageToReturn.AppendFormat("Type = {0} Name={1}",param.ParameterType.ToString(),param.Name);
                //    }
                //}
            }
            catch (Exception ex)
            {
                // gobble it up
                // we do not want the app to fail because of an exception in this block
                // Log it for further debugging
                Log(LogLevel.WARN, "GetTraceContextStart encountered an exception!!", ex);
                messageToReturn = new StringBuilder("GetTraceContextStart encountered an exception");
            }
            return messageToReturn.ToString();
        }

        // <summary>
        // log helper. Performs the actual logging operation
        // </summary>
        internal static void Log(LogLevel logLevel, string formatString, params object[] parameters)
        {
            int attempts = 0;
            bool isSucessful = false;

            formatString = DateTime.Now.ToString("yyyyMMdd_HHmmss") + " " + logLevel.ToString() + " " + formatString;

            while ((attempts < 2) && (!isSucessful))
            {
                try
                {

                    if (logLevel == LogLevel.TRACE && GetLogger().IsTraceEnabled)
                    {
                        // Note Log4Net does not support trace, so map trace to Debug
                        GetLogger().Trace(string.Format(formatString, parameters));
                    }
                    else if (logLevel == LogLevel.DEBUG && GetLogger().IsDebugEnabled)
                    {
                        GetLogger().Debug(string.Format(formatString, parameters));
                    }
                    else if (logLevel == LogLevel.INFO && GetLogger().IsInfoEnabled)
                    {
                        GetLogger().Info(string.Format(formatString, parameters));
                    }
                    else if (logLevel == LogLevel.WARN && GetLogger().IsWarnEnabled)
                    {
                        GetLogger().Warn(string.Format(formatString, parameters));
                    }
                    else if (logLevel == LogLevel.ERROR && GetLogger().IsErrorEnabled)
                    {
                        GetLogger().Error(string.Format(formatString, parameters));
                    }
                    else if (logLevel == LogLevel.FATAL && GetLogger().IsFatalEnabled)
                    {
                        GetLogger().Fatal(string.Format(formatString, parameters));
                    }
                }
                catch
                {
                    attempts++;
                    GetFunctionalLogger();
                }
                isSucessful = true;
            }
        }

        // <summary>
        // log helper. Performs the actual logging operation for an exception
        // </summary>
        internal static void Log(LogLevel logLevel, Exception exception, string formatString, params object[] parameters)
        {
            bool isSuccessful = false;
            int attempts = 0;

            while ((attempts < 2) && (!isSuccessful))
            {
                try
                {
                    if (logLevel == LogLevel.WARN && GetLogger().IsWarnEnabled)
                    {
                        string message = string.Format(formatString, parameters);
                        string logString = BuildLogString(message, exception);
                        GetLogger().Warn(logString);
                    }
                    else if (logLevel == LogLevel.ERROR && GetLogger().IsErrorEnabled)
                    {
                        string message = string.Format(formatString, parameters);
                        string logString = BuildLogString(message, exception);
                        GetLogger().Error(logString);
                    }
                    else if (logLevel == LogLevel.FATAL && GetLogger().IsFatalEnabled)
                    {
                        string message = string.Format(formatString, parameters);
                        string logString = BuildLogString(message, exception);
                        GetLogger().Fatal(logString);
                    }
                }
                catch
                {
                    attempts++;
                    GetFunctionalLogger();
                }
                isSuccessful = true;
            }

            // all others are ignored as you are not supposed to log with 
            // IsDebugEnabled and IsInfoEnabled levels for exceptions
        }

        // <summary>
        // log helper. Builds the exception log sting
        // </summary>
        private static string BuildLogString(string message, Exception ex)
        {
            StringBuilder strBuilder = null;

            if (message != null)
            {
                strBuilder = new StringBuilder();
                strBuilder.AppendLine();
                strBuilder.AppendLine("------------------------------------");
                strBuilder.AppendLine(string.Format("Message {0}:", message));
            }
            strBuilder.AppendLine(GetExceptionDetails(ex));
            strBuilder.AppendLine("------------------------------------");
            return strBuilder.ToString();
        }

        // <summary>
        // log helper. Builds the exception log details
        // </summary>
        public static string GetExceptionDetails(Exception ex)
        {
            StringBuilder strBuilder = null;
            if (ex != null)
            {
                strBuilder = new StringBuilder("Exception Occurred:");
                strBuilder.AppendLine();
                if (ex.Message != null)
                {
                    strBuilder.AppendLine(string.Format("Exception Message: {0}", ex.Message));
                } 
                
                if (ex.StackTrace != null)
                {
                    strBuilder.AppendLine(string.Format("Exception Stack IsTraceEnabled: {0}", ex.StackTrace));
                }

                

                if (ex.Data != null && ex.Data.Count > 0)
                {
                    strBuilder.AppendLine("Exception Data:");
                    strBuilder.AppendLine();
                    IDictionary dict = ex.Data;
                    if (dict != null && dict.Count > 0)
                    {
                        foreach (var key in dict.Keys)
                        {
                            strBuilder.AppendLine(string.Format("{0} = {1}", key, dict[key]));
                        }
                    }
                }

                if (ex.InnerException != null)
                {
                    strBuilder.AppendLine("Inner Exception:");
                    strBuilder.AppendLine(GetExceptionDetails(ex.InnerException));
                }
                return strBuilder.ToString();
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        private static void FormLogFileName()
        {
            string datetime;

            //format the datetime
            //datetime = String.Format("{0}_{1}_{2}_{3}.{4}", DateTime.Now.Date.Day, DateTime.Now.Date.Month, DateTime.Now.Date.Year, DateTime.Now.Hour, DateTime.Now.Minute);
            datetime = GetLogStartTime().ToString("yyyyMMdd_HHmmss");

            string logFileAddition = System.Environment.GetEnvironmentVariable("Logger_FileNameAddition");
            if (logFileAddition == null)
                logFileAddition = "";
            else
                logFileAddition = "_" + logFileAddition;

            //Log file name includes the application name, user name and datetime 
            m_logFileName = String.Format("{0}_{1}_{2}{3}.log", ApplicationName, UserName, datetime, logFileAddition);

        }

        internal static DateTime GetLogStartTime()
        {
            return m_logStartTime;
        }


        /// <summary>
        /// Tries to obtain a working logger by first trying to fetch a logger, that uses a config file and if not found
        /// tries to get a standard logger.
        /// </summary>
        private static void GetFunctionalLogger()
        {
            bool isSuccess;

            isSuccess = InitializeConfigFileLogger();

            if (!isSuccess)
            {
                InitializeStandardLogger();
            }
        }

        /// <summary>
        /// Searches for logger config file in the following order - 1. applicationname.username.logger.config
        /// 2. logger.config and set the member variable  m_loggerConfigFileFullName
        /// 
        /// 
        /// </summary>
        /// <param name="configFilePath"></param>
        private static void SetConfigFileFullName(string configFilePath)
        {
            string userConfigFullFullName = String.Format("{0}\\{1}.{2}.logger.config", configFilePath, ApplicationName, UserName);

            string configFileFullName = String.Format("{0}\\logger.config", configFilePath);

            if (File.Exists(userConfigFullFullName))
            {
                m_loggerConfigFileFullName = userConfigFullFullName;

            }
            else if (File.Exists(configFileFullName))
            {
                m_loggerConfigFileFullName = configFileFullName;

            }
        }

        /// <summary>
        /// Returns true if its able to initialize and test a logger from a config file
        /// else returns false. Tries to find a logger, capable of logging to a direcotory by first looping through
        /// through the possible logging directories and wi
        /// </summary>
        /// <returns></returns>
        private static bool InitializeConfigFileLogger()
        {

            foreach (string logfilePath in LoggerConfiguration.LogFilePaths)
            {
                foreach (string configFilePath in LoggerConfiguration.LoggerConfigFilePaths)
                {
                    //tries to find a config file at the config directory location and sets the 
                    // m_loggerConfigFileFullName member variable if config file founf.
                    SetConfigFileFullName(configFilePath);

                    //Configure and test the logger using the configuration file
                    if (m_loggerConfigFileFullName != string.Empty)
                    {
                        string logfile = string.Format("{0}\\{1}", logfilePath, m_logFileName);
                        if (ConfigureConfigFileLogger(logfile))
                        {
                            // logger configured sucessfully i.e. working logger.
                            Logger.Info("Changing Log file to: {0}", logfilePath);
                            m_logFilePath = logfilePath;
                            return true;
                        }
                    }
                }

            }
            return false;
        }


        /// <summary>
        /// Tests for user access rights to the log directory, Configure the logger froma config file,
        /// sets the log file name
        /// </summary>
        /// <param name="logfile"></param>
        /// <returns></returns>
        private static bool ConfigureConfigFileLogger(string logfile)
        {
            try
            {
                TestUserAccessRights(logfile);

                DOMConfigurator.Configure(new FileInfo(m_loggerConfigFileFullName));

                ChangeLogFileName(logfile);

            }
            catch
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// loops through all appenders and changes the log file for each RollingFileAppender.
        /// </summary>
        /// <param name="logfile"></param>
        private static void ChangeLogFileName(string logfile)
        {
            log4net.Repository.Hierarchy.Logger repositoryLogger = (log4net.Repository.Hierarchy.Logger)m_Log4NetLogger.Logger;

            AppenderCollection appenderCollection = repositoryLogger.Appenders;
            foreach (IAppender appender in appenderCollection)
            {
                if (appender is RollingFileAppender)
                {
                    ((RollingFileAppender)appender).File = logfile;
                    ((RollingFileAppender)appender).ActivateOptions();
                }
            }
        }


        /// <summary>
        /// Tests for user access rights to the log directory, creates a standard logger, sets the log file name
        /// </summary>
        /// <param name="logfile"></param>
        /// <returns></returns>
        private static bool ConfigureStandardLogger(string logfile)
        {
            try
            {
                TestUserAccessRights(logfile);
                Logger.Info("Changing Log file to: {0}", logfile);
                
                m_Log4NetLogger = new StandardLogger(logfile, Logger.LoggerName).Logger;

                //the log file name will be changed to the following.
                //applicatoname_username_DD_MM_YYYY_HH_MM.log
                ChangeLogFileName(logfile);
                m_logFilePath = logfile;
            }
            catch
            {
                return false;
            }

            return true;

        }

        /// <summary>
        /// Responsible for returning a standard logger in working condition.
        /// </summary>
        private static void InitializeStandardLogger()
        {
            //Loop through all logfile paths, test for the sucessfull working of the loger
            //by calling configurestandardLogger().
            foreach (string logfilePath in LoggerConfiguration.LogFilePaths)
            {
                string logfile = string.Format("{0}\\{1}", logfilePath, m_logFileName);

                if (ConfigureStandardLogger(logfile))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// This method tests for user rights for creating files in the logs directory by creating and deleting
        /// a dummy file.
        /// </summary>
        /// <param name="logFile"></param>
        internal static void TestUserAccessRights(String logFile)
        {
            StreamWriter sw = null;
            try
            {
                String dummyFile = logFile + ".test";
                sw = new StreamWriter(dummyFile);
               
                sw.Write("test");
                sw.Flush();
                sw.Close();
                sw = null;

                File.Delete(dummyFile);
            }
            catch (Exception e1)
            {
                int lastSlashPos = logFile.LastIndexOf("\\");
                if (lastSlashPos <= 0)
                    throw e1;

                string dir = logFile.Substring(0, lastSlashPos);

                try
                {
                    Directory.CreateDirectory(dir);
                    String dummyFile = logFile + ".test";
                    sw = new StreamWriter(dummyFile);
                    sw.Write("test");
                    sw.Flush();
                    sw.Close();
                    sw = null;

                    File.Delete(dummyFile);

                }
                catch (Exception e2)
                {
                    throw e2;
                }

            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        #endregion
    }
}