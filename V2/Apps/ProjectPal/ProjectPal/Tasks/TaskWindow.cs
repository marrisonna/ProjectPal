using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Utils;

namespace ProjectPal.Tasks
{
    public partial class TaskWindow : Form
    {
        private IList<CustomGUIControls.Grid.IGridItem> m_tasks;

        private static HashSet<TaskWindow> s_allInstances = new HashSet<TaskWindow>();


        public TaskWindow(IList<CustomGUIControls.Grid.IGridItem> tasksToDisplay, List<string> resourceFilter)
        {
            InitializeComponent();

            gridControl.FilterIsVisible = true;

            List<string> columnOrder = new List<string>();
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Id);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Urgency);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Resources);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Status);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_TentativelyAssignedResources);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Description);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_AffectedComponent);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Projects);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Priority);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_EndDate);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_StartDate);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Attachments);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Remarks);
            columnOrder.Add(ProjectPal.Tasks.GUITaskColumns.s_Owner);

            gridControl.SetColumnOrder(columnOrder);


            m_tasks = tasksToDisplay;

            //gridControl.AddColumn("OrigTaskNumber");
            /*gridControl.AddColumn("Description");
            gridControl.AddColumn("Priority");
            gridControl.AddColumn("DueDate", "dd-MMM-yyyy");
            gridControl.AddColumn("RequestedBy");
            gridControl.AddColumn("Resources");
            gridControl.AddColumn("AffectedComponent");
            gridControl.AddColumn("DateAdded", "dd-MMM-yyyy");
            gridControl.AddColumn("EffortInDays");
            gridControl.AddColumn("TaskType");
            gridControl.AddColumn("Status");
            gridControl.AddColumn("Projects");*/


            PopulateTasks();
            gridControl.SetCellDeleteFunction(ProjectPal.Tasks.GUITask.DeleteTask);
            gridControl.SetCheckCellDeleteFunction(ProjectPal.Tasks.GUITask.ConfirmDeleteTask);


            List<string> StatusFilterValues = new List<string>();
            StatusFilterValues.Add(GUITaskColumns.s_statusInProgress);
            StatusFilterValues.Add(GUITaskColumns.s_statusNotStarted);
            StatusFilterValues.Add(GUITaskColumns.s_statusReady);
            StatusFilterValues.Add(GUITaskColumns.s_statusSupport);
            StatusFilterValues.Add(GUITaskColumns.s_statusTentative);

            gridControl.SetDefaultFilterValues(ProjectPal.Tasks.GUITaskColumns.s_Status, StatusFilterValues);

            //if (!Permissions.IsSuperUser)
            if (resourceFilter != null)
            {
                gridControl.SetDefaultFilterValues(ProjectPal.Tasks.GUITaskColumns.s_Resources, resourceFilter);
            }

            s_allInstances.Add(this);
        }


        void PopulateTasks()
        {
            gridControl.AllowCellDrop = true;
            gridControl.SetColumns(GUITaskColumns.Instance);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Id, 60);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Description, 300);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Priority, 90);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Urgency, 65);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Status, 85);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_EndDate, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_StartDate, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_EffortInDays, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_PercentageAllocation, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_RequestedBy, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Resources, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_TentativelyAssignedResources, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Private, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_RefURL, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Attachments, 50);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Remarks, 50);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Owner, 50);

            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_AffectedComponent, 200);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Projects, 200);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_DetailedDescription, 200);

            gridControl.StopLayout();

            gridControl.AddRows(m_tasks.Count);

            foreach (CustomGUIControls.Grid.IGridItem task in m_tasks)
            {
                gridControl.AddDisplayItem(task);
                /*
                int rowNumber = gridControl.AddRow(task);
                gridControl.SetValue(rowNumber, "Description", task.Description);
                gridControl.SetValue(rowNumber, "Priority", task.Priority);
                gridControl.SetValue(rowNumber, "DueDate", task.DueDate);
                gridControl.SetValue(rowNumber, "RequestedBy", task.RequestedBy);
                gridControl.SetValue(rowNumber, "Resources", task.Resources);
                gridControl.SetValue(rowNumber, "AffectedComponent", task.AffectedComponent);
                gridControl.SetValue(rowNumber, "DateAdded", task.DateAdded);

                double? effort = task.EffortInDays;
                gridControl.SetValue(rowNumber, "EffortInDays", effort.HasValue?effort.ToString():null);
                gridControl.SetValue(rowNumber, "TaskType", task.TaskType);
                gridControl.SetValue(rowNumber, "Status", task.Status);
                gridControl.SetValue(rowNumber, "Projects", task.Projects);*/
            }
            gridControl.StartLayout();
            gridControl.LogPerformance();

            gridControl.SetColumnFilterSortFn(Tasks.GUITaskColumns.s_Resources, ResourceSort);


            gridControl.SetDefaultSort(Tasks.GUITaskColumns.s_Urgency, ListSortDirection.Descending);

            gridControl.SetFilters();

        }

        private int ResourceSort(object a, object b)
        {
            DBProjectPal.Person personA = DBProjectPal.Person.FindPerson(a as string);
            DBProjectPal.Person personB = DBProjectPal.Person.FindPerson(b as string);

            if (personA == null && personB != null && personB.IsResource)
                return 1;
            if (personB == null && personA != null && personA.IsResource)
                return -1;

            if (personA == null && personB != null)
                return -1;
            if (personB == null && personA != null)
                return 1;

            if (personA == null && personB == null)
                return string.Compare(a as string, b as string);

            if (personA.IsResource && !personB.IsResource)
                return -1;
            if (!personA.IsResource && personB.IsResource)
                return 1;



            return string.Compare(personA.Name, personB.Name);
        }


        private void buttonSave_Click(object sender, EventArgs e)
        {
            ApplicationProjectPal.Instance.SaveToDataBase();
        }

        public void SetGridDoubleClickFunction(CustomGUIControls.Grid.GridControl.CellDoubleClick doubleClickFunction)
        {
            gridControl.SetDoubleClickFunction(doubleClickFunction);
        }

        private void TaskWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            gridControl.WindowClosed();
            s_allInstances.Remove(this);
        }


        private bool m_ganttDisplayed = false;

        private void tabPagePlanDisplay_Enter(object sender, EventArgs e)
        {
            Functions.ClearDisplayCaches();
            RedisplayGantt();
            m_ganttDisplayed = true;
        }

        public static void RedisplayAll()
        {


            foreach (TaskWindow currentWindow in s_allInstances)
                currentWindow.Redisplay();
        }






        PlanDisplay.PlanControl m_thePlanControl = null;

        private void Redisplay()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            gridControl.Redisplay();
            if (m_ganttDisplayed)
                RedisplayGantt();
        }

        private void RedisplayGantt()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this, "Gantt"))
                return;

            bool newPlanControl = false;

            if (m_thePlanControl == null)
            {
                newPlanControl = true;
                m_thePlanControl = new PlanDisplay.PlanControl();
            }

            List<DBProjectPal.Task> tasks = new List<DBProjectPal.Task>();
            foreach (CustomGUIControls.IDisplayItem item in gridControl.VisibleItems)
            {
                GUITask currentGUITask = item as GUITask;
                if (currentGUITask != null)
                {
                    DBProjectPal.Task currentTask = currentGUITask.DBTask;
                    tasks.Add(currentTask);
                }
            }

            HashSet<DBProjectPal.Task> topLevelProjectTasksIfNeeded = new HashSet<DBProjectPal.Task>();

            PlanDisplay.Project displayProject = new PlanDisplay.Project(null, m_thePlanControl, null, PlanDisplay.Project.EventType.None);

            foreach (DBProjectPal.Task currentTask in tasks)
            {
                if (currentTask.Status == DBProjectPal.StatusValue.Cancelled ||
                    currentTask.Status == DBProjectPal.StatusValue.Closed)
                    continue;
                topLevelProjectTasksIfNeeded.Add(currentTask);
            }

            GanttDisplayHelper helper = new GanttDisplayHelper();

            displayProject.SetEventFunction(helper.HandleEvent);

            HashSet<string> allDistinctResources = GanttDisplayHelper.AddPlanDetail(m_thePlanControl,
                                                    displayProject, topLevelProjectTasksIfNeeded, (IEnumerable<DBProjectPal.Project>)null,
                                                    null, null);

            if (newPlanControl)
            {
                helper.SetPlanControl(m_thePlanControl);


                displayProject.SetDateLine(DateTime.Now.Date);

                m_thePlanControl.RedisplayProject(displayProject, allDistinctResources, ProjectPal.GanttDisplayHelper.ResourceColorMap);
                m_thePlanControl.ReScaleToAreaData(this.elementHostArea.Width, this.elementHostArea.Height);
                elementHostArea.Child = m_thePlanControl;
            }
            else
            {
                helper.SetPlanControl(m_thePlanControl);
                m_thePlanControl.RedisplayProject(displayProject, allDistinctResources, ProjectPal.GanttDisplayHelper.ResourceColorMap);
            }



        }

        private void tabPageGanttDisplay_Leave(object sender, EventArgs e)
        {
            m_ganttDisplayed = false;
        }


    }
}
