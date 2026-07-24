using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections;
using Utilities.Logging;


namespace Utilities.Configuration
{
    /// <summary>
    /// This calls accepts key values for configuration items and returns a string.
    /// </summary>
    public static class ABSConfig
    {
        #region constants
        internal const string ProdConfigDBConnectionString = "Database=BetFair;Server=HANSOLO\\SQLSERVEREXPRESS;Integrated Security=SSPI"; //hardcoded connection string to the configuration database
        private const string AppConfigDBKey = "ConfigDBConnectionString";
        #endregion

        #region private members
        private static string m_applicationName = String.Empty;
        private static string m_userName = String.Empty;
        private static Dictionary<string, ConfigItem> _configDataStore;
        private static string m_configDBConnectionString = ProdConfigDBConnectionString;
        #endregion

        #region static constructor

        /// <summary>
        /// Static Constructor that builds the config data store by looking up the db or looking uo the appsettings section.
        /// </summary>
        static ABSConfig()
        {

            //gets key value pairs from app.config 
            BuildDataStoreFromAppConfig();

            //gets key value pairs from the Config database
            BuildDataStoreFromDB();
        }

        #endregion

        #region internal Properties
        internal static string ConfigDBConnectionString
        {
            get
            {
                string ConfigDbKey = ConfigurationManager.AppSettings[AppConfigDBKey];
                if (ConfigDbKey != null)
                {
                    m_configDBConnectionString = (ConfigurationManager.ConnectionStrings[ConfigDbKey] != null) ?
                                                 ConfigurationManager.ConnectionStrings[ConfigDbKey].ConnectionString :
                                                 m_configDBConnectionString;
                    Logger.Trace("Getting the initial connection string from app.config using {ConnectionString} variable '" + 
                                 ConfigurationManager.AppSettings[AppConfigDBKey] + 
                                 "' (pointed to by '" + AppConfigDBKey + "')");
                }

                string environmentConnectionString = System.Environment.GetEnvironmentVariable(AppConfigDBKey);
                if (!string.IsNullOrEmpty(environmentConnectionString))
                {
                    m_configDBConnectionString = environmentConnectionString;
                    Logger.Info("Getting the initial connection string from environment variable '" + AppConfigDBKey + "'");
                }
                else
                {
                    Logger.Trace("Environment variable '" + AppConfigDBKey + "' is not set so using '" + m_configDBConnectionString + "' as the initial connection string.");
                }

                return m_configDBConnectionString;
            }
        }

        #endregion

        #region private properties
        /// <summary>
        /// Internal dictionary that stores keys and values.
        /// </summary>
        static private Dictionary<string, ConfigItem> ConfigDataStore
        {
            get
            {
                if (_configDataStore == null)
                {
                    _configDataStore = new Dictionary<string, ConfigItem>();

                }
                return _configDataStore;
            }
        }
        /// <summary>
        /// Application Name
        /// </summary>
        static private string ApplicationName
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
        /// User Name
        /// </summary>
        static private string UserName
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



        #region public methods

        /// <summary>
        /// Client will pass in the key and get a string in return. Incase string is not found Empty string is returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static public string GetValue(string key)
        {
            if (ConfigDataStore.ContainsKey(key))
            {
                if (Logger.IsTraceEnabled())
                    Logger.Trace("Config value: key = '{0}', value = '{1}', type = '{2}'", key, ConfigDataStore[key].Value, ConfigDataStore[key].ConfigItemType);

                return ConfigDataStore[key].Value;
            }
            return null;
        }

        /// <summary>
        /// Provides all the configuration information for an application in the following form
        /// Key=<key>;Value=<value>;Source=<source>||Key=<key>;Value=<value>;Source=<source>|| and so on..

        /// </summary>
        /// <returns></returns>
        public static string GetConfigItemSources()
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, ConfigItem>.Enumerator enumerator = ConfigDataStore.GetEnumerator();

            while (enumerator.MoveNext())
            {
                sb.Append(String.Format("Key={0};Value={1};Source={2}||", enumerator.Current.Key, enumerator.Current.Value.Value, enumerator.Current.Value.ConfigItemType.ToString()));
            }
            return sb.ToString();
        }
        #endregion


        #region private methods

        /// <summary>
        /// Looks up the app.config's appsettings section to get the keys. These key- value pairs have thier type as
        /// ConfigItemType.FileAppConfig
        /// </summary>
        static private void BuildDataStoreFromAppConfig()
        {
            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;

            string[] keys = appSettings.AllKeys;

            // Loop to get key/value pairs.
            for (int i = 0; i < appSettings.Count; i++)
            {
                ConfigItem configItemType = new ConfigItem(keys[i], appSettings[i], ConfigItemType.FileAppConfig);

                ///if a data is already present in the datastore , override it
                if (ConfigDataStore.ContainsKey(configItemType.Key))
                {
                    ConfigDataStore.Remove(configItemType.Key);

                }
                ConfigDataStore.Add(configItemType.Key, configItemType);

            }
        }

        /// <summary>
        /// Goes to the ABSSystemConfig database  to collect 
        /// </summary>
        static private void BuildDataStoreFromDB()
        {
            List<ConfigItem> configItems = ConfigDBHelper.GetConfigInformation(ApplicationName, UserName);

            foreach (ConfigItem configItem in configItems)
            {
                //Based on the ConfigItemType, a ConfigItemType of a higher priority can overide a key present
                //in the datastore of a lower priotiy
                if ((ConfigDataStore.ContainsKey(configItem.Key)) && ((ConfigDataStore[configItem.Key].ConfigItemType <= configItem.ConfigItemType)))
                {
                    ConfigDataStore[configItem.Key] = configItem;
                }
                else if (!ConfigDataStore.ContainsKey(configItem.Key))
                {
                    ConfigDataStore.Add(configItem.Key, configItem);
                }
            }
        }



        #endregion
    }
}
