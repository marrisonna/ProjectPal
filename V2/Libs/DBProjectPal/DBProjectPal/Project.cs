using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;
using DBProjectPal.Internal;

namespace DBProjectPal
{
    public class Project : DBAccess.DBObject<Project>, ISearchable, ITaskOrProject
    {
        public override string ToString()
        {
            return ObjectDescription;
        }

        public int? ProjectId
        {
            get { return this.DatabaseId; }

        }


        public override string ObjectDescription { get { return "Project: " + Utils.Misc.RemoveInvalidCharacters(this.Name); } }


        public List<ITaskOrProject> PostDependencies
        {
            get
            {
                return Internal.LinkTable_TimeDependency.ImmediatePostDependants(this);
            }
        }

        public bool HasPostDependency(ITaskOrProject postTask)
        {
            return Internal.LinkTable_TimeDependency.AlreadyLinked(this, postTask);
        }

        public void AddPostDependency(ITaskOrProject postItem)
        {
            if (!Internal.LinkTable_TimeDependency.AlreadyDirectlyLinked(this, postItem))
                Internal.LinkTable_TimeDependency.AddNewInstance(this, postItem);
        }

        public void AddPreDependency(ITaskOrProject preItem)
        {

            if (!Internal.LinkTable_TimeDependency.AlreadyDirectlyLinked(preItem, this))
                Internal.LinkTable_TimeDependency.AddNewInstance(preItem, this);
        }

        public void RemovePreDependency(ITaskOrProject preItem)
        {
            Internal.LinkTable_TimeDependency.RemoveLink(preItem, this);

        }

        public void RemovePostDependency(ITaskOrProject postItem)
        {
            Internal.LinkTable_TimeDependency.RemoveLink(this, postItem);

        }

        //protected override int RecursiveHidePrivateObjects()
        //{
        //    HashSet<int> hiddenIds = new HashSet<int>(PrivateInstances);
        //    List<Project> allInstances = new List<Project>(AllInstances);


        //    int hiddenCount = 0;
        //    foreach (Project p in allInstances)
        //    {
        //        if (p.m_parentId.HasValue && hiddenIds.Contains(p.m_parentId.Value))
        //        {
        //            hiddenCount++;
        //            MakePrivateInstanceAndHidden(p);
        //        }
        //    }
        //    if (hiddenCount > 0)
        //        RecursiveHidePrivateObjects();


        //    return 0;
        //}



        #region Public Interface

        public static bool HideCompletedProjects
        {
            get
            {
                return m_hideCompletedProjects;
            }
            set
            {
                m_hideCompletedProjects = value;
            }
        }
        private static bool m_hideCompletedProjects = false;

        public override bool IsHidden
        {
            get
            {
                return (m_hideCompletedProjects && //TotalActiveTaskCount == 0);
                         (m_priortity == PriorityValue._0_Cancelled ||
                          m_priortity == PriorityValue._0_Closed));
            }
        }

        public bool ContainsText(string searchText)
        {
            return (Name != null && Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        public bool IsClosed
        {
            get
            {
                return m_priortity == PriorityValue._0_Closed ||
                       m_priortity == PriorityValue._0_Cancelled;
            }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; StatusModified(); }
        }

        public string DetailedDescription
        {
            get { return m_detailedDescription; }
            set { m_detailedDescription = value; StatusModified(); }
        }


        //Override in sub class to hide an instance.
        public override bool IsPrivateToAnotherAndHidden
        {
            get
            {
                if (m_private && Owner != Person.DBUser && !m_superUserMayViewPrivateItems)
                    return true;


                if (Parent != null)
                    return Parent.IsPrivateToAnotherAndHidden;

                return false;
            }
        }


        public bool Private
        {
            get
            {
                return m_private;
            }
            set
            {
                if (m_private != value)
                {
                    m_private = value;
                    StatusModified();
                }
            }
        }



        public DateTime? DueDate
        {
            get { return m_dueDate; }
            set { m_dueDate = value; StatusModified(); }
        }



        public DateTime EarliestStartDate
        {
            get
            {
                return m_startDate;
            }
        }


        public DateTime StartDate
        {
            get
            {
                DateTime? latestPreDepenentEndDate = LatestPreDepenentEndDate;

                if (latestPreDepenentEndDate.HasValue)
                {
                    latestPreDepenentEndDate = Misc.AddBusinessDays(latestPreDepenentEndDate.Value, 1);
                    return latestPreDepenentEndDate.Value > m_startDate ? latestPreDepenentEndDate.Value : m_startDate;
                }

                return m_startDate;
            }
            set { m_startDate = value; StatusModified(); }
        }

        public void PerserveDependantsStartDatesForLastTask(DBProjectPal.Task lastTask)
        {
            HashSet<Task> allActiveTasks = AllActiveTasks;
            if (allActiveTasks.Count != 1)
                return;

            List<Task> activeTasks = new List<Task>(allActiveTasks);
            if (activeTasks[0] == lastTask)
            {
                if (m_parent != null)
                    m_parent.PerserveDependantsStartDatesForLastTask(lastTask);

                foreach (ITaskOrProject postItem in PostDependencies)
                {
                    Task postTask = postItem as Task;
                    Project postProject = postItem as Project;
                    if (postTask != null)
                    {
                        if (postTask.StartDate > postTask.EarliestStartDate
                            && postTask.StartDate == Utils.Misc.AddBusinessDays(this.EndDate.Value, 1))
                        {
                            postTask.StartDate = postTask.StartDate; // Side effect is to set the earliest start date
                        }

                    }
                    if (postProject != null)
                    {
                        if (postProject.StartDate > postProject.EarliestStartDate
                          && postProject.StartDate == Utils.Misc.AddBusinessDays(this.EndDate.Value, 1))
                        {
                            postProject.StartDate = postProject.StartDate; // Side effect is to set the earliest start date
                        }
                    }
                }
            }
        }

        public void ResetStartDate(bool resetChildProjects)
        {
            DateTime? minStartDate = null;

            foreach (Project currentProject in AllActiveSubProjects)
            {
                if (resetChildProjects)
                    currentProject.ResetStartDate(resetChildProjects);
                if (minStartDate == null || currentProject.StartDate < minStartDate)
                {
                    minStartDate = currentProject.StartDate;
                }
            }


            foreach (DBProjectPal.Task currentTask in this.Tasks)
            {
                if (currentTask.Status != DBProjectPal.StatusValue.Cancelled &&
                    currentTask.Status != DBProjectPal.StatusValue.Closed)
                {
                    DateTime? currentTaskStartDate = currentTask.StartDate;
                    if (currentTaskStartDate.HasValue &&
                        (minStartDate == null || currentTaskStartDate < minStartDate))
                        minStartDate = currentTaskStartDate.Value;
                }
            }

            if (minStartDate == null)
            {
                this.StartDate = DateTime.Today.Date;
            }
            else if (minStartDate != this.StartDate)
            {
                int businessdaysAdjustment = Utils.Misc.DiffBusinessDays(minStartDate.Value, this.StartDate);
                foreach (DBProjectPal.Task currentTask in this.TasksIncludingHidden)
                {
                    currentTask.AddDaysToStartRelativeDaysToProject(-businessdaysAdjustment);
                }
                this.StartDate = minStartDate.Value;
            }
        }


        DateTime? ITaskOrProject.ExpectedEndDate { get { return EndDate; } }

        internal DateTime? LatestPreDepenentEndDate
        {
            get
            {
                List<ITaskOrProject> preDependencies = Internal.LinkTable_TimeDependency.ImmediatePreDependencies(this);

                DateTime? latestEndDate = null;
                if (preDependencies.Count > 0)
                {
                    foreach (ITaskOrProject preDependant in preDependencies)
                    {
                        if (latestEndDate < preDependant.ExpectedEndDate || !latestEndDate.HasValue)
                            latestEndDate = preDependant.ExpectedEndDate;
                    }
                }

                if (m_parent != null)
                {
                    DateTime? projectLatestPreDependantEndDate = m_parent.LatestPreDepenentEndDate;
                    if (projectLatestPreDependantEndDate > latestEndDate || !latestEndDate.HasValue)
                        latestEndDate = projectLatestPreDependantEndDate;
                }

                return latestEndDate;
            }
        }

        private static Dictionary<Project, DateTime?> s_endDateCache = new Dictionary<Project, DateTime?>();
        public static void ClearDateCaches()
        {
            if (s_endDateCache != null)
                s_endDateCache.Clear();
        }



        public DateTime? EndDate
        {
            get
            {
                DateTime? endDate = null;
                if (s_endDateCache.TryGetValue(this, out endDate))
                    return endDate;

                DateTime maxEndDate = DateTime.MinValue;
                foreach (Task currentTask in Tasks)
                {
                    if (currentTask.Status == StatusValue.Cancelled ||
                        currentTask.Status == StatusValue.Closed)
                        continue;
                    DateTime? taskEndDate = currentTask.EndDate;
                    if (taskEndDate > maxEndDate)
                        maxEndDate = taskEndDate.Value;
                }

                foreach (Project currentProject in SubProjects)
                {
                    if (currentProject.Priority == PriorityValue._0_Cancelled ||
                        currentProject.Priority == PriorityValue._0_Closed)
                        continue;
                    DateTime? projectEndDate = currentProject.EndDate;
                    if (projectEndDate > maxEndDate)
                        maxEndDate = projectEndDate.Value;
                }
                if (maxEndDate != DateTime.MinValue)
                    endDate = maxEndDate;

                s_endDateCache[this] = endDate;
                return endDate;

            }
        }

        public string FullName
        {
            get
            {
                if (Parent == null) return m_name;
                return m_name + " => [" + FullParentName + "]";
            }
        }

        public string FullParentName
        {
            get
            {
                if (Parent == null) return "";
                return Parent.FullPathName;
            }
        }

        private string FullPathName
        {
            get
            {
                if (Parent == null) return m_name;
                return Parent.FullPathName + "=>" + m_name;
            }
        }

        public PriorityValue? Priority
        {
            get { return m_priortity; }
            set
            {
                m_priortity = value;
                StatusModified();
                if (m_priortity == PriorityValue._0_Closed || m_priortity == PriorityValue._0_Cancelled)
                {

                    foreach (Project subProject in SubProjects)
                    {
                        subProject.Priority = value;
                    }

                }
            }

        }

        public List<PriorityValue?> PriorityList
        {
            get
            {
                List<PriorityValue?> result;
                if (m_parent == null)
                {
                    result = new List<PriorityValue?>();
                }
                else
                {
                    result = m_parent.PriorityList;
                }
                result.Add(m_priortity);
                return result;
            }
        }

        public Project Parent
        {
            get
            {
                if (m_parent == null && m_parentId.HasValue)
                {
                    m_parent = Project.GetInstanceFromDatabaseId(m_parentId.Value);
                }
                return m_parent;
            }
            set
            {
                if (m_parent != null)
                    m_parent.m_subProjects = null; // clear cache

                m_parent = value;

                if (m_parent != null)
                    m_parent.m_subProjects = null; // clear cache
                //m_parentId = null;
                StatusModified();
            }

        }
        public IEnumerable<Project> SubProjects
        {
            get
            {
                if (m_subProjects == null)
                {
                    m_subProjects = new List<Project>();
                    foreach (Project currentProject in AllInstances)
                    {
                        if (currentProject.Parent == this)
                            if (!currentProject.IsPrivateToAnotherAndHidden)
                                m_subProjects.Add(currentProject);

                    }
                    //m_subProjects = from theInstance in AllInstances
                    //                where theInstance.Parent == this
                    //                && !theInstance.IsPrivateToOtherUser
                    //                select theInstance;

                }
                return m_subProjects;
            }
        }

        public IEnumerable<Project> SubProjectsIncludingHidden
        {
            get
            {
                IEnumerable<Project> subProjects = from theInstance in AllInstances
                                                   where theInstance.Parent == this
                                                   select theInstance;

                return subProjects;
            }
        }


        internal int ProjectID { get { return DatabaseId; } }

        public IEnumerable<Task> Tasks
        {
            get
            {
                return Task.TaskForProjects(this);
            }
        }


        public IEnumerable<Task> TasksIncludingHidden
        {
            get
            {
                return Task.TasksIncludingHiddenForProjects(this);
            }
        }





        public HashSet<Task> AllActiveTasks
        {
            get
            {
                HashSet<Task> allTasks = new HashSet<Task>();
                foreach (Project subProject in SubProjects)
                {
                    if (subProject.IsActive)
                    {
                        foreach (Task subTask in subProject.AllActiveTasks)
                        {
                            if (!allTasks.Contains(subTask))
                                allTasks.Add(subTask);
                        }
                    }
                }

                foreach (Task thisProjectTask in Tasks)
                {
                    if (thisProjectTask.Priority == PriorityValue._0_Cancelled ||
                        thisProjectTask.Priority == PriorityValue._0_Closed)
                        continue;

                    if (!allTasks.Contains(thisProjectTask))
                        allTasks.Add(thisProjectTask);
                }
                return allTasks;
            }
        }



        public HashSet<Project> AllActiveSubProjects
        {
            get
            {
                HashSet<Project> allProjects = new HashSet<Project>();
                foreach (Project subProject in SubProjects)
                {
                    if (subProject.IsActive)
                    {
                        if (!allProjects.Contains(subProject))
                            allProjects.Add(subProject);

                        HashSet<Project> activeSubProjects = subProject.AllActiveSubProjects;
                        foreach (Project activeSubProject in activeSubProjects)
                        {
                            if (!allProjects.Contains(activeSubProject))
                                allProjects.Add(activeSubProject);
                        }
                    }
                }
                return allProjects;
            }
        }

        public List<Attachment> Attachments
        {
            get
            {
                return Attachment.AttachmentsForProject(this);
            }
        }

        public double TotalActiveTaskEffort
        {
            get
            {
                double taskEffort = ActiveTaskEffort;
                foreach (DBProjectPal.Project currentProject in SubProjects)
                {
                    taskEffort += currentProject.TotalActiveTaskEffort;
                }
                return taskEffort;
            }
        }

        public double ActiveTaskEffort
        {
            get
            {
                double taskEffort = 0;
                foreach (Task currentTask in Tasks)
                {
                    if (currentTask.Status != StatusValue.Cancelled &&
                        currentTask.Status != StatusValue.Closed)
                        taskEffort += currentTask.TotalEffort;
                }

                return taskEffort;
            }
        }


        public int TotalActiveTaskCount
        {
            get
            {
                int taskCount = ActiveTaskCount;
                foreach (DBProjectPal.Project currentProject in SubProjects)
                {
                    taskCount += currentProject.TotalActiveTaskCount;
                }
                return taskCount;
            }
        }

        public int ActiveTaskCount
        {
            get
            {
                int taskCount = 0;
                foreach (Task currentTask in Tasks)
                {
                    if (currentTask.Status != StatusValue.Cancelled &&
                        currentTask.Status != StatusValue.Closed)
                        taskCount++;
                }

                return taskCount;
            }
        }

        static public Project AddNewInstance(string name, Project parent)
        {

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Project));
            return new Project(name, parent);
        }

        static public Project FindProject(string name)
        {
            foreach (Project thisProject in AllInstances)
            {
                if (thisProject.FullName == name)
                    return thisProject;
            }

            return null;

        }

        static public IList<Project> TopLevelProjects
        {
            get
            {
                List<Project> topLevelProjects = new List<Project>();

                foreach (Project thisProject in AllInstances)
                {
                    if (thisProject.Parent == null)
                        topLevelProjects.Add(thisProject);
                }


                return topLevelProjects;
            }
        }

        public bool IsDescendantOf(Project possibleParent)
        {
            if (Parent == null)
                return false;

            if (Parent == possibleParent)
                return true;

            return Parent.IsDescendantOf(possibleParent);
        }


        #endregion

        override public void CopyDetails(Project instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            m_priortity = instanceToMerge.m_priortity;
            m_name = instanceToMerge.m_name;
            m_detailedDescription = instanceToMerge.m_detailedDescription;
            m_parent = instanceToMerge.m_parent;
            m_parentId = instanceToMerge.m_parentId;
            m_subProjects = null;
            m_private = instanceToMerge.m_private;
            m_dueDate = instanceToMerge.m_dueDate;

            StatusNotModified();

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Project));
        }


        ///// Private


        int? m_parentId = null;
        Project m_parent = null;
        List<Project> m_subProjects = null;
        string m_name;
        string m_detailedDescription;
        PriorityValue? m_priortity;
        DateTime? m_dueDate = null;
        DateTime m_startDate = DateTime.Now.Date;
        bool m_private;
        //DateTime? m_startDate = null;

        private Project(string name, Project parent)
        {
            AddNewInstance(this);
            m_name = name;
            m_parent = parent;
            if (m_parent != null)
                m_parent.m_subProjects = null; // Clear Cache
        }

        //////// Database access

        static Project()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Project";
        }

        public Project()
        { }

        public bool IsActive
        {
            get
            {
                if (this.Priority == PriorityValue._0_Cancelled ||
                    this.Priority == PriorityValue._0_Closed)
                    return false;
                foreach (Task currentTask in Tasks)
                {
                    if (currentTask.Status.HasValue &&
                       currentTask.Status != StatusValue.Closed &&
                        currentTask.Status != StatusValue.Cancelled)
                        return true;
                }
                foreach (Project currentProject in SubProjects)
                {
                    if (currentProject.IsActive)
                        return true;
                }
                return false;
            }

        }



        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {
            m_parentId = file.GetInt("ParentProjectId");
            if (m_parentId.HasValue)
            {
                m_parent = Project.GetInstanceFromDatabaseId(m_parentId.Value);
            }

            m_name = Utils.SecurityFunctions.Decrypt(file.GetString("Name"));
            m_detailedDescription = Utils.SecurityFunctions.Decrypt(file.GetString("DetailedDescription"));
            Priority = DatabaseBase.ParseDBEnumString<PriorityValue>(file.GetString("Priority"));
            m_owner = file.GetString("Owner");
            m_private = file.GetBool("Private") ?? false;
            m_dueDate = file.GetDate("DueDate");
            m_startDate = file.GetDate("StartDate") ?? DateTime.Now.Date;
   
        }




        protected override void CreateFromReader(DbDataReader record)
        {

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(record);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(record, s_columnProjectId).Value;
            m_parentId = DatabaseBase.GetColumnValueAs<int>(record, s_columnParentComponentId);
            if (m_parentId.HasValue)
            {
                m_parent = Project.GetInstanceFromDatabaseId(m_parentId.Value);
            }
            m_name = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(record, s_columnName));
            m_detailedDescription = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(record, s_columnDetailedDescription));
            Priority = DatabaseBase.ParseDBEnumString<PriorityValue>(DatabaseBase.GetColumnValueAsString(record, s_columnPriority));
            m_owner = DatabaseBase.GetColumnValueAsString(record, s_columnOwner);
            m_private = DatabaseBase.GetColumnValueAs<bool>(record, s_columnPrivate) ?? false;
            m_dueDate = DatabaseBase.GetColumnValueAs<DateTime>(record, s_columnDueDate);
            m_startDate = DatabaseBase.GetColumnValueAs<DateTime>(record, s_columnStartDate) ?? DateTime.Now.Date;
        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("Name", Utils.SecurityFunctions.Encrypt(m_name));
            result.Add("ParentProjectId", (int?)GetDatabaseIdForWritingToDatabase(Parent));
            result.Add("Priority", DatabaseBase.ToDBEnumString(Priority));
            result.Add("DetailedDescription", Utils.SecurityFunctions.Encrypt(m_detailedDescription));
            result.Add("Owner", m_owner);
            result.Add("Private", m_private);
            result.Add("DueDate", m_dueDate);
            result.Add("StartDate", m_startDate);
            
            return result;
        }


        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("Project", DatabaseId);

            string nameToSave = m_private ? Utils.SecurityFunctions.Encrypt(m_name) : m_name;

            sqlForInstert.AddParameter("Name", System.Data.SqlDbType.NVarChar, 1000, nameToSave);
            sqlForInstert.AddParameter("ParentProjectId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(Parent));
            sqlForInstert.AddParameter("Priority", System.Data.SqlDbType.NVarChar, 50, DatabaseBase.ToDBEnumString(Priority));
            if (Utils.DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                sqlForInstert.AddParameter("DetailedDescription", System.Data.SqlDbType.NVarChar, 1000000, Utils.SecurityFunctions.Encrypt(m_detailedDescription));
            else
                sqlForInstert.AddParameter("DetailedDescription", System.Data.SqlDbType.NText, Utils.SecurityFunctions.Encrypt(m_detailedDescription));
            sqlForInstert.AddParameter("Owner", System.Data.SqlDbType.NVarChar, 50, m_owner);
            sqlForInstert.AddParameter("Private", System.Data.SqlDbType.Bit, m_private);
            sqlForInstert.AddParameter("DueDate", System.Data.SqlDbType.DateTime, m_dueDate);
            sqlForInstert.AddParameter("StartDate", System.Data.SqlDbType.DateTime, m_startDate);

            return sqlForInstert.Command;
        }

        override public void DeleteInstance()
        {
            if (!HasDependants)
                Project.DeleteInstance(this);

            if (this.m_parent != null)
                this.m_parent.m_subProjects = null; // clear cache

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Project)); 
        }

        public bool HasDependants
        {
            get
            {
                return this.Tasks.Count() > 0 || this.SubProjects.Count() > 0;
            }
        }


        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Project", DatabaseId);
            return deleteSql.Command;

        }


        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnProjectId = tempateRecord.GetOrdinal("ProjectId");
            s_columnParentComponentId = tempateRecord.GetOrdinal("ParentProjectId");
            s_columnName = tempateRecord.GetOrdinal("Name");
            s_columnDetailedDescription = tempateRecord.GetOrdinal("DetailedDescription");
            s_columnPriority = tempateRecord.GetOrdinal("Priority");
            s_columnOwner = tempateRecord.GetOrdinal("Owner");
            s_columnPrivate = tempateRecord.GetOrdinal("Private");
            s_columnDueDate = tempateRecord.GetOrdinal("DueDate");
            s_columnStartDate = tempateRecord.GetOrdinal("StartDate");
        }

        static bool s_setupHasBeenDone = false;

        static int s_columnProjectId;
        static int s_columnParentComponentId;
        static int s_columnName;
        static int s_columnDetailedDescription;
        static int s_columnPriority;
        static int s_columnOwner;
        static int s_columnPrivate;
        static int s_columnDueDate;
        static int s_columnStartDate;

        public List<ITaskOrProject> ImmediatePreDependencies
        {
            get
            {
                return Internal.LinkTable_TimeDependency.ImmediatePreDependencies(this);
            }
        }

        public List<ITaskOrProject> ImmediatePostDependants
        {
            get
            {
                return Internal.LinkTable_TimeDependency.ImmediatePostDependants(this);
            }
        }


        /////////////////////////       

    }
}
