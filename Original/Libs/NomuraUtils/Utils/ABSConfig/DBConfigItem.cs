using System;
using System.Collections.Generic;
using System.Text;
using Utilities.Logging;

namespace Utilities.Configuration
{
    /// <summary>
    /// The type or the source of the config item. The order in which they are defined is CRITICAL as the lower types can
    /// be overrided by the higher ones. e.g akey of type DBCommon can be overriden by a key of type DBAppOveride
    /// </summary>
    internal enum ConfigItemType 
    {
        //In order of priority
        FileAppConfig, // Key defined in app.config
        DBCommon, //key defined in the CommonConfig table of the ABSSystemConfig database
        DBAppOverride //key defined in the AppCommonConfig table of the ABSSystemConfig database
    }
    
    internal class ConfigItem
    {
        // TODO Make member variables start with 'm_'
        #region private members
        private string key;
        private string value;
        private ConfigItemType configItemType;     
        #endregion

        #region Constructor
        internal ConfigItem(string key, string value, ConfigItemType itemType)
        {
            Logger.Debug("Adding to Config '{0}' = '{1}' from '{2}'", key, value, itemType.ToString()); 
            this.key = key;
            this.value = value;
            this.configItemType = itemType;
        }

        /// <summary>
        /// private constructor
        /// </summary>
        private ConfigItem() 
        {
        }
        #endregion

        #region internal properties
        internal string Key
        {
            get { return key; }
        }

        internal string Value
        {
            get { return this.value; }          
        }

        internal ConfigItemType ConfigItemType
        {
            get { return configItemType; }
            set { configItemType = value; }
        }

        #endregion

     }
}

