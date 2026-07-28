using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBTaskMan.Internal
{
    internal class LinkTable_Task2Resource : DBAccess.DBObject<LinkTable_Task2Resource>, DBAccess.IInternalDBClass
    {

        public override string ObjectDescription { get { return "LinkTable_Task2Resource:" + this.DatabaseId; } }


        public void ClearCaches()
        {
            s_taskResourceCache = new Dictionary<Task, List<IResource>>();
        }

        private static Dictionary<Task, List<IResource>> s_taskResourceCache = new Dictionary<Task, List<IResource>>();

        static public List<IResource> ResourcesForTask(Task theTask)
        {
            List<IResource> resources;
            if (s_taskResourceCache.TryGetValue(theTask, out resources))
                return resources;

            resources = new List<IResource>();
            s_taskResourceCache.Add(theTask, resources);

            // Find People Resources

            IEnumerable<LinkTable_Task2Resource> people = from resourceLink in AllInstances
                                                          where resourceLink.m_task == theTask
                                                          select resourceLink;

            foreach (LinkTable_Task2Resource link in people)
            {
                resources.Add(link.m_resource);
            }

            // Do the same of other subclasses of IResource


            return resources;
        }

        static public void AddNewInstance(Task theTask, IResource theResource)
        {
            new LinkTable_Task2Resource(theTask, theResource);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Resource));
        }

        static public void DeleteInstance(Task theTask, IResource theResource)
        {
            Person personResource = theResource as Person;
            if (personResource != null)
            {
                foreach (LinkTable_Task2Resource link in AllInstances)
                {
                    if (link.m_task == theTask &&
                        link.m_resource == personResource)
                    {
                        LinkTable_Task2Resource.DeleteInstance(link);
                        ClearResourceCache(theTask);
                        break;
                    }
                }

                DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Resource));
            }
        }

        static public void DeleteInstance(Task theTask)
        {
            List<LinkTable_Task2Resource> itemsToDelete = new List<LinkTable_Task2Resource>();

            foreach (LinkTable_Task2Resource link in AllInstances)
            {
                if (link.m_task == theTask)
                {
                    itemsToDelete.Add(link);
                }
            }
            foreach (LinkTable_Task2Resource linkToDelete in itemsToDelete)
            {
                LinkTable_Task2Resource.DeleteInstance(linkToDelete);

            }
            ClearResourceCache(theTask);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Resource));
        }


        public List<DBAccess.DBObjectBase> AffectInstances
        {
            get
            {
                List<DBAccess.DBObjectBase> affectedInstances = new List<DBAccess.DBObjectBase>();
                if (m_task != null)
                    affectedInstances.Add(m_task);

                if (m_resource as Person != null)
                    affectedInstances.Add(m_resource as Person);

                return affectedInstances;
            }
        }


        override public void CopyDetails(LinkTable_Task2Resource instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            Logger.Log("Attempt to Merge 'LinkTable_Task2Resource' with id = " + DatabaseId);


            StatusNotModified();


            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Resource));
        }



        private Task m_task;
        private IResource m_resource;
        //////////// Private

        private LinkTable_Task2Resource(Task task, IResource resource)
        {
            AddNewInstance(this);
            m_task = task;
            m_resource = resource;
            ClearResourceCache(task);
        }

        private static void ClearResourceCache(Task theTask)
        {
            if (theTask != null)
                s_taskResourceCache.Remove(theTask);
        }

        //////// Database access

        static LinkTable_Task2Resource()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Task2Resource";
        }

        public LinkTable_Task2Resource()
        { }



        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {

            int? taskId = file.GetInt("TaskId").Value;
            if (taskId.HasValue)
                m_task = Task.GetInstanceFromDatabaseId(taskId.Value);
            int? personId = file.GetInt("PersonId");
            if (personId.HasValue)
                m_resource = Person.GetInstanceFromDatabaseId(personId.Value);
            //int? otherResourceId = file.GetInt("OtherResourceId");



        }


        protected override void CreateFromReader(DbDataReader taskRecord)
        {

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(taskRecord);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnTask2ResourceId).Value;
            int? taskId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnTaskId).Value;
            if (taskId.HasValue)
                m_task = Task.GetInstanceFromDatabaseId(taskId.Value);
            int? personId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnPersonId);
            if (personId.HasValue)
                m_resource = Person.GetInstanceFromDatabaseId(personId.Value);
            int? otherResourceId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnOtherResourceId);
        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("TaskId", (int?)GetDatabaseIdForWritingToDatabase(m_task));
            result.Add("PersonId", (int?)GetDatabaseIdForWritingToDatabase(m_resource as Person));
            result.Add("OtherResourceId", (int?)null);

            return result;
        }


        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("Task2Resource", DatabaseId);
            sqlForInstert.AddParameter("TaskId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_task));
            sqlForInstert.AddParameter("PersonId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_resource as Person));
            //sqlForInstert.AddParameter("OtherResourceId", System.Data.SqlDbType.Int, null);

            return sqlForInstert.Command;
        }


        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Task2Resource", DatabaseId);
            return deleteSql.Command;
        }



        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnTask2ResourceId = tempateRecord.GetOrdinal("Task2ResourceId");
            s_columnTaskId = tempateRecord.GetOrdinal("TaskId");
            s_columnPersonId = tempateRecord.GetOrdinal("PersonId");
            s_columnOtherResourceId = tempateRecord.GetOrdinal("OtherResourceId");

        }

        static bool s_setupHasBeenDone = false;

        static int s_columnTask2ResourceId;
        static int s_columnTaskId;
        static int s_columnPersonId;
        static int s_columnOtherResourceId;

    }


}
