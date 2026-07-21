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

namespace TaskMan.Components
{
    /// <summary>
    /// Interaction logic for ComponentControl.xaml
    /// </summary>
    public partial class ComponentControl : UserControl, CustomGUIControls.IView
    {

        CustomGUIControls.ViewImpl m_viewImplementation = null;
        static Brush s_textDropHighlightColour = new SolidColorBrush(Color.FromRgb(153, 50, 204)); // DarkOrchid
        static Brush s_black = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        private TasksToDisplayFn m_tasksToDisplay;
        public delegate GUIComponent.TasksDisplayValues TasksToDisplayFn();

        public ComponentControl(GUIComponent itemToDisplay,
                                CustomGUIControls.Grid.GridControl.CellDoubleClick taskWindowOpenFunction,
                                bool openSubComponents,
                                 TasksToDisplayFn tasksToDisplay)
        {
            InitializeComponent();
            m_tasksToDisplay = tasksToDisplay;
            m_viewImplementation = new CustomGUIControls.ViewImpl(this);
            itemToDisplay.AddView(this);
            m_viewImplementation.AddDisplayItem(itemToDisplay);

            labelComponentName.Content = itemToDisplay.Name;
            // TODO DBComponent is null on a new component, it should be set to the current user
            bool isEditAllowed = Permissions.IsPowerUser || Permissions.IsAllowed(itemToDisplay.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit);
            if (!isEditAllowed)
            {
                contextMenuComponent.IsEnabled = false;
                labelComponentName.ContextMenu = null;
                imageAddTask.Opacity = 0.4;
            }


            int noOfTasks = itemToDisplay.ActiveTaskCount;
            int noOfSubTasks = itemToDisplay.TotalActiveTaskCount;
            LabelTaskCount.Content = "(" + noOfTasks + " / " + noOfSubTasks + ")";

            m_taskWindowOpenFunction = taskWindowOpenFunction;
            if (openSubComponents)
            {
                buttonDetail.Content = "+";
                CreateSubPanel();
                m_detailVisible = true;
            }
        }

        private CustomGUIControls.Grid.GridControl.CellDoubleClick m_taskWindowOpenFunction = null;
        static private IList<string> m_hiddenTaskColumns = null;
        static private IList<string> m_columnOrder = null;

        static ComponentControl()
        {
            m_hiddenTaskColumns = new List<string>();
            m_hiddenTaskColumns.Add(TaskMan.Tasks.GUITaskColumns.s_AffectedComponent);
            m_hiddenTaskColumns.Add(TaskMan.Tasks.GUITaskColumns.s_DetailedDescription);

            m_columnOrder = new List<string>();
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Id);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Urgency);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Resources);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_TentativelyAssignedResources);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Description);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Projects);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Status);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Priority);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_EndDate);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_StartDate);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Attachments);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Remarks);
            m_columnOrder.Add(TaskMan.Tasks.GUITaskColumns.s_Owner);
        }


        public void WindowClosed()
        {
            m_viewImplementation.WindowClosed();
        }

        //GUIComponent m_underlyingItem = null;

        public void Redisplay()
        {
            Redisplay(ComponentItemOnDisplay);
        }

        public void Redisplay(CustomGUIControls.IDisplayItem itemToDisplay)
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(this, itemToDisplay))
                return;

            GUIComponent componentItemOnDisplay = ComponentItemOnDisplay;
            GUIComponent componentItemToDisplay = itemToDisplay as GUIComponent;


            if (componentItemOnDisplay != null &&
                componentItemToDisplay == componentItemOnDisplay &&
                !componentItemOnDisplay.IsDeleted)
            {
                labelComponentName.Content = componentItemOnDisplay.Name;
                int noOfTasks = componentItemOnDisplay.ActiveTaskCount;
                int noOfSubTasks = componentItemOnDisplay.TotalActiveTaskCount;
                LabelTaskCount.Content = "(" + noOfTasks + " / " + noOfSubTasks + ")";

                if (m_detailVisible == true)
                    RedisplaySubPanel();
            }

            else
                WindowClosed();

        }

        public GUIComponent ComponentItemOnDisplay
        {
            get
            {
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                    return m_viewImplementation.FirstItemToDisplay as GUIComponent;
                return null;
            }
        }


        private static int SortComponentName(GUIComponent a, GUIComponent b)
        {
            return string.Compare(a.Name, b.Name);
        }

        private void DestroySubPanel()
        {
            while (stackPanel1.Children.Count > 1)
            {
                UIElement thisChild = stackPanel1.Children[1];
                ComponentStackControl childStackControl = thisChild as ComponentStackControl;
                if (childStackControl != null)
                {
                    childStackControl.RemoveChildren();
                }
                else
                {
                    System.Windows.Forms.Integration.WindowsFormsHost host = thisChild as System.Windows.Forms.Integration.WindowsFormsHost;
                    if (host != null)
                    {
                        CustomGUIControls.Grid.GridControl gridControl = host.Child as CustomGUIControls.Grid.GridControl;
                        gridControl.WindowClosed();
                    }
                }

                stackPanel1.Children.RemoveAt(1);
            }
        }

        private void RedisplaySubPanel()
        {
            bool gridFound = false;
            for (int index = 0; index < stackPanel1.Children.Count; index++)
            {
                UIElement thisChild = stackPanel1.Children[index];
                ComponentStackControl childStackControl = thisChild as ComponentStackControl;
                if (childStackControl != null)
                {
                    List<GUIComponent> subItems = new List<GUIComponent>(ComponentItemOnDisplay.SubItems);
                    subItems.Sort(SortComponentName);

                    int maxComponentIndex = Math.Max(subItems.Count, childStackControl.ChildCount);

                    for (int componentIndex = 0; componentIndex < maxComponentIndex; componentIndex++)
                    {
                        ComponentControl currentComponentOnDisplay = childStackControl.ComponentAt(componentIndex);
                        GUIComponent currentComponentToBeDisplayed = componentIndex < subItems.Count ?
                                                                     subItems[componentIndex] : null;

                        // Delete on display item
                        if (currentComponentToBeDisplayed == null || (currentComponentOnDisplay != null && string.Compare(currentComponentToBeDisplayed.Name, currentComponentOnDisplay.ComponentItemOnDisplay.Name) > 0))
                        {

                            childStackControl.RemoveComponentAt(componentIndex);
                            componentIndex--;
                            maxComponentIndex--;
                            continue;
                        }

                        // Add on display item
                        if (currentComponentOnDisplay == null || (currentComponentToBeDisplayed != null && string.Compare(currentComponentToBeDisplayed.Name, currentComponentOnDisplay.ComponentItemOnDisplay.Name) < 0))
                        {
                            childStackControl.InsertComponent(componentIndex, currentComponentToBeDisplayed, false, m_tasksToDisplay);
                            continue;
                        }

                        childStackControl.ComponentAt(componentIndex).Redisplay();
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

                            if (m_tasksToDisplay() == GUIComponent.TasksDisplayValues.None
                                || ComponentItemOnDisplay.Tasks(m_tasksToDisplay()).Count == 0)
                            {
                                gridControl.WindowClosed();
                                stackPanel1.Children.Remove(thisChild);
                                index--;
                            }
                            else
                            {
                                gridControl.ClearRows();
                                foreach (CustomGUIControls.Grid.IGridItem task in ComponentItemOnDisplay.Tasks(m_tasksToDisplay()))
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
                m_tasksToDisplay() != GUIComponent.TasksDisplayValues.None &&
                ComponentItemOnDisplay.Tasks(m_tasksToDisplay()).Count > 0)
            {
                AddTaskGrid();
            }
        }

        private void AddTaskGrid()
        {
            GUIComponent componentItemOnDisplay = ComponentItemOnDisplay;

            CustomGUIControls.Grid.GridControl gridControl = new CustomGUIControls.Grid.GridControl(true, false);
            gridControl.SetDoubleClickFunction(m_taskWindowOpenFunction);
            if (Permissions.IsPowerUser || Permissions.IsAllowed(componentItemOnDisplay.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
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

            foreach (CustomGUIControls.Grid.IGridItem task in componentItemOnDisplay.Tasks(m_tasksToDisplay()))
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
            GUIComponent componentItemOnDisplay = ComponentItemOnDisplay;

            if (componentItemOnDisplay != null)
            {
                if (m_tasksToDisplay() != GUIComponent.TasksDisplayValues.None
                    && ComponentItemOnDisplay.Tasks(m_tasksToDisplay()).Count > 0)
                {
                    AddTaskGrid();
                }

                List<GUIComponent> subItems = new List<GUIComponent>(componentItemOnDisplay.SubItems);
                subItems.Sort(SortComponentName);

                ComponentStackControl ctrl = new ComponentStackControl(m_taskWindowOpenFunction);

                ctrl.scrollViewer1.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

                foreach (GUIComponent currentComponent in subItems)
                {
                    ctrl.AddComponent(currentComponent, false, m_tasksToDisplay);
                }

                stackPanel1.Children.Add(ctrl);
            }


        }



        private void imageAddTask_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                GUIComponent componentItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIComponent;
                if (componentItemOnDisplay != null && (Permissions.IsPowerUser || Permissions.IsAllowed(componentItemOnDisplay.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit)))
                {
                    TaskMan.Tasks.TaskDetail taskWindow = new TaskMan.Tasks.TaskDetail(componentItemOnDisplay.DBComponent);
                    taskWindow.ShowDialog();
                    Functions.ClearDisplayCaches();
                    componentItemOnDisplay.Redisplay();
                }
            }
        }

        public void AddDisplayItem(CustomGUIControls.IDisplayItem itemToDisplay)
        {
            throw new Exception("AddDisplayItem doesn't make sense for ComponentControl");
        }

        public void RemoveDisplayItem(CustomGUIControls.IDisplayItem itemToDisplay)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
            //throw new Exception("RemoveDisplayItem doesn't make sense for ComponentControl");
        }

        Point? m_dragStart = null;

        private void labelComponentName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_dragStart = e.GetPosition(labelComponentName);
            e.Handled = true;
        }




        private void labelComponentName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            m_dragStart = null;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                {
                    GUIComponent componentItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIComponent;
                    if (componentItemOnDisplay != null)
                    {
                        ComponentWindow newWindow = ComponentWindow.GetInstanceFromGUIComponent(componentItemOnDisplay);
                        newWindow.Show();
                        newWindow.Focus();
                        e.Handled = true;
                    }
                }
                e.Handled = true;
            }
        }

        private void labelComponentName_MouseLeave(object sender, MouseEventArgs e)
        {
            if (m_dragStart != null)
            {
                GUIComponent componentItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIComponent;

                if (Permissions.IsAllowed(componentItemOnDisplay.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
                {
                    DataObject data = Utils.DragDrop.DragHelper.SetDraggedObjectWPF(componentItemOnDisplay);

                    DragDrop.DoDragDrop(labelComponentName, data, DragDropEffects.All);
                }
            }
            labelComponentName.Foreground = Brushes.Black;
        }

        private void labelComponentName_DragEnter(object sender, DragEventArgs e)
        {
            GUIComponent componentItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIComponent;
            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);

            if (Permissions.IsPowerUser || Permissions.IsAllowed(componentItemOnDisplay.DBComponent.Owner, Permissions.EntityType.Component, Permissions.ChangeType.Edit))
            {
                GUIComponent source = draggedObject as GUIComponent;
                if (source != null)
                {
                    if (source.DBComponent != componentItemOnDisplay.DBComponent &&
                        !componentItemOnDisplay.DBComponent.IsDescendantOf(source.DBComponent))
                    {
                        e.Effects = DragDropEffects.Move;
                        labelComponentName.Foreground = s_textDropHighlightColour;
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
                    e.Effects = DragDropEffects.Move;
                    labelComponentName.Foreground = s_textDropHighlightColour;
                    e.Handled = true;
                    return;
                }

            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;

        }

        private void labelComponentName_Drop(object sender, DragEventArgs e)
        {
            Functions.ClearDisplayCaches();
            labelComponentName.Foreground = s_black;

            object draggedObject = Functions.ToGUIObjectIfPossible(Utils.DragDrop.DragHelper.DraggedObject);


            GUIComponent sourceComponent = draggedObject as GUIComponent;


            if (sourceComponent != null)
            {
                GUIComponent componentItemOnDisplay = m_viewImplementation.FirstItemToDisplay as GUIComponent;

                if (sourceComponent != componentItemOnDisplay &&
                    !componentItemOnDisplay.DBComponent.IsDescendantOf(sourceComponent.DBComponent))
                {
                    sourceComponent.SetNewParent(componentItemOnDisplay.DBComponent);
                }
                return;
            }

            TaskMan.Tasks.GUITask draggedTask = draggedObject as TaskMan.Tasks.GUITask;
            if (draggedTask != null)
            {
                draggedTask.DBTask.AffectedComponent = ComponentItemOnDisplay.DBComponent;

                Functions.ClearDisplayCaches();
                ApplicationTaskMan.Instance.RefreshAllWindows();
                return;
            }

        }

        private void labelComponentName_DragOver(object sender, DragEventArgs e)
        {
            labelComponentName_DragEnter(sender, e);
        }

        private void labelComponentName_DragLeave(object sender, DragEventArgs e)
        {
            labelComponentName.Foreground = s_black;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            labelComponentName.Foreground = s_textDropHighlightColour;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            labelComponentName.Foreground = s_black;
        }

        private void menuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ComponentItemOnDisplay.DBComponent.HasDependants)
            {
                System.Windows.MessageBox.Show("The component has dependants (Task or sub Components) so it cannot be deleted", "Cannot delete Component",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }

            if (System.Windows.MessageBox.Show("Are you sure you want to delete the Component?", "Delete Component",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                    == System.Windows.MessageBoxResult.Yes)
            {
                GUIComponent.DeleateAllDisplayItems(ComponentItemOnDisplay.DBComponent);
                Functions.ClearDisplayCaches();
                ApplicationTaskMan.Instance.RefreshAllWindows();
            }
        }

        private void menuItemRename_Click(object sender, RoutedEventArgs e)
        {
            NewComponent window = new NewComponent(ComponentItemOnDisplay.DBComponent, NewComponent.Mode.Rename);
            System.Windows.Forms.DialogResult result = window.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string newComponentName = window.ComponentName;
                if (string.IsNullOrEmpty(newComponentName) || newComponentName == ComponentItemOnDisplay.Name)
                {
                    return;
                }

                ComponentItemOnDisplay.Name = newComponentName;

                //DBTaskMan.Component parent = ComponentItemOnDisplay.DBComponent.Parent;
                //if (parent == null)
                //    ComponentWindow.RedisplayAll();
                //else
                //    GUIComponent.Redisplay(parent);
                Functions.ClearDisplayCaches();
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

        private void labelComponentName_MouseEnter(object sender, MouseEventArgs e)
        {
            labelComponentName.Foreground = Brushes.Blue;
        }


    }
}


