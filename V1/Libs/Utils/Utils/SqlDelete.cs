using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Utils
{
    public class SqlDelete
    {
        public SqlDelete(string tableName, int databaseId)
        {
            m_entityName = tableName;
            m_tableName = DatabaseBase.Schema + tableName;
            m_databaseId = databaseId;
        }

        
        public DbCommand Command
        {
            get
            {
                if (m_command == null)
                {
                    string sql = string.Format("delete from {0} where {1}Id = {2}", m_tableName, m_entityName,m_databaseId);
                    

                    // Add in the parameters
                    m_command = DatabaseBase.CreateCommand(sql);
                    
                }
                return m_command;

            }

        }


        DbCommand m_command = null;
        string m_entityName;
        string m_tableName;
        int m_databaseId;
    }
}
