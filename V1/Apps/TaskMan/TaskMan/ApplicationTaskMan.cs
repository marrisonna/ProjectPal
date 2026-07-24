using System;
using System.Collections.Generic;
using DBTaskMan;
using DBAccess;
using System.Text;

namespace TaskMan
{
    class ApplicationTaskMan
    {
        static private ApplicationTaskMan m_instance = null;
        static public ApplicationTaskMan Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new ApplicationTaskMan();
                return m_instance;
            }
        }


        public DateTime LastDBUpdateTime { get { return m_lastDBUpdateTime; } }
        private DateTime m_lastDBUpdateTime = DateTime.Now;

        public void SaveToDataBase()
        {
            DBAccess.DBObjectBase.Save();
        }

        public void RefreshFromDatabase()
        {


            if (m_lastDBUpdateTime >= ConfigSettings.Instance.DBLastUpdateTime)
                return;


            Functions.ClearDisplayCaches();

            m_lastDBUpdateTime = ConfigSettings.Instance.DBLastUpdateTime;
            //IEnumerator<Task> itr = Task.AllInstances.GetEnumerator();
            //itr.MoveNext();
            //Task task1 = itr.Current;
            //itr.MoveNext();
            //Task task2 = itr.Current;

            //FormTaskMerge testForm = new FormTaskMerge(task1, task2);
            //testForm.ShowDialog();


            //return;

            List<DatabaseUpdates> updates;
            if (Utils.DatabaseBase.DBType == Utils.DatabaseBase.DBTypeValues.FileSystem)
            {
                updates = DBObjectBase.AllFileSystemUpdates();
            }
            else
            {
                updates = DBObjectBase.AllDataBaseUpdates();
            }



            List<Task> tasksToRefresh = new List<Task>();
            List<Component> componentsToRefresh = new List<Component>();
            List<Project> projectsToRefresh = new List<Project>();

            foreach (DatabaseUpdates update in updates)
            {
                if (update.ObjectBaseType == typeof(Attachment))
                    UpdateAttachments(update, componentsToRefresh, projectsToRefresh, tasksToRefresh);
                if (update.ObjectBaseType == typeof(Remark))
                    UpdateRemarks(update, componentsToRefresh, projectsToRefresh, tasksToRefresh);
                if (update.ObjectBaseType == typeof(Component))
                    UpdateComponents(update, componentsToRefresh, projectsToRefresh, tasksToRefresh);
                //if(update.ObjectBaseType== typeof(Person))
                //            updatePersons(update, componentsToRefresh, projectsToRefresh, tasksToRefresh);
                if (update.ObjectBaseType == typeof(Project))
                    UpdateProjects(update, componentsToRefresh, projectsToRefresh, tasksToRefresh);
                if (update.ObjectBaseType == typeof(Task))
                    UpdateTasks(update, componentsToRefresh, projectsToRefresh, tasksToRefresh);



                foreach (DBObjectBase affectedInstance in update.AffectedInstances)
                {
                    if (affectedInstance.GetType() == typeof(Task))
                        tasksToRefresh.Add((Task)affectedInstance);
                    if (affectedInstance.GetType() == typeof(Component))
                        componentsToRefresh.Add((Component)affectedInstance);
                    if (affectedInstance.GetType() == typeof(Project))
                        projectsToRefresh.Add((Project)affectedInstance);
                }
            }




            foreach (Project p in projectsToRefresh)
            {
                AddAllProjectTasks(p, tasksToRefresh);
            }


            HashSet<Task> uniqueTasksToRefresh = new HashSet<Task>();
            HashSet<Component> uniqueComponentsToRefresh = new HashSet<Component>();
            HashSet<Project> uniqueProjectsToRefresh = new HashSet<Project>();

            foreach (Task t in tasksToRefresh)
                if (!uniqueTasksToRefresh.Contains(t))
                    uniqueTasksToRefresh.Add(t);

            foreach (Component c in componentsToRefresh)
                if (!uniqueComponentsToRefresh.Contains(c))
                    uniqueComponentsToRefresh.Add(c);

            foreach (Project p in projectsToRefresh)
                if (!uniqueProjectsToRefresh.Contains(p))
                    uniqueProjectsToRefresh.Add(p);

            //CustomGUIControls.DisplayItemImpl.RedisplayAll();


            Functions.ClearDisplayCaches();

            foreach (Task t in uniqueTasksToRefresh)
            {
                TaskMan.Tasks.GUITask.Redisplay(t);
            }


            Functions.ClearDisplayCaches();

            RefreshAllWindows();


        }

        private void AddAllProjectTasks(Project p, List<Task> tasksToRefresh)
        {
            foreach (Task t in p.TasksIncludingHidden)
                tasksToRefresh.Add(t);

            foreach (Project sp in p.SubProjectsIncludingHidden)
            {
                AddAllProjectTasks(sp, tasksToRefresh);
            }

        }

        public void RefreshAllWindows()
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this))
                return;


            //DBAccess.DBObjectBase.EnableVisibleInstanceCache(true);

            //Functions.ClearDisplayCaches();
            DBTaskMan.Functions.ClearDateCaches();

            try
            {
                TaskMan.Tasks.TaskDetail.RedisplayAll();


                TaskMan.Components.ComponentWindow.RedisplayAll();
                TaskMan.Components.GUIComponent.RedisplayAll();
                TaskMan.Projects.ProjectDetail.RedisplayAll();
                TaskMan.Projects.GUIProject.RedisplayAll();
                TaskMan.Projects.FormPlanDisplay.RedisplayAll();
                // TODO - Need to redisplay complete Task Windows, not just Gantts
                TaskMan.Tasks.TaskWindow.RedisplayAll();
                TaskMan.Components.ComponentWindow.RedisplayAllGantts();

                MainWindow.RedisplayAll();
            }
            catch (Exception err)
            {
                Utils.Logger.LogException(err, "Error when trying to refresh all windows");
            }
            finally
            {
                //DBAccess.DBObjectBase.EnableVisibleInstanceCache(false);
            }

        }

        private void UpdateAttachments(DatabaseUpdates updates,
                                       List<Component> componentsToRefresh,
                                       List<Project> projectsToRefresh,
                                       List<Task> tasksToRefresh)
        {
            foreach (Attachment currentAttachment in updates.DeletedInstances)
            {
                if (currentAttachment.Component != null)
                    componentsToRefresh.Add(currentAttachment.Component);
                if (currentAttachment.Project != null)
                    projectsToRefresh.Add(currentAttachment.Project);
                if (currentAttachment.Task != null)
                    tasksToRefresh.Add(currentAttachment.Task);
            }

            foreach (Attachment currentAttachment in updates.NewInstances)
            {
                if (currentAttachment.Component != null)
                    componentsToRefresh.Add(currentAttachment.Component);
                if (currentAttachment.Project != null)
                    projectsToRefresh.Add(currentAttachment.Project);
                if (currentAttachment.Task != null)
                    tasksToRefresh.Add(currentAttachment.Task);
            }

            // Can't be conflicted or modified attachemnts

        }

        private void UpdateRemarks(DatabaseUpdates updates,
                                   List<Component> componentsToRefresh,
                                   List<Project> projectsToRefresh,
                                   List<Task> tasksToRefresh)
        {
            foreach (Remark currentRemark in updates.DeletedInstances)
            {
                if (currentRemark.Component != null)
                    componentsToRefresh.Add(currentRemark.Component);
                if (currentRemark.Project != null)
                    projectsToRefresh.Add(currentRemark.Project);
                if (currentRemark.Task != null)
                    tasksToRefresh.Add(currentRemark.Task);
            }

            foreach (Remark currentRemark in updates.NewInstances)
            {
                if (currentRemark.Component != null)
                    componentsToRefresh.Add(currentRemark.Component);
                if (currentRemark.Project != null)
                    projectsToRefresh.Add(currentRemark.Project);
                if (currentRemark.Task != null)
                    tasksToRefresh.Add(currentRemark.Task);
            }

            foreach (Remark currentRemark in updates.UpdatedInstances)
            {

                if (currentRemark.Component != null)
                    componentsToRefresh.Add(currentRemark.Component);
                if (currentRemark.Project != null)
                    projectsToRefresh.Add(currentRemark.Project);
                if (currentRemark.Task != null)
                    tasksToRefresh.Add(currentRemark.Task);
            }

            // Can't be conflicted or modified attachemnts

        }


        private void UpdateComponents(DatabaseUpdates updates,
                                     List<Component> componentsToRefresh,
                                     List<Project> projectsToRefresh,
                                     List<Task> tasksToRefresh)
        {
            foreach (Component currentComponent in updates.DeletedInstances)
            {
                componentsToRefresh.Add(currentComponent);
            }

            foreach (Component currentComponent in updates.UpdatedInstances)
            {

                componentsToRefresh.Add(currentComponent);
            }

            foreach (UpdatedInstance updatedComponent in updates.ConflictingInstances)
            {
                FormComponentMerge mergeForm = new FormComponentMerge((Component)(updatedComponent.MemoryInstance), (Component)(updatedComponent.DatabaseInstance));
                mergeForm.ShowDialogIfMergeIsRequired();
                componentsToRefresh.Add((Component)updatedComponent.MemoryInstance);
            }

        }

        private void UpdateProjects(DatabaseUpdates updates,
                                    List<Component> componentsToRefresh,
                                    List<Project> projectsToRefresh,
                                    List<Task> tasksToRefresh)
        {
            foreach (Project currentProject in updates.DeletedInstances)
            {
                projectsToRefresh.Add(currentProject);
            }

            foreach (Project currentProject in updates.UpdatedInstances)
            {

                projectsToRefresh.Add(currentProject);
            }

            foreach (UpdatedInstance updatedProject in updates.ConflictingInstances)
            {
                FormProjectMerge mergeForm = new FormProjectMerge((Project)(updatedProject.MemoryInstance), (Project)(updatedProject.DatabaseInstance));
                mergeForm.ShowDialogIfMergeIsRequired();
                projectsToRefresh.Add((Project)updatedProject.MemoryInstance);
            }

        }


        private void UpdateTasks(DatabaseUpdates updates,
                                    List<Component> componentsToRefresh,
                                    List<Project> projectsToRefresh,
                                    List<Task> tasksToRefresh)
        {
            foreach (Task currentTask in updates.DeletedInstances)
            {
                tasksToRefresh.Add(currentTask);
                //componentsToRefresh.Add(currentTask.AffectedComponent);
                //foreach (Project proj in currentTask.Projects)
                //{
                //    projectsToRefresh.Add(proj);
                //}
            }

            foreach (Task currentTask in updates.UpdatedInstances)
            {
                tasksToRefresh.Add(currentTask);
            }

            foreach (UpdatedInstance updatedTask in updates.ConflictingInstances)
            {
                FormTaskMerge mergeForm = new FormTaskMerge((Task)(updatedTask.MemoryInstance), (Task)(updatedTask.DatabaseInstance));
                mergeForm.ShowDialogIfMergeIsRequired();
                tasksToRefresh.Add((Task)updatedTask.MemoryInstance);
            }

        }


    }
}
