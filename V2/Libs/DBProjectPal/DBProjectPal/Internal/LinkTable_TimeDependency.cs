using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBProjectPal.Internal
{
    internal class LinkTable_TimeDependency : DBAccess.DBObject<LinkTable_TimeDependency>, DBAccess.IInternalDBClass
    {

        public void ClearCaches()
        { }


        static public void AddNewInstance(ITaskOrProject preItem, ITaskOrProject postItem)
        {
            new LinkTable_TimeDependency(preItem, postItem);
            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_TimeDependency));
        }

        static public void RemoveLink(ITaskOrProject preItem, ITaskOrProject postItem)
        {
            foreach (LinkTable_TimeDependency dependency in AllInstances)
            {
                if (dependency.m_preItem == preItem && dependency.m_postItem == postItem)
                    dependency.DeleteInstance();
            }

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_TimeDependency));
        }


        static public void DeleteInstance(ITaskOrProject theItem)
        {
            List<LinkTable_TimeDependency> itemsToDelete = new List<LinkTable_TimeDependency>();

            foreach (LinkTable_TimeDependency dependency in AllInstances)
            {
                if (dependency.m_preItem == theItem || dependency.m_postItem == theItem)
                {
                    dependency.DeleteInstance();
                }
            }

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_TimeDependency));
        }

        static public bool AlreadyDirectlyLinked(ITaskOrProject preItem, ITaskOrProject postItem)
        {
            return ImmediatePostDependants(preItem).Contains(postItem);
        }


        static public bool AlreadyLinked(ITaskOrProject preItem, ITaskOrProject postItem)
        {
            return AllPostItems(preItem).Contains(postItem);
        }

        public override string ObjectDescription { get { return "LinkTable_TimeDependency:" + this.DatabaseId; } }


        static private HashSet<ITaskOrProject> AllPostItems(ITaskOrProject testItem)
        {
            List<ITaskOrProject> testItems = new List<ITaskOrProject>();
            testItems.Add(testItem);
            if (testItem is Project)
            {
                testItems.AddRange((testItem as Project).AllActiveTasks);
                testItems.AddRange((testItem as Project).AllActiveSubProjects);
            }

            HashSet<ITaskOrProject> dependencies = new HashSet<ITaskOrProject>();
            foreach (ITaskOrProject itemToTest in testItems)
            {
                foreach (ITaskOrProject dependantItem in LinkTable_TimeDependency.AllDependandItems(itemToTest))
                {
                    if (!dependencies.Contains(dependantItem))
                        dependencies.Add(dependantItem);
                }
            }
            return dependencies;
        }



        static public HashSet<ITaskOrProject> AllDependandItems(ITaskOrProject itemToCheck)
        {
            HashSet<ITaskOrProject> result = new HashSet<ITaskOrProject>();

            HashSet<ITaskOrProject> itemsCheck = new HashSet<ITaskOrProject>();
            List<ITaskOrProject> itemsToCheck = new List<ITaskOrProject>() { itemToCheck };

            while (itemsToCheck.Count > 0)
            {
                ITaskOrProject nextItemToCheck = itemsToCheck[0];

                itemsCheck.Add(nextItemToCheck);
                itemsToCheck.RemoveAt(0);

                foreach (ITaskOrProject dependantItem in ImmediatePostDependants(nextItemToCheck))
                {
                    if (!result.Contains(dependantItem))
                    {
                        result.Add(dependantItem);
                        itemsToCheck.Add(dependantItem);
                    }
                }
            }
            return result;
        }


        static public List<ITaskOrProject> ImmediatePostDependants(ITaskOrProject itemToCheck)
        {
            List<ITaskOrProject> result = new List<ITaskOrProject>();
            foreach (LinkTable_TimeDependency dependency in AllInstances)
            {
                if (dependency.m_preItem == itemToCheck && dependency.m_postItem != null && dependency.m_postItem.IsActive)
                    result.Add(dependency.m_postItem);
            }
            return result;
        }

        static public List<ITaskOrProject> ImmediatePreDependencies(ITaskOrProject itemToCheck)
        {
            List<ITaskOrProject> result = new List<ITaskOrProject>();
            foreach (LinkTable_TimeDependency dependency in AllInstances)
            {
                if (dependency.m_postItem == itemToCheck && dependency.m_preItem != null && dependency.m_preItem.IsActive)
                    result.Add(dependency.m_preItem);
            }
            return result;
        }

      

        private ITaskOrProject m_preItem;
        private ITaskOrProject m_postItem;


        public List<DBAccess.DBObjectBase> AffectInstances
        {
            get
            {
                List<DBAccess.DBObjectBase> affectedInstances = new List<DBAccess.DBObjectBase>();
                if (m_preItem != null)
                    affectedInstances.Add((DBAccess.DBObjectBase)m_preItem);
                if (m_postItem != null)
                    affectedInstances.Add((DBAccess.DBObjectBase)m_postItem);

                return affectedInstances;
            }
        }


        override public void CopyDetails(LinkTable_TimeDependency instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            Logger.Log("Attempt to Merge 'LinkTable_TimeDependency' with id = " + DatabaseId);


            StatusNotModified();


            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_TimeDependency));
        }

        //////////// Private

        private LinkTable_TimeDependency(ITaskOrProject preItem, ITaskOrProject postItem)
        {
            AddNewInstance(this);
            m_preItem = preItem;
            m_postItem = postItem;
        }

        //////// Database access

        static LinkTable_TimeDependency()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "TimeDependency";
        }

        public LinkTable_TimeDependency()
        { }

        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {
            int? preTaskId = file.GetInt("preTaskId");
            if (preTaskId.HasValue)
                m_preItem = Task.GetInstanceFromDatabaseId(preTaskId.Value);

            int? postTaskId = file.GetInt("postTaskId");
            if (postTaskId.HasValue)
                m_postItem = Task.GetInstanceFromDatabaseId(postTaskId.Value);

            int? preProjectId = file.GetInt("preProjectId");
            if (preProjectId.HasValue)
                m_preItem = Project.GetInstanceFromDatabaseId(preProjectId.Value);

            int? postProjectId = file.GetInt("postProjectId"); ;
            if (postProjectId.HasValue)
                m_postItem = Project.GetInstanceFromDatabaseId(postProjectId.Value);
          
        }

        protected override void CreateFromReader(DbDataReader timeDepenecyRecord)
        {

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(timeDepenecyRecord);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(timeDepenecyRecord, s_columnTimeDependencyId).Value;

            int? preTaskId = DatabaseBase.GetColumnValueAs<int>(timeDepenecyRecord, s_columnPreTaskId);
            if (preTaskId.HasValue)
                m_preItem = Task.GetInstanceFromDatabaseId(preTaskId.Value);

            int? postTaskId = DatabaseBase.GetColumnValueAs<int>(timeDepenecyRecord, s_columnPostTaskId);
            if (postTaskId.HasValue)
                m_postItem = Task.GetInstanceFromDatabaseId(postTaskId.Value);

            int? preProjectId = DatabaseBase.GetColumnValueAs<int>(timeDepenecyRecord, s_columnPreProjectId);
            if (preProjectId.HasValue)
                m_preItem = Project.GetInstanceFromDatabaseId(preProjectId.Value);

            int? postProjectId = DatabaseBase.GetColumnValueAs<int>(timeDepenecyRecord, s_columnPostProjectId);
            if (postProjectId.HasValue)
                m_postItem = Project.GetInstanceFromDatabaseId(postProjectId.Value);

        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("preTaskId",(int?)( m_preItem is Task ? GetDatabaseIdForWritingToDatabase(m_preItem as Task) : null));
            result.Add("postTaskId", (int?)(m_postItem is Task ? GetDatabaseIdForWritingToDatabase(m_postItem as Task) : null));
            result.Add("preProjectId", (int?)(m_preItem is Project ? GetDatabaseIdForWritingToDatabase(m_preItem as Project) : null));
            result.Add("postProjectId", (int?)(m_postItem is Project ? GetDatabaseIdForWritingToDatabase(m_postItem as Project) : null));
            
            return result;

        }

        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("TimeDependency", DatabaseId);
            sqlForInstert.AddParameter("preTaskId", System.Data.SqlDbType.Int,
                m_preItem is Task ? GetDatabaseIdForWritingToDatabase(m_preItem as Task) : null);

            sqlForInstert.AddParameter("postTaskId", System.Data.SqlDbType.Int,
                m_postItem is Task ? GetDatabaseIdForWritingToDatabase(m_postItem as Task) : null);

            sqlForInstert.AddParameter("preProjectId", System.Data.SqlDbType.Int,
                m_preItem is Project ? GetDatabaseIdForWritingToDatabase(m_preItem as Project) : null);

            sqlForInstert.AddParameter("postProjectId", System.Data.SqlDbType.Int,
                m_postItem is Project ? GetDatabaseIdForWritingToDatabase(m_postItem as Project) : null);

            return sqlForInstert.Command;
        }

        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("TimeDependency", DatabaseId);
            return deleteSql.Command;
        }






        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnTimeDependencyId = tempateRecord.GetOrdinal("TimeDependencyId");
            s_columnPreTaskId = tempateRecord.GetOrdinal("PreTaskId");
            s_columnPreProjectId = tempateRecord.GetOrdinal("PreProjectId");
            s_columnPostTaskId = tempateRecord.GetOrdinal("PostTaskId");
            s_columnPostProjectId = tempateRecord.GetOrdinal("PostProjectId");

        }

        static bool s_setupHasBeenDone = false;

        static int s_columnTimeDependencyId;
        static int s_columnPreTaskId;
        static int s_columnPreProjectId;
        static int s_columnPostTaskId;
        static int s_columnPostProjectId;

    }
}
