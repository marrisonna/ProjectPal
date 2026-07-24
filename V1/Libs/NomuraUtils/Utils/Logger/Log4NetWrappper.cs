using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.spi;
using log4net.Repository;

namespace Utilities.Logging
{
    public class Log4NetWrappper : ILogWrapper, ILogger
    {
        public Log4NetWrappper(string name)
        {
            m_logger = LogManager.GetLogger(name);

        }

        public void SetLevel(LogLevel level)
        {
            m_traceEnabled = (level == LogLevel.TRACE);
        }


        public bool IsTraceEnabled { get { return m_traceEnabled; } }

        bool m_traceEnabled = true;



        public bool IsDebugEnabled { get { return m_logger.IsDebugEnabled; } }
        public bool IsErrorEnabled { get { return m_logger.IsErrorEnabled; } }
        public bool IsFatalEnabled { get { return m_logger.IsFatalEnabled; } }
        public bool IsInfoEnabled { get { return m_logger.IsInfoEnabled; } }
        public bool IsWarnEnabled { get { return m_logger.IsWarnEnabled; } }

        public void Trace(object message)
        {
            m_logger.Debug(message);
        }
        public void Trace(object message, Exception t)
        {
            m_logger.Debug(message, t);
        }

        public void Debug(object message)
        {
            m_logger.Debug(message);   
        }
        public void Debug(object message, Exception t)
        {
            m_logger.Debug(message,t);
        }
        public void Error(object message)
        {
            m_logger.Error(message);
        }
        public void Error(object message, Exception t)
        {
            m_logger.Error(message, t);
        }
        public void Fatal(object message)
        {
            m_logger.Fatal(message);
        }
        public void Fatal(object message, Exception t)
        {
            m_logger.Fatal(message, t);
        }
        public void Info(object message)
        {
            m_logger.Info(message);
        }
        public void Info(object message, Exception t)
        {
            m_logger.Info(message, t);
        }
        public void Warn(object message)
        {
            m_logger.Warn(message);
        }
        public void Warn(object message, Exception t)
        {
            m_logger.Warn(message, t);
        }

        public ILogger Logger { get { return m_logger.Logger; } }

        private ILog m_logger;


        /// ILogger members

        public string Name { get { return m_logger.Logger.Name; } }
        public ILoggerRepository Repository { get { return m_logger.Logger.Repository; } }

        public bool IsEnabledFor(Level level)
        {
            if (level == Level.TRACE)
                return m_traceEnabled;
            return m_logger.Logger.IsEnabledFor(level);
        }

        public void Log(LoggingEvent logEvent)
        {
            m_logger.Logger.Log(logEvent);
        }
        public void Log(string callerFqcn, Level level, object message, Exception t)
        {
            m_logger.Logger.Log(callerFqcn, level, message, t);
        }
    }
}
