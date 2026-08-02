using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Utils
{
    public class SqlInsert
    {
        public SqlInsert(string tableName, int databaseId)
        {
            m_entityName = tableName;
            m_tableName = DatabaseBase.Schema + tableName;
            m_databaseId = databaseId;
        }

        private static string GO = Environment.NewLine;


        public void AddParameter(string name, System.Data.SqlDbType type, object value)
        {
            m_parameters.Add(new InsertSqlParameter(name, type, value));
        }

        public void AddParameter(string name, System.Data.SqlDbType type, int size, object value)
        {
            m_parameters.Add(new InsertSqlParameter(name, type, size, value));
        }

        static bool m_copyMode = false;
        public static bool CopyMode { get { return m_copyMode; } set { m_copyMode = value; } }

        public DbCommand Command
        {
            get
            {
                if (m_command == null)
                {
                    string thisUser = "'" + DatabaseBase.CurrentUser + "'";
                    string sql = "";


                    if (m_copyMode)
                    {
                        //sql += "set identity_insert "+m_tableName+" on";
                        //sql += GO + "go" + GO;

                        sql += "insert into " + m_tableName + " ([" + m_entityName + "Id]";
                        foreach (InsertSqlParameter param in m_parameters)
                        {
                            sql += ",[" + param.Name + "]";
                        }
                        sql += ", [ModifiedBy],[ModifiedTime]) values ( @DatabaseId";

                        foreach (InsertSqlParameter param in m_parameters)
                        {
                            sql += ",@" + param.Name;
                        }

                        sql += ", " + thisUser + ", GETDATE() )";

                        sql += GO;
                        //sql += "go" + GO;

                        //sql += "set identity_insert " + m_tableName + " off";
                        //sql += GO + "go" + GO; ;

                    }
                    else
                    {
                        sql += "update " + DatabaseBase.Schema + "[System] set LastUpdateTime = GETDATE()  ";
                        sql += GO;

                        sql += "if @DatabaseId > 0 ";
                        sql += "begin ";
                        sql += " update " + m_tableName + " set ";

                        bool firstParam = true;
                        foreach (InsertSqlParameter param in m_parameters)
                        {
                            if (firstParam)
                                firstParam = false;
                            else
                                sql += ", ";
                            sql += "[" + param.Name + "] = @" + param.Name;
                        }
                        sql += ", ModifiedBy = " + thisUser + ", ModifiedTime =  GETDATE()";
                        sql += " where " + m_entityName + "Id = @DatabaseId";
                        sql += " select  " + m_entityName + "Id, ModifiedTime from " + m_tableName + " where " + m_entityName + "Id = @DatabaseId";
                        sql += " end ";
                        sql += " else ";
                        sql += " begin ";

                        sql += " insert into " + m_tableName + "( ";

                        firstParam = true;
                        foreach (InsertSqlParameter param in m_parameters)
                        {
                            if (firstParam)
                                firstParam = false;
                            else
                                sql += ", ";
                            sql += "[" + param.Name + "] ";
                        }
                        sql += ",ModifiedBy, ModifiedTime) values (";
                        firstParam = true;
                        foreach (InsertSqlParameter param in m_parameters)
                        {
                            if (firstParam)
                                firstParam = false;
                            else
                                sql += ", ";
                            sql += "@" + param.Name;
                        }

                        sql += "," + thisUser + ",GETDATE()) ";
                        sql += " select " + m_entityName + "Id, ModifiedTime from " + m_tableName + " where " + m_entityName + "Id = convert(int,@@IDENTITY) ";

                        sql += "end";
                        sql += GO;

                    }
                    // Add in the parameters
                    m_command = DatabaseBase.CreateCommand(sql);
                    m_command.Parameters.Add(DatabaseBase.CreateParameter("@DatabaseId", System.Data.SqlDbType.Int, m_databaseId));

                    foreach (InsertSqlParameter param in m_parameters)
                    {
                        if (param.Size == null)
                            m_command.Parameters.Add(DatabaseBase.CreateParameter("@" + param.Name, param.Type, param.Value));
                        else
                            m_command.Parameters.Add(DatabaseBase.CreateParameter("@" + param.Name, param.Type, param.Size.Value, param.Value));
                    }


                }
                return m_command;


            }

        }



        List<InsertSqlParameter> m_parameters = new List<InsertSqlParameter>();
        DbCommand m_command = null;
        string m_entityName;
        string m_tableName;
        int m_databaseId;
    }
}
