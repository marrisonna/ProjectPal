using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Utils;

namespace DBProjectPal
{
    public abstract class DBObject<T> : DBObjectBase where T : DBObject<T>, new() 
    {
      
        public delegate T NewInstance();
        
        // Public static

        static public T GetInstanceFromDatabaseId(decimal theId)
        {
            if (s_allInstances == null)
            {
                LoadAllInstances();

            }

            T foundInstance = null;
            s_allInstances.TryGetValue(theId, out foundInstance);
            return foundInstance;
        }

       

        static public void SetNewDelegate(NewInstance theDelegateFunction)
        {
            s_newInstanceDelegateFunction = theDelegateFunction;
        }

        static public IEnumerable<T> AllInstances
        {
            get
            {
                if (s_allInstances == null)
                {
                    LoadAllInstances();

                }
                return s_allInstances.Values;
            }
        }
        
        // Protected


        abstract protected void CreateFromReader(SqlDataReader record);

        static protected void AddNewInstance(T theNewInstance)
        {
            theNewInstance.StatusNew();
            theNewInstance.DatabaseId = s_nextNewId--;
            s_allInstances.Add(theNewInstance.DatabaseId, theNewInstance);
        }

        
        

        // Protected Static
        static protected T CreateInstance(SqlDataReader record)
        {
            T instance = null;
            if (s_newInstanceDelegateFunction != null)
                instance = s_newInstanceDelegateFunction();
            else
                instance = new T();

            instance.CreateFromReader(record);
            instance.StatusNotModified();
            return instance;
        }

        static protected string AllInstanceQuery { get; set; }


        



        // Private static

        static private void LoadAllInstances()
        {
            new T(); // Force static constructor
            s_allInstances = new Dictionary<decimal, T>();


            SqlDataReader queryResult = ExecuteReader(AllInstanceQuery);

            while (queryResult.Read())
            {
                T taskFromDatabase = CreateInstance(queryResult);
                s_allInstances.Add(taskFromDatabase.DatabaseId, taskFromDatabase);
            }

            queryResult.Close();

        }

        private static NewInstance s_newInstanceDelegateFunction = null;
        static private Dictionary<decimal,T> s_allInstances = null;
        static private int s_nextNewId = -1;
         


       


        
        

        
        



    }
}
