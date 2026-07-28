using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace Utils
{
    public class DatabaseBase
    {
     

        public enum DBTypeValues { SQLServer, FileSystem }

        private static DBTypeValues m_dbType = DBTypeValues.SQLServer;

        public static DBTypeValues DBType
        {
            get
            {
                return m_dbType;
            }
            set
            {
                m_dbType = value;
            }
        }

        private string m_sqlServerDBName = "ProjectPalDB_1";
        static public string Schema
        {
            get
            {
                if (m_dbType == DBTypeValues.SQLServer)
                    return "TaskMan.";
                return "";
            }
        }


        static string s_currentUser = null;
        static public string CurrentUser
        {
            get
            {
                if (s_currentUser == null)
                {
                    //if (DBType == DBTypeValues.SQLServer)
                    //{
                    //    DatabaseBase theDatabase = new DatabaseBase(null);
                    //    using (DbDataReader queryResult = theDatabase.ExecuteReader("select User"))
                    //    {
                    //        queryResult.Read();
                    //        s_currentUser = DatabaseBase.GetColumnValueAsString(queryResult, 0);
                    //    }
                    //}
                    //else
                    //{
                    s_currentUser = Environment.UserDomainName + @"\" + Environment.UserName;
                    //}
                    if (Environment.UserName == "Neil" || s_currentUser == "dbo")
                        s_currentUser = @"EUROPE\marrison";

                    s_currentUser = s_currentUser == null ? null : s_currentUser.ToLower();
                }
                return s_currentUser;
            }
        }


        static Dictionary<object, DatabaseBase> s_databaseInstances = new Dictionary<object, DatabaseBase>();
        static public DatabaseBase NamedInstance(object name)
        {
            if (name == null)
                return new DatabaseBase(name);

            DatabaseBase requiredInstance;
            if (!s_databaseInstances.TryGetValue(name, out requiredInstance))
            {
                requiredInstance = new DatabaseBase(name);
                s_databaseInstances.Add(name, requiredInstance);
            }
            return requiredInstance;
        }

        static public void FreeNamedInstance(object name)
        {
            DatabaseBase requiredInstance;
            if (s_databaseInstances.TryGetValue(name, out requiredInstance))
            {
                requiredInstance.Close();
                s_databaseInstances.Remove(name);
            }
        }



        private DatabaseBase(object instanceName)
        {
            try
            {
              
                    m_sqlServerDB = new Database(m_sqlServerDBName, instanceName);
            }
            catch (Exception err)
            {
                Logger.LogException(err, "Error creating DatabaseBase");

            }
        }

        private DbConnection Connection
        {
            get
            {
                if (m_sqlServerDB != null)
                    return m_sqlServerDB.Connection;
                return null;
            }
        }

        public void Close()
        {

            if (m_sqlServerDB != null)
            {
                m_sqlServerDB.Close();
                m_sqlServerDB = null;
            }

        }

        Database m_sqlServerDB = null;

        public static DbCommand CreateCommand(string commandString)
        {

            return Database.CreateCommand(commandString);
        }


        public static DbParameter CreateParameter(string name, System.Data.SqlDbType type, object value)
        {
        

            return Database.CreateParameter(name, type, value);
        }



        public static DbParameter CreateParameter(string name, System.Data.SqlDbType type, int size, object value)
        {
    

            return Database.CreateParameter(name, type, size, value);
        }


        public static object GetColumnValueAsObject(DbDataReader reader, int columnIndex)
        {
            if (reader.FieldCount <= columnIndex)
            {
                throw new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, reader.FieldCount));
            }

            if (reader.IsDBNull(columnIndex))
                return null;

            object result = reader.GetValue(columnIndex);
            return result;
        }

        public static T? GetColumnValueAs<T>(DbDataReader reader, int columnIndex)
         where T : struct
        {
            if (reader.FieldCount <= columnIndex)
            {
                throw new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, reader.FieldCount));
            }

            if (reader.GetFieldType(columnIndex) != typeof(T))
            {
                throw new Exception("Invalid column type for column '" +
                                    reader.GetName(columnIndex) +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(T).ToString() +
                                    "' but got '" + reader.GetFieldType(columnIndex) + "'");
            }

            if (reader.IsDBNull(columnIndex))
                return null;

            T? result = (T?)reader.GetValue(columnIndex);
            return result;
        }

        public static string GetColumnValueAsString(DbDataReader reader, int columnIndex)
        {
            if (reader.FieldCount <= columnIndex)
            {
                throw new Exception(string.Format("Invalid columnIndex index= {0}. " +
                                                  "There are {1} columns in the data table",
                                                  columnIndex, reader.FieldCount));
            }

            if (reader.GetFieldType(columnIndex) != typeof(string))
            {
                throw new Exception("Invalid column type for column '" +
                                    reader.GetName(columnIndex) +
                                    "' (index = " + columnIndex +
                                    "), expected '" + typeof(string).ToString() +
                                    "' but got '" + reader.GetFieldType(columnIndex) + "'");
            }

            if (reader.IsDBNull(columnIndex))
                return null;

            string result = (string)reader.GetValue(columnIndex);
            return result;
        }


        public DbDataReader ExecuteReader(string query)
        {
            DbCommand command = CreateCommand(query);

            command.Connection = Connection;

            DbDataReader reader = command.ExecuteReader();

            return reader;
        }



        public int ExecuteNonQuery(string query)
        {

            DbCommand command = CreateCommand(query);

            command.Connection = Connection;

            int result = command.ExecuteNonQuery();

            return result;
        }

        public DbDataReader ExecuteReader(DbCommand command)
        {

            command.Connection = Connection;

            DbDataReader reader = command.ExecuteReader();

            return reader;
        }




        public void ExecuteNonQuery(DbCommand command)
        {

            command.Connection = Connection;

            command.ExecuteNonQuery();
        }

        public static T? ParseDBEnumString<T>(string stringValue) where T : struct
        {
            if (string.IsNullOrEmpty(stringValue))
                return null;

            char firstChar = stringValue[0];

            if (firstChar >= '0' && firstChar <= '9')
                stringValue = "_" + stringValue;


            T result1;
            if (Enum.TryParse(stringValue, out result1))
                return result1;

            Utils.Logger.Log("Could not parse value '" + stringValue + "' to Enum type '" + typeof(T).ToString() + "'");

            return null;

            //T? result = (T?)Enum.Parse(typeof(T), stringValue);

            //return result;
        }


        public static string ToDBEnumString<T>(Nullable<T> enumValue) where T : struct
        {
            if (!enumValue.HasValue)
                return null;

            String stringValue = Enum.GetName(typeof(T), enumValue.Value);

            if (stringValue[0] == '_')
                stringValue = stringValue.Substring(1);

            return stringValue;
        }

    }
}
