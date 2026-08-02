using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    internal class InsertSqlParameter
    {
        internal InsertSqlParameter(string name, System.Data.SqlDbType type, object value)
        {
            m_name = name;
            m_type = type;
            m_value = value;
            m_size = null;
        }

        internal InsertSqlParameter(string name, System.Data.SqlDbType type, int size, object value)
        {
            m_name = name;
            m_type = type;
            m_value = value;
            m_size = size;
        }

        public string Name { get { return m_name; } }
        public System.Data.SqlDbType Type { get { return m_type; } }
        public object Value { get { return m_value; } }
        public int? Size { get { return m_size; } }


        string m_name;
        System.Data.SqlDbType m_type;
        object m_value;
        int? m_size;
    }
}
