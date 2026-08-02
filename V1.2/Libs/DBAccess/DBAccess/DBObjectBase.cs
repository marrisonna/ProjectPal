using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBAccess
{
    public abstract class DBObjectBase
    {

        public static bool SuperUserMayViewPrivateItems
        {
            set
            {
                m_superUserMayViewPrivateItems = value;
            }
        }
        protected static bool m_superUserMayViewPrivateItems = false;

        static private Dictionary<Type, DBObjectBase> s_typeExamples = new Dictionary<Type, DBObjectBase>();
        static private Dictionary<Type, object> s_visibleInstancesCacheCache = new Dictionary<Type, object>();

        static protected object GetCachedVisibleInstances(Type objectType)
        {
            object result;
            if (s_visibleInstancesCacheCache.TryGetValue(objectType, out result))
                return result;
            return null;
        }
        static protected void AddToVisibleInstancesCacheCache(Type objectType, object allInstanceForType)
        {
            //if(m_visibleInstanceCacheIsEnabled)
            s_visibleInstancesCacheCache[objectType] = allInstanceForType;
        }

        static public void ClearVisibleInstanceCache()
        {
            s_visibleInstancesCacheCache = new Dictionary<Type, object>();
        }

        static public void RemoveTypeFromVisibleInstanceCache(Type objectType)
        {
            s_visibleInstancesCacheCache.Remove(objectType);
        }

        //static public void EnableVisibleInstanceCache(bool value)
        //{
        //    m_visibleInstanceCacheIsEnabled = value;
        //    if (!m_visibleInstanceCacheIsEnabled)
        //        ClearVisibleInstanceCache();
        //}

        //static private bool m_visibleInstanceCacheIsEnabled = false;


        public DBObjectBase()
        {
            s_allInstancesGlobal.Add(this);
            m_dataStatus = RowStatusValue.Invalid;

            Type instanceType = this.GetType();
            if (!s_typeExamples.ContainsKey(instanceType))
            {
                s_typeExamples.Add(instanceType, this);
            }

        }




        // Public 
        public enum RowStatusValue { NotModified, Modified, New, Deleted, Invalid, Temporary };

        private int m_databaseId = -1;
        private static int s_maxDatabaseId = -1;

        internal static int MaxDatabaseId { get { return s_maxDatabaseId; } }

        protected internal int DatabaseId
        {
            get { return m_databaseId; }
            set { s_maxDatabaseId = Math.Max(m_databaseId = value, s_maxDatabaseId); }
        }
        public DateTime? ModifiedTime { get; protected set; }
        public string ModifiedBy { get; protected set; }
        public string Owner
        {
            get
            {
                if (m_owner == "dbo")
                    return "marrison";
                return m_owner == null ? null : m_owner.ToLower();
            }
            set { m_owner = value; StatusModified(); }
        }
        public bool NeverSave { get { return m_neverSave; } set { m_neverSave = value; } }
        private bool m_neverSave = false;

        protected string m_owner;

        public void MergeBase(DBObjectBase instanceToMerge, bool allValuseAreDBValues)
        {
            m_owner = instanceToMerge.m_owner;
            ModifiedTime = instanceToMerge.ModifiedTime;
            if (allValuseAreDBValues)
                StatusNotModified();
            else
                StatusModified();
        }



        //public int GetDatabaseIdForWritingToDatabase { get { return DatabaseId; } }

        // This is a long ugly name to try and stop it being used for other purposes.
        public static object GetDatabaseIdForWritingToDatabase(DBObjectBase item)
        {
            if (item == null)
            {
                if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                    return DBNull.Value;
                return null;
            }
            return item.DatabaseId;
        }

        static private List<DBObjectBase> s_allInstancesGlobal = new List<DBObjectBase>();

        // Private

        private RowStatusValue m_dataStatus;

        protected void StatusModified()
        {
            if (m_dataStatus != RowStatusValue.New)
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

        protected void StatusDeleted()
        {
            m_dataStatus = RowStatusValue.Deleted;
        }

        virtual public void OnStatusTemporary()
        { }

        protected void StatusTemporary()
        {
            m_dataStatus = RowStatusValue.Temporary;
            s_allInstancesGlobal.Remove(this);
            this.OnStatusTemporary();
        }

        protected void StatusInvalid()
        {
            m_dataStatus = RowStatusValue.Invalid;
            s_allInstancesGlobal.Remove(this);
        }



        protected bool IsModified
        {
            get
            {
                return m_dataStatus == RowStatusValue.Modified;
            }
        }

        public bool IsDeleted
        {
            get
            {
                return m_dataStatus == RowStatusValue.Invalid ||
                    m_dataStatus == RowStatusValue.Deleted;
            }
        }


        //static private Database s_theDatabase = new Database("ProjectPal");

        static protected DbDataReader ExecuteReader(string querySQL)
        {
            DatabaseBase theDatabase = DatabaseBase.NamedInstance(null);
            return theDatabase.ExecuteReader(querySQL);

        }

        static protected DbDataReader ExecuteReader(string querySQL, object connectionId)
        {
            if (connectionId == null)
                return ExecuteReader(querySQL);

            return DatabaseBase.NamedInstance(connectionId).ExecuteReader(querySQL);
        }

        static protected DbDataReader ExecuteReader(DbCommand querySQL, object connectionId)
        {
            if (connectionId == null)
                return ExecuteReader(querySQL);

            return DatabaseBase.NamedInstance(connectionId).ExecuteReader(querySQL);
        }


        static protected void FreeDatabase(object connectionId)
        {
            DatabaseBase.FreeNamedInstance(connectionId);
        }

        static public DateTime DBTime
        {
            get
            {
                DatabaseBase theDatabase = DatabaseBase.NamedInstance(null);
                DbDataReader queryResult = theDatabase.ExecuteReader("select GetDate()");
                queryResult.Read();
                DateTime result = DatabaseBase.GetColumnValueAs<DateTime>(queryResult, 0).Value;
                return result;
            }
        }

        private static string s_dbUser = null;
        static public string DBUser
        {
            get
            {
                if (s_dbUser == null)
                {
                    s_dbUser = DatabaseBase.CurrentUser;

                    if (TestingOverrides.DBName != null)
                        s_dbUser = TestingOverrides.DBName;

                    s_dbUser = s_dbUser == null ? null : s_dbUser.ToLower();
                }
                return s_dbUser;
            }
        }

        public static Utils.Permissions.UserLevel? ThisUserType
        {
            get
            {
                Utils.Permissions.UserLevel? thisUserType = null;


                if (DBAccess.TestingOverrides.UserLevel.HasValue)
                {
                    thisUserType = (Permissions.UserLevel)Enum.Parse(typeof(Permissions.UserLevel), DBAccess.TestingOverrides.UserLevel.ToString());
                }
                else
                {
                    string userName = DBAccess.DBObjectBase.DBUser;
                    if (userName == "Neil" ||
                        userName.ToLower().Contains("marrison"))
                    {
                        thisUserType = Permissions.UserLevel.SuperUser;
                    }
                }

                return thisUserType;
            }
        }


        static protected DbDataReader ExecuteReader(DbCommand querySQL)
        {
            DatabaseBase theDatabase = DatabaseBase.NamedInstance(null);
            return theDatabase.ExecuteReader(querySQL);
        }

        static protected DbDataReader ExecuteReaderSharedConnection(DbCommand querySQL)
        {
            return DatabaseBase.NamedInstance(typeof(DBObjectBase)).ExecuteReader(querySQL);
        }




        static protected void ExecuteNonQuery(DbCommand querySQL)
        {
            DatabaseBase.NamedInstance(typeof(DBObjectBase)).ExecuteNonQuery(querySQL);
        }

        static protected void ExecuteNonQuery(DbCommand querySQL, object connectionId)
        {

            DatabaseBase.NamedInstance(typeof(DBObjectBase)).ExecuteNonQuery(querySQL);

        }

        static protected void ExecuteNonQuery(string querySQL, object connectionId)
        {

            DatabaseBase.NamedInstance(typeof(DBObjectBase)).ExecuteNonQuery(querySQL);

        }



        /// <summary>
        /// Returns a SQL to execute that will save the instance and
        /// return a Reader with 1 column of int which is the Id
        /// </summary>
        /// <returns></returns>
        //abstract protected string SaveToDataBaseSQLString();
        abstract protected DbCommand SaveToDataBaseSQLCommand();
        abstract protected DbCommand DeleteFromDataBaseSQLCommand();
        abstract public string ObjectDescription { get; }
        abstract protected FileSystem_File FileSystemSaveObject();


        // Implemented in DBObject<T>
        abstract protected void ChangeDBId(int newId);

        static public bool HasInstances { get { return s_allInstancesGlobal.Count > 0; } }

        static public bool HasUnsavedChanges
        {
            get
            {
                bool unsavedChangedFound = false;
                foreach (DBObjectBase currentInstance in s_allInstancesGlobal)
                {
                    if (currentInstance.m_dataStatus != RowStatusValue.NotModified &&
                        currentInstance.m_dataStatus != RowStatusValue.Invalid &&
                        !(currentInstance.m_dataStatus == RowStatusValue.Deleted && currentInstance.DatabaseId < 0))
                    {
                        unsavedChangedFound = true;
                        break;
                    }
                }
                return unsavedChangedFound;
            }
        }

        static public void Save(DBObjectBase objectToSave)
        {
            List<DBObjectBase> instanceToSave = new List<DBObjectBase>();
            instanceToSave.Add(objectToSave);
            Save(instanceToSave);
        }

        static public void CopyToNewDB(DatabaseBase.DBTypeValues databaseType)
        {

            DatabaseBase.DBTypeValues currentDBType = DatabaseBase.DBType;

            if (databaseType == currentDBType)
            {
                Logger.Log(string.Format("ERROR: Cannot copy from '{0}' to '{1}'", currentDBType, databaseType));
                return;
            }
            try
            {
                DatabaseBase.DBType = databaseType;
                DateTime startOfSaveTime = DateTime.Now;
                if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.FileSystem)
                {
                    FileSystem_File.ClearFolder();
                }
                Utils.SqlInsert.CopyMode = true;

                string connectionID = "copyDB";
                foreach (DBObjectBase currentInstance in s_allInstancesGlobal)
                {
                    if (currentInstance.NeverSave)
                        continue;

                    if (currentInstance.m_dataStatus == RowStatusValue.New ||
                        currentInstance.m_dataStatus == RowStatusValue.Modified ||
                        currentInstance.m_dataStatus == RowStatusValue.NotModified)
                    {
                        if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                        {
                            string currentType = currentInstance.GetType().Name;
                            ExecuteNonQuery("set identity_insert " + DatabaseBase.Schema + currentType + " on", connectionID);

                            ExecuteNonQuery(currentInstance.SaveToDataBaseSQLCommand(), connectionID);


                            ExecuteNonQuery("set identity_insert " + DatabaseBase.Schema + currentType + " off", connectionID);
                        }
                        else
                        {
                            FileSystem_File fileRepresentation = currentInstance.FileSystemSaveObject();
                            fileRepresentation.Save(true);

                        }
                    }
                }
                FreeDatabase(connectionID);

                if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.FileSystem)
                {
                    FileSystem_File.UpdateDirectoryTimeStamp(startOfSaveTime);
                }
            }
            catch (Exception err)
            {
                Utils.Logger.LogException(err, "Error copy to new database");
            }
            finally
            {
                DatabaseBase.DBType = currentDBType;
                Utils.SqlInsert.CopyMode = false;
            }
        }


        static public void MarkAll_NotModified_as_Modified()
        {
            foreach (DBObjectBase currentInstance in s_allInstancesGlobal)
            {
                if (currentInstance.m_dataStatus == RowStatusValue.NotModified)
                    currentInstance.m_dataStatus = RowStatusValue.Modified;
            }
        }

        static public void Save()
        {
            Save(s_allInstancesGlobal);
        }

        static private void Save(List<DBObjectBase> instancesToSave)
        {
            bool modified = false;
            DateTime startOfSaveTime = DateTime.Now;
            List<DBObjectBase> newInstances = new List<DBObjectBase>();
            foreach (DBObjectBase currentInstance in instancesToSave)
            {
                if (currentInstance.m_dataStatus == RowStatusValue.New)
                {
                    modified = true;
                    newInstances.Add(currentInstance);

                    if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                    {
                        //SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLString());
                        using (DbDataReader result = ExecuteReaderSharedConnection(currentInstance.SaveToDataBaseSQLCommand()))
                        {
                            if (result.Read())
                            {
                                currentInstance.ModifiedTime = DatabaseBase.GetColumnValueAs<DateTime>(result, 1).Value;
                                int newDBID = DatabaseBase.GetColumnValueAs<int>(result, 0).Value;
                                if (currentInstance.DatabaseId != newDBID)
                                    currentInstance.ChangeDBId(newDBID);
                            }
                            result.Close();
                        }
                    }
                    else
                    {
                        FileSystem_File fileRepresentation = currentInstance.FileSystemSaveObject();
                        fileRepresentation.Save();
                        currentInstance.ModifiedTime = fileRepresentation.ModifiedTime;
                        currentInstance.ModifiedBy = fileRepresentation.ModifiedBy;
                        if (currentInstance.DatabaseId != fileRepresentation.DatabaseId)
                            currentInstance.ChangeDBId(fileRepresentation.DatabaseId);
                        currentInstance.m_dataStatus = RowStatusValue.NotModified;
                    }

                    currentInstance.m_dataStatus = RowStatusValue.NotModified;
                }
            }

            // Save new instance twice so the DB is sure to be set in references
            foreach (DBObjectBase currentInstance in newInstances)
            {
                if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                {
                    DbDataReader result = ExecuteReaderSharedConnection(currentInstance.SaveToDataBaseSQLCommand());
                    if (result.Read())
                    {
                        currentInstance.ModifiedTime = DatabaseBase.GetColumnValueAs<DateTime>(result, 1).Value;
                        currentInstance.DatabaseId = DatabaseBase.GetColumnValueAs<int>(result, 0).Value;
                    }
                    result.Close();
                }
                else
                {
                    FileSystem_File fileRepresentation = currentInstance.FileSystemSaveObject();
                    fileRepresentation.Save(true);
                }

                currentInstance.m_dataStatus = RowStatusValue.NotModified;

            }

            foreach (DBObjectBase currentInstance in instancesToSave)
            {
                if (currentInstance.m_dataStatus == RowStatusValue.Modified)
                {
                    modified = true;
                    if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                    {
                        //SqlDataReader result = ExecuteReader(currentInstance.SaveToDataBaseSQLString());
                        DbDataReader result = ExecuteReaderSharedConnection(currentInstance.SaveToDataBaseSQLCommand());
                        if (result.Read())
                        {
                            currentInstance.ModifiedTime = DatabaseBase.GetColumnValueAs<DateTime>(result, 1).Value;

                            int newDBID = DatabaseBase.GetColumnValueAs<int>(result, 0).Value;
                            if (currentInstance.DatabaseId != newDBID)
                                currentInstance.ChangeDBId(newDBID);
                        }
                        result.Close();
                    }
                    else
                    {
                        FileSystem_File fileRepresentation = currentInstance.FileSystemSaveObject();
                        fileRepresentation.Save();
                        currentInstance.ModifiedTime = fileRepresentation.ModifiedTime;
                        currentInstance.ModifiedBy = fileRepresentation.ModifiedBy;
                        if (currentInstance.DatabaseId != fileRepresentation.DatabaseId)
                            currentInstance.ChangeDBId(fileRepresentation.DatabaseId);
                        currentInstance.m_dataStatus = RowStatusValue.NotModified;
                    }

                    currentInstance.m_dataStatus = RowStatusValue.NotModified;
                }
            }
            List<DBObjectBase> deletedInstances = new List<DBObjectBase>();
            foreach (DBObjectBase currentInstance in instancesToSave)
            {
                if (currentInstance.m_dataStatus == RowStatusValue.Deleted)
                {
                    modified = true;
                    deletedInstances.Add(currentInstance);

                    if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                    {
                        DbDataReader result = ExecuteReaderSharedConnection(currentInstance.DeleteFromDataBaseSQLCommand());
                        result.Close();
                    }
                    else
                    {
                        FileSystem_File fileRepresentation = currentInstance.FileSystemSaveObject();
                        fileRepresentation.Delete();
                    }
                }
            }
            foreach (DBObjectBase deletedInstance in deletedInstances)
            {
                deletedInstance.StatusInvalid();
            }
            if (modified && DatabaseBase.DBType == DatabaseBase.DBTypeValues.FileSystem)
            {
                FileSystem_File.UpdateDirectoryTimeStamp(startOfSaveTime);
            }
        }


        static public List<DatabaseUpdates> AllFileSystemUpdates()
        {

            List<DatabaseUpdates> updates = new List<DatabaseUpdates>();
            foreach (DBObjectBase typeExample in s_typeExamples.Values)
            {
                updates.Add(typeExample.GetFileSystemUpdates());

            }

            return updates;
        }


        static public List<DatabaseUpdates> AllDataBaseUpdates()
        {

            List<DatabaseUpdates> updates = new List<DatabaseUpdates>();
            foreach (DBObjectBase typeExample in s_typeExamples.Values)
            {
                updates.Add(typeExample.GetDataBaseUpdates());

            }

            return updates;
        }

        protected abstract DatabaseUpdates GetDataBaseUpdates();
        protected abstract DatabaseUpdates GetFileSystemUpdates();

        //Override in sub class to hide an instance.
        public virtual bool IsHidden
        {
            get
            {
                return false;
            }
        }

        //Override in sub class to hide an instance.
        public virtual bool IsPrivateToAnotherAndHidden
        {
            get
            {
                return false;
            }
        }
    }
}
