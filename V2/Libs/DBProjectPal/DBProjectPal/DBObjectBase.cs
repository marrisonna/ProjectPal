using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Utils;

namespace DBProjectPal
{
    public abstract class DBObjectBase
    {

        public DBObjectBase()
        {
            s_allInstances.Add(this);
            m_dataStatus = RowStatusValue.Invalid;
        }

        // Public 
        public enum RowStatusValue { NotModified, Modified, New, Deleted, Invalid };

        public decimal DatabaseId { get; protected set; }

        static private List<DBObjectBase> s_allInstances = new List<DBObjectBase>();

        // Private

        private RowStatusValue m_dataStatus;

        protected void StatusModified()
        {
            m_dataStatus = RowStatusValue.Modified;
        }

        protected void StatusNotModified()
        {
            m_dataStatus = RowStatusValue.NotModified;
        }

        protected void StatusNew()
        {
            m_dataStatus = RowStatusValue.New;
        }

        //static private Database s_theDatabase = new Database("ProjectPal");

        static protected SqlDataReader ExecuteReader(string querySQL)
        {
            Database theDatabase = new Database("ProjectPalDB_1");
            return theDatabase.ExecuteReader(querySQL);
        }

        static protected SqlDataReader ExecuteReader(SqlCommand querySQL)
        {
            Database theDatabase = new Database("ProjectPalDB_1");
            return theDatabase.ExecuteReader(querySQL);
        }



        /// <summary>
        /// Returns a SQL to execute that will save the instance and
        /// return a Reader with 1 column of int which is the Id
        /// </summary>
        /// <returns></returns>
        //abstract protected string SaveToDataBaseSQLString();
        abstract protected SqlCommand SaveToDataBaseSQLCommand();



        static public void Save()
        {

            List<DBObjectBase> newInstances = new List<DBObjectBase>();
            foreach (DBObjectBase currentInstance in s_allInstances)
            {
                if (currentInstance.m_dataStatus == RowStatusValue.New)
                {
                    newInstances.Add(currentInstance);
                    //SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLString());
                    SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLCommand());
                    if (result.Read())
                    {
                        currentInstance.DatabaseId = Database.GetColumnValueAs<decimal>(result, 0).Value;
                        currentInstance.m_dataStatus = RowStatusValue.NotModified;
                    }
                    result.Close();
                }
            }

            // Save new instance twice so the DB is sure to be set
            foreach (DBObjectBase currentInstance in newInstances)
            {
                SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLCommand());
                if (result.Read())
                {
                    currentInstance.DatabaseId = Database.GetColumnValueAs<decimal>(result, 0).Value;
                    currentInstance.m_dataStatus = RowStatusValue.NotModified;
                }
                result.Close();

            }




            foreach (DBObjectBase currentInstance in s_allInstances)
            {
                if (currentInstance.m_dataStatus == RowStatusValue.Modified)
                {
                    //SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLString());
                    SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLCommand());
                    if (result.Read())
                    {
                        currentInstance.DatabaseId = Database.GetColumnValueAs<decimal>(result, 0).Value;
                        currentInstance.m_dataStatus = RowStatusValue.NotModified;
                    }
                    result.Close();
                }
            }
        }



    }
}
