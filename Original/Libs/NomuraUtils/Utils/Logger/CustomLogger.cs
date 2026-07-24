using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Diagnostics;
using log4net;
using log4net.Config;
using log4net.Layout;
using log4net.Appender;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace Utilities.Logging
{
    //<summary>
    // Allows you to configure a logger with or without a custom config file
    // usage:
    //  See unit test cases
    // </summary>
    public class CustomLogger
    {
        private ILogWrapper m_Log4NetLogger = null;
        private string AppName { get; set; }
        private string LoggerName { get; set; }

        #region "Constructors"

        //<summary>
        // Initialize a logger using a config file
        //</summary>
        public CustomLogger(string applicationConfigurationFileName,string loggerName)
        {
            LoggerName = loggerName;
            log4net.Config.DOMConfigurator.Configure(new FileInfo(applicationConfigurationFileName));
            m_Log4NetLogger = new Log4NetWrappper(loggerName);
        }

        //<summary>
        // Initialize a logger programmatically.
        // when you dont want to use a config file.
        //</summary>
        public CustomLogger(string name,LogLevel logLevel, string logFilePath)
        {
            LoggerName = name;
            IAppender fileAppenderTrace = CreateFileAppender(name, logFilePath );
            AddAppender(name,fileAppenderTrace);
            SetLevel(name,logLevel);
            m_Log4NetLogger = new Log4NetWrappper(name);
        }

        //<summary>
        // Initialize a logger without using a config file
        // this configures a UDP appender instead of a file appender as the default appender
        // where we many not have direct access to the log directories.
        // This may even be used to configure logging on a central server
        // that hosts a central udp based logging service(s).
        // you can consider configuring an additional appender (say file)
        // by using the CreateAppender and AddAppender methods
        //</summary>
        public CustomLogger(string name,LogLevel logLevel,string remoteAddress, int remotePort)
        {
            IAppender udpAppender = CreateUDPAppender(name,remoteAddress,remotePort);
            AddAppender(name,udpAppender);
            SetLevel(name,logLevel);
            m_Log4NetLogger = new Log4NetWrappper(name);
        }

        #endregion 

        #region Public Methods

        // <summary>
        // Returns true if log level is IsDebugEnabled 
        // </summary>
        public bool IsTraceEnabled()
        {
            return m_Log4NetLogger.IsTraceEnabled;
        }

        // <summary>
        // Returns true if log level is IsDebugEnabled 
        // </summary>
        public bool IsDebugEnabled()
        {
            return m_Log4NetLogger.IsDebugEnabled;
        }

        // <summary>
        // Returns true if log level is INFO or lower
        // </summary>
        public bool IsInfoEnabled()
        {
            return m_Log4NetLogger.IsInfoEnabled;
        }

        // <summary>
        // Returns true if log level is warn or lower
        // </summary>
        public bool IsWarnEnabled()
        {
            return m_Log4NetLogger.IsWarnEnabled;
        }

        // <summary>
        // Returns true if log level is ERROR or lower
        // </summary>
        public bool IsErrorEnabled()
        {
            return m_Log4NetLogger.IsErrorEnabled;
        }

        // <summary>
        // Returns true if log level is IsFatalEnabled or lower
        // </summary>
        public bool IsFatalEnabled()
        {
            return m_Log4NetLogger.IsFatalEnabled;
        }

        public void DebugMethodEntry()
        {
            if(m_Log4NetLogger.IsDebugEnabled)
            {
                Log(LogLevel.DEBUG,GetTraceContextStart("Method Entry - "));
            }
        }

        public void DebugMethodExit()
        {
            if(m_Log4NetLogger.IsDebugEnabled)
            {
                Log(LogLevel.DEBUG,GetTraceContextStart("Method Exit - "));
            }
        }

        public void InfoMethodEntry()
        {
            if(m_Log4NetLogger.IsInfoEnabled)
            {
                Log(LogLevel.INFO,GetTraceContextStart("Method Entry - "));
            }
        }

        public void InfoMethodExit()
        {
            if(m_Log4NetLogger.IsInfoEnabled)
            {
                Log(LogLevel.INFO,GetTraceContextStart("Method Exit - "));
            }
        }

        public void ErrorMethodEntry()
        {
            if(m_Log4NetLogger.IsErrorEnabled)
            {
                Log(LogLevel.ERROR,GetTraceContextStart("Method Entry - "));
            }
        }

        public void ErrorMethodExit()
        {
            if(m_Log4NetLogger.IsErrorEnabled)
            {
                Log(LogLevel.ERROR,GetTraceContextStart("Method Exit - "));
            }
        }

        public void WarnMethodEntry()
        {
            if(m_Log4NetLogger.IsWarnEnabled)
            {
                Log(LogLevel.WARN,GetTraceContextStart("Method Entry - "));
            }
        }

        public void WarnMethodExit()
        {
            if(m_Log4NetLogger.IsWarnEnabled)
            {
                Log(LogLevel.WARN,GetTraceContextStart("Method Exit - "));
            }
        }

        public void FatalMethodEntry()
        {
            if(m_Log4NetLogger.IsFatalEnabled)
            {
                Log(LogLevel.FATAL,GetTraceContextStart("Method Entry - "));
            }
        }

        public void FatalMethodExit()
        {
            if(m_Log4NetLogger.IsFatalEnabled)
            {
                Log(LogLevel.FATAL,GetTraceContextStart("Method Exit - "));
            }
        }

        public void AddAppender(IAppender appender)
        {
            ILogWrapper log = new Log4NetWrappper (LoggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;
            l.AddAppender(appender);
        }


        // <summary>
        // Dumps IsDebugEnabled message
        // </summary>
        public void Trace(string formatString,params object[] parameters)
        {
            Log(LogLevel.TRACE,formatString,parameters);
        }

        // <summary>
        // Dumps IsDebugEnabled message
        // </summary>
        public void Debug(string formatString,params object[] parameters)
        {
            Log(LogLevel.DEBUG,formatString,parameters);
        }

        // <summary>
        // Dumps IsInfoEnabled/IsTraceEnabled message
        // </summary>
        public void Info(string formatString,params object[] parameters)
        {
            Log(LogLevel.INFO,formatString,parameters);
        }

        // <summary>
        // Dumps message with IsWarnEnabled log level
        // </summary>
        public void Warn(string formatString,params object[] parameters)
        {
            Log(LogLevel.WARN,formatString,parameters);
        }

        // <summary>
        // Dumps Exception message with IsWarnEnabled log level
        // </summary>
        public void WarnException(Exception t)
        {
            if(m_Log4NetLogger.IsWarnEnabled)
            {
                Log(LogLevel.WARN,t, "");
            }
        }

        // <summary>
        // Dumps message with IsErrorEnabled log level
        // </summary>
        public void Error(string formatString,params object[] parameters)
        {
            Log(LogLevel.ERROR,formatString,parameters);
        }

        // <summary>
        // Dumps Exception message with IsErrorEnabled log level
        // </summary>
        public void ErrorException(Exception t)
        {
            if(m_Log4NetLogger.IsErrorEnabled)
            {
                Log(LogLevel.ERROR,t, "");
            }
        }

        // <summary>
        // Dumps message with IsWarnEnabled log level
        // </summary>
        public void Fatal(string formatString,params object[] parameters)
        {
            Log(LogLevel.FATAL,formatString,parameters);
        }

        // <summary>
        // Dumps Exception message with IsFatalEnabled log level
        // </summary>
        public void FatalException(Exception t)
        {
            if(m_Log4NetLogger.IsFatalEnabled)
            {
                Log(LogLevel.FATAL,t, "");
            }
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // </summary>
        public void TraceThrow(Exception t)
        {
            if(m_Log4NetLogger.IsTraceEnabled)
            {
                Log(LogLevel.TRACE,t,"");
            }
            throw t;
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // </summary>
        public void DebugThrow(Exception t)
        {
            if(m_Log4NetLogger.IsDebugEnabled)
            {
                Log(LogLevel.DEBUG,t, "");
            }
            throw t;
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // </summary>
        public void InfoThrow(Exception t)
        {
            if(m_Log4NetLogger.IsInfoEnabled)
            {
                Log(LogLevel.INFO,t, "");
            }
            throw t;
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // </summary>
        public void WarnThrow(Exception t)
        {
            if(m_Log4NetLogger.IsWarnEnabled)
            {
                Log(LogLevel.WARN,t, "");
            }
            throw t;
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // </summary>
        public void ErrorThrow(Exception t)
        {
            if(m_Log4NetLogger.IsErrorEnabled)
            {
                Log(LogLevel.ERROR,t, "");
            }
            throw t;
        }

        // <summary>
        // Optionally Logs exception and rethrows it
        // </summary>
        public void FatalThrow(Exception t)
        {
            if(m_Log4NetLogger.IsFatalEnabled)
            {
                Log(LogLevel.FATAL,t, "");
            }
            throw t;
        }

        #endregion


        #region "Public static methods - helpers"
        //<summary>
        // Create a new file appender programmatically
        // usage:
        //  See unit test cases
        // </summary>
        public static IAppender CreateFileAppender(string name, string fileName)
        {
            FileAppender appender = new FileAppender();
            appender.Name = name;
            appender.File = fileName;
            appender.AppendToFile = true;

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%d [%t] %-5p %c [%x] - %m%n";
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        //<summary>
        // Create a new UDP appender programmatically
        // usage:
        //  See unit test cases
        // </summary>
        public static IAppender CreateUDPAppender(string name, string remoteAddress, int remotePort)
        {
            UdpAppender appender = new UdpAppender();
            appender.Name = name;
            appender.RemoteAddress = System.Net.IPAddress.Parse(remoteAddress);
            appender.RemotePort = remotePort;

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%d [%t] %-5p %c [%x] - %m%n";
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        #endregion


        #region "private\internal helpers"

        /// <summary>
        /// Gets the trace context start.
        /// </summary>
        /// <returns> Gets the trace context start</returns>
        private string GetTraceContextStart(string messageBeforeMethodName)
        {
            StackFrame traceContext = null;
            StringBuilder messageToReturn = null;
            try
            {
                traceContext = new StackFrame(2);
                messageToReturn = new StringBuilder();
                string clazzName = traceContext.GetMethod().ReflectedType.FullName;
                string methodName = traceContext.GetMethod().Name;
                messageToReturn.AppendFormat("{0} [{1}].[{2}]",messageBeforeMethodName,clazzName,methodName);
                string fileName = traceContext.GetFileName();
                int lineNumber = traceContext.GetFileLineNumber();
                if(fileName!=null && fileName.Trim().Length > 0)
                {
                    fileName = Path.GetFileName(fileName);
                    messageToReturn.AppendLine();
                    messageToReturn.AppendFormat("[Source File Name {0}   Line Number {1}]",fileName,lineNumber);
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
            catch(Exception ex)
            {
                // gobble it up
                // we do not want the app to fail because of an exception in this block
                // Log it for further debugging
                Log(LogLevel.WARN,"GetTraceContextStart encountered an exception!!",ex);
                messageToReturn = new StringBuilder("GetTraceContextStart encountered an exception");
            }
            return messageToReturn.ToString();
        }

        // <summary>
        // log helper. Performs the actual logging operation
        // </summary>
        internal void Log(LogLevel logLevel,string formatString,params object[] parameters)
        {
            if(logLevel == LogLevel.DEBUG && m_Log4NetLogger.IsDebugEnabled)
            {
                m_Log4NetLogger.Debug(string.Format(formatString,parameters));
            }
            else if(logLevel == LogLevel.INFO && m_Log4NetLogger.IsInfoEnabled)
            {
                m_Log4NetLogger.Info(string.Format(formatString,parameters));
            }
            else if(logLevel == LogLevel.WARN && m_Log4NetLogger.IsWarnEnabled)
            {
                m_Log4NetLogger.Warn(string.Format(formatString,parameters));
            }
            else if(logLevel == LogLevel.ERROR && m_Log4NetLogger.IsErrorEnabled)
            {
                m_Log4NetLogger.Error(string.Format(formatString,parameters));
            }
            else if(logLevel == LogLevel.FATAL && m_Log4NetLogger.IsFatalEnabled)
            {
                m_Log4NetLogger.Fatal(string.Format(formatString,parameters));
            }
        }

        // <summary>
        // log helper. Performs the actual logging operation for an exception
        // </summary>
        internal void Log(LogLevel logLevel,string message,Exception exception)
        {
            if(logLevel == LogLevel.WARN && m_Log4NetLogger.IsWarnEnabled)
            {
                string logString = BuildLogString(message,exception);
                m_Log4NetLogger.Warn(logString);
            }
            else if(logLevel == LogLevel.ERROR && m_Log4NetLogger.IsErrorEnabled)
            {
                string logString = BuildLogString(message,exception);
                m_Log4NetLogger.Error(logString);
            }
            else if(logLevel == LogLevel.FATAL && m_Log4NetLogger.IsFatalEnabled)
            {
                string logString = BuildLogString(message,exception);
                m_Log4NetLogger.Fatal(logString);
            }
            // all others are ignored as you are not supposed to log with 
            // IsDebugEnabled and IsInfoEnabled levels for exceptions
        }

        // <summary>
        // log helper. Performs the actual logging operation for an exception
        // </summary>
        internal void Log(LogLevel logLevel,Exception exception,string formatString,params object[] parameters)
        {
            if(logLevel == LogLevel.WARN && m_Log4NetLogger.IsWarnEnabled)
            {
                string message = string.Format(formatString,parameters);
                string logString = BuildLogString(message,exception);
                m_Log4NetLogger.Warn(logString);
            }
            else if(logLevel == LogLevel.ERROR && m_Log4NetLogger.IsErrorEnabled)
            {
                string message = string.Format(formatString,parameters);
                string logString = BuildLogString(message,exception);
                m_Log4NetLogger.Error(logString);
            }
            else if(logLevel == LogLevel.FATAL && m_Log4NetLogger.IsFatalEnabled)
            {
                string message = string.Format(formatString,parameters);
                string logString = BuildLogString(message,exception);
                m_Log4NetLogger.Fatal(logString);
            }
            // all others are ignored as you are not supposed to log with 
            // IsDebugEnabled and IsInfoEnabled levels for exceptions
        }

        // <summary>
        // log helper. Builds the exception log sting
        // </summary>
        private static string BuildLogString(string message,Exception ex)
        {
            StringBuilder strBuilder = null;

            if(message != null)
            {
                strBuilder = new StringBuilder();
                strBuilder.AppendLine();
                strBuilder.AppendLine("------------------------------------");
                strBuilder.AppendLine(string.Format("Message {0}:",message));
            }
            strBuilder.AppendLine(GetExceptionDetails(ex));
            strBuilder.AppendLine("------------------------------------");
            return strBuilder.ToString();
        }

        // <summary>
        // log helper. Builds the exception log details
        // </summary>
        private static string GetExceptionDetails(Exception ex)
        {
            StringBuilder strBuilder = null;
            if(ex != null)
            {
                strBuilder = new StringBuilder("Exception Occurred:");
                strBuilder.AppendLine();
                if(ex.StackTrace != null)
                {
                    strBuilder.AppendLine(string.Format("Exception Stack IsTraceEnabled: {0}",ex.StackTrace));
                }

                if(ex.Message != null)
                {
                    strBuilder.AppendLine(string.Format("Exception Message: {0}",ex.Message));
                }

                if(ex.Data != null && ex.Data.Count > 0)
                {
                    strBuilder.AppendLine("Exception Data:");
                    strBuilder.AppendLine();
                    IDictionary dict = ex.Data;
                    if(dict != null && dict.Count > 0)
                    {
                        foreach(var key in dict.Keys)
                        {
                            strBuilder.AppendLine(string.Format("{0} = {1}",key,dict[key]));
                        }
                    }
                }

                if(ex.InnerException != null)
                {
                    strBuilder.AppendLine("Inner Exception:");
                    strBuilder.AppendLine(GetExceptionDetails(ex.InnerException));
                }
                return strBuilder.ToString();
            }
            return "";
        }

        // Set the level for a named logger
        private static void SetLevel(string loggerName,LogLevel logLevel)
        {
            ILogWrapper log = new Log4NetWrappper(loggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;
            l.Level = l.Hierarchy.LevelMap[logLevel.ToString()];
        }

        private static void AddAppender(string loggerName,IAppender appender)
        {
            ILog log = log4net.LogManager.GetLogger(loggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;
            l.AddAppender(appender);
        }

        #endregion
    }
}
