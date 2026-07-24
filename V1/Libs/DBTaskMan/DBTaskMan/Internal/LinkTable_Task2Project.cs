using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

//namespace DBTaskMan.Internal
//{
//    internal class LinkTable_Task2Project : DBAccess.DBObject<LinkTable_Task2Project>, DBAccess.IInternalDBClass
//    {

//        public override string ObjectDescription { get { return "LinkTable_Task2Project:" + this.DatabaseId; } }


//        //protected override int RecursiveHidePrivateObjects()
//        //{
//        //    List<LinkTable_Task2Project> allInstances = new List<LinkTable_Task2Project>(AllInstances);


//        //    int hiddenCount = 0;
//        //    foreach (LinkTable_Task2Project l in allInstances)
//        //    {
//        //        if (l.m_project == null)
//        //        {
//        //            hiddenCount++;
//        //            MakePrivateInstanceAndHidden(l);
//        //        }
//        //    }
//        //    if (hiddenCount > 0)
//        //        RecursiveHidePrivateObjects();


//        //    return 0;
//        //}

//        //internal static void RemoveLinksToPrivateProjects()
//        //{
//        //    List<LinkTable_Task2Project> allInstances = new List<LinkTable_Task2Project>(AllInstances);


//        //    foreach (LinkTable_Task2Project l in allInstances)
//        //    {
//        //        if (l.m_project == null)
//        //        {
//        //            MakePrivateInstanceAndHidden(l);
//        //        }
//        //    }

//        //}

//        static public IEnumerable<Project> ProjectsForTask(Task theTask)
//        {
//            List<Project> projects = new List<Project>();

//            IEnumerable<LinkTable_Task2Project> projectLnks = from projectLink in AllInstances
//                                                              where projectLink.m_task == theTask
//                                                              select projectLink;

//            foreach (LinkTable_Task2Project link in projectLnks)
//            {
//                projects.Add(link.m_project);
//            }

//            return projects;
//        }

//        static public IEnumerable<Task> TaskForProjects(Project theProject)
//        {
//            List<Task> projects = new List<Task>();

//            IEnumerable<LinkTable_Task2Project> projectLnks = from projectLink in AllInstances
//                                                              where projectLink.m_project == theProject
//                                                              select projectLink;

//            foreach (LinkTable_Task2Project link in projectLnks)
//            {
//                if (!link.m_task.IsPrivateToAnotherAndHidden)
//                    projects.Add(link.m_task);
//            }

//            return projects;
//        }


//        static public IEnumerable<Task> TasksIncludingHiddenForProjects(Project theProject)
//        {
//            List<Task> projects = new List<Task>();

//            IEnumerable<LinkTable_Task2Project> projectLnks = from projectLink in AllInstances
//                                                              where projectLink.m_project == theProject
//                                                              select projectLink;

//            foreach (LinkTable_Task2Project link in projectLnks)
//                projects.Add(link.m_task);


//            return projects;
//        }






//        static public void AddNewInstance(Task theTask, Project theProject)
//        {
//            new LinkTable_Task2Project(theTask, theProject);
//            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Project));
//        }

//        static public void DeleteInstance(Task theTask, Project theProject)
//        {
//            if (theProject != null)
//            {
//                foreach (LinkTable_Task2Project link in AllInstances)
//                {
//                    if (link.m_task == theTask &&
//                        link.m_project == theProject)
//                    {
//                        LinkTable_Task2Project.DeleteInstance(link);
//                        break;
//                    }
//                }

//                DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Project));
//            }
//        }

//        static public void DeleteInstance(Task theTask)
//        {
//            List<LinkTable_Task2Project> itemsToDelete = new List<LinkTable_Task2Project>();
//            foreach (LinkTable_Task2Project link in AllInstances)
//            {
//                if (link.m_task == theTask)
//                {
//                    itemsToDelete.Add(link);
//                }
//            }
//            foreach (LinkTable_Task2Project linkToDelete in itemsToDelete)
//            {
//                LinkTable_Task2Project.DeleteInstance(linkToDelete);
//            }

//            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Project));
//        }

//        private Task m_task;
//        private Project m_project;

//        public List<DBAccess.DBObjectBase> AffectInstances
//        {
//            get
//            {
//                List<DBAccess.DBObjectBase> affectedInstances = new List<DBAccess.DBObjectBase>();
//                if (m_task != null)
//                    affectedInstances.Add(m_task);

//                if (m_project != null)
//                    affectedInstances.Add(m_project);

//                return affectedInstances;
//            }
//        }

//        override public void CopyDetails(LinkTable_Task2Project instanceToMerge)
//        {
//            CopyDetailsBase(instanceToMerge);

//            Logger.Log("Attempt to Merge 'LinkTable_Task2Project' with id = " + DatabaseId);


//            StatusNotModified();

//            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(LinkTable_Task2Project));
//        }

//        //////////// Private

//        private LinkTable_Task2Project(Task task, Project project)
//        {
//            AddNewInstance(this);
//            m_task = task;
//            m_project = project;
//        }


//        //////// Database access

//        static LinkTable_Task2Project()
//        {
//            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Task2Project";
//        }

//        public LinkTable_Task2Project()
//        { }

//        protected override void CreateFromReader(DbDataReader taskRecord)
//        {

//            if (!s_setupHasBeenDone)
//            {
//                s_setupHasBeenDone = true;
//                SetUpFromReader(taskRecord);
//            }

//            DatabaseId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnTask2ProjectId).Value;
//            int? taskId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnTaskId).Value;
//            if (taskId.HasValue)
//                m_task = Task.GetInstanceFromDatabaseId(taskId.Value);
//            int? projectId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnProjectId).Value;
//            if (projectId.HasValue)
//                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);

//        }

//        protected override DbCommand SaveToDataBaseSQLCommand()
//        {
//            SqlInsert sqlForInstert = new SqlInsert("Task2Project", DatabaseId);
//            sqlForInstert.AddParameter("TaskId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_task));
//            sqlForInstert.AddParameter("ProjectId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_project));

//            return sqlForInstert.Command;
//        }

//        protected override DbCommand DeleteFromDataBaseSQLCommand()
//        {
//            SqlDelete deleteSql = new SqlDelete("Task2Project", DatabaseId);
//            return deleteSql.Command;

//        }


//        static protected void SetUpFromReader(DbDataReader tempateRecord)
//        {

//            s_columnTask2ProjectId = tempateRecord.GetOrdinal("Task2ProjectId");
//            s_columnTaskId = tempateRecord.GetOrdinal("TaskId");
//            s_columnProjectId = tempateRecord.GetOrdinal("ProjectId");

//        }

//        static bool s_setupHasBeenDone = false;

//        static int s_columnTask2ProjectId;
//        static int s_columnTaskId;
//        static int s_columnProjectId;

//    }
//}
