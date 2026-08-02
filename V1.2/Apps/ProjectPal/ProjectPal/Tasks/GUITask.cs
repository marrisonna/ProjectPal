using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using DBProjectPal;
using System.Collections;
using System.Windows.Forms;
using Utils;

namespace ProjectPal.Tasks
{
    public class GUITask : CustomGUIControls.Grid.IGridItem
    {

        internal DBProjectPal.Task DBTask { get { return m_dbTask; } }

        private DBProjectPal.Task m_dbTask;

        public DataGridViewCellStyle GetCellStyle(string columnName, DataGridViewCellStyle defaultStyle)
        {
            return null;
        }

        public object GetFieldValue(string columnName)
        {
            switch (columnName)
            {
                case GUITaskColumns.s_Description: return Description;
                case GUITaskColumns.s_DetailedDescription: return DetailedDescription;
                case GUITaskColumns.s_Priority: return Priority;
                case GUITaskColumns.s_StartDate: return StartDate;
                case GUITaskColumns.s_EndDate: return EndDate;
                case GUITaskColumns.s_RequestedBy: return RequestedBy;
                case GUITaskColumns.s_Resources: return ResourcesString;
                case GUITaskColumns.s_TentativelyAssignedResources: return DBTask.ResourceAssignmentIsTentative ? "Y" : "N";
                case GUITaskColumns.s_Projects: return ProjectsString;
                case GUITaskColumns.s_AffectedComponent: return AffectedComponentAsString;
                case GUITaskColumns.s_DateAdded: return DateAdded;
                case GUITaskColumns.s_EffortInDays: return EffortInDays;
                case GUITaskColumns.s_EffortType: return EffortType;
                case GUITaskColumns.s_PercentageAllocation: return PercentageAllocation;
                case GUITaskColumns.s_TaskType: return TaskType;
                case GUITaskColumns.s_Status: return Status;
                case GUITaskColumns.s_StatusDate: return StatusDate;
                case GUITaskColumns.s_Urgency: return Urgency;
                case GUITaskColumns.s_OrigTaskId: return OrigTaskId;
                case GUITaskColumns.s_Attachments: return AttachmentCount;
                case GUITaskColumns.s_Remarks: return RemarkCount;
                case GUITaskColumns.s_Owner: return Person.FindPersonFromDBLogin(Owner).Name;
                case GUITaskColumns.s_Id: return TaskId;
                case GUITaskColumns.s_Private: return Private ? "Y" : "N";
                case GUITaskColumns.s_RefURL:
                    {
                        if (ExternalReferenceURL != null)
                        {
                            string[] parts = ExternalReferenceURL.Split(new char[] { '/' });
                            if (parts[parts.Length - 1].Length > 0)
                                return parts[parts.Length - 1];
                            return ExternalReferenceURL;
                        }
                        return null;
                    }
            }
            throw new Exception("There is no column called '" + columnName + "'");
        }



        public bool PopulateDragDropDataObject(System.Windows.Forms.DataObject dragdropDataContainer)
        {
            return Permissions.IsAllowed(this.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit) &&
                EditOfParentProjectsAndComponentIsAllowed();
        }

        public bool EditOfParentProjectsAndComponentIsAllowed()
        {
            if (Permissions.IsPowerUser || Permissions.IsAllowed(DBTask.AffectedComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
            {
                Project project = DBTask.Project;
                if (project != null)
                {
                    if (!Permissions.IsAllowed(project.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                        return false;
                }
                return true;
            }
            return false;
        }


        public void SetField(string columnName, string value)
        {
            switch (columnName)
            {
                case GUITaskColumns.s_Description:
                    Description = value;
                    break;
                case GUITaskColumns.s_DetailedDescription:
                    DetailedDescription = value;
                    break;
                case GUITaskColumns.s_Priority:
                    Priority = value;
                    break;

                case GUITaskColumns.s_EndDate:
                    {


                        DateTime endDate;

                        if (DateTime.TryParse(value, out endDate))
                        {
                            m_dbTask.EndDate = endDate;
                        }
                        else
                            m_dbTask.EndDate = null;
                    }
                    break;
                case GUITaskColumns.s_RequestedBy:
                    RequestedBy = value;
                    break;
                case GUITaskColumns.s_Resources:
                    {
                        List<string> resourceNames = new List<string>();
                        if (!string.IsNullOrEmpty(value))
                        {
                            string[] values = value.Split(new char[] { ',' });
                            foreach (string name in values)
                            {
                                if (!string.IsNullOrEmpty(name))
                                    resourceNames.Add(name.Trim());
                            }
                        }
                        Resources = resourceNames;
                    }
                    break;
                case GUITaskColumns.s_Projects:
                    Project = value;
                    break;
                case GUITaskColumns.s_AffectedComponent:
                    AffectedComponentAsString = value;
                    break;
                case GUITaskColumns.s_DateAdded:
                    {
                        DateTime dateAdded;

                        if (DateTime.TryParse(value, out dateAdded))
                        {
                            m_dbTask.DateAdded = dateAdded;
                        }
                    }
                    break;
                case GUITaskColumns.s_EffortInDays:
                    {
                        if (string.IsNullOrEmpty(value))
                            m_dbTask.EffortInDays = null;
                        else
                            m_dbTask.EffortInDays = Convert.ToDouble(value);
                    }
                    break;
                case GUITaskColumns.s_EffortType:
                    EffortType = value;
                    break;
                case GUITaskColumns.s_PercentageAllocation:
                    {
                        if (string.IsNullOrEmpty(value))
                            m_dbTask.PercentageAllocation = 1;
                        else
                            m_dbTask.PercentageAllocation = Convert.ToDouble(value) / 100;
                    }
                    break;
                case GUITaskColumns.s_TaskType:
                    TaskType = value;
                    break;
                case GUITaskColumns.s_Status:
                    SetStatusDateToNow();
                    Status = value;
                    break;
                case GUITaskColumns.s_TentativelyAssignedResources:
                    SetStatusDateToNow();
                    DBTask.ResourceAssignmentIsTentative = (value.ToUpper() == "Y"); ;
                    break;
                case GUITaskColumns.s_Private:
                    SetStatusDateToNow();
                    DBTask.Private = (value.ToUpper() == "Y");
                    break;
                case GUITaskColumns.s_StatusDate:
                    {
                        DateTime statusDate;

                        if (DateTime.TryParse(value, out statusDate))
                        {
                            m_dbTask.StatusDate = statusDate;
                        }
                    }
                    break;
                case GUITaskColumns.s_Owner:
                    {
                        Person newOwner = DBProjectPal.Person.FindPerson(value);
                        if (newOwner == null || string.IsNullOrEmpty(newOwner.DBLogin))
                            return;
                        string user = newOwner.DBLogin.Trim();

                        if (m_dbTask.Owner != user)
                            m_dbTask.Owner = user;
                    }
                    break;
            }
            SetStatusDateToNow();
        }



        public Color Colour
        {
            get
            {
                try
                {
                    PriorityValue? taskPriority = m_dbTask.Priority;
                    if (!taskPriority.HasValue ||
                         taskPriority.Value == PriorityValue._0_Cancelled ||
                         taskPriority.Value == PriorityValue._0_Closed)
                        return Utils.Colours.ReadOnlyColour;

                 

                    return Utils.Colours.UrgencyColour(Urgency);
                }
                catch (Exception err)
                {
                    Utils.Logger.LogException(err, "Error setting Colour for GUITask");

                    return Utils.Colours.ReadWriteColour;
                }
            }
        }


        ///////////////////////

        //static public GUITask CreateNewInstance()
        //{
        //    return new GUITask();
        //}

        static public GUITask GetInstanceFromDBTask(DBProjectPal.Task dbTask)
        {
            GUITask result = null;
            if (!m_instances.TryGetValue(dbTask, out result))
            {
                result = new GUITask(dbTask);
            }
            return result;
        }

        static public GUITask GetExistingInstanceFromDBTask(DBProjectPal.Task dbTask)
        {
            GUITask result = null;
            m_instances.TryGetValue(dbTask, out result);
            return result;
        }

        static Dictionary<DBProjectPal.Task, GUITask> m_instances = new Dictionary<DBProjectPal.Task, GUITask>();

        private GUITask(DBProjectPal.Task dbTask)
        {
            m_displayItem = new CustomGUIControls.DisplayItemImpl(this);

            m_dbTask = dbTask;

            m_instances.Add(dbTask, this);
        }



        public int AttachmentCount
        {
            get
            {
                return m_dbTask.Attachments.Count;
            }
        }

        public int RemarkCount
        {
            get
            {
                return m_dbTask.Remarks.Count;
            }
        }


        private static bool m_urgencyCachingEnabled = false;
        private double? m_urgencyCache = null;

        public static bool UrgencyCachingEnabled
        {
            get { return m_urgencyCachingEnabled; }
            set
            {
                if (m_urgencyCachingEnabled == true && value == false)
                {
                    foreach (GUITask currentTask in m_instances.Values)
                    {
                        currentTask.m_urgencyCache = null;
                    }
                }

                m_urgencyCachingEnabled = value;
            }
        }


        public double Urgency
        {
            get
            {
                if (m_urgencyCachingEnabled && m_urgencyCache.HasValue)
                    return m_urgencyCache.Value;

                DateTime today = DateTime.Now.Date;


                double result = -1;

                if (m_dbTask.Status == StatusValue.Cancelled ||
                    m_dbTask.Status == StatusValue.Closed)
                {
                    //Urgency for Closed / Cancelled Tasks
                    if (StatusDate.HasValue && (today - StatusDate.Value).Days > 10)
                    {
                        result = ((int)(100.0 / (today - StatusDate.Value).Days)) / 10.0;// Closed a while ago
                    }
                    else
                        result = 1; // Recently closed
                }
                else
                {

                    DateTime? minProjectDueDate = null;
                    double finalTaskPriority = 0;

                    List<PriorityValue> parentProjectPriorityList = new List<PriorityValue>();
                    Project currentParent = m_dbTask.Project;
                    while (currentParent != null)
                    {
                        if (currentParent.DueDate.HasValue &&
                            (minProjectDueDate == null || currentParent.DueDate < minProjectDueDate))
                            minProjectDueDate = currentParent.DueDate;

                        parentProjectPriorityList.Add(currentParent.Priority ?? PriorityValue._3_Med);
                        currentParent = currentParent.Parent;
                    }
                    parentProjectPriorityList.Reverse();

                    double max = parentProjectPriorityList.Count == 0 ? 0.5 :
                                DBProjectPal.Enums.PriorityValueAsInt(parentProjectPriorityList[0]) + 0.5;
                    double min = Math.Max(0, max - 1);


                    for (int i = 1; i < parentProjectPriorityList.Count; i++)
                    {
                        double thisPriorityMax = DBProjectPal.Enums.PriorityValueAsInt(parentProjectPriorityList[i]) + 0.5;
                        double thisPriorityMin = Math.Max(0, thisPriorityMax - 1);

                        thisPriorityMax /= 6; // 6 = mid priority * 2;
                        thisPriorityMin /= 6; // 6 = mid priority * 2;

                        double thisMean = (thisPriorityMax + thisPriorityMin);// / 2.0  *2.0; Med = 1
                        const double exagerateFactor = 1.5;
                        double thisExagerate = Math.Pow(thisMean, exagerateFactor);

                        double newMax = (min + thisExagerate * thisPriorityMax * (max - min));
                        double newMin = (min + thisExagerate * thisPriorityMin * (max - min));

                        max = newMax;
                        min = newMin;
                    }
                    double thisTaskPriority = 3;
                    if (m_dbTask.Priority.HasValue)
                        thisTaskPriority = DBProjectPal.Enums.PriorityValueAsInt(m_dbTask.Priority.Value);

                    //thisTaskPriority = (thisTaskPriority - 1) / 4;
                    //thisTaskPriority = (thisTaskPriority - 2) / 2;
                    double thisTaskFactor = thisTaskPriority / 3;

                    //finalTaskPriority = min + thisTaskPriority * (max - min);
                    finalTaskPriority = thisTaskFactor * (min + max) / 2.0;


                    DateTime? taskDate = null;
                    if (m_dbTask.StartDate != null)
                    {
                        if (m_dbTask.Status == StatusValue.NotStarted || m_dbTask.EndDate == null)
                        {
                            taskDate = m_dbTask.StartDate;
                        }
                        else if (m_dbTask.Status.Value == StatusValue.InProgress)
                        {
                            taskDate = m_dbTask.StartDate.Value.AddDays((m_dbTask.EndDate.Value - m_dbTask.StartDate.Value).Days / 2);
                        }
                        else
                        {
                            taskDate = m_dbTask.EndDate;
                        }
                    }

                    double taskPriorityMultiplier = (finalTaskPriority - 3) / 3 + 1;

                    //double taskPriorityExagerateFactor = 2.0; 
                    //taskPriorityMultiplier *= taskPriorityExagerateFactor;


                    if (taskDate == null)
                    {
                        result = 100 * taskPriorityMultiplier;
                    }
                    else
                    {
                        int daysUntilDue = (taskDate.Value - today).Days;

                        if (daysUntilDue <= 0)
                        {
                            double lateMultiplier = 1.0 - daysUntilDue / 60.0;
                            result = 100 * taskPriorityMultiplier * lateMultiplier;
                        }
                        else
                        {
                            double earlyMultiplier = Math.Pow(0.5, (daysUntilDue / 60.0));

                            result = 100 * taskPriorityMultiplier * earlyMultiplier;

                        }

                    }
                }

                m_urgencyCache = ((int)(result * 10)) / 10.0;

                return m_urgencyCache.Value;

            }
        }


        public double Urgency_orig
        {
            get
            {
                if (m_urgencyCachingEnabled && m_urgencyCache.HasValue)
                    return m_urgencyCache.Value;

                double result = -1;
                DateTime today = DateTime.Now.Date;

                PriorityValue thisPriority = PriorityValue._3_Med;
                if (m_dbTask.Priority.HasValue)
                    thisPriority = m_dbTask.Priority.Value;

                int N = DBProjectPal.Enums.PriorityValueAsInt(thisPriority);
                if (N > 0)
                {
                    List<PriorityValue?> thisPrioritiesList = new List<PriorityValue?>();
                    thisPrioritiesList.Add(m_dbTask.Priority);

                    double thisPriorityFnVal = Functions.PriorityValue(thisPrioritiesList);
                    double maxPriority = 0;
                    DateTime? minDueDate = null;
                    Project p = m_dbTask.Project;
                    if (p != null)
                    {
                        if (!(p.Priority == PriorityValue._0_Cancelled ||
                              p.Priority == PriorityValue._0_Closed))
                        {
                            if (p.DueDate.HasValue && (!minDueDate.HasValue || p.DueDate.Value < minDueDate.Value))
                                minDueDate = p.DueDate.Value;
                            List<PriorityValue?> priorities = p.PriorityList;
                            priorities.Add(m_dbTask.Priority);
                            double priority = Functions.PriorityValue(priorities);
                            if (priority > maxPriority)
                            {
                                maxPriority = priority;
                            }
                        }
                    }
                    if (maxPriority == 0)
                        maxPriority = thisPriorityFnVal;
                    double dueDateMultiplier = 1;
                    if (minDueDate.HasValue)
                    {
                        // 'cutOfPeriod' days to go = 1, 
                        // 'cutOfPeriod' days late = maxLateValue, 0 days = (maxLateValue+1)/2

                        const double maxLateValue = 2;
                        const int cutOfPeriod = 30; // days
                        int projectDueDateDiffOrig = (minDueDate.Value - DateTime.Now.Date).Days;
                        int projectDueDateDiff = Math.Max(projectDueDateDiffOrig, -cutOfPeriod);
                        //projectDueDateDiff = Math.Min(projectDueDateDiff, 30); 
                        // If more than 'cutOfPeriod' days to go
                        if (projectDueDateDiff > cutOfPeriod)
                            dueDateMultiplier = Math.Max(0.5, (356 - (projectDueDateDiff - cutOfPeriod)) / 365);
                        else
                            dueDateMultiplier = 1 + (maxLateValue - 1) * (cutOfPeriod - projectDueDateDiff) / (2 * cutOfPeriod);
                    }

                    double dateAddOn = 0;
                    if (EndDate != null)
                    {
                        int daysUntilDue = (EndDate.Value.Date - today).Days;
                        if (daysUntilDue < 30)
                            dateAddOn = -daysUntilDue;

                        if (daysUntilDue > 30)
                        {
                            dateAddOn = -30;
                            dueDateMultiplier *= Math.Max(0.1, (1 - Math.Pow((daysUntilDue - 30.0) / 90.0, 1)));
                        }

                    }


                    if (maxPriority <= 0)
                    {
                        if (m_dbTask.Priority.HasValue)
                            result = dueDateMultiplier * (1 + (int)m_dbTask.Priority.Value);// If the project priority is zero, then task priority is low
                        else
                            result = dueDateMultiplier * 1;
                    }
                    else
                        result = dueDateMultiplier * (dateAddOn + maxPriority);

                    if (result < 1.1)
                        result = 1.1;
                }
                else
                {
                    //Urgency for Closed / Cancelled Tasks
                    if (StatusDate.HasValue && (today - StatusDate.Value).Days > 10)
                    {
                        result = ((int)(100.0 / (today - StatusDate.Value).Days)) / 10.0;// Closed a while ago
                    }
                    else
                        result = 1; // Recently closed
                }

                m_urgencyCache = ((int)(result * 10)) / 10.0;

                return m_urgencyCache.Value;
            }
        }

        public string OrigTaskId
        {
            get { return m_dbTask.OrigTaskNumber; }
            set { }
        }

        public string ExternalReferenceURL
        {
            get { return m_dbTask.ExternalReferenceURL; }
            set { m_dbTask.ExternalReferenceURL = value; SetStatusDateToNow(); }
        }
        

        public string ObjectDescription { get { return m_dbTask.ObjectDescription; } }

        public string Description
        {
            get { return m_dbTask.Description; }
            set { m_dbTask.Description = value; SetStatusDateToNow(); }
        }

        public string DetailedDescription
        {
            get { return m_dbTask.DetailedDescription; }
            set { m_dbTask.DetailedDescription = value; SetStatusDateToNow(); }
        }

        public bool Private
        {
            get { return m_dbTask.Private; }
            set { m_dbTask.Private = value; SetStatusDateToNow(); }
        }


        public string Priority
        {
            get
            {
                return GUITaskColumns.PriorityString(m_dbTask.Priority);
            }
            set
            {
                SetStatusDateToNow();
                m_dbTask.Priority = null;
                switch (value)
                {

                    case GUITaskColumns.s_priortyVHigh:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._5_High;
                        break;
                    case GUITaskColumns.s_priortyHigh:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._4_MedHigh;
                        break;
                    case GUITaskColumns.s_priortyMed:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._3_Med;
                        break;
                    case GUITaskColumns.s_priortyLow:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._2_MedLow;
                        break;
                    case GUITaskColumns.s_priortyVLow:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._1_Low;
                        break;
                    case GUITaskColumns.s_priortyCancelled:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._0_Cancelled;
                        break;
                    case GUITaskColumns.s_priortyClosed:
                        m_dbTask.Priority = DBProjectPal.PriorityValue._0_Closed;
                        break;
                    default:
                        m_dbTask.Priority = null;
                        break;
                }
                SetStatusDateToNow();
            }

        }
        public DateTime? StartDate
        {
            get { return m_dbTask.StartDate; }

        }

        public DateTime? EndDate
        {
            get
            {
                return m_dbTask.EndDate;
            }
            set { m_dbTask.EndDate = value; SetStatusDateToNow(); }

        }

        public DateTime? StatusDate
        {
            get { return m_dbTask.StatusDate; }

        }

        public string Owner { get { return m_dbTask.Owner; } set { m_dbTask.Owner = value; } }


        public int? TaskId
        {
            get { return m_dbTask.TaskId; }

        }

        private void SetStatusDateToNow()
        {
            if (m_dbTask.Status != StatusValue.Cancelled && m_dbTask.Status != StatusValue.Closed)
                m_dbTask.StatusDate = DBAccess.DBObjectBase.DBTime;
        }

        public string RequestedBy
        {
            get { return m_dbTask.RequestedBy == null ? null : m_dbTask.RequestedBy.Name; }
            set
            {
                Person thePerson = Person.FindPerson(value);
                if (thePerson == null && !string.IsNullOrEmpty(value))
                {
                    thePerson = Person.AddNewInstance(value);
                }
                m_dbTask.RequestedBy = thePerson;
                SetStatusDateToNow();

            }

        }

        internal const string s_resourceSeparator = ", ";

        public string ResourcesString
        {
            get
            {
                string resourcesDescription = "";
                bool firstTime = true;
                foreach (DBProjectPal.IResource resource in m_dbTask.Resources)
                {
                    if (!firstTime)
                        resourcesDescription += s_resourceSeparator;
                    firstTime = false;
                    resourcesDescription += resource.Name;
                }
                return resourcesDescription;
            }
        }

        public IList<string> Resources
        {
            get
            {
                List<string> resources = new List<string>();

                foreach (DBProjectPal.IResource resource in m_dbTask.Resources)
                {
                    resources.Add(resource.Name);
                }
                return resources;
            }
            set
            {
                IEnumerable<IResource> currentResources = m_dbTask.Resources;

                // Find resource to add
                List<IResource> resourcesToAdd = new List<IResource>();
                foreach (string personName in value)
                {
                    bool found = false;
                    foreach (IResource currentResource in currentResources)
                    {
                        if (currentResource.Name == personName &&
                            currentResource.GetType() == typeof(Person))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Person newPerson = Person.FindPerson(personName);
                        if (newPerson != null)
                            resourcesToAdd.Add(newPerson);
                    }
                }

                // Find resource to remove
                List<IResource> resourcesToRemove = new List<IResource>();
                foreach (IResource currentResource in currentResources)
                {
                    if (!value.Contains(currentResource.Name))
                        resourcesToRemove.Add(currentResource);
                }

                foreach (IResource resourceToRemove in resourcesToRemove)
                {
                    m_dbTask.ResourceRemove(resourceToRemove);
                }

                foreach (IResource resourceToAdd in resourcesToAdd)
                {
                    m_dbTask.ResourceAdd(resourceToAdd);
                }

                SetStatusDateToNow();
            }
        }

        public string ProjectsString
        {
            get
            {
                string projectDescription = "";
                DBProjectPal.Project project = m_dbTask.Project;
                if (project != null)
                {
                    projectDescription += project.FullName;
                }
                return projectDescription;
            }
        }


        public IEnumerable<Attachment> Attachments
        {
            get
            {
                return m_dbTask.Attachments;
            }
        }


        public IEnumerable<Remark> Remarks
        {
            get
            {
                return m_dbTask.Remarks;
            }
        }

        public string Project
        {
            get
            {
                DBProjectPal.Project project = m_dbTask.Project;
                if (project != null)
                    return project.FullName;

                return "";
            }
            set
            {

                Project newProject = DBProjectPal.Project.FindProject(value);

                if (newProject != null)
                {
                    m_dbTask.Project = newProject;
                }

                SetStatusDateToNow();
            }
        }


        public string AffectedComponentAsString
        {
            get { return m_dbTask.AffectedComponent == null ? null : m_dbTask.AffectedComponent.FullName; }
            set
            {
                Component theComponent = Component.FindComponent(value);
                if (theComponent == null && !string.IsNullOrEmpty(value))
                {
                    // Need a parent to add a component
                }
                m_dbTask.AffectedComponent = theComponent;
                SetStatusDateToNow();
            }
        }

        public Component AffectedComponent
        {
            get { return m_dbTask.AffectedComponent; }
            set
            {
                m_dbTask.AffectedComponent = value;
                SetStatusDateToNow();
            }
        }
        public DateTime DateAdded
        {
            get { return m_dbTask.DateAdded; }
        }
        public double? EffortInDays
        {
            get { return m_dbTask.EffortInDays; }
            set { m_dbTask.EffortInDays = value; SetStatusDateToNow(); }

        }
        public string EffortType
        {
            get
            {
                return m_dbTask.EffortType.ToString();
            }
            set
            {
                SetStatusDateToNow();

                m_dbTask.EffortType = (DBProjectPal.EffortTypeValue)Enum.Parse(typeof(DBProjectPal.EffortTypeValue), value);
            }

        }
        public double PercentageAllocation
        {
            get { return m_dbTask.PercentageAllocation; }
            set { m_dbTask.PercentageAllocation = value; SetStatusDateToNow(); }
        }

        public string TaskType
        {
            get
            {
                if (m_dbTask.TaskType.HasValue)
                    return m_dbTask.TaskType.Value.ToString();
                return null;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    m_dbTask.TaskType = null;
                else
                    m_dbTask.TaskType = (TaskTypeValue?)Enum.Parse(typeof(TaskTypeValue), value);
                SetStatusDateToNow();
            }
        }
        public string Status
        {
            get
            {
                if (m_dbTask.Status.HasValue)
                    return m_dbTask.Status.Value.ToString();
                return null;

            }
            set
            {
                SetStatusDateToNow();
                if (string.IsNullOrEmpty(value))
                    m_dbTask.Status = null;
                else
                    try
                    {
                        StatusValue? newStatus = (StatusValue?)Enum.Parse(typeof(StatusValue), value);
                        bool userEditIsFreelyAllowed = Permissions.IsAllowed(m_dbTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit);

                        if (((newStatus == StatusValue.Closed || newStatus == StatusValue.Cancelled) && !userEditIsFreelyAllowed) ||
                            (!userEditIsFreelyAllowed && !m_dbTask.Resources.Contains(Person.CurrentUser)))
                        {
                            // Ignore the change
                            Functions.ClearDisplayCaches();
                            Redisplay();
                        }
                        else
                        {
                            m_dbTask.Status = newStatus;
                        }
                    }
                    catch (Exception)
                    { }
                SetStatusDateToNow();
            }
        }

        public void AddView(CustomGUIControls.IView view)
        {
            m_displayItem.AddView(view);
        }

        public void RemoveView(CustomGUIControls.IView view)
        {
            m_displayItem.RemoveView(view);
        }

        public void DisplayItemDeleted()
        {
            m_displayItem.DisplayItemDeleted();
        }


        public void Redisplay()
        {

            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            m_displayItem.Redisplay();

            ApplicationProjectPal.Instance.RefreshAllWindows();

        }


        static public void Redisplay(DBProjectPal.Task task)
        {
            GUITask guiTask = null;
            if (m_instances.TryGetValue(task, out guiTask))
            {
                guiTask.Redisplay();

            }
        }

        CustomGUIControls.DisplayItemImpl m_displayItem = null;

        public void DeleteInstance()
        {
            DisplayItemDeleted();
            m_dbTask.DeleteInstance();
        }

        static public void DeleteTask(CustomGUIControls.IDisplayItem task)
        {
            ProjectPal.Tasks.GUITask guiTask = task as ProjectPal.Tasks.GUITask;
            if (guiTask != null)
            {
                guiTask.DeleteInstance();
            }
        }

        static public bool ConfirmDeleteTask(CustomGUIControls.IDisplayItem task)
        {
            ProjectPal.Tasks.GUITask guiTask = task as ProjectPal.Tasks.GUITask;
            if (guiTask != null && Permissions.IsAllowed(guiTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Delete))
            {
                if (System.Windows.MessageBox.Show("Are you sure you want to delete the Task?", "Delete Task",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                    == System.Windows.MessageBoxResult.Yes)
                {
                    return true;

                }

            }
            return false;
        }

        public bool IsReadOnly(string columnName)
        {
            if (Utils.Permissions.IsAllowed(this.Owner, Utils.Permissions.EntityType.Task, Utils.Permissions.ChangeType.Edit))
            {
                return false;
            }
            if (Utils.Permissions.IsReadOnly)
                return true;

            return !(NormalUserEditableColumns.Contains(columnName) && DBTask.Resources.Contains(Person.CurrentUser));


        }

        public bool IsActive()
        {
            if (DBTask.Status == StatusValue.Cancelled ||
                DBTask.Status == StatusValue.Closed)
                return false;
            return true;
        }

        IList<string> NormalUserEditableColumns
        {
            get
            {
                List<string> result = new List<string>();
                result.Add(GUITaskColumns.s_DetailedDescription);
                result.Add(GUITaskColumns.s_Status);
                return result;
            }
        }


        public bool IsDeleted { get { return m_dbTask.IsDeleted; } }


        public bool IsPrivateToOtherUser { get { return m_dbTask.IsPrivateToAnotherAndHidden; } }



        public void GridCellDragEnter(DragEventArgs e)
        {
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;

            if (draggedTask != null)
            {
                if (Permissions.IsAllowed(draggedTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                {
                    if ((e.AllowedEffect | DragDropEffects.Link) != 0)
                        e.Effect = DragDropEffects.Link;
                }
            }
        }



        public void GridCellDragDrop(DragEventArgs e)
        {
            if ((e.AllowedEffect | DragDropEffects.Link) != 0)
            {
                object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

                ProjectPal.Tasks.GUITask draggedTask = draggedObject as ProjectPal.Tasks.GUITask;


                if (draggedTask != null)
                {
                    DBProjectPal.Task firstTask = DBTask;
                    DBProjectPal.Task secondTask = draggedTask.DBTask;
                    if (firstTask == secondTask)
                        return;

                    if (secondTask.HasPostDependency(firstTask))
                    {
                        MessageBox.Show("Circular dependencies are not allowed", "Circular Dependency", MessageBoxButtons.OK);
                    }
                    else
                    {
                        firstTask.AddPostDependency(secondTask);
                        Functions.ClearDisplayCaches();
                        ApplicationProjectPal.Instance.RefreshAllWindows();
                    }
                }
            }
        }
        public void GridCellDragLeave(EventArgs e) { return; }
    }
}
