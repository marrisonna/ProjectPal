using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBProjectPal;
using System.Reflection;
using Utils;

namespace ProjectPal
{
    public partial class MainWindow : Form
    {

        private static MainWindow m_instance;

        public MainWindow()
        {
            m_instance = this;
            InitializeComponent();

            if (Permissions.ThisUserType != Permissions.UserLevel.ReadOnlyUser)
                buttonSave.Enabled = true;
            else
                buttonSave.Enabled = false;


            toolStripVersion.Text = "About";
            ToolStripMenuItemCopyright.Text = "(C) Copyright 2010-2018 Neil Marrison.  All rights reserved";
            ToolStripMenuItemVersion.Text = "Ver " + ConfigSettings.Instance.AppVersionShort +
           " @ " + Utils.Misc.BuildDateTime.ToString("dd MMM yyyy  HH:mm:ss");
            toolStripStatusLabelUserRole.Text = Permissions.ThisUserType.ToString();

            SetUpReport();
            ShowReport();

            m_refreshTimer = new System.Windows.Threading.DispatcherTimer();
            m_refreshTimer.Interval = new TimeSpan(0, 0, 10);
            m_refreshTimer.Tick += new EventHandler(RefreshTimer_Tick);
            m_refreshTimer.Start();

        }

        private void SetUpReport()
        {
            gridControl.FilterIsVisible = true;

            List<string> columnOrder = new List<string>();
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_Person);
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_NumberOfOpenTasks);
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_NumberOfReadyTasks);
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_NumberOfInProgressTasks);
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_Effort);
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_AveUrgency);
            columnOrder.Add(ProjectPal.Reports.GUIMainReportItemColumns.s_MaxUrgency);

            gridControl.SetColumnOrder(columnOrder);

            gridControl.SetColumns(ProjectPal.Reports.GUIMainReportItemColumns.Instance);

            gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_Person, 80);
            gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_NumberOfOpenTasks, 70);
            gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_NumberOfReadyTasks, 70);
            gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_NumberOfInProgressTasks, 70);
            gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_Effort, 90);
            gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_AveUrgency, 90);
            //gridControl.ColumnWidth(ProjectPal.Reports.GUIMainReportItemColumns.s_MaxUrgency, 80);

            gridControl.FilterIsVisible = false;

            gridControl.SetDoubleClickFunction(DoubleClickRow);


        }


        public static void RedisplayAll()
        {
            m_instance.ShowReport();
        }

        private const string TotalRowLabel = "Total";

        private void ShowReport()
        {
            gridControl.ClearRows();

            SortedDictionary<string, ProjectPal.Reports.GUIMainReportItem> reportItems = new SortedDictionary<string, ProjectPal.Reports.GUIMainReportItem>();

            ProjectPal.Reports.GUIMainReportItem total = new Reports.GUIMainReportItem(TotalRowLabel);

            foreach (DBProjectPal.Task task in DBProjectPal.Task.AllInstances)
            {
                if (task.Status == StatusValue.Cancelled ||
                    task.Status == StatusValue.Closed)
                    continue;
                int resourcesCount = task.TeamResources.Count;
                double effortPerResource = resourcesCount == 0 ? 0 : ((task.EffortInDays ?? 0) / resourcesCount);
                if (task.EffortType == EffortTypeValue.Duration)
                    effortPerResource *= task.PercentageAllocation;
                ProjectPal.Tasks.GUITask guiTask = ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(task);
                double urgency = guiTask.Urgency;
                foreach (IResource person in task.TeamResources)
                {
                    ProjectPal.Reports.GUIMainReportItem reportItem;
                    if (!reportItems.TryGetValue(person.Name, out reportItem))
                    {
                        reportItems.Add(person.Name, reportItem = new Reports.GUIMainReportItem(person.Name));
                    }
                    reportItem.AddTaskData(effortPerResource, urgency, task.Status);
                }

                total.AddTaskData(effortPerResource * resourcesCount, urgency, task.Status);
            }

            //gridControl.AddDisplayItem(total);
            List<ProjectPal.Reports.GUIMainReportItem> sortedReportItems =
                new List<Reports.GUIMainReportItem>(reportItems.Values);
            sortedReportItems.Add(total);

            sortedReportItems.Sort(ReportItemSort);

            foreach (ProjectPal.Reports.GUIMainReportItem personResult
                    in sortedReportItems)
            {
                gridControl.AddDisplayItem(personResult);
            }
        }

        int ReportItemSort(Reports.GUIMainReportItem a, Reports.GUIMainReportItem b)
        {
            if (a.Person == b.Person)
                return 0;
            if (a.Person == TotalRowLabel)
                return -1;
            if (b.Person == TotalRowLabel)
                return 1;
            int aU = (int)(a.AveUrgency * 10.0);
            int bU = (int)(b.AveUrgency * 10.0);
            if (aU > bU)
                return -1;
            if (aU < bU)
                return 1;
            return 0;
        }

        private bool m_needToSave = false;

        // If set, restart as soon as a save is not required, or once this time is passed
        // restart anyway.
        private DateTime? m_restartRequiredTime = null;
        void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // We can't be in a redraw callback, so clear the redisplay status cache for good measure.
            Functions.ClearDisplayCaches();

            ApplicationProjectPal.Instance.RefreshFromDatabase();
            //buttonSave.Enabled = DBAccess.DBObjectBase.HasUnsavedChanges;

            if (!m_restartRequiredTime.HasValue &&
                ConfigSettings.Instance.AppVersionShort != ConfigSettings.Instance.DBReleaseVersion)
            {
                m_restartRequiredTime = DateTime.Now.AddSeconds(30); // force restart in 1 hours

                Utils.Logger.Log("The version of the app '" + ConfigSettings.Instance.AppVersionShort +
                   "' and the version specfied in the database '" +
                   ConfigSettings.Instance.DBReleaseVersion + "' don't match so ProjectPal will exit as" +
                   " soon as possible once any outstanding changes are saved, or by " +
                   m_restartRequiredTime.Value.ToString("dd-MMM-yyyy HH:mm:ss") + " at the latest");
            }

            if (m_needToSave && !m_restartRequiredTime.HasValue)
                return;

            m_needToSave = DBAccess.DBObjectBase.HasUnsavedChanges;
            buttonSave.ForeColor = m_needToSave ? Color.Red : Color.Black;


            if ((m_restartRequiredTime.HasValue && !m_needToSave))
            {
                Utils.Logger.Log("ProjectPal needs to exit, there are no outstanding changes to be saved");
                Close();
            }

            if (m_restartRequiredTime.HasValue)
            {
                buttonSave.ForeColor = Color.Black;
                buttonSave.BackColor = Color.Red;
            }

            if (m_restartRequiredTime < DateTime.Now)
            {
                Utils.Logger.Log("ProjectPal needs to exit, there are outstanding changes that have not been saved");
                Close();
            }

        }

        System.Windows.Threading.DispatcherTimer m_refreshTimer;


        private void buttonShowTasks_Click(object sender, EventArgs e)
        {
            if (!Permissions.IsSuperUser)
                ShowTaskListWindow(DBProjectPal.Person.CurrentUser.Name);
            else
                ShowTaskListWindow((List<string>)null);
        }

        private static void ShowTaskListWindow(string resourceFilter)
        {
            ShowTaskListWindow(new List<string>() { resourceFilter });
        }

        private static void ShowTaskListWindow(List<string> resourceFilter)
        {
            bool urgencyCacheOrig = ProjectPal.Tasks.GUITask.UrgencyCachingEnabled;
            ProjectPal.Tasks.GUITask.UrgencyCachingEnabled = true;

            List<CustomGUIControls.Grid.IGridItem> tasksForGUI = new List<CustomGUIControls.Grid.IGridItem>();

            IEnumerable<Task> tasks = Task.AllInstances;
            foreach (Task task in tasks)
            {
                //if (task.Status == StatusValue.Closed ||
                //    task.Status == StatusValue.Cancelled)
                //    continue;
                tasksForGUI.Add(ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(task));
            }

            Tasks.TaskWindow theTaskWindow = new Tasks.TaskWindow(tasksForGUI, resourceFilter);

            theTaskWindow.SetGridDoubleClickFunction(ProjectPal.Tasks.TaskDetail.ShowDetailWindow);

            theTaskWindow.Show();

            ProjectPal.Tasks.GUITask.UrgencyCachingEnabled = urgencyCacheOrig;
        }

        static public void DoubleClickRow(CustomGUIControls.IDisplayItem objectToDisplay)
        {
            ProjectPal.Reports.GUIMainReportItem selectedItem = objectToDisplay as ProjectPal.Reports.GUIMainReportItem;
            if (selectedItem != null)
            {

                if (selectedItem.Person != TotalRowLabel && selectedItem.Person != DBProjectPal.Task.OtherResource)
                {
                    ShowTaskListWindow(selectedItem.Person);
                }
                else if (selectedItem.Person == DBProjectPal.Task.OtherResource)
                {
                    List<string> otherPeople = new List<string>();
                    foreach (DBProjectPal.Person aPerson in DBProjectPal.Person.AllActiveInstances)
                    {
                        if (!aPerson.IsResource)
                            otherPeople.Add(aPerson.Name);
                    }

                    ShowTaskListWindow(otherPeople);
                }
            }
        }


        private void buttonComponents_Click(object sender, EventArgs e)
        {
            Components.ComponentWindow theComponentWindow = Components.ComponentWindow.GetInstanceFromGUIComponent(null);
            theComponentWindow.Show();
            theComponentWindow.Focus();
        }

        private void buttonProjects_Click(object sender, EventArgs e)
        {
            Projects.ProjectDetail.GetAndShowDetailWindow(null);
        }



        private void buttonSave_Click(object sender, EventArgs e)
        {
            ApplicationProjectPal.Instance.RefreshFromDatabase();
            ApplicationProjectPal.Instance.SaveToDataBase();



            m_needToSave = false;

            buttonSave.ForeColor = Color.Black;
            buttonSave.BackColor = buttonShowTasks.BackColor;
        }



        private void buttonFind_Click(object sender, EventArgs e)
        {
            FormFind ff = new FormFind();

            ff.Show();

        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DBAccess.DBObjectBase.HasUnsavedChanges)
            {
                bool forceSave = (m_restartRequiredTime.HasValue && m_restartRequiredTime.Value < DateTime.Now);

                if (!forceSave)
                {
                    DialogResult response = MessageBox.Show("There are unsaved changes.  Do you wish to save these changes before exiting?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);


                    if (response == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }

                    if (response == DialogResult.Yes)
                    {
                        DBAccess.DBObjectBase.Save();
                        MessageBox.Show("Changes have been saved.", "Saved");
                    }
                }
                else
                {
                    Utils.Logger.Log("Forcing save of outstanding items");
                    DBAccess.DBObjectBase.Save();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<CustomGUIControls.Grid.IGridItem> projectsForGUI = new List<CustomGUIControls.Grid.IGridItem>();

            IEnumerable<Project> projects = Project.AllInstances;
            foreach (Project project in projects)
            {
                if (project.IsHidden)
                    continue;
                projectsForGUI.Add(ProjectPal.Projects.GUIProject.GetInstanceFromDBProject(project));
            }

            Projects.ProjectWindow theProjectWindow = new Projects.ProjectWindow(projectsForGUI);

            theProjectWindow.SetGridDoubleClickFunction(ProjectPal.Projects.ProjectDetail.ShowDetailWindow);

            theProjectWindow.Show();
        }



        private void adminToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ToolStripMenuItem menuItem = sender as System.Windows.Forms.ToolStripMenuItem;

            const string adminName = "Admin";
            const string manageUsersName = "Manage Users";

            if (menuItem != null)
            {
                if (menuItem.Text == adminName || menuItem.Text == manageUsersName)
                {
                    bool isSuperUser = Permissions.IsSuperUser;
                    PasswordWindow windowInstance = new PasswordWindow();
                    if (isSuperUser || windowInstance.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        if (isSuperUser || Functions.AdminPassword(windowInstance.Password))
                        {
                            if (menuItem.Text == adminName)
                            {
                                AdminWindow adminWindowInstance = new AdminWindow();
                                adminWindowInstance.ShowDialog(this);
                                Program.ApplyConfigSettings();
                            }
                            if (menuItem.Text == manageUsersName)
                            {
                                AdminWindow adminWindowInstance = new AdminWindow();
                                adminWindowInstance.ShowDialog(this);
                                Program.ApplyConfigSettings();
                            }

                        }
                        else
                        {
                            MessageBox.Show("Password is not correct", "Password error", MessageBoxButtons.OK);
                        }
                    }
                }
            }
        }

        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            ConfigWindow windowInstance = new ConfigWindow();
            windowInstance.ShowDialog(this);

            Program.ApplyConfigSettings();

        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {

            ApplicationProjectPal theApp = ApplicationProjectPal.Instance;

            theApp.RefreshFromDatabase();

        }

        private void manageUsersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_windowManagePeopleInstance == null)
            {
                m_windowManagePeopleInstance = new ProjectPal.People.FormManagePeople(this);
                m_windowManagePeopleInstance.Show();
            }
            else
            {
                m_windowManagePeopleInstance.Focus();
            }

        }

        internal ProjectPal.People.FormManagePeople m_windowManagePeopleInstance = null;
    }
}
