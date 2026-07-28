using System;
using System.Collections.Generic;
using System.Text;
using DBProjectPal;
using CustomGUIControls.Grid;
using Utils;

namespace ProjectPal.Tasks
{
    class GUITaskColumns : IGridColumns
    {

        private static GUITaskColumns s_instance = null;

        private GUITaskColumns()
        { }

        public static GUITaskColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new GUITaskColumns();
                return s_instance;
            }
        }


        internal const string s_Description = "Description";
        internal const string s_DetailedDescription = "DetailedDescription";
        internal const string s_Priority = "Priority";
        internal const string s_StartDate = "StartDate";
        internal const string s_EndDate = "EndDate";
        internal const string s_RequestedBy = "RequestedBy";
        internal const string s_Resources = "Resources";
        internal const string s_TentativelyAssignedResources = "T";
        internal const string s_Projects = "Projects";
        internal const string s_AffectedComponent = "AffectedComponent";
        internal const string s_DateAdded = "DateAdded";
        internal const string s_EffortInDays = "Effort";
        internal const string s_EffortType = "EffortType";
        internal const string s_PercentageAllocation = "% Allocation";
        internal const string s_TaskType = "TaskType";
        internal const string s_Status = "Status";
        internal const string s_StatusDate = "StatusDate";
        internal const string s_Urgency = "Urgency";
        internal const string s_OrigTaskId = "OrigTaskId";
        internal const string s_Attachments = "Attachments";
        internal const string s_Remarks = "Remarks";
        internal const string s_Owner = "Owner";
        internal const string s_Id = "ID";
        internal const string s_Private = "Prvt";
        internal const string s_RefURL = "Ref URL";

        internal const string s_priortyVHigh = "5-HIGH";
        internal const string s_priortyHigh = "4-HIGH";
        internal const string s_priortyMed = "3-MED";
        internal const string s_priortyLow = "2-LOW";
        internal const string s_priortyVLow = "1-LOW";
        internal const string s_priortyCancelled = "0-Cancelled";
        internal const string s_priortyClosed = "0-Closed";

        static public string PriorityString(PriorityValue? priorityValue)
        {
            if (!priorityValue.HasValue)
                return null;

            switch (priorityValue.Value)
            {
                case DBProjectPal.PriorityValue._5_High:
                    return s_priortyVHigh;
                case DBProjectPal.PriorityValue._4_MedHigh:
                    return s_priortyHigh;
                case DBProjectPal.PriorityValue._3_Med:
                    return s_priortyMed;
                case DBProjectPal.PriorityValue._2_MedLow:
                    return s_priortyLow;
                case DBProjectPal.PriorityValue._1_Low:
                    return s_priortyVLow;
                case DBProjectPal.PriorityValue._0_Cancelled:
                    return s_priortyCancelled;
                case DBProjectPal.PriorityValue._0_Closed:
                    return s_priortyClosed;
                default:
                    return null;
            }

        }


        internal const string s_statusClosed = "Closed";
        internal const string s_statusCancelled = "Cancelled";
        internal const string s_statusInProgress = "InProgress";
        internal const string s_statusNotStarted = "NotStarted";
        internal const string s_statusReady = "Ready";
        internal const string s_statusSupport = "Support";
        internal const string s_statusTentative = "Tentative";

        internal const string s_typeEnhancement = "Enhancement";
        internal const string s_typekMaintenance = "Maintenance";
        internal const string s_typeNewDevelopment = "NewDevelopment";
        internal const string s_typeOther = "Other";
        internal const string s_typeSupport = "Support";
        internal const string s_Infrastructure = "Infrastructure";



        static List<string> s_priorityComboValues = null;
        static List<string> s_taskComboValues = null;
        static List<string> s_taskStatusValues = null;



        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();

                columns.Add(s_Id);
                columns.Add(s_Description);
                columns.Add(s_Priority);
                columns.Add(s_Urgency);
                columns.Add(s_EndDate);
                columns.Add(s_StartDate);
                columns.Add(s_RequestedBy);
                columns.Add(s_Resources);


                if (Permissions.IsSuperUser || Permissions.IsPowerUser)
                {
                    columns.Add(s_TentativelyAssignedResources);
                    columns.Add(s_Owner);
                }

                columns.Add(s_Attachments);
                columns.Add(s_Remarks);
                columns.Add(s_Projects);
                columns.Add(s_AffectedComponent);
                columns.Add(s_DateAdded);
                columns.Add(s_EffortInDays);
                columns.Add(s_EffortType);
                columns.Add(s_PercentageAllocation);
                columns.Add(s_TaskType);
                columns.Add(s_Status);
                columns.Add(s_StatusDate);
                columns.Add(s_OrigTaskId);
                columns.Add(s_Private);
                columns.Add(s_RefURL);
                columns.Add(s_DetailedDescription);


                return columns;
            }
        }

        public string ColumnFormat(string columnName)
        {
            if (columnName == s_StartDate || columnName == s_EndDate || columnName == s_DateAdded || columnName == s_StatusDate)
                return "dd-MMM-yyyy";
            if (columnName == s_PercentageAllocation)
                return "P0";
            return null;
        }

        public CustomGUIControls.Grid.GridControl.ColumnTypes ColumnType(string columnName)
        {
            if (columnName == s_Priority)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            if (columnName == s_Status)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            if (columnName == s_TaskType)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            //if (columnName == s_RequestedBy)
            //    return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            return CustomGUIControls.Grid.GridControl.ColumnTypes.Text;
        }

        public System.Windows.Forms.DataGridViewContentAlignment? ColumnAlignment(string columnName) { return null; }

        public IList<string> GetComboValues(string columnName)
        {
            return GetComboValues_static(columnName);
        }

        static public IList<string> GetComboValues_static(string columnName)
        {
            if (s_priorityComboValues == null)
            {
                s_priorityComboValues = new List<string>();
                s_priorityComboValues.Add(s_priortyVHigh);
                s_priorityComboValues.Add(s_priortyHigh);
                s_priorityComboValues.Add(s_priortyMed);
                s_priorityComboValues.Add(s_priortyLow);
                s_priorityComboValues.Add(s_priortyVLow);
                s_priorityComboValues.Add(s_priortyCancelled);
                s_priorityComboValues.Add(s_priortyClosed);
            }

            if (s_taskComboValues == null)
            {
                s_taskComboValues = new List<string>();
                s_taskComboValues.Add(s_typeEnhancement);
                s_taskComboValues.Add(s_typekMaintenance);
                s_taskComboValues.Add(s_typeNewDevelopment);
                s_taskComboValues.Add(s_typeOther);
                s_taskComboValues.Add(s_typeSupport);
                s_taskComboValues.Add(s_Infrastructure);
            }

            if (s_taskStatusValues == null)
            {
                s_taskStatusValues = new List<string>();
                s_taskStatusValues.Add(s_statusClosed);
                s_taskStatusValues.Add(s_statusCancelled);
                s_taskStatusValues.Add(s_statusInProgress);
                s_taskStatusValues.Add(s_statusNotStarted);
                s_taskStatusValues.Add(s_statusReady);
                s_taskStatusValues.Add(s_statusSupport);
                s_taskStatusValues.Add(s_statusTentative);
            }


            switch (columnName)
            {
                case s_Priority:
                    return s_priorityComboValues;
                case s_RequestedBy:
                    {
                        List<string> result = DistinctRequestedByValues;
                        return result;
                    }
                case s_AffectedComponent:
                    {
                        List<string> componentNames = new List<string>();
                        foreach (Component aComponent in DBProjectPal.Component.AllInstances)
                        {
                            componentNames.Add(aComponent.Name);
                        }
                        return componentNames;
                    }
                case s_TaskType:
                    return s_taskComboValues;
                case s_Status:
                    return s_taskStatusValues;
                case s_Resources:
                    {
                        List<string> resourceNames = new List<string>();
                        foreach (Person aPerson in DBProjectPal.Person.AllActiveInstances)
                        {
                            if (aPerson.IsResource)
                                resourceNames.Add(aPerson.Name);
                        }
                        return resourceNames;


                    }
                case s_Projects:
                    {
                        List<string> projectNames = new List<string>();
                        foreach (Project aProject in DBProjectPal.Project.AllInstances)
                        {
                            projectNames.Add(aProject.Name);
                        }
                        return projectNames;

                    }
            }

            return null;

        }


        public bool ColumnIsReadOnly(string columnName)
        {
            if (columnName == s_Urgency ||
                columnName == s_DateAdded ||
                columnName == s_StatusDate ||
                columnName == s_StartDate ||
                columnName == s_Id ||
                columnName == s_OrigTaskId )
                return true;


            if (!(Permissions.IsSuperUser || Permissions.IsPowerUser)
                &&
                (columnName == s_Owner || columnName == s_TentativelyAssignedResources))
            {
                return true;
            }

            return false;
        }

        public string MultiValueSeparator(string columnName)
        {
            if (columnName == s_Resources)
                return GUITask.s_resourceSeparator;
            return null;
        }


        public void AdjustComboEditor(string columnName, IGridItem underlyingItem,
                                    System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor,
                                        GridControl theGrid)
        {
            if (columnName == s_Status)
            {
                GUITask task = underlyingItem as GUITask;
                if (task != null)
                {
                    bool userEditIsFreelyAllowed = Permissions.IsAllowed(task.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit);

                    if (!userEditIsFreelyAllowed)
                    {
                        if (comboEditor.Items.Contains(s_statusClosed) && string.Compare(comboEditor.Text, s_statusClosed) != 0)
                            comboEditor.Items.Remove(s_statusClosed);

                        if (comboEditor.Items.Contains(s_statusCancelled) && string.Compare(comboEditor.Text, s_statusCancelled) != 0)
                            comboEditor.Items.Remove(s_statusCancelled);
                    }

                }

            }
            return;
        }



        static List<string> s_distinctRequestedByValues = null;

        static List<string> DistinctRequestedByValues
        {
            get
            {
                if (s_distinctRequestedByValues == null)
                {
                    s_distinctRequestedByValues = new List<string>();
                    foreach (Person aPerson in Person.AllActiveInstances)
                    {
                        if (!s_distinctRequestedByValues.Contains(aPerson.Name))
                            s_distinctRequestedByValues.Add(aPerson.Name);
                    }

                }
                return s_distinctRequestedByValues;
            }
        }


    }
}
