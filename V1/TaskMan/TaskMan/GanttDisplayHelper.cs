using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace TaskMan
{
    class GanttDisplayHelper
    {
        static private Dictionary<string, System.Windows.Media.Color> s_resourceColorMap = null;

        static public void ClearColourCache()
        {
            s_resourceColorMap = null;
        }

        static internal Dictionary<string, System.Windows.Media.Color> ResourceColorMap
        {
            get
            {
                if (s_resourceColorMap == null)
                {
                    s_resourceColorMap = new Dictionary<string, System.Windows.Media.Color>();
                    foreach (DBTaskMan.Person currentPerson in DBTaskMan.Person.AllInstances)
                    {
                        if (!currentPerson.IsResource)
                            continue;
                        s_resourceColorMap.Add(currentPerson.Name, currentPerson.Colour);
                    }
                    s_resourceColorMap.Add(DBTaskMan.Task.OtherResource, DBTaskMan.Person.ColourForResource(DBTaskMan.Task.OtherResource));
                    if (!s_resourceColorMap.ContainsKey("Unassigned"))
                        s_resourceColorMap.Add("Unassigned", DBTaskMan.Person.ColourForResource("Unassigned"));
                }
                return s_resourceColorMap;
            }

        }

        static public HashSet<string> AddPlanDetail(PlanDisplay.PlanControl theOwningPlanControl,
                                PlanDisplay.Project parentProject,
                                IEnumerable<DBTaskMan.Task> tasks,
                                IEnumerable<DBTaskMan.Component> components,
                                System.Comparison<DBTaskMan.Task> taskSorterFn,
                                System.Comparison<DBTaskMan.Component> componentsSorterFn)
        {
            HashSet<string> allDistinctResources = new HashSet<string>();

            AddPlanDetail(theOwningPlanControl, parentProject, tasks, components, ref allDistinctResources, taskSorterFn, componentsSorterFn);

            return allDistinctResources;
        }


        static private void AddPlanDetail(PlanDisplay.PlanControl theOwningPlanControl,
                               PlanDisplay.Project parentProject,
                               IEnumerable<DBTaskMan.Task> tasks,
                               IEnumerable<DBTaskMan.Component> components,
                               ref HashSet<string> allDistinctResources,
                               System.Comparison<DBTaskMan.Task> taskSorterFn,
                               System.Comparison<DBTaskMan.Component> componentsSorterFn)
        {
            List<DBTaskMan.Task> taskToUse = new List<DBTaskMan.Task>();

            foreach (DBTaskMan.Task task in tasks)
            {
                if (task.Status == DBTaskMan.StatusValue.Closed ||
                    task.Status == DBTaskMan.StatusValue.Cancelled ||
                    !task.StartDate.HasValue ||
                    !task.EndDate.HasValue)
                    continue;
                taskToUse.Add(task);
            }
            if (taskSorterFn != null)
            {
                taskToUse.Sort(taskSorterFn);
            }

            foreach (DBTaskMan.Task task in taskToUse)
            {
                DateTime startDate = Utils.Misc.GoodBusinessDay(task.StartDate.Value);
                DateTime endDate = Utils.Misc.GoodBusinessDay(task.EndDate.Value);


                PlanDisplay.PriorityValue taskPriority = (PlanDisplay.PriorityValue)((int)(task.Priority ?? DBTaskMan.PriorityValue._3_Med));
                PlanDisplay.StatusValue taskStatus = (PlanDisplay.StatusValue)((int)(task.Status ?? DBTaskMan.StatusValue.NotStarted));

                List<string> resourcesForThisTask = new List<string>();
                foreach (DBTaskMan.IResource resource in task.TeamResources)
                {
                    resourcesForThisTask.Add(resource.Name);
                    if (!allDistinctResources.Contains(resource.Name))
                        allDistinctResources.Add(resource.Name);
                }

                PlanDisplay.Task displayTask = new PlanDisplay.Task(task, theOwningPlanControl, startDate, endDate, resourcesForThisTask, taskPriority, taskStatus, task.TotalEffort);
                parentProject.AddPlannedItem(displayTask);
            }

            if (components != null)
            {

                List<DBTaskMan.Component> componentsToUse = new List<DBTaskMan.Component>();

                foreach (DBTaskMan.Component component in components)
                {
                    //if (component.Priority == DBTaskMan.PriorityValue._0_Cancelled ||
                    //    component.Priority == DBTaskMan.PriorityValue._0_Closed ||
                    //    !component.IsActive)
                    //    continue;
                    componentsToUse.Add(component);
                }

                if (componentsSorterFn != null)
                    componentsToUse.Sort(componentsSorterFn);

                foreach (DBTaskMan.Component component in componentsToUse)
                {
                    PlanDisplay.Project displayProject = new PlanDisplay.Project(component, theOwningPlanControl, null, parentProject.RequiredEvents);
                    parentProject.AddPlannedItem(displayProject);
                    AddPlanDetail(theOwningPlanControl, displayProject, component.Tasks, component.SubComponents, ref allDistinctResources, taskSorterFn, componentsSorterFn);



                }
            }
        }




        static public HashSet<string> AddPlanDetail(PlanDisplay.PlanControl theOwningPlanControl,
                                PlanDisplay.Project parentProject,
                                IEnumerable<DBTaskMan.Task> tasks,
                                IEnumerable<DBTaskMan.Project> projects,
                                System.Comparison<DBTaskMan.Task> taskSorterFn,
                                System.Comparison<DBTaskMan.Project> projectSorterFn)
        {
            HashSet<string> allDistinctResources = new HashSet<string>();

            AddPlanDetail(theOwningPlanControl, parentProject, tasks, projects, ref allDistinctResources, taskSorterFn, projectSorterFn);

            return allDistinctResources;
        }


        public GanttDisplayHelper()
        {
        }

        public void SetPlanControl(PlanDisplay.PlanControl thePlanControl)
        {
            m_thePlanControl = thePlanControl;
        }


        PlanDisplay.PlanControl m_thePlanControl = null;



        public void HandleEvent(object underlyingObject, object underlyingObjectParent, PlanDisplay.TimeBox.Event e, object data)
        {

            DBTaskMan.Component thisComponent = underlyingObject as DBTaskMan.Component;
            DBTaskMan.Component previousComponent = m_lastUnderlyingObjectMove as DBTaskMan.Component;
            DBTaskMan.Component parentComponent = underlyingObjectParent as DBTaskMan.Component;

            DBTaskMan.Task thisTask = underlyingObject as DBTaskMan.Task;
            DBTaskMan.Task previousTask = m_lastUnderlyingObjectMove as DBTaskMan.Task;
            DBTaskMan.Task parentTask = underlyingObjectParent as DBTaskMan.Task;

            DBTaskMan.Project thisProject = underlyingObject as DBTaskMan.Project;
            DBTaskMan.Project previousProject = m_lastUnderlyingObjectMove as DBTaskMan.Project;
            DBTaskMan.Project parentProject = underlyingObjectParent as DBTaskMan.Project;

            //string thisItem = "this " + (
            //       (thisComponent != null) ? ("Component = " + thisComponent.Name) :
            //        (thisTask != null) ? ("Task = " + thisTask.Description) :
            //        (thisProject != null) ? ("Project = " + thisProject.Name) :
            //        (underlyingObject != null) ? ("Unknown " + underlyingObject.GetType().ToString()) :
            //        "null");


            //string previousItem = "previous " + (
            //   (previousComponent != null) ? ("Component = " + previousComponent.Name) :
            //    (previousTask != null) ? ("Task = " + previousTask.Description) :
            //    (previousProject != null) ? ("Project = " + previousProject.Name) :
            //    (m_lastUnderlyingObjectMove != null) ? ("Unknown " + m_lastUnderlyingObjectMove.GetType().ToString()) :
            //    "null");

            //string parentItem = "parent " + (
            //            (parentComponent != null) ? ("Component = " + parentComponent.Name) :
            //             (parentTask != null) ? ("Task = " + parentTask.Description) :
            //             (parentProject != null) ? ("Project = " + parentProject.Name) :
            //             (underlyingObjectParent != null) ? ("Unknown " + underlyingObjectParent.GetType().ToString()) :
            //             "null");

            //Utils.Logger.Log(e.ToString()+" [" + thisItem + ", " + previousItem + ", " + parentItem + "]");




            if (e == PlanDisplay.TimeBox.Event.MouseLeave)
            {
                m_lastUnderlyingObjectMove = null;
                DBAccess.DBObjectBase dbObject = underlyingObject as DBAccess.DBObjectBase;
                if (m_thePlanControl != null && dbObject != null)
                    m_thePlanControl.SetCaption("");

            }
            else if (e == PlanDisplay.TimeBox.Event.MouseEnter || e == PlanDisplay.TimeBox.Event.MouseMove)
            {
                if (m_lastUnderlyingObjectMove != underlyingObject)
                {
                    m_lastUnderlyingObjectMove = underlyingObject;
                    if (thisTask != null)
                    {
                        string parentText = (parentProject != null) ?
                                            ((string.IsNullOrEmpty(parentProject.FullParentName) ? "" :
                                                 (parentProject.FullParentName + "=>")) + parentProject.Name) :
                                            (parentComponent != null) ?
                                            ((string.IsNullOrEmpty(parentComponent.FullParentName) ? "" :
                                                  (parentComponent.FullParentName + "=>")) +
                                              parentComponent.Name) :
                                             "";
                        if (m_thePlanControl != null)
                        {
                            string captionText = "";
                            if (string.IsNullOrEmpty(parentText))
                            {
                                DBTaskMan.Project taskParentProject = thisTask.Project;
                                if (taskParentProject != null)
                                    captionText = taskParentProject.Name;

                                captionText += " : [" + thisTask.Description + "]";
                            }
                            else
                            {
                                captionText = parentText + " : [" + thisTask.Description + "]";
                            }
                            m_thePlanControl.SetCaption(captionText);
                        }
                    }
                    else
                    {
                        if (thisProject != null)
                        {
                            string caption = "";
                            if (!string.IsNullOrEmpty(thisProject.FullParentName))
                                caption = thisProject.FullParentName + "=>";
                            if (m_thePlanControl != null)
                                m_thePlanControl.SetCaption(caption + thisProject.Name);
                        }
                        else
                        {
                            if (thisComponent != null)
                            {
                                string caption = "";
                                if (!string.IsNullOrEmpty(thisComponent.FullParentName))
                                    caption = thisComponent.FullParentName + "=>";
                                if (m_thePlanControl != null)
                                    m_thePlanControl.SetCaption(caption + thisComponent.Name);
                            }

                        }
                    }
                }
            }
            else if (e == PlanDisplay.TimeBox.Event.MouseDoubleClick)
            {
                m_lastUnderlyingObjectMove = underlyingObject;
                if (thisTask != null)
                {
                    TaskMan.Tasks.TaskDetail.GetAndShowDetailWindow(TaskMan.Tasks.GUITask.GetInstanceFromDBTask(thisTask));
                }
                else
                {
                    if (thisProject != null)
                    {
                        TaskMan.Projects.ProjectDetail.GetAndShowDetailWindow(TaskMan.Projects.GUIProject.GetInstanceFromDBProject(thisProject));
                    }
                    else
                    {
                        if (thisComponent != null)
                        {
                            TaskMan.Components.ComponentWindow window = TaskMan.Components.ComponentWindow.GetInstanceFromGUIComponent(TaskMan.Components.GUIComponent.GetInstanceFromDBComponent(thisComponent));
                            window.Show();
                            window.Focus();
                        }

                    }

                }

            }
            else if (e == PlanDisplay.TimeBox.Event.CtrlMouseDoubleClick)
            {
                m_lastUnderlyingObjectMove = underlyingObject;
                if (thisTask != null)
                {
                    // Nothing to do
                }
                else
                {
                    if (thisProject != null)
                    {
                        TaskMan.Projects.FormPlanDisplay.GetAndShowPlanDisplayWindow(thisProject);
                    }
                }

            }
            else if (e == PlanDisplay.TimeBox.Event.DayShift)
            {

                int? businessDaysToShift = data as int?;
                if (businessDaysToShift.HasValue && businessDaysToShift != 0)
                {
                    List<TaskMan.Tasks.GUITask> taskToRefresh = new List<Tasks.GUITask>();

                    if (thisProject != null &&
                        (Permissions.IsSuperUser ||
                         Permissions.IsAllowed(thisProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit)))
                    {
                        DateTime? projectStartDate = thisProject.StartDate;
                        if (projectStartDate.HasValue)
                            thisProject.StartDate = Utils.Misc.AddBusinessDays(projectStartDate.Value, businessDaysToShift.Value);


                        foreach (DBTaskMan.Project currentProject in thisProject.AllActiveSubProjects)
                        {
                            DateTime? currentProjectStartDate = currentProject.StartDate;
                            if (currentProjectStartDate.HasValue)
                                currentProject.StartDate = Utils.Misc.AddBusinessDays(currentProjectStartDate.Value, businessDaysToShift.Value);
                        }


                        //foreach (DBTaskMan.Task currentTask in thisProject.AllActiveTasks)
                        //{
                        //    if (currentTask.HasRelativeStartDate)
                        //        continue;
                        //    if (currentTask.Status != DBTaskMan.StatusValue.Cancelled &&
                        //        currentTask.Status != DBTaskMan.StatusValue.Closed &&
                        //        currentTask.StartDate.HasValue)
                        //        currentTask.StartDate = Utils.Misc.AddBusinessDays(currentTask.StartDate.Value, businessDaysToShift.Value);

                        //    TaskMan.Tasks.GUITask guiTask = TaskMan.Tasks.GUITask.GetInstanceFromDBTask(currentTask);
                        //    if (guiTask != null)
                        //        taskToRefresh.Add(guiTask);
                        //}

                    }
                    else
                    {

                        DBTaskMan.Task currentTask = underlyingObject as DBTaskMan.Task;
                        if (currentTask != null)
                        {
                            if (Permissions.IsSuperUser ||
                             Permissions.IsAllowed(currentTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                            {
                                DateTime? currentTaskStartDate = currentTask.StartDate;
                                if (currentTaskStartDate.HasValue)
                                {
                                    if (businessDaysToShift.Value >= 0 || !currentTask.StartIsDeterminedByPreDependency)
                                        currentTask.StartDate = Utils.Misc.AddBusinessDays(currentTaskStartDate.Value, businessDaysToShift.Value);
                                    else
                                    {

                                        DateTime possibleStartDate = Utils.Misc.AddBusinessDays(currentTaskStartDate.Value, businessDaysToShift.Value);
                                        DateTime? dependencyEndDate = currentTask.LatestPreDepenentEndDate;


                                        currentTask.StartDate = dependencyEndDate > possibleStartDate ? dependencyEndDate : possibleStartDate;
                                    }
                                }

                            }

                            TaskMan.Tasks.GUITask guiTask = TaskMan.Tasks.GUITask.GetInstanceFromDBTask(currentTask);
                            if (guiTask != null)
                                taskToRefresh.Add(guiTask);
                        }
                    }

                    Functions.ClearDisplayCaches();
                    foreach (TaskMan.Tasks.GUITask guiTask in taskToRefresh)
                        guiTask.Redisplay();
                    ApplicationTaskMan.Instance.RefreshAllWindows();
                }
            }
        }

        private object m_lastUnderlyingObjectMove = null;


        static private void AddPlanDetail(PlanDisplay.PlanControl theOwningPlanControl,
                                PlanDisplay.Project parentProject,
                                IEnumerable<DBTaskMan.Task> tasks,
                                IEnumerable<DBTaskMan.Project> projects,
                                ref HashSet<string> allDistinctResources,
                                System.Comparison<DBTaskMan.Task> taskSorterFn,
                                System.Comparison<DBTaskMan.Project> projectSorterFn)
        {
            List<DBTaskMan.Task> taskToUse = new List<DBTaskMan.Task>();

            foreach (DBTaskMan.Task task in tasks)
            {
                if (task.Status == DBTaskMan.StatusValue.Closed ||
                    task.Status == DBTaskMan.StatusValue.Cancelled ||
                    !task.StartDate.HasValue ||
                    !task.EndDate.HasValue)
                    continue;
                taskToUse.Add(task);
            }
            if (taskSorterFn != null)
            {
                taskToUse.Sort(taskSorterFn);
            }

            foreach (DBTaskMan.Task task in taskToUse)
            {
                DateTime startDate = Utils.Misc.GoodBusinessDay(task.StartDate.Value);
                DateTime endDate = Utils.Misc.GoodBusinessDay(task.EndDate.Value);


                PlanDisplay.PriorityValue taskPriority = (PlanDisplay.PriorityValue)((int)(task.Priority ?? DBTaskMan.PriorityValue._3_Med));
                PlanDisplay.StatusValue taskStatus = (PlanDisplay.StatusValue)((int)(task.Status ?? DBTaskMan.StatusValue.NotStarted));

                List<string> resourcesForThisTask = new List<string>();
                foreach (DBTaskMan.IResource resource in task.TeamResources)
                {
                    resourcesForThisTask.Add(resource.Name);
                    if (!allDistinctResources.Contains(resource.Name))
                        allDistinctResources.Add(resource.Name);
                }

                PlanDisplay.Task displayTask = new PlanDisplay.Task(task, theOwningPlanControl, startDate, endDate, resourcesForThisTask, taskPriority, taskStatus, task.TotalEffort);
                parentProject.AddPlannedItem(displayTask);
            }

            if (projects != null)
            {

                List<DBTaskMan.Project> projectsToUse = new List<DBTaskMan.Project>();

                foreach (DBTaskMan.Project project in projects)
                {
                    if (project.Priority == DBTaskMan.PriorityValue._0_Cancelled ||
                        project.Priority == DBTaskMan.PriorityValue._0_Closed ||
                        !project.IsActive)
                        continue;
                    projectsToUse.Add(project);
                }

                if (projectSorterFn != null)
                    projectsToUse.Sort(projectSorterFn);

                foreach (DBTaskMan.Project project in projectsToUse)
                {
                    PlanDisplay.Project displayProject = new PlanDisplay.Project(project, theOwningPlanControl, project.DueDate, parentProject.RequiredEvents);

                    parentProject.AddPlannedItem(displayProject);

                    AddPlanDetail(theOwningPlanControl, displayProject, project.Tasks, project.SubProjects, ref allDistinctResources, taskSorterFn, projectSorterFn);
                }
            }


        }



    }
}
