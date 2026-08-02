using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Utils
{
    public class InsertSql
    {
        public InsertSql(string tableName, int databaseId)
        {
            m_tableName = tableName;
            m_databaseId = databaseId;
        }

        public void AddParameter(string name, System.Data.SqlDbType type, object value)
        {
            m_parameters.Add(new InsertSqlParameter(name,type,value));
        }

        public void AddParameter(string name, System.Data.SqlDbType type, int size, object value)
        {
            m_parameters.Add(new InsertSqlParameter(name,type,size,value));
        }

        public DbCommand Command
        {
            get
            {
                if (m_command == null)
                {
                    bool firstParam = true;
                    string sql = "if @DatabaseId > 0 ";
                    sql += "begin ";
                    sql += " update " + m_tableName + " set ";
                    foreach (InsertSqlParameter param in m_parameters)
                    {
                        if (firstParam)
                            firstParam = false;
                        else
                            sql += ", ";
                        sql += "[" + param.Name + "] = @" + param.Name;
                    }
                    sql += ", ModifiedBy = user, ModifiedTime = @modifiedTime";
                    sql += " where " + m_tableName + "Id = @DatabaseId";
                    sql += " select @DatabaseId as DatabaseId, @modifiedTime as ModifiedTime ";
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

                    sql += ",user,@modifiedTime) ";
                    sql += " select convert(int,@@IDENTITY) as DatabaseId, @modifiedTime as ModifiedTime ";
                    sql += "end";


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
        string m_tableName;
        int m_databaseId;
    }
}
