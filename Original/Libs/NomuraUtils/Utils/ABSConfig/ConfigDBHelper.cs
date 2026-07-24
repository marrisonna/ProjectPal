using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using Utilities.Database;

namespace Utilities.Configuration
{
    internal static class ConfigDBHelper
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static  ConfigItemType ConvertConfigItemType(string type)
        {
            if (type == "DBCommon")
            {
                return ConfigItemType.DBCommon;
            }
            else if (type == "DBAppOverride")
            {
                return ConfigItemType.DBAppOverride;
            }
            return ConfigItemType.DBAppOverride;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public static List<ConfigItem> GetConfigInformation(string applicationName, string username)
        {
            Database.Database db = new Utilities.Database.Database();

            List<ConfigItem> configItemsList = new List<ConfigItem>();
            IDbCommand dbCommand = db.GetStoredProcCommand("GetConfigInfo");

            // TODO 
            // db.AddInParameter<string>(dbCommand, "ApplicationName", applicationName);
            db.AddInParameter(dbCommand, "ApplicationName", DbType.String, applicationName);
            db.AddInParameter(dbCommand, "UserName", DbType.String, username);

            try
            {
                IDataReader reader = db.ExecuteReader(dbCommand);

                //Populate the list..
                while (reader.Read())
                {
                    ConfigItemType configItemType = ConvertConfigItemType(reader[2].ToString());

                    ConfigItem item = new ConfigItem(reader["ConfigKey"].ToString(), reader["ConfigValue"].ToString(), configItemType);
                    configItemsList.Add(item);

                }
            }
            catch (Exception ex)
            {
                //we log the exception and eat it...reason being, we donot want Config class
                // to fail for failure of one of the config sources. the app.config could have
                // the default config info which the application could use.
                // we are consuming all exceptions thrown in the above try block
                Utilities.Logging.Logger.ErrorException(ex, "Exception while populating Config from database.");               
            }
            return configItemsList;
        }
    }
}
