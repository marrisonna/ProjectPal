using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utils;


namespace TaskMan.Projects
{
    /// <summary>
    /// Interaction logic for ProjectControl.xaml
    /// </summary>
    public partial class ProjectControl : UserControl, CustomGUIControls.IView
    {
        CustomGUIControls.ViewImpl m_viewImplementation = null;
        static Brush s_textDropHighlightColour = new SolidColorBrush(Color.FromRgb(153, 50, 204)); // DarkOrchid
        static Brush s_black = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        public ProjectControl(GUIProject itemToDisplay,
                                 CustomGUIControls.Grid.GridControl.CellDoubleClick taskWindowOpenFunction,
                                 bool openSubProjects,
                                 ActiveProjectsOnlyFn activeProjectsOnly,
                                 TasksToDisplayFn tasksToDisplay)
        {
            InitializeComponent();
            m_viewImplementation = new CustomGUIControls.ViewImpl(this);
            itemToDisplay.AddView(this);
            m_viewImplementation.AddDisplayItem(itemToDisplay);

            labelProjectName.Content = itemToDisplay.Name;

            bool isEditAllowed = Permissions.IsAllowed(itemToDisplay.DBProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit);
            if (!isEditAllowed)
            {
                contextMenuProject.IsEnabled = false;
                labelProjectName.ContextMenu = null;
                imageAddTask.Opacity = 0.4;
            }

            int noOfTasks = itemToDisplay.ActiveTaskCount;
            int noOfSubTasks = itemToDisplay.TotalActiveTaskCount;
            string priorityString = itemToDisplay.Priority;
            if (string.IsNullOrEmpty(priorityString))
                priorityString = GUIProjectColumns.s_priortyMed;
            LabelTaskCount.Content = "(" + priorityString + " - " + noOfTasks + " / " + noOfSubTasks + ")";


            m_taskWindowOpenFunction = taskWindowOpenFunction;
            m_activeProjectsOnly = activeProjectsOnly;
            m_tasksToDisplay = tasksToDisplay;
            if (openSubProjects)
                OpenDetail();

        }

        static private IList<string> m_hiddenTaskColumns = null;

        static private IList<string> m_columnOrder = null;

        static ProjectControl()
        {
            m_hiddenTaskColumns = new List<string>();
            m_hiddenTaskColumns.Add(TaskMan.Tasks.GUITaskColumns.s_Projects);
            m_hiddenTaskColumns.Add(TaskMan.Tasks.GUITaskColumns.s_DetailedDescription);


            m_columnOrder = new List<string>();
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Id);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Urgency);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Resources);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_TentativelyAssignedResources);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Description);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_AffectedComponent);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Status);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Priority);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_EndDate);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_StartDate);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Attachments);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Remarks);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Owner);


        }

        private CustomGUIControls.Grid.GridControl.CellDoubleClick m_taskWindowOpenFunction = null;
        private ActiveProjectsOnlyFn m_activeProjectsOnly;
        private TasksToDisplayFn m_tasksToDisplay;

        public delegate bool ActiveProjectsOnlyFn();
        public delegate GUIProject.TasksDisplayValues TasksToDisplayFn();


        public void WindowClosed()
        {
            m_viewImplementation.WindowClosed();
        }


        public void Redisplay()
        {
            Redisplay(ProjectItemOnDisplay);
        }

        public void Redisplay(CustomGUIControls.IDisplayItem itemToDisplay)
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this, itemToDisplay))
                return;

            GUIProject projectItemToDisplay = itemToDisplay as GUIProject;
            GUIProject projectItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIProject;


            if (projectItemOnDisplay != null &&
                projectItemToDisplay == projectItemOnDisplay &&
                !projectItemOnDisplay.IsDeleted &&
                !projectItemOnDisplay.DBProject.IsPrivateToAnotherAndHidden)
            {
                labelProjectName.Content = projectItemOnDisplay.Name;
                int noOfTasks = projectItemOnDisplay.ActiveTaskCount;
                int noOfSubTasks = projectItemOnDisplay.TotalActiveTaskCount;
                string priorityString = projectItemOnDisplay.Priority;
                if (string.IsNullOrEmpty(priorityString))
                    priorityString = GUIProjectColumns.s_priortyMed;
                LabelTaskCount.Content = "(" + priorityString + " - " + noOfTasks + " / " + noOfSubTasks + ")";

                //DestroySubPanel();
                if (m_detailVisible == true)
                    RedisplaySubPanel();
                //CreateSubPanel();  //TODO - make this 'redsiplay'
            }
            else
                WindowClosed();
        }

        public GUIProject ProjectItemOnDisplay
        {
            get
            {
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                    return m_viewImplementation.FirstItemToDisplay as GUIProject;
                return null;
            }
        }




        public void OpenDetail()
        {
            m_detailVisible = false;
            buttonDetail_Click(null, null);
        }



        private void DestroySubPanel()
        {
            int currentItem = 1;
            while (stackPanel1.Children.Count > currentItem)
            {
                bool deletePanel = false;
                UIElement thisChild = stackPanel1.Children[currentItem];
                ProjectStackControl childStackControl = thisChild as ProjectStackControl;
                if (childStackControl != null)
                {
                    childStackControl.RemoveChildren();
                    deletePanel = true;
                }
                else
                {
                    System.Windows.Forms.Integration.WindowsFormsHost host = thisChild as System.Windows.Forms.Integration.WindowsFormsHost;
                    if (host != null)
                    {
                        CustomGUIControls.Grid.GridControl gridControl = host.Child as CustomGUIControls.Grid.GridControl;
                        gridControl.WindowClosed();
                        deletePanel = true;
                    }
                }

                if (deletePanel == true)
                    stackPanel1.Children.RemoveAt(currentItem);
                else
                    currentItem++;
            }
        }

        private void RedisplaySubPanel()
        {
            bool gridFound = false;
            for (int index = 0; index < stackPanel1.Children.Count; index++)
            {
                UIElement thisChild = stackPanel1.Children[index];
                ProjectStackControl childStackControl = thisChild as ProjectStackControl;
                if (childStackControl != null)
                {
                    List<GUIProject> subProjects = null;
                    List<GUIProject> allSubItems = new List<GUIProject>(ProjectItemOnDisplay.SubItems);

                    subProjects = new List<GUIProject>();
                    foreach (GUIProject currentProject in allSubItems)
                    {
                        if (currentProject.DBProject.IsHidden)
                            continue;
                        if (!m_activeProjectsOnly() || (currentProject.DBProject.IsActive || currentProject.DBProject.HasNeverBeenSaved))
                            subProjects.Add(currentProject);
                    }

                    subProjects.Sort(GUIProject.SortProjectsByPriority);

                    int maxComponentIndex = Math.Max(subProjects.Count, childStackControl.ChildCount);

                    for (int componentIndex = 0; componentIndex < maxComponentIndex; componentIndex++)
                    {
                        ProjectControl currentComponentOnDisplay = childStackControl.ProjectAt(componentIndex);
                        GUIProject currentComponentToBeDisplayed = componentIndex < subProjects.Count ?
                                                                     subProjects[componentIndex] : null;

                        // Delete on display item
                        if (currentComponentToBeDisplayed == null || (currentComponentOnDisplay != null && string.Compare(currentComponentToBeDisplayed.Name, currentComponentOnDisplay.ProjectItemOnDisplay.Name) > 0))
                        {

                            childStackControl.RemoveProjectAt(componentIndex);
                            componentIndex--;
                            maxComponentIndex--;
                            continue;
                        }

                        // Add on display item
                        if (currentComponentOnDisplay == null || (currentComponentToBeDisplayed != null && string.Compare(currentComponentToBeDisplayed.Name, currentComponentOnDisplay.ProjectItemOnDisplay.Name) < 0))
                        {
                            childStackControl.InsertProject(componentIndex, currentComponentToBeDisplayed, false, m_activeProjectsOnly, m_tasksToDisplay);
                            continue;
                        }

                        childStackControl.ProjectAt(componentIndex).Redisplay();
                    }
                }
                else
                {
                    System.Windows.Forms.Integration.WindowsFormsHost host = thisChild as System.Windows.Forms.Integration.WindowsFormsHost;
                    if (host != null)
                    {
                        CustomGUIControls.Grid.GridControl gridControl = host.Child as CustomGUIControls.Grid.GridControl;
                        if (gridControl != null)
                        {
                            gridFound = true;
                            gridControl.ClearRows();

                            if (m_tasksToDisplay() == GUIProject.TasksDisplayValues.None
                                || ProjectItemOnDisplay.Tasks(m_tasksToDisplay()).Count == 0)
                            {
                                gridControl.WindowClosed();
                                stackPanel1.Children.Remove(thisChild);
                                index--;
                            }
                            else
                            {



                                gridControl.ClearRows();
                                foreach (CustomGUIControls.Grid.IGridItem task in ProjectItemOnDisplay.Tasks(m_tasksToDisplay()))
                                {
                                    gridControl.AddDisplayItem(task);
                                }

                                gridControl.SetFilters();
                            }
                        }
                    }
                }
            }
            if (!gridFound &&
                m_tasksToDisplay() != GUIProject.TasksDisplayValues.None &&
                ProjectItemOnDisplay.Tasks(m_tasksToDisplay()).Count > 0)
            {
                AddTaskGrid();
            }
        }

        private void AddTaskGrid()
        {
            GUIProject projectItemOnDisplay = ProjectItemOnDisplay;

            CustomGUIControls.Grid.GridControl gridControl = new CustomGUIControls.Grid.GridControl(true, false);
            gridControl.SetDoubleClickFunction(m_taskWindowOpenFunction);
            if (Permissions.IsAllowed(projectItemOnDisplay.DBProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
            {
                gridControl.SetCellDeleteFunction(TaskMan.Tasks.GUITask.DeleteTask);
                gridControl.SetCheckCellDeleteFunction(TaskMan.Tasks.GUITask.ConfirmDeleteTask);
            }
            gridControl.SetColumnOrder(m_columnOrder);

            foreach (string hiddenColumn in m_hiddenTaskColumns)
            {
                gridControl.ColumnVisible(hiddenColumn, false);
            }

            //{

            //    var source = PresentationSource.FromVisual(stackPanel1);
            //    Matrix transformToDevice = source.CompositionTarget.TransformToDevice;
            //    var pixelSize = (Size)transformToDevice.Transform((Vector)stackPanel1.RenderSize);

            //    gridControl.Width = (int)pixelSize.Width;
            //}



            gridControl.AllowCellDrop = true;
            gridControl.SetColumns(TaskMan.Tasks.GUITaskColumns.Instance);

            foreach (CustomGUIControls.Grid.IGridItem task in projectItemOnDisplay.Tasks(m_tasksToDisplay()))
            {
                gridControl.AddDisplayItem(task);
            }
            gridControl.SetFilters(true);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Id, 40);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Description, 300);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Priority, 90);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Urgency, 65);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_EndDate, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_StartDate, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_EffortInDays, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_PercentageAllocation, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_RequestedBy, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Resources, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_TentativelyAssignedResources, -1);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Attachments, 50);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Remarks, 50);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Owner, 50);

            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_AffectedComponent, 200);
            gridControl.ColumnWidth(Tasks.GUITaskColumns.s_Projects, 200);

            gridControl.SetDefaultSort(Tasks.GUITaskColumns.s_Urgency, System.ComponentModel.ListSortDirection.Descending);

            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();
            host.Width = double.NaN;
            host.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            host.Child = gridControl;
            gridControl.SetFilters(true);


            stackPanel1.Children.Insert(1, host);

        }


        private void CreateSubPanel()
        {
            GUIProject projectItemOnDisplay = ProjectItemOnDisplay;
            if (projectItemOnDisplay != null)
            {

                if (m_tasksToDisplay() != GUIProject.TasksDisplayValues.None
                   && projectItemOnDisplay.Tasks(m_tasksToDisplay()).Count > 0)
                {
                    AddTaskGrid();
                }

                List<GUIProject> subItems = new List<GUIProject>(projectItemOnDisplay.SubItems);
                subItems.Sort(GUIProject.SortProjectsByPriority);

                ProjectStackControl ctrl = new ProjectStackControl(m_taskWindowOpenFunction);

                ctrl.scrollViewer1.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

                foreach (GUIProject currentProject in subItems)
                {
                    if (currentProject.DBProject.IsHidden)
                        continue;
                    if (!m_activeProjectsOnly() || (m_activeProjectsOnly() && currentProject.DBProject.IsActive))
                        ctrl.AddProject(currentProject, false, m_activeProjectsOnly, m_tasksToDisplay);

                }

                stackPanel1.Children.Add(ctrl);
            }


        }



        private void imageAddTask_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                GUIProject projectItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIProject;
                if (projectItemOnDisplay != null && Permissions.IsAllowed(projectItemOnDisplay.DBProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                {
                    TaskMan.Tasks.TaskDetail taskWindow = new TaskMan.Tasks.TaskDetail(projectItemOnDisplay.DBProject);
                    taskWindow.ShowDialog();
                    Functions.ClearDisplayCaches();
                    projectItemOnDisplay.Redisplay();
                }
            }
        }

        public void AddDisplayItem(CustomGUIControls.IDisplayItem itemToDisplay)
        {
            throw new Exception("AddDisplayItem doesn't make sense for ProjectControl");
        }

        public void RemoveDisplayItem(CustomGUIControls.IDisplayItem itemToDisplay)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
            //throw new Exception("RemoveDisplayItem doesn't make sense for ProjectControl");
        }


        Point? m_dragStart = null;

        private void labelProjectName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_dragStart = e.GetPosition(labelProjectName);
            e.Handled = true;
        }

        private void labelProjectName_MouseLeave(object sender, MouseEventArgs e)
        {
            if (m_dragStart != null)
            {

                GUIProject projectItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIProject;

                if (Permissions.IsAllowed(projectItemOnDisplay.DBProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                {
                    DataObject data = Utils.DragDrop.DragHelper.SetDraggedObjectWPF(projectItemOnDisplay);

                    DragDropEffects effectToUse = DragDropEffects.Copy | DragDropEffects.Move;
                    bool ctrlButtonDown =
                            System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) ||
                            System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);
                    if (ctrlButtonDown)
                        effectToUse = DragDropEffects.Link;

                    DragDrop.DoDragDrop(labelProjectName, data, effectToUse);
                }
            }
            labelProjectName.Foreground = Brushes.Black;
        }

        private void labelProjectName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            m_dragStart = null;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                {
                    GUIProject projectItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIProject;
                    if (projectItemOnDisplay != null)
                    {
                        ProjectDetail.GetAndShowDetailWindow(projectItemOnDisplay);
                    }
                }
                e.Handled = true;
            }
        }

        private void labelProjectName_DragEnter(object sender, DragEventArgs e)
        {
            GUIProject projectItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIProject;

            if (Permissions.IsAllowed(projectItemOnDisplay.DBProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
            {
                object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

                GUIProject sourceProject = draggedObject as GUIProject;
                if (sourceProject != null)
                {
                    if (sourceProject.DBProject != projectItemOnDisplay.DBProject &&
                        !projectItemOnDisplay.DBProject.IsDescendantOf(sourceProject.DBProject))
                    {
                        e.Effects = DragDropEffects.Move;
                        labelProjectName.Foreground = s_textDropHighlightColour;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                        e.Handled = true;
                    }
                    return;
                }

                TaskMan.Tasks.GUITask draggedTask = draggedObject as TaskMan.Tasks.GUITask;
                if (draggedTask != null)
                {
                    if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                        e.Effects = DragDropEffects.Copy; // Add Project
                    else
                        e.Effects = DragDropEffects.Move;

                    labelProjectName.Foreground = s_textDropHighlightColour;
                    return;
                }

            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;

        }

        private void labelProjectName_DragOver(object sender, DragEventArgs e)
        {
            labelProjectName_DragEnter(sender, e);
        }

        private void labelProjectName_Drop(object sender, DragEventArgs e)
        {
            Functions.ClearDisplayCaches();
            labelProjectName.Foreground = s_black;

            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            GUIProject source = draggedObject as GUIProject;
            if (source != null)
            {
                GUIProject componentItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIProject;

                if (source != componentItemOnDisplay &&
                    !componentItemOnDisplay.DBProject.IsDescendantOf(source.DBProject))
                {
                    source.SetNewParent(componentItemOnDisplay.DBProject);
                }

                e.Handled = true;
                return;
            }
            TaskMan.Tasks.GUITask draggedTask = draggedObject as TaskMan.Tasks.GUITask;

            if (draggedTask != null)
            {
                DBTaskMan.Project origProject = draggedTask.DBTask.Project;

                DBTaskMan.Project newProject = ProjectItemOnDisplay.DBProject;

                if (newProject != null)
                {
                    DateTime? initialStartDate = draggedTask.DBTask.StartDate;
                    draggedTask.DBTask.Project = newProject;
                    if (initialStartDate.HasValue)
                        draggedTask.DBTask.StartDate = initialStartDate.Value;
                }

                ApplicationTaskMan.Instance.RefreshAllWindows();

                e.Handled = true;
                return;
            }

        }

        private void labelProjectName_DragLeave(object sender, DragEventArgs e)
        {
            labelProjectName.Foreground = s_black;

        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            labelProjectName.Foreground = s_black;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            labelProjectName.Foreground = s_textDropHighlightColour;
        }

        private void menuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectItemOnDisplay.DBProject.HasDependants)
            {
                System.Windows.MessageBox.Show("The project has dependants (Tasks or sub Projects) so it cannot be deleted", "Cannot delete Project",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }

            if (System.Windows.MessageBox.Show("Are you sure you want to delete the Project?", "Delete Project",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                    == System.Windows.MessageBoxResult.Yes)
            {
                GUIProject.DeleateAllDisplayItems(ProjectItemOnDisplay.DBProject);

                ApplicationTaskMan.Instance.RefreshAllWindows();
            }
        }

        private void menuItemRename_Click(object sender, RoutedEventArgs e)
        {
            NewProject window = new NewProject(ProjectItemOnDisplay.DBProject, NewProject.Mode.Rename);
            System.Windows.Forms.DialogResult result = window.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string newProjectName = window.ProjectName;
                if (string.IsNullOrEmpty(newProjectName) || newProjectName == ProjectItemOnDisplay.Name)
                {
                    return;
                }

                ProjectItemOnDisplay.Name = newProjectName;

                //DBTaskMan.Project parent = ProjectItemOnDisplay.DBProject.Parent;
                //if (parent == null)
                //    ProjectDetail.RedisplayAll();
                //else
                //    GUIProject.Redisplay(parent);


                ApplicationTaskMan.Instance.RefreshAllWindows();
            }
        }




        private bool m_detailVisible = false;
        private void buttonDetail_Click(object sender, RoutedEventArgs e)
        {
            m_detailVisible = !m_detailVisible;

            if (m_detailVisible)
            {
                CreateSubPanel();
                buttonDetail.Content = "-";

            }
            else
            {
                DestroySubPanel();
                buttonDetail.Content = "+";

            }
        }

        private void labelProjectName_MouseEnter(object sender, MouseEventArgs e)
        {
            labelProjectName.Foreground = Brushes.Blue;
        }
    }
}
