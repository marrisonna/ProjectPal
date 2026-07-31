using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Utils;

namespace DBProjectPal
{
    public class Task : DBAccess.DBObject<Task>, ISearchable, ITaskOrProject
    {

        //protected override int RecursiveHidePrivateObjects()
        //{
        //    List<Task> allInstances = new List<Task>(AllInstances);


        //    int hiddenCount = 0;
        //    foreach (Task t in allInstances)
        //    {

        //        if (t.Projects.Count() > 0)
        //        {
        //            bool allAreHidden = true;
        //            foreach (Project p in t.Projects)
        //            {
        //                if (p != null)
        //                {
        //                    allAreHidden = false;
        //                    break;
        //                }
        //            }
        //            if (allAreHidden)
        //            {
        //                hiddenCount++;
        //                MakePrivateInstanceAndHidden(t);
        //            }
        //        }
        //    }
        //    if (hiddenCount > 0)
        //        RecursiveHidePrivateObjects();

        //    DBProjectPal.Internal.LinkTable_Task2Project.RemoveLinksToPrivateProjects();

        //    return 0;
        //}

        public const string OtherResource = "Other";

        #region Public Interface

        public bool ContainsText(string searchText)
        {
            return (Description != null && Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1) ||
                (DetailedDescription != null && DetailedDescription.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        public bool IsClosed
        {
            get
            {
                return m_priortity == PriorityValue._0_Cancelled ||
                       m_priortity == PriorityValue._0_Closed;
            }
        }

        public bool IsActive
        {
            get
            {
                return !IsClosed;
            }
        }

        public List<IResource> Resources
        {
            get
            {
                if (HideTentative)
                    return new List<IResource>() { Person.UnassignedResource };
                return Internal.LinkTable_Task2Resource.ResourcesForTask(this);
            }
        }

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

        public List<IResource> TeamResources
        {
            get
            {
                List<IResource> resourcesForThisTask = new List<IResource>();
                foreach (DBProjectPal.IResource resource in Resources)
                {
                    DBProjectPal.Person resourceName = DBProjectPal.Person.OtherPerson;
                    DBProjectPal.Person realPerson = DBProjectPal.Person.FindPerson(resource.Name);
                    if (realPerson.IsResource)
                        resourceName = realPerson;

                    if (!resourcesForThisTask.Contains(resourceName))
                        resourcesForThisTask.Add(resourceName);
                }
                if (resourcesForThisTask.Count == 0)
                {
                    resourcesForThisTask.Add(DBProjectPal.Person.UnassignedResource);
                }
                return resourcesForThisTask;
            }
        }

        public void ResourceAdd(IResource theResource)
        {
            m_cachedDuration = null;
            Internal.LinkTable_Task2Resource.AddNewInstance(this, theResource);
        }
        public void ResourceRemove(IResource theResource)
        {
            m_cachedDuration = null;
            Internal.LinkTable_Task2Resource.DeleteInstance(this, theResource);
        }
        public PriorityValue? Priority
        {
            get { return m_priortity; }
            set
            {
                m_priortity = value;
                StatusModified();

                if (m_priortity == PriorityValue._0_Cancelled)
                    m_status = StatusValue.Cancelled;
                else if (m_priortity == PriorityValue._0_Closed)
                    m_status = StatusValue.Closed;
                else if (m_status == StatusValue.Closed ||
                         m_status == StatusValue.Cancelled)
                    m_status = StatusValue.InProgress;
            }
        }

        public void SetStartRelativeDaysToProject(int relativeDays)
        {
            m_startRelativeDaysToProject = relativeDays;
            StatusModified();
        }

        public void AddDaysToStartRelativeDaysToProject(int businessDaysToAdd)
        {
            m_startRelativeDaysToProject += businessDaysToAdd;
            StatusModified();
        }


        public Project Project
        {
            get
            {
                return m_project;
            }
            set
            {
                ClearTaskToProjectCache(m_project);
                ClearTaskToProjectCache(value);
                m_project = value;
                StatusModified();
            }
        }

        public DateTime? EarliestStartDate
        {
            get
            {
                DateTime? possibleStartDate = null;

                if (m_startRelativeDaysToProject.HasValue)
                {
                    possibleStartDate = Utils.Misc.AddBusinessDays(Project.StartDate, m_startRelativeDaysToProject.Value);

                }
                return possibleStartDate;
            }
        }

        public DateTime? StartDate
        {
            get
            {
                DateTime? startDate = null;
                if (!s_startDateCache.TryGetValue(this, out startDate))
                {

                    DateTime? earliestConstrainedStartDate = null;
                    DateTime? latestEndDate = LatestPreDepenentEndDate;
                    if (latestEndDate.HasValue)
                        earliestConstrainedStartDate = Utils.Misc.AddBusinessDays(latestEndDate.Value, 1);

                    DateTime? possibleStartDate = EarliestStartDate;

                    if (earliestConstrainedStartDate.HasValue && possibleStartDate.HasValue)
                        return earliestConstrainedStartDate > possibleStartDate ? earliestConstrainedStartDate : possibleStartDate;

                    startDate = earliestConstrainedStartDate ?? possibleStartDate;
                    s_startDateCache[this] = startDate;
                }
                return startDate;
            }
            set
            {
                if (value == null || Project == null)
                {
                    m_startRelativeDaysToProject = null;
                }
                else
                {
                    DateTime? projectStartDate = Project.StartDate;
                    if (!projectStartDate.HasValue)
                    {
                        m_startRelativeDaysToProject = 0;
                    }
                    else
                    {
                        m_startRelativeDaysToProject = Utils.Misc.DiffBusinessDays(value.Value, Project.StartDate);
                        //m_startRelativeDaysToProject = (value.Value - Project.StartDate.Value).Days;
                    }
                }
                StatusModified();
            }
        }

        public bool HasRelativeStartDate { get { return m_startRelativeDaysToProject.HasValue; } }

        public DateTime? LatestPreDepenentEndDate
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


                Project currentProject = Project;
                if (currentProject != null)
                {
                    DateTime? projectLatestPreDependantEndDate = currentProject.LatestPreDepenentEndDate;
                    if (projectLatestPreDependantEndDate > latestEndDate || !latestEndDate.HasValue)
                        latestEndDate = projectLatestPreDependantEndDate;
                }

                return latestEndDate;
            }
        }

        public bool StartIsDeterminedByPreDependency
        {
            get
            {
                return LatestPreDepenentEndDate.HasValue;
            }

        }


        public static void ClearDateCaches()
        {
            if (s_endDateCache != null)
                s_endDateCache.Clear();
            if (s_startDateCache != null)
                s_startDateCache.Clear();
        }


        private static Dictionary<Task, DateTime?> s_endDateCache = new Dictionary<Task, DateTime?>();
        private static Dictionary<Task, DateTime?> s_startDateCache = new Dictionary<Task, DateTime?>();


        public DateTime? EndDate
        {
            get
            {
                DateTime? endDate = null;
                if (s_endDateCache.TryGetValue(this, out endDate))
                    return endDate;

                double? duration = Duration;
                DateTime? startDate = StartDate;
                if (startDate.HasValue && duration.HasValue)
                    endDate = Utils.Misc.AddBusinessDays(startDate.Value, (int)(duration.Value + 0.99) - 1);


                s_endDateCache[this] = endDate;
                return endDate;

            }
            set
            {
                double? duration = Duration;
                if (value.HasValue && duration.HasValue)
                {
                    DateTime startDate = Utils.Misc.AddBusinessDays(value.Value, -((int)(duration.Value + 0.99) - 1));
                    StartDate = startDate;
                }
            }
        }


        private double? m_cachedDuration = null;
        public double? Duration
        {
            get
            {
                if (m_cachedDuration.HasValue)
                    return m_cachedDuration;

                if (!m_effortInDays.HasValue)
                    return null;

                if (m_effortType.Value == EffortTypeValue.ManDays)
                {
                    double teamResoureCount = 0;
                    foreach (DBProjectPal.Person assignedResource in this.Resources)
                    {
                        if (assignedResource.IsResource || assignedResource.Name == "Unassigned")
                            teamResoureCount++;
                    }
                    double percentageCommitment = m_percentageAllocation;
                    if (teamResoureCount == 0)
                    {
                        percentageCommitment = teamResoureCount = 1;
                    }
                    if (teamResoureCount > 0 && percentageCommitment > 0)
                        m_cachedDuration = m_effortInDays.Value / teamResoureCount / percentageCommitment;
                    return m_cachedDuration;
                }
                else
                {
                    m_cachedDuration = m_effortInDays;
                    return m_cachedDuration;
                }
            }
        }

        public double TotalEffort
        {
            get
            {
                bool includeUnassigned = true;
                bool includeUnscheduled = true;

                if (!includeUnscheduled && !EndDate.HasValue) // Unscheduled
                    return 0;

                if (!m_effortInDays.HasValue)
                    return 0;

                int resoureCount = 0;
                foreach (Person currentPerson in Resources)
                {
                    if (currentPerson != null && currentPerson.IsResource)
                    {
                        if (!includeUnassigned && currentPerson.Name == "Unassigned")
                            continue;
                        resoureCount++;
                    }
                }
                if (includeUnassigned && Resources.Count() == 0)
                    resoureCount = 1; // It is not assigned, but must be in future, else why is it here?


                if (EffortType.Value == EffortTypeValue.ManDays)
                {
                    if (resoureCount == 0) // Unassigned (and to be ignored) or Other (not team) task
                        return 0;
                    return m_effortInDays.Value;
                }

                return m_effortInDays.Value * resoureCount * m_percentageAllocation;
            }
        }


        DateTime? ITaskOrProject.ExpectedEndDate { get { return EndDate; } }

        public DateTime StatusDate
        {
            get { return m_statusDate; }
            set { m_statusDate = value; StatusModified(); }
        }

        public int? TaskId
        {
            get { return DatabaseId >= 0 ? (int?)DatabaseId : null; }
        }

        public Component AffectedComponent
        {
            get { return m_affectedComponent; }
            set { m_affectedComponent = value; StatusModified(); }
        }
        public String Description
        {
            get { return m_description; }
            set { m_description = value; StatusModified(); }
        }


        public override string ToString()
        {
            return ObjectDescription;
        }

        public override string ObjectDescription { get { return "Task: " + Utils.Misc.RemoveInvalidCharacters(Description); } }

        public String OrigTaskNumber
        {
            get { return m_origTaskNumber; }
        }

        public String ExternalReferenceURL
        {
            get { return m_externalReferenceURL; }
            set { m_externalReferenceURL = value; StatusModified(); }
        }
        

        public String DetailedDescription
        {
            get { return m_detailedDescription; }
            set { m_detailedDescription = value; StatusModified(); }
        }
        public DateTime DateAdded
        {
            get { return m_dateAdded; }
            set { m_dateAdded = value; StatusModified(); }
        }
        public double? EffortInDays
        {
            get { return m_effortInDays; }
            set { m_effortInDays = value; m_cachedDuration = null; StatusModified(); }
        }
        public EffortTypeValue? EffortType
        {
            get { return m_effortType; }
            set { m_effortType = value; m_cachedDuration = null; StatusModified(); }
        }
        public double PercentageAllocation
        {
            get { return m_percentageAllocation; }
            set { m_percentageAllocation = value; m_cachedDuration = null; StatusModified(); }
        }
        public TaskTypeValue? TaskType
        {
            get { return m_taskType; }
            set { m_taskType = value; StatusModified(); }
        }

        public bool ResourceAssignmentIsTentative
        {
            get
            {
                if (HideTentative)
                    return false;
                return m_resourceAssignmentIsTentative;
            }
            set
            {
                if (m_resourceAssignmentIsTentative != value)
                {
                    m_resourceAssignmentIsTentative = value;
                    StatusModified();
                }
            }
        }
         
        static public IEnumerable<Task> TaskForProjects(Project theProject)
        {
            List<Task> tasks;
            if (m_projectToTaskCache.TryGetValue(theProject, out tasks))
                return tasks;

            tasks = new List<Task>();
            m_projectToTaskCache.Add(theProject, tasks);

            foreach (Task currentTask in AllInstances)
            {
                if (currentTask.m_project == theProject && !currentTask.IsPrivateToAnotherAndHidden)
                    tasks.Add(currentTask);
            }

            return tasks;
        }


        private static Dictionary<Project, List<Task>> m_projectToTaskCache = new Dictionary<Project, List<Task>>();
        private static Dictionary<Project, List<Task>> m_projectToTaskIncludingHiddenCache = new Dictionary<Project, List<Task>>();
        private static void ClearTaskToProjectCache(Project projectToClear)
        {
            if (projectToClear != null)
            {
                m_projectToTaskCache.Remove(projectToClear);
                m_projectToTaskIncludingHiddenCache.Remove(projectToClear);
            }
        }

        static public IEnumerable<Task> TasksIncludingHiddenForProjects(Project theProject)
        {
            List<Task> tasks;
            if (m_projectToTaskIncludingHiddenCache.TryGetValue(theProject, out tasks))
                return tasks;

            tasks = new List<Task>();
            m_projectToTaskIncludingHiddenCache.Add(theProject, tasks);

            foreach (Task currentTask in AllInstances)
            {
                if (currentTask.m_project == theProject)
                    tasks.Add(currentTask);
            }

            return tasks;
        }


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

        //Override in sub class to hide an instance.
        public override bool IsPrivateToAnotherAndHidden
        {
            get
            {
                if (m_private && Owner != Person.DBUser && !m_superUserMayViewPrivateItems)
                    return true;


                if (Project != null)
                {
                    bool allAreHidden = true;
                    Project parentProject = Project;

                    if (!parentProject.IsPrivateToAnotherAndHidden)
                    {
                        allAreHidden = false;
                    }

                    if (allAreHidden)
                        return true;
                }

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


        public StatusValue? Status
        {
            get { return m_status; }
            set
            {
                if (m_status != StatusValue.Cancelled &&
                    m_status != StatusValue.Closed &&
                     (value == StatusValue.Cancelled || value == StatusValue.Closed))
                    PerserveDepantsStartDates();

                m_status = value;
                StatusModified();

                if (m_status == StatusValue.Cancelled)
                    m_priortity = PriorityValue._0_Cancelled;
                else if (m_status == StatusValue.Closed)
                    m_priortity = PriorityValue._0_Closed;
                else if (m_priortity == PriorityValue._0_Closed ||
                        m_priortity == PriorityValue._0_Cancelled)
                    m_priortity = PriorityValue._3_Med;
            }
        }


        private void PerserveDepantsStartDates()
        {
            if (this.EndDate.HasValue)
            {
                foreach (ITaskOrProject postItem in this.PostDependencies)
                {
                    Task postTask = postItem as Task;
                    DBProjectPal.Project postProject = postItem as DBProjectPal.Project;
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
            // Now check if this was the last active task in the project, if so we need to preserve the
            // start dates of depends on this project.
            DBProjectPal.Project parentProject = this.Project;
            if (parentProject != null)
                parentProject.PerserveDependantsStartDatesForLastTask(this);

        }


        public Person RequestedBy
        {
            get { return m_requestorPerson; }
            set { m_requestorPerson = value; StatusModified(); }
        }


        private HashSet<Attachment> m_attachments = new HashSet<Attachment>();

        public List<Attachment> Attachments
        {
            get
            {
                return new List<Attachment>(m_attachments);
                //return Attachment.AttachmentsForTask(this);
            }
        }

        public void AddAttachment(Attachment theAttachment)
        {
            m_attachments.Add(theAttachment);
        }

        public void RemoveAttachment(Attachment theAttachment)
        {
            m_attachments.Remove(theAttachment);
        }


        public List<Remark> Remarks
        {
            get
            {
                return Remark.RemarksForTask(this);
            }
        }




        static public Task AddNewInstance(string description, Component affectedComponent)
        {
            Task theNewTask = new Task(description, affectedComponent);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Task));
            return theNewTask;
        }



        private Task(string description, Component affectedComponent)
        {
            AddNewInstance(this);
            m_description = description;
            m_affectedComponent = affectedComponent;
            m_statusDate = DBAccess.DBObjectBase.DBTime;
        }



        private bool HideTentative
        {
            get
            {
                if (Person.CurrentUser == null)
                    return true;

                if (Person.CurrentUser.UserType == Permissions.UserLevel.SuperUser || !m_resourceAssignmentIsTentative)
                    return false;


                if (Person.CurrentUser.UserType == Permissions.UserLevel.PowerUser)
                {

                    if (Owner == Person.DBUser)
                        return false;

                    IEnumerable<IResource> resources = Internal.LinkTable_Task2Resource.ResourcesForTask(this);

                    if (resources.Contains(Person.CurrentUser))
                        return true;

                    return false;
                }

                return true;
            }
        }


        #endregion

        override public void CopyDetails(Task instanceToMerge)
        {
            CopyDetailsBase(instanceToMerge);

            m_requestorPerson = instanceToMerge.m_requestorPerson;
            m_affectedComponent = instanceToMerge.m_affectedComponent;
            m_priortity = instanceToMerge.m_priortity;
            m_statusDate = instanceToMerge.m_statusDate;
            m_description = instanceToMerge.m_description;
            m_detailedDescription = instanceToMerge.m_detailedDescription;
            m_origTaskNumber = instanceToMerge.m_origTaskNumber;
            m_externalReferenceURL = instanceToMerge.m_externalReferenceURL;
            m_dateAdded = instanceToMerge.m_dateAdded;
            m_effortInDays = instanceToMerge.m_effortInDays;
            m_cachedDuration = null;
            m_effortType = instanceToMerge.m_effortType;
            m_cachedDuration = null;
            m_percentageAllocation = instanceToMerge.m_percentageAllocation;
            m_taskType = instanceToMerge.m_taskType;
            m_status = instanceToMerge.m_status;
            m_private = instanceToMerge.m_private;
            m_resourceAssignmentIsTentative = instanceToMerge.m_resourceAssignmentIsTentative;

            StatusNotModified();


            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Task));
        }



        Person m_requestorPerson = null;
        Component m_affectedComponent = null;

        Project m_project;
        PriorityValue? m_priortity;
        DateTime m_statusDate;
        String m_description;
        String m_externalReferenceURL;
        String m_origTaskNumber;
        String m_detailedDescription;
        DateTime m_dateAdded;
        double? m_effortInDays;
        EffortTypeValue? m_effortType = EffortTypeValue.ManDays;
        double m_percentageAllocation = 1; // 100%
        TaskTypeValue? m_taskType;
        StatusValue? m_status;
        bool m_resourceAssignmentIsTentative;
        bool m_private;
        int? m_startRelativeDaysToProject;

        /////// Linking objects

        internal static IEnumerable<Task> RequestedTasksForPerson(Person thePerson)
        {
            IEnumerable<Task> requestedTasks = from aTask in AllInstances
                                               where aTask.RequestedBy == thePerson
                                               select aTask;
            return requestedTasks;
        }

        internal static IEnumerable<Task> TasksForComponent(Component theComponent)
        {
            List<Task> componentTasks = new List<Task>();
            foreach (Task thisTask in AllInstances)
            {
                if (thisTask.AffectedComponent == theComponent)
                    componentTasks.Add(thisTask);
            }
            //IEnumerable<Task> componentTasks = from aTask in AllInstances
            //                                   where aTask.AffectedComponent == theComponent
            //                                   select aTask;

            return componentTasks;
        }

        /////////////////////





        //////// Database access

        static Task()
        {
            AllInstanceQuery = "select * from " + DatabaseBase.Schema + "Task";
        }




        public Task()
        { }


        protected override void CreateFromFile(DBAccess.FileSystem_File file)
        {
            int? projectId = file.GetInt("ProjectId");
            if (projectId.HasValue)
            {
                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);
                ClearTaskToProjectCache(m_project);
            }

            Description = Utils.SecurityFunctions.Decrypt(file.GetString("Description"));
            m_origTaskNumber = file.GetString("OrigTaskNumber");
            m_externalReferenceURL = file.GetString("ExternalReferenceURL");
            DetailedDescription = Utils.SecurityFunctions.Decrypt(file.GetString("DetailedDescription"));
            Priority = DatabaseBase.ParseDBEnumString<PriorityValue>(file.GetString("Priority"));

            int? requestorPersonId = file.GetInt("RequestorPersonId");
            if (requestorPersonId.HasValue)
                m_requestorPerson = Person.GetInstanceFromDatabaseId(requestorPersonId.Value);

            int? affectedComponentId = file.GetInt("AffectedComponentId");
            if (affectedComponentId.HasValue)
                m_affectedComponent = Component.GetInstanceFromDatabaseId(affectedComponentId.Value);


            DateAdded = file.GetDate("DateAdded").Value;

            EffortInDays = file.GetDouble("EffortInDays");
            EffortType = DatabaseBase.ParseDBEnumString<EffortTypeValue>(file.GetString("EffortType")) ?? EffortTypeValue.ManDays;
            m_percentageAllocation = file.GetDouble("PercentageAllocation") ?? 1;
            TaskType = DatabaseBase.ParseDBEnumString<TaskTypeValue>(file.GetString("TaskType"));
            Status = DatabaseBase.ParseDBEnumString<StatusValue>(file.GetString("Status"));

            DateTime? dbStatusDate = file.GetDate("StatusDate");
            m_startRelativeDaysToProject = file.GetInt("StartRelativeDaysToProject");

            StatusDate = (dbStatusDate.HasValue) ? dbStatusDate.Value : DBAccess.DBObjectBase.DBTime.Date;
            m_owner = file.GetString("Owner");
            m_private = file.GetBool("Private") ?? false;

            m_resourceAssignmentIsTentative = file.GetBool("TentativeResourceAssignment") ?? false;

        }




        protected override void CreateFromReader(DbDataReader taskRecord)
        {

            //Utilities.

            if (!s_setupHasBeenDone)
            {
                s_setupHasBeenDone = true;
                SetUpFromReader(taskRecord);
            }

            DatabaseId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnTaskId).Value;

            int? projectId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnProjectId);
            if (projectId.HasValue)
            {
                m_project = Project.GetInstanceFromDatabaseId(projectId.Value);
                ClearTaskToProjectCache(m_project);
            }

            Description = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(taskRecord, s_columnDescription));
            m_origTaskNumber = DatabaseBase.GetColumnValueAsString(taskRecord, s_origTaskNumber);
            m_externalReferenceURL = DatabaseBase.GetColumnValueAsString(taskRecord, s_externalReferenceURL);
            DetailedDescription = Utils.SecurityFunctions.Decrypt(DatabaseBase.GetColumnValueAsString(taskRecord, s_columnDetailedDescription));
            Priority = DatabaseBase.ParseDBEnumString<PriorityValue>(DatabaseBase.GetColumnValueAsString(taskRecord, s_columnPriority));

            int? requestorPersonId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnRequestorPersonId);
            if (requestorPersonId.HasValue)
                m_requestorPerson = Person.GetInstanceFromDatabaseId(requestorPersonId.Value);



            int? affectedComponentId = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnComponentId);
            if (affectedComponentId.HasValue)
                m_affectedComponent = Component.GetInstanceFromDatabaseId(affectedComponentId.Value);


            DateAdded = DatabaseBase.GetColumnValueAs<DateTime>(taskRecord, s_columnDateAdded).Value;

            EffortInDays = DatabaseBase.GetColumnValueAs<double>(taskRecord, s_columnEffortInDays);
            EffortType = DatabaseBase.ParseDBEnumString<EffortTypeValue>(DatabaseBase.GetColumnValueAsString(taskRecord, s_columnEffortType)) ?? EffortTypeValue.ManDays;
            m_percentageAllocation = DatabaseBase.GetColumnValueAs<double>(taskRecord, s_columnPercentageAllocation) ?? 1;
            TaskType = DatabaseBase.ParseDBEnumString<TaskTypeValue>(DatabaseBase.GetColumnValueAsString(taskRecord, s_columnTaskType));
            Status = DatabaseBase.ParseDBEnumString<StatusValue>(DatabaseBase.GetColumnValueAsString(taskRecord, s_columnStatus));

            DateTime? dbStatusDate = DatabaseBase.GetColumnValueAs<DateTime>(taskRecord, s_columnStatusDate);
            m_startRelativeDaysToProject = DatabaseBase.GetColumnValueAs<int>(taskRecord, s_columnStartRelativeDaysToProject);

            StatusDate = (dbStatusDate.HasValue) ? dbStatusDate.Value : DBAccess.DBObjectBase.DBTime.Date;
            m_owner = DatabaseBase.GetColumnValueAsString(taskRecord, s_columnOwner);
            m_private = DatabaseBase.GetColumnValueAs<bool>(taskRecord, s_columnPrivate) ?? false;

            m_resourceAssignmentIsTentative = DatabaseBase.GetColumnValueAs<bool>(taskRecord, s_columnTentativeResourceAssignment) ?? false;
        }

        protected override DBAccess.FileSystem_File FileSystemSaveObject()
        {
            DBAccess.FileSystem_File result = new DBAccess.FileSystem_File(this);

            result.Add("OrigTaskNumber", OrigTaskNumber);
            result.Add("ExternalReferenceURL", ExternalReferenceURL);

            result.Add("Priority", DatabaseBase.ToDBEnumString(Priority));

            result.Add("ProjectId", (int?)GetDatabaseIdForWritingToDatabase(Project));
            result.Add("AffectedComponentId", (int?)GetDatabaseIdForWritingToDatabase(m_affectedComponent));
            result.Add("Description", Utils.SecurityFunctions.Encrypt(Description));
            result.Add("RequestorPersonId", (int?)GetDatabaseIdForWritingToDatabase(m_requestorPerson));
            result.Add("DateAdded", DateAdded);
            result.Add("EffortInDays", EffortInDays);
            result.Add("EffortType", DatabaseBase.ToDBEnumString(EffortType));
            result.Add("PercentageAllocation", PercentageAllocation);
            result.Add("TaskType", DatabaseBase.ToDBEnumString(TaskType));
            result.Add("Status", DatabaseBase.ToDBEnumString(Status));
            result.Add("StatusDate", StatusDate);
            result.Add("StartRelativeDaysToProject", m_startRelativeDaysToProject);
            result.Add("DetailedDescription", Utils.SecurityFunctions.Encrypt(DetailedDescription));
            result.Add("Owner", Owner);
            result.Add("Private", m_private);
            result.Add("TentativeResourceAssignment", m_resourceAssignmentIsTentative);

            return result;

        }

        protected override DbCommand SaveToDataBaseSQLCommand()
        {
            SqlInsert sqlForInstert = new SqlInsert("Task", DatabaseId);
            sqlForInstert.AddParameter("OrigTaskNumber", System.Data.SqlDbType.NVarChar, 50, OrigTaskNumber);
            sqlForInstert.AddParameter("ExternalReferenceURL", System.Data.SqlDbType.NVarChar, 150, ExternalReferenceURL);
            
            sqlForInstert.AddParameter("Priority", System.Data.SqlDbType.NVarChar, 16, DatabaseBase.ToDBEnumString(Priority));

            sqlForInstert.AddParameter("ProjectId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(Project));
            sqlForInstert.AddParameter("AffectedComponentId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_affectedComponent));

            bool isPrivate = m_private || m_project.Private;
            string descriptionToSave = isPrivate ? Utils.SecurityFunctions.Encrypt(Description) : Description;

            sqlForInstert.AddParameter("Description", System.Data.SqlDbType.NVarChar, 1000, descriptionToSave);
            sqlForInstert.AddParameter("RequestorPersonId", System.Data.SqlDbType.Int, GetDatabaseIdForWritingToDatabase(m_requestorPerson));
            sqlForInstert.AddParameter("DateAdded", System.Data.SqlDbType.DateTime, DateAdded);
            sqlForInstert.AddParameter("EffortInDays", System.Data.SqlDbType.Float, EffortInDays);
            sqlForInstert.AddParameter("EffortType", System.Data.SqlDbType.NVarChar, 10, DatabaseBase.ToDBEnumString(EffortType));
            sqlForInstert.AddParameter("PercentageAllocation", System.Data.SqlDbType.Float, PercentageAllocation);
            sqlForInstert.AddParameter("TaskType", System.Data.SqlDbType.NVarChar, 50, DatabaseBase.ToDBEnumString(TaskType));
            sqlForInstert.AddParameter("Status", System.Data.SqlDbType.NVarChar, 20, DatabaseBase.ToDBEnumString(Status));
            sqlForInstert.AddParameter("StatusDate", System.Data.SqlDbType.DateTime, StatusDate);
            sqlForInstert.AddParameter("StartRelativeDaysToProject", System.Data.SqlDbType.Int, m_startRelativeDaysToProject);
            if (Utils.DatabaseBase.DBType == DatabaseBase.DBTypeValues.SQLServer)
                sqlForInstert.AddParameter("DetailedDescription", System.Data.SqlDbType.NVarChar, 1000000, Utils.SecurityFunctions.Encrypt(DetailedDescription));
            else
                sqlForInstert.AddParameter("DetailedDescription", System.Data.SqlDbType.NText, Utils.SecurityFunctions.Encrypt(DetailedDescription));
            sqlForInstert.AddParameter("Owner", System.Data.SqlDbType.NVarChar, 50, Owner);
            sqlForInstert.AddParameter("Private", System.Data.SqlDbType.Bit, m_private);
            sqlForInstert.AddParameter("TentativeResourceAssignment", System.Data.SqlDbType.Bit, m_resourceAssignmentIsTentative);

            return sqlForInstert.Command;
        }

        override public void DeleteInstance()
        {
            Internal.LinkTable_Task2Resource.DeleteInstance(this);
            Internal.LinkTable_TimeDependency.DeleteInstance(this);
            Attachment.DeleteInstance(this);
            Remark.DeleteInstance(this);
            Task.DeleteInstance(this);

            ClearTaskToProjectCache(m_project);

            DBAccess.DBObjectBase.RemoveTypeFromVisibleInstanceCache(typeof(Task));
        }



        protected override DbCommand DeleteFromDataBaseSQLCommand()
        {
            SqlDelete deleteSql = new SqlDelete("Task", DatabaseId);
            return deleteSql.Command;
        }


        //protected override string SaveToDataBaseSQLString()
        //{
        //    StringBuilder sql = new StringBuilder("exec ProjectPal.UpdateTask  ");
        //    sql.Append(DatabaseId + ",");
        //    sql.Append(Database.AsSqlString(Description) + ",");
        //    sql.Append(Database.AsSqlString(Priority) + ",");
        //    sql.Append(Database.AsSqlString(DueDate) + ",");
        //    sql.Append(Database.AsSqlString(m_requestorPersonId) + ",");
        //    sql.Append(Database.AsSqlString(m_affectedComponentId) + ",");
        //    sql.Append(Database.AsSqlString(DateAdded) + ",");
        //    sql.Append(Database.AsSqlString(EffortInDays) + ",");
        //    sql.Append(Database.AsSqlString(TaskType) + ",");
        //    sql.Append(Database.AsSqlString(Status));
        //    return sql.ToString();
        //}



        static protected void SetUpFromReader(DbDataReader tempateRecord)
        {

            s_columnTaskId = tempateRecord.GetOrdinal("TaskId");
            s_columnProjectId = tempateRecord.GetOrdinal("ProjectId");
            s_columnOrigTaskNumber = tempateRecord.GetOrdinal("OrigTaskNumber");
            s_columnPriority = tempateRecord.GetOrdinal("Priority");
            s_columnComponentId = tempateRecord.GetOrdinal("AffectedComponentId");
            s_columnDescription = tempateRecord.GetOrdinal("Description");
            s_origTaskNumber = tempateRecord.GetOrdinal("OrigTaskNumber");
            s_externalReferenceURL = tempateRecord.GetOrdinal("ExternalReferenceURL");
            s_columnDetailedDescription = tempateRecord.GetOrdinal("DetailedDescription");
            s_columnRequestorPersonId = tempateRecord.GetOrdinal("RequestorPersonId");
            s_columnDateAdded = tempateRecord.GetOrdinal("DateAdded");
            s_columnEffortInDays = tempateRecord.GetOrdinal("EffortInDays");
            s_columnEffortType = tempateRecord.GetOrdinal("EffortType");
            s_columnPercentageAllocation = tempateRecord.GetOrdinal("PercentageAllocation");
            s_columnTaskType = tempateRecord.GetOrdinal("TaskType");
            s_columnStatus = tempateRecord.GetOrdinal("Status");
            s_columnStatusDate = tempateRecord.GetOrdinal("StatusDate");
            s_columnStartRelativeDaysToProject = tempateRecord.GetOrdinal("StartRelativeDaysToProject");
            s_columnOwner = tempateRecord.GetOrdinal("Owner");
            s_columnPrivate = tempateRecord.GetOrdinal("Private");
            s_columnTentativeResourceAssignment = tempateRecord.GetOrdinal("TentativeResourceAssignment");
        }

        static bool s_setupHasBeenDone = false;

        static int s_columnTaskId;
        static int s_columnProjectId;
        static int s_columnOrigTaskNumber;
        static int s_columnPriority;
        static int s_columnStatusDate;
        static int s_columnComponentId;
        static int s_columnDescription;
        static int s_origTaskNumber;
        static int s_externalReferenceURL;
        static int s_columnDetailedDescription;
        static int s_columnRequestorPersonId;
        static int s_columnDateAdded;
        static int s_columnEffortInDays;
        static int s_columnEffortType;
        static int s_columnPercentageAllocation;
        static int s_columnTaskType;
        static int s_columnStatus;
        static int s_columnOwner;
        static int s_columnPrivate;
        static int s_columnTentativeResourceAssignment;
        static int s_columnStartRelativeDaysToProject;

    }


}
