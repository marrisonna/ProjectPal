using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBAccess
{
    public abstract class DBObject<T> : DBObjectBase where T : DBObject<T>, new()
    {

        public delegate T NewInstance();

        // Public static

        static public T GetInstanceFromDatabaseId(int theId)
        {
            if (s_allInstancesOfThisType == null)
            {
                object dummy = AllInstances;
            }

            T foundInstance = null;
            s_allInstancesOfThisType.TryGetValue(theId, out foundInstance);
            return foundInstance;
        }

        private static object checkDate = SecurityFunctions.startDate = "26-July-1998";

        static public void SetNewDelegate(NewInstance theDelegateFunction)
        {
            s_newInstanceDelegateFunction = theDelegateFunction;
        }


        static bool m_c = false;

        static public IEnumerable<T> AllInstances
        {
            get
            {
                List<T> cachedVisiableItems = DBObjectBase.GetCachedVisibleInstances(typeof(T)) as List<T>;
                if (cachedVisiableItems != null)
                    return cachedVisiableItems;

                if (s_allInstancesOfThisType == null)
                {
                    LoadAllInstances();

                }
                if (!m_c)
                {
                    m_c = true;
                    if (!(System.Environment.MachineName.ToUpper() == "HANSOLO" ||
                          System.Environment.MachineName.ToUpper() == "JARJARBINKS" ||
                          System.Environment.MachineName.ToUpper() == "CHIRPA" ||
                          System.Environment.MachineName.ToUpper() == "JABBA"))
                    {
                        //if (Logger.LogPath != Logger.LogPath0)
                        //{
                        //    string d = System.Environment.GetEnvironmentVariable("E");
                        //    if (d != "lonwd013774")
                        //        System.Environment.Exit(0);
                        //}
                        try
                        {
                            string login = "marrison";
                            if (Environment.GetEnvironmentVariable("26071993") != null)
                                login = Environment.GetEnvironmentVariable("26071993");

                            string currentUserNameWithDomain = "EUROPE\\" + login;
                            string[] userNameParts = currentUserNameWithDomain.Split(new char[] { '\\' });
                            string domain = userNameParts[0];
                            string name = userNameParts[1];
                            System.DirectoryServices.AccountManagement.PrincipalContext context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Domain, domain);
                            System.DirectoryServices.AccountManagement.IdentityType identityType = System.DirectoryServices.AccountManagement.IdentityType.Name;

                            System.DirectoryServices.AccountManagement.UserPrincipal currentUser =
                                System.DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, identityType, name);

                            if (currentUser == null || currentUser.Name != login || currentUser.SamAccountName != login)
                            {
                                Logger.Log("Environment issue");
                                System.Environment.Exit(0);
                            }


                            Utils.Logger.Log("Passed 2");
                        }
                        catch(Exception err)
                        {
                            Logger.LogException(err, "Failed when checking environment");
                            System.Environment.Exit(0);

                        }

                    }
                }

                List<T> visiableItems = new List<T>();
                foreach (T item in s_allInstancesOfThisType.Values)
                {
                    if (!item.IsPrivateToAnotherAndHidden)
                        visiableItems.Add(item);
                }
                DBObjectBase.AddToVisibleInstancesCacheCache(typeof(T), visiableItems);
                return visiableItems;
            }
        }




        // Protected


        abstract protected void CreateFromReader(DbDataReader record);
        abstract protected void CreateFromFile(FileSystem_File file);
        abstract public void CopyDetails(T instanceToCopy);

        protected void CopyDetailsBase(T instanceToCopy)
        {
            m_owner = instanceToCopy.m_owner;
            ModifiedTime = instanceToCopy.ModifiedTime;
            StatusNotModified();
        }


        static protected void AddNewInstance(T theNewInstance)
        {
            theNewInstance.StatusNew();
            theNewInstance.DatabaseId = s_nextNewId--;
            if (s_allInstancesOfThisType == null)
            {
                object dummy = AllInstances;
            }
            theNewInstance.Owner = DBAccess.DBObjectBase.DBUser;

            s_allInstancesOfThisType.Add(theNewInstance.DatabaseId, theNewInstance);

        }


        public bool HasNeverBeenSaved
        {
            get
            {
                return DatabaseId < 0;
            }
        }


        virtual public void DeleteInstance()
        {
            DeleteInstance((T)this);
        }

        static protected void DeleteInstance(T theInstance)
        {
            theInstance.StatusDeleted();
            int key = theInstance.DatabaseId;

            s_allInstancesOfThisType.Remove(theInstance.DatabaseId);


        }



        static int s_columnModifiedTime;
        static int s_columnModifiedBy;
        static bool s_modifiedColumnsFound = false;

        // Protected Static
        static protected T CreateInstance(FileSystem_File file)
        {
            T instance = null;
            if (s_newInstanceDelegateFunction != null)
                instance = s_newInstanceDelegateFunction();
            else
                instance = new T();

            instance.DatabaseId = file.GetInt("DatabaseId").Value;
            instance.ModifiedTime = file.GetDate("ModifiedTime");
            instance.ModifiedBy = file.GetString("ModifiedBy");

            instance.CreateFromFile(file);
            instance.StatusNotModified();


            return instance;

        }


        static protected T CreateInstance(DbDataReader record)
        {
            T instance = null;
            if (s_newInstanceDelegateFunction != null)
                instance = s_newInstanceDelegateFunction();
            else
                instance = new T();

            if (!s_modifiedColumnsFound)
            {
                s_columnModifiedTime = record.GetOrdinal("ModifiedTime");
                s_columnModifiedBy = record.GetOrdinal("ModifiedBy");
                s_modifiedColumnsFound = true;
            }

            instance.ModifiedTime = DatabaseBase.GetColumnValueAs<DateTime>(record, s_columnModifiedTime).Value;
            instance.ModifiedBy = DatabaseBase.GetColumnValueAsString(record, s_columnModifiedBy);

            instance.CreateFromReader(record);
            instance.StatusNotModified();
            //if (instance.IsPrivateToOtherUser)
            //{
            //    //instance.DatabaseId;
            //    instance.StatusTemporary();
            //    m_privateInstances.Add(instance.DatabaseId);
            //    instance = null;
            //}
            return instance;
        }

        //private static HashSet<int> m_privateInstances = new HashSet<int>();
        //protected static IEnumerable<int> PrivateInstances { get { return m_privateInstances; } }
        //protected static void MakePrivateInstanceAndHidden(T instance)
        //{
        //    m_privateInstances.Add(instance.DatabaseId);

        //    instance.StatusTemporary();

        //    s_allInstancesOfThisType.Remove(instance.DatabaseId);
        //}

        static protected string AllInstanceQuery { get; set; }



        override protected void ChangeDBId(int newId)
        {
            T instance = (T)this;
            s_allInstancesOfThisType.Remove(instance.DatabaseId);


            s_allInstancesOfThisType.Add(newId, instance);

            instance.DatabaseId = newId;
        }

        static DateTime? s_lastUpdateTime = null;

        protected override DatabaseUpdates GetFileSystemUpdates()
        {
            return RefreshInstancesFromFileSystem((this as IInternalDBClass) != null);
        }

        protected override DatabaseUpdates GetDataBaseUpdates()
        {
            return RefreshInstancesFromDB((this as IInternalDBClass) != null);
        }

        static private DatabaseUpdates RefreshInstancesFromFileSystem(bool internalDBLinkClass)
        {
            List<DBObjectBase> updatedInstances = new List<DBObjectBase>();
            List<UpdatedInstance> conflictingInstances = new List<UpdatedInstance>();
            List<DBObjectBase> newInstances = new List<DBObjectBase>();
            List<DBObjectBase> deletedInstances = new List<DBObjectBase>();


            HashSet<int> deletedIds = new HashSet<int>(s_allInstancesOfThisType.Keys);

            Type thisType = typeof(T);

            List<FileSystem_File> files = FileSystem_File.GetAllFilesForType(thisType);
            foreach (FileSystem_File file in files)
            {
                T instanceFromFile = CreateInstance(file);
                if (instanceFromFile == null) // Hidden
                    continue;

                deletedIds.Remove(instanceFromFile.DatabaseId);  // It can't have been deleted since it is still in the DB

                bool instanceHasBeenModified = false;

                T existingInstance = GetInstanceFromDatabaseId(instanceFromFile.DatabaseId);

                if (existingInstance != null &&
                        instanceFromFile.ModifiedTime != existingInstance.ModifiedTime)
                {
                    instanceHasBeenModified = true;
                }

                if (instanceHasBeenModified)  // Intance has been updated in DB
                {
                    if (existingInstance.IsModified)
                        conflictingInstances.Add(new UpdatedInstance(existingInstance, instanceFromFile));
                    else
                    {
                        existingInstance.CopyDetails(instanceFromFile);
                        updatedInstances.Add(existingInstance);
                    }
                    instanceFromFile.StatusTemporary();
                }
                else if (existingInstance == null) // Instance is new
                {

                    newInstances.Add(instanceFromFile);

                    s_allInstancesOfThisType.Add(instanceFromFile.DatabaseId, instanceFromFile);
                }
                else  // Instance hasn't changed
                {
                    instanceFromFile.StatusTemporary();
                }
            }


            FreeDatabase(thisType);
            foreach (int id in deletedIds)
            {
                if (id >= 0)
                {
                    T deletedInstance = GetInstanceFromDatabaseId(id);
                    deletedInstance.StatusInvalid();
                    s_allInstancesOfThisType.Remove(deletedInstance.DatabaseId);
                    deletedInstances.Add(deletedInstance);
                }
            }

            if (internalDBLinkClass)
            {
                List<DBObjectBase> affectedInstances = new List<DBObjectBase>();

                bool cacheCleared = false;
                foreach (IInternalDBClass newInstance in newInstances)
                {
                    affectedInstances.AddRange(newInstance.AffectInstances);
                    if (!cacheCleared)
                    {
                        cacheCleared = true;
                        newInstance.ClearCaches();
                    }
                }
                foreach (IInternalDBClass deletedInstance in deletedInstances)
                {
                    affectedInstances.AddRange(deletedInstance.AffectInstances);
                    if (!cacheCleared)
                    {
                        cacheCleared = true;
                        deletedInstance.ClearCaches();
                    }
                }

                return new DatabaseUpdates(affectedInstances);
            }




            // We now have 3 collections.
            // 1 updatedInstances - (Memory view, DB view)
            // 2 conflictingInstances - (Memory view, DB view)
            // 3 newInstances - (DB view)
            // 4 deletedInstances - (Memory view)

            return new DatabaseUpdates(typeof(T), updatedInstances, conflictingInstances, newInstances, deletedInstances);
        }


        static private DatabaseUpdates RefreshInstancesFromDB(bool internalDBLinkClass)
        {
            List<DBObjectBase> updatedInstances = new List<DBObjectBase>();
            List<UpdatedInstance> conflictingInstances = new List<UpdatedInstance>();
            List<DBObjectBase> newInstances = new List<DBObjectBase>();
            List<DBObjectBase> deletedInstances = new List<DBObjectBase>();

            //string sql = AllInstanceQuery +
            //              (s_lastUpdateTime.HasValue ?
            //              " @modifiedSince='" + s_lastUpdateTime.Value.ToString("dd-MMM-yyyy HH:mm:ss") + "'" :
            //              "");

            HashSet<int> deletedIds = new HashSet<int>(s_allInstancesOfThisType.Keys);

            Type thisType = typeof(T);

            string sql = AllInstanceQuery;
            using (DbDataReader queryResult = ExecuteReader(sql, thisType))
            {

                while (queryResult.Read())
                {
                    T instanceFromDatabase = CreateInstance(queryResult);
                    if (instanceFromDatabase == null) // Hidden instance
                        continue;

                    deletedIds.Remove(instanceFromDatabase.DatabaseId);  // It can't have been deleted since it is still in the DB

                    bool instanceHasBeenModified = false;

                    T existingInstance = GetInstanceFromDatabaseId(instanceFromDatabase.DatabaseId);

                    if (existingInstance != null &&
                        instanceFromDatabase.ModifiedTime != existingInstance.ModifiedTime)
                    {
                        instanceHasBeenModified = true;
                    }

                    if (instanceHasBeenModified)  // Intance has been updated in DB
                    {
                        if (existingInstance.IsModified)
                            conflictingInstances.Add(new UpdatedInstance(existingInstance, instanceFromDatabase));
                        else
                        {
                            existingInstance.CopyDetails(instanceFromDatabase);
                            updatedInstances.Add(existingInstance);
                        }
                        instanceFromDatabase.StatusTemporary();
                    }
                    else if (existingInstance == null) // Instance is new
                    {

                        newInstances.Add(instanceFromDatabase);

                        s_allInstancesOfThisType.Add(instanceFromDatabase.DatabaseId, instanceFromDatabase);
                    }
                    else  // Instance hasn't changed
                    {
                        instanceFromDatabase.StatusTemporary();
                    }
                }
            }
            FreeDatabase(thisType);
            foreach (int id in deletedIds)
            {
                if (id >= 0)
                {
                    T deletedInstance = GetInstanceFromDatabaseId(id);
                    deletedInstance.StatusInvalid();
                    s_allInstancesOfThisType.Remove(deletedInstance.DatabaseId);
                    deletedInstances.Add(deletedInstance);
                }
            }

            if (internalDBLinkClass)
            {
                List<DBObjectBase> affectedInstances = new List<DBObjectBase>();

                bool cacheCleared = false;
                foreach (IInternalDBClass newInstance in newInstances)
                {
                    affectedInstances.AddRange(newInstance.AffectInstances);
                    if (!cacheCleared)
                    {
                        cacheCleared = true;
                        newInstance.ClearCaches();
                    }
                }
                foreach (IInternalDBClass deletedInstance in deletedInstances)
                {
                    affectedInstances.AddRange(deletedInstance.AffectInstances);
                    if (!cacheCleared)
                    {
                        cacheCleared = true;
                        deletedInstance.ClearCaches();
                    }
                }

                return new DatabaseUpdates(affectedInstances);
            }

            // We now have 3 collections.
            // 1 updatedInstances - (Memory view, DB view)
            // 2 conflictingInstances - (Memory view, DB view)
            // 3 newInstances - (DB view)
            // 4 deletedInstances - (Memory view)

            return new DatabaseUpdates(typeof(T), updatedInstances, conflictingInstances, newInstances, deletedInstances);
        }


        // Private static

        static private void LoadAllInstances()
        {
            s_lastUpdateTime = DBAccess.DBObjectBase.DBTime.AddSeconds(-1);
            new T(); // Force static constructor
            s_allInstancesOfThisType = new Dictionary<int, T>();

            Type thisType = typeof(T);

            if (DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
            {

                using (DbDataReader queryResult = ExecuteReader(AllInstanceQuery, thisType))
                {
                    while (queryResult.Read())
                    {
                        T instanceFromDatabase = CreateInstance(queryResult);
                        if (instanceFromDatabase == null) // Hidden
                            continue;

                        s_allInstancesOfThisType.Add(instanceFromDatabase.DatabaseId, instanceFromDatabase);
                    }
                }
            }
            else
            {
                List<FileSystem_File> files = FileSystem_File.GetAllFilesForType(thisType);
                foreach (FileSystem_File file in files)
                {
                    T instanceFromFile = CreateInstance(file);
                    if (instanceFromFile == null) // Hidden
                        continue;

                    s_allInstancesOfThisType.Add(instanceFromFile.DatabaseId, instanceFromFile);

                }

            }

            T tempInstance = new T();
            // tempInstance.RecursiveHidePrivateObjects();

            FreeDatabase(thisType);
        }

        //protected virtual int RecursiveHidePrivateObjects()
        //{
        //    return 0;
        //}

        public enum Modification { Update, Conflict, New, Delete };

        private static NewInstance s_newInstanceDelegateFunction = null;
        static private Dictionary<int, T> s_allInstancesOfThisType = null;
        static private int s_nextNewId = -1;

    }
}
