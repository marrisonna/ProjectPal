using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using Utilities.Logging;
using Utilities.Configuration;
using System.Text;

namespace Utilities.Database
{
    public class Database
    {
        #region Private members

        private DbTransaction m_transaction = null;
        private DbConnection m_connection = null;

        //default timeout in seconds
        private int m_timeout = 30;

        //batch size when updating a datatable
        //1 means one row at a  time (no batch)
        private int m_batchSize = 1;

        private static Dictionary<Type, DbType> LookUpType;

        #endregion

        #region Constructor

        // Constructor for internal members
        // used by ABSCOnfig
        internal Database()
        {
            Logger.TraceMethodEntry();
            CreateDatabase(ABSConfig.ConfigDBConnectionString);
            Logger.TraceMethodExit();
        }

        // Construct a db from a config Key
        public Database(string configKey)
        {
            Logger.TraceMethodEntry();
            Logger.Debug("Config Key = '{0}'", configKey);

            if (String.IsNullOrEmpty(configKey))
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty config key"));
            }

            string connectionString = ABSConfig.GetValue(configKey);
            Logger.Debug("connectionString = '{0}'", connectionString);
            if (String.IsNullOrEmpty(connectionString))
            {
                Logger.Warn("Null or empty connection string from Config, using '{0}' instead", configKey);
                connectionString = configKey;
            }
            CreateDatabase(connectionString);
            Logger.TraceMethodExit();
        }

        private void CreateDatabase(string connectionString)
        {
            Logger.Info("CreateDatabase: Connection string = '{0}'", connectionString);
            string providerName = GetProviderName();
            DbProviderFactory providerFactory = DbProviderFactories.GetFactory(providerName);

            try
            {
                Logger.Trace("About to try to connect to the database");
                m_EntLibDatabase = new GenericDatabase(connectionString, providerFactory);

                // try establishing a connection
                DbConnection conn = m_EntLibDatabase.CreateConnection();
                conn.Dispose();

                Logger.Debug("Connected to database. Connection string is '{0}'",
                         connectionString);
            }
            catch (Exception ex) //if it fails, throw argument exception
            {
                Logger.ErrorException(ex, "Exception connecting to Database.  Input Connection String : '{0}'", connectionString);
                Logger.ErrorThrow(new ArgumentException("Exception Occurred", ex));
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// command timeout
        /// </summary>
        public int CommandTimeout
        {
            get { return m_timeout; }
            set { m_timeout = value; }
        }

        public char ParameterToken
        {
            get { return '@'; }
        }

        public int UpdateBatchSize
        {
            get { return m_batchSize; }
            set { m_batchSize = value; }
        }
        #endregion

        #region Private methods

        static void InitLookUp()
        {
            LookUpType = new Dictionary<Type, DbType>();

            LookUpType.Add(typeof(Int16), DbType.Int16);
            LookUpType.Add(typeof(int), DbType.Int32);
            LookUpType.Add(typeof(Int64), DbType.Int64);
            LookUpType.Add(typeof(bool), DbType.Boolean);
            LookUpType.Add(typeof(double), DbType.Double);
            LookUpType.Add(typeof(decimal), DbType.Decimal);
            LookUpType.Add(typeof(float), DbType.Decimal);
            LookUpType.Add(typeof(DateTime), DbType.DateTime);
            LookUpType.Add(typeof(Guid), DbType.Guid);
            LookUpType.Add(typeof(byte), DbType.Byte);
        }

        #endregion

        #region protected Overridable methods

        protected virtual string GetProviderName()
        {
            return "System.Data.SqlClient";
        }

        #endregion

        #region Public Methods

        public IDbCommand GetStoredProcCommand(string storedProcName)
        {
            Logger.TraceMethodEntry();
            Logger.Trace("Params '{0}'", storedProcName);

            if (String.IsNullOrEmpty(storedProcName))
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty stored proc name"));
            }

            DbCommand cmd = m_EntLibDatabase.GetStoredProcCommand(storedProcName);
            cmd.CommandTimeout = CommandTimeout;

            Logger.TraceMethodExit();
            return cmd;
        }

        #region Add Parameters

        //allows addition of nullable value types (ex: int,double,datetime,decimal etc)
        // Type checking enforced at compile time
        public void AddInParameter<T>(IDbCommand cmd, string parameterName, T? value)
            where T : struct
        {
            Logger.TraceMethodEntry();

            IDataParameter param = cmd.CreateParameter();

            if (LookUpType == null)
                InitLookUp();

            //internal lookup which maps type of T to corresponding DbType 
            if (LookUpType.ContainsKey(typeof(T)))
            {
                param.DbType = LookUpType[typeof(T)];

                if (Logger.IsTraceEnabled())
                {
                    if (value.HasValue)
                    {
                        //If value type is DateTime, logs the datetime value as "dddd, MMMM dd, yyyy h:mm:ss tt"
                        //eg: Wednesday, May 16, 2001 3:02:15 AM
                        if (param.DbType == DbType.DateTime)
                        {
                            DateTime dt = Convert.ToDateTime(value.Value);
                            Logger.Trace("Params: CommandName: '{0}', ParameterName: '{1}', Value: '{2}' ",
                                cmd.CommandText, parameterName, dt.ToLongDateString() + " " + dt.ToLongTimeString());
                        }
                        else
                        {
                            Logger.Trace("Params: CommandName: '{0}', ParameterName: '{1}', Value: '{2}' ", cmd.CommandText, parameterName, value);
                        }
                    }
                    else
                    {
                        Logger.Trace("Params: CommandName: '{0}', ParameterName: '{1}'', Value: 'null' ", cmd.CommandText, parameterName);
                    }
                }
            }
            else
            {
                Logger.Error("Internal LookUp doesnot support type '{0}'", typeof(T).ToString());
                Logger.ErrorThrow(new Exception("Invalid parameter type"));
            }

            param.ParameterName = parameterName;
            param.Direction = ParameterDirection.Input;

            //Database doesnt understand NULL.. it understands DBNULL...so we do the conversion here
            if (value == null)
            {
                param.Value = DBNull.Value;
            }
            else
            {
                param.Value = value;
            }

            cmd.Parameters.Add(param);

            Logger.TraceMethodExit();
        }

        //allows addition of any type parameters
        public void AddInParameter(IDbCommand cmd, string parameterName, DbType type, object value)
        {
            Logger.TraceMethodEntry();

            if (Logger.IsTraceEnabled())
            {
                if (value != null)
                {
                    Logger.Trace("Params: CommandName: '{0}', ParameterName: '{1}', DbType: '{2}', Value: '{3}'",
                        cmd.CommandText, parameterName, type.ToString(), value.ToString());
                }
                else
                {
                    Logger.Trace("Params: CommandName: '{0}', ParameterName: '{1}', DbType: '{2}', Value: null",
                        cmd.CommandText, parameterName, type.ToString());
                }
            }

            IDataParameter param = cmd.CreateParameter();

            param.ParameterName = parameterName;
            param.DbType = type;
            param.Direction = ParameterDirection.Input;

            //Database doesnt understand NULL.. it understands DBNULL...so we do the conversion here            
            if (value == null)
            {
                param.Value = DBNull.Value;
            }
            else
            {
                param.Value = value;
            }


            cmd.Parameters.Add(param);

            Logger.TraceMethodExit();
        }

        public void AddOutParameter(IDbCommand cmd, string parameterName, DbType type, int size)
        {
            Logger.TraceMethodEntry();
            if (Logger.IsTraceEnabled())
            {
                Logger.Trace("Params: CommandName: '{0}', ParameterName: '{1}', DbType: '{2}' ",
                    cmd.CommandText, parameterName, type.ToString());
            }

            this.m_EntLibDatabase.AddOutParameter((System.Data.Common.DbCommand)cmd, parameterName, type, size);


            Logger.TraceMethodExit();
        }

        public void AddInParameter(IDbCommand cmd, string parameterName, DbType type, string sourceColumn, DataRowVersion version)
        {
            DbCommand command = cmd as DbCommand;
            m_EntLibDatabase.AddInParameter(command, parameterName, type, sourceColumn, version);

        }

        /// <summary>
        /// eg. parameters - int, datetime
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameterName"></param>
        /// <param name="type"></param>
        /// <param name="sourceColumn"></param>
        /// <param name="version"></param>
        public void AddOutParameter(IDbCommand cmd, string parameterName, DbType type, string sourceColumn, DataRowVersion version)
        {
            DbCommand command = cmd as DbCommand;
            m_EntLibDatabase.AddParameter(command, parameterName, type, ParameterDirection.Output, sourceColumn, version, parameterName);

        }

        /// <summary>
        /// Use this method when you need to specify the size of the output parameter for eg; varchar(50)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameterName"></param>
        /// <param name="type"></param>
        /// <param name="size"></param>
        /// <param name="nullable"></param>
        /// <param name="scale"></param>
        /// <param name="precision"></param>
        /// <param name="sourceColumn"></param>
        /// <param name="version"></param>
        public void AddOutParameter(IDbCommand cmd, string parameterName, DbType type, int size, bool nullable, byte scale, byte precision, string sourceColumn, DataRowVersion version)
        {
            DbCommand command = cmd as DbCommand;
            m_EntLibDatabase.AddParameter(command, parameterName, type, size, ParameterDirection.Output, nullable, scale, precision, sourceColumn, version, parameterName);

        }
        #endregion

        #region Get Parameter Value
        //allows addition of nullable value types (ex: int,double,datetime,decimal etc)
        // Type checking enforced at compile time
        public T GetParameterValue<T>(IDbCommand cmd, string parameterName)
            where T : struct
        {
            Logger.TraceMethodEntry();
            Logger.TraceMethodExit();
            return (T)m_EntLibDatabase.GetParameterValue(cmd as DbCommand, parameterName);

        }
        #endregion
        #region Query Execution

        public DataSet ExecuteDataSet(IDbCommand cmd)
        {
            Logger.TraceMethodEntry();

            DbCommand _cmd = cmd as DbCommand;
            if (_cmd == null)
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty command"));
            }

            if (Logger.IsTraceEnabled())
            {
                Logger.Trace("Params '{0}'", GetCommandString(_cmd));
            }

            DataSet returnDataSet = null;
            int retryCount = 3;
            bool retry = true;
            while (retryCount > 0 && retry)
            {
                try
                {
                    if (m_transaction == null)
                    {
                        returnDataSet = m_EntLibDatabase.ExecuteDataSet(_cmd);
                        retry = false;
                    }
                    else
                    {
                        returnDataSet = m_EntLibDatabase.ExecuteDataSet(_cmd, m_transaction);
                        retry = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: ExecuteDataSet failed for command '{0}'", GetCommandString(_cmd));
                    if (ex.Message.Substring(0, 7) != "Timeout" || retryCount == 0)
                    {
                        Logger.TraceMethodExit();
                        Logger.ErrorThrow(new DbException("ExecuteDataSet failed", ex, null));
                    }
                    retryCount--;
                    Logger.Info("Request timed out - retry = {0}", retryCount);
                }
            }
            Logger.TraceMethodExit();
            return returnDataSet;
        }

        public int ExecuteNonQuery(IDbCommand cmd)
        {
            Logger.TraceMethodEntry();
            DbCommand _cmd = cmd as DbCommand;

            if (_cmd == null)
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty command"));
            }

            Logger.Trace("Params '{0}'", GetCommandString(_cmd));

            //-1 is returned by ExecuteNonQuery for queries other than UPDATE, INSERT, and DELETE and ROLLBACK.
            int rowAffected = Int32.MinValue;
            int retryCount = 3;
            bool retry = true;
            while (retryCount > 0 && retry)
            {
                try
                {
                    if (m_transaction == null)
                    {
                        rowAffected = m_EntLibDatabase.ExecuteNonQuery(_cmd);
                        retry = false;
                    }
                    else
                    {
                        rowAffected = m_EntLibDatabase.ExecuteNonQuery(_cmd, m_transaction);
                        retry = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: ExecuteNonQuery failed for command '{0}'", GetCommandString(_cmd));
                    if (ex.Message.Substring(0, 7) != "Timeout" || retryCount == 0)
                    {
                        Logger.TraceMethodExit();
                        Logger.ErrorThrow(new DbException("ExecuteNonQuery failed", ex, null));
                    }
                    retryCount--;
                    Logger.Info("Request timed out - retry = {0}", retryCount);

                }
            }
            Logger.TraceMethodExit();
            return rowAffected;
        }

        public IDataReader ExecuteReader(IDbCommand cmd)
        {
            Logger.TraceMethodEntry();
            DbCommand _cmd = cmd as DbCommand;

            if (_cmd == null)
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty command"));
            }
            if (Logger.IsTraceEnabled())
            {
                Logger.Trace("Params '{0}'", GetCommandString(_cmd));
            }

            IDataReader reader = null;
            int retryCount = 3;
            bool retry = true;
            while (retryCount > 0 && retry)
            {
                try
                {
                    if (m_transaction == null)
                    {
                        reader = m_EntLibDatabase.ExecuteReader(_cmd);
                        retry = false;
                    }
                    else
                    {
                        reader = m_EntLibDatabase.ExecuteReader(_cmd, m_transaction);
                        retry = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: ExecuteReader failed for command '{0}'", GetCommandString(_cmd));
                    if (ex.Message.Substring(0, 7) != "Timeout" || retryCount == 0)
                    {
                        Logger.TraceMethodExit();
                        Logger.ErrorThrow(new DbException("ExecuteReader failed", ex, null));
                    }
                    retryCount--;
                    Logger.Info("Request timed out - retry = {0}", retryCount);

                }

            }
            Logger.TraceMethodExit();
            return reader;
        }

        public DataSet ExecuteDataSet(string sqlString)
        {
            Logger.TraceMethodEntry();
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug("ExecuteDataSet: SQL = '{0}'", sqlString);
            }

            if (string.IsNullOrEmpty(sqlString))
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty sql string"));
            }
            DataSet dataset = null;
            int retryCount = 3;
            bool retry = true;
            while (retryCount > 0 && retry)
            {
                try
                {
                    if (m_transaction == null)
                    {
                        dataset = m_EntLibDatabase.ExecuteDataSet(CommandType.Text, sqlString);
                        retry = false;
                    }
                    else
                    {
                        dataset = m_EntLibDatabase.ExecuteDataSet(m_transaction, CommandType.Text, sqlString);
                        retry = false;
                    }

                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: ExecuteDataSet failed for Sql query '{0}'", sqlString);
                    if (ex.Message.Substring(0, 7) != "Timeout" || retryCount == 0)
                    {
                        Logger.TraceMethodExit();
                        Logger.ErrorThrow(new DbException("ExecuteDataSet failed", ex, null));
                    }
                    retryCount--;
                    Logger.Info("Request timed out - retry = {0}", retryCount);
                }
            }
            Logger.TraceMethodExit();
            return dataset;
        }

        public IDataReader ExecuteReader(string sqlString)
        {
            Logger.TraceMethodEntry();
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug("ExecuteReader: SQL = '{0}'", sqlString);
            }

            if (String.IsNullOrEmpty(sqlString))
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty sql string"));
            }

            IDataReader reader = null;
            int retryCount = 3;
            bool retry = true;
            while (retryCount > 0 && retry)
            {
                try
                {
                    if (m_transaction == null)
                    {
                        reader = m_EntLibDatabase.ExecuteReader(CommandType.Text, sqlString);
                        retry = false;
                    }
                    else
                    {
                        reader = m_EntLibDatabase.ExecuteReader(m_transaction, CommandType.Text, sqlString);
                        retry = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: ExecuteReader failed for Sql query '{0}'", sqlString);
                    if (ex.Message.Substring(0, 7) != "Timeout" || retryCount == 0)
                    {
                        Logger.TraceMethodExit();
                        Logger.ErrorThrow(new DbException("ExecuteReader failed", ex, null));
                    }
                    retryCount--;
                    Logger.Info("Request timed out - retry = {0}", retryCount);


                }
            }
            Logger.TraceMethodExit();
            return reader;
        }

        public int ExecuteNonQuery(string sqlString)
        {
            Logger.TraceMethodEntry();
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug("ExecuteNonQuery: SQL = '{0}'", sqlString);
            }

            if (String.IsNullOrEmpty(sqlString))
            {
                Logger.ErrorThrow(new ArgumentException("Null or empty sql string"));
            }

            //-1 is returned by ExecuteNonQuery for queries other than UPDATE, INSERT, and DELETE and ROLLBACK.
            int rowAffected = Int32.MinValue;
            int retryCount = 3;
            bool retry = true;
            while (retryCount > 0 && retry)
            {
                try
                {
                    if (m_transaction == null)
                    {
                        rowAffected = m_EntLibDatabase.ExecuteNonQuery(CommandType.Text, sqlString);
                        retry = false;
                    }
                    else
                    {
                        rowAffected = m_EntLibDatabase.ExecuteNonQuery(m_transaction, CommandType.Text, sqlString);
                        retry = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: ExecuteNonQuery failed for Sql query '{0}'", sqlString);
                    if (ex.Message.Substring(0, 7) != "Timeout" || retryCount == 0)
                    {
                        Logger.TraceMethodExit();
                        Logger.ErrorThrow(new DbException("ExecuteNonQuery failed", ex, null));
                    }
                    retryCount--;
                    Logger.Info("Request timed out - retry = {0}", retryCount);

                }
            }
            Logger.TraceMethodExit();
            return rowAffected;
        }

        public int UpdateDataTable(DataTable table, IDbCommand insertCommand, IDbCommand deleteCommand, IDbCommand updateCommand)
        {
            int rowsAffected = -1;

            DbDataAdapter adapter = m_EntLibDatabase.GetDataAdapter();
            adapter.UpdateBatchSize = UpdateBatchSize;

            DbCommand insertCmd = null;
            DbCommand deleteCmd = null;
            DbCommand updateCmd = null;

            DbConnection conn = null;

            try
            {
                if (m_transaction == null)
                {
                    conn = m_EntLibDatabase.CreateConnection();
                }

                if (insertCommand != null)
                {
                    insertCmd = insertCommand as DbCommand;

                    //if batch size is specified (0 means all) then UpdateRowSource should be set 
                    //to None
                    if (UpdateBatchSize == 0 || UpdateBatchSize > 1)
                    {
                        insertCmd.UpdatedRowSource = UpdateRowSource.None;
                    }
                    if (m_transaction != null)
                    {
                        insertCmd.Connection = m_connection;
                        insertCmd.Transaction = m_transaction;
                    }
                    else
                    {
                        insertCmd.Connection = conn;
                    }
                }
                if (deleteCommand != null)
                {
                    deleteCmd = deleteCommand as DbCommand;
                    deleteCmd.Connection = conn;
                    if (UpdateBatchSize == 0 || UpdateBatchSize > 1)
                    {
                        deleteCmd.UpdatedRowSource = UpdateRowSource.None;
                    }
                    if (m_transaction != null)
                    {
                        deleteCmd.Connection = m_connection;
                        deleteCmd.Transaction = m_transaction;
                    }
                    else
                    {
                        deleteCmd.Connection = conn;
                    }
                }
                if (updateCommand != null)
                {
                    updateCmd = updateCommand as DbCommand;
                    updateCmd.Connection = conn;
                    if (UpdateBatchSize == 0 || UpdateBatchSize > 1)
                    {
                        updateCmd.UpdatedRowSource = UpdateRowSource.None;
                    }
                    if (m_transaction != null)
                    {
                        updateCmd.Connection = m_connection;
                        updateCmd.Transaction = m_transaction;
                    }
                    else
                    {
                        updateCmd.Connection = conn;
                    }
                }
                if (adapter != null)
                {
                    adapter.InsertCommand = insertCmd;
                    adapter.DeleteCommand = deleteCmd;
                    adapter.UpdateCommand = updateCmd;

                    rowsAffected = adapter.Update(table);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException(ex, "Exception: UpdateDataTable failed for table '{0}'", table);

                Logger.ErrorThrow(new DbException("UpdateDataTable failed", ex, null));
            }


            return rowsAffected;
        }

        #endregion

        #region Transactions

        public void BeginTransaction()
        {
            Logger.TraceMethodEntry();

            if (m_transaction == null)
            {
                try
                {
                    m_connection = m_EntLibDatabase.CreateConnection();
                    m_connection.Open();
                    m_transaction = m_connection.BeginTransaction();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: Error during transaction begin");
                    Logger.ErrorThrow(ex);
                }
            }
            else
            {
                Logger.ErrorThrow(new Exception("Nested transactions are not yet implemented."));
            }
            Logger.TraceMethodExit();
        }

        public void CommitTransaction()
        {
            Logger.TraceMethodEntry();

            if (m_transaction != null)
            {
                try
                {
                    m_transaction.Commit();
                    m_connection.Close();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: Error during transaction commit");
                    Logger.ErrorThrow(ex);
                }

                finally
                {
                    m_transaction = null;
                    m_connection = null;
                }
            }
            else
            {
                Logger.ErrorThrow(new Exception("No transaction exists to commit."));
            }
            Logger.TraceMethodExit();
        }

        public void RollbackTransaction()
        {
            Logger.TraceMethodEntry();

            if (m_transaction != null)
            {
                try
                {
                    m_transaction.Rollback();
                    m_connection.Close();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException(ex, "Exception: Error during transaction rollback");
                    Logger.ErrorThrow(ex);
                }
                finally
                {
                    m_transaction = null;
                    m_connection = null;
                }
            }
            else
            {
                Logger.ErrorThrow(new Exception("No transaction to rollback."));
            }
            Logger.TraceMethodExit();
        }

        #endregion

        #endregion

        private string GetCommandString(IDbCommand cmd)
        {
            if (cmd == null)
            {
                Logger.ErrorThrow(new ArgumentException("Null input parameter"));
            }
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                StringBuilder builder = new StringBuilder("execute ");
                builder.Append(cmd.CommandText);
                builder.Append(" ");

                foreach (IDataParameter parameter in cmd.Parameters)
                {
                    //handles only IN parameters for now
                    if (parameter.Direction == ParameterDirection.Input)
                    {
                        builder.Append(this.ParameterToken + parameter.ParameterName + " = ");
                        builder.Append("'" + parameter.Value + "',");
                    }
                }
                return builder.ToString().TrimEnd(',');
            }
            return cmd.CommandText;
        }
    }
}
