using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace ProjectPal.Projects
{
    public partial class FormPlanDisplay : Form
    {


        private static Dictionary<DBProjectPal.Project, FormPlanDisplay> s_allProjectInstances = new Dictionary<DBProjectPal.Project, FormPlanDisplay>();


        private static FormPlanDisplay m_topLevelWindow = null;



        static public FormPlanDisplay GetAndShowPlanDisplayWindow(DBProjectPal.Project project)
        {
            bool newWindow = false;
            FormPlanDisplay window;
            if (project == null)
            {
                if (m_topLevelWindow == null)
                {
                    newWindow = true;
                    window = new FormPlanDisplay(null);
                }
                else
                    window = m_topLevelWindow;
            }
            else if (!s_allProjectInstances.TryGetValue(project, out window))
            {
                newWindow = true;
                window = new FormPlanDisplay(project);
            }

            if (newWindow)
                window.Show();
            else
            {
                window.Redisplay();
                window.Activate();
            }

            return window;
        }


        private FormPlanDisplay(DBProjectPal.Project projectToDisplay)
        {
            InitializeComponent();
            m_projectOnDisplay = projectToDisplay;
            if (m_projectOnDisplay == null)
            {
                m_topLevelWindow = this;
                this.Text = "Gantt Display: " + "Top Level Projects";
            }
            else
            {
                s_allProjectInstances.Add(m_projectOnDisplay, this);

                this.Text = "Gantt Display: " + m_projectOnDisplay.FullName;
            }
        }

        DBProjectPal.Project m_projectOnDisplay;
        PlanDisplay.PlanControl m_thePlanControl = null;

        private void FormPlanDisplay_Load(object sender, EventArgs e)
        {
            Redisplay();
        }

        private bool m_firstRedisplay = true;
        public void Redisplay()
        {

            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;

            bool guiUrgencyCacheState = ProjectPal.Tasks.GUITask.UrgencyCachingEnabled;
            
            ProjectPal.Tasks.GUITask.UrgencyCachingEnabled = true;

            bool firstRedisplay = m_firstRedisplay;
            m_firstRedisplay = false;

            PlanDisplay.PlanControl originalControl = elementHostArea.Child as PlanDisplay.PlanControl;
            
            if (originalControl == null)
            {
                m_thePlanControl = new PlanDisplay.PlanControl();
            }
            else
            {
                m_thePlanControl = originalControl;
            }





            HashSet<string> allDistinctResources;

            PlanDisplay.Project displayProject;
            HashSet<DBProjectPal.Task> topLevelProjectTasksIfNeeded = new HashSet<DBProjectPal.Task>();


            GanttDisplayHelper displayHelper = new GanttDisplayHelper();

            if (m_projectOnDisplay == null)
            {

                List<DBProjectPal.Project> allProjects = new List<DBProjectPal.Project>(DBProjectPal.Project.TopLevelProjects);
                List<DBProjectPal.Project> projectsToShow = new List<DBProjectPal.Project>();

                foreach (DBProjectPal.Task currentTask in DBProjectPal.Task.AllInstances)
                {
                    if (currentTask.Status == DBProjectPal.StatusValue.Cancelled ||
                        currentTask.Status == DBProjectPal.StatusValue.Closed)
                        continue;
                    if(currentTask.Project != null)
                        continue;
                    topLevelProjectTasksIfNeeded.Add(currentTask);
                }

                foreach (DBProjectPal.Project currentProject in allProjects)
                {
                    if (currentProject.IsHidden)
                        continue;

                    if (!currentProject.IsActive)
                        continue;

                    projectsToShow.Add(currentProject);
                }

                displayProject = new PlanDisplay.Project(m_projectOnDisplay, m_thePlanControl, null, PlanDisplay.Project.EventType.Project);
                displayProject.SetEventFunction(displayHelper.HandleEvent);

                allDistinctResources = GanttDisplayHelper.AddPlanDetail(m_thePlanControl,
                                            displayProject, topLevelProjectTasksIfNeeded, projectsToShow,
                                            TaskSort, GUIProject.SortProjectsByPriority/*ProjectSort*/);
            }
            else
            {
                displayProject = new PlanDisplay.Project(m_projectOnDisplay, m_thePlanControl, m_projectOnDisplay.DueDate, PlanDisplay.Project.EventType.Project);
                displayProject.SetEventFunction(displayHelper.HandleEvent);

                allDistinctResources = GanttDisplayHelper.AddPlanDetail(m_thePlanControl,
                                            displayProject, m_projectOnDisplay.Tasks, m_projectOnDisplay.SubProjects,
                                            TaskSort, GUIProject.SortProjectsByPriority/*ProjectSort*/);
            }

            if (originalControl == null)
            {
                displayProject.SetDateLine(DateTime.Now.Date);

                m_thePlanControl.RedisplayProject(displayProject, allDistinctResources, ProjectPal.GanttDisplayHelper.ResourceColorMap);
                m_thePlanControl.ReScaleToAreaData(this.elementHostArea.Width, this.elementHostArea.Height);
            }
            else
            {
                m_thePlanControl.RedisplayProject(displayProject, allDistinctResources, ProjectPal.GanttDisplayHelper.ResourceColorMap);
            }

            displayHelper.SetPlanControl(m_thePlanControl);
            elementHostArea.Child = m_thePlanControl;

            ProjectPal.Tasks.GUITask.UrgencyCachingEnabled = guiUrgencyCacheState;
        }

       

        private int TaskSort(DBProjectPal.Task taskA, DBProjectPal.Task taskB)
        {
            double urgencyA = ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(taskA).Urgency;
            double urgencyB = ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(taskB).Urgency;
            if (urgencyA > urgencyB)
                return -1;
            if (urgencyA < urgencyB)
                return 1;
            return 0;

        }

        private int ProjectSort(DBProjectPal.Project ProjectA, DBProjectPal.Project ProjectB)
        {
            return string.Compare(ProjectA.Name, ProjectB.Name);
        }






        private void FormPlanDisplay_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_projectOnDisplay != null)
                s_allProjectInstances.Remove(m_projectOnDisplay);
            else
                m_topLevelWindow = null;
        }


        public static void RedisplayAll()
        {
            List<FormPlanDisplay> windows = new List<FormPlanDisplay>();
            if (m_topLevelWindow != null)
                windows.Add(m_topLevelWindow);
            windows.AddRange(s_allProjectInstances.Values);

            foreach (FormPlanDisplay currentWindow in windows)
                currentWindow.Redisplay();
        }


    }
}
