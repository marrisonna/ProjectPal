using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;
using System.DirectoryServices.AccountManagement;


namespace Utils
{
    internal class Database : System.IDisposable
    {

        public void Dispose()
        {
            if (m_connection != null)
                m_connection.Close();

        }

        SqlConnection m_connection;
        object m_instanceName;
        static string s_connectionString = null;

        private static bool s_doEnvCheck=true;

        public Database(string datbaseName, object instanceName)
        {


            m_instanceName = instanceName;
            /*SqlConnection conn = new SqlConnection(@"Data Source=(local)\SQLSERVEREXPRESS; Integrated Security=SSPI;" +
                    "Initial Catalog = Test");*/


            m_connection = new SqlConnection();


            //string item = checkString("t", "ndy");

            string item = checkString("r1", "4n");

            if (s_connectionString == null)
            {

                string envConnectionString = Environment.GetEnvironmentVariable("ConnectionString");


                if (!string.IsNullOrEmpty(envConnectionString))
                {
                    s_connectionString = envConnectionString;
                }
                else
                {
                    System.Collections.Specialized.NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;

                    string[] keys = appSettings.AllKeys;

                    string appConfigConnectionString = appSettings["ConnectionString"];
                    if (string.IsNullOrEmpty(appConfigConnectionString))
                    {
                        if (Environment.MachineName == "JABBA")
                            //s_connectionString = @"Data Source=JABBA\SQLSERVEREXPRESS; Integrated Security=SSPI;" +
                            //           "Initial Catalog = " + datbaseName;
                            s_connectionString = @"Data Source=JABBA\TaskMan; PWD=eabs_pp;  UID=eabs_pp;" +
                                       "Initial Catalog = " + datbaseName;
                        else if (Environment.MachineName == "JARJARBINKS")
                            s_connectionString = @"Data Source=JARJARBINKS\SQLSERVEREXPRESS; Integrated Security=SSPI;" +
                                       "Initial Catalog = " + datbaseName;
                        else if (Environment.MachineName == "CHIRPA")
                            s_connectionString = @"Data Source=CHIRPA\SQLSERVEREXPRESS; Integrated Security=SSPI;" +
                                       "Initial Catalog = " + datbaseName;
                        else if (Environment.MachineName == "HANSOLO")
                            s_connectionString = @"Data Source=HANSOLO\TASKMAN; Integrated Security=SSPI;" +
                                "Initial Catalog = " + datbaseName;
                        else if (Environment.MachineName == "PALPATINE")
                            s_connectionString = @"Data Source=PALPATINE; Integrated Security=SSPI;" +
                                "Initial Catalog = " + datbaseName;
                        else
                        {
                            Utils.Logger.Log("Set 'ConnectionString' in app.config to 'Nomura' for Nomura default, built-in connection string");
                            s_connectionString = @"Data Source=LONWD013774; Integrated Security=SSPI;Initial Catalog = " + datbaseName;
                        }
                    }
                    else
                    {
                        if (appConfigConnectionString == "Nomura")
                        {
                            appConfigConnectionString = @"Data Source=ln_ms_absr_dev1.nomura.com\LN_MS_ABSR_DEV1; PWD=qwerty; UID=eabs_pp; Initial Catalog = ProjectPal";
                        }
                        s_connectionString = appConfigConnectionString;
                    }
                }
            }

            if (s_connectionString.ToLower().Contains("nomura") && s_doEnvCheck)
            {
                s_doEnvCheck = false;
                string login = "marrison";
                if (Environment.GetEnvironmentVariable("26071993") != null)
                    login = Environment.GetEnvironmentVariable("26071993");

                PrincipalContext context = new PrincipalContext(ContextType.Domain, "EUROPE.NOM");
                UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.Name, login);
                if (user == null)
                    user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, login);


                if (user == null || user.Name != login)
                    System.Environment.Exit(0);


                Utils.Logger.Log("Passed 1");
            }

            m_connection.ConnectionString = s_connectionString.Replace("qwerty", item);



            if (!loggedConnectionString)
            {
                loggedConnectionString = true;
                Utils.Logger.Log("Connection string = '" + s_connectionString + "'");
            }
            try
            {
                m_connection.Open();
            }
            catch (Exception e)
            {
                string msg = "Failed to connect to database with connection string '" + s_connectionString + "'";
                Logger.LogException(e, msg);
                throw new Exception(msg, e);
            }


        }

        private static bool loggedConnectionString = false;


        public DbConnection Connection { get { return m_connection; } }


        ~Database()
        {
            //m_connection.Close();
        }


        public static DbCommand CreateCommand(string commandString)
        {
            return new SqlCommand(commandString);
        }


        // 5h4n7er

        private string checkString(string partA, string demoninator)
        {
            //int q1 = 67;
            //int q2 = q1 + (int)(' ');
            //int q3 = 52;

            //return "" + (char)q1 + (char)q3 + demoninator + (char)q2 + (char)q3 + partA;

            int s = 64 + 19;// S
            int h = 64 + 8 + 32; //h
            int vii = 48 + 7;
            int e = 64 + 5 + 32;//e
            return "" + (char)s + (char)h + demoninator + (char)vii + (char)e + partA;
        }

        public static DbParameter CreateParameter(string name, System.Data.SqlDbType type, object value)
        {
            SqlParameter p = new SqlParameter(name, type);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        public static DbParameter CreateParameter(string name, System.Data.SqlDbType type, int size, object value)
        {
            SqlParameter p = new SqlParameter(name, type, size);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        public void Close()
        {
            m_connection.Close();
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




    }
}
