using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using log4net.Layout;
using Utilities.Configuration;

namespace Utilities.Logging
{
    class StandardLogger
    {
        #region constants
        const string FILEAPPENDER_NAME = "FileAppender";
        const string CONSOLEAPPENDER_NAME = "ConsoleAppender";
        #endregion  

        #region private members
        private ILogWrapper m_logger;
        private log4net.Repository.Hierarchy.Logger m_repositoryLogger2 = null;

        private string m_fileName;        
        IAppender m_consoleAppender;
        Dictionary<string, IAppender> m_appendersList;

        #endregion

        #region Constructor
        //Default level set to WARN - Currently has one appender - Rolling FileAppender
        public StandardLogger()
        {
            string ApplicationName = Utilities.Helpers.CommonHelper.ApplicationName;
            string UserName = System.Environment.UserName;
            string datetime = Utilities.Logging.Logger.GetLogStartTime().ToString("yyyyMMdd_HHmmss");

            string logFileAddition = System.Environment.GetEnvironmentVariable("Logger_FileNameAddition");
            if (logFileAddition == null)
                logFileAddition = "";
            else
                logFileAddition = "_" +logFileAddition ;

            string logFileName = String.Format("{0}_{1}_{2}{3}_startup.log", ApplicationName, UserName, datetime,logFileAddition);

            string[] dirs = {"c:\\logs\\startup",
                             "."};

            string fileName = "";
            foreach (string dir in dirs)
            {
                string testFileName = dir + "\\" + logFileName;
                bool writePermission = true;
                try
                {
                    Utilities.Logging.Logger.TestUserAccessRights(testFileName);
                }
                catch (Exception)
                {
                    writePermission = false;
                }
                if (writePermission)
                {
                    fileName = testFileName;
                    break;
                }
            }
            // If fileName is still "", oh well!

            m_logger = new Log4NetWrappper("startupLogger");
            
            
            m_fileName = fileName;

            // Set the debug level
            // Default is 'INFO'
            LogLevel logLevel = LogLevel.INFO;
           
            // Check if LogLevel is set in app.config, if so use that.
            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            string[] keys = appSettings.AllKeys;                     

            for (int i = 0; i < appSettings.Count; i++)
            {
                if (keys[i] == "LogLevel")
                {
                    logLevel = LoggerHelper.MapLogLevel(appSettings[i]);
                    break;
                }
            }

            // Check the environment to see if 'LogLevel' is set, if so use that.
            string environmentLogLevel = System.Environment.GetEnvironmentVariable("LogLevel");
            if(!string.IsNullOrEmpty(environmentLogLevel))
            {
                logLevel = LoggerHelper.MapLogLevel(environmentLogLevel);
            }

            SetLevel(logLevel);

            AddFileAppender();

        }



        public StandardLogger(string fileName, string loggerName)
        {
            if (fileName == String.Empty || fileName == null)
            {
                Utilities.Logging.Logger.ErrorThrow(new ArgumentException("Standard Logger needs a valid file name for logging"));
            }
            m_logger = new Log4NetWrappper(loggerName);

            m_fileName = fileName;

            // Set the debug level
            // Default is 'INFO'
            LogLevel logLevel = LogLevel.INFO;

            // Check the DB config, if that is set, use that
            if (ABSConfig.GetValue("LogLevel") != null)
                logLevel = LoggerHelper.MapLogLevel(ABSConfig.GetValue("LogLevel"));

            // Check the environment to see if 'LogLevel' is set, if so use that.
            string environmentLogLevel = System.Environment.GetEnvironmentVariable("LogLevel");
            if (!string.IsNullOrEmpty(environmentLogLevel))
            {
                logLevel = LoggerHelper.MapLogLevel(environmentLogLevel);
            }


            SetLevel(logLevel);

            AddFileAppender();

        }
        #endregion

        #region internal/private properties
        internal ILogWrapper Logger
        {
            get
            {
                return m_logger;
            }
        }

        private Dictionary<string, IAppender> AppendersList
        {
            get
            {
                if (m_appendersList == null)
                {
                    m_appendersList = new Dictionary<string, IAppender>();

                }
                return m_appendersList;
            }
        }
        private log4net.Repository.Hierarchy.Logger RepositoryLogger
        {
            get
            {
                if (m_repositoryLogger2 == null)
                {
                    m_repositoryLogger2 = (log4net.Repository.Hierarchy.Logger)m_logger.Logger;
                }
                return m_repositoryLogger2;
            }
        }


        #endregion

        #region private/internal methods
        internal void SetLevel(LogLevel level)
        {
            RepositoryLogger.Level = RepositoryLogger.Hierarchy.LevelMap[level.ToString()];
            m_logger.SetLevel(level);
        }

        private void AddFileAppender()
        {
        
            RollingFileAppender rollingAppender;
                     
            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%m %n";
            layout.ActivateOptions();
            rollingAppender = new RollingFileAppender();
            rollingAppender.File = m_fileName;
            rollingAppender.Layout = layout;
            rollingAppender.MaximumFileSize = "4MB";
            rollingAppender.MaxSizeRollBackups = -1; //infinite rolling , no deletions

            rollingAppender.ActivateOptions();
                        
            RepositoryLogger.AddAppender(rollingAppender);            
            
           
        }

        internal void AddConsoleAppender()
        {
            ConsoleAppender consoleAppender = new ConsoleAppender();

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%m %n";
         //   layout.ActivateOptions();

            consoleAppender.Layout = layout;
        //  consoleAppender.ActivateOptions();
            m_consoleAppender = consoleAppender;
            

            RepositoryLogger.AddAppender(m_consoleAppender);

        }

       
        #endregion
      

    }
    
}
