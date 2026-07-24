using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Data.Common;
using System.Text;


// "C:\Program Files (x86)\Common Files\microsoft shared\Help 8\dexplore.exe" /helpcol ms-help://MS.SSC.v35 /usehelpsettings SSCBooksOnline.3.5 /LaunchNamedUrlTopic DefaultPage


namespace Utils
{
    internal class DatabaseCE : System.IDisposable
    {

        static public void Check()
        {

            SqlCeConnection temp = new SqlCeConnection();
        }

        public void Dispose()
        {
            if (m_connection != null)
                m_connection.Close();

        }

        public static void Test()
        {
            DatabaseCE db = new DatabaseCE(@"F:\Users\Neil\Documents\dev\SqlCeDBs\db1.sdf", "test");

            db.BeginTransaction();


            DbDataReader r = db.ExecuteReader("select * from abc");
            while (r.Read())
            {
                string q = DatabaseCE.GetColumnValueAsString(r, 0);

            }
            r.Close();

            db.Commit();

            DbDataReader r2 = db.ExecuteReader("select * from abc");
            while (r2.Read())
            {
                string q = DatabaseCE.GetColumnValueAsString(r2, 0);

            }
            r2.Close();

        }


        public DbConnection Connection { get { return m_connection; } }


        public static DbCommand CreateCommand(string commandString)
        {
            return new SqlCeCommand(commandString);
        }

        public static DbParameter CreateParameter(string name, System.Data.SqlDbType type, object value)
        {
            SqlCeParameter p = new SqlCeParameter(name, type);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        public static DbParameter CreateParameter(string name, System.Data.SqlDbType type, int size, object value)
        {
            SqlCeParameter p = new SqlCeParameter(name, type, size);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        SqlCeConnection m_connection;
        SqlCeTransaction m_currentTran = null;
        object m_instanceName;

        public DatabaseCE(string datbaseName, object instanceName)
        {

            m_instanceName = instanceName;
            /*SqlConnection conn = new SqlConnection(@"Data Source=(local)\SQLSERVEREXPRESS; Integrated Security=SSPI;" +
                    "Initial Catalog = Test");*/


            m_connection = new SqlCeConnection();

            m_connection.ConnectionString = "Data Source = '" + datbaseName + "';";


            Utils.Logger.Log("Connection string = '" + m_connection.ConnectionString + "'");
            try
            {
                m_connection.Open();
            }
            catch (Exception e)
            {
                string msg = "Failed to connect to database with connection string '" + m_connection.ConnectionString + "', trying to create it.";
                Logger.LogException(e, msg);

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(datbaseName));

                SqlCeEngine engine = new SqlCeEngine(m_connection.ConnectionString);
                engine.CreateDatabase();

                try
                {
                    m_connection.Open();
                }
                catch (Exception e2)
                {
                    string msg2 = "Failed to connect to database with connection string '" + m_connection.ConnectionString + "' and could not create it.";

                    Logger.LogException(e2, msg2);
                    throw new Exception(msg2, e2);
                }

            }


        }

        ~DatabaseCE()
        {
            //m_connection.Close();
        }

        public void Close()
        {
            m_connection.Close();
        }

        public void BeginTransaction()
        {
            m_currentTran = m_connection.BeginTransaction();
        }

        public void Commit()
        {
            if (m_currentTran != null)
            {
                m_currentTran.Commit();
                m_currentTran = null;
            }
        }

        public void Rollback()
        {
            if (m_currentTran != null)
            {
                m_currentTran.Rollback();
                m_currentTran = null;
            }
        }

        public DbDataReader ExecuteReader(DbCommand command)
        {

            command.Connection = m_connection;

            DbDataReader reader = command.ExecuteReader();

            return reader;
        }


        public void ExecuteNonQuery(DbCommand command)
        {

            command.Connection = m_connection;

            command.ExecuteNonQuery();
        }





        public DbDataReader ExecuteReader(string query)
        {
            SqlCeCommand command = new SqlCeCommand(query, m_connection);

            SqlCeDataReader reader = command.ExecuteReader();

            return reader;
        }

        public int ExecuteNonQuery(string query)
        {

            SqlCeCommand command = new SqlCeCommand(query, m_connection);

            int result = command.ExecuteNonQuery();

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

        #region AsSQLString

        public static string AsSqlString(string value)
        {
            if (null == value)
            {
                return "null";
            }
            value = value.Replace("'", "''");
            value = "'" + value + "'";
            return value;
        }

        public static string AsSqlString(DateTime? value)
        {
            if (false == value.HasValue)
            {
                return "null";
            }
            return "'" + value.Value.ToString("dd-MMM-yyyy HH:mmm:ss.fff tt") + "'";
        }

        public static string AsSqlString(bool? value)
        {
            if (false == value.HasValue)
            {
                return "null";
            }
            return "'" + (value.Value ? 'Y' : 'N') + "'";
        }

        public static string AsSqlString(int? value, Type enumType)
        {
            if (!value.HasValue)
                return "null";
            String name = Enum.GetName(enumType, value.Value);

            // Get rid of leading '_' since these indicate the value starts with a number
            // which enums cannot, so an '_' is added.
            while (name[0] == '_')
                name = name.Substring(1);

            return "'" + name + "'";
        }


        public static string AsSqlString<T>(Nullable<T> value) where T : struct
        {
            if (!value.HasValue)
                return "null";

            bool isEnum = false;
            try
            {

                Enum.IsDefined(typeof(T), value.Value);
                isEnum = true;
            }
            catch (Exception)
            { }

            if (isEnum)
            {
                String name = Enum.GetName(typeof(T), value.Value);

                // Get rid of leading '_' since these indicate the value starts with a number
                // which enums cannot, so an '_' is added.
                while (name[0] == '_')
                    name = name.Substring(1);

                return "'" + name + "'";

            }

            return value.Value.ToString();
        }


        //public static string AsSqlString<T>(Nullable<T> value)
        //    where T : struct
        //{
        //    if (false == value.HasValue)
        //    {
        //        return "null";
        //    }
        //    return value.Value.ToString();
        //}

        //public static string AsSqlString<T>(Nullable<T> value, string format)
        //    where T : struct
        //{

        //    // Hmmm, I wonder which 'ToString' method gets called.
        //    if (false == value.HasValue)
        //    {
        //        return "null";
        //    }
        //    T actualValue = value.Value;
        //    return actualValue.ToString();
        //}

        #endregion


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
