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

namespace TaskMan.Projects
{
    /// <summary>
    /// Interaction logic for ProjectStackControl.xaml
    /// </summary>
    public partial class ProjectStackControl : UserControl
    {
        public ProjectStackControl(CustomGUIControls.Grid.GridControl.CellDoubleClick taskWindowOpenFunction)
        {
            InitializeComponent();
            m_taskWindowOpenFunction = taskWindowOpenFunction;
        }

        public void AddProject(GUIProject itemToDisplay, bool openSubProjects, 
            ProjectControl.ActiveProjectsOnlyFn activeProjectsOnly,
            ProjectControl.TasksToDisplayFn tasksToDisplay)
        {
            ProjectControl ctrl = new ProjectControl(itemToDisplay, m_taskWindowOpenFunction, openSubProjects,
                activeProjectsOnly, tasksToDisplay);

            stackPanel1.Children.Add(ctrl);

        }

        public void InsertProject(int index, GUIProject itemToDisplay, bool openSubComponents,
            ProjectControl.ActiveProjectsOnlyFn activeProjectsOnly,
            ProjectControl.TasksToDisplayFn tasksToDisplay)
        {
            ProjectControl ctrl = new ProjectControl(itemToDisplay, m_taskWindowOpenFunction, openSubComponents,
                activeProjectsOnly, tasksToDisplay);

            stackPanel1.Children.Insert(index, ctrl);
        }

        private CustomGUIControls.Grid.GridControl.CellDoubleClick m_taskWindowOpenFunction = null;

        public int ChildCount { get { return stackPanel1.Children.Count; } }

        public void RemoveChildren()
        {
            foreach (UIElement child in stackPanel1.Children)
            {
                ProjectControl ctrl = child as ProjectControl;
                if (ctrl != null)
                {
                    ctrl.WindowClosed();
                }

            }

            stackPanel1.Children.Clear();

        }

        public ProjectControl ProjectAt(int index)
        {
            if (index >= stackPanel1.Children.Count)
                return null;
            return stackPanel1.Children[index] as ProjectControl;
        }

        public void RemoveProjectAt(int index)
        {
            if (index >= stackPanel1.Children.Count)
                return;
            UIElement child = stackPanel1.Children[index];
            ProjectControl ctrl = child as ProjectControl;
            if (ctrl != null)
            {
                ctrl.WindowClosed();
            }
            stackPanel1.Children.RemoveAt(index);
        }

    }
}
